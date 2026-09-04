import type {
  RankedPartyItem,
  SalesByCountryItem,
} from "@/lib/sales-dashboard-types";
import { formatCrores, formatSalesCurrency } from "@/lib/sales-dashboard-api";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Cell, Pie, PieChart } from "recharts";
import { cn } from "@/lib/utils";

const COUNTRY_COLORS = [
  "var(--primary)",
  "var(--success)",
  "var(--warning)",
  "oklch(0.55 0.16 300)",
  "oklch(0.6 0.12 220)",
  "oklch(0.55 0.05 250)",
  "oklch(0.62 0.14 40)",
  "oklch(0.58 0.1 180)",
];

interface SalesSummaryTablesProps {
  exportCustomers: RankedPartyItem[];
  suppliers: RankedPartyItem[];
  byCountry: SalesByCountryItem[];
  countryPeriodLabel?: string;
  isPurchase?: boolean;
  suppliersLoading?: boolean;
  totalSales?: number;
  includeIntercompany?: boolean;
}

function EmptyRow({ colSpan, message = "No data available" }: { colSpan: number; message?: string }) {
  return (
    <TableRow>
      <TableCell colSpan={colSpan} className="py-6 text-center text-sm text-muted-foreground">
        {message}
      </TableCell>
    </TableRow>
  );
}

function RankingTable({
  title,
  subtitle,
  nameHeader,
  items,
  loading = false,
}: {
  title: string;
  subtitle: string;
  nameHeader: string;
  items: RankedPartyItem[];
  loading?: boolean;
}) {
  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
      <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
        <h2 className="text-sm font-semibold">{title}</h2>
        <p className="mt-0.5 text-[11px] text-muted-foreground">{subtitle}</p>
      </header>
      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">#</TableHead>
              <TableHead>{nameHeader}</TableHead>
              <TableHead className="text-right">Amount (₹)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && items.length === 0 ? (
              <EmptyRow colSpan={3} message="Loading…" />
            ) : items.length === 0 ? (
              <EmptyRow colSpan={3} />
            ) : (
              items.map((item) => (
                <TableRow key={`${item.rank}-${item.name}`}>
                  <TableCell className="tabular-nums text-muted-foreground">{item.rank}</TableCell>
                  <TableCell className="min-w-0">
                    <div className="truncate font-medium">{item.name}</div>
                    {item.country ? (
                      <div className="truncate text-[11px] text-muted-foreground">{item.country}</div>
                    ) : null}
                  </TableCell>
                  <TableCell className="text-right text-xs tabular-nums whitespace-nowrap sm:text-sm">
                    {formatSalesCurrency(item.amount)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </section>
  );
}

export function SalesSummaryTables({
  exportCustomers,
  suppliers,
  byCountry,
  countryPeriodLabel,
  isPurchase = false,
  suppliersLoading = false,
  totalSales = 0,
  includeIntercompany = false,
}: SalesSummaryTablesProps) {
  const icNote = includeIntercompany ? "Incl. intercompany" : "Excl. intercompany";
  const countryBase = totalSales > 0
    ? totalSales
    : byCountry.reduce((sum, c) => sum + (Number(c.salesAmount) || 0), 0);
  const countryPieData = byCountry.map((c, i) => {
    const value = Number(c.salesAmount) || 0;
    const percentage = countryBase > 0 ? Math.round((value / countryBase) * 1000) / 10 : 0;
    return {
      name: c.countryName,
      value,
      percentage,
      fill: COUNTRY_COLORS[i % COUNTRY_COLORS.length],
    };
  });

  const countryConfig = Object.fromEntries(
    byCountry.map((c, i) => [
      c.countryName,
      { label: c.countryName, color: COUNTRY_COLORS[i % COUNTRY_COLORS.length] },
    ]),
  ) satisfies ChartConfig;

  return (
    <div className="space-y-3 sm:space-y-4">
      <div className={cn("grid grid-cols-1 gap-3 sm:gap-4", isPurchase ? "lg:grid-cols-1" : "lg:grid-cols-3")}>
        {!isPurchase && (
          <RankingTable
            title="Top 10 Export Customers"
            subtitle={includeIntercompany ? "Excl. India, including intercompany" : "Excl. India and intercompany"}
            nameHeader="Customer"
            items={exportCustomers}
            loading={suppliersLoading}
          />
        )}

        {!isPurchase && (
          <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
            <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
              <h2 className="text-sm font-semibold">Top 10 Countries by Sales</h2>
              <p className="mt-0.5 text-[11px] text-muted-foreground">
                {countryPeriodLabel
                  ? `${includeIntercompany ? "Excl. India, including intercompany" : "Excl. India and intercompany"} · ${countryPeriodLabel}`
                  : includeIntercompany
                    ? "Excl. India, including intercompany"
                    : "Excl. India and intercompany"}
              </p>
            </header>
            <div className="p-3 sm:p-4">
              {suppliersLoading && byCountry.length === 0 ? (
                <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground sm:aspect-square sm:h-auto sm:max-h-[240px]">
                  Loading…
                </div>
              ) : byCountry.length === 0 ? (
                <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground sm:aspect-square sm:h-auto sm:max-h-[240px]">
                  No country sales for this filter
                </div>
              ) : (
                <>
                  <ChartContainer
                    config={countryConfig}
                    className="mx-auto aspect-square max-h-[200px] w-full sm:max-h-[240px]"
                  >
                    <PieChart>
                      <ChartTooltip
                        content={
                          <ChartTooltipContent
                            hideLabel
                            formatter={(value, name, item) => (
                              <div className="flex w-full max-w-[220px] items-center justify-between gap-3">
                                <span className="truncate text-muted-foreground">{String(name)}</span>
                                <span className="shrink-0 font-medium tabular-nums">
                                  {formatCrores(Number(value))}
                                  {item?.payload?.percentage != null ? ` (${item.payload.percentage}%)` : ""}
                                </span>
                              </div>
                            )}
                          />
                        }
                      />
                      <Pie
                        data={countryPieData}
                        dataKey="value"
                        nameKey="name"
                        innerRadius={45}
                        outerRadius={72}
                        strokeWidth={2}
                      >
                        {countryPieData.map((entry) => (
                          <Cell key={entry.name} fill={entry.fill} />
                        ))}
                      </Pie>
                    </PieChart>
                  </ChartContainer>
                  <ul className="mt-2 grid grid-cols-1 gap-x-3 gap-y-1.5 text-xs sm:grid-cols-2">
                    {countryPieData.map((c, i) => (
                      <li key={c.name} className="flex min-w-0 items-center gap-1.5">
                        <span
                          className="h-2 w-2 shrink-0 rounded-[2px]"
                          style={{ backgroundColor: COUNTRY_COLORS[i % COUNTRY_COLORS.length] }}
                        />
                        <span className="truncate text-muted-foreground">{c.name}</span>
                        <span className="ml-auto font-medium tabular-nums">{c.percentage}%</span>
                      </li>
                    ))}
                  </ul>
                </>
              )}
            </div>
          </section>
        )}

        <RankingTable
          title="Top 10 Suppliers"
          subtitle={icNote}
          nameHeader="Supplier"
          items={suppliers}
          loading={suppliersLoading}
        />
      </div>
    </div>
  );
}
