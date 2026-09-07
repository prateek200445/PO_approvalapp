import { useState } from "react";
import type { SalesCompanyOption, SalesDashboardFilters, SalesReportCategory } from "@/lib/sales-dashboard-types";
import { indianFyDateRange, indianFyStartYear } from "@/lib/sales-dashboard-api";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useIsMobile } from "@/hooks/use-mobile";
import { Check, ChevronDown, RefreshCw } from "lucide-react";
import { cn } from "@/lib/utils";

function CompanyCheckButton({
  checked,
  label,
  onToggle,
}: {
  checked: boolean;
  label: string;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={checked}
      onClick={onToggle}
      className={cn(
        "flex min-h-11 w-full items-center gap-3 rounded-md px-3 py-2.5 text-left text-sm outline-none transition-colors",
        "hover:bg-secondary focus-visible:ring-2 focus-visible:ring-ring/20",
        "touch-manipulation",
        checked && "bg-accent/60 font-medium",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "flex h-5 w-5 shrink-0 items-center justify-center rounded border",
          checked
            ? "border-primary bg-primary text-primary-foreground"
            : "border-muted-foreground/50 bg-background",
        )}
      >
        {checked ? <Check className="h-3.5 w-3.5" strokeWidth={3} /> : null}
      </span>
      <span className="min-w-0 flex-1 break-words">{label}</span>
    </button>
  );
}

function CompanyCheckMenuItem({
  checked,
  label,
  onToggle,
}: {
  checked: boolean;
  label: string;
  onToggle: () => void;
}) {
  return (
    <DropdownMenuItem
      role="menuitemcheckbox"
      aria-checked={checked}
      onSelect={(e) => {
        e.preventDefault();
        onToggle();
      }}
      className={cn(
        "cursor-pointer gap-2.5 py-2.5",
        checked && "bg-accent/60 font-medium",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "flex h-4 w-4 shrink-0 items-center justify-center rounded border",
          checked
            ? "border-primary bg-primary text-primary-foreground"
            : "border-muted-foreground/50 bg-background",
        )}
      >
        {checked ? <Check className="h-3 w-3" strokeWidth={3} /> : null}
      </span>
      <span className="min-w-0 flex-1 truncate">{label}</span>
    </DropdownMenuItem>
  );
}

function CompanyOptionList({
  allSelected,
  groupOptions,
  unitOptions,
  selected,
  setCompanies,
  toggleCompany,
  asButtons,
}: {
  allSelected: boolean;
  groupOptions: SalesCompanyOption[];
  unitOptions: SalesCompanyOption[];
  selected: Set<string>;
  setCompanies: (next: string[]) => void;
  toggleCompany: (value: string) => void;
  asButtons: boolean;
}) {
  const Row = asButtons ? CompanyCheckButton : CompanyCheckMenuItem;

  return (
    <>
      <Row
        checked={allSelected}
        label="All Companies"
        onToggle={() => setCompanies([])}
      />
      {groupOptions.length > 0 && (
        <>
          {asButtons ? (
            <p className="px-3 pb-1 pt-3 text-xs font-semibold text-muted-foreground">Groups</p>
          ) : (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Groups</DropdownMenuLabel>
            </>
          )}
          {groupOptions.map((o) => (
            <Row
              key={o.value}
              checked={selected.has(o.value)}
              label={o.label}
              onToggle={() => toggleCompany(o.value)}
            />
          ))}
        </>
      )}
      {unitOptions.length > 0 && (
        <>
          {asButtons ? (
            <p className="px-3 pb-1 pt-3 text-xs font-semibold text-muted-foreground">Units</p>
          ) : (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Units</DropdownMenuLabel>
            </>
          )}
          {unitOptions.map((o) => (
            <Row
              key={o.value}
              checked={selected.has(o.value)}
              label={o.label}
              onToggle={() => toggleCompany(o.value)}
            />
          ))}
        </>
      )}
    </>
  );
}

interface SalesFiltersProps {
  companyOptions: SalesCompanyOption[];
  companiesLoading?: boolean;
  filters: SalesDashboardFilters;
  isRefreshing?: boolean;
  onChange: (next: Partial<SalesDashboardFilters>) => void;
  onRefresh: () => void;
}

type FyPreset = "current" | "previous" | "custom";

function detectFyPreset(dateFrom: string, dateTo: string): FyPreset {
  const current = indianFyDateRange(indianFyStartYear());
  const previous = indianFyDateRange(indianFyStartYear() - 1);
  const today = new Date().toISOString().slice(0, 10);
  if (dateFrom === current.dateFrom && (dateTo === current.dateTo || dateTo === today)) {
    return "current";
  }
  if (dateFrom === previous.dateFrom && dateTo === previous.dateTo) {
    return "previous";
  }
  return "custom";
}

export function SalesFilters({
  companyOptions,
  companiesLoading,
  filters,
  isRefreshing,
  onChange,
  onRefresh,
}: SalesFiltersProps) {
  const isMobile = useIsMobile();
  const [companySheetOpen, setCompanySheetOpen] = useState(false);
  const fyPreset = detectFyPreset(filters.dateFrom, filters.dateTo);
  const groupOptions = companyOptions.filter((o) => o.kind === "group");
  const unitOptions = companyOptions.filter((o) => o.kind === "company");
  const selected = new Set(filters.companyValues);
  const allSelected = selected.size === 0;
  const selectedLabels = companyOptions
    .filter((o) => selected.has(o.value))
    .map((o) => o.label);
  const companyTriggerLabel = companiesLoading
    ? "Loading…"
    : allSelected
      ? "All Companies"
      : selectedLabels.length === 1
        ? selectedLabels[0]
        : `${selectedLabels.length} companies selected`;

  function setCompanies(next: string[]) {
    onChange({
      companyValues: next,
      company: next.length === 0 ? "All Companies" : next.join(","),
    });
  }

  function toggleCompany(value: string) {
    const next = new Set(selected);
    if (next.has(value)) next.delete(value);
    else next.add(value);
    setCompanies([...next]);
  }

  function applyFyPreset(preset: FyPreset) {
    if (preset === "custom") return;
    const startYear = indianFyStartYear() - (preset === "previous" ? 1 : 0);
    const range = indianFyDateRange(startYear);
    onChange({
      dateFrom: range.dateFrom,
      dateTo: preset === "current" ? new Date().toISOString().slice(0, 10) : range.dateTo,
    });
  }

  const companyListProps = {
    allSelected,
    groupOptions,
    unitOptions,
    selected,
    setCompanies,
    toggleCompany,
  };

  return (
    <section
      className="card-3d rounded-2xl p-3 sm:p-4"
      aria-label="Sales dashboard filters"
    >
      <div className="flex flex-col gap-3">
        <fieldset className="space-y-1.5">
          <legend className="text-sm font-medium leading-none">Figures</legend>
          <div
            className="flex w-full overflow-x-auto rounded-md border border-border p-0.5"
            role="radiogroup"
            aria-label="Intercompany inclusion"
          >
            {(
              [
                { id: false, label: "Excl. intercompany" },
                { id: true, label: "Incl. intercompany" },
              ] as const
            ).map((opt) => (
              <button
                key={String(opt.id)}
                type="button"
                role="radio"
                aria-checked={filters.includeIntercompany === opt.id}
                onClick={() => onChange({ includeIntercompany: opt.id })}
                className={cn(
                  "min-h-11 shrink-0 flex-1 touch-manipulation rounded-sm px-2.5 py-2 text-xs font-medium transition-colors sm:text-sm",
                  filters.includeIntercompany === opt.id
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </fieldset>

        <div className="min-w-0 space-y-1.5">
          <Label>Company</Label>
          {isMobile ? (
            <Sheet open={companySheetOpen} onOpenChange={setCompanySheetOpen}>
              <SheetTrigger asChild>
                <button
                  type="button"
                  disabled={companiesLoading}
                  aria-label="Select companies"
                  className="flex h-11 w-full touch-manipulation items-center justify-between gap-2 rounded-md border border-border bg-background px-3 text-left text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50"
                >
                  <span className="min-w-0 truncate">{companyTriggerLabel}</span>
                  <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                </button>
              </SheetTrigger>
              <SheetContent
                side="bottom"
                className="flex max-h-[85dvh] flex-col gap-0 rounded-t-2xl p-0 pb-[env(safe-area-inset-bottom)]"
              >
                <SheetHeader className="border-b border-border px-4 py-3 text-left">
                  <SheetTitle>Select companies</SheetTitle>
                  <SheetDescription>
                    Tick one or more companies. Leave All Companies for the full group.
                  </SheetDescription>
                </SheetHeader>
                <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-1 py-2">
                  <CompanyOptionList {...companyListProps} asButtons />
                </div>
                <div className="border-t border-border p-3">
                  <Button
                    type="button"
                    className="h-11 w-full touch-manipulation"
                    onClick={() => setCompanySheetOpen(false)}
                  >
                    Done
                  </Button>
                </div>
              </SheetContent>
            </Sheet>
          ) : (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  disabled={companiesLoading}
                  aria-label="Select companies"
                  className="flex h-11 w-full items-center justify-between gap-2 rounded-md border border-border bg-background px-3 text-left text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50"
                >
                  <span className="min-w-0 truncate">{companyTriggerLabel}</span>
                  <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="start"
                className="max-h-72 w-[var(--radix-dropdown-menu-trigger-width)] min-w-[16rem]"
              >
                <CompanyOptionList {...companyListProps} asButtons={false} />
              </DropdownMenuContent>
            </DropdownMenu>
          )}
          {!allSelected && selectedLabels.length > 0 && (
            <p className="truncate text-[11px] text-muted-foreground sm:text-xs">
              Selected: {selectedLabels.join(", ")}
            </p>
          )}
        </div>

        <fieldset className="space-y-1.5">
          <legend className="text-sm font-medium leading-none">Period (Indian FY)</legend>
          <div
            className="flex w-full overflow-x-auto rounded-md border border-border p-0.5 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            role="radiogroup"
            aria-label="Financial year preset"
          >
            {(
              [
                { id: "current", label: "Current FY" },
                { id: "previous", label: "Previous FY" },
                { id: "custom", label: "Custom" },
              ] as const
            ).map((opt) => (
              <button
                key={opt.id}
                type="button"
                role="radio"
                aria-checked={fyPreset === opt.id}
                onClick={() => applyFyPreset(opt.id)}
                className={cn(
                  "min-h-11 shrink-0 flex-1 touch-manipulation rounded-sm px-2.5 py-2 text-xs font-medium transition-colors sm:flex-none sm:px-3 sm:text-sm",
                  fyPreset === opt.id
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </fieldset>

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
                    "min-h-11 flex-1 touch-manipulation rounded-sm px-3 py-2 text-xs font-medium transition-colors sm:flex-none sm:text-sm",
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
            className="h-11 w-full touch-manipulation sm:ml-auto sm:w-auto"
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
