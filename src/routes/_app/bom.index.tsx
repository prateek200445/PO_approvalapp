import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  CalendarRange,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Search,
  Users,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  BomFieldLabel,
  BomPageHeader,
  BomPageShell,
  BomPanel,
  BomRowChevron,
} from "@/components/bom/bom-ui";
import { SearchableSelect } from "@/components/SearchableSelect";
import { DatePickerField } from "@/components/DatePickerField";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { fetchBomCustomers, fetchBomUsers, searchBom } from "@/lib/bom-api";
import {
  defaultBomDateFrom,
  formatBomDate,
  formatDimension,
  toInputDate,
  type BomListItem,
  type BomSearchResult,
  type BomCustomerOption,
} from "@/lib/bom-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/bom/")({
  head: () => ({ meta: [{ title: "BOM Report — PO Portal" }] }),
  component: BomReportPage,
});

const PAGE_SIZE = 20;

type DatePreset = "3m" | "ytd" | "2024" | "2025" | "2026";

function normalizeDateRange(from: string, to: string): { from: string; to: string } {
  if (!from || !to || from <= to) return { from, to };
  return { from: to, to: from };
}

function BomReportPage() {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<BomCustomerOption[]>([]);
  const [users, setUsers] = useState<string[]>([]);
  const [partyName, setPartyName] = useState("");
  const [userName, setUserName] = useState("");
  const [dateFrom, setDateFrom] = useState(defaultBomDateFrom());
  const [dateTo, setDateTo] = useState(toInputDate(new Date()));
  const [searchText, setSearchText] = useState("");
  const [page, setPage] = useState(1);
  const [loadingParties, setLoadingParties] = useState(true);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [loading, setLoading] = useState(false);
  const [dateSortDesc, setDateSortDesc] = useState(true);
  const [result, setResult] = useState<BomSearchResult | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingUsers(true);
      try {
        const userRows = await fetchBomUsers();
        if (!cancelled) setUsers(userRows);
      } catch (err: unknown) {
        if (!cancelled) toast.error(err instanceof Error ? err.message : "Failed to load users");
      } finally {
        if (!cancelled) setLoadingUsers(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingParties(true);
      try {
        const customerRows = await fetchBomCustomers();
        if (!cancelled) setCustomers(customerRows.filter((c) => c.companyName));
      } catch (err: unknown) {
        if (!cancelled) toast.error(err instanceof Error ? err.message : "Failed to load parties");
      } finally {
        if (!cancelled) setLoadingParties(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const partyOptions = useMemo(
    () => customers.map((c) => ({ value: c.companyName, label: c.companyName })),
    [customers],
  );

  const userOptions = useMemo(
    () => users.map((name) => ({ value: name, label: name })),
    [users],
  );

  const hasActiveFilters = Boolean(partyName || userName || searchText.trim());

  const runSearch = useCallback(
    async (targetPage = 1, sortDesc = dateSortDesc) => {
      setLoading(true);
      try {
        const range = normalizeDateRange(dateFrom, dateTo);
        const data = await searchBom({
          dateFrom: range.from,
          dateTo: range.to,
          partyName: partyName || undefined,
          userName: userName || undefined,
          search: searchText.trim() || undefined,
          page: targetPage,
          pageSize: PAGE_SIZE,
          sortDirection: sortDesc ? "desc" : "asc",
        });
        setResult(data);
        setPage(targetPage);
      } catch (err: unknown) {
        toast.error(err instanceof Error ? err.message : "Search failed");
      } finally {
        setLoading(false);
      }
    },
    [dateFrom, dateTo, partyName, userName, searchText, dateSortDesc],
  );

  useEffect(() => {
    if (loadingUsers) return;
    const timer = window.setTimeout(() => void runSearch(1), 350);
    return () => window.clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-search only when dates change
  }, [dateFrom, dateTo, loadingUsers]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    void runSearch(1);
  }

  function toggleDateSort() {
    const next = !dateSortDesc;
    setDateSortDesc(next);
    void runSearch(1, next);
  }

  function applyDatePreset(preset: DatePreset) {
    const year = new Date().getFullYear();
    const today = toInputDate(new Date());
    switch (preset) {
      case "3m":
        setDateFrom(defaultBomDateFrom());
        setDateTo(today);
        break;
      case "ytd":
        setDateFrom(`${year}-01-01`);
        setDateTo(today);
        break;
      case "2024":
        setDateFrom("2024-01-01");
        setDateTo("2024-12-31");
        break;
      case "2025":
        setDateFrom("2025-01-01");
        setDateTo("2025-12-31");
        break;
      case "2026":
        setDateFrom("2026-01-01");
        setDateTo(today);
        break;
    }
  }

  function clearFilters() {
    setPartyName("");
    setUserName("");
    setSearchText("");
    void runSearch(1);
  }

  function openBom(row: BomListItem) {
    void navigate({ to: "/bom/$", params: { _splat: row.qtnNo } });
  }

  return (
    <BomPageShell>
      <BomPageHeader
        title="Bill of Materials"
        description="Search saved BOMs by party, user, or date. Select a row to view the full report, PDF, and email."
        actions={
          <Button variant="outline" size="sm" asChild>
            <Link to="/bom/customers">Customer master</Link>
          </Button>
        }
      />

      <BomPanel title="Search & filters" className="mb-5">
        <form onSubmit={handleSubmit} className="space-y-4 p-4 md:p-5">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <label className="space-y-1.5">
              <BomFieldLabel>Party</BomFieldLabel>
              <SearchableSelect
                options={partyOptions}
                value={partyName}
                onChange={setPartyName}
                placeholder={loadingParties ? "Loading…" : "All parties"}
                emptyOptionLabel="All parties"
                searchPlaceholder="Search party…"
                emptyText={loadingParties ? "Loading…" : "No parties found"}
              />
            </label>

            <label className="space-y-1.5">
              <BomFieldLabel>User</BomFieldLabel>
              <SearchableSelect
                options={userOptions}
                value={userName}
                onChange={setUserName}
                placeholder={loadingUsers ? "Loading…" : "All users"}
                emptyOptionLabel="All users"
                searchPlaceholder="Search user…"
                emptyText={loadingUsers ? "Loading…" : "No users found"}
              />
            </label>

            <label className="space-y-1.5 sm:col-span-2 lg:col-span-1">
              <BomFieldLabel>Quick search</BomFieldLabel>
              <div className="relative">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  type="search"
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                  placeholder="Qtn no, party, user…"
                  className="h-10 bg-background pl-9"
                />
              </div>
            </label>
          </div>

          <div className="flex flex-col gap-3 border-t border-border/60 pt-4 lg:flex-row lg:items-end">
            <div className="grid flex-1 gap-3 sm:grid-cols-2">
              <label className="space-y-1.5">
                <BomFieldLabel>From</BomFieldLabel>
                <DatePickerField value={dateFrom} onChange={setDateFrom} placeholder="Start date" />
              </label>
              <label className="space-y-1.5">
                <BomFieldLabel>To</BomFieldLabel>
                <DatePickerField value={dateTo} onChange={setDateTo} placeholder="End date" />
              </label>
            </div>
            <div className="flex flex-wrap gap-2 lg:pb-0.5">
              {(
                [
                  ["3m", "Last 3 mo"],
                  ["ytd", "YTD"],
                  ["2024", "2024"],
                  ["2025", "2025"],
                  ["2026", "2026"],
                ] as const
              ).map(([key, label]) => (
                <Button
                  key={key}
                  type="button"
                  variant="secondary"
                  size="sm"
                  className="h-8"
                  onClick={() => applyDatePreset(key)}
                >
                  {label}
                </Button>
              ))}
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button type="submit" disabled={loading} className="min-w-[120px]">
              {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
              Search
            </Button>
            {hasActiveFilters ? (
              <Button type="button" variant="ghost" size="sm" onClick={clearFilters}>
                <X className="h-4 w-4" />
                Clear filters
              </Button>
            ) : null}
          </div>
        </form>
      </BomPanel>

      <BomPanel
        title="Results"
        subtitle={
          result
            ? `${result.totalCount.toLocaleString()} BOM${result.totalCount === 1 ? "" : "s"} · ${formatBomDate(dateFrom)} – ${formatBomDate(dateTo)}`
            : "Adjust filters and search"
        }
        headerRight={
          result ? (
            <Badge variant="outline" className="font-normal">
              {dateSortDesc ? "Newest first" : "Oldest first"}
            </Badge>
          ) : null
        }
      >
        {loading && !result ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
            <p className="text-sm">Loading BOMs…</p>
          </div>
        ) : result && result.items.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-2 py-16 text-center">
            <CalendarRange className="h-10 w-10 text-muted-foreground/40" />
            <p className="font-medium text-foreground">No BOMs found</p>
            <p className="max-w-sm text-sm text-muted-foreground">
              Try widening the date range or clearing party / user filters.
            </p>
          </div>
        ) : result ? (
          <>
            <div className={cn("relative", loading && "pointer-events-none opacity-60")}>
              {loading ? (
                <div className="absolute inset-0 z-10 flex items-center justify-center bg-background/40">
                  <Loader2 className="h-6 w-6 animate-spin text-amber-600" />
                </div>
              ) : null}

              {/* Desktop table */}
              <div className="hidden overflow-x-auto md:block">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/60 bg-muted/30 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                      <th className="px-4 py-3">Quotation</th>
                      <th className="px-4 py-3">Party</th>
                      <th className="px-4 py-3">Bag type</th>
                      <th className="px-4 py-3">Dimensions</th>
                      <th className="px-4 py-3">
                        <button
                          type="button"
                          onClick={toggleDateSort}
                          className="inline-flex items-center gap-1 transition-colors hover:text-foreground"
                        >
                          Date
                          {dateSortDesc ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />}
                        </button>
                      </th>
                      <th className="px-4 py-3">User</th>
                      <th className="w-10 px-2 py-3" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/50">
                    {result.items.map((row) => (
                      <tr
                        key={`${row.qtnNo}-${row.srNo}`}
                        onClick={() => openBom(row)}
                        className="group cursor-pointer transition-colors hover:bg-amber-500/[0.04]"
                        tabIndex={0}
                        onKeyDown={(e) => {
                          if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            openBom(row);
                          }
                        }}
                      >
                        <td className="px-4 py-3.5">
                          <span className="font-mono text-[13px] font-medium text-foreground">{row.qtnNo}</span>
                        </td>
                        <td className="max-w-[240px] truncate px-4 py-3.5" title={row.partyName}>
                          {row.partyName}
                        </td>
                        <td className="max-w-[200px] truncate px-4 py-3.5 text-muted-foreground" title={row.bagType}>
                          {row.bagType || "—"}
                        </td>
                        <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-muted-foreground">
                          {formatDimension(row.sizeL, row.sizeW, row.sizeH)}
                        </td>
                        <td className="whitespace-nowrap px-4 py-3.5">{formatBomDate(row.date)}</td>
                        <td className="px-4 py-3.5">
                          {row.user ? (
                            <span className="inline-flex items-center gap-1 text-muted-foreground">
                              <Users className="h-3 w-3" />
                              {row.user}
                            </span>
                          ) : (
                            "—"
                          )}
                        </td>
                        <td className="px-2 py-3.5">
                          <BomRowChevron />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Mobile cards */}
              <div className="divide-y divide-border/50 md:hidden">
                {result.items.map((row) => (
                  <button
                    key={`${row.qtnNo}-${row.srNo}-m`}
                    type="button"
                    onClick={() => openBom(row)}
                    className="group flex w-full items-start gap-3 px-4 py-4 text-left transition-colors hover:bg-amber-500/[0.04]"
                  >
                    <div className="min-w-0 flex-1 space-y-1">
                      <p className="font-mono text-sm font-semibold">{row.qtnNo}</p>
                      <p className="truncate text-sm text-muted-foreground">{row.partyName}</p>
                      <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                        <span>{formatBomDate(row.date)}</span>
                        {row.user ? <span>{row.user}</span> : null}
                        <span>{formatDimension(row.sizeL, row.sizeW, row.sizeH)}</span>
                      </div>
                      {row.bagType ? (
                        <p className="truncate text-xs text-muted-foreground/80">{row.bagType}</p>
                      ) : null}
                    </div>
                    <BomRowChevron />
                  </button>
                ))}
              </div>
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border/60 bg-muted/10 px-4 py-3">
              <p className="text-xs text-muted-foreground">
                Page {page} of {Math.max(result.totalPages, 1)}
                {partyName ? ` · ${partyName}` : ""}
                {userName ? ` · ${userName}` : ""}
              </p>
              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page <= 1 || loading}
                  onClick={() => void runSearch(page - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page >= result.totalPages || loading}
                  onClick={() => void runSearch(page + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </>
        ) : null}
      </BomPanel>
    </BomPageShell>
  );
}
