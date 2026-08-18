namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningOptions
{
    public string DefaultCompanyName { get; set; } = "Plastene India Limited (Unit -II)";

    /// <summary>Default ERP CompanyId when not resolved from master (Unit-II sample data uses 1).</summary>
    public int DefaultCompanyId { get; set; } = 1;

    /// <summary>Days before FIBC fabric requirement by which loom fabric must complete.</summary>
    public int FabricBufferDays { get; set; } = 5;

    /// <summary>Do not plan more than this many days before fabric completion date.</summary>
    public int MaxPlanningHorizonDays { get; set; } = 30;

    /// <summary>Max consecutive days on one loom before spilling to another.</summary>
    public int MaxDaysPerLoomSegment { get; set; } = 14;

    /// <summary>Soft cap — allotment warns when a day would exceed this many changeovers.</summary>
    public int MaxChangeoversPerDay { get; set; } = 4;

    public double DefaultEfficiency { get; set; } = 0.80;

    /// <summary>GSM tolerance when matching similar fabric on a loom.</summary>
    public double GsmMatchTolerance { get; set; } = 2;

    /// <summary>Width (cm) tolerance when matching similar fabric.</summary>
    public double WidthMatchTolerance { get; set; } = 1;

    /// <summary>Fallback PPM when LoomSpecificationMaster has no row.</summary>
    public double DefaultPpm { get; set; } = 120;

    /// <summary>Fallback weft mesh when formula lookup fails.</summary>
    public double DefaultWeftMesh { get; set; } = 10;

    /// <summary>PPM by loom specification keyword (contains match, case-insensitive).</summary>
    public Dictionary<string, double> DefaultPpmByLoomType { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LSL"] = 110,
        ["CIRWIND"] = 105,
        ["CIRCULAR"] = 105,
        ["LOHIA"] = 115,
        ["STARlinger"] = 100,
        ["CHINA"] = 108,
    };

    /// <summary>Full PPM matrix when LoomSpecificationMaster is empty in ERP.</summary>
    public LoomEmbeddedPpmEntry[] EmbeddedPpmMatrix { get; set; } = [];

    public bool AllowConfirmSave { get; set; }

    public bool AllowReplaceExistingPlan { get; set; } = true;

    /// <summary>When true, confirm may UPDATE displaced blocking rows then insert new plan.</summary>
    public bool AllowShiftOnConfirm { get; set; } = true;
}

public sealed class LoomEmbeddedPpmEntry
{
    public string LoomType { get; set; } = "";
    public double GsmFrom { get; set; }
    public double GsmTo { get; set; }
    public double WidthFrom { get; set; }
    public double WidthTo { get; set; }
    public double Ppm { get; set; }
}
