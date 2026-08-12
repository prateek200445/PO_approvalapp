import { getApiUrl } from "@/lib/api-config";
import type { ChatHistoryItem } from "@/lib/chat-helpers";
import type { ChatApiResponse, ChatMessage, ChatTableUsed } from "@/lib/chat-types";

const SESSION_KEY = "po-chat-session";
const HISTORY_KEY = "po-chat-history";
const MAX_HISTORY = 30;

function normalizeTable(t: Record<string, unknown>): ChatTableUsed {
  return {
    objectName: String(t.objectName ?? t.ObjectName ?? ""),
    domain: String(t.domain ?? t.Domain ?? ""),
    score: Number(t.score ?? t.Score ?? 0),
  };
}

export function normalizeChatResponse(data: Record<string, unknown>): ChatApiResponse {
  const tables = (data.tablesUsed ?? data.TablesUsed ?? []) as Record<string, unknown>[];
  const rows = (data.rows ?? data.Rows ?? []) as Record<string, unknown>[];
  const totalRaw = data.totalCount ?? data.TotalCount;
  const totalCount =
    totalRaw === null || totalRaw === undefined || totalRaw === ""
      ? null
      : Number(totalRaw);

  return {
    answer: String(data.answer ?? data.Answer ?? ""),
    sql: String(data.sql ?? data.Sql ?? ""),
    tablesUsed: tables.map(normalizeTable),
    rows,
    rowCount: Number(data.rowCount ?? data.RowCount ?? rows.length),
    totalCount: Number.isFinite(totalCount as number) ? (totalCount as number) : null,
    truncated: Boolean(data.truncated ?? data.Truncated ?? false),
    warning: (data.warning ?? data.Warning ?? null) as string | null,
  };
}

export async function sendChatMessage(message: string): Promise<ChatApiResponse> {
  const response = await fetch(getApiUrl("/api/chat"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ message, topK: 4 }),
  });

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    const errMsg = String(data.message ?? data.Message ?? `Request failed (${response.status})`);
    throw new Error(errMsg);
  }

  return normalizeChatResponse(data);
}

/** Download full result set for the chat SQL (strips TOP; server caps at 5k). */
export async function exportChatCsv(sql: string, filename?: string): Promise<{ truncated: boolean; rowCount: number }> {
  const response = await fetch(getApiUrl("/api/chat/export"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sql }),
  });

  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    const errMsg = String(
      (data as { message?: string; Message?: string }).message ??
        (data as { Message?: string }).Message ??
        `Export failed (${response.status})`,
    );
    throw new Error(errMsg);
  }

  const blob = await response.blob();
  const stamp = new Date().toISOString().slice(0, 19).replace(/[:T]/g, "-");
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename ?? `assistant-export-${stamp}.csv`;
  a.click();
  URL.revokeObjectURL(url);

  const rowCount = Number(response.headers.get("X-Row-Count") ?? 0);
  const truncated = (response.headers.get("X-Truncated") ?? "").toLowerCase() === "true";
  return { truncated, rowCount };
}

export function loadChatSession(): ChatMessage[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as ChatMessage[];
    return Array.isArray(parsed) ? parsed.filter((m) => !m.pending) : [];
  } catch {
    return [];
  }
}

export function saveChatSession(messages: ChatMessage[]) {
  if (typeof window === "undefined") return;
  const toSave = messages.filter((m) => !m.pending);
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(toSave));
}

export function clearChatSession() {
  if (typeof window === "undefined") return;
  sessionStorage.removeItem(SESSION_KEY);
}

export function loadChatHistory(): ChatHistoryItem[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(HISTORY_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as ChatHistoryItem[];
    if (!Array.isArray(parsed)) return [];
    return parsed
      .filter((h) => h && typeof h.question === "string" && typeof h.timestamp === "number")
      .slice(0, MAX_HISTORY);
  } catch {
    return [];
  }
}

/** Upsert question at top of history (keeps newest first). */
export function pushChatHistory(question: string): ChatHistoryItem[] {
  if (typeof window === "undefined") return [];
  const trimmed = question.trim();
  if (!trimmed) return loadChatHistory();

  const now = Date.now();
  const prev = loadChatHistory().filter(
    (h) => h.question.toLowerCase() !== trimmed.toLowerCase(),
  );
  const next = [{ question: trimmed, timestamp: now }, ...prev].slice(0, MAX_HISTORY);
  localStorage.setItem(HISTORY_KEY, JSON.stringify(next));
  return next;
}

export function clearChatHistory() {
  if (typeof window === "undefined") return;
  localStorage.removeItem(HISTORY_KEY);
}

export function createMessageId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}
