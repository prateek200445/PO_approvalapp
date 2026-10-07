import { getApiUrl } from "@/lib/api-config";

export interface GstDocumentSeries {
  company: string;
  nature: string;
  series: string;
  fromNo: string;
  toNo: string;
  totalNumber: number;
  cancelled: number;
  netIssued: number;
  missingNumbers: string[];
  missingTruncated: boolean;
  voucherTypes: string[];
  firstDate: string;
  lastDate: string;
}

export interface GstDocumentSummary {
  from: string;
  to: string;
  generatedAtUtc: string;
  companies: string[];
  rows: GstDocumentSeries[];
}

type Raw = Record<string, unknown>;

function pick(r: Raw, camel: string): unknown {
  return r[camel] ?? r[camel.charAt(0).toUpperCase() + camel.slice(1)];
}

function strings(v: unknown): string[] {
  return Array.isArray(v) ? v.map((x) => String(x)) : [];
}

export async function getGstDocumentSummary(
  from: string,
  to: string,
  refresh = false,
): Promise<GstDocumentSummary> {
  const params = new URLSearchParams({ from, to });
  if (refresh) params.set("refresh", "true");
  const response = await fetch(getApiUrl(`/api/Gst/document-summary?${params}`));
  const text = await response.text();
  let payload: Raw & { message?: string } = {};
  try {
    payload = text ? (JSON.parse(text) as Raw & { message?: string }) : {};
  } catch {
    throw new Error(response.ok ? "Invalid document summary response" : "GST API is not running. Restart the local API.");
  }
  if (!response.ok) throw new Error(payload.message || "Failed to load document summary");

  const rows = (pick(payload, "rows") ?? []) as Raw[];
  return {
    from: String(pick(payload, "from") ?? from).slice(0, 10),
    to: String(pick(payload, "to") ?? to).slice(0, 10),
    generatedAtUtc: String(pick(payload, "generatedAtUtc") ?? ""),
    companies: strings(pick(payload, "companies")),
    rows: rows.map((r) => ({
      company: String(pick(r, "company") ?? ""),
      nature: String(pick(r, "nature") ?? ""),
      series: String(pick(r, "series") ?? ""),
      fromNo: String(pick(r, "fromNo") ?? ""),
      toNo: String(pick(r, "toNo") ?? ""),
      totalNumber: Number(pick(r, "totalNumber") ?? 0),
      cancelled: Number(pick(r, "cancelled") ?? 0),
      netIssued: Number(pick(r, "netIssued") ?? 0),
      missingNumbers: strings(pick(r, "missingNumbers")),
      missingTruncated: Boolean(pick(r, "missingTruncated")),
      voucherTypes: strings(pick(r, "voucherTypes")),
      firstDate: String(pick(r, "firstDate") ?? "").slice(0, 10),
      lastDate: String(pick(r, "lastDate") ?? "").slice(0, 10),
    })),
  };
}

export async function downloadGstDocumentSummaryExcel(
  from: string,
  to: string,
  company?: string,
): Promise<string> {
  const params = new URLSearchParams({ from, to });
  if (company) params.set("company", company);
  const response = await fetch(getApiUrl(`/api/Gst/document-summary/excel?${params}`));
  if (!response.ok) {
    let message = "Failed to download Excel";
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
    response.headers.get("content-disposition")?.match(/filename="?([^";]+)"?/i)?.[1] ||
    `gst-document-summary-${from}-to-${to}.xlsx`;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}

// ---------------------------------------------------------------- GSTR-1 return

export interface Gstr1Scope {
  key: string;
  label: string;
  gstin: string;
  companies: string[];
  isGroup: boolean;
}

export interface Gstr1SectionSummary {
  section: string;
  label: string;
  documents: number;
  invoiceValue: number;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
}

export interface Gstr1ReconRow {
  label: string;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
}

export interface Gstr1B2bRow {
  company: string;
  gstin: string;
  receiverName: string;
  invoiceNo: string;
  erpInvoiceNo: string;
  invoiceDate: string;
  invoiceValue: number;
  placeOfSupply: string;
  reverseCharge: string;
  invoiceType: string;
  rate: number;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  voucherType: string;
  approved: boolean;
  salesLedger: string;
  /** Invoice-level amounts, carried on the first rate row of the invoice only. */
  tcs: number;
  otherCharges: number;
}

export interface Gstr1B2clRow {
  company: string;
  invoiceNo: string;
  erpInvoiceNo: string;
  invoiceDate: string;
  invoiceValue: number;
  placeOfSupply: string;
  rate: number;
  taxable: number;
  igst: number;
  cess: number;
  receiverName: string;
  voucherType: string;
  approved: boolean;
}

export interface Gstr1B2csRow {
  type: string;
  placeOfSupply: string;
  rate: number;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  documents: number;
}

export interface Gstr1ExpRow {
  company: string;
  exportType: string;
  invoiceNo: string;
  erpInvoiceNo: string;
  invoiceDate: string;
  invoiceValue: number;
  portCode: string;
  shippingBillNo: string;
  shippingBillDate: string | null;
  rate: number;
  taxable: number;
  igst: number;
  cess: number;
  receiverName: string;
  voucherType: string;
  approved: boolean;
  fob: number | null;
  egmNo: string;
  egmDate: string | null;
  icegateSbNo: string;
  icegateInvoiceNo: string;
  icegateInvoiceDate: string | null;
  icegateIgst: number | null;
  icegateStatus: "" | "Matched" | "Differences" | "Not in ICEGATE" | "No ICEGATE data";
  icegateNote: string;
  filledFromIcegate: boolean;
}

export interface Gstr1IcegateExtra {
  company: string;
  companyLabel: string;
  portCode: string;
  shippingBillNo: string;
  shippingBillDate: string | null;
  invoiceNo: string;
  invoiceDate: string | null;
  fob: number | null;
  igstPaid: number | null;
  egmNo: string;
  egmDate: string | null;
}

export interface Gstr1IcegateSummary {
  hasData: boolean;
  shippingBillsStored: number;
  lastUploadUtc: string | null;
  lastFileName: string | null;
  matched: number;
  withDifferences: number;
  notInIcegate: number;
  noData: number;
  notInErp: Gstr1IcegateExtra[];
}

export interface IcegateUpload {
  id: string;
  fileName: string;
  uploadedAtUtc: string;
  shippingBills: number;
  invoices: number;
  replaced: number;
  firstSbDate: string | null;
  lastSbDate: string | null;
  companyLabels: string[];
}

export interface IcegateStatus {
  shippingBills: number;
  invoices: number;
  uploads: IcegateUpload[];
}

export interface Gstr1NoteRow {
  company: string;
  gstin: string;
  receiverName: string;
  noteNo: string;
  erpNoteNo: string;
  noteDate: string;
  noteType: "C" | "D";
  placeOfSupply: string;
  reverseCharge: string;
  supplyType: string;
  noteValue: number;
  rate: number;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  originalInvoiceNo: string;
  originalInvoiceDate: string | null;
  erpType: string;
  approved: boolean;
}

export interface Gstr1NilRow {
  description: string;
  nilRated: number;
  exempted: number;
  nonGst: number;
}

export interface Gstr1HsnRow {
  hsn: string;
  description: string;
  uqc: string;
  quantity: number;
  totalValue: number;
  rate: number;
  taxable: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  /** ERP commodity names behind the HSN, highest taxable value first, joined with " / ". */
  commodity: string;
}

export interface Gstr1Exception {
  category: string;
  title: string;
  severity: "error" | "warning" | "info";
  company: string;
  documentNo: string;
  documentDate: string | null;
  party: string;
  detail: string;
  amount: number | null;
}

export interface Gstr1Report {
  from: string;
  to: string;
  generatedAtUtc: string;
  includeUnapproved: boolean;
  scope: string;
  scopeLabel: string;
  gstins: string[];
  companies: string[];
  scopes: Gstr1Scope[];
  summary: Gstr1SectionSummary[];
  reconciliation: Gstr1ReconRow[];
  reconciliationDifference: number;
  b2b: Gstr1B2bRow[];
  b2cl: Gstr1B2clRow[];
  b2cs: Gstr1B2csRow[];
  exp: Gstr1ExpRow[];
  cdnr: Gstr1NoteRow[];
  cdnur: Gstr1NoteRow[];
  nil: Gstr1NilRow[];
  hsnB2b: Gstr1HsnRow[];
  hsnB2c: Gstr1HsnRow[];
  docs: GstDocumentSeries[];
  exceptions: Gstr1Exception[];
  icegate: Gstr1IcegateSummary | null;
  /** B2B + B2C combined HSN summary. */
  hsnSummary: Gstr1HsnRow[] | null;
  salesRegister: Gstr1SalesRegisterRow[] | null;
  trialBalance: Gstr1TrialBalanceRow[] | null;
  ledgerRecon: Gstr1LedgerReconRow[] | null;
}

/** Invoice line; grossAmount, tcs and otherCharges are carried on the first line of each invoice only. */
export interface Gstr1SalesRegisterRow {
  company: string;
  salesLedger: string;
  voucherType: string;
  invoiceNo: string;
  erpInvoiceNo: string;
  invoiceDate: string;
  party: string;
  gstin: string;
  placeOfSupply: string;
  hsn: string;
  commodity: string;
  quantity: number;
  uqc: string;
  rate: number;
  value: number;
  igst: number;
  cgst: number;
  sgst: number;
  tcs: number;
  otherCharges: number;
  grossAmount: number;
  gstr1Status: string;
}

export interface Gstr1TrialBalanceRow {
  company: string;
  /** Primary head: Liabilities, Assets, Income, Expenses (or Unclassified). */
  primary: string;
  group: string;
  under: string;
  ledger: string;
  openingDebit: number;
  openingCredit: number;
  debit: number;
  credit: number;
  closingDebit: number;
  closingCredit: number;
  isPnl: boolean;
  /** Not an ERP ledger: previous years' P&L carried forward, or the difference in ERP postings. */
  isAdjustment: boolean;
  /** Period credit − debit. */
  net: number;
}

export interface Gstr1LedgerReconRow {
  salesLedger: string;
  under: string;
  registerGross: number;
  registerValue: number;
  hsnInvoiceValue: number;
  hsnTaxable: number;
  trialBalance: number;
  diffRegisterHsn: number;
  diffRegisterTb: number;
  remarks: string;
  /** Sales Reg. GrossAmount − HSN invoice value. */
  diffInvoiceValue: number;
  /** Causes of the invoice value difference. */
  invoiceRemarks: string;
}

async function readJson<T>(response: Response, fallback: string): Promise<T> {
  const text = await response.text();
  let payload: (T & { message?: string }) | null = null;
  try {
    payload = text ? (JSON.parse(text) as T & { message?: string }) : null;
  } catch {
    throw new Error(response.ok ? fallback : "GST API is not running. Restart the local API.");
  }
  if (!response.ok || !payload) throw new Error(payload?.message || fallback);
  return payload;
}

export async function getIcegateStatus(): Promise<IcegateStatus> {
  return readJson<IcegateStatus>(await fetch(getApiUrl("/api/Gst/icegate")), "Failed to load ICEGATE uploads");
}

export async function uploadIcegateFile(file: File): Promise<{ upload: IcegateUpload; status: IcegateStatus }> {
  const body = new FormData();
  body.append("file", file);
  const response = await fetch(getApiUrl("/api/Gst/icegate"), { method: "POST", body });
  return readJson(response, "ICEGATE upload failed");
}

export async function deleteIcegateUpload(id: string): Promise<IcegateStatus> {
  const response = await fetch(getApiUrl(`/api/Gst/icegate/${encodeURIComponent(id)}`), { method: "DELETE" });
  return readJson<IcegateStatus>(response, "Failed to remove ICEGATE upload");
}

function gstr1Params(from: string, to: string, scope: string, includeUnapproved: boolean): URLSearchParams {
  const params = new URLSearchParams({ from, to, includeUnapproved: String(includeUnapproved) });
  if (scope) params.set("scope", scope);
  return params;
}

export async function getGstr1Report(
  from: string,
  to: string,
  scope: string,
  includeUnapproved: boolean,
  refresh = false,
): Promise<Gstr1Report> {
  const params = gstr1Params(from, to, scope, includeUnapproved);
  if (refresh) params.set("refresh", "true");
  const response = await fetch(getApiUrl(`/api/Gst/gstr1?${params}`));
  const text = await response.text();
  let payload: (Partial<Gstr1Report> & { message?: string }) | null = null;
  try {
    payload = text ? (JSON.parse(text) as Partial<Gstr1Report> & { message?: string }) : null;
  } catch {
    throw new Error(response.ok ? "Invalid GSTR-1 response" : "GST API is not running. Restart the local API.");
  }
  if (!response.ok || !payload) throw new Error(payload?.message || "Failed to load GSTR-1 report");
  return payload as Gstr1Report;
}

export async function downloadGstr1Excel(
  from: string,
  to: string,
  scope: string,
  includeUnapproved: boolean,
): Promise<string> {
  const params = gstr1Params(from, to, scope, includeUnapproved);
  const response = await fetch(getApiUrl(`/api/Gst/gstr1/excel?${params}`));
  if (!response.ok) {
    let message = "Failed to download Excel";
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
    response.headers.get("content-disposition")?.match(/filename="?([^";]+)"?/i)?.[1] ||
    `gstr1-${from.slice(0, 7)}.xlsx`;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}
