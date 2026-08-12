using System.Text.Json;
using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

/// <summary>
/// Loads schema-catalog.json and rewrites / flags hallucinated column names
/// against the curated allowlist for each object referenced in SQL.
/// </summary>
public sealed class SchemaCatalogService
{
    private readonly IReadOnlyDictionary<string, HashSet<string>> _columnsByObject;
    private readonly ILogger<SchemaCatalogService> _logger;

    // Common LLM inventions → preferred real names (applied when target exists on the object)
    private static readonly Dictionary<string, string[]> KnownSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["POCode"] = ["PoNo", "PurchaseCode", "PONo"],
        ["PONumber"] = ["PoNo", "PurchaseCode"],
        ["PurchaseOrderNo"] = ["PoNo", "PurchaseCode"],
        ["PurchaseDate"] = ["PODate", "deliverydate", "BillDate", "Sysdate", "sysDate"],
        ["MRDate"] = ["MRNDate", "BillDate", "GateInwardDate", "SysDate"],
        ["ItemDesc"] = ["itemdesc", "ItemName", "ItemDesc"],
        ["ItemDescription"] = ["itemdesc", "ItemDesc", "ItemName"],
        ["GSTNo"] = ["NewGSTNo", "GSTNo"],
        ["PAN"] = ["PANNo", "PermanentAccountNo"],
        ["PANNumber"] = ["PANNo", "PermanentAccountNo"],
        ["VendorName"] = ["FirmName", "PartyName", "VendorName"],
        ["Party"] = ["PartyName", "Partyname"],
        ["CompName"] = ["CompanyName", "companyname", "CompName"],
        ["Company"] = ["CompanyName", "companyname", "CompName"],
        ["TotalAmt"] = ["TotalAmount", "Amount", "BillAMount", "PaymentAmount"],
        ["Total"] = ["TotalAmount", "Total", "Amount"],
        ["Amount"] = ["TotalAmount", "Amount", "PaymentAmount", "BillAMount"],
    };

    private static readonly HashSet<string> SqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS",
        "ON", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL", "AS",
        "GROUP", "BY", "ORDER", "HAVING", "TOP", "DISTINCT", "UNION", "ALL", "CASE", "WHEN",
        "THEN", "ELSE", "END", "COUNT", "SUM", "AVG", "MIN", "MAX", "CAST", "CONVERT",
        "ISNULL", "COALESCE", "OVER", "PARTITION", "DESC", "ASC", "WITH", "NOLOCK",
        "OUTER", "APPLY", "VALUES", "DECLARE", "SET"
    };

    public SchemaCatalogService(IWebHostEnvironment env, ILogger<SchemaCatalogService> logger)
    {
        _logger = logger;
        var path = Path.Combine(env.ContentRootPath, "Chatbot", "schema-catalog.json");
        _columnsByObject = LoadCatalog(path);
        _logger.LogInformation(
            "Schema catalog loaded: {Objects} objects with column allowlists from {Path}",
            _columnsByObject.Count, path);
    }

    public IReadOnlyDictionary<string, HashSet<string>> ColumnsByObject => _columnsByObject;

    /// <summary>
    /// Rewrite hallucinated columns using catalog allowlists + synonym/fuzzy match.
    /// Returns fixed SQL and human-readable fix notes (for logs / repair prompts).
    /// </summary>
    public string FixHallucinatedColumns(string sql, out IReadOnlyList<string> fixes)
    {
        var fixList = new List<string>();
        if (string.IsNullOrWhiteSpace(sql) || _columnsByObject.Count == 0)
        {
            fixes = fixList;
            return sql;
        }

        var aliases = ExtractTableAliases(sql);
        if (aliases.Count == 0)
        {
            fixes = fixList;
            return sql;
        }

        // Qualified refs: alias.column or ObjectName.column
        var result = Regex.Replace(
            sql,
            @"\b(?<qual>[A-Za-z_][\w]*)\s*\.\s*(?<col>\[[^\]]+\]|[A-Za-z_][\w]*)\b",
            m =>
            {
                var qual = m.Groups["qual"].Value;
                var colRaw = m.Groups["col"].Value;
                var col = UnwrapBrackets(colRaw);

                if (!_columnsByObject.ContainsKey(qual)
                    && !aliases.ContainsKey(qual))
                    return m.Value; // schema prefix like dbo. — leave for next pass / ignore

                var objectName = aliases.TryGetValue(qual, out var obj) ? obj : qual;
                if (!_columnsByObject.TryGetValue(objectName, out var allowed))
                    return m.Value;

                if (allowed.Contains(col))
                    return m.Value; // exact (case-insensitive set)

                var resolved = ResolveColumn(col, allowed);
                if (resolved is null || resolved.Equals(col, StringComparison.OrdinalIgnoreCase))
                {
                    fixList.Add(
                        $"UNKNOWN {objectName}.{col} — allowed: {string.Join(", ", allowed.OrderBy(c => c).Take(24))}");
                    return m.Value;
                }

                fixList.Add($"{objectName}.{col} → {resolved}");
                var newCol = colRaw.StartsWith('[') ? $"[{resolved}]" : resolved;
                return $"{qual}.{newCol}";
            },
            RegexOptions.IgnoreCase);

        // Unqualified identifiers (bare CompName / TotalAmt) — previous logic only fixed KnownSynonyms
        // when a synonym target existed on the table; ApproveWorkOrder has neither CompName nor CompanyName.
        result = FixBareColumnIdentifiers(result, aliases, fixList);

        fixes = fixList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (fixes.Count > 0)
            _logger.LogInformation("Schema column fixes: {Fixes}", string.Join("; ", fixes));

        return result;
    }

    private string FixBareColumnIdentifiers(
        string sql,
        Dictionary<string, string> aliases,
        List<string> fixList)
    {
        var objects = aliases.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(o => _columnsByObject.ContainsKey(o))
            .ToList();
        if (objects.Count == 0) return sql;

        var scrubbed = Regex.Replace(sql, @"'([^']|'')*'", "''");

        var tokens = Regex.Matches(scrubbed, @"\b([A-Za-z_][\w]*)\b")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t =>
                !SqlKeywords.Contains(t)
                && !aliases.ContainsKey(t)
                && !_columnsByObject.ContainsKey(t)
                && !t.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                && !t.Equals("loginentry", StringComparison.OrdinalIgnoreCase)
                // Output aliases: "AS CompanyName" — not a real column ref
                && !Regex.IsMatch(scrubbed, $@"\bAS\s+{Regex.Escape(t)}\b", RegexOptions.IgnoreCase))
            .ToList();

        var result = sql;
        foreach (var token in tokens)
        {
            if (objects.Any(o => _columnsByObject[o].Contains(token)))
                continue;

            string? resolved = null;
            string? viaObject = null;
            var uniqueResolutions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in objects)
            {
                var r = ResolveColumn(token, _columnsByObject[o]);
                if (r is null) continue;
                uniqueResolutions.Add(r);
                resolved ??= r;
                viaObject ??= o;
            }

            if (resolved is not null && uniqueResolutions.Count == 1)
            {
                var before = result;
                result = Regex.Replace(
                    result,
                    $@"(?<![.\w]){Regex.Escape(token)}(?![.\w])",
                    resolved,
                    RegexOptions.IgnoreCase);
                if (before != result)
                    fixList.Add($"bare {token} → {resolved} (via {viaObject})");
                continue;
            }

            if (objects.Count == 1)
            {
                var o = objects[0];
                var allowed = _columnsByObject[o];
                var joinHint =
                    o.Equals("ApproveWorkOrder", StringComparison.OrdinalIgnoreCase)
                    || o.Equals("ApprovePO", StringComparison.OrdinalIgnoreCase)
                    || o.Equals("ApprovePOHOD", StringComparison.OrdinalIgnoreCase)
                        ? " CompanyName/TotalAmount are on PurchasePayment — JOIN PoNo = PurchasePayment.PurchaseCode."
                        : "";
                fixList.Add(
                    $"UNKNOWN {o}.{token} — allowed: {string.Join(", ", allowed.OrderBy(c => c).Take(24))}.{joinHint}");
            }
            else
            {
                fixList.Add(
                    $"UNKNOWN column '{token}' — not on any of: {string.Join(", ", objects)}. "
                    + "Join PurchasePayment for CompanyName/TotalAmount when querying Approve* tables.");
            }
        }

        return result;
    }

    public string? FormatUnknownColumnsForRepair(IReadOnlyList<string> fixes)
    {
        var unknowns = fixes.Where(f => f.StartsWith("UNKNOWN ", StringComparison.OrdinalIgnoreCase)).ToList();
        if (unknowns.Count == 0) return null;
        return "Column validation failed:\n" + string.Join("\n", unknowns)
               + "\nRewrite SQL using ONLY the allowed columns listed above.";
    }

    private static string? ResolveColumn(string bad, HashSet<string> allowed)
    {
        if (KnownSynonyms.TryGetValue(bad, out var candidates))
        {
            foreach (var c in candidates)
            {
                if (allowed.Contains(c)) return GetCanonical(allowed, c);
            }
        }

        // Normalized equality: Purchase_Code ≈ PurchaseCode
        var normBad = Normalize(bad);
        foreach (var a in allowed)
        {
            if (Normalize(a) == normBad) return a;
        }

        // Fuzzy: prefer shortest Levenshtein among close matches
        string? best = null;
        var bestDist = int.MaxValue;
        foreach (var a in allowed)
        {
            var d = Levenshtein(normBad, Normalize(a));
            var maxAllowed = Math.Max(2, Math.Max(1, normBad.Length / 3));
            if (d <= maxAllowed && d < bestDist)
            {
                bestDist = d;
                best = a;
            }
        }

        // Require reasonably close (POCode→PoNo is dist 2 on normalized "pocode"/"pono")
        if (best is not null && bestDist <= 3) return best;

        return null;
    }

    private static string GetCanonical(HashSet<string> allowed, string name)
    {
        foreach (var a in allowed)
            if (a.Equals(name, StringComparison.OrdinalIgnoreCase)) return a;
        return name;
    }

    private static string Normalize(string s) =>
        Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]", "");

    private static string UnwrapBrackets(string s) =>
        s.Length >= 2 && s[0] == '[' && s[^1] == ']' ? s[1..^1] : s;

    private Dictionary<string, string> ExtractTableAliases(string sql)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // FROM/JOIN dbo.ApprovePO ap  |  FROM ApprovePO AS ap  |  FROM loginentry.dbo.LoginRights
        var matches = Regex.Matches(
            sql,
            @"\b(?:FROM|JOIN)\s+(?:(?:\w+)\.)*(?:dbo\.)?(?<obj>(?:loginentry\.\.?dbo\.)?[\w]+)\s*(?:AS\s+)?(?<alias>\w+)?",
            RegexOptions.IgnoreCase);

        foreach (Match m in matches)
        {
            var objRaw = m.Groups["obj"].Value;
            var obj = CanonicalObjectName(objRaw);
            if (string.IsNullOrEmpty(obj) || SqlKeywords.Contains(obj)) continue;
            if (!_columnsByObject.ContainsKey(obj)
                && !_columnsByObject.Keys.Any(k => k.Equals(obj, StringComparison.OrdinalIgnoreCase)))
            {
                // Try case-insensitive lookup
                var found = _columnsByObject.Keys.FirstOrDefault(k =>
                    k.Equals(obj, StringComparison.OrdinalIgnoreCase));
                if (found is null) continue;
                obj = found;
            }
            else
            {
                obj = _columnsByObject.Keys.FirstOrDefault(k =>
                          k.Equals(obj, StringComparison.OrdinalIgnoreCase))
                      ?? obj;
            }

            if (!_columnsByObject.ContainsKey(obj)) continue;

            var alias = m.Groups["alias"].Success ? m.Groups["alias"].Value : null;
            if (!string.IsNullOrEmpty(alias)
                && !SqlKeywords.Contains(alias)
                && !alias.Equals("ON", StringComparison.OrdinalIgnoreCase))
            {
                map[alias] = obj;
            }

            map[obj] = obj;
        }

        return map;
    }

    private static string CanonicalObjectName(string objRaw)
    {
        var o = objRaw.Trim();
        if (o.Contains("LoginRights", StringComparison.OrdinalIgnoreCase))
            return "LoginRights";
        var parts = o.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? o : parts[^1];
    }

    private IReadOnlyDictionary<string, HashSet<string>> LoadCatalog(string path)
    {
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            _logger.LogWarning("schema-catalog.json missing at {Path}", path);
            return dict;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("objects", out var objects))
            return dict;

        foreach (var obj in objects.EnumerateArray())
        {
            var name = obj.TryGetProperty("objectName", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (obj.TryGetProperty("columns", out var columns))
            {
                foreach (var c in columns.EnumerateArray())
                {
                    if (c.TryGetProperty("name", out var cn))
                    {
                        var colName = cn.GetString();
                        if (!string.IsNullOrWhiteSpace(colName)) cols.Add(colName);
                    }
                }
            }

            if (obj.TryGetProperty("importantOtherColumns", out var other))
            {
                foreach (var c in other.EnumerateArray())
                {
                    var colName = c.GetString();
                    if (!string.IsNullOrWhiteSpace(colName)
                        && !colName.Contains(' ', StringComparison.Ordinal)
                        && !colName.Contains('/', StringComparison.Ordinal))
                        cols.Add(colName);
                }
            }

            if (obj.TryGetProperty("statusColumn", out var sc))
            {
                var statusCol = sc.GetString();
                if (!string.IsNullOrWhiteSpace(statusCol)) cols.Add(statusCol);
            }

            if (cols.Count > 0)
            {
                dict[name] = cols;
                // Also register short name for LoginRights
                if (name.Contains("LoginRights", StringComparison.OrdinalIgnoreCase))
                    dict["LoginRights"] = cols;
            }
        }

        return dict;
    }

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}
