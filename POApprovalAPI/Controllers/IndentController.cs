using POApprovalAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndentController : ControllerBase
{
    private static readonly TimeSpan AssociatedPoCacheTtl = TimeSpan.FromMinutes(20);

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public IndentController(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
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

        var key = indentNo.Trim();
        var cacheKey = $"indent-associated-pos:{key}";
        if (_cache.TryGetValue(cacheKey, out object? cached) && cached is not null)
            return Ok(cached);

        using var connection = _database.CreateConnection();

        // 1) Quotation bridge first — usually enough and much cheaper than PO view.
        var codes = (await connection.QueryAsync<string>(
            @"
SELECT PurchaseCode
FROM dbo.FinalQuotation WITH (NOLOCK)
WHERE StoreCode = @indentNo
  AND PurchaseCode IS NOT NULL
  AND PurchaseCode <> N''
UNION
SELECT PurchaseCode
FROM dbo.Vw_Quotation WITH (NOLOCK)
WHERE StoreCode = @indentNo
  AND PurchaseCode IS NOT NULL
  AND PurchaseCode <> N''",
            new { indentNo = key },
            commandTimeout: 30)).ToList();

        // 2) Fallback only when quotes have no link (avoid scanning Vw_PurchaseOrder unless needed).
        if (codes.Count == 0)
        {
            codes = (await connection.QueryAsync<string>(
                @"
SELECT DISTINCT PurchaseCode
FROM dbo.Vw_PurchaseOrder WITH (NOLOCK)
WHERE RefNo = @indentNo
   OR StoreCode = @indentNo",
                new { indentNo = key },
                commandTimeout: 45)).ToList();
        }

        codes = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IEnumerable<dynamic> data;
        if (codes.Count == 0)
        {
            data = Array.Empty<dynamic>();
        }
        else
        {
            // Enrich from PurchasePayment by exact PurchaseCode (no view join).
            data = await connection.QueryAsync(
                @"
SELECT
    p.PurchaseCode AS PoNo,
    MAX(NULLIF(LTRIM(RTRIM(q.FirmName)), N'')) AS FirmName,
    MAX(CAST(p.TotalAmount AS float)) AS TotalAmount,
    MAX(NULLIF(LTRIM(RTRIM(p.Currency)), N'')) AS Currency
FROM dbo.PurchasePayment p WITH (NOLOCK)
LEFT JOIN dbo.Vw_Quotation q WITH (NOLOCK)
  ON q.PurchaseCode = p.PurchaseCode
 AND q.StoreCode = @indentNo
WHERE p.PurchaseCode IN @PurchaseCodes
GROUP BY p.PurchaseCode
ORDER BY p.PurchaseCode DESC",
                new { indentNo = key, PurchaseCodes = codes },
                commandTimeout: 30);

            // Include any codes that exist only on quotation/PO view but not yet in PurchasePayment.
            var found = new HashSet<string>(
                data.Select(r => (string)r.PoNo),
                StringComparer.OrdinalIgnoreCase);
            var missing = codes.Where(c => !found.Contains(c)).ToList();
            if (missing.Count > 0)
            {
                var extras = await connection.QueryAsync(
                    @"
SELECT
    q.PurchaseCode AS PoNo,
    MAX(NULLIF(LTRIM(RTRIM(q.FirmName)), N'')) AS FirmName,
    CAST(NULL AS float) AS TotalAmount,
    CAST(NULL AS nvarchar(20)) AS Currency
FROM dbo.Vw_Quotation q WITH (NOLOCK)
WHERE q.StoreCode = @indentNo
  AND q.PurchaseCode IN @PurchaseCodes
GROUP BY q.PurchaseCode
ORDER BY q.PurchaseCode DESC",
                    new { indentNo = key, PurchaseCodes = missing },
                    commandTimeout: 30);
                data = data.Concat(extras).OrderByDescending(r => (string)r.PoNo);
            }
        }

        var payload = new
        {
            indentNo = key,
            purchaseOrders = data,
            note = "Fast path: FinalQuotation/Vw_Quotation by StoreCode, then PurchasePayment enrich; Vw_PurchaseOrder only if needed (cached).",
        };
        _cache.Set(cacheKey, payload, AssociatedPoCacheTtl);
        return Ok(payload);
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
