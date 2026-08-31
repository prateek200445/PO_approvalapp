import { getApiUrl } from "@/lib/api-config";
import { planningOrderUrl } from "@/lib/planning/planning-order-url";
import {
  normalizeFullOrderPlan,
  normalizeIntegratedOrderTimeline,
  type FullOrderPlan,
  type IntegratedOrderTimeline,
} from "@/lib/planning/integrated-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchIntegratedOrderTimeline(orderNo: string): Promise<IntegratedOrderTimeline> {
  const res = await fetch(planningOrderUrl("/api/planning/orders/timeline", orderNo));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeIntegratedOrderTimeline(data);
}

export async function previewFullOrderPlan(orderNo: string): Promise<FullOrderPlan> {
  const res = await fetch(getApiUrl("/api/planning/orders/plan/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ orderNo }),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeFullOrderPlan(data);
}

export async function confirmFullOrderPlan(orderNo: string, replaceExistingFibc = false): Promise<FullOrderPlan> {
  const res = await fetch(getApiUrl("/api/planning/orders/plan/confirm"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ orderNo, replaceExistingFibc }),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeFullOrderPlan(data);
}
