namespace POApprovalAPI.Models;

public class IndentApprovalRequest
{
    public string Username { get; set; } = "";
    public List<string> IndentSubCodes { get; set; } = new();
}