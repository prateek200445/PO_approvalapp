using System.Text.Json;

namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Shift-wise FIBC stitch targets from the All-units Excel (KPW / PIA / Gandhidham sheets).
/// </summary>
internal sealed class FibcStitchTargetCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Lazy<FibcStitchTargetCatalog> Cached = new(Load);

    public IReadOnlyList<CompanyFactoryMap> CompanyMap { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>> Factories { get; }

    private FibcStitchTargetCatalog(
        IReadOnlyList<CompanyFactoryMap> companyMap,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>> factories)
    {
        CompanyMap = companyMap;
        Factories = factories;
    }

    public static FibcStitchTargetCatalog Instance => Cached.Value;

    public string ResolveFactoryKey(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return "pia";

        foreach (var map in CompanyMap)
        {
            if (companyName.Contains(map.Contains, StringComparison.OrdinalIgnoreCase))
                return map.Factory;
        }

        return "pia";
    }

    public bool TryGetRate(string factoryKey, string activity, string heightBand, string dustColumn, out int pcsPerShift)
    {
        pcsPerShift = 0;
        if (!Factories.TryGetValue(factoryKey, out var activities))
            return false;
        if (!activities.TryGetValue(activity, out var heights))
            return false;

        if (!TryHeight(heights, heightBand, out var dusts))
            return false;

        return TryDust(dusts, dustColumn, out pcsPerShift);
    }

    public IReadOnlyList<string> HeightBands(string factoryKey, string activity)
    {
        if (!Factories.TryGetValue(factoryKey, out var activities))
            return Array.Empty<string>();
        if (!activities.TryGetValue(activity, out var heights))
            return Array.Empty<string>();
        return heights.Keys.ToList();
    }

    private static bool TryHeight(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> heights,
        string heightBand,
        out IReadOnlyDictionary<string, int> dusts)
    {
        if (heights.TryGetValue(heightBand, out dusts!))
            return true;
        if (heights.TryGetValue("_", out dusts!))
            return true;

        var ordered = new[] { "h50", "h90", "h120", "h145", "h175", "h205" };
        var idx = Array.IndexOf(ordered, heightBand);
        if (idx >= 0)
        {
            for (var delta = 1; delta < ordered.Length; delta++)
            {
                if (idx + delta < ordered.Length && heights.TryGetValue(ordered[idx + delta], out dusts!))
                    return true;
                if (idx - delta >= 0 && heights.TryGetValue(ordered[idx - delta], out dusts!))
                    return true;
            }
        }

        foreach (var kv in heights)
        {
            dusts = kv.Value;
            return true;
        }

        dusts = null!;
        return false;
    }

    private static bool TryDust(IReadOnlyDictionary<string, int> dusts, string dustColumn, out int pcs)
    {
        if (dusts.TryGetValue(dustColumn, out pcs) && pcs > 0)
            return true;
        if (dusts.TryGetValue("n", out pcs) && pcs > 0)
            return true;
        foreach (var kv in dusts)
        {
            if (kv.Value > 0)
            {
                pcs = kv.Value;
                return true;
            }
        }

        pcs = 0;
        return false;
    }

    private static FibcStitchTargetCatalog Load()
    {
        var json = ReadCatalogJson();
        if (string.IsNullOrWhiteSpace(json))
            return new FibcStitchTargetCatalog(Array.Empty<CompanyFactoryMap>(), new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>>());
        var file = JsonSerializer.Deserialize<CatalogFile>(json, JsonOptions) ?? new CatalogFile();
        var factories = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var factory in file.Factories ?? new())
        {
            var activities = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var activity in factory.Value)
            {
                var heights = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                foreach (var height in activity.Value)
                    heights[height.Key] = new Dictionary<string, int>(height.Value, StringComparer.OrdinalIgnoreCase);
                activities[activity.Key] = heights;
            }

            factories[factory.Key] = activities;
        }

        return new FibcStitchTargetCatalog(file.CompanyMap ?? [], factories);
    }

    private static string? ReadCatalogJson()
    {
        var assembly = typeof(FibcStitchTargetCatalog).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("FibcStitchTargetCatalog.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        var path = ResolveCatalogPath();
        return path is null ? null : File.ReadAllText(path);
    }

    private static string? ResolveCatalogPath()
    {
        var names = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Planning", "Fibc", "Data", "FibcStitchTargetCatalog.json"),
            Path.Combine(AppContext.BaseDirectory, "FibcStitchTargetCatalog.json"),
        };

        foreach (var name in names)
        {
            if (File.Exists(name))
                return name;
        }

        return null;
    }

    internal sealed class CompanyFactoryMap
    {
        public string Contains { get; set; } = "";
        public string Factory { get; set; } = "";
    }

    private sealed class CatalogFile
    {
        public List<CompanyFactoryMap> CompanyMap { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>> Factories { get; set; } = new();
    }
}
