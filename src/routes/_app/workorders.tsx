import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useMemo, useState, useEffect } from "react";
import { Search, ArrowUpDown, Filter } from "lucide-react";
import { formatINR, type POStatus } from "@/lib/mock-data";
import { useAuth } from "@/lib/auth-context";
import { StatusBadge } from "@/components/StatusBadge";
import { useQuery } from "@tanstack/react-query";
import { useDebounce } from "@/hooks/useDebounce";
import { SkeletonPendingList } from "@/components/SkeletonLoader";

export const Route = createFileRoute("/_app/workorders")({
  head: () => ({ meta: [{ title: "Pending Work Orders — Approval Portal" }] }),
  component: PendingList,
});

function PendingList() {
  const { user } = useAuth();

  const [q, setQ] = useState("");
  const [amount, setAmount] = useState("");
  const [filterType, setFilterType] = useState("gte");
  const [status, setStatus] = useState<"All" | POStatus>("Pending");
  const [sortDesc, setSortDesc] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 20;

  // Debounce the amount input (500ms delay)
  const debouncedAmount = useDebounce(amount, 500);

  // Fetch work orders with React Query and debounced amount filter
  const { data: pendingPOs = [], isLoading } = useQuery({
    queryKey: ['workorder-list', user?.username, debouncedAmount, filterType],
    queryFn: async () => {
      if (!user?.username) throw new Error('No username');
      const response = await fetch(
        getApiUrl(
          `/api/WorkOrder/pending/${user.username}?amount=${debouncedAmount}&filterType=${filterType}`
        )
      );
      if (!response.ok) throw new Error('Failed to fetch work orders');
      return response.json();
    },
    staleTime: 0, // Always refetch to ensure fresh data after approvals
    refetchOnMount: 'always', // Always refetch when component mounts
    enabled: !!user?.username && typeof window !== 'undefined', // Only fetch on client
  });
 



 const filtered = pendingPOs
  .filter((p) => {
    const search = q.toLowerCase();

    const matchesSearch =
      p.PoNo?.toLowerCase().includes(search) ||
      p.ApprovalName?.toLowerCase().includes(search) ||
      p.Status?.toLowerCase().includes(search) ||
      String(p.Total || "").includes(search);

    const matchesStatus =
      status === "All" || p.Status === status;

    const workOrderAmount = Number(p.Total || 0);
    const enteredAmount = Number(amount || 0);

    const matchesAmount =
      amount === ""
        ? true
        : filterType === "gte"
        ? workOrderAmount >= enteredAmount
        : workOrderAmount <= enteredAmount;

    return matchesSearch && matchesStatus && matchesAmount;
  })
  .sort((a, b) =>
    sortDesc
      ? new Date(b.PODate).getTime() - new Date(a.PODate).getTime()
      : new Date(a.PODate).getTime() - new Date(b.PODate).getTime()
  );

  // Reset to page 1 when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [q, amount, status, filterType, sortDesc]);

  // Calculate pagination
  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedData = filtered.slice(startIndex, endIndex);


  return (
    <div className="space-y-5">
      <div className="flex items-end justify-between gap-3">
        <div>
         <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
  Work Orders
</h1>
          <p className="mt-1 text-sm text-muted-foreground">{filtered.length} result{filtered.length !== 1 ? "s" : ""} {totalPages > 1 && `(Page ${currentPage} of ${totalPages})`}</p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-3 md:flex-row md:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search PO number or vendor…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
        <div className="flex gap-2">
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="Amount"
            className="h-10 w-32 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="h-10 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          >
            <option value="gte">≥</option>
            <option value="lte">≤</option>
          </select>
          <div className="relative">
            <Filter className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as POStatus | "All")}
              className="h-10 appearance-none rounded-md border border-input bg-surface pl-9 pr-8 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              <option>All</option>
              <option>Pending</option>
              <option>Approved</option>
              <option>Rejected</option>
            </select>
          </div>
          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary"
          >
            <ArrowUpDown className="h-4 w-4" />
            {sortDesc ? "Newest" : "Oldest"}
          </button>
        </div>
      </div>

      {/* Mobile cards */}
      <div className="grid gap-3 md:hidden">
        {isLoading ? (
          <SkeletonPendingList />
        ) : filtered.length === 0 ? (
          <EmptyState />
        ) : (
          paginatedData.map((p) => (
            <Link
              key={p.PoNo}
              to="/workorder/$poNo"
              params={{ poNo: p.PoNo }}
              className="block rounded-xl border border-border bg-card p-4 shadow-sm active:scale-[.99]"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="font-semibold">{p.PoNo}</div>
                  <div className="mt-0.5 truncate text-sm text-muted-foreground">{p.ApprovalName}</div>
                </div>
                <StatusBadge status={p.Status} />
              </div>
              <div className="mt-3 flex items-end justify-between">
                <div className="text-xs text-muted-foreground">{p.PODate}</div>
                <div className="text-base font-semibold tabular-nums">
    {formatINR(p.Total || 0)}
  </div>
              </div>
            </Link>
          ))
        )}
      </div>

      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-xl border border-border bg-card md:block">
        {isLoading ? (
          <div className="p-6">
            <SkeletonPendingList />
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-medium">Work Order</th>
                  <th className="px-4 py-3 font-medium">Approved By</th>
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 text-right font-medium">Amount</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {paginatedData.map((p) => (
                  <tr key={p.PoNo} className="hover:bg-secondary/40">
                    <td className="px-4 py-3 font-medium">
                      <Link to="/workorder/$poNo" params={{ poNo: p.PoNo }} className="hover:text-primary hover:underline">
                        {p.PoNo}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{p.ApprovalName}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.PODate}</td>
                   <td className="px-4 py-3 text-right font-semibold tabular-nums">
    {formatINR(p.Total || 0)}
  </td>
                    <td className="px-4 py-3"><StatusBadge status={p.Status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
            {filtered.length === 0 && <EmptyState />}
          </>
        )}
      </div>

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-border bg-card px-4 py-3 sm:px-6 rounded-b-xl">
          <div className="flex flex-1 justify-between sm:hidden">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="relative inline-flex items-center rounded-md border border-input bg-surface px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Previous
            </button>
            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="relative ml-3 inline-flex items-center rounded-md border border-input bg-surface px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
            </button>
          </div>
          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Showing <span className="font-medium">{startIndex + 1}</span> to <span className="font-medium">{Math.min(endIndex, filtered.length)}</span> of{" "}
                <span className="font-medium">{filtered.length}</span> results
              </p>
            </div>
            <div>
              <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                <button
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="relative inline-flex items-center rounded-l-md border border-input bg-surface px-2 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Previous</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fillRule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clipRule="evenodd" />
                  </svg>
                </button>
                
                {/* Page numbers */}
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  let pageNum;
                  if (totalPages <= 5) {
                    pageNum = i + 1;
                  } else if (currentPage <= 3) {
                    pageNum = i + 1;
                  } else if (currentPage >= totalPages - 2) {
                    pageNum = totalPages - 4 + i;
                  } else {
                    pageNum = currentPage - 2 + i;
                  }
                  return (
                    <button
                      key={pageNum}
                      onClick={() => setCurrentPage(pageNum)}
                      className={`relative inline-flex items-center border border-input px-4 py-2 text-sm font-medium ${
                        currentPage === pageNum
                          ? "z-10 bg-primary text-primary-foreground"
                          : "bg-surface hover:bg-secondary"
                      }`}
                    >
                      {pageNum}
                    </button>
                  );
                })}

                <button
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="relative inline-flex items-center rounded-r-md border border-input bg-surface px-2 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Next</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fillRule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clipRule="evenodd" />
                  </svg>
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center gap-2 p-10 text-center">
      <div className="text-sm font-medium">No purchase orders found</div>
      <div className="text-xs text-muted-foreground">Try adjusting your search or filters.</div>
    </div>
  );
}
