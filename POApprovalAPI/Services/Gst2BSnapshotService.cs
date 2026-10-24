using System.Data;
using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Stores one frozen GSTR-2B period per buyer GSTIN. A later download of the same month is ignored.
/// </summary>
public class Gst2BSnapshotService
{
    private static readonly HashSet<string> PurchaseSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "B2B", "B2BA", "B2B-CDNR", "B2B-CDNRA", "B2B-DNR", "B2B-DNRA"
    };

    private readonly DatabaseService _database;

    public Gst2BSnapshotService(DatabaseService database)
    {
        _database = database;
    }

    public static bool IsPurchaseSection(string section)
    {
        if (PurchaseSections.Contains(section.Trim()))
            return true;
        var token = Token(section);
        if (token.Contains("REJECT", StringComparison.Ordinal) || token.Contains("REVERSAL", StringComparison.Ordinal))
            return false;
        return token is "B2B" or "B2BA" || token.Contains("CDNR", StringComparison.Ordinal) || token is "B2BDNR" or "B2BDNRA";
    }

    public static bool TryRead(Stream stream, out Gst2BSnapshot snapshot)
    {
        snapshot = null!;
        if (!stream.CanSeek)
            return false;
        var origin = stream.Position;
        try
        {
            using var workbook = new XLWorkbook(stream);
            if (!IsPortalWorkbook(workbook))
                return false;
            snapshot = ReadWorkbook(workbook);
            return true;
        }
        finally
        {
            stream.Position = origin;
        }
    }

    public async Task<Gst2BSaveResult> SavePortalFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (!TryRead(stream, out var snapshot))
            throw new ArgumentException("This workbook is not a monthly GSTR-2B portal file.");
        return await SaveAsync(snapshot, fileName, ct);
    }

    public async Task<Gst2BSaveResult> SaveAsync(Gst2BSnapshot snapshot, string fileName, CancellationToken ct = default)
    {
        var buyer = NormGst(snapshot.BuyerGstin);
        if (buyer.Length == 0)
            throw new ArgumentException("The GSTR-2B file has no buyer GSTIN.");

        await using var connection = _database.CreateConnection();
        await EnsureStoreAsync(connection, ct);
        var group = await GroupForGstinAsync(connection, buyer, ct);
        if (group.Length == 0)
            throw new ArgumentException($"GSTIN {buyer} is not on a company in FactoryInfo.");

        await using var existing = connection.CreateCommand();
        existing.CommandText = @"
SELECT COUNT(*)
FROM dbo.AppGst2BPeriod
WHERE BuyerGstin = @Buyer AND ReturnPeriod = @Period";
        existing.Parameters.Add("@Buyer", SqlDbType.VarChar, 20).Value = buyer;
        existing.Parameters.Add("@Period", SqlDbType.Date).Value = snapshot.ReturnPeriod;
        var already = Convert.ToInt32(await existing.ExecuteScalarAsync(ct) ?? 0, CultureInfo.InvariantCulture) > 0;
        if (already)
        {
            return new Gst2BSaveResult
            {
                AlreadyFrozen = true,
                BuyerGstin = buyer,
                GroupName = group,
                ReturnPeriod = snapshot.ReturnPeriod
            };
        }

        var table = DocumentTable();
        foreach (var document in snapshot.Documents)
        {
            var row = table.NewRow();
            row["BuyerGstin"] = buyer;
            row["GroupName"] = Trim(group, 150);
            row["ReturnPeriod"] = snapshot.ReturnPeriod;
            row["Section"] = Trim(document.Section, 40);
            row["SupplierGstin"] = Trim(document.SupplierGstin, 20);
            row["SupplierName"] = Trim(document.SupplierName, 300);
            row["DocumentType"] = Trim(document.DocumentType, 40);
            row["InvoiceType"] = Trim(document.InvoiceType, 40);
            row["DocumentNumber"] = Trim(document.DocumentNumber, 200);
            row["DocumentNumberNorm"] = Trim(document.DocumentNumberNorm, 200);
            row["DocumentDate"] = document.DocumentDate;
            row["OriginalDocumentNumber"] = Trim(document.OriginalDocumentNumber, 200);
            row["OriginalDocumentDate"] = (object?)document.OriginalDocumentDate ?? DBNull.Value;
            row["PlaceOfSupply"] = Trim(document.PlaceOfSupply, 80);
            row["ReverseCharge"] = Trim(document.ReverseCharge, 10);
            row["DocumentValue"] = Money(document.DocumentValue);
            row["Taxable"] = Money(document.Taxable);
            row["Igst"] = Money(document.Igst);
            row["Cgst"] = Money(document.Cgst);
            row["Sgst"] = Money(document.Sgst);
            row["Cess"] = Money(document.Cess);
            row["ItcAvailability"] = Trim(document.ItcAvailability, 20);
            row["ItcReason"] = Trim(document.ItcReason, 200);
            row["ApplicableTaxRate"] = Trim(document.ApplicableTaxRate, 20);
            row["ItcReduced"] = Trim(document.ItcReduced, 20);
            row["ReductionIgst"] = Money(document.ReductionIgst);
            row["ReductionCgst"] = Money(document.ReductionCgst);
            row["ReductionSgst"] = Money(document.ReductionSgst);
            row["ReductionCess"] = Money(document.ReductionCess);
            row["Source"] = Trim(document.Source, 40);
            row["Irn"] = Trim(document.Irn, 80);
            row["IrnDate"] = (object?)document.IrnDate ?? DBNull.Value;
            row["Gstr1Period"] = Trim(document.Gstr1Period, 20);
            row["Gstr1FilingDate"] = (object?)document.Gstr1FilingDate ?? DBNull.Value;
            row["Remarks"] = Trim(document.Remarks, 300);
            table.Rows.Add(row);
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        try
        {
            await using (var period = connection.CreateCommand())
            {
                period.Transaction = transaction;
                period.CommandText = @"
INSERT INTO dbo.AppGst2BPeriod (BuyerGstin, GroupName, ReturnPeriod, DocumentCount, FileName)
VALUES (@Buyer, @Group, @Period, @Rows, @File)";
                period.Parameters.Add("@Buyer", SqlDbType.VarChar, 20).Value = buyer;
                period.Parameters.Add("@Group", SqlDbType.NVarChar, 150).Value = Trim(group, 150);
                period.Parameters.Add("@Period", SqlDbType.Date).Value = snapshot.ReturnPeriod;
                period.Parameters.Add("@Rows", SqlDbType.Int).Value = table.Rows.Count;
                period.Parameters.Add("@File", SqlDbType.NVarChar, 260).Value = Trim(fileName, 260);
                await period.ExecuteNonQueryAsync(ct);
            }

            if (table.Rows.Count > 0)
            {
                using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
                bulk.DestinationTableName = "dbo.AppGst2BDocument";
                bulk.BulkCopyTimeout = 180;
                foreach (DataColumn column in table.Columns)
                    bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                await bulk.WriteToServerAsync(table, ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        var sections = snapshot.Documents
            .GroupBy(document => document.Section)
            .OrderBy(group => group.Key)
            .Select(group => group.Key + "=" + group.Count());
        return new Gst2BSaveResult
        {
            InsertedRows = table.Rows.Count,
            BuyerGstin = buyer,
            GroupName = group,
            ReturnPeriod = snapshot.ReturnPeriod,
            Sections = string.Join(", ", sections)
        };
    }

    public async Task<Gst2BCoverage> CoverageAsync(string groupName, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to.Date < from.Date)
            throw new ArgumentException("Date to is before date from.");

        var periods = await GetPeriodsAsync(groupName, ct);
        var stored = periods.Select(period => new DateTime(period.ReturnPeriod.Year, period.ReturnPeriod.Month, 1)).ToHashSet();
        var missing = new List<DateTime>();
        var unavailable = new List<DateTime>();
        var inRange = new List<DateTime>();
        for (var month = new DateTime(from.Year, from.Month, 1); month <= new DateTime(to.Year, to.Month, 1); month = month.AddMonths(1))
        {
            inRange.Add(month);
            if (!IsPeriodAvailable(month))
                unavailable.Add(month);
            else if (!stored.Contains(month))
                missing.Add(month);
        }

        var covered = inRange.Where(stored.Contains).ToList();
        var coverage = new Gst2BCoverage
        {
            NeedsDownload = missing.Count > 0,
            HasStoredMonths = covered.Count > 0,
            MissingMonths = missing
        };
        if (missing.Count == 0 && unavailable.Count == 0)
            coverage.Message = $"{FormatMonthSpan(covered)} already stored. Reconciled from the database.";
        else if (missing.Count == 0)
        {
            var unavailableLabel = FormatMonthSpan(unavailable);
            var verb = unavailable.Count == 1 ? "is" : "are";
            coverage.Message = covered.Count == 0
                ? $"{unavailableLabel} {verb} not on the GST portal yet. GSTR-2B for a month is available from the 14th of the next month."
                : $"{unavailableLabel} {verb} not on the GST portal yet. Reconciled the months already stored.";
        }
        else
            coverage.Message = $"Fetch {FormatMonthSpan(missing)} from GST. Those months are not stored yet.";
        return coverage;
    }

    public static bool IsPeriodAvailable(DateTime month)
    {
        var next = new DateTime(month.Year, month.Month, 1).AddMonths(1);
        return DateTime.Today >= new DateTime(next.Year, next.Month, 14);
    }

    public static string FormatMonthSpan(IReadOnlyList<DateTime> months)
    {
        if (months.Count == 0)
            return "No months";
        var ordered = months.OrderBy(month => month).ToList();
        string Label(DateTime month) => month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        if (ordered.Count == 1)
            return Label(ordered[0]);
        var contiguous = true;
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i] != ordered[i - 1].AddMonths(1))
                contiguous = false;
        }
        if (contiguous)
            return $"{Label(ordered[0])} through {Label(ordered[^1])}";
        return string.Join(", ", ordered.Select(Label));
    }

    public async Task<IReadOnlyList<Gst2BPeriodRow>> GetPeriodsAsync(string groupName, CancellationToken ct = default)
    {
        await using var connection = _database.CreateConnection();
        await EnsureStoreAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT BuyerGstin, GroupName, ReturnPeriod, DocumentCount, FileName, DownloadedAt
FROM dbo.AppGst2BPeriod
WHERE GroupName = @Group
ORDER BY ReturnPeriod, BuyerGstin";
        cmd.Parameters.Add("@Group", SqlDbType.NVarChar, 150).Value = groupName.Trim();
        var rows = new List<Gst2BPeriodRow>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new Gst2BPeriodRow
            {
                BuyerGstin = reader.GetString(0),
                GroupName = reader.GetString(1),
                ReturnPeriod = reader.GetDateTime(2),
                RowCount = reader.GetInt32(3),
                FileName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                DownloadedAt = reader.GetDateTime(5)
            });
        }
        return rows;
    }

    public async Task<IReadOnlyList<Gst2BDocumentRow>> GetDocumentsAsync(
        string groupName,
        DateTime from,
        DateTime to,
        bool purchaseOnly,
        CancellationToken ct = default)
    {
        var periodFrom = new DateTime(from.Year, from.Month, 1);
        var periodTo = new DateTime(to.Year, to.Month, 1);
        await using var connection = _database.CreateConnection();
        await EnsureStoreAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT BuyerGstin, GroupName, ReturnPeriod, Section, SupplierGstin, SupplierName,
       DocumentType, InvoiceType, DocumentNumber, DocumentNumberNorm, DocumentDate,
       OriginalDocumentNumber, OriginalDocumentDate, PlaceOfSupply, ReverseCharge,
       DocumentValue, Taxable, Igst, Cgst, Sgst, CgstSgst, Cess,
       ItcAvailability, ItcReason, ApplicableTaxRate, ItcReduced,
       ReductionIgst, ReductionCgst, ReductionSgst, ReductionCess,
       Source, Irn, IrnDate, Gstr1Period, Gstr1FilingDate, Remarks
FROM dbo.AppGst2BDocument
WHERE GroupName = @Group
  AND ReturnPeriod >= @From
  AND ReturnPeriod <= @To
  AND (@PurchaseOnly = 0 OR Section IN ('B2B','B2BA','B2B-CDNR','B2B-CDNRA','B2B-DNR','B2B-DNRA'))
ORDER BY ReturnPeriod, Section, SupplierName, DocumentDate, DocumentNumber";
        cmd.Parameters.Add("@Group", SqlDbType.NVarChar, 150).Value = groupName.Trim();
        cmd.Parameters.Add("@From", SqlDbType.Date).Value = periodFrom;
        cmd.Parameters.Add("@To", SqlDbType.Date).Value = periodTo;
        cmd.Parameters.Add("@PurchaseOnly", SqlDbType.Bit).Value = purchaseOnly;
        var rows = new List<Gst2BDocumentRow>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(ReadDocument(reader));
        return rows;
    }

    private static Gst2BDocumentRow ReadDocument(SqlDataReader reader)
    {
        return new Gst2BDocumentRow
        {
            BuyerGstin = reader.GetString(0),
            GroupName = reader.GetString(1),
            ReturnPeriod = reader.GetDateTime(2),
            Section = reader.GetString(3),
            SupplierGstin = reader.GetString(4),
            SupplierName = reader.GetString(5),
            DocumentType = reader.GetString(6),
            InvoiceType = reader.GetString(7),
            DocumentNumber = reader.GetString(8),
            DocumentNumberNorm = reader.GetString(9),
            DocumentDate = reader.GetDateTime(10),
            OriginalDocumentNumber = reader.GetString(11),
            OriginalDocumentDate = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            PlaceOfSupply = reader.GetString(13),
            ReverseCharge = reader.GetString(14),
            DocumentValue = reader.GetDecimal(15),
            Taxable = reader.GetDecimal(16),
            Igst = reader.GetDecimal(17),
            Cgst = reader.GetDecimal(18),
            Sgst = reader.GetDecimal(19),
            CgstSgst = reader.GetDecimal(20),
            Cess = reader.GetDecimal(21),
            ItcAvailability = reader.GetString(22),
            ItcReason = reader.GetString(23),
            ApplicableTaxRate = reader.GetString(24),
            ItcReduced = reader.GetString(25),
            ReductionIgst = reader.GetDecimal(26),
            ReductionCgst = reader.GetDecimal(27),
            ReductionSgst = reader.GetDecimal(28),
            ReductionCess = reader.GetDecimal(29),
            Source = reader.GetString(30),
            Irn = reader.GetString(31),
            IrnDate = reader.IsDBNull(32) ? null : reader.GetDateTime(32),
            Gstr1Period = reader.GetString(33),
            Gstr1FilingDate = reader.IsDBNull(34) ? null : reader.GetDateTime(34),
            Remarks = reader.GetString(35)
        };
    }

    private static async Task EnsureStoreAsync(SqlConnection connection, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
IF OBJECT_ID('dbo.AppGst2BPeriod', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppGst2BPeriod (
        BuyerGstin varchar(20) NOT NULL,
        GroupName nvarchar(150) NOT NULL,
        ReturnPeriod date NOT NULL,
        DocumentCount int NOT NULL,
        FileName nvarchar(260) NULL,
        DownloadedAt datetime NOT NULL CONSTRAINT DF_AppGst2BPeriod_DownloadedAt DEFAULT GETDATE(),
        Frozen bit NOT NULL CONSTRAINT DF_AppGst2BPeriod_Frozen DEFAULT 1,
        CONSTRAINT PK_AppGst2BPeriod PRIMARY KEY (BuyerGstin, ReturnPeriod)
    );
    CREATE INDEX IX_AppGst2BPeriod_Group ON dbo.AppGst2BPeriod (GroupName, ReturnPeriod);
END
IF OBJECT_ID('dbo.AppGst2BDocument', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppGst2BDocument (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BuyerGstin varchar(20) NOT NULL,
        GroupName nvarchar(150) NOT NULL,
        ReturnPeriod date NOT NULL,
        Section varchar(40) NOT NULL,
        SupplierGstin varchar(20) NOT NULL,
        SupplierName nvarchar(300) NOT NULL,
        DocumentType nvarchar(40) NOT NULL,
        InvoiceType nvarchar(40) NOT NULL,
        DocumentNumber nvarchar(200) NOT NULL,
        DocumentNumberNorm nvarchar(200) NOT NULL,
        DocumentDate date NOT NULL,
        OriginalDocumentNumber nvarchar(200) NOT NULL,
        OriginalDocumentDate date NULL,
        PlaceOfSupply nvarchar(80) NOT NULL,
        ReverseCharge varchar(10) NOT NULL,
        DocumentValue decimal(18,2) NOT NULL,
        Taxable decimal(18,2) NOT NULL,
        Igst decimal(18,2) NOT NULL,
        Cgst decimal(18,2) NOT NULL,
        Sgst decimal(18,2) NOT NULL,
        CgstSgst AS CONVERT(decimal(18,2), Cgst + Sgst) PERSISTED,
        Cess decimal(18,2) NOT NULL,
        ItcAvailability varchar(20) NOT NULL,
        ItcReason nvarchar(200) NOT NULL,
        ApplicableTaxRate varchar(20) NOT NULL,
        ItcReduced varchar(20) NOT NULL,
        ReductionIgst decimal(18,2) NOT NULL,
        ReductionCgst decimal(18,2) NOT NULL,
        ReductionSgst decimal(18,2) NOT NULL,
        ReductionCess decimal(18,2) NOT NULL,
        Source nvarchar(40) NOT NULL,
        Irn varchar(80) NOT NULL,
        IrnDate date NULL,
        Gstr1Period varchar(20) NOT NULL,
        Gstr1FilingDate date NULL,
        Remarks nvarchar(300) NOT NULL,
        CONSTRAINT UQ_AppGst2BDocument UNIQUE (BuyerGstin, ReturnPeriod, Section, SupplierGstin, DocumentNumberNorm, DocumentDate, DocumentType)
    );
    CREATE INDEX IX_AppGst2BDocument_GroupPeriod ON dbo.AppGst2BDocument (GroupName, ReturnPeriod, Section);
END";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<string> GroupForGstinAsync(SqlConnection connection, string gstin, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT TOP 1 COALESCE(NULLIF(LTRIM(RTRIM(GroupName)), ''), LTRIM(RTRIM(Name)))
FROM FactoryInfo WITH (NOLOCK)
WHERE REPLACE(UPPER(ISNULL(NewGSTNo, '')), ' ', '') = @Gstin
  AND ISNULL(LTRIM(RTRIM(Name)), '') <> ''
ORDER BY CASE WHEN NULLIF(LTRIM(RTRIM(GroupName)), '') IS NULL THEN 1 ELSE 0 END, Name";
        cmd.Parameters.Add("@Gstin", SqlDbType.VarChar, 20).Value = gstin;
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value == DBNull.Value ? "" : Convert.ToString(value)?.Trim() ?? "";
    }

    private static DataTable DocumentTable()
    {
        var table = new DataTable();
        table.Columns.Add("BuyerGstin", typeof(string));
        table.Columns.Add("GroupName", typeof(string));
        table.Columns.Add("ReturnPeriod", typeof(DateTime));
        table.Columns.Add("Section", typeof(string));
        table.Columns.Add("SupplierGstin", typeof(string));
        table.Columns.Add("SupplierName", typeof(string));
        table.Columns.Add("DocumentType", typeof(string));
        table.Columns.Add("InvoiceType", typeof(string));
        table.Columns.Add("DocumentNumber", typeof(string));
        table.Columns.Add("DocumentNumberNorm", typeof(string));
        table.Columns.Add("DocumentDate", typeof(DateTime));
        table.Columns.Add("OriginalDocumentNumber", typeof(string));
        table.Columns.Add("OriginalDocumentDate", typeof(DateTime));
        table.Columns.Add("PlaceOfSupply", typeof(string));
        table.Columns.Add("ReverseCharge", typeof(string));
        table.Columns.Add("DocumentValue", typeof(decimal));
        table.Columns.Add("Taxable", typeof(decimal));
        table.Columns.Add("Igst", typeof(decimal));
        table.Columns.Add("Cgst", typeof(decimal));
        table.Columns.Add("Sgst", typeof(decimal));
        table.Columns.Add("Cess", typeof(decimal));
        table.Columns.Add("ItcAvailability", typeof(string));
        table.Columns.Add("ItcReason", typeof(string));
        table.Columns.Add("ApplicableTaxRate", typeof(string));
        table.Columns.Add("ItcReduced", typeof(string));
        table.Columns.Add("ReductionIgst", typeof(decimal));
        table.Columns.Add("ReductionCgst", typeof(decimal));
        table.Columns.Add("ReductionSgst", typeof(decimal));
        table.Columns.Add("ReductionCess", typeof(decimal));
        table.Columns.Add("Source", typeof(string));
        table.Columns.Add("Irn", typeof(string));
        table.Columns.Add("IrnDate", typeof(DateTime));
        table.Columns.Add("Gstr1Period", typeof(string));
        table.Columns.Add("Gstr1FilingDate", typeof(DateTime));
        table.Columns.Add("Remarks", typeof(string));
        return table;
    }

    private static bool IsPortalWorkbook(XLWorkbook workbook) =>
        workbook.Worksheets.Any(sheet => sheet.Name.Equals("Read me", StringComparison.OrdinalIgnoreCase))
        && workbook.Worksheets.Any(sheet => sheet.Name.Equals("B2B", StringComparison.OrdinalIgnoreCase));

    private static Gst2BSnapshot ReadWorkbook(XLWorkbook workbook)
    {
        var readMe = workbook.Worksheets.First(sheet => sheet.Name.Equals("Read me", StringComparison.OrdinalIgnoreCase));
        var buyer = NormGst(LabelValue(readMe, "GSTIN"));
        var period = ReturnPeriod(LabelValue(readMe, "FINANCIALYEAR"), LabelValue(readMe, "TAXPERIOD"));
        var documents = new List<Gst2BDocumentRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in workbook.Worksheets)
        {
            if (IsSummarySheet(sheet.Name) || !TryFindHeader(sheet, out var headerRow))
                continue;
            foreach (var document in ReadSheet(sheet, headerRow, period, buyer))
            {
                var key = string.Join("|", document.Section, document.SupplierGstin, document.DocumentNumberNorm, document.DocumentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), document.DocumentType);
                if (!seen.Add(key))
                    continue;
                documents.Add(document);
            }
        }

        return new Gst2BSnapshot
        {
            BuyerGstin = buyer,
            ReturnPeriod = period,
            Documents = documents
        };
    }

    private static IEnumerable<Gst2BDocumentRow> ReadSheet(IXLWorksheet sheet, int headerRow, DateTime period, string buyer)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var above = Fill(sheet, headerRow - 1, lastColumn);
        var higher = RowContains(sheet, headerRow - 2, lastColumn, "REVISED")
            ? Fill(sheet, headerRow - 2, lastColumn)
            : new string[lastColumn + 1];
        var headers = new string[lastColumn + 1];
        for (var column = 1; column <= lastColumn; column++)
        {
            var token = Token(CellText(sheet.Cell(headerRow, column)));
            if (token.Length == 0 && headerRow > 1)
                token = Token(CellText(sheet.Cell(headerRow - 1, column)));
            headers[column] = token;
        }

        var fallbackType = DefaultDocumentType(sheet.Name);
        for (var rowNumber = headerRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty())
                continue;
            var draft = new Gst2BDocumentRow
            {
                BuyerGstin = buyer,
                ReturnPeriod = period,
                Section = sheet.Name.Trim(),
                DocumentType = fallbackType
            };
            for (var column = 1; column <= lastColumn; column++)
            {
                var text = CellText(sheet.Cell(rowNumber, column));
                if (text.Length == 0)
                    continue;
                var context = higher[column] + above[column];
                Apply(draft, headers[column], context, text, sheet.Cell(rowNumber, column));
            }

            draft.DocumentNumberNorm = NormBill(draft.DocumentNumber);
            if (draft.DocumentNumberNorm.Length == 0 || draft.DocumentDate == default)
                continue;
            if (Token(draft.DocumentNumber) is "INVOICENUMBER" or "NOTENUMBER" or "DOCUMENTNUMBER" or "NUMBER")
                continue;
            draft.SupplierGstin = NormGst(draft.SupplierGstin);
            yield return draft;
        }
    }

    private static void Apply(Gst2BDocumentRow row, string header, string context, string text, IXLCell cell)
    {
        var original = context.Contains("ORIGINAL", StringComparison.Ordinal) && !context.Contains("REVISED", StringComparison.Ordinal);
        var reduction = context.Contains("REDUCTION", StringComparison.Ordinal) || context.Contains("DECLARED", StringComparison.Ordinal);
        switch (header)
        {
            case "GSTINOFSUPPLIER":
            case "GSTINOFECO":
            case "GSTINOFISD":
                row.SupplierGstin = text;
                return;
            case "TRADELEGALNAME":
                row.SupplierName = text;
                return;
            case "INVOICENUMBER":
            case "NOTENUMBER":
            case "DOCUMENTNUMBER":
            case "ISDDOCUMENTNUMBER":
                if (original)
                    row.OriginalDocumentNumber = text;
                else
                    row.DocumentNumber = text;
                return;
            case "NUMBER" when context.Contains("BILL", StringComparison.Ordinal):
                row.DocumentNumber = text;
                if (row.DocumentType.Length == 0)
                    row.DocumentType = "Bill of entry";
                return;
            case "ORIGINALINVOICENUMBER":
                row.OriginalDocumentNumber = text;
                return;
            case "INVOICETYPE":
            case "NOTESUPPLYTYPE":
                row.InvoiceType = text;
                return;
            case "NOTETYPE":
            case "DOCUMENTTYPE":
            case "ISDDOCUMENTTYPE":
                if (!original)
                    row.DocumentType = text;
                return;
            case "INVOICEDATE":
            case "NOTEDATE":
            case "DOCUMENTDATE":
            case "ISDDOCUMENTDATE":
                var when = CellDate(cell) ?? ParseDate(text);
                if (original)
                    row.OriginalDocumentDate = when;
                else if (when != null)
                    row.DocumentDate = when.Value;
                return;
            case "DATE" when context.Contains("BILL", StringComparison.Ordinal):
                var boeDate = CellDate(cell) ?? ParseDate(text);
                if (boeDate != null)
                    row.DocumentDate = boeDate.Value;
                return;
            case "ORIGINALINVOICEDATE":
                row.OriginalDocumentDate = CellDate(cell) ?? ParseDate(text);
                return;
            case "INVOICEVALUE":
            case "NOTEVALUE":
            case "DOCUMENTVALUE":
                row.DocumentValue = CellMoney(cell);
                return;
            case "PLACEOFSUPPLY":
            case "PORTCODE":
                if (row.PlaceOfSupply.Length == 0)
                    row.PlaceOfSupply = text;
                return;
            case "SUPPLYATTRACTREVERSECHARGE":
                row.ReverseCharge = text;
                return;
            case "TAXABLEVALUE":
                row.Taxable = CellMoney(cell);
                return;
            case "INTEGRATEDTAX":
                if (reduction)
                    row.ReductionIgst = CellMoney(cell);
                else
                    row.Igst = CellMoney(cell);
                return;
            case "CENTRALTAX":
                if (reduction)
                    row.ReductionCgst = CellMoney(cell);
                else
                    row.Cgst = CellMoney(cell);
                return;
            case "STATEUTTAX":
                if (reduction)
                    row.ReductionSgst = CellMoney(cell);
                else
                    row.Sgst = CellMoney(cell);
                return;
            case "CESS":
                if (reduction)
                    row.ReductionCess = CellMoney(cell);
                else
                    row.Cess = CellMoney(cell);
                return;
            case "ITCAVAILABILITY":
            case "ELIGIBILITYOFITC":
                row.ItcAvailability = text;
                return;
            case "REASON":
                row.ItcReason = text;
                return;
            case "REMARKS":
                row.Remarks = text;
                return;
            case "WHETHERITCTOBEREDUCED":
            case "WHETHERITCTOBEREDUCEDTAXPAYERSINPUT":
                row.ItcReduced = text;
                return;
            case "SOURCE":
                row.Source = text;
                return;
            case "IRN":
                row.Irn = text;
                return;
            case "IRNDATE":
                row.IrnDate = CellDate(cell) ?? ParseDate(text);
                return;
            case "ICEGATEREFERENCEDATE":
                row.Remarks = row.Remarks.Length == 0 ? "Icegate " + text : row.Remarks;
                return;
        }

        if (header.Contains("TAXRATE", StringComparison.Ordinal))
            row.ApplicableTaxRate = text;
        else if (header.Contains("PERIOD", StringComparison.Ordinal) && (header.Contains("GSTR", StringComparison.Ordinal) || header.Contains("ISD", StringComparison.Ordinal)))
            row.Gstr1Period = text;
        else if (header.Contains("FILINGDATE", StringComparison.Ordinal))
            row.Gstr1FilingDate = CellDate(cell) ?? ParseDate(text);
    }

    private static bool TryFindHeader(IXLWorksheet sheet, out int headerRow)
    {
        headerRow = 0;
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 12);
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        for (var row = 1; row <= lastRow; row++)
        {
            for (var column = 1; column <= lastColumn; column++)
            {
                var token = Token(CellText(sheet.Cell(row, column)));
                if (token is "INVOICENUMBER" or "NOTENUMBER" or "DOCUMENTNUMBER" or "ISDDOCUMENTNUMBER")
                    headerRow = row;
                else if (token == "NUMBER" && row > 1 && Token(CellText(sheet.Cell(row - 1, column))).Contains("BILL", StringComparison.Ordinal))
                    headerRow = row;
            }
        }
        return headerRow > 0;
    }

    private static bool RowContains(IXLWorksheet sheet, int row, int lastColumn, string tokenPart)
    {
        if (row < 1)
            return false;
        for (var column = 1; column <= lastColumn; column++)
        {
            if (Token(CellText(sheet.Cell(row, column))).Contains(tokenPart, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static string[] Fill(IXLWorksheet sheet, int row, int lastColumn)
    {
        var values = new string[lastColumn + 1];
        if (row < 1)
            return values;
        var carry = "";
        for (var column = 1; column <= lastColumn; column++)
        {
            var token = Token(CellText(sheet.Cell(row, column)));
            if (token.Length > 0)
                carry = token;
            values[column] = carry;
        }
        return values;
    }

    private static bool IsSummarySheet(string name)
    {
        var token = Token(name);
        return token is "README" or "ITCAVAILABLE" or "ITCNOTAVAILABLE" or "ITCREVERSAL" or "ITCREJECTED";
    }

    private static string DefaultDocumentType(string section)
    {
        var token = Token(section);
        if (token.Contains("IMPG", StringComparison.Ordinal))
            return "Bill of entry";
        if (token.StartsWith("B2B", StringComparison.Ordinal)
            && !token.Contains("CDN", StringComparison.Ordinal)
            && !token.Contains("DNR", StringComparison.Ordinal))
            return "Invoice";
        return "";
    }

    private static DateTime ReturnPeriod(string financialYear, string taxPeriod)
    {
        var yearText = (financialYear ?? "").Trim();
        var dash = yearText.IndexOf('-');
        if (dash <= 0 || !int.TryParse(yearText[..dash], NumberStyles.None, CultureInfo.InvariantCulture, out var startYear))
            throw new ArgumentException("The GSTR-2B file has no financial year.");
        var monthName = (taxPeriod ?? "").Trim();
        if (!DateTime.TryParseExact(monthName, new[] { "MMMM", "MMM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            throw new ArgumentException("The GSTR-2B file has no tax period.");
        var year = month.Month >= 4 ? startYear : startYear + 1;
        return new DateTime(year, month.Month, 1);
    }

    private static string LabelValue(IXLWorksheet sheet, string label)
    {
        var want = Token(label);
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 20);
        var lastColumn = Math.Min(sheet.LastColumnUsed()?.ColumnNumber() ?? 1, 8);
        for (var row = 1; row <= lastRow; row++)
        {
            for (var column = 1; column <= lastColumn; column++)
            {
                if (Token(CellText(sheet.Cell(row, column))) != want)
                    continue;
                for (var next = column + 1; next <= Math.Min(lastColumn, column + 4); next++)
                {
                    var value = CellText(sheet.Cell(row, next));
                    if (value.Length > 0)
                        return value;
                }
            }
        }
        return "";
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
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static DateTime? CellDate(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var date))
            return date.Date;
        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var serial) && serial > 20000 && serial < 80000)
            return DateTime.FromOADate(serial).Date;
        return null;
    }

    private static DateTime? ParseDate(string? text)
    {
        var value = (text ?? "").Trim();
        if (value.Length == 0 || value == "-")
            return null;
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;
        return null;
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Trim(string? value, int length)
    {
        var text = value ?? "";
        return text.Length <= length ? text : text[..length];
    }

    public static string NormBill(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var text = value.Trim().ToUpperInvariant().Replace(" ", "");
        return text == "-" ? "" : text;
    }

    private static string NormGst(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var text = new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
        return text is "-" or "UN-REGISTER" or "UNREGISTER" ? "" : text;
    }

    private static string Token(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}

public sealed class Gst2BSnapshot
{
    public string BuyerGstin { get; set; } = "";
    public DateTime ReturnPeriod { get; set; }
    public List<Gst2BDocumentRow> Documents { get; set; } = [];
}
