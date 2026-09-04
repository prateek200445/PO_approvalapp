import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Search, X } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useApprovalInbox, type InboxItem, type InboxKind } from "@/hooks/useApprovalInbox";
import { formatINR } from "@/lib/mock-data";
import { cn } from "@/lib/utils";
import { setApprovalListNav } from "@/lib/approval-list-nav";
import { BILL_PAYMENT_ENTRY_ENABLED } from "@/lib/feature-flags";

const KIND_LABEL: Record<InboxKind, string> = {
  po: "PO",
  workorder: "WO",
  payment: "Pay",
  advancePayment: "Bill Pay",
  indent: "Indent",
};

export function GlobalCommandPalette() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const inbox = useApprovalInbox(user?.username);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setOpen((prev) => !prev);
      }
      if (event.key === "Escape") setOpen(false);
    }
    function onOpen() {
      setOpen(true);
    }
    window.addEventListener("keydown", onKey);
    window.addEventListener("po-open-search", onOpen);
    return () => {
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("po-open-search", onOpen);
    };
  }, []);

  useEffect(() => {
    if (!open) return;
    setQuery("");
    setActive(0);
    const id = window.setTimeout(() => inputRef.current?.focus(), 20);
    return () => window.clearTimeout(id);
  }, [open]);

  const results = useMemo(() => {
    const q = query.trim().toLowerCase();
    const items = inbox.allItems.filter(
      (item) => BILL_PAYMENT_ENTRY_ENABLED || item.kind !== "advancePayment",
    );
    if (!q) return items.slice(0, 12);
    return items
      .filter(
        (item) =>
          item.title.toLowerCase().includes(q) ||
          item.subtitle.toLowerCase().includes(q) ||
          KIND_LABEL[item.kind].toLowerCase().includes(q),
      )
      .slice(0, 12);
  }, [inbox.allItems, query]);

  function go(item: InboxItem) {
    const ids = inbox.allItems.filter((row) => row.kind === item.kind).map((row) => row.id);
    setApprovalListNav(item.kind, ids);
    if (item.kind === "po") navigate({ to: "/po/$poNo", params: { poNo: item.id } });
    else if (item.kind === "workorder") navigate({ to: "/workorder/$poNo", params: { poNo: item.id } });
    else if (item.kind === "payment") navigate({ to: "/payment/$paymentNo", params: { paymentNo: item.id } });
    else if (item.kind === "advancePayment")
      navigate({ to: "/advance-payment/$paymentNo", params: { paymentNo: item.id } });
    else navigate({ to: "/indent/$indentNo", params: { indentNo: item.id } });
    setOpen(false);
  }

  function onInputKey(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((i) => Math.min(i + 1, Math.max(results.length - 1, 0)));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((i) => Math.max(i - 1, 0));
    } else if (event.key === "Enter" && results[active]) {
      event.preventDefault();
      go(results[active]);
    }
  }

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[80] flex items-end justify-center bg-black/50 md:items-start md:p-4 md:pt-[12vh]"
      onClick={() => setOpen(false)}
    >
      <div
        className="card-3d flex max-h-[88vh] w-full flex-col overflow-hidden rounded-t-2xl md:max-h-[70vh] md:max-w-lg md:rounded-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-2 border-b border-border px-3 py-3 md:py-2.5">
          <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setActive(0);
            }}
            onKeyDown={onInputKey}
            inputMode="search"
            enterKeyHint="search"
            placeholder="Search PO, vendor, payment…"
            className="h-11 w-full bg-transparent text-base outline-none md:h-9 md:text-sm"
          />
          <button
            type="button"
            onClick={() => setOpen(false)}
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-muted-foreground hover:bg-secondary md:hidden"
            aria-label="Close search"
          >
            <X className="h-4 w-4" />
          </button>
          <kbd className="hidden rounded border border-border bg-secondary px-1.5 py-0.5 text-[10px] text-muted-foreground md:inline">
            Esc
          </kbd>
        </div>
        <ul className="min-h-0 flex-1 overflow-y-auto py-1 pb-[env(safe-area-inset-bottom)]">
          {inbox.isLoading && (
            <li className="px-4 py-6 text-sm text-muted-foreground">Loading queues…</li>
          )}
          {!inbox.isLoading && results.length === 0 && (
            <li className="px-4 py-6 text-sm text-muted-foreground">No matching pending items.</li>
          )}
          {results.map((item, index) => (
            <li key={`${item.kind}-${item.id}`}>
              <button
                type="button"
                onMouseEnter={() => setActive(index)}
                onClick={() => go(item)}
                className={cn(
                  "flex min-h-14 w-full items-center justify-between gap-3 px-4 py-3 text-left md:min-h-0 md:py-2.5",
                  index === active ? "bg-primary/10" : "hover:bg-secondary/60",
                )}
              >
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="rounded bg-secondary px-1.5 py-0.5 text-[10px] font-semibold uppercase text-muted-foreground">
                      {KIND_LABEL[item.kind]}
                    </span>
                    <span className="truncate text-sm font-medium">{item.title}</span>
                  </div>
                  <div className="truncate text-xs text-muted-foreground">{item.subtitle}</div>
                </div>
                <div className="shrink-0 text-right text-xs">
                  <div className="font-semibold tabular-nums">
                    {item.amount != null ? formatINR(item.amount) : item.extra || "—"}
                  </div>
                  {item.days != null && (
                    <div className="text-muted-foreground">{item.days}d waiting</div>
                  )}
                </div>
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export function SearchTrigger({ collapsed }: { collapsed?: boolean }) {
  return (
    <button
      type="button"
      onClick={() => window.dispatchEvent(new Event("po-open-search"))}
      className={cn(
        "flex items-center rounded-xl border border-border bg-background text-sm text-muted-foreground transition hover:bg-secondary hover:text-foreground",
        collapsed ? "h-10 w-10 justify-center p-0" : "w-full gap-2 px-3 py-2",
      )}
      title="Search pending (Ctrl+K)"
    >
      <Search className="h-4 w-4 shrink-0" />
      {!collapsed && (
        <>
          <span className="flex-1 truncate text-left">Search pending</span>
          <kbd className="hidden rounded border border-border px-1.5 py-0.5 text-[10px] md:inline">
            Ctrl+K
          </kbd>
        </>
      )}
    </button>
  );
}
