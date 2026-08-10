export type SalesReportView = "Summary" | "Detail" | "Date Summary";
export type SalesReportCategory = "Sales" | "Purchase";
export type SalesTrendPeriod = "Last 6 Months" | "Last 12 Months";

export interface SalesDashboardFilters {
  company: string;
  dateFrom: string;
  dateTo: string;
  view: SalesReportView;
  category: SalesReportCategory;
  trendPeriod: SalesTrendPeriod;
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
