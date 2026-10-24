using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/gst-bill-reco")]
public class GstBillRecoController : ControllerBase
{
    private readonly GstBillRecoService _service;
    private readonly Gst2BSnapshotService _snapshots;
    private readonly GstPortalBrowserService _portal;
    private readonly IConfiguration _configuration;

    public GstBillRecoController(
        GstBillRecoService service,
        Gst2BSnapshotService snapshots,
        GstPortalBrowserService portal,
        IConfiguration configuration)
    {
        _service = service;
        _snapshots = snapshots;
        _portal = portal;
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

    [HttpGet("periods")]
    public async Task<IActionResult> Periods([FromQuery] string username, [FromQuery] string companyName, CancellationToken ct)
    {
        if (!IsAllowed(username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            return Ok(await _snapshots.GetPeriodsAsync(companyName, ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents(
        [FromQuery] string username,
        [FromQuery] string companyName,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool purchaseOnly = true,
        CancellationToken ct = default)
    {
        if (!IsAllowed(username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            return Ok(await _snapshots.GetDocumentsAsync(companyName, from, to, purchaseOnly, ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("run-stored")]
    public async Task<IActionResult> RunStored(
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
            return Ok(await _service.ReconcileStoredAsync(companyName, dateFrom, dateTo, amountTolerance, ct));
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

    [HttpPost("portal/start")]
    public async Task<IActionResult> StartPortal([FromBody] GstPortalStartRequest request, CancellationToken ct)
    {
        if (!IsAllowed(request.Username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            var coverage = await _snapshots.CoverageAsync(request.CompanyName, request.DateFrom, request.DateTo, ct);
            if (!coverage.NeedsDownload)
            {
                if (!coverage.HasStoredMonths)
                    return BadRequest(new { message = coverage.Message });
                var result = await _service.ReconcileStoredAsync(
                    request.CompanyName,
                    request.DateFrom,
                    request.DateTo,
                    request.AmountTolerance,
                    ct);
                if (!string.IsNullOrWhiteSpace(coverage.Message))
                    result.Warning = string.Join(" ", new[] { result.Warning, coverage.Message }.Where(value => !string.IsNullOrWhiteSpace(value)));
                return Ok(new
                {
                    status = "complete",
                    message = coverage.Message,
                    result
                });
            }

            return Ok(await _portal.StartAsync(request, ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("portal/captcha")]
    public async Task<IActionResult> SubmitPortalCaptcha(
        [FromBody] GstPortalCaptchaRequest request,
        CancellationToken ct)
    {
        if (!IsAllowed(request.Username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            var step = await _portal.SubmitCaptchaAsync(request.SessionId, request.Captcha);
            return await PortalStepResult(step, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("portal/otp")]
    public async Task<IActionResult> SubmitPortalOtp(
        [FromBody] GstPortalOtpRequest request,
        CancellationToken ct)
    {
        if (!IsAllowed(request.Username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            var step = await _portal.SubmitOtpAsync(request.SessionId, request.Otp);
            return await PortalStepResult(step, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("portal/diagnostic")]
    public async Task<IActionResult> DiagnosePortal(
        [FromQuery] string sessionId,
        [FromQuery] string username)
    {
        if (!IsAllowed(username))
            return StatusCode(403, new { message = "GST Bill Reconciliation is only available to Prakash." });
        try
        {
            return Ok(await _portal.DiagnoseNavigationAsync(sessionId));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<IActionResult> PortalStepResult(GstPortalStep step, CancellationToken ct)
    {
        if (step.Status != "complete")
            return Ok(step);

        var result = await _service.ReconcileStoredAsync(
            step.CompanyName,
            step.DateFrom,
            step.DateTo,
            step.AmountTolerance,
            ct);
        if (!string.IsNullOrWhiteSpace(step.Message))
            result.Warning = string.Join(" ", new[] { result.Warning, step.Message }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return Ok(new
        {
            step.Status,
            step.SessionId,
            step.Gstin,
            step.Message,
            result
        });
    }

    private bool IsAllowed(string? username)
    {
        var allowed = _configuration.GetSection("GstBillReco:AllowedUsers").Get<string[]>() ?? [];
        var name = (username ?? "").Trim();
        return name.Length > 0 && allowed.Any(user => string.Equals(user?.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }
}
