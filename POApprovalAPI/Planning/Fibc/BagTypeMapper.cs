namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Maps ERP bag type strings to planning families. Centralize future BOM → line rules here.
/// </summary>
public static class BagTypeMapper
{
    private static readonly Dictionary<string, string> ErpFamilyToLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UPanel"] = "U-Panel",
        ["Upanel"] = "U-Panel",
        ["Buffle"] = "Baffle",
        ["Circular"] = "XC / Circular",
        ["4-panel"] = "4-Panel",
    };

    public static string ToDisplayLabel(string? erpBagType)
    {
        if (string.IsNullOrWhiteSpace(erpBagType))
            return "—";

        var trimmed = erpBagType.Trim();
        if (ErpFamilyToLabel.TryGetValue(trimmed, out var label))
            return label;

        if (trimmed.Contains("UPanel", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("U-Panel", StringComparison.OrdinalIgnoreCase))
            return "U-Panel";

        if (trimmed.Contains("Buffle", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("Baffle", StringComparison.OrdinalIgnoreCase))
            return "Baffle";

        if (trimmed.Contains("Circular", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("XC", StringComparison.OrdinalIgnoreCase))
            return "XC / Circular";

        return trimmed;
    }

    public static string NormalizeErpFamily(string? erpBagType)
    {
        if (string.IsNullOrWhiteSpace(erpBagType))
            return "";

        var trimmed = erpBagType.Trim();
        foreach (var key in ErpFamilyToLabel.Keys)
        {
            if (trimmed.Equals(key, StringComparison.OrdinalIgnoreCase))
                return key.Equals("Upanel", StringComparison.OrdinalIgnoreCase) ? "UPanel" : key;
        }

        if (trimmed.Contains("UPanel", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("Upanel", StringComparison.OrdinalIgnoreCase))
            return "UPanel";

        if (trimmed.Contains("Buffle", StringComparison.OrdinalIgnoreCase))
            return "Buffle";

        if (trimmed.Contains("Circular", StringComparison.OrdinalIgnoreCase))
            return "Circular";

        return trimmed;
    }
}
