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
            TransferMode = TransferMode.StreamedResponse,
            OpenTimeout = TimeSpan.FromSeconds(15),
            SendTimeout = TimeSpan.FromMinutes(2),
            ReceiveTimeout = TimeSpan.FromMinutes(2)
        };

        ChannelFactory<IDMSService>? factory = null;
        IDMSService? client = null;

        try
        {
            factory = new ChannelFactory<IDMSService>(binding, new EndpointAddress(serviceUrl));
            client = factory.CreateChannel();
            return client.GetFile(fileId);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DMS WCF GetFile failed at {ServiceUrl} for file {FileId}", serviceUrl, fileId);
            return null;
        }
        finally
        {
            CloseClient(client);
            CloseFactory(factory);
        }
    }

    private IEnumerable<string> GetServiceUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in new[]
                 {
                     Environment.GetEnvironmentVariable("DMS_SERVICE_URL"),
                     _configuration["Dms:ServiceUrl"]
                 }
                 .Concat(_configuration.GetSection("Dms:ServiceUrls").Get<string[]>() ?? Array.Empty<string>()))
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
