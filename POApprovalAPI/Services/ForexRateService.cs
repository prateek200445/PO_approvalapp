using System.Net.Http.Json;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

public sealed class ForexRateService
{
    private readonly HttpClient _http;
    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ForexRateService> _logger;
    private static readonly SemaphoreSlim SchemaGate = new(1, 1);
    private static bool _schemaReady;

    public ForexRateService(
        HttpClient http,
        DatabaseService database,
        IMemoryCache cache,
        ILogger<ForexRateService> logger)
    {
        _http = http;
        _database = database;
        _cache = cache;
        _logger = logger;
    }

    public Task<ForexRate> GetUsdInrAsync(DateTime asOf, CancellationToken ct = default) =>
        GetRateAsync("USD", "INR", asOf, ct);

    public Task<ForexRate> GetEurUsdAsync(DateTime asOf, CancellationToken ct = default) =>
        GetRateAsync("EUR", "USD", asOf, ct);

    public Task<ForexRate> GetGbpUsdAsync(DateTime asOf, CancellationToken ct = default) =>
        GetRateAsync("GBP", "USD", asOf, ct);

    public Task<ForexRate> GetEurInrAsync(DateTime asOf, CancellationToken ct = default) =>
        GetRateAsync("EUR", "INR", asOf, ct);

    public Task<ForexRate> GetGbpInrAsync(DateTime asOf, CancellationToken ct = default) =>
        GetRateAsync("GBP", "INR", asOf, ct);

    private async Task<ForexRate> GetRateAsync(
        string baseCurrency,
        string quoteCurrency,
        DateTime asOf,
        CancellationToken ct)
    {
        asOf = asOf.Date;
        var pair = $"{baseCurrency}/{quoteCurrency}";
        var cacheKey = $"forex:{baseCurrency.ToLowerInvariant()}-{quoteCurrency.ToLowerInvariant()}:{asOf:yyyy-MM-dd}";
        if (_cache.TryGetValue(cacheKey, out ForexRate? cached) && cached is not null)
            return cached;

        await EnsureCacheTableAsync(ct);
        try
        {
            var path = $"v2/rates?date={asOf:yyyy-MM-dd}&base={baseCurrency}&quotes={quoteCurrency}";
            var rows = await _http.GetFromJsonAsync<List<FrankfurterRate>>(path, ct);
            var row = rows?.FirstOrDefault(x =>
                x.Base.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase)
                && x.Quote.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)
                && x.Rate > 0);
            if (row is not null && DateTime.TryParse(row.Date, out var rateDate))
            {
                var result = new ForexRate
                {
                    AsOf = asOf,
                    RateDate = rateDate.Date,
                    Rate = row.Rate,
                    Source = "Frankfurter",
                    UsedFallback = rateDate.Date != asOf,
                };
                await SaveCacheRowAsync(result, baseCurrency, quoteCurrency, ct);
                StoreMemory(cacheKey, result, asOf);
                return result;
            }
            _logger.LogWarning("Frankfurter returned no {Pair} rate for {AsOf:yyyy-MM-dd}", pair, asOf);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Frankfurter {Pair} lookup failed for {AsOf:yyyy-MM-dd}; using a stored fallback", pair, asOf);
        }

        var fallback = await LoadStoredFallbackAsync(baseCurrency, quoteCurrency, asOf, ct)
            ?? throw new InvalidOperationException($"No {pair} forex rate is available for {asOf:yyyy-MM-dd}.");
        StoreMemory(cacheKey, fallback, asOf);
        return fallback;
    }

    private void StoreMemory(string key, ForexRate value, DateTime asOf)
    {
        var ttl = asOf >= DateTime.Today.AddDays(-1) ? TimeSpan.FromHours(6) : TimeSpan.FromDays(7);
        _cache.Set(key, value, ttl);
    }

    private async Task SaveCacheRowAsync(
        ForexRate row,
        string baseCurrency,
        string quoteCurrency,
        CancellationToken ct)
    {
        using var con = _database.CreateConnection();
        await con.ExecuteAsync(new CommandDefinition("""
            MERGE dbo.AppForexRateCache AS target
            USING (SELECT @AsOfDate AS AsOfDate, @BaseCurrency AS BaseCurrency, @QuoteCurrency AS QuoteCurrency) AS source
            ON target.AsOfDate = source.AsOfDate
               AND target.BaseCurrency = source.BaseCurrency
               AND target.QuoteCurrency = source.QuoteCurrency
            WHEN MATCHED THEN UPDATE SET
                RateDate = @RateDate, Rate = @Rate, SourceName = @SourceName, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT
                (AsOfDate, BaseCurrency, QuoteCurrency, RateDate, Rate, SourceName)
                VALUES (@AsOfDate, @BaseCurrency, @QuoteCurrency, @RateDate, @Rate, @SourceName);
            """, new
        {
            AsOfDate = row.AsOf,
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency,
            row.RateDate,
            row.Rate,
            SourceName = row.Source,
        }, cancellationToken: ct));
    }

    private async Task<ForexRate?> LoadStoredFallbackAsync(
        string baseCurrency,
        string quoteCurrency,
        DateTime asOf,
        CancellationToken ct)
    {
        using var con = _database.CreateConnection();
        var cached = await con.QueryFirstOrDefaultAsync<StoredForexRow>(new CommandDefinition("""
            SELECT TOP 1 AsOfDate, RateDate, Rate, SourceName
            FROM dbo.AppForexRateCache WITH (NOLOCK)
            WHERE BaseCurrency = @BaseCurrency
              AND QuoteCurrency = @QuoteCurrency
              AND AsOfDate <= @AsOf
              AND RateDate <= @AsOf
            ORDER BY CASE WHEN AsOfDate = @AsOf THEN 0 ELSE 1 END, AsOfDate DESC
            """, new { AsOf = asOf, BaseCurrency = baseCurrency, QuoteCurrency = quoteCurrency }, cancellationToken: ct));
        if (cached is not null && cached.Rate > 0)
        {
            return new ForexRate
            {
                AsOf = asOf,
                RateDate = cached.RateDate,
                Rate = cached.Rate,
                Source = $"{cached.SourceName} cache",
                UsedFallback = true,
            };
        }

        var internalRate = await LoadInternalFallbackAsync(con, baseCurrency, quoteCurrency, asOf, ct);
        return internalRate is null || internalRate.Rate <= 0
            ? null
            : new ForexRate
            {
                AsOf = asOf,
                RateDate = internalRate.RateDate,
                Rate = internalRate.Rate,
                Source = $"{internalRate.Source} fallback",
                UsedFallback = true,
            };
    }

    private static async Task<InternalForexRow?> LoadInternalFallbackAsync(
        System.Data.IDbConnection con,
        string baseCurrency,
        string quoteCurrency,
        DateTime asOf,
        CancellationToken ct)
    {
        if (baseCurrency == "USD" && quoteCurrency == "INR")
        {
            return await con.QueryFirstOrDefaultAsync<InternalForexRow>(new CommandDefinition("""
                SELECT TOP 1
                    CAST(todate AS date) AS RateDate,
                    CAST(Dollar AS decimal(18, 6)) AS Rate,
                    'vw_CurrencyImport' AS Source
                FROM dbo.vw_CurrencyImport WITH (NOLOCK)
                WHERE CAST(ISNULL(Dollar, 0) AS decimal(18, 6)) > 0
                  AND CAST(todate AS date) <= @AsOf
                ORDER BY
                    CASE
                        WHEN @AsOf BETWEEN CAST(todate AS date) AND CAST(todate1 AS date)
                        THEN 0 ELSE 1
                    END,
                    todate DESC
                """, new { AsOf = asOf }, cancellationToken: ct));
        }

        var column = (baseCurrency, quoteCurrency) switch
        {
            ("EUR", "INR") => "Euro",
            ("GBP", "INR") => "Pound",
            ("EUR", "USD") => "eurousd",
            ("GBP", "USD") => "gbpusd",
            _ => null,
        };
        if (column is null) return null;
        var sql = $"""
            SELECT TOP 1
                CAST(sysdate AS date) AS RateDate,
                CAST([{column}] AS decimal(18, 6)) AS Rate,
                'Currency_RBI' AS Source
            FROM dbo.Currency_RBI WITH (NOLOCK)
            WHERE CAST(ISNULL([{column}], 0) AS decimal(18, 6)) > 0
              AND CAST(sysdate AS date) <= @AsOf
            ORDER BY
                CASE
                    WHEN @AsOf BETWEEN CAST(sysdate AS date)
                        AND CAST(ISNULL(todate, DATEADD(YEAR, 5, GETDATE())) AS date)
                    THEN 0 ELSE 1
                END,
                sysdate DESC
            """;
        return await con.QueryFirstOrDefaultAsync<InternalForexRow>(
            new CommandDefinition(sql, new { AsOf = asOf }, cancellationToken: ct));
    }

    private async Task EnsureCacheTableAsync(CancellationToken ct)
    {
        if (_schemaReady) return;
        await SchemaGate.WaitAsync(ct);
        try
        {
            if (_schemaReady) return;
            using var con = _database.CreateConnection();
            await con.ExecuteAsync(new CommandDefinition("""
                IF OBJECT_ID(N'dbo.AppForexRateCache', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.AppForexRateCache
                    (
                        AsOfDate date NOT NULL,
                        BaseCurrency char(3) NOT NULL,
                        QuoteCurrency char(3) NOT NULL,
                        RateDate date NOT NULL,
                        Rate decimal(18, 6) NOT NULL,
                        SourceName nvarchar(80) NOT NULL,
                        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_AppForexRateCache_UpdatedAt DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_AppForexRateCache PRIMARY KEY (AsOfDate, BaseCurrency, QuoteCurrency)
                    );
                END
                """, cancellationToken: ct));
            _schemaReady = true;
        }
        finally
        {
            SchemaGate.Release();
        }
    }

    private sealed class FrankfurterRate
    {
        public string Date { get; set; } = "";
        public string Base { get; set; } = "";
        public string Quote { get; set; } = "";
        public decimal Rate { get; set; }
    }

    private sealed class StoredForexRow
    {
        public DateTime AsOfDate { get; set; }
        public DateTime RateDate { get; set; }
        public decimal Rate { get; set; }
        public string SourceName { get; set; } = "";
    }

    private sealed class InternalForexRow
    {
        public DateTime RateDate { get; set; }
        public decimal Rate { get; set; }
        public string Source { get; set; } = "";
    }
}

public sealed class ForexRate
{
    public DateTime AsOf { get; set; }
    public DateTime RateDate { get; set; }
    public decimal Rate { get; set; }
    public string Source { get; set; } = "";
    public bool UsedFallback { get; set; }
}
