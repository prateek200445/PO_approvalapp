import { useState } from "react";
import { CalendarIcon, ChevronLeft, ChevronRight } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

function parseMonthValue(value: string): Date | undefined {
  const match = /^(\d{4})-(\d{2})$/.exec(value);
  if (!match) return undefined;
  return new Date(Number(match[1]), Number(match[2]) - 1, 1);
}

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

export function MonthPickerField({
  value,
  onChange,
  placeholder = "Pick month",
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const selected = parseMonthValue(value);
  const [viewYear, setViewYear] = useState(() => selected?.getFullYear() ?? new Date().getFullYear());

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) setViewYear(selected?.getFullYear() ?? new Date().getFullYear());
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          className={cn(
            "flex w-full items-center justify-between rounded-md border bg-background px-3 py-2 text-left text-sm",
            !value && "text-muted-foreground",
            disabled && "cursor-not-allowed opacity-60",
          )}
        >
          <span>{selected ? `${MONTHS[selected.getMonth()]} ${selected.getFullYear()}` : placeholder}</span>
          <CalendarIcon className="h-4 w-4 shrink-0 opacity-60" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-80 p-3" align="start">
        <div className="flex items-center justify-between gap-2">
          <button
            type="button"
            className="inline-flex h-9 w-9 items-center justify-center rounded-md border bg-background hover:bg-secondary"
            onClick={() => setViewYear((y) => y - 1)}
            aria-label="Previous year"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <div className="text-sm font-medium tabular-nums">{viewYear}</div>
          <button
            type="button"
            className="inline-flex h-9 w-9 items-center justify-center rounded-md border bg-background hover:bg-secondary"
            onClick={() => setViewYear((y) => y + 1)}
            aria-label="Next year"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>

        <div className="mt-3 grid grid-cols-3 gap-2">
          {MONTHS.map((label, index) => {
            const isSelected =
              selected?.getFullYear() === viewYear && selected?.getMonth() === index;
            return (
              <button
                key={label}
                type="button"
                onClick={() => {
                  onChange(`${viewYear}-${String(index + 1).padStart(2, "0")}`);
                  setOpen(false);
                }}
                className={cn(
                  "rounded-md border px-3 py-2 text-sm hover:bg-secondary",
                  isSelected && "border-primary bg-primary/10 text-primary",
                )}
              >
                {label}
              </button>
            );
          })}
        </div>
      </PopoverContent>
    </Popover>
  );
}
