namespace POApprovalAPI.Models
{
    public class PaymentDetailsModel : PaymentRequestModel
    {
        public string LC { get; set; } = "";
        public string UTRNo { get; set; } = "";
        public string PaymentBankName { get; set; } = "";
        public string PaymentBankAccNo { get; set; } = "";
        public string SpeReq { get; set; } = "";
        public decimal GroupBalance { get; set; }

public decimal LedgerBalance { get; set; }
    }
}