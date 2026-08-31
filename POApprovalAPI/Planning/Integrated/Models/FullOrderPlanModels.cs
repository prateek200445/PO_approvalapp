using POApprovalAPI.Planning.Execution.Models;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Integrated.Models;

public sealed class FullOrderPlanRequest
{
    public string OrderNo { get; set; } = "";
    public bool ReplaceExistingFibc { get; set; }
}

public sealed class FullOrderPlanResult
{
    public bool Success { get; set; }
    public bool ReadyToConfirm { get; set; }
    public bool Saved { get; set; }
    public string Message { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public PlanningOrderRouteDto? Route { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? FibcStartDate { get; set; }
    public DateTime? FibcEndDate { get; set; }
    public DateTime? FabricRequirementDate { get; set; }
    public DateTime? LoomEndDate { get; set; }
    public DateTime? FabricAtFibcDate { get; set; }
    public bool SequenceOk { get; set; }
    public FibcAllotmentResult? Fibc { get; set; }
    public LoomComponentBatchResult? Loom { get; set; }
    public AccessoryMaterialBoardDto? Accessories { get; set; }
    public IReadOnlyList<string> Blockers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public int LoomRowsInserted { get; set; }
    public int FibcRowsInserted { get; set; }
}
