using Dapper;
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
    /// <summary>Extra ERP CompanyName spellings that roll into this PDF unit code.</summary>
    public string[] Aliases { get; set; } = [];
    /// <summary>
    /// When set, only bag types containing any of these keywords count for this unit
    /// (used to split HPBL-4 1L / HPBL-4 4L on PartyOrder / production rows).
    /// </summary>
    public string[] IncludeBagKeywords { get; set; } = [];
    /// <summary>
    /// PartyOrder = FIBCPartyOrdermaster Order−Despatch (official SP model).
    /// MarketingPending = Despatch.vw_PendingOrderStatus FIBC dept (KPW/HPBL have no PartyOrder).
    /// </summary>
    public string BalanceSource { get; set; } = "PartyOrder";
    /// <summary>Lookback days for MarketingPending (OrderDate &gt;= asOf − days).</summary>
    public int MarketingPendingDays { get; set; } = 50;
    /// <summary>Kg/pc used when MarketingPending has no net weight (PDF plant averages).</summary>
    public double DefaultKgPerPc { get; set; } = 2.55;
    /// <summary>
    /// For HPBL-4 1L/4L marketing rows (no bag-type on pending): share of Unit-IV qty (0–1).
    /// Remaining share goes to the sibling unit when both are configured.
    /// </summary>
    public double? MarketingQtyShare { get; set; }
    /// <summary>When true, PartyOrder rows need Ready or Despatch activity.</summary>
    public bool RequireActivity { get; set; }
    /// <summary>Drop untouched PartyOrder balances above this kg (mega-order filter).</summary>
    public double MegaOrderBalKg { get; set; } = 50_000;
    /// <summary>Optional PartyOrder Sysdate lookback (0 = no date filter).</summary>
    public int PartyOrderMaxAgeDays { get; set; }
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
        "Hybrid: PartyOrder Order−Despatch (OEL) + Despatch.vw_PendingOrderStatus FIBC (KPW/PIA/PIL/PPL/HPBL*)";
    public string Note { get; set; } =
        "PDF plants: KPW, OEL, PIA, PIL, PPL, HPBL-4 1L, HPBL-4 4L, HPBL, HPBL-5. OEL uses PartyOrder (base company only). Others use marketing FIBC pending with plant lookback/kg; HPBL-4 split by PDF qty share.";
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

    // Capacity / ₹/kg rates don't change with asOf — cache across date switches.
    private const string CapacityCacheKey = "order-book-summary-v3-capacity";
    private const string ValueCacheKey = "order-book-summary-v3-value";
    private const string MarketingBandCacheKey = "order-book-summary-v3-mkt-band";
    private static readonly TimeSpan SharedMetaTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan MarketingTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SummaryTtl = TimeSpan.FromMinutes(30);
    /// <summary>Extra days so past asOf dates can reuse one marketing fetch.</summary>
    private const int MarketingAsOfBufferDays = 90;

    public async Task<OrderBookSummaryDto> GetSummaryAsync(DateTime? asOf, bool refresh = false)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var cacheKey = $"order-book-summary-v3-hybrid:{asOfDate:yyyy-MM-dd}";
        if (!refresh && _cache.TryGetValue(cacheKey, out OrderBookSummaryDto? hit) && hit != null)
            return hit;

        var units = NormalizeUnits(_options.Units);
        if (units.Count == 0)
            throw new InvalidOperationException("OrderBookSummary:Units is empty in configuration.");

        var partyUnits = units
            .Where(u => !IsMarketingSource(u.BalanceSource))
            .ToList();
        var marketingUnits = units
            .Where(u => IsMarketingSource(u.BalanceSource))
            .ToList();

        var partyCompanyNames = ExpandCompanyNames(partyUnits.SelectMany(UnitCompanyNames));
        var allCompanyNames = ExpandCompanyNames(units.SelectMany(UnitCompanyNames));
        var marketingCompanyNames = ExpandCompanyNames(marketingUnits.SelectMany(UnitCompanyNames));
        var maxMktDays = marketingUnits.Count == 0
            ? 0
            : Math.Max(1, marketingUnits.Max(u => Math.Max(1, u.MarketingPendingDays)));

        var monthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var monthDays = DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month);
        var dayOfMonth = asOfDate.Day;

        // One marketing scan (same as before), cached as a wide band so other asOf dates reuse it.
        var marketingRows = await GetMarketingPendingBandAsync(
            asOfDate, maxMktDays, marketingCompanyNames, MarketingBandCacheKey, refresh);

        // Meta often cached; party/prod/planned are asOf-specific. Parallelize the light set.
        var capacityTask = GetCapacityRowsAsync(allCompanyNames, refresh);
        var valueTask = GetValueRowsAsync(allCompanyNames, refresh);
        var balanceTask = partyCompanyNames.Count == 0
            ? Task.FromResult(new List<BalanceRow>())
            : QueryPartyBalanceAsync(asOfDate, partyCompanyNames);
        var prodTask = QueryProductionAsync(asOfDate, monthStart, allCompanyNames);
        var plannedTask = QueryPlannedAsync(asOfDate, allCompanyNames);
        await Task.WhenAll(capacityTask, valueTask, balanceTask, prodTask, plannedTask);
        var capacityRows = await capacityTask;
        var valueRows = await valueTask;
        var balanceRows = await balanceTask;
        var prodRows = await prodTask;
        var plannedRows = await plannedTask;

        var unitBlocks = new List<OrderBookUnitBlockDto>();
        foreach (var unit in units)
        {
            var names = UnitCompanyNames(unit).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var lineMap = new Dictionary<string, OrderBookBagLineDto>(StringComparer.OrdinalIgnoreCase);
            double confirmMt = 0, openMt = 0, inHandMt = 0;
            var orderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (IsMarketingSource(unit.BalanceSource))
            {
                var days = Math.Max(1, unit.MarketingPendingDays);
                var cutoff = asOfDate.AddDays(-days);
                var kg = unit.DefaultKgPerPc > 0 ? unit.DefaultKgPerPc : 2.55;
                var share = unit.MarketingQtyShare is > 0 and <= 1 ? unit.MarketingQtyShare.Value : 1.0;

                var rows = marketingRows
                    .Where(r => names.Contains((r.CompanyName ?? "").Trim()))
                    .Where(r => r.OrderDate.Date >= cutoff && r.OrderDate.Date <= asOfDate)
                    .Where(r => r.PendingQty > 0)
                    .ToList();

                foreach (var row in rows)
                {
                    var balQty = row.PendingQty * share;
                    if (balQty <= 0) continue;
                    var balWtKg = balQty * kg;
                    orderKeys.Add(string.IsNullOrWhiteSpace(row.ItemNo) ? row.MarketingInvNo : row.ItemNo);

                    var bagGroup = GuessBagGroupFromMarketing(row.ItemDesc, row.Commodity, unit);
                    if (!lineMap.TryGetValue(bagGroup, out var line))
                    {
                        line = new OrderBookBagLineDto { BagGroup = bagGroup };
                        lineMap[bagGroup] = line;
                    }
                    line.BalQty += balQty;
                    line.BalWtMt += balWtKg / 1000.0;
                    // Marketing pending has no Open/Confirm flag — treat as confirm (issued book).
                    confirmMt += balWtKg / 1000.0;
                }
            }
            else
            {
                var maxAge = unit.PartyOrderMaxAgeDays;
                var megaKg = unit.MegaOrderBalKg > 0 ? unit.MegaOrderBalKg : 50_000;
                var rows = balanceRows
                    .Where(r => names.Contains((r.CompanyName ?? "").Trim()))
                    .Where(r => BagAllowedForUnit(unit, r.TypeofBag))
                    .Where(r => maxAge <= 0 || r.OrderDate.Date >= asOfDate.AddDays(-maxAge))
                    .ToList();

                foreach (var row in rows)
                {
                    var balQty = Math.Max(0, row.OrderQty - row.DespatchQty);
                    var balWtKg = Math.Max(0, row.OrderWt - row.DespatchWt);
                    if (balQty <= 0 && balWtKg <= 0)
                        continue;

                    var hasActivity = row.ReadyWt > 0 || row.DespatchWt > 0 || row.ReadyBags > 0 || row.DespatchQty > 0;
                    if (unit.RequireActivity && !hasActivity)
                        continue;
                    if (!hasActivity && balWtKg > megaKg)
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

                    if (IsOpenContainer(row.ContainerNo))
                        openMt += balWtKg / 1000.0;
                    else
                        confirmMt += balWtKg / 1000.0;

                    inHandMt += Math.Max(0, row.ReadyWt - row.DespatchWt) / 1000.0;
                }
            }

            var caps = capacityRows
                .Where(c => names.Contains((c.CompanyName ?? "").Trim()))
                .ToList();
            var unitCapMt = caps.Sum(c => c.QtyMt);
            if (unitCapMt > 1000) unitCapMt = unitCapMt / 1000.0;

            var prods = prodRows
                .Where(p => names.Contains((p.CompanyName ?? "").Trim()))
                .Where(p => BagAllowedForUnit(unit, p.TypeOfBag))
                .ToList();
            var todayWt = prods.Sum(p => p.TodayWt);
            var mtdWt = prods.Sum(p => p.MtdWt);
            var mtdPcs = prods.Sum(p => p.MtdPcs);
            var avgProd = dayOfMonth > 0 ? (mtdWt / 1000.0) / dayOfMonth : 0;
            var target = unitCapMt > 0 ? unitCapMt : avgProd;
            var expProd = target * dayOfMonth;
            var statusMt = (mtdWt / 1000.0) - expProd;
            var statusPct = expProd > 0 ? statusMt / expProd * 100.0 : 0;

            var plannedQty = plannedRows
                .Where(p => names.Contains((p.CompanyName ?? "").Trim()))
                .Where(p => BagAllowedForUnit(unit, p.BagType))
                .Sum(p => Math.Max(0, p.PlannedQty));
            var unitBalQty = lineMap.Values.Sum(l => l.BalQty);
            var unitBalMt = lineMap.Values.Sum(l => l.BalWtMt);
            var avgKg = unitBalQty > 0 ? (unitBalMt * 1000.0) / unitBalQty : unit.DefaultKgPerPc;
            if (avgKg <= 0) avgKg = 2.0;
            var plannedMt = Math.Max(0, plannedQty) * avgKg / 1000.0;

            foreach (var line in lineMap.Values)
            {
                var matchingCap = caps
                    .Where(c => BagGroupsOverlap(line.BagGroup, NormalizeBagGroup(c.TypeofBag)))
                    .Sum(c => c.QtyMt);
                if (matchingCap > 1000) matchingCap /= 1000.0;
                line.DeclCapacityMt = Math.Round(matchingCap, 2);
                line.DeclDays = matchingCap > 0 ? Math.Round(line.BalWtMt / matchingCap, 1) : 0;

                var matchingProd = prods
                    .Where(p => BagGroupsOverlap(line.BagGroup, NormalizeBagGroup(p.TypeOfBag)))
                    .Sum(p => p.MtdWt);
                line.ActProdMt = Math.Round(matchingProd / 1000.0, 2);
                line.ActDays = matchingProd > 0 && dayOfMonth > 0
                    ? Math.Round(line.BalWtMt / Math.Max(0.001, matchingProd / 1000.0 / dayOfMonth), 1)
                    : 0;
                line.Pct = line.DeclCapacityMt > 0
                    ? Math.Round(line.ActProdMt / (line.DeclCapacityMt * dayOfMonth) * 100.0, 0)
                    : 0;
                line.BalQty = Math.Round(line.BalQty, 0);
                line.BalWtMt = Math.Round(line.BalWtMt, 2);
            }

            var val = valueRows
                .Where(v => names.Contains((v.CompanyName ?? "").Trim()))
                .Aggregate((Amount: 0.0, NetWt: 0.0), (acc, v) => (acc.Amount + v.Amount, acc.NetWt + v.NetWt));
            var rsPerKg = val.NetWt > 0 ? val.Amount / val.NetWt : 0;
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

        _cache.Set(cacheKey, dto, SummaryTtl);
        return dto;
    }

    private static List<string> ExpandCompanyNames(IEnumerable<string> names) =>
        names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(n => new[] { n, n + " " })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<List<BalanceRow>> QueryPartyBalanceAsync(DateTime asOfDate, List<string> companyNames)
    {
        await using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<BalanceRow>(@"
SELECT
    LTRIM(RTRIM(m.CompanyName)) AS CompanyName,
    ISNULL(m.TypeofBag, '') AS TypeofBag,
    ISNULL(m.ContainerNo, '') AS ContainerNo,
    ISNULL(m.PONO, '') AS Pono,
    CAST(m.Sysdate AS datetime) AS OrderDate,
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
      AND LTRIM(RTRIM(CompanyName)) IN @CompanyNames
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
      AND LTRIM(RTRIM(CompanyName)) IN @CompanyNames
    GROUP BY LTRIM(RTRIM(PONO)), LTRIM(RTRIM(CompanyName)), LTRIM(RTRIM(PartyName))
) r
    ON LTRIM(RTRIM(m.PONO)) = r.PONO
   AND LTRIM(RTRIM(m.CompanyName)) = r.CompanyName
   AND LTRIM(RTRIM(m.PartyName)) = r.PartyName
WHERE ISNULL(m.isfreeze, 'no') = 'no'
  AND m.Sysdate <= @AsOf
  AND LTRIM(RTRIM(m.CompanyName)) IN @CompanyNames
", new { AsOf = asOfDate, CompanyNames = companyNames }, commandTimeout: 180);
        return rows.ToList();
    }

    private sealed class MarketingBandCache
    {
        public DateTime AnchorEnd { get; init; }
        public DateTime WindowStart { get; init; }
        public List<MarketingPendingRow> Rows { get; init; } = [];
    }

    /// <summary>
    /// Fetches a wide marketing pending band once (keyed by short/long), then filters to the requested asOf.
    /// Subsequent date changes reuse the band without re-scanning vw_PendingOrderStatus.
    /// </summary>
    private async Task<List<MarketingPendingRow>> GetMarketingPendingBandAsync(
        DateTime asOfDate,
        int unitMaxDays,
        List<string> companyNames,
        string cacheKey,
        bool refresh)
    {
        if (companyNames.Count == 0 || unitMaxDays <= 0)
            return [];

        var neededStart = asOfDate.AddDays(-unitMaxDays);
        if (!refresh
            && _cache.TryGetValue(cacheKey, out MarketingBandCache? band)
            && band != null
            && band.WindowStart <= neededStart
            && band.AnchorEnd >= asOfDate)
        {
            return band.Rows
                .Where(r => r.OrderDate.Date >= neededStart && r.OrderDate.Date <= asOfDate)
                .ToList();
        }

        var anchorEnd = asOfDate > DateTime.Today ? asOfDate : DateTime.Today;
        var pastSlack = Math.Max(0, (anchorEnd - asOfDate).Days);
        var fetchDays = unitMaxDays + pastSlack + MarketingAsOfBufferDays;
        var windowStart = anchorEnd.AddDays(-fetchDays);
        var rows = await QueryMarketingPendingAsync(anchorEnd, fetchDays, companyNames);
        _cache.Set(cacheKey, new MarketingBandCache
        {
            AnchorEnd = anchorEnd,
            WindowStart = windowStart,
            Rows = rows,
        }, MarketingTtl);

        return rows
            .Where(r => r.OrderDate.Date >= neededStart && r.OrderDate.Date <= asOfDate)
            .ToList();
    }

    private async Task<List<MarketingPendingRow>> QueryMarketingPendingAsync(
        DateTime asOfDate, int maxDays, List<string> companyNames)
    {
        await using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<MarketingPendingRow>(@"
SELECT
    LTRIM(RTRIM(Companyname)) AS CompanyName,
    ISNULL(ItemDesc, '') AS ItemDesc,
    ISNULL(Commodity, '') AS Commodity,
    ISNULL(MarketingInvNo, '') AS MarketingInvNo,
    ISNULL(ItemNO, '') AS ItemNo,
    CAST(ISNULL(PendingQty, 0) AS float) AS PendingQty,
    CAST(ISNULL(DespatchQty, 0) AS float) AS DespatchQty,
    CAST(OrderDate AS datetime) AS OrderDate
FROM Despatch.dbo.vw_PendingOrderStatus WITH (NOLOCK)
WHERE ISNULL(PendingQty, 0) > 0
  AND UPPER(LTRIM(RTRIM(ISNULL(Deptt, '')))) = 'FIBC'
  AND OrderDate >= DATEADD(day, -@MaxDays, @AsOf)
  AND OrderDate <= @AsOf
  AND LTRIM(RTRIM(Companyname)) IN @CompanyNames
", new { AsOf = asOfDate, MaxDays = maxDays, CompanyNames = companyNames }, commandTimeout: 180);
        return rows.ToList();
    }

    private async Task<List<CapacityRow>> GetCapacityRowsAsync(List<string> companyNames, bool refresh)
    {
        if (!refresh && _cache.TryGetValue(CapacityCacheKey, out List<CapacityRow>? cached) && cached != null)
            return cached;

        await using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<CapacityRow>(@"
SELECT
    LTRIM(RTRIM(CompanyName)) AS CompanyName,
    ISNULL(TypeofBag, '') AS TypeofBag,
    CAST(ISNULL(Qty, 0) AS float) AS QtyMt
FROM Despatch.dbo.FIBCCapacityMaster WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) IN @CompanyNames
", new { CompanyNames = companyNames }, commandTimeout: 60)).ToList();
        _cache.Set(CapacityCacheKey, rows, SharedMetaTtl);
        return rows;
    }

    private async Task<List<ProdRow>> QueryProductionAsync(
        DateTime asOfDate, DateTime monthStart, List<string> companyNames)
    {
        await using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<ProdRow>(@"
SELECT
    LTRIM(RTRIM(CompanyName)) AS CompanyName,
    ISNULL(TYPEOFBAG, '') AS TypeOfBag,
    CAST(SUM(CASE WHEN Sysdate >= @AsOf AND Sysdate < DATEADD(day, 1, @AsOf)
        THEN ISNULL(BagPCS, 0) ELSE 0 END) AS float) AS TodayPcs,
    CAST(SUM(CASE WHEN Sysdate >= @AsOf AND Sysdate < DATEADD(day, 1, @AsOf)
        THEN ISNULL(BagWt, 0) ELSE 0 END) AS float) AS TodayWt,
    CAST(SUM(ISNULL(BagPCS, 0)) AS float) AS MtdPcs,
    CAST(SUM(ISNULL(BagWt, 0)) AS float) AS MtdWt
FROM dbo.VW_FIBCBagwiseProduction WITH (NOLOCK)
WHERE Sysdate >= @MonthStart AND Sysdate < DATEADD(day, 1, @AsOf)
  AND LTRIM(RTRIM(CompanyName)) IN @CompanyNames
GROUP BY LTRIM(RTRIM(CompanyName)), ISNULL(TYPEOFBAG, '')
", new { AsOf = asOfDate, MonthStart = monthStart, CompanyNames = companyNames }, commandTimeout: 120);
        return rows.ToList();
    }

    private async Task<List<PlannedRow>> QueryPlannedAsync(DateTime asOfDate, List<string> companyNames)
    {
        try
        {
            await using var connection = _database.CreateConnection();
            var rows = await connection.QueryAsync<PlannedRow>(@"
SELECT
    LTRIM(RTRIM(Companyname)) AS CompanyName,
    ISNULL(BagType, '') AS BagType,
    CAST(SUM(ISNULL(poqty, qty)) AS float) AS PlannedQty
FROM dbo.VW_MarketingLinePlanning WITH (NOLOCK)
WHERE LTRIM(RTRIM(Companyname)) IN @CompanyNames
  AND (startdate IS NULL OR startdate <= DATEADD(day, 45, @AsOf))
  AND (CompletionDate IS NULL OR CompletionDate >= DATEADD(day, -7, @AsOf))
GROUP BY LTRIM(RTRIM(Companyname)), ISNULL(BagType, '')
", new { AsOf = asOfDate, CompanyNames = companyNames }, commandTimeout: 60);
            return rows.ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<ValueRow>> GetValueRowsAsync(List<string> companyNames, bool refresh)
    {
        if (!refresh && _cache.TryGetValue(ValueCacheKey, out List<ValueRow>? cached) && cached != null)
            return cached;

        try
        {
            await using var connection = _database.CreateConnection();
            var rows = (await connection.QueryAsync<ValueRow>(@"
SELECT
    LTRIM(RTRIM(i.ProductionCompanyName)) AS CompanyName,
    CAST(SUM(ISNULL(i.Amount, 0)) AS float) AS Amount,
    CAST(SUM(CASE WHEN ISNULL(i.netwt, 0) > 0 THEN i.netwt ELSE 0 END) AS float) AS NetWt
FROM Despatch.dbo.MarketingInvItem i WITH (NOLOCK)
WHERE LTRIM(RTRIM(ISNULL(i.ProductionCompanyName, ''))) IN @CompanyNames
GROUP BY LTRIM(RTRIM(i.ProductionCompanyName))
", new { CompanyNames = companyNames }, commandTimeout: 120)).ToList();
            _cache.Set(ValueCacheKey, rows, SharedMetaTtl);
            return rows;
        }
        catch
        {
            return [];
        }
    }

    private static List<OrderBookUnitOption> NormalizeUnits(List<OrderBookUnitOption> raw) =>
        raw
            .Where(u => !string.IsNullOrWhiteSpace(u.CompanyName) && !string.IsNullOrWhiteSpace(u.Code))
            .Select(u => new OrderBookUnitOption
            {
                Code = u.Code.Trim(),
                CompanyName = u.CompanyName.Trim(),
                Aliases = (u.Aliases ?? [])
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .ToArray(),
                IncludeBagKeywords = (u.IncludeBagKeywords ?? [])
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .ToArray(),
                BalanceSource = string.IsNullOrWhiteSpace(u.BalanceSource) ? "PartyOrder" : u.BalanceSource.Trim(),
                MarketingPendingDays = u.MarketingPendingDays > 0 ? u.MarketingPendingDays : 50,
                DefaultKgPerPc = u.DefaultKgPerPc > 0 ? u.DefaultKgPerPc : 2.55,
                MarketingQtyShare = u.MarketingQtyShare,
                RequireActivity = u.RequireActivity,
                MegaOrderBalKg = u.MegaOrderBalKg > 0 ? u.MegaOrderBalKg : 50_000,
                PartyOrderMaxAgeDays = u.PartyOrderMaxAgeDays,
                Sort = u.Sort,
            })
            .OrderBy(u => u.Sort)
            .ThenBy(u => u.Code)
            .ToList();

    private static bool IsMarketingSource(string? source) =>
        string.Equals((source ?? "").Trim(), "MarketingPending", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> UnitCompanyNames(OrderBookUnitOption unit)
    {
        yield return unit.CompanyName.Trim();
        foreach (var a in unit.Aliases ?? [])
        {
            if (!string.IsNullOrWhiteSpace(a))
                yield return a.Trim();
        }
    }

    private static bool BagAllowedForUnit(OrderBookUnitOption unit, string? typeofBag)
    {
        var keys = unit.IncludeBagKeywords ?? [];
        if (keys.Length == 0) return true;
        var bag = (typeofBag ?? "").ToUpperInvariant();
        return keys.Any(k => bag.Contains(k.Trim().ToUpperInvariant(), StringComparison.Ordinal));
    }

    private static string GuessBagGroupFromMarketing(string? itemDesc, string? commodity, OrderBookUnitOption unit)
    {
        if (unit.IncludeBagKeywords?.Any(k =>
                k.Contains("LOOP", StringComparison.OrdinalIgnoreCase) == true) == true)
            return "ONE LOOP/TWO LOOPS";
        if (unit.IncludeBagKeywords?.Any(k =>
                k.Contains("BUILDER", StringComparison.OrdinalIgnoreCase)
                || k.Contains("TUNNEL", StringComparison.OrdinalIgnoreCase)) == true)
        {
            var blob = $"{itemDesc} {commodity}".ToUpperInvariant();
            if (blob.Contains("TUNNEL")) return "TUNNEL";
            if (blob.Contains("BUILDER")) return "BUILDER";
            return "BUILDER";
        }
        return NormalizeBagGroup($"{itemDesc} {commodity}");
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
        public DateTime OrderDate { get; set; }
        public double OrderQty { get; set; }
        public double OrderWt { get; set; }
        public double DespatchQty { get; set; }
        public double DespatchWt { get; set; }
        public double ReadyBags { get; set; }
        public double ReadyWt { get; set; }
    }

    private sealed class MarketingPendingRow
    {
        public string CompanyName { get; set; } = "";
        public string ItemDesc { get; set; } = "";
        public string Commodity { get; set; } = "";
        public string MarketingInvNo { get; set; } = "";
        public string ItemNo { get; set; } = "";
        public double PendingQty { get; set; }
        public double DespatchQty { get; set; }
        public DateTime OrderDate { get; set; }
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
