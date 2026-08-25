namespace POApprovalAPI.Models;

public class AdvancePaymentRequestModel
{
    public string PaymentNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string PaymentTypeNo { get; set; } = "";
    public decimal PaymentAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Remarks { get; set; } = "";
    public string PaymentRef { get; set; } = "";
    public string BankCashPayment { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal ExchangeRate { get; set; }
    public string PaymentReqNo { get; set; } = "";
    public string LedgerFrom { get; set; } = "";
    public string LedgerTo { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public int ApprovalStatus { get; set; }
}

public class AdvancePaymentDetailsModel : AdvancePaymentRequestModel
{
    public string ChequeBankName { get; set; } = "";
    public string BankBranch { get; set; } = "";
    public DateTime? InstrumentDate { get; set; }
    public string AmountWords { get; set; } = "";
    public int? RecordLogId { get; set; }
    public int? CompanyId { get; set; }
    public int? LedgerFromId { get; set; }
    public int? LedgerToId { get; set; }
}

public class AdvancePaymentHistoryModel
{
    public string ApprovalName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? ApprovalDate { get; set; }
    public string Comment { get; set; } = "";
    public string LoginName { get; set; } = "";
}
