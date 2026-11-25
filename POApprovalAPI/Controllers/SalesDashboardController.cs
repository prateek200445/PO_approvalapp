using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Documents;
using POApprovalAPI.Services;
using QuestPDF.Fluent;

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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
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
            var data = await _salesDashboard.GetOverviewAsync(category, company, from, to, refresh, includeIntercompany);
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
                domesticCustomers = data.DomesticCustomers,
                suppliers = data.Suppliers,
                company,
                category,
                includeIntercompany,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("top10/pdf")]
    public IActionResult TopTenPdf([FromBody] SalesTopTenPdfRequest request)
    {
        // Custom-month export customers can be large; allow up to 5k rows per section (matches ranking ClampTop).
        const int maxRowsPerSection = 5_000;
        if (request?.Sections is null || request.Sections.Count is 0 or > 6)
            return BadRequest(new { message = "Nothing to export." });
        if (request.Sections.Any(s => s.Rows is null || s.Rows.Count > maxRowsPerSection))
            return BadRequest(new { message = $"Each PDF section can have at most {maxRowsPerSection:N0} rows." });
        try
        {
            var bytes = new SalesTopTenPdfDocument(request).GeneratePdf();
            var kind = string.Equals(request.Category, "Purchase", StringComparison.OrdinalIgnoreCase) ? "Purchase" : "Sales";
            return File(bytes, "application/pdf", $"{kind}_Top10_{request.DateFrom}_{request.DateTo}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Excel export for ranking sections (same payload as top10/pdf). Used by Export Customers → Excel.
    /// </summary>
    [HttpPost("top10/excel")]
    public IActionResult TopTenExcel([FromBody] SalesTopTenPdfRequest request)
    {
        const int maxRowsPerSection = 5_000;
        if (request?.Sections is null || request.Sections.Count is 0 or > 6)
            return BadRequest(new { message = "Nothing to export." });
        if (request.Sections.Any(s => s.Rows is null || s.Rows.Count > maxRowsPerSection))
            return BadRequest(new { message = $"Each Excel section can have at most {maxRowsPerSection:N0} rows." });
        try
        {
            var bytes = SalesTopTenExcelBuilder.Build(request);
            var kind = string.Equals(request.Category, "Purchase", StringComparison.OrdinalIgnoreCase) ? "Purchase" : "Sales";
            var fileName = $"{kind}_Top10_{request.DateFrom}_{request.DateTo}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var totals = await _salesDashboard.GetSalesTotalsAsync(company, from, to, refresh, includeIntercompany);

            return Ok(new
            {
                totalSales = totals.TotalSales,
                totalQuantity = totals.TotalQuantity,
                averageRate = totals.AverageRate,
                byGroup = totals.ByGroup,
                bySubGroup = totals.BySubGroup,
                company,
                includeIntercompany,
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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var totals = await _salesDashboard.GetPurchaseTotalsAsync(company, from, to, refresh, includeIntercompany);

            return Ok(new
            {
                totalPurchase = totals.TotalPurchase,
                totalQuantity = totals.TotalQuantity,
                averageRate = totals.AverageRate,
                byGroup = totals.ByGroup,
                bySubGroup = totals.BySubGroup,
                company,
                includeIntercompany,
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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
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
                ? await _salesDashboard.GetPurchaseYearlyTrendAsync(company, through, years, refresh, includeIntercompany)
                : await _salesDashboard.GetSalesYearlyTrendAsync(company, through, years, refresh, includeIntercompany);

            var source = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";
            var label = isPurchase ? "Purchase" : "Sales";

            return Ok(new
            {
                trend,
                company,
                category = label,
                includeIntercompany,
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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetSalesByCountryAsync(company, from, to, top, refresh, includeIntercompany);

            return Ok(new
            {
                byCountry = result.ByCountry,
                company,
                includeIntercompany,
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
    /// valueBasis=bill (default, Export analysis) → BillAMount; accessible → AccessableValue.
    /// </summary>
    [HttpGet("top-export-customers")]
    public async Task<IActionResult> GetTopExportCustomers(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int top = 10,
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false,
        [FromQuery] string valueBasis = "bill")
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var useAccessible = IsAccessibleValueBasis(valueBasis);
            var result = await _salesDashboard.GetTopExportCustomersAsync(
                company, from, to, top, refresh, includeIntercompany, useAccessible);
            return Ok(new
            {
                items = result.Items,
                company,
                includeIntercompany,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                top,
                valueBasis = useAccessible ? "accessible" : "bill",
                source = result.Source,
                note = useAccessible
                    ? (includeIntercompany
                        ? "Top export customers excl. India, including intercompany; amount = AccessableValue (assessable)."
                        : "Top export customers excl. India and intercompany; amount = AccessableValue (assessable).")
                    : (includeIntercompany
                        ? "Top export customers excl. India, including intercompany; amount = BillAMount."
                        : "Top export customers excl. India and intercompany; amount = BillAMount."),
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Top 10 domestic customers (India), excl. intercompany.
    /// valueBasis=bill (default, Export analysis) → BillAMount; accessible → AccessableValue.
    /// </summary>
    [HttpGet("top-domestic-customers")]
    public async Task<IActionResult> GetTopDomesticCustomers(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int top = 10,
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false,
        [FromQuery] string valueBasis = "bill")
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var useAccessible = IsAccessibleValueBasis(valueBasis);
            var result = await _salesDashboard.GetTopDomesticCustomersAsync(
                company, from, to, top, refresh, includeIntercompany, useAccessible);
            return Ok(new
            {
                items = result.Items,
                company,
                includeIntercompany,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                top,
                valueBasis = useAccessible ? "accessible" : "bill",
                source = result.Source,
                note = useAccessible
                    ? (includeIntercompany
                        ? "Top domestic (India) customers, including intercompany; amount = AccessableValue (assessable)."
                        : "Top domestic (India) customers, excl. intercompany; amount = AccessableValue (assessable).")
                    : (includeIntercompany
                        ? "Top domestic (India) customers, including intercompany; amount = BillAMount."
                        : "Top domestic (India) customers, excl. intercompany; amount = BillAMount."),
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
        [FromQuery] bool refresh = false,
        [FromQuery] bool includeIntercompany = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var result = await _salesDashboard.GetTopSuppliersAsync(company, from, to, top, refresh, includeIntercompany);
            return Ok(new
            {
                items = result.Items,
                company,
                includeIntercompany,
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                top,
                source = result.Source,
                note = includeIntercompany
                    ? "Top suppliers including InterGroup='Intergroup'."
                    : "Top suppliers excl. InterGroup='Intergroup'.",
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

    /// <summary>
    /// FIBC production month-on-month (BagPCS / BagWt) from VW_FIBCBagwiseProduction.
    /// </summary>
    [HttpGet("fibc-production-monthly")]
    public async Task<IActionResult> GetFibcProductionMonthly(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var from = dateFrom ?? new DateTime(DateTime.Today.Year, 4, 1);
            var to = dateTo ?? DateTime.Today;
            var data = await _salesDashboard.GetFibcProductionMonthlyAsync(company, from, to, refresh);
            return Ok(new
            {
                months = data.Months,
                byBagType = data.ByBagType,
                totalPcs = data.TotalPcs,
                totalWt = data.TotalWt,
                company,
                dateFrom = data.DateFrom,
                dateTo = data.DateTo,
                source = data.Source,
                note = "FIBC bag production month-on-month from VW_FIBCBagwiseProduction.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private static bool IsAccessibleValueBasis(string? valueBasis) =>
        !string.IsNullOrWhiteSpace(valueBasis)
        && (valueBasis.Equals("accessible", StringComparison.OrdinalIgnoreCase)
            || valueBasis.Equals("accessable", StringComparison.OrdinalIgnoreCase)
            || valueBasis.Equals("assessable", StringComparison.OrdinalIgnoreCase)
            || valueBasis.Equals("taxable", StringComparison.OrdinalIgnoreCase));
}
