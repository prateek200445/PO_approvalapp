using System.Collections.Concurrent;
using System.ServiceModel;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

public sealed class DmsRemoteFileService
{
    private const string WorkingUrlCacheKey = "dms:working-service-url";
    private static readonly TimeSpan WorkingUrlTtl = TimeSpan.FromHours(24);
    private static readonly ConcurrentDictionary<string, ChannelFactory<IDMSService>> Factories = new(StringComparer.OrdinalIgnoreCase);

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DmsRemoteFileService> _logger;

    public DmsRemoteFileService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<DmsRemoteFileService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Stream?> TryGetFileStreamAsync(int fileId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(WorkingUrlCacheKey, out string? cachedUrl) && !string.IsNullOrWhiteSpace(cachedUrl))
        {
            var cachedHit = await FetchAsync(cachedUrl, fileId, quickProbe: false, ct);
            if (cachedHit is not null)
                return cachedHit;

            _cache.Remove(WorkingUrlCacheKey);
            _logger.LogWarning("Cached DMS URL {ServiceUrl} failed; rediscovering", cachedUrl);
        }

        var urls = GetServiceUrls().ToList();
        if (urls.Count == 0)
            return null;

        if (urls.Count == 1)
            return await TryAndRememberAsync(urls[0], fileId, quickProbe: false, ct);

        return await RaceUrlsAsync(urls, fileId, ct);
    }

    private async Task<Stream?> RaceUrlsAsync(IReadOnlyList<string> urls, int fileId, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var winner = new TaskCompletionSource<(Stream Stream, string Url)>(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = urls.Select(url => Task.Run(async () =>
        {
            try
            {
                var stream = await FetchAsync(url, fileId, quickProbe: true, linked.Token);
                if (stream is null)
                    return;

                if (winner.TrySetResult((stream, url)))
                    await linked.CancelAsync();
                else
                    await stream.DisposeAsync();
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                // Another endpoint won the race.
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "DMS remote GetFile failed at {ServiceUrl} for file {FileId}", url, fileId);
            }
        }, linked.Token)).ToArray();

        var completed = await Task.WhenAny(winner.Task, Task.WhenAll(tasks));
        if (completed == winner.Task && winner.Task.IsCompletedSuccessfully)
        {
            var (stream, url) = winner.Task.Result;
            RememberWorkingUrl(url);
            return stream;
        }

        return null;
    }

    private async Task<Stream?> TryAndRememberAsync(string url, int fileId, bool quickProbe, CancellationToken ct)
    {
        var stream = await FetchAsync(url, fileId, quickProbe, ct);
        if (stream is not null)
            RememberWorkingUrl(url);
        return stream;
    }

    private void RememberWorkingUrl(string url) =>
        _cache.Set(WorkingUrlCacheKey, url, WorkingUrlTtl);

    private Task<Stream?> FetchAsync(string serviceUrl, int fileId, bool quickProbe, CancellationToken ct) =>
        Task.Run(() => FetchFile(serviceUrl, fileId, quickProbe), ct);

    private Stream? FetchFile(string serviceUrl, int fileId, bool quickProbe)
    {
        IDMSService? client = null;
        Stream? remoteStream = null;

        try
        {
            var factory = GetOrCreateFactory(serviceUrl);
            client = factory.CreateChannel();
            remoteStream = client.GetFile(fileId);
            if (remoteStream is null)
                return null;

            var buffer = new MemoryStream();
            remoteStream.CopyTo(buffer);
            if (buffer.Length == 0)
            {
                buffer.Dispose();
                return null;
            }

            buffer.Position = 0;
            _logger.LogInformation(
                "DMS GetFile via {ServiceUrl} file {FileId} ({Bytes} bytes)",
                serviceUrl, fileId, buffer.Length);
            return buffer;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DMS WCF GetFile failed at {ServiceUrl} for file {FileId}", serviceUrl, fileId);
            InvalidateFactory(serviceUrl);
            return null;
        }
        finally
        {
            remoteStream?.Dispose();
            CloseClient(client);
        }
    }

    private static ChannelFactory<IDMSService> GetOrCreateFactory(string serviceUrl) =>
        Factories.GetOrAdd(serviceUrl, CreateFactory);

    private static ChannelFactory<IDMSService> CreateFactory(string serviceUrl)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferSize = int.MaxValue,
            TransferMode = TransferMode.Streamed,
            OpenTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(20),
            ReceiveTimeout = TimeSpan.FromSeconds(60)
        };

        return new ChannelFactory<IDMSService>(binding, new EndpointAddress(serviceUrl));
    }

    private static void InvalidateFactory(string serviceUrl)
    {
        if (Factories.TryRemove(serviceUrl, out var factory))
            CloseFactory(factory);
    }

    private IEnumerable<string> GetServiceUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        IEnumerable<string?> candidates =
        [
            Environment.GetEnvironmentVariable("DMS_SERVICE_URL"),
            _configuration["Dms:ServiceUrl"],
            ..(_configuration.GetSection("Dms:ServiceUrls").Get<string[]>() ?? Array.Empty<string>()),
            ..DmsDefaults.ServiceUrls,
        ];

        foreach (var url in candidates)
        {
            if (string.IsNullOrWhiteSpace(url))
                continue;

            if (!OperatingSystem.IsWindows() &&
                url.Contains("desktop-ijn98i2", StringComparison.OrdinalIgnoreCase))
                continue;

            var trimmed = url.Trim().TrimEnd('/');
            if (seen.Add(trimmed))
                yield return trimmed;
        }
    }

    private static void CloseClient(IDMSService? client)
    {
        if (client is not ICommunicationObject communicationObject)
            return;

        try
        {
            if (communicationObject.State == CommunicationState.Faulted)
                communicationObject.Abort();
            else
                communicationObject.Close();
        }
        catch
        {
            communicationObject.Abort();
        }
    }

    private static void CloseFactory(ChannelFactory<IDMSService>? factory)
    {
        if (factory is null)
            return;

        try
        {
            if (factory.State == CommunicationState.Faulted)
                factory.Abort();
            else
                factory.Close();
        }
        catch
        {
            factory.Abort();
        }
    }
}

[ServiceContract(Namespace = "http://tempuri.org/")]
internal interface IDMSService
{
    [OperationContract(Action = "http://tempuri.org/IDMSService/GetFile", ReplyAction = "http://tempuri.org/IDMSService/GetFileResponse")]
    Stream GetFile(int id);
}
