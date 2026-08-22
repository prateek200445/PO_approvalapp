using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

/// <summary>
/// ERP plant/unit short codes → canonical FactoryInfo company names.
/// Ignores codes embedded in document numbers (e.g. PIL2/RAW/25-26/289).
/// </summary>
internal static class CompanyAliasMap
{
    private static readonly (string Code, string CompanyName)[] PlantCodes =
    [
        ("pil8", "Plastene India Limited (Unit-VIII)"),
        ("pil6", "Plastene India Limited (Unit-VI)"),
        ("pil5", "Plastene India Limited (Unit-V)"),
        ("pil4", "Plastene India Limited (Unit-IV)"),
        ("pil3", "Plastene India Limited (Unit -III)"),
        ("pil2", "Plastene India Limited (Unit -II)"),
        ("pil1", "Plastene India Limited"),
        ("hpbl4", "HCP Plastene Bulkpack Ltd (Unit - IV)"),
        ("hpbl3", "HCP Plastene Bulkpack Ltd (Unit - III)"),
        ("hpbl2", "HCP Plastene Bulkpack Ltd (Unit - II)"),
        ("oel5", "Oswal Extrusion Limited (Unit-V)"),
        ("oel4", "Oswal Extrusion Limited (Unit-IV)"),
        ("oel3", "Oswal Extrusion Limited (Unit-III)"),
        ("oel2", "Oswal Extrusion Limited (Unit-II)"),
        ("kpw3", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)"),
        ("kpw2", "K.P. WOVEN PRIVATE LIMITED (UNIT-II)"),
        ("kpw1", "K.P. WOVEN PRIVATE LIMITED"),
        ("kpv", "K.P. WOVEN PRIVATE LIMITED"),
        ("kpw", "K.P. WOVEN PRIVATE LIMITED"),
        ("ppl", "Plastene Polyfilms Limited"),
        ("hpbl", "HCP Plastene Bulkpack Ltd"),
        ("oel", "Oswal Extrusion Limited"),
        ("hcp", "HCP Plastene Bulkpack Ltd"),
        ("pil", "Plastene India Limited"),
    ];

    internal static string? Resolve(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        var m = message.ToLowerInvariant();

        foreach (var (code, company) in PlantCodes)
        {
            if (HasStandalonePlantCode(message, code))
                return company;
        }

        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:8|viii)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit-VIII)";
        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:6|vi)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit-VI)";
        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:5|v)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit-V)";
        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:4|iv)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit-IV)";
        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:3|iii)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit -III)";
        if (Regex.IsMatch(m, @"\bunit\s*[-]?\s*(?:2|ii)\b") && m.Contains("plastene"))
            return "Plastene India Limited (Unit -II)";

        if (m.Contains("oswal")) return "Oswal Extrusion Limited";
        if ((m.Contains("k.p") || m.Contains("kp ") || m.Contains("kp woven") || m.Contains("kpwoven"))
            && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        if (Regex.IsMatch(m, @"\bkp\b") && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        if (m.Contains("polyfilms") || HasStandalonePlantCode(message, "ppl"))
            return "Plastene Polyfilms Limited";
        if (m.Contains("bulkpack") || m.Contains("hcp plastene"))
            return "HCP Plastene Bulkpack Ltd";
        if (m.Contains("plastene india") && Regex.IsMatch(m, @"\bunit\b"))
            return "Plastene India Limited (Unit -II)";
        if (m.Contains("plastene india")) return "Plastene India Limited";

        return null;
    }

    /// <summary>Plant code as a word, not the prefix of PO/MRN/GP/PR document numbers.</summary>
    internal static bool HasStandalonePlantCode(string message, string code) =>
        Regex.IsMatch(message, $@"(?<![A-Za-z0-9/]){Regex.Escape(code)}(?!\s*/)", RegexOptions.IgnoreCase);
}
