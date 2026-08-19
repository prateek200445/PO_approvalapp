namespace POApprovalAPI.Services;

/// <summary>Preloads default Sales Dashboard ERP aggregates and refreshes them on a timer.</summary>
public sealed class SalesDashboardCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalesDashboardCacheWarmupService> _logger;

    public SalesDashboardCacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SalesDashboardCacheWarmupService> logger)
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
                var sales = scope.ServiceProvider.GetRequiredService<SalesDashboardService>();
                await sales.WarmDefaultCachesAsync(stoppingToken);
                _logger.LogInformation("Sales dashboard caches warmed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sales dashboard cache warmup failed (will load on first request).");
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
