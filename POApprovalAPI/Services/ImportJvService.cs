using System.Data;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;

namespace POApprovalAPI.Services;

/// <summary>
/// Import JV from the ERP Excel layout, with TDS credit columns.
/// A TDS column header starts with "TDS". Amounts there are credited to that
/// ledger; the same figure is debited to the row's Credit Ledger party, with a
/// second New Ref on the invoice.
/// </summary>
public sealed class ImportJvService
{
    private const decimal BalanceTolerance = 0.05m;
    private const int MaxRows = 500;
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private static readonly string[] DateFormats =
    [
        "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
        "dd-MM-yy", "dd/MM/yy", "yyyy-MM-dd", "MM/dd/yyyy",
    ];

    private readonly DatabaseService _database;

    public ImportJvService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<ImportJvUnitOption>> GetUnitsAsync(string group)
    {
        var name = (group ?? "").Trim();
        if (name.Length == 0)
            throw new ArgumentException("Choose a group.");

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<ImportJvUnitOption>(
            """
            SELECT LTRIM(RTRIM(Name)) AS Name, SrNo AS CompanyId
            FROM FactoryInfo WITH (NOLOCK)
            WHERE LTRIM(RTRIM(GroupName)) = @Group
              AND ISNULL(LTRIM(RTRIM(Name)), '') <> ''
            ORDER BY Name
            """,
            new { Group = name });
        var list = rows.ToList();
        if (list.Count == 0)
            throw new ArgumentException("That group has no units.");
        return list;
    }

    public byte[] BuildTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import JV");
        var headers = new[]
        {
            "Credit Ledger", "Invoice number", "Invoice Date", "Invoice Value",
            "Interest Expense - Factoring", "Bank Charges- Others",
            "IGST Input", "CGST Input", "SGST Input",
            "TDS -194A (Interest)", "TDS -194C (Contractor)", "TDS -194H (Commission)",
            "TDS -194I (Rent)", "TDS -194J (Professional)", "TDS -194Q (Goods)",
            "Entry Date",
        };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        for (var c = 1; c <= 4; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
        for (var c = 5; c <= 9; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
        for (var c = 10; c <= 15; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        sheet.Cell(1, 16).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");

        sheet.Cell(2, 1).Value = "Alonso Logistics India Pvt Ltd";
        sheet.Cell(2, 2).Value = "EXMUMDN0926/1791";
        sheet.Cell(2, 3).Value = "01-09-2026";
        sheet.Cell(2, 4).Value = 4720;
        sheet.Cell(2, 5).Value = 4000;
        sheet.Cell(2, 7).Value = 720;
        sheet.Cell(2, 11).Value = 80;
        sheet.Cell(2, 16).Value = "01-09-2026";

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, 20, 14, 42);

        var notes = workbook.AddWorksheet("How to fill");
        notes.Cell(1, 1).Value = "What the accountant fills";
        notes.Cell(1, 1).Style.Font.Bold = true;
        var lines = new[]
        {
            "One Excel row = one journal. Rename debit columns to the real expense ledgers. Leave unused columns blank.",
            "Blue headers: who is paid and the bill. Yellow: expense / GST (these are DEBITS). Green: TDS section (these are CREDITS).",
            "Credit Ledger = party name exactly as in ERP (the vendor). Invoice Value = full bill including GST, before TDS.",
            "Put the TDS amount only in the matching green column. 194C freight, 194J professional, 194I rent, 194A interest, 194H commission, 194Q goods.",
            "Do not type the party debit for TDS and do not add TDS Payable. The import creates: Dr party (TDS amount) and Cr TDS -194… itself.",
            "Blank TDS cells = no TDS on that row (bank reverse rows stay as they are).",
            "Worked example row 2: expense 4000 + IGST 720 = bill 4720, TDS 194C 80 (2% of 4000).",
            "That posts as: Dr expense 4000, Dr IGST 720, Cr party 4720, Dr party 80, Cr TDS -194C 80. Party outstanding becomes 4640.",
            "Stored in Journal (one line per ledger), JournalItem (two New Ref on the same invoice: 4720 and 80), AccountBills if the bill is new.",
            "In ERP journal / ledger, click the amount to open the voucher. TDS 94C sheet picks up the 80 credit on TDS -194C (Contractor).",
        };
        for (var i = 0; i < lines.Length; i++)
            notes.Cell(i + 2, 1).Value = lines[i];
        notes.Column(1).Width = 140;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportJvPreview> PreviewAsync(string group, string company, Stream file, string fileName)
    {
        var parsed = ParseFile(file, fileName);
        await AttachContextAsync(group, company, parsed);
        return parsed;
    }

    public async Task<ImportJvCommitResult> CommitAsync(string group, string company, string username, Stream file, string fileName)
    {
        var user = (username ?? "").Trim();
        if (user.Length == 0)
            throw new ArgumentException("Sign in before posting.");
        if (user.Length > 50)
            user = user[..50];

        var preview = await PreviewAsync(group, company, file, fileName);
        if (preview.Vouchers.Count == 0)
            throw new ArgumentException("The file has no journal rows.");
        if (preview.ErrorCount > 0)
            throw new ArgumentException($"Fix {preview.ErrorCount} row error(s) before posting.");

        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();
        try
        {
            var posted = new List<int>();
            var nextByYear = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var voucher in preview.Vouchers)
            {
                var yr = YearFrom(voucher.EntryDate);
                if (!nextByYear.TryGetValue(yr, out var nextNo))
                {
                    nextNo = await connection.ExecuteScalarAsync<int>(
                        """
                        SELECT ISNULL(MAX(JournalNo), 0) + 1
                        FROM Journal WITH (UPDLOCK, HOLDLOCK)
                        WHERE CompanyName = @Company AND yr = @Yr
                        """,
                        new { Company = preview.Company, Yr = yr },
                        tx);
                    nextByYear[yr] = nextNo;
                }

                var journalNo = voucher.RequestedJournalNo ?? nextNo;
                if (journalNo < nextNo && voucher.RequestedJournalNo is null)
                    journalNo = nextNo;

                var exists = await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*) FROM Journal WITH (UPDLOCK, HOLDLOCK)
                    WHERE CompanyName = @Company AND yr = @Yr AND JournalNo = @No
                    """,
                    new { Company = preview.Company, Yr = yr, No = journalNo },
                    tx);
                if (exists > 0)
                    throw new InvalidOperationException($"Journal {journalNo} already exists for {preview.Company} in {yr}.");

                await InsertVoucherAsync(connection, tx, preview, voucher, journalNo, yr, user);
                posted.Add(journalNo);
                nextByYear[yr] = Math.Max(nextByYear[yr], journalNo + 1);
            }

            tx.Commit();
            return new ImportJvCommitResult
            {
                Posted = posted.Count,
                JournalNos = posted,
                Company = preview.Company,
                Year = posted.Count == 0 ? "" : YearFrom(preview.Vouchers[0].EntryDate),
                Message = $"Posted {posted.Count} journal{(posted.Count == 1 ? "" : "s")} {posted.First()}–{posted.Last()} in {preview.Company}.",
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private async Task AttachContextAsync(string group, string company, ImportJvPreview preview)
    {
        var groupName = (group ?? "").Trim();
        var companyName = (company ?? "").Trim();
        if (groupName.Length == 0) throw new ArgumentException("Choose a group.");
        if (companyName.Length == 0) throw new ArgumentException("Choose a company.");

        using var connection = _database.CreateConnection();
        var unit = await connection.QueryFirstOrDefaultAsync<ImportJvUnitOption>(
            """
            SELECT LTRIM(RTRIM(Name)) AS Name, SrNo AS CompanyId
            FROM FactoryInfo WITH (NOLOCK)
            WHERE LTRIM(RTRIM(Name)) = @Company
              AND LTRIM(RTRIM(GroupName)) = @Group
            """,
            new { Company = companyName, Group = groupName });
        if (unit is null || unit.CompanyId == 0)
            throw new ArgumentException("That company is not in the selected group.");

        preview.Group = groupName;
        preview.Company = unit.Name;
        preview.CompanyId = unit.CompanyId;

        var names = preview.Vouchers
            .SelectMany(v => v.Lines.Select(l => l.Ledger))
            .Concat(preview.Vouchers.Select(v => v.CreditLedger))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ledgers = await LoadLedgersAsync(connection, unit.Name, names);

        foreach (var voucher in preview.Vouchers)
        {
            foreach (var line in voucher.Lines)
            {
                if (!ledgers.TryGetValue(line.Ledger, out var found))
                {
                    voucher.Errors.Add($"Ledger not in {unit.Name}: {line.Ledger}");
                    continue;
                }
                line.Ledger = found.Name;
                line.LedgerId = found.Id;
            }

            if (ledgers.TryGetValue(voucher.CreditLedger, out var party))
                voucher.CreditLedger = party.Name;

            var largestDr = voucher.Lines.Where(l => l.Debit > 0).OrderByDescending(l => l.Debit).FirstOrDefault();
            var largestCr = voucher.Lines.Where(l => l.Credit > 0).OrderByDescending(l => l.Credit).FirstOrDefault();
            voucher.DrPartyName = largestDr?.Ledger ?? "";
            voucher.CrPartyName = largestCr?.Ledger ?? "";
            voucher.DrPartyId = largestDr?.LedgerId ?? 0;
            voucher.CrPartyId = largestCr?.LedgerId ?? 0;

            var debit = voucher.Lines.Sum(l => l.Debit);
            var credit = voucher.Lines.Sum(l => l.Credit);
            if (Math.Abs(debit - credit) > BalanceTolerance)
                voucher.Errors.Add($"Debit {debit:0.00} does not equal credit {credit:0.00}.");

            voucher.Errors = voucher.Errors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            voucher.Warnings = voucher.Warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        preview.OkCount = preview.Vouchers.Count(v => v.Errors.Count == 0);
        preview.ErrorCount = preview.Vouchers.Count(v => v.Errors.Count > 0);
        preview.TdsCount = preview.Vouchers.Count(v => v.TdsTotal > 0);
        preview.CanPost = preview.Vouchers.Count > 0 && preview.ErrorCount == 0;
    }

    private static async Task<Dictionary<string, LedgerHit>> LoadLedgersAsync(
        IDbConnection connection, string company, List<string> names)
    {
        var map = new Dictionary<string, LedgerHit>(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) return map;

        var rows = await connection.QueryAsync<LedgerHit>(
            """
            SELECT LTRIM(RTRIM(LedgerName)) AS Name, srno AS Id, LTRIM(RTRIM(CompanyName)) AS Company
            FROM LedgerMaster WITH (NOLOCK)
            WHERE LTRIM(RTRIM(LedgerName)) IN @Names
            """,
            new { Names = names });

        foreach (var row in rows)
        {
            if (!map.TryGetValue(row.Name, out var current))
            {
                map[row.Name] = row;
                continue;
            }
            var preferNew = string.Equals(row.Company, company, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(current.Company, company, StringComparison.OrdinalIgnoreCase);
            if (preferNew) map[row.Name] = row;
        }
        return map;
    }

    private static async Task InsertVoucherAsync(
        IDbConnection connection,
        IDbTransaction tx,
        ImportJvPreview preview,
        ImportJvVoucher voucher,
        int journalNo,
        string yr,
        string username)
    {
        foreach (var line in voucher.Lines)
        {
            var transId = await connection.ExecuteScalarAsync<int>(
                """
                INSERT INTO Journal (
                    JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit,
                    Narration, UserName, narrationFinal, JournalType, IsTDS, MRNo, InwardNo,
                    DrPartyName, CrPartyName, yr, Currency, ExchangeRate, billDate, paymentTerms,
                    TDSAccessableAmount, RecordLogId, SubLedgerName, companyId, PartyId, DrPartyID, CrPartyID)
                VALUES (
                    @JournalNo, @Date, @Company, @Type, @PartyName, @Debit, @Credit,
                    @Narration, @UserName, @NarrationFinal, 'Journal', 'no', '', '',
                    @DrPartyName, @CrPartyName, @Yr, 'Rs.', 1, @BillDate, '',
                    @TdsBase, 0, '', @CompanyId, @PartyId, @DrPartyId, @CrPartyId);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    JournalNo = journalNo,
                    Date = voucher.EntryDate.Date,
                    Company = preview.Company,
                    Type = line.Type,
                    PartyName = line.Ledger,
                    Debit = (double)line.Debit,
                    Credit = (double)line.Credit,
                    Narration = line.Remark ?? "",
                    UserName = username,
                    NarrationFinal = voucher.InvoiceNo,
                    DrPartyName = voucher.DrPartyName,
                    CrPartyName = voucher.CrPartyName,
                    Yr = yr,
                    BillDate = voucher.InvoiceDate == default ? voucher.EntryDate.Date : voucher.InvoiceDate.Date,
                    TdsBase = (double)voucher.TdsAccessableAmount,
                    CompanyId = preview.CompanyId,
                    PartyId = line.LedgerId,
                    DrPartyId = voucher.DrPartyId,
                    CrPartyId = voucher.CrPartyId,
                },
                tx);

            if (string.IsNullOrWhiteSpace(line.BillNo) || line.BillAmount <= 0)
                continue;

            var billDate = line.BillDate == default ? voucher.InvoiceDate : line.BillDate;
            if (billDate == default) billDate = voucher.EntryDate;

            var exists = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*) FROM AccountBills WITH (NOLOCK)
                WHERE CompanyName = @Company AND LedgerName = @Ledger AND BillNo = @BillNo
                """,
                new { Company = preview.Company, Ledger = line.Ledger, BillNo = line.BillNo },
                tx);
            if (exists == 0)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO AccountBills (CompanyName, LedgerName, BillNo, BillDate, DueDate, BillAmount, BillCurrency, IsClosed, companyId, LedgerId)
                    VALUES (@Company, @Ledger, @BillNo, @BillDate, @BillDate, @BillAmount, 'Rs.', 0, @CompanyId, @LedgerId)
                    """,
                    new
                    {
                        Company = preview.Company,
                        Ledger = line.Ledger,
                        BillNo = line.BillNo,
                        BillDate = billDate.Date,
                        BillAmount = (double)line.BillAmount,
                        CompanyId = preview.CompanyId,
                        LedgerId = line.LedgerId,
                    },
                    tx);
            }

            await connection.ExecuteAsync(
                """
                INSERT INTO JournalItem (
                    JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit,
                    SelectionType, BillNo, BillDate, PaidAmount, BillAmount, UserName, yr,
                    journaltransid, companyid, partyid)
                VALUES (
                    @JournalNo, @Date, @Company, @Type, @PartyName, @Debit, @Credit,
                    'New Ref', @BillNo, @BillDate, @PaidAmount, @BillAmount, @UserName, @Yr,
                    @TransId, @CompanyId, @PartyId)
                """,
                new
                {
                    JournalNo = journalNo,
                    Date = voucher.EntryDate.Date,
                    Company = preview.Company,
                    Type = line.Type,
                    PartyName = line.Ledger,
                    Debit = (double)line.Debit,
                    Credit = (double)line.Credit,
                    BillNo = line.BillNo,
                    BillDate = billDate.Date,
                    PaidAmount = (double)line.BillAmount,
                    BillAmount = (double)line.BillAmount,
                    UserName = username,
                    Yr = yr,
                    TransId = transId,
                    CompanyId = preview.CompanyId,
                    PartyId = line.LedgerId,
                },
                tx);
        }
    }

    private ImportJvPreview ParseFile(Stream file, string fileName)
    {
        var name = fileName ?? "import.xlsx";
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;
        var table = name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ReadCsv(ms)
            : ReadExcel(ms);

        if (table.Headers.Count == 0)
            throw new ArgumentException("The first row must be column headers.");

        var map = MapHeaders(table.Headers);
        if (map.CreditLedger < 0)
            throw new ArgumentException("The sheet needs a Credit Ledger column.");
        if (map.InvoiceNo < 0 || map.InvoiceDate < 0 || map.InvoiceValue < 0)
            throw new ArgumentException("The sheet needs Invoice number, Invoice Date and Invoice Value columns.");

        var preview = new ImportJvPreview
        {
            FileName = Path.GetFileName(name),
            DebitColumns = map.Debits.Select(d => table.Headers[d]).ToList(),
            TdsColumns = map.Tds.Select(d => table.Headers[d]).ToList(),
        };

        var rowNumber = 1;
        foreach (var cells in table.Rows)
        {
            rowNumber++;
            var voucher = ParseRow(rowNumber, cells, table.Headers, map);
            if (voucher is null) continue;
            preview.Vouchers.Add(voucher);
            if (preview.Vouchers.Count > MaxRows)
                throw new ArgumentException($"The file has more than {MaxRows} journals.");
        }

        if (preview.Vouchers.Count == 0)
            throw new ArgumentException("No data rows were found.");
        return preview;
    }

    private ImportJvVoucher? ParseRow(int rowNumber, List<string> cells, List<string> headers, HeaderMap map)
    {
        string Cell(int index) => index >= 0 && index < cells.Count ? cells[index].Trim() : "";

        var creditLedger = Cell(map.CreditLedger);
        var invoiceNo = Cell(map.InvoiceNo);
        if (creditLedger.Length == 0 && invoiceNo.Length == 0 && ParseAmount(Cell(map.InvoiceValue)) == 0)
            return null;

        var voucher = new ImportJvVoucher
        {
            Row = rowNumber,
            CreditLedger = creditLedger,
            InvoiceNo = invoiceNo,
        };

        if (creditLedger.Length == 0)
            voucher.Errors.Add("Credit Ledger is empty.");

        if (!TryParseDate(Cell(map.InvoiceDate), out var invoiceDate))
            voucher.Errors.Add("Invoice Date is not a date (use dd-MM-yyyy).");
        else
            voucher.InvoiceDate = invoiceDate;

        var entryRaw = map.EntryDate >= 0 ? Cell(map.EntryDate) : "";
        if (entryRaw.Length == 0)
        {
            voucher.EntryDate = voucher.InvoiceDate;
            if (voucher.InvoiceDate != default)
                voucher.Warnings.Add("Entry Date was empty, so Invoice Date was used.");
        }
        else if (!TryParseDate(entryRaw, out var entryDate))
        {
            voucher.EntryDate = voucher.InvoiceDate;
            voucher.Warnings.Add($"Entry Date '{entryRaw}' is not a valid date; Invoice Date was used.");
        }
        else
        {
            voucher.EntryDate = entryDate;
        }

        voucher.InvoiceValue = ParseAmount(Cell(map.InvoiceValue));
        if (voucher.InvoiceValue < 0)
            voucher.Errors.Add("Invoice Value cannot be negative.");

        if (map.JournalNo >= 0)
        {
            var raw = Cell(map.JournalNo);
            if (raw.Length > 0 && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var no) && no > 0)
                voucher.RequestedJournalNo = no;
        }

        decimal debitTotal = 0;
        foreach (var col in map.Debits)
        {
            var amount = ParseAmount(Cell(col));
            if (amount == 0) continue;
            if (amount < 0)
            {
                voucher.Errors.Add($"{headers[col]} is negative.");
                continue;
            }
            debitTotal += amount;
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "DR",
                Ledger = headers[col],
                Debit = amount,
            });
        }

        if (voucher.InvoiceValue == 0 && debitTotal > 0)
        {
            voucher.InvoiceValue = debitTotal;
            voucher.Warnings.Add("Invoice Value was empty, so debit total was used.");
        }

        if (voucher.InvoiceValue > 0 && Math.Abs(debitTotal - voucher.InvoiceValue) > BalanceTolerance)
            voucher.Errors.Add($"Debit columns {debitTotal:0.00} do not equal Invoice Value {voucher.InvoiceValue:0.00}.");

        if (voucher.InvoiceValue > 0 && creditLedger.Length > 0)
        {
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "CR",
                Ledger = creditLedger,
                Credit = voucher.InvoiceValue,
                BillNo = invoiceNo,
                BillDate = voucher.InvoiceDate,
                BillAmount = voucher.InvoiceValue,
            });
        }

        decimal tdsTotal = 0;
        foreach (var col in map.Tds)
        {
            var amount = ParseAmount(Cell(col));
            if (amount == 0) continue;
            if (amount < 0)
            {
                voucher.Errors.Add($"{headers[col]} is negative.");
                continue;
            }
            var tdsLedger = headers[col];
            if (tdsLedger.StartsWith("TDS Payable", StringComparison.OrdinalIgnoreCase))
                voucher.Warnings.Add($"{tdsLedger} is a transfer ledger. Deduction at booking uses TDS -194…");

            tdsTotal += amount;
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "DR",
                Ledger = creditLedger,
                Debit = amount,
                IsPartyTds = true,
                BillNo = invoiceNo,
                BillDate = voucher.InvoiceDate,
                BillAmount = amount,
            });
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "CR",
                Ledger = tdsLedger,
                Credit = amount,
                IsTds = true,
            });
        }

        voucher.TdsTotal = tdsTotal;
        voucher.TdsAccessableAmount = tdsTotal <= 0
            ? 0
            : voucher.Lines
                .Where(l => l.Debit > 0 && !l.IsPartyTds && !IsGstOrRoundOff(l.Ledger))
                .Sum(l => l.Debit);

        if (voucher.Lines.Count == 0 && voucher.Errors.Count == 0)
            return null;

        return voucher;
    }

    private static HeaderMap MapHeaders(List<string> headers)
    {
        var map = new HeaderMap();
        for (var i = 0; i < headers.Count; i++)
        {
            var key = NormalizeHeader(headers[i]);
            if (key.Length == 0)
            {
                if (map.JournalNo < 0 && i == headers.Count - 1)
                    map.JournalNo = i;
                continue;
            }
            if (key is "creditledger" or "credit") map.CreditLedger = i;
            else if (key is "invoicenumber" or "invoiceno" or "billno" or "billnumber") map.InvoiceNo = i;
            else if (key is "invoicedate" or "billdate") map.InvoiceDate = i;
            else if (key is "invoicevalue" or "invoiceamount" or "amount") map.InvoiceValue = i;
            else if (key is "entrydate" or "voucherdate" or "date") map.EntryDate = i;
            else if (key is "journalno" or "journalnumber" or "voucherno") map.JournalNo = i;
            else if (headers[i].TrimStart().StartsWith("TDS", StringComparison.OrdinalIgnoreCase))
                map.Tds.Add(i);
            else
                map.Debits.Add(i);
        }
        return map;
    }

    private static SheetTable ReadExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
                        !string.Equals(w.Name, "How to fill", StringComparison.OrdinalIgnoreCase))
                    ?? workbook.Worksheets.First();
        var range = sheet.RangeUsed();
        if (range is null) throw new ArgumentException("The Excel sheet is empty.");

        var lastCol = range.LastColumn().ColumnNumber();
        var lastRow = range.LastRow().RowNumber();
        var headers = new List<string>();
        for (var c = 1; c <= lastCol; c++)
            headers.Add(CellText(sheet.Cell(1, c)).Trim());

        var rows = new List<List<string>>();
        for (var r = 2; r <= lastRow; r++)
        {
            var row = new List<string>(headers.Count);
            var any = false;
            for (var c = 1; c <= headers.Count; c++)
            {
                var text = CellText(sheet.Cell(r, c));
                if (text.Length > 0) any = true;
                row.Add(text);
            }
            if (any) rows.Add(row);
        }
        return new SheetTable(headers, rows);
    }

    private static SheetTable ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
            lines.Add(line);
        if (lines.Count == 0) throw new ArgumentException("The CSV is empty.");

        var headers = SplitCsv(lines[0]).Select(h => h.Trim()).ToList();
        var rows = new List<List<string>>();
        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var row = SplitCsv(lines[i]);
            while (row.Count < headers.Count) row.Add("");
            if (row.TrueForAll(string.IsNullOrWhiteSpace)) continue;
            rows.Add(row);
        }
        return new SheetTable(headers, rows);
    }

    private static List<string> SplitCsv(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (ch == '"') quoted = false;
                else current.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',')
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(ch);
        }
        result.Add(current.ToString());
        return result;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        if (cell.DataType == XLDataType.Number)
        {
            var number = cell.GetDouble();
            if (number > 20000 && number < 80000 && Math.Abs(number - Math.Round(number)) < 0.0001)
            {
                try { return DateTime.FromOADate(number).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture); }
                catch { /* not a date */ }
            }
            return number.ToString("0.##########", CultureInfo.InvariantCulture);
        }
        return cell.GetString().Trim();
    }

    private static bool TryParseDate(string raw, out DateTime date)
    {
        date = default;
        var text = (raw ?? "").Trim();
        if (text.Length == 0) return false;
        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date.Day > 0;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa) && oa > 20000 && oa < 80000)
        {
            try
            {
                date = DateTime.FromOADate(oa);
                return true;
            }
            catch { return false; }
        }
        return false;
    }

    private static decimal ParseAmount(string raw)
    {
        var text = (raw ?? "").Trim().Replace(",", "");
        if (text.Length == 0 || text == "-") return 0;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (decimal.TryParse(text, NumberStyles.Number, In, out value))
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return 0;
    }

    private static string NormalizeHeader(string header) =>
        new string((header ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static bool IsGstOrRoundOff(string ledger)
    {
        var name = ledger ?? "";
        return name.Contains("GST Input", StringComparison.OrdinalIgnoreCase)
               || name.Contains("GST INPUT", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("IGST", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("CGST", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("SGST", StringComparison.OrdinalIgnoreCase)
               || name.Contains("Round Off", StringComparison.OrdinalIgnoreCase);
    }

    private static string YearFrom(DateTime date)
    {
        var start = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{start % 100:00}-{(start + 1) % 100:00}";
    }

    private sealed class HeaderMap
    {
        public int CreditLedger = -1;
        public int InvoiceNo = -1;
        public int InvoiceDate = -1;
        public int InvoiceValue = -1;
        public int EntryDate = -1;
        public int JournalNo = -1;
        public List<int> Debits { get; } = [];
        public List<int> Tds { get; } = [];
    }

    private sealed record SheetTable(List<string> Headers, List<List<string>> Rows);

    private sealed class LedgerHit
    {
        public string Name { get; set; } = "";
        public int Id { get; set; }
        public string Company { get; set; } = "";
    }
}

public sealed class ImportJvUnitOption
{
    public string Name { get; set; } = "";
    public int CompanyId { get; set; }
}

public sealed class ImportJvPreview
{
    public string Group { get; set; } = "";
    public string Company { get; set; } = "";
    public int CompanyId { get; set; }
    public string FileName { get; set; } = "";
    public List<string> DebitColumns { get; set; } = [];
    public List<string> TdsColumns { get; set; } = [];
    public List<ImportJvVoucher> Vouchers { get; set; } = [];
    public int OkCount { get; set; }
    public int ErrorCount { get; set; }
    public int TdsCount { get; set; }
    public bool CanPost { get; set; }
}

public sealed class ImportJvVoucher
{
    public int Row { get; set; }
    public string CreditLedger { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime EntryDate { get; set; }
    public decimal InvoiceValue { get; set; }
    public decimal TdsTotal { get; set; }
    public decimal TdsAccessableAmount { get; set; }
    public int? RequestedJournalNo { get; set; }
    public string DrPartyName { get; set; } = "";
    public string CrPartyName { get; set; } = "";
    public int DrPartyId { get; set; }
    public int CrPartyId { get; set; }
    public List<ImportJvLine> Lines { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class ImportJvLine
{
    public string Type { get; set; } = "";
    public string Ledger { get; set; } = "";
    public int LedgerId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public bool IsTds { get; set; }
    public bool IsPartyTds { get; set; }
    public string? BillNo { get; set; }
    public DateTime BillDate { get; set; }
    public decimal BillAmount { get; set; }
    public string? Remark { get; set; }
}

public sealed class ImportJvCommitResult
{
    public int Posted { get; set; }
    public List<int> JournalNos { get; set; } = [];
    public string Company { get; set; } = "";
    public string Year { get; set; } = "";
    public string Message { get; set; } = "";
}
