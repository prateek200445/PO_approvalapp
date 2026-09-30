using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExhibitionLeadsController : ControllerBase
{
    private readonly ExhibitionLeadService _service;
    private readonly IConfiguration _configuration;

    public ExhibitionLeadsController(ExhibitionLeadService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    /// <summary>Public: visitors submit the exhibition forms without logging in.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExhibitionLeadCreateRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new { id = created.Id, createdAt = created.CreatedAt });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? formType = null,
        [FromQuery] string? exhibitionName = null,
        [FromQuery] string username = "")
    {
        if (!CanView(username))
            return StatusCode(403, new { message = "You do not have access to Exhibition Leads." });
        try
        {
            var rows = await _service.ListAsync(formType, exhibitionName);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string? formType = null,
        [FromQuery] string? exhibitionName = null,
        [FromQuery] string username = "")
    {
        if (!CanView(username))
            return StatusCode(403, new { message = "You do not have access to Exhibition Leads." });
        try
        {
            var (bytes, fileName) = await _service.ExportExcelAsync(formType, exhibitionName);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool CanView(string? username)
    {
        var allowed = _configuration.GetSection("ExhibitionLeads:AllowedUsers").Get<string[]>();
        if (allowed is null || allowed.Length == 0)
            allowed = ["umesh"];
        return !string.IsNullOrWhiteSpace(username)
            && allowed.Any(a => string.Equals(a.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
