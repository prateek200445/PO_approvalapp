using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Integrated.Models;

public sealed class IntegratedTimelineMilestoneDto
{
    public string Stage { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Detail { get; set; }
    public int SortOrder { get; set; }
}

public sealed class IntegratedOrderTimelineDto
{
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public string? BagType { get; set; }
    public double? Quantity { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? FabricRequirementDate { get; set; }
    public DateTime? LoomStartDate { get; set; }
    public DateTime? LoomEndDate { get; set; }
    public DateTime? TransferStartDate { get; set; }
    public DateTime? TransferEndDate { get; set; }
    public DateTime? FibcStartDate { get; set; }
    public DateTime? FibcEndDate { get; set; }
    public int FabricBufferDays { get; set; }
    public int TransferBufferDays { get; set; }
    public string? FibcCompanyName { get; set; }
    public string? FabricSupplyCompanyName { get; set; }
    public bool IsInterUnit { get; set; }
    public string? RouteSource { get; set; }
    public IReadOnlyList<IntegratedTimelineMilestoneDto> Milestones { get; set; } = Array.Empty<IntegratedTimelineMilestoneDto>();
    public IReadOnlyList<LoomOrderAllocationLineDto> LoomAllocations { get; set; } = Array.Empty<LoomOrderAllocationLineDto>();
    public IReadOnlyList<FibcFabricRequirementDto> FabricRequirements { get; set; } = Array.Empty<FibcFabricRequirementDto>();
    public IReadOnlyList<FibcOrderPlanLineDto> FibcPlanLines { get; set; } = Array.Empty<FibcOrderPlanLineDto>();
    public IReadOnlyList<IntegratedBomComponentDto> BomComponents { get; set; } = Array.Empty<IntegratedBomComponentDto>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public sealed class IntegratedBomComponentDto
{
    public string Heading { get; set; } = "";
    public string Category { get; set; } = "Other";
    public string PlanningKind { get; set; } = "Other";
    public bool IsLoomEligible { get; set; }
    public string Gsm { get; set; } = "";
    public double? FabricSize { get; set; }
    public double? TotalMtr { get; set; }
    public double? TotalKg { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? SupplyCompanyName { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsInterUnit { get; set; }
    public int TransferBufferDays { get; set; }
    public string Readiness { get; set; } = "Unplanned";
    public string? Detail { get; set; }
    public string? MaterialStatus { get; set; }
    public string? IndentNo { get; set; }
    public double ReceivedQty { get; set; }
}
