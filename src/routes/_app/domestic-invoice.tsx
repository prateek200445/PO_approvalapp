import { createFileRoute } from "@tanstack/react-router";
import { DomesticInvoiceScreen } from "@/components/domestic-invoice/DomesticInvoiceScreen";

export const Route = createFileRoute("/_app/domestic-invoice")({
  head: () => ({ meta: [{ title: "Domestic Invoice — PO Portal" }] }),
  component: DomesticInvoiceScreen,
});
