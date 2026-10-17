import { getApiUrl } from "@/lib/api-config";

export interface GstItc04Registration {
  gstin: string;
  code: string;
  known: boolean;
  units: string[];
  sentRows: number;
  receivedRows: number;
  sentQty: number;
  receivedQty: number;
  sentValue: number;
}

export interface GstItc04Table4Row {
  manufacturerGstin: string;
  code: string;
  unit: string;
  jobWorkerGstin: string;
  jobWorkerState: string;
  jobWorkerType: string;
  challanNo: string;
  challanDate: string;
  goodsType: string;
  description: string;
  uqc: string;
  quantity: number;
  taxableValue: number;
  igstRate: number;
  cgstRate: number;
  sgstRate: number;
  cess: number;
  jobWorkerName: string;
  natureOfJob: string;
}

export interface GstItc04Table5Row {
  manufacturerGstin: string;
  code: string;
  unit: string;
  jobWorkerGstin: string;
  jobWorkerState: string;
  originalChallanNo: string;
  originalChallanDate: string;
  jobWorkerChallanNo: string;
  jobWorkerChallanDate: string;
  natureOfJob: string;
  description: string;
  uqc: string;
  quantity: number;
  lossUqc: string;
  lossQuantity: number;
  jobWorkerName: string;
  inwardNo: string;
}

export interface GstItc04 {
  period: string;
  periodLabel: string;
  financialYear: string;
  taxPeriod: string;
  from: string;
  to: string;
  generatedAt: string;
  registrations: GstItc04Registration[];
  table4: GstItc04Table4Row[];
  table5A: GstItc04Table5Row[];
  table5B: GstItc04Table5Row[];
  table5C: GstItc04Table5Row[];
}

export async function getGstItc04(period: string, gstin?: string, refresh = false): Promise<GstItc04> {
  const params = new URLSearchParams({ period });
  if (gstin) params.set("gstin", gstin);
  if (refresh) params.set("refresh", "true");
  const response = await fetch(getApiUrl(`/api/gst/itc04?${params}`));
  const payload = (await response.json()) as GstItc04 & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to load ITC-04");
  return payload;
}

async function downloadGstItc04File(
  kind: "excel" | "csv",
  period: string,
  gstin?: string,
): Promise<string> {
  const params = new URLSearchParams({ period });
  if (gstin) params.set("gstin", gstin);
  const response = await fetch(getApiUrl(`/api/gst/itc04/${kind}?${params}`));
  if (!response.ok) {
    let message = kind === "csv" ? "Failed to download CSV" : "Failed to download Excel";
    try {
      const payload = (await response.json()) as { message?: string };
      if (payload.message) message = payload.message;
    } catch {
      // keep default
    }
    throw new Error(message);
  }
  const blob = await response.blob();
  const fallback = kind === "csv" ? `ITC04_JobWork_${period}_CSV.zip` : `ITC_04_Offline_Utility_${period}.xlsm`;
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^";]+)"?/i)?.[1] || fallback;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}

export function downloadGstItc04Excel(period: string, gstin?: string): Promise<string> {
  return downloadGstItc04File("excel", period, gstin);
}

export function downloadGstItc04Csv(period: string, gstin?: string): Promise<string> {
  return downloadGstItc04File("csv", period, gstin);
}

/** Build period options: current and previous two half-years. */
export function itc04PeriodOptions(now = new Date()): { value: string; label: string }[] {
  const y = now.getFullYear();
  const m = now.getMonth() + 1;
  const currentH1 = m >= 4 && m <= 9;
  const currentYear = currentH1 ? y : m >= 10 ? y : y - 1;
  const currentHalf = currentH1 || m >= 10 ? (currentH1 ? 1 : 2) : 2;
  const list: { value: string; label: string }[] = [];
  let year = currentYear;
  let half = currentHalf;
  for (let i = 0; i < 4; i++) {
    const value = `${year}-H${half}`;
    const label = half === 1 ? `Apr–Sep ${year}` : `Oct ${year}–Mar ${year + 1}`;
    list.push({ value, label });
    if (half === 2) {
      half = 1;
    } else {
      half = 2;
      year -= 1;
    }
  }
  return list;
}
