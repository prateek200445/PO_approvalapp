import { useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { FileBarChart, FileSpreadsheet, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  cmaAssets,
  cmaLiabilities,
  cmaMeta,
  cmaOperating,
  downloadCmaExcel,
  faceTotals,
  formatCma,
  type CmaLine,
  type CmaYear,
} from "@/lib/cma-data";

export const Route = createFileRoute("/_app/cma")({
  head: () => ({ meta: [{ title: "CMA — PO Portal" }] }),
  component: CmaPage,
});

const TABS = [
  { id: "assets", label: "Form III — Assets" },
  { id: "liab", label: "Form III — Liabilities" },
  { id: "ops", label: "Form II — Operating" },
] as const;

function CmaPage() {
  const [tab, setTab] = useState<(typeof TABS)[number]["id"]>("assets");
  const [downloading, setDownloading] = useState(false);
  const face = faceTotals();
  const lines = tab === "assets" ? cmaAssets : tab === "liab" ? cmaLiabilities : cmaOperating;
  const mappedTotal =
    tab === "assets"
      ? cmaAssets.find((l) => l.id === "assets-total")
      : tab === "liab"
        ? cmaLiabilities.find((l) => l.id === "liab-total")
        : cmaOperating.find((l) => l.id === "pat");
  const faceRow = tab === "ops" ? face.pat : face.assets;

  function onDownloadExcel() {
    setDownloading(true);
    try {
      const name = downloadCmaExcel();
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex items-center gap-2 text-primary">
            <FileBarChart className="h-5 w-5" />
            <p className="text-xs font-semibold uppercase tracking-wide">CMA</p>
          </div>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">
            Credit Monitoring Arrangement
          </h1>
          <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
            {cmaMeta.company} standalone figures from the March-26 financial statements, filled
            into the bank CMA forms. Amounts in {cmaMeta.unit}. 2026 was still an estimate in the
            CMA workbook; both years here are taken from the audited FS.
          </p>
        </div>
        <Button variant="outline" size="sm" disabled={downloading} onClick={onDownloadExcel}>
          {downloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileSpreadsheet className="h-4 w-4" />}
          Excel
        </Button>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <SummaryCard title="Total assets / liabilities" a={face.assets.y2025} b={face.assets.y2026} />
        <SummaryCard title="Net worth" a={face.netWorth.y2025} b={face.netWorth.y2026} />
        <SummaryCard title="PAT" a={face.pat.y2025} b={face.pat.y2026} />
      </div>

      <div className="flex flex-wrap gap-1.5 rounded-xl border border-border bg-card p-1">
        {TABS.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={cn(
              "flex-1 rounded-lg px-3 py-2 text-sm font-medium",
              tab === item.id ? "bg-primary text-primary-foreground" : "hover:bg-accent",
            )}
          >
            {item.label}
          </button>
        ))}
      </div>

      <section className="overflow-x-auto rounded-2xl border border-border bg-card shadow-sm">
        <table className="w-full min-w-[40rem] border-collapse text-sm">
          <caption className="border-b border-border px-4 py-3 text-left text-sm font-semibold">
            {TABS.find((t) => t.id === tab)?.label} · year ending 31 March · {cmaMeta.unit}
          </caption>
          <thead>
            <tr className="bg-[#0B3A5B] text-white">
              <th className="w-16 border border-[#083049] px-2 py-2 font-semibold">Sl.</th>
              <th className="border border-[#083049] px-3 py-2 text-left font-semibold">Particulars</th>
              {cmaMeta.years.map((y) => (
                <th key={y.key} className="border border-[#083049] px-3 py-2 text-right font-semibold">
                  {y.label}
                  <div className="text-[11px] font-normal text-white/80">
                    {y.status} · {y.asAt}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {lines.map((row) => (
              <CmaRow key={row.id} row={row} />
            ))}
          </tbody>
        </table>
      </section>

      <p className="text-xs text-muted-foreground">
        Mapped {tab === "ops" ? "PAT" : "total"} {formatCma(mappedTotal?.y2026 ?? null)} Cr (2026) vs
        FS face {formatCma(faceRow.y2026)} Cr. Differences of a few paise are rounding from lakh to
        crore. Receivables are not split export/domestic in the FS; EDFS is shown on its own CMA
        line. 2025 opening WIP/FG on Form II use the prior CMA close because the March-24 stock
        notes were not in this FS pack.
      </p>
    </div>
  );
}

function SummaryCard({ title, a, b }: { title: string; a: number; b: number }) {
  return (
    <div className="card-3d rounded-2xl p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</p>
      <div className="mt-2 grid grid-cols-2 gap-2 text-sm">
        <div>
          <p className="text-muted-foreground">2025 Aud</p>
          <p className="text-lg font-semibold tabular-nums">{formatCma(a)}</p>
        </div>
        <div>
          <p className="text-muted-foreground">2026 Aud</p>
          <p className="text-lg font-semibold tabular-nums">{formatCma(b)}</p>
        </div>
      </div>
    </div>
  );
}

function CmaRow({ row }: { row: CmaLine }) {
  const years: CmaYear[] = ["y2025", "y2026"];
  return (
    <tr
      className={cn(
        row.kind === "head" && "bg-slate-100 font-semibold",
        row.kind === "total" && "bg-amber-200/80 font-semibold text-slate-900",
        row.kind === "line" && "odd:bg-background even:bg-muted/20",
        row.kind === "sub" && "text-muted-foreground",
      )}
    >
      <td className="border border-border px-2 py-1.5 text-center text-xs">{row.sl ?? ""}</td>
      <td className={cn("border border-border px-3 py-1.5", row.kind === "sub" && "pl-8")}>
        <span>{row.label}</span>
        {row.note ? <span className="mt-0.5 block text-[11px] font-normal text-muted-foreground">{row.note}</span> : null}
      </td>
      {years.map((y) => (
        <td key={y} className="border border-border px-3 py-1.5 text-right tabular-nums">
          {row.kind === "head" && row[y] == null ? "" : formatCma(row[y])}
        </td>
      ))}
    </tr>
  );
}
