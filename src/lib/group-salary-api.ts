import { getApiUrl } from "@/lib/api-config";

export type GroupSalaryMatrixRow = {
  name: string;
  amounts: Record<string, number>;
  total: number;
};

export type GroupSalaryLine = {
  companyName: string;
  ledgerName: string;
  year: number;
  month: number;
  monthKey: string;
  amount: number;
};

export type GroupSalaryDashboard = {
  dateFrom: string;
  dateTo: string;
  companyFilter?: string | null;
  ledgers: string[];
  months: string[];
  companies: string[];
  totalAmount: number;
  companyRows: GroupSalaryMatrixRow[];
  ledgerRows: GroupSalaryMatrixRow[];
  monthTotals: { name: string; amount: number }[];
  lines: GroupSalaryLine[];
};

function pick<T extends Record<string, unknown>>(obj: T, ...keys: string[]) {
  for (const k of keys) {
    if (obj[k] != null) return obj[k];
  }
  return undefined;
}

function mapRow(raw: Record<string, unknown>): GroupSalaryMatrixRow {
  const amountsRaw = (pick(raw, "amounts", "Amounts") ?? {}) as Record<string, unknown>;
  const amounts: Record<string, number> = {};
  for (const [k, v] of Object.entries(amountsRaw)) amounts[k] = Number(v) || 0;
  return {
    name: String(pick(raw, "name", "Name") ?? ""),
    amounts,
    total: Number(pick(raw, "total", "Total") ?? 0),
  };
}

export async function getGroupSalaryCompanies(): Promise<string[]> {
  const res = await fetch(getApiUrl("/api/group-salary/companies"));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to load companies");
  return (Array.isArray(data) ? data : []).map(String);
}

export async function getGroupSalaryDashboard(params: {
  dateFrom: string;
  dateTo: string;
  company?: string;
}): Promise<GroupSalaryDashboard> {
  const sp = new URLSearchParams({
    dateFrom: params.dateFrom,
    dateTo: params.dateTo,
  });
  if (params.company) sp.set("company", params.company);
  const res = await fetch(getApiUrl(`/api/group-salary?${sp}`));
  const text = await res.text();
  let payload: Record<string, unknown> & { message?: string } = {};
  try {
    payload = text ? (JSON.parse(text) as typeof payload) : {};
  } catch {
    throw new Error(res.ok ? "Invalid group salary response" : "Group Salary API is not running.");
  }
  if (!res.ok) throw new Error(payload.message || "Failed to load group salary");

  const companyRowsRaw = (pick(payload, "companyRows", "CompanyRows") ?? []) as Record<string, unknown>[];
  const ledgerRowsRaw = (pick(payload, "ledgerRows", "LedgerRows") ?? []) as Record<string, unknown>[];
  const monthTotalsRaw = (pick(payload, "monthTotals", "MonthTotals") ?? []) as Record<string, unknown>[];
  const linesRaw = (pick(payload, "lines", "Lines") ?? []) as Record<string, unknown>[];

  return {
    dateFrom: String(pick(payload, "dateFrom", "DateFrom") ?? params.dateFrom),
    dateTo: String(pick(payload, "dateTo", "DateTo") ?? params.dateTo),
    companyFilter: (pick(payload, "companyFilter", "CompanyFilter") as string | null | undefined) ?? null,
    ledgers: ((pick(payload, "ledgers", "Ledgers") as unknown[]) ?? []).map(String),
    months: ((pick(payload, "months", "Months") as unknown[]) ?? []).map(String),
    companies: ((pick(payload, "companies", "Companies") as unknown[]) ?? []).map(String),
    totalAmount: Number(pick(payload, "totalAmount", "TotalAmount") ?? 0),
    companyRows: companyRowsRaw.map(mapRow),
    ledgerRows: ledgerRowsRaw.map(mapRow),
    monthTotals: monthTotalsRaw.map((m) => ({
      name: String(pick(m, "name", "Name") ?? ""),
      amount: Number(pick(m, "amount", "Amount") ?? 0),
    })),
    lines: linesRaw.map((l) => ({
      companyName: String(pick(l, "companyName", "CompanyName") ?? ""),
      ledgerName: String(pick(l, "ledgerName", "LedgerName") ?? ""),
      year: Number(pick(l, "year", "Year") ?? 0),
      month: Number(pick(l, "month", "Month") ?? 0),
      monthKey: String(pick(l, "monthKey", "MonthKey") ?? ""),
      amount: Number(pick(l, "amount", "Amount") ?? 0),
    })),
  };
}

export function groupSalaryExcelUrl(params: {
  dateFrom: string;
  dateTo: string;
  company?: string;
}) {
  const sp = new URLSearchParams({
    dateFrom: params.dateFrom,
    dateTo: params.dateTo,
  });
  if (params.company) sp.set("company", params.company);
  return getApiUrl(`/api/group-salary/excel?${sp}`);
}

export function formatInr(n: number): string {
  return n.toLocaleString("en-IN", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });
}

export function currentFyRange(): { dateFrom: string; dateTo: string } {
  const today = new Date();
  const y = today.getFullYear();
  const m = today.getMonth() + 1;
  const fyStartYear = m >= 4 ? y : y - 1;
  const pad = (n: number) => String(n).padStart(2, "0");
  return {
    dateFrom: `${fyStartYear}-04-01`,
    dateTo: `${today.getFullYear()}-${pad(m)}-${pad(today.getDate())}`,
  };
}

export function monthLabel(key: string): string {
  const [y, mo] = key.split("-");
  if (!y || !mo) return key;
  const d = new Date(Number(y), Number(mo) - 1, 1);
  return d.toLocaleDateString("en-GB", { month: "short", year: "numeric" });
}
