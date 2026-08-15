using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeMrnDomainQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("mrn") || m.Contains("material receipt") || m.Contains("goods receipt")
               || m.Contains("store inward") || Regex.IsMatch(m, @"\brm\s*\d+");
    }

    private static bool TryBuildMrnPaymentEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeMrnPaymentQuestion(message)) return false;
        if (TryExtractMrnNumber(message) is not { } mrn) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PaymentNo, MRNno, BillNo, PaymentAmount, BillAmount, UTRno, status, isPaid, IsCancel
            FROM BillPaymentEntry WITH (NOLOCK)
            WHERE MRNno = '{EscapeSqlLiteral(mrn)}'
            ORDER BY PaymentAmount DESC
            """;
        warning = $"Governed MRN payment lookup for {mrn} (BillPaymentEntry.MRNno; not null vw_MRNToBillPayment lines).";
        return true;
    }

    private static bool TryBuildMrnByBillNoEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeReceiptByBillQuestion(message)) return false;
        if (TryExtractBillNo(message) is not { } billNo) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                MRNo, CompanyName, PartyName, BillNo, BillDate, ItemName, ItemCode,
                RecdQty, AcceptedQty, PendingQty, PONo, GateInwardNo, Amount
            FROM Vw_StoreInwards WITH (NOLOCK)
            WHERE BillNo = '{EscapeSqlLiteral(billNo)}'
            ORDER BY BillDate DESC
            """;
        warning = $"Governed receipts by bill {billNo} (Vw_StoreInwards.BillNo; not BillPaymentEntry.BillNo).";
        return true;
    }

    private static bool TryBuildMrnByMrNoEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeMrnDomainQuestion(message)) return false;
        if (LooksLikeMrnPaymentQuestion(message)) return false;
        if (LooksLikeReceiptByBillQuestion(message)) return false;
        if (LooksLikeMrnPendingQtyQuestion(message)) return false;
        if (TryExtractMrnNumber(message) is not { } mrn) return false;

        var m = message.ToLowerInvariant();
        var wantsHeader = m.Contains("vendor") || m.Contains("supplier") || m.Contains("who is")
                          || m.Contains("bill number") || m.Contains("bill amt") || m.Contains("bill amount")
                          || (m.Contains("bill") && !m.Contains("item") && !m.Contains("material"));
        var wantsPo = m.Contains("purchase order") || m.Contains(" po ") || m.StartsWith("po ")
                      || m.Contains("against po") || m.Contains("pono");

        if (wantsPo && !m.Contains("item") && !m.Contains("material") && !m.Contains("qty"))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    MRNo, CompanyName, PartyName, PONo, BillNo, BillDate, Amount, GateInwardNo
                FROM Vw_StoreInwards WITH (NOLOCK)
                WHERE MRNo = '{EscapeSqlLiteral(mrn)}'
                ORDER BY BillDate DESC
                """;
            warning = $"Governed PO link for MRN {mrn} (Vw_StoreInwards.PONo).";
            return true;
        }

        if (wantsHeader && !m.Contains("item") && !m.Contains("material") && !m.Contains("qty"))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    MRNo, CompanyName, PartyName, BillNo, BillDate, Amount, GateInwardNo, SupplierName
                FROM Vw_StoreInwards WITH (NOLOCK)
                WHERE MRNo = '{EscapeSqlLiteral(mrn)}'
                ORDER BY BillDate DESC
                """;
            warning = $"Governed MRN header for {mrn} (Vw_StoreInwards; vendor=PartyName).";
            return true;
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                MRNo, CompanyName, PartyName, ItemName, ItemCode, RecdQty, AcceptedQty, PendingQty,
                BillNo, BillDate, PONo, GateInwardNo, Amount
            FROM Vw_StoreInwards WITH (NOLOCK)
            WHERE MRNo = '{EscapeSqlLiteral(mrn)}'
            ORDER BY ItemCode
            """;
        warning = $"Governed MRN line items for {mrn} (Vw_StoreInwards).";
        return true;
    }

    private static bool LooksLikeMrnPendingQtyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!LooksLikeMrnReceivingCompanyIntent(message)) return false;
        return m.Contains("pending") && (m.Contains("qty") || m.Contains("quantity") || m.Contains("receive")
                                         || m.Contains("receipt"));
    }

    private static bool TryBuildMrnPendingQtyEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeMrnPendingQtyQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                MRNo, CompanyName, PartyName, ItemName, ItemCode, RecdQty, PendingQty, BillNo, BillDate, PONo
            FROM Vw_StoreInwards WITH (NOLOCK)
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND PendingQty > 0
            ORDER BY PendingQty DESC, BillDate DESC
            """;
        warning = $"Governed MRN lines with pending qty at {company} (Vw_StoreInwards.CompanyName + PendingQty > 0).";
        return true;
    }

    private static bool LooksLikeMrnPartyReceiptQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("receipt") && !m.Contains("mrn") && !m.Contains("goods")) return false;
        return m.Contains("-purchase") || m.Contains("party ")
               || (m.Contains("vendor") && m.Contains("recent"))
               || (m.Contains("supplier") && m.Contains("recent"));
    }

    private static bool TryBuildMrnPartyReceiptsEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeMrnPartyReceiptQuestion(message)) return false;
        if (TryExtractMrnNumber(message) is not null) return false;

        var party = TryExtractLedgerPartyName(message);
        if (string.IsNullOrWhiteSpace(party))
        {
            var purchaseMatch = Regex.Match(message, @"([\w\s\.]+-Purchase)", RegexOptions.IgnoreCase);
            if (purchaseMatch.Success) party = purchaseMatch.Groups[1].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(party)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                MRNo, CompanyName, PartyName, BillNo, BillDate, ItemName, RecdQty, Amount, PONo
            FROM Vw_StoreInwards WITH (NOLOCK)
            WHERE PartyName LIKE '%{EscapeSqlLiteral(party)}%'
            ORDER BY BillDate DESC
            """;
        warning = $"Governed recent receipts for party {party} (Vw_StoreInwards.PartyName; not CompanyName).";
        return true;
    }
}
