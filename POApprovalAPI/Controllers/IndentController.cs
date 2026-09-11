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
public async Task<IActionResult> GetPending(
    string username,
    [FromQuery] decimal? amount,
    [FromQuery] string? filterType)
{
    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"SELECT
            IndentNo,
            MAX(IndentDate) AS IndentDate,
            COUNT(*) AS TotalItems,
            CAST(N'Pending' AS nvarchar(20)) AS Status
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
[HttpGet("purchase-orders")]
public async Task<IActionResult> GetAssociatedPurchaseOrders([FromQuery] string indentNo)
{
    if (string.IsNullOrWhiteSpace(indentNo))
        return BadRequest(new { message = "indentNo is required." });

    using var connection = _database.CreateConnection();

    var data = await connection.QueryAsync(
        @"
;WITH Linked AS (
    SELECT DISTINCT LTRIM(RTRIM(CONVERT(nvarchar(100), v.PurchaseCode))) AS PurchaseCode
    FROM dbo.Vw_PurchaseOrder v WITH (NOLOCK)
    WHERE LTRIM(RTRIM(CONVERT(nvarchar(100), ISNULL(v.RefNo, N'')))) = @indentNo
       OR LTRIM(RTRIM(CONVERT(nvarchar(100), ISNULL(v.StoreCode, N'')))) = @indentNo

    UNION

    SELECT DISTINCT LTRIM(RTRIM(CONVERT(nvarchar(100), fq.PurchaseCode))) AS PurchaseCode
    FROM dbo.FinalQuotation fq WITH (NOLOCK)
    WHERE LTRIM(RTRIM(CONVERT(nvarchar(100), ISNULL(fq.StoreCode, N'')))) = @indentNo
      AND ISNULL(LTRIM(RTRIM(CONVERT(nvarchar(100), fq.PurchaseCode))), N'') <> N''

    UNION

    SELECT DISTINCT LTRIM(RTRIM(CONVERT(nvarchar(100), q.PurchaseCode))) AS PurchaseCode
    FROM dbo.Vw_Quotation q WITH (NOLOCK)
    WHERE LTRIM(RTRIM(CONVERT(nvarchar(100), ISNULL(q.StoreCode, N'')))) = @indentNo
      AND ISNULL(LTRIM(RTRIM(CONVERT(nvarchar(100), q.PurchaseCode))), N'') <> N''
)
SELECT
    l.PurchaseCode AS PoNo,
    MAX(NULLIF(LTRIM(RTRIM(v.FirmName)), N'')) AS FirmName,
    MAX(CAST(p.TotalAmount AS float)) AS TotalAmount,
    MAX(NULLIF(LTRIM(RTRIM(v.Currency)), N'')) AS Currency
FROM Linked l
LEFT JOIN dbo.Vw_PurchaseOrder v WITH (NOLOCK)
  ON LTRIM(RTRIM(CONVERT(nvarchar(100), v.PurchaseCode))) = l.PurchaseCode
LEFT JOIN dbo.PurchasePayment p WITH (NOLOCK)
  ON LTRIM(RTRIM(CONVERT(nvarchar(100), p.PurchaseCode))) = l.PurchaseCode
WHERE ISNULL(l.PurchaseCode, N'') <> N''
GROUP BY l.PurchaseCode
ORDER BY l.PurchaseCode DESC",
        new { indentNo = indentNo.Trim() });

    return Ok(new
    {
        indentNo = indentNo.Trim(),
        purchaseOrders = data,
        note = "PO links via Vw_PurchaseOrder.RefNo/StoreCode, FinalQuotation.StoreCode, and Vw_Quotation.StoreCode.",
    });
}
[HttpPost("approve")]
public async Task<IActionResult> Approve(
    [FromBody] POApprovalAPI.Models.IndentApprovalRequest request)
{
    using var connection = _database.CreateConnection();

    foreach (var subCode in request.IndentSubCodes)
    {
        // Existing query (keep this as it is)
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

        // New query (add this below)
        await connection.ExecuteAsync(
            @"UPDATE ItemInfo
              SET Approved = 'Approved'
              WHERE code = @subCode",
            new
            {
                subCode
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