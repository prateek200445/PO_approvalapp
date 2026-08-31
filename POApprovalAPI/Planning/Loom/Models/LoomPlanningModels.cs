namespace POApprovalAPI.Planning.Loom.Models;

public sealed class LoomPlanningConfigDto
{
    public string DefaultCompanyName { get; set; } = "";
    public bool ReadOnly { get; set; } = true;
    public bool PreviewOnly { get; set; } = true;
    public bool ConfirmSaveEnabled { get; set; }
    public bool ReplaceExistingEnabled { get; set; }
    public int FabricBufferDays { get; set; }
    public int MaxPlanningHorizonDays { get; set; }
    public int MaxDaysPerLoomSegment { get; set; }
    public int MaxChangeoversPerDay { get; set; }
    public double DefaultEfficiency { get; set; }
}

public sealed class LoomMasterDto
{
    public int LoomNo { get; set; }
    public string CompanyName { get; set; } = "";
    public string? LoomCode { get; set; }
    public string? LoomSpecification { get; set; }
    public string? Make { get; set; }
    public string? ModelNo { get; set; }
    public double? MinSize { get; set; }
    public double? MaxSize { get; set; }
    public string? CreelCapacity { get; set; }
    public bool IsFrozen { get; set; }
}

public sealed class LoomAllocationGridItemDto
{
    public int AllocationId { get; set; }
    public int LoomNo { get; set; }
    public string CompanyName { get; set; } = "";
    public string? LoomCode { get; set; }
    public string? LoomSpecification { get; set; }
    public string? PartyName { get; set; }
    public string? OrderNo { get; set; }
    public DateTime AllocationDate { get; set; }
    public DateTime? ToDate { get; set; }
    public double? ReqGsm { get; set; }
    public double? Size { get; set; }
    public string? AllocationType { get; set; }
    public string? Color { get; set; }
    public string? Sector { get; set; }
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
}

public sealed class LoomAllocationGridResult
{
    public IReadOnlyList<LoomAllocationGridItemDto> Items { get; set; } = Array.Empty<LoomAllocationGridItemDto>();
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string CompanyName { get; set; } = "";
    public int TotalRows { get; set; }
    public int ActiveLoomCount { get; set; }
}

public sealed class LoomFabricRequirementDto
{
    public string Customer { get; set; } = "";
    public string FilePoNo { get; set; } = "";
    public string BagType { get; set; } = "";
    public double? Qty { get; set; }
    public DateTime? PoDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Heading { get; set; } = "";
    public string Gsm { get; set; } = "";
    public double? FabricSize { get; set; }
    public double? TotalMtr { get; set; }
    public double? TotalKg { get; set; }
    public string Category { get; set; } = "Other";
    public string PlanningKind { get; set; } = "Other";
    public bool IsLoomEligible { get; set; }
}

public sealed class LoomOrderAllocationLineDto
{
    public int LoomNo { get; set; }
    public string? LoomCode { get; set; }
    public string? PartyName { get; set; }
    public string? OrderNo { get; set; }
    public DateTime AllocationDate { get; set; }
    public DateTime? ToDate { get; set; }
    public double? ReqGsm { get; set; }
    public double? Size { get; set; }
    public string? AllocationType { get; set; }
    public string? Color { get; set; }
    public string? Sector { get; set; }
    public string? Remarks { get; set; }
}

public sealed class LoomOrderPlanDetailDto
{
    public string OrderNo { get; set; } = "";
    public IReadOnlyList<LoomOrderAllocationLineDto> Allocations { get; set; } = Array.Empty<LoomOrderAllocationLineDto>();
    public IReadOnlyList<LoomFabricRequirementDto> FabricRequirements { get; set; } = Array.Empty<LoomFabricRequirementDto>();
}

public sealed class LoomOrderContextDto
{
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public DateTime? DispatchDate { get; set; }
    public double? Quantity { get; set; }
    public string? BagType { get; set; }
    public int ExistingAllocationCount { get; set; }
}
