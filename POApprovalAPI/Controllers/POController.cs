using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;
using POApprovalAPI.Models;
using System.Text.Json;
using System.Linq;
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
    MAX(v.FirmName) AS FirmName,
    MAX(NULLIF(LTRIM(RTRIM(v.CompanyName)), N'')) AS CompanyName
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
    MAX(v.FirmName) AS FirmName,
    MAX(NULLIF(LTRIM(RTRIM(v.CompanyName)), N'')) AS CompanyName
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
public async Task<IActionResult> GetDetails(
    [FromQuery] string poNo,
    [FromQuery] bool includePreviousQuotes = true)
{
    using var connection = _database.CreateConnection();

    var items = (await connection.QueryAsync(
        @"SELECT
            v.PurchaseCode,
            v.FirmName,
            v.ItemCode,
            v.ItemDesc,
            v.Qty,
            v.Rate,
            v.Total,
            v.DepttName,
            v.deliverydate,
            v.Currency,
            p.TotalAmount,
            COALESCE(
                NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), v.RefNo))), N''),
                NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), v.StoreCode))), N''),
                NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), fq.StoreCode))), N''),
                NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), q.StoreCode))), N'')
            ) AS IndentNo,
            v.IndentDate,
            NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), v.StoreCode))), N'') AS StoreCode,
            NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), v.SubCode))), N'') AS IndentSubCode,
            NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), v.Remarks))), N'') AS IndentRemarks,
            NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), v.Purpose))), N'') AS IndentPurpose
          FROM Vw_PurchaseOrder v
          LEFT JOIN PurchasePayment p
            ON v.PurchaseCode = p.PurchaseCode
          OUTER APPLY (
            SELECT TOP 1 fq.StoreCode
            FROM FinalQuotation fq WITH (NOLOCK)
            WHERE fq.PurchaseCode = v.PurchaseCode
              AND fq.StoreCode IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), fq.StoreCode))) <> N''
          ) fq
          OUTER APPLY (
            SELECT TOP 1 q.StoreCode
            FROM Vw_Quotation q WITH (NOLOCK)
            WHERE q.PurchaseCode = v.PurchaseCode
              AND q.StoreCode IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), q.StoreCode))) <> N''
          ) q
          WHERE v.PurchaseCode = @poNo",
        new { poNo })).ToList();

    string? indentNo = null;
    string? indentSubCode = null;
    string? indentRemarks = null;
    string? indentPurpose = null;
    DateTime? indentDate = null;
    foreach (var item in items)
    {
        indentNo ??= NullIfWhiteSpace(Convert.ToString(item.IndentNo));
        indentSubCode ??= NullIfWhiteSpace(Convert.ToString(item.IndentSubCode));
        indentRemarks ??= NullIfWhiteSpace(Convert.ToString(item.IndentRemarks));
        indentPurpose ??= NullIfWhiteSpace(Convert.ToString(item.IndentPurpose));
        if (indentDate == null && item.IndentDate != null)
            indentDate = item.IndentDate as DateTime?;
    }

    // Prefer indent-master remarks when the PO view leaves them blank.
    if (!string.IsNullOrWhiteSpace(indentNo) && string.IsNullOrWhiteSpace(indentRemarks))
    {
        try
        {
            var indentMeta = await connection.QueryFirstOrDefaultAsync(
                @"SELECT TOP 1
                    NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ii.Remarks))), N'') AS Remarks,
                    NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), sd.Purpose))), N'') AS Purpose
                  FROM ItemInfo ii WITH (NOLOCK)
                  LEFT JOIN vw_storedeptt sd WITH (NOLOCK)
                    ON sd.code = ii.code
                  WHERE (@indentSubCode IS NOT NULL AND ii.code = @indentSubCode)
                     OR ii.code LIKE @indentPrefix
                     OR sd.Expr1 = @indentNo
                     OR ii.RefNo = @indentNo
                  ORDER BY
                    CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ii.Remarks))), N'') IS NULL THEN 1 ELSE 0 END",
                new
                {
                    indentNo,
                    indentSubCode,
                    indentPrefix = indentNo + "/%",
                });

            if (indentMeta != null)
            {
                indentRemarks ??= NullIfWhiteSpace(Convert.ToString(indentMeta.Remarks));
                // Purpose on storedeptt is often just company short-code; only use when no remarks.
                indentPurpose ??= NullIfWhiteSpace(Convert.ToString(indentMeta.Purpose));
            }
        }
        catch
        {
            // Indent enrichment is optional; PO lines still return.
        }
    }

    IEnumerable<dynamic> previousQuotes = Array.Empty<dynamic>();
    if (includePreviousQuotes)
        previousQuotes = await LoadPreviousQuotesAsync(connection, poNo, items);

    return Ok(new
    {
        items,
        previousQuotes,
        indent = new
        {
            indentNo,
            indentDate,
            indentRemarks,
            indentPurpose,
        },
    });
}

[HttpGet("previous-quotes")]
public async Task<IActionResult> GetPreviousQuotes([FromQuery] string poNo)
{
    using var connection = _database.CreateConnection();
    var items = (await connection.QueryAsync(
        @"SELECT DISTINCT LTRIM(RTRIM(v.ItemCode)) AS ItemCode
          FROM Vw_PurchaseOrder v WITH (NOLOCK)
          WHERE v.PurchaseCode = @poNo
            AND v.ItemCode IS NOT NULL
            AND LTRIM(RTRIM(v.ItemCode)) <> N''",
        new { poNo })).ToList();

    var previousQuotes = await LoadPreviousQuotesAsync(connection, poNo, items);
    return Ok(previousQuotes);
}

private static async Task<IEnumerable<dynamic>> LoadPreviousQuotesAsync(
    System.Data.IDbConnection connection,
    string poNo,
    IReadOnlyList<dynamic> items)
{
    var itemCodes = items
        .Select(i => NullIfWhiteSpace(Convert.ToString(i.ItemCode)))
        .Where(c => c != null)
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (itemCodes.Count == 0)
        return Array.Empty<dynamic>();

    // Keep this bounded: VendorRate + recent prior POs only (last 2 years), one row per vendor/item.
    return await connection.QueryAsync(
        @"SELECT
            ItemCode,
            Vendor,
            Rate,
            Qty,
            Unit,
            QuotedOn,
            Source,
            PreviousPoNo
          FROM (
            SELECT
              u.ItemCode,
              u.Vendor,
              u.Rate,
              u.Qty,
              u.Unit,
              u.QuotedOn,
              u.Source,
              u.PreviousPoNo,
              ROW_NUMBER() OVER (
                PARTITION BY u.ItemCode, u.Vendor
                ORDER BY u.QuotedOn DESC
              ) AS rn
            FROM (
              SELECT * FROM (
                SELECT TOP 100
                  vr.ItemCode,
                  vr.FirmName AS Vendor,
                  vr.Rate AS Rate,
                  vr.Qty,
                  vr.Unit,
                  vr.Sysdate AS QuotedOn,
                  CAST('Quote' AS varchar(20)) AS Source,
                  CAST(NULL AS varchar(50)) AS PreviousPoNo
                FROM VendorRate vr WITH (NOLOCK)
                WHERE vr.ItemCode IN @itemCodes
                  AND vr.Rate IS NOT NULL
                  AND vr.Rate <> 0
                ORDER BY vr.Sysdate DESC
              ) quotes

              UNION ALL

              SELECT * FROM (
                SELECT TOP 100
                  po.ItemCode,
                  po.FirmName AS Vendor,
                  po.Rate AS Rate,
                  po.Qty,
                  po.Unit,
                  po.deliverydate AS QuotedOn,
                  CAST('Previous PO' AS varchar(20)) AS Source,
                  po.PurchaseCode AS PreviousPoNo
                FROM Vw_PurchaseOrder po WITH (NOLOCK)
                WHERE po.ItemCode IN @itemCodes
                  AND po.PurchaseCode <> @poNo
                  AND po.Rate IS NOT NULL
                  AND po.Rate <> 0
                  AND po.deliverydate >= DATEADD(year, -2, GETDATE())
                ORDER BY po.deliverydate DESC
              ) priorPos
            ) u
          ) q
          WHERE q.rn = 1
          ORDER BY QuotedOn DESC",
        new { itemCodes, poNo });
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

private static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}