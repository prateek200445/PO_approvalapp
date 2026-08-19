using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcQuotationHoldService
{
    private const double SlotEpsilon = 0.001;

    private readonly IFibcPlanningRepository _repository;
    private readonly IFibcPlanningEngine _engine;
    private readonly IFibcQuotationHoldRepository _holdRepository;
    private readonly FibcPlanningEmailNotifier _emailNotifier;
    private readonly FibcPlanningOptions _options;

    public FibcQuotationHoldService(
        IFibcPlanningRepository repository,
        IFibcPlanningEngine engine,
        IFibcQuotationHoldRepository holdRepository,
        FibcPlanningEmailNotifier emailNotifier,
        IOptions<FibcPlanningOptions> options)
    {
        _repository = repository;
        _engine = engine;
        _holdRepository = holdRepository;
        _emailNotifier = emailNotifier;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<FibcQuotationHoldDto>> GetActiveHoldsAsync(
        string? companyName,
        CancellationToken ct = default)
    {
        if (!_options.QuotationHoldEnabled)
            return Array.Empty<FibcQuotationHoldDto>();

        return await _holdRepository.GetActiveHoldsAsync(companyName, ct);
    }

    public async Task<FibcQuotationHoldResult> CreateHoldAsync(
        FibcQuotationHoldRequest request,
        CancellationToken ct = default)
    {
        if (!_options.QuotationHoldEnabled)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = "Quotation holds are disabled (FibcPlanning:QuotationHoldEnabled).",
            };
        }

        if (string.IsNullOrWhiteSpace(request.OrderNo))
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = "Order number is required.",
            };
        }

        await _holdRepository.ExpireStaleHoldsAsync(ct);

        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();
        var orderNo = request.OrderNo.Trim();

        var activeHolds = await _holdRepository.GetActiveHoldsAsync(company, ct);
        if (activeHolds.Any(h => h.OrderNo.Equals(orderNo, StringComparison.OrdinalIgnoreCase)))
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = $"Order {orderNo} already has an active quotation hold. Confirm or cancel it first.",
            };
        }

        var preview = await _engine.AllotOrderAsync(new FibcAllotmentRequest
        {
            OrderNo = orderNo,
            CompanyName = company,
            DispatchDate = request.DispatchDate,
            Quantity = request.Quantity,
            BagType = request.BagType,
            PartyName = request.PartyName,
            MarketingNo = request.MarketingNo,
        }, ct);

        if (!preview.Success || preview.ProposedSlots.Count == 0)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = preview.Message,
            };
        }

        var allottedTotal = preview.ProposedSlots.Sum(s => s.Allotted);
        if (Math.Round(preview.Quantity - allottedTotal, 2) > SlotEpsilon)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message =
                    $"Cannot hold: only {allottedTotal:N0} of {preview.Quantity:N0} pcs could be reserved. Free capacity or adjust inputs.",
            };
        }

        var context = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
        var enrichedRequest = new FibcQuotationHoldRequest
        {
            OrderNo = orderNo,
            CompanyName = company,
            DispatchDate = preview.DispatchDate ?? request.DispatchDate,
            Quantity = preview.Quantity,
            BagType = preview.BagType,
            PartyName = FirstNonEmpty(request.PartyName, context?.PartyName),
            MarketingNo = FirstNonEmpty(request.MarketingNo, context?.MarketingNo),
            Notes = request.Notes,
        };

        var holdDays = _options.QuotationHoldDays > 0 ? _options.QuotationHoldDays : 7;
        var expiresAt = DateTime.Now.AddDays(holdDays);

        var hold = await _holdRepository.CreateHoldAsync(
            enrichedRequest,
            preview.ProposedSlots,
            expiresAt,
            ct);

        await _emailNotifier.NotifyHoldCreatedAsync(hold, ct);

        return new FibcQuotationHoldResult
        {
            Success = true,
            Message =
                $"Quotation hold {hold.ReferenceCode} created for {preview.Quantity:N0} pcs ({preview.ProposedSlots.Count} slot(s)). Expires {expiresAt:yyyy-MM-dd HH:mm}.",
            Hold = hold,
        };
    }

    public async Task<FibcQuotationConfirmResult> ConfirmHoldAsync(
        int holdId,
        bool replaceExisting,
        CancellationToken ct = default)
    {
        if (!_options.QuotationHoldEnabled)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = "Quotation holds are disabled.",
                HoldId = holdId,
            };
        }

        if (!_options.AllowConfirmSave)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = "Confirm save is disabled (FibcPlanning:AllowConfirmSave).",
                HoldId = holdId,
            };
        }

        await _holdRepository.ExpireStaleHoldsAsync(ct);

        var hold = await _holdRepository.GetHoldAsync(holdId, ct);
        if (hold is null)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = "Quotation hold not found.",
                HoldId = holdId,
            };
        }

        if (!hold.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = $"Hold {hold.ReferenceCode} is {hold.Status} and cannot be confirmed.",
                HoldId = holdId,
            };
        }

        if (hold.ExpiresAt < DateTime.Now)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = $"Hold {hold.ReferenceCode} has expired.",
                HoldId = holdId,
            };
        }

        if (hold.Slots.Count == 0)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Message = "Hold has no reserved slots.",
                HoldId = holdId,
            };
        }

        var existing = await _repository.GetExistingAllocationCountAsync(hold.OrderNo, ct);
        if (existing > 0)
        {
            if (!replaceExisting || !_options.AllowReplaceExistingPlan)
            {
                return new FibcQuotationConfirmResult
                {
                    Success = false,
                    Message =
                        $"Order {hold.OrderNo} already has {existing} allocation row(s). Enable replace or clear the existing plan first.",
                    HoldId = holdId,
                };
            }
        }

        var minDate = hold.Slots.Min(s => s.PlanDate);
        var maxDate = hold.Slots.Max(s => s.PlanDate);
        var otherHolds = await _holdRepository.GetActiveHoldReservationsAsync(
            hold.CompanyName,
            minDate,
            maxDate,
            excludeHoldId: holdId,
            ct: ct);
        var heldBySlot = BuildHeldQtyMap(otherHolds);

        var gridSlots = hold.Slots.Select(s => new FibcSlotGridItemDto
        {
            CompanyName = hold.CompanyName,
            BagType = hold.BagType ?? "",
            BagTypeLabel = hold.BagTypeLabel,
            PartyName = hold.PartyName,
            OrderNo = hold.OrderNo,
            LineNo = s.LineNo,
            PlanDate = s.PlanDate,
            Allotted = s.Qty,
            Capacity = s.Capacity,
            AllocatedPercent = s.AllocatedPercent,
            Shift = s.Shift,
            MarketingNo = hold.MarketingNo,
        }).ToList();

        foreach (var slot in gridSlots)
        {
            var remaining = await _repository.GetSlotRemainingAsync(
                hold.CompanyName,
                slot.LineNo,
                slot.PlanDate,
                slot.Shift,
                ct);
            if (remaining is null)
            {
                return new FibcQuotationConfirmResult
                {
                    Success = false,
                    Message =
                        $"Cannot confirm: capacity slot on {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} no longer exists.",
                    HoldId = holdId,
                };
            }

            var key = ReservationKey(slot.PlanDate, slot.LineNo, slot.Shift);
            var otherHeld = heldBySlot.GetValueOrDefault(key);
            var available = remaining.Value - otherHeld;
            if (slot.Allotted > available + SlotEpsilon)
            {
                return new FibcQuotationConfirmResult
                {
                    Success = false,
                    Message =
                        $"Cannot confirm: slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} available ({otherHeld:N0} held by other quotations).",
                    HoldId = holdId,
                };
            }
        }

        try
        {
            var rows = await _repository.InsertAllocationsAsync(
                hold.CompanyName,
                hold.OrderNo,
                hold.PartyName,
                hold.MarketingNo,
                gridSlots,
                replaceExisting && existing > 0,
                allowSyntheticSlots: false,
                ct);

            await _holdRepository.MarkHoldConfirmedAsync(holdId, ct);

            await _emailNotifier.NotifyHoldConfirmedAsync(hold, rows, ct);

            return new FibcQuotationConfirmResult
            {
                Success = true,
                Saved = true,
                Message = $"Confirmed hold {hold.ReferenceCode}: saved {rows} allocation row(s) for order {hold.OrderNo}.",
                HoldId = holdId,
                RowsInserted = rows,
            };
        }
        catch (Exception ex)
        {
            return new FibcQuotationConfirmResult
            {
                Success = false,
                Saved = false,
                Message = $"Confirm failed: {ex.Message}",
                HoldId = holdId,
            };
        }
    }

    public async Task<FibcQuotationHoldResult> CancelHoldAsync(int holdId, CancellationToken ct = default)
    {
        if (!_options.QuotationHoldEnabled)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = "Quotation holds are disabled.",
            };
        }

        await _holdRepository.ExpireStaleHoldsAsync(ct);
        var hold = await _holdRepository.GetHoldAsync(holdId, ct);
        if (hold is null)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = "Quotation hold not found.",
            };
        }

        if (!hold.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = $"Hold {hold.ReferenceCode} is already {hold.Status}.",
            };
        }

        var cancelled = await _holdRepository.MarkHoldCancelledAsync(holdId, ct);
        if (!cancelled)
        {
            return new FibcQuotationHoldResult
            {
                Success = false,
                Message = "Failed to cancel hold.",
            };
        }

        var updated = await _holdRepository.GetHoldAsync(holdId, ct);

        if (updated is not null)
            await _emailNotifier.NotifyHoldCancelledAsync(updated, ct);

        return new FibcQuotationHoldResult
        {
            Success = true,
            Message = $"Cancelled quotation hold {hold.ReferenceCode}.",
            Hold = updated,
        };
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
}
