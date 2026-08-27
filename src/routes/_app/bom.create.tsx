import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { FilePlus2, Loader2, RefreshCcw, Save } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth-context";
import { createBom, fetchBomEditor, previewBom, updateBom } from "@/lib/bom-api";
import type {
  BomCreateHeaderInput,
  BomCreateLineInput,
  BomCreatePreviewResult,
  BomCreateRequest,
} from "@/lib/bom-types";
import { BomFieldLabel, BomPageHeader, BomPageShell, BomPanel } from "@/components/bom/bom-ui";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

export const Route = createFileRoute("/_app/bom/create")({
  validateSearch: (search: Record<string, unknown>) => ({
    filePoNo: typeof search.filePoNo === "string" ? search.filePoNo : "",
  }),
  head: () => ({ meta: [{ title: "Create BOM - PO Portal" }] }),
  component: BomCreatePage,
});

function emptyHeader(userName?: string): BomCreateHeaderInput {
  return {
    filePoNo: "",
    customer: "",
    sysDate: new Date().toISOString().slice(0, 10),
    printType: "",
    poNo: "",
    poNos: "",
    bagType: "",
    sizeL: null,
    sizeW: null,
    sizeH: null,
    sizeType: "INNER",
    swl: "",
    sfRatio: "",
    qty: "",
    qtyUnit: "NOS",
    fsType: "",
    dsType: "",
    dsType1: "",
    dsType2: "",
    loopType: "",
    fabColor: "",
    instruction: "",
    bodyRemarks: "",
    printingRemarks: "",
    refNo: "",
    doc: "",
    doc1: "",
    doc2: "",
    docUnit: "",
    docNumber: "",
    knotType: "",
    rpFabric: "",
    isDropLoop: false,
    userName: userName ?? "",
  };
}

function BomCreatePage() {
  const { user } = useAuth();
  const navigate = Route.useNavigate();
  const search = Route.useSearch();
  const [header, setHeader] = useState<BomCreateHeaderInput>(() => emptyHeader(user?.username));
  const [approvalsText, setApprovalsText] = useState("Marketing");
  const [bom1Text, setBom1Text] = useState("");
  const [bom3Text, setBom3Text] = useState("");
  const [linesText, setLinesText] = useState("");
  const [preview, setPreview] = useState<BomCreatePreviewResult | null>(null);
  const [loadingSnapshot, setLoadingSnapshot] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const [saving, setSaving] = useState<"create" | "update" | null>(null);
  const canPersist = preview !== null && !loadingSnapshot && !previewing && saving === null;

  const stats = useMemo(() => {
    const approvals = parseApprovals(approvalsText);
    return {
      approvalCount: approvals.length,
      bom1Count: countPairs(bom1Text),
      bom3Count: countPairs(bom3Text),
    };
  }, [approvalsText, bom1Text, bom3Text]);

  function setHeaderField<K extends keyof BomCreateHeaderInput>(key: K, value: BomCreateHeaderInput[K]) {
    setPreview(null);
    setHeader((current) => ({ ...current, [key]: value }));
  }

  function setApprovalsValue(value: string) {
    setPreview(null);
    setApprovalsText(value);
  }

  function setBom1ValueText(value: string) {
    setPreview(null);
    setBom1Text(value);
  }

  function setBom3ValueText(value: string) {
    setPreview(null);
    setBom3Text(value);
  }

  function setLinesValueText(value: string) {
    setPreview(null);
    setLinesText(value);
  }

  function setBom1KeyValue(key: string, value: string) {
    setPreview(null);
    setBom1Text((current) => setKeyValueInText(current, key, value));
  }

  function setBom3KeyValue(key: string, value: string) {
    setPreview(null);
    setBom3Text((current) => setKeyValueInText(current, key, value));
  }

  async function loadSnapshot(filePoNo: string, options?: { syncSearch?: boolean; successMessage?: string }) {
    const normalizedFilePoNo = filePoNo.trim();
    if (!normalizedFilePoNo) {
      toast.error("Enter quotation number first.");
      return;
    }

    setLoadingSnapshot(true);
    try {
      const snapshot = await fetchBomEditor(normalizedFilePoNo);
      if (!snapshot) {
        toast.error("No BOM snapshot found for that quotation.");
        return;
      }

      setHeader({
        ...snapshot.header,
        sysDate: snapshot.header.sysDate ? snapshot.header.sysDate.slice(0, 10) : "",
        userName: snapshot.header.userName || user?.username || "",
      });
      setApprovalsText(snapshot.approvals.join("\n"));
      setBom1Text(stringifyMap(snapshot.bom1Values));
      setBom3Text(stringifyMap(snapshot.bom3Values));
      setLinesText(JSON.stringify(snapshot.lines, null, 2));
      setPreview({
        filePoNo: snapshot.header.filePoNo,
        customer: snapshot.header.customer,
        lineCount: snapshot.lines.length,
        totalKg: round4(snapshot.lines.reduce((sum, line) => sum + (line.totalKg ?? 0), 0)),
        approvals: snapshot.approvals,
        warnings: [],
        lines: snapshot.lines,
      });
      if (options?.syncSearch !== false) {
        void navigate({
          to: "/bom/create",
          search: { filePoNo: normalizedFilePoNo },
          replace: true,
        });
      }
      toast.success(options?.successMessage ?? "Loaded BOM editor snapshot.");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to load BOM.");
    } finally {
      setLoadingSnapshot(false);
    }
  }

  async function handleLoadExisting() {
    await loadSnapshot(header.filePoNo, { syncSearch: true });
  }

  useEffect(() => {
    if (!search.filePoNo || search.filePoNo === header.filePoNo) return;
    setHeader((current) => ({ ...current, filePoNo: search.filePoNo }));
    void loadSnapshot(search.filePoNo, {
      syncSearch: false,
      successMessage: `Loaded ${search.filePoNo} into the editor.`,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search.filePoNo]);

  async function handlePreview(options?: { showSuccessToast?: boolean }) {
    setPreviewing(true);
    try {
      const payload = buildRequest(header, approvalsText, bom1Text, bom3Text, linesText, user?.username);
      const result = await previewBom(payload);
      setPreview(result);
      if (options?.showSuccessToast !== false) {
        toast.success(`Preview ready with ${result.lineCount} line${result.lineCount === 1 ? "" : "s"}.`);
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Preview failed.");
    } finally {
      setPreviewing(false);
    }
  }

  async function handleSave(mode: "create" | "update") {
    setSaving(mode);
    try {
      const payload = buildRequest(header, approvalsText, bom1Text, bom3Text, linesText, user?.username);
      const result =
        mode === "create"
          ? await createBom(payload)
          : await updateBom(payload.header.filePoNo, payload);

      toast.success(
        mode === "create"
          ? `BOM created for ${result.filePoNo}.`
          : `BOM updated for ${result.filePoNo}.`,
      );
      await handlePreview({ showSuccessToast: false });
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : `Failed to ${mode} BOM.`);
    } finally {
      setSaving(null);
    }
  }

  function resetForm() {
    setHeader(emptyHeader(user?.username));
    setApprovalsText("Marketing");
    setBom1Text("");
    setBom3Text("");
    setLinesText("");
    setPreview(null);
    void navigate({ to: "/bom/create", search: {}, replace: true });
  }

  return (
    <BomPageShell>
      <BomPageHeader
        title="BOM Creation"
        description="Create, preview, or update BOMs using the new backend contract. Keep the raw Bom1/Bom3 values close to ERP field names so server-side derivation can fill the rows."
        backTo="/bom"
        actions={
          <>
            <Badge variant="outline">{stats.approvalCount} approvals</Badge>
            <Badge variant="outline">{stats.bom1Count} Bom1 keys</Badge>
            <Badge variant="outline">{stats.bom3Count} Bom3 keys</Badge>
          </>
        }
      />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_420px]">
        <div className="space-y-5">
          <BomPanel title="Header" subtitle="Core BOM fields stored in BOM1.">
            <div className="grid gap-4 p-4 md:grid-cols-2 md:p-5 xl:grid-cols-3">
              <Field label="Quotation No">
                <Input value={header.filePoNo} onChange={(e) => setHeaderField("filePoNo", e.target.value)} />
              </Field>
              <Field label="Customer">
                <Input value={header.customer} onChange={(e) => setHeaderField("customer", e.target.value)} />
              </Field>
              <Field label="Date">
                <Input type="date" value={header.sysDate ?? ""} onChange={(e) => setHeaderField("sysDate", e.target.value)} />
              </Field>
              <Field label="Print type">
                <Input value={header.printType} onChange={(e) => setHeaderField("printType", e.target.value)} />
              </Field>
              <Field label="PO No">
                <Input value={header.poNo} onChange={(e) => setHeaderField("poNo", e.target.value)} />
              </Field>
              <Field label="PO Nos">
                <Input value={header.poNos} onChange={(e) => setHeaderField("poNos", e.target.value)} />
              </Field>
              <Field label="Bag type">
                <Input value={header.bagType} onChange={(e) => setHeaderField("bagType", e.target.value)} placeholder="UPanel/Non-Builder/Std" />
              </Field>
              <Field label="Size L">
                <Input value={toInputValue(header.sizeL)} onChange={(e) => setHeaderField("sizeL", parseOptionalNumber(e.target.value))} />
              </Field>
              <Field label="Size W">
                <Input value={toInputValue(header.sizeW)} onChange={(e) => setHeaderField("sizeW", parseOptionalNumber(e.target.value))} />
              </Field>
              <Field label="Size H">
                <Input value={toInputValue(header.sizeH)} onChange={(e) => setHeaderField("sizeH", parseOptionalNumber(e.target.value))} />
              </Field>
              <Field label="Size type">
                <Input value={header.sizeType} onChange={(e) => setHeaderField("sizeType", e.target.value)} />
              </Field>
              <Field label="SWL">
                <Input value={header.swl} onChange={(e) => setHeaderField("swl", e.target.value)} />
              </Field>
              <Field label="SF ratio">
                <Input value={header.sfRatio} onChange={(e) => setHeaderField("sfRatio", e.target.value)} />
              </Field>
              <Field label="Qty">
                <Input value={header.qty} onChange={(e) => setHeaderField("qty", e.target.value)} />
              </Field>
              <Field label="Qty unit">
                <Input value={header.qtyUnit} onChange={(e) => setHeaderField("qtyUnit", e.target.value)} />
              </Field>
              <Field label="Top spout type">
                <Input value={header.fsType} onChange={(e) => setHeaderField("fsType", e.target.value)} />
              </Field>
              <Field label="Bottom type">
                <Input value={header.dsType} onChange={(e) => setHeaderField("dsType", e.target.value)} />
              </Field>
              <Field label="Bottom type 1">
                <Input value={header.dsType1} onChange={(e) => setHeaderField("dsType1", e.target.value)} />
              </Field>
              <Field label="Bottom type 2">
                <Input value={header.dsType2} onChange={(e) => setHeaderField("dsType2", e.target.value)} />
              </Field>
              <Field label="Loop type">
                <Input value={header.loopType} onChange={(e) => setHeaderField("loopType", e.target.value)} />
              </Field>
              <Field label="Fabric color">
                <Input value={header.fabColor} onChange={(e) => setHeaderField("fabColor", e.target.value)} />
              </Field>
              <Field label="Ref no">
                <Input value={header.refNo} onChange={(e) => setHeaderField("refNo", e.target.value)} />
              </Field>
              <Field label="Doc">
                <Input value={header.doc} onChange={(e) => setHeaderField("doc", e.target.value)} />
              </Field>
              <Field label="Doc1">
                <Input value={header.doc1} onChange={(e) => setHeaderField("doc1", e.target.value)} />
              </Field>
              <Field label="Doc2">
                <Input value={header.doc2} onChange={(e) => setHeaderField("doc2", e.target.value)} />
              </Field>
              <Field label="Doc unit">
                <Input value={header.docUnit} onChange={(e) => setHeaderField("docUnit", e.target.value)} />
              </Field>
              <Field label="Doc number">
                <Input value={header.docNumber} onChange={(e) => setHeaderField("docNumber", e.target.value)} />
              </Field>
              <Field label="Knot type">
                <Input value={header.knotType} onChange={(e) => setHeaderField("knotType", e.target.value)} />
              </Field>
              <Field label="RP fabric">
                <Input value={header.rpFabric} onChange={(e) => setHeaderField("rpFabric", e.target.value)} />
              </Field>
              <Field label="User name">
                <Input value={header.userName} onChange={(e) => setHeaderField("userName", e.target.value)} />
              </Field>
              <label className="flex items-center gap-2 rounded-xl border border-border/60 bg-muted/20 px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={header.isDropLoop}
                  onChange={(e) => setHeaderField("isDropLoop", e.target.checked)}
                />
                Drop loop
              </label>
            </div>
            <div className="grid gap-4 border-t border-border/60 p-4 md:grid-cols-2 md:p-5">
              <Field label="Instruction">
                <Textarea value={header.instruction} onChange={(e) => setHeaderField("instruction", e.target.value)} rows={5} />
              </Field>
              <div className="grid gap-4">
                <Field label="Body remarks">
                  <Textarea value={header.bodyRemarks} onChange={(e) => setHeaderField("bodyRemarks", e.target.value)} rows={2} />
                </Field>
                <Field label="Printing remarks">
                  <Textarea value={header.printingRemarks} onChange={(e) => setHeaderField("printingRemarks", e.target.value)} rows={2} />
                </Field>
              </div>
            </div>
          </BomPanel>

          <BomPanel title="Approvals" subtitle="One value per line or comma-separated.">
            <div className="p-4 md:p-5">
              <Textarea
                value={approvalsText}
                onChange={(e) => setApprovalsValue(e.target.value)}
                rows={4}
                placeholder={"Marketing\nProduction\nQuality"}
              />
            </div>
          </BomPanel>

          <BomPanel title="Common Inputs" subtitle="Friendly fields for the usual BOM values. These write the Bom1/Bom3 keys for you.">
            <div className="grid gap-4 p-4 md:grid-cols-2 md:p-5">
              <div className="space-y-4">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Bom1 common keys</p>
                <div className="grid gap-3 md:grid-cols-2">
                  {COMMON_BOM1_FIELDS.map((field) => (
                    <Field key={field.key} label={`${field.label} · ${field.key}`}>
                      <Input
                        value={getKeyValueFromText(bom1Text, field.key)}
                        onChange={(e) => setBom1KeyValue(field.key, e.target.value)}
                      />
                    </Field>
                  ))}
                </div>
              </div>

              <div className="space-y-4">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Bom3 common keys</p>
                <div className="grid gap-3 md:grid-cols-2">
                  {COMMON_BOM3_FIELDS.map((field) => (
                    <Field key={field.key} label={`${field.label} · ${field.key}`}>
                      <Input
                        value={getKeyValueFromText(bom3Text, field.key)}
                        onChange={(e) => setBom3KeyValue(field.key, e.target.value)}
                      />
                    </Field>
                  ))}
                </div>
              </div>
            </div>
          </BomPanel>

          <BomPanel title="ERP Raw Values" subtitle="Use key=value lines. Blank lines and # comments are ignored.">
            <div className="grid gap-4 p-4 md:grid-cols-2 md:p-5">
              <Field label="Bom1 values">
                <Textarea
                  value={bom1Text}
                  onChange={(e) => setBom1ValueText(e.target.value)}
                  rows={18}
                  className="font-mono text-xs"
                  placeholder={BOM1_PLACEHOLDER}
                />
              </Field>
              <Field label="Bom3 values">
                <Textarea
                  value={bom3Text}
                  onChange={(e) => setBom3ValueText(e.target.value)}
                  rows={18}
                  className="font-mono text-xs"
                  placeholder={BOM3_PLACEHOLDER}
                />
              </Field>
            </div>
          </BomPanel>

          <BomPanel title="Explicit Lines Override" subtitle="Optional JSON array. Leave empty to let the backend derive rows from Bom1/Bom3 values.">
            <div className="p-4 md:p-5">
              <Textarea
                value={linesText}
                onChange={(e) => setLinesValueText(e.target.value)}
                rows={16}
                className="font-mono text-xs"
                placeholder={LINES_PLACEHOLDER}
              />
            </div>
          </BomPanel>
        </div>

        <div className="space-y-5">
          <BomPanel title="Actions" className="xl:sticky xl:top-6">
            <div className="space-y-4 p-4 md:p-5">
              <p className="text-sm text-muted-foreground">
                Load an existing quotation to edit it, or keep the form blank and create a new BOM.
              </p>
              <div className="grid gap-2">
                <Button type="button" variant="outline" onClick={handleLoadExisting} disabled={loadingSnapshot}>
                  {loadingSnapshot ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCcw className="h-4 w-4" />}
                  Load existing quotation
                </Button>
                <Button type="button" onClick={handlePreview} disabled={previewing || saving !== null}>
                  {previewing ? <Loader2 className="h-4 w-4 animate-spin" /> : <FilePlus2 className="h-4 w-4" />}
                  Preview derived BOM
                </Button>
                <Button type="button" variant={canPersist ? "default" : "secondary"} onClick={() => void handleSave("create")} disabled={!canPersist}>
                  {saving === "create" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Create BOM
                </Button>
                <Button type="button" variant={canPersist ? "outline" : "secondary"} onClick={() => void handleSave("update")} disabled={!canPersist}>
                  {saving === "update" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Update BOM
                </Button>
                <Button type="button" variant="ghost" onClick={resetForm} disabled={loadingSnapshot || previewing || saving !== null}>
                  Reset form
                </Button>
              </div>
              <div className="rounded-xl border border-border/60 bg-muted/20 p-3 text-xs text-muted-foreground">
                <p>`Create BOM` uses `POST /api/bom`.</p>
                <p>`Update BOM` uses `PUT /api/bom/{`{filePoNo}`}`.</p>
                <p>`Preview derived BOM` uses `POST /api/bom/preview`.</p>
              </div>
            </div>
          </BomPanel>

          <BomPanel
            title="Preview"
            subtitle={preview ? `${preview.lineCount} lines · ${preview.totalKg.toFixed(4)} kg` : "Run preview to inspect derived lines before saving."}
          >
            {preview ? (
              <div className="space-y-4 p-4 md:p-5">
                {preview.warnings.length > 0 ? (
                  <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-300">
                    {preview.warnings.map((warning) => (
                      <p key={warning}>{warning}</p>
                    ))}
                  </div>
                ) : null}
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="border-b border-border/60 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
                        <th className="px-2 py-2">Heading</th>
                        <th className="px-2 py-2">GSM</th>
                        <th className="px-2 py-2">Lami</th>
                        <th className="px-2 py-2">Color</th>
                        <th className="px-2 py-2">Fabric</th>
                        <th className="px-2 py-2">Cut</th>
                        <th className="px-2 py-2">Mtr</th>
                        <th className="px-2 py-2">Kg</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/50">
                      {preview.lines.map((line, index) => (
                        <tr key={`${line.heading}-${index}`}>
                          <td className="whitespace-nowrap px-2 py-2 font-medium">{line.heading}</td>
                          <td className="px-2 py-2">{line.gsm || "—"}</td>
                          <td className="px-2 py-2">{line.lami || "—"}</td>
                          <td className="px-2 py-2">{line.color || "—"}</td>
                          <td className="px-2 py-2">{line.fabricSize || "—"}</td>
                          <td className="px-2 py-2">{line.cutSize || "—"}</td>
                          <td className="px-2 py-2">{formatMaybeNumber(line.totalMtr)}</td>
                          <td className="px-2 py-2">{formatMaybeNumber(line.totalKg)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ) : (
              <div className="p-5 text-sm text-muted-foreground">No preview yet.</div>
            )}
          </BomPanel>

          <BomPanel title="Tips">
            <div className="space-y-2 p-4 text-sm text-muted-foreground md:p-5">
              <p>Use `Load existing quotation` first if you want to edit a saved BOM.</p>
              <p>Keep ERP-style key names in Bom1/Bom3 so the backend can derive extra rows like ropes, flaps, tunnel, and inner components.</p>
              <p>Paste explicit line JSON only when you want to override or add rows that are easier to manage directly.</p>
              <p>
                <Link to="/bom" className="text-amber-700 underline underline-offset-4 dark:text-amber-300">
                  Return to BOM report
                </Link>
              </p>
            </div>
          </BomPanel>
        </div>
      </div>
    </BomPageShell>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="space-y-1.5">
      <BomFieldLabel>{label}</BomFieldLabel>
      {children}
    </label>
  );
}

function buildRequest(
  header: BomCreateHeaderInput,
  approvalsText: string,
  bom1Text: string,
  bom3Text: string,
  linesText: string,
  fallbackUserName?: string,
): BomCreateRequest {
  return {
    header: {
      ...header,
      filePoNo: header.filePoNo.trim(),
      customer: header.customer.trim(),
      sysDate: header.sysDate ? new Date(header.sysDate).toISOString() : null,
      userName: (header.userName || fallbackUserName || "").trim(),
    },
    approvals: parseApprovals(approvalsText),
    bom1Values: parseKeyValueText(bom1Text),
    bom3Values: parseKeyValueText(bom3Text),
    lines: parseLinesText(linesText),
  };
}

function parseApprovals(value: string): string[] {
  return value
    .split(/\r?\n|,/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function parseKeyValueText(value: string): Record<string, string | null> {
  const map: Record<string, string | null> = {};
  for (const rawLine of value.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#")) continue;
    const index = line.indexOf("=");
    if (index < 1) {
      throw new Error(`Invalid key=value line: ${rawLine}`);
    }
    const key = line.slice(0, index).trim();
    const entryValue = line.slice(index + 1).trim();
    map[key] = entryValue === "" ? null : entryValue;
  }
  return map;
}

function parseKeyValueTextLenient(value: string): Record<string, string | null> {
  const map: Record<string, string | null> = {};
  for (const rawLine of value.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#")) continue;
    const index = line.indexOf("=");
    if (index < 1) continue;
    const key = line.slice(0, index).trim();
    const entryValue = line.slice(index + 1).trim();
    map[key] = entryValue === "" ? null : entryValue;
  }
  return map;
}

function parseLinesText(value: string): BomCreateLineInput[] {
  if (!value.trim()) return [];

  let parsed: unknown;
  try {
    parsed = JSON.parse(value);
  } catch {
    throw new Error("Explicit lines must be valid JSON.");
  }

  if (!Array.isArray(parsed)) {
    throw new Error("Explicit lines JSON must be an array.");
  }

  return parsed.map((item, index) => normalizeLineInput(item, index));
}

function normalizeLineInput(item: unknown, index: number): BomCreateLineInput {
  if (!item || typeof item !== "object") {
    throw new Error(`Line ${index + 1} must be an object.`);
  }

  const row = item as Record<string, unknown>;
  return {
    sortOrder: Number(row.sortOrder ?? row.SortOrder ?? index + 1),
    heading: String(row.heading ?? row.Heading ?? "").trim(),
    gsm: String(row.gsm ?? row.Gsm ?? ""),
    lami: String(row.lami ?? row.Lami ?? ""),
    color: String(row.color ?? row.Color ?? ""),
    fabricSize: String(row.fabricSize ?? row.FabricSize ?? ""),
    cutSize: String(row.cutSize ?? row.CutSize ?? ""),
    totalMtr: parseLineNumber(row.totalMtr ?? row.TotalMtr),
    totalKg: parseLineNumber(row.totalKg ?? row.TotalKg),
    remarks: String(row.remarks ?? row.Remarks ?? ""),
    gpm: String(row.gpm ?? row.Gpm ?? ""),
  };
}

function parseLineNumber(value: unknown): number | null {
  if (value == null || value === "") return null;
  const number = Number(value);
  if (!Number.isFinite(number)) {
    throw new Error(`Invalid numeric line value: ${String(value)}`);
  }
  return number;
}

function stringifyMap(map: Record<string, string | null>): string {
  return Object.entries(map)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, value]) => `${key}=${value ?? ""}`)
    .join("\n");
}

function getKeyValueFromText(text: string, key: string): string {
  const value = parseKeyValueTextLenient(text)[key];
  return value ?? "";
}

function setKeyValueInText(text: string, key: string, value: string): string {
  const map = parseKeyValueTextLenient(text);
  const trimmedValue = value.trim();
  if (!trimmedValue) {
    delete map[key];
  } else {
    map[key] = trimmedValue;
  }
  return stringifyMap(map);
}

function countPairs(value: string): number {
  return value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith("#"))
    .length;
}

function parseOptionalNumber(value: string): number | null {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function toInputValue(value: number | null | undefined): string {
  return value == null ? "" : String(value);
}

function round4(value: number): number {
  return Math.round(value * 10000) / 10000;
}

function formatMaybeNumber(value: number | null | undefined): string {
  return value == null ? "—" : value.toFixed(4);
}

const BOM1_PLACEHOLDER = `BodyGSM=180
BodyLami=20
SideGSM=180
SideLami=10
LoopGSM=45
LoopL=7
LoopW=70
loopRemarks=4
FSTieGSM=6
FSL=35
FSW=40`;

const BOM3_PLACEHOLDER = `toptypes=Top Spout
bottomtypes=Bottom Spout/Simple
TopSpoutTieNo=2
TopSpoutTieIRISNo=2
BottomSpoutTieNo=1
threadColor=White
threadtype=MF
TunnelDesign=Flexcon`;

const LINES_PLACEHOLDER = `[
  {
    "sortOrder": 1,
    "heading": "Top Flap",
    "gsm": "90 + 15",
    "lami": "Laminated",
    "color": "White",
    "fabricSize": "100",
    "cutSize": "25",
    "totalMtr": 125,
    "totalKg": 0.2875,
    "remarks": "",
    "gpm": ""
  }
]`;

const COMMON_BOM1_FIELDS: Array<{ key: string; label: string }> = [
  { key: "BodyGSM", label: "Body GSM" },
  { key: "BodyLami", label: "Body Lami" },
  { key: "SideGSM", label: "Side GSM" },
  { key: "SideLami", label: "Side Lami" },
  { key: "TopGSM", label: "Top GSM" },
  { key: "TopLami", label: "Top Lami" },
  { key: "FSGSM", label: "Top Spout GSM" },
  { key: "FSLami", label: "Top Spout Lami" },
  { key: "FSL", label: "Top Spout Dia" },
  { key: "FSW", label: "Top Spout Height" },
  { key: "FSTieGSM", label: "Top Tie GSM" },
  { key: "FSTieFabric", label: "Top Tie Fabric" },
  { key: "FSTieRemarks", label: "Top Tie Remarks" },
  { key: "DSGSM", label: "Bottom Spout GSM" },
  { key: "DSLami", label: "Bottom Spout Lami" },
  { key: "DSL", label: "Bottom Spout Dia" },
  { key: "DSW", label: "Bottom Spout Height" },
  { key: "DSTieGSM", label: "Bottom Tie GSM" },
  { key: "LoopGSM", label: "Loop GSM" },
  { key: "LoopL", label: "Loop Width" },
  { key: "LoopW", label: "Loop Length" },
  { key: "loopRemarks", label: "Loop Count" },
  { key: "loopconst", label: "Loop Construction" },
  { key: "LinerGSM", label: "Liner Micron/GSM" },
  { key: "LinerL", label: "Liner Width" },
  { key: "DocGSM", label: "Doc Micron" },
  { key: "docl", label: "Doc Length" },
  { key: "docw", label: "Doc Width" },
];

const COMMON_BOM3_FIELDS: Array<{ key: string; label: string }> = [
  { key: "toptypes", label: "Top Type" },
  { key: "bottomtypes", label: "Bottom Type" },
  { key: "TopSpoutTieNo", label: "Top Spout Tie No" },
  { key: "TopSpoutTieIRISNo", label: "Top IRIS Tie No" },
  { key: "BottomSpoutTieNo", label: "Bottom Spout Tie No" },
  { key: "BottomSpoutTieIRISNo", label: "Bottom IRIS Tie No" },
  { key: "threadColor", label: "Thread Color" },
  { key: "threadtype", label: "Thread Type" },
  { key: "TunnelDesign", label: "Tunnel Design" },
  { key: "DoubleFoldBody", label: "Double Fold Body" },
  { key: "DoubleFoldTop", label: "Double Fold Top" },
  { key: "DoubleFoldBottom", label: "Double Fold Bottom" },
  { key: "TillTheBottom", label: "Loop Till Bottom" },
  { key: "fillercordtop", label: "Filler Cord Top" },
  { key: "fillercordbottom", label: "Filler Cord Bottom" },
  { key: "fillercordtopspout", label: "Filler Cord Top Spout" },
  { key: "fillercordbottomspout", label: "Filler Cord Bottom Spout" },
  { key: "fillercordbody", label: "Filler Cord Body" },
];
