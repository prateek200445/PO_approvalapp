import { useState, type ReactNode } from "react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

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

type BobbinLine = { key: string; item: string; stock: number; qty: string };

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

const ALLOCATIONS = [
  { loom: "12", quality: "Circular 160 UL", mesh: "10 x 10", gsm: "78", sector: "Food", party: "RP Production", order: "Self", warp: "PP Tape 1100 DTEX", weft: "PP Tape 900 DTEX", fg: "UL Fabric 160 GSM" },
  { loom: "18", quality: "Circular 180 UL", mesh: "12 x 12", gsm: "90", sector: "Chemical", party: "ABC Polymers", order: "PO-4418", warp: "PP Tape 1200 DTEX", weft: "PP Tape 1000 DTEX", fg: "UL Fabric 180 GSM" },
  { loom: "24", quality: "Circular 140 UL", mesh: "8 x 8", gsm: "65", sector: "Agro", party: "Self Production", order: "Self", warp: "PP Tape 900 DTEX", weft: "PP Tape 800 DTEX", fg: "UL Fabric 140 GSM" },
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

function allocation(loom: string) {
  return ALLOCATIONS.find((row) => row.loom === loom) ?? ALLOCATIONS[0];
}

function loomRoll(loom = "12"): LoomRoll {
  const row = allocation(loom);
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
  onSaved,
}: {
  date: string;
  shift: string;
  onSaved: () => void;
}) {
  const [loom, setLoom] = useState("12");
  const [rolls, setRolls] = useState<LoomRoll[]>([loomRoll()]);
  const [bobbins, setBobbins] = useState<BobbinLine[]>([
    { key: uid(), item: "PP Tape 1100 DTEX", stock: 842.5, qty: "" },
    { key: uid(), item: "PP Tape 900 DTEX", stock: 610, qty: "" },
  ]);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<LoomSaved[]>(seedLoomRecent(date, shift));

  const active = allocation(loom);
  const rollNet = rolls.reduce((sum, row) => sum + num(row.net), 0);
  const bobbinQty = bobbins.reduce((sum, row) => sum + num(row.qty), 0);

  function applyLoom(next: string) {
    setLoom(next);
    const row = allocation(next);
    setBobbins([
      { key: uid(), item: row.warp, stock: BOBBIN_STOCK.find((b) => b.item === row.warp)?.stock ?? 0, qty: "" },
      { key: uid(), item: row.weft, stock: BOBBIN_STOCK.find((b) => b.item === row.weft)?.stock ?? 0, qty: "" },
    ]);
  }

  function patchRoll(key: string, patch: Partial<LoomRoll>) {
    setRolls((rows) =>
      rows.map((row) => {
        if (row.key !== key) return row;
        const next = { ...row, ...patch };
        if (patch.loom) {
          const filled = allocation(patch.loom);
          next.quality = filled.quality;
          next.mesh = filled.mesh;
          next.gsm = filled.gsm;
          next.logicGsm = filled.gsm;
          next.sector = filled.sector;
          next.party = filled.party;
          next.order = filled.order;
          next.warp = filled.warp;
          next.weft = filled.weft;
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

  function save() {
    if (rollNet <= 0) {
      toast.error("Enter at least one roll with a net weight.");
      return;
    }
    const id = savedId ?? `L-${recent.length + 1}`;
    const entry: LoomSaved = { id, date, shift, loom, rolls: rolls.filter((row) => num(row.net) > 0).length, net: rollNet, bobbin: bobbinQty, snapshot: rolls, bobbins };
    setRecent((rows) => [entry, ...rows.filter((row) => row.id !== id)]);
    setSavedId(id);
    toast.success(`Loom ${loom} kept on this screen. Database save comes next.`);
    onSaved();
  }

  function open(entry: LoomSaved) {
    setSavedId(entry.id);
    setLoom(entry.loom);
    setRolls(entry.snapshot);
    setBobbins(entry.bobbins);
  }

  function clear() {
    setSavedId(null);
    setRolls([loomRoll(loom)]);
    applyLoom(loom);
  }

  return (
    <div className="space-y-5">
      <section className="shadow-soft overflow-hidden rounded-2xl">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-border/70 px-4 py-4 md:px-5">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-primary">Loom</p>
            <h2 className="text-lg font-semibold tracking-tight">Rolls and bobbins</h2>
          </div>
          <div className="w-40">
            <Field label="Loom number">
              <Pick value={loom} onChange={applyLoom} options={ALLOCATIONS.map((row) => row.loom)} placeholder="Loom" />
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
                    <td className="min-w-[5rem] px-2 py-2"><Pick value={row.loom} options={ALLOCATIONS.map((item) => item.loom)} placeholder="Loom" onChange={(loomNo) => patchRoll(row.key, { loom: loomNo })} /></td>
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
                    <td className="min-w-[11rem] px-2 py-2"><Pick value={row.warp} options={BOBBIN_STOCK.map((item) => item.item)} placeholder="Warp bobbin" onChange={(warp) => patchRoll(row.key, { warp })} /></td>
                    <td className="min-w-[11rem] px-2 py-2"><Pick value={row.weft} options={BOBBIN_STOCK.map((item) => item.item)} placeholder="Weft bobbin" onChange={(weft) => patchRoll(row.key, { weft })} /></td>
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
        <div className="border-t border-border/70 px-4 py-3 md:px-5">
          <div className="mb-2 flex items-center justify-between">
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">Bobbins from Loom Godown</p>
            <Button type="button" variant="outline" size="sm" onClick={() => setRolls((rows) => [...rows, loomRoll(loom)])}>Add roll</Button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] text-sm">
              <thead>
                <tr>
                  <th className={th("left", false)}>Item</th>
                  <th className={th("right", false)}>Stock</th>
                  <th className={th("right", false)}>Consumed</th>
                  <th className={th("left", false)} />
                </tr>
              </thead>
              <tbody>
                {bobbins.map((row) => (
                  <tr key={row.key} className="border-b border-border/60 last:border-b-0">
                    <td className="px-3 py-2">
                      <Pick
                        value={row.item}
                        placeholder="Bobbin"
                        options={BOBBIN_STOCK.map((item) => item.item)}
                        onChange={(item) =>
                          setBobbins((rows) =>
                            rows.map((line) =>
                              line.key === row.key
                                ? { ...line, item, stock: BOBBIN_STOCK.find((stock) => stock.item === item)?.stock ?? 0 }
                                : line,
                            ),
                          )
                        }
                      />
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{money(row.stock)}</td>
                    <td className="px-3 py-2">
                      <Num
                        value={row.qty}
                        onChange={(qty) => setBobbins((rows) => rows.map((line) => (line.key === row.key ? { ...line, qty } : line)))}
                      />
                    </td>
                    <td className="px-2 py-2 text-right">
                      <Button type="button" variant="ghost" size="sm" className="h-8 px-2 text-muted-foreground" onClick={() => setBobbins((rows) => rows.filter((line) => line.key !== row.key))}>Remove</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Button type="button" variant="ghost" size="sm" className="mt-2" onClick={() => setBobbins((rows) => [...rows, { key: uid(), item: "", stock: 0, qty: "" }])}>Add bobbin</Button>
        </div>
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
              ? () => {
                  setRecent((rows) => rows.filter((row) => row.id !== savedId));
                  clear();
                  toast.success("Removed from this screen.");
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
  bobbins: BobbinLine[];
};

function seedLoomRecent(date: string, shift: string): LoomSaved[] {
  const roll = loomRoll("18");
  roll.metre = "1500";
  roll.gross = "190";
  roll.tare = "2";
  roll.cover = "0.4";
  roll.net = "187.60";
  roll.actualGsm = "91";
  return [{ id: "L-earlier", date, shift, loom: "18", rolls: 1, net: 187.6, bobbin: 188, snapshot: [roll], bobbins: [{ key: "b1", item: "PP Tape 1200 DTEX", stock: 455.25, qty: "110" }, { key: "b2", item: "PP Tape 1000 DTEX", stock: 388, qty: "78" }] }];
}

export function NeedlePanel({ date, shift }: { date: string; shift: string }) {
  const [rows, setRows] = useState<NeedleRow[]>([needleRow()]);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<NeedleSaved[]>([
    { id: "N-earlier", date, shift, rolls: 2, net: 86.4, snapshot: [{ ...needleRow(), key: "n1", size: "40", gpm: "42", net: "44.2" }, { ...needleRow(), key: "n2", loom: "6", size: "25", fg: "PP Tie 25mm", quality: "PP Tie 25mm", net: "42.2" }] },
  ]);
  const net = rows.reduce((sum, row) => sum + num(row.net), 0);

  function patch(key: string, next: Partial<NeedleRow>) {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...next } : row)));
  }

  function save() {
    if (net <= 0) {
      toast.error("Enter a net weight on at least one roll.");
      return;
    }
    const id = savedId ?? `N-${recent.length + 1}`;
    const entry: NeedleSaved = { id, date, shift, rolls: rows.filter((row) => num(row.net) > 0).length, net, snapshot: rows };
    setRecent((list) => [entry, ...list.filter((row) => row.id !== id)]);
    setSavedId(id);
    toast.success("Needle loom rolls kept on this screen. Consumption matching comes with the database.");
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
              ? () => {
                  setRecent((list) => list.filter((row) => row.id !== savedId));
                  setSavedId(null);
                  setRows([needleRow()]);
                  toast.success("Removed from this screen.");
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
  onContinue,
}: {
  date: string;
  shift: string;
  onContinue: () => void;
}) {
  const [category, setCategory] = useState<"All" | "IN" | "OUT">("All");
  const [rows, setRows] = useState<LamRoll[]>(LAM_SEED);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [recent, setRecent] = useState<{ id: string; date: string; shift: string; rolls: number; source: number; laminated: number; snapshot: LamRoll[] }[]>([]);

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

  function save() {
    if (chosen.length === 0) {
      toast.error("Tick the rolls to laminate.");
      return;
    }
    if (blocked) {
      toast.error("Each ticked roll needs a laminated item and a laminated net weight.");
      return;
    }
    const id = savedId ?? `LAM-${recent.length + 1}`;
    setRecent((list) => [{ id, date, shift, rolls: chosen.length, source: sourceNet, laminated: lamNet, snapshot: rows }, ...list.filter((row) => row.id !== id)]);
    setSavedId(id);
    toast.success("Lamination rolls kept on this screen. Opening Lamination consumption for this date and shift.");
    onContinue();
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
