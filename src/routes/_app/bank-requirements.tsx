import { useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Download, FileSpreadsheet, Landmark, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { getSalesCompanies, indianFyStartYear } from "@/lib/sales-dashboard-api";
import {
  allFyMonthKeys,
  defaultBankMonths,
  downloadBankSalesExcel,
  downloadBankSalesPdf,
  formatMn,
  fyMonthKeys,
  getBankSalesProfile,
  monthLabel,
} from "@/lib/bank-requirements-api";

export const Route = createFileRoute("/_app/bank-requirements")({
  head: () => ({ meta: [{ title: "Bank Requirements — PO Portal" }] }),
  component: BankRequirementsPage,
});

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

function BankRequirementsPage() {
  const currentFy = indianFyStartYear();
  const [fyStart, setFyStart] = useState(currentFy);
  const [months, setMonths] = useState<string[]>(() => defaultBankMonths());
  const [company, setCompany] = useState("All Companies");
  const [refreshToken, setRefreshToken] = useState(0);
  const [downloading, setDownloading] = useState<"excel" | "pdf" | null>(null);
  const bypassCacheRef = useRef(false);

  const fyMonths = useMemo(() => allFyMonthKeys(fyStart), [fyStart]);
  const selectableMonths = useMemo(
    () => (fyStart === currentFy ? fyMonthKeys(fyStart, true) : fyMonths),
    [fyStart, currentFy, fyMonths],
  );

  const { data: companyList, isLoading: companiesLoading } = useQuery({
    queryKey: ["sales-dashboard-companies"],
    queryFn: getSalesCompanies,
    staleTime: 60 * 60_000,
  });

  const profileQuery = useQuery({
    queryKey: ["bank-sales-profile", company, months.join(","), refreshToken],
    queryFn: () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getBankSalesProfile(company, months, refresh);
    },
    enabled: months.length > 0,
    staleTime: 30 * 60_000,
    placeholderData: keepPreviousData,
    retry: 1,
  });

  const profile = profileQuery.data;

  function applyFy(start: number) {
    setFyStart(start);
    setMonths(start === currentFy ? fyMonthKeys(start, true) : allFyMonthKeys(start));
  }

  function toggleMonth(key: string) {
    setMonths((prev) => {
      if (prev.includes(key)) {
        const next = prev.filter((m) => m !== key);
        return next.length ? next : prev;
      }
      return [...prev, key].sort();
    });
  }

  async function onDownload(kind: "excel" | "pdf") {
    setDownloading(kind);
    try {
      const name =
        kind === "excel"
          ? await downloadBankSalesExcel(company, months)
          : await downloadBankSalesPdf(company, months);
      toast.success(`Downloaded ${name}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Download failed");
    } finally {
      setDownloading(null);
    }
  }

  const rows = [
    { sr: 1, name: "Export", amount: profile?.exportAmountCr ?? 0, share: profile?.exportShare ?? 0 },
    { sr: 2, name: "Domestic", amount: profile?.domesticAmountCr ?? 0, share: profile?.domesticShare ?? 0 },
  ];

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex items-center gap-2 text-primary">
            <Landmark className="h-5 w-5" />
            <p className="text-xs font-semibold uppercase tracking-wide">Bank Requirements</p>
          </div>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight md:text-3xl">Profile of Sales</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Details may be provided on approximate basis. Taxable sales invoices in INR crore,
            excluding InterUnit and job/other sales.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={profileQuery.isFetching}
            onClick={() => {
              bypassCacheRef.current = true;
              setRefreshToken((n) => n + 1);
            }}
          >
            <RefreshCw className={cn("h-4 w-4", profileQuery.isFetching && "animate-spin")} />
            Refresh
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!profile || downloading !== null}
            onClick={() => void onDownload("excel")}
          >
            {downloading === "excel" ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileSpreadsheet className="h-4 w-4" />}
            Excel
          </Button>
          <Button
            size="sm"
            disabled={!profile || downloading !== null}
            onClick={() => void onDownload("pdf")}
          >
            {downloading === "pdf" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            PDF
          </Button>
        </div>
      </div>

      <section className="card-3d space-y-4 rounded-2xl p-4" aria-label="Bank requirements filters">
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="bank-company">Company</Label>
            <select
              id="bank-company"
              className={SELECT_CLASS}
              value={company}
              disabled={companiesLoading}
              onChange={(e) => setCompany(e.target.value || "All Companies")}
            >
              <option value="All Companies">{companiesLoading ? "Loading…" : "All Companies"}</option>
              {companyList?.options
                ?.filter((o) => o.kind === "group")
                .map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              {companyList?.options
                ?.filter((o) => o.kind === "company")
                .map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
            </select>
          </div>
          <div className="space-y-1.5">
            <Label>Financial year</Label>
            <div className="flex overflow-hidden rounded-md border border-border p-0.5">
              {[
                { start: currentFy, label: "Current FY" },
                { start: currentFy - 1, label: "Previous FY" },
              ].map((opt) => (
                <button
                  key={opt.start}
                  type="button"
                  className={cn(
                    "h-8 flex-1 rounded-sm text-sm",
                    fyStart === opt.start ? "bg-primary text-primary-foreground" : "hover:bg-accent",
                  )}
                  onClick={() => applyFy(opt.start)}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="space-y-1.5">
          <div className="flex items-center justify-between gap-2">
            <Label>Months</Label>
            <div className="flex gap-2">
              <button
                type="button"
                className="text-xs text-primary hover:underline"
                onClick={() => setMonths([...selectableMonths])}
              >
                Select available
              </button>
              <button
                type="button"
                className="text-xs text-muted-foreground hover:underline"
                onClick={() => setMonths([selectableMonths[0]])}
              >
                One month
              </button>
            </div>
          </div>
          <div className="flex flex-wrap gap-1.5">
            {fyMonths.map((key) => {
              const available = selectableMonths.includes(key);
              const selected = months.includes(key);
              return (
                <button
                  key={key}
                  type="button"
                  disabled={!available}
                  onClick={() => toggleMonth(key)}
                  className={cn(
                    "min-w-[3.4rem] rounded-md border px-2 py-1.5 text-xs font-medium",
                    selected
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-background hover:bg-accent",
                    !available && "cursor-not-allowed opacity-40",
                  )}
                >
                  {monthLabel(key)}
                </button>
              );
            })}
          </div>
          <p className="text-xs text-muted-foreground">
            Select one month or any group of months. The table column header follows the bank format (for example
            2025-26).
          </p>
        </div>
      </section>

      <section className="overflow-x-auto rounded-2xl border border-border bg-card shadow-sm">
        {profileQuery.isLoading && !profile ? (
          <div className="flex items-center justify-center gap-2 p-12 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading sales profile…
          </div>
        ) : profileQuery.isError ? (
          <p className="p-6 text-sm text-destructive">
            {profileQuery.error instanceof Error ? profileQuery.error.message : "Failed to load sales profile"}
          </p>
        ) : (
          <table className="w-full min-w-[32rem] border-collapse text-sm">
            <caption className="border-b border-border px-4 py-3 text-left text-sm font-semibold">
              Profile of Sales (Details may be provided on approximate basis)
            </caption>
            <thead>
              <tr className="bg-[#0B3A5B] text-white">
                <th rowSpan={2} className="border border-[#083049] px-3 py-2 font-semibold">
                  Sr. No.
                </th>
                <th rowSpan={2} className="border border-[#083049] px-3 py-2 text-left font-semibold">
                  Revenue Streams
                </th>
                <th colSpan={2} className="border border-[#083049] px-3 py-2 text-center font-semibold">
                  {profile?.periodLabel ?? "—"}
                </th>
              </tr>
              <tr className="bg-[#1565A8] text-white">
                <th className="border border-[#0B4F86] px-3 py-2 font-medium">Amt (INR Cr)</th>
                <th className="border border-[#0B4F86] px-3 py-2 font-medium">% Share</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.name} className="odd:bg-background even:bg-muted/30">
                  <td className="border border-border px-3 py-2 text-center">{row.sr}</td>
                  <td className="border border-border px-3 py-2">{row.name}</td>
                  <td className="border border-border px-3 py-2 text-right tabular-nums">{formatMn(row.amount)}</td>
                  <td className="border border-border px-3 py-2 text-right tabular-nums">{formatMn(row.share)}%</td>
                </tr>
              ))}
              <tr className="bg-amber-200/80 font-semibold text-slate-900">
                <td className="border border-border px-3 py-2" />
                <td className="border border-border px-3 py-2">Total</td>
                <td className="border border-border px-3 py-2 text-right tabular-nums">
                  {formatMn(profile?.totalAmountCr ?? 0)}
                </td>
                <td className="border border-border px-3 py-2 text-right tabular-nums">
                  {profile && profile.totalAmount > 0 ? "100.00%" : "0.00%"}
                </td>
              </tr>
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
