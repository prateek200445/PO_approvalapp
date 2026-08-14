using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

public sealed class BomEmailBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BomEmailBackgroundService> _logger;

    public BomEmailBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BomEmailBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void QueueSend(BomSendEmailRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
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

                var pdfBytes = new BillOfMaterialDocument(model).GeneratePdf();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOM background email failed for {FilePoNo}", request.FilePoNo);
            }
        });
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "BOM" : cleaned;
    }
}
