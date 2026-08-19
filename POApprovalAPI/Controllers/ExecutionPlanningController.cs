using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Planning.Execution;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning/execution")]
public class ExecutionPlanningController : ControllerBase
{
    private readonly ExecutionPlanningService _execution;
    private readonly PlanningSetupService _setup;

    public ExecutionPlanningController(ExecutionPlanningService execution, PlanningSetupService setup)
    {
        _execution = execution;
        _setup = setup;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrderExecution([FromQuery] string orderNo, [FromQuery] string? company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            return Ok(await _execution.GetOrderExecutionAsync(orderNo, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("orders/bailing")]
    public async Task<IActionResult> GetBailingReconciliation([FromQuery] string orderNo, [FromQuery] string? company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return BadRequest(new { message = "orderNo query parameter is required." });

            return Ok(await _execution.GetBailingReconciliationAsync(orderNo, company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetFactoryBoard(
        [FromQuery] string company,
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _execution.GetFactoryBoardAsync(company, date, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("downtime")]
    public async Task<IActionResult> GetDowntime(
        [FromQuery] string company,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company is required." });

            return Ok(await _setup.GetDowntimeAsync(company, from, to, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("downtime")]
    public async Task<IActionResult> SaveDowntime([FromBody] SavePlanningDowntimeRequest request, CancellationToken ct)
    {
        try
        {
            await _setup.SaveDowntimeAsync(request, ct);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpDelete("downtime/{downtimeId:int}")]
    public async Task<IActionResult> DeleteDowntime(int downtimeId, CancellationToken ct)
    {
        try
        {
            await _setup.DeleteDowntimeAsync(downtimeId, ct);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
