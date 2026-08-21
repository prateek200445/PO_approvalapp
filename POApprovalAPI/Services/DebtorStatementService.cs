using System.Data;
using System.Diagnostics;
using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Bank drawing-power debtor pack as-on a freeze date (option A: no subsequent receipts).
/// Bills: as-on remaining from vw_BillWiseTransactionWithOnAccount (tagged lines netted,
/// untagged on-account credits allocated LIFO). Book: group ledger closing.
/// Extra bills vs book are LIFO-allocated, not deleted.
/// </summary>
public class DebtorStatementService
{
    public const string DefaultCompany = DebtorStatementDefaults.CompanyGroup;
    private const int CommandTimeoutSeconds = 180;
    private const int BookParallelism = 8;
    private const int GroupBookSpThreshold = 500;
    private const decimal MinAmount = 1m;
    private static readonly TimeSpan ResultCacheTtl = TimeSpan.FromMinutes(20);
    private static readonly string[] TradeUnders = ["Debtors-Domestic", "Debtors-Overseas"];
    private static readonly string[] RelatedPartyNeedles =
    [
        "oliva garden",
        "plastene polyfilms",
    ];
    private static readonly string[] IntercompanyNeedles =
    [
        "plastene india ltd - unit",
        "plastene india ltd- unit",
        "plastene india limited - unit",
        "plastene india ltd - ho",
        "plastene india ltd- ho",
        "plastene india limited - ho",
        "k.p. woven",
        "kp woven",
        "hcp plastene",
        "pal polyfilm",
        "iocl - pal polyfilm",
    ];

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private readonly LedgerSummaryService _ledgerSummary;
    private readonly ILogger<DebtorStatementService> _logger;

    public DebtorStatementService(
        DatabaseService database,
        IMemoryCache cache,
        LedgerSummaryService ledgerSummary,
        ILogger<DebtorStatementService> logger)
    {
        _database = database;
        _cache = cache;
        _ledgerSummary = ledgerSummary;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LedgerCompanyOption>> GetCompaniesAsync()
    {
        const string key = "debtor-statement-companies-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<LedgerCompanyOption>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var companies = (await connection.QueryAsync<(int SrNo, string Name, string? GroupName)>(@"
SELECT fi.srno AS SrNo, fi.Name, fi.GroupName
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, '') <> ''
ORDER BY fi.Name")).ToList();

        var options = new List<LedgerCompanyOption>();
        var groups = companies
            .Select(c => (c.GroupName ?? "").Trim())
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        foreach (var group in groups)
        {
            options.Add(new LedgerCompanyOption
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                CompanyType = 1,
                CompanyName = group,
                CompanyId = 0,
            });
        }

        foreach (var c in companies)
        {
            options.Add(new LedgerCompanyOption
            {
                Value = $"C-{c.SrNo}",
                Label = c.Name.Trim(),
                CompanyType = 2,
                CompanyName = "",
                CompanyId = c.SrNo,
            });
        }

        _cache.Set(key, (IReadOnlyList<LedgerCompanyOption>)options, TimeSpan.FromHours(6));
        return options;
    }

    public async Task<DebtorStatementResultDto> QueryAsync(DebtorStatementQueryRequest request)
    {
        var company = string.IsNullOrWhiteSpace(request.Company)
            ? DefaultCompany
            : request.Company.Trim();
        var asOn = request.AsOn == default ? PreviousMonthEnd(DateTime.Today) : request.AsOn.Date;
        var includeG = request.IncludeCurrentAssets;
        var cacheKey = ResultCacheKey(company, asOn, includeG);
        if (_cache.TryGetValue(cacheKey, out DebtorStatementResultDto? cached) && cached is not null)
            return cached;

        var result = await BuildAsync(company, asOn, includeG);
        _cache.Set(cacheKey, result, ResultCacheTtl);
        return result;
    }

    public async Task<byte[]> BuildExportAsync(DebtorStatementQueryRequest request)
    {
        var result = await QueryAsync(request);
        return BuildWorkbook(result);
    }

    private async Task<DebtorStatementResultDto> BuildAsync(string company, DateTime asOn, bool includeG)
    {
        var factories = await GetFactoryRowsAsync();
        var selected = ResolveCompanies(company, factories);
        if (selected.Count == 0)
            throw new InvalidOperationException("No companies matched the selection.");

        var companyLabel = CompanyLabel(company, factories);
        var sw = Stopwatch.StartNew();
        var rawBills = await LoadOpenBillsAsync(selected, asOn);
        NormalizeBillSign(rawBills);
        var dropped = rawBills.RemoveAll(b => IsIntercompanySalesLedger(b.LedgerName));
        if (dropped > 0)
            _logger.LogInformation("Debtor statement dropped {Count} intercompany sales bills", dropped);
        _logger.LogInformation("Debtor statement bills: {Count} rows in {Ms}ms", rawBills.Count, sw.ElapsedMilliseconds);

        sw.Restart();
        var groupName = company.StartsWith("G-", StringComparison.OrdinalIgnoreCase)
            ? company[2..].Trim()
            : "";
        var bookByPair = await LoadBookBalancesAsync(selected, rawBills, asOn, includeG, groupName);
        _logger.LogInformation("Debtor statement books: {Count} pairs in {Ms}ms", bookByPair.Count, sw.ElapsedMilliseconds);
        LogWatchlist(rawBills, bookByPair);
        var gstinByParty = rawBills
            .GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => PickGstin(g.Select(x => x.Gstin), IsExportUnder(g.First().Under)),
                StringComparer.OrdinalIgnoreCase);

        foreach (var pair in bookByPair.Values)
        {
            var key = PartyKey(pair.LedgerName);
            if (!gstinByParty.ContainsKey(key) || string.IsNullOrWhiteSpace(gstinByParty[key]))
                gstinByParty[key] = PickGstin([pair.Gstin], IsExportUnder(pair.Under) || IsGovernment(pair));
        }

        var partyMeta = BuildPartyMeta(rawBills, bookByPair.Values);
        ApplyLifo(rawBills, partyMeta);

        var openBills = ToBillRows(rawBills, partyMeta);
        if (includeG)
            openBills.AddRange(BuildCurrentAssetRows(bookByPair.Values, partyMeta, asOn));

        foreach (var bill in openBills)
            ApplyAgeing(bill, asOn);

        openBills = openBills
            .OrderBy(b => b.PartyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.InvoiceDate)
            .ThenBy(b => b.InvoiceNo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pivot = BuildPivot(openBills, partyMeta, gstinByParty);
        var bookDebts = BuildBookDebts(openBills);

        var kpis = new DebtorStatementKpisDto
        {
            CompanyCount = selected.Count,
            PartyCount = pivot.Count,
            BillCount = openBills.Count,
            OpenBillCount = openBills.Count(b => b.NetAmount >= MinAmount),
            OriginalTotal = Round2(openBills.Sum(b => b.OriginalAmount)),
            AllocatedTotal = Round2(openBills.Sum(b => b.AllocatedAmount)),
            NetTotal = Round2(openBills.Sum(b => b.NetAmount)),
            BookTotal = Round2(pivot.Sum(p => p.AsPerBook)),
            DiffTotal = Round2(pivot.Sum(p => p.Diff)),
            LifoPartyCount = pivot.Count(p => p.AllocatedTotal >= MinAmount),
            NonBillGapPartyCount = pivot.Count(p => p.Status == "Non-bill balance"),
        };

        return new DebtorStatementResultDto
        {
            Company = company,
            CompanyLabel = companyLabel,
            AsOn = asOn.ToString("yyyy-MM-dd"),
            IncludeCurrentAssets = includeG,
            FreezeRule = "as-on",
            AllocationRule = "LIFO",
            Kpis = kpis,
            Bills = openBills,
            Pivot = pivot,
            BookDebts = bookDebts,
        };
    }

    private async Task<List<RawBill>> LoadOpenBillsAsync(IReadOnlyList<string> companies, DateTime asOn)
    {
        using var connection = _database.CreateConnection();
        var typeDesc = await connection.ExecuteScalarAsync<string>(
            "SELECT type_desc FROM sys.objects WHERE name = N'GetOutStandingBillWise'");
        _logger.LogInformation("GetOutStandingBillWise object type: {Type}", typeDesc ?? "(missing)");
        _logger.LogWarning("Outstanding grid returned no rows; reconstructing as-on remaining from the bill-wise view");
        var rows = (await connection.QueryAsync<RawBill>(@"
SELECT
    LTRIM(RTRIM(t1.CompanyName)) AS CompanyName,
    LTRIM(RTRIM(t1.LedgerName)) AS LedgerName,
    LTRIM(RTRIM(ISNULL(t1.billno, N''))) AS BillNo,
    CAST(MIN(t1.BillDate) AS date) AS BillDate,
    CAST(ROUND(SUM(t1.amount), 2) AS decimal(18,2)) AS Remaining
FROM (
    SELECT
        v1.companyname AS CompanyName,
        v1.ledgername AS LedgerName,
        CASE WHEN ISNULL(v1.billno, N'') = N'' THEN N'' ELSE v1.billno END AS billno,
        COALESCE(v2.billdate, v1.voucherdate) AS BillDate,
        ISNULL(v1.amount, 0) AS amount
    FROM vw_billwisetransactionwithonaccount v1 WITH (NOLOCK)
    INNER JOIN (
        SELECT DISTINCT
            LTRIM(RTRIM(ledgername)) AS ledgername,
            LTRIM(RTRIM(companyname)) AS companyname
        FROM vw_ledgergrouping WITH (NOLOCK)
        WHERE expensehead IN @Unders
           OR expensegrouphead IN @Unders
           OR b IN @Unders OR c IN @Unders OR d IN @Unders
           OR e IN @Unders OR f IN @Unders OR g IN @Unders
    ) grp
        ON grp.ledgername = LTRIM(RTRIM(v1.ledgername))
       AND grp.companyname = LTRIM(RTRIM(v1.CompanyName))
    LEFT JOIN accountbills v2 WITH (NOLOCK)
        ON v1.companyid = v2.companyid
       AND v1.ledgername = v2.ledgername
       AND v1.billno = v2.billno
       AND v1.CompanyName = v2.CompanyName
       AND v1.ledgerid = v2.LedgerId
    WHERE v1.voucherdate <= @AsOn
      AND (v2.billdate IS NULL OR v2.billdate <= @AsOn)
      AND v1.CompanyName IN @Companies
) AS t1
GROUP BY t1.CompanyName, t1.LedgerName, LTRIM(RTRIM(ISNULL(t1.billno, N'')))
HAVING ABS(ROUND(SUM(t1.amount), 2)) >= @MinAmount",
            new
            {
                AsOn = asOn,
                Companies = companies,
                Unders = TradeUnders,
                MinAmount,
            },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        var invoices = rows.Where(b => !string.IsNullOrWhiteSpace(b.BillNo)).ToList();
        var onAccount = rows.Where(b => string.IsNullOrWhiteSpace(b.BillNo)).ToList();
        var onAccountCredit = Round2(onAccount.Where(b => b.Remaining < 0).Sum(b => -b.Remaining));
        var onAccountDebit = Round2(onAccount.Where(b => b.Remaining > 0).Sum(b => b.Remaining));
        AllocateOnAccount(invoices, onAccount, asOn);
        _logger.LogInformation(
            "Debtor statement as-on remaining: {Invoices} invoices, on-account Cr {Credit} Dr {Debit}, {Open} open after allocation",
            invoices.Count,
            onAccountCredit,
            onAccountDebit,
            invoices.Count(b => b.Remaining >= MinAmount));

        var masters = await LoadLedgerMetaAsync(connection, companies, includeG: false);
        foreach (var bill in invoices)
        {
            if (masters.TryGetValue(PairKey(bill.CompanyName, bill.LedgerName), out var meta))
            {
                bill.Under = meta.Under;
                bill.Gstin = meta.Gstin;
            }
        }

        return invoices;
    }

    private async Task<List<RawBill>> TryLoadOutstandingGridAsync(
        SqlConnection connection,
        IReadOnlyList<string> companies,
        DateTime asOn)
    {
        var typeDesc = await connection.ExecuteScalarAsync<string>(
            "SELECT type_desc FROM sys.objects WHERE name = N'GetOutStandingBillWise'");
        _logger.LogInformation("GetOutStandingBillWise object type: {Type}", typeDesc ?? "(missing)");
        if (string.IsNullOrWhiteSpace(typeDesc))
            return [];

        var probeCompany = companies.FirstOrDefault(c =>
                               c.Equals("Plastene India Limited", StringComparison.OrdinalIgnoreCase))
                           ?? companies[0];
        var (currency, billno) = await ProbeOutstandingCallAsync(connection, probeCompany, asOn, typeDesc);
        if (currency is null && billno is null)
        {
            _logger.LogWarning("GetOutStandingBillWise probe returned 0 for ACCESS WORLD / Fibco");
            return [];
        }

        var pairs = (await connection.QueryAsync<(string CompanyName, string LedgerName)>(@"
SELECT
    LTRIM(RTRIM(v1.CompanyName)) AS CompanyName,
    LTRIM(RTRIM(v1.LedgerName)) AS LedgerName
FROM vw_billwisetransactionwithonaccount v1 WITH (NOLOCK)
INNER JOIN (
    SELECT DISTINCT
        LTRIM(RTRIM(ledgername)) AS ledgername,
        LTRIM(RTRIM(companyname)) AS companyname
    FROM vw_ledgergrouping WITH (NOLOCK)
    WHERE expensehead IN @Unders
       OR expensegrouphead IN @Unders
       OR b IN @Unders OR c IN @Unders OR d IN @Unders
       OR e IN @Unders OR f IN @Unders OR g IN @Unders
) grp
    ON grp.ledgername = LTRIM(RTRIM(v1.ledgername))
   AND grp.companyname = LTRIM(RTRIM(v1.CompanyName))
WHERE v1.voucherdate <= @AsOn
  AND v1.CompanyName IN @Companies
GROUP BY LTRIM(RTRIM(v1.CompanyName)), LTRIM(RTRIM(v1.LedgerName))
HAVING ABS(ROUND(SUM(ISNULL(v1.amount, 0)), 2)) >= @MinAmount",
            new
            {
                AsOn = asOn,
                Companies = companies,
                Unders = TradeUnders,
                MinAmount,
            },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        _logger.LogInformation(
            "Outstanding grid candidates {Count} using currency={Currency} billno={Billno}",
            pairs.Count,
            currency ?? "(null)",
            billno ?? "(null)");

        using var gate = new SemaphoreSlim(6);
        var tasks = pairs.Select(async pair =>
        {
            await gate.WaitAsync();
            try
            {
                using var c = _database.CreateConnection();
                return await QueryOutstandingBillsAsync(c, pair.CompanyName, pair.LedgerName, asOn, typeDesc, currency, billno);
            }
            finally
            {
                gate.Release();
            }
        });

        var parts = await Task.WhenAll(tasks);
        return parts.SelectMany(x => x).ToList();
    }

    private async Task<(string? Currency, string? Billno)> ProbeOutstandingCallAsync(
        SqlConnection connection,
        string company,
        DateTime asOn,
        string typeDesc)
    {
        var ledgers = new[] { "ACCESS WORLD", "Fibco Plastic Industry Llc", "Oliva Garden S.A." };
        object?[] currencies = ["Rs.", "", "INR", "USD", DBNull.Value];
        object?[] billnos = [DBNull.Value, "", "%"];
        foreach (var ledger in ledgers)
        {
            foreach (var currency in currencies)
            {
                foreach (var billno in billnos)
                {
                    var rows = await QueryOutstandingBillsAsync(
                        connection, company, ledger, asOn, typeDesc, currency, billno);
                    if (rows.Count == 0)
                        continue;
                    _logger.LogInformation(
                        "Outstanding probe hit {Ledger} rows={Count} currency={Currency} billno={Billno} sample={Sample}",
                        ledger,
                        rows.Count,
                        currency is DBNull ? "(null)" : currency ?? "(null)",
                        billno is DBNull ? "(null)" : billno ?? "(null)",
                        rows[0].BillNo);
                    return (
                        currency is DBNull or null ? null : Convert.ToString(currency),
                        billno is DBNull or null ? null : Convert.ToString(billno));
                }
            }
        }

        return (null, null);
    }

    private async Task<List<RawBill>> QueryOutstandingBillsAsync(
        SqlConnection connection,
        string company,
        string ledger,
        DateTime asOn,
        string typeDesc,
        object? currency,
        object? billno)
    {
        try
        {
            IEnumerable<IDictionary<string, object>> rows;
            var isFunction = typeDesc.Contains("FUNCTION", StringComparison.OrdinalIgnoreCase);
            if (isFunction)
            {
                var raw = await connection.QueryAsync(
                    """
                    SELECT * FROM dbo.GetOutStandingBillWise(@companyname, @ledgername, @Currency, @dateto, @Billno)
                    """,
                    new
                    {
                        companyname = company,
                        ledgername = ledger,
                        Currency = currency is DBNull ? null : currency,
                        dateto = asOn.Date,
                        Billno = billno is DBNull ? null : billno,
                    },
                    commandTimeout: 60);
                rows = raw.Select(r => (IDictionary<string, object>)r).ToList();
            }
            else
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "GetOutStandingBillWise";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                cmd.Parameters.AddWithValue("@companyname", company);
                cmd.Parameters.AddWithValue("@ledgername", ledger);
                cmd.Parameters.AddWithValue("@Currency", currency ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateto", asOn.Date);
                cmd.Parameters.AddWithValue("@Billno", billno ?? DBNull.Value);
                var table = new DataTable();
                await using var reader = await cmd.ExecuteReaderAsync();
                table.Load(reader);
                rows = table.Rows.Cast<DataRow>().Select(dr =>
                {
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn col in table.Columns)
                        dict[col.ColumnName] = dr[col] is DBNull ? null! : dr[col];
                    return (IDictionary<string, object>)dict;
                });
            }

            var list = new List<RawBill>();
            foreach (var row in rows)
            {
                var billNo = ReadString(row, "BillNo", "billno", "InvoiceNo", "Invoice Detail", "RefNo");
                var amount = ReadDecimal(row, "PendingAmount", "Pending", "Balance", "Amount", "Dr", "Value");
                if (string.IsNullOrWhiteSpace(billNo) && Math.Abs(amount) < MinAmount)
                    continue;
                list.Add(new RawBill
                {
                    CompanyName = ReadString(row, "CompanyName", "companyname") is { Length: > 0 } c ? c : company,
                    LedgerName = ReadString(row, "LedgerName", "ledgername") is { Length: > 0 } l ? l : ledger,
                    BillNo = string.IsNullOrWhiteSpace(billNo) ? "On Account" : billNo,
                    BillDate = ReadDate(row, "BillDate", "billdate", "InvoiceDate", "Date"),
                    Remaining = amount,
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GetOutStandingBillWise failed for {Company} / {Ledger}", company, ledger);
            return [];
        }
    }

    private void LogWatchlist(List<RawBill> bills, Dictionary<string, BookPair> books)
    {
        string[] needles =
        [
            "oliva garden",
            "fibco",
            "polyfilm",
            "juta",
            "technopac",
            "access world",
            "almatis",
            "gst receivable",
            "advance lic",
        ];
        foreach (var needle in needles)
        {
            var billHits = bills
                .Where(b => (b.LedgerName ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var bookHits = books
                .Where(kv => kv.Key.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (billHits.Count == 0 && bookHits.Count == 0)
                continue;
            _logger.LogInformation(
                "Watch {Needle}: bills {BillCount} orig {Orig} remaining {Rem}; book {BookCount} closing {Closing} ({Names})",
                needle,
                billHits.Count,
                Round2(billHits.Sum(b => b.Remaining + b.Allocated)),
                Round2(billHits.Sum(b => b.Remaining)),
                bookHits.Count,
                Round2(bookHits.Sum(kv => kv.Value.Closing)),
                string.Join(" | ", bookHits.Select(kv => kv.Value.LedgerName).Distinct().Take(5)));
        }
    }

    private async Task<Dictionary<string, BookPair>> LoadBookBalancesAsync(
        IReadOnlyList<string> companies,
        List<RawBill> bills,
        DateTime asOn,
        bool includeG,
        string groupName)
    {
        using var connection = _database.CreateConnection();
        var names = bills
            .Select(b => PartyKey(b.LedgerName))
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var meta = (await LoadLedgerMetaAsync(connection, companies, includeG)).Values.ToList();
        var samples = new Dictionary<string, BookPair>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in meta)
        {
            var key = PartyKey(row.LedgerName);
            if (!samples.TryGetValue(key, out var existing) ||
                (IsGovernment(row) && (row.LedgerName ?? "").Length > (existing.LedgerName ?? "").Length))
            {
                samples[key] = row;
            }
        }

        var selected = samples
            .Where(kv =>
                names.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) ||
                (includeG && IsGovernment(kv.Value)))
            .ToList();

        if (string.IsNullOrWhiteSpace(groupName))
            groupName = companies.FirstOrDefault() ?? "";

        var queryNames = selected
            .Select(kv => kv.Value.LedgerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation(
            "Debtor statement group book for {Count} ledgers via ledger summary ({Group})",
            queryNames.Count,
            groupName);

        var closings = await LoadGroupClosingsViaSummaryAsync(queryNames, groupName, asOn);

        var map = new Dictionary<string, BookPair>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in selected)
        {
            var sample = kv.Value;
            sample.Closing = closings.TryGetValue(PartyKey(sample.LedgerName), out var closing) ? closing : 0m;
            map[kv.Key] = sample;
        }

        return map;
    }

    private async Task<Dictionary<string, decimal>> LoadGroupClosingsViaSummaryAsync(
        IReadOnlyList<string> ledgerNames,
        string groupName,
        DateTime asOn)
    {
        var fyStart = FinancialYearStart(asOn);
        using var gate = new SemaphoreSlim(BookParallelism);
        var tasks = ledgerNames.Select(async name =>
        {
            await gate.WaitAsync();
            try
            {
                var result = await _ledgerSummary.QueryAsync(new LedgerSummaryQueryRequest
                {
                    CompanyType = 1,
                    CompanyName = groupName,
                    CompanyId = 0,
                    LedgerName = name,
                    DateFrom = fyStart,
                    DateTo = asOn,
                    InterestCal = 0,
                });
                return (name, Receivable(result.ClosingBalance));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ledger closing failed for {Ledger}", name);
                return (name, 0m);
            }
            finally
            {
                gate.Release();
            }
        });

        var rows = await Task.WhenAll(tasks);
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = PartyKey(row.name);
            if (!map.TryGetValue(key, out var current) || row.Item2 > current)
                map[key] = row.Item2;
        }

        return map;
    }

    private async Task<Dictionary<string, decimal>> LoadOutstandingPivotTotalsAsync(
        string groupName,
        DateTime asOn)
    {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "sp_Representative_Outstanding_Pivot";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = CommandTimeoutSeconds;
        cmd.Parameters.AddWithValue("@CompanyName", groupName);
        cmd.Parameters.AddWithValue("@ToDate", asOn.Date);
        cmd.Parameters.AddWithValue("@intPeriod", 3);
        cmd.Parameters.AddWithValue("@Representive", DBNull.Value);
        cmd.Parameters.AddWithValue("@Currency", "Rs.");
        cmd.Parameters.AddWithValue("@G3", "Sundry Debtors");
        cmd.Parameters.AddWithValue("@G4", DBNull.Value);
        cmd.Parameters.AddWithValue("@IsLedger", 1);

        var table = new DataTable();
        await using (var reader = await cmd.ExecuteReaderAsync())
            table.Load(reader);

        if (table.Rows.Count == 0)
            return map;

        var nameCol = table.Columns.Cast<DataColumn>()
            .FirstOrDefault(c => c.ColumnName.Equals("LedgerName", StringComparison.OrdinalIgnoreCase)
                              || c.ColumnName.Equals("Name", StringComparison.OrdinalIgnoreCase));
        var totalCol = table.Columns.Cast<DataColumn>()
            .FirstOrDefault(c => c.ColumnName.Equals("Total", StringComparison.OrdinalIgnoreCase))
            ?? table.Columns.Cast<DataColumn>().LastOrDefault(c =>
                c.DataType == typeof(decimal) || c.DataType == typeof(double) || c.DataType == typeof(float));
        if (nameCol is null || totalCol is null)
        {
            _logger.LogWarning(
                "Outstanding pivot columns not mapped. Have: {Cols}",
                string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            return map;
        }

        foreach (DataRow dr in table.Rows)
        {
            var name = Convert.ToString(dr[nameCol])?.Trim() ?? "";
            if (name.Length == 0 || name.Equals("Total", StringComparison.OrdinalIgnoreCase))
                continue;
            map[PartyKey(name)] = Receivable(Convert.ToDecimal(dr[totalCol] is DBNull ? 0 : dr[totalCol]));
        }

        return map;
    }

    private static void AllocateOnAccount(List<RawBill> invoices, List<RawBill> onAccount, DateTime asOn)
    {
        if (onAccount.Count == 0)
            return;

        var credits = onAccount
            .GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => Round2(g.Sum(x => x.Remaining < 0 ? -x.Remaining : 0m)),
                StringComparer.OrdinalIgnoreCase);
        var debits = onAccount
            .GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (
                    Amount: Round2(g.Sum(x => x.Remaining > 0 ? x.Remaining : 0m)),
                    Sample: g.First()),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in invoices.GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase))
        {
            if (!credits.TryGetValue(group.Key, out var credit) || credit < MinAmount)
                continue;

            var leftover = credit;
            foreach (var bill in group
                         .OrderByDescending(b => b.BillDate ?? DateTime.MinValue)
                         .ThenByDescending(b => b.BillNo, StringComparer.OrdinalIgnoreCase))
            {
                if (leftover < 0.005m)
                    break;
                if (bill.Remaining < MinAmount)
                    continue;
                var take = Math.Min(bill.Remaining, leftover);
                take = Round2(take);
                bill.Remaining = Round2(bill.Remaining - take);
                leftover = Round2(leftover - take);
            }
        }

        foreach (var kv in debits)
        {
            if (kv.Value.Amount < MinAmount)
                continue;
            var sample = kv.Value.Sample;
            invoices.Add(new RawBill
            {
                CompanyName = sample.CompanyName,
                LedgerName = sample.LedgerName,
                BillNo = "On Account",
                BillDate = asOn,
                Remaining = kv.Value.Amount,
            });
        }
    }

    private static DateTime FinancialYearStart(DateTime asOn) =>
        asOn.Month >= 4 ? new DateTime(asOn.Year, 4, 1) : new DateTime(asOn.Year - 1, 4, 1);

    private static string ReadString(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in row)
            {
                if (!kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (kv.Value is null || kv.Value is DBNull)
                    continue;
                var s = Convert.ToString(kv.Value)?.Trim() ?? "";
                if (s.Length > 0)
                    return s;
            }
        }

        return "";
    }

    private static decimal ReadDecimal(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in row)
            {
                if (!kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (kv.Value is null || kv.Value is DBNull)
                    continue;
                try
                {
                    return Convert.ToDecimal(kv.Value);
                }
                catch
                {
                    // try next
                }
            }
        }

        return 0m;
    }

    private static DateTime? ReadDate(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in row)
            {
                if (!kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (kv.Value is null || kv.Value is DBNull)
                    continue;
                if (kv.Value is DateTime dt)
                    return dt.Date;
                if (DateTime.TryParse(Convert.ToString(kv.Value), out var parsed))
                    return parsed.Date;
            }
        }

        return null;
    }

    private static async Task<Dictionary<string, BookPair>> LoadLedgerMetaAsync(
        SqlConnection connection,
        IReadOnlyList<string> companies,
        bool includeG)
    {
        var rows = await connection.QueryAsync<BookPair>(@"
SELECT
    LTRIM(RTRIM(lm.CompanyName)) AS CompanyName,
    LTRIM(RTRIM(lm.LedgerName)) AS LedgerName,
    LTRIM(RTRIM(ISNULL(lm.Under, N''))) AS Under,
    ISNULL(NULLIF(LTRIM(RTRIM(lm.NewGSTNo)), N''), LTRIM(RTRIM(ISNULL(lm.GSTNo, N'')))) AS Gstin,
    CAST(0 AS decimal(18,2)) AS Closing
FROM LedgerMaster lm WITH (NOLOCK)
WHERE lm.CompanyName IN @Companies
  AND (
        LTRIM(RTRIM(ISNULL(lm.Under, N''))) IN @Unders
     OR (
            @IncludeG = 1
        AND (
                LTRIM(RTRIM(lm.LedgerName)) LIKE N'GST%Receivable%'
             OR LTRIM(RTRIM(lm.LedgerName)) LIKE N'Export Advance Lic.%'
             OR LTRIM(RTRIM(lm.LedgerName)) = N'Export Advance Lic'
            )
        )
      )",
            new
            {
                Companies = companies,
                Unders = TradeUnders,
                IncludeG = includeG ? 1 : 0,
            },
            commandTimeout: 60);

        var map = new Dictionary<string, BookPair>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            row.CompanyName = (row.CompanyName ?? "").Trim();
            row.LedgerName = (row.LedgerName ?? "").Trim();
            row.Under = (row.Under ?? "").Trim();
            row.Gstin = (row.Gstin ?? "").Trim();
            map[PairKey(row.CompanyName, row.LedgerName)] = row;
        }

        return map;
    }

    private static Dictionary<string, PartyState> BuildPartyMeta(
        List<RawBill> bills,
        IEnumerable<BookPair> books)
    {
        var parties = new Dictionary<string, PartyState>(StringComparer.OrdinalIgnoreCase);

        void Touch(string ledgerName, string under, string gstin)
        {
            var key = PartyKey(ledgerName);
            if (!parties.TryGetValue(key, out var state))
            {
                state = new PartyState
                {
                    PartyName = ledgerName.Trim(),
                    Under = under ?? "",
                    Gstin = gstin ?? "",
                };
                parties[key] = state;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(state.Under) && !string.IsNullOrWhiteSpace(under))
                    state.Under = under;
                if (string.IsNullOrWhiteSpace(state.Gstin) && !string.IsNullOrWhiteSpace(gstin))
                    state.Gstin = gstin;
            }
        }

        foreach (var bill in bills)
            Touch(bill.LedgerName, bill.Under, bill.Gstin);

        foreach (var book in books)
        {
            Touch(book.LedgerName, book.Under, book.Gstin);
            var key = PartyKey(book.LedgerName);
            parties[key].Book += Receivable(book.Closing);
        }

        foreach (var bill in bills)
        {
            var key = PartyKey(bill.LedgerName);
            parties[key].Original += bill.Remaining;
        }

        return parties;
    }

    private static void ApplyLifo(List<RawBill> bills, Dictionary<string, PartyState> parties)
    {
        var byParty = bills
            .GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in byParty)
        {
            if (!parties.TryGetValue(group.Key, out var state))
                continue;

            var extra = Round2(state.Original - state.Book);
            if (extra < MinAmount)
            {
                state.Status = extra <= -MinAmount ? "Non-bill balance" : "Matched";
                continue;
            }

            var leftover = extra;
            foreach (var bill in group
                         .OrderByDescending(b => b.BillDate ?? DateTime.MinValue)
                         .ThenByDescending(b => b.BillNo, StringComparer.OrdinalIgnoreCase))
            {
                if (leftover < 0.005m)
                    break;
                var take = Math.Min(bill.Remaining, leftover);
                take = Round2(take);
                bill.Allocated = take;
                leftover = Round2(leftover - take);
            }

            state.Allocated = Round2(group.Sum(b => b.Allocated));
            state.Status = leftover >= MinAmount ? "Unmatched" : "LIFO applied";
        }

        foreach (var state in parties.Values)
        {
            if (string.IsNullOrEmpty(state.Status))
                state.Status = state.Book >= MinAmount && state.Original < MinAmount
                    ? "Non-bill balance"
                    : "Matched";
        }
    }

    private static List<DebtorBillRowDto> BuildCurrentAssetRows(
        IEnumerable<BookPair> books,
        Dictionary<string, PartyState> parties,
        DateTime asOn)
    {
        var rows = new List<DebtorBillRowDto>();
        var gBooks = books
            .Where(IsGovernment)
            .GroupBy(b => PartyKey(b.LedgerName), StringComparer.OrdinalIgnoreCase);

        foreach (var group in gBooks)
        {
            var closing = Round2(group.Sum(x => Receivable(x.Closing)));
            if (closing < MinAmount)
                continue;

            var sample = group.First();
            if (!parties.TryGetValue(group.Key, out var state))
            {
                state = new PartyState
                {
                    PartyName = sample.LedgerName,
                    Under = sample.Under,
                    Gstin = sample.Gstin,
                    Book = closing,
                    Original = closing,
                    Status = "Current asset",
                };
                parties[group.Key] = state;
            }
            else
            {
                // G ledgers are not trade bills; book already includes them. Surface as a stuffed row.
                state.Original = closing;
                state.Allocated = 0;
                state.Status = "Current asset";
            }

            var (type, category) = Classify(sample.LedgerName, sample.Under);
            rows.Add(new DebtorBillRowDto
            {
                Type = type,
                Category = category,
                CompanyName = "",
                PartyName = sample.LedgerName,
                Gstin = PickGstin(group.Select(x => x.Gstin), true),
                InvoiceNo = sample.LedgerName,
                InvoiceDate = asOn.ToString("yyyy-MM-dd"),
                OriginalAmount = closing,
                AllocatedAmount = 0,
                NetAmount = closing,
                Status = "Current asset",
                Under = sample.Under,
            });
        }

        return rows;
    }

    private static List<DebtorBillRowDto> ToBillRows(List<RawBill> bills, Dictionary<string, PartyState> _)
    {
        var rows = new List<DebtorBillRowDto>(bills.Count);
        foreach (var bill in bills)
        {
            var remaining = Round2(bill.Remaining);
            var allocated = Round2(bill.Allocated);
            var net = Round2(remaining - allocated);
            var (type, category) = Classify(bill.LedgerName, bill.Under);
            var status = allocated >= MinAmount
                ? (net < MinAmount ? "Cleared by LIFO" : "Partial LIFO")
                : "Open";

            rows.Add(new DebtorBillRowDto
            {
                Type = type,
                Category = category,
                CompanyName = bill.CompanyName,
                PartyName = bill.LedgerName,
                Gstin = PickGstin([bill.Gstin], IsExportUnder(bill.Under)),
                InvoiceNo = bill.BillNo,
                InvoiceDate = bill.BillDate?.ToString("yyyy-MM-dd") ?? "",
                OriginalAmount = remaining,
                AllocatedAmount = allocated,
                NetAmount = net,
                Status = status,
                Under = bill.Under,
            });
        }

        return rows;
    }

    private static void ApplyAgeing(DebtorBillRowDto bill, DateTime asOn)
    {
        if (!DateTime.TryParse(bill.InvoiceDate, out var invoiceDate) || invoiceDate == default)
            invoiceDate = asOn;

        var days = (asOn.Date - invoiceDate.Date).Days + 1;
        if (days < 0) days = 0;
        bill.Days = days;
        bill.Ageing = AgeingBucket(days, bill.Type.Equals("Export", StringComparison.OrdinalIgnoreCase));
        bill.Ageing2 = days <= 180 ? "0-180 DAYS" : "More than 180 Days";
    }

    private static List<DebtorPivotRowDto> BuildPivot(
        List<DebtorBillRowDto> bills,
        Dictionary<string, PartyState> parties,
        Dictionary<string, string> gstinByParty)
    {
        var rows = new List<DebtorPivotRowDto>();
        var grouped = bills.GroupBy(b => PartyKey(b.PartyName), StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            parties.TryGetValue(group.Key, out var state);
            var sample = group.First();
            var row = new DebtorPivotRowDto
            {
                PartyName = sample.PartyName,
                Gstin = gstinByParty.TryGetValue(group.Key, out var gst) && !string.IsNullOrWhiteSpace(gst)
                    ? gst
                    : sample.Gstin,
                Type = sample.Type,
                Category = sample.Category,
                OriginalTotal = Round2(group.Sum(b => b.OriginalAmount)),
                AllocatedTotal = Round2(group.Sum(b => b.AllocatedAmount)),
                GrandTotal = Round2(group.Sum(b => b.NetAmount)),
                AsPerBook = Round2(state?.Book ?? 0),
                Status = state?.Status ?? "Matched",
            };

            foreach (var bill in group)
            {
                var amt = bill.NetAmount;
                if (amt < 0.005m) continue;
                switch (bill.Ageing)
                {
                    case "0-120 DAYS":
                        row.ZeroTo120 += amt;
                        break;
                    case "1-90 DAYS":
                        row.OneTo90 += amt;
                        break;
                    case "91-120DAYS":
                        row.NinetyOneTo120 += amt;
                        break;
                    case "121-180 DAYS":
                        row.OneTwentyOneTo180 += amt;
                        break;
                    default:
                        row.Over180 += amt;
                        break;
                }
            }

            row.ZeroTo120 = Round2(row.ZeroTo120);
            row.OneTo90 = Round2(row.OneTo90);
            row.NinetyOneTo120 = Round2(row.NinetyOneTo120);
            row.OneTwentyOneTo180 = Round2(row.OneTwentyOneTo180);
            row.Over180 = Round2(row.Over180);
            row.Diff = Round2(row.GrandTotal - row.AsPerBook);
            if (Math.Abs(row.Diff) < MinAmount && row.Status != "Current asset" && row.Status != "LIFO applied")
                row.Status = "Matched";
            rows.Add(row);
        }

        // Parties with book but no bills (non-bill gap, excluding G which already stuffed a row).
        foreach (var kv in parties)
        {
            if (rows.Any(r => PartyKey(r.PartyName) == kv.Key))
                continue;
            if (kv.Value.Book < MinAmount)
                continue;
            if (IsGovernmentName(kv.Value.PartyName, kv.Value.Under))
                continue;

            var (type, category) = Classify(kv.Value.PartyName, kv.Value.Under);
            rows.Add(new DebtorPivotRowDto
            {
                PartyName = kv.Value.PartyName,
                Gstin = PickGstin([kv.Value.Gstin], IsExportUnder(kv.Value.Under)),
                Type = type,
                Category = category,
                AsPerBook = Round2(kv.Value.Book),
                Diff = Round2(0 - kv.Value.Book),
                Status = "Non-bill balance",
            });
        }

        return rows
            .OrderBy(r => r.Category)
            .ThenBy(r => r.PartyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<DebtorBookDebtRowDto> BuildBookDebts(List<DebtorBillRowDto> bills)
    {
        decimal Sum(string category, bool over180) =>
            Round2(bills
                .Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Where(b => over180 ? b.Days > 180 : b.Days <= 180)
                .Sum(b => b.NetAmount));

        decimal Covered(string category)
        {
            return Round2(bills
                .Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Where(b =>
                    b.Type.Equals("Export", StringComparison.OrdinalIgnoreCase)
                        ? b.Days <= 120
                        : b.Days <= 90)
                .Sum(b => b.NetAmount));
        }

        var rows = new List<DebtorBookDebtRowDto>
        {
            Line("Upto 6 months", Sum("G", false), Sum("R", false), Sum("O", false)),
            Line("More than 6 months", Sum("G", true), Sum("R", true), Sum("O", true)),
        };

        var total = Line(
            "Total",
            rows.Sum(r => r.Government),
            rows.Sum(r => r.Associates),
            rows.Sum(r => r.Other));
        rows.Add(total);

        rows.Add(new DebtorBookDebtRowDto
        {
            Bucket = "Covered (90 domestic / 120 export)",
            Government = Covered("G"),
            Associates = Covered("R"),
            Other = Covered("O"),
            Total = Round2(Covered("G") + Covered("R") + Covered("O")),
        });

        return rows;

        static DebtorBookDebtRowDto Line(string bucket, decimal g, decimal r, decimal o) => new()
        {
            Bucket = bucket,
            Government = Round2(g),
            Associates = Round2(r),
            Other = Round2(o),
            Total = Round2(g + r + o),
        };
    }

    private static byte[] BuildWorkbook(DebtorStatementResultDto result)
    {
        using var workbook = new XLWorkbook();
        var navy = XLColor.FromHtml("#0B3A5B");
        var headerBlue = XLColor.FromHtml("#1565A8");

        WriteBillSheet(workbook, result, navy, headerBlue);
        WritePivotSheet(workbook, result, navy);
        WriteBookDebtSheet(workbook, result, navy);
        WriteBankUploadSheet(workbook, result, navy);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteBillSheet(
        XLWorkbook workbook,
        DebtorStatementResultDto result,
        XLColor navy,
        XLColor headerBlue)
    {
        var sheet = workbook.Worksheets.Add("Debtor billwise");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;

        sheet.Range(1, 1, 1, 13).Merge();
        sheet.Cell(1, 1).Value = "DEBTOR STATEMENT — BILL WISE";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = navy;
        sheet.Row(1).Height = 26;

        sheet.Range(2, 1, 2, 13).Merge();
        sheet.Cell(2, 1).Value =
            $"{result.CompanyLabel}  ·  As on {FormatDate(result.AsOn)}  ·  Freeze: as-on only  ·  Allocation: LIFO (visible, rows kept)";
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(2, 1).Style.Fill.BackgroundColor = headerBlue;

        var headers = new[]
        {
            "Type", "category", "Name of Debtors", "GST Number", "Invoice No", "Invoice Date",
            "Original", "Allocated (LIFO)", "Net Invoice Amount", "Days", "Ageing", "Ageing2", "Status",
        };
        const int headerRow = 4;
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var data = result.Bills.Select(b => new object?[]
        {
            b.Type,
            b.Category,
            b.PartyName,
            b.Gstin,
            b.InvoiceNo,
            ParseIsoDate(b.InvoiceDate),
            b.OriginalAmount,
            b.AllocatedAmount,
            b.NetAmount,
            b.Days,
            b.Ageing,
            b.Ageing2,
            b.Status,
        });
        if (result.Bills.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        StyleHeader(sheet, headerRow, headers.Length, navy);
        var last = headerRow + Math.Max(result.Bills.Count, 1);
        sheet.Range(headerRow + 1, 7, last, 9).Style.NumberFormat.Format = "#,##0.00";
        sheet.Range(headerRow + 1, 6, last, 6).Style.DateFormat.Format = "dd-mmm-yyyy";
        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns().AdjustToContents(headerRow, last, 10, 36);
    }

    private static void WritePivotSheet(XLWorkbook workbook, DebtorStatementResultDto result, XLColor navy)
    {
        var sheet = workbook.Worksheets.Add("Pivot");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Cell(1, 1).Value = "DEBTOR PIVOT — AS PER BOOK";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = navy;
        sheet.Range(1, 1, 1, 13).Merge();

        var headers = new[]
        {
            "Name of Debtors", "GST Number", "Type", "category",
            "0-120", "1-90", "91-120", "121-180", ">180",
            "Grand Total", "As per Book", "Diff", "Status",
        };
        const int headerRow = 3;
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var data = result.Pivot.Select(p => new object?[]
        {
            p.PartyName, p.Gstin, p.Type, p.Category,
            p.ZeroTo120, p.OneTo90, p.NinetyOneTo120, p.OneTwentyOneTo180, p.Over180,
            p.GrandTotal, p.AsPerBook, p.Diff, p.Status,
        });
        if (result.Pivot.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        StyleHeader(sheet, headerRow, headers.Length, navy);
        var last = headerRow + Math.Max(result.Pivot.Count, 1);
        sheet.Range(headerRow + 1, 5, last, 12).Style.NumberFormat.Format = "#,##0.00";
        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns().AdjustToContents(headerRow, last, 10, 36);
    }

    private static void WriteBookDebtSheet(XLWorkbook workbook, DebtorStatementResultDto result, XLColor navy)
    {
        var sheet = workbook.Worksheets.Add("Book Debts");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Cell(1, 1).Value = "BOOK DEBTS SUMMARY";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = navy;
        sheet.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Bucket", "Government (G)", "Associates (R)", "Other (O)", "Total" };
        const int headerRow = 3;
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var data = result.BookDebts.Select(r => new object?[]
        {
            r.Bucket, r.Government, r.Associates, r.Other, r.Total,
        });
        if (result.BookDebts.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        StyleHeader(sheet, headerRow, headers.Length, navy);
        var last = headerRow + Math.Max(result.BookDebts.Count, 1);
        sheet.Range(headerRow + 1, 2, last, 5).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
    }

    private static void WriteBankUploadSheet(XLWorkbook workbook, DebtorStatementResultDto result, XLColor navy)
    {
        var sheet = workbook.Worksheets.Add("Sundry Debtors");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Cell(1, 1).Value = "SUNDRY DEBTORS — BANK UPLOAD";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = navy;
        sheet.Range(1, 1, 1, 7).Merge();

        var headers = new[]
        {
            "Name of the Debtor", "GST Number of Debtor", "Invoice No", "Invoice Date",
            "Invoice Value", "Invoice Discounted (Y/N)", "Related party (G/R/O)",
        };
        const int headerRow = 3;
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var upload = result.Bills.Where(b => b.NetAmount >= MinAmount).ToList();
        var data = upload.Select(b => new object?[]
        {
            b.PartyName,
            b.Gstin,
            b.InvoiceNo,
            ParseIsoDate(b.InvoiceDate),
            b.NetAmount,
            "N",
            b.Category,
        });
        if (upload.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        StyleHeader(sheet, headerRow, headers.Length, navy);
        var last = headerRow + Math.Max(upload.Count, 1);
        sheet.Range(headerRow + 1, 5, last, 5).Style.NumberFormat.Format = "#,##0.00";
        sheet.Range(headerRow + 1, 4, last, 4).Style.DateFormat.Format = "dd-mmm-yyyy";
        sheet.Columns().AdjustToContents(headerRow, last, 12, 40);
    }

    private static void StyleHeader(IXLWorksheet sheet, int headerRow, int cols, XLColor navy)
    {
        var range = sheet.Range(headerRow, 1, headerRow, cols);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = navy;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private async Task<IReadOnlyList<FactoryRow>> GetFactoryRowsAsync()
    {
        const string key = "debtor-statement-factory-rows-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<FactoryRow>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<FactoryRow>(@"
SELECT fi.srno AS SrNo,
       LTRIM(RTRIM(fi.Name)) AS Name,
       LTRIM(RTRIM(ISNULL(fi.GroupName, N''))) AS GroupName
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, '') <> ''
ORDER BY fi.Name")).ToList();

        _cache.Set(key, (IReadOnlyList<FactoryRow>)rows, TimeSpan.FromHours(6));
        return rows;
    }

    private static List<string> ResolveCompanies(string company, IReadOnlyList<FactoryRow> factories)
    {
        if (company.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
        {
            var group = company[2..].Trim();
            return factories
                .Where(f => f.GroupName.Equals(group, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Name)
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (company.StartsWith("C-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(company[2..].Trim(), out var id))
        {
            return factories
                .Where(f => f.SrNo == id)
                .Select(f => f.Name)
                .Where(n => n.Length > 0)
                .ToList();
        }

        var named = factories.FirstOrDefault(f =>
            f.Name.Equals(company, StringComparison.OrdinalIgnoreCase));
        return named is null ? [] : [named.Name];
    }

    private static string CompanyLabel(string company, IReadOnlyList<FactoryRow> factories)
    {
        if (company.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
            return company[2..] + " (Group)";
        if (company.StartsWith("C-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(company[2..].Trim(), out var id))
            return factories.FirstOrDefault(f => f.SrNo == id)?.Name ?? company;
        return company;
    }

    private static void NormalizeBillSign(List<RawBill> bills)
    {
        if (bills.Count == 0) return;
        if (bills.Any(b => b.Remaining > 0))
        {
            bills.RemoveAll(b => b.Remaining < MinAmount);
            return;
        }

        foreach (var bill in bills)
            bill.Remaining = Math.Abs(bill.Remaining);
    }

    private static (string Type, string Category) Classify(string partyName, string under)
    {
        if (IsGovernmentName(partyName, under))
            return ("Current Assets", "G");
        if (IsRelatedParty(partyName))
            return ("Associate", "R");
        if (IsExportUnder(under))
            return ("Export", "O");
        return ("Domestic", "O");
    }

    private static bool IsRelatedParty(string partyName)
    {
        var name = CompactName(partyName).ToLowerInvariant();
        if (RelatedPartyNeedles.Any(n => name.Contains(n, StringComparison.Ordinal)))
            return true;
        return name.Contains("oswal extrusion", StringComparison.Ordinal) &&
               name.Contains("kasez", StringComparison.Ordinal);
    }

    /// <summary>
    /// Sister-unit AR ledgers (PIL/KP/HCP/Oswal HO-Unit sales). Keep Polyfilms and Oswal KASEZ.
    /// </summary>
    private static bool IsIntercompanySalesLedger(string partyName)
    {
        if (IsRelatedParty(partyName))
            return false;

        var name = CompactName(partyName).ToLowerInvariant();
        if (name.Contains("oswal extrusion", StringComparison.Ordinal))
            return true;
        if (IntercompanyNeedles.Any(n => name.Contains(n, StringComparison.Ordinal)))
            return true;
        if (name.Contains("plastene india", StringComparison.Ordinal) &&
            name.Contains("sales", StringComparison.Ordinal) &&
            (name.Contains("unit", StringComparison.Ordinal) || name.Contains(" ho", StringComparison.Ordinal) || name.Contains("-ho", StringComparison.Ordinal)))
            return true;
        return false;
    }

    private static bool IsExportUnder(string under) =>
        (under ?? "").Contains("Overseas", StringComparison.OrdinalIgnoreCase);

    private static bool IsGovernment(BookPair pair) => IsGovernmentName(pair.LedgerName, pair.Under);

    private static bool IsGovernmentName(string partyName, string under)
    {
        var name = partyName ?? "";
        var u = under ?? "";
        var compact = CompactName(name);
        if (compact.StartsWith("GST Receivable", StringComparison.OrdinalIgnoreCase))
            return true;
        if (compact.StartsWith("Export Advance Lic.", StringComparison.OrdinalIgnoreCase) ||
            compact.Equals("Export Advance Lic", StringComparison.OrdinalIgnoreCase))
            return true;
        return u.Equals("GST Refund", StringComparison.OrdinalIgnoreCase);
    }

    private static string AgeingBucket(int days, bool isExport)
    {
        if (isExport)
        {
            if (days <= 120) return "0-120 DAYS";
            if (days <= 180) return "121-180 DAYS";
            return "More than 180 Days";
        }

        if (days <= 90) return "1-90 DAYS";
        if (days <= 120) return "91-120DAYS";
        if (days <= 180) return "121-180 DAYS";
        return "More than 180 Days";
    }

    private static string PickGstin(IEnumerable<string?> values, bool isExport)
    {
        var real = values
            .Select(v => (v ?? "").Trim())
            .Where(v => v.Length > 0 &&
                        !v.Equals(DebtorStatementDefaults.ExportGstin, StringComparison.OrdinalIgnoreCase) &&
                        !v.Equals(DebtorStatementDefaults.DomesticGstin, StringComparison.OrdinalIgnoreCase) &&
                        !v.Equals("N.A.", StringComparison.OrdinalIgnoreCase) &&
                        !v.Equals("NA", StringComparison.OrdinalIgnoreCase))
            .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(real))
            return real!;
        return isExport ? DebtorStatementDefaults.ExportGstin : DebtorStatementDefaults.DomesticGstin;
    }

    private static decimal Receivable(decimal closing) => closing > 0 ? Round2(closing) : 0m;

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string CompactName(string name) =>
        string.Join(" ", (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string PartyKey(string name) => CompactName(name);

    private static string PairKey(string company, string ledger) => $"{company.Trim()}\u001f{ledger.Trim()}";

    private static string ResultCacheKey(string company, DateTime asOn, bool includeG) =>
        $"debtor-statement-v11|{company}|{asOn:yyyy-MM-dd}|{includeG}";

    private static DateTime PreviousMonthEnd(DateTime today) =>
        new DateTime(today.Year, today.Month, 1).AddDays(-1);

    private static string FormatDate(string iso)
    {
        if (DateTime.TryParse(iso, out var d))
            return d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        return iso;
    }

    private static DateTime? ParseIsoDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, out var d)) return d.Date;
        return null;
    }

    private sealed class FactoryRow
    {
        public int SrNo { get; set; }
        public string Name { get; set; } = "";
        public string GroupName { get; set; } = "";
    }

    private sealed class RawBill
    {
        public string CompanyName { get; set; } = "";
        public string LedgerName { get; set; } = "";
        public string Under { get; set; } = "";
        public string Gstin { get; set; } = "";
        public string BillNo { get; set; } = "";
        public DateTime? BillDate { get; set; }
        public decimal Remaining { get; set; }
        public decimal Allocated { get; set; }
    }

    private sealed class BookPair
    {
        public string CompanyName { get; set; } = "";
        public string LedgerName { get; set; } = "";
        public string Under { get; set; } = "";
        public string Gstin { get; set; } = "";
        public decimal Closing { get; set; }
    }

    private sealed class PartyState
    {
        public string PartyName { get; set; } = "";
        public string Under { get; set; } = "";
        public string Gstin { get; set; } = "";
        public decimal Original { get; set; }
        public decimal Book { get; set; }
        public decimal Allocated { get; set; }
        public string Status { get; set; } = "";
    }

}
