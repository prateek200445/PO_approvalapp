import { getApiUrl } from "@/lib/api-config";
import {
  financialYearStart,
  toInputDate,
  type LedgerCompanyOption,
} from "@/lib/ledger-summary-types";

export type ExportCurrencyAuditItem = {
  documentType: string;
  documentNo: string;
  documentDate: string;
  companyName: string;
  partyName: string;
  ledgerName: string;
  inrAmount: number;
  storedFc: string;
  currency: string;
  exchangeRate: number;
  calculatedFc: number;
  issue: string;
};

export type ExportCurrencyAuditResult = {
  companyLabel: string;
  dateFrom: string;
  dateTo: string;
  minInrAmount: number;
  totalCount: number;
  creditNoteCount: number;
  debitNoteCount: number;
  totalInrAmount: number;
  items: ExportCurrencyAuditItem[];
};

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

function mapItem(raw: Record<string, unknown>): ExportCurrencyAuditItem {
  return {
    documentType: str(raw.documentType ?? raw.DocumentType),
    documentNo: str(raw.documentNo ?? raw.DocumentNo),
    documentDate: str(raw.documentDate ?? raw.DocumentDate),
    companyName: str(raw.companyName ?? raw.CompanyName),
    partyName: str(raw.partyName ?? raw.PartyName),
    ledgerName: str(raw.ledgerName ?? raw.LedgerName),
    inrAmount: num(raw.inrAmount ?? raw.InrAmount),
    storedFc: str(raw.storedFc ?? raw.StoredFc),
    currency: str(raw.currency ?? raw.Currency),
    exchangeRate: num(raw.exchangeRate ?? raw.ExchangeRate),
    calculatedFc: num(raw.calculatedFc ?? raw.CalculatedFc),
    issue: str(raw.issue ?? raw.Issue),
  };
}

export function defaultAuditDateRange() {
  return {
    dateFrom: financialYearStart(),
    dateTo: toInputDate(new Date()),
  };
}

export async function getExportCurrencyAuditCompanies(): Promise<LedgerCompanyOption[]> {
  const response = await fetch(getApiUrl("/api/export-currency-audit/companies"));
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Failed to load companies");
  return (Array.isArray(data) ? data : []).map((c: Record<string, unknown>) => ({
    value: str(c.value ?? c.Value),
    label: str(c.label ?? c.Label),
    companyType: num(c.companyType ?? c.CompanyType),
    companyName: str(c.companyName ?? c.CompanyName),
    companyId: num(c.companyId ?? c.CompanyId),
  }));
}

export async function runExportCurrencyAudit(filters: {
  company?: string;
  dateFrom: string;
  dateTo: string;
  minInr?: number;
}): Promise<ExportCurrencyAuditResult> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    minInr: String(filters.minInr ?? 100),
  });
  if (filters.company) params.set("company", filters.company);

  const response = await fetch(getApiUrl(`/api/export-currency-audit/run?${params}`));
  const data = await response.json();
  if (!response.ok) throw new Error(data.message || "Audit failed");

  const items = (data.items ?? data.Items ?? []).map((r: Record<string, unknown>) => mapItem(r));
  return {
    companyLabel: str(data.companyLabel ?? data.CompanyLabel),
    dateFrom: str(data.dateFrom ?? data.DateFrom),
    dateTo: str(data.dateTo ?? data.DateTo),
    minInrAmount: num(data.minInrAmount ?? data.MinInrAmount),
    totalCount: num(data.totalCount ?? data.TotalCount),
    creditNoteCount: num(data.creditNoteCount ?? data.CreditNoteCount),
    debitNoteCount: num(data.debitNoteCount ?? data.DebitNoteCount),
    totalInrAmount: num(data.totalInrAmount ?? data.TotalInrAmount),
    items,
  };
}

export async function downloadExportCurrencyAuditExcel(filters: {
  company?: string;
  dateFrom: string;
  dateTo: string;
  minInr?: number;
}): Promise<void> {
  const params = new URLSearchParams({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    minInr: String(filters.minInr ?? 100),
  });
  if (filters.company) params.set("company", filters.company);

  const response = await fetch(getApiUrl(`/api/export-currency-audit/excel?${params}`));
  if (!response.ok) {
    const err = await response.json().catch(() => ({}));
    throw new Error(err.message || "Excel export failed");
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `export-currency-audit-${filters.dateTo}.xlsx`;
  a.click();
  URL.revokeObjectURL(url);
}

export function formatInrAmount(value: number): string {
  return value.toLocaleString("en-IN", { maximumFractionDigits: 2 });
}

export function formatFcAmount(value: number, currency: string): string {
  const cur = currency.trim() || "$";
  return `${cur} ${value.toLocaleString("en-US", { maximumFractionDigits: 2 })}`;
}
