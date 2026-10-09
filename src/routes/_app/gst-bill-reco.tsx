import { useMemo, useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, CheckCircle2, Download, FileSpreadsheet, Loader2, Search, Square, X, XCircle } from "lucide-react";
import { Cell, Pie, PieChart, ResponsiveContainer, Sector, Tooltip } from "recharts";
import { toast } from "sonner";
import { DatePickerField } from "@/components/DatePickerField";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth-context";
import { canAccessGstBillReco } from "@/lib/feature-flags";
import { cn } from "@/lib/utils";
import {
  exportGstBillReco,
  formatIsoDate,
  formatMoney,
  getGstBillCompanies,
  runGstBillReco,
  type GstBillRecoResult,
  type GstBillRecoRow,
} from "@/lib/gst-bill-reco-api";

export const Route = createFileRoute("/_app/gst-bill-reco")({
  head: () => ({ meta: [{ title: "GST Bill Reconciliation — PO Portal" }] }),
  component: GstBillRecoPage,
});

const PAGE_SIZE = 25;

const fieldClass =
  "flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring";

type StatusFilter = "issues" | "all" | "Matched" | "Mismatch" | "Missing in ERP" | "Missing in 2B";

const PIE_COLORS = {
  Matched: "#16a34a",
  Mismatch: "#ea580c",
  "Missing in ERP": "#dc2626",
  "Missing in 2B": "#7c3aed",
} as const;

function todayIso() {
  const date = new Date();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function monthStartIso() {
  const date = new Date();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  return `${date.getFullYear()}-${month}-01`;
}

function isAbortError(error: unknown) {
  return error instanceof Error && (error.name === "AbortError" || error.name === "CancelledError");
}

function statusClass(status: string) {
  if (status === "Matched") return "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300";
  if (status === "Mismatch") return "bg-amber-500/15 text-amber-700 dark:text-amber-300";
  return "bg-rose-500/15 text-rose-700 dark:text-rose-300";
}

function GstBillRecoPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const allowed = canAccessGstBillReco(user?.username);
  const queryClient = useQueryClient();
  const [company, setCompany] = useState("");
  const [dateFrom, setDateFrom] = useState(monthStartIso);
  const [dateTo, setDateTo] = useState(todayIso);
  const [tolerance, setTolerance] = useState("1");
  const [file, setFile] = useState<File | null>(null);
  const [submitted, setSubmitted] = useState<{
    file: File;
    companyName: string;
    dateFrom: string;
    dateTo: string;
    amountTolerance: number;
    username: string;
  } | null>(null);
  const [searchNonce, setSearchNonce] = useState(0);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("issues");
  const [billFilter, setBillFilter] = useState("");
  const [partyFilter, setPartyFilter] = useState("");
  const [gstFilter, setGstFilter] = useState("");
  const [page, setPage] = useState(1);
  const [exporting, setExporting] = useState(false);
  const [selected, setSelected] = useState<GstBillRecoRow | null>(null);

  const companiesQuery = useQuery({
    queryKey: ["gst-bill-companies", user?.username],
    queryFn: () => getGstBillCompanies(user?.username ?? ""),
    enabled: allowed,
    staleTime: 30 * 60_000,
  });

  const recoQuery = useQuery({
    queryKey: ["gst-bill-reco", submitted, searchNonce],
    queryFn: ({ signal }) => runGstBillReco(submitted!, signal),
    enabled: !!submitted,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const result = recoQuery.data;
  const filtered = useMemo(() => filterRows(result?.rows ?? [], statusFilter, billFilter, partyFilter, gstFilter), [
    result,
    statusFilter,
    billFilter,
    partyFilter,
    gstFilter,
  ]);
  const pageCount = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, pageCount);
  const pageRows = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  function run() {
    if (!company.trim()) {
      toast.error("Select a company.");
      return;
    }
    if (!file) {
      toast.error("Upload the monthly 2B Excel file.");
      return;
    }
    setPage(1);
    setSelected(null);
    setSubmitted({
      file,
      companyName: company.trim(),
      dateFrom,
      dateTo,
      amountTolerance: Number(tolerance) || 0,
      username: user?.username ?? "",
    });
    setSearchNonce((value) => value + 1);
  }

  function abort() {
    void queryClient.cancelQueries({ queryKey: ["gst-bill-reco"] });
  }

  async function download(current: GstBillRecoResult) {
    setExporting(true);
    try {
      const blob = await exportGstBillReco(current.rows, user?.username ?? "");
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `gst-bill-reco-${current.dateFrom.slice(0, 10)}.xlsx`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Export failed.");
    } finally {
      setExporting(false);
    }
  }

  const loading = recoQuery.isFetching;
  const errorMessage = recoQuery.error && !isAbortError(recoQuery.error) ? (recoQuery.error as Error).message : "";

  if (!allowed) {
    return (
      <div className="mx-auto max-w-lg space-y-3 rounded-xl border border-border bg-card p-6 text-center">
        <h1 className="text-xl font-semibold">Restricted</h1>
        <p className="text-sm text-muted-foreground">GST Bill Reconciliation is only available to Prakash.</p>
        <Button variant="outline" onClick={() => navigate({ to: "/ledgers" })}>
          Back to ledgers
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">GST Bill Reconciliation</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Upload the monthly GSTR-2B workbook and compare it with the ERP GST summary. A bill matches only when bill
          number, bill date, and GSTIN all agree. The portal taxable value is compared with ERP Other All, or OTHER ALL when that amount rounds to zero, plus freight and C&amp;F import and export. Each upload is saved. The summary can take a few minutes.
        </p>
      </div>

      <div className="grid gap-4 rounded-xl border border-border bg-card p-4 shadow-sm md:grid-cols-2 xl:grid-cols-6">
        <div className="space-y-1.5 xl:col-span-2">
          <Label htmlFor="gst-company">Company</Label>
          <select id="gst-company" className={fieldClass} value={company} onChange={(event) => setCompany(event.target.value)}>
            <option value="">Select company</option>
            {(companiesQuery.data ?? []).map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label>From</Label>
          <DatePickerField value={dateFrom} onChange={setDateFrom} placeholder="From date" />
        </div>
        <div className="space-y-1.5">
          <Label>To</Label>
          <DatePickerField value={dateTo} onChange={setDateTo} placeholder="To date" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="gst-tolerance">Taxable tolerance</Label>
          <Input id="gst-tolerance" className="bg-background shadow-sm" inputMode="decimal" value={tolerance} onChange={(event) => setTolerance(event.target.value)} />
        </div>
        <div className="space-y-1.5 xl:col-span-2">
          <Label htmlFor="gst-file">2B Excel</Label>
          <Input
            id="gst-file"
            className="bg-background shadow-sm"
            type="file"
            accept=".xlsx"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </div>
        <div className="flex items-end gap-2 xl:col-span-2">
          <Button type="button" onClick={run} disabled={loading}>
            {loading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Search className="mr-2 h-4 w-4" />}
            Reconcile
          </Button>
          {loading && (
            <Button type="button" variant="outline" onClick={abort}>
              <Square className="mr-2 h-4 w-4" />
              Abort
            </Button>
          )}
        </div>
      </div>

      {errorMessage && <p className="text-sm text-rose-600">{errorMessage}</p>}
      {result?.warning && <p className="text-sm text-amber-700 dark:text-amber-300">{result.warning}</p>}

      {result && (
        <>
          <Overview
            result={result}
            statusFilter={statusFilter}
            onFilter={(next) => {
              setStatusFilter(next);
              setPage(1);
            }}
          />

          <div className="flex flex-col gap-2 md:flex-row md:items-center">
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as StatusFilter);
                setPage(1);
              }}
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm shadow-sm md:w-64"
            >
              <option value="issues">Problems only</option>
              <option value="all">All results</option>
              <option value="Matched">Matched</option>
              <option value="Mismatch">Mismatch</option>
              <option value="Missing in ERP">Missing in ERP</option>
              <option value="Missing in 2B">Missing in 2B</option>
            </select>
            <Button type="button" variant="outline" disabled={exporting} onClick={() => download(result)}>
              {exporting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Download className="mr-2 h-4 w-4" />}
              Export Excel
            </Button>
            <p className="text-sm text-muted-foreground">
              Click a slice or a row. The Excel file still contains every result.
            </p>
          </div>

          <div className="overflow-x-auto rounded-xl border border-border bg-card shadow-sm">
            <table className="w-full min-w-[980px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-border bg-card text-left">
                  <th className="px-3 py-2 font-medium">Status</th>
                  <th className="px-3 py-2 font-medium">Bill no</th>
                  <th className="px-3 py-2 font-medium">Bill date</th>
                  <th className="px-3 py-2 font-medium">GSTIN</th>
                  <th className="px-3 py-2 font-medium">Party</th>
                  <th className="px-3 py-2 text-right font-medium">ERP taxable</th>
                  <th className="px-3 py-2 text-right font-medium">2B taxable</th>
                  <th className="px-3 py-2 text-right font-medium">Difference</th>
                  <th className="px-3 py-2 font-medium">2B period</th>
                  <th className="px-3 py-2 font-medium">Voucher</th>
                </tr>
                <tr className="border-b border-border bg-card">
                  <th />
                  <th className="px-2 py-1.5">
                    <Input value={billFilter} onChange={(event) => { setBillFilter(event.target.value); setPage(1); }} placeholder="Filter" className="h-8 bg-background" />
                  </th>
                  <th />
                  <th className="px-2 py-1.5">
                    <Input value={gstFilter} onChange={(event) => { setGstFilter(event.target.value); setPage(1); }} placeholder="Filter" className="h-8 bg-background" />
                  </th>
                  <th className="px-2 py-1.5">
                    <Input value={partyFilter} onChange={(event) => { setPartyFilter(event.target.value); setPage(1); }} placeholder="Filter" className="h-8 bg-background" />
                  </th>
                  <th colSpan={5} />
                </tr>
              </thead>
              <tbody>
                {pageRows.map((row, index) => (
                  <tr
                    key={`${row.billNo}-${row.gstNo}-${row.voucherNo}-${index}`}
                    className="cursor-pointer border-b border-border/70 hover:bg-muted/60"
                    onClick={() => setSelected(row)}
                  >
                    <td className="px-3 py-2">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusClass(row.status)}`}>{row.status}</span>
                    </td>
                    <td className="px-3 py-2 font-medium">{row.billNo}</td>
                    <td className="px-3 py-2">{formatIsoDate(row.billDate)}</td>
                    <td className="px-3 py-2">{row.gstNo}</td>
                    <td className="max-w-[220px] truncate px-3 py-2" title={row.party}>{row.party}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{formatMoney(row.erpTaxable)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{formatMoney(row.twoBTaxable)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{formatMoney(row.difference)}</td>
                    <td className="px-3 py-2">{row.twoBPeriod}</td>
                    <td className="px-3 py-2">{row.voucherNo}</td>
                  </tr>
                ))}
                {pageRows.length === 0 && (
                  <tr>
                    <td colSpan={10} className="px-3 py-8 text-center text-muted-foreground">
                      No rows for this filter.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <span>
              {filtered.length === 0 ? "0" : `${(safePage - 1) * PAGE_SIZE + 1}–${Math.min(safePage * PAGE_SIZE, filtered.length)}`} of {filtered.length}
            </span>
            <div className="flex gap-2">
              <Button type="button" variant="outline" size="sm" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
                Prev
              </Button>
              <Button type="button" variant="outline" size="sm" disabled={safePage >= pageCount} onClick={() => setPage(safePage + 1)}>
                Next
              </Button>
            </div>
          </div>
          {selected && <BillSheet row={selected} onClose={() => setSelected(null)} />}
        </>
      )}
    </div>
  );
}

function filterRows(rows: GstBillRecoRow[], status: StatusFilter, bill: string, party: string, gst: string) {
  const billText = bill.trim().toLowerCase();
  const partyText = party.trim().toLowerCase();
  const gstText = gst.trim().toLowerCase();
  return rows.filter((row) => {
    if (status === "issues" && row.status === "Matched") return false;
    if (status !== "issues" && status !== "all" && row.status !== status) return false;
    if (billText && !row.billNo.toLowerCase().includes(billText)) return false;
    if (partyText && !row.party.toLowerCase().includes(partyText)) return false;
    if (gstText && !row.gstNo.toLowerCase().includes(gstText)) return false;
    return true;
  });
}

function Overview({
  result,
  statusFilter,
  onFilter,
}: {
  result: GstBillRecoResult;
  statusFilter: StatusFilter;
  onFilter: (next: StatusFilter) => void;
}) {
  const slices = [
    { key: "Matched" as const, name: "Matched", value: result.matched, filter: "Matched" as StatusFilter, tone: "success" as const, icon: CheckCircle2, hint: "Bill, date, and GSTIN agree" },
    { key: "Mismatch" as const, name: "Mismatch", value: result.mismatch, filter: "Mismatch" as StatusFilter, tone: "warning" as const, icon: AlertTriangle, hint: "Same bill. Taxable differs." },
    { key: "Missing in ERP" as const, name: "Missing in ERP", value: result.missingInErp, filter: "Missing in ERP" as StatusFilter, tone: "danger" as const, icon: XCircle, hint: "On the 2B file only" },
    { key: "Missing in 2B" as const, name: "Missing in 2B", value: result.missingIn2B, filter: "Missing in 2B" as StatusFilter, tone: "muted" as const, icon: FileSpreadsheet, hint: "In the ERP summary only" },
  ];
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);
  const pieData = slices
    .filter((slice) => slice.value > 0)
    .map((slice) => ({ ...slice, percent: total === 0 ? 0 : (slice.value / total) * 100 }));
  const highlight = statusFilter !== "issues" && statusFilter !== "all";
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  function apply(next: StatusFilter) {
    onFilter(statusFilter === next ? "all" : next);
  }

  function sliceActive(filter: StatusFilter) {
    return statusFilter === filter;
  }

  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm md:p-6">
      <div className="grid items-center gap-5 lg:grid-cols-[1fr_minmax(280px,360px)_1fr]">
        <div className="space-y-3">
          <StatCard label="Portal bills saved" value={result.savedRows} description="Stored from the 2B upload" icon={FileSpreadsheet} />
          <StatCard label="Matched" value={result.matched} description="Bill, date, and GSTIN agree" tone="success" icon={CheckCircle2} active={statusFilter === "Matched"} onClick={() => apply("Matched")} />
          <StatCard label="Mismatch" value={result.mismatch} description="Same bill. Taxable differs." tone="warning" icon={AlertTriangle} active={statusFilter === "Mismatch"} onClick={() => apply("Mismatch")} />
        </div>
        <div className="flex flex-col items-center">
          <div className="mb-3 text-center">
            <div className="inline-flex flex-col items-center gap-1.5">
              <h2 className="text-lg font-semibold tracking-tight text-foreground sm:text-xl">Reconciliation Overview</h2>
              <div className="h-0.5 w-12 rounded-full bg-gradient-to-r from-transparent via-primary to-transparent" />
            </div>
          </div>
          <div className="relative mx-auto h-64 w-full max-w-[300px] sm:h-72 sm:max-w-[340px]">
            {pieData.length === 0 ? (
              <div className="flex h-full items-center justify-center text-xs text-muted-foreground">No data</div>
            ) : (
              <>
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={pieData}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      innerRadius="58%"
                      outerRadius="82%"
                      paddingAngle={3}
                      cornerRadius={4}
                      stroke="hsl(var(--card))"
                      strokeWidth={3}
                      style={{ cursor: "pointer", outline: "none" }}
                      activeIndex={hoverIndex ?? undefined}
                      activeShape={renderShinySlice}
                      onMouseEnter={(_, index) => setHoverIndex(index)}
                      onMouseLeave={() => setHoverIndex(null)}
                      onClick={(_, index) => {
                        const slice = pieData[index];
                        if (slice) apply(slice.filter);
                      }}
                    >
                      {pieData.map((entry, index) => {
                        const selected = sliceActive(entry.filter);
                        const dimmed = highlight && !selected && hoverIndex !== index;
                        return (
                          <Cell
                            key={entry.key}
                            fill={PIE_COLORS[entry.key]}
                            opacity={dimmed ? 0.32 : 1}
                            style={{
                              cursor: "pointer",
                              outline: "none",
                              transition: "opacity 160ms ease",
                              filter: selected ? `drop-shadow(0 0 8px ${PIE_COLORS[entry.key]}88)` : undefined,
                            }}
                          />
                        );
                      })}
                    </Pie>
                    <Tooltip cursor={false} content={<PieTooltipBox />} wrapperStyle={{ outline: "none", zIndex: 40 }} />
                  </PieChart>
                </ResponsiveContainer>
                <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
                  <div className="text-3xl font-semibold tabular-nums tracking-tight sm:text-4xl">{total.toLocaleString("en-IN")}</div>
                  <div className="mt-1 max-w-[8rem] text-[11px] leading-snug text-muted-foreground">Compared result rows</div>
                </div>
              </>
            )}
          </div>
          <ul className="mt-3 flex flex-wrap items-center justify-center gap-x-4 gap-y-2">
            {pieData.map((entry, index) => {
              const active = sliceActive(entry.filter);
              const hovered = hoverIndex === index;
              return (
                <li key={entry.key}>
                  <button
                    type="button"
                    onMouseEnter={() => setHoverIndex(index)}
                    onMouseLeave={() => setHoverIndex(null)}
                    onClick={() => apply(entry.filter)}
                    className={cn(
                      "inline-flex items-center gap-2 rounded-full border px-2.5 py-1 text-xs transition",
                      active || hovered ? "border-foreground/30 bg-secondary font-medium" : "border-transparent hover:bg-secondary/70",
                    )}
                  >
                    <span
                      className="h-2.5 w-2.5 rounded-full transition"
                      style={{
                        backgroundColor: PIE_COLORS[entry.key],
                        boxShadow: hovered || active ? `0 0 8px ${PIE_COLORS[entry.key]}` : undefined,
                      }}
                    />
                    <span>{entry.name}</span>
                    <span className="tabular-nums text-muted-foreground">{entry.percent.toFixed(1)}%</span>
                  </button>
                </li>
              );
            })}
          </ul>
          <p className="mt-2 text-center text-[11px] text-muted-foreground">Click a slice or legend item to filter · click again to reset</p>
        </div>
        <div className="space-y-3">
          <StatCard label="ERP rows" value={result.erpRows} description="GST summary rows in this search" icon={FileSpreadsheet} />
          <StatCard label="Missing in ERP" value={result.missingInErp} description="On the 2B file only" tone="danger" icon={XCircle} active={statusFilter === "Missing in ERP"} onClick={() => apply("Missing in ERP")} />
          <StatCard label="Missing in 2B" value={result.missingIn2B} description="In the ERP summary only" tone="muted" icon={FileSpreadsheet} active={statusFilter === "Missing in 2B"} onClick={() => apply("Missing in 2B")} />
        </div>
      </div>
    </div>
  );
}

function renderShinySlice(props: {
  cx?: number;
  cy?: number;
  innerRadius?: number;
  outerRadius?: number;
  startAngle?: number;
  endAngle?: number;
  fill?: string;
}) {
  const { cx = 0, cy = 0, innerRadius = 0, outerRadius = 0, startAngle = 0, endAngle = 0, fill = "#16a34a" } = props;
  return (
    <g style={{ outline: "none" }}>
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={outerRadius + 6}
        outerRadius={outerRadius + 12}
        startAngle={startAngle}
        endAngle={endAngle}
        fill={fill}
        opacity={0.28}
        style={{ filter: "blur(0.5px)" }}
      />
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={Math.max(0, innerRadius - 2)}
        outerRadius={outerRadius + 8}
        startAngle={startAngle}
        endAngle={endAngle}
        fill={fill}
        stroke={fill}
        strokeWidth={1}
        style={{ filter: `drop-shadow(0 0 12px ${fill}) brightness(1.18)` }}
      />
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={outerRadius - 10}
        outerRadius={outerRadius + 2}
        startAngle={startAngle}
        endAngle={endAngle}
        fill="#ffffff"
        opacity={0.22}
      />
    </g>
  );
}

function PieTooltipBox({ active, payload }: { active?: boolean; payload?: Array<{ payload: { key: keyof typeof PIE_COLORS; name: string; value: number; percent: number } }> }) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div className="min-w-[150px] rounded-lg border border-border bg-popover px-3 py-2.5 text-popover-foreground shadow-lg">
      <div className="flex items-center gap-2">
        <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: PIE_COLORS[item.key] }} />
        <span className="text-xs font-semibold">{item.name}</span>
      </div>
      <div className="mt-1.5 flex items-baseline justify-between gap-4 text-sm">
        <span className="font-semibold tabular-nums">{item.value.toLocaleString("en-IN")}</span>
        <span className="text-xs tabular-nums text-muted-foreground">{item.percent.toFixed(1)}%</span>
      </div>
    </div>
  );
}

function StatCard({
  label,
  value,
  description,
  tone,
  icon: Icon,
  active,
  onClick,
}: {
  label: string;
  value: number;
  description: string;
  tone?: "success" | "warning" | "danger" | "muted";
  icon: typeof CheckCircle2;
  active?: boolean;
  onClick?: () => void;
}) {
  const surface =
    tone === "success"
      ? "border-success/35 bg-gradient-to-br from-success/20 via-success/10 to-card"
      : tone === "warning"
        ? "border-warning/35 bg-gradient-to-br from-warning/20 via-warning/10 to-card"
        : tone === "danger"
          ? "border-destructive/35 bg-gradient-to-br from-destructive/20 via-destructive/10 to-card"
          : tone === "muted"
            ? "border-border bg-gradient-to-br from-secondary via-secondary/60 to-card"
            : "border-primary/35 bg-gradient-to-br from-primary/20 via-primary/10 to-card";
  const iconWrap =
    tone === "success"
      ? "bg-success text-success-foreground"
      : tone === "warning"
        ? "bg-warning text-warning-foreground"
        : tone === "danger"
          ? "bg-destructive text-destructive-foreground"
          : tone === "muted"
            ? "bg-muted-foreground text-background"
            : "bg-primary text-primary-foreground";
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "w-full rounded-xl border p-4 text-left shadow-sm transition hover:-translate-y-0.5 hover:shadow-md",
        surface,
        active && "ring-2 ring-offset-2 ring-offset-background ring-primary/40",
        !onClick && "cursor-default hover:translate-y-0",
      )}
    >
      <div className="flex items-start gap-3">
        <div className={cn("flex h-11 w-11 shrink-0 items-center justify-center rounded-xl", iconWrap)}>
          <Icon className="h-5 w-5" />
        </div>
        <div>
          <div className="text-[11px] font-semibold uppercase tracking-wide text-foreground/70">{label}</div>
          <div className="mt-1 text-2xl font-bold tabular-nums">{value.toLocaleString("en-IN")}</div>
          <p className="mt-1 text-[11px] text-muted-foreground">{description}</p>
        </div>
      </div>
    </button>
  );
}

function BillSheet({ row, onClose }: { row: GstBillRecoRow; onClose: () => void }) {
  const fields: { label: string; erp: string; portal: string }[] = [
    { label: "Bill no", erp: row.billNo || "—", portal: row.billNo || "—" },
    { label: "Bill date", erp: formatIsoDate(row.billDate) || "—", portal: formatIsoDate(row.billDate) || "—" },
    { label: "GSTIN", erp: row.gstNo || "—", portal: row.gstNo || "—" },
    { label: "Party", erp: row.party || "—", portal: row.party || "—" },
    { label: "Taxable", erp: formatMoney(row.erpTaxable) || "—", portal: formatMoney(row.twoBTaxable) || "—" },
    { label: "IGST", erp: formatMoney(row.igst) || "—", portal: formatMoney(row.twoBIgst) || "—" },
    { label: "CGST / SGST", erp: [formatMoney(row.cgst), formatMoney(row.sgst)].filter(Boolean).join(" / ") || "—", portal: formatMoney(row.twoBCgstSgst) || "—" },
    { label: "Voucher", erp: [row.voucherType, row.voucherNo].filter(Boolean).join(" ") || "—", portal: row.documentType || "—" },
    { label: "Voucher date", erp: formatIsoDate(row.voucherDate) || "—", portal: "—" },
    { label: "2B period", erp: "—", portal: row.twoBPeriod || "—" },
    { label: "Difference", erp: formatMoney(row.difference) || "—", portal: formatMoney(row.difference) || "—" },
  ];
  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/40" onClick={onClose}>
      <div className="h-full w-full max-w-xl overflow-y-auto border-l border-border bg-card p-5 shadow-xl" onClick={(event) => event.stopPropagation()}>
        <div className="flex items-start justify-between gap-3">
          <div>
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusClass(row.status)}`}>{row.status}</span>
            <h2 className="mt-2 text-lg font-semibold">{row.billNo || "Bill"}</h2>
            <p className="text-sm text-muted-foreground">{row.party}</p>
          </div>
          <button type="button" onClick={onClose} className="rounded-md border border-border p-2 text-muted-foreground hover:text-foreground" aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
        <table className="mt-5 w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs uppercase tracking-wide text-muted-foreground">
              <th className="py-2 pr-3">Field</th>
              <th className="py-2 pr-3">ERP</th>
              <th className="py-2">2B</th>
            </tr>
          </thead>
          <tbody>
            {fields.map((field) => (
              <tr key={field.label} className="border-b border-border/70">
                <td className="py-2 pr-3 text-muted-foreground">{field.label}</td>
                <td className="py-2 pr-3">{field.erp}</td>
                <td className="py-2">{field.portal}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
