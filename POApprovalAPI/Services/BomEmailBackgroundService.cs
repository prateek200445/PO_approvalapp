using System.Collections.Concurrent;
using System.Threading.Channels;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

/// <summary>Queues BOM PDF generation + SMTP off the HTTP request thread.</summary>
public sealed class BomEmailBackgroundService : BackgroundService
{
    private const int MaxStoredJobs = 50;

    private readonly Channel<(string JobId, BomSendEmailRequest Request)> _queue =
        Channel.CreateUnbounded<(string, BomSendEmailRequest)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly ConcurrentDictionary<string, BomEmailJobStatus> _jobs = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BomEmailBackgroundService> _logger;

    public BomEmailBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BomEmailBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string? TryQueueSend(BomSendEmailRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N")[..12];
        var status = new BomEmailJobStatus
        {
            JobId = jobId,
            FilePoNo = request.FilePoNo.Trim(),
            To = request.To.Trim(),
            State = "queued",
            QueuedAt = DateTime.UtcNow,
        };

        if (!_queue.Writer.TryWrite((jobId, request)))
            return null;

        _jobs[jobId] = status;
        TrimOldJobs();
        return jobId;
    }

    public BomEmailJobStatus? GetJobStatus(string jobId) =>
        _jobs.TryGetValue(jobId, out var status) ? status : null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (jobId, request) in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSendAsync(jobId, request, stoppingToken);
            }
            catch (Exception ex)
            {
                MarkFailed(jobId, ex.Message);
                _logger.LogError(ex, "BOM background email failed for {FilePoNo}", request.FilePoNo);
            }
        }
    }

    private async Task ProcessSendAsync(string jobId, BomSendEmailRequest request, CancellationToken ct)
    {
        UpdateState(jobId, "loading_bom");

        using var scope = _scopeFactory.CreateScope();
        var bomService = scope.ServiceProvider.GetRequiredService<BomService>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var model = await bomService.BuildPdfModelAsync(request.FilePoNo);
        if (model is null)
        {
            MarkFailed(jobId, "BOM not found for this quotation number.");
            _logger.LogWarning("BOM email skipped — not found: {FilePoNo}", request.FilePoNo);
            return;
        }

        ct.ThrowIfCancellationRequested();
        UpdateState(jobId, "building_pdf");

        var pdfBytes = await Task.Run(() => new BillOfMaterialDocument(model).GeneratePdf(), ct);
        var fileName = $"{SanitizeFileName(model.QtnNo)}.pdf";
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"BOM - {model.QtnNo} - {model.PartyName}"
            : request.Subject.Trim();
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? "Please find attached Bill of Material (BOM) PDF."
            : request.Body.Trim();

        UpdateState(jobId, "sending");
        await emailService.SendMailAndWaitAsync(
            request.To.Trim(),
            subject,
            body,
            [new EmailAttachmentData(fileName, "application/pdf", pdfBytes)],
            cc: request.Cc?.Trim(),
            bcc: request.Bcc?.Trim());

        MarkSent(jobId);
        _logger.LogInformation("BOM email sent for {FilePoNo} to {To}", request.FilePoNo, request.To);
    }

    private void UpdateState(string jobId, string state)
    {
        if (_jobs.TryGetValue(jobId, out var status))
            status.State = state;
    }

    private void MarkSent(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var status))
        {
            status.State = "sent";
            status.CompletedAt = DateTime.UtcNow;
            status.Error = null;
        }
    }

    private void MarkFailed(string jobId, string error)
    {
        if (_jobs.TryGetValue(jobId, out var status))
        {
            status.State = "failed";
            status.Error = error;
            status.CompletedAt = DateTime.UtcNow;
        }
    }

    private void TrimOldJobs()
    {
        if (_jobs.Count <= MaxStoredJobs)
            return;

        foreach (var key in _jobs
                     .OrderBy(kvp => kvp.Value.QueuedAt)
                     .Take(_jobs.Count - MaxStoredJobs)
                     .Select(kvp => kvp.Key)
                     .ToList())
        {
            _jobs.TryRemove(key, out _);
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "BOM" : cleaned;
    }
}
