import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, Download, ExternalLink, Loader2, Mail, Send } from "lucide-react";
import { toast } from "sonner";
import { bomPdfUrl, fetchBomCustomer, fetchBomDetail, sendBomEmail } from "@/lib/bom-api";
import { formatBomDate, formatDimension, type BomDetailResult } from "@/lib/bom-types";

export const Route = createFileRoute("/_app/bom/$")({
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
    <div>
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  );
}

function BomDetailPage() {
  const { _splat } = Route.useParams();
  const qtnNo = decodeBomQtnParam(_splat);

  const { data, isLoading, error } = useQuery({
    queryKey: ["bom-detail", qtnNo],
    queryFn: () => fetchBomDetail(qtnNo),
    staleTime: 60_000,
  });

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center gap-2 text-muted-foreground">
        <Loader2 className="h-5 w-5 animate-spin" />
        Loading BOM…
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="mx-auto max-w-3xl space-y-4 p-6">
        <Link to="/bom" className="inline-flex items-center gap-2 text-sm text-primary hover:underline">
          <ArrowLeft className="h-4 w-4" />
          Back to report
        </Link>
        <p className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-destructive">
          {error instanceof Error ? error.message : "BOM not found."}
        </p>
      </div>
    );
  }

  return <BomDetailContent qtnNo={qtnNo} data={data} />;
}

function BomDetailContent({ qtnNo, data }: { qtnNo: string; data: BomDetailResult }) {
  const pdfUrl = bomPdfUrl(qtnNo);
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
        // Customer master email is optional
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
      await sendBomEmail({
        filePoNo: qtnNo,
        to: to.trim(),
        cc: cc.trim() || undefined,
        bcc: bcc.trim() || undefined,
        subject: subject.trim() || undefined,
        body: body.trim() || undefined,
      });
      toast.success("BOM email sent with PDF attachment.");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to send email.");
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6 p-4 pb-24 md:p-6 md:pb-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link to="/bom" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to report
        </Link>
        <div className="flex flex-wrap items-center gap-2">
          <a
            href={pdfUrl}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center gap-2 rounded-md border px-3 py-2 text-sm hover:bg-muted"
          >
            <ExternalLink className="h-4 w-4" />
            Open PDF
          </a>
          <a
            href={pdfUrl}
            download={`${qtnNo.replace(/[/\\?%*:|"<>]/g, "-")}.pdf`}
            className="inline-flex items-center gap-2 rounded-md border px-3 py-2 text-sm hover:bg-muted"
          >
            <Download className="h-4 w-4" />
            Download PDF
          </a>
        </div>
      </div>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="border-b px-4 py-3">
          <h1 className="text-lg font-semibold">Bill of Material — {header.qtnNo}</h1>
          <p className="text-sm text-muted-foreground">{header.partyName}</p>
        </div>
        <iframe
          title={`BOM PDF ${header.qtnNo}`}
          src={pdfUrl}
          className="h-[min(80vh,980px)] w-full bg-white"
        />
      </section>

      <section className="rounded-xl border bg-card p-5 shadow-sm">
        <div className="flex items-center gap-2">
          <Mail className="h-5 w-5 text-primary" />
          <div>
            <h2 className="text-base font-semibold">Email BOM</h2>
            <p className="text-xs text-muted-foreground">
              Sends QuestPDF attachment to any address you enter. Separate multiple emails with comma or semicolon.
            </p>
          </div>
        </div>

        <form onSubmit={(e) => void handleSendEmail(e)} className="mt-4 grid gap-4 md:grid-cols-2">
          <label className="space-y-1 text-sm md:col-span-2">
            <span className="font-medium text-muted-foreground">To *</span>
            <input
              type="text"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              placeholder="customer@example.com; another@example.com"
              className="w-full rounded-md border bg-background px-3 py-2"
              required
            />
            {!customerEmailsLoaded ? (
              <span className="text-xs text-muted-foreground">Loading customer emails…</span>
            ) : null}
          </label>

          <label className="space-y-1 text-sm">
            <span className="font-medium text-muted-foreground">Cc</span>
            <input
              type="text"
              value={cc}
              onChange={(e) => setCc(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2"
            />
          </label>

          <label className="space-y-1 text-sm">
            <span className="font-medium text-muted-foreground">Bcc</span>
            <input
              type="text"
              value={bcc}
              onChange={(e) => setBcc(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2"
            />
          </label>

          <label className="space-y-1 text-sm md:col-span-2">
            <span className="font-medium text-muted-foreground">Subject</span>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2"
            />
          </label>

          <label className="space-y-1 text-sm md:col-span-2">
            <span className="font-medium text-muted-foreground">Message</span>
            <textarea
              value={body}
              onChange={(e) => setBody(e.target.value)}
              rows={4}
              className="w-full rounded-md border bg-background px-3 py-2"
            />
          </label>

          <div className="md:col-span-2">
            <button
              type="submit"
              disabled={sending}
              className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
            >
              {sending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
              Send email with PDF
            </button>
          </div>
        </form>
      </section>

      <section className="rounded-xl border bg-card p-5 shadow-sm">
        <h2 className="text-base font-semibold">BOM summary</h2>
        <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 lg:grid-cols-4">
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
          <MetaItem label="Doc pouch" value={header.doc !== "N/A" ? header.doc : ""} />
          <MetaItem label="Total kg / bag" value={header.totalKg ?? undefined} />
          <MetaItem label="PO no." value={header.poNos || header.poNo} />
          <MetaItem label="Drop loop" value={header.isDropLoop} />
          <MetaItem label="RP fabric" value={header.rpFabric} />
          <MetaItem label="Knot type" value={header.knotType} />
        </dl>

        {header.printingRemarks ? (
          <p className="mt-4 rounded-md bg-muted/50 p-3 text-sm">
            <span className="font-medium">Printing remarks: </span>
            {header.printingRemarks}
          </p>
        ) : null}

        {header.instruction ? (
          <p className="mt-4 rounded-md bg-muted/50 p-3 text-sm whitespace-pre-wrap">
            <span className="font-medium">Instructions: </span>
            {header.instruction.replace(/<\/?b>/g, "").replace(/<>/g, "")}
          </p>
        ) : null}
      </section>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="border-b px-4 py-3">
          <h2 className="font-semibold">Components</h2>
          <p className="text-xs text-muted-foreground">
            {lines.length} component(s) · order matches ERP save sequence
          </p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="border-b bg-muted/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Component</th>
                <th className="px-3 py-2">GSM</th>
                <th className="px-3 py-2">Lami</th>
                <th className="px-3 py-2">Color</th>
                <th className="px-3 py-2">Fabric</th>
                <th className="px-3 py-2">Cut size</th>
                <th className="px-3 py-2 text-right">Order mtr</th>
                <th className="px-3 py-2 text-right">Kg / bag</th>
                <th className="px-3 py-2">GPM</th>
                <th className="px-3 py-2">Remarks</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => (
                <tr key={`${line.sortOrder}-${line.heading}`} className="border-b">
                  <td className="px-3 py-2 font-medium">{line.heading}</td>
                  <td className="px-3 py-2">{line.gsm || "—"}</td>
                  <td className="px-3 py-2">{line.lami || "—"}</td>
                  <td className="px-3 py-2">{line.color || "—"}</td>
                  <td className="px-3 py-2">{line.fabricSize || "—"}</td>
                  <td className="px-3 py-2">{line.cutSize || "—"}</td>
                  <td className="px-3 py-2 text-right">{line.totalMtr ?? "—"}</td>
                  <td className="px-3 py-2 text-right">{line.totalKg ?? "—"}</td>
                  <td className="px-3 py-2">{line.gpm || "—"}</td>
                  <td className="max-w-[260px] px-3 py-2">{line.remarks || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
