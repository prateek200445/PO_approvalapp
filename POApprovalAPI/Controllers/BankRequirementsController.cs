using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/BankRequirements")]
public class BankRequirementsController : ControllerBase
{
    private readonly BankRequirementsService _service;

    public BankRequirementsController(BankRequirementsService service)
    {
        _service = service;
    }

    [HttpGet("sales-profile")]
    public async Task<IActionResult> GetSalesProfile(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var data = await _service.GetSalesProfileAsync(company, monthList, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("sales-profile/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var bytes = await _service.BuildExcelAsync(company, monthList, refresh);
            var data = await _service.GetSalesProfileAsync(company, monthList, false);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"profile-of-sales-{data.PeriodLabel}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("sales-profile/pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var bytes = await _service.BuildPdfAsync(company, monthList, refresh);
            var data = await _service.GetSalesProfileAsync(company, monthList, false);
            return File(bytes, "application/pdf", $"profile-of-sales-{data.PeriodLabel}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
