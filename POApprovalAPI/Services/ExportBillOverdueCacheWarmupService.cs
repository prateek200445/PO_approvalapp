namespace POApprovalAPI.Services;

/// <summary>
/// Warms lightweight dropdowns first, then one overdue universe, without fighting Sales warmup.
/// </summary>
public sealed class ExportBillOverdueCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromHours(3);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportBillOverdueCacheWarmupService> _logger;

    public ExportBillOverdueCacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExportBillOverdueCacheWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ExportBillOverdueService>();
                await ErpCacheWarmupGate.RunAsync(
                    () => service.WarmDefaultCachesAsync(stoppingToken),
                    stoppingToken);
                _logger.LogInformation("Export bill overdue caches warmed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Export bill overdue cache warmup failed (will load on first request).");
            }

            try
            {
                await Task.Delay(RefreshEvery, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
