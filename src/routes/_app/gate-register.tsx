import { useEffect, useMemo, useState } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, Loader2 } from "lucide-react";
import { GateSelect } from "@/components/gate/GateSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth-context";
import {
  addFactoryGate,
  correctGateEntry,
  downloadGateRegister,
  formatDuration,
  formatGateTime,
  getGateContext,
  getGateSummary,
  listGateAudit,
  listGateRegister,
  type GateEntry,
  type GateRegisterFilters,
} from "@/lib/gate-api";

export const Route = createFileRoute("/_app/gate-register")({
  head: () => ({ meta: [{ title: "Gate Register — PO Portal" }] }),
  component: GateRegisterPage,
});

function GateRegisterPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";
  const queryClient = useQueryClient();
  const context = useQuery({
    queryKey: ["gate-context", username],
    queryFn: () => getGateContext(username),
    enabled: !!username,
  });
  const [factoryCode, setFactoryCode] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [vehicleNumber, setVehicleNumber] = useState("");
  const [driver, setDriver] = useState("");
  const [party, setParty] = useState("");
  const [purposeCode, setPurposeCode] = useState("");
  const [status, setStatus] = useState("");
  const [longStayOnly, setLongStayOnly] = useState(false);
  const [ready, setReady] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState("");
  const [selected, setSelected] = useState<GateEntry | null>(null);

  useEffect(() => {
    if (!context.data || ready) return;
    setDateFrom(context.data.serverDate);
    setDateTo(context.data.serverDate);
    setReady(true);
  }, [context.data, ready]);

  const factories = useMemo(() => {
    const map = new Map<string, string>();
    context.data?.gates.forEach((gate) => map.set(gate.factoryCode, gate.factoryName));
    return [...map.entries()].map(([code, name]) => ({ code, name }));
  }, [context.data]);

  const filters: GateRegisterFilters = {
    factoryCode,
    dateFrom,
    dateTo,
    vehicleNumber,
    driver,
    party,
    purposeCode,
    status,
    longStayOnly,
  };

  const summary = useQuery({
    queryKey: ["gate-summary", username, factoryCode],
    queryFn: () => getGateSummary(username, factoryCode),
    enabled: !!username && ready,
  });
  const register = useQuery({
    queryKey: ["gate-register", username, factoryCode, dateFrom, dateTo, vehicleNumber, driver, party, purposeCode, status, longStayOnly],
    queryFn: () => listGateRegister(username, filters),
    enabled: !!username && ready,
  });

  async function exportExcel() {
    setExportError("");
    setExporting(true);
    try {
      await downloadGateRegister(username, filters);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "Could not download Excel.");
    } finally {
      setExporting(false);
    }
  }

  if (context.isLoading) {
    return <p className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" /> Loading the gate register</p>;
  }
  if (context.isError) {
    return <p className="text-sm text-destructive">{context.error instanceof Error ? context.error.message : "Could not open the gate register."}</p>;
  }

  return (
    <div className="space-y-5 pb-10">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Gate Register</h1>
          <p className="text-sm text-muted-foreground">
            {context.data?.roleLabel}
            {summary.data?.serverDate ? ` · Server date ${summary.data.serverDate}` : ""}
          </p>
        </div>
        <div className="flex gap-2">
          {context.data?.canOperate ? (
            <Link to="/gate" className="inline-flex h-10 items-center rounded-md border px-3 text-sm font-medium">Gate desk</Link>
          ) : null}
          <Button type="button" variant="outline" onClick={exportExcel} disabled={exporting}>
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Excel
          </Button>
        </div>
      </div>
      {exportError ? <p className="text-sm text-destructive">{exportError}</p> : null}

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Inside now" value={String(summary.data?.insideCount ?? "—")} />
        <Kpi label="Entries today" value={String(summary.data?.todayEntries ?? "—")} />
        <Kpi label="Exits today" value={String(summary.data?.todayExits ?? "—")} />
        <Kpi label="Avg turnaround today" value={formatDuration(summary.data?.averageTurnaroundMinutes)} />
      </div>

      {summary.data && summary.data.longStay.length > 0 ? (
        <section className="rounded-2xl border border-amber-500/40 bg-amber-500/10 p-4">
          <h2 className="text-sm font-semibold">Inside longer than {summary.data.longStayHours} hours</h2>
          <ul className="mt-2 space-y-1 text-sm">
            {summary.data.longStay.map((entry) => (
              <li key={entry.entryId}>
                <span className="font-mono font-semibold">{entry.vehicleNumber}</span>
                {" · "}{entry.factoryName}{" · "}in {formatGateTime(entry.entryAt)}{" · "}{entry.gateEntryNumber}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <div className="grid grid-cols-1 gap-3 rounded-2xl border bg-card p-4 sm:grid-cols-2 lg:grid-cols-4">
        <GateSelect
          label="Factory"
          value={factoryCode}
          onChange={setFactoryCode}
          options={[{ value: "", label: "All factories" }, ...factories.map((factory) => ({ value: factory.code, label: factory.name }))]}
        />
        <Text label="From" type="date" value={dateFrom} onChange={setDateFrom} />
        <Text label="To" type="date" value={dateTo} onChange={setDateTo} />
        <GateSelect
          label="Status"
          value={status}
          onChange={setStatus}
          options={[
            { value: "", label: "All" },
            { value: "INSIDE", label: "Inside" },
            { value: "EXITED", label: "Exited" },
          ]}
        />
        <Text label="Vehicle / entry no." value={vehicleNumber} onChange={setVehicleNumber} />
        <Text label="Driver" value={driver} onChange={setDriver} />
        <Text label="Supplier / customer" value={party} onChange={setParty} />
        <GateSelect
          label="Purpose"
          value={purposeCode}
          onChange={setPurposeCode}
          options={[
            { value: "", label: "All" },
            ...(context.data?.purposes.map((purpose) => ({ value: purpose.purposeCode, label: purpose.purposeName })) ?? []),
          ]}
        />
        <label className="flex items-center gap-2 self-end pb-2 text-sm font-medium">
          <input type="checkbox" checked={longStayOnly} onChange={(event) => setLongStayOnly(event.target.checked)} />
          Long stay only
        </label>
      </div>

      {context.data?.canManageGates ? (
        <AddGate
          username={username}
          onAdded={() => void queryClient.invalidateQueries({ queryKey: ["gate-context"] })}
        />
      ) : null}

      {register.isLoading ? <p className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" /> Loading register</p> : null}
      {register.isError ? <p className="text-sm text-destructive">{register.error instanceof Error ? register.error.message : "Could not load the gate register."}</p> : null}
      {register.data ? <p className="text-sm text-muted-foreground">{register.data.total} record{register.data.total === 1 ? "" : "s"}</p> : null}

      <div className="hidden overflow-x-auto rounded-2xl border md:block">
        <table className="w-full min-w-[960px] text-left text-sm">
          <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-3 py-2 font-medium">Entry</th>
              <th className="px-3 py-2 font-medium">Vehicle</th>
              <th className="px-3 py-2 font-medium">Driver</th>
              <th className="px-3 py-2 font-medium">Party</th>
              <th className="px-3 py-2 font-medium">Purpose</th>
              <th className="px-3 py-2 font-medium">In</th>
              <th className="px-3 py-2 font-medium">Out</th>
              <th className="px-3 py-2 font-medium">Time</th>
              <th className="px-3 py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {register.data?.rows.map((entry) => (
              <tr key={entry.entryId} className="border-t align-top">
                <td className="px-3 py-2">
                  <button type="button" className="font-mono text-xs font-semibold text-primary" onClick={() => setSelected(entry)}>{entry.gateEntryNumber}</button>
                  <div className="text-xs text-muted-foreground">{entry.factoryCode} · {entry.gateName}</div>
                </td>
                <td className="px-3 py-2 font-mono">{entry.vehicleNumber}</td>
                <td className="px-3 py-2">{entry.driverName}<div className="text-xs text-muted-foreground">{entry.driverMobile}</div></td>
                <td className="px-3 py-2">{entry.partyName || "—"}</td>
                <td className="px-3 py-2">{entry.purposeName}</td>
                <td className="px-3 py-2 whitespace-nowrap">{formatGateTime(entry.entryAt)}<div className="text-xs text-muted-foreground">{entry.entryUserId}</div></td>
                <td className="px-3 py-2 whitespace-nowrap">{formatGateTime(entry.exitAt)}<div className="text-xs text-muted-foreground">{entry.exitUserId}</div></td>
                <td className="px-3 py-2">{entry.status === "INSIDE" ? "—" : formatDuration(entry.durationMinutes)}{entry.isLongStay ? <div className="text-xs font-semibold text-amber-700 dark:text-amber-300">Long stay</div> : null}</td>
                <td className="px-3 py-2">{entry.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className="space-y-2 md:hidden">
        {register.data?.rows.map((entry) => (
          <li key={entry.entryId}>
            <button type="button" className="w-full rounded-2xl border bg-card p-3 text-left" onClick={() => setSelected(entry)}>
              <div className="flex items-center justify-between gap-2">
                <span className="font-mono font-semibold">{entry.vehicleNumber}</span>
                <span className="text-xs font-semibold">{entry.status}</span>
              </div>
              <div className="text-sm">{entry.driverName} · {entry.purposeName}</div>
              <div className="text-xs text-muted-foreground">{entry.gateEntryNumber} · in {formatGateTime(entry.entryAt)}</div>
              {entry.isLongStay ? <div className="text-xs font-semibold text-amber-700 dark:text-amber-300">Long stay</div> : null}
            </button>
          </li>
        ))}
      </ul>

      {selected && context.data ? (
        <EntryDetail
          username={username}
          entry={selected}
          purposes={context.data.purposes}
          canCorrect={context.data.canCorrect}
          onClose={() => setSelected(null)}
          onSaved={(entry) => {
            setSelected(entry);
            void queryClient.invalidateQueries({ queryKey: ["gate-register"] });
            void queryClient.invalidateQueries({ queryKey: ["gate-summary"] });
          }}
        />
      ) : null}
    </div>
  );
}

function EntryDetail({
  username,
  entry,
  purposes,
  canCorrect,
  onClose,
  onSaved,
}: {
  username: string;
  entry: GateEntry;
  purposes: { purposeCode: string; purposeName: string }[];
  canCorrect: boolean;
  onClose: () => void;
  onSaved: (entry: GateEntry) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [reason, setReason] = useState("");
  const [vehicleNumber, setVehicleNumber] = useState(entry.vehicleNumber);
  const [driverName, setDriverName] = useState(entry.driverName);
  const [driverMobile, setDriverMobile] = useState(entry.driverMobile ?? "");
  const [partyName, setPartyName] = useState(entry.partyName ?? "");
  const [purposeCode, setPurposeCode] = useState(entry.purposeCode);
  const [remarks, setRemarks] = useState(entry.remarks ?? "");
  const [error, setError] = useState("");
  const audit = useQuery({
    queryKey: ["gate-audit", entry.entryId, username],
    queryFn: () => listGateAudit(username, entry.entryId),
  });
  const mutation = useMutation({
    mutationFn: () => correctGateEntry(entry.entryId, {
      username, reason, vehicleNumber, driverName, driverMobile, partyName, purposeCode, remarks,
    }),
    onSuccess: (saved) => {
      setEditing(false);
      setReason("");
      setError("");
      onSaved(saved);
    },
    onError: (err) => setError(err instanceof Error ? err.message : "Could not save the correction."),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-3 md:items-center" onClick={onClose}>
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-card p-4 shadow-xl" onClick={(event) => event.stopPropagation()}>
        <div className="flex items-start justify-between gap-3">
          <div>
            <div className="font-mono text-sm text-muted-foreground">{entry.gateEntryNumber}</div>
            <h2 className="text-xl font-semibold">{entry.vehicleNumber}</h2>
          </div>
          <button type="button" className="text-sm text-muted-foreground" onClick={onClose}>Close</button>
        </div>
        <dl className="mt-3 space-y-1 text-sm">
          <Row label="Driver" value={entry.driverName} />
          <Row label="Mobile" value={entry.driverMobile || "—"} />
          <Row label="Party" value={entry.partyName || "—"} />
          <Row label="Purpose" value={entry.purposeName} />
          <Row label="Remarks" value={entry.remarks || "—"} />
          <Row label="Entry" value={`${formatGateTime(entry.entryAt)} · ${entry.entryUserId}`} />
          <Row label="Exit" value={entry.exitAt ? `${formatGateTime(entry.exitAt)} · ${entry.exitUserId}` : "Still inside"} />
          <Row label="Time inside" value={entry.status === "EXITED" ? formatDuration(entry.durationMinutes) : "—"} />
        </dl>
        {canCorrect ? (
          editing ? (
            <form className="mt-4 space-y-2" onSubmit={(event) => { event.preventDefault(); mutation.mutate(); }}>
              <Input className="h-11" value={vehicleNumber} onChange={(event) => setVehicleNumber(event.target.value)} placeholder="Vehicle number" />
              <Input className="h-11" value={driverName} onChange={(event) => setDriverName(event.target.value)} placeholder="Driver name" />
              <Input className="h-11" value={driverMobile} onChange={(event) => setDriverMobile(event.target.value)} placeholder="Mobile" />
              <Input className="h-11" value={partyName} onChange={(event) => setPartyName(event.target.value)} placeholder="Supplier / customer" />
              <GateSelect
                label="Purpose"
                value={purposeCode}
                onChange={setPurposeCode}
                options={purposes.map((purpose) => ({ value: purpose.purposeCode, label: purpose.purposeName }))}
                triggerClassName="h-11"
              />
              <Input className="h-11" value={remarks} onChange={(event) => setRemarks(event.target.value)} placeholder="Remarks" />
              <Input className="h-11" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Reason for correction" required />
              {error ? <p className="text-sm text-destructive">{error}</p> : null}
              <div className="grid grid-cols-2 gap-2">
                <Button type="button" variant="outline" onClick={() => setEditing(false)}>Cancel</Button>
                <Button type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving" : "Save correction"}</Button>
              </div>
            </form>
          ) : (
            <Button type="button" variant="outline" className="mt-4" onClick={() => setEditing(true)}>Correct record</Button>
          )
        ) : null}
        <h3 className="mt-5 text-sm font-semibold">Audit trail</h3>
        {audit.isLoading ? <p className="mt-1 text-sm text-muted-foreground">Loading</p> : null}
        <ul className="mt-2 space-y-2 text-sm">
          {audit.data?.map((item) => (
            <li key={item.auditId} className="rounded-xl border px-3 py-2">
              <div className="font-medium">{item.action}{item.fieldName ? ` · ${item.fieldName}` : ""}</div>
              <div className="text-xs text-muted-foreground">{formatGateTime(item.changedAt)} · {item.changedBy}</div>
              {item.oldValue || item.newValue ? <div className="text-xs">{item.oldValue || "—"} → {item.newValue || "—"}</div> : null}
              {item.reason ? <div className="text-xs">{item.reason}</div> : null}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function AddGate({ username, onAdded }: { username: string; onAdded: () => void }) {
  const [open, setOpen] = useState(false);
  const [factoryCode, setFactoryCode] = useState("");
  const [factoryName, setFactoryName] = useState("");
  const [gateCode, setGateCode] = useState("");
  const [gateName, setGateName] = useState("");
  const [error, setError] = useState("");
  const mutation = useMutation({
    mutationFn: () => addFactoryGate({ username, factoryCode, factoryName, gateCode, gateName }),
    onSuccess: () => {
      setOpen(false);
      setFactoryCode("");
      setFactoryName("");
      setGateCode("");
      setGateName("");
      setError("");
      onAdded();
    },
    onError: (err) => setError(err instanceof Error ? err.message : "Could not add the gate."),
  });
  if (!open) {
    return <Button type="button" variant="outline" onClick={() => setOpen(true)}>Add factory gate</Button>;
  }
  return (
    <form className="grid grid-cols-1 gap-2 rounded-2xl border p-4 sm:grid-cols-2" onSubmit={(event) => { event.preventDefault(); mutation.mutate(); }}>
      <Input className="h-11" placeholder="Factory code" value={factoryCode} onChange={(event) => setFactoryCode(event.target.value)} required />
      <Input className="h-11" placeholder="Factory name" value={factoryName} onChange={(event) => setFactoryName(event.target.value)} required />
      <Input className="h-11" placeholder="Gate code" value={gateCode} onChange={(event) => setGateCode(event.target.value)} required />
      <Input className="h-11" placeholder="Gate name" value={gateName} onChange={(event) => setGateName(event.target.value)} required />
      {error ? <p className="text-sm text-destructive sm:col-span-2">{error}</p> : null}
      <div className="flex gap-2 sm:col-span-2">
        <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
        <Button type="submit" disabled={mutation.isPending}>Save gate</Button>
      </div>
    </form>
  );
}

function Kpi({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border bg-card px-4 py-3">
      <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="mt-1 text-2xl font-semibold tabular-nums">{value}</div>
    </div>
  );
}

function Text({ label, value, onChange, type = "text" }: { label: string; value: string; onChange: (value: string) => void; type?: string }) {
  return (
    <label className="space-y-1 text-xs font-medium">
      <span>{label}</span>
      <Input className="h-10" type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-3">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-right">{value}</dd>
    </div>
  );
}
