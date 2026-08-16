using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static string? TryResolveVendorFirmNameForEarly(string message)
    {
        return ResolveVendorFirmForChat(message);
    }

    private static string? TryExtractVendorFirmFromMessage(string message)
    {
        var m = Regex.Match(
            message,
            @"\b(?:for|from|of)\s+(?:vendor\s+)?(.+?)(?:\s+(?:with|at|and|show|list)|\?|$)",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var name = m.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
        if (name.Length < 3
            || name.Equals("vendor", StringComparison.OrdinalIgnoreCase)
            || name.Equals("supplier", StringComparison.OrdinalIgnoreCase))
            return null;
        return name;
    }

    private static bool TryBuildVendorProfileEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeVendorProfileQuestion(message)) return false;
        if (TryResolveVendorFirmNameForEarly(message) is not { } firm) return false;

        sql = BuildVendorProfileSql(firm, LooksLikeVendorBankOnlyQuestion(message));
        warning = $"Governed vendor profile for {firm} (Vendor/vw_VendorListwithBankdtls; NewGSTNo not GSTNo).";
        return true;
    }

    private static bool LooksLikeVendorCodeQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("vendor code") || m.Contains("ven0");
    }

    private static bool TryBuildVendorCodeEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeVendorCodeQuestion(message)) return false;
        if (TryResolveVendorFirmNameForEarly(message) is not { } firm) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                FirmName, VendorCode, NewGSTNo, PANNo, Email, ISMSME
            FROM Vendor WITH (NOLOCK)
            WHERE FirmName LIKE '%{EscapeSqlLiteral(firm)}%'
            ORDER BY FirmName
            """;
        warning = $"Governed vendor code lookup for {firm} (Vendor.VendorCode).";
        return true;
    }

    private static bool LooksLikeVendorRateQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("quotation") || m.Contains("quoted")) return false;
        return (m.Contains("rate") || m.Contains("nego") || m.Contains("price"))
               && (m.Contains("vendor") || m.Contains("supplier") || m.Contains("item")
                   || ResolveVendorFirmAlias(message) is not null
                   || Regex.IsMatch(m, @"\b[a-z]{3}\d{5}\b", RegexOptions.IgnoreCase));
    }

    private static bool TryBuildVendorRateEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeVendorRateQuestion(message)) return false;

        var firm = TryResolveVendorFirmNameForEarly(message);
        var itemMatch = Regex.Match(message, @"\b(?:item|code)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        var itemCode = itemMatch.Success ? itemMatch.Groups[1].Value.Trim() : null;
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            var wipMatch = Regex.Match(message, @"\b(WIP\d+)\b", RegexOptions.IgnoreCase);
            if (wipMatch.Success) itemCode = wipMatch.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(firm) && string.IsNullOrWhiteSpace(itemCode)) return false;

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(firm))
            filters.Add($"FirmName LIKE '%{EscapeSqlLiteral(firm)}%'");
        if (!string.IsNullOrWhiteSpace(itemCode))
            filters.Add($"(ItemCode LIKE '%{EscapeSqlLiteral(itemCode)}%' OR SubCode LIKE '%{EscapeSqlLiteral(itemCode)}%')");

        sql = $"""
            SELECT TOP {MaxReturnRows}
                FirmName, ItemCode, SubCode, ItemDesc, Rate, NegoRate, PaymentTerm, Discount, Sysdate
            FROM VendorRate WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY Sysdate DESC, Rate DESC
            """;
        warning = "Governed vendor-item rates (VendorRate; mandatory FirmName/ItemCode filter + TOP 50).";
        return true;
    }

    private static bool LooksLikeMsmeVendorListQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("msme") && (m.Contains("vendor") || m.Contains("list") || m.Contains("firm"));
    }

    private static bool TryBuildMsmeVendorListEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeMsmeVendorListQuestion(message)) return false;
        if (LooksLikeMsmeOverdueQuestion(message)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                FirmName, VendorCode, ISMSME, MSMENumber, NewGSTNo, Email, City
            FROM Vendor WITH (NOLOCK)
            WHERE ISMSME = 'Yes' OR MSMENumber IS NOT NULL
            ORDER BY FirmName
            """;
        warning = "Governed MSME vendor list (Vendor.ISMSME/MSMENumber).";
        return true;
    }

    private static bool LooksLikeInternalVendorQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("internal vendor") || (m.Contains("internal") && m.Contains("vendor"));
    }

    private static bool TryBuildInternalVendorEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeInternalVendorQuestion(message)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                FirmName, actualCompanyId
            FROM InternalVendor WITH (NOLOCK)
            ORDER BY FirmName
            """;
        warning = "Governed internal vendor list (InternalVendor).";
        return true;
    }
}
