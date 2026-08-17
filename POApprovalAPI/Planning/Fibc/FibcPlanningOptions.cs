namespace POApprovalAPI.Planning.Fibc;

/// <summary>
/// Configurable planning defaults. Extend via appsettings "FibcPlanning" section later.
/// </summary>
public sealed class FibcPlanningOptions
{
    public string DefaultCompanyName { get; set; } = "Plastene India Limited (Unit -II)";

    /// <summary>Days before dispatch date by which production must complete (requirements doc: 7).</summary>
    public int DispatchBufferDays { get; set; } = 7;

    /// <summary>Preferred shift order when allotting (requirements doc: C, B, A).</summary>
    public string[] ShiftPreference { get; set; } = ["C", "B", "A"];

    /// <summary>Shifts currently present in CapacityPlanning (DB today: A, B only).</summary>
    public string[] ActiveShifts { get; set; } = ["A", "B"];

    public bool AllowShiftCWhenCapacityExists { get; set; } = true;
}
