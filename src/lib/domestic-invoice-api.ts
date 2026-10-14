import { getApiUrl } from "@/lib/api-config";
import { blankLine, todayStamp } from "@/lib/domestic-invoice-calc";

export interface InvoiceLine {
  noOfPkg: string;
  mainGroup: string;
  groupName: string;
  subGroup: string;
  itemCode: string;
  commodity: string;
  itemDesc: string;
  deptt: string;
  uom: string;
  hsn: string;
  qty: number;
  rate: number;
  amount: number;
  cgstRate: number;
  cgstAmt: number;
  sgstRate: number;
  sgstAmt: number;
  igstRate: number;
  igstAmt: number;
  netWt: number;
  grossWt: number;
  nonDisplayQty: number;
  nonDisplayUnit: string;
  qtyRemarks: string;
  commodityRemarks: string;
}

export interface DomesticInvoice extends Record<string, unknown> {
  invNo: number;
  invType: string;
  gstInvNo: string;
  companyName: string;
  suffix: string;
  typeOfInvoice: string;
  buyerName: string;
  invDate: string;
  delChallanNo: string;
  delChallanDate: string;
  lrNo: string;
  lrDate: string;
  despatchNo: string;
  despatchDate: string;
  transportName: string;
  vehicleNo: string;
  vehicleDate: string;
  destination: string;
  termsOfDelivery: string;
  state: string;
  mobileNo: string;
  invoiceDateTime: string;
  removalDateTime: string;
  buyerOrderNo: string;
  buyerOrderDate: string;
  termsOfPayment: string;
  department: string;
  accountHead: string;
  tariffHeading: string;
  insurance: boolean;
  insurancePolicyNo: string;
  purchaseReturn: boolean;
  advanceLicNo: string;
  utBondNo: string;
  are1No: string;
  directExport: boolean;
  exDuty: number;
  eduCess: number;
  sheCess: number;
  taxType: string;
  vat: number;
  addVat: number;
  exciseApplicable: string;
  vatApplicable: string;
  exciseNilRemarks: string;
  vatNilRemarks: string;
  freight: number;
  tcsPercent: number;
  gstAfterFreight: boolean;
  remarks: string;
  freightRemarks: string;
  rg23: string;
  igstNilNotification: boolean;
  currency: string;
  currencyValue: string;
  availBenefit: boolean;
  utBondDetails: string;
  consigneeName: string;
  consigneeCity: string;
  consigneeAddress: string;
  consigneeCode: string;
  notifier: string;
  dispatchFrom: boolean;
  dispatchFromName: string;
  dispatchFromAddress: string;
  spDutyRemarks1: string;
  spDutyAmt1: number;
  spDutyRemarks2: string;
  spDutyAmt2: number;
  spDutyRemarks3: string;
  spDutyAmt3: number;
  gstOnSpDuty1: boolean;
  gstOnSpDuty2: boolean;
  gstOnSpDuty3: boolean;
  merchantExport: boolean;
  notification: string;
  advanceLicense: string;
  oelBasic: number;
  oelCvd: number;
  oelCess1: number;
  oelShe1: number;
  oelCess2: number;
  oelShe2: number;
  oelCess3: number;
  oelShe3: number;
  oelAddDuty: number;
  ewayNo: string;
  packingListNo: string;
  grandTotal: number;
  printed: boolean;
  marketing: boolean;
  header: string;
  lines: InvoiceLine[];
}

export interface DomesticLookups {
  companies: string[];
  years: string[];
  departments: string[];
  transporters: string[];
  currencies: string[];
  states: string[];
  paymentTerms: string[];
  buyers: string[];
  vehicles: string[];
  destinations: string[];
  accountHeads: string[];
  units: string[];
  mainGroups: string[];
  groups: { deptt: string; groupName: string }[];
  subGroups: { deptt: string; groupName: string; subGroupName: string }[];
  commodities: { name: string; deptt: string; tariffHeading: string }[];
  items: { code: string; name: string; deptt: string; groupName: string }[];
  utBonds: string[];
  vendors: string[];
  factoryState: string;
  companyGstNo: string;
  dispatchCode: string;
  mapPackingList: boolean;
  exciseDuty: number;
  eduCess: number;
  sheCess: number;
}

export interface PackingRow {
  packingListNo: string;
  invNo: string;
  partyName: string;
  despatchDate: string;
  netWt: number;
}

export interface PrintDocument {
  copyTitle: string;
  copies: string[];
  alreadyPrinted: boolean;
  ewayRequired: boolean;
  ewayGenerated: boolean;
  reportName: string;
  invoice: DomesticInvoice;
  rows: Record<string, string | null>[];
}

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(getApiUrl(path), {
    ...init,
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  });
  const text = await response.text();
  const payload = text ? JSON.parse(text) as T & { message?: string } : ({} as T & { message?: string });
  if (!response.ok) throw new Error(payload.message || "Domestic invoice request failed");
  return payload;
}

export function blankInvoice(company = "", suffix = ""): DomesticInvoice {
  const now = todayStamp();
  return {
    invNo: 0, invType: "", gstInvNo: "", companyName: company, suffix, typeOfInvoice: "",
    buyerName: "", invDate: now.slice(0, 10), delChallanNo: "", delChallanDate: now.slice(0, 10),
    lrNo: "", lrDate: now.slice(0, 10), despatchNo: "", despatchDate: now.slice(0, 10),
    transportName: "", vehicleNo: "", vehicleDate: now.slice(0, 10), destination: "", termsOfDelivery: "FOR",
    state: "", mobileNo: "", invoiceDateTime: now, removalDateTime: now, buyerOrderNo: "", buyerOrderDate: now.slice(0, 10),
    termsOfPayment: "", department: "", accountHead: "", tariffHeading: "", insurance: false, insurancePolicyNo: "",
    purchaseReturn: false, advanceLicNo: "", utBondNo: "", are1No: "", directExport: false,
    exDuty: 0, eduCess: 0, sheCess: 0, taxType: "VAT", vat: 0, addVat: 0, exciseApplicable: "NIL", vatApplicable: "NIL",
    exciseNilRemarks: "", vatNilRemarks: "", freight: 0, tcsPercent: 0, gstAfterFreight: false, remarks: "", freightRemarks: "",
    rg23: "", igstNilNotification: false, currency: "", currencyValue: "", availBenefit: false,
    utBondDetails: "EXCISE : Goods cleared under notification no. 39/2001 dated: 31-7-2001",
    consigneeName: "", consigneeCity: "", consigneeAddress: "", consigneeCode: "", notifier: "",
    dispatchFrom: false, dispatchFromName: "", dispatchFromAddress: "",
    spDutyRemarks1: "", spDutyAmt1: 0, spDutyRemarks2: "", spDutyAmt2: 0, spDutyRemarks3: "", spDutyAmt3: 0,
    gstOnSpDuty1: false, gstOnSpDuty2: false, gstOnSpDuty3: false, merchantExport: false, notification: "", advanceLicense: "",
    oelBasic: 5, oelCvd: 0, oelCess1: 10, oelShe1: 0, oelCess2: 0, oelShe2: 0, oelCess3: 0, oelShe3: 0, oelAddDuty: 0,
    ewayNo: "", packingListNo: "", grandTotal: 0, printed: false, marketing: false, header: "", lines: [blankLine()],
  };
}

export const getLookups = (company: string, asOn: string) =>
  api<DomesticLookups>(`/api/domestic-invoice/lookups?company=${encodeURIComponent(company)}&asOn=${encodeURIComponent(asOn)}`);

export const getInvoices = (company: string, suffix: string, merchant: boolean) =>
  api<string[]>(`/api/domestic-invoice/invoices?company=${encodeURIComponent(company)}&suffix=${encodeURIComponent(suffix)}&merchant=${merchant}`);

export const getInvoice = (invNo: number, company: string, suffix: string, merchant: boolean) =>
  api<DomesticInvoice>(`/api/domestic-invoice/${invNo}?company=${encodeURIComponent(company)}&suffix=${encodeURIComponent(suffix)}&merchant=${merchant}`);

export const getConsignees = (buyer: string) =>
  api<{ code: string; name: string; city: string; address: string }[]>(`/api/domestic-invoice/consignees?buyer=${encodeURIComponent(buyer)}`);

export const getCommodityTax = (company: string, commodity: string, asOn: string) =>
  api<{ tariffHeading: string; cgst: number; sgst: number; igst: number }>(
    `/api/domestic-invoice/commodity-tax?company=${encodeURIComponent(company)}&commodity=${encodeURIComponent(commodity)}&asOn=${encodeURIComponent(asOn)}`,
  );

export const getVendorAddress = (firm: string) =>
  api<{ address: string }>(`/api/domestic-invoice/vendor-address?firm=${encodeURIComponent(firm)}`);

export const getInsurance = (company: string) =>
  api<{ policyNo: string }>(`/api/domestic-invoice/insurance?company=${encodeURIComponent(company)}`);

export const getCurrencyValue = (currency: string) =>
  api<{ value: number }>(`/api/domestic-invoice/currency-value?currency=${encodeURIComponent(currency)}`);

export const getUserCompany = (login: string) =>
  api<{ companyName: string; message: string }>(`/api/domestic-invoice/user-company?login=${encodeURIComponent(login)}`);

export const getBuyerTax = (buyer: string) =>
  api<{ vatNo: string; gstNo: string; isAfTin: boolean; newGstNo: string }>(
    `/api/domestic-invoice/buyer-tax?buyer=${encodeURIComponent(buyer)}`,
  );

export async function previewInvoicePdf(invoice: DomesticInvoice): Promise<Blob> {
  const response = await fetch(getApiUrl("/api/domestic-invoice/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(invoice),
  });
  if (!response.ok) {
    const text = await response.text();
    let message = "Could not build the invoice PDF";
    try {
      const payload = JSON.parse(text) as { message?: string };
      if (payload.message) message = payload.message;
    } catch {
      if (text) message = text;
    }
    throw new Error(message);
  }
  return response.blob();
}

export const getTcs = (company: string, ledger: string, commodity: string, asOn: string) =>
  api<{ rate: number }>(`/api/domestic-invoice/tcs?company=${encodeURIComponent(company)}&ledger=${encodeURIComponent(ledger)}&commodity=${encodeURIComponent(commodity)}&asOn=${encodeURIComponent(asOn)}`);

export const saveInvoice = (invoice: DomesticInvoice, login: string) =>
  api<{ invNo: number; invType: string; gstInvNo: string; message: string }>(`/api/domestic-invoice?login=${encodeURIComponent(login)}`, {
    method: "POST", body: JSON.stringify(invoice),
  });

export const updateInvoice = (invoice: DomesticInvoice, login: string) =>
  api<{ invNo: number; invType: string; gstInvNo: string; message: string }>(`/api/domestic-invoice/${invoice.invNo}?login=${encodeURIComponent(login)}`, {
    method: "PUT", body: JSON.stringify(invoice),
  });

export const getPackingLists = (company: string) =>
  api<PackingRow[]>(`/api/domestic-invoice/packing-lists?company=${encodeURIComponent(company)}`);

export const checkPacking = (company: string, invoiceNetWt: number, packingListNos: string[], lines: InvoiceLine[]) =>
  api<{ ok: boolean; packingListNo: string }>(`/api/domestic-invoice/packing-lists/check`, {
    method: "POST",
    body: JSON.stringify({ companyName: company, invoiceNetWt, packingListNos, lines }),
  });

export const updateEway = (invNo: number, company: string, suffix: string, ewayNo: string) =>
  api<{ message: string }>(`/api/domestic-invoice/${invNo}/eway?company=${encodeURIComponent(company)}&suffix=${encodeURIComponent(suffix)}`, {
    method: "POST", body: JSON.stringify({ ewayNo }),
  });

export const getPrint = (invNo: number, company: string, suffix: string, merchant: boolean, record: boolean, login: string) =>
  api<PrintDocument>(`/api/domestic-invoice/${invNo}/print?company=${encodeURIComponent(company)}&suffix=${encodeURIComponent(suffix)}&merchant=${merchant}&record=${record}&login=${encodeURIComponent(login)}`);
