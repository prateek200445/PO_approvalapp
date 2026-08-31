import { getApiUrl } from "@/lib/api-config";
import { planningOrderUrl } from "@/lib/planning/planning-order-url";
import {
  normalizeAllotmentContext,
  normalizeAllotmentConfirmResult,
  normalizeAllotmentResult,
  normalizeCriticalShiftConfirmResult,
  normalizeCriticalShiftResult,
  normalizeLineConfig,
  normalizePlanningConfig,
  normalizeQuotationConfirmResult,
  normalizeQuotationHold,
  normalizeQuotationHoldResult,
  normalizeSlotGridResult,
  type FibcAllotmentConfirmResult,
  type FibcAllotmentRequest,
  type FibcAllotmentResult,
  type FibcCriticalShiftConfirmResult,
  type FibcCriticalShiftRequest,
  type FibcCriticalShiftResult,
  type FibcLineConfig,
  type FibcOrderAllotmentContext,
  type FibcOrderPlanDetail,
  type FibcPlanningConfig,
  type FibcQuotationConfirmResult,
  type FibcQuotationHold,
  type FibcQuotationHoldRequest,
  type FibcQuotationHoldResult,
  type FibcSlotGridResult,
} from "@/lib/planning/fibc-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchFibcPlanningConfig(): Promise<FibcPlanningConfig> {
  const res = await fetch(getApiUrl("/api/planning/fibc/config"));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizePlanningConfig(data);
}

export async function fetchFibcLines(company?: string): Promise<FibcLineConfig[]> {
  const params = new URLSearchParams();
  if (company) params.set("company", company);
  const qs = params.toString();
  const res = await fetch(getApiUrl(`/api/planning/fibc/lines${qs ? `?${qs}` : ""}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLineConfig);
}

export async function fetchFibcSlotGrid(options: {
  from: string;
  to: string;
  company?: string;
}): Promise<FibcSlotGridResult> {
  const params = new URLSearchParams({ from: options.from, to: options.to });
  if (options.company) params.set("company", options.company);
  const res = await fetch(getApiUrl(`/api/planning/fibc/grid?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeSlotGridResult(data);
}

export async function fetchFibcOrderPlan(orderNo: string): Promise<FibcOrderPlanDetail> {
  const res = await fetch(planningOrderUrl("/api/planning/fibc/orders/plan", orderNo));
  const data = await parseJson<Record<string, unknown>>(res);

  const planLines = (data.planLines ?? data.PlanLines ?? []) as Array<Record<string, unknown>>;
  const savedAllocations = (data.savedAllocations ?? data.SavedAllocations ?? []) as Array<Record<string, unknown>>;
  const fabricRequirements = (data.fabricRequirements ?? data.FabricRequirements ?? []) as Array<
    Record<string, unknown>
  >;

  return {
    orderNo: String(data.orderNo ?? data.OrderNo ?? orderNo),
    planLines: planLines.map(mapOrderPlanLine),
    savedAllocations: savedAllocations.map(mapOrderPlanLine),
    fabricRequirements: fabricRequirements.map((row) => ({
      customer: String(row.customer ?? row.Customer ?? ""),
      filePoNo: String(row.filePoNo ?? row.FilePoNo ?? ""),
      bagType: String(row.bagType ?? row.BagType ?? ""),
      qty: (row.qty ?? row.Qty ?? null) as string | null,
      poDate: (row.poDate ?? row.PoDate ?? null) as string | null,
      targetDate: (row.targetDate ?? row.TargetDate ?? null) as string | null,
      heading: String(row.heading ?? row.Heading ?? ""),
      gsm: String(row.gsm ?? row.Gsm ?? ""),
      fabricSize: (row.fabricSize ?? row.FabricSize ?? null) as number | null,
      totalMtr: (row.totalMtr ?? row.TotalMtr ?? null) as number | null,
      totalKg: (row.totalKg ?? row.TotalKg ?? null) as number | null,
      category: String(row.category ?? row.Category ?? "Other"),
      planningKind: String(row.planningKind ?? row.PlanningKind ?? "Other"),
      isLoomEligible: Boolean(row.isLoomEligible ?? row.IsLoomEligible),
    })),
  };
}

export async function fetchFibcOrderAllotmentContext(orderNo: string): Promise<FibcOrderAllotmentContext> {
  const res = await fetch(planningOrderUrl("/api/planning/fibc/orders/context", orderNo));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeAllotmentContext(data);
}

export async function fetchFibcActiveShifts(options: {
  from: string;
  to: string;
  company?: string;
}): Promise<string[]> {
  const params = new URLSearchParams({ from: options.from, to: options.to });
  if (options.company) params.set("company", options.company);
  const res = await fetch(getApiUrl(`/api/planning/fibc/shifts?${params.toString()}`));
  const data = await parseJson<unknown>(res);
  return Array.isArray(data) ? data.map(String) : [];
}

function mapOrderPlanLine(row: Record<string, unknown>) {
  return {
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    lineNo: String(row.lineNo ?? row.LineNo ?? ""),
    partyName: (row.partyName ?? row.PartyName ?? null) as string | null,
    orderNo: (row.orderNo ?? row.OrderNo ?? null) as string | null,
    poQty: (row.poQty ?? row.PoQty ?? null) as number | null,
    bagType: String(row.bagType ?? row.BagType ?? ""),
    bagTypeLabel: String(row.bagTypeLabel ?? row.BagTypeLabel ?? ""),
    startDate: (row.startDate ?? row.StartDate ?? null) as string | null,
    completionDate: (row.completionDate ?? row.CompletionDate ?? null) as string | null,
    qty: Number(row.qty ?? row.Qty ?? 0),
    planDate: String(row.planDate ?? row.PlanDate ?? ""),
    shift: String(row.shift ?? row.Shift ?? ""),
    allocatedPercent: (row.allocatedPercent ?? row.AllocatedPercent ?? null) as number | null,
  };
}

function buildAllotmentBody(request: FibcAllotmentRequest) {
  return {
    orderNo: request.orderNo,
    companyName: request.companyName,
    dispatchDate: request.dispatchDate,
    quantity: request.quantity,
    bagType: request.bagType,
    partyName: request.partyName,
    marketingNo: request.marketingNo,
    replaceExisting: request.replaceExisting ?? false,
    allotmentMode: request.allotmentMode ?? "OrderWise",
    dustLevel: request.dustLevel ?? "Normal",
  };
}

export async function previewFibcAllotment(request: FibcAllotmentRequest): Promise<FibcAllotmentResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/allot/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildAllotmentBody(request)),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeAllotmentResult(data);
}

export async function confirmFibcAllotment(request: FibcAllotmentRequest): Promise<FibcAllotmentConfirmResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/allot/confirm"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildAllotmentBody(request)),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeAllotmentConfirmResult(data);
}

function buildCriticalShiftBody(request: FibcCriticalShiftRequest) {
  return {
    orderNo: request.orderNo,
    companyName: request.companyName,
    dispatchDate: request.dispatchDate,
    quantity: request.quantity,
    bagType: request.bagType,
    partyName: request.partyName,
    marketingNo: request.marketingNo,
    replaceExisting: request.replaceExisting ?? false,
    reason: request.reason,
    pinToTargetDate: request.pinToTargetDate ?? false,
  };
}

export async function previewFibcCriticalShift(request: FibcCriticalShiftRequest): Promise<FibcCriticalShiftResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/critical/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildCriticalShiftBody(request)),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeCriticalShiftResult(data);
}

export async function confirmFibcCriticalShift(
  request: FibcCriticalShiftRequest,
): Promise<FibcCriticalShiftConfirmResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/critical/confirm"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildCriticalShiftBody(request)),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeCriticalShiftConfirmResult(data);
}

function buildQuotationHoldBody(request: FibcQuotationHoldRequest) {
  return {
    orderNo: request.orderNo,
    companyName: request.companyName,
    dispatchDate: request.dispatchDate,
    quantity: request.quantity,
    bagType: request.bagType,
    partyName: request.partyName,
    marketingNo: request.marketingNo,
    notes: request.notes,
  };
}

export async function fetchFibcQuotationHolds(company?: string): Promise<FibcQuotationHold[]> {
  const params = new URLSearchParams();
  if (company) params.set("company", company);
  const qs = params.toString();
  const res = await fetch(getApiUrl(`/api/planning/fibc/quotation/holds${qs ? `?${qs}` : ""}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeQuotationHold);
}

export async function createFibcQuotationHold(request: FibcQuotationHoldRequest): Promise<FibcQuotationHoldResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/quotation/hold"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildQuotationHoldBody(request)),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeQuotationHoldResult(data);
}

export async function confirmFibcQuotationHold(
  holdId: number,
  replaceExisting = false,
): Promise<FibcQuotationConfirmResult> {
  const res = await fetch(getApiUrl(`/api/planning/fibc/quotation/${holdId}/confirm`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ replaceExisting }),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeQuotationConfirmResult(data);
}

export async function cancelFibcQuotationHold(holdId: number): Promise<FibcQuotationHoldResult> {
  const res = await fetch(getApiUrl(`/api/planning/fibc/quotation/${holdId}/cancel`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeQuotationHoldResult(data);
}
