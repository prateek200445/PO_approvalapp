import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94I } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94i")({
  head: () => ({ meta: [{ title: "TDS 94I — PO Portal" }] }),
  component: TdsSection94I,
});
