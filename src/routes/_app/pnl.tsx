import { createFileRoute, Link } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArrowLeft, Loader2, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import {
  getPnlCompanies,
  getPnlIncomeExpense,
  getPnlProvisions,
  getPnlStatement,
  getPnlStockYear,
  savePnlProvisions,
  savePnlStockYear,
  getPnlOverhead,
  savePnlOverhead,
} from "@/lib/pnl-api";
import {
  currentMonthValue,
  formatLacs,
  formatPct,
  isSingleCompany,
  monthRange,
  type PnlHeadGroup,
  type PnlOverheadState,
  type PnlProvisionRow,
  type PnlStockYearRow,
  type PnlStockYearState,
} from "@/lib/pnl-types";
import { useIsMobile } from "@/hooks/use-mobile";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/pnl")({
  head: () => ({ meta: [{ title: "P&L / EBITDA — PO Portal" }] }),
  component: PnlPage,
});

function PnlPage() {
  const [company, setCompany] = useState("All Companies");
  const [month, setMonth] = useState(currentMonthValue());
  const [tab, setTab] = useState("books");
  const range = monthRange(month);
  const single = isSingleCompany(company);

  const companiesQuery = useQuery({
    queryKey: ["pnl-companies"],
    queryFn: getPnlCompanies,
    staleTime: 60 * 60_000,
  });

  const booksQuery = useQuery({
    queryKey: ["pnl-books", company, range.from, range.to],
    queryFn: () => getPnlIncomeExpense(company, range.from, range.to),
    enabled: tab === "books",
    staleTime: 5 * 60_000,
  });

  const provisionQuery = useQuery({
    queryKey: ["pnl-provision", company, month],
    queryFn: () => getPnlProvisions(company, month),
    enabled: tab === "provision" && single,
  });

  const stockQuery = useQuery({
    queryKey: ["pnl-stock-year", company, month],
    queryFn: () => getPnlStockYear(company, month),
    enabled: tab === "stock" && single,
  });

  const overheadQuery = useQuery({
    queryKey: ["pnl-overhead", company, month],
    queryFn: () => getPnlOverhead(company, month),
    enabled: tab === "overhead" && single,
  });

  const statementQuery = useQuery({
    queryKey: ["pnl-statement", company, month],
    queryFn: () => getPnlStatement(company, month),
    enabled: tab === "statement",
    staleTime: 30_000,
  });

  const companyOptions = useMemo(
    () => (companiesQuery.data ?? []).map((c) => ({ value: c.value, label: c.label })),
    [companiesQuery.data],
  );

  return (
    <div className="space-y-4 pb-8">
      <div className="flex items-start gap-3">
        <Link
          to="/ledgers"
          className="mt-1 rounded-md p-1 text-muted-foreground hover:bg-secondary hover:text-foreground"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">P&L / EBITDA</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Income and expense from trial balance, then enter provision, stock, and Common / HO to generate P&amp;L.
          </p>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="space-y-1 text-sm">
          <span className="text-muted-foreground">Company</span>
          <SearchableSelect
            options={companyOptions}
            value={company}
            onChange={setCompany}
            placeholder={companiesQuery.isLoading ? "Loading…" : "Select company"}
            disabled={companiesQuery.isLoading}
          />
        </label>
        <label className="space-y-1 text-sm">
          <span className="text-muted-foreground">Month</span>
          <Input type="month" value={month} onChange={(e) => setMonth(e.target.value)} />
        </label>
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList className="flex h-auto w-full flex-wrap gap-1">
          <TabsTrigger value="books">Income / Expense</TabsTrigger>
          <TabsTrigger value="provision">Provision</TabsTrigger>
          <TabsTrigger value="stock">Stock value</TabsTrigger>
          <TabsTrigger value="overhead">Common / HO</TabsTrigger>
          <TabsTrigger value="statement">P&amp;L</TabsTrigger>
        </TabsList>

        <TabsContent value="books" className="mt-4">
          <BooksTab query={booksQuery} />
        </TabsContent>
        <TabsContent value="provision" className="mt-4">
          {single ? (
            <ProvisionTab
              key={`${company}-${month}`}
              company={company}
              month={month}
              data={provisionQuery.data}
              loading={provisionQuery.isLoading}
              onSaved={() => void provisionQuery.refetch()}
            />
          ) : (
            <p className="text-sm text-muted-foreground">Pick a single company to enter provision.</p>
          )}
        </TabsContent>
        <TabsContent value="stock" className="mt-4">
          {single ? (
            <StockTab
              key={`${company}-${month}`}
              company={company}
              month={month}
              data={stockQuery.data}
              loading={stockQuery.isLoading}
              onSaved={() => void stockQuery.refetch()}
            />
          ) : (
            <p className="text-sm text-muted-foreground">Pick a single company to enter stock value (lacs).</p>
          )}
        </TabsContent>
        <TabsContent value="overhead" className="mt-4">
          {single ? (
            <OverheadTab
              key={`${company}-${month}`}
              company={company}
              month={month}
              data={overheadQuery.data}
              loading={overheadQuery.isLoading}
              onSaved={() => void overheadQuery.refetch()}
            />
          ) : (
            <p className="text-sm text-muted-foreground">
              Pick a single company to enter Common / HO allocation (lacs). Group and All Companies use the sum of saved plants.
            </p>
          )}
        </TabsContent>
        <TabsContent value="statement" className="mt-4">
          <StatementTab query={statementQuery} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function BooksTab({
  query,
}: {
  query: { data?: { incomeLacs: number; expenseLacs: number; heads: PnlHeadGroup[] }; isLoading: boolean; error: Error | null };
}) {
  if (query.isLoading) return <Busy label="Loading trial balance income / expense…" />;
  if (query.error) return <p className="text-sm text-destructive">{query.error.message}</p>;
  const data = query.data;
  if (!data) return null;

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <Kpi label="Income" value={data.incomeLacs} />
        <Kpi label="Expense" value={data.expenseLacs} />
      </div>
      <p className="text-xs text-muted-foreground">
        Amounts in lacs. Includes this month’s provision and reverses last month’s provision.
      </p>
      {data.heads.map((head) => (
        <Collapsible key={`${head.category}-${head.head}`} className="rounded-lg border bg-card">
          <CollapsibleTrigger className="flex w-full items-center justify-between gap-2 px-3 py-2.5 text-left">
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{head.category}</div>
              <div className="text-sm font-medium">{head.head}</div>
            </div>
            <div className="font-medium tabular-nums">{formatLacs(head.amountLacs)}</div>
          </CollapsibleTrigger>
          <CollapsibleContent className="border-t px-3 py-2">
            {head.ledgers.map((led) => (
              <div key={led.ledgerName} className="flex justify-between gap-2 py-1 text-sm">
                <span className="text-muted-foreground">{led.ledgerName}</span>
                <span className="tabular-nums">{formatLacs(led.amountLacs)}</span>
              </div>
            ))}
          </CollapsibleContent>
        </Collapsible>
      ))}
    </div>
  );
}

function ProvisionTab({
  company,
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: { rows: PnlProvisionRow[]; ledgerOptions: string[] };
  loading: boolean;
  onSaved: () => void;
}) {
  const [rows, setRows] = useState<PnlProvisionRow[] | null>(null);
  const visible = rows ?? data?.rows ?? [];

  const save = useMutation({
    mutationFn: () => savePnlProvisions(company, month, visible),
    onSuccess: () => {
      toast.success("Provision saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading provision…" />;

  function addRow() {
    setRows([...(visible.length ? visible : data?.rows ?? []), { ledgerName: "", amount: 0, amountLacs: 0 }]);
  }

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">Enter month-end provision in lacs. Saved to ERP Provisioning.</p>
      {visible.map((row, idx) => (
        <div key={idx} className="flex gap-2">
          <Input
            list="pnl-ledgers"
            placeholder="Ledger"
            value={row.ledgerName}
            onChange={(e) => {
              const next = visible.map((r, i) => (i === idx ? { ...r, ledgerName: e.target.value } : r));
              setRows(next);
            }}
          />
          <Input
            type="number"
            inputMode="decimal"
            className="w-28"
            placeholder="Lacs"
            value={row.amountLacs || ""}
            onChange={(e) => {
              const lacs = Number(e.target.value);
              const next = visible.map((r, i) =>
                i === idx ? { ...r, amountLacs: lacs, amount: 0 } : r,
              );
              setRows(next);
            }}
          />
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setRows(visible.filter((_, i) => i !== idx))}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}
      <datalist id="pnl-ledgers">
        {(data?.ledgerOptions ?? []).slice(0, 400).map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
      <div className="flex gap-2">
        <Button variant="outline" onClick={addRow}>
          <Plus className="mr-1 h-4 w-4" />
          Add line
        </Button>
        <Button onClick={() => save.mutate()} disabled={save.isPending}>
          {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
          Save provision
        </Button>
      </div>
    </div>
  );
}

function StockTab({
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: PnlStockYearState;
  loading: boolean;
  onSaved: () => void;
}) {
  const isMobile = useIsMobile();
  const [draft, setDraft] = useState<PnlStockYearState | null>(null);
  const sheet = draft ?? data;

  const save = useMutation({
    mutationFn: () => {
      if (!sheet) throw new Error("Nothing to save");
      return savePnlStockYear(sheet);
    },
    onSuccess: () => {
      toast.success("Stock values saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading stock values…" />;
  if (!sheet) return null;

  const columns = sheet.columns ?? [];
  const rows = (sheet.rows ?? []).map((row) => ({
    ...row,
    months: row.months ?? {},
  }));

  function setOpening(category: string, value: number) {
    setDraft({
      ...sheet!,
      rows: rows.map((r) => (r.category === category ? { ...r, openingLacs: value } : r)),
    });
  }

  function setMonthValue(category: string, key: string, value: number) {
    setDraft({
      ...sheet!,
      rows: rows.map((r) =>
        r.category === category ? { ...r, months: { ...r.months, [key]: value } } : r,
      ),
    });
  }

  function previousKey(key: string) {
    const i = columns.findIndex((c) => c.key === key);
    return i > 0 ? columns[i - 1].key : null;
  }

  const totals = (key: "opening" | string) =>
    rows.reduce((sum, r) => sum + (key === "opening" ? r.openingLacs || 0 : r.months[key] || 0), 0);

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        Stock sheet {sheet.fyLabel} (lacs). Total is RM + packing + WIP + FG + traded + stores — not entered.
        Selected month for P&amp;L is highlighted.
      </p>

      {isMobile ? (
        <MobileStockEditor
          month={month}
          rows={rows}
          previousKey={previousKey(month)}
          onOpening={setOpening}
          onMonth={setMonthValue}
        />
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-max min-w-full border-collapse text-sm">
            <thead>
              <tr className="bg-muted/60 text-xs text-muted-foreground">
                <th className="sticky left-0 z-10 bg-muted px-2 py-2 text-left font-medium">Type</th>
                <th className="px-2 py-2 text-right font-medium">op</th>
                {columns.map((col) => (
                  <th
                    key={col.key}
                    className={cn(
                      "px-2 py-2 text-right font-medium",
                      col.key === month && "bg-primary/15 text-foreground",
                    )}
                  >
                    {col.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.category} className="border-t">
                  <td className="sticky left-0 z-10 bg-card px-2 py-1.5 font-medium">{row.label}</td>
                  <td className="px-1 py-1">
                    <StockCell value={row.openingLacs} onChange={(v) => setOpening(row.category, v)} />
                  </td>
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={cn("px-1 py-1", col.key === month && "bg-primary/5")}
                    >
                      <StockCell
                        value={row.months[col.key] || 0}
                        onChange={(v) => setMonthValue(row.category, col.key, v)}
                      />
                    </td>
                  ))}
                </tr>
              ))}
              <tr className="border-t bg-muted/40 font-semibold">
                <td className="sticky left-0 z-10 bg-muted/40 px-2 py-2">Total Stock</td>
                <td className="px-2 py-2 text-right tabular-nums">{formatLacs(totals("opening"))}</td>
                {columns.map((col) => (
                  <td
                    key={col.key}
                    className={cn(
                      "px-2 py-2 text-right tabular-nums",
                      col.key === month && "bg-primary/10",
                    )}
                  >
                    {formatLacs(totals(col.key))}
                  </td>
                ))}
              </tr>
            </tbody>
          </table>
        </div>
      )}

      <Button onClick={() => save.mutate()} disabled={save.isPending}>
        {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        Save stock
      </Button>
    </div>
  );
}

function MobileStockEditor({
  month,
  rows,
  previousKey,
  onOpening,
  onMonth,
}: {
  month: string;
  rows: PnlStockYearRow[];
  previousKey: string | null;
  onOpening: (category: string, value: number) => void;
  onMonth: (category: string, key: string, value: number) => void;
}) {
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-[1fr_5.5rem_5.5rem] gap-2 text-xs font-medium text-muted-foreground">
        <span>Type</span>
        <span className="text-right">Opening</span>
        <span className="text-right">Closing</span>
      </div>
      {rows.map((row) => {
        const opening = previousKey ? row.months[previousKey] || 0 : row.openingLacs || 0;
        const closing = row.months[month] || 0;
        return (
          <div key={row.category} className="grid grid-cols-[1fr_5.5rem_5.5rem] items-center gap-2">
            <span className="text-sm">{row.label}</span>
            <StockCell
              value={opening}
              onChange={(v) => (previousKey ? onMonth(row.category, previousKey, v) : onOpening(row.category, v))}
            />
            <StockCell value={closing} onChange={(v) => onMonth(row.category, month, v)} />
          </div>
        );
      })}
    </div>
  );
}

function StockCell({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  return (
    <Input
      type="number"
      inputMode="decimal"
      className="h-8 min-w-[4.5rem] px-1 text-right text-sm"
      value={value || ""}
      onChange={(e) => onChange(Number(e.target.value))}
    />
  );
}

function OverheadTab({
  company,
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: PnlOverheadState;
  loading: boolean;
  onSaved: () => void;
}) {
  const [commonLacs, setCommonLacs] = useState<number | null>(null);
  const [hoLacs, setHoLacs] = useState<number | null>(null);
  const common = commonLacs ?? data?.commonLacs ?? 0;
  const ho = hoLacs ?? data?.hoLacs ?? 0;

  const save = useMutation({
    mutationFn: () => savePnlOverhead(company, month, common, ho),
    onSuccess: () => {
      toast.success("Common / HO saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading Common / HO…" />;

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        Plant share of common and head-office costs for this month, in lacs. Not posted to ERP ledgers — only used on
        this P&amp;L. Leave 0 if you do not allocate. YTD is the sum of April through this month.
      </p>
      <label className="block space-y-1 text-sm">
        <span className="text-muted-foreground">Common expenses</span>
        <Input
          type="number"
          inputMode="decimal"
          value={common || ""}
          onChange={(e) => setCommonLacs(Number(e.target.value))}
        />
      </label>
      <label className="block space-y-1 text-sm">
        <span className="text-muted-foreground">HO expenses</span>
        <Input
          type="number"
          inputMode="decimal"
          value={ho || ""}
          onChange={(e) => setHoLacs(Number(e.target.value))}
        />
      </label>
      <Button onClick={() => save.mutate()} disabled={save.isPending}>
        {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        Save Common / HO
      </Button>
    </div>
  );
}

function StatementTab({
  query,
}: {
  query: {
    data?: {
      ebitdaLacs: number;
      pbtLacs: number;
      stockIncomplete: boolean;
      rows: {
        id: string;
        label: string;
        kind: string;
        monthLacs: number | null;
        pctToSales: number | null;
        ytdLacs: number | null;
        ytdPctToSales: number | null;
      }[];
      unmapped: PnlHeadGroup[];
    };
    isLoading: boolean;
    error: Error | null;
    refetch: () => void;
  };
}) {
  if (query.isLoading) return <Busy label="Generating P&L…" />;
  if (query.error) return <p className="text-sm text-destructive">{query.error.message}</p>;
  const data = query.data;
  if (!data) return null;

  return (
    <div className="space-y-3">
      {data.stockIncomplete ? (
        <p className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
          Stock values are empty. Enter opening/closing stock so materials consumed and inventory change are correct.
        </p>
      ) : null}
      <div className="grid grid-cols-2 gap-3">
        <Kpi label="EBITDA" value={data.ebitdaLacs} />
        <Kpi label="PBT" value={data.pbtLacs} />
      </div>
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full min-w-[28rem] text-sm">
          <thead className="bg-muted/60 text-left text-xs text-muted-foreground">
            <tr>
              <th className="px-2 py-2 font-medium">Particulars</th>
              <th className="px-2 py-2 text-right font-medium">Month</th>
              <th className="px-2 py-2 text-right font-medium">%</th>
              <th className="px-2 py-2 text-right font-medium">YTD</th>
              <th className="px-2 py-2 text-right font-medium">YTD %</th>
            </tr>
          </thead>
          <tbody>
            {data.rows.map((row) => (
              <tr
                key={row.id}
                className={cn(
                  "border-t",
                  row.kind === "header" && "bg-muted/40 font-semibold",
                  row.kind === "total" && "font-semibold",
                  row.kind === "result" && "bg-primary/10 font-semibold",
                )}
              >
                <td className={cn("px-2 py-1.5", row.kind === "line" && "pl-4")}>{row.label}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">
                  {row.kind === "header" ? "" : formatLacs(row.monthLacs)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums text-muted-foreground">
                  {row.kind === "header" ? "" : formatPct(row.pctToSales)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums">
                  {row.kind === "header" ? "" : formatLacs(row.ytdLacs)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums text-muted-foreground">
                  {row.kind === "header" ? "" : formatPct(row.ytdPctToSales)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {data.unmapped.length > 0 ? (
        <div>
          <h3 className="mb-2 text-sm font-medium">Unmapped TB heads</h3>
          {data.unmapped.map((h) => (
            <div key={h.head} className="flex justify-between text-sm">
              <span className="text-muted-foreground">{h.head}</span>
              <span className="tabular-nums">{formatLacs(h.amountLacs)}</span>
            </div>
          ))}
        </div>
      ) : null}
      <Button variant="outline" onClick={() => query.refetch()}>
        Refresh P&amp;L
      </Button>
    </div>
  );
}

function Kpi({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <div className="text-xs text-muted-foreground">{label} (lacs)</div>
      <div className="mt-1 text-xl font-semibold tabular-nums">{formatLacs(value)}</div>
    </div>
  );
}

function Busy({ label }: { label: string }) {
  return (
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      {label}
    </div>
  );
}
