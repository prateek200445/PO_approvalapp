using Dapper;
using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Loom;
using POApprovalAPI.Planning.Setup.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Setup;

public sealed class PlanningSetupRepository : IPlanningSetupRepository
{
    private const int CommandTimeoutSeconds = 120;
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static volatile bool _schemaEnsured;

    private readonly DatabaseService _database;
    private readonly FibcPlanningOptions _fibcOptions;
    private readonly LoomPlanningOptions _loomOptions;

    public PlanningSetupRepository(
        DatabaseService database,
        IOptions<FibcPlanningOptions> fibcOptions,
        IOptions<LoomPlanningOptions> loomOptions)
    {
        _database = database;
        _fibcOptions = fibcOptions.Value;
        _loomOptions = loomOptions.Value;
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
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningFactoryConfig' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningFactoryConfig (
        ConfigId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FactoryInfoSrNo INT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        IsPlanningEnabled BIT NOT NULL CONSTRAINT DF_PlanningFactoryConfig_Enabled DEFAULT (1),
        DefaultDispatchBufferDays INT NOT NULL CONSTRAINT DF_PlanningFactoryConfig_Buffer DEFAULT (7),
        DefaultRejectionPercent FLOAT NOT NULL CONSTRAINT DF_PlanningFactoryConfig_Rejection DEFAULT (2.5),
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningFactoryConfig_Created DEFAULT (GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningFactoryConfig_Updated DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanningFactoryConfig_Company' AND object_id = OBJECT_ID('dbo.PlanningFactoryConfig'))
    CREATE UNIQUE INDEX UX_PlanningFactoryConfig_Company ON dbo.PlanningFactoryConfig(CompanyName);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningLineConfig' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningLineConfig (
        LineConfigId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        [LineNo] INT NOT NULL,
        DisplayName NVARCHAR(100) NULL,
        ErpBagType NVARCHAR(100) NULL,
        AllowedBagFamilies NVARCHAR(500) NULL,
        CapacityNormal INT NULL,
        CapacitySingleDust INT NULL,
        CapacityDoubleDust INT NULL,
        CapacityTripleDust INT NULL,
        BufferDaysOverride INT NULL,
        TeamNo NVARCHAR(50) NULL,
        ContractorCode INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PlanningLineConfig_Active DEFAULT (1),
        PreferenceOrder INT NOT NULL CONSTRAINT DF_PlanningLineConfig_Pref DEFAULT (0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningLineConfig_Updated DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanningLineConfig_Line' AND object_id = OBJECT_ID('dbo.PlanningLineConfig'))
    CREATE UNIQUE INDEX UX_PlanningLineConfig_Line ON dbo.PlanningLineConfig(CompanyName, [LineNo]);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningLoomPool' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningLoomPool (
        PoolId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        LoomNo INT NOT NULL,
        ErpLoomCode NVARCHAR(50) NULL,
        IncludeInPlanning BIT NOT NULL CONSTRAINT DF_PlanningLoomPool_Include DEFAULT (0),
        PoolPurpose NVARCHAR(50) NOT NULL CONSTRAINT DF_PlanningLoomPool_Purpose DEFAULT ('DomesticFibc'),
        LoomType NVARCHAR(50) NULL,
        WinderCategory NVARCHAR(20) NOT NULL CONSTRAINT DF_PlanningLoomPool_Winder DEFAULT ('Tube'),
        GsmMin FLOAT NULL,
        GsmMax FLOAT NULL,
        WidthMinCm FLOAT NULL,
        WidthMaxCm FLOAT NULL,
        Notes NVARCHAR(500) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningLoomPool_Updated DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanningLoomPool_Loom' AND object_id = OBJECT_ID('dbo.PlanningLoomPool'))
    CREATE UNIQUE INDEX UX_PlanningLoomPool_Loom ON dbo.PlanningLoomPool(CompanyName, LoomNo);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningTeamFactor' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningTeamFactor (
        FactorId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        [LineNo] INT NOT NULL CONSTRAINT DF_PlanningTeamFactor_Line DEFAULT (0),
        [Shift] NVARCHAR(10) NULL,
        TeamNo NVARCHAR(50) NOT NULL,
        ManualFactor FLOAT NULL,
        AutoFactor FLOAT NULL,
        EffectiveFactor FLOAT NOT NULL CONSTRAINT DF_PlanningTeamFactor_Effective DEFAULT (1),
        FactorSource NVARCHAR(20) NOT NULL CONSTRAINT DF_PlanningTeamFactor_Source DEFAULT ('Default'),
        SampleDays INT NOT NULL CONSTRAINT DF_PlanningTeamFactor_SampleDays DEFAULT (30),
        SampleProductionPcs FLOAT NULL,
        SamplePlannedCapacity FLOAT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningTeamFactor_Updated DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanningTeamFactor_Key' AND object_id = OBJECT_ID('dbo.PlanningTeamFactor'))
    CREATE UNIQUE INDEX UX_PlanningTeamFactor_Key ON dbo.PlanningTeamFactor(CompanyName, [LineNo], [Shift], TeamNo);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningBacklog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningBacklog (
        BacklogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        [LineNo] INT NOT NULL,
        [Shift] NVARCHAR(10) NOT NULL,
        OrderNo NVARCHAR(100) NOT NULL,
        BacklogQty FLOAT NOT NULL,
        Reason NVARCHAR(200) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PlanningBacklog_Status DEFAULT ('Open'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningBacklog_Created DEFAULT (GETDATE()),
        ClearedAt DATETIME NULL
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanningBacklog_Open' AND object_id = OBJECT_ID('dbo.PlanningBacklog'))
    CREATE INDEX IX_PlanningBacklog_Open ON dbo.PlanningBacklog(CompanyName, [LineNo], [Shift], Status);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningDowntime' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningDowntime (
        DowntimeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        PlanDate DATE NOT NULL,
        [LineNo] INT NOT NULL CONSTRAINT DF_PlanningDowntime_Line DEFAULT (0),
        [Shift] NVARCHAR(10) NULL,
        Reason NVARCHAR(200) NOT NULL,
        CapacityFactor FLOAT NOT NULL CONSTRAINT DF_PlanningDowntime_Factor DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningDowntime_Created DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanningDowntime_Lookup' AND object_id = OBJECT_ID('dbo.PlanningDowntime'))
    CREATE INDEX IX_PlanningDowntime_Lookup ON dbo.PlanningDowntime(CompanyName, PlanDate, [LineNo]);", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanningLoomPreferenceChart' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlanningLoomPreferenceChart (
        ChartId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        FabricForm NVARCHAR(20) NOT NULL,
        GsmMin FLOAT NOT NULL,
        GsmMax FLOAT NOT NULL,
        WidthMinCm FLOAT NOT NULL,
        WidthMaxCm FLOAT NOT NULL,
        PreferenceRank INT NOT NULL CONSTRAINT DF_PlanningLoomPref_Rank DEFAULT (1),
        LoomType NVARCHAR(50) NOT NULL,
        WinderCategory NVARCHAR(20) NULL,
        ChangeoverTier NVARCHAR(20) NOT NULL CONSTRAINT DF_PlanningLoomPref_Tier DEFAULT ('Blue'),
        Notes NVARCHAR(200) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PlanningLoomPref_Updated DEFAULT (GETDATE())
    );
END;", commandTimeout: CommandTimeoutSeconds);

            await connection.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanningLoomPref_Lookup' AND object_id = OBJECT_ID('dbo.PlanningLoomPreferenceChart'))
    CREATE INDEX IX_PlanningLoomPref_Lookup ON dbo.PlanningLoomPreferenceChart(CompanyName, FabricForm, PreferenceRank);", commandTimeout: CommandTimeoutSeconds);

            _schemaEnsured = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    public async Task<IReadOnlyList<PlanningFactoryOptionDto>> SearchFactoriesAsync(string? query, int limit = 25, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);

        var q = (query ?? "").Trim();
        limit = Math.Clamp(limit, 1, 50);

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<FactorySearchRow>(@"
SELECT TOP (@Limit)
    fi.srno AS FactoryInfoSrNo,
    fi.Name AS CompanyName,
    fi.GroupName,
    CASE WHEN pfc.IsPlanningEnabled = 1 THEN 1 ELSE 0 END AS IsPlanningEnabled,
    CASE WHEN EXISTS (SELECT 1 FROM NewLineMaster nl WITH (NOLOCK) WHERE nl.CompanyName = fi.Name) THEN 1 ELSE 0 END AS HasLineMaster,
    CASE WHEN EXISTS (SELECT 1 FROM NewMISLoomMaster lm WITH (NOLOCK) WHERE lm.CompanyName = fi.Name) THEN 1 ELSE 0 END AS HasLoomMaster
FROM FactoryInfo fi WITH (NOLOCK)
LEFT JOIN PlanningFactoryConfig pfc WITH (NOLOCK) ON pfc.CompanyName = fi.Name
WHERE (@Query = '' OR fi.Name LIKE @Like OR fi.GroupName LIKE @Like)
ORDER BY
    CASE WHEN pfc.IsPlanningEnabled = 1 THEN 0 ELSE 1 END,
    fi.Name", new
        {
            Limit = limit,
            Query = q,
            Like = $"%{q}%",
        }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new PlanningFactoryOptionDto
        {
            FactoryInfoSrNo = r.FactoryInfoSrNo,
            CompanyName = r.CompanyName ?? "",
            GroupName = r.GroupName,
            IsPlanningEnabled = r.IsPlanningEnabled != 0,
            HasLineMaster = r.HasLineMaster != 0,
            HasLoomMaster = r.HasLoomMaster != 0,
        }).ToList();
    }

    public async Task<IReadOnlyList<PlanningFactoryConfigDto>> GetEnabledFactoriesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<FactoryConfigRow>(@"
SELECT ConfigId, FactoryInfoSrNo, CompanyName, IsPlanningEnabled, DefaultDispatchBufferDays,
       DefaultRejectionPercent, Notes, UpdatedAt
FROM PlanningFactoryConfig WITH (NOLOCK)
WHERE IsPlanningEnabled = 1
ORDER BY CompanyName", commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapFactoryConfig).ToList();
    }

    public async Task<PlanningFactoryConfigDto?> GetFactoryConfigAsync(string companyName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();

        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<FactoryConfigRow>(@"
SELECT ConfigId, FactoryInfoSrNo, CompanyName, IsPlanningEnabled, DefaultDispatchBufferDays,
       DefaultRejectionPercent, Notes, UpdatedAt
FROM PlanningFactoryConfig WITH (NOLOCK)
WHERE CompanyName = @CompanyName", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return row is null ? null : MapFactoryConfig(row);
    }

    public async Task<PlanningFactoryConfigDto> UpsertFactoryConfigAsync(UpsertPlanningFactoryConfigRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();
        if (string.IsNullOrEmpty(company))
            throw new InvalidOperationException("Company name is required.");

        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
MERGE dbo.PlanningFactoryConfig AS t
USING (SELECT @CompanyName AS CompanyName) AS s ON t.CompanyName = s.CompanyName
WHEN MATCHED THEN UPDATE SET
    FactoryInfoSrNo = @FactoryInfoSrNo,
    IsPlanningEnabled = @IsPlanningEnabled,
    DefaultDispatchBufferDays = @DefaultDispatchBufferDays,
    DefaultRejectionPercent = @DefaultRejectionPercent,
    Notes = @Notes,
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (FactoryInfoSrNo, CompanyName, IsPlanningEnabled, DefaultDispatchBufferDays, DefaultRejectionPercent, Notes)
VALUES
    (@FactoryInfoSrNo, @CompanyName, @IsPlanningEnabled, @DefaultDispatchBufferDays, @DefaultRejectionPercent, @Notes);",
            new
            {
                FactoryInfoSrNo = request.FactoryInfoSrNo,
                CompanyName = company,
                IsPlanningEnabled = request.IsPlanningEnabled,
                DefaultDispatchBufferDays = Math.Max(0, request.DefaultDispatchBufferDays),
                DefaultRejectionPercent = Math.Clamp(request.DefaultRejectionPercent, 0, 50),
                Notes = request.Notes,
            }, commandTimeout: CommandTimeoutSeconds);

        return (await GetFactoryConfigAsync(company, ct))!;
    }

    public async Task<IReadOnlyList<PlanningLineConfigDto>> GetMergedLineConfigsAsync(string companyName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = companyName.Trim();
        var erpLines = await LoadErpLinesAsync(company, ct);
        var portalLines = await LoadPortalLinesAsync(company, ct);
        var portalByLine = portalLines.ToDictionary(l => l.LineNo);

        var merged = new List<PlanningLineConfigDto>();
        foreach (var erp in erpLines)
        {
            if (portalByLine.TryGetValue(erp.LineNo, out var portal))
            {
                merged.Add(MergeLine(erp, portal, fromPortal: true));
                portalByLine.Remove(erp.LineNo);
            }
            else
            {
                merged.Add(erp);
            }
        }

        merged.AddRange(portalByLine.Values.OrderBy(l => l.LineNo));
        return merged.OrderBy(l => l.LineNo).ToList();
    }

    public async Task<IReadOnlyList<PlanningLineConfigDto>> ImportLinesFromErpAsync(string companyName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = companyName.Trim();
        var erpLines = await LoadErpLinesAsync(company, ct);
        if (erpLines.Count == 0)
            return erpLines;

        await SaveLineConfigsAsync(new SavePlanningLineConfigsRequest
        {
            CompanyName = company,
            Lines = erpLines,
        }, ct);

        return await GetMergedLineConfigsAsync(company, ct);
    }

    public async Task SaveLineConfigsAsync(SavePlanningLineConfigsRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();

        using var connection = _database.CreateConnection();
        foreach (var line in request.Lines)
        {
            var families = line.AllowedBagFamilies?.Count > 0
                ? line.AllowedBagFamilies
                : PlanningSetupHelpers.InferBagFamilies(line.ErpBagType);

            await connection.ExecuteAsync(@"
MERGE dbo.PlanningLineConfig AS t
USING (SELECT @CompanyName AS CompanyName, @LineNo AS [LineNo]) AS s
    ON t.CompanyName = s.CompanyName AND t.[LineNo] = s.[LineNo]
WHEN MATCHED THEN UPDATE SET
    DisplayName = @DisplayName,
    ErpBagType = @ErpBagType,
    AllowedBagFamilies = @AllowedBagFamilies,
    CapacityNormal = @CapacityNormal,
    CapacitySingleDust = @CapacitySingleDust,
    CapacityDoubleDust = @CapacityDoubleDust,
    CapacityTripleDust = @CapacityTripleDust,
    BufferDaysOverride = @BufferDaysOverride,
    TeamNo = @TeamNo,
    ContractorCode = @ContractorCode,
    IsActive = @IsActive,
    PreferenceOrder = @PreferenceOrder,
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (CompanyName, [LineNo], DisplayName, ErpBagType, AllowedBagFamilies, CapacityNormal, CapacitySingleDust,
     CapacityDoubleDust, CapacityTripleDust, BufferDaysOverride, TeamNo, ContractorCode, IsActive, PreferenceOrder)
VALUES
    (@CompanyName, @LineNo, @DisplayName, @ErpBagType, @AllowedBagFamilies, @CapacityNormal, @CapacitySingleDust,
     @CapacityDoubleDust, @CapacityTripleDust, @BufferDaysOverride, @TeamNo, @ContractorCode, @IsActive, @PreferenceOrder);",
                new
                {
                    CompanyName = company,
                    line.LineNo,
                    DisplayName = line.DisplayName,
                    ErpBagType = line.ErpBagType,
                    AllowedBagFamilies = PlanningSetupHelpers.SerializeFamilies(families),
                    line.CapacityNormal,
                    line.CapacitySingleDust,
                    line.CapacityDoubleDust,
                    line.CapacityTripleDust,
                    line.BufferDaysOverride,
                    TeamNo = string.IsNullOrWhiteSpace(line.TeamNo) ? null : line.TeamNo.Trim(),
                    line.ContractorCode,
                    IsActive = line.IsActive,
                    PreferenceOrder = line.PreferenceOrder,
                }, commandTimeout: CommandTimeoutSeconds);
        }
    }

    public async Task<IReadOnlyList<PlanningLoomPoolDto>> GetMergedLoomPoolAsync(string companyName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = companyName.Trim();
        var erpLooms = await LoadErpLoomsAsync(company, ct);
        var portalLooms = await LoadPortalLoomsAsync(company, ct);
        var portalByLoom = portalLooms.ToDictionary(l => l.LoomNo);

        var merged = new List<PlanningLoomPoolDto>();
        foreach (var erp in erpLooms)
        {
            if (portalByLoom.TryGetValue(erp.LoomNo, out var portal))
            {
                merged.Add(MergeLoom(erp, portal));
                portalByLoom.Remove(erp.LoomNo);
            }
            else
            {
                merged.Add(erp);
            }
        }

        merged.AddRange(portalByLoom.Values.OrderBy(l => l.LoomNo));
        return merged.OrderBy(l => l.LoomNo).ToList();
    }

    public async Task SaveLoomPoolAsync(SavePlanningLoomPoolRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();

        using var connection = _database.CreateConnection();
        foreach (var loom in request.Looms)
        {
            var purpose = string.IsNullOrWhiteSpace(loom.PoolPurpose) ? "DomesticFibc" : loom.PoolPurpose.Trim();
            var winder = string.IsNullOrWhiteSpace(loom.WinderCategory) ? "Tube" : loom.WinderCategory.Trim();

            await connection.ExecuteAsync(@"
MERGE dbo.PlanningLoomPool AS t
USING (SELECT @CompanyName AS CompanyName, @LoomNo AS LoomNo) AS s
    ON t.CompanyName = s.CompanyName AND t.LoomNo = s.LoomNo
WHEN MATCHED THEN UPDATE SET
    ErpLoomCode = @ErpLoomCode,
    IncludeInPlanning = @IncludeInPlanning,
    PoolPurpose = @PoolPurpose,
    LoomType = @LoomType,
    WinderCategory = @WinderCategory,
    GsmMin = @GsmMin,
    GsmMax = @GsmMax,
    WidthMinCm = @WidthMinCm,
    WidthMaxCm = @WidthMaxCm,
    Notes = @Notes,
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (CompanyName, LoomNo, ErpLoomCode, IncludeInPlanning, PoolPurpose, LoomType, WinderCategory,
     GsmMin, GsmMax, WidthMinCm, WidthMaxCm, Notes)
VALUES
    (@CompanyName, @LoomNo, @ErpLoomCode, @IncludeInPlanning, @PoolPurpose, @LoomType, @WinderCategory,
     @GsmMin, @GsmMax, @WidthMinCm, @WidthMaxCm, @Notes);",
                new
                {
                    CompanyName = company,
                    loom.LoomNo,
                    ErpLoomCode = loom.ErpLoomCode,
                    IncludeInPlanning = loom.IncludeInPlanning,
                    PoolPurpose = purpose,
                    LoomType = loom.LoomType,
                    WinderCategory = winder,
                    loom.GsmMin,
                    loom.GsmMax,
                    loom.WidthMinCm,
                    loom.WidthMaxCm,
                    loom.Notes,
                }, commandTimeout: CommandTimeoutSeconds);
        }
    }

    public async Task<IReadOnlyList<PlanningTeamFactorDto>> GetTeamFactorsAsync(string companyName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<TeamFactorRow>(@"
SELECT FactorId, CompanyName, [LineNo], [Shift], TeamNo, ManualFactor, AutoFactor, EffectiveFactor,
       FactorSource, SampleDays, SampleProductionPcs, SamplePlannedCapacity, UpdatedAt
FROM PlanningTeamFactor WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY [LineNo], TeamNo, [Shift]", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapTeamFactor).ToList();
    }

    public async Task SaveTeamFactorsAsync(SavePlanningTeamFactorRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();

        using var connection = _database.CreateConnection();
        foreach (var factor in request.Factors)
        {
            var shift = string.IsNullOrWhiteSpace(factor.Shift) ? "" : factor.Shift.Trim().ToUpperInvariant();
            var teamNo = factor.TeamNo.Trim();
            if (string.IsNullOrEmpty(teamNo))
                continue;

            var effective = PlanningSetupHelpers.ResolveEffectiveFactor(factor.ManualFactor, factor.AutoFactor);
            var source = PlanningSetupHelpers.ResolveEffectiveFactorSource(factor.ManualFactor, factor.AutoFactor);

            await connection.ExecuteAsync(@"
MERGE dbo.PlanningTeamFactor AS t
USING (SELECT @CompanyName AS CompanyName, @LineNo AS [LineNo], @Shift AS [Shift], @TeamNo AS TeamNo) AS s
    ON t.CompanyName = s.CompanyName AND t.[LineNo] = s.[LineNo] AND ISNULL(t.[Shift], '') = s.[Shift] AND t.TeamNo = s.TeamNo
WHEN MATCHED THEN UPDATE SET
    ManualFactor = @ManualFactor,
    AutoFactor = @AutoFactor,
    EffectiveFactor = @EffectiveFactor,
    FactorSource = @FactorSource,
    SampleDays = @SampleDays,
    SampleProductionPcs = @SampleProductionPcs,
    SamplePlannedCapacity = @SamplePlannedCapacity,
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (CompanyName, [LineNo], [Shift], TeamNo, ManualFactor, AutoFactor, EffectiveFactor, FactorSource,
     SampleDays, SampleProductionPcs, SamplePlannedCapacity)
VALUES
    (@CompanyName, @LineNo, NULLIF(@Shift, ''), @TeamNo, @ManualFactor, @AutoFactor, @EffectiveFactor, @FactorSource,
     @SampleDays, @SampleProductionPcs, @SamplePlannedCapacity);",
                new
                {
                    CompanyName = company,
                    LineNo = factor.LineNo,
                    Shift = shift,
                    TeamNo = teamNo,
                    factor.ManualFactor,
                    factor.AutoFactor,
                    EffectiveFactor = effective,
                    FactorSource = source,
                    SampleDays = factor.SampleDays > 0 ? factor.SampleDays : 30,
                    factor.SampleProductionPcs,
                    factor.SamplePlannedCapacity,
                }, commandTimeout: CommandTimeoutSeconds);
        }
    }

    public async Task<int> RecalculateTeamFactorsFromErpAsync(string companyName, int sampleDays, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();
        sampleDays = Math.Clamp(sampleDays, 7, 90);
        var fromDate = DateTime.Today.AddDays(-sampleDays);

        var lineConfigs = await LoadPortalLinesAsync(company, ct);
        var lineByTeam = lineConfigs
            .Where(l => !string.IsNullOrWhiteSpace(l.TeamNo))
            .GroupBy(l => l.TeamNo!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        using var connection = _database.CreateConnection();
        var productionRows = (await connection.QueryAsync<TeamProductionRow>(@"
SELECT
    LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))) AS TeamNo,
    LTRIM(RTRIM(CAST([Shift] AS NVARCHAR(10)))) AS [Shift],
    SUM(CAST(BagPCS AS FLOAT)) AS TotalPcs,
    COUNT(DISTINCT CAST(Sysdate AS DATE)) AS ActiveDays
FROM FIBCTeamWiseProduction WITH (NOLOCK)
WHERE CompanyName LIKE @CompanyLike
  AND Sysdate >= @FromDate
  AND TeamNo IS NOT NULL
  AND LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))) <> ''
GROUP BY LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))), LTRIM(RTRIM(CAST([Shift] AS NVARCHAR(10))))",
            new { CompanyLike = $"%{company.Trim()}%", FromDate = fromDate },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        var existing = (await GetTeamFactorsAsync(company, ct))
            .ToDictionary(f => FactorKey(f.LineNo, f.Shift, f.TeamNo), StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        foreach (var row in productionRows)
        {
            if (row.ActiveDays <= 0 || row.TotalPcs <= 0)
                continue;

            var teamNo = row.TeamNo?.Trim() ?? "";
            var shift = row.Shift?.Trim().ToUpperInvariant() ?? "";
            var lineNo = 0;
            int? capacityNormal = null;

            if (lineByTeam.TryGetValue(teamNo, out var line))
            {
                lineNo = line.LineNo;
                capacityNormal = line.CapacityNormal ?? line.ErpBagCapacity;
            }

            var avgDailyPcs = row.TotalPcs / row.ActiveDays;
            double? autoFactor = null;
            if (capacityNormal is > 0)
                autoFactor = Math.Round(avgDailyPcs / capacityNormal.Value, 3);

            var key = FactorKey(lineNo, shift, teamNo);
            existing.TryGetValue(key, out var prior);
            var manual = prior?.ManualFactor;

            var dto = new PlanningTeamFactorDto
            {
                CompanyName = company,
                LineNo = lineNo,
                Shift = string.IsNullOrEmpty(shift) ? null : shift,
                TeamNo = teamNo,
                ManualFactor = manual,
                AutoFactor = autoFactor,
                EffectiveFactor = PlanningSetupHelpers.ResolveEffectiveFactor(manual, autoFactor),
                FactorSource = PlanningSetupHelpers.ResolveEffectiveFactorSource(manual, autoFactor),
                SampleDays = sampleDays,
                SampleProductionPcs = Math.Round(avgDailyPcs, 1),
                SamplePlannedCapacity = capacityNormal,
            };

            await SaveTeamFactorsAsync(new SavePlanningTeamFactorRequest
            {
                CompanyName = company,
                Factors = [dto],
            }, ct);
            updated++;
        }

        return updated;
    }

    public async Task<IReadOnlyList<PlanningBacklogDto>> GetBacklogAsync(string companyName, string? status, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();
        var statusFilter = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<BacklogRow>(@"
SELECT BacklogId, CompanyName, [LineNo], [Shift], OrderNo, BacklogQty, Reason, Status, CreatedAt, ClearedAt
FROM PlanningBacklog WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND (@Status IS NULL OR Status = @Status)
ORDER BY CreatedAt DESC, BacklogId DESC", new { CompanyName = company, Status = statusFilter }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(MapBacklog).ToList();
    }

    public async Task<PlanningBacklogDto> CreateBacklogAsync(CreatePlanningBacklogRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);

        var company = request.CompanyName.Trim();
        var shift = request.Shift.Trim().ToUpperInvariant();
        var orderNo = request.OrderNo.Trim();
        if (string.IsNullOrEmpty(company) || string.IsNullOrEmpty(shift) || string.IsNullOrEmpty(orderNo))
            throw new InvalidOperationException("Company, shift, and order number are required.");
        if (request.BacklogQty <= 0)
            throw new InvalidOperationException("Backlog quantity must be greater than zero.");

        using var connection = _database.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(@"
INSERT INTO PlanningBacklog (CompanyName, [LineNo], [Shift], OrderNo, BacklogQty, Reason, Status)
VALUES (@CompanyName, @LineNo, @Shift, @OrderNo, @BacklogQty, @Reason, 'Open');
SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                CompanyName = company,
                request.LineNo,
                Shift = shift,
                OrderNo = orderNo,
                request.BacklogQty,
                request.Reason,
            }, commandTimeout: CommandTimeoutSeconds);

        var items = await GetBacklogAsync(company, null, ct);
        return items.First(b => b.BacklogId == id);
    }

    public async Task<bool> ClearBacklogAsync(int backlogId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);

        using var connection = _database.CreateConnection();
        var rows = await connection.ExecuteAsync(@"
UPDATE PlanningBacklog
SET Status = 'Cleared', ClearedAt = GETDATE()
WHERE BacklogId = @BacklogId AND Status = 'Open'", new { BacklogId = backlogId }, commandTimeout: CommandTimeoutSeconds);

        return rows > 0;
    }

    public async Task<IReadOnlyList<PlanningDowntimeDto>> GetDowntimeAsync(
        string companyName,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<DowntimeRow>(@"
SELECT DowntimeId, CompanyName, PlanDate, [LineNo], [Shift], Reason, CapacityFactor
FROM PlanningDowntime WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND (@From IS NULL OR PlanDate >= @From)
  AND (@To IS NULL OR PlanDate <= @To)
ORDER BY PlanDate DESC, [LineNo]", new
        {
            CompanyName = company,
            From = from?.Date,
            To = to?.Date,
        }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new PlanningDowntimeDto
        {
            DowntimeId = r.DowntimeId,
            CompanyName = r.CompanyName ?? company,
            PlanDate = r.PlanDate,
            LineNo = r.LineNo,
            Shift = r.Shift,
            Reason = r.Reason ?? "",
            CapacityFactor = r.CapacityFactor,
        }).ToList();
    }

    public async Task SaveDowntimeAsync(SavePlanningDowntimeRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();

        using var connection = _database.CreateConnection();
        foreach (var entry in request.Entries)
        {
            if (entry.DowntimeId > 0)
            {
                await connection.ExecuteAsync(@"
UPDATE PlanningDowntime
SET PlanDate = @PlanDate, [LineNo] = @LineNo, [Shift] = @Shift, Reason = @Reason, CapacityFactor = @CapacityFactor
WHERE DowntimeId = @DowntimeId AND CompanyName = @CompanyName", new
                {
                    entry.DowntimeId,
                    CompanyName = company,
                    PlanDate = entry.PlanDate.Date,
                    entry.LineNo,
                    Shift = string.IsNullOrWhiteSpace(entry.Shift) ? null : entry.Shift.Trim().ToUpperInvariant(),
                    entry.Reason,
                    CapacityFactor = Math.Clamp(entry.CapacityFactor, 0, 1),
                }, commandTimeout: CommandTimeoutSeconds);
            }
            else
            {
                await connection.ExecuteAsync(@"
INSERT INTO PlanningDowntime (CompanyName, PlanDate, [LineNo], [Shift], Reason, CapacityFactor)
VALUES (@CompanyName, @PlanDate, @LineNo, @Shift, @Reason, @CapacityFactor)", new
                {
                    CompanyName = company,
                    PlanDate = entry.PlanDate.Date,
                    entry.LineNo,
                    Shift = string.IsNullOrWhiteSpace(entry.Shift) ? null : entry.Shift.Trim().ToUpperInvariant(),
                    entry.Reason,
                    CapacityFactor = Math.Clamp(entry.CapacityFactor, 0, 1),
                }, commandTimeout: CommandTimeoutSeconds);
            }
        }
    }

    public async Task<int> ClearOpenBacklogForOrderAsync(string companyName, string orderNo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();
        var order = orderNo.Trim();
        if (string.IsNullOrEmpty(company) || string.IsNullOrEmpty(order))
            return 0;

        using var connection = _database.CreateConnection();
        return await connection.ExecuteAsync(@"
UPDATE PlanningBacklog
SET Status = 'Cleared', ClearedAt = GETDATE()
WHERE CompanyName = @CompanyName AND OrderNo = @OrderNo AND Status = 'Open'",
            new { CompanyName = company, OrderNo = order },
            commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<IReadOnlyList<PlanningLoomPreferenceChartDto>> GetLoomPreferenceChartAsync(
        string companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = companyName.Trim();

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<PreferenceChartRow>(@"
SELECT ChartId, CompanyName, FabricForm, GsmMin, GsmMax, WidthMinCm, WidthMaxCm,
       PreferenceRank, LoomType, WinderCategory, ChangeoverTier, Notes
FROM PlanningLoomPreferenceChart WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY FabricForm, PreferenceRank, GsmMin, WidthMinCm",
            new { CompanyName = company },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        if (rows.Count == 0)
        {
            var seed = LoomPreferenceChartDefaults.SeedRows(company);
            await SaveLoomPreferenceChartAsync(new SavePlanningLoomPreferenceChartRequest
            {
                CompanyName = company,
                Rows = seed,
            }, ct);
            return seed;
        }

        return rows.Select(MapPreferenceChart).ToList();
    }

    public async Task SaveLoomPreferenceChartAsync(SavePlanningLoomPreferenceChartRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        var company = request.CompanyName.Trim();

        using var connection = _database.CreateConnection();
        foreach (var row in request.Rows)
        {
            if (row.ChartId > 0)
            {
                await connection.ExecuteAsync(@"
UPDATE PlanningLoomPreferenceChart
SET FabricForm = @FabricForm, GsmMin = @GsmMin, GsmMax = @GsmMax,
    WidthMinCm = @WidthMinCm, WidthMaxCm = @WidthMaxCm, PreferenceRank = @PreferenceRank,
    LoomType = @LoomType, WinderCategory = @WinderCategory, ChangeoverTier = @ChangeoverTier,
    Notes = @Notes, UpdatedAt = GETDATE()
WHERE ChartId = @ChartId AND CompanyName = @CompanyName", new
                {
                    row.ChartId,
                    CompanyName = company,
                    row.FabricForm,
                    row.GsmMin,
                    row.GsmMax,
                    row.WidthMinCm,
                    row.WidthMaxCm,
                    row.PreferenceRank,
                    row.LoomType,
                    WinderCategory = string.IsNullOrWhiteSpace(row.WinderCategory) ? null : row.WinderCategory,
                    row.ChangeoverTier,
                    row.Notes,
                }, commandTimeout: CommandTimeoutSeconds);
            }
            else
            {
                await connection.ExecuteAsync(@"
INSERT INTO PlanningLoomPreferenceChart
    (CompanyName, FabricForm, GsmMin, GsmMax, WidthMinCm, WidthMaxCm, PreferenceRank, LoomType, WinderCategory, ChangeoverTier, Notes)
VALUES
    (@CompanyName, @FabricForm, @GsmMin, @GsmMax, @WidthMinCm, @WidthMaxCm, @PreferenceRank, @LoomType, @WinderCategory, @ChangeoverTier, @Notes)", new
                {
                    CompanyName = company,
                    row.FabricForm,
                    row.GsmMin,
                    row.GsmMax,
                    row.WidthMinCm,
                    row.WidthMaxCm,
                    row.PreferenceRank,
                    row.LoomType,
                    WinderCategory = string.IsNullOrWhiteSpace(row.WinderCategory) ? null : row.WinderCategory,
                    row.ChangeoverTier,
                    row.Notes,
                }, commandTimeout: CommandTimeoutSeconds);
            }
        }
    }

    public async Task DeleteDowntimeAsync(int downtimeId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM PlanningDowntime WHERE DowntimeId = @DowntimeId",
            new { DowntimeId = downtimeId },
            commandTimeout: CommandTimeoutSeconds);
    }

    private async Task<IReadOnlyList<PlanningLineConfigDto>> LoadErpLinesAsync(string company, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<ErpLineRow>(@"
SELECT LNo, BagType, Bagcapacity, NoOfDaysChk, IsDoubleDust, IsTripleDust
FROM NewLineMaster WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY LNo, SOrderno", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows
            .GroupBy(r => r.LNo)
            .Select(g =>
            {
                var first = g.First();
                var families = PlanningSetupHelpers.InferBagFamilies(first.BagType);
                var primary = families.FirstOrDefault() ?? BagTypeMapper.NormalizeErpFamily(first.BagType);
                var dust = PlanningSetupHelpers.DefaultDustCapacities(primary, first.Bagcapacity);

                return new PlanningLineConfigDto
                {
                    CompanyName = company,
                    LineNo = first.LNo,
                    DisplayName = $"Line {first.LNo}",
                    ErpBagType = first.BagType,
                    AllowedBagFamilies = families,
                    ErpBagCapacity = first.Bagcapacity,
                    CapacityNormal = dust.normal,
                    CapacitySingleDust = dust.single,
                    CapacityDoubleDust = dust.dbl,
                    CapacityTripleDust = dust.triple,
                    ErpBufferDaysCheck = first.NoOfDaysChk,
                    BufferDaysOverride = first.NoOfDaysChk > 0 ? first.NoOfDaysChk : null,
                    IsActive = true,
                    PreferenceOrder = first.LNo,
                    FromErp = true,
                };
            })
            .OrderBy(l => l.LineNo)
            .ToList();
    }

    private async Task<IReadOnlyList<PlanningLineConfigDto>> LoadPortalLinesAsync(string company, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<PortalLineRow>(@"
SELECT LineConfigId, CompanyName, [LineNo], DisplayName, ErpBagType, AllowedBagFamilies,
       CapacityNormal, CapacitySingleDust, CapacityDoubleDust, CapacityTripleDust,
       BufferDaysOverride, TeamNo, ContractorCode, IsActive, PreferenceOrder
FROM PlanningLineConfig WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY [LineNo]", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new PlanningLineConfigDto
        {
            LineConfigId = r.LineConfigId,
            CompanyName = r.CompanyName ?? company,
            LineNo = r.LineNo,
            DisplayName = r.DisplayName,
            ErpBagType = r.ErpBagType,
            AllowedBagFamilies = PlanningSetupHelpers.DeserializeFamilies(r.AllowedBagFamilies),
            CapacityNormal = r.CapacityNormal,
            CapacitySingleDust = r.CapacitySingleDust,
            CapacityDoubleDust = r.CapacityDoubleDust,
            CapacityTripleDust = r.CapacityTripleDust,
            BufferDaysOverride = r.BufferDaysOverride,
            TeamNo = r.TeamNo,
            ContractorCode = r.ContractorCode,
            IsActive = r.IsActive,
            PreferenceOrder = r.PreferenceOrder,
            FromErp = false,
        }).ToList();
    }

    private async Task<IReadOnlyList<PlanningLoomPoolDto>> LoadErpLoomsAsync(string company, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<ErpLoomRow>(@"
SELECT LoomNo, LoomCode, LoomSpecification, make, MinSize, MaxSize, isFreeze
FROM NewMISLoomMaster WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY LoomNo", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r =>
        {
            var frozen = PlanningSetupHelpers.IsFrozen(r.isFreeze);
            var loomType = PlanningSetupHelpers.InferLoomType(r.make, r.LoomSpecification);
            return new PlanningLoomPoolDto
            {
                CompanyName = company,
                LoomNo = r.LoomNo,
                ErpLoomCode = r.LoomCode,
                ErpLoomSpecification = r.LoomSpecification,
                ErpMake = r.make,
                ErpMinSize = r.MinSize,
                ErpMaxSize = r.MaxSize,
                ErpIsFrozen = frozen,
                IncludeInPlanning = !frozen && !string.IsNullOrWhiteSpace(loomType),
                PoolPurpose = frozen ? "Maintenance" : "DomesticFibc",
                LoomType = string.IsNullOrWhiteSpace(loomType) ? null : loomType,
                WinderCategory = "Tube",
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<PlanningLoomPoolDto>> LoadPortalLoomsAsync(string company, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<PortalLoomRow>(@"
SELECT PoolId, CompanyName, LoomNo, ErpLoomCode, IncludeInPlanning, PoolPurpose, LoomType,
       WinderCategory, GsmMin, GsmMax, WidthMinCm, WidthMaxCm, Notes
FROM PlanningLoomPool WITH (NOLOCK)
WHERE CompanyName = @CompanyName
ORDER BY LoomNo", new { CompanyName = company }, commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new PlanningLoomPoolDto
        {
            PoolId = r.PoolId,
            CompanyName = r.CompanyName ?? company,
            LoomNo = r.LoomNo,
            ErpLoomCode = r.ErpLoomCode,
            IncludeInPlanning = r.IncludeInPlanning,
            PoolPurpose = r.PoolPurpose ?? "DomesticFibc",
            LoomType = r.LoomType,
            WinderCategory = r.WinderCategory ?? "Tube",
            GsmMin = r.GsmMin,
            GsmMax = r.GsmMax,
            WidthMinCm = r.WidthMinCm,
            WidthMaxCm = r.WidthMaxCm,
            Notes = r.Notes,
        }).ToList();
    }

    private static PlanningLineConfigDto MergeLine(PlanningLineConfigDto erp, PlanningLineConfigDto portal, bool fromPortal)
    {
        return new PlanningLineConfigDto
        {
            LineConfigId = portal.LineConfigId,
            CompanyName = portal.CompanyName,
            LineNo = erp.LineNo,
            DisplayName = portal.DisplayName ?? erp.DisplayName,
            ErpBagType = portal.ErpBagType ?? erp.ErpBagType,
            AllowedBagFamilies = portal.AllowedBagFamilies.Count > 0 ? portal.AllowedBagFamilies : erp.AllowedBagFamilies,
            CapacityNormal = portal.CapacityNormal ?? erp.CapacityNormal,
            CapacitySingleDust = portal.CapacitySingleDust ?? erp.CapacitySingleDust,
            CapacityDoubleDust = portal.CapacityDoubleDust ?? erp.CapacityDoubleDust,
            CapacityTripleDust = portal.CapacityTripleDust ?? erp.CapacityTripleDust,
            ErpBagCapacity = erp.ErpBagCapacity,
            BufferDaysOverride = portal.BufferDaysOverride ?? erp.BufferDaysOverride,
            ErpBufferDaysCheck = erp.ErpBufferDaysCheck,
            TeamNo = portal.TeamNo,
            ContractorCode = portal.ContractorCode,
            IsActive = portal.IsActive,
            PreferenceOrder = portal.PreferenceOrder > 0 ? portal.PreferenceOrder : erp.PreferenceOrder,
            FromErp = true,
        };
    }

    private static PlanningLoomPoolDto MergeLoom(PlanningLoomPoolDto erp, PlanningLoomPoolDto portal)
    {
        return new PlanningLoomPoolDto
        {
            PoolId = portal.PoolId,
            CompanyName = erp.CompanyName,
            LoomNo = erp.LoomNo,
            ErpLoomCode = erp.ErpLoomCode ?? portal.ErpLoomCode,
            ErpLoomSpecification = erp.ErpLoomSpecification,
            ErpMake = erp.ErpMake,
            ErpMinSize = erp.ErpMinSize,
            ErpMaxSize = erp.ErpMaxSize,
            ErpIsFrozen = erp.ErpIsFrozen,
            IncludeInPlanning = portal.PoolId.HasValue ? portal.IncludeInPlanning : erp.IncludeInPlanning,
            PoolPurpose = portal.PoolPurpose ?? erp.PoolPurpose,
            LoomType = portal.LoomType ?? erp.LoomType,
            WinderCategory = portal.WinderCategory ?? erp.WinderCategory,
            GsmMin = portal.GsmMin ?? erp.GsmMin,
            GsmMax = portal.GsmMax ?? erp.GsmMax,
            WidthMinCm = portal.WidthMinCm ?? erp.WidthMinCm,
            WidthMaxCm = portal.WidthMaxCm ?? erp.WidthMaxCm,
            Notes = portal.Notes,
        };
    }

    private static string FactorKey(int lineNo, string? shift, string teamNo) =>
        $"{lineNo}|{(shift ?? "").Trim().ToUpperInvariant()}|{teamNo.Trim()}";

    private static PlanningFactoryConfigDto MapFactoryConfig(FactoryConfigRow row) => new()
    {
        ConfigId = row.ConfigId,
        FactoryInfoSrNo = row.FactoryInfoSrNo,
        CompanyName = row.CompanyName ?? "",
        IsPlanningEnabled = row.IsPlanningEnabled != 0,
        DefaultDispatchBufferDays = row.DefaultDispatchBufferDays,
        DefaultRejectionPercent = row.DefaultRejectionPercent,
        Notes = row.Notes,
        UpdatedAt = row.UpdatedAt,
    };

    private static PlanningTeamFactorDto MapTeamFactor(TeamFactorRow row) => new()
    {
        FactorId = row.FactorId,
        CompanyName = row.CompanyName ?? "",
        LineNo = row.LineNo,
        Shift = row.Shift,
        TeamNo = row.TeamNo ?? "",
        ManualFactor = row.ManualFactor,
        AutoFactor = row.AutoFactor,
        EffectiveFactor = row.EffectiveFactor,
        FactorSource = row.FactorSource ?? "Default",
        SampleDays = row.SampleDays,
        SampleProductionPcs = row.SampleProductionPcs,
        SamplePlannedCapacity = row.SamplePlannedCapacity,
        UpdatedAt = row.UpdatedAt,
    };

    private static PlanningBacklogDto MapBacklog(BacklogRow row) => new()
    {
        BacklogId = row.BacklogId,
        CompanyName = row.CompanyName ?? "",
        LineNo = row.LineNo,
        Shift = row.Shift ?? "",
        OrderNo = row.OrderNo ?? "",
        BacklogQty = row.BacklogQty,
        Reason = row.Reason,
        Status = row.Status ?? "Open",
        CreatedAt = row.CreatedAt,
        ClearedAt = row.ClearedAt,
    };

    private sealed class FactorySearchRow
    {
        public int FactoryInfoSrNo { get; set; }
        public string? CompanyName { get; set; }
        public string? GroupName { get; set; }
        public int IsPlanningEnabled { get; set; }
        public int HasLineMaster { get; set; }
        public int HasLoomMaster { get; set; }
    }

    private sealed class FactoryConfigRow
    {
        public int ConfigId { get; set; }
        public int? FactoryInfoSrNo { get; set; }
        public string? CompanyName { get; set; }
        public int IsPlanningEnabled { get; set; }
        public int DefaultDispatchBufferDays { get; set; }
        public double DefaultRejectionPercent { get; set; }
        public string? Notes { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private sealed class ErpLineRow
    {
        public int LNo { get; set; }
        public string? BagType { get; set; }
        public int Bagcapacity { get; set; }
        public int NoOfDaysChk { get; set; }
        public int IsDoubleDust { get; set; }
        public int IsTripleDust { get; set; }
    }

    private sealed class PortalLineRow
    {
        public int LineConfigId { get; set; }
        public string? CompanyName { get; set; }
        public int LineNo { get; set; }
        public string? DisplayName { get; set; }
        public string? ErpBagType { get; set; }
        public string? AllowedBagFamilies { get; set; }
        public int? CapacityNormal { get; set; }
        public int? CapacitySingleDust { get; set; }
        public int? CapacityDoubleDust { get; set; }
        public int? CapacityTripleDust { get; set; }
        public int? BufferDaysOverride { get; set; }
        public string? TeamNo { get; set; }
        public int? ContractorCode { get; set; }
        public bool IsActive { get; set; }
        public int PreferenceOrder { get; set; }
    }

    private sealed class ErpLoomRow
    {
        public int LoomNo { get; set; }
        public string? LoomCode { get; set; }
        public string? LoomSpecification { get; set; }
        public string? make { get; set; }
        public double? MinSize { get; set; }
        public double? MaxSize { get; set; }
        public string? isFreeze { get; set; }
    }

    private sealed class PortalLoomRow
    {
        public int PoolId { get; set; }
        public string? CompanyName { get; set; }
        public int LoomNo { get; set; }
        public string? ErpLoomCode { get; set; }
        public bool IncludeInPlanning { get; set; }
        public string? PoolPurpose { get; set; }
        public string? LoomType { get; set; }
        public string? WinderCategory { get; set; }
        public double? GsmMin { get; set; }
        public double? GsmMax { get; set; }
        public double? WidthMinCm { get; set; }
        public double? WidthMaxCm { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class TeamFactorRow
    {
        public int FactorId { get; set; }
        public string? CompanyName { get; set; }
        public int LineNo { get; set; }
        public string? Shift { get; set; }
        public string? TeamNo { get; set; }
        public double? ManualFactor { get; set; }
        public double? AutoFactor { get; set; }
        public double EffectiveFactor { get; set; }
        public string? FactorSource { get; set; }
        public int SampleDays { get; set; }
        public double? SampleProductionPcs { get; set; }
        public double? SamplePlannedCapacity { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private sealed class TeamProductionRow
    {
        public string? TeamNo { get; set; }
        public string? Shift { get; set; }
        public double TotalPcs { get; set; }
        public int ActiveDays { get; set; }
    }

    private sealed class BacklogRow
    {
        public int BacklogId { get; set; }
        public string? CompanyName { get; set; }
        public int LineNo { get; set; }
        public string? Shift { get; set; }
        public string? OrderNo { get; set; }
        public double BacklogQty { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClearedAt { get; set; }
    }

    private sealed class DowntimeRow
    {
        public int DowntimeId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime PlanDate { get; set; }
        public int LineNo { get; set; }
        public string? Shift { get; set; }
        public string? Reason { get; set; }
        public double CapacityFactor { get; set; }
    }

    private sealed class PreferenceChartRow
    {
        public int ChartId { get; set; }
        public string? CompanyName { get; set; }
        public string? FabricForm { get; set; }
        public double GsmMin { get; set; }
        public double GsmMax { get; set; }
        public double WidthMinCm { get; set; }
        public double WidthMaxCm { get; set; }
        public int PreferenceRank { get; set; }
        public string? LoomType { get; set; }
        public string? WinderCategory { get; set; }
        public string? ChangeoverTier { get; set; }
        public string? Notes { get; set; }
    }

    private static PlanningLoomPreferenceChartDto MapPreferenceChart(PreferenceChartRow row) => new()
    {
        ChartId = row.ChartId,
        CompanyName = row.CompanyName ?? "",
        FabricForm = row.FabricForm ?? "Tube",
        GsmMin = row.GsmMin,
        GsmMax = row.GsmMax,
        WidthMinCm = row.WidthMinCm,
        WidthMaxCm = row.WidthMaxCm,
        PreferenceRank = row.PreferenceRank,
        LoomType = row.LoomType ?? "",
        WinderCategory = row.WinderCategory ?? "",
        ChangeoverTier = row.ChangeoverTier ?? "Blue",
        Notes = row.Notes,
    };
}
