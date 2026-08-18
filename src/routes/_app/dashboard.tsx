import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, useState } from "react";
import { ClipboardList, CheckCircle2, XCircle, FileStack, Clock } from "lucide-react";
import { formatINR } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { useAuth } from "@/lib/auth-context";
import { useQuery } from "@tanstack/react-query";
import { SkeletonStats, SkeletonPendingList } from "@/components/SkeletonLoader";
import { setApprovalListNav } from "@/lib/approval-list-nav";

export const Route = createFileRoute("/_app/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — PO Approval Portal" }] }),
  component: Dashboard,
});

function Dashboard() {
  const { user } = useAuth();

  // Ensure scroll to top on component mount
  useEffect(() => {
    window.scrollTo(0, 0);
  }, []);

  // Fetch dashboard stats with React Query
  const { data: statsData, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboard-stats', user?.username],
    queryFn: async () => {
      if (!user?.username) throw new Error('No username');
      const response = await fetch(getApiUrl(`/api/Dashboard/stats/${user.username}`));
      if (!response.ok) throw new Error('Failed to fetch stats');
      const data = await response.json();
      return data;
    },
    staleTime: 0, // Always refetch to ensure fresh counts after approvals
    refetchOnMount: 'always', // Always refetch when dashboard loads
    enabled: !!user?.username && typeof window !== 'undefined', // Only fetch on client
  });

  // Transform stats data - only use data when it's actually available
  const stats = statsData ? {
    pending: statsData?.pending ?? statsData?.Pending ?? 0,
    approved: statsData?.approved ?? statsData?.Approved ?? 0,
    rejected: statsData?.rejected ?? statsData?.Rejected ?? 0,
  } : null;

  // Fetch pending POs with React Query
  const { data: pendingData, isLoading: pendingLoading } = useQuery({
    queryKey: ['pending-list-dashboard', user?.username],
    queryFn: async () => {
      if (!user?.username) throw new Error('No username');
      const response = await fetch(getApiUrl(`/api/PO/pending/${user.username}`));
      if (!response.ok) throw new Error('Failed to fetch pending');
      const data = await response.json();
      return data;
    },
    staleTime: 0, // Always refetch to ensure fresh data
    refetchOnMount: 'always', // Always refetch when dashboard loads
    enabled: !!user?.username && typeof window !== 'undefined', // Only fetch on client
  });

  // Get top 5 pending for display; full list for prev/next navigation
  const pendingAll = Array.isArray(pendingData) ? pendingData : [];
  const pending = pendingAll.slice(0, 5);



 const cards = [
  {
    label: "Pending",
    value: stats?.pending ?? 0,
    icon: Clock,
    tone: "bg-warning/15 text-warning border-warning/25",
    bar: "bg-warning",
  },
  {
    label: "Approved",
    value: stats?.approved ?? 0,
    icon: CheckCircle2,
    tone: "bg-success/15 text-success border-success/25",
    bar: "bg-success",
  },
  {
    label: "Rejected",
    value: stats?.rejected ?? 0,
    icon: XCircle,
    tone: "bg-destructive/15 text-destructive border-destructive/25",
    bar: "bg-destructive",
  },
  {
    label: "Total",
    value: stats ? (stats.pending + stats.approved + stats.rejected) : 0,
    icon: FileStack,
    tone: "bg-primary/15 text-primary border-primary/25",
    bar: "bg-primary",
  },
];

  return (
    <div className="w-full min-w-0 max-w-full overflow-x-hidden space-y-6">
      <div className="card-3d min-w-0 overflow-hidden rounded-2xl p-5">
        <p className="text-sm font-medium text-primary">Welcome back</p>
        <h1 className="truncate text-2xl font-semibold tracking-tight md:text-3xl">{user?.name}</h1>
        <p className="mt-1 truncate text-sm text-muted-foreground">{user?.designation} · {user?.department}</p>
      </div>

      {/* Summary cards */}
      <div className="grid min-w-0 grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
        {(statsLoading || !stats) ? (
          <SkeletonStats />
        ) : (
          cards.map((c) => (
            <div key={c.label} className="card-3d min-w-0 overflow-hidden rounded-2xl p-4">
              <div className={`absolute inset-x-0 top-0 h-1.5 ${c.bar}`} />
              <div className={`mb-3 inline-flex h-10 w-10 items-center justify-center rounded-xl border icon-3d ${c.tone}`}>
                <c.icon className="h-5 w-5" />
              </div>
              <div className="text-2xl font-semibold tabular-nums">{c.value}</div>
              <div className="text-xs font-medium text-muted-foreground">{c.label}</div>
            </div>
          ))
        )}
      </div>

      {/* Quick actions */}
      <div className="grid min-w-0 grid-cols-2 gap-3">
        <Link to="/pending" className="card-3d group flex min-w-0 items-center justify-between gap-2 rounded-2xl p-4">
          <div className="min-w-0">
            <div className="text-xs text-muted-foreground">Action required</div>
            <div className="mt-0.5 truncate font-semibold">Pending POs</div>
          </div>
          <ClipboardList className="h-5 w-5 shrink-0 text-primary transition group-hover:translate-x-0.5" />
        </Link>
       
      </div>

      <div className="grid min-w-0 gap-5">
        {/* Recent pending POs */}
        <section className="shadow-soft min-w-0 overflow-hidden rounded-2xl">
         <header className="border-b border-border px-4 py-3">
  <h2 className="text-sm font-semibold">Recent Activity</h2>
</header> 
          <ul className="divide-y divide-border">
            {pendingLoading ? (
              <li className="px-4 py-3">
                <SkeletonPendingList />
              </li>
            ) : pending.length === 0 ? (
              <li className="px-4 py-3">
                <div className="text-sm text-muted-foreground">No pending items</div>
              </li>
            ) : (
              pending.map((p) => (
                <li key={p.PoNo}>
                  <Link
                    to="/po/$poNo"
                    params={{ poNo: p.PoNo }}
                    onClick={() => setApprovalListNav("po", pendingAll.map((row) => row.PoNo))}
                    className="flex min-w-0 items-center justify-between gap-2 px-4 py-3 hover:bg-secondary/40"
                  >
                    <div className="min-w-0 flex-1">
                      <div className="truncate font-medium text-sm">{p.PoNo}</div>
                      <div className="truncate text-xs text-muted-foreground">{p.FirmName}</div>
                    </div>
                    <div className="shrink-0 text-right">
                      <div className="text-sm font-semibold tabular-nums">{formatINR(p.Total || 0)}</div>
                      <StatusBadge status={p.Status} className="mt-1" />
                    </div>
                  </Link>
                </li>
              ))
            )}
          </ul>
        </section>
      </div>
    </div>
  );
}
