using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningService
{
    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcPlanningEngine _engine;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningService(
        IFibcPlanningRepository repository,
        IFibcPlanningEngine engine,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _engine = engine;
        _options = options.Value;
    }

    public FibcPlanningConfigDto GetConfig() => new()
    {
        DefaultCompanyName = _options.DefaultCompanyName,
        DispatchBufferDays = _options.DispatchBufferDays,
        ShiftPreference = _options.ShiftPreference,
        ActiveShifts = _options.ActiveShifts,
        AllotmentEnabled = true,
        PreviewOnly = true,
    };

    public Task<FibcOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(string orderNo, CancellationToken ct = default) =>
        _repository.GetOrderAllotmentContextAsync(orderNo, ct);

    public async Task<IReadOnlyList<string>> GetActiveShiftsAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return await _repository.GetDistinctShiftsAsync(from, to, companyName, ct);
    }

    public Task<IReadOnlyList<FibcLineConfigDto>> GetLinesAsync(string? companyName, CancellationToken ct = default) =>
        _repository.GetLineConfigAsync(companyName, ct);

    public Task<FibcSlotGridResult> GetSlotGridAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return _repository.GetSlotGridAsync(from, to, companyName, ct);
    }

    public async Task<FibcOrderPlanDetailDto?> GetOrderPlanAsync(string orderNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var planLinesTask = _repository.GetOrderPlanLinesAsync(trimmed, ct);
        var fabricTask = _repository.GetFabricRequirementsAsync(trimmed, ct);
        await Task.WhenAll(planLinesTask, fabricTask);

        var planLines = await planLinesTask;
        var fabric = await fabricTask;
        if (planLines.Count == 0 && fabric.Count == 0)
            return null;

        return new FibcOrderPlanDetailDto
        {
            OrderNo = trimmed,
            PlanLines = planLines,
            FabricRequirements = fabric,
        };
    }

    public Task<FibcAllotmentResult> PreviewAllotmentAsync(FibcAllotmentRequest request, CancellationToken ct = default) =>
        _engine.AllotOrderAsync(request, ct);
}
