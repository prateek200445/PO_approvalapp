import { createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/tds/import-jv")({
  beforeLoad: () => {
    throw redirect({ to: "/import-jv" });
  },
  component: () => null,
});
