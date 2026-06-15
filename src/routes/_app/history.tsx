import { createFileRoute } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useEffect, useState } from "react";
import { CheckCircle2, XCircle } from "lucide-react";
import { StatusBadge } from "@/components/StatusBadge";
import { useAuth } from "@/lib/auth-context";

export const Route = createFileRoute("/_app/history")({
  head: () => ({ meta: [{ title: "Approval History — PO Portal" }] }),
  component: History,
});

function History() {
   console.log("HISTORY COMPONENT LOADED");
  const { user } = useAuth();
  const [history, setHistory] = useState<any[]>([]);

 useEffect(() => {
  console.log("USE EFFECT RUNNING");
  console.log("USER =", user);

  if (!user?.username) return;

    fetch(getApiUrl(`/api/PO/history/${user.username}`))
      .then((response) => response.json())
     .then((data) => {
  console.log("HISTORY =", data);
  
  setHistory(data);
})
      .catch((error) => {
        console.error(error);
      });
  }, [user]);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
          Approval History
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Audit trail of all approval actions.
        </p>
      </div>

      <div className="rounded-xl border border-border bg-card p-5 md:p-6">
        <ol className="relative ml-3 space-y-6 border-l-2 border-border pl-6">
          {history.map((h, index) => {
            const Icon =
              h.Status === "Approved"
                ? CheckCircle2
                : XCircle;

            const tone =
              h.Status === "Approved"
                ? "bg-success text-success-foreground"
                : "bg-destructive text-destructive-foreground";

            return (
              <li key={index} className="relative">
                <span
                  className={`absolute -left-[34px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full ${tone}`}
                >
                  <Icon className="h-3.5 w-3.5" />
                </span>

                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div>
                    <div className="text-sm font-semibold">
                      {h.ApprovalName}
                    </div>
                  </div>

                  <div className="text-right">
                    <StatusBadge status={h.Status} />
                    <div className="mt-1 text-xs text-muted-foreground">
                      {h.ApprovalDate}
                    </div>
                  </div>
                </div>

                <div className="mt-2 text-sm">
                  PO Number:{" "}
                  <span className="font-medium">
                    {h.PoNo}
                  </span>
                </div>

               
                
              </li>
            );
          })}
        </ol>
      </div>
    </div>
  );
}