namespace POApprovalAPI.Models;

public class PaymentBulkApproveRequest
{
    public List<string> PaymentNos { get; set; } = new();
    public string? Comment { get; set; }
    public string UserName { get; set; } = "";
}

public class PaymentApproveItemResult
{
    public string? PaymentNo { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
}

public class PaymentBulkApproveResponse
{
    public int Total { get; set; }
    public List<PaymentApproveItemResult> Succeeded { get; set; } = new();
    public List<PaymentApproveItemResult> Failed { get; set; } = new();
}
