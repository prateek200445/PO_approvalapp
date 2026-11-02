import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/lib/utils";

const ALL = "__all__";

export function GateSelect({
  label,
  value,
  onChange,
  options,
  placeholder,
  triggerClassName,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  placeholder?: string;
  triggerClassName?: string;
}) {
  const hasAll = options.some((option) => option.value === "");
  const selected = value === "" ? (hasAll ? ALL : undefined) : value;

  return (
    <div className="min-w-0 space-y-1 text-xs font-medium">
      <span>{label}</span>
      <Select value={selected} onValueChange={(next) => onChange(next === ALL ? "" : next)}>
        <SelectTrigger className={cn("h-10 w-full bg-background text-sm", triggerClassName)}>
          <SelectValue placeholder={placeholder ?? label} />
        </SelectTrigger>
        <SelectContent className="max-h-72">
          {options.map((option) => (
            <SelectItem key={option.value || ALL} value={option.value || ALL} className="whitespace-normal">
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
