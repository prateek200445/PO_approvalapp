import { Link, useRouterState } from "@tanstack/react-router";
import { cn } from "@/lib/utils";

const items = [
  { to: "/tds/liability" as const, label: "Liability" },
  { to: "/tds/94a" as const, label: "94A" },
];

export function TdsSubnav() {
  const path = useRouterState({ select: (s) => s.location.pathname });

  return (
    <nav
      className="flex w-full gap-1 rounded-xl border border-border/80 bg-card/80 p-1 shadow-soft sm:w-fit"
      aria-label="TDS sections"
    >
      {items.map((item) => {
        const active = path === item.to || (item.to === "/tds/liability" && path === "/tds");
        return (
          <Link
            key={item.to}
            to={item.to}
            className={cn(
              "flex-1 rounded-lg px-3.5 py-2 text-center text-sm font-medium transition sm:flex-none",
              active
                ? "bg-primary text-primary-foreground shadow-sm"
                : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
            )}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
