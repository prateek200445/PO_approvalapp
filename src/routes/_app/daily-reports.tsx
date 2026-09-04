import { useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Check, ChevronsUpDown, ClipboardList, Loader2 } from "lucide-react";
import {
  currentMonthValue,
  formatReportDate,
  formatReportDateTime,
  getDailyReportMonths,
  getDailyReportPeople,
  getDailyReports,
  parseMonthValue,
} from "@/lib/daily-report-api";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/daily-reports")({
  head: () => ({ meta: [{ title: "Daily Reports — PO Portal" }] }),
  component: DailyReportsPage,
});

function DailyReportsPage() {
  const [monthValue, setMonthValue] = useState(currentMonthValue);
  const [employee, setEmployee] = useState("");
  const [personOpen, setPersonOpen] = useState(false);
  const parsedMonth = parseMonthValue(monthValue);

  const peopleQuery = useQuery({
    queryKey: ["daily-report-people", parsedMonth?.year, parsedMonth?.month],
    queryFn: () => getDailyReportPeople(parsedMonth?.year, parsedMonth?.month),
    staleTime: 30 * 60_000,
    enabled: !!parsedMonth,
  });

  const monthsQuery = useQuery({
    queryKey: ["daily-report-months"],
    queryFn: getDailyReportMonths,
    staleTime: 30 * 60_000,
  });

  const reportsQuery = useQuery({
    queryKey: ["daily-reports", parsedMonth?.year, parsedMonth?.month, employee],
    queryFn: () =>
      getDailyReports({
        year: parsedMonth!.year,
        month: parsedMonth!.month,
        employee,
      }),
    enabled: !!parsedMonth && employee.trim().length > 0,
    staleTime: 5 * 60_000,
  });

  const monthOptions = useMemo(() => {
    const fromApi = monthsQuery.data ?? [];
    const now = new Date();
    const current = {
      year: now.getFullYear(),
      month: now.getMonth() + 1,
      value: currentMonthValue(now),
      label: now.toLocaleDateString("en-IN", { month: "long", year: "numeric" }),
      reportCount: 0,
    };
    const list =
      fromApi.length > 0
        ? fromApi
        : Array.from({ length: 18 }, (_, i) => {
            const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
            const year = d.getFullYear();
            const month = d.getMonth() + 1;
            return {
              year,
              month,
              value: `${year}-${String(month).padStart(2, "0")}`,
              label: d.toLocaleDateString("en-IN", { month: "long", year: "numeric" }),
              reportCount: 0,
            };
          });
    if (!list.some((m) => m.value === current.value)) {
      return [current, ...list];
    }
    return list;
  }, [monthsQuery.data]);

  const people = peopleQuery.data ?? [];
  const reports = reportsQuery.data ?? [];

  return (
    <div className="space-y-5">
      <div>
        <div className="flex items-center gap-2 text-primary">
          <ClipboardList className="h-5 w-5" />
          <p className="text-xs font-semibold uppercase tracking-wide">Reports</p>
        </div>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">Daily Reports</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Choose a month and a person to load their submitted daily work reports
          {peopleQuery.isSuccess ? ` (${people.length} ${people.length === 1 ? "person" : "people"} this month)` : ""}.
        </p>
      </div>

      <section className="card-3d rounded-2xl p-3 sm:p-4" aria-label="Daily report filters">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="min-w-0 space-y-1.5">
            <Label htmlFor="daily-report-month">Month</Label>
            <select
              id="daily-report-month"
              value={monthValue}
              onChange={(e) => setMonthValue(e.target.value)}
              className="h-11 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              {monthOptions.map((m) => (
                <option key={m.value} value={m.value}>
                  {m.label}
                  {m.reportCount > 0 ? ` (${m.reportCount})` : ""}
                </option>
              ))}
            </select>
          </div>

          <div className="min-w-0 space-y-1.5">
            <Label>
              Person
              {peopleQuery.isSuccess ? (
                <span className="ml-1.5 font-normal text-muted-foreground">
                  ({people.length})
                </span>
              ) : null}
            </Label>
            <Popover open={personOpen} onOpenChange={setPersonOpen}>
              <PopoverTrigger asChild>
                <Button
                  type="button"
                  variant="outline"
                  role="combobox"
                  aria-expanded={personOpen}
                  disabled={peopleQuery.isLoading}
                  className="h-11 w-full justify-between bg-background px-3 font-normal"
                >
                  <span className="truncate">
                    {peopleQuery.isLoading
                      ? "Loading names…"
                      : employee ||
                        (peopleQuery.isSuccess
                          ? `Search or select a person (${people.length})`
                          : "Search or select a person")}
                  </span>
                  <ChevronsUpDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                </Button>
              </PopoverTrigger>
              <PopoverContent align="start" className="w-[var(--radix-popover-trigger-width)] p-0">
                <Command>
                  <CommandInput placeholder="Search name…" />
                  <CommandList>
                    <CommandEmpty>No person found.</CommandEmpty>
                    <CommandGroup heading={peopleQuery.isSuccess ? `${people.length} people` : undefined}>
                      {people.map((name) => (
                        <CommandItem
                          key={name}
                          value={name}
                          onSelect={() => {
                            setEmployee(name);
                            setPersonOpen(false);
                          }}
                        >
                          <Check
                            className={cn("h-4 w-4", employee === name ? "opacity-100" : "opacity-0")}
                          />
                          {name}
                        </CommandItem>
                      ))}
                    </CommandGroup>
                  </CommandList>
                </Command>
              </PopoverContent>
            </Popover>
          </div>
        </div>
      </section>

      {!employee && (
        <p className="text-sm text-muted-foreground">
          {peopleQuery.isSuccess
            ? `${people.length} ${people.length === 1 ? "person" : "people"} in this month. Select a person to load reports.`
            : "Select a person to load reports for this month."}
        </p>
      )}

      {reportsQuery.isFetching && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading reports…
        </div>
      )}

      {reportsQuery.isError && (
        <p className="text-sm text-destructive" role="alert">
          {reportsQuery.error instanceof Error ? reportsQuery.error.message : "Failed to load reports."}
        </p>
      )}

      {employee && reportsQuery.isSuccess && reports.length === 0 && (
        <p className="text-sm text-muted-foreground">No daily reports for {employee} in this month.</p>
      )}

      <div className="space-y-4">
        {reports.map((report, index) => (
          <article
            key={`${report.submittedForDate}-${report.submittedOn}-${index}`}
            className="rounded-2xl border border-border bg-card p-4 shadow-soft sm:p-5"
          >
            <header className="border-b border-border pb-3">
              <h2 className="text-base font-semibold">{report.employeeName || employee}</h2>
              <p className="mt-0.5 text-sm text-muted-foreground">
                {report.department || "—"}
              </p>
              <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                <span>Report date: {formatReportDate(report.submittedForDate)}</span>
                <span>Submitted: {formatReportDateTime(report.submittedOn)}</span>
              </div>
            </header>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <section>
                <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  First half
                </h3>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed">
                  {report.firstHalf || "—"}
                </p>
              </section>
              <section>
                <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Second half
                </h3>
                <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed">
                  {report.secondHalf || "—"}
                </p>
              </section>
            </div>

            <section className="mt-4">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Tomorrow’s tasks
              </h3>
              {report.tomorrowTasks.length === 0 ? (
                <p className="mt-1 text-sm text-muted-foreground">No tasks.</p>
              ) : (
                <ul className="mt-1 list-disc space-y-1 pl-5 text-sm">
                  {report.tomorrowTasks.map((task, i) => (
                    <li key={`${i}-${task.slice(0, 24)}`}>{task}</li>
                  ))}
                </ul>
              )}
            </section>
          </article>
        ))}
      </div>
    </div>
  );
}
