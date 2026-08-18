import { Link, Outlet, useNavigate, useRouterState } from "@tanstack/react-router";
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
  PanelLeftClose,
  PanelLeftOpen,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";

export function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const router = useRouterState();
  const path = router.location.pathname;
  const [dark, setDark] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

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

  useEffect(() => {
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
    const timer = setTimeout(scrollToTop, 100);
    return () => clearTimeout(timer);
  }, [path]);

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("po-theme", next ? "dark" : "light");
  }

  function toggleSidebar() {
    setSidebarCollapsed((prev) => !prev);
  }

  const desktopNav = [
    { to: "/dashboard", icon: LayoutDashboard, label: "Dashboard", match: (p: string) => p.startsWith("/dashboard") },
    {
      to: "/sales-dashboard",
      icon: BarChart3,
      label: "Sales Dashboard",
      match: (p: string) => p.startsWith("/sales-dashboard"),
    },
    { to: "/pending", icon: ClipboardList, label: "Purchase Orders", match: (p: string) => p.startsWith("/pending") || p.startsWith("/po/") },
    { to: "/workorders", icon: FileText, label: "Work Orders", match: (p: string) => p.startsWith("/workorder") },
    { to: "/payments", icon: CreditCard, label: "Payment Approval", match: (p: string) => p.startsWith("/payment") },
    { to: "/indents", icon: FileText, label: "Indent Approval", match: (p: string) => p.startsWith("/indent") },
    {
      to: "/ledgers",
      icon: BookOpen,
      label: "Ledgers",
      match: (p: string) =>
        p.startsWith("/ledgers") ||
        p.startsWith("/ledger-summary") ||
        p.startsWith("/reconciliation"),
    },
    {
      to: "/export-bill-overdue",
      icon: FileWarning,
      label: "Export Bill Overdue",
      match: (p: string) => p.startsWith("/export-bill-overdue"),
    },
    { to: "/bom", icon: Layers, label: "BOM Report", match: (p: string) => p.startsWith("/bom") },
    { to: "/profile", icon: User, label: "Profile", match: (p: string) => p.startsWith("/profile") },
  ];

  const mobileNav = [
    { to: "/dashboard", icon: LayoutDashboard, label: "Dashboard", shortLabel: "Home", match: (p: string) => p.startsWith("/dashboard") },
    {
      to: "/sales-dashboard",
      icon: BarChart3,
      label: "Sales Dashboard",
      shortLabel: "Sales",
      match: (p: string) => p.startsWith("/sales-dashboard"),
    },
    { to: "/pending", icon: ClipboardList, label: "Purchase Orders", shortLabel: "POs", match: (p: string) => p.startsWith("/pending") || p.startsWith("/po/") },
    { to: "/workorders", icon: FileText, label: "Work Orders", shortLabel: "WOs", match: (p: string) => p.startsWith("/workorder") },
    { to: "/payments", icon: CreditCard, label: "Payment Approval", shortLabel: "Pay", match: (p: string) => p.startsWith("/payment") },
    { to: "/indents", icon: FileText, label: "Indent Approval", shortLabel: "Indent", match: (p: string) => p.startsWith("/indent") },
    {
      to: "/export-bill-overdue",
      icon: FileWarning,
      label: "Export Bill Overdue",
      shortLabel: "Export",
      match: (p: string) => p.startsWith("/export-bill-overdue"),
    },
    { to: "/profile", icon: User, label: "Profile", shortLabel: "Profile", match: (p: string) => p.startsWith("/profile") || p.startsWith("/bom") },
  ];

  return (
    <div className="min-h-screen overflow-x-hidden bg-background pb-20 md:pb-0">
      <header className="app-glass sticky top-0 z-30 border-b md:hidden">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3">
          <Link to="/dashboard" className="flex items-center gap-2.5">
            <img
              src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"}
              alt="HCP Logo"
              className={dark ? "h-11 w-11 rounded-xl shadow-lg" : "h-9 w-9 rounded-xl shadow-lg ring-1 ring-black/5"}
            />
            <div className="leading-tight">
              <div className="text-sm font-semibold">HCP</div>
              <div className="text-[11px] text-muted-foreground">Approvals Portal</div>
            </div>
          </Link>

          <div className="flex items-center gap-2">
            <button
              onClick={toggleTheme}
              className="rounded-md p-2 text-muted-foreground hover:bg-secondary hover:text-foreground"
              aria-label="Toggle theme"
            >
              {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </button>
            <div className="hidden text-right text-xs leading-tight sm:block">
              <div className="font-medium">{user?.name}</div>
              <div className="text-muted-foreground">{user?.role}</div>
            </div>
          </div>
        </div>
      </header>

      <div className="md:flex md:min-h-screen">
        {/* Desktop sidebar — collapsible */}
        <aside
          className={cn(
            "app-sidebar-3d fixed inset-y-0 left-0 z-40 hidden flex-col border-r transition-[width] duration-300 ease-in-out md:flex",
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

          <nav className="flex-1 space-y-1 overflow-x-hidden overflow-y-auto px-2 py-4">
            {desktopNav.map((n) => {
              const active = n.match(path);
              return (
                <Link
                  key={n.to}
                  to={n.to}
                  title={sidebarCollapsed ? n.label : undefined}
                  className={cn(
                    "flex items-center rounded-xl py-2.5 text-sm font-medium transition-all duration-300",
                    sidebarCollapsed ? "justify-center px-2" : "gap-3 px-3",
                    active
                      ? "nav-3d-active"
                      : "text-muted-foreground hover:-translate-y-px hover:bg-secondary/90 hover:text-foreground hover:shadow-md",
                  )}
                >
                  <n.icon className="h-4 w-4 shrink-0" />
                  <span
                    className={cn(
                      "truncate whitespace-nowrap transition-all duration-300 ease-in-out",
                      sidebarCollapsed ? "max-w-0 opacity-0" : "max-w-[12rem] opacity-100",
                    )}
                  >
                    {n.label}
                  </span>
                </Link>
              );
            })}
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
          )}
          id="main-content"
        >
          <div className="mx-auto w-full max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>

      {/* Mobile bottom nav — unchanged */}
      <nav className="app-glass fixed bottom-0 left-0 right-0 z-30 grid grid-cols-8 border-t md:hidden">
        {mobileNav.map((n) => {
          const active = n.match(path);
          return (
            <Link
              key={n.to}
              to={n.to}
              className={cn(
                "flex min-w-0 flex-col items-center justify-center gap-0.5 px-0.5 py-1.5 text-[9px] font-medium tracking-tight transition-all sm:text-[10px]",
                active ? "text-primary" : "text-muted-foreground",
              )}
            >
              <span
                className={cn(
                  "flex h-8 w-8 items-center justify-center rounded-xl transition-all",
                  active && "nav-3d-active",
                )}
              >
                <n.icon className={cn("h-4.5 w-4.5 shrink-0", active && "stroke-[2.5]")} />
              </span>
              <span className="w-full truncate text-center leading-tight">{n.shortLabel}</span>
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
