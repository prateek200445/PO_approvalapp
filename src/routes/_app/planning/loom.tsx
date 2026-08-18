import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/planning/loom")({
  component: LoomPlanningLayout,
});

function LoomPlanningLayout() {
  return <Outlet />;
}
