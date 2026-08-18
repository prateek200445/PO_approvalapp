using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningEmailNotifier
{
    private readonly EmailService _emailService;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningEmailNotifier(EmailService emailService, IOptions<FibcPlanningOptions> options)
    {
        _emailService = emailService;
        _options = options.Value;
    }

    public Task NotifyHoldCreatedAsync(FibcQuotationHoldDto hold, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsEnabled())
            return Task.CompletedTask;

        var subject = $"FIBC quotation hold created — {hold.ReferenceCode} (Order {hold.OrderNo})";
        var body =
            $"A quotation hold has been created in the FIBC Line Planning portal.\n\n" +
            BuildHoldSummary(hold) +
            "\nReserved slots:\n" +
            BuildSlotLines(hold.Slots) +
            "\nPlease confirm or cancel before the hold expires.\n\n" +
            "— FIBC Line Planning (automated)";

        return SendAsync(subject, body);
    }

    public Task NotifyHoldConfirmedAsync(FibcQuotationHoldDto hold, int rowsInserted, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsEnabled())
            return Task.CompletedTask;

        var subject = $"FIBC quotation confirmed — {hold.ReferenceCode} saved to ERP (Order {hold.OrderNo})";
        var body =
            $"Quotation hold {hold.ReferenceCode} was confirmed and saved to prod_fibcallocationMaster.\n\n" +
            BuildHoldSummary(hold) +
            $"\nRows inserted: {rowsInserted}\n\n" +
            "Saved slots:\n" +
            BuildSlotLines(hold.Slots) +
            "\n— FIBC Line Planning (automated)";

        return SendAsync(subject, body);
    }

    public Task NotifyHoldCancelledAsync(FibcQuotationHoldDto hold, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsEnabled())
            return Task.CompletedTask;

        var subject = $"FIBC quotation hold cancelled — {hold.ReferenceCode} (Order {hold.OrderNo})";
        var body =
            $"Quotation hold {hold.ReferenceCode} was cancelled. Reserved capacity has been released.\n\n" +
            BuildHoldSummary(hold) +
            "\n— FIBC Line Planning (automated)";

        return SendAsync(subject, body);
    }

    public Task NotifyHoldExpiringSoonAsync(FibcQuotationHoldDto hold, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsEnabled())
            return Task.CompletedTask;

        var subject = $"FIBC quotation hold expiring soon — {hold.ReferenceCode} (Order {hold.OrderNo})";
        var body =
            $"Quotation hold {hold.ReferenceCode} will expire on {hold.ExpiresAt:yyyy-MM-dd HH:mm}.\n\n" +
            BuildHoldSummary(hold) +
            "\nReserved slots:\n" +
            BuildSlotLines(hold.Slots) +
            "\nPlease confirm to save the plan to ERP or cancel to release capacity.\n\n" +
            "— FIBC Line Planning (automated)";

        return SendAsync(subject, body);
    }

    public Task NotifyCriticalShiftConfirmedAsync(
        FibcCriticalShiftConfirmResult result,
        FibcCriticalShiftRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsCriticalShiftEnabled())
            return Task.CompletedTask;

        var subject = $"FIBC critical shift saved — Order {result.OrderNo} ({result.OrdersShifted} order(s) moved)";
        var dispatch = result.DispatchDate?.ToString("yyyy-MM-dd") ?? "—";
        var target = result.TargetCompletionDate?.ToString("yyyy-MM-dd") ?? "—";
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "—" : request.Reason.Trim();

        var body =
            $"A critical order shift was confirmed and saved to prod_fibcallocationMaster.\n\n" +
            $"Critical order:  {result.OrderNo}\n" +
            $"Customer:        {request.PartyName ?? "—"}\n" +
            $"Bag type:        {result.BagTypeLabel}\n" +
            $"Quantity:        {result.Quantity:N0} pcs\n" +
            $"Dispatch:        {dispatch}\n" +
            $"Target complete: {target}\n" +
            $"Pin to target:   {(result.PinToTargetDate ? "Yes" : "No")}\n" +
            $"Reason:          {reason}\n" +
            $"Company:         {request.CompanyName ?? _options.DefaultCompanyName}\n\n" +
            $"Orders shifted:  {result.OrdersShifted}\n" +
            $"Rows inserted:   {result.RowsInserted}\n" +
            $"Rows deleted:    {result.RowsDeleted}\n\n" +
            "Displaced orders:\n" +
            BuildDisplacementLines(result.Displacements) +
            "\nCritical order slots:\n" +
            BuildCriticalSlotLines(result.ProposedSlots) +
            "\n— FIBC Line Planning (automated)";

        return SendCriticalShiftAsync(subject, body);
    }

    private bool IsEnabled() =>
        _options.QuotationHoldEmailEnabled && _options.QuotationHoldNotifyTo.Length > 0;

    private bool IsCriticalShiftEnabled()
    {
        if (!_options.CriticalShiftEmailEnabled)
            return false;

        var to = ResolveCriticalShiftRecipients();
        return !string.IsNullOrWhiteSpace(to);
    }

    private string ResolveCriticalShiftRecipients()
    {
        var primary = _options.CriticalShiftNotifyTo.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        if (primary.Length > 0)
            return string.Join(";", primary);

        return string.Join(";", _options.QuotationHoldNotifyTo.Where(e => !string.IsNullOrWhiteSpace(e)));
    }

    private Task SendCriticalShiftAsync(string subject, string body)
    {
        var to = ResolveCriticalShiftRecipients();
        if (string.IsNullOrWhiteSpace(to))
            return Task.CompletedTask;

        var cc = string.IsNullOrWhiteSpace(_options.CriticalShiftNotifyCc)
            ? _options.QuotationHoldNotifyCc
            : _options.CriticalShiftNotifyCc;

        return _emailService.SendMail(to, subject, body, cc: cc);
    }

    private Task SendAsync(string subject, string body)
    {
        var to = string.Join(";", _options.QuotationHoldNotifyTo.Where(e => !string.IsNullOrWhiteSpace(e)));
        if (string.IsNullOrWhiteSpace(to))
            return Task.CompletedTask;

        return _emailService.SendMail(to, subject, body, cc: _options.QuotationHoldNotifyCc);
    }

    private static string BuildHoldSummary(FibcQuotationHoldDto hold)
    {
        var dispatch = hold.DispatchDate?.ToString("yyyy-MM-dd") ?? "—";
        return
            $"Reference:   {hold.ReferenceCode}\n" +
            $"Order:       {hold.OrderNo}\n" +
            $"Customer:    {hold.PartyName ?? "—"}\n" +
            $"Marketing:   {hold.MarketingNo ?? "—"}\n" +
            $"Bag type:    {hold.BagTypeLabel}\n" +
            $"Quantity:    {hold.Quantity:N0} pcs\n" +
            $"Dispatch:    {dispatch}\n" +
            $"Company:     {hold.CompanyName}\n" +
            $"Expires:     {hold.ExpiresAt:yyyy-MM-dd HH:mm}\n" +
            (string.IsNullOrWhiteSpace(hold.Notes) ? "" : $"Notes:       {hold.Notes}\n");
    }

    private static string BuildSlotLines(IReadOnlyList<FibcQuotationHoldSlotDto> slots)
    {
        if (slots.Count == 0)
            return "  (none)\n";

        return string.Join("\n", slots.Select(s =>
            $"  • {s.PlanDate:yyyy-MM-dd}  Line {s.LineNo}  Shift {s.Shift}  {s.Qty:N0} pcs"));
    }

    private static string BuildDisplacementLines(IReadOnlyList<FibcOrderShiftDisplacementDto> displacements)
    {
        if (displacements.Count == 0)
            return "  (none)\n";

        return string.Join("\n", displacements.Select(d =>
            $"  • {d.OrderNo} ({d.PartyName ?? "—"}): {d.FromPlanDate:yyyy-MM-dd} L{d.FromLineNo} {d.FromShift} → {d.ToPlanDate:yyyy-MM-dd} L{d.ToLineNo} {d.ToShift}  {d.Qty:N0} pcs"));
    }

    private static string BuildCriticalSlotLines(IReadOnlyList<FibcSlotGridItemDto> slots)
    {
        if (slots.Count == 0)
            return "  (none)\n";

        return string.Join("\n", slots.Select(s =>
            $"  • {s.PlanDate:yyyy-MM-dd}  Line {s.LineNo}  Shift {s.Shift}  {s.Allotted:N0} pcs"));
    }
}
