using System.Text.Json;
using System.Text.Json.Serialization;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementOutputLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FinancialStatementOutputLogger> _logger;

    public FinancialStatementOutputLogger(IWebHostEnvironment env, ILogger<FinancialStatementOutputLogger> logger)
    {
        _env = env;
        _logger = logger;
    }

    public string? LogGeneration(
        FinancialStatementResultDto result,
        GenerateFinancialStatementRequest request,
        string? sourceFileName = null)
    {
        if (!_env.IsDevelopment())
            return null;

        var logDir = ResolveLogDirectory();
        Directory.CreateDirectory(logDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var safeKey = SanitizeFileName(result.CompanyKey);
        var fileName = $"fs-generated-{safeKey}-{timestamp}.json";
        var filePath = Path.Combine(logDir, fileName);

        var payload = new FinancialStatementGenerationLog
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SourceFileName = sourceFileName,
            Request = new GenerationLogRequest
            {
                CompanyKey = request.CompanyKey,
                CompanyName = request.CompanyName,
                PeriodLabel = request.PeriodLabel,
                Mapping = request.Mapping
            },
            Result = result,
            Summary = BuildSummary(result)
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(payload, JsonOptions));

        var latestPath = Path.Combine(logDir, "fs-generated-latest.json");
        File.WriteAllText(latestPath, JsonSerializer.Serialize(payload, JsonOptions));

        _logger.LogInformation(
            "Financial statement output logged to {LogPath} | mapped {Mapped}/{Total} | assets {Assets} | liab+eq {LiabEq} | diff {Diff}",
            filePath,
            result.MappedLedgers,
            result.TotalLedgers,
            result.TotalAssetsLakhs,
            result.TotalLiabilitiesAndEquityLakhs,
            result.BalanceSheetTotalLakhs);

        return filePath;
    }

    private static FinancialStatementLogSummary BuildSummary(FinancialStatementResultDto result)
    {
        var bsLines = result.BalanceSheet
            .SelectMany(s => s.Lines.Where(l => !l.IsHeader))
            .Select(l => new LogLineAmount { Label = l.Label, Note = l.Note, AmountLakhs = l.AmountLakhs })
            .ToList();

        var plLines = result.ProfitAndLoss
            .Where(l => !l.IsHeader)
            .Select(l => new LogLineAmount { Label = l.Label, Note = l.Note, AmountLakhs = l.AmountLakhs, IsSubtotal = l.IsSubtotal })
            .ToList();

        var schedules = result.Schedules
            .Select(s => new LogScheduleSummary
            {
                Note = s.Note,
                Title = s.Title,
                TotalLakhs = s.TotalLakhs,
                Lines = s.Lines.Select(l => new LogLineAmount
                {
                    Label = l.Label,
                    AmountLakhs = l.AmountLakhs,
                    LedgerCount = l.LedgerCount
                }).ToList()
            })
            .ToList();

        return new FinancialStatementLogSummary
        {
            TotalLedgers = result.TotalLedgers,
            MappedLedgers = result.MappedLedgers,
            UnmappedLedgers = result.UnmappedLedgers,
            TotalAssetsLakhs = result.TotalAssetsLakhs,
            TotalLiabilitiesAndEquityLakhs = result.TotalLiabilitiesAndEquityLakhs,
            BalanceSheetDiffLakhs = result.BalanceSheetTotalLakhs,
            BalanceSheetLines = bsLines,
            ProfitAndLossLines = plLines,
            Schedules = schedules,
            UnmappedTop20 = result.Unmapped
                .OrderByDescending(u => Math.Abs(u.ClosingLakhs))
                .Take(20)
                .Select(u => new LogUnmappedLedger { Ledger = u.Ledger, ClosingLakhs = u.ClosingLakhs })
                .ToList()
        };
    }

    private string ResolveLogDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, "..", "docs", "accounting", "logs"),
            Path.Combine(_env.ContentRootPath, "Data", "FinancialStatements", "logs")
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            var parent = Path.GetDirectoryName(full);
            if (parent != null && Directory.Exists(parent))
                return full;
        }

        return Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Data", "FinancialStatements", "logs"));
    }

    private static string SanitizeFileName(string value)
    {
        var sanitized = string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
        sanitized = sanitized.Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized.ToLowerInvariant();
    }
}

public class FinancialStatementGenerationLog
{
    public DateTime GeneratedAtUtc { get; set; }
    public string? SourceFileName { get; set; }
    public GenerationLogRequest Request { get; set; } = new();
    public FinancialStatementResultDto Result { get; set; } = new();
    public FinancialStatementLogSummary Summary { get; set; } = new();
}

public class GenerationLogRequest
{
    public string CompanyKey { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public TrialBalanceColumnMapping Mapping { get; set; } = new();
}

public class FinancialStatementLogSummary
{
    public int TotalLedgers { get; set; }
    public int MappedLedgers { get; set; }
    public int UnmappedLedgers { get; set; }
    public decimal TotalAssetsLakhs { get; set; }
    public decimal TotalLiabilitiesAndEquityLakhs { get; set; }
    public decimal BalanceSheetDiffLakhs { get; set; }
    public List<LogLineAmount> BalanceSheetLines { get; set; } = [];
    public List<LogLineAmount> ProfitAndLossLines { get; set; } = [];
    public List<LogScheduleSummary> Schedules { get; set; } = [];
    public List<LogUnmappedLedger> UnmappedTop20 { get; set; } = [];
}

public class LogLineAmount
{
    public string Label { get; set; } = "";
    public string Note { get; set; } = "";
    public decimal AmountLakhs { get; set; }
    public bool IsSubtotal { get; set; }
    public int LedgerCount { get; set; }
}

public class LogScheduleSummary
{
    public string Note { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal TotalLakhs { get; set; }
    public List<LogLineAmount> Lines { get; set; } = [];
}

public class LogUnmappedLedger
{
    public string Ledger { get; set; } = "";
    public decimal ClosingLakhs { get; set; }
}
