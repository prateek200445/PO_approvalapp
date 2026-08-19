using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Builds planning slots from portal line setup when ERP vw_fibclineplanning_NEW
/// has no rows for the backward-scheduling window (e.g. dispatch dates beyond ERP data).
/// </summary>
internal static class FibcSyntheticGridBuilder
{
    public static bool HasSlotsInWindow(FibcSlotGridResult grid, DateTime from, DateTime to) =>
        grid.Items.Any(i => i.PlanDate.Date >= from.Date && i.PlanDate.Date <= to.Date);

    public static FibcSlotGridResult BuildForWindow(
        DateTime lookbackFrom,
        DateTime targetDate,
        string company,
        IReadOnlyList<string> activeShifts,
        PlanningRuntimeContext runtime,
        IReadOnlyList<FibcLineConfigDto> erpLines,
        string erpFamily)
    {
        var from = lookbackFrom.Date;
        var to = targetDate.Date;
        if (from > to)
            (from, to) = (to, from);

        var eligibleLines = ResolveEligibleLines(runtime, erpLines, erpFamily);
        var items = new List<FibcSlotGridItemDto>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            foreach (var line in eligibleLines)
            {
                foreach (var shift in activeShifts)
                {
                    var cap = runtime.GetLineCapacity(line, "Normal");
                    if (cap <= 0)
                        cap = line.ErpBagCapacity ?? line.CapacityNormal ?? 0;
                    if (cap <= 0)
                        continue;

                    items.Add(new FibcSlotGridItemDto
                    {
                        CompanyName = company,
                        BagType = erpFamily,
                        BagTypeLabel = BagTypeMapper.ToDisplayLabel(erpFamily),
                        LineNo = line.LineNo.ToString(),
                        PlanDate = day,
                        Allotted = 0,
                        Capacity = cap,
                        Remaining = cap,
                        AllocatedPercent = 0,
                        Shift = shift,
                        UtilizationPercent = 0,
                        OccupancyStatus = "free",
                    });
                }
            }
        }

        return new FibcSlotGridResult
        {
            Items = items,
            DateFrom = from,
            DateTo = to,
            CompanyName = company,
            TotalSlots = items.Count,
            OccupiedSlots = 0,
        };
    }

    private static IReadOnlyList<PlanningLineConfigDto> ResolveEligibleLines(
        PlanningRuntimeContext runtime,
        IReadOnlyList<FibcLineConfigDto> erpLines,
        string erpFamily)
    {
        var portal = runtime.Lines
            .Where(l => l.IsActive)
            .Where(l => runtime.LineSupportsBagFamily(l, l.ErpBagType, erpFamily))
            .ToList();

        if (portal.Count > 0)
            return portal;

        if (erpLines.Count == 0)
            return Array.Empty<PlanningLineConfigDto>();

        return erpLines
            .Where(l => LinePreferenceHelper.LineSupportsBagFamily(l.BagType, erpFamily))
            .Select(l => new PlanningLineConfigDto
            {
                LineNo = l.LineNo,
                ErpBagType = l.BagType,
                AllowedBagFamilies = PlanningSetupHelpers.InferBagFamilies(l.BagType),
                CapacityNormal = l.BagCapacity,
                ErpBagCapacity = l.BagCapacity,
                IsActive = true,
                PreferenceOrder = l.LineNo,
            })
            .ToList();
    }
}
