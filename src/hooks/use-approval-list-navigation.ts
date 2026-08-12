import { useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getApiUrl } from "@/lib/api-config";
import { useAuth } from "@/lib/auth-context";
import {
  getApprovalListNav,
  getApprovalListNeighbors,
  type ApprovalListKind,
} from "@/lib/approval-list-nav";

type Options = {
  kind: ApprovalListKind;
  currentId: string;
  listQueryKey: string;
  fallbackApiPath: (username: string) => string;
  extractId: (row: Record<string, unknown>) => string | undefined;
};

export function useApprovalListNavigation({
  kind,
  currentId,
  listQueryKey,
  fallbackApiPath,
  extractId,
}: Options) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const { data: fallbackList } = useQuery({
    queryKey: [listQueryKey, user?.username, "", "gte"],
    queryFn: async () => {
      const response = await fetch(getApiUrl(fallbackApiPath(user!.username)));
      if (!response.ok) throw new Error("Failed to fetch list");
      return response.json();
    },
    enabled: !!user?.username,
    staleTime: 1000 * 60 * 2,
  });

  return useMemo(() => {
    const stored = getApprovalListNav(kind);
    const fromCache = () => {
      const lists = queryClient.getQueriesData({ queryKey: [listQueryKey] });
      for (const [, data] of lists) {
        if (!Array.isArray(data)) continue;
        const ids = data
          .map((row) => extractId(row as Record<string, unknown>))
          .filter(Boolean) as string[];
        if (ids.includes(currentId)) return ids;
      }
      return [] as string[];
    };

    let ids =
      stored.length > 0 && stored.includes(currentId) ? stored : fromCache();

    if (ids.length === 0 && Array.isArray(fallbackList)) {
      ids = fallbackList
        .map((row) => extractId(row as Record<string, unknown>))
        .filter(Boolean) as string[];
    }

    return getApprovalListNeighbors(currentId, ids);
  }, [kind, currentId, fallbackList, listQueryKey, queryClient, extractId]);
}
