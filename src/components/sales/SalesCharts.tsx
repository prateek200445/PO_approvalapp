import type {
  SalesByGroupItem,
  SalesBySubGroupItem,
  SalesTrendPeriod,
} from "@/lib/sales-dashboard-types";
import { formatCrores, formatSalesCurrency } from "@/lib/sales-dashboard-api";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  XAxis,
  YAxis,
} from "recharts";

const GROUP_COLORS = [
  "var(--primary)",
  "var(--success)",
  "var(--warning)",
  "oklch(0.55 0.16 300)",
  "oklch(0.6 0.12 220)",
  "oklch(0.55 0.05 250)",
];

interface SalesChartsProps {
  byGroup: SalesByGroupItem[];
  bySubGroup: SalesBySubGroupItem[];
  /** Kept for filter state; trend chart is placeholder until ERP date series exists. */
  trendPeriod?: SalesTrendPeriod;
  onTrendPeriodChange?: (period: SalesTrendPeriod) => void;
}

export function SalesCharts({ byGroup, bySubGroup }: SalesChartsProps) {
  const groupConfig = Object.fromEntries(
    byGroup.map((g, i) => [
      g.groupName,
      { label: g.groupName, color: GROUP_COLORS[i % GROUP_COLORS.length] },
    ]),
  ) satisfies ChartConfig;

  const subGroupConfig = {
    salesAmount: { label: "Sales", color: "var(--primary)" },
  } satisfies ChartConfig;

  const pieData = byGroup.map((g, i) => ({
    name: g.groupName,
    value: g.amount,
    percentage: g.percentage,
    fill: GROUP_COLORS[i % GROUP_COLORS.length],
  }));

  return (
    <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-3">
      <section className="rounded-xl border border-dashed border-border bg-card/50 shadow-sm lg:col-span-1">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales Trend (₹)</h2>
        </header>
        <div className="flex min-h-[140px] items-center justify-center px-4 py-8 text-center text-xs text-muted-foreground sm:min-h-0 sm:aspect-[16/10] sm:py-0 sm:text-sm">
          Coming soon — no date series in SP_Sales_EBIDTA
        </div>
      </section>

      <section className="rounded-xl border border-border bg-card shadow-sm">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Group</h2>
        </header>
        <div className="p-3 sm:p-4">
          {byGroup.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground sm:aspect-square sm:max-h-[240px] sm:h-auto">
              No group data
            </div>
          ) : (
            <>
              <ChartContainer
                config={groupConfig}
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
                    data={pieData}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={45}
                    outerRadius={72}
                    strokeWidth={2}
                  >
                    {pieData.map((entry) => (
                      <Cell key={entry.name} fill={entry.fill} />
                    ))}
                  </Pie>
                </PieChart>
              </ChartContainer>
              <ul className="mt-2 grid grid-cols-1 gap-x-3 gap-y-1.5 text-xs sm:grid-cols-2">
                {byGroup.map((g, i) => (
                  <li key={g.groupName} className="flex min-w-0 items-center gap-1.5">
                    <span
                      className="h-2 w-2 shrink-0 rounded-[2px]"
                      style={{ backgroundColor: GROUP_COLORS[i % GROUP_COLORS.length] }}
                    />
                    <span className="truncate text-muted-foreground">{g.groupName}</span>
                    <span className="ml-auto font-medium tabular-nums">{g.percentage}%</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>
      </section>

      <section className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Sub Group (₹)</h2>
        </header>
        <div className="p-2 sm:p-4 overflow-x-auto">
          {bySubGroup.length === 0 ? (
            <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground sm:aspect-[16/10] sm:h-auto">
              No sub-group data
            </div>
          ) : (
            <ChartContainer
              config={subGroupConfig}
              className="min-w-[280px] aspect-[16/11] w-full sm:min-w-0 sm:aspect-[16/10]"
            >
              <BarChart
                data={bySubGroup}
                margin={{ left: 0, right: 4, top: 8, bottom: 28 }}
              >
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis
                  dataKey="subGroupName"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={6}
                  fontSize={9}
                  interval={0}
                  angle={-28}
                  textAnchor="end"
                  height={48}
                  tickFormatter={(v) => String(v).slice(0, 10)}
                />
                <YAxis
                  tickLine={false}
                  axisLine={false}
                  width={36}
                  fontSize={10}
                  tickFormatter={(v) => `${(Number(v) / 1e7).toFixed(1)}Cr`}
                />
                <ChartTooltip
                  content={
                    <ChartTooltipContent
                      formatter={(value) => formatSalesCurrency(Number(value))}
                    />
                  }
                />
                <Bar dataKey="salesAmount" fill="var(--color-salesAmount)" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ChartContainer>
          )}
        </div>
      </section>
    </div>
  );
}
