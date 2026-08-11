using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POApprovalAPI.Services;

public class GroqChatService
{
    private readonly HttpClient _http;
    private readonly ILogger<GroqChatService> _logger;
    private readonly string _model;
    private readonly int _maxTokens;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GroqChatService(HttpClient http, IConfiguration config, ILogger<GroqChatService> logger)
    {
        _http = http;
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? config["Groq:ApiKey"]
            ?? "";
        var baseUrl = (Environment.GetEnvironmentVariable("GROQ_BASE_URL")
            ?? config["Groq:BaseUrl"]
            ?? "https://api.groq.com/openai/v1").TrimEnd('/');
        _model = Environment.GetEnvironmentVariable("GROQ_MODEL")
            ?? config["Groq:Model"]
            ?? "openai/gpt-oss-120b";
        _maxTokens = int.TryParse(
            Environment.GetEnvironmentVariable("GROQ_MAX_TOKENS") ?? config["Groq:MaxTokens"],
            out var mt)
            ? mt
            : 1024;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GROQ_API_KEY is not configured.");

        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            temperature = 0.1,
            max_tokens = _maxTokens,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.PostAsync("chat/completions", content, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return text?.Trim() ?? "";
            }

            var isRateLimit = (int)response.StatusCode == 429
                              || raw.Contains("rate_limit", StringComparison.OrdinalIgnoreCase);
            if (isRateLimit && attempt < maxAttempts)
            {
                var delay = TryParseRetryDelay(raw) ?? TimeSpan.FromSeconds(12 * attempt);
                _logger.LogWarning(
                    "Groq rate limit (attempt {Attempt}/{Max}); waiting {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                continue;
            }

            _logger.LogError("Groq error {Status}: {Body}", response.StatusCode, raw);
            throw new InvalidOperationException($"Groq API error {(int)response.StatusCode}: {raw}");
        }

        throw new InvalidOperationException("Groq API failed after retries.");
    }

    private static TimeSpan? TryParseRetryDelay(string body)
    {
        // "Please try again in 10.6575s"
        var m = System.Text.RegularExpressions.Regex.Match(
            body,
            @"try again in\s+([0-9.]+)\s*s",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        if (!double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            return null;
        return TimeSpan.FromSeconds(Math.Clamp(seconds + 1.5, 2, 60));
    }
}
