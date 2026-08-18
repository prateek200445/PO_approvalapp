import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/planning/timeline")({
  component: IntegratedPlanningLayout,
});

function IntegratedPlanningLayout() {
  return <Outlet />;
}
