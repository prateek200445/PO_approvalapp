using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// GSTR-1 Table 13 "Documents issued" summary built from the ERP registers:
/// SalesVoucher (invoices), CreditNote and DebitNote. A series is the document number with its
/// running serial replaced by "#". Numbers missing between the first and last serial in the period
/// are reported as cancelled — the ERP deletes cancelled documents instead of flagging them.
/// Only sales-side debit notes are reported: those carrying GST OUTPUT tax or raised on a Debtors
/// party. Supplier-side debit notes still occupy serials in the shared series, so they are neither
/// reported nor counted as cancelled.
/// </summary>
public class GstDocumentSummaryService
{
    private const int CommandTimeoutSeconds = 120;
    private const int MaxMissingListed = 200;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);
    private static readonly Regex TrailingFinancialYear = new(@"/\d{2}-\d{2}$", RegexOptions.Compiled);

    public const string NatureInvoice = "Invoices for outward supply";
    public const string NatureCreditNote = "Credit Note";
    public const string NatureDebitNote = "Debit Note";
    public const string NatureRcmSelfInvoice = "Invoices for inward supply from unregistered person";

    private const string DocumentsSql = @"
SELECT LTRIM(RTRIM(CompanyName)) AS Company, 'INV' AS Kind, LTRIM(RTRIM(InvNo)) AS DocNo,
       InvDate AS DocDate, LTRIM(RTRIM(ISNULL(VoucherType, ''))) AS VoucherType, CAST(1 AS bit) AS Reported
FROM SalesVoucher WITH (NOLOCK)
WHERE InvDate >= @From AND InvDate < @ToExclusive AND ISNULL(LTRIM(RTRIM(InvNo)), '') <> ''
UNION ALL
SELECT LTRIM(RTRIM(CompanyName)), 'CN', LTRIM(RTRIM(CreditNoteNumber)),
       CreditNoteDate, LTRIM(RTRIM(ISNULL(CreditType, ''))), CAST(1 AS bit)
FROM CreditNote WITH (NOLOCK)
WHERE CreditNoteDate >= @From AND CreditNoteDate < @ToExclusive AND ISNULL(LTRIM(RTRIM(CreditNoteNumber)), '') <> ''
UNION ALL
SELECT LTRIM(RTRIM(d.CompanyName)), 'DN', LTRIM(RTRIM(d.DebitNoteNumber)),
       d.sysdate, LTRIM(RTRIM(ISNULL(d.PurchaseLedger, ''))),
       CAST(CASE
           WHEN EXISTS (
               SELECT 1 FROM PurchaseVoucherTax t WITH (NOLOCK)
               WHERE t.CompanyName = d.CompanyName AND t.Debitnotenumber = d.DebitNoteNumber
                 AND t.TaxledgerName LIKE '%OUTPUT%')
             OR p.Under LIKE 'Debtor%' THEN 1 ELSE 0 END AS bit)
FROM DebitNote d WITH (NOLOCK)
LEFT JOIN LedgerMaster p WITH (NOLOCK) ON p.srno = d.PartyID
WHERE d.sysdate >= @From AND d.sysdate < @ToExclusive AND ISNULL(LTRIM(RTRIM(d.DebitNoteNumber)), '') <> ''";

    internal const string CompanyCodesSql = @"
SELECT LTRIM(RTRIM(Name)) AS Company, LTRIM(RTRIM(IndentCode)) AS Code
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(IndentCode)), '') <> ''";

    /// <summary>Printed prefix of plain-number domestic invoices (e.g. PIL1/D/), from the Despatch invoice book.</summary>
    internal const string DomesticPrefixSql = @"
WITH x AS (
    SELECT LTRIM(RTRIM(CompanyName)) AS Company,
           LEFT(InvType, LEN(InvType) - CHARINDEX('/', REVERSE(InvType)) + 1) AS Prefix
    FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK)
    WHERE InvDate >= DATEADD(YEAR, -3, GETDATE())
      AND InvType LIKE '%/D/%'
      AND InvType LIKE '%/' + CAST(InvNo AS varchar(20)))
SELECT Company, Prefix FROM x GROUP BY Company, Prefix ORDER BY Company, COUNT(*) DESC";

    /// <summary>
    /// The ERP keeps no RCM self-invoice numbers. As the tax team does, one self-invoice
    /// (CODE/RCM/Mon/1) is issued per company per month with RCM purchase postings;
    /// the "GST RCM payable" ledger is the tax settlement, not a purchase.
    /// </summary>
    private const string RcmMonthsSql = @"
SELECT LTRIM(RTRIM(CompanyName)) AS Company, YEAR(Date) AS [Year], MONTH(Date) AS [Month],
       COUNT(*) AS Postings, MAX(Date) AS LastDate
FROM vw_LedgerSummary WITH (NOLOCK)
WHERE Date >= @From AND Date < @ToExclusive
  AND LedgerName LIKE '%RCM%' AND LedgerName NOT LIKE '%payable%'
GROUP BY LTRIM(RTRIM(CompanyName)), YEAR(Date), MONTH(Date)
HAVING ABS(SUM(amount)) >= 0.5";

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public GstDocumentSummaryService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<GstDocumentSummaryDto> GetSummaryAsync(DateTime from, DateTime to, bool refresh = false)
    {
        var fromDate = from.Date;
        var toDate = to.Date < fromDate ? fromDate : to.Date;
        var key = $"gst-doc-summary-v3|{fromDate:yyyy-MM-dd}|{toDate:yyyy-MM-dd}";
        if (refresh)
            _cache.Remove(key);

        if (_cache.TryGetValue(key, out GstDocumentSummaryDto? cached) && cached is not null)
            return cached;

        await using var conn = _database.CreateConnection();
        var args = new { From = fromDate, ToExclusive = toDate.AddDays(1) };
        var docs = (await conn.QueryAsync<DocRow>(DocumentsSql, args, commandTimeout: CommandTimeoutSeconds)).ToList();

        var codes = (await conn.QueryAsync<(string Company, string Code)>(CompanyCodesSql, commandTimeout: CommandTimeoutSeconds))
            .GroupBy(x => x.Company, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Code, StringComparer.OrdinalIgnoreCase);

        var domesticPrefixes = (await conn.QueryAsync<(string Company, string Prefix)>(DomesticPrefixSql, commandTimeout: CommandTimeoutSeconds))
            .GroupBy(x => x.Company, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Prefix, StringComparer.OrdinalIgnoreCase);

        var rcmMonths = await conn.QueryAsync<RcmMonthRow>(RcmMonthsSql, args, commandTimeout: CommandTimeoutSeconds);
        foreach (var m in rcmMonths)
        {
            var code = codes.TryGetValue(m.Company, out var c) && !string.IsNullOrWhiteSpace(c) ? c.Trim() : m.Company;
            var mon = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m.Month);
            docs.Add(new DocRow
            {
                Company = m.Company,
                Kind = "RCM",
                DocNo = $"{code}/RCM/{mon}/1",
                DocDate = m.LastDate,
                VoucherType = $"Self-invoice for {m.Postings} RCM purchase posting{(m.Postings == 1 ? "" : "s")}",
            });
        }

        var dto = Build(fromDate, toDate, docs, domesticPrefixes, codes);
        _cache.Set(key, dto, CacheTtl);
        return dto;
    }

    public async Task<byte[]> BuildExcelAsync(DateTime from, DateTime to, string? company, bool refresh = false)
    {
        var data = await GetSummaryAsync(from, to, refresh);
        var rows = string.IsNullOrWhiteSpace(company)
            ? data.Rows
            : data.Rows.Where(r => string.Equals(r.Company, company.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        var headerBlue = XLColor.FromHtml("#1F6FB2");
        var peach = XLColor.FromHtml("#F8CBAD");
        var paleYellow = XLColor.FromHtml("#FFF9C4");
        var periodLabel = data.From.Month == data.To.Month && data.From.Year == data.To.Year
            ? data.From.ToString("MMM-yy", CultureInfo.InvariantCulture)
            : $"{data.From:dd-MMM-yy} to {data.To:dd-MMM-yy}";

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Document Summary");
        ws.Style.Font.SetFontName("Times New Roman").Font.SetFontSize(11);

        var r = 1;
        foreach (var block in rows.GroupBy(x => x.Company, StringComparer.OrdinalIgnoreCase))
        {
            var list = block.ToList();
            var total = list.Sum(x => x.TotalNumber);
            var cancelled = list.Sum(x => x.Cancelled);
            var net = list.Sum(x => x.NetIssued);

            ws.Cell(r, 1).Value = periodLabel;
            ws.Cell(r, 1).Style.Font.SetBold().Font.SetUnderline();
            ws.Cell(r, 2).Value = block.Key;
            ws.Cell(r, 2).Style.Font.SetBold();
            ws.Cell(r, 5).Value = net;
            ws.Cell(r, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            r++;

            ws.Range(r, 1, r, 5).Style.Fill.SetBackgroundColor(headerBlue);
            ws.Cell(r, 4).Value = "Total Number";
            ws.Cell(r, 5).Value = "Total Cancelled";
            ws.Range(r, 4, r, 5).Style.Font.SetBold().Font.SetFontColor(XLColor.White);
            r++;
            ws.Cell(r, 4).Value = total;
            ws.Cell(r, 5).Value = cancelled;
            ws.Range(r, 4, r, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            r++;

            var headerRow = r;
            string[] headers = ["Nature of Document", "Sr. No. From", "Sr. No. To", "Total Number", "Cancelled"];
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(r, i + 1).Value = headers[i];
            ws.Range(r, 1, r, 5).Style.Fill.SetBackgroundColor(peach);
            ws.Range(r, 4, r, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            r++;

            foreach (var row in list)
            {
                ws.Cell(r, 1).Value = row.Nature;
                ws.Cell(r, 2).Value = row.FromNo;
                ws.Cell(r, 3).Value = row.ToNo;
                ws.Cell(r, 4).Value = row.TotalNumber;
                ws.Cell(r, 4).Style.NumberFormat.Format = "0.00";
                if (row.Cancelled > 0)
                {
                    ws.Cell(r, 5).Value = row.Cancelled;
                    ws.Cell(r, 5).Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    ws.Cell(r, 5).Value = "-";
                    ws.Cell(r, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }
                ws.Cell(r, 3).Style.Fill.SetBackgroundColor(paleYellow);
                r++;
            }

            ws.Range(headerRow - 2, 1, r - 1, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(headerRow, 1, r - 1, 5).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            r += 2;
        }

        if (rows.Count == 0)
            ws.Cell(1, 1).Value = $"{periodLabel} — no documents";

        ws.Column(1).Width = 52;
        ws.Columns(2, 3).Width = 22;
        ws.Columns(4, 5).Width = 16;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static GstDocumentSummaryDto Build(
        DateTime from,
        DateTime to,
        List<DocRow> docs,
        IReadOnlyDictionary<string, string> domesticPrefixes,
        IReadOnlyDictionary<string, string> codes)
    {
        var rows = new List<GstDocumentSeriesDto>();

        var groups = docs
            .Select(d => (Doc: d, Parsed: ParseNumber(d.DocNo)))
            .GroupBy(x => (x.Doc.Company, x.Doc.Kind, x.Parsed.Series), new SeriesKeyComparer())
            .Select(g => new SeriesGroup(g.Key.Company, g.Key.Kind, g.Key.Series, g.ToList()))
            .ToList();
        MergePrefixedIntoPlainSeries(groups);

        foreach (var g in groups.Where(g => g.Series == "#"))
        {
            var alias = g.Aliases.FirstOrDefault(a => a.EndsWith("/#", StringComparison.Ordinal));
            g.PlainPrefix = alias is not null ? alias[..^1]
                : domesticPrefixes.TryGetValue(g.Company, out var p) ? p
                : codes.TryGetValue(g.Company, out var code) ? $"{code}/D/"
                : "";
        }

        foreach (var g in groups)
        {
            var items = g.Items.Where(x => x.Doc.Reported).ToList();
            if (items.Count == 0) continue;
            var occupied = new HashSet<long>(g.Items.Where(x => x.Parsed.Serial is not null).Select(x => x.Parsed.Serial!.Value));
            var voucherTypes = items.Select(x => x.Doc.VoucherType).Where(v => v.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();
            var numbered = items.Where(x => x.Parsed.Serial is not null).ToList();
            var distinctDocs = items.Select(x => x.Doc.DocNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            if (numbered.Count == 0)
            {
                var names = items.Select(x => x.Doc.DocNo).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                rows.Add(new GstDocumentSeriesDto(
                    g.Company, NatureFor(g.Kind), g.Label, g.Display(names.First()), g.Display(names.Last()),
                    distinctDocs, 0, distinctDocs, [], false, voucherTypes,
                    items.Min(x => x.Doc.DocDate), items.Max(x => x.Doc.DocDate)));
                continue;
            }

            var serials = numbered.Select(x => x.Parsed.Serial!.Value).Distinct().OrderBy(n => n).ToList();
            var min = serials[0];
            var max = serials[^1];
            var missing = new List<string>();
            var missingCount = 0L;
            for (var n = min; n <= max; n++)
            {
                if (occupied.Contains(n)) continue;
                missingCount++;
                if (missing.Count < MaxMissingListed)
                    missing.Add(n.ToString(CultureInfo.InvariantCulture));
            }

            var minDoc = numbered.First(x => x.Parsed.Serial == min).Doc.DocNo;
            var maxDoc = numbered.First(x => x.Parsed.Serial == max).Doc.DocNo;

            rows.Add(new GstDocumentSeriesDto(
                g.Company, NatureFor(g.Kind), g.Label, g.Display(minDoc), g.Display(maxDoc),
                serials.Count + missingCount, missingCount, serials.Count, missing, missingCount > missing.Count, voucherTypes,
                items.Min(x => x.Doc.DocDate), items.Max(x => x.Doc.DocDate)));
        }

        var ordered = rows
            .OrderBy(r => r.Company, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => NatureOrder(r.Nature))
            .ThenBy(r => r.Series, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var companies = ordered.Select(r => r.Company).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();

        return new GstDocumentSummaryDto(from, to, DateTime.UtcNow, companies, ordered);
    }

    /// <summary>
    /// Some invoices of a plain-number series are keyed with a prefix (e.g. 1600 entered as PIL1/D/1600).
    /// A "PREFIX/#" series is folded into the company's plain "#" series when all its serials sit inside
    /// the plain range and none of them clash with a plain number.
    /// </summary>
    private static void MergePrefixedIntoPlainSeries(List<SeriesGroup> groups)
    {
        foreach (var plain in groups.Where(g => g.Series == "#").ToList())
        {
            var plainSerials = new HashSet<long>(plain.Items.Select(x => x.Parsed.Serial!.Value));
            var min = plainSerials.Min();
            var max = plainSerials.Max();

            var candidates = groups.Where(g =>
                    !ReferenceEquals(g, plain)
                    && g.Kind == plain.Kind
                    && string.Equals(g.Company, plain.Company, StringComparison.OrdinalIgnoreCase)
                    && g.Series.EndsWith("/#", StringComparison.Ordinal)
                    && g.Items.All(x => x.Parsed.Serial is long s && s >= min && s <= max && !plainSerials.Contains(s)))
                .ToList();

            foreach (var c in candidates)
            {
                plain.Items.AddRange(c.Items);
                plain.Aliases.Add(c.Series);
                groups.Remove(c);
            }
        }
    }

    /// <summary>
    /// The running serial is the last all-digit "/" token; the financial-year token (e.g. 26-27) never qualifies.
    /// </summary>
    private static (string Series, long? Serial) ParseNumber(string docNo)
    {
        var parts = docNo.Split('/');
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            var token = parts[i].Trim();
            if (token.Length is 0 or > 15 || !DigitsOnly.IsMatch(token)) continue;
            var copy = (string[])parts.Clone();
            copy[i] = "#";
            return (string.Join("/", copy), long.Parse(token, CultureInfo.InvariantCulture));
        }
        return (docNo, null);
    }

    private static string NatureFor(string kind) => kind switch
    {
        "CN" => NatureCreditNote,
        "DN" => NatureDebitNote,
        "RCM" => NatureRcmSelfInvoice,
        _ => NatureInvoice,
    };

    private static int NatureOrder(string nature) => nature switch
    {
        NatureInvoice => 0,
        NatureDebitNote => 1,
        NatureCreditNote => 2,
        NatureRcmSelfInvoice => 3,
        _ => 4,
    };

    private sealed class SeriesKeyComparer : IEqualityComparer<(string Company, string Kind, string Series)>
    {
        public bool Equals((string Company, string Kind, string Series) a, (string Company, string Kind, string Series) b) =>
            string.Equals(a.Company, b.Company, StringComparison.OrdinalIgnoreCase)
            && a.Kind == b.Kind
            && string.Equals(a.Series, b.Series, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Company, string Kind, string Series) k) =>
            HashCode.Combine(k.Company.ToUpperInvariant(), k.Kind, k.Series.ToUpperInvariant());
    }

    private sealed class SeriesGroup(string company, string kind, string series, List<(DocRow Doc, (string Series, long? Serial) Parsed)> items)
    {
        public string Company { get; } = company;
        public string Kind { get; } = kind;
        public string Series { get; } = series;
        public List<(DocRow Doc, (string Series, long? Serial) Parsed)> Items { get; } = items;
        public List<string> Aliases { get; } = [];
        public string PlainPrefix { get; set; } = "";
        public string Label => Display(Series);

        /// <summary>Number as printed on the return: plain serials get the company prefix, a trailing FY token is dropped.</summary>
        public string Display(string number)
        {
            var printed = Series == "#" && !number.Contains('/') ? PlainPrefix + number : number;
            return TrailingFinancialYear.Replace(printed, "");
        }
    }

    private sealed class DocRow
    {
        public string Company { get; set; } = "";
        public string Kind { get; set; } = "";
        public string DocNo { get; set; } = "";
        public DateTime DocDate { get; set; }
        public string VoucherType { get; set; } = "";
        public bool Reported { get; set; } = true;
    }

    private sealed class RcmMonthRow
    {
        public string Company { get; set; } = "";
        public int Year { get; set; }
        public int Month { get; set; }
        public int Postings { get; set; }
        public DateTime LastDate { get; set; }
    }
}

public record GstDocumentSeriesDto(
    string Company,
    string Nature,
    string Series,
    string FromNo,
    string ToNo,
    long TotalNumber,
    long Cancelled,
    long NetIssued,
    IReadOnlyList<string> MissingNumbers,
    bool MissingTruncated,
    IReadOnlyList<string> VoucherTypes,
    DateTime FirstDate,
    DateTime LastDate);

public record GstDocumentSummaryDto(
    DateTime From,
    DateTime To,
    DateTime GeneratedAtUtc,
    IReadOnlyList<string> Companies,
    IReadOnlyList<GstDocumentSeriesDto> Rows);
