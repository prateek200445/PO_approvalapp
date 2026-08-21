import { createFileRoute } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, RefreshCw, Save, Search, Settings2, Wand2 } from "lucide-react";
import { toast } from "sonner";
import { PlanningPageHeader, PlanningPageShell, PlanningPanel } from "@/components/planning/planning-ui";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useDebounce } from "@/hooks/useDebounce";
import {
  clearPlanningBacklog,
  createPlanningBacklog,
  deletePlanningDowntime,
  fetchInterUnitDefaults,
  fetchPlanningBacklog,
  fetchPlanningDowntime,
  fetchPlanningFactoryConfig,
  fetchLoomPreferenceChart,
  fetchPlanningLines,
  fetchPlanningLoomPool,
  fetchPlanningSetupConstants,
  fetchPlanningTeamFactors,
  importPlanningLinesFromErp,
  recalculatePlanningTeamFactors,
  savePlanningDowntime,
  saveLoomPreferenceChart,
  savePlanningFactoryConfig,
  savePlanningLines,
  savePlanningLoomPool,
  savePlanningTeamFactors,
  saveInterUnitDefaults,
  searchPlanningFactories,
} from "@/lib/planning/setup-api";
import {
  LOOM_POOL_INCLUDE_MODES,
  applyLoomPoolIncludeMode,
  inferLoomPoolIncludeMode,
  loomIncludedForPoolMode,
  type LoomPoolIncludeMode,
  type PlanningDowntime,
  type PlanningFactoryConfig,
  type PlanningFactoryOption,
  type PlanningInterUnitDefaults,
  type PlanningLineConfig,
  type PlanningLoomPool,
  type PlanningLoomPreferenceChart,
  type PlanningTeamFactor,
} from "@/lib/planning/setup-types";

export const Route = createFileRoute("/_app/planning/setup/")({
  head: () => ({ meta: [{ title: "Planning Setup — PO Portal" }] }),
  component: PlanningSetupPage,
});

const DEFAULT_COMPANY = "Plastene India Limited (Unit -II)";

function PlanningSetupPage() {
  const queryClient = useQueryClient();
  const [company, setCompany] = useState(DEFAULT_COMPANY);
  const [factoryQuery, setFactoryQuery] = useState("");
  const debouncedFactoryQuery = useDebounce(factoryQuery, 300);
  const [tab, setTab] = useState("factory");
  const [lineDraft, setLineDraft] = useState<PlanningLineConfig[]>([]);
  const [loomDraft, setLoomDraft] = useState<PlanningLoomPool[]>([]);
  const [factorDraft, setFactorDraft] = useState<PlanningTeamFactor[]>([]);
  const [factoryDraft, setFactoryDraft] = useState<PlanningFactoryConfig | null>(null);
  const [loomFilter, setLoomFilter] = useState("");
  const [poolIncludeMode, setPoolIncludeMode] = useState<LoomPoolIncludeMode>("DomesticOnly");
  const [backlogForm, setBacklogForm] = useState({ lineNo: 1, shift: "A", orderNo: "", backlogQty: "", reason: "" });
  const [downtimeForm, setDowntimeForm] = useState({
    planDate: new Date().toISOString().slice(0, 10),
    lineNo: 0,
    shift: "",
    reason: "",
    capacityFactor: "1",
  });
  const [downtimeDraft, setDowntimeDraft] = useState<PlanningDowntime[]>([]);
  const [prefDraft, setPrefDraft] = useState<PlanningLoomPreferenceChart[]>([]);
  const [interUnitDraft, setInterUnitDraft] = useState<PlanningInterUnitDefaults | null>(null);
  const [supplyFactoryQuery, setSupplyFactoryQuery] = useState("");
  const debouncedSupplyFactoryQuery = useDebounce(supplyFactoryQuery, 300);

  const { data: constants } = useQuery({
    queryKey: ["planning-setup-constants"],
    queryFn: fetchPlanningSetupConstants,
  });

  const { data: factoryOptions = [] } = useQuery({
    queryKey: ["planning-factory-search", debouncedFactoryQuery],
    queryFn: () => searchPlanningFactories(debouncedFactoryQuery),
  });

  const { data: factoryConfig, isLoading: factoryLoading } = useQuery({
    queryKey: ["planning-factory-config", company],
    queryFn: () => fetchPlanningFactoryConfig(company),
    enabled: !!company,
  });

  const { data: lines = [], isLoading: linesLoading } = useQuery({
    queryKey: ["planning-setup-lines", company],
    queryFn: () => fetchPlanningLines(company),
    enabled: !!company,
  });

  const { data: looms = [], isLoading: loomsLoading } = useQuery({
    queryKey: ["planning-setup-looms", company],
    queryFn: () => fetchPlanningLoomPool(company),
    enabled: !!company,
  });

  const { data: factors = [], isLoading: factorsLoading, refetch: refetchFactors } = useQuery({
    queryKey: ["planning-setup-factors", company],
    queryFn: () => fetchPlanningTeamFactors(company),
    enabled: !!company,
  });

  const { data: backlog = [], refetch: refetchBacklog } = useQuery({
    queryKey: ["planning-setup-backlog", company],
    queryFn: () => fetchPlanningBacklog(company, "Open"),
    enabled: !!company,
  });

  const { data: downtime = [], isLoading: downtimeLoading, refetch: refetchDowntime } = useQuery({
    queryKey: ["planning-setup-downtime", company],
    queryFn: () => fetchPlanningDowntime(company),
    enabled: !!company,
  });

  const { data: loomPreference = [], isLoading: prefLoading, refetch: refetchPref } = useQuery({
    queryKey: ["planning-loom-preference", company],
    queryFn: () => fetchLoomPreferenceChart(company),
    enabled: !!company,
  });

  const { data: interUnitDefaults, isLoading: interUnitLoading } = useQuery({
    queryKey: ["planning-inter-unit", company],
    queryFn: () => fetchInterUnitDefaults(company),
    enabled: !!company,
  });

  const { data: supplyFactoryOptions = [] } = useQuery({
    queryKey: ["planning-supply-factory-search", debouncedSupplyFactoryQuery],
    queryFn: () => searchPlanningFactories(debouncedSupplyFactoryQuery),
  });

  useEffect(() => {
    if (factoryConfig) setFactoryDraft(factoryConfig);
  }, [factoryConfig]);

  useEffect(() => {
    if (interUnitDefaults) setInterUnitDraft(interUnitDefaults);
  }, [interUnitDefaults]);

  useEffect(() => {
    setLineDraft(lines);
  }, [lines]);

  useEffect(() => {
    setLoomDraft(looms);
    if (looms.length > 0) setPoolIncludeMode(inferLoomPoolIncludeMode(looms));
  }, [looms]);

  useEffect(() => {
    setFactorDraft(factors);
  }, [factors]);

  useEffect(() => {
    setDowntimeDraft(downtime);
  }, [downtime]);

  useEffect(() => {
    setPrefDraft(loomPreference);
  }, [loomPreference]);

  const saveFactoryMut = useMutation({
    mutationFn: savePlanningFactoryConfig,
    onSuccess: (saved) => {
      setFactoryDraft(saved);
      queryClient.invalidateQueries({ queryKey: ["planning-factory-config", company] });
      toast.success("Factory planning settings saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveInterUnitMut = useMutation({
    mutationFn: () => saveInterUnitDefaults({ ...interUnitDraft!, fibcCompanyName: company }),
    onSuccess: (saved) => {
      setInterUnitDraft(saved);
      queryClient.invalidateQueries({ queryKey: ["planning-inter-unit", company] });
      toast.success("Inter-unit defaults saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const importLinesMut = useMutation({
    mutationFn: () => importPlanningLinesFromErp(company),
    onSuccess: (imported) => {
      setLineDraft(imported);
      queryClient.invalidateQueries({ queryKey: ["planning-setup-lines", company] });
      toast.success(`Imported ${imported.length} line(s) from ERP.`);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveLinesMut = useMutation({
    mutationFn: () => savePlanningLines(company, lineDraft),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning-setup-lines", company] });
      toast.success("Line configuration saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveLoomsMut = useMutation({
    mutationFn: () => savePlanningLoomPool(company, loomDraft),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["planning-setup-looms", company] });
      toast.success("Loom pool saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const recalcFactorsMut = useMutation({
    mutationFn: () => recalculatePlanningTeamFactors(company, 30),
    onSuccess: (result) => {
      setFactorDraft(result.factors);
      queryClient.invalidateQueries({ queryKey: ["planning-setup-factors", company] });
      toast.success(result.message);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveFactorsMut = useMutation({
    mutationFn: () => savePlanningTeamFactors(company, factorDraft),
    onSuccess: () => {
      refetchFactors();
      toast.success("Team factors saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const createBacklogMut = useMutation({
    mutationFn: createPlanningBacklog,
    onSuccess: () => {
      refetchBacklog();
      setBacklogForm({ lineNo: 1, shift: "A", orderNo: "", backlogQty: "", reason: "" });
      toast.success("Backlog recorded.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const clearBacklogMut = useMutation({
    mutationFn: clearPlanningBacklog,
    onSuccess: () => {
      refetchBacklog();
      toast.success("Backlog cleared.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const saveDowntimeMut = useMutation({
    mutationFn: () => savePlanningDowntime(company, downtimeDraft),
    onSuccess: () => {
      refetchDowntime();
      toast.success("Downtime entries saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const deleteDowntimeMut = useMutation({
    mutationFn: deletePlanningDowntime,
    onSuccess: () => {
      refetchDowntime();
      toast.success("Downtime entry removed.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const savePrefMut = useMutation({
    mutationFn: () => saveLoomPreferenceChart(company, prefDraft),
    onSuccess: () => {
      refetchPref();
      toast.success("Loom preference chart saved.");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const selectFactory = useCallback((opt: PlanningFactoryOption) => {
    setCompany(opt.companyName);
    setFactoryQuery(opt.companyName);
  }, []);

  const filteredLooms = useMemo(() => {
    const q = loomFilter.trim().toLowerCase();
    if (!q) return loomDraft;
    return loomDraft.filter(
      (l) =>
        String(l.loomNo).includes(q) ||
        (l.erpLoomCode ?? "").toLowerCase().includes(q) ||
        (l.loomType ?? "").toLowerCase().includes(q) ||
        (l.erpMake ?? "").toLowerCase().includes(q),
    );
  }, [loomDraft, loomFilter]);

  const loomStats = useMemo(() => {
    const included = loomDraft.filter((l) => l.includeInPlanning).length;
    return { total: loomDraft.length, included, excluded: loomDraft.length - included };
  }, [loomDraft]);

  const updateLine = (lineNo: number, patch: Partial<PlanningLineConfig>) => {
    setLineDraft((prev) => prev.map((l) => (l.lineNo === lineNo ? { ...l, ...patch } : l)));
  };

  const toggleBagFamily = (lineNo: number, family: string, checked: boolean) => {
    setLineDraft((prev) =>
      prev.map((l) => {
        if (l.lineNo !== lineNo) return l;
        const set = new Set(l.allowedBagFamilies);
        if (checked) set.add(family);
        else set.delete(family);
        return { ...l, allowedBagFamilies: Array.from(set) };
      }),
    );
  };

  const updateLoom = (loomNo: number, patch: Partial<PlanningLoomPool>) => {
    setLoomDraft((prev) => prev.map((l) => (l.loomNo === loomNo ? { ...l, ...patch } : l)));
  };

  const handlePoolIncludeModeChange = (mode: LoomPoolIncludeMode) => {
    setPoolIncludeMode(mode);
    setLoomDraft((prev) => applyLoomPoolIncludeMode(prev, mode));
  };

  const updateFactor = (index: number, patch: Partial<PlanningTeamFactor>) => {
    setFactorDraft((prev) => prev.map((f, i) => (i === index ? { ...f, ...patch } : f)));
  };

  return (
    <PlanningPageShell>
      <PlanningPageHeader
        title="Planning Setup"
        description="Configure factories, FIBC lines, loom pools, team capacity factors, and line+shift backlog — per unit."
        backTo="/profile"
        actions={
          <Badge variant="outline" className="font-normal">
            Phase 0 — master data
          </Badge>
        }
      />

      <PlanningPanel title="Factory" subtitle="Search FactoryInfo and select the unit to configure">
        <div className="grid gap-4 md:grid-cols-[1fr_280px]">
          <div className="space-y-3">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="pl-9"
                placeholder="Search factory name or group…"
                value={factoryQuery}
                onChange={(e) => setFactoryQuery(e.target.value)}
              />
            </div>
            <div className="max-h-48 overflow-y-auto rounded-lg border border-border">
              {factoryOptions.length === 0 ? (
                <p className="p-3 text-sm text-muted-foreground">Type to search factories.</p>
              ) : (
                factoryOptions.map((opt) => (
                  <button
                    key={`${opt.factoryInfoSrNo}-${opt.companyName}`}
                    type="button"
                    onClick={() => selectFactory(opt)}
                    className={`flex w-full items-start justify-between gap-2 border-b border-border/60 px-3 py-2 text-left text-sm last:border-0 hover:bg-secondary/60 ${
                      company === opt.companyName ? "bg-sky-500/10" : ""
                    }`}
                  >
                    <span>
                      <span className="font-medium">{opt.companyName}</span>
                      {opt.groupName ? (
                        <span className="mt-0.5 block text-xs text-muted-foreground">{opt.groupName}</span>
                      ) : null}
                    </span>
                    <span className="flex shrink-0 gap-1">
                      {opt.hasLineMaster ? <Badge variant="secondary">Lines</Badge> : null}
                      {opt.hasLoomMaster ? <Badge variant="secondary">Looms</Badge> : null}
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>
          <div className="rounded-lg border border-border bg-surface/50 p-3 text-sm">
            <div className="font-medium">Active factory</div>
            <p className="mt-1 break-words text-muted-foreground">{company || "—"}</p>
            {factoryLoading ? (
              <Loader2 className="mt-3 h-4 w-4 animate-spin text-muted-foreground" />
            ) : factoryDraft ? (
              <dl className="mt-3 space-y-1 text-xs">
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Planning enabled</dt>
                  <dd>{factoryDraft.isPlanningEnabled ? "Yes" : "No"}</dd>
                </div>
                <div className="flex justify-between gap-2">
                  <dt className="text-muted-foreground">Dispatch buffer</dt>
                  <dd>{factoryDraft.defaultDispatchBufferDays} days</dd>
                </div>
              </dl>
            ) : null}
          </div>
        </div>
      </PlanningPanel>

      <Tabs value={tab} onValueChange={setTab} className="mt-6">
        <TabsList className="mb-4 flex h-auto flex-wrap gap-1">
          <TabsTrigger value="factory">Factory settings</TabsTrigger>
          <TabsTrigger value="lines">FIBC lines</TabsTrigger>
          <TabsTrigger value="looms">Loom pool</TabsTrigger>
          <TabsTrigger value="loom-pref">Loom preference</TabsTrigger>
          <TabsTrigger value="teams">Team factors</TabsTrigger>
          <TabsTrigger value="downtime">Downtime</TabsTrigger>
          <TabsTrigger value="inter-unit">Inter-unit</TabsTrigger>
          <TabsTrigger value="backlog">Backlog</TabsTrigger>
        </TabsList>

        <TabsContent value="factory">
          <PlanningPanel title="Factory planning settings">
            {factoryDraft ? (
              <div className="grid gap-4 md:grid-cols-2">
                <label className="space-y-1.5 text-sm">
                  <span className="font-medium">Planning enabled</span>
                  <div className="flex items-center gap-2 pt-1">
                    <Checkbox
                      checked={factoryDraft.isPlanningEnabled}
                      onCheckedChange={(v) => setFactoryDraft({ ...factoryDraft, isPlanningEnabled: v === true })}
                    />
                    <span className="text-muted-foreground">Use this factory in planning portal</span>
                  </div>
                </label>
                <label className="space-y-1.5 text-sm">
                  <span className="font-medium">Default dispatch buffer (days)</span>
                  <Input
                    type="number"
                    min={0}
                    value={factoryDraft.defaultDispatchBufferDays}
                    onChange={(e) =>
                      setFactoryDraft({ ...factoryDraft, defaultDispatchBufferDays: Number(e.target.value) || 0 })
                    }
                  />
                </label>
                <label className="space-y-1.5 text-sm">
                  <span className="font-medium">Default rejection % (planning haircut)</span>
                  <Input
                    type="number"
                    min={0}
                    max={50}
                    step={0.1}
                    value={factoryDraft.defaultRejectionPercent}
                    onChange={(e) =>
                      setFactoryDraft({ ...factoryDraft, defaultRejectionPercent: Number(e.target.value) || 0 })
                    }
                  />
                </label>
                <label className="space-y-1.5 text-sm md:col-span-2">
                  <span className="font-medium">Notes</span>
                  <Input
                    value={factoryDraft.notes ?? ""}
                    onChange={(e) => setFactoryDraft({ ...factoryDraft, notes: e.target.value })}
                    placeholder="Optional notes for planners"
                  />
                </label>
                <div className="md:col-span-2">
                  <Button
                    onClick={() =>
                      saveFactoryMut.mutate({
                        ...factoryDraft,
                        companyName: company,
                        factoryInfoSrNo:
                          factoryOptions.find((f) => f.companyName === company)?.factoryInfoSrNo ??
                          factoryDraft.factoryInfoSrNo,
                      })
                    }
                    disabled={saveFactoryMut.isPending}
                  >
                    {saveFactoryMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                    Save factory settings
                  </Button>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Select a factory above.</p>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="lines">
          <PlanningPanel
            title="FIBC line mapping"
            subtitle="Bag families, dust capacities, team link — per factory (ERP NewLineMaster merged with portal overrides)"
            headerRight={
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={() => importLinesMut.mutate()} disabled={importLinesMut.isPending}>
                  {importLinesMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Wand2 className="h-4 w-4" />}
                  Import from ERP
                </Button>
                <Button size="sm" onClick={() => saveLinesMut.mutate()} disabled={saveLinesMut.isPending}>
                  {saveLinesMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Save lines
                </Button>
              </div>
            }
          >
            {linesLoading ? (
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            ) : lineDraft.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No lines in ERP for this factory. Add rows in NewLineMaster or configure manually after import from another unit template.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[960px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">ERP bag type</th>
                      <th className="px-2 py-2">Bag families</th>
                      <th className="px-2 py-2">Normal</th>
                      <th className="px-2 py-2">1-dust</th>
                      <th className="px-2 py-2">2-dust</th>
                      <th className="px-2 py-2">3-dust</th>
                      <th className="px-2 py-2">Buffer</th>
                      <th className="px-2 py-2">TeamNo</th>
                      <th className="px-2 py-2">Active</th>
                    </tr>
                  </thead>
                  <tbody>
                    {lineDraft.map((line) => (
                      <tr key={line.lineNo} className="border-b border-border/60 align-top">
                        <td className="px-2 py-2 font-medium">{line.lineNo}</td>
                        <td className="px-2 py-2 text-xs text-muted-foreground">{line.erpBagType ?? "—"}</td>
                        <td className="px-2 py-2">
                          <div className="flex flex-wrap gap-2">
                            {(constants?.bagFamilies ?? ["UPanel", "Buffle", "Circular"]).map((family) => (
                              <label key={family} className="flex items-center gap-1 text-xs">
                                <Checkbox
                                  checked={line.allowedBagFamilies.includes(family)}
                                  onCheckedChange={(v) => toggleBagFamily(line.lineNo, family, v === true)}
                                />
                                {family}
                              </label>
                            ))}
                          </div>
                        </td>
                        {(["capacityNormal", "capacitySingleDust", "capacityDoubleDust", "capacityTripleDust"] as const).map(
                          (field) => (
                            <td key={field} className="px-2 py-2">
                              <Input
                                className="h-8 w-20"
                                type="number"
                                value={line[field] ?? ""}
                                onChange={(e) =>
                                  updateLine(line.lineNo, { [field]: e.target.value ? Number(e.target.value) : null })
                                }
                              />
                            </td>
                          ),
                        )}
                        <td className="px-2 py-2">
                          <Input
                            className="h-8 w-16"
                            type="number"
                            value={line.bufferDaysOverride ?? ""}
                            onChange={(e) =>
                              updateLine(line.lineNo, {
                                bufferDaysOverride: e.target.value ? Number(e.target.value) : null,
                              })
                            }
                          />
                        </td>
                        <td className="px-2 py-2">
                          <Input
                            className="h-8 w-20"
                            value={line.teamNo ?? ""}
                            onChange={(e) => updateLine(line.lineNo, { teamNo: e.target.value || null })}
                          />
                        </td>
                        <td className="px-2 py-2">
                          <Checkbox
                            checked={line.isActive}
                            onCheckedChange={(v) => updateLine(line.lineNo, { isActive: v === true })}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="looms">
          <PlanningPanel
            title="Loom planning pool"
            subtitle={`${loomStats.included} of ${loomStats.total} looms included for planning`}
            headerRight={
              <div className="flex flex-wrap items-center gap-2">
                <label className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span className="whitespace-nowrap">Planning pool</span>
                  <select
                    className="h-8 rounded-md border border-input bg-background px-2 text-xs text-foreground"
                    value={poolIncludeMode}
                    onChange={(e) => handlePoolIncludeModeChange(e.target.value as LoomPoolIncludeMode)}
                  >
                    {LOOM_POOL_INCLUDE_MODES.map((m) => (
                      <option key={m.value} value={m.value}>
                        {m.label}
                      </option>
                    ))}
                  </select>
                </label>
                <Button size="sm" onClick={() => saveLoomsMut.mutate()} disabled={saveLoomsMut.isPending}>
                  {saveLoomsMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Save pool
                </Button>
              </div>
            }
          >
            <p className="mb-3 text-xs text-muted-foreground">
              Tag each loom as DomesticFibc or Export in the Purpose column, then choose which group is included above.
              Maintenance and Other are always excluded unless you manually check Include.
            </p>
            <Input
              className="mb-4 max-w-sm"
              placeholder="Filter looms…"
              value={loomFilter}
              onChange={(e) => setLoomFilter(e.target.value)}
            />
            {loomsLoading ? (
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            ) : (
              <div className="max-h-[520px] overflow-auto rounded-lg border border-border">
                <table className="w-full min-w-[880px] text-left text-sm">
                  <thead className="sticky top-0 bg-card">
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">#</th>
                      <th className="px-2 py-2">Code</th>
                      <th className="px-2 py-2">Make</th>
                      <th className="px-2 py-2">Type</th>
                      <th className="px-2 py-2">Include</th>
                      <th className="px-2 py-2">Purpose</th>
                      <th className="px-2 py-2">Winder</th>
                      <th className="px-2 py-2">Width cm</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredLooms.map((loom) => (
                      <tr key={loom.loomNo} className="border-b border-border/60">
                        <td className="px-2 py-1.5">{loom.loomNo}</td>
                        <td className="px-2 py-1.5 text-xs">{loom.erpLoomCode ?? "—"}</td>
                        <td className="px-2 py-1.5 text-xs">{loom.erpMake ?? "—"}</td>
                        <td className="px-2 py-1.5">
                          <Input
                            className="h-8 w-24"
                            value={loom.loomType ?? ""}
                            onChange={(e) => updateLoom(loom.loomNo, { loomType: e.target.value || null })}
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <Checkbox
                            checked={loom.includeInPlanning}
                            onCheckedChange={(v) => updateLoom(loom.loomNo, { includeInPlanning: v === true })}
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <select
                            className="h-8 rounded-md border border-input bg-background px-2 text-xs"
                            value={loom.poolPurpose}
                            onChange={(e) => {
                              const poolPurpose = e.target.value;
                              updateLoom(loom.loomNo, {
                                poolPurpose,
                                includeInPlanning: loomIncludedForPoolMode(poolPurpose, poolIncludeMode),
                              });
                            }}
                          >
                            {(constants?.poolPurposes ?? ["DomesticFibc", "Export", "Other", "Maintenance"]).map((p) => (
                              <option key={p} value={p}>
                                {p}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          <select
                            className="h-8 rounded-md border border-input bg-background px-2 text-xs"
                            value={loom.winderCategory}
                            onChange={(e) => updateLoom(loom.loomNo, { winderCategory: e.target.value })}
                          >
                            {(constants?.winderCategories ?? ["Tube", "FlatDouble", "FlatTriple"]).map((w) => (
                              <option key={w} value={w}>
                                {w}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5 text-xs text-muted-foreground">
                          {loom.erpMinSize ?? "—"} – {loom.erpMaxSize ?? "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="loom-pref">
          <PlanningPanel
            title="Loom preference chart"
            subtitle="Fabric form × GSM × width → loom type rank (GSM ≤182 = Tube). Used by loom planning engine for cases i–iv and changeover tiers."
            headerRight={
              <Button size="sm" onClick={() => savePrefMut.mutate()} disabled={savePrefMut.isPending}>
                {savePrefMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                Save chart
              </Button>
            }
          >
            {prefLoading ? (
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[900px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Form</th>
                      <th className="px-2 py-2">GSM</th>
                      <th className="px-2 py-2">Width cm</th>
                      <th className="px-2 py-2">Rank</th>
                      <th className="px-2 py-2">Loom type</th>
                      <th className="px-2 py-2">Winder</th>
                      <th className="px-2 py-2">Tier</th>
                      <th className="px-2 py-2">Notes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {prefDraft.map((row, i) => (
                      <tr key={`${row.chartId ?? "new"}-${i}`} className="border-b border-border/60">
                        <td className="px-2 py-1.5">
                          <select
                            className="h-8 rounded-md border border-border bg-background px-2 text-xs"
                            value={row.fabricForm}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, fabricForm: e.target.value } : r)),
                              )
                            }
                          >
                            {(constants?.fabricForms ?? ["Tube", "Flat"]).map((f) => (
                              <option key={f} value={f}>
                                {f}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5 text-xs">
                          <Input
                            className="mb-1 h-7 w-16"
                            type="number"
                            value={row.gsmMin}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, gsmMin: Number(e.target.value) } : r)),
                              )
                            }
                          />
                          <Input
                            className="h-7 w-16"
                            type="number"
                            value={row.gsmMax}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, gsmMax: Number(e.target.value) } : r)),
                              )
                            }
                          />
                        </td>
                        <td className="px-2 py-1.5 text-xs">
                          <Input
                            className="mb-1 h-7 w-16"
                            type="number"
                            value={row.widthMinCm}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, widthMinCm: Number(e.target.value) } : r)),
                              )
                            }
                          />
                          <Input
                            className="h-7 w-16"
                            type="number"
                            value={row.widthMaxCm}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, widthMaxCm: Number(e.target.value) } : r)),
                              )
                            }
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <Input
                            className="h-8 w-14"
                            type="number"
                            min={1}
                            value={row.preferenceRank}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, preferenceRank: Number(e.target.value) || 1 } : r)),
                              )
                            }
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <Input
                            className="h-8 w-24"
                            value={row.loomType}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, loomType: e.target.value.toUpperCase() } : r)),
                              )
                            }
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <select
                            className="h-8 rounded-md border border-border bg-background px-2 text-xs"
                            value={row.winderCategory}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, winderCategory: e.target.value } : r)),
                              )
                            }
                          >
                            {(constants?.winderCategories ?? ["Tube", "FlatDouble", "FlatTriple"]).map((w) => (
                              <option key={w} value={w}>
                                {w}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          <select
                            className="h-8 rounded-md border border-border bg-background px-2 text-xs"
                            value={row.changeoverTier}
                            onChange={(e) =>
                              setPrefDraft((prev) =>
                                prev.map((r, idx) => (idx === i ? { ...r, changeoverTier: e.target.value } : r)),
                              )
                            }
                          >
                            {(constants?.changeoverTiers ?? ["Blue", "White"]).map((t) => (
                              <option key={t} value={t}>
                                {t}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5 text-xs text-muted-foreground">{row.notes ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="teams">
          <PlanningPanel
            title="Team / contractor capacity factors"
            subtitle="Auto from FIBCTeamWiseProduction when history exists; manual override always wins"
            headerRight={
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={() => recalcFactorsMut.mutate()} disabled={recalcFactorsMut.isPending}>
                  {recalcFactorsMut.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <RefreshCw className="h-4 w-4" />
                  )}
                  Recalculate (30d)
                </Button>
                <Button size="sm" onClick={() => saveFactorsMut.mutate()} disabled={saveFactorsMut.isPending}>
                  {saveFactorsMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Save factors
                </Button>
              </div>
            }
          >
            {factorsLoading ? (
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            ) : factorDraft.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No factors yet. Link TeamNo on lines, then click Recalculate to pull history from ERP.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[720px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">Shift</th>
                      <th className="px-2 py-2">Team</th>
                      <th className="px-2 py-2">Auto</th>
                      <th className="px-2 py-2">Manual</th>
                      <th className="px-2 py-2">Effective</th>
                      <th className="px-2 py-2">Source</th>
                      <th className="px-2 py-2">Avg pcs/day</th>
                    </tr>
                  </thead>
                  <tbody>
                    {factorDraft.map((f, i) => (
                      <tr key={`${f.teamNo}-${f.lineNo}-${f.shift}-${i}`} className="border-b border-border/60">
                        <td className="px-2 py-2">{f.lineNo || "—"}</td>
                        <td className="px-2 py-2">{f.shift ?? "All"}</td>
                        <td className="px-2 py-2 font-medium">{f.teamNo}</td>
                        <td className="px-2 py-2">{f.autoFactor?.toFixed(3) ?? "—"}</td>
                        <td className="px-2 py-2">
                          <Input
                            className="h-8 w-20"
                            type="number"
                            step={0.01}
                            min={0}
                            value={f.manualFactor ?? ""}
                            onChange={(e) =>
                              updateFactor(i, {
                                manualFactor: e.target.value ? Number(e.target.value) : null,
                              })
                            }
                          />
                        </td>
                        <td className="px-2 py-2">{f.effectiveFactor.toFixed(3)}</td>
                        <td className="px-2 py-2">
                          <Badge variant="outline">{f.factorSource}</Badge>
                        </td>
                        <td className="px-2 py-2 text-xs text-muted-foreground">
                          {f.sampleProductionPcs?.toFixed(0) ?? "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="downtime">
          <PlanningPanel
            title="Planned downtime"
            subtitle="Reduce effective capacity by date, line, and shift (0 = line down, 0.5 = half shift, 1 = no impact)"
            headerRight={
              <Button size="sm" onClick={() => saveDowntimeMut.mutate()} disabled={saveDowntimeMut.isPending}>
                {saveDowntimeMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                Save downtime
              </Button>
            }
          >
            <div className="mb-4 grid gap-3 md:grid-cols-6">
              <Input
                type="date"
                value={downtimeForm.planDate}
                onChange={(e) => setDowntimeForm({ ...downtimeForm, planDate: e.target.value })}
              />
              <Input
                type="number"
                placeholder="Line # (0 = all)"
                value={downtimeForm.lineNo}
                onChange={(e) => setDowntimeForm({ ...downtimeForm, lineNo: Number(e.target.value) || 0 })}
              />
              <Input
                placeholder="Shift (blank = all)"
                value={downtimeForm.shift}
                onChange={(e) => setDowntimeForm({ ...downtimeForm, shift: e.target.value.toUpperCase() })}
              />
              <Input
                type="number"
                step={0.1}
                min={0}
                max={1}
                placeholder="Factor"
                value={downtimeForm.capacityFactor}
                onChange={(e) => setDowntimeForm({ ...downtimeForm, capacityFactor: e.target.value })}
              />
              <Input
                className="md:col-span-2"
                placeholder="Reason"
                value={downtimeForm.reason}
                onChange={(e) => setDowntimeForm({ ...downtimeForm, reason: e.target.value })}
              />
            </div>
            <Button
              variant="outline"
              size="sm"
              className="mb-4"
              onClick={() => {
                const entry: PlanningDowntime = {
                  companyName: company,
                  planDate: downtimeForm.planDate,
                  lineNo: downtimeForm.lineNo,
                  shift: downtimeForm.shift || null,
                  reason: downtimeForm.reason || "Planned downtime",
                  capacityFactor: Number(downtimeForm.capacityFactor) || 0,
                };
                setDowntimeDraft((prev) => [...prev, entry]);
                setDowntimeForm((f) => ({ ...f, reason: "", capacityFactor: "1" }));
              }}
              disabled={!downtimeForm.planDate}
            >
              Add to list
            </Button>
            {downtimeLoading ? (
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            ) : downtimeDraft.length === 0 ? (
              <p className="text-sm text-muted-foreground">No downtime entries for this factory.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted-foreground">
                      <th className="px-2 py-2">Date</th>
                      <th className="px-2 py-2">Line</th>
                      <th className="px-2 py-2">Shift</th>
                      <th className="px-2 py-2">Factor</th>
                      <th className="px-2 py-2">Reason</th>
                      <th className="px-2 py-2" />
                    </tr>
                  </thead>
                  <tbody>
                    {downtimeDraft.map((d, i) => (
                      <tr key={`${d.downtimeId ?? "new"}-${d.planDate}-${i}`} className="border-b border-border/60">
                        <td className="px-2 py-1.5">{d.planDate}</td>
                        <td className="px-2 py-1.5">{d.lineNo || "All"}</td>
                        <td className="px-2 py-1.5">{d.shift || "All"}</td>
                        <td className="px-2 py-1.5">
                          <Input
                            className="h-8 w-20"
                            type="number"
                            step={0.1}
                            min={0}
                            max={1}
                            value={d.capacityFactor}
                            onChange={(e) =>
                              setDowntimeDraft((prev) =>
                                prev.map((row, idx) =>
                                  idx === i ? { ...row, capacityFactor: Number(e.target.value) || 0 } : row,
                                ),
                              )
                            }
                          />
                        </td>
                        <td className="px-2 py-1.5">{d.reason}</td>
                        <td className="px-2 py-1.5 text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              if (d.downtimeId) deleteDowntimeMut.mutate(d.downtimeId);
                              setDowntimeDraft((prev) => prev.filter((_, idx) => idx !== i));
                            }}
                          >
                            Remove
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="inter-unit">
          <PlanningPanel
            title="Inter-unit / ICO defaults"
            subtitle={`FIBC hub: ${company} — configure fabric supply factory for sister-unit weaving`}
          >
            {interUnitLoading || !interUnitDraft ? (
              <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading inter-unit settings…
              </div>
            ) : (
              <div className="grid gap-4 md:grid-cols-2">
                <label className="space-y-1.5 text-sm md:col-span-2">
                  <span className="font-medium">Default fabric supply factory (loom weaving)</span>
                  <Input
                    value={interUnitDraft.defaultFabricSupplyCompany ?? ""}
                    onChange={(e) =>
                      setInterUnitDraft({
                        ...interUnitDraft,
                        defaultFabricSupplyCompany: e.target.value || null,
                      })
                    }
                    placeholder="Sister unit with looms (e.g. KPW / Unit-I)"
                  />
                  <Input
                    className="mt-2"
                    value={supplyFactoryQuery}
                    onChange={(e) => setSupplyFactoryQuery(e.target.value)}
                    placeholder="Search factories…"
                  />
                  {supplyFactoryOptions.filter((o) => o.hasLoomMaster).length > 0 ? (
                    <div className="mt-1 max-h-32 overflow-auto rounded border border-border">
                      {supplyFactoryOptions
                        .filter((o) => o.hasLoomMaster)
                        .map((opt) => (
                          <button
                            key={opt.companyName}
                            type="button"
                            className="block w-full border-b border-border/60 px-2 py-1.5 text-left text-xs hover:bg-muted/50 last:border-0"
                            onClick={() => {
                              setInterUnitDraft({
                                ...interUnitDraft,
                                defaultFabricSupplyCompany: opt.companyName,
                              });
                              setSupplyFactoryQuery("");
                            }}
                          >
                            {opt.companyName}
                          </button>
                        ))}
                    </div>
                  ) : null}
                </label>
                <label className="space-y-1.5 text-sm">
                  <span className="font-medium">Transfer buffer (days)</span>
                  <Input
                    type="number"
                    min={0}
                    value={interUnitDraft.defaultTransferBufferDays}
                    onChange={(e) =>
                      setInterUnitDraft({
                        ...interUnitDraft,
                        defaultTransferBufferDays: Number(e.target.value) || 0,
                      })
                    }
                  />
                  <p className="text-xs text-muted-foreground">Fabric travel time from supply factory to FIBC factory.</p>
                </label>
                <label className="space-y-1.5 text-sm">
                  <span className="font-medium">Auto-detect Sulzer / ICO from BOM</span>
                  <div className="flex items-center gap-2 pt-1">
                    <Checkbox
                      checked={interUnitDraft.autoDetectSulzerFabric}
                      onCheckedChange={(v) =>
                        setInterUnitDraft({ ...interUnitDraft, autoDetectSulzerFabric: v === true })
                      }
                    />
                    <span className="text-muted-foreground">Use supply factory when BOM mentions Sulzer fabric</span>
                  </div>
                </label>
                <label className="space-y-1.5 text-sm md:col-span-2">
                  <span className="font-medium">Notes</span>
                  <Input
                    value={interUnitDraft.notes ?? ""}
                    onChange={(e) => setInterUnitDraft({ ...interUnitDraft, notes: e.target.value || null })}
                    placeholder="Demo / ICO policy notes"
                  />
                </label>
                <div className="md:col-span-2">
                  <Button
                    onClick={() => saveInterUnitMut.mutate()}
                    disabled={saveInterUnitMut.isPending}
                  >
                    {saveInterUnitMut.isPending ? (
                      <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                    ) : (
                      <Save className="mr-1 h-4 w-4" />
                    )}
                    Save inter-unit defaults
                  </Button>
                </div>
              </div>
            )}
          </PlanningPanel>
        </TabsContent>

        <TabsContent value="backlog">
          <PlanningPanel title="Line + shift backlog" subtitle="Incomplete production reserved before next order on same line/shift">
            <div className="mb-4 grid gap-3 md:grid-cols-5">
              <Input
                type="number"
                placeholder="Line #"
                value={backlogForm.lineNo}
                onChange={(e) => setBacklogForm({ ...backlogForm, lineNo: Number(e.target.value) || 1 })}
              />
              <Input
                placeholder="Shift (A/B/C)"
                value={backlogForm.shift}
                onChange={(e) => setBacklogForm({ ...backlogForm, shift: e.target.value.toUpperCase() })}
              />
              <Input
                placeholder="Order no"
                value={backlogForm.orderNo}
                onChange={(e) => setBacklogForm({ ...backlogForm, orderNo: e.target.value })}
              />
              <Input
                type="number"
                placeholder="Backlog qty"
                value={backlogForm.backlogQty}
                onChange={(e) => setBacklogForm({ ...backlogForm, backlogQty: e.target.value })}
              />
              <Button
                onClick={() =>
                  createBacklogMut.mutate({
                    companyName: company,
                    lineNo: backlogForm.lineNo,
                    shift: backlogForm.shift,
                    orderNo: backlogForm.orderNo,
                    backlogQty: Number(backlogForm.backlogQty) || 0,
                    reason: backlogForm.reason || "Manual entry",
                  })
                }
                disabled={createBacklogMut.isPending || !backlogForm.orderNo || !backlogForm.backlogQty}
              >
                Add backlog
              </Button>
            </div>
            <Input
              className="mb-4"
              placeholder="Reason (optional)"
              value={backlogForm.reason}
              onChange={(e) => setBacklogForm({ ...backlogForm, reason: e.target.value })}
            />
            {backlog.length === 0 ? (
              <p className="text-sm text-muted-foreground">No open backlog for this factory.</p>
            ) : (
              <div className="space-y-2">
                {backlog.map((b) => (
                  <div
                    key={b.backlogId}
                    className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border px-3 py-2 text-sm"
                  >
                    <div>
                      <span className="font-medium">
                        Line {b.lineNo} · Shift {b.shift}
                      </span>
                      <span className="mx-2 text-muted-foreground">·</span>
                      <span>{b.orderNo}</span>
                      <span className="mx-2 text-muted-foreground">·</span>
                      <span>{b.backlogQty} pcs</span>
                      {b.reason ? <p className="text-xs text-muted-foreground">{b.reason}</p> : null}
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => clearBacklogMut.mutate(b.backlogId)}
                      disabled={clearBacklogMut.isPending}
                    >
                      Clear
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </PlanningPanel>
        </TabsContent>
      </Tabs>

      <div className="mt-6 flex items-center gap-2 text-xs text-muted-foreground">
        <Settings2 className="h-3.5 w-3.5" />
        Shifts are read from ERP capacity master (A/B/C when present). Material readiness assumed ready in v1.
      </div>
    </PlanningPageShell>
  );
}
