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
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <section className="rounded-xl border border-dashed border-border bg-card/50 shadow-sm lg:col-span-1">
        <header className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Sales Trend (₹)</h2>
        </header>
        <div className="flex aspect-[16/10] items-center justify-center px-4 text-center text-sm text-muted-foreground">
          Coming soon — no date series in SP_Sales_EBIDTA
        </div>
      </section>

      <section className="rounded-xl border border-border bg-card shadow-sm">
        <header className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Sales by Group</h2>
        </header>
        <div className="p-3 sm:p-4">
          {byGroup.length === 0 ? (
            <div className="flex aspect-square max-h-[240px] items-center justify-center text-sm text-muted-foreground">
              No group data
            </div>
          ) : (
            <>
              <ChartContainer
                config={groupConfig}
                className="mx-auto aspect-square max-h-[240px] w-full"
              >
                <PieChart>
                  <ChartTooltip
                    content={
                      <ChartTooltipContent
                        hideLabel
                        formatter={(value, name, item) => (
                          <div className="flex w-full items-center justify-between gap-4">
                            <span className="text-muted-foreground">{String(name)}</span>
                            <span className="font-medium tabular-nums">
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
                    innerRadius={55}
                    outerRadius={85}
                    strokeWidth={2}
                  >
                    {pieData.map((entry) => (
                      <Cell key={entry.name} fill={entry.fill} />
                    ))}
                  </Pie>
                </PieChart>
              </ChartContainer>
              <ul className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1.5 text-xs">
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

      <section className="rounded-xl border border-border bg-card shadow-sm">
        <header className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Sales by Sub Group (₹)</h2>
        </header>
        <div className="p-3 sm:p-4">
          {bySubGroup.length === 0 ? (
            <div className="flex aspect-[16/10] items-center justify-center text-sm text-muted-foreground">
              No sub-group data
            </div>
          ) : (
            <ChartContainer config={subGroupConfig} className="aspect-[16/10] w-full">
              <BarChart data={bySubGroup} margin={{ left: 0, right: 8, top: 8, bottom: 0 }}>
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis
                  dataKey="subGroupName"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  fontSize={10}
                  interval={0}
                  tickFormatter={(v) => String(v).slice(0, 12)}
                />
                <YAxis
                  tickLine={false}
                  axisLine={false}
                  width={42}
                  fontSize={11}
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
