import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import { ArrowLeft, Calculator, Check, ChevronsUpDown, Copy, Eye, Loader2, Plus, Save, Settings2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
  Select as UiSelect,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useAuth } from "@/lib/auth-context";
import {
  createWovenQuote,
  deleteWovenQuote,
  getWovenForex,
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
  type WovenDetail,
  type WovenDraft,
  type WovenForex,
  type WovenMasters,
  type WovenRate,
  type WovenResult,
} from "@/lib/woven-quote-api";
import { blankWovenDraft, calculateWovenQuote, formatQty, ratesAsOf, showWastagePercent } from "@/lib/woven-quote-calc";

type View = "list" | "edit" | "masters";
type LeaveDecision = "save-draft" | "discard" | "cancel";

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

      {view === "list" && !masters.data ? <p className="text-sm text-muted-foreground">Loading quotation filters…</p> : null}
      {view === "list" && masters.data ? <QuoteList tab={listTab} masters={masters.data} onTabChange={setListTab} onOpen={openQuote} /> : null}
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
      {view === "masters" && masters.data ? <MastersPanel masters={masters.data} rates={rates.data ?? []} onBack={() => setView("list")} /> : null}
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
  masters,
  onTabChange,
  onOpen,
}: {
  tab: "saved" | "drafts";
  masters: WovenMasters;
  onTabChange: (tab: "saved" | "drafts") => void;
  onOpen: (id: number) => void;
}) {
  const [filters, setFilters] = useState<QuoteFilters>(emptyQuoteFilters);
  const [applied, setApplied] = useState<QuoteFilters>(emptyQuoteFilters);
  const [dateOrder, setDateOrder] = useState<"desc" | "asc">("desc");
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(() => new Set());
  const [copying, setCopying] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const [previewQuotes, setPreviewQuotes] = useState<WovenDetail[]>([]);
  const [previewOpen, setPreviewOpen] = useState(false);
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
  const lookupOptions = (category: string) => [...new Set(masters.lookups.filter((row) => row.category === category).map((row) => row.value))];
  const customerOptions = [...new Set(masters.customers.map((row) => row.shortName || row.fullName).filter(Boolean))].sort((a, b) => a.localeCompare(b));
  const selectableRows = rows.filter((row) => row.productType === "WovenBag");
  const pageSelected = selectableRows.length > 0 && selectableRows.every((row) => selectedIds.has(row.id));

  function toggleSelected(id: number, checked: boolean) {
    setSelectedIds((current) => {
      const next = new Set(current);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  async function loadSelectedQuotes() {
    return Promise.all([...selectedIds].map((id) => getWovenQuote(id)));
  }

  async function previewSelected() {
    if (selectedIds.size === 0) return;
    setPreviewing(true);
    try {
      const quotes = await loadSelectedQuotes();
      setPreviewQuotes(quotes);
      setPreviewOpen(true);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Selected quotations could not be loaded.");
    } finally {
      setPreviewing(false);
    }
  }

  async function copySelected(loadedQuotes?: WovenDetail[]) {
    if (selectedIds.size === 0 && !loadedQuotes?.length) return;
    setCopying(true);
    try {
      const quotes = loadedQuotes ?? await loadSelectedQuotes();
      const { html, plainText } = mergedQuotationClipboard(quotes);
      if (navigator.clipboard.write && typeof ClipboardItem !== "undefined") {
        await navigator.clipboard.write([
          new ClipboardItem({
            "text/html": new Blob([html], { type: "text/html" }),
            "text/plain": new Blob([plainText], { type: "text/plain" }),
          }),
        ]);
      } else {
        await navigator.clipboard.writeText(plainText);
      }
      toast.success(`${quotes.length} quotation${quotes.length === 1 ? "" : "s"} copied as one table.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Selected quotations could not be copied.");
    } finally {
      setCopying(false);
    }
  }

  return (
    <section className="card-3d space-y-4 rounded-xl p-4 md:p-5">
      <Tabs value={tab} onValueChange={(value) => { onTabChange(value === "drafts" ? "drafts" : "saved"); setPage(1); setSelectedIds(new Set()); }}>
        <TabsList>
          <TabsTrigger value="saved">Saved</TabsTrigger>
          <TabsTrigger value="drafts">Drafts</TabsTrigger>
        </TabsList>
      </Tabs>
      <div className="grid gap-3 md:grid-cols-4">
        <Select label="Sector" value={filters.sector} onChange={(value) => setFilter("sector", value)} options={["", "Export", "Domestic"]} />
        <ComboboxField label="Customer" value={filters.customer} onChange={(value) => setFilter("customer", value)} options={customerOptions} placeholder="All customers" />
        <Field label="Our reference" value={filters.ourRef} onChange={(value) => setFilter("ourRef", value)} />
        <Field label="Customer reference" value={filters.customerRef} onChange={(value) => setFilter("customerRef", value)} />
        <Select label="Type" value={filters.productType} onChange={(value) => setFilter("productType", value)} options={["", "WovenBag", "AdStar"]} labels={{ WovenBag: "Woven bag", AdStar: "AD Star" }} />
        <Select label="Terms" value={filters.terms} onChange={(value) => setFilter("terms", value)} options={["", ...lookupOptions("Terms")]} />
        <Select label="Currency" value={filters.currency} onChange={(value) => setFilter("currency", value)} options={["", ...lookupOptions("Currency")]} />
        <Select
          label="Date order"
          value={dateOrder}
          onChange={(value) => { setDateOrder(value === "asc" ? "asc" : "desc"); setPage(1); setSelectedIds(new Set()); }}
          options={["desc", "asc"]}
          labels={{ desc: "Newest first", asc: "Oldest first" }}
        />
        <Field label="Date from" type="date" value={filters.from} onChange={(value) => setFilter("from", value)} />
        <Field label="Date to" type="date" value={filters.to} onChange={(value) => setFilter("to", value)} />
        <Field label="Minimum weight g" type="number" value={filters.minWeight} onChange={(value) => setFilter("minWeight", value)} />
        <Field label="Maximum weight g" type="number" value={filters.maxWeight} onChange={(value) => setFilter("maxWeight", value)} />
        <Field label="Minimum price" type="number" value={filters.minPrice} onChange={(value) => setFilter("minPrice", value)} />
        <Field label="Maximum price" type="number" value={filters.maxPrice} onChange={(value) => setFilter("maxPrice", value)} />
        <div className="flex items-end gap-2 md:col-span-2">
          <Button onClick={() => { setApplied(filters); setPage(1); setSelectedIds(new Set()); }}>Apply filters</Button>
          <Button variant="outline" onClick={() => { const empty = emptyQuoteFilters(); setFilters(empty); setApplied(empty); setPage(1); setSelectedIds(new Set()); }}>Clear</Button>
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <Button variant="outline" disabled={selectedIds.size === 0 || previewing} onClick={() => void previewSelected()}>
          {previewing ? <Loader2 className="animate-spin" /> : <Eye />}
          Preview selected{selectedIds.size > 0 ? ` (${selectedIds.size})` : ""}
        </Button>
        <Button variant="outline" disabled={selectedIds.size === 0 || copying} onClick={() => void copySelected()}>
          {copying ? <Loader2 className="animate-spin" /> : <Copy />}
          Copy selected{selectedIds.size > 0 ? ` (${selectedIds.size})` : ""}
        </Button>
        {selectedIds.size > 0 ? <Button variant="ghost" onClick={() => setSelectedIds(new Set())}>Clear selection</Button> : null}
        <p className="text-xs text-muted-foreground">Filter by customer or date, select the matching bag or fabric quotes, then copy one merged table.</p>
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
              <th className="w-10 py-2 pr-3 font-medium">
                <Checkbox
                  aria-label="Select all woven quotations on this page"
                  checked={pageSelected}
                  onCheckedChange={(checked) => {
                    setSelectedIds((current) => {
                      const next = new Set(current);
                      selectableRows.forEach((row) => checked ? next.add(row.id) : next.delete(row.id));
                      return next;
                    });
                  }}
                />
              </th>
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
                <td className="py-2.5 pr-3">
                  <Checkbox
                    aria-label={`Select ${row.customerName || "untitled"} quotation`}
                    checked={selectedIds.has(row.id)}
                    disabled={row.productType !== "WovenBag"}
                    title={row.productType !== "WovenBag" ? "Merged copy is available for bag and fabric quotations." : undefined}
                    onCheckedChange={(checked) => toggleSelected(row.id, checked === true)}
                  />
                </td>
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
              <tr><td colSpan={12} className="py-8 text-center text-muted-foreground">{tab === "drafts" ? "No drafts match these filters." : "No saved quotations match these filters."}</td></tr>
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
      <Dialog open={previewOpen} onOpenChange={setPreviewOpen}>
        <DialogContent className="flex h-[92vh] w-[90vw] max-w-3xl flex-col gap-0 overflow-hidden p-0">
          <DialogHeader className="shrink-0 px-6 pb-4 pt-6">
            <DialogTitle>Merged quotation preview</DialogTitle>
            <DialogDescription>Review the combined bag and fabric sections before copying them into an email.</DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-auto border-y border-border bg-white p-4 md:p-6">
            <div
              className="mx-auto w-full min-w-[560px]"
              dangerouslySetInnerHTML={{ __html: mergedQuotationClipboard(previewQuotes).html }}
            />
          </div>
          <div className="flex shrink-0 justify-end gap-2 px-6 py-4">
            <Button variant="outline" onClick={() => setPreviewOpen(false)}>Close</Button>
            <Button disabled={copying} onClick={() => void copySelected(previewQuotes)}>
              {copying ? <Loader2 className="animate-spin" /> : <Copy />}
              Copy merged table
            </Button>
          </div>
        </DialogContent>
      </Dialog>
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
  const [isDirty, setIsDirty] = useState(false);
  const [leavePromptOpen, setLeavePromptOpen] = useState(false);
  const [promptSaving, setPromptSaving] = useState(false);
  const dirty = useRef(false);
  const kindRef = useRef<"saved" | "draft" | "new">(quoteId == null ? "new" : "saved");
  const draftRef = useRef(draft);
  const idRef = useRef(quoteId);
  const leaveDecisionRef = useRef<((decision: LeaveDecision) => void) | null>(null);
  draftRef.current = draft;
  idRef.current = quoteId;
  const forex = useQuery({
    queryKey: ["woven-forex", draft.quoteDate],
    queryFn: () => getWovenForex(draft.quoteDate),
    enabled: /^\d{4}-\d{2}-\d{2}$/.test(draft.quoteDate),
    staleTime: 5 * 60 * 1000,
  });
  const routeBlocker = useBlocker({
    shouldBlockFn: () => isDirty,
    withResolver: true,
    enableBeforeUnload: isDirty,
    disabled: !isDirty,
  });

  useEffect(() => {
    if (quoteId == null) {
      kindRef.current = "new";
      dirty.current = false;
      setIsDirty(false);
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
        dirty.current = false;
        setIsDirty(false);
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

  useEffect(() => {
    if (quoteId != null || !forex.data) return;
    setDraft((current) => current.quoteDate === forex.data.asOf.slice(0, 10)
      ? {
          ...current,
          usdInr: forex.data.quotationDollar,
          euroPerUsd: forex.data.quotationEuroUsd,
          gbpPerUsd: forex.data.quotationGbpUsd,
        }
      : current);
  }, [forex.data, quoteId]);

  const result = useMemo(
    () => calculateWovenQuote(draft, ratesAsOf(rates, draft.sector, draft.quoteDate), masters.constants, masters.materials),
    [draft, rates, masters],
  );

  function patch(partial: Partial<WovenDraft>) {
    dirty.current = true;
    setIsDirty(true);
    setDraft((current) => ({ ...current, ...partial }));
  }

  function markClean() {
    dirty.current = false;
    setIsDirty(false);
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
    const decision = await new Promise<LeaveDecision>((resolve) => {
      leaveDecisionRef.current = resolve;
      setLeavePromptOpen(true);
    });
    if (decision === "cancel") return false;
    if (decision === "discard") {
      markClean();
      return kindRef.current;
    }
    try {
      await persist(true, kindRef.current === "saved");
      markClean();
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

  async function handleLeaveDecision(decision: LeaveDecision) {
    if (routeBlocker.status !== "blocked") {
      setLeavePromptOpen(false);
      const resolve = leaveDecisionRef.current;
      leaveDecisionRef.current = null;
      resolve?.(decision);
      return;
    }
    if (decision === "cancel") {
      routeBlocker.reset?.();
      return;
    }
    if (decision === "discard") {
      markClean();
      routeBlocker.proceed?.();
      return;
    }
    setPromptSaving(true);
    try {
      await persist(true, kindRef.current === "saved");
      markClean();
      toast.success("Draft saved.");
      routeBlocker.proceed?.();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Draft could not be saved.");
    } finally {
      setPromptSaving(false);
    }
  }

  async function save() {
    setSaving(true);
    try {
      await persist(false);
      markClean();
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
      markClean();
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
    markClean();
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
          <WovenSheet draft={draft} patch={patch} options={options} materials={masters.materials} customers={customers} result={result} forex={forex.data} />
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
      <AlertDialog
        open={leavePromptOpen || routeBlocker.status === "blocked"}
        onOpenChange={(open) => { if (!open) void handleLeaveDecision("cancel"); }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Save your changes as a draft?</AlertDialogTitle>
            <AlertDialogDescription>
              {kindRef.current === "saved"
                ? "The original quotation will remain saved. Choosing Save as draft creates a separate draft copy containing your edits."
                : kindRef.current === "draft"
                  ? "You have unsaved changes to this draft."
                  : "This new quotation has not been saved yet."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <Button variant="outline" disabled={promptSaving} onClick={() => void handleLeaveDecision("cancel")}>Stay here</Button>
            <Button variant="destructive" disabled={promptSaving} onClick={() => void handleLeaveDecision("discard")}>Discard changes</Button>
            <Button disabled={promptSaving} onClick={() => void handleLeaveDecision("save-draft")}>
              {promptSaving ? <Loader2 className="animate-spin" /> : <Save />}
              Save as draft
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <QuoteDock result={result} saving={saving} onSave={() => void save()} />
    </div>
  );
}

function QuotationTextCard({ draft, result }: { draft: WovenDraft; result: WovenResult }) {
  const isFabric = draft.bagType === "Flat Fabric" || draft.bagType === "Tubular Fabric";
  const quotation = quotationTable(draft, result, isFabric);
  const plainText = [quotation.title, "", ...quotation.rows.map(([label, value]) => `${label}: ${value || "—"}`)].join("\n");
  const html = quotationTableHtml(quotation.rows);
  async function copy() {
    try {
      if (navigator.clipboard.write && typeof ClipboardItem !== "undefined") {
        await navigator.clipboard.write([
          new ClipboardItem({
            "text/html": new Blob([html], { type: "text/html" }),
            "text/plain": new Blob([plainText], { type: "text/plain" }),
          }),
        ]);
      } else {
        await navigator.clipboard.writeText(plainText);
      }
      toast.success("Quotation table copied.");
    } catch {
      try {
        await navigator.clipboard.writeText(plainText);
        toast.success("Quotation copied as text.");
      } catch {
        toast.error("Quotation could not be copied.");
      }
    }
  }
  return (
    <section className="card-3d space-y-3 rounded-xl p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="font-semibold">{isFabric ? "Fabric quotation table" : "Bag quotation table"}</h3>
          <p className="text-xs text-muted-foreground">Copy and paste this formatted table directly into an email.</p>
        </div>
        <Button size="sm" variant="outline" onClick={() => void copy()}><Copy /> Copy table</Button>
      </div>
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full border-collapse text-sm">
          <tbody>
            {quotation.rows.map(([label, value]) => {
              const price = label.startsWith("Price/");
              return (
                <tr key={label}>
                  <td className={`w-[46%] border-b border-r border-border px-3 py-2 font-medium ${price ? "bg-amber-100 text-amber-950" : "bg-muted/60"}`}>{label}</td>
                  <td className={`border-b border-border px-3 py-2 text-right tabular-nums ${price ? "bg-amber-50 font-semibold text-amber-950" : "bg-background"}`}>{value || "—"}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function quotationTable(draft: WovenDraft, result: WovenResult, isFabric: boolean): { title: string; rows: Array<[string, string]> } {
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
    return { title: "FABRIC QUOTATION", rows: [
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
    ] };
  }
  return { title: "BAG QUOTATION", rows: [
    ["Customer Ref", draft.customerRef],
    ["Dimensions", draft.dimensionUnit],
    ["Bag/Fabric Width", number(result.offerWidth)],
    ["Gusset Width", number(result.offerGusset)],
    ["Bag Length", number(result.offerLength)],
    ["Bag Weight (+/-5%)", `${formatQty(result.totalComponentGram, 2)} g`],
    ["Fabric GSM", number(draft.fabricGsm ?? result.fabricGsmCalculated)],
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
  ] };
}

function quotationTableHtml(rows: Array<[string, string]>) {
  const cells = rows.map(([label, value]) => {
    const price = label.startsWith("Price/");
    const labelBackground = price ? "#f6d8bd" : "#e7e7e7";
    const valueBackground = price ? "#fce8d5" : "#f5f5f5";
    return `<tr><td style="border:1px solid #777;padding:6px 8px;background:${labelBackground};color:#111;font-weight:500">${escapeHtml(label)}</td><td style="border:1px solid #777;padding:6px 8px;background:${valueBackground};color:#111;text-align:right;white-space:nowrap${price ? ";font-weight:700" : ""}">${escapeHtml(value || "—")}</td></tr>`;
  }).join("");
  return `<table style="border-collapse:collapse;width:100%;max-width:720px;font-family:Arial,sans-serif;font-size:14px">${cells}</table>`;
}

function mergedQuotationClipboard(quotes: WovenDetail[]) {
  const groups = [
    {
      title: "BAG QUOTATIONS",
      quotes: quotes.filter((quote) => quote.draft.bagType !== "Flat Fabric" && quote.draft.bagType !== "Tubular Fabric"),
    },
    {
      title: "FABRIC QUOTATIONS",
      quotes: quotes.filter((quote) => quote.draft.bagType === "Flat Fabric" || quote.draft.bagType === "Tubular Fabric"),
    },
  ].filter((group) => group.quotes.length > 0);

  const plainSections: string[] = [];
  const htmlSections: string[] = [];
  for (const group of groups) {
    const tables = group.quotes.map((quote) => ({
      quote,
      table: quotationTable(quote.draft, quote.result, group.title === "FABRIC QUOTATIONS"),
    }));
    const labels = [...new Set(tables.flatMap(({ table }) => table.rows.map(([label]) => label)))];
    const headings = tables.map(({ quote }) => quoteHeading(quote));
    plainSections.push([
      group.title,
      ["Field", ...headings].join("\t"),
      ...labels.map((label) => [
        label,
        ...tables.map(({ table }) => table.rows.find(([rowLabel]) => rowLabel === label)?.[1] || "—"),
      ].join("\t")),
    ].join("\n"));

    const headerCells = headings.map((heading) =>
      `<th style="border:1px solid #777;padding:7px 9px;background:#d9e3f0;color:#111;text-align:center;white-space:nowrap">${escapeHtml(heading)}</th>`,
    ).join("");
    const bodyRows = labels.map((label) => {
      const price = label.startsWith("Price/");
      const valueCells = tables.map(({ table }) => {
        const value = table.rows.find(([rowLabel]) => rowLabel === label)?.[1] || "—";
        return `<td style="border:1px solid #777;padding:6px 8px;background:${price ? "#fce8d5" : "#f5f5f5"};color:#111;text-align:right;white-space:nowrap${price ? ";font-weight:700" : ""}">${escapeHtml(value)}</td>`;
      }).join("");
      return `<tr><td style="border:1px solid #777;padding:6px 8px;background:${price ? "#f6d8bd" : "#e7e7e7"};color:#111;font-weight:500;white-space:nowrap">${escapeHtml(label)}</td>${valueCells}</tr>`;
    }).join("");
    htmlSections.push(
      `<div style="margin-bottom:18px"><div style="margin:0 0 6px;font-family:Arial,sans-serif;font-size:15px;font-weight:700;color:#111">${group.title}</div><table style="border-collapse:collapse;width:100%;min-width:560px;font-family:Arial,sans-serif;font-size:13px"><thead><tr><th style="border:1px solid #777;padding:7px 9px;background:#c8d6e5;color:#111;text-align:left">Field</th>${headerCells}</tr></thead><tbody>${bodyRows}</tbody></table></div>`,
    );
  }
  return {
    plainText: plainSections.join("\n\n"),
    html: htmlSections.join(""),
  };
}

function quoteHeading(quote: WovenDetail) {
  const ref = quote.draft.customerRef || quote.draft.ourRef || `Quote ${quote.id}`;
  return `${quote.draft.customerName || "Untitled"} — ${ref} — ${quote.quoteDate.slice(0, 10)}`;
}

function escapeHtml(value: string) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#039;");
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
  forex,
}: {
  draft: WovenDraft;
  patch: (partial: Partial<WovenDraft>) => void;
  options: (category: string) => string[];
  materials: WovenMasters["materials"];
  customers: string[];
  result: WovenResult;
  forex?: WovenForex;
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
          <Entry label="Customer"><SheetCombobox value={draft.customerName} onChange={(customerName) => patch({ customerName })} options={customers} placeholder="Type or select customer" /></Entry>
          <Entry label="Our ref"><Input className="h-8 border-0 shadow-none" value={draft.ourRef} onChange={(e) => patch({ ourRef: e.target.value })} /></Entry>
          <Entry label="Customer ref"><Input className="h-8 border-0 shadow-none" value={draft.customerRef} onChange={(e) => patch({ customerRef: e.target.value })} /></Entry>
          <Band>Bag specification</Band>
          <Entry label="Input unit"><SheetSelect value={draft.dimensionUnit} onChange={(dimensionUnit) => patch({ dimensionUnit })} options={options("DimensionUnit")} /></Entry>
          <Entry label="Type of fabric / bag"><SheetSelect value={draft.bagType} onChange={(bagType) => patch({ bagType })} options={options("BagType")} /></Entry>
          <Entry label="Fabric colour"><SheetCombobox value={draft.fabricColour} onChange={(fabricColour) => patch({ fabricColour })} options={options("Colour")} placeholder="Type or select colour" /></Entry>
          <Entry label={`Face width ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.faceWidthCm, draft.dimensionUnit)} onChange={(value) => patch({ faceWidthCm: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Gusset ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.gussetCm, draft.dimensionUnit)} onChange={(value) => patch({ gussetCm: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Length ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.lengthCm, draft.dimensionUnit)} onChange={(value) => patch({ lengthCm: toCm(value, draft.dimensionUnit) })} /></Entry>
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
          {forex ? (
            <Calc
              label={`${forex.sourceName} USD / INR (${forex.rateFrom.slice(0, 10)}${forex.usedFallback ? ", fallback" : ""})`}
              value={formatQty(forex.sourceDollar, 4)}
            />
          ) : null}
          <Entry label="USD / INR (forex − 2)"><SheetNum value={draft.usdInr} onChange={(usdInr) => patch({ usdInr })} /></Entry>
          {forex ? (
            <Calc
              label={`${forex.euroSourceName} USD per EUR (${forex.euroRateDate.slice(0, 10)}${forex.euroUsedFallback ? ", fallback" : ""})`}
              value={formatQty(forex.sourceEuroUsd, 4)}
            />
          ) : null}
          <Entry label="USD per EUR"><SheetNum value={draft.euroPerUsd} onChange={(euroPerUsd) => patch({ euroPerUsd })} /></Entry>
          {forex ? (
            <Calc
              label={`${forex.gbpSourceName} USD per GBP (${forex.gbpRateDate.slice(0, 10)}${forex.gbpUsedFallback ? ", fallback" : ""})`}
              value={formatQty(forex.sourceGbpUsd, 4)}
            />
          ) : null}
          <Entry label="USD per GBP"><SheetNum value={draft.gbpPerUsd} onChange={(gbpPerUsd) => patch({ gbpPerUsd })} /></Entry>
          <Entry label="Freight USD/kg"><SheetNum value={draft.freightUsdPerKg} onChange={(freightUsdPerKg) => patch({ freightUsdPerKg })} /></Entry>
          <Entry label="Commission USD/kg"><SheetNum value={draft.commissionUsdPerKg} onChange={(commissionUsdPerKg) => patch({ commissionUsdPerKg })} /></Entry>
          <Calc label="FOB USD/kg" value={formatQty(result.fobUsdPerKg, 4)} />
          <Calc label="CIF USD/kg" value={formatQty(result.cifUsdPerKg, 4)} />
          <Band>Customer offer</Band>
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

function dimensionSymbol(unit: string) {
  return unit.toLowerCase() === "inch" ? "in" : "cm";
}

function fromCm(value: number | null | undefined, unit: string) {
  if (value == null) return null;
  if (unit.toLowerCase() !== "inch") return value;
  return Math.round((value / 2.54) * 1_000_000) / 1_000_000;
}

function toCm(value: number | null, unit: string) {
  if (value == null) return null;
  return unit.toLowerCase() === "inch" ? value * 2.54 : value;
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
    <UiSelect value={value || undefined} onValueChange={onChange}>
      <SelectTrigger className="h-8 rounded-none border-0 bg-transparent px-3 shadow-none focus:ring-0">
        <SelectValue placeholder="Select…" />
      </SelectTrigger>
      <SelectContent>
        {options.filter(Boolean).map((option) => <SelectItem key={option} value={option}>{option}</SelectItem>)}
      </SelectContent>
    </UiSelect>
  );
}

function SheetCombobox({
  value,
  onChange,
  options,
  placeholder,
}: {
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder: string;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const exactMatch = options.some((option) => option.toLowerCase() === search.trim().toLowerCase());
  return (
    <Popover open={open} onOpenChange={(next) => { setOpen(next); if (next) setSearch(""); }}>
      <PopoverTrigger asChild>
        <button type="button" className="flex h-8 w-full items-center justify-between gap-2 bg-transparent px-3 text-left text-sm outline-none">
          <span className={value ? "truncate" : "truncate text-muted-foreground"}>{value || placeholder}</span>
          <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-50" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0" align="start">
        <Command shouldFilter>
          <CommandInput value={search} onValueChange={setSearch} placeholder={placeholder} />
          <CommandList>
            <CommandEmpty>No matching option.</CommandEmpty>
            <CommandGroup>
              {value ? (
                <CommandItem value="clear-current-value" onSelect={() => { onChange(""); setOpen(false); }}>
                  Clear value
                </CommandItem>
              ) : null}
              {search.trim() && !exactMatch ? (
                <CommandItem value={`use ${search}`} onSelect={() => { onChange(search.trim()); setOpen(false); }}>
                  Use “{search.trim()}”
                </CommandItem>
              ) : null}
              {options.filter(Boolean).map((option) => (
                <CommandItem key={option} value={option} onSelect={() => { onChange(option); setOpen(false); }}>
                  <Check className={value === option ? "opacity-100" : "opacity-0"} />
                  {option}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
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
          <Entry label="Input unit"><SheetSelect value={draft.dimensionUnit} onChange={(dimensionUnit) => patch({ dimensionUnit })} options={["CM", "Inch"]} /></Entry>
          <Entry label={`Width ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.adWidth, draft.dimensionUnit)} onChange={(value) => patch({ adWidth: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Length ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.adLength, draft.dimensionUnit)} onChange={(value) => patch({ adLength: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Patch ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.adPatch, draft.dimensionUnit)} onChange={(value) => patch({ adPatch: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Valve length ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.adValveLength, draft.dimensionUnit)} onChange={(value) => patch({ adValveLength: toCm(value, draft.dimensionUnit) })} /></Entry>
          <Entry label={`Extended valve ${dimensionSymbol(draft.dimensionUnit)}`}><SheetNum value={fromCm(draft.adExtendedValve, draft.dimensionUnit)} onChange={(value) => patch({ adExtendedValve: toCm(value, draft.dimensionUnit) })} /></Entry>
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

function MastersPanel({ masters, rates, onBack }: { masters: WovenMasters; rates: WovenRate[]; onBack: () => void }) {
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
    <section className="card-3d space-y-4 rounded-xl p-4 md:p-5">
      <div className="flex flex-wrap items-center gap-3">
        <Button size="sm" variant="outline" onClick={onBack}><ArrowLeft /> Back to quote list</Button>
        <div>
          <h2 className="font-semibold">Quotation masters</h2>
          <p className="text-xs text-muted-foreground">Manage rates, constants, customers, and lookup values.</p>
        </div>
      </div>
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

function ComboboxField({
  label,
  value,
  onChange,
  options,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder: string;
}) {
  return (
    <div className="space-y-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <div className="rounded-md border border-input bg-background shadow-sm">
        <SheetCombobox value={value} onChange={onChange} options={options} placeholder={placeholder} />
      </div>
    </div>
  );
}

function Select({
  label,
  value,
  onChange,
  options,
  labels = {},
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
  labels?: Record<string, string>;
}) {
  return (
    <div className="space-y-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <UiSelect value={value || "__all"} onValueChange={(next) => onChange(next === "__all" ? "" : next)}>
        <SelectTrigger className="bg-background">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {options.includes("") ? <SelectItem value="__all">All</SelectItem> : null}
          {options.filter(Boolean).map((option) => <SelectItem key={option} value={option}>{labels[option] ?? option}</SelectItem>)}
        </SelectContent>
      </UiSelect>
    </div>
  );
}
