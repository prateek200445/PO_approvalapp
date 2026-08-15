using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Portal-parity ledger voucher statement for chat via sp_ac_LedgerSummary_BankRecoDate.
/// </summary>
public class LedgerStatementChatService
{
    private readonly DatabaseService _database;
    private readonly LedgerSummaryService _ledgerSummary;
    private readonly ILogger<LedgerStatementChatService> _logger;

    public LedgerStatementChatService(
        DatabaseService database,
        LedgerSummaryService ledgerSummary,
        ILogger<LedgerStatementChatService> logger)
    {
        _database = database;
        _ledgerSummary = ledgerSummary;
        _logger = logger;
    }

    public async Task<int?> ResolveCompanyIdAsync(string companyName, CancellationToken ct = default)
    {
        await using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT SrNo FROM FactoryInfo WITH (NOLOCK) WHERE Name = @Name",
            new { Name = companyName.Trim() });
    }

    public async Task<LedgerStatementChatResult> ExecuteAsync(LedgerStatementPlan plan, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plan.LedgerName))
            throw new ArgumentException("LedgerName is required.");

        var companyId = plan.CompanyId;
        if (companyId <= 0)
        {
            companyId = await ResolveCompanyIdAsync(plan.CompanyName, ct) ?? 0;
            if (companyId <= 0)
                throw new InvalidOperationException($"Company not found in FactoryInfo: {plan.CompanyName}");
        }

        var request = new LedgerSummaryQueryRequest
        {
            CompanyType = 2,
            CompanyId = companyId,
            CompanyName = plan.CompanyName,
            LedgerName = plan.LedgerName.Trim(),
            DateFrom = plan.DateFrom.Date,
            DateTo = plan.DateTo.Date,
            Currency = plan.Currency,
            InterestCal = 0,
        };

        _logger.LogInformation(
            "Executing ledger statement SP for {Ledger} at {Company} {From:d}..{To:d}",
            plan.LedgerName, plan.CompanyName, plan.DateFrom, plan.DateTo);

        var result = await _ledgerSummary.QueryAsync(request);
        var allRows = result.Rows
            .Where(r => !r.IsOpening)
            .Select(MapRow)
            .ToList();

        var total = allRows.Count;
        var capped = allRows.Take(plan.MaxRows).ToList();

        var currencyNote = string.IsNullOrWhiteSpace(plan.Currency) ? "" : $", @Currency='{plan.Currency}'";
        var capNote = total > capped.Count
            ? $" Showing {capped.Count} of {total} voucher rows (chat capped at {plan.MaxRows}). Use Export CSV for the full set."
            : "";
        return new LedgerStatementChatResult
        {
            SqlDescription = $"""
                EXEC sp_ac_LedgerSummary_BankRecoDate
                  @CompanyType=2,
                  @CompanyId={companyId},
                  @LedgerName='{plan.LedgerName}',
                  @DateFrom='{plan.DateFrom:yyyy-MM-dd}',
                  @DateTo='{plan.DateTo:yyyy-MM-dd}'{currencyNote}
                """,
            Warning =
                $"ERP ledger statement (portal parity): {plan.LedgerName} at {plan.CompanyName} from {plan.DateFrom:yyyy-MM-dd} to {plan.DateTo:yyyy-MM-dd}. Opening {result.OpeningBalance:N2}, closing {result.ClosingBalance:N2}.{capNote}",
            Rows = capped,
            TotalCount = total > capped.Count ? total : null,
        };
    }

    private static Dictionary<string, object?> MapRow(LedgerSummaryRowDto row) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Date"] = row.Date?.ToString("yyyy-MM-dd"),
            ["Particulars"] = row.Particulars,
            ["VoucherType"] = row.VoucherType,
            ["VoucherNo"] = row.VoucherNo,
            ["Debit"] = row.Debit,
            ["Credit"] = row.Credit,
            ["Closing"] = row.Closing,
            ["Currency"] = row.Currency,
        };
}
