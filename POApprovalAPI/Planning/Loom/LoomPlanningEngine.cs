using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningEngine : ILoomPlanningEngine
{
    private const double MeterEpsilon = 0.01;
    private static readonly DateTime MinValidDate = new(2000, 1, 1);

    private readonly ILoomPlanningRepository _repository;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningEngine(ILoomPlanningRepository repository, IOptions<LoomPlanningOptions> options)
    {
        _repository = repository;
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

        if (preview.Displacements.Count > 0)
        {
            result.Success = false;
            result.Message =
                $"Cannot save yet: {preview.Displacements.Count} order(s) must be shifted first (cases ii/iii/iv). Shift handling will be added in a follow-up; adjust blocking allocations in ERP or replan.";
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

        var company = ResolveCompany(request.CompanyName);
        var partyName = request.PartyName;
        if (string.IsNullOrWhiteSpace(partyName))
        {
            var ctx = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
            partyName = ctx?.PartyName;
        }

        try
        {
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
        var company = ResolveCompany(request.CompanyName);

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

        var fabricCompletion = fabricRequirementDate.Value.Date.AddDays(-_options.FabricBufferDays);
        var earliestStart = fabricCompletion.AddDays(-_options.MaxPlanningHorizonDays);
        if (earliestStart < DateTime.Today)
            earliestStart = DateTime.Today;

        if (fabricCompletion < earliestStart)
            return Fail(orderNo, $"Fabric completion date {fabricCompletion:yyyy-MM-dd} is before earliest planning window.");

        var loomsTask = _repository.GetLoomMasterAsync(company, ct);
        var allocationsTask = _repository.GetPlanningAllocationsAsync(earliestStart, fabricCompletion, company, ct);
        var ppmTask = _repository.GetPpmSpecsAsync(ct);
        var formulasTask = _repository.GetFormulasAsync(ct);
        await Task.WhenAll(loomsTask, allocationsTask, ppmTask, formulasTask);

        var looms = (await loomsTask).Where(l => !l.IsFrozen).ToList();
        if (looms.Count == 0)
            return Fail(orderNo, $"No active looms found for company '{company}'.");

        var allocations = await allocationsTask;
        var ppmSpecs = await ppmTask;
        var formulas = await formulasTask;

        var timelines = BuildTimelines(looms, allocations, orderNo);
        var candidates = new List<SegmentCandidate>();

        foreach (var loom in looms)
        {
            var timeline = timelines[loom.LoomNo];
            var ppm = ResolvePpm(loom, request.ReqGsm, request.Size, ppmSpecs);
            var formula = ResolveFormula(request.Size, request.ReqGsm, formulas);
            var weftMesh = formula?.WeftMesh ?? _options.DefaultWeftMesh;
            var metersPerDay = LoomMeterCalculator.CalculateMetersPerDay(ppm, weftMesh, _options.DefaultEfficiency);
            if (metersPerDay <= 0)
                continue;

            EvaluateSimilarForwardCases(loom, timeline, request, fabricCompletion, earliestStart, metersPerDay, formula, candidates);
            EvaluateBackwardChangeoverCases(loom, timeline, request, fabricCompletion, earliestStart, metersPerDay, formula, candidates);
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
                EarliestStartDate = earliestStart,
                Warnings = warnings,
            };
        }

        candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var proposed = new List<LoomProposedSegmentDto>();
        var displacements = new List<LoomOrderShiftDisplacementDto>();
        var remainingMeters = request.RequiredMeters;
        var usedLoomDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (remainingMeters <= MeterEpsilon)
                break;

            if (candidate.Displacement is not null)
                displacements.Add(candidate.Displacement);

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
                $"{displacements.Count} displacement(s) proposed — confirm save is blocked until shift logic is applied (cases ii/iii/iv).");
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
            EarliestStartDate = earliestStart,
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
        List<SegmentCandidate> candidates)
    {
        foreach (var gap in timeline.Gaps.Where(g => g.From <= fabricCompletion))
        {
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
            else if (timeline.Blocks.Any(b =>
                         LoomFabricMatcher.IsSimilarFabric(request.ReqGsm, request.Size, b.ReqGsm, b.Size, _options.GsmMatchTolerance, _options.WidthMatchTolerance) &&
                         gap.From > b.EndDate && gap.To < timeline.Blocks.Where(x => x.StartDate > b.EndDate).Select(x => x.StartDate).DefaultIfEmpty(fabricCompletion).Min().AddDays(-1)))
            {
                caseType = LoomAllotmentCase.CaseIV;
            }
            else
                continue;

            var gapDays = (gap.To - gap.From).Days + 1;
            var maxMeters = gapDays * metersPerDay;
            if (maxMeters <= 0)
                continue;

            candidates.Add(new SegmentCandidate
            {
                Priority = CasePriority(caseType, gap.From, similarBefore),
                LoomNo = loom.LoomNo,
                LoomCode = loom.LoomCode,
                LoomSpecification = loom.LoomSpecification,
                FromDate = gap.From,
                ToDate = gap.To,
                MaxMetersInGap = maxMeters,
                MetersPerDay = metersPerDay,
                Case = caseType,
                FormulaId = formula?.FormulaId,
                IsBackward = false,
                Displacement = displacement,
            });
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
        List<SegmentCandidate> candidates)
    {
        var lastBlock = timeline.Blocks.LastOrDefault();
        var loomGsm = lastBlock?.ReqGsm ?? request.ReqGsm;
        var loomWidth = lastBlock?.Size ?? request.Size;

        var caseType = LoomFabricMatcher.ClassifyChangeoverCase(
            request.ReqGsm, request.Size, loomGsm, loomWidth,
            _options.GsmMatchTolerance, _options.WidthMatchTolerance);

        if (caseType == LoomAllotmentCase.CaseI)
            return;

        var freeDays = CountFreeDaysBackward(timeline, fabricCompletion, earliestStart);
        if (freeDays <= 0)
            return;

        var runDays = Math.Min(freeDays, _options.MaxDaysPerLoomSegment);
        var maxMeters = runDays * metersPerDay;

        candidates.Add(new SegmentCandidate
        {
            Priority = CasePriority(caseType, fabricCompletion, false) + 10,
            LoomNo = loom.LoomNo,
            LoomCode = loom.LoomCode,
            LoomSpecification = loom.LoomSpecification,
            FromDate = fabricCompletion.AddDays(-(runDays - 1)),
            ToDate = fabricCompletion,
            MaxMetersInGap = maxMeters,
            MetersPerDay = metersPerDay,
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
        string excludeOrderNo)
    {
        var result = looms.ToDictionary(l => l.LoomNo, l => new LoomTimeline { LoomNo = l.LoomNo });

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
                    OrderNo = row.OrderNo,
                    PartyName = row.PartyName,
                    StartDate = row.AllocationDate.Date,
                    EndDate = end,
                    ReqGsm = row.ReqGsm ?? 0,
                    Size = row.Size ?? 0,
                });
            }

            timeline.BuildGaps();
        }

        foreach (var timeline in result.Values)
            timeline.BuildGaps();

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

        public void BuildGaps()
        {
            Gaps.Clear();
            if (Blocks.Count == 0)
            {
                Gaps.Add(new DateGap { From = DateTime.Today, To = DateTime.Today.AddDays(60) });
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

            Gaps.Add(new DateGap { From = ordered[^1].EndDate.AddDays(1), To = ordered[^1].EndDate.AddDays(45) });
        }
    }

    private sealed class LoomBlock
    {
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
