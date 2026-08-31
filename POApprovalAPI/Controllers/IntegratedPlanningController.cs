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

    [HttpGet("orders/timeline")]
    public async Task<IActionResult> GetOrderTimeline([FromQuery] string orderNo, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

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

    [HttpPost("orders/plan/preview")]
    public async Task<IActionResult> PreviewFullOrder([FromBody] POApprovalAPI.Planning.Integrated.Models.FullOrderPlanRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "OrderNo is required." });

            return Ok(await _service.PreviewFullOrderAsync(request.OrderNo, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("orders/plan/confirm")]
    public async Task<IActionResult> ConfirmFullOrder([FromBody] POApprovalAPI.Planning.Integrated.Models.FullOrderPlanRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "OrderNo is required." });

            return Ok(await _service.ConfirmFullOrderAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
