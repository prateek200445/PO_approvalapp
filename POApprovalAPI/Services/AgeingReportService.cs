using System.Data;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Portal-parity debtor/creditor ageing via ERP stored procedures (EXEC — not LLM SQL).
/// </summary>
public class AgeingReportService
{
    private const int SpTimeoutSeconds = 180;
    private readonly DatabaseService _database;
    private readonly ILogger<AgeingReportService> _logger;

    public AgeingReportService(DatabaseService database, ILogger<AgeingReportService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<AgeingReportResult> ExecuteAsync(AgeingReportPlan plan, CancellationToken ct = default)
    {
        return plan.Mode switch
        {
            AgeingReportMode.PartyOverdue => await QueryPartyOverdueAsync(plan, ct),
            AgeingReportMode.PartySummary => await QueryPartySummaryAsync(plan, ct),
            _ => await QueryGroupPivotAsync(plan, ct),
        };
    }

    public async Task<AgeingReportResult> QueryGroupPivotAsync(AgeingReportPlan plan, CancellationToken ct)
    {
        var table = await RunSpAsync(
            "sp_Representative_Outstanding_Pivot",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@CompanyName", plan.CompanyName);
                cmd.Parameters.AddWithValue("@ToDate", plan.ToDate.Date);
                cmd.Parameters.AddWithValue("@intPeriod", plan.PeriodMonths);
                cmd.Parameters.AddWithValue("@Representive", DBNull.Value);
                cmd.Parameters.AddWithValue("@Currency", plan.Currency);
                cmd.Parameters.AddWithValue("@G3", plan.G3);
                cmd.Parameters.AddWithValue("@G4", (object?)plan.G4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsLedger", 1);
            },
            ct);

        var allRows = DataTableToRows(table);
        var sorted = SortByTotalDesc(allRows);
        var total = sorted.Count;
        var capped = sorted.Take(plan.MaxRows).ToList();

        var g4Note = string.IsNullOrWhiteSpace(plan.G4) ? "" : $", sub-group '{plan.G4}'";
        return new AgeingReportResult
        {
            SqlDescription = $"""
                EXEC sp_Representative_Outstanding_Pivot
                  @CompanyName='{plan.CompanyName}',
                  @ToDate='{plan.ToDate:yyyy-MM-dd}',
                  @intPeriod={plan.PeriodMonths},
                  @G3='{plan.G3}'{g4Note},
                  @IsLedger=1
                """,
            Warning =
                $"ERP ageing pivot (portal parity): {plan.G3}{g4Note} for {plan.CompanyName} as on {plan.ToDate:yyyy-MM-dd}, monthly buckets.",
            Rows = capped,
            TotalCount = total > capped.Count ? total : null,
        };
    }

    public async Task<AgeingReportResult> QueryPartyOverdueAsync(AgeingReportPlan plan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plan.LedgerName))
            throw new ArgumentException("LedgerName is required for party overdue ageing.");

        var table = await RunSpAsync(
            "sp_Overdue_Ledger",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@DateTo", plan.ToDate.Date);
                cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
                cmd.Parameters.AddWithValue("@ledgername", plan.LedgerName.Trim());
                cmd.Parameters.AddWithValue("@Currency", plan.Currency);
                cmd.Parameters.AddWithValue("@Representative", DBNull.Value);
                cmd.Parameters.AddWithValue("@IncludeZero", 0);
                cmd.Parameters.AddWithValue("@Category", DBNull.Value);
                cmd.Parameters.AddWithValue("@BankName", 0);
                cmd.Parameters.AddWithValue("@LastTransaction", 0);
                cmd.Parameters.AddWithValue("@PaymentDays", 0);
                cmd.Parameters.AddWithValue("@VoucherNo", 0);
            },
            ct);

        var allRows = DataTableToRows(table);
        var total = allRows.Count;
        var capped = allRows.Take(plan.MaxRows).ToList();

        return new AgeingReportResult
        {
            SqlDescription = $"""
                EXEC sp_Overdue_Ledger
                  @DateTo='{plan.ToDate:yyyy-MM-dd}',
                  @companyname='{plan.CompanyName}',
                  @ledgername='{plan.LedgerName}',
                  @Currency='{plan.Currency}'
                """,
            Warning =
                $"ERP bill-wise overdue ageing for {plan.LedgerName} at {plan.CompanyName} as on {plan.ToDate:yyyy-MM-dd} (sp_Overdue_Ledger).",
            Rows = capped,
            TotalCount = total > capped.Count ? total : null,
        };
    }

    public async Task<AgeingReportResult> QueryPartySummaryAsync(AgeingReportPlan plan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plan.LedgerName))
            throw new ArgumentException("LedgerName is required for party ageing summary.");

        var table = await RunSpAsync(
            "sp_Overdue_Ledger_SUMMARY",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@DateTo", plan.ToDate.Date);
                cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
                cmd.Parameters.AddWithValue("@ledgername", plan.LedgerName.Trim());
                cmd.Parameters.AddWithValue("@Currency", plan.Currency);
                cmd.Parameters.AddWithValue("@Representative", DBNull.Value);
                cmd.Parameters.AddWithValue("@IncludeZero", 0);
                cmd.Parameters.AddWithValue("@Category", DBNull.Value);
                cmd.Parameters.AddWithValue("@BankName", 0);
                cmd.Parameters.AddWithValue("@LastTransaction", 0);
                cmd.Parameters.AddWithValue("@PaymentDays", 0);
                cmd.Parameters.AddWithValue("@VoucherNo", 0);
            },
            ct);

        var rows = DataTableToRows(table);
        return new AgeingReportResult
        {
            SqlDescription = $"""
                EXEC sp_Overdue_Ledger_SUMMARY
                  @DateTo='{plan.ToDate:yyyy-MM-dd}',
                  @companyname='{plan.CompanyName}',
                  @ledgername='{plan.LedgerName}'
                """,
            Warning =
                $"ERP overdue summary by currency for {plan.LedgerName} at {plan.CompanyName} (sp_Overdue_Ledger_SUMMARY).",
            Rows = rows,
            TotalCount = rows.Count,
        };
    }

    private async Task<DataTable> RunSpAsync(
        string spName,
        Action<SqlCommand> bind,
        CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = SpTimeoutSeconds;
        bind(cmd);

        _logger.LogInformation("Executing ageing SP {Sp}", spName);
        var table = new DataTable();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        table.Load(reader);
        return table;
    }

    private static List<Dictionary<string, object?>> DataTableToRows(DataTable table)
    {
        var list = new List<Dictionary<string, object?>>(table.Rows.Count);
        var names = new string[table.Columns.Count];
        for (var i = 0; i < table.Columns.Count; i++)
        {
            var raw = table.Columns[i].ColumnName;
            names[i] = string.IsNullOrWhiteSpace(raw) ? $"Col{i}" : raw.Trim();
        }

        foreach (DataRow dr in table.Rows)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var val = dr[i];
                if (val is DBNull) val = null;
                else if (val is DateTime dt) val = dt.ToString("yyyy-MM-dd");
                row[names[i]] = val;
            }
            list.Add(row);
        }

        return list;
    }

    private static List<Dictionary<string, object?>> SortByTotalDesc(List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return rows;
        if (!rows[0].ContainsKey("Total")) return rows;

        return rows
            .OrderByDescending(r =>
            {
                if (r.TryGetValue("Total", out var v) && v is not null)
                {
                    if (v is double d) return d;
                    if (v is float f) return f;
                    if (v is decimal m) return (double)m;
                    if (double.TryParse(v.ToString(), out var parsed)) return parsed;
                }
                return 0d;
            })
            .ToList();
    }
}
