import { getApiUrl } from "@/lib/api-config";

export type FibcBuyer = {
  id: string;
  companyName: string;
  country?: string | null;
  state?: string | null;
  city?: string | null;
  address?: string | null;
  email?: string | null;
  phone?: string | null;
  isGenuine?: boolean;
  genuineReasons?: string[];
  shipmentCount: number;
  firstShipment?: string | null;
  lastShipment?: string | null;
  totalQuantity: number;
  supplierCount: number;
  supplierCountries: string[];
  actualImportPort?: string | null;
  nearestPort?: string | null;
  score: number;
  scoreBreakdown?: {
    recency: number;
    frequency: number;
    volume: number;
    trend: number;
    supplierActivity: number;
    productRelevance: number;
    dataConfidence: number;
    total: number;
    explanation: string;
  };
  trend: string;
  priority: string;
  dataQuality: string;
  source: string;
};

export type FibcNamedValue = {
  name: string;
  valueUsd: number;
  valueLabel: string;
  shipmentCount?: number;
};

export type FibcImportHistoryItem = {
  id: string;
  at: string;
  mode: string;
  sourceFile?: string | null;
  recordsProcessed: number;
  newRecords: number;
  newShipments: number;
  duplicates: number;
  errors: number;
  totalBuyersAfter?: number;
  totalShipmentsAfter?: number;
  genuineBuyersAfter?: number;
  message?: string | null;
};

export type FibcImportHistoryDetail = {
  history: FibcImportHistoryItem;
  approximate: boolean;
  shipmentCount: number;
  buyerCount: number;
  topBuyers: FibcBuyer[];
  sampleShipments: {
    id: string;
    buyerId: string;
    shipmentDate?: string | null;
    industry?: string | null;
    productDescription?: string | null;
    hsCode?: string | null;
    quantity?: number | null;
    quantityUnit?: string | null;
    value?: number | null;
    unitPrice?: number | null;
    currency?: string | null;
    supplierName?: string | null;
    supplierCountry?: string | null;
    portOfLoading?: string | null;
    portOfDischarge?: string | null;
    destinationCountry?: string | null;
    originCountry?: string | null;
    shipmentReference?: string | null;
  }[];
};

export type FibcDashboard = {
  kpis: {
    totalBuyers: number;
    totalShipments: number;
    qualifiedBuyers: number;
    genuineBuyers?: number;
    hotBuyers: number;
    countries: number;
    suppliers: number;
  };
  topCountries: { name: string; count: number }[];
  scoreDistribution: { name: string; count: number }[];
  highPotential: FibcBuyer[];
  topBuyerCountriesByValue?: FibcNamedValue[];
  topSellerCountriesByValue?: FibcNamedValue[];
  topBuyersByValue?: FibcNamedValue[];
  topSellersByValue?: FibcNamedValue[];
  lastSync?: {
    at: string;
    mode: string;
    recordsProcessed: number;
    newRecords: number;
    newShipments: number;
    duplicates: number;
    errors: number;
    message?: string;
  } | null;
  keywords: string[];
  hsCodes: string[];
  sourceNote: string;
};

export type FibcFilters = {
  country?: string;
  buyer?: string;
  keyword?: string;
  minScore?: number;
  genuineOnly?: boolean;
};

function qs(params: Record<string, string | number | boolean | undefined | null>) {
  const sp = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v == null || v === "") return;
    sp.set(k, String(v));
  });
  const s = sp.toString();
  return s ? `?${s}` : "";
}

export async function getFibcDashboard(filters: FibcFilters): Promise<FibcDashboard> {
  const res = await fetch(getApiUrl(`/api/FibcBuyers/dashboard${qs(filters)}`));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to load FIBC dashboard");
  return data;
}

export async function getFibcBuyers(
  filters: FibcFilters & { page?: number; pageSize?: number },
): Promise<{ total: number; page: number; pageSize: number; items: FibcBuyer[] }> {
  const res = await fetch(getApiUrl(`/api/FibcBuyers/buyers${qs(filters)}`));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to load buyers");
  return data;
}

export async function getFibcBuyerDetail(buyerId: string) {
  const res = await fetch(getApiUrl(`/api/FibcBuyers/buyers/${encodeURIComponent(buyerId)}`));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Buyer not found");
  return data;
}

export async function getFibcCredentialStatus() {
  const res = await fetch(getApiUrl("/api/FibcBuyers/credentials-status"));
  return res.json();
}

export async function getFibcImportHistory(): Promise<{ items: FibcImportHistoryItem[] }> {
  const res = await fetch(getApiUrl("/api/FibcBuyers/history"));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to load import history");
  return data;
}

export async function getFibcImportHistoryDetail(id: string): Promise<FibcImportHistoryDetail> {
  const res = await fetch(getApiUrl(`/api/FibcBuyers/history/${encodeURIComponent(id)}`));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Import history entry not found");
  return data;
}

export async function syncFibcExim(body: {
  limit?: number;
  keywords?: string[];
  hsCodes?: string[];
  country?: string;
  dateRange?: string;
  tradeDirection?: string;
}) {
  const res = await fetch(getApiUrl("/api/FibcBuyers/sync"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  return res.json();
}

export async function importFibcCsv(file: File) {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(getApiUrl("/api/FibcBuyers/import"), {
    method: "POST",
    body: form,
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Import failed");
  return data;
}

export async function recomputeFibcBuyers() {
  const res = await fetch(getApiUrl("/api/FibcBuyers/recompute"), { method: "POST" });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Recompute failed");
  return data;
}

export function fibcExportUrl(filters: FibcFilters) {
  return getApiUrl(`/api/FibcBuyers/export.csv${qs(filters)}`);
}

export function na(v: unknown): string {
  if (v == null || v === "") return "Not Available";
  const s = String(v).trim();
  if (!s || s === "-" || s.toUpperCase() === "N/A" || s.toUpperCase() === "NA" || s === "\\N") {
    return "Not Available";
  }
  return s;
}

export function formatFibcDate(v?: string | null): string {
  if (!v) return "Not Available";
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return "Not Available";
  return d.toLocaleDateString("en-GB", { month: "short", year: "numeric" });
}
