import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getApiUrl } from "@/lib/api-config";
import type { ApprovalListKind } from "@/lib/approval-list-nav";

export type InboxKind = ApprovalListKind;

export type InboxItem = {
  kind: InboxKind;
  id: string;
  title: string;
  subtitle: string;
  amount: number | null;
  extra: string;
  date: string;
  days: number | null;
  status: string;
};

export function daysWaiting(date: string): number | null {
  if (!date) return null;
  const parsed = new Date(date);
  if (Number.isNaN(parsed.getTime())) return null;
  return Math.max(0, Math.floor((Date.now() - parsed.getTime()) / 86_400_000));
}

export type InboxCounts = {
  po: number;
  workorder: number;
  payment: number;
  advancePayment: number;
  indent: number;
  total: number;
};

function pick(row: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const val = row[key];
    if (val != null && String(val).trim() !== "") return String(val);
  }
  return "";
}

function pickAmount(row: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const val = row[key];
    if (val == null || val === "") continue;
    const n = Number(val);
    if (!Number.isNaN(n)) return n;
  }
  return null;
}

async function fetchPendingList(path: string): Promise<Record<string, unknown>[]> {
  const response = await fetch(getApiUrl(path));
  if (!response.ok) throw new Error("Failed to load pending list");
  const data = await response.json();
  return Array.isArray(data) ? data : [];
}

function mapPoLike(rows: Record<string, unknown>[], kind: "po" | "workorder"): InboxItem[] {
  return rows
    .map((row) => {
      const id = pick(row, "PoNo", "poNo", "PONo");
      if (!id) return null;
      const date = pick(row, "PODate", "poDate", "Date", "date");
      return {
        kind,
        id,
        title: id,
        subtitle: pick(row, "FirmName", "firmName", "VendorName", "vendorName") || "—",
        amount: pickAmount(row, "Total", "total"),
        extra: "",
        date,
        days: daysWaiting(date),
        status: pick(row, "Status", "status") || "Pending",
      } satisfies InboxItem;
    })
    .filter((row): row is InboxItem => row != null);
}

function mapPayments(rows: Record<string, unknown>[]): InboxItem[] {
  return rows
    .map((row) => {
      const id = pick(row, "paymentNo", "PaymentNo");
      if (!id) return null;
      const date = pick(row, "paymentDate", "PaymentDate");
      return {
        kind: "payment" as const,
        id,
        title: id,
        subtitle: pick(row, "VendorName", "vendorName", "FirmName", "firmName") || "—",
        amount: pickAmount(row, "paymentAmount", "PaymentAmount", "Total", "total"),
        extra: "",
        date,
        days: daysWaiting(date),
        status: pick(row, "Status", "status") || "Pending",
      } satisfies InboxItem;
    })
    .filter((row): row is InboxItem => row != null);
}

function mapAdvancePayments(rows: Record<string, unknown>[]): InboxItem[] {
  return rows
    .map((row) => {
      const id = pick(row, "PaymentNo", "paymentNo");
      if (!id) return null;
      const date = pick(row, "PaymentDate", "paymentDate");
      const statusRaw = pick(row, "ApprovalStatus", "approvalStatus");
      const status =
        statusRaw === "1" ? "Approved" : statusRaw === "2" ? "Rejected" : "Pending";
      return {
        kind: "advancePayment" as const,
        id,
        title: id,
        subtitle:
          pick(row, "CompanyName", "companyName") ||
          pick(row, "PaymentTypeNo", "paymentTypeNo") ||
          "—",
        amount: pickAmount(row, "PaymentAmount", "paymentAmount", "PaymentAmt", "paymentAmt"),
        extra: pick(row, "PaymentTypeNo", "paymentTypeNo", "Remarks", "remarks"),
        date,
        days: daysWaiting(date),
        status,
      } satisfies InboxItem;
    })
    .filter((row): row is InboxItem => row != null);
}

function mapIndents(rows: Record<string, unknown>[]): InboxItem[] {
  return rows
    .map((row) => {
      const id = pick(row, "IndentNo", "indentNo");
      if (!id) return null;
      const items = pick(row, "TotalItems", "totalItems");
      const date = pick(row, "IndentDate", "indentDate");
      return {
        kind: "indent" as const,
        id,
        title: id,
        subtitle: items ? `${items} items` : "Indent",
        amount: pickAmount(row, "Total", "total", "Amount", "amount"),
        extra: items ? `${items} items` : "",
        date,
        days: daysWaiting(date),
        status: pick(row, "Status", "status") || "Pending",
      } satisfies InboxItem;
    })
    .filter((row): row is InboxItem => row != null);
}

const inboxQueryOptions = {
  staleTime: 30_000,
  refetchOnMount: "always" as const,
};

export function useApprovalInbox(username?: string) {
  const enabled = !!username && typeof window !== "undefined";

  const poQuery = useQuery({
    queryKey: ["pending-list", username, "", "gte"],
    queryFn: () => fetchPendingList(`/api/PO/pending/${username}?amount=&filterType=gte`),
    ...inboxQueryOptions,
    enabled,
  });

  const workorderQuery = useQuery({
    queryKey: ["workorder-list", username, "", "gte"],
    queryFn: () => fetchPendingList(`/api/WorkOrder/pending/${username}?amount=&filterType=gte`),
    ...inboxQueryOptions,
    enabled,
  });

  const paymentQuery = useQuery({
    queryKey: ["payment-list", username, "", "gte"],
    queryFn: () => fetchPendingList(`/api/Payment/pending/${username}?amount=&filterType=gte`),
    ...inboxQueryOptions,
    enabled,
  });

  const advancePaymentQuery = useQuery({
    queryKey: ["advance-payment-list", username, "", "gte"],
    queryFn: () =>
      fetchPendingList(`/api/AdvancePayment/pending/${username}?amount=&filterType=gte`),
    ...inboxQueryOptions,
    enabled,
  });

  const indentQuery = useQuery({
    queryKey: ["indent-list", username, "", "gte"],
    queryFn: () => fetchPendingList(`/api/Indent/pending/${username}?amount=&filterType=gte`),
    ...inboxQueryOptions,
    enabled,
  });

  const poItems = useMemo(() => mapPoLike(poQuery.data ?? [], "po"), [poQuery.data]);
  const workorderItems = useMemo(
    () => mapPoLike(workorderQuery.data ?? [], "workorder"),
    [workorderQuery.data],
  );
  const paymentItems = useMemo(() => mapPayments(paymentQuery.data ?? []), [paymentQuery.data]);
  const advancePaymentItems = useMemo(
    () => mapAdvancePayments(advancePaymentQuery.data ?? []),
    [advancePaymentQuery.data],
  );
  const indentItems = useMemo(() => mapIndents(indentQuery.data ?? []), [indentQuery.data]);

  const counts: InboxCounts = {
    po: poItems.length,
    workorder: workorderItems.length,
    payment: paymentItems.length,
    advancePayment: advancePaymentItems.length,
    indent: indentItems.length,
    total:
      poItems.length +
      workorderItems.length +
      paymentItems.length +
      advancePaymentItems.length +
      indentItems.length,
  };

  const allItems = useMemo(
    () => [...poItems, ...workorderItems, ...paymentItems, ...advancePaymentItems, ...indentItems],
    [poItems, workorderItems, paymentItems, advancePaymentItems, indentItems],
  );

  const recent = useMemo(() => {
    return [...allItems]
      .sort((a, b) => (b.days ?? -1) - (a.days ?? -1) || (b.amount ?? 0) - (a.amount ?? 0))
      .slice(0, 8);
  }, [allItems]);

  const workQueue = useMemo(() => {
    const valued = allItems.filter((item) => item.amount != null);
    const totalValue = valued.reduce((sum, item) => sum + (item.amount ?? 0), 0);
    const ageDays = allItems.map((item) => item.days).filter((d): d is number => d != null);
    const oldestDays = ageDays.length ? Math.max(...ageDays) : null;
    const highValue = [...valued].sort((a, b) => (b.amount ?? 0) - (a.amount ?? 0)).slice(0, 3);
    return { totalValue, oldestDays, highValue };
  }, [allItems]);

  return {
    counts,
    recent,
    allItems,
    workQueue,
    poItems,
    workorderItems,
    paymentItems,
    advancePaymentItems,
    indentItems,
    isLoading:
      poQuery.isLoading ||
      workorderQuery.isLoading ||
      paymentQuery.isLoading ||
      advancePaymentQuery.isLoading ||
      indentQuery.isLoading,
    queues: {
      po: { loading: poQuery.isLoading, error: poQuery.isError },
      workorder: { loading: workorderQuery.isLoading, error: workorderQuery.isError },
      payment: { loading: paymentQuery.isLoading, error: paymentQuery.isError },
      advancePayment: {
        loading: advancePaymentQuery.isLoading,
        error: advancePaymentQuery.isError,
      },
      indent: { loading: indentQuery.isLoading, error: indentQuery.isError },
    },
  };
}

export function formatBadgeCount(count: number): string | null {
  if (count <= 0) return null;
  return count > 99 ? "99+" : String(count);
}
