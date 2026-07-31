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

        var sql = @"

SELECT
    employee_name AS EmployeeName,
    submitted_on AS SubmittedOn,
    submitted_for_date AS SubmittedForDate,
    content AS HtmlContent
FROM DailyReportLogs
WHERE CAST(submitted_on AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY submitted_on DESC;
";

        return await connection.QueryAsync<DailyReportEntity>(sql);
    }
}