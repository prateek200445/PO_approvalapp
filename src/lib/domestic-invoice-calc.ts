import type { DomesticInvoice, InvoiceLine } from "@/lib/domestic-invoice-api";

export const QTY_DIGITS = 3;
export const RATE_DIGITS = 2;
export const AMOUNT_DIGITS = 2;
export const OEL_COMPANY = "Oswal Extrusion Limited (Unit-II)";
export const FIBCO_COMPANY = "Fibco Plastic Industry LLC";

export function roundAway(value: number, digits: number): number {
  const f = 10 ** digits;
  return Math.sign(value) * Math.round((Math.abs(value) * f) + Number.EPSILON) / f;
}

export function roundEven(value: number, digits: number): number {
  const f = 10 ** digits;
  const x = value * f;
  const floor = Math.floor(x);
  const diff = x - floor;
  if (Math.abs(diff - 0.5) < 1e-8) return (floor % 2 === 0 ? floor : floor + 1) / f;
  return Math.round(x) / f;
}

function addDutyGst(duty: number, cgst: number, sgst: number, igst: number, rate: number) {
  const extra = roundAway(duty * rate / 100, AMOUNT_DIGITS);
  return {
    cgst: cgst > 0 ? cgst + extra : cgst,
    sgst: sgst > 0 ? sgst + extra : sgst,
    igst: igst > 0 ? igst + extra : igst,
  };
}

/** Mirrors the desktop Calculation / CalculationGST paths. Fibco invoices stay on VAT after GST began. */
export function calculateInvoice(invoice: DomesticInvoice): DomesticInvoice {
  if (invoice.companyName === FIBCO_COMPANY) return calculateFibco(invoice);
  const gst = new Date(invoice.invDate) > new Date("2017-06-30");
  return gst ? calculateGst(invoice) : calculateLegacy(invoice);
}

function calculateFibco(invoice: DomesticInvoice): DomesticInvoice {
  const lines = invoice.lines.map((line) => ({
    ...line, cgstRate: 0, cgstAmt: 0, sgstRate: 0, sgstAmt: 0, igstRate: 0, igstAmt: 0,
  }));
  let goods = 0;
  for (const line of lines) {
    if (!line.commodity || !line.qty || !line.rate) continue;
    line.qty = roundAway(line.qty, QTY_DIGITS);
    line.rate = roundAway(line.rate, RATE_DIGITS);
    line.amount = roundAway(line.qty * line.rate, AMOUNT_DIGITS);
    goods = roundAway(goods + line.amount, AMOUNT_DIGITS);
  }
  const vatAmt = roundEven(goods * invoice.vat / 100, 0);
  const addVatAmt = roundEven(goods * invoice.addVat / 100, 0);
  let total = goods + vatAmt + addVatAmt + invoice.freight;
  total += roundAway(total * invoice.tcsPercent / 100, AMOUNT_DIGITS);
  return { ...invoice, lines, vatAmt, addVatAmt, grandTotal: roundAway(total, AMOUNT_DIGITS) };
}

function calculateGst(invoice: DomesticInvoice): DomesticInvoice {
  const oel = invoice.companyName === OEL_COMPANY;
  const lines = invoice.lines.map((line) => ({ ...line }));
  let goods = 0;
  let taxable = 0;
  let cgst = 0;
  let sgst = 0;
  let igst = 0;
  let maxRate = 0;

  for (const line of lines) {
    if (!line.commodity || !line.qty || !line.rate) continue;
    if (oel && (!line.deptt || !line.uom)) continue;
    line.qty = roundAway(line.qty, QTY_DIGITS);
    line.rate = roundAway(line.rate, RATE_DIGITS);
    const baseAmt = roundAway(line.qty * line.rate, AMOUNT_DIGITS);
    goods = roundAway(goods + baseAmt, AMOUNT_DIGITS);
    let lineTotal = baseAmt;
    if (oel) {
      const basic = roundEven(baseAmt * invoice.oelBasic / 100, 0);
      const cvd = roundEven(basic * invoice.oelCvd / 100, 0);
      const e1 = roundEven((cvd + basic) * invoice.oelCess1 / 100, 0);
      const s1 = roundEven((cvd + basic) * invoice.oelShe1 / 100, 0);
      let sum1 = basic + cvd + e1 + s1;
      sum1 += roundEven((goods + sum1) * invoice.oelAddDuty / 100, 0);
      const e2 = roundEven(sum1 * invoice.oelCess2 / 100, 0);
      const s2 = roundEven(sum1 * invoice.oelShe2 / 100, 0);
      const sum2 = sum1 + e2 + s2;
      lineTotal = baseAmt + sum2 + roundEven(sum2 * invoice.oelCess3 / 100, 0) + roundEven(sum2 * invoice.oelShe3 / 100, 0);
    }
    line.amount = lineTotal;
    taxable = roundAway(taxable + lineTotal, AMOUNT_DIGITS);
    line.cgstAmt = roundAway(lineTotal * line.cgstRate / 100, AMOUNT_DIGITS);
    line.sgstAmt = roundAway(lineTotal * line.sgstRate / 100, AMOUNT_DIGITS);
    line.igstAmt = roundAway(lineTotal * line.igstRate / 100, AMOUNT_DIGITS);
    cgst += line.cgstAmt;
    sgst += line.sgstAmt;
    igst += line.igstAmt;
    maxRate = Math.max(maxRate, line.cgstRate, line.sgstRate, line.igstRate);
  }

  let total: number;
  if (oel) {
    if (invoice.gstOnSpDuty1) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt1, cgst, sgst, igst, maxRate));
    if (invoice.gstOnSpDuty2) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt2, cgst, sgst, igst, maxRate));
    if (invoice.gstOnSpDuty3) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt3, cgst, sgst, igst, maxRate));
    total = roundAway(taxable + cgst + sgst + igst, AMOUNT_DIGITS);
    if (invoice.gstAfterFreight) total += invoice.freight;
    total += invoice.spDutyAmt1 + invoice.spDutyAmt2 + invoice.spDutyAmt3;
    total += roundEven(total * invoice.tcsPercent / 100, 0);
    if (!invoice.gstAfterFreight) total += invoice.freight;
  } else {
    total = goods + invoice.freight;
    if (invoice.gstAfterFreight) ({ cgst, sgst, igst } = addDutyGst(invoice.freight, cgst, sgst, igst, maxRate));
    if (invoice.gstOnSpDuty1) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt1, cgst, sgst, igst, maxRate));
    if (invoice.gstOnSpDuty2) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt2, cgst, sgst, igst, maxRate));
    if (invoice.gstOnSpDuty3) ({ cgst, sgst, igst } = addDutyGst(invoice.spDutyAmt3, cgst, sgst, igst, maxRate));
    total = roundAway(total + cgst + sgst + igst, AMOUNT_DIGITS);
    total += invoice.spDutyAmt1 + invoice.spDutyAmt2 + invoice.spDutyAmt3;
    total += roundAway(total * invoice.tcsPercent / 100, AMOUNT_DIGITS);
  }
  return { ...invoice, lines, grandTotal: roundAway(total, AMOUNT_DIGITS) };
}

function calculateLegacy(invoice: DomesticInvoice): DomesticInvoice {
  const oel = invoice.companyName === OEL_COMPANY;
  const lines = invoice.lines.map((line) => ({ ...line }));
  let goods = 0;
  for (const line of lines) {
    if (!line.commodity || !line.qty || !line.rate) continue;
    line.qty = roundAway(line.qty, QTY_DIGITS);
    line.rate = roundAway(line.rate, RATE_DIGITS);
    line.amount = roundAway(line.qty * line.rate, AMOUNT_DIGITS);
    goods = roundAway(goods + line.amount, AMOUNT_DIGITS);
  }
  goods = roundEven(goods, 0);
  let total = goods;
  if (oel) {
    const basic = roundEven(goods * invoice.oelBasic / 100, 0);
    const cvd = roundEven((goods + basic) * invoice.oelCvd / 100, 0);
    const e1 = roundEven((cvd + basic) * invoice.oelCess1 / 100, 0);
    const s1 = roundEven((cvd + basic) * invoice.oelShe1 / 100, 0);
    let sum1 = basic + cvd + e1 + s1;
    sum1 += roundEven((goods + sum1) * invoice.oelAddDuty / 100, 0);
    const e2 = roundEven(sum1 * invoice.oelCess2 / 100, 0);
    const s2 = roundEven(sum1 * invoice.oelShe2 / 100, 0);
    const sum2 = sum1 + e2 + s2;
    total = goods + sum2 + roundEven(sum2 * invoice.oelCess3 / 100, 0) + roundEven(sum2 * invoice.oelShe3 / 100, 0);
    if (invoice.gstAfterFreight) total += invoice.freight;
    total += roundEven(total * invoice.vat / 100, 0) + roundEven(total * invoice.addVat / 100, 0);
    if (!invoice.gstAfterFreight) total += invoice.freight;
    total += roundAway(total * invoice.tcsPercent / 100, AMOUNT_DIGITS) + invoice.spDutyAmt1 + invoice.spDutyAmt2 + invoice.spDutyAmt3;
  } else {
    const excise = roundEven(goods * invoice.exDuty / 100, 0);
    const edu = roundEven(invoice.eduCess * excise / 100, 0);
    const she = roundEven(invoice.sheCess * excise / 100, 0);
    if (invoice.gstAfterFreight) total += invoice.freight;
    const vatBase = invoice.exciseApplicable === "NIL" ? total : total + excise + edu + she;
    const vat = roundEven(vatBase * invoice.vat / 100, 0);
    const addVat = roundEven(vatBase * invoice.addVat / 100, 0);
    const exciseOn = invoice.exciseApplicable !== "NIL";
    const vatOn = invoice.vatApplicable !== "NIL";
    if (!exciseOn && vatOn) total += vat + addVat;
    else if (exciseOn && !vatOn) total += edu + she + excise;
    else if (exciseOn && vatOn) total += edu + she + vat + addVat + excise;
    if (!invoice.gstAfterFreight) total += invoice.freight;
    total += roundAway(total * invoice.tcsPercent / 100, AMOUNT_DIGITS);
  }
  return { ...invoice, lines, grandTotal: roundAway(total, AMOUNT_DIGITS) };
}

export function blankLine(): InvoiceLine {
  return {
    noOfPkg: "", mainGroup: "", groupName: "", subGroup: "", itemCode: "", commodity: "", itemDesc: "",
    deptt: "", uom: "", hsn: "", qty: 0, rate: 0, amount: 0, cgstRate: 0, cgstAmt: 0, sgstRate: 0, sgstAmt: 0,
    igstRate: 0, igstAmt: 0, netWt: 0, grossWt: 0, nonDisplayQty: 0, nonDisplayUnit: "KGS", qtyRemarks: "", commodityRemarks: "",
  };
}

export function todayStamp(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
}
