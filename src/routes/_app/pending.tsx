import { createFileRoute, Link } from "@tanstack/react-router";
import { getApiUrl } from "@/lib/api-config";
import { useState, useEffect, useMemo, useRef } from "react";
import type { MouseEvent as ReactMouseEvent, TouchEvent as ReactTouchEvent } from "react";
import { Search, ArrowUpDown, Filter, X, CheckSquare, Loader2 } from "lucide-react";
import { formatINR, type POStatus } from "@/lib/mock-data";
import { useAuth } from "@/lib/auth-context";
import { StatusBadge } from "@/components/StatusBadge";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useDebounce } from "@/hooks/useDebounce";
import { SkeletonPendingList } from "@/components/SkeletonLoader";
import { toast } from "sonner";
import { setApprovalListNav } from "@/lib/approval-list-nav";
import { formatShortDate } from "@/lib/utils";

export const Route = createFileRoute("/_app/pending")({
  head: () => ({ meta: [{ title: "Pending POs — Approval Portal" }] }),
  component: PendingList,
});

function PendingList() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [q, setQ] = useState("");
  const [amount, setAmount] = useState("");
  const [filterType, setFilterType] = useState("gte");
  const [status, setStatus] = useState<"All" | POStatus>("Pending");
  const [sortDesc, setSortDesc] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [showFilterSheet, setShowFilterSheet] = useState(false);
  const itemsPerPage = 20;

  const [selectMode, setSelectMode] = useState(false);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [showBulkConfirm, setShowBulkConfirm] = useState(false);
  const [bulkRemarks, setBulkRemarks] = useState("");
  const [isBulkApproving, setIsBulkApproving] = useState(false);
  const [isTouchSelecting, setIsTouchSelecting] = useState(false);

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "instant" });
    document.documentElement.scrollTop = 0;
    document.body.scrollTop = 0;
  }, []);

  const debouncedAmount = useDebounce(amount, 500);

  const { data: pendingPOsData, isLoading, refetch } = useQuery({
    queryKey: ["pending-list", user?.username, debouncedAmount, filterType],
    queryFn: async () => {
      if (!user?.username) throw new Error("No username");
      const response = await fetch(
        getApiUrl(
          `/api/PO/pending/${user.username}?amount=${debouncedAmount}&filterType=${filterType}`
        )
      );
      if (!response.ok) throw new Error("Failed to fetch pending");
      return response.json();
    },
    staleTime: 0,
    refetchOnMount: "always",
    enabled: !!user?.username && typeof window !== "undefined",
  });

  const pendingPOs = Array.isArray(pendingPOsData) ? pendingPOsData : [];

  const filtered = pendingPOs
    .filter((p) => {
      const search = q.toLowerCase();

      const matchesSearch =
        p.PoNo?.toLowerCase().includes(search) ||
        p.ApprovalName?.toLowerCase().includes(search) ||
        p.Status?.toLowerCase().includes(search) ||
        String(p.Total || "").includes(search);
      const matchesStatus = status === "All" || p.Status === status;

      const poAmount = Number(p.Total || 0);
      const enteredAmount = Number(amount || 0);

      const matchesAmount =
        amount === ""
          ? true
          : filterType === "gte"
            ? poAmount >= enteredAmount
            : poAmount <= enteredAmount;

      return matchesSearch && matchesStatus && matchesAmount;
    })
    .sort((a, b) =>
      sortDesc
        ? new Date(b.PODate).getTime() - new Date(a.PODate).getTime()
        : new Date(a.PODate).getTime() - new Date(b.PODate).getTime()
    );

  useEffect(() => {
    setCurrentPage(1);
  }, [q, amount, status, filterType, sortDesc]);

  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedData = filtered.slice(startIndex, endIndex);

  const pageSelectableIds = useMemo(
    () =>
      paginatedData
        .filter((p) => p.Status === "Pending" && (p.TransId ?? p.Transid))
        .map((p) => Number(p.TransId ?? p.Transid)),
    [paginatedData]
  );

  const allSelectableIds = useMemo(
    () =>
      filtered
        .filter((p) => p.Status === "Pending" && (p.TransId ?? p.Transid))
        .map((p) => Number(p.TransId ?? p.Transid)),
    [filtered]
  );

  const allPageSelected =
    pageSelectableIds.length > 0 &&
    pageSelectableIds.every((id) => selected.has(id));

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
    const card = element?.closest<HTMLElement>("[data-po-select-id]");
    if (!card) return;
    const id = Number(card.dataset.poSelectId);
    if (Number.isFinite(id)) selectOne(id);
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

  const currentTouchXRef = useRef(0);
  const currentTouchYRef = useRef(0);

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

  function toggleOne(transId: number) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(transId)) next.delete(transId);
      else next.add(transId);
      return next;
    });
  }

  function selectOne(transId: number) {
    setSelected((prev) => {
      if (prev.has(transId)) return prev;
      const next = new Set(prev);
      next.add(transId);
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

  function getCardIdFromPoint(event: { clientX: number; clientY: number }) {
    const element = document.elementFromPoint(event.clientX, event.clientY);
    const card = element?.closest<HTMLElement>("[data-po-select-id]");
    if (!card) return null;

    const id = Number(card.dataset.poSelectId);
    return Number.isFinite(id) ? id : null;
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
      const card = element?.closest<HTMLElement>("[data-po-select-id]");
      const id = card ? Number(card.dataset.poSelectId) : null;
      if (Number.isFinite(id)) {
        selectOne(id as number);
      }

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
      setIsTouchSelecting(false);
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

  useEffect(() => {
    if (!isTouchSelecting) return;

    const bodyOverflow = document.body.style.overflow;
    const htmlOverflow = document.documentElement.style.overflow;
    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = bodyOverflow;
      document.documentElement.style.overflow = htmlOverflow;
    };
  }, [isTouchSelecting]);

  function startSwipeSelection(transId: number, event: ReactTouchEvent<HTMLButtonElement>) {
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
      selectOne(transId);
      setIsTouchSelecting(true);
    }, 180);
  }

  function extendSwipeSelection(transId: number, event: ReactTouchEvent<HTMLButtonElement>) {
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
    selectOne(getCardIdFromPoint(touch) ?? transId);
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
      setIsTouchSelecting(false);
    }
  }

  function handleSwipeClick(
    transId: number,
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
      toggleOne(transId);
    }
  }

  async function runBulkApprove() {
    if (!user?.username || selected.size === 0 || isBulkApproving) return;

    setIsBulkApproving(true);
    try {
      const response = await fetch(getApiUrl("/api/PO/approve-bulk"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          transIds: Array.from(selected),
          remarks: bulkRemarks,
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
        toast.success(`Approved ${okCount} PO${okCount === 1 ? "" : "s"}`);
      } else if (okCount > 0 && failCount > 0) {
        toast.warning(`Approved ${okCount}, failed ${failCount}`);
        const reasons = (result.failed as { poNo?: string; reason?: string }[])
          .slice(0, 3)
          .map((f) => `${f.poNo ?? "PO"}: ${f.reason ?? "failed"}`)
          .join(" · ");
        if (reasons) toast.message(reasons);
      } else {
        toast.error(failCount > 0 ? `All ${failCount} failed` : "Nothing was approved");
      }

      exitSelectMode();
      void queryClient.invalidateQueries({ queryKey: ["pending-list"] });
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
          <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Purchase Orders</h1>
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

      {selectMode && (
        <p className="px-1 text-xs text-muted-foreground md:hidden">
          Tap the checkbox, or long-press a card then drag up or down to select multiple rows.
        </p>
      )}

      {/* Filters - Desktop Only */}
      <div className="hidden rounded-2xl border border-border bg-card p-4 shadow-soft md:flex md:flex-col md:gap-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search PO number or vendor…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>
        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="Amount"
            className="h-10 w-full md:w-32 rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="h-10 w-full md:w-auto rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          >
            <option value="gte">≥</option>
            <option value="lte">≤</option>
          </select>
          <div className="relative flex-1 md:flex-none">
            <Filter className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as POStatus | "All")}
              className="h-10 w-full appearance-none rounded-md border border-input bg-surface pl-9 pr-8 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            >
              <option>All</option>
              <option>Pending</option>
              <option>Approved</option>
              <option>Rejected</option>
            </select>
          </div>
          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 w-full items-center justify-center gap-1.5 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary md:w-auto"
          >
            <ArrowUpDown className="h-4 w-4" />
            <span className="md:hidden">{sortDesc ? "Newest" : "Oldest"}</span>
            <span className="hidden md:inline">{sortDesc ? "Newest" : "Oldest"}</span>
          </button>
        </div>
      </div>

      {/* Filters - Mobile Only */}
      <div className="space-y-2 rounded-2xl border border-border bg-card p-3 shadow-soft md:hidden">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="text"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search PO…"
            className="h-10 w-full rounded-md border border-input bg-surface pl-9 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
          />
        </div>

        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <div className="flex min-w-0 flex-1 flex-wrap gap-1">
            {status !== "All" && (
              <button
                onClick={() => setStatus("All")}
                className="inline-flex items-center gap-1 rounded-full bg-primary/20 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/30 transition"
              >
                {status}
                <X className="h-3 w-3" />
              </button>
            )}
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
            onClick={() => setShowFilterSheet(true)}
            className="inline-flex h-10 items-center gap-2 rounded-md border border-input bg-surface px-3 text-sm font-medium hover:bg-secondary whitespace-nowrap"
          >
            <Filter className="h-4 w-4" />
            <span className="text-xs">Filters</span>
            {(status !== "All" || amount) && (
              <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                {(status !== "All" ? 1 : 0) + (amount ? 1 : 0)}
              </span>
            )}
          </button>

          <button
            onClick={() => setSortDesc((v) => !v)}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-input bg-surface px-2 text-xs font-medium hover:bg-secondary whitespace-nowrap"
          >
            <ArrowUpDown className="h-3.5 w-3.5" />
            {sortDesc ? "Newest" : "Oldest"}
          </button>
        </div>
      </div>

      {/* Mobile cards */}
      <div className="grid w-full min-w-0 max-w-full gap-3 md:hidden">
        {isLoading ? (
          <SkeletonPendingList />
        ) : filtered.length === 0 ? (
          <EmptyState />
        ) : (
          paginatedData.map((p) => {
            const transId = Number(p.TransId ?? p.Transid);
            const canSelect = selectMode && p.Status === "Pending" && !!transId;
            const isChecked = canSelect && selected.has(transId);

            if (selectMode) {
              return (
                <button
                  key={`${p.PoNo}-${transId}`}
                  type="button"
                  data-po-select-id={transId}
                  disabled={!canSelect}
                  onTouchStart={(event) => startSwipeSelection(transId, event)}
                  onClick={(event) => handleSwipeClick(transId, canSelect, event)}
                  onContextMenu={(event) => event.preventDefault()}
                  style={selectMode ? { touchAction: "pan-y" } : undefined}
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
                        if (canSelect) toggleOne(transId);
                      }}
                      className="mt-1 h-4 w-4"
                      tabIndex={-1}
                    />
                    <div className="min-w-0 flex-1">
                      <div className="flex min-w-0 items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                          <div className="truncate font-semibold">{p.PoNo}</div>
                          <div className="mt-0.5 truncate text-sm text-muted-foreground">
                            {p.FirmName || p.ApprovalName}
                          </div>
                        </div>
                        <StatusBadge status={p.Status} />
                      </div>
                      <div className="mt-3 flex min-w-0 items-end justify-between gap-2">
                        <div className="min-w-0 truncate text-xs text-muted-foreground">
                          {formatShortDate(p.PODate)}
                        </div>
                        <div className="shrink-0 text-base font-semibold tabular-nums">
                          {formatINR(p.Total || 0)}
                        </div>
                      </div>
                    </div>
                  </div>
                </button>
              );
            }

            return (
              <Link
                key={p.PoNo}
                to="/po/$poNo"
                params={{ poNo: p.PoNo }}
                onClick={() => setApprovalListNav("po", filtered.map((row) => row.PoNo))}
                className="block w-full min-w-0 max-w-full rounded-2xl border border-border bg-card p-4 shadow-soft active:scale-[.99]"
              >
                <div className="flex min-w-0 items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-semibold">{p.PoNo}</div>
                    <div className="mt-0.5 truncate text-sm text-muted-foreground">
                      {p.FirmName || p.ApprovalName}
                    </div>
                  </div>
                  <StatusBadge status={p.Status} />
                </div>
                <div className="mt-3 flex min-w-0 items-end justify-between gap-2">
                  <div className="min-w-0 truncate text-xs text-muted-foreground">
                    {formatShortDate(p.PODate)}
                  </div>
                  <div className="shrink-0 text-base font-semibold tabular-nums">
                    {formatINR(p.Total || 0)}
                  </div>
                </div>
              </Link>
            );
          })
        )}
      </div>

      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-2xl border border-border bg-card shadow-soft md:block">
        {isLoading ? (
          <div className="p-6">
            <SkeletonPendingList />
          </div>
        ) : filtered.length === 0 ? (
          <div className="p-6">
            <EmptyState />
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                {selectMode && (
                  <th className="px-4 py-3 font-medium w-10">
                    <input
                      type="checkbox"
                      checked={allPageSelected}
                      onChange={toggleSelectAllPage}
                      aria-label="Select all on page"
                    />
                  </th>
                )}
                <th className="px-4 py-3 font-medium">PO Number</th>
                <th className="px-4 py-3 font-medium">Vendor</th>
                <th className="px-4 py-3 font-medium">Date</th>
                <th className="px-4 py-3 text-right font-medium">Amount</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {paginatedData.map((p) => {
                const transId = Number(p.TransId ?? p.Transid);
                const canSelect = selectMode && p.Status === "Pending" && !!transId;
                const isChecked = canSelect && selected.has(transId);

                return (
                  <tr
                    key={`${p.PoNo}-${transId}`}
                    className={`hover:bg-secondary/40 ${isChecked ? "bg-primary/5" : ""}`}
                  >
                    {selectMode && (
                      <td className="px-4 py-3">
                        <input
                          type="checkbox"
                          disabled={!canSelect}
                          checked={!!isChecked}
                          onChange={() => canSelect && toggleOne(transId)}
                          aria-label={`Select ${p.PoNo}`}
                        />
                      </td>
                    )}
                    <td className="px-4 py-3 font-medium">
                      {selectMode ? (
                        <span>{p.PoNo}</span>
                      ) : (
                        <Link
                          to="/po/$poNo"
                          params={{ poNo: p.PoNo }}
                          onClick={() => setApprovalListNav("po", filtered.map((row) => row.PoNo))}
                          className="hover:text-primary hover:underline"
                        >
                          {p.PoNo}
                        </Link>
                      )}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{p.FirmName || p.ApprovalName}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.PODate}</td>
                    <td className="px-4 py-3 text-right font-semibold tabular-nums">
                      {formatINR(p.Total || 0)}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={p.Status} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination Controls */}
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
                        currentPage === pageNum
                          ? "z-10 bg-primary text-primary-foreground"
                          : "bg-surface hover:bg-secondary"
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

      {/* Mobile bulk approve bar */}
      {selectMode && selected.size > 0 && (
        <div className="safe-bottom fixed inset-x-0 bottom-0 z-20 border-t border-border bg-surface/95 p-3 backdrop-blur md:hidden">
          <div className="flex items-center gap-3">
            <div className="min-w-0 flex-1 text-sm font-medium">
              {selected.size} selected
            </div>
            <button
              type="button"
              onClick={() => setShowBulkConfirm(true)}
              disabled={isBulkApproving}
              className="h-11 shrink-0 rounded-md bg-primary px-5 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              Approve
            </button>
          </div>
        </div>
      )}

      {/* Bulk confirm modal */}
      {showBulkConfirm && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onClick={() => !isBulkApproving && setShowBulkConfirm(false)}
        >
          <div
            className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-base font-semibold">Approve {selected.size} PO{selected.size === 1 ? "" : "s"}?</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Each PO is approved with the same rules as single approve. Already-processed rows will be
              reported as failed without stopping the rest.
            </p>
            <textarea
              value={bulkRemarks}
              onChange={(e) => setBulkRemarks(e.target.value)}
              rows={3}
              placeholder="Optional remarks for all…"
              disabled={isBulkApproving}
              className="mt-3 w-full resize-none rounded-md border border-input bg-surface p-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
            <div className="mt-5 flex gap-2">
              <button
                type="button"
                onClick={() => setShowBulkConfirm(false)}
                disabled={isBulkApproving}
                className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={runBulkApprove}
                disabled={isBulkApproving}
                className="h-10 flex-1 rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                {isBulkApproving ? (
                  <span className="inline-flex items-center justify-center gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Approving…
                  </span>
                ) : (
                  "Confirm"
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Mobile Bottom Sheet Filter */}
      {showFilterSheet && (
        <div className="fixed inset-0 z-40 md:hidden">
          <div
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={() => setShowFilterSheet(false)}
          />

          <div className="absolute bottom-0 left-0 right-0 z-50 animate-in slide-in-from-bottom-5 rounded-t-2xl border-t border-border bg-surface p-4 pb-6 shadow-xl">
            <div className="mb-4 flex justify-center">
              <div className="h-1 w-10 rounded-full bg-muted" />
            </div>

            <h2 className="mb-4 text-lg font-semibold">Filters</h2>

            <div className="mb-5 space-y-2">
              <label className="text-sm font-medium">Status</label>
              <div className="space-y-2">
                {["All", "Pending", "Approved", "Rejected"].map((s) => (
                  <label key={s} className="flex items-center gap-3 cursor-pointer">
                    <input
                      type="radio"
                      name="status"
                      value={s}
                      checked={status === s}
                      onChange={(e) => setStatus(e.target.value as POStatus | "All")}
                      className="h-4 w-4"
                    />
                    <span className="text-sm">{s}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="mb-5 space-y-2">
              <label className="text-sm font-medium">Amount</label>
              <div className="flex gap-2">
                <input
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="Enter amount"
                  className="flex-1 h-10 rounded-md border border-input bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
                <select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                  className="h-10 w-16 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                >
                  <option value="gte">≥</option>
                  <option value="lte">≤</option>
                </select>
              </div>
            </div>

            <div className="mb-6 space-y-2">
              <label className="text-sm font-medium">Sort</label>
              <div className="space-y-2">
                {[
                  { value: true, label: "Newest" },
                  { value: false, label: "Oldest" },
                ].map((option) => (
                  <label
                    key={String(option.value)}
                    className="flex items-center gap-3 cursor-pointer"
                  >
                    <input
                      type="radio"
                      name="sort"
                      checked={sortDesc === option.value}
                      onChange={() => setSortDesc(option.value)}
                      className="h-4 w-4"
                    />
                    <span className="text-sm">{option.label}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="flex gap-2">
              <button
                onClick={() => {
                  setQ("");
                  setAmount("");
                  setFilterType("gte");
                  setStatus("All");
                  setSortDesc(true);
                  setCurrentPage(1);
                }}
                className="flex-1 h-10 rounded-md border border-input bg-background text-sm font-medium hover:bg-secondary"
              >
                Reset
              </button>
              <button
                onClick={() => setShowFilterSheet(false)}
                className="flex-1 h-10 rounded-md bg-primary text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Apply
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center gap-2 p-10 text-center">
      <div className="text-sm font-medium">No purchase orders found</div>
      <div className="text-xs text-muted-foreground">Try adjusting your search or filters.</div>
    </div>
  );
}
