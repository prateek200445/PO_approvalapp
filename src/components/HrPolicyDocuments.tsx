import { useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BookOpen, Download, ExternalLink, FileText, Loader2, Network, ScrollText, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  HR_DOCUMENT_CATEGORIES,
  deleteHrPolicyDocument,
  hrPolicyDocumentUrl,
  listHrPolicyDocuments,
  uploadHrPolicyDocument,
  type HrDocumentCategory,
  type HrPolicyDocument,
} from "@/lib/hr-engagement-api";
import { getHrCompanies } from "@/lib/hr-reports-api";
import { cn } from "@/lib/utils";

const CATEGORY_STYLE: Record<HrDocumentCategory, { icon: typeof BookOpen; accent: string; badge: string; hint: string }> = {
  "HR Policies": {
    icon: BookOpen,
    accent: "border-t-blue-500",
    badge: "bg-blue-100 text-blue-900 dark:bg-blue-500/20 dark:text-blue-100",
    hint: "Leave, attendance, travel, conduct-at-work and other HR policies",
  },
  "Code of Conduct": {
    icon: ScrollText,
    accent: "border-t-emerald-500",
    badge: "bg-emerald-100 text-emerald-900 dark:bg-emerald-500/20 dark:text-emerald-100",
    hint: "Expected behaviour, ethics and disciplinary rules",
  },
  Organogram: {
    icon: Network,
    accent: "border-t-violet-500",
    badge: "bg-violet-100 text-violet-900 dark:bg-violet-500/20 dark:text-violet-100",
    hint: "Organisation structure, reporting lines and designations",
  },
};

function fileSize(bytes: number) {
  if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  return `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

function fmtDate(iso: string) {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? iso
    : d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}

export function HrPolicyDocuments({ username }: { username: string }) {
  const queryClient = useQueryClient();
  const listQuery = useQuery({
    queryKey: ["hr-policy-documents", username],
    queryFn: () => listHrPolicyDocuments(username),
    staleTime: 5 * 60_000,
  });
  const canManage = listQuery.data?.canManage === true;

  const removeMutation = useMutation({
    mutationFn: (docId: number) => deleteHrPolicyDocument(username, docId),
    onSuccess: () => {
      toast.success("Document removed");
      void queryClient.invalidateQueries({ queryKey: ["hr-policy-documents"] });
    },
    onError: (e) => toast.error(e instanceof Error ? e.message : "Could not remove the document"),
  });

  const byCategory = useMemo(() => {
    const map = new Map<HrDocumentCategory, HrPolicyDocument[]>();
    for (const c of HR_DOCUMENT_CATEGORIES) map.set(c, []);
    for (const d of listQuery.data?.documents ?? []) map.get(d.category)?.push(d);
    return map;
  }, [listQuery.data?.documents]);

  return (
    <div className="space-y-5">
      {canManage ? <UploadForm username={username} /> : null}

      {listQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading documents…
        </div>
      ) : listQuery.isError ? (
        <p className="text-sm text-destructive">
          {listQuery.error instanceof Error ? listQuery.error.message : "Failed to load documents"}
        </p>
      ) : (
        <div className="grid gap-4 lg:grid-cols-3">
          {HR_DOCUMENT_CATEGORIES.map((category) => {
            const style = CATEGORY_STYLE[category];
            const Icon = style.icon;
            const docs = byCategory.get(category) ?? [];
            return (
              <section
                key={category}
                className={cn("rounded-xl border border-t-4 border-border bg-card p-4 shadow-sm", style.accent)}
              >
                <div className="flex items-center gap-2">
                  <Icon className="h-5 w-5 text-muted-foreground" />
                  <h2 className="text-lg font-semibold">{category}</h2>
                  <span className={cn("ml-auto rounded-full px-2 py-0.5 text-xs font-semibold", style.badge)}>
                    {docs.length}
                  </span>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">{style.hint}</p>
                {docs.length === 0 ? (
                  <p className="mt-4 rounded-lg border border-dashed border-border px-3 py-6 text-center text-sm text-muted-foreground">
                    {canManage ? "Nothing uploaded yet — use the form above." : "Not published yet."}
                  </p>
                ) : (
                  <ul className="mt-3 space-y-2">
                    {docs.map((d) => (
                      <li key={d.docId} className="rounded-lg border border-border bg-background px-3 py-2.5">
                        <div className="flex items-start gap-2">
                          <FileText className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                          <div className="min-w-0 flex-1">
                            <a
                              href={hrPolicyDocumentUrl(d.docId, username)}
                              target="_blank"
                              rel="noreferrer"
                              className="font-medium hover:underline"
                            >
                              {d.title}
                            </a>
                            {d.description ? (
                              <p className="mt-0.5 text-xs text-muted-foreground">{d.description}</p>
                            ) : null}
                            <p className="mt-1 text-[11px] text-muted-foreground">
                              {d.company ?? "All companies"} · {fileSize(d.fileSize)} · {fmtDate(d.uploadedAt)}
                              {canManage && d.uploadedBy ? ` · ${d.uploadedBy}` : ""}
                            </p>
                          </div>
                        </div>
                        <div className="mt-2 flex flex-wrap gap-1.5">
                          <Button asChild size="sm" variant="outline" className="h-7 px-2 text-xs">
                            <a href={hrPolicyDocumentUrl(d.docId, username)} target="_blank" rel="noreferrer">
                              <ExternalLink className="h-3.5 w-3.5" /> Open
                            </a>
                          </Button>
                          <Button asChild size="sm" variant="outline" className="h-7 px-2 text-xs">
                            <a href={hrPolicyDocumentUrl(d.docId, username, true)}>
                              <Download className="h-3.5 w-3.5" /> Download
                            </a>
                          </Button>
                          {canManage ? (
                            <Button
                              type="button"
                              size="sm"
                              variant="ghost"
                              className="h-7 px-2 text-xs text-destructive hover:text-destructive"
                              disabled={removeMutation.isPending}
                              onClick={() => {
                                if (window.confirm(`Remove "${d.title}"?`)) removeMutation.mutate(d.docId);
                              }}
                            >
                              <Trash2 className="h-3.5 w-3.5" /> Remove
                            </Button>
                          ) : null}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}

function UploadForm({ username }: { username: string }) {
  const queryClient = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const [category, setCategory] = useState<HrDocumentCategory>("HR Policies");
  const [title, setTitle] = useState("");
  const [company, setCompany] = useState("");
  const [description, setDescription] = useState("");
  const [file, setFile] = useState<File | null>(null);

  const companiesQuery = useQuery({
    queryKey: ["hr-companies", username],
    queryFn: () => getHrCompanies(username),
    staleTime: 60 * 60_000,
  });

  const upload = useMutation({
    mutationFn: () =>
      uploadHrPolicyDocument(username, {
        file: file!,
        category,
        title: title.trim(),
        company: company || null,
        description,
      }),
    onSuccess: (doc) => {
      toast.success(`Uploaded "${doc.title}"`);
      setTitle("");
      setDescription("");
      setFile(null);
      if (fileRef.current) fileRef.current.value = "";
      void queryClient.invalidateQueries({ queryKey: ["hr-policy-documents"] });
    },
    onError: (e) => toast.error(e instanceof Error ? e.message : "Upload failed"),
  });

  const selectClass =
    "h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

  return (
    <form
      className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm"
      onSubmit={(e) => {
        e.preventDefault();
        if (!file) return toast.error("Choose a file to upload");
        if (title.trim().length < 3) return toast.error("Enter a title");
        upload.mutate();
      }}
    >
      <div className="flex items-center gap-2">
        <Upload className="h-5 w-5 text-primary" />
        <h2 className="text-lg font-semibold">Upload a document</h2>
        <span className="text-xs text-muted-foreground">PDF, image, Word, Excel or PowerPoint · up to 20 MB</span>
      </div>
      <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4">
        <div className="space-y-1.5">
          <Label htmlFor="hr-doc-category">Category</Label>
          <select
            id="hr-doc-category"
            className={selectClass}
            value={category}
            onChange={(e) => setCategory(e.target.value as HrDocumentCategory)}
          >
            {HR_DOCUMENT_CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="hr-doc-title">Title</Label>
          <Input
            id="hr-doc-title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Leave Policy 2026"
            maxLength={200}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="hr-doc-company">Applies to</Label>
          <select id="hr-doc-company" className={selectClass} value={company} onChange={(e) => setCompany(e.target.value)}>
            <option value="">All companies</option>
            {(companiesQuery.data ?? []).map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="hr-doc-file">File</Label>
          <Input
            id="hr-doc-file"
            ref={fileRef}
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,.docx,.xlsx,.pptx"
            onChange={(e) => {
              const f = e.target.files?.[0] ?? null;
              setFile(f);
              if (f && !title.trim()) setTitle(f.name.replace(/\.[^.]+$/, ""));
            }}
          />
        </div>
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="hr-doc-description">Note (optional)</Label>
        <Textarea
          id="hr-doc-description"
          rows={2}
          maxLength={500}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Effective date, version, what changed…"
        />
      </div>
      <div className="flex justify-end">
        <Button type="submit" disabled={upload.isPending}>
          {upload.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
          Upload
        </Button>
      </div>
    </form>
  );
}
