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

export interface CompanyPosition {
  company: string;
  net: number;
  action: "pay" | "receive" | "settled";
}

export interface SettlementTransfer {
  from: string;
  to: string;
  amount: number;
}

export interface PairSettlement {
  company: string;
  counterparty: string;
  balance: number;
  payer: string;
  receiver: string;
  amount: number;
}

export interface RotationStep {
  from: string;
  to: string;
  amount: number;
}

function sameName(a: string, b: string): boolean {
  return a.toLowerCase() === b.toLowerCase();
}

export function orderRotationCompanies(companies: string[]): string[] {
  const names: string[] = [];
  for (const raw of companies) {
    const name = raw.trim();
    if (!name) continue;
    if (!names.some((n) => sameName(n, name))) names.push(name);
  }
  if (names.length < 2) return names;

  const first = names.find((n) => {
    const k = n.toLowerCase();
    return k.includes("enterprise") && k.includes("hcp");
  });
  const last = names.find((n) => n.toLowerCase().includes("bulkpack") && !sameName(n, first ?? ""));
  if (!first || !last) return names;
  return [first, ...names.filter((n) => !sameName(n, first) && !sameName(n, last)), last];
}

function pairBook(
  matrices: IntercompanyMatrix[],
  company: string,
  counterparty: string,
): number | null {
  const row = matrices.find((m) => sameName(m.company, company));
  if (!row?.amounts) return null;
  const hit = Object.entries(row.amounts).find(([name]) => sameName(name, counterparty));
  if (!hit) return null;
  return Number(hit[1]) || 0;
}

/**
 * Loop hops from the real intercompany matrix.
 * Company[i] → Company[(i+1)%n]. Amount and who gives come from that pair's book:
 * minus = that company gives, plus = it gets (the other company gives).
 */
export function buildMoneyRotation(
  companies: string[],
  matrices: IntercompanyMatrix[] = [],
): RotationStep[] {
  const names = orderRotationCompanies(companies);
  if (names.length < 2) return [];

  return names.map((company, i) => {
    const next = names[(i + 1) % names.length];
    const book = pairBook(matrices, company, next);
    const reverse = pairBook(matrices, next, company);
    const value = book != null && Math.abs(book) >= 0.01 ? book : reverse != null ? -reverse : 0;
    const amount = Math.round(Math.abs(value) * 100) / 100;
    if (value <= 0) return { from: company, to: next, amount };
    return { from: next, to: company, amount };
  });
}

/** Every real From–To pair on the intercompany books. Minus = gives, plus = gets. */
export function buildBookTransfers(matrices: IntercompanyMatrix[]): RotationStep[] {
  return buildSettlementPlan(matrices)
    .transfers.filter((t) => t.amount >= 0.01)
    .sort((a, b) => b.amount - a.amount);
}

function pairKey(a: string, b: string): string {
  return [a, b].map((n) => n.toLowerCase()).sort().join("\0");
}

export function companyNet(m: IntercompanyMatrix): number {
  if (Number.isFinite(m.total)) return m.total;
  return Object.entries(m.amounts ?? {}).reduce((sum, [name, value]) => {
    if (name.toLowerCase() === m.company.toLowerCase()) return sum;
    return sum + (Number(value) || 0);
  }, 0);
}

/**
 * Pair-wise settlement from the actual From–To books.
 * Minus on a pair = that company pays (deduct). Plus = that company receives (add).
 * Does not invent payments between companies that have no ledger pair.
 */
export function buildSettlementPlan(matrices: IntercompanyMatrix[]): {
  positions: CompanyPosition[];
  transfers: SettlementTransfer[];
  leftover: number;
} {
  const positions: CompanyPosition[] = matrices
    .map((m) => {
      const rounded = Math.round(companyNet(m) * 100) / 100;
      return {
        company: m.company,
        net: rounded,
        action: rounded < -0.005 ? "pay" : rounded > 0.005 ? "receive" : "settled",
      } as CompanyPosition;
    })
    .sort((a, b) => a.net - b.net);

  const byCompany = new Map(matrices.map((m) => [m.company.toLowerCase(), m]));
  const seen = new Set<string>();
  const transfers: SettlementTransfer[] = [];
  let leftover = 0;

  for (const matrix of matrices) {
    for (const [counterparty, raw] of Object.entries(matrix.amounts ?? {})) {
      if (counterparty.toLowerCase() === matrix.company.toLowerCase()) continue;
      const key = pairKey(matrix.company, counterparty);
      if (seen.has(key)) continue;
      seen.add(key);

      const balance = Number(raw) || 0;
      const other = byCompany.get(counterparty.toLowerCase());
      const reverseRaw = other?.amounts
        ? Object.entries(other.amounts).find(([name]) => name.toLowerCase() === matrix.company.toLowerCase())?.[1]
        : undefined;
      const reverse = reverseRaw == null ? null : Number(reverseRaw) || 0;

      if (Math.abs(balance) < 0.01 && (reverse == null || Math.abs(reverse) < 0.01)) continue;

      if (reverse != null && Math.abs(balance + reverse) >= 1) {
        leftover += Math.abs(balance + reverse);
      }

      const book = Math.abs(balance) >= 0.01 ? balance : -(reverse ?? 0);
      const amount = Math.round(Math.abs(book) * 100) / 100;
      if (amount < 0.01) continue;

      transfers.push({
        from: book < 0 ? matrix.company : counterparty,
        to: book < 0 ? counterparty : matrix.company,
        amount,
      });
    }
  }

  transfers.sort((a, b) => b.amount - a.amount || a.from.localeCompare(b.from));

  return { positions, transfers, leftover: Math.round(leftover * 100) / 100 };
}

/** Pair-wise: minus on the book company means that company pays the counterparty. */
export function buildPairSettlements(matrix: IntercompanyMatrix | undefined): PairSettlement[] {
  if (!matrix) return [];
  return Object.entries(matrix.amounts ?? {})
    .map(([counterparty, raw]) => {
      const balance = Number(raw) || 0;
      const amount = Math.abs(balance);
      const pays = balance < 0;
      return {
        company: matrix.company,
        counterparty,
        balance,
        payer: pays ? matrix.company : counterparty,
        receiver: pays ? counterparty : matrix.company,
        amount,
      };
    })
    .filter((row) => row.amount >= 0.01)
    .sort((a, b) => a.balance - b.balance);
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
