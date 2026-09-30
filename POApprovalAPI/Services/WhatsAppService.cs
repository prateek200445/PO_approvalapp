using System.Text.Json;
using Microsoft.Extensions.Options;
using POApprovalAPI.Interfaces;

namespace POApprovalAPI.Services
{
    public sealed class GupshupOptions
    {
        public const string SectionName = "Gupshup";

        public string BaseUrl { get; set; } = "https://api.gupshup.io";
        /// <summary>Set via GUPSHUP_API_KEY env var; never commit it.</summary>
        public string ApiKey { get; set; } = "";
        /// <summary>Registered WhatsApp business number, digits with country code (e.g. 9179xxxxxxxx).</summary>
        public string SourceNumber { get; set; } = "";
        /// <summary>Gupshup app name that owns the source number (src.name).</summary>
        public string AppName { get; set; } = "";
        /// <summary>
        /// Approved template id with a Document header. Required for scheduled sends: WhatsApp only allows
        /// free-form messages within 24 hours of the recipient's last message.
        /// </summary>
        public string DailyReportTemplateId { get; set; } = "";
    }

    /// <summary>WhatsApp via Gupshup (https://docs.gupshup.io/docs/template-messages).</summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _http;
        private readonly GupshupOptions _options;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient http, IOptions<GupshupOptions> options, ILogger<WhatsAppService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.ApiKey)
            && !string.IsNullOrWhiteSpace(_options.SourceNumber)
            && !string.IsNullOrWhiteSpace(_options.AppName);

        public async Task<bool> SendMessageAsync(string mobileNumber, string message)
        {
            var result = await PostAsync("/wa/api/v1/msg", mobileNumber, new Dictionary<string, string>
            {
                ["message"] = JsonSerializer.Serialize(new { type = "text", text = message }),
            }, CancellationToken.None);
            return result.Success;
        }

        public Task<WhatsAppSendResult> SendDocumentAsync(
            string mobileNumber,
            string documentUrl,
            string fileName,
            IReadOnlyList<string> templateParams,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(_options.DailyReportTemplateId))
            {
                return PostAsync("/wa/api/v1/template/msg", mobileNumber, new Dictionary<string, string>
                {
                    ["template"] = JsonSerializer.Serialize(new { id = _options.DailyReportTemplateId.Trim(), @params = templateParams }),
                    ["message"] = JsonSerializer.Serialize(new
                    {
                        type = "document",
                        document = new { link = documentUrl, filename = fileName },
                    }),
                }, cancellationToken);
            }

            // Session message: only delivered if the recipient messaged the business number in the last 24h.
            return PostAsync("/wa/api/v1/msg", mobileNumber, new Dictionary<string, string>
            {
                ["message"] = JsonSerializer.Serialize(new { type = "file", url = documentUrl, filename = fileName }),
            }, cancellationToken);
        }

        private async Task<WhatsAppSendResult> PostAsync(
            string path,
            string mobileNumber,
            Dictionary<string, string> fields,
            CancellationToken cancellationToken)
        {
            if (!IsConfigured)
                return new WhatsAppSendResult(false, null, "Gupshup is not configured (API key, source number, app name).");

            var destination = NormalizeNumber(mobileNumber);
            if (destination.Length < 10)
                return new WhatsAppSendResult(false, null, $"Invalid WhatsApp number '{mobileNumber}'.");

            fields["channel"] = "whatsapp";
            fields["source"] = NormalizeNumber(_options.SourceNumber);
            fields["destination"] = destination;
            fields["src.name"] = _options.AppName.Trim();

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.TrimEnd('/') + path)
            {
                Content = new FormUrlEncodedContent(fields),
            };
            request.Headers.Add("apikey", _options.ApiKey.Trim());

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                string? status = null, messageId = null, message = null;
                try
                {
                    using var json = JsonDocument.Parse(body);
                    var root = json.RootElement;
                    status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
                    messageId = root.TryGetProperty("messageId", out var m) ? m.GetString() : null;
                    message = root.TryGetProperty("message", out var msg) ? msg.ToString() : null;
                }
                catch (JsonException)
                {
                    message = body;
                }

                var ok = response.IsSuccessStatusCode
                    && (status is null || string.Equals(status, "submitted", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status, "success", StringComparison.OrdinalIgnoreCase));
                if (!ok)
                {
                    _logger.LogWarning("Gupshup send to {Destination} failed: {Status} {Body}", destination, (int)response.StatusCode, body);
                    return new WhatsAppSendResult(false, messageId, message ?? $"HTTP {(int)response.StatusCode}");
                }
                return new WhatsAppSendResult(true, messageId, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Gupshup send to {Destination} failed", destination);
                return new WhatsAppSendResult(false, null, ex.Message);
            }
        }

        private static string NormalizeNumber(string value)
        {
            var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
            return digits.Length == 10 ? "91" + digits : digits;
        }
    }
}
