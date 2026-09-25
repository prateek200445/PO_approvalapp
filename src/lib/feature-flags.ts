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

export function isHrReportsFullAccessUser(username?: string | null): boolean {
  if (!username) return false;
  const key = username.trim().toLowerCase();
  return HR_REPORTS_FULL_ACCESS_USERS.some((u) => u.toLowerCase() === key);
}

export function hrHomePath(username?: string | null): "/hr-reports" | "/dashboard" {
  return isHrPortalOnlyUser(username) ? "/hr-reports" : "/dashboard";
}
