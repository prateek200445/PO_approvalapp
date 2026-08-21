using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningService
{
    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcPlanningEngine _engine;
    private readonly IFibcCriticalShiftEngine _criticalShiftEngine;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningService(
        IFibcPlanningRepository repository,
        IFibcPlanningEngine engine,
        IFibcCriticalShiftEngine criticalShiftEngine,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _engine = engine;
        _criticalShiftEngine = criticalShiftEngine;
        _options = options.Value;
    }

    public FibcPlanningConfigDto GetConfig() => new()
    {
        DefaultCompanyName = _options.DefaultCompanyName,
        DispatchBufferDays = _options.DispatchBufferDays,
        ShiftPreference = _options.ShiftPreference,
        ActiveShifts = _options.ActiveShifts,
        AllotmentEnabled = true,
        PreviewOnly = !_options.AllowConfirmSave,
        ConfirmSaveEnabled = _options.AllowConfirmSave,
        ReplaceExistingEnabled = _options.AllowReplaceExistingPlan,
        QuotationHoldEnabled = _options.QuotationHoldEnabled,
        QuotationHoldDays = _options.QuotationHoldDays,
        QuotationHoldEmailEnabled = _options.QuotationHoldEmailEnabled,
        CriticalShiftEnabled = _options.CriticalShiftEnabled,
        CriticalShiftEmailEnabled = _options.CriticalShiftEmailEnabled,
    };

    public Task<FibcCriticalShiftResult> PreviewCriticalShiftAsync(FibcCriticalShiftRequest request, CancellationToken ct = default) =>
        _criticalShiftEngine.PreviewCriticalShiftAsync(request, ct);

    public Task<FibcCriticalShiftConfirmResult> ConfirmCriticalShiftAsync(FibcCriticalShiftRequest request, CancellationToken ct = default) =>
        _criticalShiftEngine.ConfirmCriticalShiftAsync(request, ct);

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
        var company = string.IsNullOrWhiteSpace(companyName)
            ? _options.DefaultCompanyName
            : companyName.Trim();
        return FibcPlanningGridComposer.BuildDisplayGridAsync(_repository, company, from, to, ct);
    }

    public async Task<FibcOrderPlanDetailDto?> GetOrderPlanAsync(string orderNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var planLinesTask = _repository.GetOrderPlanLinesAsync(trimmed, ct);
        var savedTask = _repository.GetSavedAllocationLinesAsync(trimmed, ct);
        var fabricTask = _repository.GetFabricRequirementsAsync(trimmed, ct);
        await Task.WhenAll(planLinesTask, savedTask, fabricTask);

        var planLines = await planLinesTask;
        var savedAllocations = await savedTask;
        var fabric = await fabricTask;
        if (planLines.Count == 0 && savedAllocations.Count == 0 && fabric.Count == 0)
            return null;

        return new FibcOrderPlanDetailDto
        {
            OrderNo = trimmed,
            PlanLines = planLines,
            SavedAllocations = savedAllocations,
            FabricRequirements = fabric,
        };
    }

    public Task<FibcAllotmentResult> PreviewAllotmentAsync(FibcAllotmentRequest request, CancellationToken ct = default) =>
        _engine.AllotOrderAsync(request, ct);

    public Task<FibcAllotmentConfirmResult> ConfirmAllotmentAsync(FibcAllotmentRequest request, CancellationToken ct = default) =>
        _engine.ConfirmAllotOrderAsync(request, ct);
}
