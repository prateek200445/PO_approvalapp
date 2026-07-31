namespace POApprovalAPI.Models;

public class DailyReportEntity
{
    public string EmployeeName { get; set; } = string.Empty;

    public DateTime SubmittedOn { get; set; }

    public DateTime SubmittedForDate { get; set; }

    public string HtmlContent { get; set; } = string.Empty;
}