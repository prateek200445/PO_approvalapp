namespace POApprovalAPI.Planning.Loom;

public static class LoomMeterCalculator
{
    /// <summary>
    /// Meters per day: PPM × 1440 / weftMesh / 3937 × efficiency.
    /// Flat double/triple winder multipliers per requirements doc.
    /// </summary>
    public static double CalculateMetersPerDay(
        double ppm,
        double weftMesh,
        double efficiency,
        LoomWinderCategory winderCategory = LoomWinderCategory.Tube)
    {
        if (ppm <= 0 || weftMesh <= 0 || efficiency <= 0)
            return 0;

        var baseMeters = ppm * 1440d / weftMesh / 3937d * efficiency;
        return winderCategory switch
        {
            LoomWinderCategory.FlatDouble => baseMeters * 2,
            LoomWinderCategory.FlatTriple => baseMeters * 3,
            _ => baseMeters,
        };
    }

    public static int DaysForMeters(double meters, double metersPerDay)
    {
        if (metersPerDay <= 0 || meters <= 0)
            return 0;
        return (int)Math.Ceiling(meters / metersPerDay);
    }
}

public enum LoomWinderCategory
{
    Tube,
    FlatDouble,
    FlatTriple,
}

public enum LoomAllotmentCase
{
    CaseI,
    CaseII,
    CaseIII,
    CaseIV,
    CaseV,
    CaseVI,
    CaseVII,
}

public static class LoomFabricMatcher
{
    public static bool IsSimilarFabric(double gsmA, double widthA, double gsmB, double widthB, double gsmTol, double widthTol) =>
        Math.Abs(gsmA - gsmB) <= gsmTol && Math.Abs(widthA - widthB) <= widthTol;

    public static bool SameWidth(double widthA, double widthB, double widthTol) =>
        Math.Abs(widthA - widthB) <= widthTol;

    public static bool SameGsm(double gsmA, double gsmB, double gsmTol) =>
        Math.Abs(gsmA - gsmB) <= gsmTol;

    public static LoomAllotmentCase ClassifyChangeoverCase(double reqGsm, double reqWidth, double loomGsm, double loomWidth, double gsmTol, double widthTol)
    {
        if (IsSimilarFabric(reqGsm, reqWidth, loomGsm, loomWidth, gsmTol, widthTol))
            return LoomAllotmentCase.CaseI;

        if (SameWidth(reqWidth, loomWidth, widthTol))
            return LoomAllotmentCase.CaseV;

        if (SameGsm(reqGsm, loomGsm, gsmTol))
            return LoomAllotmentCase.CaseVI;

        return LoomAllotmentCase.CaseVII;
    }

    public static string CaseLabel(LoomAllotmentCase c) => c switch
    {
        LoomAllotmentCase.CaseI => "Case i — similar fabric, forward",
        LoomAllotmentCase.CaseII => "Case ii — similar fabric, shift blocking orders",
        LoomAllotmentCase.CaseIII => "Case iii — similar fabric, shift following order",
        LoomAllotmentCase.CaseIV => "Case iv — similar fabric, free days around block",
        LoomAllotmentCase.CaseV => "Case v — same width, GSM changeover (backward)",
        LoomAllotmentCase.CaseVI => "Case vi — same GSM, width changeover (backward)",
        LoomAllotmentCase.CaseVII => "Case vii — full changeover (backward)",
        _ => c.ToString(),
    };

    public static string CaseCode(LoomAllotmentCase c) => c switch
    {
        LoomAllotmentCase.CaseI => "i",
        LoomAllotmentCase.CaseII => "ii",
        LoomAllotmentCase.CaseIII => "iii",
        LoomAllotmentCase.CaseIV => "iv",
        LoomAllotmentCase.CaseV => "v",
        LoomAllotmentCase.CaseVI => "vi",
        LoomAllotmentCase.CaseVII => "vii",
        _ => "?",
    };
}
