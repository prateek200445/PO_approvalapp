import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94A } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94a")({
  head: () => ({ meta: [{ title: "TDS 94A — PO Portal" }] }),
  component: TdsSection94A,
});
