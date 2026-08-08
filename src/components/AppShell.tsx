import { Link, Outlet, useNavigate, useRouterState } from "@tanstack/react-router";
import { LayoutDashboard, ClipboardList, User, Factory, Moon, Sun, LogOut, FileText, CreditCard  } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEffect, useState, useRef } from "react";
import { cn } from "@/lib/utils";

export function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const router = useRouterState();
  const path = router.location.pathname;
  const [dark, setDark] = useState(false);
  const prevPathRef = useRef<string>("");

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
      window.scrollTo({ top: 0, behavior: 'instant' });
      document.documentElement.scrollTop = 0;
      document.body.scrollTop = 0;
      
      const mainContent = document.getElementById('main-content');
      if (mainContent) {
        mainContent.scrollTop = 0;
      }
    };

    // Scroll immediately
    scrollToTop();
    
    // Also scroll after a brief delay to catch late-rendering content
    const timer = setTimeout(scrollToTop, 100);
    
    return () => clearTimeout(timer);
  }, [path]);

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("po-theme", next ? "dark" : "light");
  }

  const nav = [
  { to: "/dashboard", icon: LayoutDashboard, label: "Dashboard", shortLabel: "Dashboard" },

  { to: "/pending", icon: ClipboardList, label: "Purchase Orders", shortLabel: "POs" },

  { to: "/workorders", icon: FileText, label: "Work Orders", shortLabel: "Work Orders" },

  { to: "/payments", icon: CreditCard, label: "Payment Approval", shortLabel: "Payments" },

  { to: "/indents", icon: FileText, label: "Indent Approval", shortLabel: "Indents" },

  { to: "/profile", icon: User, label: "Profile", shortLabel: "Profile" },
];

  return (
    <div className="min-h-screen bg-background pb-20 md:pb-0">
      {/* Top bar */}
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

      <main className="w-full px-4 py-5 md:mx-auto md:max-w-7xl md:px-6 md:py-8" id="main-content">
        <Outlet />
      </main>

      {/* Mobile bottom nav */}
      <nav className="fixed bottom-0 left-0 right-0 z-30 grid grid-cols-6 border-t border-border bg-surface/95 backdrop-blur-md md:hidden">
        {nav.map((n) => {
          const active = path.startsWith(n.to);
          return (
            <Link
              key={n.to}
              to={n.to}
              className={cn(
                "flex flex-col items-center justify-center gap-1 py-2 text-[10px] font-medium tracking-tight transition-colors",
                active ? "text-primary" : "text-muted-foreground",
              )}
            >
              <n.icon className={cn("h-5 w-5", active && "stroke-[2.5]")} />
              <span className="truncate w-full text-center px-0.5">{n.shortLabel}</span>
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
