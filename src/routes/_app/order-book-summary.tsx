import { useEffect, useMemo, useRef, useState } from "react";
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
  type OrderBookBagLine,
  type OrderBookUnitBlock,
} from "@/lib/order-book-summary-api";

export const Route = createFileRoute("/_app/order-book-summary")({
  head: () => ({ meta: [{ title: "Order Book Summary — PO Portal" }] }),
  component: OrderBookSummaryPage,
});

const ALL_COMPANIES = "__ALL__";

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
  const [selectedUnit, setSelectedUnit] = useState(ALL_COMPANIES);
  // One-shot: only the Refresh button bypasses server cache; date changes use cache.
  const forceRefreshRef = useRef(false);

  const summaryQuery = useQuery({
    queryKey: ["order-book-summary", user?.username, asOf, refreshToken],
    queryFn: () => {
      const refresh = forceRefreshRef.current;
      forceRefreshRef.current = false;
      return getOrderBookSummary(user!.username, asOf, refresh);
    },
    enabled: allowed && !!user?.username,
    staleTime: 15 * 60_000,
    placeholderData: keepPreviousData,
    retry: 1,
  });

  const data = summaryQuery.data;
  const units = data?.units ?? [];
  const showAll = selectedUnit === ALL_COMPANIES;
  const dateMismatch = !!data && data.asOfDate !== asOf;
  const showLoadingOverlay = summaryQuery.isFetching && (!data || dateMismatch);

  useEffect(() => {
    if (selectedUnit === ALL_COMPANIES) return;
    if (units.length === 0) return;
    if (!units.some((u) => u.unitCode === selectedUnit)) {
      setSelectedUnit(ALL_COMPANIES);
    }
  }, [units, selectedUnit]);

  const unit = useMemo(
    () => (showAll ? null : units.find((u) => u.unitCode === selectedUnit) ?? null),
    [units, selectedUnit, showAll],
  );

  const allKpis = useMemo(() => {
    if (!data) return [];
    return [
      { label: "Issued (MT)", value: formatMt(data.issuedMt) },
      { label: "In hand", value: formatMt(data.inHandHoldMt) },
      { label: "Final pend", value: formatMt(data.finalPendMt) },
      { label: "Confirm", value: formatMt(data.confirmMt), pct: data.confirmPct },
      { label: "Open", value: formatMt(data.openMt), pct: data.openPct },
      { label: "Planned", value: formatMt(data.plannedMt), pct: data.plannedPct },
      { label: "OB days", value: String(data.ordBookDaysAvg) },
      { label: "Orders", value: String(data.totalOrders) },
      { label: "Value", value: `₹${formatInr(data.totalValueInr)}` },
      { label: "₹/kg", value: formatInr(data.avgRsPerKg) },
    ];
  }, [data]);

  const companyKpis = useMemo(() => {
    if (!unit) return [];
    return [
      { label: "Bal qty", value: formatMt(unit.balQty, 0) },
      { label: "Bal wt (MT)", value: formatMt(unit.balWtMt) },
      { label: "Confirm", value: formatMt(unit.confirmMt) },
      { label: "Open", value: formatMt(unit.openMt) },
      { label: "Planned", value: formatMt(unit.plannedMt) },
      { label: "Capacity", value: formatMt(unit.targetMt) },
      { label: "MTD prod", value: formatMt(unit.toDateProdMt) },
      { label: "Status %", value: `${unit.statusPct}%`, status: unit.statusPct },
      { label: "FG wt", value: formatMt(unit.fgWtMt) },
      { label: "Orders", value: String(unit.orderCount) },
      { label: "Value", value: `₹${formatInr(unit.valueInr)}` },
      { label: "₹/kg", value: formatInr(unit.rsPerKg) },
    ];
  }, [unit]);

  if (!allowed) {
    return (
      <div className="mx-auto max-w-lg space-y-3 rounded-xl border border-border bg-card p-6 text-center">
        <BookMarked className="mx-auto h-8 w-8 text-muted-foreground" />
        <h1 className="text-xl font-semibold">Restricted report</h1>
        <p className="text-sm text-muted-foreground">
          Order Book Summary is limited to an allowlisted set of users.
        </p>
        <Button variant="outline" onClick={() => navigate({ to: "/dashboard" })}>
          Back to dashboard
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3 md:min-h-0 md:flex-1 md:overflow-hidden">
      <div className="flex shrink-0 flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-primary">
            <BookMarked className="h-5 w-5 shrink-0" />
            <p className="text-xs font-semibold uppercase tracking-wide">Order Book</p>
          </div>
          <h1 className="mt-1 text-xl font-semibold tracking-tight sm:text-2xl">
            Order Book Summary
          </h1>
          {data ? (
            <p className="mt-0.5 text-sm text-muted-foreground">
              As of {data.asOfDate} · day {data.dayOfMonth}/{data.monthDays} · all plants{" "}
              {formatMt(data.issuedMt)} MT issued
            </p>
          ) : null}
        </div>
        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:flex-wrap sm:items-end">
          <div className="min-w-0 flex-1 space-y-1 sm:min-w-[14rem]">
            <Label htmlFor="obs-company" className="text-xs text-muted-foreground">
              Company / unit
            </Label>
            <select
              id="obs-company"
              value={selectedUnit}
              onChange={(e) => setSelectedUnit(e.target.value)}
              disabled={!data && units.length === 0}
              className="h-11 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 sm:h-10"
            >
              <option value={ALL_COMPANIES}>
                All companies
                {data ? ` (${formatMt(data.issuedMt)} MT)` : ""}
              </option>
              {units.map((u) => (
                <option key={u.unitCode} value={u.unitCode}>
                  {u.unitCode} — {u.companyName.trim() || u.unitCode} ({formatMt(u.balWtMt)} MT)
                </option>
              ))}
            </select>
          </div>
          <div className="flex gap-2">
            <div className="min-w-0 flex-1 space-y-1 sm:flex-none">
              <Label htmlFor="obs-asof" className="text-xs text-muted-foreground">
                As of
              </Label>
              <input
                id="obs-asof"
                type="date"
                value={asOf}
                onChange={(e) => setAsOf(e.target.value)}
                className="h-11 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 sm:h-10 sm:w-auto"
              />
            </div>
            <div className="flex flex-col justify-end">
              <Button
                variant="outline"
                size="sm"
                className="h-11 sm:h-10"
                disabled={summaryQuery.isFetching}
                onClick={() => {
                  forceRefreshRef.current = true;
                  setRefreshToken((n) => n + 1);
                  toast.message("Refreshing from ERP…");
                }}
              >
                {summaryQuery.isFetching ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )}
                <span className="ml-1.5 sm:ml-0">Refresh</span>
              </Button>
            </div>
          </div>
        </div>
      </div>

      {summaryQuery.isError ? (
        <div className="shrink-0 rounded-xl border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {summaryQuery.error instanceof Error
            ? summaryQuery.error.message
            : "Failed to load summary"}
        </div>
      ) : null}

      {showLoadingOverlay ? (
        <div className="flex flex-1 items-center justify-center gap-2 text-muted-foreground">
          <Loader2 className="h-6 w-6 animate-spin" />
          <span className="text-sm">Loading order book for {asOf}…</span>
        </div>
      ) : null}

      {!showLoadingOverlay && showAll && data ? (
        <AllCompaniesView
          units={units}
          kpis={allKpis}
          issuedMt={data.issuedMt}
          onSelectUnit={setSelectedUnit}
        />
      ) : null}

      {!showLoadingOverlay && unit ? (
        <div className="flex flex-col gap-3 md:min-h-0 md:flex-1 md:overflow-hidden">
          <div className="shrink-0 rounded-xl border border-border bg-[#0B3A5B] px-4 py-3 text-white">
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <div className="min-w-0">
                <p className="text-lg font-semibold">{unit.unitCode}</p>
                <p className="truncate text-sm text-white/80">{unit.companyName.trim()}</p>
              </div>
              <p className="text-2xl font-semibold tabular-nums">{formatMt(unit.balWtMt)} MT</p>
            </div>
          </div>

          <div className="grid shrink-0 grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-6">
            {companyKpis.map((k) => (
              <div key={k.label} className="rounded-xl border border-border bg-card px-3 py-2">
                <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
                  {k.label}
                </p>
                <p
                  className={cn(
                    "mt-0.5 text-sm font-semibold tabular-nums",
                    typeof k.status === "number" && k.status < 0 && "text-destructive",
                    typeof k.status === "number" && k.status >= 0 && "text-emerald-700",
                  )}
                >
                  {k.value}
                </p>
              </div>
            ))}
          </div>

          {/* Mobile: bag-family cards — page scroll (not nested) */}
          <div className="space-y-2 pb-4 md:hidden">
            {unit.lines.length === 0 ? (
              <div className="rounded-2xl border border-border bg-card p-6 text-center text-sm text-muted-foreground">
                No bag-family lines for this company.
              </div>
            ) : (
              unit.lines.map((line) => (
                <BagFamilyMobileCard key={line.bagGroup} line={line} />
              ))
            )}
          </div>

          {/* Desktop: bag-family table */}
          <div className="hidden min-h-0 flex-1 overflow-auto rounded-xl border border-border md:block">
            <table className="min-w-full text-left text-sm">
              <thead className="sticky top-0 bg-[#0B3A5B] text-xs uppercase tracking-wide text-white">
                <tr>
                  <th className="px-3 py-2.5 font-semibold">Bag family</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Bal qty</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Bal wt (MT)</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Decl cpct</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Days</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Act prod</th>
                  <th className="px-3 py-2.5 text-right font-semibold">Act days</th>
                  <th className="px-3 py-2.5 text-right font-semibold">%</th>
                </tr>
              </thead>
              <tbody>
                {unit.lines.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                      No bag-family lines for this company.
                    </td>
                  </tr>
                ) : (
                  unit.lines.map((line) => (
                    <tr key={line.bagGroup} className="border-t border-border">
                      <td className="px-3 py-2 font-medium">{line.bagGroup}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{formatMt(line.balQty, 0)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">{formatMt(line.balWtMt)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {formatMt(line.declCapacityMt)}
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {line.declDays ? formatMt(line.declDays, 0) : "—"}
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">{formatMt(line.actProdMt)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {line.actDays ? formatMt(line.actDays, 1) : "—"}
                      </td>
                      <td
                        className={cn(
                          "px-3 py-2 text-right tabular-nums font-medium",
                          (line.pct ?? 0) < 60 ? "text-destructive" : "text-emerald-700",
                        )}
                      >
                        {line.pct ? `${line.pct}%` : "—"}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function AllCompaniesView({
  units,
  kpis,
  issuedMt,
  onSelectUnit,
}: {
  units: OrderBookUnitBlock[];
  kpis: { label: string; value: string; pct?: number }[];
  issuedMt: number;
  onSelectUnit: (code: string) => void;
}) {
  return (
    <div className="flex flex-col gap-3 md:min-h-0 md:flex-1 md:overflow-hidden">
      <div className="shrink-0 rounded-xl border border-border bg-[#0B3A5B] px-4 py-3 text-white">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <div>
            <p className="text-lg font-semibold">All companies</p>
            <p className="text-sm text-white/80">{units.length} plants · combined order book</p>
          </div>
          <p className="text-2xl font-semibold tabular-nums">{formatMt(issuedMt)} MT</p>
        </div>
      </div>

      <div className="grid shrink-0 grid-cols-2 gap-2 sm:grid-cols-5 lg:grid-cols-10">
        {kpis.map((k) => (
          <div key={k.label} className="rounded-xl border border-border bg-card px-3 py-2">
            <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
              {k.label}
            </p>
            <p className="mt-0.5 text-sm font-semibold tabular-nums">
              {k.value}
              {typeof k.pct === "number" ? (
                <span className="ml-1 text-[10px] font-normal text-muted-foreground">{k.pct}%</span>
              ) : null}
            </p>
          </div>
        ))}
      </div>

      {/* Mobile: plant cards — scroll with main (not nested overflow) */}
      <div className="space-y-2 pb-4 md:hidden">
        {units.map((u) => (
          <button
            key={u.unitCode}
            type="button"
            onClick={() => onSelectUnit(u.unitCode)}
            className="block w-full min-w-0 touch-manipulation rounded-2xl border border-border bg-card p-4 text-left shadow-soft active:scale-[.99]"
          >
            <div className="flex min-w-0 items-start justify-between gap-2">
              <div className="min-w-0 flex-1">
                <div className="font-semibold text-primary">{u.unitCode}</div>
                <div className="mt-0.5 truncate text-sm text-muted-foreground">
                  {u.companyName.trim() || u.unitCode}
                </div>
              </div>
              <div className="shrink-0 text-right">
                <div className="text-base font-semibold tabular-nums">{formatMt(u.balWtMt)} MT</div>
                <div
                  className={cn(
                    "mt-0.5 text-xs font-medium tabular-nums",
                    u.statusPct < 0 ? "text-destructive" : "text-emerald-700",
                  )}
                >
                  {u.statusPct}% status
                </div>
              </div>
            </div>
            <div className="mt-3 grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
              <MobileStat label="Bal qty" value={formatMt(u.balQty, 0)} />
              <MobileStat label="Confirm" value={formatMt(u.confirmMt)} />
              <MobileStat label="Open" value={formatMt(u.openMt)} />
              <MobileStat label="Planned" value={formatMt(u.plannedMt)} />
              <MobileStat label="MTD prod" value={formatMt(u.toDateProdMt)} />
              <MobileStat label="Orders" value={String(u.orderCount)} />
              <MobileStat label="Value" value={`₹${formatInr(u.valueInr)}`} className="col-span-2" />
            </div>
          </button>
        ))}
        <div className="rounded-2xl border border-border bg-muted/40 p-4">
          <div className="flex items-baseline justify-between gap-2">
            <p className="font-semibold">Grand total</p>
            <p className="text-base font-semibold tabular-nums">{formatMt(issuedMt)} MT</p>
          </div>
          <div className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
            <MobileStat
              label="Bal qty"
              value={formatMt(
                units.reduce((s, u) => s + u.balQty, 0),
                0,
              )}
            />
            <MobileStat
              label="Confirm"
              value={formatMt(units.reduce((s, u) => s + u.confirmMt, 0))}
            />
            <MobileStat
              label="Open"
              value={formatMt(units.reduce((s, u) => s + u.openMt, 0))}
            />
            <MobileStat
              label="Planned"
              value={formatMt(units.reduce((s, u) => s + u.plannedMt, 0))}
            />
            <MobileStat
              label="MTD prod"
              value={formatMt(units.reduce((s, u) => s + u.toDateProdMt, 0))}
              className="col-span-2"
            />
          </div>
        </div>
        <p className="pb-1 text-xs text-muted-foreground">Tap a plant to open bag-family detail.</p>
      </div>

      {/* Desktop: table */}
      <div className="hidden min-h-0 flex-1 overflow-auto rounded-xl border border-border md:block">
        <table className="min-w-full text-left text-sm">
          <thead className="sticky top-0 bg-[#0B3A5B] text-xs uppercase tracking-wide text-white">
            <tr>
              <th className="px-3 py-2.5 font-semibold">Unit</th>
              <th className="px-3 py-2.5 font-semibold">Company</th>
              <th className="px-3 py-2.5 text-right font-semibold">Bal qty</th>
              <th className="px-3 py-2.5 text-right font-semibold">Bal wt</th>
              <th className="px-3 py-2.5 text-right font-semibold">Confirm</th>
              <th className="px-3 py-2.5 text-right font-semibold">Open</th>
              <th className="px-3 py-2.5 text-right font-semibold">Planned</th>
              <th className="px-3 py-2.5 text-right font-semibold">MTD prod</th>
              <th className="px-3 py-2.5 text-right font-semibold">Status %</th>
              <th className="px-3 py-2.5 text-right font-semibold">Orders</th>
              <th className="px-3 py-2.5 text-right font-semibold">Value</th>
            </tr>
          </thead>
          <tbody>
            {units.map((u) => (
              <tr
                key={u.unitCode}
                className="cursor-pointer border-t border-border hover:bg-muted/40"
                onClick={() => onSelectUnit(u.unitCode)}
                title="Open company detail"
              >
                <td className="px-3 py-2 font-semibold text-primary">{u.unitCode}</td>
                <td className="max-w-[14rem] truncate px-3 py-2 text-muted-foreground">
                  {u.companyName.trim()}
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(u.balQty, 0)}</td>
                <td className="px-3 py-2 text-right tabular-nums font-medium">{formatMt(u.balWtMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(u.confirmMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(u.openMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(u.plannedMt)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{formatMt(u.toDateProdMt)}</td>
                <td
                  className={cn(
                    "px-3 py-2 text-right tabular-nums font-medium",
                    u.statusPct < 0 ? "text-destructive" : "text-emerald-700",
                  )}
                >
                  {u.statusPct}%
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{u.orderCount}</td>
                <td className="px-3 py-2 text-right tabular-nums">₹{formatInr(u.valueInr)}</td>
              </tr>
            ))}
            <tr className="border-t-2 border-border bg-muted/40 font-semibold">
              <td className="px-3 py-2" colSpan={2}>
                Grand total
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMt(
                  units.reduce((s, u) => s + u.balQty, 0),
                  0,
                )}
              </td>
              <td className="px-3 py-2 text-right tabular-nums">{formatMt(issuedMt)}</td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMt(units.reduce((s, u) => s + u.confirmMt, 0))}
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMt(units.reduce((s, u) => s + u.openMt, 0))}
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMt(units.reduce((s, u) => s + u.plannedMt, 0))}
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMt(units.reduce((s, u) => s + u.toDateProdMt, 0))}
              </td>
              <td colSpan={3} />
            </tr>
          </tbody>
        </table>
      </div>
      <p className="hidden shrink-0 text-xs text-muted-foreground md:block">
        Click a row to open that company’s bag-family detail.
      </p>
    </div>
  );
}

function MobileStat({
  label,
  value,
  className,
}: {
  label: string;
  value: string;
  className?: string;
}) {
  return (
    <div className={cn("flex min-w-0 items-baseline justify-between gap-2", className)}>
      <span className="text-muted-foreground">{label}</span>
      <span className="truncate font-medium tabular-nums text-foreground">{value}</span>
    </div>
  );
}

function BagFamilyMobileCard({ line }: { line: OrderBookBagLine }) {
  return (
    <div className="rounded-2xl border border-border bg-card p-4 shadow-soft">
      <div className="flex min-w-0 items-start justify-between gap-2">
        <p className="min-w-0 flex-1 font-semibold leading-snug">{line.bagGroup}</p>
        <div className="shrink-0 text-right">
          <p className="text-base font-semibold tabular-nums">{formatMt(line.balWtMt)} MT</p>
          <p
            className={cn(
              "mt-0.5 text-xs font-medium tabular-nums",
              (line.pct ?? 0) < 60 ? "text-destructive" : "text-emerald-700",
            )}
          >
            {line.pct ? `${line.pct}%` : "—"}
          </p>
        </div>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
        <MobileStat label="Bal qty" value={formatMt(line.balQty, 0)} />
        <MobileStat label="Decl cpct" value={formatMt(line.declCapacityMt)} />
        <MobileStat label="Decl days" value={line.declDays ? formatMt(line.declDays, 0) : "—"} />
        <MobileStat label="Act prod" value={formatMt(line.actProdMt)} />
        <MobileStat
          label="Act days"
          value={line.actDays ? formatMt(line.actDays, 1) : "—"}
          className="col-span-2"
        />
      </div>
    </div>
  );
}
