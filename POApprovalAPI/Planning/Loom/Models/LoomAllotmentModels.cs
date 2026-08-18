namespace POApprovalAPI.Planning.Loom.Models;

public sealed class LoomAllotmentRequest
{
    public string OrderNo { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? PartyName { get; set; }
    public string? Heading { get; set; }
    public double ReqGsm { get; set; }
    public double Size { get; set; }
    public double RequiredMeters { get; set; }
    /// <summary>When set, fabric must complete by this date (FIBC requirement). Engine subtracts FabricBufferDays.</summary>
    public DateTime? FabricRequirementDate { get; set; }
    public string? Color { get; set; }
    public string? Sector { get; set; }
    public bool ReplaceExisting { get; set; }
}

public sealed class LoomProposedSegmentDto
{
    public int LoomNo { get; set; }
    public string? LoomCode { get; set; }
    public string? LoomSpecification { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public double PlannedMeters { get; set; }
    public double MetersPerDay { get; set; }
    public int RunDays { get; set; }
    public string AllotmentCase { get; set; } = "";
    public string CaseLabel { get; set; } = "";
    public int? FormulaId { get; set; }
    public double ReqGsm { get; set; }
    public double Size { get; set; }
}

public sealed class LoomOrderShiftDisplacementDto
{
    public int? AllocationId { get; set; }
    public int LoomNo { get; set; }
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime NewFromDate { get; set; }
    public DateTime NewToDate { get; set; }
    public string Reason { get; set; } = "";
}

public class LoomAllotmentResult
{
    public bool Success { get; set; }
    public bool FullyAllotted { get; set; }
    public string Message { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public double ReqGsm { get; set; }
    public double Size { get; set; }
    public double RequiredMeters { get; set; }
    public double AllottedMeters { get; set; }
    public double MetersPerDay { get; set; }
    public int FabricBufferDays { get; set; }
    public DateTime? FabricRequirementDate { get; set; }
    public DateTime? FabricCompletionDate { get; set; }
    public DateTime? EarliestStartDate { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyList<LoomProposedSegmentDto> ProposedSegments { get; set; } = Array.Empty<LoomProposedSegmentDto>();
    public IReadOnlyList<LoomOrderShiftDisplacementDto> Displacements { get; set; } = Array.Empty<LoomOrderShiftDisplacementDto>();
}

public sealed class LoomAllotmentConfirmResult : LoomAllotmentResult
{
    public bool Saved { get; set; }
    public int RowsInserted { get; set; }
    public int RowsDeleted { get; set; }
    public int OrdersShifted { get; set; }
}

public sealed class LoomOrderAllotmentContextDto
{
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? FabricRequirementDate { get; set; }
    public double? Quantity { get; set; }
    public string? BagType { get; set; }
    public int ExistingAllocationCount { get; set; }
    public IReadOnlyList<LoomFabricRequirementDto> FabricLines { get; set; } = Array.Empty<LoomFabricRequirementDto>();
}

public sealed class LoomPpmSpecDto
{
    public string LoomType { get; set; } = "";
    public double GsmFrom { get; set; }
    public double GsmTo { get; set; }
    public double WidthFrom { get; set; }
    public double WidthTo { get; set; }
    public double Ppm { get; set; }
}

public sealed class LoomFormulaDto
{
    public int FormulaId { get; set; }
    public double Size { get; set; }
    public double? WarpMesh { get; set; }
    public double? WeftMesh { get; set; }
    public string? FormulaName { get; set; }
}

public sealed class LoomProductionMeterDto
{
    public int LoomNo { get; set; }
    public string? LoomCode { get; set; }
    public DateTime PlanDate { get; set; }
    public double ProdMetersA { get; set; }
    public double ProdMetersB { get; set; }
    public double? ReqGsm { get; set; }
    public double? Size { get; set; }
    public string? OrderNo { get; set; }
    public string? PartyName { get; set; }
}

public sealed class LoomProductionMeterGridResult
{
    public IReadOnlyList<LoomProductionMeterDto> Items { get; set; } = Array.Empty<LoomProductionMeterDto>();
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string CompanyName { get; set; } = "";
}
