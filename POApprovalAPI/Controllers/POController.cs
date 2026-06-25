using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POController : ControllerBase
{
private readonly DatabaseService _database;


public POController(DatabaseService database)
{
    _database = database;
}

[HttpGet("pending/{username}")]
public async Task<IActionResult> GetPending(string username)
{
    using var connection = _database.CreateConnection();

  var data = await connection.QueryAsync(
    @"SELECT
        a.PoNo,
        a.ApprovalName,
        a.Status,
        a.PODate,
        a.ApprovalDate,
        a.TransId,
       MAX(ISNULL(p.TotalAmount,0)) AS Total,
MAX(v.FirmName) AS FirmName
FROM ApprovePO a
LEFT JOIN Vw_PurchaseOrder v
    ON a.PoNo = v.PurchaseCode
LEFT JOIN PurchasePayment p
    ON a.PoNo = p.PurchaseCode
      WHERE a.ApprovalName = @username
        AND a.Status = 'Pending'
      GROUP BY
        a.PoNo,
        a.ApprovalName,
        a.Status,
        a.PODate,
        a.ApprovalDate,
        a.TransId
      ORDER BY a.PODate DESC",
    new { username });

    return Ok(data);
}

[HttpGet("details")]
public async Task<IActionResult> GetDetails([FromQuery] string poNo)
{
    using var connection = _database.CreateConnection();

   var data = await connection.QueryAsync(
    @"SELECT
        v.PurchaseCode,
        v.FirmName,
        v.ItemDesc,
        v.Qty,
        v.Rate,
        v.Total,
        v.DepttName,
        v.deliverydate,
        p.TotalAmount
      FROM Vw_PurchaseOrder v
      LEFT JOIN PurchasePayment p
        ON v.PurchaseCode = p.PurchaseCode
      WHERE v.PurchaseCode = @poNo",
    new { poNo });

    return Ok(data);
}

[HttpGet("approval")]
public async Task<IActionResult> GetApproval(
    [FromQuery] string poNo,
    [FromQuery] string username)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryFirstOrDefaultAsync(
        @"SELECT TOP 1 *
          FROM ApprovePO
          WHERE PoNo = @poNo
            AND ApprovalName = @username",
        new { poNo, username });

    return Ok(data);
}

[HttpGet("workflow")]
public async Task<IActionResult> GetWorkflow([FromQuery] string poNo)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT
            ApprovalName,
            Status,
            ApprovalDate,
            TransId
          FROM ApprovePO
          WHERE PoNo = @poNo
          ORDER BY TransId",
        new { poNo });

    return Ok(data);
}

[HttpPost("approve/{transId}")]
public async Task<IActionResult> Approve(int transId)
{
    using var connection = _database.CreateConnection();

    var po = await connection.QueryFirstOrDefaultAsync(
        @"SELECT PoNo, ApprovalName
          FROM ApprovePO
          WHERE TransId = @transId",
        new { transId });

    if (po == null)
        return NotFound();

    // Get authority of current approver
    var authority = await connection.QueryFirstOrDefaultAsync<int>(
        @"SELECT authority
          FROM poallocation
          WHERE username = @ApprovalName",
        new { ApprovalName = po.ApprovalName });

    // Final Authority (authority = 1)
    if (authority == 1)
    {
        // Update all OTHER pending approvers
        await connection.ExecuteAsync(
            @"UPDATE ApprovePO
              SET Status = @Status
              WHERE PoNo = @PoNo
                AND ApprovalName <> @ApprovalName
                AND Status = 'Pending'",
            new
            {
                PoNo = po.PoNo,
                ApprovalName = po.ApprovalName,
                Status = $"Approved by {po.ApprovalName}"
            });

        // Update final approver's own row
        await connection.ExecuteAsync(
            @"UPDATE ApprovePO
              SET Status = 'Approved',
                  ApprovalDate = GETDATE()
              WHERE PoNo = @PoNo
                AND ApprovalName = @ApprovalName",
            new
            {
                PoNo = po.PoNo,
                ApprovalName = po.ApprovalName
            });

        // Update PoSignal to '*' in PurchasePayment for final approval
        await connection.ExecuteAsync(
            @"UPDATE PurchasePayment
              SET PoSignal = '*'
              WHERE PurchaseCode = @PoNo",
            new { PoNo = po.PoNo });

        return Ok(new { success = true });
    }

    // Non-final authority
    await connection.ExecuteAsync(
        @"UPDATE ApprovePO
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE TransId = @transId",
        new
        {
            transId
        });

    // Update PoSignal to '#' in PurchasePayment for intermediate approval
    await connection.ExecuteAsync(
        @"UPDATE PurchasePayment
          SET PoSignal = '#'
          WHERE PurchaseCode = @PoNo",
        new { PoNo = po.PoNo });

    return Ok(new { success = true });
}


[HttpPost("reject/{transId}")]
public async Task<IActionResult> Reject(int transId)
{
    using var connection = _database.CreateConnection();

    await connection.ExecuteAsync(
        @"UPDATE ApprovePO
          SET Status = 'Rejected',
              ApprovalDate = GETDATE()
          WHERE TransId = @transId",
        new { transId });

    return Ok(new { success = true });
}
[HttpGet("history/{username}")]
public async Task<IActionResult> GetHistory(string username)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT
            PoNo,
            ApprovalName,
            Status,
            ApprovalDate,
            TransId
          FROM ApprovePO
          WHERE ApprovalName = @username
            AND (
                Status LIKE 'Approved%'
                OR Status LIKE 'Rejected%'
            )
          ORDER BY ApprovalDate DESC",
        new { username });

   return Ok(data);
}
}