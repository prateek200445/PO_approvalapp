import { useEffect, useRef, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  CheckCircle2,
  CircleAlert,
  FileSpreadsheet,
  FolderArchive,
  Loader2,
  RefreshCw,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/utils";
import {
  AGEING_BUCKETS,
  downloadBankDocumentPack,
  getBankDocumentPack,
  getBankPackDefaults,
  type BankDocumentPack,
  type BankPackMonth,
  type BankPackParams,
  type BankPackRanking,
} from "@/lib/bank-requirements-api";

export const Route = createFileRoute("/_app/bank-requirements_/document-pack")({
  head: () => ({ meta: [{ title: "Bank Document Pack — Banking — PO Portal" }] }),
  component: BankDocumentPackPage,
});

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

const lakh = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => lakh.format(n);
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const fmtDate = (iso: string) => {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  return y && m && d ? `${String(d).padStart(2, "0")}-${MONTHS[m - 1]}-${y}` : iso;
};
const fmtMonth = (yyyyMm: string) => {
  const [y, m] = yyyyMm.split("-").map(Number);
  return y && m ? `${MONTHS[m - 1]}-${String(y).slice(2)}` : yyyyMm;
};

type ChecklistItem = { items: string; title: string; inPack: boolean; note: string };

function checklist(ageingDates: string[], stockFrom: string, stockTo: string): ChecklistItem[] {
  const ageing = ageingDates.map((d) => fmtDate(d)).join(" & ");
  const fromData: ChecklistItem[] = [
    { items: "Top 10 customers", title: "Export & domestic top 10 customers (excl. group)", inPack: true, note: "Previous FY and current FY till date" },
    { items: "Top 10 suppliers", title: "Import & domestic top 10 suppliers (excl. group)", inPack: true, note: "Previous FY and current FY till date" },
    { items: "Monthly net sales", title: "Export / domestic (group vs direct)", inPack: true, note: "April to date, net of credit notes" },
    { items: "Monthly net purchase", title: "Import / domestic (group vs direct)", inPack: true, note: "April to date" },
    { items: "Debtors ageing", title: "Party-wise ageing", inPack: true, note: `As on ${ageing}` },
    { items: "Stock statement", title: "Month-end stock values", inPack: true, note: `${fmtMonth(stockFrom)} to ${fmtMonth(stockTo)}, as entered under P&L → Stock` },
  ];
  const outside: ChecklistItem[] = [
    { items: "Assessment note", title: "Latest bank assessment note", inPack: false, note: "Issued by the bank / prepared by Finance — not held in the ERP" },
    { items: "Debt profile", title: "Lender-wise limits & outstanding (incl. ECLGS 5.0)", inPack: false, note: "Sanction details are not in the ERP — Finance to provide" },
  ];
  return [...fromData, ...outside];
}

function BankDocumentPackPage() {
  const defaultsQuery = useQuery({
    queryKey: ["bank-pack-defaults"],
    queryFn: getBankPackDefaults,
    staleTime: 60 * 60_000,
  });
  const defaults = defaultsQuery.data;

  const [company, setCompany] = useState("HPBL");
  const [ageing1, setAgeing1] = useState("");
  const [ageing2, setAgeing2] = useState("");
  const [stockFrom, setStockFrom] = useState("");
  const [stockTo, setStockTo] = useState("");
  const [request, setRequest] = useState<BankPackParams | null>(null);
  const [refreshToken, setRefreshToken] = useState(0);
  const [downloading, setDownloading] = useState(false);
  const bypassCacheRef = useRef(false);

  useEffect(() => {
    if (!defaults) return;
    setAgeing1((v) => v || defaults.ageingDates[0] || "");
    setAgeing2((v) => v || defaults.ageingDates[1] || "");
    setStockFrom((v) => v || defaults.stockFrom);
    setStockTo((v) => v || defaults.stockTo);
  }, [defaults]);

  const params: BankPackParams = {
    company,
    ageingDates: [ageing1, ageing2].filter(Boolean),
    stockFrom,
    stockTo,
  };

  const packQuery = useQuery({
    queryKey: ["bank-document-pack", request, refreshToken],
    queryFn: () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getBankDocumentPack(request!, refresh);
    },
    enabled: request != null,
    staleTime: 30 * 60_000,
    retry: 0,
  });
  const pack = packQuery.data;

  function generate(refresh = false) {
    bypassCacheRef.current = refresh;
    setRequest({ ...params });
    if (refresh) setRefreshToken((t) => t + 1);
  }

  async function onDownload() {
    setDownloading(true);
    try {
      const name = await downloadBankDocumentPack(request ?? params);
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(false);
    }
  }

  const busy = packQuery.isFetching;
  const items = checklist(params.ageingDates, stockFrom, stockTo);

  return (
    <div className="space-y-5 pb-6">
      <div>
        <Link
          to="/bank-requirements"
          className="mb-2 inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Banking
        </Link>
        <div className="flex items-center gap-2 text-primary">
          <FolderArchive className="h-5 w-5" />
          <p className="text-xs font-semibold uppercase tracking-wide">Banking</p>
        </div>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">Bank Document Pack</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Everything the bank checklist asks for that lives in the ERP — top 10 customers and suppliers
          (excluding group companies), monthly net sales and purchase splits, party-wise debtors ageing
          and the stock statement — in one Excel workbook. Amounts in INR lakh.
        </p>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-6">
          <div className="space-y-1.5 lg:col-span-2">
            <Label>Company</Label>
            <select className={SELECT_CLASS} value={company} onChange={(e) => setCompany(e.target.value)}>
              {(defaults?.companies ?? [
                { code: "HPBL", displayName: "HCP Plastene Bulkpack Ltd" },
                { code: "KPW", displayName: "K.P. Woven Pvt Ltd" },
                { code: "PIL", displayName: "Plastene India Limited" },
              ]).map((c) => (
                <option key={c.code} value={c.code}>
                  {c.code} — {c.displayName}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-1.5">
            <Label>Debtors ageing as on</Label>
            <Input type="date" value={ageing1} max={defaults?.today} onChange={(e) => setAgeing1(e.target.value)} />
          </div>
          <div className="space-y-1.5">
            <Label>and as on</Label>
            <Input type="date" value={ageing2} max={defaults?.today} onChange={(e) => setAgeing2(e.target.value)} />
          </div>
          <div className="space-y-1.5">
            <Label>Stock from</Label>
            <Input type="month" value={stockFrom} onChange={(e) => setStockFrom(e.target.value)} />
          </div>
          <div className="space-y-1.5">
            <Label>Stock to</Label>
            <Input type="month" value={stockTo} onChange={(e) => setStockTo(e.target.value)} />
          </div>
        </div>
        <div className="mt-4 flex flex-wrap items-center gap-2">
          <Button onClick={() => generate(false)} disabled={busy || !defaults}>
            {busy ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FolderArchive className="mr-2 h-4 w-4" />}
            Build pack
          </Button>
          <Button variant="outline" onClick={onDownload} disabled={downloading || !defaults}>
            {downloading ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <FileSpreadsheet className="mr-2 h-4 w-4" />
            )}
            Download Excel
          </Button>
          {pack && (
            <Button variant="ghost" size="sm" onClick={() => generate(true)} disabled={busy}>
              <RefreshCw className="mr-1.5 h-3.5 w-3.5" />
              Refresh from ERP
            </Button>
          )}
          {(busy || downloading) && (
            <span className="text-xs text-muted-foreground">
              Reading sales, purchases and bill-wise receivables from the ERP — the first build takes 1–2 minutes, repeat builds are instant.
            </span>
          )}
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
        <h2 className="text-sm font-semibold">Bank checklist coverage — {company}</h2>
        <ul className="mt-3 grid gap-2 md:grid-cols-2">
          {items.map((it) => (
            <li key={it.items} className="flex items-start gap-2 text-sm">
              {it.inPack ? (
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />
              ) : (
                <CircleAlert className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
              )}
              <span>
                <span className="font-medium">{it.items}</span>
                <span className="text-muted-foreground"> — {it.title}. {it.note}.</span>
              </span>
            </li>
          ))}
        </ul>
      </div>

      {packQuery.isError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
          {(packQuery.error as Error).message}
        </div>
      )}

      {pack && <PackPreview pack={pack} />}
    </div>
  );
}

function PackPreview({ pack }: { pack: BankDocumentPack }) {
  return (
    <Tabs defaultValue="customers" className="space-y-3">
      <TabsList className="flex h-auto flex-wrap justify-start">
        <TabsTrigger value="customers">Top 10 customers</TabsTrigger>
        <TabsTrigger value="suppliers">Top 10 suppliers</TabsTrigger>
        <TabsTrigger value="sales">Monthly net sales</TabsTrigger>
        <TabsTrigger value="purchase">Monthly net purchase</TabsTrigger>
        <TabsTrigger value="ageing">Debtors ageing</TabsTrigger>
        <TabsTrigger value="stock">Stock statement</TabsTrigger>
      </TabsList>
      <TabsContent value="customers" className="space-y-4">
        {pack.customerRankings.map((r) => (
          <RankingCard key={r.label} ranking={r} overseasLabel="Export customers" domesticLabel="Domestic customers" />
        ))}
      </TabsContent>
      <TabsContent value="suppliers" className="space-y-4">
        {pack.supplierRankings.map((r) => (
          <RankingCard key={r.label} ranking={r} overseasLabel="Import suppliers" domesticLabel="Domestic suppliers" />
        ))}
      </TabsContent>
      <TabsContent value="sales">
        <MonthlyTable months={pack.monthlySales} overseas="Export" />
      </TabsContent>
      <TabsContent value="purchase">
        <MonthlyTable months={pack.monthlyPurchases} overseas="Import" />
      </TabsContent>
      <TabsContent value="ageing" className="space-y-4">
        {pack.debtors.map((a) => (
          <AgeingCard key={a.asOf} asOf={a.asOf} parties={a.parties} />
        ))}
      </TabsContent>
      <TabsContent value="stock">
        <StockTable pack={pack} />
      </TabsContent>
    </Tabs>
  );
}

const TH = "px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground";
const TD = "px-3 py-1.5";
const NUM = "px-3 py-1.5 text-right tabular-nums";

function RankingCard({
  ranking,
  overseasLabel,
  domesticLabel,
}: {
  ranking: BankPackRanking;
  overseasLabel: string;
  domesticLabel: string;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <h3 className="font-semibold">
        {ranking.label}{" "}
        <span className="text-sm font-normal text-muted-foreground">
          ({fmtDate(ranking.from)} – {fmtDate(ranking.to)})
        </span>
      </h3>
      <div className="mt-3 grid gap-4 xl:grid-cols-2">
        <RankTable title={overseasLabel} rows={ranking.overseas} total={ranking.overseasTotalLakh} showCountry />
        <RankTable title={domesticLabel} rows={ranking.domestic} total={ranking.domesticTotalLakh} />
      </div>
    </div>
  );
}

function RankTable({
  title,
  rows,
  total,
  showCountry,
}: {
  title: string;
  rows: BankPackRanking["overseas"];
  total: number;
  showCountry?: boolean;
}) {
  const top = rows.reduce((s, r) => s + r.amountLakh, 0);
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <div className="border-b border-border bg-secondary/40 px-3 py-2 text-sm font-medium">{title}</div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-border">
            <th className={TH}>#</th>
            <th className={TH}>Party</th>
            {showCountry && <th className={TH}>Country</th>}
            <th className={cn(TH, "text-right")}>₹ lakh</th>
            <th className={cn(TH, "text-right")}>Share</th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 && (
            <tr>
              <td colSpan={showCountry ? 5 : 4} className="px-3 py-4 text-center text-muted-foreground">
                No transactions in this period
              </td>
            </tr>
          )}
          {rows.map((r) => (
            <tr key={r.name} className="border-b border-border/60 last:border-0">
              <td className={TD}>{r.rank}</td>
              <td className={TD}>{r.name}</td>
              {showCountry && <td className={TD}>{r.country ?? ""}</td>}
              <td className={NUM}>{fmt(r.amountLakh)}</td>
              <td className={NUM}>{r.sharePct.toFixed(2)}%</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t border-border bg-secondary/30 font-semibold">
            <td className={TD} colSpan={showCountry ? 3 : 2}>
              Top {rows.length} of {fmt(total)} non-group total
            </td>
            <td className={NUM}>{fmt(top)}</td>
            <td className={NUM}>{total > 0 ? ((top / total) * 100).toFixed(2) : "0.00"}%</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function MonthlyTable({ months, overseas }: { months: BankPackMonth[]; overseas: "Export" | "Import" }) {
  const sum = (f: (m: BankPackMonth) => number) => months.reduce((s, m) => s + f(m), 0);
  const cols: { label: string; f: (m: BankPackMonth) => number; strong?: boolean }[] = [
    { label: `${overseas} — group`, f: (m) => m.overseasGroupLakh },
    { label: `${overseas} — direct`, f: (m) => m.overseasDirectLakh },
    { label: `Total ${overseas.toLowerCase()}`, f: (m) => m.overseasGroupLakh + m.overseasDirectLakh, strong: true },
    { label: "Domestic — group", f: (m) => m.domesticGroupLakh },
    { label: "Domestic — direct", f: (m) => m.domesticDirectLakh },
    { label: "Total domestic", f: (m) => m.domesticGroupLakh + m.domesticDirectLakh, strong: true },
    {
      label: "Total",
      f: (m) => m.overseasGroupLakh + m.overseasDirectLakh + m.domesticGroupLakh + m.domesticDirectLakh,
      strong: true,
    },
  ];
  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-card shadow-sm">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            <th className={TH}>Month</th>
            {cols.map((c) => (
              <th key={c.label} className={cn(TH, "text-right")}>
                {c.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {months.map((m) => (
            <tr key={m.month} className="border-b border-border/60">
              <td className={TD}>
                {m.label}
                {m.partialMonth && <span className="ml-1 text-xs text-muted-foreground">(till date)</span>}
              </td>
              {cols.map((c) => (
                <td key={c.label} className={cn(NUM, c.strong && "font-semibold")}>
                  {fmt(c.f(m))}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t border-border bg-secondary/30 font-semibold">
            <td className={TD}>Total</td>
            {cols.map((c) => (
              <td key={c.label} className={NUM}>
                {fmt(sum(c.f))}
              </td>
            ))}
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function AgeingCard({ asOf, parties }: { asOf: string; parties: BankDocumentPack["debtors"][number]["parties"] }) {
  const [showAll, setShowAll] = useState(false);
  const bucketTotals = AGEING_BUCKETS.map((_, i) => parties.reduce((s, p) => s + (p.bucketsLakh[i] ?? 0), 0));
  const total = parties.reduce((s, p) => s + p.totalLakh, 0);
  const groupTotal = parties.filter((p) => p.isGroup).reduce((s, p) => s + p.totalLakh, 0);
  const visible = showAll ? parties : parties.slice(0, 25);
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h3 className="font-semibold">Debtors as on {fmtDate(asOf)}</h3>
        <p className="text-sm text-muted-foreground">
          {parties.length} parties · total ₹{fmt(total)} lakh · third party ₹{fmt(total - groupTotal)} lakh · group ₹
          {fmt(groupTotal)} lakh
        </p>
      </div>
      <div className="mt-3 overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-secondary/40">
              <th className={TH}>Party</th>
              <th className={TH}>Type</th>
              {AGEING_BUCKETS.map((b) => (
                <th key={b} className={cn(TH, "text-right")}>
                  {b}
                </th>
              ))}
              <th className={cn(TH, "text-right")}>Total</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((p) => (
              <tr key={p.party} className="border-b border-border/60">
                <td className={TD}>{p.party}</td>
                <td className={cn(TD, "whitespace-nowrap text-xs text-muted-foreground")}>
                  {p.isGroup ? "Group" : "Third party"}
                  {p.overseas ? " · Export" : ""}
                </td>
                {p.bucketsLakh.map((v, i) => (
                  <td key={i} className={NUM}>
                    {v === 0 ? "–" : fmt(v)}
                  </td>
                ))}
                <td className={cn(NUM, "font-semibold")}>{fmt(p.totalLakh)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t border-border bg-secondary/30 font-semibold">
              <td className={TD} colSpan={2}>
                Total
              </td>
              {bucketTotals.map((v, i) => (
                <td key={i} className={NUM}>
                  {fmt(v)}
                </td>
              ))}
              <td className={NUM}>{fmt(total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
      {parties.length > 25 && (
        <Button variant="link" size="sm" className="mt-1 px-0" onClick={() => setShowAll((v) => !v)}>
          {showAll ? "Show top 25 only" : `Show all ${parties.length} parties`}
        </Button>
      )}
    </div>
  );
}

function StockTable({ pack }: { pack: BankDocumentPack }) {
  const s = pack.stock;
  if (s.units.length === 0)
    return (
      <p className="rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">
        No month-end stock values have been entered for this company under P&L → Stock.
      </p>
    );
  const monthTotal = (m: number) =>
    s.units.reduce((sum, u) => sum + u.valuesLakh.reduce((cs, cat) => cs + (cat[m] ?? 0), 0), 0);
  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-card shadow-sm">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            <th className={TH}>Unit / category</th>
            {s.monthLabels.map((m) => (
              <th key={m} className={cn(TH, "text-right")}>
                {m}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {s.units.map((u) => (
            <UnitRows key={u.unit} unit={u} categories={s.categories} months={s.monthLabels.length} />
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t border-border bg-secondary/30 font-semibold">
            <td className={TD}>Total stock — all units</td>
            {s.monthLabels.map((m, i) => (
              <td key={m} className={NUM}>
                {fmt(monthTotal(i))}
              </td>
            ))}
          </tr>
        </tfoot>
      </table>
      <p className="px-3 py-2 text-xs text-muted-foreground">
        “not entered” = the unit has not uploaded that month under P&L → Stock yet (counted as zero in the total).
      </p>
    </div>
  );
}

function UnitRows({
  unit,
  categories,
  months,
}: {
  unit: BankDocumentPack["stock"]["units"][number];
  categories: string[];
  months: number;
}) {
  return (
    <>
      <tr className="border-b border-border/60 bg-secondary/20">
        <td className={cn(TD, "font-semibold")}>{unit.unit}</td>
        {Array.from({ length: months }, (_, m) => {
          const vals = unit.valuesLakh.map((c) => c[m]);
          const missing = vals.every((v) => v == null);
          return (
            <td key={m} className={cn(NUM, "font-semibold", missing && "text-amber-700")}>
              {missing ? "not entered" : fmt(vals.reduce<number>((s, v) => s + (v ?? 0), 0))}
            </td>
          );
        })}
      </tr>
      {categories.map((cat, c) =>
        unit.valuesLakh[c].every((v) => !v) ? null : (
          <tr key={cat} className="border-b border-border/40">
            <td className={cn(TD, "pl-6 text-muted-foreground")}>{cat}</td>
            {unit.valuesLakh[c].map((v, m) => (
              <td key={m} className={cn(NUM, v == null && "text-xs italic text-amber-700")}>
                {v == null ? "not entered" : fmt(v)}
              </td>
            ))}
          </tr>
        ),
      )}
    </>
  );
}
