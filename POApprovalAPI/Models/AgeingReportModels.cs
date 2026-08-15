namespace POApprovalAPI.Models;

public enum AgeingReportMode
{
    /// <summary>Party list with month buckets — sp_Representative_Outstanding_Pivot.</summary>
    GroupPivot,
    /// <summary>Single party bill-wise overdue — sp_Overdue_Ledger.</summary>
    PartyOverdue,
    /// <summary>Single party Dr/Cr totals by currency — sp_Overdue_Ledger_SUMMARY.</summary>
    PartySummary,
}

public class AgeingReportPlan
{
    public AgeingReportMode Mode { get; set; }
    public string CompanyName { get; set; } = "";
    public DateTime ToDate { get; set; }
    /// <summary>Top group: Sundry Debtors or Trade Creditors.</summary>
    public string G3 { get; set; } = "Sundry Debtors";
    /// <summary>Optional sub-group e.g. Debtors-Overseas, Creditors-RM.</summary>
    public string? G4 { get; set; }
    public string? LedgerName { get; set; }
    public string Currency { get; set; } = "Rs.";
    /// <summary>Monthly buckets (3) for pivot SP — never daily (1).</summary>
    public int PeriodMonths { get; set; } = 3;
    public int MaxRows { get; set; } = 50;
}

public class AgeingReportResult
{
    public string SqlDescription { get; set; } = "";
    public string Warning { get; set; } = "";
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int? TotalCount { get; set; }
}
