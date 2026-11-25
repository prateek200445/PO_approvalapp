import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";
import type {
  SalesByGroupItem,
  SalesBySubGroupItem,
  SalesDashboardFilters,
  SalesTrendItem,
  SalesByCountryItem,
  SalesCompanyOption,
  RankedPartyItem,
  FibcProductionMomData,
} from "@/lib/sales-dashboard-types";
import { formatINR, formatMoneyAmount } from "@/lib/mock-data";

/** Indian FY start year for a calendar date (Apr–Mar). */
export function indianFyStartYear(date = new Date()): number {
  return date.getMonth() >= 3 ? date.getFullYear() : date.getFullYear() - 1;
}

/** Format Indian FY as YYYY-MM-DD range (Apr 1 – Mar 31). */
export function indianFyDateRange(fyStartYear: number): { dateFrom: string; dateTo: string } {
  const pad = (n: number) => String(n).padStart(2, "0");
  return {
    dateFrom: `${fyStartYear}-04-01`,
    dateTo: `${fyStartYear + 1}-03-31`,
  };
}

export const DEFAULT_SALES_FILTERS: SalesDashboardFilters = (() => {
  const fy = indianFyStartYear();
  const range = indianFyDateRange(fy);
  return {
    company: "All Companies",
    companyValues: [],
    dateFrom: range.dateFrom,
    dateTo: new Date().toISOString().slice(0, 10),
    view: "Summary" as const,
    category: "Sales" as const,
    trendPeriod: "Last 6 Months" as const,
    includeIntercompany: false,
    section: "Sales" as const,
  };
})();

/** True when dates are not Current/Previous FY presets — load full lists, not top 10. */
export function isCustomSalesPeriod(dateFrom: string, dateTo: string): boolean {
  const current = indianFyDateRange(indianFyStartYear());
  const previous = indianFyDateRange(indianFyStartYear() - 1);
  const today = new Date().toISOString().slice(0, 10);
  if (dateFrom === current.dateFrom && (dateTo === current.dateTo || dateTo === today)) {
    return false;
  }
  if (dateFrom === previous.dateFrom && dateTo === previous.dateTo) {
    return false;
  }
  return true;
}

/** top=0 → all rows (custom period); otherwise top 10 for FY presets. */
export function salesRankingTop(dateFrom: string, dateTo: string): number {
  return isCustomSalesPeriod(dateFrom, dateTo) ? 0 : 10;
}

/**
 * Stock Analysis SP is slow; Vite's /api proxy can time out.
 * In local browser dev, call the API host directly (CORS is enabled).
 */
function getSalesApiUrl(path: string): string {
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

/** Fast company list from FactoryInfo, including group options (G-{group}). */
export async function getSalesCompanies(): Promise<{
  names: string[];
  options: SalesCompanyOption[];
}> {
  const response = await fetch(getSalesApiUrl("/api/SalesDashboard/companies"));
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
  const uniqueNames = ["All Companies", ...names.filter((c) => c !== "All Companies")];

  const rawOptions = payload.options ?? payload.Options ?? [];
  const options: SalesCompanyOption[] =
    rawOptions.length > 0
      ? rawOptions.map((o) => ({
          value: str(o.value ?? o.Value),
          label: str(o.label ?? o.Label) || str(o.value ?? o.Value),
          kind: (str(o.kind ?? o.Kind).toLowerCase() as SalesCompanyOption["kind"]) || "company",
        }))
      : uniqueNames.map((value) => ({
          value,
          label: value,
          kind: value === "All Companies" ? "all" : "company",
        }));

  return { names: uniqueNames, options };
}

function mapTotalsBreakdown(payload: Record<string, unknown>): {
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
} {
  const byGroupRaw = (payload.byGroup ?? payload.ByGroup ?? []) as Array<Record<string, unknown>>;
  const bySubGroupRaw = (payload.bySubGroup ?? payload.BySubGroup ?? []) as Array<Record<string, unknown>>;
  return {
    byGroup: byGroupRaw.map((g) => ({
      groupName: str(g.groupName ?? g.GroupName),
      amount: num(g.amount ?? g.Amount),
      percentage: num(g.percentage ?? g.Percentage),
    })),
    bySubGroup: bySubGroupRaw.map((s) => ({
      subGroupName: str(s.subGroupName ?? s.SubGroupName),
      quantity: num(s.quantity ?? s.Quantity),
      salesAmount: num(s.salesAmount ?? s.SalesAmount),
    })),
  };
}

/** KPI cards + group charts. Does not wait for the 5-year trend or country tables. */
function appendSalesParams(
  params: URLSearchParams,
  filters: { company: string; includeIntercompany?: boolean; refresh?: boolean },
) {
  params.set("company", filters.company);
  params.set("includeIntercompany", filters.includeIntercompany ? "true" : "false");
  params.set("refresh", filters.refresh ? "true" : "false");
}

export async function getSalesKpis(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  category: "Sales" | "Purchase";
  includeIntercompany?: boolean;
  refresh?: boolean;
}): Promise<{
  totalSales: number;
  totalPurchase: number;
  totalQuantity: number;
  averageRate: number;
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
}> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
  });
  appendSalesParams(params, filters);
  const path =
    filters.category === "Purchase"
      ? `/api/SalesDashboard/total-purchase?${params}`
      : `/api/SalesDashboard/total-sales?${params}`;
  const response = await fetch(getSalesApiUrl(path));
  const payload = (await response.json()) as Record<string, unknown> & { message?: string };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load dashboard totals");
  }
  const breakdown = mapTotalsBreakdown(payload);
  return {
    totalSales: num(payload.totalSales ?? payload.TotalSales),
    totalPurchase: num(payload.totalPurchase ?? payload.TotalPurchase),
    totalQuantity: num(payload.totalQuantity ?? payload.TotalQuantity),
    averageRate: num(payload.averageRate ?? payload.AverageRate),
    ...breakdown,
  };
}

export async function getSalesYearlyTrend(filters: {
  company: string;
  dateTo: string;
  category: "Sales" | "Purchase";
  includeIntercompany?: boolean;
  refresh?: boolean;
}): Promise<SalesTrendItem[]> {
  const params = new URLSearchParams({
    asOf: filters.dateTo,
    years: "5",
    category: filters.category,
  });
  appendSalesParams(params, filters);
  const response = await fetch(getSalesApiUrl(`/api/SalesDashboard/yearly-trend?${params}`));
  const payload = (await response.json()) as Record<string, unknown> & { message?: string };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load yearly trend");
  }
  const trendRaw = (payload.trend ?? payload.Trend ?? []) as Array<Record<string, unknown>>;
  return trendRaw.map((t) => ({
    period: str(t.period ?? t.Period),
    amount: num(t.amount ?? t.Amount),
  }));
}

/** FIBC bag production month-on-month (VW_FIBCBagwiseProduction). */
export async function getFibcProductionMonthly(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  refresh?: boolean;
}): Promise<FibcProductionMomData> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    refresh: filters.refresh ? "true" : "false",
  });
  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/fibc-production-monthly?${params}`),
  );
  const payload = (await response.json()) as Record<string, unknown> & { message?: string };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load FIBC production");
  }

  const monthsRaw = (payload.months ?? payload.Months ?? []) as Array<Record<string, unknown>>;
  const bagRaw = (payload.byBagType ?? payload.ByBagType ?? []) as Array<Record<string, unknown>>;

  return {
    months: monthsRaw.map((m) => ({
      period: str(m.period ?? m.Period),
      year: num(m.year ?? m.Year),
      month: num(m.month ?? m.Month),
      pcs: num(m.pcs ?? m.Pcs),
      wt: num(m.wt ?? m.Wt),
      pcsChangePercent: num(m.pcsChangePercent ?? m.PcsChangePercent),
      wtChangePercent: num(m.wtChangePercent ?? m.WtChangePercent),
    })),
    byBagType: bagRaw.map((b) => ({
      typeOfBag: str(b.typeOfBag ?? b.TypeOfBag),
      pcs: num(b.pcs ?? b.Pcs),
      wt: num(b.wt ?? b.Wt),
    })),
    totalPcs: num(payload.totalPcs ?? payload.TotalPcs),
    totalWt: num(payload.totalWt ?? payload.TotalWt),
  };
}

export async function getSalesTables(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  includeIntercompany?: boolean;
  refresh?: boolean;
  /** Country ranking size (default 10). Domestic is loaded separately. */
  top?: number;
}): Promise<{
  byCountry: SalesByCountryItem[];
  countryPeriodLabel: string;
}> {
  const top = String(filters.top ?? 10);
  const countryParams = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    top,
  });
  appendSalesParams(countryParams, filters);
  const countryRes = await fetch(getSalesApiUrl(`/api/SalesDashboard/by-country?${countryParams}`));
  const countryPayload = (await countryRes.json()) as Record<string, unknown> & { message?: string };
  if (!countryRes.ok) {
    throw new Error(countryPayload.message || "Failed to load country sales");
  }
  const countryRaw = (countryPayload.byCountry ?? countryPayload.ByCountry ?? []) as Array<
    Record<string, unknown>
  >;
  return {
    byCountry: countryRaw.map((c, i) => ({
      rank: num(c.rank ?? c.Rank) || i + 1,
      countryName: str(c.countryName ?? c.CountryName) || "Unknown",
      salesAmount: num(c.salesAmount ?? c.SalesAmount),
    })),
    countryPeriodLabel: str(countryPayload.periodLabel ?? countryPayload.PeriodLabel ?? countryPayload.countryPeriodLabel ?? countryPayload.CountryPeriodLabel),
  };
}

/** Export customers ranking — own date range; top defaults to 0 (full period). */
export async function getTopExportCustomers(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  top?: number;
  includeIntercompany?: boolean;
  refresh?: boolean;
  /** Default / Export analysis: BillAMount. Optional accessible = AccessableValue. */
  valueBasis?: "bill" | "accessible";
}): Promise<RankedPartyItem[]> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    top: String(filters.top ?? 0),
  });
  appendSalesParams(params, filters);
  if (filters.valueBasis) params.set("valueBasis", filters.valueBasis);
  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/top-export-customers?${params}`),
  );
  const payload = (await response.json()) as {
    items?: Array<Record<string, unknown>>;
    Items?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load export customers");
  }
  return mapRankedParties(payload);
}

/** Domestic customers ranking — own date range; top defaults to 0 (full period). */
export async function getTopDomesticCustomers(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  top?: number;
  includeIntercompany?: boolean;
  refresh?: boolean;
  /** Default / Export analysis: BillAMount. Optional accessible = AccessableValue. */
  valueBasis?: "bill" | "accessible";
}): Promise<RankedPartyItem[]> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    top: String(filters.top ?? 0),
  });
  appendSalesParams(params, filters);
  if (filters.valueBasis) params.set("valueBasis", filters.valueBasis);
  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/top-domestic-customers?${params}`),
  );
  const payload = (await response.json()) as {
    items?: Array<Record<string, unknown>>;
    Items?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load domestic customers");
  }
  return mapRankedParties(payload);
}

/** One cached payload for the live dashboard widgets. */
export async function getSalesOverview(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  category: "Sales" | "Purchase";
  includeIntercompany?: boolean;
  refresh?: boolean;
}): Promise<{
  totalSales: number;
  totalPurchase: number;
  totalQuantity: number;
  averageRate: number;
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
  trend: SalesTrendItem[];
  byCountry: SalesByCountryItem[];
  countryPeriodLabel: string;
  exportCustomers: RankedPartyItem[];
  domesticCustomers: RankedPartyItem[];
  suppliers: RankedPartyItem[];
}> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    category: filters.category,
  });
  appendSalesParams(params, filters);

  const response = await fetch(getSalesApiUrl(`/api/SalesDashboard/overview?${params}`));
  const payload = (await response.json()) as Record<string, unknown> & { message?: string };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load sales dashboard");
  }

  const breakdown = mapTotalsBreakdown(payload);
  const trendRaw = (payload.trend ?? payload.Trend ?? []) as Array<Record<string, unknown>>;
  const countryRaw = (payload.byCountry ?? payload.ByCountry ?? []) as Array<Record<string, unknown>>;
  return {
    totalSales: num(payload.totalSales ?? payload.TotalSales),
    totalPurchase: num(payload.totalPurchase ?? payload.TotalPurchase),
    totalQuantity: num(payload.totalQuantity ?? payload.TotalQuantity),
    averageRate: num(payload.averageRate ?? payload.AverageRate),
    ...breakdown,
    trend: trendRaw.map((t) => ({
      period: str(t.period ?? t.Period),
      amount: num(t.amount ?? t.Amount),
    })),
    byCountry: countryRaw.map((c, i) => ({
      rank: num(c.rank ?? c.Rank) || i + 1,
      countryName: str(c.countryName ?? c.CountryName) || "Unknown",
      salesAmount: num(c.salesAmount ?? c.SalesAmount),
    })),
    countryPeriodLabel: str(payload.countryPeriodLabel ?? payload.CountryPeriodLabel),
    exportCustomers: mapRankedParties({
      items: (payload.exportCustomers ?? payload.ExportCustomers) as Array<Record<string, unknown>> | undefined,
    }),
    domesticCustomers: mapRankedParties({
      items: (payload.domesticCustomers ?? payload.DomesticCustomers) as Array<Record<string, unknown>> | undefined,
    }),
    suppliers: mapRankedParties({
      items: (payload.suppliers ?? payload.Suppliers) as Array<Record<string, unknown>> | undefined,
    }),
  };
}

function mapRankedParties(payload: {
  items?: Array<Record<string, unknown>>;
  Items?: Array<Record<string, unknown>>;
}): RankedPartyItem[] {
  const raw = payload.items ?? payload.Items ?? [];
  return raw.map((row, i) => ({
    rank: num(row.rank ?? row.Rank) || i + 1,
    companyName: str(row.companyName ?? row.CompanyName) || null,
    name: str(row.name ?? row.Name ?? row.customerName ?? row.CustomerName) || "Unknown",
    country: str(row.country ?? row.Country) || null,
    amount: num(row.amount ?? row.Amount ?? row.salesAmount ?? row.SalesAmount),
  }));
}

/** Top suppliers from purchase EBIDTA. */
export async function getTopSuppliers(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  top?: number;
  includeIntercompany?: boolean;
  refresh?: boolean;
}): Promise<RankedPartyItem[]> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    top: String(filters.top ?? 10),
  });
  appendSalesParams(params, filters);
  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/top-suppliers?${params}`),
  );
  const payload = (await response.json()) as {
    items?: Array<Record<string, unknown>>;
    Items?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load suppliers");
  }
  return mapRankedParties(payload);
}

export type SalesTopTenPdfSection = {
  title: string;
  subtitle?: string;
  nameHeader: string;
  baseAmount?: number;
  /** Footer label for the section sum (e.g. "Period total"). */
  totalLabel?: string;
  rows: Array<{
    rank: number;
    companyName?: string | null;
    name: string;
    country?: string | null;
    amount: number;
  }>;
};

type SalesTopTenExportRequest = {
  category: "Sales" | "Purchase";
  company: string;
  dateFrom: string;
  dateTo: string;
  includeIntercompany: boolean;
  totalAmount?: number;
  /** Override banner label for totalAmount (default: Total sales/purchase for the period). */
  totalAmountLabel?: string;
  /** Optional header title (default: Sales/Purchase — Top 10 Report). */
  reportTitle?: string;
  sections: SalesTopTenPdfSection[];
  /** Optional download filename stem (without extension). */
  fileLabel?: string;
};

async function downloadSalesTopTenFile(
  path: "/api/SalesDashboard/top10/pdf" | "/api/SalesDashboard/top10/excel",
  request: SalesTopTenExportRequest,
  ext: "pdf" | "xlsx",
  failMessage: string,
): Promise<void> {
  const { fileLabel, ...body } = request;
  const response = await fetch(getSalesApiUrl(path), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
    const payload = (await response.json().catch(() => ({}))) as { message?: string };
    throw new Error(payload.message || failMessage);
  }
  const url = URL.createObjectURL(await response.blob());
  const a = document.createElement("a");
  a.href = url;
  a.download = `${fileLabel || `${request.category}_Top10`}_${request.dateFrom}_${request.dateTo}.${ext}`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export async function downloadSalesTopTenPdf(request: SalesTopTenExportRequest): Promise<void> {
  await downloadSalesTopTenFile(
    "/api/SalesDashboard/top10/pdf",
    request,
    "pdf",
    "Could not create the PDF",
  );
}

/** Excel export (ClosedXML) — same payload as PDF; used by Export / Domestic customer cards. */
export async function downloadSalesTopTenExcel(request: SalesTopTenExportRequest): Promise<void> {
  await downloadSalesTopTenFile(
    "/api/SalesDashboard/top10/excel",
    request,
    "xlsx",
    "Could not create the Excel file",
  );
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

export function formatSalesCurrency(n: number): string {
  return formatINR(n);
}

export function formatSalesQuantity(n: number): string {
  return `${formatMoneyAmount(n)} Kg`;
}

export function formatSalesRate(n: number): string {
  return `₹${new Intl.NumberFormat("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(n)} / Kg`;
}

export function formatCrores(n: number): string {
  const cr = n / 1e7;
  return `${cr.toFixed(2)} Cr`;
}
