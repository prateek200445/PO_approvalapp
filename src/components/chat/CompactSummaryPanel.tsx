import { InlineMarkdown } from "@/components/chat/FormattedAnswer";
import { cn } from "@/lib/utils";
import { BarChart3, Layers, Package } from "lucide-react";

export interface ParsedCompactSummary {
  count?: string;
  heroValue?: string;
  heroLabel?: string;
  meta: string[];
  breakdown: { label: string; value: string }[];
  topLabel?: string;
  prose?: string;
}

const BREAKDOWN_LABEL =
  /^(Fabric|Webbing|Filler|Tape|Yarn|Other|[A-Z][A-Za-z0-9 .&/-]{1,40})\s+([\d,.₹]+(?:\.\d+)?(?:\s*(?:Cr|L|K|kg|m))?)$/;

const TOTAL_SEGMENT =
  /^([\d,.₹]+(?:\.\d+)?(?:\s*(?:kg|m|mt|t))?)\s+total(?:\s+(.+))?$/i;

const COUNT_SEGMENT = /^(\d[\d,]*)\s+(lines|rows|depts|matching records?|stock line\(s\)|items?)/i;

const META_SEGMENT =
  /^(?:\d[\d,]*\s+)?(godowns?|items?|countries|parties|vendors|ledgers|customers|buyers|departments?)/i;

export function parseCompactSummary(text: string): ParsedCompactSummary {
  const normalized = text.replace(/\*\*/g, "").trim();
  if (!normalized) {
    return { meta: [], breakdown: [] };
  }

  const lines = normalized.split(/\n+/).map((l) => l.trim()).filter(Boolean);
  const prose =
    lines.length > 1
      ? lines.find((l) => l.length > 80 && !l.includes("·"))
      : undefined;

  const bulletSource = lines.length > 1 ? lines.filter((l) => l.includes("·") || l.includes(":")).join(" · ") : normalized;
  const segments = bulletSource
    .split(/\s·\s|(?<=\.)\s+(?=By material:)/i)
    .flatMap((s) => (/\bby material:/i.test(s) ? [s] : [s]))
    .map((s) => s.trim())
    .filter(Boolean);

  const result: ParsedCompactSummary = { meta: [], breakdown: [] };

  for (const seg of segments) {
    if (/\bby material:/i.test(seg)) {
      const materialParts = seg.split(/\bby material:/i)[1]?.split(/\s·\s/) ?? [];
      for (const part of materialParts) {
        const item = parseBreakdownPart(part.trim());
        if (item) result.breakdown.push(item);
      }
      continue;
    }
    if (/\(\+\d+\s+more/i.test(seg)) {
      result.topLabel = seg;
      continue;
    }

    const topMatch = seg.match(/^Top\s+(\d+)\s+(.+?):\s*(.+)$/i);
    if (topMatch) {
      result.topLabel = `Top ${topMatch[1]} ${topMatch[2]}`;
      const rest = topMatch[3].split(/\s·\s/);
      for (const part of rest) {
        const item = parseBreakdownPart(part);
        if (item) result.breakdown.push(item);
      }
      continue;
    }

    if (COUNT_SEGMENT.test(seg)) {
      result.count = seg;
      continue;
    }

    const totalMatch = seg.match(TOTAL_SEGMENT);
    if (totalMatch) {
      result.heroValue = totalMatch[1];
      result.heroLabel = totalMatch[2]?.trim() || "total";
      continue;
    }

    if (/total/i.test(seg) && !result.heroValue) {
      const num = seg.match(/([\d,.₹]+(?:\.\d+)?)/);
      if (num) {
        result.heroValue = num[1];
        const label = seg
          .replace(num[1], "")
          .replace(/^[\s—–-]+/, "")
          .replace(/\s+in hand.*$/i, " in hand")
          .trim();
        result.heroLabel = label || "total";
      }
      continue;
    }

    if (META_SEGMENT.test(seg)) {
      result.meta.push(seg);
      continue;
    }

    const breakdown = parseBreakdownPart(seg);
    if (breakdown) {
      result.breakdown.push(breakdown);
      continue;
    }

    if (!result.prose && seg.length > 20) {
      result.prose = result.prose ? `${result.prose} ${seg}` : seg;
    }
  }

  if (
    result.meta.length === 0
    && result.breakdown.length === 0
    && !result.heroValue
    && !result.count
  ) {
    result.prose = normalized;
  }

  return result;
}

function parseBreakdownPart(part: string): { label: string; value: string } | null {
  const cleaned = part.replace(/\(\+\d+\s+more[^)]*\)/i, "").trim();
  const match = cleaned.match(BREAKDOWN_LABEL);
  if (!match) return null;
  return { label: match[1].trim(), value: match[2].trim() };
}

interface CompactSummaryPanelProps {
  text: string;
  className?: string;
}

export function CompactSummaryPanel({ text, className }: CompactSummaryPanelProps) {
  const parsed = parseCompactSummary(text);
  const hasStructure =
    parsed.heroValue
    || parsed.breakdown.length > 0
    || parsed.meta.length > 0
    || parsed.count;

  if (!hasStructure) {
    return (
      <div
        className={cn(
          "rounded-xl border border-white/[0.08] bg-gradient-to-br from-white/[0.05] to-white/[0.02] px-4 py-3.5",
          className,
        )}
      >
        <p className="text-[13px] leading-relaxed text-slate-200/95 md:text-sm">
          <InlineMarkdown text={text} highlight />
        </p>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "rounded-xl border border-white/[0.08] bg-gradient-to-br from-white/[0.06] to-white/[0.02]",
        "px-4 py-4 md:px-6 md:py-5",
        className,
      )}
    >
      <div className="mb-4 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
        <BarChart3 className="h-3.5 w-3.5 text-cyan-400/80" />
        Summary
      </div>

      {(parsed.heroValue || parsed.count) && (
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end sm:gap-x-6 sm:gap-y-2">
          {parsed.heroValue && (
            <div>
              <p className="text-2xl font-semibold tabular-nums tracking-tight text-white md:text-[1.75rem]">
                {parsed.heroValue}
              </p>
              <p className="mt-1 text-xs capitalize text-slate-400">
                {parsed.heroLabel ?? "total"}
              </p>
            </div>
          )}
          {parsed.count && (
            <p className="text-sm text-slate-300/90 sm:pb-1">{parsed.count}</p>
          )}
        </div>
      )}

      {parsed.meta.length > 0 && (
        <div className="mb-4 flex flex-wrap gap-2">
          {parsed.meta.map((chip) => (
            <span
              key={chip}
              className="inline-flex items-center gap-1.5 rounded-full border border-white/[0.08] bg-white/[0.04] px-2.5 py-1 text-[11px] font-medium text-slate-300"
            >
              <Layers className="h-3 w-3 text-slate-500" />
              {chip}
            </span>
          ))}
        </div>
      )}

      {parsed.breakdown.length > 0 && (
        <div className="space-y-2.5">
          <p className="text-[11px] font-medium uppercase tracking-wide text-slate-500">
            {parsed.topLabel ?? "Breakdown"}
          </p>
          <ul className="space-y-2">
            {parsed.breakdown.map((row) => (
              <li
                key={`${row.label}-${row.value}`}
                className="flex items-center justify-between gap-4 rounded-lg bg-white/[0.03] px-3.5 py-2.5"
              >
                <span className="flex items-center gap-2 text-sm text-slate-300">
                  <Package className="h-3.5 w-3.5 shrink-0 text-slate-500" />
                  {row.label}
                </span>
                <span className="shrink-0 text-sm font-medium tabular-nums text-white">
                  {row.value}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {parsed.prose && (
        <p className="mt-3 border-t border-white/[0.06] pt-3 text-[13px] leading-relaxed text-slate-300/90">
          <InlineMarkdown text={parsed.prose} highlight />
        </p>
      )}
    </div>
  );
}
