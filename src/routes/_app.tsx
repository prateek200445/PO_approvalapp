import { createFileRoute, redirect } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { AppShell } from "@/components/AppShell";
import {
  DEFAULT_EXPORT_GROUP,
  DEFAULT_PAGE_SIZE,
  getExportBillOverdue,
  getExportBillOverdueCompanies,
  getExportBillOverdueGroups,
} from "@/lib/export-bill-overdue-api";
import { DEFAULT_SALES_FILTERS, getSalesCompanies, getSalesKpis, getSalesTables, getSalesYearlyTrend } from "@/lib/sales-dashboard-api";

export const Route = createFileRoute("/_app")({
  beforeLoad: () => {
    if (typeof window === "undefined") return;
    const raw = localStorage.getItem("po-portal-user") ?? sessionStorage.getItem("po-portal-user");
    if (!raw) throw redirect({ to: "/" });
  },
  component: AppLayout,
});

function todayIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function AppLayout() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const asOf = todayIso();
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
      queryKey: ["export-bill-overdue", "from-2026-04-01", "All Companies", DEFAULT_EXPORT_GROUP, asOf, 1, DEFAULT_PAGE_SIZE],
      queryFn: () =>
        getExportBillOverdue({
          company: "All Companies",
          groupName: DEFAULT_EXPORT_GROUP,
          asOf,
          page: 1,
          pageSize: DEFAULT_PAGE_SIZE,
        }),
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: ["sales-dashboard-companies"],
      queryFn: getSalesCompanies,
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: [
        "sales-kpis",
        DEFAULT_SALES_FILTERS.category,
        DEFAULT_SALES_FILTERS.company,
        DEFAULT_SALES_FILTERS.dateFrom,
        DEFAULT_SALES_FILTERS.dateTo,
        0,
      ],
      queryFn: () =>
        getSalesKpis({
          company: DEFAULT_SALES_FILTERS.company,
          dateFrom: DEFAULT_SALES_FILTERS.dateFrom,
          dateTo: DEFAULT_SALES_FILTERS.dateTo,
          category: DEFAULT_SALES_FILTERS.category,
        }),
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: [
        "sales-trend",
        DEFAULT_SALES_FILTERS.category,
        DEFAULT_SALES_FILTERS.company,
        DEFAULT_SALES_FILTERS.dateTo,
        0,
      ],
      queryFn: () =>
        getSalesYearlyTrend({
          company: DEFAULT_SALES_FILTERS.company,
          dateTo: DEFAULT_SALES_FILTERS.dateTo,
          category: DEFAULT_SALES_FILTERS.category,
        }),
      staleTime,
    });
    void queryClient.prefetchQuery({
      queryKey: [
        "sales-tables",
        DEFAULT_SALES_FILTERS.company,
        DEFAULT_SALES_FILTERS.dateFrom,
        DEFAULT_SALES_FILTERS.dateTo,
        0,
      ],
      queryFn: () =>
        getSalesTables({
          company: DEFAULT_SALES_FILTERS.company,
          dateFrom: DEFAULT_SALES_FILTERS.dateFrom,
          dateTo: DEFAULT_SALES_FILTERS.dateTo,
        }),
      staleTime,
    });
  }, [queryClient]);

  return <AppShell />;
}
