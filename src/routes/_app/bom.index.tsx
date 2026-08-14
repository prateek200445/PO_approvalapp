import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Loader2, Search, Layers, ArrowDown, ArrowUp } from "lucide-react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { DatePickerField } from "@/components/DatePickerField";
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
    () =>
      customers.map((c) => ({
        value: c.companyName,
        label: c.companyName,
      })),
    [customers],
  );

  const userOptions = useMemo(
    () => users.map((name) => ({ value: name, label: name })),
    [users],
  );

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

  function openBom(row: BomListItem) {
    void navigate({
      to: "/bom/$",
      params: { _splat: row.qtnNo },
    });
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6 p-4 pb-24 md:p-6 md:pb-8">
      <div className="flex items-start gap-3">
        <div className="rounded-lg bg-primary/10 p-2 text-primary">
          <Layers className="h-6 w-6" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">BOM Report</h1>
          <p className="text-sm text-muted-foreground">
            Party, date, or user wise list of saved BOMs. Click a row to open the full BOM.
          </p>
        </div>
      </div>

      <form
        onSubmit={handleSubmit}
        className="grid gap-4 rounded-xl border bg-card p-4 shadow-sm md:grid-cols-2 lg:grid-cols-3"
      >
        <label className="space-y-1 text-sm">
          <span className="font-medium text-muted-foreground">Party name</span>
          <SearchableSelect
            options={partyOptions}
            value={partyName}
            onChange={setPartyName}
            placeholder={loadingParties ? "Loading parties…" : "All parties"}
            emptyOptionLabel="All parties"
            searchPlaceholder="Search party…"
            emptyText={loadingParties ? "Loading parties…" : "No parties found"}
          />
        </label>

        <label className="space-y-1 text-sm">
          <span className="font-medium text-muted-foreground">User</span>
          <SearchableSelect
            options={userOptions}
            value={userName}
            onChange={setUserName}
            placeholder={loadingUsers ? "Loading users…" : "All users"}
            emptyOptionLabel="All users"
            searchPlaceholder="Search user…"
            emptyText={loadingUsers ? "Loading users…" : "No users found"}
          />
        </label>

        <label className="space-y-1 text-sm">
          <span className="font-medium text-muted-foreground">Search</span>
          <input
            type="search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Qtn no, party, user…"
            className="w-full rounded-md border bg-background px-3 py-2"
          />
        </label>

        <label className="space-y-1 text-sm">
          <span className="font-medium text-muted-foreground">Date from</span>
          <DatePickerField value={dateFrom} onChange={setDateFrom} placeholder="Date from" />
        </label>

        <label className="space-y-1 text-sm">
          <span className="font-medium text-muted-foreground">Date to</span>
          <DatePickerField value={dateTo} onChange={setDateTo} placeholder="Date to" />
        </label>

        <div className="flex items-end">
          <button
            type="submit"
            disabled={loading}
            className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
          >
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
            Search
          </button>
        </div>
      </form>

      <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
        {loading && !result ? (
          <div className="flex items-center justify-center gap-2 p-12 text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading BOMs…
          </div>
        ) : result && result.items.length === 0 ? (
          <p className="p-8 text-center text-muted-foreground">No BOMs found for the selected filters.</p>
        ) : result ? (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead className="border-b bg-muted/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3">Qtn No</th>
                    <th className="px-4 py-3">Party</th>
                    <th className="px-4 py-3">Bag type</th>
                    <th className="px-4 py-3">L × W × H</th>
                    <th className="px-4 py-3">
                      <button
                        type="button"
                        onClick={toggleDateSort}
                        className="inline-flex items-center gap-1 hover:text-foreground"
                        title={dateSortDesc ? "Newest first (click for oldest)" : "Oldest first (click for newest)"}
                      >
                        Date
                        {dateSortDesc ? (
                          <ArrowDown className="h-3.5 w-3.5" />
                        ) : (
                          <ArrowUp className="h-3.5 w-3.5" />
                        )}
                      </button>
                    </th>
                    <th className="px-4 py-3">User</th>
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((row) => (
                    <tr
                      key={`${row.qtnNo}-${row.srNo}`}
                      onClick={() => openBom(row)}
                      className={cn(
                        "cursor-pointer border-b transition-colors hover:bg-muted/50",
                        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                      )}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                          e.preventDefault();
                          openBom(row);
                        }
                      }}
                    >
                      <td className="px-4 py-3 font-medium">{row.qtnNo}</td>
                      <td className="max-w-[220px] truncate px-4 py-3" title={row.partyName}>
                        {row.partyName}
                      </td>
                      <td className="max-w-[200px] truncate px-4 py-3" title={row.bagType}>
                        {row.bagType || "—"}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {formatDimension(row.sizeL, row.sizeW, row.sizeH)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">{formatBomDate(row.date)}</td>
                      <td className="px-4 py-3">{row.user || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3 border-t px-4 py-3 text-sm text-muted-foreground">
              <span>
                {result.totalCount.toLocaleString()} BOM{result.totalCount === 1 ? "" : "s"}
                {" · "}
                {formatBomDate(dateFrom)} – {formatBomDate(dateTo)}
                {partyName ? ` · Party: ${partyName}` : ""}
                {userName ? ` · User: ${userName}` : ""}
              </span>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={page <= 1 || loading}
                  onClick={() => void runSearch(page - 1)}
                  className="rounded-md border px-3 py-1 disabled:opacity-50"
                >
                  Previous
                </button>
                <span>
                  Page {page} of {Math.max(result.totalPages, 1)}
                </span>
                <button
                  type="button"
                  disabled={page >= result.totalPages || loading}
                  onClick={() => void runSearch(page + 1)}
                  className="rounded-md border px-3 py-1 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        ) : null}
      </div>

      <p className="text-xs text-muted-foreground">
        Customer master updates are available on the{" "}
        <Link to="/bom/customers" className="text-primary underline-offset-2 hover:underline">
          customer list
        </Link>
        .
      </p>
    </div>
  );
}
