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

function clearStoredUser() {
  try {
    localStorage.removeItem(KEY);
    sessionStorage.removeItem(KEY);
  } catch {
    // ignore
  }
}

/** Reject sessions saved without a real username (camelCase login bug left username:null). */
function parseStoredUser(raw: string): AuthUser | null {
  try {
    const parsed = JSON.parse(raw) as Partial<AuthUser> | null;
    const username =
      typeof parsed?.username === "string" ? parsed.username.trim() : "";
    if (!username) {
      clearStoredUser();
      return null;
    }
    return {
      username,
      name:
        typeof parsed?.name === "string" && parsed.name.trim()
          ? parsed.name.trim()
          : username,
      designation: String(parsed?.designation ?? ""),
      department: String(parsed?.department ?? ""),
      role:
        parsed?.role === "HOD" ||
        parsed?.role === "Purchase Head" ||
        parsed?.role === "Finance Manager" ||
        parsed?.role === "Director"
          ? parsed.role
          : "Director",
      empCode:
        typeof parsed?.empCode === "string" && parsed.empCode.trim()
          ? parsed.empCode.trim()
          : null,
    };
  } catch {
    clearStoredUser();
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    try {
      // If Server Settings pointed at localhost, clear it on the live Vercel host
      // so API calls use the production rewrite instead of a dead local API.
      if (
        typeof window !== "undefined" &&
        window.location.hostname.endsWith("vercel.app")
      ) {
        const apiBase = localStorage.getItem("API_BASE_URL");
        if (
          apiBase &&
          (/localhost/i.test(apiBase) || /127\.0\.0\.1/.test(apiBase))
        ) {
          localStorage.removeItem("API_BASE_URL");
        }
      }

      const raw = localStorage.getItem(KEY) ?? sessionStorage.getItem(KEY);
      if (raw) setUser(parseStoredUser(raw));
    } catch {
      // ignore storage errors
    }

    setReady(true);

    const pingUrl = getApiUrl("/");
    fetch(pingUrl, { method: "GET" }).catch(() => {});
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

    const data = await response.json().catch(() => ({}));

    if (!response.ok) {
      throw new Error(
        typeof data?.message === "string" && data.message.trim()
          ? data.message
          : "Invalid Username or Password",
      );
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
    if (!loginName) {
      throw new Error("Login succeeded but username was missing. Please try again.");
    }
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

    // Drop the other storage so Remember-me toggles don't leave a stale session
    clearStoredUser();
    setUser(u);
    (remember ? localStorage : sessionStorage).setItem(KEY, JSON.stringify(u));
  }

  function logout() {
    setUser(null);
    clearStoredUser();
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