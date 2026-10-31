import { useMemo, useState, type ReactNode } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertCircle, ChevronLeft, ChevronRight, Inbox, Loader2 } from "lucide-react";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  financialQuarters,
  getTdsGroups,
  getTds94A,
  getTds94C,
  getTdsSection,
  type TdsSectionLine,
  type TdsSectionSheet,
  type TdsUnbookedLine,
} from "@/lib/tds-api";

type SectionCode = "94A" | "94C" | "94H" | "94I" | "94J" | "94Q";

const PAGE_SIZE = 100;
const RATE_NEAR = 0.06;

const money = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmt = (n: number) => money.format(n || 0);
const dateFmt = new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" });

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

export function TdsSection94A() {
  return <SectionSheet code="94A" load={getTds94A} />;
}

export function TdsSection94C() {
  return <SectionSheet code="94C" load={getTds94C} />;
}

export function TdsSection94H() {
  return <SectionSheet code="94H" load={(group, from, to) => getTdsSection("94H", group, from, to)} />;
}

export function TdsSection94I() {
  return <SectionSheet code="94I" load={(group, from, to) => getTdsSection("94I", group, from, to)} />;
}

export function TdsSection94J() {
  return <SectionSheet code="94J" load={(group, from, to) => getTdsSection("94J", group, from, to)} />;
}

export function TdsSection94Q() {
  return <SectionSheet code="94Q" load={(group, from, to) => getTdsSection("94Q", group, from, to)} />;
}

function SectionSheet({
  code,
  load,
}: {
  code: SectionCode;
  load: (group: string, from: string, to: string) => Promise<TdsSectionSheet>;
}) {
  const quarters = useMemo(() => financialQuarters(), []);
  const [group, setGroup] = useState("");
  const [quarterId, setQuarterId] = useState(quarters[0]?.id ?? "");
  const [partyQuery, setPartyQuery] = useState("");
  const [certificateOnly, setCertificateOnly] = useState(false);
  const [adjustedOnly, setAdjustedOnly] = useState(false);
  const [otherOnly, setOtherOnly] = useState(false);
  const [anomalyOnly, setAnomalyOnly] = useState(false);
  const [page, setPage] = useState(0);
  const quarter = quarters.find((q) => q.id === quarterId) ?? quarters[0];

  const groupsQuery = useQuery({
    queryKey: ["tds-groups"],
    queryFn: getTdsGroups,
    staleTime: 10 * 60_000,
  });

  const sheetQuery = useQuery({
    queryKey: ["tds-section", code, group, quarter?.from, quarter?.to],
    queryFn: () => load(group, quarter.from, quarter.to),
    enabled: group.length > 0 && !!quarter,
    placeholderData: keepPreviousData,
  });

  const data = sheetQuery.data;
  const matches = data != null && Math.abs(data.totalTds - data.liability) < 0.005;
  const needle = partyQuery.trim().toLowerCase();
  const unbooked = data?.unbooked ?? [];
  const unbookedShown = unbooked.filter((line) => {
    if (!needle) return true;
    return (
      line.party.toLowerCase().includes(needle) ||
      line.pan.toLowerCase().includes(needle) ||
      line.voucherNo.toLowerCase().includes(needle)
    );
  });

  const annotated = useMemo(() => {
    return (data?.lines ?? []).map((line) => ({
      line,
      certificate: isLowerCertificate(line),
      reduced: isAdjusted(line),
      other: otherSection(code, line),
      anomaly: anomalyLabel(code, line),
    }));
  }, [data?.lines, code]);

  const categoryOn = certificateOnly || adjustedOnly || otherOnly || anomalyOnly;
  const lines = annotated.filter(({ line, certificate, reduced, other, anomaly }) => {
    if (categoryOn) {
      const hit =
        (certificateOnly && certificate) ||
        (adjustedOnly && reduced) ||
        (otherOnly && !!other) ||
        (anomalyOnly && !!anomaly);
      if (!hit) return false;
    }
    if (!needle) return true;
    return (
      line.party.toLowerCase().includes(needle) ||
      line.pan.toLowerCase().includes(needle) ||
      line.voucherNo.toLowerCase().includes(needle)
    );
  });

  const certificateCount = annotated.filter((row) => row.certificate).length;
  const adjustedCount = annotated.filter((row) => row.reduced).length;
  const otherCount = annotated.filter((row) => row.other).length;
  const anomalyCount = annotated.filter((row) => row.anomaly).length;
  const pageCount = Math.max(1, Math.ceil(lines.length / PAGE_SIZE));
  const safePage = Math.min(page, pageCount - 1);
  const pageLines = lines.slice(safePage * PAGE_SIZE, safePage * PAGE_SIZE + PAGE_SIZE);
  const fromRow = lines.length === 0 ? 0 : safePage * PAGE_SIZE + 1;
  const toRow = Math.min(lines.length, (safePage + 1) * PAGE_SIZE);

  function resetPage() {
    setPage(0);
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 rounded-2xl border border-border/80 bg-card p-4 shadow-soft sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label htmlFor={`tds-${code}-group`}>Group</Label>
          <select
            id={`tds-${code}-group`}
            className={SELECT_CLASS}
            value={group}
            onChange={(event) => {
              setGroup(event.target.value);
              resetPage();
            }}
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
          <Label htmlFor={`tds-${code}-quarter`}>Quarter</Label>
          <select
            id={`tds-${code}-quarter`}
            className={SELECT_CLASS}
            value={quarter?.id ?? ""}
            onChange={(event) => {
              setQuarterId(event.target.value);
              resetPage();
            }}
          >
            {quarters.map((item) => (
              <option key={item.id} value={item.id}>
                {item.label}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor={`tds-${code}-find`}>Find</Label>
          <input
            id={`tds-${code}-find`}
            className={SELECT_CLASS}
            value={partyQuery}
            onChange={(event) => {
              setPartyQuery(event.target.value);
              resetPage();
            }}
            placeholder="Party, PAN, or voucher"
          />
        </div>
        <div className="flex flex-wrap gap-2 sm:col-span-2">
          <FilterButton
            pressed={certificateOnly}
            onClick={() => {
              setCertificateOnly((value) => !value);
              resetPage();
            }}
          >
            Lower certificate{data ? ` (${certificateCount})` : ""}
          </FilterButton>
          <FilterButton
            pressed={adjustedOnly}
            onClick={() => {
              setAdjustedOnly((value) => !value);
              resetPage();
            }}
          >
            Adjusted{data ? ` (${adjustedCount})` : ""}
          </FilterButton>
          <FilterButton
            pressed={otherOnly}
            onClick={() => {
              setOtherOnly((value) => !value);
              resetPage();
            }}
          >
            Other section{data ? ` (${otherCount})` : ""}
          </FilterButton>
          <FilterButton
            pressed={anomalyOnly}
            onClick={() => {
              setAnomalyOnly((value) => !value);
              resetPage();
            }}
          >
            Anomaly{data ? ` (${anomalyCount})` : ""}
          </FilterButton>
        </div>
      </div>

      {!group && <Empty message="Choose a group and a quarter." />}
      {group && sheetQuery.isLoading && !data && <Loading code={code} />}
      {group && sheetQuery.isError && <ErrorText message={(sheetQuery.error as Error).message} />}

      {data && (
        <div className="space-y-3">
          <div className="sticky top-0 z-20 flex flex-wrap items-end justify-between gap-x-6 gap-y-2 rounded-2xl border border-border/80 bg-card/95 px-4 py-3 shadow-soft backdrop-blur">
            <Total label="Lines" value={String(data.lines.length)} />
            <Total label="Reversal" value={fmt(data.reversalDebit)} />
            <Total label="Principal" value={fmt(data.totalPrincipal)} />
            <Total label="Sheet TDS" value={fmt(data.totalTds)} emphasize mismatch={!matches} />
            <Total label={`${code} liability`} value={fmt(data.liability)} />
          </div>

          {code === "94Q" && (
            <UnbookedNote lines={unbookedShown} allLines={unbooked} notPostedTds={data.unbookedTds ?? 0} />
          )}

          <div className="max-h-[70vh] overflow-auto rounded-2xl border border-border/80 bg-card shadow-soft">
            <table className="w-full min-w-[980px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-border/80 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">Date</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">Unit</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">Voucher</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">Party</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">PAN</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 text-right font-medium">Principal</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 text-right font-medium">Rate</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 text-right font-medium">TDS</th>
                  <th className="sticky top-0 z-10 bg-card px-3 py-3 font-medium">Possible section</th>
                </tr>
              </thead>
              <tbody>
                {pageLines.length === 0 && (
                  <tr>
                    <td colSpan={9} className="px-3 py-8 text-center text-muted-foreground">
                      No {code} lines match.
                    </td>
                  </tr>
                )}
                {pageLines.map(({ line, certificate, reduced, other, anomaly }, index) => (
                  <tr
                    key={`${line.unit}-${line.date}-${line.voucherNo}-${line.party}-${index}`}
                    className={cn("border-b border-border/60", (other || anomaly) && "bg-rose-500/10")}
                  >
                    <td className="px-3 py-2.5 whitespace-nowrap">{dateFmt.format(new Date(line.date))}</td>
                    <td className="px-3 py-2.5">{line.unit}</td>
                    <td className="px-3 py-2.5 whitespace-nowrap">
                      {line.voucherType} {line.voucherNo}
                    </td>
                    <td className="px-3 py-2.5">{line.party || "—"}</td>
                    <td className="px-3 py-2.5 font-mono text-xs">{line.pan || "—"}</td>
                    <td className="px-3 py-2.5 text-right tabular-nums">{line.principal > 0 ? fmt(line.principal) : "—"}</td>
                    <td className="px-3 py-2.5 text-right tabular-nums">{line.principal > 0 ? line.rate.toFixed(2) : "—"}</td>
                    <td className={cn("px-3 py-2.5 text-right tabular-nums", reduced && "text-amber-600 dark:text-amber-400")}>
                      {fmt(line.tds)}
                      {reduced && (
                        <span className="mt-0.5 block text-[11px] font-normal text-muted-foreground">
                          was {fmt(line.originalTds)}
                        </span>
                      )}
                    </td>
                    <td className={cn("px-3 py-2.5", (other || anomaly) && "font-medium text-rose-700 dark:text-rose-300")}>
                      {other ?? anomaly ?? (certificate ? "Lower certificate" : "—")}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
            <p>
              {fromRow}–{toRow} of {lines.length}
              {lines.length !== data.lines.length ? ` shown, ${data.lines.length} on the sheet` : ""}. Sheet TDS{" "}
              {fmt(data.totalTds)} against {code} liability {fmt(data.liability)}.
              {data.roundOffRemoved > 0 && ` ${data.roundOffRemoved} round-off row${data.roundOffRemoved === 1 ? "" : "s"} removed.`}
              {data.duplicatesRemoved > 0 &&
                ` ${data.duplicatesRemoved} duplicate blank-PAN row${data.duplicatesRemoved === 1 ? "" : "s"} removed.`}
            </p>
            <div className="flex items-center gap-2">
              <button
                type="button"
                className="inline-flex h-8 items-center gap-1 rounded-lg border border-border px-2.5 disabled:opacity-40"
                onClick={() => setPage(safePage - 1)}
                disabled={safePage === 0}
              >
                <ChevronLeft className="size-4" />
                Previous
              </button>
              <span className="tabular-nums">
                {safePage + 1} / {pageCount}
              </span>
              <button
                type="button"
                className="inline-flex h-8 items-center gap-1 rounded-lg border border-border px-2.5 disabled:opacity-40"
                onClick={() => setPage(safePage + 1)}
                disabled={safePage >= pageCount - 1}
              >
                Next
                <ChevronRight className="size-4" />
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function isLowerCertificate(line: TdsSectionLine) {
  const name = line.party.toLowerCase();
  return name.includes("suzlon") || name.includes("blackstone");
}

function isAdjusted(line: TdsSectionLine) {
  return Math.abs(line.tds - line.originalTds) >= 0.005;
}

/** A rate that matches another section's statutory rate. Certificate parties are left as they are. */
function sectionRates(code: SectionCode) {
  if (code === "94A") return [10];
  if (code === "94C") return [1, 2];
  if (code === "94H") return [2];
  if (code === "94Q") return [0.1];
  return [2, 10];
}

function ownsRate(code: SectionCode, rate: number) {
  const band = code === "94Q" ? 0.005 : 0.05;
  return sectionRates(code).some((target) => Math.abs(rate - target) < band);
}

function anomalyLabel(code: SectionCode, line: TdsSectionLine) {
  if (isLowerCertificate(line) || otherSection(code, line)) return null;
  if (line.principal <= 0) return "No bill";
  if (ownsRate(code, line.rate)) return null;
  return "Rate doesn't match";
}

function otherSection(code: SectionCode, line: TdsSectionLine) {
  if (isLowerCertificate(line) || line.principal <= 0) return null;
  const rate = line.rate;
  if (ownsRate(code, rate)) return null;
  if (code === "94Q" && near(rate, 0.1)) return null;
  if (near(rate, 0.1)) return "94Q";
  if (near(rate, 5)) return code === "94H" ? null : "94H";
  if (near(rate, 10)) return "94A, 94I, or 94J";
  if (near(rate, 1) || near(rate, 2)) return "94C";
  return null;
}

function near(rate: number, target: number) {
  return Math.abs(rate - target) < RATE_NEAR;
}

function UnbookedNote({
  lines,
  allLines,
  notPostedTds,
}: {
  lines: TdsUnbookedLine[];
  allLines: TdsUnbookedLine[];
  notPostedTds: number;
}) {
  const missingCount = allLines.filter((line) => !line.covered).length;
  const journalCount = allLines.length - missingCount;
  return (
    <div className="rounded-2xl border border-amber-500/40 bg-amber-500/5 px-4 py-3">
      <p className="text-sm">
        <span className="font-medium">Not added to Sheet TDS.</span> Purchase bills from domestic suppliers already past
        ₹50 lakh this quarter, saved with no 194Q line.
        {missingCount > 0 && ` ${fmt(notPostedTds)} on ${missingCount} ${missingCount === 1 ? "bill was" : "bills were"} never posted, so it is not in the liability.`}
        {journalCount > 0 &&
          ` ${journalCount} ${journalCount === 1 ? "bill is" : "bills are"} already on a journal, so that TDS is already in the sheet and is not added again.`}
        {allLines.length === 0 && " None this quarter."}
      </p>
      {lines.length > 0 && (
        <div className="mt-3 max-h-64 overflow-auto">
          <table className="w-full min-w-[920px] border-collapse text-sm">
            <thead>
              <tr className="border-b border-amber-500/30 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">Date</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">Unit</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">Voucher</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">Party</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">PAN</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 text-right font-medium dark:bg-amber-950">Principal</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 text-right font-medium dark:bg-amber-950">0.1%</th>
                <th className="sticky top-0 bg-amber-50 px-3 py-2 font-medium dark:bg-amber-950">Where it stands</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line, index) => (
                <tr key={`${line.unit}-${line.date}-${line.voucherNo}-${line.party}-${index}`} className="border-b border-amber-500/20">
                  <td className="px-3 py-2 whitespace-nowrap">{dateFmt.format(new Date(line.date))}</td>
                  <td className="px-3 py-2">{line.unit}</td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    {line.voucherType} {line.voucherNo}
                  </td>
                  <td className="px-3 py-2">{line.party}</td>
                  <td className="px-3 py-2 font-mono text-xs">{line.pan || "—"}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{fmt(line.principal)}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{fmt(line.tds)}</td>
                  <td className="px-3 py-2">{line.covered ? "Already on a journal" : "Not posted"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function Total({ label, value, emphasize }: { label: string; value: string; emphasize?: boolean }) {
  return (
    <div>
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className={cn("text-sm font-medium tabular-nums", emphasize && "text-amber-600 dark:text-amber-400")}>{value}</div>
    </div>
  );
}

function FilterButton({
  pressed,
  onClick,
  children,
}: {
  pressed: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      aria-pressed={pressed}
      onClick={onClick}
      className={cn(
        "h-9 rounded-lg border px-3 text-sm font-medium transition",
        pressed
          ? "border-primary bg-primary text-primary-foreground"
          : "border-border bg-background text-muted-foreground hover:text-foreground",
      )}
    >
      {children}
    </button>
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

function Loading({ code }: { code: string }) {
  return (
    <div className="flex items-center gap-2 px-1 text-sm text-muted-foreground">
      <Loader2 className="size-4 animate-spin" />
      Building the {code} sheet…
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
