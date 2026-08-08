using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;
using POApprovalAPI.Models;
using System.Text.Json;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrderController : ControllerBase
{
    private readonly DatabaseService _database;
    private readonly EmailService _emailService;
    private readonly WorkOrderApprovalService _woApproval;

    public WorkOrderController(
        DatabaseService database,
        EmailService emailService,
        WorkOrderApprovalService woApproval)
    {
        _database = database;
        _emailService = emailService;
        _woApproval = woApproval;
    }

    [HttpGet("pending/{username}")]
    public async Task<IActionResult> GetPending(
        string username,
        [FromQuery] decimal? amount,
        [FromQuery] string? filterType)
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
    AND j.TotalAmount IS NOT NULL
             WHERE a.ApprovalName = @username
  AND a.Status = 'Pending'
 
              GROUP BY
                a.PoNo,
                a.ApprovalName,
                a.Status,
                a.PODate,
                a.ApprovalDate,
                a.TransId
                HAVING
(
    @amount IS NULL
    OR (
        (@filterType = 'gt'  AND MAX(ISNULL(j.TotalAmount, 0)) > @amount)
        OR (@filterType = 'lt'  AND MAX(ISNULL(j.TotalAmount, 0)) < @amount)
        OR (@filterType = 'eq'  AND MAX(ISNULL(j.TotalAmount, 0)) = @amount)
        OR (@filterType = 'gte' AND MAX(ISNULL(j.TotalAmount, 0)) >= @amount)
        OR (@filterType = 'lte' AND MAX(ISNULL(j.TotalAmount, 0)) <= @amount)
    )
)
              ORDER BY a.PODate DESC",
            new
            {
                username,
                amount,
                filterType
            });

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
            IGSTAmount,
            Currency
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
    public async Task<IActionResult> Reject(
        int transId,
        [FromBody] dynamic data)
    {
        using var connection = _database.CreateConnection();

        string remarks = "";

        if (data is JsonElement json &&
            json.TryGetProperty("remarks", out JsonElement remarksElement))
        {
            remarks = remarksElement.GetString() ?? "";
        }

        var approvalData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
            @"SELECT 
            wo.PoNo,
            wo.ApprovalName,
            lr.email AS Email,
            0 AS Authority
          FROM ApproveWorkOrder wo
          LEFT JOIN PurchasePayment pp ON pp.PurchaseCode = wo.PoNo
          LEFT JOIN loginentry..loginrights lr ON lr.NAME = pp.LOGINNAME
          WHERE wo.TransId = @transId",
            new { transId });

        if (approvalData == null)
            return NotFound();

        await connection.ExecuteAsync(
            @"UPDATE ApproveWorkOrder
          SET Status = 'Rejected',
              ApprovalDate = GETDATE()
          WHERE TransId = @transId",
            new { transId });

        if (!string.IsNullOrWhiteSpace(approvalData.Email))
        {
            await _emailService.SendMail(
                approvalData.Email,
                $"Work Order {approvalData.PoNo} Rejected",
                $"Dear Sir,\n\n" +
                $"Work Order: {approvalData.PoNo}\n" +
                $"Rejected By: {approvalData.ApprovalName}\n" +
                $"Remarks: {remarks}\n\n" +
                $"Regards,\n" +
                $"{approvalData.ApprovalName}"
            );
        }

        return Ok(new { success = true });
    }

    [HttpPost("approve/{transId}")]
    public async Task<IActionResult> Approve(
        int transId,
        [FromBody] dynamic data)
    {
        string remarks = "";

        if (data is JsonElement json &&
            json.TryGetProperty("remarks", out JsonElement remarksElement))
        {
            remarks = remarksElement.GetString() ?? "";
        }

        var result = await _woApproval.ApproveOneAsync(transId, remarks);

        if (!result.Success &&
            string.Equals(result.Reason, "Work order approval row not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(result);
        }

        if (!result.Success)
            return BadRequest(result);

        return Ok(new { success = true, poNo = result.PoNo });
    }

    [HttpPost("approve-bulk")]
    public async Task<IActionResult> ApproveBulk([FromBody] PoBulkApproveRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required" });

        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { message = "UserName is required" });

        if (request.TransIds == null || request.TransIds.Count == 0)
            return BadRequest(new { message = "At least one TransId is required" });

        if (request.TransIds.Count > WorkOrderApprovalService.MaxBulkSize)
        {
            return BadRequest(new
            {
                message = $"Maximum {WorkOrderApprovalService.MaxBulkSize} work orders allowed per bulk approve"
            });
        }

        var result = await _woApproval.ApproveBulkAsync(request);
        return Ok(result);
    }
}
