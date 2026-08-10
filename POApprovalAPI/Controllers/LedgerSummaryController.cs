using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/ledger-summary")]
public class LedgerSummaryController : ControllerBase
{
    private readonly LedgerSummaryService _service;

    public LedgerSummaryController(LedgerSummaryService service)
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

    [HttpGet("ledgers")]
    public async Task<IActionResult> GetLedgers([FromQuery] string? company = null, [FromQuery] string? companies = null)
    {
        try
        {
            var values = new List<string>();
            if (!string.IsNullOrWhiteSpace(companies))
                values.AddRange(companies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (!string.IsNullOrWhiteSpace(company))
                values.Add(company.Trim());

            var ledgers = values.Count <= 1
                ? await _service.GetLedgersAsync(values.FirstOrDefault() ?? "")
                : await _service.GetLedgersForCompaniesAsync(values);

            return Ok(ledgers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] LedgerSummaryQueryRequest request)
    {
        try
        {
            var result = await _service.QueryAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("query-batch")]
    public async Task<IActionResult> QueryBatch([FromBody] LedgerSummaryBatchQueryRequest request)
    {
        try
        {
            var result = await _service.QueryBatchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("export")]
    [RequestSizeLimit(60_000_000)]
    public IActionResult Export([FromBody] LedgerSummaryExportRequest request)
    {
        try
        {
            if (request?.Result?.Rows == null || request.Result.Rows.Count == 0)
                return BadRequest(new { message = "Nothing to export." });

            var bytes = _service.BuildExport(request);
            var fileName = $"ledger-summary-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
