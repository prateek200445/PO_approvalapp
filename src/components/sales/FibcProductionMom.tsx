import { useState } from "react";
import type {
  FibcProductionByBagTypeItem,
  FibcProductionMonthItem,
} from "@/lib/sales-dashboard-types";
import { formatSalesQuantity } from "@/lib/sales-dashboard-api";
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
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { cn } from "@/lib/utils";

type Metric = "pcs" | "wt";

interface FibcProductionMomProps {
  months: FibcProductionMonthItem[];
  byBagType: FibcProductionByBagTypeItem[];
  totalPcs: number;
  totalWt: number;
  loading?: boolean;
  error?: string | null;
}

function formatPcs(n: number): string {
  return new Intl.NumberFormat("en-IN", {
    maximumFractionDigits: 0,
  }).format(n);
}

function formatChange(n: number): string {
  if (!Number.isFinite(n) || n === 0) return "—";
  const sign = n > 0 ? "+" : "";
  return `${sign}${n.toFixed(1)}%`;
}

export function FibcProductionMom({
  months,
  byBagType,
  totalPcs,
  totalWt,
  loading,
  error,
}: FibcProductionMomProps) {
  const [metric, setMetric] = useState<Metric>("pcs");

  const chartConfig = {
    value: {
      label: metric === "pcs" ? "Bags (PCS)" : "Weight",
      color: "var(--primary)",
    },
  } satisfies ChartConfig;

  const chartData = months.map((m) => ({
    period: m.period,
    value: metric === "pcs" ? m.pcs : m.wt,
  }));

  const max = chartData.reduce((acc, row) => (row.value > acc ? row.value : acc), 0);
  const yMax = max > 0 ? max * 1.1 : 1;

  return (
    <section className="rounded-2xl border border-border bg-card shadow-soft" aria-label="FIBC production">
      <header className="flex flex-col gap-2 border-b border-border px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between sm:px-4 sm:py-3">
        <div>
          <h2 className="text-sm font-semibold">FIBC Production (Month on Month)</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">
            Live ERP · VW_FIBCBagwiseProduction · uses dashboard company &amp; date filters
          </p>
        </div>
        <div
          className="flex w-full overflow-x-auto rounded-md border border-border p-0.5 sm:w-auto"
          role="radiogroup"
          aria-label="Production metric"
        >
          {(
            [
              { id: "pcs" as const, label: "Bags (PCS)" },
              { id: "wt" as const, label: "Weight" },
            ] as const
          ).map((opt) => (
            <button
              key={opt.id}
              type="button"
              role="radio"
              aria-checked={metric === opt.id}
              onClick={() => setMetric(opt.id)}
              className={cn(
                "min-h-9 flex-1 touch-manipulation rounded-sm px-3 py-1.5 text-xs font-medium transition-colors sm:flex-none",
                metric === opt.id
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground",
              )}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </header>

      <div className="grid grid-cols-1 gap-0 lg:grid-cols-5">
        <div className="border-b border-border p-2 sm:p-4 lg:col-span-3 lg:border-b-0 lg:border-r">
          {loading && chartData.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">
              Loading FIBC production…
            </div>
          ) : error ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-destructive" role="alert">
              {error}
            </div>
          ) : chartData.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">
              No FIBC production in this period
            </div>
          ) : (
            <ChartContainer config={chartConfig} className="aspect-[16/9] w-full sm:aspect-[16/8]">
              <BarChart data={chartData} margin={{ left: 4, right: 4, top: 8, bottom: 8 }}>
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis
                  dataKey="period"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={6}
                  fontSize={10}
                  interval={0}
                  angle={months.length > 8 ? -35 : 0}
                  textAnchor={months.length > 8 ? "end" : "middle"}
                  height={months.length > 8 ? 48 : 28}
                />
                <YAxis
                  type="number"
                  domain={[0, yMax]}
                  allowDataOverflow={false}
                  tickLine={false}
                  axisLine={false}
                  width={52}
                  fontSize={10}
                  tickCount={5}
                  tickFormatter={(v) =>
                    metric === "pcs"
                      ? formatPcs(Number(v))
                      : `${(Number(v) / 1000).toFixed(Number(v) >= 10000 ? 0 : 1)}t`
                  }
                />
                <ChartTooltip
                  content={
                    <ChartTooltipContent
                      formatter={(value) =>
                        metric === "pcs"
                          ? `${formatPcs(Number(value))} PCS`
                          : formatSalesQuantity(Number(value))
                      }
                    />
                  }
                />
                <Bar
                  dataKey="value"
                  fill="var(--color-value)"
                  radius={[4, 4, 0, 0]}
                  isAnimationActive={false}
                />
              </BarChart>
            </ChartContainer>
          )}

          <div className="mt-2 flex flex-wrap gap-3 px-1 text-[11px] text-muted-foreground sm:gap-4">
            <span>
              Total PCS:{" "}
              <span className="font-medium tabular-nums text-foreground">{formatPcs(totalPcs)}</span>
            </span>
            <span>
              Total Wt:{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatSalesQuantity(totalWt)}
              </span>
            </span>
          </div>
        </div>

        <div className="p-2 sm:p-3 lg:col-span-2">
          <p className="mb-2 px-1 text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
            Month detail · MoM change
          </p>
          {months.length === 0 && !loading ? (
            <p className="px-1 text-sm text-muted-foreground">No rows</p>
          ) : (
            <>
              {/* Mobile: stacked cards */}
              <div className="space-y-2 sm:hidden">
                {months.map((m) => (
                  <div
                    key={`${m.year}-${m.month}`}
                    className="rounded-xl border border-border/70 bg-background px-3 py-2.5"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <div className="text-sm font-semibold">{m.period}</div>
                        <div className="mt-0.5 text-[11px] text-muted-foreground">
                          Wt {formatSalesQuantity(m.wt)}
                        </div>
                      </div>
                      <div className="shrink-0 text-right">
                        <div className="text-sm font-medium tabular-nums">{formatPcs(m.pcs)} PCS</div>
                        <div
                          className={cn(
                            "mt-0.5 text-[11px] tabular-nums",
                            m.pcsChangePercent > 0 && "text-success",
                            m.pcsChangePercent < 0 && "text-destructive",
                            m.pcsChangePercent === 0 && "text-muted-foreground",
                          )}
                        >
                          MoM {formatChange(m.pcsChangePercent)}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Desktop / tablet: table */}
              <div className="hidden overflow-x-auto sm:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Month</TableHead>
                      <TableHead className="text-right">PCS</TableHead>
                      <TableHead className="text-right">MoM</TableHead>
                      <TableHead className="text-right">Wt</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {months.map((m) => (
                      <TableRow key={`${m.year}-${m.month}`}>
                        <TableCell className="font-medium">{m.period}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatPcs(m.pcs)}</TableCell>
                        <TableCell
                          className={cn(
                            "text-right tabular-nums",
                            m.pcsChangePercent > 0 && "text-success",
                            m.pcsChangePercent < 0 && "text-destructive",
                          )}
                        >
                          {formatChange(m.pcsChangePercent)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums text-muted-foreground">
                          {formatSalesQuantity(m.wt)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </>
          )}

          {byBagType.length > 0 && (
            <>
              <p className="mb-2 mt-4 px-1 text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                By bag type (period)
              </p>

              <div className="space-y-2 sm:hidden">
                {byBagType.slice(0, 8).map((b) => (
                  <div
                    key={b.typeOfBag}
                    className="rounded-xl border border-border/70 bg-background px-3 py-2.5"
                  >
                    <div className="break-words text-sm font-medium leading-snug">{b.typeOfBag}</div>
                    <div className="mt-1.5 flex flex-wrap gap-x-4 gap-y-1 text-[11px] text-muted-foreground">
                      <span>
                        PCS{" "}
                        <span className="font-medium tabular-nums text-foreground">
                          {formatPcs(b.pcs)}
                        </span>
                      </span>
                      <span>
                        Wt{" "}
                        <span className="font-medium tabular-nums text-foreground">
                          {formatSalesQuantity(b.wt)}
                        </span>
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              <div className="hidden overflow-x-auto sm:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Type</TableHead>
                      <TableHead className="text-right">PCS</TableHead>
                      <TableHead className="text-right">Wt</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {byBagType.slice(0, 8).map((b) => (
                      <TableRow key={b.typeOfBag}>
                        <TableCell className="max-w-[9rem] truncate font-medium" title={b.typeOfBag}>
                          {b.typeOfBag}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">{formatPcs(b.pcs)}</TableCell>
                        <TableCell className="text-right tabular-nums text-muted-foreground">
                          {formatSalesQuantity(b.wt)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </>
          )}
        </div>
      </div>
    </section>
  );
}
