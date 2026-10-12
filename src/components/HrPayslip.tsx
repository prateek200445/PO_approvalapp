import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, ExternalLink, Loader2, ReceiptIndianRupee, Wallet } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { getPayslip, getPayslipMonths, payslipPdfUrl, type HrPayslip, type HrPayslipLine } from "@/lib/hr-payslip-api";
import { cn } from "@/lib/utils";

const inr = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function monthLabel(ym: string) {
  const [y, m] = ym.split("-").map(Number);
  return new Date(y, m - 1, 1).toLocaleDateString("en-IN", { month: "long", year: "numeric" });
}

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join("");
}

export function HrPayslip({ username, empCode }: { username: string; empCode: string }) {
  const [yearMonth, setYearMonth] = useState("");

  const monthsQuery = useQuery({
    queryKey: ["hr-payslip-months", empCode, username],
    queryFn: () => getPayslipMonths(empCode, username),
    enabled: !!empCode && !!username,
    staleTime: 10 * 60_000,
  });

  useEffect(() => {
    setYearMonth("");
  }, [empCode]);

  useEffect(() => {
    const months = monthsQuery.data;
    if (months?.length && (!yearMonth || !months.includes(yearMonth))) setYearMonth(months[0]);
  }, [monthsQuery.data, yearMonth]);

  const slipQuery = useQuery({
    queryKey: ["hr-payslip", empCode, yearMonth, username],
    queryFn: () => getPayslip(empCode, yearMonth, username),
    enabled: !!empCode && !!username && !!yearMonth,
    staleTime: 10 * 60_000,
  });

  return (
    <section className="space-y-4 rounded-xl border border-t-4 border-border border-t-fuchsia-500 bg-card p-4 shadow-sm">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold text-fuchsia-700 dark:text-fuchsia-300">
            <ReceiptIndianRupee className="h-5 w-5" /> Salary slip
          </h2>
          <p className="text-xs text-muted-foreground">
            From the finalised (frozen) payroll in the ERP. A month appears once HR freezes its salary.
          </p>
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <div>
            <Label htmlFor="hr-payslip-month">Month</Label>
            <select
              id="hr-payslip-month"
              value={yearMonth}
              onChange={(e) => setYearMonth(e.target.value)}
              disabled={!monthsQuery.data?.length}
              className="mt-1 h-9 min-w-44 rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              {(monthsQuery.data ?? []).map((m) => (
                <option key={m} value={m}>
                  {monthLabel(m)}
                </option>
              ))}
            </select>
          </div>
          {slipQuery.data ? (
            <>
              <Button asChild variant="outline">
                <a href={payslipPdfUrl(empCode, yearMonth, username)} target="_blank" rel="noreferrer">
                  <ExternalLink className="h-4 w-4" /> Open PDF
                </a>
              </Button>
              <Button asChild className="bg-gradient-to-r from-indigo-600 to-fuchsia-600 text-white hover:opacity-90">
                <a href={payslipPdfUrl(empCode, yearMonth, username)} download>
                  <Download className="h-4 w-4" /> Download PDF
                </a>
              </Button>
            </>
          ) : null}
        </div>
      </div>

      {monthsQuery.isLoading || (slipQuery.isLoading && !!yearMonth) ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading salary slip…
        </div>
      ) : monthsQuery.isError ? (
        <p className="text-sm text-destructive">
          {monthsQuery.error instanceof Error ? monthsQuery.error.message : "Could not load salary months"}
        </p>
      ) : !monthsQuery.data?.length ? (
        <p className="rounded-xl border border-dashed border-border bg-secondary/20 px-4 py-8 text-center text-sm text-muted-foreground">
          No finalised salary found for this employee yet.
        </p>
      ) : slipQuery.isError ? (
        <p className="text-sm text-destructive">
          {slipQuery.error instanceof Error ? slipQuery.error.message : "Could not load the salary slip"}
        </p>
      ) : slipQuery.data ? (
        <PayslipView slip={slipQuery.data} />
      ) : null}
    </section>
  );
}

function PayslipView({ slip }: { slip: HrPayslip }) {
  const e = slip.employee;
  const facts: [string, string | null][] = [
    ["Employee code", e.empCode],
    ["Date of joining", e.dateOfJoining],
    ["Pay type", slip.payType],
    ["PAN", e.pan],
    ["PF account", e.pfAccount],
    ["UAN", e.uan],
    ["ESIC no", e.esic],
    ["Bank", [e.bankName, e.bankAccountMasked].filter(Boolean).join(" · ") || null],
  ];

  return (
    <div className="overflow-hidden rounded-2xl border border-indigo-200 shadow-lg dark:border-indigo-500/30">
      <div className="relative bg-gradient-to-r from-indigo-700 via-violet-700 to-fuchsia-600 px-5 py-4 text-white">
        <div className="pointer-events-none absolute -right-8 -top-12 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
        <div className="relative flex flex-wrap items-center justify-between gap-3">
          <div>
            <div className="text-lg font-bold">{e.company ?? "HCP Group"}</div>
            {e.branch ? <div className="text-xs text-indigo-100">{e.branch}</div> : null}
          </div>
          <div className="text-right">
            <div className="text-sm font-bold uppercase tracking-[0.2em]">Salary slip</div>
            <div className="text-sm font-semibold text-amber-200">{slip.periodLabel}</div>
          </div>
        </div>
      </div>
      <div className="flex h-1">
        <div className="flex-1 bg-fuchsia-500" />
        <div className="flex-1 bg-pink-500" />
        <div className="flex-1 bg-amber-400" />
      </div>

      <div className="space-y-4 bg-card p-4 md:p-5">
        <div className="flex flex-col-reverse gap-4 rounded-xl border border-indigo-100 bg-gradient-to-br from-indigo-50/80 to-fuchsia-50/60 p-4 sm:flex-row sm:items-start dark:border-indigo-500/20 dark:from-indigo-500/10 dark:to-fuchsia-500/5">
          <div className="min-w-0 flex-1">
            <div className="text-xl font-bold text-indigo-700 dark:text-indigo-200">{e.name}</div>
            <div className="text-sm text-muted-foreground">
              {[e.designation, e.department].filter(Boolean).join(" · ") || "—"}
            </div>
            <dl className="mt-3 grid gap-x-6 gap-y-1.5 text-sm sm:grid-cols-2">
              {facts.map(([k, v]) => (
                <div key={k} className="flex gap-1.5">
                  <dt className="text-muted-foreground">{k}:</dt>
                  <dd className="font-medium">{v || "—"}</dd>
                </div>
              ))}
            </dl>
          </div>
          <div className="mx-auto h-32 w-28 shrink-0 overflow-hidden rounded-xl border-4 border-white bg-white shadow-md ring-2 ring-fuchsia-400 sm:mx-0 dark:border-slate-800">
            {slip.photoDataUrl ? (
              <img src={slip.photoDataUrl} alt={e.name} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-indigo-500 to-fuchsia-500 text-3xl font-bold text-white">
                {initials(e.name)}
              </div>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">
          <Chip label="Days in month" value={slip.daysInMonth} tone="indigo" />
          <Chip label="Paid days" value={slip.paidDays} tone="emerald" />
          {slip.leaveDays ? <Chip label="Leave" value={slip.leaveDays} tone="blue" /> : null}
          {slip.overtimeHours ? <Chip label="Overtime" value={slip.overtimeHours} tone="orange" /> : null}
          {slip.ratePerDay ? <Chip label="Rate / day" value={`₹${inr.format(slip.ratePerDay)}`} tone="fuchsia" /> : null}
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <LinesTable title="Earnings" lines={slip.earnings} totalLabel="Gross earnings" total={slip.grossEarnings} tone="earn" />
          <LinesTable
            title="Deductions"
            lines={slip.deductions}
            totalLabel="Total deductions"
            total={slip.totalDeductions}
            tone="ded"
          />
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-gradient-to-r from-indigo-600 via-violet-600 to-fuchsia-600 px-5 py-4 text-white shadow-md">
          <div className="min-w-0">
            <div className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-widest text-indigo-100">
              <Wallet className="h-4 w-4" /> Net pay
            </div>
            <div className="mt-0.5 text-sm italic text-white/90">{slip.netPayInWords}</div>
          </div>
          <div className="text-3xl font-extrabold tabular-nums">₹{inr.format(slip.netPay)}</div>
        </div>

        {slip.notes.length > 0 ? (
          <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm dark:border-amber-500/30 dark:bg-amber-500/10">
            <div className="mb-1 text-xs font-semibold text-amber-800 dark:text-amber-200">
              Other amounts recorded in payroll for this month (for reference)
            </div>
            <ul className="space-y-0.5">
              {slip.notes.map((n) => (
                <li key={n.label} className="flex justify-between gap-3">
                  <span>{n.label}</span>
                  <span className="tabular-nums">{inr.format(n.amount)}</span>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <p className="text-center text-[11px] text-muted-foreground">
          Computer-generated salary slip — no signature required.
        </p>
      </div>
    </div>
  );
}

const CHIP_TONES = {
  indigo: "bg-indigo-50 text-indigo-700 dark:bg-indigo-500/15 dark:text-indigo-200",
  emerald: "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-200",
  blue: "bg-blue-50 text-blue-700 dark:bg-blue-500/15 dark:text-blue-200",
  orange: "bg-orange-50 text-orange-700 dark:bg-orange-500/15 dark:text-orange-200",
  fuchsia: "bg-fuchsia-50 text-fuchsia-700 dark:bg-fuchsia-500/15 dark:text-fuchsia-200",
} as const;

function Chip({ label, value, tone }: { label: string; value: number | string; tone: keyof typeof CHIP_TONES }) {
  return (
    <div className={cn("rounded-xl px-3 py-2", CHIP_TONES[tone])}>
      <div className="text-[11px] opacity-80">{label}</div>
      <div className="text-lg font-bold tabular-nums">{value}</div>
    </div>
  );
}

function LinesTable({
  title,
  lines,
  totalLabel,
  total,
  tone,
}: {
  title: string;
  lines: HrPayslipLine[];
  totalLabel: string;
  total: number;
  tone: "earn" | "ded";
}) {
  const earn = tone === "earn";
  return (
    <div
      className={cn(
        "overflow-hidden rounded-xl border",
        earn ? "border-emerald-200 dark:border-emerald-500/30" : "border-red-200 dark:border-red-500/30",
      )}
    >
      <div
        className={cn(
          "flex justify-between px-4 py-2 text-sm font-semibold text-white",
          earn ? "bg-gradient-to-r from-emerald-600 to-teal-500" : "bg-gradient-to-r from-rose-600 to-red-500",
        )}
      >
        <span>{title}</span>
        <span className="text-xs font-medium opacity-90">Amount (₹)</span>
      </div>
      <ul className="text-sm">
        {lines.length === 0 ? <li className="px-4 py-2 text-muted-foreground">—</li> : null}
        {lines.map((l, i) => (
          <li
            key={`${l.label}-${i}`}
            className={cn(
              "flex justify-between gap-3 px-4 py-1.5",
              i % 2 === 1 && (earn ? "bg-emerald-50/60 dark:bg-emerald-500/5" : "bg-red-50/60 dark:bg-red-500/5"),
            )}
          >
            <span>{l.label}</span>
            <span className="tabular-nums">{inr.format(l.amount)}</span>
          </li>
        ))}
      </ul>
      <div
        className={cn(
          "flex justify-between border-t px-4 py-2 text-sm font-bold",
          earn
            ? "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200"
            : "border-red-200 bg-red-50 text-red-800 dark:border-red-500/30 dark:bg-red-500/10 dark:text-red-200",
        )}
      >
        <span>{totalLabel}</span>
        <span className="tabular-nums">{inr.format(total)}</span>
      </div>
    </div>
  );
}
