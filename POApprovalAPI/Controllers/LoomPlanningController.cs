using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Planning.Setup;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning/loom")]
public class LoomPlanningController : ControllerBase
{
    private readonly LoomPlanningService _service;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningController(LoomPlanningService service, IOptions<LoomPlanningOptions> options)
    {
        _service = service;
        _options = options.Value;
    }

    [HttpGet("config")]
    public IActionResult GetConfig() => Ok(_service.GetConfig());

    [HttpGet("looms")]
    public async Task<IActionResult> GetLooms([FromQuery] string? company, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetLoomsAsync(company, ct));
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
            return Ok(await _service.GetAllocationGridAsync(from, to, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("production")]
    public async Task<IActionResult> GetProductionMeters(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? company,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetProductionMetersAsync(from, to, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("ppm")]
    public async Task<IActionResult> GetPpmSpecs(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetPpmSpecsAsync(ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/plan")]
    public async Task<IActionResult> GetOrderPlan([FromQuery] string orderNo, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            var detail = await _service.GetOrderPlanAsync(orderNo, ct);
            if (detail is null)
                return NotFound(new { message = "No loom allocation or BOM fabric data found for this order." });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/context")]
    public async Task<IActionResult> GetOrderContext([FromQuery] string orderNo, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            var context = await _service.GetOrderContextAsync(orderNo, ct);
            if (context is null)
                return NotFound(new { message = "No marketing or loom allocation data found for this order." });

            return Ok(context);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/allotment-context")]
    public async Task<IActionResult> GetOrderAllotmentContext([FromQuery] string orderNo, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            var context = await _service.GetOrderAllotmentContextAsync(orderNo, ct);
            if (context is null)
                return NotFound(new { message = "No marketing, BOM, or loom data found for this order." });

            return Ok(context);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/route")]
    public async Task<IActionResult> GetOrderRoute([FromQuery] string orderNo, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            var routeService = HttpContext.RequestServices.GetRequiredService<OrderPlanningRouteService>();
            return Ok(await routeService.ResolveAsync(orderNo, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("allot/preview")]
    public async Task<IActionResult> PreviewAllotment([FromBody] LoomAllotmentRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            return Ok(await _service.PreviewAllotmentAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("allot/confirm")]
    public async Task<IActionResult> ConfirmAllotment([FromBody] LoomAllotmentRequest request, CancellationToken ct)
    {
        try
        {
            if (!_options.AllowConfirmSave)
                return StatusCode(403, new { message = "Confirm save is disabled (LoomPlanning:AllowConfirmSave)." });

            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            return Ok(await _service.ConfirmAllotmentAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
