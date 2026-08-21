import type { ChatApiResponse } from "@/lib/chat-types";

export interface ChatExportContext {
  kind: string;
  plan?: Record<string, unknown>;
}

export function isExecSqlDescription(sql: string): boolean {
  return sql.trim().toUpperCase().startsWith("EXEC");
}

/** True when the server should re-run SQL or an ERP SP to produce a full CSV. */
export function shouldUseServerExport(response: ChatApiResponse): boolean {
  if (response.exportContext) return true;
  if (response.rows.length === 0) return false;
  if (!response.sql || isExecSqlDescription(response.sql)) return false;
  return true;
}

export function canExportResponse(response: ChatApiResponse): boolean {
  return response.rows.length > 0 || shouldUseServerExport(response);
}

export function exportButtonLabel(response: ChatApiResponse): string {
  if (
    response.truncated ||
    (response.totalCount != null && response.totalCount > response.rowCount) ||
    response.exportContext
  ) {
    return "Export all";
  }
  if (shouldUseServerExport(response)) return "Export all CSV";
  return "Export CSV";
}
