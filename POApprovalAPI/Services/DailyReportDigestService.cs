using System.Globalization;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Options;
using POApprovalAPI.Documents;
using POApprovalAPI.Interfaces;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

public sealed class DailyReportDigestOptions
{
    public const string SectionName = "DailyReportDigest";

    public bool Enabled { get; set; } = true;
    /// <summary>Local time (in <see cref="TimeZone"/>) when the day's PDF is sent; also the cut-off for included reports.</summary>
    public string SendTime { get; set; } = "19:00";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public List<string> Recipients { get; set; } = new();
    /// <summary>Public base URL of this API; WhatsApp downloads the PDF from here.</summary>
    public string PublicBaseUrl { get; set; } = "https://po-approvalapp.onrender.com";
    /// <summary>Portal users allowed to trigger a manual send.</summary>
    public List<string> AdminUsers { get; set; } = new();
}

public sealed class DailyReportDigestService
{
    private readonly DailyReportService _reports;
    private readonly HtmlParserService _parser;
    private readonly IWhatsAppService _whatsApp;
    private readonly DatabaseService _database;
    private readonly DailyReportDigestOptions _options;
    private readonly ILogger<DailyReportDigestService> _logger;
    private static bool _tableReady;

    public DailyReportDigestService(
        DailyReportService reports,
        HtmlParserService parser,
        IWhatsAppService whatsApp,
        DatabaseService database,
        IOptions<DailyReportDigestOptions> options,
        ILogger<DailyReportDigestService> logger)
    {
        _reports = reports;
        _parser = parser;
        _whatsApp = whatsApp;
        _database = database;
        _options = options.Value;
        _logger = logger;
    }

    public DailyReportDigestOptions Options => _options;

    public TimeZoneInfo Zone => ResolveZone(_options.TimeZone);

    public TimeSpan SendTime =>
        TimeSpan.TryParseExact(_options.SendTime, @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : new TimeSpan(19, 0, 0);

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public IReadOnlyList<string> Recipients =>
        (_options.Recipients.Count > 0 ? _options.Recipients : new List<string> { "919879203799" })
        .Select(r => r.Trim()).Where(r => r.Length > 0).Distinct().ToList();

    public bool IsAdmin(string? username)
    {
        var admins = _options.AdminUsers.Count > 0 ? _options.AdminUsers : new List<string> { "prakash" };
        return !string.IsNullOrWhiteSpace(username)
            && admins.Any(a => string.Equals(a.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Reports submitted on <paramref name="date"/> before the send time (DB times are local).</summary>
    public async Task<(byte[] Pdf, int Count, DateTime Cutoff)> BuildPdfAsync(DateTime date)
    {
        var day = date.Date;
        var cutoff = day + SendTime;
        var entities = await _reports.GetReportsSubmittedBetweenAsync(day, cutoff);
        var models = entities.Select(_parser.Parse)
            .OrderBy(m => m.EmployeeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.SubmittedOn)
            .ToList();
        var pdf = new DailyReportsDigestPdfDocument(day, cutoff, LocalNow, models).GeneratePdf();
        return (pdf, models.Count, cutoff);
    }

    public static string FileNameFor(DateTime date) => FormattableString.Invariant($"Daily-Reports-{date:dd-MMM-yyyy}.pdf");

    public async Task<DailyReportDigestStatusDto> SendAsync(DateTime date, bool force, string triggeredBy, CancellationToken ct = default)
    {
        var day = date.Date;
        await EnsureTableAsync();

        var existing = await GetStatusAsync(day);
        if (!force && existing?.SentAt is not null)
            return existing;

        if (!_whatsApp.IsConfigured)
            throw new InvalidOperationException(
                "WhatsApp (Gupshup) is not configured. Set GUPSHUP_API_KEY and Gupshup:SourceNumber / Gupshup:AppName / Gupshup:DailyReportTemplateId.");

        var (pdf, count, _) = await BuildPdfAsync(day);
        var token = existing?.Token ?? NewToken();
        var fileName = FileNameFor(day);
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/DailyReport/digest/file/{token}/{fileName}";

        using (var connection = _database.CreateConnection())
        {
            await connection.ExecuteAsync(
                """
                MERGE dbo.DailyReportDigest WITH (HOLDLOCK) AS t
                USING (SELECT @DigestDate AS DigestDate) AS s ON t.DigestDate = s.DigestDate
                WHEN MATCHED THEN UPDATE SET Token = @Token, Pdf = @Pdf, ReportCount = @Count, CreatedAt = GETDATE()
                WHEN NOT MATCHED THEN INSERT (DigestDate, Token, Pdf, ReportCount) VALUES (@DigestDate, @Token, @Pdf, @Count);
                """,
                new { DigestDate = day, Token = token, Pdf = pdf, Count = count });
        }

        var caption = new[]
        {
            day.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),
            count.ToString(CultureInfo.InvariantCulture),
        };

        var sentTo = new List<string>();
        var messageIds = new List<string>();
        var errors = new List<string>();
        foreach (var recipient in Recipients)
        {
            var result = await _whatsApp.SendDocumentAsync(recipient, url, fileName, caption, ct);
            if (result.Success)
            {
                sentTo.Add(recipient);
                if (!string.IsNullOrWhiteSpace(result.MessageId))
                    messageIds.Add(result.MessageId.Trim());
            }
            else
                errors.Add($"{recipient}: {result.Error}");
        }

        using (var connection = _database.CreateConnection())
        {
            await connection.ExecuteAsync(
                """
                UPDATE dbo.DailyReportDigest
                SET SentAt = CASE WHEN @AnySent = 1 THEN GETDATE() ELSE SentAt END,
                    SentTo = @SentTo, LastError = @LastError, TriggeredBy = @TriggeredBy, Attempts = Attempts + 1,
                    MessageId = CASE WHEN @AnySent = 1 THEN @MessageIds ELSE MessageId END,
                    DeliveryStatus = CASE WHEN @AnySent = 1 THEN 'submitted' ELSE DeliveryStatus END,
                    DeliveryError = CASE WHEN @AnySent = 1 THEN NULL ELSE DeliveryError END,
                    DeliveryUpdatedAt = CASE WHEN @AnySent = 1 THEN GETDATE() ELSE DeliveryUpdatedAt END
                WHERE DigestDate = @DigestDate
                """,
                new
                {
                    DigestDate = day,
                    AnySent = sentTo.Count > 0 ? 1 : 0,
                    SentTo = sentTo.Count > 0 ? string.Join(", ", sentTo) : null,
                    LastError = errors.Count > 0 ? Truncate(string.Join(" | ", errors), 1000) : null,
                    TriggeredBy = Truncate(triggeredBy, 100),
                    MessageIds = messageIds.Count > 0 ? Truncate(string.Join(",", messageIds), 500) : null,
                });
        }

        if (errors.Count > 0)
            _logger.LogWarning("Daily report digest {Date}: send errors {Errors}", day, string.Join(" | ", errors));

        return (await GetStatusAsync(day))!;
    }

    public async Task<DailyReportDigestStatusDto?> GetStatusAsync(DateTime date)
    {
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DailyReportDigestStatusDto>(
            """
            SELECT DigestDate, Token, ReportCount, CreatedAt, SentAt, SentTo, LastError, TriggeredBy, Attempts,
                   MessageId, DeliveryStatus, DeliveryError, DeliveryUpdatedAt
            FROM dbo.DailyReportDigest WHERE DigestDate = @DigestDate
            """,
            new { DigestDate = date.Date });
    }

    /// <summary>
    /// One overall state for the UI: scheduled, not_sent, sent, delivered, read or failed.
    /// "sent" means Gupshup accepted it; delivered/read come from the Gupshup delivery callback.
    /// </summary>
    public string ResolveState(DateTime date, DailyReportDigestStatusDto? status)
    {
        if (status?.SentAt is null)
        {
            var sendAt = date.Date + SendTime;
            return LocalNow < sendAt ? "scheduled" : "not_sent";
        }
        return (status.DeliveryStatus ?? "").ToLowerInvariant() switch
        {
            "read" => "read",
            "delivered" => "delivered",
            "failed" => "failed",
            _ => "sent",
        };
    }

    private static readonly string[] DeliveryRank = ["submitted", "enqueued", "sent", "delivered", "read"];

    /// <summary>Applies a Gupshup/WhatsApp delivery event to the digest whose send produced <paramref name="messageId"/>.</summary>
    public async Task<bool> ApplyDeliveryEventAsync(string messageId, string eventType, string? error)
    {
        var id = (messageId ?? "").Trim();
        var type = (eventType ?? "").Trim().ToLowerInvariant();
        if (id.Length is 0 or > 200 || !id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '=' or ':'))
            return false;
        if (type != "failed" && Array.IndexOf(DeliveryRank, type) < 0)
            return false;

        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var rows = await connection.ExecuteAsync(
            """
            UPDATE dbo.DailyReportDigest
            SET DeliveryStatus = @Type,
                DeliveryError = CASE WHEN @Type = 'failed' THEN @Error ELSE NULL END,
                DeliveryUpdatedAt = GETDATE()
            WHERE CHARINDEX(',' + @Id + ',', ',' + ISNULL(MessageId, '') + ',') > 0
              AND (
                    (@Type = 'failed' AND ISNULL(DeliveryStatus, '') NOT IN ('delivered', 'read'))
                 OR (@Type <> 'failed' AND @Rank > CASE ISNULL(DeliveryStatus, '')
                        WHEN 'submitted' THEN 0 WHEN 'enqueued' THEN 1 WHEN 'sent' THEN 2
                        WHEN 'delivered' THEN 3 WHEN 'read' THEN 4 ELSE -1 END)
                  )
            """,
            new { Id = id, Type = type, Rank = Array.IndexOf(DeliveryRank, type), Error = Truncate(error, 500) });
        return rows > 0;
    }

    public async Task<(byte[] Pdf, DateTime Date)?> GetPdfByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64)
            return null;
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<(byte[] Pdf, DateTime DigestDate)>(
            "SELECT Pdf, DigestDate FROM dbo.DailyReportDigest WHERE Token = @Token",
            new { Token = token });
        return row.Pdf is null ? null : (row.Pdf, row.DigestDate);
    }

    private async Task EnsureTableAsync()
    {
        if (_tableReady)
            return;
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.DailyReportDigest', 'U') IS NULL
            BEGIN
              CREATE TABLE dbo.DailyReportDigest (
                DigestDate date NOT NULL CONSTRAINT PK_DailyReportDigest PRIMARY KEY,
                Token varchar(64) NOT NULL,
                Pdf varbinary(max) NOT NULL,
                ReportCount int NOT NULL,
                CreatedAt datetime NOT NULL CONSTRAINT DF_DailyReportDigest_CreatedAt DEFAULT (GETDATE()),
                SentAt datetime NULL,
                SentTo nvarchar(500) NULL,
                LastError nvarchar(1000) NULL,
                TriggeredBy varchar(100) NULL,
                Attempts int NOT NULL CONSTRAINT DF_DailyReportDigest_Attempts DEFAULT (0)
              );
              CREATE UNIQUE INDEX UX_DailyReportDigest_Token ON dbo.DailyReportDigest (Token);
            END
            IF COL_LENGTH('dbo.DailyReportDigest', 'MessageId') IS NULL
              ALTER TABLE dbo.DailyReportDigest ADD MessageId nvarchar(500) NULL;
            IF COL_LENGTH('dbo.DailyReportDigest', 'DeliveryStatus') IS NULL
              ALTER TABLE dbo.DailyReportDigest ADD DeliveryStatus varchar(30) NULL;
            IF COL_LENGTH('dbo.DailyReportDigest', 'DeliveryError') IS NULL
              ALTER TABLE dbo.DailyReportDigest ADD DeliveryError nvarchar(500) NULL;
            IF COL_LENGTH('dbo.DailyReportDigest', 'DeliveryUpdatedAt') IS NULL
              ALTER TABLE dbo.DailyReportDigest ADD DeliveryUpdatedAt datetime NULL;
            """);
        _tableReady = true;
    }

    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length > max ? value[..max] : value;

    private static TimeZoneInfo ResolveZone(string id)
    {
        foreach (var candidate in new[] { id, "Asia/Kolkata", "India Standard Time" })
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                    return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "IST", "IST");
    }
}

public sealed class DailyReportDigestStatusDto
{
    public DateTime DigestDate { get; set; }
    public string Token { get; set; } = "";
    public int ReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? SentTo { get; set; }
    public string? LastError { get; set; }
    public string? TriggeredBy { get; set; }
    public int Attempts { get; set; }
    public string? MessageId { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? DeliveryError { get; set; }
    public DateTime? DeliveryUpdatedAt { get; set; }
}

/// <summary>Sends the day's combined daily-report PDF on WhatsApp at the configured time (default 19:00 IST).</summary>
public sealed class DailyReportDigestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DailyReportDigestBackgroundService> _logger;

    public DailyReportDigestBackgroundService(IServiceScopeFactory scopes, ILogger<DailyReportDigestBackgroundService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan wait;
            using (var scope = _scopes.CreateScope())
            {
                var digest = scope.ServiceProvider.GetRequiredService<DailyReportDigestService>();
                if (!digest.Options.Enabled)
                    return;

                var now = digest.LocalNow;
                var sendAt = now.Date + digest.SendTime;
                // Catch up if the API was asleep/restarting at send time (up to 3 hours late).
                if (now >= sendAt && now < sendAt.AddHours(3))
                {
                    await TrySendAsync(digest, now.Date, stoppingToken);
                    sendAt = sendAt.AddDays(1);
                }
                else if (now >= sendAt)
                {
                    sendAt = sendAt.AddDays(1);
                }
                wait = sendAt - digest.LocalNow;
            }

            if (wait < TimeSpan.FromSeconds(1))
                wait = TimeSpan.FromSeconds(1);
            // Re-check at least every 30 minutes so clock drift or sleep doesn't skip a day.
            if (wait > TimeSpan.FromMinutes(30))
                wait = TimeSpan.FromMinutes(30);

            try
            {
                await Task.Delay(wait, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task TrySendAsync(DailyReportDigestService digest, DateTime day, CancellationToken ct)
    {
        try
        {
            var status = await digest.SendAsync(day, force: false, triggeredBy: "scheduler", ct);
            if (status.SentAt is not null)
                _logger.LogInformation("Daily report digest {Date} sent to {SentTo} ({Count} reports)", day, status.SentTo, status.ReportCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Daily report digest {Date} not sent", day);
        }
    }
}
