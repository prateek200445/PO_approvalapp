namespace POApprovalAPI.Models
{
    /// <summary>
    /// Data model for combining approval details with email and authority information.
    /// Used to fetch all required data in a single query instead of multiple N+1 queries.
    /// Supports PO, WorkOrder, and Payment approval processes.
    /// </summary>
    public class ApprovalData
    {
        // For PO/WorkOrder approvals
        public string PoNo { get; set; }
        
        // For Payment approvals  
        public string PaymentNo { get; set; }
        
        // Common fields for all approval types
        public string ApprovalName { get; set; }
        public string Email { get; set; }
        public int Authority { get; set; }
    }
}
