import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import { exportChatCsv } from "@/lib/chat-api";
import {
  downloadCsv,
  formatCell,
  formatRowCountBadge,
  getFollowUpPrompts,
  splitAnswer,
} from "@/lib/chat-helpers";
import type { ChatApiResponse } from "@/lib/chat-types";
import { cn } from "@/lib/utils";
import { ArrowRight, Copy, Download, Table2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

interface ChatResultCardProps {
  response: ChatApiResponse;
  answer: string;
  selected?: boolean;
  onSelect?: () => void;
  onFollowUp?: (prompt: string) => void;
}

export function ChatResultCard({
  response,
  answer,
  selected,
  onSelect,
  onFollowUp,
}: ChatResultCardProps) {
  const [showAll, setShowAll] = useState(false);
  const [exporting, setExporting] = useState(false);
  const { headline, body } = splitAnswer(answer);
  const columns = response.rows.length > 0 ? Object.keys(response.rows[0]).slice(0, 6) : [];
  const visibleRows = showAll ? response.rows : response.rows.slice(0, 5);
  const followUps = getFollowUpPrompts(response);
  const needsServerExport =
    Boolean(response.truncated) ||
    (response.totalCount != null && response.totalCount > response.rowCount);

  async function copySql() {
    try {
      await navigator.clipboard.writeText(response.sql);
      toast.success("SQL copied");
    } catch {
      toast.error("Could not copy SQL");
    }
  }

  async function exportCsv() {
    if (!response.sql && response.rows.length === 0) {
      toast.error("No rows to export");
      return;
    }
    if (needsServerExport && response.sql) {
      setExporting(true);
      try {
        const { truncated, rowCount } = await exportChatCsv(response.sql);
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
    downloadCsv(response.rows);
    toast.success("CSV downloaded");
  }

  return (
    <div className="space-y-3">
      <button
        type="button"
        onClick={onSelect}
        className={cn(
          "w-full rounded-2xl border px-4 py-4 text-left shadow-md shadow-black/5 backdrop-blur-sm transition-all",
          "bg-card/80 dark:bg-card/60 dark:shadow-black/15",
          selected
            ? "border-primary/40 ring-2 ring-primary/20"
            : "border-border/50 hover:border-primary/25",
        )}
      >
        <div className="mb-2 flex flex-wrap items-center gap-1.5">
          <Badge variant="secondary" className="gap-1 font-normal">
            <Table2 className="h-3 w-3" />
            {formatRowCountBadge(response)}
          </Badge>
          {response.truncated && (
            <Badge variant="outline" className="font-normal text-amber-700 dark:text-amber-400">
              sample
            </Badge>
          )}
          {response.tablesUsed.slice(0, 2).map((t) => (
            <Badge key={t.objectName} variant="outline" className="font-normal">
              {t.objectName}
            </Badge>
          ))}
        </div>

        <h3 className="text-lg font-semibold tracking-tight text-foreground md:text-xl">
          {headline}
        </h3>
        {body && (
          <p className="mt-2 whitespace-pre-wrap text-sm leading-relaxed text-muted-foreground">
            {body}
          </p>
        )}

        {response.rows.length > 0 && columns.length > 0 && (
          <div className="mt-4 overflow-hidden rounded-xl border border-border/50 bg-muted/20">
            <ScrollArea className="w-full">
              <div className="min-w-max">
                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="border-b border-border/60 bg-muted/40">
                      {columns.map((col) => (
                        <th
                          key={col}
                          className="px-3 py-2.5 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground"
                        >
                          {col}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {visibleRows.map((row, i) => (
                      <tr
                        key={i}
                        className="border-b border-border/40 last:border-0 hover:bg-muted/30"
                      >
                        {columns.map((col) => (
                          <td key={col} className="max-w-[180px] truncate px-3 py-2.5 text-foreground/90">
                            {formatCell(row[col])}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <ScrollBar orientation="horizontal" />
            </ScrollArea>
          </div>
        )}

        <div
          className="mt-3 flex flex-wrap items-center gap-2"
          onClick={(e) => e.stopPropagation()}
        >
          {response.rows.length > 5 && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-8 gap-1 px-2 text-xs"
              onClick={() => setShowAll((v) => !v)}
            >
              {showAll ? "Show less" : `View all (${response.rows.length})`}
              {!showAll && <ArrowRight className="h-3 w-3" />}
            </Button>
          )}
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-8 gap-1 px-2 text-xs"
            onClick={copySql}
          >
            <Copy className="h-3 w-3" />
            Copy SQL
          </Button>
          {(response.rows.length > 0 || (needsServerExport && response.sql)) && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-8 gap-1 px-2 text-xs"
              onClick={(e) => {
                e.stopPropagation();
                void exportCsv();
              }}
              disabled={exporting}
            >
              <Download className="h-3 w-3" />
              {exporting
                ? "Exporting…"
                : needsServerExport
                  ? "Export all CSV"
                  : "Export CSV"}
            </Button>
          )}
        </div>
      </button>

      {onFollowUp && followUps.length > 0 && (
        <div className="flex flex-wrap gap-2 pl-1">
          {followUps.map((prompt) => (
            <button
              key={prompt}
              type="button"
              onClick={() => onFollowUp(prompt)}
              className="rounded-full border border-border/60 bg-card/70 px-3 py-1.5 text-[11px] font-medium text-muted-foreground transition-colors hover:border-primary/30 hover:bg-primary/5 hover:text-foreground"
            >
              {prompt.length > 48 ? `${prompt.slice(0, 48)}…` : prompt}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
