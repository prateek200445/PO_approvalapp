import { useEffect, useRef, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { FileSpreadsheet, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  downloadGstLiabilityExcel,
  getGstLiability,
  type GstLiabilityBalance,
  type GstLiabilityCompany,
  type GstLiabilityLedgerRow,
} from "@/lib/gst-liability-api";

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => money.format(n);

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

type Balances = Record<string, { igst: string; cgst: string; sgst: string }>;
type SourceView = "output" | "input" | "other";

const storageKey = (month: string) => `gst-liability-balances-v2:${month}`;

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

const outputTotal = (c: GstLiabilityCompany) => c.outputIgst + c.outputCgst + c.outputSgst;
const inputTotal = (c: GstLiabilityCompany) => c.inputIgst + c.inputCgst + c.inputSgst;

const TH = "px-2.5 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground";
const TD = "px-2.5 py-1.5 text-right tabular-nums";
const GAP = "w-3 border-x border-border bg-muted/40 p-0";

function Amount({ value, strong }: { value: number; strong?: boolean }) {
  return (
    <span className={cn(value < 0 && "text-red-600 dark:text-red-400", strong && "font-semibold")}>{fmt(value)}</span>
  );
}

export function GstLiability({ month, onMonthChange }: { month: string; onMonthChange: (month: string) => void }) {
  const [company, setCompany] = useState("");
  const [excludeSetOff, setExcludeSetOff] = useState(false);
  const [refreshToken, setRefreshToken] = useState(0);
  const [balances, setBalances] = useState<Balances>(() => loadBalances(month));
  const [source, setSource] = useState<SourceView>("output");
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

  const companies = data?.companies ?? [];
  const known = companies.filter((c) => c.known);
  const others = companies.filter((c) => !c.known);
  const shown = company ? companies.filter((c) => c.code === company) : known;
  const shownCodes = new Set(shown.map((c) => c.code));
  const setOffPosted = shown.reduce((s, c) => s + c.setOff, 0);

  function setBalance(code: string, field: "igst" | "cgst" | "sgst", value: string) {
    setBalances((prev) => {
      const current = prev[code] ?? { igst: "", cgst: "", sgst: "" };
      const next = { ...prev, [code]: { ...current, [field]: value } };
      localStorage.setItem(storageKey(month), JSON.stringify(next));
      return next;
    });
  }

  const balanceOf = (code: string) => {
    const b = balances[code];
    return { igst: num(b?.igst), cgst: num(b?.cgst), sgst: num(b?.sgst) };
  };
  const net = (c: GstLiabilityCompany) => {
    const b = balanceOf(c.code);
    return outputTotal(c) + inputTotal(c) + b.igst + b.cgst + b.sgst;
  };

  const totals = { oI: 0, oC: 0, oS: 0, iI: 0, iC: 0, iS: 0, bI: 0, bC: 0, bS: 0, net: 0 };
  for (const c of shown) {
    const b = balanceOf(c.code);
    totals.oI += c.outputIgst;
    totals.oC += c.outputCgst;
    totals.oS += c.outputSgst;
    totals.iI += c.inputIgst;
    totals.iC += c.inputCgst;
    totals.iS += c.inputSgst;
    totals.bI += b.igst;
    totals.bC += b.cgst;
    totals.bS += b.sgst;
    totals.net += net(c);
  }

  const sourceRows: GstLiabilityLedgerRow[] = (data?.ledgers ?? []).filter(
    (l) => l.kind === source && shownCodes.has(l.code),
  );

  async function onDownload() {
    setDownloading(true);
    try {
      const payload: GstLiabilityBalance[] = shown.map((c) => ({ code: c.code, ...balanceOf(c.code) }));
      const name = await downloadGstLiabilityExcel({
        month,
        excludeSetOff,
        codes: shown.map((c) => c.code),
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
        <div className="w-64 space-y-1.5">
          <Label htmlFor="gst-liability-company">Company</Label>
          <select
            id="gst-liability-company"
            value={company}
            onChange={(e) => setCompany(e.target.value)}
            className={SELECT_CLASS}
          >
            <option value="">All companies</option>
            {known.map((c) => (
              <option key={c.code} value={c.code}>
                {c.code} · {c.name}
              </option>
            ))}
            {others.length > 0 && (
              <optgroup label="Other units with GST postings">
                {others.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.code}
                  </option>
                ))}
              </optgroup>
            )}
          </select>
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
          <Button onClick={onDownload} disabled={downloading || !data || shown.length === 0}>
            {downloading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FileSpreadsheet className="mr-2 h-4 w-4" />}
            Download Excel
          </Button>
        </div>
        <p className="w-full text-xs text-muted-foreground">
          From the ERP Group Summary of <b>GST Liabilities</b> and <b>GST Input Credits</b> for the month. Output = closing
          balance of the Output A/c ledgers; Input = Cl balance (debit − credit for the month) of the Input ledgers. Enter
          the cash / credit ledger balance (IGST / CGST / SGST A/c) to get the net. Negative = payable.
          {!excludeSetOff && setOffPosted > 0 && (
            <span className="text-amber-700 dark:text-amber-400">
              {" "}
              Month-end set-off entries of {fmt(setOffPosted)} are already posted for this month — tick “Before month-end
              set-off” to see the position before them.
            </span>
          )}
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
          <table className="w-full min-w-[1500px] text-sm">
            <thead>
              <tr className="border-b border-border text-xs">
                <th className="px-2.5 py-1.5 text-left font-semibold" colSpan={5}>
                  Sum of Closing Balance <span className="font-normal text-muted-foreground">· Output</span>
                </th>
                <th className={GAP} />
                <th className="bg-amber-50 px-2 py-1.5 text-center font-semibold dark:bg-amber-950/30">
                  Payable in (−) / Balance (+)
                </th>
                <th className="bg-amber-50 px-2 py-1.5 text-center font-semibold dark:bg-amber-950/30" colSpan={3}>
                  Balance
                </th>
                <th className={GAP} />
                <th className="px-2.5 py-1.5 text-left font-semibold" colSpan={5}>
                  Sum of Cl balance <span className="font-normal text-muted-foreground">· Input</span>
                </th>
              </tr>
              <tr className="border-b border-border bg-secondary/40">
                <th className={cn(TH, "text-left")}>Co</th>
                <th className={TH}>IGST Output A/c</th>
                <th className={TH}>CGST Output A/c</th>
                <th className={TH}>SGST Output A/c</th>
                <th className={TH}>Grand Total</th>
                <th className={GAP} />
                <th className={TH}>Net Balance</th>
                <th className={TH}>IGST A/c</th>
                <th className={TH}>CGST A/c</th>
                <th className={TH}>SGST A/c</th>
                <th className={GAP} />
                <th className={cn(TH, "text-left")}>Co2</th>
                <th className={TH}>IGST Input</th>
                <th className={TH}>CGST Input</th>
                <th className={TH}>SGST Input</th>
                <th className={TH}>Grand Total</th>
              </tr>
            </thead>
            <tbody>
              {shown.map((c) => (
                <tr key={c.code} className="border-b border-border/60">
                  <td className="px-2.5 py-1.5 font-semibold" title={`${c.gstin}\n${c.units.join("\n")}`}>
                    {c.code}
                  </td>
                  <td className={TD}><Amount value={c.outputIgst} /></td>
                  <td className={TD}><Amount value={c.outputCgst} /></td>
                  <td className={TD}><Amount value={c.outputSgst} /></td>
                  <td className={TD}><Amount value={outputTotal(c)} strong /></td>
                  <td className={GAP} />
                  <td className={cn(TD, "bg-amber-50/60 dark:bg-amber-950/20")}><Amount value={net(c)} strong /></td>
                  {(["igst", "cgst", "sgst"] as const).map((f) => (
                    <td key={f} className="bg-amber-50/60 px-1.5 py-1 dark:bg-amber-950/20">
                      <Input
                        inputMode="decimal"
                        value={balances[c.code]?.[f] ?? ""}
                        placeholder="0.00"
                        onChange={(e) => setBalance(c.code, f, e.target.value)}
                        className="h-8 w-28 text-right tabular-nums"
                      />
                    </td>
                  ))}
                  <td className={GAP} />
                  <td className="px-2.5 py-1.5 font-semibold">{c.code}</td>
                  <td className={TD}><Amount value={c.inputIgst} /></td>
                  <td className={TD}><Amount value={c.inputCgst} /></td>
                  <td className={TD}><Amount value={c.inputSgst} /></td>
                  <td className={TD}><Amount value={inputTotal(c)} strong /></td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-border bg-secondary/40 font-semibold">
                <td className="px-2.5 py-2">Grand Total</td>
                <td className={TD}><Amount value={totals.oI} /></td>
                <td className={TD}><Amount value={totals.oC} /></td>
                <td className={TD}><Amount value={totals.oS} /></td>
                <td className={TD}><Amount value={totals.oI + totals.oC + totals.oS} strong /></td>
                <td className={GAP} />
                <td className={TD}><Amount value={totals.net} strong /></td>
                <td className={TD}><Amount value={totals.bI} /></td>
                <td className={TD}><Amount value={totals.bC} /></td>
                <td className={TD}><Amount value={totals.bS} /></td>
                <td className={GAP} />
                <td className="px-2.5 py-2">Grand Total</td>
                <td className={TD}><Amount value={totals.iI} /></td>
                <td className={TD}><Amount value={totals.iC} /></td>
                <td className={TD}><Amount value={totals.iS} /></td>
                <td className={TD}><Amount value={totals.iI + totals.iC + totals.iS} strong /></td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {data && (
        <div className="rounded-xl border border-border bg-card shadow-sm">
          <div className="flex flex-wrap items-center gap-2 border-b border-border px-4 py-3">
            <h3 className="mr-2 text-sm font-semibold">Group Summary (source for the pivot)</h3>
            {(
              [
                ["output", "GST Liabilities · Output A/c"],
                ["input", "GST Input Credits · Input"],
                ["other", "GST Payable / RCM / non-eligible"],
              ] as const
            ).map(([key, label]) => (
              <Button
                key={key}
                size="sm"
                variant={source === key ? "default" : "outline"}
                onClick={() => setSource(key)}
              >
                {label}
              </Button>
            ))}
            <span className="ml-auto text-xs text-muted-foreground">{sourceRows.length} rows</span>
          </div>
          <div className="max-h-[520px] overflow-auto">
            <table className="w-full min-w-[1100px] text-sm">
              <thead className="sticky top-0 bg-card">
                <tr className="border-b border-border bg-secondary/40">
                  <th className={cn(TH, "text-left")}>Perticular</th>
                  <th className={TH}>Opening Balance</th>
                  <th className={TH}>Debit</th>
                  <th className={TH}>Credit</th>
                  {source === "input" && <th className={TH}>Cl balance</th>}
                  <th className={TH}>Closing Balance</th>
                  <th className={cn(TH, "text-left")}>Company</th>
                  <th className={cn(TH, "text-left")}>Co</th>
                </tr>
              </thead>
              <tbody>
                {sourceRows.map((l) => (
                  <tr key={`${l.unit}|${l.ledger}`} className="border-b border-border/40">
                    <td className="px-2.5 py-1.5">{l.ledger}</td>
                    <td className={TD}><Amount value={l.opening} /></td>
                    <td className={TD}>{fmt(l.debit)}</td>
                    <td className={TD}>{fmt(l.credit)}</td>
                    {source === "input" && <td className={TD}><Amount value={l.clBalance} strong /></td>}
                    <td className={TD}><Amount value={l.closing} strong={source !== "input"} /></td>
                    <td className="px-2.5 py-1.5 text-muted-foreground">{l.unit}</td>
                    <td className="px-2.5 py-1.5 font-medium">{l.code}</td>
                  </tr>
                ))}
                {sourceRows.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-4 py-6 text-center text-sm text-muted-foreground">
                      No postings for this selection.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
