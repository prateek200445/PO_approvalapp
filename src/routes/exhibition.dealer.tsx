import { createFileRoute } from "@tanstack/react-router";
import { PublicExhibitionForm } from "@/components/exhibition/PublicExhibitionForm";

export const Route = createFileRoute("/exhibition/dealer")({
  head: () => ({
    meta: [{ title: "Dealer / Distributor — Exhibition" }],
  }),
  component: DealerExhibitionPage,
});

function DealerExhibitionPage() {
  return <PublicExhibitionForm title="Dealer / Distributor" formType="DealerDistributor" />;
}
