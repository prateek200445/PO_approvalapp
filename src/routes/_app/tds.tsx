import { createFileRoute, Outlet, redirect } from "@tanstack/react-router";
import { TdsSubnav } from "@/components/tds/TdsSubnav";

export const Route = createFileRoute("/_app/tds")({
  head: () => ({ meta: [{ title: "TDS — PO Portal" }] }),
  beforeLoad: ({ location }) => {
    if (location.pathname === "/tds") {
      throw redirect({ to: "/tds/liability" });
    }
  },
  component: TdsLayout,
});

function TdsLayout() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">TDS</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          TDS filing for a group. Import JV posts expense journals with TDS from Excel so the 26Q sheets pick them up.
        </p>
      </div>
      <TdsSubnav />
      <Outlet />
    </div>
  );
}
