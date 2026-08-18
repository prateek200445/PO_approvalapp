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

    /// <summary>When false, confirm endpoint rejects writes (preview only).</summary>
    public bool AllowConfirmSave { get; set; }

    /// <summary>When true, confirm may replace existing prod_fibcallocationMaster rows for the order.</summary>
    public bool AllowReplaceExistingPlan { get; set; }

    /// <summary>When false, quotation hold endpoints are disabled.</summary>
    public bool QuotationHoldEnabled { get; set; } = true;

    public int QuotationHoldDays { get; set; } = 7;

    /// <summary>When true, sends email on quotation hold create / confirm / cancel / expiry reminder.</summary>
    public bool QuotationHoldEmailEnabled { get; set; } = true;

    /// <summary>Primary recipients for quotation hold alerts (semicolon or array in appsettings).</summary>
    public string[] QuotationHoldNotifyTo { get; set; } = [];

    public string? QuotationHoldNotifyCc { get; set; }

    /// <summary>Send expiry reminder when hold expires within this many days (default 1).</summary>
    public int QuotationHoldExpiryReminderDays { get; set; } = 1;

    /// <summary>When false, critical order shifting endpoints are disabled.</summary>
    public bool CriticalShiftEnabled { get; set; } = true;

    /// <summary>Max days to search forward when relocating a displaced order.</summary>
    public int CriticalShiftMaxForwardDays { get; set; } = 60;
}
