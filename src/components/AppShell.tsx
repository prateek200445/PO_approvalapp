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

  useEffect(() => {
    const stored = localStorage.getItem("po-theme");
    if (stored === "dark" || (!stored && window.matchMedia("(prefers-color-scheme: dark)").matches)) {
      document.documentElement.classList.add("dark");
      setDark(true);
    }
  }, []);

  // Scroll to top on every navigation including first load
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
      match: (p: string) => p.startsWith("/ledgers") || p.startsWith("/ledger-summary") || p.startsWith("/reconciliation"),
    },
    { to: "/profile", icon: User, label: "Profile", match: (p: string) => p.startsWith("/profile") || p.startsWith("/bom") },
  ];

  // Mobile bottom nav includes Sales; 7 items → grid-cols-7
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
    { to: "/profile", icon: User, label: "Profile", shortLabel: "Profile", match: (p: string) => p.startsWith("/profile") || p.startsWith("/bom") },
  ];

  return (
    <div className="min-h-screen bg-background pb-20 md:pb-0">
      <header className="sticky top-0 z-30 border-b border-border bg-surface/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 md:px-6">
          <Link to="/dashboard" className="flex items-center gap-2.5">
            <img
              src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"}
              alt="HCP Logo"
              className={dark ? "h-11 w-11 rounded-lg" : "h-9 w-9 rounded-lg"}
            />
            <div className="leading-tight">
              <div className="text-sm font-semibold">HCP</div>
              <div className="text-[11px] text-muted-foreground">Approvals Portal</div>
            </div>
          </Link>

          <nav className="hidden items-center gap-1 lg:flex">
            {desktopNav.map((n) => {
              const active = n.match(path);
              return (
                <Link
                  key={n.to}
                  to={n.to}
                  className={cn(
                    "flex items-center gap-2 rounded-md px-2.5 py-2 text-sm font-medium transition-colors xl:px-3",
                    active
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  <n.icon className="h-4 w-4" />
                  <span className="hidden lg:inline">{n.label}</span>
                </Link>
              );
            })}
          </nav>

          <nav className="hidden items-center gap-1 md:flex lg:hidden">
            {desktopNav.map((n) => {
              const active = n.match(path);
              return (
                <Link
                  key={n.to}
                  to={n.to}
                  title={n.label}
                  className={cn(
                    "flex items-center justify-center rounded-md p-2 text-sm font-medium transition-colors",
                    active
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  <n.icon className="h-4 w-4" />
                </Link>
              );
            })}
          </nav>

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
            <button
              onClick={() => {
                logout();
                navigate({ to: "/" });
              }}
              className="hidden rounded-md p-2 text-muted-foreground hover:bg-destructive/10 hover:text-destructive md:inline-flex"
              aria-label="Logout"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        </div>
      </header>

      <main className="w-full px-4 py-5 md:mx-auto md:max-w-7xl md:px-6 md:py-8" id="main-content">
        <Outlet />
      </main>

      <nav className="fixed bottom-0 left-0 right-0 z-30 grid grid-cols-7 border-t border-border bg-surface/95 backdrop-blur-md md:hidden">
        {mobileNav.map((n) => {
          const active = n.match(path);
          return (
            <Link
              key={n.to}
              to={n.to}
              className={cn(
                "flex min-w-0 flex-col items-center justify-center gap-0.5 px-0.5 py-2 text-[9px] font-medium tracking-tight transition-colors sm:text-[10px]",
                active ? "text-primary" : "text-muted-foreground",
              )}
            >
              <n.icon className={cn("h-5 w-5 shrink-0", active && "stroke-[2.5]")} />
              <span className="w-full truncate text-center leading-tight">{n.shortLabel}</span>
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
