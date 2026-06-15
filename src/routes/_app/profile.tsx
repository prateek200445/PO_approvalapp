import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { KeyRound, LogOut, Building2, Server } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { toast } from "sonner";
import { ServerSettingsModal } from "@/components/ServerSettingsModal";

export const Route = createFileRoute("/_app/profile")({
  head: () => ({ meta: [{ title: "Profile — PO Portal" }] }),
  component: Profile,
});

function Profile() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [showPwd, setShowPwd] = useState(false);
  const [showServerSettings, setShowServerSettings] = useState(false);

  const initials = (user?.name ?? "U").split(" ").map((s) => s[0]).slice(0, 2).join("");

  return (
    <div className="mx-auto max-w-2xl space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">Profile</h1>

      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary text-xl font-semibold text-primary-foreground">
            {initials}
          </div>
          <div className="min-w-0">
            <div className="text-lg font-semibold">{user?.name}</div>
            <div className="text-sm text-muted-foreground">{user?.designation}</div>
            <div className="mt-1 inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
              {user?.role}
            </div>
          </div>
        </div>

        <dl className="mt-6 divide-y divide-border border-t border-border">
          <Row icon={Building2} label="Department" value={user?.department ?? "—"} />
       
        </dl>
      </div>

      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h2 className="text-sm font-semibold">Account Actions</h2>
        <div className="mt-4 space-y-2">
          <button
            onClick={() => setShowPwd(true)}
            className="flex w-full items-center justify-between rounded-lg border border-border bg-surface p-3 text-left hover:bg-secondary cursor-pointer"
          >
            <div className="flex items-center gap-3">
              <KeyRound className="h-5 w-5 text-muted-foreground" />
              <span className="text-sm font-medium">Change Password</span>
            </div>
          </button>
          <button
            onClick={() => setShowServerSettings(true)}
            className="flex w-full items-center justify-between rounded-lg border border-border bg-surface p-3 text-left hover:bg-secondary cursor-pointer"
          >
            <div className="flex items-center gap-3">
              <Server className="h-5 w-5 text-muted-foreground" />
              <span className="text-sm font-medium">Server Settings</span>
            </div>
          </button>
          <button
            onClick={() => { logout(); navigate({ to: "/" }); toast.success("Signed out"); }}
            className="flex w-full items-center justify-between rounded-lg border border-destructive/20 bg-destructive/5 p-3 text-left hover:bg-destructive/10"
          >
            <div className="flex items-center gap-3">
              <LogOut className="h-5 w-5 text-destructive" />
              <span className="text-sm font-medium text-destructive">Logout</span>
            </div>
          </button>
        </div>
      </div>

      {showPwd && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center" onClick={() => setShowPwd(false)}>
          <div className="w-full max-w-sm rounded-xl bg-card p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-base font-semibold">Change Password</h3>
            <p className="mt-1 text-sm text-muted-foreground">Enter your current and new password.</p>
            <div className="mt-4 space-y-3">
              <input type="password" placeholder="Current password" className="h-10 w-full rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20" />
              <input type="password" placeholder="New password" className="h-10 w-full rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20" />
              <input type="password" placeholder="Confirm new password" className="h-10 w-full rounded-md border border-input bg-surface px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/20" />
            </div>
            <div className="mt-5 flex gap-2">
              <button onClick={() => setShowPwd(false)} className="h-10 flex-1 rounded-md border border-input bg-surface text-sm font-medium hover:bg-secondary">Cancel</button>
              <button
                onClick={() => { setShowPwd(false); toast.success("Password updated"); }}
                className="h-10 flex-1 rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90"
              >
                Update
              </button>
            </div>
          </div>
        </div>
      )}
      
      <ServerSettingsModal isOpen={showServerSettings} onClose={() => setShowServerSettings(false)} />
    </div>
  );
}

function Row({ icon: Icon, label, value }: { icon: any; label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3 py-3">
      <div className="flex items-center gap-3">
        <Icon className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm text-muted-foreground">{label}</span>
      </div>
      <span className="text-sm font-medium">{value}</span>
    </div>
  );
}
