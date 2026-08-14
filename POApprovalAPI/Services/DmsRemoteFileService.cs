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
        var urls = GetOrderedServiceUrls().ToList();
        if (urls.Count == 0)
            return null;

        foreach (var serviceUrl in urls)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var stream = await Task.Run(() => FetchFile(serviceUrl, fileId), ct);
                if (stream is null)
                    continue;

                RememberWorkingUrl(serviceUrl);
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "DMS remote GetFile failed at {ServiceUrl} for file {FileId}", serviceUrl, fileId);
            }
        }

        return null;
    }

    private void RememberWorkingUrl(string url) =>
        _cache.Set(WorkingUrlCacheKey, url, WorkingUrlTtl);

    private IEnumerable<string> GetOrderedServiceUrls()
    {
        var all = GetServiceUrls().ToList();
        if (_cache.TryGetValue(WorkingUrlCacheKey, out string? cachedUrl) &&
            !string.IsNullOrWhiteSpace(cachedUrl) &&
            all.Contains(cachedUrl, StringComparer.OrdinalIgnoreCase))
        {
            yield return cachedUrl;
            foreach (var url in all.Where(u => !string.Equals(u, cachedUrl, StringComparison.OrdinalIgnoreCase)))
                yield return url;
            yield break;
        }

        foreach (var url in all)
            yield return url;
    }

    private Stream? FetchFile(string serviceUrl, int fileId)
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
            OpenTimeout = TimeSpan.FromSeconds(4),
            SendTimeout = TimeSpan.FromSeconds(15),
            ReceiveTimeout = TimeSpan.FromSeconds(30)
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
