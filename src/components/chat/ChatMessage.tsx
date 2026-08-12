import { ChatResultCard } from "@/components/chat/ChatResultCard";
import type { ChatMessage } from "@/lib/chat-types";
import { cn } from "@/lib/utils";
import { AlertCircle, Bot, Sparkles, User } from "lucide-react";

interface ChatMessageBubbleProps {
  message: ChatMessage;
  selected?: boolean;
  onSelect?: () => void;
  onFollowUp?: (prompt: string) => void;
}

export function ChatMessageBubble({
  message,
  selected,
  onSelect,
  onFollowUp,
}: ChatMessageBubbleProps) {
  const isUser = message.role === "user";
  const isError = message.role === "error";
  const isAssistantResult =
    message.role === "assistant" && !!message.response && !message.pending;

  return (
    <div className={cn("flex gap-3", isUser ? "flex-row-reverse" : "flex-row")}>
      <div
        className={cn(
          "flex h-9 w-9 shrink-0 items-center justify-center rounded-xl shadow-sm",
          isUser
            ? "bg-gradient-to-br from-primary to-primary/80 text-primary-foreground"
            : isError
              ? "bg-destructive/10 text-destructive ring-1 ring-destructive/20"
              : "bg-primary/10 text-primary ring-1 ring-primary/15",
        )}
      >
        {isUser ? (
          <User className="h-4 w-4" />
        ) : isError ? (
          <AlertCircle className="h-4 w-4" />
        ) : (
          <Bot className="h-4 w-4" />
        )}
      </div>

      <div
        className={cn(
          "min-w-0",
          isUser ? "max-w-[88%] md:max-w-[78%]" : "max-w-[92%] flex-1 md:max-w-[85%]",
        )}
      >
        {isAssistantResult && message.response ? (
          <ChatResultCard
            response={message.response}
            answer={message.content}
            selected={selected}
            onSelect={onSelect}
            onFollowUp={onFollowUp}
          />
        ) : (
          <div
            className={cn(
              "rounded-2xl px-4 py-3.5 text-sm leading-relaxed",
              isUser
                ? "bg-gradient-to-br from-primary to-primary/85 text-primary-foreground shadow-lg shadow-primary/15"
                : isError
                  ? "border border-destructive/25 bg-destructive/5 text-destructive"
                  : cn(
                      "border border-border/50 bg-card/80 text-card-foreground shadow-md shadow-black/5 backdrop-blur-sm",
                      "dark:bg-card/60 dark:shadow-black/15",
                    ),
            )}
          >
            {message.pending ? (
              <TypingIndicator />
            ) : (
              <p className="whitespace-pre-wrap">{message.content}</p>
            )}
          </div>
        )}

        <time className="mt-1.5 block text-[10px] text-muted-foreground/70">
          {new Date(message.timestamp).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
          })}
        </time>
      </div>
    </div>
  );
}

function TypingIndicator() {
  return (
    <div className="flex items-center gap-2 py-1">
      <div className="flex items-center gap-1">
        <span className="h-2 w-2 animate-bounce rounded-full bg-primary/60 [animation-delay:-0.3s]" />
        <span className="h-2 w-2 animate-bounce rounded-full bg-primary/60 [animation-delay:-0.15s]" />
        <span className="h-2 w-2 animate-bounce rounded-full bg-primary/60" />
      </div>
      <span className="text-xs text-muted-foreground">Analyzing your data…</span>
    </div>
  );
}

export function AssistantGreeting({ firstName }: { firstName: string }) {
  return (
    <div className="mb-8 flex flex-col items-start gap-3 md:mb-10">
      <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/20 to-primary/5 text-primary ring-1 ring-primary/20">
        <Sparkles className="h-6 w-6" />
      </div>
      <div>
        <h2 className="text-2xl font-semibold tracking-tight md:text-3xl">
          Hello, {firstName}
        </h2>
        <p className="mt-1.5 text-sm text-muted-foreground md:text-base">
          How can I help you with your data today?
        </p>
      </div>
    </div>
  );
}
