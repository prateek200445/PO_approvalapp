import { Link, Outlet, useNavigate, useRouter, useRouterState } from "@tanstack/react-router";
import {
  LayoutDashboard,
  ClipboardList,
  User,
  Moon,
  Sun,
  LogOut,
  FileText,
  CreditCard,
  BookOpen,
  BarChart3,
  FileWarning,
  Layers,
  Factory,
  Building2,
  PanelLeftClose,
  PanelLeftOpen,
  Hammer,
  Menu,
  Search,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEffect, useLayoutEffect, useState, type ComponentType } from "react";
import { cn } from "@/lib/utils";
import { formatBadgeCount, useApprovalInbox, type InboxKind } from "@/hooks/useApprovalInbox";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { GlobalCommandPalette, SearchTrigger } from "@/components/GlobalCommandPalette";
import { AssistantShellSkeleton } from "@/components/chat/AssistantShellSkeleton";

type AppPath =
  | "/dashboard"
  | "/pending"
  | "/workorders"
  | "/payments"
  | "/advance-payments"
  | "/indents"
  | "/sales-dashboard"
  | "/intercompany"
  | "/ledgers"
  | "/export-bill-overdue"
  | "/daily-production"
  | "/bom"
  | "/profile"
  | "/assistant";

type NavItem = {
  to: AppPath;
  icon: ComponentType<{ className?: string }>;
  label: string;
  shortLabel?: string;
  match: (p: string) => boolean;
  badge?: InboxKind;
};

export function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const router = useRouter();
  const routerState = useRouterState();
  const path = routerState.location.pathname;
  const fullScreenReport = path.includes("export-bill-overdue");
  const isCopilot = path.startsWith("/assistant");
  const assistantLoading = useRouterState({
    select: (s) =>
      s.location.pathname.startsWith("/assistant") && s.status !== "idle",
  });
  const [dark, setDark] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const inbox = useApprovalInbox(user?.username);

  useEffect(() => {
    void router.preloadRoute({ to: "/assistant" });
  }, [router]);

  useEffect(() => {
    const stored = localStorage.getItem("po-theme");
    if (stored === "dark" || (!stored && window.matchMedia("(prefers-color-scheme: dark)").matches)) {
      document.documentElement.classList.add("dark");
      setDark(true);
    }
  }, []);

  useEffect(() => {
    const stored = localStorage.getItem("po-sidebar-collapsed");
    if (stored === "true") setSidebarCollapsed(true);
  }, []);

  useEffect(() => {
    localStorage.setItem("po-sidebar-collapsed", sidebarCollapsed ? "true" : "false");
  }, [sidebarCollapsed]);

  useLayoutEffect(() => {
    document.documentElement.classList.toggle("assistant-copilot", isCopilot);
    return () => document.documentElement.classList.remove("assistant-copilot");
  }, [isCopilot]);

  useEffect(() => {
    if (isCopilot) return;

    const scrollToTop = () => {
      window.scrollTo({ top: 0, behavior: "instant" });
      document.documentElement.scrollTop = 0;
      document.body.scrollTop = 0;

      const mainContent = document.getElementById("main-content");
      if (mainContent) {
        mainContent.scrollTop = 0;
      }
    };

    scrollToTop();
    setMenuOpen(false);
    const timer = setTimeout(scrollToTop, 100);
    return () => clearTimeout(timer);
  }, [path, isCopilot]);

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("po-theme", next ? "dark" : "light");
  }

  function toggleSidebar() {
    setSidebarCollapsed((prev) => !prev);
  }

  const approvalNav: NavItem[] = [
    {
      to: "/pending",
      icon: ClipboardList,
      label: "Purchase Orders",
      shortLabel: "POs",
      match: (p) => p.startsWith("/pending") || p.startsWith("/po/"),
      badge: "po",
    },
    {
      to: "/workorders",
      icon: Hammer,
      label: "Work Orders",
      shortLabel: "WOs",
      match: (p) => p.startsWith("/workorder"),
      badge: "workorder",
    },
    {
      to: "/payments",
      icon: CreditCard,
      label: "Payment Approval",
      shortLabel: "Pay",
      match: (p) => p.startsWith("/payment"),
      badge: "payment",
    },
    {
      to: "/advance-payments",
      icon: CreditCard,
      label: "Bill Payment Entry",
      shortLabel: "Bill Pay",
      match: (p) => p.startsWith("/advance-payment") || p.startsWith("/advance-payments"),
      badge: "advancePayment",
    },
    {
      to: "/indents",
      icon: FileText,
      label: "Indent Approval",
      shortLabel: "Indent",
      match: (p) => p.startsWith("/indent"),
      badge: "indent",
    },
  ];

  const reportNav: NavItem[] = [
    {
      to: "/sales-dashboard",
      icon: BarChart3,
      label: "Sales Dashboard",
      match: (p) => p.startsWith("/sales-dashboard"),
    },
    {
      to: "/intercompany",
      icon: Building2,
      label: "Intercompany",
      match: (p) => p.startsWith("/intercompany"),
    },
    {
      to: "/ledgers",
      icon: BookOpen,
      label: "Ledgers",
      match: (p) =>
        p.startsWith("/ledgers") ||
        p.startsWith("/ledger-summary") ||
        p.startsWith("/reconciliation") ||
        p.startsWith("/pnl"),
    },
    {
      to: "/export-bill-overdue",
      icon: FileWarning,
      label: "Export Bill Overdue",
      match: (p) => p.startsWith("/export-bill-overdue"),
    },
    {
      to: "/daily-production",
      icon: Factory,
      label: "Daily Production",
      match: (p) => p.startsWith("/daily-production"),
    },
    { to: "/bom", icon: Layers, label: "BOM Report", match: (p) => p.startsWith("/bom") },
  ];

  const accountNav: NavItem[] = [
    { to: "/profile", icon: User, label: "Profile", match: (p) => p.startsWith("/profile") },
  ];

  const desktopGroups: { title: string; items: NavItem[] }[] = [
    {
      title: "Home",
      items: [
        {
          to: "/dashboard",
          icon: LayoutDashboard,
          label: "Dashboard",
          match: (p) => p.startsWith("/dashboard"),
        },
      ],
    },
    { title: "Approvals", items: approvalNav },
    { title: "Reports", items: reportNav },
    { title: "Account", items: accountNav },
  ];

  function badgeFor(kind?: InboxKind) {
    if (!kind) return null;
    return formatBadgeCount(inbox.counts[kind]);
  }

  function NavLink({
    item,
    collapsed,
  }: {
    item: NavItem;
    collapsed?: boolean;
  }) {
    const active = item.match(path);
    const badge = badgeFor(item.badge);
    return (
      <Link
        to={item.to}
        title={collapsed ? item.label : undefined}
        onClick={() => setMenuOpen(false)}
        className={cn(
          "flex items-center rounded-xl py-2.5 text-sm font-medium transition-all duration-300",
          collapsed ? "justify-center px-2" : "gap-3 px-3",
          active
            ? "nav-3d-active"
            : "text-muted-foreground hover:-translate-y-px hover:bg-secondary/90 hover:text-foreground hover:shadow-md",
        )}
      >
        <span className="relative shrink-0">
          <item.icon className="h-4 w-4" />
          {collapsed && badge && (
            <span className="absolute -right-2 -top-2 flex h-4 min-w-4 items-center justify-center rounded-full bg-warning px-0.5 text-[9px] font-bold text-warning-foreground">
              {badge}
            </span>
          )}
        </span>
        <span
          className={cn(
            "truncate whitespace-nowrap transition-all duration-300 ease-in-out",
            collapsed ? "max-w-0 opacity-0" : "max-w-[12rem] opacity-100",
          )}
        >
          {item.label}
        </span>
        {!collapsed && badge && (
          <span
            className={cn(
              "ml-auto rounded-full px-1.5 py-0.5 text-[10px] font-semibold tabular-nums",
              active ? "bg-white/20 text-primary-foreground" : "bg-warning text-warning-foreground",
            )}
          >
            {badge}
          </span>
        )}
      </Link>
    );
  }

  if (isCopilot) {
    return (
      <div className="h-dvh overflow-hidden bg-background">
        <main className="h-full w-full overflow-hidden" id="main-content">
          {assistantLoading ? <AssistantShellSkeleton /> : <Outlet />}
        </main>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "min-h-screen overflow-x-hidden bg-background",
        fullScreenReport && "flex h-dvh max-h-dvh flex-col overflow-hidden",
      )}
    >
      <header className="app-glass sticky top-0 z-30 border-b pt-[env(safe-area-inset-top,0px)] md:hidden">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-3 py-3">
          <div className="flex min-w-0 items-center gap-2">
            <button
              type="button"
              onClick={() => setMenuOpen(true)}
              className="relative rounded-md p-2 text-muted-foreground hover:bg-secondary hover:text-foreground"
              aria-label="Open menu"
            >
              <Menu className="h-5 w-5" />
              {inbox.counts.total > 0 && (
                <span className="absolute right-0.5 top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-warning px-0.5 text-[9px] font-bold text-warning-foreground">
                  {formatBadgeCount(inbox.counts.total)}
                </span>
              )}
            </button>
            <Link to="/dashboard" className="flex min-w-0 items-center gap-2.5">
              <img
                src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"}
                alt="HCP Logo"
                className={dark ? "h-10 w-10 rounded-xl shadow-lg" : "h-9 w-9 rounded-xl shadow-lg ring-1 ring-black/5"}
              />
              <div className="leading-tight">
                <div className="text-sm font-semibold">HCP</div>
                <div className="text-[11px] text-muted-foreground">Approvals Portal</div>
              </div>
            </Link>
          </div>

          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => window.dispatchEvent(new Event("po-open-search"))}
              className="rounded-md p-2 text-muted-foreground hover:bg-secondary hover:text-foreground"
              aria-label="Search pending"
            >
              <Search className="h-4 w-4" />
            </button>
            <button
              onClick={toggleTheme}
              className="rounded-md p-2 text-muted-foreground hover:bg-secondary hover:text-foreground"
              aria-label="Toggle theme"
            >
              {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </button>
          </div>
        </div>
      </header>

      <div
        className={cn(
          "md:flex md:min-h-screen",
          fullScreenReport && "flex min-h-0 flex-1 flex-col overflow-hidden md:flex-row",
        )}
      >
        <aside
          className={cn(
            "app-sidebar-3d fixed inset-y-0 left-0 z-40 hidden flex-col border-r pt-[env(safe-area-inset-top,0px)] pb-[env(safe-area-inset-bottom,0px)] pl-[env(safe-area-inset-left,0px)] transition-[width] duration-300 ease-in-out md:flex",
            sidebarCollapsed ? "w-[4.5rem]" : "w-64",
          )}
        >
          <div
            className={cn(
              "flex items-center border-b border-border py-4 transition-all duration-300",
              sidebarCollapsed ? "justify-center px-2" : "gap-3 px-4",
            )}
          >
            <button
              type="button"
              onClick={toggleSidebar}
              className="group relative flex h-10 w-10 shrink-0 items-center justify-center rounded-lg outline-none ring-primary/40 focus-visible:ring-2"
              aria-label={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
              title={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            >
              <img
                src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"}
                alt="HCP"
                className="h-9 w-9 rounded-xl object-cover shadow-md ring-1 ring-black/5 transition-all duration-300 ease-in-out group-hover:scale-90 group-hover:opacity-0"
              />
              <span className="pointer-events-none absolute inset-0 flex items-center justify-center">
                {sidebarCollapsed ? (
                  <PanelLeftOpen className="h-5 w-5 text-primary opacity-0 transition-all duration-300 ease-in-out group-hover:scale-100 group-hover:opacity-100" />
                ) : (
                  <PanelLeftClose className="h-5 w-5 text-primary opacity-0 transition-all duration-300 ease-in-out group-hover:scale-100 group-hover:opacity-100" />
                )}
              </span>
            </button>

            <div
              className={cn(
                "min-w-0 overflow-hidden leading-tight transition-all duration-300 ease-in-out",
                sidebarCollapsed ? "max-w-0 opacity-0" : "max-w-[12rem] opacity-100",
              )}
            >
              <div className="truncate text-sm font-semibold">HCP</div>
              <div className="truncate text-[11px] text-muted-foreground">Approvals Portal</div>
            </div>
          </div>

          <nav className="flex-1 space-y-4 overflow-x-hidden overflow-y-auto px-2 py-4">
            <div className={cn(sidebarCollapsed ? "px-0" : "px-1")}>
              <SearchTrigger collapsed={sidebarCollapsed} />
            </div>
            {desktopGroups.map((group) => (
              <div key={group.title} className="space-y-1">
                <div
                  className={cn(
                    "px-3 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground transition-all",
                    sidebarCollapsed ? "sr-only" : "pb-1",
                  )}
                >
                  {group.title}
                </div>
                {group.items.map((item) => (
                  <NavLink key={item.to} item={item} collapsed={sidebarCollapsed} />
                ))}
              </div>
            ))}
          </nav>

          <div
            className={cn(
              "space-y-3 border-t border-border py-4 transition-all duration-300",
              sidebarCollapsed ? "px-2" : "px-4",
            )}
          >
            <div
              className={cn(
                "min-w-0 overflow-hidden transition-all duration-300 ease-in-out",
                sidebarCollapsed ? "max-h-0 opacity-0" : "max-h-16 opacity-100",
              )}
            >
              <div className="truncate text-sm font-medium">{user?.name}</div>
              <div className="truncate text-xs text-muted-foreground">{user?.role}</div>
            </div>
            <div className={cn("flex gap-2", sidebarCollapsed ? "flex-col items-center" : "items-center")}>
              <button
                onClick={toggleTheme}
                title={dark ? "Light mode" : "Dark mode"}
                className={cn(
                  "flex items-center justify-center rounded-xl border border-border bg-background text-sm text-muted-foreground shadow-sm transition hover:-translate-y-px hover:bg-secondary hover:text-foreground hover:shadow-md",
                  sidebarCollapsed ? "h-10 w-10 p-0" : "flex-1 gap-2 px-3 py-2",
                )}
                aria-label="Toggle theme"
              >
                {dark ? <Sun className="h-4 w-4 shrink-0" /> : <Moon className="h-4 w-4 shrink-0" />}
                <span
                  className={cn(
                    "transition-all duration-300 ease-in-out",
                    sidebarCollapsed ? "sr-only max-w-0 opacity-0" : "opacity-100",
                  )}
                >
                  {dark ? "Light" : "Dark"}
                </span>
              </button>
              <button
                onClick={() => {
                  logout();
                  navigate({ to: "/" });
                }}
                title="Logout"
                className="rounded-xl border border-border p-2 text-muted-foreground shadow-sm transition hover:-translate-y-px hover:bg-destructive/10 hover:text-destructive hover:shadow-md"
                aria-label="Logout"
              >
                <LogOut className="h-4 w-4" />
              </button>
            </div>
          </div>
        </aside>

        <main
          className={cn(
            "w-full min-w-0 max-w-full overflow-x-hidden px-4 py-5 transition-[margin-left] duration-300 ease-in-out md:px-6 md:py-8 lg:px-8",
            sidebarCollapsed ? "md:ml-[4.5rem]" : "md:ml-64",
            fullScreenReport &&
              "flex min-h-0 flex-1 flex-col overflow-hidden px-2 py-2 sm:px-3 sm:py-2.5 md:h-auto md:px-3 md:py-3 lg:px-4",
          )}
          id="main-content"
        >
          <div
            className={cn(
              "mx-auto w-full max-w-7xl",
              fullScreenReport && "flex min-h-0 max-w-none flex-1 flex-col",
            )}
          >
            <Outlet />
          </div>
        </main>
      </div>

      <Sheet open={menuOpen} onOpenChange={setMenuOpen}>
        <SheetContent
          side="left"
          className="app-sidebar-3d flex w-[min(18.5rem,88vw)] flex-col gap-0 p-0 pt-[env(safe-area-inset-top,0px)] sm:max-w-none md:hidden"
        >
          <SheetHeader className="border-b border-border px-4 py-4 pr-12 text-left">
            <SheetTitle className="flex items-center gap-3 text-base font-semibold">
              <img
                src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"}
                alt=""
                className={dark ? "h-9 w-9 rounded-xl shadow-md" : "h-9 w-9 rounded-xl shadow-md ring-1 ring-black/5"}
              />
              <span className="leading-tight">
                Menu
                {inbox.counts.total > 0 && (
                  <span className="ml-2 align-middle text-[11px] font-semibold text-muted-foreground">
                    {formatBadgeCount(inbox.counts.total)} pending
                  </span>
                )}
              </span>
            </SheetTitle>
          </SheetHeader>

          <nav className="flex-1 space-y-4 overflow-y-auto px-2 py-4">
            <div className="px-1" onClick={() => setMenuOpen(false)}>
              <SearchTrigger />
            </div>
            {desktopGroups.map((group) => (
              <div key={group.title} className="space-y-1">
                <div className="px-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                  {group.title}
                </div>
                {group.items.map((item) => (
                  <NavLink key={item.to} item={item} />
                ))}
              </div>
            ))}
          </nav>

          <div className="space-y-3 border-t border-border px-4 py-4 pb-[max(1rem,env(safe-area-inset-bottom,0px))]">
            <div className="min-w-0">
              <div className="truncate text-sm font-medium">{user?.name}</div>
              <div className="truncate text-xs text-muted-foreground">{user?.role}</div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={toggleTheme}
                className="flex flex-1 items-center justify-center gap-2 rounded-xl border border-border bg-background px-3 py-2 text-sm text-muted-foreground shadow-sm"
                aria-label="Toggle theme"
              >
                {dark ? <Sun className="h-4 w-4 shrink-0" /> : <Moon className="h-4 w-4 shrink-0" />}
                {dark ? "Light" : "Dark"}
              </button>
              <button
                onClick={() => {
                  setMenuOpen(false);
                  logout();
                  navigate({ to: "/" });
                }}
                className="rounded-xl border border-border p-2 text-muted-foreground shadow-sm"
                aria-label="Logout"
              >
                <LogOut className="h-4 w-4" />
              </button>
            </div>
          </div>
        </SheetContent>
      </Sheet>
      <GlobalCommandPalette />
    </div>
  );
}
