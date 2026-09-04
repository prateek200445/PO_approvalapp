using System.Globalization;
using System.Text.RegularExpressions;
using POApprovalAPI.Planning.Bom;
using POApprovalAPI.Planning.Fibc.Models;

namespace POApprovalAPI.Planning.Fibc;

internal static class FibcStitchSpecResolver
{
    private static readonly Dictionary<string, string> ActivityLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["orsan_u2"] = "ORSAN U+2",
        ["orsan_4panel"] = "ORSAN 4-panel",
        ["bottom_4panel"] = "Bottom 4-panel",
        ["tubular"] = "Tubular base",
        ["baffle"] = "Baffle stitching",
        ["open_top"] = "Open top",
        ["flap_top"] = "Flap top",
        ["duffle"] = "Duffle",
        ["top_spout"] = "Top spout",
        ["bottom_round_25_50"] = "Bottom round",
        ["liner_hem_spout"] = "Liner hem to spout",
        ["liner_hem_duffle"] = "Liner hem to duffle",
        ["skirt"] = "Skirt",
        ["belt"] = "Cross-corner belt",
    };

    public static FibcStitchSpecDto Resolve(
        string companyName,
        string erpFamily,
        string dustLevel,
        FibcOrderAllotmentContextDto? context)
    {
        var catalog = FibcStitchTargetCatalog.Instance;
        var factoryKey = catalog.ResolveFactoryKey(companyName);
        var warnings = new List<string>();
        if (catalog.Factories.Count == 0)
        {
            warnings.Add("Excel stitch-target catalog was not found; using line Bagcapacity.");
            return Empty(factoryKey, warnings);
        }

        var sizeH = context?.SizeHCm;
        var heightBand = HeightBandFromCm(sizeH);
        if (sizeH is null or <= 0)
            warnings.Add("BOM SizeH was not found; using the 90–120 cm height band.");

        var dustColumn = DustColumn(dustLevel, context?.BagType ?? erpFamily);
        var headings = context?.BomHeadings ?? Array.Empty<string>();
        var categories = headings
            .Select(h => BomComponentClassifier.Classify(h, null, null, null, null).Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var jobs = new List<JobDraft>();
        AddBodyJobs(jobs, erpFamily);
        AddTopBottomJobs(jobs, headings, categories);
        AddAccessoryJobs(jobs, headings, categories);

        var resolved = new List<FibcStitchJobRateDto>();
        foreach (var job in jobs)
        {
            var lookupDust = job.ActivityKey == "belt" ? "uncoated" : dustColumn;
            var lookupHeight = job.ActivityKey == "belt" ? "h50" : heightBand;
            if (!catalog.TryGetRate(factoryKey, job.ActivityKey, lookupHeight, lookupDust, out var pcs))
            {
                warnings.Add($"No Excel target for {Label(job.ActivityKey)} at {factoryKey}/{lookupHeight}.");
                continue;
            }

            var bags = Math.Max(1, pcs / Math.Max(1, job.PiecesPerBag));
            resolved.Add(new FibcStitchJobRateDto
            {
                ActivityKey = job.ActivityKey,
                ActivityLabel = Label(job.ActivityKey),
                PcsPerShift = pcs,
                PiecesPerBag = job.PiecesPerBag,
                BagsPerShift = bags,
                AffectsBottleneck = job.AffectsBottleneck,
            });
        }

        var bottleneckJobs = resolved.Where(j => j.AffectsBottleneck && j.BagsPerShift > 0).ToList();
        if (bottleneckJobs.Count == 0)
        {
            warnings.Add("Could not match Excel stitch jobs for this spec; using line Bagcapacity.");
            return new FibcStitchSpecDto
            {
                FactoryKey = factoryKey,
                FactoryLabel = FactoryLabel(factoryKey),
                HeightBand = heightBand,
                SizeHCm = sizeH,
                DustColumn = dustColumn,
                UsedExcelTargets = false,
                Jobs = resolved,
                Warnings = warnings,
            };
        }

        var bottleneck = bottleneckJobs.MinBy(j => j.BagsPerShift)!;
        var lot = LcmMany(bottleneckJobs.Select(j => j.BagsPerShift));
        var parallel = resolved.Where(j => !j.AffectsBottleneck && j.BagsPerShift > 0 && j.BagsPerShift < bottleneck.BagsPerShift);
        foreach (var job in parallel)
        {
            warnings.Add(
                $"{job.ActivityLabel} is {job.BagsPerShift} bags/shift (from {job.PcsPerShift} pcs) — slower than the line bottleneck, but treated as a parallel station, not slot capacity.");
        }

        return new FibcStitchSpecDto
        {
            FactoryKey = factoryKey,
            FactoryLabel = FactoryLabel(factoryKey),
            HeightBand = heightBand,
            SizeHCm = sizeH,
            DustColumn = dustColumn,
            BottleneckBagsPerShift = bottleneck.BagsPerShift,
            AssignmentLotPcs = lot,
            BottleneckActivity = bottleneck.ActivityLabel,
            UsedExcelTargets = true,
            Jobs = resolved,
            Warnings = warnings,
        };
    }

    public static int EffectiveShiftCapacity(int lineCapacity, FibcStitchSpecDto? spec)
    {
        if (spec is { UsedExcelTargets: true, BottleneckBagsPerShift: > 0 })
        {
            if (lineCapacity > 0)
                return Math.Min(lineCapacity, spec.BottleneckBagsPerShift);
            return spec.BottleneckBagsPerShift;
        }

        return lineCapacity;
    }

    public static double? ParseSizeHCm(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var matches = Regex.Matches(raw, @"\d+(?:\.\d+)?");
        if (matches.Count == 0)
            return null;

        var last = matches[^1].Value;
        if (!double.TryParse(last, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return null;

        return value > 0 ? value : null;
    }

    private static void AddBodyJobs(List<JobDraft> jobs, string erpFamily)
    {
        var family = BagTypeMapper.NormalizeErpFamily(erpFamily);
        if (family.Equals("Circular", StringComparison.OrdinalIgnoreCase))
        {
            jobs.Add(Job("tubular", bottleneck: true));
            return;
        }

        if (family.Equals("4-panel", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("4-panel", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("4panel", StringComparison.OrdinalIgnoreCase))
        {
            jobs.Add(Job("orsan_4panel", bottleneck: true));
            jobs.Add(Job("bottom_4panel", bottleneck: true));
            return;
        }

        if (family.Equals("Buffle", StringComparison.OrdinalIgnoreCase))
        {
            jobs.Add(Job("orsan_u2", bottleneck: true));
            jobs.Add(Job("baffle", bottleneck: true));
            return;
        }

        jobs.Add(Job("orsan_u2", bottleneck: true));
    }

    private static void AddTopBottomJobs(List<JobDraft> jobs, IReadOnlyList<string> headings, HashSet<string> categories)
    {
        var joined = string.Join(" ", headings).ToUpperInvariant();
        var hasSpout = categories.Contains("Spout") || joined.Contains("SPOUT");
        var hasDuffle = categories.Contains("Duffle") || joined.Contains("DUFFLE") || joined.Contains("DUFFEL");
        var hasFlap = categories.Contains("Flap") || joined.Contains("FLAP");
        var hasBottomSpout = headings.Any(h =>
        {
            var n = BomComponentClassifier.NormalizeHeading(h).ToUpperInvariant();
            return n.Contains("BOTTOM") && (n.Contains("SPOUT") || n.Contains("OUTLET") || n.Contains("ROUND"));
        });
        var hasTopSpout = headings.Any(h =>
        {
            var n = BomComponentClassifier.NormalizeHeading(h).ToUpperInvariant();
            return (n.Contains("TOP") && n.Contains("SPOUT")) || (n.Contains("SPOUT") && !n.Contains("BOTTOM"));
        });

        if (hasDuffle)
            jobs.Add(Job("duffle", bottleneck: true));
        else if (hasFlap)
            jobs.Add(Job("flap_top", bottleneck: true));
        else if (hasTopSpout || (hasSpout && !hasBottomSpout))
            jobs.Add(Job("top_spout", bottleneck: true));
        else if (categories.Contains("Top") && !hasSpout)
            jobs.Add(Job("open_top", bottleneck: true));

        if (hasBottomSpout)
            jobs.Add(Job("bottom_round_25_50", bottleneck: true));
    }

    private static void AddAccessoryJobs(List<JobDraft> jobs, IReadOnlyList<string> headings, HashSet<string> categories)
    {
        var joined = string.Join(" ", headings).ToUpperInvariant();
        if (categories.Contains("Liner") || joined.Contains("LINER"))
        {
            var key = joined.Contains("DUFFLE") ? "liner_hem_duffle" : "liner_hem_spout";
            jobs.Add(Job(key, bottleneck: false));
        }

        if (categories.Contains("Loop") || categories.Contains("Webbing") || joined.Contains("LOOP") || joined.Contains("WEBB"))
        {
            var loops = Math.Max(4, headings.Count(h =>
            {
                var cat = BomComponentClassifier.Classify(h, null, null, null, null).Category;
                return cat is "Loop" or "Webbing";
            }));
            jobs.Add(new JobDraft("belt", PiecesPerBag: loops, AffectsBottleneck: false));
        }

        if (joined.Contains("SKIRT"))
            jobs.Add(Job("skirt", bottleneck: false));
    }

    private static JobDraft Job(string key, bool bottleneck) => new(key, 1, bottleneck);

    private static string Label(string key) =>
        ActivityLabels.TryGetValue(key, out var label) ? label : key;

    private static string FactoryLabel(string key) => key switch
    {
        "kpw" => "KPW",
        "gandhi" => "Gandhidham",
        _ => "PIA",
    };

    private static string HeightBandFromCm(double? sizeH)
    {
        var h = sizeH ?? 105;
        if (h < 90) return "h50";
        if (h <= 120) return "h90";
        if (h <= 140) return "h120";
        if (h <= 170) return "h145";
        if (h <= 200) return "h175";
        return "h205";
    }

    private static string DustColumn(string dustLevel, string bagType)
    {
        var conductive = bagType.Contains("TYPE C", StringComparison.OrdinalIgnoreCase)
            || bagType.Contains("C-TYPE", StringComparison.OrdinalIgnoreCase)
            || bagType.Contains("CONDUCTIVE", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(bagType, @"\bC\b", RegexOptions.IgnoreCase);

        var dust = dustLevel.Trim();
        if (dust.StartsWith("Triple", StringComparison.OrdinalIgnoreCase))
            return conductive ? "ct" : "t";
        if (dust.StartsWith("Double", StringComparison.OrdinalIgnoreCase))
            return conductive ? "c" : "d";
        if (dust.StartsWith("Single", StringComparison.OrdinalIgnoreCase))
            return conductive ? "c" : "s";
        return conductive ? "c" : "n";
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
            (a, b) = (b, a % b);
        return Math.Abs(a);
    }

    private static int Lcm(int a, int b)
    {
        if (a <= 0 || b <= 0)
            return Math.Max(a, b);
        var gcd = Gcd(a, b);
        var raw = (long)a / gcd * b;
        return raw > int.MaxValue ? int.MaxValue : (int)raw;
    }

    private static int LcmMany(IEnumerable<int> values)
    {
        var acc = 0;
        foreach (var v in values.Where(x => x > 0).Distinct())
            acc = acc == 0 ? v : Lcm(acc, v);
        return acc;
    }

    private static FibcStitchSpecDto Empty(string factoryKey, List<string> warnings) => new()
    {
        FactoryKey = factoryKey,
        FactoryLabel = FactoryLabel(factoryKey),
        UsedExcelTargets = false,
        Warnings = warnings,
    };

    private readonly record struct JobDraft(string ActivityKey, int PiecesPerBag, bool AffectsBottleneck);
}
