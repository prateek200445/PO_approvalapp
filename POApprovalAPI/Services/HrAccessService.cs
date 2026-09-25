using Dapper;
using Microsoft.Extensions.Options;

namespace POApprovalAPI.Services;

public sealed class HrReportsOptions
{
    public const string SectionName = "HrReports";

    /// <summary>
    /// Portal usernames (LoginRights.Name) with full HR access — all employees,
    /// leave/WFH approve, leave credit, confirmation, approve month-end verify.
    /// Includes Corporate HR, HO HR, and designated authorities (e.g. prakash).
    /// Portal features are additive; existing ERP leave/HR functions stay as-is.
    /// Everyone else uses employee self-service for their own EmpCode.
    /// </summary>
    public List<string> FullAccessUsers { get; set; } = [];
}

public sealed class HrAccessDto
{
    public string Username { get; set; } = "";
    public string? EmpCode { get; set; }
    public string? FullName { get; set; }
    public bool HasFullAccess { get; set; }
    /// <summary>True when EmpCode is linked — view own attendance + request month-end verify.</summary>
    public bool CanUseSelfService { get; set; }
    /// <summary>Employees cannot apply leave/WFH or credit PL/CL.</summary>
    public bool IsViewOnly { get; set; }
    public string Mode { get; set; } = "none"; // full | self | none
    public string Message { get; set; } = "";
}

public sealed class HrAccessService
{
    private readonly DatabaseService _database;
    private readonly HrReportsOptions _options;

    public HrAccessService(DatabaseService database, IOptions<HrReportsOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public bool HasFullAccess(string? username)
    {
        var user = (username ?? "").Trim();
        if (user.Length == 0) return false;
        return _options.FullAccessUsers.Any(u =>
            string.Equals(u?.Trim(), user, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<HrAccessDto> ResolveAsync(string? username)
    {
        var user = (username ?? "").Trim();
        if (user.Length == 0)
        {
            return new HrAccessDto
            {
                Mode = "none",
                Message = "Login required for HR Reports.",
            };
        }

        var full = HasFullAccess(user);
        string? empCode = null;
        string? fullName = null;

        try
        {
            using var connection = _database.CreateConnection();
            var row = await connection.QueryFirstOrDefaultAsync<(string? EmpCode, string? FullName)>(@"
SELECT
    NULLIF(LTRIM(RTRIM(EmpCode)), '') AS EmpCode,
    NULLIF(LTRIM(RTRIM(ISNULL(FullName, Name))), '') AS FullName
FROM loginentry.dbo.LoginRights WITH (NOLOCK)
WHERE LTRIM(RTRIM(Name)) = @Username",
                new { Username = user },
                commandTimeout: 30);
            empCode = row.EmpCode;
            fullName = row.FullName;
        }
        catch
        {
            // Fall through — full-access users can still browse without EmpCode.
        }

        if (string.IsNullOrWhiteSpace(empCode))
        {
            try
            {
                using var payroll = _database.CreatePayrollLoginEntryConnection();
                var row = await payroll.QueryFirstOrDefaultAsync<(string? EmpCode, string? FullName)>(@"
SELECT TOP 1
    NULLIF(LTRIM(RTRIM(EmpCode)), '') AS EmpCode,
    NULLIF(LTRIM(RTRIM(ISNULL(FullName, Name))), '') AS FullName
FROM LoginRights WITH (NOLOCK)
WHERE LTRIM(RTRIM(Name)) = @Username",
                    new { Username = user },
                    commandTimeout: 30);
                if (!string.IsNullOrWhiteSpace(row.EmpCode))
                    empCode = row.EmpCode;
                if (string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(row.FullName))
                    fullName = row.FullName;
            }
            catch
            {
                // ignore
            }
        }

        if (string.IsNullOrWhiteSpace(empCode))
        {
            try
            {
                using var payroll = _database.CreatePayrollLoginEntryConnection();
                empCode = await payroll.ExecuteScalarAsync<string?>(@"
SELECT TOP 1 LTRIM(RTRIM(EmpCode))
FROM empinfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(EmpCode)), '') <> ''
  AND (
        (@FullName <> '' AND LOWER(LTRIM(RTRIM(Name))) = LOWER(@FullName))
     OR LOWER(LTRIM(RTRIM(Name))) = LOWER(@Username)
  )
ORDER BY CASE WHEN ISNULL(IsHOEmp,0)=1 THEN 0 ELSE 1 END, EmpCode",
                    new { Username = user, FullName = fullName ?? "" },
                    commandTimeout: 30);
            }
            catch
            {
                // ignore
            }
        }

        if (full)
        {
            return new HrAccessDto
            {
                Username = user,
                EmpCode = empCode,
                FullName = fullName,
                HasFullAccess = true,
                CanUseSelfService = !string.IsNullOrWhiteSpace(empCode),
                IsViewOnly = false,
                Mode = "full",
                Message = "Full HR access — all employees, approve leave/WFH, PL/CL credit, month-end approvals. Cannot apply leave on behalf of employees.",
            };
        }

        if (string.IsNullOrWhiteSpace(empCode))
        {
            return new HrAccessDto
            {
                Username = user,
                FullName = fullName,
                HasFullAccess = false,
                CanUseSelfService = false,
                IsViewOnly = true,
                Mode = "none",
                Message =
                    "No EmpCode linked to your portal login. Ask HR (grouphr / plastenehr / prakash) to set LoginRights.EmpCode.",
            };
        }

        return new HrAccessDto
        {
            Username = user,
            EmpCode = empCode,
            FullName = fullName,
            HasFullAccess = false,
            CanUseSelfService = true,
            IsViewOnly = false,
            Mode = "self",
            Message =
                "Employee portal — apply leave/WFH for yourself; view attendance. HR approves requests. You cannot credit PL/CL.",
        };
    }

    public async Task EnsureCanApplyOwnLeaveAsync(string? username, string? targetEmpCode)
    {
        var access = await ResolveAsync(username);
        if (access.Mode == "none")
            throw new UnauthorizedAccessException(access.Message);

        var target = (targetEmpCode ?? "").Trim();
        if (target.Length == 0)
            throw new UnauthorizedAccessException("empCode is required.");

        if (string.IsNullOrWhiteSpace(access.EmpCode))
            throw new UnauthorizedAccessException(
                "Your login has no EmpCode. Ask HR to link LoginRights.EmpCode so you can apply leave for yourself.");

        if (!string.Equals(access.EmpCode, target, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                "You can only apply leave / WFH for yourself. HR cannot apply leave on behalf of employees — employees apply, HR approves.");
    }

    public async Task<HrAccessDto> EnsureCanAccessEmployeeAsync(
        string? username,
        string? targetEmpCode,
        bool requireFullAccess = false)
    {
        var access = await ResolveAsync(username);
        if (access.Mode == "none")
            throw new UnauthorizedAccessException(access.Message);

        if (requireFullAccess && !access.HasFullAccess)
            throw new UnauthorizedAccessException("Only HR (grouphr / plastenehr / prakash) can perform this action.");

        if (access.HasFullAccess)
            return access;

        var target = (targetEmpCode ?? "").Trim();
        if (target.Length == 0)
            throw new UnauthorizedAccessException("empCode is required.");

        if (!string.Equals(access.EmpCode, target, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("You can only access your own attendance record.");

        return access;
    }
}
