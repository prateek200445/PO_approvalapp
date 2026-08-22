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
}
