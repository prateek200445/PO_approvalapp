import { getApiUrl } from "@/lib/api-config";

export interface BobLodgementCompany {
  id: string;
  title: string;
  shortLabel: string;
}

export interface BobLodgementField {
  key: string;
  label: string;
  value: string;
  multiline: boolean;
  sort: number;
}

export interface BobLodgementDraft {
  companyId: string;
  companyTitle: string;
  invoiceNo: string;
  shippingBillNo: string;
  sourceSummary: string;
  fields: BobLodgementField[];
}

export interface BobLodgementExcelRow {
  rowNumber: number;
  companyName: string;
  invoiceNo: string;
  invoiceDate: string;
  buyerName: string;
  buyerAddress: string;
  buyerCountry: string;
  currency: string;
  amount: number;
  amountWords: string;
  shippingBillNo: string;
  shippingBillDate: string;
  blNo: string;
  blDate: string;
  portOfLoading: string;
  portOfDestination: string;
  portCode: string;
  hsCode: string;
  goods: string;
  termsOfPayment: string;
  usanceDays: string;
}

export interface BobLodgementExcelParseResult {
  fileName: string;
  sheetName: string;
  headerRow: number;
  mappedColumns: Record<string, string>;
  rows: BobLodgementExcelRow[];
}

async function readError(response: Response, fallback: string) {
  try {
    const payload = (await response.json()) as { message?: string };
    if (payload.message) return payload.message;
  } catch {
    // keep fallback
  }
  return fallback;
}

export async function listBobLodgementCompanies(): Promise<BobLodgementCompany[]> {
  const response = await fetch(getApiUrl("/api/BobLodgement/companies"));
  if (!response.ok) throw new Error(await readError(response, "Failed to load companies"));
  return response.json();
}

export async function parseBobLodgementExcel(file: File): Promise<BobLodgementExcelParseResult> {
  const body = new FormData();
  body.append("file", file);
  const response = await fetch(getApiUrl("/api/BobLodgement/parse-excel"), {
    method: "POST",
    body,
  });
  if (!response.ok) throw new Error(await readError(response, "Failed to read Excel"));
  return response.json();
}

export async function draftBobLodgementFromExcel(params: {
  companyId: string;
  row: BobLodgementExcelRow;
}): Promise<BobLodgementDraft> {
  const response = await fetch(getApiUrl("/api/BobLodgement/draft-from-excel"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(params),
  });
  if (!response.ok) throw new Error(await readError(response, "Failed to build draft"));
  return response.json();
}

export async function downloadBobLodgementPdf(request: {
  companyId: string;
  fields: BobLodgementField[];
}): Promise<string> {
  const response = await fetch(getApiUrl("/api/BobLodgement/pdf"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error(await readError(response, "Failed to download PDF"));
  const blob = await response.blob();
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^";]+)"?/i)?.[1] ||
    `BOB_Lodgement_${request.companyId}_${new Date().toISOString().slice(0, 10)}.pdf`;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}
