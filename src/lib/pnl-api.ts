import { getApiUrl } from "@/lib/api-config";
import type {
  PnlCompanyOption,
  PnlIncomeExpenseResult,
  PnlProvisionState,
  PnlStatementResult,
  PnlStockState,
  PnlStockYearState,
  PnlOverheadState,
  PnlProvisionRow,
  PnlStockRow,
} from "@/lib/pnl-types";

async function readJson<T>(res: Response): Promise<T> {
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((data as { message?: string }).message || res.statusText);
  return data as T;
}

export async function getPnlCompanies(): Promise<PnlCompanyOption[]> {
  const res = await fetch(getApiUrl("/api/pnl/companies"));
  return readJson<PnlCompanyOption[]>(res);
}

export async function getPnlIncomeExpense(company: string, dateFrom: string, dateTo: string) {
  const params = new URLSearchParams({ company, dateFrom, dateTo });
  const res = await fetch(getApiUrl(`/api/pnl/income-expense?${params}`));
  return readJson<PnlIncomeExpenseResult>(res);
}

export async function getPnlProvisions(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/provisions?${params}`));
  return readJson<PnlProvisionState>(res);
}

export async function savePnlProvisions(company: string, month: string, rows: PnlProvisionRow[]) {
  const res = await fetch(getApiUrl("/api/pnl/provisions"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ company, month, rows }),
  });
  return readJson<{ ok: boolean }>(res);
}

export async function getPnlStock(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/stock?${params}`));
  return readJson<PnlStockState>(res);
}

export async function savePnlStock(company: string, month: string, rows: PnlStockRow[]) {
  const res = await fetch(getApiUrl("/api/pnl/stock"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ company, month, rows }),
  });
  return readJson<{ ok: boolean }>(res);
}

export async function getPnlStockYear(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/stock-year?${params}`));
  return readJson<PnlStockYearState>(res);
}

export async function savePnlStockYear(payload: PnlStockYearState) {
  const res = await fetch(getApiUrl("/api/pnl/stock-year"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      company: payload.company,
      fyStart: payload.fyStart,
      rows: payload.rows,
    }),
  });
  return readJson<{ ok: boolean }>(res);
}

export async function getPnlOverhead(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/overhead?${params}`));
  return readJson<PnlOverheadState>(res);
}

export async function savePnlOverhead(
  company: string,
  month: string,
  commonLacs: number,
  hoLacs: number,
) {
  const res = await fetch(getApiUrl("/api/pnl/overhead"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ company, month, commonLacs, hoLacs }),
  });
  return readJson<{ ok: boolean }>(res);
}

export async function getPnlStatement(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/statement?${params}`));
  return readJson<PnlStatementResult>(res);
}
