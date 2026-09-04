"""Fill Op-Smt 2026 from PIL FS March-26, same classification as 2025 CMA."""

from pathlib import Path

from xlrd import open_workbook
from xlutils.copy import copy as copy_xls

SRC = Path(r"C:\Users\gdmit\OneDrive\Desktop\PO_Code\docs\cma-source\PIL_CMA_FY25.xls")
OUTS = [
    Path(r"C:\Users\gdmit\OneDrive\Desktop\CMA_OpStmt_2026_FROM_FS.xls"),
    Path(r"C:\Users\gdmit\OneDrive\Desktop\PO_Code\docs\cma-source\CMA_OpStmt_2026_FROM_FS.xls"),
    Path(r"C:\Users\gdmit\AppData\Local\Temp\CMA_OpStmt_2026_FROM_FS.xls"),
]

COL = 18  # Excel column S = year 2026


def cr(lakhs: float) -> float:
    return round(lakhs / 100.0, 2)


def r2(*xs: float) -> float:
    return round(sum(xs), 2)


def main() -> None:
    # Note 18 — same treatment as 2025: domestic includes job work
    sales_dom = cr(27435.7754928 + 1342.746984)
    sales_exp = cr(27875.6565473)
    sales_oth = cr(1711.429917756088)
    gross = r2(sales_dom, sales_exp, sales_oth)
    net = gross
    yoy = round((net - 589.34) / 589.34 * 100, 2)

    # Note 20 + 21
    rm_total = cr(34657.04 + 7856.123723811)
    rm_imp = round(rm_total * 162.16 / 455.70, 2)
    rm_ind = r2(rm_total, -rm_imp)

    # Note 25 manufacturing / CMA helper lines
    spares = cr(670.2765417)
    power = cr(1472.0306426)
    jobwork = cr(3786.8789721)
    emp = cr(2362.96)
    labour = r2(emp, jobwork)
    opex = cr(487.7626119)  # FS helper "Manufactureing exp" is 4.8776 Cr
    # The helper cell was already in crore (4.8776). Detect: 4.8776 lakhs would be tiny.
    opex = 4.88
    sga = 12.36
    nonop_25 = 3.55

    dep = cr(835.0071838135996)
    sub_i_vi = r2(rm_total, spares, power, labour, opex, dep)

    # Note 15 / 22 inventory
    open_wip = cr(841.9088223802382)
    close_wip = cr(1946.39)
    after_wip = r2(sub_i_vi, open_wip)
    cop = r2(after_wip, -close_wip)
    open_fg = cr(4798.85433607279)
    close_fg = cr(3327.93)
    after_fg = r2(cop, open_fg)
    cos = r2(after_fg, -close_fg)
    tot_5_6 = r2(cos, sga)
    op_before = r2(net, -tot_5_6)

    # Note 24
    wc_int = cr(1387.7717037)
    tl_int = cr(419.4173757)
    oth_ch = cr(92.37178781060247 + 286.9795051 + 312.77812585378)
    finance = r2(wc_int, tl_int, oth_ch)
    op_after = r2(op_before, -finance)

    other_inc = cr(100.35)
    except_ins = cr(1285.537841531)
    except_psc = cr(208.55)
    nonop_exp = r2(nonop_25, except_ins, except_psc)
    net_nonop = r2(other_inc, -nonop_exp)
    pbt = r2(op_after, net_nonop)
    tax = cr(188.35315847177418)
    pat = cr(198.70899999722576)

    cash_acc = r2(pat, dep)
    instal = cr(1528.63)
    dscr_den = r2(instal, tl_int)
    dscr_num = r2(cash_acc, tl_int)
    dscr = round(dscr_num / dscr_den, 4) if dscr_den else 0
    net_dscr = round(cash_acc / instal, 4) if instal else 0

    rb = open_workbook(str(SRC), formatting_info=True)
    wb = copy_xls(rb)
    op = wb.get_sheet(rb.sheet_names().index("Op-Smt"))

    cells = {
        8: "Aud",
        10: sales_dom,
        11: sales_exp,
        12: sales_oth,
        14: gross,
        15: 0.0,
        17: net,
        18: yoy,
        22: rm_total,
        23: rm_imp,
        24: rm_ind,
        25: rm_imp,
        26: rm_ind,
        27: spares,
        29: spares,
        30: power,
        31: labour,
        33: opex,
        34: dep,
        35: sub_i_vi,
        36: open_wip,
        37: after_wip,
        40: "Aud",
        42: close_wip,
        43: 0.0,
        44: cop,
        46: open_fg,
        47: after_fg,
        49: close_fg,
        50: 0.0,
        51: cos,
        53: sga,
        54: tot_5_6,
        55: op_before,
        57: wc_int,
        58: tl_int,
        59: oth_ch,
        60: op_after,
        62: other_inc,
        65: 0.0,
        66: other_inc,
        68: 0.0,
        70: except_ins,
        71: 0.0,
        72: r2(nonop_25, except_psc),
        73: nonop_exp,
        74: net_nonop,
        76: pbt,
        77: tax,
        79: pat,
        81: 0.0,
        85: pat,
        86: 100.0,
        91: cash_acc,
        93: cash_acc,
        94: tl_int,
        95: dscr_num,
        96: instal,
        97: tl_int,
        98: dscr_den,
        99: dscr,
        101: net_dscr,
    }
    for row, value in cells.items():
        op.write(row, COL, value)

    for dest in OUTS:
        dest.parent.mkdir(parents=True, exist_ok=True)
        wb.save(str(dest))

    print("NET SALES", net, "DOM", sales_dom, "EXP", sales_exp, "OTH", sales_oth)
    print("RM", rm_total, "POWER", power, "SPARES", spares, "LABOUR", labour, "OPEX", opex)
    print("DEP", dep, "SG&A", sga, "COS", cos)
    print("WC INT", wc_int, "TL INT", tl_int, "OTHER CH", oth_ch, "FIN", finance)
    print("OP BEFORE", op_before, "OP AFTER", op_after, "PBT", pbt, "TAX", tax, "PAT", pat)
    print("WIP", open_wip, close_wip, "FG", open_fg, close_fg)
    print("wrote", OUTS[0])


if __name__ == "__main__":
    main()
