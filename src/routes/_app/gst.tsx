import { Fragment, useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  AlertCircle,
  AlertTriangle,
  ChevronDown,
  ChevronRight,
  Download,
  FileCheck2,
  FileSpreadsheet,
  Hash,
  Inbox,
  Layers,
  ListOrdered,
  Loader2,
  ReceiptText,
  RefreshCw,
} from "lucide-react";
import { toast } from "sonner";
import {
  downloadGstDocumentSummaryExcel,
  getGstDocumentSummary,
  type GstDocumentSeries,
} from "@/lib/gst-api";
import { cn } from "@/lib/utils";
import { Gstr1Return } from "@/components/gst/Gstr1Return";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type GstView = "summary" | "gstr1";

interface GstSearch {
  view?: GstView;
  month?: string;
}

export const Route = createFileRoute("/_app/gst")({
  head: () => ({ meta: [{ title: "GST Compliance — PO Portal" }] }),
  validateSearch: (search: Record<string, unknown>): GstSearch => ({
    view: search.view === "gstr1" ? "gstr1" : search.view === "summary" ? "summary" : undefined,
    month: typeof search.month === "string" && /^\d{4}-\d{2}$/.test(search.month) ? search.month : undefined,
  }),
  component: GstCompliancePage,
});

const VIEWS: { key: GstView; label: string; title: string; description: string; icon: typeof Hash }[] = [
  {
    key: "gstr1",
    label: "GSTR-1 Return",
    title: "GSTR-1 Return",
    description:
      "Outward supplies for the month, section-wise as on the GST portal (B2B, B2CL, B2CS, exports, notes, nil, HSN, documents) with exceptions to review.",
    icon: FileSpreadsheet,
  },
  {
    key: "summary",
    label: "Document Summary",
    title: "Document Summary",
    description:
      "Documents issued during the period (GSTR-1 Table 13) — series-wise from/to numbers, cancelled and net issued.",
    icon: ListOrdered,
  },
];

function GstCompliancePage() {
  const search = Route.useSearch();
  const navigate = Route.useNavigate();
  const view: GstView = search.view ?? "summary";
  const month = search.month ?? previousMonth();
  const active = VIEWS.find((v) => v.key === view) ?? VIEWS[1];

  const update = (next: Partial<GstSearch>) =>
    void navigate({ search: (prev: GstSearch) => ({ ...prev, ...next }), replace: true });

  return (
    <div className="space-y-5">
      <div className="flex items-start gap-3">
        <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <ReceiptText className="h-5 w-5" />
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-primary">GST Compliance</p>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">{active.title}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{active.description}</p>
        </div>
      </div>

      <div className="flex w-full gap-1 rounded-xl border border-border bg-muted/40 p-1 sm:w-fit" role="tablist">
        {VIEWS.map((v) => {
          const Icon = v.icon;
          const selected = v.key === view;
          return (
            <button
              key={v.key}
              type="button"
              role="tab"
              aria-selected={selected}
              onClick={() => update({ view: v.key })}
              className={cn(
                "inline-flex flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-lg px-3.5 py-1.5 text-sm font-medium transition-colors sm:flex-none",
                selected ? "bg-card text-primary shadow-sm" : "text-muted-foreground hover:text-foreground",
              )}
            >
              <Icon className="h-4 w-4" />
              {v.label}
            </button>
          );
        })}
      </div>

      {view === "gstr1" ? (
        <Gstr1Return month={month} onMonthChange={(m) => update({ month: m })} />
      ) : (
        <GstDocumentSummaryPage month={month} onMonthChange={(m) => update({ month: m })} />
      )}
    </div>
  );
}

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

const NATURE_OPTIONS = [
  { value: "", label: "All documents" },
  { value: "Invoices for outward supply", label: "Invoices for outward supply" },
  { value: "Debit Note", label: "Debit Note" },
  { value: "Credit Note", label: "Credit Note" },
  {
    value: "Invoices for inward supply from unregistered person",
    label: "Invoices for inward supply from unregistered person",
  },
];

function previousMonth(): string {
  const d = new Date();
  d.setDate(1);
  d.setMonth(d.getMonth() - 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
}

function monthRange(month: string): { from: string; to: string } {
  const [y, m] = month.split("-").map(Number);
  const last = new Date(y, m, 0).getDate();
  const mm = String(m).padStart(2, "0");
  return { from: `${y}-${mm}-01`, to: `${y}-${mm}-${String(last).padStart(2, "0")}` };
}

function formatMonth(month: string): string {
  const [y, m] = month.split("-").map(Number);
  if (!y || !m) return month;
  return new Date(y, m - 1, 1).toLocaleDateString("en-GB", { month: "long", year: "numeric" });
}

const fmt = new Intl.NumberFormat("en-IN");

function Kpi({
  icon: Icon,
  label,
  value,
  tone = "default",
}: {
  icon: typeof Hash;
  label: string;
  value: number;
  tone?: "default" | "warn" | "good";
}) {
  return (
    <div className="rounded-2xl border border-border bg-card p-3.5 shadow-soft">
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Icon
          className={cn(
            "h-4 w-4",
            tone === "warn" ? "text-amber-600" : tone === "good" ? "text-emerald-600" : "text-primary",
          )}
        />
        {label}
      </div>
      <p
        className={cn(
          "mt-1 text-2xl font-semibold tabular-nums",
          tone === "warn" && value > 0 && "text-amber-700",
        )}
      >
        {fmt.format(value)}
      </p>
    </div>
  );
}

function MissingList({ row }: { row: GstDocumentSeries }) {
  if (row.cancelled === 0) {
    return <p className="text-xs text-muted-foreground">No gaps — every number in the range was issued.</p>;
  }
  return (
    <div className="space-y-1.5">
      <p className="text-xs font-medium text-amber-800">
        Missing serial numbers ({fmt.format(row.cancelled)}) — not found anywhere in the ERP, so treated as
        cancelled:
      </p>
      <div className="flex flex-wrap gap-1">
        {row.missingNumbers.map((n) => (
          <span
            key={n}
            className="rounded-md border border-amber-200 bg-amber-50 px-1.5 py-0.5 font-mono text-[11px] text-amber-900"
          >
            {n}
          </span>
        ))}
        {row.missingTruncated ? <span className="text-xs text-muted-foreground">…and more</span> : null}
      </div>
    </div>
  );
}

function GstDocumentSummaryPage({
  month,
  onMonthChange,
}: {
  month: string;
  onMonthChange: (month: string) => void;
}) {
  const [company, setCompany] = useState("");
  const [nature, setNature] = useState("");
  const [onlyGaps, setOnlyGaps] = useState(false);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [refreshToken, setRefreshToken] = useState(0);
  const [exporting, setExporting] = useState(false);
  const bypassCacheRef = useRef(false);

  const { from, to } = monthRange(month || previousMonth());

  const query = useQuery({
    queryKey: ["gst-document-summary", from, to, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getGstDocumentSummary(from, to, refresh);
    },
    staleTime: 15 * 60_000,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const report = query.data;
  const rows = useMemo(() => {
    return (report?.rows ?? []).filter(
      (r) =>
        (!company || r.company === company) &&
        (!nature || r.nature === nature) &&
        (!onlyGaps || r.cancelled > 0),
    );
  }, [report?.rows, company, nature, onlyGaps]);

  const grouped = useMemo(() => {
    const map = new Map<string, GstDocumentSeries[]>();
    for (const r of rows) {
      const list = map.get(r.company);
      if (list) list.push(r);
      else map.set(r.company, [r]);
    }
    return Array.from(map.entries());
  }, [rows]);

  const totals = useMemo(
    () => ({
      total: rows.reduce((s, r) => s + r.totalNumber, 0),
      cancelled: rows.reduce((s, r) => s + r.cancelled, 0),
      net: rows.reduce((s, r) => s + r.netIssued, 0),
      series: rows.length,
    }),
    [rows],
  );

  const rowKey = (r: GstDocumentSeries) => `${r.company}|${r.nature}|${r.series}`;
  function toggle(key: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  async function exportExcel() {
    if (exporting) return;
    setExporting(true);
    try {
      await downloadGstDocumentSummaryExcel(from, to, company || undefined);
      toast.success("Excel downloaded");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Download failed");
    } finally {
      setExporting(false);
    }
  }

  return (
    <div className="space-y-5">
      <div className="rounded-2xl border border-border bg-card p-3 shadow-soft sm:p-3.5">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[10rem_minmax(0,1.3fr)_minmax(0,1fr)_auto_auto] lg:items-end">
          <div className="space-y-1">
            <Label htmlFor="gst-month" className="text-xs text-muted-foreground">
              Return period
            </Label>
            <Input
              id="gst-month"
              type="month"
              value={month}
              onChange={(e) => {
                onMonthChange(e.target.value || previousMonth());
                setExpanded(new Set());
              }}
              className="h-9 bg-background"
            />
          </div>
          <div className="min-w-0 space-y-1">
            <Label htmlFor="gst-company" className="text-xs text-muted-foreground">
              Company
            </Label>
            <select
              id="gst-company"
              value={company}
              onChange={(e) => setCompany(e.target.value)}
              className={SELECT_CLASS}
            >
              <option value="">All companies</option>
              {(report?.companies ?? []).map((name) => (
                <option key={name} value={name}>
                  {name}
                </option>
              ))}
            </select>
          </div>
          <div className="min-w-0 space-y-1">
            <Label htmlFor="gst-nature" className="text-xs text-muted-foreground">
              Nature of document
            </Label>
            <select
              id="gst-nature"
              value={nature}
              onChange={(e) => setNature(e.target.value)}
              className={SELECT_CLASS}
            >
              {NATURE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <label className="flex h-9 cursor-pointer items-center gap-2 rounded-md border border-border bg-background px-3 text-sm">
            <input
              type="checkbox"
              checked={onlyGaps}
              onChange={(e) => setOnlyGaps(e.target.checked)}
              className="h-4 w-4 accent-amber-600"
            />
            Only series with gaps
          </label>
          <div className="flex flex-wrap items-end gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9 gap-1.5"
              disabled={query.isFetching}
              onClick={() => {
                bypassCacheRef.current = true;
                setRefreshToken((n) => n + 1);
              }}
            >
              <RefreshCw className={cn("h-4 w-4", query.isFetching && "animate-spin")} />
              Refresh
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9 gap-1.5"
              disabled={!report || exporting || query.isFetching}
              onClick={() => void exportExcel()}
            >
              {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
              Excel
            </Button>
          </div>
        </div>
      </div>

      {query.isError ? (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-2xl border border-destructive/30 bg-destructive/5 p-4 text-sm"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
          <div>
            <p className="font-semibold text-destructive">Could not load document summary</p>
            <p className="text-muted-foreground">
              {query.error instanceof Error ? query.error.message : "Failed to load document summary."}
            </p>
          </div>
        </div>
      ) : null}

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi icon={Hash} label="Total numbers" value={totals.total} />
        <Kpi icon={AlertTriangle} label="Cancelled / missing" value={totals.cancelled} tone="warn" />
        <Kpi icon={FileCheck2} label="Net issued" value={totals.net} tone="good" />
        <Kpi icon={Layers} label="Series" value={totals.series} />
      </div>

      {query.isFetching && !report ? (
        <div className="flex items-center gap-2 rounded-2xl border border-border bg-card p-6 text-sm text-muted-foreground shadow-soft">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          Loading documents for {formatMonth(month)}…
        </div>
      ) : null}

      {report && rows.length === 0 && !query.isFetching ? (
        <div className="flex flex-col items-center rounded-2xl border border-dashed border-border bg-card px-5 py-12 text-center shadow-soft">
          <Inbox className="mb-2 h-6 w-6 text-primary" />
          <p className="text-sm font-semibold">No documents found</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Nothing matches the selected period and filters.
          </p>
        </div>
      ) : null}

      {grouped.map(([companyName, list]) => {
        const sub = {
          total: list.reduce((s, r) => s + r.totalNumber, 0),
          cancelled: list.reduce((s, r) => s + r.cancelled, 0),
          net: list.reduce((s, r) => s + r.netIssued, 0),
        };
        return (
          <section
            key={companyName}
            className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft"
          >
            <div className="flex flex-col gap-1 bg-primary px-4 py-2.5 text-primary-foreground sm:flex-row sm:items-baseline sm:justify-between">
              <h2 className="text-sm font-semibold">{companyName}</h2>
              <p className="text-[11px] text-primary-foreground/80">
                {formatMonth(month)} · {fmt.format(sub.net)} issued
                {sub.cancelled > 0 ? ` · ${fmt.format(sub.cancelled)} cancelled` : ""}
              </p>
            </div>

            <div className="hidden overflow-x-auto md:block">
              <table className="w-full border-collapse text-sm">
                <thead>
                  <tr className="bg-primary/10 text-left text-xs text-primary">
                    <th className="w-8 px-2 py-2" />
                    <th className="px-3 py-2 font-semibold">Nature of document</th>
                    <th className="px-3 py-2 font-semibold">Series</th>
                    <th className="px-3 py-2 font-semibold">Sr. No. From</th>
                    <th className="px-3 py-2 font-semibold">Sr. No. To</th>
                    <th className="px-3 py-2 text-right font-semibold">Total</th>
                    <th className="px-3 py-2 text-right font-semibold">Cancelled</th>
                    <th className="px-3 py-2 text-right font-semibold">Net Issued</th>
                  </tr>
                </thead>
                <tbody>
                  {list.map((r, i) => {
                    const key = rowKey(r);
                    const open = expanded.has(key);
                    return (
                      <Fragment key={key}>
                        <tr
                          onClick={() => toggle(key)}
                          className={cn(
                            "cursor-pointer border-b border-border/70 transition-colors hover:bg-primary/[0.06]",
                            r.cancelled > 0 ? "bg-amber-50/60" : i % 2 === 0 ? "bg-card" : "bg-muted/30",
                          )}
                        >
                          <td className="px-2 py-1.5 text-muted-foreground">
                            {open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                          </td>
                          <td className="px-3 py-1.5">
                            {r.nature}
                            {r.voucherTypes.length > 0 ? (
                              <span className="block text-[11px] text-muted-foreground">
                                {r.voucherTypes.join(", ")}
                              </span>
                            ) : null}
                          </td>
                          <td className="px-3 py-1.5 font-mono text-xs">{r.series}</td>
                          <td className="px-3 py-1.5 font-mono text-xs">{r.fromNo}</td>
                          <td className="px-3 py-1.5 font-mono text-xs">{r.toNo}</td>
                          <td className="px-3 py-1.5 text-right tabular-nums">{fmt.format(r.totalNumber)}</td>
                          <td
                            className={cn(
                              "px-3 py-1.5 text-right tabular-nums",
                              r.cancelled > 0 ? "font-semibold text-amber-700" : "text-muted-foreground",
                            )}
                          >
                            {fmt.format(r.cancelled)}
                          </td>
                          <td className="px-3 py-1.5 text-right font-medium tabular-nums">
                            {fmt.format(r.netIssued)}
                          </td>
                        </tr>
                        {open ? (
                          <tr className="border-b border-border/70 bg-muted/20">
                            <td />
                            <td colSpan={7} className="px-3 py-2.5">
                              <MissingList row={r} />
                            </td>
                          </tr>
                        ) : null}
                      </Fragment>
                    );
                  })}
                  <tr className="bg-primary/5 font-semibold">
                    <td />
                    <td colSpan={4} className="px-3 py-2">
                      Total
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt.format(sub.total)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-amber-700">
                      {fmt.format(sub.cancelled)}
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt.format(sub.net)}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-border md:hidden">
              {list.map((r) => {
                const key = rowKey(r);
                const open = expanded.has(key);
                return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => toggle(key)}
                    className={cn("block w-full px-4 py-3 text-left", r.cancelled > 0 && "bg-amber-50/60")}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="text-xs text-muted-foreground">{r.nature}</p>
                        <p className="truncate font-mono text-sm">{r.series}</p>
                      </div>
                      {open ? (
                        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                      ) : (
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                      )}
                    </div>
                    <p className="mt-1 font-mono text-xs text-muted-foreground">
                      {r.fromNo} → {r.toNo}
                    </p>
                    <div className="mt-2 grid grid-cols-3 gap-2 text-center text-xs">
                      <div className="rounded-md bg-muted/50 py-1">
                        <p className="text-muted-foreground">Total</p>
                        <p className="font-semibold tabular-nums">{fmt.format(r.totalNumber)}</p>
                      </div>
                      <div className={cn("rounded-md py-1", r.cancelled > 0 ? "bg-amber-100" : "bg-muted/50")}>
                        <p className="text-muted-foreground">Cancelled</p>
                        <p className="font-semibold tabular-nums">{fmt.format(r.cancelled)}</p>
                      </div>
                      <div className="rounded-md bg-muted/50 py-1">
                        <p className="text-muted-foreground">Net</p>
                        <p className="font-semibold tabular-nums">{fmt.format(r.netIssued)}</p>
                      </div>
                    </div>
                    {open ? (
                      <div className="mt-2">
                        <MissingList row={r} />
                      </div>
                    ) : null}
                  </button>
                );
              })}
            </div>
          </section>
        );
      })}
    </div>
  );
}
