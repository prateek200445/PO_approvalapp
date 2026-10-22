import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Calculator, Copy, Loader2, Plus, Save, Settings2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useAuth } from "@/lib/auth-context";
import {
  createWovenQuote,
  deleteWovenQuote,
  getWovenMasters,
  getWovenQuote,
  getWovenRates,
  listWovenQuotes,
  saveWovenConstants,
  saveWovenCustomer,
  saveWovenLookup,
  saveWovenRate,
  updateWovenQuote,
  type WovenConstant,
  type WovenDraft,
  type WovenMasters,
  type WovenRate,
  type WovenResult,
} from "@/lib/woven-quote-api";
import { blankWovenDraft, calculateWovenQuote, formatQty, ratesAsOf, showWastagePercent } from "@/lib/woven-quote-calc";

type View = "list" | "edit" | "masters";

export function WovenQuoteScreen() {
  const [view, setView] = useState<View>("list");
  const [quoteId, setQuoteId] = useState<number | null>(null);
  const [listTab, setListTab] = useState<"saved" | "drafts">("saved");
  const leaveEditor = useRef<(() => Promise<"saved" | "draft" | "new" | false>) | null>(null);
  const masters = useQuery({ queryKey: ["woven-masters"], queryFn: getWovenMasters });
  const rates = useQuery({ queryKey: ["woven-rates"], queryFn: getWovenRates });

  async function leaveQuote() {
    if (view !== "edit" || !leaveEditor.current) return "new" as const;
    const outcome = await leaveEditor.current();
    if (outcome === "draft") setListTab("drafts");
    if (outcome === "saved") setListTab("saved");
    return outcome;
  }
  async function openNew() {
    if (!(await leaveQuote())) return;
    setQuoteId(null);
    setView("edit");
  }
  function openQuote(id: number) {
    setQuoteId(id);
    setView("edit");
  }
  async function showList() {
    if (!(await leaveQuote())) return;
    setView("list");
  }
  async function showMasters() {
    if (view === "masters") {
      setView("list");
      return;
    }
    if (!(await leaveQuote())) return;
    setView("masters");
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Sales costing</p>
          <h1 className="text-2xl font-semibold tracking-tight">Woven bag quotation</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Enter the bag. Weight, material, manufacturing cost, and the quotation price follow the woven pricing workbook.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant={view === "masters" ? "default" : "outline"} onClick={() => void showMasters()}>
            <Settings2 /> Masters
          </Button>
          <Button onClick={() => void openNew()}>
            <Plus /> New quote
          </Button>
        </div>
      </div>

      {masters.isError || rates.isError ? (
        <p className="rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm">
          The quotation masters could not be loaded. Check that the API is running, then refresh.
        </p>
      ) : null}

      {view === "list" ? <QuoteList tab={listTab} onTabChange={setListTab} onOpen={openQuote} /> : null}
      {view === "edit" && (!masters.data || !rates.data) ? <p className="text-sm text-muted-foreground">Loading rates and masters…</p> : null}
      {view === "edit" && masters.data && rates.data ? (
        <QuoteEditor
          quoteId={quoteId}
          masters={masters.data}
          rates={rates.data}
          onClose={() => void showList()}
          onSaved={(id) => setQuoteId(id)}
          onRegisterLeave={(leave) => { leaveEditor.current = leave; }}
        />
      ) : null}
      {view === "masters" && masters.data ? <MastersPanel masters={masters.data} rates={rates.data ?? []} /> : null}
    </div>
  );
}

const PAGE_SIZE = 100;

type QuoteFilters = {
  sector: string;
  customer: string;
  ourRef: string;
  customerRef: string;
  productType: string;
  terms: string;
  currency: string;
  from: string;
  to: string;
  minWeight: string;
  maxWeight: string;
  minPrice: string;
  maxPrice: string;
};

const emptyQuoteFilters = (): QuoteFilters => ({
  sector: "",
  customer: "",
  ourRef: "",
  customerRef: "",
  productType: "",
  terms: "",
  currency: "",
  from: "",
  to: "",
  minWeight: "",
  maxWeight: "",
  minPrice: "",
  maxPrice: "",
});

function QuoteList({
  tab,
  onTabChange,
  onOpen,
}: {
  tab: "saved" | "drafts";
  onTabChange: (tab: "saved" | "drafts") => void;
  onOpen: (id: number) => void;
}) {
  const [filters, setFilters] = useState<QuoteFilters>(emptyQuoteFilters);
  const [applied, setApplied] = useState<QuoteFilters>(emptyQuoteFilters);
  const [dateOrder, setDateOrder] = useState<"desc" | "asc">("desc");
  const [page, setPage] = useState(1);
  const setFilter = (key: keyof QuoteFilters, value: string) =>
    setFilters((current) => ({ ...current, [key]: value }));
  const query = useQuery({
    queryKey: ["woven-quotes", tab, page, dateOrder, applied],
    queryFn: () => {
      const params = new URLSearchParams();
      params.set("page", String(page));
      params.set("pageSize", String(PAGE_SIZE));
      params.set("dateOrder", dateOrder);
      params.set("status", tab === "drafts" ? "draft" : "saved");
      Object.entries(applied).forEach(([key, value]) => {
        if (value) params.set(key, value);
      });
      return listWovenQuotes(`?${params.toString()}`);
    },
  });

  const rows = query.data?.items ?? [];
  const total = query.data?.total ?? 0;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <section className="card-3d space-y-4 rounded-xl p-4 md:p-5">
      <Tabs value={tab} onValueChange={(value) => { onTabChange(value === "drafts" ? "drafts" : "saved"); setPage(1); }}>
        <TabsList>
          <TabsTrigger value="saved">Saved</TabsTrigger>
          <TabsTrigger value="drafts">Drafts</TabsTrigger>
        </TabsList>
      </Tabs>
      <div className="grid gap-3 md:grid-cols-4">
        <Select label="Sector" value={filters.sector} onChange={(value) => setFilter("sector", value)} options={["", "Export", "Domestic"]} />
        <Field label="Customer" value={filters.customer} onChange={(value) => setFilter("customer", value)} />
        <Field label="Our reference" value={filters.ourRef} onChange={(value) => setFilter("ourRef", value)} />
        <Field label="Customer reference" value={filters.customerRef} onChange={(value) => setFilter("customerRef", value)} />
        <Select label="Type" value={filters.productType} onChange={(value) => setFilter("productType", value)} options={["", "WovenBag", "AdStar"]} />
        <Field label="Terms" value={filters.terms} onChange={(value) => setFilter("terms", value)} />
        <Field label="Currency" value={filters.currency} onChange={(value) => setFilter("currency", value)} />
        <label className="space-y-1 text-xs">
          <span className="text-muted-foreground">Date order</span>
          <select
            className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            value={dateOrder}
            onChange={(event) => { setDateOrder(event.target.value === "asc" ? "asc" : "desc"); setPage(1); }}
          >
            <option value="desc">Newest first</option>
            <option value="asc">Oldest first</option>
          </select>
        </label>
        <Field label="Date from" type="date" value={filters.from} onChange={(value) => setFilter("from", value)} />
        <Field label="Date to" type="date" value={filters.to} onChange={(value) => setFilter("to", value)} />
        <Field label="Minimum weight g" type="number" value={filters.minWeight} onChange={(value) => setFilter("minWeight", value)} />
        <Field label="Maximum weight g" type="number" value={filters.maxWeight} onChange={(value) => setFilter("maxWeight", value)} />
        <Field label="Minimum price" type="number" value={filters.minPrice} onChange={(value) => setFilter("minPrice", value)} />
        <Field label="Maximum price" type="number" value={filters.maxPrice} onChange={(value) => setFilter("maxPrice", value)} />
        <div className="flex items-end gap-2 md:col-span-2">
          <Button onClick={() => { setApplied(filters); setPage(1); }}>Apply filters</Button>
          <Button variant="outline" onClick={() => { const empty = emptyQuoteFilters(); setFilters(empty); setApplied(empty); setPage(1); }}>Clear</Button>
        </div>
      </div>
      {query.isPending || query.isFetching ? (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-5 w-5 animate-spin" />
          Loading quotations…
        </div>
      ) : query.isError ? (
        <p className="py-8 text-center text-sm text-destructive">Quotations could not be loaded.</p>
      ) : (
      <div className="space-y-4">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] text-sm">
          <thead className="text-left text-xs uppercase tracking-wide text-muted-foreground">
            <tr className="border-b border-border">
              <th className="py-2 pr-3 font-medium">Date</th>
              <th className="py-2 pr-3 font-medium">Customer</th>
              <th className="py-2 pr-3 font-medium">Our reference</th>
              <th className="py-2 pr-3 font-medium">Customer reference</th>
              <th className="py-2 pr-3 font-medium">Status</th>
              <th className="py-2 pr-3 font-medium">Type</th>
              <th className="py-2 pr-3 font-medium">Terms</th>
              <th className="py-2 pr-3 font-medium">Currency</th>
              <th className="py-2 pr-3 text-right font-medium">Weight g</th>
              <th className="py-2 text-right font-medium">Price / pc</th>
              <th className="py-2 pl-3 text-right font-medium"> </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-border/70 hover:bg-muted/40">
                <td className="py-2.5 pr-3">{row.quoteDate.slice(0, 10)}</td>
                <td className="py-2.5 pr-3 font-medium">{row.customerName || "Untitled"}</td>
                <td className="py-2.5 pr-3">{row.ourRef || "—"}</td>
                <td className="py-2.5 pr-3">{row.customerRef || "—"}</td>
                <td className="py-2.5 pr-3">{row.isDraft ? <Badge variant="secondary">Draft</Badge> : <Badge variant="outline">Saved</Badge>}</td>
                <td className="py-2.5 pr-3">{row.productType === "AdStar" ? "AD Star" : "Woven bag"}</td>
                <td className="py-2.5 pr-3">{row.terms || "—"}</td>
                <td className="py-2.5 pr-3">{row.currency || "—"}</td>
                <td className="py-2.5 pr-3 text-right tabular-nums">{formatQty(row.totalWeightGram, 1)}</td>
                <td className="py-2.5 text-right font-semibold tabular-nums">{formatQty(row.pricePerPiece, 4)} {row.currency}</td>
                <td className="py-2.5 pl-3 text-right"><Button size="sm" variant="outline" onClick={() => onOpen(row.id)}>Edit</Button></td>
              </tr>
            ))}
            {rows.length === 0 ? (
              <tr><td colSpan={11} className="py-8 text-center text-muted-foreground">{tab === "drafts" ? "No drafts match these filters." : "No saved quotations match these filters."}</td></tr>
            ) : null}
          </tbody>
        </table>
      </div>
      <div className="flex items-center justify-between gap-3 text-sm">
        <p className="text-muted-foreground">{total === 0 ? (tab === "drafts" ? "0 drafts" : "0 quotations") : `${(page - 1) * PAGE_SIZE + 1}–${Math.min(page * PAGE_SIZE, total)} of ${total}`}</p>
        <div className="flex gap-2">
          <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</Button>
          <Button size="sm" variant="outline" disabled={page >= pages} onClick={() => setPage(page + 1)}>Next</Button>
        </div>
      </div>
      </div>
      )}
    </section>
  );
}

function QuoteEditor({
  quoteId,
  masters,
  rates,
  onClose,
  onSaved,
  onRegisterLeave,
}: {
  quoteId: number | null;
  masters: WovenMasters;
  rates: WovenRate[];
  onClose: () => void;
  onSaved: (id: number) => void;
  onRegisterLeave: (leave: (() => Promise<"saved" | "draft" | "new" | false>) | null) => void;
}) {
  const { user } = useAuth();
  const client = useQueryClient();
  const [draft, setDraft] = useState<WovenDraft>(blankWovenDraft);
  const [loading, setLoading] = useState(quoteId != null);
  const [saving, setSaving] = useState(false);
  const dirty = useRef(false);
  const kindRef = useRef<"saved" | "draft" | "new">(quoteId == null ? "new" : "saved");
  const draftRef = useRef(draft);
  const idRef = useRef(quoteId);
  draftRef.current = draft;
  idRef.current = quoteId;

  useEffect(() => {
    if (quoteId == null) {
      kindRef.current = "new";
      setDraft(blankWovenDraft());
      setLoading(false);
      return;
    }
    let live = true;
    setLoading(true);
    getWovenQuote(quoteId)
      .then((row) => {
        if (!live) return;
        kindRef.current = row.isDraft ? "draft" : "saved";
        setDraft(withPercentWastage(row.draft));
      })
      .catch((error: Error) => toast.error(error.message))
      .finally(() => {
        if (live) setLoading(false);
      });
    return () => {
      live = false;
    };
  }, [quoteId]);

  const result = useMemo(
    () => calculateWovenQuote(draft, ratesAsOf(rates, draft.sector, draft.quoteDate), masters.constants, masters.materials),
    [draft, rates, masters],
  );

  function patch(partial: Partial<WovenDraft>) {
    dirty.current = true;
    setDraft((current) => ({ ...current, ...partial }));
  }

  async function persist(asDraft: boolean, createCopy = false) {
    const current = draftRef.current;
    const id = idRef.current;
    const saved = id == null || createCopy
      ? await createWovenQuote(current, user?.username ?? "", asDraft)
      : await updateWovenQuote(id, current, asDraft);
    idRef.current = saved.id;
    kindRef.current = asDraft ? "draft" : "saved";
    onSaved(saved.id);
    await client.invalidateQueries({ queryKey: ["woven-quotes"] });
    return saved;
  }

  const leaveRef = useRef<() => Promise<"saved" | "draft" | "new" | false>>(async () => "new");
  leaveRef.current = async () => {
    if (!dirty.current) return kindRef.current;
    try {
      await persist(true);
      dirty.current = false;
      toast.success("Draft saved.");
      return "draft";
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Draft could not be saved.");
      return false;
    }
  };

  useEffect(() => {
    onRegisterLeave(() => leaveRef.current());
    return () => onRegisterLeave(null);
    // Register once. leaveRef always points at the latest sheet.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => () => {
    if (dirty.current) void leaveRef.current();
  }, []);

  async function save() {
    setSaving(true);
    try {
      await persist(false);
      dirty.current = false;
      toast.success("Quotation saved.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Save failed.");
    } finally {
      setSaving(false);
    }
  }

  async function saveAsNew() {
    setSaving(true);
    try {
      const saved = await persist(false, true);
      dirty.current = false;
      toast.success(`New quotation #${saved.id} created.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "New quotation could not be created.");
    } finally {
      setSaving(false);
    }
  }

  async function remove() {
    if (quoteId == null) return;
    if (!window.confirm("Delete this quotation?")) return;
    await deleteWovenQuote(quoteId);
    await client.invalidateQueries({ queryKey: ["woven-quotes"] });
    toast.success("Quotation deleted.");
    onClose();
  }

  if (loading) return <p className="text-sm text-muted-foreground">Loading quotation…</p>;

  const options = (category: string) => masters.lookups.filter((row) => row.category === category).map((row) => row.value);
  const customers = masters.customers.filter((row) => row.sector === draft.sector).map((row) => row.shortName || row.fullName);

  return (
    <div className="grid items-start gap-4 pb-20 xl:grid-cols-[minmax(0,1fr)_320px]">
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Button size="sm" variant="outline" onClick={onClose}><ArrowLeft /> Back to list</Button>
            <h2 className="font-semibold">Quote sheet</h2>
          </div>
          <div className="flex gap-2">
            <Button size="sm" variant={draft.productType === "WovenBag" ? "default" : "outline"} onClick={() => patch({ productType: "WovenBag" })}>Woven bag</Button>
            <Button size="sm" variant={draft.productType === "AdStar" ? "default" : "outline"} onClick={() => patch({ productType: "AdStar" })}>AD Star</Button>
          </div>
        </div>
        {draft.productType === "WovenBag" ? (
          <WovenSheet draft={draft} patch={patch} options={options} materials={masters.materials} customers={customers} result={result} />
        ) : (
          <AdStarSpec draft={draft} patch={patch} />
        )}
        {draft.productType === "WovenBag" ? <QuotationTextCard draft={draft} result={result} /> : null}

        <div className="flex flex-wrap gap-2">
          <Button onClick={() => void save()} disabled={saving}><Save /> {saving ? "Saving…" : "Save quotation"}</Button>
          {quoteId != null ? <Button variant="secondary" onClick={() => void saveAsNew()} disabled={saving}><Plus /> Save as new</Button> : null}
          {quoteId != null ? <Button variant="destructive" onClick={() => void remove()}><Trash2 /> Delete</Button> : null}
        </div>
      </div>

      <aside className="card-3d order-first space-y-4 rounded-xl p-4 xl:sticky xl:top-4 xl:order-last">
        <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground"><Calculator className="h-4 w-4" /> Quotation</div>
        <div>
          <p className="text-3xl font-semibold tabular-nums">{formatQty(result.pricePerPiece, 4)}</p>
          <p className="text-sm text-muted-foreground">{result.priceBasis || "per piece"}</p>
        </div>
        <dl className="space-y-2 text-sm">
          <Summary label="Selling weight" value={`${formatQty(result.totalComponentGram, 2)} g`} />
          <Summary label="Fabric weight" value={`${formatQty(result.fabricGram, 2)} g`} />
          <Summary label="Fabric GSM" value={formatQty(result.fabricGsmCalculated, 2)} />
          <Summary label="Raw material" value={`${formatQty(result.totalRmcPerKg, 2)} Rs/kg`} />
          <Summary label="Manufacturing" value={`${formatQty(result.costPerPiece, 2)} Rs/pc`} />
          <Summary label="Quote" value={`${formatQty(result.quotePerKg, 2)} Rs/kg`} />
          <Summary label="FOB" value={`${formatQty(result.fobUsdPerKg, 4)} USD/kg`} />
          {draft.productType === "WovenBag" ? (
            <>
              <Summary label="Coating wastage" value={`${formatQty(lineValue(result.lines, "Coating wastage"), 2)} %`} />
              <Summary label="Coating realisation" value={`${formatQty(lineValue(result.lines, "Coating realisation"), 2)} Rs/kg`} />
              <Summary label="Finishing realisation" value={`${formatQty(lineValue(result.lines, "Finishing realisation"), 2)} Rs/kg`} />
            </>
          ) : null}
        </dl>
        <div className="flex flex-wrap gap-1.5">
          <Badge variant="secondary">Coated {result.coatedLabel}</Badge>
          <Badge variant="secondary">Liner {result.linerLabel}</Badge>
          <Badge variant="secondary">UV {result.uvLabel}</Badge>
          <Badge variant="secondary">{result.printedLabel}</Badge>
        </div>
        {result.warnings.length > 0 ? (
          <ul className="space-y-1 text-xs text-amber-700 dark:text-amber-300">
            {result.warnings.map((warning) => <li key={warning}>{warning}</li>)}
          </ul>
        ) : null}
        <Breakdown lines={result.lines} />
      </aside>
      <QuoteDock result={result} saving={saving} onSave={() => void save()} />
    </div>
  );
}

function QuotationTextCard({ draft, result }: { draft: WovenDraft; result: WovenResult }) {
  const isFabric = draft.bagType === "Flat Fabric" || draft.bagType === "Tubular Fabric";
  const text = quotationText(draft, result, isFabric);
  async function copy() {
    try {
      await navigator.clipboard.writeText(text);
      toast.success("Quotation text copied.");
    } catch {
      toast.error("Quotation text could not be copied.");
    }
  }
  return (
    <section className="card-3d space-y-3 rounded-xl p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="font-semibold">{isFabric ? "Fabric quotation text" : "Bag quotation text"}</h3>
          <p className="text-xs text-muted-foreground">Copy and paste this directly into an email.</p>
        </div>
        <Button size="sm" variant="outline" onClick={() => void copy()}><Copy /> Copy quotation</Button>
      </div>
      <pre className="overflow-x-auto whitespace-pre-wrap rounded-lg border border-border bg-muted/30 p-4 font-sans text-sm leading-6">{text}</pre>
    </section>
  );
}

function quotationText(draft: WovenDraft, result: WovenResult, isFabric: boolean) {
  const number = (value: number | null | undefined, digits = 2) => value == null ? "" : formatQty(value, digits);
  const yesNo = (value: boolean) => value ? "Yes" : "No";
  const plate = draft.printPlateCostText || (draft.printPlateCost == null ? "" : String(draft.printPlateCost));
  const commonEnd: Array<[string, string]> = [
    ["Terms", draft.terms],
    ["Sea Port", draft.seaPort],
    ["Currency", draft.currency],
  ];
  if (isFabric) {
    const totalWidth = result.offerWidth == null || result.offerGusset == null
      ? null
      : result.offerWidth + result.offerGusset;
    const tubeFlat = draft.bagType === "Flat Fabric" ? "Flat" : "Tube";
    return formatQuotationRows("FABRIC QUOTATION", [
      ["Customer Ref", draft.customerRef],
      ["Dimensions", draft.dimensionUnit],
      ["UV (Y/N)", result.uvLabel],
      ["Colour", draft.fabricColour],
      ["Printed Y/N", result.printedLabel],
      ["Packing", draft.packing],
      ["Print Plate Cost/Colour", plate],
      ["Fabric Width", number(result.offerWidth)],
      ["Gusset", number(result.offerGusset)],
      ["Total Width", number(totalWidth)],
      ["Tube/Flat", tubeFlat],
      ["GSM", draft.displayGsm],
      ["Meter/Roll", number(draft.meterPerRoll)],
      ["Rolls/Pallet", number(draft.rollsPerPallet, 0)],
      ...commonEnd,
      ["Price/UOM", `${formatQty(result.pricePerPiece, 4)} ${draft.currency}`.trim()],
    ]);
  }
  return formatQuotationRows("BAG QUOTATION", [
    ["Customer Ref", draft.customerRef],
    ["Dimensions", draft.dimensionUnit],
    ["Bag/Fabric Width", number(result.offerWidth)],
    ["Gusset Width", number(result.offerGusset)],
    ["Bag Length", number(result.offerLength)],
    ["Bag Weight (+/-5%)", `${formatQty(result.totalComponentGram, 2)} g`],
    ["Coated (Y/N)", result.coatedLabel],
    ["UV (Y/N)", result.uvLabel],
    ["Colour", draft.fabricColour],
    ["Printed", result.printedLabel],
    ["Liner (Y/N)", result.linerLabel],
    ["Top", draft.top],
    ["Thumb Notch", yesNo(draft.thumbNotch)],
    ["Metal Detection", yesNo(draft.metalDetection)],
    ["Perforation", yesNo(draft.perforation)],
    ["Packing", draft.packing],
    ["Pieces/Bale", number(draft.pcsPerBale, 0)],
    ["Pallet Size", draft.palletSize],
    ["Bags/Pallet", number(draft.bagsPerPallet, 0)],
    ["Print Plate Cost/Colour", plate],
    ...commonEnd,
    ["Price/Piece", `${formatQty(result.pricePerPiece, 4)} ${draft.currency}`.trim()],
    ["Remarks", draft.remarks],
  ]);
}

function formatQuotationRows(title: string, rows: Array<[string, string]>) {
  return [title, "", ...rows.map(([label, value]) => `${label}: ${value || "—"}`)].join("\n");
}

function QuoteDock({ result, saving, onSave }: { result: WovenResult; saving: boolean; onSave: () => void }) {
  const [left, setLeft] = useState(0);
  useEffect(() => {
    const main = document.getElementById("main-content");
    if (!main) return;
    const update = () => setLeft(main.getBoundingClientRect().left);
    update();
    const observer = new MutationObserver(update);
    observer.observe(main, { attributes: true, attributeFilter: ["class"] });
    window.addEventListener("resize", update);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", update);
    };
  }, []);
  return (
    <div className="fixed bottom-0 z-30 border-t border-border bg-card/95 px-4 py-2.5 shadow-[0_-8px_24px_rgba(0,0,0,0.12)] backdrop-blur-md" style={{ left, right: 0 }}>
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">Price</p>
          <p className="truncate text-lg font-semibold tabular-nums">
            {formatQty(result.pricePerPiece, 4)}
            <span className="ml-2 text-sm font-normal text-muted-foreground">{result.priceBasis || "per piece"}</span>
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Selling weight</p>
          <p className="text-lg font-semibold tabular-nums">{formatQty(result.totalComponentGram, 2)} g</p>
        </div>
        <Button onClick={onSave} disabled={saving}><Save /> {saving ? "Saving…" : "Save"}</Button>
      </div>
    </div>
  );
}

const FABRIC_ORANGE = ["PP", "HD", "LLDPE", "LDPE_FY", "RP", "FILLER", "TPT", "UV", "COL", "BLACK", "CONDUCTIVE"];
const COATING_ORANGE = ["LDPE7", "PP_H350", "LDPE4", "RP", "FILLER", "TPT", "UV", "COL", "BLACK"];
const LINER_ORANGE = ["LLDPE", "LDPE_FY", "RP", "FILLER", "TPT", "UV", "COL", "BLACK"];

function WovenSheet({
  draft,
  patch,
  options,
  materials,
  customers,
  result,
}: {
  draft: WovenDraft;
  patch: (partial: Partial<WovenDraft>) => void;
  options: (category: string) => string[];
  materials: WovenMasters["materials"];
  customers: string[];
  result: WovenResult;
}) {
  const coatingWaste = result.lines.find((line) => line.label === "Coating wastage");
  const isFabric = draft.bagType === "Flat Fabric" || draft.bagType === "Tubular Fabric";
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card">
      <p className="border-b border-border px-3 py-2 text-xs text-muted-foreground">Filled rows are inputs. Shaded rows are calculated. Type 5 for 5%. Leave total weight blank unless the bag must be heavier than the calculated weight.</p>
      <table className="w-full text-sm">
        <tbody>
          <Band>Quote</Band>
          <Entry label="Date"><Input className="h-8 border-0 shadow-none" type="date" value={draft.quoteDate} onChange={(e) => patch({ quoteDate: e.target.value })} /></Entry>
          <Entry label="Sector"><SheetSelect value={draft.sector} onChange={(sector) => patch({ sector })} options={options("Sector")} /></Entry>
          <Entry label="Customer">
            <Input className="h-8 border-0 shadow-none" list="woven-customers" value={draft.customerName} onChange={(e) => patch({ customerName: e.target.value })} />
            <datalist id="woven-customers">{customers.map((name) => <option key={name} value={name} />)}</datalist>
          </Entry>
          <Entry label="Our ref"><Input className="h-8 border-0 shadow-none" value={draft.ourRef} onChange={(e) => patch({ ourRef: e.target.value })} /></Entry>
          <Entry label="Customer ref"><Input className="h-8 border-0 shadow-none" value={draft.customerRef} onChange={(e) => patch({ customerRef: e.target.value })} /></Entry>
          <Band>Bag specification</Band>
          <Entry label="Type of fabric / bag"><SheetSelect value={draft.bagType} onChange={(bagType) => patch({ bagType })} options={options("BagType")} /></Entry>
          <Entry label="Fabric colour"><SheetSelect value={draft.fabricColour} onChange={(fabricColour) => patch({ fabricColour })} options={options("Colour")} /></Entry>
          <Entry label="Face width cm"><SheetNum value={draft.faceWidthCm} onChange={(faceWidthCm) => patch({ faceWidthCm })} /></Entry>
          <Entry label="Gusset cm"><SheetNum value={draft.gussetCm} onChange={(gussetCm) => patch({ gussetCm })} /></Entry>
          <Entry label="Length cm"><SheetNum value={draft.lengthCm} onChange={(lengthCm) => patch({ lengthCm })} /></Entry>
          <Entry label="Top"><SheetSelect value={draft.top} onChange={(top) => patch({ top })} options={options("Top")} /></Entry>
          <Calc label="Cut length cm" value={formatQty(result.cutLengthCm, 2)} />
          <Calc label="Gross width cm" value={formatQty(result.grossWidthCm, 2)} />
          <Entry label="Fabric GSM"><SheetNum value={draft.fabricGsm} onChange={(fabricGsm) => patch({ fabricGsm })} /></Entry>
          <Entry label="Liner micron"><SheetNum value={draft.linerMicron} onChange={(linerMicron) => patch({ linerMicron })} /></Entry>
          <Entry label="Glossy BOPP micron"><SheetNum value={draft.glossyBoppMicron} onChange={(glossyBoppMicron) => patch({ glossyBoppMicron })} /></Entry>
          <Entry label="Matt BOPP micron"><SheetNum value={draft.mattBoppMicron} onChange={(mattBoppMicron) => patch({ mattBoppMicron })} /></Entry>
          <Entry label="Metalised BOPP micron"><SheetNum value={draft.metalisedBoppMicron} onChange={(metalisedBoppMicron) => patch({ metalisedBoppMicron })} /></Entry>
          <Entry label="White paper GSM"><SheetNum value={draft.whitePaperGsm} onChange={(whitePaperGsm) => patch({ whitePaperGsm })} /></Entry>
          <Entry label="Brown paper GSM"><SheetNum value={draft.brownPaperGsm} onChange={(brownPaperGsm) => patch({ brownPaperGsm })} /></Entry>
          <Entry label="Adhesive GSM"><SheetNum value={draft.adhesiveGsm} onChange={(adhesiveGsm) => patch({ adhesiveGsm })} /></Entry>
          <Entry label="Ink GSM"><SheetNum value={draft.inkGsm} onChange={(inkGsm) => patch({ inkGsm })} /></Entry>
          <Entry label="Coating GSM"><SheetNum value={draft.coatingGsm} onChange={(coatingGsm) => patch({ coatingGsm })} /></Entry>
          <Entry label="Handle gram"><SheetNum value={draft.handleGram} onChange={(handleGram) => patch({ handleGram })} /></Entry>
          <Calc label="Calculated weight g" value={formatQty(result.calculatedWeightGram, 2)} />
          <Entry label="Total weight gram (optional)"><SheetNum value={draft.totalWeightGram} onChange={(totalWeightGram) => patch({ totalWeightGram })} placeholder="Only if heavier than calculated" /></Entry>
          <Calc label="Selling weight g" value={formatQty(result.totalComponentGram, 2)} />
          <Calc label="Fabric weight g" value={formatQty(result.fabricGram, 2)} />
          <Calc label="Fabric GSM from weight" value={formatQty(result.fabricGsmCalculated, 2)} />
          <Band>Fabric recipe %</Band>
          <RecipeRows codes={FABRIC_ORANGE} materials={materials} recipe={draft.fabricRecipe} onChange={(fabricRecipe) => patch({ fabricRecipe })} />
          <Calc label="Fabric raw material Rs/kg" value={formatQty(result.fabricRmc, 2)} />
          <Band>Coating recipe %</Band>
          <RecipeRows codes={COATING_ORANGE} materials={materials} recipe={draft.coatingRecipe} onChange={(coatingRecipe) => patch({ coatingRecipe })} />
          <Calc label="Coating raw material Rs/kg" value={formatQty(result.coatingRmc, 2)} />
          <Band>Liner recipe %</Band>
          <RecipeRows codes={LINER_ORANGE} materials={materials} recipe={draft.linerRecipe} onChange={(linerRecipe) => patch({ linerRecipe })} />
          <Calc label="Liner raw material Rs/kg" value={formatQty(result.linerRmc, 2)} />
          <Band>Conversion, wastage and price</Band>
          <Entry label="Fabric conversion Rs/kg"><SheetNum value={draft.fabricConversion} onChange={(fabricConversion) => patch({ fabricConversion })} /></Entry>
          <Entry label="Coating conversion Rs/kg"><SheetNum value={draft.coatingConversion} onChange={(coatingConversion) => patch({ coatingConversion })} /></Entry>
          <Entry label="Liner conversion Rs/kg"><SheetNum value={draft.linerConversion} onChange={(linerConversion) => patch({ linerConversion })} /></Entry>
          <Entry label="Wastage % fabric"><SheetNum value={draft.wastageFabric} onChange={(wastageFabric) => patch({ wastageFabric })} /></Entry>
          <Calc label="Wastage % coating" value={coatingWaste ? formatQty(coatingWaste.value, 2) : "0.00"} />
          <Entry label="Wastage % liner"><SheetNum value={draft.wastageLiner} onChange={(wastageLiner) => patch({ wastageLiner })} /></Entry>
          <Entry label="Wastage % finishing"><SheetNum value={draft.wastageFinishing} onChange={(wastageFinishing) => patch({ wastageFinishing })} /></Entry>
          <Entry label="Wastage realisation fabric Rs/kg"><SheetNum value={draft.realisationFabric} onChange={(realisationFabric) => patch({ realisationFabric })} /></Entry>
          <Entry label="Wastage realisation liner Rs/kg"><SheetNum value={draft.realisationLiner} onChange={(realisationLiner) => patch({ realisationLiner })} /></Entry>
          <Entry label="Bag finishing Rs/pc"><SheetNum value={draft.bagFinishing} onChange={(bagFinishing) => patch({ bagFinishing })} /></Entry>
          <Entry label="Flexo printing Rs/pc"><SheetNum value={draft.flexoPrinting} onChange={(flexoPrinting) => patch({ flexoPrinting })} /></Entry>
          <Entry label="Rewinding Rs/kg"><SheetNum value={draft.rewindingPerKg} onChange={(rewindingPerKg) => patch({ rewindingPerKg })} /></Entry>
          <Entry label="Packing Rs/kg"><SheetNum value={draft.packingPerKg} onChange={(packingPerKg) => patch({ packingPerKg })} /></Entry>
          <Entry label="Pallet Rs/kg"><SheetNum value={draft.palletPerKg} onChange={(palletPerKg) => patch({ palletPerKg })} /></Entry>
          <Entry label="FOB cost Rs/kg"><SheetNum value={draft.fobCostPerKg} onChange={(fobCostPerKg) => patch({ fobCostPerKg })} /></Entry>
          <Entry label="Payment days"><SheetNum value={draft.paymentDays} onChange={(paymentDays) => patch({ paymentDays })} /></Entry>
          <Entry label="Profit %"><SheetNum value={draft.profitPercent} onChange={(profitPercent) => patch({ profitPercent })} /></Entry>
          <Calc label="Quote Rs/kg" value={formatQty(result.quotePerKg, 2)} />
          <Entry label="USD / INR"><SheetNum value={draft.usdInr} onChange={(usdInr) => patch({ usdInr })} /></Entry>
          <Entry label="Euro per USD"><SheetNum value={draft.euroPerUsd} onChange={(euroPerUsd) => patch({ euroPerUsd })} /></Entry>
          <Entry label="GBP per USD"><SheetNum value={draft.gbpPerUsd} onChange={(gbpPerUsd) => patch({ gbpPerUsd })} /></Entry>
          <Entry label="Freight USD/kg"><SheetNum value={draft.freightUsdPerKg} onChange={(freightUsdPerKg) => patch({ freightUsdPerKg })} /></Entry>
          <Entry label="Commission USD/kg"><SheetNum value={draft.commissionUsdPerKg} onChange={(commissionUsdPerKg) => patch({ commissionUsdPerKg })} /></Entry>
          <Calc label="FOB USD/kg" value={formatQty(result.fobUsdPerKg, 4)} />
          <Calc label="CIF USD/kg" value={formatQty(result.cifUsdPerKg, 4)} />
          <Band>Customer offer</Band>
          <Entry label="Dimensions"><SheetSelect value={draft.dimensionUnit} onChange={(dimensionUnit) => patch({ dimensionUnit })} options={options("DimensionUnit")} /></Entry>
          <Entry label="Thumb notch"><SheetSelect value={draft.thumbNotch ? "Yes" : "No"} onChange={(value) => patch({ thumbNotch: value === "Yes" })} options={["Yes", "No"]} /></Entry>
          <Entry label="Metal detection"><SheetSelect value={draft.metalDetection ? "Yes" : "No"} onChange={(value) => patch({ metalDetection: value === "Yes" })} options={["Yes", "No"]} /></Entry>
          <Entry label="Perforation"><SheetSelect value={draft.perforation ? "Yes" : "No"} onChange={(value) => patch({ perforation: value === "Yes" })} options={["Yes", "No"]} /></Entry>
          <Entry label="Terms"><SheetSelect value={draft.terms} onChange={(terms) => patch({ terms })} options={options("Terms")} /></Entry>
          <Entry label="Sea port"><Input className="h-8 border-0 shadow-none" value={draft.seaPort} onChange={(e) => patch({ seaPort: e.target.value })} /></Entry>
          <Entry label="Currency"><SheetSelect value={draft.currency} onChange={(currency) => patch({ currency })} options={options("Currency")} /></Entry>
          <Calc label="Price per piece" value={`${formatQty(result.pricePerPiece, 4)} ${draft.currency}`} />
          <Entry label="Packing"><SheetSelect value={draft.packing} onChange={(packing) => patch({ packing })} options={options("Packing")} /></Entry>
          <Entry label="No of pcs per bale"><SheetNum value={draft.pcsPerBale} onChange={(pcsPerBale) => patch({ pcsPerBale })} /></Entry>
          <Entry label="Pallet size"><Input className="h-8 border-0 shadow-none" value={draft.palletSize} onChange={(e) => patch({ palletSize: e.target.value })} /></Entry>
          <Entry label="Bags / pallet"><SheetNum value={draft.bagsPerPallet} onChange={(bagsPerPallet) => patch({ bagsPerPallet })} /></Entry>
          <Entry label="Print plate cost / colour"><Input className="h-8 border-0 shadow-none" value={draft.printPlateCostText || (draft.printPlateCost == null ? "" : String(draft.printPlateCost))} onChange={(e) => patch({ printPlateCostText: e.target.value })} placeholder="USD 50 Per Colour" /></Entry>
          {isFabric ? <Entry label="Display GSM"><Input className="h-8 border-0 shadow-none" value={draft.displayGsm} onChange={(e) => patch({ displayGsm: e.target.value })} placeholder="70+20" /></Entry> : null}
          {isFabric ? <Entry label="Meter / roll"><SheetNum value={draft.meterPerRoll} onChange={(meterPerRoll) => patch({ meterPerRoll })} /></Entry> : null}
          {isFabric ? <Entry label="Rolls / pallet"><SheetNum value={draft.rollsPerPallet} onChange={(rollsPerPallet) => patch({ rollsPerPallet })} /></Entry> : null}
          <Entry label="Remarks"><Input className="h-8 border-0 shadow-none" value={draft.remarks} onChange={(e) => patch({ remarks: e.target.value })} /></Entry>
        </tbody>
      </table>
    </div>
  );
}

function withPercentWastage(draft: WovenDraft): WovenDraft {
    return {
    ...draft,
    quoteDate: draft.quoteDate ? draft.quoteDate.slice(0, 10) : "",
    printPlateCostText: draft.printPlateCostText ?? "",
    displayGsm: draft.displayGsm ?? "",
    meterPerRoll: draft.meterPerRoll ?? null,
    rollsPerPallet: draft.rollsPerPallet ?? null,
    wastageFabric: showWastagePercent(draft.wastageFabric),
    wastageLiner: showWastagePercent(draft.wastageLiner),
    wastageFinishing: showWastagePercent(draft.wastageFinishing),
  };
}

function Band({ children }: { children: string }) {
  return (
    <tr className="bg-muted/70">
      <td colSpan={2} className="px-3 py-2 text-xs font-semibold uppercase tracking-wide">{children}</td>
    </tr>
  );
}

function Entry({ label, children }: { label: string; children: ReactNode }) {
  return (
    <tr className="border-t border-border">
      <td className="w-[46%] bg-muted/60 px-3 py-1 text-foreground">{label}</td>
      <td className="bg-background p-0">{children}</td>
    </tr>
  );
}

function Calc({ label, value }: { label: string; value: string }) {
  return (
    <tr className="border-t border-border bg-muted/30">
      <td className="px-3 py-1.5 text-muted-foreground">{label}</td>
      <td className="px-3 py-1.5 text-right font-medium tabular-nums">{value}</td>
    </tr>
  );
}

function SheetNum({ value, onChange, placeholder }: { value: number | null; onChange: (value: number | null) => void; placeholder?: string }) {
  return (
    <Input
      className="h-8 rounded-none border-0 bg-transparent shadow-none"
      type="number"
      step="any"
      placeholder={placeholder}
      value={value ?? ""}
      onChange={(e) => onChange(e.target.value === "" ? null : Number(e.target.value))}
    />
  );
}

function SheetSelect({ value, onChange, options }: { value: string; onChange: (value: string) => void; options: string[] }) {
  return (
    <select className="h-8 w-full bg-transparent px-3 text-sm outline-none" value={value} onChange={(e) => onChange(e.target.value)}>
      {options.map((option) => <option key={option} value={option}>{option}</option>)}
    </select>
  );
}

function RecipeRows({
  codes,
  materials,
  recipe,
  onChange,
}: {
  codes: string[];
  materials: WovenMasters["materials"];
  recipe: Record<string, number>;
  onChange: (recipe: Record<string, number>) => void;
}) {
  const names = new Map(materials.map((row) => [row.code, row.name]));
  return codes.map((code) => (
    <Entry key={code} label={`${names.get(code) ?? code} %`}>
      <SheetNum value={recipe[code] ?? null} onChange={(value) => onChange({ ...recipe, [code]: value ?? 0 })} />
    </Entry>
  ));
}

function AdStarSpec({ draft, patch }: { draft: WovenDraft; patch: (partial: Partial<WovenDraft>) => void }) {
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card">
      <table className="w-full text-sm">
        <tbody>
          <Entry label="Width"><SheetNum value={draft.adWidth} onChange={(adWidth) => patch({ adWidth })} /></Entry>
          <Entry label="Length"><SheetNum value={draft.adLength} onChange={(adLength) => patch({ adLength })} /></Entry>
          <Entry label="Patch"><SheetNum value={draft.adPatch} onChange={(adPatch) => patch({ adPatch })} /></Entry>
          <Entry label="Valve length"><SheetNum value={draft.adValveLength} onChange={(adValveLength) => patch({ adValveLength })} /></Entry>
          <Entry label="Extended valve"><SheetNum value={draft.adExtendedValve} onChange={(adExtendedValve) => patch({ adExtendedValve })} /></Entry>
          <Entry label="Fabric GSM"><SheetNum value={draft.adFabricGsm} onChange={(adFabricGsm) => patch({ adFabricGsm })} /></Entry>
          <Entry label="Coating GSM"><SheetNum value={draft.adCoatingGsm} onChange={(adCoatingGsm) => patch({ adCoatingGsm })} /></Entry>
          <Entry label="BOPP GSM"><SheetNum value={draft.adBoppGsm} onChange={(adBoppGsm) => patch({ adBoppGsm })} /></Entry>
          <Entry label="Liner micron"><SheetNum value={draft.adLinerMicron} onChange={(adLinerMicron) => patch({ adLinerMicron })} /></Entry>
          <Entry label="Conversion Rs/kg"><SheetNum value={draft.adConversion} onChange={(adConversion) => patch({ adConversion })} /></Entry>
          <Entry label="Sea freight Rs/kg"><SheetNum value={draft.adSeaFreight} onChange={(adSeaFreight) => patch({ adSeaFreight })} /></Entry>
          <Entry label="Printing"><Input className="h-8 border-0 bg-transparent shadow-none" value={draft.adPrinting} onChange={(e) => patch({ adPrinting: e.target.value })} /></Entry>
          <Entry label="Open mouth"><SheetSelect value={draft.adOpenMouth ? "Yes" : "No"} onChange={(value) => patch({ adOpenMouth: value === "Yes" })} options={["Yes", "No"]} /></Entry>
          <Entry label="UV"><SheetSelect value={draft.adUv ? "Yes" : "No"} onChange={(value) => patch({ adUv: value === "Yes" })} options={["Yes", "No"]} /></Entry>
        </tbody>
      </table>
    </div>
  );
}

function Breakdown({ lines }: { lines: WovenResultLines }) {
  const groups = [...new Set(lines.map((line) => line.group))];
  return (
    <div className="space-y-3 border-t border-border pt-3">
      {groups.map((group) => (
        <div key={group}>
          <p className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">{group}</p>
          {lines.filter((line) => line.group === group).map((line) => (
            <div key={`${group}-${line.label}-${line.unit}`} className="flex justify-between gap-3 py-0.5 text-xs">
              <span className="text-muted-foreground">{line.label}</span>
              <span className="tabular-nums">{formatQty(line.value, line.unit.includes("USD") || line.unit.includes("pc") ? 4 : 2)} {line.unit}</span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
type WovenResultLines = { group: string; label: string; value: number; unit: string }[];

function MastersPanel({ masters, rates }: { masters: WovenMasters; rates: WovenRate[] }) {
  const client = useQueryClient();
  const [constants, setConstants] = useState<WovenConstant[]>(masters.constants);
  const [sector, setSector] = useState("Export");
  const [rateDate, setRateDate] = useState(new Date().toISOString().slice(0, 10));
  const [pendingRates, setPendingRates] = useState<Record<string, string>>({});
  const [customerSector, setCustomerSector] = useState("Export");
  const [fullName, setFullName] = useState("");
  const [shortName, setShortName] = useState("");
  const [customerQuery, setCustomerQuery] = useState("");
  const [lookupCategory, setLookupCategory] = useState("BagType");
  const [lookupValue, setLookupValue] = useState("");

  useEffect(() => setConstants(masters.constants), [masters.constants]);

  const asOf = useMemo(() => ratesAsOf(rates, sector, rateDate), [rates, sector, rateDate]);
  const fromDate = useMemo(() => {
    const found = new Map<string, string>();
    for (const row of rates) {
      if (row.sector !== sector) continue;
      const day = row.effectiveDate.slice(0, 10);
      if (day > rateDate) continue;
      const current = found.get(row.materialCode);
      if (!current || current < day) found.set(row.materialCode, day);
    }
    return found;
  }, [rates, sector, rateDate]);

  useEffect(() => {
    const next: Record<string, string> = {};
    for (const material of masters.materials) {
      const value = asOf[material.code];
      next[material.code] = value === undefined ? "" : String(value);
    }
    setPendingRates(next);
  }, [asOf, masters.materials]);
  const shownCustomers = masters.customers.filter((row) => {
    const text = `${row.fullName} ${row.shortName}`.toLowerCase();
    return row.sector === customerSector && text.includes(customerQuery.trim().toLowerCase());
  }).slice(0, 40);

  async function refresh() {
    await client.invalidateQueries({ queryKey: ["woven-masters"] });
    await client.invalidateQueries({ queryKey: ["woven-rates"] });
  }

  async function saveRatesForDate() {
    const changed = masters.materials.filter((material) => {
      const raw = pendingRates[material.code];
      if (raw === undefined || raw === "") return false;
      const next = Number(raw);
      if (Number.isNaN(next)) return false;
      const exact = rates.find((row) => row.sector === sector && row.materialCode === material.code && row.effectiveDate.slice(0, 10) === rateDate);
      if (exact) return exact.rate !== next;
      return asOf[material.code] !== next;
    });
    if (changed.length === 0) {
      toast.success("No rate changes for this date.");
      return;
    }
    try {
      await Promise.all(changed.map((material) => saveWovenRate({
        materialCode: material.code,
        sector,
        effectiveDate: rateDate,
        rate: Number(pendingRates[material.code]),
      })));
      toast.success(`Saved ${changed.length} rate${changed.length === 1 ? "" : "s"} for ${rateDate}.`);
      await refresh();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not save rates.");
    }
  }

  return (
    <section className="card-3d rounded-xl p-4 md:p-5">
      <Tabs defaultValue="rates">
        <TabsList>
          <TabsTrigger value="rates">Raw material rates</TabsTrigger>
          <TabsTrigger value="constants">Formula constants</TabsTrigger>
          <TabsTrigger value="customers">Customers</TabsTrigger>
          <TabsTrigger value="lists">Other Masters</TabsTrigger>
        </TabsList>
        <TabsContent value="rates" className="space-y-3 pt-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <Select label="Sector" value={sector} onChange={setSector} options={["Export", "Domestic"]} />
            <Field label="Effective date" type="date" value={rateDate} onChange={setRateDate} />
          </div>
          <p className="text-xs text-muted-foreground">These are the rates in force on {rateDate}. Saving writes them on this date, so earlier quotations keep the rate that applied then. {rates.length.toLocaleString("en-IN")} historical rates are loaded.</p>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-left text-xs uppercase text-muted-foreground">
                <tr className="border-b border-border"><th className="py-2">Material</th><th className="py-2">Rate on this date</th><th className="py-2">Taken from</th></tr>
              </thead>
              <tbody>
                {masters.materials.map((material) => (
                  <tr key={material.code} className="border-b border-border/70">
                    <td className="py-2 pr-3">{material.name}</td>
                    <td className="py-2 pr-3">
                      <Input className="h-8 w-32" type="number" step="any" value={pendingRates[material.code] ?? ""} onChange={(e) => setPendingRates((rows) => ({ ...rows, [material.code]: e.target.value }))} />
                    </td>
                    <td className="py-2 tabular-nums text-muted-foreground">{fromDate.get(material.code) ?? "No rate yet"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Button onClick={() => void saveRatesForDate()}>Save rates for {rateDate}</Button>
        </TabsContent>
        <TabsContent value="constants" className="space-y-4 pt-4">
          {[...new Set(constants.map((row) => row.groupName))].map((group) => (
            <div key={group}>
              <h3 className="mb-2 text-sm font-medium">{group}</h3>
              <div className="grid gap-3 md:grid-cols-2">
                {constants.filter((row) => row.groupName === group).map((row) => (
                  <label key={row.key} className="space-y-1 text-xs">
                    <span className="text-muted-foreground">{row.label}</span>
                    <Input type="number" step="any" value={row.value} onChange={(e) => setConstants((rows) => rows.map((item) => item.key === row.key ? { ...item, value: Number(e.target.value) } : item))} />
                  </label>
                ))}
              </div>
            </div>
          ))}
          <Button onClick={() => void saveWovenConstants(constants).then(() => { toast.success("Constants saved."); return refresh(); }).catch((error: Error) => toast.error(error.message))}>Save constants</Button>
        </TabsContent>
        <TabsContent value="customers" className="space-y-3 pt-4">
          <div className="grid gap-3 md:grid-cols-4">
            <Select label="Sector" value={customerSector} onChange={setCustomerSector} options={["Export", "Domestic"]} />
            <Field label="Full name" value={fullName} onChange={setFullName} />
            <Field label="Short name" value={shortName} onChange={setShortName} />
            <div className="flex items-end">
              <Button onClick={() => void saveWovenCustomer({ sector: customerSector, fullName, shortName, isActive: true }).then(() => { setFullName(""); setShortName(""); toast.success("Customer added."); return refresh(); }).catch((error: Error) => toast.error(error.message))}>Add</Button>
            </div>
          </div>
          <Field label="Search" value={customerQuery} onChange={setCustomerQuery} />
          <ul className="max-h-80 space-y-1 overflow-auto text-sm">
            {shownCustomers.map((row) => <li key={row.id} className="flex justify-between gap-3 border-b border-border/60 py-1.5"><span>{row.fullName}</span><span className="text-muted-foreground">{row.shortName}</span></li>)}
          </ul>
        </TabsContent>
        <TabsContent value="lists" className="space-y-3 pt-4">
          <div className="grid gap-3 md:grid-cols-3">
            <Select label="List" value={lookupCategory} onChange={setLookupCategory} options={[...new Set(masters.lookups.map((row) => row.category))]} />
            <Field label="New value" value={lookupValue} onChange={setLookupValue} />
            <div className="flex items-end">
              <Button onClick={() => void saveWovenLookup(lookupCategory, lookupValue).then(() => { setLookupValue(""); toast.success("Value added."); return refresh(); }).catch((error: Error) => toast.error(error.message))}>Add</Button>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {masters.lookups.filter((row) => row.category === lookupCategory).map((row) => <Badge key={row.id} variant="secondary">{row.value}</Badge>)}
          </div>
        </TabsContent>
      </Tabs>
    </section>
  );
}

function lineValue(lines: WovenResultLines, label: string) {
  return lines.find((line) => line.label === label)?.value ?? 0;
}

function Summary({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-3">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="tabular-nums">{value}</dd>
    </div>
  );
}

function Field({ label, value, onChange, type = "text" }: { label: string; value: string; onChange: (value: string) => void; type?: string }) {
  return (
    <label className="space-y-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <Input type={type} value={value} onChange={(e) => onChange(e.target.value)} />
    </label>
  );
}

function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: string[] }) {
  return (
    <label className="space-y-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <select className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm" value={value} onChange={(e) => onChange(e.target.value)}>
        {options.map((option) => <option key={option || "blank"} value={option}>{option || "All"}</option>)}
      </select>
    </label>
  );
}
