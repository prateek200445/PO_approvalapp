using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace POApprovalAPI.Services;

public sealed class FibcBuyersOptions
{
    public const string SectionName = "FibcBuyers";
    public string DataDirectory { get; set; } = "";
    public int MaxSyncRecords { get; set; } = 20;
    public string EximLoginUrl { get; set; } = "https://www.ex-im.cloud/login";
    public string EximMarketingUrl { get; set; } = "https://www.ex-im.com/";
}

public sealed class FibcBuyersService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] DefaultKeywords =
    [
        "FIBC", "FIBC Bags", "Jumbo Bags", "Jumbo Bag", "Bulk Bags", "Bulk Bag",
        "Flexible Intermediate Bulk Container", "Big Bags", "PP Jumbo Bags",
        "Polypropylene Bulk Bags", "Super Sacks",
    ];

    private static readonly string[] DefaultHsCodes = ["630532", "63053200"];

    private readonly object _gate = new();
    private readonly string _storePath;
    private readonly string _rawDir;
    private readonly IConfiguration _config;
    private readonly ILogger<FibcBuyersService> _log;

    public FibcBuyersService(IConfiguration config, IHostEnvironment env, ILogger<FibcBuyersService> log)
    {
        _config = config;
        _log = log;
        var configured = config[$"{FibcBuyersOptions.SectionName}:DataDirectory"];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "Data", "FibcBuyers")
            : configured;
        Directory.CreateDirectory(root);
        _rawDir = Path.Combine(root, "raw");
        Directory.CreateDirectory(_rawDir);
        _storePath = Path.Combine(root, "store.json");
    }

    public object GetCredentialStatus()
    {
        var user = GetEximUsername();
        var hasPassword = !string.IsNullOrWhiteSpace(GetEximPassword());
        return new
        {
            usernameConfigured = !string.IsNullOrWhiteSpace(user),
            username = MaskEmail(user),
            passwordConfigured = hasPassword,
            loginUrl = GetEximLoginUrl(),
            marketingUrl = GetEximMarketingUrl(),
            note = "Credentials are server-side only (EXIM_USERNAME / EXIM_PASSWORD). Never sent to the browser. Member portal: ex-im.cloud (marketing site: ex-im.com).",
        };
    }

    public FibcDashboardDto GetDashboard(FibcBuyerQuery q)
    {
        var store = LoadStore();
        var buyers = FilterBuyers(store.Buyers, q).ToList();
        var shipments = store.Shipments
            .Where(s => buyers.Any(b => b.Id == s.BuyerId))
            .ToList();

        // Banner KPIs ignore GenuineOnly so totals stay visible while the list is filtered.
        var kpiQuery = new FibcBuyerQuery
        {
            Country = q.Country,
            Buyer = q.Buyer,
            Keyword = q.Keyword,
            MinScore = q.MinScore,
            GenuineOnly = null,
        };
        var kpiBuyers = FilterBuyers(store.Buyers, kpiQuery).ToList();
        var kpiShipments = store.Shipments
            .Where(s => kpiBuyers.Any(b => b.Id == s.BuyerId))
            .ToList();

        return new FibcDashboardDto
        {
            Kpis = new FibcKpisDto
            {
                TotalBuyers = kpiBuyers.Count,
                TotalShipments = kpiShipments.Count,
                QualifiedBuyers = kpiBuyers.Count(b => b.Score >= 60),
                GenuineBuyers = kpiBuyers.Count(b => b.IsGenuine),
                HotBuyers = kpiBuyers.Count(b => b.Score >= 90),
                Countries = kpiBuyers.Select(b => b.Country).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Suppliers = kpiShipments.Select(s => s.SupplierName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            },
            TopCountries = buyers
                .Where(b => !string.IsNullOrWhiteSpace(b.Country))
                .GroupBy(b => b.Country!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new FibcNamedCountDto { Name = g.Key, Count = g.Sum(x => x.ShipmentCount) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList(),
            ScoreDistribution = new[]
            {
                new FibcNamedCountDto { Name = "HOT 90-100", Count = buyers.Count(b => b.Score >= 90) },
                new FibcNamedCountDto { Name = "HIGH 75-89", Count = buyers.Count(b => b.Score is >= 75 and < 90) },
                new FibcNamedCountDto { Name = "POTENTIAL 60-74", Count = buyers.Count(b => b.Score is >= 60 and < 75) },
                new FibcNamedCountDto { Name = "LOW 40-59", Count = buyers.Count(b => b.Score is >= 40 and < 60) },
                new FibcNamedCountDto { Name = "LOW 0-39", Count = buyers.Count(b => b.Score < 40) },
            }.ToList(),
            HighPotential = buyers.OrderByDescending(b => b.Score).ThenByDescending(b => b.ShipmentCount).Take(25).ToList(),
            TopBuyerCountriesByValue = RankShipmentGroupsByValue(
                kpiShipments
                    .Where(s => !string.IsNullOrWhiteSpace(s.DestinationCountry))
                    .GroupBy(s => s.DestinationCountry!, StringComparer.OrdinalIgnoreCase)),
            TopSellerCountriesByValue = RankShipmentGroupsByValue(
                kpiShipments
                    .Where(s => !string.IsNullOrWhiteSpace(s.SupplierCountry) || !string.IsNullOrWhiteSpace(s.OriginCountry))
                    .GroupBy(s => (s.SupplierCountry ?? s.OriginCountry)!, StringComparer.OrdinalIgnoreCase)),
            TopBuyersByValue = BuildTopBuyersByValue(kpiBuyers, kpiShipments),
            TopSellersByValue = RankShipmentGroupsByValue(
                kpiShipments
                    .Where(s => !string.IsNullOrWhiteSpace(s.SupplierName) && !IsNaToken(s.SupplierName))
                    .GroupBy(s => s.SupplierName!, StringComparer.OrdinalIgnoreCase)),
            LastSync = store.LastSync,
            Keywords = DefaultKeywords.ToList(),
            HsCodes = store.ConfiguredHsCodes.Count > 0 ? store.ConfiguredHsCodes : DefaultHsCodes.ToList(),
            SourceNote = store.Shipments.Count == 0
                ? "No Ex-Im records yet. Upload a CSV/Excel export from ex-im.cloud Downloads."
                : "Dashboard values are calculated from stored Ex-Im shipment records only.",
        };
    }

    private static List<FibcNamedValueDto> BuildTopBuyersByValue(List<FibcBuyerDto> buyers, List<FibcShipmentDto> shipments)
    {
        var names = buyers.ToDictionary(b => b.Id, b => b.CompanyName, StringComparer.OrdinalIgnoreCase);
        var totals = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in shipments)
        {
            if (!names.TryGetValue(s.BuyerId, out var name) || string.IsNullOrWhiteSpace(name))
                continue;
            totals[name] = totals.GetValueOrDefault(name) + (s.Value ?? 0);
        }
        return RankNameValues(totals);
    }

    private static List<FibcNamedValueDto> RankShipmentGroupsByValue(IEnumerable<IGrouping<string, FibcShipmentDto>> groups) =>
        RankNameValues(groups.ToDictionary(g => g.Key, g => g.Sum(s => s.Value ?? 0), StringComparer.OrdinalIgnoreCase));

    private static List<FibcNamedValueDto> RankNameValues(Dictionary<string, double> totals) =>
        totals
            .Where(kv => kv.Value > 0 && !string.IsNullOrWhiteSpace(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => new FibcNamedValueDto
            {
                Name = kv.Key,
                ValueUsd = Math.Round(kv.Value, 2),
                ValueLabel = FormatUsdMillions(kv.Value),
                ShipmentCount = 0,
            })
            .ToList();

    private static string FormatUsdMillions(double usd)
    {
        if (usd >= 1_000_000)
            return $"${usd / 1_000_000d:0.00}M";
        if (usd >= 1_000)
            return $"${usd / 1_000d:0.0}K";
        return $"${usd:0}";
    }

    public object GetBuyersPage(FibcBuyerQuery q)
    {
        var store = LoadStore();
        var filtered = FilterBuyers(store.Buyers, q)
            .OrderByDescending(b => b.Score)
            .ThenByDescending(b => b.ShipmentCount)
            .ToList();
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 5, 100);
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new
        {
            total = filtered.Count,
            page,
            pageSize,
            items,
        };
    }

    public object GetImportHistory()
    {
        var store = LoadStore();
        var changed = EnsureImportHistoryBackfill(store);
        changed |= DeduplicateImportHistory(store);
        if (changed)
            SaveStore(store);
        return new { items = store.ImportHistory };
    }

    public object? GetImportHistoryDetail(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var store = LoadStore();
        if (EnsureImportHistoryBackfill(store))
            SaveStore(store);

        var entry = store.ImportHistory.FirstOrDefault(h =>
            h.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (entry == null &&
            store.LastSync != null &&
            !string.IsNullOrWhiteSpace(store.LastSync.Id) &&
            store.LastSync.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        {
            entry = store.LastSync;
        }
        if (entry == null) return null;

        var batchShipments = store.Shipments
            .Where(s => !string.IsNullOrWhiteSpace(s.ImportBatchId) &&
                        s.ImportBatchId.Equals(id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var approximate = batchShipments.Count == 0;
        if (approximate)
        {
            // Older imports (before batch tagging) — show current store as the best available snapshot.
            batchShipments = store.Shipments.ToList();
        }

        var buyerIds = batchShipments
            .Select(s => s.BuyerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buyers = store.Buyers
            .Where(b => buyerIds.Contains(b.Id))
            .OrderByDescending(b => b.IsGenuine)
            .ThenByDescending(b => b.Score)
            .ThenByDescending(b => b.ShipmentCount)
            .Take(50)
            .ToList();

        return new
        {
            history = entry,
            approximate,
            shipmentCount = batchShipments.Count,
            buyerCount = buyerIds.Count,
            topBuyers = buyers,
            sampleShipments = batchShipments
                .OrderByDescending(s => s.ShipmentDate ?? DateTime.MinValue)
                .Take(40)
                .ToList(),
        };
    }

    public FibcBuyerDetailDto? GetBuyerDetail(string buyerId)
    {
        var store = LoadStore();
        var buyer = store.Buyers.FirstOrDefault(b => b.Id.Equals(buyerId, StringComparison.OrdinalIgnoreCase));
        if (buyer == null) return null;
        var shipments = store.Shipments
            .Where(s => s.BuyerId == buyer.Id)
            .OrderByDescending(s => s.ShipmentDate ?? DateTime.MinValue)
            .ToList();
        var raw = store.RawRecords
            .Where(r => shipments.Any(s => s.RawRecordId == r.Id))
            .OrderByDescending(r => r.ExtractedAt)
            .ToList();

        var suppliers = shipments
            .Where(s => !string.IsNullOrWhiteSpace(s.SupplierName))
            .GroupBy(s => s.SupplierName!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FibcSupplierSummaryDto
            {
                Name = g.Key,
                Country = g.Select(x => x.SupplierCountry).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                ShipmentCount = g.Count(),
                FirstShipment = g.Min(x => x.ShipmentDate),
                LatestShipment = g.Max(x => x.ShipmentDate),
            })
            .OrderByDescending(x => x.ShipmentCount)
            .ToList();

        return new FibcBuyerDetailDto
        {
            Buyer = buyer,
            Shipments = shipments,
            Suppliers = suppliers,
            RawRecords = raw,
            AiSummary = BuildSummary(buyer, shipments, suppliers),
        };
    }

    public async Task<object> ImportFileAsync(Stream stream, string fileName, CancellationToken ct)
    {
        List<Dictionary<string, string>> rows;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".xlsx" or ".xlsm")
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            rows = ParseExImXlsx(ms);
        }
        else
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var text = await reader.ReadToEndAsync(ct);
            rows = ParseCsv(text);
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("No data rows found in the uploaded file.");

        return IngestRows(rows, sourceFile: fileName, searchTerm: "import-upload", hsCode: null, limit: null);
    }

    /// <summary>Backward-compatible alias for CSV uploads. </summary>
    public Task<object> ImportCsvAsync(Stream stream, string fileName, CancellationToken ct) =>
        ImportFileAsync(stream, fileName, ct);

    public object SyncLimited(FibcSyncRequest request)
    {
        // ex-im.cloud login uses Cloudflare Turnstile + OTP. Automated login/scraping from this API
        // host is blocked without an interactive browser session — do not bypass CAPTCHA/MFA.
        var username = GetEximUsername();
        var password = GetEximPassword();
        var loginUrl = GetEximLoginUrl();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new
            {
                success = false,
                blocked = true,
                reason = "missing_credentials",
                message = "Set EXIM_USERNAME and EXIM_PASSWORD in a local .env (server-side only), then retry. Preferred path: log in at ex-im.cloud, export CSV from Downloads, and use Import.",
                loginUrl,
                marketingUrl = GetEximMarketingUrl(),
            };
        }

        return new
        {
            success = false,
            blocked = true,
            reason = "cloudflare_otp_protected",
            message = "Ex-Im member login (ex-im.cloud) requires Cloudflare Turnstile and OTP — automated scrape from this API is blocked. Export 10–20 FIBC rows (HS 630532) from Downloads in your account and use Import Ex-Im CSV on this page. Interactive browser sync can be added later.",
            loginUrl,
            marketingUrl = GetEximMarketingUrl(),
            requestedLimit = Math.Clamp(request.Limit <= 0 ? 20 : request.Limit, 1, 20),
            keywords = request.Keywords?.Count > 0 ? request.Keywords : DefaultKeywords.Take(3).ToList(),
            hsCodes = request.HsCodes?.Count > 0 ? request.HsCodes : DefaultHsCodes.ToList(),
            username = MaskEmail(username),
        };
    }

    public byte[] ExportCsv(FibcBuyerQuery q)
    {
        var store = LoadStore();
        var buyers = FilterBuyers(store.Buyers, q)
            .ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);
        var shipments = store.Shipments
            .Where(s => buyers.ContainsKey(s.BuyerId))
            .OrderByDescending(s => s.ShipmentDate ?? DateTime.MinValue)
            .ThenBy(s => s.ShipmentReference)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",",
            "Shipment Date",
            "Shipment ID",
            "Buyer",
            "Buyer Country",
            "Buyer State",
            "Buyer City",
            "Buyer Address",
            "HS Code",
            "Industry",
            "Product Description",
            "Seller",
            "Seller Country",
            "Seller State",
            "Seller City",
            "Seller Address",
            "Origin Port",
            "Destination Port",
            "Unit",
            "Quantity",
            "Value (USD)",
            "Unit Price",
            "Buyer Score",
            "Status",
            "Trend"));

        foreach (var s in shipments)
        {
            buyers.TryGetValue(s.BuyerId, out var b);
            sb.AppendLine(string.Join(",",
                Csv(FormatExportDate(s.ShipmentDate)),
                Csv(CleanExport(s.ShipmentReference)),
                Csv(CleanExport(b?.CompanyName)),
                Csv(CleanExport(b?.Country ?? s.DestinationCountry)),
                Csv(CleanExport(b?.State)),
                Csv(CleanExport(b?.City)),
                Csv(CleanExport(b?.Address)),
                Csv(CleanExport(s.HsCode)),
                Csv(CleanExport(s.Industry)),
                Csv(CleanExport(s.ProductDescription)),
                Csv(CleanExport(s.SupplierName)),
                Csv(CleanExport(s.SupplierCountry ?? s.OriginCountry)),
                Csv(CleanExport(s.SupplierState)),
                Csv(CleanExport(s.SupplierCity)),
                Csv(CleanExport(s.SupplierAddress)),
                Csv(CleanExport(s.PortOfLoading)),
                Csv(CleanExport(s.PortOfDischarge)),
                Csv(CleanExport(s.QuantityUnit)),
                s.Quantity?.ToString(CultureInfo.InvariantCulture) ?? "",
                s.Value?.ToString(CultureInfo.InvariantCulture) ?? "",
                s.UnitPrice?.ToString(CultureInfo.InvariantCulture) ?? "",
                (b?.Score ?? 0).ToString(CultureInfo.InvariantCulture),
                Csv(b == null ? "" : b.IsGenuine ? "Genuine" : "Unverified"),
                Csv(CleanExport(b?.Trend))));
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8.GetBytes(sb.ToString());
    }

    private static string CleanExport(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var v = value.Trim();
        if (v.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("Not Available", StringComparison.OrdinalIgnoreCase) ||
            v == "-")
            return "";
        // Collapse noisy whitespace / newlines for a clean spreadsheet cell.
        return Regex.Replace(v, @"\s+", " ");
    }

    private static string FormatExportDate(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : "";


    private object IngestRows(
        List<Dictionary<string, string>> rows,
        string sourceFile,
        string? searchTerm,
        string? hsCode,
        int? limit)
    {
        var store = LoadStore();
        var take = limit.HasValue ? rows.Take(limit.Value).ToList() : rows;
        var now = DateTime.UtcNow;
        var newRaw = 0;
        var newShipments = 0;
        var enriched = 0;
        var shipmentByHash = store.Shipments
            .Where(s => !string.IsNullOrWhiteSpace(s.RawRecordId))
            .GroupBy(s => s.RawRecordId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var knownHashes = new HashSet<string>(shipmentByHash.Keys, StringComparer.OrdinalIgnoreCase);
        var buyerIndex = store.Buyers.ToDictionary(
            b => BuyerKey(b.NormalizedName, b.Country),
            b => b,
            StringComparer.OrdinalIgnoreCase);
        var batchId = Guid.NewGuid().ToString("N");

        foreach (var row in take)
        {
            var company = Get(row,
                "buyer", "buyer name", "importer", "importer name", "consignee", "company", "company name");
            var shipRef = Get(row, "shipment id", "bl", "bill of lading", "reference", "shipment no", "be no");
            var dateRaw = Get(row, "date", "shipment date", "arrival date", "bill date", "be date");
            var hsRaw = Get(row, "hs code", "hs", "hsn", "hsn code", "hscode");
            var valueRaw = Get(row, "value(usd)", "value usd", "usd value", "value", "invoice value", "amount");
            var dedupeKey = $"{shipRef}|{company}|{dateRaw}|{hsRaw}|{valueRaw}";
            var hash = Sha256(dedupeKey);

            if (string.IsNullOrWhiteSpace(company) || IsNaToken(company))
                continue;

            var country = Get(row, "buyer country", "destination country", "destination", "country", "import country");
            if (IsNaToken(country)) country = "";
            var city = Get(row, "buyer city", "city");
            var state = Get(row, "buyer state", "state");
            var address = Get(row, "buyer address", "address", "importer address");
            var normalized = NormalizeCompany(company);
            var bKey = BuyerKey(normalized, country);

            if (!buyerIndex.TryGetValue(bKey, out var buyer))
            {
                buyer = new FibcBuyerDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CompanyName = company.Trim(),
                    NormalizedName = normalized,
                    Country = NullIfEmpty(country),
                    State = NullIfEmpty(state),
                    City = NullIfEmpty(city),
                    Address = NullIfEmpty(address),
                    Email = NullIfEmpty(Get(row, "email", "email id", "e-mail", "buyer email", "contact email")),
                    Phone = NullIfEmpty(Get(row, "phone", "phone number", "mobile", "tel", "telephone", "buyer phone", "contact phone")),
                    DataQuality = "Verified Ex-Im Record",
                    Source = "Ex-Im",
                };
                ApplyContactFallback(buyer);
                store.Buyers.Add(buyer);
                buyerIndex[bKey] = buyer;
            }
            else
            {
                EnrichBuyerFromRow(buyer, row, city, state, address);
            }

            if (!knownHashes.Add(hash) && shipmentByHash.TryGetValue(hash, out var existingShip))
            {
                if (EnrichShipmentFromRow(existingShip, row, country, shipRef, dateRaw, hsRaw, valueRaw))
                    enriched++;
                continue;
            }

            newRaw++;
            var shipment = new FibcShipmentDto
            {
                Id = Guid.NewGuid().ToString("N"),
                BuyerId = buyer.Id,
                RawRecordId = hash,
                ImportBatchId = batchId,
                Source = "Ex-Im",
            };
            EnrichShipmentFromRow(shipment, row, country, shipRef, dateRaw, hsRaw, valueRaw);
            store.Shipments.Add(shipment);
            shipmentByHash[hash] = shipment;
            newShipments++;
        }

        store.RawRecords.Add(new FibcRawRecordDto
        {
            Id = batchId,
            ContentHash = Sha256($"{sourceFile}|{now:o}|{take.Count}|{newShipments}|{enriched}"),
            ExtractedAt = now,
            SearchTerm = searchTerm,
            HsCodeFilter = hsCode,
            SourceFile = sourceFile,
            Source = "Ex-Im",
            RawFile = null,
            Preview = $"Imported {newShipments} shipment(s) from {sourceFile} ({take.Count} row(s) scanned, {enriched} enriched).",
        });

        RecomputeBuyers(store);
        var history = new FibcSyncHistoryDto
        {
            Id = batchId,
            At = now,
            Mode = "import",
            SourceFile = sourceFile,
            RecordsProcessed = take.Count,
            NewRecords = newRaw,
            NewShipments = newShipments,
            Duplicates = Math.Max(0, take.Count - newShipments),
            Errors = 0,
            TotalBuyersAfter = store.Buyers.Count,
            TotalShipmentsAfter = store.Shipments.Count,
            GenuineBuyersAfter = store.Buyers.Count(b => b.IsGenuine),
            Message = newShipments > 0
                ? $"Imported {newShipments} new shipment(s) from {sourceFile}."
                : enriched > 0
                    ? $"Updated {enriched} existing shipment(s) from {sourceFile}."
                    : $"No new shipments from {sourceFile} (already imported or buyer N/A).",
        };
        UpsertImportHistory(store, history);
        store.LastSync = history;
        SaveStore(store);

        return new
        {
            success = true,
            importId = history.Id,
            recordsProcessed = take.Count,
            newRawRecords = newRaw,
            newShipments,
            enriched,
            totalBuyers = store.Buyers.Count,
            totalShipments = store.Shipments.Count,
            lastSync = store.LastSync,
        };
    }

    private static void EnrichBuyerFromRow(
        FibcBuyerDto buyer,
        Dictionary<string, string> row,
        string city,
        string state,
        string address)
    {
        if (string.IsNullOrWhiteSpace(buyer.State)) buyer.State = NullIfEmpty(state);
        if (string.IsNullOrWhiteSpace(buyer.City)) buyer.City = NullIfEmpty(city);
        if (string.IsNullOrWhiteSpace(buyer.Address)) buyer.Address = NullIfEmpty(address);
        if (string.IsNullOrWhiteSpace(buyer.Email))
            buyer.Email = NullIfEmpty(Get(row, "email", "email id", "e-mail", "buyer email", "contact email"));
        if (string.IsNullOrWhiteSpace(buyer.Phone))
            buyer.Phone = NullIfEmpty(Get(row, "phone", "phone number", "mobile", "tel", "telephone", "buyer phone", "contact phone"));
        ApplyContactFallback(buyer);
    }

    private static bool EnrichShipmentFromRow(
        FibcShipmentDto s,
        Dictionary<string, string> row,
        string destinationCountry,
        string shipRef,
        string dateRaw,
        string hsRaw,
        string valueRaw)
    {
        var changed = false;
        var shipDate = ParseDate(dateRaw);

        string? Fill(string? current, string? incoming)
        {
            var v = NullIfEmpty(incoming);
            if (v == null) return current;
            if (string.IsNullOrWhiteSpace(current))
            {
                changed = true;
                return v;
            }
            if (v.Length > current.Length + 8)
            {
                changed = true;
                return v;
            }
            return current;
        }

        double? FillNum(double? current, double? incoming)
        {
            if (incoming == null) return current;
            if (current == null)
            {
                changed = true;
                return incoming;
            }
            return current;
        }

        if (s.ShipmentDate == null && shipDate != null)
        {
            s.ShipmentDate = shipDate;
            changed = true;
        }

        s.ShipmentReference = Fill(s.ShipmentReference, shipRef);
        s.HsCode = Fill(s.HsCode, hsRaw);
        s.Industry = Fill(s.Industry, Get(row, "industry"));
        s.ProductDescription = Fill(
            s.ProductDescription,
            Get(row, "product description", "product", "item description", "description"));
        s.Quantity = FillNum(s.Quantity, ParseDouble(Get(row, "quantity", "qty", "qnty")));
        s.QuantityUnit = Fill(s.QuantityUnit, GetExact(row, "UNIT") ?? Get(row, "qty unit", "quantity unit"));
        s.Weight = FillNum(s.Weight, ParseDouble(Get(row, "weight", "net weight", "gross weight")));
        s.WeightUnit = Fill(s.WeightUnit, Get(row, "weight unit"));
        s.Value = FillNum(s.Value, ParseDouble(valueRaw));
        s.UnitPrice = FillNum(s.UnitPrice, ParseDouble(Get(row, "unit price", "unitprice", "price")));
        s.Currency = Fill(s.Currency, Get(row, "currency", "curr")) ?? s.Currency ?? "USD";
        s.TradeDirection = Fill(s.TradeDirection, Get(row, "trade direction", "direction")) ?? s.TradeDirection ?? "Import";
        s.OriginCountry = Fill(
            s.OriginCountry,
            Get(row, "seller country", "origin country", "country of origin", "supplier country", "origin"));
        s.DestinationCountry = Fill(s.DestinationCountry, destinationCountry);
        s.PortOfLoading = Fill(s.PortOfLoading, Get(row, "origin port", "port of loading", "loading port", "pol"));
        s.PortOfDischarge = Fill(
            s.PortOfDischarge,
            Get(row, "destination port", "port of discharge", "discharge port", "pod", "indian port"));
        s.SupplierName = Fill(s.SupplierName, Get(row, "seller", "supplier", "exporter", "exporter name", "shipper"));
        s.SupplierCountry = Fill(
            s.SupplierCountry,
            Get(row, "seller country", "supplier country", "exporter country"));
        s.SupplierState = Fill(s.SupplierState, Get(row, "seller state", "supplier state"));
        s.SupplierCity = Fill(s.SupplierCity, Get(row, "seller city", "supplier city"));
        s.SupplierAddress = Fill(s.SupplierAddress, Get(row, "seller address", "supplier address"));
        return changed;
    }

    private static string? GetExact(Dictionary<string, string> row, string header)
    {
        foreach (var kv in row)
        {
            if (kv.Key.Equals(header, StringComparison.OrdinalIgnoreCase) && !IsNaToken(kv.Value))
                return kv.Value.Trim();
        }
        return null;
    }
    private void RecomputeBuyers(FibcStore store)
    {
        var byBuyer = store.Shipments
            .GroupBy(s => s.BuyerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var buyer in store.Buyers)
        {
            if (!byBuyer.TryGetValue(buyer.Id, out var ships))
                ships = [];
            buyer.ShipmentCount = ships.Count;
            buyer.FirstShipment = ships.Count == 0 ? null : ships.Min(s => s.ShipmentDate);
            buyer.LastShipment = ships.Count == 0 ? null : ships.Max(s => s.ShipmentDate);
            buyer.TotalQuantity = ships.Where(s => s.Quantity.HasValue).Sum(s => s.Quantity!.Value);
            var suppliers = ships.Select(s => s.SupplierName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            buyer.SupplierCount = suppliers.Count;
            buyer.SupplierCountries = ships.Select(s => s.SupplierCountry).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToList();
            buyer.ActualImportPort = ships.Select(s => s.PortOfDischarge).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            buyer.NearestPort = null; // only when reliable geo exists — never guess
            buyer.Trend = ClassifyTrend(ships);
            buyer.ScoreBreakdown = ScoreBuyer(ships, buyer);
            buyer.Score = buyer.ScoreBreakdown.Total;
            buyer.Priority = PriorityLabel(buyer.Score);
            buyer.DataQuality = ships.Count == 0 ? "Incomplete" : "Verified Ex-Im Record";
            ApplyContactFallback(buyer);
            ApplyGenuineQualification(buyer, ships);
        }
    }

    private static void ApplyGenuineQualification(FibcBuyerDto buyer, List<FibcShipmentDto> ships)
    {
        var reasons = new List<string>();
        var hasName = !string.IsNullOrWhiteSpace(buyer.CompanyName) && !IsNaToken(buyer.CompanyName);
        var hasCountry = !string.IsNullOrWhiteSpace(buyer.Country);
        var hasFibcProduct = ships.Any(s =>
            ContainsFibc(s.ProductDescription) ||
            (s.HsCode?.StartsWith("630532", StringComparison.OrdinalIgnoreCase) ?? false));
        var hasLocation =
            !string.IsNullOrWhiteSpace(buyer.City) ||
            !string.IsNullOrWhiteSpace(buyer.Address) ||
            !string.IsNullOrWhiteSpace(buyer.ActualImportPort);
        var scoreOk = buyer.Score >= 60;

        reasons.Add(hasName ? "Company name present" : "Missing company name");
        reasons.Add(hasCountry ? "Country present" : "Missing country");
        reasons.Add(hasFibcProduct ? "FIBC / HS 630532 product match" : "No FIBC / HS 630532 match");
        reasons.Add(hasLocation ? "City, address, or port present" : "No city, address, or port");
        reasons.Add(scoreOk ? $"Score {buyer.Score} ≥ 60" : $"Score {buyer.Score} < 60");

        buyer.IsGenuine = hasName && hasCountry && hasFibcProduct && hasLocation && scoreOk;
        buyer.GenuineReasons = reasons;
    }

    /// <summary>
    /// Ex-Im trade exports often omit dedicated email/phone columns.
    /// Pull contacts only when present in address text (TEL:/PH:/email) — never invent.
    /// </summary>
    private static void ApplyContactFallback(FibcBuyerDto buyer)
    {
        var (email, phone) = ExtractContactsFromText($"{buyer.Address} {buyer.CompanyName}");
        if (string.IsNullOrWhiteSpace(buyer.Email)) buyer.Email = email;
        if (string.IsNullOrWhiteSpace(buyer.Phone)) buyer.Phone = phone;
    }

    private static (string? Email, string? Phone) ExtractContactsFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        string? email = null;
        var em = Regex.Match(text, @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase);
        if (em.Success) email = em.Value.Trim();

        string? phone = null;
        var labeled = Regex.Match(
            text,
            @"(?:TEL|TELEPHONE|PHONE|PH|MOB|MOBILE|CELL)\s*[:.\-]?\s*(\+?\d[\d\s().\-/]{6,}\d)",
            RegexOptions.IgnoreCase);
        if (labeled.Success)
        {
            phone = NormalizePhone(labeled.Groups[1].Value);
        }
        else
        {
            var us = Regex.Match(text, @"\b(\d{3}[-.\s]\d{3}[-.\s]\d{4})\b");
            if (us.Success) phone = NormalizePhone(us.Groups[1].Value);
        }

        return (email, phone);
    }

    private static string? NormalizePhone(string raw)
    {
        var cleaned = Regex.Replace(raw.Trim(), @"\s+", " ");
        var digits = Regex.Replace(cleaned, @"[^\d+]", "");
        if (digits.Length < 8) return null;
        return cleaned;
    }

    /// <summary>Re-enrich contacts on existing store (e.g. after schema update).</summary>
    public object RefreshBuyerContacts()
    {
        var store = LoadStore();
        RecomputeBuyers(store);
        SaveStore(store);
        return new
        {
            success = true,
            totalBuyers = store.Buyers.Count,
            genuineBuyers = store.Buyers.Count(b => b.IsGenuine),
            withEmail = store.Buyers.Count(b => !string.IsNullOrWhiteSpace(b.Email)),
            withPhone = store.Buyers.Count(b => !string.IsNullOrWhiteSpace(b.Phone)),
        };
    }

    /// <summary>Recompute scores, contacts, and genuine flags for the current store.</summary>
    public object RecomputeAll()
    {
        var store = LoadStore();
        RecomputeBuyers(store);
        SaveStore(store);
        return new
        {
            success = true,
            totalBuyers = store.Buyers.Count,
            totalShipments = store.Shipments.Count,
            genuineBuyers = store.Buyers.Count(b => b.IsGenuine),
            unverifiedBuyers = store.Buyers.Count(b => !b.IsGenuine),
        };
    }

    private static FibcScoreBreakdownDto ScoreBuyer(List<FibcShipmentDto> ships, FibcBuyerDto buyer)
    {
        var now = DateTime.UtcNow;
        var last = buyer.LastShipment;
        var recency = last == null ? 0
            : (now - last.Value).TotalDays <= 30 ? 20
            : (now - last.Value).TotalDays <= 90 ? 16
            : (now - last.Value).TotalDays <= 180 ? 12
            : (now - last.Value).TotalDays <= 365 ? 8
            : 4;

        var freq = ships.Count >= 12 ? 20 : ships.Count >= 6 ? 16 : ships.Count >= 3 ? 12 : ships.Count >= 1 ? 8 : 0;
        var volume = buyer.TotalQuantity >= 1000 ? 20 : buyer.TotalQuantity >= 200 ? 15 : buyer.TotalQuantity > 0 ? 10 : 5;
        var trendPts = buyer.Trend switch
        {
            "Increasing" => 15,
            "Stable" => 10,
            "Decreasing" => 5,
            _ => 0,
        };
        var supplierPts = Math.Min(10, buyer.SupplierCount * 3);
        var relevance = ships.Count(s =>
            ContainsFibc(s.ProductDescription) ||
            (s.HsCode?.StartsWith("630532", StringComparison.OrdinalIgnoreCase) ?? false)) > 0 ? 10 : 4;
        var confidence = ships.Count(s => !string.IsNullOrWhiteSpace(s.HsCode) && s.ShipmentDate.HasValue) >= Math.Max(1, ships.Count / 2) ? 5 : 2;

        var b = new FibcScoreBreakdownDto
        {
            Recency = recency,
            Frequency = freq,
            Volume = volume,
            Trend = trendPts,
            SupplierActivity = supplierPts,
            ProductRelevance = relevance,
            DataConfidence = confidence,
        };
        b.Total = b.Recency + b.Frequency + b.Volume + b.Trend + b.SupplierActivity + b.ProductRelevance + b.DataConfidence;
        b.Explanation =
            $"Score {b.Total}/100 from Ex-Im metrics: {ships.Count} shipment(s), last={(last?.ToString("yyyy-MM-dd") ?? "n/a")}, suppliers={buyer.SupplierCount}, trend={buyer.Trend}.";
        return b;
    }

    private static string ClassifyTrend(List<FibcShipmentDto> ships)
    {
        var dated = ships.Where(s => s.ShipmentDate.HasValue).OrderBy(s => s.ShipmentDate).ToList();
        if (dated.Count < 3) return "Insufficient Data";
        var mid = dated.Count / 2;
        var first = dated.Take(mid).Count();
        var second = dated.Skip(mid).Count();
        if (second > first * 1.2) return "Increasing";
        if (first > second * 1.2) return "Decreasing";
        return "Stable";
    }

    private static bool ContainsFibc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.ToLowerInvariant();
        return t.Contains("fibc")
            || t.Contains("jumbo bag")
            || t.Contains("bulk bag")
            || t.Contains("super sack")
            || t.Contains("big bag")
            || t.Contains("flexible intermediate");
    }

    private static string PriorityLabel(int score) =>
        score >= 90 ? "HOT" :
        score >= 75 ? "HIGH POTENTIAL" :
        score >= 60 ? "POTENTIAL" :
        score >= 40 ? "LOW PRIORITY" : "LOW";

    private static string BuildSummary(FibcBuyerDto buyer, List<FibcShipmentDto> ships, List<FibcSupplierSummaryDto> suppliers)
    {
        if (ships.Count == 0)
            return "No Ex-Im shipments are stored for this buyer yet.";
        var last = buyer.LastShipment?.ToString("MMMM yyyy") ?? "an unknown month";
        return $"The company recorded {ships.Count} relevant shipment(s) in stored Ex-Im data. Its latest recorded shipment was in {last} and it sourced from {suppliers.Count} supplier(s).";
    }

    private IEnumerable<FibcBuyerDto> FilterBuyers(List<FibcBuyerDto> buyers, FibcBuyerQuery q)
    {
        IEnumerable<FibcBuyerDto> qy = buyers;
        if (q.GenuineOnly == true)
            qy = qy.Where(b => b.IsGenuine);
        if (!string.IsNullOrWhiteSpace(q.Country) && !q.Country.Equals("All", StringComparison.OrdinalIgnoreCase))
            qy = qy.Where(b => string.Equals(b.Country, q.Country, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(q.Buyer))
            qy = qy.Where(b => (b.CompanyName ?? "").Contains(q.Buyer, StringComparison.OrdinalIgnoreCase));
        if (q.MinScore is > 0)
            qy = qy.Where(b => b.Score >= q.MinScore.Value);
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var store = LoadStore();
            var tokens = q.Keyword
                .Split(['/', ',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tokens.Count == 0)
                tokens.Add(q.Keyword.Trim());

            // Expand common FIBC label used in the UI filter dropdown.
            if (tokens.Any(t => t.Contains("FIBC", StringComparison.OrdinalIgnoreCase) ||
                                t.Contains("Jumbo", StringComparison.OrdinalIgnoreCase)))
            {
                tokens.AddRange(["FIBC", "Jumbo", "Bulk Bag", "Bulk Bags", "Flexible Intermediate"]);
            }

            var ids = store.Shipments
                .Where(s =>
                {
                    var desc = s.ProductDescription ?? "";
                    var hs = s.HsCode ?? "";
                    return tokens.Any(t =>
                        desc.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        hs.Contains(t, StringComparison.OrdinalIgnoreCase));
                })
                .Select(s => s.BuyerId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            qy = qy.Where(b => ids.Contains(b.Id));
        }
        return qy;
    }

    private FibcStore LoadStore()
    {
        lock (_gate)
        {
            if (!File.Exists(_storePath))
                return new FibcStore();
            try
            {
                var json = File.ReadAllText(_storePath);
                var store = JsonSerializer.Deserialize<FibcStore>(json, JsonOpts) ?? new FibcStore();
                store.ImportHistory ??= [];
                return store;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to read FIBC buyers store; starting empty.");
                return new FibcStore();
            }
        }
    }

    /// <summary>
    /// Seeds ImportHistory from RawRecords / LastSync / current store when the feature is first enabled.
    /// </summary>
    private static bool EnsureImportHistoryBackfill(FibcStore store)
    {
        store.ImportHistory ??= [];
        if (store.ImportHistory.Count > 0)
            return false;

        var fromRaw = store.RawRecords
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.SourceFile) ||
                string.Equals(r.SearchTerm, "import-upload", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => NormalizeSourceFileKey(r.SourceFile))
            .Select(g => g.OrderByDescending(r => r.ExtractedAt).First())
            .OrderByDescending(r => r.ExtractedAt)
            .Take(50)
            .Select(r => new FibcSyncHistoryDto
            {
                Id = string.IsNullOrWhiteSpace(r.Id) ? Guid.NewGuid().ToString("N") : r.Id,
                At = r.ExtractedAt,
                Mode = "import",
                SourceFile = r.SourceFile ?? "Imported file",
                Message = r.Preview,
                TotalBuyersAfter = store.Buyers.Count,
                TotalShipmentsAfter = store.Shipments.Count,
                GenuineBuyersAfter = store.Buyers.Count(b => b.IsGenuine),
            })
            .ToList();

        if (fromRaw.Count > 0)
        {
            store.ImportHistory = fromRaw;
            return true;
        }

        if (store.LastSync != null)
        {
            if (string.IsNullOrWhiteSpace(store.LastSync.Id))
                store.LastSync.Id = Guid.NewGuid().ToString("N");
            store.ImportHistory.Add(store.LastSync);
            return true;
        }

        if (store.Shipments.Count == 0 && store.Buyers.Count == 0)
            return false;

        store.ImportHistory.Add(new FibcSyncHistoryDto
        {
            Id = "legacy-current",
            At = store.Shipments
                    .Select(s => s.ShipmentDate)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .DefaultIfEmpty(DateTime.UtcNow)
                    .Max(),
            Mode = "import",
            SourceFile = "Previous import",
            TotalBuyersAfter = store.Buyers.Count,
            TotalShipmentsAfter = store.Shipments.Count,
            GenuineBuyersAfter = store.Buyers.Count(b => b.IsGenuine),
            Message = "Data already in store before import history was enabled.",
        });
        return true;
    }

    private static string NormalizeSourceFileKey(string? sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile)) return "";
        return Path.GetFileName(sourceFile.Trim()).ToLowerInvariant();
    }

    /// <summary>One history row per source file — keep the strongest / latest import.</summary>
    private static void UpsertImportHistory(FibcStore store, FibcSyncHistoryDto history)
    {
        store.ImportHistory ??= [];
        var key = NormalizeSourceFileKey(history.SourceFile);
        if (!string.IsNullOrEmpty(key))
        {
            var existing = store.ImportHistory.FirstOrDefault(h =>
                NormalizeSourceFileKey(h.SourceFile) == key);
            if (existing != null)
            {
                // Keep the batch that actually loaded data when this run added nothing.
                if (history.NewShipments <= 0 && existing.NewShipments > 0)
                {
                    history.Id = existing.Id;
                    history.NewShipments = existing.NewShipments;
                    history.NewRecords = Math.Max(history.NewRecords, existing.NewRecords);
                    history.RecordsProcessed = Math.Max(history.RecordsProcessed, existing.RecordsProcessed);
                    if (string.IsNullOrWhiteSpace(history.Message) ||
                        history.Message.Contains("No new shipments", StringComparison.OrdinalIgnoreCase))
                    {
                        history.Message = existing.Message ?? history.Message;
                    }
                }
                store.ImportHistory.Remove(existing);
            }
        }

        store.ImportHistory.Insert(0, history);
        if (store.ImportHistory.Count > 100)
            store.ImportHistory = store.ImportHistory.Take(100).ToList();
    }

    private static bool DeduplicateImportHistory(FibcStore store)
    {
        store.ImportHistory ??= [];
        if (store.ImportHistory.Count <= 1)
            return false;

        var before = store.ImportHistory.Count;
        store.ImportHistory = store.ImportHistory
            .Select((h, i) => (h, i))
            .GroupBy(x =>
            {
                var key = NormalizeSourceFileKey(x.h.SourceFile);
                return string.IsNullOrEmpty(key) ? $"__id:{x.h.Id}" : key;
            })
            .Select(g => g
                .OrderByDescending(x => EstimateImportShipments(x.h))
                .ThenByDescending(x => x.h.At)
                .ThenBy(x => x.i)
                .First().h)
            .OrderByDescending(h => h.At)
            .ToList();

        return store.ImportHistory.Count != before;
    }

    private static int EstimateImportShipments(FibcSyncHistoryDto h)
    {
        if (h.NewShipments > 0) return h.NewShipments;
        if (string.IsNullOrWhiteSpace(h.Message)) return 0;
        var m = Regex.Match(h.Message, @"Imported\s+(\d+)\s+shipment", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n)) return n;
        return 0;
    }

    private void SaveStore(FibcStore store)
    {
        lock (_gate)
        {
            var json = JsonSerializer.Serialize(store, JsonOpts);
            File.WriteAllText(_storePath, json);
        }
    }

    private string GetEximLoginUrl() =>
        _config[$"{FibcBuyersOptions.SectionName}:EximLoginUrl"]
        ?? "https://www.ex-im.cloud/login";

    private string GetEximMarketingUrl() =>
        _config[$"{FibcBuyersOptions.SectionName}:EximMarketingUrl"]
        ?? "https://www.ex-im.com/";

    private string GetEximUsername() =>
        Environment.GetEnvironmentVariable("EXIM_USERNAME")
        ?? _config["EXIM_USERNAME"]
        ?? _config["FibcBuyers:EximUsername"]
        ?? "";

    private string GetEximPassword() =>
        Environment.GetEnvironmentVariable("EXIM_PASSWORD")
        ?? _config["EXIM_PASSWORD"]
        ?? _config["FibcBuyers:EximPassword"]
        ?? "";

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "";
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return email[0] + "***" + email[at..];
    }

    private static string NormalizeCompany(string name)
    {
        var s = name.Trim().ToUpperInvariant();
        s = Regex.Replace(s, @"[^A-Z0-9\s]", " ");
        s = Regex.Replace(s, @"\b(LIMITED|LTD|LLC|INC|PVT|PRIVATE|CO|COMPANY)\b", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static string BuyerKey(string normalizedName, string? country) =>
        $"{normalizedName}|{(country ?? "").Trim().ToUpperInvariant()}";

    private static bool IsNaToken(string? s) =>
        string.IsNullOrWhiteSpace(s) ||
        s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
        s.Trim().Equals("NA", StringComparison.OrdinalIgnoreCase) ||
        s.Trim().Equals("\\N", StringComparison.OrdinalIgnoreCase) ||
        s.Trim() == "-";

    private static string? NullIfEmpty(string? s)
    {
        if (IsNaToken(s)) return null;
        return s!.Trim();
    }

    private static string Get(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in row)
            {
                if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && !IsNaToken(kv.Value))
                    return kv.Value.Trim();
            }
        }

        // Soft match: VALUE(USD) ↔ value(usd) / value usd
        static string Norm(string x) => Regex.Replace(x.ToLowerInvariant(), @"[^a-z0-9]", "");
        foreach (var key in keys)
        {
            var nk = Norm(key);
            foreach (var kv in row)
            {
                if (Norm(kv.Key) == nk && !IsNaToken(kv.Value))
                    return kv.Value.Trim();
            }
        }
        return "";
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
            return d.Date;
        if (DateTime.TryParse(s, out d)) return d.Date;
        // Ex-Im Excel serial dates (e.g. 46262)
        if (double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var oa)
            && oa is >= 20000 and <= 80000)
        {
            try { return DateTime.FromOADate(oa).Date; }
            catch { /* ignore */ }
        }
        return null;
    }

    private static double? ParseDouble(string? s)
    {
        if (string.IsNullOrWhiteSpace(s) || IsNaToken(s)) return null;
        var cleaned = Regex.Replace(s, @"[^\d\.\-]", "");
        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string Sha256(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    private static string Csv(string? s)
    {
        s ??= "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    /// <summary>
    /// Reads Ex-Im Trade Analysis .xlsx without relying on styles (their exports often have invalid stylesheet XML).
    /// Skips title rows and detects the header row containing DATE / BUYER / HS CODE.
    /// </summary>
    private static List<Dictionary<string, string>> ParseExImXlsx(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var ss = ReadSharedStrings(zip);
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml")
            ?? zip.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                                               && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Excel worksheet not found in the uploaded file.");

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheetRows = sheetDoc.Root?.Element(ns + "sheetData")?.Elements(ns + "row") ?? Enumerable.Empty<XElement>();

        var matrix = new List<List<string>>();
        foreach (var rowEl in sheetRows)
        {
            var cells = new Dictionary<int, string>();
            var max = -1;
            foreach (var c in rowEl.Elements(ns + "c"))
            {
                var refAttr = (string?)c.Attribute("r") ?? "";
                var idx = ColumnIndex(refAttr);
                max = Math.Max(max, idx);
                var t = (string?)c.Attribute("t");
                string val;
                var isEl = c.Element(ns + "is");
                if (isEl != null)
                {
                    val = string.Concat(isEl.Descendants(ns + "t").Select(x => x.Value));
                }
                else
                {
                    val = c.Element(ns + "v")?.Value ?? "";
                    if (t == "s" && int.TryParse(val, out var sIdx) && sIdx >= 0 && sIdx < ss.Count)
                        val = ss[sIdx];
                }
                cells[idx] = val;
            }
            var line = new List<string>();
            for (var i = 0; i <= max; i++)
                line.Add(cells.TryGetValue(i, out var v) ? v : "");
            matrix.Add(line);
        }

        var headerIdx = matrix.FindIndex(r =>
            r.Any(c => c.Equals("DATE", StringComparison.OrdinalIgnoreCase)) &&
            r.Any(c => c.Equals("BUYER", StringComparison.OrdinalIgnoreCase)));
        if (headerIdx < 0)
            headerIdx = matrix.FindIndex(r => r.Count(c => !string.IsNullOrWhiteSpace(c)) >= 5);
        if (headerIdx < 0 || headerIdx >= matrix.Count - 1)
            return [];

        var headers = matrix[headerIdx].Select(h => h.Trim()).ToList();
        var rows = new List<Dictionary<string, string>>();
        for (var i = headerIdx + 1; i < matrix.Count; i++)
        {
            var cols = matrix[i];
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                if (string.IsNullOrWhiteSpace(headers[c])) continue;
                row[headers[c]] = c < cols.Count ? cols[c].Trim() : "";
            }
            if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                rows.Add(row);
        }
        return rows;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return [];
        using var s = entry.Open();
        var doc = XDocument.Load(s);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var list = new List<string>();
        foreach (var si in doc.Root?.Elements(ns + "si") ?? Enumerable.Empty<XElement>())
        {
            var text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
            list.Add(text);
        }
        return list;
    }

    private static int ColumnIndex(string cellRef)
    {
        var n = 0;
        foreach (var ch in cellRef)
        {
            if (ch is < 'A' or > 'Z') break;
            n = n * 26 + (ch - 'A' + 1);
        }
        return Math.Max(0, n - 1);
    }

    private static List<Dictionary<string, string>> ParseCsv(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return [];
        var headers = SplitCsvLine(lines[0]).Select(h => h.Trim().Trim('"')).ToList();
        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                var val = c < cols.Count ? cols[c].Trim().Trim('"') : "";
                if (!string.IsNullOrWhiteSpace(headers[c]))
                    row[headers[c]] = val;
            }
            if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                rows.Add(row);
        }
        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(ch);
        }
        list.Add(sb.ToString());
        return list;
    }
}

public sealed class FibcStore
{
    public List<FibcBuyerDto> Buyers { get; set; } = [];
    public List<FibcShipmentDto> Shipments { get; set; } = [];
    public List<FibcRawRecordDto> RawRecords { get; set; } = [];
    public List<FibcSyncHistoryDto> ImportHistory { get; set; } = [];
    public List<string> ConfiguredHsCodes { get; set; } = ["630532", "63053200"];
    public FibcSyncHistoryDto? LastSync { get; set; }
}

public sealed class FibcBuyerQuery
{
    public string? Country { get; set; }
    public string? Buyer { get; set; }
    public string? Keyword { get; set; }
    public int? MinScore { get; set; }
    public bool? GenuineOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class FibcSyncRequest
{
    public int Limit { get; set; } = 20;
    public List<string>? Keywords { get; set; }
    public List<string>? HsCodes { get; set; }
    public string? Country { get; set; }
    public string? DateRange { get; set; }
    public string? TradeDirection { get; set; }
}

public sealed class FibcDashboardDto
{
    public FibcKpisDto Kpis { get; set; } = new();
    public List<FibcNamedCountDto> TopCountries { get; set; } = [];
    public List<FibcNamedCountDto> ScoreDistribution { get; set; } = [];
    public List<FibcBuyerDto> HighPotential { get; set; } = [];
    public List<FibcNamedValueDto> TopBuyerCountriesByValue { get; set; } = [];
    public List<FibcNamedValueDto> TopSellerCountriesByValue { get; set; } = [];
    public List<FibcNamedValueDto> TopBuyersByValue { get; set; } = [];
    public List<FibcNamedValueDto> TopSellersByValue { get; set; } = [];
    public FibcSyncHistoryDto? LastSync { get; set; }
    public List<string> Keywords { get; set; } = [];
    public List<string> HsCodes { get; set; } = [];
    public string SourceNote { get; set; } = "";
}

public sealed class FibcNamedValueDto
{
    public string Name { get; set; } = "";
    public double ValueUsd { get; set; }
    public string ValueLabel { get; set; } = "";
    public int ShipmentCount { get; set; }
}

public sealed class FibcKpisDto
{
    public int TotalBuyers { get; set; }
    public int TotalShipments { get; set; }
    public int QualifiedBuyers { get; set; }
    public int GenuineBuyers { get; set; }
    public int HotBuyers { get; set; }
    public int Countries { get; set; }
    public int Suppliers { get; set; }
}

public sealed class FibcNamedCountDto
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public sealed class FibcBuyerDto
{
    public string Id { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsGenuine { get; set; }
    public List<string> GenuineReasons { get; set; } = [];
    public int ShipmentCount { get; set; }
    public DateTime? FirstShipment { get; set; }
    public DateTime? LastShipment { get; set; }
    public double TotalQuantity { get; set; }
    public int SupplierCount { get; set; }
    public List<string> SupplierCountries { get; set; } = [];
    public string? ActualImportPort { get; set; }
    public string? NearestPort { get; set; }
    public int Score { get; set; }
    public FibcScoreBreakdownDto ScoreBreakdown { get; set; } = new();
    public string Trend { get; set; } = "Insufficient Data";
    public string Priority { get; set; } = "LOW";
    public string DataQuality { get; set; } = "Incomplete";
    public string Source { get; set; } = "Ex-Im";
}

public sealed class FibcScoreBreakdownDto
{
    public int Recency { get; set; }
    public int Frequency { get; set; }
    public int Volume { get; set; }
    public int Trend { get; set; }
    public int SupplierActivity { get; set; }
    public int ProductRelevance { get; set; }
    public int DataConfidence { get; set; }
    public int Total { get; set; }
    public string Explanation { get; set; } = "";
}

public sealed class FibcShipmentDto
{
    public string Id { get; set; } = "";
    public string BuyerId { get; set; } = "";
    public string RawRecordId { get; set; } = "";
    public DateTime? ShipmentDate { get; set; }
    public string? Industry { get; set; }
    public string? ProductDescription { get; set; }
    public string? HsCode { get; set; }
    public double? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public double? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public double? Value { get; set; }
    public double? UnitPrice { get; set; }
    public string? Currency { get; set; }
    public string? TradeDirection { get; set; }
    public string? OriginCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCountry { get; set; }
    public string? SupplierState { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierAddress { get; set; }
    public string? ShipmentReference { get; set; }
    public string? ImportBatchId { get; set; }
    public string Source { get; set; } = "Ex-Im";
}

public sealed class FibcRawRecordDto
{
    public string Id { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTime ExtractedAt { get; set; }
    public string? SearchTerm { get; set; }
    public string? HsCodeFilter { get; set; }
    public string? SourceFile { get; set; }
    public string Source { get; set; } = "Ex-Im";
    public string? RawFile { get; set; }
    public string? Preview { get; set; }
}

public sealed class FibcSyncHistoryDto
{
    public string Id { get; set; } = "";
    public DateTime At { get; set; }
    public string Mode { get; set; } = "";
    public string? SourceFile { get; set; }
    public int RecordsProcessed { get; set; }
    public int NewRecords { get; set; }
    public int NewShipments { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public int TotalBuyersAfter { get; set; }
    public int TotalShipmentsAfter { get; set; }
    public int GenuineBuyersAfter { get; set; }
    public string? Message { get; set; }
}

public sealed class FibcSupplierSummaryDto
{
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public int ShipmentCount { get; set; }
    public DateTime? FirstShipment { get; set; }
    public DateTime? LatestShipment { get; set; }
}

public sealed class FibcBuyerDetailDto
{
    public FibcBuyerDto Buyer { get; set; } = new();
    public List<FibcShipmentDto> Shipments { get; set; } = [];
    public List<FibcSupplierSummaryDto> Suppliers { get; set; } = [];
    public List<FibcRawRecordDto> RawRecords { get; set; } = [];
    public string AiSummary { get; set; } = "";
}
