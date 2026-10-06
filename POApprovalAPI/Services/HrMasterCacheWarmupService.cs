namespace POApprovalAPI.Services;

/// <summary>Keeps the HR master employee snapshot loaded so the Employee Master page never waits on the payroll ERP.</summary>
public sealed class HrMasterCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HrMasterCacheWarmupService> _logger;

    public HrMasterCacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<HrMasterCacheWarmupService> logger)
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
                var hr = scope.ServiceProvider.GetRequiredService<HrMasterService>();
                await ErpCacheWarmupGate.RunAsync(() => hr.WarmAsync(), stoppingToken);
                _logger.LogInformation("HR master cache warmed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HR master cache warmup failed (will load on first request).");
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
