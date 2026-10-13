import { useEffect, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  deleteLoom,
  deleteNeedle,
  getLaminationRecent,
  getLaminationRolls,
  getLoomEntry,
  getLoomRecent,
  getLoomSetup,
  getNeedleRecent,
  saveLamination,
  saveLoom,
  saveNeedle,
  type LoomSetupAllocation,
} from "@/lib/plant-production-api";

type LoomRoll = {
  key: string;
  loom: string;
  rollNo: string;
  quality: string;
  mesh: string;
  gsm: string;
  sector: string;
  party: string;
  order: string;
  warp: string;
  weft: string;
  warpCode: string;
  weftCode: string;
  warpQty: string;
  weftQty: string;
  fg: string;
  logicGsm: string;
  metre: string;
  gross: string;
  tare: string;
  cover: string;
  net: string;
  gpm: string;
  actualGsm: string;
  remarks: string;
};

type BobbinSummary = { item: string; stock: number; qty: number };

type LoomMaster = {
  loom: string;
  quality: string;
  mesh: string;
  gsm: string;
  sector: string;
  party: string;
  order: string;
  warp: string;
  weft: string;
  warpCode: string;
  weftCode: string;
  fg: string;
  size: number;
  warpMesh: number;
  weftMesh: number;
  warpDnr: number;
  weftDnr: number;
  rfWidth: number;
  rfNo: number;
  shrinkage: number;
  tube: boolean;
};

type NeedleRow = {
  key: string;
  loom: string;
  rollNo: string;
  size: string;
  gpm: string;
  colour: string;
  marking: string;
  quality: string;
  net: string;
  party: string;
  order: string;
  remarks: string;
  fg: string;
};

type LamRoll = {
  key: string;
  category: "IN" | "OUT";
  rollNo: string;
  metre: string;
  gross: string;
  tare: string;
  net: string;
  quality: string;
  party: string;
  order: string;
  ulCode: string;
  lamCode: string;
  colour: string;
  lamSize: string;
  lamGsm: string;
  lamQuality: string;
  outsideRoll: string;
  lamMetre: string;
  lamGross: string;
  lamTare: string;
  lamNet: string;
  actualGpm: string;
  actualGsm: string;
  gsm: string;
  remarks: string;
  sector: string;
  internalRoll: string;
  originalNet: string;
  checked: boolean;
};

const ALLOCATIONS: LoomMaster[] = [
  { loom: "12", quality: "Circular 160 UL", mesh: "10 x 10", gsm: "78", sector: "Food", party: "RP Production", order: "Self", warp: "PP Tape 1100 DTEX", weft: "PP Tape 900 DTEX", warpCode: "", weftCode: "", fg: "UL Fabric 160 GSM", size: 160, warpMesh: 10, weftMesh: 10, warpDnr: 1100, weftDnr: 900, rfWidth: 0, rfNo: 0, shrinkage: 0.02, tube: false },
  { loom: "18", quality: "Circular 180 UL", mesh: "12 x 12", gsm: "90", sector: "Chemical", party: "ABC Polymers", order: "PO-4418", warp: "PP Tape 1200 DTEX", weft: "PP Tape 1000 DTEX", warpCode: "", weftCode: "", fg: "UL Fabric 180 GSM", size: 180, warpMesh: 12, weftMesh: 12, warpDnr: 1200, weftDnr: 1000, rfWidth: 0, rfNo: 0, shrinkage: 0.02, tube: false },
  { loom: "24", quality: "Circular 140 UL", mesh: "8 x 8", gsm: "65", sector: "Agro", party: "Self Production", order: "Self", warp: "PP Tape 900 DTEX", weft: "PP Tape 800 DTEX", warpCode: "", weftCode: "", fg: "UL Fabric 140 GSM", size: 140, warpMesh: 8, weftMesh: 8, warpDnr: 900, weftDnr: 800, rfWidth: 0, rfNo: 0, shrinkage: 0.02, tube: false },
];

const BOBBIN_STOCK = [
  { item: "PP Tape 1100 DTEX", stock: 842.5 },
  { item: "PP Tape 900 DTEX", stock: 610 },
  { item: "PP Tape 1200 DTEX", stock: 455.25 },
  { item: "PP Tape 1000 DTEX", stock: 388 },
  { item: "PP Tape 800 DTEX", stock: 274.8 },
];

const COLOURS = ["Natural", "White", "Blue", "Black", "Green"];
const NEEDLE_PARTIES = ["RP Production", "ABC Polymers", "Self Production"];
const NEEDLE_ORDERS: Record<string, string[]> = {
  "RP Production": ["Self"],
  "ABC Polymers": ["PO-4418", "PO-4422"],
  "Self Production": ["Self"],
};
const NEEDLE_FG = ["PP Webbing 40mm", "PP Webbing 50mm", "PP Tie 25mm"];

const LAM_SEED: LamRoll[] = [
  seedLam("IN", "KP-L-I26J-01421", "1480", "186.40", "2.10", "184.30", "Circular 160 UL", "RP Production", "Self", "WIP00023", "WIP00025", "Natural"),
  seedLam("IN", "KP-L-I26J-01428", "1320", "162.00", "2.00", "48.50", "Circular 180 UL", "ABC Polymers", "PO-4418", "WIP00005", "WIP00007", "White"),
  seedLam("OUT", "OUT-26J-00914", "980", "121.75", "1.80", "119.95", "Circular 140 UL", "Self Production", "Self", "WIP00006", "WIP00008", "Blue"),
  seedLam("IN", "KP-L-I26J-01302LCUT", "640", "70.00", "0", "70.00", "Laminated cut 160", "RP Production", "Self", "", "", "Natural"),
];

function seedLam(
  category: "IN" | "OUT",
  rollNo: string,
  metre: string,
  gross: string,
  tare: string,
  net: string,
  quality: string,
  party: string,
  order: string,
  ulCode: string,
  lamCode: string,
  colour: string,
): LamRoll {
  return {
    key: rollNo,
    category,
    rollNo,
    metre,
    gross,
    tare,
    net,
    quality,
    party,
    order,
    ulCode,
    lamCode,
    colour,
    lamSize: "",
    lamGsm: "",
    lamQuality: quality,
    outsideRoll: "",
    lamMetre: metre,
    lamGross: "",
    lamTare: tare,
    lamNet: "",
    actualGpm: "",
    actualGsm: "",
    gsm: "",
    remarks: "",
    sector: category === "IN" ? "Food" : "Export",
    internalRoll: "",
    originalNet: net,
    checked: false,
  };
}

function num(value: string) {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function money(n: number) {
  return n.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function uid() {
  return crypto.randomUUID();
}

function allocation(loom: string, masters: LoomMaster[] = ALLOCATIONS) {
  return masters.find((row) => row.loom === loom) ?? masters[0] ?? ALLOCATIONS[0];
}

function round2(value: number) {
  return Math.round(value * 100) / 100;
}

function stockOf(item: string, stock = BOBBIN_STOCK) {
  return stock.find((row) => row.item === item)?.stock ?? 0;
}

function calculateLoom(rolls: LoomRoll[], masters: LoomMaster[] = ALLOCATIONS, stock = BOBBIN_STOCK) {
  const next = rolls.map((row) => ({ ...row }));
  const consumed = new Map<string, number>();
  for (const row of next) {
    const metre = num(row.metre);
    const net = num(row.net);
    if (metre <= 0 || net <= 0) continue;
    const master = allocation(row.loom, masters);
    if (!row.warp) row.warp = master.warp;
    if (!row.weft) row.weft = master.weft;
    if (!row.warpCode) row.warpCode = master.warpCode;
    if (!row.weftCode) row.weftCode = master.weftCode;
    const shrinkage = master.shrinkage === 0 ? 1 : master.shrinkage;
    const mul = master.tube ? 2 : 1;
    const warpCon =
      ((master.warpDnr / 9000) * (master.size / 2.54) * ((mul * metre * master.warpMesh * (1 + shrinkage)) / 1000)) +
      ((master.warpDnr / 9000) * ((master.rfWidth * master.rfNo) / 2.54) * ((master.warpMesh * metre * (1 + shrinkage)) / 1000));
    const weftCon = ((master.weftMesh / 2.54) * master.size * mul * (master.weftDnr / 9000) * (metre / 1000)) * (1 + shrinkage);
    const total = warpCon + weftCon;
    const warpQty = total > 0 ? round2((net / total) * warpCon) : 0;
    const weftQty = round2(net - warpQty);
    row.warpQty = warpQty.toFixed(2);
    row.weftQty = weftQty.toFixed(2);
    const gpm = round2((net / metre) * 1000);
    const gsmBase = (master.size * mul) / 100;
    row.gpm = gpm.toFixed(2);
    row.actualGsm = (gsmBase > 0 ? round2(gpm / gsmBase) : 0).toFixed(2);
    const req = num(row.gsm);
    const ratio = req > 0 ? num(row.actualGsm) / req : 0;
    row.logicGsm = ratio < 1.05 && ratio > 0.95 ? "0" : "1";
    consumed.set(row.warp, round2((consumed.get(row.warp) ?? 0) + warpQty));
    consumed.set(row.weft, round2((consumed.get(row.weft) ?? 0) + weftQty));
  }
  const summary: BobbinSummary[] = [...consumed.entries()].map(([item, qty]) => ({ item, stock: stockOf(item, stock), qty }));
  const over = summary.find((line) => line.qty > line.stock);
  return { rolls: next, summary, stockError: over ? `Consumption is above Loom Godown stock for ${over.item}.` : null };
}

function loomRoll(loom = "12", masters: LoomMaster[] = ALLOCATIONS): LoomRoll {
  const row = allocation(loom, masters);
  return {
    key: uid(),
    ...row,
    rollNo: "",
    warpQty: "",
    weftQty: "",
    logicGsm: row.gsm,
    metre: "",
    gross: "",
    tare: "2",
    cover: "0",
    net: "",
    gpm: "",
    actualGsm: "",
    remarks: "",
  };
}

function needleRow(): NeedleRow {
  return {
    key: uid(),
    loom: "4",
    rollNo: "",
    size: "",
    gpm: "",
    colour: "Natural",
    marking: "",
    quality: "PP Webbing 40mm",
    net: "",
    party: "RP Production",
    order: "Self",
    remarks: "",
    fg: "PP Webbing 40mm",
  };
}

export function LoomPanel({
  date,
  shift,
  company,
  userName,
  onSaved,
}: {
  date: string;
  shift: string;
  company: string;
  userName: string;
  onSaved: () => void;
}) {
  const [masters, setMasters] = useState<LoomMaster[]>(ALLOCATIONS);
  const [stock, setStock] = useState<{ item: string; stock: number; itemCode?: string }[]>(BOBBIN_STOCK);
  const [loom, setLoom] = useState("12");
  const [rolls, setRolls] = useState<LoomRoll[]>([loomRoll()]);
  const [summary, setSummary] = useState<BobbinSummary[] | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<LoomSaved[]>(company ? [] : seedLoomRecent(date, shift));

  useEffect(() => {
    if (!company) return;
    let cancel = false;
    getLoomSetup(company, date)
      .then((data) => {
        if (cancel || data.allocations.length === 0) return;
        setMasters(data.allocations);
        if (data.bobbins.length > 0) setStock(data.bobbins.map((row) => ({ item: row.item, stock: row.stock, itemCode: row.itemCode })));
      })
      .catch(() => undefined);
    getLoomRecent(company, date)
      .then((rows) => {
        if (cancel) return;
        setRecent(rows.map((row) => ({ id: String(row.id), date, shift: row.shift, loom: row.loom, rolls: row.rolls, net: row.net, bobbin: row.net, snapshot: [] })));
      })
      .catch(() => undefined);
    return () => {
      cancel = true;
    };
  }, [company, date]);

  const active = allocation(loom, masters);
  const rollNet = rolls.reduce((sum, row) => sum + num(row.net), 0);
  const bobbinQty = rolls.reduce((sum, row) => sum + num(row.warpQty) + num(row.weftQty), 0);

  function applyLoom(next: string) {
    setLoom(next);
  }

  function patchRoll(key: string, patch: Partial<LoomRoll>) {
    setRolls((rows) =>
      rows.map((row) => {
        if (row.key !== key) return row;
        const next = { ...row, ...patch };
        if (patch.loom) {
          const filled = allocation(patch.loom, masters);
          next.quality = filled.quality;
          next.mesh = filled.mesh;
          next.gsm = filled.gsm;
          next.logicGsm = filled.gsm;
          next.sector = filled.sector;
          next.party = filled.party;
          next.order = filled.order;
          next.warp = filled.warp;
          next.weft = filled.weft;
          next.warpCode = filled.warpCode;
          next.weftCode = filled.weftCode;
          next.fg = filled.fg;
        }
        if ("gross" in patch || "tare" in patch || "cover" in patch) {
          const net = Math.max(0, num(next.gross) - num(next.tare) - num(next.cover));
          next.net = net ? net.toFixed(2) : "";
        }
        return next;
      }),
    );
  }

  function runCalculate(show: boolean) {
    const weighed = rolls.filter((row) => num(row.net) > 0);
    if (weighed.length === 0) {
      toast.error("Enter at least one roll with a net weight.");
      return null;
    }
    if (weighed.some((row) => num(row.metre) <= 0)) {
      toast.error("Enter metre for every roll with a net weight.");
      return null;
    }
    if (weighed.some((row) => row.fg.trim().length === 0)) {
      toast.error("Select an FG code.");
      return null;
    }
    const result = calculateLoom(rolls, masters, stock);
    setRolls(result.rolls);
    if (show) setSummary(result.summary);
    if (result.stockError) toast.error(result.stockError);
    const net = result.rolls.reduce((sum, row) => sum + num(row.net), 0);
    const consumed = result.summary.reduce((sum, row) => sum + row.qty, 0);
    if (Math.round(net * 1000) !== Math.round(consumed * 1000)) {
      toast.error("Consumption does not match production.");
      return null;
    }
    return result;
  }

  async function save() {
    const result = runCalculate(true);
    if (!result) return;
    if (result.stockError) return;
    const net = result.rolls.reduce((sum, row) => sum + num(row.net), 0);
    const consumed = result.summary.reduce((sum, row) => sum + row.qty, 0);
    const weighed = result.rolls.filter((row) => num(row.net) > 0);
    if (!company) {
      const id = savedId ?? `L-${recent.length + 1}`;
      const entry: LoomSaved = { id, date, shift, loom, rolls: weighed.length, net, bobbin: consumed, snapshot: result.rolls };
      setRecent((rows) => [entry, ...rows.filter((row) => row.id !== id)]);
      setSavedId(id);
      setSummary(result.summary);
      toast.success(`Loom ${loom} kept on this screen.`);
      onSaved();
      return;
    }
    try {
      const saved = await saveLoom({
        company,
        userName,
        entryDate: date,
        shift,
        recordLogId: savedId && /^\d+$/.test(savedId) ? Number(savedId) : null,
        rolls: weighed.map((row) => ({
          loomNo: Number(row.loom) || 0,
          rollNo: row.rollNo,
          quality: row.quality,
          mesh: row.mesh,
          gsm: num(row.gsm),
          sector: row.sector,
          metre: num(row.metre),
          gross: num(row.gross),
          tare: num(row.tare),
          cover: num(row.cover),
          net: num(row.net),
          gpm: num(row.gpm),
          actualGsm: num(row.actualGsm),
          remarks: row.remarks,
          party: row.party,
          order: row.order,
          warpQty: num(row.warpQty),
          weftQty: num(row.weftQty),
          warpCode: row.warpCode,
          weftCode: row.weftCode,
          warpName: row.warp,
          weftName: row.weft,
          fgCode: row.fg,
          logic: Number(row.logicGsm) || 0,
        })),
      });
      setSavedId(String(saved.recordLogId));
      const numbers = [...saved.rollNos];
      setRolls(result.rolls.map((row) => (num(row.net) > 0 ? { ...row, rollNo: numbers.shift() || row.rollNo } : row)));
      setSummary(result.summary);
      const listed = await getLoomRecent(company, date);
      setRecent(listed.map((row) => ({ id: String(row.id), date, shift: row.shift, loom: row.loom, rolls: row.rolls, net: row.net, bobbin: row.net, snapshot: [] })));
      toast.success(`Loom entry saved. Record ${saved.recordLogId}.`);
      onSaved();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Save failed");
    }
  }

  async function open(entry: LoomSaved) {
    setSavedId(entry.id);
    setLoom(entry.loom);
    setSummary(null);
    if (entry.snapshot.length > 0) {
      setRolls(entry.snapshot);
      return;
    }
    if (!company || !/^\d+$/.test(entry.id)) return;
    try {
      const rows = await getLoomEntry(company, Number(entry.id));
      setRolls(rows.map((row) => ({
        ...loomRoll(row.loom, masters),
        rollNo: row.rollNo,
        quality: row.quality,
        mesh: row.mesh,
        gsm: String(row.gsm),
        sector: row.sector,
        metre: String(row.metre),
        gross: String(row.gross),
        tare: String(row.tare),
        cover: String(row.cover),
        net: String(row.net),
        gpm: String(row.gpm),
        actualGsm: String(row.actualGsm),
        remarks: row.remarks,
        party: row.party,
        order: row.order,
        warpQty: String(row.warpQty),
        weftQty: String(row.weftQty),
        warpCode: row.warpCode,
        weftCode: row.weftCode,
        fg: row.fg,
      })));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not open the entry");
    }
  }

  function clear() {
    setSavedId(null);
    setRolls([loomRoll(loom, masters)]);
    setSummary(null);
  }

  return (
    <div className="space-y-5">
      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-border/70 px-4 py-4 md:px-5">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-primary">Loom</p>
            <h2 className="text-lg font-semibold tracking-tight">Rolls</h2>
          </div>
          <div className="w-40">
            <Field label="Loom number">
              <Pick value={loom} onChange={applyLoom} options={masters.map((row) => row.loom)} placeholder="Loom" />
            </Field>
          </div>
        </div>
        <dl className="grid gap-3 border-b border-border/70 bg-muted/30 px-4 py-3 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7 md:px-5">
          <Meta label="Quality" value={active.quality} />
          <Meta label="Mesh" value={active.mesh} />
          <Meta label="Required GSM" value={active.gsm} />
          <Meta label="Sector" value={active.sector} />
          <Meta label="Party" value={active.party} />
          <Meta label="Order" value={active.order} />
          <Meta label="Fabric" value={active.fg} />
        </dl>
        <GridFrame>
          <table className="w-max min-w-full border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                <th className={groupTh()} colSpan={6}>Roll</th>
                <th className={groupTh()} colSpan={5}>Weight</th>
                <th className={groupTh()} colSpan={3}>Actual</th>
                <th className={groupTh()} colSpan={2}>Order</th>
                <th className={groupTh()} colSpan={6}>Bobbin on the roll</th>
                <th className={groupTh()} />
              </tr>
              <tr>
                {["Loom no", "Roll no", "Quality", "Mesh", "Req GSM", "Sector", "Metre", "Gross wt", "Tare wt", "Cover wt", "Net wt", "Actual GPM", "Actual GSM", "Remarks", "Party name", "Marketing inv. no.", "Warp qty", "Weft qty", "Warp bobbin", "Weft bobbin", "FG code", "Logic GSM", ""].map((label) => (
                  <th key={label || "x"} className={th(["Metre", "Gross wt", "Tare wt", "Cover wt", "Net wt", "Actual GPM", "Actual GSM", "Req GSM", "Warp qty", "Weft qty", "Logic GSM"].includes(label) ? "right" : "left")}>{label}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rolls.map((row) => {
                const gap = Math.abs(num(row.actualGsm) - num(row.gsm));
                const warn = num(row.actualGsm) > 0 && gap > 5;
                return (
                  <tr key={row.key} className="border-b border-border/60 last:border-b-0">
                    <td className="min-w-[5rem] px-2 py-2"><Pick value={row.loom} options={masters.map((item) => item.loom)} placeholder="Loom" onChange={(loomNo) => patchRoll(row.key, { loom: loomNo })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 min-w-[9rem] font-mono text-xs" value={row.rollNo} placeholder="On save" onChange={(e) => patchRoll(row.key, { rollNo: e.target.value })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 min-w-[10rem]" value={row.quality} onChange={(e) => patchRoll(row.key, { quality: e.target.value })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 w-24" value={row.mesh} onChange={(e) => patchRoll(row.key, { mesh: e.target.value })} /></td>
                    <td className="px-2 py-2"><Num value={row.gsm} onChange={(gsm) => patchRoll(row.key, { gsm })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 w-28" value={row.sector} onChange={(e) => patchRoll(row.key, { sector: e.target.value })} /></td>
                    <td className="px-2 py-2"><Num value={row.metre} onChange={(metre) => patchRoll(row.key, { metre })} /></td>
                    <td className="px-2 py-2"><Num value={row.gross} onChange={(gross) => patchRoll(row.key, { gross })} /></td>
                    <td className="px-2 py-2"><Num value={row.tare} onChange={(tare) => patchRoll(row.key, { tare })} /></td>
                    <td className="px-2 py-2"><Num value={row.cover} onChange={(cover) => patchRoll(row.key, { cover })} /></td>
                    <td className="px-2 py-2"><ReadNum value={row.net} /></td>
                    <td className="px-2 py-2"><Num value={row.gpm} onChange={(gpm) => patchRoll(row.key, { gpm })} /></td>
                    <td className="px-2 py-2"><Num value={row.actualGsm} warn={warn} onChange={(actualGsm) => patchRoll(row.key, { actualGsm })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 min-w-[8rem]" value={row.remarks} onChange={(e) => patchRoll(row.key, { remarks: e.target.value })} /></td>
                    <td className="min-w-[10rem] px-2 py-2"><Pick value={row.party} options={NEEDLE_PARTIES} placeholder="Party" onChange={(party) => patchRoll(row.key, { party, order: NEEDLE_ORDERS[party]?.[0] ?? row.order })} /></td>
                    <td className="min-w-[8rem] px-2 py-2"><Pick value={row.order} options={NEEDLE_ORDERS[row.party] ?? []} placeholder="Invoice" onChange={(order) => patchRoll(row.key, { order })} /></td>
                    <td className="px-2 py-2"><Num value={row.warpQty} onChange={(warpQty) => patchRoll(row.key, { warpQty })} /></td>
                    <td className="px-2 py-2"><Num value={row.weftQty} onChange={(weftQty) => patchRoll(row.key, { weftQty })} /></td>
                    <td className="min-w-[11rem] px-2 py-2"><Pick value={row.warp} options={stock.map((item) => item.item)} placeholder="Warp bobbin" onChange={(warp) => patchRoll(row.key, { warp, warpCode: stock.find((item) => item.item === warp)?.itemCode ?? "" })} /></td>
                    <td className="min-w-[11rem] px-2 py-2"><Pick value={row.weft} options={stock.map((item) => item.item)} placeholder="Weft bobbin" onChange={(weft) => patchRoll(row.key, { weft, weftCode: stock.find((item) => item.item === weft)?.itemCode ?? "" })} /></td>
                    <td className="px-2 py-2"><Input className="h-8 min-w-[10rem]" value={row.fg} onChange={(e) => patchRoll(row.key, { fg: e.target.value })} /></td>
                    <td className="px-2 py-2"><Num value={row.logicGsm} onChange={(logicGsm) => patchRoll(row.key, { logicGsm })} /></td>
                    <td className="px-2 py-2 text-right">
                      <Button type="button" variant="ghost" size="sm" className="h-8 px-2 text-muted-foreground" onClick={() => setRolls((rows) => rows.filter((item) => item.key !== row.key))}>Remove</Button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </GridFrame>
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border/70 px-4 py-3 md:px-5">
          <p className="text-xs text-muted-foreground">Calculate fills warp and weft from the loom allocation, then shows Loom Godown consumption.</p>
          <div className="flex gap-2">
            <Button type="button" variant="outline" size="sm" onClick={() => setRolls((rows) => [...rows, loomRoll(loom, masters)])}>Add roll</Button>
            <Button type="button" size="sm" onClick={() => runCalculate(true)}>Calculate</Button>
          </div>
        </div>
        <Dialog open={summary !== null} onOpenChange={(open) => !open && setSummary(null)}>
          <DialogContent className="sm:max-w-lg">
            <DialogHeader>
              <DialogTitle>Bobbin consumption summary</DialogTitle>
              <DialogDescription>Loom Godown stock against the quantity this entry will consume.</DialogDescription>
            </DialogHeader>
            <div className="overflow-hidden rounded-xl border border-border/70">
              <div className="grid grid-cols-[1fr_6.5rem_6.5rem] bg-muted/80 px-3 py-2 text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground">
                <span>Item</span>
                <span className="text-right">Stock</span>
                <span className="text-right">Consumed</span>
              </div>
              {(summary ?? []).map((line) => (
                <div key={line.item} className={cn("grid grid-cols-[1fr_6.5rem_6.5rem] border-t border-border/50 px-3 py-2.5 text-sm", line.qty > line.stock && "bg-destructive/10 text-destructive")}>
                  <span className="min-w-0 truncate">{line.item}</span>
                  <span className="text-right tabular-nums">{money(line.stock)}</span>
                  <span className="text-right font-medium tabular-nums">{money(line.qty)}</span>
                </div>
              ))}
            </div>
          </DialogContent>
        </Dialog>
        <Totals
          stats={[
            ["Roll net", money(rollNet)],
            ["Bobbins consumed", money(bobbinQty)],
            ["Rolls", String(rolls.filter((row) => num(row.net) > 0).length)],
          ]}
          saved={savedId}
          onSave={save}
          onClear={clear}
          onDelete={
            savedId
              ? async () => {
                  try {
                    if (company && /^\d+$/.test(savedId)) await deleteLoom({ company, userName, entryDate: date, id: Number(savedId) });
                    setRecent((rows) => rows.filter((row) => row.id !== savedId));
                    clear();
                    toast.success("Deleted.");
                  } catch (err) {
                    toast.error(err instanceof Error ? err.message : "Delete failed");
                  }
                }
              : undefined
          }
        />
      </section>
      <Recent
        date={date}
        columns={["Date", "Shift", "Loom", "Rolls", "Net", "Bobbins"]}
        rows={recent.map((row) => ({
          id: row.id,
          active: row.id === savedId,
          cells: [row.date, row.shift, row.loom, String(row.rolls), money(row.net), money(row.bobbin)],
          onOpen: () => open(row),
        }))}
      />
    </div>
  );
}

type LoomSaved = {
  id: string;
  date: string;
  shift: string;
  loom: string;
  rolls: number;
  net: number;
  bobbin: number;
  snapshot: LoomRoll[];
};

function seedLoomRecent(date: string, shift: string): LoomSaved[] {
  const roll = loomRoll("18");
  roll.metre = "1500";
  roll.gross = "190";
  roll.tare = "2";
  roll.cover = "0.4";
  roll.net = "187.60";
  roll.warpQty = "109.60";
  roll.weftQty = "78.00";
  roll.gpm = "125.07";
  roll.actualGsm = "69.48";
  roll.logicGsm = "1";
  return [{ id: "L-earlier", date, shift, loom: "18", rolls: 1, net: 187.6, bobbin: 187.6, snapshot: [roll] }];
}

export function NeedlePanel({ date, shift, company, userName }: { date: string; shift: string; company: string; userName: string }) {
  const [rows, setRows] = useState<NeedleRow[]>([needleRow()]);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<NeedleSaved[]>(company ? [] : [
    { id: "N-earlier", date, shift, rolls: 2, net: 86.4, snapshot: [{ ...needleRow(), key: "n1", size: "40", gpm: "42", net: "44.2" }, { ...needleRow(), key: "n2", loom: "6", size: "25", fg: "PP Tie 25mm", quality: "PP Tie 25mm", net: "42.2" }] },
  ]);
  const net = rows.reduce((sum, row) => sum + num(row.net), 0);

  useEffect(() => {
    if (!company) return;
    getNeedleRecent(company, date)
      .then((list) => setRecent(list.map((row) => ({
        id: String(row.id),
        date,
        shift: row.shift,
        rolls: 1,
        net: row.net,
        snapshot: [{ ...needleRow(), key: String(row.id), loom: row.loom, rollNo: row.rollNo, net: String(row.net) }],
      }))))
      .catch(() => undefined);
  }, [company, date]);

  function patch(key: string, next: Partial<NeedleRow>) {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...next } : row)));
  }

  async function save() {
    if (net <= 0) {
      toast.error("Enter a net weight on at least one roll.");
      return;
    }
    const weighed = rows.filter((row) => num(row.net) > 0);
    if (!company) {
      const id = savedId ?? `N-${recent.length + 1}`;
      setRecent((list) => [{ id, date, shift, rolls: weighed.length, net, snapshot: rows }, ...list.filter((row) => row.id !== id)]);
      setSavedId(id);
      toast.success("Needle loom rolls kept on this screen.");
      return;
    }
    try {
      const saved = await saveNeedle({
        company,
        userName,
        entryDate: date,
        shift,
        rolls: weighed.map((row) => ({
          transId: /^\d+$/.test(row.key) ? Number(row.key) : null,
          loomNo: Number(row.loom) || 0,
          rollNo: row.rollNo,
          size: num(row.size),
          reqGpm: num(row.gpm),
          colour: row.colour,
          marking: row.marking,
          quality: row.quality,
          net: num(row.net),
          party: row.party,
          order: row.order,
          remarks: row.remarks,
          fgCode: row.fg,
          rollType: "PP Webbing & Tie",
        })),
      });
      setRows(rows.map((row) => {
        const match = saved.rolls.find((item) => item.rollNo && row.rollNo === item.rollNo);
        const created = saved.rolls[weighed.indexOf(row)];
        return created ? { ...row, key: String(created.transId), rollNo: created.rollNo || row.rollNo } : match ? { ...row, key: String(match.transId), rollNo: match.rollNo } : row;
      }));
      setSavedId(String(saved.rolls[0]?.transId ?? ""));
      const listed = await getNeedleRecent(company, date);
      setRecent(listed.map((row) => ({ id: String(row.id), date, shift: row.shift, rolls: 1, net: row.net, snapshot: [{ ...needleRow(), key: String(row.id), loom: row.loom, rollNo: row.rollNo, net: String(row.net) }] })));
      toast.success("Needle loom rolls saved. Match this net in Needle Loom consumption before the rolls are closed.");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Save failed");
    }
  }

  return (
    <div className="space-y-5">
      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-4 py-4 md:px-5">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-primary">Needle loom</p>
            <h2 className="text-lg font-semibold tracking-tight">Webbing rolls</h2>
          </div>
          <span className="rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">PP Webbing & Tie</span>
        </div>
        <GridFrame>
          <table className="w-max min-w-full border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                <th className={groupTh()} colSpan={7}>Roll</th>
                <th className={groupTh()} colSpan={1}>Weight</th>
                <th className={groupTh()} colSpan={4}>Order</th>
                <th className={groupTh()} />
              </tr>
              <tr>
                {["Loom no", "Roll no", "Size (cm)", "Req GPM", "Colour", "Marking", "Quality", "Net wt", "Party name", "Marketing inv. no.", "Remarks", "FG code", ""].map((label) => (
                  <th key={label || "x"} className={th(label === "Net wt" || label === "Size (cm)" || label === "Req GPM" ? "right" : "left")}>{label}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.key} className="border-b border-border/60 last:border-b-0">
                  <td className="px-2 py-2"><Input className="h-8 w-16 text-right tabular-nums" value={row.loom} onChange={(e) => patch(row.key, { loom: e.target.value })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[9rem] font-mono text-xs" value={row.rollNo} placeholder="On save" onChange={(e) => patch(row.key, { rollNo: e.target.value })} /></td>
                  <td className="px-2 py-2"><Num value={row.size} onChange={(size) => patch(row.key, { size })} /></td>
                  <td className="px-2 py-2"><Num value={row.gpm} onChange={(gpm) => patch(row.key, { gpm })} /></td>
                  <td className="min-w-[8rem] px-2 py-2"><Pick value={row.colour} options={COLOURS} placeholder="Colour" onChange={(colour) => patch(row.key, { colour })} /></td>
                  <td className="px-2 py-2"><Input className="h-8" value={row.marking} onChange={(e) => patch(row.key, { marking: e.target.value })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[8rem]" value={row.quality} onChange={(e) => patch(row.key, { quality: e.target.value })} /></td>
                  <td className="px-2 py-2"><Num value={row.net} onChange={(netWeight) => patch(row.key, { net: netWeight })} /></td>
                  <td className="min-w-[10rem] px-2 py-2">
                    <Pick
                      value={row.party}
                      options={NEEDLE_PARTIES}
                      placeholder="Party"
                      onChange={(party) => patch(row.key, { party, order: NEEDLE_ORDERS[party]?.[0] ?? "" })}
                    />
                  </td>
                  <td className="min-w-[8rem] px-2 py-2">
                    <Pick value={row.order} options={NEEDLE_ORDERS[row.party] ?? []} placeholder="Invoice" onChange={(order) => patch(row.key, { order })} />
                  </td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[7rem]" value={row.remarks} onChange={(e) => patch(row.key, { remarks: e.target.value })} /></td>
                  <td className="min-w-[10rem] px-2 py-2"><Pick value={row.fg} options={NEEDLE_FG} placeholder="FG code" onChange={(fg) => patch(row.key, { fg })} /></td>
                  <td className="px-2 py-2">
                    <Button type="button" variant="ghost" size="sm" className="h-8 px-2 text-muted-foreground" onClick={() => setRows((current) => current.filter((item) => item.key !== row.key))}>Remove</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </GridFrame>
        <div className="px-4 py-2 md:px-5">
          <Button type="button" variant="outline" size="sm" onClick={() => setRows((current) => [...current, needleRow()])}>Add roll</Button>
        </div>
        <Totals
          stats={[["Rolls", String(rows.filter((row) => num(row.net) > 0).length)], ["Net weight", money(net)], ["Type", "PP Webbing & Tie"]]}
          saved={savedId}
          onSave={save}
          onClear={() => {
            setSavedId(null);
            setRows([needleRow()]);
          }}
          onDelete={
            savedId
              ? async () => {
                  try {
                    if (company && /^\d+$/.test(savedId)) await deleteNeedle({ company, userName, entryDate: date, id: Number(savedId) });
                    setRecent((list) => list.filter((row) => row.id !== savedId));
                    setSavedId(null);
                    setRows([needleRow()]);
                    toast.success("Deleted.");
                  } catch (err) {
                    toast.error(err instanceof Error ? err.message : "Delete failed");
                  }
                }
              : undefined
          }
        />
      </section>
      <Recent
        date={date}
        columns={["Date", "Shift", "Rolls", "Net"]}
        rows={recent.map((row) => ({
          id: row.id,
          active: row.id === savedId,
          cells: [row.date, row.shift, String(row.rolls), money(row.net)],
          onOpen: () => {
            setSavedId(row.id);
            setRows(row.snapshot);
          },
        }))}
      />
    </div>
  );
}

type NeedleSaved = { id: string; date: string; shift: string; rolls: number; net: number; snapshot: NeedleRow[] };

export function LaminationPanel({
  date,
  shift,
  company,
  userName,
  onContinue,
}: {
  date: string;
  shift: string;
  company: string;
  userName: string;
  onContinue: () => void;
}) {
  const [category, setCategory] = useState<"All" | "IN" | "OUT">("All");
  const [rows, setRows] = useState<LamRoll[]>(LAM_SEED);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<{ id: string; date: string; shift: string; rolls: number; source: number; laminated: number; snapshot: LamRoll[] }[]>([]);

  useEffect(() => {
    if (!company) return;
    getLaminationRolls(company, date)
      .then((list) => {
        if (list.length === 0) return;
        setRows(list.map((row) => {
          const seeded = seedLam(row.category === "OUT" ? "OUT" : "IN", row.rollNo, String(row.metre), String(row.gross), String(row.tare), String(row.net), row.quality, row.party, row.order, row.ulCode, row.lamCode, "");
          return { ...seeded, gsm: row.gsm, outsideRoll: row.outsideRoll, originalNet: String(row.originalNet), lamGsm: row.gsm };
        }));
      })
      .catch(() => undefined);
    getLaminationRecent(company, date)
      .then((list) => setRecent(list.map((row) => ({ id: String(row.id), date, shift: row.shift, rolls: 1, source: row.source, laminated: row.laminated, snapshot: [] }))))
      .catch(() => undefined);
  }, [company, date]);

  const visible = rows.filter((row) => category === "All" || row.category === category);
  const chosen = rows.filter((row) => row.checked);
  const sourceNet = chosen.reduce((sum, row) => sum + num(row.net), 0);
  const lamNet = chosen.reduce((sum, row) => sum + num(row.lamNet), 0);
  const blocked = chosen.some((row) => !row.lamCode || num(row.lamNet) <= 0);

  function patch(key: string, next: Partial<LamRoll>) {
    setRows((current) =>
      current.map((row) => {
        if (row.key !== key) return row;
        const merged = { ...row, ...next };
        if ("lamGross" in next || "lamTare" in next) {
          const net = Math.max(0, num(merged.lamGross) - num(merged.lamTare));
          merged.lamNet = net ? net.toFixed(2) : "";
        }
        return merged;
      }),
    );
  }

  async function save() {
    if (chosen.length === 0) {
      toast.error("Tick the rolls to laminate.");
      return;
    }
    if (blocked) {
      toast.error("Each ticked roll needs a laminated item and a laminated net weight.");
      return;
    }
    if (!company) {
      const id = savedId ?? `LAM-${recent.length + 1}`;
      setRecent((list) => [{ id, date, shift, rolls: chosen.length, source: sourceNet, laminated: lamNet, snapshot: rows }, ...list.filter((row) => row.id !== id)]);
      setSavedId(id);
      toast.success("Lamination rolls kept on this screen. Opening Lamination consumption for this date and shift.");
      onContinue();
      return;
    }
    try {
      const saved = await saveLamination({
        company,
        userName,
        entryDate: date,
        shift,
        rolls: chosen.map((row) => ({
          sourceRoll: row.rollNo,
          sourceMetre: num(row.metre),
          sourceGross: num(row.gross),
          sourceTare: num(row.tare),
          sourceNet: num(row.net),
          lamRoll: `${row.rollNo}L`,
          lamSize: num(row.lamSize),
          lamGsm: num(row.lamGsm),
          colour: row.colour,
          quality: row.lamQuality || row.quality,
          category: row.category,
          lamMetre: num(row.lamMetre),
          lamGross: num(row.lamGross),
          lamTare: num(row.lamTare),
          lamNet: num(row.lamNet),
          actualGpm: num(row.actualGpm),
          actualGsm: num(row.actualGsm),
          remarks: row.remarks,
          sector: row.sector,
          internalRoll: row.internalRoll,
          party: row.party,
          order: row.order,
          ulCode: row.ulCode,
          lamCode: row.lamCode,
        })),
      });
      setSavedId(String(saved.rolls[0]?.transId ?? ""));
      const listed = await getLaminationRecent(company, date);
      setRecent(listed.map((row) => ({ id: String(row.id), date, shift: row.shift, rolls: 1, source: row.source, laminated: row.laminated, snapshot: [] })));
      toast.success("Lamination rolls saved. Opening Lamination consumption for this date and shift.");
      onContinue();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Save failed");
    }
  }

  return (
    <div className="space-y-5">
      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-4 py-4 md:px-5">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-primary">Lamination rolls</p>
            <h2 className="text-lg font-semibold tracking-tight">Unlaminated to laminated</h2>
          </div>
          <div className="inline-flex rounded-full bg-muted p-1">
            {(["All", "IN", "OUT"] as const).map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setCategory(item)}
                className={cn(
                  "rounded-full px-3 py-1 text-xs font-medium transition-colors",
                  category === item ? "nav-3d-active" : "text-muted-foreground hover:text-foreground",
                )}
              >
                {item === "All" ? "All" : item}
              </button>
            ))}
          </div>
        </div>
        <GridFrame>
          <table className="w-max min-w-full border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                <th className={groupTh()} colSpan={6}>Unlaminated roll</th>
                <th className={groupTh()} colSpan={16}>Laminated roll</th>
                <th className={groupTh()} colSpan={5}>Order and item</th>
              </tr>
              <tr>
                <th className={th("left")}>#</th>
                {["Roll no", "Metre", "Gross wt", "Tare wt", "Net wt", "Lam roll no", "Lam size", "Lam GSM", "Colour", "Lam quality", "Outside roll no", "Category", "Lam metre", "Lam gross wt", "Lam tare wt", "Lam net wt", "Actual GPM", "Actual GSM", "Remarks", "Sector", "Internal roll no", "Party name", "Buyer order no", "FG UL code", "FG LAM code", "Ori. net wt"].map((label) => (
                  <th key={label} className={th(/wt|metre|GSM|GPM|size/i.test(label) ? "right" : "left")}>{label}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {visible.map((row) => (
                <tr key={row.key} className={cn("border-b border-border/60 last:border-b-0", row.checked && "bg-primary/5")}>
                  <td className="px-3 py-2">
                    <input type="checkbox" className="h-4 w-4 accent-[var(--primary)]" checked={row.checked} onChange={(e) => patch(row.key, { checked: e.target.checked })} />
                  </td>
                  <td className="bg-muted/40 px-2 py-2 font-medium">{row.rollNo}</td>
                  <td className="bg-muted/40 px-2 py-2 text-right tabular-nums">{row.metre}</td>
                  <td className="bg-muted/40 px-2 py-2 text-right tabular-nums">{row.gross}</td>
                  <td className="bg-muted/40 px-2 py-2 text-right tabular-nums">{row.tare}</td>
                  <td className="bg-muted/40 px-2 py-2 text-right tabular-nums">{row.net}</td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[9rem] font-mono text-xs" value={`${row.rollNo}L`} readOnly /></td>
                  <td className="px-2 py-2"><Input className="h-8 w-20" value={row.lamSize} onChange={(e) => patch(row.key, { lamSize: e.target.value })} /></td>
                  <td className="px-2 py-2"><Num value={row.lamGsm} onChange={(lamGsm) => patch(row.key, { lamGsm })} /></td>
                  <td className="min-w-[8rem] px-2 py-2"><Pick value={row.colour} options={COLOURS} placeholder="Colour" onChange={(colour) => patch(row.key, { colour })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[9rem]" value={row.lamQuality} onChange={(e) => patch(row.key, { lamQuality: e.target.value })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[8rem]" value={row.outsideRoll} onChange={(e) => patch(row.key, { outsideRoll: e.target.value })} /></td>
                  <td className="bg-muted/40 px-2 py-2">{row.category}</td>
                  <td className="px-2 py-2"><Num value={row.lamMetre} onChange={(lamMetre) => patch(row.key, { lamMetre })} /></td>
                  <td className="px-2 py-2"><Num value={row.lamGross} onChange={(lamGross) => patch(row.key, { lamGross })} /></td>
                  <td className="px-2 py-2"><Num value={row.lamTare} onChange={(lamTare) => patch(row.key, { lamTare })} /></td>
                  <td className="px-2 py-2"><ReadNum value={row.lamNet} /></td>
                  <td className="px-2 py-2"><Num value={row.actualGpm} onChange={(actualGpm) => patch(row.key, { actualGpm })} /></td>
                  <td className="px-2 py-2"><Num value={row.actualGsm} onChange={(actualGsm) => patch(row.key, { actualGsm })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[8rem]" value={row.remarks} onChange={(e) => patch(row.key, { remarks: e.target.value })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 w-24" value={row.sector} onChange={(e) => patch(row.key, { sector: e.target.value })} /></td>
                  <td className="px-2 py-2"><Input className="h-8 min-w-[8rem]" value={row.internalRoll} onChange={(e) => patch(row.key, { internalRoll: e.target.value })} /></td>
                  <td className="bg-muted/40 px-2 py-2">{row.party}</td>
                  <td className="bg-muted/40 px-2 py-2">{row.order}</td>
                  <td className="bg-muted/40 px-2 py-2 text-muted-foreground">{row.ulCode || "—"}</td>
                  <td className="px-2 py-2">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs">{row.lamCode || "—"}</span>
                      {!row.lamCode && <span className="shrink-0 rounded-full bg-warning/15 px-2 py-0.5 text-[10px] font-semibold text-warning-foreground">No item</span>}
                    </div>
                  </td>
                  <td className="bg-muted/40 px-2 py-2 text-right tabular-nums">{row.originalNet}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </GridFrame>
        {visible.length === 0 && <p className="px-5 py-10 text-center text-sm text-muted-foreground">No rolls in this category.</p>}
        <Totals
          stats={[
            ["Source net", money(sourceNet)],
            ["Laminated net", money(lamNet)],
            ["Difference", money(lamNet - sourceNet)],
          ]}
          saved={savedId}
          saveDisabled={blocked && chosen.length > 0}
          onSave={save}
          onClear={() => {
            setSavedId(null);
            setRows(LAM_SEED.map((row) => ({ ...row, checked: false, lamGross: "", lamNet: "", actualGsm: "", remarks: "" })));
          }}
        />
      </section>
      <Recent
        date={date}
        columns={["Date", "Shift", "Rolls", "Source", "Laminated"]}
        rows={recent.map((row) => ({
          id: row.id,
          active: row.id === savedId,
          cells: [row.date, row.shift, String(row.rolls), money(row.source), money(row.laminated)],
          onOpen: () => {
            setSavedId(row.id);
            setRows(row.snapshot);
          },
        }))}
      />
    </div>
  );
}

function Totals({
  stats,
  saved,
  saveDisabled,
  onSave,
  onClear,
  onDelete,
}: {
  stats: [string, string][];
  saved: string | null;
  saveDisabled?: boolean;
  onSave: () => void;
  onClear: () => void;
  onDelete?: () => void;
}) {
  return (
    <div className="flex flex-col gap-4 border-t border-border/70 bg-muted/20 px-4 py-4 md:flex-row md:items-center md:justify-between md:px-5">
      <div className="grid flex-1 grid-cols-3 gap-3">
        {stats.map(([label, value]) => (
          <div key={label}>
            <p className="text-[11px] uppercase tracking-[0.14em] text-muted-foreground">{label}</p>
            <p className="text-xl font-semibold tracking-tight tabular-nums">{value}</p>
          </div>
        ))}
      </div>
      <div className="flex flex-wrap items-center gap-2">
        {saved && <span className="text-xs text-muted-foreground">{saved}</span>}
        {onDelete && (
          <button type="button" className="px-2 text-sm text-destructive hover:underline" onClick={onDelete}>
            Delete
          </button>
        )}
        <Button type="button" variant="outline" onClick={onClear}>Clear</Button>
        <Button type="button" onClick={onSave} disabled={saveDisabled}>{saved ? "Update" : "Save"}</Button>
      </div>
    </div>
  );
}

const RECENT_RIGHT = new Set(["Rolls", "Net", "Bobbins", "Source", "Laminated"]);

function recentTemplate(columns: string[]) {
  return columns
    .map((column) => (RECENT_RIGHT.has(column) ? "7.25rem" : column === "Shift" ? "5.25rem" : "minmax(7rem,1fr)"))
    .join(" ");
}

function Recent({
  date,
  columns,
  rows,
}: {
  date: string;
  columns: string[];
  rows: { id: string; active: boolean; cells: string[]; onOpen: () => void }[];
}) {
  const template = recentTemplate(columns);
  return (
    <section className="shadow-soft overflow-hidden rounded-2xl">
      <div className="flex items-center justify-between gap-3 px-4 py-3.5 md:px-5">
        <div>
          <h2 className="text-sm font-semibold tracking-tight">Recent entries</h2>
          <p className="text-xs text-muted-foreground">On this screen for {date}. Database history connects later.</p>
        </div>
        <span className="rounded-full bg-muted px-2.5 py-1 text-[11px] font-medium tabular-nums text-muted-foreground">
          {rows.length === 1 ? "1 entry" : `${rows.length} entries`}
        </span>
      </div>
      {rows.length === 0 ? (
        <p className="border-t border-border/70 px-5 py-10 text-center text-sm text-muted-foreground">No entries for this day.</p>
      ) : (
        <div className="max-h-80 overflow-auto border-t border-border/70">
          <div className="min-w-[36rem]">
            <div className="sticky top-0 z-10 grid border-b border-border/70 bg-muted/95 px-2 backdrop-blur-sm" style={{ gridTemplateColumns: template }}>
              {columns.map((column) => (
                <div key={column} className={cn("px-3 py-2.5 text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground", RECENT_RIGHT.has(column) ? "text-right" : "text-left")}>
                  {column}
                </div>
              ))}
            </div>
            {rows.map((row) => (
              <button
                key={row.id}
                type="button"
                onClick={row.onOpen}
                className={cn(
                  "grid w-full border-b border-border/40 px-2 text-left text-sm transition last:border-b-0 hover:bg-muted/40",
                  row.active && "bg-primary/10",
                )}
                style={{ gridTemplateColumns: template }}
              >
                {row.cells.map((cell, index) => {
                  const column = columns[index] ?? "";
                  const right = RECENT_RIGHT.has(column);
                  return (
                    <span key={`${row.id}-${index}`} className={cn("flex min-w-0 items-center px-3 py-3", right ? "justify-end font-medium tabular-nums" : "justify-start")}>
                      {column === "Shift" ? (
                        <span className="rounded-md bg-primary/10 px-2 py-0.5 text-xs font-semibold text-primary">Shift {cell}</span>
                      ) : (
                        <span className={cn("min-w-0", right ? "" : "truncate", column === "Date" && "font-medium")}>{cell}</span>
                      )}
                    </span>
                  );
                })}
              </button>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}

function GridFrame({ children }: { children: ReactNode }) {
  return <div className="max-h-[32rem] overflow-auto">{children}</div>;
}

function groupTh() {
  return "sticky top-0 z-30 h-8 border-b border-border/50 bg-muted px-2 py-0 text-left text-[10px] font-semibold uppercase leading-8 tracking-[0.16em] text-muted-foreground";
}

function Meta({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <dt className="text-[11px] uppercase tracking-[0.14em] text-muted-foreground">{label}</dt>
      <dd className="truncate text-sm font-medium">{value}</dd>
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

function Pick({ value, onChange, options, placeholder }: { value: string; onChange: (value: string) => void; options: string[]; placeholder: string }) {
  return (
    <SearchableSelect
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      options={options.filter(Boolean).map((option) => ({ value: option, label: option }))}
    />
  );
}

function Num({ value, onChange, warn }: { value: string; onChange: (value: string) => void; warn?: boolean }) {
  return (
    <Input
      inputMode="decimal"
      className={cn("h-8 w-[5.5rem] text-right tabular-nums", warn && "border-warning text-warning-foreground")}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

function ReadNum({ value }: { value: string }) {
  return (
    <div className="flex h-8 w-[5.5rem] items-center justify-end rounded-md border border-input bg-muted/50 px-3 text-sm font-medium tabular-nums">
      {money(num(value))}
    </div>
  );
}

function th(align: "left" | "right", sticky = true) {
  return cn(
    "whitespace-nowrap border-b border-border/70 bg-card px-2 py-2 text-[11px] font-medium uppercase tracking-[0.12em] text-muted-foreground",
    sticky && "sticky top-8 z-20",
    align === "right" ? "text-right" : "text-left",
  );
}
