using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/OrderBookSummary")]
public class OrderBookSummaryController : ControllerBase
{
    private readonly OrderBookSummaryService _service;

    public OrderBookSummaryController(OrderBookSummaryService service)
    {
        _service = service;
    }

    [HttpGet("access")]
    public IActionResult Access([FromQuery] string username = "")
    {
        return Ok(_service.CheckAccess(username));
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string username = "",
        [FromQuery] string? asOf = null,
        [FromQuery] bool refresh = false)
    {
        var access = _service.CheckAccess(username);
        if (!access.Allowed)
            return StatusCode(403, new { message = "Order Book Summary is restricted. Ask an admin to add your username to OrderBookSummary:AllowedUsers." });

        try
        {
            DateTime? asOfDate = null;
            if (!string.IsNullOrWhiteSpace(asOf) && DateTime.TryParse(asOf, out var parsed))
                asOfDate = parsed.Date;

            var data = await _service.GetSummaryAsync(asOfDate, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
