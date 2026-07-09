namespace POApprovalAPI.Models
{
    public class PaymentHistoryModel
    {
        public string ApprovalName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? ApprovalDate { get; set; }
        public string Comment { get; set; } = "";
        public string LoginName { get; set; } = "";
    }
}