export type LedgerCompanyOption = {
  value: string;
  label: string;
  companyType: number;
  companyName: string;
  companyId: number;
};

export type LedgerNameOption = {
  ledgerId: string;
  ledgerName: string;
};

export type LedgerSummaryRow = {
  companyName: string;
  ledgerName: string;
  date?: string | null;
  particulars: string;
  voucherType: string;
  voucherNo: string;
  voucherRef: string;
  debit: number;
  credit: number;
  currency?: string | null;
  debitFc?: number | null;
  creditFc?: number | null;
  excRate: number;
  closing: number;
  closingFc: number;
  days: number;
  interest: number;
  isOpening: boolean;
  approvalStatus?: string | null;
};

export type LedgerSummaryResult = {
  openingBalance: number;
  debitTotal: number;
  creditTotal: number;
  closingBalance: number;
  companyCount: number;
  ledgerCount: number;
  pairCount: number;
  rows: LedgerSummaryRow[];
};

export function financialYearStart(today = new Date()) {
  const year = today.getMonth() >= 3 ? today.getFullYear() : today.getFullYear() - 1;
  return `${year}-04-01`;
}

export function toInputDate(d: Date) {
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}
