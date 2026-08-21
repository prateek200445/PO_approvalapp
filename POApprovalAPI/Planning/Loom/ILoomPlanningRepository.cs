using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Loom;

public interface ILoomPlanningRepository
{
    Task<IReadOnlyList<LoomMasterDto>> GetLoomMasterAsync(string? companyName, CancellationToken ct = default);

    Task<LoomAllocationGridResult> GetAllocationGridAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default);

    Task<IReadOnlyList<LoomAllocationGridItemDto>> GetPlanningAllocationsAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default);

    Task<LoomProductionMeterGridResult> GetProductionMetersAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default);

    Task<IReadOnlyList<LoomPpmSpecDto>> GetPpmSpecsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LoomFormulaDto>> GetFormulasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LoomOrderAllocationLineDto>> GetOrderAllocationsAsync(
        string orderNo,
        CancellationToken ct = default);

    /// <summary>Company name from loom master for saved allocations (actual weave site).</summary>
    Task<string?> ResolveWeavingCompanyFromAllocationsAsync(string orderNo, CancellationToken ct = default);

    Task<IReadOnlyList<LoomFabricRequirementDto>> GetFabricRequirementsAsync(
        string orderNo,
        CancellationToken ct = default);

    Task<LoomOrderContextDto?> GetOrderContextAsync(string orderNo, CancellationToken ct = default);

    Task<LoomOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(string orderNo, CancellationToken ct = default);

    Task<int> GetExistingAllocationCountAsync(string orderNo, CancellationToken ct = default);

    Task<int> InsertLoomAllocationsAsync(
        string companyName,
        string orderNo,
        string? partyName,
        IReadOnlyList<LoomProposedSegmentDto> segments,
        bool replaceExisting,
        CancellationToken ct = default);

    Task<(int RowsShifted, int RowsInserted)> ApplyLoomShiftPlanAsync(
        string orderNo,
        string? partyName,
        IReadOnlyList<LoomProposedSegmentDto> segments,
        IReadOnlyList<LoomOrderShiftDisplacementDto> displacements,
        bool replaceExisting,
        CancellationToken ct = default);
}
