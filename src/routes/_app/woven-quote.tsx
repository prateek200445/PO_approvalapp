import { createFileRoute } from "@tanstack/react-router";
import { WovenQuoteScreen } from "@/components/woven-quote/WovenQuoteScreen";

export const Route = createFileRoute("/_app/woven-quote")({
  head: () => ({ meta: [{ title: "Woven bag quotation — PO Portal" }] }),
  component: WovenQuoteScreen,
});
