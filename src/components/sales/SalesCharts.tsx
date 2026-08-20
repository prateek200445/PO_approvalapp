import type {
  SalesByGroupItem,
  SalesBySubGroupItem,
  SalesTrendItem,
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
  trend?: SalesTrendItem[];
  trendLoading?: boolean;
  groupsLoading?: boolean;
}

export function SalesCharts({
  byGroup,
  bySubGroup,
  trend = [],
  trendLoading = false,
  groupsLoading = false,
}: SalesChartsProps) {
  const groupConfig = Object.fromEntries(
    byGroup.map((g, i) => [
      g.groupName,
      { label: g.groupName, color: GROUP_COLORS[i % GROUP_COLORS.length] },
    ]),
  ) satisfies ChartConfig;

  const subGroupConfig = {
    salesAmount: { label: "Sales", color: "var(--primary)" },
  } satisfies ChartConfig;

  const trendConfig = {
    amount: { label: "Total Sales", color: "var(--primary)" },
  } satisfies ChartConfig;

  const pieData = byGroup.map((g, i) => ({
    name: g.groupName,
    value: Number(g.amount) || 0,
    percentage: g.percentage,
    fill: GROUP_COLORS[i % GROUP_COLORS.length],
  }));

  const subGroupChartData = bySubGroup.map((s) => ({
    subGroupName: s.subGroupName,
    salesAmount: Number(s.salesAmount) || 0,
  }));

  const trendChartData = trend.map((t) => ({
    period: t.period,
    amount: Number(t.amount) || 0,
  }));

  const subGroupMax = subGroupChartData.reduce(
    (max, row) => (row.salesAmount > max ? row.salesAmount : max),
    0,
  );
  const subGroupYMax = subGroupMax > 0 ? subGroupMax * 1.1 : 1;

  const trendMax = trendChartData.reduce(
    (max, row) => (row.amount > max ? row.amount : max),
    0,
  );
  const trendYMax = trendMax > 0 ? trendMax * 1.1 : 1;

  return (
    <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-3">
      <section className="rounded-2xl border border-border bg-card shadow-soft lg:col-span-1">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales Trend (₹)</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">
            Year by year (Indian FY) · Excl. intercompany
          </p>
        </header>
        <div className="p-2 sm:p-4">
          {trendLoading && trendChartData.length === 0 ? (
            <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground sm:aspect-[16/10] sm:h-auto">
              Loading yearly sales…
            </div>
          ) : trendChartData.length === 0 ? (
            <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground sm:aspect-[16/10] sm:h-auto">
              No yearly sales data
            </div>
          ) : (
            <ChartContainer
              config={trendConfig}
              className="aspect-[16/11] w-full sm:aspect-[16/10]"
            >
              <BarChart data={trendChartData} margin={{ left: 4, right: 4, top: 8, bottom: 8 }}>
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis
                  dataKey="period"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={6}
                  fontSize={10}
                  interval={0}
                />
                <YAxis
                  type="number"
                  domain={[0, trendYMax]}
                  allowDataOverflow={false}
                  tickLine={false}
                  axisLine={false}
                  width={48}
                  fontSize={10}
                  tickCount={5}
                  tickFormatter={(v) => `${(Number(v) / 1e7).toFixed(1)}Cr`}
                />
                <ChartTooltip
                  content={
                    <ChartTooltipContent
                      formatter={(value) => formatSalesCurrency(Number(value))}
                    />
                  }
                />
                <Bar
                  dataKey="amount"
                  fill="var(--color-amount)"
                  radius={[4, 4, 0, 0]}
                  isAnimationActive={false}
                />
              </BarChart>
            </ChartContainer>
          )}
        </div>
      </section>

      <section className="rounded-2xl border border-border bg-card shadow-soft">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Group</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">Excl. intercompany</p>
        </header>
        <div className="p-3 sm:p-4">
          {groupsLoading && byGroup.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground sm:aspect-square sm:max-h-[240px] sm:h-auto">
              Loading…
            </div>
          ) : byGroup.length === 0 ? (
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

      <section className="rounded-2xl border border-border bg-card shadow-soft overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Sub Group (₹)</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">Excl. intercompany</p>
        </header>
        <div className="overflow-x-auto p-2 sm:p-4">
          {groupsLoading && subGroupChartData.length === 0 ? (
            <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground sm:aspect-[16/10] sm:h-auto">
              Loading…
            </div>
          ) : subGroupChartData.length === 0 ? (
            <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground sm:aspect-[16/10] sm:h-auto">
              No sub-group data
            </div>
          ) : (
            <ChartContainer
              config={subGroupConfig}
              className="aspect-[16/11] w-full min-w-[280px] sm:min-w-0 sm:aspect-[16/10]"
            >
              <BarChart
                data={subGroupChartData}
                margin={{ left: 4, right: 4, top: 8, bottom: 28 }}
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
                  type="number"
                  domain={[0, subGroupYMax]}
                  allowDataOverflow={false}
                  tickLine={false}
                  axisLine={false}
                  width={48}
                  fontSize={10}
                  tickCount={5}
                  tickFormatter={(v) => `${(Number(v) / 1e7).toFixed(1)}Cr`}
                />
                <ChartTooltip
                  content={
                    <ChartTooltipContent
                      formatter={(value) => formatSalesCurrency(Number(value))}
                    />
                  }
                />
                <Bar
                  dataKey="salesAmount"
                  fill="var(--color-salesAmount)"
                  radius={[4, 4, 0, 0]}
                  isAnimationActive={false}
                />
              </BarChart>
            </ChartContainer>
          )}
        </div>
      </section>
    </div>
  );
}
