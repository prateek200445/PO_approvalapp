using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    /// <summary>
    /// Governed SQL for common portal domains — skips LLM when matched.
    /// </summary>
    private static bool TryBuildGovernedDomainSql(
        string message,
        out string sql,
        out string warning)
    {
        // Specific intents before generic pending PO / WO rewrites (those also match broad "pending PO" text).
        if (TryBuildCountryWiseSalesSql(message, out sql, out warning)) return true;
        if (TryBuildMrnPaymentEarlySql(message, out sql, out warning)) return true;
        if (TryBuildMrnByBillNoEarlySql(message, out sql, out warning)) return true;
        if (TryBuildMrnByMrNoEarlySql(message, out sql, out warning)) return true;
        if (TryBuildMrnPendingQtyEarlySql(message, out sql, out warning)) return true;
        if (TryBuildMrnPartyReceiptsEarlySql(message, out sql, out warning)) return true;
        if (TryBuildVendorProfileEarlySql(message, out sql, out warning)) return true;
        if (TryBuildVendorCodeEarlySql(message, out sql, out warning)) return true;
        if (TryBuildVendorRateEarlySql(message, out sql, out warning)) return true;
        if (TryBuildMsmeVendorListEarlySql(message, out sql, out warning)) return true;
        if (TryBuildInternalVendorEarlySql(message, out sql, out warning)) return true;
        if (TryBuildFinalQuotationSql(message, out sql, out warning)) return true;
        if (TryBuildQuotationByPoSql(message, out sql, out warning)) return true;
        if (TryBuildIndentQuotationSql(message, out sql, out warning)) return true;
        if (TryBuildSalesInvoiceItemsSql(message, out sql, out warning)) return true;
        if (TryBuildCreditNoteListSql(message, out sql, out warning)) return true;
        if (TryBuildDebitNoteListSql(message, out sql, out warning)) return true;
        if (TryBuildGatePassEarlySql(message, out sql, out warning)) return true;
        if (TryBuildIssueSlipEarlySql(message, out sql, out warning)) return true;
        if (TryBuildTodayOutwardEarlySql(message, out sql, out warning)) return true;
        if (TryBuildJobWorkOrderSql(message, out sql, out warning)) return true;
        if (TryBuildJobWorkEbdSql(message, out sql, out warning)) return true;
        if (TryBuildJobWorkReceiptSql(message, out sql, out warning)) return true;
        if (TryBuildPoPendingReceiptSql(message, out sql, out warning)) return true;
        if (TryBuildFibcBagProductionSql(message, out sql, out warning)) return true;
        if (TryBuildDepartmentWastageSql(message, out sql, out warning)) return true;
        if (TryBuildStitcherAttendanceSql(message, out sql, out warning)) return true;
        if (TryBuildLoomRollsSql(message, out sql, out warning)) return true;
        if (TryBuildTapePlantEarlySql(message, out sql, out warning)) return true;
        if (TryBuildFactoryProductionEarlySql(message, out sql, out warning)) return true;
        if (TryBuildWipReportEarlySql(message, out sql, out warning)) return true;
        if (TryBuildProductionEbdEarlySql(message, out sql, out warning)) return true;
        if (TryBuildRollDespatchEarlySql(message, out sql, out warning)) return true;
        if (TryBuildFibcDespatchEarlySql(message, out sql, out warning)) return true;
        if (TryBuildYarnDespatchEarlySql(message, out sql, out warning)) return true;
        if (TryBuildSmallBagDespatchEarlySql(message, out sql, out warning)) return true;
        if (TryBuildUserLookupEarlySql(message, out sql, out warning)) return true;
        if (TryBuildIndentItemsEarlySql(message, out sql, out warning)) return true;
        if (TryBuildSalesEbdEarlySql(message, out sql, out warning)) return true;
        if (TryBuildExportDebtorsDueSql(message, out sql, out warning)) return true;
        if (TryBuildJobMrnPendingWoSql(message, out sql, out warning)) return true;
        if (TryBuildPoAmendmentSql(message, out sql, out warning)) return true;
        if (TryBuildBillPaymentDraftSql(message, out sql, out warning)) return true;
        if (TryBuildPurchaseReqSql(message, out sql, out warning)) return true;
        if (TryBuildSmallBagProductionSql(message, out sql, out warning)) return true;
        if (TryBuildLedgerGroupingSql(message, out sql, out warning)) return true;
        if (TryBuildAccountVoucherApprovalSql(message, out sql, out warning)) return true;
        if (TryBuildVoucherPartySql(message, out sql, out warning)) return true;
        if (TryBuildEditPurchaseOrderSql(message, out sql, out warning)) return true;
        if (TryBuildImportPoMrnPendingSql(message, out sql, out warning)) return true;
        if (TryBuildExtendedGovernanceSql(message, out sql, out warning)) return true;
        if (TryBuildPurchaseVoucherSql(message, out sql, out warning)) return true;
        if (TryBuildPaymentVoucherSql(message, out sql, out warning)) return true;
        if (TryBuildPaymentReceiptSql(message, out sql, out warning)) return true;
        if (TryBuildAdvanceBillOutstandingSql(message, out sql, out warning)) return true;
        if (TryBuildDueOverDueSql(message, out sql, out warning)) return true;
        if (TryBuildDueDateCashFlowSql(message, out sql, out warning)) return true;
        if (TryBuildProductLineSalesSql(message, out sql, out warning)) return true;
        if (TryBuildSalesTotalsSql(message, out sql, out warning)) return true;
        if (TryBuildSalesByGroupSql(message, out sql, out warning)) return true;
        if (TryBuildPurchaseTotalsSql(message, out sql, out warning)) return true;
        if (TryBuildLedgerCountSql(message, out sql, out warning)) return true;
        if (TryBuildLedgerGroupsSql(message, out sql, out warning)) return true;
        if (TryBuildPoQueueCompareSql(message, out sql, out warning)) return true;
        if (TryBuildRejectedPoSql(message, out sql, out warning)) return true;
        if (TryBuildRejectedBillPaymentSql(message, out sql, out warning)) return true;
        if (TryBuildApprovedPaymentTotalSql(message, out sql, out warning)) return true;
        if (TryBuildPoHeaderSql(message, out sql, out warning)) return true;
        if (TryBuildHighValuePoSql(message, out sql, out warning)) return true;
        if (TryBuildSalesInvoiceTaxSql(message, out sql, out warning)) return true;
        if (TryBuildAboveMaxStockSql(message, out sql, out warning)) return true;
        if (TryBuildRmWarehouseStockSql(message, out sql, out warning)) return true;
        if (TryBuildStockInHandSql(message, out sql, out warning)) return true;
        if (TryBuildStoreIssueByDeptSql(message, out sql, out warning)) return true;
        if (TryBuildInterUnitSalesSql(message, out sql, out warning)) return true;
        if (TryBuildExportSalesInvoiceListSql(message, out sql, out warning)) return true;
        if (TryBuildRollsWaitingDespatchSql(message, out sql, out warning)) return true;
        if (TryBuildWebbingProductionSql(message, out sql, out warning)) return true;
        if (TryBuildLoomProductionByQualitySql(message, out sql, out warning)) return true;
        if (TryBuildDailyInwardOutwardSql(message, out sql, out warning)) return true;
        if (TryBuildItemFromRecentOutwardSql(message, out sql, out warning)) return true;
        if (TryBuildPoAllocationSql(message, out sql, out warning)) return true;
        if (TryBuildPendingBillPaymentSql(message, out sql, out warning)) return true;
        if (TryBuildPendingIndentSql(message, out sql, out warning)) return true;
        if (TryBuildPendingPoApprovalSql(message, "", out sql, out warning)) return true;
        if (TryBuildPendingWorkOrderSql(message, "", out sql, out warning)) return true;

        sql = "";
        warning = "";
        return false;
    }

    private static bool TryBuildRejectedPoSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("reject")) return false;
        if (!ContainsPoIntent(message)) return false;

        var days = 30;
        var dayMatch = Regex.Match(m, @"\blast\s+(\d{1,3})\s+days?\b");
        if (dayMatch.Success && int.TryParse(dayMatch.Groups[1].Value, out var d))
            days = Math.Clamp(d, 1, 365);

        sql = $"""
            SELECT TOP 50 PoNo, ApprovalName, status, ApprovalDate, Queue
            FROM (
                SELECT PoNo, ApprovalName, status, ApprovalDate, 'Standard' AS Queue
                FROM ApprovePO
                WHERE status = 'Rejected'
                  AND ApprovalDate >= DATEADD(day, -{days}, CAST(GETDATE() AS date))
                UNION ALL
                SELECT PoNo, ApprovalName, status, ApprovalDate, 'HOD' AS Queue
                FROM ApprovePOHOD
                WHERE status = 'Rejected'
                  AND ApprovalDate >= DATEADD(day, -{days}, CAST(GETDATE() AS date))
            ) x
            ORDER BY ApprovalDate DESC
            """;
        warning = $"Governed rejected PO list (last {days} days) from ApprovePO + ApprovePOHOD.";
        return true;
    }

    private static bool TryBuildPoQueueCompareSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("pending")) return false;
        if (!(m.Contains("compare") || m.Contains("vs") || m.Contains("versus"))) return false;
        if (!m.Contains("hod") && !m.Contains("standard")) return false;
        if (!ContainsPoIntent(message)) return false;

        sql = """
            SELECT 'Standard' AS Queue, COUNT(*) AS PendingCount
            FROM ApprovePO WHERE status = 'Pending'
            UNION ALL
            SELECT 'HOD', COUNT(*)
            FROM ApprovePOHOD WHERE status = 'Pending'
            """;
        warning = "Governed PO pending queue comparison: ApprovePO vs ApprovePOHOD.";
        return true;
    }

    private static bool TryBuildPendingIndentSql(
        string message,
        out string sql,
        out string warning,
        bool relaxStoreDept = false)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("indent")) return false;
        if (!m.Contains("pending")) return false;

        var company = ResolveCompanyForChat(message);
        var deptFrag = TryExtractDepartmentFragment(message);
        var isCount = m.Contains("how many") || m.Contains("count") || m.StartsWith("number of");
        var storeIndentOnly = m.Contains("store indent");

        var filters = BuildPendingIndentFilters(company, deptFrag, storeIndentOnly, relaxStoreDept);
        var where = string.Join(" AND ", filters);

        if (isCount)
        {
            sql = $"""
                SELECT COUNT(DISTINCT ai.IndentNo + '|' + ai.IndentSubCode) AS PendingIndentCount
                FROM ApproveIndent ai
                WHERE {where}
                """;
            warning = relaxStoreDept
                ? "Governed pending indent COUNT (relaxed Store dept filter — company-only via Vw_StoreDeptt EXISTS)."
                : "Governed pending indent COUNT via ApproveIndent + Vw_StoreDeptt EXISTS (no INNER JOIN drop).";
        }
        else
        {
            sql = $"""
                SELECT TOP 50
                    ai.IndentNo,
                    ai.IndentSubCode,
                    sd.CompanyName,
                    sd.Deptt,
                    sd.ReqDepartment,
                    ai.ApprovalName,
                    ai.Status,
                    ai.IndentDate
                FROM ApproveIndent ai
                LEFT JOIN Vw_StoreDeptt sd ON ai.IndentNo = sd.Expr1 AND ai.IndentSubCode = sd.code
                WHERE {where}
                ORDER BY ai.IndentDate DESC
                """;
            warning = relaxStoreDept
                ? "Governed pending indent list (relaxed Store dept filter)."
                : "Governed pending indent list via ApproveIndent LEFT JOIN Vw_StoreDeptt.";
        }
        return true;
    }

    private static List<string> BuildPendingIndentFilters(
        string? company,
        string? deptFrag,
        bool storeIndentOnly,
        bool relaxStoreDept)
    {
        var filters = new List<string> { "ai.Status = 'Pending'" };

        if (!string.IsNullOrWhiteSpace(company))
        {
            var c = EscapeSqlLiteral(company);
            filters.Add($"""
                EXISTS (
                    SELECT 1 FROM Vw_StoreDeptt sx
                    WHERE sx.Expr1 = ai.IndentNo
                      AND sx.CompanyName = '{c}'
                )
                """);
        }

        if (storeIndentOnly && !relaxStoreDept)
            filters.Add(BuildStoreIndentPredicate());

        if (!string.IsNullOrWhiteSpace(deptFrag) && !(relaxStoreDept && deptFrag.Equals("Store", StringComparison.OrdinalIgnoreCase)))
        {
            var d = EscapeSqlLiteral(deptFrag);
            if (deptFrag.Equals("Store", StringComparison.OrdinalIgnoreCase) && !relaxStoreDept)
                filters.Add(BuildStoreIndentPredicate());
            else
            {
                filters.Add($"""
                    (
                        EXISTS (
                            SELECT 1 FROM Vw_StoreDeptt sx
                            WHERE sx.Expr1 = ai.IndentNo
                              AND sx.code = ai.IndentSubCode
                              AND (sx.Deptt LIKE '%{d}%' OR sx.ReqDepartment LIKE '%{d}%')
                        )
                        OR EXISTS (
                            SELECT 1 FROM Vw_StoreDeptt sx
                            WHERE sx.Expr1 = ai.IndentNo
                              AND (sx.Deptt LIKE '%{d}%' OR sx.ReqDepartment LIKE '%{d}%')
                        )
                        OR ai.IndentNo LIKE '%/{d.ToUpperInvariant()}%'
                    )
                    """);
            }
        }

        return filters;
    }

    private static string BuildStoreIndentPredicate()
    {
        return """
            (
                ai.IndentNo LIKE '%/STR/%'
                OR ai.IndentNo LIKE '%/STO/%'
                OR ai.IndentNo LIKE '%/STORE/%'
                OR EXISTS (
                    SELECT 1 FROM Vw_StoreDeptt sx
                    WHERE sx.Expr1 = ai.IndentNo
                      AND sx.code = ai.IndentSubCode
                      AND (sx.Deptt LIKE '%Store%' OR sx.ReqDepartment LIKE '%Store%')
                )
                OR EXISTS (
                    SELECT 1 FROM Vw_StoreDeptt sx
                    WHERE sx.Expr1 = ai.IndentNo
                      AND (sx.Deptt LIKE '%Store%' OR sx.ReqDepartment LIKE '%Store%')
                )
            )
            """;
    }

    private static bool TryBuildPendingBillPaymentSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("pending")) return false;
        if (!m.Contains("payment") && !m.Contains("bill payment")) return false;
        if (m.Contains("purchase order") || Regex.IsMatch(m, @"\bpos?\b") && !m.Contains("payment")) return false;

        var approver = TryExtractPersonName(message, "approver", "assigned to", "for approver");
        if (string.IsNullOrWhiteSpace(approver)) return false;

        sql = $"""
            SELECT TOP 50
                paymentNo,
                PartyName,
                PaymentAmount,
                BillNo,
                MRNo,
                ApprovalName,
                Status,
                PaymentDate
            FROM BillPaymentHODApproval
            WHERE Status = 'Pending'
              AND ApprovalName LIKE '%{EscapeSqlLiteral(approver)}%'
            ORDER BY PaymentAmount DESC
            """;
        warning = $"Governed pending bill payments for approver LIKE '%{approver}%' (BillPaymentHODApproval).";
        return true;
    }

    private static bool TryBuildRejectedBillPaymentSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("reject")) return false;
        if (!m.Contains("payment") && !m.Contains("bill payment")) return false;

        var (start, end, label) = TryParseRelativePeriod(message);
        sql = $"""
            SELECT TOP 50
                paymentNo,
                PartyName,
                PaymentAmount,
                Comment,
                ApprovalName,
                ApprovalDate,
                Status
            FROM BillPaymentHODApproval
            WHERE Status = 'Rejected'
              AND ApprovalDate >= '{start:yyyy-MM-dd}'
              AND ApprovalDate < '{end:yyyy-MM-dd}'
            ORDER BY ApprovalDate DESC
            """;
        warning = $"Governed rejected bill payments for {label} (BillPaymentHODApproval).";
        return true;
    }

    private static bool TryBuildApprovedPaymentTotalSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("payment")) return false;
        if (!m.Contains("approved") && !m.Contains("total")) return false;
        if (!m.Contains("amount") && !m.Contains("paymentamount") && !m.Contains("sum")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var sinceDate = TryParseSinceDate(message) ?? new DateTime(DateTime.Today.Year, 7, 1);
        var c = EscapeSqlLiteral(company);
        sql = $"""
            SELECT SUM(ISNULL(x.PaymentAmount, 0)) AS TotalApprovedPaymentAmount
            FROM (
                SELECT b.PaymentAmount
                FROM BillPaymentHODApproval b
                WHERE b.Status LIKE 'Approved%'
                  AND b.ApprovalDate >= '{sinceDate:yyyy-MM-dd}'
                  AND EXISTS (
                      SELECT 1 FROM BillPaymentEntry e
                      WHERE e.PaymentNo = b.paymentNo AND e.CompanyName = '{c}'
                  )
                UNION ALL
                SELECT e.PaymentAmount
                FROM BillPaymentEntry e
                WHERE e.CompanyName = '{c}'
                  AND e.status LIKE 'Approved%'
                  AND e.PaymentDate >= '{sinceDate:yyyy-MM-dd}'
                  AND NOT EXISTS (
                      SELECT 1 FROM BillPaymentHODApproval b
                      WHERE b.paymentNo = e.PaymentNo AND b.Status LIKE 'Approved%'
                  )
            ) x
            """;
        warning = $"Governed approved bill payment total for {company} since {sinceDate:yyyy-MM-dd} (HOD approval + entry-only approved).";
        return true;
    }

    private static bool TryBuildPoHeaderSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!ContainsPoIntent(message)) return false;
        if (!(m.Contains("currency") || m.Contains("delivery") || m.Contains("payment term"))) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var openOnly = m.Contains("open") || m.Contains("pending");
        var pendingJoin = openOnly
            ? """
              INNER JOIN (
                  SELECT PoNo FROM ApprovePO WHERE status = 'Pending'
                  UNION ALL
                  SELECT PoNo FROM ApprovePOHOD WHERE status = 'Pending'
              ) pend ON pp.PurchaseCode = pend.PoNo
              """
            : "";

        sql = $"""
            SELECT TOP 1
                pp.PurchaseCode,
                pp.CompanyName,
                pp.Currency,
                pp.deliverydate,
                pp.Payment AS PaymentTerms,
                pp.TotalAmount,
                pp.DepttName
            FROM PurchasePayment pp
            {pendingJoin}
            WHERE pp.CompanyName = '{EscapeSqlLiteral(company)}'
            ORDER BY pp.deliverydate DESC, pp.PurchaseCode DESC
            """;
        warning = openOnly
            ? $"Governed recent open/pending PO header from PurchasePayment for {company}."
            : $"Governed recent PO header from PurchasePayment for {company}.";
        return true;
    }

    private static bool TryBuildHighValuePoSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!ContainsPoIntent(message) && !m.Contains("purchase order")) return false;
        if (!(m.Contains("high-value") || m.Contains("high value")
              || m.Contains("descending") || m.Contains("largest") || m.Contains("high-value")))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (fyStart, fyEnd, fyLabel) = ParseIndianFinancialYear(message);
        sql = $"""
            SELECT TOP 50
                pp.PurchaseCode,
                pp.CompanyName,
                pp.TotalAmount,
                pp.Currency,
                COALESCE(MIN(ap.PODate), MIN(pp.deliverydate)) AS PODate
            FROM PurchasePayment pp
            LEFT JOIN (
                SELECT PoNo, PODate FROM ApprovePO
                UNION ALL
                SELECT PoNo, PODate FROM ApprovePOHOD
            ) ap ON pp.PurchaseCode = ap.PoNo
            WHERE pp.CompanyName = '{EscapeSqlLiteral(company)}'
              AND (
                  (ap.PODate >= '{fyStart:yyyy-MM-dd}' AND ap.PODate < '{fyEnd:yyyy-MM-dd}')
                  OR (ap.PODate IS NULL AND pp.deliverydate >= '{fyStart:yyyy-MM-dd}' AND pp.deliverydate < '{fyEnd:yyyy-MM-dd}')
              )
            GROUP BY pp.PurchaseCode, pp.CompanyName, pp.TotalAmount, pp.Currency
            ORDER BY pp.TotalAmount DESC
            """;
        warning = $"Governed high-value PO list for {company} FY {fyLabel} (approval PODate or deliverydate fallback).";
        return true;
    }

    private static bool TryBuildAboveMaxStockSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("maximum") && !m.Contains("max level") && !m.Contains("max stock"))
            return false;
        if (!m.Contains("above") && !m.Contains("over") && !m.Contains("exceed")) return false;
        if (!m.Contains("stock") && !m.Contains("inventory")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP 50
                CompanyName,
                ItemCode,
                ItemName,
                WareHouseName,
                StkInHand,
                Maxlevel,
                Minlevel,
                ReOrder
            FROM WareHouse
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND ISNULL(Maxlevel, 0) > 0
              AND ISNULL(StkInHand, 0) > ISNULL(Maxlevel, 0)
            ORDER BY StkInHand - Maxlevel DESC
            """;
        warning = $"Governed above-max stock on WareHouse for {company} (StkInHand > Maxlevel).";
        return true;
    }

    private static bool TryBuildStoreIssueByDeptSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("issued") && !m.Contains("issue")) return false;
        if (!m.Contains("store") && !m.Contains("material")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var deptNames = TryExtractDepartmentList(message);
        if (deptNames.Count == 0) return false;

        var deptFilter = string.Join(" OR ",
            deptNames.Select(d =>
                $"(Deptt LIKE '%{EscapeSqlLiteral(d)}%' OR IssueTo LIKE '%{EscapeSqlLiteral(d)}%')"));

        var (monthStart, monthEnd, monthLabel) = TryParseMonthYear(message)
            ?? (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1),
                $"{DateTime.Today:MMMM yyyy}");

        sql = $"""
            SELECT TOP 50
                IssueSlipNo,
                Itemcode,
                ItemName,
                Qty,
                Deptt,
                IssueTo,
                WareHouse,
                sysDate
            FROM StoreOutwards
            WHERE CompName = '{EscapeSqlLiteral(company)}'
              AND ({deptFilter})
              AND sysDate >= '{monthStart:yyyy-MM-dd}'
              AND sysDate < '{monthEnd:yyyy-MM-dd}'
            ORDER BY sysDate DESC
            """;
        warning = $"Governed store issues for {company} ({monthLabel}) on StoreOutwards.CompName + Deptt/IssueTo.";
        return true;
    }

    private static bool TryBuildInterUnitSalesSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("inter-unit") && !m.Contains("inter unit") && !m.Contains("interunit")) return false;
        if (!m.Contains("sales") && !m.Contains("invoice")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP 50
                InvNo,
                InvDate,
                BuyerName,
                BillAMount,
                InvType,
                currency
            FROM vw_Salesvoucher
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND (InvType LIKE '%InterUnit%' OR InvType LIKE '%Inter Unit%' OR InvType LIKE '%Inter-Unit%')
            ORDER BY InvDate DESC
            """;
        warning = $"Governed inter-unit sales invoices on vw_Salesvoucher for {company}.";
        return true;
    }

    private static bool TryBuildExportSalesInvoiceListSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeExportSalesInvoiceListQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var companyLit = EscapeSqlLiteral(company);
        var dateFilter = "";
        var periodNote = "all dates";

        if (MessageRequestsSalesDateFilter(message))
        {
            var (start, end, label) = TryParseRelativePeriod(message);
            dateFilter = $"""
                
                  AND InvDate >= '{start:yyyy-MM-dd}'
                  AND InvDate < '{end:yyyy-MM-dd}'
                """;
            periodNote = label;
        }

        sql = $"""
            SELECT TOP 50
                InvNo,
                InvDate,
                BuyerName,
                BillAMount,
                InvType,
                CompanyName,
                currency
            FROM vw_Salesvoucher
            WHERE CompanyName = '{companyLit}'
              AND InvType LIKE '%Export%'{dateFilter}
            ORDER BY InvDate DESC
            """;
        warning =
            $"Governed export sales invoice list on vw_Salesvoucher for {company} ({periodNote}), InvType LIKE '%Export%'.";
        return true;
    }

    private static bool LooksLikeExportSalesInvoiceListQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("export")) return false;
        if (!m.Contains("invoice") && !m.Contains("sales")) return false;
        if (m.Contains("inter-unit") || m.Contains("inter unit") || m.Contains("interunit")) return false;

        // Top export customer ranking — different governed query
        if (m.Contains("customer") || m.Contains("buyer") || m.Contains("client"))
        {
            if (m.Contains("top") || m.Contains("rank") || m.Contains("largest") || m.Contains("highest"))
                return false;
        }

        return true;
    }

    private static bool MessageRequestsSalesDateFilter(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("this month") || m.Contains("last month") || m.Contains("last 30"))
            return true;
        return Regex.IsMatch(
            m,
            @"\b(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(\d{4})\b",
            RegexOptions.IgnoreCase);
    }

    private static bool IsGovernedExportSalesInvoiceSql(string sql) =>
        sql.Contains("vw_Salesvoucher", StringComparison.OrdinalIgnoreCase)
        && sql.Contains("InvType", StringComparison.OrdinalIgnoreCase)
        && sql.Contains("Export", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldForceExportSalesInvoiceRewrite(string message, string sql, int rowCount)
    {
        if (!LooksLikeExportSalesInvoiceListQuestion(message)) return false;
        if (sql.Contains("POAllocation", StringComparison.OrdinalIgnoreCase)) return true;
        return !IsGovernedExportSalesInvoiceSql(sql);
    }

    private static bool TryBuildSalesInvoiceTaxSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("tax") && !m.Contains("gst")) return false;
        if (!m.Contains("invoice") && !m.Contains("sales")) return false;

        var company = ResolveCompanyForChat(message);
        var invNo = TryExtractInvoiceNumber(message);
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(invNo)) return false;

        sql = $"""
            SELECT TOP 50
                TaxledgerName,
                Amount,
                InvNo,
                CompanyName
            FROM SalesVoucherTax
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND (InvNo = '{EscapeSqlLiteral(invNo)}' OR InvNo LIKE '%{EscapeSqlLiteral(invNo)}%')
            ORDER BY Amount DESC
            """;
        warning = $"Governed sales invoice tax lines on SalesVoucherTax for {company} InvNo {invNo}.";
        return true;
    }

    private static bool TryBuildRollsWaitingDespatchSql(
        string message,
        out string sql,
        out string warning,
        bool relaxNeedleFilter = false)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("despatch") && !m.Contains("dispatch")) return false;
        if (!m.Contains("waiting") && !m.Contains("pending") && !m.Contains("roll")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var useNeedle = m.Contains("needle") && !relaxNeedleFilter;
        var needleFilter = useNeedle
            ? """
              AND (
                  Quality LIKE '%needle%'
                  OR Sector LIKE '%needle%'
                  OR Mesh LIKE '%needle%'
                  OR Quality LIKE '%NL%'
                  OR ROllNO LIKE '%NL%'
              )
              """
            : "";

        sql = $"""
            SELECT TOP 50
                ROllNO,
                Quality,
                NetWt,
                Companyname,
                Metre,
                Sector,
                Mesh
            FROM vw_RollforDespatch
            WHERE Companyname = '{EscapeSqlLiteral(company)}'
            {needleFilter}
            ORDER BY NetWt DESC
            """;
        warning = useNeedle
            ? $"Governed needle-loom rolls waiting on vw_RollforDespatch for {company} (Quality/Sector/Mesh/RollNO needle hints)."
            : relaxNeedleFilter
                ? $"Governed all rolls waiting for despatch on vw_RollforDespatch for {company} (needle filter relaxed — no needle-tagged rows found)."
                : $"Governed rolls waiting for despatch on vw_RollforDespatch for {company} (no Invno — not yet invoiced).";
        return true;
    }

    private static bool TryBuildWebbingProductionSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("webbing")) return false;
        if (!m.Contains("production") && !m.Contains("factory")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP 50
                companyname,
                Sysdate,
                Particulars,
                TapeProduction,
                Fabric,
                SmallBag,
                Loom,
                Wastage
            FROM vw_FactoryProduction
            WHERE companyname = '{EscapeSqlLiteral(company)}'
              AND Particulars LIKE '%WEBBING%'
              AND Sysdate >= (
                  SELECT MAX(Sysdate)
                  FROM vw_FactoryProduction
                  WHERE companyname = '{EscapeSqlLiteral(company)}'
                    AND Particulars LIKE '%WEBBING%'
              )
            ORDER BY Sysdate DESC
            """;
        warning = $"Governed WEBBING production on vw_FactoryProduction for {company} (latest available dates).";
        return true;
    }

    private static bool LooksLikeLoomRollsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("loom")) return false;
        if (m.Contains("loom dept") || m.Contains("loom department")) return false;
        if (m.Contains("despatch") || m.Contains("dispatch")) return false;
        if (LooksLikeTapePlantQuestion(message)) return false;
        return m.Contains("roll")
               || m.Contains("production")
               || m.Contains("produced")
               || m.Contains("output");
    }

    private static bool TryBuildLoomRollsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeLoomRollsQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, endExclusive, periodLabel) = ResolveLoomRollsPeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var m = message.ToLowerInvariant();
        var wantDetail = m.Contains("roll no") || m.Contains("rollno") || m.Contains("list")
                         || Regex.IsMatch(m, @"\bloom\s*(no|number|#)\s*\d");
        var wantDaily = m.Contains("daily") || m.Contains("day-wise") || m.Contains("day wise")
                        || m.Contains("per day") || m.Contains("by date");

        if (wantDetail)
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Sysdate,
                    LoomNo,
                    RollNo,
                    Quality,
                    NetWt,
                    Metre,
                    Partyname,
                    Shift
                FROM vw_LoomProductionENtry WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND Sysdate >= '{start:yyyy-MM-dd}'
                  AND Sysdate < '{endExclusive:yyyy-MM-dd}'
                ORDER BY Sysdate DESC, RollNo DESC
                """;
            warning =
                $"Governed loom roll entries for {company} ({periodLabel}) on vw_LoomProductionENtry (not vw_FactoryProduction.Loom).";
            return true;
        }

        if (wantDaily)
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    CAST(Sysdate AS date) AS ProdDate,
                    COUNT(*) AS RollCount,
                    ROUND(SUM(ISNULL(NetWt, 0)), 2) AS TotalNetWtKg,
                    ROUND(SUM(ISNULL(Metre, 0)), 2) AS TotalMetre
                FROM vw_LoomProductionENtry WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND Sysdate >= '{start:yyyy-MM-dd}'
                  AND Sysdate < '{endExclusive:yyyy-MM-dd}'
                GROUP BY CAST(Sysdate AS date)
                ORDER BY ProdDate DESC
                """;
            warning =
                $"Governed daily loom rolls for {company} ({periodLabel}) on vw_LoomProductionENtry.";
            return true;
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                Quality,
                COUNT(*) AS RollCount,
                ROUND(SUM(ISNULL(NetWt, 0)), 2) AS TotalNetWtKg,
                ROUND(SUM(ISNULL(Metre, 0)), 2) AS TotalMetre
            FROM vw_LoomProductionENtry WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND Sysdate >= '{start:yyyy-MM-dd}'
              AND Sysdate < '{endExclusive:yyyy-MM-dd}'
            GROUP BY Quality
            ORDER BY SUM(ISNULL(NetWt, 0)) DESC
            """;
        warning =
            $"Governed loom rolls by quality for {company} ({periodLabel}) on vw_LoomProductionENtry. Factory daily Loom column is unused; this is actual roll entries.";
        return true;
    }

    private static (DateTime Start, DateTime EndExclusive, string Label) ResolveLoomRollsPeriod(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("last month"))
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            return (start, start.AddMonths(1), "last month");
        }

        if (Regex.IsMatch(m, @"\bfy\b|financial\s+year|\d{2}\s*[-–/]\s*\d{2}"))
        {
            var (fyStart, fyEndEx, fyLabel) = ParseIndianFinancialYear(message);
            return (fyStart, fyEndEx, $"FY {fyLabel}");
        }

        var monthsMatch = Regex.Match(m, @"last\s+(\d{1,2})\s+months?");
        if (monthsMatch.Success && int.TryParse(monthsMatch.Groups[1].Value, out var months))
        {
            months = Math.Clamp(months, 1, 24);
            var end = DateTime.Today.AddDays(1);
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
            return (start, end, $"last {months} months");
        }

        var daysMatch = Regex.Match(m, @"last\s+(\d{1,3})\s+days?");
        if (daysMatch.Success && int.TryParse(daysMatch.Groups[1].Value, out var days))
        {
            days = Math.Clamp(days, 1, 366);
            return (DateTime.Today.AddDays(-days), DateTime.Today.AddDays(1), $"last {days} days");
        }

        if (m.Contains("yesterday"))
            return (DateTime.Today.AddDays(-1), DateTime.Today, "yesterday");
        if (m.Contains("today"))
            return (DateTime.Today, DateTime.Today.AddDays(1), "today");

        if (MessageHasExplicitPeriod(message))
        {
            var (start, end, label) = TryParseRelativePeriod(message);
            return (start, end, label);
        }

        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        return (lastMonthStart, lastMonthStart.AddMonths(1), "last month");
    }

    private static bool MessageHasExplicitPeriod(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("this month") || m.Contains("last month") || m.Contains("today") || m.Contains("yesterday"))
            return true;
        if (m.Contains("last 30") || m.Contains("ytd") || m.Contains("this year"))
            return true;
        if (Regex.IsMatch(m, @"\blast\s+\d{1,3}\s+(days?|months?)\b"))
            return true;
        return TryParseMonthYear(message) is not null;
    }

    private static bool TryBuildLoomProductionByQualitySql(string message, out string sql, out string warning)
        => TryBuildLoomRollsSql(message, out sql, out warning);

    private static bool TryBuildDailyInwardOutwardSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeDailyInwardOutwardQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var m = message.ToLowerInvariant();
        var hasInward = m.Contains("inward");
        var hasOutward = m.Contains("outward") || m.Contains("stock issued");
        var outwardOnly = hasOutward && !hasInward;
        var inwardOnly = hasInward && !hasOutward;
        var itemCode = TryExtractStockItemCode(message);
        var useLatestBusinessDate = m.Contains("today") || m.Contains("daily")
            || (LooksLikePluralItemsQuestion(message) && itemCode is null);

        string qtyFilter;
        string orderBy;
        if (outwardOnly)
        {
            qtyFilter = "AND ISNULL(Outwardqty, 0) <> 0";
            orderBy = "ORDER BY Outwardqty DESC";
        }
        else if (inwardOnly)
        {
            qtyFilter = "AND ISNULL(InwardQty, 0) <> 0";
            orderBy = "ORDER BY InwardQty DESC";
        }
        else
        {
            qtyFilter = "";
            orderBy = "ORDER BY [Date] DESC, Outwardqty DESC, InwardQty DESC";
        }

        var topN = itemCode is not null ? 10 : 50;
        var itemFilter = itemCode is not null
            ? $"AND ItemCode = '{EscapeSqlLiteral(itemCode)}'"
            : "";
        var dateFilter = useLatestBusinessDate
            ? $"""
              AND CAST([Date] AS date) = (
                  SELECT MAX(CAST([Date] AS date)) FROM vw_ItemInwardOutward
                  WHERE companyname = '{EscapeSqlLiteral(company)}')
              """
            : "";

        sql = $"""
            SELECT TOP {topN} ItemCode, ItemName, InwardQty, Outwardqty, [Date]
            FROM vw_ItemInwardOutward
            WHERE companyname = '{EscapeSqlLiteral(company)}'
              {itemFilter}
              {dateFilter}
              {qtyFilter}
            {orderBy}
            """;

        var scope = itemCode is not null ? $"item {itemCode}" : "all items";
        var dayNote = useLatestBusinessDate ? " (latest posted business date when 'today' is asked)" : "";
        warning =
            $"Governed daily inward/outward on vw_ItemInwardOutward for {company}, {scope}{dayNote}.";
        return true;
    }

    private static bool TryBuildItemFromRecentOutwardSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (LooksLikePluralItemsQuestion(message)) return false;
        if (LooksLikeDailyInwardOutwardQuestion(message)) return false;
        if (m.Contains("today")) return false;
        if (m.Contains("qty") || m.Contains("quantity") || m.Contains("quantities")) return false;
        if (!m.Contains("item") && !m.Contains("hsn") && !m.Contains("description")) return false;
        if (!m.Contains("outward") && !m.Contains("issued") && !m.Contains("store")) return false;
        if (!LooksLikeSingularItemQuestion(message)
            && !m.Contains("hsn")
            && !m.Contains("description"))
            return false;

        var company = ResolveCompanyForChat(message)
                      ?? "Oswal Extrusion Limited";

        sql = $"""
            SELECT TOP 1
                so.Itemcode AS ItemCode,
                i.itemdesc AS ItemDescription,
                i.Unit,
                COALESCE(po.hsncode, sv.HSNCODE) AS HSNCode,
                so.IssueSlipNo,
                so.sysDate AS IssueDate
            FROM StoreOutwards so
            INNER JOIN ItemInfo i ON so.Itemcode = i.itemcode
            OUTER APPLY (
                SELECT TOP 1 hsncode
                FROM Vw_PurchaseOrder
                WHERE ItemCode = so.Itemcode AND hsncode IS NOT NULL AND hsncode <> ''
                ORDER BY PurchaseCode DESC
            ) po
            OUTER APPLY (
                SELECT TOP 1 HSNCODE
                FROM SalesVoucherItem
                WHERE ITEMCODE = so.Itemcode AND HSNCODE IS NOT NULL AND HSNCODE <> ''
                ORDER BY InvNo DESC
            ) sv
            WHERE so.CompName = '{EscapeSqlLiteral(company)}'
            ORDER BY so.sysDate DESC
            """;
        warning = $"Governed recent store outward item master (ItemInfo + HSN from Vw_PurchaseOrder or SalesVoucherItem) for {company}.";
        return true;
    }

    private static bool TryBuildPoAllocationSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!ContainsAllocationLimitIntent(message)) return false;
        if (!ContainsPoIntent(message)) return false;
        if (m.Contains("export") || m.Contains("sales invoice") || m.Contains("invoice"))
            return false;

        var company = ResolveCompanyForChat(message);
        var user = TryExtractPoAllocationUsername(message);

        if (!string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(user))
        {
            sql = $"""
                SELECT TOP 50
                    username,
                    CompanyName,
                    POAmount,
                    ItemAmount,
                    PartyAmount,
                    Deptt,
                    authority,
                    NewVendor,
                    NewItem
                FROM POAllocation
                WHERE CompanyName = '{EscapeSqlLiteral(company)}'
                ORDER BY POAmount DESC, username
                """;
            warning = $"Governed PO allocation limits on POAllocation for CompanyName = '{company}'.";
            return true;
        }

        if (string.IsNullOrWhiteSpace(user)) return false;

        sql = $"""
            SELECT TOP 50
                username,
                CompanyName,
                POAmount,
                ItemAmount,
                PartyAmount,
                Deptt,
                authority,
                NewVendor,
                NewItem
            FROM POAllocation
            WHERE username LIKE '%{EscapeSqlLiteral(user)}%'
            ORDER BY POAmount DESC, username
            """;
        warning = $"Governed PO allocation limits on POAllocation for username LIKE '%{user}%'.";
        return true;
    }

    private static string? TryExtractPoAllocationUsername(string message) =>
        TryExtractPersonName(message, "username", "user", "for user", "for approver");

    /// <summary>Word-boundary PO intent — avoids matching "po" inside "export".</summary>
    private static bool ContainsPoIntent(string message) =>
        Regex.IsMatch(message, @"\b(?:pos?|purchase orders?)\b", RegexOptions.IgnoreCase);

    /// <summary>Allocation/limit intent — avoids matching "limit" inside "Limited".</summary>
    private static bool ContainsAllocationLimitIntent(string message) =>
        Regex.IsMatch(message, @"\b(?:allocation|authority|(?:approval )?limits?)\b", RegexOptions.IgnoreCase);

    private static string? TryExtractDepartmentFragment(string message)
    {
        var m = Regex.Match(
            message,
            @"\b(?:for|at|in)\s+(?:the\s+)?([A-Za-z][A-Za-z /&-]{1,40}?)\s+department\b",
            RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value.Trim();
        if (message.Contains("store department", StringComparison.OrdinalIgnoreCase))
            return "Store";
        return null;
    }

    private static List<string> TryExtractDepartmentList(string message)
    {
        var list = new List<string>();
        var m = Regex.Match(
            message,
            @"\b(?:to|for)\s+(?:the\s+)?(.+?)\s+department\b",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            foreach (var part in Regex.Split(m.Groups[1].Value, @"\s+or\s+|\s+and\s+|,", RegexOptions.IgnoreCase))
            {
                var p = part.Trim();
                if (p.Length >= 3) list.Add(p);
            }
        }
        if (list.Count == 0 && message.Contains("accounts", StringComparison.OrdinalIgnoreCase)
            && message.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            list.Add("Account");
            list.Add("Admin");
        }
        return list;
    }

    private static string? TryExtractPersonName(string message, params string[] afterMarkers)
    {
        foreach (var marker in afterMarkers)
        {
            var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var rest = message[(idx + marker.Length)..].Trim().TrimStart(',').Trim();
            if (rest.StartsWith("approver ", StringComparison.OrdinalIgnoreCase))
                rest = rest[9..].Trim();
            var m = Regex.Match(rest, @"^([A-Za-z][A-Za-z0-9._-]{1,40})");
            if (m.Success) return m.Groups[1].Value;
        }

        // "approver prakash" inline
        var inline = Regex.Match(message, @"\bapprover\s+([A-Za-z][A-Za-z0-9._-]{1,40})\b", RegexOptions.IgnoreCase);
        if (inline.Success) return inline.Groups[1].Value;

        var userInline = Regex.Match(message, @"\busername\s+([A-Za-z][A-Za-z0-9._-]{1,40})\b", RegexOptions.IgnoreCase);
        if (userInline.Success) return userInline.Groups[1].Value;

        return null;
    }

    private static string? TryExtractInvoiceNumber(string message)
    {
        var m = Regex.Match(message, @"\binvoice\s+(\d{1,10})\b", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;
        m = Regex.Match(message, @"\binv(?:oice)?\.?\s*#?\s*(\d{1,10})\b", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static DateTime? TryParseSinceDate(string message)
    {
        var m = Regex.Match(
            message,
            @"\bsince\s+(\d{1,2})\s+(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(\d{4})\b",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        if (!int.TryParse(m.Groups[1].Value, out var day)) return null;
        if (!int.TryParse(m.Groups[3].Value, out var year)) return null;
        var month = ParseMonthName(m.Groups[2].Value);
        if (month is null) return null;
        try { return new DateTime(year, month.Value, day); }
        catch { return null; }
    }

    private static (DateTime Start, DateTime End, string Label) TryParseRelativePeriod(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("last month"))
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            return (start, start.AddMonths(1), "last month");
        }
        if (m.Contains("this month"))
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return (start, start.AddMonths(1), "this month");
        }
        if (m.Contains("last 30 days") || m.Contains("last thirty days"))
            return (DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1), "last 30 days");

        var monthYear = TryParseMonthYear(message);
        if (monthYear.HasValue)
            return (monthYear.Value.Start, monthYear.Value.End, monthYear.Value.Label);

        var startMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return (startMonth, startMonth.AddMonths(1), "this month");
    }

    private static (DateTime Start, DateTime End, string Label)? TryParseMonthYear(string message)
    {
        var m = Regex.Match(
            message,
            @"\b(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(\d{4})\b",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        if (!int.TryParse(m.Groups[2].Value, out var year)) return null;
        var month = ParseMonthName(m.Groups[1].Value);
        if (month is null) return null;
        var start = new DateTime(year, month.Value, 1);
        return (start, start.AddMonths(1), $"{m.Groups[1].Value} {year}");
    }

    private static int? ParseMonthName(string token)
    {
        var t = token.ToLowerInvariant();
        if (t.StartsWith("jan")) return 1;
        if (t.StartsWith("feb")) return 2;
        if (t.StartsWith("mar")) return 3;
        if (t.StartsWith("apr")) return 4;
        if (t.StartsWith("may")) return 5;
        if (t.StartsWith("jun")) return 6;
        if (t.StartsWith("jul")) return 7;
        if (t.StartsWith("aug")) return 8;
        if (t.StartsWith("sep")) return 9;
        if (t.StartsWith("oct")) return 10;
        if (t.StartsWith("nov")) return 11;
        if (t.StartsWith("dec")) return 12;
        return null;
    }

    private static bool LooksLikePendingIndentQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("indent") && m.Contains("pending");
    }

    private static bool LooksLikeAboveMaxStockQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("maximum") || m.Contains("max level") || m.Contains("max stock"))
               && (m.Contains("above") || m.Contains("over") || m.Contains("exceed"))
               && (m.Contains("stock") || m.Contains("inventory"));
    }

    private static bool TryBuildAboveMaxStockViaInventorySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP 50
                inv.CompanyName,
                inv.itemcode AS ItemCode,
                inv.ItemName,
                inv.warehouse AS WareHouseName,
                wh.StkInHand,
                wh.Maxlevel,
                wh.Minlevel,
                wh.ReOrder
            FROM vw_inventoryitemwarehouse_all inv
            INNER JOIN WareHouse wh
                ON inv.itemcode = wh.ItemCode
               AND inv.CompanyName = wh.CompanyName
            WHERE inv.CompanyName = '{EscapeSqlLiteral(company)}'
              AND ISNULL(wh.Maxlevel, 0) > 0
              AND ISNULL(wh.StkInHand, 0) > ISNULL(wh.Maxlevel, 0)
            ORDER BY wh.StkInHand - wh.Maxlevel DESC
            """;
        warning = $"Governed above-max stock via vw_inventoryitemwarehouse_all + WareHouse max levels for {company}.";
        return true;
    }

    private static bool LooksLikeSalesTotalsQuestion(string message)
    {
        if (LooksLikeCountryWiseSalesQuestion(message))
            return false;
        if (LooksLikeProductLineSalesQuestion(message))
            return false;
        if (LooksLikeSalesByGroupQuestion(message))
            return false;
        if (LooksLikeCustomerSalesCurrencyQuestion(message))
            return false;

        var m = message.ToLowerInvariant();
        if (!m.Contains("sales") && !m.Contains("sale"))
            return false;

        return m.Contains("total sales")
               || m.Contains("sales total")
               || m.Contains("how much sales")
               || m.Contains("grand total")
               || (m.Contains("total") && m.Contains("amount") && m.Contains("sales"))
               || (m.Contains("sales") && m.Contains("fy") && m.Contains("total"));
    }

    private static bool LooksLikeSalesByGroupQuestion(string message)
    {
        if (LooksLikeCountryWiseSalesQuestion(message))
            return false;
        if (LooksLikeProductLineSalesQuestion(message))
            return false;

        var m = message.ToLowerInvariant();
        if (!m.Contains("sales") && !m.Contains("sale"))
            return false;

        return m.Contains("by group")
               || m.Contains("group wise")
               || m.Contains("group-wise")
               || m.Contains("product group")
               || m.Contains("sub group")
               || m.Contains("subgroup")
               || (m.Contains("group") && m.Contains("breakdown"));
    }

    private static bool LooksLikePurchaseTotalsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("purchase"))
            return false;

        return m.Contains("total purchase")
               || m.Contains("purchase total")
               || m.Contains("how much purchase")
               || (m.Contains("total") && m.Contains("amount") && m.Contains("purchase"));
    }

    private static bool LooksLikeLedgerCountQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("how many") || m.Contains("count") || m.Contains("number of"))
               && m.Contains("ledger");
    }

    private static bool TryBuildSalesTotalsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeSalesTotalsQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
        var startLit = fyStart.ToString("yyyy-MM-dd");
        var endLit = fyEndExclusive.AddDays(-1).ToString("yyyy-MM-dd");
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                ROUND(SUM(Amount), 0) AS TotalSales,
                ROUND(SUM(netwt), 0) AS TotalQuantity
            FROM vw_Sales_EBIDTA
            WHERE CompanyName = '{companyLit}'
              AND invdate >= '{startLit}'
              AND invdate <= '{endLit}'
              AND InterGroup <> 'Intergroup'
            """;

        warning =
            $"Governed total sales on vw_Sales_EBIDTA for {company} FY {fyLabel} ({startLit} to {endLit}), excl. intercompany.";
        return true;
    }

    private static bool TryBuildSalesByGroupSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeSalesByGroupQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
        var startLit = fyStart.ToString("yyyy-MM-dd");
        var endLit = fyEndExclusive.AddDays(-1).ToString("yyyy-MM-dd");
        var companyLit = EscapeSqlLiteral(company);
        var wantsSubGroup = message.Contains("sub group", StringComparison.OrdinalIgnoreCase)
                            || message.Contains("subgroup", StringComparison.OrdinalIgnoreCase);

        if (wantsSubGroup)
        {
            sql = $"""
                SELECT TOP 50
                    Groupname,
                    SubGroupName,
                    ROUND(SUM(Amount), 0) AS SalesAmount,
                    ROUND(SUM(netwt), 0) AS Quantity
                FROM vw_Sales_EBIDTA
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{startLit}'
                  AND invdate <= '{endLit}'
                  AND InterGroup <> 'Intergroup'
                  AND ISNULL(Groupname, '') <> ''
                  AND ISNULL(SubGroupName, '') <> ''
                GROUP BY Groupname, SubGroupName
                ORDER BY SUM(Amount) DESC
                """;
            warning =
                $"Governed sales by sub-group on vw_Sales_EBIDTA for {company} FY {fyLabel}, excl. intercompany.";
        }
        else
        {
            sql = $"""
                SELECT TOP 50
                    Groupname,
                    ROUND(SUM(Amount), 0) AS SalesAmount,
                    ROUND(SUM(netwt), 0) AS Quantity
                FROM vw_Sales_EBIDTA
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{startLit}'
                  AND invdate <= '{endLit}'
                  AND InterGroup <> 'Intergroup'
                  AND ISNULL(Groupname, '') <> ''
                GROUP BY Groupname
                ORDER BY SUM(Amount) DESC
                """;
            warning =
                $"Governed sales by group on vw_Sales_EBIDTA for {company} FY {fyLabel}, excl. intercompany.";
        }

        return true;
    }

    private static bool TryBuildPurchaseTotalsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikePurchaseTotalsQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
        var startLit = fyStart.ToString("yyyy-MM-dd");
        var endLit = fyEndExclusive.AddDays(-1).ToString("yyyy-MM-dd");
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                ROUND(SUM(Amount), 0) AS TotalPurchase,
                ROUND(SUM(netwt), 0) AS TotalQuantity
            FROM vw_Purchase_EBIDTA
            WHERE CompanyName = '{companyLit}'
              AND invdate >= '{startLit}'
              AND invdate <= '{endLit}'
              AND InterGroup <> 'Intergroup'
            """;

        warning =
            $"Governed total purchase on vw_Purchase_EBIDTA for {company} FY {fyLabel} ({startLit} to {endLit}), excl. intercompany.";
        return true;
    }

    private static bool TryBuildLedgerCountSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeLedgerCountQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var underGroup = TryExtractLedgerUnderGroup(message);
        var underFilter = string.IsNullOrWhiteSpace(underGroup)
            ? ""
            : $"  AND Under LIKE '%{EscapeSqlLiteral(underGroup)}%'\n";

        sql = $"""
            SELECT COUNT(*) AS LedgerCount
            FROM LedgerMaster
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
              AND ISNULL(LedgerName, '') <> ''
            {underFilter}
            """;
        warning = string.IsNullOrWhiteSpace(underGroup)
            ? $"Governed ledger count on LedgerMaster for {company}."
            : $"Governed ledger count on LedgerMaster for {company} under group '{underGroup}' (LedgerMaster.Under).";
        return true;
    }

    /// <summary>Extract parent ledger group from "under Sundry Debtors for company" phrasing.</summary>
    private static string? TryExtractLedgerUnderGroup(string message)
    {
        string[] patterns =
        [
            @"\bunder\s+(.+?)\s+for\s+",
            @"\bunder\s+(.+?)\s+at\s+",
            @"\bunder\s+(?:the\s+)?(.+?)(?:\?|$|\.)",
            @"\bin\s+(?:the\s+)?(.+?)\s+(?:ledger\s+)?group\b",
            @"\b(?:ledger|account)s?\s+(?:in|under)\s+(.+?)\s+for\s+"
        ];

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) continue;

            var frag = m.Groups[1].Value.Trim().TrimEnd('?', '.', ',', ';');
            if (frag.Length < 3) continue;
            if (ResolveOutwardCompanyAlias(frag) is not null) continue;
            if (CanonicalizeCompanyName(frag) is not null
                && (frag.Contains("Limited", StringComparison.OrdinalIgnoreCase)
                    || frag.Contains("Ltd", StringComparison.OrdinalIgnoreCase)))
                continue;
            return frag;
        }

        return null;
    }

    private static bool LedgerCountSqlMissingUnderFilter(string message, string sql)
    {
        if (TryExtractLedgerUnderGroup(message) is null) return false;
        return !sql.Contains("Under", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBuildLedgerGroupsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeLedgerGroupQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);

        if (string.IsNullOrWhiteSpace(company))
        {
            sql = """
                SELECT DISTINCT TOP 50 Under AS GroupName
                FROM LedgerMaster
                WHERE ISNULL(Under, '') <> ''
                ORDER BY Under
                """;
            warning = "Governed ledger/account groups: DISTINCT LedgerMaster.Under (all companies).";
        }
        else
        {
            sql = $"""
                SELECT DISTINCT TOP 50 Under AS GroupName
                FROM LedgerMaster
                WHERE CompanyName = '{EscapeSqlLiteral(company)}'
                  AND ISNULL(Under, '') <> ''
                ORDER BY Under
                """;
            warning = $"Governed ledger/account groups: DISTINCT LedgerMaster.Under for {company}.";
        }

        return true;
    }

    private static bool TryBuildStockInHandSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeStockInHandQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var itemCode = TryExtractStockItemCode(message);
        var itemNameFrag = TryExtractStockItemNameFragment(message);
        var materialFrags = TryExtractStockMaterialFragments(message);
        if (string.IsNullOrWhiteSpace(itemCode)
            && string.IsNullOrWhiteSpace(itemNameFrag)
            && materialFrags.Count == 0)
            return false;

        var filters = new List<string> { $"CompanyName = '{EscapeSqlLiteral(company)}'" };

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var code = EscapeSqlLiteral(itemCode);
            filters.Add($"(ItemCode = '{code}' OR ItemCode LIKE '{code}%')");
        }
        else if (materialFrags.Count > 1)
        {
            filters.Add(BuildMultiMaterialStockFilter(materialFrags));
        }
        else if (materialFrags.Count == 1)
        {
            filters.Add(BuildStockItemNameLikeFilter(materialFrags[0]));
        }
        else if (!string.IsNullOrWhiteSpace(itemNameFrag))
        {
            filters.Add(BuildStockItemNameLikeFilter(itemNameFrag));
        }

        if (TryExtractStockWarehouseFragment(message) is { } whFrag)
            filters.Add($"(Warehousename LIKE '%{EscapeSqlLiteral(whFrag)}%' OR WareHouseName LIKE '%{EscapeSqlLiteral(whFrag)}%')");

        var where = string.Join(" AND ", filters);

        sql = $"""
            SELECT TOP 50
                Warehousename,
                ItemCode,
                ItemName,
                StkInHand,
                CompanyName
            FROM vw_itemwiseStock
            WHERE {where}
            ORDER BY StkInHand DESC
            """;
        warning = !string.IsNullOrWhiteSpace(itemCode)
            ? $"Governed stock-in-hand on vw_itemwiseStock for {company} item {itemCode}."
            : materialFrags.Count > 1
                ? $"Governed multi-material inventory on vw_itemwiseStock for {company} ({materialFrags.Count} materials, OR ItemName LIKE)."
                : $"Governed stock-in-hand on vw_itemwiseStock for {company} with ItemName LIKE filters (not exact match).";
        return true;
    }

    private static bool LooksLikeStockInHandQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (LooksLikeAboveMaxStockQuestion(message)) return false;
        if (m.Contains("reorder") || m.Contains("below minimum") || m.Contains("below min")) return false;
        if (m.Contains("list warehouse") || m.Contains("list of warehouse") || m.Contains("list warehouses")
            || m.Contains("list godown") || m.Contains("godown list")) return false;
        if (m.Contains("top stock") && m.Contains("by stock")) return false;
        if (m.Contains("stock by group") || m.Contains("by group")) return false;
        if (m.Contains("issue slip") || m.Contains("material issued")) return false;

        var hasStockIntent = m.Contains("stock in hand")
                             || m.Contains("current stock")
                             || m.Contains("stock of")
                             || m.Contains("stock for")
                             || (m.Contains("stock") && (m.Contains("how much") || m.Contains("what is")))
                             || (m.Contains("inventory") && (m.Contains("how much") || m.Contains("what is")))
                             || Regex.IsMatch(m, @"\b[\w/]+(?:/[\w]+)+\s+stock\b");

        if (!hasStockIntent) return false;

        return TryExtractStockItemCode(message) is not null
               || TryExtractStockItemNameFragment(message) is not null
               || TryExtractStockMaterialFragments(message).Count > 0;
    }

    private static string? TryExtractStockItemCode(string message)
    {
        var m = Regex.Match(message, @"\b([A-Z]{2,6}\d{3,10})\b");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? TryExtractStockItemNameFragment(string message)
    {
        string[] patterns =
        [
            @"\b(?:current\s+)?stock\s+of\s+(.+?)\s+(?:at|for|in)\s+",
            @"\bstock\s+in\s+hand\s+for\s+(?:item\s+)?(.+?)\s+(?:at|for|in|by)\s+",
            @"\bhow\s+much\s+stock\s+(?:of\s+)?(.+?)\s+(?:at|for|in)\s+",
            @"\bstock\s+for\s+(.+?)\s+(?:at|in)\s+",
            @"\bstock\s+of\s+(.+?)\s*\??\s*$",
            @"\binventory\s+of\s+(.+?)\s+(?:at|for|in)\s+",
            @"\binventory\s+of\s+(.+?)\s*\??\s*$"
        ];

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) continue;

            var frag = m.Groups[1].Value.Trim().TrimEnd('.', ',', ';', '?', '!');
            if (frag.Length < 2) continue;
            if (Regex.IsMatch(frag, @"^[A-Z]{2,6}\d+$")) continue;
            if (ResolveOutwardCompanyAlias(frag) is not null) continue;
            if (frag.Contains("Limited", StringComparison.OrdinalIgnoreCase)
                || frag.Contains("Ltd", StringComparison.OrdinalIgnoreCase))
                continue;
            return frag;
        }

        return null;
    }

    private static string? TryExtractStockWarehouseFragment(string message)
    {
        var m = Regex.Match(
            message,
            @"\bin\s+(?:the\s+)?(.+?)\s+(?:godown|warehouse)\b",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var frag = m.Groups[1].Value.Trim();
            if (frag.Length >= 3) return frag;
        }

        m = Regex.Match(
            message,
            @"\b(?:godown|warehouse)\s+(?:named\s+)?(.+?)(?:\?|$|\s+for\s+)",
            RegexOptions.IgnoreCase);
        return m.Success && m.Groups[1].Value.Trim().Length >= 3
            ? m.Groups[1].Value.Trim().TrimEnd('.', '?')
            : null;
    }

    private static string BuildStockItemNameLikeFilter(string itemFragment)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "for", "and", "item", "stock", "current", "at", "in", "of", "a", "an"
        };

        var words = Regex.Split(itemFragment.Trim(), @"\s+")
            .Select(w => w.Trim().TrimEnd('.', ',', ';', '?', '!'))
            .Where(w => w.Length >= 2 && !stop.Contains(w))
            .Select(NormalizeStockItemWordForLike)
            .Where(w => w.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (words.Count == 0)
            return $"ItemName LIKE '%{EscapeSqlLiteral(itemFragment.Trim())}%'";

        return string.Join(" AND ", words.Select(w => $"ItemName LIKE '%{EscapeSqlLiteral(w)}%'"));
    }

    private static string NormalizeStockItemWordForLike(string word)
    {
        var w = word.ToLowerInvariant();
        if (w.StartsWith("granul")) return "granul";
        if (w.StartsWith("polymer")) return "polym";
        if (w.EndsWith('s') && w.Length > 4) w = w[..^1];
        return w;
    }

    private static bool IsGovernedStockInHandSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var hasStockTable = sql.Contains("vw_itemwiseStock", StringComparison.OrdinalIgnoreCase)
                            || (sql.Contains("WareHouse", StringComparison.OrdinalIgnoreCase)
                                && sql.Contains("StkInHand", StringComparison.OrdinalIgnoreCase));
        if (!hasStockTable) return false;
        if (Regex.IsMatch(sql, @"\bItemCode\s*=", RegexOptions.IgnoreCase)) return true;
        return sql.Contains("LIKE", StringComparison.OrdinalIgnoreCase)
               && sql.Contains("ItemName", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldForceStockInHandGovernedRewrite(string sql, int rowCount)
    {
        if (IsGovernedStockInHandSql(sql) && rowCount > 0) return false;
        if (rowCount == 0) return true;
        if (Regex.IsMatch(sql, @"\bItemName\s*=\s*'", RegexOptions.IgnoreCase)) return true;
        if (sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("vw_itemwiseStock", StringComparison.OrdinalIgnoreCase))
            return true;
        return !IsGovernedStockInHandSql(sql);
    }

    private static bool LooksLikePluralItemsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (Regex.IsMatch(m, @"\b(items|materials|products|list\s+all|show\s+all|which\s+items)\b"))
            return true;
        return Regex.IsMatch(m, @"\b(list|show|display)\s+(?:the\s+)?(?:all\s+)?items\b");
    }

    private static bool LooksLikeSingularItemQuestion(string message)
    {
        if (TryExtractStockItemCode(message) is not null) return true;
        var m = message.ToLowerInvariant();
        if (Regex.IsMatch(m, @"\bfor\s+item\s+[A-Z]{2,6}\d", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(m, @"\b(latest|most recent|last|recent)\s+item\b"))
            return true;
        if (Regex.IsMatch(m, @"\bwhat\s+item\b|\bwhich\s+item\b") && !LooksLikePluralItemsQuestion(message))
            return true;
        return false;
    }

    private static bool LooksLikeDailyInwardOutwardQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (Regex.IsMatch(m, @"\bissue\s+slip\b")) return false;
        if (m.Contains("issued to") || m.Contains("issue to")) return false;
        if (m.Contains("monthly")) return false;
        if (TryParseMonthYear(message) is not null && !m.Contains("today")) return false;

        var hasInward = m.Contains("inward");
        var hasOutward = m.Contains("outward") || m.Contains("stock issued");
        var hasBoth = hasInward && hasOutward;
        var hasQty = m.Contains("qty") || m.Contains("quantity") || m.Contains("quantities");
        var hasToday = m.Contains("today") || m.Contains("daily");
        var hasMovementPhrase = m.Contains("stock movement") || m.Contains("inward outward");

        if (hasBoth) return true;
        if (hasMovementPhrase) return true;
        if (hasToday && (hasInward || hasOutward)) return true;
        if ((hasInward || hasOutward) && hasQty && LooksLikePluralItemsQuestion(message)) return true;
        return (hasInward || hasOutward) && hasQty && m.Contains("today");
    }

    private static bool ShouldRewriteToDailyInwardOutward(string sql, string message)
    {
        if (!LooksLikeDailyInwardOutwardQuestion(message)) return false;
        return Regex.IsMatch(sql, @"SELECT\s+TOP\s+1\b", RegexOptions.IgnoreCase)
               && sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Day-bucket ageing for one party: 0-30 / 31-60 / 61-90 / 90+ on vw_BillWiseTransaction.
    /// </summary>
    private static bool TryBuildPartyAgeingBucketsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (LooksLikeInactiveCustomersQuestion(message))
            return false;
        if (!LooksLikeDayBucketAgeing(message) || !LooksLikeAgeingQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var party = ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(party))
            return false;

        var asOn = TryParseAsOnDate(message) ?? DateTime.Today;
        var partyLike = EscapeSqlLiteral(party.Trim());
        var companyLit = EscapeSqlLiteral(company);
        var asOnLit = asOn.ToString("yyyy-MM-dd");

        ResolveAgeingGroups(message, out _, out _);
        var underFilter = message.Contains("creditor", StringComparison.OrdinalIgnoreCase)
                          || message.Contains("vendor", StringComparison.OrdinalIgnoreCase)
                          || message.Contains("supplier", StringComparison.OrdinalIgnoreCase)
            ? "Creditors%"
            : "Debtors%";

        sql = $"""
            SELECT TOP 1
                CompanyName,
                LedgerName,
                Under,
                SUM(CASE WHEN AgeDays BETWEEN 0 AND 30 THEN ABS(Amount) ELSE 0 END) AS Bucket_0_30,
                SUM(CASE WHEN AgeDays BETWEEN 31 AND 60 THEN ABS(Amount) ELSE 0 END) AS Bucket_31_60,
                SUM(CASE WHEN AgeDays BETWEEN 61 AND 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_61_90,
                SUM(CASE WHEN AgeDays > 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_90_Plus,
                SUM(ABS(Amount)) AS TotalOutstanding
            FROM (
                SELECT lm.CompanyName, lm.LedgerName, lm.Under,
                       DATEDIFF(day, ISNULL(b.BillDate, b.VoucherDate), CAST('{asOnLit}' AS date)) AS AgeDays,
                       b.Amount
                FROM LedgerMaster lm WITH (NOLOCK)
                INNER JOIN vw_BillWiseTransaction b WITH (NOLOCK)
                    ON b.CompanyName = lm.CompanyName AND b.LedgerName = lm.LedgerName
                WHERE lm.CompanyName = '{companyLit}'
                  AND lm.LedgerName LIKE '%{partyLike}%'
                  AND lm.Under LIKE '{underFilter}'
            ) x
            GROUP BY CompanyName, LedgerName, Under
            """;

        warning =
            $"Governed day-bucket ageing: vw_BillWiseTransaction bill-age buckets for {party} at {company} as on {asOnLit} (BillDate, VoucherDate fallback).";
        return true;
    }

    /// <summary>
    /// Day-bucket ageing list for debtor/creditor group (TOP 50 parties by total outstanding).
    /// </summary>
    private static bool TryBuildDebtorCreditorAgeingListSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (LooksLikeInactiveCustomersQuestion(message))
            return false;
        if (!LooksLikeDayBucketAgeing(message) || !LooksLikeAgeingQuestion(message))
            return false;

        if (ResolveLedgerPartyForChat(message) is not null)
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var asOn = TryParseAsOnDate(message) ?? DateTime.Today;
        var companyLit = EscapeSqlLiteral(company);
        var asOnLit = asOn.ToString("yyyy-MM-dd");

        var underFilter = message.Contains("creditor", StringComparison.OrdinalIgnoreCase)
                          || message.Contains("vendor", StringComparison.OrdinalIgnoreCase)
                          || message.Contains("supplier", StringComparison.OrdinalIgnoreCase)
            ? "Creditors%"
            : "Debtors%";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName,
                LedgerName,
                Under,
                SUM(CASE WHEN AgeDays BETWEEN 0 AND 30 THEN ABS(Amount) ELSE 0 END) AS Bucket_0_30,
                SUM(CASE WHEN AgeDays BETWEEN 31 AND 60 THEN ABS(Amount) ELSE 0 END) AS Bucket_31_60,
                SUM(CASE WHEN AgeDays BETWEEN 61 AND 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_61_90,
                SUM(CASE WHEN AgeDays > 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_90_Plus,
                SUM(ABS(Amount)) AS TotalOutstanding
            FROM (
                SELECT lm.CompanyName, lm.LedgerName, lm.Under,
                       DATEDIFF(day, ISNULL(b.BillDate, b.VoucherDate), CAST('{asOnLit}' AS date)) AS AgeDays,
                       b.Amount
                FROM LedgerMaster lm WITH (NOLOCK)
                INNER JOIN vw_BillWiseTransaction b WITH (NOLOCK)
                    ON b.CompanyName = lm.CompanyName AND b.LedgerName = lm.LedgerName
                WHERE lm.CompanyName = '{companyLit}'
                  AND lm.Under LIKE '{underFilter}'
            ) x
            GROUP BY CompanyName, LedgerName, Under
            ORDER BY TotalOutstanding DESC
            """;

        warning =
            $"Governed day-bucket ageing list: TOP {MaxReturnRows} parties under {underFilter.TrimEnd('%')}% at {company} as on {asOnLit} (vw_BillWiseTransaction).";
        return true;
    }
}
