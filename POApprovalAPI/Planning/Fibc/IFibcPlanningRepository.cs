using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public interface IFibcPlanningRepository
{
    Task<IReadOnlyList<FibcLineConfigDto>> GetLineConfigAsync(string? companyName, CancellationToken ct = default);

    Task<FibcSlotGridResult> GetSlotGridAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default);

    Task<IReadOnlyList<FibcOrderPlanLineDto>> GetOrderPlanLinesAsync(
        string orderNo,
        CancellationToken ct = default);

    Task<IReadOnlyList<FibcOrderPlanLineDto>> GetSavedAllocationLinesAsync(
        string orderNo,
        CancellationToken ct = default);

    Task<IReadOnlyList<FibcFabricRequirementDto>> GetFabricRequirementsAsync(
        string orderNo,
        CancellationToken ct = default);

    Task<FibcOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(
        string orderNo,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetDistinctShiftsAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default);

    Task<int> GetExistingAllocationCountAsync(string orderNo, CancellationToken ct = default);

    Task<double?> GetSlotRemainingAsync(
        string companyName,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default);

    Task<int?> GetCapacityTransIdAsync(
        string companyName,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default);

    Task<int> InsertAllocationsAsync(
        string companyName,
        string orderNo,
        string? partyName,
        string? marketingNo,
        IReadOnlyList<FibcSlotGridItemDto> slots,
        bool replaceExisting,
        CancellationToken ct = default);

    Task<FibcSavedAllocationRowDto?> GetSavedAllocationSlotAsync(
        string orderNo,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default);

    Task<int> DeleteAllocationSlotAsync(
        string orderNo,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default);

    Task<int> ApplyCriticalShiftPlanAsync(
        string companyName,
        string criticalOrderNo,
        string? criticalPartyName,
        string? criticalMarketingNo,
        IReadOnlyList<FibcSlotGridItemDto> criticalSlots,
        IReadOnlyList<FibcOrderShiftDisplacementDto> displacements,
        bool replaceCriticalExisting,
        CancellationToken ct = default);
}
