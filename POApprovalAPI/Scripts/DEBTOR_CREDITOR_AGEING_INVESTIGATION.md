# Debtor / Creditor Ageing — ERP Investigation Handoff

**Date:** 2026-08-15  
**Database:** `MaterialProcessing` on `103.240.33.122,5115`  
**Chatbot today:** Ledger master only (`LedgerMaster.PendingBalance`) — **no ageing wired**

---

## Executive summary

| Question | Answer |
|----------|--------|
| Can the chatbot answer debtor/creditor ageing today? | **No** — not reliably |
| Does the ERP have ageing logic? | **Yes** — multiple SPs + `vw_BillWiseTransaction` |
| Best portal-parity source for **party-wise month buckets** | `sp_Representative_Outstanding_Pivot` |
| Best source for **one party, bill-wise overdue** | `sp_Overdue_Ledger` / `sp_Overdue_Ledger_SUMMARY` |
| Best **SELECT-only** path for chatbot (no EXEC) | Governed SQL on `vw_BillWiseTransaction` + `LedgerMaster.Under` |
| Misleading names to avoid | `sp_Agingreport_*` = **inventory/stock** ageing by item subgroup, not debtors |

---

## What the chatbot supports today (Ledger domain)

| Intent | Source | Status |
|--------|--------|--------|
| Ledger count under a group | `LedgerMaster` COUNT + `Under` filter | Wired |
| List account groups | `DISTINCT LedgerMaster.Under` | Wired |
| Named party **total** outstanding | `LedgerMaster.PendingBalance` | Wired |
| Full voucher statement | `sp_ac_LedgerSummary_BankRecoDate` | Portal only (`LedgerSummaryController`); blocked in chat (`SELECT`-only guard) |
| Debtor/creditor **ageing buckets** | — | **Not wired** |

---

## ERP group names (debtors vs creditors)

From live `LedgerMaster.Under` and `vw_Commonledgergrouping`:

### Debtors (use `@G3 = 'Sundry Debtors'` in pivot SP)

| G3 (top) | G4 (sub-groups under Sundry Debtors) |
|----------|--------------------------------------|
| Sundry Debtors | Debtors-Domestic |
| | Debtors-Overseas |
| | Debtors-Legal Cases |

### Creditors (use `@G3 = 'Trade Creditors'` — **not** "Sundry Creditors")

| G3 (top) | G4 (sub-groups under Trade Creditors) |
|----------|--------------------------------------|
| Trade Creditors | Creditors-RM |
| | Creditors - Stores & Consumables |
| | Creditors - Packing Materials |
| | Creditors - Capital Goods |
| | Creditors-Services |
| | Creditors-All Others |
| | Creditors-Cha/Transportation |
| | Creditors-Credit Cards |
| | Creditors-Overseas (Other Then RM) |

**Verified:** `sp_Representative_Outstanding_Pivot` with `@G3='Sundry Creditors'` returned **0 rows** for Oswal; `@G3='Trade Creditors'` returned **1359 rows**.

---

## Recommended ERP objects for ageing

### 1. `sp_Representative_Outstanding_Pivot` — **primary ageing summary**

Party-wise outstanding split into **time buckets** (dynamic pivot).

**Example (verified live):**

```sql
EXEC sp_Representative_Outstanding_Pivot
  @CompanyName  = 'Plastene India Limited',
  @ToDate       = '2026-08-15',
  @intPeriod    = 3,          -- 3 = monthly buckets (1=daily, 3=monthly)
  @Representive = NULL,
  @Currency     = 'Rs.',
  @G3           = 'Sundry Debtors',   -- or 'Trade Creditors'
  @G4           = NULL,               -- optional sub-group e.g. 'Debtors-Overseas'
  @IsLedger     = 1;
```

**Sample output (517 rows, PIL debtors):**

| LedgerName | Opening | April 2026 | May 2026 | … | Total |
|------------|---------|------------|----------|---|-------|
| 20 Microns Limited | 945,998 | 3,930,702 | 2,240,106 | … | 7,116,806 |

**Notes:**
- Same family as Sales Dashboard pivot SPs (dynamic SQL / pivot).
- `@intPeriod=1` creates **daily** columns (hundreds of columns — avoid in chat).
- `@intPeriod=3` creates **monthly** columns — suitable for summaries.
- Requires **EXEC** — cannot run through current chatbot `SqlGuardService`.

---

### 2. `sp_Overdue_Ledger` — single party, bill-wise ageing

Bill-level overdue with dynamic bucket columns.

```sql
EXEC sp_Overdue_Ledger
  @DateTo         = '2026-08-15',
  @companyname    = 'Plastene Polyfilms Limited',
  @ledgername     = 'Commercial Bag Company',
  @Currency        = 'Rs.',
  @Representative  = NULL,
  @IncludeZero     = 0,
  @Category        = NULL,
  @BankName        = 0,
  @LastTransaction = 0,
  @PaymentDays     = 0,
  @VoucherNo       = 0;
```

**Verified:** 55 rows for Commercial Bag Company (dynamic pivot columns).

---

### 3. `sp_Overdue_Ledger_SUMMARY` — single party total by currency

Lightweight summary (2 rows for Commercial Bag: USD Dr + Rs. Cr).

```sql
EXEC sp_Overdue_Ledger_SUMMARY
  @DateTo = '2026-08-15',
  @companyname = 'Plastene Polyfilms Limited',
  @ledgername = 'Commercial Bag Company',
  @Currency = 'Rs.',
  ... (same optional params as sp_Overdue_Ledger);
```

**Output columns:** `companyname`, `ledgername`, `billcurrency`, `Type` (Dr/Cr), `PendingAmount`.

---

### 4. `sp_OutstandingAll` — all parties in a group

```sql
EXEC sp_OutstandingAll
  @DateTo      = '2026-08-15',
  @companyname = 'Plastene India Limited',
  @Months      = 3,
  @GroupName   = 'Sundry Debtors',   -- verify exact group string
  @Type        = NULL;
```

**Verified:** 1273 rows for PIL + Sundry Debtors (dynamic pivot). Heavy — use TOP/filter in a wrapper or prefer pivot SP above.

---

### 5. `vw_BillWiseTransaction` — **SELECT-friendly bill lines**

~1.6M rows. Use for chatbot governed SELECT ageing.

**Columns:** `CompanyName`, `LedgerName`, `VoucherType`, `VoucherNo`, `VoucherDate`, `RefType`, `BillNo`, `BillDate`, `Amount`, `Currency`, `ExcRate`, `DueDate`, `companyId`, `LedgerID`, `ApprovalStatus`.

**Sample bill lines (Commercial Bag / Polyfilms):** export invoices with `BillDate`, `DueDate`, `Amount`, age in days.

**Prototype governed ageing (verified):**

```sql
SELECT TOP 50
  CompanyName,
  LedgerName,
  Under,
  SUM(CASE WHEN AgeDays BETWEEN 0 AND 30 THEN ABS(Amount) ELSE 0 END) AS Bucket_0_30,
  SUM(CASE WHEN AgeDays BETWEEN 31 AND 60 THEN ABS(Amount) ELSE 0 END) AS Bucket_31_60,
  SUM(CASE WHEN AgeDays BETWEEN 61 AND 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_61_90,
  SUM(CASE WHEN AgeDays > 90 THEN ABS(Amount) ELSE 0 END) AS Bucket_90_Plus
FROM (
  SELECT lm.CompanyName, lm.LedgerName, lm.Under,
         DATEDIFF(day, ISNULL(b.BillDate, b.VoucherDate), CAST(GETDATE() AS date)) AS AgeDays,
         b.Amount
  FROM LedgerMaster lm WITH (NOLOCK)
  JOIN vw_BillWiseTransaction b WITH (NOLOCK)
    ON b.CompanyName = lm.CompanyName AND b.LedgerName = lm.LedgerName
  WHERE lm.CompanyName = @Company
    AND lm.LedgerName LIKE @PartyLike
    AND lm.Under LIKE @DebtorOrCreditorUnder   -- 'Debtors%' or 'Creditors%'
) x
GROUP BY CompanyName, LedgerName, Under;
```

**Caveats:**
- Does not replicate forex / on-account logic inside `sp_Overdue_Ledger`.
- Must filter `CompanyName` + party or group + always `TOP`.
- Age basis: `BillDate` (or `VoucherDate` fallback); portal may use `DueDate` for overdue — confirm with finance users.

**Related views:** `vw_BillWiseTransaction_New`, `vw_BillWiseTransactionWithOnAccount` (SPs prefer the on-account variant).

---

## Objects that are NOT debtor/creditor ageing

| Object | Actual purpose |
|--------|----------------|
| `sp_Agingreport_SubgroupName` (+ `_90Days`, `_91to180Days`, …) | **Inventory** ageing by item `SubGroupName` (MRN/store inward FIFO) |
| `FinalAgingReportPivot_Results` | Staging/results for item ageing pivot |
| `sp_Automail_Debtors_Aging_Report` (+ variants) | Scheduled email jobs, not interactive chat |
| `ac_form_receivable` | GST/form receivable register (EntryId, PeriodFrom, TotalInvoice…) — not AR ageing |

---

## Portal vs chatbot architecture gap

```
User question → ChatOrchestrator → LLM SQL → SqlGuard (SELECT only) → DB
                                      ↓
                            EXEC sp_*  → BLOCKED

Ledger Summary page → LedgerSummaryController → LedgerSummaryService
                                              → EXEC sp_ac_LedgerSummary_BankRecoDate ✓
```

Ageing SPs have the **same constraint** as ledger summary: they need **EXEC** from a dedicated service, or a **governed SELECT** rewrite on `vw_BillWiseTransaction`.

---

## Suggested chatbot implementation (Phase 2)

### Tier A — Quick wins (SELECT, governed)

1. **`TryBuildPartyAgeingBucketsSql`** — one party, 0–30 / 31–60 / 61–90 / 90+ on `vw_BillWiseTransaction`.
2. **`TryBuildDebtorCreditorAgeingListSql`** — TOP 50 parties under `Debtors%` or `Creditors%` with total pending from `LedgerMaster` + optional bucket sums.
3. Catalog entries: `vw_BillWiseTransaction`, `vw_Commonledgergrouping`, governance rule for ageing.
4. Eval cases in `eval_ledger.ps1` or new `eval_ageing.ps1`.

### Tier B — Portal parity (service + EXEC)

1. New **`AgeingReportService`** (mirror `LedgerSummaryService`):
   - `QueryDebtorAgeingPivot(company, toDate, g3, g4?)` → `sp_Representative_Outstanding_Pivot`
   - `QueryPartyOverdueBills(company, ledger, toDate)` → `sp_Overdue_Ledger`
2. New API endpoint `/api/ageing/query` **or** hook inside `ChatOrchestratorService` when `LooksLikeAgeingQuestion` matches (bypass LLM + SqlGuard for known intents).
3. Return JSON rows to existing chat UI (same as SQL result grid).

### Tier C — Phrasing / intent detection

Trigger ageing governed path when message contains:

- `ageing`, `aging`, `overdue`, `outstanding by age`, `bucket`, `0-30`, `90 days`
- plus `debtor`, `creditor`, `customer`, `vendor`, `party`, `sundry`

Map:
- **debtors** → `Under LIKE 'Debtors%'` or `@G3='Sundry Debtors'`
- **creditors** → `Under LIKE 'Creditors%'` or `@G3='Trade Creditors'`

---

## Live verification log

| Test | Result |
|------|--------|
| `sp_Representative_Outstanding_Pivot` PIL + Sundry Debtors + intPeriod=3 | 517 rows; cols: LedgerName, Opening, monthly buckets, Total |
| Same SP Oswal + Trade Creditors | 1359 rows |
| Same SP Oswal + Sundry Creditors | **0 rows** (wrong G3) |
| `sp_Overdue_Ledger_SUMMARY` Commercial Bag / Polyfilms | 2 rows (USD Dr 10.98M, Rs. Cr -37k) |
| `sp_Overdue_Ledger` same party | 55 rows (bill-wise pivot) |
| `sp_OutstandingAll` PIL + Sundry Debtors | 1273 rows (heavy pivot) |
| `sp_Agingreport_SubgroupName` PIL + Sundry Debtors | **0 rows** (inventory SP — wrong domain) |
| SELECT bucket prototype on `vw_BillWiseTransaction` | Returns bucket sums for Commercial Bag |
| `vw_BillWiseTransaction` row count | 1,606,504 |

---

## Files to touch when implementing

| File | Change |
|------|--------|
| `ChatOrchestratorService.cs` | Ageing intent detection; early governed path |
| `ChatOrchestratorService.GovernedQueries.cs` | `TryBuildPartyAgeingSql`, etc. |
| `schema-catalog.json` | Add `vw_BillWiseTransaction`, grouping facts |
| `build_embeddings.py` | Regenerate embeddings |
| `eval_ledger.ps1` or `eval_ageing.ps1` | Golden tests |
| Optional: `AgeingReportService.cs` + controller | EXEC-based portal parity |

---

## Open questions for finance / ERP team

1. Age basis: **BillDate** vs **DueDate** vs **VoucherDate** for ageing buckets in MIS?
2. Confirm standard bucket definitions: 0–30 / 31–60 / 61–90 / 90+ vs **monthly** pivot (portal default)?
3. Which ERP screen uses `sp_Representative_Outstanding_Pivot` vs `sp_Overdue_Ledger` (menu name)?
4. Should export debtors use `Debtors-Overseas` sub-group or separate automail SPs (`sp_Export_Debtors_Due`)?

---

*Investigation only — no chatbot or ERP code modified.*
