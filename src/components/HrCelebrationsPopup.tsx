import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Award, Cake, PartyPopper } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { useAuth } from "@/lib/auth-context";
import { getHrCelebrations, type HrCelebrationPerson } from "@/lib/hr-engagement-api";
import { cn } from "@/lib/utils";

function todayKey() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function ordinal(n: number) {
  if (n % 100 >= 11 && n % 100 <= 13) return `${n}th`;
  return `${n}${["th", "st", "nd", "rd"][n % 10] ?? "th"}`;
}

/** Once a day per user: today's birthdays and work anniversaries in the user's company. */
export function HrCelebrationsPopup() {
  const { user } = useAuth();
  const username = user?.username?.trim() ?? "";
  const seenKey = `hr-celebrations-seen:${username.toLowerCase()}:${todayKey()}`;
  const [open, setOpen] = useState(false);

  const query = useQuery({
    queryKey: ["hr-celebrations", username, todayKey()],
    queryFn: () => getHrCelebrations(username),
    enabled: username.length > 0,
    staleTime: 30 * 60_000,
    retry: false,
    refetchOnWindowFocus: false,
  });

  const data = query.data;
  const total = (data?.birthdays.length ?? 0) + (data?.anniversaries.length ?? 0);

  useEffect(() => {
    if (total === 0) return;
    if (localStorage.getItem(seenKey)) return;
    setOpen(true);
  }, [total, seenKey]);

  function close() {
    localStorage.setItem(seenKey, "1");
    setOpen(false);
  }

  if (!data || total === 0) return null;
  const mine = [...data.birthdays, ...data.anniversaries].find((p) => p.isYou);

  return (
    <Dialog open={open} onOpenChange={(o) => (o ? setOpen(true) : close())}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl">
            <PartyPopper className="h-6 w-6 text-pink-500" />
            {mine ? `Happy ${mine.years != null ? "work anniversary" : "birthday"}, ${mine.name.split(" ")[0]}!` : "Today's celebrations"}
          </DialogTitle>
          <DialogDescription>{data.company}</DialogDescription>
        </DialogHeader>

        {data.birthdays.length > 0 ? (
          <section className="space-y-2">
            <h3 className="flex items-center gap-1.5 text-sm font-semibold text-pink-700 dark:text-pink-300">
              <Cake className="h-4 w-4" /> Birthdays ({data.birthdays.length})
            </h3>
            <ul className="space-y-1.5">
              {data.birthdays.map((p) => (
                <PersonRow key={`b-${p.empCode}`} person={p} tone="pink" />
              ))}
            </ul>
          </section>
        ) : null}

        {data.anniversaries.length > 0 ? (
          <section className="space-y-2">
            <h3 className="flex items-center gap-1.5 text-sm font-semibold text-amber-700 dark:text-amber-300">
              <Award className="h-4 w-4" /> Work anniversaries ({data.anniversaries.length})
            </h3>
            <ul className="space-y-1.5">
              {data.anniversaries.map((p) => (
                <PersonRow key={`a-${p.empCode}`} person={p} tone="amber" />
              ))}
            </ul>
          </section>
        ) : null}

        <div className="flex justify-end pt-1">
          <Button type="button" onClick={close}>
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function PersonRow({ person, tone }: { person: HrCelebrationPerson; tone: "pink" | "amber" }) {
  const detail = [person.designation, person.department, person.branch].filter(Boolean).join(" · ");
  return (
    <li
      className={cn(
        "flex items-center justify-between gap-3 rounded-lg border px-3 py-2 text-sm",
        tone === "pink"
          ? "border-pink-200 bg-pink-50 dark:border-pink-500/30 dark:bg-pink-500/10"
          : "border-amber-200 bg-amber-50 dark:border-amber-500/30 dark:bg-amber-500/10",
        person.isYou && "ring-2 ring-primary/50",
      )}
    >
      <div className="min-w-0">
        <div className="truncate font-medium">
          {person.name}
          {person.isYou ? <span className="ml-1.5 text-xs font-semibold text-primary">(you)</span> : null}
        </div>
        {detail ? <div className="truncate text-xs text-muted-foreground">{detail}</div> : null}
      </div>
      {person.years != null ? (
        <span className="shrink-0 rounded-full bg-amber-200/70 px-2 py-0.5 text-xs font-semibold text-amber-900 dark:bg-amber-500/25 dark:text-amber-100">
          {ordinal(person.years)} year
        </span>
      ) : null}
    </li>
  );
}
