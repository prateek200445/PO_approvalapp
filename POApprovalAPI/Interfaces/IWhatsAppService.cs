namespace POApprovalAPI.Interfaces
{
    public interface IWhatsAppService
    {
        bool IsConfigured { get; }

        Task<bool> SendMessageAsync(string mobileNumber, string message);

        /// <summary>Sends a PDF that WhatsApp downloads from <paramref name="documentUrl"/> (must be publicly reachable).</summary>
        Task<WhatsAppSendResult> SendDocumentAsync(
            string mobileNumber,
            string documentUrl,
            string fileName,
            IReadOnlyList<string> templateParams,
            CancellationToken cancellationToken = default);
    }

    public sealed record WhatsAppSendResult(bool Success, string? MessageId, string? Error);
}
