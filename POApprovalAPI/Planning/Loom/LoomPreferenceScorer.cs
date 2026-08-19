using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Loom;

/// <summary>
/// Ranks looms using portal preference-chart rows (fabric form × GSM × width × loom type × winder).
/// Lower score = more preferred.
/// </summary>
public static class LoomPreferenceScorer
{
    public const double TubeFlatGsmThreshold = 182.0;

    public static string ResolveFabricForm(double gsm) =>
        gsm <= TubeFlatGsmThreshold ? "Tube" : "Flat";

    public static int Score(
        double reqGsm,
        double reqWidth,
        PlanningLoomPoolDto? pool,
        string? erpLoomSpecification,
        string? erpMake,
        IReadOnlyList<PlanningLoomPreferenceChartDto> chart,
        LoomAllotmentCase? changeoverCase = null)
    {
        if (pool is not null)
        {
            if (pool.GsmMin is > 0 && reqGsm < pool.GsmMin)
                return 500;
            if (pool.GsmMax is > 0 && reqGsm > pool.GsmMax)
                return 500;
            if (pool.WidthMinCm is > 0 && reqWidth < pool.WidthMinCm)
                return 500;
            if (pool.WidthMaxCm is > 0 && reqWidth > pool.WidthMaxCm)
                return 500;
        }

        if (chart.Count == 0)
            return FallbackPoolScore(pool);

        var fabricForm = ResolveFabricForm(reqGsm);
        var loomType = NormalizeLoomType(pool?.LoomType, erpMake, erpLoomSpecification);
        var winder = NormalizeWinder(pool?.WinderCategory);

        var matches = chart
            .Where(r => r.FabricForm.Equals(fabricForm, StringComparison.OrdinalIgnoreCase))
            .Where(r => reqGsm >= r.GsmMin && reqGsm <= r.GsmMax)
            .Where(r => reqWidth >= r.WidthMinCm && reqWidth <= r.WidthMaxCm)
            .Where(r => LoomTypeMatches(r.LoomType, loomType))
            .Where(r => string.IsNullOrWhiteSpace(r.WinderCategory) ||
                        r.WinderCategory.Equals(winder, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.PreferenceRank)
            .ToList();

        if (matches.Count == 0)
            return FallbackPoolScore(pool) + 100;

        var best = matches[0];
        var score = best.PreferenceRank * 10;

        if (changeoverCase is LoomAllotmentCase.CaseV or LoomAllotmentCase.CaseVI or LoomAllotmentCase.CaseVII)
        {
            if (best.ChangeoverTier.Equals("White", StringComparison.OrdinalIgnoreCase))
                score += 15;
            else if (!best.ChangeoverTier.Equals("Blue", StringComparison.OrdinalIgnoreCase))
                score += 8;
        }

        return score;
    }

    private static int FallbackPoolScore(PlanningLoomPoolDto? pool) =>
        string.IsNullOrWhiteSpace(pool?.LoomType) ? 80 : 40;

    private static string NormalizeWinder(string? category) =>
        category?.Trim() switch
        {
            "FlatDouble" => "FlatDouble",
            "FlatTriple" => "FlatTriple",
            _ => "Tube",
        };

    private static string NormalizeLoomType(string? poolType, string? make, string? specification)
    {
        if (!string.IsNullOrWhiteSpace(poolType))
            return poolType.Trim().ToUpperInvariant();

        var combined = $"{make} {specification}".Trim();
        if (string.IsNullOrEmpty(combined))
            return "";

        string[] types = ["LSL-6", "LSL", "CIRWIND", "CHINA", "LOHIA", "STARLINGER", "GCL", "YMP", "8-SHUTTLE", "10-SHUTTLE"];
        foreach (var t in types)
        {
            if (combined.Contains(t, StringComparison.OrdinalIgnoreCase))
                return t.Equals("CIRWIND", StringComparison.OrdinalIgnoreCase) ? "CIRWIND" : t.ToUpperInvariant();
        }

        return make?.Trim().ToUpperInvariant() ?? "";
    }

    private static bool LoomTypeMatches(string chartType, string loomType)
    {
        if (string.IsNullOrWhiteSpace(chartType))
            return true;
        if (string.IsNullOrWhiteSpace(loomType))
            return false;

        return chartType.Equals(loomType, StringComparison.OrdinalIgnoreCase) ||
               loomType.Contains(chartType, StringComparison.OrdinalIgnoreCase) ||
               chartType.Contains(loomType, StringComparison.OrdinalIgnoreCase);
    }
}
