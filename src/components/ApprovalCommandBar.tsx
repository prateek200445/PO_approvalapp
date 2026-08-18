import { useEffect } from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { APPROVE_REMARKS, REJECT_REMARKS } from "@/lib/remark-templates";

export function RemarkComposer({
  value,
  onChange,
}: {
  value: string;
  onChange: (next: string) => void;
}) {
  return (
    <div className="space-y-2">
      <div className="no-scrollbar -mx-1 flex gap-1.5 overflow-x-auto px-1 pb-1 md:flex-wrap md:overflow-visible">
        {[...APPROVE_REMARKS, ...REJECT_REMARKS].map((label) => (
          <button
            key={label}
            type="button"
            onClick={() => onChange(label)}
            className={cn(
              "shrink-0 rounded-full border px-3 py-1.5 text-xs font-medium transition md:px-2.5 md:py-1 md:text-[11px]",
              value === label
                ? "border-primary bg-primary/10 text-primary"
                : "border-border bg-surface text-muted-foreground hover:bg-secondary hover:text-foreground",
            )}
          >
            {label}
          </button>
        ))}
      </div>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        rows={3}
        placeholder="Add remarks (mandatory for rejection)…"
        className="w-full resize-none rounded-md border border-input bg-surface p-3 text-base outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 md:text-sm"
      />
    </div>
  );
}

export function ApprovalCommandBar({
  amountLabel,
  queueLabel,
  onApprove,
  onReject,
  isApproving,
  isRejecting,
  disabled,
}: {
  amountLabel?: string;
  queueLabel?: string;
  onApprove: () => void;
  onReject: () => void;
  isApproving?: boolean;
  isRejecting?: boolean;
  disabled?: boolean;
}) {
  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (!(event.ctrlKey || event.metaKey) || event.key !== "Enter") return;
      event.preventDefault();
      if (disabled || isApproving || isRejecting) return;
      if (event.shiftKey) onReject();
      else onApprove();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [disabled, isApproving, isRejecting, onApprove, onReject]);

  return (
    <div className="fixed inset-x-0 bottom-0 z-20 border-t border-border bg-surface/95 p-3 pb-[max(0.75rem,env(safe-area-inset-bottom,0px))] backdrop-blur md:static md:bottom-auto md:border-0 md:bg-transparent md:p-0 md:pb-0">
      <div className="mx-auto flex max-w-7xl flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <div className="min-w-0 text-xs text-muted-foreground">
          {amountLabel && (
            <div className="text-sm font-semibold tabular-nums text-foreground md:text-xs">
              {amountLabel}
            </div>
          )}
          <div className="hidden md:block">
            {queueLabel ? `${queueLabel} · ` : ""}
            Ctrl+Enter approve · Ctrl+Shift+Enter reject
          </div>
          {queueLabel && <div className="md:hidden">{queueLabel}</div>}
        </div>
        <div className="grid grid-cols-2 gap-2 md:flex md:justify-end">
          <button
            type="button"
            disabled={disabled || isRejecting || isApproving}
            onClick={onReject}
            className="h-12 rounded-xl border border-destructive/30 bg-destructive/10 text-sm font-semibold text-destructive disabled:cursor-not-allowed disabled:opacity-50 md:h-11 md:flex-none md:px-6"
          >
            {isRejecting ? (
              <span className="inline-flex items-center justify-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Rejecting...
              </span>
            ) : (
              "Reject"
            )}
          </button>
          <button
            type="button"
            disabled={disabled || isApproving || isRejecting}
            onClick={onApprove}
            className="btn-3d h-12 rounded-xl bg-primary text-sm font-semibold text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50 md:h-11 md:flex-none md:px-6"
          >
            {isApproving ? (
              <span className="inline-flex items-center justify-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Approving...
              </span>
            ) : (
              "Approve"
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
