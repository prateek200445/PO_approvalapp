using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning/fibc")]
public class FibcPlanningController : ControllerBase
{
    private readonly FibcPlanningService _service;

    public FibcPlanningController(FibcPlanningService service)
    {
        _service = service;
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(_service.GetConfig());
    }

    [HttpGet("lines")]
    public async Task<IActionResult> GetLines([FromQuery] string? company, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetLinesAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("grid")]
    public async Task<IActionResult> GetGrid(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? company,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetSlotGridAsync(from, to, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/{orderNo}")]
    public async Task<IActionResult> GetOrderPlan(string orderNo, CancellationToken ct)
    {
        try
        {
            var detail = await _service.GetOrderPlanAsync(orderNo, ct);
            if (detail is null)
                return NotFound(new { message = "No planning or BOM data found for this order." });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/{orderNo}/context")]
    public async Task<IActionResult> GetOrderAllotmentContext(string orderNo, CancellationToken ct)
    {
        try
        {
            var context = await _service.GetOrderAllotmentContextAsync(orderNo, ct);
            if (context is null)
                return NotFound(new { message = "No marketing or BOM data found for this order." });

            return Ok(context);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetActiveShifts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? company,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetActiveShiftsAsync(from, to, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Preview allotment — does not write to the database.</summary>
    [HttpPost("allot/preview")]
    public async Task<IActionResult> PreviewAllotment([FromBody] FibcAllotmentRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            var result = await _service.PreviewAllotmentAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
