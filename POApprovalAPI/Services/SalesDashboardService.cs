using System.Collections.Concurrent;
using System.Data;
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
    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private HashSet<string>? _ledgerColumns;

    public SalesDashboardService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
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
    /// Total Sales + Quantity + Average Rate + byGroup/bySubGroup.
    /// Mirrors ERP SP_Sales_EBIDTA (aggregates vw_Sales_EBIDTA with the same GROUPING SETS),
    /// but excludes intercompany: InterGroup &lt;&gt; 'Intergroup'
    /// (vw_Sales_EBIDTA maps CommonLedgerMaster.IsInterCompany='yes' ? InterGroup='Intergroup').
    /// SP_Sales_EBIDTA itself has no IC filter / param ? country chart uses the same IC flag
    /// via vw_Countrywise_sales_dashboard (IsInterCompany != 'yes').
    /// </summary>
    public Task<SalesTotalsDto> GetSalesTotalsAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo) =>
        GetEbidtaTotalsAsync("Sales", company, dateFrom, dateTo);

    /// <summary>
    /// Total Purchase + Quantity + Average Rate + byGroup/bySubGroup.
    /// Mirrors ERP SP_Purchase_EBIDTA (aggregates vw_Purchase_EBIDTA with the same GROUPING SETS),
    /// but excludes intercompany: InterGroup &lt;&gt; 'Intergroup'
    /// (vw_Purchase_EBIDTA maps CommonLedgerMaster.IsInterCompany='yes' ? InterGroup='Intergroup').
    /// </summary>
    public Task<SalesTotalsDto> GetPurchaseTotalsAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo) =>
        GetEbidtaTotalsAsync("Purchase", company, dateFrom, dateTo);

    /// <summary>
    /// Shared EBIDTA totals for Sales or Purchase (same column shape / IC filter).
    /// </summary>
    private async Task<SalesTotalsDto> GetEbidtaTotalsAsync(
        string category,
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        var column1 = isPurchase ? "Purchase" : "Sales";
        var viewName = isPurchase ? "vw_Purchase_EBIDTA" : "vw_Sales_EBIDTA";
        var procedureName = isPurchase ? "SP_Purchase_EBIDTA" : "SP_Sales_EBIDTA";

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var connection = _database.CreateConnection();

        var companies = (await GetCompaniesAsync()).ToList();
        var selectedCompanies = ResolveSelectedCompanies(company, companies);
        var companyTable = BuildCompanyTvp(selectedCompanies);
        // Same TVP type the matching EBIDTA SP uses for @companyname
        var typeName = await ResolveCompanyTableTypeAsync(connection, procedureName);

        // Exact SP select shape + company join, plus InterGroup IC filter.
        // Do not invent alternate Amount/Netwt math ? same ROUND/FORMAT as the SP
        // (with safe PerKg when Netwt = 0, matching the sales path).
        var sql = $@"
SELECT
    N'{column1}' AS Column1,
    InterGroup,
    Groupname,
    SubGroupName,
    ROUND(SUM(Amount), 0) AS Amount,
    ROUND(SUM(netwt), 0) AS Netwt,
    CASE WHEN SUM(netwt) != 0
         THEN FORMAT(ROUND(ROUND(SUM(Amount), 0) / ROUND(SUM(netwt), 0), 2), '#.00')
         ELSE FORMAT(0, '#.00') END AS PerKg,
    FORMAT(ROUND(SUM(SGSTAmount), 2), '#.00') AS SGSTAmount
FROM dbo.{viewName} WITH (NOLOCK)
INNER JOIN @companyname C ON C.StringValue = CompanyName
WHERE invdate BETWEEN @DateFrom AND @DateTo
  AND InterGroup <> N'Intergroup'
GROUP BY GROUPING SETS ((InterGroup, Groupname, SubGroupName), (InterGroup), ())
ORDER BY InterGroup, Groupname, SubGroupName";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 0;

        var companyParam = command.Parameters.Add("@companyname", SqlDbType.Structured);
        companyParam.TypeName = typeName;
        companyParam.Value = companyTable;

        command.Parameters.Add("@DateFrom", SqlDbType.Date).Value = dateFrom.Date;
        command.Parameters.Add("@DateTo", SqlDbType.Date).Value = dateTo.Date;

        var dataSet = new DataSet();
        using (var adapter = new SqlDataAdapter((SqlCommand)command))
        {
            adapter.Fill(dataSet);
        }

        sw.Stop();

        if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
        {
            return new SalesTotalsDto { ElapsedSeconds = sw.Elapsed.TotalSeconds };
        }

        var table = dataSet.Tables[0];
        var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var amountCol = ResolveSalesColumn(table);
        if (amountCol == null)
        {
            throw new InvalidOperationException(
                $"{viewName} (excl. IC) returned no recognizable Amount column. Columns: " +
                string.Join(", ", columns));
        }

        var qtyCol = ResolveQuantityColumn(table);
        var rateCol = ResolveRateColumn(table);

        // Same row shape as SP_*_EBIDTA ERP grid (IC already filtered out):
        // Column1, InterGroup, Groupname, SubGroupName, Amount, Netwt, PerKg, SGSTAmount
        // Grand total: Column1=Sales|Purchase, InterGroup blank, Groupname blank
        var totals = ResolveEbidtaGrandTotals(table, amountCol, qtyCol, rateCol, column1);
        var (byGroup, bySubGroup) = BuildEbidtaBreakdowns(table, amountCol, qtyCol, column1);

        return new SalesTotalsDto
        {
            TotalSales = isPurchase ? 0 : totals.Amount,
            TotalPurchase = isPurchase ? totals.Amount : 0,
            TotalQuantity = totals.Quantity,
            AverageRate = totals.AverageRate,
            SalesColumn = amountCol.ColumnName,
            QuantityColumn = qtyCol?.ColumnName ?? "",
            RateColumn = rateCol?.ColumnName ?? "",
            Method = totals.Method + "_ExclIntercompany",
            RowCount = table.Rows.Count,
            Columns = columns,
            ElapsedSeconds = sw.Elapsed.TotalSeconds,
            ByGroup = byGroup,
            BySubGroup = bySubGroup,
        };
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
    /// for that Indian FY (Apr?Mar). Current FY is capped at <paramref name="asOf"/>.
    /// </summary>
    public Task<List<SalesTrendDto>> GetSalesYearlyTrendAsync(
        string company,
        DateTime asOf,
        int years = 5) =>
        GetEbidtaYearlyTrendAsync("Sales", company, asOf, years);

    /// <summary>
    /// Year-by-year Total Purchase for trend chart (vw_Purchase_EBIDTA, excl. IC).
    /// </summary>
    public Task<List<SalesTrendDto>> GetPurchaseYearlyTrendAsync(
        string company,
        DateTime asOf,
        int years = 5) =>
        GetEbidtaYearlyTrendAsync("Purchase", company, asOf, years);

    private async Task<List<SalesTrendDto>> GetEbidtaYearlyTrendAsync(
        string category,
        string company,
        DateTime asOf,
        int years = 5)
    {
        years = Math.Clamp(years, 1, 8);
        var isPurchase = category.Equals("Purchase", StringComparison.OrdinalIgnoreCase);

        // Indian FY: Apr 1 ? Mar 31. FY label uses start calendar year.
        var currentFyStartYear = asOf.Month >= 4 ? asOf.Year : asOf.Year - 1;

        var ranges = new List<(string Period, DateTime From, DateTime To)>();
        for (var i = years - 1; i >= 0; i--)
        {
            var startYear = currentFyStartYear - i;
            var from = new DateTime(startYear, 4, 1);
            if (from > asOf)
                continue;

            var to = new DateTime(startYear + 1, 3, 31);
            if (to > asOf)
                to = asOf;

            var label = $"FY {startYear % 100:D2}-{(startYear + 1) % 100:D2}";
            ranges.Add((label, from, to));
        }

        // Parallel calls ? each year uses the same ERP grand-total logic
        var tasks = ranges.Select(async range =>
        {
            var totals = await GetEbidtaTotalsAsync(category, company, range.From, range.To);
            return new SalesTrendDto
            {
                Period = range.Period,
                Amount = isPurchase ? totals.TotalPurchase : totals.TotalSales,
            };
        });

        var points = await Task.WhenAll(tasks);
        return points.ToList();
    }

    /// <summary>
    /// Sales by Country from ERP vw_Countrywise_sales_dashboard (Value = Amount - DebitNote).
    /// View already excludes intercompany (IsInterCompany != 'yes').
    /// Filters: FactoryInfo.GroupName for selected company; InvYear for Indian FYs
    /// overlapping dateFrom?dateTo. View is FY-aggregated (not day-level).
    /// </summary>
    public async Task<SalesByCountryResultDto> GetSalesByCountryAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        int top = 5)
    {
        if (top <= 0)
            top = 5;
        top = Math.Clamp(top, 1, 100);

        using var connection = _database.CreateConnection();

        var allCompanies = (await GetCompaniesAsync()).ToList();
        var selectedCompanies = ResolveSelectedCompanies(company, allCompanies);
        var isAll = string.IsNullOrWhiteSpace(company) ||
                    company.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
                    company.Contains("(All)", StringComparison.OrdinalIgnoreCase);

        var groupNames = isAll
            ? new List<string>()
            : (await connection.QueryAsync<string>(
                @"SELECT DISTINCT LTRIM(RTRIM(GroupName))
                  FROM FactoryInfo WITH (NOLOCK)
                  WHERE Name IN @Names
                    AND ISNULL(LTRIM(RTRIM(GroupName)), '') <> ''",
                new { Names = selectedCompanies })).ToList();

        var invYears = GetInvYearsOverlapping(dateFrom, dateTo).ToList();
        var periodLabel = FormatPeriodLabel(invYears);
        if (invYears.Count == 0 || (!isAll && groupNames.Count == 0))
        {
            return new SalesByCountryResultDto
            {
                ByCountry = new List<SalesByCountryDto>(),
                GroupNames = groupNames,
                InvYears = invYears,
                PeriodLabel = periodLabel,
            };
        }

        // Value (= Amount - DebitNote) is the chart measure; IC already excluded in the view.
        var sql = isAll
            ? @"
SELECT TOP (@Top)
    Country AS CountryName,
    SUM(CAST(Value AS float)) AS SalesAmount
FROM dbo.vw_Countrywise_sales_dashboard WITH (NOLOCK)
WHERE InvYear IN @InvYears
GROUP BY Country
ORDER BY SalesAmount DESC"
            : @"
SELECT TOP (@Top)
    Country AS CountryName,
    SUM(CAST(Value AS float)) AS SalesAmount
FROM dbo.vw_Countrywise_sales_dashboard WITH (NOLOCK)
WHERE InvYear IN @InvYears
  AND GroupName IN @GroupNames
GROUP BY Country
ORDER BY SalesAmount DESC";

        var rows = (await connection.QueryAsync<SalesByCountryDto>(
            sql,
            isAll
                ? (object)new { Top = top, InvYears = invYears }
                : new { Top = top, InvYears = invYears, GroupNames = groupNames })).ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Rank = i + 1;
            rows[i].CountryName = string.IsNullOrWhiteSpace(rows[i].CountryName)
                ? "Unknown"
                : rows[i].CountryName.Trim();
        }

        return new SalesByCountryResultDto
        {
            ByCountry = rows,
            GroupNames = groupNames,
            InvYears = invYears,
            PeriodLabel = periodLabel,
        };
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

    private static async Task<string> ResolveCompanyTableTypeAsync(SqlConnection connection, string procedureName)
    {
        var discovered = await connection.ExecuteScalarAsync<string>($@"
            SELECT TOP 1
                QUOTENAME(SCHEMA_NAME(tt.schema_id)) + '.' + QUOTENAME(tt.name)
            FROM sys.parameters p
            INNER JOIN sys.table_types tt ON p.user_type_id = tt.user_type_id
            WHERE p.object_id = OBJECT_ID('dbo.{procedureName}')
              AND p.name IN ('@companyname', '@CompanyName')");

        if (!string.IsNullOrWhiteSpace(discovered))
            return discovered;

        var fallback = await connection.ExecuteScalarAsync<string>(@"
            SELECT TOP 1 QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)
            FROM sys.table_types
            WHERE name IN ('StringArray', 'CompanyList', 'StringList', 'StringValue')
            ORDER BY CASE name
                WHEN 'StringArray' THEN 1
                WHEN 'CompanyList' THEN 2
                ELSE 3 END");

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

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
        var selectedCompanies = ResolveSelectedCompanies(company, companies);
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
            .Take(5)
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

    private static List<string> ResolveSelectedCompanies(string company, IReadOnlyList<string> all)
    {
        if (string.IsNullOrWhiteSpace(company) ||
            company.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
            company.Contains("(All)", StringComparison.OrdinalIgnoreCase))
        {
            return all.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return new List<string> { company.Trim() };
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
