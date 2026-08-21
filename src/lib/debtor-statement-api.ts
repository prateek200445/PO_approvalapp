import { getApiUrl } from "@/lib/api-config";
import {
  type DebtorBillRow,
  type DebtorBookDebtRow,
  type DebtorCompanyOption,
  type DebtorPivotRow,
  type DebtorStatementKpis,
  type DebtorStatementQuery,
  type DebtorStatementResult,
} from "@/lib/debtor-statement-types";

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

function bool(v: unknown, fallback = false): boolean {
  if (typeof v === "boolean") return v;
  if (v == null) return fallback;
  return String(v).toLowerCase() === "true";
}

export async function getDebtorStatementCompanies(): Promise<DebtorCompanyOption[]> {
  const res = await fetch(getApiUrl("/api/debtor-statement/companies"));
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to load companies");
  return (Array.isArray(data) ? data : []).map((c: Record<string, unknown>) => ({
    value: str(c.value ?? c.Value),
    label: str(c.label ?? c.Label),
    companyType: num(c.companyType ?? c.CompanyType),
    companyName: str(c.companyName ?? c.CompanyName),
    companyId: num(c.companyId ?? c.CompanyId),
  }));
}

export async function queryDebtorStatement(
  query: DebtorStatementQuery,
): Promise<DebtorStatementResult> {
  const res = await fetch(getApiUrl("/api/debtor-statement/query"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      company: query.company,
      asOn: query.asOn,
      includeCurrentAssets: query.includeCurrentAssets,
    }),
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || "Failed to build debtor statement");
  return normalizeResult(data);
}

export async function downloadDebtorStatementExcel(query: DebtorStatementQuery): Promise<void> {
  const res = await fetch(getApiUrl("/api/debtor-statement/export"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      company: query.company,
      asOn: query.asOn,
      includeCurrentAssets: query.includeCurrentAssets,
    }),
  });
  if (!res.ok) {
    let message = "Failed to export debtor statement";
    try {
      const data = await res.json();
      message = data.message || message;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `debtor-statement-${query.asOn}.xlsx`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

function normalizeResult(data: Record<string, unknown>): DebtorStatementResult {
  const kpis = (data.kpis ?? data.Kpis ?? {}) as Record<string, unknown>;
  return {
    company: str(data.company ?? data.Company),
    companyLabel: str(data.companyLabel ?? data.CompanyLabel),
    asOn: str(data.asOn ?? data.AsOn).slice(0, 10),
    includeCurrentAssets: bool(data.includeCurrentAssets ?? data.IncludeCurrentAssets, true),
    freezeRule: str(data.freezeRule ?? data.FreezeRule) || "as-on",
    allocationRule: str(data.allocationRule ?? data.AllocationRule) || "LIFO",
    kpis: normalizeKpis(kpis),
    bills: ((data.bills ?? data.Bills ?? []) as Record<string, unknown>[]).map(normalizeBill),
    pivot: ((data.pivot ?? data.Pivot ?? []) as Record<string, unknown>[]).map(normalizePivot),
    bookDebts: ((data.bookDebts ?? data.BookDebts ?? []) as Record<string, unknown>[]).map(
      normalizeBookDebt,
    ),
  };
}

function normalizeKpis(k: Record<string, unknown>): DebtorStatementKpis {
  return {
    companyCount: num(k.companyCount ?? k.CompanyCount),
    partyCount: num(k.partyCount ?? k.PartyCount),
    billCount: num(k.billCount ?? k.BillCount),
    openBillCount: num(k.openBillCount ?? k.OpenBillCount),
    originalTotal: num(k.originalTotal ?? k.OriginalTotal),
    allocatedTotal: num(k.allocatedTotal ?? k.AllocatedTotal),
    netTotal: num(k.netTotal ?? k.NetTotal),
    bookTotal: num(k.bookTotal ?? k.BookTotal),
    diffTotal: num(k.diffTotal ?? k.DiffTotal),
    lifoPartyCount: num(k.lifoPartyCount ?? k.LifoPartyCount),
    nonBillGapPartyCount: num(k.nonBillGapPartyCount ?? k.NonBillGapPartyCount),
  };
}

function normalizeBill(r: Record<string, unknown>): DebtorBillRow {
  return {
    type: str(r.type ?? r.Type),
    category: str(r.category ?? r.Category),
    companyName: str(r.companyName ?? r.CompanyName),
    partyName: str(r.partyName ?? r.PartyName),
    gstin: str(r.gstin ?? r.Gstin),
    invoiceNo: str(r.invoiceNo ?? r.InvoiceNo),
    invoiceDate: str(r.invoiceDate ?? r.InvoiceDate).slice(0, 10),
    originalAmount: num(r.originalAmount ?? r.OriginalAmount),
    allocatedAmount: num(r.allocatedAmount ?? r.AllocatedAmount),
    netAmount: num(r.netAmount ?? r.NetAmount),
    days: num(r.days ?? r.Days),
    ageing: str(r.ageing ?? r.Ageing),
    ageing2: str(r.ageing2 ?? r.Ageing2),
    status: str(r.status ?? r.Status),
    under: str(r.under ?? r.Under),
  };
}

function normalizePivot(r: Record<string, unknown>): DebtorPivotRow {
  return {
    partyName: str(r.partyName ?? r.PartyName),
    gstin: str(r.gstin ?? r.Gstin),
    type: str(r.type ?? r.Type),
    category: str(r.category ?? r.Category),
    zeroTo120: num(r.zeroTo120 ?? r.ZeroTo120),
    oneTo90: num(r.oneTo90 ?? r.OneTo90),
    ninetyOneTo120: num(r.ninetyOneTo120 ?? r.NinetyOneTo120),
    oneTwentyOneTo180: num(r.oneTwentyOneTo180 ?? r.OneTwentyOneTo180),
    over180: num(r.over180 ?? r.Over180),
    grandTotal: num(r.grandTotal ?? r.GrandTotal),
    asPerBook: num(r.asPerBook ?? r.AsPerBook),
    diff: num(r.diff ?? r.Diff),
    originalTotal: num(r.originalTotal ?? r.OriginalTotal),
    allocatedTotal: num(r.allocatedTotal ?? r.AllocatedTotal),
    status: str(r.status ?? r.Status),
  };
}

function normalizeBookDebt(r: Record<string, unknown>): DebtorBookDebtRow {
  return {
    bucket: str(r.bucket ?? r.Bucket),
    government: num(r.government ?? r.Government),
    associates: num(r.associates ?? r.Associates),
    other: num(r.other ?? r.Other),
    total: num(r.total ?? r.Total),
  };
}
