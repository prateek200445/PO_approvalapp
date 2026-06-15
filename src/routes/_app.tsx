import { createFileRoute, redirect } from "@tanstack/react-router";
import { AppShell } from "@/components/AppShell";

export const Route = createFileRoute("/_app")({
  beforeLoad: () => {
    if (typeof window === "undefined") return;
    const raw = localStorage.getItem("po-portal-user") ?? sessionStorage.getItem("po-portal-user");
    if (!raw) throw redirect({ to: "/" });
  },
  component: AppShell,
});
