const MOCK_STORAGE_KEY = "CHAT_USE_MOCK";

/** True when frontend should use built-in mock responses (no API / LLM). */
export function isChatMockEnabled(): boolean {
  if (import.meta.env.VITE_CHAT_MOCK === "false") return false;

  if (typeof window !== "undefined") {
    const stored = localStorage.getItem(MOCK_STORAGE_KEY);
    if (stored === "true") return true;
    if (stored === "false") return false;
  }

  return import.meta.env.VITE_CHAT_MOCK === "true";
}

export function setChatMockEnabled(enabled: boolean) {
  if (typeof window === "undefined") return;
  if (import.meta.env.VITE_CHAT_MOCK === "false" && enabled) return;
  localStorage.setItem(MOCK_STORAGE_KEY, enabled ? "true" : "false");
}

export function toggleChatMockEnabled(): boolean {
  if (import.meta.env.VITE_CHAT_MOCK === "false") return false;
  const next = !isChatMockEnabled();
  setChatMockEnabled(next);
  return next;
}

export function isMockToggleAllowed(): boolean {
  return import.meta.env.VITE_CHAT_MOCK !== "false";
}
