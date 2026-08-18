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

    private bool IsEnabled() =>
        _options.QuotationHoldEmailEnabled && _options.QuotationHoldNotifyTo.Length > 0;

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
}
