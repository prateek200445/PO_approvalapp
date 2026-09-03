import type { ChatApiResponse, ChatTableUsed } from "@/lib/chat-types";
import { buildSmartRowSummary } from "@/lib/row-summary";

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
  PerKg: "Per kg",
  QuantityKg: "Quantity (kg)",
  SalesAmount: "Sales amount",
  MonthsInPeriod: "Months",
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
  NetWt: "Net weight (kg)",
  TotalNetWt: "Net weight (kg)",
  TotalNetWtKg: "Net weight (kg)",
  TotalMetre: "Metres",
  RollCount: "Rolls",
  Quality: "Quality",
  PendingQty: "Pending qty",
  PurchasedQty: "Purchased qty",
  RecdQty: "Received qty",
  AcceptedQty: "Accepted qty",
  OrderQty: "Order qty",
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
  ReportDate: "Report date",
  Department: "Department",
  ProductionQty: "Production",
  WastageQty: "Wastage",
  WastagePct: "Wastage %",
  StitchersPresent: "Present",
  AttendanceDate: "Date",
  PendingPOCount: "Pending POs",
  ExpenseGroupHead: "Expense group",
  ExpenseAmount: "Expense amount",
  TotalExpense: "Total expense",
  VoucherCount: "Vouchers",
  LedgerName: "Ledger",
  Under: "Group",
  DebitBalance: "Debit balance",
  CreditBalance: "Credit balance",
  FyOpeningBalance: "FY opening",
  EffectiveBalance: "Balance",
};

/** Count/quantity columns — never format as currency even if name contains "pending" or "total". */
const COUNT_COLS = /count|cnt|numrecords|rowcount|howmany/i;
const CURRENCY_COLS =
  /amount|bill|payment|rate|balance|debit|credit|opening|pending|price|charges|utr/i;
const DATE_COLS = /date|sysdate|sysDate|timestamp|created|modified/i;
const DOC_COLS = /^(po|indent|payment|inv|mr|issue|gate|roll|code|no|number)/i;
const NUMERIC_COLS = /qty|quantity|stock|stk|weight|wt|pcs|count|level|metre|meter|kg|ton|gsm|gpm|consum/i;

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

  if (isNum && NUMERIC_COLS.test(column)) {
    const decimals = Number.isInteger(num) ? 0 : 2;
    const text = formatIndianNumber(num, decimals);
    if (/wt|weight|\bkg\b/i.test(column) && !/perkg|per_kg|per kg/i.test(column)) {
      return { text: `${text} kg`, kind: "number", raw: value };
    }
    if (/metre|meter/i.test(column)) {
      return { text: `${text} m`, kind: "number", raw: value };
    }
    return {
      text,
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

  const perKgCol = cols.find((c) => /^perkg$/i.test(c) || /^per_kg$/i.test(c));
  if (perKgCol) {
    const formatted = formatCellValue(perKgCol, row[perKgCol]);
    return {
      label: "Per kg",
      value: formatted.kind === "currency" ? `${formatted.text}/kg` : `${formatted.text} /kg`,
      kind: "text",
    };
  }

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
    "LedgerName",
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
  const preferred = [
    "DebitBalance",
    "CreditBalance",
    "EffectiveBalance",
    "Under",
    "FyOpeningBalance",
  ];
  const picked: string[] = [];
  for (const col of preferred) {
    if (columns.includes(col) && col !== primary) picked.push(col);
  }
  for (const col of columns) {
    if (col === primary || picked.includes(col)) continue;
    if (/^pendingbalance$/i.test(col)) continue;
    picked.push(col);
    if (picked.length >= 3) break;
  }
  return picked.slice(0, 3);
}

/** Preferred column order for table/list views; drops stale PendingBalance when Debit/Credit balance is present. */
export function orderDisplayColumns(
  columns: string[],
  rows: Record<string, unknown>[],
): string[] {
  const hasAnomalyBalance = columns.some((c) =>
    /^(DebitBalance|CreditBalance|EffectiveBalance)$/i.test(c),
  );
  const pendingAlwaysZero =
    hasAnomalyBalance
    && columns.some((c) => /^pendingbalance$/i.test(c))
    && rows.every((r) => Math.abs(Number(r.PendingBalance ?? r.pendingBalance ?? 0)) < 0.01);

  const preferred = [
    "LedgerName",
    "Under",
    "DebitBalance",
    "CreditBalance",
    "EffectiveBalance",
    "FyOpeningBalance",
    "Openingbalance",
    "PendingBalance",
  ];

  const ordered: string[] = [];
  for (const col of preferred) {
    const match = columns.find((c) => c.toLowerCase() === col.toLowerCase());
    if (!match) continue;
    if (pendingAlwaysZero && /^pendingbalance$/i.test(match)) continue;
    if (!ordered.includes(match)) ordered.push(match);
  }
  for (const col of columns) {
    if (ordered.includes(col)) continue;
    if (pendingAlwaysZero && /^pendingbalance$/i.test(col)) continue;
    ordered.push(col);
  }
  return ordered;
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
  if (domains.has("warehousestock") || domains.has("warehouse") || objects.includes("warehouse")
      || objects.includes("inventoryitemwarehouse") || objects.includes("itemwisestock")) {
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

  if (/creditors with debit balance|debtors with credit balance|debitbalance|creditbalance/i.test(warning)) {
    return "These are abnormal ledger balances (creditors on debit side / debtors on credit side). Amounts use FY opening balance because ERP pending balance on LedgerMaster is often not updated.";
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

/** One-line KPI summary from known row shapes when answer body is empty. */
export function buildCompactRowSummary(
  rows: Record<string, unknown>[],
): string | null {
  if (rows.length === 0) return null;
  const row = rows[0];
  const keys = Object.keys(row).map((k) => k.toLowerCase());

  if (
    keys.includes("wastageqty")
    || (keys.includes("wastage") && keys.includes("productionqty"))
  ) {
    const dept = String(row.Department ?? row.Particulars ?? "Dept");
    const wastage = formatCellValue("WastageQty", row.WastageQty ?? row.Wastage).text;
    const prod = formatCellValue("ProductionQty", row.ProductionQty ?? row.TapeProduction).text;
    const pct = row.WastagePct != null
      ? formatCellValue("WastagePct", row.WastagePct).text
      : null;
    const date = row.ReportDate ?? row.Sysdate ?? row.date;
    const dateText = date ? formatCellValue("ReportDate", date).text : null;
    return [
      dept,
      dateText,
      `${wastage} wastage / ${prod} prod`,
      pct != null ? `${pct} of production` : null,
    ]
      .filter(Boolean)
      .join(" · ");
  }

  if (keys.includes("stitcherspresent")) {
    const count = formatCellValue("StitchersPresent", row.StitchersPresent).text;
    const date = row.AttendanceDate
      ? formatCellValue("AttendanceDate", row.AttendanceDate).text
      : null;
    return date ? `${count} present · ${date}` : `${count} stitchers/sewers present`;
  }

  if (
    rows.length > 1
    && (keys.includes("debitbalance") || keys.includes("creditbalance"))
    && keys.includes("ledgername")
  ) {
    const balanceCol = keys.includes("debitbalance") ? "DebitBalance" : "CreditBalance";
    const actualCol =
      Object.keys(rows[0]).find((k) => k.toLowerCase() === balanceCol.toLowerCase()) ?? balanceCol;
    const total = rows.reduce((sum, r) => sum + (Number(r[actualCol]) || 0), 0);
    const label = balanceCol === "DebitBalance" ? "debit balance" : "credit balance";
    const underCol = Object.keys(rows[0]).find((k) => k.toLowerCase() === "under");
    if (underCol) {
      const grouped = new Map<string, number>();
      for (const r of rows) {
        const g = String(r[underCol] ?? "Other");
        grouped.set(g, (grouped.get(g) ?? 0) + Math.abs(Number(r[actualCol]) || 0));
      }
      const top = [...grouped.entries()].sort((a, b) => b[1] - a[1]).slice(0, 3);
      const topPart = top
        .map(([g, v]) => `${g} ${formatCellValue(actualCol, v).text}`)
        .join(" · ");
      return `${rows.length} ledgers · ${formatCellValue(actualCol, Math.abs(total)).text} total ${label} · ${topPart}`;
    }
    return `${rows.length} ledgers · ${formatCellValue(actualCol, Math.abs(total)).text} total ${label}`;
  }

  if (keys.includes("stkinhand")) {
    if (rows.length === 1) {
      const item = String(row.ItemName ?? row.ItemCode ?? "Item");
      const stk = formatCellValue("StkInHand", row.StkInHand).text;
      return `${item}: ${stk} in stock`;
    }

    const totalStock = rows.reduce(
      (sum, r) => sum + (Number(r.StkInHand) || 0),
      0,
    );
    const distinctItems = new Set(
      rows.map((r) => String(r.ItemCode ?? r.ItemName ?? "").trim()).filter(Boolean),
    ).size;
    const distinctGodowns = new Set(
      rows.map((r) => String(r.Warehousename ?? r.WareHouseName ?? "").trim()).filter(Boolean),
    ).size;

    const buckets: Record<string, number> = {};
    for (const r of rows) {
      const name = String(r.ItemName ?? r.ItemCode ?? "").toLowerCase();
      const stk = Number(r.StkInHand) || 0;
      if (stk === 0) continue;
      const bucket = name.includes("fabric")
        ? "Fabric"
        : name.includes("webbing")
          ? "Webbing"
          : name.includes("filler")
            ? "Filler"
            : name.includes("yarn")
              ? "Yarn"
              : name.includes("tape")
                ? "Tape"
                : "Other";
      buckets[bucket] = (buckets[bucket] ?? 0) + stk;
    }

    const categoryPart = Object.entries(buckets)
      .filter(([k, v]) => v > 0 && k !== "Other")
      .sort((a, b) => b[1] - a[1])
      .map(([k, v]) => `${k} ${formatCellValue("StkInHand", v).text}`)
      .join(" · ");

    const totalText = formatCellValue("StkInHand", totalStock).text;
    const scope =
      distinctGodowns > 0
        ? `${rows.length} lines · ${totalText} total · ${distinctGodowns} godowns · ${distinctItems} items`
        : `${rows.length} lines · ${totalText} total · ${distinctItems} items`;
    return categoryPart ? `${scope} · ${categoryPart}` : scope;
  }

  if (
    rows.length > 1
    && (keys.includes("wastageqty") || keys.includes("wastage"))
    && keys.includes("productionqty")
  ) {
    const totalProd = rows.reduce(
      (sum, r) => sum + (Number(r.ProductionQty ?? r.TapeProduction) || 0),
      0,
    );
    const totalWastage = rows.reduce(
      (sum, r) => sum + (Number(r.WastageQty ?? r.Wastage) || 0),
      0,
    );
    const pct = totalProd > 0 ? ((totalWastage * 100) / totalProd).toFixed(2) : null;
    const wastageText = formatCellValue("WastageQty", totalWastage).text;
    const prodText = formatCellValue("ProductionQty", totalProd).text;
    return pct != null
      ? `${rows.length} depts · ${wastageText} wastage / ${prodText} prod · ${pct}% overall`
      : `${rows.length} depts · ${wastageText} wastage / ${prodText} prod`;
  }

  return buildSmartRowSummary(rows);
}

export function primarySourceLabel(tables: ChatTableUsed[]): string {
  if (tables.length === 0) return "Live database";
  const top = [...tables].sort((a, b) => b.score - a.score)[0];
  return top?.objectName?.replace(/^vw_/i, "").replace(/_/g, " ") ?? "Live database";
}
