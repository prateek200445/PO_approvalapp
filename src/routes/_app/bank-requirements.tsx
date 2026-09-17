import { createFileRoute, Link } from "@tanstack/react-router";
import { ChevronRight, FileBarChart, Landmark, Upload } from "lucide-react";

export const Route = createFileRoute("/_app/bank-requirements")({
  head: () => ({ meta: [{ title: "Banking — PO Portal" }] }),
  component: BankingHubPage,
});

function BankingHubPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Banking</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Bank-facing reports and statement tools — pick a section below.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <HubCard
          to="/bank-sales-profile"
          icon={FileBarChart}
          title="Sales Profile"
          description="Export vs domestic sales profile for bank submissions (INR crore, month / FY filters, Excel & PDF)."
        />
        <HubCard
          to="/bank-statement-import"
          icon={Upload}
          title="Bank Statement Import"
          description="Upload a bank statement (PDF / CSV / Excel), convert to ERP import format, edit rows, and optionally categorize."
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
  icon: typeof Landmark;
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
