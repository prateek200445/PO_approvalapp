import { useAuth } from "@/lib/auth-context";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, useState } from "react";
import { ApprovalDetailNav } from "@/components/ApprovalDetailNav";
import { useApprovalListNavigation } from "@/hooks/use-approval-list-navigation";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { invalidateApprovalCaches, resolveNextAfterApproval } from "@/lib/approval-after-action";
import { formatINR } from "@/lib/mock-data";

export const Route = createFileRoute("/_app/indent/$indentNo")({
  component: IndentDetailsPage,
});

type AssociatedPo = {
  PoNo?: string;
  poNo?: string;
  FirmName?: string;
  firmName?: string;
  TotalAmount?: number;
  totalAmount?: number;
  Currency?: string;
  currency?: string;
};

function IndentDetailsPage() {
  const { indentNo } = Route.useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [items, setItems] = useState<any[]>([]);
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const [workflow, setWorkflow] = useState<any[]>([]);
  const [associatedPos, setAssociatedPos] = useState<AssociatedPo[]>([]);
  const [posLoading, setPosLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const indentNavigation = useApprovalListNavigation({
    kind: "indent",
    currentId: indentNo,
    listQueryKey: "indent-list",
    fallbackApiPath: (username) => `/api/Indent/pending/${username}?amount=&filterType=gte`,
    extractId: (row) => (row.IndentNo as string | undefined) ?? undefined,
  });

  useEffect(() => {
    setSelectedItems([]);
    setAssociatedPos([]);
  }, [indentNo]);

  // Associated POs do not need auth — start as soon as indentNo is known.
  useEffect(() => {
    let cancelled = false;
    setPosLoading(true);
    fetch(getApiUrl(`/api/Indent/purchase-orders?indentNo=${encodeURIComponent(indentNo)}`))
      .then((r) => r.json())
      .then((data) => {
        if (cancelled) return;
        const rows = (data.purchaseOrders ?? data.PurchaseOrders ?? []) as AssociatedPo[];
        setAssociatedPos(Array.isArray(rows) ? rows : []);
      })
      .catch(() => {
        if (!cancelled) setAssociatedPos([]);
      })
      .finally(() => {
        if (!cancelled) setPosLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [indentNo]);

  useEffect(() => {
    if (!user?.username) return;
    fetch(getApiUrl(`/api/Indent/details?indentNo=${encodeURIComponent(indentNo)}`))
      .then((r) => r.json())
      .then((data) => {
        setItems(data);
        setSelectedItems(data.map((x: any) => x.IndentSubCode));
      });
    fetch(getApiUrl(`/api/Indent/workflow?indentNo=${encodeURIComponent(indentNo)}`))
      .then((r) => r.json())
      .then(setWorkflow);
  }, [indentNo, user?.username]);

  function navigateAfterAction() {
    const nextIndentNo = resolveNextAfterApproval(indentNo, queryClient, {
      kind: "indent",
      listQueryKey: "indent-list",
      extractId: (row) => (row.IndentNo as string | undefined) ?? undefined,
    });
    invalidateApprovalCaches(queryClient, "indent-list");

    if (nextIndentNo) {
      navigate({ to: "/indent/$indentNo", params: { indentNo: nextIndentNo } });
    } else {
      navigate({ to: "/indents" });
    }
  }

  async function approveIndent() {
    if (isSubmitting) return;
    if (selectedItems.length === 0) {
      toast.error("Select at least one item to approve");
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await fetch(getApiUrl("/api/Indent/approve"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username: user?.username,
          indentSubCodes: selectedItems,
        }),
      });

      const result = await response.json();
      if (!response.ok || !result.success) {
        throw new Error("Approval failed");
      }

      toast.success(`${result.approvedItems} item(s) approved successfully`);
      navigateAfterAction();
    } catch (err) {
      console.error(err);
      toast.error("Error while approving indent");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function rejectIndent() {
    if (isSubmitting) return;
    if (selectedItems.length === 0) {
      toast.error("Select at least one item to reject");
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await fetch(getApiUrl("/api/Indent/reject"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username: user?.username,
          indentSubCodes: selectedItems,
        }),
      });

      const result = await response.json();
      if (!response.ok || !result.success) {
        throw new Error("Rejection failed");
      }

      toast.success(`${result.rejectedItems} item(s) rejected successfully`);
      navigateAfterAction();
    } catch (err) {
      console.error(err);
      toast.error("Error while rejecting indent");
    } finally {
      setIsSubmitting(false);
    }
  }

  const normalizedPos = associatedPos
    .map((row) => {
      const poNo = String(row.PoNo ?? row.poNo ?? "").trim();
      return {
        poNo,
        firmName: String(row.FirmName ?? row.firmName ?? "").trim(),
        totalAmount: Number(row.TotalAmount ?? row.totalAmount ?? 0) || 0,
        currency: String(row.Currency ?? row.currency ?? "").trim() || "INR",
      };
    })
    .filter((row) => row.poNo.length > 0);

  return (
    <div className="space-y-6">
      <ApprovalDetailNav
        onBack={() => navigate({ to: "/indents" })}
        navigation={indentNavigation}
        onPrevious={() =>
          indentNavigation.prev &&
          navigate({ to: "/indent/$indentNo", params: { indentNo: indentNavigation.prev } })
        }
        onNext={() =>
          indentNavigation.next &&
          navigate({ to: "/indent/$indentNo", params: { indentNo: indentNavigation.next } })
        }
      />

      <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
        <div className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
          Indent Approval
        </div>

        <h1 className="text-xl font-semibold tracking-tight md:text-2xl">{indentNo}</h1>

        <p className="mt-1 text-sm text-muted-foreground">{items[0]?.CompanyName}</p>

        <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 border-t border-border pt-4">
          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Department</div>
            <div className="text-sm font-medium">{items[0]?.ReqDepartment}</div>
          </div>

          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Purpose</div>
            <div className="text-sm font-medium">{items[0]?.Purpose}</div>
          </div>

          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Signal</div>
            <div className="text-sm font-medium">{items[0]?.IndentSignal}</div>
          </div>

          <div>
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Total Items</div>
            <div className="text-sm font-medium">{items.length}</div>
          </div>

          <div className="col-span-2">
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">
              Associated PO Number{normalizedPos.length === 1 ? "" : "s"}
            </div>
            {posLoading ? (
              <div className="mt-1 text-sm text-muted-foreground">Loading…</div>
            ) : normalizedPos.length === 0 ? (
              <div className="mt-1 text-sm text-muted-foreground">
                No PO linked to this indent in ERP yet.
              </div>
            ) : normalizedPos.length === 1 ? (
              <Link
                to="/po/$poNo"
                params={{ poNo: normalizedPos[0].poNo }}
                className="mt-1 inline-block text-sm font-medium text-primary underline-offset-2 hover:underline"
              >
                {normalizedPos[0].poNo}
              </Link>
            ) : (
              <div className="mt-1 flex flex-wrap gap-x-3 gap-y-1">
                {normalizedPos.map((po) => (
                  <Link
                    key={po.poNo}
                    to="/po/$poNo"
                    params={{ poNo: po.poNo }}
                    className="text-sm font-medium text-primary underline-offset-2 hover:underline"
                  >
                    {po.poNo}
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {normalizedPos.length > 0 && (
        <Section title="Associated Purchase Orders">
          {/* Mobile cards */}
          <div className="space-y-2 sm:hidden">
            {normalizedPos.map((po) => (
              <Link
                key={po.poNo}
                to="/po/$poNo"
                params={{ poNo: po.poNo }}
                className="block rounded-xl border border-border/70 bg-background px-3 py-2.5 transition hover:border-primary/40"
              >
                <div className="text-sm font-semibold text-primary">{po.poNo}</div>
                {po.firmName ? (
                  <div className="mt-0.5 break-words text-[11px] text-muted-foreground">{po.firmName}</div>
                ) : null}
                {po.totalAmount > 0 ? (
                  <div className="mt-1.5 text-xs font-medium tabular-nums">
                    {formatINR(po.totalAmount)}
                    {po.currency && po.currency !== "INR" ? ` ${po.currency}` : ""}
                  </div>
                ) : null}
              </Link>
            ))}
          </div>

          {/* Desktop table */}
          <div className="hidden overflow-hidden rounded-lg border border-border sm:block">
            <table className="w-full text-sm">
              <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-3 py-2">PO Number</th>
                  <th className="px-3 py-2">Vendor</th>
                  <th className="px-3 py-2 text-right">Amount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {normalizedPos.map((po) => (
                  <tr key={po.poNo}>
                    <td className="px-3 py-2">
                      <Link
                        to="/po/$poNo"
                        params={{ poNo: po.poNo }}
                        className="font-medium text-primary underline-offset-2 hover:underline"
                      >
                        {po.poNo}
                      </Link>
                    </td>
                    <td className="px-3 py-2">{po.firmName || "—"}</td>
                    <td className="px-3 py-2 text-right tabular-nums">
                      {po.totalAmount > 0 ? formatINR(po.totalAmount) : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Section>
      )}

      <Section title="Indent Items">
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Select</th>
                <th className="px-3 py-2">Item Code</th>
                <th className="px-3 py-2">Description</th>
                <th className="px-3 py-2 text-right">Qty</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-border">
              {items.map((item) => (
                <tr key={item.IndentSubCode}>
                  <td className="px-3 py-2">
                    <input
                      type="checkbox"
                      checked={selectedItems.includes(item.IndentSubCode)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedItems((prev) => [...prev, item.IndentSubCode]);
                        } else {
                          setSelectedItems((prev) => prev.filter((x) => x !== item.IndentSubCode));
                        }
                      }}
                    />
                  </td>

                  <td className="px-3 py-2">{item.ItemCode}</td>

                  <td className="px-3 py-2">{item.ItemDesc}</td>

                  <td className="px-3 py-2 text-right">{item.IndentQty}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Section>

      <Section title="Approval Workflow">
        <div className="space-y-3">
          {workflow.map((w) => (
            <div key={w.TransId} className="rounded-lg border border-border p-3">
              <div className="font-medium">{w.ApprovalName}</div>

              <div
                className={`mt-2 inline-block rounded px-2 py-1 text-xs font-semibold ${
                  w.Status === "Approved"
                    ? "bg-green-500/20 text-green-400"
                    : w.Status === "Rejected"
                      ? "bg-red-500/20 text-red-400"
                      : "bg-yellow-500/20 text-yellow-400"
                }`}
              >
                {w.Status}
              </div>

              <div className="mt-2 text-sm text-muted-foreground">
                Approval Date: {w.ApprovalDate || "Pending"}
              </div>
            </div>
          ))}
        </div>
      </Section>
      <Section title="Actions">
        <div className="flex flex-wrap gap-3">
          <button
            onClick={approveIndent}
            disabled={isSubmitting}
            className="rounded-lg bg-green-600 px-5 py-2 text-sm font-medium text-white transition hover:bg-green-700 disabled:opacity-50"
          >
            Approve Selected
          </button>

          <button
            onClick={rejectIndent}
            disabled={isSubmitting}
            className="rounded-lg bg-red-600 px-5 py-2 text-sm font-medium text-white transition hover:bg-red-700 disabled:opacity-50"
          >
            Reject Selected
          </button>
        </div>
      </Section>
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
