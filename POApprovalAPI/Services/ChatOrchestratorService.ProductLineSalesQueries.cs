using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeProductLineSalesQuestion(string message)
    {
        var m = message.ToLowerInvariant();

        if (LooksLikeCountryWiseSalesQuestion(message)
            || LooksLikeExportSalesInvoiceListQuestion(message)
            || m.Contains("inter unit") || m.Contains("interunit"))
            return false;

        if ((m.Contains("despatch") || m.Contains("dispatch") || m.Contains("packing list"))
            && !m.Contains("sales") && !m.Contains("sold"))
            return false;

        if (m.Contains("production") && !m.Contains("sales") && !m.Contains("sold"))
            return false;

        var hasProductLine = ContainsProductLineIntent(message);
        if (!hasProductLine)
            return false;

        var hasSales = m.Contains("sales") || m.Contains("sold") || m.Contains("revenue");
        var hasRate = m.Contains("per kg") || m.Contains("perkg") || m.Contains("/kg")
                      || m.Contains("average rate") || m.Contains("avg rate") || m.Contains("rate per");
        var hasMonthly = WantsMonthlySalesBreakdown(message);
        var hasRolling = Regex.IsMatch(m, @"last\s+\d{1,2}\s+months?")
                         || m.Contains("last 6 month") || m.Contains("last six month")
                         || m.Contains("last 3 month") || m.Contains("last three month");

        return hasRate || hasMonthly || hasRolling || hasSales;
    }

    private static bool ContainsProductLineIntent(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("fibc") || m.Contains("jumbo") || m.Contains("bulk bag")
            || m.Contains("flexible intermediate"))
            return true;
        if (m.Contains("small bag") || m.Contains("smallbag"))
            return true;
        if (Regex.IsMatch(m, @"\btape\b") && (m.Contains("sales") || m.Contains("sold") || m.Contains("per kg")))
            return true;
        if (m.Contains("fabric") && (m.Contains("sales") || m.Contains("sold") || m.Contains("per kg")))
            return true;
        if (m.Contains("webbing") && (m.Contains("sales") || m.Contains("sold")))
            return true;
        if (m.Contains("product group") || m.Contains("product line"))
            return TryExtractNamedProductGroupFragment(message, out _);
        return false;
    }

    private static bool WantsMonthlySalesBreakdown(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("monthly") || m.Contains("month wise") || m.Contains("month-wise")
               || m.Contains("each month") || m.Contains("by month") || m.Contains("month on month")
               || m.Contains("month-on-month") || Regex.IsMatch(m, @"\bper month\b");
    }

    private static bool TryBuildProductLineSalesSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeProductLineSalesQuestion(message))
            return false;

        if (!TryBuildProductLineFilter(message, out var productFilter, out var productLabel))
            return false;

        var companyFilter = BuildSalesCompanyFilter(message, out var companyNote);
        var (periodStart, periodEnd, periodLabel) = ParseSalesRollingPeriod(message);
        var startLit = periodStart.ToString("yyyy-MM-dd");
        var endLit = periodEnd.ToString("yyyy-MM-dd");
        var wantsMonthly = WantsMonthlySalesBreakdown(message);

        if (wantsMonthly)
        {
            sql = $"""
                SELECT
                    YEAR(invdate) AS SalesYear,
                    MONTH(invdate) AS SalesMonth,
                    LEFT(DATENAME(month, invdate), 3) AS MonthName,
                    ROUND(SUM(Amount), 2) AS SalesAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE
                        WHEN SUM(netwt) > 0
                        THEN ROUND(SUM(Amount) / SUM(netwt), 2)
                    END AS PerKg
                FROM vw_Sales_EBIDTA WITH (NOLOCK)
                WHERE {companyFilter}
                  AND invdate >= '{startLit}'
                  AND invdate <= '{endLit}'
                  AND InterGroup <> 'Intergroup'
                  AND {productFilter}
                GROUP BY YEAR(invdate), MONTH(invdate), DATENAME(month, invdate)
                ORDER BY YEAR(invdate), MONTH(invdate)
                """;
            warning =
                $"Governed monthly {productLabel} sales on vw_Sales_EBIDTA for {companyNote} ({periodLabel}; {startLit} to {endLit}), excl. intercompany. PerKg = Amount/netwt.";
        }
        else
        {
            sql = $"""
                SELECT
                    ROUND(SUM(Amount), 2) AS SalesAmount,
                    ROUND(SUM(netwt), 2) AS QuantityKg,
                    CASE
                        WHEN SUM(netwt) > 0
                        THEN ROUND(SUM(Amount) / SUM(netwt), 2)
                    END AS PerKg,
                    COUNT(DISTINCT CONCAT(YEAR(invdate), '-', RIGHT('0' + CAST(MONTH(invdate) AS varchar(2)), 2))) AS MonthsInPeriod
                FROM vw_Sales_EBIDTA WITH (NOLOCK)
                WHERE {companyFilter}
                  AND invdate >= '{startLit}'
                  AND invdate <= '{endLit}'
                  AND InterGroup <> 'Intergroup'
                  AND {productFilter}
                """;
            warning =
                $"Governed {productLabel} sales summary on vw_Sales_EBIDTA for {companyNote} ({periodLabel}; {startLit} to {endLit}), excl. intercompany. PerKg = weighted Amount/netwt.";
        }

        return true;
    }

    private static string BuildSalesCompanyFilter(string message, out string companyNote)
    {
        var company = ResolveCompanyForChat(message);
        if (!string.IsNullOrWhiteSpace(company))
        {
            companyNote = company;
            return $"CompanyName = '{EscapeSqlLiteral(company)}'";
        }

        var hint = ResolveFactoryGroupHint(message);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            companyNote = $"{hint} group";
            var lit = EscapeSqlLiteral(hint);
            return $"""
                CompanyName IN (
                    SELECT Name FROM FactoryInfo WITH (NOLOCK)
                    WHERE GroupName LIKE '%{lit}%' OR Name LIKE '%{lit}%'
                )
                """;
        }

        companyNote = "all companies";
        return "1=1";
    }

    private static bool TryBuildProductLineFilter(string message, out string filterClause, out string productLabel)
    {
        filterClause = "";
        productLabel = "";
        var m = message.ToLowerInvariant();

        if (m.Contains("fibc") || m.Contains("jumbo") || m.Contains("bulk bag")
            || m.Contains("flexible intermediate"))
        {
            productLabel = "FIBC";
            filterClause = """
                (
                    Groupname LIKE '%FIBC%' OR SubGroupName LIKE '%FIBC%'
                    OR Groupname LIKE '%Jumbo%' OR SubGroupName LIKE '%Jumbo%'
                    OR Groupname LIKE '%Bulk%' OR SubGroupName LIKE '%Bulk%'
                    OR Groupname LIKE '%Flexible%' OR SubGroupName LIKE '%Flexible%'
                )
                """;
            return true;
        }

        if (m.Contains("small bag") || m.Contains("smallbag"))
        {
            productLabel = "Small Bag";
            filterClause = "(Groupname LIKE '%Small%Bag%' OR SubGroupName LIKE '%Small%Bag%')";
            return true;
        }

        if (Regex.IsMatch(m, @"\btape\b"))
        {
            productLabel = "Tape";
            filterClause = "(Groupname LIKE '%Tape%' OR SubGroupName LIKE '%Tape%')";
            return true;
        }

        if (m.Contains("fabric"))
        {
            productLabel = "Fabric";
            filterClause = "(Groupname LIKE '%Fabric%' OR SubGroupName LIKE '%Fabric%' OR Groupname LIKE '%Lam%' OR SubGroupName LIKE '%Lam%')";
            return true;
        }

        if (m.Contains("webbing"))
        {
            productLabel = "Webbing";
            filterClause = "(Groupname LIKE '%Webbing%' OR SubGroupName LIKE '%Webbing%')";
            return true;
        }

        if (TryExtractNamedProductGroupFragment(message, out var frag))
        {
            productLabel = frag;
            var lit = EscapeSqlLiteral(frag);
            filterClause = $"(Groupname LIKE '%{lit}%' OR SubGroupName LIKE '%{lit}%')";
            return true;
        }

        return false;
    }

    private static bool TryExtractNamedProductGroupFragment(string message, out string fragment)
    {
        fragment = "";
        var patterns = new[]
        {
            @"\b(?:monthly|average|total|per kg)?\s*(?:sales|sold)\s+(?:of|for)\s+([A-Za-z][A-Za-z0-9 /&-]{2,40}?)(?:\s+(?:last|for|in|at|fy|financial)|\?|$)",
            @"\b(?:per kg|rate)\s+(?:of|for)\s+([A-Za-z][A-Za-z0-9 /&-]{2,40}?)(?:\s+(?:sold|sales|last)|\?|$)",
            @"\b([A-Za-z][A-Za-z0-9 /&-]{2,40}?)\s+(?:sales|sold)\b",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;
            var raw = match.Groups[1].Value.Trim().TrimEnd('.', '?', ',');
            if (IsSalesNoiseFragment(raw)) continue;
            fragment = raw;
            return true;
        }

        return false;
    }

    private static bool IsSalesNoiseFragment(string fragment)
    {
        var f = fragment.ToLowerInvariant();
        return f is "the" or "all" or "total" or "monthly" or "average"
               || f.Contains("last") || f.Contains("month") || f.Contains("financial")
               || f.Contains("company") || f.Contains("limited") || f.Contains("ltd");
    }

    private static (DateTime Start, DateTime EndInclusive, string Label) ParseSalesRollingPeriod(string message)
    {
        var m = message.ToLowerInvariant();

        if (Regex.IsMatch(m, @"\bfy\b|financial\s+year|\d{2}\s*[-–/]\s*\d{2}|\d{4}\s*[-–/]\s*\d{2}"))
        {
            var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
            return (fyStart, fyEndExclusive.AddDays(-1), $"FY {fyLabel}");
        }

        var monthMatch = Regex.Match(m, @"last\s+(\d{1,2})\s+months?");
        if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out var months))
        {
            months = Math.Clamp(months, 1, 24);
            return BuildRollingMonthWindow(months);
        }

        if (m.Contains("last six month") || m.Contains("last 6 month"))
            return BuildRollingMonthWindow(6);
        if (m.Contains("last three month") || m.Contains("last 3 month"))
            return BuildRollingMonthWindow(3);
        if (m.Contains("this year") || m.Contains("ytd") || m.Contains("year to date"))
            return (new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today, "YTD");

        var monthYear = TryParseMonthYear(message);
        if (monthYear.HasValue)
            return (monthYear.Value.Start, monthYear.Value.End.AddDays(-1), monthYear.Value.Label);

        return BuildRollingMonthWindow(6);
    }

    /// <summary>
    /// "Last N months" = current month plus (N-1) prior calendar months (1st → today).
    /// Avoids spanning N+1 month buckets when using a rolling day offset.
    /// </summary>
    private static (DateTime Start, DateTime EndInclusive, string Label) BuildRollingMonthWindow(int months)
    {
        var end = DateTime.Today;
        var start = new DateTime(end.Year, end.Month, 1).AddMonths(-(months - 1));
        return (start, end, $"last {months} months");
    }
}
