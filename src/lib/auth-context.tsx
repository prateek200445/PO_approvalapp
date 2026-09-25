import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { getApiUrl } from "@/lib/api-config";

export interface AuthUser {
  username: string;
  name: string;
  designation: string;
  department: string;
  role: "Purchase Head" | "HOD" | "Finance Manager" | "Director";
  /** Linked payroll EmpCode when LoginRights.EmpCode is set; may be null. */
  empCode?: string | null;
}

interface AuthCtx {
  user: AuthUser | null;
  login: (username: string, password: string, remember: boolean) => Promise<void>;
  logout: () => void;
  ready: boolean;
}

const Ctx = createContext<AuthCtx | null>(null);
const KEY = "po-portal-user";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(KEY) ?? sessionStorage.getItem(KEY);

      if (raw) {
        setUser(JSON.parse(raw));
      }
    } catch {
      // ignore storage errors
    }

    setReady(true);

    // Wake up the backend immediately when the page loads to start the Render server's cold start
    const pingUrl = getApiUrl("/");
    console.log("Triggering backend cold-start ping to:", pingUrl);
    fetch(pingUrl, { method: "GET" })
      .then((res) => {
        console.log("Backend cold-start ping responded with status:", res.status);
      })
      .catch((err) => {
        console.warn("Backend cold-start ping dispatched, awaiting server wake-up:", err);
      });
  }, []);

  async function login(
    username: string,
    password: string,
    remember: boolean
  ) {
    const response = await fetch(
      getApiUrl("/api/Auth/login"),
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userName: username,
          password: password,
        }),
      }
    );

    const data = await response.json();

    if (!response.ok) {
      throw new Error("Invalid Username or Password");
    }

    const authority = Number(data.authority ?? data.Authority);
    const empCodeRaw = data.EmpCode ?? data.empCode ?? null;
    const empCode =
      typeof empCodeRaw === "string" && empCodeRaw.trim().length > 0
        ? empCodeRaw.trim()
        : null;
    const loginName =
      (typeof data.UserName === "string" && data.UserName.trim()) ||
      (typeof data.userName === "string" && data.userName.trim()) ||
      username.trim();
    const displayName =
      (typeof data.FullName === "string" && data.FullName.trim()) ||
      (typeof data.fullName === "string" && data.fullName.trim()) ||
      loginName;

    const u: AuthUser = {
      username: loginName,
      name: displayName,
      designation: String(Number.isFinite(authority) ? authority : ""),
      department: data.Deptt ?? data.deptt ?? "",
      role:
        authority === 1
          ? "HOD"
          : authority === 2
          ? "Purchase Head"
          : authority === 3
          ? "Finance Manager"
          : "Director",
      empCode,
    };

    setUser(u);

    (remember ? localStorage : sessionStorage).setItem(
      KEY,
      JSON.stringify(u)
    );
  }

  function logout() {
  setUser(null);
  localStorage.removeItem(KEY);
  sessionStorage.removeItem(KEY);

  window.location.href = "/";
}

  return (
    <Ctx.Provider value={{ user, login, logout, ready }}>
      {children}
    </Ctx.Provider>
  );
}

export function useAuth() {
  const c = useContext(Ctx);

  if (!c) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return c;
}