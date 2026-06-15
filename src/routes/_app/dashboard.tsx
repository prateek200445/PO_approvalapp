import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, useState } from "react";
import { ClipboardList, CheckCircle2, XCircle, FileStack, ArrowRight, Clock } from "lucide-react";
import { formatINR } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { useAuth } from "@/lib/auth-context";

export const Route = createFileRoute("/_app/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — PO Approval Portal" }] }),
  component: Dashboard,
});

function Dashboard() {
  const { user } = useAuth();

  const [stats, setStats] = useState({
  pending: 0,
  approved: 0,
  rejected: 0,
});

  const [pending, setPending] = useState<any[]>([]);
  const [recent, setRecent] = useState<any[]>([]);

  useEffect(() => {
    if (!user?.username) return;
    fetch(getApiUrl(`/api/Dashboard/stats/${user.username}`))
      .then((response) => response.json())
      .then((data) => {
        setStats({
          pending: data.pending ?? data.Pending ?? 0,
          approved: data.approved ?? data.Approved ?? 0,
          rejected: data.rejected ?? data.Rejected ?? 0,
        });
      })
      .catch((error) => {
        console.error(error);
      });

    fetch(getApiUrl(`/api/PO/pending/${user.username}`))
      .then((response) => response.json())
      .then((data) => {
        setPending(data.slice(0, 5));
      })
      .catch((error) => {
        console.error(error);
      });

    setRecent([]);
  }, [user]);



 const cards = [
  {
    label: "Pending",
    value: stats.pending,
    icon: Clock,
    tone: "bg-warning/10 text-warning border-warning/20",
  },
  {
    label: "Approved",
    value: stats.approved,
    icon: CheckCircle2,
    tone: "bg-success/10 text-success border-success/20",
  },
  {
    label: "Rejected",
    value: stats.rejected,
    icon: XCircle,
    tone: "bg-destructive/10 text-destructive border-destructive/20",
  },
  {
    label: "Total",
    value: stats.pending + stats.approved + stats.rejected,
    icon: FileStack,
    tone: "bg-primary/10 text-primary border-primary/20",
  },
];

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm text-muted-foreground">Welcome back,</p>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">{user?.name}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{user?.designation} · {user?.department}</p>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
        {cards.map((c) => (
          <div key={c.label} className="rounded-xl border border-border bg-card p-4 shadow-sm">
            <div className={`mb-3 inline-flex h-9 w-9 items-center justify-center rounded-lg border ${c.tone}`}>
              <c.icon className="h-4.5 w-4.5" />
            </div>
            <div className="text-2xl font-semibold tabular-nums">{c.value}</div>
            <div className="text-xs text-muted-foreground">{c.label}</div>
          </div>
        ))}
      </div>

      {/* Quick actions */}
      <div className="grid grid-cols-2 gap-3">
        <Link to="/pending" className="group flex items-center justify-between rounded-xl border border-border bg-card p-4 transition hover:border-primary/40 hover:bg-secondary/40">
          <div>
            <div className="text-xs text-muted-foreground">Action required</div>
            <div className="mt-0.5 font-semibold">Pending POs</div>
          </div>
          <ClipboardList className="h-5 w-5 text-primary transition group-hover:translate-x-0.5" />
        </Link>
       
      </div>

      <div className="grid gap-5 lg:grid-cols-2">
        {/* Recent pending POs */}
        <section className="rounded-xl border border-border bg-card">
         <header className="border-b border-border px-4 py-3">
  <h2 className="text-sm font-semibold">Recent Activity</h2>
</header> 
          <ul className="divide-y divide-border">
            {pending.map((p) => (
              <li key={p.PoNo}>
                <Link to="/po/$poNo" params={{ poNo: p.PoNo }} className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-secondary/40">
                  <div className="min-w-0">
                    <div className="truncate font-medium text-sm">{p.PoNo}</div>
                    <div className="truncate text-xs text-muted-foreground">{p.FirmName}</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm font-semibold tabular-nums">{formatINR(p.Total || 0)}</div>
                    <StatusBadge status={p.Status} className="mt-1" />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        </section>

        {/* Recent activity */}
        <section className="rounded-xl border border-border bg-card">
          <header className="flex items-center justify-between border-b border-border px-4 py-3">
            <h2 className="text-sm font-semibold">Recent Activity</h2>
           
          </header>
          <ul className="divide-y divide-border">
            {recent.length === 0 ? (
  <div className="p-4 text-sm text-muted-foreground">
    No recent activity
  </div>
) : (
  recent.map((h) => (
              <li key={h.id} className="flex items-start gap-3 px-4 py-3">
                <div className={`mt-0.5 h-2 w-2 flex-shrink-0 rounded-full ${h.status === "Approved" ? "bg-success" : h.status === "Rejected" ? "bg-destructive" : "bg-warning"}`} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <div className="truncate text-sm font-medium">{h.user} <span className="text-muted-foreground">· {h.role}</span></div>
                    <div className="text-xs text-muted-foreground">{h.date}</div>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {h.action} <span className="font-medium text-foreground">{h.poNumber}</span>
                  </div>
                </div>
              </li>
            ))
)}
          </ul>
        </section>
      </div>
    </div>
  );
}
