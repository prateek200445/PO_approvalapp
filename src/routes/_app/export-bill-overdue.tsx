import { useEffect, useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import {
  DEFAULT_EXPORT_GROUP,
  DEFAULT_PAGE_SIZE,
  PAGE_SIZES,
  formatBillAmount,
  formatDisplayDate,
  formatForeignAmount,
  getExportBillOverdue,
  getExportBillOverdueCompanies,
  getExportBillOverdueGroups,
  isAllCompanies,
  isInrCurrency,
  type ExportBillOverdueItem,
} from "@/lib/export-bill-overdue-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export const Route = createFileRoute("/_app/export-bill-overdue")({
  head: () => ({ meta: [{ title: "Export Bill Overdue — PO Portal" }] }),
  component: ExportBillOverduePage,
});

function todayIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function AmountBlock({ row }: { row: ExportBillOverdueItem }) {
  return (
    <div className="tabular-nums">
      <div className="font-semibold">₹ {formatBillAmount(row.billAmount)}</div>
      {!isInrCurrency(row.billCurrency) && row.foreignAmount > 0 ? (
        <div className="text-[11px] text-muted-foreground">
          {formatForeignAmount(row.billCurrency, row.foreignAmount)}
        </div>
      ) : !isInrCurrency(row.billCurrency) ? (
        <div className="text-[11px] text-muted-foreground">{row.billCurrency}</div>
      ) : null}
    </div>
  );
}

function MobileBillCard({ row }: { row: ExportBillOverdueItem }) {
  return (
    <article className="rounded-xl border border-border bg-card p-3.5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate text-sm font-semibold leading-snug">
            {row.customerName || row.ledgerName || "—"}
          </h3>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">{row.billNo || "—"}</p>
        </div>
        <div className="shrink-0 text-right text-sm">
          <AmountBlock row={row} />
        </div>
      </div>
      <dl className="mt-3 grid grid-cols-2 gap-2 text-xs">
        <div>
          <dt className="text-muted-foreground">Bill date</dt>
          <dd className="mt-0.5 font-medium">{formatDisplayDate(row.billDate)}</dd>
        </div>
        <div className="text-right">
          <dt className="text-muted-foreground">Due date</dt>
          <dd className="mt-0.5 font-medium">{formatDisplayDate(row.dueDate)}</dd>
        </div>
      </dl>
    </article>
  );
}

function PaginationBar({
  from,
  to,
  totalCount,
  pageSize,
  setPageSize,
  safePage,
  totalPages,
  isFetching,
  onPrev,
  onNext,
}: {
  from: number;
  to: number;
  totalCount: number;
  pageSize: number;
  setPageSize: (n: (typeof PAGE_SIZES)[number]) => void;
  safePage: number;
  totalPages: number;
  isFetching: boolean;
  onPrev: () => void;
  onNext: () => void;
}) {
  return (
    <footer className="flex flex-col gap-3 border-t border-border px-3 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-4">
      <p className="text-xs text-muted-foreground">
        {totalCount > 0 ? `Showing ${from} to ${to} of ${totalCount} entries` : "Loading page…"}
      </p>
      <div className="flex flex-wrap items-center gap-2">
        <Select
          value={String(pageSize)}
          onValueChange={(v) => setPageSize(Number(v) as (typeof PAGE_SIZES)[number])}
        >
          <SelectTrigger className="h-9 w-[88px] bg-background text-xs" aria-label="Rows per page">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {PAGE_SIZES.map((size) => (
              <SelectItem key={size} value={String(size)}>
                {size}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-9 min-w-[4.5rem]"
          disabled={safePage <= 1 || isFetching || totalCount === 0}
          onClick={onPrev}
          aria-label="Previous page"
        >
          Prev
        </Button>
        <span className="px-1 text-xs tabular-nums text-muted-foreground">
          {totalCount === 0 ? "—" : `${safePage} / ${totalPages}`}
        </span>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-9 min-w-[4.5rem]"
          disabled={safePage >= totalPages || isFetching || totalCount === 0}
          onClick={onNext}
          aria-label="Next page"
        >
          Next
        </Button>
      </div>
    </footer>
  );
}

function ExportBillOverduePage() {
  const queryClient = useQueryClient();
  const [company, setCompany] = useState("");
  const [groupName, setGroupName] = useState(DEFAULT_EXPORT_GROUP);
  const [asOfInput, setAsOfInput] = useState(todayIso);
  const [asOf, setAsOf] = useState(todayIso);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<(typeof PAGE_SIZES)[number]>(DEFAULT_PAGE_SIZE);

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["export-bill-overdue-companies"],
    queryFn: getExportBillOverdueCompanies,
    staleTime: 60 * 60_000,
  });

  const { data: groupList, isLoading: groupsLoading } = useQuery({
    queryKey: ["export-bill-overdue-groups"],
    queryFn: getExportBillOverdueGroups,
    staleTime: 60 * 60_000,
  });

  const companies = useMemo(() => companyList ?? [], [companyList]);
  const groups = useMemo(
    () => (groupList?.length ? groupList : [DEFAULT_EXPORT_GROUP]),
    [groupList],
  );

  // Pick first real company once list arrives.
  useEffect(() => {
    if (!companyList?.length) return;
    if (company && companyList.some((c) => c === company)) return;
    const first = companyList.find((c) => !isAllCompanies(c)) || companyList[0];
    setCompany(first);
    setPage(1);
  }, [companyList, company]);

  useEffect(() => {
    if (!groupList?.length) return;
    if (groupList.some((g) => g.toLowerCase() === groupName.toLowerCase())) return;
    const preferred =
      groupList.find((g) => g.toLowerCase() === DEFAULT_EXPORT_GROUP.toLowerCase()) ||
      groupList[0];
    setGroupName(preferred);
    setPage(1);
  }, [groupList, groupName]);

  // Debounce as-of date so calendar changes don't spam ERP.
  useEffect(() => {
    const t = window.setTimeout(() => {
      setAsOf((prev) => {
        if (prev === asOfInput) return prev;
        setPage(1);
        return asOfInput;
      });
    }, 350);
    return () => window.clearTimeout(t);
  }, [asOfInput]);

  const filtersReady = Boolean(company && groupName && asOf);

  const overdueQuery = useQuery({
    queryKey: ["export-bill-overdue", company, groupName, asOf, page, pageSize],
    queryFn: () =>
      getExportBillOverdue({
        company,
        groupName,
        asOf,
        page,
        pageSize,
      }),
    enabled: filtersReady,
    staleTime: 15 * 60_000,
    gcTime: 60 * 60_000,
    retry: 1,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const items = overdueQuery.data?.items ?? [];
  const totalCount = overdueQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, overdueQuery.data?.totalPages || 1);
  const safePage = Math.min(page, totalPages);
  const from = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const to = Math.min(safePage * pageSize, totalCount);
  const showSkeleton = overdueQuery.isFetching && items.length === 0;
  const isInitialLoad = overdueQuery.isFetching && !overdueQuery.isFetched;

  // Prefetch next page from API cache (usually instant after first load).
  useEffect(() => {
    if (!filtersReady || !overdueQuery.isSuccess) return;
    if (safePage >= totalPages) return;
    void queryClient.prefetchQuery({
      queryKey: ["export-bill-overdue", company, groupName, asOf, safePage + 1, pageSize],
      queryFn: () =>
        getExportBillOverdue({
          company,
          groupName,
          asOf,
          page: safePage + 1,
          pageSize,
        }),
      staleTime: 15 * 60_000,
    });
  }, [
    filtersReady,
    overdueQuery.isSuccess,
    safePage,
    totalPages,
    company,
    groupName,
    asOf,
    pageSize,
    queryClient,
  ]);

  const emptyMessage = filtersReady
    ? "No overdue export bills for this filter"
    : "Loading filters…";

  const pagination = (totalCount > 0 || (overdueQuery.isFetching && filtersReady)) && (
    <PaginationBar
      from={from}
      to={to}
      totalCount={totalCount}
      pageSize={pageSize}
      setPageSize={(n) => {
        setPageSize(n);
        setPage(1);
      }}
      safePage={safePage}
      totalPages={totalPages}
      isFetching={overdueQuery.isFetching}
      onPrev={() => setPage((p) => Math.max(1, p - 1))}
      onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
    />
  );

  return (
    <div className="space-y-4 pb-20 sm:space-y-5 md:pb-2">
      <div>
        <h1 className="text-xl font-semibold tracking-tight sm:text-2xl md:text-3xl">
          Export Bill Overdue
        </h1>
        <p className="mt-1 text-xs text-muted-foreground sm:text-sm">
          <span className="sm:hidden">
            Loads automatically when you change filters. First ERP load ~15–20s; later pages are instant.
          </span>
          <span className="hidden sm:inline">
            Data loads automatically when filters change. First ERP load is typically ~15–20s;
            later pages and revisits use cache and are near-instant.
          </span>
        </p>
      </div>

      <section
        className="rounded-xl border border-border bg-card p-3 shadow-sm sm:p-4"
        aria-label="Export bill overdue filters"
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[1fr_1fr_11rem]">
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="export-company">Company</Label>
            <Select
              value={company || undefined}
              onValueChange={(v) => {
                setCompany(v);
                setPage(1);
              }}
            >
              <SelectTrigger id="export-company" className="h-11 w-full bg-background text-sm">
                <SelectValue placeholder={companiesLoading ? "Loading…" : "Select company"} />
              </SelectTrigger>
              <SelectContent className="max-h-[50vh]">
                {companies.map((c, index) => (
                  <SelectItem key={`${index}-${c}`} value={c} className="text-sm">
                    {c}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="export-group">Group Name</Label>
            <Select
              value={groupName}
              onValueChange={(v) => {
                setGroupName(v);
                setPage(1);
              }}
            >
              <SelectTrigger id="export-group" className="h-11 w-full bg-background text-sm">
                <SelectValue placeholder="Select group" />
              </SelectTrigger>
              <SelectContent className="max-h-[50vh]">
                {groups.map((g, index) => (
                  <SelectItem key={`${index}-${g}`} value={g} className="text-sm">
                    {g}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="export-asof">As of date</Label>
            <Input
              id="export-asof"
              type="date"
              value={asOfInput}
              onChange={(e) => setAsOfInput(e.target.value)}
              className="h-11 bg-background text-sm"
            />
          </div>
        </div>
        {(companiesLoading || groupsLoading) && (
          <p className="mt-2 text-xs text-muted-foreground">Loading filters…</p>
        )}
        {isAllCompanies(company) && (
          <p className="mt-2 text-xs text-amber-700 dark:text-amber-400">
            “All Companies” is slow (every plant). Prefer one company for faster load.
          </p>
        )}
      </section>

      {overdueQuery.isFetching && (
        <div className="flex items-center gap-2 text-xs text-muted-foreground" aria-live="polite">
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          {isInitialLoad
            ? isAllCompanies(company)
              ? "Loading all companies from ERP (first time)…"
              : "Loading overdue bills from ERP (first time — next pages will be instant)…"
            : "Updating…"}
        </div>
      )}
      {overdueQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Failed to load.{" "}
          {overdueQuery.error instanceof Error ? overdueQuery.error.message : ""}
        </p>
      )}

      {/* Mobile cards */}
      <section className="md:hidden" aria-label="Overdue bills mobile">
        <header className="mb-2 px-0.5">
          <h2 className="text-sm font-semibold">Overdue bills</h2>
          <p className="text-[11px] text-muted-foreground">
            {filtersReady
              ? `Group: ${overdueQuery.data?.groupName || groupName}${
                  overdueQuery.data
                    ? ` · ${totalCount} bill(s) · ${formatDisplayDate(overdueQuery.data.asOf)}`
                    : ""
                }`
              : "Preparing…"}
          </p>
        </header>

        {showSkeleton ? (
          <div className="grid gap-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={`sk-m-${i}`}
                className="h-[5.5rem] animate-pulse rounded-xl border border-border bg-muted/60"
              />
            ))}
          </div>
        ) : !overdueQuery.isFetching && items.length === 0 ? (
          <div className="rounded-xl border border-dashed border-border bg-card px-4 py-10 text-center text-sm text-muted-foreground">
            {emptyMessage}
          </div>
        ) : (
          <div className="grid gap-3">
            {items.map((row, i) => (
              <MobileBillCard
                key={`${row.companyName}-${row.customerName}-${row.billNo}-${i}`}
                row={row}
              />
            ))}
          </div>
        )}

        {pagination ? (
          <div className="mt-3 overflow-hidden rounded-xl border border-border bg-card">
            {pagination}
          </div>
        ) : null}
      </section>

      {/* Desktop table */}
      <section className="hidden overflow-hidden rounded-xl border border-border bg-card shadow-sm md:block">
        <header className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-4 py-3">
          <div>
            <h2 className="text-sm font-semibold">Overdue bills</h2>
            <p className="text-[11px] text-muted-foreground">
              {filtersReady
                ? `Group: ${overdueQuery.data?.groupName || groupName} · ${
                    overdueQuery.data
                      ? `${totalCount} bill(s) · as of ${formatDisplayDate(overdueQuery.data.asOf)}`
                      : "loading…"
                  }`
                : "Preparing…"}
            </p>
          </div>
        </header>
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Customer name</TableHead>
                <TableHead>Bill no</TableHead>
                <TableHead>Bill date</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead>Due date</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {showSkeleton ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <TableRow key={`sk-d-${i}`}>
                    <TableCell colSpan={5} className="py-3">
                      <div className="h-4 w-full animate-pulse rounded bg-muted" />
                    </TableCell>
                  </TableRow>
                ))
              ) : !overdueQuery.isFetching && items.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={5}
                    className="py-8 text-center text-sm text-muted-foreground"
                  >
                    {emptyMessage}
                  </TableCell>
                </TableRow>
              ) : (
                items.map((row, i) => (
                  <TableRow key={`${row.companyName}-${row.customerName}-${row.billNo}-${i}`}>
                    <TableCell className="max-w-[220px] truncate font-medium sm:max-w-none">
                      {row.customerName || row.ledgerName || "—"}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-xs sm:text-sm">
                      {row.billNo || "—"}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-xs sm:text-sm">
                      {formatDisplayDate(row.billDate)}
                    </TableCell>
                    <TableCell className="text-right text-xs sm:text-sm whitespace-nowrap">
                      <AmountBlock row={row} />
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-xs sm:text-sm">
                      {formatDisplayDate(row.dueDate)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
        {pagination}
      </section>
    </div>
  );
}
