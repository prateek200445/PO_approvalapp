import { useState, useEffect } from "react";
import { getApiBaseUrl, setApiBaseUrl } from "@/lib/api-config";
import { Server, CheckCircle2, XCircle, RefreshCw } from "lucide-react";
import { toast } from "sonner";

interface ServerSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function ServerSettingsModal({ isOpen, onClose }: ServerSettingsModalProps) {
  const [url, setUrl] = useState("");
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [activeOverride, setActiveOverride] = useState("");

  useEffect(() => {
    if (isOpen) {
      const stored = getApiBaseUrl();
      setUrl(stored);
      setActiveOverride(stored);
      setTestResult(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  async function testConnection() {
    const target = (url.trim() || activeOverride || "").replace(/\/$/, "");
    // Empty URL means relative /api on this site — test that.
    const base = target || (typeof window !== "undefined" ? window.location.origin : "");
    if (!base) {
      setTestResult({ success: false, message: "Please enter a URL" });
      return;
    }

    setTesting(true);
    setTestResult(null);

    try {
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), 8000);

      const response = await fetch(`${base}/api/Auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName: "test_connection_ping", password: "" }),
        signal: controller.signal,
      });

      clearTimeout(id);

      setTestResult({
        success: true,
        message: `Connected successfully! Server responded with status ${response.status}.`,
      });
      toast.success("Connection successful!");
    } catch (err: any) {
      console.error("Connection test failed:", err);
      setTestResult({
        success: false,
        message: err.name === "AbortError"
          ? "Connection timed out (server is unreachable on this network)"
          : "Could not connect to server. Check URL, port, and network connection.",
      });
      toast.error("Connection failed.");
    } finally {
      setTesting(false);
    }
  }

  function handleSave() {
    if (!url.trim()) {
      setApiBaseUrl("");
      setActiveOverride("");
      toast.success("Reset to website default API (/api → production server)");
      onClose();
      return;
    }

    try {
      let targetUrl = url.trim();
      if (!/^https?:\/\//i.test(targetUrl)) {
        targetUrl = "http://" + targetUrl;
      }

      new URL(targetUrl);
      setApiBaseUrl(targetUrl);
      setActiveOverride(targetUrl);
      toast.success("Server URL updated successfully!");

      fetch(targetUrl, { method: "GET" })
        .then(() => console.log("New server URL pinged for cold start:", targetUrl))
        .catch((err) => console.warn("Failed to ping new server URL:", err));

      onClose();
    } catch (e) {
      toast.error("Please enter a valid URL (e.g. http://192.168.0.188:5000)");
    }
  }

  function handleResetToWebsite() {
    setUrl("");
    setApiBaseUrl("");
    setActiveOverride("");
    setTestResult(null);
    toast.success("Using website default (production API)");
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/60 p-4 backdrop-blur-sm sm:items-center animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md rounded-xl border border-border bg-card p-6 shadow-2xl animate-in zoom-in-95 duration-200"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Server className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Server Settings</h3>
            <p className="text-xs text-muted-foreground">Configure the backend API URL for this device.</p>
          </div>
        </div>

        <div className="mt-5 space-y-4">
          {activeOverride ? (
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2.5 text-xs text-amber-900 dark:text-amber-100">
              <div className="font-semibold">Custom API override is active on this browser</div>
              <div className="mt-1 break-all font-mono">{activeOverride}</div>
              <p className="mt-1.5 text-[11px] opacity-90">
                Mobile may work while this PC fails if this points to localhost or an old LAN IP. Clear it to use production.
              </p>
              <button
                type="button"
                onClick={handleResetToWebsite}
                className="mt-2 inline-flex h-8 items-center rounded-md bg-amber-600 px-3 text-[11px] font-semibold text-white hover:bg-amber-700"
              >
                Use website default
              </button>
            </div>
          ) : (
            <p className="text-[11px] text-muted-foreground">
              Currently using: <span className="font-mono text-primary">Relative /api (production)</span>
            </p>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Backend API URL
            </label>
            <input
              type="text"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="Leave empty for website default"
              className="h-10 w-full rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 transition-all font-mono"
            />
            <p className="text-[11px] text-muted-foreground">
              Only set this for local/dev testing. Leave blank on the live website.
            </p>
          </div>

          {testResult && (
            <div className={`flex items-start gap-2.5 rounded-lg border p-3 text-xs leading-relaxed ${
              testResult.success
                ? "bg-emerald-500/5 border-emerald-500/20 text-emerald-400"
                : "bg-destructive/5 border-destructive/20 text-destructive"
            }`}>
              {testResult.success ? (
                <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0 text-emerald-400" />
              ) : (
                <XCircle className="h-4.5 w-4.5 mt-0.5 flex-shrink-0" />
              )}
              <div>
                <span className="font-semibold">{testResult.success ? "Success" : "Error"}: </span>
                {testResult.message}
              </div>
            </div>
          )}
        </div>

        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
          <button
            type="button"
            disabled={testing}
            onClick={testConnection}
            className="inline-flex h-10 items-center justify-center gap-1.5 rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary disabled:opacity-50 transition cursor-pointer"
          >
            {testing ? (
              <RefreshCw className="h-4 w-4 animate-spin text-muted-foreground" />
            ) : (
              <RefreshCw className="h-4 w-4 text-muted-foreground" />
            )}
            Test Connection
          </button>

          <div className="flex gap-2 w-full sm:w-auto">
            <button
              onClick={onClose}
              className="h-10 flex-1 sm:flex-none rounded-md border border-input bg-surface px-4 text-sm font-medium hover:bg-secondary transition cursor-pointer"
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              className="h-10 flex-1 sm:flex-none rounded-md bg-primary px-5 text-sm font-semibold text-primary-foreground hover:bg-primary/90 transition shadow-sm cursor-pointer"
            >
              Save URL
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
