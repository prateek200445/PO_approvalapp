using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Slot allotment and quotation logic (Phase 2+). Phase 1 uses read-only repository only.
/// </summary>
public interface IFibcPlanningEngine
{
    Task<FibcAllotmentResult> AllotOrderAsync(FibcAllotmentRequest request, CancellationToken ct = default);

    Task<FibcAllotmentConfirmResult> ConfirmAllotOrderAsync(FibcAllotmentRequest request, CancellationToken ct = default);
}
