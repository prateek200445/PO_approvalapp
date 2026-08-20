# Planning Module — Scenario-Wise Input Guide (Every Page, Every Field)

**Unit:** Plastene India Limited (Unit -II)  
**Audience:** Demo presenters, planning incharge, factory managers  
**Purpose:** Exact inputs to enter on each screen — nothing skipped  
**Date:** August 2026  

---

## How to open each page

1. Log in to PO Portal.
2. Go to **Profile**.
3. Under **Tools**, click the page you need:

| Tool | URL | Role |
|------|-----|------|
| Planning Setup | `/planning/setup` | Master data (once / when config changes) |
| Loom Planning | `/planning/loom` | Fabric weaving schedule |
| FIBC Line Planning | `/planning/fibc` | Bag production on lines |
| Planning Timeline | `/planning/timeline` | Single-order end-to-end view |
| Planning Execution | `/planning/execution` | Plan vs produced vs bailing |

**Recommended demo order:** Setup → Loom → FIBC → Timeline → Execution  
**Same order end-to-end:** Use **Script 5** with **`PO 9305/4LT-0775`**  

---

## Unit-II data notes (important before demo)

| Fact | What to do in the UI |
|------|----------------------|
| FIBC slot grid (`vw_fibclineplanning_NEW`) has data **Feb 2025 – Dec 2025 only** | Set FIBC **From/To** to `2025-09-01` – `2025-12-31` (not Aug 2026) |
| Marketing dispatch dates are mostly invalid (`1900-01-01`, qty 0) | **Manually enter** dispatch date + quantity on FIBC panels |
| Loom allocations exist for 2026 orders | Loom date range can stay default (last 30 days → today) or widen to cover Aug 2026 |
| Only one saved FIBC plan in ERP | `PO 9305/4LT-0775` — use for “view existing plan” |
| Backlog table is empty | Add one row in Setup → Backlog before FIBC demo |

### Demo order numbers (from live DB)

| Order | Use for | Loom rows | FIBC saved | BOM body |
|-------|---------|-----------|------------|----------|
| `8500585065/157602` | Loom **partial** preview + Timeline | 4 | 0 | ~6077 m, GSM 162+25, width 98 |
| `PPL-66/2026/BIG BAGS/ITEM1` | Loom displacement / replace demo | 7 | 0 | ~6722 m, GSM 142, width 102 |
| `23129/PTS-RC` | Loom **full** preview + optional save | 1 | 0 | ~9939 m, GSM 182+25, width 107 |
| `PO 9305/4LT-0775` | **End-to-end same order** (Loom save + FIBC + Timeline + Execution) | 0 | 4 | BOM + 1170 pcs FIBC plan |
| `DEMO-Q-001` (new) | Quotation hold | — | — | enter all manually |

**Important (loom preview, validated Aug 2026):** None of the large BOM orders (~6k–10k m) show **Fully allotted: Yes** when the fabric date is **2026-09-01** and the demo is run **on or after ~2026-08-20**. The engine only plans from **today** until **fabric date − 5-day buffer** (~7 days in that case). Use **Scenario L1** to show realistic partial + warnings; use **Scenario L2** for a guaranteed full preview.

---

# PAGE 1 — Planning Setup (`/planning/setup`)

**Who:** Planning admin / incharge  
**When:** Before first demo; optional tweaks during demo  

---

## 1.1 Factory selector (top panel — always visible)

| # | Field / control | Type | Required | What to enter | Notes |
|---|-----------------|------|----------|---------------|-------|
| 1 | **Search factory name or group…** | Text input | No | Type `Plastene` or `Unit -II` | Filters factory list |
| 2 | **Factory list row** | Click button | Yes (once) | Click **Plastene India Limited (Unit -II)** | Highlights row; sets active factory |
| 3 | **Active factory** (read-only) | Display | — | Shows selected company name | Confirms unit |
| 4 | Planning enabled / Dispatch buffer (read-only summary) | Display | — | Should show **Yes**, **7 days** | From saved config |

**Demo step:** Search `Plastene` → click **Plastene India Limited (Unit -II)**.

---

## 1.2 Tab: Factory settings

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Planning enabled** | Checkbox | Yes | ☑ checked | Must be on for all planning |
| 2 | **Default dispatch buffer (days)** | Number | Yes | `7` | FIBC must finish this many days before dispatch |
| 3 | **Default rejection % (planning haircut)** | Number (0–50, step 0.1) | Yes | `2.5` | Reduces effective capacity |
| 4 | **Notes** | Text | No | `Unit-II demo Aug 2026` | Optional |
| 5 | **Save factory settings** | Button | — | Click after edits | Writes `PlanningFactoryConfig` |

**Demo scenario A — Verify factory (no change):** Open tab → confirm values above → no save needed.

**Demo scenario B — Show buffer change:** Change buffer to `8` → Save → explain effect on FIBC target date → change back to `7` → Save.

---

## 1.3 Tab: FIBC lines

| # | Field (per line row) | Type | Required | Demo action | Notes |
|---|----------------------|------|----------|-------------|-------|
| 1 | **Line** | Read-only | — | Lines 1–8 | From ERP `NewLineMaster` |
| 2 | **ERP bag type** | Read-only | — | UPanel, Buffle, Circular, etc. | |
| 3 | **Bag families** (UPanel / Buffle / Circular) | Checkbox group | Per line | Leave as imported | Controls which bag types line accepts |
| 4 | **Normal** | Number | Per line | e.g. Line 1: `450` | Capacity pcs/shift, normal dust |
| 5 | **1-dust** | Number | Optional | As imported | Single dust capacity |
| 6 | **2-dust** | Number | Optional | As imported | Double dust |
| 7 | **3-dust** | Number | Optional | As imported | Triple dust |
| 8 | **Buffer** | Number | Optional | Blank = use factory default (7) | Per-line override |
| 9 | **TeamNo** | Text | Optional | e.g. `T1` on Line 1 | Links to team factors |
| 10 | **Active** | Checkbox | Yes | ☑ all active lines | Inactive lines excluded |
| 11 | **Import from ERP** | Button | — | Click if lines empty | Pulls from `NewLineMaster` |
| 12 | **Save lines** | Button | — | Click after edits | Writes `PlanningLineConfig` |

**Demo scenario — Show line config:** Open tab → point at Line 1 UPanel 450 capacity → optionally tick **Buffle** on one line → Save.

---

## 1.4 Tab: Loom pool

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Planning pool** (dropdown) | Select | Yes | `Domestic only` | Options: Domestic only / Export only / Domestic + export |
| 2 | **Filter looms…** | Text | No | e.g. `LSL` or loom number | Filters table only |
| 3 | Per loom — **#** | Read-only | — | Loom number | |
| 4 | Per loom — **Code / Make** | Read-only | — | ERP data | |
| 5 | Per loom — **Type** | Text input | Optional | e.g. `LSL` | Loom type for engine |
| 6 | Per loom — **Include** | Checkbox | Yes | ☑ for planning looms | Only checked looms used in Loom Planning |
| 7 | Per loom — **Purpose** | Select | Yes | `DomesticFibc` or `Export` | DomesticFibc / Export / Other / Maintenance |
| 8 | Per loom — **Winder** | Select | Yes | `Tube` / `FlatDouble` / `FlatTriple` | |
| 9 | Per loom — **Width cm** | Read-only | — | Min–max from ERP | |
| 10 | **Save pool** | Button | — | Click after edits | Writes `PlanningLoomPool` |

**Demo scenario — Domestic vs Export:**
1. Pick 2 looms → set **Purpose** = `Export`.
2. Set **Planning pool** = `Domestic only` → Export looms auto-uncheck **Include**.
3. **Save pool**.
4. Explain: Loom Planning engine only sees included looms.

---

## 1.5 Tab: Loom preference

| # | Field (per chart row) | Type | Required | Demo value | Notes |
|---|------------------------|------|----------|------------|-------|
| 1 | **Form** | Select | Yes | `Tube` or `Flat` | Fabric form |
| 2 | **GSM** (min / max) | Two numbers | Yes | e.g. 140 / 180 | Range |
| 3 | **Width cm** (min / max) | Two numbers | Yes | e.g. 90 / 110 | Range |
| 4 | **Rank** | Number (min 1) | Yes | `1` = best | Preference order |
| 5 | **Loom type** | Text | Yes | e.g. `LSL` | Uppercased on save |
| 6 | **Winder** | Select | Yes | `Tube` etc. | |
| 7 | **Tier** | Select | Yes | `Blue` or `White` | Changeover tier |
| 8 | **Notes** | Read-only | — | From seed data | |
| 9 | **Save chart** | Button | — | Click after edits | Used by loom cases i–iv |

**Demo scenario — View only:** Open tab → show one row matching order GSM/width → no save unless editing.

---

## 1.6 Tab: Team factors

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Recalculate (30d)** | Button | — | Click to pull ERP history | From `FIBCTeamWiseProduction` |
| 2 | Per row — **Line / Shift / Team** | Read-only | — | — | |
| 3 | Per row — **Auto** | Read-only | — | e.g. 1.000 | Calculated factor |
| 4 | Per row — **Manual** | Number (step 0.01) | Optional | e.g. `0.85` | Overrides auto |
| 5 | Per row — **Effective** | Read-only | — | Used in FIBC capacity | |
| 6 | **Save factors** | Button | — | After manual edit | |

**Demo scenario — Slower contractor:** Find team row → set **Manual** = `0.90` → Save → explain reduced line capacity.

---

## 1.7 Tab: Downtime

### Add form (top row)

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Date** | Date picker | Yes | `2025-10-15` | Plan date |
| 2 | **Line # (0 = all)** | Number | Yes | `3` or `0` | 0 = all lines |
| 3 | **Shift (blank = all)** | Text | No | `A` or blank | A/B/C |
| 4 | **Factor** | Number 0–1 | Yes | `0` = line down, `0.5` = half | |
| 5 | **Reason** | Text | No | `Maintenance demo` | |
| 6 | **Add to list** | Button | — | Adds to draft table | Does not save until below |

### Saved list (editable)

| # | Field | Type | Notes |
|---|-------|------|-------|
| 1 | **Factor** (in table) | Number | Edit per row |
| 2 | **Remove** | Button | Deletes row |
| 3 | **Save downtime** | Button | Persists all rows |

**Demo scenario — Half shift on Line 2:**
- Date `2025-10-20`, Line `2`, Shift `B`, Factor `0.5`, Reason `PM maintenance` → Add to list → Save downtime.

---

## 1.8 Tab: Backlog

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Line #** | Number | Yes | `1` | |
| 2 | **Shift (A/B/C)** | Text | Yes | `A` | |
| 3 | **Order no** | Text | Yes | `PO 9305/4LT-0775` | Any open order on that line |
| 4 | **Backlog qty** | Number | Yes | `150` | Pcs not yet finished |
| 5 | **Reason (optional)** | Text | No | `Incomplete from prior shift` | Full-width field below row |
| 6 | **Add backlog** | Button | — | Creates open backlog row | |
| 7 | **Clear** (on existing row) | Button | — | Closes backlog | |

**Demo scenario — Must do before FIBC demo:**
- Line `1`, Shift `A`, Order `PO 9305/4LT-0775`, Qty `150`, Reason `Demo backlog` → **Add backlog**.

---

# PAGE 2 — Loom Planning (`/planning/loom`)

**Purpose:** Schedule fabric weaving on looms (writes `Prod_LoomAlocationMaster` on confirm).

---

## 2.1 Top row — three panels

### Panel A: Date range

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **From** | Date picker | Yes | Default: today − 30 days | Or `2026-07-01` to see Aug allocations |
| 2 | **To** | Date picker | Yes | Default: today | |
| 3 | **Refresh grid** | Button | — | Click after date change | Reloads allocation grid |

### Panel B: Configuration (read-only)

| Display field | Unit-II value |
|---------------|---------------|
| Company | Plastene India Limited (Unit -II) |
| Fabric buffer | 5 days |
| Max segment | 14 days |
| Confirm save | Enabled |
| Looms registered | ~93 |

### Panel C: Order lookup

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **PO / buyer order no.** | Text | Yes | `8500585065/157602` | |
| 2 | **Search** button (or Enter) | Button | — | Click | Opens order detail panel below |

**Note:** Order lookup text also filters the **allocation grid** (debounced).

---

## 2.2 Plan order (preview) — main input panel

| # | Field | Type | Required | Demo value (8500585065/157602) | Notes |
|---|-------|------|----------|----------------------------------|-------|
| 1 | **Order / PO no.** | Text | Yes | `8500585065/157602` | Tab out / blur loads BOM context automatically |
| 2 | **GSM** | Number/text | Yes | `162` | Auto-filled from BOM. ERP often stores `162+25` — **fix manually to `162`** (auto-fill may show `16225` if `+` is stripped) |
| 3 | **Width (size cm)** | Number | Yes | `98` | From BOM `FabricSize` |
| 4 | **Required meters** | Number | Yes | `6077` | From BOM `TotalMtr`. For **full preview + save (L2)** use **`300`** on `23129/PTS-RC` (not 1200 — changeover limit) |
| 5 | **Fabric requirement date (FIBC ready)** | Date picker | Yes | See scenario below | Engine backs off **5 days** for weaving; max **30-day** planning horizon before that |
| 6 | **Replace existing plan on confirm save** | Checkbox | If order has plan | ☐ unless re-planning | Only if existing loom rows + replace enabled |
| 7 | **Preview allotment** | Button | — | Click | Shows proposed segments + displacements |
| 8 | **Confirm & save** | Button | After successful preview | Click → confirm dialog | Writes ERP + shifts blocking orders |

**Auto-load behaviour:** When order number loses focus, BOM context loads: GSM, width, meters, fabric date fill in if ERP has data.

### Planning window (why partial vs full)

| Setting | Unit-II value |
|---------|---------------|
| Fabric buffer | 5 days before FIBC-ready date |
| Max planning horizon | 30 days before fabric completion |
| Earliest start | **Today** (cannot plan in the past) |
| Max segment per loom | 14 days |
| Max changeovers/day | 4 (save blocked if exceeded — **each new loom segment start on same day counts as 1**) |

**Changeover rule (validated Aug 2026):** Fully allotted is not enough for save. If the preview starts **more than 4 loom segments on the same day**, save is blocked. For `23129/PTS-RC` + fabric **2026-10-30**, keep **required meters ≤ 300** (1200 m → 15 looms start same day → blocked).

**Example (demo day ≈ 2026-08-20):**

| Fabric date | Fabric completion (−5 d) | Planning days (today → completion) | ~6k m full BOM |
|-------------|--------------------------|-------------------------------------|----------------|
| 2026-09-01 | 2026-08-27 | **~7 days** | **Partial** (~1,800 m) |
| 2026-10-30 | 2026-10-25 | **~30 days** | **Full + save OK** if meters **≤ 300** on `23129/PTS-RC` (L2) |
| 2026-12-01 | 2026-11-26 | **~30 days** (capped) | May still be partial if grid busy |

**What to say when partial:** “The engine shows what fits in the real window — not a broken preview.”

**What to say when changeover blocked:** “Save is protected — too many loom changeovers on one day (max 4). Preview still valid.”

### Confirm save dialog

| # | Control | Action |
|---|---------|--------|
| 1 | Dialog text | Shows segment count + displacement count |
| 2 | **Cancel** | Abort |
| 3 | **Confirm & save** | Execute save |

---

## 2.3 Loom register (read-only table)

No inputs — scroll to show loom count, make, size range, Frozen/Active status.

---

## 2.4 Loom allocation grid

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Filter loom no / code** | Text (header) | No | e.g. `45` | Filters grid |
| 2 | **Order** column link | Click | No | Click order number | Opens order detail |

Order lookup box (section 2.1) also filters this grid by order/party.

---

## 2.5 Production meters (read-only)

No inputs — shows `vw_Loom_Prod_Mtr` shift A/B output for date range.

---

## 2.6 Order detail panel (after lookup)

| # | Control | Action |
|---|---------|--------|
| 1 | **Close** | Dismiss panel |
| — | Loom allocations table | Read-only |
| — | BOM fabric requirements | Read-only |

---

### Loom demo scenarios (step-by-step)

#### Scenario L1 — Partial preview + warnings (real plant constraint) — **no save**

Shows how the engine handles a large order with a tight fabric date on a busy grid.

1. **From:** `2026-07-01`, **To:** `2026-08-20` (or today) → **Refresh grid**.
2. Order lookup: `8500585065/157602` → Search (read-only detail panel).
3. **Plan order** panel — enter same order, tab out → auto-load BOM.
4. Fix **GSM** to `162` (not `16225`). Verify width `98`, meters `6077`.
5. **Fabric requirement date:** `2026-09-01`.
6. Click **Preview allotment**.
7. **Expected:** `Partial preview: ~1,800 of 6,077 m allotted`, **Fully allotted: No**, possible displacement on L27, warnings:
   - `Changeover blocked on save: 2026-08-20 has … changeover(s) (max 4/day)`
   - `Could not allot remaining … m within 14-day segment limits`
8. Walk **Proposed loom segments** (Case i) and **Orders to shift** if shown.
9. Do **not** click Confirm & save.

**Say:** “Preview is free — planner adjusts dates or meters, then previews again.”

---

#### Scenario L2 — Full preview + save (Fully allotted, no changeover block) — **use this for live demo**

Validated on live Unit-II grid (Aug 2026). **Order number is fine — meters must be ≤ 300.**

1. **From:** `2026-08-01`, **To:** `2026-11-30` → **Refresh grid**.
2. **Plan order** — order: **`23129/PTS-RC`** → tab out → auto-load.
3. Set fields:

| Field | Value |
|-------|--------|
| GSM | `182` (fix if auto-fill shows `18225`) |
| Width | `107` |
| Required meters | **`300`** ← override BOM (~9939); **do not use 1200** (triggers changeover block) |
| Fabric requirement date | **`2026-10-30`** |

4. Click **Preview allotment**.
5. **Expected:** **Fully allotted: Yes**, **300 / 300 m**, ~7 segments on ~4 looms, **no changeover warnings**.
6. ☑ **Replace existing plan on confirm save** (order has 1 ERP row) → **Confirm & save** → refresh grid.
7. **Optional clean save (0 existing ERP rows):** order **`5491`**, GSM `92`, width `56`, meters **`150`**, fabric **`2026-11-15`** — override auto-filled meters (BOM shows 544,000 m).

**Say:** “BOM had 9,900 m total; we’re booking a **300 m fabric release**. Engine fully allots within changeover limits, then writes to ERP.”

---

#### Scenario L2a — Full preview only (shows changeover guard) — optional

Same as L2 but set meters to **`1200`**. **Fully allotted: Yes** but changeover warnings on 2026-09-25 — **Confirm & save disabled on server**. Use to explain the 4 changeovers/day rule; do not use for save demo.

---

#### Scenario L2b — Alternate order, clean save at 150 m

| Field | Value |
|-------|--------|
| Order | **`8500585065/157602`** |
| GSM / width | `162` / `98` |
| Required meters | **`150`** |
| Fabric date | **`2026-11-15`** |
| Replace existing | ☑ (4 ERP rows) |

**Expected:** Fully allotted, no changeover warnings (validated).

---

#### Scenario L2c — Try full BOM meters (optional, not guaranteed)

For audiences who ask “can it do the whole order?”

1. Order `8500585065/157602`, GSM `162`, width `98`, meters **`6077`** (full BOM).
2. **Fabric date:** `2026-12-01` or later.
3. Grid **From** `2026-08-01`, **To** `2026-12-31`.
4. Preview — may still be **partial** if grid is congested; use to show limits honestly.

---

#### Scenario L3 — Replace existing plan

1. Order with many existing rows: `PPL-66/2026/BIG BAGS/ITEM1` (7 rows).
2. Use **L2-style** inputs (reduced meters + later fabric date) if you need full preview first.
3. Check **Replace existing plan on confirm save**.
4. Preview → Confirm & save only if **Fully allotted: Yes** and no changeover block.

---

#### Scenario L4 — Displacement walkthrough (partial is OK)

Same inputs as **L1** on `8500585065/157602` or `PPL-66/2026/BIG BAGS/ITEM1` with tight fabric date — focus on **Orders to shift** table and case labels, not full allotment.

---

# PAGE 3 — FIBC Line Planning (`/planning/fibc`)

**Purpose:** Schedule bag production on FIBC lines (writes `prod_fibcallocationMaster` on confirm).

**Critical:** Set date range to **2025-09-01 – 2025-12-31** (ERP grid has no Aug 2026 slots).

---

## 3.1 Top row — three panels

### Panel A: Date range

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **From** | Date picker | Yes | `2025-09-01` | Must overlap ERP grid data |
| 2 | **To** | Date picker | Yes | `2025-12-31` | |
| 3 | **Refresh grid** | Button | — | Click | |

### Panel B: Configuration (read-only)

| Display field | Unit-II value |
|---------------|---------------|
| Buffer days | 7 |
| Active shifts | A, B (from ERP capacity) |
| Shift preference | C → B → A |
| Confirm save | Enabled |
| Replace existing | Enabled |
| Quotation holds | 7 days |
| Hold email alerts | Enabled |
| Critical order shift | Enabled |
| Critical shift email | Enabled |

### Panel C: Order lookup

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **PO / buyer order no.** | Text | Yes | `PO 9305/4LT-0775` | Opens modal overlay |
| 2 | **Search** / Enter | Button | — | Click | Shows saved allocations + BOM |

---

## 3.2 Plan order (preview) panel

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Order / PO number** | Text | Yes | `FIBC-DEMO-001` | New order for clean demo |
| 2 | **Load** | Button | — | Optional | Pulls marketing/BOM if order exists |
| 3 | **Quantity (pcs)** | Number | Yes | `2000` | Manual — marketing qty invalid in Unit-II |
| 4 | **Bag type (ERP)** | Text | Yes | `UPanel` | Must match line families (UPanel/Buffle/Circular) |
| 5 | **Dispatch date** | Date picker | Yes | `2025-12-15` | Manual — marketing invalid |
| 6 | **Allotment mode** | Select | Yes | `Order-wise (one line first)` or `Slot-wise (spread lines)` | |
| 7 | **Dust capacity** | Select | Yes | `Normal` / Single / Double / Triple | Affects capacity column used |
| 8 | **Replace existing plan on confirm save** | Checkbox | If re-planning | ☐ | When order already in ERP |
| 9 | **Preview allotment** | Button | — | Click | |
| 10 | **Confirm & save** | Button | After full preview | Click → dialog | |

**Confirm dialog:** Cancel or **Confirm save** → inserts into `prod_fibcallocationMaster`.

---

## 3.3 Critical order (shift blocking orders) panel

*Shown only when CriticalShiftEnabled = true.*

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Critical order / PO number** | Text | Yes | `FIBC-CRIT-001` | |
| 2 | **Load** | Button | — | Optional | |
| 3 | **Quantity (pcs)** | Number | Yes | `3000` | |
| 4 | **Bag type (ERP)** | Text | Yes | `UPanel` | |
| 5 | **Dispatch date** | Date picker | Yes | `2025-11-01` | Urgent — near grid dates |
| 6 | **Reason (optional)** | Text | No | `Customer escalation — demo` | |
| 7 | **Pin to target date only** | Checkbox | No | ☑ for shift demo | Forces slots on dispatch−7 only; triggers displacements |
| 8 | **Replace existing plan** | Checkbox | If needed | ☐ | |
| 9 | **Preview critical shift** | Button | — | Click | Shows orders to shift + proposed slots |
| 10 | **Confirm shift & save** | Button | After success | Click → dialog | Moves blocking orders + saves critical plan |

---

## 3.4 Quotation holds panel

*Shown only when QuotationHoldEnabled = true.*

### Create hold form

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Order / PO number** | Text | Yes | `DEMO-Q-001` | Must be unique vs active holds |
| 2 | **Load** | Button | — | Skip for new order | |
| 3 | **Quantity (pcs)** | Number | Yes | `1500` | |
| 4 | **Bag type (ERP)** | Text | Yes | `Buffle` | |
| 5 | **Dispatch date** | Date picker | Yes | `2025-12-20` | |
| 6 | **Notes (optional)** | Text | No | `Awaiting customer PO` | |
| 7 | **Create quotation hold** | Button | — | Click | Reserves slots in app DB for ~7 days |

### Active holds list (per hold card)

| # | Control | Action |
|---|---------|--------|
| 1 | **Refresh** | Reload holds list |
| 2 | **Confirm** | Opens dialog → save to ERP |
| 3 | **Cancel hold** | Release reserved slots |

### Confirm hold dialog

| # | Field | Type | Demo |
|---|-------|------|------|
| 1 | **Replace existing ERP plan if order already has allocations** | Checkbox | ☐ |
| 2 | **Confirm & save to ERP** | Button | Click to convert hold → ERP |

---

## 3.5 Production lines (read-only)

Table: Line, Bag type, Capacity, Dust level, Buffer — no inputs.

---

## 3.6 Slot grid

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **all / free / partial / full** | Filter buttons | No | Click `free` | Shows empty capacity |
| 2 | **Order** cell link | Click | No | Click order no. | Opens order detail modal |
| — | (Order lookup filters grid) | Text | No | Same as 3.1 Panel C | Filters by order/party/bag |

---

## 3.7 Order detail modal (overlay)

| # | Control | Action |
|---|---------|--------|
| 1 | **X Close** | Dismiss |
| — | Saved allocations | Read-only |
| — | Marketing line plan | Read-only |
| — | Fabric requirements (BOM) | Read-only |
| — | **Open BOM report →** | Link to `/bom/{order}` |

---

### FIBC demo scenarios (step-by-step)

#### Scenario F1 — View slot grid + existing plan

1. **From:** `2025-09-01`, **To:** `2025-12-31` → Refresh grid.
2. Click filter **free** → show available slots.
3. Order lookup: `PO 9305/4LT-0775` → Search → review 4 saved allocations in modal.

#### Scenario F2 — Standard plan (preview + save)

1. Plan order panel:
   - Order: `FIBC-DEMO-001`
   - Qty: `2000`
   - Bag type: `UPanel`
   - Dispatch: `2025-12-15`
   - Allotment mode: **Order-wise**
   - Dust: **Normal**
2. **Preview allotment** → verify proposed slots table (date, line, shift, qty).
3. If fully allotted → **Confirm & save** → confirm dialog → save.
4. Refresh grid → filter **partial** or search order.

#### Scenario F3 — Slot-wise spread

Same as F2 but **Allotment mode** = `Slot-wise (spread lines)` → Preview → compare multi-line spread vs F2.

#### Scenario F4 — Quotation hold → confirm

1. Quotation holds:
   - Order: `DEMO-Q-001`
   - Qty: `1500`, Bag: `Buffle`, Dispatch: `2025-12-20`, Notes: `Demo hold`
2. **Create quotation hold** → see hold in active list with slot lines.
3. Explain: other previews subtract this capacity for 7 days.
4. Click **Confirm** on hold card → dialog → **Confirm & save to ERP**.

#### Scenario F5 — Critical shift (displacement)

1. Critical panel:
   - Order: `FIBC-CRIT-001`
   - Qty: `3000`, Bag: `UPanel`, Dispatch: `2025-11-01`
   - Reason: `Urgent dispatch`
   - ☑ **Pin to target date only**
2. **Preview critical shift** → show **Orders to shift** table.
3. **Confirm shift & save** (if fully allotted).

#### Scenario F6 — Backlog effect

1. Ensure Setup backlog exists (Line 1, Shift A, 150 pcs).
2. Plan order on Line 1 Shift A dates → preview shows reduced free capacity on affected slots.

---

# PAGE 4 — Planning Timeline (`/planning/timeline`)

**Purpose:** Single-order view: Loom → Fabric ready → FIBC → Dispatch.  
**Inputs:** Only one search box.

---

## 4.1 Look up order panel

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Order number** | Text | Yes | `8500585065/157602` | Buyer order / PO |
| 2 | **Load timeline** (or Enter) | Button | — | Click | |

---

## 4.2 Results (all read-only — no inputs)

After load, these sections appear:

| Section | Content |
|---------|---------|
| **Order header** | Bag type, Quantity, Marketing no., Fabric buffer (5 days) |
| **Warnings** | Amber list if data gaps |
| **End-to-end timeline** | Milestone rail: Loom → Fabric ready → FIBC → Dispatch |
| **Loom allocations** | Table + link “Open loom planning” |
| **FIBC line plan** | Table + link “Open FIBC planning” |
| **BOM fabric requirements** | Heading, GSM, width, meters, target date |

---

### Timeline demo scenarios

#### Scenario T1 — Full chain (loom only)

1. Enter `8500585065/157602` → Load timeline.
2. Walk milestone rail left → right.
3. Expand loom table (4 segments).
4. Note FIBC section empty → “FIBC not planned yet”.
5. Show BOM body fabric row.

#### Scenario T2 — FIBC-only order

1. Enter `PO 9305/4LT-0775` → Load.
2. FIBC slots populated; loom empty; no BOM.

#### Scenario T3 — Full chain (loom + FIBC on **same order**) — use `PO 9305/4LT-0775`

**Prerequisite:** Complete **Script 5** (loom save on this order) OR order already has FIBC saved from ERP.

1. Enter **`PO 9305/4LT-0775`** → Load timeline.
2. **Expected milestones:** Fabric ready → **Loom weaving** (after you save loom) → **FIBC line production** (4 slots Oct 2025).
3. Expand **Loom allocations** table (empty until Script 5 step 2 save; populated after).
4. Expand **FIBC line plan** — 4 rows on Line 6 (already in ERP).
5. Read amber **warnings** (e.g. bailing gap, loom missing before save).
6. **Execution** uses the **same order number** — FIBC planned vs produced (loom is on Timeline, not Execution totals).

---

# PAGE 5 — Planning Execution (`/planning/execution`)

**Purpose:** Planned vs produced vs bailed; replan suggestions.  
**Company:** Fixed to **Plastene India Limited (Unit -II)** (not selectable in UI).

---

## 5.1 Order lookup panel

| # | Field | Type | Required | Demo value | Notes |
|---|-------|------|----------|------------|-------|
| 1 | **Order / PO number** | Text | Yes | `PO 9305/4LT-0775` | Must have saved FIBC plan + production data |
| 2 | **Load** (search icon) | Button | — | Click | |

---

## 5.2 Results (all read-only — no inputs)

| Section | Fields displayed |
|---------|------------------|
| **Production vs plan** | Planned, Produced, Bailed, Pending pcs; bailing gap; replan suggestions; auto-cleared backlog message |
| **Bailing reconciliation** | Dispatch-ready badge, message, shortfall vs plan |
| **Line + shift totals** | Line, Shift, Planned, Produced, Pending |
| **Plan slots (by date)** | Line, Shift, Plan date, Planned, Produced (same date), Backlog |
| **ERP production log** | Date, Line, Team, Shift, Produced |

---

### Execution demo scenarios

#### Scenario E1 — Same order as loom + FIBC demo (`PO 9305/4LT-0775`)

1. Load **`PO 9305/4LT-0775`** (same order as Script 5 / Timeline T3).
2. **Expected:** Planned **1170** pcs, Produced **1170**, Bailed **0** (bailing gap warning — good talking point).
3. Scroll **Plan slots by date** — FIBC slots 2025-10-23 / 24 (Line 6).
4. **ERP production log** — production dates may differ from plan slots (ERP quirk — warn in demo).
5. **Note:** Execution tracks **FIBC bag production**, not loom meters. Loom plan is visible on **Timeline** for the same order.

#### Scenario E2 — Order with saved plan only (legacy)

1. Load `PO 9305/4LT-0775` before adding loom — FIBC-only execution view.

#### Scenario E3 — After backlog cleared

1. If production completes backlog qty on Line 1 Shift A, execution may show “Cleared N backlog row(s) automatically.”

---

# END-TO-END DEMO SCRIPTS (full click path)

## Script 1 — “New export order” (60 min condensed: 25 min)

**Note:** Steps 3–6 use **different order numbers**. For **one order** through Loom → FIBC → Timeline → Execution, use **Script 5**.

| Step | Page | Inputs |
|------|------|--------|
| 1 | Setup → Backlog | Line 1, A, PO 9305/4LT-0775, 150 pcs |
| 2 | Setup → Loom pool | Tag 2 looms Export; pool = Domestic only; Save |
| 3 | Loom | **L1 partial:** `8500585065/157602`, Sep 1, 6077 m. **L2 save:** `23129/PTS-RC`, **300 m**, fabric **2026-10-30**, replace ☑, Confirm |
| 4 | FIBC | From 2025-09-01, To 2025-12-31; order FIBC-DEMO-001, qty 2000, UPanel, dispatch 2025-12-15; Preview → Confirm |
| 5 | Timeline | `8500585065/157602` |
| 6 | Execution | `PO 9305/4LT-0775` |

## Script 5 — **Same order end-to-end** (30 min) — **recommended**

Single order: **`PO 9305/4LT-0775`**. FIBC **already in ERP** (4 slots); you **save loom** in demo; Timeline + Execution use **same order number**.

| Step | Page | What to do |
|------|------|------------|
| 1 | **FIBC** | Dates `2025-09-01` – `2025-12-31` → Refresh. Lookup **`PO 9305/4LT-0775`** → show 4 saved slots (view only). |
| 2 | **Loom** | Dates `2026-08-01` – `2026-11-30` → Refresh. Order **`PO 9305/4LT-0775`**, GSM **`122`**, width **`103`**, meters **`200`**, fabric **`2026-10-30`**. Preview → Fully allotted, no changeover → **Confirm & save**. |
| 3 | **Timeline** | **`PO 9305/4LT-0775`** → loom + FIBC milestones together. |
| 4 | **Execution** | **`PO 9305/4LT-0775`** → 1170 planned/produced pcs, FIBC plan slots. |

**Execution shows FIBC production only** (pcs planned/produced/bailed). **Timeline** shows loom + FIBC together. Loom dates are 2026; FIBC grid is 2025 — normal for Unit-II demo data.

## Script 2 — “Quotation pipeline” (15 min)

| Step | Page | Inputs |
|------|------|--------|
| 1 | FIBC grid | Date 2025-09-01 – 2025-12-31; filter **free** |
| 2 | Quotation hold | DEMO-Q-001, 1500 pcs, Buffle, dispatch 2025-12-20 |
| 3 | Plan order | Different order → Preview → show reduced capacity |
| 4 | Quotation hold | Confirm hold → ERP |

## Script 3 — “Critical customer” (15 min)

| Step | Page | Inputs |
|------|------|--------|
| 1 | FIBC grid | Show occupied slots near 2025-11-01 |
| 2 | Critical panel | FIBC-CRIT-001, 3000, UPanel, dispatch 2025-11-01, ☑ Pin to target date |
| 3 | Preview critical shift | Walk displacements table |
| 4 | Confirm shift & save | |

## Script 4 — “Loom displacement + full preview” (15 min)

| Step | Page | Inputs |
|------|------|--------|
| 1 | Loom | **L1:** `8500585065/157602`, fabric 2026-09-01, full meters → partial + displacements |
| 2 | Loom | **L2:** `23129/PTS-RC`, **300 m**, fabric **2026-10-30** → Fully allotted, no changeover → Confirm & save |
| 3 | Loom grid | Refresh; show new segments if L2 saved |
| 4 | Loom | Optional: `PPL-66/2026/BIG BAGS/ITEM1` — replace-existing demo (L3) |

## Quick reference — every editable input by page

| Page | Editable inputs |
|------|-----------------|
| **Setup** | Factory search; Planning enabled; Buffer days; Rejection %; Notes; Line capacities/families/team/active; Loom pool mode/filter/include/purpose/winder/type; Preference chart rows; Team manual factors; Downtime add/edit; Backlog add |
| **Loom** | Date from/to; Order lookup; Plan order (order, GSM, width, meters, fabric date, replace); Loom filter; Confirm dialog |
| **FIBC** | Date from/to; Order lookup; Plan order (order, qty, bag, dispatch, mode, dust, replace); Critical (order, qty, bag, dispatch, reason, pin, replace); Hold (order, qty, bag, dispatch, notes); Slot occupancy filter; All confirm dialogs |
| **Timeline** | Order number only |
| **Execution** | Order number only |

---

## Defaults on page load (no typing needed)

| Page | Auto-filled |
|------|-------------|
| Setup | Company = Plastene India Limited (Unit -II); Downtime date = today; Backlog line = 1, shift = A |
| Loom / FIBC | Date From = today − 30 days; Date To = today |
| Timeline / Execution | Empty order field |

---

*Generated from portal source routes: `setup.index.tsx`, `loom.index.tsx`, `fibc.index.tsx`, `timeline.index.tsx`, `execution.index.tsx` and Unit-II database probe (Aug 2026). Loom scenarios L1/L2 validated against live grid behaviour Aug 2026.*
