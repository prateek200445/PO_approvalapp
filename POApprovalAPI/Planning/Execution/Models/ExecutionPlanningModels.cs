namespace POApprovalAPI.Planning.Execution.Models;

public sealed class OrderExecutionSummaryDto
{
    public string OrderNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public double PlannedQty { get; set; }
    public double ProducedQty { get; set; }
    public double BailedQty { get; set; }
    public double PendingQty { get; set; }
    public double BailingGap { get; set; }
    public IReadOnlyList<OrderExecutionLineDto> Lines { get; set; } = Array.Empty<OrderExecutionLineDto>();
    public IReadOnlyList<OrderExecutionLineShiftSummaryDto> LineShiftTotals { get; set; } =
        Array.Empty<OrderExecutionLineShiftSummaryDto>();
    public IReadOnlyList<OrderProductionEntryDto> ProductionEntries { get; set; } =
        Array.Empty<OrderProductionEntryDto>();
    public IReadOnlyList<string> ReplanSuggestions { get; set; } = Array.Empty<string>();
    public int BacklogRowsAutoCleared { get; set; }
}

public sealed class OrderExecutionLineShiftSummaryDto
{
    public int LineNo { get; set; }
    public string Shift { get; set; } = "";
    public double PlannedQty { get; set; }
    public double ProducedQty { get; set; }
    public double PendingQty { get; set; }
}

public sealed class OrderProductionEntryDto
{
    public int LineNo { get; set; }
    public string TeamNo { get; set; } = "";
    public string Shift { get; set; } = "";
    public DateTime ProdDate { get; set; }
    public double Quantity { get; set; }
}

public sealed class OrderExecutionLineDto
{
    public int LineNo { get; set; }
    public string Shift { get; set; } = "";
    public DateTime? PlanDate { get; set; }
    public double PlannedQty { get; set; }
    public double ProducedQty { get; set; }
    public double BailedQty { get; set; }
    public double OpenBacklogQty { get; set; }
}

public sealed class FactoryExecutionBoardDto
{
    public string CompanyName { get; set; } = "";
    public DateTime BoardDate { get; set; }
    public IReadOnlyList<FactoryExecutionRowDto> Rows { get; set; } = Array.Empty<FactoryExecutionRowDto>();
}

public sealed class FactoryExecutionRowDto
{
    public int LineNo { get; set; }
    public string Shift { get; set; } = "";
    public double PlannedQty { get; set; }
    public double ProducedQty { get; set; }
    public double OpenBacklogQty { get; set; }
    public double CapacityGap { get; set; }
}

public sealed class BailingReconciliationDto
{
    public string OrderNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public double PlannedQty { get; set; }
    public double BailedQty { get; set; }
    public double Shortfall { get; set; }
    public bool ReadyForDispatch { get; set; }
    public string Message { get; set; } = "";
}

public sealed class AccessoryMaterialStatusDto
{
    public string Heading { get; set; } = "";
    public string Category { get; set; } = "";
    public double? RequiredQty { get; set; }
    public string Unit { get; set; } = "";
    public string Status { get; set; } = "NotFound";
    public string? IndentNo { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDesc { get; set; }
    public double? IndentQty { get; set; }
    public string? MrnNo { get; set; }
    public double ReceivedQty { get; set; }
    public double PendingQty { get; set; }
    public string? CompanyName { get; set; }
    public string? Detail { get; set; }
}

public sealed class AccessoryMaterialBoardDto
{
    public string OrderNo { get; set; } = "";
    public DateTime? DispatchDate { get; set; }
    public IReadOnlyList<AccessoryMaterialStatusDto> Items { get; set; } = Array.Empty<AccessoryMaterialStatusDto>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}
