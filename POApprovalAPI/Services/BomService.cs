using Dapper;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class BomService
{
    private const int MaxPageSize = 100;
    private const int BomCommandTimeoutSeconds = 120;
    private const string PartyMappingCacheKey = "bom:party-mapping:v2";
    private const string UsersCacheKey = "bom:users:v1";
    private static readonly TimeSpan PartyMappingCacheTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan UsersCacheTtl = TimeSpan.FromHours(6);

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public BomService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BomCustomerOption>> GetCustomersAsync()
    {
        var entry = await GetPartyMappingEntryAsync();
        return entry.MappedCustomers;
    }

    public async Task<IReadOnlyList<string>> GetPartyNamesAsync()
    {
        var entry = await GetPartyMappingEntryAsync();
        return entry.PartyNames;
    }

    public async Task WarmCachesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await GetPartyMappingEntryAsync();
        await GetUsersAsync();
    }

    public async Task<IReadOnlyList<BomPartyGroup>> GetPartyMappingAsync()
    {
        var entry = await GetPartyMappingEntryAsync();
        return entry.Mapping.Groups;
    }

    private static IReadOnlyList<BomCustomerOption> MapGroupsToCustomerOptions(
        BomPartyMappingIndex mapping,
        IReadOnlyList<BomCustomerOption> rawCustomers)
    {
        var masterByName = rawCustomers
            .Where(c => c.FromMaster)
            .GroupBy(c => c.CompanyName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return mapping.Groups.Select(group =>
        {
            BomCustomerOption? master = null;
            if (!string.IsNullOrWhiteSpace(group.OfficialName))
                masterByName.TryGetValue(group.OfficialName, out master);

            return new BomCustomerOption
            {
                CompanyName = group.DisplayName,
                Email = master?.Email,
                Email1 = master?.Email1,
                Email2 = master?.Email2,
                City = master?.City,
                Country = master?.Country,
                FromMaster = group.FromMaster,
                AliasCount = group.Aliases.Count,
                MappingType = group.MappingType,
                OfficialName = group.OfficialName,
            };
        }).ToList();
    }

    private sealed record PartyMappingCacheEntry(
        BomPartyMappingIndex Mapping,
        IReadOnlyList<BomCustomerOption> RawCustomers,
        IReadOnlyList<BomCustomerOption> MappedCustomers,
        IReadOnlyList<string> PartyNames);

    private async Task<PartyMappingCacheEntry> GetPartyMappingEntryAsync()
    {
        if (_cache.TryGetValue(PartyMappingCacheKey, out PartyMappingCacheEntry? cached) && cached is not null)
            return cached;

        var raw = await FetchRawCustomersAsync();
        var mapping = BomPartyMappingService.Build(raw);
        var mappedCustomers = MapGroupsToCustomerOptions(mapping, raw);
        var partyNames = mappedCustomers
            .Select(c => c.CompanyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entry = new PartyMappingCacheEntry(mapping, raw, mappedCustomers, partyNames);
        _cache.Set(PartyMappingCacheKey, entry, PartyMappingCacheTtl);
        return entry;
    }

    private async Task<BomPartyMappingIndex> GetMappingIndexAsync()
    {
        var entry = await GetPartyMappingEntryAsync();
        return entry.Mapping;
    }

    private async Task<IReadOnlyList<BomCustomerOption>> FetchRawCustomersAsync()
    {
        var mastersTask = FetchMasterCustomersAsync();
        var bomOnlyTask = FetchBomOnlyCustomersAsync();
        await Task.WhenAll(mastersTask, bomOnlyTask);

        var masters = await mastersTask;
        var bomOnly = await bomOnlyTask;

        return masters
            .Concat(bomOnly)
            .OrderBy(c => c.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<BomCustomerOption>> FetchMasterCustomersAsync()
    {
        using var connection = _database.CreateProductionConnection();
        var rows = await connection.QueryAsync<BomCustomerOption>(@"
SELECT
    cm.CompanyName,
    cm.Email,
    cm.Email1,
    cm.Email2,
    cm.City,
    cm.Country,
    CAST(1 AS bit) AS FromMaster
FROM CompanyMaster cm WITH (NOLOCK)
WHERE ISNULL(cm.CompanyName, '') <> ''");
        return rows.ToList();
    }

    private async Task<IReadOnlyList<BomCustomerOption>> FetchBomOnlyCustomersAsync()
    {
        using var connection = _database.CreateProductionConnection();
        var rows = await connection.QueryAsync<BomCustomerOption>(@"
SELECT DISTINCT
    b.Customer AS CompanyName,
    NULL AS Email,
    NULL AS Email1,
    NULL AS Email2,
    NULL AS City,
    NULL AS Country,
    CAST(0 AS bit) AS FromMaster
FROM BOM1 b WITH (NOLOCK)
LEFT JOIN CompanyMaster cm WITH (NOLOCK) ON cm.CompanyName = b.Customer
WHERE ISNULL(b.Customer, '') <> ''
  AND cm.CompanyName IS NULL");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<string>> GetUsersAsync()
    {
        if (_cache.TryGetValue(UsersCacheKey, out IReadOnlyList<string>? cachedUsers) && cachedUsers is not null)
            return cachedUsers;

        using var connection = _database.CreateProductionConnection();

        var users = (await connection.QueryAsync<string>(@"
SELECT DISTINCT UserName
FROM BOM1 WITH (NOLOCK)
WHERE ISNULL(UserName, '') <> ''
ORDER BY UserName")).ToList();

        _cache.Set(UsersCacheKey, users, UsersCacheTtl);
        return users;
    }

    public async Task<BomSearchResult> SearchAsync(BomSearchRequest request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var offset = (page - 1) * pageSize;

        var dateFrom = request.DateFrom?.Date;
        var dateTo = request.DateTo?.Date;
        if (dateFrom is null && dateTo is null)
        {
            dateTo = DateTime.Today;
            dateFrom = dateTo.Value.AddMonths(-3);
        }
        else if (dateFrom is null)
            dateFrom = dateTo;
        else if (dateTo is null)
            dateTo = dateFrom;

        if (dateFrom!.Value.Date > dateTo!.Value.Date)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var inclusiveDateTo = dateTo!.Value.Date.AddDays(1);

        using var connection = _database.CreateProductionConnection();

        var mapping = await GetMappingIndexAsync();
        var partyAliases = mapping.ResolveFilterNames(request.PartyName);
        var hasPartyFilter = partyAliases.Count > 0;

        var whereClause = hasPartyFilter
            ? @"
WHERE b.SysDate >= @DateFrom
  AND b.SysDate < @InclusiveDateTo
  AND b.Customer IN @PartyNames
  AND (@UserName IS NULL OR b.UserName = @UserName)
  AND (
        @Search IS NULL
     OR b.FilePONo LIKE '%' + @Search + '%'
     OR b.Customer LIKE '%' + @Search + '%'
     OR b.UserName LIKE '%' + @Search + '%'
  )
  AND ISNULL(b.SrNo, '') <> 'temp'"
            : @"
WHERE b.SysDate >= @DateFrom
  AND b.SysDate < @InclusiveDateTo
  AND (@UserName IS NULL OR b.UserName = @UserName)
  AND (
        @Search IS NULL
     OR b.FilePONo LIKE '%' + @Search + '%'
     OR b.Customer LIKE '%' + @Search + '%'
     OR b.UserName LIKE '%' + @Search + '%'
  )
  AND ISNULL(b.SrNo, '') <> 'temp'";

        var parameters = new
        {
            DateFrom = dateFrom,
            InclusiveDateTo = inclusiveDateTo,
            PartyNames = partyAliases,
            UserName = NormalizeOptional(request.UserName),
            Search = NormalizeOptional(request.Search),
            Offset = offset,
            PageSize = pageSize,
        };

        var totalCount = await connection.ExecuteScalarAsync<int>($@"
SELECT COUNT(1)
FROM BOM1 b WITH (NOLOCK)
{whereClause}", parameters);

        var orderClause = ResolveSortDescending(request)
            ? "ORDER BY b.SysDate DESC, b.FilePONo DESC"
            : "ORDER BY b.SysDate ASC, b.FilePONo ASC";

        var items = (await connection.QueryAsync<BomListItem>($@"
SELECT
    b.FilePONo AS QtnNo,
    b.Customer AS PartyName,
    b.SizeL,
    b.SizeW,
    b.SizeH,
    b.SysDate AS [Date],
    b.UserName AS [User],
    b.BagType,
    b.SWL AS Swl,
    b.Qty,
    b.TotalKg,
    b.SrNo
FROM BOM1 b WITH (NOLOCK)
{whereClause}
{orderClause}
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", parameters)).ToList();

        foreach (var item in items)
            item.PartyName = mapping.ResolveDisplayName(item.PartyName);

        return new BomSearchResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
        };
    }

    public async Task<BomDetailResult?> GetDetailAsync(string filePoNo)
    {
        if (string.IsNullOrWhiteSpace(filePoNo))
            throw new ArgumentException("Quotation number is required.");

        var model = await BuildPdfModelAsync(filePoNo);
        if (model is null)
            return null;

        return new BomDetailResult
        {
            Header = MapHeader(model),
            Lines = model.Lines.Select(MapLine).ToList(),
            ReportLines = Array.Empty<BomReportLine>(),
        };
    }

    public async Task<bool> BomExistsAsync(string filePoNo)
    {
        if (string.IsNullOrWhiteSpace(filePoNo))
            return false;

        using var connection = _database.CreateProductionConnection();
        return await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM BOM1 WITH (NOLOCK)
    WHERE FilePONo = @FilePoNo
      AND ISNULL(SrNo, '') <> 'temp'
) THEN 1 ELSE 0 END", new { FilePoNo = filePoNo.Trim() }, commandTimeout: BomCommandTimeoutSeconds) == 1;
    }

    public async Task<BomPdfModel?> BuildPdfModelAsync(string filePoNo)
    {
        if (string.IsNullOrWhiteSpace(filePoNo))
            throw new ArgumentException("Quotation number is required.");

        var trimmed = filePoNo.Trim();
        using var connection = _database.CreateProductionConnection();

        var headerRow = await connection.QuerySingleOrDefaultAsync<BomHeaderRow>(@"
SELECT TOP 1
    b.FilePONo,
    b.Customer,
    b.SysDate,
    b.UserName,
    b.RefNo,
    b.pono AS PoNo,
    b.ponos AS PoNos,
    b.BagType,
    b.SizeL,
    b.SizeW,
    b.SizeH,
    b.SizeType,
    b.SWL AS Swl,
    b.S AS SfRatio,
    b.L,
    b.Qty,
    b.QtyUnit,
    b.PrintType,
    b.TotalKg,
    b.Instruction,
    b.printingremarks AS PrintingRemarks,
    b.BodyRemarks1 AS BodyRemarks,
    b.Doc,
    b.Doc1,
    b.Doc2,
    b.LoopL,
    b.LoopW,
    b.LoopDim,
    b.FSType,
    b.DSType,
    b.DSL,
    b.DSW,
    b.Liner,
    b.LinerL,
    b.LinerW,
    b.LinerDim,
    b.FabColor,
    b.IsDropLoop,
    b.RPfabric AS RpFabric,
    b.Knottype AS KnotType,
    b.SrNo,
    (
        SELECT TOP 1 v.MarketingInvNo
        FROM Despatch.dbo.MarketingInvoice v WITH (NOLOCK)
        WHERE v.BuyerOrderNo = b.FilePONo
    ) AS MarketingInvNo
FROM BOM1 b WITH (NOLOCK)
WHERE b.FilePONo = @FilePoNo
  AND ISNULL(b.SrNo, '') <> 'temp'
ORDER BY b.SysDate DESC", new { FilePoNo = trimmed }, commandTimeout: BomCommandTimeoutSeconds);

        if (headerRow is null)
            return null;

        var mapping = await GetMappingIndexAsync();
        var mappedPartyName = mapping.ResolveDisplayName(headerRow.Customer);

        var lineRows = (await connection.QueryAsync<BomLineRow>(@"
SELECT
    b.TransId AS SortOrder,
    b.Heading,
    b.GSM AS Gsm,
    b.Lami,
    b.Color,
    b.FabricSize,
    b.CutSize,
    b.TotalMtr,
    b.TotalKg,
    b.GPM AS Gpm,
    b.Remarks
FROM BOM b WITH (NOLOCK)
WHERE b.PONo = @FilePoNo
ORDER BY b.TransId", new { FilePoNo = trimmed }, commandTimeout: BomCommandTimeoutSeconds)).ToList();

        return new BomPdfModel
        {
            QtnNo = headerRow.FilePONo ?? trimmed,
            PartyName = mappedPartyName,
            Date = headerRow.SysDate,
            User = headerRow.UserName ?? "",
            RefNo = headerRow.RefNo ?? "",
            PoNo = headerRow.PoNo ?? "",
            PoNos = headerRow.PoNos ?? "",
            MarketingInvNo = headerRow.MarketingInvNo ?? "",
            BagType = headerRow.BagType ?? "",
            SizeType = headerRow.SizeType ?? "",
            SizeL = headerRow.SizeL,
            SizeW = headerRow.SizeW,
            SizeH = headerRow.SizeH,
            Swl = headerRow.Swl ?? "",
            SfRatio = FirstNonEmpty(headerRow.SfRatio, headerRow.L),
            Qty = headerRow.Qty ?? "",
            QtyUnit = headerRow.QtyUnit ?? "",
            PrintType = headerRow.PrintType ?? "",
            Doc = headerRow.Doc ?? "",
            Doc1 = headerRow.Doc1 ?? "",
            Doc2 = headerRow.Doc2 ?? "",
            LoopSpec = BuildLoopSpec(headerRow),
            LinerSpec = BuildLinerSpec(headerRow),
            TopSpoutType = FirstNonEmpty(headerRow.FSType),
            BottomType = BuildBottomSpec(headerRow),
            FabColor = headerRow.FabColor ?? "",
            IsDropLoop = headerRow.IsDropLoop ?? "",
            RpFabric = headerRow.RpFabric ?? "",
            KnotType = headerRow.KnotType ?? "",
            TotalKgPerBag = headerRow.TotalKg,
            Instruction = headerRow.Instruction ?? "",
            PrintingRemarks = headerRow.PrintingRemarks ?? "",
            BodyRemarks = headerRow.BodyRemarks ?? "",
            Lines = lineRows.Select(row => new BomPdfLine
            {
                SortOrder = row.SortOrder,
                Heading = row.Heading ?? "",
                Gsm = row.Gsm ?? "",
                Lami = row.Lami ?? "",
                Color = row.Color ?? "",
                FabricSize = row.FabricSize ?? "",
                CutSize = row.CutSize ?? "",
                TotalMtr = row.TotalMtr,
                TotalKg = row.TotalKg,
                Gpm = row.Gpm ?? "",
                Remarks = row.Remarks ?? "",
            }).ToList(),
        };
    }

    public async Task<BomCustomerOption?> GetCustomerAsync(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.");

        var mapping = await GetMappingIndexAsync();
        var lookupName = mapping.ResolveOfficialMasterName(companyName) ?? companyName.Trim();

        using var connection = _database.CreateProductionConnection();

        return await connection.QuerySingleOrDefaultAsync<BomCustomerOption>(@"
SELECT TOP 1
    CompanyName,
    Email,
    Email1,
    Email2,
    City,
    Country,
    CAST(1 AS bit) AS FromMaster
FROM CompanyMaster WITH (NOLOCK)
WHERE CompanyName = @CompanyName", new { CompanyName = lookupName });
    }

    public async Task<bool> UpdateCustomerAsync(string companyName, BomCustomerUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.");

        using var connection = _database.CreateProductionConnection();

        var affected = await connection.ExecuteAsync(@"
UPDATE CompanyMaster
SET Email = @Email,
    Email1 = @Email1,
    Email2 = @Email2,
    CnctPerson = @CnctPerson,
    TelNo1 = @TelNo1,
    Address = @Address,
    City = @City,
    State = @State,
    Country = @Country
WHERE CompanyName = @CompanyName", new
        {
            CompanyName = companyName.Trim(),
            Email = request.Email ?? "",
            Email1 = request.Email1 ?? "",
            Email2 = request.Email2 ?? "",
            CnctPerson = request.CnctPerson ?? "",
            TelNo1 = request.TelNo1 ?? "",
            Address = request.Address ?? "",
            City = request.City ?? "",
            State = request.State ?? "",
            Country = request.Country ?? "",
        });

        if (affected > 0)
            _cache.Remove(PartyMappingCacheKey);

        return affected > 0;
    }

    private static bool ResolveSortDescending(BomSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SortDirection))
            return !string.Equals(request.SortDirection.Trim(), "asc", StringComparison.OrdinalIgnoreCase);

        return request.DateSortDesc;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    private static BomHeader MapHeader(BomPdfModel model) => new()
    {
        QtnNo = model.QtnNo,
        PartyName = model.PartyName,
        Date = model.Date,
        User = model.User,
        RefNo = model.RefNo,
        PoNo = model.PoNo,
        PoNos = model.PoNos,
        MarketingInvNo = model.MarketingInvNo,
        BagType = model.BagType,
        SizeL = model.SizeL,
        SizeW = model.SizeW,
        SizeH = model.SizeH,
        SizeType = model.SizeType,
        Swl = model.Swl,
        SfRatio = model.SfRatio,
        Qty = model.Qty,
        QtyUnit = model.QtyUnit,
        TotalKg = model.TotalKgPerBag,
        PrintType = model.PrintType,
        Doc = model.Doc,
        Doc1 = model.Doc1,
        Doc2 = model.Doc2,
        LoopSpec = model.LoopSpec,
        LinerSpec = model.LinerSpec,
        TopSpoutType = model.TopSpoutType,
        BottomType = model.BottomType,
        FabColor = model.FabColor,
        IsDropLoop = model.IsDropLoop,
        RpFabric = model.RpFabric,
        KnotType = model.KnotType,
        Instruction = model.Instruction,
        PrintingRemarks = model.PrintingRemarks,
        BodyRemarks = model.BodyRemarks,
    };

    private static BomLineItem MapLine(BomPdfLine line) => new()
    {
        SortOrder = line.SortOrder,
        Heading = line.Heading,
        Gsm = line.Gsm,
        Lami = line.Lami,
        Color = line.Color,
        FabricSize = line.FabricSize,
        CutSize = line.CutSize,
        TotalMtr = line.TotalMtr,
        TotalKg = line.TotalKg,
        Gpm = line.Gpm,
        Remarks = line.Remarks,
    };

    private static string BuildLoopSpec(BomHeaderRow row)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.LoopL)) parts.Add($"L {row.LoopL}");
        if (!string.IsNullOrWhiteSpace(row.LoopW)) parts.Add($"W {row.LoopW}");
        if (!string.IsNullOrWhiteSpace(row.LoopDim)) parts.Add(row.LoopDim);
        return string.Join(" · ", parts);
    }

    private static string BuildLinerSpec(BomHeaderRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Liner) && string.IsNullOrWhiteSpace(row.LinerL))
            return "";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.Liner)) parts.Add(row.Liner);
        if (!string.IsNullOrWhiteSpace(row.LinerL) || !string.IsNullOrWhiteSpace(row.LinerW))
            parts.Add($"{row.LinerL} × {row.LinerW} {row.LinerDim}".Trim());
        return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string BuildBottomSpec(BomHeaderRow row)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.DSType)) parts.Add(row.DSType);
        if (!string.IsNullOrWhiteSpace(row.DSL) || !string.IsNullOrWhiteSpace(row.DSW))
            parts.Add($"{row.DSL} × {row.DSW}".Trim());
        return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "";
    }

    private sealed class BomHeaderRow
    {
        public string? FilePONo { get; set; }
        public string? Customer { get; set; }
        public DateTime? SysDate { get; set; }
        public string? UserName { get; set; }
        public string? RefNo { get; set; }
        public string? PoNo { get; set; }
        public string? PoNos { get; set; }
        public string? BagType { get; set; }
        public double? SizeL { get; set; }
        public double? SizeW { get; set; }
        public double? SizeH { get; set; }
        public string? SizeType { get; set; }
        public string? Swl { get; set; }
        public string? SfRatio { get; set; }
        public string? L { get; set; }
        public string? Qty { get; set; }
        public string? QtyUnit { get; set; }
        public string? PrintType { get; set; }
        public double? TotalKg { get; set; }
        public string? Instruction { get; set; }
        public string? PrintingRemarks { get; set; }
        public string? BodyRemarks { get; set; }
        public string? Doc { get; set; }
        public string? Doc1 { get; set; }
        public string? Doc2 { get; set; }
        public string? LoopL { get; set; }
        public string? LoopW { get; set; }
        public string? LoopDim { get; set; }
        public string? FSType { get; set; }
        public string? DSType { get; set; }
        public string? DSL { get; set; }
        public string? DSW { get; set; }
        public string? Liner { get; set; }
        public string? LinerL { get; set; }
        public string? LinerW { get; set; }
        public string? LinerDim { get; set; }
        public string? FabColor { get; set; }
        public string? IsDropLoop { get; set; }
        public string? RpFabric { get; set; }
        public string? KnotType { get; set; }
        public string? SrNo { get; set; }
        public string? MarketingInvNo { get; set; }
    }

    private sealed class BomLineRow
    {
        public int SortOrder { get; set; }
        public string? Heading { get; set; }
        public string? Gsm { get; set; }
        public string? Lami { get; set; }
        public string? Color { get; set; }
        public string? FabricSize { get; set; }
        public string? CutSize { get; set; }
        public double? TotalMtr { get; set; }
        public double? TotalKg { get; set; }
        public string? Gpm { get; set; }
        public string? Remarks { get; set; }
    }
}
