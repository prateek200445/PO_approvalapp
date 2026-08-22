using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool TryBuildExtendedGovernanceSql(string message, out string sql, out string warning)
    {
        if (TryBuildReliancePaymentSql(message, out sql, out warning)) return true;
        if (TryBuildImportDocumentSql(message, out sql, out warning)) return true;
        if (TryBuildImportPaymentTrackingSql(message, out sql, out warning)) return true;
        if (TryBuildImportPoPendingReceiptSql(message, out sql, out warning)) return true;
        if (TryBuildAdvanceLicenseExpirySql(message, out sql, out warning)) return true;
        if (TryBuildRepresentativeWiseSalesSql(message, out sql, out warning)) return true;
        if (TryBuildTopExpenseLedgersSql(message, out sql, out warning)) return true;
        if (TryBuildAllExpensesMonthlySql(message, out sql, out warning)) return true;
        if (TryBuildExpenseMonthlySql(message, out sql, out warning)) return true;
        if (TryBuildExceptionalExpenseSql(message, out sql, out warning)) return true;
        if (TryBuildSalesPerKgSql(message, out sql, out warning)) return true;
        if (TryBuildItemWiseProductionSql(message, out sql, out warning)) return true;
        if (TryBuildOwnVsTradedSalesSql(message, out sql, out warning)) return true;
        if (TryBuildMostPurchasedItemsSql(message, out sql, out warning)) return true;
        if (TryBuildMostConsumedItemsSql(message, out sql, out warning)) return true;
        if (TryBuildMisCategorySalesQtySql(message, out sql, out warning)) return true;
        if (TryBuildTopCustomersByMisCategorySql(message, out sql, out warning)) return true;
        if (TryBuildInactiveCustomersSql(message, out sql, out warning)) return true;
        if (TryBuildCustomerSalesCurrencySql(message, out sql, out warning)) return true;
        if (TryBuildPurchasePerKgSql(message, out sql, out warning)) return true;
        if (TryBuildPaymentPlanningSql(message, out sql, out warning)) return true;
        if (TryBuildImportTrackingSql(message, out sql, out warning)) return true;
        if (TryBuildAdvanceLicensePendingSql(message, out sql, out warning)) return true;
        if (TryBuildStoresInventoryValueSql(message, out sql, out warning)) return true;
        if (TryBuildStoreConsumptionSql(message, out sql, out warning)) return true;
        if (TryBuildLedgerAnomalyBalancesSql(message, out sql, out warning)) return true;
        if (TryBuildExpenseLedgerLookupSql(message, out sql, out warning)) return true;
        if (TryBuildPeriodWastageSql(message, out sql, out warning)) return true;

        sql = "";
        warning = "";
        return false;
    }

    private static bool TryBuildOwnVsTradedSalesSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!((m.Contains("own") && m.Contains("production")) || m.Contains("traded goods") || m.Contains("traded good")))
            return false;
        if (!m.Contains("sales") && !m.Contains("sold") && !m.Contains("divide") && !m.Contains("split"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                {TradedGoodsSqlCase} AS SalesType,
                ROUND(SUM(Amount), 2) AS SalesAmount,
                ROUND(SUM(netwt), 2) AS QuantityKg,
                CASE WHEN SUM(netwt) > 0 THEN ROUND(SUM(Amount) / SUM(netwt), 2) END AS PerKg
            FROM vw_Sales_EBIDTA WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND invdate >= '{start:yyyy-MM-dd}'
              AND invdate <= '{end:yyyy-MM-dd}'
              AND InterGroup <> 'Intergroup'
            GROUP BY {TradedGoodsSqlCase}
            ORDER BY SalesAmount DESC
            """;
        warning =
            $"Governed own-production vs traded-goods sales for {company} ({periodLabel}) on vw_Sales_EBIDTA. Traded = FG/Trading Items or Traded/Trading group names (ERP-validated).";
        return true;
    }

    private static bool TryBuildMisCategorySalesQtySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (m.Contains("purchas") || m.Contains("bought") || m.Contains("inward") || m.Contains("mrn"))
            return false;

        if (!TryParseMisCategoryIntent(message, out var categories, out var categoryLabel))
            return false;

        if (!m.Contains("qty") && !m.Contains("quantity") && !m.Contains("sold") && !m.Contains("sales"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var catFilter = string.Join(", ", categories.Select(c => $"'{EscapeSqlLiteral(c)}'"));
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                MISCATEGORY,
                ROUND(SUM(qTY), 2) AS TotalQty,
                COUNT(DISTINCT InvNo) AS InvoiceCount
            FROM VW_SALES_EBD_DTL WITH (NOLOCK)
            WHERE companyname = '{companyLit}'
              AND sysdate >= '{start:yyyy-MM-dd}'
              AND sysdate <= '{end:yyyy-MM-dd}'
              AND MISCATEGORY IN ({catFilter})
            GROUP BY MISCATEGORY
            ORDER BY TotalQty DESC
            """;
        warning =
            $"Governed {categoryLabel} sales qty on VW_SALES_EBD_DTL for {company} ({periodLabel}). MISCATEGORY RM/SF/FG.";
        return true;
    }

    private static bool TryBuildTopCustomersByMisCategorySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("top") && !m.Contains("highest") && !m.Contains("largest") && !m.Contains("biggest"))
            return false;
        if (!m.Contains("customer") && !m.Contains("buyer") && !m.Contains("client"))
            return false;
        if (!TryParseMisCategoryIntent(message, out var categories, out var categoryLabel))
        {
            if (!m.Contains("sf") && !m.Contains("fg") && !m.Contains("semi") && !m.Contains("finished"))
                return false;
            categories = ["SF", "FG"];
            categoryLabel = "SF/FG";
        }

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var topN = ParseTopN(message, 5);
        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var catFilter = string.Join(", ", categories.Select(c => $"'{EscapeSqlLiteral(c)}'"));
        var companyLit = EscapeSqlLiteral(company);

        var wantsQty = m.Contains("qty") || m.Contains("quantity");
        var orderCol = wantsQty ? "TotalQty" : "TotalAmount";

        sql = $"""
            SELECT TOP {topN}
                sv.BuyerName AS Customer,
                e.MISCATEGORY,
                ROUND(SUM(e.qTY), 2) AS TotalQty,
                ROUND(SUM(ISNULL(svi.Amount, 0)), 2) AS TotalAmount,
                COUNT(DISTINCT e.InvNo) AS InvoiceCount
            FROM VW_SALES_EBD_DTL e WITH (NOLOCK)
            INNER JOIN vw_Salesvoucher sv WITH (NOLOCK)
                ON e.companyname = sv.CompanyName AND e.InvNo = sv.InvNo
            LEFT JOIN SalesVoucherItem svi WITH (NOLOCK)
                ON sv.CompanyName = svi.CompanyName AND sv.InvNo = svi.InvNo
            WHERE e.companyname = '{companyLit}'
              AND e.sysdate >= '{start:yyyy-MM-dd}'
              AND e.sysdate <= '{end:yyyy-MM-dd}'
              AND e.MISCATEGORY IN ({catFilter})
            GROUP BY sv.BuyerName, e.MISCATEGORY
            ORDER BY {orderCol} DESC
            """;
        warning =
            $"Governed top {topN} customers by {categoryLabel} ({orderCol}) for {company} ({periodLabel}).";
        return true;
    }

    private static bool LooksLikeInactiveCustomersQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("customer") && !m.Contains("buyer") && !m.Contains("client"))
            return false;
        if (!m.Contains("order") && !m.Contains("invoice") && !m.Contains("sales"))
            return false;
        return m.Contains("since") || m.Contains("no order") || m.Contains("not got") || m.Contains("inactive");
    }

    private static bool TryBuildInactiveCustomersSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeInactiveCustomersQuestion(message))
            return false;

        var days = TryParseInactiveDays(message) ?? 90;
        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT TOP {MaxReturnRows}
                BuyerName AS Customer,
                MAX(InvDate) AS LastInvoiceDate,
                MAX(BuyerOrderNo) AS LastBuyerOrderNo,
                DATEDIFF(day, MAX(InvDate), CAST(GETDATE() AS date)) AS DaysSinceLastInvoice
            FROM vw_Salesvoucher WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND ISNULL(BuyerName, '') <> ''
              AND InvType NOT LIKE '%InterUnit%'
            GROUP BY BuyerName
            HAVING DATEDIFF(day, MAX(InvDate), CAST(GETDATE() AS date)) >= {days}
            ORDER BY LastInvoiceDate
            """;
        warning =
            $"Governed customers with no invoice in last {days}+ days for {company} (vw_Salesvoucher; proxy for 'no order').";
        return true;
    }

    private static bool LooksLikeCustomerSalesCurrencyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("sales") && !m.Contains("sold"))
            return false;
        if (!m.Contains("fc") && !m.Contains("inr") && !m.Contains("foreign") && !m.Contains("currency"))
            return false;

        return m.Contains("customer") || m.Contains("buyer") || m.Contains("client")
               || Regex.IsMatch(m, @"\b(?:total\s+)?sales\s+of\b")
               || !string.IsNullOrWhiteSpace(TryExtractCustomerForSalesQuery(message))
               || !string.IsNullOrWhiteSpace(ResolveLedgerPartyForChat(message));
    }

    private static bool WantsForeignCurrencySalesOnly(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("foreign") && m.Contains("currency"))
               || Regex.IsMatch(m, @"\bin\s+fc\b")
               || (Regex.IsMatch(m, @"\bfc\b") && !m.Contains("inr"));
    }

    private static string? TryExtractCustomerForSalesQuery(string message)
    {
        string? Clean(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim().TrimEnd('.', ',', ';', '?', '!');
            s = Regex.Replace(s, @"\s+in\s+(?:foreign\s+)?(?:currency|fc|inr).*$", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s+(?:at|for)\s+(?:the\s+)?[A-Za-z].*(?:Limited|Ltd|Pvt|Private).*$", "", RegexOptions.IgnoreCase);
            return s.Length >= 3 ? s : null;
        }

        foreach (var pattern in new[]
                 {
                     @"(?:total\s+)?sales\s+of\s+(?:the\s+)?(.+?)(?:\s+in\s+(?:foreign\s+)?(?:currency|fc|inr)|\s+at\s+|\s+for\s+|\s+fy|\?|$)",
                     @"(?:customer|buyer|client)\s+(.+?)(?:\s+in\s+|\s+at\s+|\s+for\s+|\?|$)"
                 })
        {
            var m = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            var p = Clean(m.Groups[1].Value);
            if (p is not null && !ChatEntityResolutionService.IsGarbagePartyHint(p))
                return FinalizeLedgerPartyName(p, message);
        }

        return null;
    }

    private static bool TryBuildCustomerSalesCurrencySql(string message, out string sql, out string warning)
        => BuildCustomerSalesCurrencySql(message, relaxForeignCurrency: false, relaxBuyerMatch: false, out sql, out warning);

    private static bool TryBuildCustomerSalesCurrencyFallbackSql(string message, out string sql, out string warning)
        => BuildCustomerSalesCurrencySql(message, relaxForeignCurrency: true, relaxBuyerMatch: true, out sql, out warning);

    private static string BuildBuyerNameLikeFilter(string party)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LLC", "L.L.C.", "Ltd", "Limited", "Pvt", "Private", "Inc", "Corp", "Corporation", "Company", "Co"
        };
        var tokens = party.Split([' ', ',', '.', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length >= 3 && !stop.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (tokens.Count == 0)
            return $"BuyerName LIKE '%{EscapeSqlLiteral(party.Trim())}%'";

        return string.Join(" AND ", tokens.Select(t => $"BuyerName LIKE '%{EscapeSqlLiteral(t)}%'"));
    }

    private static string BuildForeignCurrencySalesFilter(bool relaxed)
    {
        if (relaxed)
            return "1=1";

        return """
            (
                ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') NOT IN ('INR', 'Rs.', 'RS', 'Rs')
                OR InvType LIKE '%Export%'
                OR ISNULL(ExchangeRate, 0) > 1.0001
            )
            """;
    }

    private static bool BuildCustomerSalesCurrencySql(
        string message,
        bool relaxForeignCurrency,
        bool relaxBuyerMatch,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeCustomerSalesCurrencyQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        var party = TryExtractCustomerForSalesQuery(message) ?? ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(party))
            return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var buyerFilter = relaxBuyerMatch
            ? $"BuyerName LIKE '%{EscapeSqlLiteral(party.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? party)}%'"
            : BuildBuyerNameLikeFilter(party);

        var filters = new List<string>
        {
            $"CompanyName = '{EscapeSqlLiteral(company)}'",
            buyerFilter,
            $"InvDate >= '{start:yyyy-MM-dd}' AND InvDate <= '{end:yyyy-MM-dd}'",
            "InvType NOT LIKE '%InterUnit%'",
        };

        if (WantsForeignCurrencySalesOnly(message))
            filters.Add(BuildForeignCurrencySalesFilter(relaxForeignCurrency));
        else if (message.Contains("inr", StringComparison.OrdinalIgnoreCase))
            filters.Add("ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') IN ('INR', 'Rs.', 'RS', 'Rs')");

        sql = $"""
            SELECT
                BuyerName AS Customer,
                ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') AS Currency,
                ROUND(SUM(BillAMount), 2) AS TotalSalesInr,
                ROUND(SUM(
                    CASE
                        WHEN ISNULL(ExchangeRate, 0) > 1.0001 THEN BillAMount / NULLIF(ExchangeRate, 0)
                        WHEN ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') NOT IN ('INR', 'Rs.', 'RS', 'Rs')
                            THEN BillAMount
                        ELSE 0
                    END), 2) AS TotalSalesFc,
                COUNT(*) AS InvoiceCount,
                ROUND(AVG(NULLIF(ExchangeRate, 0)), 4) AS AvgExchangeRate
            FROM vw_Salesvoucher WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            GROUP BY BuyerName, ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR')
            ORDER BY TotalSalesFc DESC, TotalSalesInr DESC
            """;

        warning = relaxForeignCurrency || relaxBuyerMatch
            ? $"Governed customer sales by currency for {party} at {company} ({periodLabel}) — relaxed buyer/FC match on vw_Salesvoucher."
            : $"Governed customer sales by currency (FC/INR) for {party} at {company} ({periodLabel}) on vw_Salesvoucher.";
        return true;
    }

    private static bool TryBuildCustomerSalesCrossCompanySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeCustomerSalesCurrencyQuestion(message))
            return false;

        var party = TryExtractCustomerForSalesQuery(message) ?? ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(party))
            return false;

        var company = ResolveCompanyForChat(message);
        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var filters = new List<string>
        {
            BuildBuyerNameLikeFilter(party),
            $"InvDate >= '{start:yyyy-MM-dd}' AND InvDate <= '{end:yyyy-MM-dd}'",
            "InvType NOT LIKE '%InterUnit%'",
        };

        if (WantsForeignCurrencySalesOnly(message))
            filters.Add(BuildForeignCurrencySalesFilter(relaxed: false));

        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName <> '{EscapeSqlLiteral(company)}'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName,
                BuyerName AS Customer,
                ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') AS Currency,
                InvType,
                COUNT(*) AS InvoiceCount,
                ROUND(SUM(BillAMount), 2) AS TotalSalesInr,
                ROUND(SUM(
                    CASE
                        WHEN ISNULL(ExchangeRate, 0) > 1.0001 THEN BillAMount / NULLIF(ExchangeRate, 0)
                        WHEN ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') NOT IN ('INR', 'Rs.', 'RS', 'Rs')
                            THEN BillAMount
                        ELSE 0
                    END), 2) AS TotalSalesFc
            FROM vw_Salesvoucher WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            GROUP BY CompanyName, BuyerName, ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR'), InvType
            ORDER BY TotalSalesFc DESC, TotalSalesInr DESC
            """;

        var at = string.IsNullOrWhiteSpace(company) ? "the requested company" : company;
        warning =
            $"No sales for {party} at {at} in {periodLabel}; showing other companies where this customer has sales (vw_Salesvoucher).";
        return true;
    }

    private static bool TryBuildPurchasePerKgSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("purchase") && !m.Contains("procurement") && !m.Contains("bought"))
            return false;
        if (!m.Contains("per kg") && !m.Contains("perkg") && !m.Contains("/kg") && !m.Contains("rate per"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var wantsMonthly = m.Contains("monthly") || m.Contains("month wise") || m.Contains("by month");
        var companyLit = EscapeSqlLiteral(company);

        if (wantsMonthly)
        {
            sql = $"""
                SELECT
                    YEAR(invdate) AS PurchaseYear,
                    MONTH(invdate) AS PurchaseMonth,
                    LEFT(DATENAME(month, invdate), 3) AS MonthName,
                    ROUND(SUM(Amount), 2) AS PurchaseAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE WHEN SUM(netwt) > 0 THEN ROUND(SUM(Amount) / SUM(netwt), 2) END AS PerKg
                FROM vw_Purchase_EBIDTA WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{start:yyyy-MM-dd}'
                  AND invdate <= '{end:yyyy-MM-dd}'
                  AND InterGroup <> 'Intergroup'
                GROUP BY YEAR(invdate), MONTH(invdate), DATENAME(month, invdate)
                ORDER BY YEAR(invdate), MONTH(invdate)
                """;
            warning =
                $"Governed monthly purchase per kg for {company} ({periodLabel}) on vw_Purchase_EBIDTA.";
        }
        else
        {
            sql = $"""
                SELECT
                    ROUND(SUM(Amount), 2) AS PurchaseAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE WHEN SUM(netwt) > 0 THEN ROUND(SUM(Amount) / SUM(netwt), 2) END AS PerKg
                FROM vw_Purchase_EBIDTA WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{start:yyyy-MM-dd}'
                  AND invdate <= '{end:yyyy-MM-dd}'
                  AND InterGroup <> 'Intergroup'
                """;
            warning =
                $"Governed purchase per kg summary for {company} ({periodLabel}) on vw_Purchase_EBIDTA.";
        }

        return true;
    }

    private static bool TryBuildPaymentPlanningSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("payment plan") && !m.Contains("payment planning") && !m.Contains("current payment"))
            return false;

        var company = ResolveCompanyForChat(message);
        var companyFilter = string.IsNullOrWhiteSpace(company)
            ? ""
            : $"WHERE CompanyName = '{EscapeSqlLiteral(company)}'";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                PlanType, Detail, Amount, DueDate, CompanyName, PartyName
            FROM (
                SELECT
                    'LC / cashflow due' AS PlanType,
                    PaymentDetails AS Detail,
                    CAST(Amount AS decimal(18,2)) AS Amount,
                    LcDueDate AS DueDate,
                    CAST(NULL AS varchar(200)) AS CompanyName,
                    CAST(NULL AS varchar(200)) AS PartyName
                FROM Vw_DueDateCashFlow WITH (NOLOCK)
                UNION ALL
                SELECT
                    'Payment draft',
                    CONCAT(ISNULL(BillNo, ''), ' / ', ISNULL(Partyname, '')),
                    NULL,
                    NULL,
                    CompanyName,
                    Partyname
                FROM vw_BillPaymentReqDraft WITH (NOLOCK)
                {companyFilter}
            ) x
            ORDER BY DueDate, Amount DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed payment planning snapshot: LC due dates (Vw_DueDateCashFlow) + bill payment drafts (vw_BillPaymentReqDraft)."
            : $"Governed payment planning for {company} (LC due + payment drafts).";
        return true;
    }

    private static bool TryBuildImportTrackingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("import"))
            return false;
        if (!m.Contains("track") && !m.Contains("tracking") && !m.Contains("management")
            && !m.Contains("data") && !m.Contains("sheet") && !m.Contains("status"))
            return false;
        if (m.Contains("document upload") || m.Contains("reliance payment") || m.Contains("payment tracking"))
            return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(BuyerName LIKE '%{lit}%' OR BeneficiaryName LIKE '%{lit}%')");
        }

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                orderno, BuyerName, BeneficiaryName, OrderDate,
                poqty, MRNqty, pendingqty, AdvancelicNo
            FROM vw_ImportPurchasewithPOandMRNqty WITH (NOLOCK)
            {where}
            ORDER BY OrderDate DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed import PO/MRN tracking (vw_ImportPurchasewithPOandMRNqty). Document upload / Reliance sheets are outside SQL."
            : $"Governed import tracking for {company} (vw_ImportPurchasewithPOandMRNqty).";
        return true;
    }

    private static bool TryBuildAdvanceLicensePendingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("advance") && !m.Contains("advancelic") && !m.Contains("license") && !m.Contains("licence"))
            return false;
        if (!m.Contains("expir") && !m.Contains("pending") && !m.Contains("incomplete") && !m.Contains("not completed"))
            return false;
        if (m.Contains("expir"))
            return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>
        {
            "AdvancelicNo IS NOT NULL",
            "LTRIM(RTRIM(AdvancelicNo)) <> ''",
            "pendingqty > 0",
        };
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(BuyerName LIKE '%{lit}%' OR BeneficiaryName LIKE '%{lit}%')");
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                orderno, BuyerName, BeneficiaryName, OrderDate,
                poqty, MRNqty, pendingqty, AdvancelicNo
            FROM vw_ImportPurchasewithPOandMRNqty WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY OrderDate DESC
            """;
        warning =
            "Governed advance-license import lines with pending qty (vw_ImportPurchasewithPOandMRNqty). For expiry use advance license expiring query (vw_advancelicenecePOSI).";
        return true;
    }

    private static bool TryBuildReliancePaymentSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("reliance")) return false;
        if (!m.Contains("payment") && !m.Contains("sheet") && !m.Contains("invoice")
            && !m.Contains("updation") && !m.Contains("update"))
            return false;

        var company = ResolveCompanyForChat(message);
        var wantsDetails = m.Contains("detail") || m.Contains("updation") || m.Contains("line");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
        {
            foreach (var code in ResolveErpCompanyCodes(company))
                filters.Add($"companyname LIKE '%{EscapeSqlLiteral(code)}%'");
        }

        var where = filters.Count > 0 ? $"WHERE ({string.Join(" OR ", filters.Distinct())})" : "";
        var source = wantsDetails ? "vw_reliancepaymentdetails" : "vw_reliancepayment";
        var selectCols = wantsDetails
            ? """
                companyname, customername, Retailinvoice, Freightinvoice,
                amountpaybale, freightpayable, duedate, Paymentterm, bankname, accountnumber
                """
            : """
                companyname, customername, Retailinvoice, Freightinvoice,
                amountpaybale, freightpayable, duedate, Paymentterm, bankname, accountnumber
                """;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                {selectCols}
            FROM {source} WITH (NOLOCK)
            {where}
            ORDER BY duedate DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? $"Governed Reliance payment sheet ({source}; read-only list — Excel/DMS updation is outside SQL)."
            : $"Governed Reliance payment sheet for {company} ({source}; ERP company code filter; read-only).";
        return true;
    }

    private static bool TryBuildImportPaymentTrackingSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("import")) return false;
        if (!m.Contains("payment")) return false;
        if (m.Contains("reliance")) return false;
        if (m.Contains("document") || m.Contains("upload") || m.Contains("attach")) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "p.status = 'Pending'" };
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(p.BuyerName LIKE '%{lit}%' OR p.BeneficiaryName LIKE '%{lit}%')");
        }

        var whereClause = string.Join(" AND ", filters);

        sql = $"""
            SELECT TOP {MaxReturnRows} *
            FROM (
                SELECT
                    p.OrderNo,
                    p.OrderDate,
                    p.BuyerName,
                    p.BeneficiaryName,
                    p.TypeofPurchase,
                    p.status,
                    p.LC,
                    p.LCNo,
                    p.PayTerms,
                    ROUND(p.Amount, 2) AS Amount,
                    p.Currency,
                    ROUND(p.TotalAmount, 2) AS TotalAmount,
                    p.ETD,
                    p.ETA,
                    p.AdvanceLicNo AS POAdvanceLicNo,
                    m.AdvLicenseNos,
                    ROUND(m.POQty, 2) AS ImportPOQty,
                    ROUND(m.MRNQty, 2) AS MRNQty,
                    ROUND(m.PendingQty, 2) AS PendingQty,
                    lic.AdvanceLicNo AS LinkedAdvanceLicNo,
                    ROUND(lic.AmountINR, 2) AS AdvanceLicAmountINR,
                    al.IMPexpirtydate AS AdvanceLicExpiry,
                    ROUND(al.Licqty, 2) AS AdvanceLicQty
                FROM vw_PurchadeOrderImport p WITH (NOLOCK)
                OUTER APPLY (
                    SELECT
                        STRING_AGG(NULLIF(LTRIM(RTRIM(x.AdvancelicNo)), ''), ', ') AS AdvLicenseNos,
                        MIN(NULLIF(LTRIM(RTRIM(x.AdvancelicNo)), '')) AS FirstAdvLic,
                        SUM(ISNULL(x.poqty, 0)) AS POQty,
                        SUM(ISNULL(x.MRNqty, 0)) AS MRNQty,
                        SUM(ISNULL(x.pendingqty, 0)) AS PendingQty
                    FROM vw_ImportPurchasewithPOandMRNqty x WITH (NOLOCK)
                    WHERE x.orderno = p.OrderNo
                ) m
                OUTER APPLY (
                    SELECT TOP 1
                        l.AdvanceLicNo,
                        l.AmountINR
                    FROM vw_AdvanceLicImportPO l WITH (NOLOCK)
                    WHERE l.PoNo = p.OrderNo
                    ORDER BY l.orderdate DESC
                ) lic
                OUTER APPLY (
                    SELECT TOP 1
                        pos.IMPexpirtydate,
                        pos.Licqty
                    FROM vw_advancelicenecePOSI pos WITH (NOLOCK)
                    WHERE NULLIF(LTRIM(RTRIM(pos.AdvLicenseNo)), '') IN (
                        SELECT NULLIF(LTRIM(RTRIM(v)), '')
                        FROM (VALUES
                            (m.FirstAdvLic),
                            (lic.AdvanceLicNo),
                            (p.AdvanceLicNo)
                        ) AS nums(v)
                        WHERE NULLIF(LTRIM(RTRIM(v)), '') IS NOT NULL
                    )
                    ORDER BY pos.IMPexpirtydate DESC
                ) al
                WHERE {whereClause}
            ) ImportPaymentCombined
            ORDER BY OrderDate DESC
            """;
        warning =
            $"Governed import payment + advance-license tracking: vw_PurchadeOrderImport (PayTerms/Amount/LC) LEFT JOIN vw_ImportPurchasewithPOandMRNqty + vw_AdvanceLicImportPO + vw_advancelicenecePOSI on OrderNo. Chat shows TOP {MaxReturnRows}; Export CSV has full list.{(string.IsNullOrWhiteSpace(company) ? "" : $" Filter: {company}")}";
        return true;
    }

    private static bool TryBuildImportPaymentTrackingCountSql(string message, out string countSql)
    {
        countSql = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("import") || !m.Contains("payment")) return false;
        if (m.Contains("reliance") || m.Contains("document") || m.Contains("upload") || m.Contains("attach"))
            return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "status = 'Pending'" };
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(BuyerName LIKE '%{lit}%' OR BeneficiaryName LIKE '%{lit}%')");
        }

        countSql = $"""
            SELECT COUNT(*) AS TotalCount
            FROM vw_PurchadeOrderImport WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            """;
        return true;
    }

    private static bool TryBuildImportPoPendingReceiptSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("import")) return false;
        if (!m.Contains("not received") && !m.Contains("pending") && !m.Contains("mrn"))
            return false;
        if (!m.Contains("po") && !m.Contains("purchase order") && !m.Contains("order") && !m.Contains("material"))
            return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "pendingqty > 0" };
        if (!string.IsNullOrWhiteSpace(company))
        {
            var lit = EscapeSqlLiteral(company);
            filters.Add($"(BuyerName LIKE '%{lit}%' OR BeneficiaryName LIKE '%{lit}%')");
        }

        sql = $"""
            SELECT TOP {MaxReturnRows}
                orderno, BuyerName, BeneficiaryName, OrderDate,
                poqty, MRNqty, pendingqty, AdvancelicNo
            FROM vw_ImportPurchasewithPOandMRNqty WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY pendingqty DESC, OrderDate DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed import PO lines with material not fully received (vw_ImportPurchasewithPOandMRNqty.pendingqty > 0)."
            : $"Governed import PO pending receipt for {company} (vw_ImportPurchasewithPOandMRNqty).";
        return true;
    }

    private static bool TryBuildImportDocumentSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("import")) return false;
        if (!m.Contains("document") && !m.Contains("upload") && !m.Contains("attach")) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string> { "status = 'Pending'", "Attach1 IS NULL" };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"BuyerName LIKE '%{EscapeSqlLiteral(company)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                OrderNo, OrderDate, BuyerName, BeneficiaryName, status,
                FileName1, FileName2, FileName3, LCExpiryDate, AdvanceLicNo, DescriptionofGoods
            FROM vw_PurchadeOrderImport WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY OrderDate DESC
            """;
        warning =
            $"Governed import POs pending document upload (vw_PurchadeOrderImport; Attach1 IS NULL).{(string.IsNullOrWhiteSpace(company) ? "" : $" Filter: {company}")}";
        return true;
    }

    private static bool TryBuildAdvanceLicenseExpirySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("advance") && !m.Contains("license") && !m.Contains("licence")) return false;
        if (!m.Contains("expir") && !m.Contains("incomplete") && !m.Contains("not completed")) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>
        {
            "IMPexpirtydate IS NOT NULL",
            "IMPexpirtydate >= CAST(GETDATE() AS date)",
            "IMPexpirtydate <= DATEADD(month, 6, GETDATE())",
            "(ISNULL(POQty, 0) < ISNULL(Licqty, 0) OR ISNULL(SIQty, 0) < ISNULL(Licqty, 0))",
        };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"GroupName LIKE '%{EscapeSqlLiteral(company)}%'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                GroupName, AdvLicenseNo, ImpItemName, Licqty, POQty, SIQty,
                IMPexpirtydate, LicValue, LicValueUSD
            FROM vw_advancelicenecePOSI WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY IMPexpirtydate
            """;
        warning =
            "Governed advance licenses expiring within 6 months with incomplete PO/SI qty (vw_advancelicenecePOSI.IMPexpirtydate).";
        return true;
    }

    private static bool TryBuildRepresentativeWiseSalesSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("representative") && !m.Contains("rep wise") && !m.Contains("rep-wise") && !m.Contains("salesman"))
            return false;
        if (!m.Contains("sales") && !m.Contains("sold") && !m.Contains("qty") && !m.Contains("quantity"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var orderCol = m.Contains("qty") || m.Contains("quantity") ? "SalesQty" : "SalesAmount";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                lm.RepresentiveName AS Representative,
                ROUND(SUM(sv.BillAMount), 2) AS SalesAmount,
                ROUND(SUM(ISNULL(ebd.SalesQty, 0)), 2) AS SalesQty,
                COUNT(DISTINCT sv.InvNo) AS InvoiceCount
            FROM vw_Salesvoucher sv WITH (NOLOCK)
            INNER JOIN LedgerMaster lm WITH (NOLOCK)
                ON sv.CompanyName = lm.CompanyName AND sv.BuyerName = lm.LedgerName
            LEFT JOIN (
                SELECT companyname, InvNo, SUM(qTY) AS SalesQty
                FROM VW_SALES_EBD_DTL WITH (NOLOCK)
                WHERE MISCATEGORY IN ('RM', 'SF', 'FG')
                GROUP BY companyname, InvNo
            ) ebd ON sv.CompanyName = ebd.companyname AND sv.InvNo = ebd.InvNo
            WHERE sv.CompanyName = '{companyLit}'
              AND sv.InvDate >= '{start:yyyy-MM-dd}'
              AND sv.InvDate <= '{end:yyyy-MM-dd}'
              AND lm.Under LIKE 'Debtors%'
              AND ISNULL(lm.RepresentiveName, '') NOT IN ('', '.')
              AND sv.InvType NOT LIKE '%InterUnit%'
            GROUP BY lm.RepresentiveName
            ORDER BY {orderCol} DESC
            """;
        warning =
            $"Governed representative-wise sales qty + value for {company} ({periodLabel}) via vw_Salesvoucher + LedgerMaster.RepresentiveName + VW_SALES_EBD_DTL.";
        return true;
    }

    private static bool TryBuildAllExpensesMonthlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeAllExpensesMonthlyQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveExpensePeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var expenseFilter = BuildBroadExpenseLedgerFilter("av.LedgerName");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                YEAR(av.VoucherDate) AS ExpenseYear,
                MONTH(av.VoucherDate) AS ExpenseMonth,
                LEFT(DATENAME(month, av.VoucherDate), 3) AS MonthName,
                ISNULL(g.expensegrouphead, 'Unmapped') AS ExpenseGroupHead,
                ROUND(SUM(ISNULL(av.Debit, 0)), 2) AS ExpenseAmount
            FROM AccountVoucherApproval av WITH (NOLOCK)
            LEFT JOIN vw_Commonledgergrouping g WITH (NOLOCK)
                ON av.LedgerName = g.ledgername
               AND g.expensehead = 'Expenses'
            WHERE av.CompanyName = '{companyLit}'
              AND {expenseFilter}
              AND av.Status IN ('Approved', 'Pending')
              AND av.VoucherDate >= '{start:yyyy-MM-dd}'
              AND av.VoucherDate <= '{end:yyyy-MM-dd}'
            GROUP BY
                YEAR(av.VoucherDate),
                MONTH(av.VoucherDate),
                DATENAME(month, av.VoucherDate),
                g.expensegrouphead
            ORDER BY ExpenseYear, ExpenseMonth, ExpenseAmount DESC
            """;
        warning =
            $"Governed all expense group heads month-wise for {company} ({periodLabel}) on AccountVoucherApproval + vw_Commonledgergrouping (Approved + Pending).";
        return true;
    }

    private static bool TryBuildTopExpenseLedgersSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeTopExpenseLedgersQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveExpensePeriod(message);
        var topN = ParseTopN(message, 15);
        var companyLit = EscapeSqlLiteral(company);
        var expenseFilter = BuildBroadExpenseLedgerFilter("av.LedgerName");

        sql = $"""
            SELECT TOP {topN}
                av.LedgerName,
                ISNULL(g.expensegrouphead, 'Unmapped') AS ExpenseGroupHead,
                COUNT(*) AS VoucherCount,
                ROUND(SUM(ISNULL(av.Debit, 0)), 2) AS TotalExpense
            FROM AccountVoucherApproval av WITH (NOLOCK)
            LEFT JOIN vw_Commonledgergrouping g WITH (NOLOCK)
                ON av.LedgerName = g.ledgername
               AND g.expensehead = 'Expenses'
            WHERE av.CompanyName = '{companyLit}'
              AND {expenseFilter}
              AND av.Status IN ('Approved', 'Pending')
              AND av.VoucherDate >= '{start:yyyy-MM-dd}'
              AND av.VoucherDate <= '{end:yyyy-MM-dd}'
            GROUP BY av.LedgerName, g.expensegrouphead
            HAVING SUM(ISNULL(av.Debit, 0)) > 0
            ORDER BY TotalExpense DESC
            """;
        warning =
            $"Governed top {topN} expense ledgers for {company} ({periodLabel}) on AccountVoucherApproval + vw_Commonledgergrouping (Approved + Pending).";
        return true;
    }

    private static bool TryBuildExpenseMonthlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("expense") && !m.Contains("expenses")) return false;
        if (m.Contains("exceptional") || m.Contains("unusual") || m.Contains("not in line")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var ledger = ResolveNamedExpenseLedger(message);
        if (string.IsNullOrWhiteSpace(ledger)) return false;

        var hasMonthlyIntent = m.Contains("month") || m.Contains("fy") || m.Contains("financial year")
            || m.Contains("period") || Regex.IsMatch(m, @"\blast\s+\d");
        if (!hasMonthlyIntent && !m.Contains("power") && !m.Contains("electric"))
            return false;

        var (start, end, periodLabel) = ResolveMultiFinancialYearSpan(message, defaultYears: 3);
        if (Regex.IsMatch(message, @"\blast\s+\d{1,2}\s+months?", RegexOptions.IgnoreCase)
            || m.Contains("this month") || m.Contains("particular period"))
            (start, end, periodLabel) = ResolveSalesPeriod(message);

        var companyLit = EscapeSqlLiteral(company);
        var monthCount = CountMonthsInclusive(start, end);
        var isPowerExpense = IsPowerOrElectricExpenseQuestion(m, ledger);
        string ledgerFilter;
        string ledgerLabel;
        if (isPowerExpense)
        {
            ledgerFilter = BuildPowerExpenseLedgerFilter(companyLit);
            ledgerLabel = "power/electricity expense ledgers (vw_Commonledgergrouping + Power Expense)";
        }
        else
        {
            ledgerFilter = BuildRelatedExpenseLedgerFilter(companyLit, ledger);
            ledgerLabel = $"{ledger} + related ledgers (vw_Commonledgergrouping)";
        }

        sql = BuildAccountVoucherMonthlySpineSql(
            companyLit,
            ledgerFilter,
            start,
            end,
            monthCount,
            amountColumn: "Debit",
            yearAlias: "ExpenseYear",
            monthAlias: "ExpenseMonth",
            amountAlias: "ExpenseAmount",
            statusFilter: "Status IN ('Approved', 'Pending')");
        warning =
            $"Governed monthly {ledgerLabel} for {company} ({periodLabel}, {monthCount} month grid) on AccountVoucherApproval (Approved + Pending vouchers; months with no postings show 0; ERP-validated).";
        return true;
    }

    private static bool TryBuildSalesPerKgSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (LooksLikeProductLineSalesQuestion(message)) return false;

        var m = message.ToLowerInvariant();
        if (!m.Contains("sales") && !m.Contains("sold")) return false;
        if (!m.Contains("per kg") && !m.Contains("perkg") && !m.Contains("/kg") && !m.Contains("rate per"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var wantsMonthly = m.Contains("monthly") || m.Contains("month wise") || m.Contains("by month");
        var companyLit = EscapeSqlLiteral(company);

        if (wantsMonthly)
        {
            sql = $"""
                SELECT
                    YEAR(invdate) AS SalesYear,
                    MONTH(invdate) AS SalesMonth,
                    LEFT(DATENAME(month, invdate), 3) AS MonthName,
                    ROUND(SUM(Amount), 2) AS SalesAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE WHEN SUM(netwt) > 0 THEN ROUND(SUM(Amount) / SUM(netwt), 2) END AS PerKg
                FROM vw_Sales_EBIDTA WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{start:yyyy-MM-dd}'
                  AND invdate <= '{end:yyyy-MM-dd}'
                  AND InterGroup <> 'Intergroup'
                GROUP BY YEAR(invdate), MONTH(invdate), DATENAME(month, invdate)
                ORDER BY YEAR(invdate), MONTH(invdate)
                """;
            warning =
                $"Governed monthly sales price per kg for {company} ({periodLabel}) on vw_Sales_EBIDTA.";
        }
        else
        {
            sql = $"""
                SELECT
                    ROUND(SUM(Amount), 2) AS SalesAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE WHEN SUM(netwt) > 0 THEN ROUND(SUM(Amount) / SUM(netwt), 2) END AS PerKg
                FROM vw_Sales_EBIDTA WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND invdate >= '{start:yyyy-MM-dd}'
                  AND invdate <= '{end:yyyy-MM-dd}'
                  AND InterGroup <> 'Intergroup'
                """;
            warning =
                $"Governed sales price per kg summary for {company} ({periodLabel}) on vw_Sales_EBIDTA.";
        }

        return true;
    }

    private static bool TryBuildItemWiseProductionSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("production") && !m.Contains("produced")) return false;
        if (!m.Contains("item") && !m.Contains("itemwise") && !m.Contains("item wise") && !m.Contains("item-wise"))
            return false;
        if (m.Contains("sales") || m.Contains("despatch") || m.Contains("dispatch")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT TOP {MaxReturnRows}
                ItemCode,
                ItemName,
                GroupName,
                PlantName,
                ROUND(SUM(Qty), 2) AS ProductionQty,
                COUNT(DISTINCT CAST(sysdate AS date)) AS ProductionDays
            FROM VW_PRODUCTION_EBD_DTL WITH (NOLOCK)
            WHERE companyname = '{companyLit}'
              AND sysdate >= '{start:yyyy-MM-dd}'
              AND sysdate <= '{end:yyyy-MM-dd}'
            GROUP BY ItemCode, ItemName, GroupName, PlantName
            ORDER BY ProductionQty DESC
            """;
        warning =
            $"Governed item-wise production qty for {company} ({periodLabel}) on VW_PRODUCTION_EBD_DTL.";
        return true;
    }

    private static bool TryBuildExceptionalExpenseSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("exceptional") && !m.Contains("unusual") && !m.Contains("not in line"))
            return false;
        if (!m.Contains("expense") && !m.Contains("expenses")) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var companyLit = EscapeSqlLiteral(company);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var priorStart = monthStart.AddMonths(-1);

        sql = $"""
            SELECT TOP {MaxReturnRows}
                LedgerName,
                ROUND(SUM(CASE WHEN VoucherDate >= '{monthStart:yyyy-MM-dd}' THEN ISNULL(Debit, 0) ELSE 0 END), 2) AS CurrentMonthExpense,
                ROUND(SUM(CASE WHEN VoucherDate >= '{priorStart:yyyy-MM-dd}' AND VoucherDate < '{monthStart:yyyy-MM-dd}' THEN ISNULL(Debit, 0) ELSE 0 END), 2) AS PriorMonthExpense
            FROM AccountVoucherApproval WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND Status = 'Approved'
              AND VoucherDate >= '{priorStart:yyyy-MM-dd}'
              AND LedgerName IN (
                  SELECT DISTINCT ledgername FROM vw_Commonledgergrouping WITH (NOLOCK)
                  WHERE expensehead = 'Expenses' OR expensegrouphead LIKE '%Expense%'
              )
            GROUP BY LedgerName
            HAVING SUM(CASE WHEN VoucherDate >= '{monthStart:yyyy-MM-dd}' THEN ISNULL(Debit, 0) ELSE 0 END)
                 > 1.5 * NULLIF(SUM(CASE WHEN VoucherDate >= '{priorStart:yyyy-MM-dd}' AND VoucherDate < '{monthStart:yyyy-MM-dd}' THEN ISNULL(Debit, 0) ELSE 0 END), 0)
            ORDER BY CurrentMonthExpense DESC
            """;
        warning =
            $"Governed exceptional expenses (>150% vs prior month) for {company} on AccountVoucherApproval + expense ledger grouping.";
        return true;
    }

    private static bool TryBuildStoresInventoryValueSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("stores") && !m.Contains("store inventory") && !m.Contains("spares"))
            return false;
        if (!m.Contains("value") && !m.Contains("worth"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                ROUND(SUM(ISNULL(StkInHand, 0) * ISNULL(Rate, 0)), 2) AS InventoryValue,
                ROUND(SUM(ISNULL(StkInHand, 0)), 2) AS TotalQty,
                COUNT(*) AS ItemLines
            FROM WareHouse WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND ISNULL(StkInHand, 0) > 0
              AND (GroupName NOT LIKE '%RM%' OR GroupName IS NULL)
              AND (Deptt NOT LIKE '%RM%' OR Deptt IS NULL)
            """;
        warning =
            $"Governed stores/spares inventory value (Rate × StkInHand, excl. RM groups) for {company} on WareHouse.";
        return true;
    }

    private static bool TryBuildMostPurchasedItemsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("purchas") && !m.Contains("bought")) return false;
        if (!m.Contains("most") && !m.Contains("top") && !m.Contains("highest")) return false;
        if (IsRmPurchaseFocusQuestion(m)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var topN = ParseTopN(message, 10);
        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var wantsValue = m.Contains("value") || m.Contains("amount");
        var orderCol = wantsValue ? "PurchasedValue" : "PurchasedQty";

        sql = $"""
            SELECT TOP {topN}
                ItemCode,
                ItemName,
                ROUND(SUM(ISNULL(AcceptedQty, RecdQty)), 2) AS PurchasedQty,
                ROUND(SUM(ISNULL(Amount, 0)), 2) AS PurchasedValue,
                COUNT(DISTINCT MRNo) AS ReceiptCount
            FROM Vw_StoreInwards WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND BillDate >= '{start:yyyy-MM-dd}'
              AND BillDate <= '{end:yyyy-MM-dd}'
              AND ISNULL(Deptt, '') NOT LIKE '%RM%'
              AND ISNULL(ItemName, '') NOT LIKE '%RAW MATERIAL%'
            GROUP BY ItemCode, ItemName
            ORDER BY {orderCol} DESC
            """;
        warning =
            $"Governed top {topN} purchased items (excl. RM dept) by {orderCol} for {company} ({periodLabel}) on Vw_StoreInwards.";
        return true;
    }

    private static bool TryBuildMostConsumedItemsSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("consum") && !m.Contains("consumed"))
            return false;
        if (!m.Contains("most") && !m.Contains("top") && !m.Contains("highest"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var topN = ParseTopN(message, 10);
        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var wantsValue = m.Contains("value") || m.Contains("amount");
        var orderCol = wantsValue ? "ConsumedValue" : "ConsumedQty";

        if (wantsValue)
        {
            sql = $"""
                SELECT TOP {topN}
                    Itemcode,
                    ItemName,
                    ROUND(SUM(Qty), 2) AS ConsumedQty,
                    ROUND(SUM(Amount), 2) AS ConsumedValue
                FROM StoreOutwards WITH (NOLOCK)
                WHERE CompName = '{companyLit}'
                  AND CAST(SysDate AS date) >= '{start:yyyy-MM-dd}'
                  AND CAST(SysDate AS date) <= '{end:yyyy-MM-dd}'
                  AND ISNULL(Deptt, '') NOT LIKE '%RM%'
                GROUP BY Itemcode, ItemName
                ORDER BY {orderCol} DESC
                """;
            warning =
                $"Governed top {topN} consumed items by value for {company} ({periodLabel}) on StoreOutwards (excl. RM dept).";
            return true;
        }

        sql = $"""
            SELECT TOP {topN}
                Itemcode,
                Itemname,
                ROUND(SUM(OutwardQty), 2) AS ConsumedQty,
                COUNT(DISTINCT CONCAT(CAST(Year AS varchar(4)), '-', CAST(Month AS varchar(2)))) AS MonthsActive
            FROM vw_ItemMonthlyInwardOutward WITH (NOLOCK)
            WHERE companyname = '{companyLit}'
              AND DATEFROMPARTS(Year, Month, 1) >= '{start:yyyy-MM-dd}'
              AND DATEFROMPARTS(Year, Month, 1) <= '{end:yyyy-MM-dd}'
              AND ISNULL(OutwardQty, 0) > 0
            GROUP BY Itemcode, Itemname
            ORDER BY ConsumedQty DESC
            """;
        warning =
            $"Governed top {topN} consumed items by qty for {company} ({periodLabel}) on vw_ItemMonthlyInwardOutward.";
        return true;
    }

    private static bool TryBuildStoreConsumptionSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("consum") && !m.Contains("consumption"))
            return false;
        if (m.Contains("most") || m.Contains("top"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);
        var groupMatch = Regex.Match(message, @"\b(?:group|category)\s+([A-Za-z0-9 /&-]{2,40})", RegexOptions.IgnoreCase);
        var groupFilter = groupMatch.Success
            ? $" AND (ItemName LIKE '%{EscapeSqlLiteral(groupMatch.Groups[1].Value.Trim())}%' OR Itemcode LIKE '%{EscapeSqlLiteral(groupMatch.Groups[1].Value.Trim())}%')"
            : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                Itemcode,
                ItemName,
                ROUND(SUM(Qty), 2) AS ConsumedQty,
                ROUND(SUM(Amount), 2) AS ConsumedValue
            FROM StoreOutwards WITH (NOLOCK)
            WHERE CompName = '{companyLit}'
              AND CAST(SysDate AS date) >= '{start:yyyy-MM-dd}'
              AND CAST(SysDate AS date) <= '{end:yyyy-MM-dd}'
              {groupFilter}
            GROUP BY Itemcode, ItemName
            ORDER BY ConsumedQty DESC
            """;
        warning =
            $"Governed store consumption by item for {company} ({periodLabel}) on StoreOutwards.";
        return true;
    }

    private static bool TryBuildLedgerAnomalyBalancesSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        var creditorsDebit = m.Contains("creditor") && m.Contains("debit");
        var debtorsCredit = m.Contains("debtor") && m.Contains("credit");
        if (!creditorsDebit && !debtorsCredit) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;
        var companyLit = EscapeSqlLiteral(company);

        if (creditorsDebit)
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    LedgerName,
                    Under,
                    ISNULL(NULLIF(PendingBalance, 0), Openingbalance) AS DebitBalance,
                    Openingbalance AS FyOpeningBalance
                FROM LedgerMaster WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND Under LIKE 'Creditors%'
                  AND ISNULL(NULLIF(PendingBalance, 0), Openingbalance) > 0
                ORDER BY ISNULL(NULLIF(PendingBalance, 0), Openingbalance) DESC
                """;
            warning =
                $"Governed creditors with debit balance for {company} on LedgerMaster (DebitBalance = PendingBalance or FY Openingbalance when pending is stale/zero).";
        }
        else
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    LedgerName,
                    Under,
                    ISNULL(NULLIF(PendingBalance, 0), Openingbalance) AS CreditBalance,
                    Openingbalance AS FyOpeningBalance
                FROM LedgerMaster WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND Under LIKE 'Debtors%'
                  AND ISNULL(NULLIF(PendingBalance, 0), Openingbalance) < 0
                ORDER BY ISNULL(NULLIF(PendingBalance, 0), Openingbalance)
                """;
            warning =
                $"Governed debtors with credit balance for {company} on LedgerMaster (CreditBalance = PendingBalance or FY Openingbalance when pending is stale/zero).";
        }

        return true;
    }

    private static bool TryBuildExpenseLedgerLookupSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("expense") && !m.Contains("expenses"))
            return false;
        if (!m.Contains("power") && !m.Contains("electric") && !m.Contains("energy"))
            return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>
        {
            "(expensehead LIKE '%power%' OR expensehead LIKE '%electric%' OR expensegrouphead LIKE '%power%' OR expensegrouphead LIKE '%electric%' OR ledgername LIKE '%power%' OR ledgername LIKE '%electric%')"
        };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, ledgername, expensehead, expensegrouphead
            FROM vw_Commonledgergrouping WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY ledgername
            """;
        warning =
            "Governed power/electricity ledger lookup (vw_Commonledgergrouping). For month-wise amounts use ledger statement on the matched ledger name.";
        return true;
    }

    private static bool TryBuildPeriodWastageSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("wastage") && !m.Contains("wastages")) return false;
        if (!Regex.IsMatch(m, @"last\s+\d+\s+months?|\d+\s+months?|fy|financial\s+year|period|since"))
            return false;
        if (m.Contains("department") || m.Contains(" dept") || m.Contains("%"))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (start, end, periodLabel) = ResolveSalesPeriod(message);
        var companyLit = EscapeSqlLiteral(company);

        sql = $"""
            SELECT
                CAST(Sysdate AS date) AS ReportDate,
                Particulars AS Department,
                ROUND(SUM(ISNULL(Wastage, 0)), 2) AS WastageKg,
                ROUND(SUM(
                    ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                ), 2) AS ProductionQty
            FROM vw_FactoryProduction WITH (NOLOCK)
            WHERE companyname = '{companyLit}'
              AND CAST(Sysdate AS date) >= '{start:yyyy-MM-dd}'
              AND CAST(Sysdate AS date) <= '{end:yyyy-MM-dd}'
            GROUP BY CAST(Sysdate AS date), Particulars
            ORDER BY ReportDate DESC, Department
            """;
        warning =
            $"Governed factory wastage kg by date/dept for {company} ({periodLabel}) on vw_FactoryProduction.";
        return true;
    }

    private static (DateTime Start, DateTime EndInclusive, string Label) ResolveSalesPeriod(string message)
    {
        if (Regex.IsMatch(message, @"\blast\s+\d{1,2}\s+months?|\blast\s+(?:three|six)\s+months?|this year|ytd|fy|financial\s+year|\d{2}\s*[-–/]\s*\d{2}",
                RegexOptions.IgnoreCase))
            return ParseSalesRollingPeriod(message);

        var (fyStart, fyEndEx, fyLabel) = ParseIndianFinancialYear(message);
        return (fyStart, fyEndEx.AddDays(-1), $"FY {fyLabel}");
    }

    private static bool TryParseMisCategoryIntent(string message, out List<string> categories, out string label)
    {
        categories = [];
        label = "";
        var m = message.ToLowerInvariant();
        var excludesRm = Regex.IsMatch(m,
            @"\b(apart from|excluding|except|without|other than|non)\s+(rm|raw material)\b",
            RegexOptions.IgnoreCase);

        if (!excludesRm && (Regex.IsMatch(m, @"\brm\b") || m.Contains("raw material")))
            categories.Add("RM");
        if (m.Contains("sf") || m.Contains("semi finished") || m.Contains("semi-finished"))
            categories.Add("SF");
        if (m.Contains("fg") || m.Contains("finished good") || m.Contains("finished goods"))
            categories.Add("FG");

        if (categories.Count == 0
            && (m.Contains("main group") || m.Contains("miscategory") || m.Contains("mis category")))
        {
            categories.AddRange(["RM", "SF", "FG"]);
        }

        if (categories.Count == 0) return false;
        label = string.Join("/", categories.Distinct(StringComparer.OrdinalIgnoreCase));
        categories = categories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return true;
    }

    private static int? TryParseInactiveDays(string message)
    {
        var m = message.ToLowerInvariant();
        foreach (var days in new[] { 360, 180, 120, 90 })
        {
            if (m.Contains($"{days} day") || m.Contains($"{days}-day"))
                return days;
        }

        var match = Regex.Match(m, @"(\d{2,3})\s+days?");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed))
            return Math.Clamp(parsed, 30, 720);
        return null;
    }

    private const string TradedGoodsSqlCase = """
        CASE
            WHEN (Groupname = 'FG' AND SubGroupName = 'Trading Items')
              OR Groupname LIKE '%Traded%' OR SubGroupName LIKE '%Traded%'
              OR Groupname LIKE '%Trading%' OR SubGroupName LIKE '%Trading%'
            THEN 'Traded Goods'
            ELSE 'Own Production'
        END
        """;

    private static IEnumerable<string> ResolveErpCompanyCodes(string companyName)
    {
        var c = companyName.ToLowerInvariant();
        if (c.Contains("woven")) yield return "KPW";
        if (c.Contains("unit -ii") && c.Contains("plastene india")) yield return "PIL2";
        if (c.Contains("plastene india") && !c.Contains("unit")) yield return "PIL";
        if (c.Contains("polyfilms")) yield return "PPL";
        if (c.Contains("oswal")) yield return "OEL";
        if (c.Contains("hcp") || c.Contains("bulkpack")) yield return "HPBL";
        yield return companyName;
    }

    private static readonly HashSet<string> ExpenseLedgerStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "top", "highest", "largest", "major", "biggest", "all", "other", "any", "total", "every",
        "exceptional", "unusual", "month", "monthly", "annual", "average", "high", "low"
    };

    private static bool IsGarbageExpenseLedgerHint(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return true;
        var trimmed = candidate.Trim();
        if (ExpenseLedgerStopWords.Contains(trimmed)) return true;
        if (Regex.IsMatch(trimmed, @"^\d+$")) return true;

        var tokens = trimmed.Split([' ', '-', '/'], StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 && tokens.All(t => ExpenseLedgerStopWords.Contains(t) || Regex.IsMatch(t, @"^\d+$"));
    }

    private static string? ResolveNamedExpenseLedger(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("power") || m.Contains("electric") || m.Contains("eb bill"))
            return "Power Expense";

        var match = Regex.Match(message,
            @"\b([A-Za-z][A-Za-z0-9 /&-]{2,40}?)\s+expenses?\b",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var candidate = match.Groups[1].Value.Trim();
            if (!IsGarbageExpenseLedgerHint(candidate)
                && !candidate.Contains("exceptional", StringComparison.OrdinalIgnoreCase)
                && !candidate.Contains("other", StringComparison.OrdinalIgnoreCase))
                return NormalizeExpenseLedgerName(candidate);
        }

        match = Regex.Match(message,
            @"expense(?:s)?\s+(?:of|for)\s+([A-Za-z][A-Za-z0-9 /&-]{2,40})",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var candidate = match.Groups[1].Value.Trim();
            if (!IsGarbageExpenseLedgerHint(candidate))
                return NormalizeExpenseLedgerName(candidate);
        }

        if (m.Contains("other expense") || m.Contains("any expense"))
            return null;

        return null;
    }

    private static string NormalizeExpenseLedgerName(string fragment)
    {
        var trimmed = fragment.Trim();
        if (trimmed.EndsWith(" Expense", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith(" Expenses", StringComparison.OrdinalIgnoreCase))
            return trimmed.Replace(" Expenses", " Expense", StringComparison.OrdinalIgnoreCase);

        return $"{trimmed} Expense";
    }

    private static string ResolvePowerExpenseLedgerName(string message) =>
        ResolveNamedExpenseLedger(message) ?? "Power Expense";

    private static (DateTime Start, DateTime EndInclusive, string Label) ResolveMultiFinancialYearSpan(
        string message,
        int defaultYears)
    {
        if (Regex.IsMatch(message, @"\blast\s+\d{1,2}\s+months?", RegexOptions.IgnoreCase))
            return ParseSalesRollingPeriod(message);

        var years = defaultYears;
        var yearMatch = Regex.Match(message, @"\blast\s+(\d{1,2})\s+financial\s+years?", RegexOptions.IgnoreCase);
        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var y))
            years = Math.Clamp(y, 1, 5);

        var today = DateTime.Today;
        var currentFyStartYear = today.Month >= 4 ? today.Year : today.Year - 1;
        var startYear = currentFyStartYear - (years - 1);
        var start = new DateTime(startYear, 4, 1);
        return (start, today, $"last {years} financial years");
    }

    private static int CountMonthsInclusive(DateTime start, DateTime endInclusive) =>
        Math.Max(1, (endInclusive.Year - start.Year) * 12 + endInclusive.Month - start.Month + 1);

    private static bool IsRmPurchaseFocusQuestion(string messageLower)
    {
        if (Regex.IsMatch(messageLower,
                @"\b(apart from|excluding|except|without|other than|non)\s+(rm|raw material)\b",
                RegexOptions.IgnoreCase))
            return false;

        return Regex.IsMatch(messageLower, @"\brm\b") || messageLower.Contains("raw material");
    }

    private static bool IsPowerOrElectricExpenseQuestion(string messageLower, string ledger) =>
        messageLower.Contains("power") || messageLower.Contains("electric") || messageLower.Contains("eb bill")
        || messageLower.Contains("energy")
        || ledger.Equals("Power Expense", StringComparison.OrdinalIgnoreCase);

    private static string BuildPowerExpenseLedgerFilter(string companyLit) =>
        BuildExpenseKeywordLedgerFilter(companyLit, "power", "electric", extraLedgerName: "Power Expense");

    private static string BuildRelatedExpenseLedgerFilter(string companyLit, string ledgerName)
    {
        var keyword = ledgerName
            .Replace(" Expenses", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" Expense", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            keyword = ledgerName;

        return BuildExpenseKeywordLedgerFilter(companyLit, keyword, extraLedgerName: ledgerName);
    }

    private static string BuildExpenseKeywordLedgerFilter(
        string companyLit,
        string primaryKeyword,
        string? secondaryKeyword = null,
        string? extraLedgerName = null,
        string ledgerColumn = "LedgerName")
    {
        var filters = new List<string>
        {
            $"ledgername LIKE '%{EscapeSqlLiteral(primaryKeyword)}%'",
            $"expensehead LIKE '%{EscapeSqlLiteral(primaryKeyword)}%'",
            $"expensegrouphead LIKE '%{EscapeSqlLiteral(primaryKeyword)}%'"
        };
        if (!string.IsNullOrWhiteSpace(secondaryKeyword))
        {
            var secondaryLit = EscapeSqlLiteral(secondaryKeyword);
            filters.Add($"ledgername LIKE '%{secondaryLit}%'");
            filters.Add($"expensehead LIKE '%{secondaryLit}%'");
            filters.Add($"expensegrouphead LIKE '%{secondaryLit}%'");
        }

        var unionLedger = string.IsNullOrWhiteSpace(extraLedgerName)
            ? ""
            : $"""
              
              UNION ALL
              SELECT '{EscapeSqlLiteral(extraLedgerName)}'
              """;

        var primaryLit = EscapeSqlLiteral(primaryKeyword);
        var directPatterns = new List<string>
        {
            $"{ledgerColumn} LIKE '%{primaryLit}%Expense%'"
        };
        if (!string.IsNullOrWhiteSpace(secondaryKeyword))
            directPatterns.Add($"{ledgerColumn} LIKE '%{EscapeSqlLiteral(secondaryKeyword)}%Expense%'");

        return $"""
        (
            {ledgerColumn} IN (
                SELECT DISTINCT ledgername
                FROM vw_Commonledgergrouping WITH (NOLOCK)
                WHERE ({string.Join(" OR ", filters)})
                {unionLedger}
            )
            OR {string.Join(" OR ", directPatterns)}
        )
        """;
    }

    private static string BuildBroadExpenseLedgerFilter(string ledgerColumn = "LedgerName") =>
        $"""
        (
            {ledgerColumn} LIKE '%Expense%'
            OR {ledgerColumn} IN (
                SELECT DISTINCT ledgername
                FROM vw_Commonledgergrouping WITH (NOLOCK)
                WHERE expensehead = 'Expenses'
            )
        )
        """;

    private static bool LooksLikeAllExpensesMonthlyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("expense")) return false;
        if (m.Contains("exceptional") || m.Contains("unusual") || m.Contains("not in line")) return false;
        if (LooksLikeTopExpenseLedgersQuestion(message)) return false;
        if (ResolveNamedExpenseLedger(message) is not null) return false;

        var allAnyTotal = m.Contains("all expense") || m.Contains("any expense") || m.Contains("other expense")
                          || m.Contains("total expense") || m.Contains("every expense")
                          || m.Contains("expense head") || m.Contains("expense group")
                          || m.Contains("all other expense");
        if (!allAnyTotal) return false;

        return true;
    }

    private static bool LooksLikeTopExpenseLedgersQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("expense")) return false;
        if (m.Contains("exceptional") || m.Contains("unusual") || m.Contains("not in line")) return false;
        if (ResolveNamedExpenseLedger(message) is not null) return false;

        return m.Contains("top") || m.Contains("highest") || m.Contains("largest") || m.Contains("major")
               || m.Contains("biggest");
    }

    private static (DateTime Start, DateTime EndInclusive, string Label) ResolveExpensePeriod(string message)
    {
        var m = message.ToLowerInvariant();
        if (Regex.IsMatch(message, @"\blast\s+\d{1,2}\s+months?", RegexOptions.IgnoreCase)
            || m.Contains("this month") || m.Contains("particular period"))
            return ResolveSalesPeriod(message);

        if (m.Contains("last") && m.Contains("financial"))
            return ResolveMultiFinancialYearSpan(message, defaultYears: 3);

        if (m.Contains("fy") || m.Contains("financial year") || Regex.IsMatch(message, @"\b20\d{2}\s*[-–/]\s*\d{2}\b"))
            return ResolveSalesPeriod(message);

        if (m.Contains("month") || m.Contains("period"))
            return ResolveMultiFinancialYearSpan(message, defaultYears: 1);

        return ResolveSalesPeriod(message);
    }

    private static string BuildAccountVoucherMonthlySpineSql(
        string companyLit,
        string ledgerFilterSql,
        DateTime start,
        DateTime endInclusive,
        int monthCount,
        string amountColumn,
        string yearAlias,
        string monthAlias,
        string amountAlias,
        string statusFilter = "Status = 'Approved'")
    {
        var startLit = start.ToString("yyyy-MM-dd");
        var endLit = endInclusive.ToString("yyyy-MM-dd");
        return $"""
            WITH MonthSpine AS (
                SELECT DATEADD(month, n.n, '{startLit}') AS MonthStart
                FROM (
                    SELECT TOP ({monthCount})
                        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
                    FROM sys.all_objects a
                    CROSS JOIN sys.all_objects b
                ) n
                WHERE DATEADD(month, n.n, '{startLit}') <= '{endLit}'
            ),
            Expenses AS (
                SELECT
                    YEAR(VoucherDate) AS ExpenseYear,
                    MONTH(VoucherDate) AS ExpenseMonth,
                    ROUND(SUM(ISNULL({amountColumn}, 0)), 2) AS ExpenseAmount
                FROM AccountVoucherApproval WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND ({ledgerFilterSql})
                  AND {statusFilter}
                  AND VoucherDate >= '{startLit}'
                  AND VoucherDate <= '{endLit}'
                GROUP BY YEAR(VoucherDate), MONTH(VoucherDate)
            )
            SELECT
                YEAR(ms.MonthStart) AS {yearAlias},
                MONTH(ms.MonthStart) AS {monthAlias},
                LEFT(DATENAME(month, ms.MonthStart), 3) AS MonthName,
                ISNULL(e.ExpenseAmount, 0) AS {amountAlias}
            FROM MonthSpine ms
            LEFT JOIN Expenses e
                ON e.ExpenseYear = YEAR(ms.MonthStart)
               AND e.ExpenseMonth = MONTH(ms.MonthStart)
            ORDER BY {yearAlias}, {monthAlias}
            """;
    }
}
