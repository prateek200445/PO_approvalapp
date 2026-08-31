using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Planning.Setup;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningEngine : IFibcPlanningEngine
{
    private const double SlotEpsilon = 0.001;
    private const int MaxLookbackDays = 120;
    private static readonly DateTime MinValidDispatchDate = new(2000, 1, 1);

    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcQuotationHoldRepository _holdRepository;
    private readonly PlanningRuntimeContextLoader _runtimeLoader;
    private readonly FibcPlanningEmailNotifier _emailNotifier;
    private readonly OrderPlanningRouteService _routeService;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningEngine(
        IFibcPlanningRepository repository,
        IFibcQuotationHoldRepository holdRepository,
        PlanningRuntimeContextLoader runtimeLoader,
        FibcPlanningEmailNotifier emailNotifier,
        OrderPlanningRouteService routeService,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _holdRepository = holdRepository;
        _runtimeLoader = runtimeLoader;
        _emailNotifier = emailNotifier;
        _routeService = routeService;
        _options = options.Value;
    }

    public async Task<FibcAllotmentResult> AllotOrderAsync(FibcAllotmentRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var orderNo = request.OrderNo.Trim();
        var company = await ResolveFibcCompanyAsync(request, ct);

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

        var runtime = await _runtimeLoader.LoadAsync(company, ct);
        var erpLines = await _repository.GetLineConfigAsync(company, ct);

        var bufferDays = runtime.Factory.DefaultDispatchBufferDays > 0
            ? runtime.Factory.DefaultDispatchBufferDays
            : _options.DispatchBufferDays;

        var lineBuffer = runtime.Lines.Where(l => l.BufferDaysOverride > 0).Select(l => l.BufferDaysOverride!.Value)
            .Concat(erpLines.Where(l => l.BufferDaysCheck > 0).Select(l => l.BufferDaysCheck))
            .DefaultIfEmpty(bufferDays)
            .Max();
        bufferDays = Math.Max(bufferDays, lineBuffer);

        var targetDate = dispatchDate.Value.AddDays(-bufferDays);
        var lookbackFrom = targetDate.AddDays(-MaxLookbackDays);

        var gridBuild = await FibcPlanningGridComposer.BuildAsync(
            _repository,
            runtime,
            erpLines,
            Microsoft.Extensions.Options.Options.Create(_options),
            company,
            lookbackFrom,
            targetDate,
            erpFamily,
            ct);

        if (gridBuild.ActiveShifts.Count == 0)
            return Fail(orderNo, "No shift capacity found in CapacityPlanning for the selected date range.");

        if (gridBuild.Grid.Items.Count == 0)
            return Fail(orderNo, "No production lines configured for this bag type in portal setup.");

        var grid = gridBuild.Grid;
        var activeShifts = gridBuild.ActiveShifts;
        var usedSyntheticGrid = gridBuild.UsedSyntheticGrid;
        var savedAllocationsApplied = gridBuild.SavedAllocationsApplied;

        var holdReservations = _options.QuotationHoldEnabled
            ? await _holdRepository.GetActiveHoldReservationsAsync(company, lookbackFrom, targetDate, ct: ct)
            : Array.Empty<FibcHoldReservationDto>();
        var heldBySlot = BuildHeldQtyMap(holdReservations);
        var shiftPreference = OrderShifts(_options.ShiftPreference, activeShifts);

        var result = FibcAllotmentPlanner.Plan(
            request,
            runtime,
            context,
            erpFamily,
            quantity,
            dispatchDate.Value,
            bufferDays,
            targetDate,
            lookbackFrom,
            grid,
            activeShifts,
            shiftPreference,
            heldBySlot,
            erpLines);

        var warnings = result.Warnings.ToList();
        if (usedSyntheticGrid)
        {
            warnings.Insert(0,
                "ERP capacity grid has no rows for this dispatch window; preview uses portal line capacities (A/B shifts).");
        }

        if (savedAllocationsApplied > 0)
        {
            warnings.Add(
                $"Loaded {savedAllocationsApplied} saved allocation row(s) from prod_fibcallocationMaster onto the planning grid.");
        }

        result.Warnings = warnings;
        result.UsedSyntheticGrid = usedSyntheticGrid;
        result.SavedAllocationsApplied = savedAllocationsApplied;
        return result;
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

        var company = await ResolveFibcCompanyAsync(request, ct);

        var minDate = preview.ProposedSlots.Min(s => s.PlanDate);
        var maxDate = preview.ProposedSlots.Max(s => s.PlanDate);
        var holdReservations = _options.QuotationHoldEnabled
            ? await _holdRepository.GetActiveHoldReservationsAsync(company, minDate, maxDate, ct: ct)
            : Array.Empty<FibcHoldReservationDto>();
        var heldBySlot = BuildHeldQtyMap(holdReservations);

        foreach (var slot in preview.ProposedSlots)
        {
            var key = ReservationKey(slot.PlanDate, slot.LineNo, slot.Shift);
            var otherHeld = heldBySlot.GetValueOrDefault(key);
            var occupiedByOthers = await _repository.GetSavedQtyOnSlotExcludingOrderAsync(
                company,
                slot.LineNo,
                slot.PlanDate,
                slot.Shift,
                preview.OrderNo,
                ct);

            var capacity = slot.Capacity > SlotEpsilon ? slot.Capacity : PreviewSlotAvailable(slot);
            double available;

            if (preview.UsedSyntheticGrid || preview.SavedAllocationsApplied > 0 || occupiedByOthers > SlotEpsilon)
            {
                available = Math.Max(0, capacity - occupiedByOthers - otherHeld);
            }
            else
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

                available = Math.Max(0, Math.Min(remaining.Value, capacity - occupiedByOthers) - otherHeld);
            }

            if (slot.Allotted > available + SlotEpsilon)
            {
                result.Success = false;
                result.Message = occupiedByOthers > SlotEpsilon
                    ? $"Cannot save: slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} available ({occupiedByOthers:N0} pcs allocated to other orders)."
                    : $"Cannot save: slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} available ({otherHeld:N0} held by quotation holds). Refresh and retry.";
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
                allowSyntheticSlots: preview.UsedSyntheticGrid,
                ct);

            result.Saved = true;
            result.RowsInserted = rows;
            result.Success = true;
            result.Message = $"Saved {rows} allocation row(s) for order {preview.OrderNo} ({preview.Quantity:N0} pcs, {preview.AllotmentMode}).";

            try
            {
                await _emailNotifier.NotifyAllotmentConfirmedAsync(result, request, ct);
            }
            catch
            {
                // Email failure must not roll back a successful ERP save.
            }

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

    private static double PreviewSlotAvailable(FibcSlotGridItemDto slot)
    {
        var fromPreview = slot.Remaining + slot.Allotted;
        if (fromPreview > SlotEpsilon)
            return fromPreview;

        return slot.Capacity > SlotEpsilon ? slot.Capacity : 0;
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
        AllotmentMode = preview.AllotmentMode,
        DustLevel = preview.DustLevel,
        RejectionPercentApplied = preview.RejectionPercentApplied,
        UsedSyntheticGrid = preview.UsedSyntheticGrid,
        SavedAllocationsApplied = preview.SavedAllocationsApplied,
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

    private static FibcAllotmentResult Fail(string orderNo, string message) => new()
    {
        Success = false,
        Message = message,
        OrderNo = orderNo,
    };

    private async Task<string> ResolveFibcCompanyAsync(FibcAllotmentRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.CompanyName))
            return request.CompanyName.Trim();

        var orderNo = request.OrderNo?.Trim() ?? "";
        if (string.IsNullOrEmpty(orderNo))
            return _options.DefaultCompanyName;

        var route = await _routeService.ResolveAsync(orderNo, ct);
        return string.IsNullOrWhiteSpace(route.FibcCompanyName)
            ? _options.DefaultCompanyName
            : route.FibcCompanyName;
    }

    private static string? FirstNonEmpty(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();
        return null;
    }

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
