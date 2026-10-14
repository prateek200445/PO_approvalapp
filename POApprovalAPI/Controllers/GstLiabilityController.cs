using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/gst/liability")]
public class GstLiabilityController : ControllerBase
{
    private readonly GstLiabilityService _service;

    public GstLiabilityController(GstLiabilityService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string month,
        [FromQuery] bool excludeSetOff = false,
        [FromQuery] bool refresh = false)
    {
        if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m))
            return BadRequest(new { message = "Month must be yyyy-MM." });
        try
        {
            return Ok(await _service.GetAsync(m, excludeSetOff, refresh));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("excel")]
    public async Task<IActionResult> Excel([FromBody] GstLiabilityExcelRequest request)
    {
        try
        {
            var bytes = await _service.BuildExcelAsync(request);
            var m = DateTime.ParseExact(request.Month, "yyyy-MM", CultureInfo.InvariantCulture);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"GST_Input_Output_Liability_{m.ToString("MMM-yy", CultureInfo.InvariantCulture)}.xlsx");
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
