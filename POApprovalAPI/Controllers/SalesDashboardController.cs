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

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string category = "Sales",
        [FromQuery] bool refresh = false)
    {
        try
        {
            if (!string.Equals(category, "Sales", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(category, "Purchase", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "category must be Sales or Purchase." });
            }

            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var data = await _salesDashboard.GetOverviewAsync(category, company, from, to, refresh);
            var totals = data.Totals;
            return Ok(new
            {
                totalSales = totals.TotalSales,
                totalPurchase = totals.TotalPurchase,
                totalQuantity = totals.TotalQuantity,
                averageRate = totals.AverageRate,
                byGroup = totals.ByGroup,
                bySubGroup = totals.BySubGroup,
                trend = data.Trend,
                byCountry = data.ByCountry,
                countryPeriodLabel = data.CountryPeriodLabel,
                exportCustomers = data.ExportCustomers,
                suppliers = data.Suppliers,
                company,
                category,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var options = await _salesDashboard.GetCompanyOptionsAsync();
            var companies = options
                .Where(o => o.Kind != "group")
                .Select(o => o.Value)
                .ToList();
            return Ok(new { companies, options });
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
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var totals = await _salesDashboard.GetSalesTotalsAsync(company, from, to, refresh);

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
    /// Total Purchase + Quantity + Average Rate + byGroup/bySubGroup from vw_Purchase_EBIDTA
    /// (mirrors SP_Purchase_EBIDTA; excl. InterGroup='Intergroup').
    /// </summary>
    [HttpGet("total-purchase")]
    public async Task<IActionResult> GetTotalPurchase(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var totals = await _salesDashboard.GetPurchaseTotalsAsync(company, from, to, refresh);

            return Ok(new
            {
                totalPurchase = totals.TotalPurchase,
                totalQuantity = totals.TotalQuantity,
                averageRate = totals.AverageRate,
                byGroup = totals.ByGroup,
                bySubGroup = totals.BySubGroup,
                company,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                source = "vw_Purchase_EBIDTA",
                salesColumn = totals.SalesColumn,
                quantityColumn = totals.QuantityColumn,
                rateColumn = totals.RateColumn,
                method = totals.Method,
                rowCount = totals.RowCount,
                columns = totals.Columns,
                elapsedSeconds = totals.ElapsedSeconds,
                note =
                    "Mirrors SP_Purchase_EBIDTA aggregation on vw_Purchase_EBIDTA; excl. InterGroup='Intergroup' " +
                    "(IsInterCompany='yes'). KPIs from Purchase grand-total; byGroup/bySubGroup from leaf rows.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Year-by-year Total Sales or Purchase (Indian FY Apr–Mar) excl. intercompany.
    /// category=Sales → vw_Sales_EBIDTA; category=Purchase → vw_Purchase_EBIDTA.
    /// </summary>
    [HttpGet("yearly-trend")]
    public async Task<IActionResult> GetYearlyTrend(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? asOf = null,
        [FromQuery] int years = 5,
        [FromQuery] string category = "Sales",
        [FromQuery] bool refresh = false)
    {
        try
        {
            if (!string.Equals(category, "Sales", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(category, "Purchase", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "category must be Sales or Purchase." });
            }

            var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
            var through = asOf ?? DateTime.Today;
            var trend = isPurchase
                ? await _salesDashboard.GetPurchaseYearlyTrendAsync(company, through, years, refresh)
                : await _salesDashboard.GetSalesYearlyTrendAsync(company, through, years, refresh);

            var source = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";
            var label = isPurchase ? "Purchase" : "Sales";

            return Ok(new
            {
                trend,
                company,
                category = label,
                asOf = through.ToString("yyyy-MM-dd"),
                years,
                source,
                note =
                    $"Each bar is excl-IC {label} grand-total Amount for that FY (Apr–Mar); current FY capped at asOf. " +
                    $"Same basis as total-{(isPurchase ? "purchase" : "sales")} ({source}, InterGroup <> Intergroup).",
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
        [FromQuery] int top = 10,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetSalesByCountryAsync(company, from, to, top, refresh);

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
                source = string.IsNullOrWhiteSpace(result.Source)
                    ? "vw_Countrywise_sales_dashboard"
                    : result.Source,
                note = "SUM(Value) by Country; Value = Amount - DebitNote; excl. intercompany in view; FY totals via InvYear; company maps to CompanyName and/or GroupName.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Top 10 export customers (non-India), excl. intercompany.
    /// </summary>
    [HttpGet("top-export-customers")]
    public async Task<IActionResult> GetTopExportCustomers(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int top = 10,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetTopExportCustomersAsync(company, from, to, top, refresh);
            return Ok(new
            {
                items = result.Items,
                company,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                top,
                source = result.Source,
                note = "Top export customers excl. India and intercompany.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Top 10 suppliers from vw_Purchase_EBIDTA, excl. InterGroup='Intergroup'.
    /// </summary>
    [HttpGet("top-suppliers")]
    public async Task<IActionResult> GetTopSuppliers(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int top = 10,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetTopSuppliersAsync(company, from, to, top, refresh);
            return Ok(new
            {
                items = result.Items,
                company,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                top,
                source = result.Source,
                note = "Top suppliers excl. InterGroup='Intergroup'.",
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
