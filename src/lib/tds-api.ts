import { getApiUrl } from "@/lib/api-config";

export const TDS_SECTIONS = ["94A", "94C", "94H", "94I", "94J", "94Q"] as const;
export type TdsSection = (typeof TDS_SECTIONS)[number];

export interface TdsGroupOption {
  name: string;
  units: number;
}

export interface TdsLiabilityUnitRow {
  unit: string;
  sections: Record<string, number>;
  total: number;
}

export interface TdsReversalRow {
  unit: string;
  section: string;
  date: string;
  voucherType: string;
  voucherNo: string;
  particulars: string;
  amount: number;
}

export interface TdsOmitted {
  groupUnitLines: number;
  groupUnitAmount: number;
  transferLines: number;
  transferAmount: number;
}

export interface TdsLiability {
  group: string;
  from: string;
  to: string;
  units: TdsLiabilityUnitRow[];
  groupTotal: TdsLiabilityUnitRow;
  reversals: TdsReversalRow[];
  omitted: TdsOmitted;
}

export interface TdsQuarter {
  id: string;
  label: string;
  from: string;
  to: string;
}

function iso(year: number, month: number, day: number) {
  return `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
}

/** Financial year starts in April. Q1 is Apr–Jun. */
export function financialQuarters(now = new Date(), count = 8): TdsQuarter[] {
  const month = now.getMonth();
  let fyStart = month >= 3 ? now.getFullYear() : now.getFullYear() - 1;
  let quarter = 4;
  if (month >= 9) quarter = 3;
  else if (month >= 6) quarter = 2;
  else if (month >= 3) quarter = 1;
  const quarters: TdsQuarter[] = [];
  for (let i = 0; i < count; i++) {
    const span = quarterSpan(fyStart, quarter);
    const endYear = String(fyStart + 1).slice(2);
    quarters.push({
      id: `${fyStart}-Q${quarter}`,
      label: `Q${quarter} FY ${fyStart}-${endYear}`,
      from: span.from,
      to: span.to,
    });
    quarter -= 1;
    if (quarter === 0) {
      quarter = 4;
      fyStart -= 1;
    }
  }
  return quarters;
}

function quarterSpan(fyStart: number, quarter: number) {
  if (quarter === 1) return { from: iso(fyStart, 4, 1), to: iso(fyStart, 6, 30) };
  if (quarter === 2) return { from: iso(fyStart, 7, 1), to: iso(fyStart, 9, 30) };
  if (quarter === 3) return { from: iso(fyStart, 10, 1), to: iso(fyStart, 12, 31) };
  return { from: iso(fyStart + 1, 1, 1), to: iso(fyStart + 1, 3, 31) };
}

async function read<T>(path: string): Promise<T> {
  const response = await fetch(getApiUrl(path));
  const payload = (await response.json()) as T & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Request failed");
  return payload;
}

export function getTdsGroups() {
  return read<TdsGroupOption[]>("/api/tds/groups");
}

export function getTdsLiability(group: string, from: string, to: string) {
  const params = new URLSearchParams({ group, from, to });
  return read<TdsLiability>(`/api/tds/liability?${params}`);
}

export interface TdsSectionLine {
  unit: string;
  date: string;
  voucherType: string;
  voucherNo: string;
  party: string;
  pan: string;
  principal: number;
  rate: number;
  tds: number;
  originalTds: number;
}

export interface TdsSectionSheet {
  section: string;
  group: string;
  from: string;
  to: string;
  reversalDebit: number;
  liability: number;
  roundOffRemoved: number;
  duplicatesRemoved: number;
  lines: TdsSectionLine[];
  totalTds: number;
  totalPrincipal: number;
}

export function getTds94A(group: string, from: string, to: string) {
  const params = new URLSearchParams({ group, from, to });
  return read<TdsSectionSheet>(`/api/tds/94a?${params}`);
}
