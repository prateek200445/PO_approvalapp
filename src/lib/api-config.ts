/** POApprovalAPI default when running `dotnet run` (launchSettings profile POApprovalAPI). */
export const LOCAL_DEV_API = "http://localhost:5115";

function isLocalDevHost(hostname: string): boolean {
  return hostname === "localhost" || hostname === "127.0.0.1";
}

/** Stored Server Settings URLs that silently break local dev (SPA 404, bad cert, Render stale). */
function sanitizeStoredApiBase(stored: string): string {
  const trimmed = stored.trim().replace(/\/$/, "");
  if (!trimmed) return trimmed;

  if (typeof window === "undefined") return trimmed;

  try {
    const u = new URL(trimmed.startsWith("http") ? trimmed : `http://${trimmed}`);
    const frontendPort = window.location.port || (window.location.protocol === "https:" ? "443" : "80");

    // Same port as Vite/TanStack dev server — /api hits SPA routes, not .NET
    if (isLocalDevHost(u.hostname) && u.port === frontendPort) {
      return LOCAL_DEV_API;
    }

    // Dev cert for https://localhost:7115 is usually untrusted in the browser
    if (import.meta.env.DEV && isLocalDevHost(u.hostname) && u.protocol === "https:") {
      return LOCAL_DEV_API;
    }

    // localhost:8080 explicitly saved
    if (import.meta.env.DEV && isLocalDevHost(u.hostname) && u.port === "8080") {
      return LOCAL_DEV_API;
    }
  } catch {
    // keep trimmed if not a valid URL
  }

  return trimmed;
}

export function getApiBaseUrl(): string {
  if (typeof window !== "undefined") {
    const stored = localStorage.getItem("API_BASE_URL");
    if (stored?.trim()) return sanitizeStoredApiBase(stored);
  }

  const envUrl = import.meta.env.VITE_API_URL;
  if (envUrl?.trim()) return envUrl.trim().replace(/\/$/, "");

  if (import.meta.env.DEV && typeof window !== "undefined" && isLocalDevHost(window.location.hostname)) {
    return LOCAL_DEV_API;
  }

  return "";
}

export function getApiUrl(path: string): string {
  const base = getApiBaseUrl();
  const cleanPath = path.startsWith("/") ? path : `/${path}`;
  return `${base}${cleanPath}`;
}

export function setApiBaseUrl(url: string) {
  if (typeof window !== "undefined") {
    if (!url) {
      localStorage.removeItem("API_BASE_URL");
    } else {
      localStorage.setItem("API_BASE_URL", url.trim().replace(/\/$/, ""));
    }
  }
}
