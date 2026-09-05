import { getApiUrl } from "@/lib/api-config";

export type DailyReportMonth = {
  year: number;
  month: number;
  value: string;
  label: string;
  reportCount: number;
};

export type DailyReportItem = {
  employeeName: string;
  department: string;
  submittedOn: string;
  submittedForDate: string;
  firstHalf: string;
  secondHalf: string;
  tomorrowTasks: string[];
};

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

export async function getDailyReportPeople(year?: number, month?: number): Promise<string[]> {
  const params = new URLSearchParams();
  if (year && month) {
    params.set("year", String(year));
    params.set("month", String(month));
  }
  const qs = params.toString();
  const response = await fetch(getApiUrl(`/api/DailyReport/people${qs ? `?${qs}` : ""}`));
  const payload = (await response.json()) as { people?: unknown; People?: unknown; message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to load people");
  const raw = (payload.people ?? payload.People ?? []) as unknown[];
  return raw.map((n) => str(n).trim()).filter(Boolean);
}

export async function getDailyReportMonths(): Promise<DailyReportMonth[]> {
  const response = await fetch(getApiUrl("/api/DailyReport/months"));
  const payload = (await response.json()) as {
    months?: Array<Record<string, unknown>>;
    Months?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) throw new Error(payload.message || "Failed to load months");
  const raw = payload.months ?? payload.Months ?? [];
  return raw.map((m) => {
    const year = num(m.year ?? m.Year);
    const month = num(m.month ?? m.Month);
    const value = str(m.value ?? m.Value) || `${year.toString().padStart(4, "0")}-${month.toString().padStart(2, "0")}`;
    return {
      year,
      month,
      value,
      label: str(m.label ?? m.Label) || value,
      reportCount: num(m.reportCount ?? m.ReportCount),
    };
  });
}

export async function getDailyReports(filters: {
  year: number;
  month: number;
  employee: string;
}): Promise<DailyReportItem[]> {
  const params = new URLSearchParams({
    year: String(filters.year),
    month: String(filters.month),
    employee: filters.employee,
  });
  const response = await fetch(getApiUrl(`/api/DailyReport?${params}`));
  const payload = (await response.json()) as {
    reports?: Array<Record<string, unknown>>;
    Reports?: Array<Record<string, unknown>>;
    message?: string;
  };
  if (!response.ok) throw new Error(payload.message || "Failed to load daily reports");
  const raw = payload.reports ?? payload.Reports ?? [];
  return raw.map((r) => ({
    employeeName: str(r.employeeName ?? r.EmployeeName),
    department: str(r.department ?? r.Department),
    submittedOn: str(r.submittedOn ?? r.SubmittedOn),
    submittedForDate: str(r.submittedForDate ?? r.SubmittedForDate),
    firstHalf: str(r.firstHalf ?? r.FirstHalf),
    secondHalf: str(r.secondHalf ?? r.SecondHalf),
    tomorrowTasks: Array.isArray(r.tomorrowTasks ?? r.TomorrowTasks)
      ? ((r.tomorrowTasks ?? r.TomorrowTasks) as unknown[]).map((t) => str(t)).filter(Boolean)
      : [],
  }));
}

export function currentMonthValue(date = new Date()): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
}

export function parseMonthValue(value: string): { year: number; month: number } | null {
  const match = /^(\d{4})-(\d{2})$/.exec(value);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  if (month < 1 || month > 12) return null;
  return { year, month };
}

export function formatReportDate(value: string): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function formatReportDateTime(value: string): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}
