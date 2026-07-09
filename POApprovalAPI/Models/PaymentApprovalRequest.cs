namespace POApprovalAPI.Models
{
    public class PaymentApprovalRequest
    {
        public string PaymentNo { get; set; } = "";

        public string UserName { get; set; } = "";

        public string Comment { get; set; } = "";
    }
}