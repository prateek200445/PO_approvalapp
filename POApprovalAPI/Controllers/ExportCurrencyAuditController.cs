using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/export-currency-audit")]
public class ExportCurrencyAuditController : ControllerBase
{
    private readonly ExportCurrencyAuditService _audit;
    private readonly LedgerSummaryService _ledgerSummary;

    public ExportCurrencyAuditController(
        ExportCurrencyAuditService audit,
        LedgerSummaryService ledgerSummary)
    {
        _audit = audit;
        _ledgerSummary = ledgerSummary;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _ledgerSummary.GetCompaniesAsync();
            return Ok(companies);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Export credit/debit notes on export ledgers where INR is posted but stored FC/USD is zero or invalid.
    /// </summary>
    [HttpGet("run")]
    public async Task<IActionResult> Run(
        [FromQuery] string? company = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] decimal minInr = 100)
    {
        try
        {
            var to = (dateTo ?? DateTime.Today).Date;
            var from = (dateFrom ?? ExportCurrencyAuditService.FinancialYearStart(to)).Date;
            var result = await _audit.RunAuditAsync(company, from, to, minInr);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(
        [FromQuery] string? company = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] decimal minInr = 100)
    {
        try
        {
            var to = (dateTo ?? DateTime.Today).Date;
            var from = (dateFrom ?? ExportCurrencyAuditService.FinancialYearStart(to)).Date;
            var result = await _audit.RunAuditAsync(company, from, to, minInr);
            if (result.TotalCount == 0)
                return BadRequest(new { message = "Nothing to export." });

            var bytes = _audit.BuildExcel(result);
            var fileName = $"export-currency-audit-{result.DateTo}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
