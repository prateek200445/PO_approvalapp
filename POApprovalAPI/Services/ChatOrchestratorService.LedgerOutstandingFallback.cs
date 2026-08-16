using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private sealed class LedgerOutstandingEnrichment
    {
        public List<Dictionary<string, object?>> Rows { get; init; } = [];
        public string Sql { get; init; } = "";
        public string Warning { get; init; } = "";
    }

    private static bool LooksLikeNamedLedgerOutstandingQuestion(string message) =>
        LooksLikeOpeningPendingBalanceQuestion(message)
        || LooksLikeLooseOutstandingBalanceQuestion(message);

    private static bool IsLedgerMasterOutstandingSql(string sql) =>
        sql.Contains("LedgerMaster", StringComparison.OrdinalIgnoreCase)
        && sql.Contains("PendingBalance", StringComparison.OrdinalIgnoreCase);

    private static bool IsStaleLedgerMasterBalanceResult(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return false;

        foreach (var row in rows)
        {
            if (!row.Keys.Any(k => k.Equals("LedgerName", StringComparison.OrdinalIgnoreCase)))
                return false;

            var pending = GetRowDecimal(row, "PendingBalance") ?? 0m;
            var opening = GetRowDecimal(row, "Openingbalance") ?? 0m;
            if (Math.Abs(pending) >= 0.01m || Math.Abs(opening) >= 0.01m)
                return false;
        }

        return true;
    }

    private async Task<LedgerOutstandingEnrichment?> TryEnrichStaleLedgerOutstandingAsync(
        string message,
        List<Dictionary<string, object?>> ledgerRows,
        CancellationToken ct)
    {
        var primary = ledgerRows[0];
        var company = GetRowString(primary, "CompanyName") ?? ResolveCompanyForChat(message);
        var ledger = GetRowString(primary, "LedgerName");
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(ledger))
            return null;

        var (fyStart, _, fyLabel) = ParseIndianFinancialYear(message);
        var plan = new LedgerStatementPlan
        {
            CompanyName = company,
            LedgerName = ledger,
            DateFrom = fyStart,
            DateTo = DateTime.Today,
            MaxRows = 1,
        };
        if (CurrentEntities.Value?.Company is { CompanyId: > 0 } resolvedCo
            && resolvedCo.Name.Equals(company, StringComparison.OrdinalIgnoreCase))
            plan.CompanyId = resolvedCo.CompanyId;

        try
        {
            var statement = await _ledgerStatementChat.ExecuteAsync(plan, ct);
            if (Math.Abs(statement.ClosingBalance) >= 0.01m)
            {
                _logger.LogInformation(
                    "LedgerMaster pending was zero; enriched from statement closing {Closing} for {Ledger}",
                    statement.ClosingBalance, ledger);
                return BuildEnrichmentFromStatement(primary, company, ledger, statement, fyLabel, fyStart, DateTime.Today);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ledger statement fallback failed for {Ledger} at {Company}", ledger, company);
        }

        try
        {
            var ageingPlan = new AgeingReportPlan
            {
                Mode = AgeingReportMode.PartySummary,
                CompanyName = company,
                LedgerName = ledger,
                ToDate = DateTime.Today,
                MaxRows = 10,
            };
            var ageing = await _ageingService.ExecuteAsync(ageingPlan, ct);
            if (TrySumOverdueSummaryOutstanding(ageing.Rows) is { } summaryTotal
                && Math.Abs(summaryTotal) >= 0.01m)
            {
                _logger.LogInformation(
                    "LedgerMaster pending was zero; enriched from overdue summary {Total} for {Ledger}",
                    summaryTotal, ledger);
                return BuildEnrichmentFromAgeingSummary(primary, company, ledger, ageing, summaryTotal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Overdue summary fallback failed for {Ledger} at {Company}", ledger, company);
        }

        if (TryBuildPartyBillWiseOutstandingSql(company, ledger, message, out var bucketSql, out var bucketWarn))
        {
            try
            {
                var bucketRows = await ExecuteReadOnlyAsync(bucketSql, ct);
                if (bucketRows.Count > 0
                    && GetRowDecimal(bucketRows[0], "TotalOutstanding") is { } totalOutstanding
                    && Math.Abs(totalOutstanding) >= 0.01m)
                {
                    _logger.LogInformation(
                        "LedgerMaster pending was zero; enriched from bill-wise ageing {Total} for {Ledger}",
                        totalOutstanding, ledger);
                    return BuildEnrichmentFromBillWiseAgeing(primary, company, ledger, bucketRows[0], bucketSql, bucketWarn, totalOutstanding);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bill-wise ageing fallback failed for {Ledger} at {Company}", ledger, company);
            }
        }

        return null;
    }

    /// <summary>
    /// Bill-wise total outstanding for stale LedgerMaster fallback (no day-bucket keywords required).
    /// </summary>
    private static bool TryBuildPartyBillWiseOutstandingSql(
        string company,
        string ledgerName,
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(ledgerName))
            return false;

        var asOn = TryParseAsOnDate(message) ?? DateTime.Today;
        var companyLit = EscapeSqlLiteral(company.Trim());
        var ledgerLit = EscapeSqlLiteral(ledgerName.Trim());
        var asOnLit = asOn.ToString("yyyy-MM-dd");
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
                SUM(ABS(Amount)) AS TotalOutstanding
            FROM (
                SELECT lm.CompanyName, lm.LedgerName, lm.Under, b.Amount
                FROM LedgerMaster lm WITH (NOLOCK)
                INNER JOIN vw_BillWiseTransaction b WITH (NOLOCK)
                    ON b.CompanyName = lm.CompanyName AND b.LedgerName = lm.LedgerName
                WHERE lm.CompanyName = '{companyLit}'
                  AND lm.LedgerName = '{ledgerLit}'
                  AND lm.Under LIKE '{underFilter}'
            ) x
            GROUP BY CompanyName, LedgerName, Under
            """;

        warning =
            $"Governed bill-wise outstanding: vw_BillWiseTransaction total for {ledgerName} at {company} as on {asOnLit}.";
        return true;
    }

    private static LedgerOutstandingEnrichment BuildEnrichmentFromStatement(
        Dictionary<string, object?> ledgerRow,
        string company,
        string ledger,
        LedgerStatementChatResult statement,
        string fyLabel,
        DateTime from,
        DateTime to) =>
        new()
        {
            Rows = [BuildEnrichedBalanceRow(ledgerRow, company, statement.ClosingBalance, statement.OpeningBalance, "LedgerStatement", fyLabel)],
            Sql = statement.SqlDescription,
            Warning =
                $"Governed ledger outstanding for {ledger} at {company}: LedgerMaster.PendingBalance is ₹0 (stale snapshot). Enriched from ERP ledger statement ({FormatIndianDate(from)} – {FormatIndianDate(to)}, FY {fyLabel}): opening {statement.OpeningBalance:N2}, closing {statement.ClosingBalance:N2}.",
        };

    private static LedgerOutstandingEnrichment BuildEnrichmentFromAgeingSummary(
        Dictionary<string, object?> ledgerRow,
        string company,
        string ledger,
        AgeingReportResult ageing,
        decimal totalOutstanding) =>
        new()
        {
            Rows = [BuildEnrichedBalanceRow(ledgerRow, company, totalOutstanding, null, "OverdueSummary", null)],
            Sql = ageing.SqlDescription,
            Warning =
                $"Governed ledger outstanding for {ledger} at {company}: LedgerMaster.PendingBalance is ₹0 (stale snapshot). Enriched from ERP overdue summary (sp_Overdue_Ledger_SUMMARY): total outstanding {totalOutstanding:N2}.",
        };

    private static LedgerOutstandingEnrichment BuildEnrichmentFromBillWiseAgeing(
        Dictionary<string, object?> ledgerRow,
        string company,
        string ledger,
        Dictionary<string, object?> bucketRow,
        string sql,
        string bucketWarn,
        decimal totalOutstanding) =>
        new()
        {
            Rows = [BuildEnrichedBalanceRow(ledgerRow, company, totalOutstanding, null, "BillWiseAgeing", null, bucketRow)],
            Sql = sql,
            Warning =
                $"Governed ledger outstanding for {ledger} at {company}: LedgerMaster.PendingBalance is ₹0 (stale snapshot). Enriched from bill-wise ageing: {bucketWarn} Total outstanding {totalOutstanding:N2}.",
        };

    private static string? GetRowStringOrNull(Dictionary<string, object?>? row, string key) =>
        row is null ? null : GetRowString(row, key);

    private static Dictionary<string, object?> BuildEnrichedBalanceRow(
        Dictionary<string, object?> ledgerRow,
        string resolvedCompany,
        decimal outstanding,
        decimal? opening,
        string source,
        string? periodLabel,
        Dictionary<string, object?>? supplementalRow = null)
    {
        var company = GetRowString(ledgerRow, "CompanyName")
                      ?? GetRowStringOrNull(supplementalRow, "CompanyName")
                      ?? resolvedCompany;
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CompanyName"] = company,
            ["LedgerName"] = GetRowString(ledgerRow, "LedgerName")
                             ?? GetRowStringOrNull(supplementalRow, "LedgerName"),
            ["Under"] = GetRowString(ledgerRow, "Under")
                        ?? GetRowStringOrNull(supplementalRow, "Under"),
            ["PendingBalance"] = outstanding,
            ["Openingbalance"] = opening ?? GetRowDecimal(ledgerRow, "Openingbalance") ?? 0m,
            ["LedgerMasterPending"] = 0m,
            ["OutstandingSource"] = source,
        };
        if (!string.IsNullOrWhiteSpace(periodLabel))
            row["StatementPeriod"] = periodLabel;
        return row;
    }

    private static decimal? TrySumOverdueSummaryOutstanding(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return null;

        decimal net = 0m;
        foreach (var row in rows)
        {
            var amount = GetRowDecimal(row, "PendingAmount") ?? 0m;
            var type = GetRowString(row, "Type");
            if (type?.Equals("Cr", StringComparison.OrdinalIgnoreCase) == true)
                net -= Math.Abs(amount);
            else
                net += Math.Abs(amount);
        }

        return net;
    }
}
