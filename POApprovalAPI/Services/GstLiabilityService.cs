using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Monthly GST input / output liability per GST registration, from the ERP ledger postings (vw_LedgerSummary).
/// Month-end set-off journals (vouchers that also post to GST Payable / GST Receivable / "For GST Balance Only")
/// are left out of the period so the figures show the position before set-off, like the Group Summary taken
/// before Accounts book the transfer. Signs follow the ERP: debit positive, credit negative.
/// </summary>
public sealed class GstLiabilityService
{
    private const int QueryTimeoutSeconds = 300;

    public static readonly string[] OutputLedgers = ["IGST OUTPUT A/C", "CGST OUTPUT A/C", "SGST OUTPUT A/C"];
    public static readonly string[] InputLedgers = ["IGST INPUT", "CGST INPUT", "SGST INPUT"];
    public static readonly string[] OtherLedgers = ["GST PAYABLE A/C", "GST RCM PAYABLE A/C", "GST CREDIT-NON ELIGIBLE", "GST UNDER PROTEST"];

    private static readonly Dictionary<string, string> LedgerLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IGST OUTPUT A/C"] = "IGST Output A/c",
        ["CGST OUTPUT A/C"] = "CGST Output A/c",
        ["SGST OUTPUT A/C"] = "SGST Output A/c",
        ["IGST INPUT"] = "IGST Input",
        ["CGST INPUT"] = "CGST Input",
        ["SGST INPUT"] = "SGST Input",
        ["GST PAYABLE A/C"] = "GST Payable A/c",
        ["GST RCM PAYABLE A/C"] = "GST RCM payable A/c",
        ["GST CREDIT-NON ELIGIBLE"] = "GST Credit-Non Eligible",
        ["GST UNDER PROTEST"] = "GST under Protest",
    };

    /// <summary>Registrations shown in the Accounts liability working, by GSTIN.</summary>
    private static readonly (string Gstin, string Code)[] KnownRegistrations =
    [
        ("24AAACC7815N1ZI", "HCP Ent"),
        ("24AABCG1282C2Z6", "HPBL"),
        ("24AFOPG7793M1ZO", "JBP"),
        ("24AAECK6307M2ZE", "KP SEZ"),
        ("24AAECK6307M1ZF", "KPW"),
        ("24AADCM8286G1Z9", "Marudhar"),
        ("24AAACO3557B2ZV", "OCPL"),
        ("24AAACO7290H4ZB", "OEL"),
        ("24AAACO3087C1ZT", "PIL"),
        ("24AAFCP2956A1ZR", "PPL"),
    ];

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public GstLiabilityService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<GstLiabilityDto> GetAsync(DateTime monthStart, bool excludeSetOff = true, bool refresh = false)
    {
        var from = new DateTime(monthStart.Year, monthStart.Month, 1);
        var toExclusive = from.AddMonths(1);
        var key = $"gst-liability-v1:{from:yyyyMM}:{excludeSetOff}";
        if (!refresh && _cache.TryGetValue(key, out GstLiabilityDto? hit) && hit != null)
            return hit;

        const string sql = """
            WITH setoff AS (
                SELECT DISTINCT CompanyName, VoucherType, VoucherNo, Date
                FROM dbo.vw_LedgerSummary
                WHERE @ExcludeSetOff = 1 AND Date >= @From AND Date < @ToExcl
                  AND (UPPER(LTRIM(RTRIM(LedgerName))) IN ('GST PAYABLE A/C', 'GST RECEIVABLE') OR LedgerName LIKE '%For Gst%')
            ),
            l AS (
                SELECT LTRIM(RTRIM(l.CompanyName)) AS Unit, UPPER(LTRIM(RTRIM(l.LedgerName))) AS Ledger, l.Date,
                       CAST(l.amount AS decimal(18, 2)) AS Amount,
                       CASE WHEN l.Date >= @From
                             AND UPPER(LTRIM(RTRIM(l.LedgerName))) IN @SetOffLedgers
                             AND EXISTS (SELECT 1 FROM setoff s WHERE s.CompanyName = l.CompanyName AND s.VoucherType = l.VoucherType
                                                                 AND s.VoucherNo = l.VoucherNo AND s.Date = l.Date)
                            THEN 1 ELSE 0 END AS IsSetOff
                FROM dbo.vw_LedgerSummary l
                WHERE l.Date < @ToExcl AND UPPER(LTRIM(RTRIM(l.LedgerName))) IN @Ledgers
            )
            SELECT l.Unit, l.Ledger,
                   ISNULL(MAX(LTRIM(RTRIM(fi.NewGSTNo))), '') AS Gstin,
                   ISNULL(MAX(LTRIM(RTRIM(fi.GroupName))), l.Unit) AS GroupName,
                   SUM(CASE WHEN l.Date < @From THEN l.Amount ELSE 0 END) AS Opening,
                   SUM(CASE WHEN l.Date >= @From AND l.IsSetOff = 0 AND l.Amount > 0 THEN l.Amount ELSE 0 END) AS Debit,
                   SUM(CASE WHEN l.Date >= @From AND l.IsSetOff = 0 AND l.Amount < 0 THEN -l.Amount ELSE 0 END) AS Credit,
                   SUM(CASE WHEN l.IsSetOff = 1 THEN l.Amount ELSE 0 END) AS SetOff
            FROM l
            LEFT JOIN dbo.FactoryInfo fi WITH (NOLOCK) ON LTRIM(RTRIM(fi.Name)) = l.Unit
            GROUP BY l.Unit, l.Ledger
            """;

        List<LedgerRow> rows;
        using (var connection = _database.CreateConnection())
        {
            rows = (await connection.QueryAsync<LedgerRow>(sql, new
            {
                From = from,
                ToExcl = toExclusive,
                ExcludeSetOff = excludeSetOff ? 1 : 0,
                Ledgers = OutputLedgers.Concat(InputLedgers).Concat(OtherLedgers).ToArray(),
                SetOffLedgers = OutputLedgers.Concat(InputLedgers).ToArray(),
            }, commandTimeout: QueryTimeoutSeconds)).ToList();
        }

        var known = KnownRegistrations.ToDictionary(k => k.Gstin, k => k.Code, StringComparer.OrdinalIgnoreCase);
        var details = rows
            .Select(r =>
            {
                var closing = r.Opening + r.Debit - r.Credit;
                return new GstLiabilityLedgerRow
                {
                    Unit = r.Unit,
                    Gstin = r.Gstin,
                    Code = known.GetValueOrDefault(r.Gstin) ?? "",
                    Ledger = LedgerLabels.GetValueOrDefault(r.Ledger) ?? r.Ledger,
                    Kind = OutputLedgers.Contains(r.Ledger) ? "output" : InputLedgers.Contains(r.Ledger) ? "input" : "other",
                    Tax = r.Ledger.StartsWith("IGST") ? "IGST" : r.Ledger.StartsWith("CGST") ? "CGST" : r.Ledger.StartsWith("SGST") ? "SGST" : "",
                    Opening = r.Opening,
                    Debit = r.Debit,
                    Credit = r.Credit,
                    Closing = closing,
                    SetOff = r.SetOff,
                };
            })
            .Where(d => d.Opening != 0 || d.Debit != 0 || d.Credit != 0 || d.SetOff != 0 || !string.IsNullOrEmpty(d.Code))
            .OrderBy(d => d.Code == "" ? 1 : 0).ThenBy(d => d.Code).ThenBy(d => d.Unit).ThenBy(d => d.Kind).ThenBy(d => d.Ledger)
            .ToList();

        var registrations = details
            .Where(d => d.Kind != "other")
            .GroupBy(d => d.Gstin, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                decimal Sum(string kind, string tax) => g.Where(d => d.Kind == kind && d.Tax == tax).Sum(d => d.Closing);
                var group = rows.FirstOrDefault(r => string.Equals(r.Gstin, g.Key, StringComparison.OrdinalIgnoreCase))?.GroupName ?? "";
                var code = known.GetValueOrDefault(g.Key) ?? "";
                return new GstLiabilityRegistration
                {
                    Gstin = g.Key,
                    Code = code == "" ? group : code,
                    Name = group,
                    Known = code != "",
                    Units = g.Select(d => d.Unit).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList(),
                    OutputIgst = Sum("output", "IGST"),
                    OutputCgst = Sum("output", "CGST"),
                    OutputSgst = Sum("output", "SGST"),
                    InputIgst = Sum("input", "IGST"),
                    InputCgst = Sum("input", "CGST"),
                    InputSgst = Sum("input", "SGST"),
                    SetOffExcluded = g.Sum(d => Math.Abs(d.SetOff)),
                    Moved = g.Any(d => d.Debit != 0 || d.Credit != 0),
                };
            })
            .Where(r => r.Known || r.Moved)
            .OrderBy(r => r.Known ? 0 : 1).ThenBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dto = new GstLiabilityDto
        {
            Month = from.ToString("yyyy-MM"),
            From = from.ToString("yyyy-MM-dd"),
            To = toExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            ExcludeSetOff = excludeSetOff,
            GeneratedAt = DateTime.Now,
            Registrations = registrations,
            Ledgers = details.Where(d => registrations.Any(r => string.Equals(r.Gstin, d.Gstin, StringComparison.OrdinalIgnoreCase))).ToList(),
        };
        _cache.Set(key, dto, TimeSpan.FromMinutes(20));
        return dto;
    }

    public async Task<byte[]> BuildExcelAsync(GstLiabilityExcelRequest request)
    {
        if (!DateTime.TryParseExact(request.Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            throw new ArgumentException("Month must be yyyy-MM.");
        var data = await GetAsync(month, request.ExcludeSetOff);
        var include = new HashSet<string>(request.Gstins ?? [], StringComparer.OrdinalIgnoreCase);
        var regs = data.Registrations.Where(r => include.Count == 0 ? r.Known : include.Contains(r.Gstin)).ToList();
        var balances = (request.Balances ?? []).ToDictionary(b => b.Gstin, StringComparer.OrdinalIgnoreCase);
        var label = month.ToString("MMM-yy", CultureInfo.InvariantCulture);
        var title = $"GST Input / Output liability — {label}";
        var basis = data.ExcludeSetOff ? "before month-end set-off" : "including set-off entries";

        using var wb = new XLWorkbook();
        WriteSummary(wb.Worksheets.Add("Pivot"), title, basis, data, regs, balances);
        WriteDetails(wb.Worksheets.Add("Output details"), $"Output balance — {label}", data, regs, "output");
        WriteDetails(wb.Worksheets.Add("Input details"), $"Input balance — {label}", data, regs, "input");
        WriteDetails(wb.Worksheets.Add("Other GST ledgers"), $"GST Payable / RCM / non-eligible — {label}", data, regs, "other");

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor OutputFill = XLColor.FromHtml("#FDECEC");
    private static readonly XLColor InputFill = XLColor.FromHtml("#E9F6EE");
    private static readonly XLColor BalanceFill = XLColor.FromHtml("#EEF2FF");
    private const string Amount = "#,##,##0.00;[Red]-#,##,##0.00";

    private static void WriteSummary(IXLWorksheet ws, string title, string basis, GstLiabilityDto data,
        List<GstLiabilityRegistration> regs, Dictionary<string, GstLiabilityBalanceInput> balances)
    {
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 15).Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(Navy);
        ws.Cell(2, 1).Value = $"{D(data.From)} to {D(data.To)} · ledger closing balances {basis} · payable (-) / balance (+) · generated {data.GeneratedAt.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture)}";
        ws.Range(2, 1, 2, 15).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#64748B"));

        ws.Cell(4, 3).Value = "Output";
        ws.Range(4, 3, 4, 6).Merge().Style.Fill.SetBackgroundColor(OutputFill);
        ws.Cell(4, 7).Value = "Input";
        ws.Range(4, 7, 4, 10).Merge().Style.Fill.SetBackgroundColor(InputFill);
        ws.Cell(4, 11).Value = "Balance (cash / credit ledger)";
        ws.Range(4, 11, 4, 13).Merge().Style.Fill.SetBackgroundColor(BalanceFill);
        ws.Cell(4, 14).Value = "Net balance";
        ws.Range(4, 1, 4, 15).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        string[] headers = ["Co", "GSTIN", "IGST Output A/c", "CGST Output A/c", "SGST Output A/c", "Total output",
            "IGST Input", "CGST Input", "SGST Input", "Total input", "IGST A/c", "CGST A/c", "SGST A/c",
            "Payable (-) / Balance (+)", "Units"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(5, i + 1).Value = headers[i];
        ws.Range(5, 1, 5, headers.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Navy)
            .Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(5).Height = 30;

        var row = 5;
        foreach (var r in regs)
        {
            row++;
            var b = balances.GetValueOrDefault(r.Gstin);
            ws.Cell(row, 1).Value = r.Code;
            ws.Cell(row, 2).Value = r.Gstin;
            Num(row, 3, r.OutputIgst); Num(row, 4, r.OutputCgst); Num(row, 5, r.OutputSgst);
            ws.Cell(row, 6).FormulaA1 = $"SUM(C{row}:E{row})";
            Num(row, 7, r.InputIgst); Num(row, 8, r.InputCgst); Num(row, 9, r.InputSgst);
            ws.Cell(row, 10).FormulaA1 = $"SUM(G{row}:I{row})";
            Num(row, 11, b?.Igst ?? 0); Num(row, 12, b?.Cgst ?? 0); Num(row, 13, b?.Sgst ?? 0);
            ws.Cell(row, 14).FormulaA1 = $"F{row}+J{row}+SUM(K{row}:M{row})";
            ws.Cell(row, 15).Value = string.Join(", ", r.Units);
            ws.Cell(row, 15).Style.Font.SetFontColor(XLColor.FromHtml("#64748B"));
        }
        var first = 6;
        row++;
        ws.Cell(row, 1).Value = "Grand Total";
        for (var c = 3; c <= 14; c++)
        {
            var col = XLHelper.GetColumnLetterFromNumber(c);
            ws.Cell(row, c).FormulaA1 = row > first ? $"SUM({col}{first}:{col}{row - 1})" : "0";
        }
        ws.Range(row, 1, row, 15).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));
        ws.Range(first, 3, row, 14).Style.NumberFormat.Format = Amount;
        ws.Range(first, 6, row, 6).Style.Font.SetBold();
        ws.Range(first, 10, row, 10).Style.Font.SetBold();
        ws.Range(first, 14, row, 14).Style.Font.SetBold();
        ws.Range(first, 11, row - 1, 13).Style.Fill.SetBackgroundColor(BalanceFill);
        var box = ws.Range(5, 1, row, 15);
        box.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        box.Style.Border.SetInsideBorderColor(XLColor.FromHtml("#CBD5E1"));

        row += 2;
        ws.Cell(row, 1).Value = "Balance (IGST / CGST / SGST A/c) is the cash / credit ledger balance entered on the portal page; Net = total output + total input + balance. Negative net = GST payable.";
        ws.Range(row, 1, row, 15).Merge().Style.Font.SetItalic().Alignment.SetWrapText(true);
        ws.Row(row).Height = 28;

        ws.Column(1).Width = 11; ws.Column(2).Width = 18;
        for (var c = 3; c <= 14; c++) ws.Column(c).Width = 15;
        ws.Column(15).Width = 60;
        ws.SheetView.FreezeRows(5);

        void Num(int r, int c, decimal v) => ws.Cell(r, c).Value = v;
    }

    private static void WriteDetails(IXLWorksheet ws, string title, GstLiabilityDto data, List<GstLiabilityRegistration> regs, string kind)
    {
        var gstins = new HashSet<string>(regs.Select(r => r.Gstin), StringComparer.OrdinalIgnoreCase);
        var codes = regs.ToDictionary(r => r.Gstin, r => r.Code, StringComparer.OrdinalIgnoreCase);
        var rows = data.Ledgers.Where(l => l.Kind == kind && gstins.Contains(l.Gstin)).ToList();

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(Navy);
        string[] headers = ["Perticular", "Opening Balance", "Debit", "Credit", "Cl balance", "Closing Balance", "Company", "Co", "Set-off excluded"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        ws.Range(3, 1, 3, headers.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Navy);

        var row = 3;
        foreach (var l in rows)
        {
            row++;
            ws.Cell(row, 1).Value = l.Ledger;
            ws.Cell(row, 2).Value = DrCr(l.Opening);
            ws.Cell(row, 3).Value = l.Debit;
            ws.Cell(row, 4).Value = l.Credit;
            ws.Cell(row, 5).Value = l.Closing;
            ws.Cell(row, 6).Value = DrCr(l.Closing);
            ws.Cell(row, 7).Value = l.Unit;
            ws.Cell(row, 8).Value = codes.GetValueOrDefault(l.Gstin) ?? "";
            if (l.SetOff != 0) ws.Cell(row, 9).Value = l.SetOff;
        }
        if (row > 3)
        {
            ws.Range(4, 3, row, 5).Style.NumberFormat.Format = Amount;
            ws.Range(4, 9, row, 9).Style.NumberFormat.Format = Amount;
            ws.Range(4, 2, row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Range(4, 6, row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            var box = ws.Range(3, 1, row, headers.Length);
            box.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            box.Style.Border.SetInsideBorderColor(XLColor.FromHtml("#CBD5E1"));
        }
        ws.Column(1).Width = 26; ws.Column(2).Width = 18; ws.Column(3).Width = 16; ws.Column(4).Width = 16;
        ws.Column(5).Width = 16; ws.Column(6).Width = 18; ws.Column(7).Width = 42; ws.Column(8).Width = 10; ws.Column(9).Width = 16;
        ws.SheetView.FreezeRows(3);
    }

    private static string DrCr(decimal v) =>
        Math.Abs(v).ToString("#,##,##0.00", CultureInfo.GetCultureInfo("en-IN")) + (v < 0 ? " Cr" : " Dr");

    private static string D(string iso) =>
        DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
            : iso;

    private sealed class LedgerRow
    {
        public string Unit { get; set; } = "";
        public string Ledger { get; set; } = "";
        public string Gstin { get; set; } = "";
        public string GroupName { get; set; } = "";
        public decimal Opening { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal SetOff { get; set; }
    }
}

public sealed class GstLiabilityDto
{
    public string Month { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public bool ExcludeSetOff { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<GstLiabilityRegistration> Registrations { get; set; } = [];
    public List<GstLiabilityLedgerRow> Ledgers { get; set; } = [];
}

public sealed class GstLiabilityRegistration
{
    public string Gstin { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Known { get; set; }
    public List<string> Units { get; set; } = [];
    public decimal OutputIgst { get; set; }
    public decimal OutputCgst { get; set; }
    public decimal OutputSgst { get; set; }
    public decimal InputIgst { get; set; }
    public decimal InputCgst { get; set; }
    public decimal InputSgst { get; set; }
    public decimal SetOffExcluded { get; set; }
    public bool Moved { get; set; }
}

public sealed class GstLiabilityLedgerRow
{
    public string Unit { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string Code { get; set; } = "";
    public string Ledger { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Tax { get; set; } = "";
    public decimal Opening { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Closing { get; set; }
    public decimal SetOff { get; set; }
}

public sealed class GstLiabilityBalanceInput
{
    public string Gstin { get; set; } = "";
    public decimal Igst { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
}

public sealed class GstLiabilityExcelRequest
{
    public string Month { get; set; } = "";
    public bool ExcludeSetOff { get; set; } = true;
    public List<string>? Gstins { get; set; }
    public List<GstLiabilityBalanceInput>? Balances { get; set; }
}
