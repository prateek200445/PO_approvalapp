"""Load stock and provisions that the Aug-Oct packs actually contain.

Stock backfill: PIL-2 Jul-Oct and KPW-1 Sep-Oct from the EBITDA stock sheets.
OEL Jul-Oct and KPW-3 Jul/Aug/Oct from movement value columns.
KPW-3 Sep from Stock Valuation (not the PIL-1 template copy in that folder).
HPBL 1/2/3/Baroda Sep from the value summary. PIL-1, PPL, HPBL-4 Sep-Oct from templates.

Does not replace Apr-Jun, and does not replace Jul-Aug stock already taken from the
KPW-1 bible or the July-August group script. Missing stock categories are carried
forward from the prior month so a partial sheet does not zero stores.
"""
from __future__ import annotations

from collections import defaultdict
from datetime import datetime
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp")
OUT = ROOT / "POApprovalAPI" / "Scripts" / "LoadSepOctGroupEbitda.sql"
LACS = 100_000.0

PIL1 = "Plastene India Limited"
PIL2 = "Plastene India Limited (Unit -II)"
PPL = "Plastene Polyfilms Limited"
HPBL4 = "HCP Plastene Bulkpack Ltd (Unit - IV)"
HPBL1 = "HCP Plastene Bulkpack Ltd"
HPBL2 = "HCP Plastene Bulkpack Ltd (Unit - II)"
HPBL3 = "HCP Plastene Bulkpack Ltd (Unit - III)"
VAD = "HCP Plastene Bulkpack ltd (Vadodara)"
OEL = "Oswal Extrusion Limited"
KPW1 = "K.P. WOVEN PRIVATE LIMITED"
KPW3 = "K.P. WOVEN PRIVATE LIMITED (UNIT-III)"

CAT_FROM_LABEL = {
    "raw materials": "RawMaterial",
    "packing materials": "Packing",
    "wip": "WIP",
    "finished goods": "FinishedGoods",
    "stock in trade": "Traded",
    "stores, spares & consumables": "Stores",
}

LEDGER_FIX = {
    "freight outward expense (with gst)": "Freight Outward Expense (With GST)",
    "job charges": "Job Charges",
    "jobwork charges": "Job Charges",
    "repairs & maintenance exp -": "Repairs & Maintenance Exp - Electric",
    "advocate fees exp.": "Advocate Fees Expense",
    "c & f charges-export": "C & F  Charges - Export",
    "c & f charges-import": "C & F  Charges  - Import",
    "c & f charges - import": "C & F  Charges  - Import",
    "staff welfare exp.": "Staff Welfare Expense",
    "conveyance expense": "Conveyance Expenses",
    "professional tax liability a/c": "Professional Tax - Company",
    "salary & wages payable": "Salaries Expense",
    "medical expenses": "Medical Expense",
    "fumigation expenses": "Fumigation Charges",
    "vehicle hire charges": "Vehicle Hire Charges - RCM",
    "security charges": "Security Expenses",
    "repair & maintance- plant & m/c": "Repairs & Maintenance Exp - Plant & Machinery",
}

SKIP_LEDGERS = {
    "total", "total amount", "total amount (inr)", "particulars", "perticulars",
    "expense", "expense head", "exp. head", "closing stock of packing material",
    "closing stock of consumables, stores , spares", "closing stock of consumables, stores, spares",
    "common expenses", "ho expenses", "common expense", "ho expense",
}

stock = {}  # (company, month, category) -> [lacs, remark]
prov_rows = []


def n(v):
    if v is None or isinstance(v, datetime):
        return None
    if isinstance(v, (int, float)):
        return float(v)
    if isinstance(v, str):
        s = v.strip().replace(",", "")
        if not s:
            return None
        try:
            return float(s)
        except ValueError:
            return None
    return None


def sql_n(name: str) -> str:
    return "N'" + name.replace("'", "''") + "'"


def entry_type(ledger: str) -> str:
    low = ledger.lower()
    if "expense" in low:
        return "Expense"
    if "income" in low or low.startswith("job work income") or "wind mill" in low:
        return "Income"
    return "Expense"


def fix_ledger(name: str) -> str:
    raw = str(name).strip()
    key = " ".join(raw.split()).lower().rstrip(" .")
    return LEDGER_FIX.get(key, raw)


def add_stock(company, month, category, lacs, remark, allow_zero=False):
    if lacs is None:
        return
    if not allow_zero and abs(lacs) < 1e-8:
        return
    key = (company, month, category)
    if key in stock and "carried" not in remark:
        stock[key][0] += lacs
        return
    stock[key] = [lacs, remark]


def add_prov(company, month, ledger, rupees, remark):
    if ledger is None:
        return
    led = fix_ledger(str(ledger).strip())
    if not led or led.lower() in SKIP_LEDGERS:
        return
    if led.lower().startswith("closing stock"):
        return
    amt = n(rupees)
    if amt is None or abs(amt) < 0.0001:
        return
    prov_rows.append((company, month, led, amt, remark))


def ox(path):
    return load_workbook(ROOT / path, data_only=True)


def template_stock_sheet(ws, amount_col=2):
    rows = {}
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 1).value
        if not lab:
            continue
        cat = CAT_FROM_LABEL.get(str(lab).strip().lower())
        if not cat:
            continue
        val = n(ws.cell(r, amount_col).value)
        rows[cat] = 0.0 if val is None else val
    vals = [abs(v) for v in rows.values() if v]
    already_lacs = (max(vals) < 50_000) if vals else True
    return {c: (v if already_lacs else v / LACS) for c, v in rows.items()}, already_lacs


def template_prov_sheet(ws, amount_col=2):
    items = []
    started = False
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 1).value
        if lab and str(lab).strip().lower() in ("ledger name", "ledger"):
            started = True
            continue
        if not started:
            continue
        amt = n(ws.cell(r, amount_col).value)
        if lab and amt is not None and abs(amt) >= 0.0001:
            items.append((str(lab).strip(), amt))
    amts = [abs(a) for _, a in items]
    already_lacs = (max(amts) < 5_000) if amts else True
    return [(led, amt * LACS if already_lacs else amt) for led, amt in items], already_lacs


def two_col(path, sheet, company, month, name_c, amt_c, start_r, remark):
    wb = ox(path)
    ws = wb[sheet]
    count = 0
    for r in range(start_r, (ws.max_row or start_r) + 1):
        before = len(prov_rows)
        add_prov(company, month, ws.cell(r, name_c).value, ws.cell(r, amt_c).value, remark)
        count += len(prov_rows) - before
    wb.close()
    print(f"prov {company[:24]:24} {month} {count:3}  {remark}")


def tb_provision(path, sheet, company, month, remark):
    wb = ox(path)
    ws = wb[sheet]
    pcol = None
    hr = None
    for r in range(1, 8):
        for c in range(1, 12):
            lab = str(ws.cell(r, c).value or "").strip().lower()
            if lab.startswith("provis"):
                pcol, hr = c, r
                break
        if pcol:
            break
    if not pcol:
        wb.close()
        print("NO provision column", path, sheet)
        return 0
    count = 0
    for r in range(hr + 1, (ws.max_row or hr) + 1):
        led = ws.cell(r, 1).value
        before = len(prov_rows)
        add_prov(company, month, led, ws.cell(r, pcol).value, remark)
        count += len(prov_rows) - before
    wb.close()
    print(f"prov {company[:24]:24} {month} {count:3}  {remark}")
    return count


def bible_stock(path, sheet, company, months, remark):
    """FY 26-27 block starts at the column labelled Op,stock 01.04.26.

    Offsets from that column: 0 opening, 1 Apr, 2 May, 3 Jun, 4 Jul, 5 Aug, 6 Sep, 7 Oct.
    """
    wb = ox(path)
    ws = wb[sheet]
    section = None
    section_map = {
        "raw material": "RawMaterial",
        "finish material": "FinishedGoods",
        "finesh material": "FinishedGoods",
        "finished material": "FinishedGoods",
        "stores spares": "Stores",
        "stores & spares": "Stores",
    }
    offsets = {
        4: "2026-07-01",
        5: "2026-08-01",
        6: "2026-09-01",
        7: "2026-10-01",
    }
    found = []
    max_r = ws.max_row or 1
    max_c = ws.max_column or 1
    for r in range(1, max_r + 1):
        lab = str(ws.cell(r, 2).value or "").strip().lower()
        if lab in section_map:
            section = section_map[lab]
            continue
        if not section:
            continue
        start = None
        for c in range(3, max_c + 1):
            text = str(ws.cell(r, c).value or "").strip().lower()
            if "01.04.26" in text or "op,stock" in text:
                start = c
                break
        if not start:
            continue
        value_row = None
        for rr in range(r + 1, min(r + 4, max_r + 1)):
            if n(ws.cell(rr, start).value) is not None:
                value_row = rr
                break
        if not value_row:
            continue
        opening = abs(n(ws.cell(value_row, start).value) or 0)
        scale = 1.0 if opening < 100_000 else LACS
        for off, month in offsets.items():
            if month not in months:
                continue
            val = n(ws.cell(value_row, start + off).value)
            if val is None:
                continue
            add_stock(company, month, section, val / scale, remark, allow_zero=True)
            found.append((month, section, round(val / scale, 2)))
        section = None
    wb.close()
    print("bible", company[:24], found)


def movement_values(path, sheet, company, month, remark, group_col, fac_col, job_col, header_row):
    """0-based columns. SF -> WIP. Factory + job value, rupees."""
    wb = ox(path)
    ws = wb[sheet]
    buckets = defaultdict(float)
    for r in range(header_row + 1, (ws.max_row or header_row) + 1):
        grp = str(ws.cell(r, group_col + 1).value or "").strip().upper()
        if grp not in ("RM", "SF", "FG"):
            continue
        fac = n(ws.cell(r, fac_col + 1).value) or 0.0
        job = 0.0 if job_col == fac_col else (n(ws.cell(r, job_col + 1).value) or 0.0)
        cat = {"RM": "RawMaterial", "SF": "WIP", "FG": "FinishedGoods"}[grp]
        buckets[cat] += fac + job
    wb.close()
    for cat, rupees in buckets.items():
        add_stock(company, month, cat, rupees / LACS, remark)
    print("move", company[:24], month, {k: round(v / LACS, 2) for k, v in buckets.items()})


def kpw3_valuation(path, month):
    wb = ox(path)
    ws = wb["Summary"]
    buckets = defaultdict(float)
    for r in range(4, (ws.max_row or 4) + 1):
        grp = str(ws.cell(r, 1).value or "").strip()
        if not grp or grp.lower() in ("total", "grand total", "group"):
            continue
        val = (n(ws.cell(r, 34).value) or 0) + (n(ws.cell(r, 35).value) or 0)
        key = grp.upper()
        if key == "RM":
            cat = "RawMaterial"
        elif key in ("SF", "#N/A", "WIP"):
            cat = "WIP"
        elif key == "FG":
            cat = "FinishedGoods"
        elif "STORE" in key:
            cat = "Stores"
        elif "PACK" in key:
            cat = "Packing"
        else:
            continue
        buckets[cat] += val
    wb.close()
    for cat, rupees in buckets.items():
        add_stock(KPW3, month, cat, rupees / LACS, "KPW-3 Sep stock valuation value+job")
    print("kpw3 val", {k: round(v / LACS, 2) for k, v in buckets.items()})


def ppl_valuation(path, month, remark):
    wb = ox(path)
    name = next(s for s in wb.sheetnames if s.strip().upper().startswith("STOCK VALUATION"))
    ws = wb[name]
    header_r = gcol = valc = jobc = None
    for r in range(1, 8):
        for c in range(1, min(ws.max_column or 1, 60) + 1):
            v = str(ws.cell(r, c).value or "").strip().lower()
            if v == "group":
                header_r, gcol = r, c
            if v == "value":
                valc = c
            if v in ("job value", "jobvalue"):
                jobc = c
    groups = defaultdict(float)
    for r in range((header_r or 1) + 1, (ws.max_row or 1) + 1):
        g = ws.cell(r, gcol).value if gcol else None
        if not g:
            continue
        gs = str(g).strip()
        if gs.lower() in ("total", "grand total"):
            continue
        amt = (n(ws.cell(r, valc).value) or 0) + ((n(ws.cell(r, jobc).value) or 0) if jobc else 0)
        groups[gs] += amt
    stores = packing_store = 0.0
    sname = next((s for s in wb.sheetnames if s.strip().upper() == "STORE REPORT"), None)
    if sname:
        sr = wb[sname]
        for r in range(1, (sr.max_row or 1) + 1):
            labels = []
            for c in range(1, min(sr.max_column or 1, 15) + 1):
                v = sr.cell(r, c).value
                if isinstance(v, str) and v.strip():
                    labels.append(v.strip().lower())
            nums = [n(sr.cell(r, c).value) for c in range(1, min(sr.max_column or 1, 15) + 1)]
            nums = [x for x in nums if x and abs(x) > 1]
            if not nums:
                continue
            joined = " ".join(labels)
            if "store & spare" in joined:
                stores = max(nums, key=abs)
            if "packing material" in joined:
                packing_store = max(nums, key=abs)
    wb.close()
    add_stock(PPL, month, "RawMaterial", groups.get("RM", 0) / LACS, remark)
    add_stock(PPL, month, "Packing", packing_store / LACS, remark + " packing")
    add_stock(PPL, month, "WIP", groups.get("#N/A", 0) / LACS, remark + " #N/A")
    add_stock(PPL, month, "FinishedGoods", (groups.get("FG", 0) + groups.get("SF", 0)) / LACS, remark + " FG+SF")
    add_stock(PPL, month, "Stores", stores / LACS, remark + " stores")
    print("ppl", month, {k: round(v / LACS, 2) for k, v in groups.items()}, "stores", round(stores / LACS, 2), "pack", round(packing_store / LACS, 2))


def ppl_provision(path, month, remark):
    wb = ox(path)
    name = next(s for s in wb.sheetnames if "PROVISION" in s.upper())
    ws = wb[name]
    count = 0
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 1).value
        if not lab:
            continue
        name_l = str(lab).strip()
        low = name_l.lower()
        if low.startswith("expense head") or "salary summary" in low or low.startswith("total"):
            continue
        gross = n(ws.cell(r, 4).value)
        contractor = n(ws.cell(r, 3).value)
        serial = n(ws.cell(r, 2).value)
        if "job provision" in low or "job income" in low:
            amt = gross if gross and abs(gross) > 100 else (serial if serial and abs(serial) > 100 else contractor)
            if amt is None or abs(amt) < 0.0001:
                continue
            before = len(prov_rows)
            add_prov(PPL, month, "Job Work Income", -abs(amt), remark)
            count += len(prov_rows) - before
            continue
        amt = gross if gross not in (None, 0) and abs(gross) >= 0.0001 else None
        if amt is None and contractor is not None and abs(contractor) > 500:
            amt = contractor
        if amt is None:
            continue
        if "labour" in low:
            name_l = "Labour Charges"
        elif "freight outward" in low:
            name_l = "Freight Outward Expense (With GST)"
        elif "power expense" in low:
            name_l = "Power Expense"
        before = len(prov_rows)
        add_prov(PPL, month, name_l, amt, remark)
        count += len(prov_rows) - before
    wb.close()
    print(f"prov {PPL[:24]:24} {month} {count:3}  {remark}")


# ---- PIL 2 and KPW 1 official stock sheets (rupees or lacs) ----
bible_stock(
    "oct 26/pil2/EBIDTA 26-27 .xlsx",
    "Stock Sheet",
    PIL2,
    {"2026-07-01", "2026-08-01", "2026-09-01", "2026-10-01"},
    "PIL-2 EBITDA stock sheet",
)
bible_stock(
    "oct 26/kpw1/EBIDTA 26-27 - April to Oct. 26.xlsx",
    "Stock Sheet - KPW",
    KPW1,
    {"2026-09-01", "2026-10-01"},
    "KPW-1 EBITDA stock sheet",
)
for month in ("2026-09-01", "2026-10-01"):
    for cat in ("Packing", "WIP", "Traded"):
        add_stock(KPW1, month, cat, 0.0, "KPW-1 stock sheet has no packing/WIP/traded", allow_zero=True)

# ---- OEL movement value ----
for path, month in (
    ("july 26/oel1/OEL-1 Stock Movement sheet Format Jul-2026.xlsx", "2026-07-01"),
    ("aug 26/oel/OEL-1 Stock Movement sheet Format Aug-2026.xlsx", "2026-08-01"),
    ("sept 26/oel1/OEL-1 Stock Movement sheet Format Sep-2026.xlsx", "2026-09-01"),
    ("oct 26/oel1/OEL-1 Stock Movement sheet Format Oct-2026.xlsx", "2026-10-01"),
):
    movement_values(path, "Summary", OEL, month, "OEL Summary Value Factory Own", 0, 33, 33, 3)

# ---- KPW 3 movement Jul/Aug/Oct; Sep valuation ----
for path, month in (
    ("july 26/kpw3/KPW-3 Movement sheet Format Jul-2026.xlsx", "2026-07-01"),
    ("aug 26/kpw3/KPW-3 Movement sheet Format Aug-2026.xlsx", "2026-08-01"),
    ("oct 26/kpw3/KPW-3 Movement sheet Format Oct-2026.xlsx", "2026-10-01"),
):
    movement_values(path, "Summary", KPW3, month, "KPW-3 Summary factory value + at JW own", 0, 33, 35, 3)
kpw3_valuation("sept 26/KPW3/Stock Valuation Sep-26.xlsx", "2026-09-01")

# ---- HPBL 1/2/3/Baroda September value summary (rupees) ----
wb = ox("sept 26/hpbl123/HPBL Stock hpbl123.xlsx")
ws = wb.active
units = {2: HPBL1, 3: HPBL2, 4: HPBL3, 5: VAD}
cat_of = {"store": "Stores", "rm": "RawMaterial", "sf": "WIP", "fg": "FinishedGoods"}
for r in range(5, 9):
    lab = str(ws.cell(r, 1).value or "").strip().lower()
    cat = cat_of.get(lab)
    if not cat:
        continue
    for c, company in units.items():
        val = n(ws.cell(r, c).value)
        if val is None:
            continue
        add_stock(company, "2026-09-01", cat, val / LACS, "HPBL Sep stock summary", allow_zero=False)
wb.close()

# ---- templates: PIL 1, HPBL 4 ----
for path, month, company in (
    ("sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx", "2026-09-01", PIL1),
    ("oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx", "2026-10-01", PIL1),
):
    wb = ox(path)
    parsed, lacs = template_stock_sheet(wb.active)
    wb.close()
    for cat, val in parsed.items():
        add_stock(company, month, cat, val, path, allow_zero=True)
    print("tmpl", company[:12], month, "lacs" if lacs else "rupees", {k: round(v, 2) for k, v in parsed.items()})

for path, sheet, month in (
    ("sept 26/HPBL4/PnL-Stock - Template- HPBL-IV.xlsx", "SEPT-26", "2026-09-01"),
    ("oct 26/hpbl 4/PnL-Stock - Template- HPBL-IV.xlsx", "OCT-26", "2026-10-01"),
):
    wb = ox(path)
    parsed, lacs = template_stock_sheet(wb[sheet])
    wb.close()
    for cat, val in parsed.items():
        add_stock(HPBL4, month, cat, val, f"HPBL4 {sheet}", allow_zero=True)
    print("tmpl HPBL4", month, "lacs" if lacs else "rupees", {k: round(v, 2) for k, v in parsed.items()})

ppl_valuation("sept 26/PPL/Stock Valuation Sept-26.xlsx", "2026-09-01", "PPL Sep valuation")
ppl_valuation("oct 26/ppl/Stock Valuation Oct-26.xlsx", "2026-10-01", "PPL Oct valuation")

# ---- provisions: Sep and Oct only. Jul/Aug already loaded. ----
for path, month in (
    ("sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx", "2026-09-01"),
    ("oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx", "2026-10-01"),
):
    wb = ox(path)
    items, lacs = template_prov_sheet(wb.active)
    wb.close()
    for led, rs in items:
        add_prov(PIL1, month, led, rs, path)
    print(f"prov {PIL1[:24]:24} {month} {len(items):3}  template {'lacs' if lacs else 'rupees'}")

for path, sheet, month in (
    ("sept 26/HPBL4/PnL- Provision -Template- HPBL-IV.xlsx", "SEPT-26", "2026-09-01"),
):
    wb = ox(path)
    items, lacs = template_prov_sheet(wb[sheet])
    wb.close()
    for led, rs in items:
        add_prov(HPBL4, month, led, rs, f"HPBL4 {sheet}")
    print(f"prov {HPBL4[:24]:24} {month} {len(items):3}  template {'lacs' if lacs else 'rupees'}")

ppl_provision("sept 26/PPL/Stock Valuation Sept-26.xlsx", "2026-09-01", "PPL Sep PROVISION DATA")
ppl_provision("oct 26/ppl/Stock Valuation Oct-26.xlsx", "2026-10-01", "PPL Oct PROVISION DATA")

two_col("oct 26/kpw1/Provisions October- 26 kpw.xlsx", "OCTOBER- 26", KPW1, "2026-10-01", 2, 3, 4, "KPW-1 Oct provisions")
tb_provision("sept 26/kpw1/EBIDTA 26-27 - April to Sep. 26.xlsx", "SEPTEMBER - TB", KPW1, "2026-09-01", "KPW-1 Sep TB provision")
two_col("oct 26/kpw3/KPW-3 Provision Exp Oct-2026.xlsx", "Sep-26", KPW3, "2026-09-01", 1, 2, 4, "KPW-3 Sep provision sheet")
two_col("oct 26/kpw3/KPW-3 Provision Exp Oct-2026.xlsx", "Oct-26", KPW3, "2026-10-01", 1, 2, 4, "KPW-3 Oct provision sheet")
two_col("sept 26/oel1/OEL-1 provision sheet Sep-2026.xlsx", "Sep-26", OEL, "2026-09-01", 1, 2, 3, "OEL Sep provisions")
two_col("oct 26/oel1/OEL-1 provision sheet Oct-2026.xlsx", "Oct-26", OEL, "2026-10-01", 1, 2, 3, "OEL Oct provisions")
tb_provision("sept 26/PIL2/EBIDTA 26-27 PIL2.xlsx", "TB SEPT.", PIL2, "2026-09-01", "PIL-2 Sep TB provision")
tb_provision("oct 26/pil2/EBIDTA 26-27 .xlsx", "TB OCT", PIL2, "2026-10-01", "PIL-2 Oct TB provision")


def hpbl123(path, month):
    wb = ox(path)
    ws = wb.active
    for r in range(2, (ws.max_row or 2) + 1):
        lab = ws.cell(r, 1).value
        if not lab or str(lab).strip().upper() == "TOTAL":
            continue
        add_prov(HPBL1, month, lab, ws.cell(r, 3).value, f"{path} unit-1")
        add_prov(HPBL2, month, lab, ws.cell(r, 4).value, f"{path} unit-2")
    wb.close()


hpbl123("sept 26/hpbl123/hpbl123 prov sept.xlsx", "2026-09-01")

# collapse provisions
collapsed = {}
for co, mo, led, amt, rem in prov_rows:
    k = (co, mo, led)
    if k not in collapsed:
        collapsed[k] = [amt, rem]
    else:
        collapsed[k][0] += amt
prov_final = [(k[0], k[1], k[2], v[0], v[1]) for k, v in collapsed.items() if abs(v[0]) >= 0.0001]
prov_final.sort(key=lambda x: (x[1], x[0], x[2]))
stock_rows = [(co, mo, cat, vals[0], vals[1]) for (co, mo, cat), vals in stock.items()]
stock_rows.sort(key=lambda x: (x[1], x[0], x[2]))

print("\nSTOCK")
for r in stock_rows:
    print(f"  {r[1]} | {r[0][:42]:42} | {r[2]:14} {r[3]:10.2f}")
print("stock", len(stock_rows), "prov", len(prov_final))

lines = []
a = lines.append
a("/* Sep-Oct EBITDA stock and provisions, plus July-August stock that the movement")
a("   and EBITDA sheets can support. Does not delete Apr-Jun. Does not replace")
a("   Jul-Aug provisions. Missing stock categories are copied from the prior month.")
a("   Not loaded: HPBL-4 October provisions (template stops at September);")
a("   HPBL 1/2/3/Baroda stock other than September; KPW-1 movement sheet (negative RM,")
a("   bible stock sheet used instead); PIL-2 / KPW-1 stores tabs that are prior-year.")
a("*/")
a("SET NOCOUNT ON;")
a("SET XACT_ABORT ON;")
a("BEGIN TRAN;")
a("")
a("IF OBJECT_ID(N'dbo.PnlStockValue', N'U') IS NULL")
a("BEGIN")
a("    RAISERROR(N'PnlStockValue is missing.', 16, 1);")
a("    RETURN;")
a("END")
a("IF OBJECT_ID(N'dbo.Provisioning', N'U') IS NULL")
a("BEGIN")
a("    RAISERROR(N'Provisioning is missing.', 16, 1);")
a("    RETURN;")
a("END")
a("")

for co, mo, cat, lacs, rem in stock_rows:
    a(f"IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = {sql_n(co)} AND StockMonth = '{mo}' AND Category = {sql_n(cat)} AND PlantName IS NULL)")
    a("    UPDATE dbo.PnlStockValue")
    a(f"    SET AmountLacs = {lacs:.6f}, UploadedAt = GETDATE(), Remarks = {sql_n(rem[:180])}")
    a(f"    WHERE CompanyName = {sql_n(co)} AND StockMonth = '{mo}' AND Category = {sql_n(cat)} AND PlantName IS NULL;")
    a("ELSE")
    a("    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)")
    a(f"    VALUES ({sql_n(co)}, NULL, '{mo}', {sql_n(cat)}, {lacs:.6f}, {sql_n(rem[:180])}, GETDATE());")
    a("")

touched = sorted({(co, mo) for co, mo, _, _, _ in stock_rows}, key=lambda x: (x[1], x[0]))
for co, mo in touched:
    prev = {
        "2026-07-01": "2026-06-01",
        "2026-08-01": "2026-07-01",
        "2026-09-01": "2026-08-01",
        "2026-10-01": "2026-09-01",
    }[mo]
    a(f"INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)")
    a(f"SELECT p.CompanyName, NULL, '{mo}', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()")
    a(f"FROM dbo.PnlStockValue p")
    a(f"WHERE p.CompanyName = {sql_n(co)} AND p.StockMonth = '{prev}' AND p.PlantName IS NULL")
    a(f"  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '{mo}' AND c.Category = p.Category AND c.PlantName IS NULL);")
    a("")

from itertools import groupby
for (co, mo), grp in groupby(prov_final, key=lambda x: (x[0], x[1])):
    a(f"DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = {sql_n(co)} AND sysdate >= '{mo}' AND sysdate < DATEADD(month, 1, '{mo}');")
    for _, _, led, amt, rem in grp:
        a(
            "INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES ("
            f"{sql_n(co)}, '{mo}', {sql_n(led)}, {amt:.4f}, {sql_n(rem[:180])}, {sql_n(entry_type(led))});"
        )
    a("")

a("COMMIT;")
a("-- ROLLBACK;")
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", OUT, "lines", len(lines))
