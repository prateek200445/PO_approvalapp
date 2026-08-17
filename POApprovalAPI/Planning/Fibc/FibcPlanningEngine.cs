using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningEngine : IFibcPlanningEngine
{
    private const double SlotEpsilon = 0.001;
    private const int MaxLookbackDays = 120;
    private static readonly DateTime MinValidDispatchDate = new(2000, 1, 1);

    private readonly IFibcPlanningRepository _repository;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningEngine(IFibcPlanningRepository repository, IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
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
                .Where(s => s.Remaining > SlotEpsilon)
                .Where(s => activeShifts.Contains(s.Shift, StringComparer.OrdinalIgnoreCase))
                .Where(s => !usedKeys.Contains(SlotKey(s)))
                .OrderBy(s => LinePreferenceHelper.GetPreferenceRank(erpFamily, ParseLineNo(s.LineNo), s.Shift, shiftPreference))
                .ThenBy(s => RotatedLineRank(preferredLineNos, ParseLineNo(s.LineNo), lineRotation))
                .ToList();

            foreach (var slot in daySlots)
            {
                if (remainingQty <= SlotEpsilon)
                    break;

                var allotQty = Math.Round(Math.Min(remainingQty, Math.Min(slot.Capacity, slot.Remaining)), 2);
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
                    Remaining = Math.Max(0, slot.Remaining - allotQty),
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
