import { SUGGESTED_PROMPTS } from "@/lib/chat-types";
import type { GroupedHistory } from "@/lib/chat-helpers";
import { cn } from "@/lib/utils";
import { Clock, Sparkles } from "lucide-react";

interface SuggestedPromptsProps {
  onSelect: (prompt: string) => void;
  recentQuestions?: string[];
  groupedHistory?: GroupedHistory[];
  variant?: "sidebar" | "chips";
  className?: string;
}

export function SuggestedPrompts({
  onSelect,
  recentQuestions = [],
  groupedHistory,
  variant = "sidebar",
  className,
}: SuggestedPromptsProps) {
  if (variant === "chips") {
    const flat = SUGGESTED_PROMPTS.flatMap((g) => g.prompts).slice(0, 6);
    return (
      <div className={cn("flex gap-2 overflow-x-auto pb-1", className)}>
        {flat.map((prompt) => (
          <button
            key={prompt}
            type="button"
            onClick={() => onSelect(prompt)}
            className="shrink-0 rounded-full border border-border bg-card px-3 py-1.5 text-xs font-medium text-foreground transition-colors hover:border-primary/30 hover:bg-primary/5"
          >
            {prompt.length > 42 ? `${prompt.slice(0, 42)}…` : prompt}
          </button>
        ))}
      </div>
    );
  }

  const hasGrouped = groupedHistory && groupedHistory.length > 0;
  const flatRecent = !hasGrouped ? recentQuestions : [];

  return (
    <aside className={cn("flex flex-col gap-5", className)}>
      <div>
        <div className="mb-3 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
          <Sparkles className="h-3.5 w-3.5" />
          Suggested
        </div>
        <div className="space-y-4">
          {SUGGESTED_PROMPTS.map((group) => (
            <div key={group.category}>
              <p className="mb-1.5 text-[11px] font-medium text-muted-foreground">
                {group.category}
              </p>
              <div className="space-y-1">
                {group.prompts.map((prompt) => (
                  <button
                    key={prompt}
                    type="button"
                    onClick={() => onSelect(prompt)}
                    className="w-full rounded-lg border border-transparent px-2.5 py-2 text-left text-xs leading-snug text-foreground/90 transition-all hover:border-border/60 hover:bg-card/80 hover:text-foreground hover:shadow-sm"
                  >
                    {prompt}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>

      {hasGrouped && (
        <div>
          <div className="mb-3 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
            <Clock className="h-3.5 w-3.5" />
            History
          </div>
          <div className="space-y-4">
            {groupedHistory!.map((group) => (
              <div key={group.label}>
                <p className="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/80">
                  {group.label}
                </p>
                <div className="space-y-1">
                  {group.items.map((item) => (
                    <button
                      key={`${item.question}-${item.timestamp}`}
                      type="button"
                      onClick={() => onSelect(item.question)}
                      className="w-full rounded-lg border-l-2 border-transparent px-2.5 py-2 text-left text-xs leading-snug text-muted-foreground transition-all hover:border-primary/50 hover:bg-primary/5 hover:text-foreground"
                    >
                      {item.question.length > 58
                        ? `${item.question.slice(0, 58)}…`
                        : item.question}
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {!hasGrouped && flatRecent.length > 0 && (
        <div>
          <div className="mb-2 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
            <Clock className="h-3.5 w-3.5" />
            Recent
          </div>
          <div className="space-y-1">
            {flatRecent.map((q) => (
              <button
                key={q}
                type="button"
                onClick={() => onSelect(q)}
                className="w-full rounded-lg px-2.5 py-2 text-left text-xs leading-snug text-muted-foreground transition-colors hover:bg-secondary/80 hover:text-foreground"
              >
                {q.length > 60 ? `${q.slice(0, 60)}…` : q}
              </button>
            ))}
          </div>
        </div>
      )}
    </aside>
  );
}
