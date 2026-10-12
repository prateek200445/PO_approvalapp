using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Today's birthdays (empinfo.DOB) and work anniversaries (empinfo.DateOJ) of active employees in the
/// logged-in user's company (LoginRights.EmpCode → empinfo.CompanyName).
/// </summary>
public sealed class HrCelebrationService
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(30);

    private readonly DatabaseService _database;
    private readonly HrAccessService _access;
    private readonly IMemoryCache _cache;

    public HrCelebrationService(DatabaseService database, HrAccessService access, IMemoryCache cache)
    {
        _database = database;
        _access = access;
        _cache = cache;
    }

    public async Task<HrCelebrationsDto> GetTodayAsync(string? username)
    {
        var today = DateTime.Today;
        var access = await _access.ResolveAsync(username);
        if (string.IsNullOrWhiteSpace(access.EmpCode))
            return new HrCelebrationsDto(today, "", null, [], []);

        var empCode = access.EmpCode.Trim();
        var company = await CompanyOfAsync(empCode);
        if (company.Length == 0)
            return new HrCelebrationsDto(today, "", empCode, [], []);

        var rows = await _cache.GetOrCreateAsync($"hr-celebrations:{today:yyyyMMdd}:{company.ToUpperInvariant()}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            // 29 Feb birthdays / joinings are celebrated on 28 Feb in non-leap years.
            var days = today is { Month: 2, Day: 28 } && !DateTime.IsLeapYear(today.Year)
                ? new[] { 28, 29 }
                : new[] { today.Day };
            using var connection = _database.CreatePayrollLoginEntryConnection();
            return (await connection.QueryAsync<CelebrationRow>(@"
SELECT LTRIM(RTRIM(e.EmpCode)) AS EmpCode, LTRIM(RTRIM(e.Name)) AS Name, LTRIM(RTRIM(e.Designation)) AS Designation,
       LTRIM(RTRIM(e.Deptt)) AS Department, LTRIM(RTRIM(e.Branch)) AS Branch, e.DOB AS Dob, e.DateOJ AS DateOfJoining
FROM empinfo e WITH (NOLOCK)
WHERE LOWER(LTRIM(RTRIM(ISNULL(e.isactive, '')))) = 'yes'
  AND LTRIM(RTRIM(e.CompanyName)) = @Company
  AND (   (e.DOB IS NOT NULL AND YEAR(e.DOB) > 1900 AND MONTH(e.DOB) = @Month AND DAY(e.DOB) IN @Days)
       OR (e.DateOJ IS NOT NULL AND YEAR(e.DateOJ) < @Year AND MONTH(e.DateOJ) = @Month AND DAY(e.DateOJ) IN @Days))",
                new { Company = company, Month = today.Month, Days = days, Year = today.Year }, commandTimeout: 60)).ToList();
        }) ?? [];

        bool IsToday(DateTime? d) => d is { } v && v.Month == today.Month
            && (v.Day == today.Day || (today is { Month: 2, Day: 28 } && !DateTime.IsLeapYear(today.Year) && v.Day == 29));

        var birthdays = rows.Where(r => r.Dob is { Year: > 1900 } && IsToday(r.Dob))
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new HrCelebrationPersonDto(r.EmpCode, r.Name, r.Designation, r.Department, r.Branch, null,
                string.Equals(r.EmpCode, empCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var anniversaries = rows.Where(r => r.DateOfJoining is { } doj && doj.Year < today.Year && IsToday(doj))
            .OrderByDescending(r => today.Year - r.DateOfJoining!.Value.Year).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new HrCelebrationPersonDto(r.EmpCode, r.Name, r.Designation, r.Department, r.Branch,
                today.Year - r.DateOfJoining!.Value.Year, string.Equals(r.EmpCode, empCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return new HrCelebrationsDto(today, company, empCode, birthdays, anniversaries);
    }

    /// <summary>empinfo.CompanyName of an employee ("" when unknown).</summary>
    public async Task<string> CompanyOfAsync(string? empCode)
    {
        var code = (empCode ?? "").Trim();
        if (code.Length == 0) return "";
        return await _cache.GetOrCreateAsync($"hr-celebrations:company:{code.ToUpperInvariant()}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            using var connection = _database.CreatePayrollLoginEntryConnection();
            return await connection.ExecuteScalarAsync<string?>(@"
SELECT TOP 1 LTRIM(RTRIM(CompanyName)) FROM empinfo WITH (NOLOCK) WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode",
                new { EmpCode = code }, commandTimeout: 30);
        }) ?? "";
    }

    private sealed class CelebrationRow
    {
        public string EmpCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Branch { get; set; }
        public DateTime? Dob { get; set; }
        public DateTime? DateOfJoining { get; set; }
    }
}

public record HrCelebrationPersonDto(string EmpCode, string Name, string? Designation, string? Department, string? Branch,
    int? Years, bool IsYou);

public record HrCelebrationsDto(DateTime Date, string Company, string? EmpCode,
    IReadOnlyList<HrCelebrationPersonDto> Birthdays, IReadOnlyList<HrCelebrationPersonDto> Anniversaries);
