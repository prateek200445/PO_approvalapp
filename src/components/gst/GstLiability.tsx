import { Fragment, useEffect, useRef, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronDown, ChevronRight, FileSpreadsheet, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  downloadGstLiabilityExcel,
  getGstLiability,
  type GstLiabilityBalance,
  type GstLiabilityRegistration,
} from "@/lib/gst-liability-api";

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => money.format(n);

type Balances = Record<string, { igst: string; cgst: string; sgst: string }>;

const storageKey = (month: string) => `gst-liability-balances:${month}`;

function loadBalances(month: string): Balances {
  try {
    return JSON.parse(localStorage.getItem(storageKey(month)) || "{}") as Balances;
  } catch {
    return {};
  }
}

const num = (s: string | undefined) => {
  const v = Number((s ?? "").replace(/,/g, ""));
  return Number.isFinite(v) ? v : 0;
};

const outputTotal = (r: GstLiabilityRegistration) => r.outputIgst + r.outputCgst + r.outputSgst;
const inputTotal = (r: GstLiabilityRegistration) => r.inputIgst + r.inputCgst + r.inputSgst;

const TH = "px-2.5 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground";
const TD = "px-2.5 py-1.5 text-right tabular-nums";

function Amount({ value, strong }: { value: number; strong?: boolean }) {
  return (
    <span className={cn(value < 0 && "text-red-600 dark:text-red-400", strong && "font-semibold")}>{fmt(value)}</span>
  );
}

export function GstLiability({ month, onMonthChange }: { month: string; onMonthChange: (month: string) => void }) {
  const [excludeSetOff, setExcludeSetOff] = useState(true);
  const [refreshToken, setRefreshToken] = useState(0);
  const [balances, setBalances] = useState<Balances>(() => loadBalances(month));
  const [included, setIncluded] = useState<Record<string, boolean>>({});
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [downloading, setDownloading] = useState(false);
  const bypassCacheRef = useRef(false);

  useEffect(() => setBalances(loadBalances(month)), [month]);

  const query = useQuery({
    queryKey: ["gst-liability", month, excludeSetOff, refreshToken],
    queryFn: () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getGstLiability(month, excludeSetOff, refresh);
    },
    staleTime: 10 * 60_000,
    placeholderData: keepPreviousData,
    retry: 0,
  });
  const data = query.data;

  const isIncluded = (r: GstLiabilityRegistration) => included[r.gstin] ?? r.known;
  const rows = data?.registrations ?? [];
  const active = rows.filter(isIncluded);

  function setBalance(gstin: string, field: "igst" | "cgst" | "sgst", value: string) {
    setBalances((prev) => {
      const current = prev[gstin] ?? { igst: "", cgst: "", sgst: "" };
      const next = { ...prev, [gstin]: { ...current, [field]: value } };
      localStorage.setItem(storageKey(month), JSON.stringify(next));
      return next;
    });
  }

  const balanceOf = (gstin: string) => {
    const b = balances[gstin];
    return { igst: num(b?.igst), cgst: num(b?.cgst), sgst: num(b?.sgst) };
  };
  const net = (r: GstLiabilityRegistration) => {
    const b = balanceOf(r.gstin);
    return outputTotal(r) + inputTotal(r) + b.igst + b.cgst + b.sgst;
  };

  const totals = { oI: 0, oC: 0, oS: 0, iI: 0, iC: 0, iS: 0, bI: 0, bC: 0, bS: 0, net: 0 };
  for (const r of active) {
    const b = balanceOf(r.gstin);
    totals.oI += r.outputIgst;
    totals.oC += r.outputCgst;
    totals.oS += r.outputSgst;
    totals.iI += r.inputIgst;
    totals.iC += r.inputCgst;
    totals.iS += r.inputSgst;
    totals.bI += b.igst;
    totals.bC += b.cgst;
    totals.bS += b.sgst;
    totals.net += net(r);
  }

  async function onDownload() {
    setDownloading(true);
    try {
      const payload: GstLiabilityBalance[] = active.map((r) => ({ gstin: r.gstin, ...balanceOf(r.gstin) }));
      const name = await downloadGstLiabilityExcel({
        month,
        excludeSetOff,
        gstins: active.map((r) => r.gstin),
        balances: payload,
      });
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3 rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="space-y-1.5">
          <Label htmlFor="gst-liability-month">Month</Label>
          <Input
            id="gst-liability-month"
            type="month"
            value={month}
            onChange={(e) => e.target.value && onMonthChange(e.target.value)}
            className="w-44"
          />
        </div>
        <label className="flex h-9 items-center gap-2 text-sm">
          <input
            type="checkbox"
            className="h-4 w-4 accent-primary"
            checked={excludeSetOff}
            onChange={(e) => setExcludeSetOff(e.target.checked)}
          />
          Before month-end set-off
        </label>
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
          <Button onClick={onDownload} disabled={downloading || !data || active.length === 0}>
            {downloading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FileSpreadsheet className="mr-2 h-4 w-4" />}
            Download Excel
          </Button>
        </div>
        <p className="w-full text-xs text-muted-foreground">
          Closing balances of the IGST / CGST / SGST output and input ledgers for each GST registration, from ERP
          postings.{" "}
          {excludeSetOff
            ? "Month-end set-off journals (entries through GST Payable / GST Receivable / “For GST Balance Only”) are left out, so this shows the liability before set-off."
            : "Set-off journals are included, so balances match the ERP Group Summary after set-off."}{" "}
          Enter the cash / credit ledger balance (IGST / CGST / SGST A/c) to get the net payable. Negative = payable.
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
          Reading GST ledger postings from the ERP — the first load for a month takes up to a minute.
        </div>
      )}

      {data && (
        <div className="overflow-x-auto rounded-xl border border-border bg-card shadow-sm">
          <table className="w-full min-w-[1280px] text-sm">
            <thead>
              <tr className="border-b border-border">
                <th colSpan={2} />
                <th colSpan={4} className="bg-red-50 px-2 py-1.5 text-center text-xs font-semibold dark:bg-red-950/30">
                  Output
                </th>
                <th colSpan={4} className="bg-emerald-50 px-2 py-1.5 text-center text-xs font-semibold dark:bg-emerald-950/30">
                  Input
                </th>
                <th colSpan={3} className="bg-indigo-50 px-2 py-1.5 text-center text-xs font-semibold dark:bg-indigo-950/30">
                  Balance (cash / credit ledger)
                </th>
                <th className="px-2 py-1.5 text-center text-xs font-semibold">Net</th>
              </tr>
              <tr className="border-b border-border bg-secondary/40">
                <th className={cn(TH, "text-left")}>Co</th>
                <th className={cn(TH, "text-left")}>GSTIN</th>
                <th className={TH}>IGST</th>
                <th className={TH}>CGST</th>
                <th className={TH}>SGST</th>
                <th className={TH}>Total</th>
                <th className={TH}>IGST</th>
                <th className={TH}>CGST</th>
                <th className={TH}>SGST</th>
                <th className={TH}>Total</th>
                <th className={TH}>IGST A/c</th>
                <th className={TH}>CGST A/c</th>
                <th className={TH}>SGST A/c</th>
                <th className={TH}>Payable (−) / Bal (+)</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => {
                const on = isIncluded(r);
                const open = expanded[r.gstin];
                const ledgers = data.ledgers.filter((l) => l.gstin === r.gstin);
                return (
                  <Fragment key={r.gstin}>
                    <tr className={cn("border-b border-border/60", !on && "opacity-45")}>
                      <td className="px-2.5 py-1.5">
                        <div className="flex items-center gap-1.5">
                          <input
                            type="checkbox"
                            className="h-3.5 w-3.5 accent-primary"
                            checked={on}
                            title="Include in totals and Excel"
                            onChange={(e) => setIncluded((p) => ({ ...p, [r.gstin]: e.target.checked }))}
                          />
                          <button
                            type="button"
                            className="inline-flex items-center gap-0.5 font-semibold hover:text-primary"
                            onClick={() => setExpanded((p) => ({ ...p, [r.gstin]: !p[r.gstin] }))}
                          >
                            {open ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
                            {r.code}
                          </button>
                        </div>
                      </td>
                      <td className="px-2.5 py-1.5 font-mono text-xs text-muted-foreground" title={r.units.join("\n")}>
                        {r.gstin}
                      </td>
                      <td className={TD}><Amount value={r.outputIgst} /></td>
                      <td className={TD}><Amount value={r.outputCgst} /></td>
                      <td className={TD}><Amount value={r.outputSgst} /></td>
                      <td className={TD}><Amount value={outputTotal(r)} strong /></td>
                      <td className={TD}><Amount value={r.inputIgst} /></td>
                      <td className={TD}><Amount value={r.inputCgst} /></td>
                      <td className={TD}><Amount value={r.inputSgst} /></td>
                      <td className={TD}><Amount value={inputTotal(r)} strong /></td>
                      {(["igst", "cgst", "sgst"] as const).map((f) => (
                        <td key={f} className="px-1.5 py-1">
                          <Input
                            inputMode="decimal"
                            value={balances[r.gstin]?.[f] ?? ""}
                            placeholder="0.00"
                            onChange={(e) => setBalance(r.gstin, f, e.target.value)}
                            className="h-8 w-28 text-right tabular-nums"
                          />
                        </td>
                      ))}
                      <td className={TD}><Amount value={net(r)} strong /></td>
                    </tr>
                    {open && (
                      <tr className="border-b border-border/60 bg-muted/30">
                        <td colSpan={14} className="px-4 py-3">
                          <p className="mb-2 text-xs text-muted-foreground">
                            {r.name} · {r.units.length} unit{r.units.length === 1 ? "" : "s"}
                            {r.setOffExcluded > 0 && ` · set-off entries left out: ${fmt(r.setOffExcluded)}`}
                          </p>
                          <table className="w-full text-xs">
                            <thead>
                              <tr className="text-muted-foreground">
                                <th className="py-1 text-left font-medium">Unit</th>
                                <th className="py-1 text-left font-medium">Ledger</th>
                                <th className="py-1 text-right font-medium">Opening</th>
                                <th className="py-1 text-right font-medium">Debit</th>
                                <th className="py-1 text-right font-medium">Credit</th>
                                <th className="py-1 text-right font-medium">Closing</th>
                                <th className="py-1 text-right font-medium">Set-off left out</th>
                              </tr>
                            </thead>
                            <tbody>
                              {ledgers.map((l) => (
                                <tr key={`${l.unit}|${l.ledger}`} className="border-t border-border/40">
                                  <td className="py-1 pr-3">{l.unit}</td>
                                  <td className="py-1 pr-3">{l.ledger}</td>
                                  <td className="py-1 text-right tabular-nums"><Amount value={l.opening} /></td>
                                  <td className="py-1 text-right tabular-nums">{fmt(l.debit)}</td>
                                  <td className="py-1 text-right tabular-nums">{fmt(l.credit)}</td>
                                  <td className="py-1 text-right tabular-nums"><Amount value={l.closing} strong /></td>
                                  <td className="py-1 text-right tabular-nums text-muted-foreground">
                                    {l.setOff ? fmt(l.setOff) : "–"}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
            <tfoot>
              <tr className="border-t border-border bg-secondary/40 font-semibold">
                <td className="px-2.5 py-2" colSpan={2}>
                  Grand total ({active.length})
                </td>
                <td className={TD}><Amount value={totals.oI} /></td>
                <td className={TD}><Amount value={totals.oC} /></td>
                <td className={TD}><Amount value={totals.oS} /></td>
                <td className={TD}><Amount value={totals.oI + totals.oC + totals.oS} strong /></td>
                <td className={TD}><Amount value={totals.iI} /></td>
                <td className={TD}><Amount value={totals.iC} /></td>
                <td className={TD}><Amount value={totals.iS} /></td>
                <td className={TD}><Amount value={totals.iI + totals.iC + totals.iS} strong /></td>
                <td className={TD}><Amount value={totals.bI} /></td>
                <td className={TD}><Amount value={totals.bC} /></td>
                <td className={TD}><Amount value={totals.bS} /></td>
                <td className={TD}><Amount value={totals.net} strong /></td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  );
}
