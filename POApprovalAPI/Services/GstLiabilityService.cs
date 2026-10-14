using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Monthly GST input / output liability per company, built like the ERP Account → Accounts Book → Group Consolidate
/// "Group Summary" of the GST Liabilities and GST Input Credits groups (opening / debit / credit / closing per ledger
/// per unit, from vw_LedgerSummary). Output pivot = closing balance of the Output A/c ledgers; input pivot =
/// "Cl balance" (debit − credit for the month) of the Input ledgers, as in the Accounts liability working.
/// Month-end set-off journals (vouchers that also post to GST Payable / GST Receivable / "For GST Balance Only")
/// can optionally be left out to see the position before set-off. Signs follow the ERP: debit positive, credit negative.
/// </summary>
public sealed class GstLiabilityService
{
    private const int QueryTimeoutSeconds = 300;

    public static readonly string[] OutputLedgers = ["IGST OUTPUT A/C", "CGST OUTPUT A/C", "SGST OUTPUT A/C"];
    public static readonly string[] InputLedgers = ["IGST INPUT", "CGST INPUT", "SGST INPUT"];
    private static readonly string[] LiabilityOtherLedgers = ["GST PAYABLE A/C", "GST RCM PAYABLE A/C"];
    private static readonly string[] InputOtherLedgers = ["GST CREDIT-NON ELIGIBLE", "GST UNDER PROTEST", "GST 2A ENTRIES"];

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
        ["GST 2A ENTRIES"] = "GST 2A Entries",
    };

    /// <summary>Company codes used in the Accounts liability working, by GSTIN.</summary>
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

    /// <summary>Units reported under their own code instead of their GSTIN's code.</summary>
    private static readonly Dictionary<string, string> UnitCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Plastene India Limited - HO"] = "PIL HO",
    };

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public GstLiabilityService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<GstLiabilityDto> GetAsync(DateTime monthStart, bool excludeSetOff = false, bool refresh = false)
    {
        var from = new DateTime(monthStart.Year, monthStart.Month, 1);
        var toExclusive = from.AddMonths(1);
        var rows = await LoadAsync(from, toExclusive, refresh);

        var known = KnownRegistrations.ToDictionary(k => k.Gstin, k => k.Code, StringComparer.OrdinalIgnoreCase);
        var details = rows
            .Select(r =>
            {
                var debit = r.Debit + (excludeSetOff ? 0 : r.SetOffDebit);
                var credit = r.Credit + (excludeSetOff ? 0 : r.SetOffCredit);
                var knownCode = UnitCodes.GetValueOrDefault(r.Unit) ?? known.GetValueOrDefault(r.Gstin);
                return new GstLiabilityLedgerRow
                {
                    Unit = r.Unit,
                    Gstin = r.Gstin,
                    Code = knownCode ?? r.GroupName,
                    Known = knownCode != null,
                    Ledger = LedgerLabels.GetValueOrDefault(r.Ledger) ?? r.Ledger,
                    Group = OutputLedgers.Contains(r.Ledger) || LiabilityOtherLedgers.Contains(r.Ledger) ? "GST Liabilities" : "GST Input Credits",
                    Kind = OutputLedgers.Contains(r.Ledger) ? "output" : InputLedgers.Contains(r.Ledger) ? "input" : "other",
                    Tax = r.Ledger.StartsWith("IGST") ? "IGST" : r.Ledger.StartsWith("CGST") ? "CGST" : r.Ledger.StartsWith("SGST") ? "SGST" : "",
                    Opening = r.Opening,
                    Debit = debit,
                    Credit = credit,
                    Closing = r.Opening + debit - credit,
                    ClBalance = debit - credit,
                    SetOff = r.SetOffDebit - r.SetOffCredit,
                };
            })
            .Where(d => d.Opening != 0 || d.Debit != 0 || d.Credit != 0 || d.SetOff != 0 || d.Known)
            .OrderBy(d => d.Known ? 0 : 1).ThenBy(d => d.Code, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Unit)
            .ThenBy(d => d.Group).ThenBy(d => TaxOrder(d.Tax)).ThenBy(d => d.Ledger)
            .ToList();

        var companies = details
            .GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                decimal Output(string tax) => g.Where(d => d.Kind == "output" && d.Tax == tax).Sum(d => d.Closing);
                decimal Input(string tax) => g.Where(d => d.Kind == "input" && d.Tax == tax).Sum(d => d.ClBalance);
                var first = g.First();
                var group = rows.FirstOrDefault(r => string.Equals(r.Gstin, first.Gstin, StringComparison.OrdinalIgnoreCase))?.GroupName ?? "";
                return new GstLiabilityCompany
                {
                    Code = g.Key,
                    Gstin = first.Gstin,
                    Name = UnitCodes.ContainsKey(first.Unit) ? first.Unit : group,
                    Known = first.Known,
                    Units = g.Select(d => d.Unit).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList(),
                    OutputIgst = Output("IGST"),
                    OutputCgst = Output("CGST"),
                    OutputSgst = Output("SGST"),
                    InputIgst = Input("IGST"),
                    InputCgst = Input("CGST"),
                    InputSgst = Input("SGST"),
                    SetOff = g.Where(d => d.Kind != "other").Sum(d => Math.Abs(d.SetOff)),
                    Moved = g.Any(d => d.Debit != 0 || d.Credit != 0),
                };
            })
            .Where(c => c.Known || c.Moved)
            .OrderBy(c => c.Known ? 0 : 1).ThenBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var codes = new HashSet<string>(companies.Select(c => c.Code), StringComparer.OrdinalIgnoreCase);
        return new GstLiabilityDto
        {
            Month = from.ToString("yyyy-MM"),
            From = from.ToString("yyyy-MM-dd"),
            To = toExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            ExcludeSetOff = excludeSetOff,
            GeneratedAt = rows.Count > 0 ? rows[0].LoadedAt : DateTime.Now,
            Companies = companies,
            Ledgers = details.Where(d => codes.Contains(d.Code)).ToList(),
        };
    }

    private async Task<List<LedgerRow>> LoadAsync(DateTime from, DateTime toExclusive, bool refresh)
    {
        var key = $"gst-liability-v2:{from:yyyyMM}";
        if (!refresh && _cache.TryGetValue(key, out List<LedgerRow>? hit) && hit != null)
            return hit;

        // Plain "LedgerName IN" (case-insensitive collation) lets SQL Server filter inside each branch of the view;
        // the three reads run in parallel and set-off vouchers are matched here instead of with a correlated EXISTS.
        const string openingSql = """
            SELECT LTRIM(RTRIM(CompanyName)) AS Unit, UPPER(LTRIM(RTRIM(LedgerName))) AS Ledger,
                   SUM(CAST(amount AS decimal(18, 2))) AS Amount
            FROM dbo.vw_LedgerSummary
            WHERE Date < @From AND LedgerName IN @Ledgers
            GROUP BY LTRIM(RTRIM(CompanyName)), UPPER(LTRIM(RTRIM(LedgerName)))
            """;
        const string periodSql = """
            SELECT LTRIM(RTRIM(CompanyName)) AS Unit, UPPER(LTRIM(RTRIM(LedgerName))) AS Ledger,
                   VoucherType, VoucherNo, Date, SUM(CAST(amount AS decimal(18, 2))) AS Amount
            FROM dbo.vw_LedgerSummary
            WHERE Date >= @From AND Date < @ToExcl AND LedgerName IN @Ledgers
            GROUP BY LTRIM(RTRIM(CompanyName)), UPPER(LTRIM(RTRIM(LedgerName))), VoucherType, VoucherNo, Date
            """;
        const string setOffSql = """
            SELECT DISTINCT LTRIM(RTRIM(CompanyName)) AS Unit, VoucherType, VoucherNo, Date
            FROM dbo.vw_LedgerSummary
            WHERE Date >= @From AND Date < @ToExcl
              AND LedgerName IN (SELECT DISTINCT LedgerName FROM dbo.LedgerMaster WITH (NOLOCK)
                                 WHERE LedgerName IN ('GST PAYABLE A/C', 'GST RECEIVABLE') OR LedgerName LIKE '%For Gst%')
            """;
        const string unitSql = """
            SELECT LTRIM(RTRIM(Name)) AS Unit, MAX(LTRIM(RTRIM(NewGSTNo))) AS Gstin, MAX(LTRIM(RTRIM(GroupName))) AS GroupName
            FROM dbo.FactoryInfo WITH (NOLOCK) GROUP BY LTRIM(RTRIM(Name))
            """;

        var args = new
        {
            From = from,
            ToExcl = toExclusive,
            Ledgers = OutputLedgers.Concat(InputLedgers).Concat(LiabilityOtherLedgers).Concat(InputOtherLedgers).ToArray(),
        };
        async Task<List<T>> Query<T>(string sql)
        {
            using var connection = _database.CreateConnection();
            return (await connection.QueryAsync<T>(sql, args, commandTimeout: QueryTimeoutSeconds)).ToList();
        }
        var openingTask = Query<AmountRow>(openingSql);
        var periodTask = Query<VoucherRow>(periodSql);
        var setOffTask = Query<VoucherRow>(setOffSql);
        var unitTask = Query<UnitRow>(unitSql);
        await Task.WhenAll(openingTask, periodTask, setOffTask, unitTask);

        var setOffKeys = setOffTask.Result
            .Select(v => (v.Unit.ToUpperInvariant(), v.VoucherType, v.VoucherNo, v.Date))
            .ToHashSet();
        var units = unitTask.Result.ToDictionary(u => u.Unit, StringComparer.OrdinalIgnoreCase);
        var setOffLedgers = OutputLedgers.Concat(InputLedgers).ToHashSet();
        var now = DateTime.Now;
        var rows = new Dictionary<(string, string), LedgerRow>();
        LedgerRow Row(string unit, string ledger)
        {
            var k = (unit.ToUpperInvariant(), ledger);
            if (!rows.TryGetValue(k, out var r))
            {
                var u = units.GetValueOrDefault(unit);
                rows[k] = r = new LedgerRow
                {
                    Unit = unit, Ledger = ledger, Gstin = u?.Gstin ?? "", GroupName = u?.GroupName ?? unit, LoadedAt = now,
                };
            }
            return r;
        }
        foreach (var o in openingTask.Result) Row(o.Unit, o.Ledger).Opening += o.Amount;
        foreach (var v in periodTask.Result)
        {
            var r = Row(v.Unit, v.Ledger);
            var setOff = setOffLedgers.Contains(v.Ledger)
                && setOffKeys.Contains((v.Unit.ToUpperInvariant(), v.VoucherType, v.VoucherNo, v.Date));
            if (setOff)
            {
                if (v.Amount > 0) r.SetOffDebit += v.Amount; else r.SetOffCredit -= v.Amount;
            }
            else
            {
                if (v.Amount > 0) r.Debit += v.Amount; else r.Credit -= v.Amount;
            }
        }

        var list = rows.Values.ToList();
        _cache.Set(key, list, TimeSpan.FromHours(2));
        return list;
    }

    private static int TaxOrder(string tax) => tax switch { "IGST" => 0, "CGST" => 1, "SGST" => 2, _ => 3 };

    public async Task<byte[]> BuildExcelAsync(GstLiabilityExcelRequest request)
    {
        if (!DateTime.TryParseExact(request.Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            throw new ArgumentException("Month must be yyyy-MM.");
        var data = await GetAsync(month, request.ExcludeSetOff);
        var include = new HashSet<string>(request.Codes ?? [], StringComparer.OrdinalIgnoreCase);
        var companies = data.Companies
            .Where(c => include.Count == 0 ? c.Known : include.Contains(c.Code))
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (companies.Count == 0)
            throw new ArgumentException("Select at least one company.");
        var balances = (request.Balances ?? []).ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var label = month.ToString("MMM-yy", CultureInfo.InvariantCulture);
        var codes = companies.Select(c => c.Code).ToList();

        using var wb = new XLWorkbook();
        var pivotSheet = wb.Worksheets.Add("Pivot");
        var output = WriteOutputTable(wb.Worksheets.Add("Output details"), data, codes);
        var input = WriteInputTable(wb.Worksheets.Add("Input details"), $"Input Balance — {label}", data, codes);
        WriteOtherTable(wb.Worksheets.Add("Other GST ledgers"), $"GST Payable / RCM / non-eligible — {label}", data, codes);
        WritePivot(pivotSheet, data, label, codes, output, input, balances);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor BalanceFill = XLColor.FromHtml("#FFF7D6");
    private const string Amount = "#,##,##0.00;[Red]-#,##,##0.00";

    /// <summary>Group Summary rows for every code × ledger, with zero rows so both pivots list the same companies.</summary>
    private static List<GstLiabilityLedgerRow> RowsFor(GstLiabilityDto data, List<string> codes, string kind, string[] ledgers)
    {
        var rows = data.Ledgers.Where(l => l.Kind == kind && codes.Contains(l.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        foreach (var code in codes)
        {
            foreach (var ledger in ledgers)
            {
                var label = LedgerLabels[ledger];
                if (rows.Any(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase) && r.Ledger == label)) continue;
                var c = data.Companies.First(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                rows.Add(new GstLiabilityLedgerRow
                {
                    Unit = c.Units.FirstOrDefault() ?? c.Name, Code = c.Code, Ledger = label, Kind = kind,
                    Tax = ledger[..4],
                });
            }
        }
        return rows
            .OrderBy(r => r.Code, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Unit, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => TaxOrder(r.Tax))
            .ToList();
    }

    private static IXLRange WriteOutputTable(IXLWorksheet ws, GstLiabilityDto data, List<string> codes)
    {
        string[] headers = ["Perticular", "Opening Balance", "Debit", "Credit", "Closing Balance", "Company", "Co"];
        WriteHeader(ws, 1, headers);
        var row = 1;
        foreach (var l in RowsFor(data, codes, "output", OutputLedgers))
        {
            row++;
            ws.Cell(row, 1).Value = l.Ledger;
            ws.Cell(row, 2).Value = l.Opening;
            ws.Cell(row, 3).Value = l.Debit;
            ws.Cell(row, 4).Value = l.Credit;
            ws.Cell(row, 5).Value = l.Closing;
            ws.Cell(row, 6).Value = l.Unit;
            ws.Cell(row, 7).Value = l.Code;
        }
        FinishTable(ws, 1, row, headers.Length, [2, 3, 4, 5]);
        ws.Column(1).Width = 22; ws.Column(6).Width = 46; ws.Column(7).Width = 10;
        for (var c = 2; c <= 5; c++) ws.Column(c).Width = 17;
        return ws.Range(1, 1, row, headers.Length);
    }

    private static IXLRange WriteInputTable(IXLWorksheet ws, string title, GstLiabilityDto data, List<string> codes)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(Navy);
        string[] headers = ["Perticular", "Opening Balance", "Debit", "Credit", "Cl balance", "Closing Balance", "Company", "Co2"];
        WriteHeader(ws, 3, headers);
        var row = 3;
        foreach (var l in RowsFor(data, codes, "input", InputLedgers))
        {
            row++;
            ws.Cell(row, 1).Value = l.Ledger;
            ws.Cell(row, 2).Value = DrCr(l.Opening);
            ws.Cell(row, 3).Value = l.Debit;
            ws.Cell(row, 4).Value = l.Credit;
            ws.Cell(row, 5).Value = l.ClBalance;
            ws.Cell(row, 6).Value = DrCr(l.Closing);
            ws.Cell(row, 7).Value = l.Unit;
            ws.Cell(row, 8).Value = l.Code;
        }
        FinishTable(ws, 3, row, headers.Length, [3, 4, 5]);
        ws.Range(4, 2, row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Range(4, 6, row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Column(1).Width = 22; ws.Column(7).Width = 46; ws.Column(8).Width = 10;
        for (var c = 2; c <= 6; c++) ws.Column(c).Width = 17;
        return ws.Range(3, 1, row, headers.Length);
    }

    private static void WriteOtherTable(IXLWorksheet ws, string title, GstLiabilityDto data, List<string> codes)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(Navy);
        string[] headers = ["Group", "Perticular", "Opening Balance", "Debit", "Credit", "Closing Balance", "Company", "Co"];
        WriteHeader(ws, 3, headers);
        var row = 3;
        foreach (var l in data.Ledgers.Where(l => l.Kind == "other" && codes.Contains(l.Code, StringComparer.OrdinalIgnoreCase)))
        {
            row++;
            ws.Cell(row, 1).Value = l.Group;
            ws.Cell(row, 2).Value = l.Ledger;
            ws.Cell(row, 3).Value = l.Opening;
            ws.Cell(row, 4).Value = l.Debit;
            ws.Cell(row, 5).Value = l.Credit;
            ws.Cell(row, 6).Value = l.Closing;
            ws.Cell(row, 7).Value = l.Unit;
            ws.Cell(row, 8).Value = l.Code;
        }
        FinishTable(ws, 3, row, headers.Length, [3, 4, 5, 6]);
        ws.Column(1).Width = 18; ws.Column(2).Width = 24; ws.Column(7).Width = 46; ws.Column(8).Width = 10;
        for (var c = 3; c <= 6; c++) ws.Column(c).Width = 17;
    }

    private static void WriteHeader(IXLWorksheet ws, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++) ws.Cell(row, i + 1).Value = headers[i];
        ws.Range(row, 1, row, headers.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Navy);
    }

    private static void FinishTable(IXLWorksheet ws, int header, int last, int cols, int[] amountCols)
    {
        if (last > header)
        {
            foreach (var c in amountCols) ws.Range(header + 1, c, last, c).Style.NumberFormat.Format = Amount;
            var box = ws.Range(header, 1, last, cols);
            box.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            box.Style.Border.SetInsideBorderColor(XLColor.FromHtml("#CBD5E1"));
        }
        ws.SheetView.FreezeRows(header);
    }

    /// <summary>
    /// Same layout as the Accounts working: Output pivot (A–E), Net balance (F), cash / credit ledger balance (G–I),
    /// Input pivot (K–O). The pivots refresh when the workbook is opened; Net uses GETPIVOTDATA so it follows them.
    /// </summary>
    private static void WritePivot(IXLWorksheet ws, GstLiabilityDto data, string label, List<string> codes,
        IXLRange output, IXLRange input, Dictionary<string, GstLiabilityBalanceInput> balances)
    {
        ws.Cell(1, 1).Value = $"GST Input / Output liability — {label} · {D(data.From)} to {D(data.To)}"
            + (data.ExcludeSetOff ? " · before month-end set-off" : "");
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontColor(Navy);

        var outPivot = ws.PivotTables.Add("OutputPivot", ws.Cell(3, 1), output);
        outPivot.Layout = XLPivotLayout.Tabular;
        outPivot.RowLabels.Add("Co");
        outPivot.ColumnLabels.Add("Perticular");
        outPivot.Values.Add("Closing Balance", "Sum of Closing Balance").NumberFormat.Format = Amount;
        outPivot.ShowGrandTotalsRows = true;
        outPivot.ShowGrandTotalsColumns = true;
        outPivot.Theme = XLPivotTableTheme.PivotStyleLight16;

        var inPivot = ws.PivotTables.Add("InputPivot", ws.Cell(3, 11), input);
        inPivot.Layout = XLPivotLayout.Tabular;
        inPivot.RowLabels.Add("Co2");
        inPivot.ColumnLabels.Add("Perticular");
        inPivot.Values.Add("Cl balance", "Sum of Cl balance").NumberFormat.Format = Amount;
        inPivot.ShowGrandTotalsRows = true;
        inPivot.ShowGrandTotalsColumns = true;
        inPivot.Theme = XLPivotTableTheme.PivotStyleLight16;

        ws.Cell(3, 6).Value = "Payable in (-)/ Balance (+)";
        ws.Cell(3, 7).Value = "Balance";
        ws.Cell(4, 6).Value = "Net Balance";
        ws.Cell(4, 7).Value = "IGST  A/c";
        ws.Cell(4, 8).Value = "CGST  A/c";
        ws.Cell(4, 9).Value = "SGST  A/c";
        ws.Range(3, 6, 4, 9).Style.Font.SetBold().Fill.SetBackgroundColor(BalanceFill);

        var row = 4;
        foreach (var code in codes)
        {
            row++;
            var b = balances.GetValueOrDefault(code);
            var q = code.Replace("\"", "\"\"");
            ws.Cell(row, 6).FormulaA1 =
                $"IFERROR(GETPIVOTDATA(\"Closing Balance\",$A$3,\"Co\",\"{q}\"),0)+IFERROR(GETPIVOTDATA(\"Cl balance\",$K$3,\"Co2\",\"{q}\"),0)+SUM(G{row}:I{row})";
            if (b?.Igst is { } i && i != 0) ws.Cell(row, 7).Value = i;
            if (b?.Cgst is { } c && c != 0) ws.Cell(row, 8).Value = c;
            if (b?.Sgst is { } s && s != 0) ws.Cell(row, 9).Value = s;
        }
        row++;
        for (var c = 6; c <= 9; c++)
        {
            var col = XLHelper.GetColumnLetterFromNumber(c);
            ws.Cell(row, c).FormulaA1 = $"SUM({col}5:{col}{row - 1})";
        }
        ws.Range(row, 6, row, 9).Style.Font.SetBold();
        ws.Range(5, 6, row, 9).Style.NumberFormat.Format = Amount;
        ws.Range(5, 6, row, 6).Style.Font.SetBold();
        var box = ws.Range(3, 6, row, 9);
        box.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        box.Style.Border.SetInsideBorderColor(XLColor.FromHtml("#CBD5E1"));

        ws.Cell(row + 2, 1).Value =
            "Balance (IGST / CGST / SGST A/c) = cash / credit ledger balance. Net Balance = output pivot total + input pivot total + balance. Negative = GST payable.";
        ws.Cell(row + 2, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#64748B"));

        ws.Column(1).Width = 11;
        for (var c = 2; c <= 9; c++) ws.Column(c).Width = 16;
        ws.Column(10).Width = 3;
        ws.Column(11).Width = 11;
        for (var c = 12; c <= 15; c++) ws.Column(c).Width = 16;
    }

    private static string DrCr(decimal v) =>
        Math.Abs(v).ToString("#,##,##0.00", CultureInfo.GetCultureInfo("en-IN")) + (v < 0 ? " Cr" : " Dr");

    private static string D(string iso) =>
        DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
            : iso;

    private sealed class AmountRow
    {
        public string Unit { get; set; } = "";
        public string Ledger { get; set; } = "";
        public decimal Amount { get; set; }
    }

    private sealed class VoucherRow
    {
        public string Unit { get; set; } = "";
        public string Ledger { get; set; } = "";
        public string VoucherType { get; set; } = "";
        public string VoucherNo { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class UnitRow
    {
        public string Unit { get; set; } = "";
        public string? Gstin { get; set; }
        public string? GroupName { get; set; }
    }

    private sealed class LedgerRow
    {
        public string Unit { get; set; } = "";
        public string Ledger { get; set; } = "";
        public string Gstin { get; set; } = "";
        public string GroupName { get; set; } = "";
        public decimal Opening { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal SetOffDebit { get; set; }
        public decimal SetOffCredit { get; set; }
        public DateTime LoadedAt { get; set; }
    }
}

public sealed class GstLiabilityDto
{
    public string Month { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public bool ExcludeSetOff { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<GstLiabilityCompany> Companies { get; set; } = [];
    public List<GstLiabilityLedgerRow> Ledgers { get; set; } = [];
}

public sealed class GstLiabilityCompany
{
    public string Code { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Known { get; set; }
    public List<string> Units { get; set; } = [];
    public decimal OutputIgst { get; set; }
    public decimal OutputCgst { get; set; }
    public decimal OutputSgst { get; set; }
    public decimal InputIgst { get; set; }
    public decimal InputCgst { get; set; }
    public decimal InputSgst { get; set; }
    /// <summary>Absolute amount of month-end set-off entries on the Output / Input ledgers this month.</summary>
    public decimal SetOff { get; set; }
    public bool Moved { get; set; }
}

public sealed class GstLiabilityLedgerRow
{
    public string Unit { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string Code { get; set; } = "";
    public bool Known { get; set; }
    public string Ledger { get; set; } = "";
    public string Group { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Tax { get; set; } = "";
    public decimal Opening { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Closing { get; set; }
    /// <summary>Debit − credit for the month (the "Cl balance" column of the Accounts input sheet).</summary>
    public decimal ClBalance { get; set; }
    public decimal SetOff { get; set; }
}

public sealed class GstLiabilityBalanceInput
{
    public string Code { get; set; } = "";
    public decimal Igst { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
}

public sealed class GstLiabilityExcelRequest
{
    public string Month { get; set; } = "";
    public bool ExcludeSetOff { get; set; }
    public List<string>? Codes { get; set; }
    public List<GstLiabilityBalanceInput>? Balances { get; set; }
}
