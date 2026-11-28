import { useMemo, useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useAuth } from "@/lib/auth-context";
import {
  formatPendingAmount,
  getPendingPaymentCompanies,
  getPendingPayments,
  punchPendingPayments,
  type PendingPaymentRow,
} from "@/lib/pending-payment-api";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/_app/pending-payment")({
  head: () => ({ meta: [{ title: "Next Week Pending Payment — PO Portal" }] }),
  component: PendingPaymentPage,
});

type ColumnKey =
  | "companyName"
  | "partyName"
  | "billNo"
  | "billDate"
  | "mrNo"
  | "mrnDt"
  | "year"
  | "billAmount"
  | "paymentTerms"
  | "dueDt"
  | "deptt";

const FILTER_COLUMNS: { key: ColumnKey; label: string; align?: "right" }[] = [
  { key: "companyName", label: "CompanyName" },
  { key: "partyName", label: "PartyName" },
  { key: "billNo", label: "BillNo" },
  { key: "billDate", label: "BillDate" },
  { key: "mrNo", label: "MRNo" },
  { key: "mrnDt", label: "MRNDt" },
  { key: "year", label: "Year" },
  { key: "billAmount", label: "BillAmount", align: "right" },
  { key: "paymentTerms", label: "PaymentTerms" },
  { key: "dueDt", label: "DueDt" },
  { key: "deptt", label: "Department" },
];

/** null = no filter (all values). Set = only these values are visible. */
type ColumnFilters = Partial<Record<ColumnKey, Set<string> | null>>;

function rowKey(row: PendingPaymentRow) {
  return `${row.companyName}|${row.partyName}|${row.billNo}|${row.mrNo}|${row.year}`;
}

function cellText(row: PendingPaymentRow, key: ColumnKey): string {
  const value = row[key];
  if (value == null || value === "") return "(Blank)";
  if (key === "billAmount") return formatPendingAmount(Number(value));
  return String(value);
}

function passesFilters(
  row: PendingPaymentRow,
  filters: ColumnFilters,
  except?: ColumnKey,
): boolean {
  return FILTER_COLUMNS.every(({ key }) => {
    if (key === except) return true;
    const selected = filters[key];
    if (!selected) return true;
    return selected.has(cellText(row, key));
  });
}

function ExcelColumnFilter({
  columnKey,
  label,
  align,
  items,
  filters,
  onChange,
}: {
  columnKey: ColumnKey;
  label: string;
  align?: "right";
  items: PendingPaymentRow[];
  filters: ColumnFilters;
  onChange: (key: ColumnKey, next: Set<string> | null) => void;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const active = !!filters[columnKey];

  const options = useMemo(() => {
    const values = new Set<string>();
    for (const row of items) {
      if (!passesFilters(row, filters, columnKey)) continue;
      values.add(cellText(row, columnKey));
    }
    return Array.from(values).sort((a, b) =>
      a.localeCompare(b, undefined, { numeric: true, sensitivity: "base" }),
    );
  }, [items, filters, columnKey]);

  const selected = filters[columnKey] ?? null;
  const visibleOptions = search.trim()
    ? options.filter((v) => v.toLowerCase().includes(search.trim().toLowerCase()))
    : options;

  const allChecked = !selected || options.every((v) => selected.has(v));

  function toggleValue(value: string, checked: boolean) {
    const base = selected ? new Set(selected) : new Set(options);
    if (checked) base.add(value);
    else base.delete(value);
    if (base.size === 0) {
      onChange(columnKey, new Set());
      return;
    }
    if (base.size === options.length) onChange(columnKey, null);
    else onChange(columnKey, base);
  }

  function toggleAll(checked: boolean) {
    if (checked) onChange(columnKey, null);
    else onChange(columnKey, new Set());
  }

  function clearFilter() {
    onChange(columnKey, null);
    setSearch("");
    setOpen(false);
  }

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setSearch("");
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "inline-flex max-w-full items-center gap-1 rounded px-1 py-0.5 text-left text-xs font-medium hover:bg-muted",
            align === "right" && "ml-auto flex-row-reverse",
            active && "bg-primary/10 text-primary",
          )}
          aria-label={`Filter ${label}`}
        >
          <span className="truncate">{label}</span>
          <ChevronDown className={cn("h-3.5 w-3.5 shrink-0", active && "text-primary")} />
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-2" onClick={(e) => e.stopPropagation()}>
        <Input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search…"
          className="mb-2 h-8"
        />
        <div className="mb-2 flex items-center justify-between gap-2 border-b border-border pb-2">
          <label className="flex cursor-pointer items-center gap-2 text-sm">
            <Checkbox
              checked={allChecked}
              onCheckedChange={(v) => toggleAll(v === true)}
              aria-label="Select all"
            />
            (Select All)
          </label>
          {active && (
            <button
              type="button"
              className="text-xs text-primary hover:underline"
              onClick={clearFilter}
            >
              Clear
            </button>
          )}
        </div>
        <div className="max-h-56 space-y-1 overflow-auto">
          {visibleOptions.length === 0 ? (
            <p className="px-1 py-2 text-xs text-muted-foreground">No values</p>
          ) : (
            visibleOptions.map((value) => {
              const checked = !selected || selected.has(value);
              return (
                <label
                  key={value}
                  className="flex cursor-pointer items-start gap-2 rounded px-1 py-1 text-sm hover:bg-muted/60"
                >
                  <Checkbox
                    checked={checked}
                    onCheckedChange={(v) => toggleValue(value, v === true)}
                    className="mt-0.5"
                    aria-label={value}
                  />
                  <span className="break-all leading-snug">{value}</span>
                </label>
              );
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

function PendingPaymentPage() {
  const { user } = useAuth();
  const [company, setCompany] = useState("All Companies");
  const [refreshToken, setRefreshToken] = useState(0);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [punching, setPunching] = useState(false);
  const [filters, setFilters] = useState<ColumnFilters>({});

  const companiesQuery = useQuery({
    queryKey: ["pending-payment-companies"],
    queryFn: getPendingPaymentCompanies,
    staleTime: 60 * 60_000,
  });

  const listQuery = useQuery({
    queryKey: ["pending-payment", company, refreshToken],
    queryFn: () =>
      getPendingPayments({
        company,
        refresh: refreshToken > 0,
      }),
    staleTime: 5 * 60_000,
  });

  const items = listQuery.data?.items ?? [];

  const filteredItems = useMemo(
    () => items.filter((row) => passesFilters(row, filters)),
    [items, filters],
  );

  const selectedRows = useMemo(
    () => filteredItems.filter((row) => selected.has(rowKey(row))),
    [filteredItems, selected],
  );

  const selectedCount = selectedRows.length;
  const selectedSum = selectedRows.reduce((s, r) => s + r.billAmount, 0);
  const allSelected =
    filteredItems.length > 0 && selectedCount === filteredItems.length;

  function setColumnFilter(key: ColumnKey, next: Set<string> | null) {
    setFilters((prev) => {
      const copy = { ...prev };
      if (!next) delete copy[key];
      else copy[key] = next;
      return copy;
    });
    setSelected(new Set());
  }

  function toggleAll() {
    if (allSelected) setSelected(new Set());
    else setSelected(new Set(filteredItems.map(rowKey)));
  }

  function toggleOne(key: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  function handleRefresh() {
    setSelected(new Set());
    setFilters({});
    setRefreshToken((n) => n + 1);
  }

  async function handlePunch() {
    if (selectedRows.length === 0) {
      toast.error("Select at least one row");
      return;
    }
    const loginName = user?.username?.trim();
    if (!loginName) {
      toast.error("Login required to punch payment request");
      return;
    }

    setPunching(true);
    try {
      const result = await punchPendingPayments({
        loginName,
        lines: selectedRows.map((r) => ({
          companyName: r.companyName,
          partyName: r.partyName,
          billNo: r.billNo,
          billDate: r.billDate,
          mrNo: r.mrNo,
          mrnDt: r.mrnDt,
          year: r.year,
          billAmount: r.billAmount,
          paymentTerms: r.paymentTerms,
          deptt: r.deptt,
        })),
      });

      if (result.createdCount > 0) {
        toast.success(
          result.message ||
            `Punched ${result.createdCount} payment request(s): ${result.created
              .map((c) => c.paymentNo)
              .join(", ")}`,
        );
      }
      if (result.errors.length > 0) {
        toast.error(result.errors.slice(0, 3).join("; "));
      }
      setSelected(new Set());
      setRefreshToken((n) => n + 1);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Punch payment request failed");
    } finally {
      setPunching(false);
    }
  }

  const loading = listQuery.isFetching && !listQuery.data;

  return (
    <div className="space-y-4 pb-2">
      <div className="card-3d rounded-2xl p-4 sm:p-5">
        <h1 className="text-xl font-semibold tracking-tight sm:text-2xl">Next Week</h1>
        <p className="mt-1 text-xs text-muted-foreground sm:text-sm">
          MM → Display → Other → Next Week Pending Payment · ERP.PendingPayment_n
        </p>
      </div>

      <section className="card-3d rounded-2xl p-3 sm:p-4" aria-label="Company filter">
        <div className="max-w-md space-y-1.5">
          <Label htmlFor="pp-company">Company</Label>
          <select
            id="pp-company"
            value={company}
            onChange={(e) => {
              setCompany(e.target.value);
              setSelected(new Set());
              setFilters({});
            }}
            className="flex h-11 w-full rounded-md border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            disabled={companiesQuery.isLoading}
          >
            <option value="All Companies">All Companies</option>
            {(companiesQuery.data ?? []).map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
      </section>

      <section className="rounded-2xl border border-border bg-card shadow-soft">
        <div className="p-2 sm:p-3">
          {loading ? (
            <div className="flex h-[200px] items-center justify-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading…
            </div>
          ) : listQuery.isError ? (
            <p className="px-1 text-sm text-destructive" role="alert">
              {listQuery.error instanceof Error
                ? listQuery.error.message
                : "Failed to load pending payments"}
            </p>
          ) : items.length === 0 ? (
            <p className="px-1 py-8 text-center text-sm text-muted-foreground">
              No pending payment rows
            </p>
          ) : (
            <div className="max-h-[55vh] overflow-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-10">
                      <input
                        type="checkbox"
                        checked={allSelected}
                        onChange={toggleAll}
                        aria-label="Select all visible"
                        className="h-4 w-4"
                      />
                    </TableHead>
                    {FILTER_COLUMNS.map((col) => (
                      <TableHead
                        key={col.key}
                        className={cn(
                          "whitespace-nowrap",
                          col.align === "right" && "text-right",
                        )}
                      >
                        <ExcelColumnFilter
                          columnKey={col.key}
                          label={col.label}
                          align={col.align}
                          items={items}
                          filters={filters}
                          onChange={setColumnFilter}
                        />
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredItems.length === 0 ? (
                    <TableRow>
                      <TableCell
                        colSpan={FILTER_COLUMNS.length + 1}
                        className="py-8 text-center text-sm text-muted-foreground"
                      >
                        No rows match the column filters
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredItems.map((row) => {
                      const key = rowKey(row);
                      const checked = selected.has(key);
                      return (
                        <TableRow
                          key={key}
                          className={cn(checked && "bg-accent/40")}
                          onClick={() => toggleOne(key)}
                        >
                          <TableCell onClick={(e) => e.stopPropagation()}>
                            <input
                              type="checkbox"
                              checked={checked}
                              onChange={() => toggleOne(key)}
                              aria-label={`Select ${row.billNo}`}
                              className="h-4 w-4"
                            />
                          </TableCell>
                          <TableCell className="max-w-[11rem]">{row.companyName}</TableCell>
                          <TableCell className="max-w-[11rem]">{row.partyName}</TableCell>
                          <TableCell className="font-medium tabular-nums">{row.billNo}</TableCell>
                          <TableCell className="tabular-nums text-muted-foreground">
                            {row.billDate ?? "—"}
                          </TableCell>
                          <TableCell className="tabular-nums">{row.mrNo}</TableCell>
                          <TableCell className="tabular-nums text-muted-foreground">
                            {row.mrnDt ?? "—"}
                          </TableCell>
                          <TableCell className="tabular-nums">{row.year}</TableCell>
                          <TableCell className="text-right tabular-nums font-medium">
                            {formatPendingAmount(row.billAmount)}
                          </TableCell>
                          <TableCell className="max-w-[10rem] text-muted-foreground">
                            {row.paymentTerms}
                          </TableCell>
                          <TableCell className="tabular-nums">{row.dueDt ?? "—"}</TableCell>
                          <TableCell>{row.deptt}</TableCell>
                        </TableRow>
                      );
                    })
                  )}
                </TableBody>
              </Table>
            </div>
          )}
        </div>

        <div className="flex flex-col items-center gap-3 border-t border-border px-3 py-4">
          <div className="flex flex-wrap items-center justify-center gap-3">
            <Button
              type="button"
              className="h-11 min-w-[7rem] touch-manipulation gap-1.5"
              disabled={listQuery.isFetching || punching}
              onClick={handleRefresh}
            >
              <RefreshCw className={cn("h-4 w-4", listQuery.isFetching && "animate-spin")} />
              Refresh
            </Button>
            <Button
              type="button"
              className="h-11 min-w-[12rem] touch-manipulation"
              disabled={punching || selectedCount === 0 || listQuery.isFetching}
              onClick={() => void handlePunch()}
            >
              {punching ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Punching…
                </>
              ) : (
                "Punch Payment Request"
              )}
            </Button>
          </div>
          <p className="text-sm tabular-nums text-muted-foreground">
            {listQuery.isFetching ? (
              <span className="mr-3 inline-flex items-center gap-1.5 text-foreground">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Refreshing from ERP (can take ~1 min)…
              </span>
            ) : null}
            Rows : {filteredItems.length}
            <span className="mx-3">·</span>
            Selected : {selectedCount}
            <span className="mx-3">·</span>
            Sum : {formatPendingAmount(selectedSum)}
          </p>
        </div>
      </section>
    </div>
  );
}
