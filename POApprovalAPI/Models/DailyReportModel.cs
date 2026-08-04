namespace POApprovalAPI.Models;

public class DailyReportModel
{
    public string EmployeeName { get; set; } = "";

    public DateTime SubmittedOn { get; set; }

    public string HtmlContent { get; set; } = "";

    public string Department { get; set; } = "";

    public string FirstHalf { get; set; } = "";

    public string SecondHalf { get; set; } = "";

    public List<string> TomorrowTasks { get; set; } = new();

    public string ManagerEmail { get; set; } = "";

    public string ManagerMobile { get; set; } = "";

    public DateTime SubmittedForDate { get; set; }
}