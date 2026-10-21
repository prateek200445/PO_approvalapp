import { useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Loader2, Save } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth-context";
import {
  getCustomReportColumns,
  getCustomReportSaved,
  getCustomReportSources,
  runCustomReport,
  saveCustomReport,
  type CustomReportResult,
} from "@/lib/custom-report-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/custom-report")({
  head: () => ({ meta: [{ title: "All Type of Report — PO Portal" }] }),
  component: CustomReportPage,
});

function CustomReportPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";
  const qc = useQueryClient();

  const [tableName, setTableName] = useState("");
  const [reportName, setReportName] = useState("");
  const [columns, setColumns] = useState<string[]>([]);
  const [selectedIdx, setSelectedIdx] = useState<number | null>(null);
  const [sourceFilter, setSourceFilter] = useState("");
  const [result, setResult] = useState<CustomReportResult | null>(null);

  const sourcesQuery = useQuery({
    queryKey: ["custom-report-sources"],
    queryFn: getCustomReportSources,
    staleTime: 30 * 60_000,
  });

  const savedQuery = useQuery({
    queryKey: ["custom-report-saved", username],
    queryFn: () => getCustomReportSaved(username),
    staleTime: 5 * 60_000,
  });

  const filteredSources = useMemo(() => {
    const all = sourcesQuery.data ?? [];
    const q = sourceFilter.trim().toLowerCase();
    if (!q) return all;
    return all.filter((n) => n.toLowerCase().includes(q));
  }, [sourcesQuery.data, sourceFilter]);

  const savedForTable = useMemo(() => {
    const all = savedQuery.data ?? [];
    if (!tableName) return all;
    return all.filter((r) => r.tableName.toLowerCase() === tableName.toLowerCase());
  }, [savedQuery.data, tableName]);

  const loadColumnsForTable = async (table: string, preferred?: string[]) => {
    try {
      const all = await getCustomReportColumns(table);
      if (preferred?.length) {
        const selected = preferred
          .map((c) => all.find((a) => a.toLowerCase() === c.toLowerCase()))
          .filter((c): c is string => !!c);
        setColumns(selected.length ? selected : all);
      } else {
        setColumns(all);
      }
      setSelectedIdx(0);
      setResult(null);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Failed to load columns");
    }
  };

  const onTableChosen = (raw: string) => {
    const v = raw.trim();
    const match = (sourcesQuery.data ?? []).find((n) => n.toLowerCase() === v.toLowerCase());
    const next = match ?? v;
    setTableName(next);
    if (next) void loadColumnsForTable(next);
    else setColumns([]);
  };

  const applySaved = (name: string) => {
    setReportName(name);
    const hit = (savedQuery.data ?? []).find(
      (r) => r.reportName === name && (!tableName || r.tableName.toLowerCase() === tableName.toLowerCase()),
    );
    if (!hit) return;
    const wanted = hit.queryString
      .split(",")
      .map((c) => c.trim())
      .filter(Boolean);
    setTableName(hit.tableName);
    void loadColumnsForTable(hit.tableName, wanted);
  };

  const move = (dir: -1 | 1) => {
    if (selectedIdx == null) return;
    const next = selectedIdx + dir;
    if (next < 0 || next >= columns.length) return;
    const copy = [...columns];
    [copy[selectedIdx], copy[next]] = [copy[next], copy[selectedIdx]];
    setColumns(copy);
    setSelectedIdx(next);
  };

  const runMutation = useMutation({
    mutationFn: () => runCustomReport(tableName, columns),
    onSuccess: (data) => {
      setResult(data);
      toast.success(`Displaying ${data.rowCount} row(s)`);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      saveCustomReport({
        reportName,
        tableName,
        columns,
        username,
        isVisibleToAll: "no",
      }),
    onSuccess: async () => {
      toast.success("Report saved to ERP Reportall");
      await qc.invalidateQueries({ queryKey: ["custom-report-saved"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4 p-4 md:p-6">
      <div className="overflow-hidden rounded-lg border border-border bg-card shadow-sm">
        {/* ERP-style title bar */}
        <div className="bg-[#5B9BD5] px-4 py-2 text-center text-sm font-bold tracking-wide text-white md:text-base">
          ALL TYPE OF REPORT
        </div>

        <div className="space-y-4 p-4">
          <p className="text-xs text-muted-foreground">
            Report → Customize Report → Create · ERP.frmAllTypeofReport (MM) · MaterialProcessing.dbo.Reportall
          </p>

          <div className="flex flex-col gap-3 md:flex-row md:items-end">
            <div className="min-w-0 flex-1 space-y-1.5">
              <Label className="text-xs">Source table / view</Label>
              <Input
                list="custom-report-sources"
                value={tableName}
                onChange={(e) => setTableName(e.target.value)}
                onBlur={(e) => onTableChosen(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    onTableChosen((e.target as HTMLInputElement).value);
                  }
                }}
                placeholder={sourcesQuery.isLoading ? "Loading…" : "e.g. SubGroupMaster, PaymentAgstRef"}
                className="h-9 font-mono text-sm"
              />
              <datalist id="custom-report-sources">
                {filteredSources.slice(0, 400).map((n) => (
                  <option key={n} value={n} />
                ))}
              </datalist>
              <Input
                value={sourceFilter}
                onChange={(e) => setSourceFilter(e.target.value)}
                placeholder="Filter source list…"
                className="h-8 text-xs"
              />
            </div>

            <div className="min-w-0 flex-1 space-y-1.5">
              <Label className="text-xs">Report Name :</Label>
              <Input
                list="custom-report-names"
                value={reportName}
                onChange={(e) => setReportName(e.target.value)}
                onBlur={(e) => {
                  const v = e.target.value.trim();
                  const hit = (savedQuery.data ?? []).find((r) => r.reportName === v);
                  if (hit) applySaved(hit.reportName);
                }}
                placeholder="Select or type a report name"
                className="h-9 text-sm"
              />
              <datalist id="custom-report-names">
                {(tableName ? savedForTable : savedQuery.data ?? []).map((r) => (
                  <option key={`${r.username}-${r.reportName}-${r.tableName}`} value={r.reportName} />
                ))}
              </datalist>
            </div>
          </div>

          <div className="flex flex-col gap-3 md:flex-row">
            <div className="min-h-[220px] flex-1 overflow-hidden rounded border border-border bg-white">
              <ul className="h-[280px] overflow-y-auto font-mono text-sm">
                {columns.length === 0 ? (
                  <li className="px-3 py-6 text-center text-muted-foreground">
                    {tableName ? "No columns" : "Choose a source table to list fields"}
                  </li>
                ) : (
                  columns.map((col, i) => (
                    <li key={`${col}-${i}`}>
                      <button
                        type="button"
                        className={cn(
                          "w-full px-3 py-1.5 text-left hover:bg-sky-50",
                          selectedIdx === i && "bg-[#5B9BD5] text-white hover:bg-[#4A8AC4]",
                        )}
                        onClick={() => setSelectedIdx(i)}
                      >
                        {col}
                      </button>
                    </li>
                  ))
                )}
              </ul>
            </div>

            <div className="flex flex-row gap-2 md:w-40 md:flex-col">
              <Button
                type="button"
                variant="outline"
                className="h-10 gap-2 border-orange-300 bg-orange-50 text-orange-800 hover:bg-orange-100"
                disabled={selectedIdx == null || selectedIdx === 0}
                onClick={() => move(-1)}
              >
                <ArrowUp className="h-4 w-4 text-orange-500" />
                UP
              </Button>
              <Button
                type="button"
                variant="outline"
                className="h-10 gap-2 border-orange-300 bg-orange-50 text-orange-800 hover:bg-orange-100"
                disabled={selectedIdx == null || selectedIdx >= columns.length - 1}
                onClick={() => move(1)}
              >
                <ArrowDown className="h-4 w-4 text-orange-500" />
                Down
              </Button>
              <Button
                type="button"
                className="h-10 bg-[#5B9BD5] text-white hover:bg-[#4A8AC4]"
                disabled={!tableName || columns.length === 0 || runMutation.isPending}
                onClick={() => runMutation.mutate()}
              >
                {runMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                Display Report
              </Button>
              <Button
                type="button"
                variant="secondary"
                className="h-10 gap-2"
                disabled={!reportName.trim() || !tableName || columns.length === 0 || !username || saveMutation.isPending}
                onClick={() => saveMutation.mutate()}
              >
                {saveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                Save
              </Button>
            </div>
          </div>
        </div>
      </div>

      {result ? (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-border bg-card shadow-sm">
          <div className="flex items-center justify-between border-b border-border bg-muted/40 px-4 py-2">
            <div className="text-sm font-semibold">Display Alltypeof — {result.tableName}</div>
            <div className="text-xs text-muted-foreground">
              Count: {result.rowCount}
              {result.truncated ? " (truncated)" : ""}
            </div>
          </div>
          <div className="min-h-0 flex-1 overflow-auto">
            <table className="w-max min-w-full border-collapse text-xs">
              <thead className="sticky top-0 bg-[#5B9BD5] text-white">
                <tr>
                  {result.columns.map((c) => (
                    <th key={c} className="whitespace-nowrap border border-sky-600 px-2 py-1.5 text-left font-semibold">
                      {c}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {result.rows.map((row, ri) => (
                  <tr key={ri} className={ri % 2 === 0 ? "bg-white" : "bg-sky-50/40"}>
                    {result.columns.map((c) => (
                      <td key={c} className="whitespace-nowrap border border-border px-2 py-1 font-mono">
                        {formatCell(row[c])}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function formatCell(value: unknown): string {
  if (value == null) return "";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}
