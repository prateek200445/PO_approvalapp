namespace POApprovalAPI.Models;

public class LedgerStatementPlan
{
    public string CompanyName { get; set; } = "";
    public int CompanyId { get; set; }
    public string LedgerName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Currency { get; set; }
    public int MaxRows { get; set; } = 50;
}

public class LedgerStatementChatResult
{
    public string SqlDescription { get; set; } = "";
    public string Warning { get; set; } = "";
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int? TotalCount { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
}
