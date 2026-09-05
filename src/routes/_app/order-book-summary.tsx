import { useMemo, useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { BookMarked, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { useAuth } from "@/lib/auth-context";
import { canAccessOrderBookSummary } from "@/lib/feature-flags";
import {
  formatInr,
  formatMt,
  getOrderBookSummary,
  type OrderBookUnitBlock,
} from "@/lib/order-book-summary-api";

export const Route = createFileRoute("/_app/order-book-summary")({
  head: () => ({ meta: [{ title: "Order Book Summary — PO Portal" }] }),
  component: OrderBookSummaryPage,
});

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function OrderBookSummaryPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const allowed = canAccessOrderBookSummary(user?.username);
  const [asOf, setAsOf] = useState(todayIso);
  const [refreshToken, setRefreshToken] = useState(0);

  const summaryQuery = useQuery({
    queryKey: ["order-book-summary", user?.username, asOf, refreshToken],
    queryFn: () => getOrderBookSummary(user!.username, asOf, refreshToken > 0),
    enabled: allowed && !!user?.username,
    staleTime: 15 * 60_000,
    placeholderData: keepPreviousData,
    retry: 1,
  });

  const data = summaryQuery.data;
  const units = data?.units ?? [];

  const kpi = useMemo(
    () => [
      { label: "Issued orders", value: `${formatMt(data?.issuedMt ?? 0)} MT` },
      { label: "In hand / hold", value: `${formatMt(data?.inHandHoldMt ?? 0)} MT` },
      { label: "Final pending", value: `${formatMt(data?.finalPendMt ?? 0)} MT` },
      { label: "Order book days", value: String(data?.ordBookDaysAvg ?? 0) },
    ],
    [data],
  );

  if (!allowed) {
    return (
      <div className="mx-auto max-w-lg space-y-3 rounded-xl border border-border bg-card p-6 text-center">
        <BookMarked className="mx-auto h-8 w-8 text-muted-foreground" />
        <h1 className="text-xl font-semibold">Restricted report</h1>
        <p className="text-sm text-muted-foreground">
          Order Book Summary is limited to an allowlisted set of users. Ask an admin to add your
          username to <code className="text-xs">OrderBookSummary:AllowedUsers</code>.
        </p>
        <Button variant="outline" onClick={() => navigate({ to: "/dashboard" })}>
          Back to dashboard
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex items-center gap-2 text-primary">
            <BookMarked className="h-5 w-5" />
            <p className="text-xs font-semibold uppercase tracking-wide">Order Book</p>
          </div>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">
            Order Book Summary
          </h1>
          <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
            FIBC issued balance by plant and bag family from ERP pending orders (Order − Despatch),
            with MTD production and capacity context.
            {data ? ` As of ${data.asOfDate} · day ${data.dayOfMonth}/${data.monthDays}.` : null}
          </p>
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <div className="space-y-1">
            <Label htmlFor="asOf" className="text-xs text-muted-foreground">
              As of
            </Label>
            <input
              id="asOf"
              type="date"
              value={asOf}
              onChange={(e) => setAsOf(e.target.value)}
              className="h-9 rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </div>
          <Button
            variant="outline"
            size="sm"
            disabled={summaryQuery.isFetching}
            onClick={() => {
              setRefreshToken((n) => n + 1);
              toast.message("Refreshing from ERP…");
            }}
          >
            {summaryQuery.isFetching ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
            Refresh
          </Button>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {kpi.map((k) => (
          <div key={k.label} className="rounded-xl border border-border bg-card px-4 py-3">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              {k.label}
            </p>
            <p className="mt-1 text-xl font-semibold tabular-nums">{k.value}</p>
          </div>
        ))}
      </div>

      <div className="grid gap-3 lg:grid-cols-3">
        <StatusCard
          title="Confirm / Open / Planned"
          rows={[
            { label: "Confirm", value: data?.confirmMt, pct: data?.confirmPct },
            { label: "Open", value: data?.openMt, pct: data?.openPct },
            { label: "Planned", value: data?.plannedMt, pct: data?.plannedPct },
          ]}
        />
        <StatusCard
          title="Pending stack"
          rows={[
            { label: "Issued", value: data?.issuedMt },
            { label: "In hand / hold", value: data?.inHandHoldMt },
            { label: "Pend. entry", value: data?.pendEntryMt },
            { label: "Final pend", value: data?.finalPendMt },
          ]}
        />
        <div className="rounded-xl border border-border bg-card px-4 py-3">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Book value (est.)
          </p>
          <p className="mt-2 text-2xl font-semibold tabular-nums">
            ₹ {formatInr(data?.totalValueInr ?? 0)}
          </p>
          <p className="mt-1 text-sm text-muted-foreground">
            ~₹ {formatInr(data?.avgRsPerKg ?? 0)}/kg · {formatMt(data?.totalOrderWtMt ?? 0)} MT ·{" "}
            {data?.totalOrders ?? 0} orders
          </p>
        </div>
      </div>

      {summaryQuery.isError ? (
        <div className="rounded-xl border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {summaryQuery.error instanceof Error
            ? summaryQuery.error.message
            : "Failed to load summary"}
        </div>
      ) : null}

      <div className="overflow-x-auto rounded-xl border border-border">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-[#0B3A5B] text-xs uppercase tracking-wide text-white">
            <tr>
              <th className="px-3 py-2 font-semibold">Unit / bag family</th>
              <th className="px-3 py-2 text-right font-semibold">Bal qty</th>
              <th className="px-3 py-2 text-right font-semibold">Bal wt (MT)</th>
              <th className="px-3 py-2 text-right font-semibold">Decl cpct</th>
              <th className="px-3 py-2 text-right font-semibold">Days</th>
              <th className="px-3 py-2 text-right font-semibold">Act prod</th>
              <th className="px-3 py-2 text-right font-semibold">%</th>
              <th className="px-3 py-2 text-right font-semibold">Confirm</th>
              <th className="px-3 py-2 text-right font-semibold">Open</th>
              <th className="px-3 py-2 text-right font-semibold">MTD FG (MT)</th>
              <th className="px-3 py-2 text-right font-semibold">Status</th>
            </tr>
          </thead>
          <tbody>
            {summaryQuery.isLoading && !data ? (
              <tr>
                <td colSpan={11} className="px-3 py-8 text-center text-muted-foreground">
                  <Loader2 className="mx-auto h-5 w-5 animate-spin" />
                </td>
              </tr>
            ) : null}
            {units.map((unit) => (
              <UnitRows key={unit.unitCode} unit={unit} />
            ))}
            {data ? (
              <tr className="border-t-2 border-border bg-muted/40 font-semibold">
                <td className="px-3 py-2">Grand total</td>
                <td className="px-3 py-2 text-right tabular-nums">
                  {formatMt(units.reduce((s, u) => s + u.balQty, 0), 0)}
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(data.issuedMt)}</td>
                <td colSpan={4} />
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(data.confirmMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(data.openMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">
                  {formatMt(units.reduce((s, u) => s + u.toDateProdMt, 0))}
                </td>
                <td />
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>

      {data?.note ? (
        <p className="text-xs text-muted-foreground">
          {data.note} Source: {data.source}
        </p>
      ) : null}
    </div>
  );
}

function UnitRows({ unit }: { unit: OrderBookUnitBlock }) {
  return (
    <>
      <tr className="border-t border-border bg-muted/30">
        <td className="px-3 py-2 font-semibold">
          {unit.unitCode}
          <span className="ml-2 text-xs font-normal text-muted-foreground">{unit.companyName}</span>
        </td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.balQty, 0)}</td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.balWtMt)}</td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.targetMt)}</td>
        <td className="px-3 py-2 text-right tabular-nums">
          {unit.avgProdMt > 0 ? formatMt(unit.balWtMt / unit.avgProdMt, 0) : "—"}
        </td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.toDateProdMt)}</td>
        <td
          className={cn(
            "px-3 py-2 text-right tabular-nums",
            unit.statusPct < 0 ? "text-destructive" : "text-emerald-700",
          )}
        >
          {unit.statusPct}%
        </td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.confirmMt)}</td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.openMt)}</td>
        <td className="px-3 py-2 text-right tabular-nums">{formatMt(unit.fgWtMt)}</td>
        <td
          className={cn(
            "px-3 py-2 text-right tabular-nums",
            unit.statusMt < 0 ? "text-destructive" : "text-emerald-700",
          )}
        >
          {formatMt(unit.statusMt)}
        </td>
      </tr>
      {unit.lines.map((line) => (
        <tr key={`${unit.unitCode}-${line.bagGroup}`} className="border-t border-border/60">
          <td className="px-3 py-1.5 pl-8 text-muted-foreground">{line.bagGroup}</td>
          <td className="px-3 py-1.5 text-right tabular-nums">{formatMt(line.balQty, 0)}</td>
          <td className="px-3 py-1.5 text-right tabular-nums">{formatMt(line.balWtMt)}</td>
          <td className="px-3 py-1.5 text-right tabular-nums">{formatMt(line.declCapacityMt)}</td>
          <td className="px-3 py-1.5 text-right tabular-nums">
            {line.declDays ? formatMt(line.declDays, 0) : "—"}
          </td>
          <td className="px-3 py-1.5 text-right tabular-nums">{formatMt(line.actProdMt)}</td>
          <td className="px-3 py-1.5 text-right tabular-nums">{line.pct ? `${line.pct}%` : "—"}</td>
          <td colSpan={4} />
        </tr>
      ))}
    </>
  );
}

function StatusCard({
  title,
  rows,
}: {
  title: string;
  rows: { label: string; value?: number; pct?: number }[];
}) {
  return (
    <div className="rounded-xl border border-border bg-card px-4 py-3">
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</p>
      <ul className="mt-2 space-y-1.5">
        {rows.map((r) => (
          <li key={r.label} className="flex items-baseline justify-between gap-3 text-sm">
            <span>{r.label}</span>
            <span className="tabular-nums font-medium">
              {formatMt(r.value ?? 0)} MT
              {typeof r.pct === "number" ? (
                <span className="ml-2 text-xs text-muted-foreground">{r.pct}%</span>
              ) : null}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
