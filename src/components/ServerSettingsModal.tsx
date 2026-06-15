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

  useEffect(() => {
    if (isOpen) {
      setUrl(getApiBaseUrl());
      setTestResult(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  async function testConnection() {
    if (!url.trim()) {
      setTestResult({ success: false, message: "Please enter a URL" });
      return;
    }

    setTesting(true);
    setTestResult(null);

    // Normalize URL (no trailing slash)
    const normalizedUrl = url.trim().replace(/\/$/, "");

    try {
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), 6000); // 6s timeout

      const response = await fetch(`${normalizedUrl}/api/Auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName: "test_connection_ping", password: "" }),
        signal: controller.signal,
      });

      clearTimeout(id);
      
      // Even if unauthorized/bad request, the server responded!
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
      // If URL is empty, reset to relative default pathing
      setApiBaseUrl("");
      toast.success("Reset to default relative API URL (IIS Reverse Proxy mode)!");
      onClose();
      return;
    }

    try {
      // Add protocol if not present
      let targetUrl = url.trim();
      if (!/^https?:\/\//i.test(targetUrl)) {
        targetUrl = "http://" + targetUrl;
      }
      
      new URL(targetUrl);
      setApiBaseUrl(targetUrl);
      toast.success("Server URL updated successfully!");
      onClose();
    } catch (e) {
      toast.error("Please enter a valid URL (e.g. http://192.168.0.188:5000)");
    }
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
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Backend API URL
            </label>
            <input 
              type="text" 
              value={url} 
              onChange={(e) => setUrl(e.target.value)} 
              placeholder="e.g. http://192.168.0.188:5000" 
              className="h-10 w-full rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 transition-all font-mono"
            />
            <p className="text-[11px] text-muted-foreground">
              Current default: <span className="font-mono text-primary select-all">Relative to Site (/api)</span>
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
