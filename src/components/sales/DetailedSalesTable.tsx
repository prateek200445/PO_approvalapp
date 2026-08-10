import { useMemo, useState } from "react";
import type { DetailedSalesAnalysisItem } from "@/lib/sales-dashboard-types";
import {
  formatSalesCurrency,
  formatSalesQuantity,
  formatSalesRate,
} from "@/lib/sales-dashboard-api";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { FileDown, FileSpreadsheet } from "lucide-react";

interface DetailedSalesTableProps {
  rows: DetailedSalesAnalysisItem[];
}

const PAGE_SIZES = [10, 25, 50] as const;

export function DetailedSalesTable({ rows }: DetailedSalesTableProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<(typeof PAGE_SIZES)[number]>(10);

  const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
  const safePage = Math.min(page, totalPages);

  const pageRows = useMemo(() => {
    const start = (safePage - 1) * pageSize;
    return rows.slice(start, start + pageSize);
  }, [rows, safePage, pageSize]);

  const from = rows.length === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const to = Math.min(safePage * pageSize, rows.length);

  return (
    <section className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
      <header className="flex flex-col gap-3 border-b border-border px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-sm font-semibold">Detailed Sales Analysis</h2>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="border-success/30 text-success hover:bg-success/10"
            aria-label="Export Excel (coming soon)"
            onClick={() => undefined}
          >
            <FileSpreadsheet className="h-4 w-4" />
            Export Excel
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="border-destructive/30 text-destructive hover:bg-destructive/10"
            aria-label="Export PDF (coming soon)"
            onClick={() => undefined}
          >
            <FileDown className="h-4 w-4" />
            Export PDF
          </Button>
        </div>
      </header>

      {rows.length === 0 ? (
        <div className="px-4 py-10 text-center text-sm text-muted-foreground">
          No detailed analysis rows for the selected filters.
        </div>
      ) : (
        <>
          <div className="w-full overflow-x-auto">
            <Table className="min-w-[960px]">
              <TableHeader>
                <TableRow>
                  <TableHead>Group Name</TableHead>
                  <TableHead>Sub Group Name</TableHead>
                  <TableHead>Product Name</TableHead>
                  <TableHead>Inter/Group</TableHead>
                  <TableHead>Sales/Purchase</TableHead>
                  <TableHead className="text-right">Quantity (Kg)</TableHead>
                  <TableHead className="text-right">Amount (₹)</TableHead>
                  <TableHead className="text-right">Per Kg Rate (₹)</TableHead>
                  <TableHead className="text-right">GST Amount (₹)</TableHead>
                  <TableHead className="text-right">Net Amount (₹)</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pageRows.map((row) => (
                  <TableRow key={row.id}>
                    <TableCell>{row.groupName}</TableCell>
                    <TableCell>{row.subGroupName}</TableCell>
                    <TableCell className="font-medium">{row.productName}</TableCell>
                    <TableCell>{row.interGroup}</TableCell>
                    <TableCell>{row.salesPurchase}</TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatSalesQuantity(row.quantity)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatSalesCurrency(row.amount)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatSalesRate(row.perKgRate)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatSalesCurrency(row.gstAmount)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatSalesCurrency(row.netAmount)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <footer className="flex flex-col gap-3 border-t border-border px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-xs text-muted-foreground">
              Showing {from} to {to} of {rows.length} entries
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <Select
                value={String(pageSize)}
                onValueChange={(v) => {
                  setPageSize(Number(v) as (typeof PAGE_SIZES)[number]);
                  setPage(1);
                }}
              >
                <SelectTrigger className="h-8 w-[88px] bg-background text-xs" aria-label="Rows per page">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZES.map((size) => (
                    <SelectItem key={size} value={String(size)}>
                      {size}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={safePage <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                aria-label="Previous page"
              >
                Previous
              </Button>
              <span className="text-xs tabular-nums text-muted-foreground px-1">
                {safePage} / {totalPages}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={safePage >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                aria-label="Next page"
              >
                Next
              </Button>
            </div>
          </footer>
        </>
      )}
    </section>
  );
}
