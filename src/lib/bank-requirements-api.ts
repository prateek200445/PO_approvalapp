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
