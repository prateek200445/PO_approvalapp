using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Lender document pack for one legal company (all FactoryInfo units of a GroupName):
/// top 10 customers / suppliers excl. group companies, monthly net sales / purchase splits,
/// party-wise debtors ageing and the monthly stock statement. Amounts in INR lakh.
/// </summary>
public sealed class BankDocumentPackService
{
    private const double Lakh = 100_000d;
    private const int QueryTimeoutSeconds = 300;
    private static readonly CultureInfo In = CultureInfo.InvariantCulture;

    public static readonly IReadOnlyList<BankPackCompany> Companies =
    [
        new("HPBL", "HCP Plastene Bulkpack Ltd", "HCP Plastene Bulkpack Ltd"),
        new("KPW", "K.P. WOVEN PRIVATE LIMITED", "K.P. Woven Pvt Ltd"),
        new("PIL", "Plastene India Limited", "Plastene India Limited"),
    ];

    private static readonly string[] StockCategories = ["RawMaterial", "Packing", "WIP", "FinishedGoods", "Traded", "Stores"];
    private static readonly Dictionary<string, string> StockCategoryLabels = new()
    {
        ["RawMaterial"] = "Raw material",
        ["Packing"] = "Packing material",
        ["WIP"] = "Work in progress",
        ["FinishedGoods"] = "Finished goods",
        ["Traded"] = "Stock in trade",
        ["Stores"] = "Stores & spares",
    };

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public BankDocumentPackService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public static BankPackCompany ResolveCompany(string code) =>
        Companies.FirstOrDefault(c => c.Code.Equals((code ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Choose HPBL, KPW or PIL.");

    public static BankPackOptions DefaultOptions(DateTime? today = null)
    {
        var t = (today ?? DateTime.Today).Date;
        var fyStart = new DateTime(t.Month >= 4 ? t.Year : t.Year - 1, 4, 1);
        var lastMonthEnd = new DateTime(t.Year, t.Month, 1).AddDays(-1);
        return new BankPackOptions
        {
            Today = t,
            CurrentFyStart = fyStart,
            AgeingDates = [fyStart.AddDays(-1), lastMonthEnd],
            StockFrom = fyStart.AddMonths(-1),
            StockTo = new DateTime(t.Year, t.Month, 1).AddMonths(-2),
        };
    }

    public async Task<BankPackDto> BuildAsync(string code, BankPackOptions options, bool refresh = false)
    {
        var company = ResolveCompany(code);
        var key = $"bank-pack-v1:{company.Code}:{options.Today:yyyyMMdd}:{string.Join(",", options.AgeingDates.Select(d => d.ToString("yyyyMMdd")))}:{options.StockFrom:yyyyMM}:{options.StockTo:yyyyMM}";
        if (!refresh && _cache.TryGetValue(key, out BankPackDto? hit) && hit != null)
            return hit;

        var prevFyStart = options.CurrentFyStart.AddYears(-1);
        var windowTo = options.Today.AddDays(1);

        var salesTask = LoadSalesAsync(company.GroupName, prevFyStart, windowTo);
        var purchaseTask = LoadPurchasesAsync(company.GroupName, prevFyStart, windowTo);
        var ageingTasks = options.AgeingDates.Select(d => LoadAgeingAsync(company.GroupName, d)).ToList();
        var stockTask = LoadStockAsync(company.GroupName, options.StockFrom, options.StockTo);
        await Task.WhenAll(salesTask, purchaseTask, stockTask, Task.WhenAll(ageingTasks));

        var sales = await salesTask;
        var purchases = await purchaseTask;
        var fys = new[]
        {
            (Label: FyLabel(prevFyStart), From: prevFyStart, To: options.CurrentFyStart.AddDays(-1)),
            (Label: FyLabel(options.CurrentFyStart) + " (till date)", From: options.CurrentFyStart, To: options.Today),
        };

        var dto = new BankPackDto
        {
            Code = company.Code,
            CompanyName = company.DisplayName,
            GeneratedAt = DateTime.Now,
            Today = options.Today.ToString("yyyy-MM-dd"),
            CustomerRankings = fys.Select(fy => Rank(sales, fy.Label, fy.From, fy.To, l => l.IsForeign)).ToList(),
            SupplierRankings = fys.Select(fy => Rank(purchases, fy.Label, fy.From, fy.To, l => l.IsImport)).ToList(),
            MonthlySales = Monthly(sales, options.CurrentFyStart, options.Today, l => l.IsForeign),
            MonthlyPurchases = Monthly(purchases, options.CurrentFyStart, options.Today, l => l.IsImport),
            Debtors = ageingTasks.Select(t => t.Result).ToList(),
            Stock = await stockTask,
        };
        _cache.Set(key, dto, TimeSpan.FromHours(2));
        return dto;
    }

    private static string FyLabel(DateTime fyStart) => $"FY {fyStart:yy}-{fyStart.AddYears(1):yy}";

    private static bool IsGroupParty(string party, string? isInterCompany, int isFactory)
    {
        if (party.Contains("fibco", StringComparison.OrdinalIgnoreCase))
            return false;
        var flag = (isInterCompany ?? "").Trim().ToLowerInvariant();
        return isFactory == 1 || flag is "yes" or "y" or "1" or "true";
    }

    private static bool IsForeignCountry(string? country)
    {
        var c = (country ?? "").Trim().ToLowerInvariant();
        return c.Length > 0 && c is not ("in" or "ind" or "bharat") && !c.Contains("india");
    }

    private async Task<List<PartyMonth>> LoadSalesAsync(string group, DateTime from, DateTime toExclusive)
    {
        const string sql = """
            WITH units AS (
                SELECT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK) WHERE LTRIM(RTRIM(GroupName)) = @Group
            ),
            lines AS (
                SELECT v.InvDate AS D, LTRIM(RTRIM(v.BuyerName)) AS Party, CAST(ISNULL(v.AccessableValue, 0) AS float) AS Amt
                FROM dbo.SalesVoucher v WITH (NOLOCK)
                WHERE v.InvDate >= @From AND v.InvDate < @ToExcl
                  AND LTRIM(RTRIM(v.CompanyName)) IN (SELECT Name FROM units)
                  AND ISNULL(v.BuyerName, '') <> ''
                  AND LOWER(ISNULL(v.InvType, '')) NOT LIKE '%inter%unit%'
                  AND LOWER(ISNULL(v.InvType, '')) NOT LIKE '%branch transfer%'
                  AND LOWER(ISNULL(v.InvType, '')) NOT LIKE '%job%'
                  AND LOWER(ISNULL(v.InvType, '')) NOT LIKE '%rent%'
                  AND LOWER(ISNULL(v.salesLedger, '')) NOT LIKE '%branch transfer%'
                  AND LOWER(ISNULL(v.salesLedger, '')) NOT LIKE '%rent%'
                UNION ALL
                SELECT c.CreditNoteDate, LTRIM(RTRIM(c.PartyName)), -CAST(ISNULL(c.basic, 0) AS float)
                FROM dbo.CreditNote c WITH (NOLOCK)
                WHERE c.CreditNoteDate >= @From AND c.CreditNoteDate < @ToExcl
                  AND LTRIM(RTRIM(c.CompanyName)) IN (SELECT Name FROM units)
                  AND ISNULL(c.PartyName, '') <> ''
            )
            SELECT l.Party, YEAR(l.D) AS Y, MONTH(l.D) AS M, SUM(l.Amt) AS Amount,
                   MAX(cm.Country) AS Country, MAX(cm.IsInterCompany) AS IsInterCompany,
                   MAX(CASE WHEN fi.Name IS NULL THEN 0 ELSE 1 END) AS IsFactory,
                   CAST(0 AS bit) AS IsImport
            FROM lines l
            LEFT JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK) ON LTRIM(RTRIM(cm.LedgerName)) = l.Party
            LEFT JOIN (SELECT DISTINCT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK)) fi ON fi.Name = l.Party
            GROUP BY l.Party, YEAR(l.D), MONTH(l.D)
            """;
        return await QueryPartyMonthsAsync(sql, group, from, toExclusive);
    }

    private async Task<List<PartyMonth>> LoadPurchasesAsync(string group, DateTime from, DateTime toExclusive)
    {
        const string sql = """
            WITH units AS (
                SELECT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK) WHERE LTRIM(RTRIM(GroupName)) = @Group
            ),
            lines AS (
                SELECT v.SysDate AS D, LTRIM(RTRIM(v.SupplierName)) AS Party, CAST(ISNULL(v.AccessableValue, 0) AS float) AS Amt,
                       CASE WHEN LOWER(ISNULL(v.VoucherType, '')) LIKE 'imp%'
                              OR LOWER(ISNULL(v.VoucherType, '')) LIKE '%import%'
                              OR LTRIM(ISNULL(v.BillofEntryNumber, '')) <> ''
                              OR UPPER(LTRIM(RTRIM(ISNULL(v.currency, '')))) NOT IN ('', 'INR', 'RS', 'RS.', 'RUPEES')
                            THEN 1 ELSE 0 END AS IsImport
                FROM dbo.PurchaseVoucher v WITH (NOLOCK)
                WHERE v.SysDate >= @From AND v.SysDate < @ToExcl
                  AND LTRIM(RTRIM(v.CompanyName)) IN (SELECT Name FROM units)
                  AND ISNULL(v.SupplierName, '') <> ''
                  AND LOWER(ISNULL(v.VoucherType, '')) NOT LIKE '%branch transfer%'
                  AND LOWER(ISNULL(v.VoucherType, '')) NOT LIKE '%inter%unit%'
                  AND LOWER(ISNULL(v.VoucherType, '')) NOT LIKE '%fixed asset%'
                  AND LOWER(ISNULL(v.VoucherType, '')) NOT LIKE '%commision%'
                  AND LOWER(ISNULL(v.VoucherType, '')) NOT LIKE '%commission%'
            )
            SELECT l.Party, YEAR(l.D) AS Y, MONTH(l.D) AS M, SUM(l.Amt) AS Amount,
                   MAX(cm.Country) AS Country, MAX(cm.IsInterCompany) AS IsInterCompany,
                   MAX(CASE WHEN fi.Name IS NULL THEN 0 ELSE 1 END) AS IsFactory,
                   CAST(CASE WHEN MAX(l.IsImport) = 1 OR MAX(CASE WHEN LOWER(ISNULL(cm.Country, '')) LIKE '%india%' OR LTRIM(ISNULL(cm.Country, '')) = '' THEN 0 ELSE 1 END) = 1 THEN 1 ELSE 0 END AS bit) AS IsImport
            FROM lines l
            LEFT JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK) ON LTRIM(RTRIM(cm.LedgerName)) = l.Party
            LEFT JOIN (SELECT DISTINCT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK)) fi ON fi.Name = l.Party
            GROUP BY l.Party, YEAR(l.D), MONTH(l.D), l.IsImport
            """;
        return await QueryPartyMonthsAsync(sql, group, from, toExclusive);
    }

    private async Task<List<PartyMonth>> QueryPartyMonthsAsync(string sql, string group, DateTime from, DateTime toExclusive)
    {
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<PartyMonthRow>(
            sql, new { Group = group, From = from, ToExcl = toExclusive }, commandTimeout: QueryTimeoutSeconds);
        return rows.Select(r => new PartyMonth(
            r.Party ?? "",
            new DateTime(r.Y, r.M, 1),
            r.Amount,
            IsForeignCountry(r.Country),
            r.IsImport,
            IsGroupParty(r.Party ?? "", r.IsInterCompany, r.IsFactory),
            IsForeignCountry(r.Country) ? (r.Country ?? "").Trim() : null)).ToList();
    }

    private static BankPackRanking Rank(
        List<PartyMonth> lines, string label, DateTime from, DateTime to, Func<PartyMonth, bool> isOverseas)
    {
        var parties = lines
            .Where(l => !l.IsGroup && l.Month >= new DateTime(from.Year, from.Month, 1) && l.Month <= to)
            .GroupBy(l => (l.Party, Overseas: isOverseas(l)))
            .Select(g => new BankPackParty
            {
                Name = g.Key.Party,
                Country = g.Select(x => x.Country).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                AmountLakh = Math.Round(g.Sum(x => x.Amount) / Lakh, 2),
                Overseas = g.Key.Overseas,
            })
            .Where(p => p.AmountLakh > 0)
            .ToList();

        List<BankPackParty> Top(bool overseas)
        {
            var pool = parties.Where(p => p.Overseas == overseas).OrderByDescending(p => p.AmountLakh).ToList();
            var total = pool.Sum(p => p.AmountLakh);
            return pool.Take(10).Select((p, i) =>
            {
                p.Rank = i + 1;
                p.SharePct = total > 0 ? Math.Round(p.AmountLakh / total * 100, 2) : 0;
                return p;
            }).ToList();
        }

        return new BankPackRanking
        {
            Label = label,
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            Overseas = Top(true),
            Domestic = Top(false),
            OverseasTotalLakh = Math.Round(parties.Where(p => p.Overseas).Sum(p => p.AmountLakh), 2),
            DomesticTotalLakh = Math.Round(parties.Where(p => !p.Overseas).Sum(p => p.AmountLakh), 2),
        };
    }

    private static List<BankPackMonth> Monthly(List<PartyMonth> lines, DateTime from, DateTime to, Func<PartyMonth, bool> isOverseas)
    {
        var result = new List<BankPackMonth>();
        for (var m = new DateTime(from.Year, from.Month, 1); m <= to; m = m.AddMonths(1))
        {
            var month = lines.Where(l => l.Month == m).ToList();
            double Sum(bool overseas, bool group) =>
                Math.Round(month.Where(l => isOverseas(l) == overseas && l.IsGroup == group).Sum(l => l.Amount) / Lakh, 2);
            result.Add(new BankPackMonth
            {
                Month = m.ToString("yyyy-MM"),
                Label = m.ToString("MMM-yy", In),
                OverseasGroupLakh = Sum(true, true),
                OverseasDirectLakh = Sum(true, false),
                DomesticGroupLakh = Sum(false, true),
                DomesticDirectLakh = Sum(false, false),
                PartialMonth = m.Year == to.Year && m.Month == to.Month && to.Day < DateTime.DaysInMonth(to.Year, to.Month),
            });
        }
        return result;
    }

    private async Task<BankPackAgeing> LoadAgeingAsync(string group, DateTime asOf)
    {
        const string sql = """
            WITH units AS (
                SELECT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK) WHERE LTRIM(RTRIM(GroupName)) = @Group
            ),
            debtors AS (
                SELECT DISTINCT LTRIM(RTRIM(ledgername)) AS ledgername, LTRIM(RTRIM(companyname)) AS companyname
                FROM dbo.vw_ledgergrouping
                WHERE 'Sundry Debtors' IN (expensehead, expensegrouphead, b, c, d, e, f, g)
                  AND LTRIM(RTRIM(companyname)) IN (SELECT Name FROM units)
            ),
            bills AS (
                SELECT companyid, ledgername, billno, CompanyName, LedgerId, MAX(billdate) AS billdate
                FROM dbo.accountbills
                WHERE LTRIM(RTRIM(CompanyName)) IN (SELECT Name FROM units)
                GROUP BY companyid, ledgername, billno, CompanyName, LedgerId
            ),
            billwise AS (
                SELECT LTRIM(RTRIM(v1.ledgername)) AS Party,
                       LTRIM(RTRIM(v1.CompanyName)) AS Unit,
                       CASE WHEN ISNULL(v1.billno, '') = '' THEN 'On Account' ELSE v1.billno END AS BillNo,
                       MAX(CASE WHEN ISNULL(v1.billno, '') = '' THEN NULL ELSE COALESCE(b.billdate, v1.voucherdate) END) AS BillDate,
                       SUM(ISNULL(v1.amount, 0)) AS Due
                FROM dbo.vw_billwisetransactionwithonaccount v1
                INNER JOIN debtors d ON d.ledgername = LTRIM(RTRIM(v1.ledgername)) AND d.companyname = LTRIM(RTRIM(v1.CompanyName))
                LEFT JOIN bills b ON v1.companyid = b.companyid AND v1.ledgername = b.ledgername AND v1.billno = b.billno
                                 AND v1.CompanyName = b.CompanyName AND v1.ledgerid = b.LedgerId
                WHERE v1.isbillwise = 'yes' AND v1.voucherdate <= @AsOf
                GROUP BY LTRIM(RTRIM(v1.ledgername)), LTRIM(RTRIM(v1.CompanyName)),
                         CASE WHEN ISNULL(v1.billno, '') = '' THEN 'On Account' ELSE v1.billno END
                HAVING ROUND(ABS(SUM(ISNULL(v1.amount, 0))), 0) <> 0
            )
            SELECT w.Party,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) <= 30 THEN w.Due ELSE 0 END) AS D0_30,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) BETWEEN 31 AND 60 THEN w.Due ELSE 0 END) AS D31_60,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) BETWEEN 61 AND 90 THEN w.Due ELSE 0 END) AS D61_90,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) BETWEEN 91 AND 180 THEN w.Due ELSE 0 END) AS D91_180,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) BETWEEN 181 AND 365 THEN w.Due ELSE 0 END) AS D181_365,
                   SUM(CASE WHEN w.BillNo <> 'On Account' AND DATEDIFF(DAY, w.BillDate, @AsOf) > 365 THEN w.Due ELSE 0 END) AS D365Plus,
                   SUM(CASE WHEN w.BillNo = 'On Account' OR w.BillDate IS NULL THEN w.Due ELSE 0 END) AS OnAccount,
                   SUM(w.Due) AS Total,
                   MAX(cm.Country) AS Country, MAX(cm.IsInterCompany) AS IsInterCompany,
                   MAX(CASE WHEN fi.Name IS NULL THEN 0 ELSE 1 END) AS IsFactory
            FROM billwise w
            LEFT JOIN dbo.CommonLedgerMaster cm WITH (NOLOCK) ON LTRIM(RTRIM(cm.LedgerName)) = w.Party
            LEFT JOIN (SELECT DISTINCT LTRIM(RTRIM(Name)) AS Name FROM dbo.FactoryInfo WITH (NOLOCK)) fi ON fi.Name = w.Party
            GROUP BY w.Party
            HAVING ROUND(ABS(SUM(w.Due)), 0) <> 0
            """;
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<AgeingRow>(sql, new { Group = group, AsOf = asOf }, commandTimeout: QueryTimeoutSeconds);
        var parties = rows.Select(r => new BankPackAgeingParty
        {
            Party = r.Party ?? "",
            IsGroup = IsGroupParty(r.Party ?? "", r.IsInterCompany, r.IsFactory),
            Overseas = IsForeignCountry(r.Country),
            BucketsLakh =
            [
                L(r.D0_30), L(r.D31_60), L(r.D61_90), L(r.D91_180), L(r.D181_365), L(r.D365Plus), L(r.OnAccount),
            ],
            TotalLakh = L(r.Total),
        }).OrderByDescending(p => p.TotalLakh).ToList();
        return new BankPackAgeing { AsOf = asOf.ToString("yyyy-MM-dd"), Parties = parties };

        static double L(double v) => Math.Round(v / Lakh, 2);
    }

    public static readonly string[] AgeingBuckets = ["0-30 days", "31-60 days", "61-90 days", "91-180 days", "181-365 days", "Over 365 days", "On account / unadjusted"];

    private async Task<BankPackStock> LoadStockAsync(string group, DateTime from, DateTime to)
    {
        using var connection = _database.CreateConnection();
        var units = (await connection.QueryAsync<string>(
            "SELECT LTRIM(RTRIM(Name)) FROM dbo.FactoryInfo WITH (NOLOCK) WHERE LTRIM(RTRIM(GroupName)) = @Group ORDER BY LTRIM(RTRIM(Name))",
            new { Group = group })).ToList();
        var months = new List<DateTime>();
        for (var m = new DateTime(from.Year, from.Month, 1); m <= to; m = m.AddMonths(1)) months.Add(m);

        var rows = (await connection.QueryAsync<StockRow>(
            """
            SELECT LTRIM(RTRIM(CompanyName)) AS Unit, StockMonth, Category, SUM(AmountLacs) AS AmountLacs
            FROM dbo.PnlStockValue WITH (NOLOCK)
            WHERE LTRIM(RTRIM(CompanyName)) IN @Units AND StockMonth >= @From AND StockMonth <= @To
            GROUP BY LTRIM(RTRIM(CompanyName)), StockMonth, Category
            """,
            new { Units = units.DefaultIfEmpty("__none__"), From = months.FirstOrDefault(), To = months.LastOrDefault() },
            commandTimeout: 60)).ToList();

        var stock = new BankPackStock
        {
            Months = months.Select(m => m.ToString("yyyy-MM")).ToList(),
            MonthLabels = months.Select(m => m.ToString("MMM-yy", In)).ToList(),
            Categories = StockCategories.Select(c => StockCategoryLabels[c]).ToList(),
        };
        foreach (var unit in units)
        {
            var unitRows = rows.Where(r => string.Equals(r.Unit, unit, StringComparison.OrdinalIgnoreCase)).ToList();
            stock.Units.Add(new BankPackStockUnit
            {
                Unit = unit,
                ValuesLakh = StockCategories.Select(cat => months.Select(m =>
                {
                    var hit = unitRows.Where(r => r.Category == cat && r.StockMonth.Year == m.Year && r.StockMonth.Month == m.Month).ToList();
                    return hit.Count == 0 ? (double?)null : Math.Round(hit.Sum(h => h.AmountLacs), 2);
                }).ToList()).ToList(),
            });
        }
        stock.Units.RemoveAll(u => u.ValuesLakh.All(c => c.All(v => v is null)));
        return stock;
    }

    public async Task<byte[]> BuildExcelAsync(string code, BankPackOptions options, bool refresh = false)
    {
        var pack = await BuildAsync(code, options, refresh);
        using var wb = new XLWorkbook();

        WriteRankings(wb.Worksheets.Add("Top 10 Customers"), pack, pack.CustomerRankings,
            "Top 10 Customers (excluding group companies)", "Export customers", "Domestic customers", "Customer");
        WriteRankings(wb.Worksheets.Add("Top 10 Suppliers"), pack, pack.SupplierRankings,
            "Top 10 Suppliers (excluding group companies)", "Import suppliers", "Domestic suppliers", "Supplier");
        WriteMonthly(wb.Worksheets.Add("Monthly Net Sales"), pack, pack.MonthlySales,
            "Monthly Net Sales", "Export", "Domestic");
        WriteMonthly(wb.Worksheets.Add("Monthly Net Purchase"), pack, pack.MonthlyPurchases,
            "Monthly Net Purchase", "Import", "Domestic");
        foreach (var ageing in pack.Debtors)
        {
            var d = DateTime.Parse(ageing.AsOf, CultureInfo.InvariantCulture);
            WriteAgeing(wb.Worksheets.Add($"Debtors Ageing {d.ToString("MMM-yy", In)}"), pack, ageing);
        }
        WriteStock(wb.Worksheets.Add("Stock Statement"), pack);
        WriteNotes(wb.Worksheets.Add("Notes"), pack);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor Blue = XLColor.FromHtml("#1565A8");
    private static readonly XLColor Soft = XLColor.FromHtml("#EAF2FB");
    private const string AmountFormat = "#,##,##0.00";

    private static int Title(IXLWorksheet ws, BankPackDto pack, string title, int width)
    {
        ws.Cell(1, 1).Value = $"{pack.CompanyName} — {title}";
        ws.Range(1, 1, 1, width).Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(Navy);
        ws.Cell(2, 1).Value = $"Amounts in INR lakh · Source: ERP · Generated {pack.GeneratedAt.ToString("dd-MMM-yyyy HH:mm", In)}";
        ws.Range(2, 1, 2, width).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#64748B"));
        return 4;
    }

    private static void Header(IXLWorksheet ws, int row, int col, params string[] labels)
    {
        for (var i = 0; i < labels.Length; i++) ws.Cell(row, col + i).Value = labels[i];
        var r = ws.Range(row, col, row, col + labels.Length - 1);
        r.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Navy);
        r.Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }

    private static void Box(IXLRange range)
    {
        range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        range.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#CBD5E1")).Border.SetInsideBorderColor(XLColor.FromHtml("#CBD5E1"));
    }

    private static void WriteRankings(IXLWorksheet ws, BankPackDto pack, List<BankPackRanking> rankings,
        string title, string overseasLabel, string domesticLabel, string nameHeader)
    {
        var row = Title(ws, pack, title, 11);
        foreach (var fy in rankings)
        {
            ws.Cell(row, 1).Value = $"{fy.Label}  ({D(fy.From)} to {D(fy.To)})";
            ws.Range(row, 1, row, 11).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(Blue).Fill.SetBackgroundColor(Soft);
            row += 1;
            var end = Math.Max(
                Block(ws, row, 1, overseasLabel, nameHeader, fy.Overseas, fy.OverseasTotalLakh, showCountry: true),
                Block(ws, row, 7, domesticLabel, nameHeader, fy.Domestic, fy.DomesticTotalLakh, showCountry: false));
            row = end + 2;
        }
        ws.Column(1).Width = 6; ws.Column(2).Width = 42; ws.Column(3).Width = 16; ws.Column(4).Width = 15; ws.Column(5).Width = 11;
        ws.Column(6).Width = 3; ws.Column(7).Width = 6; ws.Column(8).Width = 42; ws.Column(9).Width = 15; ws.Column(10).Width = 11;
    }

    private static int Block(IXLWorksheet ws, int row, int col, string label, string nameHeader,
        List<BankPackParty> parties, double totalLakh, bool showCountry)
    {
        var width = showCountry ? 5 : 4;
        ws.Cell(row, col).Value = label;
        ws.Range(row, col, row, col + width - 1).Merge().Style.Font.SetBold().Font.SetFontColor(Navy);
        row++;
        var headers = showCountry
            ? new[] { "#", nameHeader, "Country", "Amount", "% share" }
            : new[] { "#", nameHeader, "Amount", "% share" };
        Header(ws, row, col, headers);
        var first = row;
        foreach (var p in parties)
        {
            row++;
            var c = col;
            ws.Cell(row, c++).Value = p.Rank;
            ws.Cell(row, c++).Value = p.Name;
            if (showCountry) ws.Cell(row, c++).Value = p.Country ?? "";
            ws.Cell(row, c).Value = p.AmountLakh; ws.Cell(row, c++).Style.NumberFormat.Format = AmountFormat;
            ws.Cell(row, c).Value = p.SharePct / 100; ws.Cell(row, c).Style.NumberFormat.Format = "0.00%";
        }
        if (parties.Count == 0)
        {
            row++;
            ws.Cell(row, col + 1).Value = "No transactions in this period";
            ws.Cell(row, col + 1).Style.Font.SetItalic();
        }
        row++;
        var top = parties.Sum(p => p.AmountLakh);
        ws.Cell(row, col + 1).Value = $"Top {parties.Count} total";
        var amtCol = col + (showCountry ? 3 : 2);
        ws.Cell(row, amtCol).Value = Math.Round(top, 2); ws.Cell(row, amtCol).Style.NumberFormat.Format = AmountFormat;
        ws.Cell(row, amtCol + 1).Value = totalLakh > 0 ? top / totalLakh : 0; ws.Cell(row, amtCol + 1).Style.NumberFormat.Format = "0.00%";
        ws.Range(row, col, row, col + width - 1).Style.Font.SetBold().Fill.SetBackgroundColor(Soft);
        row++;
        ws.Cell(row, col + 1).Value = "Total (all non-group parties)";
        ws.Cell(row, amtCol).Value = totalLakh; ws.Cell(row, amtCol).Style.NumberFormat.Format = AmountFormat;
        ws.Range(row, col, row, col + width - 1).Style.Font.SetItalic();
        Box(ws.Range(first, col, row, col + width - 1));
        return row;
    }

    private static void WriteMonthly(IXLWorksheet ws, BankPackDto pack, List<BankPackMonth> months,
        string title, string overseasLabel, string domesticLabel)
    {
        var row = Title(ws, pack, title, 10);
        Header(ws, row, 1, "Month",
            $"{overseasLabel} — Group companies", $"{overseasLabel} — Direct", $"Total {overseasLabel.ToLowerInvariant()}",
            $"{domesticLabel} — Group companies", $"{domesticLabel} — Direct", $"Total {domesticLabel.ToLowerInvariant()}",
            "Total");
        ws.Row(row).Height = 32;
        var first = row;
        foreach (var m in months)
        {
            row++;
            ws.Cell(row, 1).Value = m.PartialMonth ? $"{m.Label} (till date)" : m.Label;
            Values(row, m.OverseasGroupLakh, m.OverseasDirectLakh, m.OverseasGroupLakh + m.OverseasDirectLakh,
                m.DomesticGroupLakh, m.DomesticDirectLakh, m.DomesticGroupLakh + m.DomesticDirectLakh,
                m.OverseasGroupLakh + m.OverseasDirectLakh + m.DomesticGroupLakh + m.DomesticDirectLakh);
        }
        row++;
        ws.Cell(row, 1).Value = "Total";
        Values(row,
            months.Sum(m => m.OverseasGroupLakh), months.Sum(m => m.OverseasDirectLakh), months.Sum(m => m.OverseasGroupLakh + m.OverseasDirectLakh),
            months.Sum(m => m.DomesticGroupLakh), months.Sum(m => m.DomesticDirectLakh), months.Sum(m => m.DomesticGroupLakh + m.DomesticDirectLakh),
            months.Sum(m => m.OverseasGroupLakh + m.OverseasDirectLakh + m.DomesticGroupLakh + m.DomesticDirectLakh));
        ws.Range(row, 1, row, 8).Style.Font.SetBold().Fill.SetBackgroundColor(Soft);
        foreach (var c in new[] { 4, 7, 8 })
            ws.Range(first + 1, c, row, c).Style.Font.SetBold();
        Box(ws.Range(first, 1, row, 8));
        ws.Column(1).Width = 18;
        for (var c = 2; c <= 8; c++) ws.Column(c).Width = 17;

        void Values(int r, params double[] v)
        {
            for (var i = 0; i < v.Length; i++)
            {
                ws.Cell(r, 2 + i).Value = Math.Round(v[i], 2);
                ws.Cell(r, 2 + i).Style.NumberFormat.Format = AmountFormat;
            }
        }
    }

    private static void WriteAgeing(IXLWorksheet ws, BankPackDto pack, BankPackAgeing ageing)
    {
        var asOf = DateTime.Parse(ageing.AsOf, CultureInfo.InvariantCulture);
        var width = 4 + AgeingBuckets.Length;
        var row = Title(ws, pack, $"Debtors Ageing (party-wise) as on {asOf.ToString("dd-MMM-yyyy", In)}", width);
        Header(ws, row, 1, ["#", "Party", "Type", .. AgeingBuckets, "Total outstanding"]);
        ws.Row(row).Height = 32;
        var first = row;
        var n = 0;
        foreach (var p in ageing.Parties)
        {
            row++;
            ws.Cell(row, 1).Value = ++n;
            ws.Cell(row, 2).Value = p.Party;
            ws.Cell(row, 3).Value = (p.IsGroup ? "Group company" : "Third party") + (p.Overseas ? " · Export" : "");
            for (var i = 0; i < p.BucketsLakh.Length; i++)
            {
                ws.Cell(row, 4 + i).Value = p.BucketsLakh[i];
                ws.Cell(row, 4 + i).Style.NumberFormat.Format = AmountFormat;
            }
            ws.Cell(row, width).Value = p.TotalLakh;
            ws.Cell(row, width).Style.NumberFormat.Format = AmountFormat;
            ws.Cell(row, width).Style.Font.SetBold();
        }

        void TotalRow(string label, Func<BankPackAgeingParty, bool> filter, bool strong)
        {
            row++;
            ws.Cell(row, 2).Value = label;
            var set = ageing.Parties.Where(filter).ToList();
            for (var i = 0; i < AgeingBuckets.Length; i++)
            {
                ws.Cell(row, 4 + i).Value = Math.Round(set.Sum(p => p.BucketsLakh[i]), 2);
                ws.Cell(row, 4 + i).Style.NumberFormat.Format = AmountFormat;
            }
            ws.Cell(row, width).Value = Math.Round(set.Sum(p => p.TotalLakh), 2);
            ws.Cell(row, width).Style.NumberFormat.Format = AmountFormat;
            var r = ws.Range(row, 1, row, width);
            if (strong) r.Style.Font.SetBold().Fill.SetBackgroundColor(Soft);
            else r.Style.Font.SetItalic();
        }

        TotalRow("Total — third parties", p => !p.IsGroup, false);
        TotalRow("Total — group companies", p => p.IsGroup, false);
        TotalRow("Grand total", _ => true, true);
        Box(ws.Range(first, 1, row, width));
        ws.Column(1).Width = 6; ws.Column(2).Width = 46; ws.Column(3).Width = 22;
        for (var c = 4; c <= width; c++) ws.Column(c).Width = 14;
        ws.SheetView.FreezeRows(first);
    }

    private static void WriteStock(IXLWorksheet ws, BankPackDto pack)
    {
        var s = pack.Stock;
        var width = 2 + s.Months.Count;
        var row = Title(ws, pack, "Stock Statement (month-end values)", width);
        if (s.Units.Count == 0)
        {
            ws.Cell(row, 1).Value = "No month-end stock values have been entered for this company in the portal (P&L → Stock).";
            return;
        }
        Header(ws, row, 1, ["Unit", "Category", .. s.MonthLabels]);
        var first = row;
        var totals = new double?[s.Categories.Count, s.Months.Count];
        foreach (var unit in s.Units)
        {
            for (var c = 0; c < s.Categories.Count; c++)
            {
                row++;
                ws.Cell(row, 1).Value = c == 0 ? unit.Unit : "";
                ws.Cell(row, 2).Value = s.Categories[c];
                for (var m = 0; m < s.Months.Count; m++)
                {
                    var v = unit.ValuesLakh[c][m];
                    var cell = ws.Cell(row, 3 + m);
                    if (v is null)
                    {
                        cell.Value = "not entered";
                        cell.Style.Font.SetFontColor(XLColor.FromHtml("#B45309")).Font.SetItalic();
                    }
                    else
                    {
                        cell.Value = v.Value;
                        cell.Style.NumberFormat.Format = AmountFormat;
                        totals[c, m] = (totals[c, m] ?? 0) + v.Value;
                    }
                }
            }
            row++;
            ws.Cell(row, 2).Value = "Unit total";
            for (var m = 0; m < s.Months.Count; m++)
            {
                var vals = unit.ValuesLakh.Select(cat => cat[m]).ToList();
                var cell = ws.Cell(row, 3 + m);
                if (vals.All(v => v is null)) cell.Value = "not entered";
                else { cell.Value = Math.Round(vals.Sum(v => v ?? 0), 2); cell.Style.NumberFormat.Format = AmountFormat; }
            }
            ws.Range(row, 1, row, width).Style.Font.SetBold();
        }
        row += 2;
        ws.Cell(row, 1).Value = $"{pack.CompanyName} — all units";
        ws.Range(row, 1, row, width).Style.Font.SetBold().Font.SetFontColor(Navy);
        for (var c = 0; c < s.Categories.Count; c++)
        {
            row++;
            ws.Cell(row, 2).Value = s.Categories[c];
            for (var m = 0; m < s.Months.Count; m++)
            {
                ws.Cell(row, 3 + m).Value = Math.Round(totals[c, m] ?? 0, 2);
                ws.Cell(row, 3 + m).Style.NumberFormat.Format = AmountFormat;
            }
        }
        row++;
        ws.Cell(row, 2).Value = "Total stock";
        for (var m = 0; m < s.Months.Count; m++)
        {
            var sum = 0d;
            for (var c = 0; c < s.Categories.Count; c++) sum += totals[c, m] ?? 0;
            ws.Cell(row, 3 + m).Value = Math.Round(sum, 2);
            ws.Cell(row, 3 + m).Style.NumberFormat.Format = AmountFormat;
        }
        ws.Range(row, 1, row, width).Style.Font.SetBold().Fill.SetBackgroundColor(Soft);
        Box(ws.Range(first, 1, row, width));
        row += 2;
        ws.Cell(row, 1).Value = "Values are the month-end stock figures entered by Accounts in the portal (P&L → Stock). \"not entered\" means the unit has not uploaded that month yet; all-unit totals treat it as zero.";
        ws.Range(row, 1, row, width).Merge().Style.Font.SetItalic().Alignment.SetWrapText(true);
        ws.Row(row).Height = 30;
        ws.Column(1).Width = 40; ws.Column(2).Width = 20;
        for (var c = 3; c <= width; c++) ws.Column(c).Width = 13;
    }

    private static void WriteNotes(IXLWorksheet ws, BankPackDto pack)
    {
        var row = Title(ws, pack, "Basis of preparation", 2);
        string[] notes =
        [
            "Company scope: all units of the legal entity as set up in the ERP (FactoryInfo group).",
            "Group companies: parties flagged as inter-company in the ledger master, or that are themselves group units. Fibco Plastic Industry LLC is treated as a third-party customer, consistent with the portal's sales dashboard.",
            "Net sales: taxable value of sales invoices less credit notes. Inter-unit transfers, branch transfers, job-work and rent invoices are excluded. Export = customer located outside India.",
            "Net purchase: taxable value of purchase vouchers. Branch transfers, inter-unit, fixed assets and commission vouchers are excluded. Import = booked by Accounts as an import (IMP voucher type / bill of entry), foreign currency or foreign supplier. This includes SEZ and high-seas purchases from Indian suppliers where the bill of entry is filed by the company.",
            "Top 10 lists exclude group companies; % share is of the total non-group export / domestic (or import / domestic) value for the period.",
            "Debtors ageing: bill-wise outstanding of Sundry Debtors as on the date, aged from the invoice date. Unadjusted receipts / advances are shown as 'On account'. Negative figures are credit balances.",
            "Stock statement: month-end values entered by Accounts in the portal (P&L → Stock).",
            "Not available from the ERP: bank Assessment Notes (issued by the bank) and the Debt Profile (lender-wise sanctioned limits and outstanding, incl. ECLGS 5.0) — to be provided by Finance.",
        ];
        foreach (var note in notes)
        {
            ws.Cell(row, 1).Value = "•";
            ws.Cell(row, 2).Value = note;
            ws.Cell(row, 2).Style.Alignment.SetWrapText(true);
            row++;
        }
        ws.Column(1).Width = 3;
        ws.Column(2).Width = 120;
    }

    private static string D(string iso) =>
        DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd-MMM-yyyy", In) : iso;

    private sealed record PartyMonth(string Party, DateTime Month, double Amount, bool IsForeign, bool IsImport, bool IsGroup, string? Country);

    private sealed class PartyMonthRow
    {
        public string? Party { get; set; }
        public int Y { get; set; }
        public int M { get; set; }
        public double Amount { get; set; }
        public string? Country { get; set; }
        public string? IsInterCompany { get; set; }
        public int IsFactory { get; set; }
        public bool IsImport { get; set; }
    }

    private sealed class AgeingRow
    {
        public string? Party { get; set; }
        public double D0_30 { get; set; }
        public double D31_60 { get; set; }
        public double D61_90 { get; set; }
        public double D91_180 { get; set; }
        public double D181_365 { get; set; }
        public double D365Plus { get; set; }
        public double OnAccount { get; set; }
        public double Total { get; set; }
        public string? Country { get; set; }
        public string? IsInterCompany { get; set; }
        public int IsFactory { get; set; }
    }

    private sealed class StockRow
    {
        public string Unit { get; set; } = "";
        public DateTime StockMonth { get; set; }
        public string Category { get; set; } = "";
        public double AmountLacs { get; set; }
    }
}

public sealed record BankPackCompany(string Code, string GroupName, string DisplayName);

public sealed class BankPackOptions
{
    public DateTime Today { get; set; }
    public DateTime CurrentFyStart { get; set; }
    public List<DateTime> AgeingDates { get; set; } = [];
    public DateTime StockFrom { get; set; }
    public DateTime StockTo { get; set; }
}

public sealed class BankPackDto
{
    public string Code { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public string Today { get; set; } = "";
    public List<BankPackRanking> CustomerRankings { get; set; } = [];
    public List<BankPackRanking> SupplierRankings { get; set; } = [];
    public List<BankPackMonth> MonthlySales { get; set; } = [];
    public List<BankPackMonth> MonthlyPurchases { get; set; } = [];
    public List<BankPackAgeing> Debtors { get; set; } = [];
    public BankPackStock Stock { get; set; } = new();
}

public sealed class BankPackRanking
{
    public string Label { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<BankPackParty> Overseas { get; set; } = [];
    public List<BankPackParty> Domestic { get; set; } = [];
    public double OverseasTotalLakh { get; set; }
    public double DomesticTotalLakh { get; set; }
}

public sealed class BankPackParty
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public double AmountLakh { get; set; }
    public double SharePct { get; set; }
    public bool Overseas { get; set; }
}

public sealed class BankPackMonth
{
    public string Month { get; set; } = "";
    public string Label { get; set; } = "";
    public double OverseasGroupLakh { get; set; }
    public double OverseasDirectLakh { get; set; }
    public double DomesticGroupLakh { get; set; }
    public double DomesticDirectLakh { get; set; }
    public bool PartialMonth { get; set; }
}

public sealed class BankPackAgeing
{
    public string AsOf { get; set; } = "";
    public List<BankPackAgeingParty> Parties { get; set; } = [];
}

public sealed class BankPackAgeingParty
{
    public string Party { get; set; } = "";
    public bool IsGroup { get; set; }
    public bool Overseas { get; set; }
    public double[] BucketsLakh { get; set; } = [];
    public double TotalLakh { get; set; }
}

public sealed class BankPackStock
{
    public List<string> Months { get; set; } = [];
    public List<string> MonthLabels { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public List<BankPackStockUnit> Units { get; set; } = [];
}

public sealed class BankPackStockUnit
{
    public string Unit { get; set; } = "";
    public List<List<double?>> ValuesLakh { get; set; } = [];
}
