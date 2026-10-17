import { useMemo, useRef, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { FileSpreadsheet, FileText, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  downloadGstItc04Csv,
  downloadGstItc04Excel,
  getGstItc04,
  itc04PeriodOptions,
  type GstItc04Registration,
  type GstItc04Table4Row,
  type GstItc04Table5Row,
} from "@/lib/gst-itc04-api";

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const qtyFmt = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 0, maximumFractionDigits: 2 });

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20";

const TH = "px-2.5 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground";
const TD = "px-2.5 py-1.5 text-right tabular-nums";

type SheetTab = "summary" | "table4" | "table5a";

export function GstItc04({ period, onPeriodChange }: { period: string; onPeriodChange: (period: string) => void }) {
  const [gstin, setGstin] = useState("");
  const [sheet, setSheet] = useState<SheetTab>("summary");
  const [refreshToken, setRefreshToken] = useState(0);
  const [downloading, setDownloading] = useState<"excel" | "csv" | null>(null);
  const bypassCacheRef = useRef(false);
  const periods = useMemo(() => itc04PeriodOptions(), []);

  const query = useQuery({
    queryKey: ["gst-itc04", period, gstin, refreshToken],
    queryFn: () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getGstItc04(period, gstin || undefined, refresh);
    },
    staleTime: 10 * 60_000,
    placeholderData: keepPreviousData,
    retry: 0,
  });
  const data = query.data;
  const regs = data?.registrations ?? [];

  async function onDownload(kind: "excel" | "csv") {
    setDownloading(kind);
    try {
      const name =
        kind === "csv"
          ? await downloadGstItc04Csv(period, gstin || undefined)
          : await downloadGstItc04Excel(period, gstin || undefined);
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(null);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3 rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="w-56 space-y-1.5">
          <Label htmlFor="itc04-period">Tax period</Label>
          <select
            id="itc04-period"
            value={period}
            onChange={(e) => e.target.value && onPeriodChange(e.target.value)}
            className={SELECT_CLASS}
          >
            {!periods.some((p) => p.value === period) && <option value={period}>{period}</option>}
            {periods.map((p) => (
              <option key={p.value} value={p.value}>
                {p.label}
              </option>
            ))}
          </select>
        </div>
        <div className="w-72 space-y-1.5">
          <Label htmlFor="itc04-gstin">Manufacturer GSTIN</Label>
          <select
            id="itc04-gstin"
            value={gstin}
            onChange={(e) => setGstin(e.target.value)}
            className={SELECT_CLASS}
            disabled={!data}
          >
            <option value="">All GSTINs (combined view)</option>
            {regs.map((r) => (
              <option key={r.gstin} value={r.gstin}>
                {r.code} · {r.gstin}
              </option>
            ))}
          </select>
        </div>
        <div className="ml-auto flex flex-wrap gap-2">
          <Button
            variant="outline"
            onClick={() => {
              bypassCacheRef.current = true;
              setRefreshToken((t) => t + 1);
            }}
            disabled={query.isFetching}
          >
            {query.isFetching ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
            Refresh from ERP
          </Button>
          <Button variant="outline" onClick={() => onDownload("csv")} disabled={!!downloading || !data}>
            {downloading === "csv" ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FileText className="mr-2 h-4 w-4" />}
            Download CSV
          </Button>
          <Button onClick={() => onDownload("excel")} disabled={!!downloading || !data}>
            {downloading === "excel" ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FileSpreadsheet className="mr-2 h-4 w-4" />}
            Download Excel
          </Button>
        </div>
        <p className="w-full text-xs text-muted-foreground">
          From the ERP Job Work section: goods sent (<b>Despatch Challan 5A</b>) and goods received back (
          <b>Challan 5A Inward</b>). Units under the same GSTIN are combined, matching the ITC-04 offline utility layout
          (Table 4 / Table 5A). Paste into the official utility to generate the JSON for the GST portal.
        </p>
      </div>

      {query.isError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
          {(query.error as Error).message}
        </div>
      )}

      {!data && query.isFetching && (
        <div className="flex items-center gap-2 rounded-xl border border-border bg-card p-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Reading job-work challans from the ERP…
        </div>
      )}

      {data && (
        <>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Tile label="Period" value={data.periodLabel} hint={`${data.from} → ${data.to}`} />
            <Tile label="Registrations" value={String(regs.length)} hint="GSTINs with job-work movement" />
            <Tile
              label="Table 4 · Sent"
              value={`${data.table4.length} rows`}
              hint={`Qty ${qtyFmt.format(regs.reduce((s, r) => s + r.sentQty, 0))} · ₹ ${money.format(regs.reduce((s, r) => s + r.sentValue, 0))}`}
              tone="red"
            />
            <Tile
              label="Table 5A · Received"
              value={`${data.table5A.length} rows`}
              hint={`Qty ${qtyFmt.format(regs.reduce((s, r) => s + r.receivedQty, 0))}`}
              tone="green"
            />
          </div>

          <div className="flex w-full gap-1 rounded-xl border border-border bg-muted/40 p-1 sm:w-fit" role="tablist">
            {(
              [
                ["summary", "By GSTIN"],
                ["table4", "Table 4 · Sent"],
                ["table5a", "Table 5A · Received"],
              ] as const
            ).map(([key, label]) => (
              <button
                key={key}
                type="button"
                role="tab"
                aria-selected={sheet === key}
                onClick={() => setSheet(key)}
                className={cn(
                  "rounded-lg px-3.5 py-1.5 text-sm font-medium transition-colors",
                  sheet === key ? "bg-card text-primary shadow-sm" : "text-muted-foreground hover:text-foreground",
                )}
              >
                {label}
              </button>
            ))}
          </div>

          {sheet === "summary" && <SummaryTable rows={regs} />}
          {sheet === "table4" && <Table4Grid rows={data.table4} />}
          {sheet === "table5a" && <Table5Grid rows={data.table5A} />}
        </>
      )}
    </div>
  );
}

function Tile({
  label,
  value,
  hint,
  tone,
}: {
  label: string;
  value: string;
  hint: string;
  tone?: "red" | "green";
}) {
  return (
    <div
      className={cn(
        "rounded-xl border border-l-4 border-border bg-card px-4 py-3 shadow-sm",
        tone === "red" && "border-l-red-500",
        tone === "green" && "border-l-emerald-500",
        !tone && "border-l-primary",
      )}
    >
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-semibold tabular-nums">{value}</p>
      <p className="mt-0.5 text-[11px] text-muted-foreground">{hint}</p>
    </div>
  );
}

function SummaryTable({ rows }: { rows: GstItc04Registration[] }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-card shadow-sm">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            <th className={cn(TH, "text-left")}>Co</th>
            <th className={cn(TH, "text-left")}>GSTIN</th>
            <th className={cn(TH, "text-left")}>Units</th>
            <th className={TH}>Table 4</th>
            <th className={TH}>Sent qty</th>
            <th className={TH}>Sent value</th>
            <th className={TH}>Table 5A</th>
            <th className={TH}>Received qty</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.gstin} className="border-b border-border/60 even:bg-muted/20">
              <td className="px-2.5 py-1.5 font-semibold">{r.code}</td>
              <td className="px-2.5 py-1.5 font-mono text-xs text-muted-foreground">{r.gstin}</td>
              <td className="max-w-[280px] truncate px-2.5 py-1.5 text-xs text-muted-foreground" title={r.units.join("\n")}>
                {r.units.join(", ")}
              </td>
              <td className={TD}>{r.sentRows}</td>
              <td className={TD}>{qtyFmt.format(r.sentQty)}</td>
              <td className={TD}>{money.format(r.sentValue)}</td>
              <td className={TD}>{r.receivedRows}</td>
              <td className={TD}>{qtyFmt.format(r.receivedQty)}</td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td colSpan={8} className="px-4 py-8 text-center text-muted-foreground">
                No job-work challans in this period.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function Table4Grid({ rows }: { rows: GstItc04Table4Row[] }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-t-4 border-border border-t-red-500 bg-card shadow-sm">
      <div className="border-b border-border px-4 py-3">
        <h3 className="text-sm font-semibold">Table 4 — Goods sent to job worker</h3>
        <p className="text-xs text-muted-foreground">{rows.length} rows · ITC-04 offline utility columns</p>
      </div>
      <div className="max-h-[560px] overflow-auto">
        <table className="w-full min-w-[1400px] text-sm">
          <thead className="sticky top-0 bg-card">
            <tr className="border-b border-border bg-secondary/40">
              <th className={cn(TH, "text-left")}>Co</th>
              <th className={cn(TH, "text-left")}>JW GSTIN</th>
              <th className={cn(TH, "text-left")}>JW name</th>
              <th className={cn(TH, "text-left")}>Type</th>
              <th className={cn(TH, "text-left")}>Challan</th>
              <th className={TH}>Date</th>
              <th className={cn(TH, "text-left")}>Goods</th>
              <th className={cn(TH, "text-left")}>Description</th>
              <th className={TH}>UQC</th>
              <th className={TH}>Qty</th>
              <th className={TH}>Taxable value</th>
              <th className={TH}>IGST%</th>
              <th className={TH}>CGST%</th>
              <th className={TH}>SGST%</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={`${r.challanNo}|${r.description}|${i}`} className="border-b border-border/40 even:bg-muted/20">
                <td className="px-2.5 py-1.5 font-semibold">{r.code}</td>
                <td className="px-2.5 py-1.5 font-mono text-xs">{r.jobWorkerGstin || "—"}</td>
                <td className="max-w-[160px] truncate px-2.5 py-1.5" title={r.jobWorkerName}>
                  {r.jobWorkerName}
                </td>
                <td className="px-2.5 py-1.5 text-xs">{r.jobWorkerType}</td>
                <td className="px-2.5 py-1.5 font-mono text-xs">{r.challanNo}</td>
                <td className={TD}>{r.challanDate}</td>
                <td className="px-2.5 py-1.5 text-xs">{r.goodsType}</td>
                <td className="max-w-[180px] truncate px-2.5 py-1.5" title={r.description}>
                  {r.description}
                </td>
                <td className={TD}>{r.uqc}</td>
                <td className={TD}>{qtyFmt.format(r.quantity)}</td>
                <td className={TD}>{money.format(r.taxableValue)}</td>
                <td className={TD}>{r.igstRate}</td>
                <td className={TD}>{r.cgstRate}</td>
                <td className={TD}>{r.sgstRate}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Table5Grid({ rows }: { rows: GstItc04Table5Row[] }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-t-4 border-border border-t-emerald-500 bg-card shadow-sm">
      <div className="border-b border-border px-4 py-3">
        <h3 className="text-sm font-semibold">Table 5A — Goods received back from same job worker</h3>
        <p className="text-xs text-muted-foreground">{rows.length} rows · linked to original send challan</p>
      </div>
      <div className="max-h-[560px] overflow-auto">
        <table className="w-full min-w-[1400px] text-sm">
          <thead className="sticky top-0 bg-card">
            <tr className="border-b border-border bg-secondary/40">
              <th className={cn(TH, "text-left")}>Co</th>
              <th className={cn(TH, "text-left")}>JW GSTIN</th>
              <th className={cn(TH, "text-left")}>JW name</th>
              <th className={cn(TH, "text-left")}>Original challan</th>
              <th className={TH}>Orig. date</th>
              <th className={cn(TH, "text-left")}>JW challan</th>
              <th className={TH}>JW date</th>
              <th className={cn(TH, "text-left")}>Nature</th>
              <th className={cn(TH, "text-left")}>Description</th>
              <th className={TH}>UQC</th>
              <th className={TH}>Qty</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={`${r.inwardNo}|${r.originalChallanNo}|${i}`} className="border-b border-border/40 even:bg-muted/20">
                <td className="px-2.5 py-1.5 font-semibold">{r.code}</td>
                <td className="px-2.5 py-1.5 font-mono text-xs">{r.jobWorkerGstin || "—"}</td>
                <td className="max-w-[160px] truncate px-2.5 py-1.5" title={r.jobWorkerName}>
                  {r.jobWorkerName}
                </td>
                <td className="px-2.5 py-1.5 font-mono text-xs">{r.originalChallanNo}</td>
                <td className={TD}>{r.originalChallanDate || "—"}</td>
                <td className="px-2.5 py-1.5 font-mono text-xs">{r.jobWorkerChallanNo || "—"}</td>
                <td className={TD}>{r.jobWorkerChallanDate || "—"}</td>
                <td className="max-w-[140px] truncate px-2.5 py-1.5 text-xs" title={r.natureOfJob}>
                  {r.natureOfJob || "Job Work"}
                </td>
                <td className="max-w-[180px] truncate px-2.5 py-1.5" title={r.description}>
                  {r.description}
                </td>
                <td className={TD}>{r.uqc}</td>
                <td className={TD}>{qtyFmt.format(r.quantity)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
