import { getApiUrl } from "@/lib/api-config";

export type BankStatementRow = {
  id: string;
  erpBankLedgerName: string;
  partyName: string;
  valueDate: string;
  narration: string;
  refChequeNo: string;
  debit: number | null;
  credit: number | null;
  sourceLine?: number | null;
  transactionType?: string;
  mainCategory?: string;
  subCategory?: string;
};

export type BankStatementParseResult = {
  fileName: string;
  rowCount: number;
  rows: BankStatementRow[];
  sourceKind?: string | null;
  erpBankLedgerName?: string | null;
  message?: string | null;
};

function newId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID().replace(/-/g, "");
  }
  return `row-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function emptyBankStatementRow(
  defaults?: Partial<BankStatementRow>,
): BankStatementRow {
  return {
    id: newId(),
    erpBankLedgerName: "",
    partyName: "",
    valueDate: "",
    narration: "",
    refChequeNo: "",
    debit: null,
    credit: null,
    transactionType: "",
    mainCategory: "",
    subCategory: "",
    ...defaults,
  };
}

function containsAny(text: string, needles: string[]) {
  const t = text.toUpperCase();
  return needles.some((n) => t.includes(n.toUpperCase()));
}

/** Categorize a row from narration / debit-credit (same rules as API). */
export function categorizeBankRow(row: BankStatementRow): BankStatementRow {
  const hay = `${row.narration} ${row.partyName} ${row.refChequeNo}`;
  const isCredit = (row.credit ?? 0) > 0;
  const isDebit = (row.debit ?? 0) > 0;

  const transactionType =
    isCredit && !isDebit
      ? "RECEIPT"
      : isDebit && !isCredit
        ? "PAYMENT"
        : isCredit
          ? "RECEIPT"
          : "PAYMENT";

  let mainCategory = "Other / Review";
  let subCategory = isCredit ? "Unknown Receipt" : "Unknown Payment";

  if (containsAny(hay, ["GST", "GSTN", "GST PAYMENT", "CGST", "SGST", "IGST"])) {
    mainCategory = "Taxes & Government";
    subCategory = "GST";
  } else if (containsAny(hay, ["TDS", "INCOME TAX", "ADVANCE TAX"])) {
    mainCategory = "Taxes & Government";
    subCategory = "TDS";
  } else if (containsAny(hay, ["BANK CHARGES", "CHARGES", "CHG", "SMS CHARGES", "AMC"])) {
    mainCategory = "Banking";
    subCategory = "Bank Charges";
  } else if (/\bINT\d*DEAL\b/i.test(hay) || containsAny(hay, ["INTEREST", "INT."])) {
    mainCategory = "Banking";
    subCategory = "Interest";
  } else if (containsAny(hay, ["SALARY", "PAYROLL", "WAGES", "STAFF SAL"])) {
    mainCategory = "Employee Payment";
    subCategory = "Salary / Wages";
  } else if (containsAny(hay, ["ELECTRIC", "ELECTRICITY", "TORRENT", "UGVCL", "PGVCL"])) {
    mainCategory = "Operating Expense";
    subCategory = "Electricity";
  } else if (containsAny(hay, ["FREIGHT", "TRANSPORT", "LOGISTIC", "COURIER"])) {
    mainCategory = "Operating Expense";
    subCategory = "Transport / Freight";
  } else if (
    containsAny(hay, [
      "PLASTENE INDIA",
      "PLASTENE POLY",
      "HCP PLASTENE",
      "OSWAL EXTRUSION",
      "K.P. WOVEN",
      "KP WOVEN",
    ])
  ) {
    mainCategory = "Intercompany";
    subCategory = isCredit ? "Intercompany Receipt" : "Intercompany Payment";
  } else if (isCredit) {
    mainCategory = "Customer Receipt";
    subCategory = "Invoice Collection";
  } else if (isDebit) {
    mainCategory = "Supplier Payment";
    subCategory = "Purchase Payment";
  }

  return {
    ...row,
    transactionType,
    mainCategory,
    subCategory,
  };
}

export function categorizeBankRows(rows: BankStatementRow[]): BankStatementRow[] {
  return rows.map(categorizeBankRow);
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

/** Convert raw bank PDF/CSV/Excel into BANKIMPORTFORMATE rows. */
export async function convertBankStatement(
  file: File,
  erpBankLedgerName?: string,
): Promise<BankStatementParseResult> {
  const form = new FormData();
  form.append("file", file);
  if (erpBankLedgerName?.trim()) {
    form.append("erpBankLedgerName", erpBankLedgerName.trim());
  }
  const res = await fetch(getApiUrl("/api/bank-statement/convert"), {
    method: "POST",
    body: form,
  });
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as BankStatementParseResult;
}

/** Load a file already in BANKIMPORTFORMATE.csv layout. */
export async function parseBankStatementCsv(file: File): Promise<BankStatementParseResult> {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(getApiUrl("/api/bank-statement/parse"), {
    method: "POST",
    body: form,
  });
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as BankStatementParseResult;
}

export async function downloadBankStatementTemplate() {
  const res = await fetch(getApiUrl("/api/bank-statement/template"));
  if (!res.ok) throw new Error(await readError(res));
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "BANKIMPORTFORMATE.csv";
  a.click();
  URL.revokeObjectURL(url);
}

export async function exportBankStatementCsv(
  rows: BankStatementRow[],
  fileName?: string,
  includeCategorization = false,
) {
  const res = await fetch(getApiUrl("/api/bank-statement/export"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ rows, fileName, includeCategorization }),
  });
  if (!res.ok) throw new Error(await readError(res));
  const blob = await res.blob();
  const disposition = res.headers.get("Content-Disposition") ?? "";
  const match = /filename\*?=(?:UTF-8''|")?([^\";]+)/i.exec(disposition);
  const name =
    (match?.[1] ? decodeURIComponent(match[1].replace(/"/g, "")) : null) ??
    fileName ??
    "bank-import.csv";
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  URL.revokeObjectURL(url);
}

export function formatBankAmount(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) return "";
  return value.toLocaleString("en-IN", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });
}
