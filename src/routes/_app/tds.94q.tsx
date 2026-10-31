import { createFileRoute } from "@tanstack/react-router";
import { TdsSection94Q } from "@/components/tds/TdsSection94A";

export const Route = createFileRoute("/_app/tds/94q")({
  head: () => ({ meta: [{ title: "TDS 94Q — PO Portal" }] }),
  component: TdsSection94Q,
});
