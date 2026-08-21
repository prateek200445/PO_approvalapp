import { useEffect, useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FileText, Loader2, RefreshCw, Search } from "lucide-react";
import { toast } from "sonner";
import {
  DEFAULT_EXPORT_GROUP,
  DEFAULT_PAGE_SIZE,
  PAGE_SIZES,
  downloadExportBillAgingPdf,
  downloadExportBillOverdueExcel,
  downloadExportBillOverduePdf,
  formatBillAmount,
  formatDisplayDate,
  formatForeignAmount,
  formatPeriod,
  financialYearStartIso,
  getExportBillAging,
  getExportBillOverdue,
  getExportBillOverdueCompanies,
  getExportBillOverdueGroups,
  isAllCompanies,
  isCompanyGroup,
  isInrCurrency,
  type ExportBillOverdueItem,
  type ExportCompanyOption,
  type ExportOverdueView,
} from "@/lib/export-bill-overdue-api";
import { ExportAgingReportView } from "@/components/export/ExportAgingReport";
import { cn } from "@/lib/utils";
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

function MobileBillCard({
  row,
  showCompany,
}: {
  row: ExportBillOverdueItem;
  showCompany: boolean;
}) {
  return (
    <article className="rounded-2xl border border-border bg-card p-3.5 shadow-soft">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate text-sm font-semibold leading-snug">
            {row.customerName || row.ledgerName || "—"}
          </h3>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">{row.billNo || "—"}</p>
          {showCompany && row.companyName ? (
            <p className="mt-0.5 truncate text-[11px] text-muted-foreground">{row.companyName}</p>
          ) : null}
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
    <footer className="flex flex-col gap-2 border-t border-border px-2 py-2 sm:flex-row sm:items-center sm:justify-between sm:px-3">
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
  const [company, setCompany] = useState("All Companies");
  const [groupName, setGroupName] = useState(DEFAULT_EXPORT_GROUP);
  const [dateFromInput, setDateFromInput] = useState(() => financialYearStartIso(todayIso()));
  const [dateFrom, setDateFrom] = useState(() => financialYearStartIso(todayIso()));
  const [asOfInput, setAsOfInput] = useState(todayIso);
  const [asOf, setAsOf] = useState(todayIso);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<(typeof PAGE_SIZES)[number]>(DEFAULT_PAGE_SIZE);
  const [view, setView] = useState<ExportOverdueView>("overdue");
  const [exporting, setExporting] = useState<"excel" | "pdf" | null>(null);
  const [refreshToken, setRefreshToken] = useState(0);
  const bypassCacheRef = useRef(false);

  const { data: companyPayload, isLoading: companiesLoading } = useQuery({
    queryKey: ["export-bill-overdue-companies"],
    queryFn: getExportBillOverdueCompanies,
    staleTime: 60 * 60_000,
  });

  const { data: groupList, isLoading: groupsLoading } = useQuery({
    queryKey: ["export-bill-overdue-groups"],
    queryFn: getExportBillOverdueGroups,
    staleTime: 60 * 60_000,
  });

  const companyOptions = useMemo<ExportCompanyOption[]>(
    () => (companyPayload?.options ?? []).filter((o) => o.value && o.label),
    [companyPayload],
  );
  const groupOptions = useMemo(
    () => companyOptions.filter((o) => o.kind === "group"),
    [companyOptions],
  );
  const factoryOptions = useMemo(
    () => companyOptions.filter((o) => o.kind === "company"),
    [companyOptions],
  );
  const groups = useMemo(
    () => (groupList?.length ? groupList : [DEFAULT_EXPORT_GROUP]),
    [groupList],
  );
  const companyLabel = useMemo(() => {
    if (isAllCompanies(company)) return "All Companies";
    return companyOptions.find((o) => o.value === company)?.label || company.replace(/^G-/i, "");
  }, [company, companyOptions]);
  const showCompanyColumn = isAllCompanies(company) || isCompanyGroup(company);

  useEffect(() => {
    if (!companyPayload) return;
    if (isAllCompanies(company)) return;
    if (companyOptions.some((o) => o.value === company)) return;
    setCompany("All Companies");
    setPage(1);
  }, [companyPayload, company, companyOptions]);

  useEffect(() => {
    if (!groupList?.length) return;
    if (groupList.some((g) => g.toLowerCase() === groupName.toLowerCase())) return;
    const preferred =
      groupList.find((g) => g.toLowerCase() === DEFAULT_EXPORT_GROUP.toLowerCase()) ||
      groupList[0];
    setGroupName(preferred);
    setPage(1);
  }, [groupList, groupName]);

  // Debounce From / To so calendar changes don't spam ERP.
  useEffect(() => {
    const t = window.setTimeout(() => {
      let nextFrom = dateFromInput;
      let nextTo = asOfInput;
      if (nextFrom && nextTo && nextFrom > nextTo) nextFrom = nextTo;
      setDateFrom((prev) => {
        if (prev === nextFrom) return prev;
        setPage(1);
        return nextFrom;
      });
      setAsOf((prev) => {
        if (prev === nextTo) return prev;
        setPage(1);
        return nextTo;
      });
    }, 350);
    return () => window.clearTimeout(t);
  }, [dateFromInput, asOfInput]);

  useEffect(() => {
    const t = window.setTimeout(() => {
      const next = searchInput.trim();
      setSearch((prev) => {
        if (prev === next) return prev;
        setPage(1);
        return next;
      });
    }, 300);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  const filtersReady = Boolean(company && groupName && dateFrom && asOf);

  const overdueQuery = useQuery({
    queryKey: ["export-bill-overdue", "erp-v3", company, groupName, dateFrom, asOf, search, page, pageSize, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      const data = await getExportBillOverdue({
        company,
        groupName,
        dateFrom,
        asOf,
        search,
        page,
        pageSize,
        refresh,
      });
      bypassCacheRef.current = false;
      return data;
    },
    enabled: filtersReady && view === "overdue",
    staleTime: 60 * 60_000,
    gcTime: 4 * 60 * 60_000,
    retry: 1,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const agingQuery = useQuery({
    queryKey: ["export-bill-aging", "erp-v3", company, groupName, dateFrom, asOf, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      const data = await getExportBillAging({
        company,
        groupName,
        dateFrom,
        asOf,
        refresh,
      });
      bypassCacheRef.current = false;
      return data;
    },
    enabled: filtersReady && view === "aging",
    staleTime: 60 * 60_000,
    gcTime: 4 * 60 * 60_000,
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
  const isUpdating = overdueQuery.isFetching && items.length > 0;
  const isInitialLoad = overdueQuery.isFetching && !overdueQuery.isFetched;

  // Prefetch next page from API cache (usually instant after first load).
  useEffect(() => {
    if (!filtersReady || view !== "overdue" || !overdueQuery.isSuccess) return;
    if (safePage >= totalPages) return;
    void queryClient.prefetchQuery({
      queryKey: ["export-bill-overdue", "erp-v3", company, groupName, dateFrom, asOf, search, safePage + 1, pageSize],
      queryFn: () =>
        getExportBillOverdue({
          company,
          groupName,
          dateFrom,
          asOf,
          search,
          page: safePage + 1,
          pageSize,
        }),
      staleTime: 60 * 60_000,
    });
  }, [
    filtersReady,
    view,
    overdueQuery.isSuccess,
    safePage,
    totalPages,
    company,
    groupName,
    dateFrom,
    asOf,
    search,
    pageSize,
    queryClient,
  ]);

  const emptyMessage = filtersReady
    ? search
      ? `No overdue bills match “${search}”`
      : "No overdue export bills for this filter"
    : "Loading filters…";

  const canExport =
    filtersReady &&
    (view === "aging" ? (agingQuery.data?.totalBills ?? 0) > 0 : totalCount > 0);

  async function exportExcel() {
    if (!canExport) {
      toast.error("Nothing to export");
      return;
    }
    setExporting("excel");
    try {
      await downloadExportBillOverdueExcel({ company, dateFrom, asOf, groupName });
      toast.success(view === "aging" ? "Excel downloaded (Overdue + Aging sheets)" : "Excel downloaded");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(null);
    }
  }

  async function exportPdf() {
    if (!canExport) {
      toast.error("Nothing to export");
      return;
    }
    setExporting("pdf");
    try {
      if (view === "aging") {
        await downloadExportBillAgingPdf({ company, dateFrom, asOf, groupName });
        toast.success("Aging PDF downloaded");
      } else {
        await downloadExportBillOverduePdf({ company, dateFrom, asOf, groupName });
        toast.success("Overdue PDF downloaded");
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "PDF export failed");
    } finally {
      setExporting(null);
    }
  }

  const exportButton = (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-9 shrink-0 gap-1.5"
        disabled={overdueQuery.isFetching || agingQuery.isFetching || !filtersReady}
        onClick={() => {
          bypassCacheRef.current = true;
          setRefreshToken((n) => n + 1);
        }}
        aria-label="Refresh overdue bills"
      >
        <RefreshCw className={`h-4 w-4 ${overdueQuery.isFetching ? "animate-spin" : ""}`} />
        Refresh
      </Button>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-9 shrink-0 gap-1.5"
        disabled={Boolean(exporting) || !canExport || overdueQuery.isFetching || agingQuery.isFetching}
        onClick={() => void exportExcel()}
        aria-label="Export to Excel"
      >
        {exporting === "excel" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
        Export Excel
      </Button>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-9 shrink-0 gap-1.5"
        disabled={Boolean(exporting) || !canExport || overdueQuery.isFetching || agingQuery.isFetching}
        onClick={() => void exportPdf()}
        aria-label="Export to PDF"
      >
        {exporting === "pdf" ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileText className="h-4 w-4" />}
        Export PDF
      </Button>
    </div>
  );

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
    <div className="export-report-fit @container flex min-h-0 min-w-0 flex-1 flex-col gap-2 overflow-hidden pb-0">
      <div className="relative z-20 flex shrink-0 flex-col gap-2 rounded-2xl border border-border bg-card p-2.5 shadow-soft sm:p-3">
        <div className="flex flex-col gap-2 @[40rem]:flex-row @[40rem]:items-center @[40rem]:justify-between">
          <h1 className="text-[clamp(1.05rem,2.6cqi,1.4rem)] font-semibold tracking-tight">
            Export Bill Overdue
          </h1>
          {exportButton}
        </div>
        <section
          className="grid grid-cols-1 gap-2 @[28rem]:grid-cols-2 @[56rem]:grid-cols-3 @[80rem]:grid-cols-[minmax(0,1.3fr)_minmax(0,1.1fr)_minmax(9rem,11rem)_minmax(9rem,11rem)_minmax(12rem,auto)]"
          aria-label="Export bill overdue filters"
        >
          <div className="min-w-0 space-y-1">
            <Label htmlFor="export-company" className="text-xs">Company</Label>
            <select
              id="export-company"
              value={company}
              disabled={companiesLoading}
              onChange={(e) => {
                setCompany(e.target.value || "All Companies");
                setPage(1);
              }}
              className="h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50"
            >
              <option value="All Companies">
                {companiesLoading ? "Loading…" : "All Companies"}
              </option>
              {groupOptions.length > 0 ? (
                <optgroup label="Groups">
                  {groupOptions.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </optgroup>
              ) : null}
              {factoryOptions.length > 0 ? (
                <optgroup label="Companies">
                  {factoryOptions.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </optgroup>
              ) : null}
            </select>
          </div>
          <div className="min-w-0 space-y-1">
            <Label htmlFor="export-group" className="text-xs">Group Name</Label>
            <Select
              value={groupName}
              onValueChange={(v) => {
                setGroupName(v);
                setPage(1);
              }}
            >
              <SelectTrigger id="export-group" className="h-9 w-full bg-background text-sm">
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
          <div className="min-w-0 space-y-1">
            <Label htmlFor="export-from" className="text-xs">From date</Label>
            <Input
              id="export-from"
              type="date"
              value={dateFromInput}
              max={asOfInput}
              onChange={(e) => setDateFromInput(e.target.value)}
              className="h-9 bg-background text-sm"
            />
          </div>
          <div className="min-w-0 space-y-1">
            <Label htmlFor="export-asof" className="text-xs">To date</Label>
            <Input
              id="export-asof"
              type="date"
              value={asOfInput}
              onChange={(e) => setAsOfInput(e.target.value)}
              className="h-9 bg-background text-sm"
            />
          </div>
          <fieldset className="min-w-0 space-y-1">
            <legend className="text-xs font-medium leading-none">Report</legend>
            <div
              className="flex h-9 rounded-md border border-border p-0.5"
              role="radiogroup"
              aria-label="Overdue bills or aging report"
            >
              {(
                [
                  { id: "overdue", label: "Overdue bills" },
                  { id: "aging", label: "Aging report" },
                ] as const
              ).map((opt) => (
                <button
                  key={opt.id}
                  type="button"
                  role="radio"
                  aria-checked={view === opt.id}
                  onClick={() => setView(opt.id)}
                  className={cn(
                    "flex-1 whitespace-nowrap rounded-sm px-2 text-[0.95em] font-medium transition-colors",
                    view === opt.id
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </fieldset>
        </section>
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search company, customer, bill no, date, amount…"
            className="h-9 bg-background pl-8 text-sm"
            aria-label="Search overdue bills and aging report"
          />
        </div>
        {(companiesLoading || groupsLoading) && (
          <p className="text-xs text-muted-foreground">Loading filters…</p>
        )}
      </div>

      {view === "overdue" && (showSkeleton || isUpdating) && (
        <div className="flex shrink-0 items-center gap-2 text-xs text-muted-foreground" aria-live="polite">
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          {showSkeleton
            ? isInitialLoad
              ? `Loading ${companyLabel}…`
              : "Updating…"
            : "Updating… previous rows stay visible."}
        </div>
      )}
      {view === "overdue" && overdueQuery.isError && (
        <p className="shrink-0 text-xs text-destructive" role="alert">
          Failed to load overdue bills. Please try again.
        </p>
      )}

      {view === "aging" && agingQuery.isFetching && agingQuery.data && (
        <div className="flex shrink-0 items-center gap-2 text-xs text-muted-foreground" aria-live="polite">
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          Updating aging report…
        </div>
      )}
      {view === "aging" && agingQuery.isError && (
        <p className="shrink-0 text-xs text-destructive" role="alert">
          Failed to load aging report. Please try again.
        </p>
      )}

      {view === "aging" ? (
        <ExportAgingReportView
          report={agingQuery.data}
          loading={agingQuery.isFetching && !agingQuery.data}
          showCompany={showCompanyColumn}
          companyLabel={companyLabel}
          search={search}
        />
      ) : (
        <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <section className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden @[56rem]:hidden" aria-label="Overdue bills compact">
        <header className="mb-2 shrink-0 px-0.5">
          <h2 className="text-sm font-semibold">Overdue bills</h2>
          <p className="text-[11px] text-muted-foreground">
            {filtersReady
              ? `${companyLabel} · Group: ${overdueQuery.data?.groupName || groupName}${
                  overdueQuery.data
                    ? ` · ${totalCount} bill(s) · ${formatPeriod(overdueQuery.data.dateFrom || dateFrom, overdueQuery.data.asOf)}`
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
          <div className="rounded-2xl border border-dashed border-border bg-card px-4 py-10 text-center text-sm text-muted-foreground">
            {emptyMessage}
          </div>
        ) : (
          <div className="min-h-0 flex-1 space-y-3 overflow-y-auto">
            {items.map((row, i) => (
              <MobileBillCard
                key={`${row.companyName}-${row.customerName}-${row.billNo}-${i}`}
                row={row}
                showCompany={showCompanyColumn}
              />
            ))}
          </div>
        )}

        {pagination ? (
          <div className="mt-2 shrink-0 overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
            {pagination}
          </div>
        ) : null}
      </section>

      <section className="hidden min-h-0 min-w-0 flex-1 flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-soft @[56rem]:flex">
        <header className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b border-border px-4 py-2">
          <div>
            <h2 className="text-sm font-semibold">Overdue bills</h2>
            <p className="text-[11px] text-muted-foreground">
              {filtersReady
                ? `${companyLabel} · Group: ${overdueQuery.data?.groupName || groupName} · ${
                    overdueQuery.data
                      ? `${totalCount} bill(s) · ${formatPeriod(overdueQuery.data.dateFrom || dateFrom, overdueQuery.data.asOf)}`
                      : "loading…"
                  }`
                : "Preparing…"}
            </p>
          </div>
        </header>
        <div className="min-h-0 min-w-0 flex-1 overflow-auto">
          <table className="w-full table-auto border-separate border-spacing-0 caption-bottom">
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                {showCompanyColumn ? (
                  <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Company</TableHead>
                ) : null}
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Customer name</TableHead>
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Bill no</TableHead>
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Bill date</TableHead>
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card text-right">Amount</TableHead>
                <TableHead className="sticky top-0 z-20 border-b border-border bg-card">Due date</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {showSkeleton ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <TableRow key={`sk-d-${i}`}>
                    <TableCell colSpan={showCompanyColumn ? 6 : 5} className="py-3">
                      <div className="h-4 w-full animate-pulse rounded bg-muted" />
                    </TableCell>
                  </TableRow>
                ))
              ) : !overdueQuery.isFetching && items.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={showCompanyColumn ? 6 : 5}
                    className="py-8 text-center text-sm text-muted-foreground"
                  >
                    {emptyMessage}
                  </TableCell>
                </TableRow>
              ) : (
                items.map((row, i) => (
                  <TableRow key={`${row.companyName}-${row.customerName}-${row.billNo}-${i}`}>
                    {showCompanyColumn ? (
                      <TableCell className="max-w-[28cqi] truncate">
                        {row.companyName || "—"}
                      </TableCell>
                    ) : null}
                    <TableCell className="max-w-[36cqi] truncate font-medium">
                      {row.customerName || row.ledgerName || "—"}
                    </TableCell>
                    <TableCell className="whitespace-nowrap">
                      {row.billNo || "—"}
                    </TableCell>
                    <TableCell className="whitespace-nowrap">
                      {formatDisplayDate(row.billDate)}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-right">
                      <AmountBlock row={row} />
                    </TableCell>
                    <TableCell className="whitespace-nowrap">
                      {formatDisplayDate(row.dueDate)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </table>
        </div>
        {pagination}
      </section>
        </div>
      )}
    </div>
  );
}
