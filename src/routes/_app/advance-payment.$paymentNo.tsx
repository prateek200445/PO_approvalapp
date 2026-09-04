import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import {
  Building2,
  Calendar,
  Hash,
  IndianRupee,
  Landmark,
  Loader2,
  User as UserIcon,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import { getApiUrl } from "@/lib/api-config";
import { useAuth } from "@/lib/auth-context";
import { ApprovalDetailNav } from "@/components/ApprovalDetailNav";
import { ApprovalCommandBar, RemarkComposer } from "@/components/ApprovalCommandBar";
import { SkeletonCard, SkeletonSection } from "@/components/SkeletonLoader";
import { StatusBadge } from "@/components/StatusBadge";
import { formatINR } from "@/lib/mock-data";
import { useApprovalListNavigation } from "@/hooks/use-approval-list-navigation";
import { invalidateApprovalCaches, resolveNextAfterApproval } from "@/lib/approval-after-action";
import { BILL_PAYMENT_ENTRY_ENABLED } from "@/lib/feature-flags";
import { BillPaymentEntryUnavailable } from "@/components/BillPaymentEntryUnavailable";

type BillPaymentEntryDetail = {
  paymentNo: string;
  companyName: string;
  vendorName: string;
  billNo: string;
  billDate: string;
  mrnNo: string;
  mrnDate: string;
  billAmount: number;
  paymentAmount: number;
  paymentDate: string;
  requestedBy: string;
  remarks: string;
  paymentTerms: string;
  priorityLevel: string;
  currency: string;
  currencyRate: number;
  tds: number;
  debitNoteAmnt: number;
  lc: string;
  utrNo: string;
  paymentBankName: string;
  paymentBankAccNo: string;
  speReq: string;
};

type BillPaymentHistory = {
  approvalName?: string;
  ApprovalName?: string;
  status?: string;
  Status?: string;
  approvalDate?: string | null;
  ApprovalDate?: string | null;
  comment?: string;
  Comment?: string;
  loginName?: string;
  LoginName?: string;
};

export const Route = createFileRoute("/_app/advance-payment/$paymentNo")({
  head: ({ params }) => ({
    meta: [{ title: `${params.paymentNo} - Bill Payment Entry` }],
  }),
  component: BILL_PAYMENT_ENTRY_ENABLED ? AdvancePaymentDetails : BillPaymentEntryUnavailable,
  notFoundComponent: () => <div className="p-8 text-center">Bill payment entry not found.</div>,
});

function AdvancePaymentDetails() {
  const { paymentNo } = Route.useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [remarks, setRemarks] = useState("");
  const [confirm, setConfirm] = useState<null | "approve" | "reject">(null);
  const [isApproving, setIsApproving] = useState(false);
  const [isRejecting, setIsRejecting] = useState(false);

  const paymentNavigation = useApprovalListNavigation({
    kind: "advancePayment",
    currentId: paymentNo,
    listQueryKey: "advance-payment-list",
    fallbackApiPath: (username) =>
      `/api/AdvancePayment/pending/${username}?amount=&filterType=gte`,
    extractId: (row) =>
      (row.paymentNo as string | undefined) ?? (row.PaymentNo as string | undefined),
  });

  useEffect(() => {
    setRemarks("");
    setConfirm(null);
  }, [paymentNo]);

  const { data: paymentData, isLoading: paymentLoading } = useQuery({
    queryKey: ["advance-payment-details", paymentNo],
    queryFn: async () => {
      const response = await fetch(
        getApiUrl(`/api/AdvancePayment/details?paymentNo=${encodeURIComponent(paymentNo)}`),
      );
      if (!response.ok) throw new Error("Failed to fetch bill payment entry details");
      return response.json();
    },
    staleTime: Infinity,
  });

  const { data: historyData } = useQuery({
    queryKey: ["advance-payment-workflow", paymentNo],
    queryFn: async () => {
      const response = await fetch(
        getApiUrl(`/api/AdvancePayment/history?paymentNo=${encodeURIComponent(paymentNo)}`),
      );
      if (!response.ok) throw new Error("Failed to fetch bill payment workflow");
      return response.json();
    },
    staleTime: Infinity,
  });

  const payment = paymentData as BillPaymentEntryDetail | undefined;
  const workflow = Array.isArray(historyData) ? (historyData as BillPaymentHistory[]) : [];

  if (paymentLoading) {
    return (
      <div className="space-y-5 pb-action">
        <SkeletonCard />
        <div className="grid gap-5 lg:grid-cols-3">
          <div className="space-y-5 lg:col-span-2">
            <SkeletonSection title="Bill Payment Entry Details" />
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
        <p>Bill payment entry not found.</p>
        <Link to="/advance-payments" className="mt-3 inline-block text-sm font-medium text-primary hover:underline">
          Back to list
        </Link>
      </div>
    );
  }

  async function handleApprove() {
    if (isApproving) return;

    setIsApproving(true);
    try {
      const response = await fetch(getApiUrl("/api/AdvancePayment/approve"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          paymentNo,
          userName: user?.username,
          comment: remarks,
        }),
      });

      if (!response.ok) throw new Error(await response.text());

      toast.success("Bill payment entry approved successfully");

      const nextPaymentNo = resolveNextAfterApproval(paymentNo, queryClient, {
        kind: "advancePayment",
        listQueryKey: "advance-payment-list",
        extractId: (row) =>
          (row.paymentNo as string | undefined) ?? (row.PaymentNo as string | undefined) ?? undefined,
      });
      invalidateApprovalCaches(queryClient, "advance-payment-list");

      if (nextPaymentNo) {
        navigate({ to: "/advance-payment/$paymentNo", params: { paymentNo: nextPaymentNo } });
      } else {
        navigate({ to: "/advance-payments" });
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

    if (isRejecting) return;

    setIsRejecting(true);
    try {
      const response = await fetch(getApiUrl("/api/AdvancePayment/reject"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          paymentNo,
          userName: user?.username,
          comment: remarks,
        }),
      });

      if (!response.ok) throw new Error(await response.text());

      toast.success("Bill payment entry rejected successfully");
      setConfirm(null);

      const nextPaymentNo = resolveNextAfterApproval(paymentNo, queryClient, {
        kind: "advancePayment",
        listQueryKey: "advance-payment-list",
        extractId: (row) =>
          (row.paymentNo as string | undefined) ?? (row.PaymentNo as string | undefined) ?? undefined,
      });
      invalidateApprovalCaches(queryClient, "advance-payment-list");

      if (nextPaymentNo) {
        navigate({ to: "/advance-payment/$paymentNo", params: { paymentNo: nextPaymentNo } });
      } else {
        navigate({ to: "/advance-payments" });
      }
    } catch (err) {
      console.error(err);
      toast.error("Operation failed");
    } finally {
      setIsRejecting(false);
    }
  }

  const headerItems = [
    { icon: Hash, label: "Payment No", value: payment.paymentNo },
    { icon: Building2, label: "Vendor", value: payment.vendorName || "—" },
    { icon: Calendar, label: "Payment Date", value: payment.paymentDate },
    { icon: UserIcon, label: "Requested By", value: payment.requestedBy || "—" },
    { icon: IndianRupee, label: "Bill Amount", value: formatINR(payment.billAmount || 0), strong: true },
    { icon: Landmark, label: "Payment Amount", value: formatINR(payment.paymentAmount || 0), strong: true },
  ];

  const grandTotal = Number(payment?.paymentAmount || 0);

  return (
    <div className="space-y-5 pb-action max-w-full overflow-x-hidden">
      <ApprovalDetailNav
        onBack={() => navigate({ to: "/advance-payments" })}
        navigation={paymentNavigation}
        onPrevious={() =>
          paymentNavigation.prev &&
          navigate({
            to: "/advance-payment/$paymentNo",
            params: { paymentNo: paymentNavigation.prev },
          })
        }
        onNext={() =>
          paymentNavigation.next &&
          navigate({
            to: "/advance-payment/$paymentNo",
            params: { paymentNo: paymentNavigation.next },
          })
        }
        trailing={<StatusBadge status="Pending" />}
      />

      <div className="rounded-xl border border-border bg-card p-5">
        <div className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
          Bill Payment Entry Approval
        </div>
        <h1 className="text-xl font-semibold tracking-tight md:text-2xl">{payment.paymentNo}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{payment.vendorName || payment.companyName}</p>
        <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 border-t border-border pt-4 md:grid-cols-3">
          {headerItems.map((h) => (
            <div key={h.label} className="flex items-start gap-2.5">
              <h.icon className="mt-0.5 h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <div className="min-w-0">
                <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{h.label}</div>
                <div className={`truncate text-sm ${h.strong ? "font-semibold tabular-nums" : "font-medium"}`}>
                  {h.value}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="grid gap-5 lg:grid-cols-3 min-w-0 w-full">
        <div className="space-y-5 lg:col-span-2 min-w-0 w-full">
          <Section title="Bill Payment Entry Details">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <Field label="Company" value={payment.companyName || "—"} />
              <Field label="Vendor" value={payment.vendorName || "—"} />
              <Field label="Bill No" value={payment.billNo || "—"} />
              <Field label="MRN No" value={payment.mrnNo || "—"} />
              <Field label="Bill Date" value={payment.billDate || "—"} />
              <Field label="Payment Date" value={payment.paymentDate || "—"} />
              <Field label="Payment Terms" value={payment.paymentTerms || "—"} />
              <Field label="Priority" value={payment.priorityLevel || "—"} />
              <Field label="LC" value={payment.lc || "—"} />
              <Field label="TDS" value={formatINR(payment.tds || 0)} />
              <Field label="Debit Note" value={formatINR(payment.debitNoteAmnt || 0)} />
              <Field label="Special Request" value={payment.speReq || "—"} />
              <Field label="Currency" value={payment.currency || "—"} />
              <Field label="Bank" value={payment.paymentBankName || "—"} />
              <Field label="Requested By" value={payment.requestedBy || "—"} />
              <div className="col-span-2">
                <strong>Remarks</strong>
                <div>{payment.remarks || "—"}</div>
              </div>
            </div>
          </Section>

          <Section title="Remarks">
            <RemarkComposer value={remarks} onChange={setRemarks} />
          </Section>
        </div>

        <div className="lg:col-span-1 min-w-0 w-full">
          <Section title="Approval Workflow">
            <div className="space-y-3">
              {workflow.map((step, index) => {
                const name = step.approvalName || step.ApprovalName || "Approver";
                const status = step.status || step.Status || "Pending";
                const date = step.approvalDate || step.ApprovalDate;
                const comment = step.comment || step.Comment;
                return (
                <div key={`${name}-${index}`} className="rounded-lg border border-border p-3">
                  <div className="font-medium">{name}</div>
                  <div
                    className={`inline-block rounded px-2 py-1 text-xs font-semibold mt-2 ${
                      status === "Approved"
                        ? "bg-green-500/20 text-green-400"
                        : status === "Rejected"
                          ? "bg-red-500/20 text-red-400"
                          : "bg-yellow-500/20 text-yellow-400"
                    }`}
                  >
                    {status}
                  </div>
                  <div className="text-sm text-muted-foreground mt-2">
                    Approval Date: {date || "Pending"}
                  </div>
                  {comment ? (
                    <div className="text-sm text-muted-foreground">Comment: {comment}</div>
                  ) : null}
                </div>
                );
              })}
            </div>
          </Section>
        </div>
      </div>

      <ApprovalCommandBar
        amountLabel={formatINR(grandTotal)}
        queueLabel={
          paymentNavigation.total > 1 ? `${paymentNavigation.index} of ${paymentNavigation.total} in queue` : undefined
        }
        onApprove={handleApprove}
        onReject={() => setConfirm("reject")}
        isApproving={isApproving}
        isRejecting={isRejecting}
      />

      {confirm === "reject" && (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center"
          onClick={() => setConfirm(null)}
        >
          <div className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="mb-3 inline-flex h-10 w-10 items-center justify-center rounded-full bg-destructive/15 text-destructive">
              <XCircle className="h-5 w-5" />
            </div>
            <h3 className="text-base font-semibold">Reject this bill payment entry?</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              This action will reject the bill payment entry and move to the next item in queue.
            </p>
            {!remarks.trim() && (
              <p className="mt-2 text-xs font-medium text-destructive">Remarks are required to reject.</p>
            )}
            <div className="mt-5 flex gap-2">
              <button
                onClick={() => setConfirm(null)}
                className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary"
              >
                Cancel
              </button>
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

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <strong>{label}</strong>
      <div>{value}</div>
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
