export const DEFAULT_DEBTOR_COMPANY = "G-Plastene India Limited";

export type DebtorCompanyOption = {
  value: string;
  label: string;
  companyType: number;
  companyName: string;
  companyId: number;
};

export type DebtorBillRow = {
  type: string;
  category: string;
  companyName: string;
  partyName: string;
  gstin: string;
  invoiceNo: string;
  invoiceDate: string;
  originalAmount: number;
  allocatedAmount: number;
  netAmount: number;
  days: number;
  ageing: string;
  ageing2: string;
  status: string;
  under: string;
};

export type DebtorPivotRow = {
  partyName: string;
  gstin: string;
  type: string;
  category: string;
  zeroTo120: number;
  oneTo90: number;
  ninetyOneTo120: number;
  oneTwentyOneTo180: number;
  over180: number;
  grandTotal: number;
  asPerBook: number;
  diff: number;
  originalTotal: number;
  allocatedTotal: number;
  status: string;
};

export type DebtorBookDebtRow = {
  bucket: string;
  government: number;
  associates: number;
  other: number;
  total: number;
};

export type DebtorStatementKpis = {
  companyCount: number;
  partyCount: number;
  billCount: number;
  openBillCount: number;
  originalTotal: number;
  allocatedTotal: number;
  netTotal: number;
  bookTotal: number;
  diffTotal: number;
  lifoPartyCount: number;
  nonBillGapPartyCount: number;
};

export type DebtorStatementResult = {
  company: string;
  companyLabel: string;
  asOn: string;
  includeCurrentAssets: boolean;
  freezeRule: string;
  allocationRule: string;
  kpis: DebtorStatementKpis;
  bills: DebtorBillRow[];
  pivot: DebtorPivotRow[];
  bookDebts: DebtorBookDebtRow[];
};

export type DebtorStatementQuery = {
  company: string;
  asOn: string;
  includeCurrentAssets: boolean;
};

export function previousMonthEnd(today = new Date()): string {
  const first = new Date(today.getFullYear(), today.getMonth(), 1);
  const end = new Date(first.getTime() - 24 * 60 * 60 * 1000);
  const yyyy = end.getFullYear();
  const mm = String(end.getMonth() + 1).padStart(2, "0");
  const dd = String(end.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

export function formatInr(n: number): string {
  return n.toLocaleString("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

export function formatInrCr(n: number): string {
  return `${(n / 10_000_000).toLocaleString("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} Cr`;
}
