import { Link } from "@tanstack/react-router";
import { ArrowLeft, ChevronRight, Layers } from "lucide-react";
import { cn } from "@/lib/utils";

export function BomPageShell({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("min-h-full bg-gradient-to-b from-amber-500/[0.04] via-background to-background", className)}>
      <div className="mx-auto max-w-7xl px-4 pb-24 pt-5 md:px-6 md:pb-10 md:pt-8">{children}</div>
    </div>
  );
}

export function BomPageHeader({
  title,
  description,
  backTo,
  backLabel = "Back to report",
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
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-amber-500/15 text-amber-600 ring-1 ring-amber-500/20 dark:text-amber-400">
            <Layers className="h-5 w-5" />
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

export function BomPanel({
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
      {children}
    </section>
  );
}

export function BomStat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-border/60 bg-muted/20 px-3 py-2.5">
      <p className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium leading-snug text-foreground">{value}</p>
    </div>
  );
}

export function BomFieldLabel({ children }: { children: React.ReactNode }) {
  return <span className="text-xs font-medium text-muted-foreground">{children}</span>;
}

export function BomRowChevron() {
  return (
    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground/50 transition-transform group-hover:translate-x-0.5 group-hover:text-amber-600" />
  );
}
