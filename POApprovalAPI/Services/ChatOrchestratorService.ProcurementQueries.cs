using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static string? TryExtractPurchaseOrPoCode(string message)
    {
        var m = Regex.Match(
            message,
            @"\b([A-Z]{2,5}/[A-Z]{2,5}/\d{2}-\d{2}/[\dA-Za-z]+)\b",
            RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Trim();

        m = Regex.Match(
            message,
            @"\b(?:purchase\s+code|po|purchase\s+order)\s*(?:no\.?|number|code)?\s*[:#]?\s*([A-Za-z0-9/\-]+)",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var code = m.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
            return code.Length >= 5 ? code : null;
        }

        return null;
    }

    private static string? TryExtractIndentNo(string message)
    {
        var m = Regex.Match(
            message,
            @"\b([A-Z]{2,5}/\d{2}-\d{2}/[A-Za-z0-9]+)\b",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? TryExtractJobWorkOrderCode(string message)
    {
        var m = Regex.Match(
            message,
            @"\b([A-Za-z0-9]+/(?:JRO|JWO)/[\d\-/]+)\b",
            RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Trim();

        m = Regex.Match(
            message,
            @"\b(?:job\s+work\s+order|jwo|jro)\s*(?:no\.?|code)?\s*[:#]?\s*([A-Za-z0-9/\-]+)",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim().TrimEnd('.', '?', '!') : null;
    }

    private static string? TryExtractInvoiceNo(string message)
    {
        var m = Regex.Match(
            message,
            @"\b(?:invoice|inv)\s*(?:no\.?|number|#)?\s*[:#]?\s*(\d+)\b",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? TryExtractDebitCreditDocNo(string message)
    {
        var m = Regex.Match(
            message,
            @"\b([A-Za-z0-9]+/(?:DB|CR)/[\d\-/]+)\b",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static bool TryBuildQuotationByPoSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeVendorQuotationQuestion(message)) return false;

        var poCode = TryExtractPurchaseOrPoCode(message);
        if (string.IsNullOrWhiteSpace(poCode)) return false;

        var lit = EscapeSqlLiteral(poCode);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, FirmName, ItemCode, ItemDesc, Qty, Rate, NegoRate, Total, StoreCode, Sysdate
            FROM Vw_Quotation WITH (NOLOCK)
            WHERE PurchaseCode = '{lit}'
            ORDER BY FirmName, ItemCode
            """;
        warning = $"Governed vendor quotations for PO {poCode} (Vw_Quotation).";
        return true;
    }

    private static bool TryBuildIndentQuotationSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("quot") && !m.Contains("quote") && !m.Contains("rate")) return false;

        var indent = TryExtractIndentNo(message);
        if (string.IsNullOrWhiteSpace(indent)) return false;

        var lit = EscapeSqlLiteral(indent);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                Storecode, SubCode, FirmName, ItemCode, ItemDesc, Qty, Rate, NegoRate, Total, Sysdate
            FROM Vw_IndentQuotation WITH (NOLOCK)
            WHERE Storecode = '{lit}' OR Storecode LIKE '%{lit}%'
            ORDER BY FirmName, Rate
            """;
        warning = $"Governed indent-line quotations for {indent} (Vw_IndentQuotation).";
        return true;
    }

    private static bool LooksLikeFinalQuotationQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("awarded") || m.Contains("final quotation") || m.Contains("selected vendor")
                || m.Contains("final vendor") || m.Contains("who won"))
               && (m.Contains("po") || m.Contains("purchase") || TryExtractPurchaseOrPoCode(message) is not null);
    }

    private static bool TryBuildFinalQuotationSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeFinalQuotationQuestion(message) && !message.Contains("FinalQuotation", StringComparison.OrdinalIgnoreCase))
        {
            if (!LooksLikeVendorQuotationQuestion(message)) return false;
            var m = message.ToLowerInvariant();
            if (!m.Contains("final") && !m.Contains("awarded")) return false;
        }

        var poCode = TryExtractPurchaseOrPoCode(message);
        if (string.IsNullOrWhiteSpace(poCode)) return false;

        var lit = EscapeSqlLiteral(poCode);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, FirmName, ItemCode, ItemDesc, Qty, Rate, Total, GST, Sysdate
            FROM FinalQuotation WITH (NOLOCK)
            WHERE PurchaseCode = '{lit}'
            ORDER BY FirmName, ItemCode
            """;
        warning = $"Governed awarded/final quotation lines for PO {poCode} (FinalQuotation).";
        return true;
    }

    private static bool LooksLikeSalesInvoiceItemsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("invoice") || m.Contains("inv "))
               && (m.Contains("item") || m.Contains("line") || m.Contains("qty")
                   || m.Contains("rate") || TryExtractInvoiceNo(message) is not null);
    }

    private static bool TryBuildSalesInvoiceItemsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeSalesInvoiceItemsQuestion(message)) return false;

        var invNo = TryExtractInvoiceNo(message);
        if (string.IsNullOrWhiteSpace(invNo)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                svi.CompanyName, svi.InvNo, svi.ITEMCODE, svi.ItemDesc, svi.ActualQty, svi.Rate, svi.Amount, svi.Unit
            FROM SalesVoucherItem svi WITH (NOLOCK)
            WHERE svi.CompanyName = '{EscapeSqlLiteral(company)}'
              AND svi.InvNo = '{EscapeSqlLiteral(invNo)}'
            ORDER BY svi.ITEMCODE
            """;
        warning = $"Governed sales invoice line items for InvNo {invNo} at {company} (SalesVoucherItem).";
        return true;
    }

    private static bool TryBuildCreditNoteListSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeCreditNoteQuestion(message)) return false;

        var docNo = TryExtractDebitCreditDocNo(message);
        if (!string.IsNullOrWhiteSpace(docNo) && docNo.Contains("/CR/", StringComparison.OrdinalIgnoreCase))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    creditnotenumber, companyname, partyname, totalcreditamount, credittype, creditnotedate, invno
                FROM vw_creditnote WITH (NOLOCK)
                WHERE creditnotenumber LIKE '%{EscapeSqlLiteral(docNo)}%'
                ORDER BY creditnotedate DESC
                """;
            warning = $"Governed credit note lookup for {docNo} (vw_creditnote).";
            return true;
        }

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var party = TryExtractLedgerPartyName(message);

        if (string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(party))
        {
            if (!TryBuildCreditNoteCompanyPartySql(message, out sql)) return false;
            warning = "Governed credit note list (vw_creditnote; company + party filters).";
            return true;
        }

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"companyname = '{EscapeSqlLiteral(company)}'");
        if (!string.IsNullOrWhiteSpace(party))
            filters.Add($"partyname LIKE '%{EscapeSqlLiteral(party)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                creditnotenumber, companyname, partyname, totalcreditamount, credittype, creditnotedate, invno
            FROM vw_creditnote WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY creditnotedate DESC
            """;
        warning = "Governed credit note list (vw_creditnote; CompanyName=ours, PartyName=customer).";
        return true;
    }

    private static bool TryBuildDebitNoteListSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeDebitNoteQuestion(message)) return false;

        var docNo = TryExtractDebitCreditDocNo(message);
        if (!string.IsNullOrWhiteSpace(docNo) && docNo.Contains("/DB/", StringComparison.OrdinalIgnoreCase))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    DebitNoteNumber, CompanyName, PartyName, TotalDebitAmount, DebitType, DebitNoteDate, BillNo, MRNo
                FROM vw_DebitNote WITH (NOLOCK)
                WHERE DebitNoteNumber LIKE '%{EscapeSqlLiteral(docNo)}%'
                ORDER BY DebitNoteDate DESC
                """;
            warning = $"Governed debit note lookup for {docNo} (vw_DebitNote).";
            return true;
        }

        var company = TryResolveDebitNoteCompany(message, "") ?? ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        var filters = new List<string> { $"CompanyName = '{EscapeSqlLiteral(company)}'" };
        var vendor = TryExtractVendorFirmName(message) ?? TryExtractLedgerPartyName(message);
        if (!string.IsNullOrWhiteSpace(vendor))
            filters.Add($"PartyName LIKE '%{EscapeSqlLiteral(vendor)}%'");

        if (message.Contains("provisional", StringComparison.OrdinalIgnoreCase))
            filters.Add("DebitType LIKE '%Provisional%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                DebitNoteNumber, CompanyName, PartyName, TotalDebitAmount, DebitType, DebitNoteDate, BillNo, MRNo
            FROM vw_DebitNote WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY DebitNoteDate DESC
            """;
        warning = $"Governed debit note list for {company} (vw_DebitNote; CompanyName=ours, PartyName=vendor).";
        return true;
    }

    private static bool TryBuildGatePassEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeGatePassQuestion(message)) return false;

        if (TryExtractGatePassRef(message, "") is { } gpRef)
        {
            sql = BuildGatePassSql(gpRef);
            warning = $"Governed gate pass {gpRef.Prefix}/.../{gpRef.PassKind}/{gpRef.Serial} (Vw_ReturnGatePass/NRGP/IGP).";
            return true;
        }

        if (TryResolveGatePassCompanyListRewrite(message, "") is { } gpList)
        {
            sql = BuildGatePassCompanyListSql(gpList);
            warning = gpList.PendingOnly
                ? "Governed pending returnable gate passes (vw_returngatepasspending, PendingQty > 0)."
                : "Governed gate pass list (CompName LIKE + GatePassNo prefix).";
            return true;
        }

        return false;
    }

    private static bool TryBuildIssueSlipEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeIssueSlipQuestion(message)) return false;

        var slip = TryExtractIssueSlipNo(message);
        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");

        if (string.IsNullOrWhiteSpace(slip) && string.IsNullOrWhiteSpace(company)) return false;

        if (!string.IsNullOrWhiteSpace(slip))
        {
            var slipLit = EscapeSqlLiteral(slip);
            var companyFilter = string.IsNullOrWhiteSpace(company)
                ? ""
                : $" AND CompName = '{EscapeSqlLiteral(company)}'";
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    IssueSlipNo, CompName, Itemcode, ItemName, Qty, Deptt, IssueTo, WareHouse, sysDate
                FROM StoreOutwards WITH (NOLOCK)
                WHERE IssueSlipNo = '{slipLit}'{companyFilter}
                ORDER BY sysDate DESC
                """;
            warning = $"Governed issue slip {slip} on StoreOutwards (not despatch InvNo).";
            return true;
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                IssueSlipNo, CompName, Itemcode, ItemName, Qty, Deptt, IssueTo, WareHouse, sysDate
            FROM StoreOutwards WITH (NOLOCK)
            WHERE CompName = '{EscapeSqlLiteral(company!)}'
            ORDER BY sysDate DESC
            """;
        warning = $"Governed recent store issues for {company} (StoreOutwards).";
        return true;
    }

    private static bool TryBuildTodayOutwardEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeTodayOutwardQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "")
                      ?? "Oswal Extrusion Limited";
        var companyLit = EscapeSqlLiteral(company);

        if (message.Contains("inward", StringComparison.OrdinalIgnoreCase)
            && message.Contains("outward", StringComparison.OrdinalIgnoreCase))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    ItemCode, ItemName, InwardQty, Outwardqty, [Date]
                FROM vw_ItemInwardOutward WITH (NOLOCK)
                WHERE companyname = '{companyLit}'
                  AND CAST([Date] AS date) = (
                      SELECT MAX(CAST([Date] AS date)) FROM vw_ItemInwardOutward
                      WHERE companyname = '{companyLit}')
                ORDER BY Outwardqty DESC
                """;
        }
        else
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Itemcode, ItemName, Qty, IssueTo, Deptt, IssueSlipNo, sysDate
                FROM StoreOutwards WITH (NOLOCK)
                WHERE CompName = '{companyLit}'
                  AND CAST(sysDate AS date) = (
                      SELECT MAX(CAST(sysDate AS date)) FROM StoreOutwards
                      WHERE CompName = '{companyLit}')
                ORDER BY Qty DESC
                """;
        }

        warning = $"Governed outward/issue for latest available business date at {company} (not bare GETDATE()).";
        return true;
    }

    private static bool LooksLikeJobWorkEbdQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("job work") || m.Contains("jobwork"))
               && (m.Contains("qty") || m.Contains("material") || m.Contains("ebd") || m.Contains("at job"));
    }

    private static bool TryBuildJobWorkEbdSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeJobWorkEbdQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        var filters = new List<string> { $"companyname = '{EscapeSqlLiteral(company)}'" };
        var itemMatch = Regex.Match(message, @"\b(?:item|code)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        if (itemMatch.Success)
            filters.Add($"ItemCode LIKE '%{EscapeSqlLiteral(itemMatch.Groups[1].Value)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname, ItemCode, ItemName, Qty, GroupName, DescriptionGoods, sysdate
            FROM VW_JobWork_EBD_DTL WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY sysdate DESC
            """;
        warning = $"Governed job-work material qty at {company} (VW_JobWork_EBD_DTL).";
        return true;
    }

    private static bool LooksLikeJobWorkReceiptQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("job work") || m.Contains("jobwork") || m.Contains("jbin"))
               && (m.Contains("receipt") || m.Contains("received") || m.Contains("mrno"));
    }

    private static bool TryBuildJobWorkReceiptSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeJobWorkReceiptQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, ITEMCODE, ITEMNAME, MRNo, Qty, GroupName, sysdate
            FROM VW_RECJOBWORK_EBD_DTL WITH (NOLOCK)
            {(filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "")}
            ORDER BY sysdate DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed receipts from job work (VW_RECJOBWORK_EBD_DTL)."
            : $"Governed job-work receipts for {company} (VW_RECJOBWORK_EBD_DTL).";
        return true;
    }

    private static bool TryBuildJobWorkOrderSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("job work") && !m.Contains("jobwork") && !m.Contains("jwo") && !m.Contains("jro")) return false;

        var code = TryExtractJobWorkOrderCode(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(code))
            filters.Add($"(PurchaseCode = '{EscapeSqlLiteral(code)}' OR PurchaseCode LIKE '%{EscapeSqlLiteral(code)}%')");
        else
        {
            var company = ResolveOutwardCompanyAlias(message)
                          ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
            if (string.IsNullOrWhiteSpace(company)) return false;
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, CompanyName, FirmName, ItemCode, ItemDesc, Qty, Rate, JobWorkCharges, GatePassNo
            FROM Vw_EditJOBWorkOrder WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY PurchaseCode DESC
            """;
        warning = "Governed formal job-work orders (Vw_EditJOBWorkOrder).";
        return true;
    }

    private static bool LooksLikePoPendingReceiptQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("import")) return false;
        return (m.Contains("po") || m.Contains("purchase order"))
               && (m.Contains("pending") && (m.Contains("mrn") || m.Contains("receipt") || m.Contains("receive")))
               || m.Contains("pending qty on po")
               || m.Contains("po lines pending");
    }

    private static bool TryBuildPoPendingReceiptSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePoPendingReceiptQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var filters = new List<string> { "PendingQty > 0" };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var poCode = TryExtractPurchaseOrPoCode(message);
        if (!string.IsNullOrWhiteSpace(poCode))
            filters.Add($"PurchaseCode LIKE '%{EscapeSqlLiteral(poCode)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PurchaseCode, CompanyName, FirmName, ItemCode, ItemDesc, Qty, PendingQty, Rate, Total
            FROM Vw_PurchaseOrder WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY PendingQty DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed PO lines with pending receipt qty (Vw_PurchaseOrder.PendingQty > 0)."
            : $"Governed PO pending receipt for {company} (Vw_PurchaseOrder).";
        return true;
    }

    private static bool LooksLikeFibcBagProductionQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("fibc") && (m.Contains("production") || m.Contains("bag") || m.Contains("produced"));
    }

    private static bool TryBuildFibcBagProductionSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeFibcBagProductionQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, PartyName, TYPEOFBAG, BagPCS, BagWt, TeamNO, PONO, Sysdate
            FROM VW_FIBCBagwiseProduction WITH (NOLOCK)
            {where}
            ORDER BY Sysdate DESC
            """;
        warning = "Governed FIBC bag production (VW_FIBCBagwiseProduction; not SmallBagProductionEntry).";
        return true;
    }
}
