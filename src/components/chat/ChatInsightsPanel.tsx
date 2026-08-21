import { SqlHighlight } from "@/components/chat/SqlHighlight";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { exportChatCsv } from "@/lib/chat-api";
import {
  canExportResponse,
  exportButtonLabel,
  shouldUseServerExport,
} from "@/lib/chat-export";
import {
  downloadCsv,
  formatRowCountBadge,
  primaryTables,
  rowsToCsv,
} from "@/lib/chat-helpers";
import type { ChatApiResponse } from "@/lib/chat-types";
import { cn } from "@/lib/utils";
import {
  AlertTriangle,
  Copy,
  Database,
  Download,
  FileSpreadsheet,
  PanelRightClose,
  Table2,
  Telescope,
} from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

interface ChatInsightsPanelProps {
  response: ChatApiResponse | null;
  onClose?: () => void;
  className?: string;
}

function stampName(ext: string) {
  const stamp = new Date().toISOString().slice(0, 19).replace(/[:T]/g, "-");
  return `assistant-export-${stamp}.${ext}`;
}

function needsFullExport(response: ChatApiResponse) {
  return shouldUseServerExport(response);
}

export function ChatInsightsPanel({ response, onClose, className }: ChatInsightsPanelProps) {
  const [exporting, setExporting] = useState(false);

  async function copySql() {
    if (!response?.sql) return;
    try {
      await navigator.clipboard.writeText(response.sql);
      toast.success("SQL copied");
    } catch {
      toast.error("Could not copy SQL");
    }
  }

  async function exportFullOrLocal() {
    if (!response) {
      toast.error("No rows to export");
      return;
    }
    if (needsFullExport(response)) {
      setExporting(true);
      try {
        const { truncated, rowCount } = await exportChatCsv(
          response.sql || undefined,
          stampName("csv"),
          response.exportContext,
        );
        toast.success(
          truncated
            ? `Exported ${rowCount} rows (export capped)`
            : `Exported ${rowCount} rows`,
        );
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Export failed");
      } finally {
        setExporting(false);
      }
      return;
    }
    if (response.rows.length === 0) {
      toast.error("No rows to export");
      return;
    }
    downloadCsv(response.rows, stampName("csv"));
    toast.success("CSV downloaded");
  }

  async function exportExcelFriendly() {
    if (!response) {
      toast.error("No rows to export");
      return;
    }
    if (needsFullExport(response)) {
      setExporting(true);
      try {
        const { truncated, rowCount } = await exportChatCsv(
          response.sql || undefined,
          stampName("csv"),
          response.exportContext,
        );
        toast.success(
          truncated
            ? `Exported ${rowCount} rows (export capped) — open in Excel`
            : `Downloaded ${rowCount} rows — open in Excel`,
        );
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Export failed");
      } finally {
        setExporting(false);
      }
      return;
    }
    if (response.rows.length === 0) {
      toast.error("No rows to export");
      return;
    }
    const csv = "\uFEFF" + rowsToCsv(response.rows);
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = stampName("csv");
    a.click();
    URL.revokeObjectURL(url);
    toast.success("Downloaded — open in Excel");
  }

  return (
    <aside
      className={cn(
        "chat-insights-panel flex h-full w-72 shrink-0 flex-col border-l-2 border-violet-500/20 xl:w-80 dark:border-violet-400/25",
        className,
      )}
    >
      <div className="shrink-0 border-b border-violet-500/15 bg-violet-500/[0.04] px-4 py-3.5 dark:border-violet-400/20 dark:bg-violet-400/[0.06]">
        <div className="flex items-start justify-between gap-2">
          <div className="flex min-w-0 items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-violet-500/15 text-violet-700 ring-1 ring-violet-500/20 dark:bg-violet-400/15 dark:text-violet-300 dark:ring-violet-400/25">
              <Telescope className="h-4 w-4" strokeWidth={2.25} />
            </div>
            <div className="min-w-0 pt-0.5">
              <h2 className="text-sm font-semibold tracking-tight text-foreground">
                Answer details
              </h2>
              <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
                SQL, sources &amp; export for the selected reply
              </p>
            </div>
          </div>
          {onClose && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-8 w-8 shrink-0 text-muted-foreground hover:text-foreground"
              onClick={onClose}
              aria-label="Close answer details"
            >
              <PanelRightClose className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      {!response ? (
        <div className="flex flex-1 flex-col items-center justify-center gap-3 px-6 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-violet-500/10 dark:bg-violet-400/10">
            <Database className="h-6 w-6 text-violet-600/50 dark:text-violet-300/50" />
          </div>
          <div>
            <p className="text-sm font-medium text-foreground/80">No answer selected</p>
            <p className="mt-1 text-xs leading-relaxed text-muted-foreground">
              Click a reply in the chat to inspect its query, data sources, and export options here.
            </p>
          </div>
        </div>
      ) : (
        <ScrollArea className="flex-1 [scrollbar-color:color-mix(in_oklch,oklch(0.55_0.15_285)_35%,transparent)_transparent] [scrollbar-width:thin]">
          <div className="space-y-5 p-4">
            <section>
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-violet-700/80 dark:text-violet-300/80">
                Data sources
              </p>
              <div className="space-y-2">
                {primaryTables(response.tablesUsed).map((t) => (
                  <div
                    key={`${t.objectName}-${t.score}`}
                    className="flex items-center justify-between rounded-xl border border-border/60 bg-card/70 px-3 py-2.5 shadow-sm dark:bg-card/40"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-xs font-medium">{t.objectName}</p>
                      <p className="text-[10px] text-muted-foreground">{t.domain || "—"}</p>
                    </div>
                    <Badge className="shrink-0 gap-1 border-success/30 bg-success/10 font-normal text-success">
                      <span className="h-1.5 w-1.5 rounded-full bg-success" />
                      Live
                    </Badge>
                  </div>
                ))}
                {response.tablesUsed.length === 0 && (
                  <p className="text-xs text-muted-foreground">No tables reported.</p>
                )}
              </div>
            </section>

            <section>
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-violet-700/80 dark:text-violet-300/80">
                Result
              </p>
              <div className="grid grid-cols-2 gap-2">
                <div className="rounded-xl border border-border/60 bg-card/70 px-3 py-2.5 shadow-sm dark:bg-card/40">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Table2 className="h-3 w-3" />
                    <span className="text-[10px] uppercase tracking-wide">Rows</span>
                  </div>
                  <p className="mt-1 text-sm font-semibold tabular-nums leading-snug">
                    {formatRowCountBadge(response)}
                  </p>
                </div>
                <div className="rounded-xl border border-border/60 bg-card/70 px-3 py-2.5 shadow-sm dark:bg-card/40">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Database className="h-3 w-3" />
                    <span className="text-[10px] uppercase tracking-wide">Tables</span>
                  </div>
                  <p className="mt-1 text-lg font-semibold tabular-nums">
                    {response.tablesUsed.length}
                  </p>
                </div>
              </div>
            </section>

            {response.warning && (
              <section className="rounded-xl border border-warning/30 bg-warning/8 px-3 py-2.5 dark:bg-warning/10">
                <div className="mb-1 flex items-center gap-1.5 text-warning">
                  <AlertTriangle className="h-3.5 w-3.5" />
                  <span className="text-[11px] font-semibold">Governed rewrite</span>
                </div>
                <p className="text-[11px] leading-relaxed text-muted-foreground">
                  {response.warning}
                </p>
              </section>
            )}

            <section>
              <div className="mb-2 flex items-center justify-between">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-violet-700/80 dark:text-violet-300/80">
                  SQL query
                </p>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-7 gap-1 px-2 text-[11px]"
                  onClick={copySql}
                >
                  <Copy className="h-3 w-3" />
                  Copy
                </Button>
              </div>
              <SqlHighlight sql={response.sql} />
            </section>

            <section className="space-y-2 pb-2">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-violet-700/80 dark:text-violet-300/80">
                Export
              </p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-9 w-full justify-start gap-2 rounded-xl border-border/60 bg-card/60 dark:bg-card/40"
                onClick={() => void exportFullOrLocal()}
                disabled={exporting || !canExportResponse(response)}
              >
                <Download className="h-3.5 w-3.5" />
                {exporting
                  ? "Exporting…"
                  : exportButtonLabel(response)}
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-9 w-full justify-start gap-2 rounded-xl border-border/60 bg-card/60 dark:bg-card/40"
                onClick={() => void exportExcelFriendly()}
                disabled={exporting || !canExportResponse(response)}
              >
                <FileSpreadsheet className="h-3.5 w-3.5" />
                Open in Excel
              </Button>
            </section>
          </div>
        </ScrollArea>
      )}
    </aside>
  );
}
