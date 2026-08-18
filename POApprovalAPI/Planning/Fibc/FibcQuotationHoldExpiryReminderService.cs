using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

/// <summary>Sends expiry reminder emails for active quotation holds (runs periodically).</summary>
public sealed class FibcQuotationHoldExpiryReminderService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FibcQuotationHoldExpiryReminderService> _logger;

    public FibcQuotationHoldExpiryReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<FibcQuotationHoldExpiryReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "FIBC quotation hold expiry reminder run failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<FibcPlanningOptions>>().Value;

        if (!options.QuotationHoldEnabled || !options.QuotationHoldEmailEnabled)
            return;

        if (options.QuotationHoldNotifyTo.Length == 0)
            return;

        var holdRepository = scope.ServiceProvider.GetRequiredService<IFibcQuotationHoldRepository>();
        var emailNotifier = scope.ServiceProvider.GetRequiredService<FibcPlanningEmailNotifier>();

        await holdRepository.ExpireStaleHoldsAsync(ct);

        var withinDays = options.QuotationHoldExpiryReminderDays > 0
            ? options.QuotationHoldExpiryReminderDays
            : 1;

        var holds = await holdRepository.GetHoldsNeedingExpiryReminderAsync(withinDays, ct);
        if (holds.Count == 0)
            return;

        _logger.LogInformation("Sending {Count} FIBC quotation hold expiry reminder(s).", holds.Count);

        foreach (var hold in holds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await emailNotifier.NotifyHoldExpiringSoonAsync(hold, ct);
                await holdRepository.MarkExpiryReminderSentAsync(hold.HoldId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed expiry reminder for hold {ReferenceCode}.", hold.ReferenceCode);
            }
        }
    }
}
