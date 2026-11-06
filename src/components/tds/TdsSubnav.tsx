import { Link, useRouterState } from "@tanstack/react-router";
import { cn } from "@/lib/utils";

const items = [
  { to: "/tds/liability" as const, label: "Liability" },
  { to: "/tds/import-jv" as const, label: "Import JV" },
  { to: "/tds/94a" as const, label: "94A" },
  { to: "/tds/94c" as const, label: "94C" },
  { to: "/tds/94h" as const, label: "94H" },
  { to: "/tds/94i" as const, label: "94I" },
  { to: "/tds/94j" as const, label: "94J" },
  { to: "/tds/94q" as const, label: "94Q" },
];

export function TdsSubnav() {
  const path = useRouterState({ select: (s) => s.location.pathname });

  return (
    <nav
      className="flex w-full flex-wrap gap-1 rounded-xl border border-border/80 bg-card/80 p-1 shadow-soft sm:w-fit"
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
