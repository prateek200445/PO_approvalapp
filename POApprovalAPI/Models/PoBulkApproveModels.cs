namespace POApprovalAPI.Models;

public class PoBulkApproveRequest
{
    public List<int> TransIds { get; set; } = new();
    public string? Remarks { get; set; }
    /// <summary>Must match ApprovalName on each pending row.</summary>
    public string UserName { get; set; } = "";
}

public class PoApproveItemResult
{
    public int TransId { get; set; }
    public string? PoNo { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
}

public class PoBulkApproveResponse
{
    public int Total { get; set; }
    public List<PoApproveItemResult> Succeeded { get; set; } = new();
    public List<PoApproveItemResult> Failed { get; set; } = new();
}
