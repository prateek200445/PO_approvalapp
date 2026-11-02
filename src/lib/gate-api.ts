import { getApiUrl } from "@/lib/api-config";

export interface GateSite {
  gateId: number;
  factoryCode: string;
  factoryName: string;
  gateCode: string;
  gateName: string;
}

export interface GatePurpose {
  purposeId: number;
  purposeCode: string;
  purposeName: string;
}

export interface GateContext {
  roleLabel: string;
  canOperate: boolean;
  canViewRegister: boolean;
  canCorrect: boolean;
  canManageGates: boolean;
  longStayHours: number;
  serverDate: string;
  gates: GateSite[];
  purposes: GatePurpose[];
}

export interface GateEntry {
  entryId: number;
  gateEntryNumber: string;
  factoryCode: string;
  factoryName: string;
  gateId: number;
  gateName: string;
  vehicleNumber: string;
  driverName: string;
  driverMobile: string | null;
  partyName: string | null;
  purposeCode: string;
  purposeName: string;
  remarks: string | null;
  status: "INSIDE" | "EXITED";
  entryAt: string;
  entryUserId: string;
  exitAt: string | null;
  exitUserId: string | null;
  exitGateId: number | null;
  exitGateName: string | null;
  exitRemarks: string | null;
  durationMinutes: number | null;
  isLongStay: boolean;
}

export interface GateSummary {
  insideCount: number;
  todayEntries: number;
  todayExits: number;
  averageTurnaroundMinutes: number | null;
  longStayCount: number;
  longStayHours: number;
  serverDate: string;
  longStay: GateEntry[];
}

export interface GateRegisterFilters {
  factoryCode?: string;
  dateFrom?: string;
  dateTo?: string;
  vehicleNumber?: string;
  driver?: string;
  party?: string;
  purposeCode?: string;
  status?: string;
  longStayOnly?: boolean;
}

export interface GateAudit {
  auditId: number;
  entryId: number;
  action: string;
  changedBy: string;
  changedAt: string;
  fieldName: string | null;
  oldValue: string | null;
  newValue: string | null;
  reason: string | null;
}

async function readError(response: Response, fallback: string): Promise<string> {
  try {
    const payload = (await response.json()) as { message?: string };
    if (payload.message) return payload.message;
  } catch {
    // keep fallback
  }
  return fallback;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function nullable(value: unknown): string | null {
  const next = text(value).trim();
  return next.length === 0 ? null : next;
}

function num(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function mapEntry(row: Record<string, unknown>): GateEntry {
  const status = text(row.status ?? row.Status);
  return {
    entryId: Number(row.entryId ?? row.EntryId ?? 0),
    gateEntryNumber: text(row.gateEntryNumber ?? row.GateEntryNumber),
    factoryCode: text(row.factoryCode ?? row.FactoryCode),
    factoryName: text(row.factoryName ?? row.FactoryName),
    gateId: Number(row.gateId ?? row.GateId ?? 0),
    gateName: text(row.gateName ?? row.GateName),
    vehicleNumber: text(row.vehicleNumber ?? row.VehicleNumber),
    driverName: text(row.driverName ?? row.DriverName),
    driverMobile: nullable(row.driverMobile ?? row.DriverMobile),
    partyName: nullable(row.partyName ?? row.PartyName),
    purposeCode: text(row.purposeCode ?? row.PurposeCode),
    purposeName: text(row.purposeName ?? row.PurposeName),
    remarks: nullable(row.remarks ?? row.Remarks),
    status: status === "EXITED" ? "EXITED" : "INSIDE",
    entryAt: text(row.entryAt ?? row.EntryAt),
    entryUserId: text(row.entryUserId ?? row.EntryUserId),
    exitAt: nullable(row.exitAt ?? row.ExitAt),
    exitUserId: nullable(row.exitUserId ?? row.ExitUserId),
    exitGateId: num(row.exitGateId ?? row.ExitGateId),
    exitGateName: nullable(row.exitGateName ?? row.ExitGateName),
    exitRemarks: nullable(row.exitRemarks ?? row.ExitRemarks),
    durationMinutes: num(row.durationMinutes ?? row.DurationMinutes),
    isLongStay: Boolean(row.isLongStay ?? row.IsLongStay),
  };
}

export function formatGateTime(value: string | null | undefined): string {
  if (!value) return "—";
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/);
  if (!match) return value;
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
  return `${match[3]} ${months[Number(match[2]) - 1]} ${match[1]}, ${match[4]}:${match[5]}`;
}

export function formatDuration(minutes: number | null | undefined): string {
  if (minutes == null || Number.isNaN(minutes)) return "—";
  const whole = Math.max(0, Math.round(minutes));
  const hours = Math.floor(whole / 60);
  const mins = whole % 60;
  if (hours === 0) return `${mins}m`;
  return `${hours}h ${mins}m`;
}

export async function getGateContext(username: string): Promise<GateContext> {
  const params = new URLSearchParams({ username });
  const response = await fetch(getApiUrl(`/api/gate/context?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not open the factory gate."));
  const payload = (await response.json()) as Record<string, unknown>;
  const gates = (payload.gates ?? payload.Gates ?? []) as Record<string, unknown>[];
  const purposes = (payload.purposes ?? payload.Purposes ?? []) as Record<string, unknown>[];
  return {
    roleLabel: text(payload.roleLabel ?? payload.RoleLabel),
    canOperate: Boolean(payload.canOperate ?? payload.CanOperate),
    canViewRegister: Boolean(payload.canViewRegister ?? payload.CanViewRegister),
    canCorrect: Boolean(payload.canCorrect ?? payload.CanCorrect),
    canManageGates: Boolean(payload.canManageGates ?? payload.CanManageGates),
    longStayHours: Number(payload.longStayHours ?? payload.LongStayHours ?? 8),
    serverDate: text(payload.serverDate ?? payload.ServerDate),
    gates: gates.map((gate) => ({
      gateId: Number(gate.gateId ?? gate.GateId ?? 0),
      factoryCode: text(gate.factoryCode ?? gate.FactoryCode),
      factoryName: text(gate.factoryName ?? gate.FactoryName),
      gateCode: text(gate.gateCode ?? gate.GateCode),
      gateName: text(gate.gateName ?? gate.GateName),
    })),
    purposes: purposes.map((purpose) => ({
      purposeId: Number(purpose.purposeId ?? purpose.PurposeId ?? 0),
      purposeCode: text(purpose.purposeCode ?? purpose.PurposeCode),
      purposeName: text(purpose.purposeName ?? purpose.PurposeName),
    })),
  };
}

export async function getGateSummary(username: string, factoryCode: string): Promise<GateSummary> {
  const params = new URLSearchParams({ username });
  if (factoryCode) params.set("factoryCode", factoryCode);
  const response = await fetch(getApiUrl(`/api/gate/summary?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not load the gate dashboard."));
  const payload = (await response.json()) as Record<string, unknown>;
  const longStay = (payload.longStay ?? payload.LongStay ?? []) as Record<string, unknown>[];
  return {
    insideCount: Number(payload.insideCount ?? payload.InsideCount ?? 0),
    todayEntries: Number(payload.todayEntries ?? payload.TodayEntries ?? 0),
    todayExits: Number(payload.todayExits ?? payload.TodayExits ?? 0),
    averageTurnaroundMinutes: num(payload.averageTurnaroundMinutes ?? payload.AverageTurnaroundMinutes),
    longStayCount: Number(payload.longStayCount ?? payload.LongStayCount ?? 0),
    longStayHours: Number(payload.longStayHours ?? payload.LongStayHours ?? 8),
    serverDate: text(payload.serverDate ?? payload.ServerDate),
    longStay: longStay.map(mapEntry),
  };
}

export async function listInsideVehicles(username: string, factoryCode: string, query: string): Promise<GateEntry[]> {
  const params = new URLSearchParams({ username });
  if (factoryCode) params.set("factoryCode", factoryCode);
  if (query.trim()) params.set("q", query.trim());
  const response = await fetch(getApiUrl(`/api/gate/inside?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not load vehicles inside."));
  const payload = (await response.json()) as Record<string, unknown>[];
  return payload.map(mapEntry);
}

export async function createGateEntry(body: {
  username: string;
  factoryCode: string;
  gateId: number;
  vehicleNumber: string;
  driverName: string;
  driverMobile?: string;
  partyName?: string;
  purposeCode: string;
  remarks?: string;
}): Promise<GateEntry> {
  const response = await fetch(getApiUrl("/api/gate/entries"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw new Error(await readError(response, "Could not register the vehicle."));
  return mapEntry((await response.json()) as Record<string, unknown>);
}

export async function exitGateEntry(entryId: number, body: {
  username: string;
  exitGateId?: number;
  exitRemarks?: string;
}): Promise<GateEntry> {
  const response = await fetch(getApiUrl(`/api/gate/entries/${entryId}/exit`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw new Error(await readError(response, "Could not mark the exit."));
  return mapEntry((await response.json()) as Record<string, unknown>);
}

export async function correctGateEntry(entryId: number, body: {
  username: string;
  reason: string;
  vehicleNumber: string;
  driverName: string;
  driverMobile?: string;
  partyName?: string;
  purposeCode: string;
  remarks?: string;
}): Promise<GateEntry> {
  const response = await fetch(getApiUrl(`/api/gate/entries/${entryId}/correct`), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw new Error(await readError(response, "Could not save the correction."));
  return mapEntry((await response.json()) as Record<string, unknown>);
}

export async function listGateRegister(username: string, filters: GateRegisterFilters): Promise<{ total: number; rows: GateEntry[] }> {
  const params = new URLSearchParams({ username });
  Object.entries(filters).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "" || value === false) return;
    params.set(key, String(value));
  });
  const response = await fetch(getApiUrl(`/api/gate/register?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not load the gate register."));
  const payload = (await response.json()) as Record<string, unknown>;
  const rows = (payload.rows ?? payload.Rows ?? []) as Record<string, unknown>[];
  return { total: Number(payload.total ?? payload.Total ?? rows.length), rows: rows.map(mapEntry) };
}

export async function listGateAudit(username: string, entryId: number): Promise<GateAudit[]> {
  const params = new URLSearchParams({ username });
  const response = await fetch(getApiUrl(`/api/gate/entries/${entryId}/audit?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not load the audit trail."));
  const payload = (await response.json()) as Record<string, unknown>[];
  return payload.map((row) => ({
    auditId: Number(row.auditId ?? row.AuditId ?? 0),
    entryId: Number(row.entryId ?? row.EntryId ?? 0),
    action: text(row.action ?? row.Action),
    changedBy: text(row.changedBy ?? row.ChangedBy),
    changedAt: text(row.changedAt ?? row.ChangedAt),
    fieldName: nullable(row.fieldName ?? row.FieldName),
    oldValue: nullable(row.oldValue ?? row.OldValue),
    newValue: nullable(row.newValue ?? row.NewValue),
    reason: nullable(row.reason ?? row.Reason),
  }));
}

export async function downloadGateRegister(username: string, filters: GateRegisterFilters): Promise<void> {
  const params = new URLSearchParams({ username });
  Object.entries(filters).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "" || value === false) return;
    params.set(key, String(value));
  });
  const response = await fetch(getApiUrl(`/api/gate/register/excel?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not download Excel."));
  const blob = await response.blob();
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^"]+)"?/i)?.[1] || "gate-register.xlsx";
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function addFactoryGate(body: {
  username: string;
  factoryCode: string;
  factoryName: string;
  gateCode: string;
  gateName: string;
}): Promise<void> {
  const response = await fetch(getApiUrl("/api/gate/gates"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw new Error(await readError(response, "Could not add the gate."));
}
