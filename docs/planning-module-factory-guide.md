# Integrated Production Planning Module — Factory Manager Guide

**Document for:** Factory managers, planning incharge, FIBC & loom supervisors  
**Application:** PO Approval Portal — Planning tools  
**Unit (default):** Plastene India Limited (Unit -II)  
**Version:** August 2026  

---

## 1. Purpose of this module

This module helps you **plan production before work starts** and **track progress after planning**, across the full chain:

```
Fabric weaving (Loom)  →  Fabric ready  →  FIBC bag production (Lines)  →  Dispatch to customer
```

It connects to your existing ERP (SQL Server). For most actions it **reads** capacity and orders from ERP and can **write back** confirmed plans when save is enabled.

**What it replaces or improves:**
- Manual guessing of line/loom availability
- Separate Excel-based planning
- Late discovery that fabric or line capacity is missing before dispatch

---

## 2. How to open the module

1. Log in to the **PO Portal**.
2. Go to **Profile**.
3. Under **Tools**, you will see:

| Tool | Purpose |
|------|---------|
| **Planning Setup** | One-time / periodic configuration (admin & planning lead) |
| **Loom Planning** | Schedule fabric weaving on looms |
| **FIBC Line Planning** | Schedule bag production on FIBC lines |
| **Planning Timeline** | Single-order view: loom → fabric → FIBC → dispatch |
| **Planning Execution** | Plan vs actual production & bailing |

**Recommended order for a new order:** Setup (already done) → **Loom** → **FIBC** → **Timeline** (check) → **Execution** (during production).

---

## 3. Production sequence (big picture)

Every customer order passes through these stages:

| Stage | Where planned | ERP table (on confirm save) |
|-------|---------------|----------------------------|
| 1. Fabric weaving | Loom Planning | `Prod_LoomAlocationMaster` |
| 2. Fabric ready date | From BOM / marketing | — |
| 3. FIBC bag sewing | FIBC Line Planning | `prod_fibcallocationMaster` |
| 4. Dispatch | Marketing invoice date | — |

**Key buffers (defaults):**
- **FIBC:** Production must **finish 7 days before** dispatch date.
- **Loom:** Weaving must **finish 5 days before** the fabric-ready date (which feeds FIBC).

---

## 4. Page-by-page guide

### 4.1 Planning Setup (`/planning/setup`)

**Who uses it:** Planning admin, factory IT support, senior planning incharge  
**When:** Before daily planning; when lines/looms/teams change  

This is **Phase 0 — master data**. Nothing is planned here; you configure rules and pools.

#### Tabs

| Tab | What you configure | Effect on planning |
|-----|-------------------|-------------------|
| **Factory settings** | Dispatch buffer days, rejection %, planning enabled | FIBC capacity adjustment |
| **FIBC lines** | Line capacity (normal / single / double / triple dust), bag families, TeamNo, buffer override | Which lines accept which bag types; capacity per shift |
| **Loom pool** | Which looms are in planning; Domestic vs Export tag; **Planning pool** filter | Only included looms are used in Loom Planning |
| **Loom preference** | Fabric form × GSM × width → preferred loom & changeover tier | Ranks looms when several can weave the same fabric |
| **Team factors** | Contractor / in-house team performance (auto from ERP or manual) | Reduces effective FIBC line capacity for slower teams |
| **Downtime** | Line + shift capacity reduction on specific dates | Less free capacity on those days |
| **Backlog** | Reserved capacity on line + shift (unfinished work) | Less free capacity until backlog is cleared |

#### Loom pool — Domestic vs Export

Each loom is tagged:
- **DomesticFibc** — fabric for domestic FIBC production  
- **Export** — export fabric (not for domestic FIBC)  
- **Other / Maintenance** — special cases  

Use the **Planning pool** dropdown:
- **Domestic only** — include domestic looms, exclude export  
- **Export only** — include export looms only  
- **Domestic + export** — include both  

Tag each loom in the **Purpose** column, choose the filter, then click **Save pool**.

#### Contractors (FIBC lines)

On **FIBC lines** tab, assign **TeamNo** to each line. On **Team factors** tab:
- Link team performance (e.g. contractor team at **0.85** = 85% of nominal capacity)
- **Recalculate (30d)** pulls history from ERP production data

**Example:** Line 2 nominal capacity 400 pcs/shift × team factor 0.80 = **320 pcs** usable for planning.

---

### 4.2 Loom Planning (`/planning/loom`)

**Who uses it:** Weaving / fabric planning incharge  
**Purpose:** Book loom calendar time to produce required fabric meters before FIBC needs the cloth  

#### Sections on the page

| Section | What it does |
|---------|--------------|
| **Date range** | View existing loom allocations in ERP for selected dates |
| **Configuration** | Shows fabric buffer (5 days), max segment days, confirm-save status |
| **Order lookup** | Search order — view saved loom rows + BOM fabric (read-only) |
| **Plan order (preview)** | Main workflow: enter GSM, width, meters, fabric date → preview → save |
| **Loom register** | List of looms from ERP master |
| **Loom allocation grid** | All bookings in date range; click order for detail |
| **Production meters** | Actual output from ERP (`vw_Loom_Prod_Mtr`) |

#### Plan order workflow

1. Enter **Order / PO number** → system loads BOM (GSM, width, meters, dates).
2. Set **Fabric requirement date** (when fabric must be ready for FIBC).
3. Click **Preview allotment** — engine proposes loom segments (no ERP write).
4. Review **Proposed loom segments** and any **Orders to shift** table.
5. If fully allotted and save enabled → **Confirm & save** writes to ERP.

**Important:** Confirm always re-runs preview on the server before saving.

#### Loom allotment cases (i–vii)

The system classifies how fabric fits on each loom:

| Case | Description | Moves other orders? |
|------|-------------|---------------------|
| **i** | Similar fabric, forward in free gap | No |
| **ii** | Similar fabric, but blocking order in gap | **Yes** — on confirm |
| **iii** | Similar fabric, dissimilar order immediately after gap | **Yes** — on confirm |
| **iv** | Similar fabric, free days around existing block | May shift |
| **v** | Same width, GSM changeover (backward fill) | No |
| **vi** | Same GSM, width changeover (backward fill) | No |
| **vii** | Full GSM + width changeover (backward fill) | No |

Unlike FIBC, there is **no separate “Critical” panel** for loom — cases **ii/iii/iv** handle shifting within the normal **Plan order** flow.

**Similar fabric** = GSM within ±2, width within ±1 cm (defaults).

#### Limits

- Max **14 consecutive days** per loom segment (configurable).
- Max **4 loom changeovers per day** — preview warns; confirm **blocks** if exceeded.
- Only looms in **Loom pool** with **Include ✓** are considered.

---

### 4.3 FIBC Line Planning (`/planning/fibc`)

**Who uses it:** FIBC line planning incharge  
**Purpose:** Book line + date + shift slots for bag production before dispatch  

#### Sections on the page

| Section | What it does |
|---------|--------------|
| **Date range** | Load slot capacity grid from ERP |
| **Configuration** | Buffer days, shifts, feature flags (holds, critical shift, confirm save) |
| **Order lookup** | Order detail: saved plan, marketing plan, BOM fabric |
| **Plan order (preview)** | Standard allotment — uses free slots only |
| **Critical order (shift blocking orders)** | Urgent orders — may move other orders to free slots |
| **Quotation holds** | Temporary reservation for unconfirmed quotes (app database, 7 days) |
| **Production lines** | Line register from ERP |
| **Slot grid** | Full capacity view; filter free/partial/full; search orders |

#### Plan order — options

| Option | Meaning |
|--------|---------|
| **Order-wise** | Fill one preferred line first, then spill if needed |
| **Slot-wise** | Spread quantity across multiple lines |
| **Dust level** | Normal / Single / Double / Triple — adjusts effective line capacity |

#### Workflows

**A. Standard plan (confirmed order)**  
Preview → if **fully allotted** → Confirm & save → ERP `prod_fibcallocationMaster`

**B. Quotation hold (quote not yet confirmed)**  
Create hold → reserves slots in app for **7 days** → other previews see reduced capacity → later **Confirm hold** (writes ERP) or **Cancel**

**C. Critical order (rush / VIP)**  
Critical preview → may show **displacements** (other orders moved forward) → Critical confirm saves moves + new plan

**D. Replace existing plan**  
If order already has ERP rows, check **Replace existing** (when enabled) before confirm.

---

### 4.4 Planning Timeline (`/planning/timeline`)

**Who uses it:** Planning lead, factory manager  
**Purpose:** Read-only **one-screen check** for a single order  

Shows:
- Order summary (party, qty, bag type, dispatch)
- Milestone rail: **Loom weaving → Fabric ready → FIBC production → Dispatch**
- Loom allocation table
- FIBC slot table
- BOM fabric requirements
- **Warnings** (e.g. loom finishes too late, FIBC after dispatch, bailing gap)

Links to open Loom or FIBC planning for edits.

---

### 4.5 Planning Execution (`/planning/execution`)

**Who uses it:** Production supervisor, planning incharge during run  
**Purpose:** Compare **plan vs produced vs bailed**  

Shows for one order:
- Planned / Produced / Bailed / Pending quantities
- Bailing gap (produced but not yet bailed)
- Replan suggestions
- Line + shift totals
- Slot-level plan vs produced (same date match)
- ERP production log entries

Read-only — no calendar changes.

---

## 5. End-to-end worked example (with numbers)

This example uses **five fictional orders** and walks through **every major feature**.

### 5.0 Setup state (already configured)

**FIBC lines:**

| Line | Capacity/shift (Normal) | Team | Team factor |
|------|-------------------------|------|-------------|
| Line 1 | 500 pcs | TEAM-A (in-house) | 1.00 |
| Line 2 | 400 pcs | CONT-12 (contractor) | 0.85 → **340 pcs effective** |

**Loom pool:**

| Loom | Purpose | Included (Domestic only filter) |
|------|---------|--------------------------------|
| L3 | DomesticFibc | ✓ |
| L7 | DomesticFibc | ✓ |
| L9 | Export | ✗ |
| L12 | Export | ✗ |

**Backlog (Setup):** Line 1, Shift A, Aug 10 → **150 pcs** reserved (old unfinished work)

---

### 5.1 ORD-QUOTE — Quotation hold (600 bags, dispatch Aug 28)

**Customer:** Beta Industries (quote only — PO not confirmed)

| Item | Value |
|------|-------|
| Quantity | 600 bags |
| Dispatch | 28-Aug-2026 |
| FIBC must finish by | 28 − 7 = **21-Aug-2026** |

**Capacity check — Aug 10, Line 1, Shift A:**

```
Nominal capacity:     500 pcs
Backlog reserve:     −150 pcs
Available:            350 pcs
```

**Hold plan (preview):**

| Date | Line | Shift | Pcs |
|------|------|-------|-----|
| 10-Aug | L1 | A | 350 |
| 11-Aug | L1 | B | 250 |
| **Total** | | | **600** ✓ |

**Action:** FIBC → Quotation holds → **Create hold**  
**Result:** Slots reserved in portal for 7 days. **ERP unchanged.** Email alert may be sent.

---

### 5.2 ORD-MAIN — Full order (1,000 bags + 12,000 m fabric, dispatch Aug 28)

#### Step A — Loom planning (fabric first)

| BOM field | Value |
|-----------|-------|
| GSM | 170 |
| Width | 101.6 cm |
| Meters | 12,000 m |
| Fabric ready for FIBC | 21-Aug-2026 |
| Loom must finish by | 21 − 5 = **16-Aug-2026** |

**Loom calendar (L3):**

```
05-Aug – 09-Aug   FREE
10-Aug – 14-Aug   ORD-OTHER (blocking)
15-Aug – 20-Aug   FREE
```

**Preview result:**

| Loom | From | To | Meters | Case |
|------|------|-----|--------|------|
| L3 | 05-Aug | 09-Aug | ~4,000 | i |
| L3 | 10-Aug | 12-Aug | ~3,500 | ii |

**Orders to shift:**

| Order | Was | Move to |
|-------|-----|---------|
| ORD-OTHER | 10-Aug – 14-Aug on L3 | **15-Aug – 19-Aug** |

**Action:** Loom → **Confirm & save**  
**Result:** ORD-OTHER dates updated in ERP; ORD-MAIN rows inserted.

#### Step B — FIBC planning (bags)

Because ORD-QUOTE hold still active, Aug 10–11 slots show reduced free capacity for previews.

After Beta confirms PO → **Confirm quotation hold** → 600 pcs now in ERP.

**ORD-MAIN FIBC preview (1,000 bags, order-wise, Normal dust):**

| Date | Line | Shift | Pcs |
|------|------|-------|-----|
| 14-Aug | L2 | A | 340 (contractor effective cap) |
| 15-Aug | L2 | B | 340 |
| 16-Aug | L1 | A | 320 |
| **Total** | | | **1,000** ✓ |

**Action:** FIBC → Plan order → **Confirm & save**

---

### 5.3 ORD-URGENT — Critical FIBC (500 bags, dispatch Aug 25)

| Item | Value |
|------|-------|
| Dispatch | 25-Aug-2026 |
| Must finish by | **18-Aug-2026** |
| Quantity | 500 pcs |

**Normal FIBC preview:** Not enough free slots on 18-Aug (ORD-BLOCK occupies Line 1).

**Critical preview:**

**Displacements:**

| Blocker | From | To |
|---------|------|-----|
| ORD-BLOCK | 12-Aug L1-B | **22-Aug L1-B** |

**Proposed slots for ORD-URGENT:**

| Date | Line | Shift | Pcs |
|------|------|-------|-----|
| 16-Aug | L1 | A | 250 |
| 17-Aug | L1 | B | 250 |

**Action:** FIBC → Critical order → **Confirm shift & save**

---

### 5.4 Timeline check — ORD-MAIN

Planning Timeline → enter **ORD-MAIN**:

```
[Loom 05-Aug – 12-Aug] → [Fabric ready 21-Aug] → [FIBC 14-Aug – 16-Aug] → [Dispatch 28-Aug]
```

If loom ended after 16-Aug, warning would appear: *fabric buffer violated*.

---

### 5.5 Execution — ORD-URGENT (during production)

On **19-Aug**, open Planning Execution:

| Metric | Value |
|--------|-------|
| Planned | 500 pcs |
| Produced | 320 pcs |
| Bailed | 280 pcs |
| Pending | 180 pcs |
| Bailing gap | 40 pcs |

Use replan suggestions to adjust remaining slots if needed.

---

## 6. Feature summary matrix

| Feature | Loom | FIBC | Setup | Timeline | Execution |
|---------|:----:|:----:|:-----:|:--------:|:-----------:|
| Preview (no ERP write) | ✓ | ✓ | — | — | — |
| Confirm save to ERP | ✓ | ✓ | — | — | — |
| Shift other orders | ✓ (ii/iii/iv) | ✓ (Critical only) | — | — | — |
| Quotation hold | — | ✓ | — | — | — |
| Backlog reserve | — | ✓ (via Setup) | ✓ | — | — |
| Downtime | — | ✓ (via Setup) | ✓ | — | — |
| Team / contractor factor | — | ✓ (via Setup) | ✓ | — | — |
| Domestic / export loom pool | ✓ (via Setup) | — | ✓ | — | — |
| Order-wise / slot-wise | — | ✓ | — | — | — |
| Dust level capacity | — | ✓ | ✓ (line caps) | — | — |
| Replace existing plan | ✓ | ✓ | — | — | — |
| Plan vs actual | — | — | — | — | ✓ |
| End-to-end milestones | — | — | — | ✓ | — |

---

## 7. Standard operating workflow (daily)

### For a new confirmed customer order

1. **Planning Setup** — verify line capacities, loom pool, team factors are current (weekly/monthly).
2. **Loom Planning** — load order → preview fabric plan → confirm if fully allotted.
3. **FIBC Line Planning** — load order → choose order-wise/slot-wise & dust level → preview → confirm.
4. **Planning Timeline** — verify loom → fabric → FIBC → dispatch alignment.
5. During production → **Planning Execution** — monitor plan vs actual.

### For a quotation (PO not confirmed)

1. **FIBC Line Planning** → **Quotation holds** → create hold (must fully allot).
2. Inform sales: hold expires in **7 days**.
3. On PO confirmation → **Confirm hold** (writes ERP) or replan if dates changed.
4. On quote lost → **Cancel hold** (releases capacity).

### For a rush / VIP order

1. Try **FIBC Plan order** first (normal).
2. If partial/fail → **Critical order** panel → preview displacements → confirm shift & save.
3. Check **Timeline** for impact on dispatch.

---

## 8. Rules managers should know

| Rule | Detail |
|------|--------|
| **Preview ≠ Save** | Preview is safe; Confirm writes ERP (when enabled). |
| **Full allotment required** | Confirm blocked unless 100% quantity is planned. |
| **Existing plan** | Must tick **Replace existing** or clear ERP manually. |
| **Quotation hold** | Reduces capacity for all users until confirm/cancel/expiry. |
| **Backlog** | Reduces capacity on specific line+shift until cleared in Setup. |
| **Contractor lines** | Lower effective capacity via team factor — plan fewer pcs per shift. |
| **Export looms** | Excluded from domestic planning when pool filter = Domestic only. |
| **Loom changeovers** | More than 4/day blocks loom confirm save. |
| **Buffers** | 7 days (FIBC before dispatch), 5 days (loom before fabric ready). |

---

## 9. Glossary

| Term | Meaning |
|------|---------|
| **Allotment / Preview** | Computer-proposed schedule (dry run) |
| **Confirm & save** | Write proposed schedule to ERP |
| **Displacement / Shift** | Move another order’s booking to a new date/line/loom |
| **Quotation hold** | Temporary 7-day slot reservation (not in ERP until confirmed) |
| **Backlog** | Capacity reserved for unfinished work on a line+shift |
| **Team factor** | Multiplier on line capacity (contractor performance) |
| **Dust level** | Bag cleanliness level affecting line throughput |
| **Bailing gap** | Produced quantity not yet passed bailing/QC stage |
| **Pool purpose** | DomesticFibc vs Export loom classification |

---

## 10. Support

For portal access, configuration changes, or save-permission issues, contact IT / admin:

- **Phone:** +91 63574 19694  
- **Email:** gdmit@hpbl.in  

---

*This document describes the PO Portal integrated planning module as implemented for factory use. Configuration values (buffer days, hold days, changeover limits) may be adjusted in system settings by administrators.*
