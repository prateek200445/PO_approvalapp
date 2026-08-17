import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/planning/fibc")({
  component: FibcPlanningLayout,
});

function FibcPlanningLayout() {
  return <Outlet />;
}
