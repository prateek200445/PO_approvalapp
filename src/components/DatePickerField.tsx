import { useState } from "react";
import { format } from "date-fns";
import { CalendarIcon } from "lucide-react";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { toInputDate } from "@/lib/bom-types";
import { cn } from "@/lib/utils";

function parseInputDate(value: string): Date | undefined {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return undefined;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}

function shiftToMonthYear(base: Date, viewMonth: Date): Date {
  const day = base.getDate();
  const y = viewMonth.getFullYear();
  const m = viewMonth.getMonth();
  const lastDay = new Date(y, m + 1, 0).getDate();
  return new Date(y, m, Math.min(day, lastDay));
}

export function DatePickerField({
  value,
  onChange,
  placeholder = "Pick a date",
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [month, setMonth] = useState<Date>(() => parseInputDate(value) ?? new Date());
  const selected = parseInputDate(value);

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) setMonth(selected ?? new Date());
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
          <span>{selected ? format(selected, "dd MMM yyyy") : placeholder}</span>
          <CalendarIcon className="h-4 w-4 shrink-0 opacity-60" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={selected}
          month={month}
          onMonthChange={(nextMonth) => {
            setMonth(nextMonth);
            const base = selected ?? new Date();
            onChange(toInputDate(shiftToMonthYear(base, nextMonth)));
          }}
          onSelect={(date) => {
            if (!date) return;
            onChange(toInputDate(date));
            setMonth(date);
            setOpen(false);
          }}
          captionLayout="dropdown"
          startMonth={new Date(2000, 0)}
          endMonth={new Date(new Date().getFullYear() + 5, 11)}
        />
      </PopoverContent>
    </Popover>
  );
}
