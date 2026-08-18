using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public interface IFibcCriticalShiftEngine
{
    Task<FibcCriticalShiftResult> PreviewCriticalShiftAsync(FibcCriticalShiftRequest request, CancellationToken ct = default);

    Task<FibcCriticalShiftConfirmResult> ConfirmCriticalShiftAsync(FibcCriticalShiftRequest request, CancellationToken ct = default);
}

public sealed class FibcCriticalShiftEngine : IFibcCriticalShiftEngine
{
    private const double SlotEpsilon = 0.001;
    private const int MaxLookbackDays = 120;

    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcPlanningEngine _planningEngine;
    private readonly IFibcQuotationHoldRepository _holdRepository;
    private readonly FibcPlanningOptions _options;

    public FibcCriticalShiftEngine(
        IFibcPlanningRepository repository,
        IFibcPlanningEngine planningEngine,
        IFibcQuotationHoldRepository holdRepository,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _planningEngine = planningEngine;
        _holdRepository = holdRepository;
        _options = options.Value;
    }

    public async Task<FibcCriticalShiftResult> PreviewCriticalShiftAsync(
        FibcCriticalShiftRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_options.CriticalShiftEnabled)
        {
            return Fail(request.OrderNo, "Critical order shifting is disabled (FibcPlanning:CriticalShiftEnabled).");
        }

        var pinToTargetDate = request.PinToTargetDate;
        var baseRequest = ToAllotmentRequest(request);
        var initialPreview = await _planningEngine.AllotOrderAsync(baseRequest, ct);

        if (!initialPreview.Success)
            return FromAllotment(initialPreview, shiftsRequired: false, displacements: [], pinToTargetDate);

        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();
        var orderNo = request.OrderNo.Trim();
        var erpFamily = initialPreview.BagType;
        var targetDate = initialPreview.TargetCompletionDate ?? DateTime.Today;
        var lookbackFrom = targetDate.AddDays(-MaxLookbackDays);
        var forwardDays = _options.CriticalShiftMaxForwardDays > 0 ? _options.CriticalShiftMaxForwardDays : 60;
        var gridDateTo = targetDate.AddDays(forwardDays);

        var lines = await _repository.GetLineConfigAsync(company, ct);
        var eligibleLines = lines
            .Where(l => LinePreferenceHelper.LineSupportsBagFamily(l.BagType, erpFamily))
            .ToList();
        var preferredLineNos = LinePreferenceHelper.GetPreferredLines(erpFamily);

        var grid = await _repository.GetSlotGridAsync(lookbackFrom, gridDateTo, company, ct);
        var activeShifts = grid.Items
            .Where(s => s.PlanDate.Date <= targetDate.Date)
            .Select(s => s.Shift)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var holdReservations = _options.QuotationHoldEnabled
            ? await _holdRepository.GetActiveHoldReservationsAsync(company, lookbackFrom, gridDateTo, ct: ct)
            : Array.Empty<FibcHoldReservationDto>();
        var heldBySlot = BuildHeldQtyMap(holdReservations);

        var virtualRemaining = grid.Items.ToDictionary(
            s => SlotKey(s.PlanDate, s.LineNo, s.Shift),
            s => Math.Max(0, s.Remaining - heldBySlot.GetValueOrDefault(ReservationKey(s.PlanDate, s.LineNo, s.Shift))),
            StringComparer.OrdinalIgnoreCase);

        var displacements = new List<FibcOrderShiftDisplacementDto>();
        var movedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>(initialPreview.Warnings);

        if (pinToTargetDate)
        {
            warnings.Add(
                $"Pin to target date is enabled — slots must fall on target completion date ({targetDate:yyyy-MM-dd}) only.");
        }
        else
        {
            var initialRemaining = RemainingQty(initialPreview);
            if (initialRemaining <= SlotEpsilon)
                return FromAllotment(initialPreview, shiftsRequired: false, displacements: [], pinToTargetDate: false);
        }

        var blockingCandidates = grid.Items
            .Where(s => s.PlanDate.Date >= lookbackFrom.Date && s.PlanDate.Date <= targetDate.Date)
            .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
            .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
            .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
            .Where(s => !string.IsNullOrWhiteSpace(s.OrderNo))
            .Where(s => !s.OrderNo!.Equals(orderNo, StringComparison.OrdinalIgnoreCase))
            .Where(s => GetVirtualRemaining(virtualRemaining, s.PlanDate, s.LineNo, s.Shift) <= SlotEpsilon)
            .OrderByDescending(s => s.PlanDate)
            .ThenBy(s => s.LineNo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pinToTargetDate)
            blockingCandidates = blockingCandidates.Where(s => s.PlanDate.Date == targetDate.Date).ToList();

        if (pinToTargetDate)
        {
            var targetBeforeShift = AllotOnVirtualGrid(
                orderNo,
                company,
                erpFamily,
                initialPreview.Quantity,
                targetDate,
                lookbackFrom,
                grid.Items,
                CloneVirtualRemaining(virtualRemaining),
                eligibleLines,
                preferredLineNos,
                activeShifts,
                initialPreview.CapacityPerShift,
                contextParty: request.PartyName,
                targetDateOnly: true);

            var targetShortfall = RemainingQty(initialPreview.Quantity, targetBeforeShift);
            if (targetShortfall <= SlotEpsilon)
            {
                return new FibcCriticalShiftResult
                {
                    Success = true,
                    ShiftsRequired = false,
                    FullyAllotted = true,
                    Message =
                        $"Critical preview (target date only): {targetBeforeShift.Count} slot(s) on {targetDate:yyyy-MM-dd}.",
                    OrderNo = orderNo,
                    BagType = erpFamily,
                    BagTypeLabel = initialPreview.BagTypeLabel,
                    Quantity = initialPreview.Quantity,
                    CapacityPerShift = initialPreview.CapacityPerShift,
                    BufferDays = initialPreview.BufferDays,
                    DispatchDate = initialPreview.DispatchDate,
                    TargetCompletionDate = initialPreview.TargetCompletionDate,
                    PinToTargetDate = true,
                    Warnings = warnings,
                    ProposedSlots = targetBeforeShift,
                    Displacements = [],
                };
            }

            warnings.Add(
                $"Pin to target date: {targetShortfall:N0} pcs cannot fit on {targetDate:yyyy-MM-dd} without shifting blockers.");
        }

        foreach (var blocker in blockingCandidates)
        {
            if (pinToTargetDate)
            {
                if (TargetDateShortfall(
                        initialPreview.Quantity,
                        orderNo,
                        company,
                        erpFamily,
                        targetDate,
                        lookbackFrom,
                        grid.Items,
                        virtualRemaining,
                        eligibleLines,
                        preferredLineNos,
                        activeShifts,
                        initialPreview.CapacityPerShift,
                        request.PartyName) <= SlotEpsilon)
                    break;
            }
            else if (RemainingAfterDisplacements(initialPreview.Quantity, displacements, initialPreview.ProposedSlots) <= SlotEpsilon)
            {
                break;
            }

            var blockerKey = SlotKey(blocker.PlanDate, blocker.LineNo, blocker.Shift);
            if (movedKeys.Contains(blockerKey))
                continue;

            var qtyToMove = blocker.Allotted > SlotEpsilon ? blocker.Allotted : blocker.Capacity;
            if (qtyToMove <= SlotEpsilon)
                continue;

            var alternative = FindForwardAlternativeSlot(
                grid.Items,
                virtualRemaining,
                erpFamily,
                eligibleLines,
                preferredLineNos,
                activeShifts,
                blocker.LineNo,
                blocker.Shift,
                blocker.PlanDate,
                qtyToMove,
                targetDate);

            if (alternative is null)
            {
                warnings.Add(
                    $"Could not relocate order {blocker.OrderNo} from {blocker.PlanDate:yyyy-MM-dd} line {blocker.LineNo} shift {blocker.Shift}.");
                continue;
            }

            displacements.Add(new FibcOrderShiftDisplacementDto
            {
                OrderNo = blocker.OrderNo!,
                PartyName = blocker.PartyName,
                BagType = blocker.BagType,
                BagTypeLabel = blocker.BagTypeLabel,
                FromLineNo = blocker.LineNo,
                FromPlanDate = blocker.PlanDate,
                FromShift = blocker.Shift,
                ToLineNo = alternative.LineNo,
                ToPlanDate = alternative.PlanDate,
                ToShift = alternative.Shift,
                Qty = qtyToMove,
                Capacity = alternative.Capacity,
                AllocatedPercent = alternative.Capacity > 0
                    ? Math.Round(qtyToMove / alternative.Capacity * 100, 2)
                    : blocker.AllocatedPercent,
                MarketingNo = blocker.MarketingNo,
            });

            ApplyVirtualMove(virtualRemaining, blocker.PlanDate, blocker.LineNo, blocker.Shift, qtyToMove, add: true);
            ApplyVirtualMove(virtualRemaining, alternative.PlanDate, alternative.LineNo, alternative.Shift, qtyToMove, add: false);
            movedKeys.Add(blockerKey);
        }

        var proposed = AllotOnVirtualGrid(
            orderNo,
            company,
            erpFamily,
            initialPreview.Quantity,
            targetDate,
            lookbackFrom,
            grid.Items,
            virtualRemaining,
            eligibleLines,
            preferredLineNos,
            activeShifts,
            initialPreview.CapacityPerShift,
            contextParty: request.PartyName,
            targetDateOnly: pinToTargetDate);

        var remaining = RemainingQty(initialPreview.Quantity, proposed);
        var fullyAllotted = remaining <= SlotEpsilon;
        var shiftsRequired = displacements.Count > 0;

        if (!fullyAllotted && displacements.Count == 0)
        {
            warnings.Add(
                $"Critical order still short by {remaining:N0} pcs and no blocking orders could be shifted. Free capacity manually or extend the date window.");
        }
        else if (!fullyAllotted)
        {
            warnings.Add(
                $"After shifting {displacements.Count} blocking slot(s), {remaining:N0} pcs still unallocated.");
        }

        var success = proposed.Count > 0 && (fullyAllotted || shiftsRequired);
        var message = fullyAllotted
            ? shiftsRequired
                ? $"Critical preview: {proposed.Count} slot(s) after shifting {displacements.Count} blocking order(s)."
                : initialPreview.Message
            : proposed.Count > 0
                ? $"Partial critical preview: {proposed.Count} slot(s) proposed but {remaining:N0} pcs still unallocated."
                : "Could not build a critical shift plan.";

        return new FibcCriticalShiftResult
        {
            Success = success,
            ShiftsRequired = shiftsRequired,
            FullyAllotted = fullyAllotted,
            Message = message,
            OrderNo = orderNo,
            BagType = erpFamily,
            BagTypeLabel = initialPreview.BagTypeLabel,
            Quantity = initialPreview.Quantity,
            CapacityPerShift = initialPreview.CapacityPerShift,
            BufferDays = initialPreview.BufferDays,
            DispatchDate = initialPreview.DispatchDate,
            TargetCompletionDate = initialPreview.TargetCompletionDate,
            PinToTargetDate = pinToTargetDate,
            Warnings = warnings,
            ProposedSlots = proposed,
            Displacements = displacements,
        };
    }

    public async Task<FibcCriticalShiftConfirmResult> ConfirmCriticalShiftAsync(
        FibcCriticalShiftRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var orderNo = request.OrderNo.Trim();

        if (!_options.AllowConfirmSave)
        {
            return new FibcCriticalShiftConfirmResult
            {
                Success = false,
                Saved = false,
                Message = "Confirm save is disabled (FibcPlanning:AllowConfirmSave).",
                OrderNo = orderNo,
            };
        }

        var preview = await PreviewCriticalShiftAsync(request, ct);
        var result = ToConfirmResult(preview);

        if (!preview.Success || preview.ProposedSlots.Count == 0)
            return result;

        if (!preview.FullyAllotted)
        {
            result.Success = false;
            result.Message =
                $"Cannot save: critical plan is incomplete ({preview.ProposedSlots.Sum(s => s.Allotted):N0} of {preview.Quantity:N0} pcs).";
            return result;
        }

        var existing = await _repository.GetExistingAllocationCountAsync(orderNo, ct);
        if (existing > 0 && (!request.ReplaceExisting || !_options.AllowReplaceExistingPlan))
        {
            result.Success = false;
            result.Message =
                $"Order {orderNo} already has {existing} allocation row(s). Enable replace or clear existing plan first.";
            return result;
        }

        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();

        var context = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
        var partyName = FirstNonEmpty(request.PartyName, context?.PartyName);
        var marketingNo = FirstNonEmpty(request.MarketingNo, context?.MarketingNo);

        try
        {
            var rowsInserted = await _repository.ApplyCriticalShiftPlanAsync(
                company,
                orderNo,
                partyName,
                marketingNo,
                preview.ProposedSlots,
                preview.Displacements,
                request.ReplaceExisting && existing > 0,
                ct);

            result.Saved = true;
            result.RowsInserted = rowsInserted;
            result.RowsDeleted = preview.Displacements.Count + (request.ReplaceExisting && existing > 0 ? existing : 0);
            result.OrdersShifted = preview.Displacements.Select(d => d.OrderNo).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            result.Success = true;
            result.Message =
                $"Saved critical plan for {orderNo}: shifted {result.OrdersShifted} order(s), inserted {rowsInserted} row(s).";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Saved = false;
            result.Message = $"Critical save failed: {ex.Message}";
            return result;
        }
    }

    private static FibcCriticalShiftResult FromAllotment(
        FibcAllotmentResult preview,
        bool shiftsRequired,
        IReadOnlyList<FibcOrderShiftDisplacementDto> displacements,
        bool pinToTargetDate) => new()
    {
        Success = preview.Success,
        ShiftsRequired = shiftsRequired,
        FullyAllotted = RemainingQty(preview) <= SlotEpsilon,
        Message = preview.Message,
        OrderNo = preview.OrderNo,
        BagType = preview.BagType,
        BagTypeLabel = preview.BagTypeLabel,
        Quantity = preview.Quantity,
        CapacityPerShift = preview.CapacityPerShift,
        BufferDays = preview.BufferDays,
        DispatchDate = preview.DispatchDate,
        TargetCompletionDate = preview.TargetCompletionDate,
        PinToTargetDate = pinToTargetDate,
        Warnings = preview.Warnings,
        ProposedSlots = preview.ProposedSlots,
        Displacements = displacements,
    };

    private static FibcCriticalShiftConfirmResult ToConfirmResult(FibcCriticalShiftResult preview) => new()
    {
        Success = preview.Success,
        ShiftsRequired = preview.ShiftsRequired,
        FullyAllotted = preview.FullyAllotted,
        Message = preview.Message,
        OrderNo = preview.OrderNo,
        BagType = preview.BagType,
        BagTypeLabel = preview.BagTypeLabel,
        Quantity = preview.Quantity,
        CapacityPerShift = preview.CapacityPerShift,
        BufferDays = preview.BufferDays,
        DispatchDate = preview.DispatchDate,
        TargetCompletionDate = preview.TargetCompletionDate,
        PinToTargetDate = preview.PinToTargetDate,
        Warnings = preview.Warnings,
        ProposedSlots = preview.ProposedSlots,
        Displacements = preview.Displacements,
    };

    private static FibcAllotmentRequest ToAllotmentRequest(FibcCriticalShiftRequest request) => new()
    {
        OrderNo = request.OrderNo,
        CompanyName = request.CompanyName,
        DispatchDate = request.DispatchDate,
        Quantity = request.Quantity,
        BagType = request.BagType,
        PartyName = request.PartyName,
        MarketingNo = request.MarketingNo,
        ReplaceExisting = request.ReplaceExisting,
    };

    private static double RemainingQty(FibcAllotmentResult preview) =>
        RemainingQty(preview.Quantity, preview.ProposedSlots);

    private static double RemainingQty(double quantity, IReadOnlyList<FibcSlotGridItemDto> proposedSlots) =>
        Math.Round(quantity - proposedSlots.Sum(s => s.Allotted), 2);

    private double TargetDateShortfall(
        double quantity,
        string orderNo,
        string company,
        string erpFamily,
        DateTime targetDate,
        DateTime lookbackFrom,
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        Dictionary<string, double> virtualRemaining,
        IReadOnlyList<FibcLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        IReadOnlyList<string> activeShifts,
        double capacityPerShift,
        string? contextParty)
    {
        var proposed = AllotOnVirtualGrid(
            orderNo,
            company,
            erpFamily,
            quantity,
            targetDate,
            lookbackFrom,
            gridItems,
            CloneVirtualRemaining(virtualRemaining),
            eligibleLines,
            preferredLineNos,
            activeShifts,
            capacityPerShift,
            contextParty,
            targetDateOnly: true);
        return RemainingQty(quantity, proposed);
    }

    private static Dictionary<string, double> CloneVirtualRemaining(IReadOnlyDictionary<string, double> source) =>
        source.ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    private static double RemainingAfterDisplacements(
        double quantity,
        IReadOnlyList<FibcOrderShiftDisplacementDto> displacements,
        IReadOnlyList<FibcSlotGridItemDto> alreadyProposed)
    {
        var freed = displacements.Sum(d => d.Qty);
        var proposed = alreadyProposed.Sum(s => s.Allotted);
        return Math.Round(quantity - proposed, 2);
    }

    private List<FibcSlotGridItemDto> AllotOnVirtualGrid(
        string orderNo,
        string company,
        string erpFamily,
        double quantity,
        DateTime targetDate,
        DateTime lookbackFrom,
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        Dictionary<string, double> virtualRemaining,
        IReadOnlyList<FibcLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        IReadOnlyList<string> activeShifts,
        double capacityPerShift,
        string? contextParty,
        bool targetDateOnly = false)
    {
        var shiftPreference = _options.ShiftPreference;
        var proposed = new List<FibcSlotGridItemDto>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineRotation = 0;
        var remainingQty = quantity;
        var dayStop = targetDateOnly ? targetDate : lookbackFrom;

        for (var day = targetDate; day >= dayStop && remainingQty > SlotEpsilon; day = day.AddDays(-1))
        {
            var daySlots = gridItems
                .Where(s => s.PlanDate.Date == day.Date)
                .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
                .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
                .Where(s => GetVirtualRemaining(virtualRemaining, s.PlanDate, s.LineNo, s.Shift) > SlotEpsilon)
                .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
                .Where(s => !usedKeys.Contains(SlotKey(s.PlanDate, s.LineNo, s.Shift)))
                .OrderBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, ParseLineNo(s.LineNo), s.Shift, shiftPreference))
                .ThenBy(s => RotatedLineRank(preferredLineNos, ParseLineNo(s.LineNo), lineRotation))
                .ToList();

            foreach (var slot in daySlots)
            {
                if (remainingQty <= SlotEpsilon)
                    break;

                var effectiveRemaining = GetVirtualRemaining(virtualRemaining, slot.PlanDate, slot.LineNo, slot.Shift);
                var allotQty = Math.Round(Math.Min(remainingQty, Math.Min(slot.Capacity, effectiveRemaining)), 2);
                if (allotQty <= SlotEpsilon)
                    continue;

                var allocatedPercent = slot.Capacity > 0
                    ? Math.Round(allotQty / slot.Capacity * 100, 2)
                    : 100d;

                proposed.Add(new FibcSlotGridItemDto
                {
                    CompanyName = company,
                    BagType = slot.BagType,
                    BagTypeLabel = slot.BagTypeLabel,
                    PartyName = contextParty,
                    OrderNo = orderNo,
                    LineNo = slot.LineNo,
                    PlanDate = slot.PlanDate,
                    Allotted = allotQty,
                    Capacity = slot.Capacity,
                    Remaining = Math.Max(0, effectiveRemaining - allotQty),
                    AllocatedPercent = allocatedPercent,
                    Shift = slot.Shift,
                    Efficiency = slot.Efficiency,
                    UtilizationPercent = slot.Capacity > 0 ? Math.Round(allotQty / slot.Capacity * 100, 2) : 0,
                    OccupancyStatus = allotQty < slot.Capacity - SlotEpsilon ? "partial" : "full",
                });

                ApplyVirtualMove(virtualRemaining, slot.PlanDate, slot.LineNo, slot.Shift, allotQty, add: false);
                usedKeys.Add(SlotKey(slot.PlanDate, slot.LineNo, slot.Shift));
                lineRotation++;
                remainingQty = Math.Round(remainingQty - allotQty, 2);
            }
        }

        return proposed;
    }

    private FibcSlotGridItemDto? FindForwardAlternativeSlot(
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        Dictionary<string, double> virtualRemaining,
        string erpFamily,
        IReadOnlyList<FibcLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        IReadOnlyList<string> activeShifts,
        string fromLineNo,
        string fromShift,
        DateTime fromDate,
        double qtyNeeded,
        DateTime targetDate)
    {
        var maxForward = _options.CriticalShiftMaxForwardDays > 0 ? _options.CriticalShiftMaxForwardDays : 60;
        var forwardTo = fromDate.AddDays(maxForward);

        var sameLineCandidates = gridItems
            .Where(s => s.PlanDate.Date > fromDate.Date && s.PlanDate.Date <= forwardTo.Date)
            .Where(s => s.LineNo.Equals(fromLineNo, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Shift.Equals(fromShift, StringComparison.OrdinalIgnoreCase))
            .Where(s => GetVirtualRemaining(virtualRemaining, s.PlanDate, s.LineNo, s.Shift) >= qtyNeeded - SlotEpsilon)
            .OrderBy(s => s.PlanDate)
            .ToList();

        if (sameLineCandidates.Count > 0)
            return sameLineCandidates[0];

        return gridItems
            .Where(s => s.PlanDate.Date > fromDate.Date && s.PlanDate.Date <= forwardTo.Date)
            .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
            .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
            .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
            .Where(s => GetVirtualRemaining(virtualRemaining, s.PlanDate, s.LineNo, s.Shift) >= qtyNeeded - SlotEpsilon)
            .OrderBy(s => s.PlanDate)
            .ThenBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, ParseLineNo(s.LineNo), s.Shift, _options.ShiftPreference))
            .ThenBy(s => Math.Abs(ParseLineNo(s.LineNo) - ParseLineNo(fromLineNo)))
            .FirstOrDefault();
    }

    private static void ApplyVirtualMove(
        Dictionary<string, double> virtualRemaining,
        DateTime planDate,
        string lineNo,
        string shift,
        double qty,
        bool add)
    {
        var key = SlotKey(planDate, lineNo, shift);
        if (!virtualRemaining.ContainsKey(key))
            virtualRemaining[key] = 0;
        virtualRemaining[key] = add
            ? virtualRemaining[key] + qty
            : Math.Max(0, virtualRemaining[key] - qty);
    }

    private static double GetVirtualRemaining(
        IReadOnlyDictionary<string, double> virtualRemaining,
        DateTime planDate,
        string lineNo,
        string shift) =>
        virtualRemaining.GetValueOrDefault(SlotKey(planDate, lineNo, shift));

    private static FibcCriticalShiftResult Fail(string orderNo, string message) => new()
    {
        Success = false,
        Message = message,
        OrderNo = orderNo,
    };

    private static string? FirstNonEmpty(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();
        return null;
    }

    private static bool IsEligibleLine(string lineNoText, IReadOnlyList<FibcLineConfigDto> eligibleLines)
    {
        var lineNo = ParseLineNo(lineNoText);
        return eligibleLines.Any(l => l.LineNo == lineNo);
    }

    private static int ParseLineNo(string lineNoText)
    {
        var digits = new string(lineNoText.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : -1;
    }

    private static string SlotKey(DateTime planDate, string lineNo, string shift) =>
        $"{planDate:yyyy-MM-dd}|{lineNo}|{shift}";

    private static string ReservationKey(DateTime planDate, string lineNo, string shift) =>
        SlotKey(planDate, lineNo, shift);

    private static Dictionary<string, double> BuildHeldQtyMap(IReadOnlyList<FibcHoldReservationDto> reservations)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in reservations)
        {
            var key = ReservationKey(r.PlanDate, r.LineNo, r.Shift);
            map[key] = map.GetValueOrDefault(key) + r.Qty;
        }
        return map;
    }

    private static int RotatedLineRank(IReadOnlyList<int> preferredLines, int lineNo, int rotation)
    {
        var index = preferredLines.Count == 0 ? -1 : preferredLines.ToList().IndexOf(lineNo);
        if (index < 0)
            return 100;
        return (index - (rotation % preferredLines.Count) + preferredLines.Count) % preferredLines.Count;
    }
}
