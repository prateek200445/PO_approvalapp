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
}
