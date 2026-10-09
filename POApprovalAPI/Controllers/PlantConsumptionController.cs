using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/plant-consumption")]
public class PlantConsumptionController : ControllerBase
{
    private readonly PlantConsumptionService _service;

    public PlantConsumptionController(PlantConsumptionService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies(CancellationToken ct)
    {
        try { return Ok(await _service.GetCompaniesAsync(ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups([FromQuery] string company, CancellationToken ct)
    {
        try { return Ok(await _service.GetLookupsAsync(company, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("plant")]
    public async Task<IActionResult> Plant([FromQuery] string company, [FromQuery] string plant, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.GetPlantSetupAsync(company, plant, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("materials")]
    public async Task<IActionResult> Materials([FromQuery] string company, [FromQuery] string warehouse, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.GetMaterialsAsync(company, warehouse, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("rolls")]
    public async Task<IActionResult> Rolls([FromQuery] string company, [FromQuery] string plant, [FromQuery] DateTime date, [FromQuery] string shift, CancellationToken ct)
    {
        try { return Ok(new { rolls = await _service.GetRollsAsync(company, plant, date, shift, ct) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] string company, [FromQuery] string plant, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.GetRecentAsync(company, plant, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("entry")]
    public async Task<IActionResult> Entry([FromQuery] string company, [FromQuery] int groupSrNo, CancellationToken ct)
    {
        try
        {
            var row = await _service.GetEntryAsync(company, groupSrNo, ct);
            return row == null ? NotFound(new { message = "Entry not found." }) : Ok(row);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] PlantConsumptionSaveRequest request, CancellationToken ct)
    {
        try { return Ok(new { groupSrNo = await _service.SaveAsync(request, ct) }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] PlantConsumptionDeleteRequest request, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(request, ct);
            return Ok(new { ok = true });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
