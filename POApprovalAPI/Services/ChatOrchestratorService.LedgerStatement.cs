using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeLedgerStatementQuestion(string message)
    {
        var m = message.ToLowerInvariant();

        if (LooksLikeAgeingQuestion(message) && !LooksLikeDayBucketAgeing(message))
            return false;

        if (LooksLikeOpeningPendingBalanceQuestion(message)
            || LooksLikeLooseOutstandingBalanceQuestion(message))
            return false;

        var hasStatementIntent =
            m.Contains("ledger statement")
            || m.Contains("ledger summary")
            || m.Contains("account statement")
            || m.Contains("voucher history")
            || m.Contains("transaction history")
            || m.Contains("ledger transactions")
            || m.Contains("show vouchers")
            || m.Contains("voucher details")
            || m.Contains("voucher wise")
            || (m.Contains("statement") && (m.Contains("customer") || m.Contains("vendor")
                || m.Contains("supplier") || m.Contains("party") || m.Contains("ledger")))
            || (m.Contains("ledger") && Regex.IsMatch(m, @"\bfrom\b.*\bto\b"));

        if (!hasStatementIntent)
            return false;

        return TryExtractLedgerPartyName(message) is not null;
    }

    private static bool TryBuildLedgerStatementPlan(string message, out LedgerStatementPlan plan)
    {
        plan = new LedgerStatementPlan();
        if (!LooksLikeLedgerStatementQuestion(message))
            return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var party = TryExtractLedgerPartyName(message);
        if (string.IsNullOrWhiteSpace(party))
            return false;

        var (dateFrom, dateTo) = ResolveLedgerStatementDateRange(message);
        plan.CompanyName = company;
        plan.LedgerName = party;
        plan.DateFrom = dateFrom;
        plan.DateTo = dateTo;
        plan.MaxRows = MaxReturnRows;
        return true;
    }

    private static (DateTime From, DateTime To) ResolveLedgerStatementDateRange(string message)
    {
        var rangeMatch = Regex.Match(
            message,
            @"\bfrom\s+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})\s+to\s+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})\b",
            RegexOptions.IgnoreCase);
        if (rangeMatch.Success
            && DateTime.TryParse(rangeMatch.Groups[1].Value, out var fromDt)
            && DateTime.TryParse(rangeMatch.Groups[2].Value, out var toDt))
            return (fromDt.Date, toDt.Date);

        var isoRange = Regex.Match(
            message,
            @"\bfrom\s+(\d{4}-\d{2}-\d{2})\s+to\s+(\d{4}-\d{2}-\d{2})\b",
            RegexOptions.IgnoreCase);
        if (isoRange.Success
            && DateTime.TryParse(isoRange.Groups[1].Value, out fromDt)
            && DateTime.TryParse(isoRange.Groups[2].Value, out toDt))
            return (fromDt.Date, toDt.Date);

        var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
        var today = DateTime.Today;
        if (message.Contains("fy", StringComparison.OrdinalIgnoreCase)
            || message.Contains("financial year", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(message, @"\b20\d{2}\s*[-–/]\s*\d{2}\b"))
        {
            return (fyStart, fyEndEx.AddDays(-1));
        }

        return (fyStart, today);
    }
}
