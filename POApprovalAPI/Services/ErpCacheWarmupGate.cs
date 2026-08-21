namespace POApprovalAPI.Services;

/// <summary>
/// One ERP-heavy warmup at a time so Render/SQL is not flooded at boot.
/// </summary>
internal static class ErpCacheWarmupGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task RunAsync(Func<Task> work, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await work();
        }
        finally
        {
            Gate.Release();
        }
    }
}
