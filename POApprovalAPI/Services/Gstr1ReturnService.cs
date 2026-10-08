using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// GSTR-1 return computed directly from the ERP registers (SalesVoucher / SalesVoucherItem / SalesVoucherTax,
/// CreditNote / CreditNoteItem, sales-side DebitNote + PurchaseVoucherTax). The ERP procs are not used:
/// SP_GSTRegister_B2B clears the SQL cache and writes to a shared table.
/// Taxable value and tax come from invoice lines × exchange rate; GST Output ledger rows are only a cross-check,
/// except that freight / packing charges taxed in the ledger are added to the invoice's highest-rate line.
/// </summary>
public class Gstr1ReturnService
{
    private const int CommandTimeoutSeconds = 120;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private const decimal B2clThreshold = 100000m;
    private const int OriginalLookbackDays = 550;

    /// <summary>
    /// SEZ buyers are recognised by an SEZ marker in the buyer, ledger, ledger group or sales-ledger name.
    /// Add a buyer GSTIN here when an SEZ unit's ledgers carry no such marker.
    /// </summary>
    private static readonly HashSet<string> SezBuyerGstins = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex SezMarker = new(@"KASEZ|\bSEZ\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> ExportVoucherTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Export Sales", "SALES (FOREX)", "Sales Export" };
    private static readonly HashSet<string> RegisteredOnlyVoucherTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Deemed Export", "Job Invoice", "Rent Invoice", "Commission Invoice", "Branch Transfer Out", "Sales High-seas" };

    public const string SecB2b = "B2B";
    public const string SecB2cl = "B2CL";
    public const string SecB2cs = "B2CS";
    public const string SecExp = "EXP";
    public const string SecCdnr = "CDNR";
    public const string SecCdnur = "CDNUR";
    public const string SecNonGst = "NONGST";
    public const string SecExcluded = "EXCLUDED";
    public const string SecNotReported = "NOTREPORTED";

    public const string TypeRegular = "Regular B2B";
    public const string TypeSezWp = "SEZ supplies with payment";
    public const string TypeSezWop = "SEZ supplies without payment";
    public const string TypeDeemed = "Deemed Exp";
    public const string TypeIntraIgst = "Intra-State supplies attracting IGST";

    private static readonly Regex GstinPattern = new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$", RegexOptions.Compiled);
    private static readonly Regex HsnPattern = new(@"^(\d{4}|\d{6}|\d{8})$", RegexOptions.Compiled);
    private static readonly Regex TrailingFinancialYear = new(@"/\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex DomesticPrinted = new(@"^(?<p>.+/D/)(?<n>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);
    private static readonly decimal[] StandardRates = [0m, 0.1m, 0.25m, 1m, 1.5m, 3m, 5m, 6m, 7.5m, 12m, 18m, 28m, 40m];

    private static readonly Dictionary<string, string> PortalStates = new()
    {
        ["01"] = "Jammu and Kashmir", ["02"] = "Himachal Pradesh", ["03"] = "Punjab", ["04"] = "Chandigarh",
        ["05"] = "Uttarakhand", ["06"] = "Haryana", ["07"] = "Delhi", ["08"] = "Rajasthan", ["09"] = "Uttar Pradesh",
        ["10"] = "Bihar", ["11"] = "Sikkim", ["12"] = "Arunachal Pradesh", ["13"] = "Nagaland", ["14"] = "Manipur",
        ["15"] = "Mizoram", ["16"] = "Tripura", ["17"] = "Meghalaya", ["18"] = "Assam", ["19"] = "West Bengal",
        ["20"] = "Jharkhand", ["21"] = "Odisha", ["22"] = "Chhattisgarh", ["23"] = "Madhya Pradesh", ["24"] = "Gujarat",
        ["26"] = "Dadra and Nagar Haveli and Daman and Diu", ["27"] = "Maharashtra", ["29"] = "Karnataka", ["30"] = "Goa",
        ["31"] = "Lakshadweep", ["32"] = "Kerala", ["33"] = "Tamil Nadu", ["34"] = "Puducherry",
        ["35"] = "Andaman and Nicobar Islands", ["36"] = "Telangana", ["37"] = "Andhra Pradesh", ["38"] = "Ladakh",
        ["96"] = "Other Countries", ["97"] = "Other Territory",
    };

    private static readonly Dictionary<string, (string Title, string Severity)> Categories = new()
    {
        ["unapproved"] = ("Unapproved voucher", "warning"),
        ["gstin"] = ("Invalid / missing GSTIN", "error"),
        ["hsn-missing"] = ("HSN missing", "error"),
        ["hsn-malformed"] = ("HSN malformed", "error"),
        ["hsn-master"] = ("HSN differs from commodity master", "warning"),
        ["export-docs"] = ("Export without shipping bill / port", "warning"),
        ["note-original"] = ("Note without valid original invoice", "warning"),
        ["note-excluded"] = ("Note not reported", "info"),
        ["tax-mismatch"] = ("Line tax differs from ledger tax", "error"),
        ["tax-head"] = ("Tax head vs place of supply", "warning"),
        ["intra-igst"] = ("Intra-state supply with IGST", "info"),
        ["icegate-missing"] = ("Export not in ICEGATE", "warning"),
        ["icegate-extra"] = ("ICEGATE shipping bill not in ERP", "warning"),
        ["icegate-fob"] = ("Taxable value vs ICEGATE FOB", "warning"),
        ["icegate-igst"] = ("IGST vs ICEGATE IGST paid", "warning"),
        ["icegate-ref"] = ("ICEGATE reference mismatch", "warning"),
        ["pos"] = ("Place of supply not resolved", "warning"),
        ["classification"] = ("Check classification", "warning"),
        ["charges"] = ("Taxable charges added to invoice", "info"),
    };

    private const string CompanyInfoSql = @"
SELECT LTRIM(RTRIM(f.Name)) AS Company,
       UPPER(LTRIM(RTRIM(ISNULL(NULLIF(LTRIM(RTRIM(d.NewGSTNo)), ''), ISNULL(f.NewGSTNo, ''))))) AS Gstin,
       LTRIM(RTRIM(ISNULL(f.IndentCode, ''))) AS Code,
       LTRIM(RTRIM(ISNULL(d.NState, ''))) AS State
FROM FactoryInfo f WITH (NOLOCK)
LEFT JOIN Despatch.dbo.FactoryInfo d WITH (NOLOCK) ON d.Name = f.Name";

    private const string StatesSql = @"
SELECT LTRIM(RTRIM(StateName)) AS Name, LTRIM(RTRIM(CAST(STATECODE AS varchar(5)))) AS Code
FROM Despatch.dbo.StateMaster WITH (NOLOCK)";

    private const string PartyColumns = @"
       LTRIM(RTRIM(ISNULL(COALESCE(l.NewGSTNo, l2.NewGSTNo), ''))) AS PartyGstin,
       LTRIM(RTRIM(ISNULL(COALESCE(l.State, l2.State), ''))) AS PartyState,
       LTRIM(RTRIM(ISNULL(COALESCE(l.LedgerName, l2.LedgerName), ''))) AS PartyLedger,
       LTRIM(RTRIM(ISNULL(COALESCE(l.Under, l2.Under), ''))) AS PartyUnder";

    private const string InvoiceSelect = @"
SELECT LTRIM(RTRIM(sv.CompanyName)) AS Company, LTRIM(RTRIM(sv.InvNo)) AS InvNo, sv.InvDate,
       LTRIM(RTRIM(ISNULL(sv.InvYear, ''))) AS InvYear, LTRIM(RTRIM(ISNULL(sv.VoucherType, ''))) AS VoucherType,
       LTRIM(RTRIM(ISNULL(sv.BuyerName, ''))) AS PartyName, ISNULL(sv.BillAMount, 0) AS BillAmount,
       ISNULL(NULLIF(sv.ExchangeRate, 0), 1) AS ExchangeRate, ISNULL(sv.ApprovalStatus, 1) AS ApprovalStatus,
       LTRIM(RTRIM(ISNULL(sv.salesLedger, ''))) AS SalesLedger, LTRIM(RTRIM(ISNULL(sv.Destination, ''))) AS Destination,
       LTRIM(RTRIM(ISNULL(sv.ShippingBillNo, ''))) AS ShippingBillNo, sv.ShippingBDTE AS ShippingBillDate," + PartyColumns;

    private const string InvoicePartyJoins = @"
LEFT JOIN LedgerMaster l WITH (NOLOCK) ON l.srno = sv.BuyerID
OUTER APPLY (SELECT TOP 1 x.NewGSTNo, x.State, x.LedgerName, x.Under FROM LedgerMaster x WITH (NOLOCK)
             WHERE l.srno IS NULL AND x.CompanyName = sv.CompanyName AND x.LedgerName = sv.BuyerName) l2";

    private const string InvoicesSql = InvoiceSelect + @"
FROM SalesVoucher sv WITH (NOLOCK)" + InvoicePartyJoins + @"
WHERE sv.InvDate >= @From AND sv.InvDate < @ToExclusive AND ISNULL(LTRIM(RTRIM(sv.InvNo)), '') <> ''";

    private const string OriginalInvoicesSql = InvoiceSelect + @"
FROM OPENJSON(@Refs) WITH (c varchar(200) '$.c', n varchar(100) '$.n') r
JOIN SalesVoucher sv WITH (NOLOCK) ON sv.CompanyName = r.c AND sv.InvNo = r.n" + InvoicePartyJoins + @"
WHERE sv.InvDate IS NOT NULL";

    private const string LineSelect = @"
SELECT LTRIM(RTRIM(i.CompanyName)) AS Company, LTRIM(RTRIM(i.InvNo)) AS InvNo, LTRIM(RTRIM(ISNULL(i.Invyear, ''))) AS InvYear,
       i.InvDate, LTRIM(RTRIM(ISNULL(i.Commodity, ''))) AS Commodity, LTRIM(RTRIM(ISNULL(i.ItemName, ''))) AS ItemName,
       ISNULL(i.ActualQty, 0) AS Qty, LTRIM(RTRIM(ISNULL(i.Per, ''))) AS Per, ISNULL(i.Amount, 0) AS Amount,
       ISNULL(i.IGSTPer, 0) AS IgstPer, ISNULL(i.CGSTPer, 0) AS CgstPer, ISNULL(i.SGSTPer, 0) AS SgstPer,
       ISNULL(i.IGSTAmount, 0) AS Igst, ISNULL(i.CGSTAmount, 0) AS Cgst, ISNULL(i.SGSTAmount, 0) AS Sgst,
       LTRIM(RTRIM(ISNULL(i.HSNCODE, ''))) AS Hsn, ISNULL(m.THeading, '') AS MasterHsn";

    private const string CommodityApply = @"
OUTER APPLY (SELECT TOP 1 LTRIM(RTRIM(c.THeading)) AS THeading FROM Despatch.dbo.Commodity c WITH (NOLOCK)
             WHERE c.CompanyName = i.CompanyName AND c.CommodityName = i.Commodity ORDER BY c.SrNo DESC) m";

    private const string LinesSql = LineSelect + @"
FROM SalesVoucherItem i WITH (NOLOCK)" + CommodityApply + @"
WHERE i.InvDate >= @From AND i.InvDate < @ToExclusive";

    private const string OriginalLinesSql = LineSelect + @"
FROM OPENJSON(@Refs) WITH (c varchar(200) '$.c', n varchar(100) '$.n') r
JOIN SalesVoucherItem i WITH (NOLOCK) ON i.CompanyName = r.c AND i.InvNo = r.n" + CommodityApply + @"
WHERE i.InvDate IS NOT NULL";

    private const string InvoiceLedgerSql = @"
SELECT LTRIM(RTRIM(t.CompanyName)) AS Company, LTRIM(RTRIM(t.InvNo)) AS DocNo, t.sysDate AS DocDate,
       LTRIM(RTRIM(ISNULL(t.TaxledgerName, ''))) AS Ledger, ISNULL(t.Amount, 0) AS Amount, ISNULL(t.Rate, 0) AS Rate
FROM SalesVoucherTax t WITH (NOLOCK)
WHERE t.sysDate >= @From AND t.sysDate < @ToExclusive AND ISNULL(LTRIM(RTRIM(t.CreditNoteNumber)), '') = ''";

    private const string ExportInfoSql = @"
SELECT LTRIM(RTRIM(CompanyName)) AS Company, LTRIM(RTRIM(Invoice_no)) AS InvNo, Invoice_dt AS InvDate,
       LTRIM(RTRIM(ISNULL(beingexport, ''))) AS BeingExport, LTRIM(RTRIM(ISNULL(PORTCode, ''))) AS PortCode,
       LTRIM(RTRIM(ISNULL(ShippingBillNo, ''))) AS ShippingBillNo, ShippingBDTE AS ShippingBillDate,
       LTRIM(RTRIM(ISNULL(country_desti, ''))) AS Destination, 0 AS Source
FROM Despatch.dbo.Packinglist WITH (NOLOCK)
WHERE Invoice_dt >= DATEADD(DAY, -3, @From) AND Invoice_dt < DATEADD(DAY, 3, @ToExclusive)
UNION ALL
SELECT LTRIM(RTRIM(CompanyName)), LTRIM(RTRIM(InvNo)), InvDate,
       LTRIM(RTRIM(ISNULL(BeingExport, ''))), LTRIM(RTRIM(ISNULL(PORTCode, ''))),
       LTRIM(RTRIM(ISNULL(ShippingBillNo, ''))), ShippingBDTE, LTRIM(RTRIM(ISNULL(DestPlace, ''))), 1
FROM Despatch.dbo.ExportInvoice WITH (NOLOCK)
WHERE InvDate >= DATEADD(DAY, -3, @From) AND InvDate < DATEADD(DAY, 3, @ToExclusive)";

    private const string CreditNotesSql = @"
SELECT LTRIM(RTRIM(c.CompanyName)) AS Company, LTRIM(RTRIM(c.CreditNoteNumber)) AS NoteNo, c.CreditNoteDate AS NoteDate,
       LTRIM(RTRIM(ISNULL(c.CreditType, ''))) AS NoteKind, LTRIM(RTRIM(ISNULL(c.invno, ''))) AS OrigNo, c.invDate AS OrigDate,
       LTRIM(RTRIM(ISNULL(c.PartyName, ''))) AS PartyName, ISNULL(c.basic, 0) AS Basic, ISNULL(c.TotalCreditAmount, 0) AS NoteValue,
       ISNULL(NULLIF(c.ExchangeRate, 0), 1) AS ExchangeRate, ISNULL(c.ApprovalStatus, 1) AS ApprovalStatus,
       LTRIM(RTRIM(ISNULL(c.SalesLedger, ''))) AS Ledger," + PartyColumns + @"
FROM CreditNote c WITH (NOLOCK)
LEFT JOIN LedgerMaster l WITH (NOLOCK) ON l.srno = c.PartyID
OUTER APPLY (SELECT TOP 1 x.NewGSTNo, x.State, x.LedgerName, x.Under FROM LedgerMaster x WITH (NOLOCK)
             WHERE l.srno IS NULL AND x.CompanyName = c.CompanyName AND x.LedgerName = c.PartyName) l2
WHERE c.CreditNoteDate >= @From AND c.CreditNoteDate < @ToExclusive AND ISNULL(LTRIM(RTRIM(c.CreditNoteNumber)), '') <> ''";

    private const string CreditNoteItemsSql = @"
SELECT LTRIM(RTRIM(i.CompanyName)) AS Company, LTRIM(RTRIM(i.CreditNoteNumber)) AS InvNo, '' AS InvYear, c.CreditNoteDate AS InvDate,
       LTRIM(RTRIM(ISNULL(i.Commodity, ''))) AS Commodity, LTRIM(RTRIM(ISNULL(i.ItemDesc, ''))) AS ItemName,
       ISNULL(i.Qty, 0) AS Qty, LTRIM(RTRIM(ISNULL(i.Unit, ''))) AS Per, ISNULL(i.Amount, 0) AS Amount,
       ISNULL(i.IGSTPer, 0) AS IgstPer, ISNULL(i.CGSTPer, 0) AS CgstPer, ISNULL(i.SGSTPer, 0) AS SgstPer,
       ISNULL(i.IGSTAmount, 0) AS Igst, ISNULL(i.CGSTAmount, 0) AS Cgst, ISNULL(i.SGSTAmount, 0) AS Sgst,
       '' AS Hsn, ISNULL(m.THeading, '') AS MasterHsn
FROM CreditNoteItem i WITH (NOLOCK)
JOIN CreditNote c WITH (NOLOCK) ON c.CompanyName = i.CompanyName AND c.CreditNoteNumber = i.CreditNoteNumber" + CommodityApply + @"
WHERE c.CreditNoteDate >= @From AND c.CreditNoteDate < @ToExclusive";

    private const string CreditNoteLedgerSql = @"
SELECT LTRIM(RTRIM(t.CompanyName)) AS Company, LTRIM(RTRIM(t.CreditNoteNumber)) AS DocNo, t.sysDate AS DocDate,
       LTRIM(RTRIM(ISNULL(t.TaxledgerName, ''))) AS Ledger, ISNULL(t.Amount, 0) AS Amount, ISNULL(t.Rate, 0) AS Rate
FROM SalesVoucherTax t WITH (NOLOCK)
JOIN CreditNote c WITH (NOLOCK) ON c.CompanyName = t.CompanyName AND c.CreditNoteNumber = t.CreditNoteNumber
WHERE c.CreditNoteDate >= @From AND c.CreditNoteDate < @ToExclusive";

    /// <summary>Same sales-side rule as the Document Summary: GST OUTPUT tax row, or a party under a Debtors group.</summary>
    private const string DebitNotesSql = @"
SELECT LTRIM(RTRIM(d.CompanyName)) AS Company, LTRIM(RTRIM(d.DebitNoteNumber)) AS NoteNo, d.sysdate AS NoteDate,
       LTRIM(RTRIM(ISNULL(d.DebitType, ''))) AS NoteKind, LTRIM(RTRIM(ISNULL(d.BillNo, ''))) AS OrigNo, d.billDate AS OrigDate,
       LTRIM(RTRIM(ISNULL(d.PartyName, ''))) AS PartyName,
       COALESCE(NULLIF(d.Basic, 0), NULLIF(d.BillAMount, 0), 0) AS Basic, ISNULL(d.TotalDebitAmount, 0) AS NoteValue,
       ISNULL(NULLIF(d.ExchangeRate, 0), 1) AS ExchangeRate, ISNULL(d.ApprovalStatus, 1) AS ApprovalStatus,
       LTRIM(RTRIM(ISNULL(d.PurchaseLedger, ''))) AS Ledger,
       LTRIM(RTRIM(ISNULL(l.NewGSTNo, ''))) AS PartyGstin, LTRIM(RTRIM(ISNULL(l.State, ''))) AS PartyState,
       LTRIM(RTRIM(ISNULL(l.LedgerName, ''))) AS PartyLedger, LTRIM(RTRIM(ISNULL(l.Under, ''))) AS PartyUnder
FROM DebitNote d WITH (NOLOCK)
LEFT JOIN LedgerMaster l WITH (NOLOCK) ON l.srno = d.PartyID
WHERE d.sysdate >= @From AND d.sysdate < @ToExclusive AND ISNULL(LTRIM(RTRIM(d.DebitNoteNumber)), '') <> ''
  AND (EXISTS (SELECT 1 FROM PurchaseVoucherTax t WITH (NOLOCK)
               WHERE t.CompanyName = d.CompanyName AND t.Debitnotenumber = d.DebitNoteNumber AND t.TaxledgerName LIKE '%OUTPUT%')
       OR l.Under LIKE 'Debtor%')";

    private const string DebitNoteLedgerSql = @"
SELECT LTRIM(RTRIM(t.CompanyName)) AS Company, LTRIM(RTRIM(t.Debitnotenumber)) AS DocNo, t.sysDate AS DocDate,
       LTRIM(RTRIM(ISNULL(t.TaxledgerName, ''))) AS Ledger, ISNULL(t.Amount, 0) AS Amount, ISNULL(t.Rate, 0) AS Rate
FROM PurchaseVoucherTax t WITH (NOLOCK)
JOIN DebitNote d WITH (NOLOCK) ON d.CompanyName = t.CompanyName AND d.DebitNoteNumber = t.Debitnotenumber
WHERE d.sysdate >= @From AND d.sysdate < @ToExclusive AND t.TaxledgerName LIKE '%OUTPUT%'";

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private readonly GstDocumentSummaryService _documentSummary;
    private readonly IcegateService _icegate;

    public Gstr1ReturnService(DatabaseService database, IMemoryCache cache, GstDocumentSummaryService documentSummary, IcegateService icegate)
    {
        _database = database;
        _cache = cache;
        _documentSummary = documentSummary;
        _icegate = icegate;
    }

    public async Task<Gstr1ReportDto> GetReportAsync(DateTime from, DateTime to, string? scope, bool includeUnapproved, bool refresh = false)
    {
        var fromDate = from.Date;
        var toDate = to.Date < fromDate ? fromDate : to.Date;
        var data = await LoadBaseAsync(fromDate, toDate, refresh);
        var docSummary = await _documentSummary.GetSummaryAsync(fromDate, toDate, refresh);
        var trialBalance = await LoadTrialBalanceAsync(fromDate, toDate, refresh);
        var report = BuildReport(data, scope?.Trim() ?? "", includeUnapproved, docSummary.Rows, trialBalance);
        return ApplyIcegate(report, data, _icegate.Snapshot(), _icegate.GetStatus());
    }

    /// <summary>
    /// Full trial balance from vw_LedgerSummary — the ERP's ledger posting view: LedgerMaster opening balances
    /// (dated 2012-03-31) plus every voucher; debit = positive amount. Opening = all postings before the period;
    /// for P&amp;L groups only postings from 1 April of the financial year.
    /// </summary>
    private const string TrialBalanceSql = @"
WITH t AS (
    SELECT LTRIM(RTRIM(l.CompanyName)) AS Company, LTRIM(RTRIM(ISNULL(l.LedgerName, ''))) AS Ledger,
           SUM(CASE WHEN l.Date < @From THEN l.amount ELSE 0 END) AS OpenAll,
           SUM(CASE WHEN l.Date >= @FyStart AND l.Date < @From THEN l.amount ELSE 0 END) AS OpenFy,
           SUM(CASE WHEN l.Date >= @From AND l.amount > 0 THEN l.amount ELSE 0 END) AS Debit,
           SUM(CASE WHEN l.Date >= @From AND l.amount < 0 THEN -l.amount ELSE 0 END) AS Credit
    FROM vw_LedgerSummary l
    WHERE l.Date < @ToExclusive
    GROUP BY LTRIM(RTRIM(l.CompanyName)), LTRIM(RTRIM(ISNULL(l.LedgerName, ''))))
SELECT t.Company, t.Ledger, ISNULL(m.Under, '') AS Under,
       CAST(t.OpenAll AS decimal(18, 2)) AS OpenAll, CAST(t.OpenFy AS decimal(18, 2)) AS OpenFy,
       CAST(t.Debit AS decimal(18, 2)) AS Debit, CAST(t.Credit AS decimal(18, 2)) AS Credit
FROM t
OUTER APPLY (SELECT TOP 1 LTRIM(RTRIM(x.Under)) AS Under FROM LedgerMaster x WITH (NOLOCK)
             WHERE x.CompanyName = t.Company AND x.LedgerName = t.Ledger) m
WHERE ROUND(t.OpenAll, 2) <> 0 OR ROUND(t.Debit, 2) <> 0 OR ROUND(t.Credit, 2) <> 0";

    private const string GroupLedgerSql = @"
SELECT LTRIM(RTRIM(CompanyName)) AS Company, LTRIM(RTRIM(ISNULL(ExpenseHead, ''))) AS Head,
       LTRIM(RTRIM(ISNULL(ExpenseGroupHead, ''))) AS GroupHead, LTRIM(RTRIM(ISNULL(b, ''))) AS B, LTRIM(RTRIM(ISNULL(c, ''))) AS C,
       LTRIM(RTRIM(ISNULL(d, ''))) AS D, LTRIM(RTRIM(ISNULL(e, ''))) AS E, LTRIM(RTRIM(ISNULL(f, ''))) AS F,
       LTRIM(RTRIM(ISNULL(IsPnl, ''))) AS IsPnl
FROM Vw_GroupLedger";

    private static readonly string[] PrimaryOrder = ["Liabilities", "Assets", "Income", "Expenses"];
    public const string TbAdjustmentGroup = "Adjustments";

    private async Task<List<Gstr1TrialBalanceRowDto>> LoadTrialBalanceAsync(DateTime from, DateTime to, bool refresh)
    {
        var key = $"gstr1-tb-v2|{from:yyyy-MM-dd}|{to:yyyy-MM-dd}";
        if (refresh)
            _cache.Remove(key);
        if (_cache.TryGetValue(key, out List<Gstr1TrialBalanceRowDto>? cached) && cached is not null)
            return cached;

        var fyStart = new DateTime(from.Month >= 4 ? from.Year : from.Year - 1, 4, 1);
        await using var conn = _database.CreateConnection();
        var raw = (await conn.QueryAsync<TrialBalanceRow>(TrialBalanceSql,
            new { From = from, ToExclusive = to.AddDays(1), FyStart = fyStart }, commandTimeout: CommandTimeoutSeconds)).ToList();
        var groupRows = await conn.QueryAsync<GroupLedgerRow>(GroupLedgerSql, commandTimeout: CommandTimeoutSeconds);

        var groups = new Dictionary<(string Company, string Name), (string Head, string Group, bool Pnl)>();
        foreach (var g in groupRows)
        {
            var info = (g.Head, g.GroupHead.Length > 0 ? g.GroupHead : g.Head, string.Equals(g.IsPnl, "yes", StringComparison.OrdinalIgnoreCase));
            foreach (var name in new[] { g.Head, g.GroupHead, g.B, g.C, g.D, g.E, g.F })
                if (name.Length > 0)
                    groups.TryAdd((g.Company.ToUpperInvariant(), name.ToUpperInvariant()), info);
        }

        static (decimal Dr, decimal Cr) Split(decimal v) => v >= 0 ? (R(v), 0m) : (0m, R(-v));
        var rows = new List<Gstr1TrialBalanceRowDto>();
        foreach (var company in raw.GroupBy(r => r.Company, StringComparer.OrdinalIgnoreCase))
        {
            decimal priorPnl = 0, openingTotal = 0, debitTotal = 0, creditTotal = 0;
            foreach (var r in company)
            {
                var found = groups.TryGetValue((r.Company.ToUpperInvariant(), r.Under.ToUpperInvariant()), out var info);
                var head = found ? info.Head : "Unclassified";
                var group = found ? info.Group : r.Under.Length > 0 ? r.Under : "(no group)";
                var pnl = found && info.Pnl;
                var opening = pnl ? r.OpenFy : r.OpenAll;
                if (pnl) priorPnl += r.OpenAll - r.OpenFy;
                openingTotal += r.OpenAll;
                debitTotal += r.Debit;
                creditTotal += r.Credit;
                if (R(opening) == 0 && R(r.Debit) == 0 && R(r.Credit) == 0) continue;
                var (od, oc) = Split(opening);
                var (cd, cc) = Split(opening + r.Debit - r.Credit);
                rows.Add(new Gstr1TrialBalanceRowDto(r.Company, head, group, r.Under, r.Ledger.Length > 0 ? r.Ledger : "(blank ledger)",
                    od, oc, R(r.Debit), R(r.Credit), cd, cc, pnl, false));
            }
            if (R(priorPnl) != 0)
            {
                var (od, oc) = Split(priorPnl);
                rows.Add(new Gstr1TrialBalanceRowDto(company.Key, "Liabilities", TbAdjustmentGroup, "",
                    "Profit & Loss A/c — previous years (P&L groups reset on 1 April)", od, oc, 0, 0, od, oc, false, true));
            }
            var openingDiff = -openingTotal;
            var periodDiff = creditTotal - debitTotal;
            if (R(openingDiff) != 0 || R(periodDiff) != 0)
            {
                var (od, oc) = Split(openingDiff);
                var (pd, pc) = Split(periodDiff);
                var (cd, cc) = Split(openingDiff + periodDiff);
                rows.Add(new Gstr1TrialBalanceRowDto(company.Key, "Unclassified", TbAdjustmentGroup, "",
                    "Difference in ERP postings (unbalanced opening balances / vouchers)", od, oc, pd, pc, cd, cc, false, true));
            }
        }

        int HeadRank(string h) { var i = Array.FindIndex(PrimaryOrder, p => string.Equals(p, h, StringComparison.OrdinalIgnoreCase)); return i < 0 ? 99 : i; }
        rows = rows.OrderBy(r => r.IsAdjustment ? 1 : 0).ThenBy(r => HeadRank(r.Primary)).ThenBy(r => r.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Under, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Ledger, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Company, StringComparer.OrdinalIgnoreCase).ToList();
        _cache.Set(key, rows, CacheTtl);
        return rows;
    }

    // ---------------------------------------------------------------- ICEGATE

    private const decimal IcegateValueTolerance = 10m;

    private static Gstr1ReportDto ApplyIcegate(Gstr1ReportDto report, BaseData data, IReadOnlyList<IcegateShippingBill> bills, IcegateStatusDto status)
    {
        var last = status.Uploads.FirstOrDefault();
        if (bills.Count == 0)
            return report with
            {
                Exp = report.Exp.Select(e => e with { IcegateStatus = "No ICEGATE data" }).ToList(),
                Icegate = new Gstr1IcegateSummaryDto(false, 0, null, null, 0, 0, 0, report.Exp.Select(e => (e.Company, e.ErpInvoiceNo)).Distinct().Count(), []),
            };

        var erp = data.Docs.Where(d => d.Kind == "INV" && d.Section == SecExp)
            .Select(d => new IcegateErpInvoice(d.Company, d.PrintedNo, d.ErpNo, d.Date, d.PortCode, d.ShippingBillNo,
                d.ShippingBillDate, R(d.Lines.Sum(l => l.Taxable)), R(d.Lines.Sum(l => l.Igst)), d.SubType))
            .ToList();
        var erpByKey = erp.GroupBy(e => (e.Company, e.ErpNo)).ToDictionary(g => g.Key, g => g.First());
        var result = IcegateService.Match(erp, bills);

        bool InPeriod(DateTime? d) => d is { } x && x >= data.From && x <= data.To;
        var inScope = report.Scope.Length == 0 ? null : new HashSet<string>(report.Companies, StringComparer.OrdinalIgnoreCase);
        bool InScope(string company) => inScope is null || inScope.Contains(company);

        var covered = result.Matches.Keys.Select(k => k.Company)
            .Concat(result.Unmatched.Where(u => u.Company.Length > 0 && InPeriod(u.Invoice?.Date ?? u.Bill.SbDate)).Select(u => u.Company))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var exceptions = new List<Gstr1ExceptionDto>();
        void Issue(string category, string company, string docNo, DateTime? date, string party, string detail, decimal? amount)
        {
            var (title, severity) = Categories[category];
            if (category == "icegate-igst" && detail.StartsWith('!'))
            {
                severity = "error";
                detail = detail[1..];
            }
            exceptions.Add(new Gstr1ExceptionDto(category, title, severity, company, docNo, date, party, detail, amount));
        }

        var filled = new HashSet<(string Company, string InvoiceNo)>();
        var statusByInvoice = new Dictionary<(string, string), string>();
        var exp = new List<Gstr1ExpRowDto>();
        foreach (var group in report.Exp.GroupBy(e => (e.Company, e.ErpInvoiceNo)))
        {
            var first = group.First();
            if (!erpByKey.TryGetValue(group.Key, out var inv) || !result.Matches.TryGetValue(inv, out var match))
            {
                var missing = covered.Contains(group.Key.Company);
                if (missing)
                    Issue("icegate-missing", first.Company, first.InvoiceNo, first.InvoiceDate, first.ReceiverName,
                        $"Export invoice not found in the uploaded ICEGATE data (shipping bill {(first.ShippingBillNo.Length > 0 ? first.ShippingBillNo : "not recorded in ERP")})",
                        group.Sum(e => e.Taxable));
                var label = missing ? "Not in ICEGATE" : "No ICEGATE data";
                statusByInvoice[group.Key] = label;
                exp.AddRange(group.Select(e => e with { IcegateStatus = label }));
                continue;
            }

            var bill = match.Bill;
            var line = match.Invoice;
            var notes = new List<string>();
            var (billTaxable, billCount) = result.BillTaxable[bill];
            var shared = billCount > 1 ? $" (shipping bill covers {billCount} ERP invoices)" : "";

            if (bill.Fob is null)
                notes.Add("FOB not available in ICEGATE");
            else if (Math.Abs(billTaxable - bill.Fob.Value) > IcegateValueTolerance)
            {
                var diff = billTaxable - bill.Fob.Value;
                notes.Add($"Taxable {Money(billTaxable)} vs FOB {Money(bill.Fob.Value)}{shared}");
                Issue("icegate-fob", first.Company, first.InvoiceNo, first.InvoiceDate, first.ReceiverName,
                    $"Taxable value {Money(billTaxable)} differs from ICEGATE FOB {Money(bill.Fob.Value)} by {Money(diff)}{shared}", diff);
            }

            var iceIgst = line?.IgstPaid ?? 0m;
            if (line is not null && Math.Abs(inv.Igst - iceIgst) > IcegateValueTolerance)
            {
                var hard = (inv.Igst > 0) != (iceIgst > 0);
                notes.Add($"IGST ERP {Money(inv.Igst)} vs ICEGATE {(line.IgstPaid is null ? "not paid" : Money(iceIgst))}");
                Issue("icegate-igst", first.Company, first.InvoiceNo, first.InvoiceDate, first.ReceiverName,
                    (hard ? "!" : "") + $"IGST in ERP {Money(inv.Igst)} ({first.ExportType}) but ICEGATE shows {(line.IgstPaid is null ? "no IGST paid" : Money(iceIgst))}",
                    inv.Igst - iceIgst);
            }

            var refs = new List<string>();
            if (line is null)
                refs.Add("no invoice line in ICEGATE IGST sheet");
            else
            {
                var n = IcegateService.NormInvoice(line.No);
                if (n != IcegateService.NormInvoice(inv.PrintedNo) && n != IcegateService.NormInvoice(inv.ErpNo))
                    refs.Add($"invoice no {line.No} vs ERP {first.InvoiceNo}");
                if (line.Date is { } ld && ld.Date != inv.Date.Date)
                    refs.Add($"invoice date {ld:dd-MMM-yyyy} vs ERP {inv.Date:dd-MMM-yyyy}");
            }
            if (inv.ShippingBillNo.Length > 0 && IcegateService.NormSb(inv.ShippingBillNo) != bill.SbNo)
                refs.Add($"shipping bill {bill.SbNo} vs ERP {inv.ShippingBillNo}");
            else if (inv.ShippingBillDate is { } ed && bill.SbDate is { } bd && ed.Date != bd.Date)
                refs.Add($"SB date {bd:dd-MMM-yyyy} vs ERP {ed:dd-MMM-yyyy}");
            if (inv.PortCode.Length > 0 && bill.PortCode.Length > 0 && !string.Equals(inv.PortCode, bill.PortCode, StringComparison.OrdinalIgnoreCase))
                refs.Add($"port {bill.PortCode} vs ERP {inv.PortCode}");
            if (refs.Count > 0)
            {
                notes.AddRange(refs);
                Issue("icegate-ref", first.Company, first.InvoiceNo, first.InvoiceDate, first.ReceiverName,
                    "ICEGATE " + string.Join("; ", refs) + $" (matched by {match.MatchedBy})", null);
            }

            var fill = first.ShippingBillNo.Length == 0 || first.PortCode.Length == 0 || first.ShippingBillDate is null;
            if (fill)
                filled.Add((first.Company, first.InvoiceNo));
            var label2 = notes.Count == 0 ? "Matched" : "Differences";
            statusByInvoice[group.Key] = label2;
            var isFirst = true;
            foreach (var e in group)
            {
                exp.Add(e with
                {
                    PortCode = e.PortCode.Length > 0 ? e.PortCode : bill.PortCode,
                    ShippingBillNo = e.ShippingBillNo.Length > 0 ? e.ShippingBillNo : bill.SbNo,
                    ShippingBillDate = e.ShippingBillDate ?? bill.SbDate,
                    Fob = isFirst ? bill.Fob : null,
                    EgmNo = bill.EgmNo,
                    EgmDate = bill.EgmDate,
                    IcegateSbNo = bill.SbNo,
                    IcegateInvoiceNo = line?.No ?? "",
                    IcegateInvoiceDate = line?.Date,
                    IcegateIgst = isFirst ? line?.IgstPaid : null,
                    IcegateStatus = label2,
                    IcegateNote = string.Join("; ", notes),
                    FilledFromIcegate = fill,
                });
                isFirst = false;
            }
        }

        var notInErp = new List<Gstr1IcegateExtraDto>();
        foreach (var u in result.Unmatched)
        {
            var date = u.Invoice?.Date ?? u.Bill.SbDate;
            if (!InPeriod(date)) continue;
            if (u.Company.Length > 0 ? !InScope(u.Company) : inScope is not null) continue;
            notInErp.Add(new Gstr1IcegateExtraDto(u.Company, u.Bill.CompanyLabel, u.Bill.PortCode, u.Bill.SbNo, u.Bill.SbDate,
                u.Invoice?.No ?? "", u.Invoice?.Date, u.Bill.Fob, u.Invoice?.IgstPaid, u.Bill.EgmNo, u.Bill.EgmDate));
            Issue("icegate-extra", u.Company.Length > 0 ? u.Company : $"ICEGATE: {u.Bill.CompanyLabel}",
                u.Invoice?.No ?? $"SB {u.Bill.SbNo}", date, "",
                $"Shipping bill {u.Bill.SbNo} ({u.Bill.PortCode}, {u.Bill.SbDate:dd-MMM-yyyy}) has no matching ERP export invoice in this period",
                u.Bill.Fob);
        }

        // ICEGATE supplied the missing port / shipping bill, so the ERP export-docs warning no longer applies.
        var kept = report.Exceptions.Where(e => !(e.Category == "export-docs" && filled.Contains((e.Company, e.DocumentNo))
                                                   && exp.Any(x => x.Company == e.Company && x.InvoiceNo == e.DocumentNo
                                                                   && x.PortCode.Length > 0 && x.ShippingBillNo.Length > 0 && x.ShippingBillDate is not null)));
        var allExceptions = kept.Concat(exceptions)
            .OrderBy(e => SeverityOrder(e.Severity)).ThenBy(e => e.Category).ThenBy(e => e.Company)
            .ThenBy(e => e.DocumentDate).ThenBy(e => e.DocumentNo, StringComparer.OrdinalIgnoreCase).ToList();

        var counts = statusByInvoice.Values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
        int Count(string k) => counts.TryGetValue(k, out var n) ? n : 0;
        return report with
        {
            Exp = exp,
            Exceptions = allExceptions,
            Icegate = new Gstr1IcegateSummaryDto(true, bills.Count, last?.UploadedAtUtc, last?.FileName,
                Count("Matched"), Count("Differences"), Count("Not in ICEGATE"), Count("No ICEGATE data"), notInErp),
        };
    }

    private static string Money(decimal v) => v.ToString("N2", CultureInfo.GetCultureInfo("en-IN"));

    public async Task<(byte[] Bytes, Gstr1ReportDto Report)> BuildExcelAsync(DateTime from, DateTime to, string? scope, bool includeUnapproved)
    {
        var report = await GetReportAsync(from, to, scope, includeUnapproved);
        return (Gstr1ExcelBuilder.Build(report), report);
    }

    // ---------------------------------------------------------------- loading

    private async Task<BaseData> LoadBaseAsync(DateTime from, DateTime to, bool refresh)
    {
        var key = $"gstr1-base-v3|{from:yyyy-MM-dd}|{to:yyyy-MM-dd}";
        if (refresh)
            _cache.Remove(key);
        if (_cache.TryGetValue(key, out BaseData? cached) && cached is not null)
            return cached;

        await using var conn = _database.CreateConnection();
        var args = new { From = from, ToExclusive = to.AddDays(1) };

        var companyRows = await conn.QueryAsync<CompanyRow>(CompanyInfoSql, commandTimeout: CommandTimeoutSeconds);
        var states = await conn.QueryAsync<(string Name, string Code)>(StatesSql, commandTimeout: CommandTimeoutSeconds);
        var domesticPrefixes = (await conn.QueryAsync<(string Company, string Prefix)>(GstDocumentSummaryService.DomesticPrefixSql, commandTimeout: CommandTimeoutSeconds))
            .GroupBy(x => x.Company, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Prefix, StringComparer.OrdinalIgnoreCase);

        var invoices = (await conn.QueryAsync<InvoiceRow>(InvoicesSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var lines = (await conn.QueryAsync<LineRow>(LinesSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var invoiceLedger = (await conn.QueryAsync<LedgerRow>(InvoiceLedgerSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var exportInfo = (await conn.QueryAsync<ExportRow>(ExportInfoSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var creditNotes = (await conn.QueryAsync<NoteRow>(CreditNotesSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var creditItems = (await conn.QueryAsync<LineRow>(CreditNoteItemsSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var creditLedger = (await conn.QueryAsync<LedgerRow>(CreditNoteLedgerSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var debitNotes = (await conn.QueryAsync<NoteRow>(DebitNotesSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();
        var debitLedger = (await conn.QueryAsync<LedgerRow>(DebitNoteLedgerSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();

        var refs = creditNotes.Concat(debitNotes)
            .SelectMany(n => ReferenceVariants(n.OrigNo).Select(v => (n.Company, No: v)))
            .Distinct()
            .Select(x => new { c = x.Company, n = x.No })
            .ToList();
        var originals = new List<InvoiceRow>();
        var originalLines = new List<LineRow>();
        if (refs.Count > 0)
        {
            var json = JsonSerializer.Serialize(refs);
            originals = (await conn.QueryAsync<InvoiceRow>(OriginalInvoicesSql, new { Refs = json }, commandTimeout: CommandTimeoutSeconds)).ToList();
            originalLines = (await conn.QueryAsync<LineRow>(OriginalLinesSql, new { Refs = json }, commandTimeout: CommandTimeoutSeconds)).ToList();
        }

        var builder = new Builder(from, to, companyRows, states, domesticPrefixes, invoices, lines, invoiceLedger, exportInfo,
            creditNotes, creditItems, creditLedger, debitNotes, debitLedger, originals, originalLines);
        var data = builder.Build();
        _cache.Set(key, data, CacheTtl);
        return data;
    }

    // ---------------------------------------------------------------- scoped report

    private static Gstr1ReportDto BuildReport(BaseData data, string scopeKey, bool includeUnapproved, IReadOnlyList<GstDocumentSeriesDto> docRows,
        IReadOnlyList<Gstr1TrialBalanceRowDto> trialBalance)
    {
        var activeCompanies = data.Docs.Select(d => d.Company)
            .Concat(docRows.Select(r => r.Company))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var scopes = BuildScopes(activeCompanies, data.Companies);
        var scope = scopes.FirstOrDefault(s => string.Equals(s.Key, scopeKey, StringComparison.OrdinalIgnoreCase))
                    ?? scopes[0];
        var inScope = scope.Key.Length == 0
            ? null
            : new HashSet<string>(scope.Companies, StringComparer.OrdinalIgnoreCase);
        bool InScope(string company) => inScope is null || inScope.Contains(company);

        var scoped = data.Docs.Where(d => InScope(d.Company)).ToList();
        var reported = scoped.Where(d => (includeUnapproved || d.Approved)
                                         && d.Section is not (SecExcluded or SecNotReported)).ToList();
        var invoices = reported.Where(d => d.Kind == "INV").ToList();
        var notes = reported.Where(d => d.Kind != "INV").ToList();

        var b2b = new List<Gstr1B2bRowDto>();
        foreach (var d in Ordered(invoices.Where(d => d.Section == SecB2b)))
        {
            var firstRate = true;
            foreach (var g in ByRate(d.Lines.Where(l => l.Rate > 0 || d.SubType != TypeRegular)))
            {
                b2b.Add(new Gstr1B2bRowDto(d.Company, d.PartyGstin, d.Party, d.PrintedNo, d.ErpNo, d.Date, R(d.DocValue),
                    PosLabel(d.PosCode), "N", d.SubType, g.Rate, R(g.Taxable), R(g.Igst), R(g.Cgst), R(g.Sgst), 0m, d.ErpType, d.Approved,
                    d.SalesLedger, firstRate ? R(d.Tcs) : 0m, firstRate ? R(d.OtherCharges) : 0m));
                firstRate = false;
            }
        }

        var b2cl = new List<Gstr1B2clRowDto>();
        foreach (var d in Ordered(invoices.Where(d => d.Section == SecB2cl)))
            foreach (var g in ByRate(d.Lines.Where(l => l.Rate > 0)))
                b2cl.Add(new Gstr1B2clRowDto(d.Company, d.PrintedNo, d.ErpNo, d.Date, R(d.DocValue), PosLabel(d.PosCode),
                    g.Rate, R(g.Taxable), R(g.Igst), 0m, d.Party, d.ErpType, d.Approved));

        var b2cs = AggregateB2cs(reported.Where(d => d.Section == SecB2cs));
        var b2csInvoicesOnly = AggregateB2cs(invoices.Where(d => d.Section == SecB2cs));

        var exp = new List<Gstr1ExpRowDto>();
        foreach (var d in Ordered(invoices.Where(d => d.Section == SecExp)))
            foreach (var g in ByRate(d.Lines))
                exp.Add(new Gstr1ExpRowDto(d.Company, d.SubType, d.PrintedNo, d.ErpNo, d.Date, R(d.DocValue), d.PortCode,
                    d.ShippingBillNo, d.ShippingBillDate, g.Rate, R(g.Taxable), R(g.Igst), 0m, d.Party, d.ErpType, d.Approved));

        var cdnr = NoteRows(notes.Where(d => d.Section == SecCdnr));
        var cdnur = NoteRows(notes.Where(d => d.Section == SecCdnur));

        var nil = new Dictionary<string, (decimal Nil, decimal Exempt, decimal NonGst)>
        {
            [NilInterReg] = default, [NilIntraReg] = default, [NilInterUnreg] = default, [NilIntraUnreg] = default,
        };
        foreach (var d in invoices)
        {
            var bucket = NilBucket(d);
            if (d.Section == SecNonGst)
            {
                var cur = nil[bucket];
                nil[bucket] = (cur.Nil, cur.Exempt, cur.NonGst + d.Lines.Sum(l => l.Taxable));
            }
            else if (d.Section is SecB2cl or SecB2cs || (d.Section == SecB2b && d.SubType == TypeRegular))
            {
                var zero = d.Lines.Where(l => l.Rate == 0).Sum(l => l.Taxable);
                if (zero == 0) continue;
                var cur = nil[bucket];
                nil[bucket] = (cur.Nil + zero, cur.Exempt, cur.NonGst);
            }
        }
        var nilRows = nil.Select(kv => new Gstr1NilRowDto(kv.Key, R(kv.Value.Nil), R(kv.Value.Exempt), R(kv.Value.NonGst))).ToList();

        var hsnB2b = AggregateHsn(reported.Where(d =>
            (d.Kind == "INV" && d.Section == SecB2b) || (d.Kind != "INV" && d.Section == SecCdnr)));
        var hsnB2c = AggregateHsn(reported.Where(d =>
            (d.Kind == "INV" && d.Section is SecB2cl or SecB2cs or SecExp) || (d.Kind != "INV" && d.Section is SecCdnur or SecB2cs)));
        var hsnDocs = reported.Where(d =>
            (d.Kind == "INV" && d.Section is SecB2b or SecB2cl or SecB2cs or SecExp) || (d.Kind != "INV" && d.Section is SecCdnr or SecCdnur or SecB2cs)).ToList();
        var hsnSummary = AggregateHsn(hsnDocs);
        var salesRegister = BuildSalesRegister(scoped, hsnDocs, includeUnapproved);
        var tbRows = trialBalance.Where(t => InScope(t.Company)).ToList();
        var noteLedgers = scoped.Where(d => d.Kind != "INV" && d.SalesLedger.Length > 0).Select(d => d.SalesLedger);
        var ledgerRecon = BuildLedgerRecon(salesRegister, hsnDocs, tbRows, noteLedgers);

        var docs = docRows.Where(r => InScope(r.Company)).ToList();
        var exceptions = data.Exceptions.Where(e => InScope(e.Company))
            .Select(e => e.Category == "unapproved"
                ? e with { Detail = e.Detail + (includeUnapproved ? " — included in this return" : " — excluded from this return") }
                : e)
            .ToList();

        var summary = new List<Gstr1SectionSummaryDto>
        {
            SectionSummary(SecB2b, "B2B, SEZ, Deemed exports (4A, 4B, 6B, 6C)", b2b.Count == 0 ? 0 : b2b.Select(r => (r.Company, r.ErpInvoiceNo)).Distinct().Count(),
                b2b.GroupBy(r => (r.Company, r.ErpInvoiceNo)).Sum(g => g.First().InvoiceValue),
                b2b.Sum(r => r.Taxable), b2b.Sum(r => r.Igst), b2b.Sum(r => r.Cgst), b2b.Sum(r => r.Sgst)),
            SectionSummary(SecB2cl, "B2C Large (5)", b2cl.Select(r => (r.Company, r.ErpInvoiceNo)).Distinct().Count(),
                b2cl.GroupBy(r => (r.Company, r.ErpInvoiceNo)).Sum(g => g.First().InvoiceValue),
                b2cl.Sum(r => r.Taxable), b2cl.Sum(r => r.Igst), 0, 0),
            SectionSummary(SecB2cs, "B2C Small (7) — net of notes", b2cs.Sum(r => r.Documents), 0,
                b2cs.Sum(r => r.Taxable), b2cs.Sum(r => r.Igst), b2cs.Sum(r => r.Cgst), b2cs.Sum(r => r.Sgst)),
            SectionSummary(SecExp, "Exports (6A)", exp.Select(r => (r.Company, r.ErpInvoiceNo)).Distinct().Count(),
                exp.GroupBy(r => (r.Company, r.ErpInvoiceNo)).Sum(g => g.First().InvoiceValue),
                exp.Sum(r => r.Taxable), exp.Sum(r => r.Igst), 0, 0),
            NoteSummary("CDNR-C", "Credit notes — registered (9B)", cdnr.Where(r => r.NoteType == "C").ToList()),
            NoteSummary("CDNR-D", "Debit notes — registered (9B)", cdnr.Where(r => r.NoteType == "D").ToList()),
            NoteSummary("CDNUR-C", "Credit notes — unregistered (9B)", cdnur.Where(r => r.NoteType == "C").ToList()),
            NoteSummary("CDNUR-D", "Debit notes — unregistered (9B)", cdnur.Where(r => r.NoteType == "D").ToList()),
            SectionSummary("NIL", "Nil rated, exempted, non-GST (8)", 0, 0,
                nilRows.Sum(r => r.NilRated + r.Exempted + r.NonGst), 0, 0, 0),
        };
        var plus = summary.Where(s => s.Section is SecB2b or SecB2cl or SecB2cs or SecExp or "NIL" or "CDNR-D" or "CDNUR-D").ToList();
        var minus = summary.Where(s => s.Section is "CDNR-C" or "CDNUR-C").ToList();
        summary.Add(new Gstr1SectionSummaryDto("NET", "Net outward supplies", 0, 0,
            R(plus.Sum(s => s.Taxable) - minus.Sum(s => s.Taxable)),
            R(plus.Sum(s => s.Igst) - minus.Sum(s => s.Igst)),
            R(plus.Sum(s => s.Cgst) - minus.Sum(s => s.Cgst)),
            R(plus.Sum(s => s.Sgst) - minus.Sum(s => s.Sgst)), 0));
        summary.Add(new Gstr1SectionSummaryDto("DOCS", "Documents issued (13)", (int)docs.Sum(r => r.NetIssued), 0, 0, 0, 0, 0, 0));

        // Reconciliation of the invoice sections against the raw ERP lines.
        var scopedInvoices = scoped.Where(d => d.Kind == "INV").ToList();
        var recon = new List<Gstr1ReconRowDto>();
        var all = Totals(scopedInvoices, d => (d.RawTaxable, d.RawIgst, d.RawCgst, d.RawSgst));
        recon.Add(Recon("ERP sales lines — all vouchers in scope", all));
        var excluded = Totals(scopedInvoices.Where(d => d.Section == SecExcluded), d => (d.RawTaxable, d.RawIgst, d.RawCgst, d.RawSgst));
        recon.Add(Recon("Less: same-GSTIN branch transfers (not reported)", Neg(excluded)));
        var unapprovedOut = includeUnapproved
            ? default
            : Totals(scopedInvoices.Where(d => !d.Approved && d.Section != SecExcluded), d => (d.RawTaxable, d.RawIgst, d.RawCgst, d.RawSgst));
        if (!includeUnapproved)
            recon.Add(Recon("Less: unapproved vouchers (excluded by filter)", Neg(unapprovedOut)));
        var charges = Totals(invoices, d => (d.ChargesTaxable, d.ChargesIgst, d.ChargesCgst, d.ChargesSgst));
        recon.Add(Recon("Add: freight / packing charges taxed on the invoice", charges));
        var expected = Sub(Sub(Add(all, charges), excluded), unapprovedOut);
        recon.Add(Recon("Expected in return", expected));
        var returned = (
            b2b.Sum(r => r.Taxable) + b2cl.Sum(r => r.Taxable) + b2csInvoicesOnly.Sum(r => r.Taxable) + exp.Sum(r => r.Taxable) + nilRows.Sum(r => r.NilRated + r.Exempted + r.NonGst),
            b2b.Sum(r => r.Igst) + b2cl.Sum(r => r.Igst) + b2csInvoicesOnly.Sum(r => r.Igst) + exp.Sum(r => r.Igst),
            b2b.Sum(r => r.Cgst) + b2csInvoicesOnly.Sum(r => r.Cgst),
            b2b.Sum(r => r.Sgst) + b2csInvoicesOnly.Sum(r => r.Sgst));
        recon.Add(Recon("Reported in invoice sections (B2B + B2CL + B2CS + EXP + Nil)", returned));
        var diff = Sub(returned, expected);
        recon.Add(Recon("Difference", diff));
        var reconDifference = R(Math.Abs(diff.Taxable) + Math.Abs(diff.Igst) + Math.Abs(diff.Cgst) + Math.Abs(diff.Sgst));

        var gstins = scope.Companies
            .Select(c => data.Companies.TryGetValue(c, out var info) ? info.Gstin : "")
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        return new Gstr1ReportDto(
            data.From, data.To, DateTime.UtcNow, includeUnapproved, scope.Key, scope.Label, gstins, scope.Companies, scopes,
            summary, recon, reconDifference, b2b, b2cl, b2cs, exp, cdnr, cdnur, nilRows, hsnB2b, hsnB2c, docs,
            exceptions.OrderBy(e => SeverityOrder(e.Severity)).ThenBy(e => e.Category).ThenBy(e => e.Company)
                .ThenBy(e => e.DocumentDate).ThenBy(e => e.DocumentNo, StringComparer.OrdinalIgnoreCase).ToList(),
            HsnSummary: hsnSummary,
            SalesRegister: salesRegister,
            TrialBalance: tbRows,
            LedgerRecon: ledgerRecon);
    }

    private const string NoLedger = "(no sales ledger)";

    /// <summary>
    /// Invoice-line register of every invoice in scope (whether or not it is reported), raw line values before
    /// taxable charges are folded in. Gross amount (bill amount), TCS and other charges sit on the first line only,
    /// so column sums equal invoice totals.
    /// </summary>
    private static List<Gstr1SalesRegisterRowDto> BuildSalesRegister(List<Doc> scoped, List<Doc> hsnDocs, bool includeUnapproved)
    {
        var inHsn = new HashSet<Doc>(hsnDocs);
        var rows = new List<Gstr1SalesRegisterRowDto>();
        foreach (var d in Ordered(scoped.Where(d => d.Kind == "INV")))
        {
            var status = inHsn.Contains(d) ? "In GSTR-1"
                : !d.Approved && !includeUnapproved ? "Unapproved (excluded)"
                : d.Section switch
                {
                    SecExcluded => d.SubType.Length > 0 ? $"Excluded: {d.SubType}" : "Excluded",
                    SecNotReported => d.SubType.Length > 0 ? $"Not reported: {d.SubType}" : "Not reported",
                    SecNonGst => "Non-GST",
                    _ => d.Section,
                };
            var ledger = d.SalesLedger.Length > 0 ? d.SalesLedger : NoLedger;
            var lines = d.RawLines.Count > 0 ? d.RawLines : [new TaxLine()];
            var first = true;
            foreach (var l in lines)
            {
                rows.Add(new Gstr1SalesRegisterRowDto(d.Company, ledger, d.ErpType, d.PrintedNo, d.ErpNo, d.Date, d.Party, d.PartyGstin,
                    PosLabel(d.PosCode), l.Hsn, l.Commodity.Length > 0 ? l.Commodity : l.Description,
                    l.Uqc == "NA" ? 0 : Math.Round(l.Qty, 3), l.Uqc, l.Rate, R(l.Taxable), R(l.Igst), R(l.Cgst), R(l.Sgst),
                    first ? R(d.Tcs) : 0m, first ? R(d.OtherCharges) : 0m, first ? R(d.DocValue) : 0m, status));
                first = false;
            }
        }
        return rows;
    }

    /// <summary>Sales register vs HSN (same docs and netting as the HSN summary) vs trial balance, by sales ledger.</summary>
    private static List<Gstr1LedgerReconRowDto> BuildLedgerRecon(List<Gstr1SalesRegisterRowDto> register, List<Doc> hsnDocs,
        List<Gstr1TrialBalanceRowDto> tb, IEnumerable<string> noteLedgers)
    {
        var salesLedgers = new HashSet<string>(register.Select(r => r.SalesLedger).Concat(hsnDocs.Select(d => d.SalesLedger)).Concat(noteLedgers),
            StringComparer.OrdinalIgnoreCase);
        tb = tb.Where(t => !t.IsAdjustment && (salesLedgers.Contains(t.Ledger) || t.Under.StartsWith("Sales", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var acc = new Dictionary<string, LedgerAcc>(StringComparer.OrdinalIgnoreCase);
        LedgerAcc Get(string ledger)
        {
            var k = ledger.Length > 0 ? ledger : NoLedger;
            if (!acc.TryGetValue(k, out var a)) acc[k] = a = new LedgerAcc { Ledger = k };
            return a;
        }

        foreach (var r in register)
        {
            var a = Get(r.SalesLedger);
            a.RegisterGross += r.GrossAmount;
            a.RegisterValue += r.Value;
            if (r.Gstr1Status != "In GSTR-1")
            {
                a.NotInReturn += r.Value;
                a.NotInReturnGross += r.GrossAmount;
            }
        }
        foreach (var d in hsnDocs)
        {
            var a = Get(d.SalesLedger);
            var taxable = d.Sign * d.Lines.Sum(l => l.Taxable);
            var lineTax = d.Lines.Sum(l => l.Igst + l.Cgst + l.Sgst);
            var value = d.Sign * (d.Lines.Sum(l => l.Taxable) + lineTax);
            a.HsnTaxable += taxable;
            a.HsnValue += value;
            if (d.Kind == "INV")
            {
                a.Charges += d.ChargesTaxable;
                a.BillTcs += d.Tcs;
                a.BillTds += d.BillTds;
                a.BillIgstRefund += d.BillIgstRefund;
                a.BillDiscount += d.BillDiscount;
                a.BillRoundOff += d.BillRoundOff;
                a.BillCharges += d.BillCharges - d.ChargesTaxable;
                a.BillGstDiff += d.BillGst - lineTax;
            }
            else
            {
                a.Notes += taxable;
                a.NotesValue += value;
            }
        }
        foreach (var t in tb)
        {
            var a = Get(t.Ledger);
            a.Tb += t.Net;
            a.HasTb = true;
            if (a.Under.Length == 0) a.Under = t.Under;
        }

        return acc.Values
            .Where(a => a.RegisterGross != 0 || a.RegisterValue != 0 || a.HsnTaxable != 0 || a.HsnValue != 0 || a.Tb != 0)
            .OrderBy(a => a.Ledger, StringComparer.OrdinalIgnoreCase)
            .Select(a =>
            {
                var diffHsn = R(a.RegisterValue - a.HsnTaxable);
                var diffTb = R(a.RegisterValue - a.Tb);
                var notes = new List<string>();
                if (Math.Abs(a.NotInReturn) >= 0.01m) notes.Add($"invoices not in GSTR-1 {Money(a.NotInReturn)}");
                if (Math.Abs(a.Charges) >= 0.01m) notes.Add($"taxable freight/packing added to HSN {Money(a.Charges)}");
                if (Math.Abs(a.Notes) >= 0.01m) notes.Add($"credit/debit notes in HSN {Money(a.Notes)}");
                var unexplained = diffHsn - R(a.NotInReturn - a.Charges - a.Notes);
                if (Math.Abs(unexplained) > 1m) notes.Add($"other register vs HSN {Money(unexplained)}");
                var tbExtra = -diffTb;
                if (!a.HasTb && Math.Abs(diffTb) > 1m)
                    notes.Add("no postings to this ledger in the trial balance");
                else if (Math.Abs(a.Notes) >= 0.01m && Math.Abs(tbExtra - a.Notes) <= 1m)
                    notes.Add("trial balance carries the same credit/debit notes");
                else if (Math.Abs(tbExtra) > 1m)
                    notes.Add($"ledger postings other than sales invoices {Money(tbExtra)} (credit notes / journals posted to this ledger)");
                var diffInv = R(a.RegisterGross - a.HsnValue);
                return new Gstr1LedgerReconRowDto(a.Ledger, a.Under, R(a.RegisterGross), R(a.RegisterValue), R(a.HsnValue), R(a.HsnTaxable),
                    R(a.Tb), diffHsn, diffTb, string.Join("; ", notes), diffInv, InvoiceValueRemarks(a, diffInv));
            })
            .ToList();
    }

    /// <summary>Splits Sales Reg. GrossAmount − HSN invoice value into its causes (amounts signed as they move the difference).</summary>
    private static string InvoiceValueRemarks(LedgerAcc a, decimal diffInv)
    {
        if (Math.Abs(diffInv) <= 1m) return "";
        var parts = new List<(string Label, decimal Amount)>
        {
            ("invoices not in GSTR-1", a.NotInReturnGross),
            ("IGST paid on exports (refund under rebate) — not billed to the buyer", a.BillIgstRefund),
            ("TDS deducted in the bill amount", a.BillTds),
            ("discount / rate difference in the bill", a.BillDiscount),
            ("freight / C&F / packing / insurance billed but not in taxable value", a.BillCharges),
            ("TCS collected", a.BillTcs),
            ("round off", a.BillRoundOff),
            ("GST ledger vs invoice line tax", a.BillGstDiff),
            ("credit/debit notes in HSN", -a.NotesValue),
        };
        var known = parts.Sum(p => p.Amount);
        parts.Add(("other (bill amount vs ERP postings)", diffInv - known));
        return string.Join("; ", parts.Where(p => Math.Abs(p.Amount) > 1m)
            .OrderByDescending(p => Math.Abs(p.Amount))
            .Select(p => $"{p.Label} {Money(p.Amount)}"));
    }

    private sealed class LedgerAcc
    {
        public string Ledger = "", Under = "";
        public decimal RegisterGross, RegisterValue, HsnValue, HsnTaxable, Tb, NotInReturn, Charges, Notes;
        public decimal NotInReturnGross, NotesValue, BillTcs, BillTds, BillIgstRefund, BillDiscount, BillRoundOff, BillCharges, BillGstDiff;
        public bool HasTb;
    }

    private static List<Gstr1ScopeDto> BuildScopes(IEnumerable<string> activeCompanies, IReadOnlyDictionary<string, CompanyInfo> companies)
    {
        var list = activeCompanies.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        var scopes = new List<Gstr1ScopeDto> { new("", "All companies", "", list, true) };
        var groups = list
            .GroupBy(c => companies.TryGetValue(c, out var i) && IsValidGstin(i.Gstin) ? i.Gstin : "company:" + c,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).First(), StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            var members = g.OrderBy(c => c.Length).ThenBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            var gstin = g.Key.StartsWith("company:", StringComparison.Ordinal) ? "" : g.Key;
            if (members.Count > 1 && gstin.Length > 0)
                scopes.Add(new Gstr1ScopeDto($"gstin:{gstin}", $"{members[0]} — all units ({members.Count})", gstin, members, true));
            foreach (var c in members.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                scopes.Add(new Gstr1ScopeDto($"company:{c}", c, gstin, [c], false));
        }
        return scopes;
    }

    private static IEnumerable<Doc> Ordered(IEnumerable<Doc> docs) =>
        docs.OrderBy(d => d.Company, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Date).ThenBy(d => d.PrintedNo, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<RateGroup> ByRate(IEnumerable<TaxLine> lines) =>
        lines.GroupBy(l => l.Rate).OrderBy(g => g.Key).Select(g => new RateGroup(
            g.Key, g.Sum(l => l.Taxable), g.Sum(l => l.Igst), g.Sum(l => l.Cgst), g.Sum(l => l.Sgst)));

    private static List<Gstr1B2csRowDto> AggregateB2cs(IEnumerable<Doc> docs)
    {
        var map = new Dictionary<(string Pos, decimal Rate), (decimal T, decimal I, decimal C, decimal S, HashSet<string> Docs)>();
        foreach (var d in docs)
        {
            var sign = d.Sign;
            foreach (var l in d.Lines.Where(l => l.Rate > 0))
            {
                var key = (d.PosCode, l.Rate);
                if (!map.TryGetValue(key, out var cur))
                    cur = (0, 0, 0, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                cur.Docs.Add($"{d.Company}|{d.Kind}|{d.ErpNo}");
                map[key] = (cur.T + sign * l.Taxable, cur.I + sign * l.Igst, cur.C + sign * l.Cgst, cur.S + sign * l.Sgst, cur.Docs);
            }
        }
        return map.OrderBy(kv => kv.Key.Pos).ThenBy(kv => kv.Key.Rate)
            .Select(kv => new Gstr1B2csRowDto("OE", PosLabel(kv.Key.Pos), kv.Key.Rate, R(kv.Value.T), R(kv.Value.I),
                R(kv.Value.C), R(kv.Value.S), 0m, kv.Value.Docs.Count))
            .ToList();
    }

    private static List<Gstr1NoteRowDto> NoteRows(IEnumerable<Doc> docs)
    {
        var rows = new List<Gstr1NoteRowDto>();
        foreach (var d in Ordered(docs))
            foreach (var g in ByRate(d.Lines))
                rows.Add(new Gstr1NoteRowDto(d.Company, d.PartyGstin, d.Party, d.PrintedNo, d.ErpNo, d.Date,
                    d.Kind == "CN" ? "C" : "D", PosLabel(d.PosCode), "N", d.SubType, R(d.DocValue), g.Rate,
                    R(g.Taxable), R(g.Igst), R(g.Cgst), R(g.Sgst), 0m, d.OrigPrintedNo, d.OrigDate, d.ErpType, d.Approved));
        return rows;
    }

    private static List<Gstr1HsnRowDto> AggregateHsn(IEnumerable<Doc> docs)
    {
        var map = new Dictionary<(string Hsn, decimal Rate, string Uqc), HsnAcc>();
        foreach (var d in docs)
        {
            var sign = d.Sign;
            foreach (var l in d.Lines)
            {
                var key = (l.Hsn, l.Rate, l.Uqc);
                if (!map.TryGetValue(key, out var acc))
                    map[key] = acc = new HsnAcc();
                acc.Qty += sign * l.Qty;
                acc.Taxable += sign * l.Taxable;
                acc.Igst += sign * l.Igst;
                acc.Cgst += sign * l.Cgst;
                acc.Sgst += sign * l.Sgst;
                var name = l.Commodity.Length > 0 ? l.Commodity : l.Description;
                if (name.Length > 0)
                    acc.Commodities[name] = acc.Commodities.GetValueOrDefault(name) + Math.Abs(l.Taxable);
            }
        }
        return map.OrderBy(kv => kv.Key.Hsn, StringComparer.Ordinal).ThenBy(kv => kv.Key.Rate)
            .Select(kv =>
            {
                var names = kv.Value.Commodities.OrderByDescending(c => c.Value).Select(c => c.Key).ToList();
                return new Gstr1HsnRowDto(kv.Key.Hsn, HsnDescriptions.For(kv.Key.Hsn, names.FirstOrDefault() ?? ""), kv.Key.Uqc,
                    kv.Key.Uqc == "NA" ? 0 : Math.Round(kv.Value.Qty, 3),
                    R(kv.Value.Taxable + kv.Value.Igst + kv.Value.Cgst + kv.Value.Sgst), kv.Key.Rate,
                    R(kv.Value.Taxable), R(kv.Value.Igst), R(kv.Value.Cgst), R(kv.Value.Sgst), 0m,
                    string.Join(" / ", names));
            })
            .ToList();
    }

    private const string NilInterReg = "Inter-State supplies to registered persons";
    private const string NilIntraReg = "Intra-State supplies to registered persons";
    private const string NilInterUnreg = "Inter-State supplies to unregistered persons";
    private const string NilIntraUnreg = "Intra-State supplies to unregistered persons";

    private static string NilBucket(Doc d) => (d.Inter, d.PartyGstin.Length > 0) switch
    {
        (true, true) => NilInterReg,
        (false, true) => NilIntraReg,
        (true, false) => NilInterUnreg,
        _ => NilIntraUnreg,
    };

    private static Gstr1SectionSummaryDto SectionSummary(string section, string label, int count, decimal value,
        decimal taxable, decimal igst, decimal cgst, decimal sgst) =>
        new(section, label, count, R(value), R(taxable), R(igst), R(cgst), R(sgst), 0m);

    private static Gstr1SectionSummaryDto NoteSummary(string section, string label, List<Gstr1NoteRowDto> rows) =>
        SectionSummary(section, label, rows.Select(r => (r.Company, r.ErpNoteNo)).Distinct().Count(),
            rows.GroupBy(r => (r.Company, r.ErpNoteNo)).Sum(g => g.First().NoteValue),
            rows.Sum(r => r.Taxable), rows.Sum(r => r.Igst), rows.Sum(r => r.Cgst), rows.Sum(r => r.Sgst));

    private static (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) Totals(
        IEnumerable<Doc> docs, Func<Doc, (decimal, decimal, decimal, decimal)> pick)
    {
        decimal t = 0, i = 0, c = 0, s = 0;
        foreach (var d in docs)
        {
            var v = pick(d);
            t += v.Item1; i += v.Item2; c += v.Item3; s += v.Item4;
        }
        return (t, i, c, s);
    }

    private static (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) Add(
        (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) a, (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) b) =>
        (a.Taxable + b.Taxable, a.Igst + b.Igst, a.Cgst + b.Cgst, a.Sgst + b.Sgst);

    private static (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) Sub(
        (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) a, (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) b) =>
        (a.Taxable - b.Taxable, a.Igst - b.Igst, a.Cgst - b.Cgst, a.Sgst - b.Sgst);

    private static (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) Neg((decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) a) =>
        (-a.Taxable, -a.Igst, -a.Cgst, -a.Sgst);

    private static Gstr1ReconRowDto Recon(string label, (decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst) v) =>
        new(label, R(v.Taxable), R(v.Igst), R(v.Cgst), R(v.Sgst));

    private static int SeverityOrder(string s) => s switch { "error" => 0, "warning" => 1, _ => 2 };

    // ---------------------------------------------------------------- shared helpers

    internal static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static decimal D(double v) => double.IsFinite(v) ? (decimal)v : 0m;

    internal static string PosLabel(string code) =>
        code.Length == 0 ? "" : PortalStates.TryGetValue(code, out var name) ? $"{code}-{name}" : code;

    private static string NormalizeGstin(string? raw) =>
        new string((raw ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static bool IsValidGstin(string g)
    {
        if (g.Length != 15 || !GstinPattern.IsMatch(g)) return false;
        var code = g[..2];
        if (!PortalStates.ContainsKey(code) && code != "25") return false;
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sum = 0;
        for (var i = 0; i < 14; i++)
        {
            var p = chars.IndexOf(g[i]) * (i % 2 == 0 ? 1 : 2);
            sum += p / 36 + p % 36;
        }
        return chars[(36 - sum % 36) % 36] == g[14];
    }

    private static bool IsUnregisteredPlaceholder(string raw)
    {
        var n = NormalizeGstin(raw);
        return n.Length == 0 || n is "NA" or "N" or "0" or "NIL" or "URD" || n.Contains("UNREG") || n.Contains("REGISTER");
    }

    private static decimal SnapRate(decimal rate)
    {
        var best = StandardRates.MinBy(s => Math.Abs(s - rate));
        return Math.Abs(best - rate) <= 0.3m ? best : Math.Round(rate, 2);
    }

    private static string StripFinancialYear(string no) => TrailingFinancialYear.Replace(no, "");

    /// <summary>Candidate SalesVoucher numbers for a note's reference (raw, without FY, bare serial of a printed /D/ number).</summary>
    private static IEnumerable<string> ReferenceVariants(string reference)
    {
        var r = reference.Trim();
        if (r.Length == 0) yield break;
        yield return r;
        var noFy = StripFinancialYear(r);
        if (noFy != r) yield return noFy;
        var m = DomesticPrinted.Match(noFy);
        if (m.Success) yield return m.Groups["n"].Value;
    }

    private static string Uqc(string per, string hsn)
    {
        if (hsn.StartsWith("99", StringComparison.Ordinal)) return "NA";
        var p = new string(per.ToUpperInvariant().Where(char.IsLetter).ToArray());
        return p switch
        {
            "KG" or "KGS" or "KILOGRAM" or "KILOGRAMS" => "KGS-KILOGRAMS",
            "PC" or "PCS" or "PIECE" or "PIECES" => "PCS-PIECES",
            "NO" or "NOS" or "NUMBER" or "NUMBERS" => "NOS-NUMBERS",
            "MT" or "MTS" or "TON" or "TONS" or "TONNE" or "TONNES" => "MTS-METRIC TON",
            "MTR" or "MTRS" or "METER" or "METERS" or "METRE" or "METRES" => "MTR-METERS",
            "BAG" or "BAGS" => "BAG-BAGS",
            "ROL" or "ROLL" or "ROLLS" => "ROL-ROLLS",
            "SET" or "SETS" => "SET-SETS",
            _ => "OTH-OTHERS",
        };
    }

    // ---------------------------------------------------------------- base computation

    private sealed class Builder
    {
        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly Dictionary<string, CompanyInfo> _companies = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _stateCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly IReadOnlyDictionary<string, string> _domesticPrefixes;
        private readonly Dictionary<string, string> _plainPrefixes = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<InvoiceRow> _invoices;
        private readonly ILookup<string, LineRow> _linesByInvoice;
        private readonly ILookup<string, LedgerRow> _invoiceLedger;
        private readonly ILookup<string, ExportRow> _exportInfo;
        private readonly List<NoteRow> _creditNotes;
        private readonly ILookup<string, LineRow> _creditItems;
        private readonly ILookup<string, LedgerRow> _creditLedger;
        private readonly List<NoteRow> _debitNotes;
        private readonly ILookup<string, LedgerRow> _debitLedger;
        private readonly ILookup<string, InvoiceRow> _originals;
        private readonly ILookup<string, LineRow> _originalLines;
        private readonly Dictionary<string, OriginalInvoice> _originalCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Doc> _docs = [];
        private readonly List<Gstr1ExceptionDto> _exceptions = [];

        public Builder(DateTime from, DateTime to, IEnumerable<CompanyRow> companies, IEnumerable<(string Name, string Code)> states,
            IReadOnlyDictionary<string, string> domesticPrefixes, List<InvoiceRow> invoices, List<LineRow> lines,
            List<LedgerRow> invoiceLedger, List<ExportRow> exportInfo, List<NoteRow> creditNotes, List<LineRow> creditItems,
            List<LedgerRow> creditLedger, List<NoteRow> debitNotes, List<LedgerRow> debitLedger,
            List<InvoiceRow> originals, List<LineRow> originalLines)
        {
            _from = from;
            _to = to;
            foreach (var kv in PortalStates)
                _stateCodes[StateKey(kv.Value)] = kv.Key;
            foreach (var (name, code) in states)
            {
                var c = (code ?? "").PadLeft(2, '0');
                if (c == "25") c = "26";
                if (!string.IsNullOrEmpty(name) && PortalStates.ContainsKey(c))
                    _stateCodes.TryAdd(StateKey(name), c);
            }
            foreach (var (alias, code) in new[]
                     {
                         ("Orissa", "21"), ("Daman and Diu", "26"), ("Daman & Diu", "26"), ("Dadra and Nagar Haveli", "26"),
                         ("Pondicherry", "34"), ("Uttaranchal", "05"), ("Chattisgarh", "22"), ("New Delhi", "07"),
                         ("J&K", "01"), ("Jammu & Kashmir", "01"), ("Andaman & Nicobar", "35"), ("Telengana", "36"),
                     })
                _stateCodes.TryAdd(StateKey(alias), code);

            foreach (var c in companies)
            {
                var gstin = NormalizeGstin(c.Gstin);
                var state = IsValidGstin(gstin) ? gstin[..2]
                    : _stateCodes.TryGetValue(StateKey(c.State), out var sc) ? sc : "24";
                _companies[c.Company] = new CompanyInfo(c.Company, IsValidGstin(gstin) ? gstin : "", state, c.Code);
            }

            _domesticPrefixes = domesticPrefixes;
            _invoices = invoices;
            _linesByInvoice = lines.ToLookup(l => LineKey(l.Company, l.InvNo, l.InvYear), StringComparer.OrdinalIgnoreCase);
            _invoiceLedger = invoiceLedger.ToLookup(t => DocKey(t.Company, t.DocNo, t.DocDate.GetValueOrDefault()), StringComparer.OrdinalIgnoreCase);
            _exportInfo = exportInfo.ToLookup(e => NoteKey(e.Company, e.InvNo), StringComparer.OrdinalIgnoreCase);
            _creditNotes = creditNotes;
            _creditItems = creditItems.ToLookup(i => NoteKey(i.Company, i.InvNo), StringComparer.OrdinalIgnoreCase);
            _creditLedger = creditLedger.ToLookup(t => NoteKey(t.Company, t.DocNo), StringComparer.OrdinalIgnoreCase);
            _debitNotes = debitNotes;
            _debitLedger = debitLedger.ToLookup(t => NoteKey(t.Company, t.DocNo), StringComparer.OrdinalIgnoreCase);
            _originals = originals.ToLookup(o => NoteKey(o.Company, o.InvNo), StringComparer.OrdinalIgnoreCase);
            _originalLines = originalLines.ToLookup(l => LineKey(l.Company, l.InvNo, l.InvYear), StringComparer.OrdinalIgnoreCase);

            BuildPlainPrefixes();
        }

        public BaseData Build()
        {
            foreach (var inv in _invoices)
                ProcessInvoice(inv);
            foreach (var cn in _creditNotes)
                ProcessNote(cn, "CN");
            foreach (var dn in _debitNotes)
                ProcessNote(dn, "DN");
            return new BaseData(_from, _to, _companies, _docs, _exceptions);
        }

        // ------------------------------------------------ invoices

        private void ProcessInvoice(InvoiceRow inv)
        {
            var own = Company(inv.Company);
            var ex = inv.ExchangeRate > 0 ? D(inv.ExchangeRate) : 1m;
            var lines = _linesByInvoice[LineKey(inv.Company, inv.InvNo, inv.InvYear)].ToList();
            var taxLines = lines.Select(l => ToTaxLine(l, ex)).ToList();
            var printed = Printed(inv.Company, inv.InvNo);
            var export = FindExport(inv);
            var igst = taxLines.Sum(l => l.Igst);
            var cgst = taxLines.Sum(l => l.Cgst) + taxLines.Sum(l => l.Sgst);

            var doc = new Doc
            {
                Company = inv.Company,
                OwnGstin = own.Gstin,
                Kind = "INV",
                ErpNo = inv.InvNo,
                PrintedNo = printed,
                Date = inv.InvDate.Date,
                ErpType = inv.VoucherType,
                Party = inv.PartyName,
                DocValue = D(inv.BillAmount),
                Approved = inv.ApprovalStatus != 0,
                Lines = taxLines,
                RawLines = taxLines.Select(l => l.Clone()).ToList(),
                RawTaxable = taxLines.Sum(l => l.Taxable),
                RawIgst = igst,
                RawCgst = taxLines.Sum(l => l.Cgst),
                RawSgst = taxLines.Sum(l => l.Sgst),
                SalesLedger = inv.SalesLedger,
            };
            foreach (var t in _invoiceLedger[DocKey(inv.Company, inv.InvNo, inv.InvDate)])
            {
                if (t.Ledger.Contains("TCS", StringComparison.OrdinalIgnoreCase))
                    doc.Tcs += D(t.Amount);
                else if (t.Ledger.Contains("freight", StringComparison.OrdinalIgnoreCase)
                         || t.Ledger.Contains("insurance", StringComparison.OrdinalIgnoreCase))
                    doc.OtherCharges += D(t.Amount);
                AddBillComponent(doc, t);
            }
            var cls = Classify(inv, own, igst, cgst, export, doc, emit: true);
            doc.Section = cls.Section;
            doc.SubType = cls.SubType;
            doc.PartyGstin = cls.PartyGstin;
            doc.PosCode = cls.PosCode;
            doc.Inter = cls.Inter;
            _docs.Add(doc);
            if (doc.Section == SecExcluded) return;

            if (!doc.Approved)
                Issue("unapproved", doc, $"ApprovalStatus = 0 ({inv.VoucherType})", doc.DocValue);

            if (lines.Count == 0)
                Issue("tax-mismatch", doc, "Invoice has no item lines in SalesVoucherItem", doc.DocValue);

            ReconcileLedger(doc, inv);
            CheckHsn(doc, lines);

            if (doc.Section == SecExp)
            {
                doc.PortCode = CleanPort(export?.PortCode);
                doc.ShippingBillNo = CleanShippingBill(export?.ShippingBillNo is { Length: > 0 } sb ? sb : inv.ShippingBillNo);
                doc.ShippingBillDate = export?.ShippingBillDate ?? inv.ShippingBillDate;
                var missing = new List<string>();
                if (doc.ShippingBillNo.Length == 0) missing.Add("shipping bill no");
                if (doc.ShippingBillDate is null) missing.Add("shipping bill date");
                if (doc.PortCode.Length == 0) missing.Add("port code");
                if (missing.Count > 0)
                    Issue("export-docs", doc, $"Missing {string.Join(", ", missing)}" + (export is null ? " (no Despatch packing list / export invoice found)" : ""), doc.DocValue);
            }
        }

        private Classification Classify(InvoiceRow inv, CompanyInfo own, decimal igst, decimal cgstSgst, ExportRow? export, Doc? doc, bool emit)
        {
            var gstin = NormalizeGstin(inv.PartyGstin);
            var valid = IsValidGstin(gstin);
            var vt = inv.VoucherType;
            var c = new Classification { PartyGstin = valid ? gstin : "" };

            if (valid && own.Gstin.Length > 0 && gstin == own.Gstin)
            {
                c.Section = SecExcluded;
                c.SubType = "Same GSTIN transfer";
                c.PosCode = own.StateCode;
                return c;
            }

            var exportVoucher = ExportVoucherTypes.Contains(vt);
            var deemed = vt.Equals("Deemed Export", StringComparison.OrdinalIgnoreCase);
            var highSeas = vt.Contains("high", StringComparison.OrdinalIgnoreCase) && vt.Contains("sea", StringComparison.OrdinalIgnoreCase);
            var sez = valid && IsSezParty(gstin, inv.PartyName, inv.PartyLedger, inv.PartyUnder, inv.SalesLedger);

            if (emit && doc is not null && !valid && !exportVoucher)
            {
                var placeholder = IsUnregisteredPlaceholder(inv.PartyGstin);
                if (!placeholder)
                    Issue("gstin", doc, $"Buyer GSTIN '{inv.PartyGstin}' is not a valid GSTIN — treated as unregistered", D(inv.BillAmount));
                else if (RegisteredOnlyVoucherTypes.Contains(vt))
                    Issue("gstin", doc, $"No GSTIN on buyer ledger for a {vt} voucher — treated as unregistered", D(inv.BillAmount));
            }

            if (highSeas)
            {
                c.Section = SecNonGst;
                c.SubType = "High-seas sale";
                c.PosCode = valid ? gstin[..2] : ResolveState(inv.PartyState, inv.Destination) ?? own.StateCode;
                c.Inter = c.PosCode != own.StateCode;
                return c;
            }

            if (valid && deemed)
            {
                c.Section = SecB2b;
                c.SubType = TypeDeemed;
                c.PosCode = gstin[..2];
                c.Inter = igst > 0 || (cgstSgst == 0 && c.PosCode != own.StateCode);
                CheckTaxHead(c, igst, cgstSgst, doc, emit);
                return c;
            }

            if (sez)
            {
                c.Section = SecB2b;
                c.SubType = igst > 0 ? TypeSezWp : TypeSezWop;
                c.PosCode = gstin[..2];
                c.Inter = true;
                if (emit && doc is not null && cgstSgst > 0)
                    Issue("tax-head", doc, "SEZ supply charged CGST/SGST — SEZ supplies are inter-state (IGST)", cgstSgst);
                return c;
            }

            if (exportVoucher && !valid)
            {
                var rebate = export?.BeingExport.Contains("rebate", StringComparison.OrdinalIgnoreCase) == true;
                var lut = export?.BeingExport.Contains("LUT", StringComparison.OrdinalIgnoreCase) == true
                          || export?.BeingExport.Contains("Bond", StringComparison.OrdinalIgnoreCase) == true;
                c.Section = SecExp;
                c.SubType = igst > 0 || rebate ? "WPAY" : "WOPAY";
                c.PosCode = "96";
                c.Inter = true;
                if (emit && doc is not null)
                {
                    if (rebate && igst == 0)
                        Issue("classification", doc, $"Despatch marks the export as '{export!.BeingExport}' but the invoice lines carry no IGST — reported as WPAY with zero tax", D(inv.BillAmount));
                    else if (lut && igst > 0)
                        Issue("classification", doc, $"Despatch marks the export as '{export!.BeingExport}' but the invoice lines carry IGST — reported as WPAY", igst);
                    if (cgstSgst > 0)
                        Issue("tax-head", doc, "Export invoice charged CGST/SGST", cgstSgst);
                }
                return c;
            }

            if (valid)
            {
                c.Section = SecB2b;
                c.SubType = TypeRegular;
                c.PosCode = gstin[..2];
                c.Inter = igst > 0 || (cgstSgst == 0 && c.PosCode != own.StateCode);
                if (emit && doc is not null && exportVoucher)
                    Issue("classification", doc, $"'{vt}' voucher to registered buyer {gstin} with no SEZ marker — reported as Regular B2B. Mark the buyer as SEZ if applicable.", D(inv.BillAmount));
                if (igst > 0 && cgstSgst == 0 && c.PosCode == own.StateCode)
                {
                    c.SubType = TypeIntraIgst;
                    c.Inter = true;
                    if (emit && doc is not null)
                        Issue("intra-igst", doc, $"Intra-state supply ({PosLabel(c.PosCode)}) charged IGST — reported as '{TypeIntraIgst}' (expected for supplies by an SEZ unit; otherwise check the tax head)", igst);
                }
                else
                    CheckTaxHead(c, igst, cgstSgst, doc, emit);
                return c;
            }

            var pos = ResolveState(inv.PartyState, inv.Destination);
            if (pos is null)
            {
                pos = igst > 0 ? "" : own.StateCode;
                if (emit && doc is not null)
                    Issue("pos", doc, $"Unregistered buyer has no recognisable state (ledger state '{inv.PartyState}', destination '{inv.Destination}')" + (igst > 0 ? " — IGST charged, place of supply left blank" : " — assumed intra-state"), D(inv.BillAmount));
            }
            c.PosCode = pos;
            c.Inter = pos.Length == 0 ? igst > 0 : pos != own.StateCode;
            c.Section = c.Inter && D(inv.BillAmount) > B2clThreshold ? SecB2cl : SecB2cs;
            c.SubType = c.Section == SecB2cs ? "OE" : "";
            if (pos.Length > 0) CheckTaxHead(c, igst, cgstSgst, doc, emit);
            return c;
        }

        private void CheckTaxHead(Classification c, decimal igst, decimal cgstSgst, Doc? doc, bool emit)
        {
            if (!emit || doc is null || c.PosCode.Length == 0) return;
            var interPos = c.PosCode != Company(doc.Company).StateCode;
            if (interPos && cgstSgst > 0)
                Issue("tax-head", doc, $"Place of supply {PosLabel(c.PosCode)} is inter-state but CGST/SGST was charged", cgstSgst);
            else if (!interPos && igst > 0)
                Issue("tax-head", doc, $"Place of supply {PosLabel(c.PosCode)} is intra-state but IGST was charged", igst);
        }

        /// <summary>
        /// Cross-checks line tax against the GST Output ledger rows. When the difference is exactly the tax on
        /// freight / packing charges posted on the invoice, the charges are added to the highest-rate line.
        /// </summary>
        private void ReconcileLedger(Doc doc, InvoiceRow inv)
        {
            var rows = _invoiceLedger[DocKey(inv.Company, inv.InvNo, inv.InvDate)].ToList();
            var heads = HeadTotals(rows);
            var lineI = doc.Lines.Sum(l => l.Igst);
            var lineC = doc.Lines.Sum(l => l.Cgst);
            var lineS = doc.Lines.Sum(l => l.Sgst);
            var dI = heads.Igst - lineI;
            var dC = heads.Cgst - lineC;
            var dS = heads.Sgst - lineS;
            var delta = dI + dC + dS;
            if (Math.Abs(dI) <= 1 && Math.Abs(dC) <= 1 && Math.Abs(dS) <= 1) return;

            var charges = rows.Where(r => IsChargeLedger(r.Ledger)).ToList();
            var chargeTotal = charges.Sum(r => D(r.Amount));
            var top = doc.Lines.Where(l => l.Rate > 0).OrderByDescending(l => l.Rate).ThenByDescending(l => l.Taxable).FirstOrDefault();
            if (top is not null && chargeTotal > 0 && delta > 0
                && Math.Abs(delta - chargeTotal * top.Rate / 100m) <= Math.Max(1m, Math.Abs(delta) * 0.01m))
            {
                top.Taxable += chargeTotal;
                top.Igst += dI;
                top.Cgst += dC;
                top.Sgst += dS;
                doc.ChargesTaxable = chargeTotal;
                doc.ChargesIgst = dI;
                doc.ChargesCgst = dC;
                doc.ChargesSgst = dS;
                Issue("charges", doc,
                    $"{string.Join(", ", charges.Select(r => r.Ledger).Distinct())} of {chargeTotal:N2} taxed in the ledger at {top.Rate}% — added to taxable value",
                    chargeTotal);
                return;
            }

            Issue("tax-mismatch", doc,
                $"Lines: IGST {lineI:N2}, CGST {lineC:N2}, SGST {lineS:N2} · Ledger: IGST {heads.Igst:N2}, CGST {heads.Cgst:N2}, SGST {heads.Sgst:N2} — return uses line values",
                delta);
        }

        private void CheckHsn(Doc doc, List<LineRow> lines)
        {
            var missing = new List<string>();
            var malformed = new List<string>();
            var differs = new List<string>();
            foreach (var l in lines)
            {
                var name = l.Commodity.Length > 0 ? l.Commodity : l.ItemName;
                var hsn = CleanHsn(l.Hsn);
                var master = CleanHsn(l.MasterHsn);
                if (hsn.Length == 0)
                    missing.Add(master.Length > 0 ? $"{name} (master {master} used)" : name);
                else if (!HsnPattern.IsMatch(hsn))
                    malformed.Add($"{name}: '{l.Hsn}'");
                if (hsn.Length > 0 && master.Length > 0 && hsn != master)
                {
                    var prefix = hsn.StartsWith(master, StringComparison.Ordinal) || master.StartsWith(hsn, StringComparison.Ordinal);
                    differs.Add($"{name}: invoice {hsn} vs master {master}{(prefix ? " (same heading, different length)" : "")}");
                }
            }
            if (missing.Count > 0) Issue("hsn-missing", doc, string.Join("; ", missing.Distinct()), null);
            if (malformed.Count > 0) Issue("hsn-malformed", doc, string.Join("; ", malformed.Distinct()), null);
            if (differs.Count > 0) Issue("hsn-master", doc, string.Join("; ", differs.Distinct()), null);
        }

        // ------------------------------------------------ notes

        private void ProcessNote(NoteRow note, string kind)
        {
            var own = Company(note.Company);
            var gstin = NormalizeGstin(note.PartyGstin);
            var valid = IsValidGstin(gstin);
            var (exact, fallback) = ResolveOriginal(note);
            var orig = exact ?? fallback;
            var ledgerRows = (kind == "CN" ? _creditLedger : _debitLedger)[NoteKey(note.Company, note.NoteNo)].ToList();
            var heads = HeadTotals(ledgerRows);
            var items = kind == "CN" ? _creditItems[NoteKey(note.Company, note.NoteNo)].ToList() : new List<LineRow>();
            var pending = new List<(string Category, string Detail, decimal? Amount)>();

            var doc = new Doc
            {
                Company = note.Company,
                OwnGstin = own.Gstin,
                Kind = kind,
                ErpNo = note.NoteNo,
                PrintedNo = StripFinancialYear(note.NoteNo),
                Date = note.NoteDate.Date,
                ErpType = note.NoteKind,
                Party = note.PartyName,
                PartyGstin = valid ? gstin : "",
                DocValue = D(note.NoteValue),
                Approved = note.ApprovalStatus != 0,
                OrigNo = note.OrigNo,
                OrigDate = exact?.Invoice.InvDate.Date ?? note.OrigDate?.Date,
                OrigPrintedNo = exact is not null ? exact.PrintedNo : note.OrigNo,
                SalesLedger = note.Ledger.Length > 0 ? note.Ledger : orig?.Invoice.SalesLedger ?? "",
            };
            doc.Lines = NoteLines(note, items, ledgerRows, heads, orig, pending);
            var tax = doc.Lines.Sum(l => l.Igst + l.Cgst + l.Sgst);
            var origCls = orig?.Classification;
            var zeroRatedOrig = origCls is not null
                && (origCls.Section == SecExp || (origCls.Section == SecB2b && origCls.SubType is TypeSezWop or TypeDeemed));
            var label = kind == "CN" ? "Credit note" : "Debit note";

            if (valid && own.Gstin.Length > 0 && gstin == own.Gstin)
            {
                doc.Section = SecNotReported;
                doc.SubType = "Same GSTIN";
                _docs.Add(doc);
                return;
            }

            if (kind == "CN" && note.NoteKind.Equals("Provisional", StringComparison.OrdinalIgnoreCase))
            {
                doc.Section = SecNotReported;
                _docs.Add(doc);
                Issue("note-excluded", doc, $"Provisional credit note ({note.Ledger}) — not reported in CDNR" + (tax > 0 ? $"; carries GST {tax:N2}" : ""), doc.DocValue);
                return;
            }
            if (tax == 0 && !zeroRatedOrig)
            {
                doc.Section = SecNotReported;
                _docs.Add(doc);
                Issue("note-excluded", doc, $"{label} carries no GST ({note.NoteKind}{(note.Ledger.Length > 0 ? $", {note.Ledger}" : "")}) — not reported", doc.DocValue);
                return;
            }

            var igst = doc.Lines.Sum(l => l.Igst);
            var cgstSgst = doc.Lines.Sum(l => l.Cgst + l.Sgst);
            if (valid)
            {
                doc.Section = SecCdnr;
                doc.PosCode = gstin[..2];
                if (origCls?.Section == SecB2b)
                    doc.SubType = origCls.SubType;
                else if (IsSezParty(gstin, note.PartyName, note.PartyLedger, note.PartyUnder, note.Ledger))
                    doc.SubType = igst > 0 ? TypeSezWp : TypeSezWop;
                else
                    doc.SubType = TypeRegular;
                doc.Inter = doc.SubType is TypeSezWp or TypeSezWop || igst > 0 || (cgstSgst == 0 && doc.PosCode != own.StateCode);
            }
            else if (origCls?.Section == SecExp)
            {
                doc.Section = SecCdnur;
                doc.SubType = origCls.SubType == "WPAY" ? "EXPWP" : "EXPWOP";
                doc.PosCode = "96";
                doc.Inter = true;
            }
            else if (origCls?.Section == SecB2cl)
            {
                doc.Section = SecCdnur;
                doc.SubType = "B2CL";
                doc.PosCode = origCls.PosCode;
                doc.Inter = true;
            }
            else
            {
                doc.Section = SecB2cs;
                doc.SubType = "OE";
                var pos = origCls?.PosCode is { Length: > 0 } op ? op : ResolveState(note.PartyState, "");
                if (pos is null)
                {
                    pos = igst > 0 ? "" : own.StateCode;
                    Issue("pos", doc, $"Unregistered party has no recognisable state (ledger state '{note.PartyState}')", doc.DocValue);
                }
                doc.PosCode = pos;
                doc.Inter = pos.Length == 0 ? igst > 0 : pos != own.StateCode;
            }
            _docs.Add(doc);

            foreach (var p in pending)
                Issue(p.Category, doc, p.Detail, p.Amount);
            if (!doc.Approved)
                Issue("unapproved", doc, $"ApprovalStatus = 0 ({label.ToLowerInvariant()}, {note.NoteKind})", doc.DocValue);

            if (exact is null)
            {
                var refText = note.OrigNo.Length > 0
                    ? $"ERP reference '{note.OrigNo}'{(note.OrigDate is { } od ? $" dated {od:dd-MMM-yyyy}" : "")} not found in the sales register"
                    : "No original invoice reference on the note";
                if (fallback is not null)
                    refText += $" — nearest invoice {fallback.PrintedNo} dated {fallback.Invoice.InvDate:dd-MMM-yyyy} used for HSN / type";
                Issue("note-original", doc, refText, doc.DocValue);
            }
        }

        private List<TaxLine> NoteLines(NoteRow note, List<LineRow> items, List<LedgerRow> ledgerRows,
            (decimal Igst, decimal Cgst, decimal Sgst) heads, OriginalInvoice? orig,
            List<(string Category, string Detail, decimal? Amount)> pending)
        {
            var ex = note.ExchangeRate > 0 ? D(note.ExchangeRate) : 1m;
            var basic = D(note.Basic);
            var ledgerTax = heads.Igst + heads.Cgst + heads.Sgst;
            var lines = new List<TaxLine>();

            if (items.Count > 0)
            {
                var itemSum = items.Sum(i => D(i.Amount));
                var factor = basic > 0 && itemSum > 0 ? basic / itemSum : ex;
                foreach (var i in items)
                {
                    var t = ToTaxLine(i, factor);
                    t.Qty = note.NoteKind.Contains("Qty", StringComparison.OrdinalIgnoreCase) ? D(i.Qty) : 0;
                    var fromOrig = orig?.Lines.Where(l => string.Equals(l.Commodity, i.Commodity, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(l => l.Taxable).FirstOrDefault();
                    if (fromOrig is not null)
                    {
                        t.Hsn = fromOrig.Hsn;
                        t.Uqc = fromOrig.Uqc;
                    }
                    else if (t.Hsn.Length == 0)
                        AssignFromOriginal(t, orig);
                    lines.Add(t);
                }
                var itemTax = lines.Sum(l => l.Igst + l.Cgst + l.Sgst);
                if (ledgerRows.Count > 0 && Math.Abs(itemTax - ledgerTax) > 1)
                    pending.Add(("tax-mismatch",
                        $"Note items carry tax {itemTax:N2}; ledger GST rows {ledgerTax:N2} — return uses ledger values", ledgerTax - itemTax));
            }
            else
            {
                if (basic == 0) basic = D(note.NoteValue) - ledgerTax;
                var t = new TaxLine { Taxable = basic, Description = note.Ledger };
                if (ledgerTax != 0) t.Rate = RateFromLedger(ledgerRows, ledgerTax, basic);
                AssignFromOriginal(t, orig);
                lines.Add(t);
            }

            if (ledgerRows.Count > 0)
            {
                var taxed = lines.Where(l => l.Rate > 0).ToList();
                if (taxed.Count == 0 && ledgerTax != 0)
                {
                    var taxable = lines.Sum(l => l.Taxable);
                    var rate = RateFromLedger(ledgerRows, ledgerTax, taxable);
                    foreach (var l in lines) l.Rate = rate;
                    taxed = lines;
                }
                var weight = taxed.Sum(l => l.Taxable * l.Rate);
                foreach (var l in lines)
                {
                    var share = weight == 0 ? (taxed.Count == 0 ? 0 : 1m / taxed.Count) : l.Taxable * l.Rate / weight;
                    if (!taxed.Contains(l)) share = 0;
                    l.Igst = heads.Igst * share;
                    l.Cgst = heads.Cgst * share;
                    l.Sgst = heads.Sgst * share;
                }
            }

            foreach (var l in lines.Where(l => l.Hsn.Length == 0))
                pending.Add(("hsn-missing", orig is null
                    ? "HSN could not be determined — no original invoice to take it from"
                    : "HSN could not be determined from the original invoice", l.Taxable));
            return lines;
        }

        private static decimal RateFromLedger(List<LedgerRow> rows, decimal ledgerTax, decimal taxable)
        {
            var igstRate = rows.Where(r => Head(r.Ledger) == "I").Select(r => D(r.Rate)).DefaultIfEmpty(0).Max();
            var cgstRate = rows.Where(r => Head(r.Ledger) == "C").Select(r => D(r.Rate)).DefaultIfEmpty(0).Max();
            var sgstRate = rows.Where(r => Head(r.Ledger) == "S").Select(r => D(r.Rate)).DefaultIfEmpty(0).Max();
            var fromRows = igstRate > 0 ? igstRate : cgstRate + sgstRate;
            if (fromRows > 0) return SnapRate(fromRows);
            return taxable == 0 ? 0 : SnapRate(ledgerTax / taxable * 100m);
        }

        private static void AssignFromOriginal(TaxLine t, OriginalInvoice? orig)
        {
            if (orig is null || orig.Lines.Count == 0) return;
            var src = orig.Lines.Where(l => t.Rate > 0 && l.Rate == t.Rate).OrderByDescending(l => l.Taxable).FirstOrDefault()
                      ?? orig.Lines.Where(l => l.Rate > 0).OrderByDescending(l => l.Taxable).FirstOrDefault()
                      ?? orig.Lines.OrderByDescending(l => l.Taxable).First();
            t.Hsn = src.Hsn;
            t.Uqc = src.Uqc;
            if (t.Commodity.Length == 0) t.Commodity = src.Commodity;
            if (t.Description.Length == 0 || t.Description.Contains("Interest", StringComparison.OrdinalIgnoreCase))
                t.Description = src.Description;
        }

        private (OriginalInvoice? Exact, OriginalInvoice? Fallback) ResolveOriginal(NoteRow note)
        {
            var variants = ReferenceVariants(note.OrigNo).ToList();
            if (variants.Count == 0) return (null, null);
            var candidates = variants
                .SelectMany(v => _originals[NoteKey(note.Company, v)])
                .GroupBy(i => DocKey(i.Company, i.InvNo, i.InvDate), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            if (candidates.Count == 0) return (null, null);

            var exact = note.OrigDate is { } d
                ? candidates.FirstOrDefault(c => c.InvDate.Date == d.Date)
                : null;
            if (exact is not null) return (Original(exact), null);

            var fallback = candidates
                .Where(c => c.InvDate.Date <= note.NoteDate.Date && (note.NoteDate.Date - c.InvDate.Date).TotalDays <= OriginalLookbackDays)
                .OrderByDescending(c => c.InvDate)
                .FirstOrDefault();
            return (null, fallback is null ? null : Original(fallback));
        }

        private OriginalInvoice Original(InvoiceRow inv)
        {
            var key = DocKey(inv.Company, inv.InvNo, inv.InvDate);
            if (_originalCache.TryGetValue(key, out var cached)) return cached;
            var ex = inv.ExchangeRate > 0 ? D(inv.ExchangeRate) : 1m;
            var lines = _originalLines[LineKey(inv.Company, inv.InvNo, inv.InvYear)]
                .Select(l => ToTaxLine(l, ex)).ToList();
            var igst = lines.Sum(l => l.Igst);
            var cgst = lines.Sum(l => l.Cgst + l.Sgst);
            var export = FindExport(inv);
            var cls = Classify(inv, Company(inv.Company), igst, cgst, export, null, emit: false);
            var result = new OriginalInvoice(inv, Printed(inv.Company, inv.InvNo), lines, cls);
            _originalCache[key] = result;
            return result;
        }

        // ------------------------------------------------ helpers

        private TaxLine ToTaxLine(LineRow l, decimal factor)
        {
            var taxable = D(l.Amount) * factor;
            var igst = D(l.Igst) * factor;
            var cgst = D(l.Cgst) * factor;
            var sgst = D(l.Sgst) * factor;
            var tax = igst + cgst + sgst;
            var per = D(l.IgstPer) + D(l.CgstPer) + D(l.SgstPer);
            var rate = tax == 0 ? 0 : per > 0 ? SnapRate(per) : taxable == 0 ? 0 : SnapRate(tax / taxable * 100m);
            var hsn = CleanHsn(l.Hsn);
            if (hsn.Length == 0) hsn = CleanHsn(l.MasterHsn);
            return new TaxLine
            {
                Hsn = hsn,
                Description = l.Commodity.Length > 0 ? l.Commodity : l.ItemName,
                Commodity = l.Commodity,
                Uqc = Uqc(l.Per, hsn),
                Qty = D(l.Qty),
                Rate = rate,
                Taxable = taxable,
                Igst = igst,
                Cgst = cgst,
                Sgst = sgst,
            };
        }

        private ExportRow? FindExport(InvoiceRow inv)
        {
            var matches = _exportInfo[NoteKey(inv.Company, inv.InvNo)].ToList();
            if (matches.Count == 0) return null;
            return matches
                .OrderBy(e => e.InvDate.Date == inv.InvDate.Date ? 0 : 1)
                .ThenBy(e => e.ShippingBillNo.Length > 0 ? 0 : 1)
                .ThenBy(e => e.Source)
                .First();
        }

        private string? ResolveState(string state, string destination)
        {
            foreach (var text in new[] { state, destination })
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (_stateCodes.TryGetValue(StateKey(text), out var code)) return code;
            }
            return null;
        }

        private CompanyInfo Company(string name) =>
            _companies.TryGetValue(name, out var info) ? info : new CompanyInfo(name, "", "24", "");

        private string Printed(string company, string invNo)
        {
            if (DigitsOnly.IsMatch(invNo))
                return (_plainPrefixes.TryGetValue(company, out var p) ? p : DefaultPrefix(company)) + invNo;
            return StripFinancialYear(invNo);
        }

        private string DefaultPrefix(string company) =>
            _domesticPrefixes.TryGetValue(company, out var p) ? p
            : _companies.TryGetValue(company, out var info) && info.Code.Length > 0 ? $"{info.Code}/D/"
            : "";

        /// <summary>Plain serials get the prefix the Document Summary prints (prefixed variants in the month, then the Despatch book, then CODE/D/).</summary>
        private void BuildPlainPrefixes()
        {
            foreach (var g in _invoices.GroupBy(i => i.Company, StringComparer.OrdinalIgnoreCase))
            {
                var plain = g.Where(i => DigitsOnly.IsMatch(i.InvNo) && i.InvNo.Length <= 15).Select(i => long.Parse(i.InvNo, CultureInfo.InvariantCulture)).ToHashSet();
                if (plain.Count == 0) continue;
                var (min, max) = (plain.Min(), plain.Max());
                var alias = g.Select(i => DomesticPrinted.Match(i.InvNo))
                    .Where(m => m.Success && m.Groups["n"].Value.Length <= 15)
                    .Select(m => (Prefix: m.Groups["p"].Value, Serial: long.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture)))
                    .Where(x => x.Serial >= min && x.Serial <= max && !plain.Contains(x.Serial))
                    .GroupBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .FirstOrDefault();
                _plainPrefixes[g.Key] = alias ?? DefaultPrefix(g.Key);
            }
        }

        private void Issue(string category, Doc doc, string detail, decimal? amount)
        {
            var (title, severity) = Categories[category];
            _exceptions.Add(new Gstr1ExceptionDto(category, title, severity, doc.Company,
                doc.PrintedNo, doc.Date, doc.Party,
                (doc.Kind == "INV" ? "" : doc.Kind == "CN" ? "Credit note · " : "Debit note · ") + detail,
                amount is null ? null : R(amount.Value)));
        }

        private static bool IsSezParty(string gstin, params string[] names) =>
            SezBuyerGstins.Contains(gstin) || names.Any(n => !string.IsNullOrEmpty(n) && SezMarker.IsMatch(n));

        private static string Head(string ledger)
        {
            var u = ledger.ToUpperInvariant();
            if (!u.Contains("OUTPUT")) return "";
            if (u.Contains("IGST")) return "I";
            if (u.Contains("CGST")) return "C";
            if (u.Contains("SGST") || u.Contains("UTGST")) return "S";
            return "";
        }

        private static (decimal Igst, decimal Cgst, decimal Sgst) HeadTotals(IEnumerable<LedgerRow> rows)
        {
            decimal i = 0, c = 0, s = 0;
            foreach (var r in rows)
            {
                switch (Head(r.Ledger))
                {
                    case "I": i += D(r.Amount); break;
                    case "C": c += D(r.Amount); break;
                    case "S": s += D(r.Amount); break;
                }
            }
            return (i, c, s);
        }

        /// <summary>Buckets a non-GST invoice ledger row so bill amount vs HSN invoice value gaps can be explained.</summary>
        private static void AddBillComponent(Doc doc, LedgerRow t)
        {
            var u = t.Ledger.ToUpperInvariant();
            var amt = D(t.Amount);
            if (Head(t.Ledger).Length > 0) doc.BillGst += amt;
            else if (u.Contains("TCS")) { }
            else if (u.Contains("TDS")) doc.BillTds += amt;
            else if (u.Contains("REFUND") || u.Contains("REBATE")) doc.BillIgstRefund += amt;
            else if (u.Contains("DISCOUNT") || u.Contains("RATE DIFF")) doc.BillDiscount += amt;
            else if (u.Contains("ROUND")) doc.BillRoundOff += amt;
            else doc.BillCharges += amt;
        }

        private static bool IsChargeLedger(string ledger)
        {
            var u = ledger.ToUpperInvariant();
            if (u.Contains("GST") && (u.Contains("OUTPUT") || u.Contains("INPUT") || u.Contains("REFUND"))) return false;
            string[] notCharges = ["TDS", "TCS", "ROUND", "REBATE", "DISCOUNT", "RATE DIFF", "MAT CREDIT", "RECEIVABLE"];
            return !notCharges.Any(u.Contains);
        }

        private static string CleanHsn(string? raw) => new string((raw ?? "").Where(ch => !char.IsWhiteSpace(ch)).ToArray());

        private static string CleanPort(string? raw)
        {
            var p = new string((raw ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            return p.Length > 6 ? p[..6] : p;
        }

        private static string CleanShippingBill(string? raw) =>
            new string((raw ?? "").Where(char.IsLetterOrDigit).ToArray());

        private static string StateKey(string name) =>
            new string(name.ToUpperInvariant().Replace("&", "AND").Where(char.IsLetter).ToArray());

        private static string LineKey(string company, string invNo, string invYear) =>
            $"{company.Trim()}|{invNo.Trim()}|{invYear.Trim()}".ToUpperInvariant();

        private static string DocKey(string company, string no, DateTime date) =>
            $"{company.Trim()}|{no.Trim()}|{date:yyyy-MM-dd}".ToUpperInvariant();

        private static string NoteKey(string company, string no) => $"{company.Trim()}|{no.Trim()}".ToUpperInvariant();
    }

    // ---------------------------------------------------------------- internal models

    private sealed record BaseData(DateTime From, DateTime To, IReadOnlyDictionary<string, CompanyInfo> Companies,
        List<Doc> Docs, List<Gstr1ExceptionDto> Exceptions);

    private sealed record CompanyInfo(string Name, string Gstin, string StateCode, string Code);

    private sealed record OriginalInvoice(InvoiceRow Invoice, string PrintedNo, List<TaxLine> Lines, Classification Classification);

    private sealed record RateGroup(decimal Rate, decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst);

    private sealed class Classification
    {
        public string Section { get; set; } = "";
        public string SubType { get; set; } = "";
        public string PartyGstin { get; set; } = "";
        public string PosCode { get; set; } = "";
        public bool Inter { get; set; }
    }

    private sealed class HsnAcc
    {
        public decimal Qty, Taxable, Igst, Cgst, Sgst;
        public readonly Dictionary<string, decimal> Commodities = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Doc
    {
        public string Company { get; set; } = "";
        public string OwnGstin { get; set; } = "";
        public string Kind { get; set; } = "INV";
        public string ErpNo { get; set; } = "";
        public string PrintedNo { get; set; } = "";
        public DateTime Date { get; set; }
        public string ErpType { get; set; } = "";
        public string Party { get; set; } = "";
        public string PartyGstin { get; set; } = "";
        public string PosCode { get; set; } = "";
        public bool Inter { get; set; }
        public decimal DocValue { get; set; }
        public bool Approved { get; set; } = true;
        public string Section { get; set; } = "";
        public string SubType { get; set; } = "";
        public string PortCode { get; set; } = "";
        public string ShippingBillNo { get; set; } = "";
        public DateTime? ShippingBillDate { get; set; }
        public string OrigNo { get; set; } = "";
        public string OrigPrintedNo { get; set; } = "";
        public DateTime? OrigDate { get; set; }
        public List<TaxLine> Lines { get; set; } = [];
        public List<TaxLine> RawLines { get; set; } = [];
        public decimal RawTaxable { get; set; }
        public decimal RawIgst { get; set; }
        public decimal RawCgst { get; set; }
        public decimal RawSgst { get; set; }
        public string SalesLedger { get; set; } = "";
        public decimal Tcs { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal ChargesTaxable { get; set; }
        public decimal ChargesIgst { get; set; }
        public decimal ChargesCgst { get; set; }
        public decimal ChargesSgst { get; set; }
        public decimal BillGst { get; set; }
        public decimal BillTds { get; set; }
        public decimal BillIgstRefund { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal BillRoundOff { get; set; }
        public decimal BillCharges { get; set; }
        public decimal Sign => Kind == "CN" ? -1m : 1m;
    }

    private sealed class TaxLine
    {
        public string Hsn { get; set; } = "";
        public string Description { get; set; } = "";
        public string Commodity { get; set; } = "";
        public string Uqc { get; set; } = "OTH-OTHERS";
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Taxable { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public TaxLine Clone() => (TaxLine)MemberwiseClone();
    }

    private sealed class TrialBalanceRow
    {
        public string Company { get; set; } = "";
        public string Ledger { get; set; } = "";
        public string Under { get; set; } = "";
        public decimal OpenAll { get; set; }
        public decimal OpenFy { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    private sealed class GroupLedgerRow
    {
        public string Company { get; set; } = "";
        public string Head { get; set; } = "";
        public string GroupHead { get; set; } = "";
        public string B { get; set; } = "";
        public string C { get; set; } = "";
        public string D { get; set; } = "";
        public string E { get; set; } = "";
        public string F { get; set; } = "";
        public string IsPnl { get; set; } = "";
    }

    private sealed class CompanyRow
    {
        public string Company { get; set; } = "";
        public string Gstin { get; set; } = "";
        public string Code { get; set; } = "";
        public string State { get; set; } = "";
    }

    private sealed class InvoiceRow
    {
        public string Company { get; set; } = "";
        public string InvNo { get; set; } = "";
        public DateTime InvDate { get; set; }
        public string InvYear { get; set; } = "";
        public string VoucherType { get; set; } = "";
        public string PartyName { get; set; } = "";
        public double BillAmount { get; set; }
        public double ExchangeRate { get; set; }
        public int ApprovalStatus { get; set; }
        public string SalesLedger { get; set; } = "";
        public string Destination { get; set; } = "";
        public string ShippingBillNo { get; set; } = "";
        public DateTime? ShippingBillDate { get; set; }
        public string PartyGstin { get; set; } = "";
        public string PartyState { get; set; } = "";
        public string PartyLedger { get; set; } = "";
        public string PartyUnder { get; set; } = "";
    }

    private sealed class LineRow
    {
        public string Company { get; set; } = "";
        public string InvNo { get; set; } = "";
        public string InvYear { get; set; } = "";
        public DateTime InvDate { get; set; }
        public string Commodity { get; set; } = "";
        public string ItemName { get; set; } = "";
        public double Qty { get; set; }
        public string Per { get; set; } = "";
        public double Amount { get; set; }
        public double IgstPer { get; set; }
        public double CgstPer { get; set; }
        public double SgstPer { get; set; }
        public double Igst { get; set; }
        public double Cgst { get; set; }
        public double Sgst { get; set; }
        public string Hsn { get; set; } = "";
        public string MasterHsn { get; set; } = "";
    }

    private sealed class LedgerRow
    {
        public string Company { get; set; } = "";
        public string DocNo { get; set; } = "";
        public DateTime? DocDate { get; set; }
        public string Ledger { get; set; } = "";
        public double Amount { get; set; }
        public double Rate { get; set; }
    }

    private sealed class ExportRow
    {
        public string Company { get; set; } = "";
        public string InvNo { get; set; } = "";
        public DateTime InvDate { get; set; }
        public string BeingExport { get; set; } = "";
        public string PortCode { get; set; } = "";
        public string ShippingBillNo { get; set; } = "";
        public DateTime? ShippingBillDate { get; set; }
        public string Destination { get; set; } = "";
        public int Source { get; set; }
    }

    private sealed class NoteRow
    {
        public string Company { get; set; } = "";
        public string NoteNo { get; set; } = "";
        public DateTime NoteDate { get; set; }
        public string NoteKind { get; set; } = "";
        public string OrigNo { get; set; } = "";
        public DateTime? OrigDate { get; set; }
        public string PartyName { get; set; } = "";
        public double Basic { get; set; }
        public double NoteValue { get; set; }
        public double ExchangeRate { get; set; }
        public int ApprovalStatus { get; set; }
        public string Ledger { get; set; } = "";
        public string PartyGstin { get; set; } = "";
        public string PartyState { get; set; } = "";
        public string PartyLedger { get; set; } = "";
        public string PartyUnder { get; set; } = "";
    }
}

public record Gstr1ScopeDto(string Key, string Label, string Gstin, IReadOnlyList<string> Companies, bool IsGroup);

public record Gstr1SectionSummaryDto(string Section, string Label, int Documents, decimal InvoiceValue,
    decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst, decimal Cess);

public record Gstr1ReconRowDto(string Label, decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst);

public record Gstr1B2bRowDto(string Company, string Gstin, string ReceiverName, string InvoiceNo, string ErpInvoiceNo,
    DateTime InvoiceDate, decimal InvoiceValue, string PlaceOfSupply, string ReverseCharge, string InvoiceType,
    decimal Rate, decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst, decimal Cess, string VoucherType, bool Approved,
    string SalesLedger = "", decimal Tcs = 0, decimal OtherCharges = 0);

public record Gstr1B2clRowDto(string Company, string InvoiceNo, string ErpInvoiceNo, DateTime InvoiceDate, decimal InvoiceValue,
    string PlaceOfSupply, decimal Rate, decimal Taxable, decimal Igst, decimal Cess, string ReceiverName, string VoucherType, bool Approved);

public record Gstr1B2csRowDto(string Type, string PlaceOfSupply, decimal Rate, decimal Taxable, decimal Igst, decimal Cgst,
    decimal Sgst, decimal Cess, int Documents);

public record Gstr1ExpRowDto(string Company, string ExportType, string InvoiceNo, string ErpInvoiceNo, DateTime InvoiceDate,
    decimal InvoiceValue, string PortCode, string ShippingBillNo, DateTime? ShippingBillDate, decimal Rate, decimal Taxable,
    decimal Igst, decimal Cess, string ReceiverName, string VoucherType, bool Approved,
    decimal? Fob = null, string EgmNo = "", DateTime? EgmDate = null, string IcegateSbNo = "", string IcegateInvoiceNo = "",
    DateTime? IcegateInvoiceDate = null, decimal? IcegateIgst = null, string IcegateStatus = "", string IcegateNote = "",
    bool FilledFromIcegate = false);

public record Gstr1IcegateExtraDto(string Company, string CompanyLabel, string PortCode, string ShippingBillNo, DateTime? ShippingBillDate,
    string InvoiceNo, DateTime? InvoiceDate, decimal? Fob, decimal? IgstPaid, string EgmNo, DateTime? EgmDate);

public record Gstr1IcegateSummaryDto(bool HasData, int ShippingBillsStored, DateTime? LastUploadUtc, string? LastFileName,
    int Matched, int WithDifferences, int NotInIcegate, int NoData, IReadOnlyList<Gstr1IcegateExtraDto> NotInErp);

public record Gstr1NoteRowDto(string Company, string Gstin, string ReceiverName, string NoteNo, string ErpNoteNo, DateTime NoteDate,
    string NoteType, string PlaceOfSupply, string ReverseCharge, string SupplyType, decimal NoteValue, decimal Rate,
    decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst, decimal Cess, string OriginalInvoiceNo,
    DateTime? OriginalInvoiceDate, string ErpType, bool Approved);

public record Gstr1NilRowDto(string Description, decimal NilRated, decimal Exempted, decimal NonGst);

public record Gstr1HsnRowDto(string Hsn, string Description, string Uqc, decimal Quantity, decimal TotalValue, decimal Rate,
    decimal Taxable, decimal Igst, decimal Cgst, decimal Sgst, decimal Cess, string Commodity = "");

public record Gstr1ExceptionDto(string Category, string Title, string Severity, string Company, string DocumentNo,
    DateTime? DocumentDate, string Party, string Detail, decimal? Amount);

public record Gstr1ReportDto(
    DateTime From,
    DateTime To,
    DateTime GeneratedAtUtc,
    bool IncludeUnapproved,
    string Scope,
    string ScopeLabel,
    IReadOnlyList<string> Gstins,
    IReadOnlyList<string> Companies,
    IReadOnlyList<Gstr1ScopeDto> Scopes,
    IReadOnlyList<Gstr1SectionSummaryDto> Summary,
    IReadOnlyList<Gstr1ReconRowDto> Reconciliation,
    decimal ReconciliationDifference,
    IReadOnlyList<Gstr1B2bRowDto> B2b,
    IReadOnlyList<Gstr1B2clRowDto> B2cl,
    IReadOnlyList<Gstr1B2csRowDto> B2cs,
    IReadOnlyList<Gstr1ExpRowDto> Exp,
    IReadOnlyList<Gstr1NoteRowDto> Cdnr,
    IReadOnlyList<Gstr1NoteRowDto> Cdnur,
    IReadOnlyList<Gstr1NilRowDto> Nil,
    IReadOnlyList<Gstr1HsnRowDto> HsnB2b,
    IReadOnlyList<Gstr1HsnRowDto> HsnB2c,
    IReadOnlyList<GstDocumentSeriesDto> Docs,
    IReadOnlyList<Gstr1ExceptionDto> Exceptions,
    Gstr1IcegateSummaryDto? Icegate = null,
    IReadOnlyList<Gstr1HsnRowDto>? HsnSummary = null,
    IReadOnlyList<Gstr1SalesRegisterRowDto>? SalesRegister = null,
    IReadOnlyList<Gstr1TrialBalanceRowDto>? TrialBalance = null,
    IReadOnlyList<Gstr1LedgerReconRowDto>? LedgerRecon = null);

public record Gstr1SalesRegisterRowDto(string Company, string SalesLedger, string VoucherType, string InvoiceNo, string ErpInvoiceNo,
    DateTime InvoiceDate, string Party, string Gstin, string PlaceOfSupply, string Hsn, string Commodity, decimal Quantity, string Uqc,
    decimal Rate, decimal Value, decimal Igst, decimal Cgst, decimal Sgst, decimal Tcs, decimal OtherCharges, decimal GrossAmount,
    string Gstr1Status);

/// <summary>One ledger of the full trial balance; opening and closing are split into Dr / Cr columns.</summary>
public record Gstr1TrialBalanceRowDto(string Company, string Primary, string Group, string Under, string Ledger,
    decimal OpeningDebit, decimal OpeningCredit, decimal Debit, decimal Credit, decimal ClosingDebit, decimal ClosingCredit,
    bool IsPnl, bool IsAdjustment)
{
    /// <summary>Period credit − debit.</summary>
    public decimal Net => Credit - Debit;
}

public record Gstr1LedgerReconRowDto(string SalesLedger, string Under, decimal RegisterGross, decimal RegisterValue,
    decimal HsnInvoiceValue, decimal HsnTaxable, decimal TrialBalance, decimal DiffRegisterHsn, decimal DiffRegisterTb, string Remarks,
    decimal DiffInvoiceValue = 0, string InvoiceRemarks = "");
