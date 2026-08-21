using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningEngine : ILoomPlanningEngine
{
    private const double MeterEpsilon = 0.01;
    private static readonly DateTime MinValidDate = new(2000, 1, 1);

    private readonly ILoomPlanningRepository _repository;
    private readonly PlanningRuntimeContextLoader _runtimeLoader;
    private readonly IPlanningSetupRepository _setup;
    private readonly OrderPlanningRouteService _routeService;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningEngine(
        ILoomPlanningRepository repository,
        PlanningRuntimeContextLoader runtimeLoader,
        IPlanningSetupRepository setup,
        OrderPlanningRouteService routeService,
        IOptions<LoomPlanningOptions> options)
    {
        _repository = repository;
        _runtimeLoader = runtimeLoader;
        _setup = setup;
        _routeService = routeService;
        _options = options.Value;
    }

    public Task<LoomAllotmentResult> AllotAsync(LoomAllotmentRequest request, CancellationToken ct = default) =>
        PlanInternalAsync(request, ct);

    public async Task<LoomAllotmentConfirmResult> ConfirmAllotAsync(LoomAllotmentRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var orderNo = request.OrderNo.Trim();

        if (!_options.AllowConfirmSave)
        {
            return new LoomAllotmentConfirmResult
            {
                Success = false,
                Saved = false,
                Message = "Confirm save is disabled (LoomPlanning:AllowConfirmSave).",
                OrderNo = orderNo,
            };
        }

        var preview = await PlanInternalAsync(request, ct);
        var result = ToConfirmResult(preview);

        if (!preview.Success || preview.ProposedSegments.Count == 0)
            return result;

        if (!preview.FullyAllotted)
        {
            result.Success = false;
            result.Message =
                $"Cannot save: plan is incomplete ({preview.AllottedMeters:N0} of {preview.RequiredMeters:N0} m).";
            return result;
        }

        if (preview.Displacements.Count > 0 && !_options.AllowShiftOnConfirm)
        {
            result.Success = false;
            result.Message =
                $"Cannot save: {preview.Displacements.Count} order(s) must be shifted (cases ii/iii/iv). Enable LoomPlanning:AllowShiftOnConfirm.";
            return result;
        }

        var changeoverDays = CountChangeoverDays(preview.ProposedSegments, preview.Displacements);
        var overLimit = changeoverDays.Where(kv => kv.Value > _options.MaxChangeoversPerDay).ToList();
        if (overLimit.Count > 0)
        {
            result.Success = false;
            result.Message =
                $"Cannot save: {overLimit.Count} day(s) exceed max {_options.MaxChangeoversPerDay} changeover(s)/day (first: {overLimit[0].Key:yyyy-MM-dd} has {overLimit[0].Value}).";
            return result;
        }

        var existing = await _repository.GetExistingAllocationCountAsync(orderNo, ct);
        if (existing > 0 && (!request.ReplaceExisting || !_options.AllowReplaceExistingPlan))
        {
            result.Success = false;
            result.Message =
                $"Order {orderNo} already has {existing} loom allocation row(s). Enable replace or clear existing plan first.";
            return result;
        }

        var route = await _routeService.ResolveAsync(orderNo, ct);
        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? route.FabricSupplyCompanyName
            : ResolveCompany(request.CompanyName);
        var partyName = request.PartyName;
        if (string.IsNullOrWhiteSpace(partyName))
        {
            var ctx = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
            partyName = ctx?.PartyName;
        }

        try
        {
            var distinctDisplacements = preview.Displacements
                .GroupBy(d => $"{d.AllocationId}:{d.LoomNo}:{d.OrderNo}:{d.FromDate:yyyyMMdd}")
                .Select(g => g.First())
                .ToList();

            if (distinctDisplacements.Count > 0)
            {
                var (shifted, inserted) = await _repository.ApplyLoomShiftPlanAsync(
                    orderNo,
                    partyName,
                    preview.ProposedSegments,
                    distinctDisplacements,
                    request.ReplaceExisting && existing > 0,
                    ct);

                result.Saved = true;
                result.RowsInserted = inserted;
                result.RowsDeleted = request.ReplaceExisting && existing > 0 ? existing : 0;
                result.OrdersShifted = distinctDisplacements.Select(d => d.OrderNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                result.Success = true;
                result.Message =
                    $"Saved loom plan for {orderNo}: shifted {result.OrdersShifted} order(s), inserted {inserted} row(s).";
                return result;
            }

            var rowsInserted = await _repository.InsertLoomAllocationsAsync(
                company,
                orderNo,
                partyName,
                preview.ProposedSegments,
                request.ReplaceExisting && existing > 0,
                ct);

            result.Saved = true;
            result.RowsInserted = rowsInserted;
            result.RowsDeleted = request.ReplaceExisting && existing > 0 ? existing : 0;
            result.OrdersShifted = 0;
            result.Success = true;
            result.Message = $"Saved loom plan for {orderNo}: inserted {rowsInserted} row(s) into Prod_LoomAlocationMaster.";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Saved = false;
            result.Message = $"Loom save failed: {ex.Message}";
            return result;
        }
    }

    private async Task<LoomAllotmentResult> PlanInternalAsync(LoomAllotmentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var warnings = new List<string>();
        var orderNo = request.OrderNo.Trim();
        var route = await _routeService.ResolveAsync(orderNo, ct);
        var fibcCompany = route.FibcCompanyName;
        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? route.FabricSupplyCompanyName
            : ResolveCompany(request.CompanyName);

        if (string.IsNullOrWhiteSpace(orderNo))
            return Fail(orderNo, "Order number is required.");

        if (request.ReqGsm <= 0)
            return Fail(orderNo, "Required GSM must be greater than zero.");

        if (request.Size <= 0)
            return Fail(orderNo, "Fabric width (size) must be greater than zero.");

        if (request.RequiredMeters <= 0)
            return Fail(orderNo, "Required meters must be greater than zero.");

        var fabricRequirementDate = request.FabricRequirementDate;
        if (fabricRequirementDate is null || fabricRequirementDate.Value.Date < MinValidDate)
            return Fail(orderNo, "Fabric requirement date is required (FIBC fabric-ready date).");

        var isInterUnitWeave = !string.Equals(company, fibcCompany, StringComparison.OrdinalIgnoreCase);
        var transferDays = isInterUnitWeave ? Math.Max(0, route.TransferBufferDays) : 0;
        if (isInterUnitWeave)
        {
            warnings.Add(
                $"Inter-unit: weaving at {company}, FIBC at {fibcCompany} (+{transferDays} day transfer buffer).");
            if (!string.IsNullOrWhiteSpace(route.AutoDetectedReason))
                warnings.Add(route.AutoDetectedReason);
        }

        var fabricCompletion = fabricRequirementDate.Value.Date
            .AddDays(-_options.FabricBufferDays - transferDays);
        var horizonStart = fabricCompletion.AddDays(-_options.MaxPlanningHorizonDays);
        var allocationLoadFrom = horizonStart.AddDays(-Math.Max(0, _options.AllocationLookbackDays));
        var planningEarliestStart = horizonStart;
        if (planningEarliestStart < DateTime.Today)
            planningEarliestStart = DateTime.Today;

        if (fabricCompletion < planningEarliestStart)
            return Fail(orderNo, $"Fabric completion date {fabricCompletion:yyyy-MM-dd} is before earliest planning window.");

        var runtime = await _runtimeLoader.LoadAsync(company, ct);
        var preferenceChart = await _setup.GetLoomPreferenceChartAsync(company, ct);

        var loomsTask = _repository.GetLoomMasterAsync(company, ct);
        var allocationsTask = _repository.GetPlanningAllocationsAsync(allocationLoadFrom, fabricCompletion, company, ct);
        var ppmTask = _repository.GetPpmSpecsAsync(ct);
        var formulasTask = _repository.GetFormulasAsync(ct);
        await Task.WhenAll(loomsTask, allocationsTask, ppmTask, formulasTask);

        var looms = (await loomsTask).Where(l => !l.IsFrozen).ToList();
        var poolNos = runtime.GetPlanningLoomNos();
        if (runtime.LoomPool.Any(l => l.PoolId.HasValue || l.IncludeInPlanning))
            looms = looms.Where(l => poolNos.Contains(l.LoomNo)).ToList();

        if (looms.Count == 0)
            return Fail(orderNo, $"No active looms in planning pool for company '{company}'. Configure Loom Pool in Planning Setup.");

        var allocations = await allocationsTask;
        var ppmSpecs = await ppmTask;
        var formulas = await formulasTask;

        var timelines = BuildTimelines(looms, allocations, orderNo, planningEarliestStart, fabricCompletion);
        var candidates = new List<SegmentCandidate>();

        foreach (var loom in looms)
        {
            var timeline = timelines[loom.LoomNo];
            var ppm = ResolvePpm(loom, request.ReqGsm, request.Size, ppmSpecs);
            var formula = ResolveFormula(request.Size, request.ReqGsm, formulas);
            var weftMesh = formula?.WeftMesh ?? _options.DefaultWeftMesh;
            var poolEntry = runtime.GetLoomPoolEntry(loom.LoomNo);
            var winder = ParseWinderCategory(poolEntry?.WinderCategory);
            var metersPerDay = LoomMeterCalculator.CalculateMetersPerDay(ppm, weftMesh, _options.DefaultEfficiency, winder);
            if (metersPerDay <= 0)
                continue;

            var loomScore = LoomPreferenceScorer.Score(
                request.ReqGsm,
                request.Size,
                poolEntry,
                loom.LoomSpecification,
                loom.Make,
                preferenceChart);
            EvaluateSimilarForwardCases(loom, timeline, request, fabricCompletion, planningEarliestStart, metersPerDay, formula, candidates, loomScore);
            EvaluateCaseIvScenarios(loom, timeline, request, fabricCompletion, planningEarliestStart, metersPerDay, formula, candidates, loomScore);
            EvaluateBackwardChangeoverCases(loom, timeline, request, fabricCompletion, planningEarliestStart, metersPerDay, formula, candidates, poolEntry, loom, preferenceChart);
        }

        if (candidates.Count == 0)
        {
            warnings.Add("No suitable loom gaps found. Try widening the date range or freeing capacity on looms with matching GSM/width.");
            return new LoomAllotmentResult
            {
                Success = false,
                Message = "No loom capacity found for the requested fabric in the planning window.",
                OrderNo = orderNo,
                ReqGsm = request.ReqGsm,
                Size = request.Size,
                RequiredMeters = request.RequiredMeters,
                FabricBufferDays = _options.FabricBufferDays,
                FabricRequirementDate = fabricRequirementDate,
                FabricCompletionDate = fabricCompletion,
                EarliestStartDate = planningEarliestStart,
                Warnings = warnings,
            };
        }

        candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var proposed = new List<LoomProposedSegmentDto>();
        var displacements = new List<LoomOrderShiftDisplacementDto>();
        var displacementKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remainingMeters = request.RequiredMeters;
        var usedLoomDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (remainingMeters <= MeterEpsilon)
                break;

            if (candidate.Displacement is not null)
            {
                var key = $"{candidate.Displacement.AllocationId}:{candidate.Displacement.LoomNo}:{candidate.Displacement.OrderNo}:{candidate.Displacement.FromDate:yyyyMMdd}";
                if (displacementKeys.Add(key))
                    displacements.Add(candidate.Displacement);
            }

            var segmentMeters = Math.Min(remainingMeters, candidate.MaxMetersInGap);
            var runDays = Math.Min(
                LoomMeterCalculator.DaysForMeters(segmentMeters, candidate.MetersPerDay),
                _options.MaxDaysPerLoomSegment);

            if (runDays <= 0)
                continue;

            var actualMeters = Math.Min(segmentMeters, runDays * candidate.MetersPerDay);
            var fromDate = candidate.FromDate;
            var toDate = candidate.IsBackward
                ? fromDate.AddDays(runDays - 1)
                : fromDate.AddDays(runDays - 1);

            if (candidate.IsBackward)
            {
                fromDate = candidate.ToDate.AddDays(-(runDays - 1));
                toDate = candidate.ToDate;
            }

            var dayKey = $"{candidate.LoomNo}:{fromDate:yyyyMMdd}";
            if (!usedLoomDays.Add(dayKey) && proposed.Any(p => p.LoomNo == candidate.LoomNo))
                continue;

            proposed.Add(new LoomProposedSegmentDto
            {
                LoomNo = candidate.LoomNo,
                LoomCode = candidate.LoomCode,
                LoomSpecification = candidate.LoomSpecification,
                FromDate = fromDate,
                ToDate = toDate,
                PlannedMeters = Math.Round(actualMeters, 2),
                MetersPerDay = Math.Round(candidate.MetersPerDay, 2),
                RunDays = runDays,
                AllotmentCase = LoomFabricMatcher.CaseCode(candidate.Case),
                CaseLabel = LoomFabricMatcher.CaseLabel(candidate.Case),
                FormulaId = candidate.FormulaId,
                ReqGsm = request.ReqGsm,
                Size = request.Size,
            });

            remainingMeters = Math.Round(remainingMeters - actualMeters, 2);
        }

        var allotted = Math.Round(request.RequiredMeters - remainingMeters, 2);
        var fullyAllotted = remainingMeters <= MeterEpsilon;
        var avgMpd = proposed.Count > 0 ? proposed.Average(p => p.MetersPerDay) : 0;

        if (displacements.Count > 0)
        {
            warnings.Add(
                $"{displacements.Count} displacement(s) proposed — confirm will UPDATE blocking rows in Prod_LoomAlocationMaster then insert new segments.");
        }

        var changeoverDays = CountChangeoverDays(proposed, displacements);
        foreach (var (day, count) in changeoverDays.Where(kv => kv.Value > _options.MaxChangeoversPerDay))
        {
            warnings.Add($"Changeover blocked on save: {day:yyyy-MM-dd} has {count} changeover(s) (max {_options.MaxChangeoversPerDay}/day).");
        }

        if (!fullyAllotted)
        {
            warnings.Add($"Could not allot remaining {remainingMeters:N0} m within {_options.MaxDaysPerLoomSegment}-day segment limits.");
        }

        var success = proposed.Count > 0;
        var message = success
            ? fullyAllotted
                ? $"Preview: {proposed.Count} segment(s), {allotted:N0} m on {proposed.Select(p => p.LoomNo).Distinct().Count()} loom(s) by {fabricCompletion:yyyy-MM-dd}."
                : $"Partial preview: {allotted:N0} of {request.RequiredMeters:N0} m allotted."
            : "No segments could be built from available loom gaps.";

        return new LoomAllotmentResult
        {
            Success = success,
            FullyAllotted = fullyAllotted,
            Message = message,
            OrderNo = orderNo,
            ReqGsm = request.ReqGsm,
            Size = request.Size,
            RequiredMeters = request.RequiredMeters,
            AllottedMeters = allotted,
            MetersPerDay = Math.Round(avgMpd, 2),
            FabricBufferDays = _options.FabricBufferDays,
            FabricRequirementDate = fabricRequirementDate,
            FabricCompletionDate = fabricCompletion,
            EarliestStartDate = planningEarliestStart,
            Warnings = warnings,
            ProposedSegments = proposed,
            Displacements = displacements,
        };
    }

    private void EvaluateSimilarForwardCases(
        LoomMasterDto loom,
        LoomTimeline timeline,
        LoomAllotmentRequest request,
        DateTime fabricCompletion,
        DateTime earliestStart,
        double metersPerDay,
        LoomFormulaDto? formula,
        List<SegmentCandidate> candidates,
        int loomPreferenceScore)
    {
        foreach (var gap in timeline.Gaps.Where(g => g.From <= fabricCompletion && g.To >= earliestStart))
        {
            var effectiveFrom = gap.From < earliestStart ? earliestStart : gap.From;
            var effectiveTo = gap.To > fabricCompletion ? fabricCompletion : gap.To;
            if (effectiveTo < effectiveFrom)
                continue;

            var adjacent = timeline.Blocks
                .Where(b => b.EndDate.AddDays(1) == gap.From || b.StartDate == gap.To.AddDays(1))
                .ToList();

            var similarBefore = adjacent.Any(b =>
                b.EndDate.AddDays(1) == gap.From &&
                LoomFabricMatcher.IsSimilarFabric(request.ReqGsm, request.Size, b.ReqGsm, b.Size, _options.GsmMatchTolerance, _options.WidthMatchTolerance));

            var blockingAfter = timeline.Blocks.Any(b =>
                b.StartDate > gap.From &&
                b.StartDate <= fabricCompletion &&
                !string.Equals(b.OrderNo, request.OrderNo, StringComparison.OrdinalIgnoreCase) &&
                !LoomFabricMatcher.IsSimilarFabric(request.ReqGsm, request.Size, b.ReqGsm, b.Size, _options.GsmMatchTolerance, _options.WidthMatchTolerance));

            LoomAllotmentCase caseType;
            LoomOrderShiftDisplacementDto? displacement = null;

            if (similarBefore && !blockingAfter)
                caseType = LoomAllotmentCase.CaseI;
            else if (similarBefore && blockingAfter)
            {
                caseType = LoomAllotmentCase.CaseII;
                var blocker = timeline.Blocks.First(b =>
                    b.StartDate > gap.From && b.StartDate <= fabricCompletion);
                displacement = BuildDisplacement(loom.LoomNo, blocker, gap.To);
            }
            else if (adjacent.Any(b =>
                         b.StartDate == gap.To.AddDays(1) &&
                         !LoomFabricMatcher.IsSimilarFabric(request.ReqGsm, request.Size, b.ReqGsm, b.Size, _options.GsmMatchTolerance, _options.WidthMatchTolerance)))
            {
                caseType = LoomAllotmentCase.CaseIII;
                var follower = adjacent.First(b => b.StartDate == gap.To.AddDays(1));
                displacement = BuildDisplacement(loom.LoomNo, follower, gap.To.AddDays(_options.MaxDaysPerLoomSegment + 1));
            }
            else if (adjacent.Count == 0)
            {
                // Open loom / greenfield gap within the planning window.
                caseType = LoomAllotmentCase.CaseI;
                similarBefore = false;
            }
            else
                continue;

            var gapDays = (effectiveTo - effectiveFrom).Days + 1;
            var maxMeters = gapDays * metersPerDay;
            if (maxMeters <= 0)
                continue;

            candidates.Add(new SegmentCandidate
            {
                Priority = CasePriority(caseType, effectiveFrom, similarBefore) + loomPreferenceScore,
                LoomNo = loom.LoomNo,
                LoomCode = loom.LoomCode,
                LoomSpecification = loom.LoomSpecification,
                FromDate = effectiveFrom,
                ToDate = effectiveTo,
                MaxMetersInGap = maxMeters,
                MetersPerDay = metersPerDay,
                Case = caseType,
                FormulaId = formula?.FormulaId,
                IsBackward = false,
                Displacement = displacement,
            });
        }
    }

    private void EvaluateCaseIvScenarios(
        LoomMasterDto loom,
        LoomTimeline timeline,
        LoomAllotmentRequest request,
        DateTime fabricCompletion,
        DateTime earliestStart,
        double metersPerDay,
        LoomFormulaDto? formula,
        List<SegmentCandidate> candidates,
        int loomPreferenceScore)
    {
        var ordered = timeline.Blocks.OrderBy(b => b.StartDate).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var block = ordered[i];
            if (!LoomFabricMatcher.IsSimilarFabric(
                    request.ReqGsm, request.Size, block.ReqGsm, block.Size,
                    _options.GsmMatchTolerance, _options.WidthMatchTolerance))
                continue;

            var prevEnd = i > 0 ? ordered[i - 1].EndDate : earliestStart.AddDays(-1);
            var nextStart = i < ordered.Count - 1
                ? ordered[i + 1].StartDate
                : fabricCompletion.AddDays(1);

            var beforeStart = prevEnd.AddDays(1);
            var beforeEnd = block.StartDate.AddDays(-1);
            var afterStart = block.EndDate.AddDays(1);
            var afterEnd = nextStart.AddDays(-1);
            if (afterEnd > fabricCompletion)
                afterEnd = fabricCompletion;

            var freeDaysBefore = beforeEnd >= beforeStart ? (beforeEnd - beforeStart).Days + 1 : 0;
            var freeDaysAfter = afterEnd >= afterStart ? (afterEnd - afterStart).Days + 1 : 0;

            if (freeDaysAfter <= 0)
                continue;

            var freeDayScore = freeDaysBefore + freeDaysAfter;
            var metersAfter = freeDaysAfter * metersPerDay;
            var metersBefore = freeDaysBefore * metersPerDay;
            var needsBefore = request.RequiredMeters > metersAfter + MeterEpsilon && freeDaysBefore > 0;

            LoomOrderShiftDisplacementDto? displacement = null;
            if (needsBefore)
            {
                displacement = BuildDisplacement(loom.LoomNo, block, afterEnd.AddDays(1));
                displacement.Reason =
                    "Case iv: similar-fabric block shifted after combined before+after allotment.";
            }

            var casePriority = CasePriority(LoomAllotmentCase.CaseIV, afterStart, true) + loomPreferenceScore - freeDayScore;

            candidates.Add(new SegmentCandidate
            {
                Priority = casePriority,
                LoomNo = loom.LoomNo,
                LoomCode = loom.LoomCode,
                LoomSpecification = loom.LoomSpecification,
                FromDate = afterStart,
                ToDate = afterEnd,
                MaxMetersInGap = metersAfter,
                MetersPerDay = metersPerDay,
                Case = LoomAllotmentCase.CaseIV,
                FormulaId = formula?.FormulaId,
                IsBackward = false,
                Displacement = displacement,
            });

            if (needsBefore)
            {
                candidates.Add(new SegmentCandidate
                {
                    Priority = casePriority + 1,
                    LoomNo = loom.LoomNo,
                    LoomCode = loom.LoomCode,
                    LoomSpecification = loom.LoomSpecification,
                    FromDate = beforeStart,
                    ToDate = beforeEnd,
                    MaxMetersInGap = metersBefore,
                    MetersPerDay = metersPerDay,
                    Case = LoomAllotmentCase.CaseIV,
                    FormulaId = formula?.FormulaId,
                    IsBackward = false,
                    Displacement = displacement,
                });
            }
        }
    }

    private void EvaluateBackwardChangeoverCases(
        LoomMasterDto loom,
        LoomTimeline timeline,
        LoomAllotmentRequest request,
        DateTime fabricCompletion,
        DateTime earliestStart,
        double metersPerDay,
        LoomFormulaDto? formula,
        List<SegmentCandidate> candidates,
        PlanningLoomPoolDto? poolEntry,
        LoomMasterDto loomMaster,
        IReadOnlyList<PlanningLoomPreferenceChartDto> preferenceChart)
    {
        if (timeline.Blocks.Count == 0)
        {
            AddBackwardSegmentCandidate(
                loom,
                timeline,
                request,
                fabricCompletion,
                earliestStart,
                metersPerDay,
                formula,
                candidates,
                poolEntry,
                loomMaster,
                preferenceChart,
                LoomAllotmentCase.CaseI);
            return;
        }

        var lastBlock = timeline.Blocks.OrderBy(b => b.StartDate).Last();
        var loomGsm = lastBlock.ReqGsm;
        var loomWidth = lastBlock.Size;

        var caseType = LoomFabricMatcher.ClassifyChangeoverCase(
            request.ReqGsm, request.Size, loomGsm, loomWidth,
            _options.GsmMatchTolerance, _options.WidthMatchTolerance);

        if (caseType == LoomAllotmentCase.CaseI)
            return;

        AddBackwardSegmentCandidate(
            loom,
            timeline,
            request,
            fabricCompletion,
            earliestStart,
            metersPerDay,
            formula,
            candidates,
            poolEntry,
            loomMaster,
            preferenceChart,
            caseType);
    }

    private void AddBackwardSegmentCandidate(
        LoomMasterDto loom,
        LoomTimeline timeline,
        LoomAllotmentRequest request,
        DateTime fabricCompletion,
        DateTime earliestStart,
        double metersPerDay,
        LoomFormulaDto? formula,
        List<SegmentCandidate> candidates,
        PlanningLoomPoolDto? poolEntry,
        LoomMasterDto loomMaster,
        IReadOnlyList<PlanningLoomPreferenceChartDto> preferenceChart,
        LoomAllotmentCase caseType)
    {
        var changeoverScore = LoomPreferenceScorer.Score(
            request.ReqGsm,
            request.Size,
            poolEntry,
            loomMaster.LoomSpecification,
            loomMaster.Make,
            preferenceChart,
            caseType);

        var freeDays = CountFreeDaysBackward(timeline, fabricCompletion, earliestStart);
        if (freeDays <= 0)
            return;

        var adjustedMetersPerDay = ApplyChangeoverDeduction(metersPerDay, caseType);
        var runDays = Math.Min(freeDays, _options.MaxDaysPerLoomSegment);
        var maxMeters = runDays * adjustedMetersPerDay;
        if (maxMeters <= 0)
            return;

        candidates.Add(new SegmentCandidate
        {
            Priority = CasePriority(caseType, fabricCompletion, false) + changeoverScore,
            LoomNo = loom.LoomNo,
            LoomCode = loom.LoomCode,
            LoomSpecification = loom.LoomSpecification,
            FromDate = fabricCompletion.AddDays(-(runDays - 1)),
            ToDate = fabricCompletion,
            MaxMetersInGap = maxMeters,
            MetersPerDay = adjustedMetersPerDay,
            Case = caseType,
            FormulaId = formula?.FormulaId,
            IsBackward = true,
        });
    }

    private static int CountFreeDaysBackward(LoomTimeline timeline, DateTime fabricCompletion, DateTime earliestStart)
    {
        var count = 0;
        for (var d = fabricCompletion; d >= earliestStart; d = d.AddDays(-1))
        {
            if (timeline.Blocks.Any(b => d >= b.StartDate && d <= b.EndDate))
                break;
            count++;
        }
        return count;
    }

    private static LoomOrderShiftDisplacementDto BuildDisplacement(int loomNo, LoomBlock blocker, DateTime pushTo)
    {
        var span = (blocker.EndDate - blocker.StartDate).Days;
        return new LoomOrderShiftDisplacementDto
        {
            AllocationId = blocker.AllocationId,
            LoomNo = loomNo,
            OrderNo = blocker.OrderNo ?? "",
            PartyName = blocker.PartyName,
            FromDate = blocker.StartDate,
            ToDate = blocker.EndDate,
            NewFromDate = pushTo,
            NewToDate = pushTo.AddDays(Math.Max(span, 0)),
            Reason = "Blocking order must move to free capacity for similar-fabric forward allotment.",
        };
    }

    private static Dictionary<DateTime, int> CountChangeoverDays(
        IReadOnlyList<LoomProposedSegmentDto> segments,
        IReadOnlyList<LoomOrderShiftDisplacementDto> displacements)
    {
        var counts = new Dictionary<DateTime, int>();
        foreach (var seg in segments)
        {
            var day = seg.FromDate.Date;
            counts.TryGetValue(day, out var c);
            counts[day] = c + 1;
        }
        foreach (var d in displacements)
        {
            var day = d.NewFromDate.Date;
            counts.TryGetValue(day, out var c);
            counts[day] = c + 1;
        }
        return counts;
    }

    private static int CasePriority(LoomAllotmentCase c, DateTime startDate, bool similarAdjacent) =>
        c switch
        {
            LoomAllotmentCase.CaseI => 100 + startDate.Day,
            LoomAllotmentCase.CaseIV => 200 + startDate.Day,
            LoomAllotmentCase.CaseII => 300 + startDate.Day,
            LoomAllotmentCase.CaseIII => 310 + startDate.Day,
            LoomAllotmentCase.CaseV => 400 + startDate.Day,
            LoomAllotmentCase.CaseVI => 410 + startDate.Day,
            LoomAllotmentCase.CaseVII => 420 + startDate.Day,
            _ => 500,
        };

    private static LoomWinderCategory ParseWinderCategory(string? category) =>
        category?.Trim() switch
        {
            "FlatDouble" => LoomWinderCategory.FlatDouble,
            "FlatTriple" => LoomWinderCategory.FlatTriple,
            _ => LoomWinderCategory.Tube,
        };

    private static double ApplyChangeoverDeduction(double metersPerDay, LoomAllotmentCase caseType)
    {
        var hoursLost = caseType switch
        {
            LoomAllotmentCase.CaseV => 3.0,
            LoomAllotmentCase.CaseVI => 5.0,
            LoomAllotmentCase.CaseVII => 8.0,
            _ => 0.0,
        };
        if (hoursLost <= 0)
            return metersPerDay;
        return metersPerDay * Math.Max(0.25, 1.0 - hoursLost / 24.0);
    }

    private double ResolvePpm(LoomMasterDto loom, double gsm, double width, IReadOnlyList<LoomPpmSpecDto> specs)
    {
        var spec = specs.FirstOrDefault(s =>
            gsm >= s.GsmFrom && gsm <= s.GsmTo &&
            width >= s.WidthFrom && width <= s.WidthTo &&
            (string.IsNullOrWhiteSpace(s.LoomType) ||
             (loom.LoomSpecification?.Contains(s.LoomType, StringComparison.OrdinalIgnoreCase) ?? false)));

        if (spec is not null && spec.Ppm > 0)
            return spec.Ppm;

        foreach (var kv in _options.DefaultPpmByLoomType)
        {
            if (loom.LoomSpecification?.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) == true ||
                loom.LoomCode?.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) == true)
                return kv.Value;
        }

        return _options.DefaultPpm;
    }

    private static LoomFormulaDto? ResolveFormula(double width, double gsm, IReadOnlyList<LoomFormulaDto> formulas)
    {
        return formulas
            .OrderBy(f => Math.Abs(f.Size - width))
            .ThenBy(f => f.FormulaId)
            .FirstOrDefault(f => Math.Abs(f.Size - width) <= 5);
    }

    private static Dictionary<int, LoomTimeline> BuildTimelines(
        IReadOnlyList<LoomMasterDto> looms,
        IReadOnlyList<LoomAllocationGridItemDto> allocations,
        string excludeOrderNo,
        DateTime planningEarliestStart,
        DateTime fabricCompletion)
    {
        var result = looms.ToDictionary(l => l.LoomNo, l => new LoomTimeline { LoomNo = l.LoomNo });
        var gapHorizonEnd = fabricCompletion.AddDays(Math.Max(14, (fabricCompletion - planningEarliestStart).Days));

        foreach (var group in allocations
                     .Where(a => a.IsActive)
                     .Where(a => !string.Equals(a.OrderNo, excludeOrderNo, StringComparison.OrdinalIgnoreCase))
                     .GroupBy(a => a.LoomNo))
        {
            if (!result.ContainsKey(group.Key))
                continue;

            var timeline = result[group.Key];
            foreach (var row in group.OrderBy(a => a.AllocationDate))
            {
                var end = row.ToDate?.Date ?? row.AllocationDate.Date;
                if (end < row.AllocationDate.Date)
                    end = row.AllocationDate.Date;

                timeline.Blocks.Add(new LoomBlock
                {
                    AllocationId = row.AllocationId,
                    OrderNo = row.OrderNo,
                    PartyName = row.PartyName,
                    StartDate = row.AllocationDate.Date,
                    EndDate = end,
                    ReqGsm = row.ReqGsm ?? 0,
                    Size = row.Size ?? 0,
                });
            }

            timeline.BuildGaps(planningEarliestStart, gapHorizonEnd);
        }

        foreach (var timeline in result.Values)
            timeline.BuildGaps(planningEarliestStart, gapHorizonEnd);

        return result;
    }

    private string ResolveCompany(string? companyName) =>
        string.IsNullOrWhiteSpace(companyName) ? _options.DefaultCompanyName : companyName.Trim();

    private static LoomAllotmentResult Fail(string orderNo, string message) => new()
    {
        Success = false,
        Message = message,
        OrderNo = orderNo,
    };

    private static LoomAllotmentConfirmResult ToConfirmResult(LoomAllotmentResult preview) => new()
    {
        Success = preview.Success,
        FullyAllotted = preview.FullyAllotted,
        Message = preview.Message,
        OrderNo = preview.OrderNo,
        ReqGsm = preview.ReqGsm,
        Size = preview.Size,
        RequiredMeters = preview.RequiredMeters,
        AllottedMeters = preview.AllottedMeters,
        MetersPerDay = preview.MetersPerDay,
        FabricBufferDays = preview.FabricBufferDays,
        FabricRequirementDate = preview.FabricRequirementDate,
        FabricCompletionDate = preview.FabricCompletionDate,
        EarliestStartDate = preview.EarliestStartDate,
        Warnings = preview.Warnings,
        ProposedSegments = preview.ProposedSegments,
        Displacements = preview.Displacements,
        OrdersShifted = 0,
    };

    private sealed class SegmentCandidate
    {
        public int Priority { get; set; }
        public int LoomNo { get; set; }
        public string? LoomCode { get; set; }
        public string? LoomSpecification { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public double MaxMetersInGap { get; set; }
        public double MetersPerDay { get; set; }
        public LoomAllotmentCase Case { get; set; }
        public int? FormulaId { get; set; }
        public bool IsBackward { get; set; }
        public LoomOrderShiftDisplacementDto? Displacement { get; set; }
    }

    private sealed class LoomTimeline
    {
        public int LoomNo { get; set; }
        public List<LoomBlock> Blocks { get; } = [];
        public List<DateGap> Gaps { get; } = [];

        public void BuildGaps(DateTime planningEarliestStart, DateTime gapHorizonEnd)
        {
            Gaps.Clear();
            if (Blocks.Count == 0)
            {
                Gaps.Add(new DateGap { From = planningEarliestStart, To = gapHorizonEnd });
                return;
            }

            var ordered = Blocks.OrderBy(b => b.StartDate).ToList();
            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var gapStart = ordered[i].EndDate.AddDays(1);
                var gapEnd = ordered[i + 1].StartDate.AddDays(-1);
                if (gapEnd >= gapStart)
                    Gaps.Add(new DateGap { From = gapStart, To = gapEnd });
            }

            var trailingStart = ordered[^1].EndDate.AddDays(1);
            Gaps.Add(new DateGap
            {
                From = trailingStart,
                To = trailingStart > gapHorizonEnd ? trailingStart : gapHorizonEnd,
            });
        }
    }

    private sealed class LoomBlock
    {
        public int AllocationId { get; set; }
        public string? OrderNo { get; set; }
        public string? PartyName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double ReqGsm { get; set; }
        public double Size { get; set; }
    }

    private sealed class DateGap
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
