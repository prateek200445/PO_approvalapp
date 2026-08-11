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
  DetailedSalesAnalysisItem,
} from "@/lib/sales-dashboard-types";
import { viewToRptType } from "@/lib/sales-dashboard-types";
import { formatINR, formatMoneyAmount } from "@/lib/mock-data";

export const DEFAULT_SALES_FILTERS: SalesDashboardFilters = {
  company: "Oswal Extrusion Limited",
  dateFrom: "2025-04-01",
  dateTo: "2025-07-31",
  view: "Summary",
  category: "Sales",
  trendPeriod: "Last 6 Months",
};

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

/** Live ERP Amount + Netwt + PerKg + byGroup/bySubGroup from SP_Sales_EBIDTA. */
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

  const byGroupRaw = payload.byGroup ?? payload.ByGroup ?? [];
  const bySubGroupRaw = payload.bySubGroup ?? payload.BySubGroup ?? [];

  return {
    totalSales: num(payload.totalSales ?? payload.TotalSales),
    totalQuantity: num(payload.totalQuantity ?? payload.TotalQuantity),
    averageRate: num(payload.averageRate ?? payload.AverageRate),
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

/** Year-by-year Total Sales (Indian FY) from SP_Sales_EBIDTA grand-total Amount. */
export async function getSalesYearlyTrend(filters: {
  company: string;
  asOf: string;
  years?: number;
}): Promise<SalesTrendItem[]> {
  const params = new URLSearchParams({
    company: filters.company,
    asOf: filters.asOf,
    years: String(filters.years ?? 5),
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
    throw new Error(payload.message || "Failed to load yearly sales trend");
  }

  const raw = payload.trend ?? payload.Trend ?? [];
  return raw.map((t) => ({
    period: str(t.period ?? t.Period),
    amount: num(t.amount ?? t.Amount),
  }));
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
