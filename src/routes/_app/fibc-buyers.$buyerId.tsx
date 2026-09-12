import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Loader2 } from "lucide-react";
import { formatFibcDate, getFibcBuyerDetail, na } from "@/lib/fibc-buyers-api";

export const Route = createFileRoute("/_app/fibc-buyers/$buyerId")({
  head: () => ({ meta: [{ title: "FIBC Buyer Detail" }] }),
  component: FibcBuyerDetailPage,
});

type ShipmentRow = {
  id: string;
  shipmentDate?: string | null;
  shipmentReference?: string | null;
  industry?: string | null;
  productDescription?: string | null;
  hsCode?: string | null;
  quantity?: number | null;
  quantityUnit?: string | null;
  value?: number | null;
  unitPrice?: number | null;
  currency?: string | null;
  supplierName?: string | null;
  supplierCountry?: string | null;
  supplierState?: string | null;
  supplierCity?: string | null;
  supplierAddress?: string | null;
  originCountry?: string | null;
  destinationCountry?: string | null;
  portOfLoading?: string | null;
  portOfDischarge?: string | null;
  tradeDirection?: string | null;
};

function FibcBuyerDetailPage() {
  const { buyerId } = Route.useParams();
  const detailQuery = useQuery({
    queryKey: ["fibc-buyer", buyerId],
    queryFn: () => getFibcBuyerDetail(buyerId),
  });

  if (detailQuery.isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" /> Loading buyer…
      </div>
    );
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <div className="space-y-3">
        <Link to="/fibc-buyers" className="inline-flex items-center gap-1 text-sm text-primary hover:underline">
          <ArrowLeft className="h-4 w-4" /> Back
        </Link>
        <p className="text-sm text-destructive">
          {detailQuery.error instanceof Error ? detailQuery.error.message : "Buyer not found."}
        </p>
      </div>
    );
  }

  const { buyer, shipments, suppliers, aiSummary } = detailQuery.data;
  const breakdown = buyer.scoreBreakdown;
  const rows = (shipments ?? []) as ShipmentRow[];

  return (
    <div className="space-y-5 pb-6">
      <Link to="/fibc-buyers" className="inline-flex items-center gap-1 text-sm text-primary hover:underline">
        <ArrowLeft className="h-4 w-4" /> FIBC Buyers
      </Link>

      <div className="card-3d rounded-2xl p-4 sm:p-5">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
          Buyer Profile · Ex-Im
        </p>
        <h1 className="mt-1 text-xl font-semibold sm:text-2xl">{buyer.companyName}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {[buyer.country, buyer.state, buyer.city].filter(Boolean).map((x) => na(x)).join(" · ") || "—"}
        </p>
        <p className="mt-3 text-sm">{aiSummary}</p>
      </div>

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-3">
        <section className="rounded-2xl border border-border bg-card p-4 shadow-soft lg:col-span-1">
          <h2 className="text-sm font-semibold">Buyer Score: {buyer.score}/100</h2>
          <p className="mt-1 text-[11px] text-muted-foreground">Source: Calculated</p>
          <div className="mt-3 space-y-1.5 text-xs">
            <Row label="Recency" value={`${breakdown?.recency ?? 0}/20`} />
            <Row label="Frequency" value={`${breakdown?.frequency ?? 0}/20`} />
            <Row label="Volume" value={`${breakdown?.volume ?? 0}/20`} />
            <Row label="Trend" value={`${breakdown?.trend ?? 0}/15`} />
            <Row label="Supplier Activity" value={`${breakdown?.supplierActivity ?? 0}/10`} />
            <Row label="Product Relevance" value={`${breakdown?.productRelevance ?? 0}/10`} />
            <Row label="Data Confidence" value={`${breakdown?.dataConfidence ?? 0}/5`} />
          </div>
          <p className="mt-3 text-xs text-muted-foreground">{breakdown?.explanation}</p>
          <div className="mt-3 text-xs">
            Priority: <strong>{buyer.priority}</strong> · Trend: <strong>{buyer.trend}</strong>
            {buyer.isGenuine ? " · Genuine" : ""}
          </div>
        </section>

        <section className="rounded-2xl border border-border bg-card p-4 shadow-soft lg:col-span-2">
          <h2 className="text-sm font-semibold">Buyer details from Ex-Im</h2>
          <div className="mt-3 grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
            <Field label="Buyer Country" value={na(buyer.country)} />
            <Field label="Buyer State" value={na(buyer.state)} />
            <Field label="Buyer City" value={na(buyer.city)} />
            <Field label="Buyer Address" value={na(buyer.address)} />
            <Field label="Email" value={na(buyer.email)} />
            <Field label="Phone" value={na(buyer.phone)} />
            <Field label="Import Port" value={na(buyer.actualImportPort)} />
            <Field label="Shipments" value={String(buyer.shipmentCount)} />
            <Field label="First Shipment" value={formatFibcDate(buyer.firstShipment)} />
            <Field label="Latest Shipment" value={formatFibcDate(buyer.lastShipment)} />
            <Field label="Suppliers" value={String(buyer.supplierCount)} />
            <Field label="Data Quality" value={buyer.dataQuality} />
          </div>
        </section>
      </div>

      <section className="rounded-2xl border border-border bg-card shadow-soft">
        <header className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Shipment History</h2>
          <p className="text-[11px] text-muted-foreground">
            All Ex-Im Trade Analysis columns · {rows.length.toLocaleString("en-IN")} row(s)
          </p>
        </header>
        {rows.length === 0 ? (
          <p className="p-6 text-sm text-muted-foreground">No shipments linked to this buyer.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1600px] text-sm">
              <thead className="bg-secondary/40 text-left text-[11px] uppercase text-muted-foreground">
                <tr>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">Shipment ID</th>
                  <th className="px-3 py-2">HS Code</th>
                  <th className="px-3 py-2">Industry</th>
                  <th className="px-3 py-2">Product Description</th>
                  <th className="px-3 py-2">Seller</th>
                  <th className="px-3 py-2">Seller Country</th>
                  <th className="px-3 py-2">Seller State</th>
                  <th className="px-3 py-2">Seller City</th>
                  <th className="px-3 py-2">Seller Address</th>
                  <th className="px-3 py-2">Origin Port</th>
                  <th className="px-3 py-2">Destination Port</th>
                  <th className="px-3 py-2">Origin Country</th>
                  <th className="px-3 py-2">Destination Country</th>
                  <th className="px-3 py-2">Unit</th>
                  <th className="px-3 py-2">Quantity</th>
                  <th className="px-3 py-2">Value (USD)</th>
                  <th className="px-3 py-2">Unit Price</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {rows.map((s) => (
                  <tr key={s.id} className="align-top">
                    <td className="px-3 py-2 whitespace-nowrap">{formatFibcDate(s.shipmentDate)}</td>
                    <td className="px-3 py-2 text-xs whitespace-nowrap">{na(s.shipmentReference)}</td>
                    <td className="px-3 py-2 text-xs whitespace-nowrap">{na(s.hsCode)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.industry)}</td>
                    <td className="max-w-[320px] px-3 py-2 text-xs">
                      <div className="whitespace-pre-wrap break-words">{na(s.productDescription)}</div>
                    </td>
                    <td className="max-w-[180px] px-3 py-2 text-xs">{na(s.supplierName)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.supplierCountry)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.supplierState)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.supplierCity)}</td>
                    <td className="max-w-[220px] px-3 py-2 text-xs">{na(s.supplierAddress)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.portOfLoading)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.portOfDischarge)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.originCountry)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.destinationCountry)}</td>
                    <td className="px-3 py-2 text-xs">{na(s.quantityUnit)}</td>
                    <td className="px-3 py-2 text-xs tabular-nums whitespace-nowrap">
                      {s.quantity != null ? s.quantity.toLocaleString("en-US") : "—"}
                    </td>
                    <td className="px-3 py-2 text-xs tabular-nums whitespace-nowrap">
                      {s.value != null ? s.value.toLocaleString("en-US") : "—"}
                    </td>
                    <td className="px-3 py-2 text-xs tabular-nums whitespace-nowrap">
                      {s.unitPrice != null && s.unitPrice !== 0
                        ? s.unitPrice.toLocaleString("en-US")
                        : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-border bg-card shadow-soft">
        <header className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">Suppliers</h2>
        </header>
        <div className="divide-y divide-border">
          {(suppliers?.length ?? 0) === 0 ? (
            <p className="p-4 text-sm text-muted-foreground">No named sellers in Ex-Im for this buyer (often N/A).</p>
          ) : (
            suppliers.map((s: { name: string; country?: string | null; shipmentCount: number }) => (
              <div key={s.name} className="flex items-center justify-between gap-3 px-4 py-3 text-sm">
                <div>
                  <div className="font-medium">{s.name}</div>
                  <div className="text-[11px] text-muted-foreground">{na(s.country)}</div>
                </div>
                <div className="text-xs text-muted-foreground">{s.shipmentCount} shipment(s)</div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="mt-0.5 font-medium break-words">{value}</div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium tabular-nums">{value}</span>
    </div>
  );
}
