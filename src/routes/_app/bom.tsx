import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/bom")({
  component: BomLayout,
});

function BomLayout() {
  return <Outlet />;
}
