using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/import-jv")]
public class ImportJvController : ControllerBase
{
    private readonly ImportJvService _service;

    public ImportJvController(ImportJvService service)
    {
        _service = service;
    }

    [HttpGet("units")]
    public async Task<IActionResult> Units([FromQuery] string group)
    {
        try
        {
            return Ok(await _service.GetUnitsAsync(group));
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

    [HttpGet("template")]
    public async Task<IActionResult> Template([FromQuery] string group, [FromQuery] string company)
    {
        try
        {
            var bytes = await _service.BuildTemplateAsync(group, company);
            var safe = new string((company ?? "ImportJV").Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-').ToArray()).Trim();
            if (safe.Length == 0) safe = "ImportJV";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ImportJV-{safe}.xlsx");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("preview")]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview(
        IFormFile file,
        [FromForm] string group,
        [FromForm] string company)
    {
        try
        {
            await using var stream = await CopyAsync(file);
            return Ok(await _service.PreviewAsync(group, company, stream, file.FileName));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("commit")]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Commit(
        IFormFile file,
        [FromForm] string group,
        [FromForm] string company,
        [FromForm] string username)
    {
        try
        {
            await using var stream = await CopyAsync(file);
            return Ok(await _service.CommitAsync(group, company, username, stream, file.FileName));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<MemoryStream> CopyAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Upload an Excel or CSV file.");
        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            && !ext.Equals(".xlsm", StringComparison.OrdinalIgnoreCase)
            && !ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Use .xlsx or .csv.");
        if (file.Length > 15_000_000)
            throw new ArgumentException("The file is larger than 15 MB.");
        var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }
}
