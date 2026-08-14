import { dmsFileDownloadUrl, fetchDmsAttachments, formatFileSize } from "@/lib/dms-api";
import type { DmsAttachmentDto } from "@/lib/dms-types";
import { useQuery } from "@tanstack/react-query";
import { Download, FileText, Loader2, Paperclip } from "lucide-react";

interface DmsAttachmentsSectionProps {
  purchaseCode: string;
  kind?: "PO" | "WO";
}

export function DmsAttachmentsSection({
  purchaseCode,
  kind = "PO",
}: DmsAttachmentsSectionProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["dms-attachments", purchaseCode],
    queryFn: () => fetchDmsAttachments(purchaseCode),
    staleTime: 1000 * 60 * 10,
  });

  const files = data?.files ?? [];
  const label = kind === "WO" ? "Work order" : "Purchase order";

  return (
    <section className="rounded-xl border border-border bg-card">
      <header className="flex items-center gap-2 border-b border-border px-5 py-3">
        <Paperclip className="h-4 w-4 text-primary" />
        <h2 className="text-sm font-semibold">ERP Attachments</h2>
        {!isLoading && files.length > 0 && (
          <span className="ml-auto rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-semibold text-primary">
            {files.length}
          </span>
        )}
      </header>
      <div className="p-5">
        {isLoading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading attachments…
          </div>
        ) : isError ? (
          <p className="text-sm text-muted-foreground">
            Could not load {label.toLowerCase()} attachments.
          </p>
        ) : files.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No files uploaded in ERP DMS for this {label.toLowerCase()}.
          </p>
        ) : (
          <ul className="space-y-2">
            {files.map((file) => (
              <AttachmentRow key={file.fileId} file={file} />
            ))}
          </ul>
        )}
        {data?.refEntryNo && files.length > 0 && (
          <p className="mt-3 text-[10px] text-muted-foreground/80">
            DMS ref: {data.refType} · {data.refEntryNo}
          </p>
        )}
      </div>
    </section>
  );
}

function AttachmentRow({ file }: { file: DmsAttachmentDto }) {
  const uploaded = file.uploadedOn
    ? new Date(file.uploadedOn).toLocaleString()
    : null;

  return (
    <li className="flex flex-wrap items-center gap-3 rounded-lg border border-border/60 bg-muted/20 px-3 py-2.5">
      <FileText className="h-4 w-4 shrink-0 text-primary/80" />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{file.fileName}</p>
        <p className="text-[11px] text-muted-foreground">
          {formatFileSize(file.fileSize)}
          {file.uploadedBy ? ` · ${file.uploadedBy}` : ""}
          {uploaded ? ` · ${uploaded}` : ""}
        </p>
      </div>
      <a
        href={dmsFileDownloadUrl(file.fileId)}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex h-8 shrink-0 items-center gap-1.5 rounded-md border border-border bg-background px-2.5 text-xs font-medium hover:bg-accent transition"
      >
        <Download className="h-3.5 w-3.5" />
        Download
      </a>
    </li>
  );
}
