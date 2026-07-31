using System.Text;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class MessageFormatterService
{
    public string Format(DailyReportModel report)
    {
        var message = new StringBuilder();

        message.AppendLine("📋 *Daily Work Report*");
        message.AppendLine();

        message.AppendLine($"👤 *Employee:* {report.EmployeeName}");
        message.AppendLine($"🏢 *Department:* {report.Department}");
        message.AppendLine($"📅 *Report Date:* {report.SubmittedForDate:dd-MMM-yyyy}");
        message.AppendLine($"⏰ *Submitted:* {report.SubmittedOn:dd-MMM-yyyy hh:mm tt}");

        message.AppendLine();
        message.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine();

        message.AppendLine("🟢 *First Half*");
        message.AppendLine(report.FirstHalf);

        message.AppendLine();
        message.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine();

        message.AppendLine("🔵 *Second Half*");
        message.AppendLine(report.SecondHalf);

        message.AppendLine();
        message.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine();

        message.AppendLine("📌 *Tomorrow's Tasks*");

        if (report.TomorrowTasks.Any())
        {
            foreach (var task in report.TomorrowTasks)
            {
                message.AppendLine($"• {task}");
            }
        }
        else
        {
            message.AppendLine("No tasks.");
        }

        message.AppendLine();
        message.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine();
        message.AppendLine("Generated automatically from ERP");

        return message.ToString();
    }
}