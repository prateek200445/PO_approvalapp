export type BomListItem = {
  qtnNo: string;
  partyName: string;
  sizeL: number | null;
  sizeW: number | null;
  sizeH: number | null;
  date: string | null;
  user: string;
  bagType: string;
  swl: string;
  qty: string;
  totalKg: number | null;
  srNo: string;
};

export type BomSearchResult = {
  items: BomListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export type BomCustomerOption = {
  companyName: string;
  email?: string | null;
  email1?: string | null;
  email2?: string | null;
  city?: string | null;
  country?: string | null;
  fromMaster: boolean;
  aliasCount?: number;
  mappingType?: string | null;
  officialName?: string | null;
};

export type BomCustomerUpdate = {
  email?: string;
  email1?: string;
  email2?: string;
  cnctPerson?: string;
  telNo1?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
};

export type BomHeader = {
  qtnNo: string;
  partyName: string;
  date: string | null;
  user: string;
  bagType: string;
  sizeL: number | null;
  sizeW: number | null;
  sizeH: number | null;
  sizeType: string;
  swl: string;
  sfRatio: string;
  qty: string;
  qtyUnit: string;
  totalKg: number | null;
  printType: string;
  poNo: string;
  poNos: string;
  srNo: string;
  instruction: string;
  refNo: string;
  doc: string;
  doc1: string;
  doc2: string;
  loopSpec: string;
  linerSpec: string;
  topSpoutType: string;
  bottomType: string;
  fabColor: string;
  printingRemarks: string;
  bodyRemarks: string;
  marketingInvNo: string;
  isDropLoop: string;
  rpFabric: string;
  knotType: string;
};

export type BomLineItem = {
  sortOrder: number;
  heading: string;
  gsm: string;
  lami: string;
  color: string;
  fabricSize: string;
  cutSize: string;
  totalMtr: number | null;
  totalKg: number | null;
  gpm: string;
  remarks: string;
};

export type BomReportLine = {
  heading: string;
  gsm: string;
  lami: string;
  color: string;
  fabricSize: string;
  cutSize: string;
  totalMtr: number | null;
  headTotalKg: number | null;
  remarks: string;
};

export type BomDetailResult = {
  header: BomHeader;
  lines: BomLineItem[];
  reportLines: BomReportLine[];
};

export type BomSearchRequest = {
  dateFrom?: string;
  dateTo?: string;
  partyName?: string;
  userName?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortDirection?: "asc" | "desc";
  /** @deprecated use sortDirection */
  dateSortDesc?: boolean;
};

export type BomSendEmailRequest = {
  filePoNo: string;
  to: string;
  cc?: string;
  bcc?: string;
  subject?: string;
  body?: string;
};

export function toInputDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function defaultBomDateFrom(): string {
  const d = new Date();
  d.setMonth(d.getMonth() - 3);
  return toInputDate(d);
}

export function formatBomDate(value: string | null | undefined): string {
  if (!value) return "—";
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  const d = match
    ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
    : new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function formatDimension(l: number | null, w: number | null, h: number | null): string {
  const parts = [l, w, h].map((v) => (v == null ? "—" : String(v)));
  return `${parts[0]} × ${parts[1]} × ${parts[2]}`;
}

/** Normalize API PascalCase payloads from older responses if needed */
export function normalizeListItem(row: Record<string, unknown>): BomListItem {
  return {
    qtnNo: String(row.qtnNo ?? row.QtnNo ?? ""),
    partyName: String(row.partyName ?? row.PartyName ?? ""),
    sizeL: numOrNull(row.sizeL ?? row.SizeL),
    sizeW: numOrNull(row.sizeW ?? row.SizeW),
    sizeH: numOrNull(row.sizeH ?? row.SizeH),
    date: (row.date ?? row.Date) as string | null,
    user: String(row.user ?? row.User ?? ""),
    bagType: String(row.bagType ?? row.BagType ?? ""),
    swl: String(row.swl ?? row.Swl ?? ""),
    qty: String(row.qty ?? row.Qty ?? ""),
    totalKg: numOrNull(row.totalKg ?? row.TotalKg),
    srNo: String(row.srNo ?? row.SrNo ?? ""),
  };
}

function numOrNull(v: unknown): number | null {
  if (v == null || v === "") return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

export function bomDetailPath(qtnNo: string): string {
  return `/bom/${qtnNo.split("/").map(encodeURIComponent).join("/")}`;
}
