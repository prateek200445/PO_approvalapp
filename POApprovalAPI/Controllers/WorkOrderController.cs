using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrderController : ControllerBase
{
    private readonly DatabaseService _database;

    public WorkOrderController(DatabaseService database)
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
                MAX(ISNULL(j.TotalAmount,0)) AS Total
              FROM ApproveWorkOrder a
             LEFT JOIN PurchasePayment j
    ON a.PoNo = j.PurchaseCode
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
          FROM ApproveWorkOrder
          WHERE PoNo = @poNo
          ORDER BY TransId",
        new { poNo });

    return Ok(data);
}
[HttpGet("details")]
public async Task<IActionResult> GetDetails([FromQuery] string poNo)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT TOP 1
            PurchaseCode,
            FirmName,
            ItemDesc,
            Qty,
            Rate,
            Total,
            TotalAmount,
            DepttName,
            PoSignal,
            deliverydate,
            CompanyName,
            GST,
            VendorGST,
            hsncode,
            CGSTPer,
            CGSTAmount,
            SGSTPer,
            SGSTAmount,
            IGSTPer,
            IGSTAmount
          FROM Vw_PurchaseOrder
          WHERE PurchaseCode = @poNo",
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
          FROM ApproveWorkOrder
          WHERE PoNo = @poNo
            AND ApprovalName = @username",
        new { poNo, username });

    return Ok(data);
}
[HttpPost("reject/{transId}")]
public async Task<IActionResult> Reject(int transId)
{
    using var connection = _database.CreateConnection();

    await connection.ExecuteAsync(
        @"UPDATE ApproveWorkOrder
          SET Status = 'Rejected',
              ApprovalDate = GETDATE()
          WHERE TransId = @transId",
        new { transId });

    return Ok(new { success = true });
}
[HttpPost("approve/{transId}")]
public async Task<IActionResult> Approve(int transId)
{
    using var connection = _database.CreateConnection();

    var wo = await connection.QueryFirstOrDefaultAsync(
        @"SELECT PoNo, ApprovalName
          FROM ApproveWorkOrder
          WHERE TransId = @transId",
        new { transId });

    if (wo == null)
        return NotFound();

    // Get authority of current approver
    var authority = await connection.QueryFirstOrDefaultAsync<int>(
        @"SELECT authority
          FROM poallocation
          WHERE username = @ApprovalName",
        new { ApprovalName = wo.ApprovalName });
     
      Console.WriteLine("=================================");
Console.WriteLine($"PONO = {wo.PoNo}");
Console.WriteLine($"USER = {wo.ApprovalName}");
Console.WriteLine($"AUTHORITY = {authority}");
Console.WriteLine("=================================");  

    // Final Authority (authority = 1)
    if (authority == 1)
    {
      Console.WriteLine("FINAL AUTHORITY BLOCK EXECUTED");   
    await connection.ExecuteAsync(
    @"UPDATE ApproveWorkOrder
      SET Status = @Status
      WHERE PoNo = @PoNo
        AND ApprovalName <> @ApprovalName
        AND Status = 'Pending'",
    new
    {
        PoNo = wo.PoNo,
        ApprovalName = wo.ApprovalName,
        Status = $"Approved by {wo.ApprovalName}"
    });
           

        await connection.ExecuteAsync(
            @"UPDATE ApproveWorkOrder
              SET Status = 'Approved',
                  ApprovalDate = GETDATE()
              WHERE PoNo = @PoNo
                AND ApprovalName = @ApprovalName",
            new
            {
                PoNo = wo.PoNo,
                ApprovalName = wo.ApprovalName
            });

        // Final approval signal
        await connection.ExecuteAsync(
    @"UPDATE PurchasePayment
      SET PoSignal = '*'
      WHERE PurchaseCode = @PoNo",
    new { PoNo = wo.PoNo });

        return Ok(new { success = true });
    }

    // Intermediate authority
    await connection.ExecuteAsync(
        @"UPDATE ApproveWorkOrder
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE TransId = @transId",
        new { transId });

   await connection.ExecuteAsync(
    @"UPDATE PurchasePayment
      SET PoSignal = '#'
      WHERE PurchaseCode = @PoNo",
    new { PoNo = wo.PoNo });
    return Ok(new { success = true });
}
}