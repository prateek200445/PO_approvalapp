import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/planning/setup")({
  component: PlanningSetupLayout,
});

function PlanningSetupLayout() {
  return <Outlet />;
}
