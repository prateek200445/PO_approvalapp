import { CreditCard } from "lucide-react";

export function BillPaymentEntryUnavailable() {
  return (
    <div className="mx-auto max-w-lg space-y-3 py-8">
      <div className="flex items-center gap-2 text-primary">
        <CreditCard className="h-5 w-5" />
        <p className="text-xs font-semibold uppercase tracking-wide">Bill Payment Entry</p>
      </div>
      <h1 className="text-2xl font-semibold tracking-tight">Temporarily unavailable</h1>
      <p className="text-sm text-muted-foreground">
        Bill Payment Entry is turned off for now because the figures being fetched are not
        correct. Use Payment Approval for vendor bill payments. This section will come back once
        the data is fixed.
      </p>
    </div>
  );
}
