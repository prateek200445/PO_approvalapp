import { getApiUrl } from "@/lib/api-config";

export type ItemStockSummaryLine = {
  lineType: string;
  movementType: string;
  inwardQty: number;
  outwardQty: number;
  balance: number;
};

export type ItemStockTxnLine = {
  txnDate: string;
  movementType: string;
  docNo: string;
  inwardQty: number;
  outwardQty: number;
  balance: number;
};

export type ItemRollLine = {
  godown: string;
  rollNo: string;
  itemName: string;
  netWt: number;
  producedOn: string | null;
};

export type ItemStockResult = {
  companyName: string;
  itemCode: string;
  itemName: string;
  dateFrom: string;
  dateTo: string;
  summary: ItemStockSummaryLine[];
  transactions: ItemStockTxnLine[];
  rolls: ItemRollLine[];
  rollCount: number;
  rollNetWt: number;
  rollNote: string | null;
};

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function str(v: unknown): string {
  return v == null ? "" : String(v);
}

export function formatQty(value: number): string {
  return value.toLocaleString("en-IN", { minimumFractionDigits: 3, maximumFractionDigits: 3 });
}

export function formatIsoDate(value: string | null | undefined): string {
  if (!value) return "";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value.slice(0, 10);
  return d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export async function getItemStockCompanies(): Promise<string[]> {
  const response = await fetch(getApiUrl("/api/item-stock/companies"));
  const payload = (await response.json()) as unknown;
  if (!response.ok) {
    const message = payload && typeof payload === "object" && "message" in payload ? String((payload as { message?: string }).message) : "";
    throw new Error(message || "Failed to load companies");
  }
  return Array.isArray(payload) ? payload.map((n) => str(n).trim()).filter(Boolean) : [];
}

export async function queryItemStock(
  filters: {
    companyName: string;
    itemCode: string;
    dateFrom: string;
    dateTo: string;
  },
  signal?: AbortSignal,
): Promise<ItemStockResult> {
  const response = await fetch(getApiUrl("/api/item-stock/query"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    signal,
    body: JSON.stringify({
      companyName: filters.companyName,
      itemCode: filters.itemCode.trim(),
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
    }),
  });
  const payload = (await response.json()) as Record<string, unknown>;
  if (!response.ok) throw new Error(str(payload.message) || "Item stock query failed");

  const summaryRaw = (payload.summary ?? payload.Summary ?? []) as Array<Record<string, unknown>>;
  const txnRaw = (payload.transactions ?? payload.Transactions ?? []) as Array<Record<string, unknown>>;
  const rollRaw = (payload.rolls ?? payload.Rolls ?? []) as Array<Record<string, unknown>>;

  return {
    companyName: str(payload.companyName ?? payload.CompanyName),
    itemCode: str(payload.itemCode ?? payload.ItemCode),
    itemName: str(payload.itemName ?? payload.ItemName),
    dateFrom: str(payload.dateFrom ?? payload.DateFrom),
    dateTo: str(payload.dateTo ?? payload.DateTo),
    summary: summaryRaw.map((row) => ({
      lineType: str(row.lineType ?? row.LineType),
      movementType: str(row.movementType ?? row.MovementType),
      inwardQty: num(row.inwardQty ?? row.InwardQty),
      outwardQty: num(row.outwardQty ?? row.OutwardQty),
      balance: num(row.balance ?? row.Balance),
    })),
    transactions: txnRaw.map((row) => ({
      txnDate: str(row.txnDate ?? row.TxnDate),
      movementType: str(row.movementType ?? row.MovementType),
      docNo: str(row.docNo ?? row.DocNo),
      inwardQty: num(row.inwardQty ?? row.InwardQty),
      outwardQty: num(row.outwardQty ?? row.OutwardQty),
      balance: num(row.balance ?? row.Balance),
    })),
    rolls: rollRaw.map((row) => ({
      godown: str(row.godown ?? row.Godown),
      rollNo: str(row.rollNo ?? row.RollNo),
      itemName: str(row.itemName ?? row.ItemName),
      netWt: num(row.netWt ?? row.NetWt),
      producedOn: str(row.producedOn ?? row.ProducedOn) || null,
    })),
    rollCount: num(payload.rollCount ?? payload.RollCount),
    rollNetWt: num(payload.rollNetWt ?? payload.RollNetWt),
    rollNote: str(payload.rollNote ?? payload.RollNote) || null,
  };
}
