import { useEffect, useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Search, Square } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  formatIsoDate,
  formatQty,
  getItemStockCompanies,
  queryItemStock,
  type ItemStockResult,
} from "@/lib/item-stock-api";

export const Route = createFileRoute("/_app/item-stock")({
  head: () => ({ meta: [{ title: "Item Stock — PO Portal" }] }),
  component: ItemStockPage,
});

function todayIso() {
  const d = new Date();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${d.getFullYear()}-${m}-${day}`;
}

function monthStartIso() {
  const d = new Date();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  return `${d.getFullYear()}-${m}-01`;
}

function isAbortError(error: unknown) {
  return error instanceof Error && (error.name === "AbortError" || error.name === "CancelledError");
}

const PAGE_SIZE = 25;

const fieldClass =
  "flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring";

function ItemStockPage() {
  const [company, setCompany] = useState("HCP Plastene Bulkpack Ltd");
  const [itemCode, setItemCode] = useState("");
  const [dateFrom, setDateFrom] = useState(monthStartIso);
  const [dateTo, setDateTo] = useState(todayIso);
  const [submitted, setSubmitted] = useState<{
    companyName: string;
    itemCode: string;
    dateFrom: string;
    dateTo: string;
  } | null>(null);
  const [txnFilter, setTxnFilter] = useState("");
  const [rollFilter, setRollFilter] = useState("");
  const [searchNonce, setSearchNonce] = useState(0);
  const queryClient = useQueryClient();

  const companiesQuery = useQuery({
    queryKey: ["item-stock-companies"],
    queryFn: getItemStockCompanies,
    staleTime: 30 * 60_000,
  });

  const stockQuery = useQuery({
    queryKey: ["item-stock", submitted, searchNonce],
    queryFn: ({ signal }) => queryItemStock(submitted!, signal),
    enabled: !!submitted,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const companies = companiesQuery.data ?? [];
  const result = stockQuery.data;

  const txns = useMemo(() => {
    const rows = result?.transactions ?? [];
    const q = txnFilter.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((row) =>
      `${row.docNo} ${row.movementType} ${row.txnDate}`.toLowerCase().includes(q),
    );
  }, [result, txnFilter]);

  const rolls = useMemo(() => {
    const rows = result?.rolls ?? [];
    const q = rollFilter.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((row) => `${row.rollNo} ${row.godown}`.toLowerCase().includes(q));
  }, [result, rollFilter]);

  function onSearch() {
    const code = itemCode.trim();
    if (!company.trim() || !code || !dateFrom || !dateTo) {
      toast.error("Company, item code, and both dates are required.");
      return;
    }
    if (dateTo < dateFrom) {
      toast.error("Date to is before date from.");
      return;
    }
    setTxnFilter("");
    setRollFilter("");
    setSearchNonce((n) => n + 1);
    setSubmitted({ companyName: company, itemCode: code, dateFrom, dateTo });
  }

  function onAbort() {
    void queryClient.cancelQueries({ queryKey: ["item-stock"] });
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Item Stock</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Opening, movements, and closing for one item. Rolls are listed when the item is stored as rolls.
        </p>
      </div>

      <div className="grid gap-4 rounded-xl border border-border bg-card p-4 shadow-sm md:grid-cols-2 xl:grid-cols-[minmax(16rem,1.4fr)_10rem_10rem_10rem_auto] xl:items-end">
        <div className="space-y-1.5">
          <Label htmlFor="item-stock-company">Company</Label>
          <select
            id="item-stock-company"
            className={fieldClass}
            value={company}
            onChange={(e) => setCompany(e.target.value)}
          >
            {company && !companies.includes(company) && <option value={company}>{company}</option>}
            {companies.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="item-stock-code">Item code</Label>
          <Input
            id="item-stock-code"
            className="bg-background shadow-sm"
            value={itemCode}
            placeholder="WIP00023"
            onChange={(e) => setItemCode(e.target.value.toUpperCase())}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="item-stock-from">From</Label>
          <Input id="item-stock-from" className="bg-background shadow-sm" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="item-stock-to">To</Label>
          <Input id="item-stock-to" className="bg-background shadow-sm" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
        </div>
        <div className="flex flex-wrap gap-2 md:col-span-2 xl:col-span-1 xl:pb-px">
          <Button type="button" onClick={onSearch} disabled={stockQuery.isFetching}>
            {stockQuery.isFetching ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Search className="mr-2 h-4 w-4" />}
            Show stock
          </Button>
          {stockQuery.isFetching && (
            <Button type="button" variant="outline" onClick={onAbort}>
              <Square className="mr-2 h-4 w-4" />
              Abort
            </Button>
          )}
        </div>
      </div>

      {stockQuery.isError && !isAbortError(stockQuery.error) && (
        <p className="text-sm text-destructive">{(stockQuery.error as Error).message}</p>
      )}

      {result && <ItemStockTables result={result} txns={txns} rolls={rolls} txnFilter={txnFilter} rollFilter={rollFilter} onTxnFilter={setTxnFilter} onRollFilter={setRollFilter} />}
    </div>
  );
}

function usePage<T>(rows: T[], resetKey: string) {
  const [page, setPage] = useState(1);
  const totalRows = rows.length;
  const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));

  useEffect(() => {
    setPage(1);
  }, [resetKey, totalRows]);

  const safePage = Math.min(page, totalPages);
  const pageRows = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return rows.slice(start, start + PAGE_SIZE);
  }, [rows, safePage]);
  const rangeStart = totalRows === 0 ? 0 : (safePage - 1) * PAGE_SIZE + 1;
  const rangeEnd = Math.min(safePage * PAGE_SIZE, totalRows);

  return { pageRows, safePage, totalPages, totalRows, rangeStart, rangeEnd, setPage };
}

function Pager({
  rangeStart,
  rangeEnd,
  totalRows,
  safePage,
  totalPages,
  setPage,
}: {
  rangeStart: number;
  rangeEnd: number;
  totalRows: number;
  safePage: number;
  totalPages: number;
  setPage: (value: number | ((page: number) => number)) => void;
}) {
  if (totalRows === 0) return null;
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border bg-card px-3 py-2 text-xs">
      <span className="text-muted-foreground">
        {rangeStart}–{rangeEnd} of {totalRows.toLocaleString("en-IN")}
      </span>
      {totalPages > 1 && (
        <div className="flex items-center gap-1.5">
          <button
            type="button"
            disabled={safePage <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="rounded-md border border-border bg-background px-2.5 py-1 font-medium hover:bg-muted disabled:opacity-40"
          >
            Prev
          </button>
          <span className="tabular-nums text-muted-foreground">
            {safePage} / {totalPages}
          </span>
          <button
            type="button"
            disabled={safePage >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            className="rounded-md border border-border bg-background px-2.5 py-1 font-medium hover:bg-muted disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}

function ItemStockTables({
  result,
  txns,
  rolls,
  txnFilter,
  rollFilter,
  onTxnFilter,
  onRollFilter,
}: {
  result: ItemStockResult;
  txns: ItemStockResult["transactions"];
  rolls: ItemStockResult["rolls"];
  txnFilter: string;
  rollFilter: string;
  onTxnFilter: (value: string) => void;
  onRollFilter: (value: string) => void;
}) {
  const txnPage = usePage(txns, txnFilter);
  const rollPage = usePage(rolls, rollFilter);
  const head = "border-b border-border bg-card px-3 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground";
  const headRight = `${head} text-right`;

  return (
    <div className="space-y-6">
      <section className="overflow-hidden rounded-xl border border-border bg-card shadow-sm">
        <div className="border-b border-border px-4 py-3">
          <h2 className="text-lg font-semibold tracking-tight">
            {result.itemName || result.itemCode}{" "}
            <span className="text-sm font-normal text-muted-foreground">({result.itemCode})</span>
          </h2>
          <p className="text-xs text-muted-foreground">Quantities are in kg.</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[36rem] table-fixed text-sm">
            <thead>
              <tr>
                <th className={head}>Line</th>
                <th className={headRight}>Inward</th>
                <th className={headRight}>Outward</th>
                <th className={headRight}>Balance</th>
              </tr>
            </thead>
            <tbody>
              {result.summary.map((row, i) => {
                const strong = row.lineType === "Opening" || row.lineType === "Closing";
                return (
                  <tr
                    key={`${row.lineType}-${row.movementType}-${i}`}
                    className={`border-t border-border transition-colors hover:bg-muted/60 ${strong ? "bg-muted/40 font-medium" : ""}`}
                  >
                    <td className="px-3 py-2.5">{row.movementType || row.lineType}</td>
                    <td className="px-3 py-2.5 text-right tabular-nums">{row.lineType === "Opening" ? "" : formatQty(row.inwardQty)}</td>
                    <td className="px-3 py-2.5 text-right tabular-nums">{row.lineType === "Opening" ? "" : formatQty(row.outwardQty)}</td>
                    <td className="px-3 py-2.5 text-right font-medium tabular-nums">{formatQty(row.balance)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      <section className="overflow-hidden rounded-xl border border-border bg-card shadow-sm">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-border px-4 py-3">
          <h2 className="text-lg font-semibold tracking-tight">
            Transactions{" "}
            <span className="text-sm font-normal text-muted-foreground">({result.transactions.length.toLocaleString("en-IN")})</span>
          </h2>
          <Input
            className="max-w-xs bg-background shadow-sm"
            placeholder="Filter document or type"
            value={txnFilter}
            onChange={(e) => onTxnFilter(e.target.value)}
          />
        </div>
        {txnPage.totalRows === 0 ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">No transactions in this range.</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[48rem] table-fixed text-sm">
                <thead>
                  <tr>
                    <th className={`${head} w-[8.5rem]`}>Date</th>
                    <th className={head}>Type</th>
                    <th className={head}>Document</th>
                    <th className={`${headRight} w-[8rem]`}>Inward</th>
                    <th className={`${headRight} w-[8rem]`}>Outward</th>
                    <th className={`${headRight} w-[9rem]`}>Balance</th>
                  </tr>
                </thead>
                <tbody>
                  {txnPage.pageRows.map((row, i) => (
                    <tr key={`${row.txnDate}-${row.docNo}-${txnPage.rangeStart + i}`} className="border-t border-border transition-colors hover:bg-muted/60">
                      <td className="whitespace-nowrap px-3 py-2.5">{formatIsoDate(row.txnDate)}</td>
                      <td className="truncate px-3 py-2.5">{row.movementType}</td>
                      <td className="truncate px-3 py-2.5">{row.docNo || "—"}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{row.inwardQty ? formatQty(row.inwardQty) : ""}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{row.outwardQty ? formatQty(row.outwardQty) : ""}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{formatQty(row.balance)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager
              rangeStart={txnPage.rangeStart}
              rangeEnd={txnPage.rangeEnd}
              totalRows={txnPage.totalRows}
              safePage={txnPage.safePage}
              totalPages={txnPage.totalPages}
              setPage={txnPage.setPage}
            />
          </>
        )}
      </section>

      <section className="overflow-hidden rounded-xl border border-border bg-card shadow-sm">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-border px-4 py-3">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">
              Rolls{" "}
              <span className="text-sm font-normal text-muted-foreground">
                ({result.rollCount.toLocaleString("en-IN")} · {formatQty(result.rollNetWt)} kg)
              </span>
            </h2>
            {result.rollNote && <p className="mt-1 max-w-3xl text-xs text-muted-foreground">{result.rollNote}</p>}
          </div>
          {result.rollCount > 0 && (
            <Input
              className="max-w-xs bg-background shadow-sm"
              placeholder="Filter roll number"
              value={rollFilter}
              onChange={(e) => onRollFilter(e.target.value)}
            />
          )}
        </div>
        {result.rollCount === 0 ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">No rolls for this item on the end date.</p>
        ) : rollPage.totalRows === 0 ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">No rolls match that filter.</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[40rem] table-fixed text-sm">
                <thead>
                  <tr>
                    <th className={head}>Roll</th>
                    <th className={head}>Godown</th>
                    <th className={`${head} w-[9rem]`}>Produced</th>
                    <th className={`${headRight} w-[8rem]`}>Weight</th>
                  </tr>
                </thead>
                <tbody>
                  {rollPage.pageRows.map((row) => (
                    <tr key={row.rollNo} className="border-t border-border transition-colors hover:bg-muted/60">
                      <td className="truncate px-3 py-2.5 font-medium">{row.rollNo}</td>
                      <td className="truncate px-3 py-2.5">{row.godown}</td>
                      <td className="whitespace-nowrap px-3 py-2.5">{formatIsoDate(row.producedOn)}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{formatQty(row.netWt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager
              rangeStart={rollPage.rangeStart}
              rangeEnd={rollPage.rangeEnd}
              totalRows={rollPage.totalRows}
              safePage={rollPage.safePage}
              totalPages={rollPage.totalPages}
              setPage={rollPage.setPage}
            />
          </>
        )}
      </section>
    </div>
  );
}
