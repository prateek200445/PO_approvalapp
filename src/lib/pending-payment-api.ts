import { getApiUrl, getApiBaseUrl } from "@/lib/api-config";

export type PendingPaymentRow = {
  companyName: string;
  partyName: string;
  billNo: string;
  billDate: string | null;
  mrNo: string;
  mrnDt: string | null;
  year: string;
  billAmount: number;
  paymentTerms: string;
  dueDt: string | null;
  deptt: string;
};

export type PendingPaymentResult = {
  items: PendingPaymentRow[];
  count: number;
  totalBillAmount: number;
  dateFrom: string;
  dateTo: string;
  source: string;
  asOf: string;
};

function apiUrl(path: string): string {
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

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

function dateStr(v: unknown): string | null {
  if (v == null || v === "") return null;
  const s = String(v);
  if (s.length >= 10) return s.slice(0, 10);
  return s;
}

function mapRow(row: Record<string, unknown>): PendingPaymentRow {
  return {
    companyName: str(row.companyName ?? row.CompanyName),
    partyName: str(row.partyName ?? row.PartyName),
    billNo: str(row.billNo ?? row.BillNo),
    billDate: dateStr(row.billDate ?? row.BillDate),
    mrNo: str(row.mrNo ?? row.MRNo ?? row.MrNo),
    mrnDt: dateStr(row.mrnDt ?? row.MRNDt ?? row.MrnDt),
    year: str(row.year ?? row.Year),
    billAmount: num(row.billAmount ?? row.BillAmount ?? row.BillAMount),
    paymentTerms: str(row.paymentTerms ?? row.PaymentTerms),
    dueDt: dateStr(row.dueDt ?? row.DueDt),
    deptt: str(row.deptt ?? row.Deptt),
  };
}

export async function getPendingPaymentCompanies(): Promise<string[]> {
  const response = await fetch(apiUrl("/api/pending-payment/companies"));
  const payload = (await response.json()) as { names?: string[]; message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to load companies");
  return payload.names ?? [];
}

export async function getPendingPayments(filters: {
  company: string;
  refresh?: boolean;
}): Promise<PendingPaymentResult> {
  const params = new URLSearchParams({
    company: filters.company || "All Companies",
    refresh: filters.refresh ? "true" : "false",
  });
  const response = await fetch(apiUrl(`/api/pending-payment?${params}`));
  const payload = (await response.json()) as Record<string, unknown> & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to load pending payments");

  const itemsRaw = (payload.items ?? payload.Items ?? []) as Array<Record<string, unknown>>;
  return {
    items: itemsRaw.map(mapRow),
    count: num(payload.count ?? payload.Count) || itemsRaw.length,
    totalBillAmount: num(payload.totalBillAmount ?? payload.TotalBillAmount),
    dateFrom: str(payload.dateFrom ?? payload.DateFrom),
    dateTo: str(payload.dateTo ?? payload.DateTo),
    source: str(payload.source ?? payload.Source),
    asOf: str(payload.asOf ?? payload.AsOf),
  };
}

export type PendingPaymentPunchLine = {
  companyName: string;
  partyName: string;
  billNo: string;
  billDate: string | null;
  mrNo: string;
  mrnDt: string | null;
  year: string;
  billAmount: number;
  paymentTerms: string;
  deptt: string;
};

export type PendingPaymentPunchResult = {
  created: Array<{
    paymentNo: string;
    companyName: string;
    partyName: string;
    billNo: string;
    mrNo: string;
    paymentAmount: number;
    approverCount: number;
  }>;
  createdCount: number;
  errors: string[];
  message: string;
};

export async function punchPendingPayments(request: {
  loginName: string;
  lines: PendingPaymentPunchLine[];
}): Promise<PendingPaymentPunchResult> {
  const response = await fetch(apiUrl("/api/pending-payment/punch"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      loginName: request.loginName,
      lines: request.lines.map((l) => ({
        companyName: l.companyName,
        partyName: l.partyName,
        billNo: l.billNo,
        billDate: l.billDate,
        mrNo: l.mrNo,
        mrnDt: l.mrnDt,
        year: l.year,
        billAmount: l.billAmount,
        paymentTerms: l.paymentTerms,
        deptt: l.deptt,
      })),
    }),
  });
  const payload = (await response.json()) as Record<string, unknown> & {
    message?: string;
    Message?: string;
  };
  if (!response.ok) {
    const errors = (payload.errors ?? payload.Errors ?? []) as string[];
    const msg =
      str(payload.message ?? payload.Message) ||
      (errors.length ? errors.join("; ") : "Punch payment request failed");
    throw new Error(msg);
  }
  const createdRaw = (payload.created ?? payload.Created ?? []) as Array<
    Record<string, unknown>
  >;
  const errorsRaw = (payload.errors ?? payload.Errors ?? []) as string[];
  return {
    created: createdRaw.map((r) => ({
      paymentNo: str(r.paymentNo ?? r.PaymentNo),
      companyName: str(r.companyName ?? r.CompanyName),
      partyName: str(r.partyName ?? r.PartyName),
      billNo: str(r.billNo ?? r.BillNo),
      mrNo: str(r.mrNo ?? r.MRNo ?? r.MrNo),
      paymentAmount: num(r.paymentAmount ?? r.PaymentAmount),
      approverCount: num(r.approverCount ?? r.ApproverCount),
    })),
    createdCount: num(payload.createdCount ?? payload.CreatedCount) || createdRaw.length,
    errors: errorsRaw.map(String),
    message: str(payload.message ?? payload.Message),
  };
}

export async function downloadPendingPaymentExcel(filters: {
  company: string;
  refresh?: boolean;
}): Promise<void> {
  const params = new URLSearchParams({
    company: filters.company || "All Companies",
    refresh: filters.refresh ? "true" : "false",
  });
  const response = await fetch(apiUrl(`/api/pending-payment/excel?${params}`));
  if (!response.ok) {
    const payload = (await response.json().catch(() => ({}))) as { message?: string };
    throw new Error(payload.message || "Could not create the Excel file");
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `Next_Week_Pending_Payment.xlsx`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function formatPendingAmount(value: number): string {
  return value.toLocaleString("en-IN", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });
}
