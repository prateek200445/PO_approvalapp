namespace POApprovalAPI.Planning.Setup.Models;

public sealed class PlanningFactoryOptionDto
{
    public int FactoryInfoSrNo { get; set; }
    public string CompanyName { get; set; } = "";
    public string? GroupName { get; set; }
    public bool IsPlanningEnabled { get; set; }
    public bool HasLineMaster { get; set; }
    public bool HasLoomMaster { get; set; }
}

public sealed class PlanningFactoryConfigDto
{
    public int? ConfigId { get; set; }
    public int? FactoryInfoSrNo { get; set; }
    public string CompanyName { get; set; } = "";
    public bool IsPlanningEnabled { get; set; } = true;
    public int DefaultDispatchBufferDays { get; set; } = 7;
    public double DefaultRejectionPercent { get; set; } = 2.5;
    public string? Notes { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class UpsertPlanningFactoryConfigRequest
{
    public int? FactoryInfoSrNo { get; set; }
    public string CompanyName { get; set; } = "";
    public bool IsPlanningEnabled { get; set; } = true;
    public int DefaultDispatchBufferDays { get; set; } = 7;
    public double DefaultRejectionPercent { get; set; } = 2.5;
    public string? Notes { get; set; }
}

public sealed class PlanningLineConfigDto
{
    public int? LineConfigId { get; set; }
    public string CompanyName { get; set; } = "";
    public int LineNo { get; set; }
    public string? DisplayName { get; set; }
    public string? ErpBagType { get; set; }
    public IReadOnlyList<string> AllowedBagFamilies { get; set; } = Array.Empty<string>();
    public int? CapacityNormal { get; set; }
    public int? CapacitySingleDust { get; set; }
    public int? CapacityDoubleDust { get; set; }
    public int? CapacityTripleDust { get; set; }
    public int? ErpBagCapacity { get; set; }
    public int? BufferDaysOverride { get; set; }
    public int? ErpBufferDaysCheck { get; set; }
    public string? TeamNo { get; set; }
    public int? ContractorCode { get; set; }
    public bool IsActive { get; set; } = true;
    public int PreferenceOrder { get; set; }
    public bool FromErp { get; set; }
}

public sealed class SavePlanningLineConfigsRequest
{
    public string CompanyName { get; set; } = "";
    public IReadOnlyList<PlanningLineConfigDto> Lines { get; set; } = Array.Empty<PlanningLineConfigDto>();
}

public sealed class PlanningLoomPoolDto
{
    public int? PoolId { get; set; }
    public string CompanyName { get; set; } = "";
    public int LoomNo { get; set; }
    public string? ErpLoomCode { get; set; }
    public string? ErpLoomSpecification { get; set; }
    public string? ErpMake { get; set; }
    public double? ErpMinSize { get; set; }
    public double? ErpMaxSize { get; set; }
    public bool ErpIsFrozen { get; set; }
    public bool IncludeInPlanning { get; set; }
    public string PoolPurpose { get; set; } = "DomesticFibc";
    public string? LoomType { get; set; }
    public string WinderCategory { get; set; } = "Tube";
    public double? GsmMin { get; set; }
    public double? GsmMax { get; set; }
    public double? WidthMinCm { get; set; }
    public double? WidthMaxCm { get; set; }
    public string? Notes { get; set; }
}

public sealed class SavePlanningLoomPoolRequest
{
    public string CompanyName { get; set; } = "";
    public IReadOnlyList<PlanningLoomPoolDto> Looms { get; set; } = Array.Empty<PlanningLoomPoolDto>();
}

public sealed class PlanningTeamFactorDto
{
    public int? FactorId { get; set; }
    public string CompanyName { get; set; } = "";
    public int LineNo { get; set; }
    public string? Shift { get; set; }
    public string TeamNo { get; set; } = "";
    public double? ManualFactor { get; set; }
    public double? AutoFactor { get; set; }
    public double EffectiveFactor { get; set; } = 1.0;
    public string FactorSource { get; set; } = "Default";
    public int SampleDays { get; set; } = 30;
    public double? SampleProductionPcs { get; set; }
    public double? SamplePlannedCapacity { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class SavePlanningTeamFactorRequest
{
    public string CompanyName { get; set; } = "";
    public IReadOnlyList<PlanningTeamFactorDto> Factors { get; set; } = Array.Empty<PlanningTeamFactorDto>();
}

public sealed class RecalculateTeamFactorsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int UpdatedCount { get; set; }
    public IReadOnlyList<PlanningTeamFactorDto> Factors { get; set; } = Array.Empty<PlanningTeamFactorDto>();
}

public sealed class PlanningBacklogDto
{
    public int BacklogId { get; set; }
    public string CompanyName { get; set; } = "";
    public int LineNo { get; set; }
    public string Shift { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public double BacklogQty { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
}

public sealed class CreatePlanningBacklogRequest
{
    public string CompanyName { get; set; } = "";
    public int LineNo { get; set; }
    public string Shift { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public double BacklogQty { get; set; }
    public string? Reason { get; set; }
}

public sealed class PlanningDowntimeDto
{
    public int DowntimeId { get; set; }
    public string CompanyName { get; set; } = "";
    public DateTime PlanDate { get; set; }
    public int LineNo { get; set; }
    public string? Shift { get; set; }
    public string Reason { get; set; } = "";
    /// <summary>1 = full capacity, 0 = line down, 0.5 = half shift.</summary>
    public double CapacityFactor { get; set; } = 1.0;
}

public sealed class SavePlanningDowntimeRequest
{
    public string CompanyName { get; set; } = "";
    public IReadOnlyList<PlanningDowntimeDto> Entries { get; set; } = Array.Empty<PlanningDowntimeDto>();
}

public sealed class PlanningLoomPreferenceChartDto
{
    public int ChartId { get; set; }
    public string CompanyName { get; set; } = "";
    public string FabricForm { get; set; } = "Tube";
    public double GsmMin { get; set; }
    public double GsmMax { get; set; }
    public double WidthMinCm { get; set; }
    public double WidthMaxCm { get; set; }
    public int PreferenceRank { get; set; } = 1;
    public string LoomType { get; set; } = "";
    public string WinderCategory { get; set; } = "";
    public string ChangeoverTier { get; set; } = "Blue";
    public string? Notes { get; set; }
}

public sealed class SavePlanningLoomPreferenceChartRequest
{
    public string CompanyName { get; set; } = "";
    public IReadOnlyList<PlanningLoomPreferenceChartDto> Rows { get; set; } = Array.Empty<PlanningLoomPreferenceChartDto>();
}

public static class PlanningSetupConstants
{
    public static readonly string[] BagFamilies = ["UPanel", "Buffle", "Circular"];
    public static readonly string[] PoolPurposes = ["DomesticFibc", "Export", "Other", "Maintenance"];
    public static readonly string[] WinderCategories = ["Tube", "FlatDouble", "FlatTriple"];
    public static readonly string[] AllotmentModes = ["OrderWise", "SlotWise"];
    public static readonly string[] DustLevels = ["Normal", "Single", "Double", "Triple"];
    public static readonly string[] FabricForms = ["Tube", "Flat"];
    public static readonly string[] ChangeoverTiers = ["Blue", "White"];
}
