import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, Factory, Layers, Loader2, Search, Sparkles, Save, Truck, ArrowLeftRight, TriangleAlert } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { PlanningPageHeader, PlanningPageShell, PlanningPanel } from "@/components/planning/planning-ui";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  confirmFullOrderPlan,
  fetchIntegratedOrderTimeline,
  previewFullOrderPlan,
} from "@/lib/planning/integrated-api";
import {
  formatTimelineDate,
  type FullOrderPlan,
  type IntegratedOrderTimeline,
  type IntegratedTimelineMilestone,
} from "@/lib/planning/integrated-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/planning/timeline/")({
  head: () => ({ meta: [{ title: "Planning Timeline — PO Portal" }] }),
  component: IntegratedPlanningPage,
});

const stageStyles: Record<string, { ring: string; bg: string; icon: typeof Factory }> = {
  Loom: { ring: "ring-amber-500/30", bg: "bg-amber-500/15 text-amber-700 dark:text-amber-300", icon: Factory },
  Transfer: { ring: "ring-orange-500/30", bg: "bg-orange-500/15 text-orange-700 dark:text-orange-300", icon: ArrowLeftRight },
  FabricReady: { ring: "ring-emerald-500/30", bg: "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300", icon: Layers },
  Fibc: { ring: "ring-sky-500/30", bg: "bg-sky-500/15 text-sky-700 dark:text-sky-300", icon: Layers },
  Dispatch: { ring: "ring-violet-500/30", bg: "bg-violet-500/15 text-violet-700 dark:text-violet-300", icon: Truck },
};

function IntegratedPlanningPage() {
  const queryClient = useQueryClient();
  const [orderNo, setOrderNo] = useState("");
  const [searchOrder, setSearchOrder] = useState<string | null>(null);

  const { data, isLoading, isFetching, isError, error } = useQuery({
    queryKey: ["integrated-timeline", searchOrder],
    queryFn: () => fetchIntegratedOrderTimeline(searchOrder!),
    enabled: Boolean(searchOrder),
    retry: false,
  });

  useEffect(() => {
    if (!isError || !error) return;
    toast.error((error as Error).message || "Failed to load timeline.");
  }, [isError, error]);

  const handleSearch = () => {
    const trimmed = orderNo.trim();
    if (!trimmed) {
      toast.error("Enter an order number.");
      return;
    }
    setSearchOrder(trimmed);
  };

  return (
    <PlanningPageShell>
      <PlanningPageHeader
        title="Integrated planning timeline"
        description="Enter an order: the planner times FIBC sewing from dispatch, then weaves every BOM fabric to arrive before sewing."
        backTo="/profile"
      />

      <PlanningPanel title="Look up order" subtitle="Buyer order / PO number" className="mb-6">
        <div className="flex flex-wrap gap-2">
          <Input
            value={orderNo}
            onChange={(e) => setOrderNo(e.target.value)}
            placeholder="Order number"
            className="max-w-sm"
            onKeyDown={(e) => {
              if (e.key === "Enter") handleSearch();
            }}
          />
          <Button type="button" onClick={handleSearch} disabled={isFetching}>
            {isFetching ? <Loader2 className="mr-1 h-4 w-4 animate-spin" /> : <Search className="mr-1 h-4 w-4" />}
            Load timeline
          </Button>
        </div>
      </PlanningPanel>

      {searchOrder ? (
        <FullOrderPlanPanel
          orderNo={searchOrder}
          onSaved={() => {
            void queryClient.invalidateQueries({ queryKey: ["integrated-timeline", searchOrder] });
          }}
        />
      ) : null}

      {isLoading ? (
        <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading planning data…
        </div>
      ) : isError ? (
        <PlanningPanel title="Could not load timeline">
          <p className="text-sm text-destructive">{(error as Error).message || "Could not load timeline."}</p>
        </PlanningPanel>
      ) : data ? (
        <TimelineView timeline={data} />
      ) : searchOrder ? (
        <PlanningPanel>
          <p className="text-sm text-muted-foreground">No planning data found for {searchOrder}.</p>
        </PlanningPanel>
      ) : null}
    </PlanningPageShell>
  );
}

function TimelineView({ timeline }: { timeline: IntegratedOrderTimeline }) {
  return (
    <div className="space-y-6">
      <PlanningPanel title={`Order ${timeline.orderNo}`} subtitle={timeline.partyName ?? "—"}>
        <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 text-sm">
          <div>
            <dt className="text-xs text-muted-foreground">Bag type</dt>
            <dd className="font-medium">{timeline.bagType ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">Quantity</dt>
            <dd className="font-medium">{timeline.quantity?.toLocaleString() ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">Marketing no.</dt>
            <dd className="font-medium">{timeline.marketingNo ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">Fabric buffer</dt>
            <dd className="font-medium">{timeline.fabricBufferDays} days before FIBC</dd>
          </div>
          {timeline.isInterUnit ? (
            <>
              <div>
                <dt className="text-xs text-muted-foreground">Weaving factory</dt>
                <dd className="font-medium">{timeline.fabricSupplyCompanyName ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">FIBC factory</dt>
                <dd className="font-medium">{timeline.fibcCompanyName ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Transfer buffer</dt>
                <dd className="font-medium">{timeline.transferBufferDays} days</dd>
              </div>
            </>
          ) : null}
        </dl>

        {timeline.warnings.length > 0 ? (
          <ul className="mt-4 space-y-1 rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 text-xs text-amber-900 dark:text-amber-200">
            {timeline.warnings.map((w, i) => (
              <li key={i}>• {w}</li>
            ))}
          </ul>
        ) : null}
      </PlanningPanel>

      <PlanningPanel title="End-to-end timeline" subtitle="Key milestones in production sequence">
        <MilestoneRail milestones={timeline.milestones} />
      </PlanningPanel>

      <div className="grid gap-6 lg:grid-cols-2">
        <PlanningPanel
          title="Loom allocations"
          subtitle={`${timeline.loomAllocations.length} segment(s)`}
          headerRight={
            <Link to="/planning/loom" className="text-xs text-primary hover:underline">
              Open loom planning
            </Link>
          }
        >
          {timeline.loomAllocations.length === 0 ? (
            <p className="text-sm text-muted-foreground">No saved loom rows for this order.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[520px] text-left text-sm">
                <thead>
                  <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                    <th className="px-2 py-2 font-medium">Loom</th>
                    <th className="px-2 py-2 font-medium">From</th>
                    <th className="px-2 py-2 font-medium">To</th>
                    <th className="px-2 py-2 font-medium">GSM / width</th>
                  </tr>
                </thead>
                <tbody>
                  {timeline.loomAllocations.map((row, idx) => (
                    <tr key={idx} className="border-b border-border/60 last:border-0">
                      <td className="px-2 py-2.5">L{row.loomNo}</td>
                      <td className="px-2 py-2.5">{formatTimelineDate(row.allocationDate)}</td>
                      <td className="px-2 py-2.5">{formatTimelineDate(row.toDate)}</td>
                      <td className="px-2 py-2.5 text-xs text-muted-foreground">
                        {row.reqGsm ?? "—"} / {row.size ?? "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </PlanningPanel>

        <PlanningPanel
          title="FIBC line plan"
          subtitle={`${timeline.fibcPlanLines.length} slot(s)`}
          headerRight={
            <Link to="/planning/fibc" className="text-xs text-primary hover:underline">
              Open FIBC planning
            </Link>
          }
        >
          {timeline.fibcPlanLines.length === 0 ? (
            <p className="text-sm text-muted-foreground">No FIBC plan lines for this order.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[520px] text-left text-sm">
                <thead>
                  <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                    <th className="px-2 py-2 font-medium">Line</th>
                    <th className="px-2 py-2 font-medium">Date</th>
                    <th className="px-2 py-2 font-medium">Shift</th>
                    <th className="px-2 py-2 font-medium">Qty</th>
                  </tr>
                </thead>
                <tbody>
                  {timeline.fibcPlanLines.slice(0, 12).map((row, idx) => (
                    <tr key={idx} className="border-b border-border/60 last:border-0">
                      <td className="px-2 py-2.5">{row.lineNo}</td>
                      <td className="px-2 py-2.5">{formatTimelineDate(row.planDate)}</td>
                      <td className="px-2 py-2.5">{row.shift || "—"}</td>
                      <td className="px-2 py-2.5">{row.qty.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {timeline.fibcPlanLines.length > 12 ? (
                <p className="mt-2 text-xs text-muted-foreground">+ {timeline.fibcPlanLines.length - 12} more slot(s)</p>
              ) : null}
            </div>
          )}
        </PlanningPanel>
      </div>

      {timeline.bomComponents.length > 0 ? (
        <PlanningPanel
          title="BOM component readiness"
          subtitle="Loom fabrics vs accessories from Vw_Bom_PPC"
        >
          <div className="overflow-x-auto">
            <table className="w-full min-w-[960px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Heading</th>
                  <th className="px-2 py-2 font-medium">Category</th>
                  <th className="px-2 py-2 font-medium">Readiness</th>
                  <th className="px-2 py-2 font-medium">Supply</th>
                  <th className="px-2 py-2 font-medium">Due</th>
                  <th className="px-2 py-2 font-medium">Meters / kg</th>
                  <th className="px-2 py-2 font-medium">Detail</th>
                </tr>
              </thead>
              <tbody>
                {timeline.bomComponents
                  .filter((row) => row.readiness !== "Ignored")
                  .map((row, idx) => (
                    <tr key={idx} className="border-b border-border/60 last:border-0">
                      <td className="px-2 py-2.5">{row.heading}</td>
                      <td className="px-2 py-2.5 text-xs">{row.category}</td>
                      <td className="px-2 py-2.5">
                        <span
                          className={cn(
                            "rounded-full px-2 py-0.5 text-xs font-medium",
                            row.readiness === "Planned" && "bg-emerald-500/15 text-emerald-800 dark:text-emerald-300",
                            row.readiness === "Ready" && "bg-emerald-500/15 text-emerald-800 dark:text-emerald-300",
                            row.readiness === "Unplanned" && "bg-amber-500/15 text-amber-800 dark:text-amber-300",
                            row.readiness === "Partial" && "bg-amber-500/15 text-amber-800 dark:text-amber-300",
                            row.readiness === "Indented" && "bg-sky-500/15 text-sky-800 dark:text-sky-300",
                            row.readiness === "Accessory" && "bg-sky-500/15 text-sky-800 dark:text-sky-300",
                            row.readiness === "NotApplicable" && "bg-muted text-muted-foreground",
                          )}
                        >
                          {row.readiness}
                        </span>
                      </td>
                      <td className="px-2 py-2.5 text-xs">
                        {row.supplyCompanyName ?? "—"}
                        {row.isInterUnit ? (
                          <span className="ml-1 text-amber-800 dark:text-amber-200">(+{row.transferBufferDays}d)</span>
                        ) : null}
                      </td>
                      <td className="px-2 py-2.5 text-xs whitespace-nowrap">{formatTimelineDate(row.dueDate)}</td>
                      <td className="px-2 py-2.5 text-xs text-muted-foreground">
                        {row.totalMtr != null ? `${row.totalMtr.toLocaleString()} m` : "—"}
                        {row.totalKg != null ? ` / ${row.totalKg.toLocaleString()} kg` : ""}
                      </td>
                      <td className="px-2 py-2.5 text-xs text-muted-foreground">{row.detail ?? "—"}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </PlanningPanel>
      ) : null}

      {timeline.fabricRequirements.length > 0 ? (
        <PlanningPanel title="BOM fabric requirements" subtitle="From Vw_Bom_PPC">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-2 py-2 font-medium">Heading</th>
                  <th className="px-2 py-2 font-medium">GSM</th>
                  <th className="px-2 py-2 font-medium">Width</th>
                  <th className="px-2 py-2 font-medium">Meters</th>
                  <th className="px-2 py-2 font-medium">Target</th>
                </tr>
              </thead>
              <tbody>
                {timeline.fabricRequirements.map((row, idx) => (
                  <tr key={idx} className="border-b border-border/60 last:border-0">
                    <td className="px-2 py-2.5">{row.heading || "—"}</td>
                    <td className="px-2 py-2.5">{row.gsm || "—"}</td>
                    <td className="px-2 py-2.5">{row.fabricSize?.toLocaleString() ?? "—"}</td>
                    <td className="px-2 py-2.5">{row.totalMtr?.toLocaleString() ?? "—"}</td>
                    <td className="px-2 py-2.5">{formatTimelineDate(row.targetDate)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </PlanningPanel>
      ) : null}
    </div>
  );
}

function MilestoneRail({ milestones }: { milestones: IntegratedTimelineMilestone[] }) {
  const ordered = [...milestones].sort((a, b) => a.sortOrder - b.sortOrder);

  if (ordered.length === 0) {
    return <p className="text-sm text-muted-foreground">No milestones could be derived from ERP data.</p>;
  }

  return (
    <div className="overflow-x-auto pb-2">
      <div className="flex min-w-max items-stretch gap-0">
        {ordered.map((milestone, index) => {
          const style = stageStyles[milestone.stage] ?? stageStyles.Loom;
          const Icon = style.icon;
          const dateLabel =
            milestone.startDate === milestone.endDate || !milestone.endDate
              ? formatTimelineDate(milestone.startDate)
              : `${formatTimelineDate(milestone.startDate)} – ${formatTimelineDate(milestone.endDate)}`;

          return (
            <div key={`${milestone.stage}-${index}`} className="flex items-center">
              <div
                className={cn(
                  "flex w-44 flex-col rounded-xl border border-border/70 p-4 ring-2",
                  style.ring,
                  "bg-card/90",
                )}
              >
                <div className={cn("mb-3 flex h-9 w-9 items-center justify-center rounded-lg", style.bg)}>
                  <Icon className="h-4 w-4" />
                </div>
                <div className="text-sm font-semibold leading-tight">{milestone.label}</div>
                <div className="mt-1 text-xs text-muted-foreground">{dateLabel}</div>
                {milestone.detail ? (
                  <div className="mt-2 text-[11px] leading-snug text-muted-foreground">{milestone.detail}</div>
                ) : null}
              </div>
              {index < ordered.length - 1 ? (
                <ArrowRight className="mx-2 h-4 w-4 shrink-0 text-muted-foreground/60" />
              ) : null}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function FullOrderPlanPanel({ orderNo, onSaved }: { orderNo: string; onSaved: () => void }) {
  const [plan, setPlan] = useState<FullOrderPlan | null>(null);
  const [replaceFibc, setReplaceFibc] = useState(false);

  useEffect(() => {
    setPlan(null);
  }, [orderNo]);

  const previewMutation = useMutation({
    mutationFn: () => previewFullOrderPlan(orderNo),
    onSuccess: (result) => {
      setPlan(result);
      if (result.readyToConfirm) toast.success(result.message);
      else toast.message(result.message || "Plan has blockers.");
    },
    onError: (err: Error) => toast.error(err.message || "Full-order preview failed."),
  });

  const confirmMutation = useMutation({
    mutationFn: () => confirmFullOrderPlan(orderNo, replaceFibc),
    onSuccess: (result) => {
      setPlan(result);
      if (result.saved) {
        toast.success(result.message);
        onSaved();
      } else toast.error(result.message);
    },
    onError: (err: Error) => toast.error(err.message || "Full-order confirm failed."),
  });

  return (
    <PlanningPanel
      title="Plan entire order"
      subtitle="FIBC sewing from dispatch → every BOM fabric timed to arrive before sewing"
      className="mb-6"
    >
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" onClick={() => previewMutation.mutate()} disabled={previewMutation.isPending}>
          {previewMutation.isPending ? <Loader2 className="mr-1 h-4 w-4 animate-spin" /> : <Sparkles className="mr-1 h-4 w-4" />}
          Preview full plan
        </Button>
        <Button
          type="button"
          disabled={!plan?.readyToConfirm || confirmMutation.isPending}
          onClick={() => confirmMutation.mutate()}
        >
          {confirmMutation.isPending ? <Loader2 className="mr-1 h-4 w-4 animate-spin" /> : <Save className="mr-1 h-4 w-4" />}
          Confirm & save to ERP
        </Button>
        <label className="flex items-center gap-2 text-xs text-muted-foreground">
          <input
            type="checkbox"
            checked={replaceFibc}
            onChange={(e) => setReplaceFibc(e.target.checked)}
            className="h-4 w-4 rounded border-border"
          />
          Replace existing FIBC slots
        </label>
      </div>

      {plan ? (
        <div className="mt-4 space-y-3">
          <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 text-sm">
            <div>
              <dt className="text-xs text-muted-foreground">Loom fabrics</dt>
              <dd className="font-medium">
                {plan.loomFullyAllotted}/{plan.loomEligible} allotted
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">FIBC slots</dt>
              <dd className="font-medium">{plan.fibcSlots}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Fabric at FIBC</dt>
              <dd className="font-medium">{formatTimelineDate(plan.fabricAtFibcDate)}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Sewing</dt>
              <dd className="font-medium">
                {formatTimelineDate(plan.fibcStartDate)} → {formatTimelineDate(plan.fibcEndDate)}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Dispatch</dt>
              <dd className="font-medium">{formatTimelineDate(plan.dispatchDate)}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Sequence</dt>
              <dd className="font-medium">{plan.sequenceOk ? "Fabric before sewing" : "Broken"}</dd>
            </div>
          </dl>
          {plan.blockers.length > 0 ? (
            <ul className="space-y-1 text-sm text-destructive">
              {plan.blockers.map((b) => (
                <li key={b} className="flex gap-2">
                  <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                  {b}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-emerald-700 dark:text-emerald-300">{plan.message}</p>
          )}
          {plan.warnings.slice(0, 8).map((w) => (
            <p key={w} className="text-xs text-amber-800 dark:text-amber-200">
              {w}
            </p>
          ))}
        </div>
      ) : null}
    </PlanningPanel>
  );
}
