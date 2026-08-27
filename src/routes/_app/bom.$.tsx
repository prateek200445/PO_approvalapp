import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeft,
  FileText,
  Loader2,
  Mail,
  Package,
  Ruler,
  Send,
} from "lucide-react";
import { toast } from "sonner";
import { BomFieldLabel, BomPageShell, BomPanel, BomStat } from "@/components/bom/bom-ui";
import { PdfJsViewer } from "@/components/PdfJsViewer";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  bomPdfUrl,
  bomPreviewPdfUrl,
  fetchBomCustomer,
  fetchBomDetail,
  fetchBomPreviewDetail,
  sendBomEmail,
  waitForBomEmailResult,
} from "@/lib/bom-api";
import { formatBomDate, formatDimension, type BomDetailResult } from "@/lib/bom-types";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/bom/$")({
  validateSearch: (search: Record<string, unknown>) => ({
    previewId: typeof search.previewId === "string" ? search.previewId : "",
  }),
  head: ({ params }) => ({
    meta: [{ title: `${decodeBomQtnParam(params._splat)} — BOM` }],
  }),
  component: BomDetailPage,
});

function decodeBomQtnParam(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

function MetaItem({ label, value }: { label: string; value?: string | number | null }) {
  if (value == null || value === "" || value === "—") return null;
  return (
    <div className="rounded-lg border border-border/50 bg-muted/15 px-3 py-2">
      <dt className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="mt-0.5 text-sm font-medium leading-snug">{value}</dd>
    </div>
  );
}

function SummaryGroup({
  title,
  icon: Icon,
  children,
}: {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
        <Icon className="h-3.5 w-3.5" />
        {title}
      </div>
      <div className="grid gap-2">{children}</div>
    </div>
  );
}

function BomDetailPage() {
  const { _splat } = Route.useParams();
  const { previewId } = Route.useSearch();
  const qtnNo = decodeBomQtnParam(_splat);
  const isPreviewSession = previewId.trim().length > 0;

  const { data, isLoading, error } = useQuery({
    queryKey: ["bom-detail", isPreviewSession ? previewId : qtnNo],
    queryFn: () => (isPreviewSession ? fetchBomPreviewDetail(previewId) : fetchBomDetail(qtnNo)),
    staleTime: 60_000,
  });

  if (isLoading) {
    return (
      <BomPageShell>
        <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
          <p className="text-sm">Loading BOM…</p>
        </div>
      </BomPageShell>
    );
  }

  if (error || !data) {
    return (
      <BomPageShell>
        <Link
          to="/bom"
          className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to report
        </Link>
        <div className="rounded-2xl border border-destructive/30 bg-destructive/5 p-6 text-center">
          <p className="font-medium text-destructive">{error instanceof Error ? error.message : "BOM not found."}</p>
          <Button variant="outline" className="mt-4" asChild>
            <Link to="/bom">Return to list</Link>
          </Button>
        </div>
      </BomPageShell>
    );
  }

  return <BomDetailContent qtnNo={qtnNo} data={data} previewId={isPreviewSession ? previewId : ""} />;
}

function BomDetailContent({ qtnNo, data, previewId = "" }: { qtnNo: string; data: BomDetailResult; previewId?: string }) {
  const isPreviewSession = previewId.trim().length > 0;
  const pdfUrl = isPreviewSession ? bomPreviewPdfUrl(previewId) : bomPdfUrl(qtnNo);
  const { header, lines } = data;

  const defaultSubject = useMemo(
    () => `BOM - ${header.qtnNo} - ${header.partyName}`,
    [header.qtnNo, header.partyName],
  );
  const defaultBody = "Please find attached Bill of Material (BOM) PDF.";

  const [to, setTo] = useState("");
  const [cc, setCc] = useState("");
  const [bcc, setBcc] = useState("");
  const [subject, setSubject] = useState(defaultSubject);
  const [body, setBody] = useState(defaultBody);
  const [sending, setSending] = useState(false);
  const [customerEmailsLoaded, setCustomerEmailsLoaded] = useState(false);

  useEffect(() => {
    setSubject(defaultSubject);
  }, [defaultSubject]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const customer = await fetchBomCustomer(header.partyName);
        if (cancelled || !customer) return;
        const suggested = [customer.email, customer.email1, customer.email2]
          .map((e) => e?.trim())
          .filter(Boolean)
          .join("; ");
        if (suggested) setTo((current) => current || suggested);
      } catch {
        // optional
      } finally {
        if (!cancelled) setCustomerEmailsLoaded(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [header.partyName]);

  async function handleSendEmail(e: React.FormEvent) {
    e.preventDefault();
    if (!to.trim()) {
      toast.error("Enter at least one recipient email in To.");
      return;
    }

    setSending(true);
    try {
      const { jobId } = await sendBomEmail({
        filePoNo: qtnNo,
        to: to.trim(),
        cc: cc.trim() || undefined,
        bcc: bcc.trim() || undefined,
        subject: subject.trim() || undefined,
        body: body.trim() || undefined,
      });

      if (jobId) {
        toast.message("Sending BOM email…", { description: "Generating PDF and delivering via SMTP." });
        const result = await waitForBomEmailResult(jobId);
        if (result?.state === "sent") {
          toast.success("BOM email sent successfully.");
          return;
        }
        if (result?.state === "failed") {
          toast.error(result.error || "BOM email failed on the server.");
          return;
        }
        toast.message("BOM email is still processing.", {
          description: "It may arrive in your inbox shortly. Check spam if nothing in 2–3 minutes.",
        });
        return;
      }

      toast.success("BOM email is being sent. It may take a minute to arrive.");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to send email.");
    } finally {
      setSending(false);
    }
  }

  const safeFileName = header.qtnNo.replace(/[/\\?%*:|"<>]/g, "-");
  const cleanInstruction = header.instruction?.replace(/<\/?b>/g, "").replace(/<>/g, "") ?? "";

  return (
    <BomPageShell>
      <div className="sticky top-0 z-20 -mx-4 mb-5 border-b border-border/60 bg-background/85 px-4 py-3 backdrop-blur-md md:-mx-6 md:px-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-3">
            <Button variant="ghost" size="sm" className="shrink-0" asChild>
              <Link to="/bom">
                <ArrowLeft className="h-4 w-4" />
                Back
              </Link>
            </Button>
            {!isPreviewSession ? (
              <Button variant="outline" size="sm" className="shrink-0" asChild>
                <Link to="/bom/create" search={{ filePoNo: qtnNo }}>
                  Open editor
                </Link>
              </Button>
            ) : null}
            <div className="min-w-0 border-l border-border/60 pl-3">
              <p className="truncate font-mono text-sm font-semibold">{header.qtnNo}</p>
              <p className="truncate text-xs text-muted-foreground">
                {header.partyName}
                {isPreviewSession ? " · Preview session" : ""}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* PDF + compact summary sidebar */}
      <div className="mb-5 grid gap-5 lg:grid-cols-[1fr_280px]">
        <BomPanel
          title="PDF preview"
          subtitle={isPreviewSession ? "Generated from current unsaved preview" : "Generated from ERP BOM data"}
          headerRight={
            <Badge variant="secondary" className="font-normal">
              QuestPDF
            </Badge>
          }
        >
          <PdfJsViewer
            pdfUrl={pdfUrl}
            downloadFileName={`${safeFileName}.pdf`}
            minHeightClass="min-h-[400px] max-h-[min(80vh,980px)]"
          />
        </BomPanel>

        <BomPanel title="Quick summary" className="h-fit lg:sticky lg:top-[4.5rem]">
          <div className="space-y-4 p-4">
            <div className="grid grid-cols-2 gap-2">
              <BomStat label="Date" value={formatBomDate(header.date)} />
              <BomStat label="User" value={header.user || "—"} />
            </div>
            <SummaryGroup title="Bag" icon={Package}>
              <BomStat label="Bag type" value={header.bagType || "—"} />
              <BomStat label="SWL / Qty" value={
                [header.swl, header.qty && `${header.qty}${header.qtyUnit ? ` ${header.qtyUnit}` : ""}`]
                  .filter(Boolean)
                  .join(" · ") || "—"
              } />
            </SummaryGroup>
            <SummaryGroup title="Size" icon={Ruler}>
              <BomStat
                label="L × W × H"
                value={`${formatDimension(header.sizeL, header.sizeW, header.sizeH)}${header.sizeType ? ` ${header.sizeType}` : ""}`}
              />
            </SummaryGroup>
            <SummaryGroup title="Refs" icon={FileText}>
              <BomStat label="Ref no." value={header.refNo || "—"} />
              <BomStat label="PO no." value={header.poNos || header.poNo || "—"} />
            </SummaryGroup>
          </div>
        </BomPanel>
      </div>

      {/* Email — full width, original layout */}
      {!isPreviewSession ? (
      <BomPanel className="mb-5">
        <div className="flex items-center gap-2 border-b border-border/60 px-4 py-3.5 md:px-5">
          <Mail className="h-5 w-5 text-amber-600" />
          <div>
            <h2 className="text-sm font-semibold">Email BOM</h2>
            <p className="text-xs text-muted-foreground">
              Sends QuestPDF attachment. Separate multiple emails with comma or semicolon.
            </p>
          </div>
        </div>
        <form onSubmit={(e) => void handleSendEmail(e)} className="grid gap-4 p-4 md:grid-cols-2 md:p-5">
          <label className="space-y-1.5 md:col-span-2">
            <BomFieldLabel>To *</BomFieldLabel>
            <Input
              value={to}
              onChange={(e) => setTo(e.target.value)}
              placeholder="customer@example.com; another@example.com"
              required
            />
            {!customerEmailsLoaded ? (
              <span className="text-xs text-muted-foreground">Loading customer emails…</span>
            ) : null}
          </label>

          <label className="space-y-1.5">
            <BomFieldLabel>Cc</BomFieldLabel>
            <Input value={cc} onChange={(e) => setCc(e.target.value)} />
          </label>

          <label className="space-y-1.5">
            <BomFieldLabel>Bcc</BomFieldLabel>
            <Input value={bcc} onChange={(e) => setBcc(e.target.value)} />
          </label>

          <label className="space-y-1.5 md:col-span-2">
            <BomFieldLabel>Subject</BomFieldLabel>
            <Input value={subject} onChange={(e) => setSubject(e.target.value)} />
          </label>

          <label className="space-y-1.5 md:col-span-2">
            <BomFieldLabel>Message</BomFieldLabel>
            <Textarea value={body} onChange={(e) => setBody(e.target.value)} rows={4} className="resize-y" />
          </label>

          <div className="md:col-span-2">
            <Button type="submit" disabled={sending}>
              {sending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
              Send email with PDF
            </Button>
          </div>
        </form>
      </BomPanel>
      ) : null}

      {/* Full BOM summary — restored grid layout */}
      <BomPanel title="BOM summary" className="mb-5">
        <dl className="grid gap-3 p-4 text-sm sm:grid-cols-2 lg:grid-cols-4 md:p-5">
          <MetaItem label="Date" value={formatBomDate(header.date)} />
          <MetaItem label="User" value={header.user} />
          <MetaItem label="Ref no." value={header.refNo} />
          <MetaItem label="Marketing inv." value={header.marketingInvNo} />
          <MetaItem
            label="Dimensions (L × W × H)"
            value={`${formatDimension(header.sizeL, header.sizeW, header.sizeH)}${header.sizeType ? ` ${header.sizeType}` : ""}`}
          />
          <MetaItem
            label="SWL / Qty"
            value={[header.swl, header.qty && `${header.qty}${header.qtyUnit ? ` ${header.qtyUnit}` : ""}`]
              .filter(Boolean)
              .join(" · ")}
          />
          <MetaItem label="SF" value={header.sfRatio} />
          <MetaItem label="Print type" value={header.printType} />
          <MetaItem label="Bag type" value={header.bagType} />
          <MetaItem label="Fabric color" value={header.fabColor} />
          <MetaItem label="Top / FS" value={header.topSpoutType} />
          <MetaItem label="Bottom / DS" value={header.bottomType} />
          <MetaItem label="Loop" value={header.loopSpec} />
          <MetaItem label="Liner" value={header.linerSpec} />
          <MetaItem label="Doc pouch" value={header.doc !== "N/A" ? header.doc : undefined} />
          <MetaItem label="Total kg / bag" value={header.totalKg ?? undefined} />
          <MetaItem label="PO no." value={header.poNos || header.poNo} />
          <MetaItem label="Drop loop" value={header.isDropLoop} />
          <MetaItem label="RP fabric" value={header.rpFabric} />
          <MetaItem label="Knot type" value={header.knotType} />
        </dl>

        {header.printingRemarks ? (
          <p className="mx-4 mb-4 rounded-lg bg-muted/40 p-3 text-sm md:mx-5">
            <span className="font-medium">Printing remarks: </span>
            {header.printingRemarks}
          </p>
        ) : null}

        {cleanInstruction ? (
          <div className="mx-4 mb-5 rounded-lg border border-border/60 bg-muted/20 p-4 md:mx-5">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Instructions</p>
            <p className="whitespace-pre-wrap text-sm leading-relaxed text-foreground/90">{cleanInstruction}</p>
          </div>
        ) : null}
      </BomPanel>

      {/* Components — improved table, full width */}
      <BomPanel
        title="Components"
        subtitle={`${lines.length} component(s) · order matches ERP save sequence`}
      >
        <div className="max-h-[70vh] overflow-auto">
          <table className="min-w-full border-separate border-spacing-0 text-sm">
            <thead className="sticky top-0 z-10">
              <tr className="bg-muted/80 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur-sm">
                <th className="border-b border-border/60 px-4 py-3">#</th>
                <th className="border-b border-border/60 px-4 py-3">Component</th>
                <th className="border-b border-border/60 px-3 py-3">GSM</th>
                <th className="border-b border-border/60 px-3 py-3">Lami</th>
                <th className="border-b border-border/60 px-3 py-3">Color</th>
                <th className="border-b border-border/60 px-3 py-3">Fabric</th>
                <th className="border-b border-border/60 px-3 py-3">Cut size</th>
                <th className="border-b border-border/60 px-3 py-3 text-right">Order mtr</th>
                <th className="border-b border-border/60 px-3 py-3 text-right">Kg / bag</th>
                <th className="border-b border-border/60 px-3 py-3">GPM</th>
                <th className="border-b border-border/60 px-4 py-3 min-w-[200px]">Remarks</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line, index) => (
                <tr
                  key={`${line.sortOrder}-${line.heading}`}
                  className={cn(
                    "transition-colors hover:bg-amber-500/[0.04]",
                    index % 2 === 1 && "bg-muted/10",
                  )}
                >
                  <td className="border-b border-border/40 px-4 py-2.5 tabular-nums text-muted-foreground">
                    {index + 1}
                  </td>
                  <td className="border-b border-border/40 px-4 py-2.5 font-medium text-foreground">
                    {line.heading}
                  </td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.gsm || "—"}</td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.lami || "—"}</td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.color || "—"}</td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.fabricSize || "—"}</td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.cutSize || "—"}</td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-right tabular-nums">
                    {line.totalMtr ?? "—"}
                  </td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-right tabular-nums">
                    {line.totalKg ?? "—"}
                  </td>
                  <td className="border-b border-border/40 px-3 py-2.5 text-muted-foreground">{line.gpm || "—"}</td>
                  <td className="border-b border-border/40 px-4 py-2.5 text-muted-foreground">{line.remarks || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </BomPanel>
    </BomPageShell>
  );
}
