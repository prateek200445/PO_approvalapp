import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Loader2, Search, Sparkles, X } from "lucide-react";
import { toast } from "sonner";
import { PlanningPageHeader, PlanningPageShell, PlanningPanel } from "@/components/planning/planning-ui";
import { DatePickerField } from "@/components/DatePickerField";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  fetchFibcActiveShifts,
  fetchFibcLines,
  fetchFibcOrderAllotmentContext,
  fetchFibcOrderPlan,
  fetchFibcPlanningConfig,
  fetchFibcSlotGrid,
  previewFibcAllotment,
} from "@/lib/planning/fibc-api";
import {
  defaultPlanningDateFrom,
  formatPlanDate,
  occupancyBadgeClass,
  shiftBadgeClass,
  toInputDate,
  type FibcAllotmentResult,
  type FibcSlotGridItem,
} from "@/lib/planning/fibc-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/planning/fibc/")({
  head: () => ({ meta: [{ title: "FIBC Line Planning — PO Portal" }] }),
  component: FibcPlanningPage,
});

function FibcPlanningPage() {
  const [dateFrom, setDateFrom] = useState(defaultPlanningDateFrom());
  const [dateTo, setDateTo] = useState(toInputDate(new Date()));
  const [orderSearch, setOrderSearch] = useState("");
  const [selectedOrder, setSelectedOrder] = useState<string | null>(null);
  const [occupancyFilter, setOccupancyFilter] = useState<"all" | "free" | "partial" | "full">("all");

  const [planOrderNo, setPlanOrderNo] = useState("");
  const [planQty, setPlanQty] = useState("");
  const [planBagType, setPlanBagType] = useState("");
  const [planDispatchDate, setPlanDispatchDate] = useState("");
  const [previewResult, setPreviewResult] = useState<FibcAllotmentResult | null>(null);

  const { data: config } = useQuery({
    queryKey: ["fibc-planning-config"],
    queryFn: fetchFibcPlanningConfig,
    staleTime: 1000 * 60 * 30,
  });

  const company = config?.defaultCompanyName;

  const {
    data: lines = [],
    isLoading: loadingLines,
  } = useQuery({
    queryKey: ["fibc-lines", company],
    queryFn: () => fetchFibcLines(company),
    enabled: Boolean(company),
    staleTime: 1000 * 60 * 10,
  });

  const {
    data: grid,
    isLoading: loadingGrid,
    isError: gridError,
    error: gridErr,
    refetch: refetchGrid,
  } = useQuery({
    queryKey: ["fibc-grid", dateFrom, dateTo, company],
    queryFn: () => fetchFibcSlotGrid({ from: dateFrom, to: dateTo, company }),
    enabled: Boolean(dateFrom && dateTo && company),
    staleTime: 1000 * 60 * 2,
  });

  const {
    data: orderDetail,
    isFetching: loadingOrder,
  } = useQuery({
    queryKey: ["fibc-order", selectedOrder],
    queryFn: () => fetchFibcOrderPlan(selectedOrder!),
    enabled: Boolean(selectedOrder),
  });

  const { data: activeShifts = [] } = useQuery({
    queryKey: ["fibc-shifts", dateFrom, dateTo, company],
    queryFn: () => fetchFibcActiveShifts({ from: dateFrom, to: dateTo, company }),
    enabled: Boolean(dateFrom && dateTo && company),
    staleTime: 1000 * 60 * 5,
  });

  const {
    data: planContext,
    isFetching: loadingPlanContext,
    refetch: refetchPlanContext,
  } = useQuery({
    queryKey: ["fibc-plan-context", planOrderNo.trim()],
    queryFn: () => fetchFibcOrderAllotmentContext(planOrderNo.trim()),
    enabled: false,
  });

  useEffect(() => {
    if (!planContext) return;
    if (planContext.quantity != null && planContext.quantity > 0) {
      setPlanQty(String(planContext.quantity));
    }
    if (planContext.bagType) {
      setPlanBagType(planContext.bagType);
    }
    if (planContext.dispatchDate) {
      const parsed = new Date(planContext.dispatchDate);
      if (!Number.isNaN(parsed.getTime()) && parsed.getFullYear() >= 2000) {
        setPlanDispatchDate(toInputDate(parsed));
      }
    }
  }, [planContext]);

  const previewMutation = useMutation({
    mutationFn: previewFibcAllotment,
    onSuccess: (result) => {
      setPreviewResult(result);
      if (result.success) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || "Preview failed.");
    },
  });

  const displayShifts = activeShifts.length > 0 ? activeShifts : (config?.activeShifts ?? []);

  const filteredItems = useMemo(() => {
    const items = grid?.items ?? [];
    return items.filter((item) => {
      if (occupancyFilter !== "all" && item.occupancyStatus !== occupancyFilter) return false;
      if (orderSearch.trim()) {
        const q = orderSearch.trim().toLowerCase();
        const hay = [item.orderNo, item.partyName, item.marketingNo, item.bagTypeLabel]
          .filter(Boolean)
          .join(" ")
          .toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [grid?.items, occupancyFilter, orderSearch]);

  function openOrder(orderNo: string) {
    setSelectedOrder(orderNo);
  }

  function handleOrderLookup() {
    const trimmed = orderSearch.trim();
    if (!trimmed) {
      toast.error("Enter an order / PO number to look up.");
      return;
    }
    setSelectedOrder(trimmed);
  }

  function handleLoadPlanContext() {
    const trimmed = planOrderNo.trim();
    if (!trimmed) {
      toast.error("Enter an order number to load BOM / marketing data.");
      return;
    }
    refetchPlanContext();
  }

  function handlePreviewAllotment() {
    const trimmed = planOrderNo.trim();
    if (!trimmed) {
      toast.error("Order number is required.");
      return;
    }
    const qty = Number(planQty.replace(/,/g, ""));
    previewMutation.mutate({
      orderNo: trimmed,
      companyName: company,
      dispatchDate: planDispatchDate || undefined,
      quantity: qty > 0 ? qty : undefined,
      bagType: planBagType || undefined,
    });
  }

  return (
    <PlanningPageShell>
      <PlanningPageHeader
        title="FIBC Line Planning"
        description="View ERP capacity and preview backward slot allotment. Preview does not write to the database."
        backTo="/profile"
        backLabel="Back to profile"
        actions={
          <Badge variant="outline" className="font-normal">
            Preview only — no database writes
          </Badge>
        }
      />

      <div className="mb-6 grid gap-4 lg:grid-cols-3">
        <PlanningPanel title="Date range" subtitle="Loads vw_fibclineplanning_NEW from ERP">
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">From</span>
              <DatePickerField value={dateFrom} onChange={setDateFrom} placeholder="Start date" />
            </label>
            <label className="space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">To</span>
              <DatePickerField value={dateTo} onChange={setDateTo} placeholder="End date" />
            </label>
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => refetchGrid()} disabled={loadingGrid}>
              {loadingGrid ? <Loader2 className="mr-1 h-4 w-4 animate-spin" /> : null}
              Refresh grid
            </Button>
          </div>
        </PlanningPanel>

        <PlanningPanel title="Configuration" subtitle={company ?? "Loading…"}>
          {config ? (
            <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
              <dt className="text-muted-foreground">Buffer days</dt>
              <dd className="font-medium">{config.dispatchBufferDays}</dd>
              <dt className="text-muted-foreground">Active shifts</dt>
              <dd className="font-medium">{displayShifts.join(", ") || "—"}</dd>
              <dt className="text-muted-foreground">Shift preference</dt>
              <dd className="font-medium">{config.shiftPreference.join(" → ")}</dd>
            </dl>
          ) : (
            <p className="text-sm text-muted-foreground">Loading config…</p>
          )}
        </PlanningPanel>

        <PlanningPanel title="Order lookup" subtitle="Plan lines + BOM fabric (read-only)">
          <div className="flex gap-2">
            <Input
              value={orderSearch}
              onChange={(e) => setOrderSearch(e.target.value)}
              placeholder="PO / buyer order no."
              className="h-10"
              onKeyDown={(e) => e.key === "Enter" && handleOrderLookup()}
            />
            <Button type="button" onClick={handleOrderLookup} disabled={loadingOrder}>
              {loadingOrder ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
            </Button>
          </div>
        </PlanningPanel>
      </div>

      <PlanningPanel
        title="Plan order (preview)"
        subtitle="Backward scheduling from dispatch date minus buffer days"
        className="mb-6"
      >
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Order / PO number</span>
              <div className="flex gap-2">
                <Input
                  value={planOrderNo}
                  onChange={(e) => setPlanOrderNo(e.target.value)}
                  placeholder="Buyer order no."
                  className="h-10"
                />
                <Button type="button" variant="secondary" onClick={handleLoadPlanContext} disabled={loadingPlanContext}>
                  {loadingPlanContext ? <Loader2 className="h-4 w-4 animate-spin" /> : "Load"}
                </Button>
              </div>
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Quantity (pcs)</span>
                <Input
                  value={planQty}
                  onChange={(e) => setPlanQty(e.target.value)}
                  placeholder="From BOM if loaded"
                  className="h-10"
                />
              </label>
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Bag type (ERP)</span>
                <Input
                  value={planBagType}
                  onChange={(e) => setPlanBagType(e.target.value)}
                  placeholder="UPanel / Buffle / Circular"
                  className="h-10"
                />
              </label>
            </div>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Dispatch date</span>
              <DatePickerField
                value={planDispatchDate}
                onChange={setPlanDispatchDate}
                placeholder="From marketing invoice if loaded"
              />
            </label>
            {planContext?.partyName ? (
              <p className="text-xs text-muted-foreground">Customer: {planContext.partyName}</p>
            ) : null}
            <Button type="button" onClick={handlePreviewAllotment} disabled={previewMutation.isPending}>
              {previewMutation.isPending ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <Sparkles className="mr-1 h-4 w-4" />
              )}
              Preview allotment
            </Button>
          </div>

          <div className="rounded-lg border border-border/70 bg-surface/40 p-4">
            {previewResult ? (
              <AllotmentPreviewSummary result={previewResult} />
            ) : (
              <p className="text-sm text-muted-foreground">
                Enter order details and run preview to see proposed slots. Shifts are taken from CapacityPlanning for the
                selected grid date range ({displayShifts.join(", ") || "loading…"}).
              </p>
            )}
          </div>
        </div>

        {previewResult && previewResult.proposedSlots.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Date</th>
                  <th className="px-2 py-2 font-medium">Line</th>
                  <th className="px-2 py-2 font-medium">Shift</th>
                  <th className="px-2 py-2 font-medium">Qty</th>
                  <th className="px-2 py-2 font-medium">Capacity</th>
                  <th className="px-2 py-2 font-medium">Slot %</th>
                </tr>
              </thead>
              <tbody>
                {previewResult.proposedSlots.map((item, idx) => (
                  <PreviewSlotRow key={`preview-${idx}`} item={item} />
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </PlanningPanel>

      <PlanningPanel
        title="Production lines"
        subtitle="NewLineMaster — bag type & capacity per line"
        className="mb-6"
      >
        {loadingLines ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading lines…
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Line</th>
                  <th className="px-2 py-2 font-medium">Bag type</th>
                  <th className="px-2 py-2 font-medium">Capacity</th>
                  <th className="px-2 py-2 font-medium">Dust</th>
                  <th className="px-2 py-2 font-medium">Buffer</th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line) => (
                  <tr key={line.lineNo} className="border-b border-border/60 last:border-0">
                    <td className="px-2 py-2.5 font-medium">{line.lineNo}</td>
                    <td className="px-2 py-2.5">
                      <div>{line.bagTypeLabel}</div>
                      <div className="text-xs text-muted-foreground">{line.bagType}</div>
                    </td>
                    <td className="px-2 py-2.5">{line.bagCapacity.toLocaleString()}</td>
                    <td className="px-2 py-2.5 text-xs text-muted-foreground">
                      {line.isTripleDust ? "Triple" : line.isDoubleDust ? "Double" : "Normal"}
                    </td>
                    <td className="px-2 py-2.5">{line.bufferDaysCheck}d</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </PlanningPanel>

      <PlanningPanel
        title="Slot grid"
        subtitle={
          grid
            ? `${grid.occupiedSlots} occupied / ${grid.totalSlots} slots · ${formatPlanDate(grid.dateFrom)} – ${formatPlanDate(grid.dateTo)}`
            : "Select a date range"
        }
        headerRight={
          <div className="flex flex-wrap gap-1">
            {(["all", "free", "partial", "full"] as const).map((key) => (
              <button
                key={key}
                type="button"
                onClick={() => setOccupancyFilter(key)}
                className={cn(
                  "rounded-full px-2.5 py-1 text-xs font-medium capitalize transition-colors",
                  occupancyFilter === key ? "bg-primary text-primary-foreground" : "bg-secondary text-muted-foreground hover:text-foreground",
                )}
              >
                {key}
              </button>
            ))}
          </div>
        }
      >
        {loadingGrid ? (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading slot grid…
          </div>
        ) : gridError ? (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
            {(gridErr as Error)?.message ?? "Failed to load planning grid."}
          </div>
        ) : filteredItems.length === 0 ? (
          <p className="py-10 text-center text-sm text-muted-foreground">No slots match the current filters.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[960px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Date</th>
                  <th className="px-2 py-2 font-medium">Line</th>
                  <th className="px-2 py-2 font-medium">Shift</th>
                  <th className="px-2 py-2 font-medium">Bag</th>
                  <th className="px-2 py-2 font-medium">Capacity</th>
                  <th className="px-2 py-2 font-medium">Allotted</th>
                  <th className="px-2 py-2 font-medium">Remaining</th>
                  <th className="px-2 py-2 font-medium">Order</th>
                  <th className="px-2 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item, idx) => (
                  <SlotRow key={`${item.planDate}-${item.lineNo}-${item.shift}-${idx}`} item={item} onOpenOrder={openOrder} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </PlanningPanel>

      {selectedOrder ? (
        <OrderDetailPanel
          orderNo={selectedOrder}
          detail={orderDetail}
          loading={loadingOrder}
          onClose={() => setSelectedOrder(null)}
        />
      ) : null}
    </PlanningPageShell>
  );
}

function AllotmentPreviewSummary({ result }: { result: FibcAllotmentResult }) {
  const allottedTotal = result.proposedSlots.reduce((sum, slot) => sum + slot.allotted, 0);
  return (
    <div className="space-y-2 text-sm">
      <p className={cn("font-medium", result.success ? "text-foreground" : "text-destructive")}>{result.message}</p>
      <dl className="grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
        <dt className="text-muted-foreground">Bag type</dt>
        <dd>{result.bagTypeLabel}</dd>
        <dt className="text-muted-foreground">Quantity</dt>
        <dd>{result.quantity.toLocaleString()} pcs</dd>
        <dt className="text-muted-foreground">Proposed total</dt>
        <dd>{allottedTotal.toLocaleString()} pcs</dd>
        <dt className="text-muted-foreground">Capacity / shift</dt>
        <dd>{result.capacityPerShift.toLocaleString()} (from grid)</dd>
        <dt className="text-muted-foreground">Slots required</dt>
        <dd>{result.slotsRequired}</dd>
        <dt className="text-muted-foreground">Dispatch</dt>
        <dd>{formatPlanDate(result.dispatchDate)}</dd>
        <dt className="text-muted-foreground">Target complete</dt>
        <dd>
          {formatPlanDate(result.targetCompletionDate)} ({result.bufferDays}d buffer)
        </dd>
      </dl>
      {result.warnings.length > 0 ? (
        <ul className="mt-2 list-disc space-y-1 pl-4 text-xs text-amber-700 dark:text-amber-300">
          {result.warnings.map((w, i) => (
            <li key={i}>{w}</li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

function PreviewSlotRow({ item }: { item: FibcSlotGridItem }) {
  const slotPercent =
    item.allocatedPercent ?? (item.capacity > 0 ? Math.round((item.allotted / item.capacity) * 10000) / 100 : 100);

  return (
    <tr className="border-b border-border/60 last:border-0">
      <td className="px-2 py-2.5 whitespace-nowrap">{formatPlanDate(item.planDate)}</td>
      <td className="px-2 py-2.5 font-medium">{item.lineNo}</td>
      <td className="px-2 py-2.5">
        <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset", shiftBadgeClass(item.shift))}>
          {item.shift || "—"}
        </span>
      </td>
      <td className="px-2 py-2.5">{item.allotted.toLocaleString()}</td>
      <td className="px-2 py-2.5">{item.capacity.toLocaleString()}</td>
      <td className="px-2 py-2.5">
        {slotPercent}%
        {slotPercent > 0 && slotPercent < 99.9 ? (
          <span className="ml-1 text-xs text-muted-foreground">(partial)</span>
        ) : null}
      </td>
    </tr>
  );
}

function SlotRow({
  item,
  onOpenOrder,
}: {
  item: FibcSlotGridItem;
  onOpenOrder: (orderNo: string) => void;
}) {
  return (
    <tr className="border-b border-border/60 last:border-0 hover:bg-muted/30">
      <td className="px-2 py-2.5 whitespace-nowrap">{formatPlanDate(item.planDate)}</td>
      <td className="px-2 py-2.5 font-medium">{item.lineNo}</td>
      <td className="px-2 py-2.5">
        <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset", shiftBadgeClass(item.shift))}>
          {item.shift || "—"}
        </span>
      </td>
      <td className="px-2 py-2.5">{item.bagTypeLabel}</td>
      <td className="px-2 py-2.5">{item.capacity.toLocaleString()}</td>
      <td className="px-2 py-2.5">
        {item.allotted.toLocaleString()}
        {item.allocatedPercent != null && item.allocatedPercent > 0 && item.allocatedPercent < 100 ? (
          <span className="ml-1 text-xs text-muted-foreground">({item.allocatedPercent}%)</span>
        ) : null}
      </td>
      <td className="px-2 py-2.5">{item.remaining.toLocaleString()}</td>
      <td className="max-w-[180px] truncate px-2 py-2.5">
        {item.orderNo ? (
          <button type="button" className="text-left text-primary hover:underline" onClick={() => onOpenOrder(item.orderNo!)}>
            {item.orderNo}
          </button>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </td>
      <td className="px-2 py-2.5">
        <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs font-medium capitalize", occupancyBadgeClass(item.occupancyStatus))}>
          {item.occupancyStatus}
        </span>
      </td>
    </tr>
  );
}

function OrderDetailPanel({
  orderNo,
  detail,
  loading,
  onClose,
}: {
  orderNo: string;
  detail: Awaited<ReturnType<typeof fetchFibcOrderPlan>> | undefined;
  loading: boolean;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={onClose}>
      <div
        className="flex max-h-[85vh] w-full max-w-3xl flex-col overflow-hidden rounded-xl bg-card shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-3 border-b border-border px-5 py-4">
          <div className="min-w-0">
            <h3 className="truncate text-base font-semibold">{orderNo}</h3>
            <p className="text-xs text-muted-foreground">ERP plan lines & BOM fabric requirements</p>
          </div>
          <button type="button" onClick={onClose} className="rounded-md p-1 hover:bg-secondary" aria-label="Close">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="overflow-y-auto px-5 py-4">
          {loading ? (
            <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading order…
            </div>
          ) : !detail ? (
            <p className="py-8 text-sm text-muted-foreground">No planning data found for this order.</p>
          ) : (
            <div className="space-y-6">
              {detail.planLines.length > 0 ? (
                <section>
                  <h4 className="mb-2 text-sm font-semibold">Line allocations</h4>
                  <div className="space-y-2">
                    {detail.planLines.map((line, i) => (
                      <div key={i} className="rounded-lg border border-border/70 bg-surface/50 p-3 text-sm">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className={cn("rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset", shiftBadgeClass(line.shift))}>
                            L{line.lineNo} · {line.shift}
                          </span>
                          <span className="text-muted-foreground">{formatPlanDate(line.planDate)}</span>
                          <span className="font-medium">{line.qty.toLocaleString()} pcs</span>
                          {line.allocatedPercent != null ? (
                            <span className="text-xs text-muted-foreground">{line.allocatedPercent}% slot</span>
                          ) : null}
                        </div>
                        {line.partyName ? <p className="mt-1 text-xs text-muted-foreground">{line.partyName}</p> : null}
                      </div>
                    ))}
                  </div>
                </section>
              ) : null}

              {detail.fabricRequirements.length > 0 ? (
                <section>
                  <h4 className="mb-2 text-sm font-semibold">Fabric requirements (BOM)</h4>
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[520px] text-left text-xs">
                      <thead>
                        <tr className="border-b text-muted-foreground">
                          <th className="py-1.5 pr-2">Heading</th>
                          <th className="py-1.5 pr-2">GSM</th>
                          <th className="py-1.5 pr-2">Width</th>
                          <th className="py-1.5 pr-2">Meters</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.fabricRequirements.map((row, i) => (
                          <tr key={i} className="border-b border-border/50">
                            <td className="py-2 pr-2">{row.heading || "—"}</td>
                            <td className="py-2 pr-2">{row.gsm || "—"}</td>
                            <td className="py-2 pr-2">{row.fabricSize ?? "—"}</td>
                            <td className="py-2 pr-2">{row.totalMtr?.toLocaleString() ?? "—"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className="mt-3">
                    <Link
                      to="/bom/$"
                      params={{ _splat: encodeURIComponent(detail.orderNo) }}
                      className="text-sm font-medium text-primary hover:underline"
                    >
                      Open BOM report →
                    </Link>
                  </div>
                </section>
              ) : null}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
