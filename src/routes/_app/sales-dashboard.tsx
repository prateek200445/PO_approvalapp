import { useEffect, useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { SalesFilters } from "@/components/sales/SalesFilters";
import { SalesKpiCards } from "@/components/sales/SalesKpiCards";
import { SalesCharts } from "@/components/sales/SalesCharts";
import { SalesSummaryTables } from "@/components/sales/SalesSummaryTables";
import {
  DEFAULT_SALES_FILTERS,
  getSalesByCountry,
  getSalesCompanies,
  getSalesYearlyTrend,
  getTotalPurchase,
  getTotalSales,
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

/** KPI keys reserved until ERP wiring is ready (varies by category). */
const SALES_PLACEHOLDER_FIELDS = [
  "gstAmount",
  "totalPurchase",
  "grossProfit",
  "changePercents",
];

const PURCHASE_PLACEHOLDER_FIELDS = [
  "gstAmount",
  "totalSales",
  "grossProfit",
  "changePercents",
];

function ComingSoonSection({ title, description }: { title: string; description: string }) {
  return (
    <section className="rounded-xl border border-dashed border-border bg-card/50 px-4 py-10 text-center shadow-sm">
      <h2 className="text-sm font-medium text-foreground">{title}</h2>
      <p className="mt-1 text-xs text-muted-foreground">{description}</p>
    </section>
  );
}

function SalesDashboardPage() {
  const [filters, setFilters] = useState<SalesDashboardFilters>(DEFAULT_SALES_FILTERS);
  const [refreshToken, setRefreshToken] = useState(0);
  const isPurchase = filters.category === "Purchase";

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["sales-dashboard-companies"],
    queryFn: getSalesCompanies,
    staleTime: 5 * 60_000,
  });

  const salesTotalsQuery = useQuery({
    queryKey: [
      "sales-totals",
      filters.company,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getTotalSales({
        company: filters.company,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
      }),
    enabled: !isPurchase,
    staleTime: 60_000,
    retry: 1,
  });

  const purchaseTotalsQuery = useQuery({
    queryKey: [
      "purchase-totals",
      filters.company,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getTotalPurchase({
        company: filters.company,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
      }),
    enabled: isPurchase,
    staleTime: 60_000,
    retry: 1,
  });

  const yearlyTrendQuery = useQuery({
    queryKey: [
      "ebidta-yearly-trend",
      filters.category,
      filters.company,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getSalesYearlyTrend({
        company: filters.company,
        asOf: filters.dateTo,
        years: 5,
        category: filters.category,
      }),
    staleTime: 5 * 60_000,
    retry: 1,
  });

  const byCountryQuery = useQuery({
    queryKey: [
      "sales-by-country",
      filters.company,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getSalesByCountry({
        company: filters.company,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        top: 5,
      }),
    enabled: !isPurchase,
    staleTime: 60_000,
    retry: 1,
  });

  // Prefer a real FactoryInfo company once the list loads
  useEffect(() => {
    if (!companyList?.length) return;
    const realCompanies = companyList.filter((c) => c !== "All Companies");
    if (realCompanies.length === 0) return;
    const currentIsKnown = companyList.some(
      (c) => c.toLowerCase() === filters.company.toLowerCase(),
    );
    if (currentIsKnown) return;
    const preferred =
      realCompanies.find((c) => c.toLowerCase().includes("oswal extrusion limited")) ||
      realCompanies[0];
    setFilters((prev) => ({ ...prev, company: preferred }));
  }, [companyList, filters.company]);

  function patchFilters(next: Partial<SalesDashboardFilters>) {
    setFilters((prev) => ({ ...prev, ...next }));
  }

  function handleRefresh() {
    setRefreshToken((n) => n + 1);
    if (isPurchase) {
      void purchaseTotalsQuery.refetch();
    } else {
      void salesTotalsQuery.refetch();
      void byCountryQuery.refetch();
    }
    void yearlyTrendQuery.refetch();
  }

  const totalsFetching = isPurchase
    ? purchaseTotalsQuery.isFetching
    : salesTotalsQuery.isFetching;
  const totalsError = isPurchase ? purchaseTotalsQuery.isError : salesTotalsQuery.isError;

  const summary = useMemo<SalesDashboardSummary>(() => {
    if (isPurchase) {
      return {
        ...EMPTY_SUMMARY,
        totalPurchase: purchaseTotalsQuery.data?.totalPurchase ?? 0,
        totalQuantity: purchaseTotalsQuery.data?.totalQuantity ?? 0,
        averageRate: purchaseTotalsQuery.data?.averageRate ?? 0,
      };
    }
    return {
      ...EMPTY_SUMMARY,
      totalSales: salesTotalsQuery.data?.totalSales ?? 0,
      totalQuantity: salesTotalsQuery.data?.totalQuantity ?? 0,
      averageRate: salesTotalsQuery.data?.averageRate ?? 0,
    };
  }, [isPurchase, purchaseTotalsQuery.data, salesTotalsQuery.data]);

  const byGroup = isPurchase
    ? (purchaseTotalsQuery.data?.byGroup ?? [])
    : (salesTotalsQuery.data?.byGroup ?? []);
  const bySubGroup = isPurchase
    ? (purchaseTotalsQuery.data?.bySubGroup ?? [])
    : (salesTotalsQuery.data?.bySubGroup ?? []);

  const companies = useMemo(() => {
    if (companyList?.length) return companyList;
    return ["All Companies"];
  }, [companyList]);

  const placeholderFields = isPurchase
    ? PURCHASE_PLACEHOLDER_FIELDS
    : SALES_PLACEHOLDER_FIELDS;

  const isRefreshing =
    totalsFetching ||
    yearlyTrendQuery.isFetching ||
    (!isPurchase && byCountryQuery.isFetching);

  return (
    <div className="space-y-4 pb-2 sm:space-y-5 md:space-y-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight sm:text-2xl md:text-3xl">
          Sales Dashboard
        </h1>
        <p className="mt-1 text-xs text-muted-foreground sm:text-sm">
          <span className="sm:hidden">
            {isPurchase
              ? "Live purchase KPIs and group charts."
              : "Live sales KPIs, group charts, and country breakdown."}
          </span>
          <span className="hidden sm:inline">
            {isPurchase
              ? "Live purchase KPIs and group/sub-group charts for the selected company and date range."
              : "Live sales KPIs, group and sub-group charts, and country-wise breakdown for the selected company and date range."}
          </span>
        </p>
      </div>

      <SalesFilters
        companies={companies}
        filters={filters}
        isRefreshing={isRefreshing}
        onChange={patchFilters}
        onRefresh={handleRefresh}
      />

      {companiesLoading && (
        <p className="text-xs text-muted-foreground">Loading companies…</p>
      )}

      {isRefreshing && (
        <div
          className="flex items-center gap-2 text-xs text-muted-foreground"
          aria-live="polite"
        >
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          {isPurchase ? "Loading purchase data…" : "Loading sales data…"}
        </div>
      )}
      {totalsError && (
        <p className="text-xs text-destructive" role="alert">
          {isPurchase
            ? "Failed to load purchase totals. Please try again."
            : "Failed to load sales totals. Please try again."}
        </p>
      )}
      {yearlyTrendQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Failed to load yearly {isPurchase ? "purchase" : "sales"} trend. Please try again.
        </p>
      )}
      {!isPurchase && byCountryQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Failed to load sales by country. Please try again.
        </p>
      )}

      <SalesKpiCards summary={summary} unavailableFields={placeholderFields} />

      <SalesCharts
        byGroup={byGroup}
        bySubGroup={bySubGroup}
        trend={yearlyTrendQuery.data ?? []}
        trendLoading={yearlyTrendQuery.isFetching}
      />

      {isPurchase ? (
        <ComingSoonSection
          title="Purchase by country"
          description="Country-wise purchase breakdown will be added in a future update."
        />
      ) : (
        <SalesSummaryTables
          topProducts={[]}
          byCountry={byCountryQuery.data?.byCountry ?? []}
          countryPeriodLabel={byCountryQuery.data?.periodLabel}
          bySubGroup={bySubGroup}
        />
      )}

      <ComingSoonSection
        title={isPurchase ? "Detailed purchase analysis" : "Detailed sales analysis"}
        description={
          isPurchase
            ? "Line-level purchase analysis grid will appear here later."
            : "Line-level sales analysis grid will appear here later."
        }
      />
    </div>
  );
}
