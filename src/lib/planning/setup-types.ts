export type PlanningSetupConstants = {
  bagFamilies: string[];
  poolPurposes: string[];
  winderCategories: string[];
  fabricForms: string[];
  changeoverTiers: string[];
};

export type PlanningInterUnitDefaults = {
  defaultsId?: number | null;
  fibcCompanyName: string;
  defaultFabricSupplyCompany: string | null;
  defaultTransferBufferDays: number;
  autoDetectSulzerFabric: boolean;
  notes: string | null;
  updatedAt: string | null;
};

export type PlanningOrderRoute = {
  routeId?: number | null;
  orderNo: string;
  fibcCompanyName: string;
  fabricSupplyCompanyName: string;
  transferBufferDays: number;
  isInterUnit: boolean;
  routeSource: string;
  autoDetectedReason: string | null;
  updatedAt: string | null;
};

export type PlanningOrderComponentRoute = {
  componentRouteId?: number | null;
  orderNo: string;
  heading: string;
  category: string;
  planningKind: string;
  isLoomEligible: boolean;
  supplyCompanyName: string;
  fibcCompanyName: string;
  transferBufferDays: number;
  isInterUnit: boolean;
  routeSource: string;
  dueDate: string | null;
  gsm: string;
  fabricSize: number | null;
  totalMtr: number | null;
  totalKg: number | null;
};

export type PlanningOrderComponentPlan = {
  orderRoute: PlanningOrderRoute;
  dispatchDate: string | null;
  components: PlanningOrderComponentRoute[];
};

export type PlanningFactoryOption = {
  factoryInfoSrNo: number;
  companyName: string;
  groupName?: string | null;
  isPlanningEnabled: boolean;
  hasLineMaster: boolean;
  hasLoomMaster: boolean;
};

export type PlanningFactoryConfig = {
  configId?: number | null;
  factoryInfoSrNo?: number | null;
  companyName: string;
  isPlanningEnabled: boolean;
  defaultDispatchBufferDays: number;
  defaultRejectionPercent: number;
  notes?: string | null;
  updatedAt?: string | null;
};

export type PlanningLineConfig = {
  lineConfigId?: number | null;
  companyName: string;
  lineNo: number;
  displayName?: string | null;
  erpBagType?: string | null;
  allowedBagFamilies: string[];
  capacityNormal?: number | null;
  capacitySingleDust?: number | null;
  capacityDoubleDust?: number | null;
  capacityTripleDust?: number | null;
  erpBagCapacity?: number | null;
  bufferDaysOverride?: number | null;
  erpBufferDaysCheck?: number | null;
  teamNo?: string | null;
  contractorCode?: number | null;
  isActive: boolean;
  preferenceOrder: number;
  fromErp?: boolean;
};

export type PlanningLoomPool = {
  poolId?: number | null;
  companyName: string;
  loomNo: number;
  erpLoomCode?: string | null;
  erpLoomSpecification?: string | null;
  erpMake?: string | null;
  erpMinSize?: number | null;
  erpMaxSize?: number | null;
  erpIsFrozen: boolean;
  includeInPlanning: boolean;
  poolPurpose: string;
  loomType?: string | null;
  winderCategory: string;
  gsmMin?: number | null;
  gsmMax?: number | null;
  widthMinCm?: number | null;
  widthMaxCm?: number | null;
  notes?: string | null;
};

export type PlanningTeamFactor = {
  factorId?: number | null;
  companyName: string;
  lineNo: number;
  shift?: string | null;
  teamNo: string;
  manualFactor?: number | null;
  autoFactor?: number | null;
  effectiveFactor: number;
  factorSource: string;
  sampleDays: number;
  sampleProductionPcs?: number | null;
  samplePlannedCapacity?: number | null;
  updatedAt?: string | null;
};

export type PlanningBacklog = {
  backlogId: number;
  companyName: string;
  lineNo: number;
  shift: string;
  orderNo: string;
  backlogQty: number;
  reason?: string | null;
  status: string;
  createdAt: string;
  clearedAt?: string | null;
};

export type PlanningDowntime = {
  downtimeId?: number;
  companyName: string;
  planDate: string;
  lineNo: number;
  shift?: string | null;
  reason: string;
  capacityFactor: number;
};

export type PlanningLoomPreferenceChart = {
  chartId?: number;
  companyName: string;
  fabricForm: string;
  gsmMin: number;
  gsmMax: number;
  widthMinCm: number;
  widthMaxCm: number;
  preferenceRank: number;
  loomType: string;
  winderCategory: string;
  changeoverTier: string;
  notes?: string | null;
};

export type RecalculateTeamFactorsResult = {
  success: boolean;
  message: string;
  updatedCount: number;
  factors: PlanningTeamFactor[];
};

function optStr(row: Record<string, unknown>, ...keys: string[]): string | null {
  for (const k of keys) {
    const v = row[k];
    if (v != null && String(v).trim() !== "") return String(v);
  }
  return null;
}

function numField(row: Record<string, unknown>, ...keys: string[]): number {
  for (const k of keys) {
    const v = row[k];
    if (v != null && v !== "") return Number(v);
  }
  return 0;
}

function optNum(row: Record<string, unknown>, ...keys: string[]): number | null {
  for (const k of keys) {
    const v = row[k];
    if (v != null && v !== "") return Number(v);
  }
  return null;
}

function boolField(row: Record<string, unknown>, ...keys: string[]): boolean {
  for (const k of keys) {
    const v = row[k];
    if (v === true || v === false) return v;
    if (v === 1 || v === "1" || v === "true" || v === "yes") return true;
    if (v === 0 || v === "0" || v === "false" || v === "no") return false;
  }
  return false;
}

export function normalizeSetupConstants(data: Record<string, unknown>): PlanningSetupConstants {
  return {
    bagFamilies: (data.bagFamilies ?? data.BagFamilies ?? []) as string[],
    poolPurposes: (data.poolPurposes ?? data.PoolPurposes ?? []) as string[],
    winderCategories: (data.winderCategories ?? data.WinderCategories ?? []) as string[],
    fabricForms: (data.fabricForms ?? data.FabricForms ?? ["Tube", "Flat"]) as string[],
    changeoverTiers: (data.changeoverTiers ?? data.ChangeoverTiers ?? ["Blue", "White"]) as string[],
  };
}

export function normalizeFactoryOption(row: Record<string, unknown>): PlanningFactoryOption {
  return {
    factoryInfoSrNo: numField(row, "factoryInfoSrNo", "FactoryInfoSrNo"),
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    groupName: optStr(row, "groupName", "GroupName"),
    isPlanningEnabled: boolField(row, "isPlanningEnabled", "IsPlanningEnabled"),
    hasLineMaster: boolField(row, "hasLineMaster", "HasLineMaster"),
    hasLoomMaster: boolField(row, "hasLoomMaster", "HasLoomMaster"),
  };
}

export function normalizeFactoryConfig(data: Record<string, unknown>): PlanningFactoryConfig {
  return {
    configId: optNum(data, "configId", "ConfigId"),
    factoryInfoSrNo: optNum(data, "factoryInfoSrNo", "FactoryInfoSrNo"),
    companyName: String(data.companyName ?? data.CompanyName ?? ""),
    isPlanningEnabled: boolField(data, "isPlanningEnabled", "IsPlanningEnabled"),
    defaultDispatchBufferDays: numField(data, "defaultDispatchBufferDays", "DefaultDispatchBufferDays") || 7,
    defaultRejectionPercent: numField(data, "defaultRejectionPercent", "DefaultRejectionPercent") || 2.5,
    notes: optStr(data, "notes", "Notes"),
    updatedAt: optStr(data, "updatedAt", "UpdatedAt"),
  };
}

export function normalizeLineConfig(row: Record<string, unknown>): PlanningLineConfig {
  const families = (row.allowedBagFamilies ?? row.AllowedBagFamilies ?? []) as string[];
  return {
    lineConfigId: optNum(row, "lineConfigId", "LineConfigId"),
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    lineNo: numField(row, "lineNo", "LineNo"),
    displayName: optStr(row, "displayName", "DisplayName"),
    erpBagType: optStr(row, "erpBagType", "ErpBagType"),
    allowedBagFamilies: Array.isArray(families) ? families.map(String) : [],
    capacityNormal: optNum(row, "capacityNormal", "CapacityNormal"),
    capacitySingleDust: optNum(row, "capacitySingleDust", "CapacitySingleDust"),
    capacityDoubleDust: optNum(row, "capacityDoubleDust", "CapacityDoubleDust"),
    capacityTripleDust: optNum(row, "capacityTripleDust", "CapacityTripleDust"),
    erpBagCapacity: optNum(row, "erpBagCapacity", "ErpBagCapacity"),
    bufferDaysOverride: optNum(row, "bufferDaysOverride", "BufferDaysOverride"),
    erpBufferDaysCheck: optNum(row, "erpBufferDaysCheck", "ErpBufferDaysCheck"),
    teamNo: optStr(row, "teamNo", "TeamNo"),
    contractorCode: optNum(row, "contractorCode", "ContractorCode"),
    isActive: boolField(row, "isActive", "IsActive") || row.isActive === undefined,
    preferenceOrder: numField(row, "preferenceOrder", "PreferenceOrder"),
    fromErp: boolField(row, "fromErp", "FromErp"),
  };
}

export function normalizeLoomPool(row: Record<string, unknown>): PlanningLoomPool {
  return {
    poolId: optNum(row, "poolId", "PoolId"),
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    loomNo: numField(row, "loomNo", "LoomNo"),
    erpLoomCode: optStr(row, "erpLoomCode", "ErpLoomCode"),
    erpLoomSpecification: optStr(row, "erpLoomSpecification", "ErpLoomSpecification"),
    erpMake: optStr(row, "erpMake", "ErpMake"),
    erpMinSize: optNum(row, "erpMinSize", "ErpMinSize"),
    erpMaxSize: optNum(row, "erpMaxSize", "ErpMaxSize"),
    erpIsFrozen: boolField(row, "erpIsFrozen", "ErpIsFrozen"),
    includeInPlanning: boolField(row, "includeInPlanning", "IncludeInPlanning"),
    poolPurpose: String(row.poolPurpose ?? row.PoolPurpose ?? "DomesticFibc"),
    loomType: optStr(row, "loomType", "LoomType"),
    winderCategory: String(row.winderCategory ?? row.WinderCategory ?? "Tube"),
    gsmMin: optNum(row, "gsmMin", "GsmMin"),
    gsmMax: optNum(row, "gsmMax", "GsmMax"),
    widthMinCm: optNum(row, "widthMinCm", "WidthMinCm"),
    widthMaxCm: optNum(row, "widthMaxCm", "WidthMaxCm"),
    notes: optStr(row, "notes", "Notes"),
  };
}

export function normalizeTeamFactor(row: Record<string, unknown>): PlanningTeamFactor {
  return {
    factorId: optNum(row, "factorId", "FactorId"),
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    lineNo: numField(row, "lineNo", "LineNo"),
    shift: optStr(row, "shift", "Shift"),
    teamNo: String(row.teamNo ?? row.TeamNo ?? ""),
    manualFactor: optNum(row, "manualFactor", "ManualFactor"),
    autoFactor: optNum(row, "autoFactor", "AutoFactor"),
    effectiveFactor: numField(row, "effectiveFactor", "EffectiveFactor") || 1,
    factorSource: String(row.factorSource ?? row.FactorSource ?? "Default"),
    sampleDays: numField(row, "sampleDays", "SampleDays") || 30,
    sampleProductionPcs: optNum(row, "sampleProductionPcs", "SampleProductionPcs"),
    samplePlannedCapacity: optNum(row, "samplePlannedCapacity", "SamplePlannedCapacity"),
    updatedAt: optStr(row, "updatedAt", "UpdatedAt"),
  };
}

export function normalizeBacklog(row: Record<string, unknown>): PlanningBacklog {
  return {
    backlogId: numField(row, "backlogId", "BacklogId"),
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    lineNo: numField(row, "lineNo", "LineNo"),
    shift: String(row.shift ?? row.Shift ?? ""),
    orderNo: String(row.orderNo ?? row.OrderNo ?? ""),
    backlogQty: numField(row, "backlogQty", "BacklogQty"),
    reason: optStr(row, "reason", "Reason"),
    status: String(row.status ?? row.Status ?? "Open"),
    createdAt: String(row.createdAt ?? row.CreatedAt ?? ""),
    clearedAt: optStr(row, "clearedAt", "ClearedAt"),
  };
}

export function normalizeDowntime(row: Record<string, unknown>): PlanningDowntime {
  const planDate = row.planDate ?? row.PlanDate ?? "";
  return {
    downtimeId: optNum(row, "downtimeId", "DowntimeId") ?? undefined,
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    planDate: String(planDate).slice(0, 10),
    lineNo: numField(row, "lineNo", "LineNo"),
    shift: optStr(row, "shift", "Shift"),
    reason: String(row.reason ?? row.Reason ?? ""),
    capacityFactor: numField(row, "capacityFactor", "CapacityFactor") || 1,
  };
}

export function normalizeLoomPreferenceChart(row: Record<string, unknown>): PlanningLoomPreferenceChart {
  return {
    chartId: optNum(row, "chartId", "ChartId") ?? undefined,
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    fabricForm: String(row.fabricForm ?? row.FabricForm ?? "Tube"),
    gsmMin: numField(row, "gsmMin", "GsmMin"),
    gsmMax: numField(row, "gsmMax", "GsmMax"),
    widthMinCm: numField(row, "widthMinCm", "WidthMinCm"),
    widthMaxCm: numField(row, "widthMaxCm", "WidthMaxCm"),
    preferenceRank: numField(row, "preferenceRank", "PreferenceRank") || 1,
    loomType: String(row.loomType ?? row.LoomType ?? ""),
    winderCategory: String(row.winderCategory ?? row.WinderCategory ?? "Tube"),
    changeoverTier: String(row.changeoverTier ?? row.ChangeoverTier ?? "Blue"),
    notes: optStr(row, "notes", "Notes"),
  };
}

export function normalizeRecalculateResult(data: Record<string, unknown>): RecalculateTeamFactorsResult {
  const factors = (data.factors ?? data.Factors ?? []) as Array<Record<string, unknown>>;
  return {
    success: boolField(data, "success", "Success"),
    message: String(data.message ?? data.Message ?? ""),
    updatedCount: numField(data, "updatedCount", "UpdatedCount"),
    factors: factors.map(normalizeTeamFactor),
  };
}

/** Which loom purpose tags are included in the planning pool (Setup → Loom pool filter). */
export type LoomPoolIncludeMode = "DomesticOnly" | "ExportOnly" | "Both";

export const LOOM_POOL_INCLUDE_MODES: { value: LoomPoolIncludeMode; label: string }[] = [
  { value: "DomesticOnly", label: "Domestic only" },
  { value: "ExportOnly", label: "Export only" },
  { value: "Both", label: "Domestic + export" },
];

export function normalizeLoomPoolPurpose(purpose: string | null | undefined): string {
  const p = (purpose ?? "DomesticFibc").trim();
  return p || "DomesticFibc";
}

/** Whether a loom purpose tag should be included for the given pool filter mode. */
export function loomIncludedForPoolMode(purpose: string | null | undefined, mode: LoomPoolIncludeMode): boolean {
  const p = normalizeLoomPoolPurpose(purpose);
  return mode === "Both"
    ? p === "DomesticFibc" || p === "Export"
    : mode === "ExportOnly"
      ? p === "Export"
      : p === "DomesticFibc";
}

export function applyLoomPoolIncludeMode(
  looms: PlanningLoomPool[],
  mode: LoomPoolIncludeMode,
): PlanningLoomPool[] {
  return looms.map((l) => ({
    ...l,
    includeInPlanning: loomIncludedForPoolMode(l.poolPurpose, mode),
  }));
}

/** Infer filter mode from current include flags (for dropdown initial value after load). */
export function inferLoomPoolIncludeMode(looms: PlanningLoomPool[]): LoomPoolIncludeMode {
  const included = looms.filter((l) => l.includeInPlanning);
  if (included.length === 0) return "DomesticOnly";

  const hasDomestic = included.some((l) => normalizeLoomPoolPurpose(l.poolPurpose) === "DomesticFibc");
  const hasExport = included.some((l) => normalizeLoomPoolPurpose(l.poolPurpose) === "Export");

  if (hasDomestic && hasExport) return "Both";
  if (hasExport && !hasDomestic) return "ExportOnly";
  return "DomesticOnly";
}

export function normalizeInterUnitDefaults(data: Record<string, unknown>): PlanningInterUnitDefaults {
  return {
    defaultsId: optNum(data, "defaultsId", "DefaultsId"),
    fibcCompanyName: String(data.fibcCompanyName ?? data.FibcCompanyName ?? ""),
    defaultFabricSupplyCompany: optStr(data, "defaultFabricSupplyCompany", "DefaultFabricSupplyCompany"),
    defaultTransferBufferDays: numField(data, "defaultTransferBufferDays", "DefaultTransferBufferDays") || 3,
    autoDetectSulzerFabric: data.autoDetectSulzerFabric !== false && data.AutoDetectSulzerFabric !== false,
    notes: optStr(data, "notes", "Notes"),
    updatedAt: optStr(data, "updatedAt", "UpdatedAt"),
  };
}

export function normalizeOrderRoute(data: Record<string, unknown>): PlanningOrderRoute {
  return {
    routeId: optNum(data, "routeId", "RouteId"),
    orderNo: String(data.orderNo ?? data.OrderNo ?? ""),
    fibcCompanyName: String(data.fibcCompanyName ?? data.FibcCompanyName ?? ""),
    fabricSupplyCompanyName: String(data.fabricSupplyCompanyName ?? data.FabricSupplyCompanyName ?? ""),
    transferBufferDays: numField(data, "transferBufferDays", "TransferBufferDays") || 3,
    isInterUnit: boolField(data, "isInterUnit", "IsInterUnit"),
    routeSource: String(data.routeSource ?? data.RouteSource ?? "Default"),
    autoDetectedReason: optStr(data, "autoDetectedReason", "AutoDetectedReason"),
    updatedAt: optStr(data, "updatedAt", "UpdatedAt"),
  };
}

export function normalizeOrderComponentRoute(row: Record<string, unknown>): PlanningOrderComponentRoute {
  return {
    componentRouteId: optNum(row, "componentRouteId", "ComponentRouteId"),
    orderNo: String(row.orderNo ?? row.OrderNo ?? ""),
    heading: String(row.heading ?? row.Heading ?? ""),
    category: String(row.category ?? row.Category ?? "Other"),
    planningKind: String(row.planningKind ?? row.PlanningKind ?? "Other"),
    isLoomEligible: boolField(row, "isLoomEligible", "IsLoomEligible"),
    supplyCompanyName: String(row.supplyCompanyName ?? row.SupplyCompanyName ?? ""),
    fibcCompanyName: String(row.fibcCompanyName ?? row.FibcCompanyName ?? ""),
    transferBufferDays: numField(row, "transferBufferDays", "TransferBufferDays"),
    isInterUnit: boolField(row, "isInterUnit", "IsInterUnit"),
    routeSource: String(row.routeSource ?? row.RouteSource ?? "OrderDefault"),
    dueDate: optStr(row, "dueDate", "DueDate")?.slice(0, 10) ?? null,
    gsm: String(row.gsm ?? row.Gsm ?? ""),
    fabricSize: optNum(row, "fabricSize", "FabricSize"),
    totalMtr: optNum(row, "totalMtr", "TotalMtr"),
    totalKg: optNum(row, "totalKg", "TotalKg"),
  };
}

export function normalizeOrderComponentPlan(data: Record<string, unknown>): PlanningOrderComponentPlan {
  const orderRouteRaw = (data.orderRoute ?? data.OrderRoute ?? {}) as Record<string, unknown>;
  const components = (data.components ?? data.Components ?? []) as Array<Record<string, unknown>>;
  const dispatch = data.dispatchDate ?? data.DispatchDate;
  return {
    orderRoute: normalizeOrderRoute(orderRouteRaw),
    dispatchDate: dispatch ? String(dispatch).slice(0, 10) : null,
    components: components.map(normalizeOrderComponentRoute),
  };
}
