import { useEffect, useMemo, useState, type ComponentType, type ReactNode } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Boxes, Cable, ChevronLeft, Cylinder, Factory, Layers, Loader2, ScrollText, Spline } from "lucide-react";
import { toast } from "sonner";
import { LaminationPanel, LoomPanel, NeedlePanel } from "@/components/plant-entry/production-panels";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth-context";
import { cn } from "@/lib/utils";
import {
  deletePlantConsumption,
  getPlantCompanies,
  getPlantLookups,
  getPlantMaterials,
  getPlantRecent,
  getPlantRolls,
  getPlantSetup,
  loadPlantEntry,
  savePlantConsumption,
  type PlantLine,
  type PlantMaterial,
} from "@/lib/plant-consumption-api";

export const Route = createFileRoute("/_app/plant-consumption")({
  head: () => ({ meta: [{ title: "Plant entry — PO Portal" }] }),
  component: PlantConsumptionPage,
});

type EntryFamily = "consumption" | "loom" | "needle" | "lam-rolls";
type EntryBranch = "production" | "consumption";

const SAMPLE_PLANTS = ["Tape Plant", "Filler", "Yarn", "Liner Roll Plant", "Lamination"];
const SAMPLE_COMPANIES = ["K.P. WOVEN PRIVATE LIMITED", "Plastene India Limited"];
const NO_NAMES: string[] = [];
const EMPTY_MATERIALS: PlantMaterial[] = [];
const SAMPLE_MATERIALS: PlantMaterial[] = [
  { quality: "PP", grade: "PP Raffia Natural", itemCode: "RM-PP-01", stock: 2450 },
  { quality: "PP", grade: "PP Raffia White", itemCode: "RM-PP-02", stock: 860 },
  { quality: "Filler", grade: "CaCO3 Filler", itemCode: "RM-FL-01", stock: 1200 },
  { quality: "Yarn", grade: "PP Yarn 1100", itemCode: "RM-YN-01", stock: 640 },
];

const PRODUCTION_TILES: { id: Exclude<EntryFamily, "consumption">; label: string; hint: string; icon: ComponentType<{ className?: string }> }[] = [
  { id: "loom", label: "Loom", hint: "Rolls and bobbins", icon: Factory },
  { id: "needle", label: "Needle loom", hint: "Webbing rolls", icon: Cable },
  { id: "lam-rolls", label: "Lamination rolls", hint: "Unlaminated to laminated", icon: Layers },
];

const PP = new Set(["PP", "PP Granuals", "HD", "HDPE Granuals"]);
const CONSUMPTION_RECENT = "minmax(7rem,1fr) 5.25rem minmax(8rem,1.4fr) 7.25rem 7.25rem minmax(8rem,1.2fr) 6.5rem";

function todayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function num(value: string) {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function money(n: number) {
  return n.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function blankLine(): PlantLine {
  return { key: crypto.randomUUID(), quality: "", grade: "", itemCode: "", stock: 0, qty: "", percent: "", ppBags: "" };
}

function applyTape(rows: PlantLine[], tape: boolean) {
  if (!tape) return rows;
  const next = rows.map((row) => ({ ...row }));
  let totalBags = 0;
  for (const row of next) {
    if (num(row.ppBags) > 0 && PP.has(row.quality.trim())) {
      totalBags = num(row.ppBags) * 25;
      row.qty = String(totalBags);
    }
  }
  if (totalBags > 0) {
    for (const row of next) {
      if (num(row.ppBags) === 0 && !PP.has(row.quality.trim()))
        row.qty = String(Math.round((totalBags * num(row.percent)) / 100));
    }
  }
  return next;
}

function PlantConsumptionPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [company, setCompany] = useState("");
  const [entryDate, setEntryDate] = useState(todayIso);
  const [searchDate, setSearchDate] = useState(todayIso);
  const [shift, setShift] = useState("A");
  const [productionType, setProductionType] = useState("Sell");
  const [timeIn, setTimeIn] = useState("08:00:00");
  const [timeOut, setTimeOut] = useState("20:00:00");
  const [plant, setPlant] = useState("");
  const [plantSub, setPlantSub] = useState("");
  const [product, setProduct] = useState("");
  const [sector, setSector] = useState("");
  const [fromWh, setFromWh] = useState("");
  const [toWh, setToWh] = useState("");
  const [buyer, setBuyer] = useState("");
  const [orderNo, setOrderNo] = useState("");
  const [orderDate, setOrderDate] = useState("");
  const [invoice, setInvoice] = useState("");
  const [wastage, setWastage] = useState("0");
  const [trim, setTrim] = useState("0");
  const [sweeping, setSweeping] = useState("0");
  const [fabricTrim, setFabricTrim] = useState("0");
  const [fabricWaste, setFabricWaste] = useState("0");
  const [lumps, setLumps] = useState("0");
  const [lines, setLines] = useState<PlantLine[]>([blankLine()]);
  const [groupSrNo, setGroupSrNo] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const [branch, setBranch] = useState<EntryBranch | null>(null);
  const [family, setFamily] = useState<EntryFamily | null>(null);

  const companies = useQuery({ queryKey: ["plant-companies"], queryFn: getPlantCompanies, staleTime: Infinity });
  const lookups = useQuery({
    queryKey: ["plant-lookups", company],
    queryFn: () => getPlantLookups(company),
    enabled: company.length > 0,
  });
  const setup = useQuery({
    queryKey: ["plant-setup", company, plant, entryDate],
    queryFn: () => getPlantSetup(company, plant, entryDate),
    enabled: company.length > 0 && plant.length > 0,
  });
  const materials = useQuery({
    queryKey: ["plant-materials", company, fromWh, entryDate],
    queryFn: () => getPlantMaterials(company, fromWh, entryDate),
    enabled: company.length > 0 && fromWh.length > 0,
  });
  const rollsQuery = useQuery({
    queryKey: ["plant-rolls", company, plant, entryDate, shift],
    queryFn: () => getPlantRolls(company, plant, entryDate, shift),
    enabled: company.length > 0 && (plant.toLowerCase() === "lamination" || plant.toLowerCase() === "liner roll plant"),
  });
  const recent = useQuery({
    queryKey: ["plant-recent", company, plant, searchDate],
    queryFn: () => getPlantRecent(company, plant, searchDate),
    enabled: company.length > 0 && plant.length > 0,
  });

  const livePlants = lookups.data?.plants ?? NO_NAMES;
  const plants = lookups.isLoading ? NO_NAMES : livePlants.length > 0 ? livePlants : SAMPLE_PLANTS;
  const companyNames = companies.data?.map((item) => item.name) ?? NO_NAMES;
  const shownCompanies = companies.isLoading ? NO_NAMES : companyNames.length > 0 ? companyNames : SAMPLE_COMPANIES;
  const connected = livePlants.includes(plant);

  useEffect(() => {
    if (!company && shownCompanies[0]) setCompany(shownCompanies[0]);
  }, [company, shownCompanies]);

  useEffect(() => {
    if (family !== "consumption" || lookups.isLoading || plants.length === 0) return;
    if (!plants.includes(plant)) setPlant(plants.find((name) => name.toLowerCase() === "tape plant") ?? plants[0]);
  }, [family, lookups.isLoading, plant, plants]);

  useEffect(() => {
    if (!setup.data) return;
    if (!setup.data.subs.includes(plantSub)) setPlantSub(setup.data.subs[0] ?? "");
    if (!setup.data.fromWarehouses.includes(fromWh)) setFromWh(setup.data.fromWarehouses[0] ?? "");
    if (!setup.data.toWarehouses.includes(toWh)) setToWh(setup.data.toWarehouses[0] ?? "");
    if (!setup.data.products.includes(product)) setProduct(setup.data.products[0] ?? "");
  }, [setup.data, plantSub, fromWh, toWh, product]);

  const materialRows = materials.data && materials.data.length > 0 ? materials.data : connected ? EMPTY_MATERIALS : SAMPLE_MATERIALS;
  const qualities = useMemo(
    () => [...new Set(materialRows.map((m) => m.quality))].sort(),
    [materialRows],
  );
  const buyerRow = lookups.data?.buyers.find((b) => b.name === buyer);
  const orders = buyerRow?.orders ?? [];
  const invoices = orders.find((o) => o.orderNo === orderNo)?.invoices ?? [];
  const isTape = plant.toLowerCase() === "tape plant";
  const isLam = plant.toLowerCase() === "lamination";
  const isLiner = plant.toLowerCase() === "liner roll plant";
  const showExtra = isTape || isLam;
  const bagWeight = isTape
    ? lines.reduce((last, line) => (num(line.ppBags) > 0 && PP.has(line.quality.trim()) ? num(line.ppBags) * 25 : last), 0)
    : 0;

  const consumption = lines.reduce((sum, line) => sum + num(line.qty), 0);
  const wasteTotal = num(wastage) + (showExtra ? num(trim) + num(sweeping) : 0) + (isLam ? num(fabricTrim) + num(fabricWaste) + num(lumps) : 0);
  const net = consumption - wasteTotal;
  const rolls = isLam || isLiner ? (rollsQuery.data?.rolls ?? 0) : 0;

  function patchLine(key: string, patch: Partial<PlantLine>) {
    setLines((rows) => rows.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }

  function onQuality(key: string, quality: string) {
    patchLine(key, { quality, grade: "", itemCode: "", stock: 0, percent: "", ppBags: "" });
  }

  function onGrade(key: string, quality: string, grade: string) {
    const match = materialRows.find((m) => m.quality === quality && m.grade === grade);
    patchLine(key, { grade, itemCode: match?.itemCode ?? "", stock: match?.stock ?? 0 });
  }

  function clearForm() {
    setGroupSrNo(null);
    setWastage("0");
    setTrim("0");
    setSweeping("0");
    setFabricTrim("0");
    setFabricWaste("0");
    setLumps("0");
    setLines([blankLine()]);
    setBuyer("");
    setOrderNo("");
    setInvoice("");
    setOrderDate("");
  }

  async function openRecent(id: number) {
    try {
      const entry = await loadPlantEntry(company, id);
      const h = entry.header;
      setGroupSrNo(id);
      setEntryDate(h.entryDate);
      setShift(h.shift || "A");
      setProductionType(h.productionType || "Sell");
      setTimeIn(h.timeIn || "00:00:00");
      setTimeOut(h.timeOut || "00:00:00");
      setPlant(h.plant);
      setPlantSub(h.plantSub);
      setProduct(h.product);
      setSector(h.sector);
      setBuyer(h.buyer);
      setInvoice(h.marketingInvoice);
      setOrderDate(h.buyerOrderDate);
      setWastage(String(h.wastage));
      setTrim(String(h.trimWastage));
      setSweeping(String(h.sweepingWastage));
      setFabricTrim(String(h.fabricTrim));
      setFabricWaste(String(h.fabricWaste));
      setLumps(String(h.lumpsWastage));
      setLines(
        entry.lines.map((line) => ({
          key: crypto.randomUUID(),
          quality: line.quality,
          grade: line.grade,
          itemCode: line.itemCode,
          stock: 0,
          qty: String(line.qty),
          percent: "",
          ppBags: "",
        })),
      );
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not open the entry");
    }
  }

  function chooseBranch(next: EntryBranch) {
    if (branch === next && family == null) return;
    setBranch(next);
    setFamily(null);
    setPlant("");
    clearForm();
  }

  function backToMaterials() {
    setFamily(null);
    setPlant("");
  }

  function chooseConsumption(name: string) {
    if (family === "consumption" && plant === name) return;
    setBranch("consumption");
    setFamily("consumption");
    setPlant(name);
    clearForm();
  }

  function chooseProduction(next: Exclude<EntryFamily, "consumption">) {
    if (family === next) return;
    setBranch("production");
    setFamily(next);
  }

  function openLaminationConsumption() {
    const match = plants.find((name) => name.toLowerCase() === "lamination");
    if (!match) return;
    setBranch("consumption");
    setFamily("consumption");
    setPlant(match);
    clearForm();
  }

  const step: 1 | 2 | 3 = branch == null ? 1 : family == null ? 2 : 3;
  const productionLabel = PRODUCTION_TILES.find((tile) => tile.id === family)?.label;

  async function onSave() {
    if (!connected) {
      toast.success("Kept on this screen. Saving starts once this plant is loaded from the server.");
      return;
    }
    setBusy(true);
    try {
      const saved = await savePlantConsumption({
        company,
        userName: user?.username ?? "",
        entryDate,
        shift,
        productionType,
        timeIn,
        timeOut,
        plant,
        plantSub,
        product,
        sector,
        fromWarehouse: fromWh,
        toWarehouse: toWh,
        buyer,
        buyerOrder: orderNo,
        buyerOrderDate: orderDate || null,
        marketingInvoice: invoice,
        wastage: num(wastage),
        trimWastage: showExtra ? num(trim) : 0,
        sweepingWastage: showExtra ? num(sweeping) : 0,
        fabricTrim: isLam ? num(fabricTrim) : 0,
        fabricWaste: isLam ? num(fabricWaste) : 0,
        lumpsWastage: isLam ? num(lumps) : 0,
        rolls,
        groupSrNo,
        lines: lines.map((line) => ({
          quality: line.quality,
          grade: line.grade,
          itemCode: line.itemCode,
          qty: num(line.qty),
          percent: num(line.percent),
          ppBags: PP.has(line.quality.trim()) ? num(line.ppBags) : 0,
        })),
      });
      setGroupSrNo(saved.groupSrNo);
      toast.success(`Saved. Group ${saved.groupSrNo}`);
      await queryClient.invalidateQueries({ queryKey: ["plant-recent"] });
      await queryClient.invalidateQueries({ queryKey: ["plant-materials"] });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Save failed");
    } finally {
      setBusy(false);
    }
  }

  async function onDelete() {
    if (!groupSrNo) return;
    if (!window.confirm(`Delete group ${groupSrNo}? This puts the raw material back and removes the finished quantity.`)) return;
    setBusy(true);
    try {
      await deletePlantConsumption({ company, plant, fromWarehouse: fromWh, toWarehouse: toWh, groupSrNo });
      toast.success("Deleted");
      clearForm();
      await queryClient.invalidateQueries({ queryKey: ["plant-recent"] });
      await queryClient.invalidateQueries({ queryKey: ["plant-materials"] });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Delete failed");
    } finally {
      setBusy(false);
    }
  }

  const head = "sticky top-0 z-10 border-b border-border/70 bg-card px-3 py-2 text-left text-[11px] font-medium uppercase tracking-[0.12em] text-muted-foreground";
  const headRight = `${head} text-right`;

  return (
    <div className="space-y-5">
      <header className="app-glass flex flex-col gap-4 rounded-2xl px-5 py-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary">Plant entry</p>
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
            {step === 1 ? "Plant entry" : step === 2 ? (branch === "production" ? "Production" : "Consumption") : productionLabel || plant || "Entry"}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {company ? `${company} · Shift ${shift}` : "Choose a company and a shift"}
          </p>
        </div>
        <div className="grid w-full gap-3 sm:grid-cols-3 lg:w-[34rem]">
          <Field label="Company">
            <Pick value={company} onChange={setCompany} options={shownCompanies} placeholder="Company" />
          </Field>
          <Field label="Date">
            <Input type="date" max={todayIso()} value={entryDate} onChange={(e) => setEntryDate(e.target.value)} />
          </Field>
          <Field label="Shift">
            <Pick value={shift} onChange={setShift} options={["A", "B"]} placeholder="Shift" />
          </Field>
        </div>
      </header>

      <StepRail
        step={step}
        branch={branch}
        material={step === 3 ? (family === "consumption" ? plant : productionLabel ?? "") : ""}
        onType={() => {
          setBranch(null);
          setFamily(null);
          setPlant("");
        }}
        onMaterial={backToMaterials}
      />

      {step === 1 && (
        <section className="grid gap-4 md:grid-cols-2">
          <button type="button" onClick={() => chooseBranch("production")} className="flex min-h-[16rem] items-center rounded-3xl p-8 text-left card-3d">
            <TileBody large icon={Factory} label="Production" hint="Loom, needle loom, and lamination rolls" active={false} />
          </button>
          <button type="button" onClick={() => chooseBranch("consumption")} className="flex min-h-[16rem] items-center rounded-3xl p-8 text-left card-3d">
            <TileBody large icon={Boxes} label="Consumption" hint="Tape plant, filler, yarn, liner, and lamination" active={false} />
          </button>
        </section>
      )}

      {step === 2 && branch === "consumption" && (
        <section className="space-y-3">
          <BackLink label="Production or consumption" onClick={() => { setBranch(null); setFamily(null); setPlant(""); }} />
          {plants.length === 0 ? (
            <div className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-5">
              {Array.from({ length: 5 }, (_, index) => (
                <div key={index} className="min-h-[11rem] animate-pulse rounded-3xl bg-card" />
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-5">
              {plants.map((name) => {
                const meta = plantMeta(name);
                return (
                  <button key={name} type="button" onClick={() => chooseConsumption(name)} className="flex min-h-[11rem] items-center rounded-3xl p-6 text-left card-3d">
                    <TileBody large icon={meta.icon} label={name} hint={meta.hint} active={false} />
                  </button>
                );
              })}
            </div>
          )}
        </section>
      )}

      {step === 2 && branch === "production" && (
        <section className="space-y-3">
          <BackLink label="Production or consumption" onClick={() => { setBranch(null); setFamily(null); setPlant(""); }} />
          <div className="grid gap-4 md:grid-cols-3">
            {PRODUCTION_TILES.map((tile) => (
              <button key={tile.id} type="button" onClick={() => chooseProduction(tile.id)} className="flex min-h-[14rem] items-center rounded-3xl p-8 text-left card-3d">
                <TileBody large icon={tile.icon} label={tile.label} hint={tile.hint} active={false} />
              </button>
            ))}
          </div>
        </section>
      )}

      {step === 3 && (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm text-muted-foreground">
            {branch === "production" ? "Production" : "Consumption"}
            <span className="px-1.5 text-border">/</span>
            <span className="font-medium text-foreground">{family === "consumption" ? plant : productionLabel}</span>
          </p>
          <button type="button" onClick={backToMaterials} className="inline-flex items-center gap-1 text-sm text-primary hover:underline">
            <ChevronLeft className="h-4 w-4" />
            Change material
          </button>
        </div>
      )}

      {family === "loom" && <LoomPanel date={entryDate} shift={shift} onSaved={() => undefined} />}
      {family === "needle" && <NeedlePanel date={entryDate} shift={shift} />}
      {family === "lam-rolls" && <LaminationPanel date={entryDate} shift={shift} onContinue={openLaminationConsumption} />}

      {family === "consumption" && plant && (
      <>
      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="border-b border-border/70 px-4 py-4 md:px-5">
          <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-primary">{plant || "Consumption"}</p>
          <h2 className="text-lg font-semibold tracking-tight">{plantHint(plant)}</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-3 xl:grid-cols-4">
            <Field label="Plant sub name">
              <Pick value={plantSub} onChange={setPlantSub} options={setup.data?.subs ?? []} placeholder="Sub name" />
            </Field>
            <Field label="Product name">
              <Pick value={product} onChange={setProduct} options={setup.data?.products ?? []} placeholder="Product" />
            </Field>
            <Field label="Sector">
              <Pick value={sector} onChange={setSector} options={lookups.data?.sectors ?? []} placeholder="Sector" />
            </Field>
            <Field label="From warehouse">
              <Pick value={fromWh} onChange={setFromWh} options={setup.data?.fromWarehouses ?? []} placeholder="From warehouse" />
            </Field>
            <Field label="To warehouse">
              <Pick value={toWh} onChange={setToWh} options={setup.data?.toWarehouses ?? []} placeholder="To warehouse" />
            </Field>
            <Field label="Production type">
              <Pick value={productionType} onChange={setProductionType} options={["Sell", "Job", "Outside Job"]} placeholder="Type" />
            </Field>
            <Field label="Time in">
              <Input value={timeIn} onChange={(e) => setTimeIn(e.target.value)} />
            </Field>
            <Field label="Time out">
              <Input value={timeOut} onChange={(e) => setTimeOut(e.target.value)} />
            </Field>
          </div>
        </div>
        <div className="border-b border-border/70 px-4 py-4 md:px-5">
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <Field label="Buyer">
              <Pick
                value={buyer}
                placeholder="Buyer"
                options={(lookups.data?.buyers ?? []).map((b) => b.name)}
                onChange={(value) => {
                  setBuyer(value);
                  setOrderNo("");
                  setInvoice("");
                }}
              />
            </Field>
            <Field label="Buyer order no">
              <Pick
                value={orderNo}
                placeholder="Order no"
                options={orders.map((o) => o.orderNo)}
                onChange={(value) => {
                  setOrderNo(value);
                  setInvoice("");
                }}
              />
            </Field>
            <Field label="Buyer order date">
              <Input type="date" value={orderDate} onChange={(e) => setOrderDate(e.target.value)} />
            </Field>
            <Field label="Marketing invoice">
              <Pick value={invoice} onChange={setInvoice} options={invoices} placeholder="Invoice" />
            </Field>
          </div>
        </div>
        <div className="flex items-center justify-between px-4 py-3 md:px-5">
          <h2 className="text-sm font-semibold">Raw material</h2>
          <Button type="button" variant="outline" size="sm" onClick={() => setLines((rows) => [...rows, blankLine()])} disabled={!plant}>
            Add row
          </Button>
        </div>
        <div className="max-h-[32rem] overflow-auto border-t border-border/70">
          <table className="w-full min-w-[880px] border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                <th className={`${head} min-w-[11rem]`}>Quality</th>
                <th className={`${head} min-w-[14rem]`}>Grade</th>
                <th className={`${head} min-w-[8rem]`}>Item code</th>
                <th className={`${headRight} w-28`}>Qty</th>
                <th className={`${headRight} w-28`}>Stock</th>
                <th className={`${headRight} w-20`}>Rate</th>
                <th className={`${headRight} w-24`}>Amount</th>
                {isTape && <th className={`${headRight} w-20`}>%</th>}
                {isTape && <th className={`${headRight} w-24`}>PP bag</th>}
                <th className={`${head} w-16`} />
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => {
                const grades = materialRows.filter(
                  (m) => m.quality === line.quality && (m.stock > 0 || m.grade === line.grade),
                );
                const pp = PP.has(line.quality.trim());
                const qtyLocked = isTape && ((pp && num(line.ppBags) > 0) || (bagWeight > 0 && !pp));
                return (
                  <tr key={line.key} className="border-b last:border-b-0">
                    <td className="px-3 py-2 align-middle">
                      <Pick value={line.quality} onChange={(value) => onQuality(line.key, value)} options={qualities} placeholder="Quality" />
                    </td>
                    <td className="px-3 py-2 align-middle">
                      <Pick value={line.grade} onChange={(value) => onGrade(line.key, line.quality, value)} options={grades.map((g) => g.grade)} placeholder="Grade" />
                    </td>
                    <td className="px-3 py-2 align-middle font-mono text-xs text-muted-foreground">{line.itemCode || "—"}</td>
                    <td className="px-3 py-2 align-middle">
                      <Input className="text-right" value={line.qty} disabled={qtyLocked} onChange={(e) => patchLine(line.key, { qty: e.target.value })} />
                    </td>
                    <td className="px-3 py-2 text-right align-middle"><span className="inline-flex h-8 w-full items-center justify-end tabular-nums text-muted-foreground">{money(line.stock)}</span></td>
                    <td className="px-3 py-2 text-right align-middle"><span className="inline-flex h-8 w-full items-center justify-end tabular-nums text-muted-foreground">0.00</span></td>
                    <td className="px-3 py-2 text-right align-middle"><span className="inline-flex h-8 w-full items-center justify-end tabular-nums text-muted-foreground">0.00</span></td>
                    {isTape && (
                      <td className="px-3 py-2 align-middle">
                        <Input
                          className="text-right"
                          value={line.percent}
                          disabled={pp}
                          onChange={(e) => setLines((rows) => applyTape(rows.map((row) => (row.key === line.key ? { ...row, percent: pp ? "" : e.target.value } : row)), true))}
                        />
                      </td>
                    )}
                    {isTape && (
                      <td className="px-3 py-2 align-middle">
                        <Input
                          className="text-right"
                          value={line.ppBags}
                          disabled={!pp}
                          onChange={(e) => setLines((rows) => applyTape(rows.map((row) => (row.key === line.key ? { ...row, ppBags: pp ? e.target.value : "" } : row)), true))}
                        />
                      </td>
                    )}
                    <td className="px-2 py-2 text-right align-middle">
                      <Button type="button" variant="ghost" size="sm" className="h-8 px-2 text-muted-foreground" onClick={() => setLines((rows) => rows.filter((row) => row.key !== line.key))}>
                        Remove
                      </Button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="grid gap-3 border-t border-border/70 px-4 py-4 sm:grid-cols-2 xl:grid-cols-3 md:px-5">
            <Field label="Wastage"><Input value={wastage} onChange={(e) => setWastage(e.target.value)} /></Field>
            {showExtra && (
              <Field label={isTape ? "Lumps Wastage" : "Triming Wastage"}>
                <Input value={trim} onChange={(e) => setTrim(e.target.value)} />
              </Field>
            )}
            {showExtra && (
              <Field label={isTape ? "Others Wastage" : "Sweeping Wastage"}>
                <Input value={sweeping} onChange={(e) => setSweeping(e.target.value)} />
              </Field>
            )}
            {isLam && <Field label="Film Triming"><Input value={fabricTrim} onChange={(e) => setFabricTrim(e.target.value)} /></Field>}
            {isLam && <Field label="Fabric Waste"><Input value={fabricWaste} onChange={(e) => setFabricWaste(e.target.value)} /></Field>}
            {isLam && <Field label="Lumps Wastage"><Input value={lumps} onChange={(e) => setLumps(e.target.value)} /></Field>}
            {(isLam || isLiner) && (
              <Field label="Rolls">
                <Input value={rollsQuery.isError ? "" : money(rolls)} readOnly />
              </Field>
            )}
        </div>
        <div className="flex flex-col gap-4 border-t border-border/70 bg-muted/20 px-4 py-4 md:flex-row md:items-center md:justify-between md:px-5">
          <div className="grid flex-1 grid-cols-2 gap-3 sm:grid-cols-3">
            <Stat label="Total consumption" value={money(consumption)} />
            <Stat label="Wastage" value={money(wasteTotal)} />
            <Stat label="Net production" value={money(net)} />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {groupSrNo && <span className="text-xs text-muted-foreground">Group {groupSrNo}</span>}
            <button type="button" className="px-2 text-sm text-destructive hover:underline disabled:opacity-40" onClick={onDelete} disabled={busy || !groupSrNo}>Delete</button>
            <Button type="button" variant="outline" onClick={clearForm} disabled={busy}>Clear</Button>
            <Button type="button" onClick={onSave} disabled={busy || !plant}>
              {busy && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {groupSrNo ? "Update" : "Save"}
            </Button>
          </div>
        </div>
        {rollsQuery.isError && <p className="px-5 pb-3 text-xs text-destructive">{(rollsQuery.error as Error).message}</p>}
      </section>

      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="flex flex-wrap items-end justify-between gap-3 px-4 py-3">
          <div>
            <h2 className="text-sm font-semibold tracking-tight">Recent entries</h2>
            <p className="text-xs text-muted-foreground">Last 30 days up to the search date.</p>
          </div>
          <div className="w-44">
            <Field label="Search date">
              <Input type="date" value={searchDate} onChange={(e) => setSearchDate(e.target.value)} />
            </Field>
          </div>
        </div>
        {!plant ? (
          <p className="border-t border-border/70 px-5 py-10 text-center text-sm text-muted-foreground">Choose a plant to see saved entries.</p>
        ) : !connected ? (
          <p className="border-t border-border/70 px-5 py-10 text-center text-sm text-muted-foreground">No entries for this day. Saved history appears when the server is connected.</p>
        ) : recent.data?.length === 0 ? (
          <p className="border-t border-border/70 px-5 py-10 text-center text-sm text-muted-foreground">No entries for this day.</p>
        ) : (
          <div className="max-h-80 overflow-auto border-t border-border/70">
            <div className="min-w-[48rem]">
              <div className="sticky top-0 z-10 grid border-b border-border/70 bg-muted/95 px-2 backdrop-blur-sm" style={{ gridTemplateColumns: CONSUMPTION_RECENT }}>
                {["Date", "Shift", "Product", "Consumption", "Net", "Buyer", "HOD"].map((column) => (
                  <div key={column} className={cn("px-3 py-2.5 text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground", column === "Consumption" || column === "Net" ? "text-right" : "text-left")}>
                    {column}
                  </div>
                ))}
              </div>
              {(recent.data ?? []).map((row) => (
                <button
                  key={row.groupSrNo}
                  type="button"
                  onClick={() => openRecent(row.groupSrNo)}
                  className={cn(
                    "grid w-full border-b border-border/40 px-2 text-left text-sm transition last:border-b-0 hover:bg-muted/40",
                    groupSrNo === row.groupSrNo && "bg-primary/10",
                  )}
                  style={{ gridTemplateColumns: CONSUMPTION_RECENT }}
                >
                  <span className="flex items-center px-3 py-3 font-medium">{row.entryDate}</span>
                  <span className="flex items-center px-3 py-3">
                    <span className="rounded-md bg-primary/10 px-2 py-0.5 text-xs font-semibold text-primary">Shift {row.shift}</span>
                  </span>
                  <span className="flex min-w-0 items-center px-3 py-3"><span className="truncate">{row.product}</span></span>
                  <span className="flex items-center justify-end px-3 py-3 font-medium tabular-nums">{money(row.consumption)}</span>
                  <span className="flex items-center justify-end px-3 py-3 font-medium tabular-nums">{money(row.netProduction)}</span>
                  <span className="flex min-w-0 items-center px-3 py-3"><span className="truncate">{row.buyer}</span></span>
                  <span className="flex items-center px-3 py-3">
                    <span className={cn("rounded-full px-2 py-0.5 text-[11px] font-medium", row.hod === "Approved" ? "bg-success/15 text-success" : "bg-warning/15 text-warning-foreground")}>{row.hod || "Pending"}</span>
                  </span>
                </button>
              ))}
            </div>
          </div>
        )}
      </section>
      </>
      )}
    </div>
  );
}

function StepRail({
  step,
  branch,
  material,
  onType,
  onMaterial,
}: {
  step: 1 | 2 | 3;
  branch: EntryBranch | null;
  material: string;
  onType: () => void;
  onMaterial: () => void;
}) {
  const items = [
    { n: 1 as const, label: "Type", detail: branch === "production" ? "Production" : branch === "consumption" ? "Consumption" : "Choose", onClick: step > 1 ? onType : undefined },
    { n: 2 as const, label: "Material", detail: material || (step > 1 ? "Choose" : ""), onClick: step > 2 ? onMaterial : undefined },
    { n: 3 as const, label: "Entry", detail: step === 3 ? "Form" : "", onClick: undefined },
  ];
  return (
    <ol className="grid gap-2 sm:grid-cols-3">
      {items.map((item) => {
        const current = item.n === step;
        const done = item.n < step;
        const className = cn(
          "flex w-full items-center gap-3 rounded-2xl px-3 py-2.5 text-left",
          current ? "nav-3d-active" : done ? "card-3d" : "border border-dashed border-border/80 bg-card/40",
        );
        const body = (
          <>
            <span className={cn("flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold", current ? "bg-white/20 text-primary-foreground" : done ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground")}>
              {item.n}
            </span>
            <span className="min-w-0">
              <span className={cn("block text-[11px] font-semibold uppercase tracking-[0.14em]", current ? "text-primary-foreground/80" : "text-muted-foreground")}>{item.label}</span>
              {item.detail ? <span className="block truncate text-sm font-medium">{item.detail}</span> : null}
            </span>
          </>
        );
        return (
          <li key={item.n}>
            {item.onClick ? (
              <button type="button" onClick={item.onClick} className={className}>{body}</button>
            ) : (
              <div className={className}>{body}</div>
            )}
          </li>
        );
      })}
    </ol>
  );
}

function BackLink({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
      <ChevronLeft className="h-4 w-4" />
      {label}
    </button>
  );
}

function plantMeta(name: string): { hint: string; icon: ComponentType<{ className?: string }> } {
  const n = name.toLowerCase();
  if (n === "tape plant") return { hint: "Bags, percent, and wastage", icon: Cylinder };
  if (n.includes("filler")) return { hint: "Filler consumption", icon: Boxes };
  if (n.includes("yarn")) return { hint: "Yarn consumption", icon: Spline };
  if (n.includes("liner")) return { hint: "Rolls against consumption", icon: ScrollText };
  if (n === "lamination") return { hint: "Film, fabric, and lumps", icon: Layers };
  return { hint: "Quality, grade, and quantity", icon: Factory };
}

function plantHint(name: string) {
  return plantMeta(name).hint;
}

function TileBody({
  icon: Icon,
  label,
  hint,
  active,
  large,
}: {
  icon: ComponentType<{ className?: string }>;
  label: string;
  hint: string;
  active: boolean;
  large?: boolean;
}) {
  return (
    <span className={cn("flex items-start", large ? "gap-4" : "gap-3")}>
      <span className={cn("icon-3d flex shrink-0 items-center justify-center", large ? "h-14 w-14 rounded-2xl" : "h-8 w-8 rounded-xl", active ? "bg-white/15 text-primary-foreground" : "bg-primary/10 text-primary")}>
        <Icon className={large ? "h-7 w-7" : "h-4 w-4"} />
      </span>
      <span className="min-w-0">
        <span className={cn("block font-semibold", large ? "text-xl" : "truncate text-sm")}>{label}</span>
        <span className={cn("block", large ? "mt-1 text-sm" : "mt-0.5 truncate text-xs", active ? "text-primary-foreground/80" : "text-muted-foreground")}>{hint}</span>
      </span>
    </span>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] uppercase tracking-[0.14em] text-muted-foreground">{label}</p>
      <p className="text-xl font-semibold tracking-tight tabular-nums">{value}</p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block space-y-1">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      {children}
    </label>
  );
}

function Pick({
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
  return (
    <SearchableSelect
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      options={options.filter(Boolean).map((option) => ({ value: option, label: option }))}
    />
  );
}

