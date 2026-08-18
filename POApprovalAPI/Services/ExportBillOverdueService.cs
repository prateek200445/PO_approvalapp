using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill receivable overdue — mirrors ERP FrmReceivable BindGrid
/// (Outstanding → Receivable), with selectable Group Name (e.g. Debtors-Overseas).
/// Fast path: slim ERP SQL + cached IC/FX lookups applied in memory.
/// </summary>
public class ExportBillOverdueService
{
    public const string DefaultGroupName = "Debtors-Overseas";
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;
    private const int CommandTimeoutSeconds = 120;
    private const double MinPendingInr = 100;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromHours(2);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public ExportBillOverdueService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        const string key = "export-bill-overdue-companies-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<string>(
            @"SELECT Name
              FROM FactoryInfo WITH (NOLOCK)
              ORDER BY Name");
        var list = rows
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _cache.Set(key, (IReadOnlyList<string>)list, MetaCacheTtl);

        // Warm the usual export filter in the background so the first UI load is cache-hit.
        var preferred = list.FirstOrDefault(c =>
            c.Equals("HCP Plastene Bulkpack Ltd", StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(c =>
                !c.Contains("(All)", StringComparison.OrdinalIgnoreCase) &&
                !c.Equals("All Companies", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var company = preferred;
            var asOf = DateTime.Today;
            _ = Task.Run(async () =>
            {
                try
                {
                    await GetOverdueBillsAsync(company, asOf, DefaultGroupName, 1, DefaultPageSize);
                }
                catch
                {
                    // Warm is best-effort.
                }
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<string>> GetGroupsAsync()
    {
        const string key = "export-bill-overdue-groups-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<string>(
            @"SELECT DISTINCT ExpenseGroupHead
              FROM CashVoucherExpenseGroupHead WITH (NOLOCK)
              WHERE OutStanding = 'Yes'
              ORDER BY ExpenseGroupHead");
        var list = rows
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _cache.Set(key, (IReadOnlyList<string>)list, MetaCacheTtl);
        return list;
    }

    public async Task<ExportBillOverdueResultDto> GetOverdueBillsAsync(
        string company,
        DateTime asOf,
        string? groupName,
        int page = 1,
        int pageSize = DefaultPageSize,
        bool refresh = false)
    {
        var selectedGroup = string.IsNullOrWhiteSpace(groupName) ? "" : groupName.Trim();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (page - 1) * pageSize;

        var companyLabel = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var asOfDate = asOf.Date;
        var cacheKey = $"export-bill-overdue-v11|{companyLabel}|{asOfDate:yyyy-MM-dd}|{selectedGroup}";

        if (refresh)
            _cache.Remove(cacheKey);

        var rows = await GetOrLoadRowsAsync(cacheKey, companyLabel, asOfDate, selectedGroup);
        var total = rows.Count;
        var pageItems = rows.Skip(offset).Take(pageSize).ToList();

        return new ExportBillOverdueResultDto
        {
            Items = pageItems,
            Company = companyLabel,
            AsOf = asOfDate.ToString("yyyy-MM-dd"),
            GroupName = selectedGroup,
            Source = "vw_billwisetransactionwithonaccount + accountbills (FrmReceivable, fast)",
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    private async Task<List<ExportBillOverdueItemDto>> GetOrLoadRowsAsync(
        string cacheKey,
        string companyLabel,
        DateTime asOfDate,
        string selectedGroup)
    {
        if (_cache.TryGetValue(cacheKey, out List<ExportBillOverdueItemDto>? cached) && cached is not null)
            return cached;

        var gate = LoadLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
                return cached;

            var loaded = await LoadAllRowsAsync(companyLabel, asOfDate, selectedGroup);
            _cache.Set(cacheKey, loaded, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<ExportBillOverdueItemDto>> LoadAllRowsAsync(
        string companyLabel,
        DateTime asOfDate,
        string selectedGroup)
    {
        // Load once (cached): IC ledger keys + RBI rates for foreign display.
        var icTask = GetIntercompanyExclusionAsync();
        var fxTask = GetFxRatesAsync(asOfDate);
        await Task.WhenAll(icTask, fxTask);
        var ic = icTask.Result;
        var fx = fxTask.Result;

        var isAllCompanies =
            string.IsNullOrWhiteSpace(companyLabel) ||
            companyLabel.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
            companyLabel.Contains("(All)", StringComparison.OrdinalIgnoreCase);

        List<ExportBillOverdueItemDto> rows;
        if (!isAllCompanies)
        {
            using var connection = _database.CreateConnection();
            var companyId = await ResolveCompanyIdAsync(connection, companyLabel);
            if (companyId is null or 0)
                return new List<ExportBillOverdueItemDto>();

            rows = await QueryCompanyFastAsync(connection, companyId.Value, asOfDate, selectedGroup);
        }
        else
        {
            var allCompanies = await GetCompaniesAsync();
            var selected = allCompanies
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Cap parallelism so SQL Server isn't flooded.
            var bags = new ConcurrentBag<List<ExportBillOverdueItemDto>>();
            await Parallel.ForEachAsync(
                selected,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (companyName, _) =>
                {
                    using var connection = _database.CreateConnection();
                    var companyId = await ResolveCompanyIdAsync(connection, companyName);
                    if (companyId is null or 0)
                        return;
                    var batch = await QueryCompanyFastAsync(
                        connection, companyId.Value, asOfDate, selectedGroup);
                    bags.Add(batch);
                });

            rows = bags.SelectMany(b => b).ToList();
        }

        return rows
            .Where(r => !IsIntercompanyRow(r, ic))
            .Select(r => ApplyForeignAmount(r, fx))
            .OrderByDescending(r => r.OverdueDays)
            .ThenBy(r => r.LedgerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.BillNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<int?> ResolveCompanyIdAsync(SqlConnection connection, string companyName)
    {
        var key = $"export-company-id|{companyName}";
        if (_cache.TryGetValue(key, out int cachedId) && cachedId > 0)
            return cachedId;

        var companyId = await connection.ExecuteScalarAsync<int?>(
            @"SELECT TOP 1 SrNo
              FROM FactoryInfo WITH (NOLOCK)
              WHERE Name = @CompanyName",
            new { CompanyName = companyName });

        if (companyId is > 0)
            _cache.Set(key, companyId.Value, MetaCacheTtl);

        return companyId;
    }

    /// <summary>
    /// Slim query close to ERP FrmReceivable: no per-row IC / currency_rbi joins.
    /// Group filter is pushed into the innermost FROM so we never aggregate
    /// domestic / other-group ledgers for Debtors-Overseas style loads.
    /// Pending = ABS(SUM(amount)) with Forex treated as 0 (Rs calc).
    /// </summary>
    private static async Task<List<ExportBillOverdueItemDto>> QueryCompanyFastAsync(
        SqlConnection connection,
        int companyId,
        DateTime asOf,
        string groupName)
    {
        var filterByGroup = !string.IsNullOrWhiteSpace(groupName);

        // Restrict early: only ledgers in the selected outstanding group.
        var groupRestrict = filterByGroup
            ? @"
        INNER JOIN (
            SELECT DISTINCT
                LTRIM(RTRIM(ledgername)) AS ledgername,
                LTRIM(RTRIM(companyname)) AS companyname
            FROM vw_ledgergrouping WITH (NOLOCK)
            WHERE @GroupName IN (expensehead, expensegrouphead, b, c, d, e, f, g)
        ) grp
            ON grp.ledgername = LTRIM(RTRIM(v1.ledgername))
           AND grp.companyname = LTRIM(RTRIM(v1.CompanyName))"
            : "";

        var sql = $@"
SELECT
    CompanyName,
    LedgerName,
    LedgerName AS CustomerName,
    billno AS BillNo,
    BillDate,
    ROUND(ABS(SUM(amount)), 3) AS BillAmount,
    DueDate,
    CASE
        WHEN DueDate = '1900-01-01' THEN 0
        WHEN DATEDIFF(DAY, DueDate, @AsOf) < 0 THEN 0
        ELSE DATEDIFF(DAY, DueDate, @AsOf)
    END AS OverdueDays,
    ROUND(ABS(SUM(amount)), 3) AS PendingAmount,
    DisplayCurrency AS BillCurrency,
    CAST(0 AS float) AS ForeignAmount
FROM (
    SELECT
        v1.companyname AS CompanyName,
        v1.ledgername AS LedgerName,
        CASE WHEN ISNULL(v1.billno, '') = '' THEN 'On Account' ELSE v1.billno END AS billno,
        CASE WHEN ISNULL(v1.billno, '') = '' THEN CAST('1900-01-01' AS datetime) ELSE v2.billdate END AS BillDate,
        CASE WHEN ISNULL(v1.billno, '') = '' THEN CAST('1900-01-01' AS datetime) ELSE v2.duedate END AS DueDate,
        ISNULL(NULLIF(LTRIM(RTRIM(v2.BillCurrency)), ''), ISNULL(NULLIF(LTRIM(RTRIM(v1.Currency)), ''), 'Rs.')) AS DisplayCurrency,
        ISNULL(v1.amount, 0) AS amount
    FROM vw_billwisetransactionwithonaccount v1 WITH (NOLOCK)
    {groupRestrict}
    LEFT JOIN accountbills v2 WITH (NOLOCK)
        ON v1.companyid = v2.companyid
       AND v1.ledgername = v2.ledgername
       AND v1.billno = v2.billno
       AND v1.CompanyName = v2.CompanyName
       AND v1.ledgerid = v2.LedgerId
    WHERE v1.isbillwise = 'yes'
      AND v1.companyid = @CompanyId
      AND v1.voucherdate <= @AsOf
) AS t1
GROUP BY CompanyName, LedgerName, billno, BillDate, DueDate, DisplayCurrency
HAVING ROUND(ABS(SUM(amount)), 3) >= @MinPending
ORDER BY OverdueDays DESC, LedgerName, BillNo";

        var rows = await connection.QueryAsync<ExportBillOverdueRow>(
            sql,
            new
            {
                CompanyId = companyId,
                AsOf = asOf,
                GroupName = groupName,
                MinPending = MinPendingInr,
            },
            commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new ExportBillOverdueItemDto
        {
            CompanyName = (r.CompanyName ?? "").Trim(),
            LedgerName = (r.LedgerName ?? "").Trim(),
            CustomerName = (r.CustomerName ?? r.LedgerName ?? "").Trim(),
            BillNo = (r.BillNo ?? "").Trim(),
            BillDate = r.BillDate?.ToString("yyyy-MM-dd") ?? "",
            BillAmount = r.BillAmount,
            DueDate = r.DueDate?.ToString("yyyy-MM-dd") ?? "",
            OverdueDays = r.OverdueDays,
            PendingAmount = r.PendingAmount,
            BillCurrency = NormalizeCurrency(r.BillCurrency),
            ForeignAmount = 0,
        }).ToList();
    }

    private async Task<IcExclusion> GetIntercompanyExclusionAsync()
    {
        const string key = "export-bill-overdue-ic-excl-v1";
        if (_cache.TryGetValue(key, out IcExclusion? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();

        var flagged = await connection.QueryAsync<(string CompanyName, string LedgerName)>(
            @"
SELECT LTRIM(RTRIM(CompanyName)) AS CompanyName, LTRIM(RTRIM(LedgerName)) AS LedgerName
FROM CommonLedgerMaster WITH (NOLOCK)
WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(IsInterCompany, 'no'))))) IN ('yes', 'y', '1', 'true')

UNION

SELECT LTRIM(RTRIM(l.CompanyName)), LTRIM(RTRIM(l.LedgerName))
FROM ac_interCompanyLedger icl WITH (NOLOCK)
INNER JOIN LedgerMaster l WITH (NOLOCK) ON icl.LedgerId = l.srno",
            commandTimeout: 60);

        var factoryNames = await connection.QueryAsync<string>(
            @"SELECT LTRIM(RTRIM(Name))
              FROM FactoryInfo WITH (NOLOCK)
              WHERE ISNULL(Name, '') <> ''",
            commandTimeout: 30);

        var pairKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in flagged)
        {
            if (string.IsNullOrWhiteSpace(row.CompanyName) || string.IsNullOrWhiteSpace(row.LedgerName))
                continue;
            pairKeys.Add(IcKey(row.CompanyName, row.LedgerName));
        }

        var factories = new HashSet<string>(
            factoryNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var excl = new IcExclusion { PairKeys = pairKeys, FactoryNames = factories };
        _cache.Set(key, excl, MetaCacheTtl);
        return excl;
    }

    private async Task<FxRates> GetFxRatesAsync(DateTime asOf)
    {
        var key = $"export-bill-overdue-fx|{asOf:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out FxRates? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<FxRates>(
            @"SELECT TOP 1
                  CAST(ISNULL(Dollar, 1) AS float) AS Dollar,
                  CAST(ISNULL(Euro, 1) AS float) AS Euro,
                  CAST(ISNULL(Pound, 1) AS float) AS Pound,
                  CAST(ISNULL(CHF, 1) AS float) AS CHF
              FROM currency_rbi WITH (NOLOCK)
              WHERE @AsOf BETWEEN sysdate AND ISNULL(todate, DATEADD(YEAR, 5, GETDATE()))
              ORDER BY sysdate DESC",
            new { AsOf = asOf.Date });

        var rates = row ?? new FxRates { Dollar = 1, Euro = 1, Pound = 1, CHF = 1 };
        _cache.Set(key, rates, MetaCacheTtl);
        return rates;
    }

    private static bool IsIntercompanyRow(ExportBillOverdueItemDto row, IcExclusion ic) =>
        ic.PairKeys.Contains(IcKey(row.CompanyName, row.LedgerName)) ||
        ic.FactoryNames.Contains(row.LedgerName);

    private sealed class IcExclusion
    {
        public HashSet<string> PairKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> FactoryNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static ExportBillOverdueItemDto ApplyForeignAmount(ExportBillOverdueItemDto row, FxRates fx)
    {
        var rate = row.BillCurrency switch
        {
            "$" => fx.Dollar,
            "€" => fx.Euro,
            "GBP" => fx.Pound,
            "CHF" => fx.CHF,
            _ => 1d,
        };

        if (rate > 1 && !IsInr(row.BillCurrency))
            row.ForeignAmount = Math.Round(Math.Abs(row.PendingAmount) / rate, 3);
        else
            row.ForeignAmount = 0;

        return row;
    }

    private static bool IsInr(string currency) =>
        string.IsNullOrWhiteSpace(currency) ||
        currency.StartsWith("Rs", StringComparison.OrdinalIgnoreCase) ||
        currency.Equals("INR", StringComparison.OrdinalIgnoreCase) ||
        currency == "₹";

    private static string IcKey(string company, string ledger) =>
        $"{company.Trim()}|{ledger.Trim()}";

    private static string NormalizeCurrency(string? currency)
    {
        var c = (currency ?? "").Trim();
        if (string.IsNullOrEmpty(c)) return "Rs.";
        if (c.StartsWith("Rs", StringComparison.OrdinalIgnoreCase)) return "Rs.";
        if (c is "$" or "USD" or "US$") return "$";
        if (c is "€" or "?" or "EUR" or "Euro") return "€";
        if (c.Equals("GBP", StringComparison.OrdinalIgnoreCase)) return "GBP";
        if (c.Equals("CHF", StringComparison.OrdinalIgnoreCase)) return "CHF";
        return c;
    }

    private sealed class ExportBillOverdueRow
    {
        public string? CompanyName { get; set; }
        public string? LedgerName { get; set; }
        public string? CustomerName { get; set; }
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public double BillAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public int OverdueDays { get; set; }
        public double PendingAmount { get; set; }
        public string? BillCurrency { get; set; }
        public double ForeignAmount { get; set; }
    }

    private sealed class FxRates
    {
        public double Dollar { get; set; } = 1;
        public double Euro { get; set; } = 1;
        public double Pound { get; set; } = 1;
        public double CHF { get; set; } = 1;
    }
}

public class ExportBillOverdueItemDto
{
    public string CompanyName { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string BillNo { get; set; } = "";
    public string BillDate { get; set; } = "";
    public double BillAmount { get; set; }
    public string DueDate { get; set; } = "";
    public int OverdueDays { get; set; }
    public double PendingAmount { get; set; }
    public string BillCurrency { get; set; } = "Rs.";
    public double ForeignAmount { get; set; }
}

public class ExportBillOverdueResultDto
{
    public List<ExportBillOverdueItemDto> Items { get; set; } = new();
    public string Company { get; set; } = "";
    public string AsOf { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Source { get; set; } = "";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = ExportBillOverdueService.DefaultPageSize;
    public int TotalCount { get; set; }
}
