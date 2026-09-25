import { createFileRoute, redirect } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { AppShell } from "@/components/AppShell";
import {
  getExportBillOverdueCompanies,
  getExportBillOverdueGroups,
} from "@/lib/export-bill-overdue-api";
import { getSalesCompanies } from "@/lib/sales-dashboard-api";

export const Route = createFileRoute("/_app")({
  beforeLoad: () => {
    if (typeof window === "undefined") return;
    const raw =
      localStorage.getItem("po-portal-user") ??
      sessionStorage.getItem("po-portal-user");
    if (!raw) throw redirect({ to: "/" });
    try {
      const parsed = JSON.parse(raw) as { username?: unknown };
      const username =
        typeof parsed?.username === "string" ? parsed.username.trim() : "";
      if (!username) {
        localStorage.removeItem("po-portal-user");
        sessionStorage.removeItem("po-portal-user");
        throw redirect({ to: "/" });
      }
    } catch (e) {
      if (e && typeof e === "object" && "to" in e) throw e;
      localStorage.removeItem("po-portal-user");
      sessionStorage.removeItem("po-portal-user");
      throw redirect({ to: "/" });
    }
  },
  component: AppLayout,
});

function AppLayout() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const staleTime = 60 * 60_000;
    void queryClient.prefetchQuery({
      queryKey: ["export-bill-overdue-companies"],
      queryFn: getExportBillOverdueCompanies,
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: ["export-bill-overdue-groups"],
      queryFn: getExportBillOverdueGroups,
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: ["sales-dashboard-companies"],
      queryFn: getSalesCompanies,
      staleTime,
    });
  }, [queryClient]);

  return <AppShell />;
}
