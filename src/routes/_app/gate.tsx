import { useEffect, useMemo, useState, type ReactNode } from "react";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Loader2, LogOut, Search } from "lucide-react";
import { GateSelect } from "@/components/gate/GateSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth-context";
import {
  createGateEntry,
  exitGateEntry,
  formatDuration,
  formatGateTime,
  getGateContext,
  getGateSummary,
  listInsideVehicles,
  lookupGateDocument,
  type GateDocument,
  type GateEntry,
} from "@/lib/gate-api";

export const Route = createFileRoute("/_app/gate")({
  head: () => ({ meta: [{ title: "Factory Gate — PO Portal" }] }),
  component: GateDeskPage,
});

type Mode = "entry" | "exit" | "inside";

function GateDeskPage() {
  const { user } = useAuth();
  const username = user?.username ?? "";
  const queryClient = useQueryClient();
  const context = useQuery({
    queryKey: ["gate-context", username],
    queryFn: () => getGateContext(username),
    enabled: !!username,
  });

  const factories = useMemo(() => {
    const map = new Map<string, string>();
    context.data?.gates.forEach((gate) => map.set(gate.factoryCode, gate.factoryName));
    return [...map.entries()].map(([code, name]) => ({ code, name }));
  }, [context.data]);

  const [factoryCode, setFactoryCode] = useState("");
  const [gateId, setGateId] = useState("");
  const [mode, setMode] = useState<Mode>("entry");

  useEffect(() => {
    if (!context.data) return;
    const storedFactory = localStorage.getItem("gate-factory") ?? "";
    const storedGate = localStorage.getItem("gate-id") ?? "";
    const factory = context.data.gates.some((gate) => gate.factoryCode === storedFactory)
      ? storedFactory
      : context.data.gates[0]?.factoryCode ?? "";
    setFactoryCode(factory);
    const gates = context.data.gates.filter((gate) => gate.factoryCode === factory);
    const gate = gates.some((item) => String(item.gateId) === storedGate)
      ? storedGate
      : String(gates[0]?.gateId ?? "");
    setGateId(gate);
  }, [context.data]);

  useEffect(() => {
    if (factoryCode) localStorage.setItem("gate-factory", factoryCode);
  }, [factoryCode]);
  useEffect(() => {
    if (gateId) localStorage.setItem("gate-id", gateId);
  }, [gateId]);

  const gates = context.data?.gates.filter((gate) => gate.factoryCode === factoryCode) ?? [];
  const summary = useQuery({
    queryKey: ["gate-summary", username, factoryCode],
    queryFn: () => getGateSummary(username, factoryCode),
    enabled: !!username && !!factoryCode,
    refetchInterval: 30_000,
  });

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: ["gate-summary"] });
    void queryClient.invalidateQueries({ queryKey: ["gate-inside"] });
    void queryClient.invalidateQueries({ queryKey: ["gate-register"] });
  }

  if (context.isLoading) {
    return <p className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" /> Opening the gate desk</p>;
  }
  if (context.isError) {
    return <p className="text-sm text-destructive">{context.error instanceof Error ? context.error.message : "Could not open the factory gate."}</p>;
  }

  return (
    <div className="mx-auto w-full max-w-xl space-y-5 pb-8">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Factory Gate</h1>
          <p className="text-sm text-muted-foreground">{context.data?.roleLabel}</p>
        </div>
        {context.data?.canViewRegister ? (
          <Link to="/gate-register" className="text-sm font-medium text-primary underline-offset-4 hover:underline">
            Register
          </Link>
        ) : null}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <GateSelect
          label="Factory"
          value={factoryCode}
          onChange={(next) => {
            setFactoryCode(next);
            const first = context.data?.gates.find((gate) => gate.factoryCode === next);
            setGateId(first ? String(first.gateId) : "");
          }}
          options={factories.map((factory) => ({ value: factory.code, label: factory.name }))}
          triggerClassName="h-12 rounded-xl text-base"
        />
        <GateSelect
          label="Gate"
          value={gateId}
          onChange={setGateId}
          options={gates.map((gate) => ({ value: String(gate.gateId), label: gate.gateName }))}
          triggerClassName="h-12 rounded-xl text-base"
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <Stat label="Inside now" value={summary.data?.insideCount} loading={summary.isLoading} />
        <Stat label="Entries today" value={summary.data?.todayEntries} loading={summary.isLoading} />
      </div>

      <div className="grid grid-cols-3 gap-2">
        <ModeButton active={mode === "entry"} onClick={() => setMode("entry")}>New entry</ModeButton>
        <ModeButton active={mode === "exit"} onClick={() => setMode("exit")}>Exit</ModeButton>
        <ModeButton active={mode === "inside"} onClick={() => setMode("inside")}>Inside</ModeButton>
      </div>

      {mode === "entry" ? (
        <EntryForm
          username={username}
          factoryCode={factoryCode}
          gateId={Number(gateId)}
          onSaved={refresh}
        />
      ) : null}
      {mode === "exit" || mode === "inside" ? (
        <InsideList
          username={username}
          factoryCode={factoryCode}
          gateId={Number(gateId)}
          allowExit={mode === "exit"}
          onChanged={refresh}
        />
      ) : null}
    </div>
  );
}

function Stat({ label, value, loading }: { label: string; value?: number; loading: boolean }) {
  return (
    <div className="rounded-2xl border bg-card px-4 py-4 shadow-sm">
      <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="mt-1 text-3xl font-semibold tabular-nums">{loading ? "—" : value ?? 0}</div>
    </div>
  );
}

function ModeButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={active
        ? "h-12 rounded-xl bg-primary text-sm font-semibold text-primary-foreground shadow-sm"
        : "h-12 rounded-xl border bg-card text-sm font-semibold text-foreground"}
    >
      {children}
    </button>
  );
}

function EntryForm({
  username,
  factoryCode,
  gateId,
  onSaved,
}: {
  username: string;
  factoryCode: string;
  gateId: number;
  onSaved: () => void;
}) {
  const [documentNo, setDocumentNo] = useState("");
  const [document, setDocument] = useState<GateDocument | null>(null);
  const [choices, setChoices] = useState<string[]>([]);
  const [looking, setLooking] = useState(false);
  const [lookupError, setLookupError] = useState("");
  const [vehicleNumber, setVehicleNumber] = useState("");
  const [driverName, setDriverName] = useState("");
  const [driverMobile, setDriverMobile] = useState("");
  const [partyName, setPartyName] = useState("");
  const [saved, setSaved] = useState<GateEntry | null>(null);
  const [error, setError] = useState("");

  async function openDocument(documentType?: string) {
    setLookupError("");
    setSaved(null);
    setLooking(true);
    try {
      const result = await lookupGateDocument(username, documentNo, documentType);
      if (result.needsChoice) {
        setDocument(null);
        setChoices(result.choices);
        return;
      }
      setChoices([]);
      setDocument(result.document);
      setPartyName(result.document?.supplierName ?? "");
      setVehicleNumber("");
      setDriverName("");
      setDriverMobile("");
    } catch (err) {
      setDocument(null);
      setChoices([]);
      setLookupError(err instanceof Error ? err.message : "Could not open that PO or work order.");
    } finally {
      setLooking(false);
    }
  }

  function openFullDocument(doc: GateDocument) {
    const path = doc.documentType === "WO" ? "workorder" : "po";
    window.open(`/${path}/${encodeURIComponent(doc.documentNo)}`, "_blank", "noopener");
  }

  const mutation = useMutation({
    mutationFn: () => createGateEntry({
      username,
      factoryCode,
      gateId,
      vehicleNumber,
      driverName,
      driverMobile,
      partyName,
      documentType: document!.documentType,
      documentNo: document!.documentNo,
    }),
    onSuccess: (entry) => {
      setSaved(entry);
      setError("");
      setDocument(null);
      setDocumentNo("");
      setVehicleNumber("");
      setDriverName("");
      setDriverMobile("");
      setPartyName("");
      onSaved();
    },
    onError: (err) => {
      setSaved(null);
      setError(err instanceof Error ? err.message : "Could not register the vehicle.");
    },
  });

  return (
    <div className="space-y-3">
      <form
        className="space-y-3 rounded-2xl border bg-card p-4 shadow-sm"
        onSubmit={(event) => {
          event.preventDefault();
          void openDocument();
        }}
      >
        <Field label="PO or work order number" required>
          <Input
            className="h-12 text-base md:text-base"
            value={documentNo}
            onChange={(event) => setDocumentNo(event.target.value)}
            autoComplete="off"
            placeholder="Enter the number given at the gate"
            required
          />
        </Field>
        {lookupError ? <p className="text-sm text-destructive" role="alert">{lookupError}</p> : null}
        {choices.length > 0 ? (
          <div className="grid grid-cols-2 gap-2">
            {choices.map((choice) => (
              <Button key={choice} type="button" variant="outline" className="h-12" onClick={() => void openDocument(choice)}>
                Open {choice === "WO" ? "work order" : "PO"}
              </Button>
            ))}
          </div>
        ) : null}
        <Button type="submit" className="h-12 w-full text-base" disabled={looking || documentNo.trim().length < 2}>
          {looking ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
          Open document
        </Button>
      </form>

      {document ? (
        <form
          className="space-y-3 rounded-2xl border bg-card p-4 shadow-sm"
          onSubmit={(event) => {
            event.preventDefault();
            setError("");
            mutation.mutate();
          }}
        >
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                {document.documentType === "WO" ? "Work order" : "Purchase order"}
              </div>
              <div className="font-mono text-lg font-semibold">{document.documentNo}</div>
              <div className="text-sm">{document.supplierName || "Supplier not on the document"}</div>
              {document.companyName ? <div className="text-xs text-muted-foreground">{document.companyName}</div> : null}
              {document.department ? <div className="text-xs text-muted-foreground">{document.department}</div> : null}
            </div>
            <button type="button" className="text-sm font-medium text-primary" onClick={() => openFullDocument(document)}>
              Open full page
            </button>
          </div>
          <div className="max-h-56 overflow-auto rounded-xl border">
            <table className="w-full text-left text-sm">
              <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                <tr>
                  <th className="px-2 py-2 font-medium">Item</th>
                  <th className="px-2 py-2 font-medium">Qty</th>
                  <th className="px-2 py-2 font-medium">Amount</th>
                </tr>
              </thead>
              <tbody>
                {document.lines.map((line, index) => (
                  <tr key={`${line.itemCode ?? "item"}-${index}`} className="border-t">
                    <td className="px-2 py-2">{line.itemDesc || line.itemCode || "—"}</td>
                    <td className="px-2 py-2 tabular-nums">{line.qty ?? "—"}</td>
                    <td className="px-2 py-2 tabular-nums">{line.total ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Field label="Vehicle number" required>
            <Input className="h-12 text-base uppercase md:text-base" value={vehicleNumber} onChange={(event) => setVehicleNumber(event.target.value)} autoComplete="off" required />
          </Field>
          <Field label="Driver name" required>
            <Input className="h-12 text-base md:text-base" value={driverName} onChange={(event) => setDriverName(event.target.value)} required />
          </Field>
          <Field label="Driver number" required>
            <Input className="h-12 text-base md:text-base" inputMode="tel" value={driverMobile} onChange={(event) => setDriverMobile(event.target.value)} required />
          </Field>
          <Field label="Supplier">
            <Input className="h-12 text-base md:text-base" value={partyName} onChange={(event) => setPartyName(event.target.value)} />
          </Field>
          {error ? <p className="text-sm text-destructive" role="alert">{error}</p> : null}
          <Button type="submit" className="h-12 w-full text-base" disabled={mutation.isPending || !factoryCode || !gateId}>
            {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Register entry
          </Button>
        </form>
      ) : null}

      {saved ? (
        <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-3 py-3 text-sm">
          <div className="flex items-center gap-2 font-semibold"><CheckCircle2 className="h-4 w-4" /> Vehicle is inside</div>
          <div className="mt-1 font-mono text-base">{saved.gateEntryNumber}</div>
          <div className="text-muted-foreground">{saved.documentType} {saved.documentNo} · {saved.vehicleNumber} · {formatGateTime(saved.entryAt)}</div>
        </div>
      ) : null}
    </div>
  );
}

function InsideList({
  username,
  factoryCode,
  gateId,
  allowExit,
  onChanged,
}: {
  username: string;
  factoryCode: string;
  gateId: number;
  allowExit: boolean;
  onChanged: () => void;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [pending, setPending] = useState<GateEntry | null>(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(query), 250);
    return () => window.clearTimeout(timer);
  }, [query]);

  const list = useQuery({
    queryKey: ["gate-inside", username, factoryCode, debounced],
    queryFn: () => listInsideVehicles(username, factoryCode, debounced),
    enabled: !!username && !!factoryCode,
    refetchInterval: 30_000,
  });

  const mutation = useMutation({
    mutationFn: (entry: GateEntry) => exitGateEntry(entry.entryId, { username, exitGateId: gateId }),
    onSuccess: (entry) => {
      setPending(null);
      setError("");
      setMessage(`${entry.vehicleNumber} exited · ${formatDuration(entry.durationMinutes)} inside · ${entry.gateEntryNumber}`);
      onChanged();
      void list.refetch();
    },
    onError: (err) => setError(err instanceof Error ? err.message : "Could not mark the exit."),
  });

  return (
    <section className="space-y-3">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="h-12 pl-9 text-base md:text-base"
          placeholder="Vehicle number or gate entry number"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>
      {message ? <p className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-3 py-2 text-sm">{message}</p> : null}
      {error ? <p className="text-sm text-destructive" role="alert">{error}</p> : null}
      {list.isLoading ? <p className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" /> Loading vehicles</p> : null}
      {list.isError ? <p className="text-sm text-destructive">{list.error instanceof Error ? list.error.message : "Could not load vehicles inside."}</p> : null}
      {list.data && list.data.length === 0 ? <p className="text-sm text-muted-foreground">No vehicles are inside.</p> : null}
      <ul className="space-y-2">
        {list.data?.map((entry) => (
          <li key={entry.entryId} className="rounded-2xl border bg-card p-4 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="font-mono text-lg font-semibold">{entry.vehicleNumber}</div>
                <div className="text-sm">{entry.driverName}</div>
                <div className="text-xs text-muted-foreground">{entry.gateEntryNumber}{entry.documentNo ? ` · ${entry.documentType} ${entry.documentNo}` : ""} · {entry.purposeName}</div>
                <div className="text-xs text-muted-foreground">In {formatGateTime(entry.entryAt)} · {entry.gateName}</div>
                {entry.partyName ? <div className="text-xs text-muted-foreground">{entry.partyName}</div> : null}
                {entry.isLongStay ? <div className="mt-1 text-xs font-semibold text-amber-700 dark:text-amber-300">Long stay</div> : null}
              </div>
            </div>
            {allowExit ? (
              pending?.entryId === entry.entryId ? (
                <div className="mt-3 grid grid-cols-2 gap-2">
                  <Button type="button" variant="outline" className="h-12" onClick={() => setPending(null)}>Cancel</Button>
                  <Button type="button" className="h-12" disabled={mutation.isPending} onClick={() => mutation.mutate(entry)}>
                    {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <LogOut className="h-4 w-4" />}
                    Confirm exit
                  </Button>
                </div>
              ) : (
                <Button type="button" className="mt-3 h-12 w-full text-base" onClick={() => { setPending(entry); setError(""); }}>
                  Mark exit
                </Button>
              )
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  );
}

function Field({ label, required, children }: { label: string; required?: boolean; children: ReactNode }) {
  return (
    <label className="block space-y-1.5 text-sm font-medium">
      <span>{label}{required ? " *" : ""}</span>
      {children}
    </label>
  );
}
