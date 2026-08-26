import { createFileRoute, Link } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, CircleHelp, Loader2 } from "lucide-react";
import { SearchableSelect } from "@/components/SearchableSelect";
import { MonthPickerField } from "@/components/MonthPickerField";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { getPnlCompanies, getPnlStatement } from "@/lib/pnl-api";
import {
  currentMonthValue,
  formatLacs,
  formatPct,
  monthRange,
  type PnlHeadGroup,
} from "@/lib/pnl-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/pnl-result")({
  head: () => ({ meta: [{ title: "P&L Result — PO Portal" }] }),
  component: PnlResultPage,
});

function PnlResultPage() {
  const [company, setCompany] = useState("All Companies");
  const [month, setMonth] = useState(currentMonthValue());
  const range = monthRange(month);

  const companiesQuery = useQuery({
    queryKey: ["pnl-companies"],
    queryFn: getPnlCompanies,
    staleTime: 60 * 60_000,
  });

  const statementQuery = useQuery({
    queryKey: ["pnl-statement", company, month],
    queryFn: () => getPnlStatement(company, month),
    staleTime: 30_000,
  });

  const companyOptions = useMemo(
    () => (companiesQuery.data ?? []).map((c) => ({ value: c.value, label: c.label })),
    [companiesQuery.data],
  );

  return (
    <div className="space-y-4 pb-8">
      <div className="flex items-start gap-3">
        <Link
          to="/ledgers"
          className="mt-1 rounded-md p-1 text-muted-foreground hover:bg-secondary hover:text-foreground"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">P&L Result</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Generate the monthly P&amp;L / EBITDA view from trial balance plus the saved stock, provision, and
            Common / HO inputs.
          </p>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="space-y-1 text-sm">
          <span className="text-muted-foreground">Company</span>
          <SearchableSelect
            options={companyOptions}
            value={company}
            onChange={setCompany}
            placeholder={companiesQuery.isLoading ? "Loading…" : "Select company"}
            disabled={companiesQuery.isLoading}
          />
        </label>
        <label className="space-y-1 text-sm">
          <span className="text-muted-foreground">Month / Year</span>
          <MonthPickerField value={month} onChange={setMonth} placeholder="Select month" />
        </label>
      </div>

      <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <span>Range: {range.from} to {range.to}</span>
        <Link to="/pnl" className="font-medium text-primary hover:underline">
          Open input page
        </Link>
        <HelpDialog />
      </div>

      <StatementView query={statementQuery} />
    </div>
  );
}

function StatementView({
  query,
}: {
  query: {
    data?: {
      ebitdaLacs: number;
      pbtLacs: number;
      stockIncomplete: boolean;
      rows: {
        id: string;
        label: string;
        kind: string;
        monthLacs: number | null;
        pctToSales: number | null;
        ytdLacs: number | null;
        ytdPctToSales: number | null;
      }[];
      unmapped: PnlHeadGroup[];
    };
    isLoading: boolean;
    error: Error | null;
    refetch: () => void;
  };
}) {
  if (query.isLoading) return <Busy label="Generating P&L…" />;
  if (query.error) return <p className="text-sm text-destructive">{query.error.message}</p>;
  const data = query.data;
  if (!data) return null;

  return (
    <div className="space-y-3">
      {data.stockIncomplete ? (
        <p className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
          Stock values are empty. Enter opening/closing stock so materials consumed and inventory change are correct.
        </p>
      ) : null}
      <div className="grid grid-cols-2 gap-3">
        <Kpi label="EBITDA" value={data.ebitdaLacs} />
        <Kpi label="PBT" value={data.pbtLacs} />
      </div>
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full min-w-[28rem] text-sm">
          <thead className="bg-muted/60 text-left text-xs text-muted-foreground">
            <tr>
              <th className="px-2 py-2 font-medium">Particulars</th>
              <th className="px-2 py-2 text-right font-medium">Month</th>
              <th className="px-2 py-2 text-right font-medium">% of turnover</th>
              <th className="px-2 py-2 text-right font-medium">YTD</th>
              <th className="px-2 py-2 text-right font-medium">YTD % of turnover</th>
            </tr>
          </thead>
          <tbody>
            {data.rows.map((row) => (
              <tr
                key={row.id}
                className={cn(
                  "border-t",
                  row.kind === "header" && "bg-muted/40 font-semibold",
                  row.kind === "total" && "font-semibold",
                  row.kind === "result" && "bg-primary/10 font-semibold",
                )}
              >
                <td className={cn("px-2 py-1.5", row.kind === "line" && "pl-4")}>{row.label}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">
                  {row.kind === "header" ? "" : formatLacs(row.monthLacs)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums text-muted-foreground">
                  {row.kind === "header" ? "" : formatPct(row.pctToSales)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums">
                  {row.kind === "header" ? "" : formatLacs(row.ytdLacs)}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums text-muted-foreground">
                  {row.kind === "header" ? "" : formatPct(row.ytdPctToSales)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {data.unmapped.length > 0 ? (
        <div>
          <h3 className="mb-2 text-sm font-medium">Unmapped TB heads</h3>
          {data.unmapped.map((h) => (
            <div key={h.head} className="flex justify-between text-sm">
              <span className="text-muted-foreground">{h.head}</span>
              <span className="tabular-nums">{formatLacs(h.amountLacs)}</span>
            </div>
          ))}
        </div>
      ) : null}
      <Button variant="outline" onClick={() => query.refetch()}>
        Refresh P&amp;L
      </Button>
    </div>
  );
}

function Kpi({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <div className="text-xs text-muted-foreground">{label} (lacs)</div>
      <div className="mt-1 text-xl font-semibold tabular-nums">{formatLacs(value)}</div>
    </div>
  );
}

function Busy({ label }: { label: string }) {
  return (
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      {label}
    </div>
  );
}

function HelpDialog() {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button type="button" variant="outline" size="sm" className="gap-2">
          <CircleHelp className="h-4 w-4" />
          How is this calculated?
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[85vh] max-w-4xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle>P&amp;L calculation help</DialogTitle>
          <DialogDescription>
            This view is built from ERP trial balance heads, month-wise stock, provisions, and Common / HO inputs.
            Month and YTD are calculated with the same formula set.
          </DialogDescription>
        </DialogHeader>

        <div className="rounded-lg border bg-card p-3">
          <div className="text-sm font-semibold">Column guide</div>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            <HelpPill title="Month" text="Value for the selected month only." />
            <HelpPill title="% of turnover" text="Month value divided by month turnover." />
            <HelpPill title="YTD" text="Cumulative value from April to the selected month." />
            <HelpPill title="YTD % of turnover" text="YTD value divided by YTD turnover." />
          </div>
        </div>

        <Accordion type="multiple" defaultValue={["flow", "rows", "inventory"]} className="mt-2">
          <AccordionItem value="flow">
            <AccordionTrigger>Calculation flow</AccordionTrigger>
            <AccordionContent>
              <div className="space-y-2 text-sm text-muted-foreground">
                <p>1. Load ERP TB heads and sign-flip income so credits become positive for the report.</p>
                <p>2. Read the provision rows saved for the selected month from the database.</p>
                <p>
                  3. Apply the prior-month provision reversal internally when the workbook-style logic needs it. The
                  accountant only uploads month-wise provision files; the system handles the offset automatically.
                </p>
                <p>4. Pull stock opening/closing values from the saved stock table.</p>
                <p>5. Pull Common / HO from the overhead table for the month and YTD range.</p>
                <p>6. Build turnover, COGS, expenses, EBITDA, and PBT from those inputs.</p>
              </div>
            </AccordionContent>
          </AccordionItem>

          <AccordionItem value="rows">
            <AccordionTrigger>Row-by-row formula map</AccordionTrigger>
            <AccordionContent>
              <div className="space-y-4 text-sm">
                <HelpGroup
                  title="Revenue from operations"
                  rows={[
                    ["Sale of finished goods", "TB head 'Sale of finished goods'"],
                    ["Sale of traded goods", "TB head 'Sale of traded goods'"],
                    ["Total Revenue from operations", "Finished goods + traded goods"],
                  ]}
                />
                <HelpGroup
                  title="Sale of services"
                  rows={[
                    ["Income from Jobwork charges", "TB head 'Income from Jobwork charges'"],
                    ["Total Sales of services", "Jobwork charges only"],
                  ]}
                />
                <HelpGroup
                  title="Other operating revenue"
                  rows={[
                    ["Export incentives", "TB head 'Export incentives'"],
                    ["Commission income", "TB head 'Commission income'"],
                    ["Scrap sales", "TB head 'Scrap sales'"],
                    ["Total Other operating revenue", "Sum of the three rows above"],
                  ]}
                />
                <HelpGroup
                  title="Other income"
                  rows={[
                    ["Wind mill income", "TB head 'Wind mill income'"],
                    ["- on fixed deposits with Banks", "TB head mapped to fixed deposit interest"],
                    ["- from others", "Other interest income head"],
                    ["Rent Income", "TB rent head"],
                    ["Interest Received From Customers", "Customer interest head"],
                    ["Profit on sale of fixed assets", "TB fixed asset sale gain"],
                    ["Profit on sale of investments", "TB investment sale gain"],
                    ["Sundry balance written off", "TB sundry write-off head"],
                    ["Miscellaneous income", "TB misc income head"],
                    ["Total Other income", "Sum of all nine lines above"],
                  ]}
                />
                <HelpGroup
                  title="Turnover"
                  rows={[
                    ["Total Turnover", "Revenue from operations + services + other operating revenue + other income"],
                  ]}
                />
                <HelpGroup
                  title="Cost of goods"
                  rows={[
                    ["Cost of materials consumed", "Raw material purchases + opening RM/Packing stock - closing RM/Packing stock"],
                    ["Purchase of stock-in-trade", "Traded purchase feed + opening traded stock - closing traded stock"],
                    ["Changes in inventory of finished goods and work-in-progress", "Opening WIP + opening FG - closing WIP - closing FG"],
                    ["Stores, Spares & Consumables", "Stores purchase feed + opening stores - closing stores"],
                    ["Total Cost of goods", "Sum of the four lines above"],
                  ]}
                />
                <HelpGroup
                  title="Operating expenses"
                  rows={[
                    ["Employee benefit expenses", "Salaries + PF + gratuity + welfare + labour"],
                    ["Manufacturing Expenses", "Power + jobwork + other manufacturing"],
                    ["Selling and Distribution Expenses", "Advertising + travel + brokerage + conveyance + freight outward"],
                    ["Administrative Expenses", "Insurance + misc expense + other admin + exchange difference + Common + HO"],
                    ["Total Cost of goods sold", "COGS + employee + manufacturing + selling + administrative"],
                  ]}
                />
                <HelpGroup
                  title="Bottom line"
                  rows={[
                    ["EBITDA", "Total Turnover - Total Cost of goods sold"],
                    ["Depreciation", "TB depreciation head"],
                    ["Bank Charges & Commission", "TB bank charges head"],
                    ["Interest Expense - Other", "TB other interest head"],
                    ["Interest on Term Loan", "TB term-loan interest head"],
                    ["Interest on Working Capital & Term Loan", "TB working-capital interest head"],
                    ["PBT", "EBITDA - all finance cost lines above"],
                    ["Cash PBT", "PBT + Depreciation"],
                  ]}
                />
              </div>
            </AccordionContent>
          </AccordionItem>

          <AccordionItem value="inventory">
            <AccordionTrigger>Stock, provision, and Common / HO logic</AccordionTrigger>
            <AccordionContent>
              <div className="space-y-3 text-sm text-muted-foreground">
                <p>
                  Stock feeds are month-wise. Materials consumed uses opening and closing RM/Packing stock; traded uses
                  traded stock; inventory change uses WIP and finished goods; stores uses stores/spares.
                </p>
                <p>
                  Provision uploads are month-wise. The report reads the selected month’s saved provision rows, and
                  the engine handles the prior-month provision reversal internally so the result matches the workbook
                  method without extra user action. Think of it as “provision accrual + automatic unwind,” not a manual
                  step.
                </p>
                <p>
                  Common / HO is added under Administrative Expenses as separate computed lines and is included in the
                  total administrative block and YTD.
                </p>
              </div>
            </AccordionContent>
          </AccordionItem>

          <AccordionItem value="notes">
            <AccordionTrigger>Notes on signs and percentages</AccordionTrigger>
            <AccordionContent>
              <div className="space-y-2 text-sm text-muted-foreground">
                <p>Income TB credits are sign-flipped so they appear positive in the report.</p>
                <p>Expense lines keep their natural sign from the TB and saved monthly inputs.</p>
                <p>Every % column is shown against turnover, not against the row total.</p>
                <p>Unmapped TB heads appear separately below the statement so nothing is hidden.</p>
              </div>
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </DialogContent>
    </Dialog>
  );
}

function HelpPill({ title, text }: { title: string; text: string }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</div>
      <div className="mt-1 text-sm">{text}</div>
    </div>
  );
}

function HelpGroup({
  title,
  rows,
}: {
  title: string;
  rows: [string, string][];
}) {
  return (
    <div className="rounded-lg border bg-card">
      <div className="border-b px-3 py-2 text-sm font-semibold">{title}</div>
      <div className="divide-y">
        {rows.map(([label, formula]) => (
          <div key={label} className="grid gap-2 px-3 py-2 md:grid-cols-[260px_1fr]">
            <div className="font-medium text-foreground">{label}</div>
            <div className="text-muted-foreground">{formula}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
