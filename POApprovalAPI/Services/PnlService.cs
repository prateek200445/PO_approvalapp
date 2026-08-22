using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class PnlService
{
    private const int TimeoutSeconds = 180;
    private const double Lacs = 100_000d;

    private static readonly string[] StockCategories =
    {
        "RawMaterial", "Packing", "WIP", "FinishedGoods", "Traded", "Stores",
    };

    private static readonly Dictionary<string, string> StockLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RawMaterial"] = "Raw materials",
        ["Packing"] = "Packing materials",
        ["WIP"] = "WIP",
        ["FinishedGoods"] = "Finished goods",
        ["Traded"] = "Stock in trade",
        ["Stores"] = "Stores, spares & consumables",
    };

    private static readonly Dictionary<string, string> HeadAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sale of finished goods"] = "Sale of finished goods",
        ["sale of traded goods"] = "Sale of traded goods",
        ["income from jobwork charges"] = "Income from Jobwork charges",
        ["export incentives"] = "Export incentives",
        ["commission income"] = "Commission income",
        ["scrap sales"] = "Scrap sales",
        ["sales of scrap & wastage"] = "Scrap sales",
        ["wind mill income"] = "Wind mill income",
        ["- on fixed deposits with banks"] = "- on fixed deposits with Banks",
        ["interest received from bank"] = "- on fixed deposits with Banks",
        ["interest recd from others"] = "- from others",
        ["interest received from others"] = "- from others",
        ["rent income"] = "Rent Income",
        ["interest received from customers"] = "Interest Received From Customers",
        ["profit on sale of fixed assets"] = "Profit on sale of fixed assets",
        ["profit on sale of investments"] = "Profit on sale of investments",
        ["sundry balance written off"] = "Sundry balance written off",
        ["miscellaneous income"] = "Miscellaneous income",
        ["salaries, wages and bonus"] = "Salaries, wages and  bonus",
        ["wages and salary"] = "Salaries, wages and  bonus",
        ["contribution to provident and other funds"] = "Contribution to provident and other funds",
        ["gratuity expenses"] = "Gratuity Expenses",
        ["staff welfare expenses"] = "Staff welfare expenses",
        ["labour charges"] = "Labour charges",
        ["power and fuel"] = "Power and fuel",
        ["jobwork charges"] = "Jobwork charges",
        ["other manufacturing expenses"] = "Other Manufacturing Expenses",
        ["advertising and sales promotion expense"] = "Advertising and sales promotion expense",
        ["advertisement & sales promotion expense"] = "Advertising and sales promotion expense",
        ["travelling expense"] = "Travelling expense",
        ["travelling & conveyance"] = "Travelling expense",
        ["brokerage & commission"] = "Brokerage & commission",
        ["conveyance, vehicle running and traveling expenses"] = "Conveyance, vehicle running and traveling expenses",
        ["freight outward and c & f charges"] = "Freight Outward and C & F Charges",
        ["freight outward expense"] = "Freight Outward and C & F Charges",
        ["insurance"] = "Insurance",
        ["insurance expenses"] = "Insurance",
        ["miscellaneous expense"] = "Miscellaneous Expense",
        ["miscellaneous expenses"] = "Miscellaneous Expense",
        ["other administrative expenses"] = "Other Administrative Expenses",
        ["exchange rate difference"] = "Exchange Rate Difference",
        ["depreciation expenses"] = "Depreciation",
        ["bank charges & commission"] = "Bank Charges & Commission",
        ["interest expense - other"] = "Interest Expense - Other",
        ["interest on working capital & term loan"] = "Interest on Working Capital & Term Loan",
        ["income tax"] = "Income Tax",
    };

    private readonly DatabaseService _database;

    public PnlService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<PnlCompanyOption>> GetCompaniesAsync()
    {
        using var connection = _database.CreateConnection();
        var factories = (await connection.QueryAsync<(string Name, string? GroupName)>(@"
SELECT Name, GroupName
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(Name, '') <> ''
ORDER BY Name")).ToList();

        var options = new List<PnlCompanyOption>
        {
            new() { Value = "All Companies", Label = "All Companies", Kind = "all" },
        };

        foreach (var group in factories
                     .Select(f => (f.GroupName ?? "").Trim())
                     .Where(g => g.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(new PnlCompanyOption
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                Kind = "group",
            });
        }

        foreach (var factory in factories)
        {
            options.Add(new PnlCompanyOption
            {
                Value = factory.Name.Trim(),
                Label = factory.Name.Trim(),
                Kind = "company",
            });
        }

        return options;
    }

    public async Task<PnlIncomeExpenseResult> GetIncomeExpenseAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var companies = await ResolveCompaniesAsync(company);
        var rows = await LoadBooksPlusProvisionAsync(companies, dateFrom, dateTo);

        var heads = rows
            .GroupBy(r => (Category: r.Category, Head: CanonHead(r.Head)))
            .Select(g => new PnlHeadGroup
            {
                Category = g.Key.Category,
                Head = g.Key.Head,
                Amount = g.Sum(x => x.Amount),
                AmountLacs = Round2(g.Sum(x => x.Amount) / Lacs),
                Ledgers = g
                    .GroupBy(x => x.LedgerName, StringComparer.OrdinalIgnoreCase)
                    .Select(lg => new PnlLedgerLine
                    {
                        LedgerName = lg.Key,
                        Amount = lg.Sum(x => x.Amount),
                        AmountLacs = Round2(lg.Sum(x => x.Amount) / Lacs),
                    })
                    .OrderByDescending(x => Math.Abs(x.Amount))
                    .ToList(),
            })
            .OrderBy(h => h.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Head, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PnlIncomeExpenseResult
        {
            Company = company,
            DateFrom = dateFrom.ToString("yyyy-MM-dd"),
            DateTo = dateTo.ToString("yyyy-MM-dd"),
            IncomeLacs = Round2(heads.Where(h => h.Category.Equals("Income", StringComparison.OrdinalIgnoreCase)).Sum(h => h.Amount) / Lacs),
            ExpenseLacs = Round2(heads.Where(h => h.Category.Equals("Expense", StringComparison.OrdinalIgnoreCase)).Sum(h => h.Amount) / Lacs),
            Heads = heads,
        };
    }

    public async Task<PnlProvisionState> GetProvisionsAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter provision.");

        var names = await ResolveCompaniesAsync(company);
        var companyName = names.First();
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<PnlProvisionRow>(@"
SELECT LTRIM(RTRIM(Ledgername)) AS LedgerName,
       SUM(ISNULL(Amount, 0)) AS Amount
FROM Provisioning WITH (NOLOCK)
WHERE LTRIM(RTRIM(companyname)) = @Company
  AND sysdate BETWEEN @From AND @To
GROUP BY LTRIM(RTRIM(Ledgername))
ORDER BY LTRIM(RTRIM(Ledgername))",
            new { Company = companyName, From = monthStart, To = monthEnd },
            commandTimeout: TimeoutSeconds)).ToList();

        foreach (var row in rows)
            row.AmountLacs = Round4(row.Amount / Lacs);

        var ledgers = (await connection.QueryAsync<string>(@"
SELECT DISTINCT LTRIM(RTRIM(LedgerName))
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName = @Company
  AND Category IN ('Expense','Income')
  AND ISNULL(LedgerName, '') <> ''
ORDER BY 1",
            new { Company = companyName })).ToList();

        return new PnlProvisionState
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            Rows = rows,
            LedgerOptions = ledgers,
        };
    }

    public async Task SaveProvisionsAsync(PnlProvisionSaveRequest request)
    {
        var monthStart = ParseMonth(request.Month);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save provision.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        var rows = (request.Rows ?? new List<PnlProvisionRow>())
            .Where(r => !string.IsNullOrWhiteSpace(r.LedgerName))
            .Select(r => new
            {
                Ledger = r.LedgerName.Trim(),
                Amount = r.Amount != 0 ? r.Amount : r.AmountLacs * Lacs,
            })
            .Where(r => Math.Abs(r.Amount) > 0.0001)
            .ToList();

        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync(@"
DELETE FROM Provisioning
WHERE LTRIM(RTRIM(companyname)) = @Company
  AND sysdate BETWEEN @From AND @To",
            new { Company = companyName, From = monthStart, To = monthEnd },
            tx);

        var nextId = await connection.ExecuteScalarAsync<int>(@"
SELECT ISNULL(MAX(transid), 0) FROM Provisioning",
            transaction: tx);

        foreach (var row in rows)
        {
            nextId++;
            await connection.ExecuteAsync(@"
INSERT INTO Provisioning (companyname, sysdate, Ledgername, Amount, remarks, transid)
VALUES (@Company, @Date, @Ledger, @Amount, @Remarks, @TransId)",
                new
                {
                    Company = companyName,
                    Date = monthStart,
                    Ledger = row.Ledger,
                    Amount = row.Amount,
                    Remarks = "App P&L provision",
                    TransId = nextId,
                },
                tx);
        }

        tx.Commit();
    }

    public async Task<PnlStockState> GetStockAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter stock value.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureStockTableAsync();
        var prev = monthStart.AddMonths(-1);

        using var connection = _database.CreateConnection();
        var stored = (await connection.QueryAsync<(string Category, DateTime StockMonth, double AmountLacs)>(@"
SELECT Category, StockMonth, AmountLacs
FROM AppPnlStockValue WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company
  AND StockMonth IN (@Prev, @Month)",
            new { Company = companyName, Prev = prev, Month = monthStart })).ToList();

        var rows = StockCategories.Select(cat =>
        {
            var opening = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth == prev);
            var closing = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth == monthStart);
            return new PnlStockRow
            {
                Category = cat,
                Label = StockLabels[cat],
                OpeningLacs = opening.Category == null ? 0 : opening.AmountLacs,
                ClosingLacs = closing.Category == null ? 0 : closing.AmountLacs,
            };
        }).ToList();

        return new PnlStockState
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            Rows = rows,
        };
    }

    public async Task<PnlStockYearState> GetStockYearAsync(string company, DateTime monthInFy)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter stock value.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureStockTableAsync();
        var fyStart = FyStartYear(monthInFy);
        var openingMonth = new DateTime(fyStart, 3, 1);
        var fyMonths = FyMonths(fyStart);
        var loadMonths = fyMonths.Concat(new[] { openingMonth }).Distinct().ToList();

        using var connection = _database.CreateConnection();
        var stored = (await connection.QueryAsync<(string Category, DateTime StockMonth, double AmountLacs)>(@"
SELECT Category, StockMonth, AmountLacs
FROM AppPnlStockValue WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company
  AND StockMonth IN @Months",
            new { Company = companyName, Months = loadMonths })).ToList();

        double Cell(string cat, DateTime month)
        {
            var hit = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth.Date == month.Date);
            return hit.Category == null ? 0 : hit.AmountLacs;
        }

        var columns = fyMonths.Select(m => new PnlStockYearColumn
        {
            Key = m.ToString("yyyy-MM"),
            Label = m.ToString("MMM-yy"),
        }).ToList();

        var rows = StockCategories.Select(cat => new PnlStockYearRow
        {
            Category = cat,
            Label = StockLabels[cat],
            OpeningLacs = Cell(cat, openingMonth),
            Months = fyMonths.ToDictionary(m => m.ToString("yyyy-MM"), m => Cell(cat, m)),
        }).ToList();

        return new PnlStockYearState
        {
            Company = companyName,
            FyStart = fyStart,
            FyLabel = $"Apr-{fyStart % 100:00} to Mar-{(fyStart + 1) % 100:00}",
            Columns = columns,
            Rows = rows,
        };
    }

    public async Task SaveStockYearAsync(PnlStockYearSaveRequest request)
    {
        if (request.FyStart < 2000 || request.FyStart > 2100)
            throw new ArgumentException("Invalid financial year.");
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save stock value.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        await EnsureStockTableAsync();
        var openingMonth = new DateTime(request.FyStart, 3, 1);
        var fyMonths = FyMonths(request.FyStart);

        using var connection = _database.CreateConnection();
        foreach (var row in request.Rows ?? new List<PnlStockYearRow>())
        {
            if (string.IsNullOrWhiteSpace(row.Category) || row.Category.Equals("Total", StringComparison.OrdinalIgnoreCase))
                continue;
            await UpsertStockAsync(connection, companyName, openingMonth, row.Category, row.OpeningLacs);
            foreach (var month in fyMonths)
            {
                var key = month.ToString("yyyy-MM");
                var amount = 0d;
                if (row.Months != null)
                    row.Months.TryGetValue(key, out amount);
                await UpsertStockAsync(connection, companyName, month, row.Category, amount);
            }
        }
    }

    private static int FyStartYear(DateTime month) => month.Month >= 4 ? month.Year : month.Year - 1;

    private static List<DateTime> FyMonths(int fyStart) =>
        Enumerable.Range(0, 12).Select(i => new DateTime(fyStart, 4, 1).AddMonths(i)).ToList();

    public async Task SaveStockAsync(PnlStockSaveRequest request)
    {
        var monthStart = ParseMonth(request.Month);
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save stock value.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        await EnsureStockTableAsync();
        var prev = monthStart.AddMonths(-1);

        using var connection = _database.CreateConnection();
        foreach (var row in request.Rows ?? new List<PnlStockRow>())
        {
            if (string.IsNullOrWhiteSpace(row.Category))
                continue;
            await UpsertStockAsync(connection, companyName, prev, row.Category, row.OpeningLacs);
            await UpsertStockAsync(connection, companyName, monthStart, row.Category, row.ClosingLacs);
        }
    }

    public async Task<PnlStatementResult> GetStatementAsync(string company, DateTime monthStart)
    {
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var ytdFrom = monthStart.Month >= 4
            ? new DateTime(monthStart.Year, 4, 1)
            : new DateTime(monthStart.Year - 1, 4, 1);

        var companies = await ResolveCompaniesAsync(company);
        var monthBooks = await LoadBooksPlusProvisionAsync(companies, monthStart, monthEnd);
        var ytdBooks = await LoadBooksPlusProvisionAsync(companies, ytdFrom, monthEnd);

        var monthHeads = SumHeads(monthBooks);
        var ytdHeads = SumHeads(ytdBooks);

        var stock = IsAggregate(company)
            ? new PnlStockState { Rows = StockCategories.Select(c => new PnlStockRow { Category = c, Label = StockLabels[c] }).ToList() }
            : await GetStockAsync(company, monthStart);

        var ytdStockOpening = IsAggregate(company)
            ? stock
            : await GetStockAsync(company, ytdFrom);

        var monthComputed = ComputeInventory(monthHeads, stock, useYtdOpening: false);
        var ytdComputed = ComputeInventory(ytdHeads, new PnlStockState
        {
            Rows = StockCategories.Select(cat => new PnlStockRow
            {
                Category = cat,
                OpeningLacs = ytdStockOpening.Rows.First(r => r.Category == cat).OpeningLacs,
                ClosingLacs = stock.Rows.First(r => r.Category == cat).ClosingLacs,
            }).ToList(),
        }, useYtdOpening: true);

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = BuildTemplate(monthHeads, ytdHeads, monthComputed, ytdComputed, used);

        var unmapped = monthHeads
            .Where(kv => !used.Contains(kv.Key) && !IsPurchaseFeed(kv.Key) && Math.Abs(kv.Value) > 0.5)
            .Select(kv => new PnlHeadGroup
            {
                Head = kv.Key,
                Amount = kv.Value,
                AmountLacs = Round2(kv.Value / Lacs),
            })
            .OrderBy(h => h.Head)
            .ToList();

        var ebitda = rows.FirstOrDefault(r => r.Id == "ebitda")?.MonthLacs ?? 0;
        var pbt = rows.FirstOrDefault(r => r.Id == "pbt")?.MonthLacs ?? 0;
        var stockIncomplete = stock.Rows.All(r => r.OpeningLacs == 0 && r.ClosingLacs == 0);

        return new PnlStatementResult
        {
            Company = company,
            Month = monthStart.ToString("yyyy-MM"),
            DateFrom = monthStart.ToString("yyyy-MM-dd"),
            DateTo = monthEnd.ToString("yyyy-MM-dd"),
            YtdFrom = ytdFrom.ToString("yyyy-MM-dd"),
            EbitdaLacs = ebitda,
            PbtLacs = pbt,
            StockIncomplete = stockIncomplete,
            Rows = rows,
            Unmapped = unmapped,
        };
    }

    private async Task<List<BookRow>> LoadBooksPlusProvisionAsync(
        IReadOnlyList<string> companies,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var prevEnd = dateFrom.AddDays(-1);
        var prevStart = new DateTime(prevEnd.Year, prevEnd.Month, 1);
        var allCompanies = companies.Count == 0;
        var companyFilter = allCompanies ? new List<string> { "__none__" } : companies.ToList();

        using var connection = _database.CreateConnection();
        var books = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N''))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.LedgerName, N''))) AS LedgerName,
    SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM vw_LedgerSummary VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.LedgerName
   AND L.CompanyName = VL.CompanyName
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.Date BETWEEN @DateFrom AND @DateTo
  AND L.Category IN (N'Expense', N'Income')
  AND (@AllCompanies = 1 OR VL.CompanyName IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N''))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.LedgerName, N'')))",
            new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            },
            commandTimeout: TimeoutSeconds)).ToList();

        var provisionNow = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.Ledgername, N''))) AS LedgerName,
    SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM Provisioning VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.Ledgername
   AND L.CompanyName = VL.companyname
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.sysdate BETWEEN @DateFrom AND @DateTo
  AND (@AllCompanies = 1 OR VL.companyname IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.Ledgername, N'')))",
            new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            },
            commandTimeout: TimeoutSeconds)).ToList();

        var provisionPrev = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.Ledgername, N''))) AS LedgerName,
    -SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM Provisioning VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.Ledgername
   AND L.CompanyName = VL.companyname
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.sysdate BETWEEN @DateFrom AND @DateTo
  AND (@AllCompanies = 1 OR VL.companyname IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.Ledgername, N'')))",
            new
            {
                DateFrom = prevStart.Date,
                DateTo = prevEnd.Date,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            },
            commandTimeout: TimeoutSeconds)).ToList();

        books.AddRange(provisionNow);
        books.AddRange(provisionPrev);
        return books;
    }

    private async Task<List<string>> ResolveCompaniesAsync(string company)
    {
        if (string.IsNullOrWhiteSpace(company) || IsAll(company))
            return new List<string>();

        using var connection = _database.CreateConnection();
        if (company.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
        {
            var group = company[2..].Trim();
            var names = (await connection.QueryAsync<string>(@"
SELECT Name FROM FactoryInfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(ISNULL(GroupName, N''))) = @Group",
                new { Group = group })).ToList();
            return names.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        }

        return new List<string> { company.Trim() };
    }

    private async Task EnsureStockTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF OBJECT_ID(N'dbo.AppPnlStockValue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppPnlStockValue (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName VARCHAR(150) NOT NULL,
        StockMonth DATE NOT NULL,
        Category VARCHAR(40) NOT NULL,
        AmountLacs FLOAT NOT NULL CONSTRAINT DF_AppPnlStockValue_Amount DEFAULT (0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_AppPnlStockValue_Updated DEFAULT (GETDATE()),
        CONSTRAINT UQ_AppPnlStockValue UNIQUE (CompanyName, StockMonth, Category)
    );
END");
    }

    private static async Task UpsertStockAsync(
        System.Data.IDbConnection connection,
        string company,
        DateTime month,
        string category,
        double amountLacs)
    {
        await connection.ExecuteAsync(@"
IF EXISTS (
    SELECT 1 FROM AppPnlStockValue
    WHERE CompanyName = @Company AND StockMonth = @Month AND Category = @Category
)
    UPDATE AppPnlStockValue
    SET AmountLacs = @Amount, UpdatedAt = GETDATE()
    WHERE CompanyName = @Company AND StockMonth = @Month AND Category = @Category
ELSE
    INSERT INTO AppPnlStockValue (CompanyName, StockMonth, Category, AmountLacs, UpdatedAt)
    VALUES (@Company, @Month, @Category, @Amount, GETDATE())",
            new
            {
                Company = company,
                Month = month,
                Category = category,
                Amount = amountLacs,
            });
    }

    private static Dictionary<string, double> SumHeads(IEnumerable<BookRow> rows)
    {
        return rows
            .GroupBy(r => CanonHead(r.Head), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount), StringComparer.OrdinalIgnoreCase);
    }

    private static InventoryBits ComputeInventory(
        Dictionary<string, double> heads,
        PnlStockState stock,
        bool useYtdOpening)
    {
        _ = useYtdOpening;
        double HeadSum(params string[] keys) =>
            heads.Where(kv => keys.Any(k => kv.Key.Equals(k, StringComparison.OrdinalIgnoreCase) || ContainsPurchase(kv.Key, k)))
                .Sum(kv => kv.Value);

        var rmPurch = HeadSum("Add: Purchase of raw materials", "Add : Purchase of Raw Material", "Purchase of raw materials");
        var pmPurch = HeadSum("Add: Purchase of packing materials", "Add : Purchase of Packing Material", "Purchase of packing materials");
        var purchExp = HeadSum("Add: Purchase expenses", "Add : Purchase Expenses", "Purchase expenses");
        var tradedPurch = HeadSum("Add: Purchase of traded goods", "Purchase of traded goods");
        var storesPurch = HeadSum("Add: Purchase of Stores, Spares & Consumables", "Purchase of Stores, Spares & Consumables");

        double Stock(string cat, bool opening)
        {
            var row = stock.Rows.FirstOrDefault(r => r.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
            return (opening ? row?.OpeningLacs : row?.ClosingLacs) ?? 0;
        }

        return new InventoryBits
        {
            Materials = rmPurch + pmPurch + purchExp
                        + Stock("RawMaterial", true) * Lacs + Stock("Packing", true) * Lacs
                        - Stock("RawMaterial", false) * Lacs - Stock("Packing", false) * Lacs,
            Traded = tradedPurch + Stock("Traded", true) * Lacs - Stock("Traded", false) * Lacs,
            InventoryChange = Stock("WIP", true) * Lacs + Stock("FinishedGoods", true) * Lacs
                              - Stock("WIP", false) * Lacs - Stock("FinishedGoods", false) * Lacs,
            Stores = storesPurch + Stock("Stores", true) * Lacs - Stock("Stores", false) * Lacs,
        };
    }

    private static bool ContainsPurchase(string head, string key)
    {
        var h = head.ToLowerInvariant();
        var k = key.ToLowerInvariant();
        if (k.Contains("raw") && h.Contains("purchase") && h.Contains("raw"))
            return true;
        if (k.Contains("packing") && h.Contains("purchase") && h.Contains("packing"))
            return true;
        if (k.Contains("purchase expenses") && h.Contains("purchase") && h.Contains("expense"))
            return true;
        if (k.Contains("traded") && h.Contains("purchase") && h.Contains("traded"))
            return true;
        if (k.Contains("stores") && h.Contains("purchase") && (h.Contains("store") || h.Contains("spare")))
            return true;
        return false;
    }

    private static bool IsPurchaseFeed(string head)
    {
        var h = head.ToLowerInvariant();
        return h.Contains("purchase of raw")
            || h.Contains("purchase of packing")
            || h.Contains("purchase expenses")
            || h.Contains("purchase of traded")
            || h.Contains("purchase of stores")
            || h.Contains("purchase of stock");
    }

    private static List<PnlStatementRow> BuildTemplate(
        Dictionary<string, double> month,
        Dictionary<string, double> ytd,
        InventoryBits monthInv,
        InventoryBits ytdInv,
        HashSet<string> used)
    {
        double Line(string name)
        {
            used.Add(name);
            month.TryGetValue(name, out var m);
            return m;
        }

        double Ytd(string name)
        {
            ytd.TryGetValue(name, out var v);
            return v;
        }

        var built = new List<PnlStatementRow>();
        void H(string id, string label) =>
            built.Add(new PnlStatementRow { Id = id, Label = label, Kind = "header" });

        void L(string id, string label)
        {
            var m = Line(label);
            var y = Ytd(label);
            built.Add(Make(id, label, "line", m, y));
        }

        void C(string id, string label, double m, double y) =>
            built.Add(Make(id, label, "computed", m, y));

        void T(string id, string label, double m, double y) =>
            built.Add(Make(id, label, "total", m, y));

        H("rev", "Revenue from operations");
        L("fg", "Sale of finished goods");
        L("traded", "Sale of traded goods");
        var revM = Val(built, "fg") + Val(built, "traded");
        var revY = YtdVal(built, "fg") + YtdVal(built, "traded");
        T("revTotal", "Total Revenue from operations", revM, revY);

        H("svc", "Sale of services");
        L("job", "Income from Jobwork charges");
        T("svcTotal", "Total Sales of services", Val(built, "job"), YtdVal(built, "job"));

        H("oop", "Other operating revenue");
        L("export", "Export incentives");
        L("commission", "Commission income");
        L("scrap", "Scrap sales");
        T("oopTotal", "Total Other operating revenue",
            Val(built, "export") + Val(built, "commission") + Val(built, "scrap"),
            YtdVal(built, "export") + YtdVal(built, "commission") + YtdVal(built, "scrap"));

        H("oi", "Other income");
        L("wind", "Wind mill income");
        L("intFd", "- on fixed deposits with Banks");
        L("intOth", "- from others");
        L("rent", "Rent Income");
        L("intCust", "Interest Received From Customers");
        L("pfa", "Profit on sale of fixed assets");
        L("pinv", "Profit on sale of investments");
        L("sundry", "Sundry balance written off");
        L("misc", "Miscellaneous income");
        T("oiTotal", "Total Other income",
            Val(built, "wind") + Val(built, "intFd") + Val(built, "intOth") + Val(built, "rent")
            + Val(built, "intCust") + Val(built, "pfa") + Val(built, "pinv") + Val(built, "sundry") + Val(built, "misc"),
            YtdVal(built, "wind") + YtdVal(built, "intFd") + YtdVal(built, "intOth") + YtdVal(built, "rent")
            + YtdVal(built, "intCust") + YtdVal(built, "pfa") + YtdVal(built, "pinv") + YtdVal(built, "sundry") + YtdVal(built, "misc"));

        var turnM = Val(built, "revTotal") + Val(built, "svcTotal") + Val(built, "oopTotal") + Val(built, "oiTotal");
        var turnY = YtdVal(built, "revTotal") + YtdVal(built, "svcTotal") + YtdVal(built, "oopTotal") + YtdVal(built, "oiTotal");
        T("turnover", "Total Turnover", turnM, turnY);

        H("cogs", "Cost of Goods");
        C("materials", "Cost of materials consumed", monthInv.Materials, ytdInv.Materials);
        C("sit", "Purchase of stock-in-trade", monthInv.Traded, ytdInv.Traded);
        C("invchg", "Changes in inventory of finished goods and work-in-progress", monthInv.InventoryChange, ytdInv.InventoryChange);
        C("stores", "Stores, Spares & Consumables", monthInv.Stores, ytdInv.Stores);
        T("cogsTotal", "Total Cost of goods",
            Val(built, "materials") + Val(built, "sit") + Val(built, "invchg") + Val(built, "stores"),
            YtdVal(built, "materials") + YtdVal(built, "sit") + YtdVal(built, "invchg") + YtdVal(built, "stores"));

        H("emp", "Employee benefit expenses");
        L("sal", "Salaries, wages and  bonus");
        L("pf", "Contribution to provident and other funds");
        L("grat", "Gratuity Expenses");
        L("welfare", "Staff welfare expenses");
        L("labour", "Labour charges");
        T("empTotal", "Total Employee benefit expenses",
            Val(built, "sal") + Val(built, "pf") + Val(built, "grat") + Val(built, "welfare") + Val(built, "labour"),
            YtdVal(built, "sal") + YtdVal(built, "pf") + YtdVal(built, "grat") + YtdVal(built, "welfare") + YtdVal(built, "labour"));

        H("mfg", "Manufacturing Expenses");
        L("power", "Power and fuel");
        L("jw", "Jobwork charges");
        L("omfg", "Other Manufacturing Expenses");
        T("mfgTotal", "Total Manufacturing Expenses",
            Val(built, "power") + Val(built, "jw") + Val(built, "omfg"),
            YtdVal(built, "power") + YtdVal(built, "jw") + YtdVal(built, "omfg"));

        H("sell", "Selling and Distribution Expenses");
        L("ads", "Advertising and sales promotion expense");
        L("travel", "Travelling expense");
        L("broker", "Brokerage & commission");
        L("conv", "Conveyance, vehicle running and traveling expenses");
        L("freight", "Freight Outward and C & F Charges");
        T("sellTotal", "Total Selling and Distribution Expenses",
            Val(built, "ads") + Val(built, "travel") + Val(built, "broker") + Val(built, "conv") + Val(built, "freight"),
            YtdVal(built, "ads") + YtdVal(built, "travel") + YtdVal(built, "broker") + YtdVal(built, "conv") + YtdVal(built, "freight"));

        H("admin", "Administrative Expenses");
        L("ins", "Insurance");
        L("miscExp", "Miscellaneous Expense");
        L("oadm", "Other Administrative Expenses");
        L("fx", "Exchange Rate Difference");
        T("adminTotal", "Total Administrative Expenses",
            Val(built, "ins") + Val(built, "miscExp") + Val(built, "oadm") + Val(built, "fx"),
            YtdVal(built, "ins") + YtdVal(built, "miscExp") + YtdVal(built, "oadm") + YtdVal(built, "fx"));

        var soldM = Val(built, "cogsTotal") + Val(built, "empTotal") + Val(built, "mfgTotal")
                    + Val(built, "sellTotal") + Val(built, "adminTotal");
        var soldY = YtdVal(built, "cogsTotal") + YtdVal(built, "empTotal") + YtdVal(built, "mfgTotal")
                    + YtdVal(built, "sellTotal") + YtdVal(built, "adminTotal");
        T("sold", "Total Cost of goods sold", soldM, soldY);

        var ebitdaM = turnM - soldM;
        var ebitdaY = turnY - soldY;
        built.Add(Make("ebitda", "EBITDA", "result", ebitdaM, ebitdaY));

        L("dep", "Depreciation");
        L("bank", "Bank Charges & Commission");
        L("intO", "Interest Expense - Other");
        L("intTl", "Interest on Term Loan");
        L("intWc", "Interest on Working Capital & Term Loan");

        var pbtM = ebitdaM - Val(built, "dep") - Val(built, "bank") - Val(built, "intO")
                   - Val(built, "intTl") - Val(built, "intWc");
        var pbtY = ebitdaY - YtdVal(built, "dep") - YtdVal(built, "bank") - YtdVal(built, "intO")
                   - YtdVal(built, "intTl") - YtdVal(built, "intWc");
        built.Add(Make("pbt", "PBT", "result", pbtM, pbtY));
        built.Add(Make("cashPbt", "Cash PBT", "result", pbtM + Val(built, "dep"), pbtY + YtdVal(built, "dep")));

        ApplyPct(built, turnM, turnY);
        return built;
    }

    private static PnlStatementRow Make(string id, string label, string kind, double monthRupees, double ytdRupees) =>
        new()
        {
            Id = id,
            Label = label,
            Kind = kind,
            MonthLacs = Round2(monthRupees / Lacs),
            YtdLacs = Round2(ytdRupees / Lacs),
        };

    private static double Val(List<PnlStatementRow> rows, string id) =>
        (rows.FirstOrDefault(r => r.Id == id)?.MonthLacs ?? 0) * Lacs;

    private static double YtdVal(List<PnlStatementRow> rows, string id) =>
        (rows.FirstOrDefault(r => r.Id == id)?.YtdLacs ?? 0) * Lacs;

    private static void ApplyPct(List<PnlStatementRow> rows, double turnM, double turnY)
    {
        foreach (var row in rows)
        {
            if (row.MonthLacs == null)
                continue;
            row.PctToSales = turnM == 0 ? 0 : Round4((row.MonthLacs.Value * Lacs) / turnM);
            row.YtdPctToSales = turnY == 0 ? 0 : Round4((row.YtdLacs!.Value * Lacs) / turnY);
        }
    }

    private static string CanonHead(string? raw)
    {
        var s = Regex.Replace((raw ?? "").Trim(), @"\s+", " ");
        if (HeadAliases.TryGetValue(s, out var mapped))
            return mapped;
        var key = s.ToLowerInvariant();
        if (HeadAliases.TryGetValue(key, out mapped))
            return mapped;
        return s;
    }

    public static DateTime ParseMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
            throw new ArgumentException("Month is required (yyyy-MM).");
        if (DateTime.TryParseExact(month.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
            return d;
        throw new ArgumentException("Month must be yyyy-MM.");
    }

    private static bool IsAll(string company) =>
        company.Equals("All Companies", StringComparison.OrdinalIgnoreCase)
        || company == "*"
        || company.Equals("all", StringComparison.OrdinalIgnoreCase);

    private static bool IsAggregate(string company) =>
        IsAll(company) || company.StartsWith("G-", StringComparison.OrdinalIgnoreCase);

    private static double Round2(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    private static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private sealed class BookRow
    {
        public string Category { get; set; } = "";
        public string Head { get; set; } = "";
        public string LedgerName { get; set; } = "";
        public double Amount { get; set; }
    }

    private sealed class InventoryBits
    {
        public double Materials { get; set; }
        public double Traded { get; set; }
        public double InventoryChange { get; set; }
        public double Stores { get; set; }
    }
}
