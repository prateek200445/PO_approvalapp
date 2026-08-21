using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/debtor-statement")]
public class DebtorStatementController : ControllerBase
{
    private readonly DebtorStatementService _service;

    public DebtorStatementController(DebtorStatementService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _service.GetCompaniesAsync();
            return Ok(companies);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] DebtorStatementQueryRequest request)
    {
        try
        {
            var result = await _service.QueryAsync(request ?? new DebtorStatementQueryRequest());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] DebtorStatementQueryRequest request)
    {
        try
        {
            var bytes = await _service.BuildExportAsync(request ?? new DebtorStatementQueryRequest());
            var asOn = request?.AsOn == default
                ? DateTime.Today.AddDays(-DateTime.Today.Day).ToString("yyyy-MM-dd")
                : request!.AsOn.ToString("yyyy-MM-dd");
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"debtor-statement-{asOn}.xlsx");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
