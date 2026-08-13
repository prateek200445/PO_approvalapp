import type { QueryClient } from "@tanstack/react-query";
import {
  type ApprovalListKind,
  consumeApprovalListNav,
  getApprovalListNeighbors,
} from "@/lib/approval-list-nav";

export type ApprovalNavActionConfig = {
  kind: ApprovalListKind;
  listQueryKey: string;
  extractId: (row: Record<string, unknown>) => string | undefined;
};

function extractIdsFromCache(
  queryClient: QueryClient,
  listQueryKey: string,
  extractId: (row: Record<string, unknown>) => string | undefined,
  currentId: string,
): string[] {
  const lists = queryClient.getQueriesData<unknown[]>({ queryKey: [listQueryKey] });

  for (const [, data] of lists) {
    if (!Array.isArray(data) || data.length === 0) continue;
    const ids = data
      .map((row) => extractId(row as Record<string, unknown>))
      .filter(Boolean) as string[];
    if (ids.includes(currentId)) return ids;
  }

  for (const [, data] of lists) {
    if (!Array.isArray(data) || data.length === 0) continue;
    return data
      .map((row) => extractId(row as Record<string, unknown>))
      .filter(Boolean) as string[];
  }

  return [];
}

/** Next item after approve/reject: session nav first, then in-memory list cache (no extra fetch). */
export function resolveNextAfterApproval(
  currentId: string,
  queryClient: QueryClient,
  config: ApprovalNavActionConfig,
): string | null {
  const fromSession = consumeApprovalListNav(config.kind, currentId);
  if (fromSession) return fromSession;

  const cachedIds = extractIdsFromCache(
    queryClient,
    config.listQueryKey,
    config.extractId,
    currentId,
  );
  if (cachedIds.length === 0) return null;
  return getApprovalListNeighbors(currentId, cachedIds).next;
}

export function invalidateApprovalCaches(queryClient: QueryClient, listQueryKey: string) {
  void queryClient.invalidateQueries({ queryKey: [listQueryKey] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard-stats"] });
  void queryClient.invalidateQueries({ queryKey: ["pending-list-dashboard"] });
}
