using System.Data;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Reads one ERP voucher behind a GST reconciliation row (journal, purchase voucher, or debit note)
/// with its header, lines, and charges as they are stored in the ERP tables.
/// </summary>
public sealed class GstErpVoucherService
{
    private readonly DatabaseService _database;

    private static readonly HashSet<string> TotalColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Debit", "Credit", "Amount", "CGST", "SGST", "IGST", "Amount difference", "Bill amount", "Paid", "TDS",
    };

    public GstErpVoucherService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<GstErpVoucherDetail?> GetAsync(GstErpVoucherRef voucher, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(voucher.CompanyName) || voucher.VoucherDate == null)
            return null;
        await using var connection = _database.CreateConnection();
        return voucher.Source switch
        {
            "Purchase" => await PurchaseAsync(connection, voucher, ct),
            "Journal" => await JournalAsync(connection, voucher, ct),
            "DebitNote" => await DebitNoteAsync(connection, voucher, ct),
            _ => null,
        };
    }

    private static async Task<GstErpVoucherDetail?> JournalAsync(SqlConnection connection, GstErpVoucherRef voucher, CancellationToken ct)
    {
        const string where = """
            j.CompanyName = @Company AND CONVERT(varchar(40), j.JournalNo) = @No
              AND CONVERT(date, j.sysdate) = @Date AND ISNULL(j.JournalType, 'Journal') = @Type
            """;
        var header = await HeaderAsync(connection, $"""
            SELECT TOP 1
                ISNULL(j.JournalType, 'Journal') AS [Voucher type], j.JournalNo AS [Voucher no], j.sysdate AS [Voucher date],
                j.MRNo AS [Bill no], j.BillDate AS [Bill date], j.DrPartyName AS [Dr party], j.CrPartyName AS [Cr party],
                j.narrationFinal AS [Narration], j.Currency, j.ExchangeRate AS [Exchange rate], j.PaymentTerms AS [Payment terms],
                j.TDSAccessableAmount AS [TDS assessable], j.UserName AS [Entered by], j.ApprovalStatus AS [Approval status]
            FROM Journal j WITH (NOLOCK)
            WHERE {where}
            ORDER BY j.transid
            """, voucher, ct);
        if (header == null)
            return null;

        var detail = new GstErpVoucherDetail
        {
            Title = $"{voucher.VoucherType} {voucher.VoucherNo}".Trim(),
            Header = header,
            Editable = true,
            BillNo = Convert.ToString(header.FirstOrDefault(field => field.Label == "Bill no")?.Value) ?? "",
        };
        detail.Sections.Add(await SectionAsync(connection, "Ledger lines", $"""
            SELECT j.Type AS [Dr/Cr], j.PartyName AS [Ledger], NULLIF(j.SubLedgerName, '') AS [Sub ledger],
                   j.Debit, j.Credit, NULLIF(j.Narration, '') AS [Line narration]
            FROM Journal j WITH (NOLOCK)
            WHERE {where}
            ORDER BY j.transid
            """, voucher, ct));
        detail.Sections.Add(await SectionAsync(connection, "Bill references", """
            SELECT i.PartyName AS [Ledger], i.SelectionType AS [Reference], i.BillNo AS [Bill no], i.BillDate AS [Bill date],
                   i.BillAMount AS [Bill amount], i.PaidAmount AS [Paid], NULLIF(i.TDSLedgerName, '') AS [TDS ledger], i.TDSAmount AS [TDS]
            FROM JournalItem i WITH (NOLOCK)
            WHERE i.CompanyName = @Company AND CONVERT(varchar(40), i.JournalNo) = @No AND CONVERT(date, i.sysdate) = @Date
            ORDER BY i.transid
            """, voucher, ct));
        return detail;
    }

    private static async Task<GstErpVoucherDetail?> PurchaseAsync(SqlConnection connection, GstErpVoucherRef voucher, CancellationToken ct)
    {
        const string headerWhere = """
            p.CompanyName = @Company AND CONVERT(varchar(40), p.StoreInwardNo) = @Ref AND CONVERT(date, p.SysDate) = @Date
            """;
        var header = await HeaderAsync(connection, $"""
            SELECT TOP 1
                p.VoucherType AS [Voucher type], p.GeneralPurchaseNo AS [Voucher no], p.SysDate AS [Voucher date],
                p.StoreInwardNo AS [Store inward no], p.SupplierName AS [Supplier], p.SupplierInvNo AS [Bill no],
                p.SupplierInvDate AS [Bill date], p.PurchaseLedger AS [Purchase ledger], p.AccessableValue AS [Assessable value],
                p.BillAMount AS [Bill amount], p.currency AS [Currency], p.ExchangeRate AS [Exchange rate],
                p.PaymentTerms AS [Payment terms], p.BillofEntryNumber AS [Bill of entry], p.BLDATE AS [B/L date],
                p.Narration, p.LoginName AS [Entered by], p.ApprovalStatus AS [Approval status]
            FROM PurchaseVoucher p WITH (NOLOCK)
            WHERE {headerWhere}
            """, voucher, ct);
        if (header == null)
            return null;

        const string yr = $"""
            (SELECT TOP 1 p.yr FROM PurchaseVoucher p WITH (NOLOCK) WHERE {headerWhere})
            """;
        var detail = new GstErpVoucherDetail { Title = $"{voucher.VoucherType} {voucher.VoucherNo}".Trim(), Header = header };
        detail.Sections.Add(await SectionAsync(connection, "Items", $"""
            SELECT i.ItemCode AS [Item code], i.ItemName AS [Item], i.hsncode AS [HSN], i.ActualQty AS [Qty], i.BilledQty AS [Billed qty],
                   i.Per AS [Unit], i.Rate, i.Amount, i.CGSTPer AS [CGST %], i.CGSTAmount AS [CGST], i.SGSTPer AS [SGST %],
                   i.SGSTAmount AS [SGST], i.IGSTPer AS [IGST %], i.IGSTAmount AS [IGST]
            FROM PurchaseVoucherItem i WITH (NOLOCK)
            WHERE i.CompanyName = @Company AND CONVERT(varchar(40), i.StoreInwardNo) = @Ref AND i.yr = {yr}
            ORDER BY i.SrNo
            """, voucher, ct));
        detail.Sections.Add(await SectionAsync(connection, "Taxes and charges", $"""
            SELECT t.TaxledgerName AS [Ledger], t.Rate, t.Per AS [On], t.Amount, t.TypeTransaction AS [Applied to],
                   NULLIF(t.Description, '') AS [Description]
            FROM PurchaseVoucherTax t WITH (NOLOCK)
            WHERE t.CompanyName = @Company AND CONVERT(varchar(40), t.StoreInwardNo) = @Ref AND t.yr = {yr}
              AND CONVERT(date, t.sysDate) = @Date
            ORDER BY t.SrNo
            """, voucher, ct));
        return detail;
    }

    private static async Task<GstErpVoucherDetail?> DebitNoteAsync(SqlConnection connection, GstErpVoucherRef voucher, CancellationToken ct)
    {
        var header = await HeaderAsync(connection, """
            SELECT TOP 1
                'Debit Note' AS [Voucher type], d.DebitNoteNumber AS [Voucher no], d.sysdate AS [Voucher date],
                d.DebitType AS [Debit type], d.PartyName AS [Party], d.BillNo AS [Bill no], d.billDate AS [Bill date],
                d.MRNo AS [MR no], d.PurchaseLedger AS [Purchase ledger], d.Basic, d.Frieght AS [Freight],
                d.PkgFwd AS [Packing & forwarding], d.OtherChg AS [Other charges], d.TotalDebitAmount AS [Total debit],
                d.Particaulars AS [Particulars], d.Remarks, d.LoginName AS [Entered by], d.ApprovalStatus AS [Approval status]
            FROM DebitNote d WITH (NOLOCK)
            WHERE d.CompanyName = @Company AND CONVERT(varchar(40), d.DebitNoteNumber) = @No AND CONVERT(date, d.sysdate) = @Date
            """, voucher, ct);
        if (header == null)
            return null;

        var detail = new GstErpVoucherDetail { Title = $"Debit Note {voucher.VoucherNo}", Header = header };
        detail.Sections.Add(await SectionAsync(connection, "Items", """
            SELECT i.ItemCode AS [Item code], i.ItemName AS [Item], i.Unit, i.RecdQty AS [Received qty], i.AcceptedQty AS [Accepted qty],
                   i.Rate, i.Amount, i.RateDifference AS [Rate difference], i.QtyDifference AS [Qty difference],
                   i.AmountDifference AS [Amount difference], i.CGSTAmount AS [CGST], i.SGSTAmount AS [SGST], i.IGSTAmount AS [IGST],
                   NULLIF(i.remarks, '') AS [Remarks]
            FROM DebitNoteItem i WITH (NOLOCK)
            WHERE i.companyname = @Company AND CONVERT(varchar(40), i.DebitNoteNumber) = @No
            ORDER BY i.SrNo
            """, voucher, ct));
        return detail;
    }

    private static SqlCommand Command(SqlConnection connection, string sql, GstErpVoucherRef voucher)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 60;
        cmd.Parameters.Add("@Company", SqlDbType.VarChar, 150).Value = voucher.CompanyName.Trim();
        cmd.Parameters.Add("@No", SqlDbType.VarChar, 40).Value = voucher.VoucherNo.Trim();
        cmd.Parameters.Add("@Ref", SqlDbType.VarChar, 40).Value = voucher.RefNo.Trim();
        cmd.Parameters.Add("@Type", SqlDbType.VarChar, 100).Value = voucher.VoucherType.Trim();
        cmd.Parameters.Add("@Date", SqlDbType.Date).Value = voucher.VoucherDate!.Value.Date;
        return cmd;
    }

    private static async Task<List<GstErpVoucherField>?> HeaderAsync(SqlConnection connection, string sql, GstErpVoucherRef voucher, CancellationToken ct)
    {
        await using var cmd = Command(connection, sql, voucher);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;
        var fields = new List<GstErpVoucherField>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = Value(reader, i);
            if (value is null or "")
                continue;
            fields.Add(new GstErpVoucherField { Label = reader.GetName(i), Value = value });
        }
        return fields;
    }

    private static async Task<GstErpVoucherSection> SectionAsync(SqlConnection connection, string title, string sql, GstErpVoucherRef voucher, CancellationToken ct)
    {
        await using var cmd = Command(connection, sql, voucher);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var section = new GstErpVoucherSection { Title = title };
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var numeric = IsNumeric(reader.GetFieldType(i));
            var name = reader.GetName(i);
            section.Columns.Add(new GstErpVoucherColumn { Name = name, Numeric = numeric, Total = numeric && TotalColumns.Contains(name) });
        }
        while (await reader.ReadAsync(ct))
            section.Rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => Value(reader, i)).ToList());

        var empty = Enumerable.Range(0, section.Columns.Count)
            .Where(i => section.Rows.All(row => row[i] is null or "" || (section.Columns[i].Numeric && Convert.ToDecimal(row[i]) == 0)))
            .ToHashSet();
        if (section.Rows.Count > 0 && empty.Count > 0)
        {
            section.Columns = section.Columns.Where((_, i) => !empty.Contains(i)).ToList();
            section.Rows = section.Rows.Select(row => row.Where((_, i) => !empty.Contains(i)).ToList()).ToList();
        }
        return section;
    }

    private static object? Value(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTime { Year: <= 1900 } => null,
            DateTime date => date.ToString("yyyy-MM-dd"),
            string text => text.Trim(),
            double or float or decimal => Math.Round(Convert.ToDecimal(value), 2),
            _ => value,
        };
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(decimal) || type == typeof(double) || type == typeof(float)
        || type == typeof(int) || type == typeof(long) || type == typeof(short);
}
