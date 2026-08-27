using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/pnl")]
public class PnlController : ControllerBase
{
    private readonly PnlService _service;

    public PnlController(PnlService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies()
    {
        try
        {
            return Ok(await _service.GetCompaniesAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("income-expense")]
    public async Task<IActionResult> IncomeExpense(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = dateTo ?? from.AddMonths(1).AddDays(-1);
            return Ok(await _service.GetIncomeExpenseAsync(company, from, to));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("provisions")]
    public async Task<IActionResult> Provisions([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetProvisionsAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("provisions")]
    public async Task<IActionResult> SaveProvisions([FromBody] PnlProvisionSaveRequest request)
    {
        try
        {
            await _service.SaveProvisionsAsync(request);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetStockAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("stock")]
    public async Task<IActionResult> SaveStock([FromBody] PnlStockSaveRequest request)
    {
        try
        {
            await _service.SaveStockAsync(request);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stock-year")]
    public async Task<IActionResult> StockYear([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetStockYearAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("stock-year")]
    public async Task<IActionResult> SaveStockYear([FromBody] PnlStockYearSaveRequest request)
    {
        try
        {
            await _service.SaveStockYearAsync(request);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("overhead")]
    public async Task<IActionResult> Overhead([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetOverheadAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("overhead")]
    public async Task<IActionResult> SaveOverhead([FromBody] PnlOverheadSaveRequest request)
    {
        try
        {
            await _service.SaveOverheadAsync(request);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("uploads")]
    public async Task<IActionResult> Uploads([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetUploadsAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("templates/{uploadType}")]
    public async Task<IActionResult> Template(
        string uploadType,
        [FromQuery] string company,
        [FromQuery] string month)
    {
        try
        {
            var (bytes, fileName) = await _service.GetUploadTemplateAsync(company, PnlService.ParseMonth(month), uploadType);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("uploads/{uploadType}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> SaveUpload(
        string uploadType,
        IFormFile file,
        [FromForm] string company,
        [FromForm] string month,
        [FromForm] string? remarks = null)
    {
        try
        {
            ValidateExcel(file);
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            await _service.SaveUploadAsync(
                company,
                PnlService.ParseMonth(month),
                uploadType,
                file.FileName,
                file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ms.ToArray(),
                remarks);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("statement")]
    public async Task<IActionResult> Statement([FromQuery] string company, [FromQuery] string month)
    {
        try
        {
            return Ok(await _service.GetStatementAsync(company, PnlService.ParseMonth(month)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
