import { getApiUrl } from "@/lib/api-config";

export interface OrderBookBagLine {
  bagGroup: string;
  balQty: number;
  balWtMt: number;
  declCapacityMt: number;
  declDays: number;
  actProdMt: number;
  actDays: number;
  pct: number;
}

export interface OrderBookUnitBlock {
  unitCode: string;
  companyName: string;
  balQty: number;
  balWtMt: number;
  confirmMt: number;
  openMt: number;
  plannedMt: number;
  orderCount: number;
  fgQty: number;
  fgWtMt: number;
  todayProdMt: number;
  toDateProdMt: number;
  avgProdMt: number;
  targetMt: number;
  expProdMt: number;
  statusMt: number;
  statusPct: number;
  valueInr: number;
  rsPerKg: number;
  lines: OrderBookBagLine[];
}

export interface OrderBookSummary {
  asOfDate: string;
  dayOfMonth: number;
  monthDays: number;
  monthLabel: string;
  issuedMt: number;
  inHandHoldMt: number;
  pendEntryMt: number;
  finalPendMt: number;
  confirmMt: number;
  openMt: number;
  plannedMt: number;
  confirmPct: number;
  openPct: number;
  plannedPct: number;
  totalOrderWtMt: number;
  totalValueInr: number;
  avgRsPerKg: number;
  ordBookDaysAvg: number;
  ordBookDaysCurr: number;
  totalOrders: number;
  source: string;
  note: string;
  units: OrderBookUnitBlock[];
}

export interface OrderBookAccess {
  enabled: boolean;
  allowed: boolean;
  username: string;
}

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export async function getOrderBookAccess(username: string): Promise<OrderBookAccess> {
  const response = await fetch(
    getApiUrl(`/api/OrderBookSummary/access?username=${encodeURIComponent(username)}`),
  );
  if (!response.ok) throw new Error("Failed to check Order Book access");
  return response.json();
}

export async function getOrderBookSummary(
  username: string,
  asOf?: string,
  refresh = false,
): Promise<OrderBookSummary> {
  const params = new URLSearchParams({
    username,
    asOf: asOf || todayIso(),
    refresh: refresh ? "true" : "false",
  });
  const response = await fetch(getApiUrl(`/api/OrderBookSummary?${params}`));
  if (response.status === 403) {
    throw new Error("You do not have access to Order Book Summary.");
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || "Failed to load Order Book Summary");
  }
  return response.json();
}

export function formatMt(n: number, digits = 2): string {
  return (n || 0).toLocaleString("en-IN", {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

export function formatInr(n: number): string {
  return (n || 0).toLocaleString("en-IN", { maximumFractionDigits: 0 });
}
