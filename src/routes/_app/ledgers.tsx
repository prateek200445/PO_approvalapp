import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftRight, BookOpen, Building2, ChevronRight, LineChart } from "lucide-react";

export const Route = createFileRoute("/_app/ledgers")({
  head: () => ({ meta: [{ title: "Ledgers — PO Portal" }] }),
  component: LedgersHubPage,
});

function LedgersHubPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Ledgers</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          View company P&amp;L, ledger summaries, intercompany balances, or reconcile two Excel ledgers.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <HubCard
          to="/pnl"
          icon={LineChart}
          title="P&L Inputs"
          description="Maintain monthly stock, provision, and Common / HO inputs used by the result page."
        />
        <HubCard
          to="/pnl-result"
          icon={LineChart}
          title="P&L / EBITDA Result"
          description="Select company and month to generate the final P&L and EBITDA statement."
        />
        <HubCard
          to="/ledger-summary"
          icon={BookOpen}
          title="Ledger Summary"
          description="Filter by company, ledger, and date range to view voucher-wise ledger balances from ERP."
        />
        <HubCard
          to="/intercompany"
          icon={Building2}
          title="Intercompany"
          description="Group-company outstanding, balance matrix, and a settlement prediction for plus/minus nets."
        />
        <HubCard
          to="/reconciliation"
          icon={ArrowLeftRight}
          title="Ledger Reconciliation"
          description="Upload two company Excel ledgers and match Bill No + Bill Date (with voucher-date fallback)."
        />
      </div>
    </div>
  );
}

function HubCard({
  to,
  icon: Icon,
  title,
  description,
}: {
  to: string;
  icon: typeof BookOpen;
  title: string;
  description: string;
}) {
  return (
    <Link
      to={to}
      className="group rounded-xl border border-border bg-card p-5 shadow-sm transition hover:border-primary/40 hover:bg-secondary/30"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <Icon className="h-5 w-5" />
        </div>
        <ChevronRight className="h-5 w-5 text-muted-foreground transition group-hover:translate-x-0.5 group-hover:text-foreground" />
      </div>
      <h2 className="mt-4 text-lg font-semibold">{title}</h2>
      <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">{description}</p>
    </Link>
  );
}
