using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/gst-bill-reco")]
public class GstBillRecoController : ControllerBase
{
    private readonly GstBillRecoService _service;
    private readonly IConfiguration _configuration;

    public GstBillRecoController(GstBillRecoService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies([FromQuery] string username, CancellationToken ct)
    {
        if (!IsAllowed(username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            return Ok(await _service.GetCompaniesAsync(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("run")]
    [RequestSizeLimit(40_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 40_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Run(
        IFormFile file,
        [FromForm] string companyName,
        [FromForm] DateTime dateFrom,
        [FromForm] DateTime dateTo,
        [FromForm] decimal amountTolerance,
        [FromForm] string username,
        CancellationToken ct)
    {
        if (!IsAllowed(username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Upload the monthly 2B Excel file." });
            var ext = Path.GetExtension(file.FileName);
            if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "The 2B file must be an .xlsx workbook." });

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            var result = await _service.ReconcileAsync(ms, companyName, dateFrom, dateTo, amountTolerance, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("export")]
    public IActionResult Export([FromBody] GstBillRecoExportRequest request)
    {
        if (!IsAllowed(request.Username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            var bytes = _service.Export(request.Rows ?? []);
            var name = $"gst-bill-reco-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsAllowed(string? username)
    {
        var allowed = _configuration.GetSection("GstBillReco:AllowedUsers").Get<string[]>() ?? [];
        var name = (username ?? "").Trim();
        return name.Length > 0 && allowed.Any(user => string.Equals(user?.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }
}
