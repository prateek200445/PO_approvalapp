using System.ServiceModel;
using System.ServiceModel.Channels;

namespace POApprovalAPI.Services;

public sealed class DmsRemoteFileService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DmsRemoteFileService> _logger;

    public DmsRemoteFileService(IConfiguration configuration, ILogger<DmsRemoteFileService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Stream?> TryGetFileStreamAsync(int fileId, CancellationToken ct = default)
    {
        foreach (var serviceUrl in GetServiceUrls())
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var stream = await Task.Run(() => FetchFile(serviceUrl, fileId), ct);
                if (stream is not null)
                    return stream;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "DMS remote GetFile failed at {ServiceUrl} for file {FileId}", serviceUrl, fileId);
            }
        }

        return null;
    }

    private Stream? FetchFile(string serviceUrl, int fileId)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferSize = int.MaxValue,
            TransferMode = TransferMode.Streamed,
            OpenTimeout = TimeSpan.FromSeconds(8),
            SendTimeout = TimeSpan.FromSeconds(30),
            ReceiveTimeout = TimeSpan.FromSeconds(90)
        };

        ChannelFactory<IDMSService>? factory = null;
        IDMSService? client = null;
        Stream? remoteStream = null;

        try
        {
            factory = new ChannelFactory<IDMSService>(binding, new EndpointAddress(serviceUrl));
            client = factory.CreateChannel();
            remoteStream = client.GetFile(fileId);
            if (remoteStream is null)
                return null;

            var buffer = new MemoryStream();
            remoteStream.CopyTo(buffer);
            if (buffer.Length == 0)
                return null;

            buffer.Position = 0;
            _logger.LogInformation(
                "DMS remote GetFile succeeded at {ServiceUrl} for file {FileId} ({Bytes} bytes)",
                serviceUrl, fileId, buffer.Length);
            return buffer;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DMS WCF GetFile failed at {ServiceUrl} for file {FileId}", serviceUrl, fileId);
            return null;
        }
        finally
        {
            remoteStream?.Dispose();
            CloseClient(client);
            CloseFactory(factory);
        }
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
