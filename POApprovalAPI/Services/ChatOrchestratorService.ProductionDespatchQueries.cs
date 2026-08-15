using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool TryBuildFactoryProductionEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (m.Contains("webbing")) return false;
        if (!m.Contains("production") && !m.Contains("factory") && !m.Contains("produced")) return false;
        if (m.Contains("despatch") || m.Contains("dispatch")) return false;
        if (m.Contains("fibc") && m.Contains("bag")) return false;
        if (m.Contains("loom") && m.Contains("quality")) return false;
        if (m.Contains("small bag") && m.Contains("cutting")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        var particularsFilter = "";
        if (m.Contains("tape")) particularsFilter = " AND Particulars LIKE '%TAPE%'";
        else if (m.Contains("fabric")) particularsFilter = " AND Particulars LIKE '%FABRIC%'";
        else if (m.Contains("small bag")) particularsFilter = " AND Particulars LIKE '%SMALL%'";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname, Sysdate, Particulars, TapeProduction, Fabric, SmallBag, Loom, Wastage
            FROM vw_FactoryProduction WITH (NOLOCK)
            WHERE companyname = '{EscapeSqlLiteral(company)}'
            {particularsFilter}
            ORDER BY Sysdate DESC
            """;
        warning = $"Governed factory daily production for {company} (vw_FactoryProduction; tape/fabric/small bag).";
        return true;
    }

    private static bool TryBuildTapePlantEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("tape") && !m.Contains("loom dept") && !m.Contains("fibc dept")) return false;
        if (!m.Contains("production") && !m.Contains("opening") && !m.Contains("closing")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname, Sysdate, [Loom Dept], [FIBC Dept], Opening, Closing, Production, Wastage
            FROM vw_daily_tape_prod_New WITH (NOLOCK)
            WHERE companyname = '{EscapeSqlLiteral(company)}'
            ORDER BY Sysdate DESC
            """;
        warning = $"Governed tape plant daily production for {company} (vw_daily_tape_prod_New; bracket dept columns).";
        return true;
    }

    private static bool LooksLikeWipQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("wip") && (m.Contains("consumption") || m.Contains("report") || m.Contains("item"));
    }

    private static bool TryBuildWipReportEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeWipQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var itemMatch = Regex.Match(message, @"\b(WIP\d+)\b", RegexOptions.IgnoreCase);
        if (itemMatch.Success)
            filters.Add($"ItemCode LIKE '%{EscapeSqlLiteral(itemMatch.Groups[1].Value)}%'");

        if (filters.Count == 0) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, ItemCode, ItemName, Deptt, Qty, ConsumptionQty, Sysdate
            FROM vw_WIPReport WITH (NOLOCK)
            WHERE {string.Join(" AND ", filters)}
            ORDER BY Sysdate DESC
            """;
        warning = "Governed WIP consumption (vw_WIPReport; filter company/item + TOP 50).";
        return true;
    }

    private static bool TryBuildProductionEbdEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("production") && !m.Contains("produced")) return false;
        if (!m.Contains("ebd") && !m.Contains("plant") && !m.Contains("by item")) return false;
        if (m.Contains("sales") || m.Contains("despatch")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                companyname, ItemCode, ItemName, Qty, GroupName, PlantName, sysdate
            FROM VW_PRODUCTION_EBD_DTL WITH (NOLOCK)
            WHERE companyname = '{EscapeSqlLiteral(company)}'
            ORDER BY sysdate DESC
            """;
        warning = $"Governed production qty by plant for {company} (VW_PRODUCTION_EBD_DTL).";
        return true;
    }

    private static bool LooksLikeRollDespatchQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("despatch") && !m.Contains("dispatch")) return false;
        if (!m.Contains("roll")) return false;
        if (m.Contains("waiting") || m.Contains("pending") || m.Contains("available")) return false;
        return true;
    }

    private static bool TryBuildRollDespatchEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeRollDespatchQuestion(message)) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                RollNo, NetWt, PartyName, Companyname, InvNo, DespatchDate, Quality, Metre
            FROM vw_MISrolldespatch WITH (NOLOCK)
            WHERE Companyname = '{EscapeSqlLiteral(company)}'
            ORDER BY DespatchDate DESC
            """;
        warning = $"Governed roll despatch for {company} (vw_MISrolldespatch).";
        return true;
    }

    private static bool TryBuildFibcDespatchEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("fibc")) return false;
        if (!m.Contains("despatch") && !m.Contains("dispatch") && !m.Contains("packing")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"CompanyName = '{EscapeSqlLiteral(company)}'");

        var where = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "";

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, PartyName, BailNo, BagPCS, NetWt, InvNo, DespatchDate, PONO
            FROM FIBCDespatch WITH (NOLOCK)
            {where}
            ORDER BY DespatchDate DESC
            """;
        warning = "Governed FIBC despatch packing list (FIBCDespatch).";
        return true;
    }

    private static bool TryBuildYarnDespatchEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("yarn")) return false;
        if (!m.Contains("despatch") && !m.Contains("dispatch") && !m.Contains("packing")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, PartyName, InvNo, DespatchDate, Qty, ItemName, PackingListNo
            FROM MIS_YarnDespatch WITH (NOLOCK)
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
            ORDER BY DespatchDate DESC
            """;
        warning = $"Governed yarn despatch packing list for {company} (MIS_YarnDespatch).";
        return true;
    }

    private static bool TryBuildSmallBagDespatchEarlySql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        var m = message.ToLowerInvariant();
        if (!m.Contains("small bag") && !m.Contains("smallbag")) return false;
        if (!m.Contains("despatch") && !m.Contains("dispatch") && !m.Contains("bail")) return false;

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
        if (string.IsNullOrWhiteSpace(company)) return false;

        sql = $"""
            SELECT TOP {MaxReturnRows}
                CompanyName, PartyName, BailNo, NetWt, InvNo, DespatchDate, Qty
            FROM SmallBagBailForDespatch WITH (NOLOCK)
            WHERE CompanyName = '{EscapeSqlLiteral(company)}'
            ORDER BY DespatchDate DESC
            """;
        warning = $"Governed small-bag bail despatch for {company} (SmallBagBailForDespatch).";
        return true;
    }
}
