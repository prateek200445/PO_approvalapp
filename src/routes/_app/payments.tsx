import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useState, useEffect } from "react";
import { Search, ArrowUpDown, Filter, X } from "lucide-react";
import { formatINR, type POStatus } from "@/lib/mock-data";
import { useAuth } from "@/lib/auth-context";
import { StatusBadge } from "@/components/StatusBadge";
import { useQuery } from "@tanstack/react-query";
import { useDebounce } from "@/hooks/useDebounce";
import { SkeletonPendingList } from "@/components/SkeletonLoader";

export const Route = createFileRoute("/_app/payments")({
  head: () => ({ meta: [{ title: "Pending Payments — Approval Portal" }] }),
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
  const [showFilterSheet, setShowFilterSheet] = useState(false);
  const itemsPerPage = 20;

  // Debounce the amount input (500ms delay)
  const debouncedAmount = useDebounce(amount, 500);

  // Fetch payments with React Query and debounced amount filter
  const { data: pendingPayments = [], isLoading } = useQuery({
    queryKey: ['payment-list', user?.username, debouncedAmount, filterType],
    queryFn: async () => {
      if (!user?.username) throw new Error('No username');
      const response = await fetch(
        getApiUrl(
          `/api/Payment/pending/${user.username}?amount=${debouncedAmount}&filterType=${filterType}`
        )
      );
      if (!response.ok) throw new Error('Failed to fetch payments');
      return response.json();
    },
    staleTime: 0, // Always refetch to ensure fresh data after approvals
    refetchOnMount: 'always', // Always refetch when component mounts
    enabled: !!user?.username && typeof window !== 'undefined', // Only fetch on client
  });

  const filtered = pendingPayments
    .filter((p) => {
      const search = q.toLowerCase();

      const matchesSearch =
        p.paymentNo?.toLowerCase().includes(search) ||
        p.VendorName?.toLowerCase().includes(search) ||
        String(p.paymentAmount || "").includes(search);

      const matchesStatus = status === "All" || status === "Pending";

      const paymentAmount = Number(p.paymentAmount || 0);
      const enteredAmount = Number(amount || 0);

      const matchesAmount =
        amount === ""
          ? true
          : filterType === "gte"
            ? paymentAmount >= enteredAmount
            : paymentAmount <= enteredAmount;

      return matchesSearch && matchesStatus && matchesAmount;
    })
    .sort((a, b) =>
      sortDesc
        ? new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime()
        : new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime(),
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

  console.log(filtered);
  return (
    <div className="space-y-5">
      <div className="flex items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
            Payment Approvals
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {filtered.length} result{filtered.length !== 1 ? "s" : ""} {totalPages > 1 && `(Page ${currentPage} of ${totalPages})`}
          </p>
        </div>
      </div>

      {/* Filters - Desktop Only */}
      <div className="hidden md:flex md:flex-col md:gap-3">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search Payment Number or Vendor..."
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

      {/* Filters - Mobile Only */}
      <div className="space-y-2 md:hidden">
        {/* Search Bar */}
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search Payment…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>

        {/* Active Filters + Filter Button Row */}
        <div className="flex items-center gap-2">
          {/* Active Filter Chips */}
          <div className="flex flex-wrap gap-1 flex-1">
            {amount && (
              <button
                onClick={() => setAmount("")}
                className="inline-flex items-center gap-1 rounded-full bg-primary/20 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/30 transition"
              >
                {filterType} ₹{amount}
                <X className="h-3 w-3" />
              </button>
            )}
          </div>

          {/* Filter Button */}
          <button
            onClick={() => setShowFilterSheet(true)}
            className="inline-flex h-10 items-center gap-2 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary whitespace-nowrap"
          >
            <Filter className="h-4 w-4" />
            <span className="text-xs">Filters</span>
            {amount && (
              <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                1
              </span>
            )}
          </button>

          {/* Sort Button */}
          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-input bg-surface px-2 text-xs font-medium hover:bg-secondary whitespace-nowrap"
          >
            <ArrowUpDown className="h-3.5 w-3.5" />
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
          paginatedData.filter(Boolean).map((p) => (
            <Link
              key={p.paymentNo}
              to="/payment/$paymentNo"
              params={{ paymentNo: p.paymentNo }}
              className="block rounded-xl border border-border bg-card p-4 shadow-sm active:scale-[.99]"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="font-semibold">{p.paymentNo}</div>
                  <div className="mt-0.5 truncate text-sm text-muted-foreground">
                    {p.VendorName}
                  </div>
                </div>
                <StatusBadge status="Pending" />
              </div>
              <div className="mt-3 flex items-end justify-between">
                <div className="text-xs text-muted-foreground">
                  {new Date(p.paymentDate).toLocaleDateString()}
                </div>
                <div className="text-base font-semibold tabular-nums">
                  {formatINR(p.paymentAmount || 0)}
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
          <table className="w-full text-sm">
            <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3 font-medium">Payment No</th>
                <th className="px-4 py-3 font-medium">Vendor</th>
                <th className="px-4 py-3 font-medium">Payment Date</th>
                <th className="px-4 py-3 text-right font-medium">Amount</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-border">
              {paginatedData.filter(Boolean).map((p) => (
                <tr key={p.paymentNo} className="hover:bg-secondary/40">
                  <td className="px-4 py-3 font-medium">
                    <Link
                      to="/payment/$paymentNo"
                      params={{ paymentNo: p.paymentNo }}
                      className="hover:text-primary hover:underline"
                    >
                      {p.paymentNo}
                    </Link>
                  </td>

                  <td className="px-4 py-3 text-muted-foreground">
                    {p.VendorName}
                  </td>

                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(p.paymentDate).toLocaleDateString()}
                </td>

                <td className="px-4 py-3 text-right font-semibold tabular-nums">
                  {formatINR(p.paymentAmount || 0)}
                </td>

                <td className="px-4 py-3">
                  <StatusBadge status="Pending" />
                </td>
              </tr>
            ))}
            </tbody>
          </table>
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

      {/* Mobile Bottom Sheet Filter */}
      {showFilterSheet && (
        <div className="fixed inset-0 z-40 md:hidden">
        {/* Backdrop */}
        <div
          className="absolute inset-0 bg-black/40 backdrop-blur-sm"
          onClick={() => setShowFilterSheet(false)}
        />

        {/* Bottom Sheet */}
        <div className="absolute bottom-0 left-0 right-0 z-50 rounded-t-2xl border-t border-border bg-surface p-4 pb-6 shadow-xl">
          {/* Handle */}
          <div className="mb-4 flex justify-center">
            <div className="h-1 w-10 rounded-full bg-muted" />
          </div>

          {/* Title */}
          <h2 className="mb-4 text-lg font-semibold">Filters</h2>

          {/* Amount Filter */}
          <div className="mb-5 space-y-2">
            <label className="text-sm font-medium">Amount</label>
            <div className="flex gap-2">
              <input
                type="number"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="Enter amount"
                className="flex-1 h-10 rounded-md border border-input bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
              />
              <select
                value={filterType}
                onChange={(e) => setFilterType(e.target.value)}
                className="h-10 w-16 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
              >
                <option value="gte">≥</option>
                <option value="lte">≤</option>
              </select>
            </div>
          </div>

          {/* Sort Filter */}
          <div className="mb-6 space-y-2">
            <label className="text-sm font-medium">Sort</label>
            <div className="space-y-2">
              {[
                { value: true, label: "Newest" },
                { value: false, label: "Oldest" },
              ].map((option) => (
                <label key={String(option.value)} className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="radio"
                    name="sort"
                    checked={sortDesc === option.value}
                    onChange={() => setSortDesc(option.value)}
                    className="h-4 w-4"
                  />
                  <span className="text-sm">{option.label}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-2">
            <button
              onClick={() => {
                setQ("");
                setAmount("");
                setFilterType("gte");
                setCurrentPage(1);
              }}
              className="flex-1 h-10 rounded-md border border-input bg-background text-sm font-medium hover:bg-secondary"
            >
              Reset
            </button>
            <button
              onClick={() => setShowFilterSheet(false)}
              className="flex-1 h-10 rounded-md bg-primary text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Apply
            </button>
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
      <div className="text-sm font-medium">No payment requests found</div>
      <div className="text-xs text-muted-foreground">
        Try adjusting your search or filters.
      </div>
    </div>
  );
}
