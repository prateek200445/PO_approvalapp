using System.Text.RegularExpressions;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// DB-backed resolution of company, ledger party, and vendor names from messy NL questions.
/// Aliases and regex produce hints; FactoryInfo / LedgerMaster / Vendor confirm canonical names.
/// </summary>
public class ChatEntityResolutionService
{
    private readonly DatabaseService _database;
    private readonly ILogger<ChatEntityResolutionService> _logger;

    private static readonly string[] IntentNoisePatterns =
    [
        @"\b(?:show|list|get|give\s+me|find|what(?:'s| is)?)\b",
        @"\b(?:ledger\s+)?(?:statement|summary|account\s+statement)\b",
        @"\b(?:voucher\s+(?:history|details|wise)|show\s+vouchers|list\s+vouchers)\b",
        @"\b(?:transaction\s+history|ledger\s+transactions)\b",
        @"\b(?:opening|pending|outstanding)\s+(?:balance|bal)?\b",
        @"\b(?:ageing|aging|overdue|debtor|creditor)\b",
        @"\b(?:fy|financial\s+year)\s+[\d\-/–]+",
        @"\b(?:this|current)\s+year\b",
        @"\b(?:for|of|at|against|from|to|the|please|customer|buyer|vendor|supplier|party|ledger)\b",
    ];

    public ChatEntityResolutionService(
        DatabaseService database,
        ILogger<ChatEntityResolutionService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ChatEntityContext> ResolveAsync(string message, CancellationToken ct = default)
    {
        var company = await ResolveCompanyAsync(message, ct);
        var ledgerParty = company is not null
            ? await ResolveLedgerPartyAsync(message, company.Name, ct)
            : null;
        var vendorFirm = await ResolveVendorFirmAsync(message, ct);

        _logger.LogDebug(
            "Entity resolution: company={Company} party={Party} vendor={Vendor}",
            company?.Name ?? "-",
            ledgerParty?.LedgerName ?? "-",
            vendorFirm?.FirmName ?? "-");

        return new ChatEntityContext
        {
            Message = message,
            Company = company,
            LedgerParty = ledgerParty,
            VendorFirm = vendorFirm,
        };
    }

    public async Task<ResolvedCompany?> ResolveCompanyAsync(string message, CancellationToken ct = default)
    {
        var alias = ResolveOutwardCompanyAlias(message);
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var id = await LookupCompanyIdByNameAsync(alias, ct);
            if (id > 0)
                return new ResolvedCompany { Name = alias, CompanyId = id, Source = "alias" };
        }

        foreach (var hint in ExtractCompanyHints(message))
        {
            var row = await LookupCompanyByHintAsync(hint, ct);
            if (row is not null)
                return new ResolvedCompany { Name = row.Value.Name, CompanyId = row.Value.Id, Source = "db-hint" };
        }

        return null;
    }

    public async Task<ResolvedLedgerParty?> ResolveLedgerPartyAsync(
        string message,
        string companyName,
        CancellationToken ct = default)
    {
        foreach (var hint in ExtractPartyHints(message, companyName))
        {
            var ledger = await LookupLedgerByHintAsync(companyName, hint, ct);
            if (ledger is not null)
                return new ResolvedLedgerParty
                {
                    LedgerName = ledger,
                    CompanyName = companyName,
                    Source = "db-hint",
                };
        }

        return null;
    }

    public async Task<ResolvedVendorFirm?> ResolveVendorFirmAsync(string message, CancellationToken ct = default)
    {
        if (!LooksLikeVendorContext(message)) return null;

        foreach (var hint in ExtractVendorHints(message))
        {
            if (hint.Length < 3) continue;
            var db = await LookupVendorByHintAsync(hint, ct);
            if (db is not null)
                return new ResolvedVendorFirm { FirmName = db, Source = "db-hint" };
        }

        return null;
    }

    internal static string? ResolveOutwardCompanyAlias(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("oswal")) return "Oswal Extrusion Limited";
        if ((m.Contains("k.p") || m.Contains("kp ") || m.Contains("kp woven") || m.Contains("kpwoven"))
            && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        if (Regex.IsMatch(m, @"\bkp\b") && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        if (m.Contains("polyfilms") || m.Contains("ppl"))
            return "Plastene Polyfilms Limited";
        if (m.Contains("bulkpack") || m.Contains("hcp plastene"))
            return "HCP Plastene Bulkpack Ltd";
        if (m.Contains("plastene india") && m.Contains("unit"))
            return "Plastene India Limited (Unit -II)";
        if (m.Contains("plastene india")) return "Plastene India Limited";
        return null;
    }

    internal static bool NamesLooselyMatch(string a, string b)
    {
        static string Norm(string s) => Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "");
        var na = Norm(a);
        var nb = Norm(b);
        if (na.Length < 3 || nb.Length < 3) return false;
        return na == nb || na.Contains(nb) || nb.Contains(na);
    }

    private static bool LooksLikeVendorContext(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("vendor") || m.Contains("supplier")
               || m.Contains("firm name") || m.Contains("vendor code")
               || m.Contains("vendor rate") || m.Contains("bright rubber")
               || m.Contains("chemline") || m.Contains("lohia");
    }

    private async Task<int> LookupCompanyIdByNameAsync(string name, CancellationToken ct)
    {
        await using var conn = _database.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT SrNo FROM FactoryInfo WITH (NOLOCK) WHERE Name = @Name",
            new { Name = name.Trim() });
    }

    private async Task<(int Id, string Name)?> LookupCompanyByHintAsync(string hint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hint) || hint.Length < 3) return null;
        var like = $"%{hint.Trim()}%";
        await using var conn = _database.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<(int SrNo, string Name)>(
            """
            SELECT TOP 1 SrNo, Name
            FROM FactoryInfo WITH (NOLOCK)
            WHERE ISNULL(Name, '') <> ''
              AND (Name LIKE @Like OR GroupName LIKE @Like)
            ORDER BY
              CASE WHEN LOWER(Name) = LOWER(@Exact) THEN 0
                   WHEN Name LIKE @Prefix THEN 1
                   ELSE 2 END,
              LEN(Name)
            """,
            new { Like = like, Exact = hint.Trim(), Prefix = $"{hint.Trim()}%" });
        return row.Name is { Length: > 0 } ? (row.SrNo, row.Name.Trim()) : null;
    }

    private async Task<string?> LookupLedgerByHintAsync(string companyName, string hint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hint) || hint.Length < 3) return null;
        var like = $"%{hint.Trim()}%";
        await using var conn = _database.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>(
            """
            SELECT TOP 1 LedgerName
            FROM LedgerMaster WITH (NOLOCK)
            WHERE CompanyName = @Company
              AND ISNULL(LedgerName, '') <> ''
              AND LedgerName LIKE @Like
            ORDER BY
              CASE WHEN LOWER(LedgerName) = LOWER(@Exact) THEN 0
                   WHEN LedgerName LIKE @Prefix THEN 1
                   ELSE 2 END,
              LEN(LedgerName)
            """,
            new
            {
                Company = companyName.Trim(),
                Like = like,
                Exact = hint.Trim(),
                Prefix = $"{hint.Trim()}%",
            });
    }

    private async Task<string?> LookupVendorByHintAsync(string hint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hint) || hint.Length < 3) return null;
        var like = $"%{hint.Trim()}%";
        await using var conn = _database.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>(
            """
            SELECT TOP 1 FirmName
            FROM Vendor WITH (NOLOCK)
            WHERE ISNULL(FirmName, '') <> ''
              AND FirmName LIKE @Like
            ORDER BY
              CASE WHEN LOWER(FirmName) = LOWER(@Exact) THEN 0
                   WHEN FirmName LIKE @Prefix THEN 1
                   ELSE 2 END,
              LEN(FirmName)
            """,
            new { Like = like, Exact = hint.Trim(), Prefix = $"{hint.Trim()}%" });
    }

    internal static IEnumerable<string> ExtractCompanyHints(string message)
    {
        var hints = new List<string>();
        foreach (Match m in Regex.Matches(
                     message,
                     @"\b([A-Za-z0-9][A-Za-z0-9 .,&\-()']*?(?:Limited|Ltd|Pvt\.?\s*Ltd|Private Limited))\b",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        foreach (Match nm in Regex.Matches(
                     message,
                     @"\b(plastene\s+\w+|oswal\s+\w+|kp\s+woven|polyfilms|bulkpack)\b",
                     RegexOptions.IgnoreCase))
            hints.Add(nm.Groups[1].Value.Trim());

        var scrubbed = ScrubIntentNoise(message);
        foreach (Match seg in Regex.Matches(
                     scrubbed,
                     @"\b(?:at|for)\s+([a-z0-9 .,&\-']{4,60})",
                     RegexOptions.IgnoreCase))
            hints.Add(seg.Groups[1].Value.Trim());

        return hints
            .Select(NormalizeHint)
            .Where(h => h.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8);
    }

    internal static IEnumerable<string> ExtractPartyHints(string message, string? companyName)
    {
        var hints = new List<string>();
        var scrubbed = ScrubIntentNoise(message);

        foreach (Match m in Regex.Matches(
                     message,
                     @"\b(?:pe|par|mein)\s+(.+?)\s+ka\s+(?:kitna|kya)?",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        foreach (Match m in Regex.Matches(
                     message,
                     @"\b(?:show|list|get|give\s+me)\s+vouchers\s+(?:for|of)\s+(?:the\s+)?(.+)$",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        foreach (Match m in Regex.Matches(
                     message,
                     @"\b(?:ledger\s+)?(?:statement|summary|account\s+statement|transaction\s+history|voucher\s+(?:history|details|wise))\s+(?:for|of)\s+(?:the\s+)?(.+)$",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        foreach (Match m in Regex.Matches(
                     scrubbed,
                     @"\b(?:for|of|customer|buyer|vendor|supplier|party)\s+(.+)$",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        return hints
            .Select(h => StripCompanyFromHint(h, companyName ?? ResolveOutwardCompanyAlias(message)))
            .Select(NormalizePartyHint)
            .Where(h => h.Length >= 3 && !LooksLikeCompanyOnly(h, companyName) && !IsGarbagePartyHint(h))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10);
    }

    internal static string NormalizePartyHint(string hint)
    {
        var s = NormalizeHint(hint);
        s = Regex.Replace(s, @"\s+(?:ka|ke|ki|ko|kitna|kya|hai|hain|please|batao|dikhao).*$", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"^(?:pe|par|mein)\s+", "", RegexOptions.IgnoreCase);
        return s.Trim();
    }

    internal static bool IsGarbagePartyHint(string party)
    {
        var m = party.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(m) || m.Length < 3) return true;
        return m.Contains("kitna")
               || m.Contains("kya")
               || m.Contains(" pending")
               || m.Contains(" balance")
               || m.Contains(" outstanding")
               || Regex.IsMatch(m, @"\b(?:polyfilms|oswal|plastene)\s+pe\b")
               || Regex.IsMatch(m, @"\bpe\s+(?:polyfilms|oswal|plastene)\b");
    }

    private static IEnumerable<string> ExtractVendorHints(string message)
    {
        var hints = new List<string>();
        foreach (Match m in Regex.Matches(
                     message,
                     @"\b(?:vendor|supplier)\s+(.+?)(?:\s+(?:rate|code|gst|pan|profile|bank)|\?|$)",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        foreach (Match m in Regex.Matches(
                     message,
                     @"\b(?:for|from|of)\s+(?:vendor\s+)?(.+?)(?:\s+(?:with|at|and|show|list)|\?|$)",
                     RegexOptions.IgnoreCase))
            hints.Add(m.Groups[1].Value.Trim());

        return hints
            .Select(NormalizeHint)
            .Where(h => h.Length >= 3
                        && !h.Equals("vendor", StringComparison.OrdinalIgnoreCase)
                        && !h.Equals("supplier", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6);
    }

    private static string ScrubIntentNoise(string message)
    {
        var s = message;
        foreach (var pat in IntentNoisePatterns)
            s = Regex.Replace(s, pat, " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\s+(?:fy|financial\s+year)\s+[\d\-/–]+.*$", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\s+(?:this|current)\s+year\s*$", "", RegexOptions.IgnoreCase);
        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private static string StripCompanyFromHint(string hint, string? companyName)
    {
        var s = hint.Trim();
        if (string.IsNullOrWhiteSpace(companyName)) return s;

        s = Regex.Replace(
            s,
            $@"\s+(?:at|for)\s+{Regex.Escape(companyName)}\s*$",
            "",
            RegexOptions.IgnoreCase);

        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var take = Math.Min(6, words.Length - 1); take >= 1; take--)
        {
            var tail = string.Join(' ', words[^take..]);
            if (ResolveOutwardCompanyAlias(tail) == companyName
                || NamesLooselyMatch(tail, companyName))
                return string.Join(' ', words[..^take]).Trim();
        }

        s = Regex.Replace(
            s,
            @"\s+(?:at|for)\s+(?:the\s+)?[A-Za-z0-9][A-Za-z0-9 .,&\-()']*?(?:Limited|Ltd|Pvt|Private)(?:\s*\([^)]+\))?\s*$",
            "",
            RegexOptions.IgnoreCase);

        return s.Trim();
    }

    private static bool LooksLikeCompanyOnly(string hint, string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName)) return false;
        return NamesLooselyMatch(hint, companyName)
               || ResolveOutwardCompanyAlias(hint) == companyName;
    }

    private static string NormalizeHint(string hint) =>
        hint.Trim().TrimEnd('.', ',', ';', '?', '!');
}
