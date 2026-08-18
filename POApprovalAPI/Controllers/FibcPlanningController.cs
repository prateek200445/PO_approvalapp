using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning/fibc")]
public class FibcPlanningController : ControllerBase
{
    private readonly FibcPlanningService _service;
    private readonly FibcQuotationHoldService _quotationService;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningController(
        FibcPlanningService service,
        FibcQuotationHoldService quotationService,
        IOptions<FibcPlanningOptions> options)
    {
        _service = service;
        _quotationService = quotationService;
        _options = options.Value;
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

    /// <summary>Confirm allotment — re-runs preview then INSERTs into prod_fibcallocationMaster.</summary>
    [HttpPost("allot/confirm")]
    public async Task<IActionResult> ConfirmAllotment([FromBody] FibcAllotmentRequest request, CancellationToken ct)
    {
        try
        {
            if (!_options.AllowConfirmSave)
                return StatusCode(403, new { message = "Confirm save is disabled in configuration (FibcPlanning:AllowConfirmSave)." });

            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            var result = await _service.ConfirmAllotmentAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Preview critical order shift — may propose moving blocking orders to free capacity.</summary>
    [HttpPost("critical/preview")]
    public async Task<IActionResult> PreviewCriticalShift([FromBody] FibcCriticalShiftRequest request, CancellationToken ct)
    {
        try
        {
            if (!_options.CriticalShiftEnabled)
                return StatusCode(403, new { message = "Critical order shifting is disabled (FibcPlanning:CriticalShiftEnabled)." });

            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            return Ok(await _service.PreviewCriticalShiftAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Confirm critical shift — moves blocking orders then saves critical order to ERP.</summary>
    [HttpPost("critical/confirm")]
    public async Task<IActionResult> ConfirmCriticalShift([FromBody] FibcCriticalShiftRequest request, CancellationToken ct)
    {
        try
        {
            if (!_options.CriticalShiftEnabled)
                return StatusCode(403, new { message = "Critical order shifting is disabled." });

            if (!_options.AllowConfirmSave)
                return StatusCode(403, new { message = "Confirm save is disabled (FibcPlanning:AllowConfirmSave)." });

            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return BadRequest(new { message = "Order number is required." });

            return Ok(await _service.ConfirmCriticalShiftAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("quotation/holds")]
    public async Task<IActionResult> GetQuotationHolds([FromQuery] string? company, CancellationToken ct)
    {
        try
        {
            if (!_options.QuotationHoldEnabled)
                return Ok(Array.Empty<FibcQuotationHoldDto>());

            return Ok(await _quotationService.GetActiveHoldsAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("quotation/hold")]
    public async Task<IActionResult> CreateQuotationHold([FromBody] FibcQuotationHoldRequest request, CancellationToken ct)
    {
        try
        {
            if (!_options.QuotationHoldEnabled)
                return StatusCode(403, new { message = "Quotation holds are disabled (FibcPlanning:QuotationHoldEnabled)." });

            var result = await _quotationService.CreateHoldAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("quotation/{holdId:int}/confirm")]
    public async Task<IActionResult> ConfirmQuotationHold(
        int holdId,
        [FromBody] FibcQuotationConfirmRequest? request,
        CancellationToken ct)
    {
        try
        {
            if (!_options.QuotationHoldEnabled)
                return StatusCode(403, new { message = "Quotation holds are disabled." });

            if (!_options.AllowConfirmSave)
                return StatusCode(403, new { message = "Confirm save is disabled (FibcPlanning:AllowConfirmSave)." });

            var result = await _quotationService.ConfirmHoldAsync(holdId, request?.ReplaceExisting ?? false, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("quotation/{holdId:int}/cancel")]
    public async Task<IActionResult> CancelQuotationHold(int holdId, CancellationToken ct)
    {
        try
        {
            if (!_options.QuotationHoldEnabled)
                return StatusCode(403, new { message = "Quotation holds are disabled." });

            var result = await _quotationService.CancelHoldAsync(holdId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
