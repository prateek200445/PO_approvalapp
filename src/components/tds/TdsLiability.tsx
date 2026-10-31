import { useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertCircle, Inbox, Loader2 } from "lucide-react";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  financialQuarters,
  getTdsGroups,
  getTdsLiability,
  TDS_SECTIONS,
  type TdsSection,
} from "@/lib/tds-api";

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => money.format(n || 0);

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

type Selection = { unit: string | null; section: TdsSection };

export function TdsLiability() {
  const quarters = useMemo(() => financialQuarters(), []);
  const [group, setGroup] = useState("");
  const [quarterId, setQuarterId] = useState(quarters[0]?.id ?? "");
  const [selection, setSelection] = useState<Selection | null>(null);
  const quarter = quarters.find((q) => q.id === quarterId) ?? quarters[0];

  const groupsQuery = useQuery({
    queryKey: ["tds-groups"],
    queryFn: getTdsGroups,
    staleTime: 10 * 60_000,
  });

  const liabilityQuery = useQuery({
    queryKey: ["tds-liability", group, quarter?.from, quarter?.to],
    queryFn: () => getTdsLiability(group, quarter.from, quarter.to),
    enabled: group.length > 0 && !!quarter,
    placeholderData: keepPreviousData,
  });

  const data = liabilityQuery.data;
  const unitsTotal = useMemo(() => {
    const sections: Record<string, number> = {};
    for (const section of TDS_SECTIONS) {
      sections[section] = (data?.units ?? []).reduce((sum, row) => sum + (row.sections[section] ?? 0), 0);
    }
    return { sections, total: TDS_SECTIONS.reduce((sum, section) => sum + sections[section], 0) };
  }, [data]);
  const reversals = useMemo(() => {
    const rows = data?.reversals ?? [];
    if (!selection) return rows;
    return rows.filter(
      (row) => row.section === selection.section && (selection.unit == null || row.unit === selection.unit),
    );
  }, [data?.reversals, selection]);

  function toggle(unit: string | null, section: TdsSection) {
    setSelection((current) =>
      current && current.unit === unit && current.section === section ? null : { unit, section },
    );
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 rounded-2xl border border-border/80 bg-card p-4 shadow-soft sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label htmlFor="tds-group">Group</Label>
          <select
            id="tds-group"
            className={SELECT_CLASS}
            value={group}
            onChange={(event) => {
              setGroup(event.target.value);
              setSelection(null);
            }}
            disabled={groupsQuery.isLoading}
          >
            <option value="">Select a group</option>
            {(groupsQuery.data ?? []).map((item) => (
              <option key={item.name} value={item.name}>
                {item.name} ({item.units})
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="tds-quarter">Quarter</Label>
          <select
            id="tds-quarter"
            className={SELECT_CLASS}
            value={quarter?.id ?? ""}
            onChange={(event) => {
              setQuarterId(event.target.value);
              setSelection(null);
            }}
          >
            {quarters.map((item) => (
              <option key={item.id} value={item.id}>
                {item.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {groupsQuery.isError ? (
        <Message
          icon={AlertCircle}
          title="Could not load groups"
          detail={groupsQuery.error instanceof Error ? groupsQuery.error.message : "Failed to load groups."}
        />
      ) : null}

      {!group ? (
        <Message icon={Inbox} title="Choose a group" detail="The liability table is for one group and one quarter." />
      ) : null}

      {group && liabilityQuery.isFetching && !data ? (
        <div className="flex items-center gap-3 rounded-2xl border border-border/80 bg-card px-5 py-8 text-sm text-muted-foreground shadow-soft" aria-busy="true">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          Reading the section ledgers…
        </div>
      ) : null}

      {liabilityQuery.isError ? (
        <Message
          icon={AlertCircle}
          title="Could not load liability"
          detail={liabilityQuery.error instanceof Error ? liabilityQuery.error.message : "Failed to load liability."}
        />
      ) : null}

      {data ? (
        <>
          <div className="overflow-x-auto rounded-2xl border border-border/80 bg-card shadow-soft">
            <table className="w-full min-w-[760px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  <th className="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Unit</th>
                  {TDS_SECTIONS.map((section) => (
                    <th key={section} className="px-3 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                      {section}
                    </th>
                  ))}
                  <th className="px-3 py-2 text-right text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Total</th>
                </tr>
              </thead>
              <tbody>
                {data.units.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                      No TDS movement on the six 26Q ledgers in this quarter.
                    </td>
                  </tr>
                ) : (
                  data.units.map((row) => (
                    <tr key={row.unit} className="border-b border-border/70">
                      <td className="px-3 py-2 font-medium">{row.unit}</td>
                      {TDS_SECTIONS.map((section) => (
                        <td key={section} className="px-1 py-1 text-right">
                          <AmountButton
                            value={row.sections[section] ?? 0}
                            selected={selection?.unit === row.unit && selection.section === section}
                            onClick={() => toggle(row.unit, section)}
                          />
                        </td>
                      ))}
                      <td className="px-3 py-2 text-right font-medium tabular-nums">{fmt(row.total)}</td>
                    </tr>
                  ))
                )}
              </tbody>
              <tfoot>
                {data.units.length > 0 ? (
                  <tr className="border-t-2 border-border bg-muted/20">
                    <td className="px-3 py-2 font-semibold">Total of units</td>
                    {TDS_SECTIONS.map((section) => (
                      <td key={section} className="px-3 py-2 text-right font-semibold tabular-nums">
                        {fmt(unitsTotal.sections[section] ?? 0)}
                      </td>
                    ))}
                    <td className="px-3 py-2 text-right font-semibold tabular-nums">{fmt(unitsTotal.total)}</td>
                  </tr>
                ) : null}
                <tr className="border-t border-border bg-muted/40">
                  <td className="px-3 py-2 font-semibold">Group</td>
                  {TDS_SECTIONS.map((section) => {
                    const groupAmount = data.groupTotal.sections[section] ?? 0;
                    const matches = Math.abs(groupAmount - (unitsTotal.sections[section] ?? 0)) < 0.005;
                    return (
                      <td key={section} className="px-1 py-1 text-right">
                        <AmountButton
                          value={groupAmount}
                          selected={selection?.unit == null && selection?.section === section}
                          strong
                          mismatch={!matches && data.units.length > 0}
                          onClick={() => toggle(null, section)}
                        />
                      </td>
                    );
                  })}
                  <td
                    className={cn(
                      "px-3 py-2 text-right font-semibold tabular-nums",
                      data.units.length > 0 && Math.abs(data.groupTotal.total - unitsTotal.total) >= 0.005 && "text-amber-600 dark:text-amber-400",
                    )}
                  >
                    {fmt(data.groupTotal.total)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          <p className="text-sm text-muted-foreground">
            Each amount is the quarter’s credit on that section ledger, minus reversal debits.
            {data.omitted.groupUnitLines > 0
              ? ` ${data.omitted.groupUnitLines} debit${data.omitted.groupUnitLines === 1 ? "" : "s"} against a group unit (${fmt(data.omitted.groupUnitAmount)}) left out.`
              : ""}
            {data.omitted.transferLines > 0
              ? ` ${data.omitted.transferLines} transfer${data.omitted.transferLines === 1 ? "" : "s"} to TDS Payable (${fmt(data.omitted.transferAmount)}) left out.`
              : ""}
          </p>

          <section className="space-y-2">
            <h2 className="text-sm font-semibold">
              Reversals subtracted
              {selection ? ` · ${selection.section}${selection.unit ? ` · ${selection.unit}` : ""}` : ""}
              <span className="ml-2 font-normal text-muted-foreground">{reversals.length}</span>
            </h2>
            {reversals.length === 0 ? (
              <p className="rounded-xl border border-border/80 bg-card px-4 py-3 text-sm text-muted-foreground">
                No reversal debits in this {selection ? "cell" : "quarter"}.
              </p>
            ) : (
              <div className="overflow-x-auto rounded-2xl border border-border/80 bg-card shadow-soft">
                <table className="w-full min-w-[720px] border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-border bg-muted/40 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                      <th className="px-3 py-2 text-left">Date</th>
                      <th className="px-3 py-2 text-left">Unit</th>
                      <th className="px-3 py-2 text-left">Section</th>
                      <th className="px-3 py-2 text-left">Voucher</th>
                      <th className="px-3 py-2 text-left">Particulars</th>
                      <th className="px-3 py-2 text-right">Amount</th>
                    </tr>
                  </thead>
                  <tbody>
                    {reversals.map((row, index) => (
                      <tr key={`${row.unit}-${row.voucherNo}-${row.date}-${index}`} className="border-b border-border/70">
                        <td className="whitespace-nowrap px-3 py-2">{formatDate(row.date)}</td>
                        <td className="px-3 py-2">{row.unit}</td>
                        <td className="px-3 py-2">{row.section}</td>
                        <td className="whitespace-nowrap px-3 py-2">
                          {row.voucherType} {row.voucherNo}
                        </td>
                        <td className="px-3 py-2">{row.particulars || "—"}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(row.amount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      ) : null}
    </div>
  );
}

function AmountButton({
  value,
  selected,
  strong,
  mismatch,
  onClick,
}: {
  value: number;
  selected: boolean;
  strong?: boolean;
  mismatch?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "w-full rounded-md px-2 py-1 text-right tabular-nums hover:bg-muted/70",
        strong && "font-semibold",
        mismatch && "text-amber-600 dark:text-amber-400",
        selected && "bg-primary/15 ring-1 ring-primary/40",
      )}
    >
      {fmt(value)}
    </button>
  );
}

function Message({
  icon: Icon,
  title,
  detail,
}: {
  icon: typeof Inbox;
  title: string;
  detail: string;
}) {
  return (
    <div className="flex items-start gap-3 rounded-2xl border border-border/80 bg-card px-5 py-4 shadow-soft">
      <Icon className="mt-0.5 h-4 w-4 text-muted-foreground" />
      <div>
        <p className="text-sm font-medium">{title}</p>
        <p className="text-sm text-muted-foreground">{detail}</p>
      </div>
    </div>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
}
