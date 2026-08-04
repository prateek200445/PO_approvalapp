using HtmlAgilityPack;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class HtmlParserService
{
    public DailyReportModel Parse(DailyReportEntity entity)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(entity.HtmlContent);

        var model = new DailyReportModel
        {
            EmployeeName = entity.EmployeeName,
            SubmittedOn = entity.SubmittedOn,
            SubmittedForDate = entity.SubmittedForDate,
            HtmlContent = entity.HtmlContent
        };

        // Employee Name & Department
        var header = doc.DocumentNode.SelectSingleNode("//tr[1]/th");

        if (header != null)
        {
            var parts = header.InnerText.Split('-', 2);

            if (parts.Length == 2)
            {
                model.EmployeeName = parts[0].Trim();
                model.Department = parts[1].Trim();
            }
        }

        // First Half
        var firstHalfNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'First Half')]/following-sibling::td");

        if (firstHalfNode != null)
            model.FirstHalf = GetFormattedText(firstHalfNode);

        // Second Half
        var secondHalfNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Second Half')]/following-sibling::td");

        if (secondHalfNode != null)
            model.SecondHalf = GetFormattedText(secondHalfNode);

        // Tomorrow Tasks
        var allRows = doc.DocumentNode.SelectNodes("//tr");

        if (allRows != null)
        {
            bool taskSection = false;

            foreach (var row in allRows)
            {
                var cols = row.SelectNodes("td|th");

                if (cols == null)
                    continue;

                // Start reading after "To do list for tomorrow"
                if (cols.Any(c => c.InnerText.Contains("To do list for tomorrow")))
                {
                    taskSection = true;
                    continue;
                }

                if (!taskSection)
                    continue;

                var tds = row.SelectNodes("td");

                if (tds != null && tds.Count >= 2)
                {
                    var task = HtmlEntity.DeEntitize(tds[1].InnerText).Trim();

                    if (!string.IsNullOrWhiteSpace(task))
                        model.TomorrowTasks.Add(task);
                }
            }
        }

        return model;
    }

    private static string GetFormattedText(HtmlNode node)
    {
        var html = node.InnerHtml;

        html = html.Replace("<br>", "\n")
                   .Replace("<br/>", "\n")
                   .Replace("<br />", "\n");

        var temp = new HtmlDocument();
        temp.LoadHtml(html);

        return HtmlEntity.DeEntitize(temp.DocumentNode.InnerText).Trim();
    }
}