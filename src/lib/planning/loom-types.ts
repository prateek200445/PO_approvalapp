export type LoomPlanningConfig = {
  defaultCompanyName: string;
  readOnly: boolean;
  previewOnly: boolean;
  confirmSaveEnabled: boolean;
  replaceExistingEnabled: boolean;
  fabricBufferDays: number;
  maxPlanningHorizonDays: number;
  maxDaysPerLoomSegment: number;
  maxChangeoversPerDay: number;
  defaultEfficiency: number;
};

export type LoomMaster = {
  loomNo: number;
  companyName: string;
  loomCode: string | null;
  loomSpecification: string | null;
  make: string | null;
  modelNo: string | null;
  minSize: number | null;
  maxSize: number | null;
  creelCapacity: string | null;
  isFrozen: boolean;
};

export type LoomAllocationGridItem = {
  allocationId: number;
  loomNo: number;
  companyName: string;
  loomCode: string | null;
  loomSpecification: string | null;
  partyName: string | null;
  orderNo: string | null;
  allocationDate: string;
  toDate: string | null;
  reqGsm: number | null;
  size: number | null;
  allocationType: string | null;
  color: string | null;
  sector: string | null;
  remarks: string | null;
  isActive: boolean;
};

export type LoomAllocationGridResult = {
  items: LoomAllocationGridItem[];
  dateFrom: string;
  dateTo: string;
  companyName: string;
  totalRows: number;
  activeLoomCount: number;
};

export type LoomFabricRequirement = {
  customer: string;
  filePoNo: string;
  bagType: string;
  qty: number | null;
  poDate: string | null;
  targetDate: string | null;
  heading: string;
  gsm: string;
  fabricSize: number | null;
  totalMtr: number | null;
  totalKg: number | null;
  category: string;
  planningKind: string;
  isLoomEligible: boolean;
};

export type LoomOrderAllocationLine = {
  loomNo: number;
  loomCode: string | null;
  partyName: string | null;
  orderNo: string | null;
  allocationDate: string;
  toDate: string | null;
  reqGsm: number | null;
  size: number | null;
  allocationType: string | null;
  color: string | null;
  sector: string | null;
  remarks: string | null;
};

export type LoomOrderPlanDetail = {
  orderNo: string;
  allocations: LoomOrderAllocationLine[];
  fabricRequirements: LoomFabricRequirement[];
};

export type LoomOrderContext = {
  orderNo: string;
  partyName: string | null;
  marketingNo: string | null;
  dispatchDate: string | null;
  quantity: number | null;
  bagType: string | null;
  existingAllocationCount: number;
};

export type LoomOrderAllotmentContext = LoomOrderContext & {
  fabricRequirementDate: string | null;
  fabricLines: LoomFabricRequirement[];
  loomEligibleLines: LoomFabricRequirement[];
  accessoryLines: LoomFabricRequirement[];
};

export type LoomAllotmentRequest = {
  orderNo: string;
  companyName?: string;
  partyName?: string;
  heading?: string;
  reqGsm: number;
  size: number;
  requiredMeters: number;
  fabricRequirementDate?: string;
  color?: string;
  sector?: string;
  replaceExisting?: boolean;
};

export type LoomProposedSegment = {
  loomNo: number;
  loomCode: string | null;
  loomSpecification: string | null;
  fromDate: string;
  toDate: string;
  plannedMeters: number;
  metersPerDay: number;
  runDays: number;
  allotmentCase: string;
  caseLabel: string;
  formulaId: number | null;
  reqGsm: number;
  size: number;
  heading?: string | null;
};

export type LoomOrderShiftDisplacement = {
  allocationId: number | null;
  loomNo: number;
  orderNo: string;
  partyName: string | null;
  fromDate: string;
  toDate: string;
  newFromDate: string;
  newToDate: string;
  reason: string;
};

export type LoomAllotmentResult = {
  success: boolean;
  fullyAllotted: boolean;
  message: string;
  orderNo: string;
  heading?: string | null;
  reqGsm: number;
  size: number;
  requiredMeters: number;
  allottedMeters: number;
  metersPerDay: number;
  fabricBufferDays: number;
  fabricRequirementDate: string | null;
  fabricCompletionDate: string | null;
  earliestStartDate: string | null;
  warnings: string[];
  proposedSegments: LoomProposedSegment[];
  displacements: LoomOrderShiftDisplacement[];
};

export type LoomAllotmentConfirmResult = LoomAllotmentResult & {
  saved: boolean;
  rowsInserted: number;
  rowsDeleted: number;
  ordersShifted: number;
};

export type LoomComponentBatchResult = {
  success: boolean;
  message: string;
  orderNo: string;
  loomEligibleCount: number;
  fullyAllottedCount: number;
  warnings: string[];
  components: LoomAllotmentResult[];
  savedCount?: number;
  rowsInserted?: number;
  confirmed?: boolean;
};

export type LoomProductionMeter = {
  loomNo: number;
  loomCode: string | null;
  planDate: string;
  prodMetersA: number;
  prodMetersB: number;
  reqGsm: number | null;
  size: number | null;
  orderNo: string | null;
  partyName: string | null;
};

export type LoomProductionMeterGridResult = {
  items: LoomProductionMeter[];
  dateFrom: string;
  dateTo: string;
  companyName: string;
};

export type LoomPpmSpec = {
  loomType: string;
  gsmFrom: number;
  gsmTo: number;
  widthFrom: number;
  widthTo: number;
  ppm: number;
};

function strField(data: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = data[key];
    if (value != null && value !== "") return String(value);
  }
  return "";
}

function optStr(data: Record<string, unknown>, ...keys: string[]): string | null {
  const value = strField(data, ...keys);
  return value || null;
}

function numField(data: Record<string, unknown>, ...keys: string[]): number {
  for (const key of keys) {
    const value = data[key];
    if (typeof value === "number" && !Number.isNaN(value)) return value;
    if (typeof value === "string" && value.trim() !== "") {
      const parsed = Number(value);
      if (!Number.isNaN(parsed)) return parsed;
    }
  }
  return 0;
}

function optNum(data: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const value = data[key];
    if (value == null || value === "") continue;
    if (typeof value === "number" && !Number.isNaN(value)) return value;
    if (typeof value === "string") {
      const parsed = Number(value);
      if (!Number.isNaN(parsed)) return parsed;
    }
  }
  return null;
}

function optDate(data: Record<string, unknown>, ...keys: string[]): string | null {
  const raw = optStr(data, ...keys);
  if (!raw) return null;
  const parsed = new Date(raw);
  if (Number.isNaN(parsed.getTime()) || parsed.getFullYear() < 2000) return null;
  return parsed.toISOString();
}

export function toInputDate(value: Date): string {
  const y = value.getFullYear();
  const m = String(value.getMonth() + 1).padStart(2, "0");
  const d = String(value.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function defaultPlanningDateFrom(): string {
  const d = new Date();
  d.setDate(d.getDate() - 30);
  return toInputDate(d);
}

export function formatPlanDate(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return parsed.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function normalizeLoomPlanningConfig(data: Record<string, unknown>): LoomPlanningConfig {
  return {
    defaultCompanyName: strField(data, "defaultCompanyName", "DefaultCompanyName"),
    readOnly: Boolean(data.readOnly ?? data.ReadOnly ?? true),
    previewOnly: Boolean(data.previewOnly ?? data.PreviewOnly ?? true),
    confirmSaveEnabled: Boolean(data.confirmSaveEnabled ?? data.ConfirmSaveEnabled),
    replaceExistingEnabled: Boolean(data.replaceExistingEnabled ?? data.ReplaceExistingEnabled),
    fabricBufferDays: numField(data, "fabricBufferDays", "FabricBufferDays") || 5,
    maxPlanningHorizonDays: numField(data, "maxPlanningHorizonDays", "MaxPlanningHorizonDays") || 30,
    maxDaysPerLoomSegment: numField(data, "maxDaysPerLoomSegment", "MaxDaysPerLoomSegment") || 14,
    maxChangeoversPerDay: numField(data, "maxChangeoversPerDay", "MaxChangeoversPerDay") || 4,
    defaultEfficiency: numField(data, "defaultEfficiency", "DefaultEfficiency") || 0.8,
  };
}

export function normalizeLoomMaster(data: Record<string, unknown>): LoomMaster {
  return {
    loomNo: numField(data, "loomNo", "LoomNo"),
    companyName: strField(data, "companyName", "CompanyName"),
    loomCode: optStr(data, "loomCode", "LoomCode"),
    loomSpecification: optStr(data, "loomSpecification", "LoomSpecification"),
    make: optStr(data, "make", "Make"),
    modelNo: optStr(data, "modelNo", "ModelNo"),
    minSize: optNum(data, "minSize", "MinSize"),
    maxSize: optNum(data, "maxSize", "MaxSize"),
    creelCapacity: optStr(data, "creelCapacity", "CreelCapacity"),
    isFrozen: Boolean(data.isFrozen ?? data.IsFrozen),
  };
}

export function normalizeLoomAllocationGridItem(data: Record<string, unknown>): LoomAllocationGridItem {
  return {
    allocationId: numField(data, "allocationId", "AllocationId"),
    loomNo: numField(data, "loomNo", "LoomNo"),
    companyName: strField(data, "companyName", "CompanyName"),
    loomCode: optStr(data, "loomCode", "LoomCode"),
    loomSpecification: optStr(data, "loomSpecification", "LoomSpecification"),
    partyName: optStr(data, "partyName", "PartyName"),
    orderNo: optStr(data, "orderNo", "OrderNo"),
    allocationDate: strField(data, "allocationDate", "AllocationDate"),
    toDate: optDate(data, "toDate", "ToDate"),
    reqGsm: optNum(data, "reqGsm", "ReqGsm"),
    size: optNum(data, "size", "Size"),
    allocationType: optStr(data, "allocationType", "AllocationType"),
    color: optStr(data, "color", "Color"),
    sector: optStr(data, "sector", "Sector"),
    remarks: optStr(data, "remarks", "Remarks"),
    isActive: Boolean(data.isActive ?? data.IsActive ?? true),
  };
}

export function normalizeLoomAllocationGridResult(data: Record<string, unknown>): LoomAllocationGridResult {
  const items = (data.items ?? data.Items ?? []) as Array<Record<string, unknown>>;
  return {
    items: items.map(normalizeLoomAllocationGridItem),
    dateFrom: strField(data, "dateFrom", "DateFrom"),
    dateTo: strField(data, "dateTo", "DateTo"),
    companyName: strField(data, "companyName", "CompanyName"),
    totalRows: numField(data, "totalRows", "TotalRows"),
    activeLoomCount: numField(data, "activeLoomCount", "ActiveLoomCount"),
  };
}

export function normalizeLoomOrderContext(data: Record<string, unknown>): LoomOrderContext {
  return {
    orderNo: strField(data, "orderNo", "OrderNo"),
    partyName: optStr(data, "partyName", "PartyName"),
    marketingNo: optStr(data, "marketingNo", "MarketingNo"),
    dispatchDate: optDate(data, "dispatchDate", "DispatchDate"),
    quantity: optNum(data, "quantity", "Quantity"),
    bagType: optStr(data, "bagType", "BagType"),
    existingAllocationCount: numField(data, "existingAllocationCount", "ExistingAllocationCount"),
  };
}

function mapFabricLine(data: Record<string, unknown>): LoomFabricRequirement {
  return {
    customer: strField(data, "customer", "Customer"),
    filePoNo: strField(data, "filePoNo", "FilePoNo"),
    bagType: strField(data, "bagType", "BagType"),
    qty: optNum(data, "qty", "Qty"),
    poDate: optDate(data, "poDate", "PoDate"),
    targetDate: optDate(data, "targetDate", "TargetDate"),
    heading: strField(data, "heading", "Heading"),
    gsm: strField(data, "gsm", "Gsm"),
    fabricSize: optNum(data, "fabricSize", "FabricSize"),
    totalMtr: optNum(data, "totalMtr", "TotalMtr"),
    totalKg: optNum(data, "totalKg", "TotalKg"),
    category: strField(data, "category", "Category") || "Other",
    planningKind: strField(data, "planningKind", "PlanningKind") || "Other",
    isLoomEligible: Boolean(data.isLoomEligible ?? data.IsLoomEligible),
  };
}

export function normalizeLoomAllotmentContext(data: Record<string, unknown>): LoomOrderAllotmentContext {
  const fabricLines = (data.fabricLines ?? data.FabricLines ?? []) as Array<Record<string, unknown>>;
  const loomEligible = (data.loomEligibleLines ?? data.LoomEligibleLines ?? []) as Array<Record<string, unknown>>;
  const accessories = (data.accessoryLines ?? data.AccessoryLines ?? []) as Array<Record<string, unknown>>;
  const base = normalizeLoomOrderContext(data);
  const mappedLines = fabricLines.map(mapFabricLine);
  return {
    ...base,
    fabricRequirementDate: optDate(data, "fabricRequirementDate", "FabricRequirementDate"),
    fabricLines: mappedLines,
    loomEligibleLines: loomEligible.length > 0 ? loomEligible.map(mapFabricLine) : mappedLines.filter((l) => l.isLoomEligible),
    accessoryLines: accessories.length > 0 ? accessories.map(mapFabricLine) : mappedLines.filter((l) => l.planningKind === "Accessory"),
  };
}

export function normalizeLoomAllotmentResult(data: Record<string, unknown>): LoomAllotmentResult {
  const segments = (data.proposedSegments ?? data.ProposedSegments ?? []) as Array<Record<string, unknown>>;
  const displacements = (data.displacements ?? data.Displacements ?? []) as Array<Record<string, unknown>>;
  return {
    success: Boolean(data.success ?? data.Success),
    fullyAllotted: Boolean(data.fullyAllotted ?? data.FullyAllotted),
    message: strField(data, "message", "Message"),
    orderNo: strField(data, "orderNo", "OrderNo"),
    heading: optStr(data, "heading", "Heading"),
    reqGsm: numField(data, "reqGsm", "ReqGsm"),
    size: numField(data, "size", "Size"),
    requiredMeters: numField(data, "requiredMeters", "RequiredMeters"),
    allottedMeters: numField(data, "allottedMeters", "AllottedMeters"),
    metersPerDay: numField(data, "metersPerDay", "MetersPerDay"),
    fabricBufferDays: numField(data, "fabricBufferDays", "FabricBufferDays"),
    fabricRequirementDate: optDate(data, "fabricRequirementDate", "FabricRequirementDate"),
    fabricCompletionDate: optDate(data, "fabricCompletionDate", "FabricCompletionDate"),
    earliestStartDate: optDate(data, "earliestStartDate", "EarliestStartDate"),
    warnings: Array.isArray(data.warnings ?? data.Warnings) ? (data.warnings ?? data.Warnings).map(String) : [],
    proposedSegments: segments.map((s) => ({
      loomNo: numField(s, "loomNo", "LoomNo"),
      loomCode: optStr(s, "loomCode", "LoomCode"),
      loomSpecification: optStr(s, "loomSpecification", "LoomSpecification"),
      fromDate: strField(s, "fromDate", "FromDate"),
      toDate: strField(s, "toDate", "ToDate"),
      plannedMeters: numField(s, "plannedMeters", "PlannedMeters"),
      metersPerDay: numField(s, "metersPerDay", "MetersPerDay"),
      runDays: numField(s, "runDays", "RunDays"),
      allotmentCase: strField(s, "allotmentCase", "AllotmentCase"),
      caseLabel: strField(s, "caseLabel", "CaseLabel"),
      formulaId: optNum(s, "formulaId", "FormulaId"),
      reqGsm: numField(s, "reqGsm", "ReqGsm"),
      size: numField(s, "size", "Size"),
      heading: optStr(s, "heading", "Heading"),
    })),
    displacements: displacements.map((d) => ({
      allocationId: optNum(d, "allocationId", "AllocationId"),
      loomNo: numField(d, "loomNo", "LoomNo"),
      orderNo: strField(d, "orderNo", "OrderNo"),
      partyName: optStr(d, "partyName", "PartyName"),
      fromDate: strField(d, "fromDate", "FromDate"),
      toDate: strField(d, "toDate", "ToDate"),
      newFromDate: strField(d, "newFromDate", "NewFromDate"),
      newToDate: strField(d, "newToDate", "NewToDate"),
      reason: strField(d, "reason", "Reason"),
    })),
  };
}

export function normalizeLoomAllotmentConfirmResult(data: Record<string, unknown>): LoomAllotmentConfirmResult {
  const base = normalizeLoomAllotmentResult(data);
  return {
    ...base,
    saved: Boolean(data.saved ?? data.Saved),
    rowsInserted: numField(data, "rowsInserted", "RowsInserted"),
    rowsDeleted: numField(data, "rowsDeleted", "RowsDeleted"),
    ordersShifted: numField(data, "ordersShifted", "OrdersShifted"),
  };
}

export function normalizeLoomComponentBatchResult(data: Record<string, unknown>): LoomComponentBatchResult {
  const components = (data.components ?? data.Components ?? []) as Array<Record<string, unknown>>;
  const warnings = (data.warnings ?? data.Warnings ?? []) as unknown;
  return {
    success: Boolean(data.success ?? data.Success),
    message: strField(data, "message", "Message"),
    orderNo: strField(data, "orderNo", "OrderNo"),
    loomEligibleCount: numField(data, "loomEligibleCount", "LoomEligibleCount"),
    fullyAllottedCount: numField(data, "fullyAllottedCount", "FullyAllottedCount"),
    warnings: Array.isArray(warnings) ? warnings.map(String) : [],
    components: components.map(normalizeLoomAllotmentResult),
    savedCount: numField(data, "savedCount", "SavedCount"),
    rowsInserted: numField(data, "rowsInserted", "RowsInserted"),
    confirmed: Boolean(data.confirmed ?? data.Confirmed),
  };
}
