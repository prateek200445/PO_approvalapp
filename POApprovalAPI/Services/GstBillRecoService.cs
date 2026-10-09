using System.Data;
using System.Globalization;
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

    private readonly DatabaseService _database;

    public GstBillRecoService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync(CancellationToken ct = default)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT Name
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(Name, '') <> ''
ORDER BY Name";
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
        if (company.Length > 100)
            throw new ArgumentException("Company name is longer than the GST summary allows.");
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (to < from)
            throw new ArgumentException("Date to is before date from.");
        if (amountTolerance < 0)
            amountTolerance = 0;

        var parsed = ParseTwoB(twoBFile);
        if (parsed.Rows.Count == 0)
            throw new ArgumentException("No invoice, credit note, or debit note rows with GST were found. Use the Invoice Wise 2B (B2B, CN, DN) sheet.");

        var saved = await SaveTwoBAsync(company, parsed, ct);
        var inRange = parsed.Rows.Where(row => InRange(row.BillDate, row.Period, from, to)).ToList();
        if (inRange.Count == 0)
            throw new ArgumentException($"Saved {saved} portal bills. None of them fall in the selected dates.");

        var companyGstin = await LoadCompanyGstinAsync(company, ct);
        var erpRows = await LoadErpAsync(company, from, to, ct);

        var result = new GstBillRecoResult
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
            AmountTolerance = amountTolerance,
            BuyerGstin = parsed.BuyerGstin,
            CompanyGstin = companyGstin,
            TwoBRows = inRange.Count,
            SavedRows = saved,
            ErpRows = erpRows.Count,
        };

        if (!string.IsNullOrEmpty(parsed.BuyerGstin) &&
            !string.IsNullOrEmpty(companyGstin) &&
            !string.Equals(parsed.BuyerGstin, companyGstin, StringComparison.OrdinalIgnoreCase))
        {
            result.Warning =
                $"The 2B file is for GSTIN {parsed.BuyerGstin}. {company} is {companyGstin}.";
        }

        result.Rows = BuildRows(company, erpRows, inRange, amountTolerance);
        result.Matched = result.Rows.Count(r => r.Status == "Matched");
        result.Mismatch = result.Rows.Count(r => r.Status == "Mismatch");
        result.MissingInErp = result.Rows.Count(r => r.Status == "Missing in ERP");
        result.MissingIn2B = result.Rows.Count(r => r.Status == "Missing in 2B");
        return result;
    }

    public byte[] Export(IReadOnlyList<GstBillRecoRow> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new ArgumentException("There are no rows to export.");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Bill Reco");
        var headers = new List<string> { "Status", "Taxable difference", "2B taxable", "2B IGST", "2B CGST + SGST", "Document type" };
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
    /// OtherAll is used when its absolute value is above zero. A value that rounds to 0.00 counts as zero,
    /// and OTHER ALL is used only in that case. Freight and C&amp;F are included by amount.
    /// The sign follows the Other All column that was used.
    /// </summary>
    private static decimal TaxableWithFreight(SqlDataReader reader, int camelAt, int capitalAt)
    {
        var camel = camelAt >= 0 ? Money(reader, camelAt) ?? 0 : 0;
        var capital = capitalAt >= 0 ? Money(reader, capitalAt) ?? 0 : 0;
        var otherAll = Math.Abs(Math.Round(camel, 2)) > 0 ? camel : capital;
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

        var magnitude = Math.Abs(otherAll) + charges;
        return otherAll < 0 ? -magnitude : magnitude;
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

    private async Task<List<ErpLine>> LoadErpAsync(string company, DateTime from, DateTime to, CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "dbo.sp_Columner_All";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = TimeoutSeconds;
        cmd.Parameters.Add("@CompanyName", SqlDbType.VarChar, 100).Value = company;
        cmd.Parameters.Add("@DateFrom", SqlDbType.Date).Value = from;
        cmd.Parameters.Add("@DateTo", SqlDbType.Date).Value = to;
        cmd.Parameters.Add("@LedgerName", SqlDbType.VarChar, 110).Value = DBNull.Value;
        cmd.Parameters.Add("@VoucherType", SqlDbType.VarChar, 100).Value = DBNull.Value;
        cmd.Parameters.Add("@AddVoucherType", SqlDbType.VarChar, 100).Value = DBNull.Value;
        cmd.Parameters.Add("@currType", SqlDbType.VarChar, 10).Value = DBNull.Value;
        cmd.Parameters.Add("@IntSum", SqlDbType.Int).Value = 0;

        var rows = new List<ErpLine>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var ordinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            ordinal[Norm(reader.GetName(i))] = i;

        if (!ordinal.ContainsKey("BILLNO") || !ordinal.ContainsKey("GSTNO"))
            throw new ArgumentException("GST summary did not return bill number and GST number columns.");
        var otherAllAt = FindColumn(reader, "OtherAll");
        var otherAllLedgerAt = FindOtherAllLedger(reader, otherAllAt);
        if (otherAllAt < 0 && otherAllLedgerAt < 0)
            throw new ArgumentException("GST summary did not return an Other All column.");

        while (await reader.ReadAsync(ct))
        {
            var billNo = Text(reader, ordinal, "BILLNO");
            var gst = NormGst(Text(reader, ordinal, "GSTNO"));
            var billDate = DateOf(reader, ordinal, "BILLDATE");
            if (billNo.Length == 0 || gst.Length == 0 || billDate == null)
                continue;

            var cgst = SumTax(reader, ordinal, "CGST", input: true);
            var sgst = SumTax(reader, ordinal, "SGST", input: true);
            var igst = SumTax(reader, ordinal, "IGST", input: true);
            if (!KeepErpTax(cgst, sgst, igst))
                continue;

            var ledgers = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in LedgerColumns)
            {
                if (Norm(name) == "OTHERALL" && otherAllLedgerAt >= 0)
                {
                    ledgers[name] = Money(reader, otherAllLedgerAt);
                    continue;
                }
                if (ordinal.TryGetValue(Norm(name), out var at))
                    ledgers[name] = Money(reader, at);
            }

            rows.Add(new ErpLine
            {
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
                ValueAmount = MoneyAt(reader, ordinal, "VALUE"),
                Taxable = TaxableWithFreight(reader, otherAllAt, otherAllLedgerAt),
                TotalC = MoneyAt(reader, ordinal, "TOTALC"),
                OtherAll = otherAllAt >= 0 ? Money(reader, otherAllAt) : null,
                Cgst = cgst,
                Sgst = sgst,
                Igst = igst,
                Ledgers = ledgers,
            });
        }

        return rows;
    }

    private static List<GstBillRecoRow> BuildRows(
        string company,
        List<ErpLine> erpRows,
        List<TwoBLine> twoBRows,
        decimal tolerance)
    {
        var twoBGroups = twoBRows
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        var claimed = new HashSet<int>();
        var assignments = new List<(ErpLine Erp, List<TwoBLine> Hits)>();
        var pendingParts = new List<ErpLine>();
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
                pendingParts.AddRange(group);
            }
        }

        foreach (var erp in pendingParts)
        {
            var hits = new List<TwoBLine>();
            foreach (var part in erp.BillParts)
            {
                if (part == NormBill(erp.BillNo))
                    continue;
                if (!twoBGroups.TryGetValue(Key(part, erp.BillDate, erp.GstNo), out var partHits))
                    continue;
                hits.AddRange(partHits.Where(hit => !claimed.Contains(hit.Id)));
            }
            hits = hits.Distinct().ToList();
            foreach (var hit in hits)
                claimed.Add(hit.Id);
            assignments.Add((erp, hits));
        }

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

            var erpAbs = cluster.Sum(c => Math.Abs(c.Erp.Taxable));
            var twoAbs = hits.Sum(h => Math.Abs(h.Taxable));
            var difference = Math.Round(erpAbs - twoAbs, 2);
            var status = hits.Count == 0
                ? "Missing in 2B"
                : Math.Abs(difference) <= tolerance ? "Matched" : "Mismatch";
            var period = string.Join(", ", hits.Select(h => h.Period).Where(p => p.Length > 0).Distinct());
            var month = string.Join(", ", hits.Select(h => h.Month).Where(p => p.Length > 0).Distinct());
            var docType = string.Join(", ", hits.Select(h => h.DocumentType).Where(p => p.Length > 0).Distinct());
            var twoBTaxable = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.Taxable);
            var twoBIgst = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.Igst);
            var twoBCgst = hits.Count == 0 ? (decimal?)null : hits.Sum(h => h.CgstSgst);

            foreach (var item in cluster)
            {
                var erp = item.Erp;
                output.Add(new GstBillRecoRow
                {
                    Status = status,
                    CompanyName = company,
                    VoucherType = erp.VoucherType,
                    VoucherNo = erp.VoucherNo,
                    VoucherDate = erp.VoucherDate,
                    RefNo = erp.RefNo,
                    BillDate = erp.BillDate,
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

        return output
            .OrderBy(r => StatusOrder(r.Status))
            .ThenBy(r => r.BillDate)
            .ThenBy(r => r.BillNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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

    private static bool InRange(DateTime billDate, string period, DateTime from, DateTime to)
    {
        if (billDate.Date >= from && billDate.Date <= to)
            return true;
        if (TryPeriodMonth(period, out var monthStart))
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return monthStart <= to && monthEnd >= from;
        }
        return false;
    }

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
        "Missing in ERP" => 1,
        "Missing in 2B" => 2,
        _ => 3,
    };

    private sealed class TwoBParse
    {
        public string BuyerGstin { get; set; } = "";
        public List<TwoBLine> Rows { get; set; } = [];
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

    private sealed class ErpLine
    {
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
        public decimal? TotalC { get; set; }
        public decimal? OtherAll { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public Dictionary<string, decimal?> Ledgers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
