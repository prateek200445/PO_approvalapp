using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesDashboardController : ControllerBase
{
    private readonly SalesDashboardService _salesDashboard;

    public SalesDashboardController(SalesDashboardService salesDashboard)
    {
        _salesDashboard = salesDashboard;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _salesDashboard.GetCompaniesAsync();
            return Ok(new { companies });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("total-sales")]
    public async Task<IActionResult> GetTotalSales(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var totals = await _salesDashboard.GetSalesTotalsAsync(company, from, to);

            return Ok(new
            {
                totalSales = totals.TotalSales,
                totalQuantity = totals.TotalQuantity,
                averageRate = totals.AverageRate,
                byGroup = totals.ByGroup,
                bySubGroup = totals.BySubGroup,
                company,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                source = "vw_Sales_EBIDTA",
                salesColumn = totals.SalesColumn,
                quantityColumn = totals.QuantityColumn,
                rateColumn = totals.RateColumn,
                method = totals.Method,
                rowCount = totals.RowCount,
                columns = totals.Columns,
                elapsedSeconds = totals.ElapsedSeconds,
                note =
                    "Mirrors SP_Sales_EBIDTA aggregation on vw_Sales_EBIDTA; excl. InterGroup='Intergroup' " +
                    "(IsInterCompany='yes'). KPIs from Sales grand-total; byGroup/bySubGroup from leaf rows.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Year-by-year Total Sales (Indian FY Apr–Mar) excl. intercompany
    /// (same GetSalesTotalsAsync / vw_Sales_EBIDTA path as KPIs).
    /// </summary>
    [HttpGet("yearly-trend")]
    public async Task<IActionResult> GetYearlyTrend(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? asOf = null,
        [FromQuery] int years = 5)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var trend = await _salesDashboard.GetSalesYearlyTrendAsync(company, through, years);

            return Ok(new
            {
                trend,
                company,
                asOf = through.ToString("yyyy-MM-dd"),
                years,
                source = "vw_Sales_EBIDTA",
                note =
                    "Each bar is excl-IC Sales grand-total Amount for that FY (Apr–Mar); current FY capped at asOf. " +
                    "Same basis as total-sales (vw_Sales_EBIDTA, InterGroup <> Intergroup).",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Top countries by Sales Value from vw_Countrywise_sales_dashboard
    /// (Value = Amount - DebitNote; intercompany already excluded in the view).
    /// Company maps to FactoryInfo.GroupName; dates map to overlapping InvYear FYs.
    /// </summary>
    [HttpGet("by-country")]
    public async Task<IActionResult> GetByCountry(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int top = 5)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetSalesByCountryAsync(company, from, to, top);

            return Ok(new
            {
                byCountry = result.ByCountry,
                company,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                invYears = result.InvYears,
                periodLabel = result.PeriodLabel,
                groupNames = result.GroupNames,
                top,
                source = "vw_Countrywise_sales_dashboard",
                note = "SUM(Value) by Country; Value = Amount - DebitNote; excl. intercompany in view; FY totals via InvYear; company maps to FactoryInfo.GroupName.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Progressive sections: kpis | charts | tables | all.
    /// First section call loads ERP ledger into cache; later sections reuse it.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int rptType = 0,
        [FromQuery] string category = "Sales",
        [FromQuery] string section = "all",
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;

            if (rptType is < 0 or > 2)
                return BadRequest(new { message = "rptType must be 0 (Summary), 1 (Detail), or 2 (Date Summary)." });

            if (!string.Equals(category, "Sales", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(category, "Purchase", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "category must be Sales or Purchase." });
            }

            var data = await _salesDashboard.GetDashboardSectionAsync(
                company,
                from,
                to,
                rptType,
                category,
                section,
                refresh);

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
