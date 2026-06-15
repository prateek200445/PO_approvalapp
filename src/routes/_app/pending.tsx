import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useMemo, useState, useEffect } from "react";
import { Search, ArrowUpDown, Filter } from "lucide-react";
import { formatINR, type POStatus } from "@/lib/mock-data";
import { useAuth } from "@/lib/auth-context";
import { StatusBadge } from "@/components/StatusBadge";

export const Route = createFileRoute("/_app/pending")({
  head: () => ({ meta: [{ title: "Pending POs — Approval Portal" }] }),
  component: PendingList,
});

function PendingList() {
  const { user } = useAuth();

  const [pendingPOs, setPendingPOs] = useState<any[]>([]);
  useEffect(() => {
  if (!user?.username) return;

  fetch(getApiUrl(`/api/PO/pending/${user.username}`))
    .then((res) => res.json())
    .then((data) => {
      console.log("Pending POs:", data);
      setPendingPOs(data);
    })
    .catch((err) => {
      console.error("Pending API Error:", err);
    });
}, [user]);
  const [q, setQ] = useState("");
  const [status, setStatus] = useState<"All" | POStatus>("Pending");
  const [sortDesc, setSortDesc] = useState(true);
  useEffect(() => {
  if (!user?.username) return;

  fetch(getApiUrl(`/api/PO/pending/${user.username}`))
    .then((response) => response.json())
    .then((data) => {
      console.log("Pending POs:", data);
      setPendingPOs(data);
    })
    .catch((error) => {
      console.error(error);
    });
}, [user]);
 



  const filtered = pendingPOs;
  return (
    <div className="space-y-5">
      <div className="flex items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Purchase Orders</h1>
          <p className="mt-1 text-sm text-muted-foreground">{filtered.length} result{filtered.length !== 1 ? "s" : ""}</p>
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
        {filtered.map((p) => (
          <Link
            key={p.PoNo}
            to="/po/$poNo"
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
        ))}
        {filtered.length === 0 && <EmptyState />}
      </div>

      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-xl border border-border bg-card md:block">
        <table className="w-full text-sm">
          <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-medium">PO Number</th>
              <th className="px-4 py-3 font-medium">Vendor</th>
              <th className="px-4 py-3 font-medium">Date</th>
              <th className="px-4 py-3 text-right font-medium">Amount</th>
              <th className="px-4 py-3 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filtered.map((p) => (
              <tr key={p.PoNo} className="hover:bg-secondary/40">
                <td className="px-4 py-3 font-medium">
                  <Link to="/po/$poNo" params={{ poNo: p.PoNo }} className="hover:text-primary hover:underline">
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
      </div>
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
