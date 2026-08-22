import { buildBookTransfers, formatInr, type IntercompanyMatrix } from "@/lib/intercompany-api";

export function SettlementReport({
  matrices,
}: {
  asOf?: string;
  matrices: IntercompanyMatrix[];
}) {
  const steps = buildBookTransfers(matrices);
  if (steps.length === 0) return null;

  return (
    <div id="settlement-report" className="space-y-3 text-lg">
      <p className="font-semibold">Who gives money to whom</p>
      <p className="text-sm text-muted-foreground">
        These amounts are the same intercompany balances as the Balances page.
      </p>
      <ol className="list-decimal space-y-3 pl-6">
        {steps.map((step, i) => (
          <li key={`${step.from}-${step.to}-${i}`}>
            {step.from} gives ₹ {formatInr(step.amount)} to {step.to}.
          </li>
        ))}
      </ol>
    </div>
  );
}
