import { SuggestedPrompts } from "@/components/chat/SuggestedPrompts";
import { Button } from "@/components/ui/button";
import type { GroupedHistory } from "@/lib/chat-helpers";
import { cn } from "@/lib/utils";
import { MessageSquarePlus, Sparkles } from "lucide-react";

interface ChatSidebarProps {
  onNewChat: () => void;
  onSelectPrompt: (prompt: string) => void;
  groupedHistory: GroupedHistory[];
  className?: string;
}

export function ChatSidebar({
  onNewChat,
  onSelectPrompt,
  groupedHistory,
  className,
}: ChatSidebarProps) {
  return (
    <div className={cn("flex h-full flex-col", className)}>
      <div className="mb-4 shrink-0 border-b border-primary/15 pb-4 dark:border-primary/25">
        <div className="mb-3 flex items-start gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary/15 text-primary ring-1 ring-primary/20 dark:bg-primary/20 dark:ring-primary/30">
            <Sparkles className="h-4 w-4" strokeWidth={2.25} />
          </div>
          <div className="min-w-0 pt-0.5">
            <p className="text-sm font-semibold tracking-tight text-foreground">
              Chat menu
            </p>
            <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
              Suggested questions &amp; your history
            </p>
          </div>
        </div>
        <Button
          onClick={onNewChat}
          className="h-10 w-full gap-2 rounded-xl bg-gradient-to-r from-primary to-primary/80 text-sm font-semibold shadow-md shadow-primary/20 hover:from-primary/90 hover:to-primary/70"
        >
          <MessageSquarePlus className="h-4 w-4" />
          New chat
        </Button>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto pr-0.5 [scrollbar-color:color-mix(in_oklch,var(--color-primary)_35%,transparent)_transparent] [scrollbar-width:thin]">
        <SuggestedPrompts onSelect={onSelectPrompt} groupedHistory={groupedHistory} />
      </div>
    </div>
  );
}
