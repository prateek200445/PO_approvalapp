using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Bom;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

/// <summary>
/// Resolves FIBC factory vs fabric supply (loom) factory for an order,
/// and per-BOM-component supply factories / due dates.
/// </summary>
public sealed class OrderPlanningRouteService
{
    private readonly IPlanningSetupRepository _repository;
    private readonly LoomPlanningOptions _loomOptions;

    public OrderPlanningRouteService(
        IPlanningSetupRepository repository,
        IOptions<LoomPlanningOptions> loomOptions)
    {
        _repository = repository;
        _loomOptions = loomOptions.Value;
    }

    public async Task<PlanningOrderRouteDto> ResolveAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var trimmed = orderNo.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return BuildDefaultRoute("", _loomOptions.DefaultCompanyName, _loomOptions.DefaultCompanyName, 0, "Default", null);
        }

        var saved = await _repository.GetSavedOrderRouteAsync(trimmed, ct);
        if (saved is not null)
            return saved;

        var fibcCompany = _loomOptions.DefaultCompanyName;
        var defaults = await _repository.GetInterUnitDefaultsAsync(fibcCompany, ct);
        // Same-unit unless Sulzer/ICO is detected (or a saved per-order route exists).
        var supplyCompany = fibcCompany;
        var transferDays = defaults.DefaultTransferBufferDays;
        string? autoReason = null;
        var routeSource = "Default";

        if (defaults.AutoDetectSulzerFabric)
        {
            var signals = await _repository.DetectBomInterUnitSignalsAsync(trimmed, ct);
            if (signals.Count > 0 && !string.IsNullOrWhiteSpace(defaults.DefaultFabricSupplyCompany))
            {
                supplyCompany = defaults.DefaultFabricSupplyCompany!;
                autoReason = string.Join(" ", signals);
                routeSource = "AutoDetected";
            }
        }

        return BuildDefaultRoute(
            trimmed,
            fibcCompany,
            supplyCompany,
            transferDays,
            routeSource,
            autoReason);
    }

    public async Task<PlanningOrderComponentPlanDto> ResolvePlanAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var trimmed = (orderNo ?? "").Trim();
        var orderRoute = await ResolveAsync(trimmed, ct);
        if (string.IsNullOrEmpty(trimmed))
        {
            return new PlanningOrderComponentPlanDto { OrderRoute = orderRoute };
        }

        var bomTask = _repository.GetBomComponentLinesAsync(trimmed, ct);
        var savedTask = _repository.GetSavedComponentRoutesAsync(trimmed, ct);
        var dispatchTask = _repository.GetMarketingDispatchDateAsync(trimmed, ct);
        await Task.WhenAll(bomTask, savedTask, dispatchTask);

        var bom = await bomTask;
        var saved = await savedTask;
        var dispatch = await dispatchTask;
        var savedByHeading = saved
            .GroupBy(row => BomComponentClassifier.NormalizeHeading(row.Heading), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var components = bom
            .GroupBy(row => BomComponentClassifier.NormalizeHeading(row.Heading), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(line => MapComponent(trimmed, line, orderRoute, savedByHeading, dispatch))
            .ToList();

        return new PlanningOrderComponentPlanDto
        {
            OrderRoute = orderRoute,
            DispatchDate = dispatch,
            Components = components,
        };
    }

    public async Task<PlanningOrderComponentRouteDto?> ResolveComponentAsync(
        string orderNo,
        string? heading,
        CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(orderNo, ct);
        var normalized = BomComponentClassifier.NormalizeHeading(heading);
        if (string.IsNullOrEmpty(normalized))
            return null;

        return plan.Components.FirstOrDefault(row =>
            BomComponentClassifier.NormalizeHeading(row.Heading)
                .Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static PlanningOrderComponentRouteDto MapComponent(
        string orderNo,
        PlanningBomComponentLineDto line,
        PlanningOrderRouteDto orderRoute,
        IReadOnlyDictionary<string, PlanningOrderComponentRouteDto> savedByHeading,
        DateTime? dispatchDate)
    {
        var key = BomComponentClassifier.NormalizeHeading(line.Heading);
        savedByHeading.TryGetValue(key, out var saved);

        var defaultSupply = line.IsLoomEligible
            ? orderRoute.FabricSupplyCompanyName
            : orderRoute.FibcCompanyName;
        var defaultTransfer = line.IsLoomEligible
            ? Math.Max(0, orderRoute.TransferBufferDays)
            : 0;

        var supply = !string.IsNullOrWhiteSpace(saved?.SupplyCompanyName)
            ? saved!.SupplyCompanyName.Trim()
            : defaultSupply;
        var transferDays = saved is not null
            ? Math.Max(0, saved.TransferBufferDays)
            : defaultTransfer;

        var fibc = orderRoute.FibcCompanyName;
        var isInterUnit = !string.Equals(supply, fibc, StringComparison.OrdinalIgnoreCase);
        if (!isInterUnit)
            transferDays = 0;

        var due = line.TargetDate;
        if (due is null && dispatchDate is not null)
            due = dispatchDate.Value.Date.AddDays(-transferDays);

        return new PlanningOrderComponentRouteDto
        {
            ComponentRouteId = saved?.ComponentRouteId,
            OrderNo = orderNo,
            Heading = line.Heading,
            Category = line.Category,
            PlanningKind = line.PlanningKind,
            IsLoomEligible = line.IsLoomEligible,
            SupplyCompanyName = supply,
            FibcCompanyName = fibc,
            TransferBufferDays = transferDays,
            IsInterUnit = isInterUnit,
            RouteSource = saved is not null ? "SavedComponent" : "OrderDefault",
            DueDate = due,
            Gsm = line.Gsm,
            FabricSize = line.FabricSize,
            TotalMtr = line.TotalMtr,
            TotalKg = line.TotalKg,
        };
    }

    private static PlanningOrderRouteDto BuildDefaultRoute(
        string orderNo,
        string fibcCompany,
        string supplyCompany,
        int transferDays,
        string routeSource,
        string? autoReason)
    {
        var isInterUnit = !string.Equals(fibcCompany, supplyCompany, StringComparison.OrdinalIgnoreCase);
        return new PlanningOrderRouteDto
        {
            OrderNo = orderNo,
            FibcCompanyName = fibcCompany,
            FabricSupplyCompanyName = supplyCompany,
            TransferBufferDays = Math.Max(0, transferDays),
            IsInterUnit = isInterUnit,
            RouteSource = routeSource,
            AutoDetectedReason = autoReason,
        };
    }
}
