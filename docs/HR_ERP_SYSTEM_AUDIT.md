# HR ERP System Audit Report

**Project:** PO_Code (PO Approval Portal + Material Processing ERP)  
**Audit date:** 2026-09-25  
**Scope:** Existing application + both SQL databases as used by HR code  
**Status:** Audit only — **no major HR Portal implementation started** (per brief §33 / FIRST ACTION)

---

## Executive summary

The portal already has a working **HR Reports / self-service slice** (`/hr-reports` + `api/hr/reports/*`) on top of live payroll SQL (`Loginentry` @ 3445) and app SQL (`MaterialProcessing` @ 5115). Employees can view attendance/salary, apply leave/WFH (3-day window), and request month-end verification. Full HR (currently allowlisted as `prakash`) can approve leave/WFH, run confirmation+CL credit, and apply HO monthly PL credit.

**It is not yet a full HR Portal.** Gaps vs the 35-section brief: no manager/hierarchy-based approval, no employee vs HR dashboards as specified, half-day rule in code is **10:30 / 9h** (brief says **verify 10:15**), no holiday calendar integration, no notifications for HR events, hardcoded full-access user, no JWT on HR APIs, monthly PL credit is **manual** (not scheduled).

| Area | Status |
|------|--------|
| Login / EmpCode | Exists (reuse) |
| Attendance view + salary estimate | Implemented |
| Leave apply + history (`LeaveHistory`) | Implemented |
| Leave approve/reject (HR only) | Implemented |
| WFH as leave type | Implemented |
| Confirmation + CL credit | Partially (HR-only insert; no employee form/status UX) |
| Month-end attendance verify | Implemented (Pending → HR Approve) |
| HO 1.5 PL / month after 1 year | Partially (preview/apply buttons; no auto job) |
| Hierarchy-based approval | **Missing** |
| Holidays / weekly-off leave validation | **Missing / partial** |
| HR + Employee dashboards | **Missing** |
| Notifications for HR workflows | **Missing** |
| Half-day 10:15 | **Mismatch** (code uses 10:30 OR ≥9h) |

---

## 1. Frontend architecture

| Item | Detail |
|------|--------|
| Stack | TanStack Start + React 19 + Vite 7 + TanStack Router + TanStack Query + Tailwind 4 |
| Package | `tanstack_start_ts` (repo root) |
| Auth UI | `src/lib/auth-context.tsx` — login → `/api/Auth/login`; user in `localStorage`/`sessionStorage` (`po-portal-user`) |
| User fields | `username`, `name`, `designation` (from `poallocation.authority`), `department`, PO `role`, optional `empCode` |
| HR route | `src/routes/_app/hr-reports.tsx` → `/hr-reports` |
| HR API client | `src/lib/hr-reports-api.ts` |
| Full-access UI gate | `src/lib/feature-flags.ts` → `HR_REPORTS_FULL_ACCESS_USERS = ["prakash"]` |
| Navigation | `src/components/AppShell.tsx` — Reports → **HR Reports** (visible to all logged-in users; page gates by access mode) |
| Prior proposal | `docs/hr-v1-proposal.html` |

**HR UI today (tabs in `hr-reports.tsx`):**

| Tab | Employee (`mode=self`) | HR (`mode=full`) |
|-----|------------------------|------------------|
| Attendance & salary | Own only | Employee picker + reports + Excel |
| Leave | Apply + own history | Approvals queue + history (no apply form) |
| WFH | Dedicated apply tab | Hidden (WFH via Approvals) |
| Leave policy | Hidden | Confirmation + HO PL credit |
| Month-end verify | Request verification | Pending list + approve |

There is **no** separate Employee HR Portal vs HR Portal shell; one page switches by access mode.

---

## 2. Backend architecture

| Item | Detail |
|------|--------|
| Framework | ASP.NET Core (`POApprovalAPI/`) |
| DI (`Program.cs`) | `HrReportsOptions`, `HrAccessService`, `HrReportsService`, `HrSelfServiceService`; `DatabaseService` singleton |
| HR controller | `Controllers/HrReportsController.cs` — prefix **`api/hr/reports`** |
| Data access | Dapper + `Microsoft.Data.SqlClient` via `DatabaseService` factories |
| Auth for HR | Query/body `username` → `HrAccessService` (no JWT on HR endpoints) |

---

## 3. Both database names (and related connections)

| Connection key | Server (conceptual) | Database | Used by HR for |
|----------------|---------------------|----------|----------------|
| `DefaultConnection` | `103.240.33.122,5115` | **MaterialProcessing** | Login (cross-db), **`HrAttendanceMonthAck`** |
| `LoginEntryConnection` | Same host **5115** | **loginentry** (archive) | EmpCode via `loginentry.dbo.LoginRights`; **not** used for punches |
| `PayrollLoginEntryConnection` | `103.240.33.122,3445` | **Loginentry** (live payroll) | **Primary HR data**: empinfo, punches, leave, salary, confirmation |
| `ProductionConnection` | 5115 | **production** | Not used by Hr*.cs |

Secrets: `AppSecretsDefaults` / env `DB_PASSWORD`, `PAYROLL_DB_PASSWORD` (injected in `Program.cs`).

**Practical split:** Auth & app tables on **5115**; attendance/leave/payroll master on **3445**. EmpCode sync between `LoginRights` (5115) and payroll (3445) is a known fragility.

---

## 4. Relevant tables

### Payroll `Loginentry` (3445) — reused

| Table | Role | Key columns used in HR code |
|-------|------|-----------------------------|
| `empinfo` | Employee master | `EmpCode`, `Name`, `Designation`, `Deptt`, `CompanyName`, `Branch`, `IsHOEmp`, `isactive`, `DateOJ`, `SalaryPerDay`, `IsSalaryPerDay`, `CTC` |
| `tempattendance` | Primary punches | `Empcode`, `Sysdate`, `InTime`, `OutTime` |
| `Attendancemachine` | Fallback punches | `Empcode`, `AttendanceDate`, `intime`, `Branch` |
| `LeaveHistory` | Leave + WFH applications (single source of truth) | `EmpCode`, `Days`, `TypeofLeave`, `FromDate`, `FromTo`, `Purpose`, `status_leave`, `approvedby`, `Currentdte`, … |
| `availableleave` | PL/CL balances | `Empcode`, `TotalPL`, `TotalCL`, `AvailPL`, `AvailCL`, `FromDate`, `ToDate` |
| `EmployeeConfirmation` | Confirmation record | `Empcode`, `MailDate` |
| `Salary` | Wage estimate | `EmpCode`, `Salary`, `Basic_DA`, `GrossSalary`, `Total`, `FromDate`, `ToDate` |
| `LoginRights` | Payroll EmpCode fallback | `EmpCode`, `Name`/`FullName` |

### App `MaterialProcessing` (5115)

| Table | Role | Columns |
|-------|------|---------|
| `Loginentry.dbo.LoginRights` (cross-db) | Portal login + EmpCode | `Name`, `EmpCode`, `FullName`, `Password`, `email`, … |
| `poallocation` | PO authority (not HR RBAC) | `username`, `authority`, `Deptt` |
| **`HrAttendanceMonthAck`** | Month-end verify (created by app) | `EmpCode`, `YearMonth`, `Status`, `VerifiedAt`/`By`, `Note`, `RequestedBy`/`At`, `ReviewedBy`/`At` |

### Present in payroll schema probes but **not** used by HR APIs

| Object | Notes |
|--------|-------|
| `ConfirmationPeriod` | Columns dumped by `Scripts/ProbeHrEmpinfo` only |
| `CompanyLeave` | Same |
| `dbo.leavecredit` | SP exists; **HR services do not call it** — credit is direct SQL on `availableleave` |

### Not found in HR APIs

- Dedicated WFH table (WFH = `LeaveHistory.TypeofLeave = 'WFH'`)
- Holiday master table usage
- Reporting-manager / approval-hierarchy tables wired into HR

---

## 5. Relevant views

**None identified** as required by current Hr*.cs. Attendance/leave/salary use base tables with `WITH (NOLOCK)` queries.

*(Further live `INFORMATION_SCHEMA.VIEWS` enumeration should be done before any schema change; probes focused on tables + `leavecredit`.)*

---

## 6. Relevant stored procedures / functions / triggers

| Object | Status |
|--------|--------|
| `dbo.leavecredit` (payroll) | Exists; definition probeable; **not invoked** by portal HR |
| App leave credit | `HrSelfServiceService` — `UPDATE`/`INSERT` `availableleave` |
| Triggers | Not inspected live in this audit pass — **must** be checked before ALTER on `LeaveHistory` / `availableleave` / `empinfo` |

---

## 7. Existing APIs (`api/hr/reports`)

| Method | Path | Access |
|--------|------|--------|
| GET | `access` | Returns `full` / `self` / `none` |
| GET | `companies` / `branches` | Non-none; non-full gets empty lists |
| GET | `employees` | Full: search; self: own row |
| GET | `attendance` / `salary` (+ Excel) | Full any emp / self own |
| GET | `leave` / `leave-eligibility` | Same |
| POST | `leave` | **Own EmpCode only** |
| POST | `wfh` | Own only (`TypeofLeave=WFH`) |
| GET | `leave/pending` | Full only |
| POST | `leave/approve` / `leave/reject` | Full only |
| POST | `confirmation` | Full only |
| GET/POST | `leave-credit` (+ preview) | Full only |
| GET | `attendance-ack` | Own / full |
| GET | `attendance-ack/pending` | Full only |
| POST | `attendance-ack` | Employee → Pending; `approve=true` → full only |

**Dead client surface:** `hr-reports-api.ts` still references `common-bank` — backend Common Bank Sheet was **removed**; no server implementation.

Other portal APIs (PO, BOM, Chatbot, Inventory stock SPs, etc.) are out of HR scope but share auth/DB infrastructure.

---

## 8. Existing login system

1. User posts credentials to **`/api/Auth/login`** (`AuthController`).
2. Validates against **`Loginentry.dbo.LoginRights`** via MaterialProcessing connection (5115).
3. Joins **`poallocation`** for `authority` / `Deptt` (PO workflow roles).
4. Returns `UserName`, `EmpCode`, `FullName`, `authority`, `Deptt`.
5. Frontend stores session locally; subsequent HR calls pass **`username`** as query/body param.

**Do not replace.** HR Portal must continue to use this login. Gaps: EmpCode may be blank on LoginRights (blocks self mode); no bearer token; username spoofing risk on HR endpoints.

---

## 9. Existing roles (as used today)

| Layer | Roles / modes | Source |
|-------|---------------|--------|
| PO / portal | HOD, Purchase Head, Finance, Director, etc. | `poallocation.authority` |
| HR access | `full` / `self` / `none` | `HrReports:FullAccessUsers` (`prakash`, `corporatehr`, `hohr`) + resolved EmpCode |
| Config allowlist | **`prakash`**, **`corporatehr`**, **`hohr`**, **`grouphr`** | Full HR portal access; ERP leave/HR for these logins unchanged |

**Not used for HR:** `LoginRights.IsPayroll`, designation title “HR”, department name alone.

---

## 10. Existing employee hierarchy

| Concept | In HR module? | Evidence |
|---------|---------------|----------|
| Company / Branch / Deptt / Designation | Yes (filters/display from `empinfo`) | `HrReportsService` |
| IsHOEmp | Yes (HO PL rules, office filter) | `empinfo.IsHOEmp` |
| DateOJ (joining) | Yes (1-year eligibility) | `empinfo.DateOJ` |
| Reporting manager | **No** | No manager columns queried in Hr*.cs |
| Functional / department authority chain | **No** for leave/WFH | Approvals = FullAccessUsers only |
| PO approval chain | Exists for POs | Separate from HR |

**Implication:** Brief’s Employee → RM → Dept → HR → Higher Authority must be **discovered from DB/ERP** (or new mapping table if none exists) before hierarchy-based approval can be implemented. Current code cannot derive RM from HR queries alone.

---

## 11. Existing approval hierarchy

| Workflow | Approver determination | Status values |
|----------|------------------------|---------------|
| Leave / WFH | Any `FullAccessUsers` user | `Pending` → `Approved` / `Rejected` (also blank status treated as pending in queue) |
| Month-end attendance | Same full users | `None` → `Pending` → `Approved` |
| Confirmation | Full user posts confirmation (no multi-step) | Row in `EmployeeConfirmation` |
| PO approvals | `poallocation` / existing PO logic | Unrelated |

**Missing vs brief:** Draft / Submitted / Cancelled as first-class statuses; multi-level history; current approver from hierarchy; manager inbox.

Pending leave queue **filters** old ERP noise: `FromDate ≥ today OR Currentdte ≥ now−14 days`.

---

## 12. Existing attendance logic

**Sources (3445):** `tempattendance` first; if empty for range → `Attendancemachine` (in only).

**Classification (`HrReportsService.Classify`) — CURRENT:**

```text
MaxLateIn     = 10:30:00
FullDayWorked = 9 hours

No punch     → Absent
Rule off     → Present
First in ≤ 10:30 → Present
Else if (out − in) ≥ 9h → Present
Else punched → Half Day
```

**Payable days:** Present=1, Half Day=0.5, Absent=0; approved PL/CL/WFH override punch for that day. Under 1 year: PL/CL ignored on calendar (WFH still counts). Sundays excluded in salary working-day path; **no holiday calendar**.

### Conflict with fixed business rule (brief §6 / §32)

| Spec | Code today |
|------|------------|
| Half day after **10:15 AM — VERIFY** | Half day unless in ≤ **10:30** OR worked ≥ **9h** |

**Action for implementation phase:** Reconcile with stakeholders — either restore/verify **10:15** or document approved change to 10:30/9h. Do not silently keep both narratives.

---

## 13. Existing leave logic

| Rule | Implementation |
|------|----------------|
| Single source of truth | `LeaveHistory` (portal + legacy ERP rows) |
| Apply types | PL, CL, LWP, WFH |
| 3-day window | `FromDate` ∈ [today, today+3] — frontend + `LeaveApplyMaxAheadDays = 3` backend |
| Under 1 year | `DateOJ` months &lt; 12 → cannot apply PL/CL; LWP/WFH OK |
| Balance on apply | Check latest `availableleave.AvailPL` / `AvailCL` |
| Overlap | Block if non-rejected/cancelled overlap |
| Approve | Set Approved + `approvedby`; deduct PL/CL if applicable |
| Reject | Rejected; no balance change |
| Holidays / weekly offs on apply | **Not validated** as leave blockers beyond Sunday salary path |
| Existing ERP leave UI | Must remain; portal writes same table — **do not fork** |

---

## 14. Existing PL / CL logic

| Rule | Status |
|------|--------|
| Under 1 year: no PL/CL entitlement in portal | Enforced on apply + display |
| HO ≥1 year: 18 PL/year → **1.5/month** | Constants `HoPlGrantAfterOneYear=18`, `HoPlMonthlyCredit=1.5`; preview + manual apply |
| Automatic monthly credit job | **Missing** |
| CL only after confirmation | Credited when HR runs confirmation (`ClCreditOnConfirmation=6`); employee cannot credit |
| `dbo.leavecredit` SP | Not used by portal |

Eligibility date = joining + 1 year from `DateOJ` (not calendar-year alone) — aligned with brief intent when HO path is used.

---

## 15. Existing confirmation logic

| Item | Status |
|------|--------|
| Table | `EmployeeConfirmation (Empcode, MailDate)` |
| Who can process | Full HR only (`POST confirmation`) |
| Effect | Insert confirmation + credit CL toward 6 on open `availableleave` (or insert FY-start Apr 1 row) |
| Employee self-view / application form | **Missing** (no employee confirmation UX) |
| Multi-level approval | **Missing** |
| `ConfirmationPeriod` table | Exists in DB per probe; unused |

---

## 16. Existing WFH functionality

| Item | Status |
|------|--------|
| Storage | `LeaveHistory` with `TypeofLeave='WFH'` |
| Employee apply | Yes (`POST wfh` / Leave apply) |
| Approval | Same as leave (full HR) |
| Attendance integration | Approved WFH counts as payable day on calendar |
| Separate WFH policy / attendance invent | Not present — correctly reuses leave pipeline |

---

## 17. What is already implemented

- Login reuse + EmpCode resolution (with fallbacks)
- Employee attendance calendar + Excel + salary estimate
- Employee leave apply (PL/CL/LWP) with 3-day + balance + overlap + under-1yr rules
- Employee WFH apply
- Leave/WFH history from `LeaveHistory`
- HR pending approvals (date-filtered) + approve/reject + balance deduct
- HR confirmation + CL credit
- HR HO PL credit preview/apply (manual)
- Month-end attendance verification request + HR approve (`HrAttendanceMonthAck`)
- Employee list search for HR; inactive filter (`isactive=yes`)
- Access modes full/self/none + self-only apply enforcement

---

## 18. What is partially implemented

- **Two portals/views** — mode switch inside one page, not dashboards as specified
- **Confirmation** — HR write path only; no employee form/status/eligibility UX
- **Monthly PL credit** — logic exists; not automatic/scheduled
- **RBAC** — works but depends on hardcoded `prakash` allowlist
- **Attendance verification monitoring** — pending list only; no dept-wise dashboard cards
- **Leave balances display** — available via leave/eligibility APIs; not rich opening/credited/used/pending breakdown cards
- **Half-day rule** — implemented, but **differs** from 10:15 VERIFY wording
- **Approval history** — `approvedby` + status on row; no full audit trail table
- **Common Bank Sheet** — removed from product; client stubs may remain

---

## 19. What is missing (vs full brief)

1. Hierarchy-derived approvers (RM / dept / HR / higher) — no hardcoding
2. Distinct Employee Dashboard + HR Dashboard (cards, filters, reports)
3. Employee confirmation application + status UI
4. Automated monthly 1.5 PL credit job after year-1 HO eligibility
5. Holiday / weekly-off validation on leave apply (if required by existing ERP rules)
6. Attendance correction request workflow (beyond month-end verify)
7. Notifications (email exists for PO/BOM — **not** wired to HR leave/WFH/confirm/verify)
8. Draft / Cancelled leave states as first-class UX
9. Multi-level approval history retention table (if not inventable from existing ERP)
10. Secure HR API auth (JWT/session binding; stop spoofable username-only)
11. Nav role-gating for HR submenu
12. Dynamic roles from DB (drop production reliance on `prakash` string)
13. Reconciliation of half-day **10:15** vs current **10:30/9h**
14. Discovery of reporting-manager source in SQL/ERP (mandatory before §5/§19)

---

## 20. Exact files that need modification (implementation phase)

### Must touch

| File | Why |
|------|-----|
| `POApprovalAPI/Services/HrAccessService.cs` | Roles from DB; manager scope; remove hardcode dependency |
| `POApprovalAPI/Services/HrSelfServiceService.cs` | Leave/WFH/confirm/credit/verify rules; holidays; auto PL |
| `POApprovalAPI/Services/HrReportsService.cs` | Attendance classify rule; dashboards; bulk HR views |
| `POApprovalAPI/Controllers/HrReportsController.cs` | Endpoints + auth; possibly split `/api/hr/*` |
| `POApprovalAPI/Program.cs` | DI; hosted service for monthly PL if approved |
| `POApprovalAPI/appsettings.json` (+ `.env.example`) | HrReports options beyond FullAccessUsers |
| `POApprovalAPI/Services/Databaseservice.cs` | Only if new connections/tables |
| `POApprovalAPI/Controllers/AuthController.cs` | EmpCode reliability / tokens if added |
| `src/routes/_app/hr-reports.tsx` | Split employee/HR UX or new routes |
| `src/lib/hr-reports-api.ts` | Clients; remove dead common-bank |
| `src/lib/feature-flags.ts` | Sync access with backend |
| `src/lib/auth-context.tsx` | HR role / EmpCode session |
| `src/components/AppShell.tsx` | Nav gating / HR submenu |

### Likely new (only after DB proof of gap)

- `src/routes/_app/hr/*.tsx` — dashboard, approvals inbox, directory
- `HrHierarchyService` / holiday / notification helpers
- Migration scripts for any **new** tables (ack-style, audit) following existing naming
- Hosted `BackgroundService` for monthly PL credit

### Supporting / do not treat as product runtime

- `docs/hr-v1-proposal.html`, this audit
- `POApprovalAPI/Scripts/ProbeHrEmpinfo/*` (schema probes; contain credentials — keep out of commits if not already ignored)

---

## Recommended implementation order (after this audit)

Aligned with brief §33, constrained by reuse:

1. **Discover hierarchy source** in payroll/ERP (manager columns, tables, or existing ERP screens) — block approval redesign until known  
2. **Reconcile half-day 10:15 vs 10:30/9h** with business owner  
3. Split **Employee vs HR dashboards** on existing APIs  
4. Wire **manager + HR approval** without breaking FullAccessUsers during transition  
5. Employee **confirmation** UX + keep CL-after-confirmation  
6. **Schedule** monthly 1.5 PL for eligible HO (reuse existing credit SQL; optionally compare to `dbo.leavecredit`)  
7. Holidays / verify leave date rules against legacy ERP  
8. Notifications via existing `EmailService` pattern  
9. Harden HR API auth  
10. E2E test matrix (brief §31)

---

## Fixed business rules checklist (do not change without explicit approval)

| # | Rule | Portal status |
|---|------|---------------|
| 1 | Half day after 10:15 — VERIFY | **Mismatch** — code 10:30/9h |
| 2 | Every employee apply leave via own login | Done (needs EmpCode) |
| 3 | HO &gt;1yr → monthly leave credit | Manual only |
| 4 | Confirmation form in HR ERP | HR process only; employee form missing |
| 5 | CL after confirmation workflow | Done on HR confirmation |
| 6 | 18 PL/yr → 1.5/month after 1yr | Constants + manual apply |
| 7 | WFH application | Done |
| 8 | Employees view attendance in ERP | Done |
| 9 | Month-end verify | Done (request → HR) |
| 10 | Leave next 3 days only | Done (FE+BE) |
| 11 | Existing leave system remains | Same `LeaveHistory` — preserve |
| 12 | Existing hierarchy respected | **Not yet** — hierarchy not wired |

---

## Audit conclusion

The codebase is ready for **incremental** HR Portal work on top of existing `LeaveHistory` / punches / `empinfo` / login. The highest-risk unknowns before coding are: **(A)** where reporting/approval hierarchy lives in SQL, and **(B)** the authoritative half-day cutoff (**10:15** vs **10:30/9h**).

**No major implementation should proceed until those two items are confirmed and this audit is accepted as the baseline.**
