using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/planning/setup")]
public class PlanningSetupController : ControllerBase
{
    private readonly PlanningSetupService _service;

    public PlanningSetupController(PlanningSetupService service)
    {
        _service = service;
    }

    [HttpGet("constants")]
    public IActionResult GetConstants()
    {
        return Ok(new
        {
            bagFamilies = PlanningSetupConstants.BagFamilies,
            poolPurposes = PlanningSetupConstants.PoolPurposes,
            winderCategories = PlanningSetupConstants.WinderCategories,
            allotmentModes = PlanningSetupConstants.AllotmentModes,
            dustLevels = PlanningSetupConstants.DustLevels,
            fabricForms = PlanningSetupConstants.FabricForms,
            changeoverTiers = PlanningSetupConstants.ChangeoverTiers,
        });
    }

    [HttpGet("factories/search")]
    public async Task<IActionResult> SearchFactories([FromQuery] string? q, [FromQuery] int limit = 25, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SearchFactoriesAsync(q, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("factories/enabled")]
    public async Task<IActionResult> GetEnabledFactories(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetEnabledFactoriesAsync(ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("factories/config")]
    public async Task<IActionResult> GetFactoryConfig([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            var config = await _service.GetFactoryConfigAsync(company, ct);
            return Ok(config ?? new PlanningFactoryConfigDto { CompanyName = company.Trim(), IsPlanningEnabled = false });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("factories/config")]
    public async Task<IActionResult> SaveFactoryConfig([FromBody] UpsertPlanningFactoryConfigRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.SaveFactoryConfigAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("lines")]
    public async Task<IActionResult> GetLines([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.GetLinesAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("lines/import-erp")]
    public async Task<IActionResult> ImportLinesFromErp([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.ImportLinesFromErpAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("lines")]
    public async Task<IActionResult> SaveLines([FromBody] SavePlanningLineConfigsRequest request, CancellationToken ct)
    {
        try
        {
            await _service.SaveLinesAsync(request, ct);
            return Ok(new { success = true, message = "Line configuration saved." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("looms")]
    public async Task<IActionResult> GetLoomPool([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.GetLoomPoolAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("looms")]
    public async Task<IActionResult> SaveLoomPool([FromBody] SavePlanningLoomPoolRequest request, CancellationToken ct)
    {
        try
        {
            await _service.SaveLoomPoolAsync(request, ct);
            return Ok(new { success = true, message = "Loom pool saved." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("team-factors")]
    public async Task<IActionResult> GetTeamFactors([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.GetTeamFactorsAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("team-factors")]
    public async Task<IActionResult> SaveTeamFactors([FromBody] SavePlanningTeamFactorRequest request, CancellationToken ct)
    {
        try
        {
            await _service.SaveTeamFactorsAsync(request, ct);
            return Ok(new { success = true, message = "Team factors saved." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("team-factors/recalculate")]
    public async Task<IActionResult> RecalculateTeamFactors(
        [FromQuery] string company,
        [FromQuery] int sampleDays = 30,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.RecalculateTeamFactorsAsync(company, sampleDays, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("backlog")]
    public async Task<IActionResult> GetBacklog([FromQuery] string company, [FromQuery] string? status = "Open", CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.GetBacklogAsync(company, status, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("backlog")]
    public async Task<IActionResult> CreateBacklog([FromBody] CreatePlanningBacklogRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CreateBacklogAsync(request, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("backlog/{backlogId:int}/clear")]
    public async Task<IActionResult> ClearBacklog(int backlogId, CancellationToken ct)
    {
        try
        {
            var cleared = await _service.ClearBacklogAsync(backlogId, ct);
            if (!cleared)
                return NotFound(new { message = "Backlog not found or already cleared." });

            return Ok(new { success = true, message = "Backlog cleared." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("loom-preference")]
    public async Task<IActionResult> GetLoomPreferenceChart([FromQuery] string company, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            return Ok(await _service.GetLoomPreferenceChartAsync(company, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("loom-preference")]
    public async Task<IActionResult> SaveLoomPreferenceChart([FromBody] SavePlanningLoomPreferenceChartRequest request, CancellationToken ct)
    {
        try
        {
            await _service.SaveLoomPreferenceChartAsync(request, ct);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
