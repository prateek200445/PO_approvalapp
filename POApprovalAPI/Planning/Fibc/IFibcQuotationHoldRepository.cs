using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

public interface IFibcQuotationHoldRepository
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task ExpireStaleHoldsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FibcHoldReservationDto>> GetActiveHoldReservationsAsync(
        string companyName,
        DateTime dateFrom,
        DateTime dateTo,
        int? excludeHoldId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<FibcQuotationHoldDto>> GetActiveHoldsAsync(
        string? companyName,
        CancellationToken ct = default);

    Task<FibcQuotationHoldDto?> GetHoldAsync(int holdId, CancellationToken ct = default);

    Task<FibcQuotationHoldDto> CreateHoldAsync(
        FibcQuotationHoldRequest request,
        IReadOnlyList<FibcSlotGridItemDto> proposedSlots,
        DateTime expiresAt,
        CancellationToken ct = default);

    Task<bool> MarkHoldConfirmedAsync(int holdId, CancellationToken ct = default);

    Task<bool> MarkHoldCancelledAsync(int holdId, CancellationToken ct = default);

    Task<IReadOnlyList<FibcQuotationHoldDto>> GetHoldsNeedingExpiryReminderAsync(
        int withinDays,
        CancellationToken ct = default);

    Task MarkExpiryReminderSentAsync(int holdId, CancellationToken ct = default);
}
