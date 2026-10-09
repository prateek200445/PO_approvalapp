export type PlantBuyer = {
  name: string;
  orders: { orderNo: string; invoices: string[] }[];
};

export type PlantMaterial = {
  quality: string;
  grade: string;
  itemCode: string;
  stock: number;
};

export type PlantLine = {
  key: string;
  quality: string;
  grade: string;
  itemCode: string;
  stock: number;
  qty: string;
  percent: string;
  ppBags: string;
};

export type PlantRecent = {
  groupSrNo: number;
  entryDate: string;
  shift: string;
  product: string;
  productionType: string;
  consumption: number;
  wastage: number;
  netProduction: number;
  buyer: string;
  hod: string;
  excise: string;
};

async function read<T>(res: Response): Promise<T> {
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((data as { message?: string }).message || "Request failed");
  return data as T;
}

export function getPlantCompanies() {
  return fetch("/api/plant-consumption/companies").then((r) => read<{ srNo: number; name: string }[]>(r));
}

export function getPlantLookups(company: string) {
  const q = new URLSearchParams({ company });
  return fetch(`/api/plant-consumption/lookups?${q}`).then((r) =>
    read<{ plants: string[]; sectors: string[]; buyers: PlantBuyer[] }>(r),
  );
}

export function getPlantSetup(company: string, plant: string, date: string) {
  const q = new URLSearchParams({ company, plant, date });
  return fetch(`/api/plant-consumption/plant?${q}`).then((r) =>
    read<{ subs: string[]; fromWarehouses: string[]; toWarehouses: string[]; products: string[] }>(r),
  );
}

export function getPlantMaterials(company: string, warehouse: string, date: string) {
  const q = new URLSearchParams({ company, warehouse, date });
  return fetch(`/api/plant-consumption/materials?${q}`).then((r) => read<PlantMaterial[]>(r));
}

export function getPlantRolls(company: string, plant: string, date: string, shift: string) {
  const q = new URLSearchParams({ company, plant, date, shift });
  return fetch(`/api/plant-consumption/rolls?${q}`).then((r) => read<{ rolls: number }>(r));
}

export function getPlantRecent(company: string, plant: string, date: string) {
  const q = new URLSearchParams({ company, plant, date });
  return fetch(`/api/plant-consumption/recent?${q}`).then((r) => read<PlantRecent[]>(r));
}

export function savePlantConsumption(body: unknown) {
  return fetch("/api/plant-consumption/save", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ groupSrNo: number }>(r));
}

export function deletePlantConsumption(body: unknown) {
  return fetch("/api/plant-consumption/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then((r) => read<{ ok: boolean }>(r));
}

export function loadPlantEntry(company: string, groupSrNo: number) {
  const q = new URLSearchParams({ company, groupSrNo: String(groupSrNo) });
  return fetch(`/api/plant-consumption/entry?${q}`).then((r) =>
    read<{
      header: {
        entryDate: string;
        plant: string;
        plantSub: string;
        product: string;
        shift: string;
        sector: string;
        timeIn: string;
        timeOut: string;
        buyer: string;
        marketingInvoice: string;
        buyerOrderDate: string;
        productionType: string;
        wastage: number;
        trimWastage: number;
        sweepingWastage: number;
        fabricTrim: number;
        fabricWaste: number;
        lumpsWastage: number;
      };
      lines: { quality: string; grade: string; qty: number; itemCode: string }[];
    }>(r),
  );
}
