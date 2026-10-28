import { getApiUrl } from "@/lib/api-config";

export interface GstLiabilityCompany {
  code: string;
  gstin: string;
  name: string;
  known: boolean;
  units: string[];
  outputIgst: number;
  outputCgst: number;
  outputSgst: number;
  inputIgst: number;
  inputCgst: number;
  inputSgst: number;
  openingCredit: number;
  openingCash: number;
  inputBalance: number;
  outputBalance: number;
  opBalance: number;
  inputClosingIgst: number;
  inputClosingCgst: number;
  inputClosingSgst: number;
  outputClosingIgst: number;
  outputClosingCgst: number;
  outputClosingSgst: number;
  pendingInputIgst: number;
  pendingInputCgst: number;
  pendingInputSgst: number;
  pendingOutputIgst: number;
  pendingOutputCgst: number;
  pendingOutputSgst: number;
  unclaimedIgst: number;
  unclaimedCgst: number;
  unclaimedSgst: number;
  setOff: number;
  moved: boolean;
}

export interface GstLiabilityLedgerRow {
  unit: string;
  gstin: string;
  code: string;
  known: boolean;
  ledger: string;
  group: string;
  kind: "output" | "input" | "other" | "cash";
  tax: string;
  opening: number;
  debit: number;
  credit: number;
  closing: number;
  clBalance: number;
  setOff: number;
}

export interface GstLiability {
  month: string;
  from: string;
  to: string;
  excludeSetOff: boolean;
  asOnDate: string;
  generatedAt: string;
  companies: GstLiabilityCompany[];
  ledgers: GstLiabilityLedgerRow[];
}

export interface GstLiabilityBalance {
  code: string;
  igst: number;
  cgst: number;
  sgst: number;
}

export async function getGstLiability(month: string, excludeSetOff: boolean, refresh = false): Promise<GstLiability> {
  const params = new URLSearchParams({ month, excludeSetOff: String(excludeSetOff) });
  if (refresh) params.set("refresh", "true");
  const response = await fetch(getApiUrl(`/api/gst/liability?${params}`));
  const payload = (await response.json()) as GstLiability & { message?: string };
  if (!response.ok) throw new Error(payload.message || "Failed to load GST liability");
  return payload;
}

export async function downloadGstLiabilityExcel(request: {
  month: string;
  excludeSetOff: boolean;
  codes: string[];
  balances: GstLiabilityBalance[];
}): Promise<string> {
  const response = await fetch(getApiUrl("/api/gst/liability/excel"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!response.ok) {
    let message = "Failed to download Excel";
    try {
      const payload = (await response.json()) as { message?: string };
      if (payload.message) message = payload.message;
    } catch {
      // keep default
    }
    throw new Error(message);
  }
  const blob = await response.blob();
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^";]+)"?/i)?.[1] ||
    `GST_Input_Output_Liability_${request.month}.xlsx`;
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
  return fileName;
}
