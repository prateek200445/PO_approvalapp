using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class SqlGuardService
{
    // Word-boundary keywords so LoginRights columns like DelIndent / CashFlowCreation are not blocked.
    private static readonly Regex BannedKeywordRegex = BannedKeywordRegexGen();

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

        var banned = BannedKeywordRegex.Match(sql);
        if (banned.Success)
            throw new InvalidOperationException($"SQL blocked: contains forbidden token '{banned.Value}'.");

        // Prefix-style dangerous tokens
        if (Regex.IsMatch(sql, @"\bXP_", RegexOptions.IgnoreCase)
            || Regex.IsMatch(sql, @"\bSP_", RegexOptions.IgnoreCase)
            || Regex.IsMatch(sql, @"\bOPENROWSET\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(sql, @"\bOPENDATASOURCE\b", RegexOptions.IgnoreCase))
        {
            throw new InvalidOperationException("SQL blocked: contains forbidden system procedure/token.");
        }

        var upper = sql.ToUpperInvariant();
        if (!upper.TrimStart().StartsWith("SELECT", StringComparison.Ordinal)
            && !upper.TrimStart().StartsWith("WITH", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only SELECT / WITH...SELECT queries are allowed.");
        }

        // Never allow selecting Password from login tables
        if (Regex.IsMatch(sql, @"\bPassword\b", RegexOptions.IgnoreCase)
            && Regex.IsMatch(sql, @"loginrights|LoginRights", RegexOptions.IgnoreCase))
        {
            throw new InvalidOperationException("SQL blocked: Password column must never be selected from LoginRights.");
        }

        // Huge vendor-rate objects require a selective filter
        if (Regex.IsMatch(sql, @"\bVendorRate\b|\bVw_VendorItem\b", RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(
                    "SQL blocked: VendorRate/Vw_VendorItem queries require a WHERE filter (FirmName and/or ItemCode).");
            if (!Regex.IsMatch(sql, @"\bFirmName\b|\bItemCode\b|\bSubCode\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(
                    "SQL blocked: VendorRate/Vw_VendorItem must filter FirmName, ItemCode, or SubCode.");
        }

        // Huge despatch objects require a selective filter
        if (Regex.IsMatch(sql,
                @"\bvw_MISrolldespatch\b|\bMISRollforDespatch\b|\bFIBCDespatch\b|\bMIS_YarnDespatch\b|\bSmallBagBailForDespatch\b|\bvw_RollforDespatch\b|\bvw_rollforDespatchLaminated\b",
                RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(
                    "SQL blocked: despatch queries require a WHERE filter (company, invoice, party, or date).");
            if (!Regex.IsMatch(sql,
                    @"\bCompanyName\b|\bCompanyname\b|\bInvNo\b|\bInvno\b|\bPartyName\b|\bPartyname\b|\bsysdate\b|\bSysDate\b|\bPACKINGLISTNO\b|\bPackingListNo\b",
                    RegexOptions.IgnoreCase))
                throw new InvalidOperationException(
                    "SQL blocked: despatch queries must filter CompanyName/Companyname, InvNo, PartyName, date, or packing list.");
        }

        return sql;
    }

    private static string ExtractSql(string text)
    {
        var fence = SqlFenceRegex().Match(text);
        if (fence.Success)
            return fence.Groups[1].Value.Trim();

        var trimmed = text.Trim();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            return trimmed;

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

    [GeneratedRegex(
        @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|GRANT|REVOKE|DENY|BACKUP|RESTORE|SHUTDOWN|DBCC)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BannedKeywordRegexGen();
}
