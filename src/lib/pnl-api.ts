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
  PnlUploadItem,
  PnlWorkbookImportResult,
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

export async function getPnlUploads(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/uploads?${params}`));
  return readJson<PnlUploadItem[]>(res);
}

export async function savePnlUpload(
  uploadType: "stock" | "provision" | "common" | "commonexpense" | "common-expense",
  company: string,
  month: string,
  file: File,
  remarks?: string,
) {
  const body = new FormData();
  body.append("file", file);
  body.append("company", company);
  body.append("month", month);
  if (remarks) body.append("remarks", remarks);
  const res = await fetch(getApiUrl(`/api/pnl/uploads/${uploadType}`), {
    method: "POST",
    body,
  });
  return readJson<{ ok: boolean }>(res);
}

export async function savePnlWorkbook(company: string, file: File, zeroCommonHo: boolean) {
  const body = new FormData();
  body.append("file", file);
  body.append("company", company);
  body.append("zeroCommonHo", zeroCommonHo ? "true" : "false");
  const res = await fetch(getApiUrl("/api/pnl/dev/workbook"), {
    method: "POST",
    body,
  });
  return readJson<PnlWorkbookImportResult>(res);
}

export async function downloadPnlTemplate(
  uploadType: "stock" | "provision" | "common",
  company: string,
  month: string,
) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/templates/${uploadType}?${params}`));
  if (!res.ok) {
    return readJson<{ message?: string }>(res).then((data) => {
      throw new Error(data.message || res.statusText);
    });
  }

  const bytes = await res.blob();
  const url = URL.createObjectURL(bytes);
  const link = document.createElement("a");
  link.href = url;
  link.download = `pnl-${uploadType}-template-${company.replace(/[^a-z0-9]+/gi, "-").toLowerCase()}-${month}.xlsx`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

async function downloadExcelResponse(res: Response, fallbackName: string) {
  if (!res.ok) {
    return readJson<{ message?: string }>(res).then((data) => {
      throw new Error(data.message || res.statusText);
    });
  }
  const bytes = await res.blob();
  const url = URL.createObjectURL(bytes);
  const link = document.createElement("a");
  link.href = url;
  const disposition = res.headers.get("content-disposition") ?? "";
  const match = /filename\*?=(?:UTF-8'')?["']?([^";]+)/i.exec(disposition);
  link.download = match?.[1]?.trim() || fallbackName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export async function downloadPnlStatement(company: string, month: string) {
  const params = new URLSearchParams({ company, month });
  const res = await fetch(getApiUrl(`/api/pnl/statement/export?${params}`));
  await downloadExcelResponse(res, `pnl-${month}.xlsx`);
}

export async function downloadPnlSummary(month: string) {
  const params = new URLSearchParams({ month });
  const res = await fetch(getApiUrl(`/api/pnl/summary/export?${params}`));
  await downloadExcelResponse(res, `pnl-summary-${month}.xlsx`);
}

export async function downloadPnlMonthGrid(month: string) {
  const params = new URLSearchParams({ month });
  const res = await fetch(getApiUrl(`/api/pnl/month-grid/export?${params}`));
  await downloadExcelResponse(res, `pnl-month-${month}.xlsx`);
}
