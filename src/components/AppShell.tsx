import { Link, Outlet, useNavigate, useRouterState } from "@tanstack/react-router";
import { LayoutDashboard, ClipboardList, User, Factory, Moon, Sun, LogOut, FileText } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";

export function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const path = useRouterState({ select: (s) => s.location.pathname });
  const [dark, setDark] = useState(false);

  useEffect(() => {
    const stored = localStorage.getItem("po-theme");
    if (stored === "dark" || (!stored && window.matchMedia("(prefers-color-scheme: dark)").matches)) {
      document.documentElement.classList.add("dark");
      setDark(true);
    }
  }, []);

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("po-theme", next ? "dark" : "light");
  }

  const nav = [
  { to: "/dashboard", icon: LayoutDashboard, label: "Dashboard" },
  { to: "/pending", icon: ClipboardList, label: "Purchase Orders" },
  { to: "/workorders", icon: FileText, label: "Work Orders" },
  { to: "/indents", icon: FileText, label: "Indent Approval" },
  { to: "/profile", icon: User, label: "Profile" },
];

  return (
    <div className="min-h-screen bg-background pb-20 md:pb-0">
      {/* Top bar */}
      <header className="sticky top-0 z-30 border-b border-border bg-surface/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 md:px-6">
          <Link to="/dashboard" className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Factory className="h-5 w-5" />
            </div>
            <div className="leading-tight">
              <div className="text-sm font-semibold">HCP</div>
              <div className="text-[11px] text-muted-foreground">PO Approval Portal</div>
            </div>
          </Link>

          {/* Desktop nav */}
          <nav className="hidden items-center gap-1 md:flex">
            {nav.map((n) => {
              const active = path.startsWith(n.to);
              return (
                <Link
                  key={n.to}
                  to={n.to}
                  className={cn(
                    "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    active ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  <n.icon className="h-4 w-4" />
                  {n.label}
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

      <main className="mx-auto max-w-7xl px-4 py-5 md:px-6 md:py-8">
        <Outlet />
      </main>

      {/* Mobile bottom nav */}
      <nav className="fixed bottom-0 left-0 right-0 z-30 grid grid-cols-4 border-t border-border bg-surface/95 backdrop-blur-md md:hidden">
        {nav.map((n) => {
          const active = path.startsWith(n.to);
          return (
            <Link
              key={n.to}
              to={n.to}
              className={cn(
                "flex flex-col items-center gap-1 py-2.5 text-[11px] font-medium transition-colors",
                active ? "text-primary" : "text-muted-foreground",
              )}
            >
              <n.icon className={cn("h-5 w-5", active && "stroke-[2.5]")} />
              {n.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
