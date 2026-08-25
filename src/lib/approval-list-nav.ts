export type ApprovalListKind = "po" | "payment" | "advancePayment" | "workorder" | "indent";

function storageKey(kind: ApprovalListKind) {
  return `${kind}-approval-nav`;
}

export function setApprovalListNav(kind: ApprovalListKind, ids: string[]) {
  try {
    sessionStorage.setItem(storageKey(kind), JSON.stringify(ids.filter(Boolean)));
  } catch {
    /* ignore */
  }
}

export function getApprovalListNav(kind: ApprovalListKind): string[] {
  try {
    const raw = sessionStorage.getItem(storageKey(kind));
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter(Boolean) : [];
  } catch {
    return [];
  }
}

export function getApprovalListNeighbors(
  currentId: string,
  ids: string[],
): { prev: string | null; next: string | null; index: number; total: number } {
  const idx = ids.findIndex((id) => id === currentId);
  if (idx < 0) return { prev: null, next: null, index: -1, total: ids.length };
  return {
    prev: idx > 0 ? ids[idx - 1] : null,
    next: idx < ids.length - 1 ? ids[idx + 1] : null,
    index: idx + 1,
    total: ids.length,
  };
}

/** Remove current item from session nav and return the next id in list order (sync, no network). */
export function consumeApprovalListNav(kind: ApprovalListKind, currentId: string): string | null {
  const ids = getApprovalListNav(kind);
  if (!ids.includes(currentId)) return null;
  const { next } = getApprovalListNeighbors(currentId, ids);
  setApprovalListNav(
    kind,
    ids.filter((id) => id !== currentId),
  );
  return next;
}

/** @deprecated use setApprovalListNav('po', ...) */
export function setPoApprovalNav(poNos: string[]) {
  setApprovalListNav("po", poNos);
}

/** @deprecated use getApprovalListNav('po') */
export function getPoApprovalNav(): string[] {
  return getApprovalListNav("po");
}

/** @deprecated use getApprovalListNeighbors */
export function getPoNeighbors(currentPoNo: string, poNos: string[]) {
  return getApprovalListNeighbors(currentPoNo, poNos);
}
