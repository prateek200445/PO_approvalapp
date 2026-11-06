import { useMemo, useRef, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  AlertCircle,
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Loader2,
  Upload,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth-context";
import { getTdsGroups } from "@/lib/tds-api";
import {
  commitImportJv,
  formatJvDate,
  getImportJvUnits,
  importJvTemplateUrl,
  jvMoney,
  previewImportJv,
  type ImportJvPreview,
  type ImportJvVoucher,
} from "@/lib/import-jv-api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { SearchableSelect } from "@/components/SearchableSelect";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";

const SELECT_CLASS =
  "h-10 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

function money(n: number) {
  return jvMoney.format(n || 0);
}

export function ImportJvScreen() {
  const { user } = useAuth();
  const fileRef = useRef<HTMLInputElement>(null);
  const [group, setGroup] = useState("");
  const [company, setCompany] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const [posting, setPosting] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [preview, setPreview] = useState<ImportJvPreview | null>(null);
  const [selectedRow, setSelectedRow] = useState<number | null>(null);

  const groupsQuery = useQuery({
    queryKey: ["tds-groups"],
    queryFn: getTdsGroups,
    staleTime: 10 * 60_000,
  });

  const unitsQuery = useQuery({
    queryKey: ["import-jv-units", group],
    queryFn: () => getImportJvUnits(group),
    enabled: group.length > 0,
    placeholderData: keepPreviousData,
  });

  const unitOptions = useMemo(
    () => (unitsQuery.data ?? []).map((unit) => ({ value: unit.name, label: unit.name })),
    [unitsQuery.data],
  );

  const selected = preview?.vouchers.find((row) => row.row === selectedRow) ?? preview?.vouchers[0] ?? null;

  async function runPreview(nextFile: File) {
    if (!group) {
      toast.error("Choose a group first.");
      return;
    }
    if (!company) {
      toast.error("Choose the company the journals belong to.");
      return;
    }
    setPreviewing(true);
    try {
      const result = await previewImportJv({ file: nextFile, group, company });
      setPreview(result);
      setSelectedRow(result.vouchers[0]?.row ?? null);
      if (result.errorCount > 0) toast.error(`${result.errorCount} row(s) need a fix before posting.`);
      else toast.success(`Ready: ${result.okCount} journal${result.okCount === 1 ? "" : "s"}, ${result.tdsCount} with TDS.`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not read the file.");
      setPreview(null);
    } finally {
      setPreviewing(false);
    }
  }

  function onFile(next: File | null) {
    if (!next) return;
    setFile(next);
    void runPreview(next);
  }

  async function onPost() {
    if (!file || !preview || !user?.username) return;
    setPosting(true);
    try {
      const result = await commitImportJv({
        file,
        group,
        company,
        username: user.username,
      });
      toast.success(result.message);
      setConfirmOpen(false);
      setPreview(null);
      setFile(null);
      setSelectedRow(null);
      if (fileRef.current) fileRef.current.value = "";
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Posting failed. Nothing was saved.");
    } finally {
      setPosting(false);
    }
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 rounded-2xl border border-border/80 bg-card p-4 shadow-soft lg:grid-cols-3">
        <div className="space-y-1.5">
          <Label htmlFor="import-jv-group">Group</Label>
          <select
            id="import-jv-group"
            className={SELECT_CLASS}
            value={group}
            onChange={(event) => {
              setGroup(event.target.value);
              setCompany("");
              setPreview(null);
              setFile(null);
            }}
            disabled={groupsQuery.isLoading}
          >
            <option value="">Select a group</option>
            {(groupsQuery.data ?? []).map((item) => (
              <option key={item.name} value={item.name}>
                {item.name} ({item.units})
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label>Company</Label>
          <SearchableSelect
            options={unitOptions}
            value={company}
            onChange={(next) => {
              setCompany(next);
              setPreview(null);
            }}
            placeholder={group ? "Select the unit" : "Choose a group first"}
            disabled={!group || unitsQuery.isFetching}
            searchPlaceholder="Search company…"
          />
        </div>
        <div className="flex items-end">
          <Button variant="outline" className="w-full" asChild>
            <a href={importJvTemplateUrl()} download="ImportJV-TDS.xlsx">
              <Download className="h-4 w-4" />
              Download template
            </a>
          </Button>
        </div>
      </div>

      <AccountantGuide />

      <div
        className={cn(
          "rounded-2xl border border-dashed px-4 py-8 text-center shadow-soft transition",
          dragOver ? "border-primary bg-primary/5" : "border-border/80 bg-card",
          !group || !company ? "opacity-60" : "",
        )}
        onDragOver={(event) => {
          event.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragOver(false);
          onFile(event.dataTransfer.files[0] ?? null);
        }}
      >
        <input
          ref={fileRef}
          type="file"
          accept=".xlsx,.xlsm,.csv"
          className="hidden"
          onChange={(event) => onFile(event.target.files?.[0] ?? null)}
        />
        {previewing ? (
          <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin text-primary" />
            Building journals from the sheet…
          </div>
        ) : (
          <>
            <FileSpreadsheet className="mx-auto h-8 w-8 text-primary" />
            <p className="mt-3 text-sm font-medium">Drop the Import JV Excel here</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Same layout as ERP Import JV. Add TDS -194… amount columns for deduction; the party debit is created for you.
            </p>
            <Button
              className="mt-4"
              variant="secondary"
              disabled={!group || !company}
              onClick={() => fileRef.current?.click()}
            >
              <Upload className="h-4 w-4" />
              {file ? file.name : "Choose file"}
            </Button>
          </>
        )}
      </div>

      {preview ? (
        <>
          <div className="grid gap-3 sm:grid-cols-4">
            <Stat label="Journals" value={String(preview.vouchers.length)} />
            <Stat label="With TDS" value={String(preview.tdsCount)} />
            <Stat label="Ready" value={String(preview.okCount)} ok />
            <Stat label="Errors" value={String(preview.errorCount)} bad={preview.errorCount > 0} />
          </div>

          {preview.tdsColumns.length > 0 ? (
            <p className="text-xs text-muted-foreground">
              TDS credit columns: {preview.tdsColumns.join(", ")}
            </p>
          ) : (
            <p className="text-xs text-amber-700 dark:text-amber-400">
              No column starts with TDS. Journals will post without TDS lines — add a header such as TDS -194C (Contractor).
            </p>
          )}

          <div className="grid gap-4 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
            <div className="overflow-hidden rounded-2xl border border-border/80 bg-card shadow-soft">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[720px] border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-border bg-muted/40">
                      <th className="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Row</th>
                      <th className="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Party</th>
                      <th className="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Invoice</th>
                      <th className="px-3 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Value</th>
                      <th className="px-3 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">TDS</th>
                      <th className="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.vouchers.map((row) => {
                      const active = selected?.row === row.row;
                      const bad = row.errors.length > 0;
                      return (
                        <tr
                          key={row.row}
                          className={cn(
                            "cursor-pointer border-b border-border/60 last:border-0",
                            active ? "bg-primary/8" : "hover:bg-muted/40",
                            bad ? "bg-destructive/5" : "",
                          )}
                          onClick={() => setSelectedRow(row.row)}
                        >
                          <td className="px-3 py-2 tabular-nums text-muted-foreground">{row.row}</td>
                          <td className="max-w-[220px] truncate px-3 py-2" title={row.creditLedger}>
                            {row.creditLedger}
                          </td>
                          <td className="px-3 py-2">
                            <div className="font-medium">{row.invoiceNo || "—"}</div>
                            <div className="text-[11px] text-muted-foreground">{formatJvDate(row.invoiceDate)}</div>
                          </td>
                          <td className="px-3 py-2 text-right tabular-nums">{money(row.invoiceValue)}</td>
                          <td className="px-3 py-2 text-right tabular-nums">
                            {row.tdsTotal > 0 ? money(row.tdsTotal) : "—"}
                          </td>
                          <td className="px-3 py-2">
                            {bad ? (
                              <span className="inline-flex items-center gap-1 text-destructive">
                                <AlertCircle className="h-3.5 w-3.5" />
                                {row.errors.length} error
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 text-emerald-700 dark:text-emerald-400">
                                <CheckCircle2 className="h-3.5 w-3.5" />
                                Ready
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>

            {selected ? <JournalPreview voucher={selected} /> : null}
          </div>

          <div className="flex flex-wrap items-center justify-end gap-3">
            <Button
              disabled={!preview.canPost || posting}
              onClick={() => setConfirmOpen(true)}
            >
              Post {preview.okCount} journal{preview.okCount === 1 ? "" : "s"} to ERP
            </Button>
          </div>
        </>
      ) : null}

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Post journals to {company}?</DialogTitle>
            <DialogDescription>
              This writes Journal and JournalItem rows in ERP, including TDS credits and the matching party debit.
              There is no undo from this screen.
            </DialogDescription>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {preview?.okCount} voucher{preview?.okCount === 1 ? "" : "s"}
            {preview?.tdsCount ? `, ${preview.tdsCount} with TDS` : ""}. Posted as {user?.username}.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmOpen(false)} disabled={posting}>
              Cancel
            </Button>
            <Button onClick={() => void onPost()} disabled={posting}>
              {posting ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              Post now
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function Stat({
  label,
  value,
  ok,
  bad,
}: {
  label: string;
  value: string;
  ok?: boolean;
  bad?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-border/80 bg-card px-4 py-3 shadow-soft">
      <div className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{label}</div>
      <div
        className={cn(
          "mt-1 text-xl font-semibold tabular-nums",
          ok && "text-emerald-700 dark:text-emerald-400",
          bad && "text-destructive",
        )}
      >
        {value}
      </div>
    </div>
  );
}

function JournalPreview({ voucher }: { voucher: ImportJvVoucher }) {
  const debit = voucher.lines.reduce((sum, line) => sum + line.debit, 0);
  const credit = voucher.lines.reduce((sum, line) => sum + line.credit, 0);
  return (
    <div className="rounded-2xl border border-border/80 bg-card p-4 shadow-soft">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold">Journal preview</h2>
          <p className="mt-0.5 text-xs text-muted-foreground">
            Entry {formatJvDate(voucher.entryDate)}
            {voucher.requestedJournalNo ? ` · requested JV ${voucher.requestedJournalNo}` : ""}
          </p>
        </div>
        {voucher.tdsTotal > 0 ? (
          <span className="rounded-full bg-primary/10 px-2.5 py-1 text-[11px] font-medium text-primary">
            TDS {money(voucher.tdsTotal)}
          </span>
        ) : null}
      </div>

      {voucher.errors.length > 0 ? (
        <ul className="mt-3 space-y-1 text-xs text-destructive">
          {voucher.errors.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      ) : null}
      {voucher.warnings.length > 0 ? (
        <ul className="mt-3 space-y-1 text-xs text-amber-700 dark:text-amber-400">
          {voucher.warnings.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      ) : null}

      <table className="mt-3 w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-border text-[11px] uppercase tracking-wide text-muted-foreground">
            <th className="py-1.5 text-left font-semibold">Ledger</th>
            <th className="py-1.5 text-right font-semibold">Debit</th>
            <th className="py-1.5 text-right font-semibold">Credit</th>
          </tr>
        </thead>
        <tbody>
          {voucher.lines.map((line, index) => (
            <tr
              key={`${line.ledger}-${index}`}
              className={cn(
                "border-b border-border/50",
                line.isTds || line.isPartyTds ? "bg-primary/5" : "",
              )}
            >
              <td className="py-1.5">
                <div className="font-medium">{line.ledger}</div>
                <div className="text-[11px] text-muted-foreground">
                  {line.type}
                  {line.isPartyTds ? " · party TDS" : ""}
                  {line.isTds ? " · TDS ledger" : ""}
                  {line.billNo ? ` · ${line.billNo}` : ""}
                </div>
              </td>
              <td className="py-1.5 text-right tabular-nums">{line.debit ? money(line.debit) : ""}</td>
              <td className="py-1.5 text-right tabular-nums">{line.credit ? money(line.credit) : ""}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="font-semibold">
            <td className="pt-2">Total</td>
            <td className="pt-2 text-right tabular-nums">{money(debit)}</td>
            <td className="pt-2 text-right tabular-nums">{money(credit)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function AccountantGuide() {
  const sections = [
    ["94A", "TDS -194A (Interest)", "Interest / factoring"],
    ["94C", "TDS -194C (Contractor)", "Freight, C&F, contractor"],
    ["94H", "TDS -194H (Commission)", "Commission"],
    ["94I", "TDS -194I (Rent)", "Rent"],
    ["94J", "TDS -194J (Professional)", "Professional / consultancy"],
    ["94Q", "TDS -194Q (Goods)", "Purchase of goods"],
  ];
  return (
    <div className="space-y-3 rounded-2xl border border-border/80 bg-card p-4 text-sm shadow-soft">
      <h2 className="text-sm font-semibold">Tell the accountant this</h2>
      <p className="text-muted-foreground">
        All six TDS sections are columns. Put the amount only in the one that applies; leave the rest blank.
        Expense and GST columns are yellow debits. Green TDS columns are credits. Do not add a party debit for TDS —
        the import does that.
      </p>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] border-collapse text-xs">
          <thead>
            <tr className="border-b border-border text-[11px] uppercase tracking-wide text-muted-foreground">
              <th className="py-1.5 text-left font-semibold">Section</th>
              <th className="py-1.5 text-left font-semibold">Excel column (must match ERP ledger)</th>
              <th className="py-1.5 text-left font-semibold">When to fill</th>
            </tr>
          </thead>
          <tbody>
            {sections.map(([section, ledger, when]) => (
              <tr key={section} className="border-b border-border/50">
                <td className="py-1.5 font-medium">{section}</td>
                <td className="py-1.5">{ledger}</td>
                <td className="py-1.5 text-muted-foreground">{when}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-xs text-muted-foreground">
        Example: bill 4,720 (expense 4,000 + IGST 720) with 194C ₹80. Excel has 80 only in TDS -194C.
        Posted journal: Dr expense 4,000, Dr IGST 720, Cr party 4,720, Dr party 80, Cr TDS -194C 80.
        Party bill becomes 4,640. Check it in the preview on the right before posting. Delete the demo JV from ERP if you posted a test.
      </p>
    </div>
  );
}
