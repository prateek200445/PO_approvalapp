namespace POApprovalAPI.Models;

public class WovenQuoteDraft
{
    public string ProductType { get; set; } = "WovenBag";
    public DateTime? QuoteDate { get; set; }
    public string Sector { get; set; } = "Export";
    public string CustomerName { get; set; } = "";
    public string OurRef { get; set; } = "";
    public string CustomerRef { get; set; } = "";
    public string BagType { get; set; } = "Tubular Bag";
    public string FabricColour { get; set; } = "";
    public decimal? FaceWidthCm { get; set; }
    public decimal? GussetCm { get; set; }
    public decimal? LengthCm { get; set; }
    public string Top { get; set; } = "Hemmed";
    public decimal? TotalWeightGram { get; set; }
    public decimal? FabricGsm { get; set; }
    public decimal? LinerMicron { get; set; }
    public decimal? GlossyBoppMicron { get; set; }
    public decimal? MattBoppMicron { get; set; }
    public decimal? MetalisedBoppMicron { get; set; }
    public decimal? WhitePaperGsm { get; set; }
    public decimal? BrownPaperGsm { get; set; }
    public decimal? AdhesiveGsm { get; set; }
    public decimal? InkGsm { get; set; }
    public decimal? CoatingGsm { get; set; }
    public decimal? HandleGram { get; set; }
    public Dictionary<string, decimal>? FabricRecipe { get; set; }
    public Dictionary<string, decimal>? CoatingRecipe { get; set; }
    public Dictionary<string, decimal>? LinerRecipe { get; set; }
    public decimal? FabricConversion { get; set; }
    public decimal? CoatingConversion { get; set; }
    public decimal? LinerConversion { get; set; }
    public decimal? WastageFabric { get; set; }
    public decimal? WastageLiner { get; set; }
    public decimal? WastageFinishing { get; set; }
    public decimal? RealisationFabric { get; set; }
    public decimal? RealisationLiner { get; set; }
    public decimal? BagFinishing { get; set; }
    public bool ThumbNotch { get; set; }
    public bool MetalDetection { get; set; }
    public bool Perforation { get; set; }
    public decimal? FlexoPrinting { get; set; }
    public decimal? RewindingPerKg { get; set; }
    public decimal? PackingPerKg { get; set; }
    public decimal? PalletPerKg { get; set; }
    public decimal? FobCostPerKg { get; set; }
    public decimal? PaymentDays { get; set; }
    public decimal? ProfitPercent { get; set; }
    public decimal? UsdInr { get; set; }
    public decimal? EuroPerUsd { get; set; }
    public decimal? GbpPerUsd { get; set; }
    public decimal? FreightUsdPerKg { get; set; }
    public decimal? CommissionUsdPerKg { get; set; }
    public string DimensionUnit { get; set; } = "CM";
    public string Terms { get; set; } = "FOB";
    public string SeaPort { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public string Packing { get; set; } = "Floor Loaded";
    public decimal? PcsPerBale { get; set; }
    public string PalletSize { get; set; } = "";
    public decimal? BagsPerPallet { get; set; }
    public decimal? PrintPlateCost { get; set; }
    public string Remarks { get; set; } = "";
    public decimal? AdWidth { get; set; }
    public decimal? AdLength { get; set; }
    public decimal? AdPatch { get; set; }
    public decimal? AdValveLength { get; set; }
    public decimal? AdExtendedValve { get; set; }
    public decimal? AdFabricGsm { get; set; }
    public decimal? AdCoatingGsm { get; set; }
    public decimal? AdBoppGsm { get; set; }
    public bool AdOpenMouth { get; set; }
    public bool AdUv { get; set; }
    public string AdPrinting { get; set; } = "";
    public decimal? AdLinerMicron { get; set; }
    public decimal? AdConversion { get; set; }
    public decimal? AdSeaFreight { get; set; }
    public decimal? AdPcsPerBale { get; set; }
    public decimal? AdPcsPerPallet { get; set; }
}

public class WovenQuoteLine
{
    public string Group { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public string Unit { get; set; } = "";
}

public class WovenQuoteResult
{
    public List<string> Warnings { get; set; } = [];
    public decimal CutLengthCm { get; set; }
    public decimal GrossWidthCm { get; set; }
    public decimal FabricGram { get; set; }
    public decimal FabricGsmCalculated { get; set; }
    public decimal CalculatedWeightGram { get; set; }
    public decimal TotalComponentGram { get; set; }
    public decimal FabricRmc { get; set; }
    public decimal CoatingRmc { get; set; }
    public decimal LinerRmc { get; set; }
    public decimal PrintedRmc { get; set; }
    public decimal TotalRmcPerKg { get; set; }
    public decimal WastageCost { get; set; }
    public decimal CostPerPiece { get; set; }
    public decimal CostPerKg { get; set; }
    public decimal WorkingCapitalPerKg { get; set; }
    public decimal ProfitPerKg { get; set; }
    public decimal QuotePerKg { get; set; }
    public decimal FobUsdPerKg { get; set; }
    public decimal CifUsdPerKg { get; set; }
    public decimal DeliveredInrPerKg { get; set; }
    public decimal PricePerPiece { get; set; }
    public string PriceBasis { get; set; } = "";
    public string PrintedLabel { get; set; } = "";
    public string CoatedLabel { get; set; } = "";
    public string UvLabel { get; set; } = "";
    public string LinerLabel { get; set; } = "";
    public decimal? OfferWidth { get; set; }
    public decimal? OfferGusset { get; set; }
    public decimal? OfferLength { get; set; }
    public List<WovenQuoteLine> Lines { get; set; } = [];
}

public class WovenQuoteSaveRequest
{
    public WovenQuoteDraft? Draft { get; set; }
}

public class WovenQuoteListItem
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
    public string CreatedBy { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

public class WovenQuoteDetail : WovenQuoteListItem
{
    public WovenQuoteDraft Draft { get; set; } = new();
    public WovenQuoteResult Result { get; set; } = new();
}

public class WovenQuoteMaterialRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool InFabric { get; set; }
    public bool InCoating { get; set; }
    public bool InLiner { get; set; }
}

public class WovenQuoteRateRow
{
    public int Id { get; set; }
    public string MaterialCode { get; set; } = "";
    public string Sector { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public decimal Rate { get; set; }
}

public class WovenQuoteRateWrite
{
    public string MaterialCode { get; set; } = "";
    public string Sector { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public decimal Rate { get; set; }
}

public class WovenQuoteCustomerRow
{
    public int Id { get; set; }
    public string Sector { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ShortName { get; set; } = "";
    public bool IsActive { get; set; }
}

public class WovenQuoteLookupRow
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Value { get; set; } = "";
    public int SortOrder { get; set; }
}

public class WovenQuoteConstantRow
{
    public string Key { get; set; } = "";
    public decimal Value { get; set; }
    public string Label { get; set; } = "";
    public string GroupName { get; set; } = "";
}

public class WovenQuoteMasters
{
    public List<WovenQuoteMaterialRow> Materials { get; set; } = [];
    public List<WovenQuoteLookupRow> Lookups { get; set; } = [];
    public List<WovenQuoteConstantRow> Constants { get; set; } = [];
    public List<WovenQuoteCustomerRow> Customers { get; set; } = [];
    public List<WovenQuoteRateRow> LatestRates { get; set; } = [];
}
