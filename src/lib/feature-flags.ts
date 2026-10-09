/** Flip back to false if Bill Payment Entry Approval data is still wrong. */
export const BILL_PAYMENT_ENTRY_ENABLED = true;

/**
 * Order Book Summary — restricted report.
 * Keep in sync with POApprovalAPI appsettings.json → OrderBookSummary:AllowedUsers
 * (API enforces the same list; UI only hides nav for non-allowlisted users).
 * Add a username here AND in appsettings to grant access.
 */
export const ORDER_BOOK_SUMMARY_ALLOWED_USERS = [
  "aman",
  "anil",
  "dilendra",
  "prakash",
  "pritesh",
] as const;

export function canAccessOrderBookSummary(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return ORDER_BOOK_SUMMARY_ALLOWED_USERS.some((u) => u.toLowerCase() === key);
}

/** GST Bill Reconciliation. Keep in sync with appsettings.json → GstBillReco:AllowedUsers. */
export const GST_BILL_RECO_ALLOWED_USERS = ["prakash", "gstho"] as const;

export function canAccessGstBillReco(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return GST_BILL_RECO_ALLOWED_USERS.some((u) => u.toLowerCase() === key);
}

/**
 * HR Reports full access (see all employees / leave approve / leave credit / policy).
 * Keep in sync with POApprovalAPI appsettings.json → HrReports:FullAccessUsers.
 * Portal HR features are additive — existing ERP leave/HR for these logins is unchanged.
 * Everyone else uses employee self-service for their own EmpCode.
 */
export const HR_REPORTS_FULL_ACCESS_USERS = [
  "prakash",
  "grouphr",
  "plastenehr",
] as const;

/**
 * These logins only get the HR Reports shell (no PO/approvals/sales nav).
 * Speeds up load by skipping inbox + report prefetches.
 */
export const HR_PORTAL_ONLY_USERS = ["grouphr", "plastenehr"] as const;

export function isHrPortalOnlyUser(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return HR_PORTAL_ONLY_USERS.some((u) => u.toLowerCase() === key);
}

/**
 * Only these logins can change attendance results (salary / working-day overrides,
 * turning off the late / 9-hour rule, approving month-end).
 * Keep in sync with POApprovalAPI appsettings.json → HrReports:AttendanceEditors.
 */
export const HR_ATTENDANCE_EDITORS = ["grouphr", "plastenehr"] as const;

export function isHrAttendanceEditor(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return HR_ATTENDANCE_EDITORS.some((u) => u.toLowerCase() === key);
}

/**
 * Can press "Send on WhatsApp now" for the day's daily-reports PDF.
 * Keep in sync with POApprovalAPI appsettings.json → DailyReportDigest:AdminUsers.
 */
export const DAILY_REPORT_DIGEST_ADMINS = ["prakash"] as const;

export function isDailyReportDigestAdmin(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return DAILY_REPORT_DIGEST_ADMINS.some((u) => u.toLowerCase() === key);
}

export function isHrReportsFullAccessUser(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return HR_REPORTS_FULL_ACCESS_USERS.some((u) => u.toLowerCase() === key);
}

/**
 * Only these logins see Exhibition Leads, and their portal shows nothing else.
 * Keep in sync with POApprovalAPI appsettings.json → ExhibitionLeads:AllowedUsers.
 * The public exhibition forms (/exhibition/dealer, /exhibition/product) stay open to visitors.
 */
export const EXHIBITION_LEADS_USERS = ["umesh"] as const;

export function canAccessExhibitionLeads(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return EXHIBITION_LEADS_USERS.some((u) => u.toLowerCase() === key);
}

export function isExhibitionOnlyUser(username?: string | null): boolean {
  return canAccessExhibitionLeads(username);
}

/** Path prefixes a restricted login may open; null = no restriction. */
export function allowedPathsFor(username?: string | null): string[] | null {
  if (isHrPortalOnlyUser(username)) return ["/hr-reports", "/profile"];
  if (isExhibitionOnlyUser(username)) return ["/exhibition-leads", "/profile"];
  return null;
}

export function hrHomePath(username?: string | null): "/hr-reports" | "/exhibition-leads" | "/dashboard" {
  if (isHrPortalOnlyUser(username)) return "/hr-reports";
  if (isExhibitionOnlyUser(username)) return "/exhibition-leads";
  return "/dashboard";
}
