import { useMemo, useState } from "react";
import { Check, ChevronDown, Search, X } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export type MultiSelectOption = {
  value: string;
  label: string;
};

const VISIBLE_WHEN_IDLE = 80;
const VISIBLE_WHEN_SEARCH = 200;

export function MultiSelect({
  options,
  values,
  onChange,
  placeholder,
  disabled,
  searchPlaceholder = "Search…",
  emptyText = "No options",
}: {
  options: MultiSelectOption[];
  values: string[];
  onChange: (next: string[]) => void;
  placeholder: string;
  disabled?: boolean;
  searchPlaceholder?: string;
  emptyText?: string;
}) {
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");

  const valueSet = useMemo(() => new Set(values), [values]);

  const selected = useMemo(() => {
    if (values.length === 0) return [];
    return options.filter((o) => valueSet.has(o.value));
  }, [options, values, valueSet]);

  const filtered = useMemo(() => {
    const query = q.trim().toLowerCase();
    if (!query) return options;
    return options.filter(
      (o) => o.label.toLowerCase().includes(query) || o.value.toLowerCase().includes(query),
    );
  }, [options, q]);

  const limit = q.trim() ? VISIBLE_WHEN_SEARCH : VISIBLE_WHEN_IDLE;
  const visible = filtered.slice(0, limit);
  const hiddenCount = Math.max(0, filtered.length - visible.length);

  function toggle(value: string) {
    if (valueSet.has(value)) onChange(values.filter((v) => v !== value));
    else onChange([...values, value]);
  }

  function clearAll(e: React.MouseEvent) {
    e.preventDefault();
    e.stopPropagation();
    onChange([]);
  }

  const summary =
    selected.length === 0
      ? placeholder
      : selected.length === 1
        ? selected[0].label
        : `${selected.length} selected`;

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setQ("");
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          className={cn(
            "flex h-9 w-full items-center justify-between gap-2 rounded-md border border-border/70 bg-surface px-3 text-left text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50",
            selected.length === 0 && "text-muted-foreground",
          )}
        >
          <span className="min-w-0 truncate">{summary}</span>
          <span className="flex shrink-0 items-center gap-1">
            {selected.length > 0 && (
              <span
                role="button"
                tabIndex={0}
                onClick={clearAll}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") clearAll(e as any);
                }}
                className="rounded p-0.5 text-muted-foreground hover:bg-secondary hover:text-foreground"
                aria-label="Clear selection"
              >
                <X className="h-3.5 w-3.5" />
              </span>
            )}
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </span>
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0" align="start">
        <div className="border-b border-border p-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
            <input
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder={
                options.length > VISIBLE_WHEN_IDLE
                  ? `Search ${options.length.toLocaleString("en-IN")}…`
                  : searchPlaceholder
              }
              className="h-8 w-full rounded-md border border-border/70 bg-surface pl-8 pr-2 text-sm outline-none focus:border-ring"
            />
          </div>
        </div>
        <div className="max-h-64 overflow-y-auto p-1">
          {filtered.length === 0 ? (
            <p className="px-2 py-6 text-center text-xs text-muted-foreground">{emptyText}</p>
          ) : (
            <>
              {visible.map((o) => {
                const checked = valueSet.has(o.value);
                return (
                  <button
                    key={o.value}
                    type="button"
                    onClick={() => toggle(o.value)}
                    className={cn(
                      "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-secondary",
                      checked && "bg-secondary/60",
                    )}
                  >
                    <span
                      className={cn(
                        "flex h-4 w-4 shrink-0 items-center justify-center rounded-sm border border-primary",
                        checked && "bg-primary text-primary-foreground",
                      )}
                    >
                      {checked ? <Check className="h-3 w-3" /> : null}
                    </span>
                    <span className="min-w-0 truncate">{o.label}</span>
                  </button>
                );
              })}
              {hiddenCount > 0 && (
                <p className="px-2 py-2 text-center text-[11px] text-muted-foreground">
                  {hiddenCount.toLocaleString("en-IN")} more — type to search
                </p>
              )}
            </>
          )}
        </div>
        {selected.length > 0 && (
          <div className="flex flex-wrap gap-1 border-t border-border p-2">
            {selected.slice(0, 6).map((o) => (
              <button
                key={o.value}
                type="button"
                onClick={() => toggle(o.value)}
                className="inline-flex max-w-full items-center gap-1 rounded-md bg-secondary px-1.5 py-0.5 text-[11px] text-foreground"
                title="Remove"
              >
                <span className="truncate">{o.label}</span>
                <X className="h-3 w-3 shrink-0 opacity-70" />
              </button>
            ))}
            {selected.length > 6 && (
              <span className="px-1.5 py-0.5 text-[11px] text-muted-foreground">+{selected.length - 6} more</span>
            )}
          </div>
        )}
      </PopoverContent>
    </Popover>
  );
}
