import { useMemo } from "react";
import type { ExportAgingBucket, ExportAgingReport } from "@/lib/export-bill-overdue-api";
import { formatBillAmount, formatPeriod } from "@/lib/export-bill-overdue-api";
import { cn } from "@/lib/utils";
import {
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const BUCKET_TONES = [
  { bar: "bg-success", tone: "bg-success/15 text-success border-success/25", fill: "bg-success" },
  { bar: "bg-primary", tone: "bg-primary/15 text-primary border-primary/25", fill: "bg-primary" },
  { bar: "bg-warning", tone: "bg-warning/15 text-warning border-warning/25", fill: "bg-warning" },
  { bar: "bg-warning", tone: "bg-warning/20 text-warning border-warning/30", fill: "bg-warning/80" },
  { bar: "bg-destructive", tone: "bg-destructive/15 text-destructive border-destructive/25", fill: "bg-destructive" },
];

function bucketHeading(label: string): string {
  return `${label} days`;
}

function moneyOrDash(n: number): string {
  if (!n) return "—";
  return formatBillAmount(n);
}

interface ExportAgingReportViewProps {
  report: ExportAgingReport | undefined;
  loading: boolean;
  showCompany: boolean;
  companyLabel: string;
  search?: string;
}

function matchesSearch(
  row: { companyName: string; customerName: string; billCount: number; total: number; amounts: number[] },
  query: string,
): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  const haystack = [
    row.companyName,
    row.customerName,
    String(row.billCount),
    formatBillAmount(row.total),
    String(Math.round(row.total)),
    ...row.amounts.flatMap((n) => [formatBillAmount(n), String(Math.round(n))]),
  ]
    .join(" ")
    .toLowerCase();
  return haystack.includes(q);
}

export function ExportAgingReportView({
  report,
  loading,
  showCompany,
  companyLabel,
  search = "",
}: ExportAgingReportViewProps) {
  const buckets = report?.buckets ?? [];
  const customers = report?.customers ?? [];
  const query = search.trim();
  const filteredCustomers = useMemo(
    () => customers.filter((row) => matchesSearch(row, query)),
    [customers, query],
  );
  const isFiltered = query.length > 0;
  const visibleCustomers = isFiltered ? filteredCustomers : customers;
  const filteredBuckets: ExportAgingBucket[] = useMemo(() => {
    if (!isFiltered) return buckets;
    return buckets.map((bucket, i) => {
      let pendingAmount = 0;
      let billCount = 0;
      for (const row of filteredCustomers) {
        const amount = row.amounts[i] ?? 0;
        if (!amount) continue;
        pendingAmount += amount;
        billCount += 1;
      }
      return { ...bucket, pendingAmount, billCount };
    });
  }, [buckets, filteredCustomers, isFiltered]);
  const visibleBuckets = isFiltered ? filteredBuckets : buckets;
  const totalPending = isFiltered
    ? visibleCustomers.reduce((sum, row) => sum + row.total, 0)
    : (report?.totalPending ?? 0);
  const totalBills = isFiltered
    ? visibleCustomers.reduce((sum, row) => sum + row.billCount, 0)
    : (report?.totalBills ?? 0);

  if (loading && !report) {
    return (
      <div className="@container flex min-h-0 flex-1 flex-col gap-2">
        <div className="grid shrink-0 grid-cols-2 gap-2 @[32rem]:grid-cols-3 @[56rem]:grid-cols-5">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={`sk-kpi-${i}`}
              className="h-[4.5rem] animate-pulse rounded-xl border border-border bg-muted/60"
            />
          ))}
        </div>
        <div className="min-h-0 flex-1 animate-pulse rounded-2xl border border-border bg-muted/60" />
      </div>
    );
  }

  if (!loading && customers.length === 0) {
    return (
      <div className="flex min-h-0 flex-1 items-center justify-center rounded-2xl border border-dashed border-border bg-card px-4 py-10 text-center text-sm text-muted-foreground">
        No outstanding bills to age for this filter
      </div>
    );
  }

  return (
      <div className="@container flex min-h-0 min-w-0 flex-1 flex-col gap-2 overflow-hidden">
      <section className="shrink-0 overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
        <div className="flex items-center justify-between gap-3 border-b border-border bg-primary/5 px-3 py-1.5">
          <p className="min-w-0 truncate text-[0.85em] text-muted-foreground">
            <span className="font-semibold text-foreground">Aging</span>
            {report
              ? ` · ${companyLabel} · ${report.groupName} · ${formatPeriod(report.dateFrom, report.asOf)} · ${visibleCustomers.length} customer(s)${isFiltered ? ` of ${customers.length}` : ""}`
              : ` · ${companyLabel}`}
          </p>
          <div className="hidden h-1.5 min-w-[8rem] max-w-xs flex-1 overflow-hidden rounded-full bg-muted @[40rem]:flex">
            {visibleBuckets.map((bucket, i) => {
              const pct = totalPending > 0 ? (bucket.pendingAmount / totalPending) * 100 : 0;
              if (pct <= 0) return null;
              return (
                <div
                  key={bucket.key}
                  className={cn("h-full", BUCKET_TONES[i]?.fill ?? "bg-primary")}
                  style={{ width: `${pct}%` }}
                  title={`${bucketHeading(bucket.label)}: ${pct.toFixed(1)}%`}
                />
              );
            })}
          </div>
        </div>
        <div className="grid grid-cols-2 gap-px bg-border @[32rem]:grid-cols-3 @[56rem]:grid-cols-5">
          {visibleBuckets.map((bucket, i) => {
            const tone = BUCKET_TONES[i] ?? BUCKET_TONES[0];
            const share = totalPending > 0 ? (bucket.pendingAmount / totalPending) * 100 : 0;
            return (
              <div key={bucket.key} className="bg-card px-3 py-2">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-[0.8em] font-medium text-muted-foreground">{bucketHeading(bucket.label)}</span>
                  <span className={cn("rounded border px-1 py-px text-[10px] font-medium", tone.tone)}>
                    {bucket.billCount}
                  </span>
                </div>
                <div className="mt-0.5 font-semibold tabular-nums">₹ {formatBillAmount(bucket.pendingAmount)}</div>
                <div className="text-[10px] tabular-nums text-muted-foreground">{share.toFixed(1)}%</div>
              </div>
            );
          })}
        </div>
      </section>

      <section className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
        <header className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b border-border px-3 py-1.5">
          <h2 className="text-sm font-semibold">Aging by customer</h2>
          <p className="text-[11px] text-muted-foreground">
            Amounts in INR
            {report ? ` · ₹ ${formatBillAmount(totalPending)} pending` : ""}
          </p>
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto @[56rem]:hidden">
          {visibleCustomers.length === 0 ? (
            <p className="px-4 py-10 text-center text-sm text-muted-foreground">
              No rows match “{query}”
            </p>
          ) : (
          <div className="divide-y divide-border">
            {visibleCustomers.map((row, i) => (
              <article key={`${row.companyName}-${row.customerName}-${i}`} className="p-3.5">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="truncate text-sm font-semibold">{row.customerName}</h3>
                    {showCompany && row.companyName ? (
                      <p className="mt-0.5 truncate text-[11px] text-muted-foreground">{row.companyName}</p>
                    ) : null}
                  </div>
                  <div className="shrink-0 text-right text-sm font-semibold tabular-nums">
                    ₹ {formatBillAmount(row.total)}
                  </div>
                </div>
                <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
                  {buckets.map((bucket, bi) => (
                    <div key={bucket.key} className="flex justify-between gap-2">
                      <dt className="text-muted-foreground">{bucketHeading(bucket.label)}</dt>
                      <dd className="tabular-nums font-medium">{moneyOrDash(row.amounts[bi] ?? 0)}</dd>
                    </div>
                  ))}
                  <div className="col-span-2 flex justify-between border-t border-border pt-1.5 text-[11px] text-muted-foreground">
                    <dt>{row.billCount} bills</dt>
                    <dd>Total ₹ {formatBillAmount(row.total)}</dd>
                  </div>
                </dl>
              </article>
            ))}
          </div>
          )}
        </div>

        <div className="hidden min-h-0 min-w-0 flex-1 overflow-auto @[56rem]:block">
          <table className="w-full table-auto border-separate border-spacing-0 caption-bottom">
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                {showCompany ? (
                  <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Company</TableHead>
                ) : null}
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Customer</TableHead>
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card text-right">Bills</TableHead>
                {buckets.map((bucket) => (
                  <TableHead key={bucket.key} className="sticky top-0 z-20 whitespace-nowrap border-b border-border bg-card text-right">
                    {bucketHeading(bucket.label)}
                  </TableHead>
                ))}
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card text-right">Total (₹)</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visibleCustomers.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={(showCompany ? 3 : 2) + buckets.length}
                    className="py-10 text-center text-sm text-muted-foreground"
                  >
                    No rows match “{query}”
                  </TableCell>
                </TableRow>
              ) : (
                visibleCustomers.map((row, i) => (
                <TableRow key={`${row.companyName}-${row.customerName}-${i}`} className={i % 2 === 1 ? "bg-muted/30" : undefined}>
                  {showCompany ? (
                    <TableCell className="max-w-[22cqi] truncate">
                      {row.companyName || "—"}
                    </TableCell>
                  ) : null}
                  <TableCell className="max-w-[30cqi] truncate font-medium">
                    {row.customerName}
                  </TableCell>
                  <TableCell className="text-right tabular-nums text-muted-foreground">
                    {row.billCount}
                  </TableCell>
                  {buckets.map((bucket, bi) => (
                    <TableCell
                      key={bucket.key}
                      className="whitespace-nowrap text-right tabular-nums"
                    >
                      {moneyOrDash(row.amounts[bi] ?? 0)}
                    </TableCell>
                  ))}
                  <TableCell className="whitespace-nowrap text-right font-semibold tabular-nums">
                    {formatBillAmount(row.total)}
                  </TableCell>
                </TableRow>
              )))}
              {report && visibleCustomers.length > 0 ? (
                <TableRow className="bg-muted font-semibold hover:bg-muted">
                  <TableCell colSpan={showCompany ? 2 : 1}>Total</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {totalBills}
                  </TableCell>
                  {visibleBuckets.map((bucket) => (
                    <TableCell
                      key={bucket.key}
                      className="whitespace-nowrap text-right tabular-nums"
                    >
                      {formatBillAmount(bucket.pendingAmount)}
                    </TableCell>
                  ))}
                  <TableCell className="whitespace-nowrap text-right tabular-nums">
                    {formatBillAmount(totalPending)}
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </table>
        </div>
      </section>
    </div>
  );
}
