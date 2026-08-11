export type LedgerColumnMapping = {
  sheetName?: string | null;
  headerRow: number;
  company?: string | null;
  date?: string | null;
  particulars?: string | null;
  voucherNo?: string | null;
  voucherRef?: string | null;
  billNo?: string | null;
  billDate?: string | null;
  amount?: string | null;
  debit?: string | null;
  credit?: string | null;
};

export type LedgerMatchOptions = {
  dateToleranceDays: number;
  amountTolerance: number;
};

export type ExcelPreview = {
  fileName: string;
  sheetNames: string[];
  selectedSheet: string;
  headerRow: number;
  headers: string[];
  suggestedMapping: LedgerColumnMapping;
  dataRowCount: number;
  sampleRows: Record<string, string>[];
};

export type LedgerEntry = {
  rowIndex: number;
  company: string;
  date?: string | null;
  billDate?: string | null;
  particulars: string;
  voucherNo: string;
  voucherRef: string;
  billNo: string;
  signedAmount: number;
  debit: number;
  credit: number;
  amount: number;
  side: string;
};

export type ComparisonStatus =
  | "Matched"
  | "AmountMismatch"
  | "MissingInA"
  | "MissingInB"
  | "Duplicate"
  | "PotentialMatch"
  | "PendingRecord";

export type ComparisonPair = {
  id: string;
  status: ComparisonStatus;
  message: string;
  difference?: number | null;
  matchKind?: "bill-group" | "row" | string;
  entryA?: LedgerEntry | null;
  entryB?: LedgerEntry | null;
  entriesA?: LedgerEntry[];
  entriesB?: LedgerEntry[];
};

export type ComparisonSummary = {
  totalA: number;
  totalB: number;
  matched: number;
  amountMismatch: number;
  missingInA: number;
  missingInB: number;
  duplicates: number;
  potentialMatches: number;
  pendingRecords?: number;
};

export type ComparisonResult = {
  companyNameA: string;
  companyNameB: string;
  summary: ComparisonSummary;
  results: ComparisonPair[];
};

export const defaultMatchOptions: LedgerMatchOptions = {
  dateToleranceDays: 0,
  amountTolerance: 0,
};

export const statusLabel: Record<ComparisonStatus, string> = {
  Matched: "Matched",
  AmountMismatch: "Amount mismatch",
  MissingInA: "Missing in A",
  MissingInB: "Missing in B",
  Duplicate: "Duplicate",
  PotentialMatch: "Potential match",
  PendingRecord: "Pending record",
};

export function formatStatusLabel(
  status: ComparisonStatus,
  companyNameA = "A",
  companyNameB = "B",
): string {
  switch (status) {
    case "MissingInA":
      return `Missing in ${companyNameA}`;
    case "MissingInB":
      return `Missing in ${companyNameB}`;
    default:
      return statusLabel[status];
  }
}
