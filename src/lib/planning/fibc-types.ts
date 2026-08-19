export type FibcPlanningConfig = {
  defaultCompanyName: string;
  dispatchBufferDays: number;
  shiftPreference: string[];
  activeShifts: string[];
  allotmentEnabled: boolean;
  previewOnly: boolean;
  confirmSaveEnabled: boolean;
  replaceExistingEnabled: boolean;
  quotationHoldEnabled: boolean;
  quotationHoldDays: number;
  quotationHoldEmailEnabled: boolean;
  criticalShiftEnabled: boolean;
  criticalShiftEmailEnabled: boolean;
};

export type FibcOrderAllotmentContext = {
  orderNo: string;
  partyName: string | null;
  marketingNo: string | null;
  dispatchDate: string | null;
  quantity: number | null;
  bagType: string | null;
  bagTypeLabel: string;
  existingAllocationCount: number;
};

export type FibcAllotmentRequest = {
  orderNo: string;
  companyName?: string;
  dispatchDate?: string;
  quantity?: number;
  bagType?: string;
  partyName?: string;
  marketingNo?: string;
  replaceExisting?: boolean;
  allotmentMode?: "OrderWise" | "SlotWise";
  dustLevel?: "Normal" | "Single" | "Double" | "Triple";
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
  allotmentMode?: string;
  dustLevel?: string;
  rejectionPercentApplied?: number;
  warnings: string[];
  proposedSlots: FibcSlotGridItem[];
};

export type FibcAllotmentConfirmResult = FibcAllotmentResult & {
  saved: boolean;
  rowsInserted: number;
};

export type FibcCriticalShiftRequest = {
  orderNo: string;
  companyName?: string;
  dispatchDate?: string;
  quantity?: number;
  bagType?: string;
  partyName?: string;
  marketingNo?: string;
  replaceExisting?: boolean;
  reason?: string;
  /** When true, slots must fall on target completion date only (testing / strict mode). */
  pinToTargetDate?: boolean;
};

export type FibcOrderShiftDisplacement = {
  orderNo: string;
  partyName: string | null;
  bagType: string;
  bagTypeLabel: string;
  fromLineNo: string;
  fromPlanDate: string;
  fromShift: string;
  toLineNo: string;
  toPlanDate: string;
  toShift: string;
  qty: number;
  capacity: number;
  allocatedPercent: number | null;
  marketingNo: string | null;
};

export type FibcCriticalShiftResult = {
  success: boolean;
  shiftsRequired: boolean;
  fullyAllotted: boolean;
  message: string;
  orderNo: string;
  bagType: string;
  bagTypeLabel: string;
  quantity: number;
  capacityPerShift: number;
  bufferDays: number;
  dispatchDate: string | null;
  targetCompletionDate: string | null;
  pinToTargetDate: boolean;
  warnings: string[];
  proposedSlots: FibcSlotGridItem[];
  displacements: FibcOrderShiftDisplacement[];
};

export type FibcCriticalShiftConfirmResult = FibcCriticalShiftResult & {
  saved: boolean;
  rowsInserted: number;
  rowsDeleted: number;
  ordersShifted: number;
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
  savedAllocations: FibcOrderPlanLine[];
  fabricRequirements: FibcFabricRequirement[];
};

export type FibcQuotationHoldRequest = {
  orderNo: string;
  companyName?: string;
  dispatchDate?: string;
  quantity?: number;
  bagType?: string;
  partyName?: string;
  marketingNo?: string;
  notes?: string;
};

export type FibcQuotationHoldSlot = {
  planDate: string;
  lineNo: string;
  shift: string;
  qty: number;
  capacity: number;
  allocatedPercent: number | null;
};

export type FibcQuotationHold = {
  holdId: number;
  referenceCode: string;
  companyName: string;
  orderNo: string;
  partyName: string | null;
  marketingNo: string | null;
  bagType: string | null;
  bagTypeLabel: string;
  quantity: number;
  dispatchDate: string | null;
  status: string;
  notes: string | null;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
  slots: FibcQuotationHoldSlot[];
};

export type FibcQuotationHoldResult = {
  success: boolean;
  message: string;
  hold: FibcQuotationHold | null;
};

export type FibcQuotationConfirmResult = {
  success: boolean;
  saved: boolean;
  message: string;
  holdId: number;
  rowsInserted: number;
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
    confirmSaveEnabled: Boolean(data.confirmSaveEnabled ?? data.ConfirmSaveEnabled),
    replaceExistingEnabled: Boolean(data.replaceExistingEnabled ?? data.ReplaceExistingEnabled),
    quotationHoldEnabled: Boolean(data.quotationHoldEnabled ?? data.QuotationHoldEnabled),
    quotationHoldDays: numField(data, "quotationHoldDays", "QuotationHoldDays") || 7,
    quotationHoldEmailEnabled: Boolean(data.quotationHoldEmailEnabled ?? data.QuotationHoldEmailEnabled),
    criticalShiftEnabled: Boolean(data.criticalShiftEnabled ?? data.CriticalShiftEnabled),
    criticalShiftEmailEnabled: Boolean(data.criticalShiftEmailEnabled ?? data.CriticalShiftEmailEnabled),
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
    existingAllocationCount: numField(data, "existingAllocationCount", "ExistingAllocationCount"),
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
    allotmentMode: optStr(data, "allotmentMode", "AllotmentMode") ?? "OrderWise",
    dustLevel: optStr(data, "dustLevel", "DustLevel") ?? "Normal",
    rejectionPercentApplied: optNum(data, "rejectionPercentApplied", "RejectionPercentApplied") ?? undefined,
    warnings: Array.isArray(warnings) ? warnings.map(String) : [],
    proposedSlots: proposed.map(normalizeSlotGridItem),
  };
}

export function normalizeAllotmentConfirmResult(data: Record<string, unknown>): FibcAllotmentConfirmResult {
  const base = normalizeAllotmentResult(data);
  return {
    ...base,
    saved: Boolean(data.saved ?? data.Saved),
    rowsInserted: numField(data, "rowsInserted", "RowsInserted"),
  };
}

function normalizeQuotationHoldSlot(row: Record<string, unknown>): FibcQuotationHoldSlot {
  return {
    planDate: strField(row, "planDate", "PlanDate"),
    lineNo: strField(row, "lineNo", "LineNo"),
    shift: strField(row, "shift", "Shift"),
    qty: numField(row, "qty", "Qty"),
    capacity: numField(row, "capacity", "Capacity"),
    allocatedPercent: optNum(row, "allocatedPercent", "AllocatedPercent"),
  };
}

export function normalizeQuotationHold(data: Record<string, unknown>): FibcQuotationHold {
  const slots = (data.slots ?? data.Slots ?? []) as Array<Record<string, unknown>>;
  return {
    holdId: numField(data, "holdId", "HoldId"),
    referenceCode: strField(data, "referenceCode", "ReferenceCode"),
    companyName: strField(data, "companyName", "CompanyName"),
    orderNo: strField(data, "orderNo", "OrderNo"),
    partyName: optStr(data, "partyName", "PartyName"),
    marketingNo: optStr(data, "marketingNo", "MarketingNo"),
    bagType: optStr(data, "bagType", "BagType"),
    bagTypeLabel: strField(data, "bagTypeLabel", "BagTypeLabel"),
    quantity: numField(data, "quantity", "Quantity"),
    dispatchDate: optStr(data, "dispatchDate", "DispatchDate"),
    status: strField(data, "status", "Status"),
    notes: optStr(data, "notes", "Notes"),
    createdAt: strField(data, "createdAt", "CreatedAt"),
    expiresAt: strField(data, "expiresAt", "ExpiresAt"),
    confirmedAt: optStr(data, "confirmedAt", "ConfirmedAt"),
    cancelledAt: optStr(data, "cancelledAt", "CancelledAt"),
    slots: slots.map(normalizeQuotationHoldSlot),
  };
}

export function normalizeQuotationHoldResult(data: Record<string, unknown>): FibcQuotationHoldResult {
  const holdRaw = data.hold ?? data.Hold;
  return {
    success: Boolean(data.success ?? data.Success),
    message: strField(data, "message", "Message"),
    hold: holdRaw && typeof holdRaw === "object" ? normalizeQuotationHold(holdRaw as Record<string, unknown>) : null,
  };
}

export function normalizeQuotationConfirmResult(data: Record<string, unknown>): FibcQuotationConfirmResult {
  return {
    success: Boolean(data.success ?? data.Success),
    saved: Boolean(data.saved ?? data.Saved),
    message: strField(data, "message", "Message"),
    holdId: numField(data, "holdId", "HoldId"),
    rowsInserted: numField(data, "rowsInserted", "RowsInserted"),
  };
}

function normalizeOrderShiftDisplacement(row: Record<string, unknown>): FibcOrderShiftDisplacement {
  return {
    orderNo: strField(row, "orderNo", "OrderNo"),
    partyName: optStr(row, "partyName", "PartyName"),
    bagType: strField(row, "bagType", "BagType"),
    bagTypeLabel: strField(row, "bagTypeLabel", "BagTypeLabel"),
    fromLineNo: strField(row, "fromLineNo", "FromLineNo"),
    fromPlanDate: strField(row, "fromPlanDate", "FromPlanDate"),
    fromShift: strField(row, "fromShift", "FromShift"),
    toLineNo: strField(row, "toLineNo", "ToLineNo"),
    toPlanDate: strField(row, "toPlanDate", "ToPlanDate"),
    toShift: strField(row, "toShift", "ToShift"),
    qty: numField(row, "qty", "Qty"),
    capacity: numField(row, "capacity", "Capacity"),
    allocatedPercent: optNum(row, "allocatedPercent", "AllocatedPercent"),
    marketingNo: optStr(row, "marketingNo", "MarketingNo"),
  };
}

export function normalizeCriticalShiftResult(data: Record<string, unknown>): FibcCriticalShiftResult {
  const proposed = (data.proposedSlots ?? data.ProposedSlots ?? []) as Array<Record<string, unknown>>;
  const displacements = (data.displacements ?? data.Displacements ?? []) as Array<Record<string, unknown>>;
  const warnings = (data.warnings ?? data.Warnings ?? []) as unknown;
  return {
    success: Boolean(data.success ?? data.Success),
    shiftsRequired: Boolean(data.shiftsRequired ?? data.ShiftsRequired),
    fullyAllotted: Boolean(data.fullyAllotted ?? data.FullyAllotted),
    message: strField(data, "message", "Message"),
    orderNo: strField(data, "orderNo", "OrderNo"),
    bagType: strField(data, "bagType", "BagType"),
    bagTypeLabel: strField(data, "bagTypeLabel", "BagTypeLabel"),
    quantity: numField(data, "quantity", "Quantity"),
    capacityPerShift: numField(data, "capacityPerShift", "CapacityPerShift"),
    bufferDays: numField(data, "bufferDays", "BufferDays"),
    dispatchDate: optStr(data, "dispatchDate", "DispatchDate"),
    targetCompletionDate: optStr(data, "targetCompletionDate", "TargetCompletionDate"),
    pinToTargetDate: Boolean(data.pinToTargetDate ?? data.PinToTargetDate),
    warnings: Array.isArray(warnings) ? warnings.map(String) : [],
    proposedSlots: proposed.map(normalizeSlotGridItem),
    displacements: displacements.map(normalizeOrderShiftDisplacement),
  };
}

export function normalizeCriticalShiftConfirmResult(data: Record<string, unknown>): FibcCriticalShiftConfirmResult {
  const base = normalizeCriticalShiftResult(data);
  return {
    ...base,
    saved: Boolean(data.saved ?? data.Saved),
    rowsInserted: numField(data, "rowsInserted", "RowsInserted"),
    rowsDeleted: numField(data, "rowsDeleted", "RowsDeleted"),
    ordersShifted: numField(data, "ordersShifted", "OrdersShifted"),
  };
}
