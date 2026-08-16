import type { ChatApiResponse, ChatTableUsed } from "@/lib/chat-types";

export type ResultCardMode = "empty" | "summary" | "list" | "table";

export type CellKind = "currency" | "number" | "date" | "document" | "text" | "empty";

export interface FormattedCell {
  text: string;
  kind: CellKind;
  raw: unknown;
}

export interface DomainTheme {
  label: string;
  accent: string;
  glow: string;
  icon: "approvals" | "stock" | "sales" | "ledger" | "production" | "mrn" | "default";
}

const COLUMN_LABELS: Record<string, string> = {
  ItemCode: "Item code",
  ItemName: "Item",
  StkInHand: "Stock in hand",
  ReOrder: "Reorder level",
  WareHouseName: "Godown",
  Warehousename: "Godown",
  CompanyName: "Company",
  CompName: "Company",
  BuyerName: "Customer",
  FirmName: "Vendor",
  PartyName: "Party",
  BillAMount: "Bill amount",
  PaymentAmount: "Payment amount",
  TotalSalesAmount: "Total sales",
  SalesCountryCategory: "Category",
  Destination: "Country",
  Country: "Country",
  InvoiceCount: "Invoices",
  TotalDebitAmount: "Debit amount",
  TotalCreditAmount: "Credit amount",
  PoNo: "PO number",
  IndentNo: "Indent",
  PaymentNo: "Payment no.",
  InvNo: "Invoice",
  Invno: "Invoice",
  InvDate: "Invoice date",
  PODate: "PO date",
  MRNo: "MRN",
  MRNno: "MRN",
  IssueSlipNo: "Issue slip",
  GatePassNo: "Gate pass",
  RollNo: "Roll no.",
  RollNO: "Roll no.",
  NetWt: "Net weight",
  Quality: "Quality",
  Qty: "Quantity",
  ActualQty: "Qty",
  Rate: "Rate",
  Amount: "Amount",
  HSNCODE: "HSN",
  status: "Status",
  isPaid: "Paid",
  Maxlevel: "Max level",
  Minlevel: "Min level",
  Deptt: "Department",
  PendingPOCount: "Pending POs",
  LedgerCount: "Ledgers",
};

/** Count/quantity columns — never format as currency even if name contains "pending" or "total". */
const COUNT_COLS = /count|cnt|numrecords|rowcount|howmany/i;
const CURRENCY_COLS =
  /amount|bill|payment|rate|balance|total|debit|credit|opening|pending|price|charges|utr/i;
const DATE_COLS = /date|sysdate|sysDate|timestamp|created|modified/i;
const DOC_COLS = /^(po|indent|payment|inv|mr|issue|gate|roll|code|no|number)/i;
const NUMERIC_COLS = /qty|quantity|stock|stk|weight|wt|pcs|count|level|metre|consum/i;

export function humanizeColumn(col: string): string {
  if (COLUMN_LABELS[col]) return COLUMN_LABELS[col];
  return col
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

function parseMaybeDate(value: string): Date | null {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return null;
  if (!/^\d{4}-\d{2}-\d{2}/.test(value) && !value.includes("T")) return null;
  return d;
}

function formatIndianNumber(n: number, decimals = 2): string {
  const fixed = decimals > 0 ? n.toFixed(decimals) : String(Math.round(n));
  const [intPart, decPart] = fixed.split(".");
  const lastThree = intPart.slice(-3);
  const rest = intPart.slice(0, -3);
  const grouped =
    rest.length > 0
      ? `${rest.replace(/\B(?=(\d{2})+(?!\d))/g, ",")},${lastThree}`
      : lastThree;
  return decPart ? `${grouped}.${decPart}` : grouped;
}

export function formatCellValue(column: string, value: unknown): FormattedCell {
  if (value == null || value === "") {
    return { text: "—", kind: "empty", raw: value };
  }

  if (typeof value === "boolean") {
    return { text: value ? "Yes" : "No", kind: "text", raw: value };
  }

  if (typeof value === "object") {
    const text = JSON.stringify(value);
    return { text, kind: "text", raw: value };
  }

  const str = String(value).trim();
  const col = column.toLowerCase();

  if (DATE_COLS.test(column)) {
    const d = parseMaybeDate(str);
    if (d) {
      return {
        text: d.toLocaleDateString("en-IN", {
          day: "numeric",
          month: "short",
          year: "numeric",
        }),
        kind: "date",
        raw: value,
      };
    }
  }

  const num = typeof value === "number" ? value : Number(str.replace(/,/g, ""));
  const isNum = typeof value === "number" || (str !== "" && !Number.isNaN(num) && /^-?\d/.test(str));

  if (isNum && COUNT_COLS.test(column)) {
    return {
      text: formatIndianNumber(num, 0),
      kind: "number",
      raw: value,
    };
  }

  if (isNum && CURRENCY_COLS.test(column)) {
    return {
      text: `₹${formatIndianNumber(num)}`,
      kind: "currency",
      raw: value,
    };
  }

  if (isNum && NUMERIC_COLS.test(column)) {
    const decimals = Number.isInteger(num) ? 0 : 2;
    return {
      text: formatIndianNumber(num, decimals),
      kind: "number",
      raw: value,
    };
  }

  if (DOC_COLS.test(col) || /No$|Code$|Number$/i.test(column)) {
    return { text: str, kind: "document", raw: value };
  }

  return { text: str, kind: "text", raw: value };
}

/** Backward-compatible plain string formatter. */
export function formatCell(value: unknown, column?: string): string {
  if (column) return formatCellValue(column, value).text;
  if (value == null) return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

export function detectResultCardMode(
  rows: Record<string, unknown>[],
): ResultCardMode {
  if (rows.length === 0) return "empty";
  const columns = Object.keys(rows[0]);
  if (rows.length === 1 && columns.length <= 4) {
    const hasMetric = columns.some(
      (c) =>
        COUNT_COLS.test(c) ||
        CURRENCY_COLS.test(c) ||
        NUMERIC_COLS.test(c) ||
        /total|sum/i.test(c),
    );
    if (hasMetric || columns.length <= 2) return "summary";
  }
  if (columns.length <= 4 && rows.length > 1) return "list";
  if (columns.length <= 5 && rows.length <= 8) return "list";
  return "table";
}

export interface HeroMetric {
  label: string;
  value: string;
  kind: CellKind;
}

export function extractHeroMetric(
  rows: Record<string, unknown>[],
): HeroMetric | null {
  if (rows.length !== 1) return null;
  const row = rows[0];
  const cols = Object.keys(row);

  const countCol = cols.find((c) => COUNT_COLS.test(c));
  if (countCol) {
    const formatted = formatCellValue(countCol, row[countCol]);
    return {
      label: humanizeColumn(countCol),
      value: formatted.text,
      kind: formatted.kind,
    };
  }

  const priority = cols.find(
    (c) =>
      CURRENCY_COLS.test(c) ||
      /total|sum|balance|amount|qty|stock|stk/i.test(c),
  );
  if (priority) {
    const formatted = formatCellValue(priority, row[priority]);
    return {
      label: humanizeColumn(priority),
      value: formatted.text,
      kind: formatted.kind,
    };
  }

  const firstVal = cols.find((c) => row[c] != null && row[c] !== "");
  if (firstVal) {
    const formatted = formatCellValue(firstVal, row[firstVal]);
    return {
      label: humanizeColumn(firstVal),
      value: formatted.text,
      kind: formatted.kind,
    };
  }
  return null;
}

export function getListPrimaryColumn(columns: string[]): string {
  const preferred = [
    "Country",
    "SalesCountryCategory",
    "Destination",
    "ItemName",
    "BuyerName",
    "FirmName",
    "PartyName",
    "PoNo",
    "IndentNo",
    "PaymentNo",
    "InvNo",
    "Invno",
    "ItemCode",
  ];
  for (const p of preferred) {
    if (columns.includes(p)) return p;
  }
  return columns[0] ?? "";
}

export function getListSecondaryColumns(columns: string[], primary: string): string[] {
  return columns.filter((c) => c !== primary).slice(0, 3);
}

export function hasReorderProgress(columns: string[]): boolean {
  return columns.some((c) => /stk/i.test(c)) && columns.some((c) => /reorder/i.test(c));
}

export function reorderProgress(row: Record<string, unknown>): number | null {
  const stkCol = Object.keys(row).find((c) => /stk/i.test(c));
  const reCol = Object.keys(row).find((c) => /reorder/i.test(c));
  if (!stkCol || !reCol) return null;
  const stk = Number(row[stkCol]);
  const re = Number(row[reCol]);
  if (!re || re <= 0 || Number.isNaN(stk) || Number.isNaN(re)) return null;
  return Math.min(100, Math.max(0, (stk / re) * 100));
}

export function getDomainTheme(response: ChatApiResponse, answer?: string): DomainTheme {
  const sql = (response.sql ?? "").toLowerCase();
  const answerText = (answer ?? response.answer ?? "").toLowerCase();

  if (
    sql.includes("vw_countrywise_sales")
    || sql.includes("vw_salesvoucher")
    || sql.includes("salesvoucher")
    || sql.includes("salesvoucheritem")
    || answerText.includes("sales invoice")
    || answerText.includes("export sales")
  ) {
    return {
      label: "Sales",
      accent: "from-violet-400 to-purple-500",
      glow: "bg-violet-500/25",
      icon: "sales",
    };
  }

  const domains = new Set(
    response.tablesUsed.map((t) => t.domain.toLowerCase()).filter(Boolean),
  );
  const objects = response.tablesUsed.map((t) => t.objectName.toLowerCase()).join(" ");

  if (
    domains.has("po") ||
    domains.has("payment") ||
    objects.includes("approvepo") ||
    objects.includes("purchasepayment")
  ) {
    return {
      label: "Approvals",
      accent: "from-sky-400 to-blue-500",
      glow: "bg-sky-500/25",
      icon: "approvals",
    };
  }
  if (domains.has("warehousestock") || domains.has("warehouse") || objects.includes("warehouse")) {
    return {
      label: "Stock & inventory",
      accent: "from-emerald-400 to-teal-500",
      glow: "bg-emerald-500/25",
      icon: "stock",
    };
  }
  if (domains.has("sales") || objects.includes("salesvoucher")) {
    return {
      label: "Sales",
      accent: "from-violet-400 to-purple-500",
      glow: "bg-violet-500/25",
      icon: "sales",
    };
  }
  if (domains.has("ledger") || objects.includes("ledgermaster")) {
    return {
      label: "Ledger",
      accent: "from-amber-400 to-orange-500",
      glow: "bg-amber-500/25",
      icon: "ledger",
    };
  }
  if (domains.has("production") || objects.includes("production")) {
    return {
      label: "Production",
      accent: "from-rose-400 to-pink-500",
      glow: "bg-rose-500/25",
      icon: "production",
    };
  }
  if (domains.has("mrn") || domains.has("vendor") || objects.includes("storeinward")) {
    return {
      label: "MRN & vendors",
      accent: "from-cyan-400 to-sky-500",
      glow: "bg-cyan-500/25",
      icon: "mrn",
    };
  }
  return {
    label: "Data insight",
    accent: "from-indigo-400 to-violet-500",
    glow: "bg-indigo-500/25",
    icon: "default",
  };
}

export function isGovernedResponse(warning?: string | null): boolean {
  if (!warning) return false;
  return /governed|rewrote|^erp\s|verified/i.test(warning);
}

/** Warnings safe to show end users (no table/SQL names). */
export function userFacingWarning(warning?: string | null): string | null {
  if (!warning) return null;

  if (/stock analysis|STOCK_ANALYSIS|opening.*stale|warehouse\.stkinhand/i.test(warning)) {
    return "Opening/closing stock analysis may not match current warehouse stock due to known ERP data limitations.";
  }

  // Governed / ERP routing notes are for developers — hide from business users.
  if (isGovernedResponse(warning)) return null;

  return null;
}

export function shortenWarning(warning: string): string {
  return warning
    .replace(/^Governed\s+/i, "")
    .replace(/\s*\([^)]*\)\s*\.?$/g, "")
    .trim();
}

/** Split answer into title + body using first paragraph. */
export function splitAnswerRich(answer: string): { title: string; body: string } {
  const trimmed = answer.trim();
  if (!trimmed) return { title: "No answer returned", body: "" };

  const paragraphs = trimmed.split(/\n\s*\n/).filter(Boolean);
  if (paragraphs.length >= 2) {
    const title = paragraphs[0].replace(/\n/g, " ").trim();
    const body = paragraphs.slice(1).join("\n\n").trim();
    if (title.length <= 200) return { title, body };
  }

  const firstLine = trimmed.split("\n")[0]?.trim() ?? trimmed;
  if (firstLine.length <= 140) {
    const rest = trimmed.slice(firstLine.length).trim();
    return { title: firstLine, body: rest.replace(/^\n+/, "") };
  }

  const dot = trimmed.search(/[.!?]\s+/);
  if (dot > 20 && dot < 160) {
    return {
      title: trimmed.slice(0, dot + 1).trim(),
      body: trimmed.slice(dot + 1).trim(),
    };
  }

  if (trimmed.length <= 160) return { title: trimmed, body: "" };
  return { title: `${trimmed.slice(0, 157)}…`, body: trimmed };
}

export function primarySourceLabel(tables: ChatTableUsed[]): string {
  if (tables.length === 0) return "Live database";
  const top = [...tables].sort((a, b) => b.score - a.score)[0];
  return top?.objectName?.replace(/^vw_/i, "").replace(/_/g, " ") ?? "Live database";
}
