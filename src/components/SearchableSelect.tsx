import { useMemo, useState } from "react";
import { Check, ChevronDown, Search, X } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export type SearchableSelectOption = {
  value: string;
  label: string;
};

const VISIBLE_WHEN_IDLE = 80;
const VISIBLE_WHEN_SEARCH = 200;

export function SearchableSelect({
  options,
  value,
  onChange,
  placeholder,
  disabled,
  searchPlaceholder = "Search…",
  emptyText = "No matches",
  emptyOptionLabel,
}: {
  options: SearchableSelectOption[];
  value: string;
  onChange: (next: string) => void;
  placeholder: string;
  disabled?: boolean;
  searchPlaceholder?: string;
  emptyText?: string;
  emptyOptionLabel?: string;
}) {
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");

  const selected = useMemo(
    () => options.find((o) => o.value === value) ?? null,
    [options, value],
  );

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

  function pick(next: string) {
    onChange(next);
    setOpen(false);
    setQ("");
  }

  function clear(e: React.MouseEvent) {
    e.preventDefault();
    e.stopPropagation();
    onChange("");
  }

  const summary = selected?.label ?? placeholder;

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
            "flex h-10 w-full items-center justify-between gap-2 rounded-md border bg-background px-3 text-left text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50",
            !selected && "text-muted-foreground",
          )}
        >
          <span className="min-w-0 truncate">{summary}</span>
          <span className="flex shrink-0 items-center gap-1">
            {value ? (
              <span
                role="button"
                tabIndex={0}
                onClick={clear}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") clear(e as unknown as React.MouseEvent);
                }}
                className="rounded p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
                aria-label="Clear selection"
              >
                <X className="h-3.5 w-3.5" />
              </span>
            ) : null}
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
              className="h-8 w-full rounded-md border border-border/70 bg-background pl-8 pr-2 text-sm outline-none focus:border-ring"
            />
          </div>
        </div>
        <div className="max-h-64 overflow-y-auto p-1">
          {emptyOptionLabel && !q.trim() ? (
            <button
              type="button"
              onClick={() => pick("")}
              className={cn(
                "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted",
                !value && "bg-muted/60",
              )}
            >
              <span className="flex h-4 w-4 shrink-0 items-center justify-center">
                {!value ? <Check className="h-3.5 w-3.5" /> : null}
              </span>
              <span className="text-muted-foreground">{emptyOptionLabel}</span>
            </button>
          ) : null}

          {filtered.length === 0 ? (
            <p className="px-2 py-6 text-center text-xs text-muted-foreground">{emptyText}</p>
          ) : (
            <>
              {visible.map((o) => {
                const checked = value === o.value;
                return (
                  <button
                    key={o.value}
                    type="button"
                    onClick={() => pick(o.value)}
                    className={cn(
                      "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted",
                      checked && "bg-muted/60",
                    )}
                  >
                    <span className="flex h-4 w-4 shrink-0 items-center justify-center">
                      {checked ? <Check className="h-3.5 w-3.5" /> : null}
                    </span>
                    <span className="min-w-0 truncate">{o.label}</span>
                  </button>
                );
              })}
              {hiddenCount > 0 ? (
                <p className="px-2 py-2 text-center text-[11px] text-muted-foreground">
                  {hiddenCount.toLocaleString("en-IN")} more — type to search
                </p>
              ) : null}
            </>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
