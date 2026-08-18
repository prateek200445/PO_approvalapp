import { getApiUrl } from "@/lib/api-config";
import {
  normalizeLoomAllotmentConfirmResult,
  normalizeLoomAllotmentContext,
  normalizeLoomAllotmentResult,
  normalizeLoomAllocationGridResult,
  normalizeLoomMaster,
  normalizeLoomOrderContext,
  normalizeLoomPlanningConfig,
  type LoomAllotmentConfirmResult,
  type LoomAllotmentRequest,
  type LoomAllotmentResult,
  type LoomAllocationGridResult,
  type LoomMaster,
  type LoomOrderAllotmentContext,
  type LoomOrderContext,
  type LoomOrderPlanDetail,
  type LoomPlanningConfig,
  type LoomPpmSpec,
  type LoomProductionMeterGridResult,
} from "@/lib/planning/loom-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchLoomPlanningConfig(): Promise<LoomPlanningConfig> {
  const res = await fetch(getApiUrl("/api/planning/loom/config"));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomPlanningConfig(data);
}

export async function fetchLoomMaster(company?: string): Promise<LoomMaster[]> {
  const params = new URLSearchParams();
  if (company) params.set("company", company);
  const qs = params.toString();
  const res = await fetch(getApiUrl(`/api/planning/loom/looms${qs ? `?${qs}` : ""}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLoomMaster);
}

export async function fetchLoomAllocationGrid(options: {
  from: string;
  to: string;
  company?: string;
}): Promise<LoomAllocationGridResult> {
  const params = new URLSearchParams({ from: options.from, to: options.to });
  if (options.company) params.set("company", options.company);
  const res = await fetch(getApiUrl(`/api/planning/loom/grid?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomAllocationGridResult(data);
}

export async function fetchLoomProductionMeters(options: {
  from: string;
  to: string;
  company?: string;
}): Promise<LoomProductionMeterGridResult> {
  const params = new URLSearchParams({ from: options.from, to: options.to });
  if (options.company) params.set("company", options.company);
  const res = await fetch(getApiUrl(`/api/planning/loom/production?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  const items = (data.items ?? data.Items ?? []) as Array<Record<string, unknown>>;
  return {
    items: items.map((row) => ({
      loomNo: Number(row.loomNo ?? row.LoomNo ?? 0),
      loomCode: (row.loomCode ?? row.LoomCode ?? null) as string | null,
      planDate: String(row.planDate ?? row.PlanDate ?? ""),
      prodMetersA: Number(row.prodMetersA ?? row.ProdMetersA ?? 0),
      prodMetersB: Number(row.prodMetersB ?? row.ProdMetersB ?? 0),
      reqGsm: (row.reqGsm ?? row.ReqGsm ?? null) as number | null,
      size: (row.size ?? row.Size ?? null) as number | null,
      orderNo: (row.orderNo ?? row.OrderNo ?? null) as string | null,
      partyName: (row.partyName ?? row.PartyName ?? null) as string | null,
    })),
    dateFrom: String(data.dateFrom ?? data.DateFrom ?? ""),
    dateTo: String(data.dateTo ?? data.DateTo ?? ""),
    companyName: String(data.companyName ?? data.CompanyName ?? ""),
  };
}

export async function fetchLoomPpmSpecs(): Promise<LoomPpmSpec[]> {
  const res = await fetch(getApiUrl("/api/planning/loom/ppm"));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map((row) => ({
    loomType: String(row.loomType ?? row.LoomType ?? ""),
    gsmFrom: Number(row.gsmFrom ?? row.GsmFrom ?? 0),
    gsmTo: Number(row.gsmTo ?? row.GsmTo ?? 0),
    widthFrom: Number(row.widthFrom ?? row.WidthFrom ?? 0),
    widthTo: Number(row.widthTo ?? row.WidthTo ?? 0),
    ppm: Number(row.ppm ?? row.Ppm ?? 0),
  }));
}

export async function fetchLoomOrderPlan(orderNo: string): Promise<LoomOrderPlanDetail> {
  const encoded = encodeURIComponent(orderNo.trim());
  const res = await fetch(getApiUrl(`/api/planning/loom/orders/${encoded}`));
  const data = await parseJson<Record<string, unknown>>(res);

  const allocations = (data.allocations ?? data.Allocations ?? []) as Array<Record<string, unknown>>;
  const fabricRequirements = (data.fabricRequirements ?? data.FabricRequirements ?? []) as Array<
    Record<string, unknown>
  >;

  return {
    orderNo: String(data.orderNo ?? data.OrderNo ?? orderNo),
    allocations: allocations.map((row) => ({
      loomNo: Number(row.loomNo ?? row.LoomNo ?? 0),
      loomCode: (row.loomCode ?? row.LoomCode ?? null) as string | null,
      partyName: (row.partyName ?? row.PartyName ?? null) as string | null,
      orderNo: (row.orderNo ?? row.OrderNo ?? null) as string | null,
      allocationDate: String(row.allocationDate ?? row.AllocationDate ?? ""),
      toDate: (row.toDate ?? row.ToDate ?? null) as string | null,
      reqGsm: (row.reqGsm ?? row.ReqGsm ?? null) as number | null,
      size: (row.size ?? row.Size ?? null) as number | null,
      allocationType: (row.allocationType ?? row.AllocationType ?? null) as string | null,
      color: (row.color ?? row.Color ?? null) as string | null,
      sector: (row.sector ?? row.Sector ?? null) as string | null,
      remarks: (row.remarks ?? row.Remarks ?? null) as string | null,
    })),
    fabricRequirements: fabricRequirements.map((row) => ({
      customer: String(row.customer ?? row.Customer ?? ""),
      filePoNo: String(row.filePoNo ?? row.FilePoNo ?? ""),
      bagType: String(row.bagType ?? row.BagType ?? ""),
      qty: (row.qty ?? row.Qty ?? null) as number | null,
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

export async function fetchLoomOrderContext(orderNo: string): Promise<LoomOrderContext> {
  const encoded = encodeURIComponent(orderNo.trim());
  const res = await fetch(getApiUrl(`/api/planning/loom/orders/${encoded}/context`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomOrderContext(data);
}

export async function fetchLoomOrderAllotmentContext(orderNo: string): Promise<LoomOrderAllotmentContext> {
  const encoded = encodeURIComponent(orderNo.trim());
  const res = await fetch(getApiUrl(`/api/planning/loom/orders/${encoded}/allotment-context`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomAllotmentContext(data);
}

export async function previewLoomAllotment(body: LoomAllotmentRequest): Promise<LoomAllotmentResult> {
  const res = await fetch(getApiUrl("/api/planning/loom/allot/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomAllotmentResult(data);
}

export async function confirmLoomAllotment(body: LoomAllotmentRequest): Promise<LoomAllotmentConfirmResult> {
  const res = await fetch(getApiUrl("/api/planning/loom/allot/confirm"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeLoomAllotmentConfirmResult(data);
}
