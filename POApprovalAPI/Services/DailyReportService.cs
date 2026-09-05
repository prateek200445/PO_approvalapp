using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class DailyReportService
{
    private readonly DatabaseService _database;

    public DailyReportService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IEnumerable<DailyReportEntity>> GetTodaysReports()
    {
        using var connection = _database.CreateLoginEntryConnection();

        const string sql = @"
SELECT
    employee_name AS EmployeeName,
    submitted_on AS SubmittedOn,
    submitted_for_date AS SubmittedForDate,
    content AS HtmlContent
FROM DailyReportLogs
WHERE CAST(submitted_on AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY submitted_on DESC";

        return await connection.QueryAsync<DailyReportEntity>(sql);
    }

    public async Task<IReadOnlyList<string>> GetPeopleAsync(int? year = null, int? month = null)
    {
        using var connection = _database.CreateLoginEntryConnection();
        var sql = @"
SELECT DISTINCT LTRIM(RTRIM(employee_name))
FROM DailyReportLogs WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(employee_name)), N'') <> N''";
        object args;
        if (year is >= 2000 and <= 2100 && month is >= 1 and <= 12)
        {
            sql += @"
  AND YEAR(COALESCE(submitted_for_date, submitted_on)) = @Year
  AND MONTH(COALESCE(submitted_for_date, submitted_on)) = @Month";
            args = new { Year = year, Month = month };
        }
        else
        {
            args = new { };
        }

        sql += " ORDER BY 1";
        var rows = await connection.QueryAsync<string>(sql, args, commandTimeout: 60);
        return rows.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
    }

    public async Task<IReadOnlyList<DailyReportMonthDto>> GetMonthsAsync()
    {
        using var connection = _database.CreateLoginEntryConnection();
        var rows = (await connection.QueryAsync<DailyReportMonthDto>(@"
SELECT
    YEAR(COALESCE(submitted_for_date, submitted_on)) AS Year,
    MONTH(COALESCE(submitted_for_date, submitted_on)) AS Month,
    COUNT(*) AS ReportCount
FROM DailyReportLogs WITH (NOLOCK)
WHERE COALESCE(submitted_for_date, submitted_on) IS NOT NULL
GROUP BY YEAR(COALESCE(submitted_for_date, submitted_on)),
         MONTH(COALESCE(submitted_for_date, submitted_on))
ORDER BY Year DESC, Month DESC", commandTimeout: 60)).ToList();

        foreach (var row in rows)
        {
            row.Value = $"{row.Year:D4}-{row.Month:D2}";
            row.Label = new DateTime(row.Year, row.Month, 1).ToString("MMMM yyyy");
        }

        return rows;
    }

    public async Task<IEnumerable<DailyReportEntity>> GetReportsAsync(int year, int month, string employeeName)
    {
        using var connection = _database.CreateLoginEntryConnection();
        const string sql = @"
SELECT
    employee_name AS EmployeeName,
    submitted_on AS SubmittedOn,
    submitted_for_date AS SubmittedForDate,
    content AS HtmlContent
FROM DailyReportLogs WITH (NOLOCK)
WHERE YEAR(COALESCE(submitted_for_date, submitted_on)) = @Year
  AND MONTH(COALESCE(submitted_for_date, submitted_on)) = @Month
  AND LTRIM(RTRIM(employee_name)) = @EmployeeName
ORDER BY COALESCE(submitted_for_date, submitted_on) DESC, submitted_on DESC";

        return await connection.QueryAsync<DailyReportEntity>(
            sql,
            new { Year = year, Month = month, EmployeeName = employeeName.Trim() });
    }
}

public class DailyReportMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int ReportCount { get; set; }
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}
