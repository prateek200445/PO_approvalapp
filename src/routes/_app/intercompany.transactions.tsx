import { useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertCircle, ArrowLeftRight, Inbox, Loader2 } from "lucide-react";
import {
  formatAsOn,
  formatCrore,
  formatInr,
  getIntercompanyGroups,
  getIntercompanyTransactions,
  type IntercompanyTransactionCell,
} from "@/lib/intercompany-api";
import { IntercompanySubnav } from "@/components/intercompany/IntercompanySubnav";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/_app/intercompany/transactions")({
  head: () => ({ meta: [{ title: "Intercompany transactions — PO Portal" }] }),
  component: IntercompanyTransactionsPage,
});

const DEFAULT_GROUPS = [
  "HCP Plastene Bulkpack Ltd",
  "HCP ENTERPRISE LIMITED",
  "K.P. WOVEN PRIVATE LIMITED",
  "Plastene India Limited",
  "Plastene Polyfilms Limited",
  "Oswal Extrusion Limited",
  "OSWAL COMMODITIES PRIVATE LIMITED",
];

type Measure = "all" | "sales" | "purchase" | "payment" | "receipt";
type SingleMeasure = Exclude<Measure, "all">;

const MEASURES: { id: Measure; label: string; tone: string; dot: string; hint: string }[] = [
  { id: "all", label: "All four", tone: "text-foreground", dot: "bg-foreground", hint: "" },
  { id: "sales", label: "Sales", tone: "text-sky-700 dark:text-sky-300", dot: "bg-sky-400", hint: "Bill amount" },
  { id: "purchase", label: "Purchase", tone: "text-amber-700 dark:text-amber-300", dot: "bg-amber-400", hint: "Bill amount" },
  { id: "payment", label: "Payment", tone: "text-rose-700 dark:text-rose-300", dot: "bg-rose-400", hint: "Money paid" },
  { id: "receipt", label: "Receipt", tone: "text-emerald-700 dark:text-emerald-300", dot: "bg-emerald-400", hint: "Money received" },
];

const SHORT_NAMES: Record<string, string> = {
  "hcp enterprise limited": "HCP Enterprise",
  "hcp plastene bulkpack ltd": "HPBL",
  "k.p. woven private limited": "K.P. Woven",
  "oswal commodities private limited": "Oswal Commodities",
  "oswal extrusion limited": "Oswal Extrusion",
  "plastene india limited": "Plastene India",
  "plastene polyfilms limited": "Polyfilms",
  "anshita trading co.pvt. ltd.": "Anshita",
  "elite inks private limited": "Elite Inks",
  "jain bulk pack": "Jain Bulk",
  "marudhar polymers": "Marudhar",
  "oswal wood private limited": "Oswal Wood",
  "parekh bulk pack": "Parekh",
  "plastene infrastructure limited": "Infrastructure",
  "shikharji polymers": "Shikharji",
};

function shortName(name: string): string {
  const known = SHORT_NAMES[name.trim().toLowerCase()];
  if (known) return known;
  return name
    .replace(/\b(private|pvt\.?|limited|ltd\.?|llp|co\.?)\b/gi, "")
    .replace(/[().]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function todayIso(): string {
  const d = new Date();
  return iso(d);
}

function fyStartIso(): string {
  const d = new Date();
  const year = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
  return `${year}-04-01`;
}

function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function amountOf(cell: IntercompanyTransactionCell | undefined, measure: SingleMeasure): number {
  if (!cell) return 0;
  return cell[measure];
}

function cellKey(book: string, counter: string): string {
  return `${book}\u001f${counter}`;
}

function IntercompanyTransactionsPage() {
  const [fromInput, setFromInput] = useState(fyStartIso);
  const [toInput, setToInput] = useState(todayIso);
  const [draftGroups, setDraftGroups] = useState<string[] | null>(null);
  const [applied, setApplied] = useState<{ from: string; to: string; groups: string[] }>({
    from: fyStartIso(),
    to: todayIso(),
    groups: DEFAULT_GROUPS,
  });
  const [measure, setMeasure] = useState<Measure>("all");

  const groupsQuery = useQuery({
    queryKey: ["intercompany-groups"],
    queryFn: getIntercompanyGroups,
    staleTime: 60 * 60 * 1000,
  });

  const allGroups = groupsQuery.data ?? [];
  const selected = draftGroups ?? allGroups.filter((name) => DEFAULT_GROUPS.some((d) => d.toLowerCase() === name.toLowerCase()));

  const txQuery = useQuery({
    queryKey: ["intercompany-transactions", applied?.from, applied?.to, applied?.groups],
    queryFn: () => getIntercompanyTransactions(applied!.from, applied!.to, applied!.groups),
    enabled: Boolean(applied && applied.groups.length >= 2),
    placeholderData: keepPreviousData,
  });

  const report = txQuery.data;
  const gridGroups = report?.groups ?? [];

  const byPair = useMemo(() => {
    const map = new Map<string, IntercompanyTransactionCell>();
    for (const cell of report?.cells ?? []) map.set(cellKey(cell.bookGroup, cell.counterGroup), cell);
    return map;
  }, [report]);

  const totals = useMemo(() => {
    const sum = { sales: 0, purchase: 0, payment: 0, receipt: 0 };
    for (const cell of report?.cells ?? []) {
      sum.sales += cell.sales;
      sum.purchase += cell.purchase;
      sum.payment += cell.payment;
      sum.receipt += cell.receipt;
    }
    return sum;
  }, [report]);

  function toggleGroup(name: string) {
    setDraftGroups((current) => {
      const base = current ?? selected;
      return base.some((g) => g.toLowerCase() === name.toLowerCase())
        ? base.filter((g) => g.toLowerCase() !== name.toLowerCase())
        : [...base, name];
    });
  }

  function show() {
    if (!fromInput || !toInput || selected.length < 2) return;
    setApplied({ from: fromInput, to: toInput, groups: [...selected] });
  }

  const needsTwo = selected.length < 2;
  const dateOrderBad = Boolean(fromInput && toInput && toInput < fromInput);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="flex items-start gap-3.5">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl border border-primary/20 bg-primary/10 text-primary shadow-soft">
            <ArrowLeftRight className="h-5 w-5" />
          </div>
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary">Intercompany</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight text-foreground md:text-[1.75rem]">Transactions</h1>
            <p className="mt-1.5 max-w-xl text-sm leading-relaxed text-muted-foreground">
              Sales, purchase, payments and receipts between the groups you select. Each figure is from that row company’s own books.
            </p>
          </div>
        </div>
        <IntercompanySubnav />
      </div>

      <div className="rounded-2xl border border-border/80 bg-card shadow-soft">
        <div className="flex flex-col gap-4 border-b border-border/70 p-4 sm:flex-row sm:items-end sm:p-5">
          <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="ict-from" className="text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground">From</Label>
              <Input id="ict-from" type="date" value={fromInput} onChange={(e) => setFromInput(e.target.value)} className="h-11 bg-background dark:[color-scheme:dark]" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="ict-to" className="text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground">To</Label>
              <Input id="ict-to" type="date" value={toInput} onChange={(e) => setToInput(e.target.value)} className="h-11 bg-background dark:[color-scheme:dark]" />
            </div>
          </div>
          <Button type="button" className="h-11 px-7 sm:mb-px" disabled={needsTwo || dateOrderBad || groupsQuery.isLoading} onClick={show}>
            {txQuery.isFetching ? <Loader2 className="h-4 w-4 animate-spin" /> : "Show"}
          </Button>
        </div>

        <div className="space-y-3 p-4 sm:p-5">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-baseline gap-2">
              <Label className="text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground">Group companies</Label>
              <span className="text-xs tabular-nums text-muted-foreground">{selected.length} selected</span>
            </div>
            <div className="flex gap-1 rounded-lg bg-muted/50 p-0.5">
              <button
                type="button"
                className="rounded-md px-2.5 py-1 text-xs font-medium text-foreground/80 hover:bg-card hover:text-foreground"
                onClick={() => setDraftGroups(allGroups.filter((name) => DEFAULT_GROUPS.some((d) => d.toLowerCase() === name.toLowerCase())))}
              >
                Main groups
              </button>
              <button
                type="button"
                className="rounded-md px-2.5 py-1 text-xs font-medium text-foreground/80 hover:bg-card hover:text-foreground"
                onClick={() => setDraftGroups([...allGroups])}
              >
                All
              </button>
              <button
                type="button"
                className="rounded-md px-2.5 py-1 text-xs font-medium text-muted-foreground hover:bg-card hover:text-foreground"
                onClick={() => setDraftGroups([])}
              >
                Clear
              </button>
            </div>
          </div>
          {groupsQuery.isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin text-primary" /> Loading groups…
            </p>
          ) : groupsQuery.isError ? (
            <p className="text-sm text-destructive">
              {groupsQuery.error instanceof Error ? groupsQuery.error.message : "Could not load groups."}
            </p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {allGroups.map((name) => {
                const on = selected.some((g) => g.toLowerCase() === name.toLowerCase());
                return (
                  <button
                    key={name}
                    type="button"
                    aria-pressed={on}
                    title={name}
                    onClick={() => toggleGroup(name)}
                    className={cn(
                      "rounded-full border px-3 py-1.5 text-sm font-medium transition",
                      on
                        ? "border-primary bg-primary/15 text-foreground ring-1 ring-primary/30"
                        : "border-border bg-background/40 text-muted-foreground hover:border-foreground/20 hover:text-foreground",
                    )}
                  >
                    {shortName(name)}
                  </button>
                );
              })}
            </div>
          )}
          {needsTwo ? <p className="text-xs text-muted-foreground">Select at least two groups.</p> : null}
          {dateOrderBad ? <p className="text-xs text-destructive">The end date must be on or after the start date.</p> : null}
        </div>
      </div>

      {txQuery.isError ? (
        <StatusCard
          tone="error"
          icon={AlertCircle}
          title="Could not load transactions"
          detail={txQuery.error instanceof Error ? txQuery.error.message : "Failed to load intercompany transactions."}
        />
      ) : null}

      {applied && txQuery.isFetching && !report ? (
        <div className="flex items-center gap-3 rounded-2xl border border-border/80 bg-card px-5 py-8 text-sm text-muted-foreground shadow-soft" aria-busy="true">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          Adding up vouchers for the selected groups…
        </div>
      ) : null}

      {report ? (
        <>
          <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
            {(["sales", "purchase", "payment", "receipt"] as const).map((id) => {
              const item = MEASURES.find((m) => m.id === id)!;
              return (
                <button
                  key={id}
                  type="button"
                  onClick={() => setMeasure(id)}
                  className={cn(
                    "overflow-hidden rounded-2xl border bg-card text-left shadow-soft transition hover:border-foreground/20",
                    measure === id ? "border-primary ring-2 ring-primary/25" : "border-border/80",
                  )}
                >
                  <span className={cn("block h-1", item.dot)} />
                  <span className="block px-4 py-3.5 sm:px-5">
                    <span className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">
                      <span className={cn("h-1.5 w-1.5 rounded-full", item.dot)} />
                      {item.label}
                    </span>
                    <span className={cn("mt-2 block text-2xl font-semibold tabular-nums tracking-tight sm:text-[1.7rem]", item.tone)} title={`₹ ${formatInr(totals[id])}`}>
                      {formatCrore(totals[id])}
                    </span>
                    <span className="mt-1 block text-xs text-muted-foreground">{item.hint} · ₹ crore</span>
                  </span>
                </button>
              );
            })}
          </div>

          <section className="overflow-hidden rounded-2xl border border-border/80 bg-card shadow-soft">
            <div className="flex flex-col gap-4 border-b border-border/70 px-4 py-4 sm:px-5 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <h2 className="text-base font-semibold tracking-tight">Group vs group</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {formatAsOn(report.from)} – {formatAsOn(report.to)} · ₹ crore
                </p>
              </div>
              <div className="flex flex-wrap gap-1 rounded-xl border border-border/70 bg-background/50 p-1" role="tablist" aria-label="Amount to show">
                {MEASURES.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    role="tab"
                    aria-selected={measure === item.id}
                    onClick={() => setMeasure(item.id)}
                    className={cn(
                      "inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition",
                      measure === item.id ? "bg-card text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
                    )}
                  >
                    {item.id !== "all" ? <span className={cn("h-1.5 w-1.5 rounded-full", item.dot)} /> : null}
                    {item.label}
                  </button>
                ))}
              </div>
            </div>

            {gridGroups.length < 2 ? (
              <EmptyState icon={Inbox} title="Select at least two groups" detail="The matrix compares one group’s books with another. Pick a second company, then press Show." />
            ) : report.cells.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title="No transactions in this period"
                detail="These groups have no sales, purchase, payment or receipt with each other between these dates. Try a wider range or another set of companies."
              />
            ) : (
              <div className="max-h-[min(72vh,44rem)] overflow-auto overscroll-x-contain">
                <table className={cn("w-max border-separate border-spacing-0 text-sm", measure === "all" ? "min-w-[76rem]" : "min-w-[56rem]")}>
                  <thead>
                    <tr>
                      <th className="sticky top-0 left-0 z-30 min-w-[11rem] border-b border-r border-border/80 bg-card px-4 py-3 text-left text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground shadow-[4px_0_16px_-12px_rgba(0,0,0,0.65)]">
                        Books of
                      </th>
                      {gridGroups.map((name) => (
                        <th
                          key={name}
                          title={name}
                          className={cn(
                            "sticky top-0 z-20 border-b border-border/80 bg-card px-3 py-3 text-right text-sm font-semibold text-foreground",
                            measure === "all" ? "min-w-[11.5rem]" : "min-w-[8.75rem]",
                          )}
                        >
                          {shortName(name)}
                        </th>
                      ))}
                      <th className="sticky top-0 right-0 z-30 min-w-[9rem] border-b border-l border-border/80 bg-card px-4 py-3 text-right text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground shadow-[-4px_0_16px_-12px_rgba(0,0,0,0.65)]">
                        Total
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {gridGroups.map((book) => {
                      const rowCells = gridGroups
                        .filter((counter) => counter.toLowerCase() !== book.toLowerCase())
                        .map((counter) => byPair.get(cellKey(book, counter)));
                      const rowTotal: IntercompanyTransactionCell = {
                        bookGroup: book,
                        counterGroup: "",
                        sales: rowCells.reduce((n, c) => n + (c?.sales ?? 0), 0),
                        purchase: rowCells.reduce((n, c) => n + (c?.purchase ?? 0), 0),
                        payment: rowCells.reduce((n, c) => n + (c?.payment ?? 0), 0),
                        receipt: rowCells.reduce((n, c) => n + (c?.receipt ?? 0), 0),
                      };
                      return (
                        <tr key={book} className="group">
                          <th
                            scope="row"
                            title={book}
                            className="sticky left-0 z-10 min-w-[11rem] border-b border-r border-border/60 bg-card px-4 py-3.5 text-left text-sm font-semibold text-foreground shadow-[4px_0_16px_-12px_rgba(0,0,0,0.65)] group-hover:bg-muted"
                          >
                            {shortName(book)}
                          </th>
                          {gridGroups.map((counter) => {
                            const same = counter.toLowerCase() === book.toLowerCase();
                            const cell = byPair.get(cellKey(book, counter));
                            return (
                              <td key={counter} className={cn("border-b border-border/50 px-3 py-3.5 text-right align-middle tabular-nums group-hover:bg-muted/40", same && "bg-muted/25")}>
                                {same ? <span className="text-base text-muted-foreground/40">—</span> : <CellView cell={cell} measure={measure} />}
                              </td>
                            );
                          })}
                          <td className="sticky right-0 z-10 border-b border-l border-border/60 bg-card px-4 py-3.5 text-right align-middle font-semibold tabular-nums shadow-[-4px_0_16px_-12px_rgba(0,0,0,0.65)] group-hover:bg-muted">
                            <CellView cell={rowTotal} measure={measure} emphasize />
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
            <p className="border-t border-border/70 px-5 py-3.5 text-xs leading-relaxed text-muted-foreground">
              The row is the company that recorded the voucher. The column is the other group. Hover a name for the full legal name, and a figure for the rupee amount.
              Sales and purchase are the bill amount. Payment is money that row paid to the column. Receipt is money that row received from the column.
              A group with its own factories is blank. Scroll the grid sideways to compare the rest of the companies.
            </p>
          </section>
        </>
      ) : null}
    </div>
  );
}

function CellView({
  cell,
  measure,
  emphasize = false,
}: {
  cell: IntercompanyTransactionCell | undefined;
  measure: Measure;
  emphasize?: boolean;
}) {
  if (measure === "all") {
    return (
      <div className="ml-auto grid w-[10.5rem] grid-cols-[4.6rem_1fr] items-baseline gap-x-3 gap-y-1 text-left">
        {(["sales", "purchase", "payment", "receipt"] as const).map((id) => {
          const item = MEASURES.find((m) => m.id === id)!;
          const value = cell?.[id] ?? 0;
          return (
            <div key={id} className="contents" title={value ? `₹ ${formatInr(value)}` : undefined}>
              <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-muted-foreground">
                <span className={cn("h-1.5 w-1.5 shrink-0 rounded-full", item.dot)} />
                {item.label}
              </span>
              <span className={cn("text-right text-sm font-semibold tabular-nums", item.tone, value === 0 && "font-medium text-muted-foreground/45")}>
                {value ? formatCrore(value) : "—"}
              </span>
            </div>
          );
        })}
      </div>
    );
  }
  const value = amountOf(cell, measure);
  const tone = MEASURES.find((m) => m.id === measure)?.tone;
  return (
    <span
      className={cn("text-base tabular-nums", emphasize && "font-semibold", tone, value === 0 && "font-medium text-muted-foreground/45")}
      title={value ? `₹ ${formatInr(value)}` : undefined}
    >
      {value ? formatCrore(value) : "—"}
    </span>
  );
}

function EmptyState({ icon: Icon, title, detail }: { icon: typeof Inbox; title: string; detail: string }) {
  return (
    <div className="flex flex-col items-center px-6 py-16 text-center">
      <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl border border-border bg-muted/40 text-muted-foreground">
        <Icon className="h-5 w-5" />
      </div>
      <p className="text-sm font-semibold text-foreground">{title}</p>
      <p className="mt-1.5 max-w-md text-sm leading-relaxed text-muted-foreground">{detail}</p>
    </div>
  );
}

function StatusCard({
  tone,
  icon: Icon,
  title,
  detail,
}: {
  tone: "muted" | "error";
  icon: typeof Inbox;
  title: string;
  detail: string;
}) {
  return (
    <div
      role={tone === "error" ? "alert" : undefined}
      className={cn(
        "flex flex-col items-center rounded-2xl border border-dashed px-5 py-12 text-center shadow-soft",
        tone === "error" ? "border-destructive/30 bg-destructive/5" : "border-border bg-card",
      )}
    >
      <div
        className={cn(
          "mb-3 flex h-11 w-11 items-center justify-center rounded-xl",
          tone === "error" ? "bg-destructive/10 text-destructive" : "bg-primary/10 text-primary",
        )}
      >
        <Icon className="h-5 w-5" />
      </div>
      <p className={cn("text-sm font-semibold", tone === "error" ? "text-destructive" : "text-foreground")}>{title}</p>
      <p className="mt-1 max-w-md text-sm text-muted-foreground">{detail}</p>
    </div>
  );
}
