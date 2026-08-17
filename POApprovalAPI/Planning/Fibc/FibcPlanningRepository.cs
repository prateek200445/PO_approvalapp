using Dapper;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcPlanningRepository : IFibcPlanningRepository
{
    private const int CommandTimeoutSeconds = 120;

    private readonly DatabaseService _database;
    private readonly FibcPlanningOptions _options;

    public FibcPlanningRepository(DatabaseService database, IOptions<FibcPlanningOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<FibcLineConfigDto>> GetLineConfigAsync(
        string? companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = ResolveCompany(companyName);

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<LineConfigRow>(@"
SELECT
    TransId,
    CompanyName,
    LNo,
    BagType,
    IsDoubleDust,
    IsTripleDust,
    Bagcapacity,
    SOrderno,
    NoOfDaysChk
FROM NewLineMaster WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY LNo, SOrderno", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapLineConfig).ToList();
    }

    public async Task<FibcSlotGridResult> GetSlotGridAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = ResolveCompany(companyName);
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        var inclusiveDateTo = to.AddDays(1);

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<SlotGridRow>(@"
SELECT
    CompanyNam,
    bagtype,
    partyname,
    orderno,
    Linenos,
    sysdate,
    alloted,
    capacity,
    remaining,
    allocatedper,
    shift,
    MarketingNo,
    transid,
    Effi
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND sysdate >= @DateFrom
  AND sysdate < @InclusiveDateTo
ORDER BY sysdate DESC, Linenos, shift", new
        {
            CompanyName = company,
            DateFrom = from,
            InclusiveDateTo = inclusiveDateTo,
        }, commandTimeout: CommandTimeoutSeconds)).ToList();

        var items = rows.Select(MapSlotGridItem).ToList();
        var occupied = items.Count(i => i.Allotted > 0 || i.UtilizationPercent > 0);

        return new FibcSlotGridResult
        {
            Items = items,
            DateFrom = from,
            DateTo = to,
            CompanyName = company,
            TotalSlots = items.Count,
            OccupiedSlots = occupied,
        };
    }

    public async Task<IReadOnlyList<FibcOrderPlanLineDto>> GetOrderPlanLinesAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Array.Empty<FibcOrderPlanLineDto>();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<OrderPlanRow>(@"
SELECT
    Companyname,
    linenos,
    partyname,
    orderno,
    poqty,
    BagType,
    startdate,
    CompletionDate,
    qty,
    sysdate,
    shift,
    ALLOCATEDPER
FROM VW_MarketingLinePlanning WITH (NOLOCK)
WHERE orderno = @OrderNo
ORDER BY sysdate, linenos, shift", new { OrderNo = orderNo.Trim() }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapOrderPlanLine).ToList();
    }

    public async Task<IReadOnlyList<FibcFabricRequirementDto>> GetFabricRequirementsAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Array.Empty<FibcFabricRequirementDto>();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<FabricRequirementRow>(@"
SELECT
    Customer,
    FilePONo,
    BagType,
    Qty,
    PODate,
    Targetdate,
    Heading,
    GSM,
    FabricSize,
    TotalMtr,
    Totalkg
FROM production.dbo.Vw_Bom_PPC WITH (NOLOCK)
WHERE FilePONo = @OrderNo
ORDER BY Heading", new { OrderNo = orderNo.Trim() }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(row => new FibcFabricRequirementDto
        {
            Customer = row.Customer ?? "",
            FilePoNo = row.FilePONo ?? "",
            BagType = row.BagType ?? "",
            Qty = row.Qty,
            PoDate = row.PODate,
            TargetDate = row.Targetdate,
            Heading = row.Heading ?? "",
            Gsm = row.GSM ?? "",
            FabricSize = row.FabricSize,
            TotalMtr = row.TotalMtr,
            TotalKg = row.Totalkg,
        }).ToList();
    }

    public async Task<FibcOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        using var connection = _database.CreateConnection();

        var marketing = await connection.QueryFirstOrDefaultAsync<MarketingOrderRow>(@"
SELECT TOP 1
    BuyerOrderNo,
    MarketingInvNo,
    DespatchDate,
    TotalQty,
    TypeofBag
FROM Despatch.dbo.MarketingInvoice WITH (NOLOCK)
WHERE BuyerOrderNo = @OrderNo
ORDER BY DespatchDate DESC", new { OrderNo = trimmed }, commandTimeout: CommandTimeoutSeconds);

        var bom = await connection.QueryFirstOrDefaultAsync<BomOrderRow>(@"
SELECT TOP 1
    BagType,
    Qty,
    Customer
FROM production.dbo.Vw_Bom_PPC WITH (NOLOCK)
WHERE FilePONo = @OrderNo
ORDER BY Targetdate DESC", new { OrderNo = trimmed }, commandTimeout: CommandTimeoutSeconds);

        if (marketing is null && bom is null)
            return null;

        var bagType = bom?.BagType ?? marketing?.TypeofBag;
        var dispatchDate = marketing?.DespatchDate is { } despatch && FibcPlanningEngine.IsValidDispatchDate(despatch)
            ? despatch
            : (DateTime?)null;
        var marketingQty = marketing?.TotalQty is > 0 ? marketing.TotalQty : null;

        return new FibcOrderAllotmentContextDto
        {
            OrderNo = trimmed,
            PartyName = bom?.Customer,
            MarketingNo = marketing?.MarketingInvNo,
            DispatchDate = dispatchDate,
            Quantity = ParseQuantity(bom?.Qty) ?? marketingQty,
            BagType = bagType,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
        };
    }

    public async Task<IReadOnlyList<string>> GetDistinctShiftsAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = ResolveCompany(companyName);
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        using var connection = _database.CreateConnection();
        var shifts = await connection.QueryAsync<string>(@"
SELECT DISTINCT shift
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND sysdate >= @DateFrom
  AND sysdate <= @DateTo
  AND shift IS NOT NULL
  AND LTRIM(RTRIM(shift)) <> ''
ORDER BY shift", new
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
        }, commandTimeout: CommandTimeoutSeconds);

        return shifts
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static double? ParseQuantity(string? qtyText)
    {
        if (string.IsNullOrWhiteSpace(qtyText))
            return null;

        var cleaned = qtyText.Trim().Replace(",", "");
        return double.TryParse(cleaned, out var value) ? value : null;
    }

    private string ResolveCompany(string? companyName) =>
        string.IsNullOrWhiteSpace(companyName) ? _options.DefaultCompanyName : companyName.Trim();

    private static FibcLineConfigDto MapLineConfig(LineConfigRow row)
    {
        var bagType = row.BagType ?? "";
        return new FibcLineConfigDto
        {
            LineNo = row.LNo,
            CompanyName = row.CompanyName ?? "",
            BagType = bagType,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
            IsDoubleDust = row.IsDoubleDust != 0,
            IsTripleDust = row.IsTripleDust != 0,
            BagCapacity = row.Bagcapacity,
            SortOrder = row.SOrderno,
            BufferDaysCheck = row.NoOfDaysChk,
        };
    }

    private static FibcSlotGridItemDto MapSlotGridItem(SlotGridRow row)
    {
        var capacity = row.capacity;
        var allotted = row.alloted;
        var utilization = capacity > 0 ? Math.Round(allotted / capacity * 100, 2) : 0;
        var bagType = row.bagtype ?? "";

        return new FibcSlotGridItemDto
        {
            CompanyName = row.CompanyNam ?? "",
            BagType = bagType,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
            PartyName = row.partyname,
            OrderNo = row.orderno,
            LineNo = row.Linenos ?? "",
            PlanDate = row.sysdate,
            Allotted = allotted,
            Capacity = capacity,
            Remaining = row.remaining,
            AllocatedPercent = row.allocatedper,
            Shift = row.shift ?? "",
            MarketingNo = row.MarketingNo,
            TransId = row.transid,
            Efficiency = row.Effi,
            UtilizationPercent = utilization,
            OccupancyStatus = utilization <= 0 ? "free" : utilization >= 99.9 ? "full" : "partial",
        };
    }

    private static FibcOrderPlanLineDto MapOrderPlanLine(OrderPlanRow row)
    {
        var bagType = row.BagType ?? "";
        return new FibcOrderPlanLineDto
        {
            CompanyName = row.Companyname ?? "",
            LineNo = row.linenos ?? "",
            PartyName = row.partyname,
            OrderNo = row.orderno,
            PoQty = row.poqty,
            BagType = bagType,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
            StartDate = row.startdate,
            CompletionDate = row.CompletionDate,
            Qty = row.qty,
            PlanDate = row.sysdate,
            Shift = row.shift ?? "",
            AllocatedPercent = row.ALLOCATEDPER,
        };
    }

    private sealed class LineConfigRow
    {
        public int TransId { get; set; }
        public string? CompanyName { get; set; }
        public int LNo { get; set; }
        public string? BagType { get; set; }
        public int IsDoubleDust { get; set; }
        public int IsTripleDust { get; set; }
        public int Bagcapacity { get; set; }
        public int SOrderno { get; set; }
        public int NoOfDaysChk { get; set; }
    }

    private sealed class SlotGridRow
    {
        public string? CompanyNam { get; set; }
        public string? bagtype { get; set; }
        public string? partyname { get; set; }
        public string? orderno { get; set; }
        public string? Linenos { get; set; }
        public DateTime sysdate { get; set; }
        public double alloted { get; set; }
        public double capacity { get; set; }
        public double remaining { get; set; }
        public double? allocatedper { get; set; }
        public string? shift { get; set; }
        public string? MarketingNo { get; set; }
        public int? transid { get; set; }
        public double? Effi { get; set; }
    }

    private sealed class OrderPlanRow
    {
        public string? Companyname { get; set; }
        public string? linenos { get; set; }
        public string? partyname { get; set; }
        public string? orderno { get; set; }
        public double? poqty { get; set; }
        public string? BagType { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public double qty { get; set; }
        public DateTime sysdate { get; set; }
        public string? shift { get; set; }
        public double? ALLOCATEDPER { get; set; }
    }

    private sealed class FabricRequirementRow
    {
        public string? Customer { get; set; }
        public string? FilePONo { get; set; }
        public string? BagType { get; set; }
        public string? Qty { get; set; }
        public DateTime? PODate { get; set; }
        public DateTime? Targetdate { get; set; }
        public string? Heading { get; set; }
        public string? GSM { get; set; }
        public double? FabricSize { get; set; }
        public double? TotalMtr { get; set; }
        public double? Totalkg { get; set; }
    }

    private sealed class MarketingOrderRow
    {
        public string? BuyerOrderNo { get; set; }
        public string? MarketingInvNo { get; set; }
        public DateTime? DespatchDate { get; set; }
        public double? TotalQty { get; set; }
        public string? TypeofBag { get; set; }
    }

    private sealed class BomOrderRow
    {
        public string? BagType { get; set; }
        public string? Qty { get; set; }
        public string? Customer { get; set; }
    }
}
