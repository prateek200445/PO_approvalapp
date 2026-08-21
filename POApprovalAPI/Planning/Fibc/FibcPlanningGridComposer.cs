using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Builds a planning grid from ERP capacity, portal synthetic slots, and saved prod_fibcallocationMaster rows.
/// </summary>
internal static class FibcPlanningGridComposer
{
    private const double SlotEpsilon = 0.001;

    public sealed class BuildResult
    {
        public FibcSlotGridResult Grid { get; init; } = new();
        public IReadOnlyList<string> ActiveShifts { get; init; } = Array.Empty<string>();
        public bool UsedSyntheticGrid { get; init; }
        public int SavedAllocationsApplied { get; init; }
    }

    public static async Task<BuildResult> BuildAsync(
        IFibcPlanningRepository repository,
        PlanningRuntimeContext runtime,
        IReadOnlyList<FibcLineConfigDto> erpLines,
        IOptions<FibcPlanningOptions> options,
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        string erpFamily,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var opts = options.Value;
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        var grid = await repository.GetSlotGridAsync(from, to, company, ct);
        var shiftsFromDb = await repository.GetDistinctShiftsAsync(from, to, company, ct);
        var activeShifts = FilterShifts(shiftsFromDb, grid.Items, opts);
        if (activeShifts.Count == 0)
            activeShifts = OrderShifts(opts.ShiftPreference, opts.ActiveShifts.ToList());

        var usedSynthetic = false;
        if (!FibcSyntheticGridBuilder.HasSlotsInWindow(grid, from, to))
        {
            grid = FibcSyntheticGridBuilder.BuildForWindow(
                from, to, company, activeShifts, runtime, erpLines, erpFamily);
            usedSynthetic = grid.Items.Count > 0;
        }

        var saved = await repository.GetSavedAllocationsInWindowAsync(company, from, to, ct);
        if (saved.Count > 0)
            grid = ApplySavedAllocations(grid, saved);

        var occupied = grid.Items.Count(i => i.Allotted > SlotEpsilon || i.Remaining <= SlotEpsilon);
        grid = new FibcSlotGridResult
        {
            Items = grid.Items,
            DateFrom = from,
            DateTo = to,
            CompanyName = grid.CompanyName,
            TotalSlots = grid.Items.Count,
            OccupiedSlots = occupied,
        };

        return new BuildResult
        {
            Grid = grid,
            ActiveShifts = activeShifts,
            UsedSyntheticGrid = usedSynthetic,
            SavedAllocationsApplied = saved.Count,
        };
    }

    /// <summary>Slot grid for UI — merges saved prod_fibcallocationMaster rows when ERP view is empty or stale.</summary>
    public static async Task<FibcSlotGridResult> BuildDisplayGridAsync(
        IFibcPlanningRepository repository,
        string company,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        var grid = await repository.GetSlotGridAsync(from, to, company, ct);
        var saved = await repository.GetSavedAllocationsInWindowAsync(company, from, to, ct);
        if (saved.Count == 0)
            return grid;

        grid = ApplySavedAllocations(grid, saved);
        var occupied = grid.Items.Count(i => i.Allotted > SlotEpsilon);
        return new FibcSlotGridResult
        {
            Items = grid.Items,
            DateFrom = from,
            DateTo = to,
            CompanyName = grid.CompanyName,
            TotalSlots = grid.Items.Count,
            OccupiedSlots = occupied,
        };
    }

    private static FibcSlotGridResult ApplySavedAllocations(
        FibcSlotGridResult grid,
        IReadOnlyList<FibcSavedAllocationRowDto> saved)
    {
        var items = grid.Items.ToList();
        var byKey = items.ToDictionary(
            s => SlotKey(s.PlanDate, s.LineNo, s.Shift),
            s => s,
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in saved)
        {
            var lineNo = row.LineNo.Trim();
            var shift = row.Shift.Trim();
            if (string.IsNullOrEmpty(lineNo) || string.IsNullOrEmpty(shift))
                continue;

            var bagFamily = BagTypeMapper.NormalizeErpFamily(row.BagType);
            var capacity = row.Capacity > SlotEpsilon ? row.Capacity : row.Qty;
            if (capacity <= SlotEpsilon)
                continue;

            var key = SlotKey(row.PlanDate, lineNo, shift);
            if (byKey.TryGetValue(key, out var slot))
            {
                var slotCapacity = row.Capacity > SlotEpsilon
                    ? row.Capacity
                    : slot.Capacity > SlotEpsilon
                        ? slot.Capacity
                        : row.Qty;
                var combinedAllotted = CombineOccupancy(slot.OrderNo, slot.Allotted, row.OrderNo, row.Qty);
                var displayOrder = string.IsNullOrWhiteSpace(slot.OrderNo)
                    ? row.OrderNo
                    : slot.OrderNo!.Equals(row.OrderNo, StringComparison.OrdinalIgnoreCase)
                        ? row.OrderNo
                        : slot.OrderNo;

                slot.OrderNo = displayOrder;
                slot.PartyName = row.PartyName ?? slot.PartyName;
                slot.MarketingNo = row.MarketingNo ?? slot.MarketingNo;
                slot.BagType = string.IsNullOrWhiteSpace(bagFamily) ? slot.BagType : bagFamily;
                slot.BagTypeLabel = BagTypeMapper.ToDisplayLabel(slot.BagType);
                slot.Allotted = combinedAllotted;
                slot.Capacity = slotCapacity;
                slot.Remaining = Math.Max(0, slotCapacity - combinedAllotted);
                slot.AllocatedPercent = slotCapacity > 0
                    ? Math.Round(combinedAllotted / slotCapacity * 100, 2)
                    : row.AllocatedPercent;
                slot.Efficiency = row.Efficiency;
                slot.UtilizationPercent = slotCapacity > 0 ? Math.Round(combinedAllotted / slotCapacity * 100, 2) : 0;
                slot.OccupancyStatus = slot.Remaining <= SlotEpsilon ? "full" : "partial";
                continue;
            }

            var allotted = row.Qty;
            var remaining = Math.Max(0, capacity - allotted);
            var added = new FibcSlotGridItemDto
            {
                CompanyName = row.CompanyName,
                BagType = bagFamily,
                BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagFamily),
                PartyName = row.PartyName,
                OrderNo = row.OrderNo,
                LineNo = lineNo,
                PlanDate = row.PlanDate.Date,
                Allotted = allotted,
                Capacity = capacity,
                Remaining = remaining,
                AllocatedPercent = row.AllocatedPercent
                    ?? (capacity > 0 ? Math.Round(allotted / capacity * 100, 2) : 100),
                Shift = shift,
                MarketingNo = row.MarketingNo,
                Efficiency = row.Efficiency,
                UtilizationPercent = capacity > 0 ? Math.Round(allotted / capacity * 100, 2) : 0,
                OccupancyStatus = remaining <= SlotEpsilon ? "full" : "partial",
            };
            items.Add(added);
            byKey[key] = added;
        }

        return new FibcSlotGridResult
        {
            Items = items,
            DateFrom = grid.DateFrom,
            DateTo = grid.DateTo,
            CompanyName = grid.CompanyName,
            TotalSlots = items.Count,
            OccupiedSlots = items.Count(i => i.Allotted > SlotEpsilon),
        };
    }

    private static string SlotKey(DateTime planDate, string lineNo, string shift) =>
        $"{planDate:yyyy-MM-dd}|{lineNo.Trim()}|{shift.Trim()}";

    private static double CombineOccupancy(string? existingOrder, double existingQty, string newOrder, double newQty)
    {
        if (existingQty <= SlotEpsilon)
            return newQty;

        if (string.IsNullOrWhiteSpace(existingOrder))
            return newQty;

        if (existingOrder.Equals(newOrder, StringComparison.OrdinalIgnoreCase))
            return Math.Max(existingQty, newQty);

        return existingQty + newQty;
    }

    private static IReadOnlyList<string> FilterShifts(
        IReadOnlyList<string> fromCapacity,
        IReadOnlyList<FibcSlotGridItemDto> gridItems,
        FibcPlanningOptions options)
    {
        var shifts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shift in fromCapacity)
        {
            if (!string.IsNullOrWhiteSpace(shift))
                shifts.Add(shift.Trim());
        }

        foreach (var item in gridItems)
        {
            if (!string.IsNullOrWhiteSpace(item.Shift))
                shifts.Add(item.Shift.Trim());
        }

        if (!options.AllowShiftCWhenCapacityExists)
            shifts.RemoveWhere(s => s.Equals("C", StringComparison.OrdinalIgnoreCase));

        return OrderShifts(options.ShiftPreference, shifts.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList());
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
