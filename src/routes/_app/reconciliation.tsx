import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import {
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Loader2,
  Search,
  Upload,
  AlertTriangle,
  XCircle,
  ArrowLeftRight,
  Copy,
  HelpCircle,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  X,
} from "lucide-react";
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Sector } from "recharts";
import { toast } from "sonner";
import { getApiUrl } from "@/lib/api-config";
import { cn } from "@/lib/utils";
import { useIsMobile } from "@/hooks/use-mobile";
import {
  defaultMatchOptions,
  formatStatusLabel,
  type ComparisonPair,
  type ComparisonResult,
  type ComparisonStatus,
  type ExcelPreview,
  type LedgerColumnMapping,
  type LedgerEntry,
  type LedgerMatchOptions,
} from "@/lib/ledger-types";

export const Route = createFileRoute("/_app/reconciliation")({
  head: () => ({ meta: [{ title: "Ledger Reconciliation — PO Portal" }] }),
  component: ReconciliationPage,
});

type Step = 1 | 2 | 3;
type StatusFilter = "issues" | "all" | "missing" | "other" | ComparisonStatus;

type FileSide = {
  file: File | null;
  preview: ExcelPreview | null;
  mapping: LedgerColumnMapping;
  loading: boolean;
};

const emptyMapping = (): LedgerColumnMapping => ({
  headerRow: 1,
  company: null,
  date: null,
  particulars: null,
  voucherNo: null,
  voucherRef: null,
  debit: null,
  credit: null,
  sheetName: null,
});

const RESULTS_PAGE_SIZE = 50;

const PIE_COLORS = {
  Matched: "#16a34a",
  Mismatch: "#ea580c",
  Missing: "#dc2626",
  Other: "#64748b",
} as const;

function ReconciliationPage() {
  const isMobile = useIsMobile();
  const [step, setStep] = useState<Step>(1);
  const [sideA, setSideA] = useState<FileSide>({
    file: null,
    preview: null,
    mapping: emptyMapping(),
    loading: false,
  });
  const [sideB, setSideB] = useState<FileSide>({
    file: null,
    preview: null,
    mapping: emptyMapping(),
    loading: false,
  });
  const [options, setOptions] = useState<LedgerMatchOptions>(defaultMatchOptions);
  const [comparing, setComparing] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState<ComparisonResult | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("issues");
  const [q, setQ] = useState("");
  const [selected, setSelected] = useState<ComparisonPair | null>(null);
  const [currentPage, setCurrentPage] = useState(1);

  async function previewFile(side: "A" | "B", file: File) {
    const setSide = side === "A" ? setSideA : setSideB;
    setSide((prev) => ({ ...prev, loading: true, file }));
    try {
      const form = new FormData();
      form.append("file", file);
      const res = await fetch(getApiUrl("/api/excel/preview"), { method: "POST", body: form });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to read Excel file");

      const preview = normalizePreview(data);
      setSide({
        file,
        preview,
        mapping: {
          ...preview.suggestedMapping,
          sheetName: preview.selectedSheet,
          headerRow: preview.headerRow,
        },
        loading: false,
      });
      toast.success(`Loaded ${file.name} (${preview.dataRowCount} rows)`);
    } catch (err: any) {
      setSide((prev) => ({ ...prev, loading: false, file: null, preview: null }));
      toast.error(err.message || "Preview failed");
    }
  }

  async function refreshPreview(
    side: "A" | "B",
    overrides?: Partial<LedgerColumnMapping>,
  ) {
    const current = side === "A" ? sideA : sideB;
    if (!current.file) return;
    const setSide = side === "A" ? setSideA : setSideB;
    const mapping = { ...current.mapping, ...overrides };
    setSide((prev) => ({ ...prev, mapping, loading: true }));
    try {
      const form = new FormData();
      form.append("file", current.file);
      if (mapping.sheetName) form.append("sheetName", mapping.sheetName);
      form.append("headerRow", String(mapping.headerRow || 1));
      const res = await fetch(getApiUrl("/api/excel/preview"), { method: "POST", body: form });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to refresh preview");
      const preview = normalizePreview(data);
      setSide((prev) => ({
        ...prev,
        preview,
        mapping: {
          ...prev.mapping,
          sheetName: preview.selectedSheet,
          headerRow: preview.headerRow,
          date: keepOrSuggest(prev.mapping.date, preview.headers, preview.suggestedMapping.date),
          company: keepOrSuggest(prev.mapping.company, preview.headers, preview.suggestedMapping.company),
          particulars: keepOrSuggest(prev.mapping.particulars, preview.headers, preview.suggestedMapping.particulars),
          voucherNo: keepOrSuggest(prev.mapping.voucherNo, preview.headers, preview.suggestedMapping.voucherNo),
          voucherRef: keepOrSuggest(prev.mapping.voucherRef, preview.headers, preview.suggestedMapping.voucherRef),
          debit: keepOrSuggest(prev.mapping.debit, preview.headers, preview.suggestedMapping.debit),
          credit: keepOrSuggest(prev.mapping.credit, preview.headers, preview.suggestedMapping.credit),
        },
        loading: false,
      }));
    } catch (err: any) {
      setSide((prev) => ({ ...prev, loading: false }));
      toast.error(err.message || "Preview failed");
    }
  }

  function validateMappings(): string | null {
    for (const [label, side] of [
      ["File A", sideA],
      ["File B", sideB],
    ] as const) {
      if (!side.file || !side.preview) return `Upload ${label} first.`;
      if (!side.mapping.date) return `${label}: Date column is required.`;
      if (!side.mapping.debit) return `${label}: Debit column is required.`;
      if (!side.mapping.credit) return `${label}: Credit column is required.`;
    }
    return null;
  }

  async function runCompare() {
    const error = validateMappings();
    if (error) {
      toast.error(error);
      return;
    }
    setComparing(true);
    try {
      const form = buildCompareForm();
      const res = await fetch(getApiUrl("/api/excel/compare"), { method: "POST", body: form });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Comparison failed");
      setResult(normalizeResult(data));
      setStep(3);
      setStatusFilter("issues");
      setSelected(null);
      setCurrentPage(1);
      toast.success("Comparison complete");
    } catch (err: any) {
      toast.error(err.message || "Comparison failed");
    } finally {
      setComparing(false);
    }
  }

  async function runExport() {
    const error = validateMappings();
    if (error) {
      toast.error(error);
      return;
    }
    setExporting(true);
    try {
      const form = buildCompareForm();
      const res = await fetch(getApiUrl("/api/excel/export"), { method: "POST", body: form });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Export failed");
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `ledger-reconciliation-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, "-")}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success("Report downloaded");
    } catch (err: any) {
      toast.error(err.message || "Export failed");
    } finally {
      setExporting(false);
    }
  }

  function buildCompareForm() {
    const form = new FormData();
    form.append("fileA", sideA.file!);
    form.append("fileB", sideB.file!);
    form.append("mappingA", JSON.stringify(sideA.mapping));
    form.append("mappingB", JSON.stringify(sideB.mapping));
    form.append("options", JSON.stringify(options));
    return form;
  }

  const filtered = useMemo(() => {
    if (!result) return [];
    const query = q.trim().toLowerCase();
    return result.results.filter((r) => {
      if (statusFilter === "issues") {
        if (r.status === "Matched") return false;
      } else if (statusFilter === "missing") {
        if (r.status !== "MissingInA" && r.status !== "MissingInB") return false;
      } else if (statusFilter === "other") {
        if (r.status !== "Duplicate" && r.status !== "PotentialMatch") return false;
      } else if (statusFilter !== "all" && r.status !== statusFilter) {
        return false;
      }
      if (!query) return true;
      const hay = [
        r.message,
        r.entryA?.particulars,
        r.entryA?.voucherNo,
        r.entryA?.voucherRef,
        r.entryB?.particulars,
        r.entryB?.voucherNo,
        r.entryB?.voucherRef,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return hay.includes(query);
    });
  }, [result, statusFilter, q]);

  const pieData = useMemo(() => {
    if (!result) return [];
    const s = result.summary;
    const rows = [
      { key: "Matched" as const, name: "Matched", value: s.matched, filter: "Matched" as StatusFilter },
      { key: "Mismatch" as const, name: "Mismatch", value: s.amountMismatch, filter: "AmountMismatch" as StatusFilter },
      {
        key: "Missing" as const,
        name: "Missing",
        value: s.missingInA + s.missingInB,
        filter: "missing" as StatusFilter,
      },
      {
        key: "Other" as const,
        name: "Other",
        value: s.duplicates + s.potentialMatches,
        filter: "other" as StatusFilter,
      },
    ].filter((d) => d.value > 0);
    const total = rows.reduce((sum, r) => sum + r.value, 0) || 1;
    return rows.map((r) => ({
      ...r,
      percent: (r.value / total) * 100,
    }));
  }, [result]);

  const pieTotal = useMemo(
    () => pieData.reduce((sum, r) => sum + r.value, 0),
    [pieData],
  );

  function applyPieFilter(next: StatusFilter) {
    setStatusFilter((prev) => (prev === next ? "issues" : next));
  }

  function isPieSliceActive(filter: StatusFilter) {
    if (statusFilter === filter) return true;
    if (filter === "missing" && (statusFilter === "MissingInA" || statusFilter === "MissingInB")) return true;
    if (filter === "other" && (statusFilter === "Duplicate" || statusFilter === "PotentialMatch")) return true;
    return false;
  }

  const companyNameA = result?.companyNameA?.trim() || "Company A";
  const companyNameB = result?.companyNameB?.trim() || "Company B";
  const pieHighlightActive = statusFilter !== "issues" && statusFilter !== "all";

  const totalPages = Math.max(1, Math.ceil(filtered.length / RESULTS_PAGE_SIZE));
  const pageRows = useMemo(() => {
    const start = (currentPage - 1) * RESULTS_PAGE_SIZE;
    return filtered.slice(start, start + RESULTS_PAGE_SIZE);
  }, [filtered, currentPage]);

  useEffect(() => {
    setCurrentPage(1);
  }, [statusFilter, q]);

  useEffect(() => {
    if (currentPage > totalPages) setCurrentPage(totalPages);
  }, [currentPage, totalPages]);

  return (
    <div className="space-y-5 pb-24 md:pb-0">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Ledger Reconciliation</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Compare two company ledgers and reconcile debit ↔ credit transactions.
          </p>
        </div>
        {step === 3 && result && (
          <button
            type="button"
            onClick={runExport}
            disabled={exporting}
            className="hidden h-10 items-center gap-2 rounded-md bg-primary px-3 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 md:inline-flex"
          >
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Export report
          </button>
        )}
      </div>

      <StepIndicator step={step} />

      {step === 1 && (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <UploadCard
              label="Company A ledger"
              side={sideA}
              onFile={(f) => previewFile("A", f)}
              onClear={() => setSideA({ file: null, preview: null, mapping: emptyMapping(), loading: false })}
            />
            <UploadCard
              label="Company B ledger"
              side={sideB}
              onFile={(f) => previewFile("B", f)}
              onClear={() => setSideB({ file: null, preview: null, mapping: emptyMapping(), loading: false })}
            />
          </div>
          <div className="flex justify-end">
            <button
              type="button"
              disabled={!sideA.preview || !sideB.preview}
              onClick={() => setStep(2)}
              className="inline-flex h-11 w-full items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 md:w-auto"
            >
              Continue to mapping
            </button>
          </div>
        </div>
      )}

      {step === 2 && (
        <div className="space-y-4">
          <div className="grid gap-4 lg:grid-cols-2">
            <MappingCard
              title="File A mapping"
              side={sideA}
              onChange={(mapping) => setSideA((p) => ({ ...p, mapping }))}
              onSheetOrHeaderChange={(mapping) => refreshPreview("A", mapping)}
            />
            <MappingCard
              title="File B mapping"
              side={sideB}
              onChange={(mapping) => setSideB((p) => ({ ...p, mapping }))}
              onSheetOrHeaderChange={(mapping) => refreshPreview("B", mapping)}
            />
          </div>

          <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
            <h2 className="text-sm font-semibold">Match rules</h2>
            <p className="mt-1 text-xs text-muted-foreground">
              Default: voucher ref (when present) + date/amount, then reconcile opposite sides (A Debit ↔ B Credit).
            </p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={options.matchOnDate}
                  onChange={(e) => setOptions((o) => ({ ...o, matchOnDate: e.target.checked }))}
                  className="rounded border-input"
                />
                Match on date
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={options.matchOnAmount}
                  onChange={(e) => setOptions((o) => ({ ...o, matchOnAmount: e.target.checked }))}
                  className="rounded border-input"
                />
                Match on amount
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={options.preferVoucherRef}
                  onChange={(e) => setOptions((o) => ({ ...o, preferVoucherRef: e.target.checked }))}
                  className="rounded border-input"
                />
                Match on voucher / bank ref
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={options.matchOnVoucherNo}
                  onChange={(e) => setOptions((o) => ({ ...o, matchOnVoucherNo: e.target.checked }))}
                  className="rounded border-input"
                />
                Also match on voucher no
              </label>
              <label className="flex flex-col gap-1 text-sm">
                <span className="text-muted-foreground">Date tolerance (days)</span>
                <input
                  type="number"
                  min={0}
                  max={7}
                  value={options.dateToleranceDays}
                  onChange={(e) =>
                    setOptions((o) => ({ ...o, dateToleranceDays: Number(e.target.value) || 0 }))
                  }
                  className="h-10 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
              </label>
              <label className="flex flex-col gap-1 text-sm">
                <span className="text-muted-foreground">Amount tolerance</span>
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={options.amountTolerance}
                  onChange={(e) =>
                    setOptions((o) => ({ ...o, amountTolerance: Number(e.target.value) || 0 }))
                  }
                  className="h-10 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
              </label>
            </div>
          </div>

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
            <button
              type="button"
              onClick={() => setStep(1)}
              className="inline-flex h-11 items-center justify-center rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary"
            >
              Back
            </button>
            <button
              type="button"
              onClick={runCompare}
              disabled={comparing}
              className="inline-flex h-11 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {comparing ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowLeftRight className="h-4 w-4" />}
              Compare ledgers
            </button>
          </div>
        </div>
      )}

      {step === 3 && result && (
        <div className="space-y-4">
          <div className="hidden rounded-xl border border-border bg-card px-4 py-3 text-sm shadow-sm lg:block">
            <span className="text-muted-foreground">Ledgers: </span>
            <span className="font-semibold">{companyNameA}</span>
            <span className="text-muted-foreground"> · </span>
            <span className="font-semibold">{companyNameB}</span>
          </div>
          <div className="rounded-xl border border-border bg-card p-4 shadow-sm md:p-6">
            {/* Desktop: cards around central donut */}
            <div className="hidden items-center gap-5 lg:grid lg:grid-cols-[1fr_minmax(300px,380px)_1fr]">
              <div className="space-y-3">
                <StatCard
                  label={`Total — ${companyNameA}`}
                  value={result.summary.totalA}
                  description={`Total records in ${companyNameA}`}
                  icon={FileSpreadsheet}
                />
                <StatCard
                  label="Matched"
                  value={result.summary.matched}
                  description="Debit ↔ credit reconciled"
                  tone="success"
                  icon={CheckCircle2}
                  active={isPieSliceActive("Matched")}
                  onClick={() => applyPieFilter("Matched")}
                />
                <StatCard
                  label="Mismatch"
                  value={result.summary.amountMismatch}
                  description="Amount differences found"
                  tone="warning"
                  icon={AlertTriangle}
                  active={isPieSliceActive("AmountMismatch")}
                  onClick={() => applyPieFilter("AmountMismatch")}
                />
              </div>

              <OverviewDonut
                pieData={pieData}
                pieTotal={pieTotal}
                pieHighlightActive={pieHighlightActive}
                isPieSliceActive={isPieSliceActive}
                applyPieFilter={applyPieFilter}
              />

              <div className="space-y-3">
                <StatCard
                  label={`Total — ${companyNameB}`}
                  value={result.summary.totalB}
                  description={`Total records in ${companyNameB}`}
                  icon={FileSpreadsheet}
                />
                <StatCard
                  label="Missing"
                  value={result.summary.missingInA + result.summary.missingInB}
                  description="Missing in one of the ledgers"
                  tone="danger"
                  icon={XCircle}
                  active={isPieSliceActive("missing")}
                  onClick={() => applyPieFilter("missing")}
                />
                <StatCard
                  label="Other"
                  value={result.summary.duplicates + result.summary.potentialMatches}
                  description="Duplicates or potential matches"
                  tone="muted"
                  icon={HelpCircle}
                  active={isPieSliceActive("other")}
                  onClick={() => applyPieFilter("other")}
                />
              </div>
            </div>

            {/* Mobile / tablet */}
            <div className="space-y-4 lg:hidden">
              <OverviewDonut
                pieData={pieData}
                pieTotal={pieTotal}
                pieHighlightActive={pieHighlightActive}
                isPieSliceActive={isPieSliceActive}
                applyPieFilter={applyPieFilter}
              />

              <div className="rounded-xl border border-border/80 bg-surface/60 px-3.5 py-3">
                <div className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                  Comparing
                </div>
                <div className="mt-2.5 space-y-2.5">
                  <div className="flex items-start justify-between gap-3">
                    <span className="min-w-0 text-sm font-medium leading-snug">{companyNameA}</span>
                    <span className="shrink-0 text-sm font-bold tabular-nums text-primary">
                      {result.summary.totalA.toLocaleString("en-IN")}
                    </span>
                  </div>
                  <div className="flex items-start justify-between gap-3">
                    <span className="min-w-0 text-sm font-medium leading-snug">{companyNameB}</span>
                    <span className="shrink-0 text-sm font-bold tabular-nums text-primary">
                      {result.summary.totalB.toLocaleString("en-IN")}
                    </span>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <StatCard
                  label="Matched"
                  value={result.summary.matched}
                  description="Reconciled"
                  tone="success"
                  icon={CheckCircle2}
                  active={isPieSliceActive("Matched")}
                  onClick={() => applyPieFilter("Matched")}
                />
                <StatCard
                  label="Mismatch"
                  value={result.summary.amountMismatch}
                  description="Amount diffs"
                  tone="warning"
                  icon={AlertTriangle}
                  active={isPieSliceActive("AmountMismatch")}
                  onClick={() => applyPieFilter("AmountMismatch")}
                />
                <StatCard
                  label="Missing"
                  value={result.summary.missingInA + result.summary.missingInB}
                  description="In one ledger only"
                  tone="danger"
                  icon={XCircle}
                  active={isPieSliceActive("missing")}
                  onClick={() => applyPieFilter("missing")}
                />
                <StatCard
                  label="Other"
                  value={result.summary.duplicates + result.summary.potentialMatches}
                  description="Dup / potential"
                  tone="muted"
                  icon={HelpCircle}
                  active={isPieSliceActive("other")}
                  onClick={() => applyPieFilter("other")}
                />
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-2 md:flex-row md:items-center">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="Search particulars, voucher no, or voucher ref…"
                className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
              />
            </div>
            <div className="relative w-full md:w-auto md:min-w-[220px]">
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
                className="h-10 w-full appearance-none rounded-md border border-input bg-surface py-2 pl-3 pr-10 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
              >
                <option value="issues">Problems only</option>
                <option value="all">All results</option>
                <option value="Matched">Matched</option>
                <option value="AmountMismatch">Amount mismatch</option>
                <option value="missing">Missing (either ledger)</option>
                <option value="MissingInA">Missing in {companyNameA}</option>
                <option value="MissingInB">Missing in {companyNameB}</option>
                <option value="other">Other (dup / potential)</option>
                <option value="Duplicate">Duplicate</option>
                <option value="PotentialMatch">Potential match</option>
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
          </div>

          <p className="text-xs text-muted-foreground">
            Showing {filtered.length === 0 ? 0 : (currentPage - 1) * RESULTS_PAGE_SIZE + 1}
            –{Math.min(currentPage * RESULTS_PAGE_SIZE, filtered.length)} of {filtered.length.toLocaleString("en-IN")}
            {totalPages > 1 ? ` · Page ${currentPage} of ${totalPages}` : ""}
          </p>

          {isMobile ? (
            <div className="space-y-2">
              {pageRows.length === 0 ? (
                <EmptyState />
              ) : (
                pageRows.map((row) => (
                  <button
                    key={row.id}
                    type="button"
                    onClick={() => setSelected(row)}
                    className="w-full rounded-xl border border-border bg-card p-3 text-left shadow-sm"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <ResultBadge status={row.status} companyNameA={companyNameA} companyNameB={companyNameB} />
                      {row.difference != null && (
                        <span className="text-sm font-semibold tabular-nums">₹{formatNum(row.difference)}</span>
                      )}
                    </div>
                    <div className="mt-2 text-sm font-medium leading-snug">
                      {row.entryA?.particulars || row.entryB?.particulars || row.message}
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {formatDate(row.entryA?.date || row.entryB?.date)}
                      {(row.entryA?.voucherNo || row.entryB?.voucherNo) && (
                        <> · Vch {row.entryA?.voucherNo || "—"} / {row.entryB?.voucherNo || "—"}</>
                      )}
                    </div>
                  </button>
                ))
              )}
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="min-w-[1280px] w-full table-fixed text-sm">
                <colgroup>
                  <col className="w-[200px]" />
                  <col className="w-[110px]" />
                  <col />
                  <col className="w-[230px]" />
                  <col />
                  <col className="w-[230px]" />
                  <col className="w-[120px]" />
                </colgroup>
                <thead className="bg-secondary/50 text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-5 py-2.5 text-center font-medium">Status</th>
                    <th className="px-5 py-2.5 text-center font-medium">Date</th>
                    <th className="px-5 py-2.5 text-center font-medium">{companyNameA}</th>
                    <th className="px-5 py-2.5 text-center font-medium">Amount</th>
                    <th className="px-5 py-2.5 text-center font-medium">{companyNameB}</th>
                    <th className="px-5 py-2.5 text-center font-medium">Amount</th>
                    <th className="px-5 py-2.5 text-center font-medium">Diff</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {pageRows.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="px-5 py-10 text-center text-muted-foreground">
                        No rows match this filter.
                      </td>
                    </tr>
                  ) : (
                    pageRows.map((row) => (
                      <tr
                        key={row.id}
                        className="cursor-pointer hover:bg-secondary/40"
                        onClick={() => setSelected(row)}
                      >
                        <td className="px-5 py-2.5">
                          <div className="flex justify-center">
                            <ResultBadge status={row.status} companyNameA={companyNameA} companyNameB={companyNameB} />
                          </div>
                        </td>
                        <td className="px-5 py-2.5 text-center whitespace-nowrap">
                          {formatDate(row.entryA?.date || row.entryB?.date)}
                        </td>
                        <td className="px-5 py-2.5 text-center break-words">{row.entryA?.particulars || "—"}</td>
                        <td className="px-5 py-2.5">
                          <div className="flex justify-center">
                            <AmountSideCell entry={row.entryA} />
                          </div>
                        </td>
                        <td className="px-5 py-2.5 text-center break-words">{row.entryB?.particulars || "—"}</td>
                        <td className="px-5 py-2.5">
                          <div className="flex justify-center">
                            <AmountSideCell entry={row.entryB} />
                          </div>
                        </td>
                        <td className="px-5 py-2.5 text-center whitespace-nowrap tabular-nums">
                          {row.difference != null ? formatNum(row.difference) : "—"}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}

          {totalPages > 1 && (
            <div className="flex items-center justify-between gap-3">
              <button
                type="button"
                disabled={currentPage <= 1}
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                className="inline-flex h-10 items-center gap-1 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary disabled:opacity-50"
              >
                <ChevronLeft className="h-4 w-4" />
                Prev
              </button>
              <span className="text-xs text-muted-foreground tabular-nums">
                {currentPage} / {totalPages}
              </span>
              <button
                type="button"
                disabled={currentPage >= totalPages}
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                className="inline-flex h-10 items-center gap-1 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary disabled:opacity-50"
              >
                Next
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          )}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
            <button
              type="button"
              onClick={() => setStep(2)}
              className="inline-flex h-11 items-center justify-center rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary"
            >
              Back to mapping
            </button>
            <button
              type="button"
              onClick={() => {
                setStep(1);
                setResult(null);
                setSelected(null);
                setCurrentPage(1);
              }}
              className="inline-flex h-11 items-center justify-center rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary"
            >
              New comparison
            </button>
          </div>
        </div>
      )}

      {/* Mobile sticky export */}
      {step === 3 && result && (
        <div className="fixed inset-x-0 bottom-16 z-20 border-t border-border bg-surface/95 p-3 backdrop-blur md:hidden">
          <button
            type="button"
            onClick={runExport}
            disabled={exporting}
            className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-md bg-primary text-sm font-semibold text-primary-foreground disabled:opacity-50"
          >
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Export report
          </button>
        </div>
      )}

      {selected && (
        <DetailSheet
          pair={selected}
          companyNameA={companyNameA}
          companyNameB={companyNameB}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}

function StepIndicator({ step }: { step: Step }) {
  const items = [
    { n: 1 as Step, label: "Upload" },
    { n: 2 as Step, label: "Map" },
    { n: 3 as Step, label: "Results" },
  ];
  return (
    <div className="flex items-center gap-2 rounded-xl border border-border bg-card p-2">
      {items.map((item, idx) => (
        <div key={item.n} className="flex flex-1 items-center gap-2">
          <div
            className={cn(
              "flex h-8 w-8 items-center justify-center rounded-full text-xs font-semibold",
              step === item.n
                ? "bg-primary text-primary-foreground"
                : step > item.n
                  ? "bg-success/15 text-success"
                  : "bg-secondary text-muted-foreground",
            )}
          >
            {item.n}
          </div>
          <span className={cn("text-xs font-medium", step === item.n ? "text-foreground" : "text-muted-foreground")}>
            {item.label}
          </span>
          {idx < items.length - 1 && <div className="ml-auto hidden h-px flex-1 bg-border sm:block" />}
        </div>
      ))}
    </div>
  );
}

function UploadCard({
  label,
  side,
  onFile,
  onClear,
}: {
  label: string;
  side: FileSide;
  onFile: (f: File) => void;
  onClear: () => void;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <div className="flex items-center gap-2">
        <FileSpreadsheet className="h-5 w-5 text-primary" />
        <h2 className="text-sm font-semibold">{label}</h2>
      </div>
      <label className="mt-4 flex cursor-pointer flex-col items-center justify-center rounded-lg border border-dashed border-border bg-surface px-4 py-8 hover:bg-secondary/50">
        {side.loading ? (
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        ) : (
          <>
            <Upload className="h-8 w-8 text-muted-foreground" />
            <span className="mt-2 text-sm font-medium">Tap to upload .xlsx</span>
            <span className="mt-1 text-xs text-muted-foreground">Max 25 MB</span>
          </>
        )}
        <input
          type="file"
          accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
          className="hidden"
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) onFile(f);
            e.target.value = "";
          }}
        />
      </label>
      {side.preview && (
        <div className="mt-3 rounded-lg border border-border bg-surface p-3 text-sm">
          <div className="font-medium truncate">{side.preview.fileName}</div>
          <div className="mt-1 text-xs text-muted-foreground">
            {side.preview.dataRowCount} data rows · sheet “{side.preview.selectedSheet}”
          </div>
          <button type="button" onClick={onClear} className="mt-2 text-xs font-medium text-destructive hover:underline">
            Remove
          </button>
        </div>
      )}
    </div>
  );
}

function MappingCard({
  title,
  side,
  onChange,
  onSheetOrHeaderChange,
}: {
  title: string;
  side: FileSide;
  onChange: (m: LedgerColumnMapping) => void;
  onSheetOrHeaderChange: (m: LedgerColumnMapping) => void;
}) {
  if (!side.preview) return null;
  const headers = side.preview.headers;
  const fields: { key: keyof LedgerColumnMapping; label: string; required?: boolean; amount?: boolean }[] = [
    { key: "company", label: "Company" },
    { key: "date", label: "Date", required: true },
    { key: "debit", label: "Debit (INR)", required: true, amount: true },
    { key: "credit", label: "Credit (INR)", required: true, amount: true },
    { key: "particulars", label: "Particulars" },
    { key: "voucherNo", label: "Voucher No" },
    { key: "voucherRef", label: "Voucher / Bank Ref" },
  ];

  function optionsFor(field: (typeof fields)[number]) {
    if (!field.amount) return headers;
    const inr = headers.filter(isInrAmountHeader);
    const nonFc = headers.filter((h) => !isFcAmountHeader(h));
    // Prefer INR columns; if none labeled INR, hide FC amount columns.
    if (inr.length > 0) return inr;
    if (nonFc.length > 0) return nonFc;
    return headers;
  }

  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
      <h2 className="text-sm font-semibold">{title}</h2>
      <div className="mt-3 space-y-3">
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-xs text-muted-foreground">Sheet</span>
          <select
            value={side.mapping.sheetName ?? side.preview.selectedSheet}
            onChange={(e) =>
              onSheetOrHeaderChange({ ...side.mapping, sheetName: e.target.value })
            }
            className="h-10 rounded-md border border-input bg-surface px-3 text-sm"
          >
            {side.preview.sheetNames.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-xs text-muted-foreground">Header row</span>
          <input
            type="number"
            min={1}
            value={side.mapping.headerRow}
            onChange={(e) =>
              onSheetOrHeaderChange({
                ...side.mapping,
                headerRow: Number(e.target.value) || 1,
              })
            }
            className="h-10 rounded-md border border-input bg-surface px-3 text-sm"
          />
        </label>
        {fields.map((f) => (
          <label key={f.key} className="flex flex-col gap-1 text-sm">
            <span className="text-xs text-muted-foreground">
              {f.label}
              {f.required ? " *" : ""}
            </span>
            <select
              value={(side.mapping[f.key] as string) || ""}
              onChange={(e) => onChange({ ...side.mapping, [f.key]: e.target.value || null })}
              className="h-10 rounded-md border border-input bg-surface px-3 text-sm"
            >
              <option value="">— Select —</option>
              {optionsFor(f).map((h) => (
                <option key={h} value={h}>
                  {h}
                </option>
              ))}
            </select>
          </label>
        ))}
      </div>
      <p className="mt-3 text-[11px] text-muted-foreground">
        Amounts always use Debit/Credit (INR). Foreign-currency columns are ignored.
      </p>
    </div>
  );
}

function OverviewDonut({
  pieData,
  pieTotal,
  pieHighlightActive,
  isPieSliceActive,
  applyPieFilter,
}: {
  pieData: {
    key: keyof typeof PIE_COLORS;
    name: string;
    value: number;
    percent: number;
    filter: StatusFilter;
  }[];
  pieTotal: number;
  pieHighlightActive: boolean;
  isPieSliceActive: (filter: StatusFilter) => boolean;
  applyPieFilter: (next: StatusFilter) => void;
}) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  return (
    <div className="flex flex-col items-center">
      <div className="mb-3 text-center">
        <div className="inline-flex flex-col items-center gap-1.5">
          <h2 className="text-lg font-semibold tracking-tight text-foreground sm:text-xl">
            Reconciliation Overview
          </h2>
          <div className="h-0.5 w-12 rounded-full bg-gradient-to-r from-transparent via-primary to-transparent" />
        </div>
      </div>
      <div className="relative mx-auto h-64 w-full max-w-[300px] sm:h-72 sm:max-w-[340px]">
        {pieData.length === 0 ? (
          <div className="flex h-full items-center justify-center text-xs text-muted-foreground">No data</div>
        ) : (
          <>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={pieData}
                  dataKey="value"
                  nameKey="name"
                  cx="50%"
                  cy="50%"
                  innerRadius="58%"
                  outerRadius="82%"
                  paddingAngle={3}
                  cornerRadius={4}
                  stroke="hsl(var(--card))"
                  strokeWidth={3}
                  style={{ cursor: "pointer", outline: "none" }}
                  activeIndex={hoverIndex ?? undefined}
                  activeShape={renderShinySlice}
                  onMouseEnter={(_, index) => setHoverIndex(index)}
                  onMouseLeave={() => setHoverIndex(null)}
                  onClick={(_, index) => {
                    const slice = pieData[index];
                    if (slice) applyPieFilter(slice.filter);
                  }}
                >
                  {pieData.map((entry, index) => {
                    const selected = isPieSliceActive(entry.filter);
                    const dimmed = pieHighlightActive && !selected && hoverIndex !== index;
                    return (
                      <Cell
                        key={entry.key}
                        fill={PIE_COLORS[entry.key]}
                        opacity={dimmed ? 0.32 : 1}
                        style={{
                          cursor: "pointer",
                          outline: "none",
                          transition: "opacity 160ms ease",
                          filter: selected
                            ? `drop-shadow(0 0 8px ${PIE_COLORS[entry.key]}88)`
                            : undefined,
                        }}
                      />
                    );
                  })}
                </Pie>
                <Tooltip
                  cursor={false}
                  content={<PieTooltipBox />}
                  wrapperStyle={{ outline: "none", zIndex: 40 }}
                />
              </PieChart>
            </ResponsiveContainer>
            <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
              <div className="text-3xl font-semibold tabular-nums tracking-tight sm:text-4xl">
                {pieTotal.toLocaleString("en-IN")}
              </div>
              <div className="mt-1 max-w-[8rem] text-[11px] leading-snug text-muted-foreground">
                Compared result rows
              </div>
            </div>
          </>
        )}
      </div>

      <ul className="mt-3 flex flex-wrap items-center justify-center gap-x-4 gap-y-2">
        {pieData.map((entry, index) => {
          const active = isPieSliceActive(entry.filter);
          const hovered = hoverIndex === index;
          return (
            <li key={entry.key}>
              <button
                type="button"
                onMouseEnter={() => setHoverIndex(index)}
                onMouseLeave={() => setHoverIndex(null)}
                onClick={() => applyPieFilter(entry.filter)}
                className={cn(
                  "inline-flex items-center gap-2 rounded-full border px-2.5 py-1 text-xs transition",
                  active || hovered
                    ? "border-foreground/30 bg-secondary font-medium"
                    : "border-transparent hover:bg-secondary/70",
                )}
              >
                <span
                  className="h-2.5 w-2.5 rounded-full transition"
                  style={{
                    backgroundColor: PIE_COLORS[entry.key],
                    boxShadow: hovered || active ? `0 0 8px ${PIE_COLORS[entry.key]}` : undefined,
                  }}
                />
                <span>{entry.name}</span>
                <span className="tabular-nums text-muted-foreground">
                  {entry.percent.toFixed(1)}%
                </span>
              </button>
            </li>
          );
        })}
      </ul>
      <p className="mt-2 text-center text-[11px] text-muted-foreground">
        Click a slice or legend item to filter · click again to reset
      </p>
    </div>
  );
}

function renderShinySlice(props: any) {
  const {
    cx,
    cy,
    innerRadius,
    outerRadius,
    startAngle,
    endAngle,
    fill,
  } = props;

  return (
    <g style={{ outline: "none" }}>
      {/* Soft outer glow ring */}
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={outerRadius + 6}
        outerRadius={outerRadius + 12}
        startAngle={startAngle}
        endAngle={endAngle}
        fill={fill}
        opacity={0.28}
        style={{ filter: `blur(0.5px)` }}
      />
      {/* Expanded bright slice */}
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={Math.max(0, innerRadius - 2)}
        outerRadius={outerRadius + 8}
        startAngle={startAngle}
        endAngle={endAngle}
        fill={fill}
        stroke={fill}
        strokeWidth={1}
        style={{
          filter: `drop-shadow(0 0 12px ${fill}) brightness(1.18)`,
          transition: "all 160ms ease",
        }}
      />
      {/* Specular highlight arc */}
      <Sector
        cx={cx}
        cy={cy}
        innerRadius={outerRadius - 10}
        outerRadius={outerRadius + 2}
        startAngle={startAngle}
        endAngle={endAngle}
        fill="#ffffff"
        opacity={0.22}
      />
    </g>
  );
}

function PieTooltipBox({ active, payload }: { active?: boolean; payload?: Array<{ payload: any }> }) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload as {
    key: keyof typeof PIE_COLORS;
    name: string;
    value: number;
    percent: number;
  };

  return (
    <div className="min-w-[150px] rounded-lg border border-border bg-popover px-3 py-2.5 text-popover-foreground shadow-lg">
      <div className="flex items-center gap-2">
        <span
          className="h-2.5 w-2.5 shrink-0 rounded-full"
          style={{ backgroundColor: PIE_COLORS[item.key] }}
        />
        <span className="text-xs font-semibold">{item.name}</span>
      </div>
      <div className="mt-1.5 flex items-baseline justify-between gap-4 text-sm">
        <span className="font-semibold tabular-nums">{item.value.toLocaleString("en-IN")}</span>
        <span className="text-xs tabular-nums text-muted-foreground">{item.percent.toFixed(1)}%</span>
      </div>
    </div>
  );
}

function StatCard({
  label,
  value,
  description,
  tone,
  icon: Icon,
  active,
  onClick,
}: {
  label: string;
  value: number;
  description?: string;
  tone?: "success" | "warning" | "danger" | "muted";
  icon?: typeof CheckCircle2;
  active?: boolean;
  onClick?: () => void;
}) {
  const surface =
    tone === "success"
      ? "border-success/35 bg-gradient-to-br from-success/20 via-success/10 to-card shadow-success/10"
      : tone === "warning"
        ? "border-warning/35 bg-gradient-to-br from-warning/20 via-warning/10 to-card shadow-warning/10"
        : tone === "danger"
          ? "border-destructive/35 bg-gradient-to-br from-destructive/20 via-destructive/10 to-card shadow-destructive/10"
          : tone === "muted"
            ? "border-border bg-gradient-to-br from-secondary via-secondary/60 to-card shadow-black/5"
            : "border-primary/35 bg-gradient-to-br from-primary/20 via-primary/10 to-card shadow-primary/10";

  const iconWrap =
    tone === "success"
      ? "bg-success text-success-foreground shadow-md shadow-success/30"
      : tone === "warning"
        ? "bg-warning text-warning-foreground shadow-md shadow-warning/30"
        : tone === "danger"
          ? "bg-destructive text-destructive-foreground shadow-md shadow-destructive/30"
          : tone === "muted"
            ? "bg-muted-foreground/90 text-background shadow-md shadow-black/20"
            : "bg-primary text-primary-foreground shadow-md shadow-primary/30";

  const valueTone =
    tone === "success"
      ? "text-success"
      : tone === "warning"
        ? "text-warning"
        : tone === "danger"
          ? "text-destructive"
          : tone === "muted"
            ? "text-foreground"
            : "text-primary";

  const activeRing =
    tone === "success"
      ? "ring-success/50"
      : tone === "warning"
        ? "ring-warning/50"
        : tone === "danger"
          ? "ring-destructive/50"
          : tone === "muted"
            ? "ring-foreground/30"
            : "ring-primary/50";

  const className = cn(
    "w-full rounded-xl border p-4 text-left shadow-lg transition duration-200",
    surface,
    onClick && "cursor-pointer hover:-translate-y-0.5 hover:shadow-xl hover:brightness-[1.03]",
    active && cn("ring-2 ring-offset-2 ring-offset-background", activeRing),
  );

  const body = (
    <div className="flex items-start gap-3.5">
      {Icon && (
        <div className={cn("mt-0.5 flex h-11 w-11 shrink-0 items-center justify-center rounded-xl", iconWrap)}>
          <Icon className="h-5 w-5" />
        </div>
      )}
      <div className="min-w-0 flex-1">
        <div className="text-[11px] font-semibold uppercase tracking-wide text-foreground/70">{label}</div>
        <div className={cn("mt-1 text-2xl font-bold tabular-nums tracking-tight sm:text-3xl", valueTone)}>
          {value.toLocaleString("en-IN")}
        </div>
        {description && (
          <p className="mt-1.5 text-[11px] leading-snug text-muted-foreground">{description}</p>
        )}
      </div>
    </div>
  );

  if (onClick) {
    return (
      <button type="button" onClick={onClick} className={className}>
        {body}
      </button>
    );
  }

  return <div className={className}>{body}</div>;
}

function AmountSideCell({ entry }: { entry?: LedgerEntry | null }) {
  if (!entry) {
    return (
      <div className="inline-flex items-center gap-2">
        <span className="w-16 shrink-0" />
        <span className="w-[8.5rem] text-right text-muted-foreground">—</span>
      </div>
    );
  }
  const isDebit = entry.side === "Debit";
  const isCredit = entry.side === "Credit";
  return (
    <div className="inline-flex items-center gap-2">
      {isDebit || isCredit ? (
        <span
          className={cn(
            "inline-flex w-16 shrink-0 items-center justify-center rounded-full border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
            isDebit
              ? "border-primary/30 bg-primary/10 text-primary"
              : "border-success/30 bg-success/10 text-success",
          )}
        >
          {isDebit ? "Debit" : "Credit"}
        </span>
      ) : (
        <span className="w-16 shrink-0" />
      )}
      <span className="w-[8.5rem] text-right tabular-nums font-medium whitespace-nowrap">
        {formatNum(entry.amount)}
      </span>
    </div>
  );
}

function ResultBadge({
  status,
  companyNameA = "A",
  companyNameB = "B",
}: {
  status: ComparisonStatus;
  companyNameA?: string;
  companyNameB?: string;
}) {
  const map: Record<ComparisonStatus, string> = {
    Matched: "bg-success/15 text-success border-success/30",
    AmountMismatch: "bg-warning/15 text-warning border-warning/30",
    MissingInA: "bg-destructive/15 text-destructive border-destructive/30",
    MissingInB: "bg-destructive/15 text-destructive border-destructive/30",
    Duplicate: "bg-primary/15 text-primary border-primary/30",
    PotentialMatch: "bg-secondary text-muted-foreground border-border",
  };

  const missingCompany =
    status === "MissingInA" ? companyNameA : status === "MissingInB" ? companyNameB : null;
  const useStackedMissing = Boolean(missingCompany && missingCompany.length > 14);

  if (useStackedMissing) {
    return (
      <span
        className={cn(
          "inline-flex max-w-[9.5rem] flex-col items-center justify-center rounded-lg border px-2.5 py-1.5 text-center text-[10px] font-medium leading-snug",
          map[status],
        )}
        title={formatStatusLabel(status, companyNameA, companyNameB)}
      >
        <span className="whitespace-nowrap">Missing in</span>
        <span className="mt-0.5 line-clamp-2 w-full break-words">{missingCompany}</span>
      </span>
    );
  }

  return (
    <span
      className={cn(
        "inline-flex max-w-[10rem] items-center justify-center gap-1 rounded-full border px-2.5 py-1 text-center text-[11px] font-medium leading-tight",
        map[status],
      )}
      title={formatStatusLabel(status, companyNameA, companyNameB)}
    >
      {status === "Duplicate" && <Copy className="h-3 w-3 shrink-0" />}
      <span className="text-balance">{formatStatusLabel(status, companyNameA, companyNameB)}</span>
    </span>
  );
}

function DetailSheet({
  pair,
  companyNameA,
  companyNameB,
  onClose,
}: {
  pair: ComparisonPair;
  companyNameA: string;
  companyNameB: string;
  onClose: () => void;
}) {
  const fields: { label: string; a: string; b: string }[] = [
    {
      label: "Date",
      a: formatDate(pair.entryA?.date),
      b: formatDate(pair.entryB?.date),
    },
    {
      label: "Voucher No",
      a: pair.entryA?.voucherNo || "—",
      b: pair.entryB?.voucherNo || "—",
    },
    {
      label: "Voucher Ref",
      a: pair.entryA?.voucherRef || "—",
      b: pair.entryB?.voucherRef || "—",
    },
    {
      label: "Particulars",
      a: pair.entryA?.particulars || "—",
      b: pair.entryB?.particulars || "—",
    },
    {
      label: "Debit",
      a: pair.entryA ? formatNum(pair.entryA.debit) : "—",
      b: pair.entryB ? formatNum(pair.entryB.debit) : "—",
    },
    {
      label: "Credit",
      a: pair.entryA ? formatNum(pair.entryA.credit) : "—",
      b: pair.entryB ? formatNum(pair.entryB.credit) : "—",
    },
    {
      label: "Side / Amount",
      a: pair.entryA ? `${pair.entryA.side} ${formatNum(pair.entryA.amount)}` : "—",
      b: pair.entryB ? `${pair.entryB.side} ${formatNum(pair.entryB.amount)}` : "—",
    },
  ];

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-3 backdrop-blur-[2px] sm:items-center sm:p-6"
      onClick={onClose}
    >
      <div
        className="flex max-h-[90vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-3 border-b border-border px-4 py-4 sm:px-6">
          <div className="min-w-0">
            <div className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              Comparison detail
            </div>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <ResultBadge status={pair.status} companyNameA={companyNameA} companyNameB={companyNameB} />
              {pair.difference != null && (
                <span className="rounded-full border border-warning/30 bg-warning/10 px-2.5 py-0.5 text-xs font-semibold tabular-nums text-warning">
                  Diff ₹{formatNum(pair.difference)}
                </span>
              )}
            </div>
            <p className="mt-2 text-sm text-muted-foreground">{pair.message}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-border bg-surface text-muted-foreground hover:bg-secondary hover:text-foreground"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="overflow-y-auto px-4 py-4 sm:px-6 sm:py-5">
          {/* Desktop: full side-by-side comparison table */}
          <div className="hidden overflow-hidden rounded-xl border border-border md:block">
            <table className="w-full text-sm">
              <thead className="bg-secondary/60 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-medium">Field</th>
                  <th className="px-4 py-3 font-medium">{companyNameA}</th>
                  <th className="px-4 py-3 font-medium">{companyNameB}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {fields.map((f) => {
                  const differs = f.a !== f.b && f.a !== "—" && f.b !== "—";
                  return (
                    <tr key={f.label} className={differs ? "bg-warning/5" : undefined}>
                      <td className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                        {f.label}
                      </td>
                      <td className="px-4 py-3 font-medium break-words">{f.a}</td>
                      <td className="px-4 py-3 font-medium break-words">{f.b}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Mobile: stacked cards */}
          <div className="grid gap-3 md:hidden">
            <EntryBlock title={companyNameA} entry={pair.entryA} />
            <EntryBlock title={companyNameB} entry={pair.entryB} />
          </div>

          {pair.difference != null && (
            <div className="mt-4 rounded-xl border border-warning/30 bg-warning/10 px-4 py-3 text-sm">
              <div className="text-xs font-semibold uppercase tracking-wide text-warning">Amount difference</div>
              <div className="mt-1 text-lg font-bold tabular-nums text-foreground">
                ₹{formatNum(pair.difference)}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function EntryBlock({ title, entry }: { title: string; entry: ComparisonPair["entryA"] }) {
  if (!entry) {
    return (
      <div className="rounded-lg border border-dashed border-border p-3 text-sm text-muted-foreground">
        <div className="font-semibold text-foreground">{title}</div>
        <p className="mt-2">No corresponding record</p>
      </div>
    );
  }
  return (
    <div className="rounded-lg border border-border bg-surface p-3 text-sm">
      <div className="font-semibold">{title}</div>
      <dl className="mt-2 space-y-1.5">
        <Row label="Date" value={formatDate(entry.date)} />
        <Row label="Voucher" value={entry.voucherNo || "—"} />
        <Row label="Ref" value={entry.voucherRef || "—"} />
        <Row label="Particulars" value={entry.particulars || "—"} />
        <Row label="Debit" value={formatNum(entry.debit)} />
        <Row label="Credit" value={formatNum(entry.credit)} />
      </dl>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-3">
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-right font-medium">{value}</dd>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="rounded-xl border border-border bg-card p-8 text-center text-sm text-muted-foreground">
      No rows match this filter.
    </div>
  );
}

function formatNum(n: number) {
  return n.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDate(value?: string | null) {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString("en-IN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function keepOrSuggest(current: string | null | undefined, headers: string[], suggested?: string | null) {
  if (current && headers.includes(current)) return current;
  return suggested ?? null;
}

function normalizeHeaderKey(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9 ]+/g, " ").replace(/\s+/g, " ").trim();
}

function isFcAmountHeader(header: string) {
  const n = normalizeHeaderKey(header);
  return /\b(fc|foreign|usd|eur)\b/.test(n) || n.includes("debit fc") || n.includes("credit fc");
}

function isInrAmountHeader(header: string) {
  const n = normalizeHeaderKey(header);
  if (isFcAmountHeader(header)) return false;
  return (
    /\b(inr|rs|rupee)\b/.test(n) ||
    n.includes("debit inr") ||
    n.includes("credit inr") ||
    n === "debit" ||
    n === "credit" ||
    n === "dr" ||
    n === "cr"
  );
}

function normalizePreview(data: any): ExcelPreview {
  const suggested = data.suggestedMapping ?? data.SuggestedMapping ?? {};
  return {
    fileName: data.fileName ?? data.FileName ?? "",
    sheetNames: data.sheetNames ?? data.SheetNames ?? [],
    selectedSheet: data.selectedSheet ?? data.SelectedSheet ?? "",
    headerRow: data.headerRow ?? data.HeaderRow ?? 1,
    headers: data.headers ?? data.Headers ?? [],
    suggestedMapping: {
      sheetName: suggested.sheetName ?? suggested.SheetName ?? null,
      headerRow: suggested.headerRow ?? suggested.HeaderRow ?? 1,
      company: suggested.company ?? suggested.Company ?? null,
      date: suggested.date ?? suggested.Date ?? null,
      particulars: suggested.particulars ?? suggested.Particulars ?? null,
      voucherNo: suggested.voucherNo ?? suggested.VoucherNo ?? null,
      voucherRef: suggested.voucherRef ?? suggested.VoucherRef ?? null,
      debit: suggested.debit ?? suggested.Debit ?? null,
      credit: suggested.credit ?? suggested.Credit ?? null,
    },
    dataRowCount: data.dataRowCount ?? data.DataRowCount ?? 0,
    sampleRows: data.sampleRows ?? data.SampleRows ?? [],
  };
}

function normalizeResult(data: any): ComparisonResult {
  const summary = data.summary ?? data.Summary ?? {};
  const results = (data.results ?? data.Results ?? []).map((r: any) => ({
    id: r.id ?? r.Id,
    status: r.status ?? r.Status,
    message: r.message ?? r.Message ?? "",
    difference: r.difference ?? r.Difference ?? null,
    entryA: normalizeEntry(r.entryA ?? r.EntryA),
    entryB: normalizeEntry(r.entryB ?? r.EntryB),
  }));
  return {
    companyNameA: data.companyNameA ?? data.CompanyNameA ?? "Company A",
    companyNameB: data.companyNameB ?? data.CompanyNameB ?? "Company B",
    summary: {
      totalA: summary.totalA ?? summary.TotalA ?? 0,
      totalB: summary.totalB ?? summary.TotalB ?? 0,
      matched: summary.matched ?? summary.Matched ?? 0,
      amountMismatch: summary.amountMismatch ?? summary.AmountMismatch ?? 0,
      missingInA: summary.missingInA ?? summary.MissingInA ?? 0,
      missingInB: summary.missingInB ?? summary.MissingInB ?? 0,
      duplicates: summary.duplicates ?? summary.Duplicates ?? 0,
      potentialMatches: summary.potentialMatches ?? summary.PotentialMatches ?? 0,
    },
    results,
  };
}

function normalizeEntry(e: any) {
  if (!e) return null;
  return {
    rowIndex: e.rowIndex ?? e.RowIndex ?? 0,
    company: e.company ?? e.Company ?? "",
    date: e.date ?? e.Date ?? null,
    particulars: e.particulars ?? e.Particulars ?? "",
    voucherNo: e.voucherNo ?? e.VoucherNo ?? "",
    voucherRef: e.voucherRef ?? e.VoucherRef ?? "",
    debit: e.debit ?? e.Debit ?? 0,
    credit: e.credit ?? e.Credit ?? 0,
    amount: e.amount ?? e.Amount ?? 0,
    side: e.side ?? e.Side ?? "",
  };
}
