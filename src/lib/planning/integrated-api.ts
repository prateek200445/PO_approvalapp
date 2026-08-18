import { getApiUrl } from "@/lib/api-config";
import { normalizeIntegratedOrderTimeline, type IntegratedOrderTimeline } from "@/lib/planning/integrated-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchIntegratedOrderTimeline(orderNo: string): Promise<IntegratedOrderTimeline> {
  const trimmed = orderNo.trim();
  const res = await fetch(getApiUrl(`/api/planning/orders/${encodeURIComponent(trimmed)}/timeline`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeIntegratedOrderTimeline(data);
}
