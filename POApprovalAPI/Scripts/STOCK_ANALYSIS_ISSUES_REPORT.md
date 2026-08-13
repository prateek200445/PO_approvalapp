# Stock Analysis Report — Issues, Evidence & Fix Plan

**Handoff document for developers / Cursor agents**

| Field | Value |
|-------|-------|
| **Primary object** | `dbo.SP_STOCKANALYSIS_RPT_ALL` |
| **Opening function** | `dbo.FN_STOCKANALYSIS_RPT_ALL_OP` |
| **Database** | `MaterialProcessing` |
| **Server** | `103.240.33.122,5115` |
| **Analysis date** | 13 Aug 2026 |
| **SP last modified (live)** | 8 Aug 2025 |
| **Out of scope** | PO approval app, chatbot — ERP stock report only |

---

## Agent brief (read this first)

**Do not re-analyze from scratch.** This document + the SQL definition exports in this folder contain the full investigation.

### What to read

1. This file (`STOCK_ANALYSIS_ISSUES_REPORT.md`)
2. `SP_STOCKANALYSIS_RPT_ALL.definition.sql` — main report (~899 lines)
3. `FN_STOCKANALYSIS_RPT_ALL_OP.definition.sql` — opening balance function (~full)
4. `_opening_balance_dates.csv` — per-company opening snapshot dates (exact names)

### Golden test cases

| # | Company | Item / case | Period | What to verify |
|---|---------|-------------|--------|----------------|
| 1 | `Plastene India Limited ` (note trailing space) | WIP00004 | FY 25-26 | Negative opening; closing vs `WareHouse.StkInHand` |
| 2 | `Plastene India Limited ` | WIP00031 | FY 25-26 | MRN Purchase + wastage Warehouse Inwards same day |
| 3 | `Plastene India Limited ` | RM 715 / RAW05062 | Jun-2025 | MRN Purchase only; warehouse post has `transid ≠ 0` (excluded from report) |
| 4 | `Plastene Polyfilms Limited` | any | FY 25-26 | Opening snapshot is **after** report start → zero opening bug |
| 5 | `Oswal Extrusion Limited` | WIP00013 | FY 25-26 | Sanity vs godown stock |
| 6 | `HCP Plastene Bulkpack Ltd` | WIP00037 | any | Worst negative opening (−26.9L) |

### Business decisions pending (ask user / Finance before implementing)

| ID | Decision |
|----|----------|
| **ISS-07** | How to treat `WareHouseInwards` with `transid = 0` to **Wastage Godown** — exclude, separate column, or keep? |
| **ISS-10** | Re-enable commented **credit note** qty adjustment block in SP? |
| **Phase 1** | Who runs annual `OpeningBalanceCommoditywise` refresh — Finance or IT? |

### Fix order (mandatory)

```
Phase 1 — Data: refresh opening snapshots + fix negative OB items
Phase 2 — SP quick wins: stockjv filter, closing formula, remove DBCC
Phase 3 — SP logic: per-company opening, wastage handling, sync opening FN
Phase 4 — Validation: Cl.Factory Owned ≈ WareHouse.StkInHand
```

**Do not deploy SP-only fixes before Phase 1** — opening data errors will still produce wrong closing.

---

## Architecture overview

```
OpeningBalanceCommoditywise (base snapshot, often stale)
        +
FN_STOCKANALYSIS_RPT_ALL_OP (roll-forward movements → Op.Factory Owned)
        +
SP_STOCKANALYSIS_RPT_ALL period movements (@temp via UNION ALL)
        ↓
PIVOT by Type → columns (Purchase, Sales, Production, …)
        ↓
Cl.Factory Owned = Op + ins − outs (formula varies by dept block)
```

### Movement sources in `@temp` (main SP)

| Type column | Source table(s) | Notes |
|-------------|-----------------|-------|
| Purchase | `Vw_StoreInwards` | MRN; qty = `acceptedqty` or `netwt` if unit ≠ KGS |
| Warehouse Inwards | `WareHouseInwards` | **Only `transid = 0`** (~2,351 rows); mostly wastage/manual |
| Purchase (fallback) | `PurchaseVoucher` + items | Only if `StoreInwardNo NOT IN Vw_StoreInwards.SrNo` |
| Purchase (negative) | `vw_DebitNote` | `-SUM(QtyDifference)` where `DebitType = 'Qty Difference'` |
| Sales | `SalesVoucher` + items | Uses `Netwt` (not ActualQty since Apr 2024) |
| Production / Consumption | `vw_production_stk_FG`, `vw_consumption_stk_FG` | Can contain **negative qty** rows |
| JW movements | Despatch views, `Challan5AInward`, etc. | Separate closing buckets |
| Stock Adjustment | `Prod_RMD_InOut`, `WarehousetoWareHouse` | To `Stock Adjustment Entry` godown |
| stockjv | `stockjv` | **Bug:** `sysdate <= @DATEto` only (no `@DATEFROM`) |

### MRN vs warehouse — critical clarification

Normal purchase flow:

```
MRN (Vw_StoreInwards) → godown post (WareHouseInwards, transid ≠ 0)
```

| WareHouseInwards | Rows | In report? |
|------------------|------|------------|
| `transid = 0` | 2,351 | ✅ as **Warehouse Inwards** |
| `transid ≠ 0` | 1,380,814 | ❌ excluded |

So **standard purchases are not double-counted** in the SP. Duplicates users see are usually:

- **Wastage** rows (`transid = 0`, ToWareHouse = `Wastage Godown`, remarks like "OTHER WASTAGE") on same item/date as MRN
- **Detail view** showing multiple MRNo lines per item (line detail, not double stock)
- **Opening/closing math** wrong, not literal duplicate posting

**`MrnNo` on WareHouseInwards is blank** for FY 25-26 — dedupe by MRN link is not possible today.

---

## Issue register

### P0 — Critical

#### ISS-01: Stale / missing opening balance snapshots

**Symptoms:** Wrong Op.Factory Owned and Cl.Factory Owned; zero opening for valid FY.

**Root cause:** `OpeningBalanceCommoditywise` snapshot dates are old or **after** report `@DATEFROM`.

**Evidence:** See `_opening_balance_dates.csv`. Examples:

| Company | Snapshot date | Problem for FY 25-26 (@DATEFROM 2025-04-01) |
|---------|---------------|-----------------------------------------------|
| `Plastene India Limited ` | 2023-11-30 | Roll-forward ~16 months of movements |
| `Oswal Extrusion Limited` | 2023-12-31 | Same |
| `Plastene Polyfilms Limited` | **2026-03-31** | `OpeningBalanceDate <= 2025-03-31` → **no rows** |
| Global MIN (used in main SP line 32) | **2021-03-31** | Wrong for multi-company pre-sales filter |

**Fix:**

1. Annual refresh of `OpeningBalanceCommoditywise` per company at FY boundary (e.g. 2025-03-31 for FY 25-26).
2. Pre-report check: warn if no snapshot with `OpeningBalanceDate <= @DATEFROM - 1`.
3. Replace global `MIN(OpeningBalanceDate)` with per-company date in main SP (see ISS-05).

---

#### ISS-02: Negative opening balances in master

**Symptoms:** Op.Factory Owned starts negative; minus closing even when godown is positive.

**Evidence (live, Aug 2026):**

| Company | Items with neg OB | Total neg OB |
|---------|-------------------|--------------|
| HCP Plastene Bulkpack Ltd | 147 | −33.6L |
| Oswal Extrusion Limited | 21 | −12.1L |
| `Plastene India Limited ` | 34 | −9.5L |

Worst items: HCP `WIP00037` (−26.9L), Oswal `WIP00003` (−10.5L), PIL `WIP00004` (−2.9L).

**Fix:** Reconcile OB vs godown at snapshot date; correct via adjustment; add validation on new OB entry.

---

#### ISS-03: Closing ≠ physical stock (`WareHouse.StkInHand`)

**Example — PIL `WIP00004`, FY 25-26:**

| Component | Qty |
|-----------|-----|
| Opening (commodity table) | −288,804 |
| Purchase (MRN) | +4,779,532 |
| Production | +1,270,694 |
| Sales | −8,508,738 |
| Approx. report closing | ≈ −2.75M |
| Actual `SUM(WareHouse.StkInHand)` | **+65,777** |

**Fix:** Resolve ISS-01/02 first, then SP fixes; validate `Cl.Factory Owned ≈ StkInHand` per item.

---

### P1 — High (SP logic)

#### ISS-04: `stockjv` missing lower date bound

**Location:** `SP_STOCKANALYSIS_RPT_ALL.definition.sql` ~lines 306–309

```sql
-- Current (wrong):
WHERE sysdate <= @DATEto

-- Fix:
WHERE sysdate BETWEEN @DATEFROM AND @DATEto
```

**Evidence:** 103 global `stockjv` rows with `sysdate < 2025-04-01` leak into FY 25-26 report.

**Also check:** Same pattern in `FN_STOCKANALYSIS_RPT_ALL_OP` roll-forward section.

---

#### ISS-05: Global vs per-company opening date

**Location:** Main SP line 32 vs `FN_STOCKANALYSIS_RPT_ALL_OP` (per-company `OpeningBalanceDate`).

**Fix:** Use company-scoped opening date everywhere; fix `vw_SalesRegister` pre-sales filter (~line 260).

---

#### ISS-06: Inconsistent closing formula — Stock Adjustment

**Location:** Main SP closing blocks ~lines 529–540 (RM/SF/FG) vs ~607 (RM Consumables).

RM/SF/FG subtracts Stock Adjustment **after** net consumption bracket. RM Consumables subtracts **inside** consumption bracket.

**Fix:** One canonical closing formula; Finance sign-off; apply to all `@RptType` blocks.

---

#### ISS-07: MRN + Warehouse Inwards overlap (wastage)

**Location:** `Vw_StoreInwards` (Purchase) UNION `WareHouseInwards WHERE transid=0` (~lines 126–192).

**Evidence (PIL FY 25-26):** 6 item-days with both Purchase and Warehouse Inwards > 0; warehouse mostly **Wastage Godown**. Excluding wastage godown → **0 overlaps**.

**Fix options (business decision):**

- **A (recommended):** Exclude `ToWareHouse IN ('Wastage Godown','Stock Adjustment Entry')` from Warehouse Inwards; add separate column or type.
- **B:** Keep in column but exclude from closing formula.
- **C:** Populate `MrnNo` on warehouse rows and dedupe (requires ERP posting change — ISS-12).

---

#### ISS-08: Opening roll-forward compounding errors

**Location:** `FN_STOCKANALYSIS_RPT_ALL_OP.definition.sql` — full roll-forward CTE + pivot (~same UNION as main SP).

**Fix:** Apply ISS-04/07 fixes to function; consider dropping roll-forward when snapshot date = FY start.

---

### P2 — Medium

#### ISS-09: Minus values in report columns

| Column | Cause |
|--------|-------|
| Op.Factory Owned | Negative OB (ISS-02) |
| Purchase | Debit notes `-SUM(QtyDifference)` by design |
| Total Production | 1,853 rows with `qty < 0` in `vw_production_stk_FG` (FY 25-26, total −383K) |
| Net Production Own | `Total Production − Production Of JW` |
| Cl.Factory Owned | outs > ins + negative opening |

**Fix:** Data cleanup + optional UX split (adjustments vs gross). Do not blindly clamp to zero.

---

#### ISS-10: Credit note qty adjustment disabled

**Location:** Main SP ~lines 459–471 (commented out Sep 2024). Debit note purchase adjustment **is** active.

**Fix:** Uncomment/test with Finance or add symmetric sales adjustment.

---

#### ISS-11: Performance anti-patterns

**Location:** Main SP lines 15–17: `CHECKPOINT`, `DBCC DROPCLEANBUFFERS`, `DBCC FREEPROCCACHE`, `WITH RECOMPILE`.

**Fix:** Remove from production SP.

---

#### ISS-12: `MrnNo` not populated on warehouse inward

**Evidence:** FY 25-26 — `MrnNo` empty on warehouse inward rows across top companies.

**Fix:** ERP posting should set `WareHouseInwards.MrnNo` from MRN.

---

## Related database objects

| Object | Modified | Role |
|--------|----------|------|
| `SP_STOCKANALYSIS_RPT_ALL` | 2025-08-08 | **Primary** — use this |
| `FN_STOCKANALYSIS_RPT_ALL_OP` | (function) | Opening calculation |
| `SP_STOCKANALYSIS_RPT_ALL_OP` | 2022-06-27 | Legacy; commented out in main SP |
| `SP_STOCKANALYSIS_RPT_DTL` | 2021-09-08 | Detail variant — check if UI uses |
| `SP_STOCKANALYSIS_RPT_ALL_OLD` | 2021 | Do not use |
| `SP_STOCKANALYSIS_RPT_ALL_TEST` | 2022 | Test only |

### Key tables / views

- `OpeningBalanceCommoditywise` — opening snapshot
- `Vw_StoreInwards` / `StoreInwardsPayment` / `StoreInwards` — MRN
- `WareHouseInwards` — warehouse posting (`transid` discriminates manual vs system)
- `WareHouse` — current `StkInHand`
- `vw_production_stk_FG`, `vw_consumption_stk_FG` — production/consumption
- `vw_DebitNote` — purchase qty adjustments
- `stockjv` — internal stock JV

---

## Exact company names (from live DB)

Use **exact** strings in `StringArray` / filters (watch trailing spaces):

```
Plastene India Limited 
Oswal Extrusion Limited
Plastene Polyfilms Limited
HCP Plastene Bulkpack Ltd
K.P. WOVEN PRIVATE LIMITED
```

Full list: `_opening_balance_dates.csv`

---

## Validation SQL (for regression after fixes)

```sql
-- 1) Negative opening count by company
SELECT companyName, COUNT(*) AS neg_items, SUM(OpeningBalance) AS total_neg
FROM OpeningBalanceCommoditywise
WHERE OpeningBalance < 0
GROUP BY companyName
ORDER BY total_neg;

-- 2) Opening snapshot missing for FY 25-26 report start
SELECT c.companyName
FROM (SELECT DISTINCT companyName FROM OpeningBalanceCommoditywise) c
WHERE NOT EXISTS (
  SELECT 1 FROM OpeningBalanceCommoditywise o
  WHERE o.companyName = c.companyName
    AND o.OpeningBalanceDate <= '2025-03-31'
);

-- 3) MRN vs warehouse overlap (expect 0 after ISS-07 fix, excluding wastage)
-- See investigation queries in conversation; filter transid=0 and exclude Wastage Godown.

-- 4) stockjv leak check
SELECT COUNT(*) FROM stockjv
WHERE sysdate < '2025-04-01' AND sysdate <= '2026-03-31';
-- Expect 0 rows included in FY 25-26 report after ISS-04 fix.

-- 5) Item reconciliation (example)
SELECT w.ItemCode,
       SUM(w.StkInHand) AS physical,
       o.OpeningBalance AS snapshot_ob
FROM WareHouse w
LEFT JOIN OpeningBalanceCommoditywise o
  ON o.companyName = w.CompanyName AND o.ItemCode = w.ItemCode
WHERE w.CompanyName = 'Plastene India Limited '
  AND w.ItemCode = 'WIP00004'
GROUP BY w.ItemCode, o.OpeningBalance;
```

---

## Running the report (for testers)

```sql
DECLARE @co StringArray;
INSERT INTO @co VALUES ('Plastene India Limited ');  -- trailing space if DB has it

EXEC dbo.SP_STOCKANALYSIS_RPT_ALL
  @companyname = @co,
  @DATEFROM = '2025-04-01',
  @DATEto = '2026-03-31',
  @RptType = 0,   -- 0=summary, 1=item-wise, 2=date-wise
  @intOp = 0;
```

Note: Wrapping in `INSERT INTO #t EXEC ...` may fail with nested INSERT-EXEC; run directly in SSMS.

---

## Exported files in this folder

| File | Description |
|------|-------------|
| `STOCK_ANALYSIS_ISSUES_REPORT.md` | This document |
| `SP_STOCKANALYSIS_RPT_ALL.definition.sql` | Main SP export (Aug 2025 live) |
| `FN_STOCKANALYSIS_RPT_ALL_OP.definition.sql` | Opening function export (full) |
| `SP_STOCKANALYSIS_RPT_DTL.definition.sql` | Detail SP export |
| `SP_STOCKANALYSIS_RPT_ALL_OP.definition.sql` | Legacy OP SP export |
| `FN_STOCKANALYSIS_RPT_ALL_OP.tail.sql` | Partial export (superseded by full file) |
| `_opening_balance_dates.csv` | Company opening snapshot dates |

---

## Summary matrix

| ID | Issue | Severity | Fix type | Effort |
|----|-------|----------|----------|--------|
| ISS-01 | Stale/missing opening snapshots | P0 | Data + process | Medium |
| ISS-02 | Negative opening balances | P0 | Data cleanup | High |
| ISS-03 | Closing ≠ physical stock | P0 | Data + SP | High |
| ISS-04 | stockjv date filter | P1 | SP ~1 line | Low |
| ISS-05 | Global vs company opening date | P1 | SP | Medium |
| ISS-06 | Inconsistent closing formula | P1 | SP | Low–Med |
| ISS-07 | Wastage warehouse overlap | P1 | SP + business rule | Medium |
| ISS-08 | Opening roll-forward errors | P1 | Function | High |
| ISS-09 | Minus column values | P2 | Data + UX | Medium |
| ISS-10 | Credit note block disabled | P2 | SP | Low–Med |
| ISS-11 | DBCC every run | P2 | SP perf | Low |
| ISS-12 | MrnNo not on warehouse | P2 | ERP process | Medium |

---

## Changelog

| Date | Author | Notes |
|------|--------|-------|
| 2026-08-13 | Cursor analysis | Initial investigation; live DB probes; definition exports |
