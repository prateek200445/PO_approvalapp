import { ArrowLeft, ChevronLeft, ChevronRight } from "lucide-react";
import type { ReactNode } from "react";

type Navigation = {
  prev: string | null;
  next: string | null;
  index: number;
  total: number;
};

export function ApprovalDetailNav({
  onBack,
  navigation,
  onPrevious,
  onNext,
  trailing,
}: {
  onBack: () => void;
  navigation: Navigation;
  onPrevious: () => void;
  onNext: () => void;
  trailing?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          onClick={onBack}
          className="inline-flex items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" /> Back
        </button>
        {navigation.total > 1 && (
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={!navigation.prev}
              onClick={onPrevious}
              className="inline-flex h-8 items-center gap-1 rounded-md border border-input bg-surface px-2.5 text-xs font-medium hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
              Previous
            </button>
            <button
              type="button"
              disabled={!navigation.next}
              onClick={onNext}
              className="inline-flex h-8 items-center gap-1 rounded-md border border-input bg-surface px-2.5 text-xs font-medium hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
              Next
              <ChevronRight className="h-3.5 w-3.5" />
            </button>
            <span className="px-1 text-[11px] tabular-nums text-muted-foreground">
              {navigation.index} / {navigation.total}
            </span>
          </div>
        )}
      </div>
      {trailing}
    </div>
  );
}
