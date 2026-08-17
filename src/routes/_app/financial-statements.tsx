import { createFileRoute, Link } from "@tanstack/react-router";
import { memo, useCallback, useMemo, useRef, useState } from "react";
import {
  ArrowLeft,
  Download,
  FileSpreadsheet,
  Loader2,
  Upload,
  AlertTriangle,
  CheckCircle2,
} from "lucide-react";
import { toast } from "sonner";
import { getApiUrl } from "@/lib/api-config";
import { cn } from "@/lib/utils";
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  formatLakhs,
  normalizeFinancialStatementResult,
  normalizeTbMapping,
  normalizeTbPreview,
  slugifyCompanyKey,
  type FinancialStatementResult,
  type TrialBalanceColumnMapping,
  type TrialBalancePreview,
} from "@/lib/financial-statement-types";

export const Route = createFileRoute("/_app/financial-statements")({
  head: () => ({ meta: [{ title: "Financial Statements — PO Portal" }] }),
  component: FinancialStatementsPage,
});

type ActiveTab = "schedules" | "balance-sheet" | "pl" | "unmapped";

function FinancialStatementsPage() {
  const fileRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<TrialBalancePreview | null>(null);
  const [mapping, setMapping] = useState<TrialBalanceColumnMapping | null>(null);
  const [companyName, setCompanyName] = useState("");
  const [periodLabel, setPeriodLabel] = useState("");
  const [loadingPreview, setLoadingPreview] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [result, setResult] = useState<FinancialStatementResult | null>(null);
  const [activeTab, setActiveTab] = useState<ActiveTab>("schedules");

  const companyKey = useMemo(() => slugifyCompanyKey(companyName || "default"), [companyName]);

  const mappingFields: { key: keyof TrialBalanceColumnMapping; label: string; required?: boolean }[] = [
    { key: "particulars", label: "Ledger / Particulars", required: true },
    { key: "opening", label: "Opening" },
    { key: "debit", label: "Debit" },
    { key: "credit", label: "Credit" },
    { key: "closing", label: "Closing" },
    { key: "adjustedClosing", label: "Adjusted closing (optional)" },
    { key: "group", label: "Group (optional if mapping master exists)" },
  ];

  const onFileChange = useCallback(async (selected: File | null) => {
    setFile(selected);
    setPreview(null);
    setMapping(null);
    setResult(null);
    if (!selected) return;

    setLoadingPreview(true);
    try {
      const fd = new FormData();
      fd.append("file", selected);
      const res = await fetch(getApiUrl("/api/financial-statements/preview"), {
        method: "POST",
        body: fd,
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to preview trial balance");
      const parsed = normalizeTbPreview(data);
      setPreview(parsed);
      setMapping(parsed.suggestedMapping);
      if (!companyName && parsed.fileName) {
        const base = parsed.fileName.replace(/\.(xlsx|xls)$/i, "");
        setCompanyName(base.split("_")[0]?.trim() || "");
      }
    } catch (err: any) {
      toast.error(err.message || "Preview failed");
    } finally {
      setLoadingPreview(false);
    }
  }, [companyName]);

  async function generateStatements() {
    if (!file || !mapping) {
      toast.error("Upload a trial balance file first.");
      return;
    }
    if (!companyName.trim()) {
      toast.error("Enter a company name.");
      return;
    }

    setGenerating(true);
    try {
      const fd = new FormData();
      fd.append("file", file);
      fd.append(
        "requestJson",
        JSON.stringify({
          companyKey,
          companyName: companyName.trim(),
          periodLabel: periodLabel.trim(),
          mapping: {
            ...mapping,
            sheetName: preview?.selectedSheet || mapping.sheetName,
            headerRow: preview?.headerRow || mapping.headerRow,
          },
        }),
      );

      const res = await fetch(getApiUrl("/api/financial-statements/generate"), {
        method: "POST",
        body: fd,
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to generate statements");

      const parsed = normalizeFinancialStatementResult(data);
      setResult(parsed);
      setActiveTab(parsed.unmappedLedgers > 0 ? "unmapped" : "schedules");
      toast.success(
        `Generated for ${parsed.mappedLedgers}/${parsed.totalLedgers} mapped ledgers.`,
      );
    } catch (err: any) {
      toast.error(err.message || "Generation failed");
    } finally {
      setGenerating(false);
    }
  }

  async function exportExcel() {
    if (!result) return;
    setExporting(true);
    try {
      const res = await fetch(getApiUrl("/api/financial-statements/export"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(result),
      });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.message || "Export failed");
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `financial-statements-${companyKey}-${new Date().toISOString().slice(0, 10)}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success("Excel exported");
    } catch (err: any) {
      toast.error(err.message || "Export failed");
    } finally {
      setExporting(false);
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to="/ledgers"
            className="mb-2 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Ledgers
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Financial Statements</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Upload a trial balance to auto-generate schedules, balance sheet, and profit &amp; loss
            using the company&apos;s ledger-to-schedule mapping.
          </p>
        </div>
        {result && (
          <Button variant="outline" onClick={exportExcel} disabled={exporting}>
            {exporting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Download className="mr-2 h-4 w-4" />}
            Export Excel
          </Button>
        )}
      </div>

      <section className="rounded-xl border border-border bg-card p-5 shadow-sm">
        <h2 className="text-base font-semibold">1. Upload trial balance</h2>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div className="space-y-2 md:col-span-1">
            <Label htmlFor="company-name">Company name</Label>
            <Input
              id="company-name"
              placeholder="e.g. Plastene India Limited"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">Key: {companyKey}</p>
          </div>
          <div className="space-y-2 md:col-span-1">
            <Label htmlFor="period-label">Period / as-at date</Label>
            <Input
              id="period-label"
              placeholder="e.g. 31 March 2026"
              value={periodLabel}
              onChange={(e) => setPeriodLabel(e.target.value)}
            />
          </div>
          <div className="space-y-2 md:col-span-1">
            <Label>Trial balance file</Label>
            <input
              ref={fileRef}
              type="file"
              accept=".xlsx,.xls"
              className="hidden"
              onChange={(e) => onFileChange(e.target.files?.[0] ?? null)}
            />
            <Button
              type="button"
              variant="secondary"
              className="w-full justify-start"
              onClick={() => fileRef.current?.click()}
              disabled={loadingPreview}
            >
              {loadingPreview ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Upload className="mr-2 h-4 w-4" />
              )}
              {file ? file.name : "Choose Excel file"}
            </Button>
          </div>
        </div>

        {preview && mapping && (
          <div className="mt-5 space-y-4 border-t border-border pt-5">
            <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
              <span className="inline-flex items-center gap-1.5">
                <FileSpreadsheet className="h-4 w-4" />
                Sheet: <strong className="text-foreground">{preview.selectedSheet}</strong>
              </span>
              <span>Header row: {preview.headerRow}</span>
              <span>{preview.dataRowCount.toLocaleString()} ledger rows detected</span>
            </div>

            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {mappingFields.map(({ key, label }) => (
                <div key={key} className="space-y-1.5">
                  <Label>{label}</Label>
                  <Select
                    value={(mapping[key] as string) || "__none__"}
                    onValueChange={(v) =>
                      setMapping((prev) =>
                        prev ? { ...prev, [key]: v === "__none__" ? "" : v } : prev,
                      )
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select column" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__none__">— Not mapped —</SelectItem>
                      {preview.headers.map((h) => (
                        <SelectItem key={h} value={h}>
                          {h || "(blank)"}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ))}
            </div>

            <Button onClick={generateStatements} disabled={generating}>
              {generating ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <CheckCircle2 className="mr-2 h-4 w-4" />
              )}
              Generate schedules, BS &amp; P&amp;L
            </Button>
          </div>
        )}
      </section>

      {result && (
        <section className="rounded-xl border border-border bg-card shadow-sm">
          <SummaryBar result={result} />

          <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as ActiveTab)} className="p-4 pt-0">
            <TabsList className="mb-4 flex h-auto flex-wrap gap-1">
              <TabsTrigger value="schedules">Schedules</TabsTrigger>
              <TabsTrigger value="balance-sheet">Balance Sheet</TabsTrigger>
              <TabsTrigger value="pl">P&amp;L</TabsTrigger>
              <TabsTrigger value="unmapped" className="gap-1.5">
                Unmapped
                {result.unmappedLedgers > 0 && (
                  <span className="rounded-full bg-amber-500/15 px-1.5 py-0.5 text-xs text-amber-700 dark:text-amber-300">
                    {result.unmappedLedgers}
                  </span>
                )}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="schedules">
              <SchedulesView schedules={result.schedules} />
            </TabsContent>
            <TabsContent value="balance-sheet">
              <BalanceSheetView sections={result.balanceSheet} />
            </TabsContent>
            <TabsContent value="pl">
              <ProfitLossView lines={result.profitAndLoss} />
            </TabsContent>
            <TabsContent value="unmapped">
              <UnmappedView items={result.unmapped} />
            </TabsContent>
          </Tabs>
        </section>
      )}
    </div>
  );
}

const SummaryBar = memo(function SummaryBar({ result }: { result: FinancialStatementResult }) {
  const balanced = Math.abs(result.balanceSheetTotalLakhs) < 1;
  return (
    <div className="grid gap-3 border-b border-border p-4 sm:grid-cols-2 lg:grid-cols-4">
      <Stat label="Mapped ledgers" value={`${result.mappedLedgers} / ${result.totalLedgers}`} />
      <Stat label="Total assets (Lakhs)" value={formatLakhs(result.totalAssetsLakhs)} />
      <Stat
        label="Total liabilities & equity (Lakhs)"
        value={formatLakhs(result.totalLiabilitiesAndEquityLakhs)}
      />
      <div className="rounded-lg bg-secondary/40 px-3 py-2">
        <p className="text-xs text-muted-foreground">Balance check (Assets − L&amp;E)</p>
        <p
          className={cn(
            "mt-0.5 flex items-center gap-1.5 text-sm font-semibold tabular-nums",
            balanced ? "text-emerald-600 dark:text-emerald-400" : "text-amber-600 dark:text-amber-400",
          )}
        >
          {balanced ? <CheckCircle2 className="h-4 w-4" /> : <AlertTriangle className="h-4 w-4" />}
          {formatLakhs(result.balanceSheetTotalLakhs)}
        </p>
      </div>
    </div>
  );
});

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-secondary/40 px-3 py-2">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-semibold tabular-nums">{value}</p>
    </div>
  );
}

function SchedulesView({ schedules }: { schedules: FinancialStatementResult["schedules"] }) {
  return (
    <div className="space-y-6">
      {schedules.map((note) => (
        <div key={note.note} className="overflow-hidden rounded-lg border border-border">
          <div className="flex items-center justify-between bg-secondary/30 px-4 py-2.5">
            <h3 className="text-sm font-semibold">
              Note {note.note}: {note.title}
            </h3>
            <span className="text-sm font-semibold tabular-nums">{formatLakhs(note.totalLakhs)}</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-sm">
              <thead>
                <tr className="border-b border-border bg-card text-left text-muted-foreground">
                  <th className="px-4 py-2 font-medium">Line item</th>
                  <th className="px-4 py-2 text-right font-medium">Ledgers</th>
                  <th className="px-4 py-2 text-right font-medium">Amount (Lakhs)</th>
                </tr>
              </thead>
              <tbody>
                {note.lines.map((line) => (
                  <tr key={line.group} className="border-b border-border/60 last:border-0">
                    <td className="px-4 py-2">{line.label}</td>
                    <td className="px-4 py-2 text-right tabular-nums text-muted-foreground">
                      {line.ledgerCount || "—"}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums font-medium">
                      {formatLakhs(line.amountLakhs)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ))}
    </div>
  );
}

function BalanceSheetView({ sections }: { sections: FinancialStatementResult["balanceSheet"] }) {
  return (
    <div className="space-y-6">
      {sections.map((section) => (
        <div key={section.title} className="overflow-hidden rounded-lg border border-border">
          <div className="bg-secondary/30 px-4 py-2.5">
            <h3 className="text-sm font-semibold">{section.title}</h3>
          </div>
          <ReportTable lines={section.lines} totalLabel={`Total — ${section.title}`} total={section.sectionTotalLakhs} />
        </div>
      ))}
    </div>
  );
}

function ProfitLossView({ lines }: { lines: FinancialStatementResult["profitAndLoss"] }) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <div className="bg-secondary/30 px-4 py-2.5">
        <h3 className="text-sm font-semibold">Statement of Profit and Loss</h3>
      </div>
      <ReportTable lines={lines} />
    </div>
  );
}

function ReportTable({
  lines,
  totalLabel,
  total,
}: {
  lines: FinancialStatementResult["profitAndLoss"];
  totalLabel?: string;
  total?: number;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[560px] text-sm">
        <thead>
          <tr className="border-b border-border text-left text-muted-foreground">
            <th className="px-4 py-2 font-medium">Particulars</th>
            <th className="px-4 py-2 font-medium">Note</th>
            <th className="px-4 py-2 text-right font-medium">Amount (Lakhs)</th>
          </tr>
        </thead>
        <tbody>
          {lines.map((line, idx) => (
            <tr
              key={`${line.label}-${idx}`}
              className={cn(
                "border-b border-border/60 last:border-0",
                line.isHeader && "bg-muted/20",
                line.isSubtotal && "bg-secondary/20 font-semibold",
              )}
            >
              <td className={cn("px-4 py-2", line.isHeader && "italic text-muted-foreground")}>
                {line.label}
              </td>
              <td className="px-4 py-2 text-muted-foreground">{line.note || "—"}</td>
              <td className="px-4 py-2 text-right tabular-nums">
                {line.isHeader ? "" : formatLakhs(line.amountLakhs)}
              </td>
            </tr>
          ))}
          {totalLabel != null && total != null && (
            <tr className="bg-secondary/30 font-semibold">
              <td className="px-4 py-2" colSpan={2}>
                {totalLabel}
              </td>
              <td className="px-4 py-2 text-right tabular-nums">{formatLakhs(total)}</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function UnmappedView({ items }: { items: FinancialStatementResult["unmapped"] }) {
  if (items.length === 0) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/5 px-4 py-6 text-sm text-emerald-700 dark:text-emerald-300">
        <CheckCircle2 className="h-5 w-5 shrink-0" />
        All ledgers are mapped to schedule groups.
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-amber-500/30">
      <div className="flex items-center gap-2 bg-amber-500/10 px-4 py-2.5 text-sm text-amber-800 dark:text-amber-200">
        <AlertTriangle className="h-4 w-4 shrink-0" />
        These ledgers need a schedule group mapping before they appear in schedules / BS / P&amp;L.
      </div>
      <div className="max-h-[480px] overflow-auto">
        <table className="w-full text-sm">
          <thead className="sticky top-0 bg-card">
            <tr className="border-b border-border text-left text-muted-foreground">
              <th className="px-4 py-2 font-medium">Ledger</th>
              <th className="px-4 py-2 text-right font-medium">Closing (Lakhs)</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.ledger} className="border-b border-border/60">
                <td className="px-4 py-2">{item.ledger}</td>
                <td className="px-4 py-2 text-right tabular-nums">{formatLakhs(item.closingLakhs)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
