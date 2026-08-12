import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { ArrowUp, Loader2 } from "lucide-react";
import { useEffect, useRef } from "react";

interface ChatInputProps {
  value: string;
  onChange: (value: string) => void;
  onSend: () => void;
  disabled?: boolean;
  loading?: boolean;
  placeholder?: string;
}

export function ChatInput({
  value,
  onChange,
  onSend,
  disabled,
  loading,
  placeholder = "Ask about anything…",
}: ChatInputProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 160)}px`;
  }, [value]);

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (!disabled && !loading && value.trim()) onSend();
    }
  }

  return (
    <div className="shrink-0 border-t border-border/40 bg-gradient-to-t from-background via-background to-background/80 px-4 pb-4 pt-3 md:px-6 md:pb-5">
      <div className="mx-auto max-w-3xl">
        <div
          className={cn(
            "flex items-end gap-2 rounded-2xl border border-border/60 bg-card/90 p-2 shadow-xl shadow-black/5 backdrop-blur-md",
            "ring-1 ring-white/10 dark:shadow-black/20 dark:ring-white/5",
            "focus-within:border-primary/40 focus-within:ring-2 focus-within:ring-primary/20",
          )}
        >
          <textarea
            ref={textareaRef}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={disabled || loading}
            rows={1}
            placeholder={placeholder}
            className={cn(
              "max-h-40 min-h-[48px] flex-1 resize-none bg-transparent px-3 py-3 text-sm outline-none",
              "placeholder:text-muted-foreground disabled:opacity-60",
            )}
          />
          <Button
            type="button"
            size="icon"
            disabled={disabled || loading || !value.trim()}
            onClick={onSend}
            className="mb-0.5 h-11 w-11 shrink-0 rounded-xl bg-gradient-to-br from-primary to-primary/80 shadow-md shadow-primary/25 hover:from-primary/90 hover:to-primary/70"
            aria-label="Send message"
          >
            {loading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <ArrowUp className="h-4 w-4" />
            )}
          </Button>
        </div>
        <p className="mt-2 text-center text-[10px] text-muted-foreground/80">
          Enter to send · Shift+Enter for new line · Responses may take up to a minute
        </p>
      </div>
    </div>
  );
}
