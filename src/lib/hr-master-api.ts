import { getApiUrl } from "@/lib/api-config";

export type HrMasterStatus = "current" | "left_fy" | "left" | "all";

export type HrMasterFilters = {
  company?: string;
  status?: HrMasterStatus;
  designation?: string;
  department?: string;
  search?: string;
};

export type HrMasterSummary = {
  currentHeadcount: number;
  male: number;
  female: number;
  otherGender: number;
  joinersFy: number;
  leaversFy: number;
  attritionFyPct: number;
  avgTenureYears: number;
  avgAgeYears: number;
  pfCovered: number;
  esicCovered: number;
  monthlySalaryTotal: number;
  leftFlaggedActive: number;
};

export type HrHeadcountRow = {
  name: string;
  count: number;
  male: number;
  female: number;
  monthlySalary: number;
  byCompany: { name: string; count: number }[];
};

export type HrAttritionMonth = {
  month: string;
  label: string;
  opening: number;
  joiners: number;
  leavers: number;
  closing: number;
  avgHeadcount: number;
  ratePct: number;
};

export type HrAttritionGroup = {
  name: string;
  currentHeadcount: number;
  avgHeadcount: number;
  joiners: number;
  leavers: number;
  ratePct: number;
};

export type HrMasterEmployee = {
  empCode: string;
  name: string;
  fatherName: string;
  gender: string;
  dob?: string | null;
  age?: number | null;
  mobile: string;
  originState: string;
  permanentAddress: string;
  company: string;
  branch: string;
  department: string;
  designation: string;
  dateOfJoining?: string | null;
  tenureYears?: number | null;
  status: string;
  exitDate?: string | null;
  exitReason: string;
  monthlySalary?: number | null;
  salaryBasis: string;
  ctc?: number | null;
  salaryPerDay?: number | null;
  lastIncrementDate?: string | null;
  lastIncrementAmount?: number | null;
  lastIncrementPct?: number | null;
  revisions: number;
  pfApplicable: string;
  pfAccount: string;
  uan: string;
  esic: string;
};

export type HrMasterReport = {
  generatedAt: string;
  dataAsOf: string;
  fyLabel: string;
  summary: HrMasterSummary;
  designations: HrHeadcountRow[];
  departments: HrHeadcountRow[];
  companies: HrHeadcountRow[];
  originStates: HrHeadcountRow[];
  attrition: HrAttritionMonth[];
  attritionByCompany: HrAttritionGroup[];
  attritionByDepartment: HrAttritionGroup[];
  employeeTotal: number;
  employees: HrMasterEmployee[];
};

function buildParams(filters: HrMasterFilters, username: string) {
  const params = new URLSearchParams();
  if (username.trim()) params.set("username", username.trim());
  if (filters.company?.trim()) params.set("company", filters.company.trim());
  if (filters.status) params.set("status", filters.status);
  if (filters.designation?.trim()) params.set("designation", filters.designation.trim());
  if (filters.department?.trim()) params.set("department", filters.department.trim());
  if (filters.search?.trim()) params.set("search", filters.search.trim());
  return params;
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

export async function getHrMasterReport(
  filters: HrMasterFilters,
  username: string,
  limit = 200,
  refresh = false,
): Promise<HrMasterReport> {
  const params = buildParams(filters, username);
  params.set("limit", String(limit));
  if (refresh) params.set("refresh", "true");
  const res = await fetch(getApiUrl(`/api/hr/master?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as HrMasterReport;
}

export async function getHrMasterCompanies(username: string): Promise<string[]> {
  const params = buildParams({}, username);
  const res = await fetch(getApiUrl(`/api/hr/master/companies?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as string[];
}

export async function downloadHrMasterExcel(filters: HrMasterFilters, username: string) {
  const params = buildParams(filters, username);
  const res = await fetch(getApiUrl(`/api/hr/master/excel?${params}`));
  if (!res.ok) throw new Error(await readError(res));
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `hr-master-${new Date().toISOString().slice(0, 10)}.xlsx`;
  a.click();
  URL.revokeObjectURL(url);
}
