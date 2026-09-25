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
