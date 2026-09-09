import { useAuth } from "@/lib/auth-context";
import { getApiUrl } from "@/lib/api-config";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState, useEffect, useRef } from "react";
import { ArrowLeft, FileText, Download, CheckCircle2, XCircle, Building2, Calendar, User as UserIcon, Hash, IndianRupee, Briefcase, ClipboardList, ExternalLink, ChevronLeft, ChevronRight, ZoomIn, ZoomOut, Loader2 } from "lucide-react";
import { currencyLabel, formatMoney, formatMoneyAmount, type ApprovalStep } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { toast } from "sonner";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { SkeletonCard, SkeletonSection, SkeletonTable, SkeletonWorkflow } from "@/components/SkeletonLoader";
import { ApprovalDetailNav } from "@/components/ApprovalDetailNav";
import { DmsAttachmentsSection } from "@/components/DmsAttachmentsSection";
import { useApprovalListNavigation } from "@/hooks/use-approval-list-navigation";
import { invalidateApprovalCaches, resolveNextAfterApproval } from "@/lib/approval-after-action";
import { ApprovalCommandBar, RemarkComposer } from "@/components/ApprovalCommandBar";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/_app/po/$poNo")({
  head: ({ params }) => ({ meta: [{ title: `${params.poNo} — PO Details` }] }),
  component: PODetails,
  notFoundComponent: () => <div className="p-8 text-center">PO not found.</div>,
});

function PODetails() {
  const { poNo } = Route.useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const containerRef = useRef<HTMLDivElement | null>(null);
  const queryClient = useQueryClient();

  const [remarks, setRemarks] = useState("");
  const [rejectAttachment, setRejectAttachment] = useState<File | null>(null);
  const [confirm, setConfirm] = useState<null | "approve" | "reject">(null);
  const [isApproving, setIsApproving] = useState(false);
  const [isRejecting, setIsRejecting] = useState(false);
  const [selectedQuoteItemKey, setSelectedQuoteItemKey] = useState<string>("");
  const [selectedSummaryLine, setSelectedSummaryLine] = useState<string>("0");

  // PDF.js Inline Viewer State
  const [pdfDoc, setPdfDoc] = useState<any>(null);
  const [pageNum, setPageNum] = useState(1);
  const [numPages, setNumPages] = useState(0);
  const [scale, setScale] = useState(() => {
    if (typeof window !== "undefined" && window.innerWidth < 768) {
      return 0.5;
    }
    return 1.1;
  });
  const [pdfLoading, setPdfLoading] = useState(true);
  const [pdfError, setPdfError] = useState<string | null>(null);

  // Fetch PO details fast (indent + lines). Previous quotes load separately.
  const { data: poPayload, isLoading: poLoading } = useQuery({
    queryKey: ['po-details', poNo, 'indent-info-v2'],
    queryFn: async () => {
      const response = await fetch(
        getApiUrl(`/api/PO/details?poNo=${encodeURIComponent(poNo)}&includePreviousQuotes=false`),
      );
      if (!response.ok) throw new Error('Failed to fetch PO details');
      return response.json();
    },
    staleTime: 1000 * 60 * 5,
  });

  const { data: previousQuotesPayload } = useQuery({
    queryKey: ['po-previous-quotes', poNo, 'v1'],
    queryFn: async () => {
      const response = await fetch(
        getApiUrl(`/api/PO/previous-quotes?poNo=${encodeURIComponent(poNo)}`),
      );
      if (!response.ok) throw new Error('Failed to fetch previous quotes');
      return response.json();
    },
    enabled: !!poNo,
    staleTime: 1000 * 60 * 10,
  });

  // Fetch approval data with React Query
  const { data: approvalData } = useQuery({
    queryKey: ['po-approval', poNo, user?.username],
    queryFn: async () => {
      const response = await fetch(
        getApiUrl(`/api/PO/approval?poNo=${encodeURIComponent(poNo)}&username=${user?.username}`)
      );
      if (!response.ok) throw new Error('Failed to fetch approval');
      return response.json();
    },
    staleTime: 1000 * 60 * 30, // Cache for 30 minutes
    enabled: !!user?.username,
  });

  // Fetch workflow with React Query
  const { data: workflowData } = useQuery({
    queryKey: ['po-workflow', poNo],
    queryFn: async () => {
      const response = await fetch(getApiUrl(`/api/PO/workflow?poNo=${encodeURIComponent(poNo)}`));
      if (!response.ok) throw new Error('Failed to fetch workflow');
      return response.json();
    },
    staleTime: Infinity, // Cache indefinitely
  });

  const poNavigation = useApprovalListNavigation({
    kind: "po",
    currentId: poNo,
    listQueryKey: "pending-list",
    fallbackApiPath: (username) => `/api/PO/pending/${username}?amount=&filterType=gte`,
    extractId: (row) => (row.PoNo as string | undefined) ?? undefined,
  });

  useEffect(() => {
    setRemarks("");
    setRejectAttachment(null);
    setConfirm(null);
    setSelectedQuoteItemKey("");
    setSelectedSummaryLine("0");
  }, [poNo]);

  // Transform data to match original format
  const po = Array.isArray(poPayload)
    ? poPayload
    : Array.isArray(poPayload?.items)
      ? poPayload.items
      : undefined;
  const previousQuotes: any[] = Array.isArray(previousQuotesPayload)
    ? previousQuotesPayload
    : Array.isArray(poPayload?.previousQuotes)
      ? poPayload.previousQuotes
      : [];
  const poDetails = Array.isArray(po) ? po[0] : po;
  const indentInfo = poPayload?.indent && typeof poPayload.indent === "object"
    ? poPayload.indent
    : null;
  const indentNo =
    String(
      indentInfo?.indentNo ??
        poDetails?.indentNo ??
        poDetails?.IndentNo ??
        poDetails?.storeCode ??
        poDetails?.StoreCode ??
        "",
    ).trim() || null;
  const indentRemarksRaw =
    String(
      indentInfo?.indentRemarks ??
        poDetails?.indentRemarks ??
        poDetails?.IndentRemarks ??
        "",
    ).trim() || null;
  const indentPurposeText =
    String(
      indentInfo?.indentPurpose ??
        poDetails?.indentPurpose ??
        poDetails?.IndentPurpose ??
        "",
    ).trim() || null;
  const indentRemarks = indentRemarksRaw || indentPurposeText;
  const approval = approvalData;
  const workflow = Array.isArray(workflowData) ? workflowData : [];
  const loading = poLoading;

  // Dynamically load PDF.js from CDN and fetch PDF buffer (after header data is ready)
  useEffect(() => {
    if (poLoading || !poDetails) return;

    let isMounted = true;

    const loadPdf = async () => {
      try {
        setPdfLoading(true);
        setPdfError(null);

        if (!(window as any).pdfjsLib) {
          await new Promise<void>((resolve, reject) => {
            const script = document.createElement("script");
            script.src = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.4.120/pdf.min.js";
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Failed to load PDF engine"));
            document.head.appendChild(script);
          });
        }

        const pdfjsLib = (window as any).pdfjsLib;
        pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.4.120/pdf.worker.min.js";

        const pdfUrl = getApiUrl(`/api/pdf?poNo=${encodeURIComponent(poNo)}`);
        const response = await fetch(pdfUrl);
        if (!response.ok) {
          throw new Error(`Failed to load PDF (${response.status})`);
        }
        const arrayBuffer = await response.arrayBuffer();

        const loadingTask = pdfjsLib.getDocument({ data: arrayBuffer });
        const doc = await loadingTask.promise;

        if (isMounted) {
          setPdfDoc(doc);
          setNumPages(doc.numPages);
          setPdfLoading(false);
        }
      } catch (err: any) {
        console.error("PDF load error:", err);
        if (isMounted) {
          setPdfError(err.message || "Failed to parse PDF document");
          setPdfLoading(false);
        }
      }
    };

    loadPdf();

    return () => {
      isMounted = false;
    };
  }, [poNo, poLoading, poDetails?.PurchaseCode]);

  // Automatically adjust scale to fit container width on load
  useEffect(() => {
    if (!pdfDoc || !containerRef.current) return;
    
    const adjustScale = async () => {
      try {
        const page = await pdfDoc.getPage(1);
        const viewport = page.getViewport({ scale: 1.0 });
        const containerWidth = containerRef.current!.clientWidth;
        
        // Subtract padding/border spacing
        const paddedWidth = containerWidth - 24;
        let optimalScale = Number((paddedWidth / viewport.width).toFixed(2));
        
        if (window.innerWidth < 768) {
          optimalScale = 0.5;
        }
        
        // Ensure scale is within acceptable bounds (0.5 to 2.0)
        setScale(Math.max(0.5, Math.min(optimalScale, 2.0)));
      } catch (err) {
        console.error("Error adjusting scale:", err);
      }
    };
    
    const timeoutId = setTimeout(() => {
      adjustScale();
    }, 100);
    
    return () => clearTimeout(timeoutId);
  }, [pdfDoc]);



  const handlePrevPage = () => {
    if (pageNum <= 1) return;
    setPageNum(pageNum - 1);
  };

  const handleNextPage = () => {
    if (pdfDoc && pageNum >= pdfDoc.numPages) return;
    setPageNum(pageNum + 1);
  };

  const handleZoomIn = () => {
    setScale((prev) => Math.min(prev + 0.2, 2.2));
  };

  const handleZoomOut = () => {
    setScale((prev) => Math.max(prev - 0.2, 0.5));
  };
  if (loading) {
    return (
      <div className="space-y-5 pb-action">
        <SkeletonCard />
        <div className="grid gap-5 lg:grid-cols-3">
          <div className="space-y-5 lg:col-span-2">
            <SkeletonSection title="Purchase Order Summary" />
            <SkeletonSection title="Purchase Order Document" />
            <SkeletonSection title="Remarks" />
          </div>
          <div>
            <SkeletonSection title="Approval Workflow" />
          </div>
        </div>
      </div>
    );
  }

  if (!po) {
    return (
      <div className="rounded-xl border border-border bg-card p-8 text-center">
        <p>PO not found.</p>
        <Link
          to="/pending"
          className="mt-3 inline-block text-sm font-medium text-primary hover:underline"
        >
          Back to list
        </Link>
      </div>
    );
  }

  async function handleApprove() {
    if (isApproving) return; // Prevent double-click
    
    setIsApproving(true);
    try {
      const transId = approval?.TransId ?? approval?.Transid;
      if (!transId) {
        toast.error("Approval transaction ID not found");
        return;
      }
      
      const response = await fetch(
        getApiUrl(`/api/PO/approve/${transId}`),
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            remarks: remarks,
          }),
        }
      );
      
      if (!response.ok) {
        throw new Error("Failed to approve PO");
      }
      
      toast.success("PO approved successfully");

      const nextPoNo = resolveNextAfterApproval(poNo, queryClient, {
        kind: "po",
        listQueryKey: "pending-list",
        extractId: (row) => (row.PoNo as string | undefined) ?? undefined,
      });
      invalidateApprovalCaches(queryClient, "pending-list");

      if (nextPoNo) {
        navigate({ to: "/po/$poNo", params: { poNo: nextPoNo } });
      } else {
        navigate({ to: "/pending" });
      }
    } catch (err) {
      console.error(err);
      toast.error("Operation failed");
    } finally {
      setIsApproving(false);
    }
  }

  async function handleConfirm() {
    if (confirm !== "reject") return;
    
    if (!remarks.trim()) {
      toast.error("Remarks are mandatory for rejection");
      return;
    }

    if (isRejecting) return; // Prevent double-click
    
    setIsRejecting(true);
    try {
      const transId = approval?.TransId ?? approval?.Transid;
      if (!transId) {
        toast.error("Approval transaction ID not found");
        return;
      }
      
      const form = new FormData();
      form.append("remarks", remarks.trim());
      if (rejectAttachment) form.append("attachment", rejectAttachment);

      const response = await fetch(
        getApiUrl(`/api/PO/reject/${transId}`),
        {
          method: "POST",
          body: form,
        }
      );
      
      if (!response.ok) {
        const err = await response.json().catch(() => null);
        throw new Error((err as { error?: string } | null)?.error ?? "Failed to reject PO");
      }

      toast.success("PO rejected successfully");
      
      setConfirm(null);
      setRejectAttachment(null);

      const nextPoNo = resolveNextAfterApproval(poNo, queryClient, {
        kind: "po",
        listQueryKey: "pending-list",
        extractId: (row) => (row.PoNo as string | undefined) ?? undefined,
      });
      invalidateApprovalCaches(queryClient, "pending-list");

      if (nextPoNo) {
        navigate({ to: "/po/$poNo", params: { poNo: nextPoNo } });
      } else {
        navigate({ to: "/pending" });
      }
    } catch (err) {
      console.error(err);
      toast.error("Operation failed");
    } finally {
      setIsRejecting(false);
    }
  }

  const grandTotal = Number(poDetails?.TotalAmount || 0);
  const currency = poDetails?.Currency;
  const curLabel = currencyLabel(currency);

  function formatQuoteDate(value?: string | null) {
    if (!value) return "—";
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return "—";
    return d.toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });
  }

  function quotesForItem(item: any) {
    const code = String(item.ItemCode ?? "").trim().toLowerCase();
    const desc = String(item.ItemDesc ?? "").trim().toLowerCase();
    return previousQuotes.filter((q) => {
      const qCode = String(q.ItemCode ?? "").trim().toLowerCase();
      const qDesc = String(q.ItemDesc ?? "").trim().toLowerCase();
      if (code && qCode) return qCode === code;
      if (!code && desc && qDesc) return qDesc === desc;
      if (code && qDesc && !qCode) return qDesc === desc;
      return false;
    });
  }

  // One dropdown option per distinct item code (fallback to description)
  const quoteItemOptions = (() => {
    const lines = Array.isArray(po) ? po : [];
    const seen = new Set<string>();
    const options: { key: string; label: string; item: any; quoteCount: number }[] = [];
    for (const item of lines) {
      const code = String(item.ItemCode ?? "").trim();
      const desc = String(item.ItemDesc ?? "").trim();
      const key = code
        ? `code:${code.toLowerCase()}`
        : `desc:${desc.toLowerCase()}`;
      if (!key || key.endsWith(":") || seen.has(key)) continue;
      seen.add(key);
      const quotes = quotesForItem(item);
      const shortDesc = desc.length > 48 ? `${desc.slice(0, 48)}…` : desc;
      options.push({
        key,
        label: code ? `${code} — ${shortDesc || "Item"}` : shortDesc || "Item",
        item,
        quoteCount: quotes.length,
      });
    }
    return options;
  })();

  const activeQuoteKey =
    selectedQuoteItemKey && quoteItemOptions.some((o) => o.key === selectedQuoteItemKey)
      ? selectedQuoteItemKey
      : quoteItemOptions[0]?.key ?? "";

  const activeQuoteOption = quoteItemOptions.find((o) => o.key === activeQuoteKey);
  const activeQuotes = activeQuoteOption ? quotesForItem(activeQuoteOption.item) : [];

  const poLines = Array.isArray(po) ? po : [];
  const summaryLineIndex = Math.min(
    Math.max(0, Number.parseInt(selectedSummaryLine, 10) || 0),
    Math.max(0, poLines.length - 1),
  );
  const summaryItem = poLines[summaryLineIndex];

  const headerItems = [
    { icon: Hash, label: "PO Number", value: poDetails.PurchaseCode },
    { icon: Building2, label: "Vendor", value: poDetails.FirmName },
    { icon: Calendar, label: "PO Date", value: poDetails.deliverydate },
    { icon: Briefcase, label: "Department", value: poDetails.DepttName },
    { icon: UserIcon, label: "Requested By", value: poDetails.FirmName },
    {
      icon: ClipboardList,
      label: "Indent Number",
      value: indentNo || "—",
      linkTo: indentNo ? (`/indent/${encodeURIComponent(indentNo)}` as const) : undefined,
    },
    {
      icon: IndianRupee,
      label: "PO Amount",
      value: formatMoney(grandTotal, currency),
      strong: true,
    },
  ];

  return (
    <div className="space-y-5 pb-action max-w-full overflow-x-hidden">
      <ApprovalDetailNav
        onBack={() => navigate({ to: "/pending" })}
        navigation={poNavigation}
        onPrevious={() =>
          poNavigation.prev &&
          navigate({ to: "/po/$poNo", params: { poNo: poNavigation.prev } })
        }
        onNext={() =>
          poNavigation.next &&
          navigate({ to: "/po/$poNo", params: { poNo: poNavigation.next } })
        }
        trailing={<StatusBadge status="Pending" />}
      />

      {/* Header card */}
      <div className="rounded-2xl border border-border bg-card p-5 shadow-soft">
        <div className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">Purchase Order</div>
       <h1 className="text-xl font-semibold tracking-tight md:text-2xl">
  {poDetails.PurchaseCode}
</h1>
        <p className="mt-1 text-sm text-muted-foreground">
  {poDetails.FirmName}
</p>
        <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 border-t border-border pt-4 md:grid-cols-3">
          {headerItems.map((h) => (
            <div key={h.label} className="flex items-start gap-2.5">
              <h.icon className="mt-0.5 h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <div className="min-w-0">
                <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{h.label}</div>
                {"linkTo" in h && h.linkTo && indentNo ? (
                  <Link
                    to="/indent/$indentNo"
                    params={{ indentNo }}
                    className={`truncate text-sm text-primary underline-offset-2 hover:underline ${h.strong ? "font-semibold tabular-nums" : "font-medium"}`}
                  >
                    {h.value}
                  </Link>
                ) : (
                  <div className={`truncate text-sm ${h.strong ? "font-semibold tabular-nums" : "font-medium"}`}>{h.value}</div>
                )}
              </div>
            </div>
          ))}
        </div>
        {(indentRemarks || indentPurposeText) && (
          <div className="mt-4 space-y-2 border-t border-border pt-4">
            {indentRemarks ? (
              <div className="flex items-start gap-2.5">
                <FileText className="mt-0.5 h-4 w-4 flex-shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <div className="text-[11px] uppercase tracking-wide text-muted-foreground">
                    {indentRemarksRaw ? "Indent Remarks" : "Indent Purpose"}
                  </div>
                  <p className="mt-0.5 whitespace-pre-wrap text-sm font-medium leading-snug">
                    {indentRemarks}
                  </p>
                </div>
              </div>
            ) : null}
            {indentRemarksRaw && indentPurposeText && indentPurposeText !== indentRemarksRaw ? (
              <div className="flex items-start gap-2.5">
                <ClipboardList className="mt-0.5 h-4 w-4 flex-shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Indent Purpose</div>
                  <p className="mt-0.5 whitespace-pre-wrap text-sm font-medium leading-snug">
                    {indentPurposeText}
                  </p>
                </div>
              </div>
            ) : null}
          </div>
        )}
      </div>

      <div className="grid gap-5 lg:grid-cols-3 min-w-0 w-full">
        <div className="space-y-5 lg:col-span-2 min-w-0 w-full">
          {/* Section A: Summary */}
          <Section title="Purchase Order Summary">
            <p className="hidden text-sm text-muted-foreground md:block">
              {poDetails.ItemDesc}
            </p>

            {/* Mobile: one item card via dropdown */}
            <div className="mt-4 space-y-3 md:hidden">
              {poLines.length === 0 ? (
                <p className="text-sm text-muted-foreground">No items on this PO.</p>
              ) : (
                <>
                  <div className="space-y-1.5">
                    <Label htmlFor="po-summary-item">Item line</Label>
                    <Select
                      value={String(summaryLineIndex)}
                      onValueChange={setSelectedSummaryLine}
                    >
                      <SelectTrigger id="po-summary-item" className="h-11 w-full bg-background">
                        <SelectValue placeholder="Select an item" />
                      </SelectTrigger>
                      <SelectContent className="max-h-[50vh]">
                        {poLines.map((item: any, index: number) => {
                          const code = item.ItemCode ? `${item.ItemCode} · ` : "";
                          const desc = String(item.ItemDesc ?? "Item");
                          const short = desc.length > 36 ? `${desc.slice(0, 36)}…` : desc;
                          return (
                            <SelectItem key={index} value={String(index)}>
                              {index + 1}. {code}{short} · Qty {item.Qty}
                            </SelectItem>
                          );
                        })}
                      </SelectContent>
                    </Select>
                  </div>

                  {summaryItem ? (
                    <div className="overflow-hidden rounded-xl border border-border">
                      <div className="space-y-1.5 px-3 py-3">
                        {summaryItem.ItemCode ? (
                          <div className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                            {summaryItem.ItemCode}
                          </div>
                        ) : null}
                        <div className="text-sm font-medium leading-snug">{summaryItem.ItemDesc}</div>
                        <div className="text-xs text-muted-foreground">
                          Qty {summaryItem.Qty}
                          <span className="mx-1.5 text-border">·</span>
                          Rate {formatMoneyAmount(summaryItem.Rate)} {curLabel}
                        </div>
                        <div className="flex items-baseline justify-between gap-3 pt-0.5">
                          <span className="text-[11px] uppercase tracking-wide text-muted-foreground">
                            Amount
                          </span>
                          <span className="text-sm font-semibold tabular-nums">
                            {formatMoneyAmount(summaryItem.Total)}
                          </span>
                        </div>
                      </div>
                      <div className="flex items-baseline justify-between gap-3 border-t border-border bg-secondary/30 px-3 py-2.5">
                        <span className="text-xs font-medium uppercase text-muted-foreground">
                          PO total ({poLines.length} lines)
                        </span>
                        <span className="text-sm font-semibold tabular-nums">
                          {formatMoneyAmount(grandTotal)}
                        </span>
                      </div>
                    </div>
                  ) : null}
                </>
              )}
            </div>

            {/* Desktop: table */}
            <div className="mt-4 hidden overflow-x-auto rounded-xl border border-border md:block">
              <table className="w-full text-sm">
                <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 font-medium">Item</th>
                    <th className="px-3 py-2 text-right font-medium">Qty</th>
                    <th className="px-3 py-2 text-right font-medium">Rate({curLabel})</th>
                    <th className="px-3 py-2 text-right font-medium">Amount({curLabel})</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {po.map((item: any, index: number) => (
                    <tr key={index}>
                      <td className="px-3 py-2">
                        {item.ItemCode ? (
                          <div className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                            {item.ItemCode}
                          </div>
                        ) : null}
                        <div>{item.ItemDesc}</div>
                      </td>
                      <td className="px-3 py-2 text-right">{item.Qty}</td>
                      <td className="px-3 py-2 text-right">
                        {formatMoneyAmount(item.Rate)}
                      </td>
                      <td className="px-3 py-2 text-right">
                        {formatMoneyAmount(item.Total)}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-secondary/30">
                  <tr>
                    <td colSpan={3} className="px-3 py-2 text-right text-xs font-medium uppercase text-muted-foreground">Total</td>
                    <td className="px-3 py-2 text-right font-semibold tabular-nums">
                      {formatMoneyAmount(grandTotal)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </Section>

          {/* Separate section: previous vendor quotes for the same items */}
          <Section title="Previous vendor quotes">
            <p className="text-sm text-muted-foreground">
              Pick an item to see earlier vendor quotes and previous PO rates (rate per unit).
            </p>

            {quoteItemOptions.length === 0 ? (
              <p className="mt-3 text-sm text-muted-foreground">No items on this PO.</p>
            ) : (
              <div className="mt-4 space-y-3">
                <div className="space-y-1.5">
                  <Label htmlFor="po-quote-item">Item</Label>
                  <Select
                    value={activeQuoteKey}
                    onValueChange={setSelectedQuoteItemKey}
                  >
                    <SelectTrigger id="po-quote-item" className="h-11 w-full bg-background">
                      <SelectValue placeholder="Select an item" />
                    </SelectTrigger>
                    <SelectContent className="max-h-[50vh]">
                      {quoteItemOptions.map((opt) => (
                        <SelectItem key={opt.key} value={opt.key}>
                          {opt.label} ({opt.quoteCount})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {activeQuoteOption ? (
                  <div className="overflow-hidden rounded-xl border border-border">
                    <div className="border-b border-border bg-secondary/30 px-3 py-2.5">
                      <div className="text-sm font-medium leading-snug">
                        {activeQuoteOption.item.ItemDesc}
                      </div>
                      <div className="mt-0.5 text-xs text-muted-foreground">
                        This PO: {formatMoneyAmount(activeQuoteOption.item.Rate)} {curLabel}
                        {activeQuoteOption.item.Qty != null
                          ? ` · Qty ${activeQuoteOption.item.Qty}`
                          : ""}
                        {activeQuoteOption.item.FirmName
                          ? ` · ${activeQuoteOption.item.FirmName}`
                          : ""}
                      </div>
                    </div>

                    {activeQuotes.length === 0 ? (
                      <p className="px-3 py-3 text-xs text-muted-foreground">
                        No earlier vendor quotes found for this item.
                      </p>
                    ) : (
                      <>
                        <ul className="divide-y divide-border md:hidden">
                          {activeQuotes.map((q: any, qi: number) => {
                            const unitLabel = q.Unit ? String(q.Unit).trim() : "unit";
                            return (
                              <li key={qi} className="space-y-1 px-3 py-2.5">
                                <div className="text-sm font-medium leading-snug">
                                  {q.Vendor?.trim() || "Unknown vendor"}
                                </div>
                                <div className="text-xs text-muted-foreground">
                                  Rate{" "}
                                  <span className="font-semibold text-foreground tabular-nums">
                                    {q.Rate != null
                                      ? `${formatMoneyAmount(q.Rate)} ${curLabel} / ${unitLabel}`
                                      : "—"}
                                  </span>
                                  {q.Qty != null ? (
                                    <>
                                      <span className="mx-1.5 text-border">·</span>
                                      Qty {q.Qty}
                                      {q.Unit ? ` ${String(q.Unit).trim()}` : ""}
                                    </>
                                  ) : null}
                                  <span className="mx-1.5 text-border">·</span>
                                  {formatQuoteDate(q.QuotedOn)}
                                  {q.Source ? (
                                    <>
                                      <span className="mx-1.5 text-border">·</span>
                                      {q.Source}
                                    </>
                                  ) : null}
                                </div>
                              </li>
                            );
                          })}
                        </ul>

                        <div className="hidden overflow-x-auto md:block">
                          <table className="w-full text-sm">
                            <thead className="bg-secondary/20 text-left text-xs uppercase tracking-wide text-muted-foreground">
                              <tr>
                                <th className="px-3 py-2 font-medium">Vendor</th>
                                <th className="px-3 py-2 text-right font-medium">Rate (per unit)</th>
                                <th className="px-3 py-2 text-right font-medium">Qty</th>
                                <th className="px-3 py-2 font-medium">Date</th>
                                <th className="px-3 py-2 font-medium">Source</th>
                              </tr>
                            </thead>
                            <tbody className="divide-y divide-border">
                              {activeQuotes.map((q: any, qi: number) => {
                                const unitLabel = q.Unit ? String(q.Unit).trim() : "unit";
                                return (
                                  <tr key={qi}>
                                    <td className="px-3 py-2 font-medium">
                                      {q.Vendor?.trim() || "—"}
                                    </td>
                                    <td className="px-3 py-2 text-right font-semibold tabular-nums">
                                      {q.Rate != null
                                        ? `${formatMoneyAmount(q.Rate)} / ${unitLabel}`
                                        : "—"}
                                    </td>
                                    <td className="px-3 py-2 text-right tabular-nums">
                                      {q.Qty != null
                                        ? `${q.Qty}${q.Unit ? ` ${String(q.Unit).trim()}` : ""}`
                                        : "—"}
                                    </td>
                                    <td className="px-3 py-2 whitespace-nowrap">
                                      {formatQuoteDate(q.QuotedOn)}
                                    </td>
                                    <td className="px-3 py-2 text-xs text-muted-foreground">
                                      {q.Source || "—"}
                                      {q.PreviousPoNo ? ` · ${q.PreviousPoNo}` : ""}
                                    </td>
                                  </tr>
                                );
                              })}
                            </tbody>
                          </table>
                        </div>
                      </>
                    )}
                  </div>
                ) : null}
              </div>
            )}
          </Section>

          {/* Section B: PDF */}
          <Section title="Purchase Order Document">
            {pdfLoading ? (
              <div className="flex flex-col items-center justify-center rounded-2xl border border-border bg-card/50 p-12">
                <Loader2 className="h-8 w-8 animate-spin text-primary mb-3" />
                <p className="text-sm text-muted-foreground">Loading PDF document...</p>
              </div>
            ) : pdfError ? (
              <div className="flex flex-col items-center justify-center rounded-2xl border border-destructive/20 bg-card/50 p-8 text-center">
                <XCircle className="h-8 w-8 text-destructive mb-3" />
                <h3 className="text-base font-semibold text-destructive mb-1">Failed to load PDF</h3>
                <p className="text-xs text-muted-foreground max-w-sm mb-4">{pdfError}</p>
                <a
                  href={getApiUrl(`/api/pdf?poNo=${encodeURIComponent(poNo)}`)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex h-9 items-center justify-center gap-1.5 rounded-lg bg-primary px-4 text-xs font-semibold text-primary-foreground hover:bg-primary/90 transition shadow-sm"
                >
                  <span>Open Direct Link</span>
                  <ExternalLink className="h-3.5 w-3.5" />
                </a>
              </div>
            ) : (
              <div className="flex flex-col space-y-3">
                {/* PDF Controls */}
                <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={handlePrevPage}
                      disabled={pageNum <= 1}
                      className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground hover:bg-accent disabled:opacity-50 transition"
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </button>
                    <span className="text-xs font-medium px-2">
                      Page {pageNum} of {numPages}
                    </span>
                    <button
                      onClick={handleNextPage}
                      disabled={pageNum >= numPages}
                      className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground hover:bg-accent disabled:opacity-50 transition"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                  
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={handleZoomOut}
                      disabled={scale <= 0.5}
                      className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground hover:bg-accent disabled:opacity-50 transition"
                      title="Zoom Out"
                    >
                      <ZoomOut className="h-4 w-4" />
                    </button>
                    <span className="text-xs font-medium px-1 w-10 text-center">
                      {Math.round(scale * 100)}%
                    </span>
                    <button
                      onClick={handleZoomIn}
                      disabled={scale >= 2.0}
                      className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground hover:bg-accent disabled:opacity-50 transition"
                      title="Zoom In"
                    >
                      <ZoomIn className="h-4 w-4" />
                    </button>
                    <a
                      href={getApiUrl(`/api/pdf?poNo=${encodeURIComponent(poNo)}`)}
                      download={`${poNo}.pdf`}
                      className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground hover:bg-accent transition"
                      title="Download PDF"
                    >
                      <Download className="h-4 w-4" />
                    </a>
                  </div>
                </div>

                {/* PDF Render Canvas */}
                <div ref={containerRef} className="min-h-[400px] max-h-[600px] w-full overflow-auto rounded-2xl border border-border bg-muted/20 p-2 shadow-inner">
                  <PdfCanvasViewer pdfDoc={pdfDoc} pageNum={pageNum} scale={scale} />
                </div>
              </div>
            )}
          </Section>

          <DmsAttachmentsSection purchaseCode={poNo} kind="PO" />

          {/* Section D: Remarks */}
          <Section title="Remarks">
            <RemarkComposer value={remarks} onChange={setRemarks} />
          </Section>
        </div>

        {/* Section C: Workflow */}
        <div className="lg:col-span-1 min-w-0 w-full">
         <Section title="Approval Workflow">
  <div className="space-y-3">
    {workflow.map((step: any) => (
      <div
        key={step.TransId}
        className="rounded-lg border border-border p-3"
      >
        <div className="font-medium">
          {step.ApprovalName}
        </div>

       <div
  className={`inline-block rounded px-2 py-1 text-xs font-semibold mt-2 ${
    step.Status === "Approved"
      ? "bg-green-500/20 text-green-400"
      : step.Status === "Rejected"
      ? "bg-red-500/20 text-red-400"
      : "bg-yellow-500/20 text-yellow-400"
  }`}
>
  {step.Status}
</div>

        <div className="text-sm text-muted-foreground">
          Approval Date: {step.ApprovalDate || "Pending"}
        </div>
      </div>
    ))}
  </div>
</Section>
        </div>
      </div>

      <ApprovalCommandBar
        amountLabel={formatMoney(grandTotal, currency)}
        queueLabel={
          poNavigation.total > 1 ? `${poNavigation.index} of ${poNavigation.total} in queue` : undefined
        }
        onApprove={handleApprove}
        onReject={() => setConfirm("reject")}
        isApproving={isApproving}
        isRejecting={isRejecting}
      />

      {/* Confirmation dialog */}
      {confirm === "reject" && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={() => setConfirm(null)}>
          <div className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="mb-3 inline-flex h-10 w-10 items-center justify-center rounded-full bg-destructive/15 text-destructive">
              <XCircle className="h-5 w-5" />
            </div>
            <h3 className="text-base font-semibold">Reject this PO?</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              This action will reject the PO and notify the requester.
            </p>
            {!remarks.trim() && (
              <p className="mt-2 text-xs font-medium text-destructive">Remarks are required to reject.</p>
            )}
            <label className="mt-4 block text-sm font-medium">Attachment (optional)</label>
            <input
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"
              className="mt-1 w-full text-sm file:mr-3 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-1.5 file:text-sm file:font-medium"
              onChange={(e) => setRejectAttachment(e.target.files?.[0] ?? null)}
            />
            {rejectAttachment && (
              <p className="mt-1 text-xs text-muted-foreground truncate">{rejectAttachment.name}</p>
            )}
            <div className="mt-5 flex gap-2">
              <button onClick={() => setConfirm(null)} className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary">Cancel</button>
              <button
                onClick={handleConfirm}
                disabled={isRejecting}
                className="h-10 flex-1 rounded-md text-sm font-semibold text-primary-foreground bg-destructive hover:bg-destructive/90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isRejecting ? (
                  <span className="inline-flex items-center gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Rejecting...
                  </span>
                ) : (
                  "Confirm Reject"
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-border bg-card shadow-soft">
      <header className="border-b border-border px-5 py-3">
        <h2 className="text-sm font-semibold">{title}</h2>
      </header>
      <div className="p-5">{children}</div>
    </section>
  );
}

interface PdfCanvasViewerProps {
  pdfDoc: any;
  pageNum: number;
  scale: number;
}

function PdfCanvasViewer({ pdfDoc, pageNum, scale }: PdfCanvasViewerProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const renderTaskRef = useRef<any>(null);

  useEffect(() => {
    if (!pdfDoc) return;

    let isCurrent = true;

    const renderPage = async () => {
      try {
        const page = await pdfDoc.getPage(pageNum);
        const canvas = canvasRef.current;
        if (!canvas || !isCurrent) return;

        const context = canvas.getContext("2d");
        if (!context) return;

        // Cancel previous render task if active
        if (renderTaskRef.current) {
          try {
            renderTaskRef.current.cancel();
          } catch (e) {
            // Ignore cancel exceptions
          }
          renderTaskRef.current = null;
        }

        const viewport = page.getViewport({ scale });
        const dpr = window.devicePixelRatio || 1;

        canvas.width = viewport.width * dpr;
        canvas.height = viewport.height * dpr;
        canvas.style.width = `${viewport.width}px`;
        canvas.style.height = `${viewport.height}px`;

        // Reset transform to identity matrix before scaling to prevent cumulative transforms
        context.setTransform(1, 0, 0, 1, 0, 0);
        context.scale(dpr, dpr);

        const renderContext = {
          canvasContext: context,
          viewport: viewport
        };

        const renderTask = page.render(renderContext);
        renderTaskRef.current = renderTask;

        await renderTask.promise;
      } catch (err: any) {
        // Do not log cancellation warnings
        if (err && err.name !== "RenderingCancelledException") {
          console.error("PDF render error:", err);
        }
      }
    };

    const timeoutId = setTimeout(() => {
      renderPage();
    }, 50);

    return () => {
      isCurrent = false;
      clearTimeout(timeoutId);
      if (renderTaskRef.current) {
        try {
          renderTaskRef.current.cancel();
        } catch (e) {}
      }
    };
  }, [pdfDoc, pageNum, scale]);

  return (
    <canvas ref={canvasRef} className="mx-auto block shadow-md border border-border/30 bg-white" />
  );
}


