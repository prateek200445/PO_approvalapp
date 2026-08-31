import { useEffect, useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangle, Download, Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import {
  defaultAuditDateRange,
  downloadExportCurrencyAuditExcel,
  formatFcAmount,
  formatInrAmount,
  getExportCurrencyAuditCompanies,
  runExportCurrencyAudit,
  type ExportCurrencyAuditItem,
  type ExportCurrencyAuditResult,
} from "@/lib/export-currency-audit-api";
import { type LedgerCompanyOption } from "@/lib/ledger-summary-types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export const Route = createFileRoute("/_app/export-currency-audit")({
  head: () => ({ meta: [{ title: "Export Currency Audit — PO Portal" }] }),
  component: ExportCurrencyAuditPage,
});

const ALL_COMPANIES = "A-all";

function ExportCurrencyAuditPage() {
  const defaults = defaultAuditDateRange();
  const [companies, setCompanies] = useState<LedgerCompanyOption[]>([]);
  const [companyValue, setCompanyValue] = useState(ALL_COMPANIES);
  const [dateFrom, setDateFrom] = useState(defaults.dateFrom);
  const [dateTo, setDateTo] = useState(defaults.dateTo);
  const [minInr, setMinInr] = useState("100");
  const [search, setSearch] = useState("");
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [running, setRunning] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState<ExportCurrencyAuditResult | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingCompanies(true);
      try {
        const list = await getExportCurrencyAuditCompanies();
        if (!cancelled) setCompanies(list);
      } catch (err: unknown) {
        if (!cancelled) toast.error(err instanceof Error ? err.message : "Failed to load companies");
      } finally {
        if (!cancelled) setLoadingCompanies(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const companyLabel = useMemo(() => {
    if (companyValue === ALL_COMPANIES) return "All companies";
    return companies.find((c) => c.value === companyValue)?.label ?? companyValue;
  }, [companies, companyValue]);

  const filteredItems = useMemo(() => {
    if (!result) return [];
    const q = search.trim().toLowerCase();
    if (!q) return result.items;
    return result.items.filter((row) =>
      [
        row.documentType,
        row.documentNo,
        row.companyName,
        row.partyName,
        row.ledgerName,
        row.storedFc,
        row.issue,
      ].some((v) => v.toLowerCase().includes(q)),
    );
  }, [result, search]);

  async function handleRun() {
    setRunning(true);
    try {
      const audit = await runExportCurrencyAudit({
        company: companyValue === ALL_COMPANIES ? ALL_COMPANIES : companyValue,
        dateFrom,
        dateTo,
        minInr: Number(minInr) || 100,
      });
      setResult(audit);
      if (audit.totalCount === 0) {
        toast.message("No mismatches found for the selected filters.");
      } else {
        toast.success(`Found ${audit.totalCount} document(s) with INR posted but stored FC missing.`);
      }
    } catch (err: unknown) {
      setResult(null);
      toast.error(err instanceof Error ? err.message : "Audit failed");
    } finally {
      setRunning(false);
    }
  }

  async function handleExport() {
    setExporting(true);
    try {
      await downloadExportCurrencyAuditExcel({
        company: companyValue === ALL_COMPANIES ? ALL_COMPANIES : companyValue,
        dateFrom,
        dateTo,
        minInr: Number(minInr) || 100,
      });
      toast.success("Excel downloaded");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(false);
    }
  }

  return (
    <div className="space-y-6 pb-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Export currency audit</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Finds export credit and debit notes where INR is posted but the stored foreign amount
          (<code className="text-xs">currencyValue</code>) is zero or only a currency symbol.
          Intercompany ledgers are always excluded.
          Intercompany export ledgers are excluded.
        </p>
      </div>

      <section className="rounded-2xl border border-border bg-card p-4 shadow-sm md:p-5">
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
          <div className="lg:col-span-2">
            <Label htmlFor="company">Company</Label>
            <Select value={companyValue} onValueChange={setCompanyValue} disabled={loadingCompanies}>
              <SelectTrigger id="company" className="mt-1.5">
                <SelectValue placeholder="Select company" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL_COMPANIES}>All companies</SelectItem>
                {companies.map((c) => (
                  <SelectItem key={c.value} value={c.value}>
                    {c.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="dateFrom">From</Label>
            <Input
              id="dateFrom"
              type="date"
              className="mt-1.5"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="dateTo">To</Label>
            <Input
              id="dateTo"
              type="date"
              className="mt-1.5"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="minInr">Min INR</Label>
            <Input
              id="minInr"
              type="number"
              min={0}
              className="mt-1.5"
              value={minInr}
              onChange={(e) => setMinInr(e.target.value)}
            />
          </div>
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          <Button onClick={handleRun} disabled={running}>
            {running ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Search className="mr-2 h-4 w-4" />}
            Run audit
          </Button>
          <Button
            variant="outline"
            onClick={handleExport}
            disabled={exporting || !result || result.totalCount === 0}
          >
            {exporting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Download className="mr-2 h-4 w-4" />}
            Export Excel
          </Button>
        </div>
      </section>

      {result && (
        <>
          <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Documents flagged" value={String(result.totalCount)} />
            <StatCard label="Credit notes" value={String(result.creditNoteCount)} />
            <StatCard label="Debit notes" value={String(result.debitNoteCount)} />
            <StatCard label="Total INR flagged" value={`₹ ${formatInrAmount(result.totalInrAmount)}`} />
          </section>

          <section className="rounded-2xl border border-border bg-card shadow-sm">
            <div className="flex flex-col gap-3 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="font-semibold">Results</h2>
                <p className="text-xs text-muted-foreground">
                  {companyLabel} · {result.dateFrom} to {result.dateTo}
                </p>
              </div>
              <div className="relative w-full sm:max-w-xs">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Filter rows…"
                  className="pl-9"
                />
              </div>
            </div>

            {filteredItems.length === 0 ? (
              <p className="p-8 text-center text-sm text-muted-foreground">No rows match your filter.</p>
            ) : (
              <>
                <div className="hidden overflow-x-auto md:block">
                  <table className="w-full text-sm">
                    <TableHeader>
                      <TableRow>
                        <TableHead>Type</TableHead>
                        <TableHead>Document</TableHead>
                        <TableHead>Date</TableHead>
                        <TableHead>Company / party</TableHead>
                        <TableHead>Export ledger</TableHead>
                        <TableHead className="text-right">INR</TableHead>
                        <TableHead>Stored FC</TableHead>
                        <TableHead className="text-right">Calc. FC</TableHead>
                        <TableHead>Issue</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredItems.map((row) => (
                        <AuditRow key={`${row.documentType}-${row.documentNo}-${row.companyName}`} row={row} />
                      ))}
                    </TableBody>
                  </table>
                </div>
                <div className="space-y-3 p-3 md:hidden">
                  {filteredItems.map((row) => (
                    <MobileAuditCard key={`${row.documentType}-${row.documentNo}-${row.companyName}`} row={row} />
                  ))}
                </div>
              </>
            )}
          </section>
        </>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-semibold tabular-nums">{value}</p>
    </div>
  );
}

function AuditRow({ row }: { row: ExportCurrencyAuditItem }) {
  return (
    <TableRow>
      <TableCell>{row.documentType}</TableCell>
      <TableCell className="font-medium">{row.documentNo}</TableCell>
      <TableCell>{row.documentDate || "—"}</TableCell>
      <TableCell>
        <div className="max-w-[14rem] truncate">{row.companyName}</div>
        <div className="max-w-[14rem] truncate text-xs text-muted-foreground">{row.partyName}</div>
      </TableCell>
      <TableCell className="max-w-[12rem] truncate">{row.ledgerName}</TableCell>
      <TableCell className="text-right tabular-nums">₹ {formatInrAmount(row.inrAmount)}</TableCell>
      <TableCell>
        <span className="inline-flex items-center gap-1 text-amber-700 dark:text-amber-400">
          <AlertTriangle className="h-3.5 w-3.5 shrink-0" />
          {row.storedFc || "—"}
        </span>
      </TableCell>
      <TableCell className="text-right tabular-nums">
        {formatFcAmount(row.calculatedFc, row.currency)}
      </TableCell>
      <TableCell className="max-w-[16rem] text-xs text-muted-foreground">{row.issue}</TableCell>
    </TableRow>
  );
}

function MobileAuditCard({ row }: { row: ExportCurrencyAuditItem }) {
  return (
    <article className="rounded-xl border border-border bg-background p-3.5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-xs font-medium text-muted-foreground">{row.documentType}</p>
          <h3 className="truncate font-semibold">{row.documentNo}</h3>
          <p className="text-xs text-muted-foreground">{row.documentDate}</p>
        </div>
        <div className="text-right text-sm font-semibold tabular-nums">₹ {formatInrAmount(row.inrAmount)}</div>
      </div>
      <dl className="mt-3 space-y-1 text-xs">
        <div>
          <dt className="text-muted-foreground">Party</dt>
          <dd className="font-medium">{row.partyName}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Ledger</dt>
          <dd>{row.ledgerName}</dd>
        </div>
        <div className="flex justify-between gap-2 pt-1">
          <span className="text-amber-700 dark:text-amber-400">Stored: {row.storedFc || "—"}</span>
          <span className="tabular-nums">Calc: {formatFcAmount(row.calculatedFc, row.currency)}</span>
        </div>
        <p className="pt-1 text-muted-foreground">{row.issue}</p>
      </dl>
    </article>
  );
}
