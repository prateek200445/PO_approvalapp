using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Woven-bag and AD Star quotation math taken from Quote Woven Pricing.
/// Defaults reproduce the workbook, including the coating-wastage and CIF-commission behaviour.
/// </summary>
public static class WovenQuoteMath
{
    public static WovenQuoteResult Calculate(
        WovenQuoteDraft draft,
        IReadOnlyDictionary<string, decimal> rates,
        IReadOnlyDictionary<string, decimal> constants,
        IReadOnlyList<string> fabricCodes,
        IReadOnlyList<string> coatingCodes,
        IReadOnlyList<string> linerCodes)
    {
        return string.Equals(draft.ProductType, "AdStar", StringComparison.OrdinalIgnoreCase)
            ? CalculateAdStar(draft, rates, constants)
            : CalculateWoven(draft, rates, constants, fabricCodes, coatingCodes, linerCodes);
    }

    private static WovenQuoteResult CalculateWoven(
        WovenQuoteDraft d,
        IReadOnlyDictionary<string, decimal> rates,
        IReadOnlyDictionary<string, decimal> c,
        IReadOnlyList<string> fabricCodes,
        IReadOnlyList<string> coatingCodes,
        IReadOnlyList<string> linerCodes)
    {
        var result = new WovenQuoteResult();
        var warnings = result.Warnings;
        if (d.QuoteDate is null) warnings.Add("Enter a quote date so raw-material rates can be picked.");
        if (string.IsNullOrWhiteSpace(d.CustomerName)) warnings.Add("Enter a customer.");

        decimal density = K(c, "density", 0.92m);
        decimal width = Z(d.FaceWidthCm);
        decimal gusset = Z(d.GussetCm);
        decimal length = Z(d.LengthCm);
        string top = d.Top?.Trim() ?? "";
        string bag = d.BagType?.Trim() ?? "";
        decimal cutExtra = top switch
        {
            "Heat Cut" => K(c, "cutHeat", 5m),
            "Hemmed" => K(c, "cutHem", 8m),
            "Ez Open" => K(c, "cutEz", 0m),
            "No Fold" => K(c, "cutNoFold", 0m),
            _ => 0m,
        };
        if (top.Length == 0) warnings.Add("Select the bag top. Cut length stays at the entered length until you do.");
        decimal cut = length + cutExtra;
        bool backSeam = bag.Equals("Back Seam Bag", StringComparison.OrdinalIgnoreCase);
        decimal gross = width + gusset + (backSeam ? K(c, "backSeamExtraCm", 2.5m) : 0m);
        bool bagKnown = bag.Equals("Back Seam Bag", StringComparison.OrdinalIgnoreCase)
            || bag.Equals("Tubular Bag", StringComparison.OrdinalIgnoreCase)
            || bag.Equals("Tubular Fabric", StringComparison.OrdinalIgnoreCase)
            || bag.Equals("Flat Fabric", StringComparison.OrdinalIgnoreCase);
        if (!bagKnown) warnings.Add("Select a bag type.");

        decimal handle = Z(d.HandleGram);
        decimal easy = top.Equals("Ez Open", StringComparison.OrdinalIgnoreCase)
            ? K(c, "easyOpenGramPerMeter", 7m) / 100m * (width + K(c, "easyOpenWidthExtra", 10m))
            : 0m;
        decimal liner = Area(width + gusset + K(c, "linerWidthExtra", 2m), cut + K(c, "linerLengthExtra", 5m)) * Z(d.LinerMicron) * density;
        decimal boppWidth = width + gusset + K(c, "boppWidthExtra", 1.5m);
        decimal glossy = Area(boppWidth, cut) * Z(d.GlossyBoppMicron) * density;
        decimal matt = Area(boppWidth, cut) * Z(d.MattBoppMicron) * density;
        decimal metal = Area(boppWidth, cut) * Z(d.MetalisedBoppMicron) * density;
        decimal white = Area(boppWidth, cut) * Z(d.WhitePaperGsm);
        decimal brown = Area(boppWidth, cut) * Z(d.BrownPaperGsm);
        decimal adhesive = Area(boppWidth, cut) * Z(d.AdhesiveGsm) * density;
        decimal ink = Area(boppWidth, cut) * Z(d.InkGsm);
        decimal solvent = ink == 0 ? 0 : ink * K(c, "solventInkFactor", 2m);
        decimal backSeamGram = backSeam ? K(c, "backSeamCoatingGrPerM", 2m) * cut / 100m : 0m;
        decimal coating = Area(width + gusset + K(c, "coatingWidthExtra", 2m), cut) * Z(d.CoatingGsm);
        bool stitched = bag.Equals("Tubular Bag", StringComparison.OrdinalIgnoreCase) || backSeam;
        decimal thread = 0m;
        if (stitched)
        {
            decimal divisor = K(c, "threadDivisor", 61m);
            if (divisor == 0) divisor = 61m;
            thread = top switch
            {
                "Hemmed" => K(c, "threadHem", 3m) / divisor * width,
                "Heat Cut" => K(c, "threadHeat", 2m) / divisor * width,
                "Ez Open" => K(c, "threadEz", 1.2m) / divisor * (width + K(c, "threadEzWidthExtra", 10m)),
                _ => 0m,
            };
        }

        decimal parts = handle + easy + liner + glossy + matt + metal + white + brown + adhesive + ink + backSeamGram + coating + thread;
        bool gsmBlank = d.FabricGsm is null;
        decimal fabricFromGsm = gsmBlank ? 0 : Area(gross, cut) * Z(d.FabricGsm);
        decimal calculated = gsmBlank ? 0 : fabricFromGsm + parts;
        decimal fabric;
        decimal sellingWeight;
        if (d.TotalWeightGram is decimal typed && typed > calculated)
        {
            sellingWeight = typed;
            fabric = typed - parts;
        }
        else
        {
            if (d.TotalWeightGram is decimal typedBelow && typedBelow < calculated)
                warnings.Add("Entered weight is below the calculated weight, so the calculated weight is used.");
            sellingWeight = calculated;
            fabric = fabricFromGsm;
        }
        decimal fabricGsm = gross <= 0 || cut <= 0 ? 0 : Math.Round(fabric / (gross * 2m * cut / 10000m), 2, MidpointRounding.AwayFromZero);

        var fabricRmc = Blend(d.FabricRecipe, rates, fabricCodes);
        var coatingRmc = Blend(d.CoatingRecipe, rates, coatingCodes);
        var linerRmc = Blend(d.LinerRecipe, rates, linerCodes);
        WarnRecipe(warnings, "Fabric", d.FabricRecipe);
        WarnRecipe(warnings, "Coating", d.CoatingRecipe);
        WarnRecipe(warnings, "Liner", d.LinerRecipe);

        decimal printDen = white + brown + glossy + matt + metal + adhesive + ink;
        decimal printNum =
            white * Rate(rates, "WHITE_PAPER")
            + brown * Rate(rates, "BROWN_PAPER")
            + glossy * Rate(rates, "GLOSSY")
            + matt * Rate(rates, "MATT")
            + metal * Rate(rates, "METAL")
            + adhesive * Rate(rates, "ADHESIVE")
            + ink * Rate(rates, "INK")
            + solvent * Rate(rates, "SOLVENT");
        decimal printWaste = Z(d.InkGsm) > 0 ? K(c, "printWasteFraction", 0.05m) : 0m;
        decimal printedRmc = printDen == 0 ? 0 : printNum / printDen * (1m + printWaste);
        decimal printedGram = printDen;

        decimal ez = handle + easy + thread;
        decimal totalGram = fabric + coating + liner + backSeamGram + printedGram + ez;
        decimal uvRate = Rate(rates, "UV");
        decimal backRate = Rate(rates, "BACKSEAM");
        decimal totalRmc = totalGram == 0
            ? 0
            : (fabric * fabricRmc + coating * coatingRmc + liner * linerRmc + backSeamGram * backRate + printedGram * printedRmc + ez * uvRate) / totalGram;

        decimal convF = Z(d.FabricConversion, 25m);
        decimal convC = Z(d.CoatingConversion, 14m);
        decimal convL = Z(d.LinerConversion, 14m);
        decimal paperGsm = Z(d.WhitePaperGsm) + Z(d.BrownPaperGsm);
        decimal boppMic = Z(d.GlossyBoppMicron) + Z(d.MattBoppMicron) + Z(d.MetalisedBoppMicron);
        decimal convP = Z(d.InkGsm) <= 0 ? 0 : paperGsm > 0 ? K(c, "printConvPaper", 10m) : boppMic > 0 ? K(c, "printConvBopp", 40m) : 0m;

        decimal wasF = PercentFraction(d.WastageFabric, 5m);
        decimal wasL = PercentFraction(d.WastageLiner, 2m);
        decimal wasFin = PercentFraction(d.WastageFinishing, 3m);
        decimal span = (width + gusset) * 2m;
        decimal wasC = 0m;
        if (Z(d.CoatingGsm) > 0 && span > 0)
        {
            decimal num = bag.Equals("Tubular Bag", StringComparison.OrdinalIgnoreCase) || bag.Equals("Tubular Fabric", StringComparison.OrdinalIgnoreCase)
                ? K(c, "coatWasteTubularNum", 1.75m)
                : K(c, "coatWasteOtherNum", 3.75m);
            wasC = num / span + K(c, "coatWasteBase", 0.0075m);
        }

        decimal realF = Z(d.RealisationFabric, 50m);
        decimal realL = Z(d.RealisationLiner, 40m);
        decimal realC = paperGsm > 0 ? K(c, "realisationCoatingPaper", 0m) : K(c, "realisationCoatingNoPaper", 35m);
        decimal realFin = paperGsm > 0 ? K(c, "realisationFinishingPaper", 8m) : K(c, "realisationFinishingNoPaper", 35m);
        decimal thumb = d.ThumbNotch ? K(c, "thumbNotch", 0.4m) : 0m;
        decimal metalCost = d.MetalDetection ? K(c, "metalDetection", 0.3m) : 0m;
        decimal perf = d.Perforation ? K(c, "perforation", 0.2m) : 0m;
        decimal flexo = Z(d.FlexoPrinting, 0.6m);
        decimal finishSum = Z(d.BagFinishing, 1.4m) + thumb + metalCost + perf + flexo;

        decimal coatWeightForWaste = K(c, "coatingWastageUsesTotalWeight", 1m) >= 1m ? totalGram : coating;
        decimal waste =
            (fabricRmc + convF - realF) * wasF * fabric / 1000m
            + (d.CoatingGsm is null ? 0 : (coatingRmc + convC - realC) * coatWeightForWaste * wasC / 1000m)
            + (linerRmc + convL - realL) * wasL * liner / 1000m;
        if (totalGram > 0)
        {
            decimal convBag = (fabric * convF + coating * convC + liner * convL + printedGram * convP) / 1000m;
            waste += ((totalRmc + (convBag + finishSum) / totalGram * 1000m) - realFin) * wasFin * totalGram / 1000m;
        }

        decimal perKg = Z(d.RewindingPerKg) + Z(d.PackingPerKg, 1m) + Z(d.PalletPerKg, 4m) + Z(d.FobCostPerKg, 3.5m);
        decimal costPc =
            (fabricRmc + convF) * fabric / 1000m
            + (coatingRmc + convC) * coating / 1000m
            + (linerRmc + convL) * liner / 1000m
            + backRate * backSeamGram / 1000m
            + (printedRmc + convP) * printedGram / 1000m
            + ez * uvRate / 1000m
            + waste
            + finishSum
            + perKg * totalGram / 1000m;
        decimal costKg = totalGram == 0 ? 0 : costPc / totalGram * 1000m;
        decimal pay = Z(d.PaymentDays, 30m);
        decimal year = K(c, "yearDays", 365m);
        if (year == 0) year = 365m;
        decimal wc = costKg * (K(c, "wcBaseDays", 30m) + pay) / year * K(c, "wcRate", 0.125m);
        decimal fixedAsset = K(c, "fixedAssetPerKg", 13.14m);
        decimal profitPct = Z(d.ProfitPercent, 8m);
        decimal profit = (costKg + wc + fixedAsset) * profitPct / 100m;
        decimal quote = costKg + wc + fixedAsset + profit;
        decimal usd = Z(d.UsdInr, 88m);
        decimal euro = Z(d.EuroPerUsd, 1.05m);
        decimal gbp = Z(d.GbpPerUsd, 1.23m);
        decimal commission = Z(d.CommissionUsdPerKg);
        decimal freight = Z(d.FreightUsdPerKg);
        if (usd == 0) warnings.Add("USD / INR is zero, so the foreign-currency price is blank.");
        decimal fobUsd = usd == 0 ? 0 : quote / usd + commission;
        bool cifAgain = K(c, "cifAddsCommissionAgain", 1m) >= 1m;
        decimal cifUsd = fobUsd + freight + (cifAgain ? commission : 0m);
        decimal delivered = (fobUsd + freight + (cifAgain ? commission : 0m)) * usd;
        decimal fobEuro = euro == 0 ? 0 : fobUsd / euro;
        decimal cifEuro = euro == 0 ? 0 : cifUsd / euro;
        decimal fobGbp = gbp == 0 ? 0 : fobUsd / gbp;
        decimal cifGbp = gbp == 0 ? 0 : cifUsd / gbp;

        string currency = (d.Currency ?? "").Trim();
        string terms = (d.Terms ?? "").Trim();
        bool fobLike = terms.Equals("FOB", StringComparison.OrdinalIgnoreCase);
        bool exWorks = terms.Equals("Ex Works", StringComparison.OrdinalIgnoreCase);
        decimal perKgPrice = currency switch
        {
            "USD" => fobLike ? fobUsd : cifUsd,
            "Euro" => fobLike ? fobEuro : cifEuro,
            "GBP" => fobLike ? fobGbp : cifGbp,
            "INR" => exWorks ? quote : delivered,
            _ => 0m,
        };
        if (currency.Length == 0) warnings.Add("Select a currency.");
        decimal pricePc = perKgPrice * sellingWeight / 1000m;

        bool flat = bag.Equals("Flat Fabric", StringComparison.OrdinalIgnoreCase);
        bool inch = (d.DimensionUnit ?? "").Equals("Inch", StringComparison.OrdinalIgnoreCase);
        decimal? offerWidth = fabric <= 0 ? null : inch
            ? (flat ? width / 2.54m * 2m : width / 2.54m)
            : (flat ? width * 2m : width);
        decimal? offerGusset = fabric <= 0 ? null : inch ? gusset / 2.54m : gusset;
        decimal? offerLength = fabric <= 0 ? null : inch ? length / 2.54m : length;

        string printed = Z(d.InkGsm) > 0 && Z(d.GlossyBoppMicron) > 0 ? "Printed Glossy BOPP"
            : Z(d.InkGsm) > 0 && Z(d.MattBoppMicron) > 0 ? "Printed Matte BOPP"
            : Z(d.InkGsm) > 0 && Z(d.MetalisedBoppMicron) > 0 ? "Printed Metalised BOPP"
            : Z(d.InkGsm) > 0 && paperGsm > 0 ? "Printed Paper"
            : flexo > 0 ? "Yes"
            : "No";
        decimal uvPct = Pct(d.FabricRecipe, "UV");
        result.CutLengthCm = cut;
        result.GrossWidthCm = gross;
        result.FabricGram = fabric;
        result.FabricGsmCalculated = fabricGsm;
        result.CalculatedWeightGram = calculated;
        result.TotalComponentGram = totalGram;
        result.FabricRmc = fabricRmc;
        result.CoatingRmc = coatingRmc;
        result.LinerRmc = linerRmc;
        result.PrintedRmc = printedRmc;
        result.TotalRmcPerKg = totalRmc;
        result.WastageCost = waste;
        result.CostPerPiece = costPc;
        result.CostPerKg = costKg;
        result.WorkingCapitalPerKg = wc;
        result.ProfitPerKg = profit;
        result.QuotePerKg = quote;
        result.FobUsdPerKg = fobUsd;
        result.CifUsdPerKg = cifUsd;
        result.DeliveredInrPerKg = delivered;
        result.PricePerPiece = pricePc;
        result.PriceBasis = $"{terms} {currency}".Trim();
        result.PrintedLabel = printed;
        result.CoatedLabel = coating > 0 ? "Yes" : "No";
        result.UvLabel = uvPct > 0 ? "Yes" : "No";
        result.LinerLabel = Z(d.LinerMicron) > 0 ? "Yes" : "No";
        result.OfferWidth = offerWidth;
        result.OfferGusset = offerGusset;
        result.OfferLength = offerLength;

        void Line(string group, string label, decimal value, string unit) =>
            result.Lines.Add(new WovenQuoteLine { Group = group, Label = label, Value = value, Unit = unit });

        Line("Weight", "Cut length", cut, "cm");
        Line("Weight", "Gross width", gross, "cm");
        Line("Weight", "Fabric", fabric, "g");
        Line("Weight", "Fabric GSM", fabricGsm, "gsm");
        Line("Weight", "Liner", liner, "g");
        Line("Weight", "Coating", coating, "g");
        Line("Weight", "Glossy BOPP", glossy, "g");
        Line("Weight", "Matt BOPP", matt, "g");
        Line("Weight", "Metalised BOPP", metal, "g");
        Line("Weight", "White paper", white, "g");
        Line("Weight", "Brown paper", brown, "g");
        Line("Weight", "Adhesive", adhesive, "g");
        Line("Weight", "Ink", ink, "g");
        Line("Weight", "Solvent", solvent, "g");
        Line("Weight", "Back seam coating", backSeamGram, "g");
        Line("Weight", "Thread", thread, "g");
        Line("Weight", "Easy open", easy, "g");
        Line("Weight", "Handle", handle, "g");
        Line("Weight", "Check weight from entered GSM", calculated, "g");
        Line("Material", "Fabric raw material", fabricRmc, "Rs/kg");
        Line("Material", "Coating raw material", coatingRmc, "Rs/kg");
        Line("Material", "Liner raw material", linerRmc, "Rs/kg");
        Line("Material", "Printed film / paper", printedRmc, "Rs/kg");
        Line("Material", "Blended raw material", totalRmc, "Rs/kg");
        Line("Cost", "Wastage", waste, "Rs/pc");
        Line("Cost", "Manufacturing cost", costPc, "Rs/pc");
        Line("Cost", "Manufacturing cost", costKg, "Rs/kg");
        Line("Cost", "Working capital", wc, "Rs/kg");
        Line("Cost", "Fixed assets and depreciation", fixedAsset, "Rs/kg");
        Line("Cost", "Profit", profit, "Rs/kg");
        Line("Price", "Quote", quote, "Rs/kg");
        Line("Price", "FOB", fobUsd, "USD/kg");
        Line("Price", "CIF", cifUsd, "USD/kg");
        Line("Price", "Delivered", delivered, "Rs/kg");
        Line("Wastage", "Coating wastage", wasC * 100m, "%");
        Line("Wastage", "Coating realisation", realC, "Rs/kg");
        Line("Wastage", "Finishing realisation", realFin, "Rs/kg");
        Line("Price", "Quotation", pricePc, currency.Length == 0 ? "per pc" : $"{currency}/pc");
        return result;
    }

    private static WovenQuoteResult CalculateAdStar(
        WovenQuoteDraft d,
        IReadOnlyDictionary<string, decimal> rates,
        IReadOnlyDictionary<string, decimal> c)
    {
        var result = new WovenQuoteResult();
        if (d.QuoteDate is null) result.Warnings.Add("Enter a quote date so the PP rate can be picked.");
        decimal width = Z(d.AdWidth);
        decimal length = Z(d.AdLength);
        decimal patch = Z(d.AdPatch);
        decimal valve = Z(d.AdValveLength);
        decimal extended = Z(d.AdExtendedValve);
        decimal gsm = Z(d.AdFabricGsm) + Z(d.AdCoatingGsm) + Z(d.AdBoppGsm);
        decimal extra = K(c, "adPatchGsmExtra", 20m);
        decimal density = K(c, "density", 0.92m);
        decimal bag = d.AdOpenMouth
            ? width * 2m * (length + patch / 2m + K(c, "adOpenLengthExtra", 1.5m)) / 10000m * gsm
              + (gsm + extra) * width * patch / 10000m
            : width * 2m * (length + patch + K(c, "adClosedLengthExtra", 3m)) / 10000m * gsm
              + width * patch * 2m * (gsm + extra) / 10000m
              + patch * (valve + extended) * 2m / 10000m * (Z(d.AdFabricGsm) + Z(d.AdCoatingGsm) + Z(d.AdCoatingGsm));
        decimal liner = (width + K(c, "adLinerWidthExtra", 3m)) * 2m * (length + patch) * Z(d.AdLinerMicron) * density / 10000m;
        decimal total = bag + liner;
        decimal pp = Rate(rates, "PP");
        decimal fobInr = pp + Z(d.AdConversion);
        decimal usd = Z(d.UsdInr, 88m);
        decimal euro = Z(d.EuroPerUsd, 1.08m);
        decimal freight = Z(d.AdSeaFreight);
        decimal usdPc = usd == 0 ? 0 : (fobInr + freight) / usd * total / 1000m;
        decimal euroPc = euro == 0 ? 0 : usdPc / euro;
        result.FabricGram = bag;
        result.TotalComponentGram = total;
        result.FabricRmc = pp;
        result.CostPerKg = fobInr;
        result.QuotePerKg = fobInr + freight;
        result.PricePerPiece = usdPc;
        result.FobUsdPerKg = total == 0 || usd == 0 ? 0 : usdPc / total * 1000m;
        result.PriceBasis = "USD per piece";
        result.LinerLabel = Z(d.AdLinerMicron) > 0 ? "Yes" : "No";
        result.UvLabel = d.AdUv ? "Yes" : "No";
        result.CoatedLabel = Z(d.AdCoatingGsm) > 0 ? "Yes" : "No";
        void Line(string group, string label, decimal value, string unit) =>
            result.Lines.Add(new WovenQuoteLine { Group = group, Label = label, Value = value, Unit = unit });
        Line("Weight", "Bag", bag, "g");
        Line("Weight", "Liner", liner, "g");
        Line("Weight", "Total", total, "g");
        Line("Material", "PP rafia", pp, "Rs/kg");
        Line("Cost", "Conversion", Z(d.AdConversion), "Rs/kg");
        Line("Cost", "FOB before freight", fobInr, "Rs/kg");
        Line("Cost", "Sea freight", freight, "Rs/kg");
        Line("Price", "Quotation", usdPc, "USD/pc");
        Line("Price", "Quotation", euroPc, "Euro/pc");
        return result;
    }

    private static decimal Area(decimal widthCm, decimal lengthCm) => widthCm * 2m * lengthCm / 10000m;

    private static decimal Blend(
        Dictionary<string, decimal>? recipe,
        IReadOnlyDictionary<string, decimal> rates,
        IReadOnlyList<string> codes)
    {
        decimal sum = 0m;
        decimal weighted = 0m;
        foreach (var code in codes)
        {
            decimal pct = 0m;
            recipe?.TryGetValue(code, out pct);
            sum += pct;
            weighted += pct * Rate(rates, code);
        }
        return sum == 0 ? 0 : weighted / sum;
    }

    private static void WarnRecipe(List<string> warnings, string name, Dictionary<string, decimal>? recipe)
    {
        if (recipe is null) return;
        decimal sum = recipe.Values.Sum();
        if (sum > 0 && Math.Abs(sum - 100m) > 0.05m)
            warnings.Add($"{name} recipe adds up to {sum:0.##}%. The blend still uses that total, the same way the workbook does.");
    }

    private static decimal Pct(Dictionary<string, decimal>? recipe, string code)
    {
        if (recipe is null || !recipe.TryGetValue(code, out var value)) return 0m;
        return value;
    }

    private static decimal Rate(IReadOnlyDictionary<string, decimal> rates, string code) =>
        rates.TryGetValue(code, out var value) ? value : 0m;

    private static decimal K(IReadOnlyDictionary<string, decimal> constants, string key, decimal fallback) =>
        constants.TryGetValue(key, out var value) ? value : fallback;

    private static decimal Z(decimal? value, decimal fallback = 0m) => value ?? fallback;

    /// <summary>5 means 5%. Values of 0.2 or less are the old workbook fractions, such as 0.05.</summary>
    private static decimal PercentFraction(decimal? value, decimal fallbackPercent)
    {
        decimal percent = value ?? fallbackPercent;
        if (percent > 0 && percent <= 0.2m) return percent;
        return percent / 100m;
    }
}
