import { planningOrderUrl } from "@/lib/planning/planning-order-url";
import { normalizeIntegratedOrderTimeline, type IntegratedOrderTimeline } from "@/lib/planning/integrated-types";

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
