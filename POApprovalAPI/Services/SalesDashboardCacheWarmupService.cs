namespace POApprovalAPI.Services;

/// <summary>Preloads Sales companies, then one overview, serialized with overdue warmup.</summary>
public sealed class SalesDashboardCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromHours(3);
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
                var sales = scope.ServiceProvider.GetRequiredService<SalesDashboardService>();
                await ErpCacheWarmupGate.RunAsync(
                    () => sales.WarmDefaultCachesAsync(stoppingToken),
                    stoppingToken);
                _logger.LogInformation("Sales dashboard caches warmed.");
            }
            catch (OperationCanceledException)
            {
                break;
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
