using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeUserLookupQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("password")) return false;
        return m.Contains("email") || m.Contains("login") || m.Contains("username")
               || m.Contains("user ") || m.Contains("approver") || m.Contains("requester")
               || m.Contains("contact no") || m.Contains("employee")
               || m.Contains("finance people") || m.Contains("purchase people")
               || Regex.IsMatch(m, @"\b[a-z]+\s+ka\s+email\b");
    }

    private static string? TryExtractUsername(string message)
    {
        var quoted = Regex.Match(message, @"['""]([a-zA-Z0-9_]+)['""]");
        if (quoted.Success) return quoted.Groups[1].Value.Trim();

        var userMatch = Regex.Match(
            message,
            @"\b(?:user|username|login|name)\s+([a-zA-Z0-9_]+)\b",
            RegexOptions.IgnoreCase);
        if (userMatch.Success) return userMatch.Groups[1].Value.Trim();

        var emailOf = Regex.Match(
            message,
            @"\b(?:email\s+(?:of|for)|contact\s+for)\s+([a-zA-Z0-9_]+)\b",
            RegexOptions.IgnoreCase);
        if (emailOf.Success) return emailOf.Groups[1].Value.Trim();

        return null;
    }

    private static bool TryBuildUserLookupEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeUserLookupQuestion(message)) return false;

        var m = message.ToLowerInvariant();
        if (m.Contains("admin") && (m.Contains("how many") || m.Contains("count")))
        {
            sql = """
                SELECT COUNT(*) AS AdminUserCount
                FROM loginentry.dbo.LoginRights WITH (NOLOCK)
                WHERE IsAdmin = 'yes'
                """;
            warning = "Governed admin user count (loginentry.dbo.LoginRights; never Password).";
            return true;
        }

        if (m.Contains("finance") && m.Contains("email"))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Name, FullName, Email, Category
                FROM loginentry.dbo.LoginRights WITH (NOLOCK)
                WHERE IsFinance = 'yes' AND Email IS NOT NULL
                ORDER BY Name
                """;
            warning = "Governed finance user emails (loginentry.dbo.LoginRights).";
            return true;
        }

        if (m.Contains("purchase") && m.Contains("email"))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Name, FullName, Email, Category
                FROM loginentry.dbo.LoginRights WITH (NOLOCK)
                WHERE Category = 'Purchase' AND Email IS NOT NULL
                ORDER BY Name
                """;
            warning = "Governed purchase user emails (loginentry.dbo.LoginRights).";
            return true;
        }

        if (m.Contains("po requester") || (m.Contains("requester") && m.Contains("email")))
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    p.LoginName, lr.FullName, lr.Email
                FROM PurchasePayment p WITH (NOLOCK)
                JOIN loginentry.dbo.LoginRights lr ON lr.Name = p.LoginName
                WHERE lr.Email IS NOT NULL
                ORDER BY p.deliverydate DESC
                """;
            warning = "Governed PO requester emails (PurchasePayment.LoginName → LoginRights).";
            return true;
        }

        if (TryExtractUsername(message) is { } username)
        {
            sql = $"""
                SELECT TOP {MaxReturnRows}
                    Name, FullName, Email, ContactNo, EmpCode, Category
                FROM loginentry.dbo.LoginRights WITH (NOLOCK)
                WHERE Name = '{EscapeSqlLiteral(username)}'
                """;
            warning = $"Governed user lookup for {username} (loginentry.dbo.LoginRights; never Password).";
            return true;
        }

        return false;
    }

    private static bool LooksLikeIndentItemsQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("pending") && m.Contains("indent")) return false;
        if (m.Contains("quotation") || m.Contains("quote")) return false;
        if (!m.Contains("indent")) return false;
        return m.Contains("item") || m.Contains("qty") || m.Contains("quantity")
               || m.Contains("line") || m.Contains("material")
               || TryExtractIndentNo(message) is not null;
    }

    private static bool TryBuildIndentItemsEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeIndentItemsQuestion(message)) return false;
        if (TryExtractIndentNo(message) is not { } indent) return false;

        var lit = EscapeSqlLiteral(indent);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                sd.Expr1 AS IndentNo, sd.code AS IndentSubCode, sd.CompanyName, sd.Deptt,
                sd.ItemCode, sd.ItemName, sd.Qty, sd.Unit, ii.itemdesc
            FROM Vw_StoreDeptt sd WITH (NOLOCK)
            LEFT JOIN ItemInfo ii ON sd.code = ii.code
            WHERE sd.Expr1 = '{lit}' OR sd.Expr1 LIKE '%{lit}%'
            ORDER BY sd.code
            """;
        warning = $"Governed indent line items for {indent} (Vw_StoreDeptt + ItemInfo).";
        return true;
    }

    private static bool LooksLikeSalesEbdQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("invoice") && m.Contains("item")) return false;
        return (m.Contains("sales") && (m.Contains("ebd") || m.Contains("qty") || m.Contains("quantity")))
               || m.Contains("sales ebd") || m.Contains("item wise sales");
    }

    private static bool TryBuildSalesEbdEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeSalesEbdQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"companyname = '{EscapeSqlLiteral(company)}'");

        var itemMatch = Regex.Match(message, @"\b(?:item|code)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        if (itemMatch.Success)
            filters.Add($"itemcode LIKE '%{EscapeSqlLiteral(itemMatch.Groups[1].Value)}%'");

        if (filters.Count == 0) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname, InvNo, itemcode, itemname, qTY, Commodity, sysdate
            FROM VW_SALES_EBD_DTL WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY sysdate DESC
            """;
        warning = "Governed MIS sales qty by item (VW_SALES_EBD_DTL; prefer SalesVoucherItem for rates).";
        return true;
    }
}
