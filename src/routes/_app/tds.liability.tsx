import { createFileRoute } from "@tanstack/react-router";
import { TdsLiability } from "@/components/tds/TdsLiability";

export const Route = createFileRoute("/_app/tds/liability")({
  head: () => ({ meta: [{ title: "TDS liability — PO Portal" }] }),
  component: TdsLiability,
});
