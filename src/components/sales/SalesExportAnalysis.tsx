import { useState, type ReactNode } from "react";
import { FileDown, Loader2 } from "lucide-react";
import { toast } from "sonner";
import type { RankedPartyItem } from "@/lib/sales-dashboard-types";
import {
  downloadSalesTopTenExcel,
  downloadSalesTopTenPdf,
  formatSalesCurrency,
  type SalesTopTenPdfSection,
} from "@/lib/sales-dashboard-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";

type ExportCompanyPivot = {
  company: string;
  amount: number;
  customers: Array<{ name: string; country?: string | null; amount: number }>;
};

/** Client-side pivot of export customers by selling group company (export-only). */
function buildExportCompanyPivot(rows: RankedPartyItem[]): {
  companies: ExportCompanyPivot[];
  total: number;
} {
  const map = new Map<string, ExportCompanyPivot>();
  for (const row of rows) {
    const company = row.companyName?.trim() || "Unknown";
    const key = company.toLowerCase();
    let entry = map.get(key);
    if (!entry) {
      entry = { company, amount: 0, customers: [] };
      map.set(key, entry);
    }
    const amount = Number(row.amount) || 0;
    entry.amount += amount;
    entry.customers.push({
      name: row.name,
      country: row.country,
      amount,
    });
  }
  const companies = [...map.values()]
    .map((c) => ({
      ...c,
      customers: [...c.customers].sort((a, b) => b.amount - a.amount),
    }))
    .sort((a, b) => b.amount - a.amount);
  return {
    companies,
    total: companies.reduce((sum, c) => sum + c.amount, 0),
  };
}

function EmptyRow({ colSpan, message = "No data available" }: { colSpan: number; message?: string }) {
  return (
    <TableRow>
      <TableCell colSpan={colSpan} className="py-6 text-center text-sm text-muted-foreground">
        {message}
      </TableCell>
    </TableRow>
  );
}

function RankingTable({
  title,
  subtitle,
  nameHeader,
  items,
  loading = false,
  showGroupCompany = false,
  scrollable = false,
  headerExtra,
}: {
  title: string;
  subtitle: string;
  nameHeader: string;
  items: RankedPartyItem[];
  loading?: boolean;
  showGroupCompany?: boolean;
  scrollable?: boolean;
  headerExtra?: ReactNode;
}) {
  const colSpan = showGroupCompany ? 4 : 3;
  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
      <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <h2 className="text-sm font-semibold">{title}</h2>
            <p className="mt-0.5 text-[11px] text-muted-foreground">{subtitle}</p>
          </div>
          {headerExtra}
        </div>
      </header>

      <div className={cn("space-y-2 p-3 sm:hidden", scrollable && "max-h-[28rem] overflow-y-auto")}>
        {loading && items.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Loading…</p>
        ) : items.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">No data available</p>
        ) : (
          items.map((item) => (
            <div
              key={`${item.rank}-${item.companyName ?? ""}-${item.name}`}
              className="flex items-start gap-3 rounded-xl border border-border/70 bg-background px-3 py-2.5"
            >
              <span className="mt-0.5 w-6 shrink-0 text-xs tabular-nums text-muted-foreground">
                #{item.rank}
              </span>
              <div className="min-w-0 flex-1">
                <div className="break-words text-sm font-medium leading-snug">{item.name}</div>
                {showGroupCompany && item.companyName ? (
                  <div className="mt-0.5 text-[11px] text-muted-foreground">{item.companyName}</div>
                ) : null}
                {item.country ? (
                  <div className="mt-0.5 text-[11px] text-muted-foreground">{item.country}</div>
                ) : null}
              </div>
              <div className="shrink-0 text-right text-xs font-medium tabular-nums">
                {formatSalesCurrency(item.amount)}
              </div>
            </div>
          ))
        )}
      </div>

      <div
        className={cn(
          "hidden overflow-x-auto overscroll-x-contain sm:block",
          scrollable && "max-h-[28rem] overflow-y-auto",
        )}
      >
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">#</TableHead>
              {showGroupCompany ? <TableHead>Group company</TableHead> : null}
              <TableHead>{nameHeader}</TableHead>
              <TableHead className="text-right">Accessible value (₹)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && items.length === 0 ? (
              <EmptyRow colSpan={colSpan} message="Loading…" />
            ) : items.length === 0 ? (
              <EmptyRow colSpan={colSpan} />
            ) : (
              items.map((item) => (
                <TableRow key={`${item.rank}-${item.companyName ?? ""}-${item.name}`}>
                  <TableCell className="tabular-nums text-muted-foreground">{item.rank}</TableCell>
                  {showGroupCompany ? (
                    <TableCell className="min-w-0 max-w-[10rem]">
                      <div className="truncate text-sm" title={item.companyName ?? undefined}>
                        {item.companyName || "—"}
                      </div>
                    </TableCell>
                  ) : null}
                  <TableCell className="min-w-0">
                    <div className="truncate font-medium">{item.name}</div>
                    {item.country ? (
                      <div className="truncate text-[11px] text-muted-foreground">{item.country}</div>
                    ) : null}
                  </TableCell>
                  <TableCell className="text-right text-xs tabular-nums whitespace-nowrap sm:text-sm">
                    {formatSalesCurrency(item.amount)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </section>
  );
}

export interface SalesExportAnalysisProps {
  exportCustomers: RankedPartyItem[];
  domesticCustomers: RankedPartyItem[];
  loading?: boolean;
  includeIntercompany?: boolean;
  exportDateFrom: string;
  exportDateTo: string;
  onExportDateFromChange: (value: string) => void;
  onExportDateToChange: (value: string) => void;
  domesticDateFrom: string;
  domesticDateTo: string;
  onDomesticDateFromChange: (value: string) => void;
  onDomesticDateToChange: (value: string) => void;
  pdfContext: {
    company: string;
    dateFrom: string;
    dateTo: string;
  };
}

/** Full-period export / domestic customers + group company pivot (Sales Dashboard page 2). */
export function SalesExportAnalysis({
  exportCustomers,
  domesticCustomers,
  loading = false,
  includeIntercompany = false,
  exportDateFrom,
  exportDateTo,
  onExportDateFromChange,
  onExportDateToChange,
  domesticDateFrom,
  domesticDateTo,
  onDomesticDateFromChange,
  onDomesticDateToChange,
  pdfContext,
}: SalesExportAnalysisProps) {
  const [downloading, setDownloading] = useState<"pdf" | "excel" | null>(null);
  const exportPeriodTotal = exportCustomers.reduce((sum, r) => sum + (Number(r.amount) || 0), 0);
  const domesticPeriodTotal = domesticCustomers.reduce(
    (sum, r) => sum + (Number(r.amount) || 0),
    0,
  );
  const exportDateLabel = `${exportDateFrom} → ${exportDateTo}`;
  const domesticDateLabel = `${domesticDateFrom} → ${domesticDateTo}`;
  const exportSubtitle = [
    includeIntercompany ? "Excl. India, including intercompany" : "Excl. India and intercompany",
    "Full selected period",
    exportDateLabel,
  ].join(" · ");
  const domesticSubtitle = [
    includeIntercompany ? "India only, including intercompany" : "India only, excl. intercompany",
    "Full selected period",
    domesticDateLabel,
  ].join(" · ");
  const exportPivot = buildExportCompanyPivot(exportCustomers);

  const toPdfRows = (rows: RankedPartyItem[]) =>
    rows.map((r) => ({
      rank: r.rank,
      companyName: r.companyName,
      name: r.name,
      country: r.country,
      amount: r.amount,
    }));

  const exportPdfSection: SalesTopTenPdfSection = {
    title: "Export Customers",
    subtitle: exportSubtitle,
    nameHeader: "Customer",
    baseAmount: exportPeriodTotal,
    totalLabel: `Period total (${exportCustomers.length} customers)`,
    rows: toPdfRows(exportCustomers),
  };
  const domesticPdfSection: SalesTopTenPdfSection = {
    title: "Domestic Customers",
    subtitle: domesticSubtitle,
    nameHeader: "Customer",
    baseAmount: domesticPeriodTotal,
    totalLabel: `Period total (${domesticCustomers.length} customers)`,
    rows: toPdfRows(domesticCustomers),
  };
  const companySummarySection: SalesTopTenPdfSection = {
    title: "Export by Group Company",
    subtitle: exportSubtitle,
    nameHeader: "Group company",
    baseAmount: exportPivot.total,
    totalLabel: `Period total (${exportPivot.companies.length} companies)`,
    rows: exportPivot.companies.map((c, i) => ({
      rank: i + 1,
      name: c.company,
      amount: c.amount,
    })),
  };
  const companyDetailSection: SalesTopTenPdfSection = {
    title: "Export by Company × Customer",
    subtitle: exportSubtitle,
    nameHeader: "Customer",
    baseAmount: exportPivot.total,
    totalLabel: `Period total (${exportCustomers.length} rows)`,
    rows: exportPivot.companies
      .flatMap((c) =>
        c.customers.map((cust) => ({
          companyName: c.company,
          name: cust.name,
          country: cust.country,
          amount: cust.amount,
        })),
      )
      .map((row, i) => ({ ...row, rank: i + 1 })),
  };
  const pivotSections: SalesTopTenPdfSection[] = [companySummarySection, companyDetailSection];
  const canDownloadExport = !loading && exportCustomers.length > 0;
  const canDownloadDomestic = !loading && domesticCustomers.length > 0;
  const canDownloadPivot = canDownloadExport && exportPivot.companies.length > 0;
  const pivotExportOpts = {
    fileLabel: "ExportByCompany",
    reportTitle: "Sales — Export by group company",
    dateFrom: exportDateFrom,
    dateTo: exportDateTo,
    totalAmount: exportPivot.total,
    totalAmountLabel: "Export sales total for the period",
  };

  type ExportOpts = {
    fileLabel?: string;
    dateFrom?: string;
    dateTo?: string;
    totalAmount?: number;
    totalAmountLabel?: string;
    reportTitle?: string;
  };

  function buildExportRequest(sections: SalesTopTenPdfSection[], opts?: ExportOpts) {
    return {
      category: "Sales" as const,
      company: pdfContext.company,
      dateFrom: opts?.dateFrom ?? pdfContext.dateFrom,
      dateTo: opts?.dateTo ?? pdfContext.dateTo,
      includeIntercompany,
      totalAmount: opts?.totalAmount,
      totalAmountLabel: opts?.totalAmountLabel,
      reportTitle: opts?.reportTitle,
      sections: sections.filter((s) => s.rows.length > 0),
      fileLabel: opts?.fileLabel,
    };
  }

  async function handleDownloadPdf(sections: SalesTopTenPdfSection[], opts?: ExportOpts) {
    setDownloading("pdf");
    try {
      await downloadSalesTopTenPdf(buildExportRequest(sections, opts));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not create the PDF");
    } finally {
      setDownloading(null);
    }
  }

  async function handleDownloadExcel(sections: SalesTopTenPdfSection[], opts?: ExportOpts) {
    setDownloading("excel");
    try {
      await downloadSalesTopTenExcel(buildExportRequest(sections, opts));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not create the Excel file");
    } finally {
      setDownloading(null);
    }
  }

  return (
    <div className="space-y-3 sm:space-y-4">
      <div className="min-w-0">
        <h2 className="text-sm font-semibold sm:text-base">Export analysis</h2>
        <p className="mt-0.5 text-[11px] text-muted-foreground">
          Uses the Period / Date From–To above (Custom months apply here). Card dates can override
          Export vs Domestic separately · PDF/Excel packs · amounts are bill value (BillAMount)
        </p>
      </div>

      {/* Group company pivot — prominent first block */}
      <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="min-w-0">
              <h2 className="text-sm font-semibold">Export sales by group company</h2>
              <p className="mt-0.5 text-[11px] text-muted-foreground">
                {[
                  "Export only · pivot of selling company × customer",
                  exportDateLabel,
                  `${exportPivot.companies.length} companies`,
                ].join(" · ")}
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-1.5">
              <Button
                size="sm"
                variant="outline"
                className="h-8 gap-1 px-2 text-[11px]"
                disabled={!canDownloadPivot || downloading !== null}
                onClick={() => handleDownloadPdf(pivotSections, pivotExportOpts)}
                title="Download company pivot PDF (export only)"
              >
                {downloading === "pdf" ? (
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                ) : (
                  <FileDown className="h-3.5 w-3.5" />
                )}
                PDF
              </Button>
              <Button
                size="sm"
                variant="outline"
                className="h-8 gap-1 px-2 text-[11px]"
                disabled={!canDownloadPivot || downloading !== null}
                onClick={() => handleDownloadExcel(pivotSections, pivotExportOpts)}
                title="Download company pivot Excel (export only)"
              >
                {downloading === "excel" ? (
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                ) : (
                  <FileDown className="h-3.5 w-3.5" />
                )}
                Excel
              </Button>
            </div>
          </div>
        </header>

        <div className="grid grid-cols-1 gap-0 lg:grid-cols-[minmax(0,16rem)_1fr]">
          <div className="border-b border-border lg:border-b-0 lg:border-r">
            <div className="border-b border-border/70 bg-muted/30 px-3 py-1.5 text-[11px] font-medium text-muted-foreground">
              Company totals
            </div>
            <div className="max-h-[22rem] overflow-y-auto">
              {loading && exportPivot.companies.length === 0 ? (
                <p className="px-3 py-6 text-center text-sm text-muted-foreground">Loading…</p>
              ) : exportPivot.companies.length === 0 ? (
                <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                  No export sales for this period
                </p>
              ) : (
                <ul className="divide-y divide-border/60">
                  {exportPivot.companies.map((c, i) => {
                    const share =
                      exportPivot.total > 0
                        ? Math.round((c.amount / exportPivot.total) * 1000) / 10
                        : 0;
                    return (
                      <li key={c.company} className="flex items-start gap-2 px-3 py-2">
                        <span className="mt-0.5 w-5 shrink-0 text-[11px] tabular-nums text-muted-foreground">
                          {i + 1}
                        </span>
                        <div className="min-w-0 flex-1">
                          <div className="truncate text-sm font-medium" title={c.company}>
                            {c.company}
                          </div>
                          <div className="text-[11px] text-muted-foreground">
                            {c.customers.length} customer
                            {c.customers.length === 1 ? "" : "s"} · {share}%
                          </div>
                        </div>
                        <div className="shrink-0 text-right text-xs font-medium tabular-nums">
                          {formatSalesCurrency(c.amount)}
                        </div>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
            {exportPivot.companies.length > 0 ? (
              <div className="flex items-center justify-between border-t border-border bg-muted/20 px-3 py-2 text-xs font-semibold">
                <span>Export total</span>
                <span className="tabular-nums">{formatSalesCurrency(exportPivot.total)}</span>
              </div>
            ) : null}
          </div>

          <div className="min-w-0">
            <div className="border-b border-border/70 bg-muted/30 px-3 py-1.5 text-[11px] font-medium text-muted-foreground">
              Detail · company × customer
            </div>
            <div className="max-h-[22rem] overflow-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-10">#</TableHead>
                    <TableHead>Group company</TableHead>
                    <TableHead>Customer</TableHead>
                    <TableHead className="text-right">Accessible value (₹)</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading && exportCustomers.length === 0 ? (
                    <EmptyRow colSpan={4} message="Loading…" />
                  ) : exportCustomers.length === 0 ? (
                    <EmptyRow colSpan={4} message="No export sales for this period" />
                  ) : (
                    exportPivot.companies.flatMap((c) =>
                      c.customers.map((cust, i) => (
                        <TableRow key={`${c.company}-${cust.name}-${i}`}>
                          <TableCell className="tabular-nums text-muted-foreground">
                            {i + 1}
                          </TableCell>
                          <TableCell className="min-w-0 max-w-[10rem]">
                            <div className="truncate text-sm" title={c.company}>
                              {c.company}
                            </div>
                          </TableCell>
                          <TableCell className="min-w-0">
                            <div className="truncate font-medium">{cust.name}</div>
                            {cust.country ? (
                              <div className="truncate text-[11px] text-muted-foreground">
                                {cust.country}
                              </div>
                            ) : null}
                          </TableCell>
                          <TableCell className="text-right text-xs tabular-nums whitespace-nowrap sm:text-sm">
                            {formatSalesCurrency(cust.amount)}
                          </TableCell>
                        </TableRow>
                      )),
                    )
                  )}
                </TableBody>
              </Table>
            </div>
          </div>
        </div>
      </section>

      <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-2">
        <RankingTable
          title="Export Customers"
          subtitle={exportSubtitle}
          nameHeader="Customer"
          items={exportCustomers}
          loading={loading}
          showGroupCompany
          scrollable
          headerExtra={
            <div className="flex w-full min-w-0 flex-col items-stretch gap-2 sm:w-auto sm:items-end">
              <div className="grid grid-cols-2 gap-1.5 sm:w-[17rem]">
                <div className="min-w-0 space-y-0.5">
                  <Label htmlFor="export-date-from" className="text-[10px] text-muted-foreground">
                    From
                  </Label>
                  <Input
                    id="export-date-from"
                    type="date"
                    value={exportDateFrom}
                    onChange={(e) => onExportDateFromChange(e.target.value)}
                    className="h-8 bg-background px-2 text-[11px]"
                  />
                </div>
                <div className="min-w-0 space-y-0.5">
                  <Label htmlFor="export-date-to" className="text-[10px] text-muted-foreground">
                    To
                  </Label>
                  <Input
                    id="export-date-to"
                    type="date"
                    value={exportDateTo}
                    onChange={(e) => onExportDateToChange(e.target.value)}
                    className="h-8 bg-background px-2 text-[11px]"
                  />
                </div>
              </div>
              <div className="flex flex-wrap items-center justify-end gap-1.5 self-end">
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 gap-1 px-2 text-[11px]"
                  disabled={!canDownloadExport || downloading !== null}
                  onClick={() =>
                    handleDownloadPdf([exportPdfSection], {
                      fileLabel: "ExportCustomers",
                      reportTitle: "Sales — Export Customers",
                      dateFrom: exportDateFrom,
                      dateTo: exportDateTo,
                      totalAmount: exportPeriodTotal,
                      totalAmountLabel: "Export sales total for the period",
                    })
                  }
                  title="Download export customers only (selected period)"
                >
                  {downloading === "pdf" ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <FileDown className="h-3.5 w-3.5" />
                  )}
                  PDF
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 gap-1 px-2 text-[11px]"
                  disabled={!canDownloadExport || downloading !== null}
                  onClick={() =>
                    handleDownloadExcel([exportPdfSection], {
                      fileLabel: "ExportCustomers",
                      reportTitle: "Sales — Export Customers",
                      dateFrom: exportDateFrom,
                      dateTo: exportDateTo,
                      totalAmount: exportPeriodTotal,
                      totalAmountLabel: "Export sales total for the period",
                    })
                  }
                  title="Download export customers Excel (selected period)"
                >
                  {downloading === "excel" ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <FileDown className="h-3.5 w-3.5" />
                  )}
                  Excel
                </Button>
              </div>
            </div>
          }
        />

        <RankingTable
          title="Domestic Customers"
          subtitle={domesticSubtitle}
          nameHeader="Customer"
          items={domesticCustomers}
          loading={loading}
          scrollable
          headerExtra={
            <div className="flex w-full min-w-0 flex-col items-stretch gap-2 sm:w-auto sm:items-end">
              <div className="grid grid-cols-2 gap-1.5 sm:w-[17rem]">
                <div className="min-w-0 space-y-0.5">
                  <Label htmlFor="domestic-date-from" className="text-[10px] text-muted-foreground">
                    From
                  </Label>
                  <Input
                    id="domestic-date-from"
                    type="date"
                    value={domesticDateFrom}
                    onChange={(e) => onDomesticDateFromChange(e.target.value)}
                    className="h-8 bg-background px-2 text-[11px]"
                  />
                </div>
                <div className="min-w-0 space-y-0.5">
                  <Label htmlFor="domestic-date-to" className="text-[10px] text-muted-foreground">
                    To
                  </Label>
                  <Input
                    id="domestic-date-to"
                    type="date"
                    value={domesticDateTo}
                    onChange={(e) => onDomesticDateToChange(e.target.value)}
                    className="h-8 bg-background px-2 text-[11px]"
                  />
                </div>
              </div>
              <div className="flex flex-wrap items-center justify-end gap-1.5 self-end">
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 gap-1 px-2 text-[11px]"
                  disabled={!canDownloadDomestic || downloading !== null}
                  onClick={() =>
                    handleDownloadPdf([domesticPdfSection], {
                      fileLabel: "DomesticCustomers",
                      reportTitle: "Sales — Domestic Customers",
                      dateFrom: domesticDateFrom,
                      dateTo: domesticDateTo,
                      totalAmount: domesticPeriodTotal,
                      totalAmountLabel: "Domestic sales total for the period",
                    })
                  }
                  title="Download domestic customers only (selected period)"
                >
                  {downloading === "pdf" ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <FileDown className="h-3.5 w-3.5" />
                  )}
                  PDF
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 gap-1 px-2 text-[11px]"
                  disabled={!canDownloadDomestic || downloading !== null}
                  onClick={() =>
                    handleDownloadExcel([domesticPdfSection], {
                      fileLabel: "DomesticCustomers",
                      reportTitle: "Sales — Domestic Customers",
                      dateFrom: domesticDateFrom,
                      dateTo: domesticDateTo,
                      totalAmount: domesticPeriodTotal,
                      totalAmountLabel: "Domestic sales total for the period",
                    })
                  }
                  title="Download domestic customers Excel (selected period)"
                >
                  {downloading === "excel" ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <FileDown className="h-3.5 w-3.5" />
                  )}
                  Excel
                </Button>
              </div>
            </div>
          }
        />
      </div>
    </div>
  );
}
