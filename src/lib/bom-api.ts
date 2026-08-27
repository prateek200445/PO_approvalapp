import { getApiUrl } from "@/lib/api-config";
import type {
  BomCreateLineInput,
  BomCreatePreviewResult,
  BomCreateRequest,
  BomEditorSnapshot,
  BomSaveResult,
  BomCustomerOption,
  BomCustomerUpdate,
  BomDetailResult,
  BomSearchRequest,
  BomSearchResult,
  BomSendEmailRequest,
  BomListItem,
} from "@/lib/bom-types";
import { normalizeListItem } from "@/lib/bom-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchBomPartyNames(): Promise<string[]> {
  const partiesRes = await fetch(getApiUrl("/api/bom/parties"));
  if (partiesRes.ok) {
    const names = (await partiesRes.json()) as unknown;
    if (Array.isArray(names) && names.length > 0 && names.every((n) => typeof n === "string")) {
      return names;
    }
  }

  // Older live API has no /parties route — "parties" hits GET /api/bom/{qtn} and 404s.
  const mappingRes = await fetch(getApiUrl("/api/bom/party-mapping"));
  const groups = await parseJson<Array<Record<string, unknown>>>(mappingRes);
  const names = groups
    .map((g) => String(g.displayName ?? g.DisplayName ?? "").trim())
    .filter(Boolean);
  return [...new Set(names)].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
}

export async function fetchBomCustomers(): Promise<BomCustomerOption[]> {
  const res = await fetch(getApiUrl("/api/bom/customers"));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map((row) => ({
    companyName: String(row.companyName ?? row.CompanyName ?? ""),
    email: (row.email ?? row.Email) as string | null | undefined,
    email1: (row.email1 ?? row.Email1) as string | null | undefined,
    email2: (row.email2 ?? row.Email2) as string | null | undefined,
    city: (row.city ?? row.City) as string | null | undefined,
    country: (row.country ?? row.Country) as string | null | undefined,
    fromMaster: Boolean(row.fromMaster ?? row.FromMaster),
    aliasCount: Number(row.aliasCount ?? row.AliasCount ?? 1),
    mappingType: (row.mappingType ?? row.MappingType) as string | null | undefined,
    officialName: (row.officialName ?? row.OfficialName) as string | null | undefined,
  }));
}

export async function fetchBomUsers(): Promise<string[]> {
  const res = await fetch(getApiUrl("/api/bom/users"));
  return parseJson<string[]>(res);
}

export async function searchBom(request: BomSearchRequest): Promise<BomSearchResult> {
  const res = await fetch(getApiUrl("/api/bom/search"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  const rawItems = (data.items ?? data.Items ?? []) as Array<Record<string, unknown>>;
  return {
    items: rawItems.map(normalizeListItem),
    totalCount: Number(data.totalCount ?? data.TotalCount ?? 0),
    page: Number(data.page ?? data.Page ?? 1),
    pageSize: Number(data.pageSize ?? data.PageSize ?? 20),
    totalPages: Number(data.totalPages ?? data.TotalPages ?? 0),
  };
}

export async function fetchBomDetail(filePoNo: string): Promise<BomDetailResult> {
  const res = await fetch(getApiUrl(`/api/bom/${encodeURIComponent(filePoNo)}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeDetail(data);
}

export async function fetchBomEditor(filePoNo: string): Promise<BomEditorSnapshot | null> {
  const res = await fetch(getApiUrl(`/api/bom/editor/${encodeURIComponent(filePoNo)}`));
  if (res.status === 404) return null;
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeEditorSnapshot(data);
}

export async function previewBom(request: BomCreateRequest): Promise<BomCreatePreviewResult> {
  const res = await fetch(getApiUrl("/api/bom/preview"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizePreview(data);
}

export async function createBom(request: BomCreateRequest): Promise<BomSaveResult> {
  const res = await fetch(getApiUrl("/api/bom"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeSaveResult(data);
}

export async function updateBom(filePoNo: string, request: BomCreateRequest): Promise<BomSaveResult> {
  const res = await fetch(getApiUrl(`/api/bom/${encodeURIComponent(filePoNo)}`), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeSaveResult(data);
}

export async function updateBomCustomer(
  companyName: string,
  payload: BomCustomerUpdate,
): Promise<void> {
  const res = await fetch(getApiUrl(`/api/bom/customers/${encodeURIComponent(companyName)}`), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  await parseJson(res);
}

export async function fetchBomCustomer(companyName: string): Promise<BomCustomerOption | null> {
  const res = await fetch(getApiUrl(`/api/bom/customers/${encodeURIComponent(companyName)}`));
  if (res.status === 404) return null;
  const data = await parseJson<Record<string, unknown>>(res);
  return {
    companyName: String(data.companyName ?? data.CompanyName ?? companyName),
    email: (data.email ?? data.Email) as string | null | undefined,
    email1: (data.email1 ?? data.Email1) as string | null | undefined,
    email2: (data.email2 ?? data.Email2) as string | null | undefined,
    city: (data.city ?? data.City) as string | null | undefined,
    country: (data.country ?? data.Country) as string | null | undefined,
    fromMaster: Boolean(data.fromMaster ?? data.FromMaster),
  };
}

export async function sendBomEmail(request: BomSendEmailRequest): Promise<{ jobId?: string }> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 15000);

  try {
    const res = await fetch(getApiUrl("/api/bom/email"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        filePoNo: request.filePoNo,
        to: request.to,
        cc: request.cc || undefined,
        bcc: request.bcc || undefined,
        subject: request.subject || undefined,
        body: request.body || undefined,
      }),
      signal: controller.signal,
      keepalive: true,
    });
    const data = await parseJson<{ jobId?: string; message?: string }>(res);
    return { jobId: data.jobId };
  } catch (err: unknown) {
    if (err instanceof Error && err.name === "AbortError") {
      return {};
    }
    throw err;
  } finally {
    clearTimeout(timeoutId);
  }
}

export type BomEmailJobStatus = {
  jobId: string;
  filePoNo: string;
  to: string;
  state: "queued" | "loading_bom" | "building_pdf" | "sending" | "sent" | "failed" | string;
  error?: string | null;
  queuedAt: string;
  completedAt?: string | null;
};

export async function fetchBomEmailStatus(jobId: string): Promise<BomEmailJobStatus | null> {
  const res = await fetch(getApiUrl(`/api/bom/email-status/${encodeURIComponent(jobId)}`));
  if (res.status === 404) return null;
  return parseJson<BomEmailJobStatus>(res);
}

export async function waitForBomEmailResult(
  jobId: string,
  opts?: { timeoutMs?: number; intervalMs?: number },
): Promise<BomEmailJobStatus | null> {
  const timeoutMs = opts?.timeoutMs ?? 120_000;
  const intervalMs = opts?.intervalMs ?? 3_000;
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const status = await fetchBomEmailStatus(jobId);
    if (!status) return null;
    if (status.state === "sent" || status.state === "failed") return status;
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return null;
}

export function bomPdfUrl(qtnNo: string): string {
  return getApiUrl(`/api/pdf/bom?filePoNo=${encodeURIComponent(qtnNo)}`);
}

function strField(obj: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = obj[key];
    if (value != null && String(value).trim() !== "") return String(value);
  }
  return "";
}

function normalizeDetail(data: Record<string, unknown>): BomDetailResult {
  const header = (data.header ?? data.Header ?? {}) as Record<string, unknown>;
  const lines = (data.lines ?? data.Lines ?? []) as Array<Record<string, unknown>>;
  const reportLines = (data.reportLines ?? data.ReportLines ?? []) as Array<Record<string, unknown>>;

  return {
    header: {
      qtnNo: strField(header, "qtnNo", "QtnNo"),
      partyName: strField(header, "partyName", "PartyName"),
      date: (header.date ?? header.Date) as string | null,
      user: strField(header, "user", "User"),
      bagType: strField(header, "bagType", "BagType"),
      sizeL: num(header.sizeL ?? header.SizeL),
      sizeW: num(header.sizeW ?? header.SizeW),
      sizeH: num(header.sizeH ?? header.SizeH),
      sizeType: strField(header, "sizeType", "SizeType"),
      swl: strField(header, "swl", "Swl"),
      sfRatio: strField(header, "sfRatio", "SfRatio"),
      qty: strField(header, "qty", "Qty"),
      qtyUnit: strField(header, "qtyUnit", "QtyUnit"),
      totalKg: num(header.totalKg ?? header.TotalKg),
      printType: strField(header, "printType", "PrintType"),
      poNo: strField(header, "poNo", "PoNo"),
      poNos: strField(header, "poNos", "PoNos"),
      srNo: strField(header, "srNo", "SrNo"),
      instruction: strField(header, "instruction", "Instruction"),
      refNo: strField(header, "refNo", "RefNo"),
      doc: strField(header, "doc", "Doc"),
      doc1: strField(header, "doc1", "Doc1"),
      doc2: strField(header, "doc2", "Doc2"),
      loopSpec: strField(header, "loopSpec", "LoopSpec"),
      linerSpec: strField(header, "linerSpec", "LinerSpec"),
      topSpoutType: strField(header, "topSpoutType", "TopSpoutType"),
      bottomType: strField(header, "bottomType", "BottomType"),
      fabColor: strField(header, "fabColor", "FabColor"),
      printingRemarks: strField(header, "printingRemarks", "PrintingRemarks"),
      bodyRemarks: strField(header, "bodyRemarks", "BodyRemarks"),
      marketingInvNo: strField(header, "marketingInvNo", "MarketingInvNo"),
      isDropLoop: strField(header, "isDropLoop", "IsDropLoop"),
      rpFabric: strField(header, "rpFabric", "RpFabric"),
      knotType: strField(header, "knotType", "KnotType"),
    },
    lines: lines.map((row) => ({
      sortOrder: Number(row.sortOrder ?? row.SortOrder ?? 0),
      heading: strField(row, "heading", "Heading"),
      gsm: strField(row, "gsm", "Gsm"),
      lami: strField(row, "lami", "Lami"),
      color: strField(row, "color", "Color"),
      fabricSize: strField(row, "fabricSize", "FabricSize"),
      cutSize: strField(row, "cutSize", "CutSize"),
      totalMtr: num(row.totalMtr ?? row.TotalMtr),
      totalKg: num(row.totalKg ?? row.TotalKg),
      gpm: strField(row, "gpm", "Gpm"),
      remarks: strField(row, "remarks", "Remarks"),
    })),
    reportLines: reportLines.map((row) => ({
      heading: strField(row, "heading", "Heading"),
      gsm: strField(row, "gsm", "Gsm"),
      lami: strField(row, "lami", "Lami"),
      color: strField(row, "color", "Color"),
      fabricSize: strField(row, "fabricSize", "FabricSize"),
      cutSize: strField(row, "cutSize", "CutSize"),
      totalMtr: num(row.totalMtr ?? row.TotalMtr),
      headTotalKg: num(row.headTotalKg ?? row.HeadTotalKg),
      remarks: strField(row, "remarks", "Remarks"),
    })),
  };
}

function normalizeCreateLine(row: Record<string, unknown>): BomCreateLineInput {
  return {
    sortOrder: Number(row.sortOrder ?? row.SortOrder ?? 0),
    heading: strField(row, "heading", "Heading"),
    gsm: strField(row, "gsm", "Gsm"),
    lami: strField(row, "lami", "Lami"),
    color: strField(row, "color", "Color"),
    fabricSize: strField(row, "fabricSize", "FabricSize"),
    cutSize: strField(row, "cutSize", "CutSize"),
    totalMtr: num(row.totalMtr ?? row.TotalMtr),
    totalKg: num(row.totalKg ?? row.TotalKg),
    remarks: strField(row, "remarks", "Remarks"),
    gpm: strField(row, "gpm", "Gpm"),
  };
}

function normalizeEditorSnapshot(data: Record<string, unknown>): BomEditorSnapshot {
  const header = (data.header ?? data.Header ?? {}) as Record<string, unknown>;
  const lines = (data.lines ?? data.Lines ?? []) as Array<Record<string, unknown>>;
  const approvals = (data.approvals ?? data.Approvals ?? []) as unknown[];

  return {
    header: {
      filePoNo: strField(header, "filePoNo", "FilePoNo"),
      customer: strField(header, "customer", "Customer"),
      sysDate: (header.sysDate ?? header.SysDate) as string | null,
      printType: strField(header, "printType", "PrintType"),
      poNo: strField(header, "poNo", "PoNo"),
      poNos: strField(header, "poNos", "PoNos"),
      bagType: strField(header, "bagType", "BagType"),
      sizeL: num(header.sizeL ?? header.SizeL),
      sizeW: num(header.sizeW ?? header.SizeW),
      sizeH: num(header.sizeH ?? header.SizeH),
      sizeType: strField(header, "sizeType", "SizeType"),
      swl: strField(header, "swl", "Swl"),
      sfRatio: strField(header, "sfRatio", "SfRatio"),
      qty: strField(header, "qty", "Qty"),
      qtyUnit: strField(header, "qtyUnit", "QtyUnit"),
      fsType: strField(header, "fsType", "FSType"),
      dsType: strField(header, "dsType", "DSType"),
      dsType1: strField(header, "dsType1", "DSType1"),
      dsType2: strField(header, "dsType2", "DSType2"),
      loopType: strField(header, "loopType", "LoopType"),
      fabColor: strField(header, "fabColor", "FabColor"),
      instruction: strField(header, "instruction", "Instruction"),
      bodyRemarks: strField(header, "bodyRemarks", "BodyRemarks"),
      printingRemarks: strField(header, "printingRemarks", "PrintingRemarks"),
      refNo: strField(header, "refNo", "RefNo"),
      doc: strField(header, "doc", "Doc"),
      doc1: strField(header, "doc1", "Doc1"),
      doc2: strField(header, "doc2", "Doc2"),
      docUnit: strField(header, "docUnit", "DocUnit"),
      docNumber: strField(header, "docNumber", "DocNumber"),
      knotType: strField(header, "knotType", "KnotType"),
      rpFabric: strField(header, "rpFabric", "RpFabric"),
      isDropLoop: Boolean(header.isDropLoop ?? header.IsDropLoop),
      userName: strField(header, "userName", "UserName"),
    },
    approvals: approvals.map((value) => String(value)).filter(Boolean),
    lines: lines.map(normalizeCreateLine),
    bom1Values: normalizeStringMap(data.bom1Values ?? data.Bom1Values),
    bom3Values: normalizeStringMap(data.bom3Values ?? data.Bom3Values),
  };
}

function normalizePreview(data: Record<string, unknown>): BomCreatePreviewResult {
  const lines = (data.lines ?? data.Lines ?? []) as Array<Record<string, unknown>>;
  const approvals = (data.approvals ?? data.Approvals ?? []) as unknown[];
  const warnings = (data.warnings ?? data.Warnings ?? []) as unknown[];

  return {
    filePoNo: strField(data, "filePoNo", "FilePoNo"),
    customer: strField(data, "customer", "Customer"),
    lineCount: Number(data.lineCount ?? data.LineCount ?? 0),
    totalKg: Number(data.totalKg ?? data.TotalKg ?? 0),
    approvals: approvals.map((value) => String(value)).filter(Boolean),
    warnings: warnings.map((value) => String(value)).filter(Boolean),
    lines: lines.map(normalizeCreateLine),
  };
}

function normalizeSaveResult(data: Record<string, unknown>): BomSaveResult {
  return {
    filePoNo: strField(data, "filePoNo", "FilePoNo"),
    srNo: strField(data, "srNo", "SrNo"),
    created: Boolean(data.created ?? data.Created),
    updated: Boolean(data.updated ?? data.Updated),
    totalKg: Number(data.totalKg ?? data.TotalKg ?? 0),
    lineCount: Number(data.lineCount ?? data.LineCount ?? 0),
  };
}

function num(v: unknown): number | null {
  if (v == null || v === "") return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

function normalizeStringMap(value: unknown): Record<string, string | null> {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};
  return Object.fromEntries(
    Object.entries(value as Record<string, unknown>).map(([key, item]) => [key, item == null ? null : String(item)]),
  );
}

export type { BomListItem };
