using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

/// <summary>
/// Resolves FIBC factory vs fabric supply (loom) factory for an order.
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
