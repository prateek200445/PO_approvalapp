/** Attendance status palette for the HR dashboard. Excel uses the same colours (HrReportsService.StatusFill). */
export const HR_STATUS_STYLES: Record<string, { label: string; chip: string; dot: string; ring: string }> = {
  Present: {
    label: "Present",
    chip: "bg-emerald-100 text-emerald-900 dark:bg-emerald-500/20 dark:text-emerald-100",
    dot: "bg-emerald-500",
    ring: "border-emerald-500 bg-emerald-50 text-emerald-900 dark:bg-emerald-500/15 dark:text-emerald-100",
  },
  Absent: {
    label: "Absent",
    chip: "bg-red-100 text-red-900 dark:bg-red-500/20 dark:text-red-100",
    dot: "bg-red-500",
    ring: "border-red-500 bg-red-50 text-red-900 dark:bg-red-500/15 dark:text-red-100",
  },
  "Half Day": {
    label: "Half Day",
    chip: "bg-amber-100 text-amber-900 dark:bg-amber-500/20 dark:text-amber-100",
    dot: "bg-amber-500",
    ring: "border-amber-500 bg-amber-50 text-amber-900 dark:bg-amber-500/15 dark:text-amber-100",
  },
  PL: {
    label: "PL (leave)",
    chip: "bg-blue-100 text-blue-900 dark:bg-blue-500/20 dark:text-blue-100",
    dot: "bg-blue-500",
    ring: "border-blue-500 bg-blue-50 text-blue-900 dark:bg-blue-500/15 dark:text-blue-100",
  },
  CL: {
    label: "CL (leave)",
    chip: "bg-sky-100 text-sky-900 dark:bg-sky-500/20 dark:text-sky-100",
    dot: "bg-sky-500",
    ring: "border-sky-500 bg-sky-50 text-sky-900 dark:bg-sky-500/15 dark:text-sky-100",
  },
  WFH: {
    label: "WFH",
    chip: "bg-teal-100 text-teal-900 dark:bg-teal-500/20 dark:text-teal-100",
    dot: "bg-teal-500",
    ring: "border-teal-500 bg-teal-50 text-teal-900 dark:bg-teal-500/15 dark:text-teal-100",
  },
  Holiday: {
    label: "Holiday",
    chip: "bg-violet-100 text-violet-900 dark:bg-violet-500/20 dark:text-violet-100",
    dot: "bg-violet-500",
    ring: "border-violet-500 bg-violet-50 text-violet-900 dark:bg-violet-500/15 dark:text-violet-100",
  },
  "Weekly Off": {
    label: "Weekly Off",
    chip: "bg-gray-200 text-gray-800 dark:bg-gray-500/25 dark:text-gray-100",
    dot: "bg-gray-400",
    ring: "border-gray-500 bg-gray-100 text-gray-900 dark:bg-gray-500/20 dark:text-gray-100",
  },
};

export function hrStatusChip(status: string | null | undefined): string {
  return (status && HR_STATUS_STYLES[status]?.chip) || "bg-secondary text-foreground";
}

export function hrStatusDot(status: string | null | undefined): string {
  return (status && HR_STATUS_STYLES[status]?.dot) || "bg-muted-foreground/40";
}
