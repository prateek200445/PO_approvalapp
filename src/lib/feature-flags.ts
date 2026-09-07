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
