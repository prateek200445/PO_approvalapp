import type { SalesDashboardSummary } from "@/lib/sales-dashboard-types";
import {
  formatChangePercent,
  formatSalesCurrency,
  formatSalesQuantity,
  formatSalesRate,
} from "@/lib/sales-dashboard-api";
import {
  BarChart3,
  ShoppingCart,
  FileText,
  Percent,
  Download,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface SalesKpiCardsProps {
  summary: SalesDashboardSummary;
  unavailableFields?: string[];
}

type KpiConfig = {
  id: string;
  label: string;
  displayValue: string;
  changePercent: number;
  showChange: boolean;
  unavailable?: boolean;
  icon: LucideIcon;
  tone: string;
};

export function SalesKpiCards({ summary, unavailableFields = [] }: SalesKpiCardsProps) {
  const noChange = unavailableFields.includes("changePercents");
  const noGross = unavailableFields.includes("grossProfit");

  const cards: KpiConfig[] = [
    {
      id: "totalSales",
      label: "Total Sales",
      displayValue: unavailableFields.includes("totalSales")
        ? "—"
        : formatSalesCurrency(summary.totalSales),
      changePercent: summary.totalSalesChangePercent,
      showChange: !noChange && !unavailableFields.includes("totalSales"),
      unavailable: unavailableFields.includes("totalSales"),
      icon: BarChart3,
      tone: "bg-primary/10 text-primary border-primary/20",
    },
    {
      id: "totalQuantity",
      label: "Total Quantity",
      displayValue: formatSalesQuantity(summary.totalQuantity),
      changePercent: summary.totalQuantityChangePercent,
      showChange: !noChange,
      icon: ShoppingCart,
      tone: "bg-success/10 text-success border-success/20",
    },
    {
      id: "averageRate",
      label: "Average Rate",
      displayValue: unavailableFields.includes("averageRate")
        ? "—"
        : formatSalesRate(summary.averageRate),
      changePercent: summary.averageRateChangePercent,
      showChange: !noChange && !unavailableFields.includes("averageRate"),
      unavailable: unavailableFields.includes("averageRate"),
      icon: FileText,
      tone: "bg-warning/10 text-warning border-warning/20",
    },
    {
      id: "gstAmount",
      label: "GST Amount",
      displayValue: unavailableFields.includes("gstAmount")
        ? "—"
        : formatSalesCurrency(summary.gstAmount),
      changePercent: summary.gstAmountChangePercent,
      showChange: !noChange && !unavailableFields.includes("gstAmount"),
      unavailable: unavailableFields.includes("gstAmount"),
      icon: Percent,
      tone: "bg-accent text-accent-foreground border-border",
    },
    {
      id: "totalPurchase",
      label: "Total Purchase",
      displayValue: unavailableFields.includes("totalPurchase")
        ? "—"
        : formatSalesCurrency(summary.totalPurchase),
      changePercent: summary.totalPurchaseChangePercent,
      showChange: !noChange && !unavailableFields.includes("totalPurchase"),
      unavailable: unavailableFields.includes("totalPurchase"),
      icon: Download,
      tone: "bg-secondary text-secondary-foreground border-border",
    },
    {
      id: "grossProfit",
      label: "Gross Profit",
      displayValue: noGross ? "—" : formatSalesCurrency(summary.grossProfit),
      changePercent: summary.grossProfitChangePercent,
      showChange: !noChange && !noGross,
      unavailable: noGross,
      icon: TrendingUp,
      tone: "bg-success/10 text-success border-success/20",
    },
  ];

  return (
    <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 sm:gap-3 xl:grid-cols-6 md:gap-4">
      {cards.map((card) => (
        <div
          key={card.id}
          className="rounded-xl border border-border bg-card p-3 shadow-sm sm:p-4"
        >
          <div
            className={cn(
              "mb-2 inline-flex h-8 w-8 items-center justify-center rounded-lg border sm:mb-3 sm:h-9 sm:w-9",
              card.tone,
            )}
          >
            <card.icon className="h-3.5 w-3.5 sm:h-4 sm:w-4" aria-hidden />
          </div>
          <div className="text-[11px] text-muted-foreground sm:text-xs">{card.label}</div>
          <div className="mt-1 text-base font-semibold tabular-nums leading-tight break-words sm:text-lg md:text-xl">
            {card.displayValue}
          </div>
          {card.unavailable ? (
            <div className="mt-1.5 text-[11px] text-muted-foreground sm:mt-2 sm:text-xs">
              Coming soon
            </div>
          ) : card.showChange ? (
            <div
              className={cn(
                "mt-1.5 text-[11px] font-medium tabular-nums sm:mt-2 sm:text-xs",
                card.changePercent >= 0 ? "text-success" : "text-destructive",
              )}
            >
              {formatChangePercent(card.changePercent)} vs last period
            </div>
          ) : (
            <div className="mt-1.5 text-[11px] text-muted-foreground sm:mt-2 sm:text-xs">
              Period comparison pending
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
