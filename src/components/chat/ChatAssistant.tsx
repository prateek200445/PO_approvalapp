import { ChatInput } from "@/components/chat/ChatInput";
import { ChatInsightsPanel } from "@/components/chat/ChatInsightsPanel";
import { ChatNavbar } from "@/components/chat/ChatNavbar";
import { AssistantGreeting, ChatMessageBubble } from "@/components/chat/ChatMessage";
import { ChatSidebar } from "@/components/chat/ChatSidebar";
import { SuggestedPrompts } from "@/components/chat/SuggestedPrompts";
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
import {
  isChatMockEnabled,
  isMockToggleAllowed,
  toggleChatMockEnabled,
} from "@/lib/chat-mock-config";
import { getMockExamplePrompts, MOCK_SCENARIOS } from "@/lib/chat-mocks";
import type { ChatHistoryItem } from "@/lib/chat-helpers";
import { groupHistoryByDay } from "@/lib/chat-helpers";
import type { ChatApiResponse, ChatMessage } from "@/lib/chat-types";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

export function ChatAssistant() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { dark, toggleTheme } = useTheme();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [insightsOpen, setInsightsOpen] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [history, setHistory] = useState<ChatHistoryItem[]>([]);
  const [mockMode, setMockMode] = useState(() =>
    typeof window !== "undefined" ? isChatMockEnabled() : false,
  );
  const bottomRef = useRef<HTMLDivElement>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const hydrated = useRef(false);

  const firstName = user?.name?.split(" ")[0] ?? "there";
  const initials = (user?.name ?? "U").split(" ").map((s) => s[0]).slice(0, 2).join("");

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
    <div className="relative flex h-full min-h-0 flex-col overflow-hidden bg-background animate-in fade-in duration-200">
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_90%_60%_at_50%_-10%,rgba(59,130,246,0.14),transparent_55%)] dark:bg-[radial-gradient(ellipse_90%_60%_at_50%_-10%,rgba(96,165,250,0.1),transparent_55%)]"
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_50%_40%_at_100%_100%,rgba(59,130,246,0.06),transparent)] dark:bg-[radial-gradient(ellipse_50%_40%_at_100%_100%,rgba(96,165,250,0.05),transparent)]"
        aria-hidden
      />

      <ChatNavbar
        user={user}
        initials={initials}
        dark={dark}
        mockMode={mockMode}
        mockToggleAllowed={isMockToggleAllowed()}
        hasMessages={hasMessages}
        loading={loading}
        selectedResponse={selectedResponse}
        onOpenSidebar={() => setSidebarOpen(true)}
        onNewChat={handleClear}
        onClearChat={handleClear}
        onToggleTheme={toggleTheme}
        onToggleMock={() => {
          const next = toggleChatMockEnabled();
          setMockMode(next);
          toast.success(
            next ? "Mock mode on — no API key needed" : "Mock mode off — using live API",
          );
        }}
        onOpenDetails={() => setInsightsOpen(true)}
        onLogout={() => {
          logout();
          navigate({ to: "/" });
          toast.success("Signed out");
        }}
      />

      <div className="relative flex min-h-0 flex-1">
        <aside className="chat-nav-panel hidden w-64 shrink-0 border-r-2 border-primary/20 p-4 lg:block xl:w-72 dark:border-primary/30">
          <ChatSidebar
            onNewChat={handleClear}
            onSelectPrompt={handlePromptSelect}
            groupedHistory={groupedHistory}
          />
        </aside>

        <div className="relative flex min-w-0 flex-1 flex-col bg-background/80">
          <div
            ref={scrollRef}
            className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
          >
            <div className="mx-auto max-w-3xl space-y-6 px-4 py-6 pb-8 md:px-6 md:py-8">
              {!hasMessages && (
                <EmptyState
                  firstName={firstName}
                  onSelect={handlePromptSelect}
                  mockMode={mockMode}
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

      {/* Mobile sidebar sheet — chat menu */}
      <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
        <SheetContent
          side="left"
          className="chat-nav-panel w-[min(100vw,20rem)] border-r-2 border-primary/20 p-4 dark:border-primary/30"
        >
          <SheetHeader className="sr-only">
            <SheetTitle>Chat menu</SheetTitle>
          </SheetHeader>
          <ChatSidebar
            onNewChat={() => {
              handleClear();
              setSidebarOpen(false);
            }}
            onSelectPrompt={handlePromptSelect}
            groupedHistory={groupedHistory}
          />
        </SheetContent>
      </Sheet>

      {/* Mobile / tablet — answer details sheet */}
      <Sheet open={insightsOpen} onOpenChange={setInsightsOpen}>
        <SheetContent
          side="right"
          className="w-full max-w-sm border-l-2 border-violet-500/20 p-0 sm:max-w-md dark:border-violet-400/25"
        >
          <SheetHeader className="sr-only">
            <SheetTitle>Answer details</SheetTitle>
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
  mockMode,
}: {
  firstName: string;
  onSelect: (prompt: string) => void;
  mockMode: boolean;
}) {
  const examples = mockMode
    ? getMockExamplePrompts()
    : [
        "How many purchase orders are pending approval?",
        "What is stock in hand for item WIP00013 at Oswal Extrusion Limited?",
        "For Oswal Extrusion Limited show items with outward qty today",
        "How many ledgers does Oswal Extrusion Limited have?",
      ];

  return (
    <div className="pt-4 md:pt-8">
      <AssistantGreeting firstName={firstName} />

      {mockMode && (
        <div className="mb-6 rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-900 dark:text-amber-100">
          <p className="font-medium">Mock mode is on</p>
          <p className="mt-1 text-xs leading-relaxed text-amber-800/90 dark:text-amber-200/80">
            Responses are simulated — no API or LLM key required. Click any example below
            to preview card layouts ({MOCK_SCENARIOS.length} scenarios). Type{" "}
            <code className="rounded bg-black/10 px-1 py-0.5 font-mono text-[11px]">
              mock:empty-result
            </code>{" "}
            for a specific scenario.
          </p>
        </div>
      )}

      <p className="mb-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
        {mockMode ? "Mock examples" : "Try asking"}
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
