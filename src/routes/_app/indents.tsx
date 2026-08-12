import { createFileRoute, Link } from "@tanstack/react-router";

import { getApiUrl } from "@/lib/api-config";
import {  useState, useEffect } from "react";
import { Search, ArrowUpDown, Filter, X } from "lucide-react";

import { useAuth } from "@/lib/auth-context";
import { setApprovalListNav } from "@/lib/approval-list-nav";

export const Route = createFileRoute("/_app/indents")({
  head: () => ({ meta: [{ title: "Pending Indents — Approval Portal" }] }),
  component: PendingList,
});

function PendingList() {
  const { user } = useAuth();

  const [pendingPOs, setPendingPOs] = useState<any[]>([]);
  const [amount, setAmount] = useState("");
  const [filterType, setFilterType] = useState("gte");
  const [showFilterSheet, setShowFilterSheet] = useState(false);
  
  // Scroll to top on component mount
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' });
    document.documentElement.scrollTop = 0;
    document.body.scrollTop = 0;
  }, []);
  
  useEffect(() => {
    if (!user?.username) return;

   fetch(
  getApiUrl(
    `/api/Indent/pending/${user.username}?amount=${amount}&filterType=${filterType}`
  )
)
      .then((res) => res.json())
      .then((data) => {
        setPendingPOs(data);
      })
      .catch((err) => {
        console.error("Pending API Error:", err);
      });
 }, [user?.username, amount, filterType]);

  const [q, setQ] = useState("");
  const [status, setStatus] = useState("Pending");
  const [sortDesc, setSortDesc] = useState(true);
  const [selectedIndents, setSelectedIndents] = useState<string[]>([]);



  const filtered = pendingPOs
  .filter((p) => {
    const search = q.toLowerCase();

    const matchesSearch =
      p.IndentNo?.toLowerCase().includes(search) ||
      String(p.TotalItems || "").includes(search);

    const matchesStatus =
      status === "All" || p.Status === status;

    return matchesSearch && matchesStatus;
  })
  .sort((a, b) =>
    sortDesc
      ? new Date(b.IndentDate).getTime() -
        new Date(a.IndentDate).getTime()
      : new Date(a.IndentDate).getTime() -
        new Date(b.IndentDate).getTime()
  );

  const toggleIndent = (indentNo: string) => {
  if (selectedIndents.includes(indentNo)) {
    setSelectedIndents(
      selectedIndents.filter((id) => id !== indentNo)
    );
  } else {
    setSelectedIndents([...selectedIndents, indentNo]);
  }
};
  return (
    <div className="space-y-5">
      <div className="flex items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Indent Approvals</h1>
          <p className="mt-1 text-sm text-muted-foreground">{filtered.length} result{filtered.length !== 1 ? "s" : ""}</p>
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
            placeholder="Search Indent number…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
        <div className="flex flex-wrap gap-2">
          <div className="relative">
            <Filter className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
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
            placeholder="Search Indent…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>

        {/* Active Filters + Filter Button Row */}
        <div className="flex items-center gap-2">
          {/* Active Filter Chips */}
          <div className="flex flex-wrap gap-1 flex-1">
            {status !== "All" && (
              <button
                onClick={() => setStatus("All")}
                className="inline-flex items-center gap-1 rounded-full bg-primary/20 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/30 transition"
              >
                {status}
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
            {status !== "All" && (
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
        {filtered.map((p) => (
          <Link
           key={p.IndentNo}
to="/indent/$indentNo"
params={{ indentNo: p.IndentNo }}
onClick={() =>
  setApprovalListNav(
    "indent",
    filtered.map((row) => row.IndentNo).filter(Boolean),
  )
}
            className="block rounded-xl border border-border bg-card p-4 shadow-sm active:scale-[.99]"
          >
           <div className="flex items-start gap-3">
  <input
    type="checkbox"
    checked={selectedIndents.includes(p.IndentNo)}
    onClick={(e) => e.preventDefault()}
    onChange={() => toggleIndent(p.IndentNo)}
  />

  <div className="min-w-0">
    <div className="font-semibold">{p.IndentNo}</div>
    <div className="mt-0.5 truncate text-sm text-muted-foreground">
      {p.TotalItems} Items
    </div>
  </div>
</div>

<div className="mt-3 flex items-end justify-between">
  <div className="text-xs text-muted-foreground">
    {p.IndentDate}
  </div>
</div>
          </Link>
        ))}
        {filtered.length === 0 && <EmptyState />}
      </div>

      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-xl border border-border bg-card md:block">
        <table className="w-full text-sm">
          <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-medium">Indent No</th>
<th className="px-4 py-3 font-medium">Date</th>
<th className="px-4 py-3 font-medium">Items</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filtered.map((p) => (
              <tr key={p.IndentNo} className="hover:bg-secondary/40">
 <td className="px-4 py-3 font-medium">
  <Link
    to="/indent/$indentNo"
    params={{ indentNo: p.IndentNo }}
    onClick={() =>
      setApprovalListNav(
        "indent",
        filtered.map((row) => row.IndentNo).filter(Boolean),
      )
    }
    className="hover:text-primary hover:underline"
  >
    {p.IndentNo}
  </Link>
</td>

  <td className="px-4 py-3 text-muted-foreground">
    {p.IndentDate}
  </td>

  <td className="px-4 py-3">
    {p.TotalItems}
  </td>
</tr>
            ))}
          </tbody>
        </table>
        {filtered.length === 0 && <EmptyState />}
      </div>

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

            {/* Status Filter */}
            <div className="mb-6 space-y-2">
              <label className="text-sm font-medium">Status</label>
              <div className="space-y-2">
                {["All", "Pending", "Approved", "Rejected"].map((s) => (
                  <label key={s} className="flex items-center gap-3 cursor-pointer">
                    <input
                      type="radio"
                      name="status"
                      value={s}
                      checked={status === s}
                      onChange={(e) => setStatus(e.target.value)}
                      className="h-4 w-4"
                    />
                    <span className="text-sm">{s}</span>
                  </label>
                ))}
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
                  setStatus("Pending");
                  setSortDesc(true);
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
      <div className="text-sm font-medium">No pending indents found</div>
      <div className="text-xs text-muted-foreground">Try adjusting your search or filters.</div>
    </div>
  );
}
