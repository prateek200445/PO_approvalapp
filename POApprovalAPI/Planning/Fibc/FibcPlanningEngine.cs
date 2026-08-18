using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningEngine : IFibcPlanningEngine
{
    private const double SlotEpsilon = 0.001;
    private const int MaxLookbackDays = 120;
    private static readonly DateTime MinValidDispatchDate = new(2000, 1, 1);

    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcQuotationHoldRepository _holdRepository;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningEngine(
        IFibcPlanningRepository repository,
        IFibcQuotationHoldRepository holdRepository,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _holdRepository = holdRepository;
        _options = options.Value;
    }

    public async Task<FibcAllotmentResult> AllotOrderAsync(FibcAllotmentRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var warnings = new List<string>();
        var orderNo = request.OrderNo.Trim();
        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();

        var context = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
        var dispatchDate = ResolveDispatchDate(request.DispatchDate, context?.DispatchDate);
        var bagTypeRaw = !string.IsNullOrWhiteSpace(request.BagType) ? request.BagType : context?.BagType;
        var erpFamily = BagTypeMapper.NormalizeErpFamily(bagTypeRaw);
        var quantity = request.Quantity > 0 ? request.Quantity : context?.Quantity ?? 0;

        if (string.IsNullOrWhiteSpace(erpFamily))
            return Fail(orderNo, "Bag type is required. Provide it or ensure BOM / marketing data exists for this order.");

        if (quantity <= 0)
            return Fail(orderNo, "Quantity is required. Provide it or ensure BOM / marketing data exists for this order.");

        if (dispatchDate is null)
            return Fail(orderNo, "Dispatch date is required. Provide it or ensure a marketing invoice exists for this order.");

        var lines = await _repository.GetLineConfigAsync(company, ct);
        var eligibleLines = lines
            .Where(l => LinePreferenceHelper.LineSupportsBagFamily(l.BagType, erpFamily))
            .ToList();

        if (eligibleLines.Count == 0)
            return Fail(orderNo, $"No production lines configured for bag family '{BagTypeMapper.ToDisplayLabel(erpFamily)}'.");

        var preferredLineNos = LinePreferenceHelper.GetPreferredLines(erpFamily);

        var bufferDays = _options.DispatchBufferDays;
        var lineBuffer = eligibleLines.Where(l => l.BufferDaysCheck > 0).Select(l => l.BufferDaysCheck).DefaultIfEmpty(bufferDays).Max();
        bufferDays = Math.Max(bufferDays, lineBuffer);

        var targetDate = dispatchDate.Value.AddDays(-bufferDays);
        var lookbackFrom = targetDate.AddDays(-MaxLookbackDays);

        var gridTask = _repository.GetSlotGridAsync(lookbackFrom, targetDate, company, ct);
        var shiftsTask = _repository.GetDistinctShiftsAsync(lookbackFrom, targetDate, company, ct);
        await Task.WhenAll(gridTask, shiftsTask);

        var grid = await gridTask;
        var activeShifts = FilterShifts(await shiftsTask, grid.Items);
        if (activeShifts.Count == 0)
            return Fail(orderNo, "No shift capacity found in CapacityPlanning for the selected date range.");

        var holdReservations = _options.QuotationHoldEnabled
            ? await _holdRepository.GetActiveHoldReservationsAsync(company, lookbackFrom, targetDate, ct: ct)
            : Array.Empty<FibcHoldReservationDto>();
        var heldBySlot = BuildHeldQtyMap(holdReservations);

        var capacityPerShift = ResolveGridCapacityPerShift(grid.Items, erpFamily, eligibleLines, preferredLineNos, lines);
        if (capacityPerShift <= 0)
            return Fail(orderNo, "Could not determine shift capacity from the planning grid.");

        var shiftPreference = OrderShifts(_options.ShiftPreference, activeShifts);
        var slotsRequired = quantity / capacityPerShift;

        var proposed = new List<FibcSlotGridItemDto>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineRotation = 0;
        var remainingQty = quantity;

        for (var day = targetDate; day >= lookbackFrom && remainingQty > SlotEpsilon; day = day.AddDays(-1))
        {
            var daySlots = grid.Items
                .Where(s => s.PlanDate.Date == day.Date)
                .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
                .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
                .Where(s => GetEffectiveRemaining(s, heldBySlot) > SlotEpsilon)
                .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
                .Where(s => !usedKeys.Contains(SlotKey(s)))
                .OrderBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, ParseLineNo(s.LineNo), s.Shift, shiftPreference))
                .ThenBy(s => RotatedLineRank(preferredLineNos, ParseLineNo(s.LineNo), lineRotation))
                .ToList();

            foreach (var slot in daySlots)
            {
                if (remainingQty <= SlotEpsilon)
                    break;

                var effectiveRemaining = GetEffectiveRemaining(slot, heldBySlot);
                var allotQty = Math.Round(Math.Min(remainingQty, Math.Min(slot.Capacity, effectiveRemaining)), 2);
                if (allotQty <= SlotEpsilon)
                    continue;

                var isPartial = allotQty < slot.Capacity - SlotEpsilon;
                var allocatedPercent = slot.Capacity > 0
                    ? Math.Round(allotQty / slot.Capacity * 100, 2)
                    : 100d;

                proposed.Add(new FibcSlotGridItemDto
                {
                    CompanyName = slot.CompanyName,
                    BagType = slot.BagType,
                    BagTypeLabel = slot.BagTypeLabel,
                    PartyName = context?.PartyName,
                    OrderNo = orderNo,
                    LineNo = slot.LineNo,
                    PlanDate = slot.PlanDate,
                    Allotted = allotQty,
                    Capacity = slot.Capacity,
                    Remaining = Math.Max(0, effectiveRemaining - allotQty),
                    AllocatedPercent = allocatedPercent,
                    Shift = slot.Shift,
                    MarketingNo = context?.MarketingNo,
                    TransId = slot.TransId,
                    Efficiency = slot.Efficiency,
                    UtilizationPercent = slot.Capacity > 0 ? Math.Round(allotQty / slot.Capacity * 100, 2) : 0,
                    OccupancyStatus = isPartial ? "partial" : "full",
                });

                usedKeys.Add(SlotKey(slot));
                lineRotation++;
                remainingQty = Math.Round(remainingQty - allotQty, 2);
            }
        }

        if (remainingQty > SlotEpsilon && holdReservations.Count > 0)
        {
            warnings.Add("Some capacity may be reserved by active quotation holds.");
        }

        if (remainingQty > SlotEpsilon)
        {
            warnings.Add(
                $"Could not allot remaining {remainingQty:N0} pcs between {lookbackFrom:yyyy-MM-dd} and {targetDate:yyyy-MM-dd}. Free more capacity or extend the lookback window.");
        }

        var success = proposed.Count > 0;
        var fullyAllotted = remainingQty <= SlotEpsilon;
        var message = success
            ? fullyAllotted
                ? $"Preview: {proposed.Count} slot(s) proposed ({quantity:N0} pcs) ending by {targetDate:yyyy-MM-dd} ({bufferDays}-day buffer before dispatch)."
                : $"Partial preview: {proposed.Count} slot(s) proposed but {remainingQty:N0} pcs still unallocated."
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
            Warnings = warnings,
            ProposedSlots = proposed,
        };
    }

    public async Task<FibcAllotmentConfirmResult> ConfirmAllotOrderAsync(
        FibcAllotmentRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var orderNo = request.OrderNo.Trim();

        if (!_options.AllowConfirmSave)
        {
            return new FibcAllotmentConfirmResult
            {
                Success = false,
                Saved = false,
                Message = "Confirm save is disabled. Set FibcPlanning:AllowConfirmSave to true in appsettings to enable writes.",
                OrderNo = orderNo,
            };
        }

        var preview = await AllotOrderAsync(request, ct);
        var result = ToConfirmResult(preview);

        if (!preview.Success || preview.ProposedSlots.Count == 0)
            return result;

        var allottedTotal = preview.ProposedSlots.Sum(s => s.Allotted);
        if (Math.Round(preview.Quantity - allottedTotal, 2) > SlotEpsilon)
        {
            result.Success = false;
            result.Message =
                $"Cannot save: only {allottedTotal:N0} of {preview.Quantity:N0} pcs could be allotted. Free capacity or adjust inputs.";
            return result;
        }

        var existing = await _repository.GetExistingAllocationCountAsync(preview.OrderNo, ct);
        if (existing > 0)
        {
            if (!request.ReplaceExisting || !_options.AllowReplaceExistingPlan)
            {
                result.Success = false;
                result.Message =
                    $"Cannot save: order {preview.OrderNo} already has {existing} allocation row(s) in prod_fibcallocationMaster. Enable replace or clear the existing plan in ERP first.";
                return result;
            }
        }

        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();

        var minDate = preview.ProposedSlots.Min(s => s.PlanDate);
        var maxDate = preview.ProposedSlots.Max(s => s.PlanDate);
        var holdReservations = _options.QuotationHoldEnabled
            ? await _holdRepository.GetActiveHoldReservationsAsync(company, minDate, maxDate, ct: ct)
            : Array.Empty<FibcHoldReservationDto>();
        var heldBySlot = BuildHeldQtyMap(holdReservations);

        foreach (var slot in preview.ProposedSlots)
        {
            var remaining = await _repository.GetSlotRemainingAsync(
                company,
                slot.LineNo,
                slot.PlanDate,
                slot.Shift,
                ct);
            if (remaining is null)
            {
                result.Success = false;
                result.Message =
                    $"Cannot save: capacity slot on {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} no longer exists.";
                return result;
            }

            var key = ReservationKey(slot.PlanDate, slot.LineNo, slot.Shift);
            var otherHeld = heldBySlot.GetValueOrDefault(key);
            var available = remaining.Value - otherHeld;

            if (slot.Allotted > available + SlotEpsilon)
            {
                result.Success = false;
                result.Message =
                    $"Cannot save: slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} available ({otherHeld:N0} held by quotation holds). Refresh and retry.";
                return result;
            }
        }

        var context = await _repository.GetOrderAllotmentContextAsync(preview.OrderNo, ct);
        var partyName = FirstNonEmpty(request.PartyName, context?.PartyName);
        var marketingNo = FirstNonEmpty(request.MarketingNo, context?.MarketingNo);

        try
        {
            var rows = await _repository.InsertAllocationsAsync(
                company,
                preview.OrderNo,
                partyName,
                marketingNo,
                preview.ProposedSlots,
                request.ReplaceExisting && existing > 0,
                ct);

            result.Saved = true;
            result.RowsInserted = rows;
            result.Success = true;
            result.Message = $"Saved {rows} allocation row(s) for order {preview.OrderNo} ({preview.Quantity:N0} pcs).";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Saved = false;
            result.Message = $"Save failed: {ex.Message}";
            return result;
        }
    }

    private static FibcAllotmentConfirmResult ToConfirmResult(FibcAllotmentResult preview) => new()
    {
        Success = preview.Success,
        Message = preview.Message,
        OrderNo = preview.OrderNo,
        BagType = preview.BagType,
        BagTypeLabel = preview.BagTypeLabel,
        Quantity = preview.Quantity,
        CapacityPerShift = preview.CapacityPerShift,
        SlotsRequired = preview.SlotsRequired,
        BufferDays = preview.BufferDays,
        DispatchDate = preview.DispatchDate,
        TargetCompletionDate = preview.TargetCompletionDate,
        Warnings = preview.Warnings,
        ProposedSlots = preview.ProposedSlots,
        Saved = false,
        RowsInserted = 0,
    };

    private static DateTime? ResolveDispatchDate(DateTime? requestDate, DateTime? contextDate)
    {
        if (requestDate?.Date is { } requested && IsValidDispatchDate(requested))
            return requested;

        if (contextDate is { } fromContext && IsValidDispatchDate(fromContext))
            return fromContext.Date;

        return null;
    }

    internal static bool IsValidDispatchDate(DateTime date) => date.Date >= MinValidDispatchDate;

    private static double ResolveGridCapacityPerShift(
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        string erpFamily,
        IReadOnlyList<FibcLineConfigDto> eligibleLines,
        IReadOnlyList<int> preferredLineNos,
        IReadOnlyList<FibcLineConfigDto> allLines)
    {
        var capacities = gridItems
            .Where(s => BagTypeMapper.NormalizeErpFamily(s.BagType).Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
            .Where(s => IsEligibleLine(s.LineNo, eligibleLines))
            .Where(s => s.Capacity > 0)
            .Select(s => s.Capacity)
            .ToList();

        if (capacities.Count > 0)
            return capacities.GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;

        var lineMasterCapacity = allLines
            .Where(l => preferredLineNos.Contains(l.LineNo))
            .Select(l => (double)l.BagCapacity)
            .FirstOrDefault(c => c > 0);

        if (lineMasterCapacity > 0)
            return lineMasterCapacity;

        return eligibleLines.Max(l => (double)l.BagCapacity);
    }

    private static FibcAllotmentResult Fail(string orderNo, string message) => new()
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

    private static string SlotKey(FibcSlotGridItemDto slot) =>
        $"{slot.PlanDate:yyyy-MM-dd}|{slot.LineNo}|{slot.Shift}|{slot.BagType}";

    private static string ReservationKey(DateTime planDate, string lineNo, string shift) =>
        $"{planDate:yyyy-MM-dd}|{lineNo}|{shift}";

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

    private static double GetEffectiveRemaining(FibcSlotGridItemDto slot, IReadOnlyDictionary<string, double> heldBySlot)
    {
        var key = ReservationKey(slot.PlanDate, slot.LineNo, slot.Shift);
        var held = heldBySlot.GetValueOrDefault(key);
        return Math.Max(0, slot.Remaining - held);
    }

    private static int RotatedLineRank(IReadOnlyList<int> preferredLines, int lineNo, int rotation)
    {
        var index = preferredLines.Count == 0 ? -1 : preferredLines.ToList().IndexOf(lineNo);
        if (index < 0)
            return 100;

        return (index - (rotation % preferredLines.Count) + preferredLines.Count) % preferredLines.Count;
    }

    private IReadOnlyList<string> FilterShifts(IReadOnlyList<string> fromCapacity, IReadOnlyList<FibcSlotGridItemDto> gridItems)
    {
        var shifts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shift in fromCapacity)
            shifts.Add(shift);

        foreach (var item in gridItems)
        {
            if (!string.IsNullOrWhiteSpace(item.Shift))
                shifts.Add(item.Shift.Trim());
        }

        if (!_options.AllowShiftCWhenCapacityExists)
            shifts.RemoveWhere(s => s.Equals("C", StringComparison.OrdinalIgnoreCase));

        return OrderShifts(_options.ShiftPreference, shifts.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IReadOnlyList<string> OrderShifts(IReadOnlyList<string> preference, IReadOnlyList<string> active)
    {
        var ordered = new List<string>();
        foreach (var shift in preference)
        {
            if (active.Any(a => a.Equals(shift, StringComparison.OrdinalIgnoreCase))
                && !ordered.Any(o => o.Equals(shift, StringComparison.OrdinalIgnoreCase)))
            {
                ordered.Add(active.First(a => a.Equals(shift, StringComparison.OrdinalIgnoreCase)));
            }
        }

        foreach (var shift in active)
        {
            if (!ordered.Any(o => o.Equals(shift, StringComparison.OrdinalIgnoreCase)))
                ordered.Add(shift);
        }

        return ordered;
    }
}
