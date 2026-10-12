import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Loader2, Users } from "lucide-react";
import { HrEmployeeMaster } from "@/components/HrEmployeeMaster";
import { HrHero } from "@/components/HrHero";
import { useAuth } from "@/lib/auth-context";
import { isHrReportsFullAccessUser } from "@/lib/feature-flags";
import { getHrAccess } from "@/lib/hr-reports-api";

export const Route = createFileRoute("/_app/hr-reports_/employee-master")({
  head: () => ({ meta: [{ title: "Employee Master — PO Portal" }] }),
  component: HrEmployeeMasterPage,
});

function HrEmployeeMasterPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";

  const accessQuery = useQuery({
    queryKey: ["hr-access", username],
    queryFn: () => getHrAccess(username),
    enabled: !!username,
    staleTime: 5 * 60_000,
  });
  const isFullAccess = accessQuery.data?.hasFullAccess === true || isHrReportsFullAccessUser(username);

  return (
    <div className="space-y-5 pb-8">
      <HrHero
        icon={Users}
        eyebrow={
          <Link to="/hr-reports" className="inline-flex items-center gap-1.5 hover:underline">
            <ArrowLeft className="h-3.5 w-3.5" />
            HR Reports
          </Link>
        }
        title="Employee Master"
        description="Headcount, attrition, joining and exits, salary increments, PF/ESIC and origin across companies."
      />

      {!username ? (
        <div className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-10 text-center text-sm text-muted-foreground">
          Please log in to view employee master data.
        </div>
      ) : !isFullAccess && accessQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Checking HR access…
        </div>
      ) : !isFullAccess ? (
        <div className="rounded-xl border border-amber-500/40 bg-amber-500/10 px-4 py-6 text-sm">
          Only HR can view employee master data.
        </div>
      ) : (
        <HrEmployeeMaster username={username} />
      )}
    </div>
  );
}
