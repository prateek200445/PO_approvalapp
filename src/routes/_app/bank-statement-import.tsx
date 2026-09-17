import { useMemo, useRef, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import {
  ArrowLeft,
  Download,
  FileSpreadsheet,
  Loader2,
  Plus,
  Trash2,
  Upload,
} from "lucide-react";
import { toast } from "sonner";
import {
  convertBankStatement,
  categorizeBankRows,
  downloadBankStatementTemplate,
  emptyBankStatementRow,
  exportBankStatementCsv,
  formatBankAmount,
  parseBankStatementCsv,
  type BankStatementRow,
} from "@/lib/bank-statement-api";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";

export const Route = createFileRoute("/_app/bank-statement-import")({
  head: () => ({ meta: [{ title: "Bank Statement Import — PO Portal" }] }),
  component: BankStatementImportPage,
});

const DEFAULT_LEDGER = "Bank Overdraft BOB 03240400000456";

function BankStatementImportPage() {
  const [fileName, setFileName] = useState<string | null>(null);
  const [erpLedger, setErpLedger] = useState(DEFAULT_LEDGER);
  const [rows, setRows] = useState<BankStatementRow[]>([]);
  const [converting, setConverting] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [busyTemplate, setBusyTemplate] = useState(false);
  const [categorizePayments, setCategorizePayments] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const totals = useMemo(() => {
    let debit = 0;
    let credit = 0;
    for (const row of rows) {
      debit += row.debit ?? 0;
      credit += row.credit ?? 0;
    }
    return { debit, credit, count: rows.length };
  }, [rows]);

  const categorySummary = useMemo(() => {
    if (!categorizePayments) return [];
    const map = new Map<string, number>();
    for (const row of rows) {
      const key = row.mainCategory?.trim() || "Uncategorized";
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    return [...map.entries()].sort((a, b) => b[1] - a[1]);
  }, [rows, categorizePayments]);

  function applyCategorization(nextRows: BankStatementRow[], enabled: boolean) {
    return enabled ? categorizeBankRows(nextRows) : nextRows;
  }

  function onToggleCategorize(enabled: boolean) {
    setCategorizePayments(enabled);
    if (rows.length === 0) return;
    if (enabled) {
      setRows((prev) => categorizeBankRows(prev));
      toast.success("Payment categorization enabled");
    } else {
      toast.message("Categorization hidden — import columns unchanged");
    }
  }

  async function onFileChosen(file: File | null) {
    if (!file) return;
    setConverting(true);
    try {
      const lower = file.name.toLowerCase();
      const isPdfOrExcel =
        lower.endsWith(".pdf") || lower.endsWith(".xlsx") || lower.endsWith(".xlsm");

      let result;
      if (isPdfOrExcel) {
        result = await convertBankStatement(file, erpLedger);
      } else {
        // CSV: try convert first (raw bank or import format). Fall back to strict import parse.
        try {
          result = await convertBankStatement(file, erpLedger);
        } catch {
          result = await parseBankStatementCsv(file);
        }
      }

      setRows(applyCategorization(result.rows, categorizePayments));
      setFileName(result.fileName);
      if (result.erpBankLedgerName) setErpLedger(result.erpBankLedgerName);
      toast.success(
        result.message ??
          `Converted ${result.rowCount} entr${result.rowCount === 1 ? "y" : "ies"} to import format`,
      );
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to convert bank statement");
    } finally {
      setConverting(false);
    }
  }

  function updateRow(id: string, patch: Partial<BankStatementRow>) {
    setRows((prev) => prev.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  }

  function removeRow(id: string) {
    setRows((prev) => prev.filter((r) => r.id !== id));
  }

  function addRow() {
    const last = rows[rows.length - 1];
    setRows((prev) => [
      ...prev,
      emptyBankStatementRow({
        erpBankLedgerName: last?.erpBankLedgerName || erpLedger,
        valueDate: last?.valueDate ?? "",
      }),
    ]);
  }

  function applyLedgerToAll() {
    const ledger = erpLedger.trim();
    if (!ledger) {
      toast.error("Enter an ERP bank ledger name first.");
      return;
    }
    setRows((prev) => prev.map((r) => ({ ...r, erpBankLedgerName: ledger })));
    toast.success("ERP bank ledger applied to all rows");
  }

  function startBlank() {
    setFileName("new-bank-import.csv");
    setRows([emptyBankStatementRow({ erpBankLedgerName: erpLedger })]);
  }

  async function onExport() {
    if (rows.length === 0) {
      toast.error("Add at least one entry before exporting.");
      return;
    }
    setExporting(true);
    try {
      const outName = fileName
        ? `${fileName.replace(/\.(pdf|xlsx|xlsm|csv|txt)$/i, "")}-import.csv`
        : `bank-import-${new Date().toISOString().slice(0, 10)}.csv`;
      await exportBankStatementCsv(rows, outName, categorizePayments);
      toast.success(
        categorizePayments
          ? "Downloaded with categorization columns"
          : "Downloaded in BANKIMPORTFORMATE.csv layout",
      );
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(false);
    }
  }

  async function onTemplate() {
    setBusyTemplate(true);
    try {
      await downloadBankStatementTemplate();
      toast.success("Template downloaded");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Template download failed");
    } finally {
      setBusyTemplate(false);
    }
  }

  return (
    <div className="space-y-5 pb-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to="/bank-requirements"
            className="mb-2 inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Banking
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
            Bank Statement Import
          </h1>
          <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
            Upload a raw bank statement (BOB PDF, bank CSV/Excel). It is converted into your import
            format — ERP bank ledger, party, value date, narration, ref/cheque, debit, credit — then
            you can edit, add, or remove rows and download the CSV.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button type="button" variant="outline" onClick={() => void onTemplate()} disabled={busyTemplate}>
            {busyTemplate ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileSpreadsheet className="h-4 w-4" />}
            Blank template
          </Button>
          <Button type="button" variant="outline" onClick={startBlank}>
            <Plus className="h-4 w-4" />
            Start blank
          </Button>
          <Button type="button" onClick={() => void onExport()} disabled={exporting || rows.length === 0}>
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download import CSV
          </Button>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 shadow-sm space-y-3">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
          <div className="flex-1">
            <label className="text-xs font-medium text-muted-foreground">ERP bank ledger name</label>
            <Input
              value={erpLedger}
              onChange={(e) => setErpLedger(e.target.value)}
              placeholder="e.g. Bank Overdraft BOB 03240400000456"
              className="mt-1"
            />
          </div>
          <Button type="button" variant="outline" onClick={applyLedgerToAll} disabled={rows.length === 0}>
            Apply to all rows
          </Button>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-secondary/30 px-3 py-2.5">
          <div>
            <div className="text-sm font-medium">Categorize payments</div>
            <div className="text-xs text-muted-foreground">
              When on: add Transaction Type, Main Category, Sub-Category (editable). Toggle off to hide.
            </div>
          </div>
          <Switch checked={categorizePayments} onCheckedChange={onToggleCategorize} />
        </div>

        <button
          type="button"
          disabled={converting}
          onClick={() => fileRef.current?.click()}
          onDragEnter={(e) => {
            e.preventDefault();
            e.stopPropagation();
            setDragOver(true);
          }}
          onDragOver={(e) => {
            e.preventDefault();
            e.stopPropagation();
            setDragOver(true);
          }}
          onDragLeave={(e) => {
            e.preventDefault();
            e.stopPropagation();
            setDragOver(false);
          }}
          onDrop={(e) => {
            e.preventDefault();
            e.stopPropagation();
            setDragOver(false);
            const next = e.dataTransfer.files?.[0] ?? null;
            if (!next) {
              toast.error("No file dropped.");
              return;
            }
            void onFileChosen(next);
          }}
          className={cn(
            "flex w-full cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border border-dashed px-4 py-8 text-center transition",
            dragOver
              ? "border-primary bg-primary/10 scale-[1.01]"
              : fileName
                ? "border-primary/40 bg-primary/5 hover:bg-primary/10"
                : "border-border hover:bg-secondary/40",
            converting && "pointer-events-none opacity-70",
          )}
        >
          {converting ? (
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          ) : (
            <Upload className={cn("h-5 w-5", dragOver ? "text-primary" : "text-muted-foreground")} />
          )}
          <div className="text-sm font-medium">
            {dragOver
              ? "Drop file to upload"
              : fileName
                ? fileName
                : "Drop or choose bank statement (PDF / CSV / Excel)"}
          </div>
          <div className="max-w-xl text-xs text-muted-foreground">
            Drag and drop a raw BOB PDF or bank CSV/Excel here, or click to browse.
          </div>
        </button>
        <input
          ref={fileRef}
          type="file"
          accept=".pdf,.csv,.txt,.xlsx,.xlsm,application/pdf,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
          className="sr-only"
          onChange={(e) => {
            const next = e.target.files?.[0] ?? null;
            void onFileChosen(next);
            e.target.value = "";
          }}
        />
      </div>

      {rows.length > 0 ? (
        <>
          <div className="grid gap-3 sm:grid-cols-3">
            <Stat label="Entries" value={String(totals.count)} />
            <Stat label="Total debit" value={formatBankAmount(totals.debit) || "0"} />
            <Stat label="Total credit" value={formatBankAmount(totals.credit) || "0"} />
          </div>

          {categorizePayments && categorySummary.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {categorySummary.slice(0, 8).map(([name, count]) => (
                <div
                  key={name}
                  className="rounded-lg border border-border bg-secondary/40 px-3 py-1.5 text-xs"
                >
                  <span className="font-medium">{name}</span>
                  <span className="ml-2 text-muted-foreground">{count}</span>
                </div>
              ))}
            </div>
          ) : null}

          <div className="rounded-xl border border-border bg-card shadow-sm">
            <table
              className={cn(
                "w-full table-fixed border-separate border-spacing-0 text-sm",
                categorizePayments ? "min-w-[1100px]" : "min-w-[900px]",
              )}
            >
              <colgroup>
                <col className="w-[14%]" />
                <col className="w-[12%]" />
                <col className="w-[7%]" />
                <col className={categorizePayments ? "w-[14%]" : "w-[22%]"} />
                <col className="w-[10%]" />
                <col className="w-[8%]" />
                <col className="w-[8%]" />
                {categorizePayments ? (
                  <>
                    <col className="w-[7%]" />
                    <col className="w-[10%]" />
                    <col className="w-[10%]" />
                  </>
                ) : null}
                <col className="w-[40px]" />
              </colgroup>
              <thead>
                <tr className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="border-b border-border px-2 py-2.5 font-medium">ERP bank ledger</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium">Party name</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium">Value date</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium">Narration</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium">Ref / cheque</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium text-right">Debit</th>
                  <th className="border-b border-l border-border px-2 py-2.5 font-medium text-right">Credit</th>
                  {categorizePayments ? (
                    <>
                      <th className="border-b border-l-2 border-l-primary/30 border-border bg-primary/5 px-2 py-2.5 font-medium">
                        Type
                      </th>
                      <th className="border-b border-l border-border bg-primary/5 px-2 py-2.5 font-medium">
                        Main category
                      </th>
                      <th className="border-b border-l border-border bg-primary/5 px-2 py-2.5 font-medium">
                        Sub-category
                      </th>
                    </>
                  ) : null}
                  <th className="border-b border-l border-border px-1 py-2.5 font-medium" />
                </tr>
              </thead>
              <tbody>
                {rows.map((row, index) => {
                  const isLast = index === rows.length - 1;
                  const rowBorder = isLast ? "" : "border-b border-border/70";
                  return (
                    <tr key={row.id} className="align-middle hover:bg-secondary/20">
                      <td className={cn("px-1.5 py-1.5", rowBorder)}>
                        <Input
                          value={row.erpBankLedgerName}
                          onChange={(e) =>
                            updateRow(row.id, { erpBankLedgerName: e.target.value })
                          }
                          className="h-8 w-full min-w-0 text-xs"
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          value={row.partyName}
                          onChange={(e) => updateRow(row.id, { partyName: e.target.value })}
                          className="h-8 w-full min-w-0 text-xs"
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          value={row.valueDate}
                          onChange={(e) => updateRow(row.id, { valueDate: e.target.value })}
                          placeholder="dd/MM/yyyy"
                          className="h-8 w-full min-w-0 text-xs"
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          value={row.narration}
                          onChange={(e) => updateRow(row.id, { narration: e.target.value })}
                          className="h-8 w-full min-w-0 text-xs"
                          title={row.narration}
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          value={row.refChequeNo}
                          onChange={(e) => updateRow(row.id, { refChequeNo: e.target.value })}
                          className="h-8 w-full min-w-0 text-xs"
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          type="number"
                          inputMode="decimal"
                          value={row.debit ?? ""}
                          onChange={(e) =>
                            updateRow(row.id, {
                              debit: e.target.value === "" ? null : Number(e.target.value),
                            })
                          }
                          className="h-8 w-full min-w-0 text-right text-xs"
                        />
                      </td>
                      <td className={cn("border-l border-border/60 px-1.5 py-1.5", rowBorder)}>
                        <Input
                          type="number"
                          inputMode="decimal"
                          value={row.credit ?? ""}
                          onChange={(e) =>
                            updateRow(row.id, {
                              credit: e.target.value === "" ? null : Number(e.target.value),
                            })
                          }
                          className="h-8 w-full min-w-0 text-right text-xs"
                        />
                      </td>
                      {categorizePayments ? (
                        <>
                          <td
                            className={cn(
                              "border-l-2 border-l-primary/25 border-border/60 bg-primary/[0.03] px-1.5 py-1.5",
                              rowBorder,
                            )}
                          >
                            <Input
                              value={row.transactionType ?? ""}
                              onChange={(e) =>
                                updateRow(row.id, { transactionType: e.target.value })
                              }
                              className="h-8 w-full min-w-0 text-xs"
                            />
                          </td>
                          <td
                            className={cn(
                              "border-l border-border/60 bg-primary/[0.03] px-1.5 py-1.5",
                              rowBorder,
                            )}
                          >
                            <Input
                              value={row.mainCategory ?? ""}
                              onChange={(e) =>
                                updateRow(row.id, { mainCategory: e.target.value })
                              }
                              className="h-8 w-full min-w-0 text-xs"
                            />
                          </td>
                          <td
                            className={cn(
                              "border-l border-border/60 bg-primary/[0.03] px-1.5 py-1.5",
                              rowBorder,
                            )}
                          >
                            <Input
                              value={row.subCategory ?? ""}
                              onChange={(e) =>
                                updateRow(row.id, { subCategory: e.target.value })
                              }
                              className="h-8 w-full min-w-0 text-xs"
                            />
                          </td>
                        </>
                      ) : null}
                      <td className={cn("border-l border-border/60 px-0.5 py-1.5", rowBorder)}>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-muted-foreground hover:text-destructive"
                          onClick={() => removeRow(row.id)}
                          title="Remove entry"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="flex flex-wrap gap-2 pb-6">
            <Button type="button" variant="outline" onClick={addRow}>
              <Plus className="h-4 w-4" />
              Add entry
            </Button>
            {categorizePayments ? (
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setRows((prev) => categorizeBankRows(prev));
                  toast.success("Categories re-applied from narration rules");
                }}
              >
                Re-apply categories
              </Button>
            ) : null}
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                setRows([]);
                setFileName(null);
              }}
            >
              Clear all
            </Button>
          </div>
        </>
      ) : (
        <div className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-10 text-center text-sm text-muted-foreground">
          Upload a bank statement PDF (e.g. <span className="font-medium text-foreground">25_BOB_456.pdf</span>)
          to convert it into the import format, or start blank.
        </div>
      )}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border bg-card px-4 py-3 shadow-sm">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
    </div>
  );
}
