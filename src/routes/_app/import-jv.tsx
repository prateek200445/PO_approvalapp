import { createFileRoute } from "@tanstack/react-router";
import { ImportJvScreen } from "@/components/tds/ImportJvScreen";

export const Route = createFileRoute("/_app/import-jv")({
  head: () => ({ meta: [{ title: "Import JV — PO Portal" }] }),
  component: ImportJvPage,
});

function ImportJvPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Import JV</h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Upload journals for one company. Download the JV template after you pick the unit — ledger cells only accept names from that company.
        </p>
      </div>
      <ImportJvScreen />
    </div>
  );
}
