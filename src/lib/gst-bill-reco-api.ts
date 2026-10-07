import { getApiUrl } from "@/lib/api-config";

export type GstBillRecoRow = {
  status: string;
  companyName: string;
  voucherType: string;
  voucherNo: string;
  voucherDate: string | null;
  refNo: string;
  billDate: string | null;
  billNo: string;
  ledger: string;
  party: string;
  gstNo: string;
  grossAmount: number | null;
  erpTaxable: number | null;
  twoBTaxable: number | null;
  difference: number;
  totalC: number | null;
  otherAll: number | null;
  cgst: number | null;
  sgst: number | null;
  igst: number | null;
  twoBIgst: number | null;
  twoBCgstSgst: number | null;
  monthAsPer2B: string;
  twoBPeriod: string;
  documentType: string;
  ledgers: Record<string, number | null>;
};

export type GstBillRecoResult = {
  companyName: string;
  dateFrom: string;
  dateTo: string;
  amountTolerance: number;
  buyerGstin: string | null;
  companyGstin: string | null;
  warning: string | null;
  twoBRows: number;
  savedRows: number;
  erpRows: number;
  matched: number;
  mismatch: number;
  missingInErp: number;
  missingIn2B: number;
  rows: GstBillRecoRow[];
};

function str(value: unknown): string {
  return value == null ? "" : String(value);
}

function num(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function rowFrom(raw: Record<string, unknown>): GstBillRecoRow {
  const ledgersRaw = raw.ledgers;
  const ledgers: Record<string, number | null> = {};
  if (ledgersRaw && typeof ledgersRaw === "object") {
    for (const [key, value] of Object.entries(ledgersRaw as Record<string, unknown>))
      ledgers[key] = num(value);
  }
  return {
    status: str(raw.status),
    companyName: str(raw.companyName),
    voucherType: str(raw.voucherType),
    voucherNo: str(raw.voucherNo),
    voucherDate: raw.voucherDate == null ? null : str(raw.voucherDate),
    refNo: str(raw.refNo),
    billDate: raw.billDate == null ? null : str(raw.billDate),
    billNo: str(raw.billNo),
    ledger: str(raw.ledger),
    party: str(raw.party),
    gstNo: str(raw.gstNo),
    grossAmount: num(raw.grossAmount),
    erpTaxable: num(raw.erpTaxable),
    twoBTaxable: num(raw.twoBTaxable),
    difference: num(raw.difference) ?? 0,
    totalC: num(raw.totalC),
    otherAll: num(raw.otherAll),
    cgst: num(raw.cgst),
    sgst: num(raw.sgst),
    igst: num(raw.igst),
    twoBIgst: num(raw.twoBIgst),
    twoBCgstSgst: num(raw.twoBCgstSgst),
    monthAsPer2B: str(raw.monthAsPer2B),
    twoBPeriod: str(raw.twoBPeriod),
    documentType: str(raw.documentType),
    ledgers,
  };
}

export function formatMoney(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) return "";
  return value.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function formatIsoDate(value: string | null | undefined): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value.slice(0, 10);
  return date.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

async function readError(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as { message?: string };
    if (payload?.message) return payload.message;
  } catch {
    /* response was not JSON */
  }
  return "GST bill reconciliation failed.";
}

export async function getGstBillCompanies(): Promise<string[]> {
  const response = await fetch(getApiUrl("/api/gst-bill-reco/companies"));
  if (!response.ok) throw new Error(await readError(response));
  const payload = (await response.json()) as unknown;
  return Array.isArray(payload) ? payload.map((name) => str(name).trim()).filter(Boolean) : [];
}

export async function runGstBillReco(
  input: {
    file: File;
    companyName: string;
    dateFrom: string;
    dateTo: string;
    amountTolerance: number;
  },
  signal?: AbortSignal,
): Promise<GstBillRecoResult> {
  const body = new FormData();
  body.append("file", input.file);
  body.append("companyName", input.companyName);
  body.append("dateFrom", input.dateFrom);
  body.append("dateTo", input.dateTo);
  body.append("amountTolerance", String(input.amountTolerance));
  const response = await fetch(getApiUrl("/api/gst-bill-reco/run"), { method: "POST", body, signal });
  if (!response.ok) throw new Error(await readError(response));
  const payload = (await response.json()) as Record<string, unknown>;
  const rows = Array.isArray(payload.rows) ? payload.rows.map((row) => rowFrom(row as Record<string, unknown>)) : [];
  return {
    companyName: str(payload.companyName),
    dateFrom: str(payload.dateFrom),
    dateTo: str(payload.dateTo),
    amountTolerance: num(payload.amountTolerance) ?? 0,
    buyerGstin: payload.buyerGstin == null ? null : str(payload.buyerGstin),
    companyGstin: payload.companyGstin == null ? null : str(payload.companyGstin),
    warning: payload.warning == null ? null : str(payload.warning),
    twoBRows: num(payload.twoBRows) ?? 0,
    savedRows: num(payload.savedRows) ?? 0,
    erpRows: num(payload.erpRows) ?? 0,
    matched: num(payload.matched) ?? 0,
    mismatch: num(payload.mismatch) ?? 0,
    missingInErp: num(payload.missingInErp) ?? 0,
    missingIn2B: num(payload.missingIn2B) ?? rows.filter((row) => row.status === "Missing in 2B").length,
    rows,
  };
}

export async function exportGstBillReco(rows: GstBillRecoRow[]): Promise<Blob> {
  const response = await fetch(getApiUrl("/api/gst-bill-reco/export"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ rows }),
  });
  if (!response.ok) throw new Error(await readError(response));
  return response.blob();
}
