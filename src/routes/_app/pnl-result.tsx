import { createFileRoute, Link } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, CircleHelp, Download, FileSpreadsheet, Loader2 } from "lucide-react";
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
import { getPnlCompanies, getPnlStatement, downloadPnlStatement, downloadPnlSummary, downloadPnlMonthGrid } from "@/lib/pnl-api";
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
  const [exporting, setExporting] = useState<"statement" | "summary" | "month" | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);
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

  const runExport = async (kind: "statement" | "summary" | "month") => {
    setExportError(null);
    setExporting(kind);
    try {
      if (kind === "statement") await downloadPnlStatement(company, month);
      else if (kind === "summary") await downloadPnlSummary(month);
      else await downloadPnlMonthGrid(month);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "Export failed.");
    } finally {
      setExporting(null);
    }
  };

  const companyOptions = useMemo(
    () => (companiesQuery.data ?? []).map((c) => ({ value: c.value, label: c.label })),
    [companiesQuery.data],
  );

  return (
    <div className="mx-auto max-w-6xl space-y-6 pb-10">
      <div className="flex items-start gap-4">
        <Link
          to="/ledgers"
          className="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border bg-card text-muted-foreground shadow-sm transition hover:border-primary/40 hover:bg-secondary hover:text-foreground"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div className="min-w-0 flex-1 pt-0.5">
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">P&L Result</h1>
          <p className="mt-1.5 max-w-2xl text-sm leading-relaxed text-muted-foreground">
            Monthly P&amp;L and EBITDA from trial balance, plus saved stock, provision, and Common / HO.
          </p>
        </div>
      </div>

      <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="space-y-2 text-sm">
            <span className="font-medium text-foreground">Company</span>
            <SearchableSelect
              options={companyOptions}
              value={company}
              onChange={setCompany}
              placeholder={companiesQuery.isLoading ? "Loading…" : "Select company"}
              disabled={companiesQuery.isLoading}
            />
          </label>
          <label className="space-y-2 text-sm">
            <span className="font-medium text-foreground">Month / Year</span>
            <MonthPickerField value={month} onChange={setMonth} placeholder="Select month" />
          </label>
        </div>

        <div className="mt-4 flex flex-col gap-3 border-t pt-4 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span className="rounded-md bg-secondary px-2 py-1 font-medium text-foreground">
              {range.from} → {range.to}
            </span>
            <Link to="/pnl" className="font-medium text-primary hover:underline">
              Open input page
            </Link>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <HelpDialog />
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-2"
              disabled={exporting !== null}
              onClick={() => void runExport("statement")}
            >
              {exporting === "statement" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
              Export P&amp;L
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-2"
              disabled={exporting !== null}
              onClick={() => void runExport("summary")}
            >
              {exporting === "summary" ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <FileSpreadsheet className="h-4 w-4" />
              )}
              Export Summary
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="gap-2"
              disabled={exporting !== null}
              onClick={() => void runExport("month")}
            >
              {exporting === "month" ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <FileSpreadsheet className="h-4 w-4" />
              )}
              Export month P&amp;L
            </Button>
          </div>
        </div>
        {exporting === "summary" ? (
          <p className="mt-3 text-xs text-muted-foreground">
            Building the plant summary from April through {month}. This can take a minute.
          </p>
        ) : null}
        {exporting === "month" ? (
          <p className="mt-3 text-xs text-muted-foreground">
            Building the {month} P&amp;L grid for all plants. This can take a minute.
          </p>
        ) : null}
        {exportError ? <p className="mt-3 text-sm text-destructive">{exportError}</p> : null}
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
  if (query.isLoading) {
    return (
      <div className="rounded-2xl border bg-card px-6 py-16 shadow-sm">
        <Busy label="Generating P&L…" />
      </div>
    );
  }
  if (query.error) {
    return (
      <p className="rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
        {query.error.message}
      </p>
    );
  }
  const data = query.data;
  if (!data) return null;

  return (
    <div className="space-y-5">
      {data.stockIncomplete ? (
        <p className="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm">
          Stock values are empty. Enter opening/closing stock so materials consumed and inventory change are correct.
        </p>
      ) : null}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Month EBITDA" value={data.ebitdaLacs} tone="teal" />
        <Kpi label="YTD EBITDA" value={data.rows.find((r) => r.id === "ebitda")?.ytdLacs ?? null} tone="teal" />
        <Kpi label="Month PBT" value={data.pbtLacs} tone="gold" />
        <Kpi label="YTD PBT" value={data.rows.find((r) => r.id === "pbt")?.ytdLacs ?? null} tone="gold" />
      </div>
      <p className="text-xs text-muted-foreground">
        FY opening stock changes YTD only. July month EBITDA / PBT stay the same.
      </p>
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[36rem] text-sm">
            <thead className="sticky top-0 bg-muted/80 text-left text-xs text-muted-foreground backdrop-blur">
              <tr>
                <th className="px-4 py-3 font-medium">Particulars</th>
                <th className="px-3 py-3 text-right font-medium">Month</th>
                <th className="px-3 py-3 text-right font-medium">% of turnover</th>
                <th className="px-3 py-3 text-right font-medium">YTD</th>
                <th className="px-4 py-3 text-right font-medium">YTD % of turnover</th>
              </tr>
            </thead>
            <tbody>
              {data.rows.map((row) => (
                <tr
                  key={row.id}
                  className={cn(
                    "border-t border-border/70",
                    row.kind === "header" && "bg-muted/50",
                    row.kind === "total" && "bg-sky-500/5 font-semibold",
                    row.kind === "result" && "bg-emerald-500/10 font-semibold",
                  )}
                >
                  <td
                    className={cn(
                      "px-4 py-2.5",
                      row.kind === "line" && "pl-7 text-muted-foreground",
                      row.kind === "header" && "text-[11px] font-semibold uppercase tracking-wide text-foreground",
                    )}
                  >
                    {row.label}
                  </td>
                  <td className={cn("px-3 py-2.5 text-right tabular-nums", amountClass(row.monthLacs, row.kind))}>
                    {row.kind === "header" ? "" : formatLacs(row.monthLacs)}
                  </td>
                  <td className="px-3 py-2.5 text-right tabular-nums text-muted-foreground">
                    {row.kind === "header" ? "" : formatPct(row.pctToSales)}
                  </td>
                  <td className={cn("px-3 py-2.5 text-right tabular-nums", amountClass(row.ytdLacs, row.kind))}>
                    {row.kind === "header" ? "" : formatLacs(row.ytdLacs)}
                  </td>
                  <td className="px-4 py-2.5 text-right tabular-nums text-muted-foreground">
                    {row.kind === "header" ? "" : formatPct(row.ytdPctToSales)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="flex items-center justify-end border-t bg-muted/20 px-4 py-3">
          <Button variant="outline" size="sm" onClick={() => query.refetch()}>
            Refresh P&amp;L
          </Button>
        </div>
      </div>
      {data.unmapped.length > 0 ? (
        <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-5">
          <h3 className="text-sm font-semibold">Unmapped TB heads</h3>
          <div className="mt-3 divide-y">
            {data.unmapped.map((h) => (
              <div key={h.head} className="flex justify-between gap-4 py-2 text-sm">
                <span className="text-muted-foreground">{h.head}</span>
                <span className={cn("tabular-nums", (h.amountLacs ?? 0) < 0 && "text-red-600 dark:text-red-400")}>
                  {formatLacs(h.amountLacs)}
                </span>
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function amountClass(value: number | null | undefined, kind: string) {
  if (kind === "header") return "";
  if (value != null && value < 0) return "text-red-600 dark:text-red-400";
  if (kind === "result") return "text-emerald-800 dark:text-emerald-300";
  return "";
}

function Kpi({
  label,
  value,
  tone,
}: {
  label: string;
  value: number | null | undefined;
  tone: "teal" | "gold";
}) {
  const negative = value != null && value < 0;
  return (
    <div
      className={cn(
        "rounded-2xl border bg-card p-4 shadow-sm",
        tone === "teal" && "border-l-4 border-l-emerald-600",
        tone === "gold" && "border-l-4 border-l-amber-500",
      )}
    >
      <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <div
        className={cn(
          "mt-2 text-2xl font-semibold tabular-nums tracking-tight",
          negative && "text-red-600 dark:text-red-400",
        )}
      >
        {formatLacs(value)}
      </div>
      <div className="mt-0.5 text-[11px] text-muted-foreground">lacs</div>
    </div>
  );
}

function Busy({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
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
                  Provision uploads are month-wise. The month column is this month’s provision minus last month,
                  except April (FY start) which takes April provision only — March is not reversed. YTD is the
                  selected month’s provision only, not the sum of every month.
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
