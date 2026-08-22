import { useMemo, useState, type ReactNode } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { Download, Factory, Loader2, Upload } from "lucide-react";
import { toast } from "sonner";
import {
  downloadDailyProductionExcel,
  formatMoney,
  formatQty,
  processDailyProductionExcel,
  type DailyProductionCurrencyTotal,
  type DailyProductionProcessResponse,
} from "@/lib/daily-production-api";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

export const Route = createFileRoute("/_app/daily-production")({
  head: () => ({ meta: [{ title: "Daily Production — PO Portal" }] }),
  component: DailyProductionPage,
});

function DailyProductionPage() {
  const [file, setFile] = useState<File | null>(null);
  const [processing, setProcessing] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [result, setResult] = useState<DailyProductionProcessResponse | null>(null);

  const summary = result?.summary;
  const topIcos = useMemo(
    () => (summary?.byIco ?? []).filter((r) => r.priced).slice(0, 12),
    [summary],
  );
  const lowIcos = useMemo(() => {
    const priced = (summary?.byIco ?? []).filter((r) => r.priced && r.valuePerKg != null);
    return [...priced].sort((a, b) => (a.valuePerKg ?? 0) - (b.valuePerKg ?? 0)).slice(0, 8);
  }, [summary]);

  async function process() {
    if (!file) {
      toast.error("Choose the Daily Production .xlsx first.");
      return;
    }
    setProcessing(true);
    try {
      const next = await processDailyProductionExcel(file);
      setResult(next);
      toast.success(`Priced ${next.summary.pricedIcoCount} of ${next.summary.uniqueIcoCount} ICOs`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Process failed");
    } finally {
      setProcessing(false);
    }
  }

  async function download() {
    if (!result) return;
    setDownloading(true);
    try {
      await downloadDailyProductionExcel(result.downloadToken, result.fileName);
      toast.success("Excel downloaded");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Daily Production</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Upload the Daily Production workbook. Process looks up ERP sales price by{" "}
          <span className="font-medium text-foreground">ICO#</span>, converts it to{" "}
          <span className="font-medium text-foreground">INR</span> using RBI forex for{" "}
          <span className="font-medium text-foreground">today (upload date)</span>, and writes Sales
          Price, Currency, and Sales Value on the production tab.
        </p>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
        <label
          className={cn(
            "flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border border-dashed px-4 py-8 text-center transition",
            file ? "border-primary/40 bg-primary/5" : "border-border hover:bg-secondary/40",
          )}
        >
          <Upload className="h-5 w-5 text-muted-foreground" />
          <div className="text-sm font-medium">{file ? file.name : "Drop or choose .xlsx"}</div>
          <div className="text-xs text-muted-foreground">Same layout as the HPBL daily production report</div>
          <input
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            className="sr-only"
            onChange={(e) => {
              const next = e.target.files?.[0] ?? null;
              setFile(next);
              setResult(null);
            }}
          />
        </label>
        <div className="mt-4 flex flex-wrap gap-2">
          <Button type="button" onClick={() => void process()} disabled={!file || processing}>
            {processing ? <Loader2 className="h-4 w-4 animate-spin" /> : <Factory className="h-4 w-4" />}
            Process
          </Button>
          <Button type="button" variant="outline" onClick={() => void download()} disabled={!result || downloading}>
            {downloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download Excel
          </Button>
        </div>
      </div>

      {summary ? (
        <>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <Stat label="Production rows" value={formatQty(summary.productionRows)} />
            <Stat
              label="ICOs priced"
              value={`${formatQty(summary.pricedIcoCount)} / ${formatQty(summary.uniqueIcoCount)}`}
            />
            <Stat label="Total pcs" value={formatQty(summary.totalPcs)} />
            <Stat label="Total kg" value={formatQty(summary.totalKgs, 1)} />
            <Stat label="Sales value (INR)" value={`₹ ${formatMoney(summary.totalSalesValueInr)}`} />
          </div>

          {summary.fx ? (
            <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-sm font-semibold">Forex used (INR)</h2>
              <p className="mt-1 text-xs text-muted-foreground">
                Converted as of {summary.fx.asOf}
                {summary.fx.rateFrom ? ` · RBI row from ${summary.fx.rateFrom}` : ""}
                {summary.fx.rateTo ? ` to ${summary.fx.rateTo}` : ""}
                {summary.fx.usedFallback ? " · no row for today, used latest earlier rate" : ""}
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                <FxPill label="USD" value={summary.fx.dollar} />
                <FxPill label="EUR" value={summary.fx.euro} />
                <FxPill label="GBP" value={summary.fx.pound} />
                <FxPill label="CHF" value={summary.fx.chf} />
              </div>
            </div>
          ) : null}

          {summary.currencyTotals.length > 0 ? (
            <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-sm font-semibold">Sales value (INR)</h2>
              <p className="mt-1 text-xs text-muted-foreground">All invoice currencies converted to INR.</p>
              <div className="mt-3 flex flex-wrap gap-2">
                {summary.currencyTotals.map((c) => (
                  <div key={c.currency} className="rounded-lg border border-border bg-secondary/40 px-3 py-2 text-sm">
                    <div className="text-xs text-muted-foreground">{c.currency}</div>
                    <div className="font-semibold tabular-nums">₹ {formatMoney(c.salesValue)}</div>
                  </div>
                ))}
              </div>
            </div>
          ) : null}

          {summary.hints.length > 0 ? (
            <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-sm font-semibold">What to look at</h2>
              <ul className="mt-2 list-disc space-y-1.5 pl-5 text-sm text-muted-foreground">
                {summary.hints.map((hint) => (
                  <li key={hint}>{hint}</li>
                ))}
              </ul>
            </div>
          ) : null}

          <Section title="By line" subtitle="Sewing-line totals in INR, so lines can be compared.">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="text-xs text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 font-medium">Line</th>
                  <th className="px-3 py-2 font-medium">Pcs</th>
                  <th className="px-3 py-2 font-medium">Kg</th>
                  <th className="px-3 py-2 font-medium">Priced rows</th>
                  <th className="px-3 py-2 font-medium">Sales value</th>
                </tr>
              </thead>
              <tbody>
                {summary.byLine.map((row) => (
                  <tr key={row.lineNo} className="border-t border-border">
                    <td className="px-3 py-2 font-medium">{row.lineNo}</td>
                    <td className="px-3 py-2 tabular-nums">{formatQty(row.pcs)}</td>
                    <td className="px-3 py-2 tabular-nums">{formatQty(row.kgs, 1)}</td>
                    <td className="px-3 py-2 tabular-nums">
                      {row.pricedRows}/{row.pricedRows + row.unpricedRows}
                    </td>
                    <td className="px-3 py-2">{formatCurrencies(row.currencies)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>

          <Section
            title="By bag weight range"
            subtitle="Same bands as the range sheet in the workbook, now with selling value."
          >
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="text-xs text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 font-medium">kg / pc</th>
                  <th className="px-3 py-2 font-medium">Pcs</th>
                  <th className="px-3 py-2 font-medium">Kg</th>
                  <th className="px-3 py-2 font-medium">Share of pcs</th>
                  <th className="px-3 py-2 font-medium">Sales value</th>
                </tr>
              </thead>
              <tbody>
                {summary.byWeightBand.map((row) => (
                  <tr key={row.band} className="border-t border-border">
                    <td className="px-3 py-2 font-medium">{row.band}</td>
                    <td className="px-3 py-2 tabular-nums">{formatQty(row.pcs)}</td>
                    <td className="px-3 py-2 tabular-nums">{formatQty(row.kgs, 1)}</td>
                    <td className="px-3 py-2 tabular-nums">
                      {summary.totalPcs > 0 ? `${((row.pcs / summary.totalPcs) * 100).toFixed(1)}%` : "—"}
                    </td>
                    <td className="px-3 py-2">{formatCurrencies(row.currencies)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>

          <Section
            title="Highest value per kg"
            subtitle="Priced ICOs ranked by INR sales value / kg."
          >
            <IcoTable rows={topIcos} />
          </Section>

          <Section title="Lowest value per kg" subtitle="Candidates to delay or move if a higher-value bag can run on that line.">
            <IcoTable rows={lowIcos} />
          </Section>

          {summary.unpriced.length > 0 ? (
            <Section title="Unpriced ICOs" subtitle="No Rate in Vw_MarketingInvoice for this ICO#.">
              <table className="w-full min-w-[480px] text-left text-sm">
                <thead className="text-xs text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 font-medium">ICO</th>
                    <th className="px-3 py-2 font-medium">P.O.No</th>
                    <th className="px-3 py-2 font-medium">Consignee</th>
                    <th className="px-3 py-2 font-medium">Pcs</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.unpriced.map((row) => (
                    <tr key={row.ico} className="border-t border-border">
                      <td className="px-3 py-2 font-medium">{row.ico || "—"}</td>
                      <td className="px-3 py-2">{row.poNo || "—"}</td>
                      <td className="px-3 py-2">{row.consignee || "—"}</td>
                      <td className="px-3 py-2 tabular-nums">{formatQty(row.pcs)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Section>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border bg-card px-4 py-3 shadow-sm">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
    </div>
  );
}

function Section({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <h2 className="text-sm font-semibold">{title}</h2>
      <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p>
      <div className="mt-3 overflow-x-auto">{children}</div>
    </div>
  );
}

function formatCurrencies(items: DailyProductionCurrencyTotal[]): string {
  if (!items.length) return "—";
  return items.map((c) => `₹ ${formatMoney(c.salesValue)}`).join(" · ");
}

function FxPill({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-border bg-secondary/40 px-3 py-2 text-sm">
      <div className="text-xs text-muted-foreground">1 {label}</div>
      <div className="font-semibold tabular-nums">₹ {formatMoney(value)}</div>
    </div>
  );
}

function IcoTable({
  rows,
}: {
  rows: DailyProductionProcessResponse["summary"]["byIco"];
}) {
  if (rows.length === 0) {
    return <p className="text-sm text-muted-foreground">No priced ICOs.</p>;
  }
  return (
    <table className="w-full min-w-[880px] text-left text-sm">
      <thead className="text-xs text-muted-foreground">
        <tr>
          <th className="px-3 py-2 font-medium">ICO</th>
          <th className="px-3 py-2 font-medium">Consignee</th>
          <th className="px-3 py-2 font-medium">Line</th>
          <th className="px-3 py-2 font-medium">kg/pc</th>
          <th className="px-3 py-2 font-medium">Pcs</th>
          <th className="px-3 py-2 font-medium">Price (INR)</th>
          <th className="px-3 py-2 font-medium">ERP rate</th>
          <th className="px-3 py-2 font-medium">Value / kg (INR)</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.ico} className="border-t border-border">
            <td className="px-3 py-2 font-medium">{row.ico}</td>
            <td className="px-3 py-2">{row.consignee || "—"}</td>
            <td className="px-3 py-2">{row.lineNos || "—"}</td>
            <td className="px-3 py-2 tabular-nums">{row.weightPerPc ? formatQty(row.weightPerPc, 3) : "—"}</td>
            <td className="px-3 py-2 tabular-nums">{formatQty(row.pcs)}</td>
            <td className="px-3 py-2 tabular-nums">
              {row.priced ? `₹ ${formatMoney(row.salesPrice)}` : "—"}
            </td>
            <td className="px-3 py-2 tabular-nums text-muted-foreground">
              {row.priced && row.originalCurrency
                ? `${row.originalCurrency} ${formatMoney(row.originalPrice)}`
                : "—"}
            </td>
            <td className="px-3 py-2 tabular-nums">
              {row.priced ? `₹ ${formatMoney(row.valuePerKg)}` : "—"}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
