export type SalesReportView = "Summary" | "Detail" | "Date Summary";
export type SalesReportCategory = "Sales" | "Purchase";
export type SalesTrendPeriod = "Last 6 Months" | "Last 12 Months";

export interface SalesDashboardFilters {
  company: string;
  companyValues: string[];
  dateFrom: string;
  dateTo: string;
  view: SalesReportView;
  category: SalesReportCategory;
  trendPeriod: SalesTrendPeriod;
}

export interface SalesCompanyOption {
  value: string;
  label: string;
  kind: "all" | "group" | "company";
}

export interface RankedPartyItem {
  rank: number;
  name: string;
  country?: string | null;
  amount: number;
}

export interface SalesDashboardSummary {
  totalSales: number;
  totalQuantity: number;
  averageRate: number;
  gstAmount: number;
  totalPurchase: number;
  grossProfit: number;
  totalSalesChangePercent: number;
  totalQuantityChangePercent: number;
  averageRateChangePercent: number;
  gstAmountChangePercent: number;
  totalPurchaseChangePercent: number;
  grossProfitChangePercent: number;
}

export interface SalesTrendItem {
  period: string;
  amount: number;
}

export interface SalesByGroupItem {
  groupName: string;
  amount: number;
  percentage: number;
}

export interface SalesByCompanyItem {
  companyName: string;
  amount: number;
}

export interface TopProduct {
  rank: number;
  productName: string;
  quantity: number;
  salesAmount: number;
}

/** Sales amounts aggregated by destination/billing country (ERP-backed when available). */
export interface SalesByCountryItem {
  rank: number;
  countryName: string;
  salesAmount: number;
}

/** Response for GET /api/SalesDashboard/by-country including FY period label. */
export interface SalesByCountryResponse {
  byCountry: SalesByCountryItem[];
  invYears: string[];
  /** e.g. "FY 25-26" or "FY 24-25, FY 25-26" */
  periodLabel: string;
}

/** @deprecated Prefer SalesByCountryItem — kept temporarily for older API shapes. */
export interface TopCustomer {
  rank: number;
  customerName: string;
  salesAmount: number;
}

export interface SalesBySubGroupItem {
  subGroupName: string;
  quantity: number;
  salesAmount: number;
}

export interface DetailedSalesAnalysisItem {
  id: string;
  groupName: string;
  subGroupName: string;
  productName: string;
  interGroup: string;
  salesPurchase: string;
  quantity: number;
  amount: number;
  perKgRate: number;
  gstAmount: number;
  netAmount: number;
}

export interface SalesDashboardData {
  companies: string[];
  filters: SalesDashboardFilters;
  summary: SalesDashboardSummary;
  trend: SalesTrendItem[];
  byGroup: SalesByGroupItem[];
  byCompany: SalesByCompanyItem[];
  topProducts: TopProduct[];
  byCountry: SalesByCountryItem[];
  /** @deprecated Prefer byCountry */
  topCustomers: TopCustomer[];
  bySubGroup: SalesBySubGroupItem[];
  detailedAnalysis: DetailedSalesAnalysisItem[];
  unavailableFields: string[];
}

export function viewToRptType(view: SalesReportView): number {
  if (view === "Detail") return 1;
  if (view === "Date Summary") return 2;
  return 0;
}
