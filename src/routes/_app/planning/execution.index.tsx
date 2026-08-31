import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import { PlanningPageHeader, PlanningPageShell, PlanningPanel } from "@/components/planning/planning-ui";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { fetchBailingReconciliation, fetchOrderExecution, fetchAccessoryMaterials } from "@/lib/planning/execution-api";
import { formatPlanDate } from "@/lib/planning/fibc-types";

const DEFAULT_COMPANY = "Plastene India Limited (Unit -II)";

export const Route = createFileRoute("/_app/planning/execution/")({
  head: () => ({ meta: [{ title: "Planning Execution — PO Portal" }] }),
  component: ExecutionPage,
});

function ExecutionPage() {
  const [orderNo, setOrderNo] = useState("");
  const [lookup, setLookup] = useState<string | null>(null);

  const { data: execution, isFetching, isError, error } = useQuery({
    queryKey: ["planning-execution", lookup, DEFAULT_COMPANY],
    queryFn: () => fetchOrderExecution(lookup!, DEFAULT_COMPANY),
    enabled: Boolean(lookup),
    retry: false,
  });

  const { data: bailing } = useQuery({
    queryKey: ["planning-bailing", lookup, DEFAULT_COMPANY],
    queryFn: () => fetchBailingReconciliation(lookup!, DEFAULT_COMPANY),
    enabled: Boolean(lookup) && Boolean(execution),
    retry: false,
  });

  const { data: accessories } = useQuery({
    queryKey: ["planning-accessories", lookup],
    queryFn: () => fetchAccessoryMaterials(lookup!),
    enabled: Boolean(lookup),
    retry: false,
  });

  useEffect(() => {
    if (!isError || !error) return;
    toast.error(error.message || "Failed to load execution data.");
  }, [isError, error]);

  return (
    <PlanningPageShell>
      <PlanningPageHeader
        title="Planning Execution"
        description="Planned vs produced vs bailed — replan suggestions from live ERP data."
        backTo="/profile"
      />

      <PlanningPanel title="Order lookup">
        <div className="flex max-w-md gap-2">
          <Input value={orderNo} onChange={(e) => setOrderNo(e.target.value)} placeholder="Order / PO number" />
          <Button type="button" onClick={() => setLookup(orderNo.trim() || null)}>
            <Search className="h-4 w-4" />
            Load
          </Button>
        </div>
      </PlanningPanel>

      {isFetching ? (
        <Loader2 className="mt-6 h-6 w-6 animate-spin text-muted-foreground" />
      ) : isError ? (
        <PlanningPanel title="Could not load order" className="mt-6">
          <p className="text-sm text-destructive">{(error as Error).message || "Execution lookup failed."}</p>
        </PlanningPanel>
      ) : execution ? (
        <div className="mt-6 grid gap-4 lg:grid-cols-2">
          <PlanningPanel title="Production vs plan">
            <dl className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <dt className="text-muted-foreground">Planned</dt>
                <dd className="text-lg font-semibold">{execution.plannedQty.toLocaleString()} pcs</dd>
              </div>
              <div>
                <dt className="text-muted-foreground">Produced</dt>
                <dd className="text-lg font-semibold">{execution.producedQty.toLocaleString()} pcs</dd>
              </div>
              <div>
                <dt className="text-muted-foreground">Bailed</dt>
                <dd className="text-lg font-semibold">{execution.bailedQty.toLocaleString()} pcs</dd>
              </div>
              <div>
                <dt className="text-muted-foreground">Pending</dt>
                <dd className="text-lg font-semibold">{execution.pendingQty.toLocaleString()} pcs</dd>
              </div>
            </dl>
            {execution.bailingGap > 0 ? (
              <p className="mt-3 text-xs text-muted-foreground">
                Bailing gap: {execution.bailingGap.toLocaleString()} pcs produced but not yet bailed.
              </p>
            ) : null}
            {execution.replanSuggestions.length > 0 ? (
              <ul className="mt-4 space-y-2 text-sm text-amber-800 dark:text-amber-200">
                {execution.replanSuggestions.map((s) => (
                  <li key={s} className="rounded-md border border-amber-500/30 bg-amber-500/10 px-3 py-2">
                    {s}
                  </li>
                ))}
              </ul>
            ) : null}
            {execution.backlogRowsAutoCleared ? (
              <p className="mt-3 text-sm text-emerald-700 dark:text-emerald-300">
                Cleared {execution.backlogRowsAutoCleared} backlog row(s) automatically.
              </p>
            ) : null}
          </PlanningPanel>

          <PlanningPanel title="Bailing reconciliation">
            {bailing ? (
              <div className="space-y-3 text-sm">
                <Badge variant={bailing.readyForDispatch ? "default" : "secondary"}>
                  {bailing.readyForDispatch ? "Dispatch-ready" : "Not ready"}
                </Badge>
                <p>{bailing.message}</p>
                <p className="text-muted-foreground">
                  Shortfall vs plan: {bailing.shortfall.toLocaleString()} pcs
                </p>
              </div>
            ) : null}
          </PlanningPanel>

          {execution.lineShiftTotals.length > 0 ? (
            <PlanningPanel title="Line + shift totals" className="lg:col-span-2">
              <p className="mb-3 text-xs text-muted-foreground">
                Planned vs produced by line and shift (ERP TeamNo maps to line number).
              </p>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">Shift</th>
                      <th className="px-2 py-2">Planned</th>
                      <th className="px-2 py-2">Produced</th>
                      <th className="px-2 py-2">Pending</th>
                    </tr>
                  </thead>
                  <tbody>
                    {execution.lineShiftTotals.map((l) => (
                      <tr key={`${l.lineNo}-${l.shift}`} className="border-b border-border/60">
                        <td className="px-2 py-1.5">{l.lineNo}</td>
                        <td className="px-2 py-1.5">{l.shift}</td>
                        <td className="px-2 py-1.5">{l.plannedQty.toLocaleString()}</td>
                        <td className="px-2 py-1.5">{l.producedQty.toLocaleString()}</td>
                        <td className="px-2 py-1.5">{l.pendingQty > 0 ? l.pendingQty.toLocaleString() : "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </PlanningPanel>
          ) : null}

          {execution.lines.length > 0 ? (
            <PlanningPanel title="Plan slots (by date)" className="lg:col-span-2">
              <p className="mb-3 text-xs text-muted-foreground">
                Planned slots from saved allocations. Produced on slot only when ERP date matches exactly.
              </p>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">Shift</th>
                      <th className="px-2 py-2">Plan date</th>
                      <th className="px-2 py-2">Planned</th>
                      <th className="px-2 py-2">Produced (same date)</th>
                      <th className="px-2 py-2">Backlog</th>
                    </tr>
                  </thead>
                  <tbody>
                    {execution.lines.map((l, i) => (
                      <tr key={`${l.lineNo}-${l.shift}-${l.planDate}-${i}`} className="border-b border-border/60">
                        <td className="px-2 py-1.5">{l.lineNo}</td>
                        <td className="px-2 py-1.5">{l.shift}</td>
                        <td className="px-2 py-1.5">{l.planDate ? formatPlanDate(l.planDate) : "—"}</td>
                        <td className="px-2 py-1.5">{l.plannedQty.toLocaleString()}</td>
                        <td className="px-2 py-1.5">{l.producedQty > 0 ? l.producedQty.toLocaleString() : "—"}</td>
                        <td className="px-2 py-1.5">{l.openBacklogQty > 0 ? l.openBacklogQty.toLocaleString() : "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </PlanningPanel>
          ) : null}

          {execution.productionEntries.length > 0 ? (
            <PlanningPanel title="ERP production log" className="lg:col-span-2">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Date</th>
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">Team</th>
                      <th className="px-2 py-2">Shift</th>
                      <th className="px-2 py-2">Produced</th>
                    </tr>
                  </thead>
                  <tbody>
                    {execution.productionEntries.map((entry, i) => (
                      <tr key={`${entry.prodDate}-${entry.lineNo}-${entry.shift}-${i}`} className="border-b border-border/60">
                        <td className="px-2 py-1.5">{formatPlanDate(entry.prodDate)}</td>
                        <td className="px-2 py-1.5">{entry.lineNo || "—"}</td>
                        <td className="px-2 py-1.5">{entry.teamNo || "—"}</td>
                        <td className="px-2 py-1.5">{entry.shift}</td>
                        <td className="px-2 py-1.5">{entry.quantity.toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </PlanningPanel>
          ) : null}
        </div>
      ) : lookup ? (
        <PlanningPanel title="No data" className="mt-6">
          <p className="text-sm text-muted-foreground">No execution data found for {lookup}.</p>
        </PlanningPanel>
      ) : null}

      {accessories && accessories.items.length > 0 ? (
        <PlanningPanel title="Accessory indent / MRN" className="mt-6" subtitle="Matched from Vw_StoreDeptt + Vw_StoreInwards by order number">
          {accessories.warnings.map((w) => (
            <p key={w} className="mb-2 text-xs text-amber-800 dark:text-amber-200">
              {w}
            </p>
          ))}
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead>
                <tr className="border-b border-border text-xs text-muted-foreground">
                  <th className="px-2 py-2">Heading</th>
                  <th className="px-2 py-2">Status</th>
                  <th className="px-2 py-2">Required</th>
                  <th className="px-2 py-2">Indent</th>
                  <th className="px-2 py-2">MRN</th>
                  <th className="px-2 py-2">Received</th>
                  <th className="px-2 py-2">Detail</th>
                </tr>
              </thead>
              <tbody>
                {accessories.items.map((row) => (
                  <tr key={row.heading} className="border-b border-border/60">
                    <td className="px-2 py-1.5">{row.heading}</td>
                    <td className="px-2 py-1.5">
                      <Badge variant={row.status === "Received" ? "default" : "secondary"}>{row.status}</Badge>
                    </td>
                    <td className="px-2 py-1.5 text-xs">
                      {row.requiredQty != null ? `${row.requiredQty.toLocaleString()} ${row.unit}` : "—"}
                    </td>
                    <td className="px-2 py-1.5 text-xs">{row.indentNo ?? "—"}</td>
                    <td className="px-2 py-1.5 text-xs">{row.mrnNo ?? "—"}</td>
                    <td className="px-2 py-1.5 text-xs">{row.receivedQty ? row.receivedQty.toLocaleString() : "—"}</td>
                    <td className="px-2 py-1.5 text-xs text-muted-foreground">{row.detail ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </PlanningPanel>
      ) : null}
    </PlanningPageShell>
  );
}
