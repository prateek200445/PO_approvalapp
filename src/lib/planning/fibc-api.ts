import { getApiUrl } from "@/lib/api-config";
import {
  normalizeAllotmentContext,
  normalizeAllotmentResult,
  normalizeLineConfig,
  normalizePlanningConfig,
  normalizeSlotGridResult,
  type FibcAllotmentRequest,
  type FibcAllotmentResult,
  type FibcLineConfig,
  type FibcOrderAllotmentContext,
  type FibcOrderPlanDetail,
  type FibcPlanningConfig,
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
  const encoded = encodeURIComponent(orderNo.trim());
  const res = await fetch(getApiUrl(`/api/planning/fibc/orders/${encoded}`));
  const data = await parseJson<Record<string, unknown>>(res);

  const planLines = (data.planLines ?? data.PlanLines ?? []) as Array<Record<string, unknown>>;
  const fabricRequirements = (data.fabricRequirements ?? data.FabricRequirements ?? []) as Array<
    Record<string, unknown>
  >;

  return {
    orderNo: String(data.orderNo ?? data.OrderNo ?? orderNo),
    planLines: planLines.map((row) => ({
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
    })),
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
    })),
  };
}

export async function fetchFibcOrderAllotmentContext(orderNo: string): Promise<FibcOrderAllotmentContext> {
  const encoded = encodeURIComponent(orderNo.trim());
  const res = await fetch(getApiUrl(`/api/planning/fibc/orders/${encoded}/context`));
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

export async function previewFibcAllotment(request: FibcAllotmentRequest): Promise<FibcAllotmentResult> {
  const res = await fetch(getApiUrl("/api/planning/fibc/allot/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      orderNo: request.orderNo,
      companyName: request.companyName,
      dispatchDate: request.dispatchDate,
      quantity: request.quantity,
      bagType: request.bagType,
    }),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeAllotmentResult(data);
}
