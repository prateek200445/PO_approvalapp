import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";

export const DEFAULT_EXPORT_GROUP = "Debtors-Overseas";
export const DEFAULT_PAGE_SIZE = 25;
export const PAGE_SIZES = [25, 50, 100] as const;

export interface ExportBillOverdueItem {
  companyName: string;
  ledgerName: string;
  customerName: string;
  billNo: string;
  billDate: string;
  billAmount: number;
  dueDate: string;
  overdueDays: number;
  pendingAmount: number;
  billCurrency: string;
  foreignAmount: number;
}

export interface ExportBillOverdueResponse {
  items: ExportBillOverdueItem[];
  company: string;
  asOf: string;
  groupName: string;
  count: number;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

function getExportApiUrl(path: string): string {
  const configured = getApiBaseUrl();
  if (configured) {
    const cleanPath = path.startsWith("/") ? path : `/${path}`;
    return `${configured}${cleanPath}`;
  }

  if (typeof window !== "undefined") {
    const host = window.location.hostname;
    if (host === "localhost" || host === "127.0.0.1") {
      const cleanPath = path.startsWith("/") ? path : `/${path}`;
      return `http://localhost:5115${cleanPath}`;
    }
  }

  return getApiUrl(path);
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

export async function getExportBillOverdueCompanies(): Promise<string[]> {
  const response = await fetch(getExportApiUrl("/api/ExportBillOverdue/companies"));
  const payload = (await response.json()) as {
    companies?: string[];
    Companies?: string[];
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load companies");
  }
  const names = payload.companies ?? payload.Companies ?? [];
  const unique = Array.from(
    new Set(names.map((c) => String(c).trim()).filter(Boolean)),
  );
  // Prefer single-company first — All Companies is slow (every plant).
  return [...unique.filter((c) => c !== "All Companies"), "All Companies"];
}

export async function getExportBillOverdueGroups(): Promise<string[]> {
  const response = await fetch(getExportApiUrl("/api/ExportBillOverdue/groups"));
  const payload = (await response.json()) as {
    groups?: string[];
    Groups?: string[];
    defaultGroup?: string;
    DefaultGroup?: string;
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load groups");
  }
  const names = payload.groups ?? payload.Groups ?? [];
  const unique = Array.from(
    new Set(names.map((c) => String(c).trim()).filter(Boolean)),
  );
  const preferred = str(payload.defaultGroup ?? payload.DefaultGroup) || DEFAULT_EXPORT_GROUP;
  if (preferred && !unique.some((g) => g.toLowerCase() === preferred.toLowerCase())) {
    unique.unshift(preferred);
  }
  return unique;
}

export async function getExportBillOverdue(filters: {
  company: string;
  asOf: string;
  groupName: string;
  page: number;
  pageSize: number;
  refresh?: boolean;
}): Promise<ExportBillOverdueResponse> {
  const params = new URLSearchParams({
    company: filters.company,
    asOf: filters.asOf,
    groupName: filters.groupName,
    page: String(filters.page),
    pageSize: String(filters.pageSize),
  });
  if (filters.refresh) params.set("refresh", "true");
  const response = await fetch(
    getExportApiUrl(`/api/ExportBillOverdue?${params}`),
  );
  const payload = (await response.json()) as Record<string, unknown>;
  if (!response.ok) {
    throw new Error(str(payload.message) || "Failed to load export bill overdue");
  }

  const raw = (payload.items ?? payload.Items ?? []) as Array<Record<string, unknown>>;
  const items: ExportBillOverdueItem[] = raw.map((r) => ({
    companyName: str(r.companyName ?? r.CompanyName),
    ledgerName: str(r.ledgerName ?? r.LedgerName),
    customerName: str(r.customerName ?? r.CustomerName),
    billNo: str(r.billNo ?? r.BillNo),
    billDate: str(r.billDate ?? r.BillDate),
    billAmount: num(r.billAmount ?? r.BillAmount),
    dueDate: str(r.dueDate ?? r.DueDate),
    overdueDays: num(r.overdueDays ?? r.OverdueDays),
    pendingAmount: num(r.pendingAmount ?? r.PendingAmount),
    billCurrency: str(r.billCurrency ?? r.BillCurrency) || "Rs.",
    foreignAmount: num(r.foreignAmount ?? r.ForeignAmount),
  }));

  const page = num(payload.page ?? payload.Page) || filters.page;
  const pageSize = num(payload.pageSize ?? payload.PageSize) || filters.pageSize;
  const totalCount = num(payload.totalCount ?? payload.TotalCount);
  const totalPages =
    num(payload.totalPages ?? payload.TotalPages) ||
    (pageSize > 0 ? Math.max(1, Math.ceil(totalCount / pageSize)) : 1);

  return {
    items,
    company: str(payload.company ?? payload.Company) || filters.company,
    asOf: str(payload.asOf ?? payload.AsOf) || filters.asOf,
    groupName: str(payload.groupName ?? payload.GroupName) || filters.groupName,
    count: num(payload.count ?? payload.Count) || items.length,
    totalCount,
    page,
    pageSize,
    totalPages: totalCount === 0 ? 0 : totalPages,
  };
}

export function formatBillAmount(n: number): string {
  return new Intl.NumberFormat("en-IN", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
  }).format(n);
}

export function isInrCurrency(currency: string): boolean {
  const c = (currency || "").trim().toLowerCase();
  return !c || c.startsWith("rs") || c === "inr" || c === "₹";
}

/** ERP-style foreign amount line, e.g. "$ 1,250.00" */
export function formatForeignAmount(currency: string, amount: number): string {
  if (isInrCurrency(currency) || !amount) return "";
  const code = currency.trim() || "";
  return `${code} ${formatBillAmount(amount)}`;
}

export function formatDisplayDate(iso: string): string {
  if (!iso || iso.startsWith("1900-01-01")) return "—";
  const d = new Date(`${iso}T00:00:00`);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("en-GB");
}

export function isAllCompanies(company: string): boolean {
  return (
    !company ||
    company.toLowerCase() === "all companies" ||
    company.toLowerCase().includes("(all)")
  );
}
