import { useMemo } from "react";
import { Scale } from "lucide-react";
import {
  buildSettlementPlan,
  formatAsOn,
  formatCrore,
  formatInr,
  type IntercompanyMatrix,
} from "@/lib/intercompany-api";
import { SettlementCircle } from "@/components/intercompany/SettlementCircle";
import { SettlementReport } from "@/components/intercompany/SettlementReport";

export function SettlementGuide({
  asOf,
  matrices,
  selected,
}: {
  asOf: string;
  matrices: IntercompanyMatrix[];
  selected?: IntercompanyMatrix;
}) {
  const plan = useMemo(() => buildSettlementPlan(matrices), [matrices]);
  if (plan.positions.length === 0) return null;

  const payers = plan.positions.filter((p) => p.action === "pay");
  const receivers = plan.positions.filter((p) => p.action === "receive");
  const settled = plan.positions.filter((p) => p.action === "settled");

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
      <div className="bg-primary px-4 py-3 text-primary-foreground">
        <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-[0.12em] md:text-[15px]">
          <Scale className="h-4 w-4" />
          How to settle these balances
        </h2>
        <p className="mt-1 text-[12px] text-primary-foreground/85">
          As on {formatAsOn(asOf)}. Minus is paid out, plus is taken in. The loop is the full
          transfer, once.
        </p>
      </div>

      <div className="grid gap-2 border-b border-border bg-muted/30 p-3 sm:grid-cols-2">
        <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2.5">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-red-800">
            1. If the balance is minus (−)
          </p>
          <p className="mt-1 text-sm text-red-950">
            Money is <strong>taken out</strong> of that company. It must <strong>pay</strong> the
            group.
          </p>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2.5">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-emerald-800">
            2. If the balance is plus (+)
          </p>
          <p className="mt-1 text-sm text-emerald-950">
            Money is <strong>added</strong> to that company. It must <strong>receive</strong> from
            the group.
          </p>
        </div>
      </div>

      <div className="grid gap-3 border-b border-border p-3 lg:grid-cols-2">
        <div>
          <h3 className="mb-2 text-sm font-semibold text-red-800">Who should pay (minus)</h3>
          {payers.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nobody is in minus on this date.</p>
          ) : (
            <ul className="space-y-2">
              {payers.map((row) => (
                <li
                  key={row.company}
                  className="flex items-start justify-between gap-3 rounded-xl border border-red-200 bg-red-50/90 px-3 py-2"
                >
                  <span className="min-w-0 text-sm font-medium">{row.company}</span>
                  <span className="shrink-0 text-right text-sm font-semibold tabular-nums text-red-700">
                    Pay ₹ {formatInr(Math.abs(row.net))}
                    <span className="block text-[11px] font-normal text-red-800/70">
                      {formatCrore(Math.abs(row.net))} Cr
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <div>
          <h3 className="mb-2 text-sm font-semibold text-emerald-800">Who should receive (plus)</h3>
          {receivers.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nobody is in plus on this date.</p>
          ) : (
            <ul className="space-y-2">
              {receivers.map((row) => (
                <li
                  key={row.company}
                  className="flex items-start justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50/90 px-3 py-2"
                >
                  <span className="min-w-0 text-sm font-medium">{row.company}</span>
                  <span className="shrink-0 text-right text-sm font-semibold tabular-nums text-emerald-700">
                    Receive ₹ {formatInr(row.net)}
                    <span className="block text-[11px] font-normal text-emerald-800/70">
                      {formatCrore(row.net)} Cr
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {settled.length > 0 ? (
        <p className="border-b border-border px-4 py-2 text-xs text-muted-foreground">
          Already near zero: {settled.map((s) => s.company).join(", ")}.
        </p>
      ) : null}

      <div className="px-4 py-4">
        <h3 className="text-sm font-semibold">Transfer loop</h3>
        <p className="mt-0.5 text-xs text-muted-foreground">
          Shown once on the 3D circle. Each company pays the next; the last pays the first.
        </p>
        <div className="mt-3">
          <SettlementCircle
            companies={matrices.map((m) => m.company).filter(Boolean)}
            matrices={matrices}
          />
        </div>
      </div>

      <div className="border-t border-border px-4 py-4">
        <SettlementReport asOf={asOf} matrices={matrices} />
      </div>
    </section>
  );
}
