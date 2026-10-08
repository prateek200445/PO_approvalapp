import { Fragment, useMemo, useRef, useState, type ReactNode } from "react";
import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Download,
  FileText,
  Inbox,
  Info,
  Landmark,
  Loader2,
  RefreshCw,
  Scale,
  Trash2,
  Upload,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import {
  deleteIcegateUpload,
  downloadGstr1Excel,
  getGstr1Report,
  getIcegateStatus,
  uploadIcegateFile,
  type GstDocumentSeries,
  type Gstr1IcegateExtra,
  type Gstr1IcegateSummary,
  type Gstr1B2bRow,
  type Gstr1B2clRow,
  type Gstr1B2csRow,
  type Gstr1Exception,
  type Gstr1ExpRow,
  type Gstr1HsnRow,
  type Gstr1LedgerReconRow,
  type Gstr1NilRow,
  type Gstr1NoteRow,
  type Gstr1Report,
  type Gstr1SalesRegisterRow,
  type Gstr1TrialBalanceRow,
} from "@/lib/gst-api";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

const PAGE_SIZE = 200;

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const count = new Intl.NumberFormat("en-IN");
const qty = new Intl.NumberFormat("en-IN", { maximumFractionDigits: 3 });

function fmtMoney(v: number | null | undefined): string {
  return money.format(v ?? 0);
}

function fmtDate(v: string | null | undefined): string {
  if (!v) return "";
  const d = new Date(v.slice(0, 10) + "T00:00:00");
  if (Number.isNaN(d.getTime())) return v;
  return d.toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
}

function monthRange(month: string): { from: string; to: string } {
  const [y, m] = month.split("-").map(Number);
  const last = new Date(y, m, 0).getDate();
  const mm = String(m).padStart(2, "0");
  return { from: `${y}-${mm}-01`, to: `${y}-${mm}-${String(last).padStart(2, "0")}` };
}

function formatMonth(month: string): string {
  const [y, m] = month.split("-").map(Number);
  if (!y || !m) return month;
  return new Date(y, m - 1, 1).toLocaleDateString("en-GB", { month: "long", year: "numeric" });
}

// ---------------------------------------------------------------- generic table

interface Column<T> {
  header: string;
  /** Numeric columns are right-aligned and summed in the totals row when `sum` is set. */
  value?: (r: T) => number;
  cell?: (r: T) => ReactNode;
  sum?: boolean;
  format?: "money" | "count" | "qty" | "rate";
  mono?: boolean;
  /** Mobile card placement. */
  role?: "title" | "subtitle" | "hide";
  className?: string;
}

function formatNumber(v: number, format: Column<unknown>["format"]): string {
  if (format === "count") return count.format(v);
  if (format === "qty") return qty.format(v);
  if (format === "rate") return `${qty.format(v)}%`;
  return fmtMoney(v);
}

function renderCell<T>(col: Column<T>, row: T): ReactNode {
  if (col.cell) return col.cell(row);
  if (col.value) return formatNumber(col.value(row), col.format);
  return null;
}

function DataTable<T>({
  rows,
  columns,
  rowKey,
  rowClassName,
  empty = "Nothing to report in this section.",
}: {
  rows: T[];
  columns: Column<T>[];
  rowKey: (r: T, i: number) => string;
  rowClassName?: (r: T) => string | false | undefined;
  empty?: string;
}) {
  const [limit, setLimit] = useState(PAGE_SIZE);
  const visible = rows.slice(0, limit);
  const hasTotals = columns.some((c) => c.sum);
  const totals = useMemo(
    () => columns.map((c) => (c.sum && c.value ? rows.reduce((s, r) => s + (c.value?.(r) ?? 0), 0) : null)),
    [columns, rows],
  );

  if (rows.length === 0) {
    return (
      <div className="flex flex-col items-center px-5 py-10 text-center">
        <Inbox className="mb-2 h-6 w-6 text-primary" />
        <p className="text-sm text-muted-foreground">{empty}</p>
      </div>
    );
  }

  const title = columns.find((c) => c.role === "title");
  const subtitle = columns.find((c) => c.role === "subtitle");
  const cardFields = columns.filter((c) => c !== title && c !== subtitle && c.role !== "hide");
  const firstTextIndex = columns.findIndex((c) => !c.value);

  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="bg-primary/10 text-left text-xs text-primary">
              {columns.map((c) => (
                <th
                  key={c.header}
                  className={cn("whitespace-nowrap px-3 py-2 font-semibold", c.value && "text-right", c.className)}
                >
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {visible.map((r, i) => (
              <tr
                key={rowKey(r, i)}
                className={cn(
                  "border-b border-border/70",
                  i % 2 === 0 ? "bg-card" : "bg-muted/30",
                  rowClassName?.(r),
                )}
              >
                {columns.map((c) => (
                  <td
                    key={c.header}
                    className={cn(
                      "px-3 py-1.5 align-top",
                      c.value && "whitespace-nowrap text-right tabular-nums",
                      c.mono && "font-mono text-xs",
                      c.className,
                    )}
                  >
                    {renderCell(c, r)}
                  </td>
                ))}
              </tr>
            ))}
            {hasTotals ? (
              <tr className="bg-primary/5 font-semibold">
                {columns.map((c, i) => (
                  <td
                    key={c.header}
                    className={cn("px-3 py-2", c.value && "whitespace-nowrap text-right tabular-nums")}
                  >
                    {totals[i] !== null
                      ? formatNumber(totals[i] ?? 0, c.format)
                      : i === Math.max(firstTextIndex, 0)
                        ? `Total (${count.format(rows.length)})`
                        : null}
                  </td>
                ))}
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>

      <div className="divide-y divide-border md:hidden">
        {hasTotals ? (
          <div className="bg-primary/5 px-4 py-3">
            <p className="text-xs font-semibold text-primary">Total ({count.format(rows.length)})</p>
            <div className="mt-1.5 grid grid-cols-2 gap-x-3 gap-y-1 text-xs">
              {columns.map((c, i) =>
                totals[i] !== null ? (
                  <div key={c.header} className="flex justify-between gap-2">
                    <span className="text-muted-foreground">{c.header}</span>
                    <span className="font-semibold tabular-nums">{formatNumber(totals[i] ?? 0, c.format)}</span>
                  </div>
                ) : null,
              )}
            </div>
          </div>
        ) : null}
        {visible.map((r, i) => (
          <div key={rowKey(r, i)} className={cn("px-4 py-3", rowClassName?.(r))}>
            {title ? <p className="break-words text-sm font-semibold">{renderCell(title, r)}</p> : null}
            {subtitle ? <p className="break-words text-xs text-muted-foreground">{renderCell(subtitle, r)}</p> : null}
            <div className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-xs">
              {cardFields.map((c) => (
                <div key={c.header} className="min-w-0">
                  <span className="text-muted-foreground">{c.header}: </span>
                  <span className={cn("break-words", c.value && "tabular-nums", c.mono && "font-mono")}>
                    {renderCell(c, r)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {rows.length > limit ? (
        <div className="flex items-center justify-between gap-2 border-t border-border px-4 py-2.5 text-xs text-muted-foreground">
          <span>
            Showing {count.format(limit)} of {count.format(rows.length)} rows (totals cover all rows; Excel has
            everything)
          </span>
          <Button type="button" variant="outline" size="sm" className="h-8" onClick={() => setLimit((n) => n + PAGE_SIZE * 5)}>
            Show more
          </Button>
        </div>
      ) : null}
    </>
  );
}

// ---------------------------------------------------------------- columns

function ApprovedBadge({ approved }: { approved: boolean }) {
  return approved ? null : (
    <span className="ml-1 rounded bg-amber-100 px-1 py-px text-[10px] font-semibold text-amber-800">UNAPPROVED</span>
  );
}

function InvoiceCell({ no, erp, approved }: { no: string; erp: string; approved: boolean }) {
  return (
    <span>
      <span className="font-mono text-xs">{no}</span>
      <ApprovedBadge approved={approved} />
      {erp && erp !== no ? <span className="block text-[11px] text-muted-foreground">ERP: {erp}</span> : null}
    </span>
  );
}

const b2bColumns: Column<Gstr1B2bRow>[] = [
  { header: "Receiver", role: "title", cell: (r) => r.receiverName },
  { header: "GSTIN", mono: true, role: "subtitle", cell: (r) => r.gstin },
  { header: "Voucher Type", className: "whitespace-nowrap", cell: (r) => r.voucherType },
  { header: "Sales Ledger", className: "min-w-[10rem]", cell: (r) => r.salesLedger },
  { header: "Invoice", cell: (r) => <InvoiceCell no={r.invoiceNo} erp={r.erpInvoiceNo} approved={r.approved} /> },
  { header: "Date", cell: (r) => fmtDate(r.invoiceDate), className: "whitespace-nowrap" },
  { header: "POS", cell: (r) => r.placeOfSupply, className: "whitespace-nowrap" },
  { header: "Type", cell: (r) => r.invoiceType },
  { header: "Invoice value", value: (r) => r.invoiceValue },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "CGST", value: (r) => r.cgst, sum: true },
  { header: "SGST", value: (r) => r.sgst, sum: true },
  { header: "TCS", value: (r) => r.tcs, sum: true },
  { header: "Other Charges", value: (r) => r.otherCharges, sum: true },
];

const b2clColumns: Column<Gstr1B2clRow>[] = [
  { header: "Receiver", role: "title", cell: (r) => r.receiverName },
  { header: "Invoice", role: "subtitle", cell: (r) => <InvoiceCell no={r.invoiceNo} erp={r.erpInvoiceNo} approved={r.approved} /> },
  { header: "Date", cell: (r) => fmtDate(r.invoiceDate), className: "whitespace-nowrap" },
  { header: "POS", cell: (r) => r.placeOfSupply },
  { header: "Invoice value", value: (r) => r.invoiceValue },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
];

const b2csColumns: Column<Gstr1B2csRow>[] = [
  { header: "Place of supply", role: "title", cell: (r) => r.placeOfSupply },
  { header: "Type", role: "subtitle", cell: (r) => r.type },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Documents", value: (r) => r.documents, format: "count" },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "CGST", value: (r) => r.cgst, sum: true },
  { header: "SGST", value: (r) => r.sgst, sum: true },
];

const ICEGATE_STATUS_STYLE: Record<string, string> = {
  Matched: "bg-emerald-100 text-emerald-800",
  Differences: "bg-amber-100 text-amber-800",
  "Not in ICEGATE": "bg-red-100 text-red-800",
  "No ICEGATE data": "bg-muted text-muted-foreground",
};

function IcegateStatusCell({ row }: { row: Gstr1ExpRow }) {
  if (!row.icegateStatus) return null;
  return (
    <span className="block min-w-[9rem]">
      <span
        className={cn(
          "whitespace-nowrap rounded px-1.5 py-px text-[10px] font-semibold",
          ICEGATE_STATUS_STYLE[row.icegateStatus] ?? "bg-muted",
        )}
      >
        {row.icegateStatus}
      </span>
      {row.icegateNote ? <span className="mt-0.5 block text-[11px] text-amber-800">{row.icegateNote}</span> : null}
    </span>
  );
}

function hasIcegateMatch(r: Gstr1ExpRow): boolean {
  return r.icegateStatus === "Matched" || r.icegateStatus === "Differences";
}

function optionalMoney(v: number | null | undefined, missing = "—"): ReactNode {
  return v === null || v === undefined ? <span className="text-muted-foreground">{missing}</span> : fmtMoney(v);
}

const expColumns: Column<Gstr1ExpRow>[] = [
  { header: "Invoice", role: "title", cell: (r) => <InvoiceCell no={r.invoiceNo} erp={r.erpInvoiceNo} approved={r.approved} /> },
  { header: "Buyer", role: "subtitle", cell: (r) => r.receiverName },
  { header: "Date", cell: (r) => fmtDate(r.invoiceDate), className: "whitespace-nowrap" },
  { header: "Type", cell: (r) => r.exportType },
  { header: "Port", mono: true, cell: (r) => r.portCode || <span className="text-amber-700">—</span> },
  {
    header: "Shipping Bill No",
    cell: (r) =>
      r.shippingBillNo ? (
        <span className="whitespace-nowrap">
          {r.shippingBillNo}
          {r.filledFromIcegate ? (
            <span className="ml-1 rounded bg-sky-100 px-1 py-px text-[10px] font-semibold text-sky-800">ICEGATE</span>
          ) : null}
        </span>
      ) : (
        <span className="text-amber-700">—</span>
      ),
  },
  {
    header: "Shipping Bill Date",
    className: "whitespace-nowrap",
    cell: (r) => (r.shippingBillDate ? fmtDate(r.shippingBillDate) : <span className="text-amber-700">—</span>),
  },
  { header: "Invoice value", value: (r) => r.invoiceValue },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "FOB (ICEGATE)", value: (r) => r.fob ?? 0, cell: (r) => optionalMoney(r.fob), sum: true },
  {
    header: "IGST paid (ICEGATE)",
    value: (r) => r.icegateIgst ?? 0,
    cell: (r) => optionalMoney(r.icegateIgst, r.icegateInvoiceNo ? "not paid" : "—"),
    sum: true,
  },
  {
    header: "EGM No",
    className: "whitespace-nowrap",
    cell: (r) => r.egmNo || (hasIcegateMatch(r) ? <span className="text-muted-foreground">pending</span> : null),
  },
  {
    header: "EGM Date",
    className: "whitespace-nowrap",
    cell: (r) =>
      r.egmDate ? fmtDate(r.egmDate) : hasIcegateMatch(r) ? <span className="text-muted-foreground">pending</span> : null,
  },
  { header: "ICEGATE Invoice No", mono: true, className: "whitespace-nowrap", cell: (r) => r.icegateInvoiceNo },
  {
    header: "ICEGATE Invoice Date",
    className: "whitespace-nowrap",
    cell: (r) => (r.icegateInvoiceDate ? fmtDate(r.icegateInvoiceDate) : null),
  },
  { header: "ICEGATE match", cell: (r) => <IcegateStatusCell row={r} /> },
];

const icegateExtraColumns: Column<Gstr1IcegateExtra>[] = [
  { header: "Shipping Bill No", role: "title", mono: true, cell: (r) => r.shippingBillNo },
  { header: "Company", role: "subtitle", cell: (r) => r.company || `ICEGATE: ${r.companyLabel}` },
  { header: "Shipping Bill Date", className: "whitespace-nowrap", cell: (r) => fmtDate(r.shippingBillDate) },
  { header: "Port", mono: true, cell: (r) => r.portCode },
  { header: "Invoice No", mono: true, className: "whitespace-nowrap", cell: (r) => r.invoiceNo || "—" },
  { header: "Invoice Date", className: "whitespace-nowrap", cell: (r) => fmtDate(r.invoiceDate) },
  { header: "EGM No", className: "whitespace-nowrap", cell: (r) => r.egmNo },
  { header: "EGM Date", className: "whitespace-nowrap", cell: (r) => fmtDate(r.egmDate) },
  { header: "FOB", value: (r) => r.fob ?? 0, cell: (r) => optionalMoney(r.fob), sum: true },
  { header: "IGST paid", value: (r) => r.igstPaid ?? 0, cell: (r) => optionalMoney(r.igstPaid, "not paid"), sum: true },
];

function IcegatePanel({ summary }: { summary: Gstr1IcegateSummary | null }) {
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [showUploads, setShowUploads] = useState(false);
  const statusQuery = useQuery({
    queryKey: ["icegate-status"],
    queryFn: getIcegateStatus,
    enabled: showUploads,
    staleTime: 60_000,
  });

  async function refreshAll() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["gstr1-report"] }),
      queryClient.invalidateQueries({ queryKey: ["icegate-status"] }),
    ]);
  }

  async function onFile(file: File | undefined) {
    if (!file || busy) return;
    setBusy(true);
    try {
      const { upload } = await uploadIcegateFile(file);
      toast.success(
        `ICEGATE file loaded — ${upload.shippingBills} shipping bills` +
          (upload.firstSbDate ? ` (${fmtDate(upload.firstSbDate)} to ${fmtDate(upload.lastSbDate)})` : "") +
          (upload.replaced > 0 ? `, ${upload.replaced} updated` : ""),
      );
      await refreshAll();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Upload failed");
    } finally {
      setBusy(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  async function remove(id: string) {
    try {
      await deleteIcegateUpload(id);
      toast.success("ICEGATE upload removed");
      await refreshAll();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Remove failed");
    }
  }

  const chips = summary?.hasData
    ? [
        { label: "Matched", n: summary.matched, cls: "bg-emerald-100 text-emerald-800" },
        { label: "With differences", n: summary.withDifferences, cls: "bg-amber-100 text-amber-800" },
        { label: "Not in ICEGATE", n: summary.notInIcegate, cls: "bg-red-100 text-red-800" },
        { label: "ICEGATE only", n: summary.notInErp.length, cls: "bg-red-100 text-red-800" },
        { label: "No ICEGATE data", n: summary.noData, cls: "bg-muted text-muted-foreground" },
      ]
    : [];

  return (
    <div className="space-y-2 border-b border-border bg-sky-50/40 px-4 py-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0 text-xs">
          <p className="font-semibold text-foreground">ICEGATE shipping bill check</p>
          <p className="text-muted-foreground">
            {summary?.hasData
              ? `${summary.shippingBillsStored} shipping bills on file · last upload ${summary.lastFileName ?? ""}${
                  summary.lastUploadUtc ? ` on ${fmtDate(summary.lastUploadUtc)}` : ""
                }`
              : "Upload the ICEGATE export workbook to compare FOB, EGM and IGST paid with the ERP export invoices."}
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <input
            ref={inputRef}
            type="file"
            accept=".xlsx"
            className="hidden"
            onChange={(e) => void onFile(e.target.files?.[0])}
          />
          <Button
            type="button"
            size="sm"
            className="h-8 gap-1.5"
            disabled={busy}
            onClick={() => inputRef.current?.click()}
          >
            {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
            Upload ICEGATE file
          </Button>
          {summary?.hasData ? (
            <Button type="button" variant="outline" size="sm" className="h-8" onClick={() => setShowUploads((v) => !v)}>
              {showUploads ? "Hide uploads" : "Uploads"}
            </Button>
          ) : null}
        </div>
      </div>
      {chips.length > 0 ? (
        <div className="flex flex-wrap gap-1.5">
          {chips.map((c) => (
            <span key={c.label} className={cn("rounded-full px-2 py-0.5 text-[11px] font-medium", c.cls)}>
              {c.label}: {count.format(c.n)}
            </span>
          ))}
        </div>
      ) : null}
      {showUploads ? (
        <ul className="divide-y divide-border rounded-lg border border-border bg-card text-xs">
          {statusQuery.isLoading ? <li className="px-3 py-2 text-muted-foreground">Loading…</li> : null}
          {(statusQuery.data?.uploads ?? []).map((u) => (
            <li key={u.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
              <span className="min-w-0">
                <span className="font-medium">{u.fileName}</span>
                <span className="text-muted-foreground">
                  {" "}
                  · {u.shippingBills} shipping bills · SB {fmtDate(u.firstSbDate)} – {fmtDate(u.lastSbDate)} ·{" "}
                  {u.companyLabels.join(", ")} · uploaded {fmtDate(u.uploadedAtUtc)}
                </span>
              </span>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-7 gap-1 text-destructive"
                onClick={() => void remove(u.id)}
              >
                <Trash2 className="h-3.5 w-3.5" />
                Remove
              </Button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

function noteColumns(registered: boolean): Column<Gstr1NoteRow>[] {
  return [
    { header: "Party", role: "title", cell: (r) => r.receiverName },
    ...(registered ? [{ header: "GSTIN", mono: true, cell: (r: Gstr1NoteRow) => r.gstin } satisfies Column<Gstr1NoteRow>] : []),
    {
      header: "Note",
      role: "subtitle",
      cell: (r) => (
        <span>
          <span
            className={cn(
              "mr-1 rounded px-1 py-px text-[10px] font-semibold",
              r.noteType === "C" ? "bg-sky-100 text-sky-800" : "bg-violet-100 text-violet-800",
            )}
          >
            {r.noteType === "C" ? "CREDIT" : "DEBIT"}
          </span>
          <InvoiceCell no={r.noteNo} erp={r.erpNoteNo} approved={r.approved} />
        </span>
      ),
    },
    { header: "Date", cell: (r) => fmtDate(r.noteDate), className: "whitespace-nowrap" },
    {
      header: "Original invoice",
      cell: (r) =>
        r.originalInvoiceNo ? (
          <span className="whitespace-nowrap">
            <span className="font-mono text-xs">{r.originalInvoiceNo}</span>
            {r.originalInvoiceDate ? (
              <span className="text-muted-foreground"> · {fmtDate(r.originalInvoiceDate)}</span>
            ) : null}
          </span>
        ) : (
          <span className="text-amber-700">not linked</span>
        ),
    },
    { header: "Supply type", cell: (r) => r.supplyType },
    { header: "POS", cell: (r) => r.placeOfSupply, className: "whitespace-nowrap" },
    { header: "Rate", value: (r) => r.rate, format: "rate" },
    { header: "Taxable", value: (r) => r.taxable, sum: true },
    { header: "IGST", value: (r) => r.igst, sum: true },
    { header: "CGST", value: (r) => r.cgst, sum: true },
    { header: "SGST", value: (r) => r.sgst, sum: true },
  ];
}

const nilColumns: Column<Gstr1NilRow>[] = [
  { header: "Description", role: "title", cell: (r) => r.description },
  { header: "Nil rated", value: (r) => r.nilRated, sum: true },
  { header: "Exempted", value: (r) => r.exempted, sum: true },
  { header: "Non-GST", value: (r) => r.nonGst, sum: true },
];

const hsnColumns: Column<Gstr1HsnRow>[] = [
  { header: "HSN", mono: true, role: "title", cell: (r) => r.hsn || <span className="text-amber-700">blank</span> },
  { header: "Description", role: "subtitle", cell: (r) => r.description },
  { header: "UQC", cell: (r) => r.uqc },
  { header: "Quantity", value: (r) => r.quantity, format: "qty" },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Total value", value: (r) => r.totalValue, sum: true },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "CGST", value: (r) => r.cgst, sum: true },
  { header: "SGST", value: (r) => r.sgst, sum: true },
];

const hsnSummaryColumns: Column<Gstr1HsnRow>[] = [
  { header: "HSN", mono: true, role: "title", cell: (r) => r.hsn || <span className="text-amber-700">blank</span> },
  { header: "Commodity", className: "min-w-[12rem] max-w-[22rem]", cell: (r) => r.commodity },
  { header: "Description", role: "subtitle", className: "whitespace-nowrap", cell: (r) => r.description },
  { header: "UQC", cell: (r) => r.uqc },
  { header: "Quantity", value: (r) => r.quantity, format: "qty" },
  { header: "Total value", value: (r) => r.totalValue, sum: true },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Taxable", value: (r) => r.taxable, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "CGST", value: (r) => r.cgst, sum: true },
  { header: "SGST", value: (r) => r.sgst, sum: true },
  { header: "Cess", value: (r) => r.cess, sum: true },
];

const salesRegisterColumns: Column<Gstr1SalesRegisterRow>[] = [
  { header: "Sales Ledger", role: "title", className: "min-w-[12rem]", cell: (r) => r.salesLedger },
  { header: "Voucher Type", className: "whitespace-nowrap", cell: (r) => r.voucherType },
  { header: "Invoice", mono: true, role: "subtitle", cell: (r) => r.invoiceNo },
  { header: "Date", className: "whitespace-nowrap", cell: (r) => fmtDate(r.invoiceDate) },
  { header: "Party", className: "min-w-[12rem]", cell: (r) => r.party },
  { header: "GSTIN", mono: true, cell: (r) => r.gstin },
  { header: "POS", className: "whitespace-nowrap", cell: (r) => r.placeOfSupply },
  { header: "HSN", mono: true, cell: (r) => r.hsn },
  { header: "Commodity", className: "min-w-[10rem]", cell: (r) => r.commodity },
  { header: "Qty", value: (r) => r.quantity, format: "qty" },
  { header: "UQC", cell: (r) => r.uqc },
  { header: "Rate", value: (r) => r.rate, format: "rate" },
  { header: "Value", value: (r) => r.value, sum: true },
  { header: "IGST", value: (r) => r.igst, sum: true },
  { header: "CGST", value: (r) => r.cgst, sum: true },
  { header: "SGST", value: (r) => r.sgst, sum: true },
  { header: "TCS", value: (r) => r.tcs, sum: true },
  { header: "Other charges", value: (r) => r.otherCharges, sum: true },
  { header: "GrossAmount", value: (r) => r.grossAmount, sum: true },
  {
    header: "GSTR-1",
    className: "whitespace-nowrap",
    cell: (r) =>
      r.gstr1Status === "In GSTR-1" ? (
        <span className="text-emerald-700">In GSTR-1</span>
      ) : (
        <span className="text-amber-700">{r.gstr1Status}</span>
      ),
  },
];

type TbTotals = Pick<
  Gstr1TrialBalanceRow,
  "openingDebit" | "openingCredit" | "debit" | "credit" | "closingDebit" | "closingCredit"
>;

const TB_AMOUNTS: { key: keyof TbTotals; label: string }[] = [
  { key: "openingDebit", label: "Opening Dr" },
  { key: "openingCredit", label: "Opening Cr" },
  { key: "debit", label: "Debit" },
  { key: "credit", label: "Credit" },
  { key: "closingDebit", label: "Closing Dr" },
  { key: "closingCredit", label: "Closing Cr" },
];

function tbTotals(rows: Gstr1TrialBalanceRow[]): TbTotals {
  const t: TbTotals = { openingDebit: 0, openingCredit: 0, debit: 0, credit: 0, closingDebit: 0, closingCredit: 0 };
  for (const r of rows) for (const { key } of TB_AMOUNTS) t[key] += r[key];
  return t;
}

function tbAmount(v: number): string {
  return Math.abs(v) < 0.005 ? "" : fmtMoney(v);
}

const TB_PAGE = 400;

function TrialBalanceView({ rows }: { rows: Gstr1TrialBalanceRow[] }) {
  const [search, setSearch] = useState("");
  const [group, setGroup] = useState("");
  const [limit, setLimit] = useState(TB_PAGE);
  const groupKey = (r: Gstr1TrialBalanceRow) => `${r.primary} › ${r.group}`;
  const groupOptions = useMemo(() => [...new Set(rows.map(groupKey))], [rows]);
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return rows.filter(
      (r) =>
        (!group || groupKey(r) === group) &&
        (!q || `${r.ledger} ${r.under} ${r.group} ${r.company}`.toLowerCase().includes(q)),
    );
  }, [rows, search, group]);
  const groups = useMemo(() => {
    const map = new Map<string, Gstr1TrialBalanceRow[]>();
    for (const r of filtered) {
      const k = groupKey(r);
      const list = map.get(k);
      if (list) list.push(r);
      else map.set(k, [r]);
    }
    return [...map.entries()];
  }, [filtered]);
  const all = useMemo(() => tbTotals(rows), [rows]);
  const shown = useMemo(() => tbTotals(filtered), [filtered]);
  const balanced = (a: number, b: number) => Math.abs(a - b) < 1;
  const ok =
    balanced(all.openingDebit, all.openingCredit) &&
    balanced(all.debit, all.credit) &&
    balanced(all.closingDebit, all.closingCredit);
  const adjustments = rows.filter((r) => r.isAdjustment);
  const multiCompany = new Set(rows.map((r) => r.company)).size > 1;

  if (rows.length === 0) {
    return (
      <div className="flex flex-col items-center px-5 py-10 text-center">
        <Inbox className="mb-2 h-6 w-6 text-primary" />
        <p className="text-sm text-muted-foreground">No ledger balances for this period.</p>
      </div>
    );
  }

  let budget = limit;
  return (
    <>
      <div className="flex flex-col gap-2 border-b border-border px-4 py-3 sm:flex-row sm:items-center">
        <Input
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setLimit(TB_PAGE);
          }}
          placeholder="Search ledger, group or company"
          className="h-9 sm:max-w-xs"
        />
        <select
          value={group}
          onChange={(e) => {
            setGroup(e.target.value);
            setLimit(TB_PAGE);
          }}
          className={cn(SELECT_CLASS, "sm:max-w-xs")}
        >
          <option value="">All groups ({count.format(groupOptions.length)})</option>
          {groupOptions.map((g) => (
            <option key={g} value={g}>
              {g}
            </option>
          ))}
        </select>
        <span
          className={cn(
            "inline-flex items-center gap-1 text-xs sm:ml-auto",
            ok ? "text-emerald-700" : "font-semibold text-red-700",
          )}
        >
          {ok ? <CheckCircle2 className="h-3.5 w-3.5" /> : <AlertTriangle className="h-3.5 w-3.5" />}
          {ok ? "Dr = Cr" : "Totals do not balance"}
          {adjustments.length > 0 ? " · includes adjustment rows" : ""}
        </span>
      </div>

      <div className="hidden overflow-x-auto md:block">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="bg-primary/10 text-left text-xs text-primary">
              <th className="px-3 py-2 font-semibold">Ledger</th>
              <th className="px-3 py-2 font-semibold">Sub-group</th>
              {multiCompany ? <th className="px-3 py-2 font-semibold">Company</th> : null}
              {TB_AMOUNTS.map((a) => (
                <th key={a.key} className="whitespace-nowrap px-3 py-2 text-right font-semibold">
                  {a.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {groups.map(([key, list]) => {
              if (budget <= 0) return null;
              const visible = list.slice(0, budget);
              budget -= visible.length;
              const t = tbTotals(list);
              return (
                <Fragment key={key}>
                  <tr className="bg-primary/5">
                    <td colSpan={multiCompany ? 9 : 8} className="px-3 py-1.5 text-xs font-semibold text-primary">
                      {key}
                    </td>
                  </tr>
                  {visible.map((r) => (
                    <tr
                      key={`${r.company}|${r.ledger}`}
                      className={cn("border-b border-border/60", r.isAdjustment && "bg-amber-50/70")}
                    >
                      <td className="px-3 py-1">{r.ledger}</td>
                      <td className="px-3 py-1 text-xs text-muted-foreground">{r.under}</td>
                      {multiCompany ? <td className="px-3 py-1 text-xs text-muted-foreground">{r.company}</td> : null}
                      {TB_AMOUNTS.map((a) => (
                        <td key={a.key} className="whitespace-nowrap px-3 py-1 text-right tabular-nums">
                          {tbAmount(r[a.key])}
                        </td>
                      ))}
                    </tr>
                  ))}
                  <tr className="border-b border-border text-xs font-semibold italic">
                    <td className="px-3 py-1" colSpan={multiCompany ? 3 : 2}>
                      Total {key.split(" › ")[1]}
                    </td>
                    {TB_AMOUNTS.map((a) => (
                      <td key={a.key} className="whitespace-nowrap px-3 py-1 text-right tabular-nums">
                        {tbAmount(t[a.key])}
                      </td>
                    ))}
                  </tr>
                </Fragment>
              );
            })}
            <tr className="bg-primary/10 font-semibold">
              <td className="px-3 py-2" colSpan={multiCompany ? 3 : 2}>
                {filtered.length === rows.length ? "Grand Total" : `Total of ${count.format(filtered.length)} shown`}
              </td>
              {TB_AMOUNTS.map((a) => (
                <td key={a.key} className="whitespace-nowrap px-3 py-2 text-right tabular-nums">
                  {fmtMoney(shown[a.key])}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>

      <div className="divide-y divide-border md:hidden">
        <div className="bg-primary/5 px-4 py-3">
          <p className="text-xs font-semibold text-primary">
            {filtered.length === rows.length ? "Grand Total" : `Total of ${count.format(filtered.length)} shown`}
          </p>
          <div className="mt-1.5 grid grid-cols-2 gap-x-3 gap-y-1 text-xs">
            {TB_AMOUNTS.map((a) => (
              <div key={a.key} className="flex justify-between gap-2">
                <span className="text-muted-foreground">{a.label}</span>
                <span className="font-semibold tabular-nums">{fmtMoney(shown[a.key])}</span>
              </div>
            ))}
          </div>
        </div>
        {groups.map(([key, list]) => {
          const t = tbTotals(list);
          return (
            <div key={key} className="px-4 py-3">
              <p className="text-xs font-semibold text-primary">{key}</p>
              <p className="text-[11px] text-muted-foreground">
                {count.format(list.length)} ledgers · Closing Dr {fmtMoney(t.closingDebit)} · Cr {fmtMoney(t.closingCredit)}
              </p>
              <div className="mt-2 space-y-2">
                {list.slice(0, 50).map((r) => (
                  <div
                    key={`${r.company}|${r.ledger}`}
                    className={cn("rounded border border-border/70 p-2 text-xs", r.isAdjustment && "bg-amber-50/70")}
                  >
                    <p className="break-words font-medium">{r.ledger}</p>
                    <div className="mt-1 grid grid-cols-2 gap-x-3 gap-y-0.5">
                      {TB_AMOUNTS.filter((a) => Math.abs(r[a.key]) >= 0.005).map((a) => (
                        <div key={a.key} className="flex justify-between gap-2">
                          <span className="text-muted-foreground">{a.label}</span>
                          <span className="tabular-nums">{fmtMoney(r[a.key])}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
                {list.length > 50 ? (
                  <p className="text-[11px] text-muted-foreground">
                    +{count.format(list.length - 50)} more — search or use Excel for the full list
                  </p>
                ) : null}
              </div>
            </div>
          );
        })}
      </div>

      {filtered.length > limit ? (
        <div className="hidden items-center justify-between gap-2 border-t border-border px-4 py-2.5 text-xs text-muted-foreground md:flex">
          <span>
            Showing {count.format(limit)} of {count.format(filtered.length)} ledgers (totals cover all; Excel has everything)
          </span>
          <Button type="button" variant="outline" size="sm" className="h-8" onClick={() => setLimit((n) => n + TB_PAGE * 3)}>
            Show more
          </Button>
        </div>
      ) : null}
      {adjustments.length > 0 ? (
        <p className="border-t border-border px-4 py-2 text-xs text-muted-foreground">
          Highlighted rows are not ERP ledgers: previous years&apos; P&amp;L carried forward (P&amp;L groups restart on 1
          April) and the difference in ERP postings, shown so that Dr = Cr.
        </p>
      ) : null}
    </>
  );
}

function PivotTable({
  title,
  rows,
  h1,
  h2,
  v1,
  v2,
}: {
  title: string;
  rows: Gstr1LedgerReconRow[];
  h1: string;
  h2: string;
  v1: (r: Gstr1LedgerReconRow) => number;
  v2: (r: Gstr1LedgerReconRow) => number;
}) {
  return (
    <div className="min-w-0">
      <p className="mb-1 text-sm font-semibold">{title}</p>
      <div className="overflow-x-auto rounded border border-border">
        <table className="w-full border-collapse text-xs sm:text-sm">
          <thead>
            <tr className="border-b border-border bg-sky-100 text-left font-semibold text-slate-800">
              <th className="px-2 py-1.5">Row Labels</th>
              <th className="whitespace-nowrap px-2 py-1.5 text-right">{h1}</th>
              <th className="whitespace-nowrap px-2 py-1.5 text-right">{h2}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr
                key={r.salesLedger}
                className={cn("border-b border-border/50", hasPivotDiff(r) && "bg-yellow-100/80")}
              >
                <td className="px-2 py-1">{r.salesLedger}</td>
                <td className="whitespace-nowrap px-2 py-1 text-right tabular-nums">{fmtMoney(v1(r))}</td>
                <td className="whitespace-nowrap px-2 py-1 text-right tabular-nums">{fmtMoney(v2(r))}</td>
              </tr>
            ))}
            <tr className="border-t-2 border-slate-400 bg-sky-100 font-semibold">
              <td className="px-2 py-1.5">Grand Total</td>
              <td className="whitespace-nowrap px-2 py-1.5 text-right tabular-nums">
                {fmtMoney(rows.reduce((s, r) => s + v1(r), 0))}
              </td>
              <td className="whitespace-nowrap px-2 py-1.5 text-right tabular-nums">
                {fmtMoney(rows.reduce((s, r) => s + v2(r), 0))}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}

function hasPivotDiff(r: Gstr1LedgerReconRow): boolean {
  return Math.abs(r.registerGross - r.hsnInvoiceValue) > 1 || Math.abs(r.registerValue - r.hsnTaxable) > 1;
}

function reconDiff(v: number): ReactNode {
  return <span className={cn(Math.abs(v) > 1 && "font-semibold text-amber-700")}>{fmtMoney(v)}</span>;
}

const reconDiffColumns: Column<Gstr1LedgerReconRow>[] = [
  { header: "Sales ledger", role: "title", className: "min-w-[14rem]", cell: (r) => r.salesLedger },
  {
    header: "Diff in invoice value",
    value: (r) => r.diffInvoiceValue,
    cell: (r) => reconDiff(r.diffInvoiceValue),
    sum: true,
  },
  {
    header: "Invoice value — causes",
    className: "min-w-[20rem] text-xs text-muted-foreground",
    cell: (r) => r.invoiceRemarks || "—",
  },
  {
    header: "Diff in taxable value",
    value: (r) => r.diffRegisterHsn,
    cell: (r) => reconDiff(r.diffRegisterHsn),
    sum: true,
  },
  { header: "Trial balance", value: (r) => r.trialBalance },
  { header: "Reg value − TB", value: (r) => r.diffRegisterTb, cell: (r) => reconDiff(r.diffRegisterTb), sum: true },
  {
    header: "Taxable / TB remarks",
    role: "subtitle",
    className: "min-w-[18rem] text-xs text-muted-foreground",
    cell: (r) => r.remarks || "—",
  },
];

function LedgerReconView({ rows }: { rows: Gstr1LedgerReconRow[] }) {
  const pivotRows = rows.filter(
    (r) => r.registerGross !== 0 || r.registerValue !== 0 || r.hsnInvoiceValue !== 0 || r.hsnTaxable !== 0,
  );
  const diffs = rows.filter((r) => hasPivotDiff(r) || Math.abs(r.diffRegisterTb) > 1);
  if (pivotRows.length === 0) {
    return (
      <div className="flex flex-col items-center px-5 py-10 text-center">
        <Inbox className="mb-2 h-6 w-6 text-primary" />
        <p className="text-sm text-muted-foreground">No sales in this period.</p>
      </div>
    );
  }
  return (
    <>
      <div className="grid gap-4 p-4 lg:grid-cols-2 2xl:grid-cols-3">
        <PivotTable
          title="Sales Reg."
          rows={pivotRows}
          h1="Sum of GrossAmount"
          h2="Sum of Value"
          v1={(r) => r.registerGross}
          v2={(r) => r.registerValue}
        />
        <PivotTable
          title="HSN"
          rows={pivotRows}
          h1="Sum of Invoice Value"
          h2="Sum of Taxable Value"
          v1={(r) => r.hsnInvoiceValue}
          v2={(r) => r.hsnTaxable}
        />
        <PivotTable
          title="Difference (Sales Reg. − HSN)"
          rows={pivotRows}
          h1="Diff in Invoice Value"
          h2="Diff in Taxable Value"
          v1={(r) => r.registerGross - r.hsnInvoiceValue}
          v2={(r) => r.registerValue - r.hsnTaxable}
        />
      </div>
      <div className="border-t border-border">
        <p className="px-4 pt-3 text-sm font-semibold">Differences explained (Sales Reg. vs HSN vs Trial Balance)</p>
        <DataTable
          rows={diffs}
          columns={reconDiffColumns}
          rowKey={(r) => r.salesLedger}
          empty="No ledger differs by more than ₹1."
        />
      </div>
      <div className="space-y-1 border-t border-border px-4 py-3 text-xs text-muted-foreground">
        <p>
          Sales Reg.: every sales invoice for the period (reported or not). GrossAmount = bill amount; Value = line amount
          × exchange rate.
        </p>
        <p>
          HSN: the same documents and netting as the HSN summary — credit/debit notes sit under the note&apos;s own ledger
          and taxable freight/packing is included in the taxable value.
        </p>
        <p>
          Diff in Invoice Value = GrossAmount − HSN Invoice Value (taxable + GST); Diff in Taxable Value = Value − HSN
          Taxable Value. Causes are signed as they move the difference.
        </p>
        <p>Highlighted rows: either difference exceeds ₹1.</p>
      </div>
    </>
  );
}

const docsColumns: Column<GstDocumentSeries>[] = [
  { header: "Nature of document", role: "subtitle", cell: (r) => r.nature },
  { header: "Series", mono: true, role: "title", cell: (r) => r.series },
  { header: "Company", cell: (r) => r.company },
  { header: "From", mono: true, cell: (r) => r.fromNo },
  { header: "To", mono: true, cell: (r) => r.toNo },
  { header: "Total", value: (r) => r.totalNumber, format: "count", sum: true },
  { header: "Cancelled", value: (r) => r.cancelled, format: "count", sum: true },
  { header: "Net issued", value: (r) => r.netIssued, format: "count", sum: true },
];

// ---------------------------------------------------------------- tabs

type TabKey =
  | "summary"
  | "b2b"
  | "b2cl"
  | "b2cs"
  | "exp"
  | "cdnr"
  | "cdnur"
  | "nil"
  | "hsnB2b"
  | "hsnB2c"
  | "hsnSummary"
  | "salesRegister"
  | "trialBalance"
  | "ledgerRecon"
  | "docs"
  | "exceptions";

const TABS: { key: TabKey; label: string; hint: string }[] = [
  { key: "summary", label: "Summary", hint: "Section totals and reconciliation with ERP sales" },
  { key: "b2b", label: "B2B", hint: "Table 4 — invoices to registered persons, incl. SEZ and deemed exports" },
  { key: "b2cl", label: "B2CL", hint: "Table 5 — inter-state invoices above ₹1,00,000 to unregistered persons" },
  { key: "b2cs", label: "B2CS", hint: "Table 7 — other supplies to unregistered persons, by place of supply and rate" },
  {
    key: "exp",
    label: "EXP",
    hint: "Table 6A — exports with/without payment of tax, checked against the uploaded ICEGATE shipping bills (FOB, EGM, IGST paid)",
  },
  { key: "cdnr", label: "CDNR", hint: "Table 9B — credit/debit notes to registered persons" },
  { key: "cdnur", label: "CDNUR", hint: "Table 9B — credit/debit notes to unregistered persons (B2CL / exports)" },
  { key: "nil", label: "Nil / Exempt", hint: "Table 8 — nil rated, exempted and non-GST supplies" },
  { key: "hsnB2b", label: "HSN B2B", hint: "Table 12 — HSN summary of supplies to registered persons" },
  { key: "hsnB2c", label: "HSN B2C", hint: "Table 12 — HSN summary of supplies to unregistered persons" },
  {
    key: "hsnSummary",
    label: "HSN Summary",
    hint: "HSN-wise summary of B2B + B2C with ERP commodity and 30-character GST description",
  },
  { key: "salesRegister", label: "Sales Register", hint: "Invoice-line sales register for the period" },
  { key: "trialBalance", label: "Trial Balance", hint: "Sales ledger postings for the period" },
  {
    key: "ledgerRecon",
    label: "Sales Reg vs HSN",
    hint: "Sales register vs HSN summary vs trial balance, by sales ledger",
  },
  { key: "docs", label: "Docs", hint: "Table 13 — documents issued (from Document Summary)" },
  { key: "exceptions", label: "Exceptions", hint: "Items to review before filing" },
];

function tabCount(report: Gstr1Report, key: TabKey): number | null {
  switch (key) {
    case "b2b":
      return new Set(report.b2b.map((r) => `${r.company}|${r.invoiceNo}`)).size;
    case "b2cl":
      return new Set(report.b2cl.map((r) => `${r.company}|${r.invoiceNo}`)).size;
    case "b2cs":
      return report.b2cs.length;
    case "exp":
      return new Set(report.exp.map((r) => `${r.company}|${r.invoiceNo}`)).size;
    case "cdnr":
      return new Set(report.cdnr.map((r) => `${r.company}|${r.noteType}|${r.noteNo}`)).size;
    case "cdnur":
      return new Set(report.cdnur.map((r) => `${r.company}|${r.noteType}|${r.noteNo}`)).size;
    case "hsnB2b":
      return report.hsnB2b.length;
    case "hsnB2c":
      return report.hsnB2c.length;
    case "hsnSummary":
      return report.hsnSummary?.length ?? 0;
    case "salesRegister":
      return new Set((report.salesRegister ?? []).map((r) => `${r.company}|${r.erpInvoiceNo}|${r.invoiceDate}`)).size;
    case "trialBalance":
      return report.trialBalance?.length ?? 0;
    case "ledgerRecon":
      return report.ledgerRecon?.length ?? 0;
    case "docs":
      return report.docs.length;
    case "exceptions":
      return report.exceptions.length;
    default:
      return null;
  }
}

const SEVERITY_STYLE: Record<Gstr1Exception["severity"], { icon: typeof Info; badge: string; row: string }> = {
  error: { icon: XCircle, badge: "bg-red-100 text-red-800", row: "border-l-red-500" },
  warning: { icon: AlertTriangle, badge: "bg-amber-100 text-amber-800", row: "border-l-amber-500" },
  info: { icon: Info, badge: "bg-sky-100 text-sky-800", row: "border-l-sky-400" },
};

function ExceptionsView({ exceptions }: { exceptions: Gstr1Exception[] }) {
  const [category, setCategory] = useState("");
  const [limit, setLimit] = useState(PAGE_SIZE);
  const categories = useMemo(() => {
    const map = new Map<string, { title: string; severity: Gstr1Exception["severity"]; n: number }>();
    for (const e of exceptions) {
      const cur = map.get(e.category);
      if (cur) cur.n++;
      else map.set(e.category, { title: e.title, severity: e.severity, n: 1 });
    }
    return Array.from(map.entries());
  }, [exceptions]);
  const rows = category ? exceptions.filter((e) => e.category === category) : exceptions;

  if (exceptions.length === 0) {
    return (
      <div className="flex flex-col items-center px-5 py-10 text-center">
        <CheckCircle2 className="mb-2 h-6 w-6 text-emerald-600" />
        <p className="text-sm text-muted-foreground">No exceptions — nothing needs review.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="flex flex-wrap gap-1.5 border-b border-border p-3">
        <button
          type="button"
          onClick={() => setCategory("")}
          className={cn(
            "rounded-full border px-2.5 py-1 text-xs",
            category === "" ? "border-primary bg-primary text-primary-foreground" : "border-border bg-background",
          )}
        >
          All ({count.format(exceptions.length)})
        </button>
        {categories.map(([key, c]) => {
          const Icon = SEVERITY_STYLE[c.severity].icon;
          return (
            <button
              key={key}
              type="button"
              onClick={() => {
                setCategory(key);
                setLimit(PAGE_SIZE);
              }}
              className={cn(
                "inline-flex items-center gap-1 rounded-full border px-2.5 py-1 text-xs",
                category === key ? "border-primary bg-primary text-primary-foreground" : "border-border bg-background",
              )}
            >
              <Icon className="h-3.5 w-3.5" />
              {c.title} ({count.format(c.n)})
            </button>
          );
        })}
      </div>
      <ul className="divide-y divide-border">
        {rows.slice(0, limit).map((e, i) => {
          const s = SEVERITY_STYLE[e.severity];
          return (
            <li key={`${e.category}|${e.company}|${e.documentNo}|${i}`} className={cn("border-l-4 px-4 py-2.5", s.row)}>
              <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                <span className={cn("rounded px-1.5 py-px text-[10px] font-semibold uppercase", s.badge)}>
                  {e.severity}
                </span>
                <span className="text-xs font-medium">{e.title}</span>
                {e.documentNo ? <span className="font-mono text-xs">{e.documentNo}</span> : null}
                {e.documentDate ? <span className="text-xs text-muted-foreground">{fmtDate(e.documentDate)}</span> : null}
                {e.amount ? <span className="ml-auto text-xs tabular-nums">₹ {fmtMoney(e.amount)}</span> : null}
              </div>
              <p className="mt-0.5 text-sm">{e.detail}</p>
              <p className="text-[11px] text-muted-foreground">
                {e.company}
                {e.party ? ` · ${e.party}` : ""}
              </p>
            </li>
          );
        })}
      </ul>
      {rows.length > limit ? (
        <div className="flex items-center justify-between gap-2 border-t border-border px-4 py-2.5 text-xs text-muted-foreground">
          <span>
            Showing {count.format(limit)} of {count.format(rows.length)}
          </span>
          <Button type="button" variant="outline" size="sm" className="h-8" onClick={() => setLimit((n) => n + PAGE_SIZE * 5)}>
            Show more
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function SummaryView({ report }: { report: Gstr1Report }) {
  const balanced = Math.abs(report.reconciliationDifference) < 1;
  const sections = report.summary.filter((s) => s.section !== "DOCS");
  const docs = report.summary.find((s) => s.section === "DOCS");
  return (
    <div className="space-y-4 p-3 sm:p-4">
      <div className="overflow-x-auto rounded-xl border border-border">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="bg-primary/10 text-left text-xs text-primary">
              <th className="px-3 py-2 font-semibold">Section</th>
              <th className="px-3 py-2 text-right font-semibold">Docs</th>
              <th className="hidden px-3 py-2 text-right font-semibold sm:table-cell">Invoice value</th>
              <th className="px-3 py-2 text-right font-semibold">Taxable</th>
              <th className="px-3 py-2 text-right font-semibold">IGST</th>
              <th className="px-3 py-2 text-right font-semibold">CGST</th>
              <th className="px-3 py-2 text-right font-semibold">SGST</th>
            </tr>
          </thead>
          <tbody>
            {sections.map((s) => {
              const net = s.section === "NET";
              return (
                <tr
                  key={s.section}
                  className={cn("border-b border-border/70", net ? "bg-primary/5 font-semibold" : "bg-card")}
                >
                  <td className="px-3 py-1.5">
                    <span className="font-medium">{s.section}</span>
                    <span className="block text-[11px] font-normal text-muted-foreground">{s.label}</span>
                  </td>
                  <td className="px-3 py-1.5 text-right tabular-nums">{net ? "" : count.format(s.documents)}</td>
                  <td className="hidden px-3 py-1.5 text-right tabular-nums sm:table-cell">
                    {net ? "" : fmtMoney(s.invoiceValue)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(s.taxable)}</td>
                  <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(s.igst)}</td>
                  <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(s.cgst)}</td>
                  <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(s.sgst)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {docs ? (
        <p className="text-xs text-muted-foreground">
          Documents issued (Table 13): {count.format(docs.documents)} net across {count.format(report.docs.length)}{" "}
          series. Credit notes are deducted in NET.
        </p>
      ) : null}

      <div className="rounded-xl border border-border">
        <div
          className={cn(
            "flex items-center gap-2 rounded-t-xl px-3 py-2 text-sm font-semibold",
            balanced ? "bg-emerald-50 text-emerald-800" : "bg-red-50 text-red-800",
          )}
        >
          <Scale className="h-4 w-4" />
          Reconciliation with ERP sales lines —{" "}
          {balanced ? "balanced" : `difference ₹ ${fmtMoney(report.reconciliationDifference)}`}
        </div>
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-semibold" />
                <th className="px-3 py-2 text-right font-semibold">Taxable</th>
                <th className="px-3 py-2 text-right font-semibold">IGST</th>
                <th className="px-3 py-2 text-right font-semibold">CGST</th>
                <th className="px-3 py-2 text-right font-semibold">SGST</th>
              </tr>
            </thead>
            <tbody>
              {report.reconciliation.map((r) => {
                const strong = r.label.startsWith("Expected") || r.label.startsWith("Reported") || r.label === "Difference";
                return (
                  <tr key={r.label} className={cn("border-t border-border/70", strong && "font-semibold")}>
                    <td className="min-w-[12rem] px-3 py-1.5">{r.label}</td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(r.taxable)}</td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(r.igst)}</td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(r.cgst)}</td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">{fmtMoney(r.sgst)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <p className="border-t border-border px-3 py-2 text-[11px] text-muted-foreground">
          Line values × exchange rate from the sales vouchers, compared with what the invoice sections report. Notes
          (CDNR/CDNUR) are reported separately and are not part of this check. Small paise differences come from
          per-invoice rounding.
        </p>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------- page

function Kpi({
  icon: Icon,
  label,
  value,
  sub,
  tone = "default",
}: {
  icon: typeof Info;
  label: string;
  value: string;
  sub?: string;
  tone?: "default" | "warn" | "good" | "bad";
}) {
  return (
    <div className="rounded-2xl border border-border bg-card p-3.5 shadow-soft">
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Icon
          className={cn(
            "h-4 w-4",
            tone === "warn"
              ? "text-amber-600"
              : tone === "good"
                ? "text-emerald-600"
                : tone === "bad"
                  ? "text-red-600"
                  : "text-primary",
          )}
        />
        {label}
      </div>
      <p className="mt-1 truncate text-xl font-semibold tabular-nums md:text-2xl">{value}</p>
      {sub ? <p className="truncate text-[11px] text-muted-foreground">{sub}</p> : null}
    </div>
  );
}

export function Gstr1Return({ month, onMonthChange }: { month: string; onMonthChange: (month: string) => void }) {
  const [scope, setScope] = useState("");
  const [includeUnapproved, setIncludeUnapproved] = useState(true);
  const [tab, setTab] = useState<TabKey>("summary");
  const [refreshToken, setRefreshToken] = useState(0);
  const [exporting, setExporting] = useState(false);
  const bypassCacheRef = useRef(false);

  const { from, to } = monthRange(month);

  const query = useQuery({
    queryKey: ["gstr1-report", from, to, scope, includeUnapproved, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getGstr1Report(from, to, scope, includeUnapproved, refresh);
    },
    staleTime: 15 * 60_000,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const report = query.data;
  const scopeOptions = useMemo(() => {
    const list = report?.scopes ?? [];
    return {
      all: list.find((s) => s.key === ""),
      groups: list.filter((s) => s.key.startsWith("gstin:")),
      units: list.filter((s) => s.key.startsWith("company:")),
    };
  }, [report?.scopes]);
  const knownScope = !report || report.scopes.some((s) => s.key === scope);

  const summary = (key: string) => report?.summary.find((s) => s.section === key);
  const net = summary("NET");
  const errors = report?.exceptions.filter((e) => e.severity === "error").length ?? 0;
  const warnings = report?.exceptions.filter((e) => e.severity === "warning").length ?? 0;
  const balanced = report ? Math.abs(report.reconciliationDifference) < 1 : true;

  async function exportExcel() {
    if (exporting) return;
    setExporting(true);
    try {
      await downloadGstr1Excel(from, to, scope, includeUnapproved);
      toast.success("GSTR-1 Excel downloaded");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Download failed");
    } finally {
      setExporting(false);
    }
  }

  const activeTab = TABS.find((t) => t.key === tab) ?? TABS[0];

  return (
    <div className="space-y-5">
      <div className="rounded-2xl border border-border bg-card p-3 shadow-soft sm:p-3.5">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[10rem_minmax(0,1.6fr)_auto_auto] lg:items-end">
          <div className="space-y-1">
            <Label htmlFor="gstr1-month" className="text-xs text-muted-foreground">
              Return period
            </Label>
            <Input
              id="gstr1-month"
              type="month"
              value={month}
              onChange={(e) => e.target.value && onMonthChange(e.target.value)}
              className="h-9 bg-background"
            />
          </div>
          <div className="min-w-0 space-y-1">
            <Label htmlFor="gstr1-scope" className="text-xs text-muted-foreground">
              Company / GSTIN
            </Label>
            <select
              id="gstr1-scope"
              value={scope}
              onChange={(e) => setScope(e.target.value)}
              className={SELECT_CLASS}
            >
              {!knownScope ? <option value={scope}>{scope.replace(/^company:|^gstin:/, "")}</option> : null}
              <option value="">{scopeOptions.all?.label ?? "All companies"} (review only — multiple GSTINs)</option>
              {scopeOptions.groups.length > 0 ? (
                <optgroup label="GSTIN — all units filing together">
                  {scopeOptions.groups.map((s) => (
                    <option key={s.key} value={s.key}>
                      {s.label} · {s.gstin}
                    </option>
                  ))}
                </optgroup>
              ) : null}
              <optgroup label="Single unit">
                {scopeOptions.units.map((s) => (
                  <option key={s.key} value={s.key}>
                    {s.label}
                    {s.gstin ? ` · ${s.gstin}` : ""}
                  </option>
                ))}
              </optgroup>
            </select>
          </div>
          <label className="flex h-9 cursor-pointer items-center gap-2 rounded-md border border-border bg-background px-3 text-sm">
            <input
              type="checkbox"
              checked={includeUnapproved}
              onChange={(e) => setIncludeUnapproved(e.target.checked)}
              className="h-4 w-4 accent-primary"
            />
            Include unapproved vouchers
          </label>
          <div className="flex flex-wrap items-end gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9 gap-1.5"
              disabled={query.isFetching}
              onClick={() => {
                bypassCacheRef.current = true;
                setRefreshToken((n) => n + 1);
              }}
            >
              <RefreshCw className={cn("h-4 w-4", query.isFetching && "animate-spin")} />
              Refresh
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9 gap-1.5"
              disabled={!report || exporting || query.isFetching}
              onClick={() => void exportExcel()}
            >
              {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
              Excel
            </Button>
          </div>
        </div>
        {report ? (
          <p className="mt-2.5 text-xs text-muted-foreground">
            {report.scopeLabel}
            {report.gstins.length === 1 ? ` · GSTIN ${report.gstins[0]}` : ""} · {formatMonth(month)} ·{" "}
            {report.companies.length} unit{report.companies.length === 1 ? "" : "s"}
          </p>
        ) : null}
      </div>

      {report && report.gstins.length > 1 ? (
        <div className="flex items-start gap-3 rounded-2xl border border-amber-300 bg-amber-50 p-3.5 text-sm text-amber-900">
          <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
          <p>
            This selection spans {report.gstins.length} GSTINs. A GSTR-1 is filed per GSTIN — use it for review only
            and pick a GSTIN or unit before downloading the Excel for filing.
          </p>
        </div>
      ) : null}

      {query.isError ? (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-2xl border border-destructive/30 bg-destructive/5 p-4 text-sm"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
          <div>
            <p className="font-semibold text-destructive">Could not load GSTR-1</p>
            <p className="text-muted-foreground">
              {query.error instanceof Error ? query.error.message : "Failed to load GSTR-1 report."}
            </p>
          </div>
        </div>
      ) : null}

      {query.isFetching && !report ? (
        <div className="flex items-center gap-2 rounded-2xl border border-border bg-card p-6 text-sm text-muted-foreground shadow-soft">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          Building GSTR-1 for {formatMonth(month)}…
        </div>
      ) : null}

      {report ? (
        <>
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <Kpi icon={Landmark} label="Net taxable value" value={`₹ ${fmtMoney(net?.taxable)}`} />
            <Kpi
              icon={FileText}
              label="Net tax"
              value={`₹ ${fmtMoney((net?.igst ?? 0) + (net?.cgst ?? 0) + (net?.sgst ?? 0))}`}
              sub={`IGST ${fmtMoney(net?.igst)} · CGST ${fmtMoney(net?.cgst)} · SGST ${fmtMoney(net?.sgst)}`}
            />
            <Kpi
              icon={Scale}
              label="Reconciliation"
              value={balanced ? "Balanced" : `₹ ${fmtMoney(report.reconciliationDifference)}`}
              sub="vs ERP sales voucher lines"
              tone={balanced ? "good" : "bad"}
            />
            <Kpi
              icon={AlertTriangle}
              label="Exceptions"
              value={count.format(report.exceptions.length)}
              sub={`${errors} error${errors === 1 ? "" : "s"} · ${warnings} warning${warnings === 1 ? "" : "s"}`}
              tone={errors > 0 ? "bad" : warnings > 0 ? "warn" : "good"}
            />
          </div>

          <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
            <div className="overflow-x-auto border-b border-border bg-muted/30">
              <div className="flex min-w-max gap-1 p-1.5" role="tablist">
                {TABS.map((t) => {
                  const n = tabCount(report, t.key);
                  const active = t.key === tab;
                  return (
                    <button
                      key={t.key}
                      type="button"
                      role="tab"
                      aria-selected={active}
                      onClick={() => setTab(t.key)}
                      className={cn(
                        "inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg px-3 py-1.5 text-sm transition-colors",
                        active ? "bg-primary text-primary-foreground shadow-sm" : "text-muted-foreground hover:bg-background",
                      )}
                    >
                      {t.label}
                      {n !== null ? (
                        <span
                          className={cn(
                            "rounded-full px-1.5 text-[11px] tabular-nums",
                            active
                              ? "bg-primary-foreground/20"
                              : t.key === "exceptions" && n > 0
                                ? "bg-amber-100 text-amber-800"
                                : "bg-muted",
                          )}
                        >
                          {count.format(n)}
                        </span>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            </div>
            <p className="border-b border-border px-4 py-2 text-xs text-muted-foreground">{activeTab.hint}</p>

            <div className={cn(query.isFetching && "opacity-60 transition-opacity")}>
              {tab === "summary" ? <SummaryView report={report} /> : null}
              {tab === "b2b" ? (
                <DataTable
                  key={`b2b|${scope}|${from}`}
                  rows={report.b2b}
                  columns={b2bColumns}
                  rowKey={(r, i) => `${r.company}|${r.invoiceNo}|${r.rate}|${i}`}
                  rowClassName={(r) => !r.approved && "bg-amber-50/60"}
                />
              ) : null}
              {tab === "b2cl" ? (
                <DataTable
                  key={`b2cl|${scope}|${from}`}
                  rows={report.b2cl}
                  columns={b2clColumns}
                  rowKey={(r, i) => `${r.company}|${r.invoiceNo}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "b2cs" ? (
                <DataTable
                  key={`b2cs|${scope}|${from}`}
                  rows={report.b2cs}
                  columns={b2csColumns}
                  rowKey={(r, i) => `${r.type}|${r.placeOfSupply}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "exp" ? (
                <>
                  <IcegatePanel summary={report.icegate} />
                  <DataTable
                    key={`exp|${scope}|${from}`}
                    rows={report.exp}
                    columns={expColumns}
                    rowKey={(r, i) => `${r.company}|${r.invoiceNo}|${r.rate}|${i}`}
                    rowClassName={(r) => !r.approved && "bg-amber-50/60"}
                  />
                  {report.icegate && report.icegate.notInErp.length > 0 ? (
                    <div className="border-t border-border">
                      <p className="bg-red-50 px-4 py-2 text-xs font-semibold text-red-800">
                        ICEGATE shipping bills with no ERP export invoice in this period (
                        {count.format(report.icegate.notInErp.length)})
                      </p>
                      <DataTable
                        key={`icegate-extra|${scope}|${from}`}
                        rows={report.icegate.notInErp}
                        columns={icegateExtraColumns}
                        rowKey={(r, i) => `${r.portCode}|${r.shippingBillNo}|${r.invoiceNo}|${i}`}
                      />
                    </div>
                  ) : null}
                </>
              ) : null}
              {tab === "cdnr" ? (
                <DataTable
                  key={`cdnr|${scope}|${from}`}
                  rows={report.cdnr}
                  columns={noteColumns(true)}
                  rowKey={(r, i) => `${r.company}|${r.noteType}|${r.noteNo}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "cdnur" ? (
                <DataTable
                  key={`cdnur|${scope}|${from}`}
                  rows={report.cdnur}
                  columns={noteColumns(false)}
                  rowKey={(r, i) => `${r.company}|${r.noteType}|${r.noteNo}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "nil" ? (
                <DataTable
                  key={`nil|${scope}|${from}`}
                  rows={report.nil}
                  columns={nilColumns}
                  rowKey={(r) => r.description}
                />
              ) : null}
              {tab === "hsnB2b" ? (
                <DataTable
                  key={`hsnB2b|${scope}|${from}`}
                  rows={report.hsnB2b}
                  columns={hsnColumns}
                  rowKey={(r, i) => `${r.hsn}|${r.uqc}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "hsnB2c" ? (
                <DataTable
                  key={`hsnB2c|${scope}|${from}`}
                  rows={report.hsnB2c}
                  columns={hsnColumns}
                  rowKey={(r, i) => `${r.hsn}|${r.uqc}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "hsnSummary" ? (
                <DataTable
                  key={`hsnSummary|${scope}|${from}`}
                  rows={report.hsnSummary ?? []}
                  columns={hsnSummaryColumns}
                  rowKey={(r, i) => `${r.hsn}|${r.uqc}|${r.rate}|${i}`}
                />
              ) : null}
              {tab === "salesRegister" ? (
                <DataTable
                  key={`salesRegister|${scope}|${from}`}
                  rows={report.salesRegister ?? []}
                  columns={salesRegisterColumns}
                  rowKey={(r, i) => `${r.company}|${r.erpInvoiceNo}|${i}`}
                  rowClassName={(r) => r.gstr1Status !== "In GSTR-1" && "bg-amber-50/60"}
                />
              ) : null}
              {tab === "trialBalance" ? (
                <TrialBalanceView key={`trialBalance|${scope}|${from}`} rows={report.trialBalance ?? []} />
              ) : null}
              {tab === "ledgerRecon" ? (
                <LedgerReconView key={`ledgerRecon|${scope}|${from}`} rows={report.ledgerRecon ?? []} />
              ) : null}
              {tab === "docs" ? (
                <DataTable
                  key={`docs|${scope}|${from}`}
                  rows={report.docs}
                  columns={docsColumns}
                  rowKey={(r) => `${r.company}|${r.nature}|${r.series}`}
                />
              ) : null}
              {tab === "exceptions" ? <ExceptionsView key={`exc|${scope}|${from}`} exceptions={report.exceptions} /> : null}
            </div>
          </section>
        </>
      ) : null}
    </div>
  );
}
