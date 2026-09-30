import { getApiUrl } from "@/lib/api-config";

export const PRODUCT_INQUIRIES = [
  "Shade Net",
  "Insect Net",
  "Weed Control / Weed Barrier Fabric",
  "Greenhouse Skirting / Apron",
  "Greenhouse Cladding Film / Polyfilm",
  "Planter Bags / Grow Bags",
] as const;

export type ExhibitionFormType = "DealerDistributor" | "ProductInquiry";

export interface ExhibitionLead {
  id: number;
  formType: ExhibitionFormType;
  companyName: string | null;
  personName: string;
  contactNumber: string;
  email: string | null;
  address: string | null;
  postalCode: string | null;
  productInquiry: string | null;
  quantity: number | null;
  exhibitionName: string | null;
  createdAt: string;
}

export interface ExhibitionLeadCreate {
  formType: ExhibitionFormType;
  companyName?: string | null;
  personName: string;
  contactNumber: string;
  email?: string | null;
  address?: string | null;
  postalCode?: string | null;
  productInquiry?: string | null;
  quantity?: number | null;
}

function asString(value: unknown): string {
  return value == null ? "" : String(value);
}

function asNullable(value: unknown): string | null {
  const text = asString(value).trim();
  return text.length === 0 ? null : text;
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

export async function createExhibitionLead(body: ExhibitionLeadCreate): Promise<{ id: number; createdAt: string }> {
  const response = await fetch(getApiUrl("/api/ExhibitionLeads"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw new Error(await readError(response, "Could not submit the form."));
  const payload = (await response.json()) as { id?: number; createdAt?: string };
  return { id: Number(payload.id ?? 0), createdAt: asString(payload.createdAt) };
}

export async function listExhibitionLeads(formType: ExhibitionFormType, username: string): Promise<ExhibitionLead[]> {
  const params = new URLSearchParams({ formType, username });
  const response = await fetch(getApiUrl(`/api/ExhibitionLeads?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not load submissions."));
  const payload = (await response.json()) as unknown[];
  return payload.map((row) => {
    const item = row as Record<string, unknown>;
    const quantity = item.quantity ?? item.Quantity;
    return {
      id: Number(item.id ?? item.Id ?? 0),
      formType: asString(item.formType ?? item.FormType) as ExhibitionFormType,
      companyName: asNullable(item.companyName ?? item.CompanyName),
      personName: asString(item.personName ?? item.PersonName),
      contactNumber: asString(item.contactNumber ?? item.ContactNumber),
      email: asNullable(item.email ?? item.Email),
      address: asNullable(item.address ?? item.Address),
      postalCode: asNullable(item.postalCode ?? item.PostalCode),
      productInquiry: asNullable(item.productInquiry ?? item.ProductInquiry),
      quantity: quantity == null || quantity === "" ? null : Number(quantity),
      exhibitionName: asNullable(item.exhibitionName ?? item.ExhibitionName),
      createdAt: asString(item.createdAt ?? item.CreatedAt),
    };
  });
}

export async function downloadExhibitionLeadsExcel(formType: ExhibitionFormType, username: string): Promise<void> {
  const params = new URLSearchParams({ formType, username });
  const response = await fetch(getApiUrl(`/api/ExhibitionLeads/excel?${params}`));
  if (!response.ok) throw new Error(await readError(response, "Could not download Excel."));
  const blob = await response.blob();
  const fileName =
    response.headers.get("content-disposition")?.match(/filename="?([^"]+)"?/i)?.[1] ||
    `exhibition-leads-${formType}.xlsx`;
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export function formatLeadTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
