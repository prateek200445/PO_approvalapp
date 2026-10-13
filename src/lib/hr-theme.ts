/** Section accents for the HR portal (hub cards, tabs, banners). Class strings are literal so Tailwind keeps them. */
export type HrAccent = "emerald" | "amber" | "blue" | "violet" | "pink" | "cyan" | "fuchsia" | "orange" | "indigo";

export const HR_ACCENTS: Record<
  HrAccent,
  { gradient: string; tile: string; soft: string; text: string; border: string; shadow: string }
> = {
  emerald: {
    gradient: "from-emerald-500 to-teal-500",
    tile: "bg-gradient-to-br from-emerald-500 to-teal-500 text-white",
    soft: "bg-emerald-50 dark:bg-emerald-500/10",
    text: "text-emerald-700 dark:text-emerald-300",
    border: "border-emerald-200 hover:border-emerald-400 dark:border-emerald-500/30",
    shadow: "hover:shadow-emerald-500/20",
  },
  amber: {
    gradient: "from-amber-400 to-orange-500",
    tile: "bg-gradient-to-br from-amber-400 to-orange-500 text-white",
    soft: "bg-amber-50 dark:bg-amber-500/10",
    text: "text-amber-700 dark:text-amber-300",
    border: "border-amber-200 hover:border-amber-400 dark:border-amber-500/30",
    shadow: "hover:shadow-amber-500/20",
  },
  blue: {
    gradient: "from-blue-500 to-indigo-500",
    tile: "bg-gradient-to-br from-blue-500 to-indigo-500 text-white",
    soft: "bg-blue-50 dark:bg-blue-500/10",
    text: "text-blue-700 dark:text-blue-300",
    border: "border-blue-200 hover:border-blue-400 dark:border-blue-500/30",
    shadow: "hover:shadow-blue-500/20",
  },
  violet: {
    gradient: "from-violet-500 to-purple-600",
    tile: "bg-gradient-to-br from-violet-500 to-purple-600 text-white",
    soft: "bg-violet-50 dark:bg-violet-500/10",
    text: "text-violet-700 dark:text-violet-300",
    border: "border-violet-200 hover:border-violet-400 dark:border-violet-500/30",
    shadow: "hover:shadow-violet-500/20",
  },
  pink: {
    gradient: "from-pink-500 to-rose-500",
    tile: "bg-gradient-to-br from-pink-500 to-rose-500 text-white",
    soft: "bg-pink-50 dark:bg-pink-500/10",
    text: "text-pink-700 dark:text-pink-300",
    border: "border-pink-200 hover:border-pink-400 dark:border-pink-500/30",
    shadow: "hover:shadow-pink-500/20",
  },
  cyan: {
    gradient: "from-cyan-500 to-sky-500",
    tile: "bg-gradient-to-br from-cyan-500 to-sky-500 text-white",
    soft: "bg-cyan-50 dark:bg-cyan-500/10",
    text: "text-cyan-700 dark:text-cyan-300",
    border: "border-cyan-200 hover:border-cyan-400 dark:border-cyan-500/30",
    shadow: "hover:shadow-cyan-500/20",
  },
  fuchsia: {
    gradient: "from-fuchsia-500 to-pink-500",
    tile: "bg-gradient-to-br from-fuchsia-500 to-pink-500 text-white",
    soft: "bg-fuchsia-50 dark:bg-fuchsia-500/10",
    text: "text-fuchsia-700 dark:text-fuchsia-300",
    border: "border-fuchsia-200 hover:border-fuchsia-400 dark:border-fuchsia-500/30",
    shadow: "hover:shadow-fuchsia-500/20",
  },
  orange: {
    gradient: "from-orange-500 to-red-500",
    tile: "bg-gradient-to-br from-orange-500 to-red-500 text-white",
    soft: "bg-orange-50 dark:bg-orange-500/10",
    text: "text-orange-700 dark:text-orange-300",
    border: "border-orange-200 hover:border-orange-400 dark:border-orange-500/30",
    shadow: "hover:shadow-orange-500/20",
  },
  indigo: {
    gradient: "from-indigo-500 to-violet-500",
    tile: "bg-gradient-to-br from-indigo-500 to-violet-500 text-white",
    soft: "bg-indigo-50 dark:bg-indigo-500/10",
    text: "text-indigo-700 dark:text-indigo-300",
    border: "border-indigo-200 hover:border-indigo-400 dark:border-indigo-500/30",
    shadow: "hover:shadow-indigo-500/20",
  },
};
