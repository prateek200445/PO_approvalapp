import { ChatInput } from "@/components/chat/ChatInput";
import { ChatInsightsPanel } from "@/components/chat/ChatInsightsPanel";
import { AssistantGreeting, ChatMessageBubble } from "@/components/chat/ChatMessage";
import { ChatSidebar } from "@/components/chat/ChatSidebar";
import { SuggestedPrompts } from "@/components/chat/SuggestedPrompts";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { useAuth } from "@/lib/auth-context";
import { useTheme } from "@/hooks/use-theme";
import {
  clearChatSession,
  createMessageId,
  loadChatHistory,
  loadChatSession,
  pushChatHistory,
  saveChatSession,
  sendChatMessage,
} from "@/lib/chat-api";
import type { ChatHistoryItem } from "@/lib/chat-helpers";
import { groupHistoryByDay } from "@/lib/chat-helpers";
import type { ChatApiResponse, ChatMessage } from "@/lib/chat-types";
import { ArrowLeft, MessageSquarePlus, Moon, PanelLeft, PanelRight, Sun } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "@tanstack/react-router";
import { toast } from "sonner";

export function ChatAssistant() {
  const { user } = useAuth();
  const { dark, toggleTheme } = useTheme();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [insightsOpen, setInsightsOpen] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [history, setHistory] = useState<ChatHistoryItem[]>([]);
  const bottomRef = useRef<HTMLDivElement>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const hydrated = useRef(false);

  const firstName = user?.name?.split(" ")[0] ?? "there";
  const initials = (user?.name ?? "U").split(" ").map((s) => s[0]).slice(0, 2).join("");

  useEffect(() => {
    document.documentElement.classList.add("assistant-copilot");
    return () => document.documentElement.classList.remove("assistant-copilot");
  }, []);

  useEffect(() => {
    if (hydrated.current) return;
    hydrated.current = true;
    const loaded = loadChatSession();
    setMessages(loaded);
    let hist = loadChatHistory();
    // Seed history from current session if local history is empty (oldest → newest)
    if (hist.length === 0) {
      for (const m of loaded) {
        if (m.role === "user" && m.content.trim()) {
          hist = pushChatHistory(m.content);
        }
      }
    }
    setHistory(hist);
    const lastWithResponse = [...loaded].reverse().find((m) => m.response);
    if (lastWithResponse) setSelectedId(lastWithResponse.id);
  }, []);

  useEffect(() => {
    if (!hydrated.current) return;
    saveChatSession(messages);
  }, [messages]);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = "smooth") => {
    const el = scrollRef.current;
    if (!el) return;
    const run = () => {
      el.scrollTo({ top: el.scrollHeight, behavior });
    };
    // Result cards grow after paint — scroll twice so loading/answer stays above input
    requestAnimationFrame(() => {
      run();
      requestAnimationFrame(run);
    });
    window.setTimeout(run, 120);
  }, []);

  useEffect(() => {
    scrollToBottom(loading ? "auto" : "smooth");
  }, [messages, loading, scrollToBottom]);

  // Keep pinned to bottom while result cards (tables) grow after paint
  useEffect(() => {
    const el = scrollRef.current;
    const content = el?.firstElementChild;
    if (!el || !content) return;
    const ro = new ResizeObserver(() => {
      const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 160;
      if (nearBottom || loading) el.scrollTop = el.scrollHeight;
    });
    ro.observe(content);
    return () => ro.disconnect();
  }, [loading, messages.length]);

  const recentQuestions = useMemo(
    () => history.map((h) => h.question).slice(0, 6),
    [history],
  );

  const groupedHistory = useMemo(() => groupHistoryByDay(history), [history]);

  const selectedResponse: ChatApiResponse | null = useMemo(() => {
    if (!selectedId) return null;
    return messages.find((m) => m.id === selectedId)?.response ?? null;
  }, [messages, selectedId]);

  const submitQuestion = useCallback(
    async (question: string) => {
      const trimmed = question.trim();
      if (!trimmed || loading) return;

      const userMsg: ChatMessage = {
        id: createMessageId(),
        role: "user",
        content: trimmed,
        timestamp: Date.now(),
      };

      const pendingId = createMessageId();
      const pendingMsg: ChatMessage = {
        id: pendingId,
        role: "assistant",
        content: "",
        timestamp: Date.now(),
        pending: true,
      };

      setMessages((prev) => [...prev, userMsg, pendingMsg]);
      setInput("");
      setLoading(true);
      setHistory(pushChatHistory(trimmed));

      try {
        const response = await sendChatMessage(trimmed);
        setMessages((prev) =>
          prev.map((m) =>
            m.id === pendingId
              ? {
                  ...m,
                  pending: false,
                  content: response.answer || "No answer returned.",
                  response,
                }
              : m,
          ),
        );
        setSelectedId(pendingId);
      } catch (err) {
        const message = err instanceof Error ? err.message : "Something went wrong.";
        setMessages((prev) =>
          prev.map((m) =>
            m.id === pendingId
              ? {
                  id: pendingId,
                  role: "error",
                  content: message,
                  timestamp: Date.now(),
                  pending: false,
                }
              : m,
          ),
        );
        toast.error(message);
      } finally {
        setLoading(false);
      }
    },
    [loading],
  );

  function handleClear() {
    clearChatSession();
    setMessages([]);
    setInput("");
    setSelectedId(null);
    setInsightsOpen(false);
    toast.success("Chat cleared");
  }

  function selectMessage(id: string) {
    setSelectedId(id);
    setInsightsOpen(true);
  }

  function handlePromptSelect(prompt: string) {
    setSidebarOpen(false);
    void submitQuestion(prompt);
  }

  const hasMessages = messages.length > 0;

  return (
    <div className="relative flex h-dvh flex-col overflow-hidden bg-background">
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_90%_60%_at_50%_-10%,rgba(59,130,246,0.14),transparent_55%)] dark:bg-[radial-gradient(ellipse_90%_60%_at_50%_-10%,rgba(96,165,250,0.1),transparent_55%)]"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_50%_40%_at_100%_100%,rgba(59,130,246,0.06),transparent)] dark:bg-[radial-gradient(ellipse_50%_40%_at_100%_100%,rgba(96,165,250,0.05),transparent)]"
        aria-hidden
      />

      <header className="relative z-10 flex shrink-0 items-center justify-between gap-3 border-b border-border/40 bg-background/70 px-4 py-3 backdrop-blur-xl md:px-5">
        <div className="flex min-w-0 items-center gap-2 sm:gap-3">
          <Button
            type="button"
            variant="outline"
            size="icon"
            onClick={() => setSidebarOpen(true)}
            className="h-9 w-9 shrink-0 rounded-xl border-border/60 bg-card/50 lg:hidden"
            aria-label="Open menu"
          >
            <PanelLeft className="h-4 w-4" />
          </Button>
          <Link
            to="/profile"
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border border-border/60 bg-card/80 text-muted-foreground shadow-sm transition-colors hover:bg-secondary hover:text-foreground"
            aria-label="Back to profile"
          >
            <ArrowLeft className="h-4 w-4" />
          </Link>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="truncate text-base font-semibold tracking-tight md:text-lg">
                Data Assistant
              </h1>
              <Badge
                variant="secondary"
                className="shrink-0 border-primary/20 bg-primary/10 text-[10px] text-primary"
              >
                Beta
              </Badge>
            </div>
            <p className="hidden truncate text-xs text-muted-foreground sm:block">
              POs · stock · ledgers · production
            </p>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <div className="hidden items-center gap-2 rounded-xl border border-border/50 bg-card/50 px-2.5 py-1.5 sm:flex">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-primary/10 text-[10px] font-semibold text-primary">
              {initials}
            </div>
            <div className="hidden text-left leading-tight md:block">
              <p className="max-w-[120px] truncate text-xs font-medium">{user?.name}</p>
              <p className="text-[10px] text-muted-foreground">{user?.role}</p>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            size="icon"
            onClick={toggleTheme}
            className="h-9 w-9 rounded-xl border-border/60 bg-card/50"
            aria-label="Toggle theme"
          >
            {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setInsightsOpen(true)}
            disabled={!selectedResponse}
            className="gap-1.5 border-border/60 bg-card/50 backdrop-blur-sm xl:hidden"
          >
            <PanelRight className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Insights</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={handleClear}
            disabled={!hasMessages && !loading}
            className="gap-1.5 border-border/60 bg-card/50 backdrop-blur-sm"
          >
            <MessageSquarePlus className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Clear chat</span>
          </Button>
        </div>
      </header>

      <div className="relative flex min-h-0 flex-1">
        <aside className="hidden w-64 shrink-0 border-r border-border/40 bg-muted/15 p-4 backdrop-blur-sm lg:block xl:w-72">
          <ChatSidebar
            onNewChat={handleClear}
            onSelectPrompt={handlePromptSelect}
            groupedHistory={groupedHistory}
            userName={user?.name}
            userRole={user?.role}
            userInitials={initials}
          />
        </aside>

        <div className="relative flex min-w-0 flex-1 flex-col">
          <div
            ref={scrollRef}
            className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
          >
            <div className="mx-auto max-w-3xl space-y-6 px-4 py-6 pb-8 md:px-6 md:py-8">
              {!hasMessages && (
                <EmptyState
                  firstName={firstName}
                  onSelect={handlePromptSelect}
                />
              )}

              {messages.map((message) => (
                <ChatMessageBubble
                  key={message.id}
                  message={message}
                  selected={selectedId === message.id}
                  onSelect={() => selectMessage(message.id)}
                  onFollowUp={handlePromptSelect}
                />
              ))}
              <div ref={bottomRef} className="h-px w-full shrink-0" aria-hidden />
            </div>
          </div>

          <div className="shrink-0 border-t border-border/40 bg-muted/10 px-3 py-2 lg:hidden">
            <SuggestedPrompts
              variant="chips"
              onSelect={handlePromptSelect}
              recentQuestions={recentQuestions}
            />
          </div>

          <ChatInput
            value={input}
            onChange={setInput}
            onSend={() => void submitQuestion(input)}
            loading={loading}
            disabled={loading}
          />
        </div>

        {/* Desktop insights panel */}
        <div className="hidden xl:flex">
          <ChatInsightsPanel response={selectedResponse} />
        </div>
      </div>

      {/* Mobile sidebar sheet */}
      <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
        <SheetContent side="left" className="w-[min(100vw,20rem)] border-border/40 p-4">
          <SheetHeader className="sr-only">
            <SheetTitle>Menu</SheetTitle>
          </SheetHeader>
          <ChatSidebar
            onNewChat={() => {
              handleClear();
              setSidebarOpen(false);
            }}
            onSelectPrompt={handlePromptSelect}
            groupedHistory={groupedHistory}
            userName={user?.name}
            userRole={user?.role}
            userInitials={initials}
          />
        </SheetContent>
      </Sheet>

      {/* Mobile / tablet insights sheet */}
      <Sheet open={insightsOpen} onOpenChange={setInsightsOpen}>
        <SheetContent side="right" className="w-full max-w-sm border-border/40 p-0 sm:max-w-md">
          <SheetHeader className="sr-only">
            <SheetTitle>Insights</SheetTitle>
          </SheetHeader>
          <ChatInsightsPanel
            response={selectedResponse}
            onClose={() => setInsightsOpen(false)}
            className="w-full border-l-0"
          />
        </SheetContent>
      </Sheet>
    </div>
  );
}

function EmptyState({
  firstName,
  onSelect,
}: {
  firstName: string;
  onSelect: (prompt: string) => void;
}) {
  const examples = [
    "How many ledgers does Oswal Extrusion Limited have?",
    "Recent pending purchase orders",
    "Stock in hand at Oswal Extrusion Limited",
    "FIBC bag production for Oswal Extrusion Limited",
  ];

  return (
    <div className="pt-4 md:pt-8">
      <AssistantGreeting firstName={firstName} />

      <p className="mb-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
        Try asking
      </p>
      <div className="grid gap-2 sm:grid-cols-2">
        {examples.map((ex) => (
          <button
            key={ex}
            type="button"
            onClick={() => onSelect(ex)}
            className="group rounded-xl border border-border/50 bg-card/60 px-4 py-3.5 text-left text-xs leading-snug shadow-sm backdrop-blur-sm transition-all hover:border-primary/30 hover:bg-card hover:shadow-md dark:bg-card/40"
          >
            <span className="text-foreground/90 group-hover:text-foreground">{ex}</span>
          </button>
        ))}
      </div>
    </div>
  );
}
