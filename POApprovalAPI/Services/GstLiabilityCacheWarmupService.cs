namespace POApprovalAPI.Services;

/// <summary>Keeps the GST liability ledger data for the previous and current month in cache.</summary>
public sealed class GstLiabilityCacheWarmupService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(60);
    // Shorter than the 2-hour cache lifetime so the cached months never expire between runs.
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(90);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GstLiabilityCacheWarmupService> _logger;

    public GstLiabilityCacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<GstLiabilityCacheWarmupService> logger)
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
                var gst = scope.ServiceProvider.GetRequiredService<GstLiabilityService>();
                await ErpCacheWarmupGate.RunAsync(() => gst.WarmAsync(), stoppingToken);
                _logger.LogInformation("GST liability cache warmed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GST liability cache warmup failed (will load on first request).");
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
