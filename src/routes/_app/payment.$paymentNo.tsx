import { useAuth } from "@/lib/auth-context";
import { getApiUrl } from "@/lib/api-config";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { ArrowLeft, CheckCircle2, XCircle, Building2, Calendar, User as UserIcon, Hash, IndianRupee, Briefcase, Loader2, Landmark, Wallet} from "lucide-react";
import { formatINR } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { toast } from "sonner";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { SkeletonCard, SkeletonSection } from "@/components/SkeletonLoader";

export const Route = createFileRoute("/_app/payment/$paymentNo")({
 head: ({ params }) => ({
    meta: [{ title: `${params.paymentNo} - Payment Details` }],
}),
 component: PaymentDetails,
  notFoundComponent: () => <div className="p-8 text-center">Work Order not found.</div>,
});

function PaymentDetails() {
  const { paymentNo } = Route.useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [remarks, setRemarks] = useState("");
  const [confirm, setConfirm] = useState<null | "approve" | "reject">(null);
  const [isApproving, setIsApproving] = useState(false);
  const [isRejecting, setIsRejecting] = useState(false);

  // Fetch Payment details with React Query (cache indefinitely - immutable data)
  const { data: paymentData, isLoading: paymentLoading } = useQuery({
    queryKey: ['payment-details', paymentNo],
    queryFn: async () => {
      const response = await fetch(getApiUrl(`/api/Payment/details?paymentNo=${encodeURIComponent(paymentNo)}`));
      if (!response.ok) throw new Error('Failed to fetch Payment details');
      return response.json();
    },
    staleTime: Infinity, // Cache indefinitely
  });

  // Fetch workflow/history with React Query
  const { data: workflowData } = useQuery({
    queryKey: ['payment-workflow', paymentNo],
    queryFn: async () => {
      const response = await fetch(getApiUrl(`/api/Payment/history?paymentNo=${encodeURIComponent(paymentNo)}`));
      if (!response.ok) throw new Error('Failed to fetch workflow');
      return response.json();
    },
    staleTime: Infinity, // Cache indefinitely
  });

  // Transform data to match original format
  const payment = paymentData;
  const workflow = Array.isArray(workflowData) ? workflowData : [];
  const loading = paymentLoading;

  if (loading) {
    return (
      <div className="space-y-5 pb-24 md:pb-0">
        <SkeletonCard />
        <div className="grid gap-5 lg:grid-cols-3">
          <div className="space-y-5 lg:col-span-2">
            <SkeletonSection title="Payment Details" />
            <SkeletonSection title="Remarks" />
          </div>
          <div>
            <SkeletonSection title="Approval Workflow" />
          </div>
        </div>
      </div>
    );
  }

if (!payment) {
  return (
    <div className="rounded-xl border border-border bg-card p-8 text-center">
      <p>Payment not found.</p>
      <Link
        to="/payments"
        className="mt-3 inline-block text-sm font-medium text-primary hover:underline"
      >
        Back to list
      </Link>
    </div>
  );
}

  async function handleApprove() {
  if (isApproving) return; // Prevent double-click
  
  setIsApproving(true);
  try {
    const response = await fetch(
      getApiUrl("/api/Payment/approve"),
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          paymentNo: paymentNo,
          userName: user?.username,
          comment: remarks,
        }),
      }
    );

    if (!response.ok) {
      throw new Error(await response.text());
    }

    toast.success("Payment approved successfully");

    const cachedLists = queryClient.getQueriesData({ queryKey: ["payment-list"] });
    let nextPaymentNo: string | null = null;
    for (const [, data] of cachedLists) {
      if (Array.isArray(data)) {
        const next = data.find((p: any) => (p.paymentNo ?? p.PaymentNo) !== paymentNo);
        if (next) {
          nextPaymentNo = next.paymentNo ?? next.PaymentNo ?? null;
          break;
        }
      }
    }

    void queryClient.invalidateQueries({ queryKey: ["payment-list"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboard-stats"] });
    void queryClient.invalidateQueries({ queryKey: ["pending-list-dashboard"] });

    if (nextPaymentNo) {
      navigate({ to: "/payment/$paymentNo", params: { paymentNo: nextPaymentNo } });
    } else {
      navigate({ to: "/payments" });
    }
  } catch (err) {
    console.error(err);
    toast.error("Approval failed");
  } finally {
    setIsApproving(false);
  }
}

  async function handleConfirm() {
  if (confirm !== "reject") return;
  
  if (!remarks.trim()) {
    toast.error("Remarks are mandatory for rejection");
    return;
  }

  if (isRejecting) return; // Prevent double-click
  
  setIsRejecting(true);
  try {
    const response = await fetch(
      getApiUrl("/api/Payment/reject"),
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          paymentNo,
          userName: user?.username,
          comment: remarks,
        }),
      }
    );

    if (!response.ok) {
      throw new Error(await response.text());
    }

    toast.success("Payment rejected successfully");

    setConfirm(null);

    const cachedLists = queryClient.getQueriesData({ queryKey: ["payment-list"] });
    let nextPaymentNo: string | null = null;
    for (const [, data] of cachedLists) {
      if (Array.isArray(data)) {
        const next = data.find((p: any) => (p.paymentNo ?? p.PaymentNo) !== paymentNo);
        if (next) {
          nextPaymentNo = next.paymentNo ?? next.PaymentNo ?? null;
          break;
        }
      }
    }

    void queryClient.invalidateQueries({ queryKey: ["payment-list"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboard-stats"] });
    void queryClient.invalidateQueries({ queryKey: ["pending-list-dashboard"] });

    if (nextPaymentNo) {
      navigate({ to: "/payment/$paymentNo", params: { paymentNo: nextPaymentNo } });
    } else {
      navigate({ to: "/payments" });
    }
  } catch (err) {
    console.error(err);
    toast.error("Operation failed");
  } finally {
    setIsRejecting(false);
  }
}

const grandTotal = Number(payment?.paymentAmount || 0);

const headerItems = [
  {
    icon: Hash,
    label: "Payment No",
    value: payment?.paymentNo,
  },
  {
    icon: Building2,
    label: "Vendor",
    value: payment?.vendorName,
  },
  {
    icon: Calendar,
    label: "Payment Date",
    value: payment?.paymentDate,
  },
  {
    icon: Briefcase,
    label: "Company",
    value: payment?.companyName,
  },
  {
    icon: UserIcon,
    label: "Requested By",
    value: payment?.requestedBy,
  },
  {
    icon: IndianRupee,
    label: "Payment Amount",
    value: formatINR(payment?.paymentAmount || 0),
    strong: true,
  },
  {
    icon: Hash,
    label: "Bill No",
    value: payment?.billNo,
  },
 {
    icon: Wallet,
    label: "Bill Amount",
    value: formatINR(payment?.billAmount),
    strong: true,
},
{
    icon: Landmark,
    label: "Payment Amount",
    value: formatINR(payment?.paymentAmount),
    strong: true,
},
];

  return (
    <div className="space-y-5 pb-24 md:pb-0 max-w-full overflow-x-hidden">
      <div className="flex items-center justify-between">
        <button onClick={() => navigate({ to: "/payments" })} className="inline-flex items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" /> Back
        </button>
        <StatusBadge status="Pending" />
      </div>

     {/* Header card */}
<div className="rounded-xl border border-border bg-card p-5">

  <div className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
    Payment Approval
</div>

<h1 className="text-xl font-semibold tracking-tight md:text-2xl">
    {payment?.paymentNo}
</h1>

<p className="mt-1 text-sm text-muted-foreground">
    {payment?.vendorName}
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

      <div className="grid gap-5 lg:grid-cols-3 min-w-0 w-full">
        <div className="space-y-5 lg:col-span-2 min-w-0 w-full">
          {/* Section A: Summary */}
         <Section title="Payment Details">
    <div className="grid grid-cols-2 gap-4 text-sm">

        <div>
            <strong>Company</strong>
            <div>{payment?.companyName}</div>
        </div>

        <div>
            <strong>Vendor</strong>
            <div>{payment?.vendorName}</div>
        </div>

        <div>
            <strong>Bill No</strong>
            <div>{payment?.billNo}</div>
        </div>

        <div>
            <strong>MRN No</strong>
            <div>{payment?.mrnNo}</div>
        </div>

        <div>
            <strong>Bill Date</strong>
            <div>{payment?.billDate}</div>
        </div>

        <div>
            <strong>Payment Date</strong>
            <div>{payment?.paymentDate}</div>
        </div>

        <div>
            <strong>Payment Terms</strong>
            <div>{payment?.paymentTerms}</div>
        </div>

        <div>
            <strong>Currency</strong>
            <div>{payment?.currency}</div>
        </div>

        <div>
            <strong>Remarks</strong>
            <div>{payment?.remarks}</div>
        </div>

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
        <div className="lg:col-span-1 min-w-0 w-full">
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
            onClick={() => navigate({ to: "/payments" })}
            className="hidden h-11 flex-1 rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary md:inline-flex md:flex-none md:items-center"
          >
            Back
          </button>
          <button
            disabled={isRejecting || isApproving}
            onClick={() => setConfirm("reject")}
            className="h-11 flex-1 rounded-md border border-destructive/30 bg-destructive/10 px-4 text-sm font-semibold text-destructive hover:bg-destructive/20 disabled:opacity-50 disabled:cursor-not-allowed md:flex-none md:px-6"
          >
            {isRejecting ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Rejecting...
              </span>
            ) : (
              "Reject"
            )}
          </button>
          <button
            disabled={isApproving || isRejecting}
            onClick={handleApprove}
            className="h-11 flex-1 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed md:flex-none md:px-6"
          >
            {isApproving ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Approving...
              </span>
            ) : (
              "Approve"
            )}
          </button>
        </div>
      </div>

      {/* Confirmation dialog */}
      {confirm === "reject" && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={() => setConfirm(null)}>
          <div className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="mb-3 inline-flex h-10 w-10 items-center justify-center rounded-full bg-destructive/15 text-destructive">
              <XCircle className="h-5 w-5" />
            </div>
            <h3 className="text-base font-semibold">Reject this WO?</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              This action will reject the WO and notify the requester.
            </p>
            {!remarks.trim() && (
              <p className="mt-2 text-xs font-medium text-destructive">Remarks are required to reject.</p>
            )}
            <div className="mt-5 flex gap-2">
              <button onClick={() => setConfirm(null)} className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary">Cancel</button>
              <button
                onClick={handleConfirm}
                disabled={isRejecting}
                className="h-10 flex-1 rounded-md text-sm font-semibold text-primary-foreground bg-destructive hover:bg-destructive/90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isRejecting ? (
                  <span className="inline-flex items-center gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Rejecting...
                  </span>
                ) : (
                  "Confirm Reject"
                )}
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

