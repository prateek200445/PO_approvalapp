import { createFileRoute, Link } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArrowLeft, ClipboardList, Download, FileSpreadsheet, Loader2, Package, Plus, Trash2, Upload, Building2 } from "lucide-react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { MonthPickerField } from "@/components/MonthPickerField";
import {
  getPnlCompanies,
  getPnlProvisions,
  getPnlStockYear,
  getPnlUploads,
  getPnlOverhead,
  savePnlProvisions,
  savePnlStockYear,
  savePnlOverhead,
  downloadPnlTemplate,
  savePnlUpload,
} from "@/lib/pnl-api";
import {
  currentMonthValue,
  formatLacs,
  isSingleCompany,
  type PnlOverheadState,
  type PnlProvisionRow,
  type PnlStockYearRow,
  type PnlStockYearState,
  type PnlUploadItem,
} from "@/lib/pnl-types";
import { useIsMobile } from "@/hooks/use-mobile";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/pnl")({
  head: () => ({ meta: [{ title: "P&L Inputs — PO Portal" }] }),
  component: PnlPage,
});

function PnlPage() {
  const [company, setCompany] = useState("All Companies");
  const [month, setMonth] = useState(currentMonthValue());
  const single = isSingleCompany(company);

  const companiesQuery = useQuery({
    queryKey: ["pnl-companies"],
    queryFn: getPnlCompanies,
    staleTime: 60 * 60_000,
  });

  const uploadsQuery = useQuery({
    queryKey: ["pnl-uploads", company, month],
    queryFn: () => getPnlUploads(company, month),
    enabled: single,
    staleTime: 15_000,
  });

  const companyOptions = useMemo(
    () => (companiesQuery.data ?? []).map((c) => ({ value: c.value, label: c.label })),
    [companiesQuery.data],
  );

  return (
    <div className="mx-auto max-w-6xl space-y-6 pb-10">
      <div className="flex items-start gap-4">
        <Link
          to="/ledgers"
          className="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border bg-card text-muted-foreground shadow-sm transition hover:border-primary/40 hover:bg-secondary hover:text-foreground"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div className="min-w-0 pt-0.5">
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">P&L Inputs</h1>
          <p className="mt-1.5 max-w-2xl text-sm leading-relaxed text-muted-foreground">
            Upload monthly stock, provision, and Common / HO files. The result page uses these together with the trial
            balance.
          </p>
        </div>
      </div>

      <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="space-y-2 text-sm">
            <span className="font-medium text-foreground">Company</span>
            <SearchableSelect
              options={companyOptions}
              value={company}
              onChange={setCompany}
              placeholder={companiesQuery.isLoading ? "Loading…" : "Select company"}
              disabled={companiesQuery.isLoading}
            />
          </label>
          <label className="space-y-2 text-sm">
            <span className="font-medium text-foreground">Month / Year</span>
            <MonthPickerField value={month} onChange={setMonth} placeholder="Select month" />
          </label>
        </div>
      </div>

      {single ? (
        <UploadInboxPanel
          company={company}
          month={month}
          uploads={uploadsQuery.data}
          loading={uploadsQuery.isLoading}
          onSaved={() => void uploadsQuery.refetch()}
        />
      ) : (
        <div className="rounded-2xl border border-dashed bg-muted/20 px-6 py-10 text-center">
          <FileSpreadsheet className="mx-auto h-8 w-8 text-muted-foreground/70" />
          <p className="mt-3 text-sm font-medium">Select one company</p>
          <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
            Pick a single factory from the list above to download templates and upload Excel files for that month.
          </p>
        </div>
      )}
    </div>
  );
}

function UploadInboxPanel({
  company,
  month,
  uploads,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  uploads?: PnlUploadItem[];
  loading: boolean;
  onSaved: () => void;
}) {
  const [files, setFiles] = useState<{
    stock: File | null;
    provision: File | null;
    common: File | null;
  }>({
    stock: null,
    provision: null,
    common: null,
  });

  const upload = useMutation({
    mutationFn: async (input: { type: "stock" | "provision" | "common"; file: File }) => {
      await savePnlUpload(input.type, company, month, input.file);
    },
    onSuccess: () => {
      toast.success("File uploaded");
      setFiles({ stock: null, provision: null, common: null });
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  const [downloading, setDownloading] = useState<"stock" | "provision" | "common" | null>(null);

  const cards: {
    key: keyof typeof files;
    type: "stock" | "provision" | "common";
    title: string;
    hint: string;
    icon: typeof Package;
  }[] = [
    {
      key: "stock",
      type: "stock",
      title: "Stock Excel",
      hint: "Download the template, fill amounts in lacs, then upload.",
      icon: Package,
    },
    {
      key: "provision",
      type: "provision",
      title: "Provision Excel",
      hint: "Month-end provision ledgers for this company.",
      icon: ClipboardList,
    },
    {
      key: "common",
      type: "common",
      title: "Common / HO Excel",
      hint: "Plant share of common and head-office costs.",
      icon: Building2,
    },
  ];

  async function handleTemplateDownload(type: "stock" | "provision" | "common") {
    setDownloading(type);
    try {
      await downloadPnlTemplate(type, company, month);
      toast.success("Template downloaded");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Failed to download template");
    } finally {
      setDownloading(null);
    }
  }

  return (
    <div className="space-y-6">
      <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold tracking-tight">Excel uploads</h2>
            <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
              One file per type for this company and month. Download the template first if you need the layout.
            </p>
          </div>
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <FileSpreadsheet className="h-5 w-5" />
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-3">
          {cards.map((card) => {
            const Icon = card.icon;
            const selected = files[card.key];
            return (
              <div key={card.key} className="flex flex-col rounded-xl border bg-background p-4">
                <div className="flex items-start gap-3">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-secondary text-foreground">
                    <Icon className="h-4 w-4" />
                  </div>
                  <div className="min-w-0">
                    <div className="text-sm font-semibold">{card.title}</div>
                    <div className="mt-1 text-xs leading-relaxed text-muted-foreground">{card.hint}</div>
                  </div>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  className="mt-4 w-full"
                  disabled={downloading === card.type}
                  onClick={() => handleTemplateDownload(card.type)}
                >
                  {downloading === card.type ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Download className="mr-2 h-4 w-4" />
                  )}
                  Download template
                </Button>
                <label
                  className={cn(
                    "mt-3 flex min-h-[7.5rem] cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border border-dashed px-3 py-5 text-center transition",
                    selected
                      ? "border-primary/50 bg-primary/5"
                      : "hover:border-primary/30 hover:bg-secondary/40",
                  )}
                >
                  <Upload className="h-5 w-5 text-muted-foreground" />
                  <span className="max-w-full truncate px-1 text-xs font-medium">
                    {selected?.name ?? "Choose .xlsx"}
                  </span>
                  <input
                    type="file"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    className="sr-only"
                    onChange={(e) => {
                      const next = e.target.files?.[0] ?? null;
                      setFiles((prev) => ({ ...prev, [card.key]: next }));
                    }}
                  />
                </label>
                <Button
                  type="button"
                  className="mt-3 w-full"
                  disabled={!selected || upload.isPending}
                  onClick={() => {
                    if (!selected) return;
                    upload.mutate({ type: card.type, file: selected });
                  }}
                >
                  {upload.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                  Upload
                </Button>
              </div>
            );
          })}
        </div>
      </div>

      <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-6">
        <h3 className="text-sm font-semibold tracking-tight">Latest uploads</h3>
        <div className="mt-4 space-y-2">
          {loading ? (
            <Busy label="Loading uploaded files…" />
          ) : (uploads ?? []).length > 0 ? (
            uploads!.map((item) => (
              <div
                key={item.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-xl border bg-background px-3 py-3 text-sm"
              >
                <div className="min-w-0">
                  <span className="inline-flex rounded-md bg-secondary px-2 py-0.5 text-xs font-medium">
                    {item.uploadType}
                  </span>
                  <span className="ml-2 text-muted-foreground">{item.originalFileName}</span>
                </div>
                <div className="text-xs text-muted-foreground">
                  {item.fileSizeBytes.toLocaleString("en-IN")} bytes · {new Date(item.uploadedAt).toLocaleString()}
                </div>
              </div>
            ))
          ) : (
            <p className="rounded-xl border border-dashed px-4 py-6 text-center text-sm text-muted-foreground">
              No files uploaded yet for this company and month.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

function ProvisionTab({
  company,
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: { rows: PnlProvisionRow[]; ledgerOptions: string[] };
  loading: boolean;
  onSaved: () => void;
}) {
  const [rows, setRows] = useState<PnlProvisionRow[] | null>(null);
  const visible = rows ?? data?.rows ?? [];

  const save = useMutation({
    mutationFn: () => savePnlProvisions(company, month, visible),
    onSuccess: () => {
      toast.success("Provision saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading provision…" />;

  function addRow() {
    setRows([...(visible.length ? visible : data?.rows ?? []), { ledgerName: "", amount: 0, amountLacs: 0 }]);
  }

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">Enter month-end provision in lacs. Saved to ERP Provisioning.</p>
      {visible.map((row, idx) => (
        <div key={idx} className="flex gap-2">
          <Input
            list="pnl-ledgers"
            placeholder="Ledger"
            value={row.ledgerName}
            onChange={(e) => {
              const next = visible.map((r, i) => (i === idx ? { ...r, ledgerName: e.target.value } : r));
              setRows(next);
            }}
          />
          <Input
            type="number"
            inputMode="decimal"
            className="w-28"
            placeholder="Lacs"
            value={row.amountLacs || ""}
            onChange={(e) => {
              const lacs = Number(e.target.value);
              const next = visible.map((r, i) =>
                i === idx ? { ...r, amountLacs: lacs, amount: 0 } : r,
              );
              setRows(next);
            }}
          />
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setRows(visible.filter((_, i) => i !== idx))}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}
      <datalist id="pnl-ledgers">
        {(data?.ledgerOptions ?? []).slice(0, 400).map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
      <div className="flex gap-2">
        <Button variant="outline" onClick={addRow}>
          <Plus className="mr-1 h-4 w-4" />
          Add line
        </Button>
        <Button onClick={() => save.mutate()} disabled={save.isPending}>
          {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
          Save provision
        </Button>
      </div>
    </div>
  );
}

function StockTab({
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: PnlStockYearState;
  loading: boolean;
  onSaved: () => void;
}) {
  const isMobile = useIsMobile();
  const [draft, setDraft] = useState<PnlStockYearState | null>(null);
  const sheet = draft ?? data;

  const save = useMutation({
    mutationFn: () => {
      if (!sheet) throw new Error("Nothing to save");
      return savePnlStockYear(sheet);
    },
    onSuccess: () => {
      toast.success("Stock values saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading stock values…" />;
  if (!sheet) return null;

  const columns = sheet.columns ?? [];
  const rows = (sheet.rows ?? []).map((row) => ({
    ...row,
    months: row.months ?? {},
  }));

  function setOpening(category: string, value: number) {
    setDraft({
      ...sheet!,
      rows: rows.map((r) => (r.category === category ? { ...r, openingLacs: value } : r)),
    });
  }

  function setMonthValue(category: string, key: string, value: number) {
    setDraft({
      ...sheet!,
      rows: rows.map((r) =>
        r.category === category ? { ...r, months: { ...r.months, [key]: value } } : r,
      ),
    });
  }

  function previousKey(key: string) {
    const i = columns.findIndex((c) => c.key === key);
    return i > 0 ? columns[i - 1].key : null;
  }

  const totals = (key: "opening" | string) =>
    rows.reduce((sum, r) => sum + (key === "opening" ? r.openingLacs || 0 : r.months[key] || 0), 0);

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        Stock sheet {sheet.fyLabel} (lacs). Total is RM + packing + WIP + FG + traded + stores — not entered.
        Selected month for P&amp;L is highlighted.
      </p>

      {isMobile ? (
        <MobileStockEditor
          month={month}
          rows={rows}
          previousKey={previousKey(month)}
          onOpening={setOpening}
          onMonth={setMonthValue}
        />
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-max min-w-full border-collapse text-sm">
            <thead>
              <tr className="bg-muted/60 text-xs text-muted-foreground">
                <th className="sticky left-0 z-10 bg-muted px-2 py-2 text-left font-medium">Type</th>
                <th className="px-2 py-2 text-right font-medium">op</th>
                {columns.map((col) => (
                  <th
                    key={col.key}
                    className={cn(
                      "px-2 py-2 text-right font-medium",
                      col.key === month && "bg-primary/15 text-foreground",
                    )}
                  >
                    {col.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.category} className="border-t">
                  <td className="sticky left-0 z-10 bg-card px-2 py-1.5 font-medium">{row.label}</td>
                  <td className="px-1 py-1">
                    <StockCell value={row.openingLacs} onChange={(v) => setOpening(row.category, v)} />
                  </td>
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={cn("px-1 py-1", col.key === month && "bg-primary/5")}
                    >
                      <StockCell
                        value={row.months[col.key] || 0}
                        onChange={(v) => setMonthValue(row.category, col.key, v)}
                      />
                    </td>
                  ))}
                </tr>
              ))}
              <tr className="border-t bg-muted/40 font-semibold">
                <td className="sticky left-0 z-10 bg-muted/40 px-2 py-2">Total Stock</td>
                <td className="px-2 py-2 text-right tabular-nums">{formatLacs(totals("opening"))}</td>
                {columns.map((col) => (
                  <td
                    key={col.key}
                    className={cn(
                      "px-2 py-2 text-right tabular-nums",
                      col.key === month && "bg-primary/10",
                    )}
                  >
                    {formatLacs(totals(col.key))}
                  </td>
                ))}
              </tr>
            </tbody>
          </table>
        </div>
      )}

      <Button onClick={() => save.mutate()} disabled={save.isPending}>
        {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        Save stock
      </Button>
    </div>
  );
}

function MobileStockEditor({
  month,
  rows,
  previousKey,
  onOpening,
  onMonth,
}: {
  month: string;
  rows: PnlStockYearRow[];
  previousKey: string | null;
  onOpening: (category: string, value: number) => void;
  onMonth: (category: string, key: string, value: number) => void;
}) {
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-[1fr_5.5rem_5.5rem] gap-2 text-xs font-medium text-muted-foreground">
        <span>Type</span>
        <span className="text-right">Opening</span>
        <span className="text-right">Closing</span>
      </div>
      {rows.map((row) => {
        const opening = previousKey ? row.months[previousKey] || 0 : row.openingLacs || 0;
        const closing = row.months[month] || 0;
        return (
          <div key={row.category} className="grid grid-cols-[1fr_5.5rem_5.5rem] items-center gap-2">
            <span className="text-sm">{row.label}</span>
            <StockCell
              value={opening}
              onChange={(v) => (previousKey ? onMonth(row.category, previousKey, v) : onOpening(row.category, v))}
            />
            <StockCell value={closing} onChange={(v) => onMonth(row.category, month, v)} />
          </div>
        );
      })}
    </div>
  );
}

function StockCell({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  return (
    <Input
      type="number"
      inputMode="decimal"
      className="h-8 min-w-[4.5rem] px-1 text-right text-sm"
      value={value || ""}
      onChange={(e) => onChange(Number(e.target.value))}
    />
  );
}

function OverheadTab({
  company,
  month,
  data,
  loading,
  onSaved,
}: {
  company: string;
  month: string;
  data?: PnlOverheadState;
  loading: boolean;
  onSaved: () => void;
}) {
  const [commonLacs, setCommonLacs] = useState<number | null>(null);
  const [hoLacs, setHoLacs] = useState<number | null>(null);
  const common = commonLacs ?? data?.commonLacs ?? 0;
  const ho = hoLacs ?? data?.hoLacs ?? 0;

  const save = useMutation({
    mutationFn: () => savePnlOverhead(company, month, common, ho),
    onSuccess: () => {
      toast.success("Common / HO saved");
      onSaved();
    },
    onError: (err: Error) => toast.error(err.message),
  });

  if (loading && !data) return <Busy label="Loading Common / HO…" />;

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        Plant share of common and head-office costs for this month, in lacs. Not posted to ERP ledgers — only used on
        this P&amp;L. Leave 0 if you do not allocate. YTD is the sum of April through this month.
      </p>
      <label className="block space-y-1 text-sm">
        <span className="text-muted-foreground">Common expenses</span>
        <Input
          type="number"
          inputMode="decimal"
          value={common || ""}
          onChange={(e) => setCommonLacs(Number(e.target.value))}
        />
      </label>
      <label className="block space-y-1 text-sm">
        <span className="text-muted-foreground">HO expenses</span>
        <Input
          type="number"
          inputMode="decimal"
          value={ho || ""}
          onChange={(e) => setHoLacs(Number(e.target.value))}
        />
      </label>
      <Button onClick={() => save.mutate()} disabled={save.isPending}>
        {save.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        Save Common / HO
      </Button>
    </div>
  );
}

function Kpi({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <div className="text-xs text-muted-foreground">{label} (lacs)</div>
      <div className="mt-1 text-xl font-semibold tabular-nums">{formatLacs(value)}</div>
    </div>
  );
}

function Busy({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-4 text-sm text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      {label}
    </div>
  );
}
