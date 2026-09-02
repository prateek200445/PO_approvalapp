import { useMemo, useRef, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { AlertCircle, Building2, Download, FileText, Inbox, Loader2, RefreshCw, Scale } from "lucide-react";
import { toast } from "sonner";
import {
  downloadIntercompanyExcel,
  downloadIntercompanyPdf,
  formatAsOn,
  getIntercompanyDashboard,
} from "@/lib/intercompany-api";
import { IntercompanySubnav } from "@/components/intercompany/IntercompanySubnav";
import { SettlementGuide } from "@/components/intercompany/SettlementGuide";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/_app/intercompany/settlement")({
  head: () => ({ meta: [{ title: "How to settle — Intercompany — PO Portal" }] }),
  component: IntercompanySettlementPage,
});

const ALL_COMPANIES = "";

const SELECT_CLASS =
  "h-9 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50";

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function IntercompanySettlementPage() {
  const [asOfInput, setAsOfInput] = useState(todayIso);
  const [asOf, setAsOf] = useState(todayIso);
  const [selectedCompany, setSelectedCompany] = useState(ALL_COMPANIES);
  const [refreshToken, setRefreshToken] = useState(0);
  const [exporting, setExporting] = useState<"excel" | "pdf" | null>(null);
  const bypassCacheRef = useRef(false);

  const query = useQuery({
    queryKey: ["intercompany-balances", asOf, refreshToken],
    queryFn: async () => {
      const refresh = bypassCacheRef.current;
      bypassCacheRef.current = false;
      return getIntercompanyDashboard(asOf, refresh);
    },
    staleTime: 60 * 60_000,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });

  const report = query.data;
  const matrices = report?.matrices ?? [];
  const companyNames = useMemo(() => matrices.map((m) => m.company).filter(Boolean), [matrices]);
  const showAllCompanies = !selectedCompany || !companyNames.some((name) => name === selectedCompany);
  const activeCompany = showAllCompanies ? ALL_COMPANIES : selectedCompany;
  const matrix = showAllCompanies ? undefined : matrices.find((m) => m.company === activeCompany);

  async function exportFile(kind: "excel" | "pdf") {
    if (!report || exporting) return;
    setExporting(kind);
    try {
      if (kind === "excel") await downloadIntercompanyExcel(asOf);
      else await downloadIntercompanyPdf(asOf);
      toast.success(kind === "excel" ? "Excel downloaded" : "PDF downloaded");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Download failed");
    } finally {
      setExporting(null);
    }
  }

  return (
    <div className="space-y-5">
      <div>
        <div className="mt-1 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex items-start gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Scale className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">How to settle</h1>
              <p className="mt-1 text-sm text-muted-foreground">
                A separate Intercompany page that says who pays, who receives, and the payment
                steps.
              </p>
            </div>
          </div>
          <IntercompanySubnav />
        </div>
      </div>

      <div className="rounded-2xl border border-border bg-card p-3 shadow-soft sm:p-3.5">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[minmax(0,1.4fr)_11rem_auto] lg:items-end">
          {companyNames.length > 0 ? (
            <div className="min-w-0 space-y-1">
              <Label htmlFor="settle-company" className="text-xs text-muted-foreground">
                Company
              </Label>
              <select
                id="settle-company"
                value={activeCompany}
                onChange={(e) => setSelectedCompany(e.target.value)}
                className={SELECT_CLASS}
              >
                <option value={ALL_COMPANIES}>All companies</option>
                {companyNames.map((name) => (
                  <option key={name} value={name}>
                    {name}
                  </option>
                ))}
              </select>
            </div>
          ) : (
            <div className="min-w-0 space-y-1">
              <Label className="text-xs text-muted-foreground">Company</Label>
              <div className="flex h-9 items-center rounded-md border border-dashed border-border bg-muted/40 px-3 text-sm text-muted-foreground">
                Available after load
              </div>
            </div>
          )}
          <div className="space-y-1">
            <Label htmlFor="settle-asof" className="text-xs text-muted-foreground">
              As on
            </Label>
            <Input
              id="settle-asof"
              type="date"
              value={asOfInput}
              onChange={(e) => {
                setAsOfInput(e.target.value);
                setAsOf(e.target.value || todayIso());
              }}
              className="h-9 bg-background"
            />
          </div>
          <div className="flex flex-wrap items-end gap-2">
            <Button
              type="button"
              variant="outline"
              className="h-9 gap-1.5"
              disabled={query.isFetching}
              onClick={() => {
                bypassCacheRef.current = true;
                setRefreshToken((n) => n + 1);
              }}
            >
              <RefreshCw className={cn("h-4 w-4", query.isFetching && "animate-spin")} />
              Refresh
            </Button>
            <Button
              type="button"
              variant="outline"
              className="h-9 gap-1.5"
              disabled={!report || Boolean(exporting) || query.isFetching}
              onClick={() => void exportFile("excel")}
              aria-label="Download Excel"
            >
              {exporting === "excel" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
              Excel
            </Button>
            <Button
              type="button"
              variant="outline"
              className="h-9 gap-1.5"
              disabled={!report || Boolean(exporting) || query.isFetching}
              onClick={() => void exportFile("pdf")}
              aria-label="Download PDF"
            >
              {exporting === "pdf" ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileText className="h-4 w-4" />}
              PDF
            </Button>
          </div>
        </div>
      </div>

      {query.isError ? (
        <div className="rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-8 text-center text-sm text-destructive">
          <AlertCircle className="mx-auto mb-2 h-5 w-5" />
          {query.error instanceof Error ? query.error.message : "Failed to load balances."}
        </div>
      ) : null}

      {query.isFetching && !report ? (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          Loading settlement from live intercompany balances…
        </div>
      ) : null}

      {report && matrices.length === 0 && !query.isFetching ? (
        <div className="rounded-2xl border border-dashed border-border bg-card px-4 py-10 text-center text-sm text-muted-foreground">
          <Inbox className="mx-auto mb-2 h-5 w-5" />
          No intercompany outstanding as on {formatAsOn(report.asOf)}.
        </div>
      ) : null}

      {matrices.length > 0 ? (
        <SettlementGuide asOf={report?.asOf ?? asOf} matrices={matrices} selected={matrix} />
      ) : null}

      <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
        <Building2 className="h-3.5 w-3.5" />
        Uses the same ERP balances as the Intercompany Balances page.
      </p>
    </div>
  );
}
