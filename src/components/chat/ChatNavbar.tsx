import type { AuthUser } from "@/lib/auth-context";
import type { ChatApiResponse } from "@/lib/chat-types";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Link } from "@tanstack/react-router";
import {
  ArrowLeft,
  Bot,
  ChevronDown,
  FlaskConical,
  LogOut,
  MessageSquarePlus,
  Moon,
  MoreVertical,
  PanelLeft,
  PanelRight,
  Sun,
  Trash2,
  User,
} from "lucide-react";

interface ChatNavbarProps {
  user: AuthUser | null;
  initials: string;
  dark: boolean;
  mockMode: boolean;
  mockToggleAllowed: boolean;
  hasMessages: boolean;
  loading: boolean;
  selectedResponse: ChatApiResponse | null;
  onOpenSidebar: () => void;
  onNewChat: () => void;
  onClearChat: () => void;
  onToggleTheme: () => void;
  onToggleMock: () => void;
  onOpenDetails: () => void;
  onLogout: () => void;
}

export function ChatNavbar({
  user,
  initials,
  dark,
  mockMode,
  mockToggleAllowed,
  hasMessages,
  loading,
  selectedResponse,
  onOpenSidebar,
  onNewChat,
  onClearChat,
  onToggleTheme,
  onToggleMock,
  onOpenDetails,
  onLogout,
}: ChatNavbarProps) {
  function handleClearWithConfirm() {
    if (!hasMessages && !loading) return;
    if (window.confirm("Clear this conversation? This cannot be undone.")) {
      onClearChat();
    }
  }

  return (
    <header className="chat-nav-header relative z-10 flex shrink-0 items-center justify-between gap-2 border-b border-primary/15 bg-background/80 px-3 py-2.5 backdrop-blur-xl sm:gap-3 sm:px-4 md:px-5 dark:border-primary/25">
      <div className="flex min-w-0 items-center gap-2 sm:gap-2.5">
        <Button
          type="button"
          variant="outline"
          size="icon"
          onClick={onOpenSidebar}
          className="h-9 w-9 shrink-0 rounded-xl border-border/60 bg-card/50 lg:hidden"
          aria-label="Open chat menu"
        >
          <PanelLeft className="h-4 w-4" />
        </Button>
        <Link
          to="/profile"
          className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border-2 border-border bg-card text-foreground/80 shadow-sm transition-all hover:border-primary/40 hover:bg-primary/5 hover:text-foreground hover:shadow-md dark:border-border/80 dark:bg-card/90 dark:hover:border-primary/50 dark:hover:bg-primary/10"
          aria-label="Back to profile"
        >
          <ArrowLeft className="h-4 w-4" strokeWidth={2.25} />
        </Link>
        <div className="flex min-w-0 items-center gap-2.5 sm:gap-3">
          <div className="relative shrink-0">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-primary/85 shadow-md shadow-primary/15 ring-1 ring-white/10 dark:shadow-primary/10 sm:h-10 sm:w-10 sm:rounded-2xl">
              <Bot className="h-4 w-4 text-primary-foreground sm:h-5 sm:w-5" strokeWidth={2.25} />
            </div>
            {mockMode && (
              <span
                className="absolute -bottom-0.5 -right-1 rounded-full bg-amber-500 px-1.5 py-px text-[8px] font-bold uppercase leading-none text-amber-950 ring-2 ring-background"
                title="Mock data mode — responses are simulated"
              >
                Mock
              </span>
            )}
          </div>
          <div className="min-w-0">
            <h1 className="assistant-title-shimmer truncate text-sm font-bold tracking-tight sm:text-base md:text-lg">
              Data Assistant
            </h1>
          </div>
        </div>
      </div>

      <div className="flex shrink-0 items-center gap-1.5 sm:gap-2">
        {user && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                className="flex max-w-[9rem] items-center gap-1.5 rounded-full border border-border/50 bg-card/60 py-1 pl-1 pr-2 text-left transition-colors hover:bg-card sm:max-w-[11rem] sm:pr-2.5 dark:bg-card/40"
                aria-label="Account menu"
              >
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/12 text-[10px] font-semibold text-primary dark:bg-primary/20">
                  {initials}
                </span>
                <span className="hidden min-w-0 truncate text-xs font-medium sm:inline">
                  {user.name.split(" ")[0]}
                </span>
                <ChevronDown className="hidden h-3.5 w-3.5 shrink-0 text-muted-foreground sm:block" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-52">
              <DropdownMenuLabel className="font-normal">
                <p className="truncate text-sm font-medium">{user.name}</p>
                <p className="truncate text-xs text-muted-foreground">{user.role}</p>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link to="/profile" className="cursor-pointer gap-2">
                  <User className="h-4 w-4" />
                  Profile
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={onToggleTheme} className="gap-2">
                {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
                {dark ? "Light mode" : "Dark mode"}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={onLogout}
                className="gap-2 text-destructive focus:text-destructive"
              >
                <LogOut className="h-4 w-4" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        <Button
          type="button"
          size="sm"
          onClick={onNewChat}
          disabled={!hasMessages && !loading}
          className="h-9 gap-1.5 rounded-xl bg-gradient-to-r from-primary to-primary/85 px-2.5 text-xs font-semibold shadow-md shadow-primary/20 hover:from-primary/90 hover:to-primary/75 sm:px-3 sm:text-sm"
        >
          <MessageSquarePlus className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
          <span className="hidden sm:inline">New chat</span>
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              type="button"
              variant="outline"
              size="icon"
              className="h-9 w-9 rounded-xl border-border/60 bg-card/50"
              aria-label="More actions"
            >
              <MoreVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-52">
            <DropdownMenuItem
              onClick={handleClearWithConfirm}
              disabled={!hasMessages && !loading}
              className="gap-2"
            >
              <Trash2 className="h-4 w-4" />
              Clear conversation
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={onOpenDetails}
              disabled={!selectedResponse}
              className="gap-2 xl:hidden"
            >
              <PanelRight className="h-4 w-4 text-violet-600 dark:text-violet-400" />
              Answer details
            </DropdownMenuItem>
            {mockToggleAllowed && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={onToggleMock} className="gap-2">
                  <FlaskConical className="h-4 w-4" />
                  {mockMode ? "Turn off mock data" : "Turn on mock data"}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
