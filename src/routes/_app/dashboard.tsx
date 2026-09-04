import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, type ComponentType } from "react";
import {
  ClipboardList,
  CreditCard,
  FileText,
  Hammer,
  Wallet,
  CheckCircle2,
  XCircle,
  ArrowRight,
} from "lucide-react";
import { formatINR } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { useAuth } from "@/lib/auth-context";
import { useQuery } from "@tanstack/react-query";
import { SkeletonPendingList } from "@/components/SkeletonLoader";
import { setApprovalListNav, type ApprovalListKind } from "@/lib/approval-list-nav";
import { formatShortDate } from "@/lib/utils";
import { useApprovalInbox, type InboxItem, type InboxKind } from "@/hooks/useApprovalInbox";
import type { POStatus } from "@/lib/mock-data";
import { BILL_PAYMENT_ENTRY_ENABLED } from "@/lib/feature-flags";

export const Route = createFileRoute("/_app/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — PO Approval Portal" }] }),
  component: Dashboard,
});

const KIND_META: Record<
  InboxKind,
  { label: string; shortLabel: string; listLabel: string; tone: string; bar: string; icon: ComponentType<{ className?: string }> }
> = {
  po: {
    label: "Purchase Orders",
    shortLabel: "POs",
    listLabel: "PO",
    tone: "bg-warning/15 text-warning border-warning/25",
    bar: "bg-warning",
    icon: ClipboardList,
  },
  workorder: {
    label: "Work Orders",
    shortLabel: "WOs",
    listLabel: "WO",
    tone: "bg-primary/15 text-primary border-primary/25",
    bar: "bg-primary",
    icon: Hammer,
  },
  payment: {
    label: "Payments",
    shortLabel: "Pay",
    listLabel: "Pay",
    tone: "bg-success/15 text-success border-success/25",
    bar: "bg-success",
    icon: CreditCard,
  },
  advancePayment: {
    label: "Bill Payment Entry",
    shortLabel: "Bill Pay",
    listLabel: "Bill Pay",
    tone: "bg-cyan-500/15 text-cyan-600 border-cyan-500/25",
    bar: "bg-cyan-500",
    icon: Wallet,
  },
  indent: {
    label: "Indents",
    shortLabel: "Indent",
    listLabel: "Indent",
    tone: "bg-accent text-accent-foreground border-border",
    bar: "bg-muted-foreground/50",
    icon: FileText,
  },
};

function inboxStatus(status: string): POStatus {
  if (status.toLowerCase().startsWith("approved")) return "Approved";
  if (status.toLowerCase().startsWith("rejected")) return "Rejected";
  return "Pending";
}

function InboxItemLink({ item, ids }: { item: InboxItem; ids: string[] }) {
  const meta = KIND_META[item.kind];
  const kind: ApprovalListKind = item.kind;
  const className =
    "flex min-w-0 flex-col gap-2 px-3 py-3 active:bg-secondary/50 sm:flex-row sm:items-center sm:justify-between sm:px-4";

  const body = (
    <>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="rounded-md bg-secondary px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
            {meta.listLabel}
          </span>
          <div className="truncate font-medium text-sm">{item.title}</div>
        </div>
        <div className="mt-1 truncate text-xs text-muted-foreground">{item.subtitle}</div>
      </div>
      <div className="flex items-center justify-between gap-2 sm:shrink-0 sm:flex-col sm:items-end sm:text-right">
        <div className="text-sm font-semibold tabular-nums">
          {item.amount != null ? formatINR(item.amount) : item.extra || "—"}
        </div>
        <div className="flex items-center gap-2">
          {item.days != null && (
            <span className="text-[11px] text-muted-foreground">{item.days}d waiting</span>
          )}
          <span className="hidden text-[11px] text-muted-foreground sm:inline">
            {formatShortDate(item.date)}
          </span>
          <StatusBadge status={inboxStatus(item.status)} />
        </div>
      </div>
    </>
  );

  const onClick = () => setApprovalListNav(kind, ids);

  if (item.kind === "po") {
    return (
      <Link to="/po/$poNo" params={{ poNo: item.id }} onClick={onClick} className={className}>
        {body}
      </Link>
    );
  }
  if (item.kind === "workorder") {
    return (
      <Link to="/workorder/$poNo" params={{ poNo: item.id }} onClick={onClick} className={className}>
        {body}
      </Link>
    );
  }
  if (item.kind === "payment") {
    return (
      <Link
        to="/payment/$paymentNo"
        params={{ paymentNo: item.id }}
        onClick={onClick}
        className={className}
      >
        {body}
      </Link>
    );
  }
  if (item.kind === "advancePayment") {
    return (
      <Link
        to="/advance-payment/$paymentNo"
        params={{ paymentNo: item.id }}
        onClick={onClick}
        className={className}
      >
        {body}
      </Link>
    );
  }
  return (
    <Link
      to="/indent/$indentNo"
      params={{ indentNo: item.id }}
      onClick={onClick}
      className={className}
    >
      {body}
    </Link>
  );
}

function Dashboard() {
  const { user } = useAuth();
  const inbox = useApprovalInbox(user?.username);

  useEffect(() => {
    window.scrollTo(0, 0);
  }, []);

  const { data: statsData } = useQuery({
    queryKey: ["dashboard-stats", user?.username],
    queryFn: async () => {
      if (!user?.username) throw new Error("No username");
      const response = await fetch(getApiUrl(`/api/Dashboard/stats/${user.username}`));
      if (!response.ok) throw new Error("Failed to fetch stats");
      return response.json();
    },
    staleTime: 30_000,
    refetchOnMount: "always",
    enabled: !!user?.username && typeof window !== "undefined",
  });

  const poApproved = statsData?.approved ?? statsData?.Approved ?? null;
  const poRejected = statsData?.rejected ?? statsData?.Rejected ?? null;

  const queues: {
    kind: InboxKind;
    to: "/pending" | "/workorders" | "/payments" | "/advance-payments" | "/indents";
  }[] = [
    { kind: "po", to: "/pending" },
    { kind: "workorder", to: "/workorders" },
    { kind: "payment", to: "/payments" },
    ...(BILL_PAYMENT_ENTRY_ENABLED ? [{ kind: "advancePayment" as const, to: "/advance-payments" as const }] : []),
    { kind: "indent", to: "/indents" },
  ];

  const idsByKind: Record<InboxKind, string[]> = {
    po: inbox.poItems.map((row) => row.id),
    workorder: inbox.workorderItems.map((row) => row.id),
    payment: inbox.paymentItems.map((row) => row.id),
    advancePayment: inbox.advancePaymentItems.map((row) => row.id),
    indent: inbox.indentItems.map((row) => row.id),
  };

  return (
    <div className="w-full min-w-0 max-w-full overflow-x-hidden space-y-4 md:space-y-6">
      <div className="card-3d min-w-0 overflow-hidden rounded-2xl p-4 md:p-5">
        <p className="text-sm font-medium text-primary">Needs your action</p>
        <h1 className="truncate text-xl font-semibold tracking-tight md:text-3xl">{user?.name}</h1>
        <p className="mt-1 truncate text-sm text-muted-foreground">
          {user?.designation} · {user?.department}
        </p>
        <p className="mt-3 text-sm text-muted-foreground">
          {inbox.isLoading ? (
            "Checking approval queues…"
          ) : inbox.counts.total === 0 ? (
            "You are all caught up. No pending approvals."
          ) : (
            <>
              <span className="font-semibold tabular-nums text-foreground">{inbox.counts.total}</span>
              {" pending across POs, work orders, payments, and indents."}
            </>
          )}
        </p>
        {(poApproved != null || poRejected != null) && (
          <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <CheckCircle2 className="h-3.5 w-3.5 text-success" />
              {poApproved ?? 0} POs approved
            </span>
            <span className="inline-flex items-center gap-1">
              <XCircle className="h-3.5 w-3.5 text-destructive" />
              {poRejected ?? 0} POs rejected
            </span>
          </p>
        )}
      </div>

      {!inbox.isLoading && inbox.counts.total > 0 && (
        <div className="grid min-w-0 grid-cols-2 gap-3 md:grid-cols-3">
          <div className="card-3d rounded-2xl p-4">
            <div className="text-xs text-muted-foreground">Value waiting</div>
            <div className="mt-1 text-lg font-semibold tabular-nums">
              {formatINR(inbox.workQueue.totalValue)}
            </div>
          </div>
          <div className="card-3d rounded-2xl p-4">
            <div className="text-xs text-muted-foreground">Oldest item</div>
            <div className="mt-1 text-lg font-semibold tabular-nums">
              {inbox.workQueue.oldestDays == null ? "—" : `${inbox.workQueue.oldestDays} days`}
            </div>
          </div>
          <div className="card-3d col-span-2 rounded-2xl p-4 md:col-span-1">
            <div className="text-xs text-muted-foreground">Highest value</div>
            <div className="mt-1 truncate text-sm font-semibold">
              {inbox.workQueue.highValue[0]
                ? `${inbox.workQueue.highValue[0].title} · ${formatINR(inbox.workQueue.highValue[0].amount ?? 0)}`
                : "—"}
            </div>
          </div>
        </div>
      )}

      <div className="grid min-w-0 grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
        {queues.map(({ kind, to }) => {
          const meta = KIND_META[kind];
          const count = inbox.counts[kind];
          const state = inbox.queues[kind];
          const Icon = meta.icon;
          return (
            <Link key={kind} to={to} className="card-3d min-w-0 overflow-hidden rounded-2xl p-3 md:p-4">
              <div className={`absolute inset-x-0 top-0 h-1.5 ${meta.bar}`} />
              <div
                className={`mb-3 inline-flex h-10 w-10 items-center justify-center rounded-xl border icon-3d ${meta.tone}`}
              >
                <Icon className="h-5 w-5" />
              </div>
              <div className="text-xl font-semibold tabular-nums md:text-2xl">
                {state.loading ? "—" : state.error ? "!" : count}
              </div>
              <div className="text-xs font-medium text-muted-foreground">
                <span className="sm:hidden">{meta.shortLabel}</span>
                <span className="hidden sm:inline">{meta.label}</span>
              </div>
              <div className="mt-2 flex items-center gap-1 text-[11px] font-medium text-primary">
                {state.error ? "Couldn’t load" : count > 0 ? "Review now" : "All clear"}
                <ArrowRight className="h-3 w-3" />
              </div>
            </Link>
          );
        })}
      </div>

      <section className="shadow-soft min-w-0 overflow-hidden rounded-2xl">
        <header className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Waiting longest</h2>
          {!inbox.isLoading && inbox.counts.total > 0 && (
            <span className="text-xs text-muted-foreground">{inbox.recent.length} latest</span>
          )}
        </header>
        <ul className="divide-y divide-border">
          {inbox.isLoading ? (
            <li className="px-4 py-3">
              <SkeletonPendingList />
            </li>
          ) : inbox.recent.length === 0 ? (
            <li className="px-4 py-6 text-sm text-muted-foreground">No pending items across queues.</li>
          ) : (
            inbox.recent.map((item) => (
              <li key={`${item.kind}-${item.id}`}>
                <InboxItemLink item={item} ids={idsByKind[item.kind]} />
              </li>
            ))
          )}
        </ul>
      </section>
    </div>
  );
}
