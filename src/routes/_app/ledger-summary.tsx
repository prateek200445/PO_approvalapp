import { createFileRoute, Link } from "@tanstack/react-router";
import { memo, useCallback, useEffect, useMemo, useState } from "react";
import { ArrowLeft, ChevronDown, Download, Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import { getApiUrl } from "@/lib/api-config";
import { cn } from "@/lib/utils";
import { useDebounce } from "@/hooks/useDebounce";
import { useIsMobile } from "@/hooks/use-mobile";
import { MultiSelect } from "@/components/MultiSelect";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import {
  financialYearStart,
  toInputDate,
  type LedgerCompanyOption,
  type LedgerNameOption,
  type LedgerSummaryResult,
  type LedgerSummaryRow,
} from "@/lib/ledger-summary-types";

export const Route = createFileRoute("/_app/ledger-summary")({
  head: () => ({ meta: [{ title: "Ledger Summary — PO Portal" }] }),
  component: LedgerSummaryPage,
});

const PAGE_SIZE = 20;

type AppliedContext = {
  companyLabels: string[];
  ledgerNames: string[];
  dateFrom: string;
  dateTo: string;
};

type LedgerSectionData = {
  id: string;
  ledgerName: string;
  companyNames: string[];
  rows: LedgerSummaryRow[];
  opening: number;
  debitTotal: number;
  creditTotal: number;
  closing: number;
  debitEntries: number;
  creditEntries: number;
};

function LedgerSummaryPage() {
  const isMobile = useIsMobile();
  const [companies, setCompanies] = useState<LedgerCompanyOption[]>([]);
  const [ledgers, setLedgers] = useState<LedgerNameOption[]>([]);
  const [companyValues, setCompanyValues] = useState<string[]>([]);
  const [ledgerNames, setLedgerNames] = useState<string[]>([]);
  const [dateFrom, setDateFrom] = useState(financialYearStart());
  const [dateTo, setDateTo] = useState(toInputDate(new Date()));
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingLedgers, setLoadingLedgers] = useState(false);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState<LedgerSummaryResult | null>(null);
  const [applied, setApplied] = useState<AppliedContext | null>(null);

  const debouncedCompanyValues = useDebounce(companyValues, 280);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingCompanies(true);
      try {
        const res = await fetch(getApiUrl("/api/ledger-summary/companies"));
        const data = await res.json();
        if (!res.ok) throw new Error(data.message || "Failed to load companies");
        if (!cancelled) setCompanies(normalizeCompanies(data));
      } catch (err: any) {
        if (!cancelled) toast.error(err.message || "Failed to load companies");
      } finally {
        if (!cancelled) setLoadingCompanies(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (debouncedCompanyValues.length === 0) {
      setLedgers([]);
      setLedgerNames([]);
      setLoadingLedgers(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoadingLedgers(true);
      try {
        const res = await fetch(
          getApiUrl(
            `/api/ledger-summary/ledgers?companies=${encodeURIComponent(debouncedCompanyValues.join(","))}`,
          ),
        );
        const data = await res.json();
        if (!res.ok) throw new Error(data.message || "Failed to load ledgers");
        const next = normalizeLedgers(data);
        if (!cancelled) {
          setLedgers(next);
          const allowed = new Set(next.map((l) => l.ledgerName));
          setLedgerNames((prev) => prev.filter((n) => allowed.has(n)));
        }
      } catch (err: any) {
        if (!cancelled) toast.error(err.message || "Failed to load ledgers");
      } finally {
        if (!cancelled) setLoadingLedgers(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [debouncedCompanyValues]);

  const ledgersPending =
    loadingLedgers || companyValues.join("\0") !== debouncedCompanyValues.join("\0");
  const selectedCompanies = useMemo(
    () => companies.filter((c) => companyValues.includes(c.value)),
    [companies, companyValues],
  );

  const companyOptions = useMemo(
    () => companies.map((c) => ({ value: c.value, label: c.label })),
    [companies],
  );

  const ledgerOptions = useMemo(
    () => ledgers.map((l) => ({ value: l.ledgerName, label: l.ledgerName })),
    [ledgers],
  );

  const entryCounts = useMemo(() => {
    if (!result) return { debit: 0, credit: 0 };
    let debit = 0;
    let credit = 0;
    for (const r of result.rows) {
      if (r.isOpening) continue;
      if (r.debit > 0) debit += 1;
      if (r.credit > 0) credit += 1;
    }
    return { debit, credit };
  }, [result]);

  async function runQuery() {
    if (selectedCompanies.length === 0) {
      toast.error("Select at least one company");
      return;
    }
    if (ledgerNames.length === 0) {
      toast.error("Select at least one ledger");
      return;
    }
    if (selectedCompanies.length * ledgerNames.length > 40) {
      toast.error("Too many combinations (max 40). Narrow your selection.");
      return;
    }

    setLoading(true);
    try {
      const res = await fetch(getApiUrl("/api/ledger-summary/query-batch"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          companies: selectedCompanies.map((c) => ({
            value: c.value,
            label: c.label,
            companyType: c.companyType,
            companyName: c.companyName,
            companyId: c.companyId,
          })),
          ledgerNames,
          dateFrom,
          dateTo,
          currency: null,
          interestCal: 0,
        }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to load ledger summary");
      const normalized = normalizeResult(data);
      setResult(normalized);
      setApplied({
        companyLabels: selectedCompanies.map((c) => c.label),
        ledgerNames: [...ledgerNames],
        dateFrom,
        dateTo,
      });
      toast.success(`Loaded ${normalized.rows.length.toLocaleString("en-IN")} transactions`);
    } catch (err: any) {
      toast.error(err.message || "Query failed");
    } finally {
      setLoading(false);
    }
  }

  async function exportExcel() {
    if (!result || !applied || result.rows.length === 0) {
      toast.error("Nothing to export");
      return;
    }
    setExporting(true);
    try {
      const res = await fetch(getApiUrl("/api/ledger-summary/export"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          result,
          dateFrom: applied.dateFrom,
          dateTo: applied.dateTo,
          companyLabels: applied.companyLabels,
          ledgerNames: applied.ledgerNames,
        }),
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Export failed");
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `ledger-summary-${applied.dateFrom}-${applied.dateTo}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success("Excel downloaded");
    } catch (err: any) {
      toast.error(err.message || "Export failed");
    } finally {
      setExporting(false);
    }
  }

  const fieldClass =
    "h-9 w-full rounded-md border border-border/70 bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20";

  const identitySubtitle = applied
    ? [
        `${applied.companyLabels.length} compan${applied.companyLabels.length === 1 ? "y" : "ies"}`,
        `${applied.ledgerNames.length} ledger${applied.ledgerNames.length === 1 ? "" : "s"}`,
        `${formatDateLong(applied.dateFrom)} – ${formatDateLong(applied.dateTo)}`,
      ].join(" · ")
    : null;

  const identityTitle =
    applied == null
      ? null
      : applied.ledgerNames.length === 1
        ? applied.ledgerNames[0]
        : `${applied.ledgerNames.length} ledgers`;

  return (
    <div className="space-y-4 pb-8">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <Link
            to="/ledgers"
            className="mb-2 inline-flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Ledgers
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Ledger Summary</h1>
          {applied ? (
            <div className="mt-1.5 space-y-0.5">
              <p className="truncate text-base font-medium text-foreground">{identityTitle}</p>
              <p className="text-sm text-muted-foreground">{identitySubtitle}</p>
              {applied.companyLabels.length === 1 && (
                <p className="truncate text-sm text-muted-foreground">{applied.companyLabels[0]}</p>
              )}
            </div>
          ) : (
            <p className="mt-1 text-sm text-muted-foreground">
              Multi-select companies and ledgers. Results show one table per ledger.
            </p>
          )}
        </div>
        {result && (
          <button
            type="button"
            onClick={exportExcel}
            disabled={exporting}
            className="inline-flex h-9 shrink-0 items-center gap-1.5 rounded-md border border-border/70 bg-surface px-3 text-sm font-medium hover:bg-secondary disabled:opacity-60"
          >
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Export Excel
          </button>
        )}
      </div>

      <div className="rounded-xl border border-border/60 bg-card px-3 py-3 sm:px-4">
        <div className="flex flex-col gap-2.5 xl:flex-row xl:items-end">
          <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm">
            <span className="text-xs text-muted-foreground">Companies</span>
            <MultiSelect
              options={companyOptions}
              values={companyValues}
              onChange={setCompanyValues}
              placeholder={loadingCompanies ? "Loading…" : "Select companies"}
              disabled={loadingCompanies}
              searchPlaceholder="Search company…"
            />
          </label>

          <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm">
            <span className="text-xs text-muted-foreground">Ledgers</span>
            <MultiSelect
              options={ledgerOptions}
              values={ledgerNames}
              onChange={setLedgerNames}
              placeholder={
                companyValues.length === 0
                  ? "Select companies first"
                  : ledgersPending
                    ? "Loading ledgers…"
                    : "Select ledgers"
              }
              disabled={companyValues.length === 0 || ledgersPending}
              searchPlaceholder="Search ledger…"
              emptyText="No ledgers for selected companies"
            />
          </label>

          <div className="flex min-w-0 flex-[1.1] flex-col gap-1 text-sm">
            <span className="text-xs text-muted-foreground">Date range</span>
            <div className="flex items-center gap-2">
              <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} className={fieldClass} />
              <span className="shrink-0 text-muted-foreground">→</span>
              <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} className={fieldClass} />
            </div>
          </div>

          <button
            type="button"
            onClick={runQuery}
            disabled={loading || companyValues.length === 0 || ledgerNames.length === 0}
            className="inline-flex h-9 shrink-0 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Apply Filters
          </button>
        </div>
        {(companyValues.length > 0 || ledgerNames.length > 0) && (
          <p className="mt-2 text-[11px] text-muted-foreground">
            {companyValues.length} company × {ledgerNames.length} ledger ={" "}
            {ledgerNames.length} ledger table{ledgerNames.length === 1 ? "" : "s"}
            {companyValues.length * ledgerNames.length > 40 ? " (query over limit — max 40 pairs)" : ""}
          </p>
        )}
      </div>

      {result && (
        <LedgerResultsPanel result={result} entryCounts={entryCounts} isMobile={isMobile} />
      )}
    </div>
  );
}

const LedgerResultsPanel = memo(function LedgerResultsPanel({
  result,
  entryCounts,
  isMobile,
}: {
  result: LedgerSummaryResult;
  entryCounts: { debit: number; credit: number };
  isMobile: boolean;
}) {
  const [q, setQ] = useState("");
  const debouncedQ = useDebounce(q, 250);
  const [openMap, setOpenMap] = useState<Record<string, boolean>>({});

  const filteredRows = useMemo(() => {
    const query = debouncedQ.trim().toLowerCase();
    if (!query) return result.rows;
    return result.rows.filter((r) =>
      [r.particulars, r.voucherType, r.voucherNo, r.voucherRef, r.companyName, r.ledgerName]
        .filter(Boolean)
        .join(" ")
        .toLowerCase()
        .includes(query),
    );
  }, [result.rows, debouncedQ]);

  const sections = useMemo(() => buildSections(filteredRows), [filteredRows]);

  // Collapse all by default when a new result set arrives
  useEffect(() => {
    const next: Record<string, boolean> = {};
    for (const s of buildSections(result.rows)) next[s.id] = false;
    setOpenMap(next);
    setQ("");
  }, [result]);

  const handleOpenChange = useCallback((id: string, open: boolean) => {
    setOpenMap((prev) => ({ ...prev, [id]: open }));
  }, []);

  function setAllOpen(open: boolean) {
    const next: Record<string, boolean> = {};
    for (const s of sections) next[s.id] = open;
    setOpenMap(next);
  }

  return (
    <>
      <div className="grid gap-2.5 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryTile label="Opening (combined)" value={result.openingBalance} />
        <SummaryTile
          label="Total Debit"
          value={result.debitTotal}
          hint={`${entryCounts.debit.toLocaleString("en-IN")} entries`}
        />
        <SummaryTile
          label="Total Credit"
          value={result.creditTotal}
          hint={`${entryCounts.credit.toLocaleString("en-IN")} entries`}
        />
        <SummaryTile label="Closing (combined)" value={result.closingBalance} emphasis />
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-md">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search within statements…"
            className="h-9 w-full rounded-md border border-border/70 bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
        <div className="flex flex-wrap items-center gap-2 text-xs">
          <span className="text-muted-foreground">
            {sections.length} ledger{sections.length === 1 ? "" : "s"} ·{" "}
            {filteredRows.length.toLocaleString("en-IN")} txns
          </span>
          {sections.length > 1 && (
            <>
              <button
                type="button"
                onClick={() => setAllOpen(true)}
                className="rounded-md border border-border/70 bg-surface px-2 py-1 font-medium hover:bg-secondary"
              >
                Expand all
              </button>
              <button
                type="button"
                onClick={() => setAllOpen(false)}
                className="rounded-md border border-border/70 bg-surface px-2 py-1 font-medium hover:bg-secondary"
              >
                Collapse all
              </button>
            </>
          )}
        </div>
      </div>

      {sections.length === 0 ? (
        <EmptyRows />
      ) : (
        <div className="space-y-3.5">
          {sections.map((section) => (
            <LedgerStatementSection
              key={section.id}
              section={section}
              open={openMap[section.id] ?? false}
              onOpenChange={handleOpenChange}
              isMobile={isMobile}
            />
          ))}
        </div>
      )}
    </>
  );
});

const LedgerStatementSection = memo(function LedgerStatementSection({
  section,
  open,
  onOpenChange,
  isMobile,
}: {
  section: LedgerSectionData;
  open: boolean;
  onOpenChange: (id: string, open: boolean) => void;
  isMobile: boolean;
}) {
  const [page, setPage] = useState(1);
  const totalRows = section.rows.length;
  const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));

  useEffect(() => {
    setPage(1);
  }, [section.id, totalRows, open]);

  const safePage = Math.min(page, totalPages);
  const pageRows = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return section.rows.slice(start, start + PAGE_SIZE);
  }, [section.rows, safePage]);

  const rangeStart = totalRows === 0 ? 0 : (safePage - 1) * PAGE_SIZE + 1;
  const rangeEnd = Math.min(safePage * PAGE_SIZE, totalRows);

  return (
    <Collapsible open={open} onOpenChange={(next) => onOpenChange(section.id, next)}>
      <div
        className={cn(
          "overflow-hidden rounded-xl border bg-card transition-all duration-200 ease-out",
          open
            ? "border-primary/35 shadow-md shadow-black/10 ring-1 ring-primary/15"
            : "border-border/70 shadow-sm shadow-black/5 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-lg hover:shadow-black/15",
        )}
      >
        <CollapsibleTrigger asChild>
          <button
            type="button"
            className={cn(
              "flex w-full items-start gap-3 px-4 py-3.5 text-left transition-colors duration-200",
              open ? "bg-secondary/25" : "hover:bg-secondary/45",
            )}
          >
            <ChevronDown
              className={cn(
                "mt-0.5 h-4 w-4 shrink-0 text-muted-foreground transition-transform duration-200",
                open ? "rotate-0 text-foreground" : "-rotate-90",
              )}
            />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-semibold tracking-tight">{section.ledgerName || "Ledger"}</div>
              <div className="mt-0.5 truncate text-xs text-muted-foreground">
                {formatCompanySubtitle(section.companyNames)}
                <span className="mx-1.5 text-border">·</span>
                {section.rows.length.toLocaleString("en-IN")} transactions
              </div>
              <div className="mt-2.5 flex flex-wrap gap-x-4 gap-y-1 text-[11px] tabular-nums text-muted-foreground">
                <span>
                  Opening{" "}
                  <span className={cn("font-medium", balanceTone(section.opening))}>
                    {formatSignedMoney(section.opening)}
                  </span>
                </span>
                <span>
                  Debit{" "}
                  <span className="font-medium text-foreground">{formatSignedMoney(section.debitTotal)}</span>
                  <span className="ml-1 opacity-70">({section.debitEntries})</span>
                </span>
                <span>
                  Credit{" "}
                  <span className="font-medium text-foreground">{formatSignedMoney(section.creditTotal)}</span>
                  <span className="ml-1 opacity-70">({section.creditEntries})</span>
                </span>
                <span>
                  Closing{" "}
                  <span className={cn("font-semibold", balanceTone(section.closing))}>
                    {formatSignedMoney(section.closing)}
                  </span>
                </span>
              </div>
            </div>
          </button>
        </CollapsibleTrigger>

        {open ? (
          <CollapsibleContent>
            <div className="border-t border-border/60">
              {totalRows === 0 ? (
                <p className="px-4 py-6 text-center text-sm text-muted-foreground">No matching rows</p>
              ) : (
                <>
                  {isMobile ? (
                    <div className="space-y-2 p-3">
                      {pageRows.map((row, idx) => (
                        <MobileRow
                          key={`${row.companyName}-${row.voucherNo}-${row.date}-${rangeStart + idx}`}
                          row={row}
                          showCompany={section.companyNames.length > 1}
                        />
                      ))}
                    </div>
                  ) : (
                    <div className="overflow-auto">
                      <table className="min-w-[900px] w-full text-sm">
                        <thead className="sticky top-0 z-10 border-b border-border bg-card text-xs uppercase tracking-wide text-muted-foreground">
                          <tr>
                            {section.companyNames.length > 1 && (
                              <th className="bg-card px-3 py-2 text-left font-medium">Company</th>
                            )}
                            <th className="bg-card px-3 py-2 text-left font-medium">Date</th>
                            <th className="bg-card px-3 py-2 text-left font-medium">Particulars</th>
                            <th className="bg-card px-3 py-2 text-left font-medium">Voucher</th>
                            <th className="bg-card px-3 py-2 text-right font-medium">Debit</th>
                            <th className="bg-card px-3 py-2 text-right font-medium">Credit</th>
                            <th className="bg-card px-3 py-2 text-right font-medium">Running Balance</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border/70">
                          {pageRows.map((row, idx) => (
                            <DesktopRow
                              key={`${row.companyName}-${row.voucherNo}-${row.date}-${rangeStart + idx}`}
                              row={row}
                              showCompany={section.companyNames.length > 1}
                            />
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  {totalPages > 1 && (
                    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border/60 px-3 py-2 text-xs">
                      <span className="text-muted-foreground">
                        {rangeStart}–{rangeEnd} of {totalRows.toLocaleString("en-IN")}
                      </span>
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          disabled={safePage <= 1}
                          onClick={() => setPage((p) => Math.max(1, p - 1))}
                          className="rounded-md border border-border/70 bg-surface px-2.5 py-1 font-medium hover:bg-secondary disabled:opacity-40"
                        >
                          Prev
                        </button>
                        <span className="tabular-nums text-muted-foreground">
                          {safePage} / {totalPages}
                        </span>
                        <button
                          type="button"
                          disabled={safePage >= totalPages}
                          onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                          className="rounded-md border border-border/70 bg-surface px-2.5 py-1 font-medium hover:bg-secondary disabled:opacity-40"
                        >
                          Next
                        </button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </CollapsibleContent>
        ) : null}
      </div>
    </Collapsible>
  );
});

const MobileRow = memo(function MobileRow({
  row,
  showCompany,
}: {
  row: LedgerSummaryRow;
  showCompany: boolean;
}) {
  return (
    <div
      className={cn("rounded-lg border border-border/50 bg-surface/40 p-3", row.isOpening && "bg-secondary/30")}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          {showCompany ? (
            <div className="mb-0.5 truncate text-[11px] text-muted-foreground">{row.companyName || "—"}</div>
          ) : null}
          <div className="truncate text-sm font-medium">{row.particulars || "—"}</div>
          <div className="mt-0.5 text-xs text-muted-foreground">
            {formatDateLong(row.date)}
            {row.voucherType ? ` · ${row.voucherType}` : ""}
            {row.voucherNo ? ` · ${row.voucherNo}` : ""}
          </div>
        </div>
        <div className="shrink-0 text-right text-sm tabular-nums">
          {row.debit > 0 && <div>{formatMoney(row.debit)}</div>}
          {row.credit > 0 && <div className="text-muted-foreground">{formatMoney(row.credit)}</div>}
        </div>
      </div>
      <div className="mt-2 flex justify-between text-xs">
        <span className="text-muted-foreground">Running balance</span>
        <span className={cn("font-semibold tabular-nums", balanceTone(row.closing))}>
          {formatSignedMoney(row.closing)}
        </span>
      </div>
    </div>
  );
});

const DesktopRow = memo(function DesktopRow({
  row,
  showCompany,
}: {
  row: LedgerSummaryRow;
  showCompany: boolean;
}) {
  return (
    <tr className={cn("hover:bg-secondary/30", row.isOpening && "bg-secondary/25")}>
      {showCompany ? (
        <td className="max-w-[180px] px-3 py-2 text-xs text-muted-foreground">
          <div className="truncate" title={row.companyName}>
            {row.companyName || "—"}
          </div>
        </td>
      ) : null}
      <td className="whitespace-nowrap px-3 py-2 text-muted-foreground">{formatDateLong(row.date)}</td>
      <td className="max-w-[300px] px-3 py-2">
        <div className="truncate font-medium" title={row.particulars}>
          {row.particulars || "—"}
        </div>
        {row.voucherRef ? (
          <div className="mt-0.5 truncate text-[11px] text-muted-foreground" title={row.voucherRef}>
            Ref {row.voucherRef}
          </div>
        ) : null}
      </td>
      <td className="px-3 py-2">
        <div>{row.voucherType || "—"}</div>
        <div className="mt-0.5 text-[11px] text-muted-foreground">{row.voucherNo || "—"}</div>
      </td>
      <td className="px-3 py-2 text-right tabular-nums">
        {row.debit > 0 ? formatMoney(row.debit) : <span className="text-muted-foreground/40">—</span>}
      </td>
      <td className="px-3 py-2 text-right tabular-nums">
        {row.credit > 0 ? formatMoney(row.credit) : <span className="text-muted-foreground/40">—</span>}
      </td>
      <td className={cn("px-3 py-2 text-right text-[13px] font-semibold tabular-nums", balanceTone(row.closing))}>
        {formatSignedMoney(row.closing)}
      </td>
    </tr>
  );
});

function buildSections(rows: LedgerSummaryRow[]): LedgerSectionData[] {
  const map = new Map<string, LedgerSummaryRow[]>();
  for (const row of rows) {
    const ledger = (row.ledgerName || "").trim() || "—";
    const id = ledger.toLowerCase();
    if (!map.has(id)) map.set(id, []);
    map.get(id)!.push(row);
  }

  return [...map.entries()]
    .map(([id, sectionRows]) => {
      // Shared stream per ledger: openings first, then chronological (not per company)
      const ordered = [...sectionRows].sort((a, b) => {
        if (a.isOpening !== b.isOpening) return a.isOpening ? -1 : 1;
        const da = a.date ? new Date(a.date).getTime() : 0;
        const db = b.date ? new Date(b.date).getTime() : 0;
        if (da !== db) return da - db;
        return (a.voucherNo || "").localeCompare(b.voucherNo || "");
      });

      const companyNames = [
        ...new Set(ordered.map((r) => (r.companyName || "").trim() || "—")),
      ].sort((a, b) => a.localeCompare(b));

      let opening = 0;
      let debitTotal = 0;
      let creditTotal = 0;
      let debitEntries = 0;
      let creditEntries = 0;

      for (const r of ordered) {
        if (r.isOpening) {
          opening += r.debit - r.credit;
          continue;
        }
        if (r.debit > 0) {
          debitTotal += r.debit;
          debitEntries += 1;
        }
        if (r.credit > 0) {
          creditTotal += r.credit;
          creditEntries += 1;
        }
      }

      const closing = ordered.length ? ordered[ordered.length - 1].closing : 0;

      return {
        id,
        ledgerName: ordered[0]?.ledgerName || "—",
        companyNames,
        rows: ordered,
        opening,
        debitTotal,
        creditTotal,
        closing,
        debitEntries,
        creditEntries,
      };
    })
    .sort((a, b) => a.ledgerName.localeCompare(b.ledgerName));
}

function formatCompanySubtitle(companyNames: string[]) {
  if (companyNames.length === 0) return "—";
  if (companyNames.length === 1) return companyNames[0];
  if (companyNames.length === 2) return companyNames.join(" · ");
  return `${companyNames.length} companies`;
}

function SummaryTile({
  label,
  value,
  hint,
  emphasis,
}: {
  label: string;
  value: number;
  hint?: string;
  emphasis?: boolean;
}) {
  return (
    <div
      className={cn(
        "rounded-xl border border-border/60 bg-card px-4 py-3",
        emphasis && "border-border bg-secondary/20",
      )}
    >
      <div className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className={cn("mt-1 text-lg font-semibold tabular-nums", balanceTone(value))}>
        {formatSignedMoney(value)}
      </div>
      {hint ? <div className="mt-0.5 text-[11px] text-muted-foreground">{hint}</div> : null}
    </div>
  );
}

function EmptyRows() {
  return (
    <div className="rounded-xl border border-border/60 bg-card p-8 text-center text-sm text-muted-foreground">
      No rows for this filter.
    </div>
  );
}

function formatMoney(n: number) {
  return `₹${Math.abs(n).toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function formatSignedMoney(n: number) {
  if (n === 0) return `₹0.00`;
  const abs = formatMoney(n);
  return n < 0 ? `−${abs}` : abs;
}

function balanceTone(n: number) {
  if (n < 0) return "text-destructive/80";
  if (n > 0) return "text-foreground";
  return "text-muted-foreground";
}

const dateFormatCache = new Map<string, string>();

function formatDateLong(value?: string | null) {
  if (!value) return "—";
  const cached = dateFormatCache.get(value);
  if (cached) return cached;
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const formatted = d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
  dateFormatCache.set(value, formatted);
  return formatted;
}

function normalizeCompanies(data: any[]): LedgerCompanyOption[] {
  return (data ?? []).map((c: any) => ({
    value: c.value ?? c.Value ?? "",
    label: c.label ?? c.Label ?? "",
    companyType: c.companyType ?? c.CompanyType ?? 2,
    companyName: c.companyName ?? c.CompanyName ?? "",
    companyId: c.companyId ?? c.CompanyId ?? 0,
  }));
}

function normalizeLedgers(data: any[]): LedgerNameOption[] {
  return (data ?? []).map((l: any) => ({
    ledgerId: l.ledgerId ?? l.LedgerId ?? l.ledgerName ?? l.LedgerName ?? "",
    ledgerName: l.ledgerName ?? l.LedgerName ?? "",
  }));
}

function normalizeResult(data: any): LedgerSummaryResult {
  return {
    openingBalance: data.openingBalance ?? data.OpeningBalance ?? 0,
    debitTotal: data.debitTotal ?? data.DebitTotal ?? 0,
    creditTotal: data.creditTotal ?? data.CreditTotal ?? 0,
    closingBalance: data.closingBalance ?? data.ClosingBalance ?? 0,
    companyCount: data.companyCount ?? data.CompanyCount ?? 1,
    ledgerCount: data.ledgerCount ?? data.LedgerCount ?? 1,
    pairCount: data.pairCount ?? data.PairCount ?? 1,
    rows: ((data.rows ?? data.Rows ?? []) as any[]).map(
      (r): LedgerSummaryRow => ({
        companyName: r.companyName ?? r.CompanyName ?? "",
        ledgerName: r.ledgerName ?? r.LedgerName ?? "",
        date: r.date ?? r.Date ?? null,
        particulars: r.particulars ?? r.Particulars ?? "",
        voucherType: r.voucherType ?? r.VoucherType ?? "",
        voucherNo: r.voucherNo ?? r.VoucherNo ?? "",
        voucherRef: r.voucherRef ?? r.VoucherRef ?? "",
        debit: r.debit ?? r.Debit ?? 0,
        credit: r.credit ?? r.Credit ?? 0,
        currency: r.currency ?? r.Currency ?? null,
        debitFc: r.debitFc ?? r.DebitFc ?? null,
        creditFc: r.creditFc ?? r.CreditFc ?? null,
        excRate: r.excRate ?? r.ExcRate ?? 0,
        closing: r.closing ?? r.Closing ?? 0,
        closingFc: r.closingFc ?? r.ClosingFc ?? 0,
        days: r.days ?? r.Days ?? 0,
        interest: r.interest ?? r.Interest ?? 0,
        isOpening: r.isOpening ?? r.IsOpening ?? false,
        approvalStatus: r.approvalStatus ?? r.ApprovalStatus ?? null,
      }),
    ),
  };
}
