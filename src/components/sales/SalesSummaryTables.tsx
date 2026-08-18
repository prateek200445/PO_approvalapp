import type {
  SalesByCountryItem,
  SalesBySubGroupItem,
  TopProduct,
} from "@/lib/sales-dashboard-types";
import { formatCrores, formatSalesCurrency, formatSalesQuantity } from "@/lib/sales-dashboard-api";
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
  topProducts: TopProduct[];
  byCountry: SalesByCountryItem[];
  /** e.g. "FY 25-26" from API — shown under Sales by Country */
  countryPeriodLabel?: string;
  bySubGroup: SalesBySubGroupItem[];
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

export function SalesSummaryTables({
  topProducts,
  byCountry,
  countryPeriodLabel,
  bySubGroup,
}: SalesSummaryTablesProps) {
  const countryTotal = byCountry.reduce((sum, c) => sum + (Number(c.salesAmount) || 0), 0);
  const countryPieData = byCountry.map((c, i) => {
    const value = Number(c.salesAmount) || 0;
    const percentage =
      countryTotal > 0 ? Math.round((value / countryTotal) * 1000) / 10 : 0;
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
    <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-3">
      <section className="rounded-2xl border border-border bg-card shadow-soft overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Top 5 Products by Sales</h2>
        </header>
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">#</TableHead>
                <TableHead>Product Name</TableHead>
                <TableHead className="text-right">Quantity</TableHead>
                <TableHead className="text-right">Sales (₹)</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {topProducts.length === 0 ? (
                <EmptyRow colSpan={4} message="Coming soon" />
              ) : (
                topProducts.map((p) => (
                  <TableRow key={p.rank}>
                    <TableCell className="tabular-nums text-muted-foreground">{p.rank}</TableCell>
                    <TableCell className="font-medium">{p.productName}</TableCell>
                    <TableCell className="text-right tabular-nums text-xs sm:text-sm">
                      {formatSalesQuantity(p.quantity)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums text-xs sm:text-sm">
                      {formatSalesCurrency(p.salesAmount)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
        <footer className="border-t border-border px-4 py-2.5">
          <button type="button" className="text-xs font-medium text-primary hover:underline">
            View All Products
          </button>
        </footer>
      </section>

      <section className="rounded-2xl border border-border bg-card shadow-soft overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Country</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">
            {countryPeriodLabel
              ? `Excl. intercompany · ${countryPeriodLabel}`
              : "Excl. intercompany · Top countries by sales"}
          </p>
        </header>
        <div className="p-3 sm:p-4">
          {byCountry.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground sm:aspect-square sm:max-h-[240px] sm:h-auto">
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
                              {item?.payload?.percentage != null
                                ? ` (${item.payload.percentage}%)`
                                : ""}
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

      <section className="rounded-2xl border border-border bg-card shadow-soft overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Sub Group</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">Excl. intercompany</p>
        </header>
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Sub Group Name</TableHead>
                <TableHead className="text-right">Quantity</TableHead>
                <TableHead className="text-right">Sales (₹)</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {bySubGroup.length === 0 ? (
                <EmptyRow colSpan={3} />
              ) : (
                bySubGroup.map((s) => (
                  <TableRow key={s.subGroupName}>
                    <TableCell className="max-w-[140px] truncate font-medium sm:max-w-none">
                      {s.subGroupName}
                    </TableCell>
                    <TableCell className="text-right tabular-nums text-xs sm:text-sm whitespace-nowrap">
                      {formatSalesQuantity(s.quantity)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums text-xs sm:text-sm whitespace-nowrap">
                      {formatSalesCurrency(s.salesAmount)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
        <footer className="border-t border-border px-4 py-2.5">
          <button type="button" className="text-xs font-medium text-primary hover:underline">
            View All Sub Groups
          </button>
        </footer>
      </section>
    </div>
  );
}
