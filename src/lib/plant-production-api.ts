import { getApiUrl } from "@/lib/api-config";

async function read<T>(res: Response): Promise<T> {
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((data as { message?: string }).message || "Request failed");
  return data as T;
}

function q(params: Record<string, string>) {
  return new URLSearchParams(params).toString();
}

export type LoomSetupAllocation = {
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

export type BobbinStock = { itemCode: string; item: string; stock: number };

export function getLoomSetup(company: string, date: string) {
  return fetch(getApiUrl(`/api/plant-production/loom?${q({ company, date })}`)).then((r) =>
    read<{ allocations: LoomSetupAllocation[]; bobbins: BobbinStock[] }>(r),
  );
}

export function getLoomRecent(company: string, date: string) {
  return fetch(getApiUrl(`/api/plant-production/loom/recent?${q({ company, date })}`)).then((r) =>
    read<{ id: number; shift: string; loom: string; rolls: number; net: number }[]>(r),
  );
}

export function getLoomEntry(company: string, id: number) {
  return fetch(getApiUrl(`/api/plant-production/loom/entry?${q({ company, id: String(id) })}`)).then((r) =>
    read<{
      loom: string; rollNo: string; quality: string; mesh: string; gsm: number; sector: string;
      metre: number; gross: number; tare: number; cover: number; net: number; gpm: number; actualGsm: number;
      remarks: string; party: string; order: string; warpQty: number; weftQty: number;
      warpCode: string; weftCode: string; fg: string;
    }[]>(r),
  );
}

export function saveLoom(body: unknown) {
  return fetch(getApiUrl("/api/plant-production/loom/save"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ recordLogId: number; rollNos: string[] }>(r));
}

export function deleteLoom(body: unknown) {
  return fetch(getApiUrl("/api/plant-production/loom/delete"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ ok: boolean }>(r));
}

export function saveNeedle(body: unknown) {
  return fetch(getApiUrl("/api/plant-production/needle/save"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ rolls: { transId: number; rollNo: string }[]; net: number }>(r));
}

export function deleteNeedle(body: unknown) {
  return fetch(getApiUrl("/api/plant-production/needle/delete"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ ok: boolean }>(r));
}

export function getNeedleRecent(company: string, date: string) {
  return fetch(getApiUrl(`/api/plant-production/needle/recent?${q({ company, date })}`)).then((r) =>
    read<{ id: number; shift: string; rollNo: string; net: number; loom: string }[]>(r),
  );
}

export type LamListRoll = {
  rollNo: string;
  metre: number;
  gross: number;
  tare: number;
  net: number;
  originalNet: number;
  quality: string;
  gsm: string;
  outsideRoll: string;
  category: string;
  party: string;
  order: string;
  ulCode: string;
  lamCode: string;
  lamRoll: string;
};

export function getLaminationRolls(company: string, date: string) {
  return fetch(getApiUrl(`/api/plant-production/lamination-rolls?${q({ company, date })}`)).then((r) => read<LamListRoll[]>(r));
}

export function saveLamination(body: unknown) {
  return fetch(getApiUrl("/api/plant-production/lamination/save"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ rolls: { transId: number; rollNo: string }[]; source: number; laminated: number }>(r));
}

export function getLaminationRecent(company: string, date: string) {
  return fetch(getApiUrl(`/api/plant-production/lamination/recent?${q({ company, date })}`)).then((r) =>
    read<{ id: number; shift: string; rollNo: string; source: number; laminated: number }[]>(r),
  );
}
