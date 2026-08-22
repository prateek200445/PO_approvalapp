using System.Data;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Read-only ERP finance/inventory reports via EXEC stored procedures.
/// </summary>
public class ErpFinanceReportService
{
    private const int SpTimeoutSeconds = 180;
    private readonly DatabaseService _database;
    private readonly ILogger<ErpFinanceReportService> _logger;

    public ErpFinanceReportService(DatabaseService database, ILogger<ErpFinanceReportService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ErpSpReportResult> ExecuteAsync(ErpFinanceReportPlan plan, CancellationToken ct = default)
    {
        return plan.Mode switch
        {
            ErpFinanceReportMode.StockAgeing => await QueryStockAgeingAsync(plan, ct),
            ErpFinanceReportMode.GroupOverdueDays => await QueryGroupOverdueDaysAsync(plan, ct),
            ErpFinanceReportMode.OutstandingAll => await QueryOutstandingAllAsync(plan, ct),
            ErpFinanceReportMode.MsmeOverdue => await QueryMsmeOverdueAsync(plan, ct),
            ErpFinanceReportMode.SalesDiscount => await QuerySalesDiscountAsync(plan, ct),
            ErpFinanceReportMode.ExportDebtorsLast3Months => await QueryExportDebtorsLast3MonthsAsync(plan, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(plan.Mode)),
        };
    }

    public async Task<ErpSpReportResult> QueryStockAgeingAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        var sp = plan.StockAgeingSp;
        var tried = new List<string>();
        var merged = new List<Dictionary<string, object?>>();

        async Task RunOneAsync(string? subgroup)
        {
            var table = await RunSpAsync(sp, cmd =>
            {
                cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
                cmd.Parameters.AddWithValue("@subgroupname", (object?)subgroup ?? DBNull.Value);
            }, ct);
            merged.AddRange(DataTableToRows(table));
            tried.Add(subgroup is null ? "NULL" : $"'{subgroup}'");
        }

        await RunOneAsync(plan.SubGroupName);
        if (merged.Count == 0 && string.IsNullOrWhiteSpace(plan.SubGroupName))
        {
            var subgroups = await QueryInventorySubGroupsAsync(plan.CompanyName, plan.GroupName, ct);
            foreach (var sg in subgroups)
            {
                if (merged.Count >= plan.MaxRows) break;
                await RunOneAsync(sg);
            }
        }

        var sqlDesc = $"""
            EXEC {sp}
              @companyname='{plan.CompanyName}',
              @subgroupname={(plan.SubGroupName is null ? "NULL" : $"'{plan.SubGroupName}'")}{(tried.Count > 1 ? $" (+ retried {tried.Count - 1} subgroup(s): {string.Join(", ", tried.Skip(1))})" : "")}
            """;
        var warnPrefix = merged.Count > 0 && tried.Count > 1
            ? $"ERP inventory/stock ageing ({sp}) for {plan.CompanyName} — tried {tried.Count} subgroup filter(s)."
            : $"ERP inventory/stock ageing ({sp}) for {plan.CompanyName}.";

        return CapResultFromRows(merged, plan, sqlDesc, warnPrefix);
    }

    private async Task<List<string>> QueryInventorySubGroupsAsync(
        string companyName,
        string? groupName,
        CancellationToken ct)
    {
        var filters = new List<string>
        {
            "CompanyName = @company",
            "ISNULL(StkInHand, 0) > 0",
            "ISNULL(SubGroupName, '') <> ''",
        };
        if (!string.IsNullOrWhiteSpace(groupName))
            filters.Add("(GroupName = @group OR Deptt LIKE @groupLike)");

        var sql = $"""
            SELECT DISTINCT TOP 15 SubGroupName
            FROM vw_inventoryitemwarehouse_all WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY SubGroupName
            """;

        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = SpTimeoutSeconds;
        cmd.Parameters.AddWithValue("@company", companyName);
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            cmd.Parameters.AddWithValue("@group", groupName);
            cmd.Parameters.AddWithValue("@groupLike", $"%{groupName}%");
        }

        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var sg = reader["SubGroupName"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(sg)) list.Add(sg);
        }

        return list;
    }

    public async Task<ErpSpReportResult> QueryGroupOverdueDaysAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        var group = plan.GroupName ?? "Sundry Debtors";
        var table = await RunSpAsync("sp_Overdue_Group_Days", cmd =>
        {
            cmd.Parameters.AddWithValue("@DateTo", plan.ToDate.Date);
            cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
            cmd.Parameters.AddWithValue("@username", DBNull.Value);
            cmd.Parameters.AddWithValue("@GroupName", group);
            cmd.Parameters.AddWithValue("@Type", DBNull.Value);
            cmd.Parameters.AddWithValue("@days", plan.Days);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_Overdue_Group_Days
              @DateTo='{plan.ToDate:yyyy-MM-dd}',
              @companyname='{plan.CompanyName}',
              @GroupName='{group}',
              @days={plan.Days}
            """, $"ERP group overdue ({plan.Days}-day buckets) for {group} at {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryMsmeOverdueAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plan.LedgerName))
            throw new ArgumentException("LedgerName required for MSME overdue.");

        var table = await RunSpAsync("sp_Overdue_Ledger_MSME", cmd =>
        {
            cmd.Parameters.AddWithValue("@DateTo", plan.ToDate.Date);
            cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
            cmd.Parameters.AddWithValue("@ledgername", plan.LedgerName.Trim());
            cmd.Parameters.AddWithValue("@Currency", plan.Currency);
            cmd.Parameters.AddWithValue("@Category", DBNull.Value);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_Overdue_Ledger_MSME
              @DateTo='{plan.ToDate:yyyy-MM-dd}',
              @companyname='{plan.CompanyName}',
              @ledgername='{plan.LedgerName}',
              @Currency='{plan.Currency}'
            """, $"ERP MSME overdue for {plan.LedgerName} at {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryOutstandingAllAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        var group = plan.GroupName ?? "Sundry Debtors";
        var table = await RunSpAsync("sp_OutstandingAll", cmd =>
        {
            cmd.Parameters.AddWithValue("@DateTo", plan.ToDate.Date);
            cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
            cmd.Parameters.AddWithValue("@Months", plan.PeriodMonths);
            cmd.Parameters.AddWithValue("@GroupName", group);
            cmd.Parameters.AddWithValue("@Type", DBNull.Value);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_OutstandingAll
              @DateTo='{plan.ToDate:yyyy-MM-dd}',
              @companyname='{plan.CompanyName}',
              @Months={plan.PeriodMonths},
              @GroupName='{group}'
            """, $"ERP all-party outstanding pivot for {group} at {plan.CompanyName} ({plan.PeriodMonths}-month buckets).", sortByTotal: true);
    }

    public async Task<ErpSpReportResult> QuerySalesDiscountAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        if (plan.SalesDiscountSp.Equals("sp_salesdiscount_customer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(plan.CustomerName))
                throw new ArgumentException("CustomerName required for sp_salesdiscount_customer.");

            var table = await RunSpAsync("sp_salesdiscount_customer", cmd =>
            {
                cmd.Parameters.AddWithValue("@customername", plan.CustomerName.Trim());
            }, ct);

            return CapResult(table, plan, $"""
                EXEC sp_salesdiscount_customer @customername='{plan.CustomerName}'
                """, $"ERP sales discount report for customer {plan.CustomerName}.");
        }

        var companyTable = await RunSpAsync("sp_salesdiscount_companyname", cmd =>
        {
            cmd.Parameters.AddWithValue("@companyname", plan.CompanyName);
        }, ct);

        return CapResult(companyTable, plan, $"""
            EXEC sp_salesdiscount_companyname @companyname='{plan.CompanyName}'
            """, $"ERP sales discount report for {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryExportDebtorsLast3MonthsAsync(ErpFinanceReportPlan plan, CancellationToken ct)
    {
        var group = plan.GroupCompany ?? plan.CompanyName;
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("GroupCompany required for sp_Export_Debtors_Last3Months.");

        var table = await RunSpAsync("sp_Export_Debtors_Last3Months", cmd =>
        {
            cmd.Parameters.AddWithValue("@GroupCompany", group.Trim());
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_Export_Debtors_Last3Months @GroupCompany='{group}'
            """, $"ERP export debtors invoice totals (last 3 months) for group {group} — Debtors-Overseas buyers only.");
    }

    private ErpSpReportResult CapResult(
        DataTable table,
        ErpFinanceReportPlan plan,
        string sqlDesc,
        string warnPrefix,
        bool sortByTotal = false)
    {
        var allRows = DataTableToRows(table);
        return CapResultFromRows(allRows, plan, sqlDesc, warnPrefix, sortByTotal);
    }

    private ErpSpReportResult CapResultFromRows(
        List<Dictionary<string, object?>> allRows,
        ErpFinanceReportPlan plan,
        string sqlDesc,
        string warnPrefix,
        bool sortByTotal = false)
    {
        if (sortByTotal)
            allRows = SortByTotalDesc(allRows);
        var total = allRows.Count;
        var capped = allRows.Take(plan.MaxRows).ToList();
        var capNote = total > capped.Count
            ? $" Showing {capped.Count} of {total} rows (chat capped at {plan.MaxRows})."
            : "";
        return new ErpSpReportResult
        {
            SqlDescription = sqlDesc,
            Warning = warnPrefix + capNote,
            Rows = capped,
            TotalCount = total > capped.Count ? total : null,
        };
    }

    private async Task<DataTable> RunSpAsync(string spName, Action<SqlCommand> bind, CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = SpTimeoutSeconds;
        bind(cmd);

        _logger.LogInformation("Executing finance SP {Sp}", spName);
        var table = new DataTable();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        table.Load(reader);
        return table;
    }

    internal static List<Dictionary<string, object?>> DataTableToRows(DataTable table)
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
        if (rows.Count == 0 || !rows[0].ContainsKey("Total")) return rows;
        return rows
            .OrderByDescending(r =>
            {
                if (r.TryGetValue("Total", out var v) && v is not null
                    && double.TryParse(v.ToString(), out var parsed))
                    return parsed;
                return 0d;
            })
            .ToList();
    }
}
