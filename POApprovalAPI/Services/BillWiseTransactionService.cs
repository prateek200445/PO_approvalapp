using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill-wise recon against vw_BillWiseTransactionWithOnAccount.
/// Dropdown lists are cached; data fetches use equality filters and a narrow column set.
/// </summary>
public class BillWiseTransactionService
{
    private static readonly TimeSpan CompaniesCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LedgersCacheTtl = TimeSpan.FromMinutes(15);
    private const string CompaniesCacheKey = "billwise:companies";

    private static readonly HashSet<string> NoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "limited", "ltd", "pvt", "private", "llp", "llc", "inc", "incorporated",
        "company", "co", "corp", "corporation", "the", "and", "of",
        "ho", "head", "office", "headoffice",
        "unit", "uniti", "unitii", "unitiii", "branch",
        "purchase", "sales", "trading", "manufacturing",
    };

    private readonly DatabaseService _database;
    private readonly ExcelLedgerService _excel;
    private readonly IMemoryCache _cache;

    public BillWiseTransactionService(
        DatabaseService database,
        ExcelLedgerService excel,
        IMemoryCache cache)
    {
        _database = database;
        _excel = excel;
        _cache = cache;
    }

    /// <summary>
    /// Fast company typeahead — searches FactoryInfo (not the heavy bill-wise view).
    /// Requires q length &gt;= 1; returns at most <paramref name="take"/> rows.
    /// </summary>
    public async Task<IReadOnlyList<string>> SearchCompaniesAsync(string? q, int take = 40)
    {
        take = Math.Clamp(take, 5, 80);
        var query = (q ?? "").Trim();
        if (query.Length == 0)
            return Array.Empty<string>();

        var cacheKey = $"billwise:co-search:{query.ToLowerInvariant()}:{take}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        using var connection = _database.CreateConnection();
        // FactoryInfo is much cheaper than DISTINCT on the bill-wise view.
        var rows = (await connection.QueryAsync<string>(@"
SELECT TOP (@Take) fi.Name
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, '') <> ''
  AND fi.Name LIKE '%' + @Q + '%'
ORDER BY
  CASE WHEN fi.Name LIKE @Q + '%' THEN 0 ELSE 1 END,
  fi.Name",
            new { Q = query, Take = take },
            commandTimeout: 30)).ToList();

        var list = rows
            .Select(r => (r ?? "").Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)list, TimeSpan.FromMinutes(10));
        return list;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        // Kept for compatibility — prefer SearchCompaniesAsync for UI.
        if (_cache.TryGetValue(CompaniesCacheKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<string>(@"
SELECT fi.Name
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, '') <> ''
ORDER BY fi.Name",
            commandTimeout: 60)).ToList();

        var list = rows
            .Select(r => (r ?? "").Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(CompaniesCacheKey, (IReadOnlyList<string>)list, CompaniesCacheTtl);
        return list;
    }

    public async Task<IReadOnlyList<string>> GetLedgersAsync(string companyName)
    {
        var company = (companyName ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");

        var cacheKey = $"billwise:ledgers:{company.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<string>(@"
SELECT DISTINCT LedgerName
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND ISNULL(LedgerName, '') <> ''
ORDER BY LedgerName",
            new { CompanyName = company },
            commandTimeout: 90)).ToList();

        var list = rows
            .Select(r => (r ?? "").Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)list, LedgersCacheTtl);
        return list;
    }

    /// <summary>
    /// Ledger typeahead for one company. Searches the ledger master, same idea as company search.
    /// </summary>
    public async Task<IReadOnlyList<string>> SearchLedgersAsync(string companyName, string? q, int take = 40)
    {
        var company = (companyName ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");

        take = Math.Clamp(take, 5, 80);
        var query = (q ?? "").Trim();
        if (query.Length == 0)
            return Array.Empty<string>();

        var cacheKey = $"billwise:ledger-search:{company.ToLowerInvariant()}:{query.ToLowerInvariant()}:{take}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<string>(@"
SELECT TOP (@Take) l.LedgerName
FROM ledgermaster l WITH (NOLOCK)
WHERE LTRIM(RTRIM(l.CompanyName)) = @Company
  AND ISNULL(l.LedgerName, '') <> ''
  AND l.LedgerName LIKE '%' + @Q + '%'
ORDER BY
  CASE WHEN l.LedgerName LIKE @Q + '%' THEN 0 ELSE 1 END,
  l.LedgerName",
            new { Company = company, Q = query, Take = take },
            commandTimeout: 30)).ToList();

        var list = rows
            .Select(r => (r ?? "").Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<string>)list, TimeSpan.FromMinutes(10));
        return list;
    }

    /// <summary>
    /// Two companies → two swapped LIKE queries (same pattern as manual SSMS checks):
    ///   CompanyName LIKE %CoreA% AND LedgerName LIKE %CoreB%
    ///   CompanyName LIKE %CoreB% AND LedgerName LIKE %CoreA%
    /// Place tokens from parentheses (e.g. Vadodara) apply to BOTH CompanyName and LedgerName
    /// on the side that owns that place.
    /// </summary>
    public async Task<ComparisonResultDto> CompareFromCompaniesAsync(
        string companyA,
        string companyB,
        LedgerMatchOptions? options = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateRangeField = null)
    {
        companyA = (companyA ?? "").Trim();
        companyB = (companyB ?? "").Trim();
        if (companyA.Length == 0 || companyB.Length == 0)
            throw new InvalidOperationException("Select both companies.");
        if (string.Equals(companyA, companyB, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Pick two different companies.");

        var coreA = BuildCompanyCore(companyA);
        var coreB = BuildCompanyCore(companyB);
        var placesA = ExtractPlaceTokens(companyA);
        var placesB = ExtractPlaceTokens(companyB);

        if (string.IsNullOrWhiteSpace(coreA) || string.IsNullOrWhiteSpace(coreB))
            throw new InvalidOperationException("Could not derive search cores from the selected companies.");

        if (string.Equals(NormalizeLoose(coreA), NormalizeLoose(coreB), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Company cores look the same ('{coreA}'). Pick clearer company names.");

        var queryA = FormatSwappedQuery(coreA, placesA, coreB, placesB);
        var queryB = FormatSwappedQuery(coreB, placesB, coreA, placesA);

        Console.WriteLine("========== Bill-wise compare ==========");
        Console.WriteLine($"Selected A: {companyA}");
        Console.WriteLine($"Selected B: {companyB}");
        Console.WriteLine($"Core A: {coreA}  |  places: {(placesA.Count == 0 ? "(none)" : string.Join(", ", placesA))}");
        Console.WriteLine($"Core B: {coreB}  |  places: {(placesB.Count == 0 ? "(none)" : string.Join(", ", placesB))}");
        Console.WriteLine($"Query A: {queryA}");
        Console.WriteLine($"Query B: {queryB}");
        Console.WriteLine("=======================================");

        // Side A books: company~A (+placesA), ledger~B (+placesB)
        var fetchA = FetchBySwappedLikeAsync(coreA, placesA, coreB, placesB, dateFrom, dateTo, dateRangeField);
        // Side B books: company~B (+placesB), ledger~A (+placesA)
        var fetchB = FetchBySwappedLikeAsync(coreB, placesB, coreA, placesA, dateFrom, dateTo, dateRangeField);
        await Task.WhenAll(fetchA, fetchB);
        var entriesA = await fetchA;
        var entriesB = await fetchB;

        Console.WriteLine($"Rows A: {entriesA.Count}  |  Rows B: {entriesB.Count}");

        var sideA = await SupplementSideAsync(companyA, null, companyB, entriesA, dateFrom, dateTo);
        var sideB = await SupplementSideAsync(companyB, null, companyA, entriesB, dateFrom, dateTo);
        entriesA = sideA.Entries;
        entriesB = sideB.Entries;

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows for '{coreA}' ↔ '{coreB}'. " +
                "Check company spelling against ERP CompanyName / LedgerName.");
        }

        return FinishCompare(entriesA, entriesB, companyA, companyB, options, sideA.Closing, sideB.Closing);
    }

    private static string FormatSwappedQuery(
        string companyCore,
        IReadOnlyList<string> companyPlaces,
        string ledgerCore,
        IReadOnlyList<string> ledgerPlaces)
    {
        var parts = new List<string>
        {
            $"CompanyName LIKE '%{ToLikeFragment(companyCore)}%'",
        };
        foreach (var p in companyPlaces)
            parts.Add($"CompanyName LIKE '%{p}%'");
        parts.Add($"LedgerName LIKE '%{ToLikeFragment(ledgerCore)}%'");
        foreach (var p in ledgerPlaces)
            parts.Add($"LedgerName LIKE '%{p}%'");
        return string.Join(" AND ", parts);
    }

    /// <summary>
    /// CompanyName LIKE %companyCore% [+ company places] AND LedgerName LIKE %ledgerCore% [+ ledger places].
    /// </summary>
    private async Task<List<LedgerEntryDto>> FetchBySwappedLikeAsync(
        string companyCore,
        IReadOnlyList<string> companyPlaces,
        string ledgerCore,
        IReadOnlyList<string> ledgerPlaces,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateRangeField = null)
    {
        var companyFrag = ToLikeFragment(companyCore);
        var ledgerFrag = ToLikeFragment(ledgerCore);
        if (string.IsNullOrWhiteSpace(companyFrag) || string.IsNullOrWhiteSpace(ledgerFrag))
            return new List<LedgerEntryDto>();

        using var connection = _database.CreateConnection();

        var sql = @"
SELECT
    CompanyName,
    LedgerName,
    VoucherNo,
    VoucherDate,
    BillNo,
    BillDate,
    Amount,
    RefType
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName LIKE @CompanyPattern
  AND LedgerName LIKE @LedgerPattern";

        var parameters = new DynamicParameters();
        parameters.Add("CompanyPattern", "%" + companyFrag + "%");
        parameters.Add("LedgerPattern", "%" + ledgerFrag + "%");

        var i = 0;
        foreach (var place in companyPlaces.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var name = $"CoPlace{i++}";
            sql += $"\n  AND CompanyName LIKE @{name}";
            parameters.Add(name, "%" + place.Trim() + "%");
        }

        i = 0;
        foreach (var place in ledgerPlaces.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var name = $"LedPlace{i++}";
            sql += $"\n  AND LedgerName LIKE @{name}";
            parameters.Add(name, "%" + place.Trim() + "%");
        }

        AppendDateRangeFilter(ref sql, parameters, dateFrom, dateTo, dateRangeField);

        var rows = (await connection.QueryAsync(sql, parameters, commandTimeout: 180)).ToList();
        var entries = new List<LedgerEntryDto>(rows.Count);
        var rowIndex = 1;
        foreach (var row in rows)
        {
            var entry = MapRow(row, rowIndex);
            if (entry == null) continue;
            entries.Add(entry);
            rowIndex++;
        }
        return entries;
    }

    /// <summary>
    /// Brand core for LIKE:
    /// "HCP ENTERPRISE LIMITED" → "HCP Enterprise"
    /// "K.P. WOVEN PRIVATE LIMITED" → "K.P Woven" (initials kept)
    /// "Plastene India Limited" → "Plastene India"
    /// </summary>
    private static string BuildCompanyCore(string rawName)
    {
        var withoutParens = Regex.Replace(rawName ?? "", @"\([^)]*\)", " ");

        // Preserve dotted initials as one token BEFORE stripping dots: "K.P." → "K.P"
        string? initials = null;
        var initMatch = Regex.Match(withoutParens, @"\b((?:[A-Za-z]\.){1,}[A-Za-z]?)\.?");
        if (initMatch.Success)
        {
            initials = initMatch.Groups[1].Value.TrimEnd('.');
            if (initials.Length >= 3) // e.g. K.P or H.C.P
            {
                withoutParens =
                    withoutParens[..initMatch.Index]
                    + " "
                    + withoutParens[(initMatch.Index + initMatch.Length)..];
            }
            else
            {
                initials = null;
            }
        }

        var rest = CoreWordsFromPlainText(withoutParens);
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(initials))
        {
            parts.Add(initials);
            // Initials + next brand word: "K.P" + "Woven"
            if (rest.Count > 0)
                parts.Add(rest[0]);
        }
        else
        {
            parts.AddRange(rest.Take(2));
        }

        if (parts.Count == 0)
            return (rawName ?? "").Trim();
        return string.Join(" ", parts);
    }

    private static List<string> CoreWordsFromPlainText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        text = Regex.Replace(text, @"[_\./\\|,;:+]+", " ");
        text = text.Replace('-', ' ');

        return Regex.Split(text, @"\s+")
            .Select(w => w.Trim())
            .Where(w => w.Length >= 2)
            .Where(w => !NoiseTokens.Contains(w))
            .Where(w => !Regex.IsMatch(w, @"^(i{1,3}|iv|v|vi{0,3}|ix|x|\d+)$", RegexOptions.IgnoreCase))
            .ToList();
    }

    private static List<string> CoreWords(string rawName)
    {
        // Kept for BuildSearchTokens / preferred token helpers.
        var withoutParens = Regex.Replace(rawName ?? "", @"\([^)]*\)", " ");
        return CoreWordsFromPlainText(withoutParens);
    }

    /// <summary>Turn "HCP Enterprise" / "K.P Woven" into LIKE fragment with flexible gaps.</summary>
    private static string ToLikeFragment(string core)
    {
        var parts = (core ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .ToArray();
        return parts.Length == 0 ? "" : string.Join("%", parts);
    }

    /// <summary>
    /// Exact company/ledger pairs from dropdowns. Fetches both sides in parallel.
    /// </summary>
    public async Task<ComparisonResultDto> CompareFromSelectionAsync(
        string companyA,
        string ledgerA,
        string companyB,
        string ledgerB,
        LedgerMatchOptions? options = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateRangeField = null)
    {
        companyA = (companyA ?? "").Trim();
        ledgerA = (ledgerA ?? "").Trim();
        companyB = (companyB ?? "").Trim();
        ledgerB = (ledgerB ?? "").Trim();

        // If ledgers omitted, auto-resolve from the two companies.
        if (ledgerA.Length == 0 || ledgerB.Length == 0)
            return await CompareFromCompaniesAsync(companyA, companyB, options, dateFrom, dateTo, dateRangeField);

        if (companyA.Length == 0 || companyB.Length == 0)
            throw new InvalidOperationException("Select Company and Ledger for both sides.");

        var fetchA = FetchBillWiseExactAsync(companyA, ledgerA, dateFrom, dateTo, dateRangeField);
        var fetchB = FetchBillWiseExactAsync(companyB, ledgerB, dateFrom, dateTo, dateRangeField);
        await Task.WhenAll(fetchA, fetchB);

        var entriesA = await fetchA;
        var entriesB = await fetchB;

        var sideA = await SupplementSideAsync(companyA, ledgerA, companyB, entriesA, dateFrom, dateTo);
        var sideB = await SupplementSideAsync(companyB, ledgerB, companyA, entriesB, dateFrom, dateTo);
        entriesA = sideA.Entries;
        entriesB = sideB.Entries;

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows for '{companyA}' / '{ledgerA}' or '{companyB}' / '{ledgerB}'.");
        }

        var result = FinishCompare(entriesA, entriesB, companyA, companyB, options, sideA.Closing, sideB.Closing);
        await AttachVoucherDateCounterpartsAsync(result, companyA, ledgerA, companyB, ledgerB, dateFrom, dateTo);
        ApplySalesPurchaseSign(result, ledgerA, ledgerB);
        await AttachPurchaseStockLegAsync(result, companyA, ledgerA, companyB, ledgerB);
        HideSettledVoucherRemainders(result);
        HideMissingThatDoNotExplainDifference(result);
        return result;
    }

    private static List<string> MatchLedgersToParty(IReadOnlyList<string> ledgers, string partyCompany)
    {
        var placeTokens = ExtractPlaceTokens(partyCompany);

        var scored = ledgers
            .Select(l => (Ledger: l, Score: ScoreNameMatch(l, partyCompany)))
            .Where(x => x.Score >= 12)
            .Where(x => placeTokens.Count == 0 || PlaceTokensPresent(x.Ledger, placeTokens))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0) return new List<string>();

        // Keep top cluster (best score and near-best) so Purchase + Sales variants are included.
        var best = scored[0].Score;
        return scored
            .Where(x => x.Score >= Math.Max(12, best - 15))
            .Select(x => x.Ledger)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    /// <summary>
    /// Place/branch names in parentheses, e.g. "HCP … (Vadodara)" → ["Vadodara"].
    /// When present, matched ledgers must include these tokens.
    /// </summary>
    private static List<string> ExtractPlaceTokens(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName)) return new List<string>();

        var places = new List<string>();
        foreach (Match m in Regex.Matches(companyName, @"\(([^)]+)\)"))
        {
            foreach (var w in SignificantWords(m.Groups[1].Value))
            {
                if (!places.Contains(w, StringComparer.OrdinalIgnoreCase))
                    places.Add(w);
            }
        }
        return places;
    }

    private static bool PlaceTokensPresent(string ledgerName, List<string> placeTokens)
    {
        if (placeTokens.Count == 0) return true;
        var ledger = ledgerName ?? "";
        // Every place token from the company must appear in the ledger name.
        return placeTokens.All(p =>
            ledger.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static int ScoreNameMatch(string candidate, string needle)
    {
        var cTokens = SignificantWords(candidate);
        var nTokens = SignificantWords(needle);
        if (cTokens.Count == 0 || nTokens.Count == 0) return 0;

        var score = 0;
        foreach (var n in nTokens)
        {
            foreach (var c in cTokens)
            {
                if (string.Equals(c, n, StringComparison.OrdinalIgnoreCase))
                {
                    // Exact word match (e.g. Plastene, Woven, Vadodara, HCP)
                    score += 10 + Math.Min(n.Length, 8);
                }
                else if (n.Length >= 4 && c.Length >= 4
                         && (c.Contains(n, StringComparison.OrdinalIgnoreCase)
                             || n.Contains(c, StringComparison.OrdinalIgnoreCase)))
                {
                    // Substring only for substantive tokens — blocks "A" matching inside "Plastene"
                    score += 4 + Math.Min(Math.Min(n.Length, c.Length), 6);
                }
            }
        }

        var cl = candidate.ToLowerInvariant();
        var nl = needle.ToLowerInvariant();
        if (nl.Length >= 8 && cl.Contains(nl)) score += 20;
        else if (cl.Length >= 8 && nl.Contains(cl)) score += 20;
        return score;
    }

    public async Task<ComparisonResultDto> CompareFromUploadedLedgersAsync(
        Stream streamA,
        LedgerColumnMapping mappingA,
        Stream streamB,
        LedgerColumnMapping mappingB,
        LedgerMatchOptions? options = null,
        string? companyOverrideA = null,
        string? companyOverrideB = null)
    {
        var exactA = !string.IsNullOrWhiteSpace(companyOverrideA);
        var exactB = !string.IsNullOrWhiteSpace(companyOverrideB);

        var companyA = exactA
            ? companyOverrideA!.Trim()
            : _excel.ExtractCompanyName(streamA, mappingA);
        var companyB = exactB
            ? companyOverrideB!.Trim()
            : _excel.ExtractCompanyName(streamB, mappingB);

        if (string.IsNullOrWhiteSpace(companyA) || string.IsNullOrWhiteSpace(companyB))
            throw new InvalidOperationException(
                "Could not read company names from the uploaded files. Map the Company column or enter company names manually.");

        var coreA = exactA ? companyA : BuildPreferredSearchToken(companyA);
        var coreB = exactB ? companyB : BuildPreferredSearchToken(companyB);

        if (string.IsNullOrWhiteSpace(coreA) || string.IsNullOrWhiteSpace(coreB))
            throw new InvalidOperationException(
                $"Could not derive search names from '{companyA}' / '{companyB}'.");

        if (string.Equals(NormalizeLoose(coreA), NormalizeLoose(coreB), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Company A and Company B resolve to the same search name ('{coreA}'). " +
                "Upload ledgers from two different companies or enter clearer overrides.");

        var entriesA = await FetchBillWiseAsync(companyA, companyB, exactCompany: exactA, exactLedger: exactB);
        var entriesB = await FetchBillWiseAsync(companyB, companyA, exactCompany: exactB, exactLedger: exactA);

        var sideA = await SupplementSideAsync(companyA, null, companyB, entriesA, null, null);
        var sideB = await SupplementSideAsync(companyB, null, companyA, entriesB, null, null);
        entriesA = sideA.Entries;
        entriesB = sideB.Entries;

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows found in vw_BillWiseTransactionWithOnAccount for " +
                $"'{companyA}' ↔ '{companyB}' (searched as '{coreA}' ↔ '{coreB}'). " +
                "Try a shorter override like 'HCP Enterprise' / 'Plastene India'.");
        }

        return FinishCompare(entriesA, entriesB, companyA, companyB, options, sideA.Closing, sideB.Closing);
    }

    public async Task<List<LedgerEntryDto>> FetchBillWiseForLedgersAsync(
        string companyName,
        IReadOnlyList<string> ledgerNames,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateRangeField = null)
    {
        var company = (companyName ?? "").Trim();
        var ledgers = (ledgerNames ?? Array.Empty<string>())
            .Select(l => (l ?? "").Trim())
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (company.Length == 0 || ledgers.Count == 0)
            return new List<LedgerEntryDto>();

        using var connection = _database.CreateConnection();
        var sql = @"
SELECT
    CompanyName,
    LedgerName,
    VoucherNo,
    VoucherDate,
    BillNo,
    BillDate,
    Amount,
    RefType
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND LedgerName IN @LedgerNames";

        var parameters = new DynamicParameters();
        parameters.Add("CompanyName", company);
        parameters.Add("LedgerNames", ledgers);
        AppendDateRangeFilter(ref sql, parameters, dateFrom, dateTo, dateRangeField);

        var rows = (await connection.QueryAsync(
            sql,
            parameters,
            commandTimeout: 180)).ToList();

        var entries = new List<LedgerEntryDto>(rows.Count);
        var rowIndex = 1;
        foreach (var row in rows)
        {
            var entry = MapRow(row, rowIndex);
            if (entry == null) continue;
            entries.Add(entry);
            rowIndex++;
        }
        return entries;
    }

    public async Task<List<LedgerEntryDto>> FetchBillWiseExactAsync(
        string companyName,
        string ledgerName,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateRangeField = null)
    {
        return await FetchBillWiseForLedgersAsync(companyName, new[] { ledgerName }, dateFrom, dateTo, dateRangeField);
    }

    public async Task<List<LedgerEntryDto>> FetchBillWiseAsync(
        string companyName,
        string ledgerName,
        bool exactCompany = false,
        bool exactLedger = false)
    {
        if (exactCompany && exactLedger)
            return await FetchBillWiseExactAsync(companyName, ledgerName);

        var companyTokens = exactCompany
            ? ExactTokenList(companyName)
            : BuildSearchTokens(companyName);
        var ledgerTokens = exactLedger
            ? ExactTokenList(ledgerName)
            : BuildSearchTokens(ledgerName);
        if (companyTokens.Count == 0 || ledgerTokens.Count == 0)
            return new List<LedgerEntryDto>();

        using var connection = _database.CreateConnection();

        foreach (var companyToken in companyTokens)
        {
            foreach (var ledgerToken in ledgerTokens)
            {
                var rows = await QueryBillWiseLikeAsync(connection, companyToken, ledgerToken);
                if (rows.Count == 0) continue;

                var entries = new List<LedgerEntryDto>(rows.Count);
                var rowIndex = 1;
                foreach (var row in rows)
                {
                    var entry = MapRow(row, rowIndex);
                    if (entry == null) continue;
                    entries.Add(entry);
                    rowIndex++;
                }

                if (entries.Count > 0)
                    return entries;
            }
        }

        return new List<LedgerEntryDto>();
    }

    private static List<string> ExactTokenList(string rawName)
    {
        var token = (rawName ?? "").Trim();
        return string.IsNullOrWhiteSpace(token) ? new List<string>() : new List<string> { token };
    }

    private static async Task<List<dynamic>> QueryBillWiseLikeAsync(
        System.Data.IDbConnection connection,
        string companyToken,
        string ledgerToken)
    {
        const string sql = @"
SELECT
    CompanyName,
    LedgerName,
    VoucherNo,
    VoucherDate,
    BillNo,
    BillDate,
    Amount,
    RefType
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName LIKE @CompanyPattern
  AND LedgerName LIKE @LedgerPattern";

        var rows = await connection.QueryAsync(
            sql,
            new
            {
                CompanyPattern = $"%{companyToken}%",
                LedgerPattern = $"%{ledgerToken}%",
            },
            commandTimeout: 180);

        return rows.ToList();
    }

    public static string BuildPreferredSearchToken(string rawName)
    {
        var tokens = BuildSearchTokens(rawName);
        var twoWord = tokens.FirstOrDefault(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 2);
        if (!string.IsNullOrWhiteSpace(twoWord)) return twoWord;
        return tokens.FirstOrDefault() ?? "";
    }

    public static List<string> BuildSearchTokens(string rawName)
    {
        var words = SignificantWords(rawName);
        if (words.Count == 0) return new List<string>();

        var tokens = new List<string>();

        void Add(string token)
        {
            token = Regex.Replace(token.Trim(), @"\s+", " ");
            if (token.Length < 2) return;
            if (tokens.Any(t => string.Equals(t, token, StringComparison.OrdinalIgnoreCase))) return;
            tokens.Add(token);
        }

        if (words.Count >= 3)
            Add(string.Join(" ", words.Take(3)));
        if (words.Count >= 2)
            Add(string.Join(" ", words.Take(2)));
        Add(string.Join(" ", words));
        Add(words[0]);

        return tokens
            .OrderByDescending(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ThenByDescending(t => t.Length)
            .ToList();
    }

    private static List<string> SignificantWords(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return new List<string>();

        // Keep place names from parentheses: "HCP … (Vadodara)" → include Vadodara
        var text = Regex.Replace(rawName, @"[()]", " ");
        text = Regex.Replace(text, @"[_\./\\|,;:+]+", " ");
        text = text.Replace('-', ' ');

        return Regex.Split(text, @"\s+")
            .Select(w => w.Trim())
            .Where(w => w.Length >= 3) // drop "A", "C", "P" letter noise
            .Where(w => !NoiseTokens.Contains(w))
            .Where(w => !Regex.IsMatch(w, @"^(i{1,3}|iv|v|vi{0,3}|ix|x|\d+)$", RegexOptions.IgnoreCase))
            .ToList();
    }

    private static LedgerEntryDto? MapRow(dynamic row, int rowIndex)
    {
        var dict = (IDictionary<string, object>)row;
        var amount = ToDecimal(Get(dict, "Amount", "amount", "Amt"));
        var billNo = ToString(Get(dict, "BillNo", "Bill No", "billno"));
        var voucherNo = ToString(Get(dict, "VoucherNo", "Voucher No", "voucherno"));
        var particulars = ToString(Get(dict, "LedgerName", "Ledger Name", "Particulars", "particulars"));
        var company = ToString(Get(dict, "CompanyName", "Company Name", "Company"));
        var voucherDate = ToDate(Get(dict, "VoucherDate", "Voucher Date", "Date", "date"));
        var billDate = ToDate(Get(dict, "BillDate", "Bill Date", "billdate"));

        if (amount == 0m && string.IsNullOrWhiteSpace(billNo) && voucherDate == null && billDate == null)
            return null;

        return new LedgerEntryDto
        {
            RowIndex = rowIndex,
            Company = company,
            Date = voucherDate ?? billDate,
            BillDate = billDate ?? voucherDate,
            Particulars = particulars,
            VoucherNo = voucherNo,
            VoucherRef = ToString(Get(dict, "VoucherRef", "Voucher Ref", "RefType")),
            BillNo = billNo,
            SignedAmount = amount,
            Debit = amount < 0 ? Math.Abs(amount) : 0m,
            Credit = amount > 0 ? amount : 0m,
        };
    }

    private static object? Get(IDictionary<string, object> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value is DBNull ? null : kv.Value;
            }
        }
        return null;
    }

    private static string ToString(object? value)
    {
        if (value == null || value is DBNull) return "";
        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
    }

    private static decimal ToDecimal(object? value)
    {
        if (value == null || value is DBNull) return 0m;
        if (value is decimal d) return Math.Round(d, 2);
        if (value is double dbl) return Math.Round((decimal)dbl, 2);
        if (value is float f) return Math.Round((decimal)f, 2);
        if (value is int i) return i;
        if (value is long l) return l;
        var s = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(s) || s == "-" || s == "—") return 0m;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Round(parsed, 2)
            : 0m;
    }

    private static DateTime? ToDate(object? value)
    {
        if (value == null || value is DBNull) return null;
        if (value is DateTime dt) return dt.Date;
        if (value is DateTimeOffset dto) return dto.Date;
        var s = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out var enIn))
            return enIn.Date;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inv))
            return inv.Date;
        return null;
    }

    private static string NormalizeLoose(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static (DateTime? From, DateTime? ToExclusive) NormalizeDateRange(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom == null && dateTo == null)
            return (null, null);

        var from = dateFrom?.Date;
        var to = dateTo?.Date;
        if (from == null && to == null)
            return (null, null);
        if (from == null)
            from = to;
        if (to == null)
            to = from;
        if (from!.Value > to!.Value)
            (from, to) = (to, from);

        return (from, to!.Value.AddDays(1));
    }

    private static string ResolveDateRangeColumn(string? dateRangeField) =>
        string.Equals(dateRangeField, "bill", StringComparison.OrdinalIgnoreCase)
            ? "BillDate"
            : "VoucherDate";

    private static void AppendDateRangeFilter(
        ref string sql,
        DynamicParameters parameters,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? dateRangeField = null)
    {
        var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
        if (from == null || toExclusive == null)
            return;

        var column = ResolveDateRangeColumn(dateRangeField);
        sql += $@"
  AND {column} >= @DateFrom
  AND {column} < @DateToExclusive";
        parameters.Add("DateFrom", from.Value);
        parameters.Add("DateToExclusive", toExclusive.Value);
    }

    /// <summary>
    /// A bill can show two lines on one side and one on the other because the settlement
    /// was posted to a different bill date. Pull that one line by voucher date and amount.
    /// </summary>
    private async Task AttachVoucherDateCounterpartsAsync(
        ComparisonResultDto result,
        string companyA,
        string ledgerA,
        string companyB,
        string ledgerB,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var jobs = new List<(ComparisonPairDto Pair, bool MissingOnA, LedgerEntryDto Extra)>();
        foreach (var pair in result.Results)
        {
            if (pair.EntriesA.Count == 0 || pair.EntriesB.Count == 0)
                continue;
            jobs.AddRange(SurplusLines(pair, pair.EntriesA, pair.EntriesB).Select(line => (pair, MissingOnA: false, line)));
            jobs.AddRange(SurplusLines(pair, pair.EntriesB, pair.EntriesA).Select(line => (pair, MissingOnA: true, line)));
        }

        if (jobs.Count == 0)
            return;

        var from = dateFrom?.Date;
        var to = dateTo?.Date;
        jobs = jobs.Where(job =>
        {
            var day = job.Extra.Date?.Date;
            if (day == null) return false;
            if (from != null && day < from) return false;
            if (to != null && day > to) return false;
            return true;
        }).ToList();
        if (jobs.Count == 0)
            return;

        try
        {
            using var connection = _database.CreateConnection();
            var nextIndex = result.Results
                .SelectMany(p => p.EntriesA.Concat(p.EntriesB))
                .Select(e => e.RowIndex)
                .DefaultIfEmpty(0)
                .Max() + 1;
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in result.Results)
            {
                foreach (var entry in pair.EntriesA.Concat(pair.EntriesB))
                    used.Add(EntryKey(entry));
            }

            var addedA = 0;
            var addedB = 0;
            var touched = new HashSet<ComparisonPairDto>();
            foreach (var group in jobs.GroupBy(job => job.MissingOnA))
            {
                var company = group.Key ? companyA : companyB;
                var ledger = group.Key ? ledgerA : ledgerB;
                var found = await FindByVoucherDateAndAmountAsync(connection, company, ledger, group.Select(job => job.Extra).ToList());
                foreach (var job in group)
                {
                    var amount = Math.Abs(job.Extra.SignedAmount);
                    var day = job.Extra.Date!.Value.Date;
                    var billKeys = BillKeys(job.Pair);
                    foreach (var row in found)
                    {
                        if (row.Date?.Date != day) continue;
                        if (Math.Abs(Math.Abs(row.SignedAmount) - amount) > 0.01m) continue;
                        if (!billKeys.Contains(BillKey(row.BillNo))) continue;
                        var key = EntryKey(row);
                        if (!used.Add(key)) continue;

                        row.RowIndex = nextIndex++;
                        if (job.MissingOnA)
                        {
                            job.Pair.EntriesA.Add(row);
                            addedA++;
                        }
                        else
                        {
                            job.Pair.EntriesB.Add(row);
                            addedB++;
                        }
                        touched.Add(job.Pair);
                    }
                }
            }

            if (addedA == 0 && addedB == 0)
                return;

            foreach (var pair in touched)
                RefreshPairTotals(pair);

            result.Summary.TotalA += addedA;
            result.Summary.TotalB += addedB;
            result.Summary.Matched = result.Results.Count(r => r.Status == "Matched");
            result.Summary.AmountMismatch = result.Results.Count(r => r.Status == "AmountMismatch");
            result.Summary.MissingInA = result.Results.Count(r => r.Status == "MissingInA");
            result.Summary.MissingInB = result.Results.Count(r => r.Status == "MissingInB");
            result.Summary.PotentialMatches = result.Results.Count(r => r.Status == "PotentialMatch");
            result.Summary.PendingRecords = result.Results.Count(r => r.Status == "PendingRecord");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Voucher-date counterpart lookup skipped: {ex.Message}");
        }
    }

    /// <summary>
    /// When sales has two credits and purchase has only the invoice, the other leg of that
    /// purchase voucher is on the stock ledger. Add it on the purchase side as a debit so
    /// the bill matches. Closings stay on the two selected ledgers.
    /// </summary>
    private async Task AttachPurchaseStockLegAsync(
        ComparisonResultDto result,
        string companyA,
        string ledgerA,
        string companyB,
        string ledgerB)
    {
        if (!TrySalesPurchaseSides(ledgerA, ledgerB, out var salesIsA))
            return;

        var jobs = new List<(ComparisonPairDto Pair, string VoucherNo, decimal Amount, int SalesSign)>();
        foreach (var pair in result.Results)
        {
            var sales = salesIsA ? pair.EntriesA : pair.EntriesB;
            var purchase = salesIsA ? pair.EntriesB : pair.EntriesA;
            foreach (var group in sales.GroupBy(line => Math.Round(Math.Abs(line.SignedAmount), 2)))
            {
                if (group.Key <= 0.01m) continue;
                var lines = group.ToList();
                if (lines.Count < 2) continue;
                var sign = Math.Sign(lines[0].SignedAmount);
                if (sign == 0 || lines.Any(line => Math.Sign(line.SignedAmount) != sign))
                    continue;

                var purchaseLines = purchase
                    .Where(line => Math.Abs(Math.Abs(line.SignedAmount) - group.Key) <= 0.01m)
                    .ToList();
                var missing = lines.Count - purchaseLines.Count;
                if (missing <= 0) continue;

                var voucher = purchaseLines
                    .Select(line => (line.VoucherNo ?? "").Trim())
                    .FirstOrDefault(no => no.Length > 0);
                if (string.IsNullOrWhiteSpace(voucher)) continue;
                for (var n = 0; n < missing; n++)
                    jobs.Add((pair, voucher, group.Key, sign));
            }
        }

        if (jobs.Count == 0)
            return;

        var purchaseCompany = salesIsA ? companyB : companyA;
        var purchaseLedger = salesIsA ? ledgerB : ledgerA;
        try
        {
            using var connection = _database.CreateConnection();
            var found = await FindStockLegAsync(connection, purchaseCompany, purchaseLedger, jobs);
            var nextIndex = result.Results
                .SelectMany(p => p.EntriesA.Concat(p.EntriesB))
                .Select(e => e.RowIndex)
                .DefaultIfEmpty(0)
                .Max() + 1;
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var touched = new HashSet<ComparisonPairDto>();
            var addedA = 0;
            var addedB = 0;

            foreach (var job in jobs)
            {
                var row = found.FirstOrDefault(entry =>
                    string.Equals((entry.VoucherNo ?? "").Trim(), job.VoucherNo, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(Math.Abs(entry.SignedAmount) - job.Amount) <= 0.01m
                    && used.Add(EntryKey(entry)));
                if (row == null) continue;

                var target = -job.SalesSign * job.Amount;
                if (Math.Sign(row.SignedAmount) != Math.Sign(target))
                {
                    row.SignedAmount = target;
                    row.Debit = row.SignedAmount < 0 ? Math.Abs(row.SignedAmount) : 0m;
                    row.Credit = row.SignedAmount > 0 ? row.SignedAmount : 0m;
                }

                row.RowIndex = nextIndex++;
                if (salesIsA)
                {
                    job.Pair.EntriesB.Add(row);
                    addedB++;
                }
                else
                {
                    job.Pair.EntriesA.Add(row);
                    addedA++;
                }
                touched.Add(job.Pair);
            }

            if (touched.Count == 0)
                return;

            foreach (var pair in touched)
                RefreshPairTotals(pair);

            result.Summary.TotalA += addedA;
            result.Summary.TotalB += addedB;
            RecountSummary(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Purchase stock-leg lookup skipped: {ex.Message}");
        }
    }

    private async Task<List<LedgerEntryDto>> FindStockLegAsync(
        System.Data.Common.DbConnection connection,
        string company,
        string purchaseLedger,
        List<(ComparisonPairDto Pair, string VoucherNo, decimal Amount, int SalesSign)> jobs)
    {
        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("CompanyName", company);
        parameters.Add("PurchaseLedger", purchaseLedger);
        var i = 0;
        foreach (var job in jobs.GroupBy(job => $"{job.VoucherNo}|{job.Amount:0.00}", StringComparer.OrdinalIgnoreCase))
        {
            var sample = job.First();
            parameters.Add($"V{i}", sample.VoucherNo);
            parameters.Add($"A{i}", sample.Amount);
            clauses.Add($"(VoucherNo = @V{i} AND ABS(Amount) BETWEEN @A{i} - 0.01 AND @A{i} + 0.01)");
            i++;
        }

        var sql = $@"
SELECT
    CompanyName,
    LedgerName,
    VoucherNo,
    VoucherDate,
    BillNo,
    BillDate,
    Amount,
    RefType
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND LedgerName <> @PurchaseLedger
  AND ({string.Join(" OR ", clauses)})";

        var rows = (await connection.QueryAsync(sql, parameters, commandTimeout: 180)).ToList();
        var entries = new List<LedgerEntryDto>();
        foreach (var row in rows)
        {
            var entry = MapRow(row, 0);
            if (entry != null)
                entries.Add(entry);
        }
        return entries;
    }

    /// <summary>
    /// Sales often stores the settlement as a second credit. On a purchase ledger that same
    /// settlement is a credit and cancels the invoice. For a sales-versus-purchase compare,
    /// turn that against-reference into the opposite of the sales line so both purchase
    /// lines stay on the invoice's side. Other ledger pairs are left unchanged.
    /// </summary>
    private static bool TrySalesPurchaseSides(string ledgerA, string ledgerB, out bool salesIsA)
    {
        var aIsSales = IsSalesLedger(ledgerA);
        var bIsSales = IsSalesLedger(ledgerB);
        var aIsPurchase = IsPurchaseLedger(ledgerA);
        var bIsPurchase = IsPurchaseLedger(ledgerB);
        if (aIsSales && bIsPurchase && !aIsPurchase && !bIsSales)
        {
            salesIsA = true;
            return true;
        }
        if (bIsSales && aIsPurchase && !bIsPurchase && !aIsSales)
        {
            salesIsA = false;
            return true;
        }
        salesIsA = false;
        return false;
    }

    /// <summary>
    /// A no-bill line left over from a voucher whose bill lines are already in the compare
    /// is the outside-range remainder. It is already inside the ledger closing, so it is not shown.
    /// A missing bill, or a no-bill line that is the only line for its voucher, stays.
    /// </summary>
    private static void HideSettledVoucherRemainders(ComparisonResultDto result)
    {
        static string Key(LedgerEntryDto entry) =>
            $"{(entry.VoucherNo ?? "").Trim().ToUpperInvariant()}|{(entry.Date?.ToString("yyyy-MM-dd") ?? "")}";

        var billedA = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var billedB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in result.Results)
        {
            foreach (var entry in pair.EntriesA)
            {
                if (!string.IsNullOrWhiteSpace(entry.BillNo) && !string.IsNullOrWhiteSpace(entry.VoucherNo))
                    billedA.Add(Key(entry));
            }
            foreach (var entry in pair.EntriesB)
            {
                if (!string.IsNullOrWhiteSpace(entry.BillNo) && !string.IsNullOrWhiteSpace(entry.VoucherNo))
                    billedB.Add(Key(entry));
            }
        }

        var removedA = 0;
        var removedB = 0;
        result.Results = result.Results.Where(pair =>
        {
            var onA = pair.EntriesA.Count > 0 && pair.EntriesB.Count == 0;
            var onB = pair.EntriesB.Count > 0 && pair.EntriesA.Count == 0;
            if (!onA && !onB)
                return true;
            if (pair.Status != "MissingInA" && pair.Status != "MissingInB")
                return true;

            var line = onA ? pair.EntriesA[0] : pair.EntriesB[0];
            if (pair.EntriesA.Count + pair.EntriesB.Count != 1)
                return true;
            if (!string.IsNullOrWhiteSpace(line.BillNo) || string.IsNullOrWhiteSpace(line.VoucherNo) || line.Date == null)
                return true;
            if (!(onA ? billedA : billedB).Contains(Key(line)))
                return true;

            if (onA) removedA++;
            else removedB++;
            return false;
        }).ToList();

        if (removedA == 0 && removedB == 0)
            return;

        result.Summary.TotalA = Math.Max(0, result.Summary.TotalA - removedA);
        result.Summary.TotalB = Math.Max(0, result.Summary.TotalB - removedB);
        RecountSummary(result);
    }

    /// <summary>
    /// When the two closings already agree (within ₹1), missing rows stay only if their signed
    /// total is that difference. One row or several rows can make that total. A large closing
    /// difference keeps its missing rows, because those rows are the unmatched balance.
    /// The closing figures are left unchanged.
    /// </summary>
    private static void HideMissingThatDoNotExplainDifference(ComparisonResultDto result)
    {
        if (!result.Summary.ClosingKnown)
            return;

        if (Math.Abs(result.Summary.ClosingDifference) > 1m)
            return;

        var missing = result.Results
            .Where(pair => pair.Status is "MissingInA" or "MissingInB")
            .ToList();
        if (missing.Count == 0)
            return;

        var signed = missing.Sum(pair => pair.EntriesA.Sum(e => e.SignedAmount) + pair.EntriesB.Sum(e => e.SignedAmount));
        if (Math.Abs(signed - result.Summary.ClosingDifference) <= 1m)
            return;

        var removedA = missing.Sum(pair => pair.EntriesA.Count);
        var removedB = missing.Sum(pair => pair.EntriesB.Count);
        result.Results = result.Results
            .Where(pair => pair.Status is not "MissingInA" and not "MissingInB")
            .ToList();
        result.Summary.TotalA = Math.Max(0, result.Summary.TotalA - removedA);
        result.Summary.TotalB = Math.Max(0, result.Summary.TotalB - removedB);
        RecountSummary(result);
    }

    private static void RecountSummary(ComparisonResultDto result)
    {
        result.Summary.Matched = result.Results.Count(r => r.Status == "Matched");
        result.Summary.AmountMismatch = result.Results.Count(r => r.Status == "AmountMismatch");
        result.Summary.MissingInA = result.Results.Count(r => r.Status == "MissingInA");
        result.Summary.MissingInB = result.Results.Count(r => r.Status == "MissingInB");
        result.Summary.PotentialMatches = result.Results.Count(r => r.Status == "PotentialMatch");
        result.Summary.PendingRecords = result.Results.Count(r => r.Status == "PendingRecord");
    }

    private static void ApplySalesPurchaseSign(ComparisonResultDto result, string ledgerA, string ledgerB)
    {
        if (!TrySalesPurchaseSides(ledgerA, ledgerB, out var salesIsA))
            return;

        var touched = new HashSet<ComparisonPairDto>();
        foreach (var pair in result.Results)
        {
            var salesLines = salesIsA ? pair.EntriesA : pair.EntriesB;
            var purchaseLines = salesIsA ? pair.EntriesB : pair.EntriesA;
            if (!FlipPurchaseSettlement(salesLines, purchaseLines))
                continue;
            touched.Add(pair);
        }

        if (touched.Count == 0)
            return;

        foreach (var pair in touched)
            RefreshPairTotals(pair);

        RecountSummary(result);
    }

    private static bool IsSalesLedger(string ledger) =>
        ledger.Contains("sales", StringComparison.OrdinalIgnoreCase);

    private static bool IsPurchaseLedger(string ledger) =>
        ledger.Contains("purchase", StringComparison.OrdinalIgnoreCase);

    private static bool FlipPurchaseSettlement(List<LedgerEntryDto> sales, List<LedgerEntryDto> purchase)
    {
        var changed = false;
        foreach (var group in sales.GroupBy(line => Math.Round(Math.Abs(line.SignedAmount), 2)))
        {
            if (group.Key <= 0.01m) continue;
            var lines = group.ToList();
            if (lines.Count < 2) continue;
            var sign = Math.Sign(lines[0].SignedAmount);
            if (sign == 0 || lines.Any(line => Math.Sign(line.SignedAmount) != sign))
                continue;

            foreach (var line in purchase)
            {
                if (Math.Abs(Math.Abs(line.SignedAmount) - group.Key) > 0.01m) continue;
                if (Math.Sign(line.SignedAmount) != sign) continue;
                if (line.VoucherRef.IndexOf("agst", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                line.SignedAmount = -line.SignedAmount;
                line.Debit = line.SignedAmount < 0 ? Math.Abs(line.SignedAmount) : 0m;
                line.Credit = line.SignedAmount > 0 ? line.SignedAmount : 0m;
                changed = true;
            }
        }
        return changed;
    }

    private static IEnumerable<LedgerEntryDto> SurplusLines(
        ComparisonPairDto pair,
        List<LedgerEntryDto> side,
        List<LedgerEntryDto> other)
    {
        foreach (var amountGroup in side.GroupBy(line => Math.Round(Math.Abs(line.SignedAmount), 2)))
        {
            if (amountGroup.Key <= 0.01m) continue;
            var here = amountGroup.OrderBy(line => line.Date ?? DateTime.MaxValue).ToList();
            var otherCount = other.Count(line => Math.Abs(Math.Abs(line.SignedAmount) - amountGroup.Key) <= 0.01m);
            if (here.Count <= otherCount) continue;
            foreach (var extra in here.Skip(otherCount))
            {
                if (extra.Date == null) continue;
                if (string.IsNullOrWhiteSpace(pair.EntryA?.BillNo) && string.IsNullOrWhiteSpace(pair.EntryB?.BillNo))
                    continue;
                yield return extra;
            }
        }
    }

    private async Task<List<LedgerEntryDto>> FindByVoucherDateAndAmountAsync(
        System.Data.Common.DbConnection connection,
        string company,
        string ledger,
        List<LedgerEntryDto> extras)
    {
        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("CompanyName", company);
        parameters.Add("LedgerName", ledger);
        var i = 0;
        foreach (var extra in extras.GroupBy(line => $"{line.Date:yyyy-MM-dd}|{Math.Round(Math.Abs(line.SignedAmount), 2)}"))
        {
            var sample = extra.First();
            parameters.Add($"D{i}", sample.Date!.Value.Date);
            parameters.Add($"N{i}", sample.Date!.Value.Date.AddDays(1));
            parameters.Add($"A{i}", Math.Round(Math.Abs(sample.SignedAmount), 2));
            clauses.Add($"(VoucherDate >= @D{i} AND VoucherDate < @N{i} AND ABS(Amount) BETWEEN @A{i} - 0.01 AND @A{i} + 0.01)");
            i++;
        }

        if (clauses.Count == 0)
            return new List<LedgerEntryDto>();

        var sql = $@"
SELECT
    CompanyName,
    LedgerName,
    VoucherNo,
    VoucherDate,
    BillNo,
    BillDate,
    Amount,
    RefType
FROM dbo.vw_BillWiseTransactionWithOnAccount WITH (NOLOCK)
WHERE CompanyName = @CompanyName
  AND LedgerName = @LedgerName
  AND ({string.Join(" OR ", clauses)})";

        var rows = (await connection.QueryAsync(sql, parameters, commandTimeout: 180)).ToList();
        var entries = new List<LedgerEntryDto>();
        foreach (var row in rows)
        {
            var entry = MapRow(row, 0);
            if (entry != null)
                entries.Add(entry);
        }
        return entries;
    }

    private static void RefreshPairTotals(ComparisonPairDto pair)
    {
        if (pair.EntriesA.Count > 0)
            pair.EntryA = SummarizeSide(pair.EntriesA, pair.EntryA);
        if (pair.EntriesB.Count > 0)
            pair.EntryB = SummarizeSide(pair.EntriesB, pair.EntryB);

        var totalA = pair.EntriesA.Sum(e => e.SignedAmount);
        var totalB = pair.EntriesB.Sum(e => e.SignedAmount);
        var gap = Math.Abs(Math.Abs(totalA) - Math.Abs(totalB));
        var lines = $"{pair.EntriesA.Count} ↔ {pair.EntriesB.Count} lines";
        var bothZero = Math.Abs(totalA) <= 1m && Math.Abs(totalB) <= 1m;
        var opposite = totalA != 0 && totalB != 0 && Math.Sign(totalA) != Math.Sign(totalB) && gap <= 1m;
        if (bothZero || opposite)
        {
            pair.Status = "Matched";
            pair.Difference = null;
            pair.Message = bothZero
                ? $"Bill totals both net to 0 after including the voucher-date line ({lines})"
                : $"Reconciled after including the voucher-date line ({lines})";
            return;
        }

        pair.Status = "PotentialMatch";
        pair.Difference = gap;
        pair.Message = $"Related bill includes the other side's voucher-date line ({lines}, diff {gap:N2})";
    }

    private static LedgerEntryDto SummarizeSide(List<LedgerEntryDto> entries, LedgerEntryDto? previous)
    {
        var total = entries.Sum(e => e.SignedAmount);
        var first = entries[0];
        return new LedgerEntryDto
        {
            RowIndex = previous?.RowIndex ?? first.RowIndex,
            Company = previous?.Company ?? first.Company,
            BillNo = string.IsNullOrWhiteSpace(previous?.BillNo) ? first.BillNo : previous!.BillNo,
            BillDate = previous?.BillDate ?? first.BillDate,
            Date = entries.Select(e => e.Date).Where(d => d.HasValue).OrderBy(d => d).FirstOrDefault(),
            Particulars = entries.Count == 1 ? first.Particulars : $"Bill total ({entries.Count} lines)",
            VoucherNo = entries.Count == 1 ? first.VoucherNo : "",
            SignedAmount = total,
            Debit = total < 0 ? Math.Abs(total) : 0m,
            Credit = total > 0 ? total : 0m,
        };
    }

    private static HashSet<string> BillKeys(ComparisonPairDto pair)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? bill)
        {
            var key = BillKey(bill);
            if (key.Length > 0) keys.Add(key);
        }

        Add(pair.EntryA?.BillNo);
        Add(pair.EntryB?.BillNo);
        foreach (var entry in pair.EntriesA.Concat(pair.EntriesB))
            Add(entry.BillNo);
        return keys;
    }

    private static string BillKey(string? billNo)
    {
        if (string.IsNullOrWhiteSpace(billNo)) return "";
        var chars = billNo.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToUpperInvariant();
    }

    private static string EntryKey(LedgerEntryDto entry) =>
        $"{entry.VoucherNo}|{entry.Date:yyyy-MM-dd}|{entry.BillNo}|{entry.SignedAmount:0.00}";

    private ComparisonResultDto FinishCompare(
        List<LedgerEntryDto> entriesA,
        List<LedgerEntryDto> entriesB,
        string companyA,
        string companyB,
        LedgerMatchOptions? options,
        decimal closingA,
        decimal closingB)
    {
        var result = _excel.CompareEntries(entriesA, entriesB, companyA, companyB, options);
        result.Summary.ClosingA = closingA;
        result.Summary.ClosingB = closingB;
        result.Summary.ClosingDifference = closingA + closingB;
        result.Summary.ClosingKnown = true;
        return result;
    }

    /// <summary>
    /// Bill-wise rows miss journals, on-account credit notes, and some inter-unit lines.
    /// Pull the same ledger from the ledger summary and add only what the view does not already cover.
    /// </summary>
    private async Task<(List<LedgerEntryDto> Entries, decimal Closing)> SupplementSideAsync(
        string companyName,
        string? exactLedger,
        string otherCompanyName,
        List<LedgerEntryDto> billWise,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var entries = billWise;
        try
        {
            using var connection = _database.CreateConnection();
            var companyId = await connection.QueryFirstOrDefaultAsync<int?>(@"
SELECT TOP 1 fi.srno
FROM FactoryInfo fi WITH (NOLOCK)
WHERE LTRIM(RTRIM(fi.Name)) = @Name",
                new { Name = companyName });

            if (companyId is null or 0)
            {
                var core = BuildCompanyCore(companyName);
                if (!string.IsNullOrWhiteSpace(core))
                {
                    companyId = await connection.QueryFirstOrDefaultAsync<int?>(@"
SELECT TOP 1 fi.srno
FROM FactoryInfo fi WITH (NOLOCK)
WHERE fi.Name LIKE @Like
ORDER BY LEN(fi.Name)",
                        new { Like = "%" + core.Replace("%", "") + "%" });
                }
            }

            if (companyId is null or 0)
                return (entries, 0m);

            var ledgers = await ResolveSupplementLedgersAsync(connection, companyId.Value, exactLedger, otherCompanyName, entries);
            if (ledgers.Count == 0)
                return (entries, 0m);

            var from = (dateFrom ?? new DateTime(1990, 4, 1)).Date;
            var to = (dateTo ?? DateTime.Today).Date;
            if (from > to)
                (from, to) = (to, from);

            var closing = 0m;
            var nextIndex = entries.Count == 0 ? 1 : entries.Max(e => e.RowIndex) + 1;
            var merged = new List<LedgerEntryDto>(entries);

            foreach (var ledger in ledgers)
            {
                var table = await LoadLedgerSummaryAsync(connection, companyId.Value, ledger, from, to);
                if (table.Rows.Count == 0)
                    continue;

                closing += LedgerClosing(table);
                var added = MissingLedgerLines(merged, table, companyName, ref nextIndex);
                merged.AddRange(added);
            }

            Console.WriteLine($"Ledger supplement {companyName}: +{merged.Count - entries.Count} lines, closing {closing:0.00}");
            return (merged, closing);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ledger supplement skipped for {companyName}: {ex.Message}");
            return (entries, 0m);
        }
    }

    private async Task<List<string>> ResolveSupplementLedgersAsync(
        System.Data.Common.DbConnection connection,
        int companyId,
        string? exactLedger,
        string otherCompanyName,
        List<LedgerEntryDto> billWise)
    {
        if (!string.IsNullOrWhiteSpace(exactLedger))
            return new List<string> { exactLedger.Trim() };

        var names = billWise
            .Select(e => (e.Particulars ?? "").Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        var otherCore = BuildCompanyCore(otherCompanyName);
        var likeOther = string.IsNullOrWhiteSpace(otherCore) ? "%" : "%" + otherCore.Replace("%", "") + "%";

        var candidates = (await connection.QueryAsync<string>(@"
SELECT DISTINCT TOP 40 LTRIM(RTRIM(l.LedgerName))
FROM ledgermaster l WITH (NOLOCK)
INNER JOIN FactoryInfo fi WITH (NOLOCK) ON fi.Name = l.CompanyName AND fi.srno = @CompanyId
WHERE ISNULL(l.LedgerName, '') <> ''
  AND (
        l.LedgerName LIKE @LikeOther
     OR l.LedgerName LIKE '%inter%unit%'
     OR l.LedgerName LIKE '%intercompany%'
     OR l.LedgerName LIKE '%inter company%'
     OR l.LedgerName LIKE '%inter-company%'
  )",
            new { CompanyId = companyId, LikeOther = likeOther })).ToList();

        foreach (var candidate in candidates)
        {
            var name = (candidate ?? "").Trim();
            if (name.Length == 0) continue;
            if (names.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

            var interUnit = name.Contains("inter", StringComparison.OrdinalIgnoreCase)
                && (name.Contains("unit", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("company", StringComparison.OrdinalIgnoreCase));
            var score = ScoreNameMatch(name, otherCompanyName);
            if (score >= 12 || (interUnit && score >= 8) || name.Contains(otherCore ?? "", StringComparison.OrdinalIgnoreCase) && (otherCore?.Length ?? 0) >= 4)
                names.Add(name);

            if (names.Count >= 15) break;
        }

        return names;
    }

    private static async Task<DataTable> LoadLedgerSummaryAsync(
        System.Data.Common.DbConnection connection,
        int companyId,
        string ledgerName,
        DateTime from,
        DateTime to)
    {
        var table = new DataTable();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "sp_ac_LedgerSummary_BankRecoDate";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 120;
        AddParam(cmd, "@CompanyType", 2);
        AddParam(cmd, "@CompanyName", "");
        AddParam(cmd, "@CompanyId", companyId);
        AddParam(cmd, "@LedgerName", ledgerName);
        AddParam(cmd, "@Currency", DBNull.Value);
        AddParam(cmd, "@DateFrom", from);
        AddParam(cmd, "@DateTo", to);
        AddParam(cmd, "@InterestCal", 0);
        await using var reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);
        return table;
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static decimal LedgerClosing(DataTable table)
    {
        var closing = 0m;
        foreach (DataRow row in table.Rows)
            closing += CellDecimal(row, "amount", "Amount");
        return closing;
    }

    private static List<LedgerEntryDto> MissingLedgerLines(
        List<LedgerEntryDto> existing,
        DataTable table,
        string companyName,
        ref int nextIndex)
    {
        var covered = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in existing)
        {
            var key = VoucherKey(entry.VoucherNo, entry.Date);
            covered[key] = covered.GetValueOrDefault(key) + entry.SignedAmount;
        }

        var fresh = new List<(string Key, decimal Signed, string Type, DateTime? Date, string Voucher, string Contra, string VoucherRef)>();
        foreach (DataRow row in table.Rows)
        {
            var type = CellString(row, "VoucherType");
            if (string.Equals(type, "z", StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = CellDecimal(row, "amount", "Amount");
            if (raw == 0m)
                continue;

            var date = CellDate(row, "Date");
            var voucher = CellString(row, "voucherno", "VoucherNo");
            fresh.Add((
                VoucherKey(voucher, date),
                -raw,
                type,
                date,
                voucher,
                CellString(row, "LedgerName"),
                CellString(row, "VoucherRef")));
        }

        var added = new List<LedgerEntryDto>();
        foreach (var group in fresh.GroupBy(x => x.Key))
        {
            var already = covered.GetValueOrDefault(group.Key);
            var ledgerTotal = group.Sum(x => x.Signed);
            if (Math.Abs(already - ledgerTotal) <= 1m)
                continue;

            // Bill list has this voucher as a credit and the ledger has it as a debit
            // (or the reverse) for the same amount. Inserting the gap would double it.
            if (Math.Abs(Math.Abs(already) - Math.Abs(ledgerTotal)) <= 1m)
                continue;

            if (Math.Abs(already) <= 0.005m)
            {
                foreach (var line in group)
                    added.Add(ToEntry(line.Type, line.Date, line.Voucher, line.Contra, line.VoucherRef, line.Signed, companyName, ref nextIndex));
            }
            else if (Math.Sign(already) == Math.Sign(ledgerTotal))
            {
                var sample = group.First();
                var gap = ledgerTotal - already;
                added.Add(ToEntry(sample.Type, sample.Date, sample.Voucher, sample.Contra, sample.VoucherRef, gap, companyName, ref nextIndex));
            }

            covered[group.Key] = ledgerTotal;
        }

        return added;
    }

    private static LedgerEntryDto ToEntry(
        string voucherType,
        DateTime? date,
        string voucherNo,
        string contra,
        string voucherRef,
        decimal signed,
        string companyName,
        ref int nextIndex)
    {
        var type = string.IsNullOrWhiteSpace(voucherType) ? "Journal" : voucherType.Trim();
        var note = type.Contains("credit", StringComparison.OrdinalIgnoreCase)
            || type.Contains("note", StringComparison.OrdinalIgnoreCase);
        var bill = "";
        if (note && !string.IsNullOrWhiteSpace(voucherRef)
            && !string.Equals(voucherRef.Trim(), (voucherNo ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            bill = voucherRef.Trim();

        var particulars = string.IsNullOrWhiteSpace(contra) ? type : $"{type} · {contra.Trim()}";
        return new LedgerEntryDto
        {
            RowIndex = nextIndex++,
            Company = companyName,
            Date = date,
            BillDate = bill.Length > 0 ? date : null,
            Particulars = particulars,
            VoucherNo = (voucherNo ?? "").Trim(),
            VoucherRef = type,
            BillNo = bill,
            SignedAmount = signed,
            Debit = signed < 0 ? Math.Abs(signed) : 0m,
            Credit = signed > 0 ? signed : 0m,
        };
    }

    private static string VoucherKey(string? voucherNo, DateTime? date) =>
        $"{(voucherNo ?? "").Trim().ToUpperInvariant()}|{(date?.ToString("yyyy-MM-dd") ?? "")}";

    private static string CellString(DataRow row, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                return row[column] == DBNull.Value ? "" : Convert.ToString(row[column])?.Trim() ?? "";
            }
        }
        return "";
    }

    private static decimal CellDecimal(DataRow row, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                return ToDecimal(row[column]);
            }
        }
        return 0m;
    }

    private static DateTime? CellDate(DataRow row, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (row[column] == DBNull.Value) return null;
                if (row[column] is DateTime dt) return dt.Date;
                return ToDate(row[column]);
            }
        }
        return null;
    }
}
