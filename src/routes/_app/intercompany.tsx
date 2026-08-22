import { useMemo, useRef, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  AlertCircle,
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  Building2,
  Inbox,
  Loader2,
  RefreshCw,
  Search,
} from "lucide-react";
import {
  formatAsOn,
  formatCrore,
  formatInr,
  getIntercompanyDashboard,
  type IntercompanyMatrix,
} from "@/lib/intercompany-api";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/_app/intercompany")({
  head: () => ({ meta: [{ title: "Intercompany — PO Portal" }] }),
  component: IntercompanyPage,
});

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Align ERP names: case-insensitive, drop Ltd/Limited/Pvt/Private and punctuation. */
function companyAlignKey(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\b(pvt|private|ltd|limited|llp)\b/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function amountTone(value: number, empty = false) {
  if (empty || value === 0) return "text-muted-foreground";
  if (value < 0) return "text-red-600";
  return "text-slate-700";
}

function buildBalanceGrid(matrices: IntercompanyMatrix[]) {
  const fromCompanies = matrices.map((m) => m.company).filter(Boolean);
  const fromByKey = new Map<string, string>();
  for (const name of fromCompanies) {
    const key = companyAlignKey(name);
    if (key && !fromByKey.has(key)) fromByKey.set(key, name);
  }

  const extraColumns: string[] = [];
  const extraSeen = new Set<string>();
  const cells = new Map<string, Map<string, number>>();

  const addCell = (from: string, to: string, value: number) => {
    if (!from || !to || companyAlignKey(from) === companyAlignKey(to)) return;
    let row = cells.get(from);
    if (!row) {
      row = new Map();
      cells.set(from, row);
    }
    row.set(to, (row.get(to) ?? 0) + value);
  };

  for (const matrix of matrices) {
    for (const [rawTo, rawValue] of Object.entries(matrix.amounts ?? {})) {
      const value = Number(rawValue) || 0;
      const mapped = fromByKey.get(companyAlignKey(rawTo));
      if (mapped) {
        addCell(matrix.company, mapped, value);
        continue;
      }
      const extra = rawTo.trim();
      if (!extra) continue;
      const extraKey = extra.toLowerCase();
      if (!extraSeen.has(extraKey)) {
        extraSeen.add(extraKey);
        extraColumns.push(extra);
      }
      addCell(matrix.company, extra, value);
    }
  }

  extraColumns.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
  const columns = [...fromCompanies, ...extraColumns];

  const rowTotals = fromCompanies.map((from) =>
    columns.reduce((sum, to) => sum + (cells.get(from)?.get(to) ?? 0), 0),
  );
  const colTotals = columns.map((to) =>
    fromCompanies.reduce((sum, from) => sum + (cells.get(from)?.get(to) ?? 0), 0),
  );
  const grandTotal = rowTotals.reduce((sum, n) => sum + n, 0);

  return { fromCompanies, extraColumns, columns, cells, rowTotals, colTotals, grandTotal };
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
        tone === "error"
          ? "border-destructive/30 bg-destructive/5"
          : "border-border bg-card",
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
      <p className={cn("text-sm font-semibold", tone === "error" ? "text-destructive" : "text-foreground")}>
        {title}
      </p>
      <p className="mt-1 max-w-md text-sm text-muted-foreground">{detail}</p>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="space-y-4" aria-busy="true" aria-live="polite">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin text-primary" />
        Loading intercompany balances…
      </div>
      <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
        <div className="h-11 bg-primary/90" />
        <div className="space-y-2 p-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-8 animate-pulse rounded-md bg-muted/70" />
          ))}
        </div>
      </div>
    </div>
  );
}

function BalanceMatrixSection({
  asOf,
  matrices,
}: {
  asOf: string;
  matrices: IntercompanyMatrix[];
}) {
  const grid = useMemo(() => buildBalanceGrid(matrices), [matrices]);
  if (grid.fromCompanies.length === 0) return null;

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
      <div className="flex flex-col items-center gap-0.5 bg-primary px-4 py-2.5 text-center text-primary-foreground">
        <h2 className="text-sm font-semibold uppercase tracking-[0.12em] md:text-[15px]">
          Inter-Company Balance Matrix
        </h2>
        <p className="text-[11px] font-medium text-primary-foreground/80">
          As on {formatAsOn(asOf)} · ₹ Crore
        </p>
      </div>
      <div className="w-full overflow-x-auto">
        <table className="w-full min-w-0 table-fixed border-collapse text-[10px] leading-tight sm:text-[11px]">
          <thead>
            <tr className="bg-primary/10">
              <th className="w-[11%] border border-border/80 px-1 py-1.5 text-left align-bottom font-semibold text-primary">
                From \ To
              </th>
              {grid.columns.map((name) => (
                <th
                  key={name}
                  className="break-words hyphens-auto whitespace-normal border border-border/80 px-1 py-1.5 text-center align-bottom font-semibold text-slate-800"
                >
                  {name}
                </th>
              ))}
              <th className="w-[8%] break-words border border-primary/40 bg-primary px-1 py-1.5 text-center align-bottom font-semibold text-primary-foreground">
                Total
              </th>
            </tr>
          </thead>
          <tbody>
            {grid.fromCompanies.map((from, rowIndex) => (
              <tr key={from} className="transition-colors hover:bg-primary/[0.06]">
                <td className="break-words border border-border/80 bg-primary/10 px-1 py-1 font-medium text-slate-800">
                  {from}
                </td>
                {grid.columns.map((to) => {
                  const isDiagonal = companyAlignKey(from) === companyAlignKey(to);
                  const value = isDiagonal ? 0 : (grid.cells.get(from)?.get(to) ?? 0);
                  return (
                    <td
                      key={`${from}-${to}`}
                      className={cn(
                        "break-all border border-border/80 px-1 py-1 text-right tabular-nums",
                        isDiagonal && "bg-muted",
                        !isDiagonal && value !== 0 && "bg-sky-50 font-medium",
                        amountTone(value, isDiagonal || value === 0),
                      )}
                    >
                      {isDiagonal || value === 0 ? "" : formatCrore(value)}
                    </td>
                  );
                })}
                <td
                  className={cn(
                    "break-all border border-primary/40 bg-primary px-1 py-1 text-right font-semibold tabular-nums text-primary-foreground",
                    grid.rowTotals[rowIndex] < 0 && "text-red-200",
                  )}
                >
                  {grid.rowTotals[rowIndex] ? formatCrore(grid.rowTotals[rowIndex]) : ""}
                </td>
              </tr>
            ))}
            <tr className="bg-primary">
              <td className="break-words border border-primary/40 px-1 py-1.5 font-semibold tracking-wide text-primary-foreground">
                Total
              </td>
              {grid.colTotals.map((total, i) => (
                <td
                  key={grid.columns[i]}
                  className={cn(
                    "break-all border border-primary/40 px-1 py-1.5 text-right font-semibold tabular-nums text-primary-foreground",
                    total < 0 && "text-red-200",
                  )}
                >
                  {total ? formatCrore(total) : ""}
                </td>
              ))}
              <td
                className={cn(
                  "break-all border border-primary/50 bg-primary px-1 py-1.5 text-right font-bold tabular-nums text-primary-foreground ring-1 ring-inset ring-white/20",
                  grid.grandTotal < 0 && "text-red-200",
                )}
              >
                {grid.grandTotal ? formatCrore(grid.grandTotal) : ""}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  );
}

function IntercompanyPage() {
  const [asOfInput, setAsOfInput] = useState(todayIso);
  const [asOf, setAsOf] = useState(todayIso);
  const [search, setSearch] = useState("");
  const [balanceSort, setBalanceSort] = useState<"asc" | "desc">("asc");
  const [selectedCompany, setSelectedCompany] = useState("");
  const [refreshToken, setRefreshToken] = useState(0);
  const bypassCacheRef = useRef(false);

  const query = useQuery({
    queryKey: ["intercompany-balances", asOf, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getIntercompanyDashboard(asOf, refresh);
    },
    staleTime: 60 * 60_000,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const report = query.data;
  const q = search.trim().toLowerCase();
  const lines = useMemo(() => {
    const all = report?.lines ?? [];
    const filtered = q ? all.filter((row) => row.company.toLowerCase().includes(q)) : all;
    const dir = balanceSort === "asc" ? 1 : -1;
    return [...filtered].sort((a, b) => (a.balance - b.balance) * dir);
  }, [report?.lines, q, balanceSort]);

  const matrices = report?.matrices ?? [];
  const counterparties = report?.counterparties ?? [];
  const companyNames = useMemo(() => matrices.map((m) => m.company).filter(Boolean), [matrices]);
  const activeCompany =
    selectedCompany && companyNames.some((name) => name === selectedCompany)
      ? selectedCompany
      : (companyNames[0] ?? "");
  const matrix = matrices.find((m) => m.company === activeCompany);
  const columns = useMemo(() => {
    if (!matrix) return [];
    const seen = new Set<string>();
    const ordered: string[] = [];
    const add = (name: string) => {
      const key = name.toLowerCase();
      if (!name || key === matrix.company.toLowerCase() || seen.has(key)) return;
      const value = matrix.amounts[name] ?? 0;
      if (!value) return;
      seen.add(key);
      ordered.push(name);
    };
    counterparties.forEach(add);
    Object.keys(matrix.amounts).forEach(add);
    return ordered.sort((a, b) => (matrix.amounts[a] ?? 0) - (matrix.amounts[b] ?? 0));
  }, [matrix, counterparties]);

  return (
    <div className="space-y-5">
      <div>
        <Link
          to="/ledgers"
          className="mb-2 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Ledgers
        </Link>
        <div className="mt-1 flex items-start gap-3">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Building2 className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Intercompany Balances</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Outstanding balances between group companies, as on the selected date.
            </p>
          </div>
        </div>
      </div>

      <div className="rounded-2xl border border-border bg-card p-3 shadow-soft sm:p-3.5">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[minmax(0,1.4fr)_11rem_auto] lg:items-end">
          {companyNames.length > 0 ? (
            <div className="min-w-0 space-y-1">
              <Label htmlFor="ic-company" className="text-xs text-muted-foreground">
                Company
              </Label>
              <select
                id="ic-company"
                value={activeCompany}
                onChange={(e) => setSelectedCompany(e.target.value)}
                className={SELECT_CLASS}
              >
                {companyNames.map((name) => (
                  <option key={name} value={name}>
                    {name}
                  </option>
                ))}
              </select>
            </div>
          ) : (
            <div className="min-w-0 space-y-1">
              <Label className="text-xs text-muted-foreground">Company</Label>
              <div className="flex h-9 items-center rounded-md border border-dashed border-border bg-muted/40 px-3 text-sm text-muted-foreground">
                Available after load
              </div>
            </div>
          )}
          <div className="space-y-1">
            <Label htmlFor="ic-asof" className="text-xs text-muted-foreground">
              As on
            </Label>
            <Input
              id="ic-asof"
              type="date"
              value={asOfInput}
              onChange={(e) => {
                setAsOfInput(e.target.value);
                setAsOf(e.target.value || todayIso());
              }}
              className="h-9 bg-background"
            />
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="h-9 gap-1.5"
            disabled={query.isFetching}
            onClick={() => {
              bypassCacheRef.current = true;
              setRefreshToken((n) => n + 1);
            }}
          >
            <RefreshCw className={cn("h-4 w-4", query.isFetching && "animate-spin")} />
            Refresh
          </Button>
        </div>
      </div>

      {query.isError ? (
        <StatusCard
          tone="error"
          icon={AlertCircle}
          title="Could not load balances"
          detail={
            query.error instanceof Error
              ? query.error.message
              : "Failed to load intercompany balances."
          }
        />
      ) : null}

      {query.isFetching && !report ? <LoadingSkeleton /> : null}

      {report && matrices.length === 0 && lines.length === 0 && !query.isFetching ? (
        <StatusCard
          tone="muted"
          icon={Inbox}
          title="No outstanding found"
          detail={`No intercompany outstanding as on ${formatAsOn(report.asOf)}. Try another date or refresh.`}
        />
      ) : null}

      {matrices.length > 0 ? <BalanceMatrixSection asOf={report?.asOf ?? asOf} matrices={matrices} /> : null}

      {matrix ? (
        <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
          <div className="flex flex-col gap-0.5 border-b border-border bg-primary/5 px-4 py-2.5 sm:flex-row sm:items-baseline sm:justify-between">
            <h2 className="text-sm font-semibold text-foreground">{matrix.company}</h2>
            <p className="text-[11px] text-muted-foreground">Single-company view · INR</p>
          </div>
          <div className="w-full overflow-x-auto">
            <table className="w-full min-w-0 table-fixed border-collapse text-[11px] leading-tight sm:text-xs">
              <thead>
                <tr className="bg-primary/10">
                  <th className="w-[16%] break-words border border-border/80 px-1.5 py-1.5 text-left align-bottom font-semibold text-primary">
                    Company
                  </th>
                  {columns.map((name) => (
                    <th
                      key={name}
                      className="break-words hyphens-auto whitespace-normal border border-border/80 px-1.5 py-1.5 text-right align-bottom font-semibold text-slate-800"
                    >
                      {name}
                    </th>
                  ))}
                  <th className="w-[11%] break-words border border-border/80 bg-primary px-1.5 py-1.5 text-right align-bottom font-semibold text-primary-foreground">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr className="transition-colors hover:bg-primary/[0.06]">
                  <td className="break-words border border-border/80 bg-primary/5 px-1.5 py-1.5 font-medium">
                    {matrix.company}
                  </td>
                  {columns.map((name) => {
                    const value = matrix.amounts[name] ?? 0;
                    return (
                      <td
                        key={name}
                        className={cn(
                          "break-all border border-border/80 px-1.5 py-1.5 text-right tabular-nums",
                          value !== 0 && "bg-sky-50/80 font-medium",
                          amountTone(value),
                        )}
                      >
                        {value ? formatInr(value) : ""}
                      </td>
                    );
                  })}
                  <td
                    className={cn(
                      "break-all border border-primary/40 bg-primary px-1.5 py-1.5 text-right font-semibold tabular-nums text-primary-foreground",
                      matrix.total < 0 && "text-red-200",
                    )}
                  >
                    {formatInr(matrix.total)}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
        <div className="flex flex-col gap-3 border-b border-border bg-primary px-4 py-3 text-primary-foreground sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-sm font-semibold uppercase tracking-[0.12em] md:text-[15px]">
              Detailed Inter-Company Balances
            </h2>
            <p className="mt-0.5 text-[11px] text-primary-foreground/75">
              As on {formatAsOn(report?.asOf ?? asOf)} · Search by company
            </p>
          </div>
          <div className="relative w-full sm:max-w-xs">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-primary/50" />
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search company…"
              className="h-9 border-white/20 bg-white pl-8 text-sm text-foreground placeholder:text-muted-foreground"
              aria-label="Search by company"
            />
          </div>
        </div>
        <div className="max-h-[min(70vh,36rem)] overflow-auto">
          <table className="w-full min-w-[36rem] table-fixed border-collapse text-xs sm:min-w-0 sm:text-sm">
            <thead className="sticky top-0 z-10">
              <tr className="bg-primary text-primary-foreground">
                <th className="w-[32%] px-3 py-2 text-left font-semibold">Company</th>
                <th className="w-[32%] px-3 py-2 text-left font-semibold">Counterparty</th>
                <th
                  className="w-[18%] px-3 py-2 text-right font-semibold"
                  aria-sort={balanceSort === "asc" ? "ascending" : "descending"}
                >
                  <button
                    type="button"
                    className="inline-flex w-full items-center justify-end gap-1 hover:underline"
                    onClick={() => setBalanceSort((d) => (d === "asc" ? "desc" : "asc"))}
                  >
                    Balance (₹)
                    {balanceSort === "asc" ? (
                      <ArrowUp className="h-3.5 w-3.5 shrink-0" aria-hidden />
                    ) : (
                      <ArrowDown className="h-3.5 w-3.5 shrink-0" aria-hidden />
                    )}
                  </button>
                </th>
                <th
                  className="w-[18%] px-3 py-2 text-right font-semibold"
                  aria-sort={balanceSort === "asc" ? "ascending" : "descending"}
                >
                  <button
                    type="button"
                    className="inline-flex w-full items-center justify-end gap-1 hover:underline"
                    onClick={() => setBalanceSort((d) => (d === "asc" ? "desc" : "asc"))}
                  >
                    Balance (₹ Cr.)
                    {balanceSort === "asc" ? (
                      <ArrowUp className="h-3.5 w-3.5 shrink-0" aria-hidden />
                    ) : (
                      <ArrowDown className="h-3.5 w-3.5 shrink-0" aria-hidden />
                    )}
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              {query.isFetching && lines.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-3 py-10 text-center text-muted-foreground">
                    <span className="inline-flex items-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin text-primary" />
                      Loading detailed balances…
                    </span>
                  </td>
                </tr>
              ) : lines.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-3 py-10 text-center text-muted-foreground">
                    {q ? `No companies match “${search.trim()}”.` : "No detailed balances for this date."}
                  </td>
                </tr>
              ) : (
                lines.map((row, i) => (
                  <tr
                    key={`${row.company}-${row.counterparty}-${i}`}
                    className={cn(
                      "border-b border-border/70 transition-colors hover:bg-primary/[0.06]",
                      i % 2 === 0 ? "bg-card" : "bg-muted/40",
                    )}
                  >
                    <td className="px-3 py-1.5">{row.company}</td>
                    <td className="px-3 py-1.5">{row.counterparty}</td>
                    <td className={cn("px-3 py-1.5 text-right tabular-nums font-medium", amountTone(row.balance))}>
                      {formatInr(row.balance)}
                    </td>
                    <td className={cn("px-3 py-1.5 text-right tabular-nums", amountTone(row.balanceCr))}>
                      {formatInr(row.balanceCr)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
