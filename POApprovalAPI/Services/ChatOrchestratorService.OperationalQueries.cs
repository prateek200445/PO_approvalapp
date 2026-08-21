using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    /// <summary>
    /// Department wastage qty and % vs production (vw_FactoryProduction / vw_daily_tape_prod_New).
    /// </summary>
    private static bool TryBuildDepartmentWastageSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeDepartmentWastageQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        if (TryResolveTapePlantWastageDept(message, out var tapeWastageCol, out var tapeProdCol, out var tapeDeptLabel))
        {
            var companyLit = EscapeSqlLiteral(company);
            sql = $"""
                SELECT TOP 1
                    CompanyName,
                    date AS ReportDate,
                    '{EscapeSqlLiteral(tapeDeptLabel)}' AS Department,
                    ISNULL([{tapeProdCol}], Production) AS ProductionQty,
                    ISNULL([{tapeWastageCol}], 0) AS WastageQty,
                    CASE
                        WHEN ISNULL([{tapeProdCol}], Production) > 0
                        THEN CAST(ISNULL([{tapeWastageCol}], 0) * 100.0 / ISNULL([{tapeProdCol}], Production) AS decimal(10, 2))
                        ELSE NULL
                    END AS WastagePct
                FROM vw_daily_tape_prod_New WITH (NOLOCK)
                WHERE CompanyName = '{companyLit}'
                  AND CAST(date AS date) = (
                      SELECT MAX(CAST(date AS date))
                      FROM vw_daily_tape_prod_New WITH (NOLOCK)
                      WHERE CompanyName = '{companyLit}'
                  )
                """;
            warning =
                $"Governed tape-plant dept wastage % for {company} (vw_daily_tape_prod_New; latest business date — calendar today may lag).";
            return true;
        }

        var particularsFilter = TryBuildFactoryParticularsFilter(message, out var deptLabel);
        var companyLit2 = EscapeSqlLiteral(company);
        var particularsClause = string.IsNullOrWhiteSpace(particularsFilter)
            ? ""
            : $" AND {particularsFilter}";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname AS CompanyName,
                Sysdate AS ReportDate,
                Particulars AS Department,
                CASE
                    WHEN Particulars LIKE '%TAPE%' THEN ISNULL(TapeProduction, 0)
                    WHEN Particulars LIKE '%FABRIC%' THEN ISNULL(Fabric, 0)
                    WHEN Particulars LIKE '%SMALL%' THEN ISNULL(SmallBag, 0)
                    WHEN Particulars LIKE '%WEBBING%' THEN ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                    ELSE ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                END AS ProductionQty,
                ISNULL(Wastage, 0) AS WastageQty,
                CASE
                    WHEN (
                        CASE
                            WHEN Particulars LIKE '%TAPE%' THEN ISNULL(TapeProduction, 0)
                            WHEN Particulars LIKE '%FABRIC%' THEN ISNULL(Fabric, 0)
                            WHEN Particulars LIKE '%SMALL%' THEN ISNULL(SmallBag, 0)
                            WHEN Particulars LIKE '%WEBBING%' THEN ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                            ELSE ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                        END
                    ) > 0
                    THEN CAST(
                        ISNULL(Wastage, 0) * 100.0 / (
                            CASE
                                WHEN Particulars LIKE '%TAPE%' THEN ISNULL(TapeProduction, 0)
                                WHEN Particulars LIKE '%FABRIC%' THEN ISNULL(Fabric, 0)
                                WHEN Particulars LIKE '%SMALL%' THEN ISNULL(SmallBag, 0)
                                WHEN Particulars LIKE '%WEBBING%' THEN ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                                ELSE ISNULL(TapeProduction, 0) + ISNULL(Fabric, 0) + ISNULL(SmallBag, 0) + ISNULL(Loom, 0)
                            END
                        ) AS decimal(10, 2))
                    ELSE NULL
                END AS WastagePct
            FROM vw_FactoryProduction WITH (NOLOCK)
            WHERE companyname = '{companyLit2}'
              AND CAST(Sysdate AS date) = (
                  SELECT MAX(CAST(Sysdate AS date))
                  FROM vw_FactoryProduction WITH (NOLOCK)
                  WHERE companyname = '{companyLit2}'
              )
            {particularsClause}
            ORDER BY Particulars
            """;
        warning = string.IsNullOrWhiteSpace(deptLabel)
            ? $"Governed factory wastage % by department for {company} (vw_FactoryProduction; latest business date)."
            : $"Governed {deptLabel} wastage % vs production for {company} (vw_FactoryProduction; latest business date).";
        return true;
    }

    private static bool LooksLikeDepartmentWastageQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("wastage") && !m.Contains("wastages")) return false;
        if (m.Contains("godown") || m.Contains("warehouse")) return false;
        return m.Contains("department") || m.Contains(" dept")
               || m.Contains("production") || m.Contains("against")
               || m.Contains("today") || m.Contains("yesterday")
               || m.Contains("%") || m.Contains("percent")
               || m.Contains("tape") || m.Contains("fabric") || m.Contains("webbing")
               || m.Contains("loom") || m.Contains("fibc") || m.Contains("small bag");
    }

    private static string? TryBuildFactoryParticularsFilter(string message, out string? deptLabel)
    {
        deptLabel = null;

        var deptAbbrev = Regex.Match(
            message,
            @"\b(tape|fabric|webbing|small\s*bag|loom|fibc)\s+dept\b",
            RegexOptions.IgnoreCase);
        if (deptAbbrev.Success)
        {
            var token = deptAbbrev.Groups[1].Value.Trim().ToLowerInvariant();
            if (token.Contains("fabric")) { deptLabel = "Fabric"; return "Particulars LIKE '%FABRIC%'"; }
            if (token.Contains("webbing")) { deptLabel = "WEBBING"; return "Particulars LIKE '%WEBBING%'"; }
            if (token.Contains("tape")) { deptLabel = "Tape"; return "Particulars LIKE '%TAPE%'"; }
            if (token.Contains("small")) { deptLabel = "Small Bag"; return "Particulars LIKE '%SMALL%'"; }
        }

        var wastageCtx = ExtractWastageContext(message);
        var m = wastageCtx.ToLowerInvariant();
        var deptFrag = TryExtractDepartmentFragment(wastageCtx);

        if (!string.IsNullOrWhiteSpace(deptFrag))
        {
            var df = deptFrag.ToLowerInvariant();
            if (df.Contains("fabric"))
            {
                deptLabel = "Fabric";
                return "Particulars LIKE '%FABRIC%'";
            }
            if (df.Contains("webbing"))
            {
                deptLabel = "WEBBING";
                return "Particulars LIKE '%WEBBING%'";
            }
            if (df.Contains("tape"))
            {
                deptLabel = "Tape";
                return "Particulars LIKE '%TAPE%'";
            }
            if (df.Contains("small"))
            {
                deptLabel = "Small Bag";
                return "Particulars LIKE '%SMALL%'";
            }
            deptLabel = deptFrag;
            var lit = EscapeSqlLiteral(deptFrag);
            return $"(Particulars LIKE '%{lit}%' OR Particulars LIKE '%{lit.ToUpperInvariant()}%')";
        }

        if (m.Contains("fabric"))
        {
            deptLabel = "Fabric";
            return "Particulars LIKE '%FABRIC%'";
        }
        if (m.Contains("webbing"))
        {
            deptLabel = "WEBBING";
            return "Particulars LIKE '%WEBBING%'";
        }
        if (m.Contains("small bag") || m.Contains("smallbag"))
        {
            deptLabel = "Small Bag";
            return "Particulars LIKE '%SMALL%'";
        }
        if (m.Contains("tape"))
        {
            deptLabel = "Tape";
            return "Particulars LIKE '%TAPE%'";
        }
        return null;
    }

    private static string ExtractWastageContext(string message)
    {
        var lower = message.ToLowerInvariant();
        foreach (var anchor in new[] { "wastage", "wastages" })
        {
            var idx = lower.IndexOf(anchor, StringComparison.Ordinal);
            if (idx >= 0)
                return message[idx..];
        }
        return message;
    }

    private static bool TryResolveTapePlantWastageDept(
        string message,
        out string wastageCol,
        out string productionCol,
        out string deptLabel)
    {
        wastageCol = "Wastage Dept";
        productionCol = "Total Production";
        deptLabel = "";
        var m = message.ToLowerInvariant();
        if (m.Contains("wastage dept") || (m.Contains("tape plant") && m.Contains("wastage")))
        {
            deptLabel = "Wastage Dept";
            return true;
        }
        if (m.Contains("tape plant") && (m.Contains("wastage") || m.Contains("production")))
        {
            deptLabel = "Tape Plant";
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stitcher/sewer headcount from attendance punch (Loginentry.dbo.Attendancemachine + empinfo).
    /// </summary>
    private static bool TryBuildStitcherAttendanceSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeStitcherAttendanceQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var dateClause = BuildAttendanceDateClause(message);
        var companyFilter = string.IsNullOrWhiteSpace(company)
            ? ""
            : $" AND e.CompanyName LIKE '%{EscapeSqlLiteral(company)}%'";

        sql = $"""
            SELECT
                COUNT(DISTINCT am.Empcode) AS StitchersPresent,
                CAST(MAX(am.AttendanceDate) AS date) AS AttendanceDate
            FROM Loginentry.dbo.Attendancemachine am WITH (NOLOCK)
            INNER JOIN Loginentry.dbo.empinfo e WITH (NOLOCK) ON am.Empcode = e.EmpCode
            WHERE am.intime IS NOT NULL
              AND {dateClause}
              AND (
                  e.Designation LIKE '%stitch%'
                  OR e.Designation LIKE '%Stich%'
                  OR e.Designation LIKE '%sewer%'
                  OR e.Deptt LIKE '%stitch%'
                  OR e.Deptt LIKE '%sew%'
              )
            {companyFilter}
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Governed stitcher/sewer present count (Loginentry Attendancemachine + empinfo; punch intime required)."
            : $"Governed stitcher/sewer present count for {company} (Loginentry Attendancemachine + empinfo).";
        return true;
    }

    private static bool LooksLikeStitcherAttendanceQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        var hasRole = m.Contains("sewer") || m.Contains("sewers") || m.Contains("stitcher")
                      || m.Contains("stitchers") || m.Contains("stitching staff")
                      || m.Contains("sewing staff") || m.Contains("sewing");
        var hasPresence = m.Contains("present") || m.Contains("attendance") || m.Contains("how many")
                          || m.Contains("count") || m.Contains("headcount") || m.Contains("number of")
                          || m.Contains("punched") || m.Contains("punch in") || m.Contains("punch-in");
        if (!hasRole || !hasPresence) return false;
        return m.Contains("yesterday") || m.Contains("today") || m.Contains("last day")
               || Regex.IsMatch(m, @"\b\d{1,2}\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)");
    }

    private static string BuildAttendanceDateClause(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("yesterday"))
            return "CAST(am.AttendanceDate AS date) = CAST(DATEADD(day, -1, GETDATE()) AS date)";
        if (m.Contains("today"))
            return "CAST(am.AttendanceDate AS date) = CAST(GETDATE() AS date)";

        var parsed = TryParseSinceDate(message);
        if (parsed.HasValue)
            return $"CAST(am.AttendanceDate AS date) = '{parsed.Value:yyyy-MM-dd}'";

        return "CAST(am.AttendanceDate AS date) = CAST(DATEADD(day, -1, GETDATE()) AS date)";
    }

    private static List<string> TryExtractStockMaterialFragments(string message)
    {
        var list = new List<string>();

        var slashStock = Regex.Match(
            message,
            @"\b((?:[\w]+(?:/[\w]+)+))\s+stock\b",
            RegexOptions.IgnoreCase);
        if (slashStock.Success)
            list.AddRange(SplitMaterialList(slashStock.Groups[1].Value));

        var patterns = new[]
        {
            @"\binventory\s+of\s+(.+?)(?:\s+(?:at|for|in)\s+|\?|$)",
            @"\binventory\s+for\s+(.+?)(?:\s+(?:at|for|in)\s+|\?|$)",
            @"\bstock\s+of\s+(.+?)(?:\s*/\s*|\s+and\s+|\s+or\s+|,)",
        };

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            list.AddRange(SplitMaterialList(m.Groups[1].Value));
            if (list.Count > 0) break;
        }

        if (list.Count == 0 && message.Contains('/', StringComparison.Ordinal))
        {
            var inv = Regex.Match(message, @"\b(?:inventory|stock)\b.+$", RegexOptions.IgnoreCase);
            if (inv.Success)
                list.AddRange(SplitMaterialList(inv.Value));
        }

        return list
            .Where(f => f.Length >= 2 && !IsStockCompanyFragment(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> SplitMaterialList(string raw)
    {
        var list = new List<string>();
        foreach (var part in Regex.Split(raw, @"\s*/\s*|\s+and\s+|\s+or\s+|,"))
        {
            var p = part.Trim().TrimEnd('.', ',', ';', '?', '!', '/');
            p = Regex.Replace(p, @"\b(?:etc\.?|at|for|in|the|inventory|stock)\b", "", RegexOptions.IgnoreCase).Trim();
            if (p.Length >= 2 && !IsStockCompanyFragment(p))
                list.Add(p);
        }
        return list;
    }

    private static bool IsStockCompanyFragment(string fragment)
    {
        if (CompanyAliasMap.Resolve(fragment) is not null) return true;
        if (ResolveOutwardCompanyAlias(fragment) is not null) return true;
        return fragment.Contains("Limited", StringComparison.OrdinalIgnoreCase)
               || fragment.Contains("Ltd", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildMultiMaterialStockFilter(IReadOnlyList<string> fragments)
    {
        var ors = fragments
            .Select(f =>
            {
                var words = Regex.Split(f.Trim(), @"\s+")
                    .Select(w => w.Trim().TrimEnd('.', ',', ';', '?', '!'))
                    .Where(w => w.Length >= 2)
                    .Select(NormalizeStockItemWordForLike)
                    .Where(w => w.Length >= 2)
                    .ToList();
                if (words.Count == 0)
                    return $"ItemName LIKE '%{EscapeSqlLiteral(f.Trim())}%'";
                var ands = string.Join(" AND ", words.Select(w => $"ItemName LIKE '%{EscapeSqlLiteral(w)}%'"));
                return $"({ands})";
            })
            .ToList();
        return ors.Count == 1 ? ors[0] : $"({string.Join(" OR ", ors)})";
    }

    private async Task<(bool Ok, List<Dictionary<string, object?>> Rows, string Sql, string Warning)>
        TryExecuteCompositeOperationalQueryAsync(string message, CancellationToken ct)
    {
        var sections = new List<(string Name, string Sql, string Warning)>();

        if (TryBuildStockInHandSql(message, out var stockSql, out var stockWarn))
            sections.Add(("Inventory", stockSql, stockWarn));
        if (TryBuildStitcherAttendanceSql(message, out var attSql, out var attWarn))
            sections.Add(("Stitcher attendance", attSql, attWarn));
        if (TryBuildDepartmentWastageSql(message, out var wastSql, out var wastWarn))
            sections.Add(("Wastage", wastSql, wastWarn));

        if (sections.Count < 2)
            return (false, [], "", "");

        var allRows = new List<Dictionary<string, object?>>();
        var sqlParts = new List<string>();
        var warnParts = new List<string>();

        foreach (var (name, sectionSql, sectionWarn) in sections)
        {
            sqlParts.Add($"-- {name}\n{sectionSql}");
            warnParts.Add(sectionWarn);
            try
            {
                var sectionRows = await ExecuteReadOnlyAsync(sectionSql, ct);
                if (sectionRows.Count == 0)
                {
                    allRows.Add(new Dictionary<string, object?>
                    {
                        ["Section"] = name,
                        ["Note"] = "No matching records for this part of the question.",
                    });
                    continue;
                }

                foreach (var row in sectionRows)
                {
                    var tagged = new Dictionary<string, object?>(row) { ["Section"] = name };
                    allRows.Add(tagged);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Composite operational section {Section} failed", name);
                allRows.Add(new Dictionary<string, object?>
                {
                    ["Section"] = name,
                    ["Error"] = ex.Message,
                });
            }
        }

        var sql = string.Join("\n\n", sqlParts);
        var warning =
            $"Governed composite answer ({string.Join(" + ", sections.Select(s => s.Name))}). {string.Join(" ", warnParts)}";
        return (true, allRows, sql, warning);
    }
}
