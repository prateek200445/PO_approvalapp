using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportBillOverdueController : ControllerBase
{
    private readonly ExportBillOverdueService _service;

    public ExportBillOverdueController(ExportBillOverdueService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _service.GetCompaniesAsync();
            var options = await _service.GetCompanyOptionsAsync();
            return Ok(new { companies, options });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load companies." });
        }
    }

    /// <summary>
    /// Export-relevant outstanding group names only (not the full ERP catalog).
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups()
    {
        try
        {
            var groups = await _service.GetGroupsAsync();
            return Ok(new
            {
                groups,
                defaultGroup = ExportBillOverdueService.DefaultGroupName,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load groups." });
        }
    }

    /// <summary>
    /// Bill receivable lines — ERP bill-wise outstanding (Opening + Debit − Credit).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOverdue(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ExportBillOverdueService.DefaultPageSize,
        [FromQuery] bool refresh = false,
        [FromQuery] string? search = null)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var from = dateFrom ?? ExportBillOverdueService.FinancialYearStart(through);
            var result = await _service.GetOverdueBillsAsync(
                company, through, groupName, page, pageSize, refresh, from, search);
            var totalPages = result.PageSize <= 0
                ? 0
                : (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

            return Ok(new
            {
                items = result.Items,
                company = result.Company,
                dateFrom = result.DateFrom,
                asOf = result.AsOf,
                groupName = result.GroupName,
                count = result.Items.Count,
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load export bill overdue." });
        }
    }

    /// <summary>
    /// Customer-wise aging of overdue export bills (same filters as the overdue grid).
    /// </summary>
    [HttpGet("aging")]
    public async Task<IActionResult> GetAging(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var from = dateFrom ?? ExportBillOverdueService.FinancialYearStart(through);
            var result = await _service.GetAgingReportAsync(company, through, groupName, refresh, from);
            return Ok(new
            {
                company = result.Company,
                dateFrom = result.DateFrom,
                asOf = result.AsOf,
                groupName = result.GroupName,
                totalPending = result.TotalPending,
                totalBills = result.TotalBills,
                buckets = result.Buckets,
                customers = result.Customers,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load export bill aging." });
        }
    }

    /// <summary>
    /// Excel of all overdue bills for the current company / group / as-of filters
    /// (not just the current page). Pending = Opening + Debit − Credit per bill.
    /// </summary>
    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var from = dateFrom ?? ExportBillOverdueService.FinancialYearStart(through);
            var bytes = await _service.BuildExportAsync(company, through, groupName, from);
            var fileName = $"export-bill-overdue-{through:yyyy-MM-dd}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to export overdue bills." });
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var from = dateFrom ?? ExportBillOverdueService.FinancialYearStart(through);
            var bytes = await _service.BuildOverduePdfAsync(company, through, groupName, from);
            return File(bytes, "application/pdf", $"export-bill-overdue-{through:yyyy-MM-dd}.pdf");
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to export overdue bills PDF." });
        }
    }

    [HttpGet("aging/pdf")]
    public async Task<IActionResult> ExportAgingPdf(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var from = dateFrom ?? ExportBillOverdueService.FinancialYearStart(through);
            var bytes = await _service.BuildAgingPdfAsync(company, through, groupName, from);
            return File(bytes, "application/pdf", $"export-bill-aging-{through:yyyy-MM-dd}.pdf");
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to export aging report PDF." });
        }
    }
}
