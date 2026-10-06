import { useEffect, useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertTriangle, Download, Loader2, Search, X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  downloadHrMasterExcel,
  getHrMasterCompanies,
  getHrMasterReport,
  type HrAttritionGroup,
  type HrHeadcountRow,
  type HrMasterStatus,
} from "@/lib/hr-master-api";

type View = "headcount" | "attrition" | "employees";
type HeadcountBy = "designations" | "departments" | "companies" | "originStates";

const STATUS_OPTIONS: { id: HrMasterStatus; label: string }[] = [
  { id: "current", label: "Current employees" },
  { id: "left_fy", label: "Left this financial year" },
  { id: "left", label: "All leavers" },
  { id: "all", label: "Everyone" },
];

const HEADCOUNT_BY: { id: HeadcountBy; label: string; column: string }[] = [
  { id: "designations", label: "Designation", column: "Designation" },
  { id: "departments", label: "Department", column: "Department" },
  { id: "companies", label: "Company", column: "Company" },
  { id: "originStates", label: "Origin state", column: "State" },
];

const selectClass =
  "h-10 w-full min-w-0 rounded-md border border-input bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20";

function num(n: number | null | undefined) {
  return n == null ? "—" : n.toLocaleString("en-IN", { maximumFractionDigits: 1 });
}

function inr(n: number | null | undefined) {
  return n == null ? "—" : `₹${Math.round(n).toLocaleString("en-IN")}`;
}

function crore(n: number) {
  return n >= 1e7 ? `₹${(n / 1e7).toFixed(2)} cr` : n >= 1e5 ? `₹${(n / 1e5).toFixed(1)} L` : inr(n);
}

function date(v: string | null | undefined) {
  if (!v) return "—";
  const d = new Date(v);
  return Number.isNaN(d.getTime())
    ? "—"
    : d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function HrEmployeeMaster({ username }: { username: string }) {
  const [company, setCompany] = useState("");
  const [status, setStatus] = useState<HrMasterStatus>("current");
  const [designation, setDesignation] = useState("");
  const [department, setDepartment] = useState("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [view, setView] = useState<View>("headcount");
  const [headcountBy, setHeadcountBy] = useState<HeadcountBy>("designations");
  const [headcountFilter, setHeadcountFilter] = useState("");
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    const t = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(t);
  }, [search]);

  const filters = { company, status, designation, department, search: debouncedSearch };

  const companiesQuery = useQuery({
    queryKey: ["hr-master-companies", username],
    queryFn: () => getHrMasterCompanies(username),
    enabled: !!username,
    staleTime: 10 * 60_000,
  });

  const reportQuery = useQuery({
    queryKey: ["hr-master", username, filters],
    queryFn: () => getHrMasterReport(filters, username),
    enabled: !!username,
    placeholderData: keepPreviousData,
    staleTime: 2 * 60_000,
  });
  const report = reportQuery.data;

  const headcountRows = useMemo(() => {
    const rows: HrHeadcountRow[] = report?.[headcountBy] ?? [];
    const q = headcountFilter.trim().toLowerCase();
    return q ? rows.filter((r) => r.name.toLowerCase().includes(q)) : rows;
  }, [report, headcountBy, headcountFilter]);

  async function handleExport() {
    setExporting(true);
    try {
      await downloadHrMasterExcel(filters, username);
      toast.success("HR master Excel downloaded");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Download failed");
    } finally {
      setExporting(false);
    }
  }

  function openEmployees(by: HeadcountBy, name: string) {
    if (by === "designations") setDesignation(name);
    else if (by === "departments") setDepartment(name);
    else if (by === "companies") setCompany(name);
    setStatus("current");
    setView("employees");
  }

  const s = report?.summary;
  const activeChips = [
    designation && { label: `Designation: ${designation}`, clear: () => setDesignation("") },
    department && { label: `Department: ${department}`, clear: () => setDepartment("") },
  ].filter(Boolean) as { label: string; clear: () => void }[];

  return (
    <section className="space-y-4">
      <div className="space-y-3 rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-sm text-muted-foreground">
              Personal details, origin, joining and exit, salary and increments, PF/ESIC, headcount and
              attrition from the ERP{report ? ` · ${report.fyLabel}` : ""}.
            </p>
          </div>
          <Button onClick={handleExport} disabled={exporting || !report} className="shrink-0 gap-2">
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download full Excel
          </Button>
        </div>

        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          <select
            className={selectClass}
            value={company}
            onChange={(e) => setCompany(e.target.value)}
            aria-label="Company"
          >
            <option value="">All companies</option>
            {(companiesQuery.data ?? []).map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
          <select
            className={selectClass}
            value={status}
            onChange={(e) => setStatus(e.target.value as HrMasterStatus)}
            aria-label="Employee status"
          >
            {STATUS_OPTIONS.map((o) => (
              <option key={o.id} value={o.id}>
                {o.label}
              </option>
            ))}
          </select>
          <div className="relative sm:col-span-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search name, code, designation, UAN, PF or ESIC no…"
              className="pl-9"
            />
          </div>
        </div>
        {activeChips.length > 0 ? (
          <div className="flex flex-wrap gap-1.5">
            {activeChips.map((c) => (
              <button
                key={c.label}
                type="button"
                onClick={c.clear}
                className="inline-flex items-center gap-1 rounded-full bg-primary/15 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/25"
              >
                {c.label}
                <X className="h-3 w-3" />
              </button>
            ))}
          </div>
        ) : null}
        <p className="text-xs text-muted-foreground">
          The Excel download contains every sheet for the selected company and list: employees with personal,
          PF/ESIC and bank details, full salary history with increments, headcount by designation, department and
          origin state, month-wise attrition, and the leavers list.
        </p>
      </div>

      {reportQuery.isError ? (
        <p className="rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {reportQuery.error instanceof Error ? reportQuery.error.message : "Could not load HR master data."}
        </p>
      ) : null}

      {!report && reportQuery.isLoading ? (
        <div className="flex items-center gap-2 rounded-xl border border-border bg-card px-4 py-8 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading employee data from the ERP…
        </div>
      ) : null}

      {s ? (
        <>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-8">
            <Kpi label="Current headcount" value={num(s.currentHeadcount)} hint={`${num(s.male)} M · ${num(s.female)} F`} />
            <Kpi label={`Joiners ${report.fyLabel}`} value={num(s.joinersFy)} />
            <Kpi label={`Leavers ${report.fyLabel}`} value={num(s.leaversFy)} />
            <Kpi label="Attrition" value={`${num(s.attritionFyPct)}%`} hint={report.fyLabel} />
            <Kpi label="Avg tenure" value={`${num(s.avgTenureYears)} yrs`} />
            <Kpi label="Avg age" value={`${num(s.avgAgeYears)} yrs`} />
            <Kpi label="PF / ESIC" value={`${num(s.pfCovered)} / ${num(s.esicCovered)}`} hint="current employees" />
            <Kpi label="Monthly salary" value={crore(s.monthlySalaryTotal)} hint="current employees" />
          </div>

          {s.leftFlaggedActive > 0 ? (
            <p className="flex items-start gap-2 rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-xs text-amber-700 dark:text-amber-300">
              <AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
              {num(s.leftFlaggedActive)} employees are still marked active in the ERP but have a leaving form after
              their joining date. They are counted as left here.
            </p>
          ) : null}

          <div className="flex gap-1 overflow-x-auto rounded-lg border border-border bg-card p-1">
            {(
              [
                { id: "headcount", label: "Headcount" },
                { id: "attrition", label: "Attrition" },
                { id: "employees", label: `Employees (${num(report.employeeTotal)})` },
              ] as { id: View; label: string }[]
            ).map((v) => (
              <button
                key={v.id}
                type="button"
                onClick={() => setView(v.id)}
                className={cn(
                  "shrink-0 rounded-md px-3 py-1.5 text-sm transition",
                  view === v.id
                    ? "bg-primary font-medium text-primary-foreground"
                    : "text-muted-foreground hover:bg-secondary/70",
                )}
              >
                {v.label}
              </button>
            ))}
            {reportQuery.isFetching ? <Loader2 className="my-auto ml-auto h-4 w-4 shrink-0 animate-spin text-muted-foreground" /> : null}
          </div>

          {view === "headcount" ? (
            <div className="space-y-3 rounded-xl border border-border bg-card p-4 shadow-sm">
              <div className="flex flex-wrap items-center gap-2">
                <div className="flex flex-wrap gap-1">
                  {HEADCOUNT_BY.map((h) => (
                    <button
                      key={h.id}
                      type="button"
                      onClick={() => setHeadcountBy(h.id)}
                      className={cn(
                        "rounded-full border px-3 py-1 text-xs font-medium transition",
                        headcountBy === h.id
                          ? "border-primary bg-primary/15 text-primary"
                          : "border-border text-muted-foreground hover:bg-secondary/60",
                      )}
                    >
                      {h.label}
                    </button>
                  ))}
                </div>
                <Input
                  value={headcountFilter}
                  onChange={(e) => setHeadcountFilter(e.target.value)}
                  placeholder="Filter…"
                  className="h-8 w-full sm:ml-auto sm:w-56"
                />
              </div>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-sm">
                  <thead>
                    <tr className="border-b border-border text-left text-xs uppercase text-muted-foreground">
                      <th className="py-2 pr-3">{HEADCOUNT_BY.find((h) => h.id === headcountBy)?.column}</th>
                      <th className="py-2 pr-3 text-right">Headcount</th>
                      <th className="py-2 pr-3 text-right">Male</th>
                      <th className="py-2 pr-3 text-right">Female</th>
                      <th className="py-2 pr-3 text-right">Monthly salary</th>
                      {headcountBy !== "companies" ? <th className="py-2">Top companies</th> : null}
                    </tr>
                  </thead>
                  <tbody>
                    {headcountRows.map((r) => (
                      <tr
                        key={r.name}
                        className={cn(
                          "border-b border-border/60",
                          headcountBy !== "originStates" && "cursor-pointer hover:bg-secondary/40",
                        )}
                        onClick={() => headcountBy !== "originStates" && openEmployees(headcountBy, r.name)}
                      >
                        <td className="py-2 pr-3 font-medium">{r.name}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{num(r.count)}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{num(r.male)}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{num(r.female)}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">{inr(r.monthlySalary)}</td>
                        {headcountBy !== "companies" ? (
                          <td className="py-2 text-xs text-muted-foreground">
                            {r.byCompany
                              .slice(0, 3)
                              .map((c) => `${c.name} ${c.count}`)
                              .join(" · ")}
                          </td>
                        ) : null}
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="font-semibold">
                      <td className="py-2 pr-3">Total</td>
                      <td className="py-2 pr-3 text-right tabular-nums">
                        {num(headcountRows.reduce((a, r) => a + r.count, 0))}
                      </td>
                      <td className="py-2 pr-3 text-right tabular-nums">
                        {num(headcountRows.reduce((a, r) => a + r.male, 0))}
                      </td>
                      <td className="py-2 pr-3 text-right tabular-nums">
                        {num(headcountRows.reduce((a, r) => a + r.female, 0))}
                      </td>
                      <td className="py-2 pr-3 text-right tabular-nums">
                        {inr(headcountRows.reduce((a, r) => a + r.monthlySalary, 0))}
                      </td>
                      {headcountBy !== "companies" ? <td /> : null}
                    </tr>
                  </tfoot>
                </table>
              </div>
              {headcountBy !== "originStates" ? (
                <p className="text-xs text-muted-foreground">Click a row to see those employees.</p>
              ) : (
                <p className="text-xs text-muted-foreground">
                  Origin state is read from the permanent address in the ERP (state, district or PIN code).
                </p>
              )}
            </div>
          ) : null}

          {view === "attrition" ? (
            <div className="space-y-4">
              <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
                <h3 className="mb-1 font-semibold">Month-wise attrition · {report.fyLabel}</h3>
                <p className="mb-3 text-xs text-muted-foreground">
                  Attrition = leavers ÷ average headcount ((opening + closing) ÷ 2) × 100.
                </p>
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[560px] text-sm">
                    <thead>
                      <tr className="border-b border-border text-left text-xs uppercase text-muted-foreground">
                        <th className="py-2 pr-3">Month</th>
                        <th className="py-2 pr-3 text-right">Opening</th>
                        <th className="py-2 pr-3 text-right">Joiners</th>
                        <th className="py-2 pr-3 text-right">Leavers</th>
                        <th className="py-2 pr-3 text-right">Closing</th>
                        <th className="py-2 text-right">Attrition</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.attrition.map((m) => (
                        <tr key={m.month} className="border-b border-border/60">
                          <td className="py-2 pr-3 font-medium">{m.label}</td>
                          <td className="py-2 pr-3 text-right tabular-nums">{num(m.opening)}</td>
                          <td className="py-2 pr-3 text-right tabular-nums text-emerald-600 dark:text-emerald-400">
                            +{num(m.joiners)}
                          </td>
                          <td className="py-2 pr-3 text-right tabular-nums text-rose-600 dark:text-rose-400">
                            −{num(m.leavers)}
                          </td>
                          <td className="py-2 pr-3 text-right tabular-nums">{num(m.closing)}</td>
                          <td className="py-2 text-right tabular-nums">{num(m.ratePct)}%</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr className="font-semibold">
                        <td className="py-2 pr-3">{report.fyLabel}</td>
                        <td />
                        <td className="py-2 pr-3 text-right tabular-nums">+{num(s.joinersFy)}</td>
                        <td className="py-2 pr-3 text-right tabular-nums">−{num(s.leaversFy)}</td>
                        <td />
                        <td className="py-2 text-right tabular-nums">{num(s.attritionFyPct)}%</td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>
              <AttritionTable title="By company" rows={report.attritionByCompany} fy={report.fyLabel} />
              <AttritionTable title="By department" rows={report.attritionByDepartment} fy={report.fyLabel} />
            </div>
          ) : null}

          {view === "employees" ? (
            <div className="space-y-2 rounded-xl border border-border bg-card p-4 shadow-sm">
              {report.employeeTotal > report.employees.length ? (
                <p className="text-xs text-muted-foreground">
                  Showing first {num(report.employees.length)} of {num(report.employeeTotal)}. Narrow the search or
                  download the Excel for the full list.
                </p>
              ) : null}
              <div className="overflow-x-auto">
                <table className="w-full min-w-[1100px] text-sm">
                  <thead>
                    <tr className="border-b border-border text-left text-xs uppercase text-muted-foreground">
                      <th className="py-2 pr-3">Employee</th>
                      <th className="py-2 pr-3">Company / designation</th>
                      <th className="py-2 pr-3">Origin</th>
                      <th className="py-2 pr-3">Joined</th>
                      <th className="py-2 pr-3">Exit</th>
                      <th className="py-2 pr-3 text-right">Monthly salary</th>
                      <th className="py-2 pr-3">Last increment</th>
                      <th className="py-2">PF / UAN / ESIC</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.employees.map((e) => (
                      <tr key={e.empCode} className="border-b border-border/60 align-top">
                        <td className="py-2 pr-3">
                          <div className="font-medium">{e.name}</div>
                          <div className="text-xs text-muted-foreground">
                            {e.empCode} · {e.gender}
                            {e.age ? ` · ${num(e.age)} yrs` : ""}
                            {e.mobile ? ` · ${e.mobile}` : ""}
                          </div>
                        </td>
                        <td className="py-2 pr-3">
                          <div>{e.designation}</div>
                          <div className="text-xs text-muted-foreground">
                            {e.company} · {e.department}
                          </div>
                        </td>
                        <td className="py-2 pr-3" title={e.permanentAddress}>
                          {e.originState}
                        </td>
                        <td className="py-2 pr-3 whitespace-nowrap">
                          {date(e.dateOfJoining)}
                          {e.tenureYears != null ? (
                            <div className="text-xs text-muted-foreground">{num(e.tenureYears)} yrs</div>
                          ) : null}
                        </td>
                        <td className="py-2 pr-3 whitespace-nowrap">
                          {e.exitDate ? (
                            <>
                              <span className="text-rose-600 dark:text-rose-400">{date(e.exitDate)}</span>
                              {e.exitReason ? (
                                <div className="max-w-[12rem] truncate text-xs text-muted-foreground">{e.exitReason}</div>
                              ) : null}
                            </>
                          ) : (
                            <span className="text-xs text-muted-foreground">{e.status}</span>
                          )}
                        </td>
                        <td className="py-2 pr-3 text-right tabular-nums">
                          {inr(e.monthlySalary)}
                          <div className="text-xs text-muted-foreground">{e.salaryBasis}</div>
                        </td>
                        <td className="py-2 pr-3 whitespace-nowrap">
                          {e.lastIncrementDate ? (
                            <>
                              {date(e.lastIncrementDate)}
                              <div className="text-xs text-emerald-600 dark:text-emerald-400">
                                +{inr(e.lastIncrementAmount)}
                                {e.lastIncrementPct != null ? ` (${num(e.lastIncrementPct)}%)` : ""}
                              </div>
                            </>
                          ) : (
                            <span className="text-xs text-muted-foreground">—</span>
                          )}
                        </td>
                        <td className="py-2 text-xs">
                          <div>PF: {e.pfAccount || "—"}</div>
                          <div>UAN: {e.uan || "—"}</div>
                          <div>ESIC: {e.esic || "—"}</div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {report.employees.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">No employees match these filters.</p>
              ) : null}
            </div>
          ) : null}
        </>
      ) : null}
    </section>
  );
}

function Kpi({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-xl border border-border bg-card p-3 shadow-sm">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
      {hint ? <div className="text-xs text-muted-foreground">{hint}</div> : null}
    </div>
  );
}

function AttritionTable({ title, rows, fy }: { title: string; rows: HrAttritionGroup[]; fy: string }) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <h3 className="mb-3 font-semibold">
        {title} · {fy}
      </h3>
      <div className="max-h-[28rem] overflow-auto">
        <table className="w-full min-w-[560px] text-sm">
          <thead className="sticky top-0 bg-card">
            <tr className="border-b border-border text-left text-xs uppercase text-muted-foreground">
              <th className="py-2 pr-3">{title.replace("By ", "")}</th>
              <th className="py-2 pr-3 text-right">Current</th>
              <th className="py-2 pr-3 text-right">Avg headcount</th>
              <th className="py-2 pr-3 text-right">Joiners</th>
              <th className="py-2 pr-3 text-right">Leavers</th>
              <th className="py-2 text-right">Attrition</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.name} className="border-b border-border/60">
                <td className="py-2 pr-3 font-medium">{r.name}</td>
                <td className="py-2 pr-3 text-right tabular-nums">{num(r.currentHeadcount)}</td>
                <td className="py-2 pr-3 text-right tabular-nums">{num(r.avgHeadcount)}</td>
                <td className="py-2 pr-3 text-right tabular-nums">{num(r.joiners)}</td>
                <td className="py-2 pr-3 text-right tabular-nums">{num(r.leavers)}</td>
                <td className="py-2 text-right tabular-nums">{num(r.ratePct)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
