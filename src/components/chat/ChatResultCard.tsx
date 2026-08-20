import { FormattedAnswer, InlineMarkdown } from "@/components/chat/FormattedAnswer";
import { SqlHighlight } from "@/components/chat/SqlHighlight";
import { exportChatCsv } from "@/lib/chat-api";
import {
  downloadCsv,
  formatRowCountBadge,
  getFollowUpPrompts,
} from "@/lib/chat-helpers";
import type { ChatApiResponse } from "@/lib/chat-types";
import {
  detectResultCardMode,
  extractHeroMetric,
  formatCellValue,
  getDomainTheme,
  getListPrimaryColumn,
  getListSecondaryColumns,
  hasReorderProgress,
  humanizeColumn,
  isGovernedResponse,
  reorderProgress,
  userFacingWarning,
  splitAnswerRich,
} from "@/lib/result-card-format";
import { cn } from "@/lib/utils";
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Copy,
  Download,
  Layers,
  PanelRightOpen,
  SearchX,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { useState, type ReactNode } from "react";
import { toast } from "sonner";

interface ChatResultCardProps {
  response: ChatApiResponse;
  answer: string;
  selected?: boolean;
  onSelect?: () => void;
  onFollowUp?: (prompt: string) => void;
}

const PREVIEW_ROWS = 6;
const PREVIEW_TABLE_ROWS = 8;

/** Title already states a count — skip the large hero block on mobile to save space. */
function titleAlreadyShowsCount(title: string): boolean {
  return /\bfound\b[\s\S]*?[\d,]+/i.test(title) || /[\d,]+\s+matching/i.test(title);
}

export function ChatResultCard({
  response,
  answer,
  selected,
  onSelect,
  onFollowUp,
}: ChatResultCardProps) {
  const [showAll, setShowAll] = useState(false);
  const [showSql, setShowSql] = useState(false);
  const [exporting, setExporting] = useState(false);

  const { title, body } = splitAnswerRich(answer);
  const theme = getDomainTheme(response, answer);
  const mode = detectResultCardMode(response.rows);
  const columns =
    response.rows.length > 0 ? Object.keys(response.rows[0]) : [];
  const hero = extractHeroMetric(response.rows);
  const governed = isGovernedResponse(response.warning);
  const displayWarning = userFacingWarning(response.warning);
  const followUps = getFollowUpPrompts(response);
  const showProgress = hasReorderProgress(columns);
  const listPrimary = getListPrimaryColumn(columns);
  const listSecondary = getListSecondaryColumns(columns, listPrimary);

  const needsServerExport =
    Boolean(response.truncated) ||
    (response.totalCount != null && response.totalCount > response.rowCount);

  const visibleRows = showAll
    ? response.rows
    : response.rows.slice(0, mode === "table" ? PREVIEW_TABLE_ROWS : PREVIEW_ROWS);

  const rowLabel = formatRowCountBadge(response);
  const hideHeroOnMobile = titleAlreadyShowsCount(title);

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
    <div className="result-card space-y-3">
      <article
        className={cn(
          "relative overflow-hidden rounded-[1.35rem] border border-white/[0.09]",
          "bg-[#0a0d12] text-slate-100 shadow-[0_24px_80px_-12px_rgba(0,0,0,0.65)]",
          "ring-1 ring-white/[0.04]",
          selected && "ring-2 ring-cyan-400/50 shadow-cyan-500/10",
        )}
      >
        {/* Ambient glow */}
        <div
          className={cn(
            "pointer-events-none absolute -right-16 -top-16 h-48 w-48 rounded-full blur-3xl",
            theme.glow,
          )}
        />
        <div className="pointer-events-none absolute -bottom-20 -left-10 h-40 w-40 rounded-full bg-indigo-600/10 blur-3xl" />

        {/* Accent strip */}
        <div className={cn("h-1 w-full bg-gradient-to-r", theme.accent)} />

        {/* Header */}
        <header className="relative flex flex-wrap items-start justify-between gap-2 border-b border-white/[0.06] px-3 py-2.5 md:gap-3 md:px-5 md:py-4">
          <div className="min-w-0 space-y-2">
            <div className="flex flex-wrap items-center gap-2">
              <span
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r px-2.5 py-1 text-[11px] font-semibold tracking-wide text-white",
                  theme.accent,
                )}
              >
                <Sparkles className="h-3 w-3 opacity-90" />
                {theme.label}
              </span>
              <LiveBadge />
              {governed && (
                <span className="inline-flex items-center gap-1 rounded-full border border-emerald-400/30 bg-emerald-500/10 px-2 py-0.5 text-[10px] font-medium text-emerald-300">
                  <ShieldCheck className="h-3 w-3" />
                  Verified
                </span>
              )}
              {response.truncated && (
                <span className="rounded-full border border-amber-400/25 bg-amber-500/10 px-2 py-0.5 text-[10px] font-medium text-amber-200">
                  Sample
                </span>
              )}
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <div className="rounded-lg border border-white/[0.08] bg-white/[0.03] px-2 py-1.5 text-right md:rounded-xl md:px-3 md:py-2">
              <p className="text-[9px] uppercase tracking-widest text-slate-500 md:text-[10px]">Records</p>
              <p className="text-sm font-semibold tabular-nums leading-none text-white md:text-lg">
                {rowLabel}
              </p>
            </div>
            {onSelect && (
              <button
                type="button"
                onClick={onSelect}
                className={cn(
                  "flex h-8 w-8 items-center justify-center rounded-lg border transition-colors md:h-10 md:w-10 md:rounded-xl",
                  selected
                    ? "border-cyan-400/40 bg-cyan-500/15 text-cyan-300"
                    : "border-white/[0.08] bg-white/[0.03] text-slate-400 hover:border-white/20 hover:text-white",
                )}
                title="Open answer details"
              >
                <PanelRightOpen className="h-4 w-4" />
              </button>
            )}
          </div>
        </header>

        {/* Answer */}
        <section className="relative px-3 pt-3 md:px-5 md:pt-5">
          <h3 className="text-[15px] font-semibold leading-snug tracking-tight text-white md:text-[1.35rem]">
            <InlineMarkdown text={title} />
          </h3>
          {body && (
            <div className="mt-2 text-sm leading-relaxed text-slate-200/95 md:mt-3 md:text-[15px]">
              <FormattedAnswer text={body} variant="highlight" />
            </div>
          )}
        </section>

        {/* User-facing note (no table/SQL names) */}
        {displayWarning && (
          <div className="relative mx-3 mt-3 flex gap-2.5 rounded-xl border border-emerald-500/20 bg-emerald-500/[0.07] px-3 py-2.5 md:mx-5 md:mt-4 md:gap-3 md:px-4 md:py-3">
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-400" />
            <div>
              <p className="text-xs font-medium text-emerald-300">Note</p>
              <p className="mt-0.5 text-[11px] leading-relaxed text-emerald-200/70">
                {displayWarning}
              </p>
            </div>
          </div>
        )}

        {/* Data body */}
        <section className="relative px-3 py-3 md:px-5 md:py-5">
          {mode === "empty" && <EmptyState />}
          {mode === "summary" && hero && (
            <div className={cn(hideHeroOnMobile && "hidden md:block")}>
              <SummaryHero label={hero.label} value={hero.value} kind={hero.kind} />
            </div>
          )}
          {mode === "summary" && !hero && columns.length > 0 && (
            <KeyValueGrid row={response.rows[0]} columns={columns} />
          )}
          {mode === "list" && (
            <RankedList
              rows={visibleRows}
              primary={listPrimary}
              secondary={listSecondary}
              showProgress={showProgress}
              startIndex={0}
            />
          )}
          {mode === "table" && columns.length > 0 && (
            <DataTable columns={columns} rows={visibleRows} />
          )}
        </section>

        {/* Expand + footer actions */}
        {response.rows.length >
          (mode === "table" ? PREVIEW_TABLE_ROWS : PREVIEW_ROWS) && (
          <div className="px-3 pb-2 md:px-5">
            <button
              type="button"
              onClick={() => setShowAll((v) => !v)}
              className="flex w-full items-center justify-center gap-2 rounded-xl border border-white/[0.08] bg-white/[0.03] py-2.5 text-xs font-medium text-slate-300 transition-colors hover:bg-white/[0.06] hover:text-white"
            >
              {showAll ? (
                <>
                  <ChevronUp className="h-3.5 w-3.5" />
                  Show less
                </>
              ) : (
                <>
                  <ChevronDown className="h-3.5 w-3.5" />
                  View all {response.rowCount} rows
                </>
              )}
            </button>
          </div>
        )}

        <footer className="relative flex flex-wrap items-center gap-1.5 border-t border-white/[0.06] px-3 py-2.5 md:gap-2 md:px-5 md:py-3.5">
          {(response.rows.length > 0 || (needsServerExport && response.sql)) && (
            <ActionButton
              icon={<Download className="h-3.5 w-3.5" />}
              label={
                exporting
                  ? "Exporting…"
                  : needsServerExport
                    ? "Export all"
                    : "Export CSV"
              }
              onClick={() => void exportCsv()}
              disabled={exporting}
              primary
            />
          )}
          <ActionButton
            icon={<Copy className="h-3.5 w-3.5" />}
            label="Copy SQL"
            onClick={() => void copySql()}
          />
          <ActionButton
            icon={<Layers className="h-3.5 w-3.5" />}
            label={showSql ? "Hide SQL" : "View SQL"}
            onClick={() => setShowSql((v) => !v)}
            active={showSql}
          />
        </footer>

        {showSql && response.sql && (
          <div className="border-t border-white/[0.06] px-3 py-3 md:px-5 md:py-4">
            <p className="mb-2 text-[10px] font-semibold uppercase tracking-widest text-slate-500">
              Generated query
            </p>
            <div className="overflow-hidden rounded-xl border border-white/[0.06]">
              <SqlHighlight sql={response.sql} />
            </div>
          </div>
        )}
      </article>

      {onFollowUp && followUps.length > 0 && (
        <div className="flex flex-wrap gap-2 pl-1">
          <span className="self-center text-[10px] uppercase tracking-wider text-slate-500">
            Try next
          </span>
          {followUps.map((prompt) => (
            <button
              key={prompt}
              type="button"
              onClick={() => onFollowUp(prompt)}
              className="rounded-full border border-white/10 bg-[#0a0d12]/80 px-3.5 py-1.5 text-xs text-slate-300 transition-all hover:border-cyan-400/30 hover:bg-cyan-500/10 hover:text-cyan-100"
            >
              {prompt.length > 52 ? `${prompt.slice(0, 52)}…` : prompt}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

function LiveBadge() {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.04] px-2 py-0.5 text-[10px] font-medium text-slate-300">
      <span className="relative flex h-1.5 w-1.5">
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-60" />
        <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-emerald-400" />
      </span>
      Live
    </span>
  );
}

function EmptyState() {
  return (
    <div className="flex flex-col items-center rounded-xl border border-dashed border-white/10 bg-white/[0.02] px-4 py-6 text-center md:rounded-2xl md:px-6 md:py-10">
      <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-xl bg-slate-800/80 ring-1 ring-white/10 md:mb-4 md:h-14 md:w-14 md:rounded-2xl">
        <SearchX className="h-5 w-5 text-slate-500 md:h-7 md:w-7" />
      </div>
      <p className="text-sm font-medium text-slate-200 md:text-base">No matching records</p>
      <p className="mt-1.5 max-w-sm text-xs leading-relaxed text-slate-500 md:mt-2 md:text-sm">
        The query ran successfully but returned zero rows. Filters may be too strict, or
        nothing is recorded for this period.
      </p>
    </div>
  );
}

function SummaryHero({
  label,
  value,
  kind,
}: {
  label: string;
  value: string;
  kind: string;
}) {
  return (
    <div className="rounded-xl border border-white/[0.08] bg-gradient-to-br from-white/[0.06] to-white/[0.02] px-4 py-4 text-center md:rounded-2xl md:px-6 md:py-8">
      <p className="text-[10px] font-medium uppercase tracking-[0.15em] text-slate-500 md:text-xs md:tracking-[0.2em]">
        {label}
      </p>
      <p
        className={cn(
          "mt-2 font-semibold tabular-nums tracking-tight text-white md:mt-3",
          kind === "currency"
            ? "text-2xl md:text-5xl"
            : "text-xl md:text-4xl",
        )}
      >
        {value}
      </p>
      <div className="mx-auto mt-2 flex h-1 w-12 overflow-hidden rounded-full bg-white/10 md:mt-4 md:w-16">
        <div className={cn("h-full w-full rounded-full bg-gradient-to-r", "from-cyan-400 to-violet-500")} />
      </div>
    </div>
  );
}

function KeyValueGrid({
  row,
  columns,
}: {
  row: Record<string, unknown>;
  columns: string[];
}) {
  return (
    <dl className="grid gap-2 sm:grid-cols-2 md:gap-3">
      {columns.map((col) => {
        const cell = formatCellValue(col, row[col]);
        return (
          <div
            key={col}
            className="rounded-lg border border-white/[0.06] bg-white/[0.03] px-3 py-2 md:rounded-xl md:px-4 md:py-3"
          >
            <dt className="text-[10px] font-medium uppercase tracking-wider text-slate-500">
              {humanizeColumn(col)}
            </dt>
            <dd
              className={cn(
                "mt-0.5 text-sm font-medium text-slate-100 md:mt-1",
                cell.kind === "currency" && "text-base tabular-nums text-emerald-300 md:text-lg",
                cell.kind === "document" && "font-mono text-cyan-200/90",
              )}
            >
              {cell.text}
            </dd>
          </div>
        );
      })}
    </dl>
  );
}

function RankedList({
  rows,
  primary,
  secondary,
  showProgress,
  startIndex,
}: {
  rows: Record<string, unknown>[];
  primary: string;
  secondary: string[];
  showProgress: boolean;
  startIndex: number;
}) {
  return (
    <ul className="space-y-1.5 md:space-y-2">
      {rows.map((row, i) => {
        const rank = startIndex + i + 1;
        const primaryCell = formatCellValue(primary, row[primary]);
        const progress = showProgress ? reorderProgress(row) : null;

        return (
          <li
            key={i}
            className="group flex gap-2.5 rounded-lg border border-white/[0.06] bg-white/[0.025] px-3 py-2 transition-colors hover:border-white/12 hover:bg-white/[0.04] md:gap-3 md:rounded-xl md:px-4 md:py-3"
          >
            <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-white/[0.06] text-[11px] font-bold tabular-nums text-slate-400 group-hover:text-cyan-300 md:h-8 md:w-8 md:rounded-lg md:text-xs">
              {rank}
            </span>
            <div className="min-w-0 flex-1">
              <p
                className={cn(
                  "truncate font-medium text-slate-100",
                  primaryCell.kind === "document" && "font-mono text-sm text-cyan-100/90",
                )}
                title={primaryCell.text}
              >
                {primaryCell.text}
              </p>
              {secondary.length > 0 && (
                <div className="mt-1.5 flex flex-wrap gap-x-4 gap-y-1">
                  {secondary.map((col) => {
                    const cell = formatCellValue(col, row[col]);
                    return (
                      <span key={col} className="text-xs text-slate-500">
                        <span className="text-slate-600">{humanizeColumn(col)}: </span>
                        <span
                          className={cn(
                            cell.kind === "currency" && "font-medium text-emerald-400/90",
                            cell.kind === "number" && "tabular-nums text-slate-300",
                          )}
                        >
                          {cell.text}
                        </span>
                      </span>
                    );
                  })}
                </div>
              )}
              {progress != null && (
                <div className="mt-2.5">
                  <div className="h-1.5 overflow-hidden rounded-full bg-white/10">
                    <div
                      className={cn(
                        "h-full rounded-full transition-all",
                        progress < 30
                          ? "bg-gradient-to-r from-rose-500 to-orange-400"
                          : progress < 70
                            ? "bg-gradient-to-r from-amber-400 to-yellow-300"
                            : "bg-gradient-to-r from-emerald-400 to-teal-400",
                      )}
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                </div>
              )}
            </div>
          </li>
        );
      })}
    </ul>
  );
}

function DataTable({
  columns,
  rows,
}: {
  columns: string[];
  rows: Record<string, unknown>[];
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-white/[0.08]">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[480px] text-left text-sm">
          <thead>
            <tr className="border-b border-white/[0.08] bg-white/[0.04]">
              {columns.map((col) => (
                <th
                  key={col}
                  className="whitespace-nowrap px-4 py-3 text-[11px] font-semibold tracking-wide text-slate-400"
                >
                  {humanizeColumn(col)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr
                key={i}
                className="border-b border-white/[0.04] last:border-0 transition-colors hover:bg-white/[0.03]"
              >
                {columns.map((col) => {
                  const cell = formatCellValue(col, row[col]);
                  return (
                    <td
                      key={col}
                      className={cn(
                        "max-w-[220px] px-4 py-3 text-slate-200",
                        cell.kind === "currency" &&
                          "whitespace-nowrap font-medium tabular-nums text-emerald-300/95",
                        cell.kind === "number" && "tabular-nums text-slate-300",
                        cell.kind === "document" &&
                          "font-mono text-xs text-cyan-200/85",
                        cell.kind === "date" && "whitespace-nowrap text-slate-400",
                        cell.kind === "text" && "truncate",
                      )}
                      title={cell.text}
                    >
                      {cell.text}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ActionButton({
  icon,
  label,
  onClick,
  disabled,
  primary,
  active,
}: {
  icon: ReactNode;
  label: string;
  onClick: () => void;
  disabled?: boolean;
  primary?: boolean;
  active?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-[11px] font-medium transition-all md:gap-2 md:rounded-xl md:px-3.5 md:py-2 md:text-xs",
        primary &&
          "border border-cyan-400/30 bg-gradient-to-r from-cyan-500/20 to-violet-500/20 text-cyan-100 hover:from-cyan-500/30 hover:to-violet-500/30",
        !primary &&
          (active
            ? "border border-white/20 bg-white/10 text-white"
            : "border border-white/[0.08] bg-white/[0.03] text-slate-400 hover:border-white/15 hover:text-slate-200"),
        disabled && "pointer-events-none opacity-50",
      )}
    >
      {icon}
      {label}
    </button>
  );
}
