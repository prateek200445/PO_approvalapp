using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static bool LooksLikeWarehouseStockSummaryQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("warehouse stock summary") || m.Contains("warehouse stock report")
                || m.Contains("godown stock summary") || m.Contains("warehouse wise stock summary"))
               && !m.Contains("ageing") && !m.Contains("aging");
    }

    private static bool LooksLikeStockSummaryByDeptQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("stock summary") || m.Contains("stock report by dept")
                || m.Contains("department wise stock"))
               && !LooksLikeWarehouseStockSummaryQuestion(message)
               && !m.Contains("ageing") && !m.Contains("analysis report");
    }

    private static bool HasExplicitPlantRmStockIntent(string messageLower) =>
        messageLower.Contains("plant") || messageLower.Contains("loom") || messageLower.Contains("tape")
        || messageLower.Contains("lamination") || messageLower.Contains("tfo") || messageLower.Contains("needle")
        || messageLower.Contains("brrope") || messageLower.Contains("braid") || messageLower.Contains("fcbr");

    private static bool LooksLikeGenericRmWarehouseStockQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("ageing") || m.Contains("aging") || m.Contains("stock analysis") || m.Contains("stockanalysis"))
            return false;
        if (!m.Contains("stock")) return false;

        var hasRm = m.Contains("rm stock") || m.Contains("raw material stock")
            || (Regex.IsMatch(m, @"\brm\b") && m.Contains("stock"));
        if (!hasRm || HasExplicitPlantRmStockIntent(m)) return false;

        return m.Contains("current") || m.Contains("show") || m.Contains("what is")
               || m.Contains("how much") || m.Contains("list");
    }

    private static bool TryBuildRmWarehouseStockSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeGenericRmWarehouseStockQuestion(message)) return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company)) return false;

        var companyLit = EscapeSqlLiteral(company);
        sql = $"""
            SELECT TOP {MaxReturnRows}
                warehouse,
                Deptt,
                GroupName,
                SubGroupName,
                itemcode AS ItemCode,
                ItemName,
                ROUND(ISNULL(StkInHand, 0), 2) AS StkInHand,
                unit
            FROM vw_inventoryitemwarehouse_all WITH (NOLOCK)
            WHERE CompanyName = '{companyLit}'
              AND ISNULL(StkInHand, 0) <> 0
              AND (
                  ISNULL(Deptt, '') LIKE '%RM%'
                  OR ISNULL(GroupName, '') LIKE '%RM%'
                  OR ISNULL(GroupName, '') LIKE '%Raw%'
                  OR ISNULL(SubGroupName, '') LIKE '%RM%'
              )
            ORDER BY StkInHand DESC
            """;
        warning =
            $"Governed current RM warehouse stock on vw_inventoryitemwarehouse_all for {company} (Deptt/GroupName RM; use loom/tape plant phrasing for sp_Prod_GetRowMaterialStock_*).";
        return true;
    }

    /// <summary>
    /// When sp_Agingreport_SubgroupName returns 0 rows, show FG/SF/RM stock with age from last production or inward.
    /// </summary>
    private static bool TryBuildStockGroupAgeingFallbackSql(string message, out string sql, out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeStockAgeingQuestion(message))
            return false;

        var company = ResolveCompanyForChat(message);
        if (string.IsNullOrWhiteSpace(company))
            return false;

        var group = ResolveStockAgeingGroupName(message);
        var subgroup = ResolveStockAgeingSubGroupName(message);
        if (string.IsNullOrWhiteSpace(group) && string.IsNullOrWhiteSpace(subgroup))
            return false;

        var filters = new List<string>
        {
            $"v.CompanyName = '{EscapeSqlLiteral(company)}'",
            "ISNULL(v.StkInHand, 0) > 0",
        };
        if (!string.IsNullOrWhiteSpace(group))
        {
            var g = EscapeSqlLiteral(group);
            filters.Add($"(v.GroupName = '{g}' OR v.Deptt LIKE '%{g}%')");
        }

        if (!string.IsNullOrWhiteSpace(subgroup))
            filters.Add($"v.SubGroupName LIKE '%{EscapeSqlLiteral(subgroup)}%'");

        var label = !string.IsNullOrWhiteSpace(group) ? group : subgroup!;
        sql = $"""
            SELECT TOP {MaxReturnRows}
                v.GroupName,
                v.SubGroupName,
                v.itemcode AS ItemCode,
                v.ItemName,
                v.warehouse,
                ROUND(ISNULL(v.StkInHand, 0), 2) AS StkInHand,
                prod.LastProductionDate,
                inward.LastInwardDate,
                DATEDIFF(
                    day,
                    COALESCE(prod.LastProductionDate, inward.LastInwardDate),
                    CAST(GETDATE() AS date)) AS StockAgeDays
            FROM vw_inventoryitemwarehouse_all v WITH (NOLOCK)
            OUTER APPLY (
                SELECT MAX(CAST(p.sysdate AS date)) AS LastProductionDate
                FROM VW_PRODUCTION_EBD_DTL p WITH (NOLOCK)
                WHERE p.companyname = v.CompanyName
                  AND p.ItemCode = v.itemcode
            ) prod
            OUTER APPLY (
                SELECT MAX(CAST(si.BillDate AS date)) AS LastInwardDate
                FROM Vw_StoreInwards si WITH (NOLOCK)
                WHERE si.CompanyName = v.CompanyName
                  AND si.ItemCode = v.itemcode
            ) inward
            WHERE {string.Join(" AND ", filters)}
            ORDER BY StockAgeDays DESC, v.StkInHand DESC
            """;
        warning =
            $"Governed {label} stock ageing fallback for {company} (vw_inventoryitemwarehouse_all + last production/inward date; ERP ageing SP returned 0 rows).";
        return true;
    }

    private static bool LooksLikePlantRmStockQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("stock analysis") || m.Contains("stockanalysis")) return false;
        if ((m.Contains("current") || m.Contains("show me") || m.Contains("show"))
            && (m.Contains("rm stock") || m.Contains("raw material stock")))
            return HasExplicitPlantRmStockIntent(m);

        return (m.Contains("raw material stock") || m.Contains("rm stock at plant")
                || m.Contains("rm stock") || m.Contains("loom stock") || m.Contains("tape plant stock")
                || m.Contains("lamination stock") || m.Contains("tfo stock")
                || m.Contains("needle loom stock") || m.Contains("plant wise rm"))
               && HasExplicitPlantRmStockIntent(m);
    }

    private static bool LooksLikeMisReportQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("mis report") || m.Contains("mis data") || m.Contains("mis consolidated"))
               && !m.Contains("ebd") && !m.Contains("dashboard");
    }

    private static bool LooksLikeTop100PurchasedQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("top 100") && (m.Contains("purchase") || m.Contains("stores")
                                         || m.Contains("spares") || m.Contains("item"));
    }

    private static bool LooksLikeEbidtaPivotQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("pivot") && m.Contains("ebidta");
    }

    private static bool LooksLikeAutoStockSnapshotQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("auto stock") || m.Contains("roll stock report")
               || m.Contains("fibc stock report") || m.Contains("small bag stock report")
               || m.Contains("roll item stock") || m.Contains("small bag item stock");
    }

    private static bool TryBuildInventoryReportPlan(string message, out ErpInventoryReportPlan plan)
    {
        plan = new ErpInventoryReportPlan { MaxRows = MaxReturnRows };

        if (TryBuildStockAnalysisPlan(message, out plan))
            return true;

        plan = new ErpInventoryReportPlan { MaxRows = MaxReturnRows };

        if (LooksLikeTop100PurchasedQuestion(message))
        {
            plan.Mode = ErpInventoryReportMode.Top100PurchasedItems;
            return true;
        }

        if (LooksLikeAutoStockSnapshotQuestion(message))
        {
            var m = message.ToLowerInvariant();
            if (m.Contains("fibc")) plan.Mode = ErpInventoryReportMode.AutoFibcStock;
            else if (m.Contains("small bag item")) plan.Mode = ErpInventoryReportMode.SmallBagItemStock;
            else if (m.Contains("small bag")) plan.Mode = ErpInventoryReportMode.AutoSmallBagStock;
            else if (m.Contains("roll item")) plan.Mode = ErpInventoryReportMode.RollItemStock;
            else plan.Mode = ErpInventoryReportMode.AutoRollStock;
            return true;
        }

        var company = ResolveCompanyForChat(message);

        if (LooksLikeEbidtaPivotQuestion(message))
        {
            if (string.IsNullOrWhiteSpace(company)) return false;
            var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
            plan.CompanyName = company;
            plan.DateFrom = fyStart;
            plan.DateTo = fyEndEx.AddDays(-1);
            plan.Mode = message.Contains("purchase", StringComparison.OrdinalIgnoreCase)
                ? ErpInventoryReportMode.EbidtaPivotPurchase
                : ErpInventoryReportMode.EbidtaPivotSales;
            return true;
        }

        if (LooksLikeMisReportQuestion(message))
        {
            if (string.IsNullOrWhiteSpace(company)) return false;
            var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
            plan.CompanyName = company;
            plan.DateFrom = fyStart;
            plan.DateTo = fyEndEx.AddDays(-1);
            plan.Mode = ErpInventoryReportMode.MisReport;
            return true;
        }

        if (LooksLikeWarehouseStockSummaryQuestion(message))
        {
            if (string.IsNullOrWhiteSpace(company)) return false;
            var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
            plan.CompanyName = company;
            plan.DateFrom = fyStart;
            plan.DateTo = fyEndEx.AddDays(-1);
            plan.DeptName = TryExtractDepartmentFragment(message);
            plan.Mode = ErpInventoryReportMode.WarehouseStockSummary;
            return true;
        }

        if (LooksLikeStockSummaryByDeptQuestion(message))
        {
            if (string.IsNullOrWhiteSpace(company)) return false;
            var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
            plan.CompanyName = company;
            plan.DateFrom = fyStart;
            plan.DateTo = fyEndEx.AddDays(-1);
            plan.DeptName = TryExtractDepartmentFragment(message);
            plan.Mode = ErpInventoryReportMode.StockSummaryByDept;
            return true;
        }

        if (LooksLikePlantRmStockQuestion(message))
        {
            if (string.IsNullOrWhiteSpace(company)) return false;
            var (fyStart, fyEndEx, _) = ParseIndianFinancialYear(message);
            plan.CompanyName = company;
            plan.DateFrom = fyStart;
            plan.DateTo = fyEndEx.AddDays(-1);
            plan.PlantStockSp = ResolvePlantRmStockSp(message);
            plan.PlantName = TryExtractPlantName(message);
            plan.Mode = ErpInventoryReportMode.PlantRawMaterialStock;
            return true;
        }

        return false;
    }

    private static string ResolvePlantRmStockSp(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("tape")) return "sp_Prod_GetRowMaterialStock_Tape";
        if (m.Contains("lamination")) return "sp_Prod_GetRowMaterialStock_Lamination";
        if (m.Contains("tfo")) return "sp_Prod_GetRowMaterialStock_TFO";
        if (m.Contains("needle")) return "sp_Prod_GetRowMaterialStock_NeedleLoom";
        if (m.Contains("brrope") || m.Contains("braid")) return "sp_Prod_GetRowMaterialStock_BRROPE";
        if (m.Contains("fcbr")) return "sp_Prod_GetRowMaterialStock_FCBR";
        if (m.Contains("loom")) return "sp_Prod_GetRowMaterialStock_Loom";

        var company = ResolveCompanyForChat(message)?.ToLowerInvariant() ?? "";
        if (company.Contains("woven")) return "sp_Prod_GetRowMaterialStock_Tape";

        return "sp_Prod_GetRowMaterialStock_Loom";
    }

    private static string? TryExtractPlantName(string message)
    {
        var m = Regex.Match(message, @"\bplant\s+(.+?)(?:\s+at|\s+for|\?|$)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var name = m.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
        return name.Length >= 2 ? name : null;
    }
}
