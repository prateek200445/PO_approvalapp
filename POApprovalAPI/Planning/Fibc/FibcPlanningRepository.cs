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
    v.CompanyNam,
    v.bagtype,
    v.partyname,
    v.orderno,
    v.Linenos,
    v.sysdate,
    v.alloted,
    v.capacity,
    v.remaining,
    v.allocatedper,
    v.shift,
    v.MarketingNo,
    COALESCE(cp.TransId, v.transid) AS transid,
    v.Effi
FROM vw_fibclineplanning_NEW v WITH (NOLOCK)
LEFT JOIN CapacityPlanning cp WITH (NOLOCK)
    ON cp.CompanyNam = v.CompanyNam
   AND cp.Linenos = v.Linenos
   AND cp.sysdate = v.sysdate
   AND cp.shift = v.shift
WHERE v.CompanyNam = @CompanyName
  AND v.sysdate >= @DateFrom
  AND v.sysdate < @InclusiveDateTo
ORDER BY v.sysdate DESC, v.Linenos, v.shift", new
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
            FabricSize = ParseNullableDouble(row.FabricSize),
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
    TypeofBag,
    BuyerName
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
        {
            var savedOnly = await GetSavedAllocationLinesAsync(trimmed, ct);
            if (savedOnly.Count == 0)
                return null;

            var first = savedOnly[0];
            return new FibcOrderAllotmentContextDto
            {
                OrderNo = trimmed,
                PartyName = first.PartyName,
                MarketingNo = null,
                DispatchDate = null,
                Quantity = savedOnly.Sum(s => s.Qty),
                BagType = first.BagType,
                BagTypeLabel = first.BagTypeLabel,
                ExistingAllocationCount = savedOnly.Count,
            };
        }

        var bagType = bom?.BagType ?? marketing?.TypeofBag;
        var dispatchDate = marketing?.DespatchDate is { } despatch && FibcPlanningEngine.IsValidDispatchDate(despatch)
            ? despatch
            : (DateTime?)null;
        var marketingQty = marketing?.TotalQty is > 0 ? marketing.TotalQty : null;
        var existingCount = await GetExistingAllocationCountAsync(trimmed, ct);

        return new FibcOrderAllotmentContextDto
        {
            OrderNo = trimmed,
            PartyName = bom?.Customer ?? marketing?.BuyerName,
            MarketingNo = marketing?.MarketingInvNo,
            DispatchDate = dispatchDate,
            Quantity = ParseQuantity(bom?.Qty) ?? marketingQty,
            BagType = bagType,
            BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
            ExistingAllocationCount = existingCount,
        };
    }

    public async Task<IReadOnlyList<FibcOrderPlanLineDto>> GetSavedAllocationLinesAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Array.Empty<FibcOrderPlanLineDto>();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<SavedAllocationRow>(@"
SELECT
    Companyname,
    linenos,
    partyname,
    orderno,
    qty,
    sysdate,
    shift,
    ALLOCATEDPER,
    PBagType
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE orderno = @OrderNo
ORDER BY sysdate, linenos, shift", new { OrderNo = orderNo.Trim() }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(row =>
        {
            var bagType = row.PBagType ?? "";
            return new FibcOrderPlanLineDto
            {
                CompanyName = row.Companyname ?? "",
                LineNo = row.linenos ?? "",
                PartyName = row.partyname,
                OrderNo = row.orderno,
                BagType = bagType,
                BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
                Qty = row.qty,
                PlanDate = row.sysdate,
                Shift = row.shift ?? "",
                AllocatedPercent = row.ALLOCATEDPER,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<FibcSavedAllocationRowDto>> GetSavedAllocationsInWindowAsync(
        string companyName,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = ResolveCompany(companyName);
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<SavedAllocationRow>(@"
SELECT
    Companyname,
    linenos,
    partyname,
    orderno,
    qty,
    sysdate,
    shift,
    ALLOCATEDPER,
    PBagType,
    MarketingNo,
    QCapacity,
    Effi
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname = @CompanyName
  AND sysdate >= @DateFrom
  AND sysdate <= @DateTo
ORDER BY sysdate, linenos, shift", new
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
        }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(row => new FibcSavedAllocationRowDto
        {
            CompanyName = row.Companyname ?? company,
            LineNo = row.linenos ?? "",
            PartyName = row.partyname,
            OrderNo = row.orderno ?? "",
            BagType = row.PBagType ?? "",
            PlanDate = row.sysdate,
            Shift = row.shift ?? "",
            Qty = row.qty,
            AllocatedPercent = row.ALLOCATEDPER,
            Capacity = row.QCapacity,
            Efficiency = row.Effi,
            MarketingNo = row.MarketingNo,
        }).ToList();
    }

    public async Task<double> GetSavedQtyOnSlotExcludingOrderAsync(
        string companyName,
        string lineNo,
        DateTime planDate,
        string shift,
        string excludeOrderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(lineNo) || string.IsNullOrWhiteSpace(shift))
            return 0;

        using var connection = _database.CreateConnection();
        return await connection.ExecuteScalarAsync<double>(@"
SELECT ISNULL(SUM(qty), 0)
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname = @CompanyName
  AND linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift
  AND orderno <> @ExcludeOrderNo", new
        {
            CompanyName = ResolveCompany(companyName),
            LineNo = lineNo.Trim(),
            PlanDate = planDate.Date,
            Shift = shift.Trim(),
            ExcludeOrderNo = excludeOrderNo.Trim(),
        }, commandTimeout: CommandTimeoutSeconds);
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

    public async Task<int> GetExistingAllocationCountAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return 0;

        using var connection = _database.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(*)
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE orderno = @OrderNo", new { OrderNo = orderNo.Trim() }, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<double?> GetSlotRemainingAsync(
        string companyName,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(lineNo) || string.IsNullOrWhiteSpace(shift))
            return null;

        using var connection = _database.CreateConnection();
        return await connection.ExecuteScalarAsync<double?>(@"
SELECT remaining
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND Linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
        {
            CompanyName = companyName,
            LineNo = lineNo.Trim(),
            PlanDate = planDate.Date,
            Shift = shift.Trim(),
        }, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<int?> GetCapacityTransIdAsync(
        string companyName,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(lineNo) || string.IsNullOrWhiteSpace(shift))
            return null;

        using var connection = _database.CreateConnection();
        return await connection.ExecuteScalarAsync<int?>(@"
SELECT TOP 1 TransId
FROM CapacityPlanning WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND Linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift
ORDER BY TransId", new
        {
            CompanyName = companyName,
            LineNo = lineNo.Trim(),
            PlanDate = planDate.Date,
            Shift = shift.Trim(),
        }, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<int> InsertAllocationsAsync(
        string companyName,
        string orderNo,
        string? partyName,
        string? marketingNo,
        IReadOnlyList<FibcSlotGridItemDto> slots,
        bool replaceExisting,
        bool allowSyntheticSlots = false,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (slots.Count == 0)
            return 0;

        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            if (replaceExisting)
            {
                await connection.ExecuteAsync(@"
DELETE FROM dbo.prod_fibcallocationMaster
WHERE orderno = @OrderNo", new { OrderNo = orderNo }, transaction, commandTimeout: CommandTimeoutSeconds);
            }

            var inserted = 0;
            foreach (var slot in slots)
            {
                ct.ThrowIfCancellationRequested();

                var occupiedByOthers = await connection.ExecuteScalarAsync<double>(@"
SELECT ISNULL(SUM(qty), 0)
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname = @CompanyName
  AND linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift
  AND orderno <> @OrderNo", new
                {
                    CompanyName = companyName,
                    LineNo = slot.LineNo,
                    PlanDate = slot.PlanDate.Date,
                    Shift = slot.Shift,
                    OrderNo = orderNo.Trim(),
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                var remaining = await connection.ExecuteScalarAsync<double?>(@"
SELECT remaining
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND Linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
                {
                    CompanyName = companyName,
                    LineNo = slot.LineNo,
                    PlanDate = slot.PlanDate.Date,
                    Shift = slot.Shift,
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                var capacity = slot.Capacity > 0.001 ? slot.Capacity : slot.Allotted;
                if (capacity <= 0.001 && remaining is > 0)
                    capacity = remaining.Value + slot.Allotted;

                double available;
                if (allowSyntheticSlots || remaining is null)
                {
                    if (remaining is null && capacity <= 0.001)
                    {
                        capacity = slot.Remaining + slot.Allotted;
                        if (capacity <= 0.001)
                            capacity = slot.Allotted;
                    }

                    available = Math.Max(0, capacity - occupiedByOthers);
                }
                else
                {
                    available = Math.Max(0, Math.Min(remaining.Value, capacity - occupiedByOthers));
                }

                if (slot.Allotted > available + 0.01)
                {
                    throw new InvalidOperationException(
                        occupiedByOthers > 0.001
                            ? $"Slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} available ({occupiedByOthers:N0} pcs allocated to other orders)."
                            : $"Slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} only has {available:N0} remaining but {slot.Allotted:N0} was requested.");
                }

                if (remaining is null && !allowSyntheticSlots)
                {
                    throw new InvalidOperationException(
                        $"Capacity slot on {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} no longer exists.");
                }

                var allocatedPercent = slot.AllocatedPercent
                    ?? (slot.Capacity > 0 ? Math.Round(slot.Allotted / slot.Capacity * 100, 2) : 100d);
                var efficiency = slot.Efficiency ?? 0.5d;

                await connection.ExecuteAsync(@"
INSERT INTO dbo.prod_fibcallocationMaster
    (Companyname, linenos, partyname, orderno, qty, sysdate, ALLOCATEDPER, shift, MarketingNo, PBagType, QCapacity, Effi)
VALUES
    (@Companyname, @Linenos, @Partyname, @Orderno, @Qty, @Sysdate, @AllocatedPer, @Shift, @MarketingNo, @PBagType, @QCapacity, @Effi)", new
                {
                    Companyname = companyName,
                    Linenos = slot.LineNo,
                    Partyname = partyName,
                    Orderno = orderNo,
                    Qty = slot.Allotted,
                    Sysdate = slot.PlanDate.Date,
                    AllocatedPer = allocatedPercent,
                    Shift = slot.Shift,
                    MarketingNo = marketingNo,
                    PBagType = slot.BagType,
                    QCapacity = slot.Capacity,
                    Effi = efficiency,
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                inserted++;
            }

            transaction.Commit();
            return inserted;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<FibcSavedAllocationRowDto?> GetSavedAllocationSlotAsync(
        string orderNo,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<SavedAllocationRow>(@"
SELECT TOP 1
    Companyname,
    linenos,
    partyname,
    orderno,
    qty,
    sysdate,
    shift,
    ALLOCATEDPER,
    MarketingNo,
    PBagType,
    QCapacity,
    Effi
FROM dbo.prod_fibcallocationMaster WITH (NOLOCK)
WHERE orderno = @OrderNo
  AND linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
        {
            OrderNo = orderNo.Trim(),
            LineNo = lineNo.Trim(),
            PlanDate = planDate.Date,
            Shift = shift.Trim(),
        }, commandTimeout: CommandTimeoutSeconds);

        if (row is null)
            return null;

        return new FibcSavedAllocationRowDto
        {
            CompanyName = row.Companyname ?? "",
            OrderNo = row.orderno ?? "",
            PartyName = row.partyname,
            MarketingNo = row.MarketingNo,
            BagType = row.PBagType ?? "",
            LineNo = row.linenos ?? "",
            PlanDate = row.sysdate,
            Shift = row.shift ?? "",
            Qty = row.qty,
            AllocatedPercent = row.ALLOCATEDPER,
            Capacity = row.QCapacity,
            Efficiency = row.Effi,
        };
    }

    public async Task<int> DeleteAllocationSlotAsync(
        string orderNo,
        string lineNo,
        DateTime planDate,
        string shift,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        return await connection.ExecuteAsync(@"
DELETE FROM dbo.prod_fibcallocationMaster
WHERE orderno = @OrderNo
  AND linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
        {
            OrderNo = orderNo.Trim(),
            LineNo = lineNo.Trim(),
            PlanDate = planDate.Date,
            Shift = shift.Trim(),
        }, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<int> ApplyCriticalShiftPlanAsync(
        string companyName,
        string criticalOrderNo,
        string? criticalPartyName,
        string? criticalMarketingNo,
        IReadOnlyList<FibcSlotGridItemDto> criticalSlots,
        IReadOnlyList<FibcOrderShiftDisplacementDto> displacements,
        bool replaceCriticalExisting,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var deleted = 0;
        var inserted = 0;

        try
        {
            foreach (var move in displacements)
            {
                ct.ThrowIfCancellationRequested();
                var saved = await connection.QueryFirstOrDefaultAsync<SavedAllocationRow>(@"
SELECT TOP 1
    Companyname, linenos, partyname, orderno, qty, sysdate, shift,
    ALLOCATEDPER, MarketingNo, PBagType, QCapacity, Effi
FROM dbo.prod_fibcallocationMaster
WHERE orderno = @OrderNo AND linenos = @LineNo AND sysdate = @PlanDate AND shift = @Shift", new
                {
                    OrderNo = move.OrderNo.Trim(),
                    LineNo = move.FromLineNo.Trim(),
                    PlanDate = move.FromPlanDate.Date,
                    Shift = move.FromShift.Trim(),
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                if (saved is null)
                {
                    throw new InvalidOperationException(
                        $"Blocking allocation for order {move.OrderNo} on {move.FromPlanDate:yyyy-MM-dd} line {move.FromLineNo} shift {move.FromShift} no longer exists.");
                }

                var remaining = await connection.ExecuteScalarAsync<double?>(@"
SELECT remaining
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND Linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
                {
                    CompanyName = companyName,
                    LineNo = move.ToLineNo.Trim(),
                    PlanDate = move.ToPlanDate.Date,
                    Shift = move.ToShift.Trim(),
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                if (remaining is null)
                {
                    throw new InvalidOperationException(
                        $"Target slot {move.ToPlanDate:yyyy-MM-dd} line {move.ToLineNo} shift {move.ToShift} no longer exists.");
                }

                if (move.Qty > remaining.Value + 0.01)
                {
                    throw new InvalidOperationException(
                        $"Target slot only has {remaining.Value:N0} remaining but {move.Qty:N0} needed to shift order {move.OrderNo}.");
                }

                deleted += await connection.ExecuteAsync(@"
DELETE FROM dbo.prod_fibcallocationMaster
WHERE orderno = @OrderNo AND linenos = @LineNo AND sysdate = @PlanDate AND shift = @Shift", new
                {
                    OrderNo = move.OrderNo.Trim(),
                    LineNo = move.FromLineNo.Trim(),
                    PlanDate = move.FromPlanDate.Date,
                    Shift = move.FromShift.Trim(),
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                var allocatedPercent = move.AllocatedPercent
                    ?? (move.Capacity > 0 ? Math.Round(move.Qty / move.Capacity * 100, 2) : saved.ALLOCATEDPER ?? 100d);

                await connection.ExecuteAsync(@"
INSERT INTO dbo.prod_fibcallocationMaster
    (Companyname, linenos, partyname, orderno, qty, sysdate, ALLOCATEDPER, shift, MarketingNo, PBagType, QCapacity, Effi)
VALUES
    (@Companyname, @Linenos, @Partyname, @Orderno, @Qty, @Sysdate, @AllocatedPer, @Shift, @MarketingNo, @PBagType, @QCapacity, @Effi)", new
                {
                    Companyname = saved.Companyname ?? companyName,
                    Linenos = move.ToLineNo.Trim(),
                    Partyname = saved.partyname,
                    Orderno = move.OrderNo.Trim(),
                    Qty = move.Qty,
                    Sysdate = move.ToPlanDate.Date,
                    AllocatedPer = allocatedPercent,
                    Shift = move.ToShift.Trim(),
                    MarketingNo = saved.MarketingNo,
                    PBagType = saved.PBagType ?? move.BagType,
                    QCapacity = move.Capacity > 0 ? move.Capacity : saved.QCapacity,
                    Effi = saved.Effi,
                }, transaction, commandTimeout: CommandTimeoutSeconds);
                inserted++;
            }

            if (replaceCriticalExisting)
            {
                deleted += await connection.ExecuteAsync(@"
DELETE FROM dbo.prod_fibcallocationMaster
WHERE orderno = @OrderNo", new { OrderNo = criticalOrderNo.Trim() }, transaction, commandTimeout: CommandTimeoutSeconds);
            }

            foreach (var slot in criticalSlots)
            {
                ct.ThrowIfCancellationRequested();
                var remaining = await connection.ExecuteScalarAsync<double?>(@"
SELECT remaining
FROM vw_fibclineplanning_NEW WITH (NOLOCK)
WHERE CompanyNam = @CompanyName
  AND Linenos = @LineNo
  AND sysdate = @PlanDate
  AND shift = @Shift", new
                {
                    CompanyName = companyName,
                    LineNo = slot.LineNo,
                    PlanDate = slot.PlanDate.Date,
                    Shift = slot.Shift,
                }, transaction, commandTimeout: CommandTimeoutSeconds);

                if (remaining is null)
                {
                    throw new InvalidOperationException(
                        $"Critical slot {slot.PlanDate:yyyy-MM-dd} line {slot.LineNo} shift {slot.Shift} no longer exists.");
                }

                if (slot.Allotted > remaining.Value + 0.01)
                {
                    throw new InvalidOperationException(
                        $"Critical slot only has {remaining.Value:N0} remaining but {slot.Allotted:N0} requested.");
                }

                var allocatedPercent = slot.AllocatedPercent
                    ?? (slot.Capacity > 0 ? Math.Round(slot.Allotted / slot.Capacity * 100, 2) : 100d);
                var efficiency = slot.Efficiency ?? 0.5d;

                await connection.ExecuteAsync(@"
INSERT INTO dbo.prod_fibcallocationMaster
    (Companyname, linenos, partyname, orderno, qty, sysdate, ALLOCATEDPER, shift, MarketingNo, PBagType, QCapacity, Effi)
VALUES
    (@Companyname, @Linenos, @Partyname, @Orderno, @Qty, @Sysdate, @AllocatedPer, @Shift, @MarketingNo, @PBagType, @QCapacity, @Effi)", new
                {
                    Companyname = companyName,
                    Linenos = slot.LineNo,
                    Partyname = criticalPartyName,
                    Orderno = criticalOrderNo.Trim(),
                    Qty = slot.Allotted,
                    Sysdate = slot.PlanDate.Date,
                    AllocatedPer = allocatedPercent,
                    Shift = slot.Shift,
                    MarketingNo = criticalMarketingNo,
                    PBagType = slot.BagType,
                    QCapacity = slot.Capacity,
                    Effi = efficiency,
                }, transaction, commandTimeout: CommandTimeoutSeconds);
                inserted++;
            }

            transaction.Commit();
            return inserted;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static double? ParseQuantity(string? qtyText)
    {
        if (string.IsNullOrWhiteSpace(qtyText))
            return null;

        var cleaned = qtyText.Trim().Replace(",", "");
        return double.TryParse(cleaned, out var value) ? value : null;
    }

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = value.Trim().Replace(",", "");
        return double.TryParse(cleaned, out var parsed) ? parsed : null;
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
        public string? FabricSize { get; set; }
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
        public string? BuyerName { get; set; }
    }

    private sealed class SavedAllocationRow
    {
        public string? Companyname { get; set; }
        public string? linenos { get; set; }
        public string? partyname { get; set; }
        public string? orderno { get; set; }
        public double qty { get; set; }
        public DateTime sysdate { get; set; }
        public string? shift { get; set; }
        public double? ALLOCATEDPER { get; set; }
        public string? PBagType { get; set; }
        public string? MarketingNo { get; set; }
        public double QCapacity { get; set; }
        public double Effi { get; set; }
    }

    private sealed class BomOrderRow
    {
        public string? BagType { get; set; }
        public string? Qty { get; set; }
        public string? Customer { get; set; }
    }
}
