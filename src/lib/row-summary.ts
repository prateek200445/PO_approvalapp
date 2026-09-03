const DIMENSION_HINTS =
  /country|nation|buyer|customer|client|party|vendor|firm|supplier|ledger|item|product|material|department|particulars|dept|group|under|subgroup|representative|salesman|state|city|region|buyername|partyname|firmname|ledgername|itemname|productgroup|countryname/i;

const MEASURE_HINTS =
  /amount|billamount|total|qty|quantity|stkinhand|stock|debit|credit|balance|pending|outstanding|opening|closing|production|wastage|value|netamount|net|billamt|sum/i;

const SKIP_COLUMNS =
  /^(section|error|note|password|rownum|rn)$/i;

function humanizeColumn(col: string): string {
  return col
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .trim();
}

function formatSummaryValue(col: string, value: number): string {
  const lower = col.toLowerCase();
  const isQty =
    /qty|quantity|stk|stock|pcs|count|weight|wt|metre|meter|kg|ton|gsm|gpm|roll/i.test(
      lower,
    );
  const isMoney =
    !isQty &&
    (/amount|bill|debit|credit|balance|outstanding|opening|closing|value|price|inr/i.test(
      lower,
    ) ||
      (/net/i.test(lower) && /amount|amt|value/i.test(lower)) ||
      (/\bpending/i.test(lower) && !/pendingqty|pending qty|pendingpo/i.test(lower)));
  if (isMoney) {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
      maximumFractionDigits: 2,
    }).format(value);
  }
  const formatted = new Intl.NumberFormat("en-IN", { maximumFractionDigits: 2 }).format(
    value,
  );
  if (/wt|weight|kg/i.test(lower)) return `${formatted} kg`;
  if (/metre|meter/i.test(lower)) return `${formatted} m`;
  return formatted;
}

function rowVal(row: Record<string, unknown>, col: string): unknown {
  if (col in row) return row[col];
  const key = Object.keys(row).find((k) => k.toLowerCase() === col.toLowerCase());
  return key ? row[key] : undefined;
}

function rowString(row: Record<string, unknown>, col: string): string {
  const v = rowVal(row, col);
  if (v == null) return "";
  return String(v).trim();
}

function rowNumber(row: Record<string, unknown>, col: string): number | null {
  const v = rowVal(row, col);
  if (v == null || v === "") return null;
  const n = typeof v === "number" ? v : Number(v);
  return Number.isFinite(n) ? n : null;
}

function scoreDimensionColumn(
  col: string,
  rows: Record<string, unknown>[],
): number {
  if (SKIP_COLUMNS.test(col)) return -100;
  const lower = col.toLowerCase();
  let score = 0;
  if (DIMENSION_HINTS.test(lower)) score += 12;
  if (/name$/i.test(col) || /name/i.test(col)) score += 4;
  if (/company/i.test(lower)) score += 3;
  if (/id$|srno|sysdate|date|time|email|phone|gst|pan|utr|invno|pono|mrno|voucherno/i.test(lower))
    score -= 4;

  const values = rows.map((r) => rowString(r, col)).filter(Boolean);
  if (values.length === 0) return -100;
  const distinct = new Set(values.map((v) => v.toLowerCase())).size;
  if (distinct <= 1) return -50;
  if (distinct === rows.length && rows.length > 8) score -= 8;
  if (distinct >= 2 && distinct <= Math.max(rows.length, 2)) score += 6;
  if (distinct >= 2 && distinct <= 25) score += 4;
  return score;
}

function scoreMeasureColumn(
  col: string,
  rows: Record<string, unknown>[],
): number {
  if (SKIP_COLUMNS.test(col)) return -100;
  const lower = col.toLowerCase();
  if (/pct|percent|ratio|rate|%/i.test(lower)) return -20;
  if (/^(expense|sales)?year$|^month$|^expensemonth$|^salesmonth$|^day$|^week$|^quarter$/i.test(lower))
    return -40;
  if (/\byear\b|\bmonth\b|\bday\b|\bweek\b|\bquarter\b|\bfy\b/i.test(lower) && !/amount|qty|count|total/i.test(lower))
    return -30;

  let score = 0;
  if (MEASURE_HINTS.test(lower)) score += 12;
  if (/^debitbalance$|^creditbalance$|^effectivebalance$/i.test(lower)) score += 25;
  if (
    /^pendingbalance$/i.test(lower)
    && rows.every((r) => Math.abs(rowNumber(r, col) ?? 0) < 0.01)
    && rows.some((r) =>
      Object.keys(r).some((k) => /^(debitbalance|creditbalance|effectivebalance)$/i.test(k)),
    )
  )
    score -= 30;
  if (/count|cnt/i.test(lower) && !/country/i.test(lower)) score += 6;

  const nums = rows.map((r) => rowNumber(r, col)).filter((n): n is number => n != null);
  if (nums.length < Math.ceil(rows.length * 0.4)) return -50;
  const sum = nums.reduce((a, b) => a + b, 0);
  if (sum === 0) score -= 10;
  else score += 5;
  return score;
}

function pickBestColumn(
  rows: Record<string, unknown>[],
  scorer: (col: string, rows: Record<string, unknown>[]) => number,
): string | null {
  const cols = Object.keys(rows[0] ?? {}).filter((c) => !SKIP_COLUMNS.test(c));
  let best: { col: string; score: number } | null = null;
  for (const col of cols) {
    const score = scorer(col, rows);
    if (!best || score > best.score) best = { col, score };
  }
  return best && best.score > 0 ? best.col : null;
}

function pickMeasureColumns(rows: Record<string, unknown>[]): string[] {
  const cols = Object.keys(rows[0] ?? {}).filter((c) => !SKIP_COLUMNS.test(c));
  return cols
    .map((col) => ({ col, score: scoreMeasureColumn(col, rows) }))
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score)
    .slice(0, 3)
    .map((x) => x.col);
}

/** Context-aware one-line summary for any multi-row result shape. */
export function buildSmartRowSummary(
  rows: Record<string, unknown>[],
): string | null {
  if (rows.length === 0) return null;

  const dimensionCol = pickBestColumn(rows, scoreDimensionColumn);
  const measureCol = pickBestColumn(rows, scoreMeasureColumn);

  if (dimensionCol && measureCol) {
    const grouped = new Map<string, number>();
    for (const row of rows) {
      const label = rowString(row, dimensionCol) || "Unknown";
      const val = rowNumber(row, measureCol) ?? 0;
      grouped.set(label, (grouped.get(label) ?? 0) + val);
    }
    const sorted = [...grouped.entries()].sort((a, b) => b[1] - a[1]);
    const grandTotal = sorted.reduce((s, [, v]) => s + v, 0);
    const top = sorted.slice(0, 5);
    const dimLabel = humanizeColumn(dimensionCol).toLowerCase();
    const measureLabel = humanizeColumn(measureCol).toLowerCase();

    const topPart = top
      .map(([label, val]) => `${label} ${formatSummaryValue(measureCol, val)}`)
      .join(" · ");
    const more =
      sorted.length > 5 ? ` (+${sorted.length - 5} more ${dimLabel})` : "";
    const totalPart =
      grandTotal !== 0
        ? `${formatSummaryValue(measureCol, grandTotal)} total ${measureLabel}`
        : `${rows.length} rows`;

    return `${rows.length} rows · ${totalPart} · Top ${Math.min(5, sorted.length)} ${dimLabel}: ${topPart}${more}`;
  }

  if (dimensionCol) {
    const distinct = [
      ...new Set(rows.map((r) => rowString(r, dimensionCol)).filter(Boolean)),
    ];
    if (distinct.length > 1) {
      const dimLabel = humanizeColumn(dimensionCol).toLowerCase();
      const top = distinct.slice(0, 5);
      const more = distinct.length > 5 ? ` (+${distinct.length - 5} more)` : "";
      return `${distinct.length} ${dimLabel}${distinct.length === 1 ? "" : "s"} · ${top.join(" · ")}${more}`;
    }
  }

  const measures = pickMeasureColumns(rows);
  if (measures.length > 0) {
    const parts = measures.map((col) => {
      const sum = rows.reduce((s, r) => s + (rowNumber(r, col) ?? 0), 0);
      return `${formatSummaryValue(col, sum)} ${humanizeColumn(col).toLowerCase()}`;
    });
    return `${rows.length} rows · ${parts.join(" · ")}`;
  }

  if (rows.length > 1) {
    return `${rows.length} matching records — see table for details.`;
  }

  return null;
}
