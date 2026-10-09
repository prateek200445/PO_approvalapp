import { useEffect, useMemo, useState, type ReactNode } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth-context";
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
} from "@/lib/plant-consumption-api";

export const Route = createFileRoute("/_app/plant-consumption")({
  head: () => ({ meta: [{ title: "Plant Consumption — PO Portal" }] }),
  component: PlantConsumptionPage,
});

const PP = new Set(["PP", "PP Granuals", "HD", "HDPE Granuals"]);

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

  useEffect(() => {
    if (!company && companies.data?.[0]) setCompany(companies.data[0].name);
  }, [companies.data, company]);

  useEffect(() => {
    const next = lookups.data?.plants[0];
    if (next && !lookups.data.plants.includes(plant)) setPlant(next);
  }, [lookups.data, plant]);

  useEffect(() => {
    if (!setup.data) return;
    if (!setup.data.subs.includes(plantSub)) setPlantSub(setup.data.subs[0] ?? "");
    if (!setup.data.fromWarehouses.includes(fromWh)) setFromWh(setup.data.fromWarehouses[0] ?? "");
    if (!setup.data.toWarehouses.includes(toWh)) setToWh(setup.data.toWarehouses[0] ?? "");
    if (!setup.data.products.includes(product)) setProduct(setup.data.products[0] ?? "");
  }, [setup.data, plantSub, fromWh, toWh, product]);

  const qualities = useMemo(
    () => [...new Set((materials.data ?? []).map((m) => m.quality))].sort(),
    [materials.data],
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
    const match = (materials.data ?? []).find((m) => m.quality === quality && m.grade === grade);
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

  async function onSave() {
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

  const noPlants = lookups.isSuccess && (lookups.data?.plants.length ?? 0) === 0;
  const head = "border-b border-border bg-card px-3 py-2 text-left text-[11px] font-medium text-muted-foreground";
  const headRight = `${head} text-right`;

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight text-foreground md:text-3xl">Plant Consumption</h1>

      <section className="rounded-xl border bg-card p-4">
        <h2 className="mb-3 text-sm font-semibold">Entry</h2>
        <div className="grid gap-3 md:grid-cols-3 xl:grid-cols-6">
          <Field label="Company">
            <Pick value={company} onChange={setCompany} options={(companies.data ?? []).map((c) => c.name)} placeholder="Company" />
          </Field>
          <Field label="Date">
            <Input type="date" max={todayIso()} value={entryDate} onChange={(e) => setEntryDate(e.target.value)} />
          </Field>
          <Field label="Shift">
            <Pick value={shift} onChange={setShift} options={["A", "B"]} placeholder="Shift" />
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
      </section>

      <section className="rounded-xl border bg-card p-4">
        <h2 className="mb-3 text-sm font-semibold">Plant</h2>
        {noPlants ? (
          <p className="text-sm text-muted-foreground">This company has no plant.</p>
        ) : (
          <div className="grid gap-3 md:grid-cols-3">
            <Field label="Plant">
              <Pick value={plant} onChange={setPlant} options={lookups.data?.plants ?? []} placeholder="Plant" />
            </Field>
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
          </div>
        )}
        {lookups.isError && <p className="mt-3 text-sm text-destructive">{(lookups.error as Error).message}</p>}
      </section>

      {!noPlants && (
        <section className="rounded-xl border bg-card p-4">
          <h2 className="mb-3 text-sm font-semibold">Buyer</h2>
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
        </section>
      )}

      <section className="overflow-hidden rounded-xl border bg-card">
        <div className="flex items-center justify-between px-4 py-3">
          <h2 className="text-sm font-semibold">Raw material</h2>
          <Button type="button" variant="outline" size="sm" onClick={() => setLines((rows) => [...rows, blankLine()])} disabled={noPlants}>
            Add row
          </Button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[880px] border-t text-sm">
            <thead>
              <tr>
                <th className={`${head} min-w-[11rem]`}>Quality</th>
                <th className={`${head} min-w-[14rem]`}>Grade</th>
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
                const grades = (materials.data ?? []).filter(
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
                    <td className="px-3 py-2 align-middle">
                      <Input className="text-right" value={line.qty} disabled={qtyLocked} onChange={(e) => patchLine(line.key, { qty: e.target.value })} />
                    </td>
                    <td className="px-3 py-2 text-right align-middle tabular-nums text-muted-foreground">{money(line.stock)}</td>
                    <td className="px-3 py-2 text-right align-middle tabular-nums text-muted-foreground">0.00</td>
                    <td className="px-3 py-2 text-right align-middle tabular-nums text-muted-foreground">0.00</td>
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
      </section>

      <section className="rounded-xl border bg-card p-4">
        <div className="grid gap-4 lg:grid-cols-[1fr_240px] lg:items-end">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
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
          <div className="space-y-2 border-t pt-3 text-sm lg:border-l lg:border-t-0 lg:pl-4 lg:pt-0">
            <Row label="Total Consumption" value={money(consumption)} />
            <Row label="Net Production" value={money(net)} />
            {rollsQuery.isError && <p className="text-xs text-destructive">{(rollsQuery.error as Error).message}</p>}
          </div>
        </div>
        <div className="mt-4 flex flex-wrap items-center gap-2">
          <Button type="button" onClick={onSave} disabled={busy || noPlants}>
            {busy && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {groupSrNo ? "Update" : "Save"}
          </Button>
          <Button type="button" variant="outline" onClick={clearForm} disabled={busy}>Clear</Button>
          <Button type="button" variant="outline" onClick={onDelete} disabled={busy || !groupSrNo}>Delete</Button>
          {groupSrNo && <span className="text-sm text-muted-foreground">Group {groupSrNo}</span>}
        </div>
      </section>

      <section className="overflow-hidden rounded-xl border bg-card">
        <div className="flex flex-wrap items-end justify-between gap-3 px-4 py-3">
          <div>
            <h2 className="text-sm font-semibold">Recent entries</h2>
            <p className="text-xs text-muted-foreground">Last 30 days up to the search date.</p>
          </div>
          <div className="w-44">
            <Field label="Search date">
              <Input type="date" value={searchDate} onChange={(e) => setSearchDate(e.target.value)} />
            </Field>
          </div>
        </div>
        {!plant || noPlants ? (
          <p className="border-t px-4 py-6 text-sm text-muted-foreground">Choose a plant to see saved entries.</p>
        ) : recent.data?.length === 0 ? (
          <p className="border-t px-4 py-6 text-sm text-muted-foreground">No entries in this range.</p>
        ) : (
          <div className="overflow-x-auto border-t">
            <table className="w-full min-w-[720px] text-sm">
              <thead>
                <tr>
                  <th className={head}>Date</th>
                  <th className={head}>Shift</th>
                  <th className={head}>Product</th>
                  <th className={headRight}>Consumption</th>
                  <th className={headRight}>Net</th>
                  <th className={head}>Buyer</th>
                  <th className={head}>HOD</th>
                </tr>
              </thead>
              <tbody>
                {(recent.data ?? []).map((row) => (
                  <tr key={row.groupSrNo} className="cursor-pointer border-b last:border-b-0 hover:bg-muted/40" onClick={() => openRecent(row.groupSrNo)}>
                    <td className="px-3 py-2">{row.entryDate}</td>
                    <td className="px-3 py-2">{row.shift}</td>
                    <td className="px-3 py-2">{row.product}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(row.consumption)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(row.netProduction)}</td>
                    <td className="px-3 py-2">{row.buyer}</td>
                    <td className="px-3 py-2">{row.hod}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
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

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium tabular-nums">{value}</span>
    </div>
  );
}
