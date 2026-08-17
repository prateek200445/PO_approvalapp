namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Line + shift preference order from requirements (C before B before A; line order per bag family).
/// </summary>
internal static class LinePreferenceHelper
{
    private static readonly Dictionary<string, int[]> LinesByBagFamily = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UPanel"] = [1, 2, 5, 6, 7],
        ["Buffle"] = [3],
        ["Circular"] = [4, 8],
    };

    public static bool LineSupportsBagFamily(string? lineBagType, string erpFamily)
    {
        if (string.IsNullOrWhiteSpace(lineBagType))
            return false;

        var normalizedLine = BagTypeMapper.NormalizeErpFamily(lineBagType);
        if (normalizedLine.Equals(erpFamily, StringComparison.OrdinalIgnoreCase))
            return true;

        return lineBagType.Contains(erpFamily, StringComparison.OrdinalIgnoreCase)
            || (erpFamily.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                && lineBagType.Contains("Upanel", StringComparison.OrdinalIgnoreCase))
            || (erpFamily.Equals("Buffle", StringComparison.OrdinalIgnoreCase)
                && (lineBagType.Contains("Buffle", StringComparison.OrdinalIgnoreCase)
                    || lineBagType.Contains("Baffle", StringComparison.OrdinalIgnoreCase)))
            || (erpFamily.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                && lineBagType.Contains("Circular", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<int> GetPreferredLines(string erpFamily)
    {
        if (LinesByBagFamily.TryGetValue(erpFamily, out var lines))
            return lines;

        return Array.Empty<int>();
    }

    /// <summary>
    /// Preference rank — lower is better. Unlisted combinations sort last.
    /// </summary>
    public static int GetPreferenceRank(string erpFamily, int lineNo, string shift, IReadOnlyList<string> shiftPreference)
    {
        if (!LinesByBagFamily.TryGetValue(erpFamily, out var lines))
            return 10_000;

        var lineIndex = Array.IndexOf(lines, lineNo);
        if (lineIndex < 0)
            return 10_000;

        var shiftIndex = IndexOfShift(shift, shiftPreference);
        if (shiftIndex < 0)
            return 10_000;

        return shiftIndex * 100 + lineIndex;
    }

    private static int IndexOfShift(string shift, IReadOnlyList<string> shiftPreference)
    {
        for (var i = 0; i < shiftPreference.Count; i++)
        {
            if (shiftPreference[i].Equals(shift, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}
