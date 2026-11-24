using Dapper;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Changes the bill number on an ERP journal, or posts a reversal journal for it, from the GST
/// bill reconciliation screen. Writes follow the ERP journal screen: Journal, JournalItem,
/// JournalInventory, and CostCenterEntries. AccountBills is kept in step by the ERP's JournalItem trigger.
/// Purchase vouchers are never changed here.
/// </summary>
public sealed class GstJournalEditService
{
    private const int TimeoutSeconds = 120;
    private readonly DatabaseService _database;

    public GstJournalEditService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<GstJournalActionResult> ChangeBillNoAsync(GstJournalActionRequest request, string user, CancellationToken ct = default)
    {
        var newBill = (request.NewBillNo ?? "").Trim();
        if (newBill.Length == 0)
            throw new ArgumentException("Enter the new bill number.");
        if (newBill.Length > 100)
            throw new ArgumentException("The bill number can be at most 100 characters.");

        using var connection = _database.CreateConnection();
        await EnsureAuditAsync(connection);
        using var tx = connection.BeginTransaction();
        try
        {
            var lines = await LoadLinesAsync(connection, tx, request);
            var yr = lines[0].Yr;
            await RefusePendingApprovalAsync(connection, tx, request, yr);
            var oldBill = lines.Select(line => line.MRNo).FirstOrDefault(bill => bill.Length > 0) ?? "";
            if (string.Equals(oldBill, newBill, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The new bill number is the same as the current one.");

            var transIds = lines.Select(line => line.TransId).ToArray();
            var items = (await connection.QueryAsync<ItemLine>(
                """
                SELECT PartyName, BillNo
                FROM JournalItem WITH (UPDLOCK)
                WHERE journaltransid IN @TransIds
                """, new { TransIds = transIds }, tx, TimeoutSeconds)).ToList();

            if (oldBill.Length > 0)
            {
                foreach (var party in items.Where(item => Same(item.BillNo, oldBill)).Select(item => item.PartyName).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var settled = await connection.ExecuteScalarAsync<int>(
                        """
                        SELECT (SELECT COUNT(*) FROM PaymentAgstRef WITH (NOLOCK)
                                WHERE CompanyName = @Company AND PartyName = @Party AND BillNo = @Bill)
                             + (SELECT COUNT(*) FROM JournalItem WITH (NOLOCK)
                                WHERE CompanyName = @Company AND PartyName = @Party AND BillNo = @Bill
                                  AND SelectionType = 'Agst Ref' AND journaltransid NOT IN @TransIds)
                        """, new { Company = request.CompanyName, Party = party, Bill = oldBill, TransIds = transIds }, tx, TimeoutSeconds);
                    if (settled > 0)
                        throw new InvalidOperationException(
                            $"Bill {oldBill} of {party} already has a payment or adjustment against it. Change it in the ERP so the payment is updated too.");
                }
            }

            await connection.ExecuteAsync(
                """
                UPDATE Journal SET MRNo = @New, InwardNo = CASE WHEN InwardNo = @Old THEN LEFT(@New, 50) ELSE InwardNo END
                WHERE transid IN @TransIds;
                UPDATE JournalItem SET BillNo = @New
                WHERE journaltransid IN @TransIds AND (BillNo = @Old OR (@Old = '' AND ISNULL(BillNo, '') = ''));
                """, new { New = newBill, Old = oldBill, TransIds = transIds }, tx, TimeoutSeconds);

            // trgJornalItem refreshes AccountBills for the new bill; the old bill is refreshed here from what is left on it.
            if (oldBill.Length > 0)
            {
                foreach (var party in items.Where(item => Same(item.BillNo, oldBill)).Select(item => item.PartyName).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    await connection.ExecuteAsync("EXEC sp_UpdateAccountBills 'B', @Company, @Party, @Bill",
                        new { Company = request.CompanyName.Trim(), Party = party, Bill = oldBill }, tx, TimeoutSeconds);
                }
            }

            await AuditAsync(connection, tx, "Change bill no", request, yr, oldBill, newBill, null, user);
            tx.Commit();
            return new GstJournalActionResult
            {
                Message = $"{request.JournalType} {request.JournalNo}: bill number changed from {Blank(oldBill)} to {newBill}.",
                Year = yr,
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<GstJournalActionResult> PostReversalAsync(GstJournalActionRequest request, string user, CancellationToken ct = default)
    {
        var date = (request.ReversalDate ?? DateTime.Today).Date;
        using var connection = _database.CreateConnection();
        await EnsureAuditAsync(connection);
        using var tx = connection.BeginTransaction();
        try
        {
            var lines = await LoadLinesAsync(connection, tx, request);
            await RefusePendingApprovalAsync(connection, tx, request, lines[0].Yr);
            var bill = lines.Select(line => line.MRNo).FirstOrDefault(value => value.Length > 0) ?? "";
            if (bill.Length == 0)
                throw new InvalidOperationException("This journal has no bill number, so there is nothing to reverse against.");
            if (GstBillRecoService.ReversalBasis(bill).Length > 0)
                throw new InvalidOperationException($"{bill} is itself a reversal.");
            if (date < request.VoucherDate.Date)
                throw new ArgumentException("The reversal date cannot be before the journal date.");

            var bookings = await connection.QueryAsync<(string MRNo, string JournalNo, string Yr)>(
                """
                SELECT DISTINCT LTRIM(RTRIM(MRNo)), CONVERT(varchar(40), JournalNo), yr
                FROM Journal WITH (UPDLOCK, HOLDLOCK)
                WHERE CompanyName = @Company AND MRNo LIKE '%' + @Bill + '%'
                """, new { Company = request.CompanyName, Bill = bill }, tx, TimeoutSeconds);
            var normBill = GstBillRecoService.NormBill(bill);
            var originals = bookings.Count(row => GstBillRecoService.NormBill(row.MRNo) == normBill);
            var reversals = bookings.Count(row => GstBillRecoService.ReversalBasis(row.MRNo) == normBill);
            if (reversals >= originals)
                throw new InvalidOperationException(
                    $"Bill {bill} is booked {originals} time(s) and already reversed {reversals} time(s). Nothing is left to reverse.");

            var yr = YearFrom(date);
            var journalNo = await connection.ExecuteScalarAsync<int>(
                """
                SELECT ISNULL(MAX(JournalNo), 0) + 1
                FROM Journal WITH (UPDLOCK, HOLDLOCK)
                WHERE CompanyName = @Company AND yr = @Yr
                """, new { Company = request.CompanyName, Yr = yr }, tx, TimeoutSeconds);

            var reversalBill = "Rev_" + bill;
            if (reversalBill.Length > 100)
                throw new InvalidOperationException("The bill number is too long to add the Rev_ prefix.");
            var reason = (request.Reason ?? "").Trim();
            var narration = $"Reversal of {request.JournalType} {request.JournalNo} dated {request.VoucherDate:dd-MM-yyyy}" + (reason.Length > 0 ? $" - {reason}" : "");
            foreach (var line in lines)
            {
                var transId = await connection.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO Journal (
                        JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit, Narration, UserName, narrationFinal,
                        JournalType, IsTDS, MRNo, InwardNo, DrPartyName, CrPartyName, yr, Currency, ExchangeRate, billDate,
                        paymentTerms, TDSAccessableAmount, RecordLogId, SubLedgerName, companyId, PartyId, DrPartyID, CrPartyID)
                    SELECT @JournalNo, @Date, CompanyName, CASE WHEN UPPER(Type) = 'DR' THEN 'CR' ELSE 'DR' END, PartyName,
                           Credit, Debit, Narration, @User, @Narration, JournalType, IsTDS, @Bill, LEFT(@Bill, 50), CrPartyName, DrPartyName,
                           @Yr, Currency, ExchangeRate, BillDate, PaymentTerms, TDSAccessableAmount, 0, SubLedgerName, companyId,
                           PartyID, CrPartyID, DrPartyID
                    FROM Journal WHERE transid = @Source;
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { JournalNo = journalNo, Date = date, User = user, Narration = narration, Bill = reversalBill, Yr = yr, Source = line.TransId },
                    tx, TimeoutSeconds);

                await connection.ExecuteAsync(
                    """
                    INSERT INTO JournalItem (
                        JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit, SelectionType, BillNo, BillDate,
                        PaidAmount, BillAmount, UserName, yr, journaltransid, companyid, partyid)
                    SELECT @JournalNo, @Date, CompanyName, CASE WHEN UPPER(Type) = 'DR' THEN 'CR' ELSE 'DR' END, PartyName,
                           Credit, Debit, SelectionType, BillNo, BillDate, PaidAmount, BillAMount, @User, @Yr, @TransId, companyId, PartyID
                    FROM JournalItem WHERE journaltransid = @Source;

                    INSERT INTO JournalInventory (
                        JournalNo, sysdate, CompanyName, Type, PartyName, GroupName, Qty, Rate, Amount, yr, journaltransid, companyId, PartyID,
                        Unit, Netwt, Itemcode)
                    SELECT @JournalNo, @Date, CompanyName, CASE WHEN UPPER(Type) = 'DR' THEN 'CR' ELSE 'DR' END, PartyName,
                           GroupName, Qty, Rate, Amount, @Yr, @TransId, companyId, PartyID, Unit, Netwt, Itemcode
                    FROM JournalInventory WHERE journaltransid = @Source;

                    INSERT INTO CostCenterEntries (Srno, CompanyName, yr, LedgerName, Amount, VoucherType, CostCenterId, companyId, LedgerID)
                    SELECT @TransId, CompanyName, @Yr, LedgerName, -Amount, VoucherType, CostCenterId, companyId, LedgerID
                    FROM CostCenterEntries WHERE Srno = @Source AND VoucherType = 'Journal';
                    """,
                    new { JournalNo = journalNo, Date = date, User = user, Yr = yr, TransId = transId, Source = line.TransId },
                    tx, TimeoutSeconds);
            }

            await AuditAsync(connection, tx, "Post reversal", request, lines[0].Yr, bill, reversalBill, journalNo.ToString(), user);
            tx.Commit();
            return new GstJournalActionResult
            {
                Message = $"Reversal posted as Journal {journalNo} ({yr}) dated {date:dd-MM-yyyy} with bill no {reversalBill}.",
                NewJournalNo = journalNo.ToString(),
                Year = yr,
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task<List<JournalLine>> LoadLinesAsync(SqlConnection connection, SqlTransaction tx, GstJournalActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.JournalNo))
            throw new ArgumentException("Company and journal number are required.");
        var lines = (await connection.QueryAsync<JournalLine>(
            """
            SELECT transid AS TransId, yr AS Yr, LTRIM(RTRIM(ISNULL(MRNo, ''))) AS MRNo
            FROM Journal WITH (UPDLOCK)
            WHERE CompanyName = @Company AND CONVERT(varchar(40), JournalNo) = @No
              AND CONVERT(date, sysdate) = @Date AND ISNULL(JournalType, 'Journal') = @Type
            ORDER BY transid
            """,
            new { Company = request.CompanyName.Trim(), No = request.JournalNo.Trim(), Date = request.VoucherDate.Date, Type = request.JournalType.Trim() },
            tx, TimeoutSeconds)).ToList();
        if (lines.Count == 0)
            throw new InvalidOperationException("This journal was not found in the ERP.");
        if (lines.Select(line => line.Yr).Distinct().Count() > 1)
            throw new InvalidOperationException("This journal number exists in more than one financial year on that date.");
        return lines;
    }

    private static async Task RefusePendingApprovalAsync(SqlConnection connection, SqlTransaction tx, GstJournalActionRequest request, string yr)
    {
        var pending = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM Journal_Approval WITH (NOLOCK)
            WHERE CompanyName = @Company AND CONVERT(varchar(40), JournalNo) = @No AND yr = @Yr
            """, new { Company = request.CompanyName.Trim(), No = request.JournalNo.Trim(), Yr = yr }, tx, TimeoutSeconds);
        if (pending > 0)
            throw new InvalidOperationException("This journal is waiting for approval in the ERP. Approve it there first.");
    }

    private static async Task EnsureAuditAsync(SqlConnection connection)
    {
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.AppGstRecoJournalAction', 'U') IS NULL
            CREATE TABLE dbo.AppGstRecoJournalAction (
                Id int IDENTITY(1, 1) PRIMARY KEY,
                Action varchar(40) NOT NULL,
                CompanyName varchar(150) NOT NULL,
                JournalType varchar(100) NOT NULL,
                JournalNo varchar(40) NOT NULL,
                Yr varchar(10) NOT NULL,
                VoucherDate date NOT NULL,
                OldValue varchar(200) NULL,
                NewValue varchar(200) NULL,
                NewJournalNo varchar(40) NULL,
                Reason nvarchar(500) NULL,
                UserName varchar(100) NOT NULL,
                CreatedAt datetime NOT NULL DEFAULT GETDATE()
            );
            """, commandTimeout: TimeoutSeconds);
    }

    private static Task AuditAsync(SqlConnection connection, SqlTransaction tx, string action, GstJournalActionRequest request,
        string yr, string oldValue, string newValue, string? newJournalNo, string user) =>
        connection.ExecuteAsync(
            """
            INSERT INTO dbo.AppGstRecoJournalAction (Action, CompanyName, JournalType, JournalNo, Yr, VoucherDate, OldValue, NewValue, NewJournalNo, Reason, UserName)
            VALUES (@Action, @Company, @Type, @No, @Yr, @Date, @Old, @New, @NewNo, @Reason, @User)
            """,
            new
            {
                Action = action, Company = request.CompanyName.Trim(), Type = request.JournalType.Trim(), No = request.JournalNo.Trim(), Yr = yr,
                Date = request.VoucherDate.Date, Old = oldValue, New = newValue, NewNo = newJournalNo, Reason = request.Reason?.Trim(), User = user,
            }, tx, TimeoutSeconds);

    private static bool Same(string? left, string right) =>
        string.Equals((left ?? "").Trim(), right, StringComparison.OrdinalIgnoreCase);

    private static string Blank(string value) => value.Length == 0 ? "(blank)" : value;

    private static string YearFrom(DateTime date)
    {
        var start = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{start % 100:00}-{(start + 1) % 100:00}";
    }

    private sealed class JournalLine
    {
        public int TransId { get; set; }
        public string Yr { get; set; } = "";
        public string MRNo { get; set; } = "";
    }

    private sealed class ItemLine
    {
        public string PartyName { get; set; } = "";
        public string? BillNo { get; set; }
    }
}
