import { createFileRoute, Link } from "@tanstack/react-router";
import { memo, useCallback, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Save, Search, Sparkles, Timer, TriangleAlert, X } from "lucide-react";
import { toast } from "sonner";
import { PlanningPageHeader, PlanningPageShell, PlanningPanel } from "@/components/planning/planning-ui";
import { DatePickerField } from "@/components/DatePickerField";
import { useDebounce } from "@/hooks/useDebounce";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  cancelFibcQuotationHold,
  confirmFibcAllotment,
  confirmFibcCriticalShift,
  confirmFibcQuotationHold,
  createFibcQuotationHold,
  fetchFibcActiveShifts,
  fetchFibcLines,
  fetchFibcOrderAllotmentContext,
  fetchFibcOrderPlan,
  fetchFibcPlanningConfig,
  fetchFibcQuotationHolds,
  fetchFibcSlotGrid,
  previewFibcAllotment,
  previewFibcCriticalShift,
} from "@/lib/planning/fibc-api";
import {
  defaultPlanningDateFrom,
  formatPlanDate,
  occupancyBadgeClass,
  shiftBadgeClass,
  toInputDate,
  type FibcAllotmentRequest,
  type FibcAllotmentResult,
  type FibcCriticalShiftRequest,
  type FibcCriticalShiftResult,
  type FibcLineConfig,
  type FibcPlanningConfig,
  type FibcQuotationHold,
  type FibcSlotGridItem,
  type FibcSlotGridResult,
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
  const debouncedOrderSearch = useDebounce(orderSearch, 250);
  const [selectedOrder, setSelectedOrder] = useState<string | null>(null);
  const [occupancyFilter, setOccupancyFilter] = useState<"all" | "free" | "partial" | "full">("all");

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

  const displayShifts = activeShifts.length > 0 ? activeShifts : (config?.activeShifts ?? []);

  const openOrder = useCallback((orderNo: string) => {
    setSelectedOrder(orderNo);
  }, []);

  const handleOrderLookup = useCallback(() => {
    const trimmed = orderSearch.trim();
    if (!trimmed) {
      toast.error("Enter an order / PO number to look up.");
      return;
    }
    setSelectedOrder(trimmed);
  }, [orderSearch]);

  return (
    <PlanningPageShell>
      <PlanningPageHeader
        title="FIBC Line Planning"
        description={
          config?.confirmSaveEnabled
            ? "View ERP capacity, preview backward slot allotment, and save confirmed plans to prod_fibcallocationMaster."
            : "View ERP capacity and preview backward slot allotment. Preview does not write to the database."
        }
        backTo="/profile"
        backLabel="Back to profile"
        actions={
          <Badge variant="outline" className="font-normal">
            {config?.confirmSaveEnabled ? "Confirm save enabled" : "Preview only — no database writes"}
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
              <dt className="text-muted-foreground">Confirm save</dt>
              <dd className="font-medium">{config.confirmSaveEnabled ? "Enabled" : "Disabled"}</dd>
              <dt className="text-muted-foreground">Replace existing</dt>
              <dd className="font-medium">{config.replaceExistingEnabled ? "Enabled" : "Disabled"}</dd>
              <dt className="text-muted-foreground">Quotation holds</dt>
              <dd className="font-medium">
                {config.quotationHoldEnabled ? `${config.quotationHoldDays} days` : "Disabled"}
              </dd>
              <dt className="text-muted-foreground">Hold email alerts</dt>
              <dd className="font-medium">{config.quotationHoldEmailEnabled ? "Enabled" : "Disabled"}</dd>
              <dt className="text-muted-foreground">Critical order shift</dt>
              <dd className="font-medium">{config.criticalShiftEnabled ? "Enabled" : "Disabled"}</dd>
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

      <PlanOrderPanel
        company={company}
        config={config}
        displayShifts={displayShifts}
        onGridRefresh={refetchGrid}
      />

      {config?.criticalShiftEnabled ? (
        <CriticalOrderPanel company={company} config={config} onGridRefresh={refetchGrid} />
      ) : null}

      <QuotationHoldPanel company={company} config={config} onGridRefresh={refetchGrid} />

      <LinesPanel lines={lines} loading={loadingLines} />

      <SlotGridPanel
        grid={grid}
        loading={loadingGrid}
        gridError={gridError}
        gridErr={gridErr}
        occupancyFilter={occupancyFilter}
        onOccupancyFilterChange={setOccupancyFilter}
        orderSearch={debouncedOrderSearch}
        onOpenOrder={openOrder}
      />

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

const SLOT_ROW_HEIGHT = 42;
const SLOT_GRID_VIEWPORT_HEIGHT = 520;
const SLOT_GRID_OVERSCAN = 10;

function filterSlotItems(
  items: FibcSlotGridItem[],
  occupancyFilter: "all" | "free" | "partial" | "full",
  orderSearch: string,
): FibcSlotGridItem[] {
  const q = orderSearch.trim().toLowerCase();
  return items.filter((item) => {
    if (occupancyFilter !== "all" && item.occupancyStatus !== occupancyFilter) return false;
    if (q) {
      const hay = [item.orderNo, item.partyName, item.marketingNo, item.bagTypeLabel]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      if (!hay.includes(q)) return false;
    }
    return true;
  });
}

function PlanOrderPanel({
  company,
  config,
  displayShifts,
  onGridRefresh,
}: {
  company?: string;
  config?: FibcPlanningConfig;
  displayShifts: string[];
  onGridRefresh: () => void;
}) {
  const [planOrderNo, setPlanOrderNo] = useState("");
  const [planQty, setPlanQty] = useState("");
  const [planBagType, setPlanBagType] = useState("");
  const [planDispatchDate, setPlanDispatchDate] = useState("");
  const [previewResult, setPreviewResult] = useState<FibcAllotmentResult | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [replaceExisting, setReplaceExisting] = useState(false);
  const [contextOrderNo, setContextOrderNo] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const {
    data: planContext,
    isFetching: loadingPlanContext,
  } = useQuery({
    queryKey: ["fibc-plan-context", contextOrderNo],
    queryFn: () => fetchFibcOrderAllotmentContext(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
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

  const confirmMutation = useMutation({
    mutationFn: confirmFibcAllotment,
    onSuccess: (result) => {
      setPreviewResult(result);
      setConfirmOpen(false);
      if (result.saved) {
        toast.success(result.message);
        onGridRefresh();
        const trimmed = planOrderNo.trim();
        if (trimmed) {
          setContextOrderNo(trimmed);
          queryClient.invalidateQueries({ queryKey: ["fibc-order", trimmed] });
        }
      } else if (result.success) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || "Confirm save failed.");
    },
  });

  function buildAllotmentRequest(): FibcAllotmentRequest | null {
    const trimmed = planOrderNo.trim();
    if (!trimmed) return null;
    const qty = Number(planQty.replace(/,/g, ""));
    return {
      orderNo: trimmed,
      companyName: company,
      dispatchDate: planDispatchDate || undefined,
      quantity: qty > 0 ? qty : undefined,
      bagType: planBagType || undefined,
      partyName: planContext?.partyName ?? undefined,
      marketingNo: planContext?.marketingNo ?? undefined,
      replaceExisting: replaceExisting,
    };
  }

  function handleLoadPlanContext() {
    const trimmed = planOrderNo.trim();
    if (!trimmed) {
      toast.error("Enter an order number to load BOM / marketing data.");
      return;
    }
    setContextOrderNo(trimmed);
  }

  function handlePreviewAllotment() {
    const request = buildAllotmentRequest();
    if (!request) {
      toast.error("Order number is required.");
      return;
    }
    previewMutation.mutate(request);
  }

  function handleConfirmAllotment() {
    const request = buildAllotmentRequest();
    if (!request) {
      toast.error("Order number is required.");
      return;
    }
    confirmMutation.mutate(request);
  }

  const previewFullyAllotted =
    previewResult != null &&
    previewResult.proposedSlots.length > 0 &&
    Math.round(
      previewResult.quantity -
        previewResult.proposedSlots.reduce((sum, slot) => sum + slot.allotted, 0),
    ) <= 0;

  const canConfirmSave =
    Boolean(config?.confirmSaveEnabled) &&
    previewResult?.success === true &&
    previewFullyAllotted &&
    previewResult.proposedSlots.length > 0 &&
    (!(planContext?.existingAllocationCount ?? 0) || (replaceExisting && config?.replaceExistingEnabled));

  const hasExistingPlan = (planContext?.existingAllocationCount ?? 0) > 0;

  return (
    <>
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
            {hasExistingPlan ? (
              <div className="rounded-md border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-800 dark:text-amber-200">
                This order already has {planContext?.existingAllocationCount} saved slot(s) in ERP.
                {config?.replaceExistingEnabled ? (
                  <label className="mt-2 flex cursor-pointer items-center gap-2">
                    <input
                      type="checkbox"
                      checked={replaceExisting}
                      onChange={(e) => setReplaceExisting(e.target.checked)}
                      className="h-4 w-4 rounded border-border"
                    />
                    Replace existing plan on confirm save
                  </label>
                ) : (
                  <span className="mt-1 block">Clear existing rows in ERP before saving again.</span>
                )}
              </div>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <Button type="button" onClick={handlePreviewAllotment} disabled={previewMutation.isPending}>
                {previewMutation.isPending ? (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                ) : (
                  <Sparkles className="mr-1 h-4 w-4" />
                )}
                Preview allotment
              </Button>
              {config?.confirmSaveEnabled ? (
                <Button
                  type="button"
                  variant="default"
                  onClick={() => setConfirmOpen(true)}
                  disabled={!canConfirmSave || confirmMutation.isPending}
                >
                  {confirmMutation.isPending ? (
                    <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-1 h-4 w-4" />
                  )}
                  Confirm & save
                </Button>
              ) : null}
            </div>
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

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Save allotment to ERP?</AlertDialogTitle>
            <AlertDialogDescription>
              This will insert {previewResult?.proposedSlots.length ?? 0} row(s) into{" "}
              <span className="font-medium">prod_fibcallocationMaster</span> for order{" "}
              <span className="font-medium">{planOrderNo.trim() || "—"}</span>. The server re-runs preview before
              saving and rolls back if any slot is no longer free.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmAllotment} disabled={confirmMutation.isPending}>
              {confirmMutation.isPending ? "Saving…" : "Confirm save"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

function CriticalOrderPanel({
  company,
  config,
  onGridRefresh,
}: {
  company?: string;
  config?: FibcPlanningConfig;
  onGridRefresh: () => void;
}) {
  const [criticalOrderNo, setCriticalOrderNo] = useState("");
  const [criticalQty, setCriticalQty] = useState("");
  const [criticalBagType, setCriticalBagType] = useState("");
  const [criticalDispatchDate, setCriticalDispatchDate] = useState("");
  const [criticalReason, setCriticalReason] = useState("");
  const [pinToTargetDate, setPinToTargetDate] = useState(false);
  const [previewResult, setPreviewResult] = useState<FibcCriticalShiftResult | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [replaceExisting, setReplaceExisting] = useState(false);
  const [contextOrderNo, setContextOrderNo] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { data: planContext, isFetching: loadingPlanContext } = useQuery({
    queryKey: ["fibc-critical-context", contextOrderNo],
    queryFn: () => fetchFibcOrderAllotmentContext(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
  });

  useEffect(() => {
    if (!planContext) return;
    if (planContext.quantity != null && planContext.quantity > 0) {
      setCriticalQty(String(planContext.quantity));
    }
    if (planContext.bagType) {
      setCriticalBagType(planContext.bagType);
    }
    if (planContext.dispatchDate) {
      const parsed = new Date(planContext.dispatchDate);
      if (!Number.isNaN(parsed.getTime()) && parsed.getFullYear() >= 2000) {
        setCriticalDispatchDate(toInputDate(parsed));
      }
    }
  }, [planContext]);

  const previewMutation = useMutation({
    mutationFn: previewFibcCriticalShift,
    onSuccess: (result) => {
      setPreviewResult(result);
      if (result.success) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || "Critical preview failed.");
    },
  });

  const confirmMutation = useMutation({
    mutationFn: confirmFibcCriticalShift,
    onSuccess: (result) => {
      setPreviewResult(result);
      setConfirmOpen(false);
      if (result.saved) {
        toast.success(result.message);
        onGridRefresh();
        const trimmed = criticalOrderNo.trim();
        if (trimmed) {
          setContextOrderNo(trimmed);
          queryClient.invalidateQueries({ queryKey: ["fibc-order", trimmed] });
        }
      } else if (result.success) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || "Critical confirm save failed.");
    },
  });

  function buildCriticalRequest(): FibcCriticalShiftRequest | null {
    const trimmed = criticalOrderNo.trim();
    if (!trimmed) return null;
    const qty = Number(criticalQty.replace(/,/g, ""));
    return {
      orderNo: trimmed,
      companyName: company,
      dispatchDate: criticalDispatchDate || undefined,
      quantity: qty > 0 ? qty : undefined,
      bagType: criticalBagType || undefined,
      partyName: planContext?.partyName ?? undefined,
      marketingNo: planContext?.marketingNo ?? undefined,
      replaceExisting,
      reason: criticalReason.trim() || undefined,
      pinToTargetDate,
    };
  }

  function handleLoadContext() {
    const trimmed = criticalOrderNo.trim();
    if (!trimmed) {
      toast.error("Enter an order number to load BOM / marketing data.");
      return;
    }
    setContextOrderNo(trimmed);
  }

  function handlePreview() {
    const request = buildCriticalRequest();
    if (!request) {
      toast.error("Order number is required.");
      return;
    }
    previewMutation.mutate(request);
  }

  function handleConfirm() {
    const request = buildCriticalRequest();
    if (!request) {
      toast.error("Order number is required.");
      return;
    }
    confirmMutation.mutate(request);
  }

  const previewFullyAllotted =
    previewResult != null &&
    previewResult.proposedSlots.length > 0 &&
    Math.round(
      previewResult.quantity - previewResult.proposedSlots.reduce((sum, slot) => sum + slot.allotted, 0),
    ) <= 0;

  const hasExistingPlan = (planContext?.existingAllocationCount ?? 0) > 0;

  const canConfirmSave =
    Boolean(config?.confirmSaveEnabled) &&
    previewResult?.success === true &&
    previewFullyAllotted &&
    previewResult.proposedSlots.length > 0 &&
    (!(planContext?.existingAllocationCount ?? 0) || (replaceExisting && config?.replaceExistingEnabled));

  return (
    <>
      <PlanningPanel
        title="Critical order (shift blocking orders)"
        subtitle="When normal allotment is blocked, preview moving other orders forward then allot this order"
        className="mb-6"
      >
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Critical order / PO number</span>
              <div className="flex gap-2">
                <Input
                  value={criticalOrderNo}
                  onChange={(e) => setCriticalOrderNo(e.target.value)}
                  placeholder="Buyer order no."
                  className="h-10"
                />
                <Button type="button" variant="secondary" onClick={handleLoadContext} disabled={loadingPlanContext}>
                  {loadingPlanContext ? <Loader2 className="h-4 w-4 animate-spin" /> : "Load"}
                </Button>
              </div>
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Quantity (pcs)</span>
                <Input
                  value={criticalQty}
                  onChange={(e) => setCriticalQty(e.target.value)}
                  placeholder="From BOM if loaded"
                  className="h-10"
                />
              </label>
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Bag type (ERP)</span>
                <Input
                  value={criticalBagType}
                  onChange={(e) => setCriticalBagType(e.target.value)}
                  placeholder="UPanel / Buffle / Circular"
                  className="h-10"
                />
              </label>
            </div>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Dispatch date</span>
              <DatePickerField
                value={criticalDispatchDate}
                onChange={setCriticalDispatchDate}
                placeholder="From marketing invoice if loaded"
              />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Reason (optional)</span>
              <Input
                value={criticalReason}
                onChange={(e) => setCriticalReason(e.target.value)}
                placeholder="Urgent dispatch, customer escalation…"
                className="h-10"
              />
            </label>
            <label className="flex cursor-pointer items-start gap-2 rounded-md border border-border/70 bg-muted/30 px-3 py-2 text-xs">
              <input
                type="checkbox"
                checked={pinToTargetDate}
                onChange={(e) => setPinToTargetDate(e.target.checked)}
                className="mt-0.5 h-4 w-4 rounded border-border"
              />
              <span>
                <span className="font-medium text-foreground">Pin to target date only</span>
                <span className="mt-0.5 block text-muted-foreground">
                  Require slots on the target completion date (dispatch − buffer). Off by default — not standard
                  production behaviour. Turn on to demo order shifting when that day is blocked.
                </span>
              </span>
            </label>
            {planContext?.partyName ? (
              <p className="text-xs text-muted-foreground">Customer: {planContext.partyName}</p>
            ) : null}
            {hasExistingPlan ? (
              <div className="rounded-md border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-800 dark:text-amber-200">
                This order already has {planContext?.existingAllocationCount} saved slot(s) in ERP.
                {config?.replaceExistingEnabled ? (
                  <label className="mt-2 flex cursor-pointer items-center gap-2">
                    <input
                      type="checkbox"
                      checked={replaceExisting}
                      onChange={(e) => setReplaceExisting(e.target.checked)}
                      className="h-4 w-4 rounded border-border"
                    />
                    Replace existing plan on confirm save
                  </label>
                ) : (
                  <span className="mt-1 block">Clear existing rows in ERP before saving again.</span>
                )}
              </div>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <Button type="button" onClick={handlePreview} disabled={previewMutation.isPending}>
                {previewMutation.isPending ? (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                ) : (
                  <TriangleAlert className="mr-1 h-4 w-4" />
                )}
                Preview critical shift
              </Button>
              {config?.confirmSaveEnabled ? (
                <Button
                  type="button"
                  variant="default"
                  onClick={() => setConfirmOpen(true)}
                  disabled={!canConfirmSave || confirmMutation.isPending}
                >
                  {confirmMutation.isPending ? (
                    <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-1 h-4 w-4" />
                  )}
                  Confirm shift & save
                </Button>
              ) : null}
            </div>
          </div>

          <div className="rounded-lg border border-border/70 bg-surface/40 p-4">
            {previewResult ? (
              <CriticalShiftPreviewSummary result={previewResult} />
            ) : (
              <p className="text-sm text-muted-foreground">
                Run preview when a critical order cannot be fully allotted. The engine will try to move blocking orders
                to later free slots, then propose slots for this order.
              </p>
            )}
          </div>
        </div>

        {previewResult && previewResult.displacements.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">Orders to shift</h4>
            <table className="w-full min-w-[900px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Order</th>
                  <th className="px-2 py-2 font-medium">From</th>
                  <th className="px-2 py-2 font-medium">To</th>
                  <th className="px-2 py-2 font-medium">Qty</th>
                </tr>
              </thead>
              <tbody>
                {previewResult.displacements.map((item, idx) => (
                  <tr key={`shift-${idx}`} className="border-b border-border/60 last:border-0">
                    <td className="px-2 py-2.5">
                      <div className="font-medium">{item.orderNo}</div>
                      {item.partyName ? <div className="text-xs text-muted-foreground">{item.partyName}</div> : null}
                    </td>
                    <td className="px-2 py-2.5 whitespace-nowrap">
                      {formatPlanDate(item.fromPlanDate)} · L{item.fromLineNo} · {item.fromShift}
                    </td>
                    <td className="px-2 py-2.5 whitespace-nowrap">
                      {formatPlanDate(item.toPlanDate)} · L{item.toLineNo} · {item.toShift}
                    </td>
                    <td className="px-2 py-2.5">{item.qty.toLocaleString()} pcs</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}

        {previewResult && previewResult.proposedSlots.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">Proposed slots for critical order</h4>
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
                  <PreviewSlotRow key={`critical-preview-${idx}`} item={item} />
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </PlanningPanel>

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Save critical shift to ERP?</AlertDialogTitle>
            <AlertDialogDescription>
              This will move {previewResult?.displacements.length ?? 0} blocking slot(s) to new dates/lines and insert{" "}
              {previewResult?.proposedSlots.length ?? 0} row(s) for critical order{" "}
              <span className="font-medium">{criticalOrderNo.trim() || "—"}</span> in{" "}
              <span className="font-medium">prod_fibcallocationMaster</span>. The server re-runs preview before saving.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirm} disabled={confirmMutation.isPending}>
              {confirmMutation.isPending ? "Saving…" : "Confirm shift & save"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

function QuotationHoldPanel({
  company,
  config,
  onGridRefresh,
}: {
  company?: string;
  config?: FibcPlanningConfig;
  onGridRefresh: () => void;
}) {
  const [holdOrderNo, setHoldOrderNo] = useState("");
  const [holdQty, setHoldQty] = useState("");
  const [holdBagType, setHoldBagType] = useState("");
  const [holdDispatchDate, setHoldDispatchDate] = useState("");
  const [holdNotes, setHoldNotes] = useState("");
  const [contextOrderNo, setContextOrderNo] = useState<string | null>(null);
  const [confirmHoldId, setConfirmHoldId] = useState<number | null>(null);
  const [replaceExisting, setReplaceExisting] = useState(false);
  const queryClient = useQueryClient();

  const holdsEnabled = Boolean(config?.quotationHoldEnabled);

  const { data: activeHolds = [], isLoading: loadingHolds, refetch: refetchHolds } = useQuery({
    queryKey: ["fibc-quotation-holds", company],
    queryFn: () => fetchFibcQuotationHolds(company),
    enabled: holdsEnabled && Boolean(company),
    staleTime: 1000 * 30,
  });

  const { data: holdContext, isFetching: loadingHoldContext } = useQuery({
    queryKey: ["fibc-hold-context", contextOrderNo],
    queryFn: () => fetchFibcOrderAllotmentContext(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
  });

  useEffect(() => {
    if (!holdContext) return;
    if (holdContext.quantity != null && holdContext.quantity > 0) {
      setHoldQty(String(holdContext.quantity));
    }
    if (holdContext.bagType) {
      setHoldBagType(holdContext.bagType);
    }
    if (holdContext.dispatchDate) {
      const parsed = new Date(holdContext.dispatchDate);
      if (!Number.isNaN(parsed.getTime()) && parsed.getFullYear() >= 2000) {
        setHoldDispatchDate(toInputDate(parsed));
      }
    }
  }, [holdContext]);

  const createHoldMutation = useMutation({
    mutationFn: createFibcQuotationHold,
    onSuccess: (result) => {
      if (result.success) {
        toast.success(result.message);
        refetchHolds();
        onGridRefresh();
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => toast.error(err.message || "Create hold failed."),
  });

  const confirmHoldMutation = useMutation({
    mutationFn: ({ holdId, replace }: { holdId: number; replace: boolean }) =>
      confirmFibcQuotationHold(holdId, replace),
    onSuccess: (result) => {
      setConfirmHoldId(null);
      if (result.saved) {
        toast.success(result.message);
        refetchHolds();
        onGridRefresh();
        queryClient.invalidateQueries({ queryKey: ["fibc-order"] });
      } else if (result.success) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => toast.error(err.message || "Confirm hold failed."),
  });

  const cancelHoldMutation = useMutation({
    mutationFn: cancelFibcQuotationHold,
    onSuccess: (result) => {
      if (result.success) {
        toast.success(result.message);
        refetchHolds();
        onGridRefresh();
      } else {
        toast.error(result.message);
      }
    },
    onError: (err: Error) => toast.error(err.message || "Cancel hold failed."),
  });

  function handleLoadHoldContext() {
    const trimmed = holdOrderNo.trim();
    if (!trimmed) {
      toast.error("Enter an order number to load BOM / marketing data.");
      return;
    }
    setContextOrderNo(trimmed);
  }

  function handleCreateHold() {
    const trimmed = holdOrderNo.trim();
    if (!trimmed) {
      toast.error("Order number is required.");
      return;
    }
    const qty = Number(holdQty.replace(/,/g, ""));
    createHoldMutation.mutate({
      orderNo: trimmed,
      companyName: company,
      dispatchDate: holdDispatchDate || undefined,
      quantity: qty > 0 ? qty : undefined,
      bagType: holdBagType || undefined,
      partyName: holdContext?.partyName ?? undefined,
      marketingNo: holdContext?.marketingNo ?? undefined,
      notes: holdNotes || undefined,
    });
  }

  const selectedHold = activeHolds.find((h) => h.holdId === confirmHoldId);

  if (!holdsEnabled) {
    return null;
  }

  return (
    <>
      <PlanningPanel
        title="Quotation holds"
        subtitle={`Reserve slots for ~${config?.quotationHoldDays ?? 7} days while marketing prepares a quotation`}
        className="mb-6"
      >
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Order / PO number</span>
              <div className="flex gap-2">
                <Input
                  value={holdOrderNo}
                  onChange={(e) => setHoldOrderNo(e.target.value)}
                  placeholder="Buyer order no."
                  className="h-10"
                />
                <Button type="button" variant="secondary" onClick={handleLoadHoldContext} disabled={loadingHoldContext}>
                  {loadingHoldContext ? <Loader2 className="h-4 w-4 animate-spin" /> : "Load"}
                </Button>
              </div>
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Quantity (pcs)</span>
                <Input value={holdQty} onChange={(e) => setHoldQty(e.target.value)} className="h-10" />
              </label>
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Bag type (ERP)</span>
                <Input value={holdBagType} onChange={(e) => setHoldBagType(e.target.value)} className="h-10" />
              </label>
            </div>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Dispatch date</span>
              <DatePickerField value={holdDispatchDate} onChange={setHoldDispatchDate} />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Notes (optional)</span>
              <Input
                value={holdNotes}
                onChange={(e) => setHoldNotes(e.target.value)}
                placeholder="Marketing reference"
                className="h-10"
              />
            </label>
            <Button type="button" onClick={handleCreateHold} disabled={createHoldMutation.isPending}>
              {createHoldMutation.isPending ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <Timer className="mr-1 h-4 w-4" />
              )}
              Create quotation hold
            </Button>
            <p className="text-xs text-muted-foreground">
              Runs the same allotment engine as preview, then stores reserved slots in app tables. Other previews subtract
              active holds from available capacity.
            </p>
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-sm font-medium">Active holds</h3>
              <Button type="button" variant="ghost" size="sm" onClick={() => refetchHolds()} disabled={loadingHolds}>
                Refresh
              </Button>
            </div>
            {loadingHolds ? (
              <p className="text-sm text-muted-foreground">Loading holds…</p>
            ) : activeHolds.length === 0 ? (
              <p className="text-sm text-muted-foreground">No active quotation holds.</p>
            ) : (
              <div className="space-y-3">
                {activeHolds.map((hold) => (
                  <QuotationHoldCard
                    key={hold.holdId}
                    hold={hold}
                    confirmSaveEnabled={Boolean(config?.confirmSaveEnabled)}
                    onConfirm={() => setConfirmHoldId(hold.holdId)}
                    onCancel={() => cancelHoldMutation.mutate(hold.holdId)}
                    cancelling={cancelHoldMutation.isPending}
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      </PlanningPanel>

      <AlertDialog open={confirmHoldId != null} onOpenChange={(open) => !open && setConfirmHoldId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirm quotation hold?</AlertDialogTitle>
            <AlertDialogDescription>
              This will insert {selectedHold?.slots.length ?? 0} row(s) into{" "}
              <span className="font-medium">prod_fibcallocationMaster</span> for order{" "}
              <span className="font-medium">{selectedHold?.orderNo ?? "—"}</span> and mark hold{" "}
              <span className="font-medium">{selectedHold?.referenceCode ?? ""}</span> as confirmed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {config?.replaceExistingEnabled ? (
            <label className="flex cursor-pointer items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={replaceExisting}
                onChange={(e) => setReplaceExisting(e.target.checked)}
                className="h-4 w-4 rounded border-border"
              />
              Replace existing ERP plan if order already has allocations
            </label>
          ) : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmHoldMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (confirmHoldId != null) {
                  confirmHoldMutation.mutate({ holdId: confirmHoldId, replace: replaceExisting });
                }
              }}
              disabled={confirmHoldMutation.isPending}
            >
              {confirmHoldMutation.isPending ? "Saving…" : "Confirm & save to ERP"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

function QuotationHoldCard({
  hold,
  confirmSaveEnabled,
  onConfirm,
  onCancel,
  cancelling,
}: {
  hold: FibcQuotationHold;
  confirmSaveEnabled: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  cancelling: boolean;
}) {
  const expires = new Date(hold.expiresAt);
  const expiresLabel = Number.isNaN(expires.getTime())
    ? hold.expiresAt
    : expires.toLocaleString("en-IN", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });

  return (
    <div className="rounded-lg border border-border/70 bg-surface/40 p-3 text-sm">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="font-medium">{hold.referenceCode}</p>
          <p className="text-xs text-muted-foreground">
            Order {hold.orderNo} · {hold.quantity.toLocaleString()} pcs · {hold.bagTypeLabel || hold.bagType || "—"}
          </p>
          <p className="text-xs text-muted-foreground">Expires {expiresLabel}</p>
          {hold.partyName ? <p className="text-xs text-muted-foreground">{hold.partyName}</p> : null}
        </div>
        <div className="flex flex-wrap gap-2">
          {confirmSaveEnabled ? (
            <Button type="button" size="sm" onClick={onConfirm}>
              <Save className="mr-1 h-3.5 w-3.5" />
              Confirm
            </Button>
          ) : null}
          <Button type="button" size="sm" variant="secondary" onClick={onCancel} disabled={cancelling}>
            Cancel hold
          </Button>
        </div>
      </div>
      {hold.slots.length > 0 ? (
        <ul className="mt-2 space-y-1 text-xs text-muted-foreground">
          {hold.slots.map((slot, idx) => (
            <li key={`${hold.holdId}-${idx}`}>
              {formatPlanDate(slot.planDate)} · Line {slot.lineNo} · Shift {slot.shift} · {slot.qty.toLocaleString()} pcs
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

const LinesPanel = memo(function LinesPanel({
  lines,
  loading,
}: {
  lines: FibcLineConfig[];
  loading: boolean;
}) {
  return (
    <PlanningPanel
      title="Production lines"
      subtitle="NewLineMaster — bag type & capacity per line"
      className="mb-6"
    >
      {loading ? (
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
  );
});

const VirtualizedSlotTable = memo(function VirtualizedSlotTable({
  items,
  onOpenOrder,
}: {
  items: FibcSlotGridItem[];
  onOpenOrder: (orderNo: string) => void;
}) {
  const [scrollTop, setScrollTop] = useState(0);

  useEffect(() => {
    setScrollTop(0);
  }, [items]);

  const { startIndex, endIndex, paddingTop, paddingBottom } = useMemo(() => {
    const visibleCount = Math.ceil(SLOT_GRID_VIEWPORT_HEIGHT / SLOT_ROW_HEIGHT) + SLOT_GRID_OVERSCAN * 2;
    const start = Math.max(0, Math.floor(scrollTop / SLOT_ROW_HEIGHT) - SLOT_GRID_OVERSCAN);
    const end = Math.min(items.length, start + visibleCount);
    return {
      startIndex: start,
      endIndex: end,
      paddingTop: start * SLOT_ROW_HEIGHT,
      paddingBottom: (items.length - end) * SLOT_ROW_HEIGHT,
    };
  }, [items.length, scrollTop]);

  const visibleItems = useMemo(
    () => items.slice(startIndex, endIndex),
    [items, startIndex, endIndex],
  );

  return (
    <div
      className="overflow-x-auto overflow-y-auto rounded-md border border-border/60"
      style={{ maxHeight: SLOT_GRID_VIEWPORT_HEIGHT }}
      onScroll={(event) => setScrollTop(event.currentTarget.scrollTop)}
    >
      <table className="w-full min-w-[960px] text-left text-sm">
        <thead className="sticky top-0 z-10 bg-card">
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
          {paddingTop > 0 ? (
            <tr aria-hidden className="border-0">
              <td colSpan={9} style={{ height: paddingTop, padding: 0, border: 0 }} />
            </tr>
          ) : null}
          {visibleItems.map((item, idx) => (
            <SlotRow
              key={`${item.planDate}-${item.lineNo}-${item.shift}-${startIndex + idx}`}
              item={item}
              onOpenOrder={onOpenOrder}
            />
          ))}
          {paddingBottom > 0 ? (
            <tr aria-hidden className="border-0">
              <td colSpan={9} style={{ height: paddingBottom, padding: 0, border: 0 }} />
            </tr>
          ) : null}
        </tbody>
      </table>
    </div>
  );
});

const SlotGridPanel = memo(function SlotGridPanel({
  grid,
  loading,
  gridError,
  gridErr,
  occupancyFilter,
  onOccupancyFilterChange,
  orderSearch,
  onOpenOrder,
}: {
  grid?: FibcSlotGridResult;
  loading: boolean;
  gridError: boolean;
  gridErr: unknown;
  occupancyFilter: "all" | "free" | "partial" | "full";
  onOccupancyFilterChange: (value: "all" | "free" | "partial" | "full") => void;
  orderSearch: string;
  onOpenOrder: (orderNo: string) => void;
}) {
  const filteredItems = useMemo(
    () => filterSlotItems(grid?.items ?? [], occupancyFilter, orderSearch),
    [grid?.items, occupancyFilter, orderSearch],
  );

  return (
    <PlanningPanel
      title="Slot grid"
      subtitle={
        grid
          ? `${grid.occupiedSlots} occupied / ${grid.totalSlots} slots · ${formatPlanDate(grid.dateFrom)} – ${formatPlanDate(grid.dateTo)}${filteredItems.length !== grid.totalSlots ? ` · showing ${filteredItems.length.toLocaleString()}` : ""}`
          : "Select a date range"
      }
      headerRight={
        <div className="flex flex-wrap gap-1">
          {(["all", "free", "partial", "full"] as const).map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => onOccupancyFilterChange(key)}
              className={cn(
                "rounded-full px-2.5 py-1 text-xs font-medium capitalize transition-colors",
                occupancyFilter === key
                  ? "bg-primary text-primary-foreground"
                  : "bg-secondary text-muted-foreground hover:text-foreground",
              )}
            >
              {key}
            </button>
          ))}
        </div>
      }
    >
      {loading ? (
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
        <VirtualizedSlotTable items={filteredItems} onOpenOrder={onOpenOrder} />
      )}
    </PlanningPanel>
  );
});

function CriticalShiftPreviewSummary({ result }: { result: FibcCriticalShiftResult }) {
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
        <dt className="text-muted-foreground">Shifts required</dt>
        <dd>{result.shiftsRequired ? `Yes (${result.displacements.length})` : "No"}</dd>
        <dt className="text-muted-foreground">Fully allotted</dt>
        <dd>{result.fullyAllotted ? "Yes" : "No"}</dd>
        <dt className="text-muted-foreground">Pin to target date</dt>
        <dd>{result.pinToTargetDate ? "Yes" : "No"}</dd>
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

const SlotRow = memo(function SlotRow({
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
});

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
              {detail.savedAllocations.length > 0 ? (
                <section>
                  <h4 className="mb-2 text-sm font-semibold">Saved allocations (prod_fibcallocationMaster)</h4>
                  <div className="space-y-2">
                    {detail.savedAllocations.map((line, i) => (
                      <div key={`saved-${i}`} className="rounded-lg border border-emerald-500/25 bg-emerald-500/5 p-3 text-sm">
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

              {detail.planLines.length > 0 ? (
                <section>
                  <h4 className="mb-2 text-sm font-semibold">Marketing line plan</h4>
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
