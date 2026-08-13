import type { ChatApiResponse, ChatTableUsed } from "@/lib/chat-types";
import { SUGGESTED_PROMPTS } from "@/lib/chat-types";

import { formatCell as formatCellRich } from "@/lib/result-card-format";

export function formatCell(value: unknown, column?: string): string {
  if (column) return formatCellRich(value, column);
  if (value == null) return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

/** Split answer into a short headline + remaining body. */
export function splitAnswer(answer: string): { headline: string; body: string } {
  const trimmed = answer.trim();
  if (!trimmed) return { headline: "No answer returned.", body: "" };

  const firstBreak = trimmed.search(/[.!?\n]/);
  if (firstBreak > 0 && firstBreak < 160) {
    const headline = trimmed.slice(0, firstBreak + 1).trim();
    const body = trimmed.slice(firstBreak + 1).trim();
    return { headline, body };
  }

  if (trimmed.length <= 120) return { headline: trimmed, body: "" };
  return { headline: `${trimmed.slice(0, 117)}…`, body: trimmed };
}

export function getFollowUpPrompts(response: ChatApiResponse): string[] {
  const domains = new Set(
    response.tablesUsed.map((t) => t.domain.toLowerCase()).filter(Boolean),
  );
  const objectNames = response.tablesUsed.map((t) => t.objectName.toLowerCase()).join(" ");

  const preferredCategories: string[] = [];
  if (
    domains.has("po") ||
    domains.has("payment") ||
    objectNames.includes("approvepo") ||
    objectNames.includes("purchasepayment")
  ) {
    preferredCategories.push("Approvals");
  }
  if (domains.has("warehouse") || domains.has("warehousestock") || objectNames.includes("warehouse")) {
    preferredCategories.push("Stock");
  }
  if (domains.has("ledger") || objectNames.includes("ledgermaster") || objectNames.includes("factoryinfo")) {
    preferredCategories.push("Ledgers");
  }
  if (domains.has("production") || objectNames.includes("production")) {
    preferredCategories.push("Production");
  }
  if (
    domains.has("mrn") ||
    domains.has("vendor") ||
    objectNames.includes("vendor") ||
    objectNames.includes("storeinward")
  ) {
    preferredCategories.push("MRN & Vendors");
  }

  const picks: string[] = [];
  const categories =
    preferredCategories.length > 0
      ? preferredCategories
      : SUGGESTED_PROMPTS.map((g) => g.category);

  for (const cat of categories) {
    const group = SUGGESTED_PROMPTS.find((g) => g.category === cat);
    if (!group) continue;
    for (const p of group.prompts) {
      if (!picks.includes(p)) picks.push(p);
      if (picks.length >= 3) return picks;
    }
  }

  for (const group of SUGGESTED_PROMPTS) {
    for (const p of group.prompts) {
      if (!picks.includes(p)) picks.push(p);
      if (picks.length >= 3) return picks;
    }
  }

  return picks.slice(0, 3);
}

export function rowsToCsv(rows: Record<string, unknown>[]): string {
  if (rows.length === 0) return "";
  const columns = Object.keys(rows[0]);
  const escape = (v: unknown) => {
    const s = v == null ? "" : typeof v === "object" ? JSON.stringify(v) : String(v);
    if (/[",\n\r]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
    return s;
  };
  const header = columns.map(escape).join(",");
  const lines = rows.map((row) => columns.map((c) => escape(row[c])).join(","));
  return [header, ...lines].join("\n");
}

export function downloadCsv(
  rows: Record<string, unknown>[],
  filename?: string,
) {
  const stamp = new Date().toISOString().slice(0, 19).replace(/[:T]/g, "-");
  const csv = rowsToCsv(rows);
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename ?? `assistant-export-${stamp}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

/** Badge label: "50 of 127 rows" when capped, else "N rows". */
export function formatRowCountBadge(response: {
  rowCount: number;
  totalCount?: number | null;
  truncated?: boolean;
}): string {
  const n = response.rowCount;
  const unit = n === 1 ? "row" : "rows";
  if (
    response.truncated &&
    response.totalCount != null &&
    response.totalCount > n
  ) {
    return `${n} of ${response.totalCount} ${unit}`;
  }
  if (response.truncated) {
    return `${n}+ ${unit} (capped)`;
  }
  if (response.totalCount != null && response.totalCount !== n && n === 0) {
    return `${response.totalCount} ${response.totalCount === 1 ? "row" : "rows"}`;
  }
  return `${n} ${unit}`;
}

export function primaryTables(tables: ChatTableUsed[], limit = 4): ChatTableUsed[] {
  return [...tables].sort((a, b) => b.score - a.score).slice(0, limit);
}

export type HistoryBucket = "Today" | "Yesterday" | "Earlier";

export interface ChatHistoryItem {
  question: string;
  timestamp: number;
}

export interface GroupedHistory {
  label: HistoryBucket;
  items: ChatHistoryItem[];
}

function startOfDay(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

export function groupHistoryByDay(items: ChatHistoryItem[]): GroupedHistory[] {
  const now = new Date();
  const todayStart = startOfDay(now);
  const yesterdayStart = todayStart - 24 * 60 * 60 * 1000;

  const buckets: Record<HistoryBucket, ChatHistoryItem[]> = {
    Today: [],
    Yesterday: [],
    Earlier: [],
  };

  for (const item of items) {
    if (item.timestamp >= todayStart) buckets.Today.push(item);
    else if (item.timestamp >= yesterdayStart) buckets.Yesterday.push(item);
    else buckets.Earlier.push(item);
  }

  return (["Today", "Yesterday", "Earlier"] as HistoryBucket[])
    .map((label) => ({ label, items: buckets[label] }))
    .filter((g) => g.items.length > 0);
}
