import { getApiUrl } from "@/lib/api-config";

export type HrCelebrationPerson = {
  empCode: string;
  name: string;
  designation: string | null;
  department: string | null;
  branch: string | null;
  /** Completed years, for work anniversaries. */
  years: number | null;
  isYou: boolean;
};

export type HrCelebrations = {
  date: string;
  company: string;
  empCode: string | null;
  birthdays: HrCelebrationPerson[];
  anniversaries: HrCelebrationPerson[];
};

export const HR_DOCUMENT_CATEGORIES = ["HR Policies", "Code of Conduct", "Organogram"] as const;
export type HrDocumentCategory = (typeof HR_DOCUMENT_CATEGORIES)[number];

export type HrPolicyDocument = {
  docId: number;
  category: HrDocumentCategory;
  title: string;
  /** Null = applies to all companies. */
  company: string | null;
  description: string | null;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedBy: string | null;
  uploadedAt: string;
};

export type HrPolicyDocumentList = {
  canManage: boolean;
  company: string | null;
  documents: HrPolicyDocument[];
};

async function readError(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as { message?: string };
    if (data?.message) return data.message;
  } catch {
    /* ignore */
  }
  return `Request failed (${res.status})`;
}

export async function listHrPolicyDocuments(username: string): Promise<HrPolicyDocumentList> {
  const res = await fetch(getApiUrl(`/api/hr/engagement/documents?${new URLSearchParams({ username })}`));
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as HrPolicyDocumentList;
}

export function hrPolicyDocumentUrl(docId: number, username: string, download = false): string {
  const params = new URLSearchParams({ username });
  if (download) params.set("download", "true");
  return getApiUrl(`/api/hr/engagement/documents/${docId}/file?${params}`);
}

export async function uploadHrPolicyDocument(
  username: string,
  input: { file: File; category: HrDocumentCategory; title: string; company: string | null; description: string },
): Promise<HrPolicyDocument> {
  const form = new FormData();
  form.append("file", input.file);
  form.append("category", input.category);
  form.append("title", input.title);
  if (input.company) form.append("company", input.company);
  if (input.description.trim()) form.append("description", input.description.trim());
  const res = await fetch(getApiUrl(`/api/hr/engagement/documents?${new URLSearchParams({ username })}`), {
    method: "POST",
    body: form,
  });
  if (!res.ok) throw new Error(await readError(res));
  return (await res.json()) as HrPolicyDocument;
}

export async function deleteHrPolicyDocument(username: string, docId: number): Promise<void> {
  const res = await fetch(getApiUrl(`/api/hr/engagement/documents/${docId}?${new URLSearchParams({ username })}`), {
    method: "DELETE",
  });
  if (!res.ok) throw new Error(await readError(res));
}

export async function getHrCelebrations(username: string): Promise<HrCelebrations> {
  const params = new URLSearchParams({ username });
  const res = await fetch(getApiUrl(`/api/hr/engagement/celebrations?${params}`));
  if (!res.ok) throw new Error(`Celebrations request failed (${res.status})`);
  return (await res.json()) as HrCelebrations;
}
