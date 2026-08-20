using Microsoft.Data.SqlClient;
using Dapper;

const string company = "Plastene India Limited (Unit -II)";
var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? throw new InvalidOperationException("Set DB_PASSWORD env var.");
var server = "103.240.33.122,5115";

var mp = $"Server={server};Database=MaterialProcessing;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=30;";
var prod = $"Server={server};Database=production;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=30;";
var despatch = $"Server={server};Database=Despatch;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=30;";

Console.WriteLine($"=== DEMO DATA CHECK: {company} ===\n");

await using (var cn = new SqlConnection(mp))
{
    await cn.OpenAsync();
    await Section(cn, "Pending PO approvers (ApprovePO)", @"SELECT TOP 10 ApprovalName, COUNT(*) Cnt FROM ApprovePO WHERE Status = 'Pending' GROUP BY ApprovalName ORDER BY COUNT(*) DESC", null);
    await Section(cn, "Total pending PO rows", @"SELECT COUNT(*) FROM ApprovePO WHERE Status = 'Pending'", null);
    await Section(cn, "PlanningFactoryConfig", "SELECT TOP 1 * FROM PlanningFactoryConfig WHERE CompanyName = @Co", new { Co = company });
    await Section(cn, "PlanningLineConfig count", "SELECT COUNT(*) FROM PlanningLineConfig WHERE CompanyName = @Co", new { Co = company });
    await Section(cn, "PlanningLoomPool", @"SELECT PoolPurpose, IncludeInPlanning, COUNT(*) Cnt FROM PlanningLoomPool WHERE CompanyName = @Co GROUP BY PoolPurpose, IncludeInPlanning", new { Co = company });
    await Section(cn, "PlanningBacklog (open)", @"SELECT TOP 5 [LineNo], [Shift], BacklogQty, Reason FROM PlanningBacklog WHERE CompanyName = @Co AND Status = 'Open'", new { Co = company });
    await Section(cn, "Team factors", @"SELECT TOP 10 [LineNo], [Shift], TeamNo, EffectiveFactor FROM PlanningTeamFactor WHERE CompanyName = @Co", new { Co = company });
    await Section(cn, "FIBC lines (ERP)", @"SELECT TOP 10 LNo, BagType, Bagcapacity FROM NewLineMaster WHERE CompanyName = @Co ORDER BY LNo", new { Co = company });
    await Section(cn, "Recent FIBC plans", @"SELECT TOP 10 orderno, COUNT(*) Rows, MIN(sysdate) FromDt, MAX(sysdate) ToDt FROM prod_fibcallocationMaster WHERE Companyname = @Co GROUP BY orderno ORDER BY MAX(sysdate) DESC", new { Co = company });
    await Section(cn, "Active looms count", @"SELECT COUNT(*) FROM NewMISLoomMaster WHERE CompanyName = @Co AND (isFreeze IS NULL OR UPPER(LTRIM(RTRIM(CAST(isFreeze AS NVARCHAR(20))))) IN ('0','N','NO','FALSE'))", new { Co = company });
    await Section(cn, "Recent loom plans", @"SELECT TOP 10 PONO, COUNT(*) Rows, MIN(AllocationDate) FromDt, MAX(ToDate) ToDt FROM Prod_LoomAlocationMaster WHERE PONO IS NOT NULL AND LTRIM(PONO) <> '' AND (isActive IS NULL OR UPPER(LTRIM(RTRIM(isActive))) <> 'N') GROUP BY PONO ORDER BY MAX(AllocationDate) DESC", null);
    await Section(cn, "FIBC grid company names", @"SELECT DISTINCT TOP 5 CompanyNam FROM vw_fibclineplanning_NEW ORDER BY CompanyNam", null);
    await Section(cn, "FIBC grid date range", @"SELECT MIN(sysdate) MinDt, MAX(sysdate) MaxDt, COUNT(*) Cnt FROM vw_fibclineplanning_NEW WHERE CompanyNam = @Co", new { Co = company });
    await Section(cn, "FIBC grid Aug 2026 sample", @"SELECT TOP 15 Linenos, sysdate, shift, capacity, remaining, orderno FROM vw_fibclineplanning_NEW WHERE CompanyNam = @Co AND sysdate >= '2026-08-01' AND sysdate < '2026-09-01' ORDER BY sysdate, Linenos", new { Co = company });
    await Section(cn, "FIBC slot grid sample (next 14d free-ish)", @"SELECT TOP 15 Linenos, sysdate, shift, capacity, remaining, orderno FROM vw_fibclineplanning_NEW WHERE CompanyNam = @Co AND sysdate >= CAST(GETDATE() AS DATE) AND sysdate < DATEADD(day, 14, CAST(GETDATE() AS DATE)) ORDER BY remaining DESC, sysdate", new { Co = company });
}

await using (var cn = new SqlConnection(despatch))
{
    await cn.OpenAsync();
    await Section(cn, "Marketing valid dispatch 2025+", @"SELECT TOP 20 BuyerOrderNo, BuyerName, DespatchDate, TotalQty, TypeofBag FROM MarketingInvoice WHERE DespatchDate >= '2025-01-01' AND DespatchDate < '2030-01-01' AND TotalQty > 0 AND BuyerOrderNo IS NOT NULL ORDER BY DespatchDate DESC", null);
    await Section(cn, "Marketing + BOM overlap", @"SELECT TOP 15 m.BuyerOrderNo, m.BuyerName, m.DespatchDate, m.TotalQty, b.TotalMtr, b.GSM, b.Heading FROM MarketingInvoice m INNER JOIN production.dbo.Vw_Bom_PPC b ON b.FilePONo = m.BuyerOrderNo WHERE m.DespatchDate >= '2025-06-01' AND m.DespatchDate < '2027-01-01' AND m.TotalQty > 100 AND b.Heading LIKE '%Body%' ORDER BY m.DespatchDate", null);
}

await using (var cn = new SqlConnection(prod))
{
    await cn.OpenAsync();
    await Section(cn, "BOM body fabric (meters>500)", @"SELECT TOP 20 FilePONo, Customer, Heading, GSM, FabricSize, TotalMtr, Targetdate FROM Vw_Bom_PPC WHERE Heading LIKE '%Body%' AND TotalMtr > 500 AND Targetdate >= '2025-01-01' AND Targetdate < '2027-01-01' ORDER BY TotalMtr DESC", null);
    await Section(cn, "Loom orders with body BOM", @"SELECT TOP 15 l.PONO, COUNT(DISTINCT l.LoomNo) Looms, MAX(b.TotalMtr) Mtr, MAX(b.GSM) GSM FROM MaterialProcessing.dbo.Prod_LoomAlocationMaster l INNER JOIN Vw_Bom_PPC b ON b.FilePONo = l.PONO AND b.Heading LIKE '%Body%' WHERE l.PONO <> ' Self' AND b.TotalMtr > 100 GROUP BY l.PONO ORDER BY MAX(l.AllocationDate) DESC", null);
}

Console.WriteLine("\n=== DEMO CANDIDATE ORDERS ===\n");
var candidates = new[] {
    "8500585065/157602", "PPL-66/2026/BIG BAGS/ITEM1", "8500597622/166691",
    "23129/PTS-RC", "8500596797/A-141364", "PO 9305/4LT-0775", "5491", "2026290"
};
foreach (var ord in candidates)
{
    Console.WriteLine($"===== {ord} =====");
    await using (var cn = new SqlConnection(mp))
    {
        await cn.OpenAsync();
        await Section(cn, "FIBC saved", @"SELECT COUNT(*) Cnt FROM prod_fibcallocationMaster WHERE orderno = @O", new { O = ord });
        await Section(cn, "Loom saved", @"SELECT COUNT(*) Cnt, MIN(AllocationDate) Fr, MAX(ToDate) ToDt FROM Prod_LoomAlocationMaster WHERE PONO = @O", new { O = ord });
    }
    await using (var cn = new SqlConnection(despatch))
    {
        await cn.OpenAsync();
        await Section(cn, "Marketing", @"SELECT TOP 1 BuyerName, DespatchDate, TotalQty, TypeofBag FROM MarketingInvoice WHERE BuyerOrderNo = @O", new { O = ord });
    }
    await using (var cn = new SqlConnection(prod))
    {
        await cn.OpenAsync();
        await Section(cn, "BOM body", @"SELECT TOP 1 Heading, GSM, FabricSize, TotalMtr, Targetdate FROM Vw_Bom_PPC WHERE FilePONo = @O AND Heading LIKE '%Body%'", new { O = ord });
    }
}

static async Task Section(SqlConnection cn, string title, string sql, object? param)
{
    Console.WriteLine($"--- {title} ---");
    try
    {
        var rows = (await cn.QueryAsync(sql, param)).ToList();
        if (rows.Count == 0) { Console.WriteLine("(no rows)\n"); return; }
        foreach (IDictionary<string, object> row in rows)
        {
            Console.WriteLine(string.Join(" | ", row.Select(kv => $"{kv.Key}={kv.Value}")));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
    Console.WriteLine();
}
