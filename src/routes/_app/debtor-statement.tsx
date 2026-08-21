import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, Download, Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import { DatePickerField } from "@/components/DatePickerField";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { downloadDebtorStatementExcel, getDebtorStatementCompanies, queryDebtorStatement } from "@/lib/debtor-statement-api";
import {
  DEFAULT_DEBTOR_COMPANY,
  formatInr,
  formatInrCr,
  previousMonthEnd,
  type DebtorCompanyOption,
  type DebtorStatementQuery,
  type DebtorStatementResult,
} from "@/lib/debtor-statement-types";
import { formatShortDate } from "@/lib/utils";

export const Route = createFileRoute("/_app/debtor-statement")({
  head: () => ({ meta: [{ title: "Debtor Statement — PO Portal" }] }),
  component: DebtorStatementPage,
});

function DebtorStatementPage() {
  const [companies, setCompanies] = useState<DebtorCompanyOption[]>([]);
  const [company, setCompany] = useState(DEFAULT_DEBTOR_COMPANY);
  const [asOn, setAsOn] = useState(previousMonthEnd());
  const [includeCurrentAssets, setIncludeCurrentAssets] = useState(true);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState<DebtorStatementResult | null>(null);
  const [search, setSearch] = useState("");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingCompanies(true);
      try {
        const next = await getDebtorStatementCompanies();
        if (!cancelled) {
          setCompanies(next);
          if (next.length && !next.some((c) => c.value === company)) {
            const pil = next.find((c) => c.value === DEFAULT_DEBTOR_COMPANY);
            setCompany(pil?.value || next[0].value);
          }
        }
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

  const query: DebtorStatementQuery = {
    company,
    asOn,
    includeCurrentAssets,
  };

  async function run() {
    setLoading(true);
    try {
      const next = await queryDebtorStatement(query);
      setResult(next);
      toast.success(`As-on pack ready — ${next.kpis.openBillCount} open bills`);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to build debtor statement");
    } finally {
      setLoading(false);
    }
  }

  async function exportExcel() {
    setExporting(true);
    try {
      await downloadDebtorStatementExcel(query);
      toast.success("Excel downloaded");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(false);
    }
  }

  const q = search.trim().toLowerCase();
  const bills = useMemo(() => {
    const rows = result?.bills ?? [];
    if (!q) return rows;
    return rows.filter((r) =>
      [r.partyName, r.invoiceNo, r.gstin, r.type, r.status, r.companyName]
        .join(" ")
        .toLowerCase()
        .includes(q),
    );
  }, [result, q]);

  const pivot = useMemo(() => {
    const rows = result?.pivot ?? [];
    if (!q) return rows;
    return rows.filter((r) =>
      [r.partyName, r.gstin, r.type, r.status].join(" ").toLowerCase().includes(q),
    );
  }, [result, q]);

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to="/ledgers"
            className="mb-2 inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Ledgers
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Debtor statement</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Drawing-power pack as on a freeze date. Subsequent receipts are ignored. Extra invoices
            stay on the grid; LIFO is applied as a visible allocation.
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          disabled={exporting || loading || !result}
          onClick={() => void exportExcel()}
        >
          {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
          Export Excel
        </Button>
      </div>

      <section className="rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="space-y-1.5">
            <Label>Company / group</Label>
            <Select value={company} onValueChange={setCompany} disabled={loadingCompanies || loading}>
              <SelectTrigger>
                <SelectValue placeholder={loadingCompanies ? "Loading…" : "Select company"} />
              </SelectTrigger>
              <SelectContent>
                {companies.map((c) => (
                  <SelectItem key={c.value} value={c.value}>
                    {c.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <Label>As on</Label>
            <DatePickerField value={asOn} onChange={setAsOn} disabled={loading} />
          </div>
          <div className="flex items-end">
            <label className="flex items-center gap-2 pb-2 text-sm">
              <Checkbox
                checked={includeCurrentAssets}
                onCheckedChange={(v) => setIncludeCurrentAssets(v === true)}
                disabled={loading}
              />
              Include GST + advance licence (G)
            </label>
          </div>
          <div className="flex items-end">
            <Button type="button" className="w-full" disabled={loading || !company || !asOn} onClick={() => void run()}>
              {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {loading ? "Building as-on pack…" : "Run statement"}
            </Button>
          </div>
        </div>
        {loading ? (
          <p className="mt-3 text-xs text-muted-foreground">
            Matching bill-wise outstanding to ledger closing. This can take a minute for a full group.
          </p>
        ) : null}
      </section>

      {result ? (
        <>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Kpi label="Net debtors" value={formatInrCr(result.kpis.netTotal)} hint={`${result.kpis.openBillCount} open bills`} />
            <Kpi label="As per book" value={formatInrCr(result.kpis.bookTotal)} hint={`${result.kpis.partyCount} parties`} />
            <Kpi
              label="LIFO allocated"
              value={formatInrCr(result.kpis.allocatedTotal)}
              hint={`${result.kpis.lifoPartyCount} parties`}
            />
            <Kpi
              label="Remaining diff"
              value={formatInrCr(result.kpis.diffTotal)}
              hint={`${result.kpis.nonBillGapPartyCount} non-bill gaps`}
            />
          </div>

          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Search party, invoice, GSTIN…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <Tabs defaultValue="bills">
            <TabsList>
              <TabsTrigger value="bills">Bill-wise ({bills.length})</TabsTrigger>
              <TabsTrigger value="pivot">Pivot ({pivot.length})</TabsTrigger>
              <TabsTrigger value="book">Book debts</TabsTrigger>
            </TabsList>

            <TabsContent value="bills">
              <div className="overflow-auto rounded-xl border border-border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Type</TableHead>
                      <TableHead>Cat</TableHead>
                      <TableHead>Party</TableHead>
                      <TableHead>Invoice</TableHead>
                      <TableHead>Date</TableHead>
                      <TableHead className="text-right">Original</TableHead>
                      <TableHead className="text-right">Allocated</TableHead>
                      <TableHead className="text-right">Net</TableHead>
                      <TableHead className="text-right">Days</TableHead>
                      <TableHead>Ageing</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {bills.slice(0, 500).map((row, i) => (
                      <TableRow key={`${row.partyName}-${row.invoiceNo}-${i}`}>
                        <TableCell>{row.type}</TableCell>
                        <TableCell>{row.category}</TableCell>
                        <TableCell className="max-w-[220px] truncate" title={row.partyName}>
                          {row.partyName}
                        </TableCell>
                        <TableCell className="whitespace-nowrap">{row.invoiceNo}</TableCell>
                        <TableCell className="whitespace-nowrap">{formatShortDate(row.invoiceDate)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.originalAmount)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.allocatedAmount)}</TableCell>
                        <TableCell className="text-right tabular-nums font-medium">{formatInr(row.netAmount)}</TableCell>
                        <TableCell className="text-right tabular-nums">{row.days}</TableCell>
                        <TableCell className="whitespace-nowrap">{row.ageing}</TableCell>
                        <TableCell className="whitespace-nowrap text-muted-foreground">{row.status}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                {bills.length > 500 ? (
                  <p className="px-3 py-2 text-xs text-muted-foreground">
                    Showing first 500 of {bills.length}. Export Excel for the full pack.
                  </p>
                ) : null}
              </div>
            </TabsContent>

            <TabsContent value="pivot">
              <div className="overflow-auto rounded-xl border border-border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Party</TableHead>
                      <TableHead className="text-right">0–120</TableHead>
                      <TableHead className="text-right">1–90</TableHead>
                      <TableHead className="text-right">91–120</TableHead>
                      <TableHead className="text-right">121–180</TableHead>
                      <TableHead className="text-right">&gt;180</TableHead>
                      <TableHead className="text-right">Total</TableHead>
                      <TableHead className="text-right">As per book</TableHead>
                      <TableHead className="text-right">Diff</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pivot.map((row) => (
                      <TableRow key={row.partyName}>
                        <TableCell className="max-w-[240px] truncate" title={row.partyName}>
                          {row.partyName}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.zeroTo120)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.oneTo90)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.ninetyOneTo120)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.oneTwentyOneTo180)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.over180)}</TableCell>
                        <TableCell className="text-right tabular-nums font-medium">{formatInr(row.grandTotal)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.asPerBook)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.diff)}</TableCell>
                        <TableCell className="whitespace-nowrap text-muted-foreground">{row.status}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </TabsContent>

            <TabsContent value="book">
              <div className="overflow-auto rounded-xl border border-border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Bucket</TableHead>
                      <TableHead className="text-right">Government (G)</TableHead>
                      <TableHead className="text-right">Associates (R)</TableHead>
                      <TableHead className="text-right">Other (O)</TableHead>
                      <TableHead className="text-right">Total</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(result.bookDebts ?? []).map((row) => (
                      <TableRow key={row.bucket}>
                        <TableCell>{row.bucket}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.government)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.associates)}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatInr(row.other)}</TableCell>
                        <TableCell className="text-right tabular-nums font-medium">{formatInr(row.total)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </TabsContent>
          </Tabs>
        </>
      ) : null}
    </div>
  );
}

function Kpi({ label, value, hint }: { label: string; value: string; hint: string }) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-semibold tabular-nums">{value}</p>
      <p className="mt-0.5 text-xs text-muted-foreground">{hint}</p>
    </div>
  );
}
