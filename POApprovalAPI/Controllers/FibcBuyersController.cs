using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FibcBuyersController : ControllerBase
{
    private readonly FibcBuyersService _service;

    public FibcBuyersController(FibcBuyersService service)
    {
        _service = service;
    }

    [HttpGet("credentials-status")]
    public IActionResult CredentialsStatus() => Ok(_service.GetCredentialStatus());

    [HttpGet("dashboard")]
    public IActionResult Dashboard(
        [FromQuery] string? country = null,
        [FromQuery] string? buyer = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int? minScore = null,
        [FromQuery] bool? genuineOnly = null)
    {
        var data = _service.GetDashboard(new FibcBuyerQuery
        {
            Country = country,
            Buyer = buyer,
            Keyword = keyword,
            MinScore = minScore,
            GenuineOnly = genuineOnly,
        });
        return Ok(data);
    }

    [HttpGet("buyers")]
    public IActionResult Buyers(
        [FromQuery] string? country = null,
        [FromQuery] string? buyer = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int? minScore = null,
        [FromQuery] bool? genuineOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return Ok(_service.GetBuyersPage(new FibcBuyerQuery
        {
            Country = country,
            Buyer = buyer,
            Keyword = keyword,
            MinScore = minScore,
            GenuineOnly = genuineOnly,
            Page = page,
            PageSize = pageSize,
        }));
    }

    [HttpGet("buyers/{buyerId}")]
    public IActionResult BuyerDetail(string buyerId)
    {
        var detail = _service.GetBuyerDetail(buyerId);
        if (detail == null) return NotFound(new { message = "Buyer not found." });
        return Ok(detail);
    }

    [HttpGet("history")]
    public IActionResult ImportHistory() => Ok(_service.GetImportHistory());

    [HttpGet("history/{id}")]
    public IActionResult ImportHistoryDetail(string id)
    {
        var detail = _service.GetImportHistoryDetail(id);
        if (detail == null) return NotFound(new { message = "Import history entry not found." });
        return Ok(detail);
    }

    [HttpPost("import")]
    [RequestSizeLimit(40_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 40_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Upload a CSV or Excel (.xlsx) exported from Ex-Im (ex-im.cloud Downloads)." });

        var name = file.FileName ?? "upload.csv";
        if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Upload an Ex-Im CSV or Excel (.xlsx) export." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _service.ImportFileAsync(stream, name, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sync")]
    public IActionResult Sync([FromBody] FibcSyncRequest? request)
    {
        var result = _service.SyncLimited(request ?? new FibcSyncRequest { Limit = 20 });
        return Ok(result);
    }

    [HttpPost("refresh-contacts")]
    public IActionResult RefreshContacts() => Ok(_service.RefreshBuyerContacts());

    [HttpPost("recompute")]
    public IActionResult Recompute() => Ok(_service.RecomputeAll());

    [HttpGet("export.csv")]
    public IActionResult ExportCsv(
        [FromQuery] string? country = null,
        [FromQuery] string? buyer = null,
        [FromQuery] int? minScore = null,
        [FromQuery] bool? genuineOnly = null)
    {
        var bytes = _service.ExportCsv(new FibcBuyerQuery
        {
            Country = country,
            Buyer = buyer,
            MinScore = minScore,
            GenuineOnly = genuineOnly,
        });
        return File(
            bytes,
            "text/csv; charset=utf-8",
            $"FIBC_Shipments_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }
}
