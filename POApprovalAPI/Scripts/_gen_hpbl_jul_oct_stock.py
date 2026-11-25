"""July HPBL stock, August HPBL 1/2/Vadodara, October HPBL 1 valuation.

HPBL 3 July/August/October stay as the September copy already loaded.
"""
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp")
OUT = ROOT / "POApprovalAPI" / "Scripts" / "LoadHpblJulAugOctStock.sql"
LACS = 100_000.0

HPBL1 = "HCP Plastene Bulkpack Ltd"
HPBL2 = "HCP Plastene Bulkpack Ltd (Unit - II)"
VAD = "HCP Plastene Bulkpack ltd (Vadodara)"

CAT = {
    "store": "Stores",
    "stores": "Stores",
    "rm": "RawMaterial",
    "sf": "WIP",
    "fg": "FinishedGoods",
}
UNITS = {2: HPBL1, 3: HPBL2, 5: VAD}  # column 4 is HPBL 3; leave the September copy


def sql_n(name: str) -> str:
    return "N'" + name.replace("'", "''") + "'"


def n(v):
    if isinstance(v, (int, float)):
        return float(v)
    return None


rows = []


def add_sheet(path, month, remark, skip_hpbl3=True):
    wb = load_workbook(ROOT / path, data_only=True)
    ws = wb.active
    for r in range(1, (ws.max_row or 1) + 1):
        lab = ws.cell(r, 2).value if path.endswith("Valuation.xlsx") else ws.cell(r, 1).value
        # valuation labels are in column B; summary labels are in column A
        if path.endswith("Valuation.xlsx"):
            lab = ws.cell(r, 2).value
            amt = n(ws.cell(r, 3).value)
            cat = CAT.get(str(lab or "").strip().lower())
            if cat and amt and abs(amt) >= 1:
                rows.append((HPBL1, month, cat, amt / LACS, remark))
            continue
        cat = CAT.get(str(lab or "").strip().lower())
        if not cat:
            continue
        for col, company in UNITS.items():
            amt = n(ws.cell(r, col).value)
            if amt is None or abs(amt) < 1:
                continue
            rows.append((company, month, cat, amt / LACS, remark))
    wb.close()


add_sheet("HPBL Stock.xlsx", "2026-07-01", "HPBL Stock.xlsx July")
add_sheet("HPBL Stock aug.xlsx", "2026-08-01", "HPBL Stock aug.xlsx August")
add_sheet("Valuation.xlsx", "2026-10-01", "Valuation.xlsx October HPBL 1")

lines = [
    "/* July stock for HPBL 1, HPBL 2 and Vadodara.",
    "   August stock for those three. October stock for HPBL 1 from Valuation.xlsx.",
    "   HPBL 3 July, August and October stay equal to September. No carry-forward. */",
    "SET NOCOUNT ON;",
    "SET XACT_ABORT ON;",
    "BEGIN TRAN;",
    "",
]
for company, month, cat, lacs, remark in rows:
    lines.append(
        "IF EXISTS (SELECT 1 FROM dbo.PnlStockValue "
        f"WHERE CompanyName = {sql_n(company)} AND StockMonth = '{month}' "
        f"AND Category = {sql_n(cat)} AND PlantName IS NULL)"
    )
    lines.append("    UPDATE dbo.PnlStockValue")
    lines.append(
        f"    SET AmountLacs = {lacs:.6f}, UploadedAt = GETDATE(), Remarks = {sql_n(remark)}"
    )
    lines.append(
        f"    WHERE CompanyName = {sql_n(company)} AND StockMonth = '{month}' "
        f"AND Category = {sql_n(cat)} AND PlantName IS NULL;"
    )
    lines.append("ELSE")
    lines.append(
        "    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)"
    )
    lines.append(
        f"    VALUES ({sql_n(company)}, NULL, '{month}', {sql_n(cat)}, {lacs:.6f}, {sql_n(remark)}, GETDATE());"
    )
    lines.append("")
    print(f"{month} {company[:28]:28} {cat:14} {lacs:10.4f}")

lines.append("COMMIT;")
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("rows", len(rows), "wrote", OUT)
