using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;
using POApprovalAPI.Models;
using System.Text.Json;
namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POController : ControllerBase
{
private readonly DatabaseService _database;
private readonly EmailService _emailService;
private readonly PoApprovalService _poApproval;

public POController(
    DatabaseService database,
    EmailService emailService,
    PoApprovalService poApproval)
{
    _database = database;
    _emailService = emailService;
    _poApproval = poApproval;
}

[HttpGet("pending/{username}")]
public async Task<IActionResult> GetPending(
    string username,
    [FromQuery] decimal? amount,
    [FromQuery] string? filterType)
{
    using var connection = _database.CreateConnection();

    var sql = @"
SELECT
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
  AND (@amount IS NULL OR
      (
          @filterType = 'gt' AND p.TotalAmount > @amount
          OR @filterType = 'lt' AND p.TotalAmount < @amount
          OR @filterType = 'eq' AND p.TotalAmount = @amount
          OR @filterType = 'gte' AND p.TotalAmount >= @amount
          OR @filterType = 'lte' AND p.TotalAmount <= @amount
      ))
GROUP BY
    a.PoNo,
    a.ApprovalName,
    a.Status,
    a.PODate,
    a.ApprovalDate,
    a.TransId

UNION ALL

SELECT
    a.PoNo,
    a.ApprovalName,
    a.Status,
    a.PODate,
    a.ApprovalDate,
    a.TransId,
    MAX(ISNULL(p.TotalAmount,0)) AS Total,
    MAX(v.FirmName) AS FirmName
FROM ApprovePOHOD a
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

ORDER BY PODate DESC;";

    var data = await connection.QueryAsync(
    sql,
    new
    {
        username,
        amount,
        filterType
    });

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
        v.Currency,
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
    @"
    SELECT TOP 1 *
    FROM
    (
        SELECT *
        FROM ApprovePO
        WHERE PoNo = @poNo
          AND ApprovalName = @username

        UNION ALL

        SELECT *
        FROM ApprovePOHOD
        WHERE PoNo = @poNo
          AND ApprovalName = @username
    ) x",
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

    var result = await _poApproval.ApproveOneAsync(transId, remarks);

    if (!result.Success &&
        string.Equals(result.Reason, "PO approval row not found", StringComparison.OrdinalIgnoreCase))
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

    if (request.TransIds.Count > PoApprovalService.MaxBulkSize)
    {
        return BadRequest(new
        {
            message = $"Maximum {PoApprovalService.MaxBulkSize} POs allowed per bulk approve"
        });
    }

    var result = await _poApproval.ApproveBulkAsync(request);
    return Ok(result);
}


[HttpPost("reject/{transId}")]
[Consumes("application/json", "multipart/form-data")]
public async Task<IActionResult> Reject(int transId)
{
    var remarks = await RejectRequestHelper.ReadRemarksAsync(Request);
    var attachmentFile = RejectRequestHelper.GetOptionalAttachment(Request);
    var (attachment, attachError) = await RejectRequestHelper.ReadOptionalAttachmentAsync(attachmentFile);
    if (attachError != null)
        return BadRequest(new { error = attachError });

    using var connection = _database.CreateConnection();

    string table = "ApprovePO";

var po = await connection.QueryFirstOrDefaultAsync(
    @"SELECT PoNo, ApprovalName
      FROM ApprovePO
      WHERE TransId = @transId",
    new { transId });

if (po == null)
{
    po = await connection.QueryFirstOrDefaultAsync(
        @"SELECT PoNo, ApprovalName
          FROM ApprovePOHOD
          WHERE TransId = @transId",
        new { transId });

    if (po != null)
        table = "ApprovePOHOD";
}

if (po == null)
    return NotFound();
 
   var email = await connection.QueryFirstOrDefaultAsync<string>(
    @"SELECT lr.email
      FROM PurchasePayment pp
      INNER JOIN loginentry..loginrights lr
          ON lr.NAME = pp.LOGINNAME
      WHERE pp.PurchaseCode = @PoNo",
    new { PoNo = po.PoNo }); 

   await connection.ExecuteAsync(
    $@"UPDATE {table}
       SET Status = 'Rejected',
           ApprovalDate = GETDATE()
       WHERE TransId = @transId",
    new { transId });

    if (!string.IsNullOrWhiteSpace(email))
{
    await _emailService.SendMail(
        email,
        $"PO {po.PoNo} Rejected",
        $"Dear Sir,\n\n" +
        $"PO Number: {po.PoNo}\n" +
        $"Rejected By: {po.ApprovalName}\n" +
        $"Remarks: {remarks}\n\n" +
        $"Regards,\n" +
        $"{po.ApprovalName}",
        attachment != null ? [attachment] : null
    );
}

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