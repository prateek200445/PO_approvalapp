using System.Globalization;
using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static readonly Regex DimensionHintRegex = new(
        @"country|nation|buyer|customer|client|party|vendor|firm|supplier|ledger|item|product|material|department|particulars|dept|group|under|subgroup|representative|salesman|state|city|region|buyername|partyname|firmname|ledgername|itemname|productgroup|countryname",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MeasureHintRegex = new(
        @"amount|billamount|total|qty|quantity|stkinhand|stock|debit|credit|balance|pending|outstanding|opening|closing|production|wastage|value|netamount|net|billamt|sum",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SkipColumnRegex = new(
        @"^(section|error|note|password|rownum|rn)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PercentColumnRegex = new(
        @"pct|percent|ratio|rate|%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Context-aware summary for any multi-row result (top-N dimensions + totals).</summary>
    private static string? BuildSmartMultiRowSummary(List<Dictionary<string, object?>> rows)
    {
        if (rows.Count <= 1) return null;

        var dimensionCol = PickBestColumn(rows, ScoreDimensionColumn);
        var measureCol = PickBestColumn(rows, ScoreMeasureColumn);

        if (dimensionCol != null && measureCol != null)
        {
            var grouped = rows
                .GroupBy(r => GetRowString(r, dimensionCol) ?? "Unknown", StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Label = g.Key,
                    Total = g.Sum(r => GetRowDecimal(r, measureCol) ?? 0m),
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var grandTotal = grouped.Sum(x => x.Total);
            var top = grouped.Take(5).ToList();
            var dimLabel = HumanizeSummaryColumn(dimensionCol);
            var measureLabel = HumanizeSummaryColumn(measureCol);

            var topPart = string.Join(" · ", top.Select(x => $"**{x.Label}** {FormatSummaryMeasure(measureCol, x.Total)}"));
            var more = grouped.Count > 5 ? $" (+{grouped.Count - 5} more {dimLabel.ToLowerInvariant()})" : "";
            var totalPart = grandTotal != 0m
                ? $"**{FormatSummaryMeasure(measureCol, grandTotal)}** total {measureLabel.ToLowerInvariant()}"
                : $"**{rows.Count:N0}** rows";

            return $"**{rows.Count:N0}** rows — {totalPart}. Top {Math.Min(5, grouped.Count)} {dimLabel.ToLowerInvariant()}: {topPart}{more}.";
        }

        if (dimensionCol != null)
        {
            var distinct = rows
                .Select(r => GetRowString(r, dimensionCol))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinct.Count > 1)
            {
                var dimLabel = HumanizeSummaryColumn(dimensionCol).ToLowerInvariant();
                var top = distinct.Take(5).ToList();
                var more = distinct.Count > 5 ? $" (+{distinct.Count - 5} more)" : "";
                var topPart = string.Join(" · ", top.Select(x => $"**{x}**"));
                return $"**{distinct.Count}** {dimLabel}{(distinct.Count == 1 ? "" : "s")}: {topPart}{more}.";
            }
        }

        var measures = PickMeasureColumns(rows);
        if (measures.Count > 0)
        {
            var parts = measures.Select(col =>
            {
                var sum = rows.Sum(r => GetRowDecimal(r, col) ?? 0m);
                return $"**{FormatSummaryMeasure(col, sum)}** {HumanizeSummaryColumn(col).ToLowerInvariant()}";
            });
            return $"**{rows.Count:N0}** rows — {string.Join(" · ", parts)}.";
        }

        return $"**{rows.Count:N0}** matching records — see table below for details.";
    }

    private static string? PickBestColumn(
        List<Dictionary<string, object?>> rows,
        Func<string, List<Dictionary<string, object?>>, int> scorer)
    {
        if (rows.Count == 0) return null;
        string? bestCol = null;
        var bestScore = int.MinValue;
        foreach (var col in rows[0].Keys.Where(k => !SkipColumnRegex.IsMatch(k)))
        {
            var score = scorer(col, rows);
            if (score > bestScore)
            {
                bestScore = score;
                bestCol = col;
            }
        }
        return bestScore > 0 ? bestCol : null;
    }

    private static List<string> PickMeasureColumns(List<Dictionary<string, object?>> rows)
    {
        return rows[0].Keys
            .Where(k => !SkipColumnRegex.IsMatch(k))
            .Select(col => (col, score: ScoreMeasureColumn(col, rows)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(3)
            .Select(x => x.col)
            .ToList();
    }

    private static int ScoreDimensionColumn(string col, List<Dictionary<string, object?>> rows)
    {
        if (SkipColumnRegex.IsMatch(col)) return -100;
        var score = 0;
        if (DimensionHintRegex.IsMatch(col)) score += 12;
        if (col.Contains("name", StringComparison.OrdinalIgnoreCase)) score += 4;
        if (col.Contains("company", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (Regex.IsMatch(col, @"id$|srno|sysdate|date|time|email|phone|gst|pan|utr|invno|pono|mrno|voucherno", RegexOptions.IgnoreCase))
            score -= 4;

        var values = rows.Select(r => GetRowString(r, col)).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (values.Count == 0) return -100;
        var distinct = values.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct <= 1) return -50;
        if (distinct == rows.Count && rows.Count > 8) score -= 8;
        if (distinct >= 2) score += 6;
        if (distinct is >= 2 and <= 25) score += 4;
        return score;
    }

    private static int ScoreMeasureColumn(string col, List<Dictionary<string, object?>> rows)
    {
        if (SkipColumnRegex.IsMatch(col)) return -100;
        if (PercentColumnRegex.IsMatch(col)) return -20;

        var score = MeasureHintRegex.IsMatch(col) ? 12 : 0;
        if (col.Equals("DebitBalance", StringComparison.OrdinalIgnoreCase)
            || col.Equals("CreditBalance", StringComparison.OrdinalIgnoreCase)
            || col.Equals("EffectiveBalance", StringComparison.OrdinalIgnoreCase))
            score += 25;
        if (col.Equals("PendingBalance", StringComparison.OrdinalIgnoreCase)
            && rows.All(r => Math.Abs(GetRowDecimal(r, col) ?? 0m) < 0.01m)
            && rows.Any(r => r.ContainsKey("DebitBalance") || r.ContainsKey("CreditBalance")))
            score -= 30;
        if (Regex.IsMatch(col, @"count|cnt", RegexOptions.IgnoreCase) && !col.Contains("country", StringComparison.OrdinalIgnoreCase))
            score += 6;

        var nums = rows.Select(r => GetRowDecimal(r, col)).Where(v => v.HasValue).ToList();
        if (nums.Count < Math.Ceiling(rows.Count * 0.4)) return -50;
        if (nums.Sum(v => v!.Value) == 0m) score -= 10;
        else score += 5;
        return score;
    }

    private static string HumanizeSummaryColumn(string col) =>
        Regex.Replace(col, @"([a-z])([A-Z])", "$1 $2").Replace("_", " ").Trim();

    private static string FormatSummaryMeasure(string col, decimal value)
    {
        if (Regex.IsMatch(col, @"amount|bill|debit|credit|balance|pending|outstanding|opening|closing|value|net", RegexOptions.IgnoreCase))
            return $"₹{value:N2}";
        return FormatQty(value);
    }
}
