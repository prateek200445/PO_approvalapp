using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeJobMrnPendingWoQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("job") && m.Contains("mrn") && (m.Contains("pending") || m.Contains("work order")))
               || m.Contains("job mrn pending");
    }

    private static bool TryBuildJobMrnPendingWoSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeJobMrnPendingWoQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var vendor = TryExtractVendorFirmName(message);
        if (!string.IsNullOrWhiteSpace(vendor))
            filters.Add($"SupplierName LIKE '%{EscapeSqlLiteral(vendor)}%'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, MRNo, SysDate, SupplierName, BillNo, ItemCode, ItemName, GroupName, challanos, Remarks
            FROM vw_JobMRN_PendingWO WITH (NOLOCK)
            {where}
            ORDER BY SysDate DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed job-work MRN pending WO (vw_JobMRN_PendingWO)."
            : $"Governed job-work MRN pending WO for {company} (vw_JobMRN_PendingWO).";
        return true;
    }

    private static bool LooksLikePoAmendmentQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (ContainsPoIntent(message) && m.Contains("pending")) return false;
        return m.Contains("po amendment") || m.Contains("purchase order amendment")
               || m.Contains("amended po") || (m.Contains("amendment") && m.Contains("purchase"));
    }

    private static bool TryBuildPoAmendmentSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePoAmendmentQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "PendingQty > 0" };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var vendor = TryExtractVendorFirmName(message);
        if (!string.IsNullOrWhiteSpace(vendor))
            filters.Add($"FirmName LIKE '%{EscapeSqlLiteral(vendor)}%'");

        var poMatch = Regex.Match(message, @"\b(?:po|order)\s*(?:no\.?|number|code)?\s*[:#]?\s*([A-Za-z0-9/\-]+)", RegexOptions.IgnoreCase);
        if (poMatch.Success)
            filters.Add($"PurchaseCode LIKE '%{EscapeSqlLiteral(poMatch.Groups[1].Value.Trim())}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, CompanyName, FirmName, ItemCode, ItemDesc, Qty, PendingQty, sysdate, Rate, Total
            FROM Vw_AmendmentPurchaseOrder WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY sysdate DESC
            """;
        warning = $"Governed PO amendments with pending qty (Vw_AmendmentPurchaseOrder).";
        return true;
    }

    private static bool LooksLikeBillPaymentDraftQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("payment draft") || m.Contains("bill payment draft")
                || m.Contains("payment request draft") || m.Contains("draft payment request"))
               && !m.Contains("approved");
    }

    private static bool TryBuildBillPaymentDraftSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeBillPaymentDraftQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var companyLit = EscapeSqlLiteral(company);
        var filters = new List<string> { $"CompanyName = '{companyLit}'" };

        var vendor = TryExtractVendorFirmName(message);
        if (!string.IsNullOrWhiteSpace(vendor))
            filters.Add($"Partyname LIKE '%{EscapeSqlLiteral(vendor)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, srno, BillNo, BillDate, Partyname, PoNO, Itemcode, Itemname, Deptt, OrderQty, Challanqty, PendingQty
            FROM vw_BillPaymentReqDraft WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY [Date] DESC
            """;
        warning = $"Governed bill payment request drafts for {company} (vw_BillPaymentReqDraft).";
        return true;
    }

    private static bool LooksLikePurchaseReqQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("quotation") || m.Contains("vendor rate")) return false;
        return (m.Contains("purchase req") || m.Contains("purchase requisition")
                || Regex.IsMatch(m, @"\bpr\b") || m.Contains(" pr "))
               && !m.Contains("prq") && !m.Contains("payment req");
    }

    private static bool TryBuildPurchaseReqSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePurchaseReqQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var companyLit = EscapeSqlLiteral(company);
        var m = message.ToLowerInvariant();
        var pendingOnly = m.Contains("pending") || m.Contains("not ordered") || m.Contains("not yet ordered");

        if (pendingOnly)
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Code, CompanyName, Deptt, ItemCode, ItemDesc, ReqQty, POQty, RecdQty, PurchaseCode, loginname, sysdate
                FROM Vw_PurchaseReq WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}' AND ReqQty > ISNULL(POQty, 0)
                ORDER BY sysdate DESC
                """;
            warning = $"Governed pending PRs not fully ordered for {company} (Vw_PurchaseReq).";
        }
        else
        {
            var dept = TryExtractDepartmentFragment(message);
            var filters = new List<string> { $"CompanyName = '{companyLit}'" };
            if (!string.IsNullOrWhiteSpace(dept))
                filters.Add($"Deptt LIKE '%{EscapeSqlLiteral(dept)}%'");

            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Code, CompanyName, Deptt, ItemCode, ItemDesc, Qty, Unit, Purpose, loginname, sysdate
                FROM PurchaseReq WITH (NOLOCK)
                WHERE {string.Join(" AND ", filters)}
                ORDER BY sysdate DESC
                """;
            warning = $"Governed purchase requisitions for {company} (PurchaseReq).";
        }
        return true;
    }

    private static bool TryBuildSmallBagProductionSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeSmallBagProductionQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, PartyName, BagSize, OrderQty, BagWt, Sysdate, MarketingOrdNo, BagPcs, bailing, Cutting, Stitching, Despatch, Shift, Wastage
            FROM vw_DailySmallBagProductionReport WITH (NOLOCK)
            {where}
            ORDER BY Sysdate DESC
            """;
        warning = "Governed daily small-bag production report (vw_DailySmallBagProductionReport).";
        return true;
    }

    private static bool LooksLikeLedgerGroupingQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("ledger group") || m.Contains("ledger hierarchy")
                || m.Contains("expense group") || m.Contains("common ledger grouping"))
               && !m.Contains("count") && !m.Contains("how many");
    }

    private static bool TryBuildLedgerGroupingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeLedgerGroupingQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var party = ResolveLedgerPartyForChat(message);
        if (!string.IsNullOrWhiteSpace(party))
            filters.Add($"ledgername LIKE '%{EscapeSqlLiteral(party)}%'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, ledgername, expensehead, expensegrouphead, b, c, d, e, f, g, h, i, j, k, ISPNL
            FROM vw_Commonledgergrouping WITH (NOLOCK)
            {where}
            ORDER BY ledgername
            """;
        warning = "Governed ledger hierarchy/grouping (vw_Commonledgergrouping).";
        return true;
    }

    private static bool LooksLikeAccountVoucherApprovalQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("voucher approval") || m.Contains("account voucher")
                || m.Contains("accounting voucher pending"))
               && (m.Contains("pending") || m.Contains("approval"));
    }

    private static bool TryBuildAccountVoucherApprovalSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeAccountVoucherApprovalQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "Status = 'Pending'", "Isdel = 0" };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var party = ResolveLedgerPartyForChat(message);
        if (!string.IsNullOrWhiteSpace(party))
            filters.Add($"LedgerName LIKE '%{EscapeSqlLiteral(party)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, VoucherDate, VoucherType, VoucherNo, LedgerName, Debit, Credit, Status, ApprovalName, Sysdate
            FROM AccountVoucherApproval WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY VoucherDate DESC
            """;
        warning = "Governed pending account voucher approvals (AccountVoucherApproval).";
        return true;
    }

    private static bool LooksLikeVoucherPartyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("voucher party") || m.Contains("party ledger on voucher")
                || m.Contains("second ledger on voucher"))
               && !LooksLikeAccountVoucherApprovalQuestion(message);
    }

    private static bool TryBuildVoucherPartySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeVoucherPartyQuestion(message)) return false;

        var party = ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(party)) return false;

        var partyLit = EscapeSqlLiteral(party);
        var (_, _, fyLabel) = ParseIndianFinancialYear(message);
        var shortFy = fyLabel.Length >= 7
            ? $"{fyLabel.AsSpan(2, 2)}-{fyLabel.AsSpan(fyLabel.Length - 2)}"
            : fyLabel;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                avp.FinancialYear, avp.VoucherTypeGroup, avp.VoucherNo,
                avp.PartyLedgerName, avp.SecondLedgerName, fi.Name AS CompanyName
            FROM ac_voucher_party avp WITH (NOLOCK)
            INNER JOIN FactoryInfo fi WITH (NOLOCK) ON avp.CompanyId = fi.SrNo
            WHERE (avp.PartyLedgerName LIKE '%{partyLit}%' OR avp.SecondLedgerName LIKE '%{partyLit}%')
              AND avp.FinancialYear = '{EscapeSqlLiteral(shortFy)}'
            ORDER BY avp.VoucherNo DESC
            """;
        warning = $"Governed voucher party mapping for {party} FY {shortFy} (ac_voucher_party + FactoryInfo).";
        return true;
    }

    private static bool LooksLikeEditPurchaseOrderQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (LooksLikePoAmendmentQuestion(message)) return false;
        return (m.Contains("edit purchase order") || m.Contains("purchase order lines")
                || m.Contains("po lines") || (ContainsPoIntent(message) && m.Contains("item")))
               && !m.Contains("pending approval") && !m.Contains("amendment");
    }

    private static bool TryBuildEditPurchaseOrderSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeEditPurchaseOrderQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var poMatch = Regex.Match(message, @"\b(?:po|purchase code|order)\s*(?:no\.?|number|code)?\s*[:#]?\s*([A-Za-z0-9/\-]+)", RegexOptions.IgnoreCase);
        if (poMatch.Success)
            filters.Add($"PurchaseCode LIKE '%{EscapeSqlLiteral(poMatch.Groups[1].Value.Trim())}%'");
        else if (filters.Count == 0)
            return false;

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, CompanyName, ItemCode, ItemDesc, Qty, Rate, Total, Unit, FirmName, Delivery
            FROM Vw_EditPurchaseOrder WITH (NOLOCK)
            {where}
            ORDER BY PurchaseCode DESC
            """;
        warning = "Governed purchase order line details (Vw_EditPurchaseOrder).";
        return true;
    }
}
