using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

/// <summary>ERP Report → Customize Report → Create (frmAllTypeofReport).</summary>
[ApiController]
[Route("api/custom-report")]
public class CustomReportController : ControllerBase
{
    private readonly CustomReportService _service;

    public CustomReportController(CustomReportService service) => _service = service;

    [HttpGet("sources")]
    public async Task<IActionResult> Sources(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ListSourcesAsync(ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("saved")]
    public async Task<IActionResult> Saved([FromQuery] string? username, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.ListSavedAsync(username, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("columns")]
    public async Task<IActionResult> Columns([FromQuery] string table, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetColumnsAsync(table, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] CustomReportRunRequest body, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RunAsync(body, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] CustomReportSaveRequest body, CancellationToken ct)
    {
        try
        {
            await _service.SaveAsync(body, ct);
            return Ok(new { message = "Saved" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
