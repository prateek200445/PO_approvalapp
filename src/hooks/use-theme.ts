import { useCallback, useEffect, useState } from "react";

const THEME_KEY = "po-theme";

function readDarkPreference(): boolean {
  if (typeof window === "undefined") return false;
  const stored = localStorage.getItem(THEME_KEY);
  if (stored === "dark") return true;
  if (stored === "light") return false;
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

export function useTheme() {
  const [dark, setDark] = useState(false);

  useEffect(() => {
    const isDark = readDarkPreference();
    document.documentElement.classList.toggle("dark", isDark);
    setDark(isDark);
  }, []);

  const toggleTheme = useCallback(() => {
    setDark((prev) => {
      const next = !prev;
      document.documentElement.classList.toggle("dark", next);
      localStorage.setItem(THEME_KEY, next ? "dark" : "light");
      return next;
    });
  }, []);

  return { dark, toggleTheme };
}
