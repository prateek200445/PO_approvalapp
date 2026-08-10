import type {
  SalesBySubGroupItem,
  TopCustomer,
  TopProduct,
} from "@/lib/sales-dashboard-types";
import { formatSalesCurrency, formatSalesQuantity } from "@/lib/sales-dashboard-api";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

interface SalesSummaryTablesProps {
  topProducts: TopProduct[];
  topCustomers: TopCustomer[];
  bySubGroup: SalesBySubGroupItem[];
}

function EmptyRow({ colSpan, message = "No data available" }: { colSpan: number; message?: string }) {
  return (
    <TableRow>
      <TableCell colSpan={colSpan} className="py-6 text-center text-sm text-muted-foreground">
        {message}
      </TableCell>
    </TableRow>
  );
}

export function SalesSummaryTables({
  topProducts,
  topCustomers,
  bySubGroup,
}: SalesSummaryTablesProps) {
  return (
    <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-3">
      <section className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Top 5 Products by Sales</h2>
        </header>
        <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">#</TableHead>
              <TableHead>Product Name</TableHead>
              <TableHead className="text-right">Quantity</TableHead>
              <TableHead className="text-right">Sales (₹)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {topProducts.length === 0 ? (
              <EmptyRow colSpan={4} message="Coming soon" />
            ) : (
              topProducts.map((p) => (
                <TableRow key={p.rank}>
                  <TableCell className="tabular-nums text-muted-foreground">{p.rank}</TableCell>
                  <TableCell className="font-medium">{p.productName}</TableCell>
                  <TableCell className="text-right tabular-nums text-xs sm:text-sm">
                    {formatSalesQuantity(p.quantity)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums text-xs sm:text-sm">
                    {formatSalesCurrency(p.salesAmount)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        </div>
        <footer className="border-t border-border px-4 py-2.5">
          <button type="button" className="text-xs font-medium text-primary hover:underline">
            View All Products
          </button>
        </footer>
      </section>

      <section className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Customer (Top 5)</h2>
        </header>
        <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">#</TableHead>
              <TableHead>Customer Name</TableHead>
              <TableHead className="text-right">Sales (₹)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {topCustomers.length === 0 ? (
              <EmptyRow colSpan={3} message="Coming soon" />
            ) : (
              topCustomers.map((c) => (
                <TableRow key={c.rank}>
                  <TableCell className="tabular-nums text-muted-foreground">{c.rank}</TableCell>
                  <TableCell className="font-medium">{c.customerName}</TableCell>
                  <TableCell className="text-right tabular-nums text-xs sm:text-sm">
                    {formatSalesCurrency(c.salesAmount)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        </div>
        <footer className="border-t border-border px-4 py-2.5">
          <button type="button" className="text-xs font-medium text-primary hover:underline">
            View All Customers
          </button>
        </footer>
      </section>

      <section className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <header className="border-b border-border px-3 py-2.5 sm:px-4 sm:py-3">
          <h2 className="text-sm font-semibold">Sales by Sub Group</h2>
        </header>
        <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Sub Group Name</TableHead>
              <TableHead className="text-right">Quantity</TableHead>
              <TableHead className="text-right">Sales (₹)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {bySubGroup.length === 0 ? (
              <EmptyRow colSpan={3} />
            ) : (
              bySubGroup.map((s) => (
                <TableRow key={s.subGroupName}>
                  <TableCell className="max-w-[140px] truncate font-medium sm:max-w-none">
                    {s.subGroupName}
                  </TableCell>
                  <TableCell className="text-right tabular-nums text-xs sm:text-sm whitespace-nowrap">
                    {formatSalesQuantity(s.quantity)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums text-xs sm:text-sm whitespace-nowrap">
                    {formatSalesCurrency(s.salesAmount)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        </div>
        <footer className="border-t border-border px-4 py-2.5">
          <button type="button" className="text-xs font-medium text-primary hover:underline">
            View All Sub Groups
          </button>
        </footer>
      </section>
    </div>
  );
}
