using System.Threading.Channels;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

/// <summary>Queues BOM PDF generation + SMTP off the HTTP request thread.</summary>
public sealed class BomEmailBackgroundService : BackgroundService
{
    private readonly Channel<BomSendEmailRequest> _queue =
        Channel.CreateUnbounded<BomSendEmailRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BomEmailBackgroundService> _logger;

    public BomEmailBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BomEmailBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool TryQueueSend(BomSendEmailRequest request) =>
        _queue.Writer.TryWrite(request);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSendAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOM background email failed for {FilePoNo}", request.FilePoNo);
            }
        }
    }

    private async Task ProcessSendAsync(BomSendEmailRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var bomService = scope.ServiceProvider.GetRequiredService<BomService>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var model = await bomService.BuildPdfModelAsync(request.FilePoNo);
        if (model is null)
        {
            _logger.LogWarning("BOM email skipped — not found: {FilePoNo}", request.FilePoNo);
            return;
        }

        ct.ThrowIfCancellationRequested();

        var pdfBytes = await Task.Run(() => new BillOfMaterialDocument(model).GeneratePdf(), ct);
        var fileName = $"{SanitizeFileName(model.QtnNo)}.pdf";
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"BOM - {model.QtnNo} - {model.PartyName}"
            : request.Subject.Trim();
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? "Please find attached Bill of Material (BOM) PDF."
            : request.Body.Trim();

        await emailService.SendMailAndWaitAsync(
            request.To.Trim(),
            subject,
            body,
            [new EmailAttachmentData(fileName, "application/pdf", pdfBytes)],
            cc: request.Cc?.Trim(),
            bcc: request.Bcc?.Trim());

        _logger.LogInformation("BOM email sent for {FilePoNo} to {To}", request.FilePoNo, request.To);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "BOM" : cleaned;
    }
}
