import { useAuth } from "@/lib/auth-context";
import { getApiUrl } from "@/lib/api-config";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState, useEffect, useRef } from "react";
import { ArrowLeft, FileText, Download, CheckCircle2, XCircle, Building2, Calendar, User as UserIcon, Hash, IndianRupee, Briefcase, ExternalLink, ChevronLeft, ChevronRight, ZoomIn, ZoomOut, Loader2 } from "lucide-react";
import { currencyLabel, formatMoney, formatMoneyAmount, type ApprovalStep } from "@/lib/mock-data";
import { StatusBadge } from "@/components/StatusBadge";
import { toast } from "sonner";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { SkeletonCard, SkeletonSection, SkeletonTable, SkeletonWorkflow } from "@/components/SkeletonLoader";
import { ApprovalDetailNav } from "@/components/ApprovalDetailNav";
import { useApprovalListNavigation } from "@/hooks/use-approval-list-navigation";
import { invalidateApprovalCaches, resolveNextAfterApproval } from "@/lib/approval-after-action";

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

  // Fetch PO details with React Query (cache indefinitely - immutable data)
  const { data: poData, isLoading: poLoading } = useQuery({
    queryKey: ['po-details', poNo],
    queryFn: async () => {
      const response = await fetch(getApiUrl(`/api/PO/details?poNo=${encodeURIComponent(poNo)}`));
      if (!response.ok) throw new Error('Failed to fetch PO details');
      return response.json();
    },
    staleTime: Infinity, // Cache indefinitely
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
  }, [poNo]);

  // Transform data to match original format
  const po = poData;
  const poDetails = Array.isArray(po) ? po[0] : po;
  const approval = approvalData;
  const workflow = Array.isArray(workflowData) ? workflowData : [];
  const loading = poLoading;

  // Dynamically load PDF.js from CDN and fetch PDF buffer
  useEffect(() => {
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
  }, [poNo]);

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
      <div className="space-y-5 pb-24 md:pb-0">
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

  const headerItems = [
    { icon: Hash, label: "PO Number", value: poDetails.PurchaseCode },
    { icon: Building2, label: "Vendor", value: poDetails.FirmName },
    { icon: Calendar, label: "PO Date", value: poDetails.deliverydate },
    { icon: Briefcase, label: "Department", value: poDetails.DepttName },
    { icon: UserIcon, label: "Requested By", value: poDetails.FirmName },
    {
      icon: IndianRupee,
      label: "PO Amount",
      value: formatMoney(grandTotal, currency),
      strong: true,
    },
  ];

  return (
    <div className="space-y-5 pb-24 md:pb-0 max-w-full overflow-x-hidden">
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
      <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
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
                <div className={`truncate text-sm ${h.strong ? "font-semibold tabular-nums" : "font-medium"}`}>{h.value}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="grid gap-5 lg:grid-cols-3 min-w-0 w-full">
        <div className="space-y-5 lg:col-span-2 min-w-0 w-full">
          {/* Section A: Summary */}
          <Section title="Purchase Order Summary">
            <p className="hidden text-sm text-muted-foreground md:block">
              {poDetails.ItemDesc}
            </p>

            {/* Mobile: stacked item rows */}
            <div className="mt-4 overflow-hidden rounded-lg border border-border md:hidden">
              <ul className="divide-y divide-border">
                {po.map((item: any, index: number) => (
                  <li key={index} className="space-y-1.5 px-3 py-3">
                    <div className="text-sm font-medium leading-snug">{item.ItemDesc}</div>
                    <div className="text-xs text-muted-foreground">
                      Qty {item.Qty}
                      <span className="mx-1.5 text-border">·</span>
                      Rate {formatMoneyAmount(item.Rate)} {currencyLabel(currency)}
                    </div>
                    <div className="flex items-baseline justify-between gap-3 pt-0.5">
                      <span className="text-[11px] uppercase tracking-wide text-muted-foreground">Amount</span>
                      <span className="text-sm font-semibold tabular-nums">
                        {formatMoneyAmount(item.Total)}
                      </span>
                    </div>
                  </li>
                ))}
              </ul>
              <div className="flex items-baseline justify-between gap-3 border-t border-border bg-secondary/30 px-3 py-2.5">
                <span className="text-xs font-medium uppercase text-muted-foreground">Total</span>
                <span className="text-sm font-semibold tabular-nums">
                  {formatMoneyAmount(grandTotal)}
                </span>
              </div>
            </div>

            {/* Desktop: table */}
            <div className="mt-4 hidden overflow-x-auto rounded-lg border border-border md:block">
              <table className="w-full text-sm">
                <thead className="bg-secondary/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 font-medium">Item</th>
                    <th className="px-3 py-2 text-right font-medium">Qty</th>
                    <th className="px-3 py-2 text-right font-medium">Rate({currencyLabel(currency)})</th>
                    <th className="px-3 py-2 text-right font-medium">Amount({currencyLabel(currency)})</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {po.map((item: any, index: number) => (
                    <tr key={index}>
                      <td className="px-3 py-2">{item.ItemDesc}</td>
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

          {/* Section B: PDF */}
          <Section title="Purchase Order Document">
            {pdfLoading ? (
              <div className="flex flex-col items-center justify-center p-12 bg-card/50 rounded-xl border border-border">
                <Loader2 className="h-8 w-8 animate-spin text-primary mb-3" />
                <p className="text-sm text-muted-foreground">Loading PDF document...</p>
              </div>
            ) : pdfError ? (
              <div className="flex flex-col items-center justify-center p-8 bg-card/50 rounded-xl border border-destructive/20 text-center">
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
                <div ref={containerRef} className="w-full overflow-auto bg-muted/20 border border-border rounded-xl p-2 min-h-[400px] max-h-[600px] shadow-inner">
                  <PdfCanvasViewer pdfDoc={pdfDoc} pageNum={pageNum} scale={scale} />
                </div>
              </div>
            )}
          </Section>

          {/* Section D: Remarks */}
          <Section title="Remarks">
            <textarea
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              rows={4}
              placeholder="Add remarks (mandatory for rejection)…"
              className="w-full resize-none rounded-md border border-input bg-surface p-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
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

      {/* Action buttons */}
      <div className="fixed inset-x-0 bottom-16 z-20 border-t border-border bg-surface/95 p-3 backdrop-blur md:static md:border-0 md:bg-transparent md:p-0">
        <div className="mx-auto flex max-w-7xl gap-2 md:justify-end">
          <button
            onClick={() => navigate({ to: "/pending" })}
            className="hidden h-11 flex-1 rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary md:inline-flex md:flex-none md:items-center"
          >
            Back
          </button>
          <button
            disabled={isRejecting || isApproving}
            onClick={() => setConfirm("reject")}
            className="h-11 flex-1 rounded-md border border-destructive/30 bg-destructive/10 px-4 text-sm font-semibold text-destructive hover:bg-destructive/20 disabled:opacity-50 disabled:cursor-not-allowed md:flex-none md:px-6"
          >
            {isRejecting ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Rejecting...
              </span>
            ) : (
              "Reject"
            )}
          </button>
          <button
            disabled={isApproving || isRejecting}
            onClick={handleApprove}
            className="h-11 flex-1 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed md:flex-none md:px-6"
          >
            {isApproving ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Approving...
              </span>
            ) : (
              "Approve"
            )}
          </button>
        </div>
      </div>

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
    <section className="rounded-xl border border-border bg-card">
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


