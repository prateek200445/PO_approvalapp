import { createFileRoute, Link } from "@tanstack/react-router";
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
  ChevronUp,
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
  billNo: null,
  billDate: null,
  amount: null,
  debit: null,
  credit: null,
  sheetName: null,
});

const RESULTS_PAGE_SIZE = 15;

type SortKey = "status" | "date" | "sideA" | "amountA" | "sideB" | "amountB" | "diff";
type SortDir = "asc" | "desc";

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
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("Matched");
  const [q, setQ] = useState("");
  const [selected, setSelected] = useState<ComparisonPair | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDir, setSortDir] = useState<SortDir>("asc");

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
          billNo: keepOrSuggest(prev.mapping.billNo, preview.headers, preview.suggestedMapping.billNo),
          billDate: keepOrSuggest(prev.mapping.billDate, preview.headers, preview.suggestedMapping.billDate),
          amount: keepOrSuggest(prev.mapping.amount, preview.headers, preview.suggestedMapping.amount),
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
      if (!side.mapping.date) return `${label}: Voucher Date column is required.`;
      if (!side.mapping.amount && (!side.mapping.debit || !side.mapping.credit)) {
        return `${label}: Amount column (or Debit + Credit) is required.`;
      }
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
      setStatusFilter("Matched");
      setSelected(null);
      setCurrentPage(1);
      requestAnimationFrame(() => {
        const scrollTop = () => {
          window.scrollTo({ top: 0, behavior: "instant" });
          document.documentElement.scrollTop = 0;
          document.body.scrollTop = 0;
          const mainContent = document.getElementById("main-content");
          if (mainContent) mainContent.scrollTop = 0;
        };
        scrollTop();
        setTimeout(scrollTop, 50);
      });
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
        if (r.status !== "Duplicate" && r.status !== "PotentialMatch" && r.status !== "PendingRecord") return false;
      } else if (statusFilter !== "all" && r.status !== statusFilter) {
        return false;
      }
      if (!query) return true;
      const norm = (s: string) => s.toLowerCase().replace(/\s+/g, "");
      const queryNorm = norm(query);
      const entryBits = (e?: LedgerEntry | null) =>
        e ? [e.particulars, e.voucherNo, e.billNo, e.voucherRef] : [];
      const billNos = [
        r.entryA?.billNo,
        r.entryB?.billNo,
        ...(r.entriesA ?? []).map((e) => e.billNo),
        ...(r.entriesB ?? []).map((e) => e.billNo),
      ].filter(Boolean) as string[];
      if (billNos.some((b) => norm(b).includes(queryNorm))) return true;
      const hay = [
        r.message,
        ...entryBits(r.entryA),
        ...entryBits(r.entryB),
        ...(r.entriesA ?? []).flatMap((e) => entryBits(e)),
        ...(r.entriesB ?? []).flatMap((e) => entryBits(e)),
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return hay.includes(query);
    });
  }, [result, statusFilter, q]);

  const sorted = useMemo(() => {
    if (!sortKey) return filtered;
    const dir = sortDir === "asc" ? 1 : -1;
    const statusRank: Record<string, number> = {
      Matched: 0,
      AmountMismatch: 1,
      MissingInA: 2,
      MissingInB: 3,
      Duplicate: 4,
      PotentialMatch: 5,
      PendingRecord: 6,
    };
    const dateValue = (r: ComparisonPair) => {
      const raw = r.entryA?.billDate || r.entryA?.date || r.entryB?.billDate || r.entryB?.date;
      if (!raw) return 0;
      const t = new Date(raw).getTime();
      return Number.isNaN(t) ? 0 : t;
    };
    const amount = (e?: LedgerEntry | null) => e?.signedAmount ?? e?.amount ?? null;
    const text = (v: string) => v.trim().toLowerCase();

    return [...filtered].sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case "status":
          cmp = (statusRank[a.status] ?? 99) - (statusRank[b.status] ?? 99);
          break;
        case "date":
          cmp = dateValue(a) - dateValue(b);
          break;
        case "sideA":
          cmp = text(pairSideLabel(a, "A")).localeCompare(text(pairSideLabel(b, "A")));
          break;
        case "sideB":
          cmp = text(pairSideLabel(a, "B")).localeCompare(text(pairSideLabel(b, "B")));
          break;
        case "amountA": {
          const av = amount(a.entryA);
          const bv = amount(b.entryA);
          if (av == null && bv == null) cmp = 0;
          else if (av == null) cmp = 1;
          else if (bv == null) cmp = -1;
          else cmp = av - bv;
          break;
        }
        case "amountB": {
          const av = amount(a.entryB);
          const bv = amount(b.entryB);
          if (av == null && bv == null) cmp = 0;
          else if (av == null) cmp = 1;
          else if (bv == null) cmp = -1;
          else cmp = av - bv;
          break;
        }
        case "diff": {
          const av = a.difference ?? null;
          const bv = b.difference ?? null;
          if (av == null && bv == null) cmp = 0;
          else if (av == null) cmp = 1;
          else if (bv == null) cmp = -1;
          else cmp = av - bv;
          break;
        }
      }
      return cmp * dir;
    });
  }, [filtered, sortKey, sortDir]);

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
    setStatusFilter((prev) => (prev === next ? "Matched" : next));
  }

  function isPieSliceActive(filter: StatusFilter) {
    if (statusFilter === filter) return true;
    if (filter === "missing" && (statusFilter === "MissingInA" || statusFilter === "MissingInB")) return true;
    if (filter === "other" && (statusFilter === "Duplicate" || statusFilter === "PotentialMatch" || statusFilter === "PendingRecord")) return true;
    return false;
  }

  const companyNameA = result?.companyNameA?.trim() || "Company A";
  const companyNameB = result?.companyNameB?.trim() || "Company B";
  const pieHighlightActive = statusFilter !== "issues" && statusFilter !== "all";

  const totalPages = Math.max(1, Math.ceil(sorted.length / RESULTS_PAGE_SIZE));
  const pageRows = useMemo(() => {
    const start = (currentPage - 1) * RESULTS_PAGE_SIZE;
    return sorted.slice(start, start + RESULTS_PAGE_SIZE);
  }, [sorted, currentPage]);

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDir("asc");
    }
    setCurrentPage(1);
  }

  const hasActiveFilters = q.trim() !== "" || statusFilter !== "Matched" || sortKey != null;

  function clearFilters() {
    setQ("");
    setStatusFilter("Matched");
    setSortKey(null);
    setSortDir("asc");
    setCurrentPage(1);
  }

  function applyManualMatch(pairId: string, selectedRowIndicesA: number[], selectedRowIndicesB: number[]) {
    setResult((prev) => {
      if (!prev) return prev;
      const source = prev.results.find((r) => r.id === pairId);
      if (!source) return prev;

      const rowsA = source.entriesA?.length ? source.entriesA : source.entryA ? [source.entryA] : [];
      const rowsB = source.entriesB?.length ? source.entriesB : source.entryB ? [source.entryB] : [];
      const selectedA = rowsA.filter((e) => selectedRowIndicesA.includes(e.rowIndex));
      const selectedB = rowsB.filter((e) => selectedRowIndicesB.includes(e.rowIndex));
      if (selectedA.length === 0 || selectedB.length === 0) return prev;

      const totalA = sumSignedAmount(selectedA);
      const totalB = sumSignedAmount(selectedB);
      if (!canManualMatchTotals(totalA, totalB)) return prev;

      const remainingA = rowsA.filter((e) => !selectedRowIndicesA.includes(e.rowIndex));
      const remainingB = rowsB.filter((e) => !selectedRowIndicesB.includes(e.rowIndex));

      const matchedPair: ComparisonPair = {
        id: `${pairId}-manual-${Date.now()}`,
        status: "Matched",
        matchKind: "bill-group",
        message: `Manually matched (${selectedA.length} ↔ ${selectedB.length} lines)`,
        difference: 0,
        entryA: summarizeEntries(selectedA, source.entryA?.company ?? source.entriesA?.[0]?.company ?? ""),
        entryB: summarizeEntries(selectedB, source.entryB?.company ?? source.entriesB?.[0]?.company ?? ""),
        entriesA: selectedA,
        entriesB: selectedB,
      };

      const nextResults = prev.results.filter((r) => r.id !== pairId);
      nextResults.push(matchedPair);

      if (remainingA.length > 0 || remainingB.length > 0) {
        const pendingPair: ComparisonPair = {
          id: `${pairId}-pending-${Date.now()}`,
          status: "PendingRecord",
          matchKind: "bill-group",
          message: "Residual lines pending record after manual match",
          difference: Math.abs(Math.abs(sumSignedAmount(remainingA)) - Math.abs(sumSignedAmount(remainingB))),
          entryA: remainingA.length ? summarizeEntries(remainingA, matchedPair.entryA?.company ?? "") : null,
          entryB: remainingB.length ? summarizeEntries(remainingB, matchedPair.entryB?.company ?? "") : null,
          entriesA: remainingA,
          entriesB: remainingB,
        };
        nextResults.push(pendingPair);
      }

      const nextSummary = buildSummaryFromResults(prev.summary.totalA, prev.summary.totalB, nextResults);
      return { ...prev, results: nextResults, summary: nextSummary };
    });
    setSelected(null);
  }

  function markAsAmountMismatch(pairId: string) {
    setResult((prev) => {
      if (!prev) return prev;
      const source = prev.results.find((r) => r.id === pairId);
      if (!source) return prev;

      const rowsA = source.entriesA?.length ? source.entriesA : source.entryA ? [source.entryA] : [];
      const rowsB = source.entriesB?.length ? source.entriesB : source.entryB ? [source.entryB] : [];
      const difference = Math.abs(Math.abs(sumSignedAmount(rowsA)) - Math.abs(sumSignedAmount(rowsB)));

      const updatedPair: ComparisonPair = {
        ...source,
        status: "AmountMismatch",
        message: source.message?.startsWith("Manual amount mismatch")
          ? source.message
          : `Manual amount mismatch: ${source.message}`,
        difference,
      };

      const nextResults = prev.results.map((r) => (r.id === pairId ? updatedPair : r));
      const nextSummary = buildSummaryFromResults(prev.summary.totalA, prev.summary.totalB, nextResults);
      return { ...prev, results: nextResults, summary: nextSummary };
    });
    setSelected(null);
  }

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
          <Link
            to="/ledgers"
            className="mb-2 inline-flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground"
          >
            ← Ledgers
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Ledger Reconciliation</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Compare two company ledgers. Match by Bill No first (including swapped formats), then Voucher Date; final reconciliation requires opposite signed amounts.
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
              1) Group by exact Bill No + Bill Date and compare bill totals (many lines can sum to one). Opposite signs required. 2) Rows without Bill No fall back to Voucher Date. Different Bill Nos never match.
            </p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
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
                  value={result.summary.duplicates + result.summary.potentialMatches + (result.summary.pendingRecords ?? 0)}
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
                  value={result.summary.duplicates + result.summary.potentialMatches + (result.summary.pendingRecords ?? 0)}
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
                placeholder="Search bill no, particulars, voucher no…"
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
                <option value="PendingRecord">Pending record</option>
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <button
              type="button"
              onClick={clearFilters}
              disabled={!hasActiveFilters}
              className="inline-flex h-10 shrink-0 items-center justify-center gap-1.5 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
              <X className="h-3.5 w-3.5" />
              Clear filters
            </button>
          </div>

          <p className="text-xs text-muted-foreground">
            Showing {sorted.length === 0 ? 0 : (currentPage - 1) * RESULTS_PAGE_SIZE + 1}
            –{Math.min(currentPage * RESULTS_PAGE_SIZE, sorted.length)} of {sorted.length.toLocaleString("en-IN")}
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
                      {pairListTitle(row)}
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
                      {row.matchKind === "bill-group" && (
                        <span className="rounded-md bg-secondary/70 px-1.5 py-0.5 font-medium text-foreground/80">
                          {lineCountLabel(row)} lines
                        </span>
                      )}
                      <span>
                        {formatDate(row.entryA?.billDate || row.entryA?.date || row.entryB?.billDate || row.entryB?.date)}
                        {row.matchKind !== "bill-group" && (row.entryA?.billNo || row.entryB?.billNo)
                          ? ` · ${row.entryA?.billNo || row.entryB?.billNo}`
                          : ""}
                      </span>
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
                    <SortableTh label="Status" column="status" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label="Date" column="date" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label={companyNameA} column="sideA" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label="Amount" column="amountA" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label={companyNameB} column="sideB" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label="Amount" column="amountB" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                    <SortableTh label="Diff" column="diff" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
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
                          {formatDate(row.entryA?.billDate || row.entryA?.date || row.entryB?.billDate || row.entryB?.date)}
                        </td>
                        <td className="px-5 py-2.5 text-center break-words">
                          <div className="leading-snug">{pairSideLabel(row, "A")}</div>
                          {row.matchKind === "bill-group" && (row.entriesA?.length ?? 0) > 1 && (
                            <div className="mt-0.5 text-[11px] text-muted-foreground">
                              {row.entriesA!.length} lines
                            </div>
                          )}
                        </td>
                        <td className="px-5 py-2.5">
                          <div className="flex justify-center">
                            <AmountSideCell entry={row.entryA} />
                          </div>
                        </td>
                        <td className="px-5 py-2.5 text-center break-words">
                          <div className="leading-snug">{pairSideLabel(row, "B")}</div>
                          {row.matchKind === "bill-group" && (row.entriesB?.length ?? 0) > 1 && (
                            <div className="mt-0.5 text-[11px] text-muted-foreground">
                              {row.entriesB!.length} lines
                            </div>
                          )}
                        </td>
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
          onManualMatch={applyManualMatch}
          onMarkAmountMismatch={markAsAmountMismatch}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}

function SortableTh({
  label,
  column,
  sortKey,
  sortDir,
  onSort,
}: {
  label: string;
  column: SortKey;
  sortKey: SortKey | null;
  sortDir: SortDir;
  onSort: (key: SortKey) => void;
}) {
  const active = sortKey === column;
  return (
    <th className="px-5 py-2.5 text-center font-medium">
      <button
        type="button"
        onClick={() => onSort(column)}
        className={cn(
          "inline-flex max-w-full items-center justify-center gap-1 rounded-md px-1 py-0.5 transition hover:text-foreground",
          active ? "text-foreground" : "text-muted-foreground",
        )}
      >
        <span className="truncate">{label}</span>
        <span className="inline-flex shrink-0 flex-col leading-none" aria-hidden>
          <ChevronUp
            className={cn(
              "h-3 w-3 -mb-0.5",
              active && sortDir === "asc" ? "text-foreground" : "text-muted-foreground/40",
            )}
          />
          <ChevronDown
            className={cn(
              "h-3 w-3 -mt-0.5",
              active && sortDir === "desc" ? "text-foreground" : "text-muted-foreground/40",
            )}
          />
        </span>
      </button>
    </th>
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
    { key: "company", label: "Company Name" },
    { key: "date", label: "Voucher Date", required: true },
    { key: "amount", label: "Amount (signed)", required: true, amount: true },
    { key: "billNo", label: "Bill No" },
    { key: "billDate", label: "Bill Date" },
    { key: "particulars", label: "Ledger Name / Particulars" },
    { key: "voucherNo", label: "Voucher No" },
    { key: "debit", label: "Debit (legacy)", amount: true },
    { key: "credit", label: "Credit (legacy)", amount: true },
  ];

  function optionsFor(field: (typeof fields)[number]) {
    if (field.key === "amount") {
      // Prefer plain Amount; still hide obvious FC amount columns.
      const nonFc = headers.filter((h) => !isFcAmountHeader(h));
      return nonFc.length > 0 ? nonFc : headers;
    }
    if (!field.amount) return headers;
    const inr = headers.filter(isInrAmountHeader);
    const nonFc = headers.filter((h) => !isFcAmountHeader(h));
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
        Prefer signed Amount (− debit / + credit). Legacy Debit/Credit columns are optional when Amount is mapped.
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
    PendingRecord: "bg-secondary/80 text-foreground border-border",
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
  onManualMatch,
  onMarkAmountMismatch,
  onClose,
}: {
  pair: ComparisonPair;
  companyNameA: string;
  companyNameB: string;
  onManualMatch: (pairId: string, selectedRowIndicesA: number[], selectedRowIndicesB: number[]) => void;
  onMarkAmountMismatch: (pairId: string) => void;
  onClose: () => void;
}) {
  const isBillGroup = pair.matchKind === "bill-group";
  const entriesA = pair.entriesA?.length ? pair.entriesA : pair.entryA ? [pair.entryA] : [];
  const entriesB = pair.entriesB?.length ? pair.entriesB : pair.entryB ? [pair.entryB] : [];
  const [selectedA, setSelectedA] = useState<number[]>([]);
  const [selectedB, setSelectedB] = useState<number[]>([]);
  const canManual = pair.status === "PotentialMatch" || pair.status === "AmountMismatch";

  useEffect(() => {
    setSelectedA(entriesA.map((e) => e.rowIndex));
    setSelectedB(entriesB.map((e) => e.rowIndex));
  }, [pair.id]);

  const selectedTotalA = sumSignedAmount(entriesA.filter((e) => selectedA.includes(e.rowIndex)));
  const selectedTotalB = sumSignedAmount(entriesB.filter((e) => selectedB.includes(e.rowIndex)));
  const diff = Math.abs(Math.abs(selectedTotalA) - Math.abs(selectedTotalB));
  const canConfirm =
    canManual && selectedA.length > 0 && selectedB.length > 0 && canManualMatchTotals(selectedTotalA, selectedTotalB);

  const fields: { label: string; a: string; b: string }[] = isBillGroup
    ? [
        {
          label: "Bill No",
          a: pair.entryA?.billNo || "—",
          b: pair.entryB?.billNo || "—",
        },
        {
          label: "Bill Date",
          a: formatDate(pair.entryA?.billDate),
          b: formatDate(pair.entryB?.billDate),
        },
        {
          label: "Lines",
          a: entriesA.length ? String(entriesA.length) : "—",
          b: entriesB.length ? String(entriesB.length) : "—",
        },
        {
          label: "Bill total",
          a: pair.entryA ? formatSigned(pair.entryA.signedAmount ?? pair.entryA.amount, pair.entryA.side) : "—",
          b: pair.entryB ? formatSigned(pair.entryB.signedAmount ?? pair.entryB.amount, pair.entryB.side) : "—",
        },
        {
          label: "Side",
          a: pair.entryA?.side || "—",
          b: pair.entryB?.side || "—",
        },
      ]
    : [
        {
          label: "Bill No",
          a: pair.entryA?.billNo || "—",
          b: pair.entryB?.billNo || "—",
        },
        {
          label: "Bill Date",
          a: formatDate(pair.entryA?.billDate),
          b: formatDate(pair.entryB?.billDate),
        },
        {
          label: "Voucher Date",
          a: formatDate(pair.entryA?.date),
          b: formatDate(pair.entryB?.date),
        },
        {
          label: "Voucher No",
          a: pair.entryA?.voucherNo || "—",
          b: pair.entryB?.voucherNo || "—",
        },
        {
          label: "Ledger",
          a: pair.entryA?.particulars || "—",
          b: pair.entryB?.particulars || "—",
        },
        {
          label: "Amount",
          a: pair.entryA ? formatSigned(pair.entryA.signedAmount ?? pair.entryA.amount, pair.entryA.side) : "—",
          b: pair.entryB ? formatSigned(pair.entryB.signedAmount ?? pair.entryB.amount, pair.entryB.side) : "—",
        },
        {
          label: "Side",
          a: pair.entryA?.side || "—",
          b: pair.entryB?.side || "—",
        },
      ];

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-3 sm:items-center sm:p-6"
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
              {isBillGroup && (
                <span className="rounded-md bg-secondary/70 px-2 py-0.5 text-[11px] font-medium text-foreground/80">
                  Bill · {lineCountLabel(pair)} lines
                </span>
              )}
              {pair.difference != null && (
                <span className="rounded-md border border-warning/30 bg-warning/10 px-2 py-0.5 text-xs font-semibold tabular-nums text-warning">
                  Diff ₹{formatNum(pair.difference)}
                </span>
              )}
            </div>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{pair.message}</p>
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

        <div className="overflow-y-auto px-4 py-4 sm:px-6 sm:py-5 space-y-4">
          <div className="overflow-hidden rounded-xl border border-border">
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

          {(isBillGroup || entriesA.length > 1 || entriesB.length > 1) && (
            <div className="space-y-2">
              <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Line breakdown
              </div>
              <div className="grid gap-3 md:grid-cols-2">
                <LineList
                  title={companyNameA}
                  entries={entriesA}
                  selectable={canManual}
                  selected={selectedA}
                  onToggle={(rowIndex) =>
                    setSelectedA((prev) =>
                      prev.includes(rowIndex) ? prev.filter((x) => x !== rowIndex) : [...prev, rowIndex],
                    )
                  }
                />
                <LineList
                  title={companyNameB}
                  entries={entriesB}
                  selectable={canManual}
                  selected={selectedB}
                  onToggle={(rowIndex) =>
                    setSelectedB((prev) =>
                      prev.includes(rowIndex) ? prev.filter((x) => x !== rowIndex) : [...prev, rowIndex],
                    )
                  }
                />
              </div>
            </div>
          )}

          {canManual && (
            <div className="rounded-xl border border-border bg-surface/30 px-4 py-3">
              <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Manual reconciliation
              </div>
              <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm tabular-nums">
                <span>
                  A selected: <b>{formatSigned(selectedTotalA)}</b>
                </span>
                <span>
                  B selected: <b>{formatSigned(selectedTotalB)}</b>
                </span>
                <span>
                  Diff:{" "}
                  <b className={canManualMatchTotals(selectedTotalA, selectedTotalB) ? "text-success" : "text-warning"}>
                    {formatSigned(diff)}
                  </b>
                </span>
              </div>
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  disabled={!canConfirm}
                  onClick={() => onManualMatch(pair.id, selectedA, selectedB)}
                  className="inline-flex h-9 items-center rounded-md bg-primary px-3 text-sm font-semibold text-primary-foreground disabled:opacity-50"
                >
                  Manually match selected
                </button>
                <button
                  type="button"
                  onClick={() => onMarkAmountMismatch(pair.id)}
                  className="inline-flex h-9 items-center rounded-md border border-warning/40 bg-warning/10 px-3 text-sm font-semibold text-warning hover:bg-warning/20"
                >
                  Mark as amount mismatch
                </button>
              </div>
            </div>
          )}

          {pair.difference != null && (
            <div className="rounded-xl border border-warning/30 bg-warning/10 px-4 py-3 text-sm">
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

function LineList({
  title,
  entries,
  selectable = false,
  selected = [],
  onToggle,
}: {
  title: string;
  entries: LedgerEntry[];
  selectable?: boolean;
  selected?: number[];
  onToggle?: (rowIndex: number) => void;
}) {
  if (entries.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border px-3 py-4 text-sm text-muted-foreground">
        <div className="font-semibold text-foreground">{title}</div>
        <p className="mt-2">No lines</p>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border bg-surface/30 p-3">
      <div className="flex items-center justify-between gap-2 border-b border-border/60 pb-2">
        <div className="text-sm font-semibold">{title}</div>
        <div className="text-[11px] tabular-nums text-muted-foreground">
          {entries.length} line{entries.length === 1 ? "" : "s"}
        </div>
      </div>
      <ul className="mt-2 divide-y divide-border/60">
        {entries.map((e, idx) => (
          <li key={`${e.rowIndex}-${idx}`} className="flex items-start justify-between gap-3 py-2.5 text-sm">
            <div className="min-w-0">
              {selectable && (
                <label className="mb-1 inline-flex items-center gap-1.5 text-[11px] text-muted-foreground">
                  <input
                    type="checkbox"
                    checked={selected.includes(e.rowIndex)}
                    onChange={() => onToggle?.(e.rowIndex)}
                  />
                  Select
                </label>
              )}
              <div className="truncate font-medium">{e.particulars || "—"}</div>
              <div className="mt-0.5 text-[11px] text-muted-foreground">
                {formatDate(e.date)}
                {e.voucherNo ? ` · ${e.voucherNo}` : ""}
              </div>
            </div>
            <div className="shrink-0 text-right">
              <div className="text-[10px] uppercase tracking-wide text-muted-foreground">{e.side}</div>
              <div className="tabular-nums font-semibold">
                {formatSigned(e.signedAmount ?? e.amount, e.side)}
              </div>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function pairListTitle(row: ComparisonPair) {
  if (row.matchKind === "bill-group") {
    return row.entryA?.billNo || row.entryB?.billNo || "Bill group";
  }
  return row.entryA?.particulars || row.entryB?.particulars || row.message;
}

function pairSideLabel(row: ComparisonPair, side: "A" | "B") {
  const entry = side === "A" ? row.entryA : row.entryB;
  if (!entry) return "—";
  if (row.matchKind === "bill-group") {
    const count = side === "A" ? row.entriesA?.length ?? 0 : row.entriesB?.length ?? 0;
    if (count > 1) return "Bill total";
    return entry.particulars || entry.billNo || "—";
  }
  return entry.particulars || "—";
}

function lineCountLabel(row: ComparisonPair) {
  const a = row.entriesA?.length || (row.entryA ? 1 : 0);
  const b = row.entriesB?.length || (row.entryB ? 1 : 0);
  return `${a} ↔ ${b}`;
}

function entrySignedAmount(e: LedgerEntry) {
  if (e.signedAmount != null && e.signedAmount !== 0) return e.signedAmount;
  if (e.side === "Debit") return -Math.abs(e.amount ?? 0);
  if (e.side === "Credit") return Math.abs(e.amount ?? 0);
  return e.amount ?? 0;
}

function sumSignedAmount(entries: LedgerEntry[]) {
  return entries.reduce((sum, e) => sum + entrySignedAmount(e), 0);
}

/** Match when opposite signs and absolute totals agree (e.g. +23,600 ↔ −23,600). */
function canManualMatchTotals(totalA: number, totalB: number) {
  if (totalA === 0 || totalB === 0) return false;
  if (Math.sign(totalA) === Math.sign(totalB)) return false;
  return Math.abs(Math.abs(totalA) - Math.abs(totalB)) <= 0.01;
}

function summarizeEntries(entries: LedgerEntry[], fallbackCompany: string): LedgerEntry | null {
  if (entries.length === 0) return null;
  const totalSigned = sumSignedAmount(entries);
  const first = entries[0];
  return {
    ...first,
    company: first.company || fallbackCompany,
    particulars: entries.length === 1 ? first.particulars : `Manual total (${entries.length} lines)`,
    voucherNo: entries.length === 1 ? first.voucherNo : "",
    voucherRef: "",
    signedAmount: totalSigned,
    debit: totalSigned < 0 ? Math.abs(totalSigned) : 0,
    credit: totalSigned > 0 ? totalSigned : 0,
    amount: Math.abs(totalSigned),
    side: totalSigned < 0 ? "Debit" : totalSigned > 0 ? "Credit" : "None",
  };
}

function buildSummaryFromResults(totalA: number, totalB: number, results: ComparisonPair[]) {
  return {
    totalA,
    totalB,
    matched: results.filter((r) => r.status === "Matched").length,
    amountMismatch: results.filter((r) => r.status === "AmountMismatch").length,
    missingInA: results.filter((r) => r.status === "MissingInA").length,
    missingInB: results.filter((r) => r.status === "MissingInB").length,
    duplicates: results.filter((r) => r.status === "Duplicate").length,
    potentialMatches: results.filter((r) => r.status === "PotentialMatch").length,
    pendingRecords: results.filter((r) => r.status === "PendingRecord").length,
  };
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

function formatSigned(n: number, side?: string) {
  const abs = Math.abs(n);
  const formatted = formatNum(abs);
  if (n < 0 || side === "Debit") return `−${formatted}`;
  if (n > 0 || side === "Credit") return `+${formatted}`;
  return formatNum(n);
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
      billNo: suggested.billNo ?? suggested.BillNo ?? null,
      billDate: suggested.billDate ?? suggested.BillDate ?? null,
      amount: suggested.amount ?? suggested.Amount ?? null,
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
    matchKind: r.matchKind ?? r.MatchKind ?? "row",
    entryA: normalizeEntry(r.entryA ?? r.EntryA),
    entryB: normalizeEntry(r.entryB ?? r.EntryB),
    entriesA: ((r.entriesA ?? r.EntriesA ?? []) as any[]).map(normalizeEntry).filter(Boolean) as LedgerEntry[],
    entriesB: ((r.entriesB ?? r.EntriesB ?? []) as any[]).map(normalizeEntry).filter(Boolean) as LedgerEntry[],
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
      pendingRecords: summary.pendingRecords ?? summary.PendingRecords ?? 0,
    },
    results,
  };
}

function normalizeEntry(e: any) {
  if (!e) return null;
  const signed =
    e.signedAmount ?? e.SignedAmount ?? null;
  const debit = e.debit ?? e.Debit ?? 0;
  const credit = e.credit ?? e.Credit ?? 0;
  const inferredSigned =
    signed != null
      ? Number(signed)
      : credit > 0
        ? Number(credit)
        : debit > 0
          ? -Number(debit)
          : 0;
  return {
    rowIndex: e.rowIndex ?? e.RowIndex ?? 0,
    company: e.company ?? e.Company ?? "",
    date: e.date ?? e.Date ?? null,
    billDate: e.billDate ?? e.BillDate ?? null,
    particulars: e.particulars ?? e.Particulars ?? "",
    voucherNo: e.voucherNo ?? e.VoucherNo ?? "",
    voucherRef: e.voucherRef ?? e.VoucherRef ?? "",
    billNo: e.billNo ?? e.BillNo ?? "",
    signedAmount: inferredSigned,
    debit,
    credit,
    amount: e.amount ?? e.Amount ?? Math.abs(inferredSigned),
    side: e.side ?? e.Side ?? (inferredSigned < 0 ? "Debit" : inferredSigned > 0 ? "Credit" : ""),
  };
}
