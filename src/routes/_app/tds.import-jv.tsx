import { createFileRoute } from "@tanstack/react-router";
import { ImportJvScreen } from "@/components/tds/ImportJvScreen";

export const Route = createFileRoute("/_app/tds/import-jv")({
  head: () => ({ meta: [{ title: "Import JV — PO Portal" }] }),
  component: ImportJvScreen,
});
