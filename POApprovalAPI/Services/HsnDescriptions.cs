using System.Text.RegularExpressions;

namespace POApprovalAPI.Services;

/// <summary>
/// Short HSN descriptions for GSTR-1 table 12. The GST offline tool / portal accepts at most 30 characters,
/// so descriptions are condensed HSN heading texts (longest prefix wins), falling back to the commodity name.
/// </summary>
public static class HsnDescriptions
{
    public const int MaxLength = 30;

    private static readonly Dictionary<string, string> ByPrefix = new(StringComparer.Ordinal)
    {
        ["25"] = "SALT, EARTHS, STONE & CEMENT",
        ["2523"] = "CEMENT",
        ["27"] = "MINERAL FUELS & OILS",
        ["2710"] = "PETROLEUM OILS & LUBRICANTS",
        ["32"] = "DYES, PIGMENTS, PAINTS & INKS",
        ["3204"] = "SYNTHETIC ORGANIC COLOURANTS",
        ["3206"] = "COLOURING MATTER/PREPARATIONS",
        ["3215"] = "PRINTING INK",
        ["34"] = "SOAPS, WAXES & LUBRICANTS",
        ["3403"] = "LUBRICATING PREPARATIONS",
        ["35"] = "GLUES & ADHESIVES",
        ["3506"] = "PREPARED GLUES & ADHESIVES",
        ["38"] = "MISC CHEMICAL PRODUCTS",
        ["3812"] = "STABILISERS FOR PLASTICS",
        ["3814"] = "ORGANIC SOLVENTS & THINNERS",
        ["3823"] = "INDUSTRIAL FATTY ACIDS",
        ["39"] = "PLASTICS & ARTICLES THEREOF",
        ["3901"] = "POLYMERS OF ETHYLENE",
        ["3902"] = "POLYMERS OF PROPYLENE",
        ["3915"] = "PLASTIC WASTE AND SCRAP",
        ["3917"] = "PLASTIC TUBES, PIPES & HOSES",
        ["3919"] = "SELF-ADHESIVE PLASTIC TAPES",
        ["3920"] = "PLASTIC FILMS, SHEETS & STRIPS",
        ["3923"] = "PLASTIC PACKING ARTICLES",
        ["392310"] = "PLASTIC BOXES, CASES, CRATES",
        ["392321"] = "SACKS & BAGS OF ETHYLENE",
        ["392329"] = "SACKS & BAGS OF OTHER PLASTICS",
        ["392330"] = "PLASTIC CARBOYS & BOTTLES",
        ["392350"] = "PLASTIC STOPPERS, LIDS & CAPS",
        ["392390"] = "OTHER PLASTIC PACKING ARTICLE",
        ["3926"] = "OTHER ARTICLES OF PLASTICS",
        ["40"] = "RUBBER & ARTICLES THEREOF",
        ["4001"] = "NATURAL RUBBER",
        ["4008"] = "VULCANISED RUBBER SHEETS",
        ["4017"] = "HARD RUBBER & ARTICLES",
        ["44"] = "WOOD & ARTICLES OF WOOD",
        ["4401"] = "FUEL WOOD, WOOD WASTE & SCRAP",
        ["4415"] = "WOODEN PALLETS & PACKING CASES",
        ["47"] = "PULP & WASTE PAPER",
        ["4707"] = "WASTE PAPER & PAPERBOARD",
        ["48"] = "PAPER & PAPERBOARD ARTICLES",
        ["4802"] = "UNCOATED PAPER & PAPERBOARD",
        ["4804"] = "KRAFT PAPER & PAPERBOARD",
        ["4810"] = "COATED PAPER & PAPERBOARD",
        ["4819"] = "PAPER CARTONS, BOXES & BAGS",
        ["4821"] = "PAPER OR PAPERBOARD LABELS",
        ["4822"] = "PAPER BOBBINS, SPOOLS & CORES",
        ["54"] = "MAN-MADE FILAMENTS",
        ["5402"] = "SYNTHETIC FILAMENT YARN",
        ["5404"] = "SYNTHETIC MONOFILAMENT & STRIP",
        ["5407"] = "SYNTHETIC FILAMENT FABRICS",
        ["540720"] = "WOVEN FABRIC OF PP/PE STRIP",
        ["55"] = "MAN-MADE STAPLE FIBRES",
        ["5509"] = "SYNTHETIC STAPLE FIBRE YARN",
        ["56"] = "NONWOVENS, TWINE & ROPES",
        ["5603"] = "NONWOVEN FABRICS",
        ["58"] = "SPECIAL WOVEN FABRICS",
        ["5806"] = "NARROW WOVEN FABRICS",
        ["5807"] = "TEXTILE LABELS & BADGES",
        ["59"] = "COATED TEXTILE FABRICS",
        ["5906"] = "RUBBERISED TEXTILE FABRICS",
        ["63"] = "MADE-UP TEXTILE ARTICLES",
        ["6305"] = "SACKS & BAGS FOR PACKING",
        ["630532"] = "FIBC - FLEXIBLE BULK CONTAINER",
        ["630533"] = "PP/PE STRIP SACKS & BAGS",
        ["6310"] = "TEXTILE RAGS & WASTE",
        ["64"] = "FOOTWEAR",
        ["6402"] = "RUBBER/PLASTIC FOOTWEAR",
        ["68"] = "STONE, CEMENT & ASBESTOS GOODS",
        ["6802"] = "WORKED BUILDING STONE",
        ["6811"] = "ASBESTOS-CEMENT ARTICLES",
        ["70"] = "GLASS & GLASSWARE",
        ["7019"] = "GLASS FIBRE & ARTICLES",
        ["72"] = "IRON AND STEEL",
        ["7204"] = "FERROUS WASTE AND SCRAP",
        ["7207"] = "SEMI-FINISHED IRON/STEEL",
        ["7211"] = "FLAT-ROLLED STEEL",
        ["7216"] = "IRON/STEEL ANGLES & SECTIONS",
        ["7223"] = "STAINLESS STEEL WIRE",
        ["73"] = "ARTICLES OF IRON OR STEEL",
        ["7306"] = "STEEL TUBES & PIPES",
        ["7318"] = "SCREWS, BOLTS & NUTS",
        ["83"] = "MISC ARTICLES OF BASE METAL",
        ["8306"] = "STATUETTES & ORNAMENTS",
        ["84"] = "MACHINERY & MECHANICAL PARTS",
        ["8418"] = "REFRIGERATING EQUIPMENT PARTS",
        ["8422"] = "PACKING MACHINERY & PARTS",
        ["8441"] = "PAPER CUTTING MACHINES",
        ["8443"] = "PRINTING MACHINERY & PARTS",
        ["8446"] = "WEAVING MACHINES (LOOMS)",
        ["8448"] = "TEXTILE MACHINERY PARTS",
        ["8452"] = "SEWING MACHINES & PARTS",
        ["8482"] = "BALL OR ROLLER BEARINGS",
        ["8483"] = "TRANSMISSION SHAFTS & GEARS",
        ["85"] = "ELECTRICAL MACHINERY & PARTS",
        ["8536"] = "LOW VOLTAGE ELECTRIC APPARATUS",
        ["8539"] = "ELECTRIC LAMPS",
        ["94"] = "FURNITURE & LIGHTING",
        ["9405"] = "LAMPS & LIGHTING FITTINGS",
        ["96"] = "MISC MANUFACTURED ARTICLES",
        ["9609"] = "PENCILS & CRAYONS",
        ["99"] = "SERVICES",
        ["9972"] = "REAL ESTATE SERVICES",
        ["997212"] = "RENTAL - NON-RESIDENTIAL PROP",
        ["9973"] = "LEASING & RENTAL SERVICES",
        ["9985"] = "SUPPORT SERVICES",
        ["9988"] = "JOB WORK MANUFACTURING SERVICE",
    };

    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex Brackets = new(@"\s*\([^)]*\)", RegexOptions.Compiled);

    public static string For(string hsn, string fallbackName)
    {
        var digits = new string((hsn ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length >= 4)
        {
            for (var len = Math.Min(digits.Length, 8); len >= 2; len--)
                if (ByPrefix.TryGetValue(digits[..len], out var text))
                    return Shorten(text);
        }
        return Shorten(fallbackName);
    }

    /// <summary>Uppercase, single-spaced, ≤30 chars; drops bracketed text first and cuts on a word boundary.</summary>
    public static string Shorten(string? text)
    {
        var s = Spaces.Replace((text ?? "").Trim(), " ").ToUpperInvariant();
        if (s.Length <= MaxLength) return s;
        var noBrackets = Spaces.Replace(Brackets.Replace(s, ""), " ").Trim();
        if (noBrackets.Length > 0) s = noBrackets;
        if (s.Length <= MaxLength) return s;
        var cut = s[..(MaxLength + 1)];
        var space = cut.LastIndexOf(' ');
        s = space >= MaxLength / 2 ? cut[..space] : s[..MaxLength];
        return s.TrimEnd(' ', '-', ',', '/', '&', '.', '(');
    }
}
