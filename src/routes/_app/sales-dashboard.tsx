import { useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { SalesFilters } from "@/components/sales/SalesFilters";
import { SalesKpiCards } from "@/components/sales/SalesKpiCards";
import { SalesCharts } from "@/components/sales/SalesCharts";
import { SalesSummaryTables } from "@/components/sales/SalesSummaryTables";
import {
  DEFAULT_SALES_FILTERS,
  getSalesCompanies,
  getSalesOverview,
  getTopSuppliers,
} from "@/lib/sales-dashboard-api";
import type {
  SalesDashboardFilters,
  SalesDashboardSummary,
} from "@/lib/sales-dashboard-types";

export const Route = createFileRoute("/_app/sales-dashboard")({
  head: () => ({ meta: [{ title: "Sales Dashboard — PO Approval Portal" }] }),
  component: SalesDashboardPage,
});

const EMPTY_SUMMARY: SalesDashboardSummary = {
  totalSales: 0,
  totalQuantity: 0,
  averageRate: 0,
  gstAmount: 0,
  totalPurchase: 0,
  grossProfit: 0,
  totalSalesChangePercent: 0,
  totalQuantityChangePercent: 0,
  averageRateChangePercent: 0,
  gstAmountChangePercent: 0,
  totalPurchaseChangePercent: 0,
  grossProfitChangePercent: 0,
};

function SalesDashboardPage() {
  const [filters, setFilters] = useState<SalesDashboardFilters>(DEFAULT_SALES_FILTERS);
  const [refreshToken, setRefreshToken] = useState(0);
  const bypassCacheRef = useRef(false);
  const isPurchase = filters.category === "Purchase";
  const companyFilter = filters.company?.trim() || "All Companies";

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["sales-dashboard-companies"],
    queryFn: getSalesCompanies,
    staleTime: 30 * 60_000,
  });

  const overviewQuery = useQuery({
    queryKey: [
      "sales-overview",
      filters.category,
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      const data = await getSalesOverview({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        category: filters.category,
        refresh,
      });
      if (isPurchase) bypassCacheRef.current = false;
      return data;
    },
    staleTime: 2 * 60_000,
    gcTime: 30 * 60_000,
    placeholderData: keepPreviousData,
    retry: 1,
  });

  const suppliersQuery = useQuery({
    queryKey: [
      "sales-suppliers",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: async () => {
      const items = await getTopSuppliers({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        refresh: bypassCacheRef.current,
      });
      bypassCacheRef.current = false;
      return items;
    },
    staleTime: 60 * 60_000,
    gcTime: 4 * 60 * 60_000,
    retry: 1,
  });

  const companyOptions = companyList?.options ?? [];
  const overview = overviewQuery.data;

  function patchFilters(next: Partial<SalesDashboardFilters>) {
    setFilters((prev) => ({ ...prev, ...next }));
  }

  const summary = useMemo<SalesDashboardSummary>(() => {
    if (!overview) return EMPTY_SUMMARY;
    if (isPurchase) {
      return {
        ...EMPTY_SUMMARY,
        totalPurchase: overview.totalPurchase,
        totalQuantity: overview.totalQuantity,
        averageRate: overview.averageRate,
      };
    }
    return {
      ...EMPTY_SUMMARY,
      totalSales: overview.totalSales,
      totalQuantity: overview.totalQuantity,
      averageRate: overview.averageRate,
    };
  }, [isPurchase, overview]);

  return (
    <div className="space-y-5 pb-2 sm:space-y-6 md:space-y-7">
      <div className="card-3d rounded-2xl p-4 sm:p-5">
        <h1 className="text-xl font-semibold tracking-tight sm:text-2xl md:text-3xl">
          Sales Dashboard
        </h1>
        <p className="mt-1 text-xs text-muted-foreground sm:text-sm">
          Live ERP figures, excluding intercompany.
        </p>
      </div>

      <SalesFilters
        companyOptions={companyOptions}
        companiesLoading={companiesLoading}
        filters={filters}
        isRefreshing={overviewQuery.isFetching || suppliersQuery.isFetching}
        onChange={patchFilters}
        onRefresh={() => {
          bypassCacheRef.current = true;
          setRefreshToken((n) => n + 1);
        }}
      />

      {overviewQuery.isFetching && (
        <div
          className="flex items-center gap-2 text-xs text-muted-foreground"
          aria-live="polite"
        >
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          Loading {isPurchase ? "purchase" : "sales"} data…
        </div>
      )}
      {overviewQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Failed to load dashboard data. Please try again.
        </p>
      )}

      <SalesKpiCards summary={summary} isPurchase={isPurchase} />

      <SalesCharts
        byGroup={overview?.byGroup ?? []}
        bySubGroup={overview?.bySubGroup ?? []}
        trend={overview?.trend ?? []}
        trendLoading={overviewQuery.isFetching && !overview}
      />

      <SalesSummaryTables
        exportCustomers={overview?.exportCustomers ?? []}
        suppliers={
          isPurchase ? (overview?.suppliers ?? []) : (suppliersQuery.data ?? [])
        }
        byCountry={overview?.byCountry ?? []}
        countryPeriodLabel={overview?.countryPeriodLabel}
        isPurchase={isPurchase}
        suppliersLoading={suppliersQuery.isFetching && !suppliersQuery.data}
        totalSales={overview?.totalSales ?? 0}
      />
    </div>
  );
}
