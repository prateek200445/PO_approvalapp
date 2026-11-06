import { getApiUrl } from "@/lib/api-config";

export interface ImportJvUnit {
  name: string;
  companyId: number;
}

export interface ImportJvLine {
  type: "DR" | "CR" | string;
  ledger: string;
  ledgerId: number;
  debit: number;
  credit: number;
  isTds: boolean;
  isPartyTds: boolean;
  billNo?: string | null;
  billAmount: number;
}

export interface ImportJvVoucher {
  row: number;
  creditLedger: string;
  invoiceNo: string;
  invoiceDate: string;
  entryDate: string;
  invoiceValue: number;
  tdsTotal: number;
  tdsAccessableAmount: number;
  requestedJournalNo?: number | null;
  drPartyName: string;
  crPartyName: string;
  lines: ImportJvLine[];
  errors: string[];
  warnings: string[];
}

export interface ImportJvPreview {
  group: string;
  company: string;
  companyId: number;
  fileName: string;
  debitColumns: string[];
  tdsColumns: string[];
  vouchers: ImportJvVoucher[];
  okCount: number;
  errorCount: number;
  tdsCount: number;
  canPost: boolean;
}

export interface ImportJvCommitResult {
  posted: number;
  journalNos: number[];
  company: string;
  year: string;
  message: string;
}

async function readJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  if (!text) throw new Error("Empty response from the API.");
  const payload = JSON.parse(text) as T & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Request failed");
  return payload;
}

export async function getImportJvUnits(group: string): Promise<ImportJvUnit[]> {
  const params = new URLSearchParams({ group });
  const response = await fetch(getApiUrl(`/api/import-jv/units?${params}`));
  return readJson<ImportJvUnit[]>(response);
}

export function importJvTemplateUrl() {
  return getApiUrl("/api/import-jv/template");
}

export async function previewImportJv(input: {
  file: File;
  group: string;
  company: string;
}): Promise<ImportJvPreview> {
  const body = new FormData();
  body.append("file", input.file);
  body.append("group", input.group);
  body.append("company", input.company);
  const response = await fetch(getApiUrl("/api/import-jv/preview"), { method: "POST", body });
  return readJson<ImportJvPreview>(response);
}

export async function commitImportJv(input: {
  file: File;
  group: string;
  company: string;
  username: string;
}): Promise<ImportJvCommitResult> {
  const body = new FormData();
  body.append("file", input.file);
  body.append("group", input.group);
  body.append("company", input.company);
  body.append("username", input.username);
  const response = await fetch(getApiUrl("/api/import-jv/commit"), { method: "POST", body });
  return readJson<ImportJvCommitResult>(response);
}

export function formatJvDate(value?: string | null) {
  if (!value) return "";
  const iso = value.slice(0, 10);
  const [y, m, d] = iso.split("-");
  if (!y || !m || !d) return value;
  return `${d}-${m}-${y}`;
}

export const jvMoney = new Intl.NumberFormat("en-IN", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});
