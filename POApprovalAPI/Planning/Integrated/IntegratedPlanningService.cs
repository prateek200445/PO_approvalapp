using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Execution;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Integrated.Models;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Integrated;

public sealed class IntegratedPlanningService
{
    private static readonly DateTime MinValidDate = new(2000, 1, 1);

    private readonly FibcPlanningService _fibc;
    private readonly LoomPlanningService _loom;
    private readonly ExecutionPlanningService _execution;
    private readonly PlanningRuntimeContextLoader _runtimeLoader;
    private readonly OrderPlanningRouteService _routeService;
    private readonly LoomPlanningOptions _loomOptions;

    public IntegratedPlanningService(
        FibcPlanningService fibc,
        LoomPlanningService loom,
        ExecutionPlanningService execution,
        PlanningRuntimeContextLoader runtimeLoader,
        OrderPlanningRouteService routeService,
        IOptions<LoomPlanningOptions> loomOptions)
    {
        _fibc = fibc;
        _loom = loom;
        _execution = execution;
        _runtimeLoader = runtimeLoader;
        _routeService = routeService;
        _loomOptions = loomOptions.Value;
    }

    public async Task<IntegratedOrderTimelineDto?> GetOrderTimelineAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var routeTask = _routeService.ResolveAsync(trimmed, ct);
        var fibcPlanTask = _fibc.GetOrderPlanAsync(trimmed, ct);
        var fibcCtxTask = _fibc.GetOrderAllotmentContextAsync(trimmed, ct);
        var loomPlanTask = _loom.GetOrderPlanAsync(trimmed, ct);
        var loomCtxTask = _loom.GetOrderAllotmentContextAsync(trimmed, ct);
        await Task.WhenAll(routeTask, fibcPlanTask, fibcCtxTask, loomPlanTask, loomCtxTask);

        var route = await routeTask;
        var fibcPlan = await fibcPlanTask;
        var fibcCtx = await fibcCtxTask;
        var loomPlan = await loomPlanTask;
        var loomCtx = await loomCtxTask;

        var hasLoom = (loomPlan?.Allocations.Count ?? 0) > 0;
        var hasFibc = (fibcPlan?.PlanLines.Count ?? 0) > 0 || (fibcPlan?.SavedAllocations.Count ?? 0) > 0;
        var hasFabric = (fibcPlan?.FabricRequirements.Count ?? 0) > 0 || (loomPlan?.FabricRequirements.Count ?? 0) > 0;
        var hasContext = fibcCtx is not null || loomCtx is not null;

        if (!hasLoom && !hasFibc && !hasFabric && !hasContext)
            return null;

        var loomAllocations = loomPlan?.Allocations ?? Array.Empty<LoomOrderAllocationLineDto>();
        var effectiveRoute = route;
        if (hasLoom)
        {
            var wovenAt = await _loom.ResolveWeavingCompanyForOrderAsync(trimmed, ct);
            if (!string.IsNullOrWhiteSpace(wovenAt))
                effectiveRoute = WithWeavingCompany(route, wovenAt);
        }

        var warnings = new List<string>();
        var fabricRequirements = MergeFabricRequirements(fibcPlan, loomPlan);

        var dispatchDate = FirstValidDate(fibcCtx?.DispatchDate, loomCtx?.DispatchDate);
        var fabricRequirementDate = FirstValidDate(
            loomCtx?.FabricRequirementDate,
            fabricRequirements.Select(f => f.TargetDate).FirstOrDefault(IsValidDate));

        var loomStart = loomAllocations.Count > 0
            ? loomAllocations.Min(a => a.AllocationDate.Date)
            : (DateTime?)null;
        var loomEnd = loomAllocations.Count > 0
            ? loomAllocations.Max(a => (a.ToDate ?? a.AllocationDate).Date)
            : (DateTime?)null;

        var transferDays = effectiveRoute.IsInterUnit ? Math.Max(0, effectiveRoute.TransferBufferDays) : 0;
        DateTime? transferStart = loomEnd;
        DateTime? transferEnd = loomEnd is not null && transferDays > 0
            ? loomEnd.Value.AddDays(transferDays)
            : loomEnd;

        var fibcLines = MergeFibcLines(fibcPlan);
        var fibcStart = fibcLines.Count > 0 ? MinFibcDate(fibcLines, true) : null;
        var fibcEnd = fibcLines.Count > 0 ? MinFibcDate(fibcLines, false) : null;

        if (effectiveRoute.IsInterUnit)
        {
            warnings.Add(
                $"Inter-unit: fabric woven at {effectiveRoute.FabricSupplyCompanyName}, FIBC at {effectiveRoute.FibcCompanyName} " +
                $"(transfer buffer {transferDays} day(s), source: {effectiveRoute.RouteSource}).");
            if (!string.IsNullOrWhiteSpace(effectiveRoute.AutoDetectedReason))
                warnings.Add(effectiveRoute.AutoDetectedReason);
        }

        if (loomEnd is not null && fabricRequirementDate is not null)
        {
            var loomMustCompleteBy = fabricRequirementDate.Value.Date
                .AddDays(-_loomOptions.FabricBufferDays - transferDays);
            if (loomEnd.Value.Date > loomMustCompleteBy)
            {
                warnings.Add(
                    effectiveRoute.IsInterUnit
                        ? $"Loom weaving ends {loomEnd:yyyy-MM-dd} but must complete by {loomMustCompleteBy:yyyy-MM-dd} " +
                          $"({_loomOptions.FabricBufferDays}-day FIBC buffer + {transferDays}-day inter-unit transfer)."
                        : $"Loom weaving ends {loomEnd:yyyy-MM-dd} but fabric should complete by {loomMustCompleteBy:yyyy-MM-dd} ({_loomOptions.FabricBufferDays}-day buffer before FIBC).");
            }
        }

        if (transferEnd is not null && fabricRequirementDate is not null && effectiveRoute.IsInterUnit)
        {
            var fabricReadyBy = fabricRequirementDate.Value.Date.AddDays(-_loomOptions.FabricBufferDays);
            if (transferEnd.Value.Date > fabricReadyBy)
            {
                warnings.Add(
                    $"Inter-unit transfer ends {transferEnd:yyyy-MM-dd} after fabric-ready target {fabricReadyBy:yyyy-MM-dd}.");
            }
        }

        if (fibcEnd is not null && dispatchDate is not null && fibcEnd.Value.Date > dispatchDate.Value.Date)
        {
            warnings.Add($"FIBC plan ends {fibcEnd:yyyy-MM-dd} after dispatch date {dispatchDate:yyyy-MM-dd}.");
        }

        if (!hasLoom)
        {
            warnings.Add(effectiveRoute.IsInterUnit
                ? $"No loom allocations at supply factory ({effectiveRoute.FabricSupplyCompanyName})."
                : "No loom allocations found for this order.");
        }

        if (!hasFibc)
            warnings.Add("No FIBC line plan found for this order.");

        try
        {
            var exec = await _execution.GetOrderExecutionAsync(trimmed, route.FibcCompanyName, ct);
            if (exec.BailingGap > 0)
                warnings.Add($"Bailing gap: {exec.BailingGap:N0} pcs produced but not bailed.");
            foreach (var s in exec.ReplanSuggestions)
                warnings.Add(s);
        }
        catch
        {
            // Non-fatal — timeline still useful without execution data
        }

        if (fibcCtx?.PartyName is not null || hasFibc)
        {
            try
            {
                var runtime = await _runtimeLoader.LoadAsync(effectiveRoute.FibcCompanyName, ct);
                if (runtime.LoomPool.Any(l => l.PoolId.HasValue) && !hasLoom && !effectiveRoute.IsInterUnit)
                    warnings.Add("Loom pool is configured but this order has no loom plan — fabric may be missing.");
            }
            catch
            {
                // ignore
            }
        }

        var milestones = BuildMilestones(
            loomStart,
            loomEnd,
            transferStart,
            transferEnd,
            fabricRequirementDate,
            fibcStart,
            fibcEnd,
            dispatchDate,
            effectiveRoute,
            loomAllocations,
            fibcLines);

        return new IntegratedOrderTimelineDto
        {
            OrderNo = trimmed,
            PartyName = fibcCtx?.PartyName ?? loomCtx?.PartyName,
            MarketingNo = fibcCtx?.MarketingNo ?? loomCtx?.MarketingNo,
            BagType = fibcCtx?.BagType ?? loomCtx?.BagType,
            Quantity = fibcCtx?.Quantity ?? loomCtx?.Quantity,
            DispatchDate = dispatchDate,
            FabricRequirementDate = fabricRequirementDate,
            LoomStartDate = loomStart,
            LoomEndDate = loomEnd,
            TransferStartDate = effectiveRoute.IsInterUnit ? transferStart : null,
            TransferEndDate = effectiveRoute.IsInterUnit ? transferEnd : null,
            FibcStartDate = fibcStart,
            FibcEndDate = fibcEnd,
            FabricBufferDays = _loomOptions.FabricBufferDays,
            TransferBufferDays = transferDays,
            FibcCompanyName = effectiveRoute.FibcCompanyName,
            FabricSupplyCompanyName = effectiveRoute.FabricSupplyCompanyName,
            IsInterUnit = effectiveRoute.IsInterUnit,
            RouteSource = effectiveRoute.RouteSource,
            Milestones = milestones,
            LoomAllocations = loomAllocations,
            FabricRequirements = fabricRequirements,
            FibcPlanLines = fibcLines,
            Warnings = warnings,
        };
    }

    private static IReadOnlyList<FibcFabricRequirementDto> MergeFabricRequirements(
        FibcOrderPlanDetailDto? fibcPlan,
        LoomOrderPlanDetailDto? loomPlan)
    {
        if (fibcPlan?.FabricRequirements.Count > 0)
            return fibcPlan.FabricRequirements;

        if (loomPlan?.FabricRequirements.Count > 0)
        {
            return loomPlan.FabricRequirements.Select(f => new FibcFabricRequirementDto
            {
                Customer = f.Customer,
                FilePoNo = f.FilePoNo,
                BagType = f.BagType,
                Qty = f.Qty?.ToString(),
                PoDate = f.PoDate,
                TargetDate = f.TargetDate,
                Heading = f.Heading,
                Gsm = f.Gsm,
                FabricSize = f.FabricSize,
                TotalMtr = f.TotalMtr,
                TotalKg = f.TotalKg,
            }).ToList();
        }

        return Array.Empty<FibcFabricRequirementDto>();
    }

    private static IReadOnlyList<FibcOrderPlanLineDto> MergeFibcLines(FibcOrderPlanDetailDto? fibcPlan)
    {
        if (fibcPlan is null)
            return Array.Empty<FibcOrderPlanLineDto>();

        if (fibcPlan.SavedAllocations.Count > 0)
            return fibcPlan.SavedAllocations;

        return fibcPlan.PlanLines;
    }

    private static DateTime? MinFibcDate(IReadOnlyList<FibcOrderPlanLineDto> lines, bool start)
    {
        if (start)
        {
            var dates = lines
                .SelectMany(l => new[] { l.StartDate, l.PlanDate })
                .Where(IsValidDate)
                .Select(d => d!.Value.Date)
                .ToList();
            return dates.Count > 0 ? dates.Min() : null;
        }

        var endDates = lines
            .SelectMany(l => new[] { l.CompletionDate, l.PlanDate })
            .Where(IsValidDate)
            .Select(d => d!.Value.Date)
            .ToList();
        return endDates.Count > 0 ? endDates.Max() : null;
    }

    private static List<IntegratedTimelineMilestoneDto> BuildMilestones(
        DateTime? loomStart,
        DateTime? loomEnd,
        DateTime? transferStart,
        DateTime? transferEnd,
        DateTime? fabricRequirementDate,
        DateTime? fibcStart,
        DateTime? fibcEnd,
        DateTime? dispatchDate,
        PlanningOrderRouteDto route,
        IReadOnlyList<LoomOrderAllocationLineDto> loomAllocations,
        IReadOnlyList<FibcOrderPlanLineDto> fibcLines)
    {
        var milestones = new List<IntegratedTimelineMilestoneDto>();
        var sort = 1;

        if (loomStart is not null || loomEnd is not null)
        {
            var loomCount = loomAllocations.Select(a => a.LoomNo).Distinct().Count();
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "Loom",
                Label = route.IsInterUnit ? "Loom weaving (supply factory)" : "Loom weaving",
                StartDate = loomStart,
                EndDate = loomEnd,
                Detail = loomCount > 0
                    ? $"{loomCount} loom(s) at {route.FabricSupplyCompanyName}"
                    : route.FabricSupplyCompanyName,
                SortOrder = sort++,
            });
        }

        if (route.IsInterUnit && (transferStart is not null || transferEnd is not null))
        {
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "Transfer",
                Label = "Inter-unit fabric transfer",
                StartDate = transferStart,
                EndDate = transferEnd,
                Detail = $"{route.FabricSupplyCompanyName} → {route.FibcCompanyName} ({route.TransferBufferDays} day buffer)",
                SortOrder = sort++,
            });
        }

        if (fabricRequirementDate is not null)
        {
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "FabricReady",
                Label = "Fabric ready for FIBC",
                StartDate = fabricRequirementDate,
                EndDate = fabricRequirementDate,
                Detail = route.IsInterUnit
                    ? $"Target at {route.FibcCompanyName} (BOM / dispatch plan)"
                    : "Target from BOM / dispatch plan",
                SortOrder = sort++,
            });
        }

        if (fibcStart is not null || fibcEnd is not null)
        {
            var lineCount = fibcLines.Select(l => l.LineNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "Fibc",
                Label = "FIBC line production",
                StartDate = fibcStart,
                EndDate = fibcEnd,
                Detail = lineCount > 0
                    ? $"{lineCount} line(s) at {route.FibcCompanyName}"
                    : route.FibcCompanyName,
                SortOrder = sort++,
            });
        }

        if (dispatchDate is not null)
        {
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "Dispatch",
                Label = "Dispatch",
                StartDate = dispatchDate,
                EndDate = dispatchDate,
                Detail = "Marketing invoice despatch date",
                SortOrder = sort++,
            });
        }

        return milestones;
    }

    private static PlanningOrderRouteDto WithWeavingCompany(PlanningOrderRouteDto route, string weavingCompany)
    {
        var interUnit = !string.Equals(weavingCompany, route.FibcCompanyName, StringComparison.OrdinalIgnoreCase);
        return new PlanningOrderRouteDto
        {
            OrderNo = route.OrderNo,
            FibcCompanyName = route.FibcCompanyName,
            FabricSupplyCompanyName = weavingCompany,
            TransferBufferDays = route.TransferBufferDays,
            IsInterUnit = interUnit,
            RouteSource = interUnit == route.IsInterUnit ? route.RouteSource : "ActualWeave",
            AutoDetectedReason = route.AutoDetectedReason,
        };
    }

    private static DateTime? FirstValidDate(params DateTime?[] dates)
    {
        foreach (var date in dates)
        {
            if (IsValidDate(date))
                return date!.Value.Date;
        }
        return null;
    }

    private static bool IsValidDate(DateTime? date) =>
        date is { } value && value.Date >= MinValidDate;
}
