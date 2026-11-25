"""April–October 2026 cost-per-kg workbook from the exported ERP extracts."""
import csv
from collections import defaultdict
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.chart.shapes import GraphicalProperties
from openpyxl.formatting.rule import CellIsRule, FormulaRule
from openpyxl.workbook.properties import CalcProperties
from openpyxl.worksheet.page import PageMargins
from openpyxl.chart.legend import Legend
from openpyxl.formatting.rule import ColorScaleRule
from openpyxl.comments import Comment

DATA = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp\POApprovalAPI\Scripts\_pkg_data")
OUT = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp\Cost per kg Apr-Oct 2026.xlsx")

COMPANIES = [
    ("PIL 1", "Plastene India Limited"),
    ("PIL 2", "Plastene India Limited (Unit -II)"),
    ("PPL", "Plastene Polyfilms Limited"),
    ("HPBL 1", "HCP Plastene Bulkpack Ltd"),
    ("HPBL 2", "HCP Plastene Bulkpack Ltd (Unit - II)"),
    ("HPBL 3", "HCP Plastene Bulkpack Ltd (Unit - III)"),
    ("HPBL 4", "HCP Plastene Bulkpack Ltd (Unit - IV)"),
    ("Vadodara", "HCP Plastene Bulkpack ltd (Vadodara)"),
    ("KPW 1", "K.P. WOVEN PRIVATE LIMITED"),
    ("KPW 3", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)"),
    ("OEL", "Oswal Extrusion Limited"),
]
MONTHS = ["2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10"]
MONTH_LABEL = {
    "2026-04": "April",
    "2026-05": "May",
    "2026-06": "June",
    "2026-07": "July",
    "2026-08": "August",
    "2026-09": "September",
    "2026-10": "October",
}

BUCKETS = [
    ("packing", "Packing consumed & purchase expenses"),
    ("stores", "Stores consumed, net of scrap sales"),
    ("salaries", "Salaries & staff welfare"),
    ("labour", "Labour charges"),
    ("power", "Power & fuel"),
    ("job", "Jobwork charges"),
    ("omfg", "Other manufacturing"),
    ("travel", "Travelling & conveyance"),
    ("selling", "Selling & distribution"),
    ("insurance", "Insurance"),
    ("admin", "Other administrative"),
    ("fx", "Exchange difference"),
    ("ho", "HO & common expenses"),
]

HEAD_BUCKET = {}


def bind(bucket, *names):
    for name in names:
        HEAD_BUCKET[name.lower()] = bucket


bind("packing", "purchase of packing materials", "purchase expenses", "add: purchase expenses")
bind("stores", "purchase of stores, spares & consumables", "add: purchase of stores, spares & consumables")
bind(
    "salaries",
    "salaries, wages and  bonus",
    "salaries, wages and bonus",
    "contribution to provident and other funds",
    "gratuity expenses",
    "staff welfare expenses",
    "wages and salary",
    "bonus",
    "leave encashment",
)
bind("labour", "labour charges")
bind("power", "power and fuel")
bind("job", "jobwork charges")
bind("omfg", "other manufacturing expenses")
bind("travel", "travelling expense", "conveyance, vehicle running and traveling expenses")
bind(
    "selling",
    "freight outward and c & f charges",
    "freight outward expense",
    "advertising and sales promotion expense",
    "brokerage & commission",
)
bind("insurance", "insurance", "insurance expenses")
bind("admin", "other administrative expenses", "bank charges & commission", "sundry balance written off")
bind("fx", "exchange rate difference")

NAVY = "1B3A4B"
TEAL = "1F6A62"
GOLD = "C4A36A"
CREAM = "F7F4EF"
MIST = "E8F0F1"
WHITE = "FFFFFF"
INK = "1C2830"
MUTED = "5E6A72"
LINE = "D5DEE2"
GREEN = "1B6B45"
GREEN_BG = "E5F4EA"
RED = "9B2C2C"
AMBER = "FBF3E4"
TOTAL = "16324A"
MARGIN = "0F4C3A"

thin = Border(
    left=Side(style="thin", color=LINE),
    right=Side(style="thin", color=LINE),
    top=Side(style="thin", color=LINE),
    bottom=Side(style="thin", color=LINE),
)
fill = lambda hex_: PatternFill("solid", fgColor=hex_)
font = lambda **kw: Font(name="Calibri", **kw)


def read_csv(name):
    with (DATA / name).open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def num(value):
    if value is None or value == "":
        return 0.0
    return float(value)


def load():
    prod = {}
    for row in read_csv("prod.csv"):
        prod[(row["Company"], row["Mth"])] = num(row["Mt"])
    sales = defaultdict(lambda: {"EX": [0.0, 0.0], "IC": [0.0, 0.0]})
    for row in read_csv("sales.csv"):
        sales[(row["Company"], row["Mth"])][row["Kind"]] = [num(row["Amt"]), num(row["Kg"])]
    pp = {}
    for row in read_csv("pp.csv"):
        pp[(row["Company"], row["Mth"])] = [num(row["Amt"]), num(row["Kg"])]
    tb = defaultdict(float)
    for row in read_csv("tb.csv"):
        tb[(row["Company"], row["Mth"], row["Head"])] += num(row["Amt"])
    prov = defaultdict(float)
    for row in read_csv("prov.csv"):
        prov[(row["Company"], row["Mth"], row["Head"])] += num(row["Amt"])
    ho = {}
    for row in read_csv("ho.csv"):
        ho[(row["Company"], row["Mth"])] = (num(row["CommonLacs"]) + num(row["HoLacs"])) * 100_000.0
    stock = {}
    for row in read_csv("stock.csv"):
        stock[(row["Company"], row["Mth"], row["Category"])] = num(row["Lacs"])
    # March stores for Vadodara was never loaded (the table has 0). The April trial balance
    # opening stock of stores is Rs 18,34,912, the same figure as the April closing.
    stock[("HCP Plastene Bulkpack ltd (Vadodara)", "2026-03", "Stores")] = 18.34912
    scrap = {}
    for row in read_csv("scrap.csv"):
        scrap[(row["Company"], row["Mth"])] = num(row["Amt"])
    return prod, sales, pp, tb, prov, ho, stock, scrap


def previous_month(month):
    year, mon = (int(part) for part in month.split("-"))
    if mon == 1:
        return f"{year - 1}-12"
    return f"{year}-{mon - 1:02d}"


def apply_stock_and_sales(cost, company, month, stock, scrap):
    """Opening + purchases − closing − outside scrap sales. Group and interunit sales are left out."""
    prev = previous_month(month)

    def movement(category):
        opening = stock.get((company, prev, category), 0.0)
        closing = stock.get((company, month, category), 0.0)
        return (opening - closing) * 100_000.0

    cost["stores"] += movement("Stores") - scrap.get((company, month), 0.0)
    cost["packing"] += movement("Packing")
    return cost


def month_heads(company, month, tb, prov):
    idx = MONTHS.index(month)
    totals = defaultdict(float)
    for (co, mth, head), amt in tb.items():
        if co == company and mth == month:
            totals[head] += amt
    for (co, mth, head), amt in prov.items():
        if co == company and mth == month:
            totals[head] += amt
        if idx > 0 and co == company and mth == MONTHS[idx - 1]:
            totals[head] -= amt
    return totals


def buckets_for(heads, ho_rupees):
    out = {key: 0.0 for key, _ in BUCKETS}
    for head, amt in heads.items():
        bucket = HEAD_BUCKET.get(" ".join(head.split()).lower())
        if bucket:
            out[bucket] += amt
    out["ho"] += ho_rupees
    return out


def sales_choice(block):
    ex_amt, ex_kg = block["EX"]
    ic_amt, ic_kg = block["IC"]
    if ex_kg >= 1000:
        return ex_amt, ex_kg, False
    if ex_kg + ic_kg > 0:
        return ex_amt + ic_amt, ex_kg + ic_kg, ex_kg < 1000
    return 0.0, 0.0, False


def per_kg(rupees, mt):
    if not mt:
        return None
    return rupees / (mt * 1000.0)


def build_company(prod, sales, pp, tb, prov, ho, stock, scrap, company):
    months = []
    for month in MONTHS:
        mt = prod.get((company, month), 0.0)
        sale = sales_choice(sales.get((company, month), {"EX": [0.0, 0.0], "IC": [0.0, 0.0]}))
        purchase = pp.get((company, month), [0.0, 0.0])
        heads = month_heads(company, month, tb, prov)
        cost = apply_stock_and_sales(
            buckets_for(heads, ho.get((company, month), 0.0)),
            company,
            month,
            stock,
            scrap,
        )
        months.append(
            {
                "mt": mt,
                "sale_amt": sale[0],
                "sale_kg": sale[1],
                "sale_fallback": sale[2],
                "pp_amt": purchase[0],
                "pp_kg": purchase[1],
                "cost": cost,
            }
        )
    return months


def apply_fill_font(cell, fg, color="1C2830", bold=False, size=11, align="right"):
    cell.fill = fill(fg)
    cell.font = font(color=color, bold=bold, size=size)
    cell.alignment = Alignment(horizontal=align, vertical="center")
    cell.border = thin


def write_company(ws, short, company, months):
    last_col = 1 + len(MONTHS) * 2 + 2
    ws.sheet_view.showGridLines = False
    ws.sheet_view.zoomScale = 110
    ws.page_setup.orientation = "landscape"
    ws.page_setup.fitToPage = True
    ws.page_setup.fitToWidth = 1
    ws.page_setup.fitToHeight = 1
    ws.page_setup.paperSize = ws.PAPERSIZE_A3
    ws.page_margins = PageMargins(left=0.4, right=0.4, top=0.5, bottom=0.5, header=0.2, footer=0.2)
    ws.sheet_properties.tabColor = TEAL
    ws.oddHeader.left.text = short
    ws.oddFooter.left.text = "Qty: MT    Rates: Rs/kg    Costs: Rs lacs    ·    April to October 2026"
    ws.oddFooter.right.text = "Page &P of &N"
    ws.print_title_rows = "1:5"
    ws.freeze_panes = "B6"
    ws.auto_filter.ref = None
    ws.sheet_view.view = "pageBreakPreview"
    ws.sheet_view.view = "normal"

    ws.merge_cells(start_row=1, start_column=1, end_row=1, end_column=last_col)
    banner = ws.cell(1, 1, company)
    banner.font = font(color=WHITE, bold=True, size=18)
    banner.fill = fill(NAVY)
    banner.alignment = Alignment(horizontal="left", vertical="center", indent=1)
    ws.row_dimensions[1].height = 32

    note = "Quantities in metric tons    ·    rates in Rs per kg    ·    costs in Rs lacs"
    if short == "HPBL 3":
        note += "    ·    no provisions were available, so these rows are the trial balance only"
    if short == "KPW 3":
        note += "    ·    bag production is not in the production view, so Rs per kg is blank"
    ws.merge_cells(start_row=2, start_column=1, end_row=2, end_column=last_col)
    sub = ws.cell(2, 1, note)
    sub.font = font(color="E7D7B8", size=10, italic=True)
    sub.fill = fill(NAVY)
    sub.alignment = Alignment(horizontal="left", vertical="center", indent=1)
    ws.row_dimensions[2].height = 18

    ws.row_dimensions[3].height = 8
    for col in range(1, last_col + 1):
        ws.cell(3, col).fill = fill(CREAM)

    label = ws.cell(4, 1, "")
    label.fill = fill(NAVY)
    label.border = thin
    ws.cell(5, 1, "Particulars").font = font(color=WHITE, bold=True, size=10)
    ws.cell(5, 1).fill = fill(NAVY)
    ws.cell(5, 1).alignment = Alignment(horizontal="left", vertical="center", indent=1)
    ws.cell(5, 1).border = thin

    for i, month in enumerate(MONTHS):
        c1 = 2 + i * 2
        c2 = c1 + 1
        ws.merge_cells(start_row=4, start_column=c1, end_row=4, end_column=c2)
        head = ws.cell(4, c1, MONTH_LABEL[month])
        tone = NAVY if i % 2 == 0 else "245066"
        head.font = font(color=WHITE, bold=True, size=12)
        head.fill = fill(tone)
        head.alignment = Alignment(horizontal="center", vertical="center")
        ws.cell(4, c2).fill = fill(tone)
        ws.cell(4, c1).border = thin
        ws.cell(4, c2).border = thin
        for col, text in ((c1, "Amount"), (c2, "Rs / kg")):
            cell = ws.cell(5, col, text)
            cell.font = font(color="D5E4E2", bold=True, size=9)
            cell.fill = fill(TEAL if col == c2 else "1A5C56")
            cell.alignment = Alignment(horizontal="center", vertical="center")
            cell.border = thin
    y1, y2 = last_col - 1, last_col
    ws.merge_cells(start_row=4, start_column=y1, end_row=4, end_column=y2)
    ytd = ws.cell(4, y1, "April–October")
    ytd.font = font(color=NAVY, bold=True, size=12)
    ytd.fill = fill(GOLD)
    ytd.alignment = Alignment(horizontal="center", vertical="center")
    ws.cell(4, y2).fill = fill(GOLD)
    ws.cell(4, y1).border = thin
    ws.cell(4, y2).border = thin
    for col, text in ((y1, "Amount"), (y2, "Rs / kg")):
        cell = ws.cell(5, col, text)
        cell.font = font(color=NAVY, bold=True, size=9)
        cell.fill = fill("E4C98A" if col == y2 else "D7B56A")
        cell.alignment = Alignment(horizontal="center", vertical="center")
        cell.border = thin
    ws.row_dimensions[4].height = 22
    ws.row_dimensions[5].height = 18

    rows = []
    rows.append(("section", "Quantity (metric tons) and price (Rs per kg)"))
    rows.append(("mt", "Production (MT)"))
    rows.append(("sale_mt", "FIBC sold (MT)"))
    rows.append(("sale_rate", "FIBC sales rate (Rs/kg)"))
    rows.append(("pp_mt", "PP granules purchased (MT)"))
    rows.append(("pp_rate", "PP purchase rate (Rs/kg)"))
    rows.append(("spread", "Spread (Rs/kg)  ·  sales rate minus PP rate"))
    rows.append(("section", "Cost (Rs lacs) and cost per kg (Rs/kg)"))
    for key, label_text in BUCKETS:
        rows.append(("cost", f"{label_text} (Rs lacs)", key))
    rows.append(("total", "Total cost (Rs lacs)"))
    rows.append(("margin", "Margin per kg (Rs/kg)  ·  spread minus total cost"))

    def put(row, col, value, kind, fg, color=INK, bold=False, fmt=None):
        cell = ws.cell(row, col, None if value is None else value)
        apply_fill_font(cell, fg, color, bold, 11 if bold else 10, "right" if col > 1 else "left")
        if col == 1:
            cell.alignment = Alignment(horizontal="left", vertical="center", indent=1)
        if fmt and value is not None:
            cell.number_format = fmt
        return cell

    ytd_mt = sum(m["mt"] for m in months)
    ytd_sale_amt = sum(m["sale_amt"] for m in months)
    ytd_sale_kg = sum(m["sale_kg"] for m in months)
    ytd_pp_amt = sum(m["pp_amt"] for m in months)
    ytd_pp_kg = sum(m["pp_kg"] for m in months)
    ytd_cost = {key: sum(m["cost"][key] for m in months) for key, _ in BUCKETS}
    ytd_total = sum(ytd_cost.values())
    ytd_sale_rate = ytd_sale_amt / ytd_sale_kg if ytd_sale_kg else None
    ytd_pp_rate = ytd_pp_amt / ytd_pp_kg if ytd_pp_kg else None
    ytd_spread = None if ytd_sale_rate is None or ytd_pp_rate is None else ytd_sale_rate - ytd_pp_rate
    ytd_cost_pk = per_kg(ytd_total, ytd_mt)
    ytd_margin = None if ytd_spread is None or ytd_cost_pk is None else ytd_spread - ytd_cost_pk

    excel_row = 6
    rate_fmt = '#,##0.00;[Red]-#,##0.00;"–"'
    amt_fmt = '#,##0.00;[Red]-#,##0.00;"–"'
    mt_fmt = '#,##0.00;"–"'
    for spec in rows:
        kind = spec[0]
        ws.row_dimensions[excel_row].height = 20 if kind != "section" else 22
        if kind == "section":
            ws.merge_cells(start_row=excel_row, start_column=1, end_row=excel_row, end_column=last_col)
            cell = ws.cell(excel_row, 1, spec[1].upper())
            cell.font = font(color=WHITE, bold=True, size=10)
            cell.fill = fill(TEAL)
            cell.alignment = Alignment(horizontal="left", vertical="center", indent=1)
            for col in range(1, last_col + 1):
                ws.cell(excel_row, col).fill = fill(TEAL)
                ws.cell(excel_row, col).border = thin
            excel_row += 1
            continue

        label_text = spec[1]
        put(excel_row, 1, label_text, kind, CREAM if kind in ("spread", "total", "margin") else WHITE, INK, kind in ("spread", "total", "margin"))
        if kind in ("total", "margin"):
            ws.cell(excel_row, 1).fill = fill(TOTAL if kind == "total" else MARGIN)
            ws.cell(excel_row, 1).font = font(color=WHITE, bold=True, size=11)

        values = []
        for item in months:
            sale_rate = item["sale_amt"] / item["sale_kg"] if item["sale_kg"] else None
            pp_rate = item["pp_amt"] / item["pp_kg"] if item["pp_kg"] else None
            spread = None if sale_rate is None or pp_rate is None else sale_rate - pp_rate
            total = sum(item["cost"].values())
            cost_pk = per_kg(total, item["mt"])
            margin = None if spread is None or cost_pk is None else spread - cost_pk
            if kind == "mt":
                values.append((item["mt"] or None, None, False, mt_fmt, None))
            elif kind == "sale_mt":
                values.append(((item["sale_kg"] / 1000.0) if item["sale_kg"] else None, None, item["sale_fallback"], mt_fmt, None))
            elif kind == "sale_rate":
                values.append((None, sale_rate, item["sale_fallback"], None, rate_fmt))
            elif kind == "pp_mt":
                values.append(((item["pp_kg"] / 1000.0) if item["pp_kg"] else None, None, False, mt_fmt, None))
            elif kind == "pp_rate":
                values.append((None, pp_rate, False, None, rate_fmt))
            elif kind == "spread":
                values.append((None, spread, False, None, rate_fmt))
            elif kind == "cost":
                rupees = item["cost"][spec[2]]
                values.append((rupees / 100_000.0, per_kg(rupees, item["mt"]), False, amt_fmt, rate_fmt))
            elif kind == "total":
                values.append((total / 100_000.0, cost_pk, False, amt_fmt, rate_fmt))
            else:
                values.append((None, margin, False, None, rate_fmt))

        if kind == "mt":
            ytd_pair = (ytd_mt or None, None, False, mt_fmt, None)
        elif kind == "sale_mt":
            ytd_pair = ((ytd_sale_kg / 1000.0) if ytd_sale_kg else None, None, False, mt_fmt, None)
        elif kind == "sale_rate":
            ytd_pair = (None, ytd_sale_rate, False, None, rate_fmt)
        elif kind == "pp_mt":
            ytd_pair = ((ytd_pp_kg / 1000.0) if ytd_pp_kg else None, None, False, mt_fmt, None)
        elif kind == "pp_rate":
            ytd_pair = (None, ytd_pp_rate, False, None, rate_fmt)
        elif kind == "spread":
            ytd_pair = (None, ytd_spread, False, None, rate_fmt)
        elif kind == "cost":
            rupees = ytd_cost[spec[2]]
            ytd_pair = (rupees / 100_000.0, per_kg(rupees, ytd_mt), False, amt_fmt, rate_fmt)
        elif kind == "total":
            ytd_pair = (ytd_total / 100_000.0, ytd_cost_pk, False, amt_fmt, rate_fmt)
        else:
            ytd_pair = (None, ytd_margin, False, None, rate_fmt)
        values.append(ytd_pair)

        for i, (amount, rate, amber, amount_fmt, rate_fmt_i) in enumerate(values):
            c1 = 2 + i * 2
            c2 = c1 + 1
            amount_bg = WHITE
            rate_bg = MIST
            color = INK
            bold = False
            if kind == "spread":
                amount_bg = GREEN_BG
                rate_bg = GREEN_BG
                bold = True
            elif kind == "total":
                amount_bg = TOTAL
                rate_bg = TOTAL
                color = WHITE
                bold = True
            elif kind == "margin":
                amount_bg = MARGIN
                rate_bg = MARGIN
                color = WHITE
                bold = True
            elif amber and kind in ("sale_mt", "sale_rate"):
                amount_bg = AMBER
                rate_bg = AMBER
            elif i == len(values) - 1:
                amount_bg = "F8F1DE"
                rate_bg = "F3E6C4"
            put(excel_row, c1, amount, kind, amount_bg, color, bold, amount_fmt)
            put(excel_row, c2, rate, kind, rate_bg, color, bold, rate_fmt_i)
            if amber and kind == "sale_rate" and rate is not None and i < len(MONTHS):
                ws.cell(excel_row, c2).comment = Comment(
                    "Outside-customer FIBC was under 1 MT, so this rate uses all FIBC sales including group transfers.",
                    "Cost per kg",
                    width=240,
                    height=50,
                )
        excel_row += 1

    excel_row += 1
    notes = [
        "Production is bag weight from the FIBC production view, in metric tons.",
        "FIBC sales rate is outside-customer FIBC bags. An amber cell means that month had under 1 MT sold outside the group, so group transfers are included.",
        "PP purchase rate is PP granule purchases, including purchases from other group companies. Filler and masterbatch are not included.",
        "Cost amounts are in Rs lacs. The OEL working sheet stores that same figure times 100, so 96.00 lacs is written there as 9,600, and 9,600 divided by tonnes equals Rs per kg. This file shows 96.00 in the amount column and the same Rs per kg beside it.",
        "Cost rows other than stores and packing are the month's trial balance plus that month's provisions, minus the previous month's provisions. Raw-material purchases are not in these rows; they are in the PP rate.",
        "Stores consumed is opening stock plus purchases minus closing stock minus scrap and waste sold to outside customers. Sales to other group companies and other units are not deducted. Packing consumed is opening stock plus packing purchases minus closing stock, and purchase expenses stay in that row. Opening stock is the previous month's closing.",
        "Rs per kg divides the rupee cost by production kilograms. April to October rates are weighted by the kilograms or tonnes in that column, not a simple average of the months.",
        "The trial balance is live. A voucher posted later into an earlier month will change that month.",
    ]
    for text in notes:
        ws.merge_cells(start_row=excel_row, start_column=1, end_row=excel_row, end_column=last_col)
        cell = ws.cell(excel_row, 1, text)
        cell.font = font(color=MUTED, size=9, italic=True)
        cell.alignment = Alignment(horizontal="left", vertical="center", indent=1)
        cell.fill = fill(CREAM)
        ws.row_dimensions[excel_row].height = 16
        excel_row += 1

    ws.column_dimensions["A"].width = 54
    for col in range(2, last_col + 1):
        ws.column_dimensions[get_column_letter(col)].width = 13
    ws.auto_filter.ref = None
    ws.page_setup.horizontalCentered = True
    ws.sheet_view.showGridLines = False
    ws.oddHeader.center.text = ""
    return {
        "mt": ytd_mt,
        "sale_rate": ytd_sale_rate,
        "pp_rate": ytd_pp_rate,
        "cost_pk": ytd_cost_pk,
        "margin": ytd_margin,
    }


def write_index(wb, summary):
    ws = wb.active
    ws.title = "Index"
    last = 8
    ws.sheet_view.showGridLines = False
    ws.sheet_view.zoomScale = 120
    ws.page_setup.orientation = "landscape"
    ws.page_setup.fitToPage = True
    ws.page_setup.fitToWidth = 1
    ws.page_setup.fitToHeight = 1
    ws.page_setup.paperSize = ws.PAPERSIZE_A3
    ws.page_margins = PageMargins(left=0.5, right=0.5, top=0.5, bottom=0.5)
    ws.sheet_properties.tabColor = GOLD
    ws.merge_cells("A1:H1")
    title = ws.cell(1, 1, "Cost per kilogram")
    title.font = font(color=WHITE, bold=True, size=26)
    title.fill = fill(NAVY)
    title.alignment = Alignment(horizontal="left", vertical="center", indent=1)
    ws.row_dimensions[1].height = 42
    ws.merge_cells("A2:H2")
    sub = ws.cell(2, 1, "April to October 2026    ·    production in MT    ·    rates in Rs per kg    ·    open a plant for cost in Rs lacs")
    sub.font = font(color="E7D7B8", size=12, italic=True)
    sub.fill = fill(NAVY)
    sub.alignment = Alignment(horizontal="left", vertical="center", indent=1)
    ws.row_dimensions[2].height = 22
    ws.row_dimensions[3].height = 12
    for col in range(1, 9):
        ws.cell(3, col).fill = fill(CREAM)

    headers = [
        "Plant",
        "Production (MT)",
        "FIBC rate (Rs/kg)",
        "PP rate (Rs/kg)",
        "Spread (Rs/kg)",
        "Cost (Rs/kg)",
        "Margin (Rs/kg)",
        "Sheet",
    ]
    for col, text in enumerate(headers, 1):
        cell = ws.cell(4, col, text)
        cell.font = font(color=WHITE, bold=True, size=11)
        cell.fill = fill(TEAL)
        cell.alignment = Alignment(horizontal="center" if col > 1 else "left", vertical="center", indent=1 if col == 1 else 0)
        cell.border = thin
    ws.row_dimensions[4].height = 22
    ws.auto_filter.ref = "A4:H15"
    ws.freeze_panes = "A5"

    for i, ((short, company), stats) in enumerate(summary):
        row = 5 + i
        ws.row_dimensions[row].height = 22
        bg = WHITE if i % 2 == 0 else CREAM
        name = ws.cell(row, 1, short)
        apply_fill_font(name, bg, INK, True, 12, "left")
        name.alignment = Alignment(horizontal="left", vertical="center", indent=1)
        pairs = [
            (2, stats["mt"], '#,##0.00'),
            (3, stats["sale_rate"], '#,##0.00'),
            (4, stats["pp_rate"], '#,##0.00'),
            (5, None if stats["sale_rate"] is None or stats["pp_rate"] is None else stats["sale_rate"] - stats["pp_rate"], '#,##0.00;[Red]-#,##0.00'),
            (6, stats["cost_pk"], '#,##0.00'),
            (7, stats["margin"], '#,##0.00;[Red]-#,##0.00'),
        ]
        for col, value, fmt in pairs:
            cell = ws.cell(row, col, value)
            tone = GREEN_BG if col == 7 and value is not None and value >= 0 else bg
            if col == 7 and value is not None and value < 0:
                tone = "FDECEC"
            apply_fill_font(cell, tone, INK, col == 7, 11, "right")
            if value is not None:
                cell.number_format = fmt
        link = ws.cell(row, 8, "Open")
        link.hyperlink = f"#'{short}'!A1"
        link.font = font(color=TEAL, bold=True, size=11, underline="single")
        link.fill = fill(bg)
        link.alignment = Alignment(horizontal="center", vertical="center")
        link.border = thin

    ws.merge_cells("A17:H17")
    foot = ws.cell(17, 1, "April–October figures are weighted by tonnes and kilograms. Amber rates on a plant sheet include group transfers because outside sales were under 1 MT.")
    foot.font = font(color=MUTED, size=9, italic=True)
    foot.alignment = Alignment(horizontal="left", vertical="center", indent=1)
    widths = [16, 18, 18, 16, 16, 14, 16, 12]
    for i, width in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = width
    ws.row_dimensions[17].height = 20
    ws.sheet_view.view = "normal"
    ws.print_options.horizontalCentered = True


def main():
    prod, sales, pp, tb, prov, ho, stock, scrap = load()
    wb = Workbook()
    summary = []
    for short, company in COMPANIES:
        months = build_company(prod, sales, pp, tb, prov, ho, stock, scrap, company)
        ws = wb.create_sheet(short)
        stats = write_company(ws, short, company, months)
        summary.append(((short, company), stats))
    write_index(wb, summary)
    for ws in wb.worksheets:
        ws.page_setup.horizontalCentered = True
        ws.sheet_view.zoomScale = 110 if ws.title != "Index" else 120
        ws.sheet_format.defaultRowHeight = 18
    wb.properties.title = "Cost per kg April to October 2026"
    wb.properties.creator = "PO Approval"
    try:
        wb.save(OUT)
        print(OUT)
    except PermissionError:
        fallback = OUT.with_name("Cost per kg Apr-Oct 2026 updated.xlsx")
        wb.save(fallback)
        print(fallback)


if __name__ == "__main__":
    main()
