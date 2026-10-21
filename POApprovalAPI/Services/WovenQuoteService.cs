using System.Text.Json;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class WovenQuoteService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly DatabaseService _database;
    private readonly IWebHostEnvironment _environment;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _ready;

    public WovenQuoteService(DatabaseService database, IWebHostEnvironment environment)
    {
        _database = database;
        _environment = environment;
    }

    public async Task<WovenQuoteMasters> GetMastersAsync()
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        return new WovenQuoteMasters
        {
            Materials = (await con.QueryAsync<WovenQuoteMaterialRow>("SELECT Code, Name, SortOrder, InFabric, InCoating, InLiner FROM dbo.WovenQuoteMaterial ORDER BY SortOrder")).ToList(),
            Lookups = (await con.QueryAsync<WovenQuoteLookupRow>("SELECT Id, Category, Value, SortOrder FROM dbo.WovenQuoteLookup ORDER BY Category, SortOrder, Value")).ToList(),
            Constants = (await con.QueryAsync<WovenQuoteConstantRow>("SELECT [Key], Value, Label, GroupName FROM dbo.WovenQuoteConstant ORDER BY GroupName, Label")).ToList(),
            Customers = (await con.QueryAsync<WovenQuoteCustomerRow>("SELECT Id, Sector, FullName, ShortName, IsActive FROM dbo.WovenQuoteCustomer WHERE IsActive = 1 ORDER BY Sector, FullName")).ToList(),
            LatestRates = (await con.QueryAsync<WovenQuoteRateRow>("""
                SELECT r.Id, r.MaterialCode, r.Sector, r.EffectiveDate, r.Rate
                FROM dbo.WovenQuoteRmRate r
                INNER JOIN (
                    SELECT MaterialCode, Sector, MAX(EffectiveDate) AS EffectiveDate
                    FROM dbo.WovenQuoteRmRate
                    GROUP BY MaterialCode, Sector
                ) latest ON latest.MaterialCode = r.MaterialCode AND latest.Sector = r.Sector AND latest.EffectiveDate = r.EffectiveDate
                ORDER BY r.Sector, r.MaterialCode
                """)).ToList(),
        };
    }

    public async Task<IReadOnlyList<WovenQuoteRateRow>> GetRatesAsync(string? sector, string? code)
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        return (await con.QueryAsync<WovenQuoteRateRow>("""
            SELECT Id, MaterialCode, Sector, EffectiveDate, Rate
            FROM dbo.WovenQuoteRmRate
            WHERE (@sector IS NULL OR Sector = @sector)
              AND (@code IS NULL OR MaterialCode = @code)
            ORDER BY EffectiveDate DESC, MaterialCode
            """, new { sector = BlankToNull(sector), code = BlankToNull(code) })).ToList();
    }

    public async Task SaveRateAsync(WovenQuoteRateWrite row)
    {
        if (string.IsNullOrWhiteSpace(row.MaterialCode) || string.IsNullOrWhiteSpace(row.Sector))
            throw new ArgumentException("Material and sector are required.");
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        await con.ExecuteAsync("""
            MERGE dbo.WovenQuoteRmRate AS t
            USING (SELECT @code AS MaterialCode, @sector AS Sector, @date AS EffectiveDate) AS s
            ON t.MaterialCode = s.MaterialCode AND t.Sector = s.Sector AND t.EffectiveDate = s.EffectiveDate
            WHEN MATCHED THEN UPDATE SET Rate = @rate
            WHEN NOT MATCHED THEN INSERT (MaterialCode, Sector, EffectiveDate, Rate) VALUES (@code, @sector, @date, @rate);
            """, new { code = row.MaterialCode.Trim(), sector = row.Sector.Trim(), date = row.EffectiveDate.Date, rate = row.Rate });
    }

    public async Task SaveConstantsAsync(IEnumerable<WovenQuoteConstantRow> rows)
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key)) continue;
            await con.ExecuteAsync("UPDATE dbo.WovenQuoteConstant SET Value = @value WHERE [Key] = @key", new { key = row.Key.Trim(), value = row.Value });
        }
    }

    public async Task<int> SaveCustomerAsync(WovenQuoteCustomerRow row)
    {
        if (string.IsNullOrWhiteSpace(row.FullName) || string.IsNullOrWhiteSpace(row.Sector))
            throw new ArgumentException("Customer name and sector are required.");
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        var shortName = string.IsNullOrWhiteSpace(row.ShortName) ? row.FullName.Trim() : row.ShortName.Trim();
        if (row.Id > 0)
        {
            await con.ExecuteAsync("""
                UPDATE dbo.WovenQuoteCustomer
                SET Sector = @sector, FullName = @fullName, ShortName = @shortName, IsActive = @active
                WHERE Id = @id
                """, new { id = row.Id, sector = row.Sector.Trim(), fullName = row.FullName.Trim(), shortName, active = row.IsActive });
            return row.Id;
        }
        return await con.ExecuteScalarAsync<int>("""
            INSERT INTO dbo.WovenQuoteCustomer (Sector, FullName, ShortName, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@sector, @fullName, @shortName, 1)
            """, new { sector = row.Sector.Trim(), fullName = row.FullName.Trim(), shortName });
    }

    public async Task SaveLookupAsync(string category, string value)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Category and value are required.");
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        await con.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM dbo.WovenQuoteLookup WHERE Category = @category AND Value = @value)
                INSERT INTO dbo.WovenQuoteLookup (Category, Value, SortOrder)
                VALUES (@category, @value, (SELECT ISNULL(MAX(SortOrder), 0) + 1 FROM dbo.WovenQuoteLookup WHERE Category = @category))
            """, new { category = category.Trim(), value = value.Trim() });
    }

    public async Task<WovenQuoteResult> PreviewAsync(WovenQuoteDraft draft)
    {
        await EnsureReadyAsync();
        var (rates, constants, fabric, coating, liner) = await LoadCalcContextAsync(draft);
        return WovenQuoteMath.Calculate(draft, rates, constants, fabric, coating, liner);
    }

    public async Task<IReadOnlyList<WovenQuoteListItem>> ListAsync(string? sector, string? customer, string? reference, DateTime? from, DateTime? to)
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        var rows = (await con.QueryAsync<QuoteStoreRow>("""
            SELECT Id, ProductType, QuoteDate, Sector, CustomerName, OurRef, CustomerRef, Currency, Terms,
                   PricePerPiece, TotalWeightGram, PayloadJson, ResultJson, CreatedBy, UpdatedAt, IsDraft
            FROM dbo.WovenQuote
            WHERE (@sector IS NULL OR Sector = @sector)
              AND (@customer IS NULL OR CustomerName LIKE @customerLike)
              AND (@reference IS NULL OR OurRef LIKE @referenceLike OR CustomerRef LIKE @referenceLike)
              AND (@fromDate IS NULL OR QuoteDate >= @fromDate)
              AND (@toDate IS NULL OR QuoteDate <= @toDate)
            ORDER BY QuoteDate DESC, Id DESC
            """, new
        {
            sector = BlankToNull(sector),
            customer = BlankToNull(customer),
            customerLike = string.IsNullOrWhiteSpace(customer) ? null : $"%{customer.Trim()}%",
            reference = BlankToNull(reference),
            referenceLike = string.IsNullOrWhiteSpace(reference) ? null : $"%{reference.Trim()}%",
            fromDate = from?.Date,
            toDate = to?.Date,
        })).ToList();
        var materials = (await con.QueryAsync<WovenQuoteMaterialRow>("SELECT Code, Name, SortOrder, InFabric, InCoating, InLiner FROM dbo.WovenQuoteMaterial ORDER BY SortOrder")).ToList();
        var constants = (await con.QueryAsync<WovenQuoteConstantRow>("SELECT [Key], Value, Label, GroupName FROM dbo.WovenQuoteConstant")).ToDictionary(x => x.Key, x => x.Value);
        var allRates = (await con.QueryAsync<WovenQuoteRateRow>("SELECT Id, MaterialCode, Sector, EffectiveDate, Rate FROM dbo.WovenQuoteRmRate")).ToList();
        var fabric = materials.Where(m => m.InFabric).Select(m => m.Code).ToList();
        var coating = materials.Where(m => m.InCoating).Select(m => m.Code).ToList();
        var liner = materials.Where(m => m.InLiner).Select(m => m.Code).ToList();
        var list = new List<WovenQuoteListItem>(rows.Count);
        foreach (var row in rows)
        {
            var draft = JsonSerializer.Deserialize<WovenQuoteDraft>(row.PayloadJson, JsonOptions) ?? new WovenQuoteDraft();
            var quoteSector = string.IsNullOrWhiteSpace(draft.Sector) ? "Export" : draft.Sector.Trim();
            var when = (draft.QuoteDate ?? row.QuoteDate).Date;
            var rates = allRates
                .Where(r => string.Equals(r.Sector, quoteSector, StringComparison.OrdinalIgnoreCase) && r.EffectiveDate.Date <= when)
                .GroupBy(r => r.MaterialCode)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EffectiveDate).First().Rate);
            var result = WovenQuoteMath.Calculate(draft, rates, constants, fabric, coating, liner);
            list.Add(new WovenQuoteListItem
            {
                Id = row.Id,
                ProductType = row.ProductType,
                QuoteDate = row.QuoteDate,
                Sector = row.Sector,
                CustomerName = row.CustomerName,
                OurRef = row.OurRef,
                CustomerRef = row.CustomerRef,
                Currency = row.Currency,
                Terms = row.Terms,
                PricePerPiece = result.PricePerPiece,
                TotalWeightGram = result.TotalComponentGram,
                CreatedBy = row.CreatedBy,
                UpdatedAt = row.UpdatedAt,
                IsDraft = row.IsDraft,
            });
        }
        return list;
    }

    public async Task<WovenQuoteDetail?> GetAsync(int id)
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        var row = await con.QuerySingleOrDefaultAsync<QuoteStoreRow>("SELECT * FROM dbo.WovenQuote WHERE Id = @id", new { id });
        return row is null ? null : ToDetail(row);
    }

    public async Task<WovenQuoteDetail> SaveAsync(int? id, WovenQuoteDraft draft, string? username, bool isDraft = false)
    {
        if (!isDraft && string.IsNullOrWhiteSpace(draft.CustomerName))
            throw new ArgumentException("Customer is required.");
        draft.QuoteDate ??= DateTime.Today;
        if (string.IsNullOrWhiteSpace(draft.CustomerName))
            draft.CustomerName = "";
        await EnsureReadyAsync();
        var (rates, constants, fabric, coating, liner) = await LoadCalcContextAsync(draft);
        var result = WovenQuoteMath.Calculate(draft, rates, constants, fabric, coating, liner);
        var payload = JsonSerializer.Serialize(draft, JsonOptions);
        var resultJson = JsonSerializer.Serialize(result, JsonOptions);
        var weight = result.TotalComponentGram;
        using var con = _database.CreateConnection();
        if (id is null)
        {
            var newId = await con.ExecuteScalarAsync<int>("""
                INSERT INTO dbo.WovenQuote
                    (ProductType, QuoteDate, Sector, CustomerName, OurRef, CustomerRef, Currency, Terms,
                     PricePerPiece, TotalWeightGram, PayloadJson, ResultJson, CreatedBy, IsDraft)
                OUTPUT INSERTED.Id
                VALUES
                    (@productType, @quoteDate, @sector, @customer, @ourRef, @customerRef, @currency, @terms,
                     @price, @weight, @payload, @result, @createdBy, @isDraft)
                """, new
            {
                productType = draft.ProductType,
                quoteDate = draft.QuoteDate.Value.Date,
                sector = draft.Sector,
                customer = draft.CustomerName.Trim(),
                ourRef = draft.OurRef ?? "",
                customerRef = draft.CustomerRef ?? "",
                currency = draft.Currency ?? "",
                terms = draft.Terms ?? "",
                price = result.PricePerPiece,
                weight,
                payload,
                result = resultJson,
                createdBy = username ?? "",
                isDraft,
            });
            return (await GetAsync(newId))!;
        }

        var updated = await con.ExecuteAsync("""
            UPDATE dbo.WovenQuote SET
                ProductType = @productType, QuoteDate = @quoteDate, Sector = @sector, CustomerName = @customer,
                OurRef = @ourRef, CustomerRef = @customerRef, Currency = @currency, Terms = @terms,
                PricePerPiece = @price, TotalWeightGram = @weight, PayloadJson = @payload, ResultJson = @result,
                IsDraft = @isDraft, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @id
            """, new
        {
            id,
            productType = draft.ProductType,
            quoteDate = draft.QuoteDate.Value.Date,
            sector = draft.Sector,
            customer = draft.CustomerName.Trim(),
            ourRef = draft.OurRef ?? "",
            customerRef = draft.CustomerRef ?? "",
            currency = draft.Currency ?? "",
            terms = draft.Terms ?? "",
            price = result.PricePerPiece,
            weight,
            payload,
            result = resultJson,
            isDraft,
        });
        if (updated == 0) throw new ArgumentException("Quote was not found.");
        return (await GetAsync(id.Value))!;
    }

    public async Task DeleteAsync(int id)
    {
        await EnsureReadyAsync();
        using var con = _database.CreateConnection();
        await con.ExecuteAsync("DELETE FROM dbo.WovenQuote WHERE Id = @id", new { id });
    }

    private async Task<(Dictionary<string, decimal> Rates, Dictionary<string, decimal> Constants, List<string> Fabric, List<string> Coating, List<string> Liner)> LoadCalcContextAsync(WovenQuoteDraft draft)
    {
        using var con = _database.CreateConnection();
        var materials = (await con.QueryAsync<WovenQuoteMaterialRow>("SELECT Code, Name, SortOrder, InFabric, InCoating, InLiner FROM dbo.WovenQuoteMaterial ORDER BY SortOrder")).ToList();
        var constants = (await con.QueryAsync<WovenQuoteConstantRow>("SELECT [Key], Value, Label, GroupName FROM dbo.WovenQuoteConstant")).ToDictionary(x => x.Key, x => x.Value);
        var when = (draft.QuoteDate ?? DateTime.Today).Date;
        var sector = string.IsNullOrWhiteSpace(draft.Sector) ? "Export" : draft.Sector.Trim();
        var rates = (await con.QueryAsync<WovenQuoteRateRow>("""
            SELECT r.MaterialCode, r.Rate
            FROM dbo.WovenQuoteRmRate r
            INNER JOIN (
                SELECT MaterialCode, MAX(EffectiveDate) AS EffectiveDate
                FROM dbo.WovenQuoteRmRate
                WHERE Sector = @sector AND EffectiveDate <= @when
                GROUP BY MaterialCode
            ) pick ON pick.MaterialCode = r.MaterialCode AND pick.EffectiveDate = r.EffectiveDate
            WHERE r.Sector = @sector
            """, new { sector, when })).ToDictionary(x => x.MaterialCode, x => x.Rate);
        return (
            rates,
            constants,
            materials.Where(m => m.InFabric).Select(m => m.Code).ToList(),
            materials.Where(m => m.InCoating).Select(m => m.Code).ToList(),
            materials.Where(m => m.InLiner).Select(m => m.Code).ToList());
    }

    private static WovenQuoteDetail ToDetail(QuoteStoreRow row)
    {
        return new WovenQuoteDetail
        {
            Id = row.Id,
            ProductType = row.ProductType,
            QuoteDate = row.QuoteDate,
            Sector = row.Sector,
            CustomerName = row.CustomerName,
            OurRef = row.OurRef,
            CustomerRef = row.CustomerRef,
            Currency = row.Currency,
            Terms = row.Terms,
            PricePerPiece = row.PricePerPiece,
            TotalWeightGram = row.TotalWeightGram,
            CreatedBy = row.CreatedBy,
            UpdatedAt = row.UpdatedAt,
            IsDraft = row.IsDraft,
            Draft = JsonSerializer.Deserialize<WovenQuoteDraft>(row.PayloadJson, JsonOptions) ?? new WovenQuoteDraft(),
            Result = JsonSerializer.Deserialize<WovenQuoteResult>(row.ResultJson, JsonOptions) ?? new WovenQuoteResult(),
        };
    }

    private async Task EnsureReadyAsync()
    {
        if (_ready) return;
        await _gate.WaitAsync();
        try
        {
            if (_ready) return;
            using var con = _database.CreateConnection();
            await con.ExecuteAsync(SchemaSql);
            var count = await con.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.WovenQuoteMaterial");
            if (count == 0)
                await SeedAsync(con);
            await con.ExecuteAsync("""
                IF NOT EXISTS (SELECT 1 FROM dbo.WovenQuoteConstant WHERE [Key] = 'adPatchGsmExtra')
                    INSERT INTO dbo.WovenQuoteConstant ([Key], Value, Label, GroupName) VALUES
                    ('adPatchGsmExtra', 20, 'AD Star patch GSM extra', 'AD Star'),
                    ('adClosedLengthExtra', 3, 'AD Star closed-mouth length extra cm', 'AD Star'),
                    ('adOpenLengthExtra', 1.5, 'AD Star open-mouth length extra cm', 'AD Star'),
                    ('adLinerWidthExtra', 3, 'AD Star liner width extra cm', 'AD Star')
                """);
            var quotes = await con.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.WovenQuote");
            if (quotes == 0)
                await InsertSampleAsync(con);
            _ready = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> ReplaceRatesFromSeedAsync()
    {
        await EnsureReadyAsync();
        var seed = await ReadSeedAsync();
        using var con = _database.CreateConnection();
        if (con.State != System.Data.ConnectionState.Open) con.Open();
        using var tx = con.BeginTransaction();
        await con.ExecuteAsync("DELETE FROM dbo.WovenQuoteRmRate", transaction: tx);
        foreach (var row in seed.Rates)
        {
            await con.ExecuteAsync("""
                INSERT INTO dbo.WovenQuoteRmRate (MaterialCode, Sector, EffectiveDate, Rate)
                VALUES (@code, @sector, @effectiveDate, @rate)
                """, row, tx);
        }
        tx.Commit();
        return seed.Rates.Count;
    }

    private async Task<SeedFile> ReadSeedAsync()
    {
        var path = Path.Combine(_environment.ContentRootPath, "Data", "WovenQuote", "seed.json");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", "WovenQuote", "seed.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("Woven quote seed file was not found.");
        return JsonSerializer.Deserialize<SeedFile>(await File.ReadAllTextAsync(path), JsonOptions)
            ?? throw new InvalidOperationException("Woven quote seed file is empty.");
    }

    private async Task SeedAsync(System.Data.IDbConnection con)
    {
        var seed = await ReadSeedAsync();
        using var tx = con.BeginTransaction();
        foreach (var row in seed.Materials)
        {
            await con.ExecuteAsync("""
                INSERT INTO dbo.WovenQuoteMaterial (Code, Name, SortOrder, InFabric, InCoating, InLiner)
                VALUES (@code, @name, @sortOrder, @inFabric, @inCoating, @inLiner)
                """, row, tx);
        }
        foreach (var row in seed.Lookups)
        {
            await con.ExecuteAsync("INSERT INTO dbo.WovenQuoteLookup (Category, Value, SortOrder) VALUES (@category, @value, @sortOrder)", row, tx);
        }
        foreach (var row in seed.Constants)
        {
            await con.ExecuteAsync("INSERT INTO dbo.WovenQuoteConstant ([Key], Value, Label, GroupName) VALUES (@key, @value, @label, @groupName)", row, tx);
        }
        foreach (var row in seed.Customers)
        {
            await con.ExecuteAsync("INSERT INTO dbo.WovenQuoteCustomer (Sector, FullName, ShortName, IsActive) VALUES (@sector, @fullName, @shortName, 1)", row, tx);
        }
        foreach (var row in seed.Rates)
        {
            await con.ExecuteAsync("""
                INSERT INTO dbo.WovenQuoteRmRate (MaterialCode, Sector, EffectiveDate, Rate)
                VALUES (@code, @sector, @effectiveDate, @rate)
                """, row, tx);
        }
        tx.Commit();
    }

    private async Task InsertSampleAsync(System.Data.IDbConnection con)
    {
        var draft = new WovenQuoteDraft
        {
            ProductType = "WovenBag",
            QuoteDate = new DateTime(2026, 5, 19),
            Sector = "Export",
            CustomerName = "ISP Korea",
            OurRef = "100X155",
            CustomerRef = "100X155",
            BagType = "Tubular Bag",
            FabricColour = "Coloured",
            FaceWidthCm = 100,
            GussetCm = 0,
            LengthCm = 155,
            Top = "Hemmed",
            TotalWeightGram = 273,
            FabricGsm = 58,
            LinerMicron = 20,
            InkGsm = 0,
            CoatingGsm = 0,
            HandleGram = 15.6m,
            FabricRecipe = new Dictionary<string, decimal> { ["PP"] = 92, ["FILLER"] = 5, ["COL"] = 3 },
            CoatingRecipe = new Dictionary<string, decimal> { ["LDPE7"] = 20, ["PP_H350"] = 80 },
            LinerRecipe = new Dictionary<string, decimal> { ["LLDPE"] = 90, ["LDPE_FY"] = 10 },
            FabricConversion = 25,
            CoatingConversion = 14,
            LinerConversion = 14,
            WastageFabric = 0.05m,
            WastageLiner = 0.02m,
            WastageFinishing = 0.03m,
            RealisationFabric = 50,
            RealisationLiner = 40,
            BagFinishing = 1.4m,
            FlexoPrinting = 0.6m,
            PackingPerKg = 1,
            PalletPerKg = 4,
            FobCostPerKg = 3.5m,
            PaymentDays = 30,
            ProfitPercent = 8,
            UsdInr = 88,
            EuroPerUsd = 1.05m,
            GbpPerUsd = 1.23m,
            DimensionUnit = "CM",
            Terms = "FOB",
            SeaPort = "Mundra",
            Currency = "USD",
            Packing = "Floor Loaded",
            PcsPerBale = 500,
        };
        var materials = (await con.QueryAsync<WovenQuoteMaterialRow>("SELECT Code, InFabric, InCoating, InLiner, Name, SortOrder FROM dbo.WovenQuoteMaterial")).ToList();
        var constants = (await con.QueryAsync<WovenQuoteConstantRow>("SELECT [Key], Value FROM dbo.WovenQuoteConstant")).ToDictionary(x => x.Key, x => x.Value);
        var rates = (await con.QueryAsync<WovenQuoteRateRow>("""
            SELECT r.MaterialCode, r.Rate
            FROM dbo.WovenQuoteRmRate r
            INNER JOIN (
                SELECT MaterialCode, MAX(EffectiveDate) AS EffectiveDate
                FROM dbo.WovenQuoteRmRate
                WHERE Sector = 'Export' AND EffectiveDate <= '2026-05-19'
                GROUP BY MaterialCode
            ) pick ON pick.MaterialCode = r.MaterialCode AND pick.EffectiveDate = r.EffectiveDate
            WHERE r.Sector = 'Export'
            """)).ToDictionary(x => x.MaterialCode, x => x.Rate);
        var result = WovenQuoteMath.Calculate(
            draft,
            rates,
            constants,
            materials.Where(m => m.InFabric).Select(m => m.Code).ToList(),
            materials.Where(m => m.InCoating).Select(m => m.Code).ToList(),
            materials.Where(m => m.InLiner).Select(m => m.Code).ToList());
        await con.ExecuteAsync("""
            INSERT INTO dbo.WovenQuote
                (ProductType, QuoteDate, Sector, CustomerName, OurRef, CustomerRef, Currency, Terms,
                 PricePerPiece, TotalWeightGram, PayloadJson, ResultJson, CreatedBy)
            VALUES
                ('WovenBag', '2026-05-19', 'Export', 'ISP Korea', '100X155', '100X155', 'USD', 'FOB',
                 @price, 273, @payload, @result, 'workbook')
            """, new
        {
            price = result.PricePerPiece,
            payload = JsonSerializer.Serialize(draft, JsonOptions),
            result = JsonSerializer.Serialize(result, JsonOptions),
        });
    }

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private const string SchemaSql = """
        IF OBJECT_ID(N'dbo.WovenQuoteMaterial', N'U') IS NULL
        CREATE TABLE dbo.WovenQuoteMaterial (
            Code VARCHAR(30) NOT NULL CONSTRAINT PK_WovenQuoteMaterial PRIMARY KEY,
            Name NVARCHAR(120) NOT NULL,
            SortOrder INT NOT NULL,
            InFabric BIT NOT NULL,
            InCoating BIT NOT NULL,
            InLiner BIT NOT NULL
        );
        IF OBJECT_ID(N'dbo.WovenQuoteRmRate', N'U') IS NULL
        CREATE TABLE dbo.WovenQuoteRmRate (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WovenQuoteRmRate PRIMARY KEY,
            MaterialCode VARCHAR(30) NOT NULL,
            Sector VARCHAR(20) NOT NULL,
            EffectiveDate DATE NOT NULL,
            Rate DECIMAL(18,6) NOT NULL,
            CONSTRAINT UQ_WovenQuoteRmRate UNIQUE (MaterialCode, Sector, EffectiveDate)
        );
        IF OBJECT_ID(N'dbo.WovenQuoteCustomer', N'U') IS NULL
        CREATE TABLE dbo.WovenQuoteCustomer (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WovenQuoteCustomer PRIMARY KEY,
            Sector VARCHAR(20) NOT NULL,
            FullName NVARCHAR(200) NOT NULL,
            ShortName NVARCHAR(200) NOT NULL,
            IsActive BIT NOT NULL CONSTRAINT DF_WovenQuoteCustomer_Active DEFAULT (1)
        );
        IF OBJECT_ID(N'dbo.WovenQuoteLookup', N'U') IS NULL
        CREATE TABLE dbo.WovenQuoteLookup (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WovenQuoteLookup PRIMARY KEY,
            Category VARCHAR(40) NOT NULL,
            Value NVARCHAR(80) NOT NULL,
            SortOrder INT NOT NULL
        );
        IF OBJECT_ID(N'dbo.WovenQuoteConstant', N'U') IS NULL
        CREATE TABLE dbo.WovenQuoteConstant (
            [Key] VARCHAR(60) NOT NULL CONSTRAINT PK_WovenQuoteConstant PRIMARY KEY,
            Value DECIMAL(18,6) NOT NULL,
            Label NVARCHAR(160) NOT NULL,
            GroupName VARCHAR(40) NOT NULL
        );
        IF OBJECT_ID(N'dbo.WovenQuote', N'U') IS NULL
        CREATE TABLE dbo.WovenQuote (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WovenQuote PRIMARY KEY,
            ProductType VARCHAR(20) NOT NULL,
            QuoteDate DATE NOT NULL,
            Sector VARCHAR(20) NOT NULL,
            CustomerName NVARCHAR(200) NOT NULL,
            OurRef NVARCHAR(120) NOT NULL CONSTRAINT DF_WovenQuote_OurRef DEFAULT (''),
            CustomerRef NVARCHAR(120) NOT NULL CONSTRAINT DF_WovenQuote_CustomerRef DEFAULT (''),
            Currency VARCHAR(10) NOT NULL CONSTRAINT DF_WovenQuote_Currency DEFAULT (''),
            Terms NVARCHAR(40) NOT NULL CONSTRAINT DF_WovenQuote_Terms DEFAULT (''),
            PricePerPiece DECIMAL(18,6) NOT NULL,
            TotalWeightGram DECIMAL(18,4) NOT NULL,
            PayloadJson NVARCHAR(MAX) NOT NULL,
            ResultJson NVARCHAR(MAX) NOT NULL,
            CreatedBy NVARCHAR(80) NOT NULL CONSTRAINT DF_WovenQuote_CreatedBy DEFAULT (''),
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_WovenQuote_CreatedAt DEFAULT (SYSUTCDATETIME()),
            UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WovenQuote_UpdatedAt DEFAULT (SYSUTCDATETIME()),
            IsDraft BIT NOT NULL CONSTRAINT DF_WovenQuote_IsDraft DEFAULT (0)
        );
        IF COL_LENGTH(N'dbo.WovenQuote', N'IsDraft') IS NULL
            ALTER TABLE dbo.WovenQuote ADD IsDraft BIT NOT NULL CONSTRAINT DF_WovenQuote_IsDraft DEFAULT (0);
        """;

    private sealed class QuoteStoreRow
    {
        public int Id { get; set; }
        public string ProductType { get; set; } = "";
        public DateTime QuoteDate { get; set; }
        public string Sector { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string OurRef { get; set; } = "";
        public string CustomerRef { get; set; } = "";
        public string Currency { get; set; } = "";
        public string Terms { get; set; } = "";
        public decimal PricePerPiece { get; set; }
        public decimal TotalWeightGram { get; set; }
        public string PayloadJson { get; set; } = "";
        public string ResultJson { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
        public bool IsDraft { get; set; }
    }

    private sealed class SeedFile
    {
        public List<WovenQuoteMaterialRow> Materials { get; set; } = [];
        public List<SeedRate> Rates { get; set; } = [];
        public List<SeedCustomer> Customers { get; set; } = [];
        public List<WovenQuoteLookupRow> Lookups { get; set; } = [];
        public List<WovenQuoteConstantRow> Constants { get; set; } = [];
    }

    private sealed class SeedRate
    {
        public string Code { get; set; } = "";
        public string Sector { get; set; } = "";
        public DateTime EffectiveDate { get; set; }
        public decimal Rate { get; set; }
    }

    private sealed class SeedCustomer
    {
        public string Sector { get; set; } = "";
        public string FullName { get; set; } = "";
        public string ShortName { get; set; } = "";
    }
}
