using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningService
{
    private readonly ILoomPlanningRepository _repository;
    private readonly ILoomPlanningEngine _engine;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningService(
        ILoomPlanningRepository repository,
        ILoomPlanningEngine engine,
        IOptions<LoomPlanningOptions> options)
    {
        _repository = repository;
        _engine = engine;
        _options = options.Value;
    }

    public LoomPlanningConfigDto GetConfig() => new()
    {
        DefaultCompanyName = _options.DefaultCompanyName,
        ReadOnly = !_options.AllowConfirmSave,
        PreviewOnly = !_options.AllowConfirmSave,
        ConfirmSaveEnabled = _options.AllowConfirmSave,
        ReplaceExistingEnabled = _options.AllowReplaceExistingPlan,
        FabricBufferDays = _options.FabricBufferDays,
        MaxPlanningHorizonDays = _options.MaxPlanningHorizonDays,
        MaxDaysPerLoomSegment = _options.MaxDaysPerLoomSegment,
        MaxChangeoversPerDay = _options.MaxChangeoversPerDay,
        DefaultEfficiency = _options.DefaultEfficiency,
    };

    public Task<IReadOnlyList<LoomMasterDto>> GetLoomsAsync(string? companyName, CancellationToken ct = default) =>
        _repository.GetLoomMasterAsync(companyName, ct);

    public Task<LoomAllocationGridResult> GetAllocationGridAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return _repository.GetAllocationGridAsync(from, to, companyName, ct);
    }

    public Task<LoomProductionMeterGridResult> GetProductionMetersAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return _repository.GetProductionMetersAsync(from, to, companyName, ct);
    }

    public Task<IReadOnlyList<LoomPpmSpecDto>> GetPpmSpecsAsync(CancellationToken ct = default) =>
        _repository.GetPpmSpecsAsync(ct);

    public async Task<LoomOrderPlanDetailDto?> GetOrderPlanAsync(string orderNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var allocationsTask = _repository.GetOrderAllocationsAsync(trimmed, ct);
        var fabricTask = _repository.GetFabricRequirementsAsync(trimmed, ct);
        await Task.WhenAll(allocationsTask, fabricTask);

        var allocations = await allocationsTask;
        var fabric = await fabricTask;
        if (allocations.Count == 0 && fabric.Count == 0)
            return null;

        return new LoomOrderPlanDetailDto
        {
            OrderNo = trimmed,
            Allocations = allocations,
            FabricRequirements = fabric,
        };
    }

    public Task<LoomOrderContextDto?> GetOrderContextAsync(string orderNo, CancellationToken ct = default) =>
        _repository.GetOrderContextAsync(orderNo, ct);

    public Task<LoomOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(string orderNo, CancellationToken ct = default) =>
        _repository.GetOrderAllotmentContextAsync(orderNo, ct);

    public Task<string?> ResolveWeavingCompanyForOrderAsync(string orderNo, CancellationToken ct = default) =>
        _repository.ResolveWeavingCompanyFromAllocationsAsync(orderNo, ct);

    public Task<LoomAllotmentResult> PreviewAllotmentAsync(LoomAllotmentRequest request, CancellationToken ct = default) =>
        _engine.AllotAsync(request, ct);

    public Task<LoomAllotmentConfirmResult> ConfirmAllotmentAsync(LoomAllotmentRequest request, CancellationToken ct = default) =>
        _engine.ConfirmAllotAsync(request, ct);
}
