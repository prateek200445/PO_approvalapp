import { createFileRoute } from "@tanstack/react-router";
import { PublicExhibitionForm } from "@/components/exhibition/PublicExhibitionForm";

export const Route = createFileRoute("/exhibition/product")({
  head: () => ({
    meta: [{ title: "Product Inquiry — Exhibition" }],
  }),
  component: ProductExhibitionPage,
});

function ProductExhibitionPage() {
  return <PublicExhibitionForm title="Product Inquiry" formType="ProductInquiry" />;
}
