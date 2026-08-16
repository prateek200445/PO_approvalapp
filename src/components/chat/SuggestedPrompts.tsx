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
            title={prompt}
            className="shrink-0 rounded-full border border-border/60 bg-card/80 px-3.5 py-2 text-xs font-medium text-foreground shadow-sm transition-all hover:border-primary/35 hover:bg-primary/8 hover:shadow-md active:scale-[0.98]"
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
        <div className="mb-3 flex items-center gap-2 rounded-lg border border-primary/20 bg-primary/10 px-2.5 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-primary dark:border-sky-400/35 dark:bg-sky-500/15 dark:text-sky-200">
          <Sparkles className="h-3.5 w-3.5 shrink-0 dark:text-sky-300" />
          Suggested · tap to ask
        </div>
        <div className="space-y-4">
          {SUGGESTED_PROMPTS.map((group) => (
            <div key={group.category}>
              <p className="mb-2 pl-0.5 text-[11px] font-bold uppercase tracking-wide text-foreground/80 dark:text-foreground/75">
                {group.category}
              </p>
              <div className="flex flex-col gap-1.5">
                {group.prompts.map((prompt) => (
                  <button
                    key={prompt}
                    type="button"
                    onClick={() => onSelect(prompt)}
                    title={prompt}
                    className="group w-full rounded-xl border border-border/60 bg-card/90 px-3 py-2 text-left text-[11px] leading-snug text-foreground/85 shadow-sm transition-all hover:border-primary/40 hover:bg-primary/6 hover:text-foreground hover:shadow-md active:scale-[0.99] dark:bg-card/60 dark:hover:bg-primary/10"
                  >
                    <span className="line-clamp-2 group-hover:text-foreground">{prompt}</span>
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>

      {hasGrouped && (
        <div>
          <div className="mb-3 flex items-center gap-2 rounded-lg border border-border/60 bg-muted/50 px-2.5 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground dark:border-border/50 dark:bg-muted/30 dark:text-foreground/70">
            <Clock className="h-3.5 w-3.5 shrink-0" />
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
