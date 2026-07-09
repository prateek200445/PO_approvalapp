namespace POApprovalAPI.Models
{
    public class PaymentRequestModel
    {
        public string PaymentNo { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string VendorName { get; set; } = "";
        public string BillNo { get; set; } = "";
        public string MRNNo { get; set; } = "";

        public DateTime? BillDate { get; set; }
        public DateTime? MRNDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        public decimal BillAmount { get; set; }
        public decimal PaymentAmount { get; set; }

        public string PaymentTerms { get; set; } = "";
        public string PriorityLevel { get; set; } = "";
        public string Currency { get; set; } = "";
        public decimal CurrencyRate { get; set; }

        public decimal TDS { get; set; }
        public decimal DebitNoteAmnt { get; set; }

        public decimal Outstanding { get; set; }
        public decimal LedgerOSTAmt { get; set; }

        public string Remarks { get; set; } = "";
        public string RequestedBy { get; set; } = "";
    }
}