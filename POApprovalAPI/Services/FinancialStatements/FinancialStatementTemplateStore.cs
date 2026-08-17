using System.Text.Json;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementTemplateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _dataDir;
    private ReportStructure? _structure;
    private List<LedgerGroupMappingDto>? _defaultMappings;

    public FinancialStatementTemplateStore(IWebHostEnvironment env)
    {
        _dataDir = Path.Combine(env.ContentRootPath, "Data", "FinancialStatements");
    }

    public ReportStructure GetStructure()
    {
        _structure ??= LoadJson<ReportStructure>(Path.Combine(_dataDir, "report-structure.json"))
            ?? throw new InvalidOperationException("Financial statement report structure is missing.");
        return _structure;
    }

    public IReadOnlyList<LedgerGroupMappingDto> GetDefaultMappings()
    {
        _defaultMappings ??= LoadJson<List<LedgerGroupMappingDto>>(Path.Combine(_dataDir, "default-ledger-groups.json")) ?? [];
        return _defaultMappings;
    }

    public IReadOnlyList<string> GetAllScheduleGroups()
    {
        return LoadJson<List<string>>(Path.Combine(_dataDir, "schedule-groups.json")) ?? [];
    }

    private static T? LoadJson<T>(string path)
    {
        if (!File.Exists(path))
            return default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}

public class ReportStructure
{
    public decimal AmountDivisor { get; set; } = 100_000m;
    public List<ScheduleDefinition> Schedules { get; set; } = [];
    public Dictionary<string, string> GroupNature { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public BalanceSheetDefinition BalanceSheet { get; set; } = new();
    public ProfitLossDefinition ProfitLoss { get; set; } = new();
}

public class ScheduleDefinition
{
    public string Note { get; set; } = "";
    public string Title { get; set; } = "";
    public List<string> Groups { get; set; } = [];
}

public class BalanceSheetDefinition
{
    public List<BalanceSheetSectionDefinition> Sections { get; set; } = [];
}

public class BalanceSheetSectionDefinition
{
    public string Title { get; set; } = "";
    public List<ReportLineDefinition> Lines { get; set; } = [];
}

public class ProfitLossDefinition
{
    public List<ReportLineDefinition> Lines { get; set; } = [];
}

public class ReportLineDefinition
{
    public string Label { get; set; } = "";
    public string Note { get; set; } = "";
    public string Type { get; set; } = "line";
    public string Nature { get; set; } = "";
    public List<string> Groups { get; set; } = [];
    public List<string> SumLabels { get; set; } = [];
    public string Formula { get; set; } = "";
}
