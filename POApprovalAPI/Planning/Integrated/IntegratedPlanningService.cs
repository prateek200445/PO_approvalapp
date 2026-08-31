using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Bom;
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
    private readonly FibcPlanningOptions _fibcOptions;

    public IntegratedPlanningService(
        FibcPlanningService fibc,
        LoomPlanningService loom,
        ExecutionPlanningService execution,
        PlanningRuntimeContextLoader runtimeLoader,
        OrderPlanningRouteService routeService,
        IOptions<LoomPlanningOptions> loomOptions,
        IOptions<FibcPlanningOptions> fibcOptions)
    {
        _fibc = fibc;
        _loom = loom;
        _execution = execution;
        _runtimeLoader = runtimeLoader;
        _routeService = routeService;
        _loomOptions = loomOptions.Value;
        _fibcOptions = fibcOptions.Value;
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

        var accessoryBoard = await _execution.GetAccessoryMaterialsAsync(trimmed, ct);

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
        var componentPlan = await _routeService.ResolvePlanAsync(trimmed, ct);

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
            BomComponents = BuildBomComponents(fabricRequirements, loomAllocations, componentPlan.Components, accessoryBoard.Items),
            Warnings = warnings,
        };
    }

    public Task<FullOrderPlanResult> PreviewFullOrderAsync(string orderNo, CancellationToken ct = default) =>
        RunFullOrderAsync(orderNo, confirm: false, replaceExistingFibc: false, ct);

    public Task<FullOrderPlanResult> ConfirmFullOrderAsync(FullOrderPlanRequest request, CancellationToken ct = default) =>
        RunFullOrderAsync(request.OrderNo, confirm: true, replaceExistingFibc: request.ReplaceExistingFibc, ct);

    private async Task<FullOrderPlanResult> RunFullOrderAsync(
        string orderNo,
        bool confirm,
        bool replaceExistingFibc,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trimmed = (orderNo ?? "").Trim();
        var blockers = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrEmpty(trimmed))
        {
            return new FullOrderPlanResult
            {
                Success = false,
                Message = "Order number is required.",
                Blockers = ["Order number is required."],
            };
        }

        var route = await _routeService.ResolveAsync(trimmed, ct);
        var fibcCtx = await _fibc.GetOrderAllotmentContextAsync(trimmed, ct);
        var accessories = await _execution.GetAccessoryMaterialsAsync(trimmed, ct);

        if (fibcCtx is null)
        {
            return new FullOrderPlanResult
            {
                Success = false,
                OrderNo = trimmed,
                Route = route,
                Accessories = accessories,
                Message = "No marketing invoice or BOM found for this order.",
                Blockers = ["No marketing invoice or BOM found for this order."],
            };
        }

        var dispatch = fibcCtx.DispatchDate;
        var fibcPreview = await _fibc.PreviewAllotmentAsync(new FibcAllotmentRequest
        {
            OrderNo = trimmed,
            CompanyName = route.FibcCompanyName,
            DispatchDate = dispatch,
            Quantity = fibcCtx.Quantity ?? 0,
            BagType = fibcCtx.BagType,
            PartyName = fibcCtx.PartyName,
            MarketingNo = fibcCtx.MarketingNo,
        }, ct);

        foreach (var w in fibcPreview.Warnings)
            warnings.Add(w);

        var fibcStart = fibcPreview.ProposedSlots.Count > 0
            ? fibcPreview.ProposedSlots.Min(s => s.PlanDate.Date)
            : (DateTime?)null;
        var fibcEnd = fibcPreview.ProposedSlots.Count > 0
            ? fibcPreview.ProposedSlots.Max(s => s.PlanDate.Date)
            : (DateTime?)null;

        var allottedPcs = fibcPreview.ProposedSlots.Sum(s => s.Allotted);
        var fibcComplete = fibcPreview.Success
            && fibcPreview.ProposedSlots.Count > 0
            && allottedPcs + 0.5 >= Math.Max(1, fibcPreview.Quantity);

        if (!fibcComplete)
            blockers.Add(string.IsNullOrWhiteSpace(fibcPreview.Message)
                ? "FIBC line slots could not cover the full bag quantity before dispatch."
                : $"FIBC: {fibcPreview.Message}");

        var buffer = fibcPreview.BufferDays > 0 ? fibcPreview.BufferDays : _fibcOptions.DispatchBufferDays;
        var fabricRequirement = fibcStart
            ?? (dispatch is { } d && d.Year >= 2000 ? d.Date.AddDays(-buffer) : (DateTime?)null);

        if (fabricRequirement is null)
            blockers.Add("No dispatch date or FIBC start date — cannot time loom fabric.");

        LoomComponentBatchResult? loomBatch = null;
        DateTime? loomEnd = null;
        if (fabricRequirement is not null)
        {
            loomBatch = await _loom.PreviewAllLoomComponentsAsync(new LoomComponentBatchRequest
            {
                OrderNo = trimmed,
                PartyName = fibcCtx.PartyName,
                FabricRequirementDate = fabricRequirement,
            }, ct);

            foreach (var w in loomBatch.Warnings)
                warnings.Add(w);

            var segmentEnds = loomBatch.Components
                .SelectMany(c => c.ProposedSegments)
                .Select(s => s.ToDate.Date)
                .ToList();
            if (segmentEnds.Count > 0)
                loomEnd = segmentEnds.Max();
            else
            {
                var completions = loomBatch.Components
                    .Select(c => c.FabricCompletionDate)
                    .Where(d => d is { Year: >= 2000 })
                    .Select(d => d!.Value.Date)
                    .ToList();
                if (completions.Count > 0)
                    loomEnd = completions.Max();
            }

            if (loomBatch.LoomEligibleCount > 0 && loomBatch.FullyAllottedCount < loomBatch.LoomEligibleCount)
            {
                blockers.Add(
                    $"Loom: {loomBatch.FullyAllottedCount}/{loomBatch.LoomEligibleCount} fabric heading(s) fully allotted. {loomBatch.Message}");
            }
            else if (loomBatch.LoomEligibleCount == 0)
            {
                warnings.Add("No loom-eligible BOM fabrics (Body/Side/Top/Bottom/Baffle/Spout with meters). FIBC-only plan.");
            }
        }

        var transferDays = route.IsInterUnit ? Math.Max(0, route.TransferBufferDays) : 0;
        DateTime? fabricAtFibc = loomEnd is not null ? loomEnd.Value.AddDays(transferDays) : null;
        var sequenceOk = fabricAtFibc is null || fibcStart is null || fabricAtFibc.Value <= fibcStart.Value;
        if (!sequenceOk)
        {
            blockers.Add(
                $"Sequence: fabric arrives {fabricAtFibc:yyyy-MM-dd} but FIBC sewing starts {fibcStart:yyyy-MM-dd}. " +
                "Need earlier weaving, more looms, or a later dispatch.");
        }

        foreach (var acc in accessories.Items.Where(a =>
                     a.Status is "NotFound" or "Partial"))
        {
            warnings.Add($"Accessory {acc.Heading}: {acc.Status}" + (string.IsNullOrWhiteSpace(acc.Detail) ? "" : $" — {acc.Detail}"));
        }

        var ready = blockers.Count == 0;
        var result = new FullOrderPlanResult
        {
            Success = ready,
            ReadyToConfirm = ready,
            OrderNo = trimmed,
            Route = route,
            DispatchDate = dispatch ?? fibcPreview.DispatchDate,
            FibcStartDate = fibcStart,
            FibcEndDate = fibcEnd,
            FabricRequirementDate = fabricRequirement,
            LoomEndDate = loomEnd,
            FabricAtFibcDate = fabricAtFibc,
            SequenceOk = sequenceOk,
            Fibc = fibcPreview,
            Loom = loomBatch,
            Accessories = accessories,
            Blockers = blockers,
            Warnings = warnings,
            Message = ready
                ? $"Full order plan is feasible: fabric at FIBC by {fabricAtFibc:yyyy-MM-dd}, sewing {fibcStart:yyyy-MM-dd}–{fibcEnd:yyyy-MM-dd}, dispatch {dispatch:yyyy-MM-dd}."
                : string.Join(" ", blockers),
        };

        if (!confirm)
            return result;

        if (!ready)
        {
            result.Saved = false;
            result.Message = "Cannot save: fix blockers first. " + result.Message;
            return result;
        }

        if (loomBatch is { LoomEligibleCount: > 0 })
        {
            var loomSaved = await _loom.ConfirmAllLoomComponentsAsync(new LoomComponentBatchRequest
            {
                OrderNo = trimmed,
                PartyName = fibcCtx.PartyName,
                FabricRequirementDate = fabricRequirement,
            }, ct);
            result.Loom = loomSaved;
            result.LoomRowsInserted = loomSaved.RowsInserted;
            if (loomSaved.FullyAllottedCount < loomSaved.LoomEligibleCount && loomSaved.SavedCount == 0)
            {
                result.Success = false;
                result.Saved = false;
                result.ReadyToConfirm = false;
                result.Message = "Loom confirm did not save a complete fabric plan. FIBC was not written.";
                return result;
            }
        }

        var fibcSaved = await _fibc.ConfirmAllotmentAsync(new FibcAllotmentRequest
        {
            OrderNo = trimmed,
            CompanyName = route.FibcCompanyName,
            DispatchDate = dispatch,
            Quantity = fibcCtx.Quantity ?? 0,
            BagType = fibcCtx.BagType,
            PartyName = fibcCtx.PartyName,
            MarketingNo = fibcCtx.MarketingNo,
            ReplaceExisting = replaceExistingFibc,
        }, ct);
        result.Fibc = fibcSaved;
        result.FibcRowsInserted = fibcSaved.RowsInserted;
        result.Saved = fibcSaved.Saved || result.LoomRowsInserted > 0;
        result.Success = fibcSaved.Saved;
        result.Message = fibcSaved.Saved
            ? $"Saved full order plan: {result.LoomRowsInserted} loom row(s), {fibcSaved.RowsInserted} FIBC slot(s)."
            : fibcSaved.Message;
        return result;
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
                Category = f.Category,
                PlanningKind = f.PlanningKind,
                IsLoomEligible = f.IsLoomEligible,
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

    private static IReadOnlyList<IntegratedBomComponentDto> BuildBomComponents(
        IReadOnlyList<FibcFabricRequirementDto> fabricRequirements,
        IReadOnlyList<LoomOrderAllocationLineDto> loomAllocations,
        IReadOnlyList<PlanningOrderComponentRouteDto> componentRoutes,
        IReadOnlyList<POApprovalAPI.Planning.Execution.Models.AccessoryMaterialStatusDto> materials)
    {
        if (fabricRequirements.Count == 0)
            return Array.Empty<IntegratedBomComponentDto>();

        var routesByHeading = componentRoutes
            .GroupBy(row => BomComponentClassifier.NormalizeHeading(row.Heading), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var materialsByHeading = materials
            .GroupBy(row => BomComponentClassifier.NormalizeHeading(row.Heading), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return fabricRequirements.Select(row =>
        {
            var classified = string.IsNullOrWhiteSpace(row.Category) || row.Category == "Other"
                ? BomComponentClassifier.Classify(row.Heading, row.Gsm, row.FabricSize, row.TotalMtr, row.TotalKg)
                : new BomComponentClassifier.Classification(row.Category, row.PlanningKind, row.IsLoomEligible);

            routesByHeading.TryGetValue(BomComponentClassifier.NormalizeHeading(row.Heading), out var route);
            materialsByHeading.TryGetValue(BomComponentClassifier.NormalizeHeading(row.Heading), out var material);

            string readiness;
            string? detail;
            if (classified.PlanningKind == BomComponentClassifier.KindAdjustment)
            {
                readiness = "Ignored";
                detail = "BOM adjustment row (less/excess/wastage)";
            }
            else if (classified.PlanningKind == BomComponentClassifier.KindAccessory)
            {
                readiness = material?.Status switch
                {
                    "Received" => "Ready",
                    "Partial" => "Partial",
                    "Indented" => "Indented",
                    _ => "Accessory",
                };
                var due = route?.DueDate ?? row.TargetDate;
                var factory = route?.SupplyCompanyName;
                var baseDetail = string.IsNullOrWhiteSpace(factory)
                    ? "Needed at FIBC sewing — not scheduled on looms"
                    : due is not null
                        ? $"Needed at FIBC by {due:yyyy-MM-dd} from {factory}"
                        : $"Needed at FIBC sewing from {factory}";
                detail = string.IsNullOrWhiteSpace(material?.Detail) ? baseDetail : $"{baseDetail}. {material.Detail}";
            }
            else if (classified.IsLoomEligible)
            {
                var planned = IsLoomComponentPlanned(row, classified, loomAllocations);
                readiness = planned ? "Planned" : "Unplanned";
                detail = planned
                    ? route is not null
                        ? $"Matching loom allocation at {route.SupplyCompanyName}"
                        : "Matching loom allocation found"
                    : route is not null
                        ? $"Loom fabric not yet allotted at {route.SupplyCompanyName}"
                        : "Loom fabric not yet allotted";
            }
            else
            {
                readiness = "NotApplicable";
                detail = classified.PlanningKind == BomComponentClassifier.KindLoomFabric
                    ? "Fabric heading without meters/width"
                    : null;
            }

            return new IntegratedBomComponentDto
            {
                Heading = row.Heading,
                Category = classified.Category,
                PlanningKind = classified.PlanningKind,
                IsLoomEligible = classified.IsLoomEligible,
                Gsm = row.Gsm,
                FabricSize = row.FabricSize,
                TotalMtr = row.TotalMtr,
                TotalKg = row.TotalKg,
                TargetDate = row.TargetDate,
                SupplyCompanyName = route?.SupplyCompanyName,
                DueDate = route?.DueDate ?? row.TargetDate,
                IsInterUnit = route?.IsInterUnit ?? false,
                TransferBufferDays = route?.TransferBufferDays ?? 0,
                Readiness = readiness,
                Detail = detail,
                MaterialStatus = material?.Status,
                IndentNo = material?.IndentNo,
                ReceivedQty = material?.ReceivedQty ?? 0,
            };
        }).ToList();
    }

    private static bool IsLoomComponentPlanned(
        FibcFabricRequirementDto row,
        BomComponentClassifier.Classification classified,
        IReadOnlyList<LoomOrderAllocationLineDto> loomAllocations)
    {
        if (loomAllocations.Count == 0)
            return false;

        var heading = BomComponentClassifier.NormalizeHeading(row.Heading);
        if (!string.IsNullOrEmpty(heading)
            && loomAllocations.Any(a =>
                !string.IsNullOrWhiteSpace(a.Remarks)
                && BomComponentClassifier.NormalizeHeading(a.Remarks)
                    .Equals(heading, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var gsm = BomComponentClassifier.ParseGsm(row.Gsm);
        if (gsm <= 0 || row.FabricSize is not > 0)
            return classified.IsLoomEligible && loomAllocations.Count > 0 && classified.Category == "Body";

        return loomAllocations.Any(a =>
            a.ReqGsm is > 0
            && a.Size is > 0
            && Math.Abs(a.ReqGsm.Value - gsm) <= 2
            && Math.Abs(a.Size.Value - row.FabricSize.Value) <= 1.5);
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
