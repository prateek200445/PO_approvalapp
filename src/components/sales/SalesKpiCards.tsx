import type { SalesDashboardSummary } from "@/lib/sales-dashboard-types";
import {
  formatSalesCurrency,
  formatSalesQuantity,
  formatSalesRate,
} from "@/lib/sales-dashboard-api";
import {
  BarChart3,
  ShoppingCart,
  FileText,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface SalesKpiCardsProps {
  summary: SalesDashboardSummary;
  isPurchase?: boolean;
  loading?: boolean;
}

export function SalesKpiCards({ summary, isPurchase, loading }: SalesKpiCardsProps) {
  const cards: { label: string; value: string; icon: LucideIcon; tone: string; bar: string }[] = [
    {
      label: isPurchase ? "Total Purchase" : "Total Sales",
      value: formatSalesCurrency(isPurchase ? summary.totalPurchase : summary.totalSales),
      icon: BarChart3,
      tone: "bg-primary/15 text-primary border-primary/25",
      bar: "bg-primary",
    },
    {
      label: "Total Quantity",
      value: formatSalesQuantity(summary.totalQuantity),
      icon: ShoppingCart,
      tone: "bg-success/15 text-success border-success/25",
      bar: "bg-success",
    },
    {
      label: "Average Rate",
      value: formatSalesRate(summary.averageRate),
      icon: FileText,
      tone: "bg-warning/15 text-warning border-warning/25",
      bar: "bg-warning",
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-3 sm:gap-3 md:gap-4">
      {cards.map((card) => (
        <div key={card.label} className="card-3d overflow-hidden rounded-2xl p-3 sm:p-4">
          <div className={cn("absolute inset-x-0 top-0 h-1.5", card.bar)} />
          <div
            className={cn(
              "icon-3d mb-2 inline-flex h-8 w-8 items-center justify-center rounded-xl border sm:mb-3 sm:h-9 sm:w-9",
              card.tone,
            )}
          >
            <card.icon className="h-3.5 w-3.5 sm:h-4 sm:w-4" aria-hidden />
          </div>
          <div className="text-[11px] text-muted-foreground sm:text-xs">{card.label}</div>
          <div
            className={cn(
              "mt-1 text-base font-semibold tabular-nums leading-tight break-words sm:text-lg md:text-xl",
              loading && "animate-pulse text-muted-foreground",
            )}
          >
            {loading ? "…" : card.value}
          </div>
        </div>
      ))}
    </div>
  );
}
