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
        if (TryBuildPoQueueCompareSql(message, out sql, out warning)) return true;
        if (TryBuildRejectedPoSql(message, out sql, out warning)) return true;
        if (TryBuildRejectedBillPaymentSql(message, out sql, out warning)) return true;
        if (TryBuildApprovedPaymentTotalSql(message, out sql, out warning)) return true;
        if (TryBuildPoHeaderSql(message, out sql, out warning)) return true;
        if (TryBuildHighValuePoSql(message, out sql, out warning)) return true;
        if (TryBuildSalesInvoiceTaxSql(message, out sql, out warning)) return true;
        if (TryBuildAboveMaxStockSql(message, out sql, out warning)) return true;
        if (TryBuildStoreIssueByDeptSql(message, out sql, out warning)) return true;
        if (TryBuildInterUnitSalesSql(message, out sql, out warning)) return true;
        if (TryBuildRollsWaitingDespatchSql(message, out sql, out warning)) return true;
        if (TryBuildWebbingProductionSql(message, out sql, out warning)) return true;
        if (TryBuildLoomProductionByQualitySql(message, out sql, out warning)) return true;
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
        if (!m.Contains("po") && !m.Contains("purchase order")) return false;

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
        if (!m.Contains("po") && !m.Contains("purchase order")) return false;

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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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
        if (!m.Contains("po") && !m.Contains("purchase order")) return false;
        if (!(m.Contains("currency") || m.Contains("delivery") || m.Contains("payment term"))) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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
        if (!m.Contains("purchase order") && !Regex.IsMatch(m, @"\bpos?\b")) return false;
        if (!(m.Contains("high-value") || m.Contains("high value")
              || m.Contains("descending") || m.Contains("largest") || m.Contains("high-value")))
            return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

    private static bool TryBuildSalesInvoiceTaxSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("tax") && !m.Contains("gst")) return false;
        if (!m.Contains("invoice") && !m.Contains("sales")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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

    private static bool TryBuildLoomProductionByQualitySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("loom")) return false;
        if (!m.Contains("production")) return false;
        if (!m.Contains("quality") && !m.Contains("group")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP 50
                Quality,
                SUM(ISNULL(NetWt, 0)) AS TotalNetWt,
                SUM(ISNULL(Metre, 0)) AS TotalMetre,
                COUNT(*) AS RollCount
            FROM vw_LoomProductionENtry
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
            GROUP BY Quality
            ORDER BY SUM(ISNULL(NetWt, 0)) DESC
            """;
        warning = $"Governed loom production grouped by Quality on vw_LoomProductionENtry for {company}.";
        return true;
    }

    private static bool TryBuildItemFromRecentOutwardSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("item") && !m.Contains("hsn") && !m.Contains("description")) return false;
        if (!m.Contains("outward") && !m.Contains("issued") && !m.Contains("store")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "")
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
        if (!m.Contains("allocation") && !m.Contains("authority") && !m.Contains("limit"))
            return false;
        if (!m.Contains("po") && !m.Contains("purchase order")) return false;

        var user = TryExtractPersonName(message, "username", "user", "for");
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
        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
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
}
