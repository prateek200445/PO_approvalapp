import { createFileRoute, Link, Outlet, useRouterState } from "@tanstack/react-router";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Download,
  Loader2,
  RefreshCw,
  Search,
  Upload,
  Globe2,
  Mail,
  Phone,
  FileSpreadsheet,
  ShieldCheck,
  Users,
  Package,
  Flame,
  MapPinned,
  Building2,
  History,
  LayoutDashboard,
  ExternalLink,
  type LucideIcon,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  fibcExportUrl,
  formatFibcDate,
  getFibcBuyers,
  getFibcCredentialStatus,
  getFibcDashboard,
  getFibcImportHistory,
  getFibcImportHistoryDetail,
  importFibcCsv,
  na,
  syncFibcExim,
  type FibcBuyer,
  type FibcImportHistoryItem,
  type FibcNamedValue,
} from "@/lib/fibc-buyers-api";

export const Route = createFileRoute("/_app/fibc-buyers")({
  head: () => ({ meta: [{ title: "FIBC Buyers — Ex-Im Trade Intelligence" }] }),
  component: FibcBuyersPage,
});

const SELECT =
  "h-10 w-full rounded-xl border border-border/70 bg-background px-3 text-sm outline-none transition focus:border-primary focus:ring-2 focus:ring-primary/20";

function FibcBuyersPage() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  // Nested detail route lives under /fibc-buyers/$buyerId — parent must yield to <Outlet />.
  if (pathname !== "/fibc-buyers" && pathname.startsWith("/fibc-buyers/")) {
    return <Outlet />;
  }
  return <FibcBuyersDashboard />;
}

function FibcBuyersDashboard() {
  const queryClient = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const [keyword, setKeyword] = useState("");
  const [hsCode, setHsCode] = useState("630532");
  const [country, setCountry] = useState("All");
  const [buyer, setBuyer] = useState("");
  const [dateRange, setDateRange] = useState("All");
  const [tradeDirection, setTradeDirection] = useState("Both");
  const [minScore, setMinScore] = useState<number | undefined>(undefined);
  const [genuineOnly, setGenuineOnly] = useState(true);
  const [page, setPage] = useState(1);
  const [busy, setBusy] = useState<"sync" | "import" | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const [mainTab, setMainTab] = useState<"dashboard" | "history">("dashboard");
  const [selectedHistoryId, setSelectedHistoryId] = useState<string | null>(null);

  const filters = useMemo(
    () => ({
      country: country === "All" ? undefined : country,
      buyer: buyer.trim() || undefined,
      keyword: keyword.trim() || undefined,
      minScore,
      genuineOnly: genuineOnly ? true : undefined,
    }),
    [country, buyer, keyword, minScore, genuineOnly],
  );

  const dashQuery = useQuery({
    queryKey: ["fibc-dashboard", filters],
    queryFn: () => getFibcDashboard(filters),
    staleTime: 30_000,
  });

  const buyersQuery = useQuery({
    queryKey: ["fibc-buyers", filters, page],
    queryFn: () => getFibcBuyers({ ...filters, page, pageSize: 10 }),
    staleTime: 30_000,
  });

  const credQuery = useQuery({
    queryKey: ["fibc-cred-status"],
    queryFn: getFibcCredentialStatus,
    staleTime: 60_000,
  });

  const historyQuery = useQuery({
    queryKey: ["fibc-history"],
    queryFn: getFibcImportHistory,
    staleTime: 30_000,
    enabled: mainTab === "history",
  });

  const historyDetailQuery = useQuery({
    queryKey: ["fibc-history-detail", selectedHistoryId],
    queryFn: () => getFibcImportHistoryDetail(selectedHistoryId!),
    enabled: mainTab === "history" && !!selectedHistoryId,
    staleTime: 30_000,
  });

  const dash = dashQuery.data;
  const buyers = buyersQuery.data;
  const historyItems = historyQuery.data?.items ?? [];

  useEffect(() => {
    if (mainTab !== "history") return;
    if (selectedHistoryId) return;
    const first = historyQuery.data?.items?.[0]?.id;
    if (first) setSelectedHistoryId(first);
  }, [mainTab, selectedHistoryId, historyQuery.data?.items]);

  async function onSync() {
    setBusy("sync");
    try {
      const result = await syncFibcExim({
        limit: 20,
        keywords: keyword ? [keyword] : ["FIBC", "Jumbo Bags"],
        hsCodes: hsCode ? [hsCode] : ["630532"],
        country: country === "All" ? undefined : country,
        dateRange,
        tradeDirection,
      });
      if (result.success) {
        toast.success("Ex-Im sync completed");
        await queryClient.invalidateQueries({ queryKey: ["fibc-dashboard"] });
        await queryClient.invalidateQueries({ queryKey: ["fibc-buyers"] });
      } else {
        toast.message(result.message || "Sync blocked", {
          description: "Upload an Ex-Im Excel/CSV export instead.",
          duration: 7000,
        });
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Sync failed");
    } finally {
      setBusy(null);
    }
  }

  async function onImport(file: File) {
    setBusy("import");
    try {
      const result = await importFibcCsv(file);
      const ships = result.newShipments ?? 0;
      const total = result.totalShipments ?? 0;
      const buyerCount = result.totalBuyers ?? 0;
      if (ships > 0) {
        toast.success(
          `Imported ${ships.toLocaleString("en-IN")} shipment(s) · ${buyerCount.toLocaleString("en-IN")} buyers`,
        );
      } else if (total > 0) {
        toast.message("Sheet already loaded", {
          description: `${total.toLocaleString("en-IN")} shipments · ${buyerCount.toLocaleString("en-IN")} buyers in store`,
        });
      } else {
        toast.error("No buyers found in this file.");
      }
      setPage(1);
      await queryClient.invalidateQueries({ queryKey: ["fibc-dashboard"] });
      await queryClient.invalidateQueries({ queryKey: ["fibc-buyers"] });
      await queryClient.invalidateQueries({ queryKey: ["fibc-history"] });
      if (result.importId) {
        setSelectedHistoryId(String(result.importId));
        setMainTab("history");
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Import failed");
    } finally {
      setBusy(null);
      if (fileRef.current) fileRef.current.value = "";
    }
  }

  function acceptFile(file: File | undefined | null) {
    if (!file) return;
    const name = file.name.toLowerCase();
    if (
      !name.endsWith(".xlsx") &&
      !name.endsWith(".xlsm") &&
      !name.endsWith(".csv") &&
      !name.endsWith(".txt")
    ) {
      toast.error("Upload an Ex-Im .xlsx or .csv file");
      return;
    }
    void onImport(file);
  }

  function runSearch() {
    setPage(1);
    void dashQuery.refetch();
    void buyersQuery.refetch();
  }

  const kpis: { label: string; value: number; icon: LucideIcon; tone: string; bar: string }[] = [
    {
      label: "Buyers",
      value: dash?.kpis.totalBuyers ?? 0,
      icon: Users,
      tone: "bg-primary/15 text-primary border-primary/25",
      bar: "bg-primary",
    },
    {
      label: "Shipments",
      value: dash?.kpis.totalShipments ?? 0,
      icon: Package,
      tone: "bg-sky-500/15 text-sky-700 border-sky-500/25 dark:text-sky-300",
      bar: "bg-sky-500",
    },
    {
      label: "Genuine",
      value: dash?.kpis.genuineBuyers ?? 0,
      icon: ShieldCheck,
      tone: "bg-emerald-500/15 text-emerald-700 border-emerald-500/25 dark:text-emerald-300",
      bar: "bg-emerald-500",
    },
    {
      label: "Hot",
      value: dash?.kpis.hotBuyers ?? 0,
      icon: Flame,
      tone: "bg-amber-500/15 text-amber-800 border-amber-500/25 dark:text-amber-300",
      bar: "bg-amber-500",
    },
    {
      label: "Countries",
      value: dash?.kpis.countries ?? 0,
      icon: MapPinned,
      tone: "bg-teal-500/15 text-teal-700 border-teal-500/25 dark:text-teal-300",
      bar: "bg-teal-500",
    },
    {
      label: "Suppliers",
      value: dash?.kpis.suppliers ?? 0,
      icon: Building2,
      tone: "bg-violet-500/15 text-violet-700 border-violet-500/25 dark:text-violet-300",
      bar: "bg-violet-500",
    },
  ];

  return (
    <div className="space-y-5 pb-8">
      {/* Hero */}
      <div className="card-3d relative overflow-hidden rounded-2xl">
        <div className="absolute inset-0 bg-gradient-to-br from-primary via-primary to-[#1a326f]" />
        <div className="absolute -right-16 -top-16 h-56 w-56 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -bottom-20 left-1/3 h-48 w-48 rounded-full bg-sky-300/10 blur-3xl" />

        <div className="relative p-5 text-primary-foreground sm:p-6">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
            <div className="max-w-xl">
              <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-white/70">
                Reports · Ex-Im
              </p>
              <h1 className="mt-1.5 text-2xl font-semibold tracking-tight sm:text-3xl">
                FIBC Buyer Dashboard
              </h1>
              <p className="mt-2 text-sm leading-relaxed text-white/85">
                Upload an Ex-Im trade sheet to score FIBC buyers and surface genuine leads from real
                shipment data.
              </p>
              <div className="mt-4 flex flex-wrap gap-2 text-[11px] text-white/80">
                <span className="rounded-full bg-white/15 px-2.5 py-1 backdrop-blur-sm">
                  {(dash?.kpis.totalBuyers ?? 0).toLocaleString("en-IN")} buyers
                </span>
                <span className="rounded-full bg-white/15 px-2.5 py-1 backdrop-blur-sm">
                  {(dash?.kpis.totalShipments ?? 0).toLocaleString("en-IN")} shipments
                </span>
                <span className="rounded-full bg-emerald-400/25 px-2.5 py-1 text-emerald-50 backdrop-blur-sm">
                  {(dash?.kpis.genuineBuyers ?? 0).toLocaleString("en-IN")} genuine
                </span>
                {credQuery.data?.passwordConfigured ? (
                  <span className="rounded-full bg-white/15 px-2.5 py-1 backdrop-blur-sm">
                    Sync ready
                  </span>
                ) : null}
              </div>
            </div>

            <div className="flex flex-wrap gap-2 lg:justify-end">
              <Button
                type="button"
                className="h-10 border-0 bg-white text-primary shadow-sm hover:bg-white/95"
                disabled={busy !== null}
                onClick={() => fileRef.current?.click()}
              >
                {busy === "import" ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Upload className="h-4 w-4" />
                )}
                Upload sheet
              </Button>
              <Button
                type="button"
                variant="outline"
                className="h-10 border-white/35 bg-white/10 text-white hover:bg-white/15 hover:text-white"
                disabled={busy !== null}
                onClick={() => void onSync()}
              >
                {busy === "sync" ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )}
                Sync
              </Button>
              <Button
                type="button"
                variant="outline"
                className="h-10 border-white/35 bg-white/10 text-white hover:bg-white/15 hover:text-white"
                asChild
              >
                <a href={fibcExportUrl(filters)}>
                  <Download className="h-4 w-4" />
                  Export
                </a>
              </Button>
            </div>
          </div>

          <button
            type="button"
            className={cn(
              "mt-5 w-full rounded-2xl border border-dashed px-4 py-7 text-center transition",
              dragOver
                ? "border-white bg-white/20 scale-[1.01]"
                : "border-white/40 bg-white/8 hover:bg-white/12",
              busy === "import" && "pointer-events-none opacity-70",
            )}
            onClick={() => fileRef.current?.click()}
            onDragOver={(e) => {
              e.preventDefault();
              setDragOver(true);
            }}
            onDragLeave={() => setDragOver(false)}
            onDrop={(e) => {
              e.preventDefault();
              setDragOver(false);
              acceptFile(e.dataTransfer.files?.[0]);
            }}
          >
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-white/15">
              <FileSpreadsheet className="h-6 w-6 text-white" />
            </div>
            <p className="mt-3 text-sm font-semibold">Drop Ex-Im Excel / CSV here</p>
            <p className="mt-1 text-xs text-white/70">
              or click to browse · HS {hsCode || "630532"} FIBC trade reports
            </p>
          </button>
          <input
            ref={fileRef}
            type="file"
            accept=".csv,.txt,.xlsx,.xlsm,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            className="hidden"
            onChange={(e) => acceptFile(e.target.files?.[0])}
          />
        </div>
      </div>

      {/* Section tabs */}
      <div className="flex gap-1 rounded-2xl border border-border/70 bg-secondary/40 p-1">
        <button
          type="button"
          className={cn(
            "inline-flex flex-1 items-center justify-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition sm:flex-none sm:px-5",
            mainTab === "dashboard"
              ? "bg-background text-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
          onClick={() => setMainTab("dashboard")}
        >
          <LayoutDashboard className="h-4 w-4" />
          Dashboard
        </button>
        <button
          type="button"
          className={cn(
            "inline-flex flex-1 items-center justify-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition sm:flex-none sm:px-5",
            mainTab === "history"
              ? "bg-background text-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
          onClick={() => setMainTab("history")}
        >
          <History className="h-4 w-4" />
          History
        </button>
      </div>

      {mainTab === "history" ? (
        <HistoryPanel
          items={historyItems}
          loading={historyQuery.isLoading}
          selectedId={selectedHistoryId}
          onSelect={setSelectedHistoryId}
          detail={historyDetailQuery.data}
          detailLoading={historyDetailQuery.isLoading}
        />
      ) : (
        <>
      {/* KPIs */}
      <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-6 sm:gap-3">
        {kpis.map((k) => (
          <div key={k.label} className="card-3d relative overflow-hidden rounded-2xl p-3 sm:p-3.5">
            <div className={cn("absolute inset-x-0 top-0 h-1", k.bar)} />
            <div
              className={cn(
                "icon-3d mb-2 inline-flex h-8 w-8 items-center justify-center rounded-xl border",
                k.tone,
              )}
            >
              <k.icon className="h-3.5 w-3.5" aria-hidden />
            </div>
            <div className="text-[11px] text-muted-foreground">{k.label}</div>
            <div
              className={cn(
                "mt-0.5 text-lg font-semibold tabular-nums sm:text-xl",
                dashQuery.isLoading && "animate-pulse text-muted-foreground",
              )}
            >
              {dashQuery.isLoading ? "…" : k.value.toLocaleString("en-IN")}
            </div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <section className="card-3d rounded-2xl p-4 sm:p-5" aria-label="Search filters">
        <div className="mb-3 flex items-center justify-between gap-2">
          <h2 className="text-sm font-semibold">Search & filters</h2>
          <label className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1.5 text-xs font-medium text-emerald-800 dark:text-emerald-300">
            <input
              type="checkbox"
              className="size-3.5 accent-emerald-600"
              checked={genuineOnly}
              onChange={(e) => {
                setGenuineOnly(e.target.checked);
                setPage(1);
              }}
            />
            Genuine only
          </label>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <Field label="Product / Keyword">
            <Input className="h-10 rounded-xl" value={keyword} onChange={(e) => setKeyword(e.target.value)} />
          </Field>
          <Field label="HS Code">
            <Input className="h-10 rounded-xl" value={hsCode} onChange={(e) => setHsCode(e.target.value)} />
          </Field>
          <Field label="Country">
            <select className={SELECT} value={country} onChange={(e) => setCountry(e.target.value)}>
              <option>All</option>
              {(dash?.topCountries ?? []).map((c) => (
                <option key={c.name}>{c.name}</option>
              ))}
            </select>
          </Field>
          <Field label="Time Period">
            <select className={SELECT} value={dateRange} onChange={(e) => setDateRange(e.target.value)}>
              {["All", "Last 30 days", "Last 3 months", "Last 6 months", "Last 12 Months", "Last 2 years"].map(
                (x) => (
                  <option key={x}>{x}</option>
                ),
              )}
            </select>
          </Field>
          <Field label="Trade Direction">
            <select
              className={SELECT}
              value={tradeDirection}
              onChange={(e) => setTradeDirection(e.target.value)}
            >
              {["Both", "Import", "Export"].map((x) => (
                <option key={x}>{x}</option>
              ))}
            </select>
          </Field>
          <Field label="Buyer">
            <Input className="h-10 rounded-xl" value={buyer} onChange={(e) => setBuyer(e.target.value)} />
          </Field>
        </div>
        <div className="mt-3 flex flex-wrap items-end gap-3">
          <Field label="Min Score">
            <select
              className={cn(SELECT, "w-28")}
              value={minScore ?? ""}
              onChange={(e) => setMinScore(e.target.value ? Number(e.target.value) : undefined)}
            >
              <option value="">Any</option>
              <option value="60">60+</option>
              <option value="75">75+</option>
              <option value="90">90+</option>
            </select>
          </Field>
          <Button type="button" className="h-10 rounded-xl px-5" onClick={runSearch}>
            <Search className="h-4 w-4" />
            Search
          </Button>
        </div>
      </section>

      {/* Genuine rules */}
      <section className="card-3d rounded-2xl p-4 sm:p-5">
        <div className="flex items-start gap-3">
          <div className="icon-3d inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-emerald-500/25 bg-emerald-500/15 text-emerald-700 dark:text-emerald-300">
            <ShieldCheck className="h-5 w-5" />
          </div>
          <div className="min-w-0 flex-1">
            <h2 className="text-sm font-semibold">How genuine buyers are decided</h2>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Qualification uses only imported Ex-Im shipment fields — contacts are never invented.
            </p>
            <ol className="mt-3 grid list-decimal gap-2 pl-4 text-xs text-muted-foreground sm:grid-cols-2 lg:grid-cols-3">
              <li>Real company name (not N/A)</li>
              <li>Country present</li>
              <li>HS 630532 or FIBC product match</li>
              <li>City, address, or destination port</li>
              <li>Buyer score ≥ 60</li>
            </ol>
          </div>
        </div>
      </section>

      {/* Ex-Im style trade intelligence by USD value */}
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        <ValueRankCard
          title="Top Buyer Countries"
          items={dash?.topBuyerCountriesByValue ?? []}
          empty="Upload Ex-Im data to see buyer countries."
        />
        <ValueRankCard
          title="Top Seller Countries"
          items={dash?.topSellerCountriesByValue ?? []}
          empty="Upload Ex-Im data to see seller countries."
        />
        <ValueRankCard
          title="Top Buyers"
          items={dash?.topBuyersByValue ?? []}
          empty="Upload Ex-Im data to see top buyers."
        />
        <ValueRankCard
          title="Top Sellers"
          items={dash?.topSellersByValue ?? []}
          empty="Upload Ex-Im data to see top sellers."
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        <section className="card-3d overflow-hidden rounded-2xl">
          <header className="border-b border-border/70 px-4 py-3">
            <h2 className="text-sm font-semibold">Top Countries by Shipments</h2>
          </header>
          <div className="space-y-2.5 p-4">
            {(dash?.topCountries?.length ?? 0) === 0 ? (
              <p className="text-sm text-muted-foreground">Upload an Ex-Im sheet to see countries.</p>
            ) : (
              dash!.topCountries.map((c) => {
                const max = Math.max(...dash!.topCountries.map((x) => x.count), 1);
                return (
                  <div key={c.name} className="flex items-center gap-3">
                    <div className="w-28 truncate text-xs font-medium">{c.name}</div>
                    <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-secondary">
                      <div
                        className="h-2.5 rounded-full bg-primary transition-all"
                        style={{ width: `${(c.count / max) * 100}%` }}
                      />
                    </div>
                    <div className="w-10 text-right text-xs font-medium tabular-nums">{c.count}</div>
                  </div>
                );
              })
            )}
          </div>
        </section>

        <section className="card-3d overflow-hidden rounded-2xl">
          <header className="border-b border-border/70 px-4 py-3">
            <h2 className="text-sm font-semibold">Buyer Score Distribution</h2>
          </header>
          <div className="space-y-2 p-4">
            {(dash?.scoreDistribution ?? []).map((c) => {
              const max = Math.max(...(dash?.scoreDistribution ?? []).map((x) => x.count), 1);
              return (
                <div key={c.name} className="flex items-center gap-3 text-sm">
                  <span className="w-28 shrink-0 text-xs text-muted-foreground">{c.name}</span>
                  <div className="h-2 flex-1 overflow-hidden rounded-full bg-secondary">
                    <div
                      className="h-2 rounded-full bg-primary/70"
                      style={{ width: `${(c.count / max) * 100}%` }}
                    />
                  </div>
                  <span className="w-8 text-right text-xs font-semibold tabular-nums">{c.count}</span>
                </div>
              );
            })}
          </div>
        </section>
      </div>

      {/* High potential */}
      {(dash?.highPotential?.length ?? 0) > 0 && (
        <section className="card-3d overflow-hidden rounded-2xl">
          <header className="border-b border-border/70 px-4 py-3">
            <h2 className="text-sm font-semibold">High Potential Buyers</h2>
          </header>
          <div className="grid grid-cols-1 gap-2.5 p-3 sm:grid-cols-2 lg:grid-cols-3 sm:p-4">
            {dash!.highPotential.slice(0, 6).map((b) => (
              <div
                key={b.id}
                className="rounded-xl border border-border/60 bg-background/80 p-3 shadow-sm transition hover:border-primary/30 hover:shadow-md"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <div className="truncate text-sm font-semibold">{b.companyName}</div>
                    <div className="mt-0.5 text-[11px] text-muted-foreground">{na(b.country)}</div>
                  </div>
                  <div className="flex flex-col items-end gap-1">
                    <ScorePill score={b.score} />
                    <StatusPill genuine={!!b.isGenuine} />
                  </div>
                </div>
                <div className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-muted-foreground">
                  <span className="inline-flex items-center gap-1">
                    <Mail className="h-3 w-3" /> {na(b.email)}
                  </span>
                  <span className="inline-flex items-center gap-1">
                    <Phone className="h-3 w-3" /> {na(b.phone)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Leads table */}
      <section className="card-3d overflow-hidden rounded-2xl">
        <header className="flex flex-col gap-2 border-b border-border/70 px-4 py-3.5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-sm font-semibold">FIBC Buyer Leads</h2>
            <p className="text-[11px] text-muted-foreground">
              {(buyers?.total ?? 0).toLocaleString("en-IN")} shown
              {genuineOnly ? " · genuine only" : ""}
            </p>
          </div>
          <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
            <Globe2 className="h-3.5 w-3.5" />
            Last sync: {dash?.lastSync?.at ? new Date(dash.lastSync.at).toLocaleString() : "Never"}
          </div>
        </header>

        <div className="space-y-2 p-3 sm:hidden">
          {(buyers?.items ?? []).length === 0 ? (
            <EmptyBuyers genuineOnly={genuineOnly} />
          ) : (
            buyers!.items.map((b, i) => (
              <MobileBuyerCard key={b.id} buyer={b} index={(page - 1) * 10 + i + 1} />
            ))
          )}
        </div>

        <div className="hidden overflow-x-auto sm:block">
          <table className="w-full min-w-[1080px] text-sm">
            <thead className="bg-secondary/50 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5 font-medium">#</th>
                <th className="px-3 py-2.5 font-medium">Company</th>
                <th className="px-3 py-2.5 font-medium">Country</th>
                <th className="px-3 py-2.5 font-medium">Status</th>
                <th className="px-3 py-2.5 font-medium">Email</th>
                <th className="px-3 py-2.5 font-medium">Phone</th>
                <th className="px-3 py-2.5 text-right font-medium">Shipments</th>
                <th className="px-3 py-2.5 font-medium">Last Shipment</th>
                <th className="px-3 py-2.5 font-medium">Score</th>
                <th className="px-3 py-2.5 font-medium">Trend</th>
                <th className="px-3 py-2.5 font-medium"> </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/70">
              {(buyers?.items ?? []).length === 0 ? (
                <tr>
                  <td colSpan={11} className="px-3 py-12 text-center text-muted-foreground">
                    <EmptyBuyers genuineOnly={genuineOnly} />
                  </td>
                </tr>
              ) : (
                buyers!.items.map((b, i) => (
                  <tr key={b.id} className="transition hover:bg-secondary/30">
                    <td className="px-3 py-2.5 tabular-nums text-muted-foreground">
                      {(page - 1) * 10 + i + 1}
                    </td>
                    <td className="px-3 py-2.5 font-medium">{b.companyName}</td>
                    <td className="px-3 py-2.5">{na(b.country)}</td>
                    <td className="px-3 py-2.5">
                      <StatusPill genuine={!!b.isGenuine} />
                    </td>
                    <td className="px-3 py-2.5 text-xs">{na(b.email)}</td>
                    <td className="px-3 py-2.5 text-xs">{na(b.phone)}</td>
                    <td className="px-3 py-2.5 text-right tabular-nums">{b.shipmentCount}</td>
                    <td className="px-3 py-2.5">{formatFibcDate(b.lastShipment)}</td>
                    <td className="px-3 py-2.5">
                      <ScorePill score={b.score} />
                    </td>
                    <td className="px-3 py-2.5 text-xs">{b.trend}</td>
                    <td className="px-3 py-2.5">
                      <Link
                        to="/fibc-buyers/$buyerId"
                        params={{ buyerId: b.id }}
                        className="inline-flex items-center gap-1 text-xs font-medium text-primary hover:underline"
                      >
                        View <ExternalLink className="h-3 w-3" />
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {(buyers?.total ?? 0) > 10 && (
          <div className="flex items-center justify-between border-t border-border/70 px-4 py-3 text-xs">
            <button
              type="button"
              className="rounded-lg border border-border px-3 py-1.5 transition hover:bg-secondary disabled:opacity-40"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Previous
            </button>
            <span className="text-muted-foreground">
              Page {page} · {(buyers?.total ?? 0).toLocaleString("en-IN")} buyers
            </span>
            <button
              type="button"
              className="rounded-lg border border-border px-3 py-1.5 transition hover:bg-secondary disabled:opacity-40"
              disabled={page * 10 >= (buyers?.total ?? 0)}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        )}
      </section>
        </>
      )}
    </div>
  );
}

function HistoryPanel({
  items,
  loading,
  selectedId,
  onSelect,
  detail,
  detailLoading,
}: {
  items: FibcImportHistoryItem[];
  loading: boolean;
  selectedId: string | null;
  onSelect: (id: string) => void;
  detail: Awaited<ReturnType<typeof getFibcImportHistoryDetail>> | undefined;
  detailLoading: boolean;
}) {
  return (
    <div className="grid grid-cols-1 gap-3 lg:grid-cols-5">
      <section className="card-3d overflow-hidden rounded-2xl lg:col-span-2">
        <header className="border-b border-border/70 px-4 py-3">
          <h2 className="text-sm font-semibold">Import history</h2>
          <p className="text-[11px] text-muted-foreground">
            Every Ex-Im file you upload is kept here so you can open it again.
          </p>
        </header>
        <div className="max-h-[32rem] divide-y divide-border/60 overflow-y-auto">
          {loading ? (
            <p className="px-4 py-8 text-center text-sm text-muted-foreground">Loading history…</p>
          ) : items.length === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-muted-foreground">
              No imports yet. Upload an Ex-Im sheet from the Dashboard tab.
            </p>
          ) : (
            items.map((h) => {
              const active = h.id === selectedId;
              return (
                <button
                  key={h.id}
                  type="button"
                  className={cn(
                    "w-full px-4 py-3 text-left transition hover:bg-secondary/40",
                    active && "bg-primary/8 border-l-2 border-l-primary",
                  )}
                  onClick={() => onSelect(h.id)}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <div className="truncate text-sm font-medium">
                        {h.sourceFile || "Imported file"}
                      </div>
                      <div className="mt-0.5 text-[11px] text-muted-foreground">
                        {h.at ? new Date(h.at).toLocaleString() : "—"}
                      </div>
                    </div>
                    <span className="shrink-0 rounded-full bg-secondary px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
                      {h.mode || "import"}
                    </span>
                  </div>
                  <div className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-muted-foreground">
                    <span>
                      {(h.newShipments || 0).toLocaleString("en-IN")} new shipments
                    </span>
                    {(h.totalBuyersAfter ?? 0) > 0 ? (
                      <span>{h.totalBuyersAfter!.toLocaleString("en-IN")} buyers after</span>
                    ) : null}
                  </div>
                </button>
              );
            })
          )}
        </div>
      </section>

      <section className="card-3d overflow-hidden rounded-2xl lg:col-span-3">
        <header className="border-b border-border/70 px-4 py-3">
          <h2 className="text-sm font-semibold">Import snapshot</h2>
          <p className="text-[11px] text-muted-foreground">
            Buyers and sample shipments from this import.
          </p>
        </header>
        {!selectedId ? (
          <p className="px-4 py-10 text-center text-sm text-muted-foreground">
            Select an import on the left.
          </p>
        ) : detailLoading ? (
          <p className="px-4 py-10 text-center text-sm text-muted-foreground">Loading snapshot…</p>
        ) : !detail ? (
          <p className="px-4 py-10 text-center text-sm text-muted-foreground">Could not load this import.</p>
        ) : (
          <div className="space-y-4 p-4">
            <div className="rounded-xl border border-border/60 bg-secondary/30 p-3">
              <div className="text-sm font-semibold">
                {detail.history.sourceFile || "Imported file"}
              </div>
              <p className="mt-1 text-xs text-muted-foreground">
                {detail.history.message ||
                  `Imported on ${detail.history.at ? new Date(detail.history.at).toLocaleString() : "—"}`}
              </p>
              {detail.approximate ? (
                <p className="mt-2 text-[11px] text-amber-700 dark:text-amber-300">
                  Exact batch tags were not stored for this older import — showing current store data.
                </p>
              ) : null}
              <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
                <MiniStat label="Rows scanned" value={detail.history.recordsProcessed} />
                <MiniStat label="New shipments" value={detail.history.newShipments} />
                <MiniStat label="Buyers in batch" value={detail.buyerCount} />
                <MiniStat label="Shipments in batch" value={detail.shipmentCount} />
              </div>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Top buyers from this import
              </h3>
              {detail.topBuyers.length === 0 ? (
                <p className="text-sm text-muted-foreground">No buyers linked to this import.</p>
              ) : (
                <div className="space-y-2">
                  {detail.topBuyers.slice(0, 12).map((b) => (
                    <div
                      key={b.id}
                      className="flex items-center justify-between gap-3 rounded-xl border border-border/60 px-3 py-2"
                    >
                      <div className="min-w-0">
                        <div className="truncate text-sm font-medium">{b.companyName}</div>
                        <div className="text-[11px] text-muted-foreground">
                          {na(b.country)} · {b.shipmentCount} shipments
                        </div>
                      </div>
                      <div className="flex shrink-0 flex-col items-end gap-1">
                        <ScorePill score={b.score} />
                        <StatusPill genuine={!!b.isGenuine} />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Sample shipments
              </h3>
              {detail.sampleShipments.length === 0 ? (
                <p className="text-sm text-muted-foreground">No shipments in this import.</p>
              ) : (
                <div className="overflow-x-auto rounded-xl border border-border/60">
                  <table className="w-full min-w-[920px] text-sm">
                    <thead className="bg-secondary/50 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
                      <tr>
                        <th className="px-3 py-2 font-medium">Date</th>
                        <th className="px-3 py-2 font-medium">Product</th>
                        <th className="px-3 py-2 font-medium">HS</th>
                        <th className="px-3 py-2 font-medium">Qty</th>
                        <th className="px-3 py-2 font-medium">Seller</th>
                        <th className="px-3 py-2 font-medium">Dest. Port</th>
                        <th className="px-3 py-2 text-right font-medium">Value</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/60">
                      {detail.sampleShipments.map((s) => (
                        <tr key={s.id}>
                          <td className="px-3 py-2 text-xs whitespace-nowrap">
                            {formatFibcDate(s.shipmentDate)}
                          </td>
                          <td className="max-w-[260px] px-3 py-2 text-xs" title={s.productDescription ?? undefined}>
                            <div className="line-clamp-2">{na(s.productDescription)}</div>
                            {s.industry ? (
                              <div className="mt-0.5 text-[10px] text-muted-foreground">{s.industry}</div>
                            ) : null}
                          </td>
                          <td className="px-3 py-2 text-xs">{na(s.hsCode)}</td>
                          <td className="px-3 py-2 text-xs tabular-nums whitespace-nowrap">
                            {s.quantity != null
                              ? `${s.quantity.toLocaleString("en-US")} ${s.quantityUnit ?? ""}`.trim()
                              : "—"}
                          </td>
                          <td className="max-w-[160px] truncate px-3 py-2 text-xs" title={s.supplierName ?? undefined}>
                            {na(s.supplierName)}
                          </td>
                          <td className="px-3 py-2 text-xs">{na(s.portOfDischarge)}</td>
                          <td className="px-3 py-2 text-right text-xs tabular-nums whitespace-nowrap">
                            {s.value != null
                              ? `${s.currency || "USD"} ${s.value.toLocaleString("en-US")}`
                              : "—"}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}

function MiniStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg bg-background/80 px-2.5 py-2">
      <div className="text-[10px] text-muted-foreground">{label}</div>
      <div className="text-sm font-semibold tabular-nums">{(value || 0).toLocaleString("en-IN")}</div>
    </div>
  );
}

function ValueRankCard({
  title,
  items,
  empty,
}: {
  title: string;
  items: FibcNamedValue[];
  empty: string;
}) {
  const max = Math.max(...items.map((x) => x.valueUsd), 1);
  return (
    <section className="card-3d overflow-hidden rounded-2xl">
      <header className="border-b border-border/70 px-4 py-3">
        <h2 className="text-sm font-semibold">{title}</h2>
        <p className="text-[11px] text-muted-foreground">By trade value (USD) · from imported Ex-Im sheet</p>
      </header>
      <div className="max-h-72 space-y-2.5 overflow-y-auto p-4">
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">{empty}</p>
        ) : (
          items.map((item) => (
            <div key={item.name} className="flex items-center gap-3">
              <div className="w-28 truncate text-xs font-medium sm:w-36" title={item.name}>
                {item.name}
              </div>
              <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-secondary">
                <div
                  className="h-2.5 rounded-full bg-primary transition-all"
                  style={{ width: `${(item.valueUsd / max) * 100}%` }}
                />
              </div>
              <div className="w-14 shrink-0 text-right text-xs font-semibold tabular-nums text-primary">
                {item.valueLabel}
              </div>
            </div>
          ))
        )}
      </div>
    </section>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-[11px] font-medium text-muted-foreground">{label}</Label>
      {children}
    </div>
  );
}

function EmptyBuyers({ genuineOnly }: { genuineOnly: boolean }) {
  return (
    <div className="mx-auto max-w-md py-2 text-sm">
      <p className="font-medium text-foreground">
        {genuineOnly ? "No genuine buyers match these filters." : "No buyers match these filters."}
      </p>
      <p className="mt-1 text-muted-foreground">
        {genuineOnly
          ? "Turn off “Genuine only” or upload a fuller Ex-Im sheet."
          : "Upload an Ex-Im Excel/CSV export to load buyers."}
      </p>
    </div>
  );
}

function StatusPill({ genuine }: { genuine: boolean }) {
  return (
    <span
      className={cn(
        "inline-flex rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
        genuine
          ? "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300"
          : "bg-muted text-muted-foreground",
      )}
    >
      {genuine ? "Genuine" : "Unverified"}
    </span>
  );
}

function ScorePill({ score }: { score: number }) {
  const tone =
    score >= 90
      ? "bg-emerald-500/20 text-emerald-700 dark:text-emerald-300"
      : score >= 75
        ? "bg-lime-500/20 text-lime-800 dark:text-lime-300"
        : score >= 60
          ? "bg-amber-500/20 text-amber-800 dark:text-amber-300"
          : "bg-muted text-muted-foreground";
  return (
    <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs font-semibold tabular-nums", tone)}>
      {score}
    </span>
  );
}

function MobileBuyerCard({ buyer, index }: { buyer: FibcBuyer; index: number }) {
  return (
    <div className="rounded-xl border border-border/70 bg-background px-3 py-2.5 shadow-sm">
      <div className="flex items-start justify-between gap-2">
        <div>
          <div className="text-[11px] text-muted-foreground">#{index}</div>
          <div className="text-sm font-semibold">{buyer.companyName}</div>
          <div className="mt-0.5 text-[11px] text-muted-foreground">{na(buyer.country)}</div>
        </div>
        <div className="flex flex-col items-end gap-1">
          <ScorePill score={buyer.score} />
          <StatusPill genuine={!!buyer.isGenuine} />
        </div>
      </div>
      <div className="mt-2 space-y-0.5 text-[11px] text-muted-foreground">
        <div className="inline-flex items-center gap-1">
          <Mail className="h-3 w-3" /> {na(buyer.email)}
        </div>
        <div className="inline-flex items-center gap-1">
          <Phone className="h-3 w-3" /> {na(buyer.phone)}
        </div>
        <div>
          Shipments {buyer.shipmentCount} · {formatFibcDate(buyer.lastShipment)}
        </div>
      </div>
      <Link
        to="/fibc-buyers/$buyerId"
        params={{ buyerId: buyer.id }}
        className="mt-2 inline-flex items-center gap-1 text-xs font-medium text-primary hover:underline"
      >
        View shipments <ExternalLink className="h-3 w-3" />
      </Link>
    </div>
  );
}
