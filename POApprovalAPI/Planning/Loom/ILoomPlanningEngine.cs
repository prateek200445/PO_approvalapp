using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Loom;

public interface ILoomPlanningEngine
{
    Task<LoomAllotmentResult> AllotAsync(LoomAllotmentRequest request, CancellationToken ct = default);

    Task<LoomAllotmentConfirmResult> ConfirmAllotAsync(LoomAllotmentRequest request, CancellationToken ct = default);
}
