import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";

export const DEFAULT_EXPORT_GROUP = "Debtors-Overseas";
export const DEFAULT_PAGE_SIZE = 25;
export const PAGE_SIZES = [25, 50, 100] as const;

export interface ExportCompanyOption {
  value: string;
  label: string;
  kind: "all" | "group" | "company";
}

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
  dateFrom: string;
  asOf: string;
  groupName: string;
  count: number;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export type ExportOverdueView = "overdue" | "aging";

export interface ExportAgingBucket {
  key: string;
  label: string;
  pendingAmount: number;
  billCount: number;
}

export interface ExportAgingCustomer {
  companyName: string;
  customerName: string;
  amounts: number[];
  total: number;
  billCount: number;
}

export interface ExportAgingReport {
  company: string;
  dateFrom: string;
  asOf: string;
  groupName: string;
  totalPending: number;
  totalBills: number;
  buckets: ExportAgingBucket[];
  customers: ExportAgingCustomer[];
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

export async function getExportBillOverdueCompanies(): Promise<{
  names: string[];
  options: ExportCompanyOption[];
}> {
  const response = await fetch(getExportApiUrl("/api/ExportBillOverdue/companies"));
  const payload = (await response.json()) as {
    companies?: string[];
    Companies?: string[];
    options?: Array<Record<string, unknown>>;
    Options?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load companies");
  }
  const names = Array.from(
    new Set((payload.companies ?? payload.Companies ?? []).map((c) => String(c).trim()).filter(Boolean)),
  );

  const rawOptions = payload.options ?? payload.Options ?? [];
  const options: ExportCompanyOption[] =
    rawOptions.length > 0
      ? rawOptions.map((o) => ({
          value: str(o.value ?? o.Value),
          label: str(o.label ?? o.Label) || str(o.value ?? o.Value),
          kind: (str(o.kind ?? o.Kind).toLowerCase() as ExportCompanyOption["kind"]) || "company",
        }))
      : names.map((value) => ({
          value,
          label: value,
          kind: "company" as const,
        }));

  return { names, options };
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
  dateFrom: string;
  asOf: string;
  groupName: string;
  page: number;
  pageSize: number;
  refresh?: boolean;
  search?: string;
}): Promise<ExportBillOverdueResponse> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    asOf: filters.asOf,
    groupName: filters.groupName,
    page: String(filters.page),
    pageSize: String(filters.pageSize),
  });
  if (filters.refresh) params.set("refresh", "true");
  if (filters.search?.trim()) params.set("search", filters.search.trim());
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
    dateFrom: str(payload.dateFrom ?? payload.DateFrom) || filters.dateFrom,
    asOf: str(payload.asOf ?? payload.AsOf) || filters.asOf,
    groupName: str(payload.groupName ?? payload.GroupName) || filters.groupName,
    count: num(payload.count ?? payload.Count) || items.length,
    totalCount,
    page,
    pageSize,
    totalPages: totalCount === 0 ? 0 : totalPages,
  };
}

export async function getExportBillAging(filters: {
  company: string;
  dateFrom: string;
  asOf: string;
  groupName: string;
  refresh?: boolean;
}): Promise<ExportAgingReport> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    asOf: filters.asOf,
    groupName: filters.groupName,
  });
  if (filters.refresh) params.set("refresh", "true");
  const response = await fetch(getExportApiUrl(`/api/ExportBillOverdue/aging?${params}`));
  const payload = (await response.json()) as Record<string, unknown>;
  if (!response.ok) {
    throw new Error(str(payload.message) || "Failed to load export bill aging");
  }

  const bucketsRaw = (payload.buckets ?? payload.Buckets ?? []) as Array<Record<string, unknown>>;
  const customersRaw = (payload.customers ?? payload.Customers ?? []) as Array<Record<string, unknown>>;
  return {
    company: str(payload.company ?? payload.Company) || filters.company,
    dateFrom: str(payload.dateFrom ?? payload.DateFrom) || filters.dateFrom,
    asOf: str(payload.asOf ?? payload.AsOf) || filters.asOf,
    groupName: str(payload.groupName ?? payload.GroupName) || filters.groupName,
    totalPending: num(payload.totalPending ?? payload.TotalPending),
    totalBills: num(payload.totalBills ?? payload.TotalBills),
    buckets: bucketsRaw.map((b, i) => ({
      key: str(b.key ?? b.Key) || `b${i}`,
      label: str(b.label ?? b.Label),
      pendingAmount: num(b.pendingAmount ?? b.PendingAmount),
      billCount: num(b.billCount ?? b.BillCount),
    })),
    customers: customersRaw.map((c) => {
      const amountsRaw = (c.amounts ?? c.Amounts ?? []) as unknown[];
      return {
        companyName: str(c.companyName ?? c.CompanyName),
        customerName: str(c.customerName ?? c.CustomerName),
        amounts: amountsRaw.map((n) => num(n)),
        total: num(c.total ?? c.Total),
        billCount: num(c.billCount ?? c.BillCount),
      };
    }),
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

export function financialYearStartIso(iso?: string): string {
  const d = iso ? new Date(`${iso}T00:00:00`) : new Date();
  const year = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
  return `${year}-04-01`;
}

export function formatPeriod(dateFrom: string, asOf: string): string {
  return `${formatDisplayDate(dateFrom)} – ${formatDisplayDate(asOf)}`;
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

export function isCompanyGroup(company: string): boolean {
  return company.trim().toUpperCase().startsWith("G-");
}

async function downloadExportFile(
  path: string,
  fallbackName: string,
  failMessage: string,
): Promise<string> {
  const response = await fetch(getExportApiUrl(path));
  if (!response.ok) {
    let message = failMessage;
    try {
      const payload = (await response.json()) as { message?: string };
      if (payload.message) message = payload.message;
    } catch {
      // keep default
    }
    throw new Error(message);
  }

  const blob = await response.blob();
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^"]+)"?/i)?.[1] || fallbackName;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}

export async function downloadExportBillOverdueExcel(filters: {
  company: string;
  dateFrom: string;
  asOf: string;
  groupName: string;
}): Promise<string> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    asOf: filters.asOf,
    groupName: filters.groupName,
  });
  return downloadExportFile(
    `/api/ExportBillOverdue/excel?${params}`,
    `export-bill-overdue-${filters.asOf}.xlsx`,
    "Failed to export overdue bills",
  );
}

export async function downloadExportBillOverduePdf(filters: {
  company: string;
  dateFrom: string;
  asOf: string;
  groupName: string;
}): Promise<string> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    asOf: filters.asOf,
    groupName: filters.groupName,
  });
  return downloadExportFile(
    `/api/ExportBillOverdue/pdf?${params}`,
    `export-bill-overdue-${filters.asOf}.pdf`,
    "Failed to export overdue bills PDF",
  );
}

export async function downloadExportBillAgingPdf(filters: {
  company: string;
  dateFrom: string;
  asOf: string;
  groupName: string;
}): Promise<string> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    asOf: filters.asOf,
    groupName: filters.groupName,
  });
  return downloadExportFile(
    `/api/ExportBillOverdue/aging/pdf?${params}`,
    `export-bill-aging-${filters.asOf}.pdf`,
    "Failed to export aging report PDF",
  );
}
