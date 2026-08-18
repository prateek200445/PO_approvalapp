export type IntegratedTimelineMilestone = {
  stage: string;
  label: string;
  startDate: string | null;
  endDate: string | null;
  detail: string | null;
  sortOrder: number;
};

export type IntegratedFabricRequirement = {
  customer: string;
  filePoNo: string;
  bagType: string;
  qty: string | null;
  poDate: string | null;
  targetDate: string | null;
  heading: string;
  gsm: string;
  fabricSize: number | null;
  totalMtr: number | null;
  totalKg: number | null;
};

export type IntegratedLoomAllocation = {
  loomNo: number;
  loomCode: string | null;
  partyName: string | null;
  orderNo: string | null;
  allocationDate: string;
  toDate: string | null;
  reqGsm: number | null;
  size: number | null;
  allocationType: string | null;
};

export type IntegratedFibcPlanLine = {
  companyName: string;
  lineNo: string;
  partyName: string | null;
  orderNo: string | null;
  bagType: string;
  bagTypeLabel: string;
  startDate: string | null;
  completionDate: string | null;
  planDate: string;
  shift: string;
  qty: number;
};

export type IntegratedOrderTimeline = {
  orderNo: string;
  partyName: string | null;
  marketingNo: string | null;
  bagType: string | null;
  quantity: number | null;
  dispatchDate: string | null;
  fabricRequirementDate: string | null;
  loomStartDate: string | null;
  loomEndDate: string | null;
  fibcStartDate: string | null;
  fibcEndDate: string | null;
  fabricBufferDays: number;
  milestones: IntegratedTimelineMilestone[];
  loomAllocations: IntegratedLoomAllocation[];
  fabricRequirements: IntegratedFabricRequirement[];
  fibcPlanLines: IntegratedFibcPlanLine[];
  warnings: string[];
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
    if (typeof value === "number" && !Number.isNaN(value)) return value;
    if (typeof value === "string" && value.trim() !== "") {
      const parsed = Number(value);
      if (!Number.isNaN(parsed)) return parsed;
    }
  }
  return null;
}

function optDate(data: Record<string, unknown>, ...keys: string[]): string | null {
  for (const key of keys) {
    const value = data[key];
    if (value == null || value === "") continue;
    const text = String(value);
    if (text.startsWith("0001-01-01")) return null;
    return text.slice(0, 10);
  }
  return null;
}

export function formatTimelineDate(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return parsed.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function normalizeIntegratedOrderTimeline(data: Record<string, unknown>): IntegratedOrderTimeline {
  const milestones = (data.milestones ?? data.Milestones ?? []) as Array<Record<string, unknown>>;
  const loomAllocations = (data.loomAllocations ?? data.LoomAllocations ?? []) as Array<Record<string, unknown>>;
  const fabricRequirements = (data.fabricRequirements ?? data.FabricRequirements ?? []) as Array<Record<string, unknown>>;
  const fibcPlanLines = (data.fibcPlanLines ?? data.FibcPlanLines ?? []) as Array<Record<string, unknown>>;

  return {
    orderNo: strField(data, "orderNo", "OrderNo"),
    partyName: optStr(data, "partyName", "PartyName"),
    marketingNo: optStr(data, "marketingNo", "MarketingNo"),
    bagType: optStr(data, "bagType", "BagType"),
    quantity: optNum(data, "quantity", "Quantity"),
    dispatchDate: optDate(data, "dispatchDate", "DispatchDate"),
    fabricRequirementDate: optDate(data, "fabricRequirementDate", "FabricRequirementDate"),
    loomStartDate: optDate(data, "loomStartDate", "LoomStartDate"),
    loomEndDate: optDate(data, "loomEndDate", "LoomEndDate"),
    fibcStartDate: optDate(data, "fibcStartDate", "FibcStartDate"),
    fibcEndDate: optDate(data, "fibcEndDate", "FibcEndDate"),
    fabricBufferDays: numField(data, "fabricBufferDays", "FabricBufferDays"),
    warnings: Array.isArray(data.warnings ?? data.Warnings) ? (data.warnings ?? data.Warnings).map(String) : [],
    milestones: milestones.map((m) => ({
      stage: strField(m, "stage", "Stage"),
      label: strField(m, "label", "Label"),
      startDate: optDate(m, "startDate", "StartDate"),
      endDate: optDate(m, "endDate", "EndDate"),
      detail: optStr(m, "detail", "Detail"),
      sortOrder: numField(m, "sortOrder", "SortOrder"),
    })),
    loomAllocations: loomAllocations.map((a) => ({
      loomNo: numField(a, "loomNo", "LoomNo"),
      loomCode: optStr(a, "loomCode", "LoomCode"),
      partyName: optStr(a, "partyName", "PartyName"),
      orderNo: optStr(a, "orderNo", "OrderNo"),
      allocationDate: strField(a, "allocationDate", "AllocationDate"),
      toDate: optDate(a, "toDate", "ToDate"),
      reqGsm: optNum(a, "reqGsm", "ReqGsm"),
      size: optNum(a, "size", "Size"),
      allocationType: optStr(a, "allocationType", "AllocationType"),
    })),
    fabricRequirements: fabricRequirements.map((f) => ({
      customer: strField(f, "customer", "Customer"),
      filePoNo: strField(f, "filePoNo", "FilePoNo"),
      bagType: strField(f, "bagType", "BagType"),
      qty: optStr(f, "qty", "Qty"),
      poDate: optDate(f, "poDate", "PoDate"),
      targetDate: optDate(f, "targetDate", "TargetDate"),
      heading: strField(f, "heading", "Heading"),
      gsm: strField(f, "gsm", "Gsm"),
      fabricSize: optNum(f, "fabricSize", "FabricSize"),
      totalMtr: optNum(f, "totalMtr", "TotalMtr"),
      totalKg: optNum(f, "totalKg", "TotalKg"),
    })),
    fibcPlanLines: fibcPlanLines.map((l) => ({
      companyName: strField(l, "companyName", "CompanyName"),
      lineNo: strField(l, "lineNo", "LineNo"),
      partyName: optStr(l, "partyName", "PartyName"),
      orderNo: optStr(l, "orderNo", "OrderNo"),
      bagType: strField(l, "bagType", "BagType"),
      bagTypeLabel: strField(l, "bagTypeLabel", "BagTypeLabel"),
      startDate: optDate(l, "startDate", "StartDate"),
      completionDate: optDate(l, "completionDate", "CompletionDate"),
      planDate: strField(l, "planDate", "PlanDate"),
      shift: strField(l, "shift", "Shift"),
      qty: numField(l, "qty", "Qty"),
    })),
  };
}
