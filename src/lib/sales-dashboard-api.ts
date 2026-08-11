import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";
import type {
  SalesByCompanyItem,
  SalesByGroupItem,
  SalesBySubGroupItem,
  SalesDashboardData,
  SalesDashboardFilters,
  SalesDashboardSummary,
  SalesTrendItem,
  TopCustomer,
  TopProduct,
  SalesByCountryItem,
  SalesByCountryResponse,
  DetailedSalesAnalysisItem,
} from "@/lib/sales-dashboard-types";
import { viewToRptType } from "@/lib/sales-dashboard-types";
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
    company: "Oswal Extrusion Limited",
    dateFrom: range.dateFrom,
    dateTo: new Date().toISOString().slice(0, 10),
    view: "Summary" as const,
    category: "Sales" as const,
    trendPeriod: "Last 6 Months" as const,
  };
})();

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

/** Fast company list from FactoryInfo — does not wait for Stock Analysis SP. */
export async function getSalesCompanies(): Promise<string[]> {
  const response = await fetch(getSalesApiUrl("/api/SalesDashboard/companies"));
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

  return ["All Companies", ...unique.filter((c) => c !== "All Companies")];
}

function mapTotalsBreakdown(payload: {
  byGroup?: Array<Record<string, unknown>>;
  ByGroup?: Array<Record<string, unknown>>;
  bySubGroup?: Array<Record<string, unknown>>;
  BySubGroup?: Array<Record<string, unknown>>;
}): {
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
} {
  const byGroupRaw = payload.byGroup ?? payload.ByGroup ?? [];
  const bySubGroupRaw = payload.bySubGroup ?? payload.BySubGroup ?? [];
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

/** Live ERP Amount + Netwt + PerKg + byGroup/bySubGroup from vw_Sales_EBIDTA (excl. IC). */
export async function getTotalSales(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
}): Promise<{
  totalSales: number;
  totalQuantity: number;
  averageRate: number;
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
}> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
  });

  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/total-sales?${params}`),
  );
  const payload = (await response.json()) as {
    totalSales?: number;
    TotalSales?: number;
    totalQuantity?: number;
    TotalQuantity?: number;
    averageRate?: number;
    AverageRate?: number;
    byGroup?: Array<Record<string, unknown>>;
    ByGroup?: Array<Record<string, unknown>>;
    bySubGroup?: Array<Record<string, unknown>>;
    BySubGroup?: Array<Record<string, unknown>>;
    message?: string;
  };

  if (!response.ok) {
    throw new Error(payload.message || "Failed to load total sales");
  }

  const breakdown = mapTotalsBreakdown(payload);
  return {
    totalSales: num(payload.totalSales ?? payload.TotalSales),
    totalQuantity: num(payload.totalQuantity ?? payload.TotalQuantity),
    averageRate: num(payload.averageRate ?? payload.AverageRate),
    ...breakdown,
  };
}

/** Live ERP Amount + Netwt + PerKg + byGroup/bySubGroup from vw_Purchase_EBIDTA (excl. IC). */
export async function getTotalPurchase(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
}): Promise<{
  totalPurchase: number;
  totalQuantity: number;
  averageRate: number;
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
}> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
  });

  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/total-purchase?${params}`),
  );
  const payload = (await response.json()) as {
    totalPurchase?: number;
    TotalPurchase?: number;
    totalQuantity?: number;
    TotalQuantity?: number;
    averageRate?: number;
    AverageRate?: number;
    byGroup?: Array<Record<string, unknown>>;
    ByGroup?: Array<Record<string, unknown>>;
    bySubGroup?: Array<Record<string, unknown>>;
    BySubGroup?: Array<Record<string, unknown>>;
    message?: string;
  };

  if (!response.ok) {
    throw new Error(payload.message || "Failed to load total purchase");
  }

  const breakdown = mapTotalsBreakdown(payload);
  return {
    totalPurchase: num(payload.totalPurchase ?? payload.TotalPurchase),
    totalQuantity: num(payload.totalQuantity ?? payload.TotalQuantity),
    averageRate: num(payload.averageRate ?? payload.AverageRate),
    ...breakdown,
  };
}

/** Year-by-year Total Sales or Purchase (Indian FY) excl. IC. */
export async function getSalesYearlyTrend(filters: {
  company: string;
  asOf: string;
  years?: number;
  category?: "Sales" | "Purchase";
}): Promise<SalesTrendItem[]> {
  const params = new URLSearchParams({
    company: filters.company,
    asOf: filters.asOf,
    years: String(filters.years ?? 5),
    category: filters.category ?? "Sales",
  });

  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/yearly-trend?${params}`),
  );
  const payload = (await response.json()) as {
    trend?: Array<Record<string, unknown>>;
    Trend?: Array<Record<string, unknown>>;
    message?: string;
  };

  if (!response.ok) {
    throw new Error(payload.message || "Failed to load yearly trend");
  }

  const raw = payload.trend ?? payload.Trend ?? [];
  return raw.map((t) => ({
    period: str(t.period ?? t.Period),
    amount: num(t.amount ?? t.Amount),
  }));
}

/** Top countries by Sales Value from vw_Countrywise_sales_dashboard (excl. IC). */
export async function getSalesByCountry(filters: {
  company: string;
  dateFrom: string;
  dateTo: string;
  top?: number;
}): Promise<SalesByCountryResponse> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    top: String(filters.top ?? 5),
  });

  const response = await fetch(
    getSalesApiUrl(`/api/SalesDashboard/by-country?${params}`),
  );
  const payload = (await response.json()) as {
    byCountry?: Array<Record<string, unknown>>;
    ByCountry?: Array<Record<string, unknown>>;
    invYears?: string[];
    InvYears?: string[];
    periodLabel?: string;
    PeriodLabel?: string;
    message?: string;
  };

  if (!response.ok) {
    throw new Error(payload.message || "Failed to load sales by country");
  }

  const raw = payload.byCountry ?? payload.ByCountry ?? [];
  const invYears = (payload.invYears ?? payload.InvYears ?? []).map(String);
  const periodLabel =
    str(payload.periodLabel ?? payload.PeriodLabel) ||
    (invYears.length > 0 ? invYears.map((y) => `FY ${y}`).join(", ") : "");

  return {
    byCountry: raw.map((c, i) => ({
      rank: num(c.rank ?? c.Rank) || i + 1,
      countryName: str(c.countryName ?? c.CountryName) || "Unknown",
      salesAmount: num(c.salesAmount ?? c.SalesAmount),
    })),
    invYears,
    periodLabel,
  };
}

type ApiSummary = {
  totalSales?: number;
  TotalSales?: number;
  totalQuantity?: number;
  TotalQuantity?: number;
  averageRate?: number;
  AverageRate?: number;
  gstAmount?: number;
  GstAmount?: number;
  totalPurchase?: number;
  TotalPurchase?: number;
  grossProfit?: number;
  GrossProfit?: number;
  totalSalesChangePercent?: number;
  TotalSalesChangePercent?: number;
  totalQuantityChangePercent?: number;
  TotalQuantityChangePercent?: number;
  averageRateChangePercent?: number;
  AverageRateChangePercent?: number;
  gstAmountChangePercent?: number;
  GstAmountChangePercent?: number;
  totalPurchaseChangePercent?: number;
  TotalPurchaseChangePercent?: number;
  grossProfitChangePercent?: number;
  GrossProfitChangePercent?: number;
};

type ApiResponse = {
  companies?: string[];
  Companies?: string[];
  company?: string;
  Company?: string;
  dateFrom?: string;
  DateFrom?: string;
  dateTo?: string;
  DateTo?: string;
  category?: string;
  Category?: string;
  summary?: ApiSummary;
  Summary?: ApiSummary;
  trend?: Array<Record<string, unknown>>;
  Trend?: Array<Record<string, unknown>>;
  byGroup?: Array<Record<string, unknown>>;
  ByGroup?: Array<Record<string, unknown>>;
  byCompany?: Array<Record<string, unknown>>;
  ByCompany?: Array<Record<string, unknown>>;
  topProducts?: Array<Record<string, unknown>>;
  TopProducts?: Array<Record<string, unknown>>;
  byCountry?: Array<Record<string, unknown>>;
  ByCountry?: Array<Record<string, unknown>>;
  topCustomers?: Array<Record<string, unknown>>;
  TopCustomers?: Array<Record<string, unknown>>;
  bySubGroup?: Array<Record<string, unknown>>;
  BySubGroup?: Array<Record<string, unknown>>;
  detailedAnalysis?: Array<Record<string, unknown>>;
  DetailedAnalysis?: Array<Record<string, unknown>>;
  unavailableFields?: string[];
  UnavailableFields?: string[];
  message?: string;
};

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

/**
 * Loads one dashboard section. First section (kpis) loads ERP ledger into API cache;
 * charts/tables reuse that cache for the same company+dates.
 */
export async function getSalesDashboardSection(
  filters: SalesDashboardFilters,
  section: "kpis" | "charts" | "tables" | "all",
  refresh = false,
): Promise<SalesDashboardData> {
  const params = new URLSearchParams({
    company: filters.company,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    rptType: String(viewToRptType(filters.view)),
    category: filters.category,
    section,
    refresh: refresh ? "true" : "false",
  });

  const response = await fetch(getSalesApiUrl(`/api/SalesDashboard/summary?${params}`));
  const payload = (await response.json()) as ApiResponse;

  if (!response.ok) {
    throw new Error(payload.message || `Failed to load sales dashboard (${section})`);
  }

  return mapApiResponse(payload, filters);
}

/**
 * Loads Sales Dashboard from vw_ItemLedgerTransaction via API (full payload).
 */
export async function getSalesDashboard(
  filters: SalesDashboardFilters,
): Promise<SalesDashboardData> {
  return getSalesDashboardSection(filters, "all", false);
}

function mapApiResponse(
  payload: ApiResponse,
  filters: SalesDashboardFilters,
): SalesDashboardData {
  const summarySrc = payload.summary ?? payload.Summary ?? {};
  const summary: SalesDashboardSummary = {
    totalSales: num(summarySrc.totalSales ?? summarySrc.TotalSales),
    totalQuantity: num(summarySrc.totalQuantity ?? summarySrc.TotalQuantity),
    averageRate: num(summarySrc.averageRate ?? summarySrc.AverageRate),
    gstAmount: num(summarySrc.gstAmount ?? summarySrc.GstAmount),
    totalPurchase: num(summarySrc.totalPurchase ?? summarySrc.TotalPurchase),
    grossProfit: num(summarySrc.grossProfit ?? summarySrc.GrossProfit),
    totalSalesChangePercent: num(
      summarySrc.totalSalesChangePercent ?? summarySrc.TotalSalesChangePercent,
    ),
    totalQuantityChangePercent: num(
      summarySrc.totalQuantityChangePercent ?? summarySrc.TotalQuantityChangePercent,
    ),
    averageRateChangePercent: num(
      summarySrc.averageRateChangePercent ?? summarySrc.AverageRateChangePercent,
    ),
    gstAmountChangePercent: num(
      summarySrc.gstAmountChangePercent ?? summarySrc.GstAmountChangePercent,
    ),
    totalPurchaseChangePercent: num(
      summarySrc.totalPurchaseChangePercent ?? summarySrc.TotalPurchaseChangePercent,
    ),
    grossProfitChangePercent: num(
      summarySrc.grossProfitChangePercent ?? summarySrc.GrossProfitChangePercent,
    ),
  };

  const trendRaw = payload.trend ?? payload.Trend ?? [];
  const trend: SalesTrendItem[] = trendRaw.map((t) => ({
    period: str(t.period ?? t.Period),
    amount: num(t.amount ?? t.Amount),
  }));

  const byGroupRaw = payload.byGroup ?? payload.ByGroup ?? [];
  const byGroup: SalesByGroupItem[] = byGroupRaw.map((g) => ({
    groupName: str(g.groupName ?? g.GroupName),
    amount: num(g.amount ?? g.Amount),
    percentage: num(g.percentage ?? g.Percentage),
  }));

  const byCompanyRaw = payload.byCompany ?? payload.ByCompany ?? [];
  const byCompany: SalesByCompanyItem[] = byCompanyRaw.map((c) => ({
    companyName: str(c.companyName ?? c.CompanyName),
    amount: num(c.amount ?? c.Amount),
  }));

  const topProductsRaw = payload.topProducts ?? payload.TopProducts ?? [];
  const topProducts: TopProduct[] = topProductsRaw.map((p) => ({
    rank: num(p.rank ?? p.Rank),
    productName: str(p.productName ?? p.ProductName),
    quantity: num(p.quantity ?? p.Quantity),
    salesAmount: num(p.salesAmount ?? p.SalesAmount),
  }));

  const topCustomersRaw = payload.topCustomers ?? payload.TopCustomers ?? [];
  const topCustomers: TopCustomer[] = topCustomersRaw.map((c) => ({
    rank: num(c.rank ?? c.Rank),
    customerName: str(c.customerName ?? c.CustomerName),
    salesAmount: num(c.salesAmount ?? c.SalesAmount),
  }));

  const byCountryRaw = payload.byCountry ?? payload.ByCountry ?? [];
  const byCountry: SalesByCountryItem[] =
    byCountryRaw.length > 0
      ? byCountryRaw.map((c, i) => ({
          rank: num(c.rank ?? c.Rank) || i + 1,
          countryName: str(c.countryName ?? c.CountryName),
          salesAmount: num(c.salesAmount ?? c.SalesAmount),
        }))
      : [];

  const bySubGroupRaw = payload.bySubGroup ?? payload.BySubGroup ?? [];
  const bySubGroup: SalesBySubGroupItem[] = bySubGroupRaw.map((s) => ({
    subGroupName: str(s.subGroupName ?? s.SubGroupName),
    quantity: num(s.quantity ?? s.Quantity),
    salesAmount: num(s.salesAmount ?? s.SalesAmount),
  }));

  const detailedRaw = payload.detailedAnalysis ?? payload.DetailedAnalysis ?? [];
  const detailedAnalysis: DetailedSalesAnalysisItem[] = detailedRaw.map((r, i) => ({
    id: str(r.id ?? r.Id ?? i + 1),
    groupName: str(r.groupName ?? r.GroupName),
    subGroupName: str(r.subGroupName ?? r.SubGroupName),
    productName: str(r.productName ?? r.ProductName),
    interGroup: str(r.interGroup ?? r.InterGroup),
    salesPurchase: str(r.salesPurchase ?? r.SalesPurchase),
    quantity: num(r.quantity ?? r.Quantity),
    amount: num(r.amount ?? r.Amount),
    perKgRate: num(r.perKgRate ?? r.PerKgRate),
    gstAmount: num(r.gstAmount ?? r.GstAmount),
    netAmount: num(r.netAmount ?? r.NetAmount),
  }));

  const companies = payload.companies ?? payload.Companies ?? [filters.company];

  return {
    companies,
    filters: {
      ...filters,
      company: str(payload.company ?? payload.Company) || filters.company,
      dateFrom: str(payload.dateFrom ?? payload.DateFrom) || filters.dateFrom,
      dateTo: str(payload.dateTo ?? payload.DateTo) || filters.dateTo,
      category:
        (str(payload.category ?? payload.Category) as SalesDashboardFilters["category"]) ||
        filters.category,
    },
    summary,
    trend,
    byGroup,
    byCompany,
    topProducts,
    byCountry,
    topCustomers,
    bySubGroup,
    detailedAnalysis,
    unavailableFields: payload.unavailableFields ?? payload.UnavailableFields ?? [],
  };
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

export function formatChangePercent(n: number): string {
  const sign = n > 0 ? "+" : "";
  return `${sign}${n.toFixed(1)}%`;
}
