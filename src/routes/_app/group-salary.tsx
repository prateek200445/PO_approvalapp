import { useMemo, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ArrowLeft, Download, Loader2, RefreshCw, Wallet } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  currentFyRange,
  formatInr,
  getGroupSalaryCompanies,
  getGroupSalaryDashboard,
  groupSalaryExcelUrl,
  monthLabel,
  type GroupSalaryMatrixRow,
} from "@/lib/group-salary-api";

export const Route = createFileRoute("/_app/group-salary")({
  head: () => ({ meta: [{ title: "Group Salary — PO Portal" }] }),
  component: GroupSalaryPage,
});

const SELECT =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20";

function GroupSalaryPage() {
  const fy = useMemo(() => currentFyRange(), []);
  const [dateFrom, setDateFrom] = useState(fy.dateFrom);
  const [dateTo, setDateTo] = useState(fy.dateTo);
  const [company, setCompany] = useState("All");
  const [tab, setTab] = useState<"company" | "ledger">("company");

  const companiesQuery = useQuery({
    queryKey: ["group-salary-companies"],
    queryFn: getGroupSalaryCompanies,
    staleTime: 5 * 60_000,
  });

  const filters = useMemo(
    () => ({
      dateFrom,
      dateTo,
      company: company === "All" ? undefined : company,
    }),
    [dateFrom, dateTo, company],
  );

  const dashQuery = useQuery({
    queryKey: ["group-salary", filters],
    queryFn: () => getGroupSalaryDashboard(filters),
    placeholderData: keepPreviousData,
    staleTime: 60_000,
  });

  const data = dashQuery.data;
  const rows = tab === "company" ? data?.companyRows ?? [] : data?.ledgerRows ?? [];
  const months = data?.months ?? [];

  return (
    <div className="space-y-4 pb-8">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <Link
            to="/ledgers"
            className="mb-2 inline-flex items-center gap-1 text-sm text-primary hover:underline"
          >
            <ArrowLeft className="h-4 w-4" /> Ledgers
          </Link>
          <div className="flex items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Wallet className="h-4.5 w-4.5" />
            </div>
            <div>
              <h1 className="text-2xl font-semibold tracking-tight">Group Salary</h1>
              <p className="text-sm text-muted-foreground">
                Month-wise salary &amp; allowance from <code className="text-xs">vw_LedgerSummary</code>
              </p>
            </div>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            className="h-9"
            disabled={dashQuery.isFetching}
            onClick={() => void dashQuery.refetch()}
          >
            {dashQuery.isFetching ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
            Refresh
          </Button>
          <Button type="button" className="h-9" asChild>
            <a href={groupSalaryExcelUrl(filters)}>
              <Download className="h-4 w-4" />
              Excel
            </a>
          </Button>
        </div>
      </div>

      <section className="grid grid-cols-1 gap-3 rounded-xl border border-border bg-card p-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="space-y-1.5">
          <Label className="text-xs">From</Label>
          <Input
            type="date"
            className="h-9"
            value={dateFrom}
            onChange={(e) => setDateFrom(e.target.value)}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs">To</Label>
          <Input type="date" className="h-9" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
        </div>
        <div className="space-y-1.5 lg:col-span-2">
          <Label className="text-xs">Company</Label>
          <select className={SELECT} value={company} onChange={(e) => setCompany(e.target.value)}>
            <option value="All">All Companies</option>
            {(companiesQuery.data ?? []).map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
      </section>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <Kpi
          label="Total"
          value={data ? `₹ ${formatInr(data.totalAmount)}` : "—"}
          loading={dashQuery.isLoading}
        />
        <Kpi label="Companies" value={String(data?.companies.length ?? "—")} loading={dashQuery.isLoading} />
        <Kpi label="Months" value={String(data?.months.length ?? "—")} loading={dashQuery.isLoading} />
        <Kpi label="Ledgers" value={String(data?.ledgers.length ?? "—")} loading={dashQuery.isLoading} />
      </div>

      <div className="flex gap-1 rounded-xl border border-border bg-secondary/40 p-1">
        <TabButton active={tab === "company"} onClick={() => setTab("company")}>
          Company × Month
        </TabButton>
        <TabButton active={tab === "ledger"} onClick={() => setTab("ledger")}>
          Ledger × Month
        </TabButton>
      </div>

      {dashQuery.isError ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
          {dashQuery.error instanceof Error ? dashQuery.error.message : "Failed to load"}
        </div>
      ) : null}

      <MatrixTable
        nameHeader={tab === "company" ? "Company" : "Ledger"}
        months={months}
        rows={rows}
        loading={dashQuery.isLoading}
      />

      <section className="rounded-xl border border-border bg-card p-4">
        <h2 className="text-sm font-semibold">Included ledgers</h2>
        <p className="mt-1 text-xs text-muted-foreground">
          Exact matches from <code>vw_LedgerSummary.LedgerName</code>
        </p>
        <ul className="mt-3 grid list-disc gap-1 pl-5 text-xs text-muted-foreground sm:grid-cols-2 lg:grid-cols-3">
          {(data?.ledgers ?? []).map((l) => (
            <li key={l}>{l}</li>
          ))}
        </ul>
      </section>
    </div>
  );
}

function MatrixTable({
  nameHeader,
  months,
  rows,
  loading,
}: {
  nameHeader: string;
  months: string[];
  rows: GroupSalaryMatrixRow[];
  loading: boolean;
}) {
  const monthTotals = months.map((m) => rows.reduce((s, r) => s + (r.amounts[m] ?? 0), 0));
  const grand = rows.reduce((s, r) => s + r.total, 0);

  return (
    <section className="overflow-hidden rounded-xl border border-border bg-card">
      <header className="border-b border-border px-4 py-3">
        <h2 className="text-sm font-semibold">{nameHeader}-wise salary</h2>
      </header>
      {loading && rows.length === 0 ? (
        <div className="flex items-center gap-2 p-8 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading…
        </div>
      ) : rows.length === 0 ? (
        <p className="p-8 text-sm text-muted-foreground">No salary postings in this period.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-sm">
            <thead className="bg-secondary/50 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="sticky left-0 z-10 bg-secondary/50 px-3 py-2.5 font-medium">{nameHeader}</th>
                {months.map((m) => (
                  <th key={m} className="px-3 py-2.5 text-right font-medium whitespace-nowrap">
                    {monthLabel(m)}
                  </th>
                ))}
                <th className="px-3 py-2.5 text-right font-medium">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {rows.map((r) => (
                <tr key={r.name} className="hover:bg-secondary/20">
                  <td className="sticky left-0 z-10 max-w-[240px] truncate bg-card px-3 py-2 font-medium" title={r.name}>
                    {r.name}
                  </td>
                  {months.map((m) => {
                    const v = r.amounts[m] ?? 0;
                    return (
                      <td
                        key={m}
                        className={cn(
                          "px-3 py-2 text-right tabular-nums whitespace-nowrap",
                          v === 0 && "text-muted-foreground",
                        )}
                      >
                        {v === 0 ? "—" : formatInr(v)}
                      </td>
                    );
                  })}
                  <td className="px-3 py-2 text-right font-semibold tabular-nums whitespace-nowrap">
                    {formatInr(r.total)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-border bg-secondary/30 font-semibold">
                <td className="sticky left-0 z-10 bg-secondary/30 px-3 py-2.5">Total</td>
                {monthTotals.map((v, i) => (
                  <td key={months[i]} className="px-3 py-2.5 text-right tabular-nums whitespace-nowrap">
                    {v === 0 ? "—" : formatInr(v)}
                  </td>
                ))}
                <td className="px-3 py-2.5 text-right tabular-nums whitespace-nowrap">{formatInr(grand)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </section>
  );
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex-1 rounded-lg px-3 py-2 text-sm font-medium transition",
        active ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
      )}
    >
      {children}
    </button>
  );
}

function Kpi({ label, value, loading }: { label: string; value: string; loading?: boolean }) {
  return (
    <div className="rounded-xl border border-border bg-card p-3">
      <div className="text-[11px] text-muted-foreground">{label}</div>
      <div className={cn("mt-0.5 text-lg font-semibold tabular-nums", loading && "animate-pulse")}>{value}</div>
    </div>
  );
}
