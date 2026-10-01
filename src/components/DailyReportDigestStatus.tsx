import { Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { CheckCheck, CheckCircle2, Clock, MessageCircle, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  formatReportDateTime,
  getDailyReportDigestStatus,
  type DailyReportDigestState,
  type DailyReportDigestStatus,
} from "@/lib/daily-report-api";

export function isoDate(d: Date) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function to12h(hhmm?: string) {
  const [h, m] = (hhmm ?? "19:00").split(":").map(Number);
  if (!Number.isFinite(h) || !Number.isFinite(m)) return "7:00 PM";
  return `${h % 12 || 12}:${String(m).padStart(2, "0")} ${h < 12 ? "AM" : "PM"}`;
}

const STATE_UI: Record<
  DailyReportDigestState,
  { label: string; tone: string; icon: typeof CheckCircle2 }
> = {
  read: {
    label: "Sent · Read",
    tone: "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    icon: CheckCheck,
  },
  delivered: {
    label: "Sent · Received",
    tone: "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    icon: CheckCheck,
  },
  sent: {
    label: "Sent",
    tone: "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    icon: CheckCircle2,
  },
  failed: {
    label: "Not received",
    tone: "border-destructive/30 bg-destructive/10 text-destructive",
    icon: XCircle,
  },
  not_sent: {
    label: "Not sent",
    tone: "border-destructive/30 bg-destructive/10 text-destructive",
    icon: XCircle,
  },
  scheduled: {
    label: "Scheduled",
    tone: "border-border bg-secondary/60 text-muted-foreground",
    icon: Clock,
  },
};

export function digestState(status?: DailyReportDigestStatus): DailyReportDigestState {
  if (status?.state) return status.state;
  return status?.sentAt ? "sent" : "not_sent";
}

export function DigestStatusPill({ status, className }: { status?: DailyReportDigestStatus; className?: string }) {
  const state = digestState(status);
  const ui = STATE_UI[state];
  const Icon = ui.icon;
  const label = state === "scheduled" ? `Scheduled ${to12h(status?.sendTime)}` : ui.label;
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-semibold",
        ui.tone,
        className,
      )}
    >
      <Icon className="h-3.5 w-3.5" />
      {label}
    </span>
  );
}

/** One-line explanation under the pill: when it was sent, or why it was not. */
export function digestStatusDetail(status?: DailyReportDigestStatus): string | null {
  if (!status) return null;
  const state = digestState(status);
  const count = status.reportCount != null ? ` · ${status.reportCount} report(s)` : "";
  switch (state) {
    case "read":
    case "delivered":
      return `Sent ${formatReportDateTime(status.sentAt ?? "")} to ${status.sentTo ?? "—"}${count}`;
    case "sent":
      return `Sent ${formatReportDateTime(status.sentAt ?? "")} to ${status.sentTo ?? "—"}${count} · waiting for WhatsApp delivery confirmation`;
    case "failed":
      return `WhatsApp could not deliver it${status.deliveryError ? `: ${status.deliveryError}` : ""}`;
    case "not_sent":
      return status.lastError ? `Not sent: ${status.lastError}` : "The PDF was not sent for this day.";
    case "scheduled":
      return `Will be sent automatically at ${to12h(status.sendTime)} to ${status.recipients?.join(", ") || "—"}`;
  }
}

function DigestRow({ label, date }: { label: string; date: string }) {
  const query = useQuery({
    queryKey: ["daily-report-digest-status", date],
    queryFn: () => getDailyReportDigestStatus(date),
    staleTime: 60_000,
    refetchInterval: 120_000,
  });
  const detail = digestStatusDetail(query.data);
  return (
    <div className="flex min-w-0 flex-wrap items-center gap-x-3 gap-y-1">
      <span className="w-20 shrink-0 text-xs font-medium text-muted-foreground">{label}</span>
      {query.isLoading ? (
        <span className="text-xs text-muted-foreground">Checking…</span>
      ) : query.isError ? (
        <span className="text-xs text-muted-foreground">Status unavailable</span>
      ) : (
        <>
          <DigestStatusPill status={query.data} />
          {detail ? <span className="min-w-0 truncate text-xs text-muted-foreground">{detail}</span> : null}
        </>
      )}
    </div>
  );
}

/** Dashboard strip for the digest recipient/admin: today's and yesterday's WhatsApp PDF status. */
export function DailyReportDigestDashboardStrip() {
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  return (
    <section className="card-3d min-w-0 rounded-2xl p-4" aria-label="WhatsApp daily reports status">
      <div className="mb-2 flex items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-sm font-semibold">
          <MessageCircle className="h-4 w-4 text-primary" /> Daily reports on WhatsApp
        </h2>
        <Link to="/daily-reports" className="text-xs font-medium text-primary hover:underline">
          Open
        </Link>
      </div>
      <div className="space-y-1.5">
        <DigestRow label="Today" date={isoDate(today)} />
        <DigestRow label="Yesterday" date={isoDate(yesterday)} />
      </div>
    </section>
  );
}
