import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import {
  ArrowRight,
  CheckCircle2,
  Database,
  FilePlus2,
  FileText,
  Layers,
  Loader2,
  Package,
  RefreshCcw,
  Ruler,
  Save,
  Settings2,
  Sparkles,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth-context";
import { bomPreviewPdfUrl, createBom, fetchBomEditor, previewBom, updateBom } from "@/lib/bom-api";
import type {
  BomCreateHeaderInput,
  BomCreateLineInput,
  BomCreatePreviewResult,
  BomCreateRequest,
} from "@/lib/bom-types";
import { BomFieldLabel, BomPageHeader, BomPageShell } from "@/components/bom/bom-ui";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";

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
  const bom1Draft = useMemo(() => parseKeyValueTextLenient(bom1Text), [bom1Text]);
  const bom3Draft = useMemo(() => parseKeyValueTextLenient(bom3Text), [bom3Text]);

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
        previewId: "",
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
    <BomPageShell className="bg-[radial-gradient(circle_at_top,_rgba(245,158,11,0.12),_transparent_32%),linear-gradient(180deg,rgba(15,23,42,0.18),transparent_40%)]">
      <BomPageHeader
        title="BOM Creation"
        description="A guided workspace for building BOMs without drowning users in ERP field noise. Fill the essentials, tune the components, then preview and save."
        backTo="/bom"
      />

      <div className="mb-6 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <MiniStat
          icon={Package}
          label="Quotation"
          value={header.filePoNo || "Start a new BOM"}
          tone="amber"
        />
        <MiniStat
          icon={Ruler}
          label="Bag profile"
          value={
            header.bagType
              ? `${header.bagType}${header.sizeL && header.sizeW && header.sizeH ? ` · ${header.sizeL} × ${header.sizeW} × ${header.sizeH}` : ""}`
              : "Bag type and dimensions"
          }
        />
        <MiniStat
          icon={Layers}
          label="Data richness"
          value={`${stats.bom1Count} Bom1 · ${stats.bom3Count} Bom3`}
        />
        <MiniStat
          icon={CheckCircle2}
          label="Save status"
          value={preview ? "Ready to create or update" : "Preview required before save"}
          tone={preview ? "emerald" : "slate"}
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="space-y-5">
          <Tabs defaultValue="basics" className="space-y-4">
            <div className="sticky top-3 z-10 rounded-2xl border border-border/70 bg-background/85 p-2 shadow-sm backdrop-blur">
              <TabsList className="grid h-auto w-full grid-cols-1 gap-2 bg-transparent p-0 md:grid-cols-3">
                <TabsTrigger value="basics" className="min-w-0 h-auto justify-start rounded-xl border border-border/60 px-4 py-3 data-[state=active]:border-amber-500/30 data-[state=active]:bg-amber-500/10">
                  <div className="flex min-w-0 items-center gap-3 text-left">
                    <Package className="h-4 w-4 text-amber-600 dark:text-amber-300" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold">Basics</p>
                      <p className="whitespace-normal break-words text-xs text-muted-foreground">Quotation, bag profile, approvals</p>
                    </div>
                  </div>
                </TabsTrigger>
                <TabsTrigger value="components" className="min-w-0 h-auto justify-start rounded-xl border border-border/60 px-4 py-3 data-[state=active]:border-amber-500/30 data-[state=active]:bg-amber-500/10">
                  <div className="flex min-w-0 items-center gap-3 text-left">
                    <Layers className="h-4 w-4 text-amber-600 dark:text-amber-300" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold">Components</p>
                      <p className="whitespace-normal break-words text-xs text-muted-foreground">Fabric, spouts, ties, liner, docs</p>
                    </div>
                  </div>
                </TabsTrigger>
                <TabsTrigger value="advanced" className="min-w-0 h-auto justify-start rounded-xl border border-border/60 px-4 py-3 data-[state=active]:border-amber-500/30 data-[state=active]:bg-amber-500/10">
                  <div className="flex min-w-0 items-center gap-3 text-left">
                    <Database className="h-4 w-4 text-amber-600 dark:text-amber-300" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold">Advanced</p>
                      <p className="whitespace-normal break-words text-xs text-muted-foreground">Raw ERP values and manual overrides</p>
                    </div>
                  </div>
                </TabsTrigger>
              </TabsList>
            </div>

            <TabsContent value="basics" className="space-y-5">
              <StudioCard
                icon={Package}
                title="Identity & Bag Profile"
                subtitle="Capture the business identity first, then define the bag dimensions and commercial context."
              >
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                  <Field label="Quotation No">
                    <Input value={header.filePoNo} onChange={(e) => setHeaderField("filePoNo", e.target.value)} />
                  </Field>
                  <Field label="Customer">
                    <Input value={header.customer} onChange={(e) => setHeaderField("customer", e.target.value)} />
                  </Field>
                  <Field label="Date">
                    <Input type="date" value={header.sysDate ?? ""} onChange={(e) => setHeaderField("sysDate", e.target.value)} />
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
                  <Field label="Fabric color">
                    <Input value={header.fabColor} onChange={(e) => setHeaderField("fabColor", e.target.value)} />
                  </Field>
                </div>
              </StudioCard>

              <div className="grid gap-5 lg:grid-cols-2">
                <StudioCard
                  icon={Ruler}
                  title="Commercial Setup"
                  subtitle="These values drive preview totals and downstream PDF metadata."
                >
                  <div className="grid gap-4 md:grid-cols-2">
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
                    <Field label="Print type">
                      <Input value={header.printType} onChange={(e) => setHeaderField("printType", e.target.value)} />
                    </Field>
                    <Field label="User name">
                      <Input value={header.userName} onChange={(e) => setHeaderField("userName", e.target.value)} />
                    </Field>
                    <label className="flex items-center gap-2 rounded-xl border border-border/60 bg-background/60 px-3 py-2 text-sm">
                      <input
                        type="checkbox"
                        checked={header.isDropLoop}
                        onChange={(e) => setHeaderField("isDropLoop", e.target.checked)}
                      />
                      Drop loop
                    </label>
                  </div>
                </StudioCard>

                <StudioCard
                  icon={FileText}
                  title="References & Approvals"
                  subtitle="Reference numbers, pouch specs, and sign-off routing for the final BOM."
                >
                  <div className="grid gap-4 md:grid-cols-2">
                    <Field label="PO No">
                      <Input value={header.poNo} onChange={(e) => setHeaderField("poNo", e.target.value)} />
                    </Field>
                    <Field label="PO Nos">
                      <Input value={header.poNos} onChange={(e) => setHeaderField("poNos", e.target.value)} />
                    </Field>
                    <Field label="Ref no">
                      <Input value={header.refNo} onChange={(e) => setHeaderField("refNo", e.target.value)} />
                    </Field>
                    <Field label="Knot type">
                      <Input value={header.knotType} onChange={(e) => setHeaderField("knotType", e.target.value)} />
                    </Field>
                    <Field label="RP fabric">
                      <Input value={header.rpFabric} onChange={(e) => setHeaderField("rpFabric", e.target.value)} />
                    </Field>
                    <Field label="Approvals">
                      <Textarea
                        value={approvalsText}
                        onChange={(e) => setApprovalsValue(e.target.value)}
                        rows={4}
                        placeholder={"Marketing\nProduction\nQuality"}
                      />
                    </Field>
                  </div>
                </StudioCard>
              </div>

              <StudioCard
                icon={Settings2}
                title="Notes"
                subtitle="Use these notes for BOM context and instructions that should follow the item through review."
              >
                <div className="grid gap-4 md:grid-cols-2">
                  <Field label="Instruction">
                    <Textarea value={header.instruction} onChange={(e) => setHeaderField("instruction", e.target.value)} rows={6} />
                  </Field>
                  <div className="grid gap-4">
                    <Field label="Body remarks">
                      <Textarea value={header.bodyRemarks} onChange={(e) => setHeaderField("bodyRemarks", e.target.value)} rows={3} />
                    </Field>
                    <Field label="Printing remarks">
                      <Textarea value={header.printingRemarks} onChange={(e) => setHeaderField("printingRemarks", e.target.value)} rows={3} />
                    </Field>
                  </div>
                </div>
              </StudioCard>
            </TabsContent>

            <TabsContent value="components" className="space-y-5">
              <div className="grid gap-5 xl:grid-cols-2">
                <StudioCard
                  icon={Layers}
                  title="Core Fabric"
                  subtitle="The structural fabric settings that drive body, side, top, loop, liner, and document calculations."
                >
                  <div className="grid gap-3 md:grid-cols-2">
                    {COMMON_BOM1_FIELDS.filter((field) => CORE_BOM1_KEYS.has(field.key)).map((field) => (
                      <Field key={field.key} label={field.label}>
                        <Input
                          value={bom1Draft[field.key] ?? ""}
                          onChange={(e) => setBom1KeyValue(field.key, e.target.value)}
                        />
                      </Field>
                    ))}
                  </div>
                </StudioCard>

                <StudioCard
                  icon={Sparkles}
                  title="Spouts, Ties & Behavior"
                  subtitle="Friendly controls for spout/tie counts and the logic switches that influence derived rows."
                >
                  <div className="grid gap-3 md:grid-cols-2">
                    {COMMON_BOM1_FIELDS.filter((field) => SPOUT_BOM1_KEYS.has(field.key)).map((field) => (
                      <Field key={field.key} label={field.label}>
                        <Input
                          value={bom1Draft[field.key] ?? ""}
                          onChange={(e) => setBom1KeyValue(field.key, e.target.value)}
                        />
                      </Field>
                    ))}
                    {COMMON_BOM3_FIELDS.map((field) => (
                      <Field key={field.key} label={field.label}>
                        <Input
                          value={bom3Draft[field.key] ?? ""}
                          onChange={(e) => setBom3KeyValue(field.key, e.target.value)}
                        />
                      </Field>
                    ))}
                  </div>
                </StudioCard>
              </div>

              <div className="grid gap-5 xl:grid-cols-2">
                <StudioCard
                  icon={FileText}
                  title="Document & Reference Pouches"
                  subtitle="These fields shape the document pouch calculations and what the final PDF will display."
                >
                  <div className="grid gap-4 md:grid-cols-2">
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
                    {COMMON_BOM1_FIELDS.filter((field) => DOC_BOM1_KEYS.has(field.key)).map((field) => (
                      <Field key={field.key} label={field.label}>
                        <Input
                          value={bom1Draft[field.key] ?? ""}
                          onChange={(e) => setBom1KeyValue(field.key, e.target.value)}
                        />
                      </Field>
                    ))}
                  </div>
                </StudioCard>

                <StudioCard
                  icon={Settings2}
                  title="Styling Principles"
                  subtitle="Preview will only show what’s relevant. Use advanced raw values later for uncommon ERP edge cases."
                >
                  <div className="grid gap-3 sm:grid-cols-2">
                    <HintTile
                      title="Friendly first"
                      text="Users fill meaningful labels like Body GSM and Top Spout Dia instead of memorizing ERP keys."
                    />
                    <HintTile
                      title="Raw still available"
                      text="Every friendly field writes through to the Bom1/Bom3 dictionaries, so compatibility stays intact."
                    />
                    <HintTile
                      title="Preview gated save"
                      text="Create and update stay disabled until the current draft has been previewed."
                    />
                    <HintTile
                      title="Advanced only when needed"
                      text="Use the Advanced tab for rare line overrides, debugging, or power-user ERP tweaks."
                    />
                  </div>
                </StudioCard>
              </div>
            </TabsContent>

            <TabsContent value="advanced" className="space-y-5">
              <StudioCard
                icon={Database}
                title="Advanced ERP Surface"
                subtitle="Everything below is still available, but it no longer clutters the main editing flow."
              >
                <Accordion type="multiple" defaultValue={["raw", "manual"]} className="space-y-3">
                  <AccordionItem value="raw" className="rounded-2xl border border-border/60 px-4">
                    <AccordionTrigger className="py-4 text-base font-semibold hover:no-underline">
                      Raw Bom1 / Bom3 values
                    </AccordionTrigger>
                    <AccordionContent className="pb-4">
                      <div className="grid gap-4 md:grid-cols-2">
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
                    </AccordionContent>
                  </AccordionItem>

                  <AccordionItem value="manual" className="rounded-2xl border border-border/60 px-4">
                    <AccordionTrigger className="py-4 text-base font-semibold hover:no-underline">
                      Explicit line override JSON
                    </AccordionTrigger>
                    <AccordionContent className="pb-4">
                      <Field label="Explicit lines override">
                        <Textarea
                          value={linesText}
                          onChange={(e) => setLinesValueText(e.target.value)}
                          rows={16}
                          className="font-mono text-xs"
                          placeholder={LINES_PLACEHOLDER}
                        />
                      </Field>
                    </AccordionContent>
                  </AccordionItem>
                </Accordion>
              </StudioCard>
            </TabsContent>
          </Tabs>
        </div>

        <div className="space-y-5">
          <div className="xl:sticky xl:top-6">
            <div className="space-y-5">
              <StudioCard
                icon={Sparkles}
                title="Review & Actions"
                subtitle="A focused control tower for loading, previewing, and saving the current draft."
                className="border-amber-500/20 bg-gradient-to-br from-amber-500/[0.08] via-card/95 to-card/90"
              >
                <div className="space-y-4">
                  <div className="rounded-2xl border border-border/60 bg-background/70 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold">Current state</p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {preview
                            ? "Preview is current. You can create or update this BOM now."
                            : "Make your edits, then run Preview derived BOM to unlock save actions."}
                        </p>
                      </div>
                      <Badge
                        variant="secondary"
                        className={cn(
                          "border",
                          preview
                            ? "border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300"
                            : "border-border/60 bg-muted/40 text-muted-foreground",
                        )}
                      >
                        {preview ? "Ready" : "Preview needed"}
                      </Badge>
                    </div>
                  </div>

                  <div className="grid gap-2">
                    <Button type="button" variant="outline" onClick={handleLoadExisting} disabled={loadingSnapshot}>
                      {loadingSnapshot ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCcw className="h-4 w-4" />}
                      Load existing quotation
                    </Button>
                    <Button type="button" onClick={handlePreview} disabled={previewing || saving !== null}>
                      {previewing ? <Loader2 className="h-4 w-4 animate-spin" /> : <FilePlus2 className="h-4 w-4" />}
                      Preview derived BOM
                    </Button>
                    <Button
                      type="button"
                      variant={canPersist ? "default" : "secondary"}
                      className={cn(canPersist && "shadow-lg shadow-amber-500/20")}
                      onClick={() => void handleSave("create")}
                      disabled={!canPersist}
                    >
                      {saving === "create" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                      Create BOM
                    </Button>
                    <Button type="button" variant="outline" onClick={() => void handleSave("update")} disabled={!canPersist}>
                      {saving === "update" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                      Update BOM
                    </Button>
                    {preview?.previewId ? (
                      <>
                        <Button type="button" variant="outline" asChild>
                          <Link to="/bom/$" params={{ _splat: "preview" }} search={{ previewId: preview.previewId }} target="_blank">
                            <ArrowRight className="h-4 w-4" />
                            Open report preview
                          </Link>
                        </Button>
                        <Button type="button" variant="outline" asChild>
                          <a href={bomPreviewPdfUrl(preview.previewId)} target="_blank" rel="noreferrer">
                            <FileText className="h-4 w-4" />
                            Open PDF preview
                          </a>
                        </Button>
                      </>
                    ) : null}
                    <Button type="button" variant="ghost" onClick={resetForm} disabled={loadingSnapshot || previewing || saving !== null}>
                      Reset form
                    </Button>
                  </div>

                  <div className="grid gap-2 sm:grid-cols-2">
                    <HintTile title="1. Basics" text="Quotation, bag type, size, and approvals." />
                    <HintTile title="2. Components" text="Friendly material fields that map raw keys for you." />
                    <HintTile title="3. Advanced" text="Raw Bom1/Bom3 and explicit line overrides stay tucked away." />
                    <HintTile title="4. Review" text="Preview first, then create or update with confidence." />
                  </div>
                </div>
              </StudioCard>

              <StudioCard
                icon={FileText}
                title="Preview"
                subtitle={preview ? `${preview.lineCount} lines · ${preview.totalKg.toFixed(4)} kg` : "Run preview to inspect derived lines before saving."}
              >
                {preview ? (
                  <div className="space-y-4">
                    {preview.warnings.length > 0 ? (
                      <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-300">
                        {preview.warnings.map((warning) => (
                          <p key={warning}>{warning}</p>
                        ))}
                      </div>
                    ) : null}
                    <div className="overflow-x-auto rounded-2xl border border-border/60">
                      <table className="min-w-full text-sm">
                        <thead className="bg-muted/30">
                          <tr className="text-left text-[11px] uppercase tracking-wide text-muted-foreground">
                            <th className="px-3 py-2">Heading</th>
                            <th className="px-3 py-2">GSM</th>
                            <th className="px-3 py-2">Lami</th>
                            <th className="px-3 py-2">Color</th>
                            <th className="px-3 py-2">Fabric</th>
                            <th className="px-3 py-2">Cut</th>
                            <th className="px-3 py-2">Mtr</th>
                            <th className="px-3 py-2">Kg</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border/50 bg-background/60">
                          {preview.lines.map((line, index) => (
                            <tr key={`${line.heading}-${index}`} className="hover:bg-muted/20">
                              <td className="whitespace-nowrap px-3 py-2 font-medium">{line.heading}</td>
                              <td className="px-3 py-2">{line.gsm || "—"}</td>
                              <td className="px-3 py-2">{line.lami || "—"}</td>
                              <td className="px-3 py-2">{line.color || "—"}</td>
                              <td className="px-3 py-2">{line.fabricSize || "—"}</td>
                              <td className="px-3 py-2">{line.cutSize || "—"}</td>
                              <td className="px-3 py-2">{formatMaybeNumber(line.totalMtr)}</td>
                              <td className="px-3 py-2">{formatMaybeNumber(line.totalKg)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ) : (
                  <div className="rounded-2xl border border-dashed border-border/70 bg-muted/15 p-5 text-sm text-muted-foreground">
                    No preview yet. Fill the basics, tune the components, then click <span className="font-medium text-foreground">Preview derived BOM</span>.
                  </div>
                )}
              </StudioCard>

              <StudioCard
                icon={Database}
                title="Developer Notes"
                subtitle="A small reminder of what powers the page under the hood."
              >
                <div className="space-y-2 text-sm text-muted-foreground">
                  <p>The modern form writes friendly inputs into the same `Bom1` / `Bom3` key dictionaries used by the backend.</p>
                  <p>The advanced tab remains available for ERP-specific overrides, unusual structures, and manual line injection.</p>
                  <p>
                    <Link to="/bom" className="inline-flex items-center gap-1 text-amber-700 underline underline-offset-4 dark:text-amber-300">
                      Return to BOM report
                      <ArrowRight className="h-3.5 w-3.5" />
                    </Link>
                  </p>
                </div>
              </StudioCard>
            </div>
          </div>
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

function StudioCard({
  icon: Icon,
  title,
  subtitle,
  children,
  className,
}: {
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "overflow-hidden rounded-[28px] border border-border/70 bg-card/85 shadow-[0_18px_50px_-24px_rgba(15,23,42,0.65)] backdrop-blur-sm",
        className,
      )}
    >
      <div className="border-b border-border/60 bg-gradient-to-r from-amber-500/[0.08] via-transparent to-transparent px-5 py-4">
        <div className="flex min-w-0 items-start gap-3">
          <div className="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-amber-500/12 text-amber-600 ring-1 ring-amber-500/20 dark:text-amber-300">
            <Icon className="h-4 w-4" />
          </div>
          <div className="min-w-0">
            <h2 className="text-base font-semibold tracking-tight">{title}</h2>
            {subtitle ? <p className="mt-1 max-w-2xl whitespace-normal break-words text-sm text-muted-foreground">{subtitle}</p> : null}
          </div>
        </div>
      </div>
      <div className="p-5">{children}</div>
    </section>
  );
}

function MiniStat({
  icon: Icon,
  label,
  value,
  tone = "slate",
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  tone?: "slate" | "amber" | "emerald";
}) {
  const toneClass =
    tone === "amber"
      ? "bg-amber-500/10 text-amber-700 ring-amber-500/20 dark:text-amber-300"
      : tone === "emerald"
        ? "bg-emerald-500/10 text-emerald-700 ring-emerald-500/20 dark:text-emerald-300"
        : "bg-muted/50 text-muted-foreground ring-border/60";

  return (
    <div className="rounded-3xl border border-border/70 bg-card/80 p-4 shadow-sm backdrop-blur-sm">
      <div className="flex min-w-0 items-start gap-3">
        <div className={cn("flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl ring-1", toneClass)}>
          <Icon className="h-4 w-4" />
        </div>
        <div className="min-w-0">
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
          <p className="mt-1 break-words text-sm font-medium text-foreground">{value}</p>
        </div>
      </div>
    </div>
  );
}

function HintTile({ title, text }: { title: string; text: string }) {
  return (
    <div className="rounded-2xl border border-border/60 bg-background/55 p-3">
      <p className="text-sm font-semibold text-foreground">{title}</p>
      <p className="mt-1 text-xs leading-relaxed text-muted-foreground">{text}</p>
    </div>
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

const CORE_BOM1_KEYS = new Set([
  "BodyGSM",
  "BodyLami",
  "SideGSM",
  "SideLami",
  "TopGSM",
  "TopLami",
  "LoopGSM",
  "LoopL",
  "LoopW",
  "loopRemarks",
  "loopconst",
  "LinerGSM",
  "LinerL",
]);

const SPOUT_BOM1_KEYS = new Set([
  "FSGSM",
  "FSLami",
  "FSL",
  "FSW",
  "FSTieGSM",
  "FSTieFabric",
  "FSTieRemarks",
  "DSGSM",
  "DSLami",
  "DSL",
  "DSW",
  "DSTieGSM",
]);

const DOC_BOM1_KEYS = new Set(["DocGSM", "docl", "docw"]);

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
