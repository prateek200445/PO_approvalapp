using System.Text.Json;

namespace POApprovalAPI.Services.FinancialStatements;

public class PresentationRulesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _companiesDir;

    public PresentationRulesService(IWebHostEnvironment env)
    {
        _companiesDir = Path.Combine(env.ContentRootPath, "Data", "FinancialStatements", "companies");
    }

    public PresentationRules? GetRules(string companyKey)
    {
        if (string.IsNullOrWhiteSpace(companyKey))
            return null;

        var path = Path.Combine(_companiesDir, companyKey.Trim(), "presentation-rules.json");
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PresentationRules>(json, JsonOptions);
    }
}

public class PresentationRules
{
    public string CompanyKey { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public Dictionary<string, decimal> GroupPresentationLakhs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, decimal> GroupAdjustmentsLakhs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CogsMovementRules? CogsMovement { get; set; }
    public Dictionary<string, LoanMaturitySplit> LoanMaturityLakhs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public TradePayablesRules? TradePayables { get; set; }
    public decimal? TradeReceivablesLakhs { get; set; }
    public decimal? InventoriesLakhs { get; set; }
    public decimal? CurrentMaturitiesLakhs { get; set; }
    public decimal? LongTermBorrowingsLakhs { get; set; }
    public decimal? OtherCurrentLiabilitiesLakhs { get; set; }
    public decimal? DepreciationLakhs { get; set; }
    public decimal? ChangesInInventoryLakhs { get; set; }
    public decimal? OtherOperatingRevenueLakhs { get; set; }
    public decimal? OtherIncomeLakhs { get; set; }
    public decimal? FinanceCostsLakhs { get; set; }
    public decimal? LongTermProvisionsLakhs { get; set; }
    public decimal? ShortTermProvisionsLakhs { get; set; }
    public decimal? OtherNonCurrentAssetsLakhs { get; set; }
    public decimal? OtherCurrentAssetsLakhs { get; set; }
    public decimal? PropertyPlantEquipmentLakhs { get; set; }
    public decimal? ReservesAndSurplusLakhs { get; set; }
    public decimal? LoansAndAdvancesCurrentLakhs { get; set; }
    public decimal? LoansAndAdvancesNonCurrentLakhs { get; set; }
    public decimal? OtherExpensesLakhs { get; set; }
    public decimal? CogsLakhs { get; set; }
    public decimal? RevenueFromOperationsLakhs { get; set; }
    public decimal? EmployeeBenefitsLakhs { get; set; }
    public decimal? DeferredTaxLiabilityLakhs { get; set; }
    public decimal? CashAndBankLakhs { get; set; }
    public decimal? InsuranceClaimEarlierYearLakhs { get; set; }
    public decimal? CurrentTaxLakhs { get; set; }
    public decimal? DeferredTaxLakhs { get; set; }
}

public class CogsMovementRules
{
    public List<string> PurchaseGroups { get; set; } = [];
    public decimal OpeningRmPackingLakhs { get; set; }
    public decimal ClosingRmPackingLakhs { get; set; }
}

public class LoanMaturitySplit
{
    public decimal CurrentLakhs { get; set; }
    public decimal NonCurrentLakhs { get; set; }
}

public class TradePayablesRules
{
    public decimal MsmeLakhs { get; set; }
    public decimal CreditorsOthersLakhs { get; set; }
    public decimal BillOfExchangeLakhs { get; set; }
}
