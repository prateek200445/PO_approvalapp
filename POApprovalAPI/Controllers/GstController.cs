using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/Gst")]
public class GstController : ControllerBase
{
    private readonly GstDocumentSummaryService _documentSummary;
    private readonly Gstr1ReturnService _gstr1;
    private readonly IcegateService _icegate;

    public GstController(GstDocumentSummaryService documentSummary, Gstr1ReturnService gstr1, IcegateService icegate)
    {
        _documentSummary = documentSummary;
        _gstr1 = gstr1;
        _icegate = icegate;
    }

    [HttpGet("icegate")]
    public IActionResult GetIcegateStatus() => Ok(_icegate.GetStatus());

    [HttpPost("icegate")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public IActionResult UploadIcegate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Choose the ICEGATE export workbook (.xlsx) to upload." });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .xlsx files are supported — save the ICEGATE download as an Excel workbook." });
        try
        {
            using var stream = file.OpenReadStream();
            var upload = _icegate.Import(stream, file.FileName);
            return Ok(new { upload, status = _icegate.GetStatus() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Could not read the ICEGATE file: {ex.Message}" });
        }
    }

    [HttpDelete("icegate/{id}")]
    public IActionResult DeleteIcegateUpload(string id) =>
        _icegate.DeleteUpload(id) ? Ok(_icegate.GetStatus()) : NotFound(new { message = "Upload not found" });

    [HttpGet("gstr1")]
    public async Task<IActionResult> GetGstr1(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? scope = null,
        [FromQuery] bool includeUnapproved = true,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var (start, end) = ResolvePeriod(from, to);
            return Ok(await _gstr1.GetReportAsync(start, end, scope, includeUnapproved, refresh));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("gstr1/excel")]
    public async Task<IActionResult> ExportGstr1(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? scope = null,
        [FromQuery] bool includeUnapproved = true)
    {
        try
        {
            var (start, end) = ResolvePeriod(from, to);
            var (bytes, report) = await _gstr1.BuildExcelAsync(start, end, scope, includeUnapproved);
            var tag = report.Gstins.Count == 1 ? report.Gstins[0] : report.Scope.Length == 0 ? "all" : "selection";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"gstr1-{tag}-{start:yyyy-MM}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("document-summary")]
    public async Task<IActionResult> GetDocumentSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var (start, end) = ResolvePeriod(from, to);
            return Ok(await _documentSummary.GetSummaryAsync(start, end, refresh));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("document-summary/excel")]
    public async Task<IActionResult> ExportDocumentSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? company = null)
    {
        try
        {
            var (start, end) = ResolvePeriod(from, to);
            var bytes = await _documentSummary.BuildExcelAsync(start, end, company);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"gst-document-summary-{start:yyyy-MM-dd}-to-{end:yyyy-MM-dd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private static (DateTime From, DateTime To) ResolvePeriod(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var prevMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var start = (from ?? prevMonthStart).Date;
        var end = (to ?? start.AddMonths(1).AddDays(-1)).Date;
        if (end < start) end = start;
        if ((end - start).TotalDays > 366) end = start.AddDays(366);
        return (start, end);
    }
}
