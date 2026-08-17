export type FibcPlanningConfig = {
  defaultCompanyName: string;
  dispatchBufferDays: number;
  shiftPreference: string[];
  activeShifts: string[];
  allotmentEnabled: boolean;
  previewOnly: boolean;
};

export type FibcOrderAllotmentContext = {
  orderNo: string;
  partyName: string | null;
  marketingNo: string | null;
  dispatchDate: string | null;
  quantity: number | null;
  bagType: string | null;
  bagTypeLabel: string;
};

export type FibcAllotmentRequest = {
  orderNo: string;
  companyName?: string;
  dispatchDate?: string;
  quantity?: number;
  bagType?: string;
};

export type FibcAllotmentResult = {
  success: boolean;
  message: string;
  orderNo: string;
  bagType: string;
  bagTypeLabel: string;
  quantity: number;
  capacityPerShift: number;
  slotsRequired: number;
  bufferDays: number;
  dispatchDate: string | null;
  targetCompletionDate: string | null;
  warnings: string[];
  proposedSlots: FibcSlotGridItem[];
};

export type FibcLineConfig = {
  lineNo: number;
  companyName: string;
  bagType: string;
  bagTypeLabel: string;
  isDoubleDust: boolean;
  isTripleDust: boolean;
  bagCapacity: number;
  sortOrder: number;
  bufferDaysCheck: number;
};

export type FibcSlotGridItem = {
  companyName: string;
  bagType: string;
  bagTypeLabel: string;
  partyName: string | null;
  orderNo: string | null;
  lineNo: string;
  planDate: string;
  allotted: number;
  capacity: number;
  remaining: number;
  allocatedPercent: number | null;
  shift: string;
  marketingNo: string | null;
  transId: number | null;
  efficiency: number | null;
  utilizationPercent: number;
  occupancyStatus: "free" | "partial" | "full";
};

export type FibcSlotGridResult = {
  items: FibcSlotGridItem[];
  dateFrom: string;
  dateTo: string;
  companyName: string;
  totalSlots: number;
  occupiedSlots: number;
};

export type FibcOrderPlanLine = {
  companyName: string;
  lineNo: string;
  partyName: string | null;
  orderNo: string | null;
  poQty: number | null;
  bagType: string;
  bagTypeLabel: string;
  startDate: string | null;
  completionDate: string | null;
  qty: number;
  planDate: string;
  shift: string;
  allocatedPercent: number | null;
};

export type FibcFabricRequirement = {
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

export type FibcOrderPlanDetail = {
  orderNo: string;
  planLines: FibcOrderPlanLine[];
  fabricRequirements: FibcFabricRequirement[];
};

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
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function shiftBadgeClass(shift: string): string {
  switch (shift.toUpperCase()) {
    case "A":
      return "bg-rose-500/15 text-rose-700 ring-rose-500/25 dark:text-rose-300";
    case "B":
      return "bg-emerald-500/15 text-emerald-700 ring-emerald-500/25 dark:text-emerald-300";
    case "C":
      return "bg-sky-500/15 text-sky-700 ring-sky-500/25 dark:text-sky-300";
    default:
      return "bg-muted text-muted-foreground";
  }
}

export function occupancyBadgeClass(status: FibcSlotGridItem["occupancyStatus"]): string {
  switch (status) {
    case "full":
      return "bg-destructive/15 text-destructive";
    case "partial":
      return "bg-amber-500/15 text-amber-800 dark:text-amber-300";
    default:
      return "bg-secondary text-muted-foreground";
  }
}

function numField(row: Record<string, unknown>, camel: string, pascal: string): number {
  const v = row[camel] ?? row[pascal];
  return typeof v === "number" ? v : Number(v ?? 0);
}

function optNum(row: Record<string, unknown>, camel: string, pascal: string): number | null {
  const v = row[camel] ?? row[pascal];
  if (v === null || v === undefined || v === "") return null;
  const n = typeof v === "number" ? v : Number(v);
  return Number.isNaN(n) ? null : n;
}

function strField(row: Record<string, unknown>, camel: string, pascal: string): string {
  return String(row[camel] ?? row[pascal] ?? "").trim();
}

function optStr(row: Record<string, unknown>, camel: string, pascal: string): string | null {
  const s = strField(row, camel, pascal);
  return s || null;
}

export function normalizeLineConfig(row: Record<string, unknown>): FibcLineConfig {
  return {
    lineNo: numField(row, "lineNo", "LineNo"),
    companyName: strField(row, "companyName", "CompanyName"),
    bagType: strField(row, "bagType", "BagType"),
    bagTypeLabel: strField(row, "bagTypeLabel", "BagTypeLabel"),
    isDoubleDust: Boolean(row.isDoubleDust ?? row.IsDoubleDust),
    isTripleDust: Boolean(row.isTripleDust ?? row.IsTripleDust),
    bagCapacity: numField(row, "bagCapacity", "BagCapacity"),
    sortOrder: numField(row, "sortOrder", "SortOrder"),
    bufferDaysCheck: numField(row, "bufferDaysCheck", "BufferDaysCheck"),
  };
}

export function normalizeSlotGridItem(row: Record<string, unknown>): FibcSlotGridItem {
  const status = strField(row, "occupancyStatus", "OccupancyStatus") as FibcSlotGridItem["occupancyStatus"];
  return {
    companyName: strField(row, "companyName", "CompanyName"),
    bagType: strField(row, "bagType", "BagType"),
    bagTypeLabel: strField(row, "bagTypeLabel", "BagTypeLabel"),
    partyName: optStr(row, "partyName", "PartyName"),
    orderNo: optStr(row, "orderNo", "OrderNo"),
    lineNo: strField(row, "lineNo", "LineNo"),
    planDate: strField(row, "planDate", "PlanDate"),
    allotted: numField(row, "allotted", "Allotted"),
    capacity: numField(row, "capacity", "Capacity"),
    remaining: numField(row, "remaining", "Remaining"),
    allocatedPercent: optNum(row, "allocatedPercent", "AllocatedPercent"),
    shift: strField(row, "shift", "Shift"),
    marketingNo: optStr(row, "marketingNo", "MarketingNo"),
    transId: optNum(row, "transId", "TransId"),
    efficiency: optNum(row, "efficiency", "Efficiency"),
    utilizationPercent: numField(row, "utilizationPercent", "UtilizationPercent"),
    occupancyStatus: status === "full" || status === "partial" ? status : "free",
  };
}

export function normalizeSlotGridResult(data: Record<string, unknown>): FibcSlotGridResult {
  const items = (data.items ?? data.Items ?? []) as Array<Record<string, unknown>>;
  return {
    items: items.map(normalizeSlotGridItem),
    dateFrom: strField(data, "dateFrom", "DateFrom"),
    dateTo: strField(data, "dateTo", "DateTo"),
    companyName: strField(data, "companyName", "CompanyName"),
    totalSlots: numField(data, "totalSlots", "TotalSlots"),
    occupiedSlots: numField(data, "occupiedSlots", "OccupiedSlots"),
  };
}

export function normalizePlanningConfig(data: Record<string, unknown>): FibcPlanningConfig {
  const shiftPref = (data.shiftPreference ?? data.ShiftPreference ?? []) as unknown;
  const activeShifts = (data.activeShifts ?? data.ActiveShifts ?? []) as unknown;
  return {
    defaultCompanyName: strField(data, "defaultCompanyName", "DefaultCompanyName"),
    dispatchBufferDays: numField(data, "dispatchBufferDays", "DispatchBufferDays"),
    shiftPreference: Array.isArray(shiftPref) ? shiftPref.map(String) : [],
    activeShifts: Array.isArray(activeShifts) ? activeShifts.map(String) : [],
    allotmentEnabled: Boolean(data.allotmentEnabled ?? data.AllotmentEnabled),
    previewOnly: Boolean(data.previewOnly ?? data.PreviewOnly ?? true),
  };
}

export function normalizeAllotmentContext(data: Record<string, unknown>): FibcOrderAllotmentContext {
  return {
    orderNo: strField(data, "orderNo", "OrderNo"),
    partyName: optStr(data, "partyName", "PartyName"),
    marketingNo: optStr(data, "marketingNo", "MarketingNo"),
    dispatchDate: optStr(data, "dispatchDate", "DispatchDate"),
    quantity: optNum(data, "quantity", "Quantity"),
    bagType: optStr(data, "bagType", "BagType"),
    bagTypeLabel: strField(data, "bagTypeLabel", "BagTypeLabel"),
  };
}

export function normalizeAllotmentResult(data: Record<string, unknown>): FibcAllotmentResult {
  const proposed = (data.proposedSlots ?? data.ProposedSlots ?? []) as Array<Record<string, unknown>>;
  const warnings = (data.warnings ?? data.Warnings ?? []) as unknown;
  return {
    success: Boolean(data.success ?? data.Success),
    message: strField(data, "message", "Message"),
    orderNo: strField(data, "orderNo", "OrderNo"),
    bagType: strField(data, "bagType", "BagType"),
    bagTypeLabel: strField(data, "bagTypeLabel", "BagTypeLabel"),
    quantity: numField(data, "quantity", "Quantity"),
    capacityPerShift: numField(data, "capacityPerShift", "CapacityPerShift"),
    slotsRequired: numField(data, "slotsRequired", "SlotsRequired"),
    bufferDays: numField(data, "bufferDays", "BufferDays"),
    dispatchDate: optStr(data, "dispatchDate", "DispatchDate"),
    targetCompletionDate: optStr(data, "targetCompletionDate", "TargetCompletionDate"),
    warnings: Array.isArray(warnings) ? warnings.map(String) : [],
    proposedSlots: proposed.map(normalizeSlotGridItem),
  };
}
