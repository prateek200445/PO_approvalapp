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
        DateTime? dateTo = null)
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
        var fetchA = FetchBySwappedLikeAsync(coreA, placesA, coreB, placesB, dateFrom, dateTo);
        // Side B books: company~B (+placesB), ledger~A (+placesA)
        var fetchB = FetchBySwappedLikeAsync(coreB, placesB, coreA, placesA, dateFrom, dateTo);
        await Task.WhenAll(fetchA, fetchB);
        var entriesA = await fetchA;
        var entriesB = await fetchB;

        Console.WriteLine($"Rows A: {entriesA.Count}  |  Rows B: {entriesB.Count}");

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows for '{coreA}' ↔ '{coreB}'. " +
                "Check company spelling against ERP CompanyName / LedgerName.");
        }

        return _excel.CompareEntries(entriesA, entriesB, companyA, companyB, options);
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
        DateTime? dateTo = null)
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

        AppendDateRangeFilter(ref sql, parameters, dateFrom, dateTo);

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
        DateTime? dateTo = null)
    {
        companyA = (companyA ?? "").Trim();
        ledgerA = (ledgerA ?? "").Trim();
        companyB = (companyB ?? "").Trim();
        ledgerB = (ledgerB ?? "").Trim();

        // If ledgers omitted, auto-resolve from the two companies.
        if (ledgerA.Length == 0 || ledgerB.Length == 0)
            return await CompareFromCompaniesAsync(companyA, companyB, options, dateFrom, dateTo);

        if (companyA.Length == 0 || companyB.Length == 0)
            throw new InvalidOperationException("Select Company and Ledger for both sides.");

        var fetchA = FetchBillWiseExactAsync(companyA, ledgerA, dateFrom, dateTo);
        var fetchB = FetchBillWiseExactAsync(companyB, ledgerB, dateFrom, dateTo);
        await Task.WhenAll(fetchA, fetchB);

        var entriesA = await fetchA;
        var entriesB = await fetchB;

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows for '{companyA}' / '{ledgerA}' or '{companyB}' / '{ledgerB}'.");
        }

        return _excel.CompareEntries(entriesA, entriesB, companyA, companyB, options);
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

        if (entriesA.Count == 0 && entriesB.Count == 0)
        {
            throw new InvalidOperationException(
                $"No bill-wise rows found in vw_BillWiseTransactionWithOnAccount for " +
                $"'{companyA}' ↔ '{companyB}' (searched as '{coreA}' ↔ '{coreB}'). " +
                "Try a shorter override like 'HCP Enterprise' / 'Plastene India'.");
        }

        return _excel.CompareEntries(entriesA, entriesB, companyA, companyB, options);
    }

    public async Task<List<LedgerEntryDto>> FetchBillWiseForLedgersAsync(
        string companyName,
        IReadOnlyList<string> ledgerNames,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
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
        AppendDateRangeFilter(ref sql, parameters, dateFrom, dateTo);

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
        DateTime? dateTo = null)
    {
        return await FetchBillWiseForLedgersAsync(companyName, new[] { ledgerName }, dateFrom, dateTo);
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

    private static void AppendDateRangeFilter(
        ref string sql,
        DynamicParameters parameters,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
        if (from == null || toExclusive == null)
            return;

        sql += @"
  AND VoucherDate >= @DateFrom
  AND VoucherDate < @DateToExclusive";
        parameters.Add("DateFrom", from.Value);
        parameters.Add("DateToExclusive", toExclusive.Value);
    }
}
