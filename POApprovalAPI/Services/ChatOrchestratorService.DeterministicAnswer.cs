using System.Globalization;
using System.Text.RegularExpressions;

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
        if (rows.Count == 0)
            return "No matching records were found for your question.";

        var headline = BuildDeterministicHeadline(rows, totalCount, truncated);
        var body = TryBuildDeterministicBody(rows, warning);
        return string.IsNullOrWhiteSpace(body) ? headline : $"{headline}\n\n{body}";
    }

    private static string BuildDeterministicHeadline(
        List<Dictionary<string, object?>> rows,
        int? totalCount,
        bool truncated)
    {
        if (totalCount.HasValue)
        {
            if (truncated && totalCount.Value > rows.Count)
                return $"Found {totalCount.Value:N0} matching record(s). Showing the first {rows.Count:N0} below — use Export for the full list.";
            return $"Found {totalCount.Value:N0} matching record(s).";
        }

        if (truncated)
            return $"Showing {rows.Count:N0} record(s) below (results may be capped — use Export for more).";

        return rows.Count == 1
            ? "Found 1 matching record."
            : $"Found {rows.Count:N0} matching records.";
    }

    private static string? TryBuildDeterministicBody(
        List<Dictionary<string, object?>> rows,
        string? warning)
    {
        if (rows.Count >= 1 && rows.Any(r => r.ContainsKey("Section")))
            return BuildCompositeOperationalBody(rows);

        if (!string.IsNullOrWhiteSpace(warning)
            && warning.StartsWith("ERP ledger statement", StringComparison.OrdinalIgnoreCase))
            return BuildLedgerStatementBody(warning, rows);

        if (rows.Count >= 1 && LooksLikeLedgerMasterBalanceRow(rows[0]))
            return BuildLedgerBalanceBody(rows[0]);

        if (rows.Count >= 1 && LooksLikeWastageKpiRow(rows[0]))
            return BuildWastageKpiBody(rows);

        if (rows.Count == 1 && LooksLikeStitcherAttendanceRow(rows[0]))
            return BuildStitcherAttendanceBody(rows[0]);

        if (rows.Count >= 1 && LooksLikeStockInHandRow(rows[0]))
            return BuildStockInHandBody(rows);

        if (rows.Count == 1 && LooksLikeProductLineSalesSummaryRow(rows[0]))
            return BuildProductLineSalesSummaryBody(rows[0], warning);

        if (rows.Count >= 1 && LooksLikeProductLineSalesMonthlyRow(rows[0]))
            return BuildProductLineSalesMonthlyBody(rows, warning);

        if (rows.Count > 1)
            return BuildSmartMultiRowSummary(rows);

        return null;
    }

    private static bool LooksLikeProductLineSalesSummaryRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("PerKg")
               && keys.Contains("SalesAmount")
               && !keys.Contains("SalesMonth");
    }

    private static bool LooksLikeProductLineSalesMonthlyRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("PerKg")
               && keys.Contains("SalesAmount")
               && (keys.Contains("SalesMonth") || keys.Contains("MonthName"));
    }

    private static string BuildProductLineSalesSummaryBody(Dictionary<string, object?> row, string? warning)
    {
        var product = ExtractProductLineLabelFromWarning(warning) ?? "Product line";
        var amount = GetRowDecimal(row, "SalesAmount");
        var qty = GetRowDecimal(row, "QuantityKg");
        var perKg = GetRowDecimal(row, "PerKg");
        var months = GetRowDecimal(row, "MonthsInPeriod");
        var period = ExtractPeriodFromProductLineWarning(warning);

        var parts = new List<string>();
        if (perKg.HasValue)
            parts.Add($"**{product}** weighted rate{period}: **₹{perKg.Value:N2}/kg**");
        parts.Add($"**{FormatInr(amount)}** on **{FormatQty(qty)} kg**");
        if (months.HasValue && months.Value > 0)
            parts.Add($"across **{(int)months.Value}** month(s)");
        return string.Join(" — ", parts) + ".";
    }

    private static string BuildProductLineSalesMonthlyBody(
        List<Dictionary<string, object?>> rows,
        string? warning)
    {
        var product = ExtractProductLineLabelFromWarning(warning) ?? "Product line";
        var period = ExtractPeriodFromProductLineWarning(warning);
        var totalAmount = rows.Sum(r => GetRowDecimal(r, "SalesAmount") ?? 0m);
        var totalKg = rows.Sum(r => GetRowDecimal(r, "QuantityKg") ?? 0m);
        var overallPerKg = totalKg > 0m ? totalAmount / totalKg : (decimal?)null;

        var headline =
            $"**{product}** monthly sales{period}: **{FormatInr(totalAmount)}** on **{FormatQty(totalKg)} kg**"
            + (overallPerKg.HasValue ? $" — overall **₹{overallPerKg.Value:N2}/kg**" : "")
            + $" across **{rows.Count}** month(s).";

        if (rows.Count <= 6)
        {
            var monthParts = rows.Select(r =>
            {
                var label = GetRowString(r, "MonthName")
                            ?? GetRowString(r, "SalesMonth")
                            ?? "?";
                var yr = GetRowString(r, "SalesYear");
                var monthLabel = yr is not null ? $"{label} {yr}" : label;
                var amt = GetRowDecimal(r, "SalesAmount");
                var pkg = GetRowDecimal(r, "PerKg");
                return pkg.HasValue
                    ? $"**{monthLabel}** {FormatInr(amt)} (₹{pkg.Value:N2}/kg)"
                    : $"**{monthLabel}** {FormatInr(amt)}";
            });
            return $"{headline} {string.Join(" · ", monthParts)}.";
        }

        return headline;
    }

    private static string? ExtractProductLineLabelFromWarning(string? warning)
    {
        if (string.IsNullOrWhiteSpace(warning)) return null;
        var m = Regex.Match(warning, @"Governed (?:monthly )?(.+?) sales (?:summary )?on", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string ExtractPeriodFromProductLineWarning(string? warning)
    {
        if (string.IsNullOrWhiteSpace(warning)) return "";
        var m = Regex.Match(warning, @"\(([^)]+);\s*\d{4}-\d{2}-\d{2}\s+to", RegexOptions.IgnoreCase);
        return m.Success ? $" ({m.Groups[1].Value.Trim()})" : "";
    }

    private static bool LooksLikeWastageKpiRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (keys.Contains("WastageQty") || keys.Contains("Wastage"))
               && (keys.Contains("ProductionQty") || keys.Contains("TapeProduction"));
    }

    private static string BuildWastageKpiBody(List<Dictionary<string, object?>> rows)
    {
        if (rows.Count > 1)
        {
            var totalProd = rows.Sum(r => GetRowDecimal(r, "ProductionQty")
                ?? GetRowDecimal(r, "TapeProduction")
                ?? GetRowDecimal(r, "Fabric")
                ?? GetRowDecimal(r, "SmallBag")
                ?? 0m);
            var totalWastage = rows.Sum(r => GetRowDecimal(r, "WastageQty") ?? GetRowDecimal(r, "Wastage") ?? 0m);
            var overallPct = totalProd > 0m
                ? totalWastage * 100m / totalProd
                : (decimal?)null;
            var dateRaw = GetRowString(rows[0], "ReportDate") ?? GetRowString(rows[0], "Sysdate") ?? GetRowString(rows[0], "date");
            var dateNote = TryFormatRowDate(dateRaw) is { } d ? $" ({d})" : "";
            var pctNote = overallPct.HasValue ? $" — **{overallPct.Value:N2}%** overall" : "";
            var overall = $"Overall{dateNote}: **{FormatQty(totalWastage)}** wastage vs **{FormatQty(totalProd)}** production{pctNote} across **{rows.Count}** department(s).";
            if (rows.Count <= 3)
            {
                var deptParts = rows.Select(r =>
                {
                    var dept = GetRowString(r, "Department") ?? GetRowString(r, "Particulars") ?? "Dept";
                    var pct = GetRowDecimal(r, "WastagePct");
                    return pct.HasValue ? $"**{dept}** {pct.Value:N2}%" : $"**{dept}**";
                });
                return $"{overall} {string.Join(" · ", deptParts)}.";
            }
            return overall;
        }

        var parts = new List<string>();
        foreach (var row in rows)
        {
            var dept = GetRowString(row, "Department")
                       ?? GetRowString(row, "Particulars")
                       ?? "Department";
            var company = GetRowString(row, "CompanyName") ?? GetRowString(row, "companyname");
            var prod = GetRowDecimal(row, "ProductionQty")
                       ?? GetRowDecimal(row, "TapeProduction")
                       ?? GetRowDecimal(row, "Fabric")
                       ?? GetRowDecimal(row, "SmallBag");
            var wastage = GetRowDecimal(row, "WastageQty") ?? GetRowDecimal(row, "Wastage");
            var pct = GetRowDecimal(row, "WastagePct");
            var dateRaw = GetRowString(row, "ReportDate") ?? GetRowString(row, "Sysdate") ?? GetRowString(row, "date");

            var atCompany = string.IsNullOrWhiteSpace(company) ? "" : $" at **{company}**";
            var dateNote = TryFormatRowDate(dateRaw) is { } d ? $" ({d})" : "";
            var pctNote = pct.HasValue ? $" — **{pct.Value:N2}%** of production" : "";

            parts.Add(
                $"**{dept}**{atCompany}{dateNote}: **{FormatQty(wastage)}** wastage vs **{FormatQty(prod)}** production{pctNote}.");
        }

        return string.Join(" ", parts);
    }

    private static string BuildCompositeOperationalBody(List<Dictionary<string, object?>> rows)
    {
        var parts = new List<string>();
        foreach (var group in rows.GroupBy(r => GetRowString(r, "Section") ?? "Result"))
        {
            var sectionRows = group
                .Where(r => !r.ContainsKey("Note") && !r.ContainsKey("Error"))
                .Select(r => r.Where(kv => !kv.Key.Equals("Section", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value))
                .ToList();

            if (group.Any(r => r.ContainsKey("Note")))
            {
                parts.Add($"**{group.Key}:** no matching records.");
                continue;
            }

            if (group.Any(r => r.ContainsKey("Error")))
            {
                var err = GetRowString(group.First(r => r.ContainsKey("Error")), "Error");
                parts.Add($"**{group.Key}:** query failed ({err}).");
                continue;
            }

            if (sectionRows.Count == 0)
            {
                parts.Add($"**{group.Key}:** no matching records.");
                continue;
            }

            if (group.Key.Equals("Stitcher attendance", StringComparison.OrdinalIgnoreCase)
                && sectionRows.Count == 1
                && LooksLikeStitcherAttendanceRow(sectionRows[0]))
            {
                parts.Add($"**{group.Key}:** {BuildStitcherAttendanceBody(sectionRows[0])}");
                continue;
            }

            if (group.Key.Equals("Wastage", StringComparison.OrdinalIgnoreCase)
                && LooksLikeWastageKpiRow(sectionRows[0]))
            {
                parts.Add($"**{group.Key}:** {BuildWastageKpiBody(sectionRows)}");
                continue;
            }

            if (group.Key.Equals("Inventory", StringComparison.OrdinalIgnoreCase)
                && LooksLikeStockInHandRow(sectionRows[0]))
            {
                parts.Add($"**{group.Key}:** {BuildStockInHandBody(sectionRows)}");
                continue;
            }

            if (sectionRows.Count > 1)
            {
                var smart = BuildSmartMultiRowSummary(sectionRows);
                if (!string.IsNullOrWhiteSpace(smart))
                {
                    parts.Add($"**{group.Key}:** {smart}");
                    continue;
                }
            }

            parts.Add($"**{group.Key}:** {sectionRows.Count:N0} row(s).");
        }

        return string.Join("\n\n", parts);
    }

    private static bool LooksLikeStitcherAttendanceRow(Dictionary<string, object?> row) =>
        row.Keys.Any(k => k.Equals("StitchersPresent", StringComparison.OrdinalIgnoreCase));

    private static string BuildStitcherAttendanceBody(Dictionary<string, object?> row)
    {
        var count = GetRowDecimal(row, "StitchersPresent") ?? 0m;
        var dateRaw = GetRowString(row, "AttendanceDate");
        var dateNote = TryFormatRowDate(dateRaw) is { } d ? $" on **{d}**" : "";
        var label = count == 1 ? "stitcher/sewer was" : "stitchers/sewers were";
        return $"**{count:N0}** {label} present{dateNote} (machine punch with intime).";
    }

    private static bool LooksLikeStockInHandRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("StkInHand")
               && (keys.Contains("ItemName") || keys.Contains("ItemCode"));
    }

    private static string BuildStockInHandBody(List<Dictionary<string, object?>> rows)
    {
        var company = GetRowString(rows[0], "CompanyName");
        var atCompany = string.IsNullOrWhiteSpace(company) ? "" : $" at **{company}**";

        if (rows.Count == 1)
        {
            var item = GetRowString(rows[0], "ItemName") ?? GetRowString(rows[0], "ItemCode") ?? "Item";
            var wh = GetRowString(rows[0], "Warehousename") ?? GetRowString(rows[0], "WareHouseName");
            var stk = GetRowDecimal(rows[0], "StkInHand");
            var whNote = string.IsNullOrWhiteSpace(wh) ? "" : $" in **{wh}**";
            return $"**{item}**{atCompany}{whNote}: **{FormatQty(stk)}** in stock.";
        }

        var totalStock = rows.Sum(r => GetRowDecimal(r, "StkInHand") ?? 0m);
        var distinctItems = rows
            .Select(r => GetRowString(r, "ItemCode") ?? GetRowString(r, "ItemName"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var distinctGodowns = rows
            .Select(r => GetRowString(r, "Warehousename") ?? GetRowString(r, "WareHouseName"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var categoryPart = BuildStockCategorySummary(rows);
        var scopeNote = distinctGodowns > 0
            ? $" across **{distinctGodowns}** godown(s), **{distinctItems}** item(s)"
            : $" across **{distinctItems}** item(s)";

        return string.IsNullOrWhiteSpace(categoryPart)
            ? $"**{rows.Count:N0}** stock line(s){atCompany} — **total {FormatQty(totalStock)}** in hand{scopeNote}."
            : $"**{rows.Count:N0}** stock line(s){atCompany} — **total {FormatQty(totalStock)}** in hand{scopeNote}. {categoryPart}";
    }

    private static string BuildStockCategorySummary(List<Dictionary<string, object?>> rows)
    {
        var buckets = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var name = (GetRowString(row, "ItemName") ?? GetRowString(row, "ItemCode") ?? "").ToLowerInvariant();
            var stk = GetRowDecimal(row, "StkInHand") ?? 0m;
            if (stk == 0m) continue;

            var bucket = name.Contains("fabric") ? "Fabric"
                : name.Contains("webbing") ? "Webbing"
                : name.Contains("filler") ? "Filler"
                : name.Contains("yarn") ? "Yarn"
                : name.Contains("tape") ? "Tape"
                : "Other";

            buckets[bucket] = buckets.GetValueOrDefault(bucket) + stk;
        }

        var parts = buckets
            .Where(kv => kv.Value > 0m && !kv.Key.Equals("Other", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kv => kv.Value)
            .Select(kv => $"**{kv.Key}** {FormatQty(kv.Value)}")
            .ToList();

        if (parts.Count == 0 && buckets.TryGetValue("Other", out var other) && other > 0m)
            parts.Add($"**Other** {FormatQty(other)}");

        return parts.Count == 0 ? "" : $"By material: {string.Join(" · ", parts)}.";
    }

    private static string FormatQty(decimal? qty) =>
        qty.HasValue ? qty.Value.ToString("N2", CultureInfo.GetCultureInfo("en-IN")).TrimEnd('0').TrimEnd('.') : "0";

    private static string? TryFormatRowDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            || DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out dt))
            return FormatIndianDate(dt);
        return null;
    }

    private static bool LooksLikeLedgerMasterBalanceRow(Dictionary<string, object?> row)
    {
        var keys = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("LedgerName")
               && (keys.Contains("PendingBalance") || keys.Contains("Openingbalance"));
    }

    private static string BuildLedgerBalanceBody(Dictionary<string, object?> row)
    {
        var ledger = GetRowString(row, "LedgerName") ?? "This party";
        var company = GetRowString(row, "CompanyName");
        var opening = GetRowDecimal(row, "Openingbalance");
        var pending = GetRowDecimal(row, "PendingBalance");
        var under = GetRowString(row, "Under");
        var source = GetRowString(row, "OutstandingSource");
        var period = GetRowString(row, "StatementPeriod");

        var atCompany = string.IsNullOrWhiteSpace(company) ? "" : $" at **{company}**";
        var groupNote = string.IsNullOrWhiteSpace(under) ? "" : $" Group: **{under}**.";

        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceLabel = source switch
            {
                "LedgerStatement" => string.IsNullOrWhiteSpace(period)
                    ? "ERP ledger statement (current FY)"
                    : $"ERP ledger statement (FY {period})",
                "OverdueSummary" => "ERP overdue summary",
                "BillWiseAgeing" => "bill-wise ageing",
                _ => "ERP outstanding report",
            };

            return
                $"**{ledger}**{atCompany}: LedgerMaster snapshot shows **₹0.00** pending, but **{sourceLabel}** shows **{FormatInr(pending)}** outstanding"
                + (opening.HasValue && Math.Abs(opening.Value) >= 0.01m ? $" (opening **{FormatInr(opening)}**)" : "")
                + $".{groupNote}";
        }

        return
            $"**{ledger}**{atCompany} has **{FormatInr(pending)}** pending balance and **{FormatInr(opening)}** opening balance.{groupNote}";
    }

    private static string? BuildLedgerStatementBody(string warning, List<Dictionary<string, object?>> rows)
    {
        var meta = Regex.Match(
            warning,
            @":\s*(.+?)\s+at\s+(.+?)\s+from\s+(\d{4}-\d{2}-\d{2})\s+to\s+(\d{4}-\d{2}-\d{2})\.\s+Opening\s+([\d,.\-]+),\s+closing\s+([\d,.\-]+)",
            RegexOptions.IgnoreCase);
        if (!meta.Success) return null;

        var ledger = meta.Groups[1].Value.Trim();
        var company = meta.Groups[2].Value.Trim();
        var from = DateTime.ParseExact(meta.Groups[3].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = DateTime.ParseExact(meta.Groups[4].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var opening = meta.Groups[5].Value.Trim();
        var closing = meta.Groups[6].Value.Trim();

        var voucherNote = rows.Count switch
        {
            0 => "No voucher lines were returned for this period.",
            1 => "One voucher line is shown below.",
            _ => $"{rows.Count:N0} voucher lines are shown below.",
        };

        return
            $"Ledger statement for **{ledger}** at **{company}** ({FormatIndianDate(from)} – {FormatIndianDate(to)}): opening **₹{opening}**, closing **₹{closing}**. {voucherNote}";
    }

    private static string? GetRowString(Dictionary<string, object?> row, string key)
    {
        var match = row.FirstOrDefault(kvp =>
            kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (match.Key is null) return null;
        var text = match.Value?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static decimal? GetRowDecimal(Dictionary<string, object?> row, string key)
    {
        var match = row.FirstOrDefault(kvp =>
            kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (match.Key is null || match.Value is null) return null;
        if (match.Value is decimal d) return d;
        if (match.Value is double db) return (decimal)db;
        if (match.Value is float f) return (decimal)f;
        if (match.Value is int i) return i;
        if (match.Value is long l) return l;
        return decimal.TryParse(match.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string FormatInr(decimal? amount) =>
        amount.HasValue ? $"₹{amount.Value:N2}" : "₹0.00";

    private static string FormatIndianDate(DateTime date) =>
        date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-IN"));
}
