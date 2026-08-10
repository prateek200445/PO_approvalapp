import type { SalesDashboardFilters, SalesReportCategory, SalesReportView } from "@/lib/sales-dashboard-types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { RefreshCw } from "lucide-react";
import { cn } from "@/lib/utils";

interface SalesFiltersProps {
  companies: string[];
  filters: SalesDashboardFilters;
  isRefreshing?: boolean;
  onChange: (next: Partial<SalesDashboardFilters>) => void;
  onRefresh: () => void;
}

const VIEWS: SalesReportView[] = ["Summary", "Detail", "Date Summary"];

export function SalesFilters({
  companies,
  filters,
  isRefreshing,
  onChange,
  onRefresh,
}: SalesFiltersProps) {
  return (
    <section
      className="rounded-xl border border-border bg-card p-3 shadow-sm sm:p-4"
      aria-label="Sales dashboard filters"
    >
      <div className="flex flex-col gap-3">
        <div className="min-w-0 space-y-1.5">
          <Label htmlFor="sales-company">Company</Label>
          <Select
            value={filters.company}
            onValueChange={(company) => onChange({ company })}
          >
            <SelectTrigger id="sales-company" className="h-11 w-full bg-background text-sm">
              <SelectValue placeholder="Select company" />
            </SelectTrigger>
            <SelectContent className="max-h-[50vh]">
              {companies.map((c, index) => (
                <SelectItem key={`${index}-${c}`} value={c} className="text-sm">
                  {c}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="grid grid-cols-2 gap-2 sm:gap-3">
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="sales-date-from">Date From</Label>
            <Input
              id="sales-date-from"
              type="date"
              value={filters.dateFrom}
              onChange={(e) => onChange({ dateFrom: e.target.value })}
              className="h-11 bg-background text-sm"
            />
          </div>
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="sales-date-to">Date To</Label>
            <Input
              id="sales-date-to"
              type="date"
              value={filters.dateTo}
              onChange={(e) => onChange({ dateTo: e.target.value })}
              className="h-11 bg-background text-sm"
            />
          </div>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
          <fieldset className="min-w-0 flex-1 space-y-1.5">
            <legend className="text-sm font-medium leading-none">View</legend>
            <div
              className="flex w-full overflow-x-auto rounded-md border border-border p-0.5 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
              role="radiogroup"
              aria-label="Report view"
            >
              {VIEWS.map((view) => (
                <button
                  key={view}
                  type="button"
                  role="radio"
                  aria-checked={filters.view === view}
                  onClick={() => onChange({ view })}
                  className={cn(
                    "shrink-0 flex-1 rounded-sm px-2.5 py-2 text-xs font-medium transition-colors sm:flex-none sm:px-3 sm:text-sm",
                    filters.view === view
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  {view}
                </button>
              ))}
            </div>
          </fieldset>

          <fieldset className="space-y-1.5">
            <legend className="text-sm font-medium leading-none">Category</legend>
            <div
              className="flex rounded-md border border-border p-0.5"
              role="radiogroup"
              aria-label="Sales or purchase"
            >
              {(["Sales", "Purchase"] as SalesReportCategory[]).map((category) => (
                <button
                  key={category}
                  type="button"
                  role="radio"
                  aria-checked={filters.category === category}
                  onClick={() => onChange({ category })}
                  className={cn(
                    "flex-1 rounded-sm px-3 py-2 text-xs font-medium transition-colors sm:flex-none sm:text-sm",
                    filters.category === category
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )}
                >
                  {category}
                </button>
              ))}
            </div>
          </fieldset>

          <Button
            type="button"
            onClick={onRefresh}
            disabled={isRefreshing}
            className="h-11 w-full sm:w-auto"
            aria-label="Refresh sales dashboard"
          >
            <RefreshCw className={cn("h-4 w-4", isRefreshing && "animate-spin")} />
            Refresh
          </Button>
        </div>
      </div>
    </section>
  );
}
