import { getApiUrl } from "@/lib/api-config";

export interface IntercompanyLine {
  company: string;
  counterparty: string;
  ledgerName: string;
  balance: number;
  balanceCr: number;
}

export interface IntercompanyMatrix {
  company: string;
  amounts: Record<string, number>;
  total: number;
}

export interface IntercompanyDashboard {
  asOf: string;
  counterparties: string[];
  matrices: IntercompanyMatrix[];
  lines: IntercompanyLine[];
}

export async function getIntercompanyDashboard(asOf: string, refresh = false): Promise<IntercompanyDashboard> {
  const params = new URLSearchParams({ asOf });
  if (refresh) params.set("refresh", "true");
  const response = await fetch(getApiUrl(`/api/Intercompany?${params}`));
  const text = await response.text();
  let payload: Record<string, unknown> & { message?: string } = {};
  try {
    payload = text ? (JSON.parse(text) as Record<string, unknown> & { message?: string }) : {};
  } catch {
    throw new Error(response.ok ? "Invalid intercompany response" : "Intercompany API is not running. Restart the local API.");
  }
  if (!response.ok) {
    throw new Error(payload.message || "Failed to load intercompany balances");
  }

  const matricesRaw = (payload.matrices ?? payload.Matrices ?? []) as Array<Record<string, unknown>>;
  const linesRaw = (payload.lines ?? payload.Lines ?? []) as Array<Record<string, unknown>>;
  const counterparties = ((payload.counterparties ?? payload.Counterparties ?? []) as unknown[])
    .map((n) => String(n))
    .filter(Boolean);

  return {
    asOf: String(payload.asOf ?? payload.AsOf ?? asOf),
    counterparties,
    matrices: matricesRaw.map((m) => ({
      company: String(m.company ?? m.Company ?? ""),
      amounts: (m.amounts ?? m.Amounts ?? {}) as Record<string, number>,
      total: Number(m.total ?? m.Total ?? 0),
    })),
    lines: linesRaw.map((r) => ({
      company: String(r.company ?? r.Company ?? ""),
      counterparty: String(r.counterparty ?? r.Counterparty ?? ""),
      ledgerName: String(r.ledgerName ?? r.LedgerName ?? ""),
      balance: Number(r.balance ?? r.Balance ?? 0),
      balanceCr: Number(r.balanceCr ?? r.BalanceCr ?? 0),
    })),
  };
}

export function formatInr(n: number): string {
  return new Intl.NumberFormat("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(n);
}

const INR_PER_CRORE = 1_00_00_000;

export function toCrore(amount: number): number {
  return amount / INR_PER_CRORE;
}

export function formatCrore(amount: number): string {
  return formatInr(toCrore(amount));
}

export function formatAsOn(iso: string): string {
  const [y, m, d] = iso.split("-").map(Number);
  if (!y || !m || !d) return iso;
  return new Date(y, m - 1, d).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
}
