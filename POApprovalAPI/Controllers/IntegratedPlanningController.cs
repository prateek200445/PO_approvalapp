using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Planning.Integrated;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning")]
public class IntegratedPlanningController : ControllerBase
{
    private readonly IntegratedPlanningService _service;

    public IntegratedPlanningController(IntegratedPlanningService service)
    {
        _service = service;
    }

    [HttpGet("orders/{orderNo}/timeline")]
    public async Task<IActionResult> GetOrderTimeline(string orderNo, CancellationToken ct)
    {
        try
        {
            var timeline = await _service.GetOrderTimelineAsync(orderNo, ct);
            if (timeline is null)
                return NotFound(new { message = "No planning data found for this order." });

            return Ok(timeline);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
