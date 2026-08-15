namespace POApprovalAPI.Models;

public enum ErpInventoryReportMode
{
    WarehouseStockSummary,
    StockSummaryByDept,
    PlantRawMaterialStock,
    MisReport,
    Top100PurchasedItems,
    EbidtaPivotSales,
    EbidtaPivotPurchase,
    AutoRollStock,
    AutoFibcStock,
    AutoSmallBagStock,
    RollItemStock,
    SmallBagItemStock,
    StockAnalysisReport,
    StockAnalysisDetail,
}

public class ErpInventoryReportPlan
{
    public ErpInventoryReportMode Mode { get; set; }
    public string CompanyName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? DeptName { get; set; }
    public string? PlantName { get; set; }
    public string? ToWarehouse { get; set; }
    public string PlantStockSp { get; set; } = "sp_Prod_GetRowMaterialStock_Loom";
    public int PeriodCount { get; set; } = 12;
    public int PeriodType { get; set; } = 1;
    public int ReportType { get; set; }
    public int IntOp { get; set; }
    public int MaxRows { get; set; } = 50;
}
