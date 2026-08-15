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

    private static bool LooksLikePlantRmStockQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("stock analysis") || m.Contains("stockanalysis")) return false;
        return (m.Contains("raw material stock") || m.Contains("rm stock at plant")
                || m.Contains("loom stock") || m.Contains("tape plant stock")
                || m.Contains("lamination stock") || m.Contains("tfo stock")
                || m.Contains("needle loom stock") || m.Contains("plant wise rm"))
               && (m.Contains("plant") || m.Contains("loom") || m.Contains("tape")
                   || m.Contains("lamination") || m.Contains("tfo") || m.Contains("needle"));
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

        var company = ResolveOutwardCompanyAlias(message)
                      ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");

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
