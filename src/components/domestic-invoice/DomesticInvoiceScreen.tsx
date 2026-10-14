import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Calculator, Loader2, Printer, RefreshCw, RotateCcw, Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { useAuth } from "@/lib/auth-context";
import {
  blankInvoice, checkPacking, getBuyerTax, getCommodityTax, getConsignees, getCurrencyValue, getInsurance,
  getInvoice, getInvoices, getLookups, getPackingLists, getPrint, getTcs, getUserCompany, getVendorAddress,
  previewInvoicePdf, saveInvoice, updateEway, updateInvoice, type DomesticInvoice, type InvoiceLine,
} from "@/lib/domestic-invoice-api";
import { blankLine, calculateInvoice, OEL_COMPANY } from "@/lib/domestic-invoice-calc";

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="grid gap-1 text-sm">
      <span className="text-muted-foreground">{label}</span>
      {children}
    </label>
  );
}

function Combo({ id, value, options, onChange, disabled }: { id: string; value: string; options: string[]; onChange: (v: string) => void; disabled?: boolean }) {
  return (
    <>
      <Input id={id} list={`${id}-list`} value={value} disabled={disabled} onChange={(e) => onChange(e.target.value)} />
      <datalist id={`${id}-list`}>{options.map((o) => <option key={o} value={o} />)}</datalist>
    </>
  );
}

function DropDown({ value, options, onChange, placeholder }: { value: string; options: string[]; onChange: (v: string) => void; placeholder: string }) {
  const choices = value && !options.includes(value) ? [value, ...options] : options;
  return (
    <select
      className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
      value={value}
      onChange={(e) => onChange(e.target.value)}
    >
      <option value="">{placeholder}</option>
      {choices.map((option) => <option key={option} value={option}>{option}</option>)}
    </select>
  );
}

export function DomesticInvoiceScreen() {
  const { user } = useAuth();
  const [invoice, setInvoice] = useState<DomesticInvoice>(() => blankInvoice());
  const [editing, setEditing] = useState(false);
  const [busy, setBusy] = useState(false);
  const [loadNo, setLoadNo] = useState("");
  const [packingOpen, setPackingOpen] = useState(false);
  const [packingRows, setPackingRows] = useState<{ no: string; party: string; date: string; net: number; checked: boolean }[]>([]);
  const [consignees, setConsignees] = useState<{ code: string; name: string; city: string; address: string }[]>([]);

  const asOn = invoice.invDate.slice(0, 10);
  const mine = useQuery({
    queryKey: ["domestic-invoice-user-company", user?.username],
    queryFn: () => getUserCompany(user?.username ?? ""),
    enabled: Boolean(user?.username),
  });
  const lockedCompany = mine.data?.companyName
    || (mine.data && /plastene/i.test(mine.data.message) ? "Plastene India Limited (Unit -II)" : "");
  const lookups = useQuery({
    queryKey: ["domestic-invoice-lookups", invoice.companyName, asOn],
    queryFn: () => getLookups(invoice.companyName, asOn),
  });
  const numbers = useQuery({
    queryKey: ["domestic-invoice-nos", invoice.companyName, invoice.suffix, invoice.merchantExport],
    queryFn: () => getInvoices(invoice.companyName, invoice.suffix, invoice.merchantExport),
    enabled: Boolean(invoice.companyName && invoice.suffix),
  });

  useEffect(() => {
    if (!lockedCompany) return;
    setInvoice((prev) => (prev.companyName === lockedCompany ? prev : { ...prev, companyName: lockedCompany, packingListNo: "" }));
  }, [lockedCompany]);

  useEffect(() => {
    if (mine.data && !lockedCompany && mine.data.message) toast.error(mine.data.message);
  }, [mine.data, lockedCompany]);

  useEffect(() => {
    if (lookups.error instanceof Error) toast.error(lookups.error.message);
  }, [lookups.error]);

  useEffect(() => {
    const data = lookups.data;
    if (!data) return;
    setInvoice((prev) => ({
      ...prev,
      suffix: prev.suffix || data.years[0] || "",
      currency: prev.currency || data.currencies[0] || "",
      transportName: prev.transportName || data.transporters[0] || "",
      exDuty: prev.exDuty || data.exciseDuty,
      eduCess: prev.eduCess || data.eduCess,
      sheCess: prev.sheCess || data.sheCess,
      oelCvd: prev.oelCvd || data.exciseDuty,
      oelCess1: prev.oelCess1 || data.eduCess,
      oelShe1: prev.oelShe1 || data.sheCess,
    }));
  }, [lookups.data]);

  const priced = useMemo(() => calculateInvoice(invoice), [invoice]);
  const gstEra = new Date(invoice.invDate) > new Date("2017-06-30");
  const showPurchaseReturn = new Date(invoice.invDate) < new Date("2019-01-01");
  const oel = invoice.companyName === OEL_COMPANY;
  const L = lookups.data;

  function patch(partial: Partial<DomesticInvoice>) {
    setInvoice((prev) => ({ ...prev, ...partial }));
  }
  function patchLine(index: number, partial: Partial<InvoiceLine>) {
    setInvoice((prev) => ({
      ...prev,
      lines: prev.lines.map((line, i) => (i === index ? { ...line, ...partial } : line)),
    }));
  }

  async function onCommodity(index: number, name: string) {
    patchLine(index, { commodity: name });
    if (!name || !invoice.companyName) return;
    try {
      const tax = await getCommodityTax(invoice.companyName, name, asOn);
      const sameState = invoice.state.trim().toUpperCase() === (L?.factoryState ?? "").trim().toUpperCase();
      patchLine(index, {
        commodity: name,
        hsn: tax.tariffHeading,
        deptt: L?.commodities.find((c) => c.name === name)?.deptt ?? "",
        cgstRate: invoice.merchantExport || !sameState ? 0 : tax.cgst,
        sgstRate: invoice.merchantExport || !sameState ? 0 : tax.sgst,
        igstRate: invoice.merchantExport || sameState ? 0 : tax.igst,
      });
      if (!invoice.tariffHeading) patch({ tariffHeading: tax.tariffHeading });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load GST rates");
    }
  }

  async function applyInvoiceType(state: string, buyer: string, date: string) {
    if (!state) {
      patch({ typeOfInvoice: "" });
      return;
    }
    const preGst = new Date(date) < new Date("2017-07-01");
    if (!preGst) {
      patch({ typeOfInvoice: "Tax Invoice" });
      return;
    }
    if (state.trim().toUpperCase() !== "GUJARAT") {
      patch({ typeOfInvoice: "Retail Invoice" });
      return;
    }
    if (!buyer) {
      patch({ typeOfInvoice: "Retail Invoice" });
      return;
    }
    try {
      const tax = await getBuyerTax(buyer);
      const registered = tax.isAfTin || tax.vatNo.length >= 11 || tax.gstNo.length >= 11 || tax.newGstNo.length > 12;
      patch({ typeOfInvoice: registered ? "Tax Invoice" : "Retail Invoice" });
    } catch {
      patch({ typeOfInvoice: "Retail Invoice" });
    }
  }

  async function onBuyer(name: string) {
    patch({ buyerName: name });
    if (!name) {
      setConsignees([]);
      return;
    }
    try { setConsignees(await getConsignees(name)); } catch { setConsignees([]); }
    if (invoice.state) void applyInvoiceType(invoice.state, name, invoice.invDate);
  }

  async function loadExisting() {
    if (!loadNo) return;
    setBusy(true);
    try {
      const loaded = await getInvoice(Number(loadNo), invoice.companyName, invoice.suffix, invoice.merchantExport);
      if (loaded.marketing) toast.message("This is a marketing invoice. Update it against the order.");
      setInvoice({ ...blankInvoice(), ...loaded, lines: loaded.lines.length ? [...loaded.lines, blankLine()] : [blankLine()] });
      setEditing(true);
      if (loaded.buyerName) setConsignees(await getConsignees(loaded.buyerName));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Invoice not found");
    } finally {
      setBusy(false);
    }
  }

  async function openPacking() {
    if (!L?.mapPackingList) {
      toast.message("This company does not map packing lists.");
      return;
    }
    const rows = await getPackingLists(invoice.companyName);
    setPackingRows(rows.map((r) => ({
      no: r.packingListNo || r.invNo, party: r.partyName, date: r.despatchDate, net: r.netWt, checked: false,
    })));
    setPackingOpen(true);
  }

  async function acceptPacking() {
    const chosen = packingRows.filter((r) => r.checked);
    const net = priced.lines.reduce((sum, line) => sum + (line.commodity ? line.netWt : 0), 0);
    try {
      const result = await checkPacking(invoice.companyName, net, chosen.map((r) => r.no), priced.lines);
      patch({ packingListNo: result.packingListNo });
      setPackingOpen(false);
      toast.success("Packing list matched.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Packing list does not match");
    }
  }

  async function save() {
    const benefit = invoice.availBenefit ? "You have availed benefit 39-A." : "You have not availed benefit 39-A.";
    const ok = window.confirm(`Generate ${invoice.typeOfInvoice || "invoice"} for ${invoice.companyName}?\n${benefit}`);
    if (!ok) return;
    if (L?.mapPackingList && !invoice.packingListNo) {
      toast.error("Select a packing list before saving.");
      return;
    }
    setBusy(true);
    try {
      const body = { ...priced, lines: priced.lines.filter((l) => l.commodity) };
      const result = editing ? await updateInvoice(body, user?.username ?? "") : await saveInvoice(body, user?.username ?? "");
      toast.success(result.message + (result.gstInvNo ? ` GST no ${result.gstInvNo}` : ""));
      setInvoice(blankInvoice(invoice.companyName, invoice.suffix));
      setEditing(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Save failed");
    } finally {
      setBusy(false);
    }
  }

  function openPdf(blob: Blob) {
    const url = URL.createObjectURL(blob);
    window.open(url, "_blank", "noopener");
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  async function preview() {
    setBusy(true);
    try {
      const body = { ...priced, lines: priced.lines.filter((line) => line.commodity) };
      openPdf(await previewInvoicePdf(body));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not build the PDF");
    } finally {
      setBusy(false);
    }
  }

  async function recordPrint() {
    if (!invoice.invNo) {
      toast.error("Save or open an invoice before recording a print.");
      return;
    }
    setBusy(true);
    try {
      const doc = await getPrint(invoice.invNo, invoice.companyName, invoice.suffix, invoice.merchantExport, true, user?.username ?? "");
      openPdf(await previewInvoicePdf(doc.invoice.lines?.length ? doc.invoice : priced));
      toast.success(doc.alreadyPrinted ? "Extra copy recorded." : "Original print recorded. The invoice can no longer be edited.");
      if (!doc.alreadyPrinted) patch({ printed: true });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Print failed");
    } finally {
      setBusy(false);
    }
  }

  function initialize() {
    setEditing(false);
    setLoadNo("");
    setConsignees([]);
    setInvoice({
      ...blankInvoice(lockedCompany || invoice.companyName, L?.years[0] ?? invoice.suffix),
      currency: L?.currencies[0] ?? "",
      transportName: L?.transporters[0] ?? "",
      exDuty: L?.exciseDuty ?? 0,
      eduCess: L?.eduCess ?? 0,
      sheCess: L?.sheCess ?? 0,
      oelCvd: L?.exciseDuty ?? 0,
      oelCess1: L?.eduCess ?? 0,
      oelShe1: L?.sheCess ?? 0,
    });
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Domestic Invoice</h1>
          <p className="text-sm text-muted-foreground">
            Year {invoice.suffix || "—"} is taken from the latest financial year and stored with the invoice.
            {invoice.gstInvNo || invoice.invType ? ` Printed number ${invoice.gstInvNo || invoice.invType}.` : ""}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" disabled={busy} onClick={() => void preview()}><Printer className="mr-2 h-4 w-4" />Preview PDF</Button>
          <Button variant="outline" disabled={busy || !invoice.invNo} onClick={() => void recordPrint()}>Record print</Button>
        </div>
      </div>

      <div className="grid gap-3 rounded-xl border bg-card p-4 md:grid-cols-4">
        <Field label="Company"><Input readOnly value={invoice.companyName} placeholder={mine.isLoading ? "Loading your company" : "No company on this login"} /></Field>
        <Field label="Type of invoice"><Input readOnly value={invoice.typeOfInvoice} placeholder="Set when you choose the state" /></Field>
        <Field label="Buyer"><DropDown value={invoice.buyerName} options={L?.buyers ?? []} placeholder={lookups.isFetching ? "Loading buyers" : invoice.companyName ? "Select buyer" : "Company is required first"} onChange={(v) => void onBuyer(v)} /></Field>
        <Field label="Inv no">
          <div className="flex gap-2">
            <Input value={editing ? String(invoice.invNo) : loadNo} onChange={(e) => setLoadNo(e.target.value)} list="inv-nos" />
            <datalist id="inv-nos">{(numbers.data ?? []).map((n) => <option key={n} value={n} />)}</datalist>
            <Button type="button" variant="secondary" onClick={() => void loadExisting()}>Open</Button>
          </div>
        </Field>
        <Field label="Inv date"><Input type="date" value={invoice.invDate.slice(0, 10)} onChange={(e) => { patch({ invDate: e.target.value }); if (invoice.state) void applyInvoiceType(invoice.state, invoice.buyerName, e.target.value); }} /></Field>
        <Field label="Delivery order no"><Input value={invoice.delChallanNo} onChange={(e) => patch({ delChallanNo: e.target.value })} /></Field>
        <Field label="Order date"><Input type="date" value={invoice.delChallanDate.slice(0, 10)} onChange={(e) => patch({ delChallanDate: e.target.value })} /></Field>
        <Field label="LR no"><Input value={invoice.lrNo} onChange={(e) => patch({ lrNo: e.target.value })} /></Field>
        <Field label="LR date"><Input type="date" value={invoice.lrDate.slice(0, 10)} onChange={(e) => patch({ lrDate: e.target.value })} /></Field>
        <Field label="Despatch no"><Input readOnly value={invoice.despatchNo} /></Field>
        <Field label="Despatch date"><Input type="date" value={invoice.despatchDate.slice(0, 10)} onChange={(e) => patch({ despatchDate: e.target.value })} /></Field>
        <Field label="E-way no">
          <div className="flex gap-2">
            <Input value={invoice.ewayNo} onChange={(e) => patch({ ewayNo: e.target.value })} />
            <Button type="button" variant="secondary" disabled={!invoice.invNo} onClick={() => void updateEway(invoice.invNo, invoice.companyName, invoice.suffix, invoice.ewayNo).then(() => toast.success("E-way updated")).catch((e) => toast.error(e.message))}>Update</Button>
          </div>
        </Field>
        <Field label="Packing list">
          <div className="flex gap-2">
            <Input readOnly value={invoice.packingListNo} />
            <Button type="button" variant="secondary" onClick={() => void openPacking()}>Select</Button>
          </div>
        </Field>
        <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.directExport} onCheckedChange={(v) => patch({ directExport: v === true })} />Is direct export</label>
      </div>

      <div className="overflow-x-auto rounded-xl border">
        <table className="min-w-[1600px] w-full text-sm">
          <thead className="bg-muted/60 text-left">
            <tr>{["Pkg", "Main group", "Group", "Sub group", "Item", "Commodity", "Description", "Deptt", "UOM", "HSN", "Qty", "Rate", "Total", "CGST %", "CGST", "SGST %", "SGST", "IGST %", "IGST", "Net", "Gross", "Non qty", "Qty remarks", "Commodity remarks"].map((h) => <th key={h} className="px-2 py-2 font-medium">{h}</th>)}</tr>
          </thead>
          <tbody>
            {invoice.lines.map((line, index) => (
              <tr key={index} className="border-t">
                <td><Input value={line.noOfPkg} onChange={(e) => patchLine(index, { noOfPkg: e.target.value })} /></td>
                <td><Combo id={`mg${index}`} value={line.mainGroup} options={L?.mainGroups ?? []} onChange={(mainGroup) => patchLine(index, { mainGroup })} /></td>
                <td><Combo id={`g${index}`} value={line.groupName} options={(L?.groups ?? []).filter((g) => !line.mainGroup || g.deptt === line.mainGroup).map((g) => g.groupName)} onChange={(groupName) => patchLine(index, { groupName })} /></td>
                <td><Combo id={`sg${index}`} value={line.subGroup} options={(L?.subGroups ?? []).filter((g) => !line.groupName || g.groupName === line.groupName).map((g) => g.subGroupName)} onChange={(subGroup) => patchLine(index, { subGroup })} /></td>
                <td><Combo id={`it${index}`} value={L?.items.find((i) => i.code === line.itemCode)?.name ?? line.itemCode} options={(L?.items ?? []).map((i) => i.name)} onChange={(itemName) => {
                  const item = L?.items.find((i) => i.name === itemName);
                  setInvoice((prev) => {
                    const lines = prev.lines.map((row, i) => i === index ? { ...row, itemCode: item?.code ?? "", itemDesc: item?.name ?? row.itemDesc, mainGroup: item?.deptt ?? row.mainGroup, groupName: item?.groupName ?? row.groupName } : row);
                    return { ...prev, lines: index === prev.lines.length - 1 ? [...lines, blankLine()] : lines };
                  });
                }} /></td>
                <td><Combo id={`cm${index}`} value={line.commodity} options={(L?.commodities ?? []).map((c) => c.name)} onChange={(v) => void onCommodity(index, v)} /></td>
                <td><Input value={line.itemDesc} onChange={(e) => patchLine(index, { itemDesc: e.target.value })} /></td>
                <td><Combo id={`dp${index}`} value={line.deptt} options={L?.departments ?? []} onChange={(deptt) => patchLine(index, { deptt })} /></td>
                <td><Combo id={`u${index}`} value={line.uom} options={L?.units ?? []} onChange={(uom) => patchLine(index, uom.trim().toLowerCase() === "kgs" ? { uom, netWt: line.qty, grossWt: line.qty } : { uom })} /></td>
                <td><Input value={line.hsn} onChange={(e) => patchLine(index, { hsn: e.target.value })} /></td>
                <td><Input type="number" value={line.qty || ""} onChange={(e) => {
                  const qty = Number(e.target.value);
                  patchLine(index, line.uom.trim().toLowerCase() === "kgs" ? { qty, netWt: qty, grossWt: qty } : { qty });
                }} /></td>
                <td><Input type="number" value={line.rate || ""} onChange={(e) => patchLine(index, { rate: Number(e.target.value) })} /></td>
                <td className="px-2">{priced.lines[index]?.amount.toFixed(2)}</td>
                <td><Input type="number" value={line.cgstRate || ""} onChange={(e) => patchLine(index, { cgstRate: Number(e.target.value) })} /></td>
                <td className="px-2">{priced.lines[index]?.cgstAmt.toFixed(2)}</td>
                <td><Input type="number" value={line.sgstRate || ""} onChange={(e) => patchLine(index, { sgstRate: Number(e.target.value) })} /></td>
                <td className="px-2">{priced.lines[index]?.sgstAmt.toFixed(2)}</td>
                <td><Input type="number" value={line.igstRate || ""} onChange={(e) => patchLine(index, { igstRate: Number(e.target.value) })} /></td>
                <td className="px-2">{priced.lines[index]?.igstAmt.toFixed(2)}</td>
                <td><Input type="number" value={line.netWt || ""} onChange={(e) => patchLine(index, { netWt: Number(e.target.value) })} /></td>
                <td><Input type="number" value={line.grossWt || ""} onChange={(e) => patchLine(index, { grossWt: Number(e.target.value) })} /></td>
                <td><Input type="number" value={line.nonDisplayQty || ""} onChange={(e) => patchLine(index, { nonDisplayQty: Number(e.target.value) })} /></td>
                <td><Input value={line.qtyRemarks} onChange={(e) => patchLine(index, { qtyRemarks: e.target.value })} /></td>
                <td><Input value={line.commodityRemarks} onChange={(e) => patchLine(index, { commodityRemarks: e.target.value })} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Tabs defaultValue="transport">
        <TabsList className="h-auto flex-wrap">
          <TabsTrigger value="transport">Transport</TabsTrigger>
          <TabsTrigger value="sales">Sales order</TabsTrigger>
          <TabsTrigger value="tax">Taxation</TabsTrigger>
          <TabsTrigger value="remarks">Remarks</TabsTrigger>
          <TabsTrigger value="currency">Currency</TabsTrigger>
          <TabsTrigger value="consignee">Consignee</TabsTrigger>
          <TabsTrigger value="other">Other tax</TabsTrigger>
          <TabsTrigger value="merchant">Merchant export</TabsTrigger>
        </TabsList>
        <TabsContent value="transport" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          <Field label="Transporter"><Combo id="tr" value={invoice.transportName} options={L?.transporters ?? []} onChange={(transportName) => patch({ transportName })} /></Field>
          <Field label="Vehicle no"><Combo id="vh" value={invoice.vehicleNo} options={L?.vehicles ?? []} onChange={(vehicleNo) => patch({ vehicleNo })} /></Field>
          <Field label="Vehicle date"><Input type="date" value={invoice.vehicleDate.slice(0, 10)} onChange={(e) => patch({ vehicleDate: e.target.value })} /></Field>
          <Field label="Place of supply"><Combo id="dest" value={invoice.destination} options={L?.destinations ?? []} onChange={(destination) => patch({ destination })} /></Field>
          <div className="md:col-span-3">
            <Field label="Terms of delivery"><Textarea className="min-h-24" value={invoice.termsOfDelivery} onChange={(e) => patch({ termsOfDelivery: e.target.value })} /></Field>
          </div>
          <Field label="State"><Combo id="st" value={invoice.state} options={L?.states ?? []} onChange={(state) => { patch({ state }); void applyInvoiceType(state, invoice.buyerName, invoice.invDate); }} /></Field>
          <Field label="Mobile"><Input value={invoice.mobileNo} onChange={(e) => patch({ mobileNo: e.target.value })} /></Field>
          <Field label="Date and time of invoice"><Input type="datetime-local" value={invoice.invoiceDateTime.slice(0, 16)} onChange={(e) => patch({ invoiceDateTime: e.target.value })} /></Field>
          <Field label="Date and time of removal"><Input type="datetime-local" value={invoice.removalDateTime.slice(0, 16)} onChange={(e) => patch({ removalDateTime: e.target.value })} /></Field>
        </TabsContent>
        <TabsContent value="sales" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          <Field label="Buyer order no"><Input value={invoice.buyerOrderNo} onChange={(e) => patch({ buyerOrderNo: e.target.value })} /></Field>
          <Field label="Buyer order date"><Input type="date" value={invoice.buyerOrderDate.slice(0, 10)} onChange={(e) => patch({ buyerOrderDate: e.target.value })} /></Field>
          <Field label="Terms of payment"><Combo id="pay" value={invoice.termsOfPayment} options={L?.paymentTerms ?? []} onChange={(termsOfPayment) => patch({ termsOfPayment })} /></Field>
          <Field label="Department"><Combo id="dept" value={invoice.department} options={L?.departments ?? []} onChange={(department) => patch({ department })} /></Field>
          <Field label="Account head"><Combo id="ah" value={invoice.accountHead} options={L?.accountHeads ?? []} onChange={async (accountHead) => {
            patch({ accountHead });
            const commodity = invoice.lines.find((l) => l.commodity)?.commodity ?? "";
            if (commodity) {
              const tcs = await getTcs(invoice.companyName, accountHead, commodity, asOn);
              patch({ accountHead, tcsPercent: tcs.rate });
            }
          }} /></Field>
          <Field label="T. heading"><Input readOnly value={invoice.tariffHeading} /></Field>
          <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.insurance} onCheckedChange={async (v) => {
            const insurance = v === true;
            let insurancePolicyNo = invoice.insurancePolicyNo;
            if (insurance) insurancePolicyNo = (await getInsurance(invoice.companyName)).policyNo ?? "";
            patch({ insurance, insurancePolicyNo });
          }} />Insurance</label>
          {invoice.insurance && <Field label="Policy no"><Input value={invoice.insurancePolicyNo} onChange={(e) => patch({ insurancePolicyNo: e.target.value })} /></Field>}
          {showPurchaseReturn && <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.purchaseReturn} onCheckedChange={(v) => patch({ purchaseReturn: v === true })} />Purchase return</label>}
          {invoice.directExport && <>
            <Field label="Advance lic no"><Input value={invoice.advanceLicNo} onChange={(e) => patch({ advanceLicNo: e.target.value })} /></Field>
            <Field label="LUT bond no"><Combo id="ut" value={invoice.utBondNo} options={L?.utBonds ?? []} onChange={(utBondNo) => patch({ utBondNo })} /></Field>
            <Field label="ARE1 no"><Input value={invoice.are1No} onChange={(e) => patch({ are1No: e.target.value })} /></Field>
          </>}
        </TabsContent>
        <TabsContent value="tax" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          <Field label="Ex. duty %"><Input type="number" value={invoice.exDuty} onChange={(e) => patch({ exDuty: Number(e.target.value) })} /></Field>
          <Field label="Edu cess %"><Input type="number" value={invoice.eduCess} onChange={(e) => patch({ eduCess: Number(e.target.value) })} /></Field>
          <Field label="Sec. hi. edu. cess %"><Input type="number" value={invoice.sheCess} onChange={(e) => patch({ sheCess: Number(e.target.value) })} /></Field>
          {!gstEra && <Field label="Type"><Combo id="taxtype" value={invoice.taxType} options={["VAT", "CST"]} onChange={(taxType) => patch({ taxType, addVat: taxType === "CST" ? 0 : invoice.addVat })} /></Field>}
          <Field label={invoice.taxType === "CST" ? "CST %" : "VAT %"}><Input type="number" value={invoice.vat} onChange={(e) => patch({ vat: Number(e.target.value) })} /></Field>
          {!gstEra && invoice.taxType !== "CST" && <Field label="Add VAT %"><Input type="number" value={invoice.addVat} onChange={(e) => patch({ addVat: Number(e.target.value) })} /></Field>}
          <Field label="Excise"><Combo id="exapp" value={invoice.exciseApplicable} options={["NIL", "AMOUNT"]} onChange={(exciseApplicable) => patch({ exciseApplicable })} /></Field>
          <Field label="VAT"><Combo id="vatapp" value={invoice.vatApplicable} options={["NIL", "AMOUNT"]} onChange={(vatApplicable) => patch({ vatApplicable })} /></Field>
          {invoice.exciseApplicable === "NIL" && <Field label="Excise nil remarks"><Input value={invoice.exciseNilRemarks} onChange={(e) => patch({ exciseNilRemarks: e.target.value })} /></Field>}
          {invoice.vatApplicable === "NIL" && <Field label="VAT nil remarks"><Input value={invoice.vatNilRemarks} onChange={(e) => patch({ vatNilRemarks: e.target.value })} /></Field>}
          <Field label="Freight"><Input type="number" value={invoice.freight} onChange={(e) => patch({ freight: Number(e.target.value) })} /></Field>
          <Field label="TCS %"><Input type="number" value={invoice.tcsPercent} onChange={(e) => patch({ tcsPercent: Number(e.target.value) })} /></Field>
          <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.gstAfterFreight} onCheckedChange={(v) => patch({ gstAfterFreight: v === true })} />GST calculation after freight</label>
        </TabsContent>
        <TabsContent value="remarks" className="grid gap-3 rounded-xl border p-4">
          <Field label="Remarks"><Input value={invoice.remarks} onChange={(e) => patch({ remarks: e.target.value })} /></Field>
          <Field label="Freight remarks"><Input value={invoice.freightRemarks} onChange={(e) => patch({ freightRemarks: e.target.value })} /></Field>
          <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.igstNilNotification} onCheckedChange={(v) => patch({ igstNilNotification: v === true })} />IGST nil against notification 33/2015-2020 dated 13.10.2017</label>
        </TabsContent>
        <TabsContent value="currency" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          <Field label="Currency"><Combo id="cur" value={invoice.currency} options={L?.currencies ?? []} onChange={async (currency) => {
            const value = await getCurrencyValue(currency);
            patch({ currency, currencyValue: String(value.value) });
          }} /></Field>
          <Field label="Currency value"><Input value={invoice.currencyValue} onChange={(e) => patch({ currencyValue: e.target.value })} /></Field>
          <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.availBenefit} onCheckedChange={(v) => patch({ availBenefit: v === true })} />Avail benefit 39-A</label>
          {invoice.availBenefit && <Field label="UT bond details"><Input value={invoice.utBondDetails} onChange={(e) => patch({ utBondDetails: e.target.value })} /></Field>}
        </TabsContent>
        <TabsContent value="consignee" className="grid gap-3 rounded-xl border p-4 md:grid-cols-2">
          <Field label="Consignee"><Combo id="cn" value={invoice.consigneeName} options={consignees.map((c) => c.name)} onChange={(consigneeName) => {
            const row = consignees.find((c) => c.name === consigneeName);
            patch({ consigneeName, consigneeCode: row?.code ?? "", consigneeCity: row?.city ?? invoice.consigneeCity, consigneeAddress: row?.address ?? invoice.consigneeAddress });
          }} /></Field>
          <Field label="City"><Combo id="cc" value={invoice.consigneeCity} options={[...new Set(consignees.map((c) => c.city))]} onChange={(consigneeCity) => patch({ consigneeCity })} /></Field>
          <Field label="Address if other than buyer"><Input value={invoice.consigneeAddress} onChange={(e) => patch({ consigneeAddress: e.target.value })} /></Field>
          <Field label="Notifier"><Input value={invoice.notifier} onChange={(e) => patch({ notifier: e.target.value })} /></Field>
          <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice.dispatchFrom} onCheckedChange={(v) => patch({ dispatchFrom: v === true })} />Is dispatch from</label>
          {invoice.dispatchFrom && <>
            <Field label="Dispatch from"><Combo id="df" value={invoice.dispatchFromName} options={L?.vendors ?? []} onChange={async (dispatchFromName) => {
              const address = await getVendorAddress(dispatchFromName);
              patch({ dispatchFromName, dispatchFromAddress: address.address });
            }} /></Field>
            <Field label="Address"><Input value={invoice.dispatchFromAddress} onChange={(e) => patch({ dispatchFromAddress: e.target.value })} /></Field>
          </>}
        </TabsContent>
        <TabsContent value="other" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          {([1, 2, 3] as const).map((n) => (
            <div key={n} className="space-y-2 rounded-lg border p-3">
              <Field label={`Special duty remarks ${n}`}><Input value={invoice[`spDutyRemarks${n}`]} onChange={(e) => patch({ [`spDutyRemarks${n}`]: e.target.value })} /></Field>
              <Field label={`Special duty amount ${n}`}><Input type="number" value={invoice[`spDutyAmt${n}`]} onChange={(e) => patch({ [`spDutyAmt${n}`]: Number(e.target.value) })} /></Field>
              <label className="flex items-center gap-2 text-sm"><Checkbox checked={invoice[`gstOnSpDuty${n}`]} onCheckedChange={(v) => patch({ [`gstOnSpDuty${n}`]: v === true })} />GST on special duty {n}</label>
            </div>
          ))}
        </TabsContent>
        <TabsContent value="merchant" className="grid gap-3 rounded-xl border p-4 md:grid-cols-3">
          <label className="flex items-center gap-2 text-sm md:col-span-3"><Checkbox checked={invoice.merchantExport} onCheckedChange={(v) => patch({ merchantExport: v === true, notification: v === true ? "Supply to Merchant Exporter" : "" })} />Sales to merchant exporter</label>
          <Field label="Notification"><Input value={invoice.notification} onChange={(e) => patch({ notification: e.target.value })} /></Field>
          <Field label="Advance license no"><Input value={invoice.advanceLicense} onChange={(e) => patch({ advanceLicense: e.target.value })} /></Field>
          {oel && <>
            <Field label="Basic %"><Input type="number" value={invoice.oelBasic} onChange={(e) => patch({ oelBasic: Number(e.target.value) })} /></Field>
            <Field label="CVD %"><Input type="number" value={invoice.oelCvd} onChange={(e) => patch({ oelCvd: Number(e.target.value) })} /></Field>
            <Field label="Edu cess 1 / SWS %"><Input type="number" value={invoice.oelCess1} onChange={(e) => patch({ oelCess1: Number(e.target.value) })} /></Field>
            <Field label="SHE cess 1 %"><Input type="number" value={invoice.oelShe1} onChange={(e) => patch({ oelShe1: Number(e.target.value) })} /></Field>
            <Field label="Edu cess 2 %"><Input type="number" value={invoice.oelCess2} onChange={(e) => patch({ oelCess2: Number(e.target.value) })} /></Field>
            <Field label="SHE cess 2 %"><Input type="number" value={invoice.oelShe2} onChange={(e) => patch({ oelShe2: Number(e.target.value) })} /></Field>
            <Field label="Edu cess 3 %"><Input type="number" value={invoice.oelCess3} onChange={(e) => patch({ oelCess3: Number(e.target.value) })} /></Field>
            <Field label="SHE cess 3 %"><Input type="number" value={invoice.oelShe3} onChange={(e) => patch({ oelShe3: Number(e.target.value) })} /></Field>
            <Field label="Add duty %"><Input type="number" value={invoice.oelAddDuty} onChange={(e) => patch({ oelAddDuty: Number(e.target.value) })} /></Field>
          </>}
        </TabsContent>
      </Tabs>

      <div className="flex flex-wrap items-center gap-2">
        <Button variant="outline" disabled={lookups.isFetching} onClick={() => void lookups.refetch()}><RefreshCw className="mr-2 h-4 w-4" />Refresh</Button>
        <Button variant="outline" onClick={initialize}><RotateCcw className="mr-2 h-4 w-4" />Initialize</Button>
        <Button disabled={busy || editing || invoice.printed} onClick={() => void save()}>
          {busy && !editing ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}Save
        </Button>
        <Button disabled={busy || !editing || invoice.printed} onClick={() => void save()}>
          {busy && editing ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}Update
        </Button>
        <Button variant="secondary" onClick={() => toast.message(`Grand total ${priced.grandTotal.toFixed(2)}`)}><Calculator className="mr-2 h-4 w-4" />Calculation</Button>
        <span className="ml-auto text-sm font-medium">Grand total {priced.grandTotal.toFixed(2)}</span>
      </div>

      <Dialog open={packingOpen} onOpenChange={setPackingOpen}>
        <DialogContent className="max-w-3xl">
          <DialogHeader><DialogTitle>Pending packing lists</DialogTitle></DialogHeader>
          <div className="max-h-96 overflow-auto">
            <table className="w-full text-sm">
              <thead><tr><th /><th className="text-left">Packing list</th><th className="text-left">Party</th><th className="text-left">Date</th><th className="text-right">Net wt</th></tr></thead>
              <tbody>
                {packingRows.map((row, i) => (
                  <tr key={row.no + i} className="border-t">
                    <td><Checkbox checked={row.checked} onCheckedChange={(v) => setPackingRows((rows) => rows.map((r, j) => j === i ? { ...r, checked: v === true } : r))} /></td>
                    <td>{row.no}</td><td>{row.party}</td><td>{row.date.slice(0, 10)}</td><td className="text-right">{row.net}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Button onClick={() => void acceptPacking()}>Use selected</Button>
        </DialogContent>
      </Dialog>

    </div>
  );
}
