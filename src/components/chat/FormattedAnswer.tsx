import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

/** Strip basic markdown markers for plain-text contexts (CSV, clipboard, etc.). */
export function stripMarkdown(text: string): string {
  return text
    .replace(/\*\*([^*]+)\*\*/g, "$1")
    .replace(/\*([^*]+)\*/g, "$1")
    .replace(/__([^_]+)__/g, "$1")
    .replace(/^[\*\-•]\s+/gm, "")
    .replace(/\*\*/g, "")
    .replace(/\*/g, "")
    .trim();
}

/** Parse inline **bold**, *italic*, and stray markers so raw ** never shows in UI. */
export function InlineMarkdown({ text, className }: { text: string; className?: string }) {
  const nodes = parseInlineMarkdown(text);
  return <span className={className}>{nodes}</span>;
}

function parseInlineMarkdown(text: string): ReactNode[] {
  const nodes: ReactNode[] = [];
  const pattern = /(\*\*[^*\n]+?\*\*|\*[^*\n]+?\*|__[^_\n]+?__)/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;
  let key = 0;

  while ((match = pattern.exec(text)) !== null) {
    if (match.index > lastIndex) {
      nodes.push(<span key={key++}>{text.slice(lastIndex, match.index)}</span>);
    }

    const token = match[0];
    if (token.startsWith("**") && token.endsWith("**")) {
      nodes.push(
        <strong key={key++} className="font-semibold text-slate-200">
          {token.slice(2, -2)}
        </strong>,
      );
    } else if (token.startsWith("*") && token.endsWith("*")) {
      nodes.push(
        <em key={key++} className="text-slate-300 not-italic font-medium">
          {token.slice(1, -1)}
        </em>,
      );
    } else if (token.startsWith("__") && token.endsWith("__")) {
      nodes.push(
        <strong key={key++} className="font-semibold text-slate-200">
          {token.slice(2, -2)}
        </strong>,
      );
    } else {
      nodes.push(<span key={key++}>{token.replace(/\*\*/g, "").replace(/\*/g, "")}</span>);
    }

    lastIndex = match.index + token.length;
  }

  if (lastIndex < text.length) {
    const tail = text.slice(lastIndex).replace(/\*\*/g, "");
    nodes.push(<span key={key++}>{tail}</span>);
  }

  return nodes.length > 0 ? nodes : [text.replace(/\*\*/g, "")];
}

/** Render LLM answer text with basic markdown (bold, bullets) — no raw ** visible. */
export function FormattedAnswer({
  text,
  className,
}: {
  text: string;
  className?: string;
}) {
  const lines = text.split("\n");

  return (
    <div className={cn("space-y-2", className)}>
      {lines.map((line, i) => {
        const trimmed = line.trim();
        if (!trimmed) return <div key={i} className="h-1" />;

        const bullet = trimmed.match(/^[\*\-•]\s+(.*)$/);
        if (bullet) {
          return (
            <div key={i} className="flex gap-2 pl-1">
              <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-cyan-400/80" />
              <p className="min-w-0 flex-1 leading-relaxed">
                <InlineMarkdown text={bullet[1]} />
              </p>
            </div>
          );
        }

        const numbered = trimmed.match(/^\d+[.)]\s+(.*)$/);
        if (numbered) {
          return (
            <p key={i} className="leading-relaxed pl-1">
              <InlineMarkdown text={trimmed} />
            </p>
          );
        }

        return (
          <p key={i} className="leading-relaxed">
            <InlineMarkdown text={trimmed} />
          </p>
        );
      })}
    </div>
  );
}
