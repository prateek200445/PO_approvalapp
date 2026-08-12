using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POApprovalAPI.Services;

public class GeminiChatService : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiChatService(HttpClient http, IConfiguration config, ILogger<GeminiChatService> logger)
    {
        _http = http;
        _logger = logger;

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? config["Gemini:ApiKey"]
            ?? "";
        var baseUrl = (Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? config["Gemini:BaseUrl"]
            ?? "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');
        _model = Environment.GetEnvironmentVariable("GEMINI_MODEL")
            ?? config["Gemini:Model"]
            ?? "gemini-2.5-flash";
        _maxTokens = int.TryParse(
            Environment.GetEnvironmentVariable("GEMINI_MAX_TOKENS") ?? config["Gemini:MaxTokens"],
            out var mt)
            ? mt
            : 4096;

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("GEMINI_API_KEY is not configured.");

        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var body = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = _maxTokens
            }
        };

        var path =
            $"models/{Uri.EscapeDataString(_model)}:generateContent?key={Uri.EscapeDataString(_apiKey)}";

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.PostAsync(path, content, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(raw);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                    || candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Gemini returned no candidates: {Body}", raw);
                    return "";
                }

                var candidate = candidates[0];
                if (!candidate.TryGetProperty("content", out var contentEl)
                    || !contentEl.TryGetProperty("parts", out var parts)
                    || parts.GetArrayLength() == 0)
                {
                    var finish = candidate.TryGetProperty("finishReason", out var fr)
                        ? fr.GetString()
                        : "?";
                    _logger.LogWarning("Gemini empty content (finish={Finish}): {Body}", finish, raw);
                    return "";
                }

                var sb = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textEl))
                        sb.Append(textEl.GetString());
                }

                return sb.ToString().Trim();
            }

            var isRateLimit = (int)response.StatusCode == 429
                              || raw.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
                              || raw.Contains("rate", StringComparison.OrdinalIgnoreCase);
            if (isRateLimit && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(8 * attempt);
                _logger.LogWarning(
                    "Gemini rate limit (attempt {Attempt}/{Max}); waiting {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                continue;
            }

            _logger.LogError("Gemini error {Status}: {Body}", response.StatusCode, raw);
            throw new InvalidOperationException($"Gemini API error {(int)response.StatusCode}: {raw}");
        }

        throw new InvalidOperationException("Gemini API failed after retries.");
    }
}
