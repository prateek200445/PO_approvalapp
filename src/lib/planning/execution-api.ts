import { getApiUrl } from "@/lib/api-config";
import { planningOrderUrl } from "@/lib/planning/planning-order-url";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) throw new Error((data as { message?: string }).message || res.statusText);
  return data as T;
}

export type OrderExecutionSummary = {
  orderNo: string;
  companyName: string;
  plannedQty: number;
  producedQty: number;
  bailedQty: number;
  pendingQty: number;
  bailingGap: number;
  lines: Array<{
    lineNo: number;
    shift: string;
    planDate: string | null;
    plannedQty: number;
    producedQty: number;
    openBacklogQty: number;
  }>;
  lineShiftTotals: Array<{
    lineNo: number;
    shift: string;
    plannedQty: number;
    producedQty: number;
    pendingQty: number;
  }>;
  productionEntries: Array<{
    lineNo: number;
    teamNo: string;
    shift: string;
    prodDate: string;
    quantity: number;
  }>;
  replanSuggestions: string[];
  backlogRowsAutoCleared?: number;
};

export type BailingReconciliation = {
  orderNo: string;
  plannedQty: number;
  bailedQty: number;
  shortfall: number;
  readyForDispatch: boolean;
  message: string;
};

function mapExecutionLine(row: Record<string, unknown>) {
  return {
    lineNo: Number(row.lineNo ?? row.LineNo ?? 0),
    shift: String(row.shift ?? row.Shift ?? ""),
    planDate: (row.planDate ?? row.PlanDate ?? null) as string | null,
    plannedQty: Number(row.plannedQty ?? row.PlannedQty ?? 0),
    producedQty: Number(row.producedQty ?? row.ProducedQty ?? 0),
    openBacklogQty: Number(row.openBacklogQty ?? row.OpenBacklogQty ?? 0),
  };
}

function normalizeExecution(data: Record<string, unknown>): OrderExecutionSummary {
  const lines = (data.lines ?? data.Lines ?? []) as Array<Record<string, unknown>>;
  const lineShiftTotals = (data.lineShiftTotals ?? data.LineShiftTotals ?? []) as Array<Record<string, unknown>>;
  const productionEntries = (data.productionEntries ?? data.ProductionEntries ?? []) as Array<
    Record<string, unknown>
  >;
  const suggestions = (data.replanSuggestions ?? data.ReplanSuggestions ?? []) as unknown;
  return {
    orderNo: String(data.orderNo ?? data.OrderNo ?? ""),
    companyName: String(data.companyName ?? data.CompanyName ?? ""),
    plannedQty: Number(data.plannedQty ?? data.PlannedQty ?? 0),
    producedQty: Number(data.producedQty ?? data.ProducedQty ?? 0),
    bailedQty: Number(data.bailedQty ?? data.BailedQty ?? 0),
    pendingQty: Number(data.pendingQty ?? data.PendingQty ?? 0),
    bailingGap: Number(data.bailingGap ?? data.BailingGap ?? 0),
    lines: lines.map(mapExecutionLine),
    lineShiftTotals: lineShiftTotals.map((row) => ({
      lineNo: Number(row.lineNo ?? row.LineNo ?? 0),
      shift: String(row.shift ?? row.Shift ?? ""),
      plannedQty: Number(row.plannedQty ?? row.PlannedQty ?? 0),
      producedQty: Number(row.producedQty ?? row.ProducedQty ?? 0),
      pendingQty: Number(row.pendingQty ?? row.PendingQty ?? 0),
    })),
    productionEntries: productionEntries.map((row) => ({
      lineNo: Number(row.lineNo ?? row.LineNo ?? 0),
      teamNo: String(row.teamNo ?? row.TeamNo ?? ""),
      shift: String(row.shift ?? row.Shift ?? ""),
      prodDate: String(row.prodDate ?? row.ProdDate ?? ""),
      quantity: Number(row.quantity ?? row.Quantity ?? 0),
    })),
    replanSuggestions: Array.isArray(suggestions) ? suggestions.map(String) : [],
    backlogRowsAutoCleared: Number(data.backlogRowsAutoCleared ?? data.BacklogRowsAutoCleared ?? 0) || undefined,
  };
}

export async function fetchOrderExecution(orderNo: string, company?: string): Promise<OrderExecutionSummary> {
  const res = await fetch(planningOrderUrl("/api/planning/execution/orders", orderNo, { company }));
  return normalizeExecution(await parseJson(res));
}

export async function fetchBailingReconciliation(orderNo: string, company?: string): Promise<BailingReconciliation> {
  const res = await fetch(planningOrderUrl("/api/planning/execution/orders/bailing", orderNo, { company }));
  const data = await parseJson<Record<string, unknown>>(res);
  return {
    orderNo: String(data.orderNo ?? data.OrderNo ?? orderNo),
    plannedQty: Number(data.plannedQty ?? data.PlannedQty ?? 0),
    bailedQty: Number(data.bailedQty ?? data.BailedQty ?? 0),
    shortfall: Number(data.shortfall ?? data.Shortfall ?? 0),
    readyForDispatch: Boolean(data.readyForDispatch ?? data.ReadyForDispatch),
    message: String(data.message ?? data.Message ?? ""),
  };
}

export async function fetchFactoryExecutionBoard(company: string, date?: string) {
  const params = new URLSearchParams({ company });
  if (date) params.set("date", date);
  const res = await fetch(getApiUrl(`/api/planning/execution/board?${params.toString()}`));
  return parseJson(res);
}

export type AccessoryMaterialStatus = {
  heading: string;
  category: string;
  requiredQty: number | null;
  unit: string;
  status: string;
  indentNo: string | null;
  itemCode: string | null;
  itemDesc: string | null;
  indentQty: number | null;
  mrnNo: string | null;
  receivedQty: number;
  pendingQty: number;
  companyName: string | null;
  detail: string | null;
};

export type AccessoryMaterialBoard = {
  orderNo: string;
  dispatchDate: string | null;
  items: AccessoryMaterialStatus[];
  warnings: string[];
};

export async function fetchAccessoryMaterials(orderNo: string): Promise<AccessoryMaterialBoard> {
  const params = new URLSearchParams({ orderNo });
  const res = await fetch(getApiUrl(`/api/planning/execution/orders/accessories?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  const items = (data.items ?? data.Items ?? []) as Array<Record<string, unknown>>;
  const warnings = (data.warnings ?? data.Warnings ?? []) as unknown;
  const num = (row: Record<string, unknown>, ...keys: string[]) => {
    for (const key of keys) {
      const value = row[key];
      if (typeof value === "number" && !Number.isNaN(value)) return value;
    }
    return 0;
  };
  const optNum = (row: Record<string, unknown>, ...keys: string[]): number | null => {
    for (const key of keys) {
      const value = row[key];
      if (typeof value === "number" && !Number.isNaN(value)) return value;
    }
    return null;
  };
  const str = (row: Record<string, unknown>, ...keys: string[]) => {
    for (const key of keys) {
      const value = row[key];
      if (value != null && value !== "") return String(value);
    }
    return "";
  };
  return {
    orderNo: str(data, "orderNo", "OrderNo"),
    dispatchDate: str(data, "dispatchDate", "DispatchDate") || null,
    warnings: Array.isArray(warnings) ? warnings.map(String) : [],
    items: items.map((row) => ({
      heading: str(row, "heading", "Heading"),
      category: str(row, "category", "Category"),
      requiredQty: optNum(row, "requiredQty", "RequiredQty"),
      unit: str(row, "unit", "Unit"),
      status: str(row, "status", "Status") || "NotFound",
      indentNo: str(row, "indentNo", "IndentNo") || null,
      itemCode: str(row, "itemCode", "ItemCode") || null,
      itemDesc: str(row, "itemDesc", "ItemDesc") || null,
      indentQty: optNum(row, "indentQty", "IndentQty"),
      mrnNo: str(row, "mrnNo", "MrnNo") || null,
      receivedQty: num(row, "receivedQty", "ReceivedQty"),
      pendingQty: num(row, "pendingQty", "PendingQty"),
      companyName: str(row, "companyName", "CompanyName") || null,
      detail: str(row, "detail", "Detail") || null,
    })),
  };
}
