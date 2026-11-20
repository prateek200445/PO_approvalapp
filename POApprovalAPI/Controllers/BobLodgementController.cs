using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/BobLodgement")]
public class BobLodgementController : ControllerBase
{
    private readonly BobLodgementService _service;

    public BobLodgementController(BobLodgementService service) => _service = service;

    [HttpGet("companies")]
    public IActionResult Companies() => Ok(_service.ListCompanies());

    [HttpPost("parse-excel")]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> ParseExcel(IFormFile? file, CancellationToken ct = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Upload the Container Dispatch / finance Excel (.xlsx)." });
            var name = file.FileName ?? "";
            if (!name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Upload an Excel file (.xlsx)." });

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            return Ok(_service.ParseExcel(ms, name));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("draft-from-excel")]
    public IActionResult DraftFromExcel([FromBody] BobLodgementExcelDraftRequest request)
    {
        try
        {
            if (request.Row == null)
                return BadRequest(new { message = "Select a row from the uploaded Excel." });
            return Ok(_service.BuildDraftFromExcel(request.CompanyId, request.Row));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string company,
        [FromQuery] string? q = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SearchAsync(company, q, from, to, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("draft")]
    public async Task<IActionResult> Draft(
        [FromQuery] string company,
        [FromQuery] string? invoiceNo = null,
        [FromQuery] string? shippingBillNo = null,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceNo) && string.IsNullOrWhiteSpace(shippingBillNo))
                return BadRequest(new { message = "Provide invoiceNo or shippingBillNo." });
            return Ok(await _service.BuildDraftAsync(company, invoiceNo, shippingBillNo, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("pdf")]
    public IActionResult Pdf([FromBody] BobLodgementDownloadRequest request)
    {
        try
        {
            var bytes = _service.BuildPdf(request);
            var safe = string.IsNullOrWhiteSpace(request.CompanyId) ? "BOB" : request.CompanyId.Trim().ToUpperInvariant();
            return File(bytes, "application/pdf", $"BOB_Lodgement_{safe}_{DateTime.Today:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
