export type PnlCompanyOption = {
  value: string;
  label: string;
  kind: "all" | "group" | "company" | string;
};

export type PnlLedgerLine = {
  ledgerName: string;
  amount: number;
  amountLacs: number;
};

export type PnlHeadGroup = {
  category: string;
  head: string;
  amount: number;
  amountLacs: number;
  ledgers: PnlLedgerLine[];
};

export type PnlIncomeExpenseResult = {
  company: string;
  dateFrom: string;
  dateTo: string;
  incomeLacs: number;
  expenseLacs: number;
  heads: PnlHeadGroup[];
};

export type PnlProvisionRow = {
  ledgerName: string;
  amount: number;
  amountLacs: number;
};

export type PnlProvisionState = {
  company: string;
  month: string;
  rows: PnlProvisionRow[];
  ledgerOptions: string[];
};

export type PnlStockRow = {
  category: string;
  label: string;
  openingLacs: number;
  closingLacs: number;
};

export type PnlStockState = {
  company: string;
  month: string;
  rows: PnlStockRow[];
};

export type PnlStockYearColumn = {
  key: string;
  label: string;
};

export type PnlStockYearRow = {
  category: string;
  label: string;
  openingLacs: number;
  months: Record<string, number>;
};

export type PnlStockYearState = {
  company: string;
  fyStart: number;
  fyLabel: string;
  columns: PnlStockYearColumn[];
  rows: PnlStockYearRow[];
};

export type PnlOverheadState = {
  company: string;
  month: string;
  commonLacs: number;
  hoLacs: number;
};

export type PnlUploadItem = {
  id: number;
  company: string;
  month: string;
  uploadType: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  remarks?: string | null;
  uploadedAt: string;
};

export type PnlWorkbookSheetResult = {
  sheet: string;
  month: string;
  uploadType: string;
  status: string;
  detail?: string | null;
};

export type PnlWorkbookImportResult = {
  ok: boolean;
  company: string;
  months: string[];
  sheets: PnlWorkbookSheetResult[];
  zeroedCommonHo: boolean;
};

export type PnlStatementRow = {
  id: string;
  label: string;
  kind: "header" | "line" | "total" | "computed" | "result" | string;
  monthLacs: number | null;
  pctToSales: number | null;
  ytdLacs: number | null;
  ytdPctToSales: number | null;
};

export type PnlStatementResult = {
  company: string;
  month: string;
  dateFrom: string;
  dateTo: string;
  ytdFrom: string;
  ebitdaLacs: number;
  pbtLacs: number;
  stockIncomplete: boolean;
  rows: PnlStatementRow[];
  unmapped: PnlHeadGroup[];
};

export function currentMonthValue(d = new Date()) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
}

export function monthRange(month: string) {
  const [y, m] = month.split("-").map(Number);
  const from = `${y}-${String(m).padStart(2, "0")}-01`;
  const last = new Date(y, m, 0).getDate();
  const to = `${y}-${String(m).padStart(2, "0")}-${String(last).padStart(2, "0")}`;
  return { from, to };
}

export function formatLacs(n: number | null | undefined) {
  if (n == null || Number.isNaN(n)) return "—";
  return n.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function formatPct(n: number | null | undefined) {
  if (n == null || Number.isNaN(n)) return "—";
  return `${(n * 100).toFixed(1)}%`;
}

export function isSingleCompany(value: string) {
  return Boolean(value) && value !== "All Companies" && !value.startsWith("G-");
}
