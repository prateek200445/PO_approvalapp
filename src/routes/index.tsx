import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { Factory, Lock, User, ShieldCheck, Settings, Eye, EyeOff } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { toast } from "sonner";
import { ServerSettingsModal } from "@/components/ServerSettingsModal";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Sign in — Approvals Portal" },
      { name: "description", content: "Sign in to the HCP Plastene Bulkpack Ltd. Approvals Portal." },
    ],
  }),
  component: Login,
});

function Login() {
  const { login, user, ready } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(true);
  const [loading, setLoading] = useState(false);
  const [showServerSettings, setShowServerSettings] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [dark, setDark] = useState(false);

  useEffect(() => {
    if (ready && user) navigate({ to: "/dashboard" });
  }, [ready, user, navigate]);

  useEffect(() => {
    const stored = localStorage.getItem("po-theme");
    if (stored === "dark" || (!stored && window.matchMedia("(prefers-color-scheme: dark)").matches)) {
      setDark(true);
    }
  }, []);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!username || !password) {
      toast.error("Please enter username and password");
      return;
    }
   setLoading(true);

try {
  await login(username, password, remember);
  toast.success("Welcome back");
  navigate({ to: "/dashboard" });
} catch (err: any) {
  alert("ERROR: " + (err?.message || "Unknown error"));
} finally {
  setLoading(false);
}
  }

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      {/* Brand side */}
      <div className="relative hidden flex-col justify-between overflow-hidden bg-primary p-12 text-primary-foreground lg:flex">
        <div className="absolute inset-0 opacity-20" style={{
          backgroundImage: "radial-gradient(circle at 20% 30%, rgba(255,255,255,.4) 0, transparent 40%), radial-gradient(circle at 80% 70%, rgba(255,255,255,.25) 0, transparent 40%)"
        }} />
        <div className="relative flex items-center gap-3">
          <img 
            src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"} 
            alt="HCP Logo" 
            className={dark ? "h-13 w-13 rounded-lg" : "h-11 w-11 rounded-lg"}
          />
          <div>
            <div className="text-lg font-semibold">HCP Plastene Bulkpack Ltd.</div>
            <div className="text-xs opacity-80">Enterprise Approval Suite</div>
          </div>
        </div>
        <div className="relative space-y-5">
          <h1 className="text-4xl font-semibold leading-tight">Approvals Portal</h1>
          <p className="max-w-md text-base opacity-85">
            Review, approve and audit purchase orders from the plant floor or on the move. Built for managers, HODs, Finance and Directors.
          </p>
          <div className="flex items-center gap-2 text-sm opacity-90">
            <ShieldCheck className="h-4 w-4" />
            Secure SSO-ready · SQL Server integration ready
          </div>
        </div>
        <div className="relative text-xs opacity-70">© HCP Plastene Bulkpack Ltd.</div>
      </div>

      {/* Form side */}
      <div className="relative flex items-center justify-center bg-background px-5 py-12 sm:px-8">
        <button
          type="button"
          onClick={() => setShowServerSettings(true)}
          className="absolute right-4 top-4 flex h-9 w-9 items-center justify-center rounded-lg border border-border bg-card text-muted-foreground transition hover:bg-secondary hover:text-foreground cursor-pointer shadow-sm z-10"
          title="Server Settings"
        >
          <Settings className="h-4.5 w-4.5" />
        </button>

        <div className="w-full max-w-sm">
          <div className="mb-8 flex items-center gap-2.5 lg:hidden">
            <img 
              src={dark ? "/hcp_logo_dark.png" : "/hcp_logo.jpeg"} 
              alt="HCP Logo" 
              className={dark ? "h-12 w-12 rounded-lg" : "h-10 w-10 rounded-lg"}
            />
            <div>
              <div className="text-sm font-semibold">HCP Plastene Bulkpack Ltd.</div>
              <div className="text-[11px] text-muted-foreground">Approvals Portal</div>
            </div>
          </div>

          <h2 className="text-2xl font-semibold tracking-tight">Welcome!</h2>
          <p className="mt-1 text-sm text-muted-foreground">Use your corporate credentials to continue.</p>

          <form onSubmit={submit} className="mt-7 space-y-4">
            <div className="space-y-1.5">
              <label className="text-sm font-medium">Username</label>
              <div className="relative">
                <User className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="text"
                  autoComplete="username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="employee.id"
                  className="h-11 w-full rounded-md border border-input bg-surface pl-10 pr-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="text-sm font-medium">Password</label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder=""
                  className="h-11 w-full rounded-md border border-input bg-surface pl-10 pr-10 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 text-sm text-muted-foreground">
                <input
                  type="checkbox"
                  checked={remember}
                  onChange={(e) => setRemember(e.target.checked)}
                  className="h-4 w-4 rounded border-input accent-primary"
                />
                Remember me
              </label>
              <button type="button" className="text-sm font-medium text-primary hover:underline">
                Forgot password?
              </button>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="h-11 w-full rounded-md bg-primary text-sm font-semibold text-primary-foreground transition hover:bg-primary/90 disabled:opacity-60"
            >
              {loading ? "Signing in…" : "Sign in"}
            </button>

            
          </form>
        </div>
      </div>
      <ServerSettingsModal isOpen={showServerSettings} onClose={() => setShowServerSettings(false)} />
    </div>
  );
}
