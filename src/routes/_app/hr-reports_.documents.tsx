import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeft, BookOpen } from "lucide-react";
import { HrHero } from "@/components/HrHero";
import { HrPolicyDocuments } from "@/components/HrPolicyDocuments";
import { useAuth } from "@/lib/auth-context";

export const Route = createFileRoute("/_app/hr-reports_/documents")({
  head: () => ({ meta: [{ title: "HR Policies & Documents — PO Portal" }] }),
  component: HrDocumentsPage,
});

function HrDocumentsPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";

  return (
    <div className="space-y-5 pb-8">
      <HrHero
        icon={BookOpen}
        eyebrow={
          <Link to="/hr-reports" className="inline-flex items-center gap-1.5 hover:underline">
            <ArrowLeft className="h-3.5 w-3.5" />
            HR Reports
          </Link>
        }
        title="HR Policies & Documents"
        description="HR policies, code of conduct and the organogram. Documents marked for one company are shown only to that company's employees."
      />
      {username ? (
        <HrPolicyDocuments username={username} />
      ) : (
        <div className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-10 text-center text-sm text-muted-foreground">
          Please log in to view HR documents.
        </div>
      )}
    </div>
  );
}
