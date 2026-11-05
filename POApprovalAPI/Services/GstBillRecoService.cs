using System.Data;
using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class GstBillRecoService
{
    private const int TimeoutSeconds = 300;
    private const decimal TaxEpsilon = 0.005m;

    private static readonly string[] FormatColumns =
    [
        "Companyname", "VoucherType", "VoucherNo", "VoucherDate", "RefNo", "Billdate", "Billno",
        "ledger", "Party", "GSTNo", "GrossAmount", "value", "TotalC", "OtherAll",
        "CGST INPUT", "SGST INPUT", "IGST INPUT",
        "FREIGHT INWARD EXPENSE (WITH GST)", "FREIGHT INWARD EXPENSE (WITHOUT GST)",
        "FREIGHT OUTWARD EXPENSE (WITH GST)", "GST CREDIT-NON ELIGIBLE",
        "VEHICLE HIRE CHARGES - WITH GST", "C & F  CHARGES - EXPORT", "OTHER ALL",
        "TCS", "TDS", "MONTH AS PER 2B", "2B PERIOD",
    ];

    private static readonly string[] LedgerColumns =
    [
        "FREIGHT INWARD EXPENSE (WITH GST)", "FREIGHT INWARD EXPENSE (WITHOUT GST)",
        "FREIGHT OUTWARD EXPENSE (WITH GST)", "GST CREDIT-NON ELIGIBLE",
        "VEHICLE HIRE CHARGES - WITH GST", "C & F  CHARGES - EXPORT", "OTHER ALL",
        "TCS", "TDS",
    ];

    private static int _billProcedureReady;

    private readonly DatabaseService _database;
    private readonly Gst2BSnapshotService _snapshots;

    public GstBillRecoService(DatabaseService database, Gst2BSnapshotService snapshots)
    {
        _database = database;
        _snapshots = snapshots;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync(CancellationToken ct = default)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT DISTINCT
    COALESCE(NULLIF(LTRIM(RTRIM(GroupName)), ''), LTRIM(RTRIM(Name))) AS GroupLabel
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(Name)), '') <> ''
ORDER BY GroupLabel";
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            names.Add(reader.GetString(0).Trim());
        return names;
    }

    public async Task<GstBillRecoResult> ReconcileAsync(
        Stream twoBFile,
        string companyName,
        DateTime dateFrom,
        DateTime dateTo,
        decimal amountTolerance,
        CancellationToken ct = default)
    {
        var company = (companyName ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");
        if (company.Length > 150)
            throw new ArgumentException("Company group name is too long.");
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (to < from)
            throw new ArgumentException("Date to is before date from.");
        if (amountTolerance < 0)
            amountTolerance = 0;

        TwoBParse parsed;
        int saved;
        if (Gst2BSnapshotService.TryRead(twoBFile, out var snapshot))
        {
            var stored = await _snapshots.SaveAsync(snapshot, "portal.xlsx", ct);
            parsed = FromSnapshot(snapshot);
            saved = stored.InsertedRows;
        }
        else
        {
            if (twoBFile.CanSeek)
                twoBFile.Position = 0;
            parsed = ParseTwoB(twoBFile);
            saved = -1;
        }
        if (parsed.Rows.Count == 0)
            throw new ArgumentException("No invoice, credit note, or debit note rows with GST were found. Use the Invoice Wise 2B (B2B, CN, DN) sheet.");

        var members = await LoadGroupMembersAsync(company, ct);
        if (members.Count == 0)
            throw new ArgumentException($"No ERP companies were found for group {company}.");

        var buyerGstin = NormGst(parsed.BuyerGstin);
        if (buyerGstin.Length == 0)
        {
            var groupGstins = members.Select(member => member.Gstin).Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (groupGstins.Count == 1)
            {
                buyerGstin = groupGstins[0];
                parsed.BuyerGstin = buyerGstin;
            }
        }
        if (buyerGstin.Length == 0)
            throw new ArgumentException("The 2B file has no buyer GSTIN, so its company group cannot be identified.");

        var matchingMembers = members
            .Where(member => string.Equals(member.Gstin, buyerGstin, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matchingMembers.Count == 0)
            throw new ArgumentException($"The uploaded 2B GSTIN {buyerGstin} does not belong to {company}.");

        if (saved < 0)
            saved = await SaveTwoBAsync(company, parsed, ct);
        var inRange = parsed.Rows.Where(row => InRange(row.BillDate, from, to)).ToList();
        if (inRange.Count == 0)
            throw new ArgumentException($"Saved {saved} portal bills. None of them fall in the selected dates.");

        var billFrom = PreviousFinancialYearStart(from);
        var earlier = await LoadEarlierPortalAsync(company, billFrom, from, inRange, ct);
        var erpParts = await Task.WhenAll(matchingMembers.Select(member => LoadErpAsync(member.Name, from, to, billFrom, ct)));
        var erpRows = erpParts.SelectMany(part => part).ToList();

        var result = new GstBillRecoResult
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
            AmountTolerance = amountTolerance,
            BuyerGstin = parsed.BuyerGstin,
            CompanyGstin = buyerGstin,
            TwoBRows = inRange.Count,
            SavedRows = saved,
            ErpRows = erpRows.Count,
        };

        if (matchingMembers.Count > 1)
            result.Warning = $"Combined {matchingMembers.Count} ERP units in {company} that use GSTIN {buyerGstin}.";

        result.Rows = BuildRows(company, erpRows, inRange, amountTolerance, earlier);
        NoteEarlierMatches(result, from);
        await AddAttachmentReferencesAsync(result.Rows, ct);
        result.Matched = result.Rows.Count(r => r.Status == "Matched");
        result.PotentialMatch = result.Rows.Count(r => r.Status == "Potential match");
        result.Mismatch = result.Rows.Count(r => r.Status == "Mismatch");
        result.MissingInErp = result.Rows.Count(r => r.Status == "Missing in ERP");
        result.MissingIn2B = result.Rows.Count(r => r.Status == "Missing in 2B");
        result.DateDifference = result.Rows.Count(r => r.Status == "Matched" && r.DateDiffers);
        return result;
    }

    public async Task<GstBillRecoResult> ReconcileStoredAsync(
        string companyName,
        DateTime dateFrom,
        DateTime dateTo,
        decimal amountTolerance,
        CancellationToken ct = default)
    {
        var company = (companyName ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (to < from)
            throw new ArgumentException("Date to is before date from.");
        if (amountTolerance < 0)
            amountTolerance = 0;

        var documents = await _snapshots.GetDocumentsAsync(company, from, to, purchaseOnly: true, ct);
        var parsed = FromDocuments(documents);
        if (parsed.Rows.Count == 0)
            throw new ArgumentException("No frozen GSTR-2B purchase documents are stored for this group in those months.");

        var members = await LoadGroupMembersAsync(company, ct);
        var buyerGstins = documents.Select(document => document.BuyerGstin).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var matchingMembers = members
            .Where(member => buyerGstins.Contains(member.Gstin, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (matchingMembers.Count == 0)
            throw new ArgumentException($"No ERP units in {company} use the stored GSTIN.");

        var billFrom = PreviousFinancialYearStart(from);
        var earlier = await LoadEarlierPortalAsync(company, billFrom, from, parsed.Rows, ct);
        var erpParts = await Task.WhenAll(matchingMembers.Select(member => LoadErpAsync(member.Name, from, to, billFrom, ct)));
        var erpRows = erpParts.SelectMany(part => part).ToList();
        var result = new GstBillRecoResult
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
            AmountTolerance = amountTolerance,
            BuyerGstin = string.Join(", ", buyerGstins),
            CompanyGstin = string.Join(", ", buyerGstins),
            TwoBRows = parsed.Rows.Count,
            SavedRows = parsed.Rows.Count,
            ErpRows = erpRows.Count,
        };
        if (matchingMembers.Count > 1)
            result.Warning = $"Combined {matchingMembers.Count} ERP units in {company} that use the stored GSTIN.";
        result.Rows = BuildRows(company, erpRows, parsed.Rows, amountTolerance, earlier);
        NoteEarlierMatches(result, from);
        await AddAttachmentReferencesAsync(result.Rows, ct);
        result.Matched = result.Rows.Count(r => r.Status == "Matched");
        result.PotentialMatch = result.Rows.Count(r => r.Status == "Potential match");
        result.Mismatch = result.Rows.Count(r => r.Status == "Mismatch");
        result.MissingInErp = result.Rows.Count(r => r.Status == "Missing in ERP");
        result.MissingIn2B = result.Rows.Count(r => r.Status == "Missing in 2B");
        result.DateDifference = result.Rows.Count(r => r.Status == "Matched" && r.DateDiffers);
        return result;
    }

    private static TwoBParse FromSnapshot(Gst2BSnapshot snapshot) => FromDocuments(snapshot.Documents);

    private static TwoBParse FromDocuments(IEnumerable<Gst2BDocumentRow> documents)
    {
        var rows = new List<TwoBLine>();
        var nextId = 1;
        foreach (var document in documents)
        {
            if (!Gst2BSnapshotService.IsPurchaseSection(document.Section))
                continue;
            if (!IncludeDocument(document.DocumentType))
                continue;
            var billNo = Gst2BSnapshotService.NormBill(document.DocumentNumber);
            var gst = document.SupplierGstin;
            if (billNo.Length == 0 || gst.Length == 0 || document.DocumentDate == default)
                continue;
            if (Math.Abs(document.Igst) <= TaxEpsilon && Math.Abs(document.Cgst + document.Sgst) <= TaxEpsilon)
                continue;
            rows.Add(new TwoBLine
            {
                Id = nextId++,
                BillNo = billNo,
                BillDate = document.DocumentDate,
                GstNo = gst,
                Key = Key(billNo, document.DocumentDate, gst),
                Supplier = document.SupplierName,
                DocumentType = document.DocumentType,
                Taxable = document.Taxable,
                Igst = document.Igst,
                CgstSgst = document.Cgst + document.Sgst,
                Period = document.ReturnPeriod.ToString("MMM-yy", CultureInfo.InvariantCulture),
                Month = document.ReturnPeriod.ToString("MMM-yy", CultureInfo.InvariantCulture),
            });
        }

        var buyer = documents.Select(document => document.BuyerGstin).FirstOrDefault(value => value.Length > 0) ?? "";
        return new TwoBParse { BuyerGstin = buyer, Rows = rows };
    }

    public byte[] Export(IReadOnlyList<GstBillRecoRow> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new ArgumentException("There are no rows to export.");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Bill Reco");
        var headers = new List<string> { "Status", "How matched", "2B bill no", "Date differs", "2B bill date", "Taxable difference", "2B taxable", "2B IGST", "2B CGST + SGST", "Document type" };
        headers.AddRange(FormatColumns);

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = i + 2;
            var values = new List<object?>
            {
                row.Status,
                row.MatchReason,
                row.TwoBBillNo,
                row.DateDiffers ? "Yes" : "",
                row.TwoBBillDate,
                row.Difference,
                row.TwoBTaxable,
                row.TwoBIgst,
                row.TwoBCgstSgst,
                row.DocumentType,
                row.CompanyName,
                row.VoucherType,
                row.VoucherNo,
                row.VoucherDate,
                row.RefNo,
                row.BillDate,
                row.BillNo,
                row.Ledger,
                row.Party,
                row.GstNo,
                row.GrossAmount,
                row.Status == "Missing in ERP" ? row.TwoBTaxable : row.ValueAmount,
                row.TotalC,
                row.OtherAll,
                row.Cgst,
                row.Sgst,
                row.Igst,
            };
            foreach (var ledger in LedgerColumns)
                values.Add(LedgerAmount(row, ledger));
            values.Add(row.MonthAsPer2B);
            values.Add(row.TwoBPeriod);

            for (var c = 0; c < values.Count; c++)
                WriteCell(sheet.Cell(line, c + 1), values[c]);
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(rows.Count + 1, 2), headers.Count).SetAutoFilter();
        sheet.Columns().AdjustToContents(1, 40);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private async Task<string> LoadCompanyGstinAsync(string company, CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT TOP 1 NewGSTNo
FROM FactoryInfo WITH (NOLOCK)
WHERE Name = @Name";
        cmd.Parameters.Add("@Name", SqlDbType.VarChar, 150).Value = company;
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value == DBNull.Value ? "" : NormGst(Convert.ToString(value));
    }

    private async Task<List<CompanyMember>> LoadGroupMembersAsync(string groupLabel, CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT LTRIM(RTRIM(Name)) AS Name, LTRIM(RTRIM(ISNULL(NewGSTNo, ''))) AS Gstin
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(Name)), '') <> ''
  AND COALESCE(NULLIF(LTRIM(RTRIM(GroupName)), ''), LTRIM(RTRIM(Name))) = @GroupLabel
ORDER BY Name";
        cmd.Parameters.Add("@GroupLabel", SqlDbType.NVarChar, 150).Value = groupLabel;

        var members = new List<CompanyMember>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            members.Add(new CompanyMember
            {
                Name = Convert.ToString(reader["Name"])?.Trim() ?? "",
                Gstin = NormGst(Convert.ToString(reader["Gstin"]))
            });
        }
        return members;
    }

    private async Task AddAttachmentReferencesAsync(List<GstBillRecoRow> rows, CancellationToken ct)
    {
        var erpRows = rows
            .Where(row => row.VoucherDate.HasValue && !string.IsNullOrWhiteSpace(row.VoucherNo))
            .ToList();
        if (erpRows.Count == 0)
            return;

        foreach (var companyRows in erpRows.GroupBy(row => row.CompanyName, StringComparer.OrdinalIgnoreCase))
            await AddCompanyAttachmentReferencesAsync(companyRows.Key, companyRows.ToList(), ct);
    }

    private async Task AddCompanyAttachmentReferencesAsync(
        string companyName,
        List<GstBillRecoRow> rows,
        CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
DECLARE @Code varchar(50) = (
    SELECT TOP 1 LTRIM(RTRIM(ISNULL(IndentCode, '')))
    FROM FactoryInfo WITH (NOLOCK)
    WHERE Name = @CompanyName
);

IF OBJECT_ID('dbo.dms_ref', 'U') IS NOT NULL AND OBJECT_ID('dbo.dms_files', 'U') IS NOT NULL
BEGIN
    SELECT @Code AS IndentCode, r.RefType, r.RefEntryNo, COUNT_BIG(*) AS AttachmentCount
    FROM dbo.dms_ref r WITH (NOLOCK)
    INNER JOIN dbo.dms_files f WITH (NOLOCK) ON f.RefId = r.RefId
    WHERE @Code <> ''
      AND r.RefEntryNo LIKE @Code + '/%'
      AND ISNULL(f.IsDeleted, 0) = 0
    GROUP BY r.RefType, r.RefEntryNo;
END
""";
        cmd.Parameters.Add("@CompanyName", SqlDbType.VarChar, 150).Value = companyName;
        var years = rows
            .Where(row => row.VoucherDate.HasValue)
            .Select(row => FinancialYear(row.VoucherDate!.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (years.Count == 0)
            return;
        var yearClauses = new List<string>();
        for (var index = 0; index < years.Count; index++)
        {
            var parameterName = "@Fy" + index.ToString(CultureInfo.InvariantCulture);
            yearClauses.Add($"CHARINDEX('/' + {parameterName} + '/', r.RefEntryNo) > 0");
            cmd.Parameters.Add(parameterName, SqlDbType.VarChar, 10).Value = years[index];
        }
        cmd.CommandText = cmd.CommandText.Replace(
            "AND r.RefEntryNo LIKE @Code + '/%'",
            "AND r.RefEntryNo LIKE @Code + '/%' AND (" + string.Join(" OR ", yearClauses) + ")");

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var references = new List<(string Code, string Type, string Entry, int Count)>();
        while (await reader.ReadAsync(ct))
        {
            references.Add((
                Convert.ToString(reader["IndentCode"])?.Trim() ?? "",
                Convert.ToString(reader["RefType"])?.Trim() ?? "",
                Convert.ToString(reader["RefEntryNo"])?.Trim() ?? "",
                Convert.ToInt32(reader["AttachmentCount"], CultureInfo.InvariantCulture)));
        }

        foreach (var row in rows)
        {
            var fy = FinancialYear(row.VoucherDate!.Value);
            var voucherNo = row.VoucherNo.Trim();
            var voucherType = row.VoucherType.Trim();
            var candidates = references
                .Where(reference => AttachmentReferenceMatches(reference.Code, reference.Entry, fy, voucherNo))
                .ToList();
            var typed = candidates
                .Where(reference => AttachmentTypeMatches(reference.Type, reference.Entry, voucherType))
                .ToList();
            var pool = typed.Count > 0 ? typed : candidates;
            var match = pool
                .OrderBy(reference => reference.Entry.Count(ch => ch == '/'))
                .ThenBy(reference => reference.Entry, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (match.Count <= 0)
                continue;
            row.AttachmentCount = match.Count;
            row.DmsRefType = match.Type;
            row.DmsRefEntryNo = match.Entry;
        }
    }

    /// <summary>
    /// Company code, financial year, and voucher number must each be their own segment.
    /// Voucher type is used to choose among those matches, so a different type with the same number is not preferred.
    /// </summary>
    private static bool AttachmentReferenceMatches(string code, string entry, string fy, string voucherNo)
    {
        if (code.Length == 0 || fy.Length == 0 || voucherNo.Length == 0)
            return false;
        var parts = entry.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return false;
        if (!parts[0].Equals(code, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!parts[^1].Equals(voucherNo, StringComparison.OrdinalIgnoreCase))
            return false;
        return parts.Any(part => part.Equals(fy, StringComparison.OrdinalIgnoreCase));
    }

    private static bool AttachmentTypeMatches(string refType, string entry, string voucherType)
    {
        if (voucherType.Length == 0)
            return false;
        if (refType.Equals(voucherType, StringComparison.OrdinalIgnoreCase))
            return true;
        var wanted = Norm(voucherType);
        if (wanted.Length == 0)
            return false;
        if (Norm(refType) == wanted)
            return true;
        return entry.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => Norm(part) == wanted || SameVoucherFamily(Norm(part), wanted));
    }

    private static bool SameVoucherFamily(string left, string right)
    {
        if (left.Length < 3 || right.Length < 3)
            return false;
        return (left.StartsWith("PUR", StringComparison.Ordinal) && right.StartsWith("PUR", StringComparison.Ordinal))
            || (left.StartsWith("JOUR", StringComparison.Ordinal) && right.StartsWith("JOUR", StringComparison.Ordinal))
            || (left.Contains("CREDIT", StringComparison.Ordinal) && right.Contains("CREDIT", StringComparison.Ordinal))
            || (left.Contains("DEBIT", StringComparison.Ordinal) && right.Contains("DEBIT", StringComparison.Ordinal));
    }

    private static string FinancialYear(DateTime date)
    {
        var start = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{start % 100:00}-{(start + 1) % 100:00}";
    }

    private static DateTime PreviousFinancialYearStart(DateTime date)
    {
        var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        return new DateTime(startYear - 1, 4, 1);
    }

    /// <summary>
    /// Stored 2B from the previous financial year. It can match a bill booked in the selected
    /// months, and an unmatched invoice from those earlier months is not reported as missing.
    /// </summary>
    private async Task<List<TwoBLine>> LoadEarlierPortalAsync(
        string company,
        DateTime billFrom,
        DateTime from,
        IReadOnlyList<TwoBLine> current,
        CancellationToken ct)
    {
        if (billFrom.Date >= from.Date)
            return [];
        var documents = await _snapshots.GetDocumentsAsync(company, billFrom, from.AddDays(-1), purchaseOnly: true, ct);
        var rows = FromDocuments(documents).Rows
            .Where(row => current.All(existing => !string.Equals(existing.Key, row.Key, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var nextId = current.Count == 0 ? 1 : current.Max(row => row.Id) + 1;
        foreach (var row in rows)
            row.Id = nextId++;
        return rows;
    }

    private static void NoteEarlierMatches(GstBillRecoResult result, DateTime from)
    {
        var monthStart = new DateTime(from.Year, from.Month, 1);
        var used = result.Rows.Count(row =>
            row.Status is "Matched" or "Potential match" or "Mismatch"
            && PeriodBefore(row.TwoBPeriod, monthStart));
        if (used == 0)
            return;
        var note = $"{used} bill(s) booked in this range matched GSTR-2B from the previous financial year. Unmatched invoices from those months are not listed as missing in ERP.";
        result.Warning = string.IsNullOrEmpty(result.Warning) ? note : result.Warning + " " + note;
    }

    private static bool PeriodBefore(string period, DateTime monthStart)
    {
        if (string.IsNullOrWhiteSpace(period))
            return false;
        foreach (var part in period.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryPeriodMonth(part, out var month) && month < monthStart)
                return true;
        }
        return false;
    }

    private async Task<string> CompanyNameForGstinAsync(string gstin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return "";
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT TOP 1 Name
FROM FactoryInfo WITH (NOLOCK)
WHERE REPLACE(UPPER(ISNULL(NewGSTNo, '')), ' ', '') = @Gstin";
        cmd.Parameters.Add("@Gstin", SqlDbType.VarChar, 20).Value = NormGst(gstin);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value == DBNull.Value ? "" : Convert.ToString(value)?.Trim() ?? "";
    }

    private async Task<int> SaveTwoBAsync(string company, TwoBParse parsed, CancellationToken ct)
    {
        var buyer = NormGst(parsed.BuyerGstin);
        if (buyer.Length == 0)
            buyer = NormGst(await LoadCompanyGstinAsync(company, ct));
        if (buyer.Length == 0)
            throw new ArgumentException("The 2B file has no buyer GSTIN, so it cannot be saved.");

        await using var connection = _database.CreateConnection();
        await EnsureStoreAsync(connection, ct);

        var table = new DataTable();
        table.Columns.Add("BuyerGstin", typeof(string));
        table.Columns.Add("CompanyName", typeof(string));
        table.Columns.Add("SupplierName", typeof(string));
        table.Columns.Add("SupplierGstin", typeof(string));
        table.Columns.Add("BillNo", typeof(string));
        table.Columns.Add("BillDate", typeof(DateTime));
        table.Columns.Add("DocumentType", typeof(string));
        table.Columns.Add("Taxable", typeof(decimal));
        table.Columns.Add("Igst", typeof(decimal));
        table.Columns.Add("CgstSgst", typeof(decimal));
        table.Columns.Add("TwoBPeriod", typeof(string));

        foreach (var row in parsed.Rows)
        {
            table.Rows.Add(
                buyer,
                company,
                TrimTo(row.Supplier, 300),
                row.GstNo,
                TrimTo(row.BillNo, 200),
                row.BillDate,
                TrimTo(row.DocumentType, 50),
                row.Taxable,
                row.Igst,
                row.CgstSgst,
                TrimTo(row.Period, 40));
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await using (var create = connection.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText = @"
CREATE TABLE #Gst2B (
    BuyerGstin varchar(20) NOT NULL,
    CompanyName varchar(150) NULL,
    SupplierName nvarchar(300) NULL,
    SupplierGstin varchar(20) NOT NULL,
    BillNo nvarchar(200) NOT NULL,
    BillDate date NOT NULL,
    DocumentType varchar(50) NULL,
    Taxable decimal(18,2) NOT NULL,
    Igst decimal(18,2) NOT NULL,
    CgstSgst decimal(18,2) NOT NULL,
    TwoBPeriod varchar(40) NULL
)";
            await create.ExecuteNonQueryAsync(ct);
        }

        using (var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
        {
            bulk.DestinationTableName = "#Gst2B";
            bulk.BulkCopyTimeout = 180;
            foreach (DataColumn column in table.Columns)
                bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            await bulk.WriteToServerAsync(table, ct);
        }

        await using (var merge = connection.CreateCommand())
        {
            merge.Transaction = transaction;
            merge.CommandTimeout = 180;
            merge.CommandText = @"
MERGE dbo.AppGstBill2B AS target
USING (
    SELECT BuyerGstin, MAX(CompanyName) AS CompanyName, MAX(SupplierName) AS SupplierName,
           SupplierGstin, BillNo, BillDate, MAX(DocumentType) AS DocumentType,
           SUM(Taxable) AS Taxable, SUM(Igst) AS Igst, SUM(CgstSgst) AS CgstSgst, MAX(TwoBPeriod) AS TwoBPeriod
    FROM #Gst2B
    GROUP BY BuyerGstin, SupplierGstin, BillNo, BillDate
) AS source
ON target.BuyerGstin COLLATE DATABASE_DEFAULT = source.BuyerGstin COLLATE DATABASE_DEFAULT
   AND target.SupplierGstin COLLATE DATABASE_DEFAULT = source.SupplierGstin COLLATE DATABASE_DEFAULT
   AND target.BillNo COLLATE DATABASE_DEFAULT = source.BillNo COLLATE DATABASE_DEFAULT
   AND target.BillDate = source.BillDate
WHEN MATCHED THEN UPDATE SET
    CompanyName = source.CompanyName,
    SupplierName = source.SupplierName,
    DocumentType = source.DocumentType,
    Taxable = source.Taxable,
    Igst = source.Igst,
    CgstSgst = source.CgstSgst,
    TwoBPeriod = source.TwoBPeriod,
    UploadedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT (
    BuyerGstin, CompanyName, SupplierName, SupplierGstin, BillNo, BillDate,
    DocumentType, Taxable, Igst, CgstSgst, TwoBPeriod, UploadedAt)
VALUES (
    source.BuyerGstin, source.CompanyName, source.SupplierName, source.SupplierGstin, source.BillNo, source.BillDate,
    source.DocumentType, source.Taxable, source.Igst, source.CgstSgst, source.TwoBPeriod, GETDATE());";
            await merge.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return table.Rows.Count;
    }

    private static async Task EnsureStoreAsync(SqlConnection connection, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
IF OBJECT_ID('dbo.AppGstBill2B', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppGstBill2B (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BuyerGstin varchar(20) NOT NULL,
        CompanyName varchar(150) NULL,
        SupplierName nvarchar(300) NULL,
        SupplierGstin varchar(20) NOT NULL,
        BillNo nvarchar(200) NOT NULL,
        BillDate date NOT NULL,
        DocumentType varchar(50) NULL,
        Taxable decimal(18,2) NOT NULL,
        Igst decimal(18,2) NOT NULL,
        CgstSgst decimal(18,2) NOT NULL,
        TwoBPeriod varchar(40) NULL,
        UploadedAt datetime NOT NULL CONSTRAINT DF_AppGstBill2B_UploadedAt DEFAULT GETDATE(),
        CONSTRAINT UQ_AppGstBill2B UNIQUE (BuyerGstin, SupplierGstin, BillNo, BillDate)
    );
END";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string TrimTo(string? value, int length)
    {
        var text = value ?? "";
        return text.Length <= length ? text : text[..length];
    }

    /// <summary>
    /// Produces the valid ERP taxable alternatives. If both OtherAll columns contain values,
    /// either may reconcile the bill. Freight and C&amp;F are included in every alternative.
    /// </summary>
    private static List<decimal> TaxableCandidatesWithFreight(SqlDataReader reader, int camelAt, int capitalAt)
    {
        var camel = camelAt >= 0 ? Money(reader, camelAt) ?? 0 : 0;
        var capital = capitalAt >= 0 ? Money(reader, capitalAt) ?? 0 : 0;
        decimal charges = 0;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (i == camelAt || i == capitalAt)
                continue;
            var name = Norm(reader.GetName(i));
            if (name.Length == 0 || name == "OTHERALL")
                continue;
            if (!name.Contains("FREIGHT", StringComparison.Ordinal) && !name.Contains("C&F", StringComparison.Ordinal))
                continue;
            charges += Math.Abs(Money(reader, i) ?? 0);
        }

        var bases = new List<decimal>();
        if (Math.Abs(Math.Round(camel, 2)) > 0)
            bases.Add(camel);
        if (Math.Abs(Math.Round(capital, 2)) > 0)
            bases.Add(capital);
        if (bases.Count == 0)
            bases.Add(0);

        return bases
            .Select(value =>
            {
                var magnitude = Math.Abs(value) + charges;
                return value < 0 ? -magnitude : magnitude;
            })
            .Distinct()
            .ToList();
    }

    private static int FindOtherAllLedger(SqlDataReader reader, int camelAt)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (i == camelAt)
                continue;
            if (Norm(reader.GetName(i)) == "OTHERALL")
                return i;
        }
        return -1;
    }

    private static int FindColumn(SqlDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static async Task EnsureBillProcedureAsync(SqlConnection connection, CancellationToken ct)
    {
        if (Volatile.Read(ref _billProcedureReady) == 1)
            return;
        var resourceName = typeof(GstBillRecoService).Assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("AppGstRecoBills.sql", StringComparison.Ordinal));
        if (resourceName == null)
            throw new InvalidOperationException("The bill reconciliation procedure script is missing from the API.");
        await using var stream = typeof(GstBillRecoService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The bill reconciliation procedure script could not be opened.");
        using var text = new StreamReader(stream);
        var sql = await text.ReadToEndAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 60;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
        Volatile.Write(ref _billProcedureReady, 1);
    }

    private async Task<List<ErpLine>> LoadErpAsync(string company, DateTime from, DateTime to, DateTime billFrom, CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await EnsureBillProcedureAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "dbo.AppGstRecoBills";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = TimeoutSeconds;
        cmd.Parameters.Add("@CompanyName", SqlDbType.VarChar, 100).Value = company;
        cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = from;
        cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = to;
        if (billFrom.Date < from.Date)
            cmd.Parameters.Add("@BillFrom", SqlDbType.DateTime).Value = billFrom.Date;

        var rows = new List<ErpLine>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var ordinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            ordinal[Norm(reader.GetName(i))] = i;
        if (!ordinal.ContainsKey("BILLNO") || !ordinal.ContainsKey("GSTIN") || !ordinal.ContainsKey("BASIC"))
            throw new ArgumentException("Purchase bills did not return bill number, GSTIN, and basic value.");

        while (await reader.ReadAsync(ct))
        {
            var billNo = Text(reader, ordinal, "BILLNO");
            var gst = NormGst(Text(reader, ordinal, "GSTIN"));
            var billDate = DateOf(reader, ordinal, "BILLDATE");
            if (billNo.Length == 0 || gst.Length == 0 || billDate == null || !InRange(billDate.Value, billFrom, to))
                continue;

            var basic = MoneyAt(reader, ordinal, "BASIC") ?? 0;
            var cgst = MoneyAt(reader, ordinal, "CGST") ?? 0;
            var sgst = MoneyAt(reader, ordinal, "SGST") ?? 0;
            var igst = MoneyAt(reader, ordinal, "IGST") ?? 0;
            if (!KeepErpTax(cgst, sgst, igst))
                continue;

            var freight = MoneyAt(reader, ordinal, "FREIGHT") ?? 0;
            var other = MoneyAt(reader, ordinal, "OTHERCHARGE") ?? 0;
            var loading = MoneyAt(reader, ordinal, "LOADING") ?? 0;
            var packing = MoneyAt(reader, ordinal, "PACKING") ?? 0;
            var charges = Math.Abs(freight) + Math.Abs(other) + Math.Abs(loading) + Math.Abs(packing);
            var sign = basic < 0 || (basic == 0 && cgst + sgst + igst < 0) ? -1m : 1m;
            var basicAbs = Math.Abs(basic);
            var candidates = new List<decimal> { sign * basicAbs };
            if (charges > TaxEpsilon)
                candidates.Add(sign * (basicAbs + charges));
            rows.Add(new ErpLine
            {
                CompanyName = company,
                BillNo = billNo,
                BillParts = BillParts(billNo),
                BillDate = billDate.Value,
                GstNo = gst,
                FullKey = Key(NormBill(billNo), billDate.Value, gst),
                Party = Text(reader, ordinal, "PARTY"),
                VoucherType = Text(reader, ordinal, "VOUCHERTYPE"),
                VoucherNo = Text(reader, ordinal, "VOUCHERNO"),
                VoucherDate = DateOf(reader, ordinal, "VOUCHERDATE"),
                RefNo = Text(reader, ordinal, "REFNO"),
                Ledger = Text(reader, ordinal, "LEDGER"),
                GrossAmount = MoneyAt(reader, ordinal, "GROSSAMOUNT"),
                ValueAmount = basic,
                Taxable = candidates[0],
                TaxableCandidates = candidates,
                OtherAll = other + loading + packing,
                Cgst = cgst,
                Sgst = sgst,
                Igst = igst,
                Ledgers = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FREIGHT INWARD EXPENSE (WITH GST)"] = freight,
                    ["OTHER ALL"] = other + loading + packing,
                },
            });
        }

        await reader.CloseAsync();
        rows.AddRange(await LoadAdjustmentVouchersAsync(connection, company, from, to, billFrom, ct));
        return NetSignedBills(rows);
    }

    /// <summary>
    /// Journal and debit-note vouchers carry the reversals. Reading them from the voucher
    /// tables avoids the full GST summary. A later net combines a reversal with the purchase
    /// that has the same bill number, date, and GSTIN.
    /// </summary>
    private static async Task<List<ErpLine>> LoadAdjustmentVouchersAsync(
        SqlConnection connection,
        string company,
        DateTime from,
        DateTime to,
        DateTime billFrom,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = TimeoutSeconds;
        cmd.CommandText = """
SELECT
    ISNULL(j.JournalType, 'Journal') AS VoucherType,
    CONVERT(varchar(40), j.JournalNo) AS VoucherNo,
    j.sysdate AS VoucherDate,
    MAX(CASE WHEN j.BillDate IS NOT NULL THEN LTRIM(RTRIM(ISNULL(j.MRNo, ''))) ELSE '' END) AS BillNo,
    MAX(j.BillDate) AS BillDate,
    MAX(party.PartyName) AS Party,
    MAX(party.Gstin) AS Gstin,
    SUM(CASE WHEN (j.PartyName LIKE '%CGST%INPUT%' OR j.PartyName LIKE '%INPUT%CGST%') AND j.PartyName NOT LIKE '%OUTPUT%'
        THEN ISNULL(j.Debit, 0) - ISNULL(j.Credit, 0) ELSE 0 END) AS Cgst,
    SUM(CASE WHEN (j.PartyName LIKE '%SGST%INPUT%' OR j.PartyName LIKE '%INPUT%SGST%') AND j.PartyName NOT LIKE '%OUTPUT%'
        THEN ISNULL(j.Debit, 0) - ISNULL(j.Credit, 0) ELSE 0 END) AS Sgst,
    SUM(CASE WHEN (j.PartyName LIKE '%IGST%INPUT%' OR j.PartyName LIKE '%INPUT%IGST%') AND j.PartyName NOT LIKE '%OUTPUT%'
        THEN ISNULL(j.Debit, 0) - ISNULL(j.Credit, 0) ELSE 0 END) AS Igst,
    SUM(CASE WHEN j.PartyName NOT LIKE '%GST%' AND j.PartyName NOT LIKE 'TCS%' AND j.PartyName NOT LIKE 'TDS%'
              AND (lm.Category IS NULL OR lm.Category NOT IN ('Customer-Supplier', 'Customer', 'Supplier', 'Service Provider', 'Bank Accounts'))
        THEN ISNULL(j.Debit, 0) - ISNULL(j.Credit, 0) ELSE 0 END) AS Taxable
FROM Journal j WITH (NOLOCK)
LEFT JOIN LedgerMaster lm WITH (NOLOCK)
    ON lm.CompanyName = j.CompanyName AND lm.LedgerName = j.PartyName
OUTER APPLY (
    SELECT TOP 1 p.PartyName, LTRIM(RTRIM(ISNULL(pl.NewGSTNo, ''))) AS Gstin
    FROM Journal p WITH (NOLOCK)
    INNER JOIN LedgerMaster pl WITH (NOLOCK)
        ON pl.CompanyName = p.CompanyName AND pl.LedgerName = p.PartyName
    WHERE p.CompanyName = j.CompanyName AND p.JournalNo = j.JournalNo AND p.sysdate = j.sysdate
      AND pl.Category IN ('Customer-Supplier', 'Customer', 'Supplier', 'Service Provider')
      AND LTRIM(RTRIM(ISNULL(pl.NewGSTNo, ''))) <> ''
    ORDER BY ABS(ISNULL(p.Debit, 0) - ISNULL(p.Credit, 0)) DESC
) party
WHERE j.CompanyName = @Company
  AND j.sysdate >= @From AND j.sysdate < @ToExclusive
GROUP BY j.JournalType, j.JournalNo, j.sysdate

UNION ALL

SELECT
    'Debit Note' AS VoucherType,
    CONVERT(varchar(40), d.DebitNoteNumber) AS VoucherNo,
    d.sysdate AS VoucherDate,
    LTRIM(RTRIM(ISNULL(d.BillNo, ''))) AS BillNo,
    d.billDate AS BillDate,
    d.PartyName AS Party,
    LTRIM(RTRIM(ISNULL(lm.NewGSTNo, ''))) AS Gstin,
    -ISNULL(i.Cgst, 0) AS Cgst,
    -ISNULL(i.Sgst, 0) AS Sgst,
    -ISNULL(i.Igst, 0) AS Igst,
    -ISNULL(i.Taxable, 0) AS Taxable
FROM DebitNote d WITH (NOLOCK)
LEFT JOIN LedgerMaster lm WITH (NOLOCK)
    ON lm.CompanyName = d.CompanyName AND lm.LedgerName = d.PartyName
LEFT JOIN (
    SELECT CompanyName, DebitNoteNumber,
           SUM(TRY_CONVERT(decimal(18, 2), CGSTAmount)) AS Cgst,
           SUM(TRY_CONVERT(decimal(18, 2), SGSTAmount)) AS Sgst,
           SUM(TRY_CONVERT(decimal(18, 2), IGSTAmount)) AS Igst,
           SUM(TRY_CONVERT(decimal(18, 2), AmountDifference)) AS Taxable
    FROM DebitNoteItem WITH (NOLOCK)
    GROUP BY CompanyName, DebitNoteNumber
) i ON i.CompanyName = d.CompanyName AND i.DebitNoteNumber = d.DebitNoteNumber
WHERE d.CompanyName = @Company
  AND d.sysdate >= @From AND d.sysdate < @ToExclusive;
""";
        cmd.Parameters.Add("@Company", SqlDbType.VarChar, 100).Value = company;
        cmd.Parameters.Add("@From", SqlDbType.DateTime).Value = from;
        cmd.Parameters.Add("@ToExclusive", SqlDbType.DateTime).Value = to.Date.AddDays(1);

        var rows = new List<ErpLine>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var ordinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            ordinal[Norm(reader.GetName(i))] = i;
        while (await reader.ReadAsync(ct))
        {
            var billNo = Text(reader, ordinal, "BILLNO");
            var gst = NormGst(Text(reader, ordinal, "GSTIN"));
            var billDate = DateOf(reader, ordinal, "BILLDATE");
            if (billNo.Length == 0 || gst.Length == 0 || billDate == null || !InRange(billDate.Value, billFrom, to))
                continue;
            var cgst = MoneyAt(reader, ordinal, "CGST") ?? 0;
            var sgst = MoneyAt(reader, ordinal, "SGST") ?? 0;
            var igst = MoneyAt(reader, ordinal, "IGST") ?? 0;
            if (!KeepErpTax(cgst, sgst, igst))
                continue;
            var taxable = MoneyAt(reader, ordinal, "TAXABLE") ?? 0;
            rows.Add(new ErpLine
            {
                CompanyName = company,
                BillNo = billNo,
                BillParts = BillParts(billNo),
                BillDate = billDate.Value,
                GstNo = gst,
                FullKey = Key(NormBill(billNo), billDate.Value, gst),
                Party = Text(reader, ordinal, "PARTY"),
                VoucherType = Text(reader, ordinal, "VOUCHERTYPE"),
                VoucherNo = Text(reader, ordinal, "VOUCHERNO"),
                VoucherDate = DateOf(reader, ordinal, "VOUCHERDATE"),
                RefNo = billNo,
                GrossAmount = taxable + cgst + sgst + igst,
                ValueAmount = taxable,
                Taxable = taxable,
                TaxableCandidates = [taxable],
                Cgst = cgst,
                Sgst = sgst,
                Igst = igst,
                Ledgers = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase),
            });
        }
        return rows;
    }

    /// <summary>
    /// Adds a reversal to the purchase with the same bill. A journal marked as a reversal
    /// (R-, Reversed, _rev) is folded onto that bill first. Equal and opposite GST cancels the bill.
    /// A debit note with its own bill number stays on its own and can match the portal debit note.
    /// </summary>
    private static List<ErpLine> NetSignedBills(List<ErpLine> rows)
    {
        FoldMarkedReversals(rows);
        var netted = new List<ErpLine>();
        foreach (var group in rows.GroupBy(row => row.FullKey, StringComparer.OrdinalIgnoreCase))
        {
            var items = group.ToList();
            if (items.Count == 1)
            {
                netted.Add(items[0]);
                continue;
            }

            var cgst = items.Sum(row => row.Cgst);
            var sgst = items.Sum(row => row.Sgst);
            var igst = items.Sum(row => row.Igst);
            if (!KeepErpTax(cgst, sgst, igst))
                continue;

            var taxable = items.Sum(row => row.Taxable);
            var keeper = items
                .OrderByDescending(row => IsAdjustmentVoucher(row.VoucherType) ? 0 : 1)
                .ThenByDescending(row => Math.Abs(row.Cgst) + Math.Abs(row.Sgst) + Math.Abs(row.Igst))
                .First();
            keeper.Cgst = cgst;
            keeper.Sgst = sgst;
            keeper.Igst = igst;
            keeper.Taxable = taxable;
            keeper.TaxableCandidates = [taxable];
            keeper.ValueAmount = taxable;
            keeper.GrossAmount = items.Sum(row => row.GrossAmount ?? 0);
            netted.Add(keeper);
        }
        return netted;
    }

    /// <summary>
    /// Points R- / Reversed / _rev journals at the original bill when that bill is in the same run,
    /// on the same date and GSTIN. The existing net then sums them or drops both when GST cancels.
    /// </summary>
    private static void FoldMarkedReversals(List<ErpLine> rows)
    {
        var byBill = new Dictionary<string, ErpLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = Key(NormBill(row.BillNo), row.BillDate, row.GstNo);
            if (!byBill.ContainsKey(key))
                byBill[key] = row;
        }

        foreach (var row in rows)
        {
            var basis = ReversalBasis(row.BillNo);
            if (basis.Length == 0)
                continue;
            if (!byBill.TryGetValue(Key(basis, row.BillDate, row.GstNo), out var original) || ReferenceEquals(original, row))
                continue;
            row.BillNo = original.BillNo;
            row.BillParts = original.BillParts;
            row.FullKey = original.FullKey;
            original.MatchReason = ReasonReversal;
            row.MatchReason = ReasonReversal;
        }
    }

    private static string ReversalBasis(string? billNo)
    {
        var text = NormBill(billNo);
        string[] suffixes = ["REVERSED", "_REV", "-REV", "/REV"];
        foreach (var suffix in suffixes)
        {
            if (text.Length <= suffix.Length || !text.EndsWith(suffix, StringComparison.Ordinal))
                continue;
            return text[..^suffix.Length].TrimEnd('-', '_', '/');
        }

        return text.StartsWith("R-", StringComparison.Ordinal) && text.Length > 2 ? text[2..] : "";
    }

    private static bool IsAdjustmentVoucher(string voucherType)
    {
        var token = Norm(voucherType);
        return token.Contains("JOURNAL", StringComparison.Ordinal) || token.Contains("DEBIT", StringComparison.Ordinal);
    }

    private static async Task<List<ErpLine>> ReadInvoiceSummaryAsync(
        SqlDataReader reader,
        Dictionary<string, int> ordinal,
        string company,
        CancellationToken ct)
    {
        var rows = new List<ErpLine>();
        while (await reader.ReadAsync(ct))
        {
            var billNo = Text(reader, ordinal, "INVNO");
            var gst = NormGst(Text(reader, ordinal, "SUPPLIERGSTNO"));
            var billDate = DateOf(reader, ordinal, "INVDATE");
            if (billNo.Length == 0 || gst.Length == 0 || billDate == null)
                continue;

            var igst = MoneyAt(reader, ordinal, "IGST") ?? 0;
            var combined = ordinal.ContainsKey("GST") ? MoneyAt(reader, ordinal, "GST") ?? 0 : 0;
            if (!KeepErpTax(combined == 0 ? 0 : combined, combined == 0 ? 0 : combined, igst) && combined == 0)
                continue;

            var cgst = combined == 0 ? 0 : Math.Round(combined / 2, 2, MidpointRounding.AwayFromZero);
            var sgst = combined - cgst;
            var taxable = MoneyAt(reader, ordinal, "BASIC") ?? 0;
            rows.Add(new ErpLine
            {
                CompanyName = company,
                BillNo = billNo,
                BillParts = BillParts(billNo),
                BillDate = billDate.Value,
                GstNo = gst,
                FullKey = Key(NormBill(billNo), billDate.Value, gst),
                ValueAmount = taxable,
                Taxable = taxable,
                TaxableCandidates = [taxable],
                Igst = igst,
                Cgst = cgst,
                Sgst = sgst,
                Ledgers = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase),
            });
        }
        return rows;
    }

    private static async Task ReadVoucherSummaryAsync(
        SqlDataReader reader,
        Dictionary<string, int> ordinal,
        Dictionary<string, List<ErpLine>> vouchers,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var fromYear = FinancialYear(from);
        var toYear = FinancialYear(to);
        while (await reader.ReadAsync(ct))
        {
            var billNo = NormBill(Text(reader, ordinal, "BILLNO"));
            var voucherDate = DateOf(reader, ordinal, "REFDATE");
            if (billNo.Length == 0 || voucherDate is null)
                continue;
            var year = FinancialYear(voucherDate.Value);
            if (!year.Equals(fromYear, StringComparison.Ordinal) && !year.Equals(toYear, StringComparison.Ordinal))
                continue;
            if (!vouchers.TryGetValue(billNo, out var list))
            {
                list = [];
                vouchers[billNo] = list;
            }
            list.Add(new ErpLine
            {
                BillNo = billNo,
                VoucherType = Text(reader, ordinal, "VOUCHERTYPE"),
                VoucherNo = Text(reader, ordinal, "VOUCHERNO"),
                VoucherDate = voucherDate,
                RefNo = Text(reader, ordinal, "REFNO"),
                Party = Text(reader, ordinal, "PARTY"),
                GrossAmount = MoneyAt(reader, ordinal, "GROSSAMOUNT"),
            });
        }
    }

    /// <summary>
    /// Keeps the voucher from the same financial year as the bill, closest to the bill date.
    /// A repeated bill number must not inherit the first historical voucher.
    /// </summary>
    private static ErpLine? SelectVoucher(IReadOnlyList<ErpLine> candidates, DateTime billDate)
    {
        var year = FinancialYear(billDate);
        ErpLine? best = null;
        var bestDistance = int.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.VoucherDate is not DateTime date || FinancialYear(date) != year)
                continue;
            var distance = Math.Abs((date.Date - billDate.Date).Days);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static List<GstBillRecoRow> BuildRows(
        string company,
        List<ErpLine> erpRows,
        List<TwoBLine> twoBRows,
        decimal tolerance,
        List<TwoBLine>? earlierRows = null)
    {
        var claimed = new HashSet<int>();
        var assignments = new List<(ErpLine Erp, List<TwoBLine> Hits)>();
        AssignPortalHits(erpRows, twoBRows, claimed, assignments, tolerance);
        if (earlierRows is { Count: > 0 })
        {
            var stillOpen = new List<ErpLine>();
            assignments.RemoveAll(item =>
            {
                if (item.Hits.Count > 0)
                    return false;
                stillOpen.Add(item.Erp);
                return true;
            });
            if (stillOpen.Count > 0)
                AssignPortalHits(stillOpen, earlierRows, claimed, assignments, tolerance);
        }

        var portalPool = earlierRows == null ? twoBRows : twoBRows.Concat(earlierRows).ToList();
        LinkExceptions(assignments, portalPool, claimed, tolerance);

        var clusters = new List<List<(ErpLine Erp, List<TwoBLine> Hits)>>();
        var bySignature = new Dictionary<string, List<(ErpLine Erp, List<TwoBLine> Hits)>>();
        foreach (var item in assignments)
        {
            if (item.Hits.Count == 0)
            {
                clusters.Add([item]);
                continue;
            }

            var signature = string.Join("|", item.Hits.Select(h => h.Id).OrderBy(id => id));
            if (!bySignature.TryGetValue(signature, out var cluster))
            {
                cluster = [];
                bySignature[signature] = cluster;
                clusters.Add(cluster);
            }
            cluster.Add(item);
        }

        var used = new HashSet<int>();
        var output = new List<GstBillRecoRow>();
        foreach (var cluster in clusters)
        {
            var hits = cluster[0].Hits;
            foreach (var hit in hits)
                used.Add(hit.Id);

            var twoAbs = hits.Sum(h => Math.Abs(h.Taxable));
            SelectBestTaxableCandidates(cluster, twoAbs);
            var erpAbs = cluster.Sum(c => Math.Abs(c.Erp.Taxable));
            var difference = Math.Round(erpAbs - twoAbs, 2);
            var baseStatus = hits.Count == 0
                ? "Missing in 2B"
                : Math.Abs(difference) <= tolerance
                    ? "Matched"
                    : TaxMatches(cluster, hits, tolerance) ? "Potential match" : "Mismatch";
            var period = string.Join(", ", hits.Select(h => h.Period).Where(p => p.Length > 0).Distinct());
            var month = string.Join(", ", hits.Select(h => h.Month).Where(p => p.Length > 0).Distinct());
            var docType = string.Join(", ", hits.Select(h => h.DocumentType).Where(p => p.Length > 0).Distinct());
            var twoBTaxable = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.Taxable);
            var twoBIgst = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.Igst);
            var twoBCgst = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.CgstSgst);
            var portalDates = hits.Select(hit => hit.BillDate.Date).Distinct().ToList();

            foreach (var item in cluster)
            {
                var erp = item.Erp;
                var datesDiffer = portalDates.Count > 0 && portalDates.Any(date => date != erp.BillDate.Date);
                var lineStatus = ExceptionStatus(baseStatus, erp.MatchReason);
                output.Add(new GstBillRecoRow
                {
                    Status = lineStatus,
                    MatchReason = erp.MatchReason,
                    TwoBBillNo = string.Join(", ", hits.Select(hit => hit.BillNo).Where(bill => bill.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)),
                    CompanyName = erp.CompanyName,
                    VoucherType = erp.VoucherType,
                    VoucherNo = erp.VoucherNo,
                    VoucherDate = erp.VoucherDate,
                    RefNo = erp.RefNo,
                    BillDate = erp.BillDate,
                    TwoBBillDate = portalDates.Count == 0 ? null : portalDates[0],
                    DateDiffers = datesDiffer,
                    BillNo = erp.BillNo,
                    Ledger = erp.Ledger,
                    Party = erp.Party,
                    GstNo = erp.GstNo,
                    GrossAmount = erp.GrossAmount,
                    ErpTaxable = erp.Taxable,
                    ValueAmount = erp.ValueAmount,
                    TwoBTaxable = twoBTaxable,
                    Difference = hits.Count == 0 ? Math.Round(Math.Abs(erp.Taxable), 2) : difference,
                    TotalC = erp.TotalC,
                    OtherAll = erp.OtherAll,
                    Cgst = erp.Cgst,
                    Sgst = erp.Sgst,
                    Igst = erp.Igst,
                    TwoBIgst = twoBIgst,
                    TwoBCgstSgst = twoBCgst,
                    MonthAsPer2B = month,
                    TwoBPeriod = period,
                    DocumentType = docType,
                    Ledgers = erp.Ledgers,
                });
            }
        }

        foreach (var portal in twoBRows.Where(r => !used.Contains(r.Id)))
        {
            output.Add(new GstBillRecoRow
            {
                Status = "Missing in ERP",
                CompanyName = company,
                VoucherType = portal.DocumentType,
                BillDate = portal.BillDate,
                BillNo = portal.BillNo,
                Party = portal.Supplier,
                GstNo = portal.GstNo,
                ErpTaxable = null,
                TwoBTaxable = portal.Taxable,
                Difference = Math.Round(-Math.Abs(portal.Taxable), 2),
                Igst = portal.Igst,
                TwoBIgst = portal.Igst,
                TwoBCgstSgst = portal.CgstSgst,
                MonthAsPer2B = portal.Month,
                TwoBPeriod = portal.Period,
                DocumentType = portal.DocumentType,
            });
        }

        return DropRepeatedJournals(output, tolerance)
            .OrderBy(r => StatusOrder(r.Status))
            .ThenBy(r => r.BillDate)
            .ThenBy(r => r.BillNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Drops a journal still missing in 2B when it repeats a bill already matched:
    /// the opposite amount on the same day, or the same bill number with a short letter prefix.
    /// </summary>
    private static List<GstBillRecoRow> DropRepeatedJournals(List<GstBillRecoRow> rows, decimal tolerance)
    {
        var matched = rows.Where(row => row.Status == "Matched" && row.BillDate != null).ToList();
        if (matched.Count == 0)
            return rows;

        return rows.Where(row => !IsRepeatedJournal(row, matched, tolerance)).ToList();
    }

    private static bool IsRepeatedJournal(GstBillRecoRow open, List<GstBillRecoRow> matched, decimal tolerance)
    {
        if (open.Status != "Missing in 2B" || open.BillDate == null || !IsAdjustmentVoucher(open.VoucherType))
            return false;
        var openTax = open.ErpTaxable ?? 0;
        if (Math.Abs(openTax) <= TaxEpsilon)
            return false;
        var openGst = Math.Abs(open.Cgst ?? 0) + Math.Abs(open.Sgst ?? 0) + Math.Abs(open.Igst ?? 0);

        foreach (var hit in matched)
        {
            if (hit.BillDate!.Value.Date != open.BillDate.Value.Date)
                continue;
            if (!string.Equals(open.GstNo, hit.GstNo, StringComparison.OrdinalIgnoreCase))
                continue;
            var hitTax = hit.ErpTaxable ?? 0;
            if (Math.Abs(Math.Abs(openTax) - Math.Abs(hitTax)) > tolerance)
                continue;
            var hitGst = Math.Abs(hit.Cgst ?? 0) + Math.Abs(hit.Sgst ?? 0) + Math.Abs(hit.Igst ?? 0);
            if (Math.Abs(openGst - hitGst) > Math.Max(tolerance, 1m))
                continue;
            var opposite = Math.Sign(openTax) != Math.Sign(hitTax);
            if (opposite || BillIsPrefixedTwin(open.BillNo, hit.BillNo))
                return true;
        }

        return false;
    }

    private static bool BillIsPrefixedTwin(string? openBill, string? matchedBill)
    {
        var left = MatchBill(openBill);
        var right = MatchBill(matchedBill);
        if (left.Length < 3 || right.Length < 3 || string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return false;
        var longer = left.Length >= right.Length ? left : right;
        var shorter = left.Length >= right.Length ? right : left;
        if (!longer.EndsWith(shorter, StringComparison.OrdinalIgnoreCase))
            return false;
        var prefix = longer[..^shorter.Length];
        return prefix.Length is >= 1 and <= 3 && prefix.All(char.IsLetter);
    }

    private static void SelectBestTaxableCandidates(
        List<(ErpLine Erp, List<TwoBLine> Hits)> cluster,
        decimal target)
    {
        var combinations = 1L;
        foreach (var item in cluster)
        {
            combinations *= Math.Max(1, item.Erp.TaxableCandidates.Count);
            if (combinations > 65_536)
                return;
        }

        var current = new decimal[cluster.Count];
        var best = cluster.Select(item => item.Erp.Taxable).ToArray();
        var bestDifference = Math.Abs(best.Sum(Math.Abs) - target);

        void Visit(int index, decimal total)
        {
            if (index == cluster.Count)
            {
                var candidateDifference = Math.Abs(total - target);
                if (candidateDifference >= bestDifference)
                    return;
                bestDifference = candidateDifference;
                Array.Copy(current, best, current.Length);
                return;
            }

            foreach (var candidate in cluster[index].Erp.TaxableCandidates)
            {
                current[index] = candidate;
                Visit(index + 1, total + Math.Abs(candidate));
            }
        }

        Visit(0, 0);
        for (var i = 0; i < cluster.Count; i++)
            cluster[i].Erp.Taxable = best[i];
    }

    private static bool TaxMatches(
        List<(ErpLine Erp, List<TwoBLine> Hits)> cluster,
        List<TwoBLine> hits,
        decimal tolerance)
    {
        var erpIgst = cluster.Sum(item => Math.Abs(item.Erp.Igst));
        var twoBIgst = hits.Sum(item => Math.Abs(item.Igst));
        var erpCgstSgst = cluster.Sum(item => Math.Abs(item.Erp.Cgst) + Math.Abs(item.Erp.Sgst));
        var twoBCgstSgst = hits.Sum(item => Math.Abs(item.CgstSgst));

        return SameNonZeroAmount(erpIgst, twoBIgst, tolerance)
            || SameNonZeroAmount(erpCgstSgst, twoBCgstSgst, tolerance);
    }

    private static bool SameNonZeroAmount(decimal erp, decimal portal, decimal tolerance) =>
        erp > TaxEpsilon && portal > TaxEpsilon && Math.Abs(erp - portal) <= tolerance;

    public async Task<int> ImportFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new ArgumentException("2B file was not found.");
        await using var stream = File.OpenRead(path);
        var parsed = ParseTwoB(stream);
        if (parsed.Rows.Count == 0)
            throw new ArgumentException("No invoice, credit note, or debit note rows with GST were found.");
        var company = await CompanyNameForGstinAsync(parsed.BuyerGstin, ct);
        return await SaveTwoBAsync(company, parsed, ct);
    }

    private static TwoBParse ParseTwoB(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault(s =>
            Norm(s.Name).Contains("B2B") && (Norm(s.Name).Contains("CNDN") || Norm(s.Name).Contains("CN")))
            ?? workbook.Worksheets.FirstOrDefault(s => Norm(s.Name).Contains("B2B"))
            ?? throw new ArgumentException("The workbook has no Invoice Wise 2B sheet.");

        var headerRow = 0;
        var map = new Dictionary<string, int>();
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 25);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        for (var r = 1; r <= lastRow && headerRow == 0; r++)
        {
            for (var c = 1; c <= lastCol; c++)
            {
                if (Norm(sheet.Cell(r, c).GetString()) == "2BPERIOD")
                {
                    headerRow = r;
                    break;
                }
            }
        }
        if (headerRow == 0)
            throw new ArgumentException("Could not find the 2B Period column. Upload the portal MonthWise 2B workbook.");

        var periodCol = 0;
        for (var c = 1; c <= lastCol; c++)
        {
            if (Norm(sheet.Cell(headerRow, c).GetString()) == "2BPERIOD")
                periodCol = c;
        }

        void TakeLast(string token)
        {
            var at = 0;
            for (var c = 1; c < periodCol; c++)
            {
                if (Norm(sheet.Cell(headerRow, c).GetString()) == token)
                    at = c;
            }
            if (at > 0)
                map[token] = at;
        }

        TakeLast("DOCUMENTTYPE");
        TakeLast("DOCUMENTNUMBER");
        TakeLast("DOCUMENTDATE");
        TakeLast("TAXABLEVALUE");
        TakeLast("IGST");
        TakeLast("CGST+SGST");
        map["2BPERIOD"] = periodCol;

        var supplierCol = ColumnOf(sheet, headerRow, lastCol, "SUPPLIERNAME");
        var gstCol = ColumnOf(sheet, headerRow, lastCol, "GSTIN");
        if (headerRow > 1)
        {
            if (supplierCol == 0)
                supplierCol = ColumnOf(sheet, headerRow - 1, lastCol, "SUPPLIERNAME");
            if (gstCol == 0)
                gstCol = ColumnOf(sheet, headerRow - 1, lastCol, "GSTIN");
        }
        foreach (var required in new[] { "DOCUMENTTYPE", "DOCUMENTNUMBER", "DOCUMENTDATE", "TAXABLEVALUE" })
        {
            if (!map.ContainsKey(required))
                throw new ArgumentException("The 2B sheet is missing " + required + ".");
        }

        var buyerGstin = "";
        for (var r = 1; r < headerRow; r++)
        {
            for (var c = 1; c <= Math.Min(lastCol, 12); c++)
            {
                var gst = NormGst(sheet.Cell(r, c).GetString());
                if (IsGstin(gst))
                    buyerGstin = gst;
            }
        }

        var rows = new List<TwoBLine>();
        var nextId = 1;
        var dataLast = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= dataLast; r++)
        {
            var docType = CellText(sheet.Cell(r, map["DOCUMENTTYPE"]));
            if (!IncludeDocument(docType))
                continue;

            var billNo = NormBill(CellText(sheet.Cell(r, map["DOCUMENTNUMBER"])));
            var gst = gstCol == 0 ? "" : NormGst(CellText(sheet.Cell(r, gstCol)));
            var billDate = CellDate(sheet.Cell(r, map["DOCUMENTDATE"]));
            if (billNo.Length == 0 || gst.Length == 0 || billDate == null)
                continue;

            var period = CellText(sheet.Cell(r, map["2BPERIOD"]));
            var taxable = CellMoney(sheet.Cell(r, map["TAXABLEVALUE"]));
            var igst = map.TryGetValue("IGST", out var igstCol) ? CellMoney(sheet.Cell(r, igstCol)) : 0;
            var cgstSgst = map.TryGetValue("CGST+SGST", out var taxCol) ? CellMoney(sheet.Cell(r, taxCol)) : 0;
            if (Math.Abs(igst) <= TaxEpsilon && Math.Abs(cgstSgst) <= TaxEpsilon)
                continue;

            rows.Add(new TwoBLine
            {
                Id = nextId++,
                BillNo = billNo,
                BillDate = billDate.Value,
                GstNo = gst,
                Key = Key(billNo, billDate.Value, gst),
                Supplier = supplierCol == 0 ? "" : CellText(sheet.Cell(r, supplierCol)),
                DocumentType = docType,
                Taxable = taxable,
                Igst = igst,
                CgstSgst = cgstSgst,
                Period = period,
                Month = MonthLabel(period, billDate.Value),
            });
        }

        return new TwoBParse { BuyerGstin = buyerGstin, Rows = rows };
    }

    private static bool InRange(DateTime billDate, DateTime from, DateTime to)
        => billDate.Date >= from.Date && billDate.Date <= to.Date;

    private static bool TryPeriodMonth(string period, out DateTime monthStart)
    {
        monthStart = default;
        var text = (period ?? "").Trim();
        if (text.Length == 0 || text == "-")
            return false;
        var formats = new[] { "MMM-yy", "MMM-yyyy", "MMMM-yy", "MMMM-yyyy", "MMM yy", "MMMM yyyy" };
        if (!DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;
        monthStart = new DateTime(parsed.Year, parsed.Month, 1);
        return true;
    }

    private static string MonthLabel(string period, DateTime billDate)
    {
        if (TryPeriodMonth(period, out var month))
            return month.ToString("MMM-yy", CultureInfo.InvariantCulture);
        return billDate.ToString("MMM-yy", CultureInfo.InvariantCulture);
    }

    private static bool IncludeDocument(string documentType)
    {
        var token = Norm(documentType);
        if (token.Length == 0 || token == "-")
            return false;
        if (token.Contains("IMPG") || token.Contains("ISD") || token.Contains("BILLOFENTRY"))
            return false;
        return token.Contains("INVOICE") || token.Contains("CREDIT") || token.Contains("DEBIT")
            || token is "CN" or "DN" || token.Contains("CDN");
    }

    private static bool KeepErpTax(decimal cgst, decimal sgst, decimal igst)
    {
        var c = Math.Abs(cgst) > TaxEpsilon;
        var s = Math.Abs(sgst) > TaxEpsilon;
        var i = Math.Abs(igst) > TaxEpsilon;
        return (c && s) || (i && !c && !s);
    }

    private static decimal SumTax(SqlDataReader reader, Dictionary<string, int> ordinal, string tax, bool input)
    {
        decimal total = 0;
        foreach (var pair in ordinal)
        {
            var name = pair.Key;
            if (!name.Contains(tax, StringComparison.Ordinal))
                continue;
            var isInput = name.Contains("INPUT", StringComparison.Ordinal);
            var isOutput = name.Contains("OUTPUT", StringComparison.Ordinal);
            if (input && (!isInput || isOutput))
                continue;
            total += Money(reader, pair.Value) ?? 0;
        }
        return total;
    }

    private static int ColumnOf(IXLWorksheet sheet, int row, int lastCol, string token)
    {
        for (var c = 1; c <= lastCol; c++)
        {
            if (Norm(sheet.Cell(row, c).GetString()) == token)
                return c;
        }
        return 0;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
            return "";
        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var number))
        {
            var rounded = Math.Round(number);
            if (Math.Abs(number - rounded) < 0.0000001)
                return rounded.ToString("0", CultureInfo.InvariantCulture);
        }
        return cell.GetString().Trim();
    }

    private static decimal CellMoney(IXLCell cell)
    {
        if (cell.IsEmpty())
            return 0;
        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var number))
            return Convert.ToDecimal(number);
        var text = cell.GetString().Trim().Replace(",", "");
        if (text.Length == 0 || text == "-")
            return 0;
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static DateTime? CellDate(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;
        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var date))
            return date.Date;
        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var serial) && serial > 20000 && serial < 80000)
            return DateTime.FromOADate(serial).Date;
        return ParseDate(cell.GetString());
    }

    private static DateTime? ParseDate(string? text)
    {
        var value = (text ?? "").Trim();
        if (value.Length == 0 || value == "-")
            return null;
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out var local))
            return local.Date;
        return null;
    }

    private static string Text(SqlDataReader reader, Dictionary<string, int> ordinal, string name)
    {
        if (!ordinal.TryGetValue(name, out var at) || reader.IsDBNull(at))
            return "";
        return Convert.ToString(reader.GetValue(at))?.Trim() ?? "";
    }

    private static DateTime? DateOf(SqlDataReader reader, Dictionary<string, int> ordinal, string name)
    {
        if (!ordinal.TryGetValue(name, out var at) || reader.IsDBNull(at))
            return null;
        var value = reader.GetValue(at);
        if (value is DateTime date)
            return date.Date;
        return ParseDate(Convert.ToString(value));
    }

    private static decimal? MoneyAt(SqlDataReader reader, Dictionary<string, int> ordinal, string name)
    {
        return ordinal.TryGetValue(name, out var at) ? Money(reader, at) : null;
    }

    private static decimal? Money(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static decimal? LedgerAmount(GstBillRecoRow row, string name)
    {
        return row.Ledgers != null && row.Ledgers.TryGetValue(name, out var amount) ? amount : null;
    }

    private static void WriteCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                return;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = "dd-MM-yyyy";
                return;
            case decimal number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            default:
                cell.Value = Convert.ToString(value) ?? "";
                return;
        }
    }

    private static List<string> BillParts(string billNo)
    {
        return billNo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormBill)
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Key(string billNo, DateTime billDate, string gstNo)
        => billNo + "|" + billDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + gstNo;

    /// <summary>
    /// Comparison form of a bill number. Slashes, dashes, and other punctuation are ignored.
    /// A number with fewer than three letters or digits keeps its exact text.
    /// </summary>
    private static string MatchBill(string? value)
    {
        var text = NormBill(value);
        var alnum = new string(text.Where(char.IsLetterOrDigit).ToArray());
        return alnum.Length >= 3 ? alnum : text;
    }

    private static string MatchDateKey(string billNo, DateTime billDate, string gstNo)
        => MatchBill(billNo) + "|" + billDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + gstNo;

    /// <summary>
    /// Matches ERP bills to one portal pool. Unmatched ERP rows stay in the assignment list with no hits.
    /// </summary>
    private static void AssignPortalHits(
        List<ErpLine> erpRows,
        List<TwoBLine> twoBRows,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var twoBGroups = twoBRows
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        var pending = new List<ErpLine>();
        foreach (var group in erpRows.GroupBy(row => row.FullKey))
        {
            if (twoBGroups.TryGetValue(group.Key, out var hits))
            {
                foreach (var hit in hits)
                    claimed.Add(hit.Id);
                foreach (var erp in group)
                    assignments.Add((erp, hits));
            }
            else
            {
                pending.AddRange(group);
            }
        }

        var twoBByMatchDate = twoBRows
            .GroupBy(row => MatchDateKey(row.BillNo, row.BillDate, row.GstNo))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var afterNumber = new List<ErpLine>();
        foreach (var group in pending.GroupBy(row => MatchDateKey(row.BillNo, row.BillDate, row.GstNo)))
        {
            if (twoBByMatchDate.TryGetValue(group.Key, out var hits))
            {
                var open = hits.Where(hit => !claimed.Contains(hit.Id)).ToList();
                if (open.Count > 0)
                {
                    foreach (var hit in open)
                        claimed.Add(hit.Id);
                    foreach (var erp in group)
                        assignments.Add((erp, open));
                    continue;
                }
            }
            afterNumber.AddRange(group);
        }

        var afterParts = new List<ErpLine>();
        foreach (var erp in afterNumber)
        {
            var hits = new List<TwoBLine>();
            foreach (var part in erp.BillParts)
            {
                if (MatchBill(part) == MatchBill(erp.BillNo))
                    continue;
                if (twoBGroups.TryGetValue(Key(part, erp.BillDate, erp.GstNo), out var exactParts))
                    hits.AddRange(exactParts.Where(hit => !claimed.Contains(hit.Id)));
                if (twoBByMatchDate.TryGetValue(MatchDateKey(part, erp.BillDate, erp.GstNo), out var looseParts))
                    hits.AddRange(looseParts.Where(hit => !claimed.Contains(hit.Id)));
            }
            hits = hits.Distinct().ToList();
            if (hits.Count == 0)
            {
                afterParts.Add(erp);
                continue;
            }
            foreach (var hit in hits)
                claimed.Add(hit.Id);
            assignments.Add((erp, hits));
        }

        AssignNearestDates(afterParts, twoBRows, claimed, assignments, tolerance);
    }

    private const string ReasonRelated = "Related bill number";
    private const string ReasonAsset = "Asset voucher";
    private const string ReasonAssetDate = "Asset voucher, same date";
    private const string ReasonAmount = "Same amount, different bill";
    private const string ReasonReversal = "Reversal netted";

    /// <summary>
    /// Pairs rows that are still missing on both sides when they are the same document written
    /// differently. Each accepted pair keeps a reason so the screen can show how it was handled.
    /// </summary>
    private static void LinkExceptions(
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        List<TwoBLine> portalRows,
        HashSet<int> claimed,
        decimal tolerance)
    {
        var open = new List<ErpLine>();
        assignments.RemoveAll(item =>
        {
            if (item.Hits.Count > 0)
                return false;
            open.Add(item.Erp);
            return true;
        });
        var portal = portalRows.Where(row => !claimed.Contains(row.Id)).ToList();
        if (open.Count == 0 || portal.Count == 0)
        {
            foreach (var erp in open)
                assignments.Add((erp, []));
            return;
        }

        LinkRelatedBills(open, portal, claimed, assignments, tolerance);
        LinkAssetVouchers(open, portal, claimed, assignments, tolerance);
        LinkSameAmount(open, portal, claimed, assignments, tolerance);
        LinkAssetSameDate(open, portal, claimed, assignments, tolerance);
        foreach (var erp in open)
            assignments.Add((erp, []));
    }

    private static void LinkRelatedBills(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var pairs = new List<(int Rank, int Distance, decimal Gap, int ErpIndex, int PortalIndex)>();
        for (var erpIndex = 0; erpIndex < open.Count; erpIndex++)
        {
            var erp = open[erpIndex];
            for (var portalIndex = 0; portalIndex < portal.Count; portalIndex++)
            {
                var hit = portal[portalIndex];
                if (!string.Equals(erp.GstNo, hit.GstNo, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!BillsLookRelated(erp.BillNo, hit.BillNo))
                    continue;
                if (!RelatedTaxOk(erp, hit, tolerance))
                    continue;
                var gap = Math.Abs(Math.Abs(erp.Taxable) - Math.Abs(hit.Taxable));
                pairs.Add((gap <= tolerance ? 0 : 1, Math.Abs((erp.BillDate.Date - hit.BillDate.Date).Days), gap, erpIndex, portalIndex));
            }
        }

        TakeGreedyPairs(open, portal, claimed, assignments, pairs, ReasonRelated);
    }

    private static void LinkAssetVouchers(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var pairs = new List<(ErpLine Erp, TwoBLine Hit)>();
        foreach (var erp in open)
        {
            if (!IsAssetVoucher(erp))
                continue;
            foreach (var hit in portal)
            {
                if (claimed.Contains(hit.Id) || !string.Equals(erp.GstNo, hit.GstNo, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (AssetTaxOk(erp, hit, tolerance))
                    pairs.Add((erp, hit));
            }
        }

        TakeUniquePairs(open, portal, claimed, assignments, pairs, ReasonAsset);
    }

    private static void LinkSameAmount(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var pairs = new List<(ErpLine Erp, TwoBLine Hit)>();
        foreach (var erp in open)
        {
            foreach (var hit in portal)
            {
                if (claimed.Contains(hit.Id) || !string.Equals(erp.GstNo, hit.GstNo, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (BillsLookRelated(erp.BillNo, hit.BillNo))
                    continue;
                if (erp.TaxableCandidates.Any(candidate => Math.Abs(Math.Abs(candidate) - Math.Abs(hit.Taxable)) <= tolerance))
                    pairs.Add((erp, hit));
            }
        }

        TakeUniquePairs(open, portal, claimed, assignments, pairs, ReasonAmount);
    }

    /// <summary>
    /// Asset bills that share a taxable value are left open by the unique-amount rule.
    /// Pair one when the bill date belongs to exactly one open CAP bill and one portal invoice of that amount.
    /// </summary>
    private static void LinkAssetSameDate(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var pairs = new List<(ErpLine Erp, TwoBLine Hit)>();
        foreach (var erp in open)
        {
            if (!IsAssetVoucher(erp))
                continue;
            foreach (var hit in portal)
            {
                if (claimed.Contains(hit.Id) || !string.Equals(erp.GstNo, hit.GstNo, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (erp.BillDate.Date != hit.BillDate.Date)
                    continue;
                if (AssetTaxOk(erp, hit, tolerance))
                    pairs.Add((erp, hit));
            }
        }

        TakeUniquePairs(open, portal, claimed, assignments, pairs, ReasonAssetDate);
    }

    private static void TakeGreedyPairs(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        List<(int Rank, int Distance, decimal Gap, int ErpIndex, int PortalIndex)> pairs,
        string reason)
    {
        pairs.Sort((left, right) =>
        {
            var byRank = left.Rank.CompareTo(right.Rank);
            if (byRank != 0)
                return byRank;
            var byDistance = left.Distance.CompareTo(right.Distance);
            return byDistance != 0 ? byDistance : left.Gap.CompareTo(right.Gap);
        });
        var usedErp = new bool[open.Count];
        var usedPortal = new bool[portal.Count];
        var taken = new List<ErpLine>();
        foreach (var pair in pairs)
        {
            if (usedErp[pair.ErpIndex] || usedPortal[pair.PortalIndex] || claimed.Contains(portal[pair.PortalIndex].Id))
                continue;
            usedErp[pair.ErpIndex] = true;
            usedPortal[pair.PortalIndex] = true;
            var erp = open[pair.ErpIndex];
            var hit = portal[pair.PortalIndex];
            erp.MatchReason = reason;
            claimed.Add(hit.Id);
            assignments.Add((erp, [hit]));
            taken.Add(erp);
        }

        foreach (var erp in taken)
            open.Remove(erp);
    }

    private static void TakeUniquePairs(
        List<ErpLine> open,
        List<TwoBLine> portal,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        List<(ErpLine Erp, TwoBLine Hit)> pairs,
        string reason)
    {
        var byErp = pairs.GroupBy(pair => pair.Erp).Where(group => group.Count() == 1).Select(group => group.First()).ToList();
        var unique = byErp.GroupBy(pair => pair.Hit.Id).Where(group => group.Count() == 1).Select(group => group.First());
        var taken = new List<ErpLine>();
        foreach (var pair in unique)
        {
            if (claimed.Contains(pair.Hit.Id) || !open.Contains(pair.Erp))
                continue;
            pair.Erp.MatchReason = reason;
            claimed.Add(pair.Hit.Id);
            assignments.Add((pair.Erp, [pair.Hit]));
            taken.Add(pair.Erp);
        }

        foreach (var erp in taken)
            open.Remove(erp);
        _ = portal;
    }

    private static string ExceptionStatus(string status, string reason)
    {
        if (reason == ReasonAmount && status is "Matched" or "Mismatch")
            return "Potential match";
        if (reason == ReasonRelated && status == "Mismatch")
            return "Potential match";
        return status;
    }

    private static bool RelatedTaxOk(ErpLine erp, TwoBLine hit, decimal tolerance)
    {
        if (TaxAgrees(erp, [hit], tolerance))
            return true;
        var scale = Math.Max(Math.Abs(erp.Taxable), Math.Abs(hit.Taxable));
        return scale > TaxEpsilon && Math.Abs(Math.Abs(erp.Taxable) - Math.Abs(hit.Taxable)) <= scale * 0.05m;
    }

    private static bool AssetTaxOk(ErpLine erp, TwoBLine hit, decimal tolerance)
    {
        if (!erp.TaxableCandidates.Any(candidate => Math.Abs(Math.Abs(candidate) - Math.Abs(hit.Taxable)) <= tolerance))
            return false;
        var erpGst = Math.Abs(erp.Igst) + Math.Abs(erp.Cgst) + Math.Abs(erp.Sgst);
        var portalGst = Math.Abs(hit.Igst) + Math.Abs(hit.CgstSgst);
        return Math.Abs(erpGst - portalGst) <= Math.Max(tolerance, 1m);
    }

    private static bool IsAssetVoucher(ErpLine erp) =>
        NormBill(erp.BillNo).StartsWith("CAP", StringComparison.Ordinal);

    /// <summary>
    /// Same rule as ledger reconciliation: the serial matches after the financial year and
    /// leading zeros are removed. A prefix may contain the other prefix (AG and G).
    /// </summary>
    private static bool BillsLookRelated(string? billNoA, string? billNoB)
    {
        if (string.IsNullOrWhiteSpace(billNoA) || string.IsNullOrWhiteSpace(billNoB))
            return false;
        if (string.Equals(MatchBill(billNoA), MatchBill(billNoB), StringComparison.OrdinalIgnoreCase))
            return true;

        var tokensA = TokenizeBillNo(billNoA);
        var tokensB = TokenizeBillNo(billNoB);
        if (tokensA.Numeric.Count == 0 || tokensB.Numeric.Count == 0)
            return false;
        if (!HasStrongNumericCore(tokensA.Numeric, tokensB.Numeric))
            return false;
        if (tokensA.Alpha.Count > 0 && tokensB.Alpha.Count > 0 && !AlphasCompatible(tokensA.Alpha, tokensB.Alpha))
            return false;
        return BillSerials(billNoA).Overlaps(BillSerials(billNoB));
    }

    private static (HashSet<string> Numeric, HashSet<string> Alpha) TokenizeBillNo(string value)
    {
        var numeric = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var alpha = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var text = value.ToLowerInvariant();
        var index = 0;
        while (index < text.Length)
        {
            if (char.IsLetter(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsLetter(text[index]))
                    index++;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
                alpha.Add(text[start..index]);
            }
            else if (char.IsDigit(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
                numeric.Add(StripLeadingZeros(text[start..index]));
            }
            else
            {
                index++;
            }
        }

        return (numeric, alpha);
    }

    private static bool AlphasCompatible(HashSet<string> left, HashSet<string> right)
    {
        if (left.Overlaps(right))
            return true;
        foreach (var a in left)
        {
            foreach (var b in right)
            {
                if (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static bool HasStrongNumericCore(HashSet<string> numsA, HashSet<string> numsB)
    {
        var primaryA = PrimaryBillToken(numsA);
        var primaryB = PrimaryBillToken(numsB);
        if (primaryA.Length > 0 && primaryB.Length > 0 && !string.Equals(primaryA, primaryB, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!numsA.Overlaps(numsB))
            return false;
        var fyA = FinancialYearTokens(numsA);
        var fyB = FinancialYearTokens(numsB);
        return fyA.Count == 0 || fyB.Count == 0 || fyA.Overlaps(fyB);
    }

    private static string PrimaryBillToken(HashSet<string> numericTokens)
    {
        return numericTokens
            .Select(token => int.TryParse(token, out var value) ? (Token: token, Value: value, Len: token.Length) : (Token: "", Value: -1, Len: 0))
            .Where(item => item.Value >= 100 || item.Len >= 3)
            .OrderByDescending(item => item.Value)
            .Select(item => item.Token)
            .FirstOrDefault() ?? "";
    }

    private static HashSet<string> FinancialYearTokens(HashSet<string> numericTokens)
    {
        var years = numericTokens
            .Where(token => token.Length <= 2 && int.TryParse(token, out var year) && year is >= 0 and <= 99)
            .Select(int.Parse)
            .Distinct()
            .ToList();
        var pairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var year in years)
        {
            if (years.Contains(year + 1) || years.Contains(year - 1))
                pairs.Add(year.ToString("00"));
        }

        return pairs;
    }

    private static HashSet<string> BillSerials(string? value)
    {
        var numbers = new List<string>();
        var text = value ?? "";
        var index = 0;
        while (index < text.Length)
        {
            if (char.IsLetter(text[index]))
            {
                while (index < text.Length && char.IsLetter(text[index]))
                    index++;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
            }
            else if (char.IsDigit(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
                numbers.Add(StripLeadingZeros(text[start..index]));
            }
            else
            {
                index++;
            }
        }

        for (var k = 0; k + 1 < numbers.Count; k++)
        {
            if (numbers[k].Length > 2 || numbers[k + 1].Length > 2)
                continue;
            if (int.Parse(numbers[k + 1]) != int.Parse(numbers[k]) + 1)
                continue;
            numbers.RemoveRange(k, 2);
            break;
        }

        return new HashSet<string>(numbers, StringComparer.OrdinalIgnoreCase);
    }

    private static string StripLeadingZeros(string digits)
    {
        var trimmed = digits.TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    /// <summary>
    /// Links a remaining ERP bill to the nearest portal date when the bill number and GSTIN agree
    /// and the taxable value or the GST amount is within tolerance.
    /// </summary>
    private static void AssignNearestDates(
        List<ErpLine> erpRows,
        List<TwoBLine> twoBRows,
        HashSet<int> claimed,
        List<(ErpLine Erp, List<TwoBLine> Hits)> assignments,
        decimal tolerance)
    {
        var portalGroups = twoBRows
            .Where(row => !claimed.Contains(row.Id))
            .GroupBy(row => row.Key)
            .Select(group => group.ToList())
            .ToList();
        var portalByBill = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (var portalIndex = 0; portalIndex < portalGroups.Count; portalIndex++)
        {
            var portal = portalGroups[portalIndex][0];
            var bucket = MatchBill(portal.BillNo) + "|" + portal.GstNo;
            if (!portalByBill.TryGetValue(bucket, out var indexes))
            {
                indexes = [];
                portalByBill[bucket] = indexes;
            }
            indexes.Add(portalIndex);
        }
        var pairs = new List<(int Distance, decimal Gap, int ErpIndex, int PortalIndex)>();
        for (var erpIndex = 0; erpIndex < erpRows.Count; erpIndex++)
        {
            var erp = erpRows[erpIndex];
            var bill = MatchBill(erp.BillNo);
            if (bill.Length < 3)
                continue;
            if (!portalByBill.TryGetValue(bill + "|" + erp.GstNo, out var indexes))
                continue;
            foreach (var portalIndex in indexes)
            {
                var hits = portalGroups[portalIndex];
                if (!TaxAgrees(erp, hits, tolerance))
                    continue;
                var distance = Math.Abs((erp.BillDate.Date - hits[0].BillDate.Date).Days);
                if (distance == 0)
                    continue;
                var gap = Math.Abs(Math.Abs(erp.Taxable) - hits.Sum(hit => Math.Abs(hit.Taxable)));
                pairs.Add((distance, gap, erpIndex, portalIndex));
            }
        }

        pairs.Sort((left, right) =>
        {
            var byDistance = left.Distance.CompareTo(right.Distance);
            return byDistance != 0 ? byDistance : left.Gap.CompareTo(right.Gap);
        });
        var usedErp = new bool[erpRows.Count];
        var usedPortal = new bool[portalGroups.Count];
        foreach (var pair in pairs)
        {
            if (usedErp[pair.ErpIndex] || usedPortal[pair.PortalIndex])
                continue;
            usedErp[pair.ErpIndex] = true;
            usedPortal[pair.PortalIndex] = true;
            var hits = portalGroups[pair.PortalIndex];
            foreach (var hit in hits)
                claimed.Add(hit.Id);
            assignments.Add((erpRows[pair.ErpIndex], hits));
        }

        for (var index = 0; index < erpRows.Count; index++)
        {
            if (!usedErp[index])
                assignments.Add((erpRows[index], []));
        }
    }

    private static bool TaxAgrees(ErpLine erp, List<TwoBLine> hits, decimal tolerance)
    {
        var portalTaxable = hits.Sum(hit => Math.Abs(hit.Taxable));
        if (erp.TaxableCandidates.Any(candidate => Math.Abs(Math.Abs(candidate) - portalTaxable) <= tolerance))
            return true;
        return TaxMatches([(erp, hits)], hits, tolerance);
    }

    private static string NormBill(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var text = value.Trim().ToUpperInvariant().Replace(" ", "");
        return text == "-" ? "" : text;
    }

    private static bool IsGstin(string value)
        => value.Length == 15 && System.Text.RegularExpressions.Regex.IsMatch(value, "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][A-Z0-9]Z[A-Z0-9]$");

    private static string NormGst(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var text = new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
        return text is "-" or "UN-REGISTER" or "UNREGISTER" ? "" : text;
    }

    private static string Norm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
    }

    private static int StatusOrder(string status) => status switch
    {
        "Mismatch" => 0,
        "Potential match" => 1,
        "Missing in ERP" => 2,
        "Missing in 2B" => 3,
        _ => 3,
    };

    private sealed class TwoBParse
    {
        public string BuyerGstin { get; set; } = "";
        public List<TwoBLine> Rows { get; set; } = [];
    }

    private sealed class CompanyMember
    {
        public string Name { get; set; } = "";
        public string Gstin { get; set; } = "";
    }

    private sealed class TwoBLine
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string BillNo { get; set; } = "";
        public DateTime BillDate { get; set; }
        public string GstNo { get; set; } = "";
        public string Supplier { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public decimal Taxable { get; set; }
        public decimal Igst { get; set; }
        public decimal CgstSgst { get; set; }
        public string Period { get; set; } = "";
        public string Month { get; set; } = "";
    }

    private sealed class PurchaseBill
    {
        public string BillNo { get; set; } = "";
        public DateTime BillDate { get; set; }
        public string GstNo { get; set; } = "";
        public string Party { get; set; } = "";
        public string Ledger { get; set; } = "";
        public string VoucherType { get; set; } = "";
        public string VoucherNo { get; set; } = "";
        public DateTime? VoucherDate { get; set; }
        public string RefNo { get; set; } = "";
        public decimal VoucherWeight { get; set; } = -1;
        public decimal Basic { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Freight { get; set; }
        public decimal Other { get; set; }
        public decimal Loading { get; set; }
        public decimal Packing { get; set; }
        public decimal GrossAmount { get; set; }
    }

    private sealed class ErpLine
    {
        public string CompanyName { get; set; } = "";
        public string FullKey { get; set; } = "";
        public string BillNo { get; set; } = "";
        public List<string> BillParts { get; set; } = [];
        public DateTime BillDate { get; set; }
        public string GstNo { get; set; } = "";
        public string Party { get; set; } = "";
        public string VoucherType { get; set; } = "";
        public string VoucherNo { get; set; } = "";
        public DateTime? VoucherDate { get; set; }
        public string RefNo { get; set; } = "";
        public string Ledger { get; set; } = "";
        public decimal? GrossAmount { get; set; }
        public decimal? ValueAmount { get; set; }
        public decimal Taxable { get; set; }
        public List<decimal> TaxableCandidates { get; set; } = [];
        public decimal? TotalC { get; set; }
        public decimal? OtherAll { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public string MatchReason { get; set; } = "";
        public Dictionary<string, decimal?> Ledgers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
