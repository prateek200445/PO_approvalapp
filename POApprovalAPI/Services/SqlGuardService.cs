using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class SqlGuardService
{
    private static readonly string[] Banned =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "XP_", "SP_", "OPENROWSET", "OPENDATASOURCE", "INTO ",
        "GRANT", "REVOKE", "DENY", "BACKUP", "RESTORE", "SHUTDOWN", "DBCC"
    ];

    public string NormalizeAndValidate(string rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql))
            throw new InvalidOperationException("Model returned empty SQL.");

        var sql = ExtractSql(rawSql).Trim().TrimEnd(';').Trim();
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("Could not extract SQL from model response.");

        // Single statement only
        if (sql.Contains(';'))
            throw new InvalidOperationException("Multiple SQL statements are not allowed.");

        var upper = sql.ToUpperInvariant();
        foreach (var banned in Banned)
        {
            if (upper.Contains(banned, StringComparison.Ordinal))
                throw new InvalidOperationException($"SQL blocked: contains forbidden token '{banned.Trim()}'.");
        }

        if (!upper.TrimStart().StartsWith("SELECT", StringComparison.Ordinal)
            && !upper.TrimStart().StartsWith("WITH", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only SELECT / WITH...SELECT queries are allowed.");
        }

        return sql;
    }

    private static string ExtractSql(string text)
    {
        var fence = SqlFenceRegex().Match(text);
        if (fence.Success)
            return fence.Groups[1].Value.Trim();

        // If the whole reply looks like SQL
        var trimmed = text.Trim();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        // Find first SELECT/WITH
        var idxSelect = text.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        var idxWith = text.IndexOf("WITH", StringComparison.OrdinalIgnoreCase);
        var start = -1;
        if (idxSelect >= 0 && idxWith >= 0) start = Math.Min(idxSelect, idxWith);
        else if (idxSelect >= 0) start = idxSelect;
        else if (idxWith >= 0) start = idxWith;

        if (start < 0) return text;
        return text[start..].Trim();
    }

    [GeneratedRegex(@"```(?:sql)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase)]
    private static partial Regex SqlFenceRegex();
}
