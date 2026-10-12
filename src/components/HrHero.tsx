import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

/** Colourful page banner used across the HR portal. */
export function HrHero({
  icon: Icon,
  eyebrow,
  title,
  description,
  actions,
  className,
}: {
  icon: LucideIcon;
  eyebrow?: ReactNode;
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-2xl bg-gradient-to-r from-indigo-600 via-fuchsia-600 to-pink-500 px-5 py-6 text-white shadow-lg md:px-7",
        className,
      )}
    >
      <div className="pointer-events-none absolute -right-10 -top-16 h-48 w-48 rounded-full bg-white/15 blur-2xl" />
      <div className="pointer-events-none absolute -bottom-20 left-1/3 h-48 w-48 rounded-full bg-amber-300/25 blur-3xl" />
      <div className="relative flex flex-wrap items-start justify-between gap-4">
        <div className="flex min-w-0 items-start gap-4">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-white/20 ring-1 ring-white/30 backdrop-blur">
            <Icon className="h-6 w-6" />
          </div>
          <div className="min-w-0">
            {eyebrow ? <div className="text-xs font-semibold uppercase tracking-wider text-white/80">{eyebrow}</div> : null}
            <h1 className="text-2xl font-bold tracking-tight md:text-3xl">{title}</h1>
            {description ? <div className="mt-1 max-w-3xl text-sm text-white/85">{description}</div> : null}
          </div>
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
    </div>
  );
}
