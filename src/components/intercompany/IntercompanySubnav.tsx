import { Link, useRouterState } from "@tanstack/react-router";
import { cn } from "@/lib/utils";

export function IntercompanySubnav() {
  const path = useRouterState({ select: (s) => s.location.pathname });
  const onSettle = path.includes("/intercompany/settlement");

  return (
    <nav className="flex w-fit gap-0.5 rounded-lg border border-border bg-muted/50 p-0.5" aria-label="Intercompany pages">
      <Link
        to="/intercompany"
        className={cn(
          "rounded-md px-3 py-1.5 text-sm font-medium",
          !onSettle ? "bg-card text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
        )}
      >
        Balances
      </Link>
      <Link
        to="/intercompany/settlement"
        className={cn(
          "rounded-md px-3 py-1.5 text-sm font-medium",
          onSettle ? "bg-card text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground",
        )}
      >
        How to settle
      </Link>
    </nav>
  );
}
