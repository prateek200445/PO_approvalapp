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

    public async Task<byte[]> BuildTemplateAsync(string group, string company)
    {
        var groupName = (group ?? "").Trim();
        var companyName = (company ?? "").Trim();
        if (groupName.Length == 0) throw new ArgumentException("Choose a group.");
        if (companyName.Length == 0) throw new ArgumentException("Choose a company, then download the template so ledger dropdowns are for that unit.");

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

        var ledgers = (await connection.QueryAsync<string>(
            """
            SELECT DISTINCT LTRIM(RTRIM(LedgerName))
            FROM LedgerMaster WITH (NOLOCK)
            WHERE LTRIM(RTRIM(CompanyName)) = @Company
              AND ISNULL(LTRIM(RTRIM(LedgerName)), '') <> ''
            ORDER BY 1
            """,
            new { Company = unit.Name })).ToList();

        using var workbook = new XLWorkbook();
        var list = workbook.AddWorksheet("Ledgers");
        if (ledgers.Count == 0)
            list.Cell(1, 1).Value = "(no ledgers)";
        else
        {
            for (var i = 0; i < ledgers.Count; i++)
                list.Cell(i + 1, 1).Value = ledgers[i];
        }
        list.Column(1).Width = 48;
        list.Hide();

        var sheet = workbook.AddWorksheet("Import JV");
        var headers = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            headers.Add($"Credit Ledger {i}");
            headers.Add($"Credit Amount {i}");
        }
        for (var i = 1; i <= 5; i++)
        {
            headers.Add($"Debit Ledger {i}");
            headers.Add($"Debit Amount {i}");
        }
        headers.AddRange(["Invoice number", "Invoice Date", "Invoice Value", "Entry Date", "Narration",
            "Credit Total", "Debit Total", "Status"]);

        for (var i = 0; i < headers.Count; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Alignment.WrapText = true;
        sheet.Row(1).Height = 30;
        for (var c = 1; c <= 10; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        for (var c = 11; c <= 20; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
        for (var c = 21; c <= 25; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
        for (var c = 26; c <= 28; c++)
            sheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");

        const int lastData = 201;
        var listRef = $"Ledgers!$A$1:$A${Math.Max(ledgers.Count, 1)}";
        var ledgerCols = new[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
        foreach (var col in ledgerCols)
        {
            var dv = sheet.Range(2, col, lastData, col).CreateDataValidation();
            dv.IgnoreBlanks = true;
            dv.InCellDropdown = true;
            dv.AllowedValues = XLAllowedValues.List;
            dv.List(listRef, true);
            dv.ShowErrorMessage = true;
            dv.ErrorStyle = XLErrorStyle.Stop;
            dv.ErrorTitle = "Ledger";
            dv.ErrorMessage = "That ledger is not in ERP for this company. Pick a name from the dropdown.";
        }

        for (var r = 2; r <= lastData; r++)
        {
            sheet.Cell(r, 26).FormulaA1 = $"B{r}+D{r}+F{r}+H{r}+J{r}";
            sheet.Cell(r, 27).FormulaA1 = $"L{r}+N{r}+P{r}+R{r}+T{r}";
            sheet.Cell(r, 28).FormulaA1 =
                $"IF(AND(Z{r}=0,AA{r}=0),\"\",IF(ROUND(ABS(Z{r}-AA{r}),2)<=0.05,\"OK\",\"Amounts don't reconcile\"))";
            sheet.Cell(r, 26).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(r, 27).Style.NumberFormat.Format = "#,##0.00";
        }

        var statusRange = sheet.Range(2, 28, lastData, 28);
        var red = statusRange.AddConditionalFormat().WhenEquals("Amounts don't reconcile");
        red.Fill.SetBackgroundColor(XLColor.FromHtml("#FFC7CE"));
        red.Font.SetFontColor(XLColor.FromHtml("#9C0006"));
        var ok = statusRange.AddConditionalFormat().WhenEquals("OK");
        ok.Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE"));
        ok.Font.SetFontColor(XLColor.FromHtml("#006100"));

        sheet.Cell(2, 1).Value = "Alonso Logistics India Pvt Ltd";
        sheet.Cell(2, 2).Value = 4720;
        sheet.Cell(2, 3).Value = "TDS -194C (Contractor)";
        sheet.Cell(2, 4).Value = 80;
        sheet.Cell(2, 11).Value = "Interest Expense - Factoring";
        sheet.Cell(2, 12).Value = 4000;
        sheet.Cell(2, 13).Value = "IGST Input";
        sheet.Cell(2, 14).Value = 720;
        sheet.Cell(2, 15).Value = "Alonso Logistics India Pvt Ltd";
        sheet.Cell(2, 16).Value = 80;
        sheet.Cell(2, 21).Value = "EXMUMDN0926/1791";
        sheet.Cell(2, 22).Value = "01-09-2026";
        sheet.Cell(2, 23).Value = 4720;
        sheet.Cell(2, 24).Value = "01-09-2026";
        sheet.Cell(2, 25).Value = "Import JV with TDS";

        sheet.SheetView.FreezeRows(1);
        sheet.SheetView.FreezeColumns(2);
        for (var c = 1; c <= 28; c++)
            sheet.Column(c).Width = c % 2 == 1 && c <= 20 ? 28 : 14;
        sheet.Column(21).Width = 22;
        sheet.Column(25).Width = 28;
        sheet.Column(28).Width = 28;

        var notes = workbook.AddWorksheet("How to fill");
        notes.Cell(1, 1).Value = $"Import JV — {unit.Name}";
        notes.Cell(1, 1).Style.Font.Bold = true;
        var lines = new[]
        {
            "Choose this file only for the company named above. Ledger cells have a dropdown of that company's ledgers.",
            "One row = one journal. Up to 5 credits and 5 debits. Leave unused pairs blank.",
            "Green = credit (party, TDS -194C, bank). Yellow = debit (expense, GST, party TDS). Blue = invoice / date / narration.",
            "Orange Status must show OK. If it is red, Credit Total and Debit Total do not match — fix the amounts before upload. Import will also refuse the row.",
            "TDS: Credit Ledger 2 = TDS -194C (Contractor) (or 194J / 194I / …) and its amount. Debit the same party for that TDS amount so the row balances. If you skip the party debit, import still adds it, but Status stays red until you add it.",
            "Do not use TDS Payable here.",
            "Row 2 is the worked example: Cr Alonso 4720 + Cr TDS 80; Dr expense 4000 + Dr IGST 720 + Dr Alonso 80. Status = OK.",
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
                voucher.Errors.Add($"Amounts don't reconcile. Debit {debit:0.00} vs credit {credit:0.00}.");

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
                    NarrationFinal = string.IsNullOrWhiteSpace(voucher.Narration) ? voucher.InvoiceNo : voucher.Narration,
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
        if (map.PairLayout)
        {
            if (map.CreditLedgers[0] < 0 || map.DebitLedgers[0] < 0)
                throw new ArgumentException("The sheet needs Credit Ledger 1 / Credit Amount 1 and Debit Ledger 1 / Debit Amount 1.");
        }
        else
        {
            if (map.CreditLedger < 0)
                throw new ArgumentException("Use the new template (Credit Ledger 1, Debit Ledger 1, amounts) or the old Credit Ledger column.");
            if (map.InvoiceNo < 0 || map.InvoiceDate < 0 || map.InvoiceValue < 0)
                throw new ArgumentException("The sheet needs Invoice number, Invoice Date and Invoice Value columns.");
        }

        var preview = new ImportJvPreview
        {
            FileName = Path.GetFileName(name),
            DebitColumns = map.PairLayout
                ? Enumerable.Range(1, 5).Select(i => $"Debit Ledger {i}").ToList()
                : map.Debits.Select(d => table.Headers[d]).ToList(),
            TdsColumns = map.PairLayout
                ? Enumerable.Range(1, 5).Select(i => $"Credit Ledger {i}").ToList()
                : map.Tds.Select(d => table.Headers[d]).ToList(),
        };

        var rowNumber = 1;
        foreach (var cells in table.Rows)
        {
            rowNumber++;
            var voucher = map.PairLayout
                ? ParsePairRow(rowNumber, cells, map)
                : ParseRow(rowNumber, cells, table.Headers, map);
            if (voucher is null) continue;
            preview.Vouchers.Add(voucher);
            if (preview.Vouchers.Count > MaxRows)
                throw new ArgumentException($"The file has more than {MaxRows} journals.");
        }

        if (preview.Vouchers.Count == 0)
            throw new ArgumentException("No data rows were found.");
        return preview;
    }

    private ImportJvVoucher? ParsePairRow(int rowNumber, List<string> cells, HeaderMap map)
    {
        string Cell(int index) => index >= 0 && index < cells.Count ? cells[index].Trim() : "";

        var credits = new List<(string Ledger, decimal Amount)>();
        var debits = new List<(string Ledger, decimal Amount)>();
        for (var i = 0; i < 5; i++)
        {
            var cName = Cell(map.CreditLedgers[i]);
            var cAmt = ParseAmount(Cell(map.CreditAmounts[i]));
            if (cName.Length > 0 || cAmt != 0) credits.Add((cName, cAmt));
            var dName = Cell(map.DebitLedgers[i]);
            var dAmt = ParseAmount(Cell(map.DebitAmounts[i]));
            if (dName.Length > 0 || dAmt != 0) debits.Add((dName, dAmt));
        }

        var invoiceNo = Cell(map.InvoiceNo);
        var narration = Cell(map.Narration);
        if (credits.Count == 0 && debits.Count == 0 && invoiceNo.Length == 0 && narration.Length == 0)
            return null;

        var voucher = new ImportJvVoucher
        {
            Row = rowNumber,
            InvoiceNo = invoiceNo,
            Narration = narration,
        };

        foreach (var (ledger, amount) in credits)
        {
            if (ledger.Length == 0) voucher.Errors.Add("A credit amount has no ledger. Pick Credit Ledger from the dropdown.");
            else if (amount < 0) voucher.Errors.Add($"{ledger} credit is negative.");
            else if (amount == 0) voucher.Errors.Add($"{ledger} credit amount is empty.");
        }
        foreach (var (ledger, amount) in debits)
        {
            if (ledger.Length == 0) voucher.Errors.Add("A debit amount has no ledger. Pick Debit Ledger from the dropdown.");
            else if (amount < 0) voucher.Errors.Add($"{ledger} debit is negative.");
            else if (amount == 0) voucher.Errors.Add($"{ledger} debit amount is empty.");
        }

        var enteredCr = credits.Where(x => x.Ledger.Length > 0 && x.Amount > 0).Sum(x => x.Amount);
        var enteredDr = debits.Where(x => x.Ledger.Length > 0 && x.Amount > 0).Sum(x => x.Amount);

        if (!TryParseDate(Cell(map.InvoiceDate), out var invoiceDate))
        {
            if (Cell(map.InvoiceDate).Length > 0)
                voucher.Errors.Add("Invoice Date is not a date (use dd-MM-yyyy).");
        }
        else voucher.InvoiceDate = invoiceDate;

        var entryRaw = Cell(map.EntryDate);
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
        else voucher.EntryDate = entryDate;

        if (voucher.EntryDate == default && voucher.InvoiceDate == default)
            voucher.Errors.Add("Entry Date or Invoice Date is required.");

        voucher.InvoiceValue = ParseAmount(Cell(map.InvoiceValue));
        if (map.JournalNo >= 0)
        {
            var raw = Cell(map.JournalNo);
            if (raw.Length > 0 && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var no) && no > 0)
                voucher.RequestedJournalNo = no;
        }

        var party = credits.FirstOrDefault(x => x.Ledger.Length > 0 && !IsTdsSection(x.Ledger)).Ledger;
        voucher.CreditLedger = party;

        foreach (var (ledger, amount) in credits.Where(x => x.Ledger.Length > 0 && x.Amount > 0))
        {
            var tds = IsTdsSection(ledger);
            if (ledger.StartsWith("TDS Payable", StringComparison.OrdinalIgnoreCase))
                voucher.Warnings.Add($"{ledger} is a transfer ledger. Deduction at booking uses TDS -194…");
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "CR",
                Ledger = ledger,
                Credit = amount,
                IsTds = tds,
                BillNo = !tds && ledger.Equals(party, StringComparison.OrdinalIgnoreCase) ? invoiceNo : null,
                BillDate = voucher.InvoiceDate,
                BillAmount = !tds && ledger.Equals(party, StringComparison.OrdinalIgnoreCase) ? amount : 0,
            });
        }

        foreach (var (ledger, amount) in debits.Where(x => x.Ledger.Length > 0 && x.Amount > 0))
        {
            var partyTds = party.Length > 0 && ledger.Equals(party, StringComparison.OrdinalIgnoreCase);
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "DR",
                Ledger = ledger,
                Debit = amount,
                IsPartyTds = partyTds,
                BillNo = partyTds ? invoiceNo : null,
                BillDate = voucher.InvoiceDate,
                BillAmount = partyTds ? amount : 0,
            });
        }

        ApplyAutoTdsPartyDebit(voucher, invoiceNo);

        var tdsTotal = voucher.Lines.Where(l => l.IsTds).Sum(l => l.Credit);
        voucher.TdsTotal = tdsTotal;
        voucher.TdsAccessableAmount = tdsTotal <= 0
            ? 0
            : voucher.Lines.Where(l => l.Debit > 0 && !l.IsPartyTds && !IsGstOrRoundOff(l.Ledger)).Sum(l => l.Debit);

        if (voucher.InvoiceValue == 0)
        {
            var partyCredit = voucher.Lines.Where(l => l.Type == "CR" && !l.IsTds).Sum(l => l.Credit);
            if (partyCredit > 0) voucher.InvoiceValue = partyCredit;
        }

        if (Math.Abs(enteredDr - enteredCr) > BalanceTolerance)
        {
            var finalDr = voucher.Lines.Sum(l => l.Debit);
            var finalCr = voucher.Lines.Sum(l => l.Credit);
            if (Math.Abs(finalDr - finalCr) > BalanceTolerance)
                voucher.Errors.Add($"Amounts don't reconcile. Debit {enteredDr:0.00} vs credit {enteredCr:0.00}.");
            else
                voucher.Warnings.Add("Excel Status was red until the party TDS debit was added. The posted journal balances.");
        }

        if (voucher.Lines.Count == 0 && voucher.Errors.Count == 0)
            return null;
        return voucher;
    }

    private static void ApplyAutoTdsPartyDebit(ImportJvVoucher voucher, string invoiceNo)
    {
        var party = voucher.CreditLedger;
        if (string.IsNullOrWhiteSpace(party)) return;

        foreach (var tds in voucher.Lines.Where(l => l.IsTds && l.Credit > 0).ToList())
        {
            var already = voucher.Lines.Any(l =>
                l.IsPartyTds
                && l.Ledger.Equals(party, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(l.Debit - tds.Credit) <= BalanceTolerance);
            if (already) continue;
            voucher.Lines.Add(new ImportJvLine
            {
                Type = "DR",
                Ledger = party,
                Debit = tds.Credit,
                IsPartyTds = true,
                BillNo = invoiceNo,
                BillDate = voucher.InvoiceDate,
                BillAmount = tds.Credit,
            });
            voucher.Warnings.Add($"Added party debit {tds.Credit:0.00} on {party} for {tds.Ledger} so the journal balances.");
        }
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
        map.PairLayout = headers.Select(NormalizeHeader).Any(k => k is "creditledger1" or "debitledger1");
        for (var i = 0; i < headers.Count; i++)
        {
            var key = NormalizeHeader(headers[i]);
            if (key.Length == 0)
            {
                if (map.JournalNo < 0 && i == headers.Count - 1)
                    map.JournalNo = i;
                continue;
            }
            if (key is "credittotal" or "debittotal" or "status" or "difference" or "reconcile")
                continue;
            if (key is "narration" or "narrationfinal" or "remark") map.Narration = i;
            else if (TrySlot(key, "creditledger", out var cLedger)) map.CreditLedgers[cLedger] = i;
            else if (TrySlot(key, "creditamount", out var cAmt)) map.CreditAmounts[cAmt] = i;
            else if (TrySlot(key, "debitledger", out var dLedger)) map.DebitLedgers[dLedger] = i;
            else if (TrySlot(key, "debitamount", out var dAmt)) map.DebitAmounts[dAmt] = i;
            else if (key is "creditledger" or "credit") map.CreditLedger = i;
            else if (key is "invoicenumber" or "invoiceno" or "billno" or "billnumber") map.InvoiceNo = i;
            else if (key is "invoicedate" or "billdate") map.InvoiceDate = i;
            else if (key is "invoicevalue" or "invoiceamount") map.InvoiceValue = i;
            else if (key is "entrydate" or "voucherdate" or "date") map.EntryDate = i;
            else if (key is "journalno" or "journalnumber" or "voucherno") map.JournalNo = i;
            else if (headers[i].TrimStart().StartsWith("TDS", StringComparison.OrdinalIgnoreCase))
                map.Tds.Add(i);
            else if (!map.PairLayout)
                map.Debits.Add(i);
        }
        return map;
    }

    private static bool TrySlot(string key, string prefix, out int index)
    {
        index = 0;
        if (!key.StartsWith(prefix, StringComparison.Ordinal) || key.Length != prefix.Length + 1)
            return false;
        var n = key[^1] - '0';
        if (n is < 1 or > 5) return false;
        index = n - 1;
        return true;
    }

    private static SheetTable ReadExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
                        string.Equals(w.Name, "Import JV", StringComparison.OrdinalIgnoreCase))
                    ?? workbook.Worksheets.FirstOrDefault(w =>
                        !string.Equals(w.Name, "How to fill", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(w.Name, "Ledgers", StringComparison.OrdinalIgnoreCase))
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

    private static bool IsTdsSection(string ledger)
    {
        var name = (ledger ?? "").Trim();
        return name.StartsWith("TDS -", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("TDS-", StringComparison.OrdinalIgnoreCase);
    }

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
        public int Narration = -1;
        public bool PairLayout;
        public int[] CreditLedgers { get; } = [-1, -1, -1, -1, -1];
        public int[] CreditAmounts { get; } = [-1, -1, -1, -1, -1];
        public int[] DebitLedgers { get; } = [-1, -1, -1, -1, -1];
        public int[] DebitAmounts { get; } = [-1, -1, -1, -1, -1];
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
    public string Narration { get; set; } = "";
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
