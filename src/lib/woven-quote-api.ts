import { getApiUrl } from "@/lib/api-config";

export interface WovenDraft {
  productType: "WovenBag" | "AdStar";
  quoteDate: string;
  sector: string;
  customerName: string;
  ourRef: string;
  customerRef: string;
  bagType: string;
  fabricColour: string;
  faceWidthCm: number | null;
  gussetCm: number | null;
  lengthCm: number | null;
  top: string;
  totalWeightGram: number | null;
  fabricGsm: number | null;
  linerMicron: number | null;
  glossyBoppMicron: number | null;
  mattBoppMicron: number | null;
  metalisedBoppMicron: number | null;
  whitePaperGsm: number | null;
  brownPaperGsm: number | null;
  adhesiveGsm: number | null;
  inkGsm: number | null;
  coatingGsm: number | null;
  handleGram: number | null;
  fabricRecipe: Record<string, number>;
  coatingRecipe: Record<string, number>;
  linerRecipe: Record<string, number>;
  fabricConversion: number | null;
  coatingConversion: number | null;
  linerConversion: number | null;
  wastageFabric: number | null;
  wastageLiner: number | null;
  wastageFinishing: number | null;
  realisationFabric: number | null;
  realisationLiner: number | null;
  bagFinishing: number | null;
  thumbNotch: boolean;
  metalDetection: boolean;
  perforation: boolean;
  flexoPrinting: number | null;
  rewindingPerKg: number | null;
  packingPerKg: number | null;
  palletPerKg: number | null;
  fobCostPerKg: number | null;
  paymentDays: number | null;
  profitPercent: number | null;
  usdInr: number | null;
  euroPerUsd: number | null;
  gbpPerUsd: number | null;
  freightUsdPerKg: number | null;
  commissionUsdPerKg: number | null;
  dimensionUnit: string;
  terms: string;
  seaPort: string;
  currency: string;
  packing: string;
  pcsPerBale: number | null;
  palletSize: string;
  bagsPerPallet: number | null;
  printPlateCost: number | null;
  remarks: string;
  adWidth: number | null;
  adLength: number | null;
  adPatch: number | null;
  adValveLength: number | null;
  adExtendedValve: number | null;
  adFabricGsm: number | null;
  adCoatingGsm: number | null;
  adBoppGsm: number | null;
  adOpenMouth: boolean;
  adUv: boolean;
  adPrinting: string;
  adLinerMicron: number | null;
  adConversion: number | null;
  adSeaFreight: number | null;
  adPcsPerBale: number | null;
  adPcsPerPallet: number | null;
}

export interface WovenLine {
  group: string;
  label: string;
  value: number;
  unit: string;
}

export interface WovenResult {
  warnings: string[];
  cutLengthCm: number;
  grossWidthCm: number;
  fabricGram: number;
  fabricGsmCalculated: number;
  calculatedWeightGram: number;
  totalComponentGram: number;
  fabricRmc: number;
  coatingRmc: number;
  linerRmc: number;
  printedRmc: number;
  totalRmcPerKg: number;
  wastageCost: number;
  costPerPiece: number;
  costPerKg: number;
  workingCapitalPerKg: number;
  profitPerKg: number;
  quotePerKg: number;
  fobUsdPerKg: number;
  cifUsdPerKg: number;
  deliveredInrPerKg: number;
  pricePerPiece: number;
  priceBasis: string;
  printedLabel: string;
  coatedLabel: string;
  uvLabel: string;
  linerLabel: string;
  offerWidth: number | null;
  offerGusset: number | null;
  offerLength: number | null;
  lines: WovenLine[];
}

export interface WovenListItem {
  id: number;
  productType: string;
  quoteDate: string;
  sector: string;
  customerName: string;
  ourRef: string;
  customerRef: string;
  currency: string;
  terms: string;
  pricePerPiece: number;
  totalWeightGram: number;
  createdBy: string;
  updatedAt: string;
  isDraft: boolean;
}

export interface WovenDetail extends WovenListItem {
  draft: WovenDraft;
  result: WovenResult;
}

export interface WovenMaterial {
  code: string;
  name: string;
  sortOrder: number;
  inFabric: boolean;
  inCoating: boolean;
  inLiner: boolean;
}

export interface WovenRate {
  id: number;
  materialCode: string;
  sector: string;
  effectiveDate: string;
  rate: number;
}

export interface WovenCustomer {
  id: number;
  sector: string;
  fullName: string;
  shortName: string;
  isActive: boolean;
}

export interface WovenLookup {
  id: number;
  category: string;
  value: string;
  sortOrder: number;
}

export interface WovenConstant {
  key: string;
  value: number;
  label: string;
  groupName: string;
}

export interface WovenMasters {
  materials: WovenMaterial[];
  lookups: WovenLookup[];
  constants: WovenConstant[];
  customers: WovenCustomer[];
  latestRates: WovenRate[];
}

async function read<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(getApiUrl(path), {
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
    ...init,
  });
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { message?: string } | null;
    throw new Error(body?.message || `Request failed (${response.status})`);
  }
  return response.json() as Promise<T>;
}

export const getWovenMasters = () => read<WovenMasters>("/api/woven-quote/masters");
export const getWovenRates = () => read<WovenRate[]>("/api/woven-quote/rates");
export const listWovenQuotes = (query: string) => read<WovenListItem[]>(`/api/woven-quote${query}`);
export const getWovenQuote = (id: number) => read<WovenDetail>(`/api/woven-quote/${id}`);

export function saveWovenRate(row: { materialCode: string; sector: string; effectiveDate: string; rate: number }) {
  return read<{ ok: boolean }>("/api/woven-quote/rates", { method: "POST", body: JSON.stringify(row) });
}

export function saveWovenConstants(rows: WovenConstant[]) {
  return read<{ ok: boolean }>("/api/woven-quote/constants", { method: "PUT", body: JSON.stringify(rows) });
}

export function saveWovenCustomer(row: Partial<WovenCustomer>) {
  return read<{ id: number }>("/api/woven-quote/customers", { method: "POST", body: JSON.stringify(row) });
}

export function saveWovenLookup(category: string, value: string) {
  return read<{ ok: boolean }>("/api/woven-quote/lookups", { method: "POST", body: JSON.stringify({ category, value, sortOrder: 0 }) });
}

export function createWovenQuote(draft: WovenDraft, username: string, asDraft = false) {
  const draftFlag = asDraft ? "&draft=true" : "";
  return read<WovenDetail>(`/api/woven-quote?username=${encodeURIComponent(username)}${draftFlag}`, {
    method: "POST",
    body: JSON.stringify({ draft }),
  });
}

export function updateWovenQuote(id: number, draft: WovenDraft, asDraft = false) {
  return read<WovenDetail>(`/api/woven-quote/${id}${asDraft ? "?draft=true" : ""}`, { method: "PUT", body: JSON.stringify({ draft }) });
}

export function deleteWovenQuote(id: number) {
  return read<{ ok: boolean }>(`/api/woven-quote/${id}`, { method: "DELETE" });
}
