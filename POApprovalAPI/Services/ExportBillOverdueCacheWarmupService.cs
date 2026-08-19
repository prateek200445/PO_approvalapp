namespace POApprovalAPI.Services;

/// <summary>
/// Keeps Export Bill Overdue All-Companies data hot so the first UI open is a cache hit.
/// </summary>
public sealed class ExportBillOverdueCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(10);
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ExportBillOverdueService>();
                await service.WarmDefaultCachesAsync(stoppingToken);
                _logger.LogInformation("Export bill overdue caches warmed.");
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
