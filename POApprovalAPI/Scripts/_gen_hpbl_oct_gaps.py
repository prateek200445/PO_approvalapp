"""HPBL October gaps: HPBL-4 provisions, HPBL-3 stock copy, HPBL-1/2 Book321."""
from collections import defaultdict
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(r"c:\Users\Admin\Desktop\approval\PO_approvalapp")
OUT = ROOT / "POApprovalAPI" / "Scripts" / "LoadHpblOctGaps.sql"

HPBL1 = "HCP Plastene Bulkpack Ltd"
HPBL2 = "HCP Plastene Bulkpack Ltd (Unit - II)"
HPBL3 = "HCP Plastene Bulkpack Ltd (Unit - III)"
HPBL4 = "HCP Plastene Bulkpack Ltd (Unit - IV)"

LEDGER_FIX = {
    "jobwork charges": "Job Charges",
    "conveyance expense": "Conveyance Expenses",
}


def n(v):
    if isinstance(v, (int, float)):
        return float(v)
    return None


def fix(name: str) -> str:
    raw = " ".join(str(name).split())
    return LEDGER_FIX.get(raw.lower().rstrip(" ."), raw)


def entry_type(ledger: str) -> str:
    return "Income" if "income" in ledger.lower() else "Expense"


def sql_n(name: str) -> str:
    return "N'" + name.replace("'", "''") + "'"


lines = []
a = lines.append
a("/* HPBL gaps.")
a("   HPBL-4 October provisions from Check Report OCT-2026, sheet Provision and Prepaid Exp.")
a("   Labour is the contractor total. The 0.17 electric cell is omitted.")
a("   HPBL-3 July, August, and October stock copied from September.")
a("   HPBL-1 and HPBL-2 October provisions from Book321 unit columns. */")
a("SET NOCOUNT ON;")
a("SET XACT_ABORT ON;")
a("BEGIN TRAN;")
a("")

for mo in ("2026-07-01", "2026-08-01", "2026-10-01"):
    a("INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)")
    a(f"SELECT {sql_n(HPBL3)}, NULL, '{mo}', p.Category, p.AmountLacs, N'same as September stock', GETDATE()")
    a("FROM dbo.PnlStockValue p")
    a(f"WHERE p.CompanyName = {sql_n(HPBL3)} AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL")
    a(
        "  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c "
        f"WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '{mo}' "
        "AND c.Category = p.Category AND c.PlantName IS NULL);"
    )
    a("")

wb = load_workbook(ROOT / "oct 26" / "hpbl 4" / "Check Report OCT-2026.xlsx", data_only=True)
ws = wb["Provision & Prepaid Exp"]
labour = n(ws.cell(15, 2).value)
if labour is None or labour < 1_000_000:
    raise SystemExit(f"unexpected labour total {labour}")
items = [("Labour Charges", labour)]
for r in range(38, (ws.max_row or 38) + 1):
    lab = ws.cell(r, 1).value
    amt = n(ws.cell(r, 2).value)
    if not lab or amt is None:
        continue
    lab = fix(str(lab))
    if lab.lower() in ("total", "total amount"):
        continue
    if abs(amt) < 1:
        print("skip tiny", lab, amt)
        continue
    items.append((lab, amt))
wb.close()
print("HPBL4", len(items), round(sum(v for _, v in items), 2))

prov = [
    (HPBL4, "2026-10-01", led, amt, "Check Report OCT-2026 Provision and Prepaid Exp")
    for led, amt in items
]

wb = load_workbook(ROOT / "Book321.xlsx", data_only=True)
ws = wb["Sheet1"]
units = {3: HPBL1, 4: HPBL2}
for r in range(2, (ws.max_row or 2) + 1):
    lab = ws.cell(r, 1).value
    if not lab:
        continue
    led = fix(str(lab))
    if led.lower() == "total":
        continue
    for col, company in units.items():
        amt = n(ws.cell(r, col).value)
        if amt is None or abs(amt) < 1:
            continue
        prov.append((company, "2026-10-01", led, amt, "Book321 October unit split"))
wb.close()

by = defaultdict(list)
for row in prov:
    by[(row[0], row[1])].append(row)

for (company, month), rows in by.items():
    print(company, month, len(rows), round(sum(x[3] for x in rows), 2))
    a(
        "DELETE FROM dbo.Provisioning "
        f"WHERE LTRIM(RTRIM(companyname)) = {sql_n(company)} "
        f"AND sysdate >= '{month}' AND sysdate < DATEADD(month, 1, '{month}');"
    )
    for _, _, led, amt, rem in rows:
        a(
            "INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES ("
            f"{sql_n(company)}, '{month}', {sql_n(led)}, {amt:.4f}, {sql_n(rem)}, {sql_n(entry_type(led))});"
        )
    a("")

a("COMMIT;")
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", OUT, "lines", len(lines))
