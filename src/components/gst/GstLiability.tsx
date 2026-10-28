import { Fragment, useEffect, useRef, useState } from "react";
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

type Tax = { igst: number; cgst: number; sgst: number };
const z = (): Tax => ({ igst: 0, cgst: 0, sgst: 0 });
const add = (...rows: Tax[]): Tax =>
  rows.reduce((s, r) => ({ igst: s.igst + r.igst, cgst: s.cgst + r.cgst, sgst: s.sgst + r.sgst }), z());
const sub = (a: Tax, b: Tax): Tax => ({ igst: a.igst - b.igst, cgst: a.cgst - b.cgst, sgst: a.sgst - b.sgst });

/** Same order as GstItcUtilisation on the API. Returns the movement, not the balance. */
function itcAdjustment(net: Tax): Tax {
  const credit = { igst: Math.max(0, net.igst), cgst: Math.max(0, net.cgst), sgst: Math.max(0, net.sgst) };
  const liab = { igst: Math.max(0, -net.igst), cgst: Math.max(0, -net.cgst), sgst: Math.max(0, -net.sgst) };
  const room = liab.cgst + liab.sgst;
  const take = Math.min(credit.igst, room);
  if (take > 0 && room > 0) {
    let toC = Math.round((take * liab.cgst) / room * 100) / 100;
    if (toC > liab.cgst) toC = liab.cgst;
    let toS = Math.round((take - toC) * 100) / 100;
    if (toS > liab.sgst) {
      toS = liab.sgst;
      toC = Math.min(liab.cgst, Math.round((take - toS) * 100) / 100);
    }
    credit.igst -= toC + toS;
    liab.cgst -= toC;
    liab.sgst -= toS;
  }
  const use = (from: "cgst" | "sgst") => {
    const used = Math.min(credit[from], liab.igst);
    credit[from] -= used;
    liab.igst -= used;
  };
  use("cgst");
  use("sgst");
  const after = { igst: credit.igst - liab.igst, cgst: credit.cgst - liab.cgst, sgst: credit.sgst - liab.sgst };
  return sub(after, net);
}

function statement(c: GstLiabilityCompany, portal: Tax) {
  const op: Tax = { igst: c.opBalance || 0, cgst: 0, sgst: 0 };
  const input: Tax = { igst: c.inputClosingIgst || 0, cgst: c.inputClosingCgst || 0, sgst: c.inputClosingSgst || 0 };
  const pendingIn: Tax = { igst: c.pendingInputIgst || 0, cgst: c.pendingInputCgst || 0, sgst: c.pendingInputSgst || 0 };
  const a = add(op, input, pendingIn);
  const output: Tax = { igst: -(c.outputClosingIgst || 0), cgst: -(c.outputClosingCgst || 0), sgst: -(c.outputClosingSgst || 0) };
  const pendingOut: Tax = { igst: c.pendingOutputIgst || 0, cgst: c.pendingOutputCgst || 0, sgst: c.pendingOutputSgst || 0 };
  const b = add(output, pendingOut);
  const total = sub(a, b);
  const before = add(total, portal);
  const adjustment = itcAdjustment(before);
  const net = add(before, adjustment);
  const unclaimed: Tax = { igst: c.unclaimedIgst || 0, cgst: c.unclaimedCgst || 0, sgst: c.unclaimedSgst || 0 };
  return { op, input, pendingIn, a, output, pendingOut, b, total, portal, adjustment, net, unclaimed };
}

const TH = "px-2.5 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground";
const TD = "px-2.5 py-1.5 text-right tabular-nums";
function Amount({ value, strong }: { value: number; strong?: boolean }) {
  return (
    <span className={cn(value < 0 && "text-red-600 dark:text-red-400", strong && "font-semibold")}>{fmt(value)}</span>
  );
}

const TONES = {
  red: "border-l-red-500",
  green: "border-l-emerald-500",
  amber: "border-l-amber-500",
} as const;

function SummaryTile({
  label,
  hint,
  value,
  tone,
  strong,
}: {
  label: string;
  hint: string;
  value: number;
  tone: keyof typeof TONES;
  strong?: boolean;
}) {
  return (
    <div className={cn("rounded-xl border border-l-4 border-border bg-card px-4 py-3 shadow-sm", TONES[tone])}>
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p
        className={cn(
          "mt-1 tabular-nums",
          strong ? "text-2xl font-bold" : "text-xl font-semibold",
          value < 0 && "text-red-600 dark:text-red-400",
        )}
      >
        ₹ {fmt(value)}
      </p>
      <p className="mt-0.5 text-[11px] text-muted-foreground">{hint}</p>
    </div>
  );
}

function PivotCard({
  title,
  subtitle,
  accent,
  coLabel,
  columns,
  rows,
  totals,
}: {
  title: string;
  subtitle: string;
  accent: string;
  coLabel: string;
  columns: [string, string, string];
  rows: { code: string; title: string; values: number[] }[];
  totals: number[];
}) {
  const sum = (v: number[]) => v.reduce((s, x) => s + x, 0);
  return (
    <section className={cn("overflow-hidden rounded-xl border border-t-4 border-border bg-card shadow-sm", accent)}>
      <header className="border-b border-border px-4 py-3">
        <h3 className="text-sm font-semibold">{title}</h3>
        <p className="text-xs text-muted-foreground">{subtitle}</p>
      </header>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-secondary/40">
              <th className={cn(TH, "w-[18%] text-left")}>{coLabel}</th>
              {columns.map((c) => (
                <th key={c} className={TH}>
                  {c}
                </th>
              ))}
              <th className={TH}>Grand Total</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.code} className="border-b border-border/60 even:bg-muted/20">
                <td className="px-3 py-1.5 font-semibold" title={r.title}>
                  {r.code}
                </td>
                {r.values.map((v, i) => (
                  <td key={i} className={TD}>
                    <Amount value={v} />
                  </td>
                ))}
                <td className={TD}>
                  <Amount value={sum(r.values)} strong />
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-border bg-secondary/50 font-semibold">
              <td className="px-3 py-2">Grand Total</td>
              {totals.map((v, i) => (
                <td key={i} className={TD}>
                  <Amount value={v} strong />
                </td>
              ))}
              <td className={TD}>
                <Amount value={sum(totals)} strong />
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>
  );
}

export function GstLiability({ month, onMonthChange }: { month: string; onMonthChange: (month: string) => void }) {
  const [company, setCompany] = useState("");
  const [excludeSetOff, setExcludeSetOff] = useState(true);
  const [refreshToken, setRefreshToken] = useState(0);
  const [balances, setBalances] = useState<Balances>(() => loadBalances(month));
  const [source, setSource] = useState<SourceView>("output");
  const [downloading, setDownloading] = useState(false);
  const bypassCacheRef = useRef(false);

  useEffect(() => setBalances(loadBalances(month)), [month]);

  const query = useQuery({
    queryKey: ["gst-liability-ason", month, excludeSetOff, refreshToken],
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
  const netOf = (c: GstLiabilityCompany) => {
    const row = statement(c, balanceOf(c.code)).net;
    return row.igst + row.cgst + row.sgst;
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
    totals.net += netOf(c);
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
                {c.name && c.name !== c.code ? `${c.code} · ${c.name}` : c.code}
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
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryTile label="Output liability" hint="IGST + CGST + SGST Output A/c" value={totals.oI + totals.oC + totals.oS} tone="red" />
          <SummaryTile label="Input credit" hint="Cl balance of IGST / CGST / SGST Input" value={totals.iI + totals.iC + totals.iS} tone="green" />
          <SummaryTile label="Cash / credit ledger balance" hint="Entered below" value={totals.bI + totals.bC + totals.bS} tone="amber" />
          <SummaryTile
            label={totals.net < 0 ? "Net GST payable" : "Net balance"}
            hint="After ITC set-off, as on date"
            value={totals.net}
            tone={totals.net < 0 ? "red" : "green"}
            strong
          />
        </div>
      )}

      {data && (
        <PivotCard
          title="Output liability"
          subtitle="Sum of Closing Balance · GST Liabilities group"
          accent="border-t-red-500"
          coLabel="Co"
          columns={["IGST Output A/c", "CGST Output A/c", "SGST Output A/c"]}
          rows={shown.map((c) => ({ code: c.code, title: c.units.join("\n"), values: [c.outputIgst, c.outputCgst, c.outputSgst] }))}
          totals={[totals.oI, totals.oC, totals.oS]}
        />
      )}

      {data && (
        <PivotCard
          title="Input credit"
          subtitle="Sum of Cl balance (debit − credit for the month) · GST Input Credits group"
          accent="border-t-emerald-500"
          coLabel="Co2"
          columns={["IGST Input", "CGST Input", "SGST Input"]}
          rows={shown.map((c) => ({ code: c.code, title: c.units.join("\n"), values: [c.inputIgst, c.inputCgst, c.inputSgst] }))}
          totals={[totals.iI, totals.iC, totals.iS]}
        />
      )}

      {data && (
        <section className="overflow-hidden rounded-xl border border-t-4 border-border border-t-amber-500 bg-card shadow-sm">
          <header className="border-b border-border px-4 py-3">
            <h3 className="text-sm font-semibold">Net balance — as on date</h3>
            <p className="text-xs text-muted-foreground">
              Same basis as the GST balance mail, with IGST, CGST and SGST on each row. (A) is opening + input closing + pending input. (B) is output and pending output, shown as GST payable. Total is A − B. OP Balance is the GST Receivable ledger, which ERP does not keep tax-wise, so the full opening sits under IGST and the adjustment uses it against any tax until it is exhausted. Pending entries are from {data.asOnDate ? new Date(data.asOnDate).toLocaleDateString("en-GB") : "the previous month end"} through today. Unclaimed is shown separately and is not in the net. Portal A/c is optional.
            </p>
          </header>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-secondary/40">
                  <th className={cn(TH, "text-left")}>Particular</th>
                  <th className={TH}>IGST</th>
                  <th className={TH}>CGST</th>
                  <th className={TH}>SGST</th>
                </tr>
              </thead>
              <tbody>
                {shown.map((c) => {
                  const portal = balanceOf(c.code);
                  const s = statement(c, portal);
                  const rows: { label: string; tax: Tax; strong?: boolean; inputs?: boolean; muted?: boolean }[] = [
                    { label: "OP Balance", tax: s.op },
                    { label: "Input (Closing)", tax: s.input },
                    { label: "Pending Entry (Input)", tax: s.pendingIn },
                    { label: "(A)", tax: s.a, strong: true },
                    { label: "Output (Closing)", tax: s.output },
                    { label: "Pending Entry (Output)", tax: s.pendingOut },
                    { label: "(B)", tax: s.b, strong: true },
                    { label: "Total (A − B)", tax: s.total, strong: true },
                    { label: "Portal A/c", tax: portal, inputs: true },
                    { label: "Adjustment", tax: s.adjustment },
                    { label: "Net Balance", tax: s.net, strong: true },
                    { label: "Unclaimed", tax: s.unclaimed, muted: true },
                  ];
                  return (
                    <Fragment key={c.code}>
                      <tr className="border-b border-border bg-secondary/30">
                        <td className="px-3 py-1.5 font-semibold" title={`${c.gstin}\n${c.units.join("\n")}`} colSpan={4}>
                          {c.code}
                          <span className="ml-2 font-normal text-muted-foreground">{c.gstin}</span>
                        </td>
                      </tr>
                      {rows.map((row) => (
                        <tr key={row.label} className={cn("border-b border-border/40", row.strong && "bg-muted/30", row.muted && "text-muted-foreground")}>
                          <td className={cn("py-1.5 pl-6 pr-3", row.strong ? "font-semibold" : "text-muted-foreground")}>{row.label}</td>
                          {row.inputs
                            ? (["igst", "cgst", "sgst"] as const).map((f) => (
                                <td key={f} className="px-2 py-1">
                                  <Input
                                    inputMode="decimal"
                                    value={balances[c.code]?.[f] ?? ""}
                                    placeholder="0.00"
                                    onChange={(e) => setBalance(c.code, f, e.target.value)}
                                    className="ml-auto h-8 w-full max-w-[150px] text-right tabular-nums"
                                  />
                                </td>
                              ))
                            : (["igst", "cgst", "sgst"] as const).map((f) => (
                                <td key={f} className={TD}>
                                  <Amount value={row.tax[f]} strong={row.strong} />
                                </td>
                              ))}
                        </tr>
                      ))}
                    </Fragment>
                  );
                })}
              </tbody>
              <tfoot>
                {(() => {
                  const grand = shown.map((c) => statement(c, balanceOf(c.code))).reduce(
                    (s, row) => ({
                      op: add(s.op, row.op),
                      input: add(s.input, row.input),
                      pendingIn: add(s.pendingIn, row.pendingIn),
                      a: add(s.a, row.a),
                      output: add(s.output, row.output),
                      pendingOut: add(s.pendingOut, row.pendingOut),
                      b: add(s.b, row.b),
                      total: add(s.total, row.total),
                      portal: add(s.portal, row.portal),
                      adjustment: add(s.adjustment, row.adjustment),
                      net: add(s.net, row.net),
                      unclaimed: add(s.unclaimed, row.unclaimed),
                    }),
                    { op: z(), input: z(), pendingIn: z(), a: z(), output: z(), pendingOut: z(), b: z(), total: z(), portal: z(), adjustment: z(), net: z(), unclaimed: z() },
                  );
                  const foot: { label: string; tax: Tax; strong?: boolean }[] = [
                    { label: "OP Balance", tax: grand.op },
                    { label: "Input (Closing)", tax: grand.input },
                    { label: "Pending Entry (Input)", tax: grand.pendingIn },
                    { label: "(A)", tax: grand.a, strong: true },
                    { label: "Output (Closing)", tax: grand.output },
                    { label: "Pending Entry (Output)", tax: grand.pendingOut },
                    { label: "(B)", tax: grand.b, strong: true },
                    { label: "Total (A − B)", tax: grand.total, strong: true },
                    { label: "Portal A/c", tax: grand.portal },
                    { label: "Adjustment", tax: grand.adjustment },
                    { label: "Net Balance", tax: grand.net, strong: true },
                    { label: "Unclaimed", tax: grand.unclaimed },
                  ];
                  return (
                    <>
                      <tr className="border-t-2 border-border bg-secondary/50">
                        <td className="px-3 py-2 font-semibold" colSpan={4}>Grand Total</td>
                      </tr>
                      {foot.map((row) => (
                        <tr key={row.label} className="border-b border-border/40 bg-secondary/30">
                          <td className={cn("py-1.5 pl-6 pr-3", row.strong && "font-semibold")}>{row.label}</td>
                          {(["igst", "cgst", "sgst"] as const).map((f) => (
                            <td key={f} className={TD}><Amount value={row.tax[f]} strong={row.strong} /></td>
                          ))}
                        </tr>
                      ))}
                    </>
                  );
                })()}
              </tfoot>
            </table>
          </div>
        </section>
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
            <table className="w-full min-w-[900px] text-sm">
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
