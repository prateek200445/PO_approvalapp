import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, useEffect, useMemo, useRef } from "react";
import type { MouseEvent as ReactMouseEvent, TouchEvent as ReactTouchEvent } from "react";
import { ArrowUpDown, CheckSquare, Filter, Loader2, Search, X } from "lucide-react";
import { toast } from "sonner";
import { getApiUrl } from "@/lib/api-config";
import { useAuth } from "@/lib/auth-context";
import { SkeletonPendingList } from "@/components/SkeletonLoader";
import { StatusBadge } from "@/components/StatusBadge";
import { useDebounce } from "@/hooks/useDebounce";
import { setApprovalListNav } from "@/lib/approval-list-nav";
import { formatINR, type POStatus } from "@/lib/mock-data";
import { formatShortDate } from "@/lib/utils";
import { BILL_PAYMENT_ENTRY_ENABLED } from "@/lib/feature-flags";
import { BillPaymentEntryUnavailable } from "@/components/BillPaymentEntryUnavailable";

type BillPaymentEntryRow = {
  paymentNo: string;
  PaymentNo?: string;
  companyName: string;
  VendorName?: string;
  vendorName?: string;
  billNo?: string;
  BillNo?: string;
  paymentAmount: number;
  paymentDate: string;
  remarks: string;
};

function vendorName(row: BillPaymentEntryRow): string {
  return row.vendorName || row.VendorName || row.companyName || "";
}

function billNo(row: BillPaymentEntryRow): string {
  return row.billNo || row.BillNo || "";
}

export const Route = createFileRoute("/_app/advance-payments")({
  head: () => ({ meta: [{ title: "Bill Payment Entry — Approval Portal" }] }),
  component: BILL_PAYMENT_ENTRY_ENABLED ? AdvancePaymentsList : BillPaymentEntryUnavailable,
});

function AdvancePaymentsList() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [q, setQ] = useState("");
  const [amount, setAmount] = useState("");
  const [filterType, setFilterType] = useState("gte");
  const [status, setStatus] = useState<"All" | POStatus>("Pending");
  const [sortDesc, setSortDesc] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [selectMode, setSelectMode] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [showBulkConfirm, setShowBulkConfirm] = useState(false);
  const [bulkRemarks, setBulkRemarks] = useState("");
  const [isBulkApproving, setIsBulkApproving] = useState(false);
  const itemsPerPage = 20;

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "instant" });
    document.documentElement.scrollTop = 0;
    document.body.scrollTop = 0;
  }, []);

  const debouncedAmount = useDebounce(amount, 500);

  const { data: pendingPaymentsData, isLoading, refetch } = useQuery({
    queryKey: ["advance-payment-list", user?.username, debouncedAmount, filterType],
    queryFn: async () => {
      if (!user?.username) throw new Error("No username");
      const response = await fetch(
        getApiUrl(
          `/api/AdvancePayment/pending/${user.username}?amount=${debouncedAmount}&filterType=${filterType}`,
        ),
      );
      if (!response.ok) throw new Error("Failed to fetch bill payment entries");
      return response.json();
    },
    staleTime: 0,
    refetchOnMount: "always",
    enabled: !!user?.username && typeof window !== "undefined",
  });

  const pendingPayments = Array.isArray(pendingPaymentsData) ? (pendingPaymentsData as BillPaymentEntryRow[]) : [];

  const filtered = pendingPayments
    .filter((p) => {
      const search = q.toLowerCase();
      const matchesSearch =
        p.paymentNo?.toLowerCase().includes(search) ||
        vendorName(p).toLowerCase().includes(search) ||
        billNo(p).toLowerCase().includes(search) ||
        p.companyName?.toLowerCase().includes(search) ||
        p.remarks?.toLowerCase().includes(search);

      const matchesStatus = status === "All" || status === "Pending";
      const paymentAmount = Number(p.paymentAmount || 0);
      const enteredAmount = Number(amount || 0);
      const matchesAmount =
        amount === ""
          ? true
          : filterType === "gte"
            ? paymentAmount >= enteredAmount
            : paymentAmount <= enteredAmount;

      return matchesSearch && matchesStatus && matchesAmount;
    })
    .sort((a, b) =>
      sortDesc
        ? new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime()
        : new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime(),
    );

  useEffect(() => {
    setCurrentPage(1);
  }, [q, amount, status, filterType, sortDesc]);

  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedData = filtered.slice(startIndex, endIndex);

  const pageSelectableIds = useMemo(
    () => paginatedData.filter((p) => !!p?.paymentNo).map((p) => String(p.paymentNo)),
    [paginatedData],
  );

  const allSelectableIds = useMemo(
    () => filtered.filter((p) => !!p?.paymentNo).map((p) => String(p.paymentNo)),
    [filtered],
  );

  const allPageSelected =
    pageSelectableIds.length > 0 && pageSelectableIds.every((id) => selected.has(id));

  const allResultsSelected =
    allSelectableIds.length > 0 && allSelectableIds.every((id) => selected.has(id));

  const swipeSelectionRef = useRef<{
    active: boolean;
    touchId: number | null;
    suppressNextClick: boolean;
    dragging: boolean;
    startX: number;
    startY: number;
    lastX: number;
    lastY: number;
    pressTimer: number | null;
  }>({
    active: false,
    touchId: null,
    suppressNextClick: false,
    dragging: false,
    startX: 0,
    startY: 0,
    lastX: 0,
    lastY: 0,
    pressTimer: null,
  });

  const autoScrollRef = useRef<{
    raf: number | null;
    direction: -1 | 0 | 1;
    speed: number;
  }>({
    raf: null,
    direction: 0,
    speed: 0,
  });

  const currentTouchXRef = useRef(0);
  const currentTouchYRef = useRef(0);

  function stopAutoScroll() {
    const state = autoScrollRef.current;
    if (state.raf != null) {
      window.cancelAnimationFrame(state.raf);
      state.raf = null;
    }
    state.direction = 0;
    state.speed = 0;
  }

  function selectCardAtPoint(x: number, y: number) {
    const element = document.elementFromPoint(x, y);
    const card = element?.closest<HTMLElement>("[data-advance-payment-select-id]");
    if (!card) return;
    const id = card.dataset.advancePaymentSelectId;
    if (id) selectOne(id);
  }

  function updateAutoScroll(x: number, y: number) {
    const edgeThreshold = 220;
    const height = window.innerHeight;
    const state = autoScrollRef.current;

    let direction: -1 | 0 | 1 = 0;
    let speed = 0;

    if (y < edgeThreshold) {
      direction = -1;
      speed = Math.max(6, Math.round((edgeThreshold - y) / 4));
    } else if (y > height - edgeThreshold) {
      direction = 1;
      speed = Math.max(6, Math.round((y - (height - edgeThreshold)) / 4));
    }

    state.direction = direction;
    state.speed = speed;

    if (direction === 0) {
      stopAutoScroll();
      return;
    }

    if (state.raf != null) return;

    const tick = () => {
      const current = autoScrollRef.current;
      if (current.direction === 0) {
        current.raf = null;
        return;
      }

      window.scrollBy(0, current.direction * current.speed);
      selectCardAtPoint(currentTouchXRef.current, currentTouchYRef.current);
      current.raf = window.requestAnimationFrame(tick);
    };

    state.raf = window.requestAnimationFrame(tick);
  }

  function getCardIdFromPoint(event: { clientX: number; clientY: number }) {
    const element = document.elementFromPoint(event.clientX, event.clientY);
    const card = element?.closest<HTMLElement>("[data-advance-payment-select-id]");
    return card?.dataset.advancePaymentSelectId ?? null;
  }

  function getActiveTouch(event: ReactTouchEvent<HTMLButtonElement>) {
    const touchId = swipeSelectionRef.current.touchId;
    if (touchId == null) return null;
    return Array.from(event.touches).find((touch) => touch.identifier === touchId) ?? null;
  }

  useEffect(() => {
    if (!selectMode) return;

    function onTouchMove(event: TouchEvent) {
      const gesture = swipeSelectionRef.current;
      if (gesture.touchId == null) return;

      const touch = Array.from(event.touches).find((item) => item.identifier === gesture.touchId);
      if (!touch) return;

      currentTouchXRef.current = touch.clientX;
      currentTouchYRef.current = touch.clientY;

      const dx = Math.abs(touch.clientX - gesture.startX);
      const dy = Math.abs(touch.clientY - gesture.startY);

      if (!gesture.active) {
        if (dx > 8 || dy > 8) {
          if (gesture.pressTimer) {
            window.clearTimeout(gesture.pressTimer);
            gesture.pressTimer = null;
          }
          stopAutoScroll();
        }
        return;
      }

      event.preventDefault();
      gesture.suppressNextClick = true;

      const element = document.elementFromPoint(touch.clientX, touch.clientY);
      const card = element?.closest<HTMLElement>("[data-advance-payment-select-id]");
      const id = card?.dataset.advancePaymentSelectId;
      if (id) selectOne(id);

      updateAutoScroll(touch.clientX, touch.clientY);
    }

    function resetGesture(event: TouchEvent) {
      const gesture = swipeSelectionRef.current;
      if (gesture.touchId == null) return;

      const ended = Array.from(event.changedTouches).some((item) => item.identifier === gesture.touchId);
      if (!ended) return;

      if (gesture.pressTimer) {
        window.clearTimeout(gesture.pressTimer);
        gesture.pressTimer = null;
      }
      gesture.active = false;
      gesture.dragging = false;
      gesture.touchId = null;
      stopAutoScroll();
    }

    window.addEventListener("touchmove", onTouchMove, { passive: false });
    window.addEventListener("touchend", resetGesture);
    window.addEventListener("touchcancel", resetGesture);

    return () => {
      window.removeEventListener("touchmove", onTouchMove);
      window.removeEventListener("touchend", resetGesture);
      window.removeEventListener("touchcancel", resetGesture);
    };
  }, [selectMode]);

  function startSwipeSelection(paymentNo: string, event: ReactTouchEvent<HTMLButtonElement>) {
    if (!selectMode) return;

    const touch = event.changedTouches[0];
    if (!touch) return;

    const gesture = swipeSelectionRef.current;
    if (gesture.pressTimer) {
      window.clearTimeout(gesture.pressTimer);
      gesture.pressTimer = null;
    }

    gesture.active = false;
    gesture.touchId = touch.identifier;
    gesture.suppressNextClick = false;
    gesture.dragging = false;
    gesture.startX = touch.clientX;
    gesture.startY = touch.clientY;
    gesture.pressTimer = window.setTimeout(() => {
      const current = swipeSelectionRef.current;
      if (!selectMode || current.touchId !== touch.identifier) return;
      current.active = true;
      current.dragging = true;
      current.suppressNextClick = true;
      current.pressTimer = null;
      selectOne(paymentNo);
    }, 180);
  }

  function extendSwipeSelection(paymentNo: string, event: ReactTouchEvent<HTMLButtonElement>) {
    const gesture = swipeSelectionRef.current;
    const touch = getActiveTouch(event);
    if (!selectMode || !touch || gesture.touchId !== touch.identifier) return;

    if (!gesture.active) {
      const dx = Math.abs(touch.clientX - gesture.startX);
      const dy = Math.abs(touch.clientY - gesture.startY);
      if (dx > 8 || dy > 8) {
        if (gesture.pressTimer) {
          window.clearTimeout(gesture.pressTimer);
          gesture.pressTimer = null;
        }
      }
      return;
    }

    event.preventDefault();
    gesture.suppressNextClick = true;
    selectOne(getCardIdFromPoint(touch) ?? paymentNo);
  }

  function endSwipeSelection(event: ReactTouchEvent<HTMLButtonElement>) {
    const gesture = swipeSelectionRef.current;
    const touch = event.changedTouches[0];
    if (touch && gesture.touchId === touch.identifier) {
      if (gesture.pressTimer) {
        window.clearTimeout(gesture.pressTimer);
        gesture.pressTimer = null;
      }
      gesture.active = false;
      gesture.dragging = false;
      gesture.touchId = null;
    }
  }

  function handleSwipeClick(
    paymentNo: string,
    canSelect: boolean,
    event: ReactMouseEvent<HTMLButtonElement>
  ) {
    const gesture = swipeSelectionRef.current;
    if (gesture.suppressNextClick) {
      gesture.suppressNextClick = false;
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (canSelect) {
      toggleOne(paymentNo);
    }
  }

  function exitSelectMode() {
    setSelectMode(false);
    setSelected(new Set());
    setShowBulkConfirm(false);
    setBulkRemarks("");
  }

  function toggleSelectMode() {
    if (selectMode) {
      exitSelectMode();
    } else {
      setSelectMode(true);
      setSelected(new Set());
    }
  }

  function toggleOne(paymentNo: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(paymentNo)) next.delete(paymentNo);
      else next.add(paymentNo);
      return next;
    });
  }

  function selectOne(paymentNo: string) {
    setSelected((prev) => {
      if (prev.has(paymentNo)) return prev;
      const next = new Set(prev);
      next.add(paymentNo);
      return next;
    });
  }

  function toggleSelectAllPage() {
    setSelected((prev) => {
      const next = new Set(prev);
      if (allPageSelected) {
        pageSelectableIds.forEach((id) => next.delete(id));
      } else {
        pageSelectableIds.forEach((id) => next.add(id));
      }
      return next;
    });
  }

  function toggleSelectAllResults() {
    setSelected((prev) => {
      const next = new Set(prev);
      if (allResultsSelected) {
        allSelectableIds.forEach((id) => next.delete(id));
      } else {
        allSelectableIds.forEach((id) => next.add(id));
      }
      return next;
    });
  }

  async function runBulkApprove() {
    if (!user?.username || selected.size === 0 || isBulkApproving) return;

    setIsBulkApproving(true);
    try {
      const response = await fetch(getApiUrl("/api/AdvancePayment/approve-bulk"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          paymentNos: Array.from(selected),
          comment: bulkRemarks,
          userName: user.username,
        }),
      });

      if (!response.ok) {
        const errText = await response.text();
        throw new Error(errText || "Bulk approve failed");
      }

      const result = await response.json();
      const okCount = Array.isArray(result.succeeded) ? result.succeeded.length : 0;
      const failCount = Array.isArray(result.failed) ? result.failed.length : 0;

      if (okCount > 0 && failCount === 0) {
        toast.success(`Approved ${okCount} bill payment${okCount === 1 ? "" : "s"}`);
      } else if (okCount > 0 && failCount > 0) {
        toast.warning(`Approved ${okCount}, failed ${failCount}`);
      } else {
        toast.error(failCount > 0 ? `All ${failCount} failed` : "Nothing was approved");
      }

      exitSelectMode();
      void queryClient.invalidateQueries({ queryKey: ["advance-payment-list"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard-stats"] });
      void queryClient.invalidateQueries({ queryKey: ["pending-list-dashboard"] });
      await refetch();
    } catch (err) {
      console.error(err);
      toast.error("Bulk approve failed");
    } finally {
      setIsBulkApproving(false);
    }
  }

  return (
    <div className={`w-full min-w-0 max-w-full overflow-x-hidden space-y-6 ${selectMode && selected.size > 0 ? "pb-24 md:pb-0" : ""}`}>
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div className="card-3d min-w-0 w-full flex-1 rounded-2xl px-4 py-3">
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Bill Payment Entry Approval</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {filtered.length} result{filtered.length !== 1 ? "s" : ""}{" "}
            {totalPages > 1 && `(Page ${currentPage} of ${totalPages})`}
          </p>
        </div>
        <div className="flex w-full shrink-0 flex-wrap items-center justify-start gap-2 md:w-auto md:justify-end">
          {selectMode && selected.size > 0 && (
            <button
              type="button"
              onClick={() => setShowBulkConfirm(true)}
              disabled={isBulkApproving}
              className="hidden h-10 items-center rounded-md bg-primary px-3 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 md:inline-flex"
            >
              Approve selected ({selected.size})
            </button>
          )}
          {selectMode && allSelectableIds.length > 0 && (
            <button
              type="button"
              onClick={toggleSelectAllResults}
              className="inline-flex h-10 items-center gap-2 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary"
            >
              <CheckSquare className="h-4 w-4" />
              {allResultsSelected ? "Clear all" : "Select all"}
            </button>
          )}
          <button
            type="button"
            onClick={toggleSelectMode}
            disabled={isLoading || filtered.length === 0}
            className="inline-flex h-10 items-center gap-2 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary disabled:opacity-50"
          >
            <CheckSquare className="h-4 w-4" />
            {selectMode ? "Cancel select" : "Select"}
          </button>
        </div>
      </div>

      <div className="hidden rounded-2xl border border-border bg-card p-4 shadow-soft md:flex md:flex-col md:gap-3">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search payment number, vendor, bill no…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
        <div className="flex gap-2">
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="Amount"
            className="h-10 w-32 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="h-10 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          >
            <option value="gte">≥</option>
            <option value="lte">≤</option>
          </select>
          <div className="relative">
            <Filter className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as POStatus | "All")}
              className="h-10 appearance-none rounded-md border border-input bg-surface pl-9 pr-8 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              <option>All</option>
              <option>Pending</option>
              <option>Approved</option>
              <option>Rejected</option>
            </select>
          </div>
          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary"
          >
            <ArrowUpDown className="h-4 w-4" />
            {sortDesc ? "Newest" : "Oldest"}
          </button>
        </div>
      </div>

      <div className="space-y-2 rounded-2xl border border-border bg-card p-3 shadow-soft md:hidden">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search bill payment entry…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>

        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <div className="flex min-w-0 flex-1 flex-wrap gap-1">
            {amount && (
              <button
                onClick={() => setAmount("")}
                className="inline-flex items-center gap-1 rounded-full bg-primary/20 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/30 transition"
              >
                {filterType} ₹{amount}
                <X className="h-3 w-3" />
              </button>
            )}
          </div>

          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-input bg-surface px-2 text-xs font-medium hover:bg-secondary whitespace-nowrap"
          >
            <ArrowUpDown className="h-3.5 w-3.5" />
            {sortDesc ? "Newest" : "Oldest"}
          </button>
        </div>
      </div>

      <div className="grid w-full min-w-0 max-w-full gap-3 md:hidden">
        {isLoading ? (
          <SkeletonPendingList />
        ) : filtered.length === 0 ? (
          <div className="rounded-2xl border border-border bg-card p-6 text-sm text-muted-foreground">
            No bill payment entries found.
          </div>
        ) : (
          paginatedData.map((p) => {
            const paymentNo = String(p.paymentNo ?? "");
            const canSelect = selectMode && !!paymentNo;
            const isChecked = canSelect && selected.has(paymentNo);
            const status = "Pending" as const;

            if (selectMode) {
              return (
                <button
                  key={paymentNo}
                  type="button"
                  data-advance-payment-select-id={paymentNo}
                  disabled={!canSelect}
                  onTouchStart={(event) => startSwipeSelection(paymentNo, event)}
                  onClick={(event) => handleSwipeClick(paymentNo, canSelect, event)}
                  onContextMenu={(event) => event.preventDefault()}
                  style={selectMode ? { touchAction: "none" } : undefined}
                  className={`block w-full min-w-0 max-w-full text-left rounded-2xl border bg-card p-4 shadow-soft ${
                    isChecked ? "border-primary ring-1 ring-primary/30" : "border-border"
                  } ${!canSelect ? "opacity-50" : ""} select-none`}
                >
                  <div className="flex items-start gap-3">
                    <input
                      type="checkbox"
                      checked={!!isChecked}
                      onTouchStart={(event) => event.stopPropagation()}
                      onClick={(event) => {
                        event.stopPropagation();
                        if (canSelect) toggleOne(paymentNo);
                      }}
                      className="mt-1 h-4 w-4"
                      tabIndex={-1}
                    />
                    <div className="min-w-0 flex-1">
                      <div className="flex min-w-0 items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                          <div className="truncate font-semibold">{p.paymentNo}</div>
                          <div className="mt-0.5 truncate text-sm text-muted-foreground">
                            {vendorName(p)}
                          </div>
                        </div>
                        <StatusBadge status={status} />
                      </div>
                      <div className="mt-2 truncate text-xs text-muted-foreground">
                        {billNo(p) || vendorName(p)}
                      </div>
                      <div className="mt-3 flex min-w-0 items-end justify-between gap-2">
                        <div className="min-w-0 truncate text-xs text-muted-foreground">
                          {formatShortDate(p.paymentDate)}
                        </div>
                        <div className="shrink-0 text-base font-semibold tabular-nums">
                          {formatINR(p.paymentAmount || 0)}
                        </div>
                      </div>
                    </div>
                  </div>
                </button>
              );
            }

            return (
              <Link
                key={paymentNo}
                to="/advance-payment/$paymentNo"
                params={{ paymentNo }}
                onClick={() =>
                  setApprovalListNav(
                    "advancePayment",
                    filtered.map((row) => String(row.paymentNo ?? "")).filter(Boolean),
                  )
                }
                className="block w-full min-w-0 max-w-full rounded-2xl border border-border bg-card p-4 shadow-soft active:scale-[.99]"
              >
                <div className="flex min-w-0 items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-semibold">{p.paymentNo}</div>
                    <div className="mt-0.5 truncate text-sm text-muted-foreground">{vendorName(p)}</div>
                  </div>
                  <StatusBadge status={status} />
                </div>
                <div className="mt-2 truncate text-xs text-muted-foreground">
                  {billNo(p) || "—"}
                </div>
                <div className="mt-3 flex min-w-0 items-end justify-between gap-2">
                  <div className="min-w-0 truncate text-xs text-muted-foreground">
                    {formatShortDate(p.paymentDate)}
                  </div>
                  <div className="shrink-0 text-base font-semibold tabular-nums">
                    {formatINR(p.paymentAmount || 0)}
                  </div>
                </div>
              </Link>
            );
          })
        )}
      </div>

      <div className="hidden overflow-hidden rounded-2xl border border-border bg-card shadow-soft md:block">
        {isLoading ? (
          <div className="p-6">
            <SkeletonPendingList />
          </div>
        ) : filtered.length === 0 ? (
          <div className="p-6 text-sm text-muted-foreground">No bill payment entries found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                {selectMode && (
                  <th className="w-10 px-4 py-3 font-medium">
                    <input
                      type="checkbox"
                      checked={allPageSelected}
                      onChange={toggleSelectAllPage}
                      aria-label="Select all on page"
                    />
                  </th>
                )}
                <th className="px-4 py-3 font-medium">Payment No</th>
                <th className="px-4 py-3 font-medium">Company</th>
                <th className="px-4 py-3 font-medium">Vendor</th>
                <th className="px-4 py-3 font-medium">Bill No</th>
                <th className="px-4 py-3 font-medium">Payment Date</th>
                <th className="px-4 py-3 text-right font-medium">Amount</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {paginatedData.map((p) => {
                const paymentNo = String(p.paymentNo ?? "");
                const canSelect = selectMode && !!paymentNo;
                const isChecked = canSelect && selected.has(paymentNo);
                const statusLabel = "Pending" as const;
                return (
                  <tr key={paymentNo} className={`hover:bg-secondary/40 ${isChecked ? "bg-primary/5" : ""}`}>
                    {selectMode && (
                      <td className="px-4 py-3">
                        <input
                          type="checkbox"
                          disabled={!canSelect}
                          checked={!!isChecked}
                          onChange={() => canSelect && toggleOne(paymentNo)}
                          aria-label={`Select ${p.paymentNo}`}
                        />
                      </td>
                    )}
                    <td className="px-4 py-3 font-medium">
                      {selectMode ? (
                        <span>{p.paymentNo}</span>
                      ) : (
                        <Link
                          to="/advance-payment/$paymentNo"
                          params={{ paymentNo }}
                          onClick={() =>
                            setApprovalListNav(
                              "advancePayment",
                              filtered.map((row) => String(row.paymentNo ?? "")).filter(Boolean),
                            )
                          }
                          className="hover:text-primary hover:underline"
                        >
                          {p.paymentNo}
                        </Link>
                      )}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{p.companyName || "—"}</td>
                    <td className="px-4 py-3 text-muted-foreground">{vendorName(p)}</td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {billNo(p) || "—"}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(p.paymentDate).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3 text-right font-semibold tabular-nums">
                      {formatINR(p.paymentAmount || 0)}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={statusLabel} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between rounded-2xl border border-border bg-card px-4 py-3 shadow-soft sm:px-6">
          <div className="flex flex-1 justify-between sm:hidden">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="relative inline-flex items-center rounded-md border border-input bg-surface px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Previous
            </button>
            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="relative ml-3 inline-flex items-center rounded-md border border-input bg-surface px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
            </button>
          </div>
          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Showing <span className="font-medium">{startIndex + 1}</span> to{" "}
                <span className="font-medium">{Math.min(endIndex, filtered.length)}</span> of{" "}
                <span className="font-medium">{filtered.length}</span> results
              </p>
            </div>
            <div>
              <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                <button
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="relative inline-flex items-center rounded-l-md border border-input bg-surface px-2 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Previous</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path
                      fillRule="evenodd"
                      d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z"
                      clipRule="evenodd"
                    />
                  </svg>
                </button>

                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  let pageNum;
                  if (totalPages <= 5) {
                    pageNum = i + 1;
                  } else if (currentPage <= 3) {
                    pageNum = i + 1;
                  } else if (currentPage >= totalPages - 2) {
                    pageNum = totalPages - 4 + i;
                  } else {
                    pageNum = currentPage - 2 + i;
                  }
                  return (
                    <button
                      key={pageNum}
                      onClick={() => setCurrentPage(pageNum)}
                      className={`relative inline-flex items-center border border-input px-4 py-2 text-sm font-medium ${
                        currentPage === pageNum ? "z-10 bg-primary text-primary-foreground" : "bg-surface hover:bg-secondary"
                      }`}
                    >
                      {pageNum}
                    </button>
                  );
                })}

                <button
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="relative inline-flex items-center rounded-r-md border border-input bg-surface px-2 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Next</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path
                      fillRule="evenodd"
                      d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
                      clipRule="evenodd"
                    />
                  </svg>
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}

      {showBulkConfirm && selected.size > 0 && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={() => setShowBulkConfirm(false)}>
          <div className="w-full max-w-md rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-base font-semibold">Approve selected bill payment entries?</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              You are about to approve {selected.size} item{selected.size === 1 ? "" : "s"}.
            </p>
            <textarea
              value={bulkRemarks}
              onChange={(e) => setBulkRemarks(e.target.value)}
              placeholder="Optional remarks"
              className="mt-4 min-h-24 w-full rounded-md border border-input bg-surface px-3 py-2 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
            <div className="mt-5 flex gap-2">
              <button onClick={() => setShowBulkConfirm(false)} className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary">
                Cancel
              </button>
              <button
                onClick={runBulkApprove}
                disabled={isBulkApproving}
                className="h-10 flex-1 rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isBulkApproving ? (
                  <span className="inline-flex items-center gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Approving...
                  </span>
                ) : (
                  "Confirm approve"
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
