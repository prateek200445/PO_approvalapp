using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Sales Dashboard from ERP financial source vw_ItemLedgerTransaction.
/// Progressive sections: kpis (loads+caches ledger), charts, tables (reuse cache).
/// </summary>
public class SalesDashboardService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();
    private static readonly string[] PartyColumnCandidates =
    {
        "LedgerName", "CustomerName", "PartyName", "SupplierName", "Customer", "Ledger", "Party",
        "AccountName", "AccName", "BuyerName", "VendorName", "Vendor", "Supplier", "ConsigneeName",
        "Consignee", "Particulars", "LedName", "Party_Name", "CustName",
    };
    private static readonly string[] CountryColumnCandidates =
    {
        "Country", "CountryName", "Country_Name", "BillingCountry", "ShipCountry",
        "MailingCountry", "LedCountry", "Nation", "CtryName", "Ctry",
    };
    private static readonly string[] CompanyColumnCandidates =
    {
        "CompanyName", "Company", "FactoryName", "Factory", "UnitName", "Unit",
    };
    private static readonly string[] PurchasePartyColumnCandidates =
    {
        "BuyerName", "SupplierName", "VendorName", "PartyName", "LedgerName", "AccountName",
    };
    private static readonly string[] DateColumnCandidates =
    {
        "invdate", "InvDate", "BillDate", "VoucherDate", "InvoiceDate", "SysDate", "Date",
    };
    private static readonly string[] AmountColumnCandidates =
    {
        "BillAMount", "BillAmount", "Amount", "TaxableAmount", "NetAmount", "InvoiceAmount",
    };
    private static readonly string[] SalesAmountColumnCandidates =
    {
        "Amount", "AccessableValue", "TaxableAmount", "BillAMount", "BillAmount", "NetAmount",
    };
    private static readonly TimeSpan ResultCacheTtl = TimeSpan.FromHours(4);
    private static readonly TimeSpan SupplierCacheTtl = TimeSpan.FromHours(4);
    private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim FactoryLock = new(1, 1);
    private const int QueryTimeoutSeconds = 90;
    private const int PartyQueryTimeoutSeconds = 180;

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private HashSet<string>? _ledgerColumns;
    private readonly ConcurrentDictionary<string, HashSet<string>> _viewColumns = new(StringComparer.OrdinalIgnoreCase);

    public SalesDashboardService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    private async Task<T> CachedAsync<T>(string key, bool refresh, Func<Task<T>> factory, TimeSpan? ttl = null)
        where T : class
    {
        if (!refresh && _cache.TryGetValue(key, out T? hit) && hit is not null)
            return hit;

        var gate = LoadLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (!refresh && _cache.TryGetValue(key, out hit) && hit is not null)
                return hit;

            var value = await factory();
            _cache.Set(key, value, ttl ?? ResultCacheTtl);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        using var connection = _database.CreateConnection();
        // ERP: select name from factoryinfo order by name
        var rows = await connection.QueryAsync<string>(
            @"SELECT Name
              FROM FactoryInfo WITH (NOLOCK)
              ORDER BY Name");
        return rows
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Company dropdown with FactoryInfo groups first (same G-{group} pattern as Ledger Summary).
    /// </summary>
    public Task<List<SalesCompanyOptionDto>> GetCompanyOptionsAsync() =>
        CachedAsync("sales-company-options", false, BuildCompanyOptionsCoreAsync, MetaCacheTtl);

    private async Task<List<SalesCompanyOptionDto>> BuildCompanyOptionsCoreAsync()
    {
        var factories = await GetFactoryRowsAsync();
        var salesNames = await GetSalesCompanyNamesAsync();
        var options = new List<SalesCompanyOptionDto>();

        var groups = factories
            .Select(f => f.GroupName)
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groups)
        {
            options.Add(new SalesCompanyOptionDto
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                Kind = "group",
            });
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var factory in factories.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!seen.Add(factory.Name)) continue;
            options.Add(new SalesCompanyOptionDto
            {
                Value = factory.Name,
                Label = factory.Name,
                Kind = "company",
            });
        }

        foreach (var name in salesNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            if (!seen.Add(name)) continue;
            options.Add(new SalesCompanyOptionDto
            {
                Value = name,
                Label = name,
                Kind = "company",
            });
        }

        return options;
    }

    /// <summary>
    /// Preload default All-Companies current-FY universe so every G-{group} slice is a cache hit.
    /// </summary>
    public async Task WarmDefaultCachesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var today = DateTime.Today;
        var fyStartYear = today.Month >= 4 ? today.Year : today.Year - 1;
        var from = new DateTime(fyStartYear, 4, 1);

        await GetCompanyOptionsAsync();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await GetOverviewAsync("Sales", "All Companies", from, today);
        }
        catch (Exception)
        {
            // First user request will load Sales if warmup fails.
        }
    }

    public async Task<SalesOverviewDto> GetOverviewAsync(
        string category,
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var universe = await GetOrLoadUniverseAsync(category, dateFrom, dateTo, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        var totals = AggregateTotals(universe.Leaves, category, slice, universe.ElapsedSeconds, includeIntercompany);
        var trend = AggregateTrend(universe.Trend, universe.TrendRanges, slice);

        if (isPurchase)
        {
            return new SalesOverviewDto
            {
                Totals = totals,
                Trend = trend,
            };
        }

        return new SalesOverviewDto
        {
            Totals = totals,
            Trend = trend,
            ByCountry = AggregateCountries(universe.Countries, slice, 10),
            CountryPeriodLabel = universe.CountryPeriodLabel,
            ExportCustomers = AggregateParties(universe.ExportCustomers, slice, 10),
        };
    }

    private static string NormalizeCategory(string category) =>
        category.Equals("Purchase", StringComparison.OrdinalIgnoreCase) ? "Purchase" : "Sales";

    private Task<List<EbidtaLeaf>> GetOrLoadLeavesAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh)
    {
        var selected = NormalizeCategory(category);
        var key = $"sales-leaves-v1:{selected}:{dateFrom:yyyy-MM-dd}:{dateTo:yyyy-MM-dd}";
        return CachedAsync(key, refresh, () => LoadEbidtaLeavesAsync(selected, dateFrom, dateTo));
    }

    private Task<TrendBundle> GetOrLoadTrendAsync(
        string category,
        DateTime asOf,
        int years,
        bool refresh,
        bool includeIntercompany = false)
    {
        years = Math.Clamp(years, 1, 8);
        var selected = NormalizeCategory(category);
        var icKey = includeIntercompany ? "ic" : "xic";
        var key = $"sales-trend-v1:{icKey}:{selected}:{asOf:yyyy-MM-dd}:{years}";
        return CachedAsync(key, refresh, async () =>
        {
            var (rows, ranges) = await LoadTrendLeavesAsync(selected, asOf, years, includeIntercompany);
            return new TrendBundle { Rows = rows, Ranges = ranges };
        });
    }

    private Task<GeoBundle> GetOrLoadGeoAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh,
        bool includeIntercompany = false)
    {
        var icKey = includeIntercompany ? "ic" : "xic";
        var key = $"sales-geo-v2-fibco:{icKey}:{dateFrom:yyyy-MM-dd}:{dateTo:yyyy-MM-dd}";
        return CachedAsync(key, refresh, async () =>
        {
            var invYears = GetInvYearsOverlapping(dateFrom, dateTo).ToList();
            var (countries, exportCustomers, geoSource) = await LoadSalesGeoUniverseAsync(
                dateFrom, dateTo, includeIntercompany);
            return new GeoBundle
            {
                Countries = countries,
                ExportCustomers = exportCustomers,
                Source = geoSource,
                InvYears = invYears,
                CountryPeriodLabel = FormatPeriodLabel(invYears),
            };
        });
    }

    private async Task<SalesUniverse> GetOrLoadUniverseAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh,
        bool includeIntercompany = false)
    {
        var selectedCategory = NormalizeCategory(category);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var leavesTask = GetLeavesForModeAsync(selectedCategory, dateFrom, dateTo, refresh, includeIntercompany);
        var trendTask = GetOrLoadTrendAsync(selectedCategory, dateTo, 5, refresh, includeIntercompany);

        if (selectedCategory == "Purchase")
        {
            await Task.WhenAll(leavesTask, trendTask);
            var trend = await trendTask;
            sw.Stop();
            return new SalesUniverse
            {
                Leaves = await leavesTask,
                Trend = trend.Rows,
                TrendRanges = trend.Ranges,
                ElapsedSeconds = sw.Elapsed.TotalSeconds,
            };
        }

        var geoTask = GetOrLoadGeoAsync(dateFrom, dateTo, refresh, includeIntercompany);
        await Task.WhenAll(leavesTask, trendTask, geoTask);
        var salesTrend = await trendTask;
        var geo = await geoTask;
        sw.Stop();
        return new SalesUniverse
        {
            Leaves = await leavesTask,
            Trend = salesTrend.Rows,
            TrendRanges = salesTrend.Ranges,
            Countries = geo.Countries,
            ExportCustomers = geo.ExportCustomers,
            CountryPeriodLabel = geo.CountryPeriodLabel,
            InvYears = geo.InvYears,
            CountrySource = geo.Source,
            ExportSource = geo.Source,
            ElapsedSeconds = sw.Elapsed.TotalSeconds,
        };
    }

    /// <summary>
    /// Total Sales + Quantity + Average Rate + byGroup/bySubGroup.
    /// Mirrors ERP SP_Sales_EBIDTA (aggregates vw_Sales_EBIDTA with the same GROUPING SETS),
    /// but excludes intercompany: InterGroup &lt;&gt; 'Intergroup'
    /// (vw_Sales_EBIDTA maps CommonLedgerMaster.IsInterCompany='yes' ? InterGroup='Intergroup').
    /// SP_Sales_EBIDTA itself has no IC filter / param ? country chart uses the same IC flag
    /// via vw_Countrywise_sales_dashboard (IsInterCompany != 'yes').
    /// </summary>
    public async Task<SalesTotalsDto> GetSalesTotalsAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var leaves = await GetLeavesForModeAsync("Sales", dateFrom, dateTo, refresh, includeIntercompany);
        sw.Stop();
        var slice = await ResolveSliceCompaniesAsync(company);
        return AggregateTotals(leaves, "Sales", slice, sw.Elapsed.TotalSeconds, includeIntercompany);
    }

    /// <summary>
    /// Bank Profile of Sales from portal country-wise MIS
    /// (vw_Countrywise_sales Amount; GroupName = legal company / FactoryInfo.Name).
    /// </summary>
    public async Task<SalesExportSplitDto> GetBankSalesSplitAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh = false)
    {
        var voucher = await LoadBankVoucherSplitAsync(company, dateFrom, dateTo);
        if (voucher != null && voucher.TotalSales > 0)
            return voucher;

        foreach (var view in new[] { "vw_Countrywise_sales", "vw_Countrywise_sales_dashboard" })
        {
            var loaded = await LoadCountrywiseBankSplitAsync(
                view,
                new[] { "Amount", "Value" },
                company,
                dateFrom,
                dateTo);
            if (loaded != null && loaded.TotalSales > 0)
                return loaded;
        }

        var split = await GetSalesExportSplitAsync(company, dateFrom, dateTo, refresh);
        var export = Math.Max(0, split.ExportSales);
        var domestic = Math.Max(0, split.DomesticSales) + Math.Max(0, split.IntercompanySales);
        return new SalesExportSplitDto
        {
            TotalSales = export + domestic,
            ExportSales = export,
            DomesticSales = domestic,
            IntercompanySales = split.IntercompanySales,
            Source = split.Source,
        };
    }

    private static bool IsIndiaCountry(string? country)
    {
        var value = (country ?? "").Trim().ToLowerInvariant();
        return value.Length == 0 || value is "india" or "in" or "ind" or "bharat" || value.Contains("india");
    }

    /// <summary>
    /// Bank totals from sales invoices: exclude InterUnit (stock/sister transfers),
    /// keep third-party domestic and export. Company slice is FactoryInfo group members.
    /// </summary>
    private async Task<SalesExportSplitDto?> LoadBankVoucherSplitAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        foreach (var viewName in new[] { "vw_Salesvoucher", "SalesVoucher" })
        {
            var loaded = await TryLoadBankVoucherSplitFromViewAsync(
                viewName, company, dateFrom, dateTo);
            if (loaded != null && loaded.TotalSales > 0)
                return loaded;
        }

        return null;
    }

    private async Task<SalesExportSplitDto?> TryLoadBankVoucherSplitFromViewAsync(
        string viewName,
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var cols = await GetViewColumnsAsync(connection, viewName);
        if (cols.Count == 0)
            return null;

        var dateCol = FirstExisting(cols, DateColumnCandidates);
        var amountCol = FirstExisting(cols, new[]
        {
            "TaxableAmount", "AccessableValue", "NetAmount", "BillAMount", "BillAmount", "Amount", "InvoiceAmount",
        });
        var companyCol = FirstExisting(cols, CompanyColumnCandidates);
        var partyCol = FirstExisting(cols, PurchasePartyColumnCandidates)
            ?? FirstExisting(cols, PartyColumnCandidates);
        var invTypeCol = FirstExisting(cols, new[] { "InvType", "InvoiceType", "VoucherType" });
        if (dateCol == null || amountCol == null || companyCol == null)
            return null;

        var masterCols = await GetViewColumnsAsync(connection, "CommonLedgerMaster");
        var countryCol = FirstExisting(masterCols, CountryColumnCandidates);

        var companySql = "";
        List<string> companyNames = [];
        if (!IsAllCompaniesSelection(company))
        {
            companyNames = NonEmptyInList(await ResolveSelectedCompaniesAsync(company));
            if (companyNames.Count == 0)
                return null;
            companySql = $"AND LTRIM(RTRIM(ISNULL(v.{Bracket(companyCol)}, N''))) IN @CompanyNames";
        }

        var interUnitSql = invTypeCol == null
            ? ""
            : $@"AND LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) NOT LIKE N'%interunit%'
  AND LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) NOT LIKE N'%inter unit%'
  AND LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) NOT LIKE N'%inter-unit%'
  AND LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) NOT LIKE N'%job%'
  AND LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) NOT LIKE N'%other sales%'";

        var sisterSql = partyCol == null
            ? ""
            : $@"
  AND (
        {PartyIsFibco($"v.{Bracket(partyCol)}")}
        OR NOT EXISTS (
            SELECT 1
            FROM dbo.FactoryInfo fi WITH (NOLOCK)
            WHERE fi.Name = v.{Bracket(partyCol)}
        )
  )";

        var joinLedger = "";
        var streamExpr = "N'Domestic'";
        if (partyCol != null && countryCol != null)
        {
            joinLedger = $@"
LEFT JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK)
    ON cm.LedgerName = v.{Bracket(partyCol)}";
            var exportInv = invTypeCol == null
                ? "1 = 0"
                : $"LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) LIKE N'%export%'";
            streamExpr = $@"CASE
            WHEN {exportInv}
              OR (
                    LTRIM(RTRIM(ISNULL(cm.{Bracket(countryCol)}, N''))) <> N''
                    AND LOWER(LTRIM(RTRIM(cm.{Bracket(countryCol)}))) NOT IN (N'india', N'in', N'ind', N'bharat')
                    AND LOWER(LTRIM(RTRIM(cm.{Bracket(countryCol)}))) NOT LIKE N'%india%'
                 )
                THEN N'Export'
            ELSE N'Domestic'
        END";
        }
        else if (invTypeCol != null)
        {
            streamExpr = $@"CASE
            WHEN LOWER(ISNULL(v.{Bracket(invTypeCol)}, N'')) LIKE N'%export%' THEN N'Export'
            ELSE N'Domestic'
        END";
        }

        var sql = $@"
SELECT
    {streamExpr} AS Country,
    SUM(CAST(v.{Bracket(amountCol)} AS float)) AS Amount
FROM dbo.{viewName} v WITH (NOLOCK)
{joinLedger}
WHERE v.{Bracket(dateCol)} BETWEEN @DateFrom AND @DateTo
  {companySql}
  {interUnitSql}
  {sisterSql}
GROUP BY {streamExpr}";

        try
        {
            var rows = (await connection.QueryAsync<CountryAmountRow>(
                sql,
                new
                {
                    DateFrom = dateFrom.Date,
                    DateTo = dateTo.Date,
                    CompanyNames = companyNames.Count == 0 ? new List<string> { "__none__" } : companyNames,
                },
                commandTimeout: QueryTimeoutSeconds)).ToList();

            var export = rows.Where(r => !IsIndiaCountry(r.Country) &&
                                         !r.Country.Equals("Domestic", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.Amount);
            var domestic = rows.Where(r => IsIndiaCountry(r.Country) ||
                                           r.Country.Equals("Domestic", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.Amount);
            if (export <= 0 && domestic <= 0)
                return null;

            return new SalesExportSplitDto
            {
                TotalSales = export + domestic,
                ExportSales = Math.Max(0, export),
                DomesticSales = Math.Max(0, domestic),
                IntercompanySales = 0,
                Source = $"{viewName} taxable excl InterUnit",
            };
        }
        catch (SqlException)
        {
            return null;
        }
    }

    private async Task<SalesExportSplitDto?> LoadCountrywiseBankSplitAsync(
        string viewName,
        IReadOnlyList<string> amountCandidates,
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var cols = await GetViewColumnsAsync(connection, viewName);
        if (cols.Count == 0)
            return null;

        var countryCol = FirstExisting(cols, CountryColumnCandidates);
        var valueCol = FirstExisting(cols, amountCandidates);
        var dateCol = FirstExisting(cols, new[] { "invdate", "InvDate", "InvoiceDate", "BillDate", "VoucherDate" });
        var invYearCol = FirstExisting(cols, new[] { "InvYear" });
        if (countryCol == null || valueCol == null)
            return null;

        var invYears = GetInvYearsOverlapping(dateFrom, dateTo).ToList();
        string periodSql;
        if (invYearCol != null && CanUseInvYearRange(dateFrom, dateTo))
        {
            periodSql = $"v.{Bracket(invYearCol)} IN @InvYears";
        }
        else if (dateCol != null)
        {
            periodSql = $"v.{Bracket(dateCol)} BETWEEN @DateFrom AND @DateTo";
        }
        else
        {
            return null;
        }

        var companySql = "";
        List<string> legalNames = [];
        if (!IsAllCompaniesSelection(company))
        {
            legalNames = NonEmptyInList(ResolveBankCountrywiseNames(company));
            if (legalNames.Count == 0)
                return null;
            // vw_Countrywise_sales.GroupName is the legal company name (e.g. Plastene India Limited),
            // not FactoryInfo.GroupName and not every unit suffix.
            companySql = "AND LTRIM(RTRIM(ISNULL(v.GroupName, N''))) IN @CompanyNames";
        }

        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(v.{Bracket(countryCol)}, N''))) AS Country,
    SUM(CAST(v.{Bracket(valueCol)} AS float)) AS Amount
FROM dbo.{viewName} v WITH (NOLOCK)
WHERE {periodSql}
  {companySql}
GROUP BY LTRIM(RTRIM(ISNULL(v.{Bracket(countryCol)}, N'')))";

        try
        {
            var rows = (await connection.QueryAsync<CountryAmountRow>(
                sql,
                new
                {
                    DateFrom = dateFrom.Date,
                    DateTo = dateTo.Date,
                    InvYears = invYears,
                    GroupNames = legalNames.Count == 0 ? new List<string> { "__none__" } : legalNames,
                    CompanyNames = legalNames.Count == 0 ? new List<string> { "__none__" } : legalNames,
                },
                commandTimeout: QueryTimeoutSeconds)).ToList();

            var export = rows.Where(r => !IsIndiaCountry(r.Country)).Sum(r => r.Amount);
            var domestic = rows.Where(r => IsIndiaCountry(r.Country)).Sum(r => r.Amount);
            if (export <= 0 && domestic <= 0)
                return null;

            return new SalesExportSplitDto
            {
                TotalSales = export + domestic,
                ExportSales = Math.Max(0, export),
                DomesticSales = Math.Max(0, domestic),
                IntercompanySales = 0,
                Source = viewName,
            };
        }
        catch (SqlException)
        {
            return null;
        }
    }

    private static bool CanUseInvYearRange(DateTime dateFrom, DateTime dateTo)
    {
        if (dateFrom.Month != 4 || dateFrom.Day != 1)
            return false;
        var fyEnd = new DateTime(dateFrom.Year + 1, 3, 31);
        if (dateTo.Date == fyEnd)
            return true;
        var today = DateTime.Today;
        var fyStart = today.Month >= 4 ? new DateTime(today.Year, 4, 1) : new DateTime(today.Year - 1, 4, 1);
        return dateFrom.Date == fyStart.Date && dateTo.Date <= fyEnd && dateTo.Date >= today.AddDays(-3);
    }

    private sealed class CountryAmountRow
    {
        public string Country { get; set; } = "";
        public double Amount { get; set; }
    }

    public async Task<SalesExportSplitDto> GetSalesExportSplitAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh = false)
    {
        var totalsTask = GetSalesTotalsAsync(company, dateFrom, dateTo, refresh);
        var geoTask = GetOrLoadGeoAsync(dateFrom, dateTo, refresh);
        var icTask = GetOrLoadIntercompanyLeavesAsync(dateFrom, dateTo, refresh);
        await Task.WhenAll(totalsTask, geoTask, icTask);
        var totals = await totalsTask;
        var geo = await geoTask;
        var icLeaves = await icTask;
        var slice = await ResolveSliceCompaniesAsync(company);
        var export = FilterByCompany(geo.ExportCustomers, slice, r => r.CompanyName).Sum(p => p.Amount);
        var intercompany = FilterByCompany(icLeaves, slice, r => r.CompanyName).Sum(r => r.Amount);
        if (export < 0) export = 0;
        if (export > totals.TotalSales) export = totals.TotalSales;
        if (intercompany < 0) intercompany = 0;
        return new SalesExportSplitDto
        {
            TotalSales = totals.TotalSales,
            ExportSales = export,
            DomesticSales = Math.Max(0, totals.TotalSales - export),
            IntercompanySales = intercompany,
            Source = string.IsNullOrWhiteSpace(geo.Source) ? "vw_Sales_EBIDTA" : geo.Source,
        };
    }

    /// <summary>
    /// Total Purchase + Quantity + Average Rate + byGroup/bySubGroup.
    /// Mirrors ERP SP_Purchase_EBIDTA (aggregates vw_Purchase_EBIDTA with the same GROUPING SETS),
    /// but excludes intercompany: InterGroup &lt;&gt; 'Intergroup'
    /// (vw_Purchase_EBIDTA maps CommonLedgerMaster.IsInterCompany='yes' ? InterGroup='Intergroup').
    /// </summary>
    public async Task<SalesTotalsDto> GetPurchaseTotalsAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var leaves = await GetLeavesForModeAsync("Purchase", dateFrom, dateTo, refresh, includeIntercompany);
        sw.Stop();
        var slice = await ResolveSliceCompaniesAsync(company);
        return AggregateTotals(leaves, "Purchase", slice, sw.Elapsed.TotalSeconds, includeIntercompany);
    }

    /// <summary>
    /// All-Companies EBIDTA leaves with CompanyName so G- groups can slice in memory.
    /// No TVP join — one scan serves every company group.
    /// </summary>
    private async Task<List<EbidtaLeaf>> LoadEbidtaLeavesAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var viewName = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";

        using var connection = _database.CreateConnection();
        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(CompanyName, N''))) AS CompanyName,
    InterGroup,
    Groupname,
    SubGroupName,
    ROUND(SUM(Amount), 0) AS Amount,
    ROUND(SUM(netwt), 0) AS Netwt
FROM dbo.{viewName} WITH (NOLOCK)
WHERE invdate BETWEEN @DateFrom AND @DateTo
  AND InterGroup <> N'Intergroup'
GROUP BY GROUPING SETS (
  (CompanyName, InterGroup, Groupname, SubGroupName)
)";

        var rows = await connection.QueryAsync<EbidtaLeaf>(
            sql,
            new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
            commandTimeout: QueryTimeoutSeconds);
        return rows.ToList();
    }

    private async Task<List<EbidtaLeaf>> GetLeavesForModeAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh,
        bool includeIntercompany)
    {
        var leaves = await GetOrLoadLeavesAsync(category, dateFrom, dateTo, refresh);
        if (!includeIntercompany)
            return leaves;

        var ic = await GetOrLoadIntercompanyLeavesAsync(category, dateFrom, dateTo, refresh);
        var combined = new List<EbidtaLeaf>(leaves.Count + ic.Count);
        combined.AddRange(leaves);
        combined.AddRange(ic);
        return combined;
    }

    private Task<List<EbidtaLeaf>> GetOrLoadIntercompanyLeavesAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh)
    {
        return GetOrLoadIntercompanyLeavesAsync("Sales", dateFrom, dateTo, refresh);
    }

    private Task<List<EbidtaLeaf>> GetOrLoadIntercompanyLeavesAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh)
    {
        var selected = NormalizeCategory(category);
        var key = $"sales-ic-leaves-v2:{selected}:{dateFrom:yyyy-MM-dd}:{dateTo:yyyy-MM-dd}";
        return CachedAsync(key, refresh, () => LoadIntercompanyLeavesAsync(selected, dateFrom, dateTo));
    }

    private async Task<List<EbidtaLeaf>> LoadIntercompanyLeavesAsync(
        string category,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var viewName = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<EbidtaLeaf>(
            $@"
SELECT
    LTRIM(RTRIM(ISNULL(CompanyName, N''))) AS CompanyName,
    InterGroup,
    Groupname,
    SubGroupName,
    ROUND(SUM(Amount), 0) AS Amount,
    ROUND(SUM(netwt), 0) AS Netwt
FROM dbo.{viewName} WITH (NOLOCK)
WHERE invdate BETWEEN @DateFrom AND @DateTo
  AND InterGroup = N'Intergroup'
GROUP BY GROUPING SETS (
  (CompanyName, InterGroup, Groupname, SubGroupName)
)",
            new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
            commandTimeout: QueryTimeoutSeconds);
        return rows.ToList();
    }

    /// <summary> Backward-compatible wrapper. </summary>
    public async Task<(double TotalSales, string SalesColumn, int RowCount, List<string> Columns, double ElapsedSeconds)> GetTotalSalesAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var t = await GetSalesTotalsAsync(company, dateFrom, dateTo);
        return (t.TotalSales, t.SalesColumn, t.RowCount, t.Columns, t.ElapsedSeconds);
    }

    /// <summary>
    /// Year-by-year Total Sales for Sales Trend chart.
    /// Each point = excl-IC Sales grand-total Amount (vw_Sales_EBIDTA, same as GetSalesTotalsAsync)
    /// for that Indian FY (Apr–Mar). Current FY is capped at <paramref name="asOf"/>.
    /// </summary>
    public async Task<List<SalesTrendDto>> GetSalesYearlyTrendAsync(
        string company,
        DateTime asOf,
        int years = 5,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        var bundle = await GetOrLoadTrendAsync("Sales", asOf, years, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        return TakeTrendYears(AggregateTrend(bundle.Rows, bundle.Ranges, slice), years);
    }

    /// <summary>
    /// Year-by-year Total Purchase for trend chart (vw_Purchase_EBIDTA).
    /// </summary>
    public async Task<List<SalesTrendDto>> GetPurchaseYearlyTrendAsync(
        string company,
        DateTime asOf,
        int years = 5,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        var bundle = await GetOrLoadTrendAsync("Purchase", asOf, years, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        return TakeTrendYears(AggregateTrend(bundle.Rows, bundle.Ranges, slice), years);
    }

    private async Task<(List<TrendLeaf> Rows, List<TrendRange> Ranges)> LoadTrendLeavesAsync(
        string category,
        DateTime asOf,
        int years,
        bool includeIntercompany = false)
    {
        years = Math.Clamp(years, 1, 8);
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var viewName = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";
        var ranges = BuildTrendRanges(asOf, years);
        if (ranges.Count == 0)
            return (new List<TrendLeaf>(), ranges);

        using var connection = _database.CreateConnection();
        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(v.CompanyName, N''))) AS CompanyName,
    CASE WHEN DATEPART(MONTH, v.invdate) >= 4 THEN YEAR(v.invdate) ELSE YEAR(v.invdate) - 1 END AS FyStart,
    ROUND(SUM(v.Amount), 0) AS Amount
FROM dbo.{viewName} v WITH (NOLOCK)
WHERE v.invdate BETWEEN @DateFrom AND @DateTo
  {(includeIntercompany ? "" : "AND v.InterGroup <> N'Intergroup'")}
GROUP BY LTRIM(RTRIM(ISNULL(v.CompanyName, N''))),
    CASE WHEN DATEPART(MONTH, v.invdate) >= 4 THEN YEAR(v.invdate) ELSE YEAR(v.invdate) - 1 END";

        var rows = (await connection.QueryAsync<TrendLeaf>(
            sql,
            new { DateFrom = ranges[0].From.Date, DateTo = asOf.Date },
            commandTimeout: QueryTimeoutSeconds)).ToList();
        return (rows, ranges);
    }

    /// <summary>
    /// Top export countries from SalesVoucher BillAmount (same grain as the country dashboard view).
    /// Excludes India and intercompany buyers (CommonLedgerMaster.IsInterCompany = yes).
    /// </summary>
    public async Task<SalesByCountryResultDto> GetSalesByCountryAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int top = 10,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        if (top <= 0)
            top = 10;
        top = Math.Clamp(top, 1, 100);
        var geo = await GetOrLoadGeoAsync(dateFrom, dateTo, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        return new SalesByCountryResultDto
        {
            ByCountry = AggregateCountries(geo.Countries, slice, top),
            InvYears = geo.InvYears,
            PeriodLabel = geo.CountryPeriodLabel,
            Source = geo.Source,
        };
    }

    /// <summary>
    /// One All-Companies voucher/EBIDTA scan with CompanyName. Country pie and export
    /// customers are both derived in memory after a group slice.
    /// </summary>
    private async Task<(List<CountryLeaf> Countries, List<PartyLeaf> ExportCustomers, string Source)> LoadSalesGeoUniverseAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany = false)
    {
        var fromVoucher = await TryLoadExportPartyLeavesFromVoucherAsync(dateFrom, dateTo, includeIntercompany);
        var source = fromVoucher != null ? "SalesVoucher" : "vw_Sales_EBIDTA";
        var parties = fromVoucher ?? await LoadExportPartyLeavesFromEbidtaAsync(dateFrom, dateTo, includeIntercompany);
        var countries = parties
            .GroupBy(
                p => (Company: p.CompanyName, Country: string.IsNullOrWhiteSpace(p.Country) ? "Unknown" : p.Country.Trim()),
                t => t.Amount)
            .Select(g => new CountryLeaf
            {
                CompanyName = g.Key.Company,
                CountryName = g.Key.Country,
                SalesAmount = g.Sum(),
            })
            .ToList();
        return (countries, parties, source);
    }

    private async Task<List<PartyLeaf>?> TryLoadExportPartyLeavesFromVoucherAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany = false)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var cols = await GetViewColumnsAsync(connection, "SalesVoucher");
        var partyCol = FirstExisting(cols, PurchasePartyColumnCandidates);
        var dateCol = FirstExisting(cols, DateColumnCandidates);
        var amountCol = FirstExisting(cols, AmountColumnCandidates);
        var companyCol = FirstExisting(cols, CompanyColumnCandidates);
        var masterCols = await GetViewColumnsAsync(connection, "CommonLedgerMaster");
        var countryCol = FirstExisting(masterCols, CountryColumnCandidates);
        var icCol = FirstExisting(masterCols, new[] { "IsInterCompany" });
        if (partyCol == null || dateCol == null || amountCol == null || countryCol == null || companyCol == null)
            return null;

        var partySql = Bracket(partyCol);
        var dateSql = Bracket(dateCol);
        var amountSql = Bracket(amountCol);
        var companySql = Bracket(companyCol);
        var countrySql = Bracket(countryCol);
        var countryExpr = $@"CASE
            WHEN LOWER(LTRIM(RTRIM(cm.{countrySql}))) IN (N'india', N'in', N'ind', N'bharat')
              OR LOWER(LTRIM(RTRIM(cm.{countrySql}))) LIKE N'%india%'
                THEN N'India'
            ELSE UPPER(LTRIM(RTRIM(cm.{countrySql})))
        END";
        var icFilter = includeIntercompany || icCol == null
            ? ""
            : $"AND {OrFibcoParty($"pv.{partySql}", InterCompanyNotYes($"cm.{Bracket(icCol)}"))}";
        var sisterFilter = includeIntercompany
            ? ""
            : $@"
  AND (
        {PartyIsFibco($"pv.{partySql}")}
        OR NOT EXISTS (
            SELECT 1
            FROM dbo.FactoryInfo fi WITH (NOLOCK)
            WHERE fi.Name = pv.{partySql}
        )
  )";

        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(pv.{companySql}, N''))) AS CompanyName,
    LTRIM(RTRIM(pv.{partySql})) AS Name,
    MAX({countryExpr}) AS Country,
    ROUND(SUM(CAST(pv.{amountSql} AS float)), 0) AS Amount
FROM dbo.SalesVoucher pv WITH (NOLOCK)
INNER JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK)
    ON cm.LedgerName = pv.{partySql}
WHERE pv.{dateSql} BETWEEN @DateFrom AND @DateTo
  AND pv.{partySql} IS NOT NULL
  AND pv.{partySql} <> N''
  AND LTRIM(RTRIM(ISNULL(cm.{countrySql}, N''))) <> N''
  AND {OrFibcoParty($"pv.{partySql}", ExportCountryPredicate($"cm.{countrySql}"))}
  {icFilter}
  {sisterFilter}
GROUP BY LTRIM(RTRIM(ISNULL(pv.{companySql}, N''))), LTRIM(RTRIM(pv.{partySql}))";

        try
        {
            var rows = await connection.QueryAsync<PartyLeaf>(
                sql,
                new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
                commandTimeout: QueryTimeoutSeconds);
            return rows.ToList();
        }
        catch (SqlException)
        {
            return null;
        }
    }

    private async Task<List<PartyLeaf>> LoadExportPartyLeavesFromEbidtaAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany = false)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var ebidtaCols = await GetViewColumnsAsync(connection, "vw_Sales_EBIDTA");
        var partyCol = FirstExisting(ebidtaCols, PartyColumnCandidates)
            ?? FirstExisting(ebidtaCols, PurchasePartyColumnCandidates);
        var ebidtaCountry = FirstExisting(ebidtaCols, CountryColumnCandidates);
        var masterCols = await GetViewColumnsAsync(connection, "CommonLedgerMaster");
        var ledgerCountry = FirstExisting(masterCols, CountryColumnCandidates);
        var icCol = FirstExisting(masterCols, new[] { "IsInterCompany" });
        if (partyCol == null)
            return new List<PartyLeaf>();

        var partySql = Bracket(partyCol);
        string countryExpr;
        var joinLedger = "";
        var extraIc = "";
        if (ebidtaCountry != null)
        {
            countryExpr = $@"CASE
                WHEN LOWER(LTRIM(RTRIM(v.{Bracket(ebidtaCountry)}))) IN (N'india', N'in', N'ind', N'bharat')
                  OR LOWER(LTRIM(RTRIM(v.{Bracket(ebidtaCountry)}))) LIKE N'%india%'
                    THEN N'India'
                ELSE UPPER(LTRIM(RTRIM(v.{Bracket(ebidtaCountry)})))
            END";
            extraIc += $@"
  AND LTRIM(RTRIM(ISNULL(v.{Bracket(ebidtaCountry)}, N''))) <> N''
  AND {OrFibcoParty($"v.{partySql}", ExportCountryPredicate("v." + Bracket(ebidtaCountry)))}";
        }
        else if (ledgerCountry != null)
        {
            countryExpr = $@"CASE
                WHEN LOWER(LTRIM(RTRIM(m.{Bracket(ledgerCountry)}))) IN (N'india', N'in', N'ind', N'bharat')
                  OR LOWER(LTRIM(RTRIM(m.{Bracket(ledgerCountry)}))) LIKE N'%india%'
                    THEN N'India'
                ELSE UPPER(LTRIM(RTRIM(m.{Bracket(ledgerCountry)})))
            END";
            joinLedger = $@"
INNER JOIN CommonLedgerMaster m WITH (NOLOCK)
    ON m.LedgerName = v.{partySql}";
            extraIc = icCol == null || includeIntercompany
                ? $@"AND {OrFibcoParty($"v.{partySql}", ExportCountryPredicate($"m.{Bracket(ledgerCountry)}"))}"
                : $"AND {OrFibcoParty($"v.{partySql}", InterCompanyNotYes($"m.{Bracket(icCol)}"))} AND {OrFibcoParty($"v.{partySql}", ExportCountryPredicate($"m.{Bracket(ledgerCountry)}"))}";
        }
        else
        {
            return new List<PartyLeaf>();
        }

        var sisterFilter = includeIntercompany
            ? ""
            : $@"
  AND (
        {PartyIsFibco($"v.{partySql}")}
        OR NOT EXISTS (
            SELECT 1 FROM dbo.FactoryInfo fi WITH (NOLOCK)
            WHERE fi.Name = v.{partySql}
        )
  )";

        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(v.CompanyName, N''))) AS CompanyName,
    LTRIM(RTRIM(v.{partySql})) AS Name,
    MAX({countryExpr}) AS Country,
    ROUND(SUM(v.Amount), 0) AS Amount
FROM dbo.vw_Sales_EBIDTA v WITH (NOLOCK)
{joinLedger}
WHERE v.invdate BETWEEN @DateFrom AND @DateTo
  {(includeIntercompany ? "" : $"AND (v.InterGroup <> N'Intergroup' OR {PartyIsFibco($"v.{partySql}")})")}
  AND v.{partySql} IS NOT NULL
  AND v.{partySql} <> N''
  {extraIc}
  {sisterFilter}
GROUP BY LTRIM(RTRIM(ISNULL(v.CompanyName, N''))), LTRIM(RTRIM(v.{partySql}))";

        var rows = await connection.QueryAsync<PartyLeaf>(
            sql,
            new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
            commandTimeout: QueryTimeoutSeconds);
        return rows.ToList();
    }

    /// <summary>
    /// Top export customers (non-India), excl. intercompany.
    /// </summary>
    public async Task<RankedPartyResultDto> GetTopExportCustomersAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int top = 5,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        top = ClampTop(top);
        var geo = await GetOrLoadGeoAsync(dateFrom, dateTo, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        return ToRankedResult(AggregateParties(geo.ExportCustomers, slice, top), geo.Source, "", "");
    }

    /// <summary>
    /// Top suppliers from PurchaseVoucher (fast). Falls back to vw_Purchase_EBIDTA.
    /// </summary>
    public async Task<RankedPartyResultDto> GetTopSuppliersAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int top = 5,
        bool refresh = false,
        bool includeIntercompany = false)
    {
        top = ClampTop(top);
        var leaves = await GetOrLoadSupplierUniverseAsync(dateFrom, dateTo, refresh, includeIntercompany);
        var slice = await ResolveSliceCompaniesAsync(company);
        return ToRankedResult(AggregateParties(leaves, slice, top), "PurchaseVoucher", "", null);
    }

    private Task<List<PartyLeaf>> GetOrLoadSupplierUniverseAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh,
        bool includeIntercompany = false)
    {
        var icKey = includeIntercompany ? "ic" : "xic";
        var key = $"sales-suppliers-universe-v1:{icKey}:{dateFrom:yyyy-MM-dd}:{dateTo:yyyy-MM-dd}";
        return CachedAsync(key, refresh, () => LoadSupplierLeavesAsync(dateFrom, dateTo, includeIntercompany), SupplierCacheTtl);
    }

    private async Task<List<PartyLeaf>> LoadSupplierLeavesAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany)
    {
        var fromVoucher = await TryLoadSupplierLeavesFromVoucherAsync(dateFrom, dateTo, includeIntercompany);
        if (fromVoucher != null)
            return fromVoucher;
        return await LoadSupplierLeavesFromEbidtaAsync(dateFrom, dateTo, includeIntercompany);
    }

    private async Task<List<PartyLeaf>?> TryLoadSupplierLeavesFromVoucherAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany = false)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var cols = await GetViewColumnsAsync(connection, "PurchaseVoucher");
        var partyCol = FirstExisting(cols, PurchasePartyColumnCandidates);
        var dateCol = FirstExisting(cols, DateColumnCandidates);
        var amountCol = FirstExisting(cols, AmountColumnCandidates);
        var companyCol = FirstExisting(cols, CompanyColumnCandidates);
        if (partyCol == null || dateCol == null || amountCol == null || companyCol == null)
            return null;

        var masterCols = await GetViewColumnsAsync(connection, "CommonLedgerMaster");
        var icCol = FirstExisting(masterCols, new[] { "IsInterCompany" });
        var voucherIc = FirstExisting(cols, new[] { "InterGroup", "IsInterCompany" });
        var partySql = Bracket(partyCol);
        var dateSql = Bracket(dateCol);
        var amountSql = Bracket(amountCol);
        var companySql = Bracket(companyCol);

        string icJoin = "";
        string icFilter;
        if (icCol != null)
        {
            icJoin = $@"
INNER JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK)
    ON cm.LedgerName = pv.{partySql}";
            icFilter = "AND ISNULL(cm.IsInterCompany, N'') <> N'yes'";
        }
        else if (voucherIc != null && voucherIc.Equals("InterGroup", StringComparison.OrdinalIgnoreCase))
        {
            icFilter = $"AND pv.{Bracket(voucherIc)} <> N'Intergroup'";
        }
        else if (voucherIc != null)
        {
            icFilter = $"AND ISNULL(pv.{Bracket(voucherIc)}, N'') <> N'yes'";
        }
        else
        {
            icFilter = "";
        }

        if (includeIntercompany)
            icFilter = "";

        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(pv.{companySql}, N''))) AS CompanyName,
    MAX(pv.{partySql}) AS Name,
    CAST(NULL AS nvarchar(200)) AS Country,
    ROUND(SUM(CAST(pv.{amountSql} AS float)), 0) AS Amount
FROM dbo.PurchaseVoucher pv WITH (NOLOCK)
{icJoin}
WHERE pv.{dateSql} BETWEEN @DateFrom AND @DateTo
  AND pv.{partySql} IS NOT NULL
  AND pv.{partySql} <> N''
  {icFilter}
GROUP BY LTRIM(RTRIM(ISNULL(pv.{companySql}, N''))), pv.{partySql}";

        try
        {
            var rows = await connection.QueryAsync<PartyLeaf>(
                sql,
                new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
                commandTimeout: QueryTimeoutSeconds);
            return rows.ToList();
        }
        catch (SqlException)
        {
            return null;
        }
    }

    private async Task<List<PartyLeaf>> LoadSupplierLeavesFromEbidtaAsync(
        DateTime dateFrom,
        DateTime dateTo,
        bool includeIntercompany = false)
    {
        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var cols = await GetViewColumnsAsync(connection, "vw_Purchase_EBIDTA");
        var partyCol = FirstExisting(cols, PurchasePartyColumnCandidates) ?? FirstExisting(cols, PartyColumnCandidates);
        if (partyCol == null)
            return new List<PartyLeaf>();

        var partySql = Bracket(partyCol);
        var sql = $@"
SELECT
    LTRIM(RTRIM(ISNULL(v.CompanyName, N''))) AS CompanyName,
    MAX(v.{partySql}) AS Name,
    CAST(NULL AS nvarchar(200)) AS Country,
    ROUND(SUM(v.Amount), 0) AS Amount
FROM dbo.vw_Purchase_EBIDTA v WITH (NOLOCK)
WHERE v.invdate BETWEEN @DateFrom AND @DateTo
  {(includeIntercompany ? "" : "AND v.InterGroup <> N'Intergroup'")}
  AND v.{partySql} IS NOT NULL
  AND v.{partySql} <> N''
GROUP BY LTRIM(RTRIM(ISNULL(v.CompanyName, N''))), v.{partySql}";

        var rows = await connection.QueryAsync<PartyLeaf>(
            sql,
            new { DateFrom = dateFrom.Date, DateTo = dateTo.Date },
            commandTimeout: PartyQueryTimeoutSeconds);
        return rows.ToList();
    }

    public Task<HashSet<string>?> ResolveCompanySliceAsync(string company) =>
        ResolveSliceCompaniesAsync(company);

    public bool CompanyBelongsToSlice(string companyName, HashSet<string>? slice)
    {
        if (slice == null) return true;
        if (slice.Count == 0) return false;
        var name = companyName ?? "";
        if (slice.Contains(name)) return true;
        foreach (var selected in slice)
        {
            if (CompanyKeysMatch(selected, name)) return true;
        }
        return false;
    }

    private async Task<HashSet<string>?> ResolveSliceCompaniesAsync(string company)
    {
        if (IsAllCompaniesSelection(company))
            return null;

        var names = await ResolveSelectedCompaniesAsync(company);
        return names.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<T> FilterByCompany<T>(
        IEnumerable<T> rows,
        HashSet<string>? companies,
        Func<T, string> companyOf)
    {
        if (companies == null)
            return rows;
        if (companies.Count == 0)
            return Array.Empty<T>();
        return rows.Where(r =>
        {
            var name = companyOf(r) ?? "";
            if (companies.Contains(name)) return true;
            foreach (var selected in companies)
            {
                if (CompanyKeysMatch(selected, name)) return true;
            }
            return false;
        });
    }

    private static SalesTotalsDto AggregateTotals(
        IReadOnlyList<EbidtaLeaf> leaves,
        string category,
        HashSet<string>? companies,
        double elapsedSeconds,
        bool includeIntercompany = false)
    {
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var filtered = FilterByCompany(leaves, companies, r => r.CompanyName);
        var rows = includeIntercompany
            ? filtered.ToList()
            : filtered.Where(r => !r.InterGroup.Equals("Intergroup", StringComparison.OrdinalIgnoreCase)).ToList();

        var amount = rows.Sum(r => r.Amount);
        var qty = rows.Sum(r => r.Netwt);
        var rate = qty > 0 ? amount / qty : 0;

        var groupMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var subMap = new Dictionary<string, (double Amount, double Qty)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var group = (row.Groupname ?? "").Trim();
            var sub = (row.SubGroupName ?? "").Trim();
            if (group.Length > 0)
            {
                groupMap.TryGetValue(group, out var gAmt);
                groupMap[group] = gAmt + row.Amount;
            }
            if (sub.Length > 0)
            {
                subMap.TryGetValue(sub, out var s);
                subMap[sub] = (s.Amount + row.Amount, s.Qty + row.Netwt);
            }
        }

        var groupTotal = groupMap.Values.Sum();
        return new SalesTotalsDto
        {
            TotalSales = isPurchase ? 0 : amount,
            TotalPurchase = isPurchase ? amount : 0,
            TotalQuantity = qty,
            AverageRate = rate,
            SalesColumn = "Amount",
            QuantityColumn = "Netwt",
            RateColumn = "PerKg",
            Method = includeIntercompany
                ? $"{category}_UniverseSlice_InclIntercompany"
                : $"{category}_UniverseSlice_ExclIntercompany",
            RowCount = rows.Count,
            Columns = new List<string> { "CompanyName", "InterGroup", "Groupname", "SubGroupName", "Amount", "Netwt" },
            ElapsedSeconds = elapsedSeconds,
            ByGroup = groupMap
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new SalesByGroupDto
                {
                    GroupName = kv.Key,
                    Amount = kv.Value,
                    Percentage = groupTotal > 0 ? Math.Round(kv.Value / groupTotal * 100, 1) : 0,
                })
                .ToList(),
            BySubGroup = subMap
                .OrderByDescending(kv => kv.Value.Amount)
                .Select(kv => new SalesBySubGroupDto
                {
                    SubGroupName = kv.Key,
                    SalesAmount = kv.Value.Amount,
                    Quantity = kv.Value.Qty,
                })
                .ToList(),
        };
    }

    private static List<SalesTrendDto> AggregateTrend(
        IReadOnlyList<TrendLeaf> leaves,
        IReadOnlyList<TrendRange> ranges,
        HashSet<string>? companies)
    {
        var rows = FilterByCompany(leaves, companies, r => r.CompanyName);
        var byFy = rows
            .GroupBy(r => r.FyStart)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        return ranges
            .Select(r => new SalesTrendDto
            {
                Period = r.Period,
                Amount = byFy.TryGetValue(r.StartYear, out var amount) ? amount : 0,
            })
            .ToList();
    }

    private static List<SalesTrendDto> TakeTrendYears(List<SalesTrendDto> trend, int years)
    {
        years = Math.Clamp(years, 1, 8);
        return trend.Count <= years ? trend : trend.Skip(trend.Count - years).ToList();
    }

    private static List<TrendRange> BuildTrendRanges(DateTime asOf, int years)
    {
        var currentFyStartYear = asOf.Month >= 4 ? asOf.Year : asOf.Year - 1;
        var ranges = new List<TrendRange>();
        for (var i = years - 1; i >= 0; i--)
        {
            var startYear = currentFyStartYear - i;
            var from = new DateTime(startYear, 4, 1);
            if (from > asOf)
                continue;

            var to = new DateTime(startYear + 1, 3, 31);
            if (to > asOf)
                to = asOf;

            ranges.Add(new TrendRange
            {
                StartYear = startYear,
                Period = $"FY {startYear % 100:D2}-{(startYear + 1) % 100:D2}",
                From = from,
                To = to,
            });
        }

        return ranges;
    }

    private static List<SalesByCountryDto> AggregateCountries(
        IReadOnlyList<CountryLeaf> leaves,
        HashSet<string>? companies,
        int top)
    {
        return FilterByCompany(leaves, companies, r => r.CompanyName)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.CountryName) ? "Unknown" : r.CountryName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesByCountryDto
            {
                CountryName = g.First().CountryName.Trim().Length == 0 ? "Unknown" : g.First().CountryName.Trim(),
                SalesAmount = g.Sum(x => x.SalesAmount),
            })
            .OrderByDescending(r => r.SalesAmount)
            .Take(top)
            .Select((r, i) =>
            {
                r.Rank = i + 1;
                return r;
            })
            .ToList();
    }

    private static List<RankedPartyDto> AggregateParties(
        IReadOnlyList<PartyLeaf> leaves,
        HashSet<string>? companies,
        int top)
    {
        return FilterByCompany(leaves, companies, r => r.CompanyName)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Name) ? "Unknown" : r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var country = g
                    .Select(x => x.Country)
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
                return new RankedPartyDto
                {
                    Name = g.Key,
                    Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
                    Amount = g.Sum(x => x.Amount),
                };
            })
            .OrderByDescending(r => r.Amount)
            .Take(top)
            .Select((r, i) =>
            {
                r.Rank = i + 1;
                return r;
            })
            .ToList();
    }

    /// <summary>Indian FY labels (yy-yy+1) that overlap [dateFrom, dateTo].</summary>
    private static IEnumerable<string> GetInvYearsOverlapping(DateTime dateFrom, DateTime dateTo)
    {
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var years = new SortedSet<string>(StringComparer.Ordinal);
        var cursor = new DateTime(dateFrom.Year, dateFrom.Month, 1);
        var end = new DateTime(dateTo.Year, dateTo.Month, 1);
        while (cursor <= end)
        {
            years.Add(ToInvYearLabel(cursor));
            cursor = cursor.AddMonths(1);
        }

        return years;
    }

    private static string ToInvYearLabel(DateTime date)
    {
        var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{startYear % 100:D2}-{(startYear + 1) % 100:D2}";
    }

    /// <summary>e.g. "FY 25-26" or "FY 24-25, FY 25-26" from InvYear labels.</summary>
    private static string FormatPeriodLabel(IReadOnlyList<string> invYears)
    {
        if (invYears == null || invYears.Count == 0)
            return "";
        return string.Join(", ", invYears.Select(y => $"FY {y}"));
    }

    /// <summary>
    /// Prefer SP grand-total row (Column1=Sales|Purchase, blank InterGroup+Groupname).
    /// Amount = category total; Netwt = Total Quantity; PerKg = Average Rate.
    /// Fallback: sum detail Amount / Netwt; rate = ERP PerKg on single grand row,
    /// or Amount/Netwt when multiple / detail-only (do not sum PerKg).
    /// </summary>
    private static (double Amount, double Quantity, double AverageRate, string Method) ResolveEbidtaGrandTotals(
        DataTable table,
        DataColumn amountCol,
        DataColumn? qtyCol,
        DataColumn? rateCol,
        string categoryLabel)
    {
        var hasColumn1 = table.Columns.Contains("Column1");
        var hasInterGroup = table.Columns.Contains("InterGroup");
        var hasGroupname = table.Columns.Contains("Groupname");

        double grandAmount = 0;
        double grandQty = 0;
        double lastPerKg = 0;
        var grandRowCount = 0;

        foreach (DataRow row in table.Rows)
        {
            if (IsTotalLabelRow(row))
                continue;

            var col1 = hasColumn1 ? (Convert.ToString(row["Column1"]) ?? "") : categoryLabel;
            if (!col1.Contains(categoryLabel, StringComparison.OrdinalIgnoreCase))
                continue;

            var inter = hasInterGroup ? (Convert.ToString(row["InterGroup"]) ?? "") : "";
            var group = hasGroupname ? (Convert.ToString(row["Groupname"]) ?? "") : "";

            // ERP category block grand total row
            if (string.IsNullOrWhiteSpace(inter) && string.IsNullOrWhiteSpace(group))
            {
                grandAmount += ToDouble(row[amountCol]);
                if (qtyCol != null)
                    grandQty += ToDouble(row[qtyCol]);
                if (rateCol != null)
                    lastPerKg = ToDouble(row[rateCol]);
                grandRowCount++;
            }
        }

        if (grandRowCount > 0)
        {
            // One grand-total row ? use ERP PerKg as-is. Multiple ? weighted Amount/Netwt.
            var rate = grandRowCount == 1 && rateCol != null
                ? lastPerKg
                : (grandQty > 0 ? grandAmount / grandQty : 0);
            return (grandAmount, grandQty, rate, $"{categoryLabel}_GrandTotal_Row");
        }

        // Fallback: sum detail lines only (Groupname not blank)
        double detailAmount = 0;
        double detailQty = 0;
        foreach (DataRow row in table.Rows)
        {
            if (IsTotalLabelRow(row) || IsEbidtaHeaderRow(row))
                continue;

            if (hasColumn1)
            {
                var col1 = Convert.ToString(row["Column1"]) ?? "";
                if (!col1.Contains(categoryLabel, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            detailAmount += ToDouble(row[amountCol]);
            if (qtyCol != null)
                detailQty += ToDouble(row[qtyCol]);
        }

        var detailRate = detailQty > 0 ? detailAmount / detailQty : 0;
        return (detailAmount, detailQty, detailRate, $"Sum_{categoryLabel}_Detail");
    }

    /// <summary>
    /// Aggregate Amount by Groupname / SubGroupName from leaf category rows only
    /// (SubGroupName filled). Avoids double-counting group header / grand-total rows.
    /// Fallback: Groupname-filled rows excluding blank InterGroup+Groupname grand total.
    /// </summary>
    private static (List<SalesByGroupDto> ByGroup, List<SalesBySubGroupDto> BySubGroup) BuildEbidtaBreakdowns(
        DataTable table,
        DataColumn amountCol,
        DataColumn? qtyCol,
        string categoryLabel)
    {
        var hasColumn1 = table.Columns.Contains("Column1");
        var hasInterGroup = table.Columns.Contains("InterGroup");
        var hasGroupname = table.Columns.Contains("Groupname");
        var hasSubGroup = table.Columns.Contains("SubGroupName");

        var leafRows = new List<DataRow>();
        var groupOnlyRows = new List<DataRow>();

        foreach (DataRow row in table.Rows)
        {
            if (IsTotalLabelRow(row))
                continue;

            if (hasColumn1)
            {
                var col1 = Convert.ToString(row["Column1"]) ?? "";
                if (!col1.Contains(categoryLabel, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var inter = hasInterGroup ? (Convert.ToString(row["InterGroup"]) ?? "") : "";
            var group = hasGroupname ? (Convert.ToString(row["Groupname"]) ?? "") : "";
            var sub = hasSubGroup ? (Convert.ToString(row["SubGroupName"]) ?? "") : "";

            // Defensive: never include IC (ERP InterGroup='Intergroup') in breakdowns
            if (inter.Equals("Intergroup", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip category grand-total / InterGroup-only subtotal rows
            if (string.IsNullOrWhiteSpace(inter) && string.IsNullOrWhiteSpace(group))
                continue;
            if (!string.IsNullOrWhiteSpace(inter) && string.IsNullOrWhiteSpace(group) &&
                string.IsNullOrWhiteSpace(sub))
                continue;

            if (!string.IsNullOrWhiteSpace(sub))
                leafRows.Add(row);
            else if (!string.IsNullOrWhiteSpace(group))
                groupOnlyRows.Add(row);
        }

        var sourceRows = leafRows.Count > 0 ? leafRows : groupOnlyRows;

        var groupMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var subMap = new Dictionary<string, (double Amount, double Qty)>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sourceRows)
        {
            var amount = ToDouble(row[amountCol]);
            var qty = qtyCol != null ? ToDouble(row[qtyCol]) : 0;

            if (hasGroupname)
            {
                var group = (Convert.ToString(row["Groupname"]) ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(group))
                {
                    groupMap.TryGetValue(group, out var gAmt);
                    groupMap[group] = gAmt + amount;
                }
            }

            if (hasSubGroup)
            {
                var sub = (Convert.ToString(row["SubGroupName"]) ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(sub))
                {
                    subMap.TryGetValue(sub, out var s);
                    subMap[sub] = (s.Amount + amount, s.Qty + qty);
                }
            }
            else if (hasGroupname)
            {
                // No subgroup column ? expose group as subgroup fallback
                var group = (Convert.ToString(row["Groupname"]) ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(group))
                {
                    subMap.TryGetValue(group, out var s);
                    subMap[group] = (s.Amount + amount, s.Qty + qty);
                }
            }
        }

        var groupTotal = groupMap.Values.Sum();
        var byGroup = groupMap
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new SalesByGroupDto
            {
                GroupName = kv.Key,
                Amount = kv.Value,
                Percentage = groupTotal > 0
                    ? Math.Round(kv.Value / groupTotal * 100, 1)
                    : 0,
            })
            .ToList();

        var bySubGroup = subMap
            .OrderByDescending(kv => kv.Value.Amount)
            .Select(kv => new SalesBySubGroupDto
            {
                SubGroupName = kv.Key,
                SalesAmount = kv.Value.Amount,
                Quantity = kv.Value.Qty,
            })
            .ToList();

        return (byGroup, bySubGroup);
    }

    private static DataColumn? ResolveQuantityColumn(DataTable table)
    {
        // SP_Sales_EBIDTA uses Netwt for quantity (kg)
        string[] preferred = { "Netwt", "NetWt", "Qty", "Quantity", "Net Weight" };
        foreach (var name in preferred)
        {
            foreach (DataColumn col in table.Columns)
            {
                if (string.Equals(col.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
        }

        return null;
    }

    private static DataColumn? ResolveRateColumn(DataTable table)
    {
        // SP_Sales_EBIDTA uses PerKg for average rate (?/kg)
        string[] preferred = { "PerKg", "Per Kg", "AvgRate", "AverageRate", "Rate" };
        foreach (var name in preferred)
        {
            foreach (DataColumn col in table.Columns)
            {
                if (string.Equals(col.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
        }

        return null;
    }

    private static double ToDouble(object? value)
    {
        if (value == null || value is DBNull) return 0;
        return double.TryParse(Convert.ToString(value), out var n) ? n : 0;
    }

    private static DataColumn? ResolveSalesColumn(DataTable table)
    {
        // SP_Sales_EBIDTA confirmed columns include Amount (? sales value)
        string[] preferred =
        {
            "Amount", "Sales", "SalesAmount", "SaleAmount", "TotalSales", "NetSales", "Sales Value", "SalesValue"
        };

        foreach (var name in preferred)
        {
            foreach (DataColumn col in table.Columns)
            {
                if (string.Equals(col.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
        }

        foreach (DataColumn col in table.Columns)
        {
            if (col.ColumnName.Contains("Sales", StringComparison.OrdinalIgnoreCase) &&
                !col.ColumnName.Contains("Qty", StringComparison.OrdinalIgnoreCase) &&
                !col.ColumnName.Contains("Quantity", StringComparison.OrdinalIgnoreCase))
            {
                return col;
            }
        }

        return null;
    }

    private static bool IsEbidtaHeaderRow(DataRow row)
    {
        // Match ERP highlighting: empty Groupname (column index 2) = header / subtotal row
        if (row.Table.Columns.Count > 2)
        {
            var third = Convert.ToString(row[2]) ?? "";
            if (string.IsNullOrWhiteSpace(third))
                return true;
        }

        return false;
    }

    private static bool IsTotalLabelRow(DataRow row)
    {
        foreach (var col in row.Table.Columns.Cast<DataColumn>().Take(4))
        {
            var text = Convert.ToString(row[col]) ?? "";
            if (text.Contains("Total", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static DataTable BuildCompanyTvp(IReadOnlyList<string> companies)
    {
        var table = new DataTable("CompanyList");
        table.Columns.Add("StringValue", typeof(string));
        foreach (var name in companies)
        {
            if (!string.IsNullOrWhiteSpace(name))
                table.Rows.Add(name.Trim());
        }

        if (table.Rows.Count == 0)
            table.Rows.Add("");

        return table;
    }

    private async Task<string> ResolveCompanyTableTypeAsync(SqlConnection connection, string procedureName)
    {
        var cacheKey = "sales-tvp:" + procedureName;
        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var discovered = await connection.ExecuteScalarAsync<string>($@"
            SELECT TOP 1
                QUOTENAME(SCHEMA_NAME(tt.schema_id)) + '.' + QUOTENAME(tt.name)
            FROM sys.parameters p
            INNER JOIN sys.table_types tt ON p.user_type_id = tt.user_type_id
            WHERE p.object_id = OBJECT_ID('dbo.{procedureName}')
              AND p.name IN ('@companyname', '@CompanyName')");

        if (!string.IsNullOrWhiteSpace(discovered))
        {
            _cache.Set(cacheKey, discovered, MetaCacheTtl);
            return discovered;
        }

        var fallback = await connection.ExecuteScalarAsync<string>(@"
            SELECT TOP 1 QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)
            FROM sys.table_types
            WHERE name IN ('StringArray', 'CompanyList', 'StringList', 'StringValue')
            ORDER BY CASE name
                WHEN 'StringArray' THEN 1
                WHEN 'CompanyList' THEN 2
                ELSE 3 END");

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _cache.Set(cacheKey, fallback, MetaCacheTtl);
            return fallback;
        }

        throw new InvalidOperationException(
            $"Could not resolve table type for @companyname on {procedureName}.");
    }

    public Task<SalesDashboardResult> GetDashboardAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int rptType,
        string category,
        bool refresh = false) =>
        GetDashboardSectionAsync(company, dateFrom, dateTo, rptType, category, "all", refresh);

    public async Task<SalesDashboardResult> GetDashboardSectionAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int rptType,
        string category,
        string section,
        bool refresh = false)
    {
        var companies = (await GetCompaniesAsync()).ToList();
        var selectedCompanies = await ResolveSelectedCompaniesAsync(company);
        var selectedCategory = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase)
            ? "Purchase"
            : "Sales";
        var normalizedSection = NormalizeSection(section);

        var cache = await GetOrLoadLedgerCacheAsync(
            selectedCompanies, dateFrom, dateTo, refresh);

        var result = new SalesDashboardResult
        {
            Companies = BuildCompanyOptions(companies),
            Company = company,
            DateFrom = dateFrom.ToString("yyyy-MM-dd"),
            DateTo = dateTo.ToString("yyyy-MM-dd"),
            RptType = rptType,
            Category = selectedCategory,
            UnavailableFields = cache.UnavailableFields.ToList(),
            Diagnostics = new SalesDashboardDiagnostics
            {
                SummarySource = "vw_ItemLedgerTransaction",
                SummaryColumns = cache.Columns.ToList(),
                Note =
                    $"section={normalizedSection}; cacheRows={cache.Rows.Count}; fromCache={!cache.JustLoaded}. " +
                    "Amount/Qty from frmSalesPurchaseItemWise. Gross Profit unavailable (ERP P&L only).",
                CompanyParamRows = selectedCompanies.Count,
                Table0Rows = cache.Rows.Count,
            },
        };

        if (normalizedSection is "kpis" or "all")
            result.Summary = BuildSummary(cache.Rows, selectedCategory);

        if (normalizedSection is "charts" or "all")
        {
            result.Trend = BuildTrend(cache.Rows, selectedCategory);
            result.ByGroup = BuildByGroup(cache.Rows, selectedCategory);
            result.ByCompany = BuildByCompany(cache.Rows, selectedCategory);
        }

        if (normalizedSection is "tables" or "all")
        {
            result.TopProducts = BuildTopProducts(cache.Rows, selectedCategory);
            result.TopCustomers = cache.HasSupplier
                ? BuildTopCustomers(cache.Rows, selectedCategory)
                : new List<TopCustomerDto>();
            result.BySubGroup = BuildBySubGroup(cache.Rows, selectedCategory);
            result.DetailedAnalysis = BuildDetailed(cache.Rows, selectedCategory, rptType);
            result.Diagnostics!.Table1Rows = result.DetailedAnalysis.Count;
        }

        return result;
    }

    private async Task<LedgerCacheEntry> GetOrLoadLedgerCacheAsync(
        IReadOnlyList<string> selectedCompanies,
        DateTime dateFrom,
        DateTime dateTo,
        bool refresh)
    {
        var key = BuildCacheKey(selectedCompanies, dateFrom, dateTo);
        if (refresh)
            _cache.Remove(key);

        if (!refresh && _cache.TryGetValue(key, out LedgerCacheEntry? existing) && existing != null)
        {
            existing.JustLoaded = false;
            return existing;
        }

        var gate = LoadLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (!refresh && _cache.TryGetValue(key, out existing) && existing != null)
            {
                existing.JustLoaded = false;
                return existing;
            }

            var loaded = await LoadLedgerRowsAsync(selectedCompanies, dateFrom, dateTo);
            loaded.JustLoaded = true;
            _cache.Set(key, loaded, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
            });
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<LedgerCacheEntry> LoadLedgerRowsAsync(
        IReadOnlyList<string> selectedCompanies,
        DateTime dateFrom,
        DateTime dateTo)
    {
        using var connection = _database.CreateConnection();
        var columns = await GetLedgerColumnsAsync(connection);

        var hasGst = columns.Contains("TotalGST");
        var hasCgst = columns.Contains("CGSTAmount");
        var hasSgst = columns.Contains("SGSTAmount");
        var hasIgst = columns.Contains("IGSTAmount");
        var hasBill = columns.Contains("BillAmount");
        var hasMainGroup = columns.Contains("MainGroup");
        var hasItemGroup = columns.Contains("ItemGroupname");
        var hasSubGroup = columns.Contains("SubGroupName");
        var hasSupplier = columns.Contains("SupplierName");
        var hasItemMaster = columns.Contains("itemMasterName");

        var itemExpr = hasItemMaster
            ? "ISNULL(NULLIF(LTRIM(RTRIM(itemMasterName)), ''), ItemName)"
            : "ItemName";
        var gstExpr = BuildGstExpression(hasGst, hasCgst, hasSgst, hasIgst);
        var billExpr = hasBill
            ? "CASE WHEN VoucherType IN ('Sales Return','Purchase Return') THEN -ISNULL(BillAmount,0) ELSE ISNULL(BillAmount,0) END"
            : "CAST(0 AS float)";
        const string qtyExpr = "ABS(ISNULL(InwardQty,0) - ISNULL(OutwardQty,0))";
        const string amountExpr = "ABS(ISNULL(InwardValue,0) - ISNULL(OutwardValue,0))";

        var groupExpr = hasMainGroup && hasItemGroup
            ? "CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(MainGroup,''))), '') IS NOT NULL THEN MainGroup ELSE ISNULL(ItemGroupname,'') END"
            : hasMainGroup ? "ISNULL(MainGroup,'')"
            : hasItemGroup ? "ISNULL(ItemGroupname,'')"
            : "CAST('' AS nvarchar(200))";
        var subGroupExpr = hasSubGroup ? "ISNULL(SubGroupName,'')" : "CAST('' AS nvarchar(200))";
        var supplierExpr = hasSupplier ? "ISNULL(SupplierName,'')" : "CAST('' AS nvarchar(200))";

        var companyFilter = BuildCompanyFilter(selectedCompanies, out var parameters);
        parameters.Add("DateFrom", dateFrom.Date);
        parameters.Add("DateTo", dateTo.Date);

        var sql = $@"
SELECT
    ISNULL(CompanyName,'') AS CompanyName,
    SysDate,
    {supplierExpr} AS CustomerName,
    {itemExpr} AS ProductName,
    {groupExpr} AS GroupName,
    {subGroupExpr} AS SubGroupName,
    {qtyExpr} AS Qty,
    {amountExpr} AS Amount,
    {gstExpr} AS GstAmount,
    {billExpr} AS NetAmount,
    CASE
        WHEN VoucherType IN (SELECT DISTINCT VoucherType FROM SalesVoucher) THEN 'Sales'
        WHEN VoucherType IN (SELECT DISTINCT VoucherType FROM PurchaseVoucher) THEN 'Purchase'
        ELSE 'Other'
    END AS Category
FROM vw_ItemLedgerTransaction WITH (NOLOCK)
WHERE {companyFilter}
  AND SysDate >= @DateFrom AND SysDate < DATEADD(day, 1, @DateTo)
  AND (
        VoucherType IN (SELECT DISTINCT VoucherType FROM SalesVoucher)
     OR VoucherType IN (SELECT DISTINCT VoucherType FROM PurchaseVoucher)
  )";

        var rows = (await connection.QueryAsync<DashRow>(sql, parameters, commandTimeout: 0)).ToList();

        var unavailable = new List<string> { "changePercents", "grossProfit" };
        if (!hasGst && !hasCgst && !hasSgst && !hasIgst)
            unavailable.Add("gstAmount");
        if (!hasBill)
            unavailable.Add("netAmount");
        if (!hasSupplier)
            unavailable.Add("topCustomers");

        return new LedgerCacheEntry
        {
            Rows = rows,
            Columns = columns.OrderBy(c => c).ToList(),
            UnavailableFields = unavailable,
            HasSupplier = hasSupplier,
        };
    }

    private static SalesSummaryDto BuildSummary(IReadOnlyList<DashRow> rows, string selectedCategory)
    {
        var sales = rows.Where(r => r.Category == "Sales").ToList();
        var purchase = rows.Where(r => r.Category == "Purchase").ToList();
        var selected = selectedCategory == "Purchase" ? purchase : sales;
        var qty = selected.Sum(r => r.Qty);
        var amount = selected.Sum(r => r.Amount);
        return new SalesSummaryDto
        {
            TotalSales = sales.Sum(r => r.Amount),
            TotalPurchase = purchase.Sum(r => r.Amount),
            TotalQuantity = qty,
            AverageRate = qty > 0 ? Math.Round(amount / qty, 2) : 0,
            GstAmount = selected.Sum(r => r.GstAmount),
            GrossProfit = 0,
        };
    }

    private static List<SalesTrendDto> BuildTrend(IReadOnlyList<DashRow> rows, string category) =>
        rows.Where(r => r.Category == category)
            .GroupBy(r => new { r.SysDate.Year, r.SysDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new SalesTrendDto
            {
                Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy"),
                Amount = g.Sum(x => x.Amount),
            })
            .ToList();

    private static List<SalesByGroupDto> BuildByGroup(IReadOnlyList<DashRow> rows, string category)
    {
        var byGroup = rows.Where(r => r.Category == category)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.GroupName) ? "" : r.GroupName.Trim())
            .Where(g => g.Key.Length > 0)
            .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .ToList();
        var total = byGroup.Sum(g => g.Amount);
        return byGroup.Select(g => new SalesByGroupDto
        {
            GroupName = g.Name,
            Amount = g.Amount,
            Percentage = total > 0 ? Math.Round(g.Amount * 100.0 / total, 1) : 0,
        }).ToList();
    }

    private static List<SalesByCompanyDto> BuildByCompany(IReadOnlyList<DashRow> rows, string category) =>
        rows.Where(r => r.Category == category)
            .GroupBy(r => r.CompanyName)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new SalesByCompanyDto { CompanyName = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .ToList();

    private static List<TopProductDto> BuildTopProducts(IReadOnlyList<DashRow> rows, string category) =>
        rows.Where(r => r.Category == category)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.ProductName) ? "" : r.ProductName.Trim())
            .Where(g => g.Key.Length > 0)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Qty), Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(5)
            .Select((g, i) => new TopProductDto
            {
                Rank = i + 1,
                ProductName = g.Name,
                Quantity = g.Qty,
                SalesAmount = g.Amount,
            })
            .ToList();

    private static List<TopCustomerDto> BuildTopCustomers(IReadOnlyList<DashRow> rows, string category) =>
        rows.Where(r => r.Category == category)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.CustomerName) ? "" : r.CustomerName.Trim())
            .Where(g => g.Key.Length > 0)
            .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .Select((g, i) => new TopCustomerDto
            {
                Rank = i + 1,
                CustomerName = g.Name,
                SalesAmount = g.Amount,
            })
            .ToList();

    private static List<SalesBySubGroupDto> BuildBySubGroup(IReadOnlyList<DashRow> rows, string category) =>
        rows.Where(r => r.Category == category)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.SubGroupName) ? "" : r.SubGroupName.Trim())
            .Where(g => g.Key.Length > 0)
            .Select(g => new SalesBySubGroupDto
            {
                SubGroupName = g.Key,
                Quantity = g.Sum(x => x.Qty),
                SalesAmount = g.Sum(x => x.Amount),
            })
            .OrderByDescending(g => g.SalesAmount)
            .Take(20)
            .ToList();

    private static List<DetailedSalesRowDto> BuildDetailed(
        IReadOnlyList<DashRow> rows,
        string category,
        int rptType)
    {
        IEnumerable<IGrouping<string, DashRow>> groups;
        var filtered = rows.Where(r => r.Category == category);

        if (rptType == 2)
        {
            groups = filtered.GroupBy(r => $"{r.CompanyName}|{r.SysDate:yyyy-MM-dd}");
        }
        else if (rptType == 1)
        {
            groups = filtered.GroupBy(r => $"{r.CompanyName}|{r.GroupName}|{r.SubGroupName}|{r.ProductName}");
        }
        else
        {
            groups = filtered.GroupBy(r => $"{r.CompanyName}|{r.GroupName}|{r.SubGroupName}");
        }

        return groups.Select((g, i) =>
        {
            var first = g.First();
            var qty = g.Sum(x => x.Qty);
            var amount = g.Sum(x => x.Amount);
            return new DetailedSalesRowDto
            {
                Id = Convert.ToString(i + 1)!,
                GroupName = rptType == 2 ? first.SysDate.ToString("yyyy-MM-dd") : (first.GroupName ?? ""),
                SubGroupName = rptType == 2 ? "" : (first.SubGroupName ?? ""),
                ProductName = rptType == 1 ? (first.ProductName ?? "") : "",
                InterGroup = first.CompanyName,
                SalesPurchase = category,
                Quantity = qty,
                Amount = amount,
                PerKgRate = qty > 0 ? Math.Round(amount / qty, 2) : 0,
                GstAmount = g.Sum(x => x.GstAmount),
                NetAmount = g.Sum(x => x.NetAmount),
            };
        })
        .OrderByDescending(r => r.Amount)
        .ToList();
    }

    private static string NormalizeSection(string section)
    {
        var s = (section ?? "all").Trim().ToLowerInvariant();
        return s is "kpis" or "charts" or "tables" ? s : "all";
    }

    private static string BuildCacheKey(
        IReadOnlyList<string> companies,
        DateTime dateFrom,
        DateTime dateTo) =>
        $"sales-ledger:{string.Join("|", companies.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))}:" +
        $"{dateFrom:yyyy-MM-dd}:{dateTo:yyyy-MM-dd}";

    private static string BuildGstExpression(bool hasTotal, bool hasC, bool hasS, bool hasI)
    {
        if (hasTotal)
            return "ISNULL(TotalGST,0)";

        var parts = new List<string>();
        if (hasC) parts.Add("ISNULL(CGSTAmount,0)");
        if (hasS) parts.Add("ISNULL(SGSTAmount,0)");
        if (hasI) parts.Add("ISNULL(IGSTAmount,0)");
        return parts.Count == 0 ? "CAST(0 AS float)" : string.Join(" + ", parts);
    }

    private async Task<HashSet<string>> GetLedgerColumnsAsync(SqlConnection connection)
    {
        if (_ledgerColumns != null)
            return _ledgerColumns;

        var names = await connection.QueryAsync<string>(@"
            SELECT c.name
            FROM sys.columns c
            INNER JOIN sys.objects o ON c.object_id = o.object_id
            WHERE o.name = 'vw_ItemLedgerTransaction'");
        _ledgerColumns = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return _ledgerColumns;
    }

    private async Task<HashSet<string>> GetViewColumnsAsync(SqlConnection connection, string viewName)
    {
        var cacheKey = "sales-cols:" + viewName;
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached != null)
            return cached;
        if (_viewColumns.TryGetValue(viewName, out var local))
            return local;

        var names = await connection.QueryAsync<string>(@"
            SELECT c.name
            FROM sys.columns c
            INNER JOIN sys.objects o ON c.object_id = o.object_id
            WHERE o.name = @ViewName",
            new { ViewName = viewName });
        var set = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        _viewColumns[viewName] = set;
        _cache.Set(cacheKey, set, MetaCacheTtl);
        return set;
    }

    private async Task<IReadOnlyList<FactoryRow>> GetFactoryRowsAsync()
    {
        if (_cache.TryGetValue("sales-factories", out List<FactoryRow>? cached) && cached != null)
            return cached;

        await FactoryLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue("sales-factories", out cached) && cached != null)
                return cached;

            using var connection = _database.CreateConnection();
            var rows = (await connection.QueryAsync<FactoryRow>(@"
SELECT fi.srno AS SrNo, LTRIM(RTRIM(fi.Name)) AS Name, LTRIM(RTRIM(ISNULL(fi.GroupName, N''))) AS GroupName
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, N'') <> N''
ORDER BY fi.Name")).ToList();
            _cache.Set("sales-factories", rows, MetaCacheTtl);
            return rows;
        }
        finally
        {
            FactoryLock.Release();
        }
    }

    /// <summary>
    /// Country-wise MIS GroupName is the legal company (e.g. Plastene India Limited),
    /// not every factory unit. G-Plastene India Limited → that legal name only.
    /// </summary>
    private static List<string> ResolveBankCountrywiseNames(string company)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in SplitCompanyTokens(company))
        {
            if (IsAllToken(token))
                continue;
            names.Add(token.StartsWith("G-", StringComparison.OrdinalIgnoreCase)
                ? token[2..].Trim()
                : token.Trim());
        }
        return names.ToList();
    }

    private async Task<List<string>> ResolveSelectedCompaniesAsync(string company)
    {
        var factories = await GetFactoryRowsAsync();
        var salesNames = await GetSalesCompanyNamesAsync();
        var universe = factories
            .Select(f => f.Name)
            .Concat(salesNames)
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tokens = SplitCompanyTokens(company);
        if (tokens.Count == 0 || tokens.Any(IsAllToken))
            return universe;

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            if (token.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
            {
                var group = token[2..].Trim();
                foreach (var factory in factories.Where(f =>
                             f.GroupName.Equals(group, StringComparison.OrdinalIgnoreCase)
                             || f.Name.StartsWith(group, StringComparison.OrdinalIgnoreCase)))
                    names.Add(factory.Name);
            }
            else
            {
                names.Add(token);
                foreach (var name in universe.Where(n => CompanyKeysMatch(n, token)))
                    names.Add(name);
            }
        }

        return names.ToList();
    }

    private async Task<IReadOnlyList<string>> GetSalesCompanyNamesAsync()
    {
        if (_cache.TryGetValue("sales-ebidta-companies", out List<string>? cached) && cached != null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<string>(
            @"SELECT DISTINCT LTRIM(RTRIM(CompanyName))
              FROM dbo.vw_Sales_EBIDTA WITH (NOLOCK)
              WHERE ISNULL(LTRIM(RTRIM(CompanyName)), N'') <> N''",
            commandTimeout: QueryTimeoutSeconds)).ToList();
        _cache.Set("sales-ebidta-companies", rows, MetaCacheTtl);
        return rows;
    }

    private static string CompanyKey(string name)
    {
        var s = (name ?? "").Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", " ");
        s = Regex.Replace(s, @"\b(pvt|private|ltd|limited|llp)\b", " ");
        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private static bool CompanyKeysMatch(string left, string right)
    {
        var a = CompanyKey(left);
        var b = CompanyKey(right);
        if (a.Length == 0 || b.Length == 0) return false;
        if (a.Equals(b, StringComparison.Ordinal)) return true;
        if (a.Length >= 4 && b.StartsWith(a + " ", StringComparison.Ordinal)) return true;
        if (b.Length >= 4 && a.StartsWith(b + " ", StringComparison.Ordinal)) return true;
        return false;
    }

    private static async Task<List<string>> GetGroupNamesForCompaniesAsync(
        SqlConnection connection,
        IReadOnlyList<string> selectedCompanies)
    {
        if (selectedCompanies.Count == 0)
            return new List<string>();

        return (await connection.QueryAsync<string>(
            @"SELECT DISTINCT LTRIM(RTRIM(GroupName))
              FROM FactoryInfo WITH (NOLOCK)
              WHERE Name IN @Names
                AND ISNULL(LTRIM(RTRIM(GroupName)), '') <> ''",
            new { Names = selectedCompanies })).ToList();
    }

    /// <summary>
    /// Country view company keys: FactoryInfo group + factory names, intersected with
    /// actual vw_Countrywise_sales_dashboard.GroupName values when those differ.
    /// Also filters CompanyName when that column exists (same key as EBIDTA).
    /// </summary>
    private async Task<CountryViewCompanyFilter> ResolveCountryViewCompanyFilterAsync(
        SqlConnection connection,
        IReadOnlyList<string> selectedCompanies,
        IReadOnlyList<string> invYears)
    {
        var cols = await GetViewColumnsAsync(connection, "vw_Countrywise_sales_dashboard");
        var companyCol = FirstExisting(cols, CompanyColumnCandidates);
        var groupNames = await ResolveCountryViewGroupNamesAsync(connection, selectedCompanies, invYears);
        var companySql = companyCol == null
            ? "LTRIM(RTRIM(ISNULL(GroupName, N''))) IN @GroupNames"
            : $"(LTRIM(RTRIM(ISNULL({Bracket(companyCol)}, N''))) IN @CompanyNames OR LTRIM(RTRIM(ISNULL(GroupName, N''))) IN @GroupNames)";

        return new CountryViewCompanyFilter(
            companySql,
            groupNames,
            selectedCompanies.ToList(),
            companyCol);
    }

    private async Task<List<string>> ResolveCountryViewGroupNamesAsync(
        SqlConnection connection,
        IReadOnlyList<string> selectedCompanies,
        IReadOnlyList<string> invYears)
    {
        var factoryGroups = await GetGroupNamesForCompaniesAsync(connection, selectedCompanies);
        var candidates = factoryGroups
            .Concat(selectedCompanies)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0 || invYears.Count == 0)
            return candidates;

        var viewGroupKey = "sales-country-groups:" + string.Join(",", invYears);
        List<string> viewGroups;
        if (_cache.TryGetValue(viewGroupKey, out List<string>? cachedGroups) && cachedGroups != null)
        {
            viewGroups = cachedGroups;
        }
        else
        {
            try
            {
                viewGroups = (await connection.QueryAsync<string>(
                    @"SELECT DISTINCT LTRIM(RTRIM(GroupName))
                      FROM dbo.vw_Countrywise_sales_dashboard WITH (NOLOCK)
                      WHERE InvYear IN @InvYears
                        AND ISNULL(LTRIM(RTRIM(GroupName)), '') <> ''",
                    new { InvYears = invYears })).ToList();
                _cache.Set(viewGroupKey, viewGroups, MetaCacheTtl);
            }
            catch
            {
                return candidates;
            }
        }

        if (viewGroups.Count == 0)
            return candidates;

        var exact = viewGroups
            .Where(vg => candidates.Any(c => vg.Equals(c, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (exact.Count > 0)
            return exact;

        static string Normalize(string value)
        {
            var chars = value.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToLowerInvariant();
        }

        var candidateNorms = candidates
            .Select(c => Normalize(c))
            .Where(n => n.Length >= 6)
            .ToList();

        var fuzzy = viewGroups
            .Where(vg =>
            {
                var vn = Normalize(vg);
                if (vn.Length < 4)
                    return false;
                return candidateNorms.Any(c => vn.Contains(c) || c.Contains(vn));
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Country view only contains a subset of FactoryInfo groups (e.g. no Oswal).
        // Empty means "not in this view" — caller should fall back to EBIDTA.
        return fuzzy;
    }

    private static List<string> NonEmptyInList(IReadOnlyList<string> values) =>
        values.Count > 0 ? values.ToList() : new List<string> { "__none__" };

    private sealed record CountryViewCompanyFilter(
        string Sql,
        List<string> GroupNames,
        List<string> CompanyNames,
        string? CompanyColumn);

    private static void AddCompanyTvp(IDbCommand command, string typeName, DataTable companyTable)
    {
        var sqlCommand = (SqlCommand)command;
        var companyParam = sqlCommand.Parameters.Add("@companyname", SqlDbType.Structured);
        companyParam.TypeName = typeName;
        companyParam.Value = companyTable;
    }

    private static bool IsAllCompaniesSelection(string company)
    {
        var tokens = SplitCompanyTokens(company);
        return tokens.Count == 0 || tokens.Any(IsAllToken);
    }

    private static bool IsAllToken(string token) =>
        string.IsNullOrWhiteSpace(token) ||
        token.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
        token.Contains("(All)", StringComparison.OrdinalIgnoreCase);

    private static List<string> SplitCompanyTokens(string company) =>
        (company ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .ToList();

    private static int ClampTop(int top)
    {
        if (top <= 0) top = 5;
        return Math.Clamp(top, 1, 100);
    }

    private static string? FirstExisting(HashSet<string> columns, IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (columns.Contains(candidate))
                return columns.First(c => c.Equals(candidate, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }

    private static string Bracket(string column) => "[" + column.Replace("]", "]]") + "]";

    private static string PartyIsFibco(string partyExpr) =>
        $"LOWER(LTRIM(RTRIM(ISNULL({partyExpr}, N'')))) LIKE N'%fibco%'";

    private static string OrFibcoParty(string partyExpr, string predicate) =>
        $"(({predicate}) OR {PartyIsFibco(partyExpr)})";

    private static string InterCompanyNotYes(string icExpr) =>
        $@"LOWER(LTRIM(RTRIM(ISNULL({icExpr}, N'no')))) NOT IN (N'yes', N'y', N'true', N'1')";

    private static string ExportCountryPredicate(string countryExpr) =>
        $@"LOWER(LTRIM(RTRIM(ISNULL({countryExpr}, N'')))) NOT IN (N'india', N'in', N'ind', N'bharat')
  AND LOWER(LTRIM(RTRIM(ISNULL({countryExpr}, N'')))) NOT LIKE N'%india%'
  AND LTRIM(RTRIM(ISNULL({countryExpr}, N''))) <> N''";

    private async Task<string> BuildExportLedgerJoinAsync(SqlConnection connection, string partySql)
    {
        var masterCols = await GetViewColumnsAsync(connection, "CommonLedgerMaster");
        var groupCols = new[]
        {
            "GroupName", "LedgerGroup", "Under", "ParentGroup", "PrimaryGroup",
            "expensehead", "expensegrouphead", "b", "c", "d", "e", "f", "g",
        }
            .Where(masterCols.Contains)
            .Select(Bracket)
            .ToList();

        if (groupCols.Count == 0)
            return "";

        var likes = groupCols.SelectMany(col => new[]
        {
            $"LOWER(ISNULL({col}, N'')) LIKE N'%overseas%'",
            $"LOWER(ISNULL({col}, N'')) LIKE N'%export%'",
            $"LOWER(ISNULL({col}, N'')) LIKE N'%foreign%'",
        });

        return $@"
INNER JOIN (
    SELECT DISTINCT LTRIM(RTRIM(LedgerName)) AS LedgerName
    FROM CommonLedgerMaster WITH (NOLOCK)
    WHERE {string.Join(" OR ", likes)}
) m ON m.LedgerName = LTRIM(RTRIM(v.{partySql}))";
    }

    private static RankedPartyResultDto EmptyRanked(string source, string? partyColumn, string? countryColumn) =>
        new()
        {
            Items = new List<RankedPartyDto>(),
            Source = source,
            PartyColumn = partyColumn ?? "",
            CountryColumn = countryColumn ?? "",
        };

    private static RankedPartyResultDto ToRankedResult(
        List<RankedPartyDto> rows,
        string source,
        string? partyColumn,
        string? countryColumn)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Rank = i + 1;
            rows[i].Name = string.IsNullOrWhiteSpace(rows[i].Name) ? "Unknown" : rows[i].Name.Trim();
            var country = rows[i].Country;
            rows[i].Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
        }

        return new RankedPartyResultDto
        {
            Items = rows,
            Source = source,
            PartyColumn = partyColumn ?? "",
            CountryColumn = countryColumn ?? "",
        };
    }

    private static List<string> ResolveSelectedCompanies(string company, IReadOnlyList<string> all)
    {
        if (IsAllCompaniesSelection(company))
        {
            return all.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        var trimmed = company.Trim();
        if (trimmed.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
            return all.ToList();

        return new List<string> { trimmed };
    }

    private static string BuildCompanyFilter(IReadOnlyList<string> companies, out DynamicParameters parameters)
    {
        parameters = new DynamicParameters();
        if (companies.Count == 0)
        {
            parameters.Add("Company0", "");
            return "CompanyName = @Company0";
        }

        var names = new List<string>();
        for (var i = 0; i < companies.Count; i++)
        {
            var p = $"Company{i}";
            parameters.Add(p, companies[i]);
            names.Add("@" + p);
        }

        return companies.Count == 1
            ? $"CompanyName = {names[0]}"
            : $"CompanyName IN ({string.Join(",", names)})";
    }

    private static List<string> BuildCompanyOptions(IReadOnlyList<string> companies)
    {
        var list = new List<string> { "All Companies" };
        foreach (var name in companies)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.Equals("All Companies", StringComparison.OrdinalIgnoreCase)) continue;
            if (list.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
            list.Add(name);
        }
        return list;
    }

    private sealed class DashRow
    {
        public string CompanyName { get; set; } = "";
        public DateTime SysDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string SubGroupName { get; set; } = "";
        public double Qty { get; set; }
        public double Amount { get; set; }
        public double GstAmount { get; set; }
        public double NetAmount { get; set; }
        public string Category { get; set; } = "";
    }

    private sealed class LedgerCacheEntry
    {
        public List<DashRow> Rows { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string> UnavailableFields { get; set; } = new();
        public bool HasSupplier { get; set; }
        public bool JustLoaded { get; set; }
    }
    private sealed class FactoryRow
    {
        public int SrNo { get; set; }
        public string Name { get; set; } = "";
        public string GroupName { get; set; } = "";
    }

    private sealed class TrendBundle
    {
        public List<TrendLeaf> Rows { get; set; } = new();
        public List<TrendRange> Ranges { get; set; } = new();
    }

    private sealed class GeoBundle
    {
        public List<CountryLeaf> Countries { get; set; } = new();
        public List<PartyLeaf> ExportCustomers { get; set; } = new();
        public string Source { get; set; } = "";
        public List<string> InvYears { get; set; } = new();
        public string CountryPeriodLabel { get; set; } = "";
    }

    private sealed class SalesUniverse
    {
        public List<EbidtaLeaf> Leaves { get; set; } = new();
        public List<TrendLeaf> Trend { get; set; } = new();
        public List<TrendRange> TrendRanges { get; set; } = new();
        public List<CountryLeaf> Countries { get; set; } = new();
        public List<PartyLeaf> ExportCustomers { get; set; } = new();
        public string CountryPeriodLabel { get; set; } = "";
        public List<string> InvYears { get; set; } = new();
        public string CountrySource { get; set; } = "";
        public string ExportSource { get; set; } = "";
        public double ElapsedSeconds { get; set; }
    }

    private sealed class EbidtaLeaf
    {
        public string CompanyName { get; set; } = "";
        public string InterGroup { get; set; } = "";
        public string Groupname { get; set; } = "";
        public string SubGroupName { get; set; } = "";
        public double Amount { get; set; }
        public double Netwt { get; set; }
    }

    private sealed class TrendLeaf
    {
        public string CompanyName { get; set; } = "";
        public int FyStart { get; set; }
        public double Amount { get; set; }
    }

    private sealed class TrendRange
    {
        public int StartYear { get; set; }
        public string Period { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    private sealed class CountryLeaf
    {
        public string CompanyName { get; set; } = "";
        public string CountryName { get; set; } = "";
        public double SalesAmount { get; set; }
    }

    private sealed class PartyLeaf
    {
        public string CompanyName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Country { get; set; }
        public double Amount { get; set; }
    }
}

public class SalesCompanyOptionDto
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "company";
}

public class RankedPartyDto
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public double Amount { get; set; }
}

public class RankedPartyResultDto
{
    public List<RankedPartyDto> Items { get; set; } = new();
    public string Source { get; set; } = "";
    public string PartyColumn { get; set; } = "";
    public string CountryColumn { get; set; } = "";
    public List<string> Columns { get; set; } = new();
}

public class SalesExportSplitDto
{
    public double TotalSales { get; set; }
    public double ExportSales { get; set; }
    public double DomesticSales { get; set; }
    public double IntercompanySales { get; set; }
    public string Source { get; set; } = "";
}

public class SalesTotalsDto
{
    public double TotalSales { get; set; }
    public double TotalPurchase { get; set; }
    public double TotalQuantity { get; set; }
    public double AverageRate { get; set; }
    public string SalesColumn { get; set; } = "";
    public string QuantityColumn { get; set; } = "";
    public string RateColumn { get; set; } = "";
    public string Method { get; set; } = "";
    public int RowCount { get; set; }
    public List<string> Columns { get; set; } = new();
    public double ElapsedSeconds { get; set; }
    public List<SalesByGroupDto> ByGroup { get; set; } = new();
    public List<SalesBySubGroupDto> BySubGroup { get; set; } = new();
}

public class SalesDashboardResult
{
    public List<string> Companies { get; set; } = new();
    public string Company { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public int RptType { get; set; }
    public string Category { get; set; } = "Sales";
    public SalesSummaryDto Summary { get; set; } = new();
    public List<SalesTrendDto> Trend { get; set; } = new();
    public List<SalesByGroupDto> ByGroup { get; set; } = new();
    public List<SalesByCompanyDto> ByCompany { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<SalesBySubGroupDto> BySubGroup { get; set; } = new();
    public List<DetailedSalesRowDto> DetailedAnalysis { get; set; } = new();
    public List<string> UnavailableFields { get; set; } = new();
    public SalesDashboardDiagnostics? Diagnostics { get; set; }
}

public class SalesDashboardDiagnostics
{
    public int TableCount { get; set; }
    public int Table0Rows { get; set; }
    public int Table1Rows { get; set; }
    public string SummarySource { get; set; } = "";
    public List<string> SummaryColumns { get; set; } = new();
    public List<string> ItemColumns { get; set; } = new();
    public string CompanyTypeName { get; set; } = "";
    public int CompanyParamRows { get; set; }
    public string Note { get; set; } = "";
}

public class SalesSummaryDto
{
    public double TotalSales { get; set; }
    public double TotalQuantity { get; set; }
    public double AverageRate { get; set; }
    public double GstAmount { get; set; }
    public double TotalPurchase { get; set; }
    public double GrossProfit { get; set; }
    public double TotalSalesChangePercent { get; set; }
    public double TotalQuantityChangePercent { get; set; }
    public double AverageRateChangePercent { get; set; }
    public double GstAmountChangePercent { get; set; }
    public double TotalPurchaseChangePercent { get; set; }
    public double GrossProfitChangePercent { get; set; }
}

public class SalesTrendDto
{
    public string Period { get; set; } = "";
    public double Amount { get; set; }
}

public class SalesByGroupDto
{
    public string GroupName { get; set; } = "";
    public double Amount { get; set; }
    public double Percentage { get; set; }
}

public class SalesByCompanyDto
{
    public string CompanyName { get; set; } = "";
    public double Amount { get; set; }
}

public class TopProductDto
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = "";
    public double Quantity { get; set; }
    public double SalesAmount { get; set; }
}

public class TopCustomerDto
{
    public int Rank { get; set; }
    public string CustomerName { get; set; } = "";
    public double SalesAmount { get; set; }
}

public class SalesByCountryDto
{
    public int Rank { get; set; }
    public string CountryName { get; set; } = "";
    public double SalesAmount { get; set; }
}

public class SalesByCountryResultDto
{
    public List<SalesByCountryDto> ByCountry { get; set; } = new();
    public List<string> GroupNames { get; set; } = new();
    public List<string> InvYears { get; set; } = new();
    /// <summary>Display label e.g. "FY 25-26" or "FY 24-25, FY 25-26".</summary>
    public string PeriodLabel { get; set; } = "";
    public string Source { get; set; } = "";
}

public class SalesOverviewDto
{
    public SalesTotalsDto Totals { get; set; } = new();
    public List<SalesTrendDto> Trend { get; set; } = new();
    public List<SalesByCountryDto> ByCountry { get; set; } = new();
    public string CountryPeriodLabel { get; set; } = "";
    public List<RankedPartyDto> ExportCustomers { get; set; } = new();
    public List<RankedPartyDto> Suppliers { get; set; } = new();
}

public class SalesBySubGroupDto
{
    public string SubGroupName { get; set; } = "";
    public double Quantity { get; set; }
    public double SalesAmount { get; set; }
}

public class DetailedSalesRowDto
{
    public string Id { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string SubGroupName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string InterGroup { get; set; } = "";
    public string SalesPurchase { get; set; } = "";
    public double Quantity { get; set; }
    public double Amount { get; set; }
    public double PerKgRate { get; set; }
    public double GstAmount { get; set; }
    public double NetAmount { get; set; }
}
