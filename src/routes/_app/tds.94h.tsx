import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94H } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94h")({
  head: () => ({ meta: [{ title: "TDS 94H — PO Portal" }] }),
  component: TdsSection94H,
});
