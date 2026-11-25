import { useEffect, useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { SalesFilters } from "@/components/sales/SalesFilters";
import { SalesKpiCards } from "@/components/sales/SalesKpiCards";
import { SalesCharts } from "@/components/sales/SalesCharts";
import { SalesSummaryTables } from "@/components/sales/SalesSummaryTables";
import { SalesExportAnalysis } from "@/components/sales/SalesExportAnalysis";
import { FibcProductionMom } from "@/components/sales/FibcProductionMom";
import {
  DEFAULT_SALES_FILTERS,
  getSalesCompanies,
  getSalesKpis,
  getSalesTables,
  getSalesYearlyTrend,
  getTopDomesticCustomers,
  getTopExportCustomers,
  getTopSuppliers,
  getFibcProductionMonthly,
} from "@/lib/sales-dashboard-api";
import type {
  SalesDashboardFilters,
  SalesDashboardSummary,
  SalesDashboardSalesPage,
} from "@/lib/sales-dashboard-types";
import { cn } from "@/lib/utils";

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

const SALES_PAGES: { id: SalesDashboardSalesPage; label: string }[] = [
  { id: "overview", label: "Dashboard" },
  { id: "export-analysis", label: "Export analysis" },
];

function SalesDashboardPage() {
  const [filters, setFilters] = useState<SalesDashboardFilters>(DEFAULT_SALES_FILTERS);
  const [salesPage, setSalesPage] = useState<SalesDashboardSalesPage>("overview");
  const [refreshToken, setRefreshToken] = useState(0);
  const bypassCacheRef = useRef(false);
  const isPurchase = filters.category === "Purchase";
  const isFibcSection = filters.section === "FIBC Production";
  const isOverview = salesPage === "overview";
  const isExportAnalysis = salesPage === "export-analysis";
  const companyFilter = filters.company?.trim() || "All Companies";
  const includeIntercompany = filters.includeIntercompany;

  /**
   * Export analysis dates — follow the global Period/Date From/To by default so Custom
   * selections at the top actually drive the page. Per-card pickers can still override
   * Export vs Domestic independently until the global period changes again.
   */
  const [exportDateFrom, setExportDateFrom] = useState(filters.dateFrom);
  const [exportDateTo, setExportDateTo] = useState(filters.dateTo);
  const [domesticDateFrom, setDomesticDateFrom] = useState(filters.dateFrom);
  const [domesticDateTo, setDomesticDateTo] = useState(filters.dateTo);

  useEffect(() => {
    setExportDateFrom(filters.dateFrom);
    setExportDateTo(filters.dateTo);
    setDomesticDateFrom(filters.dateFrom);
    setDomesticDateTo(filters.dateTo);
  }, [filters.dateFrom, filters.dateTo]);

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["sales-dashboard-companies"],
    queryFn: getSalesCompanies,
    staleTime: 60 * 60_000,
  });

  const showSalesContent = !isFibcSection;
  const showOverviewQueries = showSalesContent && isOverview;
  const showExportQueries = showSalesContent && isExportAnalysis && !isPurchase;

  const kpisQuery = useQuery({
    queryKey: [
      "sales-kpis",
      filters.category,
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getSalesKpis({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        category: filters.category,
        includeIntercompany,
        refresh: bypassCacheRef.current,
      }),
    enabled: showOverviewQueries,
    ...queryOptions,
  });

  const trendQuery = useQuery({
    queryKey: [
      "sales-trend",
      filters.category,
      companyFilter,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getSalesYearlyTrend({
        company: companyFilter,
        dateTo: filters.dateTo,
        category: filters.category,
        includeIntercompany,
        refresh: bypassCacheRef.current,
      }),
    enabled: showOverviewQueries,
    ...queryOptions,
  });

  const tablesQuery = useQuery({
    queryKey: [
      "sales-tables",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getSalesTables({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        includeIntercompany,
        refresh: bypassCacheRef.current,
        top: 10,
      }),
    enabled: showOverviewQueries && !isPurchase,
    ...queryOptions,
  });

  /** Overview Top 10 export customers — global dashboard dates. */
  const overviewExportQuery = useQuery({
    queryKey: [
      "sales-export-customers-top10",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getTopExportCustomers({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        top: 10,
        includeIntercompany,
        refresh: bypassCacheRef.current,
      }),
    enabled: showOverviewQueries && !isPurchase,
    ...queryOptions,
  });

  /** Overview Top 10 domestic customers — global dashboard dates. */
  const overviewDomesticQuery = useQuery({
    queryKey: [
      "sales-domestic-customers-top10",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getTopDomesticCustomers({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        top: 10,
        includeIntercompany,
        refresh: bypassCacheRef.current,
      }),
    enabled: showOverviewQueries && !isPurchase,
    ...queryOptions,
  });

  const suppliersQuery = useQuery({
    queryKey: [
      "sales-suppliers",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getTopSuppliers({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        top: 10,
        includeIntercompany,
        refresh: bypassCacheRef.current,
      }),
    enabled: showOverviewQueries,
    ...queryOptions,
  });

  /** No keepPreviousData — otherwise new From/To labels sit on the previous period's totals. */
  const exportAnalysisQueryOptions = {
    staleTime: queryOptions.staleTime,
    gcTime: queryOptions.gcTime,
    retry: queryOptions.retry,
    refetchOnWindowFocus: queryOptions.refetchOnWindowFocus,
  };

  /** Export analysis — full period. BillAMount (~74 Cr Oct-2026), not AccessableValue. */
  const exportCustomersQuery = useQuery({
    queryKey: [
      "sales-export-customers-full",
      "bill",
      companyFilter,
      exportDateFrom,
      exportDateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getTopExportCustomers({
        company: companyFilter,
        dateFrom: exportDateFrom,
        dateTo: exportDateTo,
        top: 0,
        includeIntercompany,
        refresh: bypassCacheRef.current,
        valueBasis: "bill",
      }),
    enabled: showExportQueries,
    ...exportAnalysisQueryOptions,
  });

  const domesticCustomersQuery = useQuery({
    queryKey: [
      "sales-domestic-customers-full",
      "bill",
      companyFilter,
      domesticDateFrom,
      domesticDateTo,
      includeIntercompany,
      refreshToken,
    ],
    queryFn: () =>
      getTopDomesticCustomers({
        company: companyFilter,
        dateFrom: domesticDateFrom,
        dateTo: domesticDateTo,
        top: 0,
        includeIntercompany,
        refresh: bypassCacheRef.current,
        valueBasis: "bill",
      }),
    enabled: showExportQueries,
    ...exportAnalysisQueryOptions,
  });

  const fibcProdQuery = useQuery({
    queryKey: [
      "sales-fibc-production-mom",
      companyFilter,
      filters.dateFrom,
      filters.dateTo,
      refreshToken,
    ],
    queryFn: () =>
      getFibcProductionMonthly({
        company: companyFilter,
        dateFrom: filters.dateFrom,
        dateTo: filters.dateTo,
        refresh: bypassCacheRef.current,
      }),
    enabled: isFibcSection,
    ...queryOptions,
  });

  useEffect(() => {
    const busy = isFibcSection
      ? fibcProdQuery.isFetching
      : isExportAnalysis
        ? exportCustomersQuery.isFetching || domesticCustomersQuery.isFetching
        : kpisQuery.isFetching ||
          trendQuery.isFetching ||
          tablesQuery.isFetching ||
          overviewExportQuery.isFetching ||
          overviewDomesticQuery.isFetching ||
          suppliersQuery.isFetching;
    if (busy) return;
    bypassCacheRef.current = false;
  }, [
    isFibcSection,
    isExportAnalysis,
    kpisQuery.isFetching,
    trendQuery.isFetching,
    tablesQuery.isFetching,
    overviewExportQuery.isFetching,
    overviewDomesticQuery.isFetching,
    exportCustomersQuery.isFetching,
    domesticCustomersQuery.isFetching,
    suppliersQuery.isFetching,
    fibcProdQuery.isFetching,
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

  const isRefreshing = isFibcSection
    ? fibcProdQuery.isFetching
    : isExportAnalysis
      ? exportCustomersQuery.isFetching || domesticCustomersQuery.isFetching
      : kpisQuery.isFetching ||
        trendQuery.isFetching ||
        tablesQuery.isFetching ||
        overviewExportQuery.isFetching ||
        overviewDomesticQuery.isFetching ||
        suppliersQuery.isFetching;

  const overviewTablesLoading =
    (overviewExportQuery.isFetching && !overviewExportQuery.data) ||
    (overviewDomesticQuery.isFetching && !overviewDomesticQuery.data) ||
    (tablesQuery.isFetching && !tablesQuery.data) ||
    (suppliersQuery.isFetching && !suppliersQuery.data);

  const exportAnalysisLoading =
    exportCustomersQuery.isFetching || domesticCustomersQuery.isFetching;

  return (
    <div className="space-y-5 pb-2 sm:space-y-6 md:space-y-7">
      <div className="card-3d rounded-2xl p-4 sm:p-5">
        <h1 className="text-xl font-semibold tracking-tight sm:text-2xl md:text-3xl">
          Sales Dashboard
        </h1>
        <p className="mt-1 text-xs text-muted-foreground sm:text-sm">
          {isFibcSection
            ? "FIBC bag production month-on-month from ERP. Use company and date filters below."
            : isExportAnalysis
              ? "Full-period export and domestic customer lists, PDF/Excel packs, and group company pivot."
              : "Live ERP figures. Switch to Incl. intercompany for the same KPIs with IC, and tick companies to combine units."}
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

      {isFibcSection ? (
        <>
          {fibcProdQuery.isFetching && !fibcProdQuery.data && (
            <div
              className="flex items-center gap-2 text-xs text-muted-foreground"
              aria-live="polite"
            >
              <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
              Loading FIBC production…
            </div>
          )}
          <FibcProductionMom
            months={fibcProdQuery.data?.months ?? []}
            byBagType={fibcProdQuery.data?.byBagType ?? []}
            totalPcs={fibcProdQuery.data?.totalPcs ?? 0}
            totalWt={fibcProdQuery.data?.totalWt ?? 0}
            loading={fibcProdQuery.isFetching && !fibcProdQuery.data}
            error={
              fibcProdQuery.isError
                ? fibcProdQuery.error instanceof Error
                  ? fibcProdQuery.error.message
                  : "Failed to load FIBC production"
                : null
            }
          />
        </>
      ) : (
        <>
          <fieldset className="space-y-1.5">
            <legend className="sr-only">Sales page</legend>
            <div
              className="flex w-full overflow-x-auto rounded-md border border-border bg-card p-0.5 shadow-soft"
              role="radiogroup"
              aria-label="Sales dashboard page"
            >
              {SALES_PAGES.map((opt) => (
                <button
                  key={opt.id}
                  type="button"
                  role="radio"
                  aria-checked={salesPage === opt.id}
                  onClick={() => setSalesPage(opt.id)}
                  className={cn(
                    "min-h-11 shrink-0 flex-1 touch-manipulation rounded-sm px-2.5 py-2 text-xs font-medium transition-colors sm:text-sm",
                    salesPage === opt.id
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </fieldset>

          {isExportAnalysis ? (
            <>
              {isPurchase ? (
                <p className="text-sm text-muted-foreground">
                  Export analysis is available for Sales category. Switch Category to Sales above.
                </p>
              ) : (
                <>
                  {exportAnalysisLoading && (
                    <div
                      className="flex items-center gap-2 text-xs text-muted-foreground"
                      aria-live="polite"
                    >
                      <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
                      Loading export analysis…
                    </div>
                  )}
                  {isRefreshing && !exportAnalysisLoading && (
                    <div
                      className="flex items-center gap-2 text-xs text-muted-foreground"
                      aria-live="polite"
                    >
                      <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
                      Updating export analysis…
                    </div>
                  )}
                  {(exportCustomersQuery.isError || domesticCustomersQuery.isError) && (
                    <p className="text-xs text-destructive" role="alert">
                      Failed to load export analysis data. Please try again.
                    </p>
                  )}
                  <SalesExportAnalysis
                    exportCustomers={exportCustomersQuery.data ?? []}
                    domesticCustomers={domesticCustomersQuery.data ?? []}
                    loading={exportAnalysisLoading}
                    includeIntercompany={includeIntercompany}
                    exportDateFrom={exportDateFrom}
                    exportDateTo={exportDateTo}
                    onExportDateFromChange={setExportDateFrom}
                    onExportDateToChange={setExportDateTo}
                    domesticDateFrom={domesticDateFrom}
                    domesticDateTo={domesticDateTo}
                    onDomesticDateFromChange={setDomesticDateFrom}
                    onDomesticDateToChange={setDomesticDateTo}
                    pdfContext={{
                      company: companyFilter,
                      dateFrom: filters.dateFrom,
                      dateTo: filters.dateTo,
                    }}
                  />
                </>
              )}
            </>
          ) : (
            <>
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
                includeIntercompany={includeIntercompany}
              />

              <SalesSummaryTables
                exportCustomers={overviewExportQuery.data ?? []}
                domesticCustomers={overviewDomesticQuery.data ?? []}
                suppliers={suppliersQuery.data ?? []}
                byCountry={tablesQuery.data?.byCountry ?? []}
                countryPeriodLabel={tablesQuery.data?.countryPeriodLabel}
                isPurchase={isPurchase}
                suppliersLoading={overviewTablesLoading}
                totalSales={kpis?.totalSales ?? 0}
                includeIntercompany={includeIntercompany}
                pdfContext={{
                  company: companyFilter,
                  dateFrom: filters.dateFrom,
                  dateTo: filters.dateTo,
                  totalAmount: isPurchase ? kpis?.totalPurchase : kpis?.totalSales,
                }}
              />
            </>
          )}
        </>
      )}
    </div>
  );
}
