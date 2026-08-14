namespace POApprovalAPI.Services;

/// <summary>Preloads BOM party/user caches on API startup so first dropdown open is fast.</summary>
public sealed class BomCacheWarmupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BomCacheWarmupService> _logger;

    public BomCacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<BomCacheWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bom = scope.ServiceProvider.GetRequiredService<BomService>();
                await bom.WarmCachesAsync(cancellationToken);
                _logger.LogInformation("BOM lookup caches warmed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BOM cache warmup failed (will load on first request).");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
