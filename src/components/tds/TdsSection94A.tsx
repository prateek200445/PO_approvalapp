import { useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertCircle, Inbox, Loader2 } from "lucide-react";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { financialQuarters, getTdsGroups, getTds94A } from "@/lib/tds-api";

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => money.format(n || 0);
const dateFmt = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" });

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

export function TdsSection94A() {
  const quarters = useMemo(() => financialQuarters(), []);
  const [group, setGroup] = useState("");
  const [quarterId, setQuarterId] = useState(quarters[0]?.id ?? "");
  const quarter = quarters.find((q) => q.id === quarterId) ?? quarters[0];

  const groupsQuery = useQuery({
    queryKey: ["tds-groups"],
    queryFn: getTdsGroups,
    staleTime: 10 * 60_000,
  });

  const sheetQuery = useQuery({
    queryKey: ["tds-94a", group, quarter?.from, quarter?.to],
    queryFn: () => getTds94A(group, quarter.from, quarter.to),
    enabled: group.length > 0 && !!quarter,
    placeholderData: keepPreviousData,
  });

  const data = sheetQuery.data;
  const matches = data != null && Math.abs(data.totalTds - data.liability) < 0.005;

  return (
    <div className="space-y-5">
      <div className="grid gap-4 rounded-2xl border border-border/80 bg-card p-4 shadow-soft sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label htmlFor="tds-94a-group">Group</Label>
          <select
            id="tds-94a-group"
            className={SELECT_CLASS}
            value={group}
            onChange={(event) => setGroup(event.target.value)}
            disabled={groupsQuery.isLoading}
          >
            <option value="">Select a group</option>
            {(groupsQuery.data ?? []).map((item) => (
              <option key={item.name} value={item.name}>
                {item.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="tds-94a-quarter">Quarter</Label>
          <select
            id="tds-94a-quarter"
            className={SELECT_CLASS}
            value={quarter?.id ?? ""}
            onChange={(event) => setQuarterId(event.target.value)}
          >
            {quarters.map((item) => (
              <option key={item.id} value={item.id}>
                {item.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {!group && <Empty message="Choose a group and a quarter." />}
      {group && sheetQuery.isLoading && <Loading />}
      {group && sheetQuery.isError && <ErrorText message={(sheetQuery.error as Error).message} />}

      {data && (
        <div className="space-y-3">
          <div className="overflow-x-auto rounded-2xl border border-border/80 bg-card shadow-soft">
            <table className="w-full min-w-[880px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-border/80 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-3 py-3 font-medium">Date</th>
                  <th className="px-3 py-3 font-medium">Unit</th>
                  <th className="px-3 py-3 font-medium">Voucher</th>
                  <th className="px-3 py-3 font-medium">Party</th>
                  <th className="px-3 py-3 font-medium">PAN</th>
                  <th className="px-3 py-3 text-right font-medium">Principal</th>
                  <th className="px-3 py-3 text-right font-medium">Rate</th>
                  <th className="px-3 py-3 text-right font-medium">TDS</th>
                </tr>
              </thead>
              <tbody>
                {data.lines.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                      No 94A credit lines in this quarter.
                    </td>
                  </tr>
                )}
                {data.lines.map((line) => {
                  const reduced = Math.abs(line.tds - line.originalTds) >= 0.005;
                  return (
                    <tr key={`${line.unit}-${line.date}-${line.voucherNo}-${line.party}`} className="border-b border-border/60">
                      <td className="px-3 py-2.5 whitespace-nowrap">{dateFmt.format(new Date(line.date))}</td>
                      <td className="px-3 py-2.5">{line.unit}</td>
                      <td className="px-3 py-2.5 whitespace-nowrap">
                        {line.voucherType} {line.voucherNo}
                      </td>
                      <td className="px-3 py-2.5">{line.party || "—"}</td>
                      <td className="px-3 py-2.5 font-mono text-xs">{line.pan || "—"}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{fmt(line.principal)}</td>
                      <td className="px-3 py-2.5 text-right tabular-nums">{line.rate.toFixed(2)}</td>
                      <td className={cn("px-3 py-2.5 text-right tabular-nums", reduced && "text-amber-600 dark:text-amber-400")}>
                        {fmt(line.tds)}
                        {reduced && (
                          <span className="mt-0.5 block text-[11px] font-normal text-muted-foreground">
                            was {fmt(line.originalTds)}
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
              {data.lines.length > 0 && (
                <tfoot>
                  <tr className="border-t border-border bg-muted/40 font-medium">
                    <td className="px-3 py-3" colSpan={5}>
                      Sheet total
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">{fmt(data.totalPrincipal)}</td>
                    <td />
                    <td className={cn("px-3 py-3 text-right tabular-nums", !matches && "text-amber-600 dark:text-amber-400")}>
                      {fmt(data.totalTds)}
                    </td>
                  </tr>
                  <tr className="border-t border-border/60">
                    <td className="px-3 py-3 text-muted-foreground" colSpan={7}>
                      94A liability
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">{fmt(data.liability)}</td>
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
          <p className="text-sm text-muted-foreground">
            Rate is TDS ÷ principal, rounded to two decimals. Reversal debit of {fmt(data.reversalDebit)} is taken off
            the sheet. A reduced row keeps that rate and the principal is resized.
            {data.roundOffRemoved > 0 && ` ${data.roundOffRemoved} round-off row${data.roundOffRemoved === 1 ? "" : "s"} removed.`}
            {data.duplicatesRemoved > 0 &&
              ` ${data.duplicatesRemoved} duplicate blank-PAN row${data.duplicatesRemoved === 1 ? "" : "s"} removed.`}
          </p>
        </div>
      )}
    </div>
  );
}

function Empty({ message }: { message: string }) {
  return (
    <div className="flex items-center gap-2 rounded-2xl border border-dashed border-border px-4 py-8 text-sm text-muted-foreground">
      <Inbox className="size-4" />
      {message}
    </div>
  );
}

function Loading() {
  return (
    <div className="flex items-center gap-2 px-1 text-sm text-muted-foreground">
      <Loader2 className="size-4 animate-spin" />
      Building the 94A sheet…
    </div>
  );
}

function ErrorText({ message }: { message: string }) {
  return (
    <div className="flex items-start gap-2 rounded-xl border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
      <AlertCircle className="mt-0.5 size-4 shrink-0" />
      {message}
    </div>
  );
}
