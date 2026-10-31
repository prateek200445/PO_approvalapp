import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94C } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94c")({
  head: () => ({ meta: [{ title: "TDS 94C — PO Portal" }] }),
  component: TdsSection94C,
});
