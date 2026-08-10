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
  getSalesCompanies,
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

/** KPI keys reserved until ERP wiring is ready. */
const PLACEHOLDER_FIELDS = [
  "gstAmount",
  "totalPurchase",
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

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["sales-dashboard-companies"],
    queryFn: getSalesCompanies,
    staleTime: 5 * 60_000,
  });

  const totalsQuery = useQuery({
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
    void totalsQuery.refetch();
  }

  const summary = useMemo<SalesDashboardSummary>(
    () => ({
      ...EMPTY_SUMMARY,
      totalSales: totalsQuery.data?.totalSales ?? 0,
      totalQuantity: totalsQuery.data?.totalQuantity ?? 0,
      averageRate: totalsQuery.data?.averageRate ?? 0,
    }),
    [totalsQuery.data],
  );

  const byGroup = totalsQuery.data?.byGroup ?? [];
  const bySubGroup = totalsQuery.data?.bySubGroup ?? [];

  const companies = useMemo(() => {
    if (companyList?.length) return companyList;
    return ["All Companies"];
  }, [companyList]);

  return (
    <div className="space-y-5 md:space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Sales Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Live KPIs, Sales by Group & Sub Group from `SP_Sales_EBIDTA`. Other sections coming soon.
        </p>
      </div>

      <SalesFilters
        companies={companies}
        filters={filters}
        isRefreshing={totalsQuery.isFetching}
        onChange={patchFilters}
        onRefresh={handleRefresh}
      />

      {companiesLoading && (
        <p className="text-xs text-muted-foreground">Loading companies from FactoryInfo…</p>
      )}

      {totalsQuery.isFetching && (
        <div
          className="flex items-center gap-2 text-xs text-muted-foreground"
          aria-live="polite"
        >
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          Loading live data from SP_Sales_EBIDTA…
        </div>
      )}
      {totalsQuery.isError && (
        <p className="text-xs text-destructive" role="alert">
          Live SP_Sales_EBIDTA failed.{" "}
          {totalsQuery.error instanceof Error ? totalsQuery.error.message : ""}
        </p>
      )}

      <SalesKpiCards summary={summary} unavailableFields={PLACEHOLDER_FIELDS} />

      <SalesCharts byGroup={byGroup} bySubGroup={bySubGroup} />

      <SalesSummaryTables topProducts={[]} topCustomers={[]} bySubGroup={bySubGroup} />

      <ComingSoonSection
        title="Detailed sales analysis"
        description="Line-level sales analysis grid will appear here later."
      />
    </div>
  );
}
