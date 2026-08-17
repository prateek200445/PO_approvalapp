import { Link } from "@tanstack/react-router";
import { ArrowLeft, CalendarRange } from "lucide-react";
import { cn } from "@/lib/utils";

export function PlanningPageShell({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("min-h-full bg-gradient-to-b from-sky-500/[0.05] via-background to-background", className)}>
      <div className="mx-auto max-w-7xl px-4 pb-24 pt-5 md:px-6 md:pb-10 md:pt-8">{children}</div>
    </div>
  );
}

export function PlanningPageHeader({
  title,
  description,
  backTo,
  backLabel = "Back to profile",
  actions,
}: {
  title: string;
  description?: string;
  backTo?: string;
  backLabel?: string;
  actions?: React.ReactNode;
}) {
  return (
    <header className="mb-6 space-y-4">
      {backTo ? (
        <Link
          to={backTo}
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          {backLabel}
        </Link>
      ) : null}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex min-w-0 items-start gap-3">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-sky-500/15 text-sky-600 ring-1 ring-sky-500/20 dark:text-sky-400">
            <CalendarRange className="h-5 w-5" />
          </div>
          <div className="min-w-0">
            <h1 className="text-2xl font-semibold tracking-tight text-foreground">{title}</h1>
            {description ? (
              <p className="mt-1 max-w-2xl text-sm leading-relaxed text-muted-foreground">{description}</p>
            ) : null}
          </div>
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
    </header>
  );
}

export function PlanningPanel({
  title,
  subtitle,
  children,
  className,
  headerRight,
}: {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
  headerRight?: React.ReactNode;
}) {
  return (
    <section className={cn("overflow-hidden rounded-2xl border border-border/80 bg-card/80 shadow-sm backdrop-blur-sm", className)}>
      {title ? (
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/60 px-4 py-3.5 md:px-5">
          <div>
            <h2 className="text-sm font-semibold tracking-tight">{title}</h2>
            {subtitle ? <p className="mt-0.5 text-xs text-muted-foreground">{subtitle}</p> : null}
          </div>
          {headerRight}
        </div>
      ) : null}
      <div className="p-4 md:p-5">{children}</div>
    </section>
  );
}
