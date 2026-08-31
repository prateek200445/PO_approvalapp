using System.Globalization;
using System.Text.RegularExpressions;

namespace POApprovalAPI.Planning.Bom;

/// <summary>
/// Maps free-text BOM <c>Heading</c> values to planning categories.
/// Loom-eligible fabric is scheduled on looms; accessories are readiness-only for now.
/// </summary>
public static class BomComponentClassifier
{
    public const string KindLoomFabric = "LoomFabric";
    public const string KindAccessory = "Accessory";
    public const string KindAdjustment = "Adjustment";
    public const string KindOther = "Other";

    public readonly record struct Classification(
        string Category,
        string PlanningKind,
        bool IsLoomEligible);

    public static Classification Classify(
        string? heading,
        string? gsm,
        double? fabricSize,
        double? totalMtr,
        double? totalKg)
    {
        var normalized = NormalizeHeading(heading);
        var category = ResolveCategory(normalized);
        var kind = ResolveKind(category, normalized);
        var gsmValue = ParseGsm(gsm);
        var hasMeters = totalMtr is > 0.01;
        var hasWidth = fabricSize is > 0;
        var loomEligible = kind == KindLoomFabric && hasMeters && (hasWidth || gsmValue > 0);
        return new Classification(category, kind, loomEligible);
    }

    public static double ParseGsm(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        var trimmed = raw.Trim();
        var composite = Regex.Match(trimmed, @"^(\d+(?:\.\d+)?)\s*\+\s*(\d+(?:\.\d+)?)$");
        if (composite.Success
            && double.TryParse(composite.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
            && double.TryParse(composite.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
        {
            return a + b;
        }

        var digits = Regex.Match(trimmed, @"\d+(?:\.\d+)?");
        if (digits.Success
            && double.TryParse(digits.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return 0;
    }

    public static int SortRank(string category) => category switch
    {
        "Body" => 1,
        "Side" => 2,
        "Top" => 3,
        "Bottom" => 4,
        "Baffle" => 5,
        "Spout" => 6,
        "Duffle" => 7,
        "Flap" => 8,
        "Leno" => 9,
        "Patch" => 10,
        "Loop" => 20,
        "Liner" => 21,
        "Webbing" => 22,
        "Thread" => 23,
        "Label" => 24,
        "DocPouch" => 25,
        "Rope" => 26,
        "FillerCord" => 27,
        "Felt" => 28,
        "Block" => 29,
        "Tie" => 30,
        "Adjustment" => 90,
        _ => 50,
    };

    public static string NormalizeHeading(string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
            return "";

        var trimmed = Regex.Replace(heading.Trim(), @"\s+", " ");
        return trimmed.TrimEnd(' ', '~', '.').Trim();
    }

    public static bool HeadingsMatch(string? left, string? right)
    {
        var a = NormalizeHeading(left);
        var b = NormalizeHeading(right);
        return !string.IsNullOrEmpty(a) && a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> MatchKeywords(string category) => category switch
    {
        "Thread" => ["THREAD", "SEWING", "YARN"],
        "Label" => ["LABEL"],
        "DocPouch" => ["DOCPOUCH", "DOC POUCH", "POUCH", "DOCUMENT"],
        "Liner" => ["LINER"],
        "Webbing" => ["WEBB", "MFWEB", "WEBBING"],
        "Rope" => ["ROPE"],
        "FillerCord" => ["FILLER", "CORD"],
        "Felt" => ["FELT"],
        "Block" => ["B-LOCK", "B LOCK", "BLOCK"],
        "Tie" => ["TIE"],
        "Loop" => ["LOOP"],
        _ => Array.Empty<string>(),
    };

    private static string ResolveKind(string category, string heading)
    {
        if (category is "Adjustment")
            return KindAdjustment;

        if (category is "Body" or "Side" or "Top" or "Bottom" or "Baffle" or "Spout"
            or "Duffle" or "Flap" or "Leno" or "Patch" or "Reinforce")
            return KindLoomFabric;

        if (category is "Thread" or "Label" or "DocPouch" or "Liner" or "Webbing"
            or "Rope" or "FillerCord" or "Felt" or "Block" or "Tie" or "Loop")
            return KindAccessory;

        if (heading.Contains("INNER", StringComparison.OrdinalIgnoreCase)
            && (heading.Contains("BODY", StringComparison.OrdinalIgnoreCase)
                || heading.Contains("SIDE", StringComparison.OrdinalIgnoreCase)
                || heading.Contains("TOP", StringComparison.OrdinalIgnoreCase)
                || heading.Contains("BOTTOM", StringComparison.OrdinalIgnoreCase)
                || heading.Contains("BUFF", StringComparison.OrdinalIgnoreCase)))
            return KindLoomFabric;

        return KindOther;
    }

    private static string ResolveCategory(string heading)
    {
        if (string.IsNullOrEmpty(heading))
            return "Other";

        if (ContainsAny(heading, "LESS ", "EXCESS", "WASTAGE", "WASTE"))
            return "Adjustment";

        if (ContainsAny(heading, "THREAD"))
            return "Thread";

        if (ContainsAny(heading, "DOCPOUCH", "DOC POUCH", "POUCH"))
            return "DocPouch";

        if (ContainsAny(heading, "LABEL"))
            return "Label";

        if (ContainsAny(heading, "LINER"))
            return "Liner";

        if (ContainsAny(heading, "FELT"))
            return "Felt";

        if (ContainsAny(heading, "FILLER CORD", "FILLER CORD", "FIILER CORD"))
            return "FillerCord";

        if (ContainsAny(heading, "MFWEB", "WEBBING", "WEB"))
            return "Webbing";

        if (ContainsAny(heading, "TIE", "SLIT TIE"))
            return "Tie";

        if (ContainsAny(heading, "ROPE"))
            return "Rope";

        if (ContainsAny(heading, "B-LOCK", "B LOCK", "BLOCK"))
            return "Block";

        if (ContainsAny(heading, "SPOUT"))
            return "Spout";

        if (ContainsAny(heading, "DUFFLE", "SKRIT", "SKIRT"))
            return "Duffle";

        if (ContainsAny(heading, "BUFFLE", "BAFFLE"))
            return "Baffle";

        if (ContainsAny(heading, "LOOP"))
            return "Loop";

        if (ContainsAny(heading, "LENO"))
            return "Leno";

        if (ContainsAny(heading, "PATCH", "REINFORCE"))
            return heading.Contains("REINFORCE", StringComparison.OrdinalIgnoreCase) ? "Reinforce" : "Patch";

        if (ContainsAny(heading, "FLAP"))
            return "Flap";

        if (ContainsAny(heading, "BODY"))
            return "Body";

        if (ContainsAny(heading, "SIDE"))
            return "Side";

        if (ContainsAny(heading, "BOTTOM"))
            return "Bottom";

        if (ContainsAny(heading, "TOP"))
            return "Top";

        return "Other";
    }

    private static bool ContainsAny(string heading, params string[] needles) =>
        needles.Any(n => heading.Contains(n, StringComparison.OrdinalIgnoreCase));
}
