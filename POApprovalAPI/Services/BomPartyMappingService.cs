using System.Text;
using System.Text.RegularExpressions;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public sealed class BomPartyMappingIndex
{
    public IReadOnlyList<BomPartyGroup> Groups { get; init; } = Array.Empty<BomPartyGroup>();
    public IReadOnlyDictionary<string, string> AliasToDisplay { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> ResolveFilterNames(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Array.Empty<string>();

        var group = Groups.FirstOrDefault(g =>
            string.Equals(g.DisplayName, displayName.Trim(), StringComparison.OrdinalIgnoreCase));
        return group?.Aliases.ToList() ?? [displayName.Trim()];
    }

    public string ResolveDisplayName(string? rawPartyName)
    {
        if (string.IsNullOrWhiteSpace(rawPartyName))
            return "";

        var trimmed = rawPartyName.Trim();
        return AliasToDisplay.TryGetValue(trimmed, out var display) ? display : trimmed;
    }

    public string? ResolveOfficialMasterName(string? rawPartyName)
    {
        if (string.IsNullOrWhiteSpace(rawPartyName))
            return null;

        if (!AliasToDisplay.TryGetValue(rawPartyName.Trim(), out var display))
            display = rawPartyName.Trim();

        var group = Groups.FirstOrDefault(g =>
            string.Equals(g.DisplayName, display, StringComparison.OrdinalIgnoreCase));
        return group?.OfficialName;
    }
}

public sealed class BomPartyGroup
{
    public string DisplayName { get; init; } = "";
    public string? OfficialName { get; init; }
    public bool FromMaster { get; init; }
    public string MappingType { get; init; } = "";
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
}

public static class BomPartyMappingService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "THE", "AND", "FOR", "CO", "CORP", "CORPORATION", "COMPANY", "GROUP", "PACKAGING",
        "PACK", "BAG", "BAGS", "FIBC", "FLEXIBLE", "FLEXIBLES", "PRODUCTS", "SERVICES",
        "INTERNATIONAL", "INDUSTRIES", "ENTERPRISES", "TRADING", "EXPORT", "IMPORT",
        "PRIVATE", "LIMITED", "LTD", "LLC", "LIC", "INC", "PVT", "SA", "SL", "GMBH",
        "BV", "AB", "AS", "SRL", "SAS", "SARL", "NV", "SPA", "PLC", "PTY", "COM",
    };

    private static readonly string[] LegalSuffixPattern =
    [
        @"\bGROUP\b", @"\bPRIVATE\b", @"\bLIMITED\b", @"\bLTD\.?\b", @"\bLLC\.?\b", @"\bLIC\b",
        @"\bINC\.?\b", @"\bPVT\.?\b", @"\bCORP\.?\b", @"\bCORPORATION\b", @"\bCO\.?\b",
        @"\bS\.?A\.?\b", @"\bS\.?L\.?\b", @"\bGMBH\b", @"\bB\.?V\.?\b", @"\bAB\b", @"\bA/S\b",
        @"\bSRL\b", @"\bSAS\b", @"\bSARL\b", @"\bNV\b", @"\bSPA\b", @"\bPLC\b", @"\bPTY\b",
        @"\.COM\b", @"\bCOM\b",
    ];

    public static BomPartyMappingIndex Build(IReadOnlyList<BomCustomerOption> rawCustomers)
    {
        var masters = rawCustomers
            .Where(c => c.FromMaster && !string.IsNullOrWhiteSpace(c.CompanyName))
            .Select(c => c.CompanyName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var masterSet = new HashSet<string>(masters, StringComparer.OrdinalIgnoreCase);

        var bomOnly = rawCustomers
            .Where(c => !c.FromMaster && !string.IsNullOrWhiteSpace(c.CompanyName))
            .Select(c => c.CompanyName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allRaw = masters.Concat(bomOnly).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var groups = new Dictionary<string, PartyGroupBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var official in masters)
        {
            groups[official] = PartyGroupBuilder.ForOfficial(official);
        }

        var unassigned = new List<string>();

        foreach (var raw in bomOnly)
        {
            if (masterSet.Contains(raw))
                continue;

            var official = TryResolveOfficial(raw, masters);
            if (official is not null)
            {
                if (!groups.ContainsKey(official))
                    groups[official] = PartyGroupBuilder.ForOfficial(official);
                groups[official].AddAlias(raw, "Region");
                continue;
            }

            unassigned.Add(raw);
        }

        var clusterBuckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in unassigned)
        {
            var key = ClusterKey(raw);
            if (!clusterBuckets.TryGetValue(key, out var list))
            {
                list = [];
                clusterBuckets[key] = list;
            }
            list.Add(raw);
        }

        MergeSimilarClusterKeys(clusterBuckets);

        foreach (var (clusterKey, members) in clusterBuckets)
        {
            if (members.Count == 0)
                continue;

            if (members.Count == 1)
            {
                var only = members[0];
                if (!groups.ContainsKey(only))
                {
                    var singleton = PartyGroupBuilder.ForSingleton(only);
                    singleton.AddAlias(only, "Singleton");
                    groups[only] = singleton;
                }

                continue;
            }

            var display = PickClusterDisplayName(clusterKey, members);
            if (groups.ContainsKey(display))
            {
                foreach (var member in members)
                    groups[display].AddAlias(member, "Cluster");
            }
            else
            {
                var builder = PartyGroupBuilder.ForCluster(display);
                foreach (var member in members)
                    builder.AddAlias(member, "Cluster");
                groups[display] = builder;
            }
        }

        foreach (var official in masters)
        {
            if (groups.TryGetValue(official, out var builder))
                builder.AddAlias(official, "Official");
        }

        var builtGroups = groups.Values
            .Select(b => b.Build())
            .Where(g => g.Aliases.Count > 0)
            .OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var aliasToDisplay = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in builtGroups)
        {
            foreach (var alias in group.Aliases)
                aliasToDisplay[alias] = group.DisplayName;
        }

        return new BomPartyMappingIndex
        {
            Groups = builtGroups,
            AliasToDisplay = aliasToDisplay,
        };
    }

    private sealed class PartyGroupBuilder
    {
        private readonly HashSet<string> _aliases = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _types = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _displayName;
        private readonly string? _officialName;
        private readonly bool _fromMaster;

        private PartyGroupBuilder(string displayName, string? officialName, bool fromMaster)
        {
            _displayName = displayName;
            _officialName = officialName;
            _fromMaster = fromMaster;
        }

        public static PartyGroupBuilder ForOfficial(string official) =>
            new(official, official, fromMaster: true);

        public static PartyGroupBuilder ForCluster(string display) =>
            new(display, officialName: null, fromMaster: false);

        public static PartyGroupBuilder ForSingleton(string name) =>
            new(name, officialName: null, fromMaster: false);

        public void AddAlias(string alias, string type)
        {
            if (string.IsNullOrWhiteSpace(alias))
                return;
            _aliases.Add(alias.Trim());
            _types.Add(type);
        }

        public BomPartyGroup Build()
        {
            var mappingType = _fromMaster
                ? (_types.Contains("Region") ? "Official+Region" : "Official")
                : (_types.Contains("Cluster") ? "Cluster" : "Singleton");

            return new BomPartyGroup
            {
                DisplayName = _displayName,
                OfficialName = _officialName,
                FromMaster = _fromMaster,
                MappingType = mappingType,
                Aliases = _aliases.OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToList(),
            };
        }
    }

    private static string? TryResolveOfficial(string raw, IReadOnlyList<string> masters)
    {
        var lower = raw.ToLowerInvariant();

        if (TryEmbeddedBrandOfficial(raw, out var embedded))
            return FindMaster(masters, embedded);

        var regionOfficial = TryRegionalOfficial(lower, masters);
        if (regionOfficial is not null)
            return regionOfficial;

        if (TryExplicitAlias(lower, out var aliasOfficial))
            return FindMaster(masters, aliasOfficial);

        return null;
    }

    private static bool TryEmbeddedBrandOfficial(string raw, out string brand)
    {
        brand = "";
        var match = Regex.Match(raw, @"\(([^)]+)\)");
        if (match.Success)
        {
            var inner = match.Groups[1].Value.Trim();
            if (inner.Length >= 4 && !inner.Equals("RED", StringComparison.OrdinalIgnoreCase))
            {
                brand = inner;
                return true;
            }
        }

        foreach (var token in new[] { "ALMATIS", "GREIF", "NNZ", "CESUR", "BOXON", "STORSACK" })
        {
            if (raw.Contains(token, StringComparison.OrdinalIgnoreCase)
                && !raw.Trim().Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                brand = token;
                return true;
            }
        }

        return false;
    }

    private static string? TryRegionalOfficial(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("greif"))
            return MatchGreif(lower, masters);
        if (lower.Contains("lc packaging") || lower.Contains("l c packaging"))
            return MatchLcPackaging(lower, masters);
        if (lower.Contains("cesur"))
            return MatchCesur(lower, masters);
        if (Regex.IsMatch(lower, @"\bnnz\b"))
            return MatchNnz(lower, masters);
        if (lower.Contains("storsack"))
            return MatchStorsack(lower, masters);
        if (lower.Contains("boxon"))
            return MatchBoxon(lower, masters);
        return null;
    }

    private static bool TryExplicitAlias(string lower, out string official)
    {
        official = "";
        var norm = NormalizeLoose(lower);

        if (norm.StartsWith("procon"))
        {
            official = "PROCON PACIFIC LLC";
            return true;
        }

        if (norm.StartsWith("bolpamur"))
        {
            official = "BOLPAMUR";
            return true;
        }

        if (norm.StartsWith("baobag") || norm.Contains("sas baobag"))
        {
            official = "Baobag";
            return true;
        }

        if (norm.Contains("globalpak") || norm.Contains("global pak"))
        {
            official = "GLOBAL PAK";
            return true;
        }

        if (norm is "cargill" or "cargil")
        {
            official = "Cargill.com";
            return true;
        }

        if (norm.Contains("alliedpotato") || norm.Contains("aliiedpotato"))
        {
            official = "";
            return false;
        }

        return false;
    }

    private static string ClusterKey(string raw)
    {
        var known = KnownClusterOverride(raw);
        if (known is not null)
            return known;

        if (TryEmbeddedBrandOfficial(raw, out var embedded))
            return NormalizeLoose(embedded).ToUpperInvariant();

        var stripped = StripLegalSuffixes(raw);
        var tokens = SignificantTokens(stripped);
        if (tokens.Count == 0)
            return NormalizeLoose(raw);

        if (tokens.Count >= 2 && IsPersonLike(raw))
            return $"{tokens[0]} {tokens[1]}";

        return tokens[0].ToUpperInvariant();
    }

    private static string? KnownClusterOverride(string raw)
    {
        var loose = NormalizeLoose(raw);
        if (loose.Contains("alliedpotato") || loose.Contains("aliiedpotato"))
            return "ALLIEDPOTATO";
        if (loose.Contains("alkimia"))
            return "ALKIMIA";
        if (loose.Contains("almatis"))
            return "ALMATIS";
        if (loose.StartsWith("allcomp"))
            return "ALLCOMP";
        if (loose.Contains("allroundpackaging"))
            return "ALLROUND";
        if (loose.StartsWith("almega"))
            return "ALMEGA";
        if (Regex.IsMatch(loose, @"allis+a"))
            return "ALLISA";
        return null;
    }

    private static bool IsPersonLike(string raw)
    {
        var tokens = Tokenize(raw);
        if (tokens.Count != 2)
            return false;
        return tokens[1].Equals("ibrahim", StringComparison.OrdinalIgnoreCase)
            || tokens[1].Equals("ji", StringComparison.OrdinalIgnoreCase)
            || tokens[1].Equals("sir", StringComparison.OrdinalIgnoreCase)
            || tokens[1].Equals("bhai", StringComparison.OrdinalIgnoreCase);
    }

    private static void MergeSimilarClusterKeys(Dictionary<string, List<string>> buckets)
    {
        // Only merge keys in small prefix buckets to avoid O(n²) over thousands of keys.
        const int maxBucketSize = 40;
        var prefixGroups = buckets.Keys
            .GroupBy(k => k.Length >= 3 ? k[..3].ToUpperInvariant() : k.ToUpperInvariant())
            .ToList();

        foreach (var prefixGroup in prefixGroups)
        {
            var keys = prefixGroup.ToList();
            if (keys.Count > maxBucketSize)
                continue;

            var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < keys.Count; i++)
            {
                if (merged.Contains(keys[i]))
                    continue;

                for (var j = i + 1; j < keys.Count; j++)
                {
                    if (merged.Contains(keys[j]))
                        continue;

                    if (ShouldMergeClusterKeys(keys[i], keys[j]))
                    {
                        buckets[keys[i]].AddRange(buckets[keys[j]]);
                        buckets.Remove(keys[j]);
                        merged.Add(keys[j]);
                    }
                }
            }
        }
    }

    private static bool ShouldMergeClusterKeys(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;

        if (a.Length >= 4 && b.Length >= 4)
        {
            var dist = LevenshteinDistance(a.ToUpperInvariant(), b.ToUpperInvariant());
            if (dist <= 1)
                return true;
        }

        return false;
    }

    private static string PickClusterDisplayName(string clusterKey, IReadOnlyList<string> members)
    {
        var ranked = members
            .OrderByDescending(m => ScoreDisplayCandidate(m, clusterKey))
            .ToList();
        return ranked[0];
    }

    private static int ScoreDisplayCandidate(string name, string clusterKey)
    {
        var score = 0;
        var stripped = StripLegalSuffixes(name);
        if (stripped.Length >= clusterKey.Length)
            score += 5;
        if (!name.Equals(name.ToUpperInvariant()))
            score += 2;
        if (!Regex.IsMatch(name, @"\b(GROUP|LTD|LLC|INC|PVT|PRIVATE|LIMITED|S\.?A\.?|S\.?L\.?)\b", RegexOptions.IgnoreCase))
            score += 3;
        score += Math.Min(stripped.Length, 20);
        return score;
    }

    private static string? MatchGreif(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("benelux")) return FindMaster(masters, "GREIF BENELUX");
        if (lower.Contains("france")) return FindMaster(masters, "Greif France");
        if (lower.Contains("germany")) return FindMaster(masters, "GREIF GERMANY");
        if (lower.Contains("italy")) return FindMaster(masters, "Greif Italy");
        if (lower.Contains("ireland")) return FindMaster(masters, "Greif Ireland");
        if (lower.Contains("portugal") || lower.Contains("iberia")) return FindMaster(masters, "Greif Portugal");
        if (lower.Contains("turkey")) return FindMaster(masters, "Greif Turkey");
        if (lower.Contains("australia")) return FindMaster(masters, "GREIF FLEXIBLE AUSTRALIA");
        if (lower.Contains("sweden")) return FindMaster(masters, "Greif Flexible Sweden AB");
        if (lower.Contains("finland")) return FindMaster(masters, "GREIF FLEXILBE FINLAND");
        if (Regex.IsMatch(lower, @"\buk\b")) return FindMaster(masters, "Greif Flexibles UK Limited");
        if (lower.Contains("usa") || lower.Contains("america") || lower.Contains("u.s"))
            return FindMaster(masters, "GREIF USA");
        if (lower.Contains("chile") || lower.Contains("chille")) return FindMaster(masters, "Greif Chille");
        if (lower.Contains("netherland")) return FindMaster(masters, "Greif Netherland");
        if (lower.Contains("china") || lower.Contains("vietnam") || lower.Contains("mexico") || lower.Contains("spain"))
            return FindMaster(masters, "GREIF");
        return FindMaster(masters, "GREIF");
    }

    private static string? MatchLcPackaging(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("france")) return FindMaster(masters, "LC Packaging France");
        if (lower.Contains("spain")) return FindMaster(masters, "LC Packaging Spain");
        if (lower.Contains("netherland")) return FindMaster(masters, "LC Packaging Netherland");
        if (Regex.IsMatch(lower, @"\buk\b")) return FindMaster(masters, "L C PACKAGING UK LTD");
        if (lower.Contains("africa")) return FindMaster(masters, "LC Packaging Africa");
        return FindMaster(masters, "LC Packaging International");
    }

    private static string? MatchCesur(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("benelux") || lower.Contains("belgium")) return FindMaster(masters, "Cesur Benelux BV");
        if (Regex.IsMatch(lower, @"\buk\b")) return FindMaster(masters, "CESUR PACKAGING (UK) LIMITED");
        if (lower.Contains("usa")) return FindMaster(masters, "Cesur USA");
        return FindMaster(masters, "Cesur Packaging");
    }

    private static string? MatchNnz(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("denmark")) return FindMaster(masters, "NNZ Denmark");
        if (lower.Contains("germany") || lower.Contains("gmbh")) return FindMaster(masters, "NNZ GERMANY");
        if (lower.Contains("italy")) return FindMaster(masters, "NNZ Italy");
        if (lower.Contains("poland")) return FindMaster(masters, "NNZ POLAND");
        if (lower.Contains("switzerland")) return FindMaster(masters, "NNZ Switzerland");
        if (lower.Contains("usa") || lower.Contains("america")) return FindMaster(masters, "NNZ USA");
        return FindMaster(masters, "NNZ bv");
    }

    private static string? MatchStorsack(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("austria")) return FindMaster(masters, "Storsack Austria");
        if (lower.Contains("france")) return FindMaster(masters, "Storsack France");
        if (lower.Contains("germany")) return FindMaster(masters, "STORSACK GERMANY");
        if (lower.Contains("netherland")) return FindMaster(masters, "Storsack Netherlands");
        if (lower.Contains("nordic")) return FindMaster(masters, "Storsack Nordic AB");
        if (lower.Contains("usa") || lower.Contains("america")) return FindMaster(masters, "Storsack USA");
        return FindMaster(masters, "Storsack");
    }

    private static string? MatchBoxon(string lower, IReadOnlyList<string> masters)
    {
        if (lower.Contains("france") || lower.Contains("sarl")) return FindMaster(masters, "Boxon Bag France");
        if (lower.Contains("germany") || lower.Contains("gmbh")) return FindMaster(masters, "Boxon Bag Germany");
        return FindMaster(masters, "Boxon Bags AB");
    }

    private static string? FindMaster(IReadOnlyList<string> masters, string target)
    {
        return masters.FirstOrDefault(m => m.Equals(target, StringComparison.OrdinalIgnoreCase))
            ?? masters.FirstOrDefault(m => m.Contains(target, StringComparison.OrdinalIgnoreCase))
            ?? masters.FirstOrDefault(m => target.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> SignificantTokens(string value)
    {
        return Tokenize(StripLegalSuffixes(value))
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> Tokenize(string value)
    {
        return Regex.Split(value.Trim(), @"[^A-Za-z0-9]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static string StripLegalSuffixes(string value)
    {
        var result = value.Trim();
        foreach (var pattern in LegalSuffixPattern)
            result = Regex.Replace(result, pattern, " ", RegexOptions.IgnoreCase);
        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    private static string NormalizeLoose(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var costs = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            costs[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            costs[0] = i;
            var last = i - 1;
            for (var j = 1; j <= b.Length; j++)
            {
                var temp = costs[j];
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), last + cost);
                last = temp;
            }
        }

        return costs[b.Length];
    }
}
