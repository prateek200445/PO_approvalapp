using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/Intercompany")]
public class IntercompanyBalanceController : ControllerBase
{
    private readonly IntercompanyBalanceService _service;
    private readonly IntercompanyTransactionService _transactions;

    public IntercompanyBalanceController(
        IntercompanyBalanceService service,
        IntercompanyTransactionService transactions)
    {
        _service = service;
        _transactions = transactions;
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Groups()
    {
        try
        {
            var names = await _transactions.GetGroupsAsync();
            return Ok(names);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] List<string>? groups)
    {
        if (from is null || to is null)
            return BadRequest(new { message = "Choose a from date and a to date." });

        try
        {
            var data = await _transactions.GetTransactionsAsync(from.Value, to.Value, groups ?? []);
            return Ok(data);
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

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var data = await _service.GetDashboardAsync(through, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var bytes = await _service.BuildExcelAsync(through, refresh);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"intercompany-balances-{through:yyyy-MM-dd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var bytes = await _service.BuildPdfAsync(through, refresh);
            return File(bytes, "application/pdf", $"intercompany-balances-{through:yyyy-MM-dd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
