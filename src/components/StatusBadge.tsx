import type { POStatus } from "@/lib/mock-data";
import { cn } from "@/lib/utils";

export function StatusBadge({ status, className }: { status: POStatus; className?: string }) {
  const map: Record<POStatus, string> = {
    Pending: "bg-warning/15 text-warning border-warning/30",
    Approved: "bg-success/15 text-success border-success/30",
    Rejected: "bg-destructive/15 text-destructive border-destructive/30",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium",
        map[status],
        className,
      )}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current" />
      {status}
    </span>
  );
}
