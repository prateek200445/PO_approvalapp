import { useEffect, useMemo, useRef, useState } from "react";
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
  getSalesKpis,
  getSalesTables,
  getSalesYearlyTrend,
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

const queryOptions = {
  staleTime: 60 * 60_000,
  gcTime: 4 * 60 * 60_000,
  placeholderData: keepPreviousData,
  retry: 1 as const,
  refetchOnWindowFocus: false,
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
    staleTime: 60 * 60_000,
  });

  const kpisQuery = useQuery({
    queryKey: [
      "sales-kpis",
      filters.category,
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getSalesKpis({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        category: filters.category,
        refresh: bypassCacheRef.current,
      }),
    ...queryOptions,
  });

  const trendQuery = useQuery({
    queryKey: [
      "sales-trend",
      filters.category,
      companyFilter,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getSalesYearlyTrend({
        company: companyFilter,
        dateTo: filters.dateTo,
        category: filters.category,
        refresh: bypassCacheRef.current,
      }),
    ...queryOptions,
  });

  const tablesQuery = useQuery({
    queryKey: [
      "sales-tables",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getSalesTables({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        refresh: bypassCacheRef.current,
      }),
    enabled: !isPurchase,
    ...queryOptions,
  });

  const suppliersQuery = useQuery({
    queryKey: [
      "sales-suppliers",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getTopSuppliers({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        top: 10,
        refresh: bypassCacheRef.current,
      }),
    enabled: true,
    ...queryOptions,
  });

  useEffect(() => {
    if (
      kpisQuery.isFetching ||
      trendQuery.isFetching ||
      tablesQuery.isFetching ||
      suppliersQuery.isFetching
    ) {
      return;
    }
    bypassCacheRef.current = false;
  }, [
    kpisQuery.isFetching,
    trendQuery.isFetching,
    tablesQuery.isFetching,
    suppliersQuery.isFetching,
  ]);

  const companyOptions = companyList?.options ?? [];
  const kpis = kpisQuery.data;

  function patchFilters(next: Partial<SalesDashboardFilters>) {
    setFilters((prev) => ({ ...prev, ...next }));
  }

  const summary = useMemo<SalesDashboardSummary>(() => {
    if (!kpis) return EMPTY_SUMMARY;
    if (isPurchase) {
      return {
        ...EMPTY_SUMMARY,
        totalPurchase: kpis.totalPurchase,
        totalQuantity: kpis.totalQuantity,
        averageRate: kpis.averageRate,
      };
    }
    return {
      ...EMPTY_SUMMARY,
      totalSales: kpis.totalSales,
      totalQuantity: kpis.totalQuantity,
      averageRate: kpis.averageRate,
    };
  }, [isPurchase, kpis]);

  const isRefreshing =
    kpisQuery.isFetching ||
    trendQuery.isFetching ||
    tablesQuery.isFetching ||
    suppliersQuery.isFetching;

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
        isRefreshing={isRefreshing}
        onChange={patchFilters}
        onRefresh={() => {
          bypassCacheRef.current = true;
          setRefreshToken((n) => n + 1);
        }}
      />

      {kpisQuery.isFetching && !kpis && (
        <div
          className="flex items-center gap-2 text-xs text-muted-foreground"
          aria-live="polite"
        >
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          Loading {isPurchase ? "purchase" : "sales"} totals…
        </div>
      )}
      {isRefreshing && kpis && (
        <div
          className="flex items-center gap-2 text-xs text-muted-foreground"
          aria-live="polite"
        >
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          Updating charts and tables…
        </div>
      )}
      {kpisQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Failed to load dashboard data. Please try again.
        </p>
      )}

      <SalesKpiCards
        summary={summary}
        isPurchase={isPurchase}
        loading={kpisQuery.isFetching && !kpis}
      />

      <SalesCharts
        byGroup={kpis?.byGroup ?? []}
        bySubGroup={kpis?.bySubGroup ?? []}
        trend={trendQuery.data ?? []}
        trendLoading={trendQuery.isFetching && !trendQuery.data}
        groupsLoading={kpisQuery.isFetching && !kpis}
      />

      <SalesSummaryTables
        exportCustomers={tablesQuery.data?.exportCustomers ?? []}
        suppliers={suppliersQuery.data ?? []}
        byCountry={tablesQuery.data?.byCountry ?? []}
        countryPeriodLabel={tablesQuery.data?.countryPeriodLabel}
        isPurchase={isPurchase}
        suppliersLoading={suppliersQuery.isFetching && !suppliersQuery.data}
        totalSales={kpis?.totalSales ?? 0}
      />
    </div>
  );
}
