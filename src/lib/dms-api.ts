import { getApiUrl } from "@/lib/api-config";
import type { DmsAttachmentListResponse } from "@/lib/dms-types";

export function dmsAttachmentsUrl(purchaseCode: string): string {
  return getApiUrl(
    `/api/dms/attachments?poNo=${encodeURIComponent(purchaseCode)}`,
  );
}

export function dmsFileDownloadUrl(fileId: number): string {
  return getApiUrl(`/api/dms/files/${fileId}`);
}

export async function downloadDmsFile(
  fileId: number,
  fileName: string,
): Promise<void> {
  const response = await fetch(dmsFileDownloadUrl(fileId));
  if (!response.ok) {
    let message = "Could not download attachment.";
    try {
      const body = await response.json();
      if (body?.error) message = body.error;
    } catch {
      // ignore non-json error bodies
    }
    throw new Error(message);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName || `attachment-${fileId}`;
  link.rel = "noopener";
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export async function fetchDmsAttachments(
  purchaseCode: string,
): Promise<DmsAttachmentListResponse> {
  const response = await fetch(dmsAttachmentsUrl(purchaseCode));
  if (!response.ok) {
    throw new Error("Failed to load attachments");
  }
  return response.json();
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
