import { getApiUrl } from "@/lib/api-config";
import {
  normalizeBacklog,
  normalizeFactoryConfig,
  normalizeFactoryOption,
  normalizeLineConfig,
  normalizeLoomPool,
  normalizeRecalculateResult,
  normalizeSetupConstants,
  normalizeTeamFactor,
  normalizeDowntime,
  normalizeLoomPreferenceChart,
  normalizeInterUnitDefaults,
  normalizeOrderRoute,
  type PlanningBacklog,
  type PlanningDowntime,
  type PlanningLoomPreferenceChart,
  type PlanningInterUnitDefaults,
  type PlanningOrderRoute,
  type PlanningFactoryConfig,
  type PlanningFactoryOption,
  type PlanningLineConfig,
  type PlanningLoomPool,
  type PlanningSetupConstants,
  type PlanningTeamFactor,
  type RecalculateTeamFactorsResult,
} from "@/lib/planning/setup-types";

async function parseJson<T>(res: Response): Promise<T> {
  const data = await res.json();
  if (!res.ok) {
    throw new Error((data as { message?: string }).message || res.statusText || "Request failed");
  }
  return data as T;
}

export async function fetchPlanningSetupConstants(): Promise<PlanningSetupConstants> {
  const res = await fetch(getApiUrl("/api/planning/setup/constants"));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeSetupConstants(data);
}

export async function searchPlanningFactories(q: string): Promise<PlanningFactoryOption[]> {
  const params = new URLSearchParams({ limit: "25" });
  if (q.trim()) params.set("q", q.trim());
  const res = await fetch(getApiUrl(`/api/planning/setup/factories/search?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeFactoryOption);
}

export async function fetchPlanningFactoryConfig(company: string): Promise<PlanningFactoryConfig> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/factories/config?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeFactoryConfig(data);
}

export async function savePlanningFactoryConfig(config: PlanningFactoryConfig): Promise<PlanningFactoryConfig> {
  const res = await fetch(getApiUrl("/api/planning/setup/factories/config"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(config),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeFactoryConfig(data);
}

export async function fetchPlanningLines(company: string): Promise<PlanningLineConfig[]> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/lines?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLineConfig);
}

export async function importPlanningLinesFromErp(company: string): Promise<PlanningLineConfig[]> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/lines/import-erp?${params.toString()}`), { method: "POST" });
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLineConfig);
}

export async function savePlanningLines(company: string, lines: PlanningLineConfig[]): Promise<void> {
  const res = await fetch(getApiUrl("/api/planning/setup/lines"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ companyName: company, lines }),
  });
  await parseJson(res);
}

export async function fetchPlanningLoomPool(company: string): Promise<PlanningLoomPool[]> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/looms?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLoomPool);
}

export async function savePlanningLoomPool(company: string, looms: PlanningLoomPool[]): Promise<void> {
  const res = await fetch(getApiUrl("/api/planning/setup/looms"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ companyName: company, looms }),
  });
  await parseJson(res);
}

export async function fetchPlanningTeamFactors(company: string): Promise<PlanningTeamFactor[]> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/team-factors?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeTeamFactor);
}

export async function savePlanningTeamFactors(company: string, factors: PlanningTeamFactor[]): Promise<void> {
  const res = await fetch(getApiUrl("/api/planning/setup/team-factors"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ companyName: company, factors }),
  });
  await parseJson(res);
}

export async function recalculatePlanningTeamFactors(
  company: string,
  sampleDays = 30,
): Promise<RecalculateTeamFactorsResult> {
  const params = new URLSearchParams({ company, sampleDays: String(sampleDays) });
  const res = await fetch(getApiUrl(`/api/planning/setup/team-factors/recalculate?${params.toString()}`), {
    method: "POST",
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeRecalculateResult(data);
}

export async function fetchPlanningBacklog(company: string, status = "Open"): Promise<PlanningBacklog[]> {
  const params = new URLSearchParams({ company, status });
  const res = await fetch(getApiUrl(`/api/planning/setup/backlog?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeBacklog);
}

export async function createPlanningBacklog(payload: {
  companyName: string;
  lineNo: number;
  shift: string;
  orderNo: string;
  backlogQty: number;
  reason?: string;
}): Promise<PlanningBacklog> {
  const res = await fetch(getApiUrl("/api/planning/setup/backlog"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeBacklog(data);
}

export async function clearPlanningBacklog(backlogId: number): Promise<void> {
  const res = await fetch(getApiUrl(`/api/planning/setup/backlog/${backlogId}/clear`), { method: "POST" });
  await parseJson(res);
}

export async function fetchPlanningDowntime(company: string, from?: string, to?: string): Promise<PlanningDowntime[]> {
  const params = new URLSearchParams({ company });
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const res = await fetch(getApiUrl(`/api/planning/execution/downtime?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeDowntime);
}

export async function savePlanningDowntime(company: string, entries: PlanningDowntime[]): Promise<void> {
  const res = await fetch(getApiUrl("/api/planning/execution/downtime"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      companyName: company,
      entries: entries.map((e) => ({
        downtimeId: e.downtimeId ?? 0,
        companyName: company,
        planDate: e.planDate,
        lineNo: e.lineNo,
        shift: e.shift ?? null,
        reason: e.reason,
        capacityFactor: e.capacityFactor,
      })),
    }),
  });
  await parseJson(res);
}

export async function deletePlanningDowntime(downtimeId: number): Promise<void> {
  const res = await fetch(getApiUrl(`/api/planning/execution/downtime/${downtimeId}`), { method: "DELETE" });
  await parseJson(res);
}

export async function fetchLoomPreferenceChart(company: string): Promise<PlanningLoomPreferenceChart[]> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/loom-preference?${params.toString()}`));
  const data = await parseJson<Array<Record<string, unknown>>>(res);
  return data.map(normalizeLoomPreferenceChart);
}

export async function saveLoomPreferenceChart(company: string, rows: PlanningLoomPreferenceChart[]): Promise<void> {
  const res = await fetch(getApiUrl("/api/planning/setup/loom-preference"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      companyName: company,
      rows: rows.map((r) => ({
        chartId: r.chartId ?? 0,
        companyName: company,
        fabricForm: r.fabricForm,
        gsmMin: r.gsmMin,
        gsmMax: r.gsmMax,
        widthMinCm: r.widthMinCm,
        widthMaxCm: r.widthMaxCm,
        preferenceRank: r.preferenceRank,
        loomType: r.loomType,
        winderCategory: r.winderCategory,
        changeoverTier: r.changeoverTier,
        notes: r.notes ?? null,
      })),
    }),
  });
  await parseJson(res);
}

export async function fetchInterUnitDefaults(company: string): Promise<PlanningInterUnitDefaults> {
  const params = new URLSearchParams({ company });
  const res = await fetch(getApiUrl(`/api/planning/setup/inter-unit/defaults?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeInterUnitDefaults(data);
}

export async function saveInterUnitDefaults(
  payload: PlanningInterUnitDefaults,
): Promise<PlanningInterUnitDefaults> {
  const res = await fetch(getApiUrl("/api/planning/setup/inter-unit/defaults"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      fibcCompanyName: payload.fibcCompanyName,
      defaultFabricSupplyCompany: payload.defaultFabricSupplyCompany,
      defaultTransferBufferDays: payload.defaultTransferBufferDays,
      autoDetectSulzerFabric: payload.autoDetectSulzerFabric,
      notes: payload.notes,
    }),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeInterUnitDefaults(data);
}

export async function resolveOrderPlanningRoute(orderNo: string): Promise<PlanningOrderRoute> {
  const params = new URLSearchParams({ orderNo });
  const res = await fetch(getApiUrl(`/api/planning/setup/order-routes/resolve?${params.toString()}`));
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeOrderRoute(data);
}

export async function saveOrderPlanningRoute(route: {
  orderNo: string;
  fibcCompanyName: string;
  fabricSupplyCompanyName: string;
  transferBufferDays?: number;
  isInterUnit?: boolean;
}): Promise<PlanningOrderRoute> {
  const res = await fetch(getApiUrl("/api/planning/setup/order-routes"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(route),
  });
  const data = await parseJson<Record<string, unknown>>(res);
  return normalizeOrderRoute(data);
}
