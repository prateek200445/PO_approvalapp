import { Link, useRouterState } from "@tanstack/react-router";
import { cn } from "@/lib/utils";

export function IntercompanySubnav() {
  const path = useRouterState({ select: (s) => s.location.pathname });
  const onSettle = path.includes("/intercompany/settlement");
  const onTransactions = path.includes("/intercompany/transactions");

  return (
    <nav
      className="flex w-full gap-1 rounded-xl border border-border/80 bg-card/80 p-1 shadow-soft sm:w-fit"
      aria-label="Intercompany pages"
    >
      <Link
        to="/intercompany"
        className={cn(
          "flex-1 rounded-lg px-3.5 py-2 text-center text-sm font-medium transition sm:flex-none",
          !onSettle && !onTransactions
            ? "bg-primary text-primary-foreground shadow-sm"
            : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
        )}
      >
        Balances
      </Link>
      <Link
        to="/intercompany/transactions"
        className={cn(
          "flex-1 rounded-lg px-3.5 py-2 text-center text-sm font-medium transition sm:flex-none",
          onTransactions
            ? "bg-primary text-primary-foreground shadow-sm"
            : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
        )}
      >
        Transactions
      </Link>
      <Link
        to="/intercompany/settlement"
        className={cn(
          "flex-1 rounded-lg px-3.5 py-2 text-center text-sm font-medium transition sm:flex-none",
          onSettle
            ? "bg-primary text-primary-foreground shadow-sm"
            : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
        )}
      >
        How to settle
      </Link>
    </nav>
  );
}
