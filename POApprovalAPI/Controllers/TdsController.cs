using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/tds")]
public class TdsController : ControllerBase
{
    private readonly TdsLiabilityService _liability;
    private readonly TdsSectionSheetService _sections;

    public TdsController(TdsLiabilityService liability, TdsSectionSheetService sections)
    {
        _liability = liability;
        _sections = sections;
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Groups()
    {
        try
        {
            return Ok(await _liability.GetGroupsAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("liability")]
    public async Task<IActionResult> Liability([FromQuery] string group, [FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(group))
            return BadRequest(new { message = "Choose a group." });
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
            return BadRequest(new { message = "Dates must be yyyy-MM-dd." });
        if (toDate < fromDate)
            return BadRequest(new { message = "The end date is before the start date." });
        if ((toDate - fromDate).TotalDays > 400)
            return BadRequest(new { message = "Choose a period of a year or less." });

        try
        {
            return Ok(await _liability.GetLiabilityAsync(group.Trim(), fromDate, toDate));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("94a")]
    public async Task<IActionResult> Section94A([FromQuery] string group, [FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(group))
            return BadRequest(new { message = "Choose a group." });
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
            return BadRequest(new { message = "Dates must be yyyy-MM-dd." });
        if (toDate < fromDate)
            return BadRequest(new { message = "The end date is before the start date." });
        if ((toDate - fromDate).TotalDays > 400)
            return BadRequest(new { message = "Choose a period of a year or less." });

        try
        {
            return Ok(await _sections.Get94AAsync(group.Trim(), fromDate, toDate));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("94c")]
    public async Task<IActionResult> Section94C([FromQuery] string group, [FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(group))
            return BadRequest(new { message = "Choose a group." });
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
            return BadRequest(new { message = "Dates must be yyyy-MM-dd." });
        if (toDate < fromDate)
            return BadRequest(new { message = "The end date is before the start date." });
        if ((toDate - fromDate).TotalDays > 400)
            return BadRequest(new { message = "Choose a period of a year or less." });

        try
        {
            return Ok(await _sections.Get94CAsync(group.Trim(), fromDate, toDate));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("94h")]
    public Task<IActionResult> Section94H([FromQuery] string group, [FromQuery] string from, [FromQuery] string to) =>
        Section(group, from, to, _sections.Get94HAsync);

    [HttpGet("94i")]
    public Task<IActionResult> Section94I([FromQuery] string group, [FromQuery] string from, [FromQuery] string to) =>
        Section(group, from, to, _sections.Get94IAsync);

    [HttpGet("94j")]
    public Task<IActionResult> Section94J([FromQuery] string group, [FromQuery] string from, [FromQuery] string to) =>
        Section(group, from, to, _sections.Get94JAsync);

    [HttpGet("94q")]
    public Task<IActionResult> Section94Q([FromQuery] string group, [FromQuery] string from, [FromQuery] string to) =>
        Section(group, from, to, _sections.Get94QAsync);

    private async Task<IActionResult> Section(
        string group,
        string from,
        string to,
        Func<string, DateTime, DateTime, Task<TdsSectionSheetDto>> load)
    {
        if (string.IsNullOrWhiteSpace(group))
            return BadRequest(new { message = "Choose a group." });
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
            return BadRequest(new { message = "Dates must be yyyy-MM-dd." });
        if (toDate < fromDate)
            return BadRequest(new { message = "The end date is before the start date." });
        if ((toDate - fromDate).TotalDays > 400)
            return BadRequest(new { message = "Choose a period of a year or less." });

        try
        {
            return Ok(await load(group.Trim(), fromDate, toDate));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
