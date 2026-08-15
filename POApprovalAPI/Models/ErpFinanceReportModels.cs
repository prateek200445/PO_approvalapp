namespace POApprovalAPI.Models;

public enum ErpFinanceReportMode
{
    StockAgeing,
    GroupOverdueDays,
    OutstandingAll,
    MsmeOverdue,
    SalesDiscount,
    ExportDebtorsLast3Months,
}

public class ErpFinanceReportPlan
{
    public ErpFinanceReportMode Mode { get; set; }
    public string CompanyName { get; set; } = "";
    public DateTime ToDate { get; set; }
    public string? SubGroupName { get; set; }
    public string? GroupName { get; set; }
    public string? LedgerName { get; set; }
    public string? CustomerName { get; set; }
    public int Days { get; set; } = 90;
    public int PeriodMonths { get; set; } = 3;
    public string Currency { get; set; } = "Rs.";
    public string StockAgeingSp { get; set; } = "sp_Agingreport_SubgroupName";
    public string SalesDiscountSp { get; set; } = "sp_salesdiscount_companyname";
    public string? GroupCompany { get; set; }
    public int MaxRows { get; set; } = 50;
}

public class ErpSpReportResult
{
    public string SqlDescription { get; set; } = "";
    public string Warning { get; set; } = "";
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int? TotalCount { get; set; }
}
