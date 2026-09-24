"""Generate LoadJulyAugGroupEbitda.sql from july 26 / aug 26 / public templates."""
from __future__ import annotations

from collections import defaultdict
from pathlib import Path

from openpyxl import load_workbook
import xlrd

ROOT = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp")
OUT = ROOT / "POApprovalAPI" / "Scripts" / "LoadJulyAugGroupEbitda.sql"
LACS = 100_000.0

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

stock_rows = []  # (company, month, category, lacs, remark)
prov_rows = []   # (company, month, ledger, rupees, remark)


def n(v):
    if v is None:
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


def fmt_lacs(x: float) -> str:
    return f"{x:.6f}"


def fmt_rs(x: float) -> str:
    return f"{x:.4f}"


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


def add_stock(company, month, category, lacs, remark):
    if lacs is None:
        return
    if abs(lacs) < 1e-8:
        return
    stock_rows.append((company, month, category, lacs, remark))


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


def template_stock_sheet(ws, amount_col=2, already_lacs=None):
    rows = {}
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 1).value
        if not lab:
            continue
        cat = CAT_FROM_LABEL.get(str(lab).strip().lower())
        if not cat:
            continue
        rows[cat] = n(ws.cell(r, amount_col).value) or 0.0
    vals = [v for v in rows.values() if v]
    if already_lacs is None:
        already_lacs = (max(vals) < 50_000) if vals else True
    out = {}
    for c, v in rows.items():
        out[c] = v if already_lacs else v / LACS
    return out


def template_prov_sheet(ws, amount_col=2, already_lacs=None):
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
    if already_lacs is None:
        amts = [a for _, a in items]
        already_lacs = (max(abs(a) for a in amts) < 5_000) if amts else True
    out = []
    for led, amt in items:
        out.append((led, amt * LACS if already_lacs else amt))
    return out


# ---- PIL-1 stock ----
wb = ox("public/pil1 stock.xlsx")
ws = wb.active
jul = template_stock_sheet(ws, amount_col=5, already_lacs=False)  # rupees in July col
wb.close()
for c, v in jul.items():
    add_stock("Plastene India Limited", "2026-07-01", c, v, "public/pil1 stock.xlsx July / 1e5")

wb = ox("aug 26/pil1/pnl-stock-template-plastene-india-limited-2026-08 (1).xlsx")
aug = template_stock_sheet(wb.active, amount_col=2, already_lacs=False)
wb.close()
for c, v in aug.items():
    add_stock("Plastene India Limited", "2026-08-01", c, v, "aug 26 PIL-1 stock template / 1e5")

# ---- PIL-1 prov ----
wb = ox("july 26/pil1/Provision July-26.xlsx")
ws = wb["Sheet1"]
for r in range(7, 40):
    add_prov("Plastene India Limited", "2026-07-01", ws.cell(r, 2).value, ws.cell(r, 3).value, "july 26/pil1/Provision July-26.xlsx")
wb.close()

wb = ox("aug 26/pil1/pnl-provision-template-plastene-india-limited-2026-08 (1).xlsx")
for led, rs in template_prov_sheet(wb.active, already_lacs=False):  # rupees sitting in Amount Lacs
    add_prov("Plastene India Limited", "2026-08-01", led, rs, "aug 26 PIL-1 provision template (rupees)")
wb.close()

# ---- PPL stock Jul public (already lacs) ----
wb = ox("public/ppl_stock.xlsx")
ws = wb["Stock - July-26"]
for c, v in template_stock_sheet(ws, already_lacs=True).items():
    add_stock("Plastene Polyfilms Limited", "2026-07-01", c, v, "public/ppl_stock.xlsx July")
wb.close()


def ppl_valuation(path, month, remark):
    wb = ox(path)
    ws = wb["STOCK VALUATION "]
    header_r = 1
    gcol = valc = jobc = None
    for r in range(1, 8):
        for c in range(1, min(ws.max_column, 60) + 1):
            v = str(ws.cell(r, c).value or "").strip().lower()
            if v == "group":
                header_r, gcol = r, c
            if v == "value":
                valc = c
            if v in ("job value", "jobvalue"):
                jobc = c
    groups = defaultdict(float)
    packing = 0.0
    for r in range(header_r + 1, ws.max_row + 1):
        g = ws.cell(r, gcol).value if gcol else None
        if not g:
            continue
        gs = str(g).strip()
        if gs.lower() in ("total", "grand total"):
            continue
        amt = (n(ws.cell(r, valc).value) or 0) + ((n(ws.cell(r, jobc).value) or 0) if jobc else 0)
        groups[gs] += amt
        sg = str(ws.cell(r, 2).value or "") + " " + str(ws.cell(r, 3).value or "")
        if "pack" in sg.lower() or "liner" in sg.lower() or "craft" in sg.lower():
            packing += amt
    stores = packing_store = 0.0
    if "STORE REPORT" in wb.sheetnames:
        sr = wb["STORE REPORT"]
        for r in range(1, sr.max_row + 1):
            labels = []
            for c in range(1, min(sr.max_column, 15) + 1):
                v = sr.cell(r, c).value
                if isinstance(v, str) and v.strip():
                    labels.append(v.strip().lower())
            nums = [n(sr.cell(r, c).value) for c in range(1, min(sr.max_column, 15) + 1)]
            nums = [x for x in nums if x and abs(x) > 1]
            if not nums:
                continue
            joined = " ".join(labels)
            if "store & spare" in joined or joined == "store & spare":
                stores = max(nums, key=abs)
            if "packing material" in joined:
                packing_store = max(nums, key=abs)
    wb.close()
    rm = groups.get("RM", 0) / LACS
    wip = groups.get("#N/A", 0) / LACS
    fg = (groups.get("FG", 0) + groups.get("SF", 0)) / LACS
    pk = packing_store / LACS
    st = stores / LACS
    add_stock("Plastene Polyfilms Limited", month, "RawMaterial", rm, remark)
    add_stock("Plastene Polyfilms Limited", month, "Packing", pk, remark + " STORE REPORT Packing Material")
    add_stock("Plastene Polyfilms Limited", month, "WIP", wip, remark + " #N/A")
    add_stock("Plastene Polyfilms Limited", month, "FinishedGoods", fg, remark + " FG+SF")
    add_stock("Plastene Polyfilms Limited", month, "Stores", st, remark + " STORE & SPARE")
    return groups, stores, packing_store


ppl_valuation("aug 26/PPL/Stock Valuation Aug-26.xlsx", "2026-08-01", "PPL Aug valuation")


def ppl_provision_data(path, month, remark):
    """Col D Gross Value; skip S.No. JOB PROVISION -> Job Work Income (negative, Income)."""
    wb = ox(path)
    ws = wb["PROVISION DATA "]
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 1).value
        amt = n(ws.cell(r, 4).value)
        if not lab or amt is None or abs(amt) < 0.0001:
            continue
        name = str(lab).strip()
        low = name.lower()
        if low.startswith("expense head") or "salary summary" in low:
            continue
        if "job provision" in low or "job income" in low:
            add_prov("Plastene Polyfilms Limited", month, "Job Work Income", -abs(amt), remark)
            continue
        if "labour" in low:
            name = "Labour Charges"
        elif "freight outward" in low:
            name = "Freight Outward Expense (With GST)"
        elif "power expense" in low:
            name = "Power Expense"
        add_prov("Plastene Polyfilms Limited", month, name, amt, remark)
    wb.close()


wb = ox("public/ppl_prov.xlsx")
for led, rs in template_prov_sheet(wb["Provision - July-26"], already_lacs=True):
    add_prov("Plastene Polyfilms Limited", "2026-07-01", led, rs, "public/ppl_prov.xlsx July * 1e5")
wb.close()
ppl_provision_data("aug 26/PPL/Stock Valuation Aug-26.xlsx", "2026-08-01", "PPL Aug PROVISION DATA col D")

# ---- HPBL4 stock public (lacs) ----
wb = ox("public/hplb_4_stock.xlsx")
for sheet, month in [("JUL-26", "2026-07-01"), ("AUG-26", "2026-08-01")]:
    for c, v in template_stock_sheet(wb[sheet], already_lacs=True).items():
        add_stock("HCP Plastene Bulkpack Ltd (Unit - IV)", month, c, v, f"public/hplb_4_stock.xlsx {sheet}")
wb.close()

# ---- HPBL4 prov: public JUL (lacs), Aug folder template AUG (lacs) ----
wb = ox("public/hplb_4_prov.xlsx")
for led, rs in template_prov_sheet(wb["JUL-26"], already_lacs=True):
    add_prov("HCP Plastene Bulkpack Ltd (Unit - IV)", "2026-07-01", led, rs, "public/hplb_4_prov.xlsx JUL-26 * 1e5")
wb.close()

wb = ox("aug 26/HPBL4/pnl-provision-template-HPBL-IV.xlsx")
for led, rs in template_prov_sheet(wb["AUG-26"], already_lacs=True):
    add_prov("HCP Plastene Bulkpack Ltd (Unit - IV)", "2026-08-01", led, rs, "aug 26 HPBL4 provision template AUG * 1e5")
wb.close()


def two_col_list(path, sheet, company, month, name_c, amt_c, start_r, remark):
    wb = ox(path)
    ws = wb[sheet]
    for r in range(start_r, (ws.max_row or start_r) + 1):
        add_prov(company, month, ws.cell(r, name_c).value, ws.cell(r, amt_c).value, remark)
    wb.close()


two_col_list("july 26/kpw1/4 Provisions July 26 kpw.xlsx", "July- 26", "K.P. WOVEN PRIVATE LIMITED", "2026-07-01", 2, 3, 4, "july 26 kpw1 provisions")
two_col_list("aug 26/kpw1/5 Provisions August - 26.xlsx", "August- 26", "K.P. WOVEN PRIVATE LIMITED", "2026-08-01", 2, 3, 4, "aug 26 kpw1 provisions")
two_col_list("july 26/kpw3/KPW-3 Provision Exp Jul-2026.xlsx", "Jul-26", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)", "2026-07-01", 1, 2, 4, "july 26 kpw3 provisions")
two_col_list("aug 26/kpw3/KPW-3 Provision Exp Aug-2026.xlsx", "Aug-26", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)", "2026-08-01", 1, 2, 4, "aug 26 kpw3 provisions")
two_col_list("july 26/oel1/OEL-1 provision sheet Jul-2026.xlsx", "Jul-26", "Oswal Extrusion Limited", "2026-07-01", 1, 2, 3, "july 26 OEL provisions")
two_col_list("aug 26/oel/OEL-1 provision sheet Aug-2026.xlsx", "Aug-26", "Oswal Extrusion Limited", "2026-08-01", 1, 2, 3, "aug 26 OEL provisions")


def hpbl123(path, month):
    wb = ox(path)
    ws = wb.active
    for r in range(2, (ws.max_row or 2) + 1):
        lab = ws.cell(r, 1).value
        if not lab or str(lab).strip().upper() == "TOTAL":
            continue
        add_prov("HCP Plastene Bulkpack Ltd", month, lab, ws.cell(r, 3).value, f"{path} unit-1")
        add_prov("HCP Plastene Bulkpack Ltd (Unit - II)", month, lab, ws.cell(r, 4).value, f"{path} unit-2")
    wb.close()


hpbl123("july 26/hpbl123/prov hpbl 123 jul 26.xlsx", "2026-07-01")
hpbl123("aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx", "2026-08-01")


def pil2_tb(path, sheet, month, remark):
    wb = ox(path)
    ws = wb[sheet]
    # header
    hr = 1
    for r in range(1, 8):
        row = [str(ws.cell(r, c).value or "").lower() for c in range(1, 9)]
        if any("provis" in x for x in row) and any("group" in x for x in row):
            hr = r
            break
    count = 0
    for r in range(hr + 1, ws.max_row + 1):
        led = str(ws.cell(r, 1).value or "").strip()
        if not led or led.lower() in ("grand total", "total"):
            continue
        amt = n(ws.cell(r, 6).value)
        if amt is None or abs(amt) < 0.0001:
            continue
        add_prov("Plastene India Limited (Unit -II)", month, led, amt, remark)
        count += 1
    wb.close()
    return count


# n() shadowed — fix by using numeric helper already named n. The inner amt = n(...) uses global n. OK.
pil2_tb("july 26/pil2/EBIDTA 26-27 .xlsx", "TB JULY", "2026-07-01", "PIL-2 TB JULY Provisin col")
pil2_tb("aug 26/pil2/EBIDTA 26-27 .xlsx", "TB AUG", "2026-08-01", "PIL-2 TB AUG Provisin col")


def pil2_movement(path, month):
    book = xlrd.open_workbook(ROOT / path)
    print("PIL2 xls sheets", book.sheet_names())
    groups = defaultdict(float)
    for sname in book.sheet_names():
        sh = book.sheet_by_name(sname)
        # hunt group/value
        gcol = vcols = None
        hr = None
        for r in range(min(12, sh.nrows)):
            labels = {}
            for c in range(min(sh.ncols, 50)):
                v = sh.cell_value(r, c)
                if isinstance(v, str) and v.strip():
                    labels[v.strip().lower()] = c
            if "group" in labels and ("value" in labels or "closing value" in labels):
                hr = r
                gcol = labels.get("group")
                vcols = [labels[k] for k in labels if k in ("value", "job value", "jobvalue", "closing value")]
                break
        if hr is None:
            continue
        print("  using", sname, "hr", hr, "g", gcol, "v", vcols)
        for r in range(hr + 1, sh.nrows):
            g = sh.cell_value(r, gcol)
            if not g:
                continue
            gs = str(g).strip()
            if gs.lower() in ("total", "grand total"):
                continue
            amt = 0.0
            for c in vcols:
                x = sh.cell_value(r, c)
                if isinstance(x, (int, float)):
                    amt += float(x)
            groups[gs] += amt
        if groups:
            break
    print("  groups", {k: round(v / LACS, 4) for k, v in groups.items()})
    if not groups:
        return
    add_stock("Plastene India Limited (Unit -II)", month, "RawMaterial", groups.get("RM", 0) / LACS, f"{path} RM")
    add_stock("Plastene India Limited (Unit -II)", month, "WIP", groups.get("#N/A", 0) / LACS, f"{path} #N/A")
    fg = groups.get("FG", 0) + groups.get("SF", 0)
    add_stock("Plastene India Limited (Unit -II)", month, "FinishedGoods", fg / LACS, f"{path} FG+SF")


try:
    pil2_movement("july 26/pil2/MOVEMENT SHEET JULY 26.xls", "2026-07-01")
    pil2_movement("aug 26/pil2/MOVEMENT SHEET AUG 26 pil2.xls", "2026-08-01")
except Exception as e:
    print("PIL2 movement error", e)


# Collapse provisions: same company/month/ledger -> sum (Salary payable + Salaries)
collapsed = {}
for co, mo, led, amt, rem in prov_rows:
    k = (co, mo, led)
    if k not in collapsed:
        collapsed[k] = [amt, rem]
    else:
        collapsed[k][0] += amt
prov_final = [(k[0], k[1], k[2], v[0], v[1]) for k, v in collapsed.items() if abs(v[0]) >= 0.0001]
prov_final.sort(key=lambda x: (x[1], x[0], x[2]))
stock_rows.sort(key=lambda x: (x[1], x[0], x[2]))

print("\nSTOCK")
for r in stock_rows:
    print(f"  {r[1]} | {r[0][:40]:40} | {r[2]:14} {r[3]:10.4f}  {r[4][:50]}")
print("stock rows", len(stock_rows))
print("prov rows", len(prov_final))
from collections import Counter
print("prov by co/month", Counter((p[0], p[1]) for p in prov_final))

lines = []
a = lines.append
a("/* Generated from july 26 / aug 26 factory files + public templates already in stored form.")
a("   Stock: PnlStockValue AmountLacs, PlantName NULL, months 2026-07-01 and 2026-08-01 only.")
a("   Provisions: rupees + EntryType. DELETE/INSERT those two months only. Does not touch Apr-Jun.")
a("   Gaps not loaded: KPW-2 / VAD / HO / HPBL-3 stock; KPW/OEL/HPBL123 stock (qty movement only);")
a("   PIL-2 stock skipped (movement .xls is qty; stors sheet is FY 23-24). PPL Aug packing from STORE REPORT.")
a("   Review CompanyName against FactoryInfo before COMMIT.")
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
    a(f"    SET AmountLacs = {fmt_lacs(lacs)}, UploadedAt = GETDATE(), Remarks = {sql_n(rem)}")
    a(f"    WHERE CompanyName = {sql_n(co)} AND StockMonth = '{mo}' AND Category = {sql_n(cat)} AND PlantName IS NULL;")
    a("ELSE")
    a("    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)")
    a(f"    VALUES ({sql_n(co)}, NULL, '{mo}', {sql_n(cat)}, {fmt_lacs(lacs)}, {sql_n(rem)}, GETDATE());")
    a("")

# provisions grouped
from itertools import groupby
for (co, mo), grp in groupby(prov_final, key=lambda x: (x[0], x[1])):
    a(f"DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = {sql_n(co)} AND sysdate >= '{mo}' AND sysdate < DATEADD(month, 1, '{mo}');")
    for _, _, led, amt, rem in grp:
        et = entry_type(led)
        a(f"INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES ({sql_n(co)}, '{mo}', {sql_n(led)}, {fmt_rs(amt)}, {sql_n(rem)}, {sql_n(et)});")
    a("")

a("COMMIT;")
a("-- ROLLBACK;")
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", OUT, "lines", len(lines))
