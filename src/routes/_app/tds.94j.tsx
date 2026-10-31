import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94J } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94j")({
  head: () => ({ meta: [{ title: "TDS 94J — PO Portal" }] }),
  component: TdsSection94J,
});
