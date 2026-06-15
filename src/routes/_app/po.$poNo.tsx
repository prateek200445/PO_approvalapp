import { useAuth } from "@/lib/auth-context";
import { getApiUrl } from "@/lib/api-config";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState, useEffect } from "react";
import { ArrowLeft, FileText, Download, CheckCircle2, XCircle, Building2, Calendar, User as UserIcon, Hash, IndianRupee, Briefcase, ExternalLink } from "lucide-react";
import { formatINR, type ApprovalStep } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { toast } from "sonner";

export const Route = createFileRoute("/_app/po/$poNo")({
  head: ({ params }) => ({ meta: [{ title: `${params.poNo} — PO Details` }] }),
  component: PODetails,
  notFoundComponent: () => <div className="p-8 text-center">PO not found.</div>,
});

function PODetails() {
  const { poNo } = Route.useParams();
const navigate = useNavigate();
const { user } = useAuth();

const [po, setPo] = useState<any>(null);
const [workflow, setWorkflow] = useState<any[]>([]);
const [approval, setApproval] = useState<any>(null);
const [loading, setLoading] = useState(true);

const [remarks, setRemarks] = useState("");
const [confirm, setConfirm] = useState<null | "approve" | "reject">(null);
useEffect(() => {
  fetch(
    getApiUrl(`/api/PO/details?poNo=${encodeURIComponent(poNo)}`)
  )
    .then((r) => r.json())
    .then(async (data) => {
      setPo(data);

      const username = user?.username;

      const approvalRes = await fetch(
        getApiUrl(`/api/PO/approval?poNo=${encodeURIComponent(poNo)}&username=${username}`)
      );

      const approvalData = await approvalRes.json();

      setApproval(approvalData);

      // WORKFLOW API
      const workflowRes = await fetch(
        getApiUrl(`/api/PO/workflow?poNo=${encodeURIComponent(poNo)}`)
      );

      const workflowData = await workflowRes.json();

      setWorkflow(workflowData);

      setLoading(false);
    })
    .catch((err) => {
      console.error("DETAIL ERROR =", err);
      setLoading(false);
    });
}, [poNo]);
  if (loading) {
  return <div className="p-8">Loading...</div>;
}

if (!po || po.length === 0) {
  return (
    <div className="rounded-xl border border-border bg-card p-8 text-center">
      <p>PO not found.</p>
      <Link
        to="/pending"
        className="mt-3 inline-block text-sm font-medium text-primary hover:underline"
      >
        Back to list
      </Link>
    </div>
  );
}

const poData = Array.isArray(po) ? po[0] : null;



 async function handleConfirm() {
  if (confirm === "reject" && !remarks.trim()) {
    toast.error("Remarks are mandatory for rejection");
    return;
  }

  try {

    if (confirm === "approve") {
      await fetch(
        getApiUrl(`/api/PO/approve/${approval.TransId}`),
        {
          method: "POST",
        }
      );

      toast.success("PO approved successfully");
    }

    if (confirm === "reject") {
      await fetch(
        getApiUrl(`/api/PO/reject/${approval.TransId}`),
        {
          method: "POST",
        }
      );

      toast.success("PO rejected successfully");
    }

    setConfirm(null);
    setTimeout(() => navigate({ to: "/pending" }), 600);
  } catch (err) {
    console.error(err);
    toast.error("Operation failed");
  }
}
const grandTotal = Number(poData?.TotalAmount || 0);

const headerItems = [
  { icon: Hash, label: "PO Number", value: poData.PurchaseCode },
  { icon: Building2, label: "Vendor", value: poData.FirmName },
  { icon: Calendar, label: "PO Date", value: poData.deliverydate },
  { icon: Briefcase, label: "Department", value: poData.DepttName },
  { icon: UserIcon, label: "Requested By", value: poData.FirmName },
  {
    icon: IndianRupee,
    label: "PO Amount",
    value: formatINR(grandTotal),
    strong: true,
  },
];

  return (
    <div className="space-y-5 pb-24 md:pb-0">
      <div className="flex items-center justify-between">
        <button onClick={() => navigate({ to: "/pending" })} className="inline-flex items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" /> Back
        </button>
        <StatusBadge status="Pending" />
      </div>

      {/* Header card */}
      <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
        <div className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">Purchase Order</div>
       <h1 className="text-xl font-semibold tracking-tight md:text-2xl">
  {poData.PurchaseCode}
</h1>
        <p className="mt-1 text-sm text-muted-foreground">
  {poData.FirmName}
</p>
        <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 border-t border-border pt-4 md:grid-cols-3">
          {headerItems.map((h) => (
            <div key={h.label} className="flex items-start gap-2.5">
              <h.icon className="mt-0.5 h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <div className="min-w-0">
                <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{h.label}</div>
                <div className={`truncate text-sm ${h.strong ? "font-semibold tabular-nums" : "font-medium"}`}>{h.value}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="grid gap-5 lg:grid-cols-3">
        <div className="space-y-5 lg:col-span-2">
          {/* Section A: Summary */}
          <Section title="Purchase Order Summary">
            <p className="text-sm text-muted-foreground">
  {poData.ItemDesc}
</p>
            <div className="mt-4 overflow-hidden rounded-lg border border-border">
              <table className="w-full text-sm">
                <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 font-medium">Item</th>
                    <th className="px-3 py-2 text-right font-medium">Qty</th>
                    <th className="px-3 py-2 text-right font-medium">Rate</th>
                    <th className="px-3 py-2 text-right font-medium">Amount</th>
                  </tr>
                </thead>
               <tbody className="divide-y divide-border">
  {po.map((item: any, index: number) => (
    <tr key={index}>
      <td className="px-3 py-2">{item.ItemDesc}</td>
      <td className="px-3 py-2 text-right">{item.Qty}</td>
      <td className="px-3 py-2 text-right">
        {formatINR(item.Rate)}
      </td>
      <td className="px-3 py-2 text-right">
        {formatINR(item.Total)}
      </td>
    </tr>
  ))}
</tbody>
                <tfoot className="bg-secondary/30">
                  <tr>
                    <td colSpan={3} className="px-3 py-2 text-right text-xs font-medium uppercase text-muted-foreground">Total</td>
                    <td className="px-3 py-2 text-right font-semibold tabular-nums">
  {formatINR(grandTotal)}
</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </Section>

          {/* Section B: PDF */}
          <Section title="Purchase Order Document">
            <div className="flex flex-col items-center justify-center rounded-xl border border-border border-dashed p-8 text-center bg-card/50">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary mb-4">
                <FileText className="h-7 w-7" />
              </div>
              <h3 className="text-base font-semibold mb-1">Generated PO PDF</h3>
              <p className="text-sm text-muted-foreground max-w-sm mb-6">
                The official purchase order document is ready. Click below to view the PDF details in a new page.
              </p>
              <a
                href={getApiUrl(`/api/pdf?poNo=${encodeURIComponent(poNo)}`)}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-primary px-6 text-sm font-semibold text-primary-foreground hover:bg-primary/90 transition shadow-sm"
              >
                <span>View PDF Details</span>
                <ExternalLink className="h-4 w-4" />
              </a>
            </div>
          </Section>

          {/* Section D: Remarks */}
          <Section title="Remarks">
            <textarea
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              rows={4}
              placeholder="Add remarks (mandatory for rejection)…"
              className="w-full resize-none rounded-md border border-input bg-surface p-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </Section>
        </div>

        {/* Section C: Workflow */}
        <div className="lg:col-span-1">
         <Section title="Approval Workflow">
  <div className="space-y-3">
    {workflow.map((step: any) => (
      <div
        key={step.TransId}
        className="rounded-lg border border-border p-3"
      >
        <div className="font-medium">
          {step.ApprovalName}
        </div>

       <div
  className={`inline-block rounded px-2 py-1 text-xs font-semibold mt-2 ${
    step.Status === "Approved"
      ? "bg-green-500/20 text-green-400"
      : step.Status === "Rejected"
      ? "bg-red-500/20 text-red-400"
      : "bg-yellow-500/20 text-yellow-400"
  }`}
>
  {step.Status}
</div>

        <div className="text-sm text-muted-foreground">
          Approval Date: {step.ApprovalDate || "Pending"}
        </div>
      </div>
    ))}
  </div>
</Section>
        </div>
      </div>

      {/* Action buttons */}
      <div className="fixed inset-x-0 bottom-16 z-20 border-t border-border bg-surface/95 p-3 backdrop-blur md:static md:border-0 md:bg-transparent md:p-0">
        <div className="mx-auto flex max-w-7xl gap-2 md:justify-end">
          <button
            onClick={() => navigate({ to: "/pending" })}
            className="hidden h-11 flex-1 rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary md:inline-flex md:flex-none md:items-center"
          >
            Back
          </button>
          <button
            disabled={false}
            onClick={() => setConfirm("reject")}
            className="h-11 flex-1 rounded-md border border-destructive/30 bg-destructive/10 px-4 text-sm font-semibold text-destructive hover:bg-destructive/20 disabled:opacity-50 md:flex-none md:px-6"
          >
            Reject
          </button>
          <button
            disabled={false}
            onClick={() => setConfirm("approve")}
            className="h-11 flex-1 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 md:flex-none md:px-6"
          >
            Approve
          </button>
        </div>
      </div>

      {/* Confirmation dialog */}
      {confirm && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={() => setConfirm(null)}>
          <div className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className={`mb-3 inline-flex h-10 w-10 items-center justify-center rounded-full ${confirm === "approve" ? "bg-success/15 text-success" : "bg-destructive/15 text-destructive"}`}>
              {confirm === "approve" ? <CheckCircle2 className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}
            </div>
            <h3 className="text-base font-semibold">{confirm === "approve" ? "Approve this PO?" : "Reject this PO?"}</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              {confirm === "approve"
  ? `You are approving ${poData.PurchaseCode} for ${formatINR(poData.Total)}.`
                : "This action will reject the PO and notify the requester."}
            </p>
            {confirm === "reject" && !remarks.trim() && (
              <p className="mt-2 text-xs font-medium text-destructive">Remarks are required to reject.</p>
            )}
            <div className="mt-5 flex gap-2">
              <button onClick={() => setConfirm(null)} className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary">Cancel</button>
              <button
                onClick={handleConfirm}
                className={`h-10 flex-1 rounded-md text-sm font-semibold text-primary-foreground ${confirm === "approve" ? "bg-success hover:bg-success/90" : "bg-destructive hover:bg-destructive/90"}`}
              >
                Confirm {confirm === "approve" ? "Approve" : "Reject"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-border bg-card">
      <header className="border-b border-border px-5 py-3">
        <h2 className="text-sm font-semibold">{title}</h2>
      </header>
      <div className="p-5">{children}</div>
    </section>
  );
}


