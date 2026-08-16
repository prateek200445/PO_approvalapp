using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeStockAgeingQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        var hasAgeing = m.Contains("ageing") || m.Contains("aging");
        var hasStock = m.Contains("stock") || m.Contains("inventory") || m.Contains("item")
                       || m.Contains("raw material") || m.Contains("rm ageing") || m.Contains("subgroup");
        if (!hasAgeing || !hasStock) return false;
        if (m.Contains("debtor") || m.Contains("creditor") || m.Contains("customer") || m.Contains("vendor"))
            return false;
        return true;
    }

    private static bool TryBuildStockAgeingPlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.StockAgeing };
        if (!LooksLikeStockAgeingQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        plan.CompanyName = company;
        plan.ToDate = TryParseAsOnDate(message) ?? DateTime.Today;
        plan.MaxRows = MaxReturnRows;
        plan.StockAgeingSp = ResolveStockAgeingSp(message);

        var subMatch = Regex.Match(message, @"\bsub\s*[- ]?group\s+(.+?)(?:\s+at|\s+for|\?|$)", RegexOptions.IgnoreCase);
        if (subMatch.Success)
            plan.SubGroupName = subMatch.Groups[1].Value.Trim().TrimEnd('.', '?', '!');

        return true;
    }

    private static string ResolveStockAgeingSp(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("720") || m.Contains("above 720")) return "sp_Agingreport_SubgroupName_720Days";
        if (m.Contains("361") || m.Contains("360")) return "sp_Agingreport_SubgroupName_361to720Days";
        if (m.Contains("181") || m.Contains("180")) return "sp_Agingreport_SubgroupName_181to360Days";
        if (m.Contains("91") || m.Contains("90")) return "sp_Agingreport_SubgroupName_91to180Days";
        if (Regex.IsMatch(m, @"\b90\s*days\b")) return "sp_Agingreport_SubgroupName_90Days";
        return "sp_Agingreport_SubgroupName";
    }

    private static bool LooksLikeGroupOverdueDaysQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (LooksLikeStockAgeingQuestion(message)) return false;
        if (!LooksLikeDayBucketAgeing(message) && !m.Contains("group overdue") && !m.Contains("overdue group"))
            return false;
        if (ResolveLedgerPartyForChat(message) is not null) return false;
        return m.Contains("debtor") || m.Contains("creditor") || m.Contains("sundry")
               || m.Contains("trade creditor") || m.Contains("group overdue");
    }

    private static bool TryBuildGroupOverdueDaysPlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.GroupOverdueDays };
        if (!LooksLikeGroupOverdueDaysQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        plan.CompanyName = company;
        plan.ToDate = TryParseAsOnDate(message) ?? DateTime.Today;
        plan.MaxRows = MaxReturnRows;
        plan.GroupName = message.Contains("creditor", StringComparison.OrdinalIgnoreCase)
                         || message.Contains("vendor", StringComparison.OrdinalIgnoreCase)
            ? "Trade Creditors"
            : "Sundry Debtors";

        var dayMatch = Regex.Match(message, @"\b(\d{1,3})\s*days?\b", RegexOptions.IgnoreCase);
        plan.Days = dayMatch.Success && int.TryParse(dayMatch.Groups[1].Value, out var d)
            ? Math.Clamp(d, 1, 365)
            : 90;

        return true;
    }

    private static bool LooksLikeMsmeOverdueQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("msme") && (m.Contains("overdue") || m.Contains("ageing") || m.Contains("aging")
                                      || m.Contains("outstanding"));
    }

    private static bool TryBuildMsmeOverduePlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.MsmeOverdue };
        if (!LooksLikeMsmeOverdueQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var party = ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(party)) return false;

        plan.CompanyName = company;
        plan.LedgerName = party;
        plan.ToDate = TryParseAsOnDate(message) ?? DateTime.Today;
        plan.MaxRows = MaxReturnRows;
        return true;
    }

    private static bool LooksLikeOutstandingAllQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (ResolveLedgerPartyForChat(message) is not null) return false;
        if (LooksLikeStockAgeingQuestion(message)) return false;

        var wantsAll = m.Contains("outstanding all") || m.Contains("all outstanding")
                       || m.Contains("all parties outstanding") || m.Contains("full outstanding");
        if (!wantsAll) return false;

        return m.Contains("debtor") || m.Contains("creditor") || m.Contains("sundry")
               || m.Contains("trade creditor") || m.Contains("vendor") || m.Contains("supplier");
    }

    private static bool TryBuildOutstandingAllPlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.OutstandingAll };
        if (!LooksLikeOutstandingAllQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        plan.CompanyName = company;
        plan.ToDate = TryParseAsOnDate(message) ?? DateTime.Today;
        plan.MaxRows = MaxReturnRows;
        plan.PeriodMonths = 3;

        if (message.Contains("creditor", StringComparison.OrdinalIgnoreCase)
            || message.Contains("vendor", StringComparison.OrdinalIgnoreCase)
            || message.Contains("supplier", StringComparison.OrdinalIgnoreCase))
            plan.GroupName = "Trade Creditors";
        else
            plan.GroupName = "Sundry Debtors";

        var monthMatch = Regex.Match(message, @"\b(\d{1,2})\s*months?\b", RegexOptions.IgnoreCase);
        if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out var months))
            plan.PeriodMonths = Math.Clamp(months, 1, 12);

        return true;
    }

    private static bool LooksLikeSalesDiscountQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("sales discount") || m.Contains("discount report") || m.Contains("discount given"))
               && !m.Contains("vendor rate");
    }

    private static bool TryBuildSalesDiscountPlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.SalesDiscount };
        if (!LooksLikeSalesDiscountQuestion(message)) return false;

        var customer = ResolveLedgerPartyForChat(message)
                       ?? TryExtractCustomerNameForDiscount(message);
        if (!string.IsNullOrWhiteSpace(customer))
        {
            plan.CustomerName = customer;
            plan.SalesDiscountSp = "sp_salesdiscount_customer";
            plan.MaxRows = MaxReturnRows;
            return true;
        }

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        plan.CompanyName = company;
        plan.SalesDiscountSp = "sp_salesdiscount_companyname";
        plan.MaxRows = MaxReturnRows;
        return true;
    }

    private static string? TryExtractCustomerNameForDiscount(string message)
    {
        var m = Regex.Match(message, @"\b(?:customer|buyer|party)\s+(.+?)(?:\s+at|\s+for|\?|$)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var name = m.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
        return name.Length >= 3 ? name : null;
    }
}
