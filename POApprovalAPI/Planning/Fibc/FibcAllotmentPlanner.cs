using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Fibc;

internal static class FibcAllotmentPlanner
{
    private const double SlotEpsilon = 0.001;

    public static FibcAllotmentResult Plan(
        FibcAllotmentRequest request,
        PlanningRuntimeContext runtime,
        FibcOrderAllotmentContextDto? context,
        string erpFamily,
        double quantity,
        DateTime dispatchDate,
        int bufferDays,
        DateTime targetDate,
        DateTime lookbackFrom,
        FibcSlotGridResult grid,
        IReadOnlyList<string> activeShifts,
        IReadOnlyList<string> shiftPreference,
        IReadOnlyDictionary<string, double> heldBySlot,
        IReadOnlyList<FibcLineConfigDto> erpLines)
    {
        var warnings = new List<string>();
        var orderNo = request.OrderNo.Trim();
        var dustLevel = NormalizeDustLevel(request.DustLevel);
        var mode = NormalizeAllotmentMode(request.AllotmentMode);
        var rejectionFactor = 1.0 - Math.Clamp(runtime.Factory.DefaultRejectionPercent, 0, 50) / 100.0;

        var eligiblePortalLines = runtime.Lines
            .Where(l => runtime.LineSupportsBagFamily(l, l.ErpBagType, erpFamily))
            .ToList();

        if (eligiblePortalLines.Count == 0 && erpLines.Count > 0)
        {
            eligiblePortalLines = erpLines
                .Where(l => LinePreferenceHelper.LineSupportsBagFamily(l.BagType, erpFamily))
                .Select(l => new PlanningLineConfigDto
                {
                    LineNo = l.LineNo,
                    ErpBagType = l.BagType,
                    AllowedBagFamilies = PlanningSetupHelpers.InferBagFamilies(l.BagType),
                    CapacityNormal = l.BagCapacity,
                    IsActive = true,
                    PreferenceOrder = l.LineNo,
                })
                .ToList();
        }

        if (eligiblePortalLines.Count == 0)
        {
            return Fail(orderNo, $"No production lines configured for bag family '{BagTypeMapper.ToDisplayLabel(erpFamily)}'.");
        }

        var preferredLineNos = runtime.GetPreferredLines(erpFamily);
        if (preferredLineNos.Count == 0)
            preferredLineNos = eligiblePortalLines.Select(l => l.LineNo).Distinct().OrderBy(n => n).ToList();

        var stitchSpec = FibcStitchSpecResolver.Resolve(runtime.CompanyName, erpFamily, dustLevel, context);
        warnings.AddRange(stitchSpec.Warnings);

        var capacityPerShift = ResolveCapacityPerShift(
            grid.Items, erpFamily, eligiblePortalLines, preferredLineNos, runtime, dustLevel, rejectionFactor, stitchSpec);
        if (capacityPerShift <= 0)
            return Fail(orderNo, "Could not determine shift capacity from planning grid or line setup.");

        var slotsRequired = quantity / capacityPerShift;
        var backlogRemaining = runtime.BacklogByLineShift.ToDictionary(
            kv => kv.Key,
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        if (backlogRemaining.Values.Sum() > SlotEpsilon)
            warnings.Add($"Open backlog reserves {backlogRemaining.Values.Sum():N0} pcs on line+shift before new orders.");

        var proposed = mode == "SlotWise"
            ? AllotSlotWise(orderNo, context, erpFamily, quantity, targetDate, lookbackFrom, grid.Items, activeShifts,
                shiftPreference, heldBySlot, eligiblePortalLines, preferredLineNos, runtime, dustLevel, rejectionFactor,
                backlogRemaining, stitchSpec)
            : AllotOrderWise(orderNo, context, erpFamily, quantity, targetDate, lookbackFrom, grid.Items, activeShifts,
                shiftPreference, heldBySlot, eligiblePortalLines, preferredLineNos, runtime, dustLevel, rejectionFactor,
                backlogRemaining, stitchSpec);

        var remainingQty = Math.Round(quantity - proposed.Sum(p => p.Allotted), 2);
        if (remainingQty > SlotEpsilon && heldBySlot.Count > 0)
            warnings.Add("Some capacity may be reserved by active quotation holds.");

        if (remainingQty > SlotEpsilon)
        {
            warnings.Add(
                $"Could not allot remaining {remainingQty:N0} pcs between {lookbackFrom:yyyy-MM-dd} and {targetDate:yyyy-MM-dd}. Free more capacity or extend the lookback window.");
        }

        var success = proposed.Count > 0;
        var fullyAllotted = remainingQty <= SlotEpsilon;
        var modeLabel = mode == "OrderWise" ? "order-wise" : "slot-wise";
        var specNote = stitchSpec.UsedExcelTargets
            ? $" Spec rate {stitchSpec.BottleneckBagsPerShift:N0} bags/shift ({stitchSpec.FactoryLabel}, bottleneck {stitchSpec.BottleneckActivity}); assignment lot {stitchSpec.AssignmentLotPcs:N0} pcs (LCM of stitch jobs — not per-shift capacity)."
            : "";

        var message = success
            ? fullyAllotted
                ? $"Preview ({modeLabel}): {proposed.Count} slot(s) proposed ({quantity:N0} pcs) ending by {targetDate:yyyy-MM-dd} ({bufferDays}-day buffer before dispatch).{specNote}"
                : $"Partial preview ({modeLabel}): {proposed.Count} slot(s) proposed but {remainingQty:N0} pcs still unallocated.{specNote}"
            : "No free slots found in the lookback window for this bag type.";

        return new FibcAllotmentResult
        {
            Success = success,
            Message = message,
            OrderNo = orderNo,
            BagType = erpFamily,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(erpFamily),
            Quantity = quantity,
            CapacityPerShift = capacityPerShift,
            SlotsRequired = Math.Round(slotsRequired, 3),
            BufferDays = bufferDays,
            DispatchDate = dispatchDate,
            TargetCompletionDate = targetDate,
            AllotmentMode = mode,
            DustLevel = dustLevel,
            RejectionPercentApplied = runtime.Factory.DefaultRejectionPercent,
            Warnings = warnings,
            ProposedSlots = proposed,
            StitchSpec = stitchSpec,
        };
    }

    private static List<FibcSlotGridItemDto> AllotOrderWise(
        string orderNo,
        FibcOrderAllotmentContextDto? context,
        string erpFamily,
        double quantity,
        DateTime targetDate,
        DateTime lookbackFrom,
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        IReadOnlyList<string> activeShifts,
        IReadOnlyList<string> shiftPreference,
        IReadOnlyDictionary<string, double> heldBySlot,
        IReadOnlyList<PlanningLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        PlanningRuntimeContext runtime,
        string dustLevel,
        double rejectionFactor,
        Dictionary<string, double> backlogRemaining,
        FibcStitchSpecDto stitchSpec)
    {
        var proposed = new List<FibcSlotGridItemDto>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remainingQty = quantity;

        foreach (var lineNo in preferredLineNos)
        {
            if (remainingQty <= SlotEpsilon)
                break;

            if (!eligibleLines.Any(l => l.LineNo == lineNo))
                continue;

            for (var day = targetDate; day >= lookbackFrom && remainingQty > SlotEpsilon; day = day.AddDays(-1))
            {
                var daySlots = gridItems
                    .Where(s => s.PlanDate.Date == day.Date)
                    .Where(s => ParseLineNo(s.LineNo) == lineNo)
                    .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
                    .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
                    .Where(s => !usedKeys.Contains(SlotKey(s)))
                    .OrderBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, lineNo, s.Shift, shiftPreference))
                    .ToList();

                foreach (var slot in daySlots)
                {
                    if (remainingQty <= SlotEpsilon)
                        break;

                    var allotQty = TryAllotSlot(slot, lineNo, eligibleLines, runtime, dustLevel, rejectionFactor,
                        heldBySlot, backlogRemaining, remainingQty, stitchSpec, out var item);
                    if (allotQty <= SlotEpsilon)
                        continue;

                    item.PartyName = context?.PartyName;
                    item.OrderNo = orderNo;
                    item.MarketingNo = context?.MarketingNo;
                    proposed.Add(item);
                    usedKeys.Add(SlotKey(slot));
                    remainingQty = Math.Round(remainingQty - allotQty, 2);
                }
            }
        }

        return proposed;
    }

    private static List<FibcSlotGridItemDto> AllotSlotWise(
        string orderNo,
        FibcOrderAllotmentContextDto? context,
        string erpFamily,
        double quantity,
        DateTime targetDate,
        DateTime lookbackFrom,
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        IReadOnlyList<string> activeShifts,
        IReadOnlyList<string> shiftPreference,
        IReadOnlyDictionary<string, double> heldBySlot,
        IReadOnlyList<PlanningLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        PlanningRuntimeContext runtime,
        string dustLevel,
        double rejectionFactor,
        Dictionary<string, double> backlogRemaining,
        FibcStitchSpecDto stitchSpec)
    {
        var proposed = new List<FibcSlotGridItemDto>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineRotation = 0;
        var remainingQty = quantity;

        for (var day = targetDate; day >= lookbackFrom && remainingQty > SlotEpsilon; day = day.AddDays(-1))
        {
            var daySlots = gridItems
                .Where(s => s.PlanDate.Date == day.Date)
                .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
                .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
                .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
                .Where(s => !usedKeys.Contains(SlotKey(s)))
                .OrderBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, ParseLineNo(s.LineNo), s.Shift, shiftPreference))
                .ThenBy(s => RotatedLineRank(preferredLineNos, ParseLineNo(s.LineNo), lineRotation))
                .ToList();

            foreach (var slot in daySlots)
            {
                if (remainingQty <= SlotEpsilon)
                    break;

                var lineNo = ParseLineNo(slot.LineNo);
                var allotQty = TryAllotSlot(slot, lineNo, eligibleLines, runtime, dustLevel, rejectionFactor,
                    heldBySlot, backlogRemaining, remainingQty, stitchSpec, out var item);
                if (allotQty <= SlotEpsilon)
                    continue;

                item.PartyName = context?.PartyName;
                item.OrderNo = orderNo;
                item.MarketingNo = context?.MarketingNo;
                proposed.Add(item);
                usedKeys.Add(SlotKey(slot));
                lineRotation++;
                remainingQty = Math.Round(remainingQty - allotQty, 2);
            }
        }

        return proposed;
    }

    private static double TryAllotSlot(
        FibcSlotGridItemDto slot,
        int lineNo,
        IReadOnlyList<PlanningLineConfigDto> eligibleLines,
        PlanningRuntimeContext runtime,
        string dustLevel,
        double rejectionFactor,
        IReadOnlyDictionary<string, double> heldBySlot,
        Dictionary<string, double> backlogRemaining,
        double remainingQty,
        FibcStitchSpecDto stitchSpec,
        out FibcSlotGridItemDto result)
    {
        result = slot;
        var portalLine = eligibleLines.FirstOrDefault(l => l.LineNo == lineNo);
        var lineCap = portalLine is not null
            ? runtime.GetLineCapacity(portalLine, dustLevel)
            : (int)slot.Capacity;
        if (lineCap <= 0)
            lineCap = (int)slot.Capacity;
        var baseCap = FibcStitchSpecResolver.EffectiveShiftCapacity(lineCap, stitchSpec);

        var teamFactor = runtime.GetTeamFactor(lineNo, slot.Shift, portalLine?.TeamNo);
        var downtimeFactor = runtime.GetDowntimeFactor(slot.PlanDate, lineNo, slot.Shift);
        var effectiveCap = baseCap * teamFactor * rejectionFactor * downtimeFactor;

        var held = heldBySlot.GetValueOrDefault(ReservationKey(slot.PlanDate, slot.LineNo, slot.Shift));
        var gridRemaining = Math.Max(0, slot.Remaining - held);

        var lineShiftKey = PlanningRuntimeContext.LineShiftKey(lineNo, slot.Shift);
        var backlogReserve = backlogRemaining.GetValueOrDefault(lineShiftKey);
        if (backlogReserve > SlotEpsilon)
        {
            var consume = Math.Min(backlogReserve, effectiveCap);
            backlogRemaining[lineShiftKey] = Math.Round(backlogReserve - consume, 2);
            gridRemaining = Math.Max(0, gridRemaining - consume);
        }

        var maxAllot = Math.Min(effectiveCap, gridRemaining);
        var allotQty = Math.Round(Math.Min(remainingQty, maxAllot), 2);
        if (allotQty <= SlotEpsilon)
            return 0;

        var isPartial = allotQty < effectiveCap - SlotEpsilon;
        var allocatedPercent = effectiveCap > 0 ? Math.Round(allotQty / effectiveCap * 100, 2) : 100d;

        result = new FibcSlotGridItemDto
        {
            CompanyName = slot.CompanyName,
            BagType = slot.BagType,
            BagTypeLabel = slot.BagTypeLabel,
            LineNo = slot.LineNo,
            PlanDate = slot.PlanDate,
            Allotted = allotQty,
            Capacity = Math.Round(effectiveCap, 2),
            Remaining = Math.Max(0, gridRemaining - allotQty),
            AllocatedPercent = allocatedPercent,
            Shift = slot.Shift,
            TransId = slot.TransId,
            Efficiency = slot.Efficiency,
            UtilizationPercent = effectiveCap > 0 ? Math.Round(allotQty / effectiveCap * 100, 2) : 0,
            OccupancyStatus = isPartial ? "partial" : "full",
        };

        return allotQty;
    }

    private static double ResolveCapacityPerShift(
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        string erpFamily,
        IReadOnlyList<PlanningLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        PlanningRuntimeContext runtime,
        string dustLevel,
        double rejectionFactor,
        FibcStitchSpecDto stitchSpec)
    {
        var samples = new List<double>();
        foreach (var slot in gridItems.Where(s =>
                     BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase)))
        {
            var lineNo = ParseLineNo(slot.LineNo);
            if (!eligibleLines.Any(l => l.LineNo == lineNo))
                continue;

            var portalLine = eligibleLines.First(l => l.LineNo == lineNo);
            var slotLineCap = runtime.GetLineCapacity(portalLine, dustLevel);
            if (slotLineCap <= 0)
                slotLineCap = (int)slot.Capacity;
            var baseCap = FibcStitchSpecResolver.EffectiveShiftCapacity(slotLineCap, stitchSpec);

            var teamFactor = runtime.GetTeamFactor(lineNo, slot.Shift, portalLine.TeamNo);
            var downtimeFactor = runtime.GetDowntimeFactor(slot.PlanDate, lineNo, slot.Shift);
            var effective = baseCap * teamFactor * rejectionFactor * downtimeFactor;
            if (effective > 0)
                samples.Add(effective);
        }

        if (samples.Count > 0)
            return samples.GroupBy(c => Math.Round(c, 0)).OrderByDescending(g => g.Count()).First().Key;

        var lineCap = eligibleLines
            .Where(l => preferredLineNos.Contains(l.LineNo))
            .Select(l => (double)FibcStitchSpecResolver.EffectiveShiftCapacity(runtime.GetLineCapacity(l, dustLevel), stitchSpec))
            .FirstOrDefault(c => c > 0);

        if (lineCap > 0)
            return lineCap;

        return eligibleLines.Max(l =>
            (double)FibcStitchSpecResolver.EffectiveShiftCapacity(runtime.GetLineCapacity(l, dustLevel), stitchSpec));
    }

    private static string NormalizeAllotmentMode(string? mode)
    {
        if (string.Equals(mode, "SlotWise", StringComparison.OrdinalIgnoreCase))
            return "SlotWise";
        return "OrderWise";
    }

    private static string NormalizeDustLevel(string? dust)
    {
        if (string.IsNullOrWhiteSpace(dust))
            return "Normal";
        var d = dust.Trim();
        if (d.StartsWith("Single", StringComparison.OrdinalIgnoreCase)) return "Single";
        if (d.StartsWith("Double", StringComparison.OrdinalIgnoreCase)) return "Double";
        if (d.StartsWith("Triple", StringComparison.OrdinalIgnoreCase)) return "Triple";
        return "Normal";
    }

    private static FibcAllotmentResult Fail(string orderNo, string message) => new()
    {
        Success = false,
        Message = message,
        OrderNo = orderNo,
    };

    private static bool IsEligibleLine(string lineNoText, IReadOnlyList<PlanningLineConfigDto> eligibleLines)
    {
        var lineNo = ParseLineNo(lineNoText);
        return eligibleLines.Any(l => l.LineNo == lineNo);
    }

    private static int ParseLineNo(string lineNoText)
    {
        var digits = new string(lineNoText.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : -1;
    }

    private static string SlotKey(FibcSlotGridItemDto slot) =>
        $"{slot.PlanDate:yyyy-MM-dd}|{slot.LineNo}|{slot.Shift}|{slot.BagType}";

    private static string ReservationKey(DateTime planDate, string lineNo, string shift) =>
        $"{planDate:yyyy-MM-dd}|{lineNo}|{shift}";

    private static int RotatedLineRank(IReadOnlyList<int> preferredLines, int lineNo, int rotation)
    {
        var index = preferredLines.Count == 0 ? -1 : preferredLines.ToList().IndexOf(lineNo);
        if (index < 0)
            return 100;
        return (index - (rotation % preferredLines.Count) + preferredLines.Count) % preferredLines.Count;
    }
}
