import { SuggestedPrompts } from "@/components/chat/SuggestedPrompts";
import { Button } from "@/components/ui/button";
import type { GroupedHistory } from "@/lib/chat-helpers";
import { cn } from "@/lib/utils";
import { Plus } from "lucide-react";

interface ChatSidebarProps {
  onNewChat: () => void;
  onSelectPrompt: (prompt: string) => void;
  groupedHistory: GroupedHistory[];
  userName?: string;
  userRole?: string;
  userInitials?: string;
  className?: string;
}

export function ChatSidebar({
  onNewChat,
  onSelectPrompt,
  groupedHistory,
  userName,
  userRole,
  userInitials,
  className,
}: ChatSidebarProps) {
  return (
    <div className={cn("flex h-full flex-col", className)}>
      <Button
        onClick={onNewChat}
        className="mb-5 h-10 w-full gap-2 rounded-xl bg-gradient-to-r from-primary to-primary/80 text-sm font-semibold shadow-md shadow-primary/20 hover:from-primary/90 hover:to-primary/70"
      >
        <Plus className="h-4 w-4" />
        New chat
      </Button>
      <div className="min-h-0 flex-1 overflow-y-auto pr-1">
        <SuggestedPrompts onSelect={onSelectPrompt} groupedHistory={groupedHistory} />
      </div>
      {userName && (
        <div className="mt-4 flex items-center gap-2.5 rounded-xl border border-border/50 bg-card/60 p-2.5">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-xs font-semibold text-primary">
            {userInitials ?? userName.slice(0, 2).toUpperCase()}
          </div>
          <div className="min-w-0">
            <p className="truncate text-xs font-medium">{userName}</p>
            {userRole && (
              <p className="truncate text-[10px] text-muted-foreground">{userRole}</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
