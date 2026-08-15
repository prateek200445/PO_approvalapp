using System.Globalization;
using System.Text;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool ShouldUseDeterministicAnswer(
        bool skipSqlExecution,
        string? warning,
        string sql)
    {
        if (skipSqlExecution) return true;
        if (string.IsNullOrWhiteSpace(warning)) return false;

        return warning.Contains("Governed", StringComparison.OrdinalIgnoreCase)
               || warning.Contains("(governed", StringComparison.OrdinalIgnoreCase)
               || warning.StartsWith("ERP ", StringComparison.OrdinalIgnoreCase)
               || warning.Contains("Rewrote", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDeterministicAnswer(
        string question,
        List<Dictionary<string, object?>> rows,
        string? warning,
        int? totalCount,
        bool truncated)
    {
        var sb = new StringBuilder();

        if (rows.Count == 0)
        {
            sb.Append("No matching records were found for this question.");
            if (!string.IsNullOrWhiteSpace(warning))
                sb.Append(' ').Append(warning.Trim());
            return sb.ToString().Trim();
        }

        var countText = totalCount.HasValue
            ? truncated
                ? $"Found {totalCount.Value} matching row(s); showing {rows.Count}."
                : $"Found {rows.Count} matching row(s)."
            : truncated
                ? $"Showing {rows.Count} row(s) (result may be capped)."
                : $"Found {rows.Count} row(s).";

        sb.Append(countText);

        if (!string.IsNullOrWhiteSpace(warning))
        {
            var w = warning.Trim();
            if (!w.StartsWith("Showing ", StringComparison.OrdinalIgnoreCase))
                sb.Append(' ').Append(w);
        }

        sb.AppendLine();
        sb.AppendLine("Sample rows:");

        var previewCount = Math.Min(rows.Count, 8);
        for (var i = 0; i < previewCount; i++)
        {
            sb.Append("- ");
            sb.AppendLine(FormatRowPreview(rows[i]));
        }

        if (rows.Count > previewCount)
            sb.Append($"... and {rows.Count - previewCount} more row(s) in the result grid.");

        return sb.ToString().Trim();
    }

    private static string FormatRowPreview(Dictionary<string, object?> row)
    {
        var keys = row.Keys
            .Where(k => row[k] is not null && !string.IsNullOrWhiteSpace(row[k]?.ToString()))
            .Take(6)
            .ToList();

        if (keys.Count == 0) return "(empty row)";

        return string.Join(", ", keys.Select(k =>
        {
            var val = row[k];
            var text = val switch
            {
                null => "",
                IFormattable f when val is float or double or decimal =>
                    f.ToString("N2", CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                _ => val.ToString() ?? ""
            };
            return $"{k}={text}";
        }));
    }
}
