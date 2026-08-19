using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

internal static class LoomPreferenceChartDefaults
{
    public static IReadOnlyList<PlanningLoomPreferenceChartDto> SeedRows(string companyName) =>
    [
        // Tube body fabric (GSM ≤ 182) — preference rank 1 = best
        Row(companyName, "Tube", 120, 182, 85, 105, 1, "LSL", "Tube", "Blue", "Primary tube LSL"),
        Row(companyName, "Tube", 120, 182, 85, 105, 2, "LSL-6", "Tube", "Blue", "LSL-6 tube"),
        Row(companyName, "Tube", 120, 182, 85, 105, 3, "CIRWIND", "Tube", "Blue", "Cirwind tube"),
        Row(companyName, "Tube", 120, 182, 85, 105, 4, "CHINA", "Tube", "White", "China tube fallback"),
        Row(companyName, "Tube", 120, 182, 106, 120, 5, "LSL", "Tube", "Blue", "Wide tube LSL"),
        Row(companyName, "Tube", 120, 182, 106, 120, 6, "CIRWIND", "Tube", "Blue", "Wide tube cirwind"),

        // Flat fabric (GSM > 182)
        Row(companyName, "Flat", 183, 220, 100, 130, 1, "LSL", "FlatDouble", "Blue", "Flat double LSL"),
        Row(companyName, "Flat", 183, 220, 100, 130, 2, "CIRWIND", "FlatDouble", "Blue", "Flat double cirwind"),
        Row(companyName, "Flat", 183, 220, 100, 130, 3, "CHINA", "FlatTriple", "White", "Flat triple china"),
        Row(companyName, "Flat", 183, 220, 131, 160, 4, "LSL", "FlatDouble", "Blue", "Wide flat LSL"),
        Row(companyName, "Flat", 183, 220, 131, 160, 5, "CIRWIND", "FlatTriple", "Blue", "Wide flat triple"),
        Row(companyName, "Flat", 221, 280, 100, 160, 6, "LOHIA", "FlatTriple", "White", "Heavy GSM flat"),
    ];

    private static PlanningLoomPreferenceChartDto Row(
        string company,
        string fabricForm,
        double gsmMin,
        double gsmMax,
        double widthMin,
        double widthMax,
        int rank,
        string loomType,
        string winder,
        string tier,
        string notes) => new()
    {
        CompanyName = company,
        FabricForm = fabricForm,
        GsmMin = gsmMin,
        GsmMax = gsmMax,
        WidthMinCm = widthMin,
        WidthMaxCm = widthMax,
        PreferenceRank = rank,
        LoomType = loomType,
        WinderCategory = winder,
        ChangeoverTier = tier,
        Notes = notes,
    };
}
