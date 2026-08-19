import { getApiUrl } from "@/lib/api-config";

/** Build a planning API URL with orderNo as a query param (supports `/` in PO numbers). */
export function planningOrderUrl(
  basePath: string,
  orderNo: string,
  extraParams?: Record<string, string | undefined>,
): string {
  const params = new URLSearchParams({ orderNo: orderNo.trim() });
  if (extraParams) {
    for (const [key, value] of Object.entries(extraParams)) {
      if (value) params.set(key, value);
    }
  }
  return getApiUrl(`${basePath}?${params.toString()}`);
}
