using System.Data;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Read-only ERP inventory/MIS reports via EXEC stored procedures.
/// </summary>
public class ErpInventoryReportService
{
    private const int SpTimeoutSeconds = 180;
    private readonly DatabaseService _database;
    private readonly ILogger<ErpInventoryReportService> _logger;

    public ErpInventoryReportService(DatabaseService database, ILogger<ErpInventoryReportService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ErpSpReportResult> ExecuteAsync(ErpInventoryReportPlan plan, CancellationToken ct = default)
    {
        return plan.Mode switch
        {
            ErpInventoryReportMode.WarehouseStockSummary => await QueryWarehouseStockSummaryAsync(plan, ct),
            ErpInventoryReportMode.StockSummaryByDept => await QueryStockSummaryByDeptAsync(plan, ct),
            ErpInventoryReportMode.PlantRawMaterialStock => await QueryPlantRawMaterialStockAsync(plan, ct),
            ErpInventoryReportMode.MisReport => await QueryMisReportAsync(plan, ct),
            ErpInventoryReportMode.Top100PurchasedItems => await QueryTop100PurchasedAsync(plan, ct),
            ErpInventoryReportMode.EbidtaPivotSales => await QueryEbidtaPivotAsync(plan, "SP_Sales_EBIDTA_Pivot", ct),
            ErpInventoryReportMode.EbidtaPivotPurchase => await QueryEbidtaPivotAsync(plan, "SP_Purchase_EBIDTA_Pivot", ct),
            ErpInventoryReportMode.AutoRollStock => await QueryParameterlessSpAsync(plan, "sp_Auto_RollStock", "ERP auto roll stock snapshot.", ct),
            ErpInventoryReportMode.AutoFibcStock => await QueryParameterlessSpAsync(plan, "sp_Auto_FIBCStock", "ERP auto FIBC stock snapshot.", ct),
            ErpInventoryReportMode.AutoSmallBagStock => await QueryParameterlessSpAsync(plan, "sp_Auto_SmallBagStock", "ERP auto small-bag stock snapshot.", ct),
            ErpInventoryReportMode.RollItemStock => await QueryParameterlessSpAsync(plan, "SP_Roll_ITEM_Stock", "ERP roll item stock report.", ct),
            ErpInventoryReportMode.SmallBagItemStock => await QueryParameterlessSpAsync(plan, "SP_SmallBag_ITEM_Stock", "ERP small-bag item stock report.", ct),
            ErpInventoryReportMode.StockAnalysisReport => await QueryStockAnalysisAsync(plan, "SP_STOCKANALYSIS_RPT_ALL", ct),
            ErpInventoryReportMode.StockAnalysisDetail => await QueryStockAnalysisAsync(plan, "SP_STOCKANALYSIS_RPT_DTL", ct),
            _ => throw new ArgumentOutOfRangeException(nameof(plan.Mode)),
        };
    }

    public async Task<ErpSpReportResult> QueryWarehouseStockSummaryAsync(ErpInventoryReportPlan plan, CancellationToken ct)
    {
        var table = await RunSpAsync("sp_WarehouseStockSummry", cmd =>
        {
            cmd.Parameters.AddWithValue("@Datefrom", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@Date", plan.DateTo.Date);
            cmd.Parameters.AddWithValue("@Companyname", plan.CompanyName);
            cmd.Parameters.AddWithValue("@deptt", (object?)plan.DeptName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@loginName", DBNull.Value);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_WarehouseStockSummry
              @Datefrom='{plan.DateFrom:yyyy-MM-dd}',
              @Date='{plan.DateTo:yyyy-MM-dd}',
              @Companyname='{plan.CompanyName}'
            """, $"ERP warehouse stock summary for {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryStockSummaryByDeptAsync(ErpInventoryReportPlan plan, CancellationToken ct)
    {
        var table = await RunSpAsync("sp_StockSummry_new", cmd =>
        {
            cmd.Parameters.AddWithValue("@Datefrom", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@Date", plan.DateTo.Date);
            cmd.Parameters.AddWithValue("@Companyname", plan.CompanyName);
            cmd.Parameters.AddWithValue("@deptt", (object?)plan.DeptName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@loginName", DBNull.Value);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_StockSummry_new
              @Datefrom='{plan.DateFrom:yyyy-MM-dd}',
              @Date='{plan.DateTo:yyyy-MM-dd}',
              @Companyname='{plan.CompanyName}'
            """, $"ERP stock summary by department for {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryPlantRawMaterialStockAsync(ErpInventoryReportPlan plan, CancellationToken ct)
    {
        var sp = plan.PlantStockSp;
        var table = await RunSpAsync(sp, cmd =>
        {
            cmd.Parameters.AddWithValue("@CompanyName", plan.CompanyName);
            cmd.Parameters.AddWithValue("@YrFromDate", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@YrToDate", plan.DateTo.Date);
            cmd.Parameters.AddWithValue("@towarehouse", (object?)plan.ToWarehouse ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PlantName", (object?)plan.PlantName ?? DBNull.Value);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC {sp}
              @CompanyName='{plan.CompanyName}',
              @YrFromDate='{plan.DateFrom:yyyy-MM-dd}',
              @YrToDate='{plan.DateTo:yyyy-MM-dd}'
            """, $"ERP plant raw-material stock ({sp}) for {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryMisReportAsync(ErpInventoryReportPlan plan, CancellationToken ct)
    {
        var table = await RunSpAsync("sp_ac_getMISReportData", cmd =>
        {
            cmd.Parameters.AddWithValue("@CompanyName", plan.CompanyName);
            cmd.Parameters.AddWithValue("@DateFrom", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@DateTo", plan.DateTo.Date);
            cmd.Parameters.AddWithValue("@PeriodCount", plan.PeriodCount);
            cmd.Parameters.AddWithValue("@PeriodType", plan.PeriodType);
        }, ct);

        return CapResult(table, plan, $"""
            EXEC sp_ac_getMISReportData
              @CompanyName='{plan.CompanyName}',
              @DateFrom='{plan.DateFrom:yyyy-MM-dd}',
              @DateTo='{plan.DateTo:yyyy-MM-dd}',
              @PeriodCount={plan.PeriodCount},
              @PeriodType={plan.PeriodType}
            """, $"ERP MIS consolidated report for {plan.CompanyName}.");
    }

    public async Task<ErpSpReportResult> QueryTop100PurchasedAsync(ErpInventoryReportPlan plan, CancellationToken ct)
    {
        var table = await RunSpAsync("sp_top100_items_valuewise_stores&spares_purchased", _ => { }, ct);
        return CapResult(table, plan, "EXEC sp_top100_items_valuewise_stores&spares_purchased",
            "ERP top 100 stores/spares items by purchase value (last 1 year).");
    }

    public async Task<ErpSpReportResult> QueryEbidtaPivotAsync(
        ErpInventoryReportPlan plan,
        string spName,
        CancellationToken ct)
    {
        var table = await RunSpAsync(spName, cmd =>
        {
            var tvp = BuildStringArrayTvp(plan.CompanyName);
            var p = cmd.Parameters.Add("@CompanyName", SqlDbType.Structured);
            p.Value = tvp;
            p.TypeName = "dbo.StringArray";
            cmd.Parameters.AddWithValue("@DateFrom", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@DateTo", plan.DateTo.Date);
        }, ct);

        var label = spName.Contains("Purchase", StringComparison.OrdinalIgnoreCase) ? "purchase" : "sales";
        return CapResult(table, plan, $"""
            EXEC {spName}
              @CompanyName=(StringArray '{plan.CompanyName}'),
              @DateFrom='{plan.DateFrom:yyyy-MM-dd}',
              @DateTo='{plan.DateTo:yyyy-MM-dd}'
            """, $"ERP {label} EBIDTA pivot for {plan.CompanyName}.");
    }

    private async Task<ErpSpReportResult> QueryParameterlessSpAsync(
        ErpInventoryReportPlan plan,
        string spName,
        string warnPrefix,
        CancellationToken ct)
    {
        var table = await RunSpAsync(spName, _ => { }, ct);
        return CapResult(table, plan, $"EXEC {spName}", warnPrefix);
    }

    public async Task<ErpSpReportResult> QueryStockAnalysisAsync(
        ErpInventoryReportPlan plan,
        string spName,
        CancellationToken ct)
    {
        const string dataCaveat =
            " CAUTION: opening/closing balances may be wrong per STOCK_ANALYSIS_ISSUES_REPORT.md (stale opening snapshots, MRN/warehouse double-count risk) — validate against WareHouse.StkInHand before acting on numbers.";

        var table = await RunSpAsync(spName, cmd =>
        {
            var tvp = BuildStringArrayTvp(plan.CompanyName);
            var p = cmd.Parameters.Add("@companyname", SqlDbType.Structured);
            p.Value = tvp;
            p.TypeName = "dbo.StringArray";
            cmd.Parameters.AddWithValue("@DATEFROM", plan.DateFrom.Date);
            cmd.Parameters.AddWithValue("@DATEto", plan.DateTo.Date);
            if (spName.Equals("SP_STOCKANALYSIS_RPT_ALL", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@RptType", plan.ReportType);
                cmd.Parameters.AddWithValue("@intOp", plan.IntOp);
            }
        }, ct);

        var rptNote = plan.ReportType switch
        {
            1 => " (item-wise)",
            2 => " (date-wise)",
            _ => " (summary)",
        };

        return CapResult(table, plan, $"""
            EXEC {spName}
              @companyname=(StringArray '{plan.CompanyName}'),
              @DATEFROM='{plan.DateFrom:yyyy-MM-dd}',
              @DATEto='{plan.DateTo:yyyy-MM-dd}',
              @RptType={plan.ReportType},
              @intOp={plan.IntOp}
            """, $"ERP stock analysis report{rptNote} for {plan.CompanyName}.{dataCaveat}");
    }

    private static DataTable BuildStringArrayTvp(string value)
    {
        var tvp = new DataTable();
        tvp.Columns.Add("StringValue", typeof(string));
        tvp.Rows.Add(value);
        return tvp;
    }

    private ErpSpReportResult CapResult(
        DataTable table,
        ErpInventoryReportPlan plan,
        string sqlDesc,
        string warnPrefix)
    {
        var allRows = ErpFinanceReportService.DataTableToRows(table);
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

        _logger.LogInformation("Executing inventory SP {Sp}", spName);
        var table = new DataTable();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        table.Load(reader);
        return table;
    }
}
