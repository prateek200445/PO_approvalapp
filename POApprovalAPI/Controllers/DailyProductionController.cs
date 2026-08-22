using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/daily-production")]
public class DailyProductionController : ControllerBase
{
    private readonly DailyProductionPriceService _service;

    public DailyProductionController(DailyProductionPriceService service)
    {
        _service = service;
    }

    [HttpPost("process")]
    [RequestSizeLimit(30_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Process(IFormFile file, CancellationToken ct)
    {
        try
        {
            ValidateExcel(file);
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;

            var result = await _service.ProcessAsync(ms, file.FileName, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("download/{token}")]
    public IActionResult Download(string token)
    {
        if (!_service.TryGetExport(token, out var bytes, out var fileName) || bytes is null || bytes.Length == 0)
            return NotFound(new { message = "Download expired. Process the Excel again." });

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static void ValidateExcel(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Excel file is required.");

        if (file.Length > 25_000_000)
            throw new InvalidOperationException("File exceeds the 25 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx")
            throw new InvalidOperationException("Only .xlsx files are supported.");
    }
}
