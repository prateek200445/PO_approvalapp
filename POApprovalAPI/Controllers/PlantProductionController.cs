using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/plant-production")]
public class PlantProductionController : ControllerBase
{
    private readonly PlantProductionService _service;

    public PlantProductionController(PlantProductionService service) => _service = service;

    [HttpGet("loom")]
    public async Task<IActionResult> Loom([FromQuery] string company, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.GetLoomSetupAsync(company, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("loom/recent")]
    public async Task<IActionResult> LoomRecent([FromQuery] string company, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.RecentLoomAsync(company, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("loom/entry")]
    public async Task<IActionResult> LoomEntry([FromQuery] string company, [FromQuery] int id, CancellationToken ct)
    {
        try { return Ok(await _service.LoomEntryAsync(company, id, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("loom/save")]
    public async Task<IActionResult> SaveLoom([FromBody] LoomSaveRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.SaveLoomAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("loom/delete")]
    public async Task<IActionResult> DeleteLoom([FromBody] ProductionDeleteRequest request, CancellationToken ct)
    {
        try { await _service.DeleteLoomAsync(request, ct); return Ok(new { ok = true }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("lamination-rolls")]
    public async Task<IActionResult> LaminationRolls([FromQuery] string company, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.GetLaminationRollsAsync(company, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("lamination/recent")]
    public async Task<IActionResult> LaminationRecent([FromQuery] string company, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.RecentLaminationAsync(company, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("lamination/save")]
    public async Task<IActionResult> SaveLamination([FromBody] LaminationSaveRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.SaveLaminationAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("needle/recent")]
    public async Task<IActionResult> NeedleRecent([FromQuery] string company, [FromQuery] DateTime date, CancellationToken ct)
    {
        try { return Ok(await _service.RecentNeedleAsync(company, date, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("needle/save")]
    public async Task<IActionResult> SaveNeedle([FromBody] NeedleSaveRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.SaveNeedleAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("needle/delete")]
    public async Task<IActionResult> DeleteNeedle([FromBody] ProductionDeleteRequest request, CancellationToken ct)
    {
        try { await _service.DeleteNeedleAsync(request, ct); return Ok(new { ok = true }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
