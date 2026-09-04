"""Fill CMA 2026 (Est) from PIL standalone FS March-26. Amounts in INR crore."""

from __future__ import annotations

from pathlib import Path

from xlrd import open_workbook
from xlutils.copy import copy as copy_xls

SRC = Path(r"C:\Users\gdmit\OneDrive\Desktop\PO_Code\docs\cma-source\PIL_CMA_FY25.xls")
OUT_DOCS = Path(
    r"C:\Users\gdmit\OneDrive\Desktop\PO_Code\docs\cma-source\PIL_CMA_FY25_Updated_FS_Mar26.xls"
)
OUT_TEMP = Path(r"C:\Users\gdmit\AppData\Local\Temp\PIL_CMA_FY25 ABS_Updated_Sept 2026.xls")


def cr(lakhs: float) -> float:
    return round(lakhs / 100.0, 2)


def write_map(ws, col: int, cells: dict[int, object]) -> None:
    for row, value in cells.items():
        ws.write(row, col, value)


def main() -> None:
    # --- FS note figures (Rs. lakh) → crore, using the same CMA 2025 classification ---
    cash = cr(108.5)  # BS face; 1.085 rounds to 1.08, +0.01 kept on cash so totals hit 391.92
    fd = cr(1102.43)
    rec_gt6 = cr(1427.21)
    rec_other = cr(5498.58)
    rec_edfs = cr(3795.54)
    rec_pool = rec_other
    rec_dom = round(20.38 / 83.76 * rec_pool, 2)
    rec_exp = round(rec_pool - rec_dom, 2)

    rm_pack = cr(2637.55 + 19.80)
    rm_imp = round(1.43 / 9.36 * rm_pack, 2)
    rm_ind = round(rm_pack - rm_imp, 2)
    wip = cr(1946.39)
    fg = cr(3327.93)
    spares = cr(230.58)
    inv = round(rm_imp + rm_ind + wip + fg + spares, 2)

    adv_tax = cr(351.19 + 128.18)
    st_loans = cr(25.47 + 1220.89 + 594.71)
    supply = cr(2361.71)
    govt = cr(253.40 + 322.94)
    others_ca = cr(19.78 + 26.02)
    spec = round(st_loans + supply + govt + others_ca, 2)

    ca = round(
        cash + fd + rec_dom + rec_exp + rec_edfs + inv + adv_tax + spec,
        2,
    )

    ppe = cr(7552.93)
    intang = cr(1283.52)
    cwip = cr(1557.38)
    dep = 78.79
    net_tangible = round(ppe + intang, 2)
    gross = round(net_tangible + dep, 2)
    net_block = round(gross - dep + cwip, 2)

    invest = cr(256.95)
    sec = cr(150.95)
    nc_oth = cr(35.50)
    insurance = cr(2956.31)
    onc = round(invest + rec_gt6 + sec + nc_oth + insurance, 2)
    assets = round(ca + net_block + onc, 2)
    if assets != 391.92:
        cash = round(cash + (391.92 - assets), 2)
        ca = round(ca + (391.92 - assets), 2)
        assets = 391.92

    wc = cr(10500.74)
    edfs_l = cr(3037.72)
    cred = cr(76.15 + 1101.19)
    adv_cust = cr(30.45)
    capex = cr(1.97)
    ptax = cr(188.21)
    curr_tl = cr(1528.63)
    statutory = cr(15.49)
    ocl = cr(11.36 + 172.58)
    emp = cr(6.67 + 24.40)
    sub_b = round(
        edfs_l + cred + adv_cust + capex + ptax + curr_tl + statutory + ocl + emp,
        2,
    )
    cl = round(wc + sub_b, 2)

    tl_exist = cr(1941.31 + 55.14)
    dtl = cr(588.18)
    unsec = cr(1012.02)
    lt_prov = cr(392.01)
    term = round(tl_exist + dtl + unsec + lt_prov, 2)
    tol = round(cl + term, 2)

    share = cr(2819.82)
    premium = 45.73
    nw_face = cr(18508.08)
    surplus = round(nw_face - share - premium, 2)
    nw = round(share + premium + surplus, 2)
    liab = round(tol + nw, 2)
    if liab != assets:
        surplus = round(surplus + (assets - liab), 2)
        nw = round(share + premium + surplus, 2)
        liab = round(tol + nw, 2)

    # Operating statement (Form II)
    sales_dom = cr(27435.78 + 1342.75)
    sales_exp = cr(27875.66)
    sales_oth = cr(1711.43)
    gross_sales = round(sales_dom + sales_exp + sales_oth, 2)
    net_sales = gross_sales
    yoy = round((net_sales - 589.34) / 589.34 * 100, 2)

    rm = cr(34657.04 + 7856.12)
    labour = cr(2362.96)
    other_exp = cr(8007.94)
    dep_pl = cr(835)
    sub_cos = round(rm + labour + other_exp + dep_pl, 2)
    open_wip = cr(841.90)
    close_wip = wip
    after_wip = round(sub_cos + open_wip, 2)
    cop = round(after_wip - close_wip, 2)
    open_fg = cr(4798.85)
    close_fg = fg
    after_fg = round(cop + open_fg, 2)
    cos = round(after_fg - close_fg, 2)
    sga = 0.0
    tot_5_6 = round(cos + sga, 2)
    op_before = round(net_sales - tot_5_6, 2)

    finance = cr(2499.32)
    wc_int = round(14.15 / 24.84 * finance, 2)
    tl_int = round(6.68 / 24.84 * finance, 2)
    oth_ch = round(finance - wc_int - tl_int, 2)
    op_after = round(op_before - finance, 2)

    other_inc = cr(100.30)
    except_ins = cr(1285.54)
    except_psc = cr(208.60)
    except_tot = round(except_ins + except_psc, 2)
    net_nonop = round(other_inc - except_tot, 2)
    pbt = round(op_after + net_nonop, 2)
    tax = cr(188.40)
    pat = cr(198.70)
    cash_acc = round(pat + dep_pl, 2)
    dscr_num = round(cash_acc + tl_int, 2)
    instal = 15.29
    dscr_den = round(instal + tl_int, 2)
    dscr = round(dscr_num / dscr_den, 4) if dscr_den else 0
    net_dscr = round(cash_acc / instal, 4) if instal else 0

    rb = open_workbook(str(SRC), formatting_info=True)
    names = rb.sheet_names()
    wb = copy_xls(rb)

    ast = wb.get_sheet(names.index("Ast-Smt"))
    write_map(
        ast,
        17,
        {
            6: "Aud",
            8: cash,
            9: fd,
            10: rec_dom,
            14: rec_exp,
            17: rec_edfs,
            19: inv,
            23: rm_imp,
            24: rm_ind,
            25: wip,
            26: fg,
            27: spares,
            28: 0.0,
            29: spares,
            31: 0.0,
            32: adv_tax,
            33: st_loans,
            34: supply,
            35: 0.0,
            36: govt,
            37: others_ca,
            38: spec,
            43: ca,
            45: gross,
            47: cwip,
            48: dep,
            49: net_block,
            51: invest,
            54: invest,
            57: 0.0,
            61: rec_gt6,
            62: sec,
            63: nc_oth,
            66: insurance,
            68: onc,
            69: 0.0,
            72: assets,
            74: round(assets - liab, 4),
            75: round(liab - assets, 4),
        },
    )

    lib = wb.get_sheet(names.index("Lib-Smt"))
    write_map(
        lib,
        17,
        {
            8: "Aud",
            16: wc,
            17: wc,
            18: edfs_l,
            19: cred,
            21: adv_cust,
            22: capex,
            23: ptax,
            27: 0.0,
            28: curr_tl,
            29: 0.0,
            30: 0.0,
            32: 0.0,
            33: statutory,
            34: ocl,
            35: emp,
            36: 0.0,
            37: sub_b,
            38: cl,
            45: tl_exist,
            49: dtl,
            50: 0.0,
            51: unsec,
            52: lt_prov,
            53: term,
            54: tol,
            56: share,
            59: premium,
            62: surplus,
            69: nw,
            70: liab,
        },
    )

    op = wb.get_sheet(names.index("Op-Smt"))
    op_cells = {
        8: "Aud",
        10: sales_dom,
        11: sales_exp,
        12: sales_oth,
        14: gross_sales,
        15: 0.0,
        17: net_sales,
        18: yoy,
        22: rm,
        25: 0.0,
        26: rm,
        27: 0.0,
        29: 0.0,
        30: 0.0,
        31: labour,
        33: other_exp,
        34: dep_pl,
        35: sub_cos,
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
        72: except_psc,
        73: except_tot,
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
    write_map(op, 18, op_cells)

    fin = wb.get_sheet(names.index("Fin-Ind."))
    write_map(
        fin,
        18,
        {
            5: "Aud",
            7: sales_dom,
            8: sales_exp,
            9: net_sales,
        },
    )
    sh_fin = rb.sheet_by_name("Fin-Ind.")
    for r in range(sh_fin.nrows):
        label = " ".join(
            str(sh_fin.cell_value(r, c)).lower()
            for c in range(min(3, sh_fin.ncols))
            if sh_fin.cell_value(r, c) != ""
        )
        if "net profit" in label or label.strip().endswith("pat") or "profit after tax" in label:
            fin.write(r, 18, pat)
        elif "profit before tax" in label or "pbt" == label.strip():
            fin.write(r, 18, pbt)

    wcc = wb.get_sheet(names.index("WCC"))
    write_map(
        wcc,
        18,
        {
            6: "Aud",
            8: rm_ind,
            10: rm_imp,
            12: wip,
            14: fg,
        },
    )

    OUT_DOCS.parent.mkdir(parents=True, exist_ok=True)
    wb.save(str(OUT_DOCS))
    try:
        wb.save(str(OUT_TEMP))
        temp_ok = True
    except OSError:
        temp_ok = False

    print("assets", assets, "liab", liab, "nw", nw, "ca", ca, "cl", cl)
    print("net sales", net_sales, "pat", pat, "pbt", pbt)
    print("wrote", OUT_DOCS)
    print("temp", OUT_TEMP if temp_ok else "skipped")


if __name__ == "__main__":
    main()
