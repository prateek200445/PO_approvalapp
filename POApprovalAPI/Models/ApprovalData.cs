namespace POApprovalAPI.Models
{
    /// <summary>
    /// Data model for combining work order/PO approval details with email and authority information.
    /// Used to fetch all required data in a single query instead of multiple N+1 queries.
    /// </summary>
    public class ApprovalData
    {
        public string PoNo { get; set; }
        public string ApprovalName { get; set; }
        public string Email { get; set; }
        public int Authority { get; set; }
    }
}
