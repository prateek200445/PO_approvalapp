export function getApiBaseUrl(): string {
  if (typeof window !== "undefined") {
    const stored = localStorage.getItem("API_BASE_URL");
    if (stored) return stored.trim().replace(/\/$/, "");
  }
  
  const envUrl = import.meta.env.VITE_API_URL;
  if (envUrl) return envUrl.trim().replace(/\/$/, "");

  // Fallback default (empty string resolves to relative paths on the same host/port)
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
