namespace POApprovalAPI.Planning.Fibc.Models;

public sealed class FibcPlanningConfigDto
{
    public string DefaultCompanyName { get; set; } = "";
    public int DispatchBufferDays { get; set; }
    public IReadOnlyList<string> ShiftPreference { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveShifts { get; set; } = Array.Empty<string>();
    public bool AllotmentEnabled { get; set; }
    public bool PreviewOnly { get; set; } = true;
    public bool ConfirmSaveEnabled { get; set; }
    public bool ReplaceExistingEnabled { get; set; }
    public bool QuotationHoldEnabled { get; set; }
    public int QuotationHoldDays { get; set; }
    public bool QuotationHoldEmailEnabled { get; set; }
    public bool CriticalShiftEnabled { get; set; }
}

public sealed class FibcLineConfigDto
{
    public int LineNo { get; set; }
    public string CompanyName { get; set; } = "";
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public bool IsDoubleDust { get; set; }
    public bool IsTripleDust { get; set; }
    public int BagCapacity { get; set; }
    public int SortOrder { get; set; }
    public int BufferDaysCheck { get; set; }
}

public sealed class FibcSlotGridItemDto
{
    public string CompanyName { get; set; } = "";
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public string? PartyName { get; set; }
    public string? OrderNo { get; set; }
    public string LineNo { get; set; } = "";
    public DateTime PlanDate { get; set; }
    public double Allotted { get; set; }
    public double Capacity { get; set; }
    public double Remaining { get; set; }
    public double? AllocatedPercent { get; set; }
    public string Shift { get; set; } = "";
    public string? MarketingNo { get; set; }
    public int? TransId { get; set; }
    public double? Efficiency { get; set; }
    public double UtilizationPercent { get; set; }
    public string OccupancyStatus { get; set; } = "free";
}

public sealed class FibcSlotGridResult
{
    public IReadOnlyList<FibcSlotGridItemDto> Items { get; set; } = Array.Empty<FibcSlotGridItemDto>();
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string CompanyName { get; set; } = "";
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
}

public sealed class FibcOrderPlanLineDto
{
    public string CompanyName { get; set; } = "";
    public string LineNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? OrderNo { get; set; }
    public double? PoQty { get; set; }
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public double Qty { get; set; }
    public DateTime PlanDate { get; set; }
    public string Shift { get; set; } = "";
    public double? AllocatedPercent { get; set; }
}

public sealed class FibcFabricRequirementDto
{
    public string Customer { get; set; } = "";
    public string FilePoNo { get; set; } = "";
    public string BagType { get; set; } = "";
    public string? Qty { get; set; }
    public DateTime? PoDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Heading { get; set; } = "";
    public string Gsm { get; set; } = "";
    public double? FabricSize { get; set; }
    public double? TotalMtr { get; set; }
    public double? TotalKg { get; set; }
}

public sealed class FibcOrderPlanDetailDto
{
    public string OrderNo { get; set; } = "";
    public IReadOnlyList<FibcOrderPlanLineDto> PlanLines { get; set; } = Array.Empty<FibcOrderPlanLineDto>();
    public IReadOnlyList<FibcOrderPlanLineDto> SavedAllocations { get; set; } = Array.Empty<FibcOrderPlanLineDto>();
    public IReadOnlyList<FibcFabricRequirementDto> FabricRequirements { get; set; } = Array.Empty<FibcFabricRequirementDto>();
}

public sealed class FibcAllotmentRequest
{
    public string OrderNo { get; set; } = "";
    public string? CompanyName { get; set; }
    public DateTime? DispatchDate { get; set; }
    public double Quantity { get; set; }
    public string? BagType { get; set; }
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public bool ReplaceExisting { get; set; }
}

public sealed class FibcOrderAllotmentContextDto
{
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public DateTime? DispatchDate { get; set; }
    public double? Quantity { get; set; }
    public string? BagType { get; set; }
    public string BagTypeLabel { get; set; } = "";
    public int ExistingAllocationCount { get; set; }
}

public class FibcAllotmentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public double Quantity { get; set; }
    public double CapacityPerShift { get; set; }
    public double SlotsRequired { get; set; }
    public int BufferDays { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyList<FibcSlotGridItemDto> ProposedSlots { get; set; } = Array.Empty<FibcSlotGridItemDto>();
}

public sealed class FibcAllotmentConfirmResult : FibcAllotmentResult
{
    public bool Saved { get; set; }
    public int RowsInserted { get; set; }
}
