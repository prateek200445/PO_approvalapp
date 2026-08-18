using Dapper;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Fibc;

public sealed class FibcQuotationHoldRepository : IFibcQuotationHoldRepository
{
    private const int CommandTimeoutSeconds = 120;
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static volatile bool _schemaEnsured;

    private readonly DatabaseService _database;
    private readonly FibcPlanningOptions _options;

    public FibcQuotationHoldRepository(DatabaseService database, IOptions<FibcPlanningOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        if (_schemaEnsured)
            return;

        await SchemaLock.WaitAsync(ct);
        try
        {
            if (_schemaEnsured)
                return;

            ct.ThrowIfCancellationRequested();
            using var connection = _database.CreateConnection();

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FibcQuotationHold' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.FibcQuotationHold (
        HoldId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReferenceCode NVARCHAR(32) NOT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        OrderNo NVARCHAR(100) NOT NULL,
        PartyName NVARCHAR(200) NULL,
        MarketingNo NVARCHAR(100) NULL,
        BagType NVARCHAR(100) NULL,
        Quantity FLOAT NOT NULL,
        DispatchDate DATE NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FibcQuotationHold_Status DEFAULT ('Active'),
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_FibcQuotationHold_CreatedAt DEFAULT (GETDATE()),
        ExpiresAt DATETIME NOT NULL,
        ConfirmedAt DATETIME NULL,
        CancelledAt DATETIME NULL
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FibcQuotationHold_ReferenceCode' AND object_id = OBJECT_ID('dbo.FibcQuotationHold'))
    CREATE UNIQUE INDEX UX_FibcQuotationHold_ReferenceCode ON dbo.FibcQuotationHold(ReferenceCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FibcQuotationHold_Status' AND object_id = OBJECT_ID('dbo.FibcQuotationHold'))
    CREATE INDEX IX_FibcQuotationHold_Status ON dbo.FibcQuotationHold(Status, ExpiresAt);", commandTimeout: CommandTimeoutSeconds);

            // LineNum + [Shift]: avoid LineNo identifier; bracket Shift in T-SQL.
            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FibcQuotationHoldSlot' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.FibcQuotationHoldSlot (
        SlotId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HoldId INT NOT NULL,
        PlanDate DATE NOT NULL,
        LineNum NVARCHAR(20) NOT NULL,
        [Shift] NVARCHAR(10) NOT NULL,
        Qty FLOAT NOT NULL,
        Capacity FLOAT NOT NULL,
        AllocatedPercent FLOAT NULL,
        CONSTRAINT FK_FibcQuotationHoldSlot_Hold FOREIGN KEY (HoldId) REFERENCES dbo.FibcQuotationHold(HoldId)
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FibcQuotationHoldSlot_Lookup' AND object_id = OBJECT_ID('dbo.FibcQuotationHoldSlot'))
    CREATE INDEX IX_FibcQuotationHoldSlot_Lookup ON dbo.FibcQuotationHoldSlot(PlanDate, LineNum, [Shift]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FibcQuotationHoldSlot_HoldId' AND object_id = OBJECT_ID('dbo.FibcQuotationHoldSlot'))
    CREATE INDEX IX_FibcQuotationHoldSlot_HoldId ON dbo.FibcQuotationHoldSlot(HoldId);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FibcQuotationHold') AND name = 'ExpiryReminderSentAt')
    ALTER TABLE dbo.FibcQuotationHold ADD ExpiryReminderSentAt DATETIME NULL;", commandTimeout: CommandTimeoutSeconds);

            _schemaEnsured = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    public async Task ExpireStaleHoldsAsync(CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
UPDATE dbo.FibcQuotationHold
SET Status = 'Expired'
WHERE Status = 'Active' AND ExpiresAt < GETDATE()", commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<IReadOnlyList<FibcHoldReservationDto>> GetActiveHoldReservationsAsync(
        string companyName,
        DateTime dateFrom,
        DateTime dateTo,
        int? excludeHoldId = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        ct.ThrowIfCancellationRequested();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<HoldReservationRow>(@"
SELECT
    h.HoldId,
    h.OrderNo,
    h.ReferenceCode,
    s.PlanDate,
    s.LineNum,
    s.[Shift],
    s.Qty
FROM dbo.FibcQuotationHold h
INNER JOIN dbo.FibcQuotationHoldSlot s ON s.HoldId = h.HoldId
WHERE h.Status = 'Active'
  AND h.CompanyName = @CompanyName
  AND s.PlanDate >= @DateFrom
  AND s.PlanDate <= @DateTo
  AND (@ExcludeHoldId IS NULL OR h.HoldId <> @ExcludeHoldId)
ORDER BY s.PlanDate, s.LineNum, s.[Shift]", new
        {
            CompanyName = companyName,
            DateFrom = dateFrom.Date,
            DateTo = dateTo.Date,
            ExcludeHoldId = excludeHoldId,
        }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new FibcHoldReservationDto
        {
            HoldId = r.HoldId,
            OrderNo = r.OrderNo ?? "",
            ReferenceCode = r.ReferenceCode ?? "",
            PlanDate = r.PlanDate,
            LineNo = r.LineNum ?? "",
            Shift = r.Shift ?? "",
            Qty = r.Qty,
        }).ToList();
    }

    public async Task<IReadOnlyList<FibcQuotationHoldDto>> GetActiveHoldsAsync(
        string? companyName,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await ExpireStaleHoldsAsync(ct);
        ct.ThrowIfCancellationRequested();

        var company = string.IsNullOrWhiteSpace(companyName) ? _options.DefaultCompanyName : companyName.Trim();
        using var connection = _database.CreateConnection();
        var holds = (await connection.QueryAsync<HoldHeaderRow>(@"
SELECT
    HoldId, ReferenceCode, CompanyName, OrderNo, PartyName, MarketingNo, BagType,
    Quantity, DispatchDate, Status, Notes, CreatedAt, ExpiresAt, ConfirmedAt, CancelledAt
FROM dbo.FibcQuotationHold
WHERE Status = 'Active' AND CompanyName = @CompanyName
ORDER BY ExpiresAt, HoldId", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds)).ToList();

        return await LoadHoldsWithSlotsAsync(connection, holds);
    }

    public async Task<FibcQuotationHoldDto?> GetHoldAsync(int holdId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        ct.ThrowIfCancellationRequested();

        using var connection = _database.CreateConnection();
        var hold = await connection.QueryFirstOrDefaultAsync<HoldHeaderRow>(@"
SELECT
    HoldId, ReferenceCode, CompanyName, OrderNo, PartyName, MarketingNo, BagType,
    Quantity, DispatchDate, Status, Notes, CreatedAt, ExpiresAt, ConfirmedAt, CancelledAt
FROM dbo.FibcQuotationHold
WHERE HoldId = @HoldId", new { HoldId = holdId }, commandTimeout: CommandTimeoutSeconds);

        if (hold is null)
            return null;

        var list = await LoadHoldsWithSlotsAsync(connection, [hold]);
        return list.FirstOrDefault();
    }

    public async Task<FibcQuotationHoldDto> CreateHoldAsync(
        FibcQuotationHoldRequest request,
        IReadOnlyList<FibcSlotGridItemDto> proposedSlots,
        DateTime expiresAt,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        ct.ThrowIfCancellationRequested();

        var company = string.IsNullOrWhiteSpace(request.CompanyName)
            ? _options.DefaultCompanyName
            : request.CompanyName.Trim();
        var orderNo = request.OrderNo.Trim();

        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var holdId = await connection.ExecuteScalarAsync<int>(@"
INSERT INTO dbo.FibcQuotationHold
    (ReferenceCode, CompanyName, OrderNo, PartyName, MarketingNo, BagType, Quantity, DispatchDate, Status, Notes, ExpiresAt)
VALUES
    (@ReferenceCode, @CompanyName, @OrderNo, @PartyName, @MarketingNo, @BagType, @Quantity, @DispatchDate, 'Active', @Notes, @ExpiresAt);
SELECT CAST(SCOPE_IDENTITY() AS INT);", new
            {
                ReferenceCode = "PENDING",
                CompanyName = company,
                OrderNo = orderNo,
                PartyName = request.PartyName,
                MarketingNo = request.MarketingNo,
                BagType = request.BagType,
                Quantity = request.Quantity,
                DispatchDate = request.DispatchDate?.Date,
                Notes = request.Notes,
                ExpiresAt = expiresAt,
            }, transaction, commandTimeout: CommandTimeoutSeconds);

            var referenceCode = $"QH-{DateTime.UtcNow:yyyyMMdd}-{holdId:D4}";
            await connection.ExecuteAsync(@"
UPDATE dbo.FibcQuotationHold SET ReferenceCode = @ReferenceCode WHERE HoldId = @HoldId", new
            {
                ReferenceCode = referenceCode,
                HoldId = holdId,
            }, transaction, commandTimeout: CommandTimeoutSeconds);

            foreach (var slot in proposedSlots)
            {
                var allocatedPercent = slot.AllocatedPercent
                    ?? (slot.Capacity > 0 ? Math.Round(slot.Allotted / slot.Capacity * 100, 2) : 100d);

                await connection.ExecuteAsync(@"
INSERT INTO dbo.FibcQuotationHoldSlot
    (HoldId, PlanDate, LineNum, [Shift], Qty, Capacity, AllocatedPercent)
VALUES
    (@HoldId, @PlanDate, @LineNum, @Shift, @Qty, @Capacity, @AllocatedPercent)", new
                {
                    HoldId = holdId,
                    PlanDate = slot.PlanDate.Date,
                    LineNum = slot.LineNo,
                    Shift = slot.Shift,
                    Qty = slot.Allotted,
                    Capacity = slot.Capacity,
                    AllocatedPercent = allocatedPercent,
                }, transaction, commandTimeout: CommandTimeoutSeconds);
            }

            transaction.Commit();

            var created = await GetHoldAsync(holdId, ct);
            return created ?? throw new InvalidOperationException("Failed to load hold after create.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> MarkHoldConfirmedAsync(int holdId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var rows = await connection.ExecuteAsync(@"
UPDATE dbo.FibcQuotationHold
SET Status = 'Confirmed', ConfirmedAt = GETDATE()
WHERE HoldId = @HoldId AND Status = 'Active'", new { HoldId = holdId }, commandTimeout: CommandTimeoutSeconds);
        return rows > 0;
    }

    public async Task<bool> MarkHoldCancelledAsync(int holdId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var rows = await connection.ExecuteAsync(@"
UPDATE dbo.FibcQuotationHold
SET Status = 'Cancelled', CancelledAt = GETDATE()
WHERE HoldId = @HoldId AND Status = 'Active'", new { HoldId = holdId }, commandTimeout: CommandTimeoutSeconds);
        return rows > 0;
    }

    public async Task<IReadOnlyList<FibcQuotationHoldDto>> GetHoldsNeedingExpiryReminderAsync(
        int withinDays,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        ct.ThrowIfCancellationRequested();

        var days = withinDays > 0 ? withinDays : 1;
        using var connection = _database.CreateConnection();
        var holds = (await connection.QueryAsync<HoldHeaderRow>(@"
SELECT
    HoldId, ReferenceCode, CompanyName, OrderNo, PartyName, MarketingNo, BagType,
    Quantity, DispatchDate, Status, Notes, CreatedAt, ExpiresAt, ConfirmedAt, CancelledAt
FROM dbo.FibcQuotationHold
WHERE Status = 'Active'
  AND ExpiresAt > GETDATE()
  AND ExpiresAt <= DATEADD(day, @WithinDays, GETDATE())
  AND ExpiryReminderSentAt IS NULL
ORDER BY ExpiresAt, HoldId", new { WithinDays = days }, commandTimeout: CommandTimeoutSeconds)).ToList();

        return await LoadHoldsWithSlotsAsync(connection, holds);
    }

    public async Task MarkExpiryReminderSentAsync(int holdId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
UPDATE dbo.FibcQuotationHold
SET ExpiryReminderSentAt = GETDATE()
WHERE HoldId = @HoldId AND ExpiryReminderSentAt IS NULL", new { HoldId = holdId }, commandTimeout: CommandTimeoutSeconds);
    }

    private async Task<IReadOnlyList<FibcQuotationHoldDto>> LoadHoldsWithSlotsAsync(
        System.Data.IDbConnection connection,
        IReadOnlyList<HoldHeaderRow> holds)
    {
        if (holds.Count == 0)
            return Array.Empty<FibcQuotationHoldDto>();

        var holdIds = holds.Select(h => h.HoldId).ToList();
        var slots = (await connection.QueryAsync<HoldSlotRow>(@"
SELECT HoldId, PlanDate, LineNum, [Shift], Qty, Capacity, AllocatedPercent
FROM dbo.FibcQuotationHoldSlot
WHERE HoldId IN @HoldIds
ORDER BY PlanDate, LineNum, [Shift]", new { HoldIds = holdIds }, commandTimeout: CommandTimeoutSeconds)).ToList();

        var slotsByHold = slots.GroupBy(s => s.HoldId).ToDictionary(g => g.Key, g => g.ToList());

        return holds.Select(h =>
        {
            var bagType = h.BagType ?? "";
            var holdSlots = slotsByHold.GetValueOrDefault(h.HoldId) ?? [];
            return new FibcQuotationHoldDto
            {
                HoldId = h.HoldId,
                ReferenceCode = h.ReferenceCode ?? "",
                CompanyName = h.CompanyName ?? "",
                OrderNo = h.OrderNo ?? "",
                PartyName = h.PartyName,
                MarketingNo = h.MarketingNo,
                BagType = bagType,
                BagTypeLabel = BagTypeMapper.ToDisplayLabel(bagType),
                Quantity = h.Quantity,
                DispatchDate = h.DispatchDate,
                Status = h.Status ?? "",
                Notes = h.Notes,
                CreatedAt = h.CreatedAt,
                ExpiresAt = h.ExpiresAt,
                ConfirmedAt = h.ConfirmedAt,
                CancelledAt = h.CancelledAt,
                Slots = holdSlots.Select(s => new FibcQuotationHoldSlotDto
                {
                    PlanDate = s.PlanDate,
                    LineNo = s.LineNum ?? "",
                    Shift = s.Shift ?? "",
                    Qty = s.Qty,
                    Capacity = s.Capacity,
                    AllocatedPercent = s.AllocatedPercent,
                }).ToList(),
            };
        }).ToList();
    }

    private sealed class HoldHeaderRow
    {
        public int HoldId { get; set; }
        public string? ReferenceCode { get; set; }
        public string? CompanyName { get; set; }
        public string? OrderNo { get; set; }
        public string? PartyName { get; set; }
        public string? MarketingNo { get; set; }
        public string? BagType { get; set; }
        public double Quantity { get; set; }
        public DateTime? DispatchDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    private sealed class HoldSlotRow
    {
        public int HoldId { get; set; }
        public DateTime PlanDate { get; set; }
        public string? LineNum { get; set; }
        public string? Shift { get; set; }
        public double Qty { get; set; }
        public double Capacity { get; set; }
        public double? AllocatedPercent { get; set; }
    }

    private sealed class HoldReservationRow
    {
        public int HoldId { get; set; }
        public string? OrderNo { get; set; }
        public string? ReferenceCode { get; set; }
        public DateTime PlanDate { get; set; }
        public string? LineNum { get; set; }
        public string? Shift { get; set; }
        public double Qty { get; set; }
    }
}
