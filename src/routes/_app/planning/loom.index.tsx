import { createFileRoute } from "@tanstack/react-router";
import { memo, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Save, Search, Sparkles, TriangleAlert, X } from "lucide-react";
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
  confirmLoomAllotment,
  confirmAllLoomComponents,
  fetchLoomAllocationGrid,
  fetchLoomMaster,
  fetchLoomOrderAllotmentContext,
  fetchLoomOrderPlan,
  fetchLoomOrderRoute,
  fetchLoomPlanningConfig,
  fetchLoomProductionMeters,
  parsePlanningGsm,
  previewAllLoomComponents,
  previewLoomAllotment,
} from "@/lib/planning/loom-api";
import { searchPlanningFactories, fetchOrderComponentPlan, saveOrderComponentRoutes } from "@/lib/planning/setup-api";
import {
  defaultPlanningDateFrom,
  formatPlanDate,
  toInputDate,
  type LoomAllocationGridItem,
  type LoomAllocationGridResult,
  type LoomAllotmentRequest,
  type LoomAllotmentResult,
  type LoomComponentBatchResult,
  type LoomFabricRequirement,
  type LoomMaster,
  type LoomOrderPlanDetail,
  type LoomPlanningConfig,
  type LoomProductionMeterGridResult,
} from "@/lib/planning/loom-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/planning/loom/")({
  head: () => ({ meta: [{ title: "Loom Planning — PO Portal" }] }),
  component: LoomPlanningPage,
});

function LoomPlanningPage() {
  const [dateFrom, setDateFrom] = useState(defaultPlanningDateFrom());
  const [dateTo, setDateTo] = useState(toInputDate(new Date()));
  const [orderSearch, setOrderSearch] = useState("");
  const [loomFilter, setLoomFilter] = useState("");
  const debouncedOrderSearch = useDebounce(orderSearch, 250);
  const debouncedLoomFilter = useDebounce(loomFilter, 200);
  const [selectedOrder, setSelectedOrder] = useState<string | null>(null);
  const [weavingFactoryQuery, setWeavingFactoryQuery] = useState("");
  const debouncedWeavingFactoryQuery = useDebounce(weavingFactoryQuery, 300);

  const { data: config } = useQuery({
    queryKey: ["loom-planning-config"],
    queryFn: fetchLoomPlanningConfig,
    staleTime: 1000 * 60 * 30,
  });

  const fibcCompany = config?.defaultCompanyName ?? "";
  const [weavingCompany, setWeavingCompany] = useState("");

  useEffect(() => {
    if (config?.defaultCompanyName && !weavingCompany) {
      setWeavingCompany(config.defaultCompanyName);
    }
  }, [config?.defaultCompanyName, weavingCompany]);

  const { data: weavingFactoryOptions = [] } = useQuery({
    queryKey: ["loom-weaving-factory-search", debouncedWeavingFactoryQuery],
    queryFn: () => searchPlanningFactories(debouncedWeavingFactoryQuery),
    enabled: debouncedWeavingFactoryQuery.length >= 0,
  });

  const company = weavingCompany || fibcCompany;

  const {
    data: looms = [],
    isLoading: loadingLooms,
  } = useQuery({
    queryKey: ["loom-master", company],
    queryFn: () => fetchLoomMaster(company),
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
    queryKey: ["loom-grid", dateFrom, dateTo, company],
    queryFn: () => fetchLoomAllocationGrid({ from: dateFrom, to: dateTo, company }),
    enabled: Boolean(dateFrom && dateTo && company),
    staleTime: 1000 * 60 * 2,
  });

  const { data: productionGrid, isLoading: loadingProduction } = useQuery({
    queryKey: ["loom-production", dateFrom, dateTo, company],
    queryFn: () => fetchLoomProductionMeters({ from: dateFrom, to: dateTo, company }),
    enabled: Boolean(dateFrom && dateTo && company),
    staleTime: 1000 * 60 * 2,
  });

  const {
    data: orderDetail,
    isFetching: loadingOrder,
  } = useQuery({
    queryKey: ["loom-order", selectedOrder],
    queryFn: () => fetchLoomOrderPlan(selectedOrder!),
    enabled: Boolean(selectedOrder),
  });

  const filteredItems = useMemo(
    () => filterGridItems(grid?.items ?? [], debouncedOrderSearch, debouncedLoomFilter),
    [grid?.items, debouncedOrderSearch, debouncedLoomFilter],
  );

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
        title="Loom Planning"
        description={
          config?.confirmSaveEnabled
            ? "View loom capacity, preview allotment (cases i–vii), and save confirmed plans to Prod_LoomAlocationMaster."
            : "View loom capacity and preview allotment. Preview does not write to the database."
        }
        backTo="/profile"
        backLabel="Back to profile"
        actions={
          <Badge variant="outline" className="font-normal">
            {config?.confirmSaveEnabled ? "Confirm save enabled" : "Preview only — no database writes"}
          </Badge>
        }
      />

      <div className="mb-6 grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
        <PlanningPanel title="Date range" subtitle="Loads Prod_LoomAlocationMaster from ERP">
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
              <dt className="text-muted-foreground">FIBC factory</dt>
              <dd className="font-medium">{fibcCompany}</dd>
              <dt className="text-muted-foreground">Weaving factory</dt>
              <dd className="font-medium">{company}</dd>
              <dt className="text-muted-foreground">Fabric buffer</dt>
              <dd className="font-medium">{config.fabricBufferDays} days</dd>
              <dt className="text-muted-foreground">Max segment</dt>
              <dd className="font-medium">{config.maxDaysPerLoomSegment} days</dd>
              <dt className="text-muted-foreground">Confirm save</dt>
              <dd className="font-medium">{config.confirmSaveEnabled ? "Enabled" : "Disabled"}</dd>
              <dt className="text-muted-foreground">Looms registered</dt>
              <dd className="font-medium">{loadingLooms ? "…" : looms.length}</dd>
            </dl>
          ) : (
            <p className="text-sm text-muted-foreground">Loading config…</p>
          )}
        </PlanningPanel>

        <PlanningPanel title="Weaving factory" subtitle="Loom pool for this grid & allotment">
          <label className="block space-y-1.5">
            <span className="text-xs font-medium text-muted-foreground">Search factory</span>
            <Input
              value={weavingFactoryQuery}
              onChange={(e) => setWeavingFactoryQuery(e.target.value)}
              placeholder="Unit-II, KPW, sister unit…"
            />
          </label>
          <div className="mt-2 max-h-36 overflow-auto rounded-md border border-border">
            {weavingFactoryOptions.length === 0 ? (
              <p className="p-2 text-xs text-muted-foreground">Type to search factories with looms.</p>
            ) : (
              weavingFactoryOptions
                .filter((opt) => opt.hasLoomMaster)
                .map((opt) => (
                  <button
                    key={opt.companyName}
                    type="button"
                    className={cn(
                      "flex w-full items-center justify-between gap-2 border-b border-border/60 px-2 py-2 text-left text-sm last:border-0 hover:bg-muted/50",
                      opt.companyName === company && "bg-primary/5",
                    )}
                    onClick={() => {
                      setWeavingCompany(opt.companyName);
                      setWeavingFactoryQuery("");
                    }}
                  >
                    <span className="font-medium">{opt.companyName}</span>
                    {opt.companyName === company ? <Badge variant="secondary">Active</Badge> : null}
                  </button>
                ))
            )}
          </div>
          {weavingCompany && weavingCompany !== fibcCompany ? (
            <p className="mt-2 text-xs text-amber-700 dark:text-amber-300">
              Inter-unit: weaving at <strong>{weavingCompany}</strong>, FIBC stays at {fibcCompany}.
            </p>
          ) : null}
        </PlanningPanel>

        <PlanningPanel title="Order lookup" subtitle="Loom allocations + BOM fabric (read-only)">
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
        weavingCompany={company}
        fibcCompany={fibcCompany}
        setWeavingCompany={setWeavingCompany}
        config={config}
        onGridRefresh={refetchGrid}
      />

      <LoomMasterPanel looms={looms} loading={loadingLooms} />

      <AllocationGridPanel
        grid={grid}
        items={filteredItems}
        loading={loadingGrid}
        gridError={gridError}
        gridErr={gridErr}
        loomFilter={loomFilter}
        onLoomFilterChange={setLoomFilter}
        orderSearch={orderSearch}
        onOpenOrder={openOrder}
      />

      <ProductionMetersPanel grid={productionGrid} loading={loadingProduction} />

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

function filterGridItems(items: LoomAllocationGridItem[], orderSearch: string, loomFilter: string) {
  const orderQ = orderSearch.trim().toLowerCase();
  const loomQ = loomFilter.trim().toLowerCase();

  return items.filter((item) => {
    if (loomQ) {
      const hay = [String(item.loomNo), item.loomCode, item.loomSpecification].filter(Boolean).join(" ").toLowerCase();
      if (!hay.includes(loomQ)) return false;
    }
    if (orderQ) {
      const hay = [item.orderNo, item.partyName, item.allocationType, item.color]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      if (!hay.includes(orderQ)) return false;
    }
    return true;
  });
}

const LoomMasterPanel = memo(function LoomMasterPanel({
  looms,
  loading,
}: {
  looms: LoomMaster[];
  loading: boolean;
}) {
  return (
    <PlanningPanel
      title="Loom register"
      subtitle="NewMISLoomMaster — type, make & size range"
      className="mb-6"
    >
      {loading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading looms…
        </div>
      ) : (
        <div className="max-h-64 overflow-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="sticky top-0 bg-card">
              <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                <th className="px-2 py-2 font-medium">Loom</th>
                <th className="px-2 py-2 font-medium">Code</th>
                <th className="px-2 py-2 font-medium">Specification</th>
                <th className="px-2 py-2 font-medium">Make</th>
                <th className="px-2 py-2 font-medium">Size range</th>
                <th className="px-2 py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {looms.map((loom) => (
                <tr key={loom.loomNo} className="border-b border-border/60 last:border-0">
                  <td className="px-2 py-2.5 font-medium">{loom.loomNo}</td>
                  <td className="px-2 py-2.5">{loom.loomCode ?? "—"}</td>
                  <td className="px-2 py-2.5">{loom.loomSpecification ?? "—"}</td>
                  <td className="px-2 py-2.5 text-xs text-muted-foreground">{loom.make ?? "—"}</td>
                  <td className="px-2 py-2.5 whitespace-nowrap">
                    {loom.minSize != null || loom.maxSize != null
                      ? `${loom.minSize ?? "—"} – ${loom.maxSize ?? "—"}`
                      : "—"}
                  </td>
                  <td className="px-2 py-2.5">
                    {loom.isFrozen ? (
                      <Badge variant="secondary" className="font-normal">
                        Frozen
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="font-normal">
                        Active
                      </Badge>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </PlanningPanel>
  );
});

const AllocationGridPanel = memo(function AllocationGridPanel({
  grid,
  items,
  loading,
  gridError,
  gridErr,
  loomFilter,
  onLoomFilterChange,
  orderSearch,
  onOpenOrder,
}: {
  grid?: LoomAllocationGridResult;
  items: LoomAllocationGridItem[];
  loading: boolean;
  gridError: boolean;
  gridErr: Error | null;
  loomFilter: string;
  onLoomFilterChange: (value: string) => void;
  orderSearch: string;
  onOpenOrder: (orderNo: string) => void;
}) {
  return (
    <PlanningPanel
      title="Loom allocation grid"
      subtitle={
        grid
          ? `${grid.totalRows.toLocaleString()} row(s) · ${grid.activeLoomCount} loom(s) with allocations in range`
          : "Prod_LoomAlocationMaster"
      }
      className="mb-6"
      headerRight={
        <div className="flex flex-wrap gap-2">
          <Input
            value={loomFilter}
            onChange={(e) => onLoomFilterChange(e.target.value)}
            placeholder="Filter loom no / code"
            className="h-8 w-40 text-sm"
          />
        </div>
      }
    >
      {loading ? (
        <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading allocations…
        </div>
      ) : gridError ? (
        <p className="py-4 text-sm text-destructive">{gridErr?.message || "Failed to load grid."}</p>
      ) : items.length === 0 ? (
        <p className="py-4 text-sm text-muted-foreground">
          No allocations in this date range
          {orderSearch.trim() || loomFilter.trim() ? " matching your filters" : ""}.
        </p>
      ) : (
        <div className="max-h-[520px] overflow-auto">
          <table className="w-full min-w-[960px] text-left text-sm">
            <thead className="sticky top-0 bg-card">
              <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                <th className="px-2 py-2 font-medium">Date</th>
                <th className="px-2 py-2 font-medium">Loom</th>
                <th className="px-2 py-2 font-medium">Order</th>
                <th className="px-2 py-2 font-medium">Party</th>
                <th className="px-2 py-2 font-medium">GSM</th>
                <th className="px-2 py-2 font-medium">Size</th>
                <th className="px-2 py-2 font-medium">Type</th>
                <th className="px-2 py-2 font-medium">To date</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr
                  key={item.allocationId}
                  className={cn(
                    "border-b border-border/60 last:border-0",
                    !item.isActive && "opacity-60",
                  )}
                >
                  <td className="px-2 py-2.5 whitespace-nowrap">{formatPlanDate(item.allocationDate)}</td>
                  <td className="px-2 py-2.5">
                    <div className="font-medium">L{item.loomNo}</div>
                    {item.loomCode ? <div className="text-xs text-muted-foreground">{item.loomCode}</div> : null}
                  </td>
                  <td className="px-2 py-2.5">
                    {item.orderNo ? (
                      <button
                        type="button"
                        className="font-medium text-primary hover:underline"
                        onClick={() => onOpenOrder(item.orderNo!)}
                      >
                        {item.orderNo}
                      </button>
                    ) : (
                      "—"
                    )}
                  </td>
                  <td className="px-2 py-2.5 max-w-[180px] truncate">{item.partyName ?? "—"}</td>
                  <td className="px-2 py-2.5">{item.reqGsm != null ? item.reqGsm : "—"}</td>
                  <td className="px-2 py-2.5">{item.size != null ? item.size : "—"}</td>
                  <td className="px-2 py-2.5 text-xs text-muted-foreground">{item.allocationType ?? "—"}</td>
                  <td className="px-2 py-2.5 whitespace-nowrap">{formatPlanDate(item.toDate)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </PlanningPanel>
  );
});

function OrderDetailPanel({
  orderNo,
  detail,
  loading,
  onClose,
}: {
  orderNo: string;
  detail?: LoomOrderPlanDetail;
  loading: boolean;
  onClose: () => void;
}) {
  return (
    <PlanningPanel
      title={`Order ${orderNo}`}
      subtitle="Saved loom allocations + BOM fabric requirements"
      className="mb-6"
      headerRight={
        <Button type="button" variant="ghost" size="sm" onClick={onClose}>
          <X className="mr-1 h-4 w-4" />
          Close
        </Button>
      }
    >
      {loading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading order…
        </div>
      ) : !detail ? (
        <p className="text-sm text-muted-foreground">No data returned for this order.</p>
      ) : (
        <div className="space-y-6">
          {detail.allocations.length > 0 ? (
            <div>
              <h3 className="mb-2 text-sm font-medium">Loom allocations ({detail.allocations.length})</h3>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Loom</th>
                      <th className="px-2 py-2 font-medium">From</th>
                      <th className="px-2 py-2 font-medium">To</th>
                      <th className="px-2 py-2 font-medium">GSM</th>
                      <th className="px-2 py-2 font-medium">Size</th>
                      <th className="px-2 py-2 font-medium">Type</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.allocations.map((row, idx) => (
                      <tr key={`alloc-${idx}`} className="border-b border-border/60 last:border-0">
                        <td className="px-2 py-2.5">L{row.loomNo}{row.loomCode ? ` (${row.loomCode})` : ""}</td>
                        <td className="px-2 py-2.5">{formatPlanDate(row.allocationDate)}</td>
                        <td className="px-2 py-2.5">{formatPlanDate(row.toDate)}</td>
                        <td className="px-2 py-2.5">{row.reqGsm ?? "—"}</td>
                        <td className="px-2 py-2.5">{row.size ?? "—"}</td>
                        <td className="px-2 py-2.5 text-xs text-muted-foreground">{row.allocationType ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">No saved loom allocations for this order.</p>
          )}

          {detail.fabricRequirements.length > 0 ? (
            <div>
              <h3 className="mb-2 text-sm font-medium">BOM fabric requirements</h3>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[720px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Heading</th>
                      <th className="px-2 py-2 font-medium">Kind</th>
                      <th className="px-2 py-2 font-medium">GSM</th>
                      <th className="px-2 py-2 font-medium">Size</th>
                      <th className="px-2 py-2 font-medium">Meters</th>
                      <th className="px-2 py-2 font-medium">Kg</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.fabricRequirements.map((row, idx) => (
                      <tr key={`fabric-${idx}`} className="border-b border-border/60 last:border-0">
                        <td className="px-2 py-2.5">{row.heading}</td>
                        <td className="px-2 py-2.5 text-xs text-muted-foreground">
                          {row.isLoomEligible ? "Loom" : row.planningKind}
                        </td>
                        <td className="px-2 py-2.5">{row.gsm || "—"}</td>
                        <td className="px-2 py-2.5">{row.fabricSize ?? "—"}</td>
                        <td className="px-2 py-2.5">{row.totalMtr?.toLocaleString() ?? "—"}</td>
                        <td className="px-2 py-2.5">{row.totalKg?.toLocaleString() ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : null}
        </div>
      )}
    </PlanningPanel>
  );
}

function PlanOrderPanel({
  weavingCompany,
  fibcCompany,
  setWeavingCompany,
  config,
  onGridRefresh,
}: {
  weavingCompany: string;
  fibcCompany: string;
  setWeavingCompany: (company: string) => void;
  config?: LoomPlanningConfig;
  onGridRefresh: () => void;
}) {
  const [planOrderNo, setPlanOrderNo] = useState("");
  const [reqGsm, setReqGsm] = useState("");
  const [size, setSize] = useState("");
  const [requiredMeters, setRequiredMeters] = useState("");
  const [fabricReqDate, setFabricReqDate] = useState("");
  const [heading, setHeading] = useState("");
  const [previewResult, setPreviewResult] = useState<LoomAllotmentResult | null>(null);
  const [batchResult, setBatchResult] = useState<LoomComponentBatchResult | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmAllOpen, setConfirmAllOpen] = useState(false);
  const [replaceExisting, setReplaceExisting] = useState(false);
  const [contextOrderNo, setContextOrderNo] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const {
    data: planContext,
    isFetching: loadingPlanContext,
    isError: planContextError,
    error: planContextErr,
  } = useQuery({
    queryKey: ["loom-plan-context", contextOrderNo],
    queryFn: () => fetchLoomOrderAllotmentContext(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
    retry: false,
  });

  const { data: orderRoute } = useQuery({
    queryKey: ["loom-order-route", contextOrderNo],
    queryFn: () => fetchLoomOrderRoute(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
    retry: false,
  });

  const { data: componentPlan } = useQuery({
    queryKey: ["order-component-plan", contextOrderNo],
    queryFn: () => fetchOrderComponentPlan(contextOrderNo!),
    enabled: Boolean(contextOrderNo),
    retry: false,
  });

  const [routeDrafts, setRouteDrafts] = useState<Record<string, { supply: string; days: string }>>({});

  useEffect(() => {
    if (!componentPlan) return;
    const next: Record<string, { supply: string; days: string }> = {};
    for (const row of componentPlan.components) {
      next[row.heading] = {
        supply: row.supplyCompanyName,
        days: String(row.transferBufferDays),
      };
    }
    setRouteDrafts(next);
  }, [componentPlan]);

  const autoRouteOrderRef = useRef<string | null>(null);

  useEffect(() => {
    if (!orderRoute?.fabricSupplyCompanyName || !contextOrderNo) return;
    // Auto-pick weaving factory once per order when BOM/route explicitly says inter-unit (Sulzer) or saved route.
    if (orderRoute.routeSource !== "Saved" && orderRoute.routeSource !== "AutoDetected") return;
    if (autoRouteOrderRef.current === contextOrderNo) return;
    autoRouteOrderRef.current = contextOrderNo;
    setWeavingCompany(orderRoute.fabricSupplyCompanyName);
  }, [orderRoute, contextOrderNo, setWeavingCompany]);

  useEffect(() => {
    setPreviewResult(null);
  }, [weavingCompany]);

  useEffect(() => {
    if (!planContextError || !planContextErr) return;
    toast.error(planContextErr.message || "Failed to load order context.");
  }, [planContextError, planContextErr]);

  function triggerContextLoad(orderNo: string) {
    const trimmed = orderNo.trim();
    if (!trimmed) return;
    setContextOrderNo(trimmed);
    void queryClient.invalidateQueries({ queryKey: ["loom-plan-context", trimmed] });
  }

  useEffect(() => {
    if (!planContext) return;
    const preferred =
      planContext.loomEligibleLines.find((l) => l.category === "Body") ??
      planContext.loomEligibleLines[0] ??
      planContext.fabricLines.find((l) => l.isLoomEligible) ??
      planContext.fabricLines[0];
    applyFabricLine(preferred);
    if (planContext.fabricRequirementDate) {
      const parsed = new Date(planContext.fabricRequirementDate);
      if (!Number.isNaN(parsed.getTime())) setFabricReqDate(toInputDate(parsed));
    }
  }, [planContext]);

  function applyFabricLine(line?: LoomFabricRequirement) {
    if (!line) return;
    if (line.gsm) setReqGsm(parsePlanningGsm(line.gsm));
    if (line.fabricSize != null) setSize(String(line.fabricSize));
    if (line.totalMtr != null && line.totalMtr > 0) setRequiredMeters(String(line.totalMtr));
    if (line.heading) setHeading(line.heading);
    const supply = routeDrafts[line.heading]?.supply;
    if (supply) setWeavingCompany(supply);
    setPreviewResult(null);
    setBatchResult(null);
  }

  const previewMutation = useMutation({
    mutationFn: previewLoomAllotment,
    onSuccess: (result) => {
      setPreviewResult(result);
      if (result.success) toast.success(result.message);
      else toast.error(result.message);
    },
    onError: (err: Error) => toast.error(err.message || "Preview failed."),
  });

  const batchMutation = useMutation({
    mutationFn: previewAllLoomComponents,
    onSuccess: (result) => {
      setBatchResult(result);
      if (result.success) toast.success(result.message);
      else toast.message(result.message);
    },
    onError: (err: Error) => toast.error(err.message || "Component preview failed."),
  });

  const confirmAllMutation = useMutation({
    mutationFn: confirmAllLoomComponents,
    onSuccess: (result) => {
      setBatchResult(result);
      setConfirmAllOpen(false);
      if (result.savedCount && result.savedCount > 0) {
        toast.success(result.message);
        onGridRefresh();
        const trimmed = planOrderNo.trim();
        if (trimmed) {
          setContextOrderNo(trimmed);
          queryClient.invalidateQueries({ queryKey: ["loom-order", trimmed] });
          queryClient.invalidateQueries({ queryKey: ["loom-plan-context", trimmed] });
        }
      } else toast.message(result.message);
    },
    onError: (err: Error) => toast.error(err.message || "Confirm all failed."),
  });

  const confirmMutation = useMutation({
    mutationFn: confirmLoomAllotment,
    onSuccess: (result) => {
      setPreviewResult(result);
      setConfirmOpen(false);
      if (result.saved) {
        toast.success(result.message);
        onGridRefresh();
        const trimmed = planOrderNo.trim();
        if (trimmed) {
          setContextOrderNo(trimmed);
          queryClient.invalidateQueries({ queryKey: ["loom-order", trimmed] });
        }
      } else if (result.success) toast.success(result.message);
      else toast.error(result.message);
    },
    onError: (err: Error) => toast.error(err.message || "Confirm failed."),
  });

  const saveRoutesMutation = useMutation({
    mutationFn: saveOrderComponentRoutes,
    onSuccess: (plan) => {
      toast.success("Saved component supply factories.");
      queryClient.setQueryData(["order-component-plan", contextOrderNo], plan);
    },
    onError: (err: Error) => toast.error(err.message || "Failed to save component routes."),
  });

  function handleSaveComponentRoutes() {
    const orderNo = planOrderNo.trim();
    if (!orderNo) {
      toast.error("Order number is required.");
      return;
    }
    const components = (componentPlan?.components ?? planContext?.fabricLines ?? []).map((row) => {
      const heading = "heading" in row ? row.heading : "";
      const draft = routeDrafts[heading];
      return {
        heading,
        supplyCompanyName: draft?.supply?.trim() ?? "",
        transferBufferDays: Number(draft?.days) || 0,
      };
    }).filter((row) => row.heading && row.supplyCompanyName);
    if (components.length === 0) {
      toast.error("Enter a supply factory on at least one component.");
      return;
    }
    saveRoutesMutation.mutate({ orderNo, components });
  }

  const buildRequest = (): LoomAllotmentRequest | null => {
    const orderNo = planOrderNo.trim();
    if (!orderNo) {
      toast.error("Order number is required.");
      return null;
    }
    const gsm = Number(reqGsm);
    const width = Number(size);
    const meters = Number(requiredMeters);
    if (!gsm || !width || !meters) {
      toast.error("GSM, width (size), and required meters are required.");
      return null;
    }
    if (!fabricReqDate) {
      toast.error("Fabric requirement date is required.");
      return null;
    }
    return {
      orderNo,
      companyName: weavingCompany || fibcCompany,
      partyName: planContext?.partyName ?? undefined,
      heading: heading.trim() || undefined,
      reqGsm: gsm,
      size: width,
      requiredMeters: meters,
      fabricRequirementDate: fabricReqDate,
      replaceExisting,
    };
  };

  const handlePreview = () => {
    const req = buildRequest();
    if (!req) return;
    setContextOrderNo(req.orderNo);
    setBatchResult(null);
    previewMutation.mutate(req);
  };

  const handlePreviewAllComponents = () => {
    const orderNo = planOrderNo.trim();
    if (!orderNo) {
      toast.error("Order number is required.");
      return;
    }
    if (!fabricReqDate) {
      toast.error("Fabric requirement date is required.");
      return;
    }
    setContextOrderNo(orderNo);
    setPreviewResult(null);
    batchMutation.mutate({
      orderNo,
      companyName: weavingCompany || fibcCompany,
      partyName: planContext?.partyName ?? undefined,
      fabricRequirementDate: fabricReqDate,
    });
  };

  const handleConfirm = () => {
    const req = buildRequest();
    if (!req) return;
    confirmMutation.mutate(req);
  };

  const handleConfirmAllComponents = () => {
    const orderNo = planOrderNo.trim();
    if (!orderNo) {
      toast.error("Order number is required.");
      return;
    }
    if (!fabricReqDate) {
      toast.error("Fabric requirement date is required.");
      return;
    }
    confirmAllMutation.mutate({
      orderNo,
      companyName: weavingCompany || fibcCompany,
      partyName: planContext?.partyName ?? undefined,
      fabricRequirementDate: fabricReqDate,
    });
  };

  const hasExistingPlan = (planContext?.existingAllocationCount ?? 0) > 0;
  const canConfirmSave = Boolean(previewResult?.success && previewResult.fullyAllotted && config?.confirmSaveEnabled);
  const canConfirmAll = Boolean(
    batchResult &&
      !batchResult.confirmed &&
      batchResult.fullyAllottedCount > 0 &&
      config?.confirmSaveEnabled,
  );

  return (
    <>
      <PlanningPanel
        title="Plan order (preview)"
        subtitle="Backward/forward loom allotment — cases i–vii"
        className="mb-6"
      >
        {orderRoute?.isInterUnit && weavingCompany !== fibcCompany ? (
          <div className="mb-4 rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 text-sm text-amber-900 dark:text-amber-200">
            <div className="flex items-start gap-2">
              <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
              <div>
                <strong>Inter-unit order</strong> — weaving at{" "}
                <span className="font-medium">{weavingCompany}</span>, FIBC at{" "}
                <span className="font-medium">{fibcCompany}</span>
                {orderRoute.transferBufferDays > 0
                  ? ` (+${orderRoute.transferBufferDays} day transfer buffer)`
                  : null}
                . Route: {orderRoute.routeSource}.
                {orderRoute.autoDetectedReason ? (
                  <p className="mt-1 text-xs opacity-90">{orderRoute.autoDetectedReason}</p>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Order / PO no.</span>
              <Input
                value={planOrderNo}
                onChange={(e) => setPlanOrderNo(e.target.value)}
                placeholder="Buyer order number"
                onBlur={() => triggerContextLoad(planOrderNo)}
              />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Weaving factory</span>
              <Input value={weavingCompany} readOnly className="bg-muted/30" />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">FIBC factory (fixed)</span>
              <Input value={fibcCompany} readOnly className="bg-muted/30" />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">BOM component</span>
              <Input value={heading || "—"} readOnly className="bg-muted/30" />
            </label>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">GSM</span>
                <Input value={reqGsm} onChange={(e) => setReqGsm(e.target.value)} placeholder="e.g. 170" />
              </label>
              <label className="space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">Width (size cm)</span>
                <Input value={size} onChange={(e) => setSize(e.target.value)} placeholder="e.g. 101.6" />
              </label>
            </div>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Required meters</span>
              <Input value={requiredMeters} onChange={(e) => setRequiredMeters(e.target.value)} placeholder="From BOM" />
            </label>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">Fabric requirement date (FIBC ready)</span>
              <DatePickerField value={fabricReqDate} onChange={setFabricReqDate} placeholder="Target fabric date" />
            </label>
            {loadingPlanContext ? (
              <p className="text-xs text-muted-foreground flex items-center gap-1">
                <Loader2 className="h-3 w-3 animate-spin" /> Loading BOM context…
              </p>
            ) : null}
            {hasExistingPlan ? (
              <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 text-sm text-amber-900 dark:text-amber-200">
                <div className="flex items-start gap-2">
                  <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                  <div>
                    This order already has {planContext?.existingAllocationCount} saved loom row(s). Leave replace
                    unchecked to append this component. Replace deletes every loom row for the order.
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
                </div>
              </div>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <Button type="button" onClick={handlePreview} disabled={previewMutation.isPending}>
                {previewMutation.isPending ? (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                ) : (
                  <Sparkles className="mr-1 h-4 w-4" />
                )}
                Preview allotment
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={handlePreviewAllComponents}
                disabled={batchMutation.isPending || !planOrderNo.trim()}
              >
                {batchMutation.isPending ? (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                ) : (
                  <Sparkles className="mr-1 h-4 w-4" />
                )}
                Preview all loom fabrics
              </Button>
              {config?.confirmSaveEnabled ? (
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setConfirmAllOpen(true)}
                  disabled={!canConfirmAll || confirmAllMutation.isPending}
                >
                  {confirmAllMutation.isPending ? (
                    <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-1 h-4 w-4" />
                  )}
                  Confirm all fabrics
                </Button>
              ) : null}
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
              <LoomPreviewSummary
                result={previewResult}
                weavingCompany={weavingCompany || fibcCompany}
                fibcCompany={fibcCompany}
              />
            ) : (
              <p className="text-sm text-muted-foreground">
                Enter fabric GSM, width, meters and fabric requirement date. Engine uses {config?.fabricBufferDays ?? 5}-day
                buffer before FIBC date and cases i–vii (PPM from LoomSpecificationMaster or defaults).
              </p>
            )}
          </div>
        </div>

        {planContext && planContext.fabricLines.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">
              BOM components ({planContext.loomEligibleLines.length} loom fabric
              {planContext.accessoryLines.length > 0 ? `, ${planContext.accessoryLines.length} accessory` : ""})
            </h4>
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Heading</th>
                  <th className="px-2 py-2 font-medium">Category</th>
                  <th className="px-2 py-2 font-medium">Plan as</th>
                  <th className="px-2 py-2 font-medium">Supply factory</th>
                  <th className="px-2 py-2 font-medium">Xfer days</th>
                  <th className="px-2 py-2 font-medium">Due</th>
                  <th className="px-2 py-2 font-medium">GSM</th>
                  <th className="px-2 py-2 font-medium">Width</th>
                  <th className="px-2 py-2 font-medium">Meters</th>
                </tr>
              </thead>
              <tbody>
                {planContext.fabricLines.map((row, idx) => (
                  <tr
                    key={`bom-${idx}`}
                    className={cn(
                      "border-b border-border/60 last:border-0",
                      row.isLoomEligible ? "cursor-pointer hover:bg-muted/40" : "opacity-90",
                      heading === row.heading && "bg-primary/5",
                    )}
                    onClick={() => {
                      if (row.isLoomEligible) applyFabricLine(row);
                    }}
                  >
                    <td className="px-2 py-2.5">{row.heading}</td>
                    <td className="px-2 py-2.5 text-xs">{row.category}</td>
                    <td className="px-2 py-2.5 text-xs text-muted-foreground">
                      {row.isLoomEligible ? "Loom" : row.planningKind}
                    </td>
                    <td className="px-2 py-2.5" onClick={(e) => e.stopPropagation()}>
                      <Input
                        className="h-8 min-w-[160px] text-xs"
                        value={routeDrafts[row.heading]?.supply ?? ""}
                        onChange={(e) =>
                          setRouteDrafts((prev) => ({
                            ...prev,
                            [row.heading]: {
                              supply: e.target.value,
                              days: prev[row.heading]?.days ?? "0",
                            },
                          }))
                        }
                        placeholder={row.isLoomEligible ? "Weaving factory" : "FIBC / supplier"}
                      />
                    </td>
                    <td className="px-2 py-2.5" onClick={(e) => e.stopPropagation()}>
                      <Input
                        className="h-8 w-16 text-xs"
                        value={routeDrafts[row.heading]?.days ?? "0"}
                        onChange={(e) =>
                          setRouteDrafts((prev) => ({
                            ...prev,
                            [row.heading]: {
                              supply: prev[row.heading]?.supply ?? "",
                              days: e.target.value,
                            },
                          }))
                        }
                      />
                    </td>
                    <td className="px-2 py-2.5 text-xs text-muted-foreground whitespace-nowrap">
                      {componentPlan?.components.find((c) => c.heading === row.heading)?.dueDate?.slice(0, 10) ?? "—"}
                    </td>
                    <td className="px-2 py-2.5">{row.gsm || "—"}</td>
                    <td className="px-2 py-2.5">{row.fabricSize ?? "—"}</td>
                    <td className="px-2 py-2.5">{row.totalMtr?.toLocaleString() ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleSaveComponentRoutes}
                disabled={saveRoutesMutation.isPending}
              >
                {saveRoutesMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : <Save className="h-3 w-3" />}
                <span className="ml-1">Save component factories</span>
              </Button>
              <p className="text-xs text-muted-foreground">
                Click a loom-eligible row to load GSM, width, and meters. Accessories are due at FIBC sewing — assign a
                supplier here, they are not slotted on looms.
              </p>
            </div>
          </div>
        ) : null}

        {batchResult ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">
              {batchResult.confirmed ? "All loom fabrics confirm" : "All loom fabrics preview"}
            </h4>
            {batchResult.warnings.map((w, i) => (
              <p key={i} className="mb-2 text-xs text-amber-800 dark:text-amber-200">
                {w}
              </p>
            ))}
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Component</th>
                  <th className="px-2 py-2 font-medium">Status</th>
                  <th className="px-2 py-2 font-medium">Meters</th>
                  <th className="px-2 py-2 font-medium">Segments</th>
                </tr>
              </thead>
              <tbody>
                {batchResult.components.map((row, idx) => (
                  <tr key={`batch-${idx}`} className="border-b border-border/60 last:border-0">
                    <td className="px-2 py-2.5">{row.heading || "—"}</td>
                    <td className="px-2 py-2.5 text-xs">{row.fullyAllotted ? "Fully allotted" : row.message}</td>
                    <td className="px-2 py-2.5">
                      {row.allottedMeters.toLocaleString()} / {row.requiredMeters.toLocaleString()}
                    </td>
                    <td className="px-2 py-2.5">{row.proposedSegments.length}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}

        {previewResult && previewResult.displacements.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">
              Orders to shift ({previewResult.displacements.length}) — applied on confirm save
            </h4>
            <div className="max-h-48 overflow-y-auto rounded-lg border border-border/60">
              <table className="w-full min-w-[720px] text-left text-sm">
                <thead>
                  <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                    <th className="px-2 py-2 font-medium">Loom</th>
                    <th className="px-2 py-2 font-medium">Order</th>
                    <th className="px-2 py-2 font-medium">From</th>
                    <th className="px-2 py-2 font-medium">To</th>
                  </tr>
                </thead>
                <tbody>
                  {previewResult.displacements.map((d, idx) => (
                    <tr key={`disp-${idx}`} className="border-b border-border/60 last:border-0">
                      <td className="px-2 py-2.5">L{d.loomNo}</td>
                      <td className="px-2 py-2.5">{d.orderNo}</td>
                      <td className="px-2 py-2.5 whitespace-nowrap">
                        {formatPlanDate(d.fromDate)} → {formatPlanDate(d.toDate)}
                      </td>
                      <td className="px-2 py-2.5 whitespace-nowrap">
                        {formatPlanDate(d.newFromDate)} → {formatPlanDate(d.newToDate)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ) : null}

        {previewResult && previewResult.proposedSegments.length > 0 ? (
          <div className="mt-4 overflow-x-auto">
            <h4 className="mb-2 text-sm font-medium">Proposed loom segments</h4>
            <table className="w-full min-w-[800px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Loom</th>
                  <th className="px-2 py-2 font-medium">From</th>
                  <th className="px-2 py-2 font-medium">To</th>
                  <th className="px-2 py-2 font-medium">Meters</th>
                  <th className="px-2 py-2 font-medium">m/day</th>
                  <th className="px-2 py-2 font-medium">Case</th>
                </tr>
              </thead>
              <tbody>
                {previewResult.proposedSegments.map((seg, idx) => (
                  <tr key={`seg-${idx}`} className="border-b border-border/60 last:border-0">
                    <td className="px-2 py-2.5">L{seg.loomNo}</td>
                    <td className="px-2 py-2.5">{formatPlanDate(seg.fromDate)}</td>
                    <td className="px-2 py-2.5">{formatPlanDate(seg.toDate)}</td>
                    <td className="px-2 py-2.5">{seg.plannedMeters.toLocaleString()} m</td>
                    <td className="px-2 py-2.5">{seg.metersPerDay.toLocaleString()}</td>
                    <td className="px-2 py-2.5 text-xs text-muted-foreground">{seg.caseLabel}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </PlanningPanel>

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Save loom plan to ERP?</AlertDialogTitle>
            <AlertDialogDescription>
              {previewResult?.displacements.length ? (
                <>
                  Shift {previewResult.displacements.length} blocking order(s), then insert{" "}
                  {previewResult.proposedSegments.length} row(s) into Prod_LoomAlocationMaster for{" "}
                  <span className="font-medium">{heading || planOrderNo.trim() || "—"}</span>
                  {replaceExisting ? " (replace all existing loom rows for this order)" : " (append this component)"}.
                  The server re-runs preview before saving.
                </>
              ) : (
                <>
                  Insert {previewResult?.proposedSegments.length ?? 0} row(s) into Prod_LoomAlocationMaster for{" "}
                  <span className="font-medium">{heading || planOrderNo.trim() || "—"}</span>
                  {replaceExisting ? " (replace all existing loom rows for this order)" : " (append this component)"}.
                  The server re-runs preview before saving.
                </>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirm} disabled={confirmMutation.isPending}>
              {confirmMutation.isPending ? "Saving…" : "Confirm & save"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={confirmAllOpen} onOpenChange={setConfirmAllOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Save all loom fabrics to ERP?</AlertDialogTitle>
            <AlertDialogDescription>
              Confirms each loom-eligible BOM heading in sequence (append). Later fabrics see earlier saves. Headings
              that already have loom rows are skipped. This does not replace the whole order plan.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmAllMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmAllComponents} disabled={confirmAllMutation.isPending}>
              {confirmAllMutation.isPending ? "Saving…" : "Confirm all & save"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

function filterSameUnitLoomWarnings(
  warnings: string[],
  weavingCompany: string,
  fibcCompany: string,
): string[] {
  if (!weavingCompany || !fibcCompany || weavingCompany === fibcCompany) {
    return warnings.filter(
      (w) =>
        !/^Inter-unit:/i.test(w) &&
        !/sister-unit weave/i.test(w) &&
        !/Sulzer fabric/i.test(w),
    );
  }
  return warnings;
}

function LoomPreviewSummary({
  result,
  weavingCompany,
  fibcCompany,
}: {
  result: LoomAllotmentResult;
  weavingCompany: string;
  fibcCompany: string;
}) {
  const visibleWarnings = filterSameUnitLoomWarnings(result.warnings, weavingCompany, fibcCompany);
  return (
    <div className="space-y-2 text-sm">
      <p className={result.success ? "text-foreground" : "text-destructive"}>{result.message}</p>
      <dl className="grid grid-cols-2 gap-x-3 gap-y-1 text-xs">
        <dt className="text-muted-foreground">Allotted</dt>
        <dd>
          {result.allottedMeters.toLocaleString()} / {result.requiredMeters.toLocaleString()} m
        </dd>
        <dt className="text-muted-foreground">Fabric complete by</dt>
        <dd>{formatPlanDate(result.fabricCompletionDate)}</dd>
        <dt className="text-muted-foreground">Avg m/day</dt>
        <dd>{result.metersPerDay.toLocaleString()}</dd>
        <dt className="text-muted-foreground">Fully allotted</dt>
        <dd>{result.fullyAllotted ? "Yes" : "No"}</dd>
      </dl>
      {visibleWarnings.length > 0 ? (
        <ul className="mt-2 space-y-1 text-xs text-amber-700 dark:text-amber-300">
          {visibleWarnings.map((w, i) => (
            <li key={i}>• {w}</li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

const ProductionMetersPanel = memo(function ProductionMetersPanel({
  grid,
  loading,
}: {
  grid?: LoomProductionMeterGridResult;
  loading: boolean;
}) {
  return (
    <PlanningPanel
      title="Production meters"
      subtitle="vw_Loom_Prod_Mtr — shift A/B actual output"
      className="mb-6"
    >
      {loading ? (
        <div className="flex items-center gap-2 py-4 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading production data…
        </div>
      ) : !grid?.items.length ? (
        <p className="text-sm text-muted-foreground">No production meter rows in this date range.</p>
      ) : (
        <div className="max-h-64 overflow-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="sticky top-0 bg-card">
              <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                <th className="px-2 py-2 font-medium">Date</th>
                <th className="px-2 py-2 font-medium">Loom</th>
                <th className="px-2 py-2 font-medium">Order</th>
                <th className="px-2 py-2 font-medium">Shift A m</th>
                <th className="px-2 py-2 font-medium">Shift B m</th>
                <th className="px-2 py-2 font-medium">GSM</th>
              </tr>
            </thead>
            <tbody>
              {grid.items.slice(0, 200).map((row, idx) => (
                <tr key={`prod-${idx}`} className="border-b border-border/60 last:border-0">
                  <td className="px-2 py-2.5 whitespace-nowrap">{formatPlanDate(row.planDate)}</td>
                  <td className="px-2 py-2.5">L{row.loomNo}</td>
                  <td className="px-2 py-2.5">{row.orderNo ?? "—"}</td>
                  <td className="px-2 py-2.5">{row.prodMetersA.toLocaleString()}</td>
                  <td className="px-2 py-2.5">{row.prodMetersB.toLocaleString()}</td>
                  <td className="px-2 py-2.5">{row.reqGsm ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {grid.items.length > 200 ? (
            <p className="mt-2 text-xs text-muted-foreground">Showing first 200 of {grid.items.length} rows.</p>
          ) : null}
        </div>
      )}
    </PlanningPanel>
  );
});
