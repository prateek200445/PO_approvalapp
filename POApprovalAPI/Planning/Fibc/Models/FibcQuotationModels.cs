namespace POApprovalAPI.Planning.Fibc.Models;

public sealed class FibcQuotationHoldRequest
{
    public string OrderNo { get; set; } = "";
    public string? CompanyName { get; set; }
    public DateTime? DispatchDate { get; set; }
    public double Quantity { get; set; }
    public string? BagType { get; set; }
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public string? Notes { get; set; }
}

public sealed class FibcQuotationHoldSlotDto
{
    public DateTime PlanDate { get; set; }
    public string LineNo { get; set; } = "";
    public string Shift { get; set; } = "";
    public double Qty { get; set; }
    public double Capacity { get; set; }
    public double? AllocatedPercent { get; set; }
}

public sealed class FibcQuotationHoldDto
{
    public int HoldId { get; set; }
    public string ReferenceCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string? MarketingNo { get; set; }
    public string? BagType { get; set; }
    public string BagTypeLabel { get; set; } = "";
    public double Quantity { get; set; }
    public DateTime? DispatchDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<FibcQuotationHoldSlotDto> Slots { get; set; } = Array.Empty<FibcQuotationHoldSlotDto>();
}

public sealed class FibcQuotationHoldResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public FibcQuotationHoldDto? Hold { get; set; }
}

public sealed class FibcQuotationConfirmResult
{
    public bool Success { get; set; }
    public bool Saved { get; set; }
    public string Message { get; set; } = "";
    public int HoldId { get; set; }
    public int RowsInserted { get; set; }
}

public sealed class FibcQuotationConfirmRequest
{
    public bool ReplaceExisting { get; set; }
}

public sealed class FibcHoldReservationDto
{
    public int HoldId { get; set; }
    public string OrderNo { get; set; } = "";
    public string ReferenceCode { get; set; } = "";
    public DateTime PlanDate { get; set; }
    public string LineNo { get; set; } = "";
    public string Shift { get; set; } = "";
    public double Qty { get; set; }
}
