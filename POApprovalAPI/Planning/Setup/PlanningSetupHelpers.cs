using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

public static class PlanningSetupHelpers
{
    public static IReadOnlyList<string> InferBagFamilies(string? erpBagType)
    {
        if (string.IsNullOrWhiteSpace(erpBagType))
            return Array.Empty<string>();

        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = BagTypeMapper.NormalizeErpFamily(erpBagType);
        if (!string.IsNullOrEmpty(normalized))
            families.Add(normalized);

        if (erpBagType.Contains("Double Loop", StringComparison.OrdinalIgnoreCase))
            families.Add("UPanel");

        if (erpBagType.Contains("Upanel", StringComparison.OrdinalIgnoreCase) ||
            erpBagType.Contains("UPanel", StringComparison.OrdinalIgnoreCase) ||
            erpBagType.Contains("U-Panel", StringComparison.OrdinalIgnoreCase))
            families.Add("UPanel");

        if (erpBagType.Contains("Buffle", StringComparison.OrdinalIgnoreCase) ||
            erpBagType.Contains("Baffle", StringComparison.OrdinalIgnoreCase))
            families.Add("Buffle");

        if (erpBagType.Contains("Circular", StringComparison.OrdinalIgnoreCase) ||
            erpBagType.Contains("XC", StringComparison.OrdinalIgnoreCase))
            families.Add("Circular");

        return families.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static (int? normal, int? single, int? dbl, int? triple) DefaultDustCapacities(string primaryFamily, int? erpCapacity)
    {
        var baseCap = erpCapacity ?? primaryFamily switch
        {
            "UPanel" => 700,
            "Buffle" => 450,
            "Circular" => 750,
            _ => 700,
        };

        return primaryFamily switch
        {
            "UPanel" => (baseCap, 650, 600, 475),
            "Buffle" => (baseCap, 450, 400, 350),
            "Circular" => (baseCap, null, null, null),
            _ => (baseCap, null, null, null),
        };
    }

    public static string SerializeFamilies(IEnumerable<string>? families) =>
        string.Join(",", (families ?? Array.Empty<string>()).Select(f => f.Trim()).Where(f => f.Length > 0));

    public static IReadOnlyList<string> DeserializeFamilies(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static bool IsFrozen(object? value)
    {
        if (value is null)
            return false;

        var s = value.ToString()?.Trim() ?? "";
        return s.Equals("1", StringComparison.OrdinalIgnoreCase)
            || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || s.Equals("y", StringComparison.OrdinalIgnoreCase)
            || s.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveEffectiveFactorSource(double? manual, double? auto)
    {
        if (manual is > 0)
            return "Manual";
        if (auto is > 0)
            return "Auto";
        return "Default";
    }

    public static double ResolveEffectiveFactor(double? manual, double? auto) =>
        manual is > 0 ? manual.Value : auto is > 0 ? auto.Value : 1.0;

    public static string InferLoomType(string? make, string? specification)
    {
        var combined = $"{make} {specification}".Trim();
        if (string.IsNullOrEmpty(combined))
            return "";

        string[] types = ["LSL", "CIRWIND", "CIRCULAR", "LOHIA", "CHINA", "8-SHUTTLE", "10-SHUTTLE", "STARLINGER", "GCL", "YMP"];
        foreach (var t in types)
        {
            if (combined.Contains(t, StringComparison.OrdinalIgnoreCase))
                return t.Equals("CIRCULAR", StringComparison.OrdinalIgnoreCase) ? "CIRWIND" : t.ToUpperInvariant();
        }

        if (combined.Contains("Cirwind", StringComparison.OrdinalIgnoreCase))
            return "CIRWIND";

        return make?.Trim() ?? "";
    }
}
