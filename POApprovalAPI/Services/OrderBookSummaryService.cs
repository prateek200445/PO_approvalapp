using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace POApprovalAPI.Services;

public sealed class OrderBookSummaryOptions
{
    public const string SectionName = "OrderBookSummary";
    public bool Enabled { get; set; } = true;
    public string[] AllowedUsers { get; set; } = [];
    public List<OrderBookUnitOption> Units { get; set; } = [];
}

public sealed class OrderBookUnitOption
{
    public string Code { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public int Sort { get; set; }
}

public sealed class OrderBookAccessDto
{
    public bool Enabled { get; set; }
    public bool Allowed { get; set; }
    public string Username { get; set; } = "";
}

public sealed class OrderBookBagLineDto
{
    public string BagGroup { get; set; } = "";
    public double BalQty { get; set; }
    public double BalWtMt { get; set; }
    public double DeclCapacityMt { get; set; }
    public double DeclDays { get; set; }
    public double ActProdMt { get; set; }
    public double ActDays { get; set; }
    public double Pct { get; set; }
}

public sealed class OrderBookUnitBlockDto
{
    public string UnitCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public double BalQty { get; set; }
    public double BalWtMt { get; set; }
    public double ConfirmMt { get; set; }
    public double OpenMt { get; set; }
    public double PlannedMt { get; set; }
    public int OrderCount { get; set; }
    public double FgQty { get; set; }
    public double FgWtMt { get; set; }
    public double TodayProdMt { get; set; }
    public double ToDateProdMt { get; set; }
    public double AvgProdMt { get; set; }
    public double TargetMt { get; set; }
    public double ExpProdMt { get; set; }
    public double StatusMt { get; set; }
    public double StatusPct { get; set; }
    public double ValueInr { get; set; }
    public double RsPerKg { get; set; }
    public List<OrderBookBagLineDto> Lines { get; set; } = [];
}

public sealed class OrderBookSummaryDto
{
    public string AsOfDate { get; set; } = "";
    public int DayOfMonth { get; set; }
    public int MonthDays { get; set; }
    public string MonthLabel { get; set; } = "";
    public double IssuedMt { get; set; }
    public double InHandHoldMt { get; set; }
    public double PendEntryMt { get; set; }
    public double FinalPendMt { get; set; }
    public double ConfirmMt { get; set; }
    public double OpenMt { get; set; }
    public double PlannedMt { get; set; }
    public double ConfirmPct { get; set; }
    public double OpenPct { get; set; }
    public double PlannedPct { get; set; }
    public double TotalOrderWtMt { get; set; }
    public double TotalValueInr { get; set; }
    public double AvgRsPerKg { get; set; }
    public double OrdBookDaysAvg { get; set; }
    public double OrdBookDaysCurr { get; set; }
    public int TotalOrders { get; set; }
    public string Source { get; set; } =
        "MaterialProcessing.FIBCPartyOrdermaster + FIBCDespatch + VW_FIBCBagwiseProduction + Despatch.FIBCCapacityMaster";
    public string Note { get; set; } =
        "Balance = Order − Despatch (positive). Open ≈ ContainerNo OPEN/OPN. Confirm = other issued balance. Planned ≈ VW_MarketingLinePlanning. Sample PDF codes KPW/HPBL* are not company names in FIBCPartyOrdermaster — map them in OrderBookSummary:Units when known. Add users via AllowedUsers (appsettings) and ORDER_BOOK_SUMMARY_ALLOWED_USERS (frontend).";
    public List<OrderBookUnitBlockDto> Units { get; set; } = [];
}

public sealed class OrderBookSummaryService
{
    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private readonly OrderBookSummaryOptions _options;

    public OrderBookSummaryService(
        DatabaseService database,
        IMemoryCache cache,
        IOptions<OrderBookSummaryOptions> options)
    {
        _database = database;
        _cache = cache;
        _options = options.Value;
    }

    public OrderBookAccessDto CheckAccess(string? username)
    {
        var user = (username ?? "").Trim();
        var allowed = _options.Enabled
            && user.Length > 0
            && _options.AllowedUsers.Any(u =>
                string.Equals(u?.Trim(), user, StringComparison.OrdinalIgnoreCase));
        return new OrderBookAccessDto
        {
            Enabled = _options.Enabled,
            Allowed = allowed,
            Username = user,
        };
    }

    public async Task<OrderBookSummaryDto> GetSummaryAsync(DateTime? asOf, bool refresh = false)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var cacheKey = $"order-book-summary-v1:{asOfDate:yyyy-MM-dd}";
        if (!refresh && _cache.TryGetValue(cacheKey, out OrderBookSummaryDto? hit) && hit != null)
            return hit;

        var units = _options.Units
            .Where(u => !string.IsNullOrWhiteSpace(u.CompanyName) && !string.IsNullOrWhiteSpace(u.Code))
            .Select(u => new OrderBookUnitOption
            {
                Code = u.Code.Trim(),
                CompanyName = u.CompanyName.Trim(),
                Sort = u.Sort,
            })
            .OrderBy(u => u.Sort)
            .ThenBy(u => u.Code)
            .ToList();

        if (units.Count == 0)
            throw new InvalidOperationException("OrderBookSummary:Units is empty in configuration.");

        var companyNames = units.Select(u => u.CompanyName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        // ERP often stores "Plastene India Limited " with a trailing space — include both forms.
        var companyNamesForSql = companyNames
            .SelectMany(n => new[] { n, n + " " })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var monthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var monthDays = DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month);
        var dayOfMonth = asOfDate.Day;

        using var connection = _database.CreateConnection();

        var balanceRows = (await connection.QueryAsync<BalanceRow>(@"
SELECT
    LTRIM(RTRIM(m.CompanyName)) AS CompanyName,
    ISNULL(m.TypeofBag, '') AS TypeofBag,
    ISNULL(m.ContainerNo, '') AS ContainerNo,
    ISNULL(m.PONO, '') AS Pono,
    CAST(ISNULL(m.Quantity, 0) AS float) AS OrderQty,
    CAST(ISNULL(m.TotalWt, 0) AS float) AS OrderWt,
    CAST(ISNULL(d.DespatchQty, 0) AS float) AS DespatchQty,
    CAST(ISNULL(d.DespatchWt, 0) AS float) AS DespatchWt,
    CAST(ISNULL(r.ReadyBags, 0) AS float) AS ReadyBags,
    CAST(ISNULL(r.ReadyWt, 0) AS float) AS ReadyWt
FROM dbo.FIBCPartyOrdermaster m WITH (NOLOCK)
LEFT JOIN (
    SELECT
        LTRIM(RTRIM(PONO)) AS PONO,
        LTRIM(RTRIM(CompanyName)) AS CompanyName,
        LTRIM(RTRIM(PartyName)) AS PartyName,
        SUM(CAST(ISNULL(Qty, 0) AS float)) AS DespatchQty,
        SUM(CAST(ISNULL(Wt, 0) AS float)) AS DespatchWt
    FROM dbo.FIBCDespatch WITH (NOLOCK)
    WHERE Sysdate <= @AsOf
    GROUP BY LTRIM(RTRIM(PONO)), LTRIM(RTRIM(CompanyName)), LTRIM(RTRIM(PartyName))
) d
    ON LTRIM(RTRIM(m.PONO)) = d.PONO
   AND LTRIM(RTRIM(m.CompanyName)) = d.CompanyName
   AND LTRIM(RTRIM(m.PartyName)) = d.PartyName
LEFT JOIN (
    SELECT
        LTRIM(RTRIM(PONO)) AS PONO,
        LTRIM(RTRIM(CompanyName)) AS CompanyName,
        LTRIM(RTRIM(PartyName)) AS PartyName,
        SUM(CAST(ISNULL(BagPCS, 0) AS float)) AS ReadyBags,
        SUM(CAST(ISNULL(BagWt, 0) AS float)) AS ReadyWt
    FROM dbo.FIBCTeamWiseProduction WITH (NOLOCK)
    WHERE Sysdate <= @AsOf
    GROUP BY LTRIM(RTRIM(PONO)), LTRIM(RTRIM(CompanyName)), LTRIM(RTRIM(PartyName))
) r
    ON LTRIM(RTRIM(m.PONO)) = r.PONO
   AND LTRIM(RTRIM(m.CompanyName)) = r.CompanyName
   AND LTRIM(RTRIM(m.PartyName)) = r.PartyName
WHERE ISNULL(m.isfreeze, 'no') = 'no'
  AND m.Sysdate <= @AsOf
  AND LTRIM(RTRIM(m.CompanyName)) IN @CompanyNames
", new { AsOf = asOfDate, CompanyNames = companyNamesForSql }, commandTimeout: 180)).ToList();

        var capacityRows = (await connection.QueryAsync<CapacityRow>(@"
SELECT
    LTRIM(RTRIM(CompanyName)) AS CompanyName,
    ISNULL(TypeofBag, '') AS TypeofBag,
    CAST(ISNULL(Qty, 0) AS float) AS QtyMt
FROM Despatch.dbo.FIBCCapacityMaster WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) IN @CompanyNames
", new { CompanyNames = companyNamesForSql }, commandTimeout: 60)).ToList();

        var prodRows = (await connection.QueryAsync<ProdRow>(@"
SELECT
    LTRIM(RTRIM(CompanyName)) AS CompanyName,
    ISNULL(TYPEOFBAG, '') AS TypeOfBag,
    CAST(SUM(CASE WHEN CONVERT(date, Sysdate) = @AsOf THEN ISNULL(BagPCS, 0) ELSE 0 END) AS float) AS TodayPcs,
    CAST(SUM(CASE WHEN CONVERT(date, Sysdate) = @AsOf THEN ISNULL(BagWt, 0) ELSE 0 END) AS float) AS TodayWt,
    CAST(SUM(ISNULL(BagPCS, 0)) AS float) AS MtdPcs,
    CAST(SUM(ISNULL(BagWt, 0)) AS float) AS MtdWt
FROM dbo.VW_FIBCBagwiseProduction WITH (NOLOCK)
WHERE Sysdate >= @MonthStart AND Sysdate < DATEADD(day, 1, @AsOf)
  AND LTRIM(RTRIM(CompanyName)) IN @CompanyNames
GROUP BY LTRIM(RTRIM(CompanyName)), ISNULL(TYPEOFBAG, '')
", new { AsOf = asOfDate, MonthStart = monthStart, CompanyNames = companyNamesForSql }, commandTimeout: 120)).ToList();

        List<PlannedRow> plannedRows;
        try
        {
            plannedRows = (await connection.QueryAsync<PlannedRow>(@"
SELECT
    LTRIM(RTRIM(Companyname)) AS CompanyName,
    ISNULL(BagType, '') AS BagType,
    CAST(SUM(ISNULL(poqty, qty)) AS float) AS PlannedQty
FROM dbo.VW_MarketingLinePlanning WITH (NOLOCK)
WHERE LTRIM(RTRIM(Companyname)) IN @CompanyNames
  AND (startdate IS NULL OR startdate <= DATEADD(day, 45, @AsOf))
  AND (CompletionDate IS NULL OR CompletionDate >= DATEADD(day, -7, @AsOf))
GROUP BY LTRIM(RTRIM(Companyname)), ISNULL(BagType, '')
", new { AsOf = asOfDate, CompanyNames = companyNamesForSql }, commandTimeout: 60)).ToList();
        }
        catch
        {
            plannedRows = [];
        }

        List<ValueRow> valueRows;
        try
        {
            valueRows = (await connection.QueryAsync<ValueRow>(@"
SELECT
    LTRIM(RTRIM(i.ProductionCompanyName)) AS CompanyName,
    CAST(SUM(ISNULL(i.Amount, 0)) AS float) AS Amount,
    CAST(SUM(CASE WHEN ISNULL(i.netwt, 0) > 0 THEN i.netwt ELSE 0 END) AS float) AS NetWt
FROM Despatch.dbo.MarketingInvItem i WITH (NOLOCK)
WHERE LTRIM(RTRIM(ISNULL(i.ProductionCompanyName, ''))) IN @CompanyNames
GROUP BY LTRIM(RTRIM(i.ProductionCompanyName))
", new { CompanyNames = companyNamesForSql }, commandTimeout: 120)).ToList();
        }
        catch
        {
            valueRows = [];
        }

        var capacityByCompany = capacityRows
            .GroupBy(c => c.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var prodByCompany = prodRows
            .GroupBy(p => p.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var plannedByCompany = plannedRows
            .GroupBy(p => p.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0, x.PlannedQty)), StringComparer.OrdinalIgnoreCase);
        var valueByCompany = valueRows
            .GroupBy(v => v.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (Amount: g.Sum(x => x.Amount), NetWt: g.Sum(x => x.NetWt)),
                StringComparer.OrdinalIgnoreCase);

        var unitBlocks = new List<OrderBookUnitBlockDto>();
        foreach (var unit in units)
        {
            var rows = balanceRows
                .Where(r => string.Equals(r.CompanyName, unit.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            var lineMap = new Dictionary<string, OrderBookBagLineDto>(StringComparer.OrdinalIgnoreCase);
            double confirmMt = 0, openMt = 0, inHandMt = 0;
            var orderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var balQty = Math.Max(0, row.OrderQty - row.DespatchQty);
                var balWtKg = Math.Max(0, row.OrderWt - row.DespatchWt);
                if (balQty <= 0 && balWtKg <= 0)
                    continue;
                // Drop never-touched mega-orders that skew plant totals (no ready/despatch activity).
                if (row.ReadyWt <= 0 && row.DespatchWt <= 0 && balWtKg > 50_000)
                    continue;

                orderKeys.Add(row.Pono);
                var bagGroup = NormalizeBagGroup(row.TypeofBag);
                if (!lineMap.TryGetValue(bagGroup, out var line))
                {
                    line = new OrderBookBagLineDto { BagGroup = bagGroup };
                    lineMap[bagGroup] = line;
                }

                line.BalQty += balQty;
                line.BalWtMt += balWtKg / 1000.0;

                var open = IsOpenContainer(row.ContainerNo);
                if (open)
                    openMt += balWtKg / 1000.0;
                else
                    confirmMt += balWtKg / 1000.0;

                var stockKg = Math.Max(0, row.ReadyWt - row.DespatchWt);
                inHandMt += stockKg / 1000.0;
            }

            capacityByCompany.TryGetValue(unit.CompanyName.Trim(), out var caps);
            var unitCapMt = caps?.Sum(c => c.QtyMt) ?? 0;
            if (unitCapMt > 1000) unitCapMt = unitCapMt / 1000.0; // some master rows store PCS-scale

            prodByCompany.TryGetValue(unit.CompanyName.Trim(), out var prods);
            var todayWt = prods?.Sum(p => p.TodayWt) ?? 0;
            var mtdWt = prods?.Sum(p => p.MtdWt) ?? 0;
            var mtdPcs = prods?.Sum(p => p.MtdPcs) ?? 0;
            var avgProd = dayOfMonth > 0 ? (mtdWt / 1000.0) / dayOfMonth : 0;
            var target = unitCapMt > 0 ? unitCapMt : avgProd;
            var expProd = target * dayOfMonth;
            var statusMt = (mtdWt / 1000.0) - expProd;
            var statusPct = expProd > 0 ? statusMt / expProd * 100.0 : 0;

            plannedByCompany.TryGetValue(unit.CompanyName.Trim(), out var plannedQty);
            // Planned weight estimate: use average kg/bag from balances when available
            var unitBalQty = lineMap.Values.Sum(l => l.BalQty);
            var unitBalMt = lineMap.Values.Sum(l => l.BalWtMt);
            var avgKg = unitBalQty > 0 ? (unitBalMt * 1000.0) / unitBalQty : 2.0;
            var plannedMt = Math.Max(0, plannedQty) * avgKg / 1000.0;

            foreach (var line in lineMap.Values)
            {
                var matchingCap = caps?
                    .Where(c => BagGroupsOverlap(line.BagGroup, NormalizeBagGroup(c.TypeofBag)))
                    .Sum(c => c.QtyMt) ?? 0;
                if (matchingCap > 1000) matchingCap /= 1000.0;
                line.DeclCapacityMt = Math.Round(matchingCap, 2);
                line.DeclDays = matchingCap > 0 ? Math.Round(line.BalWtMt / matchingCap, 1) : 0;

                var matchingProd = prods?
                    .Where(p => BagGroupsOverlap(line.BagGroup, NormalizeBagGroup(p.TypeOfBag)))
                    .Sum(p => p.MtdWt) ?? 0;
                line.ActProdMt = Math.Round(matchingProd / 1000.0, 2);
                line.ActDays = matchingProd > 0 && dayOfMonth > 0
                    ? Math.Round((line.BalWtMt) / Math.Max(0.001, matchingProd / 1000.0 / dayOfMonth), 1)
                    : 0;
                line.Pct = line.DeclCapacityMt > 0
                    ? Math.Round(line.ActProdMt / (line.DeclCapacityMt * dayOfMonth) * 100.0, 0)
                    : 0;
                line.BalQty = Math.Round(line.BalQty, 0);
                line.BalWtMt = Math.Round(line.BalWtMt, 2);
            }

            valueByCompany.TryGetValue(unit.CompanyName.Trim(), out var val);
            var rsPerKg = val.NetWt > 0 ? val.Amount / val.NetWt : 0;
            // Estimate open order book value from balance MT × recent Rs/Kg
            var valueInr = unitBalMt * 1000.0 * rsPerKg;

            unitBlocks.Add(new OrderBookUnitBlockDto
            {
                UnitCode = unit.Code,
                CompanyName = unit.CompanyName,
                BalQty = Math.Round(unitBalQty, 0),
                BalWtMt = Math.Round(unitBalMt, 2),
                ConfirmMt = Math.Round(confirmMt, 2),
                OpenMt = Math.Round(openMt, 2),
                PlannedMt = Math.Round(plannedMt, 2),
                OrderCount = orderKeys.Count,
                FgQty = Math.Round(mtdPcs, 0),
                FgWtMt = Math.Round(mtdWt / 1000.0, 2),
                TodayProdMt = Math.Round(todayWt / 1000.0, 2),
                ToDateProdMt = Math.Round(mtdWt / 1000.0, 2),
                AvgProdMt = Math.Round(avgProd, 2),
                TargetMt = Math.Round(target, 2),
                ExpProdMt = Math.Round(expProd, 2),
                StatusMt = Math.Round(statusMt, 2),
                StatusPct = Math.Round(statusPct, 0),
                ValueInr = Math.Round(valueInr, 0),
                RsPerKg = Math.Round(rsPerKg, 0),
                Lines = lineMap.Values.OrderByDescending(l => l.BalWtMt).ToList(),
            });
        }

        var issuedMt = unitBlocks.Sum(u => u.BalWtMt);
        var confirmMtAll = unitBlocks.Sum(u => u.ConfirmMt);
        var openMtAll = unitBlocks.Sum(u => u.OpenMt);
        var plannedMtAll = unitBlocks.Sum(u => u.PlannedMt);
        var inHandAll = balanceRows.Sum(r => Math.Max(0, r.ReadyWt - r.DespatchWt) / 1000.0);
        // Pending entry ≈ planned not yet on issued book (rough)
        var pendEntry = Math.Max(0, plannedMtAll);
        var finalPend = issuedMt + inHandAll + pendEntry;
        var totalBucket = confirmMtAll + openMtAll + plannedMtAll;
        var totalValue = unitBlocks.Sum(u => u.ValueInr);
        var totalWtForRate = unitBlocks.Sum(u => u.BalWtMt);
        var avgProdAll = unitBlocks.Sum(u => u.AvgProdMt);
        var currProdAll = unitBlocks.Sum(u => u.TodayProdMt);

        var dto = new OrderBookSummaryDto
        {
            AsOfDate = asOfDate.ToString("yyyy-MM-dd"),
            DayOfMonth = dayOfMonth,
            MonthDays = monthDays,
            MonthLabel = asOfDate.ToString("MMMM"),
            IssuedMt = Math.Round(issuedMt, 2),
            InHandHoldMt = Math.Round(inHandAll, 2),
            PendEntryMt = Math.Round(pendEntry, 2),
            FinalPendMt = Math.Round(finalPend, 2),
            ConfirmMt = Math.Round(confirmMtAll, 2),
            OpenMt = Math.Round(openMtAll, 2),
            PlannedMt = Math.Round(plannedMtAll, 2),
            ConfirmPct = Share(confirmMtAll, totalBucket),
            OpenPct = Share(openMtAll, totalBucket),
            PlannedPct = Share(plannedMtAll, totalBucket),
            TotalOrderWtMt = Math.Round(totalBucket, 2),
            TotalValueInr = Math.Round(totalValue, 0),
            AvgRsPerKg = totalWtForRate > 0 ? Math.Round(totalValue / (totalWtForRate * 1000.0), 0) : 0,
            OrdBookDaysAvg = avgProdAll > 0 ? Math.Round(issuedMt / avgProdAll, 0) : 0,
            OrdBookDaysCurr = currProdAll > 0 ? Math.Round(issuedMt / currProdAll, 0) : 0,
            TotalOrders = unitBlocks.Sum(u => u.OrderCount),
            Units = unitBlocks,
        };

        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(30));
        return dto;
    }

    private static double Share(double part, double total) =>
        total > 0 ? Math.Round(part / total * 100.0, 0) : 0;

    private static bool IsOpenContainer(string? container)
    {
        var c = (container ?? "").Trim().ToUpperInvariant();
        return c is "OPEN" or "OPN" or "O";
    }

    internal static string NormalizeBagGroup(string? raw)
    {
        var t = (raw ?? "").Trim().ToUpperInvariant();
        if (t.Length == 0) return "OTHER";
        if (t.Contains("SLING")) return "SLING BAG";
        if (t.Contains("FUSION")) return "FUSION";
        if (t.Contains("TUNNEL")) return "TUNNEL";
        if (t.Contains("BUILDER")) return "BUILDER";
        if (t.Contains("Q") && t.Contains("BAG"))
            return t.Contains('X') ? "Q-BAG/X-CORNER" : "Q-BAG/CORNER";
        if (t.Contains("CIRCULAR") || t is "XC")
            return "CIRCULAR/UPANEL/4 PANEL/X-CORNER";
        if (t.Contains("LOOP") || t.Contains("ONE LOOP") || t.Contains("TWO LOOP") || t.Contains("SINGLE"))
            return "ONE LOOP/TWO LOOPS";
        if (t.Contains("U+") || t.Contains("U +") || t.Contains("UPANEL") || t.Contains("U PANEL")
            || t.Contains("4 PANEL") || t.Contains("4-PANEL") || t.Contains("4P") || t.Contains("BAFFLE")
            || t.Contains("BUFFLE") || t is "QB" || t is "U+CB" || t is "B/B")
            return "UPANEL/4 PANEL/CORNER LOOPS";
        return t.Length > 40 ? t[..40] : t;
    }

    private static bool BagGroupsOverlap(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        var x = a.ToUpperInvariant();
        var y = b.ToUpperInvariant();
        if (x.Contains("CIRCULAR") && y.Contains("CIRCULAR")) return true;
        if (x.Contains("Q-BAG") && (y.Contains("Q") && y.Contains("BAG"))) return true;
        if (x.Contains("UPANEL") && (y.Contains("U PANEL") || y.Contains("UPANEL") || y.Contains("4 PANEL") || y.Contains("BAFFLE")))
            return true;
        if (x.Contains("LOOP") && y.Contains("LOOP")) return true;
        return false;
    }

    private sealed class BalanceRow
    {
        public string CompanyName { get; set; } = "";
        public string TypeofBag { get; set; } = "";
        public string ContainerNo { get; set; } = "";
        public string Pono { get; set; } = "";
        public double OrderQty { get; set; }
        public double OrderWt { get; set; }
        public double DespatchQty { get; set; }
        public double DespatchWt { get; set; }
        public double ReadyBags { get; set; }
        public double ReadyWt { get; set; }
    }

    private sealed class CapacityRow
    {
        public string CompanyName { get; set; } = "";
        public string TypeofBag { get; set; } = "";
        public double QtyMt { get; set; }
    }

    private sealed class ProdRow
    {
        public string CompanyName { get; set; } = "";
        public string TypeOfBag { get; set; } = "";
        public double TodayPcs { get; set; }
        public double TodayWt { get; set; }
        public double MtdPcs { get; set; }
        public double MtdWt { get; set; }
    }

    private sealed class PlannedRow
    {
        public string CompanyName { get; set; } = "";
        public string BagType { get; set; } = "";
        public double PlannedQty { get; set; }
    }

    private sealed class ValueRow
    {
        public string CompanyName { get; set; } = "";
        public double Amount { get; set; }
        public double NetWt { get; set; }
    }
}
