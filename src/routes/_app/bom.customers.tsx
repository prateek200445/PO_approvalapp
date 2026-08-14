import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, Loader2, Save } from "lucide-react";
import { toast } from "sonner";
import { SearchableSelect } from "@/components/SearchableSelect";
import { fetchBomCustomers, updateBomCustomer } from "@/lib/bom-api";
import type { BomCustomerOption } from "@/lib/bom-types";

export const Route = createFileRoute("/_app/bom/customers")({
  head: () => ({ meta: [{ title: "BOM Customers — PO Portal" }] }),
  component: BomCustomersPage,
});

function BomCustomersPage() {
  const [customers, setCustomers] = useState<BomCustomerOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState("");
  const [email, setEmail] = useState("");
  const [email1, setEmail1] = useState("");
  const [email2, setEmail2] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      try {
        const rows = await fetchBomCustomers();
        if (!cancelled) setCustomers(rows);
      } catch (err: unknown) {
        if (!cancelled) toast.error(err instanceof Error ? err.message : "Failed to load customers");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const masterCustomers = customers.filter((c) => c.fromMaster);
  const selectedRow = masterCustomers.find((c) => c.companyName === selected);

  const companyOptions = useMemo(
    () => masterCustomers.map((c) => ({ value: c.companyName, label: c.companyName })),
    [masterCustomers],
  );

  useEffect(() => {
    if (!selectedRow) {
      setEmail("");
      setEmail1("");
      setEmail2("");
      return;
    }
    setEmail(selectedRow.email ?? "");
    setEmail1(selectedRow.email1 ?? "");
    setEmail2(selectedRow.email2 ?? "");
  }, [selectedRow]);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setSaving(true);
    try {
      await updateBomCustomer(selected, { email, email1, email2 });
      toast.success("Customer emails updated");
      setCustomers((prev) =>
        prev.map((c) =>
          c.companyName === selected ? { ...c, email, email1, email2 } : c,
        ),
      );
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Update failed");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 pb-24 md:p-6">
      <Link to="/bom" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="h-4 w-4" />
        Back to BOM report
      </Link>

      <div>
        <h1 className="text-2xl font-semibold">Customer Master</h1>
        <p className="text-sm text-muted-foreground">
          Update contact emails for parties in Company Master (used for BOM email routing).
        </p>
      </div>

      {loading ? (
        <div className="flex items-center gap-2 text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      ) : (
        <form onSubmit={handleSave} className="space-y-4 rounded-xl border bg-card p-4 shadow-sm">
          <label className="block space-y-1 text-sm">
            <span className="font-medium">Company</span>
            <SearchableSelect
              options={companyOptions}
              value={selected}
              onChange={setSelected}
              placeholder="Select company…"
              searchPlaceholder="Search company…"
              emptyText="No companies found"
            />
          </label>

          {customers.some((c) => !c.fromMaster) ? (
            <p className="text-xs text-muted-foreground">
              {customers.filter((c) => !c.fromMaster).length} party name(s) appear on BOMs but are not in Company
              Master yet — add them in the ERP master first.
            </p>
          ) : null}

          <label className="block space-y-1 text-sm">
            <span className="font-medium">Primary email</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={!selected}
              className="w-full rounded-md border bg-background px-3 py-2 disabled:opacity-50"
            />
          </label>
          <label className="block space-y-1 text-sm">
            <span className="font-medium">CC email 1</span>
            <input
              type="email"
              value={email1}
              onChange={(e) => setEmail1(e.target.value)}
              disabled={!selected}
              className="w-full rounded-md border bg-background px-3 py-2 disabled:opacity-50"
            />
          </label>
          <label className="block space-y-1 text-sm">
            <span className="font-medium">CC email 2</span>
            <input
              type="email"
              value={email2}
              onChange={(e) => setEmail2(e.target.value)}
              disabled={!selected}
              className="w-full rounded-md border bg-background px-3 py-2 disabled:opacity-50"
            />
          </label>

          <button
            type="submit"
            disabled={!selected || saving}
            className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
          >
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Save
          </button>
        </form>
      )}
    </div>
  );
}
