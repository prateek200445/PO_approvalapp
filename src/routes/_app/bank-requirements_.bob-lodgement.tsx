import { useMemo, useRef, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Download, Loader2, Upload } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  draftBobLodgementFromExcel,
  downloadBobLodgementPdf,
  listBobLodgementCompanies,
  parseBobLodgementExcel,
  type BobLodgementExcelParseResult,
  type BobLodgementExcelRow,
  type BobLodgementField,
} from "@/lib/bob-lodgement-api";

export const Route = createFileRoute("/_app/bank-requirements_/bob-lodgement")({
  head: () => ({ meta: [{ title: "BOB Lodgement — Banking — PO Portal" }] }),
  component: BobLodgementPage,
});

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

const REQUEST_TYPES = [
  "Collection Bills (FBC)",
  "Against 100% Advance (FBM)",
  "Direct Bills - Collection (FBCA)",
  "Bill Purchase – Non LC (FPO)",
  "Bill Purchase – LC (FPL)",
  "Bill Purchase FCY (FCP)",
  "Bill Discount - Non LC (FDO)",
  "Bill Discount- LC (FDL)",
  "Bill Discount FCY (FCD)",
  "Bill Negotiation-LC (FBN)",
  "PSDL",
];

function matchesCompany(row: BobLodgementExcelRow, companyId: string) {
  const c = `${row.companyName} ${row.invoiceNo}`.toUpperCase();
  if (!row.companyName?.trim()) return true;
  if (companyId === "PIL") return c.includes("PIL") || c.includes("PLASTENE INDIA");
  if (companyId === "KPW") return c.includes("KPW") || c.includes("K.P") || c.includes("KP WOVEN") || c.includes("K P WOVEN");
  return true;
}

function BobLodgementPage() {
  const companiesQuery = useQuery({
    queryKey: ["bob-lodgement-companies"],
    queryFn: listBobLodgementCompanies,
    staleTime: 60 * 60_000,
  });

  const fileRef = useRef<HTMLInputElement>(null);
  const [companyId, setCompanyId] = useState("PIL");
  const [parsed, setParsed] = useState<BobLodgementExcelParseResult | null>(null);
  const [filter, setFilter] = useState("");
  const [uploading, setUploading] = useState(false);
  const [selectedRow, setSelectedRow] = useState<number | null>(null);
  const [fields, setFields] = useState<BobLodgementField[]>([]);
  const [summary, setSummary] = useState<string | null>(null);
  const [loadingDraft, setLoadingDraft] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const money = useMemo(
    () => new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
    [],
  );

  const rows = useMemo(() => {
    const list = parsed?.rows ?? [];
    const q = filter.trim().toLowerCase();
    return list.filter((r) => {
      if (!matchesCompany(r, companyId)) return false;
      if (!q) return true;
      const blob = `${r.invoiceNo} ${r.buyerName} ${r.shippingBillNo} ${r.blNo} ${r.companyName}`.toLowerCase();
      return blob.includes(q);
    });
  }, [parsed, companyId, filter]);

  const onUpload = async (file: File | null) => {
    if (!file) return;
    setUploading(true);
    setFields([]);
    setSummary(null);
    setSelectedRow(null);
    try {
      const result = await parseBobLodgementExcel(file);
      setParsed(result);
      toast.success(`Loaded ${result.rows.length} rows from ${result.sheetName}`);
    } catch (err) {
      setParsed(null);
      toast.error(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  };

  const loadDraft = async (row: BobLodgementExcelRow) => {
    setSelectedRow(row.rowNumber);
    setLoadingDraft(true);
    try {
      const draft = await draftBobLodgementFromExcel({ companyId, row });
      setFields(draft.fields);
      setSummary(draft.sourceSummary);
      toast.success(`Filled ${companyId} lodgement from Excel — edit before download.`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to build draft");
    } finally {
      setLoadingDraft(false);
    }
  };

  const updateField = (key: string, value: string) => {
    setFields((prev) => prev.map((f) => (f.key === key ? { ...f, value } : f)));
  };

  const onDownload = async () => {
    if (!fields.length) return;
    setDownloading(true);
    try {
      const name = await downloadBobLodgementPdf({ companyId, fields });
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(false);
    }
  };

  const sortedFields = useMemo(
    () => [...fields].sort((a, b) => a.sort - b.sort || a.label.localeCompare(b.label)),
    [fields],
  );

  const mapped = parsed ? Object.entries(parsed.mappedColumns) : [];

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to="/bank-requirements"
            className="mb-2 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> Banking
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">BOB Lodgement</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Upload the Container Dispatch / finance Excel, pick PIL or KPW, select a row, edit fields, and download the filled Bank of Baroda lodgement PDF.
          </p>
        </div>
        <Button onClick={onDownload} disabled={downloading || !fields.length}>
          {downloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
          Download PDF
        </Button>
      </div>

      <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)]">
        <aside className="space-y-4 rounded-xl border border-border bg-card p-4">
          <div className="space-y-2">
            <Label htmlFor="company">Lodgement format</Label>
            <select
              id="company"
              className={SELECT_CLASS}
              value={companyId}
              onChange={(e) => {
                setCompanyId(e.target.value);
                setFields([]);
                setSummary(null);
                setSelectedRow(null);
              }}
            >
              {(companiesQuery.data ?? [
                { id: "PIL", title: "Plastene India Limited (PIL)", shortLabel: "PIL" },
                { id: "KPW", title: "KP Woven Private Limited (KPW)", shortLabel: "KPW" },
              ]).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.title}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label>Dispatch / finance Excel</Label>
            <input
              ref={fileRef}
              type="file"
              accept=".xlsx,.xlsm,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              className="hidden"
              onChange={(e) => void onUpload(e.target.files?.[0] ?? null)}
            />
            <Button
              type="button"
              variant="secondary"
              className="w-full"
              disabled={uploading}
              onClick={() => fileRef.current?.click()}
            >
              {uploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
              {parsed ? "Replace Excel" : "Upload Excel"}
            </Button>
            {parsed && (
              <p className="text-xs text-muted-foreground">
                {parsed.fileName || "Workbook"} · sheet {parsed.sheetName} · {parsed.rows.length} rows
              </p>
            )}
          </div>

          {mapped.length > 0 && (
            <div className="rounded-md border border-border bg-secondary/20 p-2 text-xs text-muted-foreground">
              <div className="mb-1 font-medium text-foreground">Mapped columns</div>
              <div className="space-y-0.5">
                {mapped.map(([key, header]) => (
                  <div key={key}>
                    <span className="text-foreground">{key}</span> ← {header}
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="filter">Filter rows</Label>
            <Input
              id="filter"
              value={filter}
              placeholder="Invoice / SB / buyer / BL"
              onChange={(e) => setFilter(e.target.value)}
              disabled={!parsed}
            />
          </div>

          <div className="max-h-[420px] space-y-2 overflow-auto pr-1">
            {rows.map((row) => {
              const active = row.rowNumber === selectedRow;
              return (
                <button
                  key={row.rowNumber}
                  type="button"
                  onClick={() => void loadDraft(row)}
                  className={`w-full rounded-lg border px-3 py-2 text-left text-sm transition ${
                    active ? "border-primary bg-primary/5" : "border-border hover:bg-secondary/40"
                  }`}
                >
                  <div className="font-medium">{row.buyerName || "—"}</div>
                  <div className="mt-0.5 text-xs text-muted-foreground">
                    {row.invoiceNo || "No invoice"} · SB {row.shippingBillNo || "—"}
                  </div>
                  <div className="mt-0.5 text-xs text-muted-foreground">
                    {row.currency || "—"} {money.format(row.amount || 0)}
                    {row.portOfDestination ? ` · ${row.portOfDestination}` : ""}
                  </div>
                </button>
              );
            })}
            {parsed && !rows.length && (
              <p className="text-xs text-muted-foreground">No rows match this company / filter.</p>
            )}
            {!parsed && (
              <p className="text-xs text-muted-foreground">Upload the master Excel to list shipments.</p>
            )}
          </div>
        </aside>

        <section className="space-y-3 rounded-xl border border-border bg-card p-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <h2 className="text-sm font-semibold">Lodgement fields (editable)</h2>
              {summary && <p className="mt-0.5 text-xs text-muted-foreground">{summary}</p>}
            </div>
            {loadingDraft && (
              <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                <Loader2 className="h-3.5 w-3.5 animate-spin" /> Loading…
              </span>
            )}
          </div>

          {!sortedFields.length ? (
            <p className="text-sm text-muted-foreground">
              Upload Excel, choose PIL or KPW, then click a row to fill the lodgement form.
            </p>
          ) : (
            <div className="grid gap-3">
              {sortedFields.map((field) => (
                <div key={field.key} className="space-y-1.5">
                  <Label htmlFor={`f-${field.key}`}>{field.label}</Label>
                  {field.key === "requestType" ? (
                    <select
                      id={`f-${field.key}`}
                      className={SELECT_CLASS}
                      value={field.value}
                      onChange={(e) => updateField(field.key, e.target.value)}
                    >
                      {REQUEST_TYPES.map((t) => (
                        <option key={t} value={t}>
                          {t}
                        </option>
                      ))}
                    </select>
                  ) : field.key === "underLc" ? (
                    <select
                      id={`f-${field.key}`}
                      className={SELECT_CLASS}
                      value={field.value}
                      onChange={(e) => updateField(field.key, e.target.value)}
                    >
                      <option value="No">No</option>
                      <option value="Yes">Yes</option>
                    </select>
                  ) : field.multiline ? (
                    <Textarea
                      id={`f-${field.key}`}
                      rows={4}
                      value={field.value}
                      onChange={(e) => updateField(field.key, e.target.value)}
                    />
                  ) : (
                    <Input
                      id={`f-${field.key}`}
                      value={field.value}
                      onChange={(e) => updateField(field.key, e.target.value)}
                    />
                  )}
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
