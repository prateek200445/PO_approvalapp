import { useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Download, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  downloadExhibitionLeadsExcel,
  formatLeadTime,
  listExhibitionLeads,
  type ExhibitionFormType,
  type ExhibitionLead,
} from "@/lib/exhibition-leads-api";

export const Route = createFileRoute("/_app/exhibition-leads")({
  head: () => ({ meta: [{ title: "Exhibition Leads — PO Portal" }] }),
  component: ExhibitionLeadsPage,
});

function ExhibitionLeadsPage() {
  return (
    <div className="mx-auto w-full max-w-lg space-y-8 pb-6">
      <h1 className="text-xl font-semibold">Exhibition Leads</h1>
      <LeadSection title="Dealer / Distributor submissions" formType="DealerDistributor" />
      <LeadSection title="Product Inquiry submissions" formType="ProductInquiry" />
    </div>
  );
}

function LeadSection({ title, formType }: { title: string; formType: ExhibitionFormType }) {
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState("");
  const query = useQuery({
    queryKey: ["exhibition-leads", formType],
    queryFn: () => listExhibitionLeads(formType),
  });

  async function exportExcel() {
    setExportError("");
    setExporting(true);
    try {
      await downloadExhibitionLeadsExcel(formType);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "Could not download Excel.");
    } finally {
      setExporting(false);
    }
  }

  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-base font-semibold">{title}</h2>
        <Button type="button" variant="outline" size="sm" onClick={exportExcel} disabled={exporting}>
          {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
          Export Excel
        </Button>
      </div>
      {exportError ? <p className="text-sm text-destructive">{exportError}</p> : null}
      {query.isLoading ? (
        <p className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading
        </p>
      ) : query.isError ? (
        <p className="text-sm text-destructive">{query.error instanceof Error ? query.error.message : "Could not load submissions."}</p>
      ) : query.data && query.data.length > 0 ? (
        <ul className="space-y-2">
          {query.data.map((lead) => (
            <li key={lead.id} className="rounded-xl border bg-card p-3 text-sm">
              <LeadBody lead={lead} formType={formType} />
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-muted-foreground">No submissions yet.</p>
      )}
    </section>
  );
}

function LeadBody({ lead, formType }: { lead: ExhibitionLead; formType: ExhibitionFormType }) {
  return (
    <div className="space-y-1">
      <div className="font-medium">{lead.personName}</div>
      <div>{lead.contactNumber}</div>
      {formType === "DealerDistributor" ? (
        <>
          {lead.companyName ? <div>{lead.companyName}</div> : null}
          {lead.email ? <div className="text-muted-foreground">{lead.email}</div> : null}
          {lead.address ? <div className="text-muted-foreground">{lead.address}</div> : null}
          {lead.postalCode ? <div className="text-muted-foreground">{lead.postalCode}</div> : null}
        </>
      ) : (
        <>
          {lead.email ? <div className="text-muted-foreground">{lead.email}</div> : null}
          {lead.productInquiry ? <div>{lead.productInquiry}</div> : null}
          {lead.quantity != null ? <div>Qty {lead.quantity}</div> : null}
        </>
      )}
      <div className="text-xs text-muted-foreground">{formatLeadTime(lead.createdAt)}</div>
    </div>
  );
}
