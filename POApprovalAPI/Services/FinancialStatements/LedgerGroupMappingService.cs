using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using System.Text.Json;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class LedgerGroupMappingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly FinancialStatementTemplateStore _templates;
    private readonly string _companiesDir;

    public LedgerGroupMappingService(FinancialStatementTemplateStore templates, IWebHostEnvironment env)
    {
        _templates = templates;
        _companiesDir = Path.Combine(env.ContentRootPath, "Data", "FinancialStatements", "companies");
        Directory.CreateDirectory(_companiesDir);
    }

    public IReadOnlyList<LedgerGroupMappingDto> GetMappings(string companyKey)
    {
        var key = NormalizeCompanyKey(companyKey);
        var companyPath = GetCompanyMappingPath(key);
        if (File.Exists(companyPath))
        {
            var json = File.ReadAllText(companyPath);
            return System.Text.Json.JsonSerializer.Deserialize<List<LedgerGroupMappingDto>>(json, JsonOptions) ?? [];
        }

        return _templates.GetDefaultMappings().ToList();
    }

    public bool HasCustomMapping(string companyKey)
    {
        return File.Exists(GetCompanyMappingPath(NormalizeCompanyKey(companyKey)));
    }

    public void SaveMappings(SaveMappingRequest request)
    {
        var key = NormalizeCompanyKey(request.CompanyKey);
        var path = GetCompanyMappingPath(key);
        var payload = request.Mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.Ledger))
            .Select(m => new LedgerGroupMappingDto
            {
                Ledger = m.Ledger.Trim(),
                Group = m.Group?.Trim() ?? ""
            })
            .ToList();

        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(payload, JsonOptions));

        var metaPath = Path.Combine(_companiesDir, key, "meta.json");
        Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);
        File.WriteAllText(metaPath, System.Text.Json.JsonSerializer.Serialize(new
        {
            companyKey = key,
            companyName = request.CompanyName?.Trim() ?? key,
            updatedAt = DateTime.UtcNow
        }, JsonOptions));
    }

    public List<CompanyMappingSummaryDto> ListCompanies()
    {
        var results = new List<CompanyMappingSummaryDto>
        {
            new()
            {
                CompanyKey = "default",
                CompanyName = "Default template",
                MappingCount = _templates.GetDefaultMappings().Count,
                UsesDefaultMapping = true
            }
        };

        if (!Directory.Exists(_companiesDir))
            return results;

        foreach (var dir in Directory.GetDirectories(_companiesDir))
        {
            var key = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(key) || key.Equals("default", StringComparison.OrdinalIgnoreCase))
                continue;

            var mappingPath = Path.Combine(dir, "ledger-groups.json");
            if (!File.Exists(mappingPath))
                continue;

            var name = key;
            var metaPath = Path.Combine(dir, "meta.json");
            if (File.Exists(metaPath))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(metaPath));
                if (doc.RootElement.TryGetProperty("companyName", out var cn))
                    name = cn.GetString() ?? key;
            }

            var count = System.Text.Json.JsonSerializer
                .Deserialize<List<LedgerGroupMappingDto>>(File.ReadAllText(mappingPath), JsonOptions)?.Count ?? 0;

            results.Add(new CompanyMappingSummaryDto
            {
                CompanyKey = key,
                CompanyName = name,
                MappingCount = count,
                UsesDefaultMapping = false
            });
        }

        return results;
    }

    public Dictionary<string, string> BuildLookup(string companyKey, IEnumerable<TrialBalanceRowDto> rows)
    {
        var mappings = GetMappings(companyKey);
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in mappings)
        {
            if (string.IsNullOrWhiteSpace(map.Ledger) || string.IsNullOrWhiteSpace(map.Group))
                continue;
            lookup[map.Ledger.Trim()] = map.Group.Trim();
        }

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Group) && !lookup.ContainsKey(row.Ledger))
                lookup[row.Ledger] = row.Group.Trim();
        }

        return lookup;
    }

    private string GetCompanyMappingPath(string companyKey)
        => Path.Combine(_companiesDir, companyKey, "ledger-groups.json");

    private static string NormalizeCompanyKey(string companyKey)
    {
        var key = (companyKey ?? "default").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
            key = "default";
        key = Regex.Replace(key, @"[^a-z0-9\-_]+", "-");
        key = Regex.Replace(key, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(key) ? "default" : key;
    }
}
