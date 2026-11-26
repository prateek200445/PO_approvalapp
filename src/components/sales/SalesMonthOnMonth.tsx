import { useState } from "react";
import { FileDown, Loader2 } from "lucide-react";
import { toast } from "sonner";
import type { SalesMonthlyMomMonthItem } from "@/lib/sales-dashboard-types";
import {
  downloadSalesMonthlyTrendExcel,
  formatCrores,
  formatSalesCurrency,
  formatSalesQuantity,
  formatSalesRate,
} from "@/lib/sales-dashboard-api";
import { Button } from "@/components/ui/button";
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

interface SalesMonthOnMonthProps {
  months: SalesMonthlyMomMonthItem[];
  totalAmount: number;
  totalQuantity: number;
  averageRate: number;
  category: string;
  company: string;
  dateFrom: string;
  dateTo: string;
  includeIntercompany: boolean;
  loading?: boolean;
  error?: string | null;
}

function formatChange(n: number): string {
  if (!Number.isFinite(n) || n === 0) return "—";
  const sign = n > 0 ? "+" : "";
  return `${sign}${n.toFixed(1)}%`;
}

export function SalesMonthOnMonth({
  months,
  totalAmount,
  totalQuantity,
  averageRate,
  category,
  company,
  dateFrom,
  dateTo,
  includeIntercompany,
  loading,
  error,
}: SalesMonthOnMonthProps) {
  const [downloading, setDownloading] = useState(false);
  const isPurchase = category.toLowerCase() === "purchase";
  const amountLabel = isPurchase ? "Purchase" : "Sales";

  const chartConfig = {
    amount: {
      label: amountLabel,
      color: "var(--primary)",
    },
  } satisfies ChartConfig;

  const chartData = months.map((m) => ({
    period: m.period,
    amount: m.amount,
  }));

  const max = chartData.reduce((acc, row) => (row.amount > acc ? row.amount : acc), 0);
  const yMax = max > 0 ? max * 1.1 : 1;

  async function handleDownloadExcel() {
    setDownloading(true);
    try {
      await downloadSalesMonthlyTrendExcel({
        company,
        dateFrom,
        dateTo,
        category,
        includeIntercompany,
      });
      toast.success("Excel downloaded");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not create the Excel file");
    } finally {
      setDownloading(false);
    }
  }

  return (
    <section className="rounded-2xl border border-border bg-card shadow-soft" aria-label="Month on Month">
      <header className="flex flex-col gap-2 border-b border-border px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between sm:px-4 sm:py-3">
        <div className="min-w-0">
          <h2 className="text-sm font-semibold">{amountLabel} — Month on Month</h2>
          <p className="mt-0.5 text-[11px] text-muted-foreground">
            EBIDTA Amount · same basis as dashboard KPI totals · uses company &amp; date filters
            {includeIntercompany ? " · incl. intercompany" : " · excl. intercompany"}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-9 shrink-0 touch-manipulation gap-1.5"
          disabled={downloading || loading || months.length === 0}
          onClick={() => void handleDownloadExcel()}
          title="Download month-on-month Excel"
        >
          {downloading ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
          ) : (
            <FileDown className="h-3.5 w-3.5" aria-hidden />
          )}
          Excel
        </Button>
      </header>

      <div className="grid grid-cols-1 gap-0 lg:grid-cols-5">
        <div className="border-b border-border p-2 sm:p-4 lg:col-span-3 lg:border-b-0 lg:border-r">
          {loading && chartData.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">
              Loading month-on-month…
            </div>
          ) : error ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-destructive" role="alert">
              {error}
            </div>
          ) : chartData.length === 0 ? (
            <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">
              No {amountLabel.toLowerCase()} in this period
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
                  tickFormatter={(v) => formatCrores(Number(v)).replace(" Cr", "")}
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

          <div className="mt-2 flex flex-wrap gap-3 px-1 text-[11px] text-muted-foreground sm:gap-4">
            <span>
              Total {amountLabel}:{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatCrores(totalAmount)}
              </span>
            </span>
            <span>
              Qty:{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatSalesQuantity(totalQuantity)}
              </span>
            </span>
            <span>
              Avg rate:{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatSalesRate(averageRate)}
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
                          Qty {formatSalesQuantity(m.quantity)}
                        </div>
                      </div>
                      <div className="shrink-0 text-right">
                        <div className="text-sm font-medium tabular-nums">
                          {formatCrores(m.amount)}
                        </div>
                        <div
                          className={cn(
                            "mt-0.5 text-[11px] tabular-nums",
                            m.amountChangePercent > 0 && "text-success",
                            m.amountChangePercent < 0 && "text-destructive",
                            m.amountChangePercent === 0 && "text-muted-foreground",
                          )}
                        >
                          MoM {formatChange(m.amountChangePercent)}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              <div className="hidden max-h-[28rem] overflow-auto sm:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Month</TableHead>
                      <TableHead className="text-right">{amountLabel}</TableHead>
                      <TableHead className="text-right">MoM</TableHead>
                      <TableHead className="text-right">Qty</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {months.map((m) => (
                      <TableRow key={`${m.year}-${m.month}`}>
                        <TableCell className="font-medium">{m.period}</TableCell>
                        <TableCell className="text-right tabular-nums">
                          {formatCrores(m.amount)}
                        </TableCell>
                        <TableCell
                          className={cn(
                            "text-right tabular-nums",
                            m.amountChangePercent > 0 && "text-success",
                            m.amountChangePercent < 0 && "text-destructive",
                          )}
                        >
                          {formatChange(m.amountChangePercent)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums text-muted-foreground">
                          {formatSalesQuantity(m.quantity)}
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
