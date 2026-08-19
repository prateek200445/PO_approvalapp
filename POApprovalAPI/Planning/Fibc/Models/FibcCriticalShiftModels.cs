namespace POApprovalAPI.Planning.Fibc.Models;

public sealed class FibcCriticalShiftRequest
{
    public string OrderNo { get; set; } = "";
    public string? CompanyName { get; set; }
    public DateTime? DispatchDate { get; set; }
    public double Quantity { get; set; }
    public string? BagType { get; set; }
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public bool ReplaceExisting { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// When true, critical allotment must use target completion date slots only (no earlier days).
    /// Optional — not default production behaviour; useful for testing displacement scenarios.
    /// </summary>
    public bool PinToTargetDate { get; set; }

    /// <summary>OrderWise (default) or SlotWise — matches standard FIBC planner.</summary>
    public string? AllotmentMode { get; set; }

    public string? DustLevel { get; set; }
}

public sealed class FibcOrderShiftDisplacementDto
{
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public string FromLineNo { get; set; } = "";
    public DateTime FromPlanDate { get; set; }
    public string FromShift { get; set; } = "";
    public string ToLineNo { get; set; } = "";
    public DateTime ToPlanDate { get; set; }
    public string ToShift { get; set; } = "";
    public double Qty { get; set; }
    public double Capacity { get; set; }
    public double? AllocatedPercent { get; set; }
    public string? MarketingNo { get; set; }
}

public class FibcCriticalShiftResult
{
    public bool Success { get; set; }
    public bool ShiftsRequired { get; set; }
    public bool FullyAllotted { get; set; }
    public string Message { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string BagType { get; set; } = "";
    public string BagTypeLabel { get; set; } = "";
    public double Quantity { get; set; }
    public double CapacityPerShift { get; set; }
    public int BufferDays { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public bool PinToTargetDate { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyList<FibcSlotGridItemDto> ProposedSlots { get; set; } = Array.Empty<FibcSlotGridItemDto>();
    public IReadOnlyList<FibcOrderShiftDisplacementDto> Displacements { get; set; } = Array.Empty<FibcOrderShiftDisplacementDto>();
}

public sealed class FibcCriticalShiftConfirmResult : FibcCriticalShiftResult
{
    public bool Saved { get; set; }
    public int RowsInserted { get; set; }
    public int RowsDeleted { get; set; }
    public int OrdersShifted { get; set; }
}

public sealed class FibcAllocationSlotKey
{
    public string OrderNo { get; set; } = "";
    public string LineNo { get; set; } = "";
    public DateTime PlanDate { get; set; }
    public string Shift { get; set; } = "";
}

public sealed class FibcSavedAllocationRowDto
{
    public string CompanyName { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public string BagType { get; set; } = "";
    public string LineNo { get; set; } = "";
    public DateTime PlanDate { get; set; }
    public string Shift { get; set; } = "";
    public double Qty { get; set; }
    public double? AllocatedPercent { get; set; }
    public double Capacity { get; set; }
    public double Efficiency { get; set; }
}
