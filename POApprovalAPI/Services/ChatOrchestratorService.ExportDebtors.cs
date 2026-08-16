using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeExportDebtorsDueQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("export debtor") || m.Contains("export debtors") || m.Contains("overseas debtor"))
               && (m.Contains("due") || m.Contains("overdue") || m.Contains("pending") || m.Contains("outstanding"))
               && !m.Contains("last 3 month") && !m.Contains("last three month") && !m.Contains("ageing")
               && !m.Contains("aging");
    }

    private static bool LooksLikeExportDebtorsLast3MonthsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("export debtor") || m.Contains("export debtors") || m.Contains("overseas debtor"))
               && (m.Contains("last 3 month") || m.Contains("last three month") || m.Contains("3 month"));
    }

    private static bool TryBuildExportDebtorsDueSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeExportDebtorsDueQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var party = ResolveLedgerPartyForChat(message);
        if (!string.IsNullOrWhiteSpace(party))
            filters.Add($"LedgerName LIKE '%{EscapeSqlLiteral(party)}%'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, LedgerName, BillNo, BillDate, Type, PendingAmount, BillAmount, DueDate
            FROM AutoMail_Export_Debtors_Due WITH (NOLOCK)
            {where}
            ORDER BY DueDate, PendingAmount DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed export debtors due (AutoMail_Export_Debtors_Due snapshot — refreshed by automail job)."
            : $"Governed export debtors due for {company} (AutoMail_Export_Debtors_Due).";
        return true;
    }

    private static bool TryBuildExportDebtorsLast3MonthsPlan(string message, out ErpFinanceReportPlan plan)
    {
        plan = new ErpFinanceReportPlan { Mode = ErpFinanceReportMode.ExportDebtorsLast3Months };
        if (!LooksLikeExportDebtorsLast3MonthsQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        plan.CompanyName = company;
        plan.GroupCompany = company;
        plan.MaxRows = MaxReturnRows;
        return true;
    }

    private static bool LooksLikeStockAnalysisQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("stock analysis") || m.Contains("stockanalysis")
               || (m.Contains("opening") && m.Contains("closing") && m.Contains("stock"))
               || (m.Contains("factory owned") && m.Contains("stock"));
    }

    private static bool TryBuildStockAnalysisPlan(string message, out ErpInventoryReportPlan plan)
    {
        plan = new ErpInventoryReportPlan { MaxRows = MaxReturnRows };
        if (!LooksLikeStockAnalysisQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
        plan.CompanyName = company;
        plan.DateFrom = fyStart;
        plan.DateTo = fyEndEx.AddDays(-1);
        plan.ReportType = ResolveStockAnalysisReportType(message);
        plan.IntOp = 0;
        plan.Mode = message.Contains("detail", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("item wise", StringComparison.OrdinalIgnoreCase)
            ? ErpInventoryReportMode.StockAnalysisDetail
            : ErpInventoryReportMode.StockAnalysisReport;
        return true;
    }

    private static int ResolveStockAnalysisReportType(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("item wise") || m.Contains("item-wise") || m.Contains("by item")) return 1;
        if (m.Contains("date wise") || m.Contains("date-wise") || m.Contains("by date")) return 2;
        return 0;
    }
}
