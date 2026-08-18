using Dapper;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Loom.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningRepository : ILoomPlanningRepository
{
    private const int CommandTimeoutSeconds = 120;

    private readonly DatabaseService _database;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningRepository(DatabaseService database, IOptions<LoomPlanningOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<LoomMasterDto>> GetLoomMasterAsync(
        string? companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = ResolveCompany(companyName);

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<LoomMasterRow>(@"
SELECT
    LoomNo,
    CompanyName,
    LoomCode,
    LoomSpecification,
    make,
    ModelNo,
    MinSize,
    MaxSize,
    CreelCapicity,
    isFreeze
FROM NewMISLoomMaster WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY LoomNo", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapLoomMaster).ToList();
    }

    public async Task<LoomAllocationGridResult> GetAllocationGridAsync(
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
        var rows = (await connection.QueryAsync<AllocationGridRow>(@"
SELECT
    a.SrNo,
    a.LoomNo,
    m.CompanyName,
    m.LoomCode,
    m.LoomSpecification,
    a.PartyName,
    a.PONO,
    a.AllocationDate,
    a.ToDate,
    a.ReqGSM,
    a.asize,
    a.AllocationType,
    a.Color,
    a.Sector,
    a.Remarks,
    a.isActive
FROM Prod_LoomAlocationMaster a WITH (NOLOCK)
INNER JOIN NewMISLoomMaster m WITH (NOLOCK)
    ON m.LoomNo = a.LoomNo
   AND m.CompanyName = @CompanyName
WHERE a.AllocationDate >= @DateFrom
  AND a.AllocationDate < @InclusiveDateTo
ORDER BY a.AllocationDate DESC, a.LoomNo", new
        {
            CompanyName = company,
            DateFrom = from,
            InclusiveDateTo = inclusiveDateTo,
        }, commandTimeout: CommandTimeoutSeconds)).ToList();

        var items = rows.Select(MapAllocationGridItem).ToList();
        var activeLooms = items.Select(i => i.LoomNo).Distinct().Count();

        return new LoomAllocationGridResult
        {
            Items = items,
            DateFrom = from,
            DateTo = to,
            CompanyName = company,
            TotalRows = items.Count,
            ActiveLoomCount = activeLooms,
        };
    }

    public async Task<IReadOnlyList<LoomOrderAllocationLineDto>> GetOrderAllocationsAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Array.Empty<LoomOrderAllocationLineDto>();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<OrderAllocationRow>(@"
SELECT
    a.LoomNo,
    m.LoomCode,
    a.PartyName,
    a.PONO,
    a.AllocationDate,
    a.ToDate,
    a.ReqGSM,
    a.asize,
    a.AllocationType,
    a.Color,
    a.Sector,
    a.Remarks
FROM Prod_LoomAlocationMaster a WITH (NOLOCK)
LEFT JOIN NewMISLoomMaster m WITH (NOLOCK) ON m.LoomNo = a.LoomNo
WHERE a.PONO = @OrderNo
ORDER BY a.AllocationDate DESC, a.LoomNo", new { OrderNo = orderNo.Trim() }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(row => new LoomOrderAllocationLineDto
        {
            LoomNo = row.LoomNo,
            LoomCode = row.LoomCode,
            PartyName = row.PartyName,
            OrderNo = row.PONO,
            AllocationDate = row.AllocationDate,
            ToDate = row.ToDate,
            ReqGsm = row.ReqGSM,
            Size = row.asize,
            AllocationType = row.AllocationType,
            Color = row.Color,
            Sector = row.Sector,
            Remarks = row.Remarks,
        }).ToList();
    }

    public async Task<IReadOnlyList<LoomFabricRequirementDto>> GetFabricRequirementsAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Array.Empty<LoomFabricRequirementDto>();

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

        return rows.Select(row => new LoomFabricRequirementDto
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

    public async Task<LoomOrderContextDto?> GetOrderContextAsync(string orderNo, CancellationToken ct = default)
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

        var allocationCount = await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(*)
FROM Prod_LoomAlocationMaster WITH (NOLOCK)
WHERE PONO = @OrderNo
  AND (isActive IS NULL OR UPPER(LTRIM(RTRIM(isActive))) <> 'N')",
            new { OrderNo = trimmed },
            commandTimeout: CommandTimeoutSeconds);

        if (marketing is null && allocationCount == 0)
            return null;

        return new LoomOrderContextDto
        {
            OrderNo = trimmed,
            PartyName = marketing?.BuyerName,
            MarketingNo = marketing?.MarketingInvNo,
            DispatchDate = NormalizeDate(marketing?.DespatchDate),
            Quantity = marketing?.TotalQty,
            BagType = marketing?.TypeofBag,
            ExistingAllocationCount = allocationCount,
        };
    }

    public async Task<LoomOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(string orderNo, CancellationToken ct = default)
    {
        var basic = await GetOrderContextAsync(orderNo, ct);
        var fabric = await GetFabricRequirementsAsync(orderNo, ct);
        if (basic is null && fabric.Count == 0)
            return null;

        var trimmed = orderNo.Trim();
        DateTime? fabricReq = null;
        if (basic?.DispatchDate is not null)
            fabricReq = basic.DispatchDate.Value;

        var firstFabric = fabric.FirstOrDefault(f => f.TargetDate is not null && f.TargetDate.Value.Year >= 2000);
        if (firstFabric?.TargetDate is not null)
            fabricReq = firstFabric.TargetDate;

        return new LoomOrderAllotmentContextDto
        {
            OrderNo = trimmed,
            PartyName = basic?.PartyName,
            MarketingNo = basic?.MarketingNo,
            DispatchDate = basic?.DispatchDate,
            FabricRequirementDate = fabricReq,
            Quantity = basic?.Quantity,
            BagType = basic?.BagType,
            ExistingAllocationCount = basic?.ExistingAllocationCount ?? 0,
            FabricLines = fabric,
        };
    }

    public Task<int> GetExistingAllocationCountAsync(string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderNo))
            return Task.FromResult(0);

        return GetAllocationCountInternalAsync(orderNo.Trim(), ct);
    }

    public async Task<IReadOnlyList<LoomAllocationGridItemDto>> GetPlanningAllocationsAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var grid = await GetAllocationGridAsync(dateFrom, dateTo, companyName, ct);
        return grid.Items;
    }

    public async Task<LoomProductionMeterGridResult> GetProductionMetersAsync(
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
        var rows = await connection.QueryAsync<ProductionMeterRow>(@"
SELECT
    loomno,
    LOOMCODE,
    sysdate,
    prodmtrA,
    prodmtrB,
    ReqGSM,
    size,
    pono,
    partyname
FROM vw_Loom_Prod_Mtr WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND sysdate >= @DateFrom
  AND sysdate <= @DateTo
ORDER BY sysdate DESC, loomno", new
        {
            CompanyName = company,
            DateFrom = from,
            DateTo = to,
        }, commandTimeout: CommandTimeoutSeconds);

        var items = rows.Select(r => new LoomProductionMeterDto
        {
            LoomNo = r.loomno,
            LoomCode = r.LOOMCODE,
            PlanDate = r.sysdate,
            ProdMetersA = r.prodmtrA,
            ProdMetersB = r.prodmtrB,
            ReqGsm = r.ReqGSM,
            Size = r.size,
            OrderNo = r.pono,
            PartyName = r.partyname,
        }).ToList();

        return new LoomProductionMeterGridResult
        {
            Items = items,
            DateFrom = from,
            DateTo = to,
            CompanyName = company,
        };
    }

    public async Task<IReadOnlyList<LoomPpmSpecDto>> GetPpmSpecsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<PpmSpecRow>(@"
SELECT LoomType, GSMFrom, GSMTo, WidthFrom, WidthTo, PPM
FROM LoomSpecificationMaster WITH (NOLOCK)
ORDER BY LoomType, GSMFrom", commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new LoomPpmSpecDto
        {
            LoomType = r.LoomType ?? "",
            GsmFrom = r.GSMFrom,
            GsmTo = r.GSMTo,
            WidthFrom = r.WidthFrom,
            WidthTo = r.WidthTo,
            Ppm = r.PPM,
        }).Concat(_options.EmbeddedPpmMatrix.Select(e => new LoomPpmSpecDto
        {
            LoomType = e.LoomType,
            GsmFrom = e.GsmFrom,
            GsmTo = e.GsmTo,
            WidthFrom = e.WidthFrom,
            WidthTo = e.WidthTo,
            Ppm = e.Ppm,
        })).ToList();
    }

    public async Task<IReadOnlyList<LoomFormulaDto>> GetFormulasAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<FormulaRow>(@"
SELECT FormulaId, Size, WarpMesh, WeftMesh, FormulaName
FROM Prod_LoomFormulaMaster WITH (NOLOCK)
WHERE IsActive = 'Yes' OR IsActive IS NULL
ORDER BY FormulaId DESC", commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new LoomFormulaDto
        {
            FormulaId = r.FormulaId,
            Size = r.Size,
            WarpMesh = r.WarpMesh,
            WeftMesh = r.WeftMesh,
            FormulaName = r.FormulaName,
        }).ToList();
    }

    public async Task<int> InsertLoomAllocationsAsync(
        string companyName,
        string orderNo,
        string? partyName,
        IReadOnlyList<LoomProposedSegmentDto> segments,
        bool replaceExisting,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (segments.Count == 0)
            return 0;

        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var inserted = 0;

        try
        {
            if (replaceExisting)
            {
                await connection.ExecuteAsync(@"
DELETE FROM dbo.Prod_LoomAlocationMaster
WHERE PONO = @OrderNo", new { OrderNo = orderNo.Trim() }, transaction, commandTimeout: CommandTimeoutSeconds);
            }

            var companyId = await connection.ExecuteScalarAsync<int?>(@"
SELECT TOP 1 CompanyId
FROM Prod_LoomAlocationMaster WITH (NOLOCK)
WHERE CompanyId IS NOT NULL
ORDER BY SrNo DESC", transaction: transaction, commandTimeout: CommandTimeoutSeconds)
                ?? _options.DefaultCompanyId;

            var recordLogId = await connection.ExecuteScalarAsync<int>(@"
SELECT ISNULL(MAX(RecordLogId), 0) + 1 FROM Prod_LoomAlocationMaster WITH (UPDLOCK, HOLDLOCK)", transaction: transaction, commandTimeout: CommandTimeoutSeconds);

            foreach (var seg in segments)
            {
                ct.ThrowIfCancellationRequested();
                var formulaId = seg.FormulaId ?? 0;
                await connection.ExecuteAsync(@"
INSERT INTO dbo.Prod_LoomAlocationMaster
    (CompanyId, LoomNo, ReqGSM, Color, Sector, PartyName, PONO,
     AllocationDate, ToDate, RecordLogId, FormulaID,
     WarpBobin, WeftBobin, Remarks, isActive, asize,
     AllocationType, FGItemCode, CondWarp, CondWeft, MonoYarn, CondDNR, Vent)
VALUES
    (@CompanyId, @LoomNo, @ReqGSM, @Color, @Sector, @PartyName, @PONO,
     @AllocationDate, @ToDate, @RecordLogId, @FormulaID,
     @WarpBobin, @WeftBobin, @Remarks, @IsActive, @asize,
     @AllocationType, @FGItemCode, @CondWarp, @CondWeft, @MonoYarn, @CondDNR, @Vent)",
                    new
                    {
                        CompanyId = companyId,
                        LoomNo = seg.LoomNo,
                        ReqGSM = seg.ReqGsm,
                        Color = "MW",
                        Sector = "BIGBAG",
                        PartyName = partyName ?? "",
                        PONO = orderNo.Trim(),
                        AllocationDate = seg.FromDate.Date,
                        ToDate = seg.ToDate.Date,
                        RecordLogId = recordLogId++,
                        FormulaID = formulaId,
                        WarpBobin = "",
                        WeftBobin = "",
                        Remarks = "0",
                        IsActive = "Yes",
                        asize = seg.Size,
                        AllocationType = $"WEB-{seg.AllotmentCase}",
                        FGItemCode = "WIP00023",
                        CondWarp = "",
                        CondWeft = "",
                        MonoYarn = "",
                        CondDNR = 0,
                        Vent = 0d,
                    },
                    transaction,
                    commandTimeout: CommandTimeoutSeconds);
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

    public async Task<(int RowsShifted, int RowsInserted)> ApplyLoomShiftPlanAsync(
        string orderNo,
        string? partyName,
        IReadOnlyList<LoomProposedSegmentDto> segments,
        IReadOnlyList<LoomOrderShiftDisplacementDto> displacements,
        bool replaceExisting,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var shifted = 0;
        var inserted = 0;

        try
        {
            foreach (var move in displacements)
            {
                ct.ThrowIfCancellationRequested();
                var updated = await connection.ExecuteAsync(@"
UPDATE dbo.Prod_LoomAlocationMaster
SET AllocationDate = @NewFromDate,
    ToDate = @NewToDate
WHERE SrNo = @AllocationId
   OR (LoomNo = @LoomNo AND PONO = @OrderNo AND AllocationDate = @FromDate)",
                    new
                    {
                        move.AllocationId,
                        move.LoomNo,
                        OrderNo = move.OrderNo.Trim(),
                        FromDate = move.FromDate.Date,
                        NewFromDate = move.NewFromDate.Date,
                        NewToDate = move.NewToDate.Date,
                    },
                    transaction,
                    commandTimeout: CommandTimeoutSeconds);

                if (updated == 0)
                {
                    throw new InvalidOperationException(
                        $"Blocking allocation for order {move.OrderNo} on loom {move.LoomNo} from {move.FromDate:yyyy-MM-dd} no longer exists.");
                }

                shifted += updated;
            }

            if (replaceExisting)
            {
                await connection.ExecuteAsync(@"
DELETE FROM dbo.Prod_LoomAlocationMaster
WHERE PONO = @OrderNo", new { OrderNo = orderNo.Trim() }, transaction, commandTimeout: CommandTimeoutSeconds);
            }

            var companyId = await connection.ExecuteScalarAsync<int?>(@"
SELECT TOP 1 CompanyId FROM Prod_LoomAlocationMaster WITH (NOLOCK)
WHERE CompanyId IS NOT NULL ORDER BY SrNo DESC", transaction: transaction, commandTimeout: CommandTimeoutSeconds)
                ?? _options.DefaultCompanyId;

            var recordLogId = await connection.ExecuteScalarAsync<int>(@"
SELECT ISNULL(MAX(RecordLogId), 0) + 1 FROM Prod_LoomAlocationMaster WITH (UPDLOCK, HOLDLOCK)",
                transaction: transaction, commandTimeout: CommandTimeoutSeconds);

            foreach (var seg in segments)
            {
                ct.ThrowIfCancellationRequested();
                await connection.ExecuteAsync(@"
INSERT INTO dbo.Prod_LoomAlocationMaster
    (CompanyId, LoomNo, ReqGSM, Color, Sector, PartyName, PONO,
     AllocationDate, ToDate, RecordLogId, FormulaID,
     WarpBobin, WeftBobin, Remarks, isActive, asize,
     AllocationType, FGItemCode, CondWarp, CondWeft, MonoYarn, CondDNR, Vent)
VALUES
    (@CompanyId, @LoomNo, @ReqGSM, @Color, @Sector, @PartyName, @PONO,
     @AllocationDate, @ToDate, @RecordLogId, @FormulaID,
     @WarpBobin, @WeftBobin, @Remarks, @IsActive, @asize,
     @AllocationType, @FGItemCode, @CondWarp, @CondWeft, @MonoYarn, @CondDNR, @Vent)",
                    new
                    {
                        CompanyId = companyId,
                        LoomNo = seg.LoomNo,
                        ReqGSM = seg.ReqGsm,
                        Color = "MW",
                        Sector = "BIGBAG",
                        PartyName = partyName ?? "",
                        PONO = orderNo.Trim(),
                        AllocationDate = seg.FromDate.Date,
                        ToDate = seg.ToDate.Date,
                        RecordLogId = recordLogId++,
                        FormulaID = seg.FormulaId ?? 0,
                        WarpBobin = "",
                        WeftBobin = "",
                        Remarks = "0",
                        IsActive = "Yes",
                        asize = seg.Size,
                        AllocationType = $"WEB-{seg.AllotmentCase}",
                        FGItemCode = "WIP00023",
                        CondWarp = "",
                        CondWeft = "",
                        MonoYarn = "",
                        CondDNR = 0,
                        Vent = 0d,
                    },
                    transaction,
                    commandTimeout: CommandTimeoutSeconds);
                inserted++;
            }

            transaction.Commit();
            return (shifted, inserted);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task<int> GetAllocationCountInternalAsync(string orderNo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(*)
FROM Prod_LoomAlocationMaster WITH (NOLOCK)
WHERE PONO = @OrderNo
  AND (isActive IS NULL OR UPPER(LTRIM(RTRIM(isActive))) <> 'N')",
            new { OrderNo = orderNo },
            commandTimeout: CommandTimeoutSeconds);
    }

    private string ResolveCompany(string? companyName) =>
        string.IsNullOrWhiteSpace(companyName) ? _options.DefaultCompanyName : companyName.Trim();

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (value is null || value.Value.Year < 2000)
            return null;
        return value.Value.Date;
    }

    private static LoomMasterDto MapLoomMaster(LoomMasterRow row) => new()
    {
        LoomNo = row.LoomNo,
        CompanyName = row.CompanyName ?? "",
        LoomCode = row.LoomCode,
        LoomSpecification = row.LoomSpecification,
        Make = row.make,
        ModelNo = row.ModelNo,
        MinSize = row.MinSize,
        MaxSize = row.MaxSize,
        CreelCapacity = row.CreelCapicity,
        IsFrozen = string.Equals(row.isFreeze?.Trim(), "Y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.isFreeze?.Trim(), "True", StringComparison.OrdinalIgnoreCase),
    };

    private static LoomAllocationGridItemDto MapAllocationGridItem(AllocationGridRow row) => new()
    {
        AllocationId = row.SrNo,
        LoomNo = row.LoomNo,
        CompanyName = row.CompanyName ?? "",
        LoomCode = row.LoomCode,
        LoomSpecification = row.LoomSpecification,
        PartyName = row.PartyName,
        OrderNo = row.PONO,
        AllocationDate = row.AllocationDate,
        ToDate = row.ToDate,
        ReqGsm = row.ReqGSM,
        Size = row.asize,
        AllocationType = row.AllocationType,
        Color = row.Color,
        Sector = row.Sector,
        Remarks = row.Remarks,
        IsActive = !string.Equals(row.isActive?.Trim(), "N", StringComparison.OrdinalIgnoreCase),
    };

    private sealed class LoomMasterRow
    {
        public int LoomNo { get; set; }
        public string? CompanyName { get; set; }
        public string? LoomCode { get; set; }
        public string? LoomSpecification { get; set; }
        public string? make { get; set; }
        public string? ModelNo { get; set; }
        public double? MinSize { get; set; }
        public double? MaxSize { get; set; }
        public string? CreelCapicity { get; set; }
        public string? isFreeze { get; set; }
    }

    private sealed class AllocationGridRow
    {
        public int SrNo { get; set; }
        public int LoomNo { get; set; }
        public string? CompanyName { get; set; }
        public string? LoomCode { get; set; }
        public string? LoomSpecification { get; set; }
        public string? PartyName { get; set; }
        public string? PONO { get; set; }
        public DateTime AllocationDate { get; set; }
        public DateTime? ToDate { get; set; }
        public double? ReqGSM { get; set; }
        public double? asize { get; set; }
        public string? AllocationType { get; set; }
        public string? Color { get; set; }
        public string? Sector { get; set; }
        public string? Remarks { get; set; }
        public string? isActive { get; set; }
    }

    private sealed class OrderAllocationRow
    {
        public int LoomNo { get; set; }
        public string? LoomCode { get; set; }
        public string? PartyName { get; set; }
        public string? PONO { get; set; }
        public DateTime AllocationDate { get; set; }
        public DateTime? ToDate { get; set; }
        public double? ReqGSM { get; set; }
        public double? asize { get; set; }
        public string? AllocationType { get; set; }
        public string? Color { get; set; }
        public string? Sector { get; set; }
        public string? Remarks { get; set; }
    }

    private sealed class FabricRequirementRow
    {
        public string? Customer { get; set; }
        public string? FilePONo { get; set; }
        public string? BagType { get; set; }
        public double? Qty { get; set; }
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
        public string? BuyerName { get; set; }
    }

    private sealed class ProductionMeterRow
    {
        public int loomno { get; set; }
        public string? LOOMCODE { get; set; }
        public DateTime sysdate { get; set; }
        public double prodmtrA { get; set; }
        public double prodmtrB { get; set; }
        public double? ReqGSM { get; set; }
        public double? size { get; set; }
        public string? pono { get; set; }
        public string? partyname { get; set; }
    }

    private sealed class PpmSpecRow
    {
        public string? LoomType { get; set; }
        public double GSMFrom { get; set; }
        public double GSMTo { get; set; }
        public double WidthFrom { get; set; }
        public double WidthTo { get; set; }
        public double PPM { get; set; }
    }

    private sealed class FormulaRow
    {
        public int FormulaId { get; set; }
        public double Size { get; set; }
        public double? WarpMesh { get; set; }
        public double? WeftMesh { get; set; }
        public string? FormulaName { get; set; }
    }
}
