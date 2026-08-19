using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

public interface IPlanningSetupRepository
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PlanningFactoryOptionDto>> SearchFactoriesAsync(string? query, int limit = 25, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningFactoryConfigDto>> GetEnabledFactoriesAsync(CancellationToken ct = default);

    Task<PlanningFactoryConfigDto?> GetFactoryConfigAsync(string companyName, CancellationToken ct = default);

    Task<PlanningFactoryConfigDto> UpsertFactoryConfigAsync(UpsertPlanningFactoryConfigRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningLineConfigDto>> GetMergedLineConfigsAsync(string companyName, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningLineConfigDto>> ImportLinesFromErpAsync(string companyName, CancellationToken ct = default);

    Task SaveLineConfigsAsync(SavePlanningLineConfigsRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningLoomPoolDto>> GetMergedLoomPoolAsync(string companyName, CancellationToken ct = default);

    Task SaveLoomPoolAsync(SavePlanningLoomPoolRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningTeamFactorDto>> GetTeamFactorsAsync(string companyName, CancellationToken ct = default);

    Task SaveTeamFactorsAsync(SavePlanningTeamFactorRequest request, CancellationToken ct = default);

    Task<int> RecalculateTeamFactorsFromErpAsync(string companyName, int sampleDays, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningBacklogDto>> GetBacklogAsync(string companyName, string? status, CancellationToken ct = default);

    Task<PlanningBacklogDto> CreateBacklogAsync(CreatePlanningBacklogRequest request, CancellationToken ct = default);

    Task<bool> ClearBacklogAsync(int backlogId, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningDowntimeDto>> GetDowntimeAsync(
        string companyName,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    Task SaveDowntimeAsync(SavePlanningDowntimeRequest request, CancellationToken ct = default);

    Task DeleteDowntimeAsync(int downtimeId, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningLoomPreferenceChartDto>> GetLoomPreferenceChartAsync(string companyName, CancellationToken ct = default);

    Task SaveLoomPreferenceChartAsync(SavePlanningLoomPreferenceChartRequest request, CancellationToken ct = default);

    Task<int> ClearOpenBacklogForOrderAsync(string companyName, string orderNo, CancellationToken ct = default);
}
