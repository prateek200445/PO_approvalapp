import xlrd

def analyze(f, name):
    wb = xlrd.open_workbook(f)
    sh = wb.sheet_by_index(0)
    print(f"=== {name} ===")
    qty = bag_type = po = customer = ico = size = swl = fabric_color = None
    fabric_lines = []
    for r in range(sh.nrows):
        c0 = str(sh.cell_value(r, 0)).strip()
        c2 = str(sh.cell_value(r, 2)).strip() if sh.ncols > 2 else ""
        row_vals = [sh.cell_value(r, c) for c in range(sh.ncols)]
        row_str = " ".join(str(v) for v in row_vals)
        if "ICO NO" in row_str:
            ico = row_str.split("ICO NO")[1].strip(" .:") if "ICO NO" in row_str else row_str
        if "Customer" in c0:
            customer = c2
        if "File / P.O" in c0 or "File / P.O" in str(sh.cell_value(r, 1)):
            po = c2
        if "Qty." in c0:
            try:
                qty = float(c2.replace("BAGS", "").strip())
            except ValueError:
                qty = c2
        if c0 == "Type" or ("Type" in c0 and "BAG" in c2.upper()):
            bag_type = c2
        if "Size" in c0 and "CM" in row_str:
            size = tuple(row_vals[4:7]) if len(row_vals) > 6 else None
        if c0 == "SWL":
            swl = c2
        if "Fabric colour" in c0:
            fabric_color = c2
        headings = {
            "Body", "BODY", "Top", "F/S", "Fs", "Bottom", "D/s", "SIDE", "Side",
            "F/SKIRT", "Petal Flap", "Petal Rope", "Loop", "Liner", "LINER", "Tie", "TIE",
        }
        if c0 in headings:
            gsm = row_vals[1]
            lami = str(row_vals[2])
            fab_size = row_vals[3]
            cut = row_vals[4]
            total_mtr = row_vals[5] if len(row_vals) > 5 else ""
            total_kg = row_vals[6] if len(row_vals) > 6 else ""
            remarks = row_vals[7] if len(row_vals) > 7 else ""
            fabric_lines.append(
                dict(
                    heading=c0,
                    gsm=gsm,
                    type=lami,
                    fabric_size=fab_size,
                    cut=cut,
                    total_mtr=total_mtr,
                    total_kg=total_kg,
                    remarks=remarks,
                )
            )
    print(f"ICO: {ico}")
    print(f"PO: {po}")
    print(f"Customer: {customer}")
    print(f"Qty: {qty} bags")
    print(f"Type: {bag_type}")
    print(f"Size (LxWxH cm): {size}")
    print(f"SWL: {swl}")
    print(f"Fabric colour: {fabric_color}")
    print()
    print("All BOM component lines:")
    loom_types = ("UL", "LAMI", "VENT", "LENO", "MW")
    loom_headings = {"Body", "BODY", "Top", "F/S", "Fs", "Bottom", "D/s", "SIDE", "Side", "F/SKIRT", "Petal Flap"}
    for ln in fabric_lines:
        is_loom = ln["heading"] in loom_headings and any(t in ln["type"].upper() for t in ("UL", "LAMI", "VENT", "LENO"))
        is_accessory = ln["heading"] in ("Loop", "Tie", "TIE", "Liner", "LINER", "Petal Rope")
        flag = "LOOM-FABRIC" if is_loom else ("ACCESSORY" if is_accessory else "OTHER")
        print(
            f"  {ln['heading']:12} GSM={str(ln['gsm']):10} {ln['type']:12} "
            f"fab={ln['fabric_size']} cut={ln['cut']} mtr={ln['total_mtr']} kg={ln['total_kg']} [{flag}]"
        )
        if ln["remarks"]:
            print(f"    remarks: {ln['remarks']}")
    # Per-line kg per bag * qty estimate if no meters
    if qty and isinstance(qty, float):
        print()
        print("Per-bag fabric kg (loom lines only):")
        loom_kg = 0
        for ln in fabric_lines:
            if ln["heading"] in loom_headings and isinstance(ln["total_kg"], (int, float)):
                loom_kg += ln["total_kg"]
                print(f"  {ln['heading']}: {ln['total_kg']} kg/bag")
        print(f"  Subtotal loom fabric kg/bag: {loom_kg:.4f}")
        print(f"  Order total loom kg (x {qty}): {loom_kg * qty:.1f}")
    print()


files = [
    ("HGL-14 (standard export)", r"c:\Users\Admin\Desktop\approval\PO_approvalapp\public\HGL-14-07092023.xls"),
    ("6110-JS ICO", r"c:\Users\Admin\Desktop\approval\PO_approvalapp\public\6110-JS-15028 ICO 2604-091.xls"),
    ("KPW-01 ventilated", r"c:\Users\Admin\Desktop\approval\PO_approvalapp\public\KPW-01-2022-03.xls"),
]
for n, f in files:
    analyze(f, n)
