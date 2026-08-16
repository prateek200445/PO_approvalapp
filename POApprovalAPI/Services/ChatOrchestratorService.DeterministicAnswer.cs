using System.Globalization;
using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool ShouldUseDeterministicAnswer(
        bool skipSqlExecution,
        string? warning,
        string sql)
    {
        if (skipSqlExecution) return true;
        if (string.IsNullOrWhiteSpace(warning)) return false;

        return warning.Contains("Governed", StringComparison.OrdinalIgnoreCase)
               || warning.Contains("(governed", StringComparison.OrdinalIgnoreCase)
               || warning.StartsWith("ERP ", StringComparison.OrdinalIgnoreCase)
               || warning.Contains("Rewrote", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDeterministicAnswer(
        string question,
        List<Dictionary<string, object?>> rows,
        string? warning,
        int? totalCount,
        bool truncated)
    {
        if (rows.Count == 0)
            return "No matching records were found for your question.";

        var headline = BuildDeterministicHeadline(rows, totalCount, truncated);
        var body = TryBuildDeterministicBody(rows, warning);
        return string.IsNullOrWhiteSpace(body) ? headline : $"{headline}\n\n{body}";
    }

    private static string BuildDeterministicHeadline(
        List<Dictionary<string, object?>> rows,
        int? totalCount,
        bool truncated)
    {
        if (totalCount.HasValue)
        {
            if (truncated && totalCount.Value > rows.Count)
                return $"Found {totalCount.Value:N0} matching record(s). Showing the first {rows.Count:N0} below — use Export for the full list.";
            return $"Found {totalCount.Value:N0} matching record(s).";
        }

        if (truncated)
            return $"Showing {rows.Count:N0} record(s) below (results may be capped — use Export for more).";

        return rows.Count == 1
            ? "Found 1 matching record."
            : $"Found {rows.Count:N0} matching records.";
    }

    private static string? TryBuildDeterministicBody(
        List<Dictionary<string, object?>> rows,
        string? warning)
    {
        if (!string.IsNullOrWhiteSpace(warning)
            && warning.StartsWith("ERP ledger statement", StringComparison.OrdinalIgnoreCase))
            return BuildLedgerStatementBody(warning, rows);

        if (rows.Count >= 1 && LooksLikeLedgerMasterBalanceRow(rows[0]))
            return BuildLedgerBalanceBody(rows[0]);

        return null;
    }

    private static bool LooksLikeLedgerMasterBalanceRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("LedgerName")
               && (keys.Contains("PendingBalance") || keys.Contains("Openingbalance"));
    }

    private static string BuildLedgerBalanceBody(Dictionary<string, object?> row)
    {
        var ledger = GetRowString(row, "LedgerName") ?? "This party";
        var company = GetRowString(row, "CompanyName");
        var opening = GetRowDecimal(row, "Openingbalance");
        var pending = GetRowDecimal(row, "PendingBalance");
        var under = GetRowString(row, "Under");
        var source = GetRowString(row, "OutstandingSource");
        var period = GetRowString(row, "StatementPeriod");

        var atCompany = string.IsNullOrWhiteSpace(company) ? "" : $" at **{company}**";
        var groupNote = string.IsNullOrWhiteSpace(under) ? "" : $" Group: **{under}**.";

        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceLabel = source switch
            {
                "LedgerStatement" => string.IsNullOrWhiteSpace(period)
                    ? "ERP ledger statement (current FY)"
                    : $"ERP ledger statement (FY {period})",
                "OverdueSummary" => "ERP overdue summary",
                "BillWiseAgeing" => "bill-wise ageing",
                _ => "ERP outstanding report",
            };

            return
                $"**{ledger}**{atCompany}: LedgerMaster snapshot shows **₹0.00** pending, but **{sourceLabel}** shows **{FormatInr(pending)}** outstanding"
                + (opening.HasValue && Math.Abs(opening.Value) >= 0.01m ? $" (opening **{FormatInr(opening)}**)" : "")
                + $".{groupNote}";
        }

        return
            $"**{ledger}**{atCompany} has **{FormatInr(pending)}** pending balance and **{FormatInr(opening)}** opening balance.{groupNote}";
    }

    private static string? BuildLedgerStatementBody(string warning, List<Dictionary<string, object?>> rows)
    {
        var meta = Regex.Match(
            warning,
            @":\s*(.+?)\s+at\s+(.+?)\s+from\s+(\d{4}-\d{2}-\d{2})\s+to\s+(\d{4}-\d{2}-\d{2})\.\s+Opening\s+([\d,.\-]+),\s+closing\s+([\d,.\-]+)",
            RegexOptions.IgnoreCase);
        if (!meta.Success) return null;

        var ledger = meta.Groups[1].Value.Trim();
        var company = meta.Groups[2].Value.Trim();
        var from = DateTime.ParseExact(meta.Groups[3].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = DateTime.ParseExact(meta.Groups[4].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var opening = meta.Groups[5].Value.Trim();
        var closing = meta.Groups[6].Value.Trim();

        var voucherNote = rows.Count switch
        {
            0 => "No voucher lines were returned for this period.",
            1 => "One voucher line is shown below.",
            _ => $"{rows.Count:N0} voucher lines are shown below.",
        };

        return
            $"Ledger statement for **{ledger}** at **{company}** ({FormatIndianDate(from)} – {FormatIndianDate(to)}): opening **₹{opening}**, closing **₹{closing}**. {voucherNote}";
    }

    private static string? GetRowString(Dictionary<string, object?> row, string key)
    {
        var match = row.FirstOrDefault(kvp =>
            kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (match.Key is null) return null;
        var text = match.Value?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static decimal? GetRowDecimal(Dictionary<string, object?> row, string key)
    {
        var match = row.FirstOrDefault(kvp =>
            kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (match.Key is null || match.Value is null) return null;
        if (match.Value is decimal d) return d;
        if (match.Value is double db) return (decimal)db;
        if (match.Value is float f) return (decimal)f;
        if (match.Value is int i) return i;
        if (match.Value is long l) return l;
        return decimal.TryParse(match.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string FormatInr(decimal? amount) =>
        amount.HasValue ? $"₹{amount.Value:N2}" : "₹0.00";

    private static string FormatIndianDate(DateTime date) =>
        date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-IN"));
}
