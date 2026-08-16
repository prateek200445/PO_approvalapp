using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikePurchaseVoucherQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("purchase order") || m.Contains(" pending po") || ContainsPoIntent(message))
            return false;
        return (m.Contains("purchase invoice") || m.Contains("purchase voucher")
                || m.Contains("purchase bill") || (m.Contains("purchase") && m.Contains("invoice")))
               && !m.Contains("ebidta") && !m.Contains("total purchase");
    }

    private static bool TryBuildPurchaseVoucherSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePurchaseVoucherQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (fyStart, fyEndEx, fyLabel) = ParseIndianFinancialYear(message);
        var companyLit = EscapeSqlLiteral(company);
        var filters = new List<string>
        {
            $"CompanyName = '{companyLit}'",
            $"SysDate >= '{fyStart:yyyy-MM-dd}'",
            $"SysDate < '{fyEndEx:yyyy-MM-dd}'",
        };

        var vendor = TryExtractVendorFirmName(message);
        if (!string.IsNullOrWhiteSpace(vendor))
            filters.Add($"SupplierName LIKE '%{EscapeSqlLiteral(vendor)}%'");

        var invMatch = Regex.Match(message, @"\b(?:invoice|voucher|bill|store)\s*(?:no\.?|number|inward)?\s*[:#]?\s*([A-Za-z0-9/\-]+)", RegexOptions.IgnoreCase);
        if (invMatch.Success)
            filters.Add($"(StoreInwardNo LIKE '%{EscapeSqlLiteral(invMatch.Groups[1].Value.Trim())}%')");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, StoreInwardNo, SysDate, SupplierName, BillAMount, currency, VoucherType, BillNo, BillDate
            FROM PurchaseVoucher WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY SysDate DESC, StoreInwardNo DESC
            """;
        warning = $"Governed purchase vouchers/invoices for {company} FY {fyLabel} (PurchaseVoucher).";
        return true;
    }

    private static bool LooksLikePaymentVoucherQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("bill payment") || m.Contains("payment approval") || m.Contains("pending payment"))
            return false;
        return m.Contains("payment voucher") || m.Contains("payment entry")
               || (m.Contains("payments made") && !m.Contains("bill"));
    }

    private static bool TryBuildPaymentVoucherSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePaymentVoucherQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (fyStart, fyEndEx, fyLabel) = ParseIndianFinancialYear(message);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                PaymentNo, PaymentDate, CompanyName, Currency
            FROM Payment WITH (NOLOCK)
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND PaymentDate >= '{fyStart:yyyy-MM-dd}'
              AND PaymentDate < '{fyEndEx:yyyy-MM-dd}'
            ORDER BY PaymentDate DESC, PaymentNo DESC
            """;
        warning = $"Governed payment vouchers (outgoing) for {company} FY {fyLabel} (Payment table).";
        return true;
    }

    private static bool LooksLikePaymentReceiptQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("payment receipt") || m.Contains("receipt voucher")
               || m.Contains("money received") || (m.Contains("receipt") && m.Contains("payment"));
    }

    private static bool TryBuildPaymentReceiptSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePaymentReceiptQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (fyStart, fyEndEx, fyLabel) = ParseIndianFinancialYear(message);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                PaymentNo, PaymentDate, companyname, currency
            FROM PaymentReceipt WITH (NOLOCK)
            WHERE companyname = '{EscapeSqlLiteral(company)}'
              AND PaymentDate >= '{fyStart:yyyy-MM-dd}'
              AND PaymentDate < '{fyEndEx:yyyy-MM-dd}'
            ORDER BY PaymentDate DESC, PaymentNo DESC
            """;
        warning = $"Governed payment receipts (incoming) for {company} FY {fyLabel} (PaymentReceipt table).";
        return true;
    }

    private static bool LooksLikeAdvanceBillOutstandingQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("advance bill") || m.Contains("advance outstanding")
               || (m.Contains("advance") && m.Contains("outstanding"));
    }

    private static bool TryBuildAdvanceBillOutstandingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeAdvanceBillOutstandingQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var filters = new List<string> { $"CompanyName = '{EscapeSqlLiteral(company)}'" };
        var party = ResolveLedgerPartyForChat(message);
        if (!string.IsNullOrWhiteSpace(party))
            filters.Add($"LedgerName LIKE '%{EscapeSqlLiteral(party)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, LedgerName, BillNo, BillDate, Amount, PendingAmount, ledgerbalance, RepresentiveName
            FROM vw_advancebilloutstanding WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY PendingAmount DESC
            """;
        warning = $"Governed advance bill outstanding for {company} (vw_advancebilloutstanding).";
        return true;
    }

    private static bool LooksLikeDueOverDueQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("due overdue") || m.Contains("overdue summary")
               || (m.Contains("highly overdue") && m.Contains("hod"));
    }

    private static bool TryBuildDueOverDueSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeDueOverDueQuestion(message)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                Name,
                [Pending But Not Overdue],
                OverDue_Hod, HighlyOverDue_H0D,
                OverDue_Acct, highlyoverdue_acct,
                OverDue_Fin, highlyoverdue_fin,
                tot, tot1
            FROM vw_DueOverDue WITH (NOLOCK)
            ORDER BY tot DESC
            """;
        warning = "Governed due/overdue summary by representative (vw_DueOverDue).";
        return true;
    }

    private static bool LooksLikeDueDateCashFlowQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("cash flow") || m.Contains("cashflow")
               || m.Contains("lc due") || m.Contains("due date cash");
    }

    private static bool TryBuildDueDateCashFlowSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeDueDateCashFlowQuestion(message)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PaymentDetails, Amount, LcDueDate
            FROM Vw_DueDateCashFlow WITH (NOLOCK)
            ORDER BY LcDueDate
            """;
        warning = "Governed LC/payment due-date cash flow (Vw_DueDateCashFlow).";
        return true;
    }

    private static string? TryExtractVendorFirmName(string message)
    {
        var m = Regex.Match(message, @"\b(?:vendor|supplier)\s+(.+?)(?:\s+at|\s+for|\?|$)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var name = m.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
        return name.Length >= 3 ? name : null;
    }

    private static bool LooksLikeImportPoMrnPendingQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("import") && (m.Contains("po") || m.Contains("purchase order"))
                && (m.Contains("mrn") || m.Contains("pending") || m.Contains("receipt")))
               || m.Contains("import po mrn pending")
               || m.Contains("pending mrn against import");
    }

    private static bool TryBuildImportPoMrnPendingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeImportPoMrnPendingQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);

        var filters = new List<string> { "pendingqty > 0" };
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(BuyerName LIKE '%{lit}%' OR BeneficiaryName LIKE '%{lit}%')");
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                orderno, BeneficiaryName, BuyerName, OrderDate, poqty, MRNqty, pendingqty, AdvancelicNo
            FROM vw_ImportPurchasewithPOandMRNqty WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY pendingqty DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed import PO vs MRN pending qty (vw_ImportPurchasewithPOandMRNqty, pendingqty > 0)."
            : $"Governed import PO/MRN pending for {company} (vw_ImportPurchasewithPOandMRNqty).";
        return true;
    }
}
