using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DatabaseService _database;

    public DashboardController(DatabaseService database)
    {
        _database = database;
    }

    [HttpGet("stats/{username}")]
    public async Task<IActionResult> GetDashboard(string username)
    {
        using var connection = _database.CreateConnection();

        var pending = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM ApprovePO
              WHERE ApprovalName = @username
                AND Status = 'Pending'",
            new { username });

        var approved = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM ApprovePO
              WHERE ApprovalName = @username
                AND Status LIKE 'Approved%'",
            new { username });

        var rejected = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM ApprovePO
              WHERE ApprovalName = @username
                AND Status LIKE 'Rejected%'",
            new { username });

        return Ok(new
        {
            pending,
            approved,
            rejected
        });
    }
}