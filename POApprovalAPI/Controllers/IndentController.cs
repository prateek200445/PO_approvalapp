using POApprovalAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndentController : ControllerBase
{
    private readonly DatabaseService _database;

    public IndentController(DatabaseService database)
{
    _database = database;
}

   [HttpGet("pending/{username}")]
public async Task<IActionResult> GetPending(string username)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT
            IndentNo,
            MAX(IndentDate) AS IndentDate,
            COUNT(*) AS TotalItems
          FROM ApproveIndent
          WHERE ApprovalName = @username
            AND Status = 'Pending'
          GROUP BY IndentNo
          ORDER BY MAX(IndentDate) DESC",
        new { username });

    return Ok(data);
}
    [HttpGet("workflow")]
public async Task<IActionResult> GetWorkflow([FromQuery] string indentNo)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
       @"SELECT
    ApprovalName,
    MAX(Status) AS Status,
    MAX(ApprovalDate) AS ApprovalDate,
    MIN(TransId) AS TransId
  FROM ApproveIndent
  WHERE IndentNo = @indentNo
  GROUP BY ApprovalName
  ORDER BY MIN(TransId)",
        new { indentNo });

    return Ok(data);
}
[HttpGet("details")]
public async Task<IActionResult> GetDetails([FromQuery] string indentNo)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT
            code AS IndentSubCode,
            itemcode AS ItemCode,
            itemdesc AS ItemDesc,
            Qty AS IndentQty,
            Unit,
            Purpose,
            ReqDepartment,
            CompanyName,
            IndentSignal
          FROM vw_storedeptt
          WHERE Expr1 = @indentNo",
        new { indentNo });

    return Ok(data);
}
[HttpPost("approve")]
public async Task<IActionResult> Approve(
    [FromBody] POApprovalAPI.Models.IndentApprovalRequest request)
{
    using var connection = _database.CreateConnection();

    foreach (var subCode in request.IndentSubCodes)
    {
        await connection.ExecuteAsync(
            @"UPDATE ApproveIndent
              SET Status = 'Approved',
                  ApprovalDate = GETDATE()
              WHERE IndentSubCode = @subCode
                AND ApprovalName = @username
                AND Status = 'Pending'",
            new
            {
                subCode,
                username = request.Username
            });
    }

    return Ok(new
    {
        success = true,
        approvedItems = request.IndentSubCodes.Count
    });
}
[HttpPost("reject")]
public async Task<IActionResult> Reject(
    [FromBody] POApprovalAPI.Models.IndentApprovalRequest request)
{
    using var connection = _database.CreateConnection();

    foreach (var subCode in request.IndentSubCodes)
    {
        await connection.ExecuteAsync(
            @"UPDATE ApproveIndent
              SET Status = 'Rejected',
                  ApprovalDate = GETDATE()
              WHERE IndentSubCode = @subCode
                AND ApprovalName = @username
                AND Status = 'Pending'",
            new
            {
                subCode,
                username = request.Username
            });
    }

    return Ok(new
    {
        success = true,
        rejectedItems = request.IndentSubCodes.Count
    });
}
}