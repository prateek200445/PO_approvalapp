import { useAuth } from "@/lib/auth-context";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, useState, ReactNode } from "react";
import { ApprovalDetailNav } from "@/components/ApprovalDetailNav";
import { useApprovalListNavigation } from "@/hooks/use-approval-list-navigation";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { invalidateApprovalCaches, resolveNextAfterApproval } from "@/lib/approval-after-action";

export const Route = createFileRoute("/_app/indent/$indentNo")({
  component: IndentDetailsPage,
});

function IndentDetailsPage() {
  const { indentNo } = Route.useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [items, setItems] = useState<any[]>([]);
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const [workflow, setWorkflow] = useState<any[]>([]);
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
        </div>
      </div>

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
