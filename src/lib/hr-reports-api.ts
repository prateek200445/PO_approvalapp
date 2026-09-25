import { getApiUrl } from "@/lib/api-config";

export type HrEmployeeOption = {
  empCode: string;
  name: string;
  designation?: string | null;
  department?: string | null;
  companyName?: string | null;
  branch?: string | null;
  isHoEmp?: boolean;
  isActive?: string | null;
};

export type HrAttendanceDay = {
  date: string;
  dayName: string;
  punchIn?: string | null;
  punchOut?: string | null;
  workedHours?: number | null;
  status: string;
  payableDay: number;
  branch?: string | null;
};

export type HrAttendanceSummary = {
  calendarDays: number;
  presentDays: number;
  halfDays: number;
  absentDays: number;
  plDays: number;
  clDays: number;
  wfhDays: number;
  payableDays: number;
};

export type HrLeaveBalance = {
  totalPl: number;
  totalCl: number;
  availPl: number;
  availCl: number;
  periodFrom?: string | null;
  periodTo?: string | null;
};

export type HrAttendanceReport = {
  yearMonth: string;
  periodLabel: string;
  halfDayAfter: string;
  applyHalfDayRule?: boolean;
  employee: HrEmployeeOption;
  summary: HrAttendanceSummary;
  leaveBalance?: HrLeaveBalance | null;
  days: HrAttendanceDay[];
  dataNote?: string | null;
  canApplyPlCl?: boolean;
  completedOneYear?: boolean;
  monthsOfService?: number;
  dateOfJoining?: string | null;
};

export type HrSalaryReport = {
  yearMonth: string;
  periodLabel: string;
  employee: HrEmployeeOption;
  attendanceSummary: HrAttendanceSummary;
  monthlyBasic?: number | null;
  dailyRate?: number | null;
  erpBasicDa?: number | null;
  erpGrossSalary?: number | null;
  rateSource?: string | null;
  rateDetail?: string | null;
  workingDays: number;
  payableDays: number;
  formula: string;
  computedSalary: number;
  note: string;
};

function mapEmployee(raw: Record<string, unknown>): HrEmployeeOption {
  return {
    empCode: String(raw.empCode ?? raw.EmpCode ?? ""),
    name: String(raw.name ?? raw.Name ?? ""),
    designation: (raw.designation ?? raw.Designation) as string | null,
    department: (raw.department ?? raw.Department) as string | null,
    companyName: (raw.companyName ?? raw.CompanyName) as string | null,
    branch: (raw.branch ?? raw.Branch) as string | null,
    isHoEmp: Boolean(raw.isHoEmp ?? raw.IsHoEmp ?? false),
    isActive: (raw.isActive ?? raw.IsActive) as string | null,
  };
}

async function readError(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as { message?: string };
    if (data?.message) return data.message;
  } catch {
    /* ignore */
  }
  return `Request failed (${res.status})`;
}

export function currentYearMonth(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
}

export type HrAccess = {
  username: string;
  empCode?: string | null;
  fullName?: string | null;
  hasFullAccess: boolean;
  canUseSelfService: boolean;
  isViewOnly: boolean;
  mode: "full" | "self" | "none";
  message: string;
};

export async function getHrAccess(username: string): Promise<HrAccess> {
  const params = new URLSearchParams({ username });
  const res = await fetch(getApiUrl(`/api/hr/reports/access?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  const modeRaw = String(raw.mode ?? raw.Mode ?? "none").toLowerCase();
  const mode = modeRaw === "full" || modeRaw === "self" ? modeRaw : "none";
  const hasFullAccess = Boolean(raw.hasFullAccess ?? raw.HasFullAccess);
  return {
    username: String(raw.username ?? raw.Username ?? username),
    empCode: (raw.empCode ?? raw.EmpCode) as string | null,
    fullName: (raw.fullName ?? raw.FullName) as string | null,
    hasFullAccess,
    canUseSelfService: Boolean(raw.canUseSelfService ?? raw.CanUseSelfService),
    isViewOnly: Boolean(raw.isViewOnly ?? raw.IsViewOnly ?? !hasFullAccess),
    mode,
    message: String(raw.message ?? raw.Message ?? ""),
  };
}

function withUser(params: URLSearchParams, username?: string | null) {
  if (username?.trim()) params.set("username", username.trim());
  return params;
}

export async function getHrCompanies(username?: string): Promise<string[]> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/companies?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as string[];
}

export async function getHrBranches(company?: string, username?: string): Promise<string[]> {
  const params = withUser(new URLSearchParams(), username);
  if (company?.trim()) params.set("company", company.trim());
  const res = await fetch(getApiUrl(`/api/hr/reports/branches?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as string[];
}

export async function searchHrEmployees(args: {
  q?: string;
  company?: string;
  branch?: string;
  officeOnly?: boolean;
  username?: string;
  take?: number;
}): Promise<HrEmployeeOption[]> {
  const params = withUser(new URLSearchParams(), args.username);
  if (args.q?.trim()) params.set("q", args.q.trim());
  if (args.company?.trim()) params.set("company", args.company.trim());
  if (args.branch?.trim()) params.set("branch", args.branch.trim());
  if (args.officeOnly) params.set("officeOnly", "true");
  const take = Math.min(Math.max(args.take ?? 50, 1), 200);
  params.set("take", String(take));
  const res = await fetch(getApiUrl(`/api/hr/reports/employees?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const rows = (await res.json()) as Record<string, unknown>[];
  return rows.map(mapEmployee);
}

export async function getHrAttendanceReport(
  empCode: string,
  yearMonth: string,
  applyHalfDayRule = true,
  username?: string,
): Promise<HrAttendanceReport> {
  const params = withUser(new URLSearchParams({ empCode, yearMonth }), username);
  params.set("applyHalfDayRule", applyHalfDayRule ? "true" : "false");
  const res = await fetch(getApiUrl(`/api/hr/reports/attendance?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  const summary = (raw.summary ?? raw.Summary ?? {}) as Record<string, unknown>;
  const days = ((raw.days ?? raw.Days ?? []) as Record<string, unknown>[]).map((d) => ({
    date: String(d.date ?? d.Date ?? ""),
    dayName: String(d.dayName ?? d.DayName ?? ""),
    punchIn: (d.punchIn ?? d.PunchIn) as string | null,
    punchOut: (d.punchOut ?? d.PunchOut) as string | null,
    workedHours:
      d.workedHours != null || d.WorkedHours != null
        ? Number(d.workedHours ?? d.WorkedHours)
        : null,
    status: String(d.status ?? d.Status ?? ""),
    payableDay: Number(d.payableDay ?? d.PayableDay ?? 0),
    branch: (d.branch ?? d.Branch) as string | null,
  }));
  return {
    yearMonth: String(raw.yearMonth ?? raw.YearMonth ?? yearMonth),
    periodLabel: String(raw.periodLabel ?? raw.PeriodLabel ?? ""),
    halfDayAfter: String(raw.halfDayAfter ?? raw.HalfDayAfter ?? "in≤10:30 or ≥9h worked"),
    applyHalfDayRule: Boolean(raw.applyHalfDayRule ?? raw.ApplyHalfDayRule ?? applyHalfDayRule),
    employee: mapEmployee((raw.employee ?? raw.Employee ?? {}) as Record<string, unknown>),
    summary: {
      calendarDays: Number(summary.calendarDays ?? summary.CalendarDays ?? 0),
      presentDays: Number(summary.presentDays ?? summary.PresentDays ?? 0),
      halfDays: Number(summary.halfDays ?? summary.HalfDays ?? 0),
      absentDays: Number(summary.absentDays ?? summary.AbsentDays ?? 0),
      plDays: Number(summary.plDays ?? summary.PlDays ?? 0),
      clDays: Number(summary.clDays ?? summary.ClDays ?? 0),
      wfhDays: Number(summary.wfhDays ?? summary.WfhDays ?? 0),
      payableDays: Number(summary.payableDays ?? summary.PayableDays ?? 0),
    },
    leaveBalance: mapLeaveBalance(raw.leaveBalance ?? raw.LeaveBalance),
    days,
    dataNote: (raw.dataNote ?? raw.DataNote) as string | null,
    canApplyPlCl: raw.canApplyPlCl != null || raw.CanApplyPlCl != null
      ? Boolean(raw.canApplyPlCl ?? raw.CanApplyPlCl)
      : true,
    completedOneYear: Boolean(raw.completedOneYear ?? raw.CompletedOneYear ?? true),
    monthsOfService: Number(raw.monthsOfService ?? raw.MonthsOfService ?? 0),
    dateOfJoining: (raw.dateOfJoining ?? raw.DateOfJoining) as string | null,
  };
}

function mapLeaveBalance(raw: unknown): HrLeaveBalance | null {
  if (!raw || typeof raw !== "object") return null;
  const b = raw as Record<string, unknown>;
  return {
    totalPl: Number(b.totalPl ?? b.TotalPl ?? 0),
    totalCl: Number(b.totalCl ?? b.TotalCl ?? 0),
    availPl: Number(b.availPl ?? b.AvailPl ?? 0),
    availCl: Number(b.availCl ?? b.AvailCl ?? 0),
    periodFrom: (b.periodFrom ?? b.PeriodFrom) as string | null,
    periodTo: (b.periodTo ?? b.PeriodTo) as string | null,
  };
}

export async function getHrSalaryReport(args: {
  empCode: string;
  yearMonth: string;
  monthlyBasic?: number | null;
  dailyRate?: number | null;
  workingDays?: number | null;
  applyHalfDayRule?: boolean;
  username?: string;
}): Promise<HrSalaryReport> {
  const params = withUser(
    new URLSearchParams({
      empCode: args.empCode,
      yearMonth: args.yearMonth,
    }),
    args.username,
  );
  if (args.monthlyBasic != null && args.monthlyBasic > 0) {
    params.set("monthlyBasic", String(args.monthlyBasic));
  }
  if (args.dailyRate != null && args.dailyRate > 0) {
    params.set("dailyRate", String(args.dailyRate));
  }
  if (args.workingDays != null && args.workingDays > 0) {
    params.set("workingDays", String(args.workingDays));
  }
  params.set("applyHalfDayRule", args.applyHalfDayRule === false ? "false" : "true");
  const res = await fetch(getApiUrl(`/api/hr/reports/salary?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  const summary = (raw.attendanceSummary ?? raw.AttendanceSummary ?? {}) as Record<string, unknown>;
  return {
    yearMonth: String(raw.yearMonth ?? raw.YearMonth ?? args.yearMonth),
    periodLabel: String(raw.periodLabel ?? raw.PeriodLabel ?? ""),
    employee: mapEmployee((raw.employee ?? raw.Employee ?? {}) as Record<string, unknown>),
    attendanceSummary: {
      calendarDays: Number(summary.calendarDays ?? summary.CalendarDays ?? 0),
      presentDays: Number(summary.presentDays ?? summary.PresentDays ?? 0),
      halfDays: Number(summary.halfDays ?? summary.HalfDays ?? 0),
      absentDays: Number(summary.absentDays ?? summary.AbsentDays ?? 0),
      plDays: Number(summary.plDays ?? summary.PlDays ?? 0),
      clDays: Number(summary.clDays ?? summary.ClDays ?? 0),
      wfhDays: Number(summary.wfhDays ?? summary.WfhDays ?? 0),
      payableDays: Number(summary.payableDays ?? summary.PayableDays ?? 0),
    },
    monthlyBasic: (raw.monthlyBasic ?? raw.MonthlyBasic) as number | null,
    dailyRate: (raw.dailyRate ?? raw.DailyRate) as number | null,
    erpBasicDa: (raw.erpBasicDa ?? raw.ErpBasicDa) as number | null,
    erpGrossSalary: (raw.erpGrossSalary ?? raw.ErpGrossSalary) as number | null,
    rateSource: (raw.rateSource ?? raw.RateSource) as string | null,
    rateDetail: (raw.rateDetail ?? raw.RateDetail) as string | null,
    workingDays: Number(raw.workingDays ?? raw.WorkingDays ?? 0),
    payableDays: Number(raw.payableDays ?? raw.PayableDays ?? 0),
    formula: String(raw.formula ?? raw.Formula ?? ""),
    computedSalary: Number(raw.computedSalary ?? raw.ComputedSalary ?? 0),
    note: String(raw.note ?? raw.Note ?? ""),
  };
}

async function downloadBlob(path: string, fallbackName: string) {
  const res = await fetch(getApiUrl(path));
  if (!res.ok) throw new Error(await readError(res));
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fallbackName;
  a.click();
  URL.revokeObjectURL(url);
}

export async function downloadHrAttendanceExcel(
  empCode: string,
  yearMonth: string,
  applyHalfDayRule = true,
  username?: string,
) {
  const params = withUser(new URLSearchParams({ empCode, yearMonth }), username);
  params.set("applyHalfDayRule", applyHalfDayRule ? "true" : "false");
  await downloadBlob(
    `/api/hr/reports/attendance/excel?${params}`,
    `attendance-${empCode}-${yearMonth}.xlsx`,
  );
}

export async function downloadHrSalaryExcel(args: {
  empCode: string;
  yearMonth: string;
  monthlyBasic?: number | null;
  dailyRate?: number | null;
  workingDays?: number | null;
  applyHalfDayRule?: boolean;
  username?: string;
}) {
  const params = withUser(
    new URLSearchParams({
      empCode: args.empCode,
      yearMonth: args.yearMonth,
    }),
    args.username,
  );
  if (args.monthlyBasic != null && args.monthlyBasic > 0) {
    params.set("monthlyBasic", String(args.monthlyBasic));
  }
  if (args.dailyRate != null && args.dailyRate > 0) {
    params.set("dailyRate", String(args.dailyRate));
  }
  if (args.workingDays != null && args.workingDays > 0) {
    params.set("workingDays", String(args.workingDays));
  }
  params.set("applyHalfDayRule", args.applyHalfDayRule === false ? "false" : "true");
  await downloadBlob(
    `/api/hr/reports/salary/excel?${params}`,
    `salary-${args.empCode}-${args.yearMonth}.xlsx`,
  );
}

export function formatInr(n: number): string {
  return n.toLocaleString("en-IN", { maximumFractionDigits: 2, minimumFractionDigits: 0 });
}

export type HrLeaveApplication = {
  empCode: string;
  leaveType: string;
  days: number;
  fromDate: string;
  toDate: string;
  purpose?: string | null;
  status?: string | null;
  approvedBy?: string | null;
  appliedAt?: string | null;
};

export async function getHrLeaveApplications(
  empCode: string,
  username?: string,
): Promise<HrLeaveApplication[]> {
  const params = withUser(new URLSearchParams({ empCode }), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const rows = (await res.json()) as Record<string, unknown>[];
  return rows.map((r) => ({
    empCode: String(r.empCode ?? r.EmpCode ?? ""),
    leaveType: String(r.leaveType ?? r.LeaveType ?? "").trim(),
    days: Number(r.days ?? r.Days ?? 0),
    fromDate: String(r.fromDate ?? r.FromDate ?? ""),
    toDate: String(r.toDate ?? r.ToDate ?? ""),
    purpose: (r.purpose ?? r.Purpose) as string | null,
    status: (r.status ?? r.Status) as string | null,
    approvedBy: (r.approvedBy ?? r.ApprovedBy) as string | null,
    appliedAt: (r.appliedAt ?? r.AppliedAt) as string | null,
  }));
}

export type HrLeaveEligibility = {
  empCode: string;
  dateOfJoining?: string | null;
  monthsOfService: number;
  completedOneYear: boolean;
  canApplyPlCl: boolean;
  message: string;
};

export async function getHrLeaveEligibility(
  empCode: string,
  username?: string,
): Promise<HrLeaveEligibility> {
  const params = withUser(new URLSearchParams({ empCode }), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave-eligibility?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    empCode: String(raw.empCode ?? raw.EmpCode ?? ""),
    dateOfJoining: (raw.dateOfJoining ?? raw.DateOfJoining) as string | null,
    monthsOfService: Number(raw.monthsOfService ?? raw.MonthsOfService ?? 0),
    completedOneYear: Boolean(raw.completedOneYear ?? raw.CompletedOneYear),
    canApplyPlCl: Boolean(raw.canApplyPlCl ?? raw.CanApplyPlCl),
    message: String(raw.message ?? raw.Message ?? ""),
  };
}

export async function applyHrLeave(body: {
  empCode: string;
  leaveType: string;
  fromDate: string;
  toDate?: string;
  days?: number;
  purpose?: string;
  username?: string;
}): Promise<{ message: string }> {
  const params = withUser(new URLSearchParams(), body.username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as { message: string };
}

export async function applyHrWfh(body: {
  empCode: string;
  fromDate: string;
  toDate?: string;
  days?: number;
  purpose?: string;
  username?: string;
}): Promise<{ message: string }> {
  const params = withUser(new URLSearchParams(), body.username);
  const res = await fetch(getApiUrl(`/api/hr/reports/wfh?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return { message: String(raw.message ?? raw.Message ?? "Submitted") };
}

export async function listPendingHrLeave(username?: string): Promise<HrLeaveApplication[]> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave/pending?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>[];
  return (Array.isArray(raw) ? raw : []).map((r) => ({
    empCode: String(r.empCode ?? r.EmpCode ?? ""),
    leaveType: String(r.leaveType ?? r.LeaveType ?? ""),
    days: Number(r.days ?? r.Days ?? 0),
    fromDate: String(r.fromDate ?? r.FromDate ?? ""),
    toDate: String(r.toDate ?? r.ToDate ?? ""),
    purpose: (r.purpose ?? r.Purpose) as string | null,
    status: (r.status ?? r.Status) as string | null,
    approvedBy: (r.approvedBy ?? r.ApprovedBy) as string | null,
    appliedAt: (r.appliedAt ?? r.AppliedAt) as string | null,
  }));
}

export async function decideHrLeave(body: {
  empCode: string;
  leaveType: string;
  fromDate: string;
  toDate?: string;
  username?: string;
  approve: boolean;
}): Promise<{ message: string; status: string }> {
  const path = body.approve ? "leave/approve" : "leave/reject";
  const params = withUser(new URLSearchParams(), body.username);
  const res = await fetch(getApiUrl(`/api/hr/reports/${path}?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    message: String(raw.message ?? raw.Message ?? "Done"),
    status: String(raw.status ?? raw.Status ?? ""),
  };
}

export async function applyHrConfirmation(
  empCode: string,
  username?: string,
): Promise<{ message: string; clCredited: number }> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/confirmation?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ empCode, appliedBy: username }),
  });
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    message: String(raw.message ?? raw.Message ?? "Confirmed"),
    clCredited: Number(raw.clCredited ?? raw.ClCredited ?? 0),
  };
}

export type HrLeaveCreditPreview = {
  empCode: string;
  isHoEmp: boolean;
  dateOfJoining?: string | null;
  monthsOfService: number;
  completedOneYear: boolean;
  eligible: boolean;
  oneTimePlGrant: number;
  monthlyPlCredit: number;
  message: string;
};

export async function getHrLeaveCreditPreview(
  empCode: string,
  username?: string,
): Promise<HrLeaveCreditPreview> {
  const params = withUser(new URLSearchParams({ empCode }), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave-credit/preview?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    empCode: String(raw.empCode ?? raw.EmpCode ?? ""),
    isHoEmp: Boolean(raw.isHoEmp ?? raw.IsHoEmp),
    dateOfJoining: (raw.dateOfJoining ?? raw.DateOfJoining) as string | null,
    monthsOfService: Number(raw.monthsOfService ?? raw.MonthsOfService ?? 0),
    completedOneYear: Boolean(raw.completedOneYear ?? raw.CompletedOneYear),
    eligible: Boolean(raw.eligible ?? raw.Eligible),
    oneTimePlGrant: Number(raw.oneTimePlGrant ?? raw.OneTimePlGrant ?? 18),
    monthlyPlCredit: Number(raw.monthlyPlCredit ?? raw.MonthlyPlCredit ?? 1.5),
    message: String(raw.message ?? raw.Message ?? ""),
  };
}

export async function applyHrLeaveCredit(
  empCode: string,
  includeOneTimeGrant = false,
  username?: string,
): Promise<{ message: string; plCredited: number }> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/leave-credit?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ empCode, includeOneTimeGrant }),
  });
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    message: String(raw.message ?? raw.Message ?? "Credited"),
    plCredited: Number(raw.plCredited ?? raw.PlCredited ?? 0),
  };
}

export type HrAttendanceAck = {
  empCode: string;
  yearMonth: string;
  status?: string;
  verified: boolean;
  pendingHr?: boolean;
  verifiedAt?: string | null;
  verifiedBy?: string | null;
  note?: string | null;
  requestedBy?: string | null;
  requestedAt?: string | null;
  reviewedBy?: string | null;
  reviewedAt?: string | null;
};

function mapAttendanceAck(raw: Record<string, unknown>, yearMonthFallback = ""): HrAttendanceAck {
  return {
    empCode: String(raw.empCode ?? raw.EmpCode ?? ""),
    yearMonth: String(raw.yearMonth ?? raw.YearMonth ?? yearMonthFallback),
    status: String(raw.status ?? raw.Status ?? "None"),
    verified: Boolean(raw.verified ?? raw.Verified),
    pendingHr: Boolean(raw.pendingHr ?? raw.PendingHr),
    verifiedAt: (raw.verifiedAt ?? raw.VerifiedAt) as string | null,
    verifiedBy: (raw.verifiedBy ?? raw.VerifiedBy) as string | null,
    note: (raw.note ?? raw.Note) as string | null,
    requestedBy: (raw.requestedBy ?? raw.RequestedBy) as string | null,
    requestedAt: (raw.requestedAt ?? raw.RequestedAt) as string | null,
    reviewedBy: (raw.reviewedBy ?? raw.ReviewedBy) as string | null,
    reviewedAt: (raw.reviewedAt ?? raw.ReviewedAt) as string | null,
  };
}

export async function getHrAttendanceAck(
  empCode: string,
  yearMonth: string,
  username?: string,
): Promise<HrAttendanceAck> {
  const params = withUser(new URLSearchParams({ empCode, yearMonth }), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/attendance-ack?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  return mapAttendanceAck((await res.json()) as Record<string, unknown>, yearMonth);
}

/** Employee: send month-end verify request to HR. */
export async function verifyHrAttendanceMonth(
  empCode: string,
  yearMonth: string,
  username?: string,
): Promise<HrAttendanceAck> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/attendance-ack?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ empCode, yearMonth, verifiedBy: username, approve: false }),
  });
  if (!res.ok) throw new Error(await readError(res));
  return mapAttendanceAck((await res.json()) as Record<string, unknown>, yearMonth);
}

/** HR: approve month-end verify. */
export async function approveHrAttendanceMonth(
  empCode: string,
  yearMonth: string,
  username?: string,
): Promise<HrAttendanceAck> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/attendance-ack?${params}`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ empCode, yearMonth, verifiedBy: username, approve: true }),
  });
  if (!res.ok) throw new Error(await readError(res));
  return mapAttendanceAck((await res.json()) as Record<string, unknown>, yearMonth);
}

export async function listPendingHrAttendanceAck(username?: string): Promise<HrAttendanceAck[]> {
  const params = withUser(new URLSearchParams(), username);
  const res = await fetch(getApiUrl(`/api/hr/reports/attendance-ack/pending?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>[];
  return (Array.isArray(raw) ? raw : []).map((r) => mapAttendanceAck(r));
}

export type HrBankSheetRow = {
  com: string;
  empCode: string;
  name: string;
  present: number;
  bankName: string;
  bankNo: string;
  ifscCode: string;
  netPayable: number;
  hasErrors: boolean;
  missingFields: string[];
};

export type HrBankSheetPreview = {
  yearMonth: string;
  periodLabel: string;
  daysInMonth: number;
  comCode: string;
  employeeCount: number;
  errorCount: number;
  readyToExport: boolean;
  bankColumnsResolved?: string | null;
  note: string;
  rows: HrBankSheetRow[];
  errors: { empCode: string; name: string; message: string }[];
};

export async function getHrCommonBankPreview(opts: {
  yearMonth: string;
  username?: string;
  company?: string;
  branch?: string;
  officeOnly?: boolean;
  com?: string;
  applyHalfDayRule?: boolean;
}): Promise<HrBankSheetPreview> {
  const params = withUser(new URLSearchParams({ yearMonth: opts.yearMonth }), opts.username);
  if (opts.company) params.set("company", opts.company);
  if (opts.branch) params.set("branch", opts.branch);
  params.set("officeOnly", opts.officeOnly === false ? "false" : "true");
  if (opts.com) params.set("com", opts.com);
  params.set("applyHalfDayRule", opts.applyHalfDayRule === false ? "false" : "true");
  const res = await fetch(getApiUrl(`/api/hr/reports/common-bank?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const raw = (await res.json()) as Record<string, unknown>;
  const rows = ((raw.rows ?? raw.Rows ?? []) as Record<string, unknown>[]).map((r) => ({
    com: String(r.com ?? r.Com ?? ""),
    empCode: String(r.empCode ?? r.EmpCode ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    present: Number(r.present ?? r.Present ?? 0),
    bankName: String(r.bankName ?? r.BankName ?? ""),
    bankNo: String(r.bankNo ?? r.BankNo ?? ""),
    ifscCode: String(r.ifscCode ?? r.IfscCode ?? ""),
    netPayable: Number(r.netPayable ?? r.NetPayable ?? 0),
    hasErrors: Boolean(r.hasErrors ?? r.HasErrors),
    missingFields: ((r.missingFields ?? r.MissingFields ?? []) as unknown[]).map(String),
  }));
  const errors = ((raw.errors ?? raw.Errors ?? []) as Record<string, unknown>[]).map((e) => ({
    empCode: String(e.empCode ?? e.EmpCode ?? ""),
    name: String(e.name ?? e.Name ?? ""),
    message: String(e.message ?? e.Message ?? ""),
  }));
  return {
    yearMonth: String(raw.yearMonth ?? raw.YearMonth ?? opts.yearMonth),
    periodLabel: String(raw.periodLabel ?? raw.PeriodLabel ?? ""),
    daysInMonth: Number(raw.daysInMonth ?? raw.DaysInMonth ?? 0),
    comCode: String(raw.comCode ?? raw.ComCode ?? "HCP"),
    employeeCount: Number(raw.employeeCount ?? raw.EmployeeCount ?? rows.length),
    errorCount: Number(raw.errorCount ?? raw.ErrorCount ?? errors.length),
    readyToExport: Boolean(raw.readyToExport ?? raw.ReadyToExport),
    bankColumnsResolved: (raw.bankColumnsResolved ?? raw.BankColumnsResolved) as string | null,
    note: String(raw.note ?? raw.Note ?? ""),
    rows,
    errors,
  };
}

export async function downloadHrCommonBankExcel(opts: {
  yearMonth: string;
  username?: string;
  company?: string;
  branch?: string;
  officeOnly?: boolean;
  com?: string;
  applyHalfDayRule?: boolean;
  allowErrors?: boolean;
}): Promise<void> {
  const params = withUser(new URLSearchParams({ yearMonth: opts.yearMonth }), opts.username);
  if (opts.company) params.set("company", opts.company);
  if (opts.branch) params.set("branch", opts.branch);
  params.set("officeOnly", opts.officeOnly === false ? "false" : "true");
  if (opts.com) params.set("com", opts.com);
  params.set("applyHalfDayRule", opts.applyHalfDayRule === false ? "false" : "true");
  params.set("allowErrors", opts.allowErrors ? "true" : "false");
  const res = await fetch(getApiUrl(`/api/hr/reports/common-bank/excel?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `common-bank-${opts.yearMonth}.xlsx`;
  a.click();
  URL.revokeObjectURL(url);
}
