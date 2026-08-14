import { useEffect, useRef, useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Download,
  Loader2,
  XCircle,
  ZoomIn,
  ZoomOut,
} from "lucide-react";

interface PdfJsViewerProps {
  pdfUrl: string;
  downloadFileName: string;
  minHeightClass?: string;
}

function PdfCanvasViewer({
  pdfDoc,
  pageNum,
  scale,
}: {
  pdfDoc: any;
  pageNum: number;
  scale: number;
}) {
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

        if (renderTaskRef.current) {
          try {
            renderTaskRef.current.cancel();
          } catch {
            // ignore cancel errors
          }
          renderTaskRef.current = null;
        }

        const viewport = page.getViewport({ scale });
        const dpr = window.devicePixelRatio || 1;

        canvas.width = viewport.width * dpr;
        canvas.height = viewport.height * dpr;
        canvas.style.width = `${viewport.width}px`;
        canvas.style.height = `${viewport.height}px`;

        context.setTransform(1, 0, 0, 1, 0, 0);
        context.scale(dpr, dpr);

        const renderTask = page.render({
          canvasContext: context,
          viewport,
        });
        renderTaskRef.current = renderTask;
        await renderTask.promise;
      } catch (err: unknown) {
        if (err && typeof err === "object" && "name" in err && err.name !== "RenderingCancelledException") {
          console.error("PDF render error:", err);
        }
      }
    };

    const timeoutId = setTimeout(renderPage, 50);

    return () => {
      isCurrent = false;
      clearTimeout(timeoutId);
      if (renderTaskRef.current) {
        try {
          renderTaskRef.current.cancel();
        } catch {
          // ignore
        }
      }
    };
  }, [pdfDoc, pageNum, scale]);

  return (
    <canvas ref={canvasRef} className="mx-auto block border border-border/30 bg-white shadow-md" />
  );
}

export function PdfJsViewer({
  pdfUrl,
  downloadFileName,
  minHeightClass = "min-h-[400px] max-h-[600px]",
}: PdfJsViewerProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [pdfDoc, setPdfDoc] = useState<any>(null);
  const [pageNum, setPageNum] = useState(1);
  const [numPages, setNumPages] = useState(0);
  const [scale, setScale] = useState(() =>
    typeof window !== "undefined" && window.innerWidth < 768 ? 0.5 : 1.1,
  );
  const [pdfLoading, setPdfLoading] = useState(true);
  const [pdfError, setPdfError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadPdf = async () => {
      try {
        setPdfLoading(true);
        setPdfError(null);
        setPdfDoc(null);
        setPageNum(1);
        setNumPages(0);

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
        pdfjsLib.GlobalWorkerOptions.workerSrc =
          "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.4.120/pdf.worker.min.js";

        const response = await fetch(pdfUrl);
        if (!response.ok) {
          throw new Error(`Failed to load PDF (${response.status})`);
        }

        const arrayBuffer = await response.arrayBuffer();
        const doc = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;

        if (isMounted) {
          setPdfDoc(doc);
          setNumPages(doc.numPages);
          setPdfLoading(false);
        }
      } catch (err: unknown) {
        if (isMounted) {
          setPdfError(err instanceof Error ? err.message : "Failed to load PDF document");
          setPdfLoading(false);
        }
      }
    };

    void loadPdf();

    return () => {
      isMounted = false;
    };
  }, [pdfUrl]);

  useEffect(() => {
    if (!pdfDoc || !containerRef.current) return;

    const adjustScale = async () => {
      try {
        const page = await pdfDoc.getPage(1);
        const viewport = page.getViewport({ scale: 1.0 });
        const containerWidth = containerRef.current!.clientWidth;
        const paddedWidth = containerWidth - 24;
        let optimalScale = Number((paddedWidth / viewport.width).toFixed(2));

        if (window.innerWidth < 768) {
          optimalScale = 0.5;
        }

        setScale(Math.max(0.5, Math.min(optimalScale, 2.0)));
      } catch (err) {
        console.error("Error adjusting PDF scale:", err);
      }
    };

    const timeoutId = setTimeout(() => {
      void adjustScale();
    }, 100);

    return () => clearTimeout(timeoutId);
  }, [pdfDoc]);

  if (pdfLoading) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-border bg-card/50 p-12">
        <Loader2 className="mb-3 h-8 w-8 animate-spin text-primary" />
        <p className="text-sm text-muted-foreground">Loading PDF document…</p>
      </div>
    );
  }

  if (pdfError) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-destructive/20 bg-card/50 p-8 text-center">
        <XCircle className="mb-3 h-8 w-8 text-destructive" />
        <h3 className="mb-1 text-base font-semibold text-destructive">Failed to load PDF</h3>
        <p className="max-w-sm text-xs text-muted-foreground">{pdfError}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
        <div className="flex items-center gap-1.5">
          <button
            type="button"
            onClick={() => setPageNum((n) => Math.max(1, n - 1))}
            disabled={pageNum <= 1}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground transition hover:bg-accent disabled:opacity-50"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="px-2 text-xs font-medium">
            Page {pageNum} of {numPages}
          </span>
          <button
            type="button"
            onClick={() => setPageNum((n) => Math.min(numPages, n + 1))}
            disabled={pageNum >= numPages}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground transition hover:bg-accent disabled:opacity-50"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>

        <div className="flex items-center gap-1.5">
          <button
            type="button"
            onClick={() => setScale((prev) => Math.max(prev - 0.2, 0.5))}
            disabled={scale <= 0.5}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground transition hover:bg-accent disabled:opacity-50"
            title="Zoom out"
          >
            <ZoomOut className="h-4 w-4" />
          </button>
          <span className="w-10 px-1 text-center text-xs font-medium">{Math.round(scale * 100)}%</span>
          <button
            type="button"
            onClick={() => setScale((prev) => Math.min(prev + 0.2, 2.2))}
            disabled={scale >= 2.0}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground transition hover:bg-accent disabled:opacity-50"
            title="Zoom in"
          >
            <ZoomIn className="h-4 w-4" />
          </button>
          <a
            href={pdfUrl}
            download={downloadFileName}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-input bg-background text-foreground transition hover:bg-accent"
            title="Download PDF"
          >
            <Download className="h-4 w-4" />
          </a>
        </div>
      </div>

      <div
        ref={containerRef}
        className={`w-full overflow-auto rounded-xl border border-border bg-muted/20 p-2 shadow-inner ${minHeightClass}`}
      >
        <PdfCanvasViewer pdfDoc={pdfDoc} pageNum={pageNum} scale={scale} />
      </div>
    </div>
  );
}
