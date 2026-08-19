using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

public sealed class PlanningSetupService
{
    private readonly IPlanningSetupRepository _repository;

    public PlanningSetupService(IPlanningSetupRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PlanningFactoryOptionDto>> SearchFactoriesAsync(string? query, CancellationToken ct = default) =>
        _repository.SearchFactoriesAsync(query, ct: ct);

    public Task<IReadOnlyList<PlanningFactoryConfigDto>> GetEnabledFactoriesAsync(CancellationToken ct = default) =>
        _repository.GetEnabledFactoriesAsync(ct);

    public Task<PlanningFactoryConfigDto?> GetFactoryConfigAsync(string companyName, CancellationToken ct = default) =>
        _repository.GetFactoryConfigAsync(companyName, ct);

    public Task<PlanningFactoryConfigDto> SaveFactoryConfigAsync(UpsertPlanningFactoryConfigRequest request, CancellationToken ct = default) =>
        _repository.UpsertFactoryConfigAsync(request, ct);

    public Task<IReadOnlyList<PlanningLineConfigDto>> GetLinesAsync(string companyName, CancellationToken ct = default) =>
        _repository.GetMergedLineConfigsAsync(companyName, ct);

    public Task<IReadOnlyList<PlanningLineConfigDto>> ImportLinesFromErpAsync(string companyName, CancellationToken ct = default) =>
        _repository.ImportLinesFromErpAsync(companyName, ct);

    public Task SaveLinesAsync(SavePlanningLineConfigsRequest request, CancellationToken ct = default) =>
        _repository.SaveLineConfigsAsync(request, ct);

    public Task<IReadOnlyList<PlanningLoomPoolDto>> GetLoomPoolAsync(string companyName, CancellationToken ct = default) =>
        _repository.GetMergedLoomPoolAsync(companyName, ct);

    public Task SaveLoomPoolAsync(SavePlanningLoomPoolRequest request, CancellationToken ct = default) =>
        _repository.SaveLoomPoolAsync(request, ct);

    public Task<IReadOnlyList<PlanningTeamFactorDto>> GetTeamFactorsAsync(string companyName, CancellationToken ct = default) =>
        _repository.GetTeamFactorsAsync(companyName, ct);

    public Task SaveTeamFactorsAsync(SavePlanningTeamFactorRequest request, CancellationToken ct = default) =>
        _repository.SaveTeamFactorsAsync(request, ct);

    public async Task<RecalculateTeamFactorsResult> RecalculateTeamFactorsAsync(
        string companyName,
        int sampleDays = 30,
        CancellationToken ct = default)
    {
        var updated = await _repository.RecalculateTeamFactorsFromErpAsync(companyName, sampleDays, ct);
        var factors = await _repository.GetTeamFactorsAsync(companyName, ct);
        return new RecalculateTeamFactorsResult
        {
            Success = true,
            UpdatedCount = updated,
            Factors = factors,
            Message = updated > 0
                ? $"Recalculated {updated} team factor row(s) from FIBCTeamWiseProduction."
                : "No team production history found for this factory in the selected window. Link TeamNo on lines or enter manual factors.",
        };
    }

    public Task<IReadOnlyList<PlanningBacklogDto>> GetBacklogAsync(string companyName, string? status = "Open", CancellationToken ct = default) =>
        _repository.GetBacklogAsync(companyName, status, ct);

    public Task<PlanningBacklogDto> CreateBacklogAsync(CreatePlanningBacklogRequest request, CancellationToken ct = default) =>
        _repository.CreateBacklogAsync(request, ct);

    public Task<bool> ClearBacklogAsync(int backlogId, CancellationToken ct = default) =>
        _repository.ClearBacklogAsync(backlogId, ct);

    public Task<IReadOnlyList<PlanningDowntimeDto>> GetDowntimeAsync(
        string companyName,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default) =>
        _repository.GetDowntimeAsync(companyName, from, to, ct);

    public Task SaveDowntimeAsync(SavePlanningDowntimeRequest request, CancellationToken ct = default) =>
        _repository.SaveDowntimeAsync(request, ct);

    public Task DeleteDowntimeAsync(int downtimeId, CancellationToken ct = default) =>
        _repository.DeleteDowntimeAsync(downtimeId, ct);

    public Task<IReadOnlyList<PlanningLoomPreferenceChartDto>> GetLoomPreferenceChartAsync(string companyName, CancellationToken ct = default) =>
        _repository.GetLoomPreferenceChartAsync(companyName, ct);

    public Task SaveLoomPreferenceChartAsync(SavePlanningLoomPreferenceChartRequest request, CancellationToken ct = default) =>
        _repository.SaveLoomPreferenceChartAsync(request, ct);

    public Task<int> ClearOpenBacklogForOrderAsync(string companyName, string orderNo, CancellationToken ct = default) =>
        _repository.ClearOpenBacklogForOrderAsync(companyName, orderNo, ct);
}
