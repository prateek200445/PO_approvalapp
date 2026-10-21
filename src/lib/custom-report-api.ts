import { getApiUrl } from "@/lib/api-config";

export interface CustomReportSaved {
  reportName: string;
  queryString: string;
  tableName: string;
  username: string;
  isVisibleToAll: string;
}

export interface CustomReportResult {
  tableName: string;
  columns: string[];
  rows: Record<string, unknown>[];
  rowCount: number;
  truncated: boolean;
}

async function readError(res: Response, fallback: string): Promise<string> {
  try {
    const body = (await res.json()) as { message?: string };
    if (body.message) return body.message;
  } catch {
    // ignore
  }
  return fallback;
}

export async function getCustomReportSources(): Promise<string[]> {
  const res = await fetch(getApiUrl("/api/custom-report/sources"));
  if (!res.ok) throw new Error(await readError(res, "Failed to load sources"));
  return (await res.json()) as string[];
}

export async function getCustomReportSaved(username?: string): Promise<CustomReportSaved[]> {
  const params = new URLSearchParams();
  if (username?.trim()) params.set("username", username.trim());
  const res = await fetch(getApiUrl(`/api/custom-report/saved?${params}`));
  if (!res.ok) throw new Error(await readError(res, "Failed to load saved reports"));
  const raw = (await res.json()) as Array<Record<string, unknown>>;
  return raw.map((r) => ({
    reportName: String(r.reportName ?? r.ReportName ?? ""),
    queryString: String(r.queryString ?? r.QueryString ?? ""),
    tableName: String(r.tableName ?? r.TableName ?? ""),
    username: String(r.username ?? r.Username ?? ""),
    isVisibleToAll: String(r.isVisibleToAll ?? r.IsVisibleToAll ?? "no"),
  }));
}

export async function getCustomReportColumns(table: string): Promise<string[]> {
  const params = new URLSearchParams({ table });
  const res = await fetch(getApiUrl(`/api/custom-report/columns?${params}`));
  if (!res.ok) throw new Error(await readError(res, "Failed to load columns"));
  return (await res.json()) as string[];
}

export async function runCustomReport(tableName: string, columns: string[]): Promise<CustomReportResult> {
  const res = await fetch(getApiUrl("/api/custom-report/run"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ tableName, columns }),
  });
  if (!res.ok) throw new Error(await readError(res, "Failed to run report"));
  const raw = (await res.json()) as Record<string, unknown>;
  return {
    tableName: String(raw.tableName ?? raw.TableName ?? tableName),
    columns: (raw.columns ?? raw.Columns ?? []) as string[],
    rows: (raw.rows ?? raw.Rows ?? []) as Record<string, unknown>[],
    rowCount: Number(raw.rowCount ?? raw.RowCount ?? 0),
    truncated: Boolean(raw.truncated ?? raw.Truncated),
  };
}

export async function saveCustomReport(input: {
  reportName: string;
  tableName: string;
  columns: string[];
  username: string;
  isVisibleToAll?: string;
}): Promise<void> {
  const res = await fetch(getApiUrl("/api/custom-report/save"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  if (!res.ok) throw new Error(await readError(res, "Failed to save report"));
}
