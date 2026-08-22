import { getApiBaseUrl, getApiUrl } from "@/lib/api-config";

export type DailyProductionCurrencyTotal = {
  currency: string;
  salesValue: number;
  pcs: number;
  kgs: number;
};

export type DailyProductionLineRow = {
  lineNo: string;
  pcs: number;
  kgs: number;
  pricedRows: number;
  unpricedRows: number;
  currencies: DailyProductionCurrencyTotal[];
};

export type DailyProductionBandRow = {
  band: string;
  sortOrder: number;
  pcs: number;
  kgs: number;
  currencies: DailyProductionCurrencyTotal[];
};

export type DailyProductionIcoRow = {
  ico: string;
  consignee: string;
  poNo: string;
  bagType: string;
  size: string;
  lineNos: string;
  pcs: number;
  kgs: number;
  weightPerPc: number;
  salesPrice: number | null;
  currency: string;
  salesValue: number | null;
  valuePerKg: number | null;
  priced: boolean;
};

export type DailyProductionUnpricedRow = {
  ico: string;
  poNo: string;
  consignee: string;
  pcs: number;
};

export type DailyProductionSummary = {
  productionRows: number;
  uniqueIcoCount: number;
  pricedIcoCount: number;
  pricedRowCount: number;
  unpricedRowCount: number;
  totalPcs: number;
  totalKgs: number;
  currencyTotals: DailyProductionCurrencyTotal[];
  byLine: DailyProductionLineRow[];
  byWeightBand: DailyProductionBandRow[];
  byIco: DailyProductionIcoRow[];
  unpriced: DailyProductionUnpricedRow[];
  hints: string[];
};

export type DailyProductionProcessResponse = {
  downloadToken: string;
  fileName: string;
  sheetName: string;
  summary: DailyProductionSummary;
};

function apiUrl(path: string): string {
  const configured = getApiBaseUrl();
  if (configured) {
    const cleanPath = path.startsWith("/") ? path : `/${path}`;
    return `${configured}${cleanPath}`;
  }
  if (typeof window !== "undefined") {
    const host = window.location.hostname;
    if (host === "localhost" || host === "127.0.0.1") {
      const cleanPath = path.startsWith("/") ? path : `/${path}`;
      return `http://localhost:5115${cleanPath}`;
    }
  }
  return getApiUrl(path);
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

function nullableNum(v: unknown): number | null {
  if (v == null || v === "") return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

function mapCurrency(raw: unknown): DailyProductionCurrencyTotal {
  const row = (raw ?? {}) as Record<string, unknown>;
  return {
    currency: str(row.currency ?? row.Currency),
    salesValue: num(row.salesValue ?? row.SalesValue),
    pcs: num(row.pcs ?? row.Pcs),
    kgs: num(row.kgs ?? row.Kgs),
  };
}

function mapSummary(raw: unknown): DailyProductionSummary {
  const s = (raw ?? {}) as Record<string, unknown>;
  const byLine = (s.byLine ?? s.ByLine ?? []) as unknown[];
  const byBand = (s.byWeightBand ?? s.ByWeightBand ?? []) as unknown[];
  const byIco = (s.byIco ?? s.ByIco ?? []) as unknown[];
  const unpriced = (s.unpriced ?? s.Unpriced ?? []) as unknown[];
  const hints = (s.hints ?? s.Hints ?? []) as unknown[];
  return {
    productionRows: num(s.productionRows ?? s.ProductionRows),
    uniqueIcoCount: num(s.uniqueIcoCount ?? s.UniqueIcoCount),
    pricedIcoCount: num(s.pricedIcoCount ?? s.PricedIcoCount),
    pricedRowCount: num(s.pricedRowCount ?? s.PricedRowCount),
    unpricedRowCount: num(s.unpricedRowCount ?? s.UnpricedRowCount),
    totalPcs: num(s.totalPcs ?? s.TotalPcs),
    totalKgs: num(s.totalKgs ?? s.TotalKgs),
    currencyTotals: ((s.currencyTotals ?? s.CurrencyTotals ?? []) as unknown[]).map(mapCurrency),
    byLine: byLine.map((item) => {
      const row = item as Record<string, unknown>;
      return {
        lineNo: str(row.lineNo ?? row.LineNo),
        pcs: num(row.pcs ?? row.Pcs),
        kgs: num(row.kgs ?? row.Kgs),
        pricedRows: num(row.pricedRows ?? row.PricedRows),
        unpricedRows: num(row.unpricedRows ?? row.UnpricedRows),
        currencies: ((row.currencies ?? row.Currencies ?? []) as unknown[]).map(mapCurrency),
      };
    }),
    byWeightBand: byBand.map((item) => {
      const row = item as Record<string, unknown>;
      return {
        band: str(row.band ?? row.Band),
        sortOrder: num(row.sortOrder ?? row.SortOrder),
        pcs: num(row.pcs ?? row.Pcs),
        kgs: num(row.kgs ?? row.Kgs),
        currencies: ((row.currencies ?? row.Currencies ?? []) as unknown[]).map(mapCurrency),
      };
    }),
    byIco: byIco.map((item) => {
      const row = item as Record<string, unknown>;
      return {
        ico: str(row.ico ?? row.Ico),
        consignee: str(row.consignee ?? row.Consignee),
        poNo: str(row.poNo ?? row.PoNo),
        bagType: str(row.bagType ?? row.BagType),
        size: str(row.size ?? row.Size),
        lineNos: str(row.lineNos ?? row.LineNos),
        pcs: num(row.pcs ?? row.Pcs),
        kgs: num(row.kgs ?? row.Kgs),
        weightPerPc: num(row.weightPerPc ?? row.WeightPerPc),
        salesPrice: nullableNum(row.salesPrice ?? row.SalesPrice),
        currency: str(row.currency ?? row.Currency),
        salesValue: nullableNum(row.salesValue ?? row.SalesValue),
        valuePerKg: nullableNum(row.valuePerKg ?? row.ValuePerKg),
        priced: Boolean(row.priced ?? row.Priced),
      };
    }),
    unpriced: unpriced.map((item) => {
      const row = item as Record<string, unknown>;
      return {
        ico: str(row.ico ?? row.Ico),
        poNo: str(row.poNo ?? row.PoNo),
        consignee: str(row.consignee ?? row.Consignee),
        pcs: num(row.pcs ?? row.Pcs),
      };
    }),
    hints: hints.map((h) => str(h)).filter(Boolean),
  };
}

export async function processDailyProductionExcel(file: File): Promise<DailyProductionProcessResponse> {
  const body = new FormData();
  body.append("file", file);
  const res = await fetch(apiUrl("/api/daily-production/process"), {
    method: "POST",
    body,
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    throw new Error(str(data.message) || "Failed to process the production Excel.");
  }
  return {
    downloadToken: str(data.downloadToken ?? data.DownloadToken),
    fileName: str(data.fileName ?? data.FileName) || "daily-production.xlsx",
    sheetName: str(data.sheetName ?? data.SheetName),
    summary: mapSummary(data.summary ?? data.Summary),
  };
}

export function dailyProductionDownloadUrl(token: string): string {
  return apiUrl(`/api/daily-production/download/${encodeURIComponent(token)}`);
}

export async function downloadDailyProductionExcel(token: string, fileName: string): Promise<void> {
  const res = await fetch(dailyProductionDownloadUrl(token));
  if (!res.ok) {
    const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
    throw new Error(str(data.message) || "Download expired. Process the file again.");
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName || "daily-production.xlsx";
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function formatQty(n: number, digits = 0): string {
  return n.toLocaleString("en-IN", { maximumFractionDigits: digits, minimumFractionDigits: 0 });
}

export function formatMoney(n: number | null | undefined, digits = 2): string {
  if (n == null || !Number.isFinite(n)) return "—";
  return n.toLocaleString("en-IN", { minimumFractionDigits: digits, maximumFractionDigits: digits });
}
