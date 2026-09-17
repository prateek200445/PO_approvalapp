using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using UglyToad.PdfPig;

namespace POApprovalAPI.Services;

public sealed class BankStatementService
{
    public static readonly string[] ExpectedHeaders =
    [
        "ERPBANKLEDGERNAME",
        "PARTY NAME",
        "Date (Value Date)",
        "Narration",
        "Ref/Cheque No.",
        "Debit",
        "Credit",
    ];

    private static readonly Regex DateOnlyLine = new(@"^\d{2}/\d{2}/\d{4}$", RegexOptions.Compiled);
    private static readonly Regex DateAtStart = new(@"^(\d{2}/\d{2}/\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex InrAmount = new(@"\d{1,3}(?:,\d{2,3})+(?:\.\d{2})|\d+\.\d{2}", RegexOptions.Compiled);
    private static readonly Regex UtrRef = new(
        @"(?:NEFT|RTGS|IMPS)[- ]?([A-Z0-9]+)|(?:INT\d+DEAL\d+)|(?:UPI[A-Z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex JunkLine = new(
        @"^(Contact-Us|Page \d|Customer Id|Branch Name|IFSC|Your Account|Statement of|Main Account|Address\s*:|MICR|Account No|Nominee|Statement Period|\*This is computer|H B J|G\*J|I\*D|A\*M|P\*N|No$|\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}\s+Page)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Parse a file already in BANKIMPORTFORMATE.csv layout.</summary>
    public BankStatementParseResult Parse(Stream stream, string? fileName)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var text = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("The uploaded file is empty.");

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1)
            throw new InvalidOperationException("No header row found.");

        var headers = SplitCsvLine(lines[0]).Select(NormalizeHeader).ToList();
        ValidateHeaders(headers);

        var rows = new List<BankStatementRowDto>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.All(string.IsNullOrWhiteSpace)) continue;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                var key = headers[c];
                if (string.IsNullOrWhiteSpace(key)) continue;
                map[key] = c < cols.Count ? cols[c].Trim().Trim('"') : "";
            }

            var row = MapImportRow(map, i);
            if (IsBlankRow(row)) continue;
            rows.Add(row);
        }

        return new BankStatementParseResult
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? "bank-statement.csv" : fileName,
            RowCount = rows.Count,
            Rows = rows,
            SourceKind = "import-format",
        };
    }

    /// <summary>
    /// Convert a raw bank statement (BOB PDF, or bank CSV/Excel) into BANKIMPORTFORMATE rows.
    /// </summary>
    public BankStatementParseResult ConvertFromBankFile(
        Stream stream,
        string? fileName,
        string? erpBankLedgerName)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        var ledger = (erpBankLedgerName ?? "").Trim();

        List<BankStatementRowDto> rows = ext switch
        {
            ".pdf" => ConvertBobPdf(stream, ledger),
            ".csv" or ".txt" => ConvertFlexibleCsv(stream, ledger, fileName),
            ".xlsx" or ".xlsm" => ConvertFlexibleExcel(stream, ledger),
            _ => throw new InvalidOperationException(
                "Unsupported bank statement type. Upload a bank PDF (BOB), CSV, or Excel (.xlsx)."),
        };

        if (rows.Count == 0)
            throw new InvalidOperationException("No transactions could be read from the bank statement.");

        return new BankStatementParseResult
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? "bank-statement" : fileName!,
            RowCount = rows.Count,
            Rows = rows,
            SourceKind = ext.TrimStart('.'),
            ErpBankLedgerName = ledger,
            Message = $"Converted {rows.Count} transaction(s) into bank import format.",
        };
    }

    public byte[] ExportCsv(IEnumerable<BankStatementRowDto> rows, bool includeCategorization = false)
    {
        var headers = includeCategorization
            ? ExpectedHeaders.Concat(new[] { "TRANSACTION TYPE", "MAIN CATEGORY", "SUB-CATEGORY" })
            : ExpectedHeaders;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            var cells = new List<string>
            {
                EscapeCsv(row.ErpBankLedgerName),
                EscapeCsv(row.PartyName),
                EscapeCsv(row.ValueDate),
                EscapeCsv(row.Narration),
                EscapeCsv(row.RefChequeNo),
                EscapeCsv(FormatAmount(row.Debit)),
                EscapeCsv(FormatAmount(row.Credit)),
            };
            if (includeCategorization)
            {
                cells.Add(EscapeCsv(row.TransactionType));
                cells.Add(EscapeCsv(row.MainCategory));
                cells.Add(EscapeCsv(row.SubCategory));
            }

            sb.AppendLine(string.Join(",", cells));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>
    /// Apply payment categorization rules (transaction type + main/sub category) from narration/party/amounts.
    /// </summary>
    public static void ApplyCategorization(IEnumerable<BankStatementRowDto> rows)
    {
        foreach (var row in rows)
            CategorizeRow(row);
    }

    private static void CategorizeRow(BankStatementRowDto row)
    {
        var narration = $"{row.Narration} {row.PartyName} {row.RefChequeNo}";
        var isCredit = row.Credit is > 0;
        var isDebit = row.Debit is > 0;

        row.TransactionType = isCredit && !isDebit
            ? "RECEIPT"
            : isDebit && !isCredit
                ? "PAYMENT"
                : isCredit
                    ? "RECEIPT"
                    : "PAYMENT";

        if (ContainsAny(narration, "GST", "GSTN", "GST PAYMENT", "CGST", "SGST", "IGST"))
        {
            SetCat(row, "Taxes & Government", "GST");
            return;
        }

        if (ContainsAny(narration, "TDS", "INCOME TAX", "ADVANCE TAX"))
        {
            SetCat(row, "Taxes & Government", "TDS");
            return;
        }

        if (ContainsAny(narration, "BANK CHARGES", "CHARGES", "CHG", "SMS CHARGES", "AMC"))
        {
            SetCat(row, "Banking", "Bank Charges");
            return;
        }

        if (Regex.IsMatch(narration, @"\bINT\d*DEAL\b", RegexOptions.IgnoreCase)
            || ContainsAny(narration, "INTEREST", "INT."))
        {
            SetCat(row, "Banking", "Interest");
            return;
        }

        if (ContainsAny(narration, "SALARY", "PAYROLL", "WAGES", "STAFF SAL"))
        {
            SetCat(row, "Employee Payment", "Salary / Wages");
            return;
        }

        if (ContainsAny(narration, "ELECTRIC", "ELECTRICITY", "TORRENT", "UGVCL", "PGVCL"))
        {
            SetCat(row, "Operating Expense", "Electricity");
            return;
        }

        if (ContainsAny(narration, "FREIGHT", "TRANSPORT", "LOGISTIC", "COURIER"))
        {
            SetCat(row, "Operating Expense", "Transport / Freight");
            return;
        }

        // Own-company / sister transfers
        if (ContainsAny(narration, "PLASTENE INDIA", "PLASTENE POLY", "HCP PLASTENE", "OSWAL EXTRUSION", "K.P. WOVEN", "KP WOVEN"))
        {
            SetCat(row,
                isCredit ? "Intercompany" : "Intercompany",
                isCredit ? "Intercompany Receipt" : "Intercompany Payment");
            return;
        }

        if (isCredit)
        {
            SetCat(row, "Customer Receipt", "Invoice Collection");
            return;
        }

        if (isDebit)
        {
            SetCat(row, "Supplier Payment", "Purchase Payment");
            return;
        }

        SetCat(row, "Other / Review", isCredit ? "Unknown Receipt" : "Unknown Payment");
    }

    private static void SetCat(BankStatementRowDto row, string main, string sub)
    {
        row.MainCategory = main;
        row.SubCategory = sub;
    }

    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    private List<BankStatementRowDto> ConvertBobPdf(Stream stream, string erpBankLedgerName)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        using var doc = PdfDocument.Open(ms);
        var text = ExtractPdfTextByLines(doc);
        var detectedLedger = string.IsNullOrWhiteSpace(erpBankLedgerName)
            ? DetectBobLedgerName(text)
            : erpBankLedgerName;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var start = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("NARRATION", StringComparison.OrdinalIgnoreCase)
                && lines[i].Contains("VALUE DATE", StringComparison.OrdinalIgnoreCase))
            {
                start = i + 1;
                break;
            }
        }

        var clean = new List<string>();
        for (var i = start; i < lines.Count; i++)
        {
            var ln = lines[i];
            if (JunkLine.IsMatch(ln)) continue;
            if (ln.Contains("Overdraft Account", StringComparison.OrdinalIgnoreCase)) continue;
            if (ln.Contains("NARRATION", StringComparison.OrdinalIgnoreCase)
                && ln.Contains("VALUE DATE", StringComparison.OrdinalIgnoreCase))
                continue;
            clean.Add(ln);
        }

        var blocks = new List<string>();
        string? cur = null;
        foreach (var ln in clean)
        {
            if (DateOnlyLine.IsMatch(ln))
            {
                if (cur != null)
                    cur = cur + " " + ln;
                continue;
            }

            if (DateAtStart.IsMatch(ln))
            {
                if (cur != null) blocks.Add(cur);
                cur = ln;
            }
            else if (cur != null)
            {
                cur = cur + " " + ln;
            }
        }

        if (cur != null) blocks.Add(cur);

        var rows = new List<BankStatementRowDto>();
        var lineNo = 0;
        foreach (var block in blocks)
        {
            lineNo++;
            var row = ParseBobBlock(block, detectedLedger, lineNo);
            if (row != null && !IsBlankRow(row))
                rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Normalize BOB PDF text: PdfPig page.Text keeps hyphens but glues dates/amounts.
    /// </summary>
    private static string ExtractPdfTextByLines(PdfDocument doc)
    {
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            var text = page.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) continue;

            // amount glued to value date: 1,98,538.0031/07/2026
            text = Regex.Replace(text, @"(\.\d{2})(\d{2}/\d{2}/\d{4})", "$1 $2");
            // value date glued to next tran date: 31/07/202631/07/2026
            text = Regex.Replace(text, @"(\d{2}/\d{2}/\d{4})(\d{2}/\d{2}/\d{4})", "$1\n$2");
            // tran date glued to deposit amount: 31/07/20268,00,000.00
            text = Regex.Replace(text, @"(\d{2}/\d{2}/\d{4})(\d{1,3},)", "$1 $2");
            // INT deal id (usually 7 digits) glued to amount: DEAL687915622,624.20 or DEAL6870273720.40
            text = Regex.Replace(
                text,
                @"(DEAL\d{7})(\d{1,3}(?:,\d{2,3})*\.\d{2})",
                "$1 $2",
                RegexOptions.IgnoreCase);
            // letter glued to amount: LTD-S1,98,538.00
            text = Regex.Replace(text, @"([A-Za-z\)])(\d{1,3}(?:,\d{2,3})+\.\d{2})", "$1 $2");
            // Split transactions onto new lines at tran-date boundaries
            text = Regex.Replace(text, @"(?<=\S)(?=\d{2}/\d{2}/\d{4}\s)", "\n");
            // Header labels often stuck together
            text = text
                .Replace("NARRATIONDEPOSIT", "NARRATION DEPOSIT", StringComparison.OrdinalIgnoreCase)
                .Replace("DEPOSIT(CR)TRAN", "DEPOSIT(CR) TRAN", StringComparison.OrdinalIgnoreCase)
                .Replace("TRAN DATECHQ", "TRAN DATE CHQ", StringComparison.OrdinalIgnoreCase)
                .Replace("CHQ.NO.WITHDRAWAL", "CHQ.NO. WITHDRAWAL", StringComparison.OrdinalIgnoreCase)
                .Replace("WITHDRAWAL(DR)BALANCE", "WITHDRAWAL(DR) BALANCE", StringComparison.OrdinalIgnoreCase)
                .Replace("BALANCE(INR)VALUE", "BALANCE(INR) VALUE", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string DetectBobLedgerName(string text)
    {
        var m = Regex.Match(
            text,
            @"Overdraft Account\s*-\s*0*32X+456|Overdraft Account\s*-\s*(\d{3,}X+\d+|\d{10,})",
            RegexOptions.IgnoreCase);
        if (m.Success)
            return "Bank Overdraft BOB 03240400000456";

        if (text.Contains("BARB0", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Bank of Baroda", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Overdraft Account", StringComparison.OrdinalIgnoreCase))
            return "Bank Overdraft BOB";

        return "";
    }

    private static BankStatementRowDto? ParseBobBlock(string block, string erpBankLedgerName, int sourceLine)
    {
        var m = DateAtStart.Match(block);
        if (!m.Success) return null;

        var tranDate = m.Groups[1].Value;
        var rest = block[m.Length..].Trim();
        var dri = Regex.Match(rest, @"\s*Dr\s*", RegexOptions.IgnoreCase);
        if (!dri.Success) return null;

        var before = rest[..dri.Index].Trim();
        var after = rest[(dri.Index + dri.Length)..].Trim();

        decimal? deposit = null;
        var beforeAmounts = InrAmount.Matches(before);
        if (beforeAmounts.Count >= 2)
            deposit = ParseInr(beforeAmounts[0].Value);

        var valueDate = tranDate;
        var vdMatch = Regex.Match(after, @"(\d{2}/\d{2}/\d{4})\s*$");
        if (vdMatch.Success)
        {
            valueDate = vdMatch.Groups[1].Value;
            after = after[..vdMatch.Index].Trim();
        }

        decimal? withdrawal = null;
        var narration = after;
        var afterAmounts = InrAmount.Matches(after);
        if (deposit is null && afterAmounts.Count > 0)
        {
            var last = afterAmounts[^1];
            var tail = after[last.Index..].Trim();
            if (InrAmount.IsMatch(tail) && Regex.Replace(tail, @"\s+", "") == last.Value.Replace(" ", ""))
            {
                withdrawal = ParseInr(last.Value);
                narration = after[..last.Index].Trim();
            }
        }

        narration = Regex.Replace(narration, @"\s+", " ").Trim().Trim('-', ' ');
        if (string.IsNullOrWhiteSpace(narration) && deposit is null && withdrawal is null)
            return null;

        var (refNo, party) = ExtractRefAndParty(narration);

        return new BankStatementRowDto
        {
            Id = Guid.NewGuid().ToString("N"),
            ErpBankLedgerName = erpBankLedgerName,
            PartyName = party,
            ValueDate = valueDate,
            Narration = narration,
            RefChequeNo = refNo,
            Debit = withdrawal,
            Credit = deposit,
            SourceLine = sourceLine,
        };
    }

    private static (string Ref, string Party) ExtractRefAndParty(string narration)
    {
        var refNo = "";
        var party = "";

        // Prefer UTR-style refs; avoid swallowing the amount into INT refs.
        var intMatch = Regex.Match(narration, @"\bINT\d+DEAL\d+\b", RegexOptions.IgnoreCase);
        if (intMatch.Success)
            refNo = intMatch.Value;
        else
        {
            var m = UtrRef.Match(narration);
            if (m.Success)
            {
                refNo = m.Groups[1].Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value)
                    ? m.Groups[1].Value.Trim()
                    : m.Value.Trim();
            }
        }

        // NEFT/RTGS-UTR-PARTY
        var parts = narration.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var head = parts[0];
            if (head.StartsWith("NEFT", StringComparison.OrdinalIgnoreCase)
                || head.StartsWith("RTGS", StringComparison.OrdinalIgnoreCase)
                || head.StartsWith("IMPS", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(refNo))
                    refNo = parts[1];
                party = string.Join(" ", parts.Skip(2)).Trim();
            }
        }
        else if (parts.Length == 2
                 && !parts[0].StartsWith("INT", StringComparison.OrdinalIgnoreCase))
        {
            party = parts[^1];
        }

        party = Regex.Replace(party, @"\s+", " ").Trim();
        return (refNo, party);
    }

    private List<BankStatementRowDto> ConvertFlexibleCsv(Stream stream, string erpBankLedgerName, string? fileName)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var text = reader.ReadToEnd();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            throw new InvalidOperationException("CSV has no data rows.");

        var headers = SplitCsvLine(lines[0]).Select(NormalizeHeader).ToList();

        // If already import format, reuse Parse path
        if (ExpectedHeaders.All(h => headers.Any(x => x.Equals(h, StringComparison.OrdinalIgnoreCase))))
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var parsed = Parse(ms, fileName);
            if (!string.IsNullOrWhiteSpace(erpBankLedgerName))
            {
                foreach (var r in parsed.Rows)
                {
                    if (string.IsNullOrWhiteSpace(r.ErpBankLedgerName))
                        r.ErpBankLedgerName = erpBankLedgerName;
                }
            }

            return parsed.Rows;
        }

        var mapIdx = ResolveFlexibleColumns(headers);
        var rows = new List<BankStatementRowDto>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.All(string.IsNullOrWhiteSpace)) continue;
            var row = MapFlexibleRow(cols, mapIdx, erpBankLedgerName, i);
            if (row != null && !IsBlankRow(row))
                rows.Add(row);
        }

        return rows;
    }

    private List<BankStatementRowDto> ConvertFlexibleExcel(Stream stream, string erpBankLedgerName)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed();
        if (used is null)
            throw new InvalidOperationException("Excel sheet is empty.");

        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();

        var headers = new List<string>();
        for (var c = firstCol; c <= lastCol; c++)
            headers.Add(NormalizeHeader(ws.Cell(firstRow, c).GetString()));

        var mapIdx = ResolveFlexibleColumns(headers);
        var rows = new List<BankStatementRowDto>();
        for (var r = firstRow + 1; r <= lastRow; r++)
        {
            var cols = new List<string>();
            for (var c = firstCol; c <= lastCol; c++)
                cols.Add(ws.Cell(r, c).GetFormattedString().Trim());
            if (cols.All(string.IsNullOrWhiteSpace)) continue;
            var row = MapFlexibleRow(cols, mapIdx, erpBankLedgerName, r);
            if (row != null && !IsBlankRow(row))
                rows.Add(row);
        }

        return rows;
    }

    private sealed class FlexibleColumnMap
    {
        public int? ValueDate { get; set; }
        public int? TranDate { get; set; }
        public int? Narration { get; set; }
        public int? Ref { get; set; }
        public int? Debit { get; set; }
        public int? Credit { get; set; }
        public int? Party { get; set; }
        public int? Ledger { get; set; }
        public int? Amount { get; set; }
    }

    private static FlexibleColumnMap ResolveFlexibleColumns(List<string> headers)
    {
        int? Find(params string[] aliases)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                foreach (var a in aliases)
                {
                    if (h.Equals(a, StringComparison.OrdinalIgnoreCase)
                        || h.Contains(a, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return null;
        }

        var map = new FlexibleColumnMap
        {
            ValueDate = Find("value date", "valuedate", "date (value date)"),
            TranDate = Find("tran date", "transaction date", "txn date", "trans date", "date"),
            Narration = Find("narration", "particulars", "description", "remarks"),
            Ref = Find("ref/cheque", "cheque", "chq.no", "chq no", "utr", "reference", "ref no"),
            Debit = Find("withdrawal", "debit", "withdrawal(dr)", "dr amount"),
            Credit = Find("deposit", "credit", "deposit(cr)", "cr amount"),
            Party = Find("party name", "party", "beneficiary", "counterparty"),
            Ledger = Find("erpbankledgername", "erp bank ledger", "bank ledger"),
            Amount = Find("amount"),
        };

        if (map.Narration is null && map.Debit is null && map.Credit is null && map.Amount is null)
            throw new InvalidOperationException(
                "Could not detect bank statement columns. Need Narration/Particulars and Debit/Credit (or Amount).");

        return map;
    }

    private static BankStatementRowDto? MapFlexibleRow(
        List<string> cols,
        FlexibleColumnMap map,
        string erpBankLedgerName,
        int sourceLine)
    {
        string Cell(int? idx) =>
            idx is int i && i >= 0 && i < cols.Count ? cols[i].Trim() : "";

        var narration = Cell(map.Narration);
        var valueDate = Cell(map.ValueDate);
        if (string.IsNullOrWhiteSpace(valueDate))
            valueDate = Cell(map.TranDate);

        var debit = ParseAmount(Cell(map.Debit));
        var credit = ParseAmount(Cell(map.Credit));
        if (debit is null && credit is null && map.Amount is not null)
        {
            var amt = ParseAmount(Cell(map.Amount));
            if (amt is > 0) credit = amt;
            else if (amt is < 0) debit = Math.Abs(amt.Value);
        }

        var party = Cell(map.Party);
        var refNo = Cell(map.Ref);
        if (string.IsNullOrWhiteSpace(party) || string.IsNullOrWhiteSpace(refNo))
        {
            var extracted = ExtractRefAndParty(narration);
            if (string.IsNullOrWhiteSpace(refNo)) refNo = extracted.Ref;
            if (string.IsNullOrWhiteSpace(party)) party = extracted.Party;
        }

        var ledger = Cell(map.Ledger);
        if (string.IsNullOrWhiteSpace(ledger))
            ledger = erpBankLedgerName;

        return new BankStatementRowDto
        {
            Id = Guid.NewGuid().ToString("N"),
            ErpBankLedgerName = ledger,
            PartyName = party,
            ValueDate = valueDate,
            Narration = narration,
            RefChequeNo = refNo,
            Debit = debit,
            Credit = credit,
            SourceLine = sourceLine,
        };
    }

    private static BankStatementRowDto MapImportRow(Dictionary<string, string> map, int sourceLine) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ErpBankLedgerName = Get(map, "ERPBANKLEDGERNAME"),
            PartyName = Get(map, "PARTY NAME"),
            ValueDate = Get(map, "Date (Value Date)"),
            Narration = Get(map, "Narration"),
            RefChequeNo = Get(map, "Ref/Cheque No."),
            Debit = ParseAmount(Get(map, "Debit")),
            Credit = ParseAmount(Get(map, "Credit")),
            SourceLine = sourceLine + 1,
        };

    private static string Get(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out var v) ? v.Trim() : "";

    private static bool IsBlankRow(BankStatementRowDto row) =>
        string.IsNullOrWhiteSpace(row.ErpBankLedgerName)
        && string.IsNullOrWhiteSpace(row.PartyName)
        && string.IsNullOrWhiteSpace(row.ValueDate)
        && string.IsNullOrWhiteSpace(row.Narration)
        && string.IsNullOrWhiteSpace(row.RefChequeNo)
        && row.Debit is null
        && row.Credit is null;

    private static void ValidateHeaders(List<string> headers)
    {
        var set = new HashSet<string>(headers.Where(h => !string.IsNullOrWhiteSpace(h)), StringComparer.OrdinalIgnoreCase);
        var missing = ExpectedHeaders.Where(h => !set.Contains(h)).ToList();
        if (missing.Count == 0) return;

        throw new InvalidOperationException(
            "CSV headers must match the bank import format. Missing: "
            + string.Join(", ", missing)
            + ". Expected: "
            + string.Join(", ", ExpectedHeaders)
            + ". Or upload a raw bank statement (PDF/CSV/Excel) via Convert.");
    }

    private static string NormalizeHeader(string header)
    {
        var h = header.Trim().Trim('"');
        if (h.Equals("Narration", StringComparison.OrdinalIgnoreCase))
            return "Narration";
        return h;
    }

    private static decimal? ParseAmount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var cleaned = raw.Trim()
            .Replace(",", "")
            .Replace("₹", "")
            .Replace("Rs.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("DR", "", StringComparison.OrdinalIgnoreCase)
            .Replace("CR", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        if (cleaned is "-" or "—" or ".") return null;
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.GetCultureInfo("en-IN"), out value))
            return value;
        return null;
    }

    private static decimal ParseInr(string raw) =>
        decimal.Parse(raw.Replace(",", ""), CultureInfo.InvariantCulture);

    private static string FormatAmount(decimal? value) =>
        value is null ? "" : value.Value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string EscapeCsv(string? value)
    {
        var s = value ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(ch);
        }

        list.Add(sb.ToString());
        return list;
    }
}

public sealed class BankStatementParseResult
{
    public string FileName { get; set; } = "";
    public int RowCount { get; set; }
    public List<BankStatementRowDto> Rows { get; set; } = [];
    public string? SourceKind { get; set; }
    public string? ErpBankLedgerName { get; set; }
    public string? Message { get; set; }
}

public sealed class BankStatementRowDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ErpBankLedgerName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string ValueDate { get; set; } = "";
    public string Narration { get; set; } = "";
    public string RefChequeNo { get; set; } = "";
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public int? SourceLine { get; set; }
    public string TransactionType { get; set; } = "";
    public string MainCategory { get; set; } = "";
    public string SubCategory { get; set; } = "";
}

public sealed class BankStatementExportRequest
{
    [JsonPropertyName("rows")]
    public List<BankStatementRowDto> Rows { get; set; } = [];

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("includeCategorization")]
    public bool IncludeCategorization { get; set; }
}
