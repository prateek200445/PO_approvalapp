using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeAgeingQuestion(string message)
    {
        if (LooksLikeInactiveCustomersQuestion(message))
            return false;

        var m = message.ToLowerInvariant();

        var hasAgeingIntent =
            m.Contains("ageing") || m.Contains("aging")
            || m.Contains("overdue")
            || m.Contains("age wise") || m.Contains("agewise")
            || m.Contains("age bucket") || m.Contains("age analysis")
            || (m.Contains("outstanding") && (m.Contains("bucket") || m.Contains("days") || m.Contains("month")))
            || Regex.IsMatch(m, @"\b0\s*[-–]\s*30\b|\b31\s*[-–]\s*60\b|\b61\s*[-–]\s*90\b|\b90\s*\+|\b90\s+days\b");

        if (!hasAgeingIntent) return false;

        // Exclude inventory/stock ageing (sp_Agingreport_* domain)
        if ((m.Contains("item") || m.Contains("stock") || m.Contains("inventory") || m.Contains("raw material"))
            && !m.Contains("debtor") && !m.Contains("creditor") && !m.Contains("customer") && !m.Contains("vendor"))
            return false;

        var hasLedgerContext =
            m.Contains("debtor") || m.Contains("creditor")
            || m.Contains("customer") || m.Contains("vendor") || m.Contains("supplier")
            || m.Contains("party") || m.Contains("sundry") || m.Contains("trade creditor")
            || m.Contains("receivable") || m.Contains("payable")
            || m.Contains("ledger");

        return hasLedgerContext || ResolveLedgerPartyForChat(message) is not null;
    }

    private static bool LooksLikeDayBucketAgeing(string message)
    {
        if (LooksLikeInactiveCustomersQuestion(message))
            return false;

        var m = message.ToLowerInvariant();
        return Regex.IsMatch(m, @"\b0\s*[-–]\s*30\b|\b31\s*[-–]\s*60\b|\b61\s*[-–]\s*90\b|\b90\s*\+|\b90\s+days\b")
            || m.Contains("day bucket") || m.Contains("days bucket")
            || m.Contains("age bucket") || m.Contains("bucket wise")
            || (m.Contains("bucket") && (m.Contains("debtor") || m.Contains("creditor")
                || m.Contains("ageing") || m.Contains("aging") || m.Contains("overdue")));
    }

    private static bool TryBuildAgeingReportPlan(string message, out AgeingReportPlan plan)
    {
        plan = new AgeingReportPlan();
        if (!LooksLikeAgeingQuestion(message))
            return false;

        // Day-bucket ageing uses governed SELECT on vw_BillWiseTransaction instead.
        if (LooksLikeDayBucketAgeing(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        plan.CompanyName = company;
        plan.ToDate = TryParseAsOnDate(message) ?? DateTime.Today;
        plan.MaxRows = MaxReturnRows;
        plan.PeriodMonths = 3;

        ResolveAgeingGroups(message, out var g3, out var g4);
        plan.G3 = g3;
        plan.G4 = g4;

        var party = ResolveLedgerPartyForChat(message);
        if (!string.IsNullOrWhiteSpace(party))
        {
            plan.LedgerName = party;
            var m = message.ToLowerInvariant();
            plan.Mode = m.Contains("summary") || m.Contains("total outstanding") && !m.Contains("bill")
                ? AgeingReportMode.PartySummary
                : AgeingReportMode.PartyOverdue;
            return true;
        }

        plan.Mode = AgeingReportMode.GroupPivot;
        return true;
    }

    private static void ResolveAgeingGroups(string message, out string g3, out string? g4)
    {
        var m = message.ToLowerInvariant();
        g4 = null;

        if (m.Contains("creditor") || m.Contains("vendor") || m.Contains("supplier") || m.Contains("payable"))
            g3 = "Trade Creditors";
        else
            g3 = "Sundry Debtors";

        if (m.Contains("overseas") && m.Contains("debtor"))
            g4 = "Debtors-Overseas";
        else if (m.Contains("domestic") && m.Contains("debtor"))
            g4 = "Debtors-Domestic";
        else if (m.Contains("legal") && m.Contains("debtor"))
            g4 = "Debtors-Legal Cases";
        else if (m.Contains("rm") && m.Contains("creditor"))
            g4 = "Creditors-RM";
        else if (m.Contains("service") && m.Contains("creditor"))
            g4 = "Creditors-Services";
    }

    private static DateTime? TryParseAsOnDate(string message)
    {
        var m = Regex.Match(
            message,
            @"\b(?:as\s+on|as\s+at|on)\s+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})\b",
            RegexOptions.IgnoreCase);
        if (m.Success && DateTime.TryParse(m.Groups[1].Value, out var dt))
            return dt.Date;

        m = Regex.Match(
            message,
            @"\b(?:as\s+on|as\s+at)\s+(\d{4}-\d{2}-\d{2})\b",
            RegexOptions.IgnoreCase);
        if (m.Success && DateTime.TryParse(m.Groups[1].Value, out dt))
            return dt.Date;

        return null;
    }
}
