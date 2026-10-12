import { getApiUrl } from "@/lib/api-config";

export type HrPayslipLine = { label: string; amount: number };

export type HrPayslip = {
  yearMonth: string;
  periodLabel: string;
  payType: "Monthly" | "Per day" | string;
  employee: {
    empCode: string;
    name: string;
    fatherName: string | null;
    designation: string | null;
    department: string | null;
    company: string | null;
    branch: string | null;
    dateOfJoining: string | null;
    pfAccount: string | null;
    uan: string | null;
    esic: string | null;
    pan: string | null;
    bankName: string | null;
    bankAccountMasked: string | null;
    ifsc: string | null;
  };
  photoDataUrl: string | null;
  daysInMonth: number;
  paidDays: number;
  workingDays: number | null;
  leaveDays: number | null;
  overtimeHours: number | null;
  ratePerDay: number | null;
  paymentMode: string | null;
  earnings: HrPayslipLine[];
  deductions: HrPayslipLine[];
  notes: HrPayslipLine[];
  grossEarnings: number;
  totalDeductions: number;
  netPay: number;
  netPayInWords: string;
  generatedAt: string;
};

async function readError(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as { message?: string };
    if (data?.message) return data.message;
  } catch {
    /* ignore */
  }
  if (res.status === 403) return "You can only view your own salary slip.";
  return `Request failed (${res.status})`;
}

export async function getPayslipMonths(empCode: string, username: string): Promise<string[]> {
  const res = await fetch(getApiUrl(`/api/hr/payslip/months?${new URLSearchParams({ empCode, username })}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as string[];
}

export async function getPayslip(empCode: string, yearMonth: string, username: string): Promise<HrPayslip> {
  const res = await fetch(getApiUrl(`/api/hr/payslip?${new URLSearchParams({ empCode, yearMonth, username })}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as HrPayslip;
}

export function payslipPdfUrl(empCode: string, yearMonth: string, username: string): string {
  return getApiUrl(`/api/hr/payslip/pdf?${new URLSearchParams({ empCode, yearMonth, username })}`);
}
