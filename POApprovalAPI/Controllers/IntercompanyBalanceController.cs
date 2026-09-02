using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/Intercompany")]
public class IntercompanyBalanceController : ControllerBase
{
    private readonly IntercompanyBalanceService _service;

    public IntercompanyBalanceController(IntercompanyBalanceService service)
    {
        _service = service;
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
