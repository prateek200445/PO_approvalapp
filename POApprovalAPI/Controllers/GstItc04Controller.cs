using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/gst/itc04")]
public class GstItc04Controller : ControllerBase
{
    private readonly GstItc04Service _service;

    public GstItc04Controller(GstItc04Service service)
    {
        _service = service;
    }

    /// <summary>period = yyyy-H1 | yyyy-H2 | yyyy-MM (maps to containing half-year). Optional gstin filter.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string period,
        [FromQuery] string? gstin = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            return Ok(await _service.GetAsync(period, gstin, refresh));
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

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] string period, [FromQuery] string? gstin = null)
    {
        try
        {
            var bytes = await _service.BuildExcelAsync(period, gstin);
            var (_, _, label, _, _) = GstItc04Service.ParsePeriod(period);
            var safe = label.Replace('–', '-').Replace(' ', '_');
            return File(bytes,
                "application/vnd.ms-excel.sheet.macroEnabled.12",
                $"ITC_04_Offline_Utility_{safe}.xlsm");
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

    /// <summary>ZIP of Summary / Table 4 / Table 5A CSV files (ITC-04 offline utility columns).</summary>
    [HttpGet("csv")]
    public async Task<IActionResult> Csv([FromQuery] string period, [FromQuery] string? gstin = null)
    {
        try
        {
            var bytes = await _service.BuildCsvZipAsync(period, gstin);
            var (_, _, label, _, _) = GstItc04Service.ParsePeriod(period);
            var safe = label.Replace('–', '-').Replace(' ', '_');
            return File(bytes, "application/zip", $"ITC04_JobWork_{safe}_CSV.zip");
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
