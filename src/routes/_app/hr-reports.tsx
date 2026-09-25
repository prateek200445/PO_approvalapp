import { useEffect, useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Download, Loader2, Search, Users } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { useAuth } from "@/lib/auth-context";
import { HR_REPORTS_FULL_ACCESS_USERS } from "@/lib/feature-flags";
import {
  currentYearMonth,
  downloadHrAttendanceExcel,
  downloadHrSalaryExcel,
  formatInr,
  getHrAccess,
  getHrAttendanceAck,
  getHrAttendanceReport,
  getHrBranches,
  getHrCompanies,
  getHrLeaveApplications,
  getHrLeaveCreditPreview,
  getHrLeaveEligibility,
  getHrSalaryReport,
  applyHrConfirmation,
  applyHrLeave,
  applyHrLeaveCredit,
  applyHrWfh,
  listPendingHrLeave,
  decideHrLeave,
  searchHrEmployees,
  verifyHrAttendanceMonth,
  approveHrAttendanceMonth,
  listPendingHrAttendanceAck,
  type HrEmployeeOption,
} from "@/lib/hr-reports-api";

type HrTab = "attendance" | "leave" | "wfh" | "verify" | "policy";

export const Route = createFileRoute("/_app/hr-reports")({
  head: () => ({ meta: [{ title: "HR Reports — PO Portal" }] }),
  component: HrReportsPage,
});

function todayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function plusDaysIso(n: number) {
  const d = new Date();
  d.setDate(d.getDate() + n);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function HrReportsPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";
  const [yearMonth, setYearMonth] = useState(currentYearMonth);
  const [query, setQuery] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [company, setCompany] = useState("");
  const [branch, setBranch] = useState("");
  const [officeOnly, setOfficeOnly] = useState(true);
  const [applyHalfDayRule, setApplyHalfDayRule] = useState(true);
  const [selected, setSelected] = useState<HrEmployeeOption | null>(null);
  const [monthlyBasic, setMonthlyBasic] = useState("");
  const [dailyRate, setDailyRate] = useState("");
  const [workingDays, setWorkingDays] = useState("");
  const [exporting, setExporting] = useState<"att" | "sal" | null>(null);
  const [tab, setTab] = useState<HrTab>("leave");
  const [leaveType, setLeaveType] = useState("LWP");
  const [leaveFrom, setLeaveFrom] = useState(todayIso);
  const [leaveTo, setLeaveTo] = useState(todayIso);
  const [leaveDays, setLeaveDays] = useState("1");
  const [leavePurpose, setLeavePurpose] = useState("");
  const [wfhFrom, setWfhFrom] = useState(todayIso);
  const [wfhTo, setWfhTo] = useState(todayIso);
  const [wfhPurpose, setWfhPurpose] = useState("");
  const [busy, setBusy] = useState<string | null>(null);

  useEffect(() => {
    const t = window.setTimeout(() => setDebouncedQ(query.trim()), 280);
    return () => window.clearTimeout(t);
  }, [query]);

  const accessQuery = useQuery({
    queryKey: ["hr-access", username],
    queryFn: () => getHrAccess(username),
    enabled: !!username,
    staleTime: 5 * 60_000,
  });
  const access = accessQuery.data;
  const isFullAccess =
    access?.hasFullAccess === true ||
    HR_REPORTS_FULL_ACCESS_USERS.some(
      (u) => u.toLowerCase() === username.trim().toLowerCase(),
    );
  const isSelfMode = access?.mode === "self" && !isFullAccess;
  const isViewOnly = !isFullAccess;

  const companiesQuery = useQuery({
    queryKey: ["hr-companies", username],
    queryFn: () => getHrCompanies(username),
    staleTime: 30 * 60_000,
    enabled: !!username && isFullAccess,
  });

  const branchesQuery = useQuery({
    queryKey: ["hr-branches", company, username],
    queryFn: () => getHrBranches(company || undefined, username),
    staleTime: 30 * 60_000,
    enabled: !!username && isFullAccess,
  });

  const employeesQuery = useQuery({
    queryKey: [
      "hr-employees",
      debouncedQ,
      company,
      branch,
      officeOnly,
      username,
      access?.mode,
    ],
    queryFn: () =>
      searchHrEmployees({
        q: debouncedQ,
        company: company || undefined,
        branch: branch || undefined,
        officeOnly,
        username,
        take: 50,
      }),
    placeholderData: keepPreviousData,
    // Full HR: wait for search/filter so Approvals tab opens without a 300-row payroll scan
    enabled:
      !!username &&
      (isSelfMode ||
        (isFullAccess &&
          (debouncedQ.length >= 1 || !!company.trim() || !!branch.trim()) &&
          !companiesQuery.isError)),
  });

  // Self users: auto-select their own employee row
  useEffect(() => {
    if (!isSelfMode) return;
    const self = employeesQuery.data?.[0];
    if (self && selected?.empCode !== self.empCode) {
      setSelected(self);
    }
  }, [isSelfMode, employeesQuery.data, selected?.empCode]);

  // Prefer login EmpCode when available (picker still works for other employees)
  useEffect(() => {
    if (selected || !isFullAccess) return;
    const code = (user?.empCode ?? access?.empCode ?? "").trim();
    if (!code) return;
    const match = employeesQuery.data?.find(
      (e) => e.empCode.toLowerCase() === code.toLowerCase(),
    );
    if (match) setSelected(match);
  }, [selected, isFullAccess, user?.empCode, access?.empCode, employeesQuery.data]);

  useEffect(() => {
    if (!isFullAccess && tab === "policy") setTab("attendance");
  }, [isFullAccess, tab]);

  const attendanceQuery = useQuery({
    queryKey: ["hr-attendance", selected?.empCode, yearMonth, applyHalfDayRule, username],
    queryFn: () =>
      getHrAttendanceReport(selected!.empCode, yearMonth, applyHalfDayRule, username),
    enabled: !!selected?.empCode && /^\d{4}-\d{2}$/.test(yearMonth) && !!username,
  });

  const monthlyNum = monthlyBasic.trim() === "" ? null : Number(monthlyBasic);
  const dailyNum = dailyRate.trim() === "" ? null : Number(dailyRate);
  const workingNum = workingDays.trim() === "" ? null : Number(workingDays);
  // Auto salary: ERP rate loads when employee is selected; typed values override.
  const canCalc = !!selected?.empCode;

  const salaryQuery = useQuery({
    queryKey: [
      "hr-salary",
      selected?.empCode,
      yearMonth,
      monthlyNum,
      dailyNum,
      workingNum,
      applyHalfDayRule,
      username,
    ],
    queryFn: () =>
      getHrSalaryReport({
        empCode: selected!.empCode,
        yearMonth,
        monthlyBasic: monthlyNum,
        dailyRate: dailyNum,
        workingDays: workingNum,
        applyHalfDayRule,
        username,
      }),
    enabled: canCalc && /^\d{4}-\d{2}$/.test(yearMonth) && !!username,
  });

  const leaveQuery = useQuery({
    queryKey: ["hr-leave", selected?.empCode, username],
    queryFn: () => getHrLeaveApplications(selected!.empCode, username),
    enabled: !!selected?.empCode && !!username,
  });

  const leaveEligQuery = useQuery({
    queryKey: ["hr-leave-elig", selected?.empCode, username],
    queryFn: () => getHrLeaveEligibility(selected!.empCode, username),
    enabled: !!selected?.empCode && !!username,
  });

  const canApplyPlCl =
    leaveEligQuery.data?.canApplyPlCl ??
    attendanceQuery.data?.canApplyPlCl ??
    true;

  useEffect(() => {
    if (leaveEligQuery.data && !canApplyPlCl && (leaveType === "PL" || leaveType === "CL")) {
      setLeaveType("LWP");
    }
  }, [leaveEligQuery.data, canApplyPlCl, leaveType]);

  const ackQuery = useQuery({
    queryKey: ["hr-att-ack", selected?.empCode, yearMonth, username],
    queryFn: () => getHrAttendanceAck(selected!.empCode, yearMonth, username),
    enabled: !!selected?.empCode && /^\d{4}-\d{2}$/.test(yearMonth) && !!username,
  });

  const pendingAckQuery = useQuery({
    queryKey: ["hr-att-ack-pending", username],
    queryFn: () => listPendingHrAttendanceAck(username),
    enabled: !!username && isFullAccess && tab === "verify",
    refetchInterval: 30_000,
  });

  const pendingLeaveQuery = useQuery({
    queryKey: ["hr-leave-pending", username],
    queryFn: () => listPendingHrLeave(username),
    enabled: !!username && isFullAccess && tab === "leave",
    refetchInterval: 20_000,
  });

  const creditQuery = useQuery({
    queryKey: ["hr-leave-credit", selected?.empCode, username],
    queryFn: () => getHrLeaveCreditPreview(selected!.empCode, username),
    enabled: !!selected?.empCode && !!username && isFullAccess && tab === "policy",
  });

  // Clear manual overrides when switching employee
  useEffect(() => {
    setMonthlyBasic("");
    setDailyRate("");
    setWorkingDays("");
    setLeaveFrom(todayIso());
    setLeaveTo(todayIso());
    setWfhFrom(todayIso());
    setWfhTo(todayIso());
  }, [selected?.empCode]);

  const summary = attendanceQuery.data?.summary;

  const statusTone = useMemo(
    () =>
      ({
        Present: "bg-emerald-500/15 text-emerald-800 dark:text-emerald-200",
        "Half Day": "bg-amber-500/20 text-amber-900 dark:text-amber-100",
        PL: "bg-violet-500/15 text-violet-900 dark:text-violet-200",
        CL: "bg-sky-500/15 text-sky-900 dark:text-sky-200",
        WFH: "bg-teal-500/15 text-teal-900 dark:text-teal-200",
        Absent: "bg-slate-500/15 text-slate-700 dark:text-slate-200",
      }) as Record<string, string>,
    [],
  );

  async function onExportAttendance() {
    if (!selected) return;
    setExporting("att");
    try {
      await downloadHrAttendanceExcel(selected.empCode, yearMonth, applyHalfDayRule, username);
      toast.success("Attendance Excel downloaded");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(null);
    }
  }

  async function onExportSalary() {
    if (!selected || !canCalc) return;
    setExporting("sal");
    try {
      await downloadHrSalaryExcel({
        empCode: selected.empCode,
        yearMonth,
        monthlyBasic: monthlyNum,
        dailyRate: dailyNum,
        workingDays: workingNum,
        applyHalfDayRule,
        username,
      });
      toast.success("Salary Excel downloaded");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(null);
    }
  }

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="flex items-center gap-2 text-primary">
          <Users className="h-5 w-5" />
          <p className="text-xs font-semibold uppercase tracking-wide">HR</p>
        </div>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">HR Reports</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          {isFullAccess
            ? "Full HR access — approve leave/WFH, PL/CL credit, month-end. Employees apply their own leave; HR does not apply on behalf."
            : isSelfMode
              ? "Apply leave/WFH for yourself (Pending → HR approves). View attendance. Month-end verify sends a request to HR. You cannot credit PL/CL."
              : "HR Reports access is limited by login."}
        </p>
        {access?.message ? (
          <p className="mt-2 text-xs text-muted-foreground">{access.message}</p>
        ) : null}
        {(companiesQuery.isError ||
          (isFullAccess && pendingLeaveQuery.isError)) && (
          <div className="mt-3 rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-950 dark:text-amber-100">
            <p className="font-medium">Payroll database unreachable from the cloud API</p>
            <p className="mt-1 text-xs opacity-90">
              {(companiesQuery.error instanceof Error
                ? companiesQuery.error.message
                : pendingLeaveQuery.error instanceof Error
                  ? pendingLeaveQuery.error.message
                  : null) ||
                "HR data lives on SQL port 3445. IT must allow Render → 180.211.107.118:3445. Until then use localhost (API on :5115) which can reach payroll."}
            </p>
          </div>
        )}
      </div>

      {!username ? (
        <div className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-10 text-center text-sm text-muted-foreground">
          Please log in to view HR attendance.
        </div>
      ) : accessQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Checking HR access…
        </div>
      ) : access?.mode === "none" ? (
        <div className="rounded-xl border border-amber-500/40 bg-amber-500/10 px-4 py-6 text-sm">
          {access.message}
        </div>
      ) : null}

      {username && access && access.mode !== "none" ? (
      <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
        {isFullAccess ? (
        <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4">
          <div className="lg:col-span-2">
            <Label htmlFor="hr-emp-search">Employee</Label>
            <div className="relative mt-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="hr-emp-search"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search EmpCode / name / department"
                className="pl-9"
              />
            </div>
          </div>
          <div>
            <Label htmlFor="hr-company">Company</Label>
            <select
              id="hr-company"
              value={company}
              onChange={(e) => {
                setCompany(e.target.value);
                setBranch("");
                setSelected(null);
              }}
              className="mt-1 h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              <option value="">All companies</option>
              {(companiesQuery.data ?? []).map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </div>
          <div>
            <Label htmlFor="hr-month">Month</Label>
            <Input
              id="hr-month"
              type="month"
              value={yearMonth}
              onChange={(e) => setYearMonth(e.target.value)}
              className="mt-1"
            />
          </div>
        </div>
        ) : (
        <div className="grid gap-3 md:grid-cols-2">
          <div>
            <Label>Employee</Label>
            <p className="mt-1 text-sm font-medium">
              {selected?.name ?? access.fullName ?? username}{" "}
              <span className="text-muted-foreground">({selected?.empCode ?? access.empCode})</span>
            </p>
          </div>
          <div>
            <Label htmlFor="hr-month-self">Month</Label>
            <Input
              id="hr-month-self"
              type="month"
              value={yearMonth}
              onChange={(e) => setYearMonth(e.target.value)}
              className="mt-1"
            />
          </div>
        </div>
        )}

        {isFullAccess ? (
        <>
        <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
          <div>
            <Label htmlFor="hr-branch">Branch</Label>
            <select
              id="hr-branch"
              value={branch}
              onChange={(e) => {
                setBranch(e.target.value);
                setSelected(null);
              }}
              className="mt-1 h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              <option value="">All branches</option>
              {(branchesQuery.data ?? []).map((b) => (
                <option key={b} value={b}>
                  {b}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-end">
            <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-border bg-secondary/30 px-3 py-2 text-sm">
              <input
                type="checkbox"
                checked={officeOnly}
                onChange={(e) => {
                  setOfficeOnly(e.target.checked);
                  setSelected(null);
                }}
                className="h-4 w-4"
              />
              <span>
                Head office / office staff only
                <span className="mt-0.5 block text-xs text-muted-foreground">
                  Uses empinfo.IsHOEmp (not Branch=RegistrationOffice — that mix includes plant staff)
                </span>
              </span>
            </label>
          </div>
          <div className="flex items-end">
            <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-border bg-secondary/30 px-3 py-2 text-sm">
              <input
                type="checkbox"
                checked={applyHalfDayRule}
                onChange={(e) => setApplyHalfDayRule(e.target.checked)}
                className="h-4 w-4"
              />
              <span>
                Max late 10:30 / 9 hours
                <span className="mt-0.5 block text-xs text-muted-foreground">
                  {applyHalfDayRule
                    ? "On — Present if in ≤ 10:30 AM or worked ≥ 9 hours; else Half Day"
                    : "Off — any punch = full Present (1 day)"}
                </span>
              </span>
            </label>
          </div>
        </div>

        <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
          <span>
            {employeesQuery.isFetching
              ? "Refreshing…"
              : `${employeesQuery.data?.length ?? 0} employee(s)${officeOnly ? " · HO/office flag" : ""}`}
          </span>
        </div>

        <div className="max-h-64 overflow-y-auto rounded-lg border border-border">
          {employeesQuery.isLoading ? (
            <div className="flex items-center gap-2 p-4 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" /> Loading employees…
            </div>
          ) : employeesQuery.isError ? (
            <p className="p-4 text-sm text-destructive">
              {employeesQuery.error instanceof Error
                ? employeesQuery.error.message
                : "Failed to load employees"}
            </p>
          ) : (employeesQuery.data?.length ?? 0) === 0 ? (
            <p className="p-4 text-sm text-muted-foreground">No employees found.</p>
          ) : (
            <ul className="divide-y divide-border">
              {(employeesQuery.data ?? []).map((emp) => {
                const active = selected?.empCode === emp.empCode;
                return (
                  <li key={emp.empCode}>
                    <button
                      type="button"
                      onClick={() => setSelected(emp)}
                      className={cn(
                        "flex w-full flex-col gap-0.5 px-3 py-2.5 text-left text-sm transition hover:bg-secondary/50",
                        active && "bg-primary/10",
                      )}
                    >
                      <span className="font-medium">
                        {emp.name}{" "}
                        <span className="text-muted-foreground">({emp.empCode})</span>
                        {emp.isHoEmp ? (
                          <span className="ml-2 rounded bg-sky-500/15 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-sky-800 dark:text-sky-200">
                            HO
                          </span>
                        ) : null}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {[emp.designation, emp.department, emp.companyName, emp.branch]
                          .filter(Boolean)
                          .join(" · ")}
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        {selected ? (
          <p className="text-sm">
            Selected:{" "}
            <span className="font-semibold">
              {selected.name} ({selected.empCode})
            </span>
          </p>
        ) : null}
        </>
        ) : (
          <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-border bg-secondary/30 px-3 py-2 text-sm w-fit">
            <input
              type="checkbox"
              checked={applyHalfDayRule}
              onChange={(e) => setApplyHalfDayRule(e.target.checked)}
              className="h-4 w-4"
            />
            <span>
              Max late 10:30 / 9 hours
              <span className="mt-0.5 block text-xs text-muted-foreground">
                {applyHalfDayRule
                  ? "On — Present if in ≤ 10:30 AM or worked ≥ 9 hours; else Half Day"
                  : "Off — any punch = full Present (1 day)"}
              </span>
            </span>
          </label>
        )}
      </section>
      ) : null}

      {username && access && access.mode !== "none" && (selected || isFullAccess) ? (
        <>
          <div className="flex flex-wrap gap-1 rounded-xl border border-border bg-card p-1 shadow-sm">
            {(
              [
                { id: "attendance" as const, label: "Attendance & salary" },
                {
                  id: "leave" as const,
                  label: isFullAccess ? "Approvals" : "Leave apply",
                },
                ...(isFullAccess
                  ? [{ id: "policy" as const, label: "Leave policy" }]
                  : [{ id: "wfh" as const, label: "Work from home" }]),
                { id: "verify" as const, label: "Month-end verify" },
              ]
            ).map((t) => (
              <button
                key={t.id}
                type="button"
                onClick={() => setTab(t.id)}
                className={cn(
                  "rounded-lg px-3 py-1.5 text-sm transition",
                  tab === t.id
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-secondary/60",
                )}
              >
                {t.label}
              </button>
            ))}
          </div>

          {tab === "attendance" ? (
            !selected ? (
              <p className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-8 text-center text-sm text-muted-foreground">
                Select an employee to view attendance and salary.
              </p>
            ) : (
            <>
          <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-8">
            <Stat label="Present" value={String(summary?.presentDays ?? "—")} />
            <Stat
              label={applyHalfDayRule ? "Half day (late >10:30 & <9h)" : "Half day (off)"}
              value={String(summary?.halfDays ?? "—")}
            />
            {canApplyPlCl ? (
              <>
                <Stat label="PL (month)" value={summary ? String(summary.plDays) : "—"} />
                <Stat label="CL (month)" value={summary ? String(summary.clDays) : "—"} />
              </>
            ) : null}
            <Stat label="WFH (month)" value={summary ? String(summary.wfhDays) : "—"} />
            <Stat label="Absent" value={String(summary?.absentDays ?? "—")} />
            <Stat
              label="Payable days"
              value={summary ? String(summary.payableDays) : "—"}
            />
            {canApplyPlCl ? (
              <Stat
                label="Avail PL / CL"
                value={
                  attendanceQuery.data?.leaveBalance
                    ? `${attendanceQuery.data.leaveBalance.availPl} / ${attendanceQuery.data.leaveBalance.availCl}`
                    : "—"
                }
              />
            ) : (
              <Stat label="PL / CL" value="N/A (<1 yr)" />
            )}
          </section>

          {canApplyPlCl && attendanceQuery.data?.leaveBalance ? (
            <p className="text-xs text-muted-foreground">
              Leave balance from ERP{" "}
              <code className="text-[11px]">availableleave</code>
              {": "}
              PL available {attendanceQuery.data.leaveBalance.availPl} of{" "}
              {attendanceQuery.data.leaveBalance.totalPl}
              {" · "}
              CL available {attendanceQuery.data.leaveBalance.availCl} of{" "}
              {attendanceQuery.data.leaveBalance.totalCl}
              {attendanceQuery.data.leaveBalance.periodFrom
                ? ` (period from ${attendanceQuery.data.leaveBalance.periodFrom}${
                    attendanceQuery.data.leaveBalance.periodTo
                      ? ` to ${attendanceQuery.data.leaveBalance.periodTo}`
                      : " → open"
                  })`
                : ""}
              . Approved PL/CL/WFH days count as payable.
            </p>
          ) : !canApplyPlCl && selected ? (
            <p className="rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
              PL / CL not available — employee has not completed 1 year of joining
              {attendanceQuery.data?.dateOfJoining
                ? ` (DOJ ${attendanceQuery.data.dateOfJoining}, ${attendanceQuery.data.monthsOfService ?? leaveEligQuery.data?.monthsOfService ?? "—"} months)`
                : leaveEligQuery.data?.dateOfJoining
                  ? ` (DOJ ${leaveEligQuery.data.dateOfJoining}, ${leaveEligQuery.data.monthsOfService} months)`
                  : ""}
              . LWP / WFH only.
            </p>
          ) : null}

          <section className="space-y-3 rounded-xl border border-border bg-card p-4 shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-lg font-semibold">Attendance</h2>
              <Button
                type="button"
                variant="outline"
                disabled={!attendanceQuery.data || exporting === "att"}
                onClick={() => void onExportAttendance()}
              >
                {exporting === "att" ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Download className="h-4 w-4" />
                )}
                Excel
              </Button>
            </div>

            {attendanceQuery.isLoading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" /> Loading attendance…
              </div>
            ) : attendanceQuery.isError ? (
              <p className="text-sm text-destructive">
                {attendanceQuery.error instanceof Error
                  ? attendanceQuery.error.message
                  : "Failed to load attendance"}
              </p>
            ) : (
              <>
                {attendanceQuery.data?.dataNote ? (
                  <p className="rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-950 dark:text-amber-100">
                    {attendanceQuery.data.dataNote}
                  </p>
                ) : null}
              <div className="overflow-x-auto rounded-lg border border-border">
                <table className="w-full min-w-[36rem] border-collapse text-sm">
                  <thead>
                    <tr className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                      <th className="px-3 py-2 font-medium">Date</th>
                      <th className="px-3 py-2 font-medium">Day</th>
                      <th className="px-3 py-2 font-medium">Punch in</th>
                      <th className="px-3 py-2 font-medium">Punch out</th>
                      <th className="px-3 py-2 font-medium text-right">Hours</th>
                      <th className="px-3 py-2 font-medium">Status</th>
                      <th className="px-3 py-2 font-medium text-right">Payable</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(attendanceQuery.data?.days ?? []).map((day) => (
                      <tr key={day.date} className="border-t border-border/70">
                        <td className="px-3 py-1.5 tabular-nums">{day.date}</td>
                        <td className="px-3 py-1.5">{day.dayName}</td>
                        <td className="px-3 py-1.5 tabular-nums">{day.punchIn ?? "—"}</td>
                        <td className="px-3 py-1.5 tabular-nums">{day.punchOut ?? "—"}</td>
                        <td className="px-3 py-1.5 text-right tabular-nums">
                          {day.workedHours != null ? day.workedHours.toFixed(2) : "—"}
                        </td>
                        <td className="px-3 py-1.5">
                          <span
                            className={cn(
                              "inline-flex rounded-md px-2 py-0.5 text-xs font-medium",
                              statusTone[day.status] ?? "bg-secondary",
                            )}
                          >
                            {day.status}
                          </span>
                        </td>
                        <td className="px-3 py-1.5 text-right tabular-nums">{day.payableDay}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              </>
            )}
          </section>

          <section className="space-y-3 rounded-xl border border-border bg-card p-4 shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-lg font-semibold">Salary calculation</h2>
              <Button
                type="button"
                variant="outline"
                disabled={!salaryQuery.data || exporting === "sal"}
                onClick={() => void onExportSalary()}
              >
                {exporting === "sal" ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Download className="h-4 w-4" />
                )}
                Excel
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">
              Auto-loads monthly package from ERP <code className="text-[11px]">Salary</code> master
              (or daily from <code className="text-[11px]">SalaryPerDay</code>). Leave fields blank to
              use ERP; type a value to override. Working days default = calendar days excluding Sundays.
            </p>
            {salaryQuery.data?.rateSource ? (
              <p className="rounded-lg border border-border bg-secondary/30 px-3 py-2 text-xs">
                <span className="font-medium">Rate source:</span> {salaryQuery.data.rateSource}
                {salaryQuery.data.rateDetail ? (
                  <span className="mt-0.5 block text-muted-foreground">{salaryQuery.data.rateDetail}</span>
                ) : null}
                {salaryQuery.data.erpBasicDa != null && salaryQuery.data.erpBasicDa > 0 ? (
                  <span className="mt-0.5 block text-muted-foreground">
                    ERP Basic_DA: ₹ {formatInr(salaryQuery.data.erpBasicDa)}
                    {salaryQuery.data.erpGrossSalary != null && salaryQuery.data.erpGrossSalary > 0
                      ? ` · Gross: ₹ ${formatInr(salaryQuery.data.erpGrossSalary)}`
                      : ""}
                  </span>
                ) : null}
              </p>
            ) : null}
            <div className="grid gap-3 sm:grid-cols-3">
              <div>
                <Label htmlFor="hr-basic">Monthly salary override</Label>
                <Input
                  id="hr-basic"
                  type="number"
                  inputMode="decimal"
                  value={monthlyBasic}
                  onChange={(e) => setMonthlyBasic(e.target.value)}
                  placeholder={
                    salaryQuery.data?.monthlyBasic != null
                      ? `ERP ${salaryQuery.data.monthlyBasic}`
                      : "auto from ERP"
                  }
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="hr-daily">Daily rate override</Label>
                <Input
                  id="hr-daily"
                  type="number"
                  inputMode="decimal"
                  value={dailyRate}
                  onChange={(e) => setDailyRate(e.target.value)}
                  placeholder={
                    salaryQuery.data?.dailyRate != null
                      ? `ERP ${salaryQuery.data.dailyRate}`
                      : "auto from ERP"
                  }
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="hr-wd">Working days override</Label>
                <Input
                  id="hr-wd"
                  type="number"
                  inputMode="numeric"
                  value={workingDays}
                  onChange={(e) => setWorkingDays(e.target.value)}
                  placeholder="optional"
                  className="mt-1"
                />
              </div>
            </div>

            {salaryQuery.isLoading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" /> Calculating from ERP…
              </div>
            ) : salaryQuery.isError ? (
              <p className="text-sm text-destructive">
                {salaryQuery.error instanceof Error
                  ? salaryQuery.error.message
                  : "Salary calculation failed"}
              </p>
            ) : salaryQuery.data ? (
              <div className="space-y-2 rounded-lg border border-border bg-secondary/30 p-4">
                <div className="text-sm text-muted-foreground">{salaryQuery.data.formula}</div>
                <div className="text-2xl font-semibold tabular-nums">
                  ₹ {formatInr(salaryQuery.data.computedSalary)}
                </div>
                <p className="text-xs text-muted-foreground">{salaryQuery.data.note}</p>
              </div>
            ) : null}
          </section>
            </>
            )
          ) : null}

          {tab === "leave" ? (
            <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-lg font-semibold">
                {isFullAccess
                  ? selected
                    ? `Leave history — ${selected.name} (${selected.empCode})`
                    : "Leave / WFH approval"
                  : "Leave application"}
              </h2>

              {isFullAccess ? (
                <>
                  <p className="text-xs text-muted-foreground">
                    {selected
                      ? "This employee's leave history only. HR cannot apply leave on their behalf."
                      : "Select an employee to view their leave history. Pending requests for all employees appear below."}
                  </p>
                  {!selected ? (
                    <>
                      <h3 className="text-sm font-semibold">Pending requests</h3>
                      {pendingLeaveQuery.isLoading ? (
                        <p className="text-sm text-muted-foreground">Loading…</p>
                      ) : (pendingLeaveQuery.data?.length ?? 0) === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No pending leave / WFH requests.
                        </p>
                      ) : (
                        <div className="overflow-x-auto rounded-lg border border-border">
                          <table className="w-full min-w-[44rem] text-sm">
                            <thead>
                              <tr className="bg-secondary/50 text-left text-xs uppercase text-muted-foreground">
                                <th className="px-3 py-2">Emp</th>
                                <th className="px-3 py-2">Type</th>
                                <th className="px-3 py-2">From</th>
                                <th className="px-3 py-2">To</th>
                                <th className="px-3 py-2">Days</th>
                                <th className="px-3 py-2">Purpose</th>
                                <th className="px-3 py-2">Applied</th>
                                <th className="px-3 py-2" />
                              </tr>
                            </thead>
                            <tbody>
                              {(pendingLeaveQuery.data ?? []).map((row, i) => {
                                const key = `${row.empCode}-${row.leaveType}-${row.fromDate}-${row.toDate}-${i}`;
                                return (
                                  <tr key={key} className="border-t border-border/70">
                                    <td className="px-3 py-1.5 tabular-nums">{row.empCode}</td>
                                    <td className="px-3 py-1.5">{row.leaveType}</td>
                                    <td className="px-3 py-1.5 tabular-nums">{row.fromDate}</td>
                                    <td className="px-3 py-1.5 tabular-nums">{row.toDate}</td>
                                    <td className="px-3 py-1.5">{row.days}</td>
                                    <td className="px-3 py-1.5">{row.purpose ?? "—"}</td>
                                    <td className="px-3 py-1.5 text-xs">{row.appliedAt ?? "—"}</td>
                                    <td className="px-3 py-1.5 text-right whitespace-nowrap">
                                      <Button
                                        type="button"
                                        size="sm"
                                        className="mr-1"
                                        disabled={busy === `ap-${key}`}
                                        onClick={() => {
                                          void (async () => {
                                            setBusy(`ap-${key}`);
                                            try {
                                              const r = await decideHrLeave({
                                                empCode: row.empCode,
                                                leaveType: row.leaveType,
                                                fromDate: row.fromDate,
                                                toDate: row.toDate,
                                                username,
                                                approve: true,
                                              });
                                              toast.success(r.message);
                                              await pendingLeaveQuery.refetch();
                                            } catch (err) {
                                              toast.error(
                                                err instanceof Error
                                                  ? err.message
                                                  : "Approve failed",
                                              );
                                            } finally {
                                              setBusy(null);
                                            }
                                          })();
                                        }}
                                      >
                                        Approve
                                      </Button>
                                      <Button
                                        type="button"
                                        size="sm"
                                        variant="outline"
                                        disabled={busy === `rj-${key}`}
                                        onClick={() => {
                                          void (async () => {
                                            setBusy(`rj-${key}`);
                                            try {
                                              const r = await decideHrLeave({
                                                empCode: row.empCode,
                                                leaveType: row.leaveType,
                                                fromDate: row.fromDate,
                                                toDate: row.toDate,
                                                username,
                                                approve: false,
                                              });
                                              toast.success(r.message);
                                              await pendingLeaveQuery.refetch();
                                            } catch (err) {
                                              toast.error(
                                                err instanceof Error
                                                  ? err.message
                                                  : "Reject failed",
                                              );
                                            } finally {
                                              setBusy(null);
                                            }
                                          })();
                                        }}
                                      >
                                        Reject
                                      </Button>
                                    </td>
                                  </tr>
                                );
                              })}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </>
                  ) : (
                    <>
                      {(() => {
                        const empPending = (pendingLeaveQuery.data ?? []).filter(
                          (r) =>
                            r.empCode.toLowerCase() === selected.empCode.toLowerCase(),
                        );
                        if (empPending.length === 0) return null;
                        return (
                          <div className="space-y-2">
                            <h3 className="text-sm font-semibold">Pending for this employee</h3>
                            <div className="overflow-x-auto rounded-lg border border-border">
                              <table className="w-full min-w-[36rem] text-sm">
                                <thead>
                                  <tr className="bg-secondary/50 text-left text-xs uppercase text-muted-foreground">
                                    <th className="px-3 py-2">Type</th>
                                    <th className="px-3 py-2">From</th>
                                    <th className="px-3 py-2">To</th>
                                    <th className="px-3 py-2">Days</th>
                                    <th className="px-3 py-2" />
                                  </tr>
                                </thead>
                                <tbody>
                                  {empPending.map((row, i) => {
                                    const key = `sel-${row.leaveType}-${row.fromDate}-${i}`;
                                    return (
                                      <tr key={key} className="border-t border-border/70">
                                        <td className="px-3 py-1.5">{row.leaveType}</td>
                                        <td className="px-3 py-1.5 tabular-nums">{row.fromDate}</td>
                                        <td className="px-3 py-1.5 tabular-nums">{row.toDate}</td>
                                        <td className="px-3 py-1.5">{row.days}</td>
                                        <td className="px-3 py-1.5 text-right whitespace-nowrap">
                                          <Button
                                            type="button"
                                            size="sm"
                                            className="mr-1"
                                            disabled={busy === `ap-${key}`}
                                            onClick={() => {
                                              void (async () => {
                                                setBusy(`ap-${key}`);
                                                try {
                                                  const r = await decideHrLeave({
                                                    empCode: row.empCode,
                                                    leaveType: row.leaveType,
                                                    fromDate: row.fromDate,
                                                    toDate: row.toDate,
                                                    username,
                                                    approve: true,
                                                  });
                                                  toast.success(r.message);
                                                  await pendingLeaveQuery.refetch();
                                                  await leaveQuery.refetch();
                                                  await attendanceQuery.refetch();
                                                } catch (err) {
                                                  toast.error(
                                                    err instanceof Error
                                                      ? err.message
                                                      : "Approve failed",
                                                  );
                                                } finally {
                                                  setBusy(null);
                                                }
                                              })();
                                            }}
                                          >
                                            Approve
                                          </Button>
                                          <Button
                                            type="button"
                                            size="sm"
                                            variant="outline"
                                            disabled={busy === `rj-${key}`}
                                            onClick={() => {
                                              void (async () => {
                                                setBusy(`rj-${key}`);
                                                try {
                                                  const r = await decideHrLeave({
                                                    empCode: row.empCode,
                                                    leaveType: row.leaveType,
                                                    fromDate: row.fromDate,
                                                    toDate: row.toDate,
                                                    username,
                                                    approve: false,
                                                  });
                                                  toast.success(r.message);
                                                  await pendingLeaveQuery.refetch();
                                                  await leaveQuery.refetch();
                                                } catch (err) {
                                                  toast.error(
                                                    err instanceof Error
                                                      ? err.message
                                                      : "Reject failed",
                                                  );
                                                } finally {
                                                  setBusy(null);
                                                }
                                              })();
                                            }}
                                          >
                                            Reject
                                          </Button>
                                        </td>
                                      </tr>
                                    );
                                  })}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        );
                      })()}
                      <h3 className="text-sm font-semibold">All leave applications</h3>
                      {leaveQuery.isLoading ? (
                        <p className="text-sm text-muted-foreground">Loading…</p>
                      ) : (leaveQuery.data?.length ?? 0) === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No leave records for this employee.
                        </p>
                      ) : (
                        <div className="overflow-x-auto rounded-lg border border-border">
                          <table className="w-full min-w-[40rem] text-sm">
                            <thead>
                              <tr className="bg-secondary/50 text-left text-xs uppercase text-muted-foreground">
                                <th className="px-3 py-2">Type</th>
                                <th className="px-3 py-2">From</th>
                                <th className="px-3 py-2">To</th>
                                <th className="px-3 py-2">Days</th>
                                <th className="px-3 py-2">Status</th>
                                <th className="px-3 py-2">Purpose</th>
                              </tr>
                            </thead>
                            <tbody>
                              {(leaveQuery.data ?? []).map((row, i) => (
                                <tr
                                  key={`${row.fromDate}-${row.leaveType}-${i}`}
                                  className="border-t border-border/70"
                                >
                                  <td className="px-3 py-1.5">{row.leaveType}</td>
                                  <td className="px-3 py-1.5 tabular-nums">{row.fromDate}</td>
                                  <td className="px-3 py-1.5 tabular-nums">{row.toDate}</td>
                                  <td className="px-3 py-1.5">{row.days}</td>
                                  <td className="px-3 py-1.5">{row.status ?? "—"}</td>
                                  <td className="px-3 py-1.5">{row.purpose ?? "—"}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </>
                  )}
                </>
              ) : (
                <>
                  <p className="text-xs text-muted-foreground">
                    Submit leave for yourself. Status starts as Pending — HR must approve
                    before it reflects on attendance. Start date within next 3 days (
                    {todayIso()} → {plusDaysIso(3)}).
                  </p>
                  {leaveEligQuery.data ? (
                    <p
                      className={cn(
                        "rounded-lg border px-3 py-2 text-sm",
                        canApplyPlCl
                          ? "border-emerald-500/40 bg-emerald-500/10"
                          : "border-amber-500/40 bg-amber-500/10",
                      )}
                    >
                      {leaveEligQuery.data.message}
                      {leaveEligQuery.data.dateOfJoining
                        ? ` · DOJ ${leaveEligQuery.data.dateOfJoining}`
                        : ""}
                    </p>
                  ) : null}
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
                    <div>
                      <Label>Type</Label>
                      <select
                        value={leaveType}
                        onChange={(e) => setLeaveType(e.target.value)}
                        className="mt-1 h-9 w-full rounded-md border border-border bg-background px-3 text-sm"
                      >
                        {canApplyPlCl ? (
                          <>
                            <option value="PL">PL</option>
                            <option value="CL">CL</option>
                          </>
                        ) : null}
                        <option value="LWP">LWP</option>
                      </select>
                    </div>
                    <div>
                      <Label>From</Label>
                      <Input
                        type="date"
                        className="mt-1"
                        value={leaveFrom}
                        min={todayIso()}
                        max={plusDaysIso(3)}
                        onChange={(e) => setLeaveFrom(e.target.value)}
                      />
                    </div>
                    <div>
                      <Label>To</Label>
                      <Input
                        type="date"
                        className="mt-1"
                        value={leaveTo}
                        min={leaveFrom}
                        onChange={(e) => setLeaveTo(e.target.value)}
                      />
                    </div>
                    <div>
                      <Label>Days</Label>
                      <Input
                        className="mt-1"
                        value={leaveDays}
                        onChange={(e) => setLeaveDays(e.target.value)}
                      />
                    </div>
                    <div>
                      <Label>Purpose</Label>
                      <Input
                        className="mt-1"
                        value={leavePurpose}
                        onChange={(e) => setLeavePurpose(e.target.value)}
                      />
                    </div>
                  </div>
                  <Button
                    type="button"
                    disabled={busy === "leave"}
                    onClick={() => {
                      void (async () => {
                        setBusy("leave");
                        try {
                          const r = await applyHrLeave({
                            empCode: selected.empCode,
                            leaveType,
                            fromDate: leaveFrom,
                            toDate: leaveTo,
                            days: Number(leaveDays) || undefined,
                            purpose: leavePurpose || undefined,
                            username,
                          });
                          toast.success(r.message);
                          await leaveQuery.refetch();
                        } catch (err) {
                          toast.error(err instanceof Error ? err.message : "Leave apply failed");
                        } finally {
                          setBusy(null);
                        }
                      })();
                    }}
                  >
                    {busy === "leave" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                    Submit leave
                  </Button>
                  <h3 className="pt-2 text-sm font-semibold">Your leave applications</h3>
                  {leaveQuery.isLoading ? (
                    <p className="text-sm text-muted-foreground">Loading…</p>
                  ) : (
                    <div className="overflow-x-auto rounded-lg border border-border">
                      <table className="w-full min-w-[40rem] text-sm">
                        <thead>
                          <tr className="bg-secondary/50 text-left text-xs uppercase text-muted-foreground">
                            <th className="px-3 py-2">Type</th>
                            <th className="px-3 py-2">From</th>
                            <th className="px-3 py-2">To</th>
                            <th className="px-3 py-2">Days</th>
                            <th className="px-3 py-2">Status</th>
                            <th className="px-3 py-2">Purpose</th>
                          </tr>
                        </thead>
                        <tbody>
                          {(leaveQuery.data ?? []).map((row, i) => (
                            <tr
                              key={`${row.fromDate}-${row.leaveType}-${i}`}
                              className="border-t border-border/70"
                            >
                              <td className="px-3 py-1.5">{row.leaveType}</td>
                              <td className="px-3 py-1.5 tabular-nums">{row.fromDate}</td>
                              <td className="px-3 py-1.5 tabular-nums">{row.toDate}</td>
                              <td className="px-3 py-1.5">{row.days}</td>
                              <td className="px-3 py-1.5">{row.status ?? "—"}</td>
                              <td className="px-3 py-1.5">{row.purpose ?? "—"}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </>
              )}
            </section>
          ) : null}

          {tab === "wfh" ? (
            <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-lg font-semibold">Work from home</h2>
              <p className="text-xs text-muted-foreground">
                Stored as leave type <code className="text-[11px]">WFH</code> in LeaveHistory (Pending).
                Same 3-day start-date window as leave.
              </p>
              <div className="grid gap-3 sm:grid-cols-3">
                <div>
                  <Label>From</Label>
                  <Input type="date" className="mt-1" value={wfhFrom} min={todayIso()} max={plusDaysIso(3)}
                    onChange={(e) => setWfhFrom(e.target.value)} />
                </div>
                <div>
                  <Label>To</Label>
                  <Input type="date" className="mt-1" value={wfhTo} min={wfhFrom}
                    onChange={(e) => setWfhTo(e.target.value)} />
                </div>
                <div>
                  <Label>Purpose</Label>
                  <Input className="mt-1" value={wfhPurpose} onChange={(e) => setWfhPurpose(e.target.value)} />
                </div>
              </div>
              <Button
                type="button"
                disabled={busy === "wfh"}
                onClick={() => {
                  void (async () => {
                    setBusy("wfh");
                    try {
                      const r = await applyHrWfh({
                        empCode: selected.empCode,
                        fromDate: wfhFrom,
                        toDate: wfhTo,
                        purpose: wfhPurpose || undefined,
                        username,
                      });
                      toast.success(r.message);
                      await leaveQuery.refetch();
                      await attendanceQuery.refetch();
                    } catch (err) {
                      toast.error(err instanceof Error ? err.message : "WFH apply failed");
                    } finally {
                      setBusy(null);
                    }
                  })();
                }}
              >
                {busy === "wfh" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                Submit WFH
              </Button>
            </section>
          ) : null}

          {tab === "verify" ? (
            <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-lg font-semibold">Month-end attendance verify</h2>
              <p className="text-xs text-muted-foreground">
                {isFullAccess
                  ? "Employees send a verify request to HR. Approve pending requests below, or approve the selected employee/month directly."
                  : "Review your attendance for this month, then send a verify request to HR. HR will approve."}
              </p>

              {ackQuery.data?.verified ? (
                <p className="rounded-lg border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-sm">
                  Approved by HR on {ackQuery.data.reviewedAt ?? ackQuery.data.verifiedAt}
                  {ackQuery.data.reviewedBy || ackQuery.data.verifiedBy
                    ? ` (${ackQuery.data.reviewedBy ?? ackQuery.data.verifiedBy})`
                    : ""}
                  .
                </p>
              ) : ackQuery.data?.pendingHr ? (
                <p className="rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
                  Request sent to HR
                  {ackQuery.data.requestedAt ? ` on ${ackQuery.data.requestedAt}` : ""}
                  {ackQuery.data.requestedBy ? ` by ${ackQuery.data.requestedBy}` : ""}. Waiting for
                  approval.
                </p>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No verify request yet for {yearMonth}.
                </p>
              )}

              {!isFullAccess ? (
                <Button
                  type="button"
                  disabled={busy === "ack" || ackQuery.data?.verified || ackQuery.data?.pendingHr}
                  onClick={() => {
                    void (async () => {
                      setBusy("ack");
                      try {
                        await verifyHrAttendanceMonth(selected.empCode, yearMonth, username);
                        toast.success("Verify request sent to HR for " + yearMonth);
                        await ackQuery.refetch();
                      } catch (err) {
                        toast.error(err instanceof Error ? err.message : "Request failed");
                      } finally {
                        setBusy(null);
                      }
                    })();
                  }}
                >
                  {busy === "ack" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                  Send verify request to HR
                </Button>
              ) : (
                <div className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    disabled={busy === "ack" || ackQuery.data?.pendingHr || ackQuery.data?.verified}
                    onClick={() => {
                      void (async () => {
                        setBusy("ack");
                        try {
                          await verifyHrAttendanceMonth(selected.empCode, yearMonth, username);
                          toast.success("Request logged for " + yearMonth);
                          await ackQuery.refetch();
                          await pendingAckQuery.refetch();
                        } catch (err) {
                          toast.error(err instanceof Error ? err.message : "Request failed");
                        } finally {
                          setBusy(null);
                        }
                      })();
                    }}
                  >
                    Log pending request
                  </Button>
                  <Button
                    type="button"
                    disabled={busy === "approve-ack" || ackQuery.data?.verified}
                    onClick={() => {
                      void (async () => {
                        setBusy("approve-ack");
                        try {
                          await approveHrAttendanceMonth(selected.empCode, yearMonth, username);
                          toast.success("Approved month-end for " + yearMonth);
                          await ackQuery.refetch();
                          await pendingAckQuery.refetch();
                        } catch (err) {
                          toast.error(err instanceof Error ? err.message : "Approve failed");
                        } finally {
                          setBusy(null);
                        }
                      })();
                    }}
                  >
                    {busy === "approve-ack" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                    Approve this month (HR)
                  </Button>
                </div>
              )}

              {isFullAccess ? (
                <div className="space-y-2 pt-2">
                  <h3 className="text-sm font-semibold">Pending verify requests</h3>
                  {pendingAckQuery.isLoading ? (
                    <p className="text-sm text-muted-foreground">Loading…</p>
                  ) : (pendingAckQuery.data?.length ?? 0) === 0 ? (
                    <p className="text-sm text-muted-foreground">No pending requests.</p>
                  ) : (
                    <div className="overflow-x-auto rounded-lg border border-border">
                      <table className="w-full min-w-[32rem] text-sm">
                        <thead>
                          <tr className="bg-secondary/50 text-left text-xs uppercase text-muted-foreground">
                            <th className="px-3 py-2">EmpCode</th>
                            <th className="px-3 py-2">Month</th>
                            <th className="px-3 py-2">Requested</th>
                            <th className="px-3 py-2">By</th>
                            <th className="px-3 py-2" />
                          </tr>
                        </thead>
                        <tbody>
                          {(pendingAckQuery.data ?? []).map((row) => (
                            <tr
                              key={`${row.empCode}-${row.yearMonth}`}
                              className="border-t border-border/70"
                            >
                              <td className="px-3 py-1.5 tabular-nums">{row.empCode}</td>
                              <td className="px-3 py-1.5 tabular-nums">{row.yearMonth}</td>
                              <td className="px-3 py-1.5">{row.requestedAt ?? row.verifiedAt ?? "—"}</td>
                              <td className="px-3 py-1.5">{row.requestedBy ?? row.verifiedBy ?? "—"}</td>
                              <td className="px-3 py-1.5 text-right">
                                <Button
                                  type="button"
                                  size="sm"
                                  disabled={busy === `approve-${row.empCode}-${row.yearMonth}`}
                                  onClick={() => {
                                    void (async () => {
                                      const key = `approve-${row.empCode}-${row.yearMonth}`;
                                      setBusy(key);
                                      try {
                                        await approveHrAttendanceMonth(
                                          row.empCode,
                                          row.yearMonth,
                                          username,
                                        );
                                        toast.success(`Approved ${row.empCode} ${row.yearMonth}`);
                                        await pendingAckQuery.refetch();
                                        if (
                                          selected.empCode === row.empCode &&
                                          yearMonth === row.yearMonth
                                        ) {
                                          await ackQuery.refetch();
                                        }
                                      } catch (err) {
                                        toast.error(
                                          err instanceof Error ? err.message : "Approve failed",
                                        );
                                      } finally {
                                        setBusy(null);
                                      }
                                    })();
                                  }}
                                >
                                  Approve
                                </Button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              ) : null}
            </section>
          ) : null}

          {tab === "policy" ? (
            <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
              <h2 className="text-lg font-semibold">Leave policy & confirmation</h2>
              <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
                <li>Present if first punch ≤ 10:30 AM <strong>or</strong> worked ≥ 9 hours; else Half Day (0.5).</li>
                <li>
                  Approval flow: Employee applies leave/WFH → Pending → HR Approvals tab Approve/Reject →
                  attendance & balances update. Month-end verify: employee sends request → HR approves.
                </li>
                <li>HO emp after 1 year: one-time 18 PL, then +1.5 PL every month (replaces yearly bulk).</li>
                <li>CL credited when employee confirmation is applied.</li>
              </ul>

              {creditQuery.data ? (
                <div className="rounded-lg border border-border bg-secondary/30 px-3 py-2 text-sm">
                  <div className="font-medium">Monthly PL credit preview</div>
                  <p className="mt-1 text-muted-foreground">{creditQuery.data.message}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    DOJ {creditQuery.data.dateOfJoining ?? "—"} · {creditQuery.data.monthsOfService} months
                    · HO={creditQuery.data.isHoEmp ? "yes" : "no"}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      disabled={!creditQuery.data.eligible || busy === "credit"}
                      onClick={() => {
                        void (async () => {
                          setBusy("credit");
                          try {
                            const r = await applyHrLeaveCredit(selected.empCode, false, username);
                            toast.success(r.message);
                            await attendanceQuery.refetch();
                            await creditQuery.refetch();
                          } catch (err) {
                            toast.error(err instanceof Error ? err.message : "Credit failed");
                          } finally {
                            setBusy(null);
                          }
                        })();
                      }}
                    >
                      Credit +1.5 PL (monthly)
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={!creditQuery.data.eligible || busy === "credit18"}
                      onClick={() => {
                        void (async () => {
                          setBusy("credit18");
                          try {
                            const r = await applyHrLeaveCredit(selected.empCode, true, username);
                            toast.success(r.message);
                            await attendanceQuery.refetch();
                            await creditQuery.refetch();
                          } catch (err) {
                            toast.error(err instanceof Error ? err.message : "Credit failed");
                          } finally {
                            setBusy(null);
                          }
                        })();
                      }}
                    >
                      Credit 18 + 1.5 PL (year-1 grant)
                    </Button>
                  </div>
                </div>
              ) : null}

              <div className="rounded-lg border border-border px-3 py-3">
                <div className="font-medium text-sm">Employee confirmation</div>
                <p className="mt-1 text-xs text-muted-foreground">
                  Writes <code className="text-[11px]">EmployeeConfirmation</code> and credits CL
                  (up to 6) on the open availableleave period.
                </p>
                <Button
                  type="button"
                  className="mt-3"
                  disabled={busy === "confirm"}
                  onClick={() => {
                    void (async () => {
                      setBusy("confirm");
                      try {
                        const r = await applyHrConfirmation(selected.empCode, username);
                        toast.success(r.message);
                        await attendanceQuery.refetch();
                      } catch (err) {
                        toast.error(err instanceof Error ? err.message : "Confirmation failed");
                      } finally {
                        setBusy(null);
                      }
                    })();
                  }}
                >
                  {busy === "confirm" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                  Apply confirmation + credit CL
                </Button>
              </div>
            </section>
          ) : null}
        </>
      ) : username && access?.mode === "full" ? (
        <div className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-10 text-center text-sm text-muted-foreground">
          Search and select an employee to load attendance and self-service actions.
        </div>
      ) : null}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border bg-card px-4 py-3 shadow-sm">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
    </div>
  );
}
