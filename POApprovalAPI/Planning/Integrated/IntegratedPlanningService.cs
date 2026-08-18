using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Integrated.Models;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Integrated;

public sealed class IntegratedPlanningService
{
    private static readonly DateTime MinValidDate = new(2000, 1, 1);

    private readonly FibcPlanningService _fibc;
    private readonly LoomPlanningService _loom;
    private readonly LoomPlanningOptions _loomOptions;

    public IntegratedPlanningService(
        FibcPlanningService fibc,
        LoomPlanningService loom,
        IOptions<LoomPlanningOptions> loomOptions)
    {
        _fibc = fibc;
        _loom = loom;
        _loomOptions = loomOptions.Value;
    }

    public async Task<IntegratedOrderTimelineDto?> GetOrderTimelineAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var fibcPlanTask = _fibc.GetOrderPlanAsync(trimmed, ct);
        var fibcCtxTask = _fibc.GetOrderAllotmentContextAsync(trimmed, ct);
        var loomPlanTask = _loom.GetOrderPlanAsync(trimmed, ct);
        var loomCtxTask = _loom.GetOrderAllotmentContextAsync(trimmed, ct);
        await Task.WhenAll(fibcPlanTask, fibcCtxTask, loomPlanTask, loomCtxTask);

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

        var warnings = new List<string>();
        var fabricRequirements = MergeFabricRequirements(fibcPlan, loomPlan);

        var dispatchDate = FirstValidDate(fibcCtx?.DispatchDate, loomCtx?.DispatchDate);
        var fabricRequirementDate = FirstValidDate(
            loomCtx?.FabricRequirementDate,
            fabricRequirements.Select(f => f.TargetDate).FirstOrDefault(IsValidDate));

        var loomAllocations = loomPlan?.Allocations ?? Array.Empty<LoomOrderAllocationLineDto>();
        var loomStart = loomAllocations.Count > 0
            ? loomAllocations.Min(a => a.AllocationDate.Date)
            : (DateTime?)null;
        var loomEnd = loomAllocations.Count > 0
            ? loomAllocations.Max(a => (a.ToDate ?? a.AllocationDate).Date)
            : (DateTime?)null;

        var fibcLines = MergeFibcLines(fibcPlan);
        var fibcStart = fibcLines.Count > 0 ? MinFibcDate(fibcLines, true) : null;
        var fibcEnd = fibcLines.Count > 0 ? MinFibcDate(fibcLines, false) : null;

        if (loomEnd is not null && fabricRequirementDate is not null)
        {
            var fabricReadyBy = fabricRequirementDate.Value.Date.AddDays(-_loomOptions.FabricBufferDays);
            if (loomEnd.Value.Date > fabricReadyBy)
            {
                warnings.Add(
                    $"Loom weaving ends {loomEnd:yyyy-MM-dd} but fabric should complete by {fabricReadyBy:yyyy-MM-dd} ({_loomOptions.FabricBufferDays}-day buffer before FIBC).");
            }
        }

        if (fibcEnd is not null && dispatchDate is not null && fibcEnd.Value.Date > dispatchDate.Value.Date)
        {
            warnings.Add($"FIBC plan ends {fibcEnd:yyyy-MM-dd} after dispatch date {dispatchDate:yyyy-MM-dd}.");
        }

        if (!hasLoom)
            warnings.Add("No loom allocations found for this order.");
        if (!hasFibc)
            warnings.Add("No FIBC line plan found for this order.");

        var milestones = BuildMilestones(loomStart, loomEnd, fabricRequirementDate, fibcStart, fibcEnd, dispatchDate, loomAllocations, fibcLines);

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
            FibcStartDate = fibcStart,
            FibcEndDate = fibcEnd,
            FabricBufferDays = _loomOptions.FabricBufferDays,
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
        DateTime? fabricRequirementDate,
        DateTime? fibcStart,
        DateTime? fibcEnd,
        DateTime? dispatchDate,
        IReadOnlyList<LoomOrderAllocationLineDto> loomAllocations,
        IReadOnlyList<FibcOrderPlanLineDto> fibcLines)
    {
        var milestones = new List<IntegratedTimelineMilestoneDto>();

        if (loomStart is not null || loomEnd is not null)
        {
            var loomCount = loomAllocations.Select(a => a.LoomNo).Distinct().Count();
            milestones.Add(new IntegratedTimelineMilestoneDto
            {
                Stage = "Loom",
                Label = "Loom weaving",
                StartDate = loomStart,
                EndDate = loomEnd,
                Detail = loomCount > 0 ? $"{loomCount} loom(s), {loomAllocations.Count} segment(s)" : null,
                SortOrder = 1,
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
                Detail = "Target from BOM / dispatch plan",
                SortOrder = 2,
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
                Detail = lineCount > 0 ? $"{lineCount} line(s), {fibcLines.Count} slot(s)" : null,
                SortOrder = 3,
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
                SortOrder = 4,
            });
        }

        return milestones;
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
