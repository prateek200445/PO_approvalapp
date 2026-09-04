/** CMA figures mapped from PIL standalone FS (Rs. lakh → INR crore). */

export const CMA_COMPANY = "Plastene India Limited";
export const CMA_UNIT = "INR Crore";

export type CmaYear = "y2025" | "y2026";

export type CmaLine = {
  id: string;
  sl?: string;
  label: string;
  y2025: number | null;
  y2026: number | null;
  kind?: "head" | "line" | "sub" | "total";
  note?: string;
};

const L = (lakhs: number) => lakhs / 100;

function sum(lines: CmaLine[], ids: string[], year: CmaYear): number {
  return ids.reduce((acc, id) => acc + (lines.find((l) => l.id === id)?.[year] ?? 0), 0);
}

function withTotal(
  lines: CmaLine[],
  id: string,
  sl: string | undefined,
  label: string,
  ids: string[],
  kind: CmaLine["kind"] = "total",
): CmaLine {
  return {
    id,
    sl,
    label,
    kind,
    y2025: sum(lines, ids, "y2025"),
    y2026: sum(lines, ids, "y2026"),
  };
}

/** Face totals from the standalone balance sheet (lakh). */
const BS = {
  share: { y2025: 2819.82, y2026: 2819.82 },
  reserves: { y2025: 15489.55, y2026: 15688.26 },
  ltBorrow: { y2025: 4598.22, y2026: 3008.47 },
  dtl: { y2025: 588, y2026: 588.2 },
  ltProv: { y2025: 143.4, y2026: 392 },
  stBorrow: { y2025: 13512.58, y2026: 13538.46 },
  tradePay: { y2025: 48.38 + 779.3, y2026: 76.15 + 1101.19 },
  ocl: { y2025: 2150.19, y2026: 1760.48 },
  stProv: { y2025: 62.63, y2026: 219.3 },
  ppe: { y2025: 7618.33, y2026: 7552.93 },
  intang: { y2025: 1665.48, y2026: 1283.52 },
  cwip: { y2025: 1210.22, y2026: 1557.38 },
  ncInvest: { y2025: 256.9, y2026: 256.9 },
  ncLoans: { y2025: 157.6, y2026: 186.5 },
  ncOther: { y2025: 4660.28, y2026: 4058.74 },
  inventory: { y2025: 6827.02, y2026: 8162.25 },
  receivables: { y2025: 11866.27, y2026: 10721.33 },
  cash: { y2025: 6.78, y2026: 108.5 },
  caLoans: { y2025: 5053.79, y2026: 4955.35 },
  caOther: { y2025: 869.4, y2026: 349 },
};

export const cmaMeta = {
  company: CMA_COMPANY,
  unit: CMA_UNIT,
  sourceFs: "PIL FS March-26 (31.08R) — standalone, as at 31 March 2026 / 2025",
  sourceCma: "PIL CMA FY25 ABS (updated Sept 2026) — Form II / Form III layout",
  years: [
    { key: "y2025" as const, label: "2025", status: "Audited", asAt: "31 March 2025" },
    { key: "y2026" as const, label: "2026", status: "Audited", asAt: "31 March 2026" },
  ],
};

const assetLines: CmaLine[] = [
  { id: "ca-head", label: "Current assets", kind: "head", y2025: null, y2026: null },
  { id: "cash", sl: "26", label: "Cash and bank balances", kind: "line", y2025: L(BS.cash.y2025), y2026: L(BS.cash.y2026), note: "Note 17 current" },
  { id: "fd", sl: "", label: "Fixed deposits with banks (current)", kind: "sub", y2025: 0, y2026: 0, note: "Margin FDs sit in non-current assets (note 14 / 17)" },
  { id: "rec-dom", sl: "28(i)", label: "Receivables other than deferred & exports", kind: "line", y2025: L(389.34 + 8375.32), y2026: L(1427.21 + 5498.58), note: "Note 16: >6 months + other receivables. FS has no export/domestic debtor split." },
  { id: "rec-exp", sl: "28(ii)", label: "Export receivables", kind: "line", y2025: 0, y2026: 0, note: "Not disclosed separately in the FS" },
  { id: "edfs", sl: "29", label: "Instalments of deferred / EDFS receivable", kind: "line", y2025: L(3101.61), y2026: L(3795.54), note: "Note 16 EDFS receivable" },
  { id: "inv-head", sl: "30", label: "Inventory", kind: "head", y2025: L(BS.inventory.y2025), y2026: L(BS.inventory.y2026) },
  { id: "inv-rm", sl: "", label: "Raw materials (indigenous, incl. packing)", kind: "sub", y2025: L(922.04 + 14.79), y2026: L(2637.55 + 19.8) },
  { id: "inv-wip", sl: "", label: "Stock-in-process", kind: "sub", y2025: L(841.9), y2026: L(1946.39) },
  { id: "inv-fg", sl: "", label: "Finished goods", kind: "sub", y2025: L(4798.85), y2026: L(3327.93) },
  { id: "inv-spares", sl: "", label: "Stores, spares and consumables", kind: "sub", y2025: L(249.42), y2026: L(230.58) },
  { id: "ca-invest", sl: "31", label: "Current investments", kind: "line", y2025: 0, y2026: 0 },
  { id: "adv-tax", sl: "32", label: "Advance payment of taxes", kind: "line", y2025: L(395.79), y2026: L(351.19), note: "Note 13 current" },
  { id: "st-loans", sl: "33", label: "Short-term loans & advances", kind: "line", y2025: L(25.99 + 886.67), y2026: L(25.47 + 1220.89), note: "Employees + other advances" },
  { id: "adv-supply", sl: "", label: "Advance for supply of goods / services", kind: "sub", y2025: L(2183.22), y2026: L(2361.71) },
  { id: "cap-adv-ca", sl: "", label: "Capital advances (current)", kind: "sub", y2025: L(567.76), y2026: L(594.71) },
  { id: "exp-inc", sl: "", label: "Export incentive receivables", kind: "sub", y2025: L(869.42), y2026: L(322.94), note: "Note 14 current" },
  { id: "govt", sl: "", label: "Balances with government authorities", kind: "sub", y2025: L(823.32), y2026: L(253.4) },
  { id: "mat", sl: "", label: "MAT credit entitlement", kind: "sub", y2025: L(128.18), y2026: L(128.18) },
  { id: "prepaid", sl: "", label: "Prepaid expenses", kind: "sub", y2025: L(42.86), y2026: L(19.78) },
  { id: "int-fd", sl: "", label: "Interest accrued on fixed deposits", kind: "sub", y2025: 0, y2026: L(26.02) },
];

const caIds = [
  "cash",
  "fd",
  "rec-dom",
  "rec-exp",
  "edfs",
  "inv-rm",
  "inv-wip",
  "inv-fg",
  "inv-spares",
  "ca-invest",
  "adv-tax",
  "st-loans",
  "adv-supply",
  "cap-adv-ca",
  "exp-inc",
  "govt",
  "mat",
  "prepaid",
  "int-fd",
];

assetLines.push(withTotal(assetLines, "ca-total", "34", "Total current assets", caIds));

assetLines.push(
  { id: "fa-head", label: "Fixed assets (WDV as in FS — depreciation already netted)", kind: "head", y2025: null, y2026: null },
  { id: "ppe", sl: "35", label: "Property, plant and equipment", kind: "line", y2025: L(BS.ppe.y2025), y2026: L(BS.ppe.y2026), note: "Note 11" },
  { id: "intang", sl: "", label: "Intangible assets", kind: "sub", y2025: L(BS.intang.y2025), y2026: L(BS.intang.y2026) },
  { id: "cwip", sl: "", label: "Capital work in progress", kind: "sub", y2025: L(BS.cwip.y2025), y2026: L(BS.cwip.y2026) },
  { id: "dep", sl: "36", label: "Depreciation to date (shown separately)", kind: "line", y2025: 0, y2026: 0, note: "FS presents written-down values" },
);

assetLines.push(withTotal(assetLines, "net-block", "37", "Net block (PPE + intangible + CWIP)", ["ppe", "intang", "cwip"]));

assetLines.push(
  { id: "onc-head", label: "Other non-current assets", kind: "head", y2025: null, y2026: null },
  { id: "invest", sl: "38(i)", label: "Investments (subsidiaries / others)", kind: "line", y2025: L(BS.ncInvest.y2025), y2026: L(BS.ncInvest.y2026), note: "Note 12" },
  { id: "sec-dep", sl: "38(iv)(b)", label: "Security deposits", kind: "line", y2025: L(122.07), y2026: L(150.95), note: "Note 13 non-current" },
  { id: "nc-tax", sl: "38(iv)(c)", label: "Taxes paid under dispute", kind: "sub", y2025: L(35.5), y2026: L(35.5) },
  { id: "nc-fd", sl: "", label: "Non-current bank deposits (margin)", kind: "line", y2025: L(418.43), y2026: L(1102.43), note: "Note 14 / 17" },
  { id: "insurance", sl: "40", label: "Insurance receivables", kind: "line", y2025: L(4241.84), y2026: L(2956.31), note: "Note 14" },
);

assetLines.push(withTotal(assetLines, "onc-total", "41", "Total other non-current assets", ["invest", "sec-dep", "nc-tax", "nc-fd", "insurance"]));
assetLines.push(withTotal(assetLines, "assets-total", "43", "Total assets (34 + 37 + 41)", ["ca-total", "net-block", "onc-total"]));

const liabLines: CmaLine[] = [
  { id: "cl-head", label: "Current liabilities", kind: "head", y2025: null, y2026: null },
  { id: "wc", sl: "1", label: "Short-term borrowings from banks (working capital)", kind: "line", y2025: L(10509.13), y2026: L(10500.74), note: "Note 8" },
  { id: "edfs-lib", sl: "", label: "Channel finance (eDFS) limit", kind: "sub", y2025: L(3003.44), y2026: L(3037.72) },
  { id: "creditors", sl: "2", label: "Sundry creditors (trade)", kind: "line", y2025: L(BS.tradePay.y2025), y2026: L(BS.tradePay.y2026), note: "Note 9 incl. MSME, others, bills of exchange" },
  { id: "adv-cust", sl: "4", label: "Advance payments from customers", kind: "line", y2025: L(109.67), y2026: L(30.45), note: "Note 10" },
  { id: "capex-crs", sl: "", label: "Creditors for capital expenditure", kind: "sub", y2025: L(2.19), y2026: L(1.97) },
  { id: "prov-tax", sl: "5", label: "Provision for taxes", kind: "line", y2025: L(31.56), y2026: L(188.21), note: "Note 7 current" },
  { id: "curr-tl", sl: "8", label: "Current maturities of term loans", kind: "line", y2025: L(1938.06), y2026: L(1528.63), note: "Note 10 / 5" },
  { id: "stat", sl: "9", label: "Other statutory liabilities", kind: "line", y2025: L(64.86), y2026: L(15.49) },
  { id: "ocl-other", sl: "", label: "Other current liabilities", kind: "sub", y2025: L(11.36 + 24.05), y2026: L(11.36 + 172.58) },
  { id: "emp-prov", sl: "", label: "Provision for employee benefits (current)", kind: "sub", y2025: L(6.67 + 24.4), y2026: L(6.67 + 24.4) },
];

const clIds = ["wc", "edfs-lib", "creditors", "adv-cust", "capex-crs", "prov-tax", "curr-tl", "stat", "ocl-other", "emp-prov"];
liabLines.push(withTotal(liabLines, "cl-total", "", "Total current liabilities", clIds));

liabLines.push(
  { id: "tl-head", label: "Term liabilities", kind: "head", y2025: null, y2026: null },
  { id: "tl-banks", sl: "13", label: "Term loans from banks (excluding current instalment)", kind: "line", y2025: L(3512.18), y2026: L(1941.31), note: "Note 5" },
  { id: "tl-veh", sl: "", label: "Vehicle / equipment loans (non-current)", kind: "sub", y2025: L(74.02), y2026: L(55.14) },
  { id: "tl-unsec", sl: "17", label: "Unsecured loans (long-term)", kind: "line", y2025: L(1012.02), y2026: L(1012.02) },
  { id: "dtl", sl: "15", label: "Deferred tax liability", kind: "line", y2025: L(BS.dtl.y2025), y2026: L(BS.dtl.y2026), note: "Note 6" },
  { id: "lt-prov", sl: "", label: "Long-term provisions", kind: "line", y2025: L(BS.ltProv.y2025), y2026: L(BS.ltProv.y2026), note: "Note 7 non-current" },
);

const tlIds = ["tl-banks", "tl-veh", "tl-unsec", "dtl", "lt-prov"];
liabLines.push(withTotal(liabLines, "tl-total", "18", "Total term liabilities", tlIds));
liabLines.push(withTotal(liabLines, "tol", "19", "Total outside liabilities", ["cl-total", "tl-total"]));

liabLines.push(
  { id: "nw-head", label: "Net worth", kind: "head", y2025: null, y2026: null },
  { id: "equity", sl: "20", label: "Ordinary share capital", kind: "line", y2025: L(BS.share.y2025), y2026: L(BS.share.y2026), note: "Note 3" },
  { id: "reserves", sl: "23–26", label: "Reserves and surplus", kind: "line", y2025: L(BS.reserves.y2025), y2026: L(BS.reserves.y2026), note: "Note 4 (share premium + surplus combined; FS face)" },
);

liabLines.push(withTotal(liabLines, "nw", "27", "Net worth", ["equity", "reserves"]));
liabLines.push(withTotal(liabLines, "liab-total", "28", "Total liabilities", ["tol", "nw"]));

const opLines: CmaLine[] = [
  { id: "sales-head", sl: "1", label: "Gross sales", kind: "head", y2025: null, y2026: null },
  { id: "dom", sl: "1(i)", label: "Domestic sales", kind: "line", y2025: L(23783.66), y2026: L(27435.78), note: "Note 18" },
  { id: "exp", sl: "1(ii)", label: "Export sales", kind: "line", y2025: L(33324.69), y2026: L(27875.66) },
  { id: "oth-op", sl: "1(iii)", label: "Other operational income (job work + other operating)", kind: "line", y2025: L(272.5 + 1553.45), y2026: L(1342.75 + 1711.43) },
];

opLines.push(withTotal(opLines, "gross", "", "Total revenue from operations", ["dom", "exp", "oth-op"]));
opLines.push({ id: "excise", sl: "2", label: "Less: excise duty", kind: "line", y2025: 0, y2026: 0 });
opLines.push({
  id: "net-sales",
  sl: "3",
  label: "Net sales",
  kind: "total",
  y2025: sum(opLines, ["gross"], "y2025"),
  y2026: sum(opLines, ["gross"], "y2026"),
});

const yoy = (curr: number, prev: number) => (prev === 0 ? 0 : ((curr - prev) / prev) * 100);
opLines.push({
  id: "yoy",
  sl: "4",
  label: "% rise / (fall) in net sales vs previous year",
  kind: "line",
  y2025: null,
  y2026: yoy(sum(opLines, ["gross"], "y2026"), sum(opLines, ["gross"], "y2025")),
});

opLines.push(
  { id: "cos-head", sl: "5", label: "Cost of sales", kind: "head", y2025: null, y2026: null },
  { id: "rm", sl: "5(i)", label: "Raw materials consumed (incl. packing)", kind: "line", y2025: L(26077.7), y2026: L(34657.04), note: "Note 20" },
  { id: "traded", sl: "", label: "Purchase of traded goods", kind: "sub", y2025: L(19492.43), y2026: L(7856.12), note: "Note 21" },
  { id: "labour", sl: "5(iv)", label: "Employee benefits / direct labour", kind: "line", y2025: L(2150.25), y2026: L(2362.96), note: "Note 23 — FS does not split factory vs admin" },
  { id: "dep-pl", sl: "5(vi)", label: "Depreciation", kind: "line", y2025: L(928), y2026: L(835), note: "Note 11" },
  { id: "other-exp", sl: "5(v)+6", label: "Other expenses (power, stores, operating, SG&A)", kind: "line", y2025: L(7728.95), y2026: L(8007.94), note: "Note 25 — not split in the source dump" },
  { id: "open-wip", sl: "5(viii)", label: "Add: opening stock-in-process", kind: "line", y2025: 13.14, y2026: L(841.9), note: "2025 opening from CMA FY24 close (13.14 Cr)" },
  { id: "close-wip", sl: "5(ix)", label: "Less: closing stock-in-process", kind: "line", y2025: L(841.9), y2026: L(1946.39) },
  { id: "open-fg", sl: "5(xi)", label: "Add: opening finished goods", kind: "line", y2025: 28.31, y2026: L(4798.85), note: "2025 opening from CMA FY24 close (28.31 Cr)" },
  { id: "close-fg", sl: "5(xii)", label: "Less: closing finished goods", kind: "line", y2025: L(4798.85), y2026: L(3327.93) },
);

const cos2025 =
  L(26077.7 + 19492.43 + 2150.25 + 928 + 7728.95) + 13.14 - L(841.9) + 28.31 - L(4798.85);
const cos2026 =
  L(34657.04 + 7856.12 + 2362.96 + 835 + 8007.94) + L(841.9) - L(1946.39) + L(4798.85) - L(3327.93);

opLines.push({ id: "cos", sl: "5(xiii)", label: "Total cost of sales (mapped)", kind: "total", y2025: cos2025, y2026: cos2026 });

const net2025 = sum(opLines, ["net-sales"], "y2025");
const net2026 = sum(opLines, ["net-sales"], "y2026");
opLines.push({
  id: "op-ebit",
  sl: "8",
  label: "Operating profit before interest",
  kind: "total",
  y2025: net2025 - cos2025,
  y2026: net2026 - cos2026,
});
opLines.push(
  { id: "finance", sl: "9", label: "Finance costs (working capital + term loan)", kind: "line", y2025: L(2482.92), y2026: L(2499.32), note: "Note 24 — FS does not split WC vs term" },
);
opLines.push({
  id: "op-after-int",
  sl: "10",
  label: "Operating profit after interest",
  kind: "total",
  y2025: net2025 - cos2025 - L(2482.92),
  y2026: net2026 - cos2026 - L(2499.32),
});
opLines.push(
  { id: "other-inc", sl: "11(i)", label: "Other non-operating income", kind: "line", y2025: L(86.42), y2026: L(100.3), note: "Note 19" },
  { id: "except", sl: "11(ii)", label: "Less: exceptional items (insurance claim prior year + past service cost)", kind: "line", y2025: L(1285.54), y2026: L(1285.54 + 208.6), note: "P&L exceptional — 2025 insurance line is as printed in the FS" },
);
opLines.push({
  id: "pbt",
  sl: "12",
  label: "Profit before tax",
  kind: "total",
  y2025: net2025 - cos2025 - L(2482.92) + L(86.42) - L(1285.54),
  y2026: net2026 - cos2026 - L(2499.32) + L(100.3) - L(1285.54 + 208.6),
});
opLines.push({ id: "tax", sl: "13", label: "Provision for taxes", kind: "line", y2025: L(93.12), y2026: L(188.4), note: "Current + deferred" });
opLines.push({
  id: "pat",
  sl: "15",
  label: "Net profit for the year",
  kind: "total",
  y2025: L(276.8),
  y2026: L(198.7),
  note: "P&L face (used instead of the reconstructed PBT minus tax, which is sensitive to exceptional-line printing)",
});

export const cmaAssets = assetLines;
export const cmaLiabilities = liabLines;
export const cmaOperating = opLines;

export function formatCma(value: number | null): string {
  if (value == null) return "—";
  const abs = Math.abs(value);
  const body = abs.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return value < 0 ? `(${body})` : body;
}

export function faceTotals() {
  return {
    assets: { y2025: L(40192.1), y2026: L(39192.31) },
    liabilities: { y2025: L(40192.1), y2026: L(39192.31) },
    netWorth: { y2025: L(18309.37), y2026: L(18508.08) },
    pat: { y2025: L(276.8), y2026: L(198.7) },
  };
}

function xmlEscape(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function xlsxCell(col: string, row: number, value: string | number | null, style?: number): string {
  const ref = `${col}${row}`;
  const s = style != null ? ` s="${style}"` : "";
  if (value == null || value === "") return `<c r="${ref}"${s}/>`;
  if (typeof value === "number") return `<c r="${ref}"${s}><v>${value}</v></c>`;
  return `<c r="${ref}"${s} t="inlineStr"><is><t>${xmlEscape(value)}</t></is></c>`;
}

function xlsxSheetXml(title: string, rows: CmaLine[]): string {
  const lines: string[] = [];
  let r = 1;
  lines.push(`<row r="${r}">${xlsxCell("A", r, title, 1)}</row>`);
  r += 1;
  lines.push(`<row r="${r}">${xlsxCell("A", r, `${cmaMeta.company} · ${cmaMeta.unit}`)}</row>`);
  r += 2;
  lines.push(
    `<row r="${r}">${xlsxCell("A", r, "Sl.", 2)}${xlsxCell("B", r, "Particulars", 2)}${xlsxCell("C", r, "2025 Aud", 2)}${xlsxCell("D", r, "2026 Aud", 2)}</row>`,
  );
  r += 1;
  for (const row of rows) {
    const style = row.kind === "total" ? 3 : row.kind === "head" ? 4 : undefined;
    const numStyle = row.kind === "total" ? 5 : 6;
    lines.push(
      `<row r="${r}">${xlsxCell("A", r, row.sl ?? "", style)}${xlsxCell("B", r, row.label, style)}${xlsxCell("C", r, row.y2025, row.y2025 == null ? style : numStyle)}${xlsxCell("D", r, row.y2026, row.y2026 == null ? style : numStyle)}</row>`,
    );
    r += 1;
  }
  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheetData>${lines.join("")}</sheetData>
</worksheet>`;
}

function crc32(data: Uint8Array): number {
  let crc = 0xffffffff;
  for (const byte of data) {
    crc ^= byte;
    for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function zipStore(files: { name: string; content: string }[]): Blob {
  const encoder = new TextEncoder();
  const locals: Uint8Array[] = [];
  const centrals: Uint8Array[] = [];
  let offset = 0;
  for (const file of files) {
    const data = encoder.encode(file.content);
    const name = encoder.encode(file.name);
    const crc = crc32(data);
    const local = new Uint8Array(30 + name.length + data.length);
    const lv = new DataView(local.buffer);
    lv.setUint32(0, 0x04034b50, true);
    lv.setUint16(4, 20, true);
    lv.setUint16(8, 0, true);
    lv.setUint16(10, 0, true);
    lv.setUint16(12, 0, true);
    lv.setUint32(14, crc, true);
    lv.setUint32(18, data.length, true);
    lv.setUint32(22, data.length, true);
    lv.setUint16(26, name.length, true);
    local.set(name, 30);
    local.set(data, 30 + name.length);
    locals.push(local);
    const central = new Uint8Array(46 + name.length);
    const cv = new DataView(central.buffer);
    cv.setUint32(0, 0x02014b50, true);
    cv.setUint16(4, 20, true);
    cv.setUint16(6, 20, true);
    cv.setUint32(16, crc, true);
    cv.setUint32(20, data.length, true);
    cv.setUint32(24, data.length, true);
    cv.setUint16(28, name.length, true);
    cv.setUint32(42, offset, true);
    central.set(name, 46);
    centrals.push(central);
    offset += local.length;
  }
  const centralSize = centrals.reduce((n, p) => n + p.length, 0);
  const end = new Uint8Array(22);
  const ev = new DataView(end.buffer);
  ev.setUint32(0, 0x06054b50, true);
  ev.setUint16(8, files.length, true);
  ev.setUint16(10, files.length, true);
  ev.setUint32(12, centralSize, true);
  ev.setUint32(16, offset, true);
  return new Blob([...locals, ...centrals, end], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
}

export function downloadCmaExcel(): string {
  const sheets = [
    { path: "xl/worksheets/sheet1.xml", title: "Form II — Operating Statement", rows: cmaOperating, name: "Form II Operating" },
    { path: "xl/worksheets/sheet2.xml", title: "Form III — Assets", rows: cmaAssets, name: "Form III Assets" },
    { path: "xl/worksheets/sheet3.xml", title: "Form III — Liabilities", rows: cmaLiabilities, name: "Form III Liabilities" },
  ];
  const styles = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="4">
    <font><sz val="11"/><name val="Calibri"/></font>
    <font><b/><sz val="14"/><color rgb="FF0B3A5B"/><name val="Calibri"/></font>
    <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
    <font><b/><sz val="11"/><name val="Calibri"/></font>
  </fonts>
  <fills count="5">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FF0B3A5B"/></patternFill></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FFF5D76E"/></patternFill></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FFE2E8F0"/></patternFill></fill>
  </fills>
  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
  <cellStyleXfs count="1"><xf/></cellStyleXfs>
  <cellXfs count="7">
    <xf xfId="0"/>
    <xf xfId="0" fontId="1"/>
    <xf xfId="0" fontId="2" fillId="2" applyFont="1" applyFill="1"/>
    <xf xfId="0" fontId="3" fillId="3" applyFont="1" applyFill="1"/>
    <xf xfId="0" fontId="3" fillId="4" applyFont="1" applyFill="1"/>
    <xf numFmtId="4" fontId="3" fillId="3" applyNumberFormat="1" applyFont="1" applyFill="1"/>
    <xf numFmtId="4" applyNumberFormat="1"/>
  </cellXfs>
</styleSheet>`;
  const workbook = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    ${sheets.map((s, i) => `<sheet name="${xmlEscape(s.name)}" sheetId="${i + 1}" r:id="rId${i + 1}"/>`).join("")}
  </sheets>
</workbook>`;
  const workbookRels = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
  <Relationship Id="rId4" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>`;
  const rels = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>`;
  const types = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>`;
  const blob = zipStore([
    { name: "[Content_Types].xml", content: types },
    { name: "_rels/.rels", content: rels },
    { name: "xl/workbook.xml", content: workbook },
    { name: "xl/_rels/workbook.xml.rels", content: workbookRels },
    { name: "xl/styles.xml", content: styles },
    ...sheets.map((s) => ({ name: s.path, content: xlsxSheetXml(s.title, s.rows) })),
  ]);
  const fileName = "PIL-CMA-FY26.xlsx";
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}
