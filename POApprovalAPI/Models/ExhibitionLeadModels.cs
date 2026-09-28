namespace POApprovalAPI.Models;

public class ExhibitionLead
{
    public int Id { get; set; }
    public string FormType { get; set; } = "";
    public string? CompanyName { get; set; }
    public string PersonName { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? ProductInquiry { get; set; }
    public decimal? Quantity { get; set; }
    public string? ExhibitionName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExhibitionLeadCreateRequest
{
    public string FormType { get; set; } = "";
    public string? CompanyName { get; set; }
    public string PersonName { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? ProductInquiry { get; set; }
    public decimal? Quantity { get; set; }
    public string? ExhibitionName { get; set; }
}
