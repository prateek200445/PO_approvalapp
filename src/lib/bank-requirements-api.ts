import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";
import { indianFyStartYear } from "@/lib/sales-dashboard-api";

export interface BankSalesProfile {
  company: string;
  dateFrom: string;
  dateTo: string;
  periodLabel: string;
  months: string[];
  exportAmount: number;
  domesticAmount: number;
  totalAmount: number;
  exportAmountCr: number;
  domesticAmountCr: number;
  totalAmountCr: number;
  exportShare: number;
  domesticShare: number;
  source: string;
  note: string;
}

function getBankApiUrl(path: string): string {
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

export function fyMonthKeys(fyStartYear: number, throughToday = true): string[] {
  const today = new Date();
  const months: string[] = [];
  for (let i = 0; i < 12; i++) {
    const d = new Date(fyStartYear, 3 + i, 1);
    if (throughToday && d > today) break;
    months.push(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`);
  }
  return months;
}

export function allFyMonthKeys(fyStartYear: number): string[] {
  const months: string[] = [];
  for (let i = 0; i < 12; i++) {
    const d = new Date(fyStartYear, 3 + i, 1);
    months.push(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`);
  }
  return months;
}

export function defaultBankMonths(): string[] {
  return fyMonthKeys(indianFyStartYear(), true);
}

function queryString(company: string, months: string[], refresh = false): string {
  const params = new URLSearchParams({ company });
  if (months.length) params.set("months", months.join(","));
  if (refresh) params.set("refresh", "true");
  return params.toString();
}

export async function getBankSalesProfile(
  company: string,
  months: string[],
  refresh = false,
): Promise<BankSalesProfile> {
  const response = await fetch(
    getBankApiUrl(`/api/BankRequirements/sales-profile?${queryString(company, months, refresh)}`),
  );
  const payload = (await response.json()) as BankSalesProfile & {
    message?: string;
    exportAmountMn?: number;
    domesticAmountMn?: number;
    totalAmountMn?: number;
  };
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load bank sales profile");
  }
  return {
    company: payload.company,
    dateFrom: payload.dateFrom,
    dateTo: payload.dateTo,
    periodLabel: payload.periodLabel,
    months: payload.months ?? months,
    exportAmount: Number(payload.exportAmount) || 0,
    domesticAmount: Number(payload.domesticAmount) || 0,
    totalAmount: Number(payload.totalAmount) || 0,
    exportAmountCr: Number(payload.exportAmountCr ?? payload.exportAmountMn) || 0,
    domesticAmountCr: Number(payload.domesticAmountCr ?? payload.domesticAmountMn) || 0,
    totalAmountCr: Number(payload.totalAmountCr ?? payload.totalAmountMn) || 0,
    exportShare: Number(payload.exportShare) || 0,
    domesticShare: Number(payload.domesticShare) || 0,
    source: payload.source ?? "vw_Sales_EBIDTA",
    note: payload.note ?? "",
  };
}

async function downloadFile(path: string, fallbackName: string, failMessage: string): Promise<string> {
  const response = await fetch(getBankApiUrl(path));
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

export async function downloadBankSalesExcel(company: string, months: string[]): Promise<string> {
  return downloadFile(
    `/api/BankRequirements/sales-profile/excel?${queryString(company, months)}`,
    "profile-of-sales.xlsx",
    "Failed to download Excel",
  );
}

export async function downloadBankSalesPdf(company: string, months: string[]): Promise<string> {
  return downloadFile(
    `/api/BankRequirements/sales-profile/pdf?${queryString(company, months)}`,
    "profile-of-sales.pdf",
    "Failed to download PDF",
  );
}

export interface BankPackParty {
  rank: number;
  name: string;
  country?: string | null;
  amountLakh: number;
  sharePct: number;
}

export interface BankPackRanking {
  label: string;
  from: string;
  to: string;
  overseas: BankPackParty[];
  domestic: BankPackParty[];
  overseasTotalLakh: number;
  domesticTotalLakh: number;
}

export interface BankPackMonth {
  month: string;
  label: string;
  overseasGroupLakh: number;
  overseasDirectLakh: number;
  domesticGroupLakh: number;
  domesticDirectLakh: number;
  partialMonth: boolean;
}

export interface BankPackAgeingParty {
  party: string;
  isGroup: boolean;
  overseas: boolean;
  bucketsLakh: number[];
  totalLakh: number;
}

export interface BankPackStockUnit {
  unit: string;
  valuesLakh: (number | null)[][];
}

export interface BankDocumentPack {
  code: string;
  companyName: string;
  generatedAt: string;
  today: string;
  customerRankings: BankPackRanking[];
  supplierRankings: BankPackRanking[];
  monthlySales: BankPackMonth[];
  monthlyPurchases: BankPackMonth[];
  debtors: { asOf: string; parties: BankPackAgeingParty[] }[];
  stock: { months: string[]; monthLabels: string[]; categories: string[]; units: BankPackStockUnit[] };
}

export interface BankPackDefaults {
  companies: { code: string; displayName: string }[];
  today: string;
  ageingDates: string[];
  stockFrom: string;
  stockTo: string;
}

export interface BankPackParams {
  company: string;
  ageingDates: string[];
  stockFrom: string;
  stockTo: string;
}

export const AGEING_BUCKETS = [
  "0-30",
  "31-60",
  "61-90",
  "91-180",
  "181-365",
  ">365",
  "On account",
];

function packQuery(p: BankPackParams, refresh = false): string {
  const params = new URLSearchParams({ company: p.company });
  if (p.ageingDates.length) params.set("ageingDates", p.ageingDates.join(","));
  if (p.stockFrom) params.set("stockFrom", p.stockFrom);
  if (p.stockTo) params.set("stockTo", p.stockTo);
  if (refresh) params.set("refresh", "true");
  return params.toString();
}

export async function getBankPackDefaults(): Promise<BankPackDefaults> {
  const response = await fetch(getBankApiUrl("/api/BankRequirements/document-pack/defaults"));
  if (!response.ok) throw new Error("Failed to load document pack defaults");
  return (await response.json()) as BankPackDefaults;
}

export async function getBankDocumentPack(p: BankPackParams, refresh = false): Promise<BankDocumentPack> {
  const response = await fetch(getBankApiUrl(`/api/BankRequirements/document-pack?${packQuery(p, refresh)}`));
  const payload = (await response.json()) as BankDocumentPack & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to build document pack");
  return payload;
}

export async function downloadBankDocumentPack(p: BankPackParams): Promise<string> {
  return downloadFile(
    `/api/BankRequirements/document-pack/excel?${packQuery(p)}`,
    `${p.company}_Bank_Document_Pack.xlsx`,
    "Failed to download document pack",
  );
}

export function formatMn(n: number): string {
  return new Intl.NumberFormat("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(n);
}

export function monthLabel(yyyyMm: string): string {
  const [y, m] = yyyyMm.split("-").map(Number);
  return new Date(y, (m || 1) - 1, 1).toLocaleString("en-IN", { month: "short" });
}
