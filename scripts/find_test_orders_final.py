import pymssql

SERVER = "103.240.33.122"
PORT = 5115
USER = "sa"
PASSWORD = "PlastOswal#@123$%^&*()iop"
COMPANY_LIKE = "%Unit -II%"

conn = pymssql.connect(server=SERVER, port=PORT, user=USER, password=PASSWORD, database="MaterialProcessing")
cur = conn.cursor(as_dict=True)

def section(t):
    print("\n" + "="*70 + "\n" + t + "\n" + "="*70)

def sp(*a):
    print(" ", " | ".join(str(x) for x in a))

candidates = [
    "FIBCO/PO/2026/0056/JBPL-01",
    "OEL 287/2110 NEUTRE",
    "PO 9305/4LT-0775",
]

section("1. BOM detail for candidate orders")
for o in candidates:
    cur.execute("""
    SELECT FilePONo, Heading, BagType, Qty, GSM, FabricSize, TotalMtr, Customer, Targetdate
    FROM production.dbo.Vw_Bom_PPC WHERE FilePONo = %s ORDER BY Heading
    """, (o,))
    rows = cur.fetchall()
    print(f"\n  Order: {o} ({len(rows)} BOM lines)")
    for r in rows[:6]:
        sp(r.get("Heading","")[:20], r["BagType"], f"Qty={r['Qty']}", f"GSM={r['GSM']}", f"W={r['FabricSize']}", f"Mtr={r['TotalMtr']}")

section("2. FIBC plan status for candidates")
for o in candidates:
    cur.execute("SELECT SUM(qty) p, COUNT(*) n FROM prod_fibcallocationMaster WHERE orderno=%s AND Companyname LIKE %s", (o, COMPANY_LIKE))
    r = cur.fetchone()
    sp(o, f"Planned={r['p'] or 0}", f"Slots={r['n']}")

section("3. Top unplanned BOM orders (qty 500-50000)")
cur.execute("""
SELECT TOP 20 FilePONo, MAX(BagType) BagType, MAX(Qty) Qty, MAX(Customer) Customer,
       MAX(GSM) GSM, MAX(FabricSize) W, MAX(TotalMtr) Mtr
FROM production.dbo.Vw_Bom_PPC b
WHERE TRY_CAST(Qty AS FLOAT) BETWEEN 500 AND 50000
  AND NOT EXISTS (SELECT 1 FROM prod_fibcallocationMaster f WHERE f.orderno = b.FilePONo AND f.Companyname LIKE %s)
GROUP BY FilePONo
ORDER BY MAX(Qty) DESC
""", (COMPANY_LIKE,))
fresh = cur.fetchall()
for r in fresh[:12]:
    sp(r["FilePONo"], r["BagType"], f"Qty={r['Qty']}", f"GSM={r['GSM']}", f"W={r['W']}", f"Mtr={r['Mtr']}", r["Customer"][:25] if r["Customer"] else "")

section("4. Loom allocations with real PONO")
cur.execute("""
SELECT TOP 15 a.PONO, a.LoomNo, CAST(a.AllocationDate AS DATE) FromD, CAST(a.ToDate AS DATE) ToD,
       a.ReqGSM, a.asize
FROM Prod_LoomAlocationMaster a
INNER JOIN NewMISLoomMaster m ON m.LoomNo = a.LoomNo
WHERE m.CompanyName LIKE %s AND a.PONO <> 'Self'
  AND LTRIM(RTRIM(a.PONO)) <> ''
ORDER BY a.AllocationDate DESC
""", (COMPANY_LIKE,))
for r in cur.fetchall(): sp(r)

section("5. Full detail: best fresh order for Loom+FIBC test")
if fresh:
    pick = fresh[0]
    o = pick["FilePONo"]
    cur.execute("SELECT FilePONo, Heading, BagType, Qty, GSM, FabricSize, TotalMtr, Customer FROM production.dbo.Vw_Bom_PPC WHERE FilePONo=%s", (o,))
    bom = cur.fetchall()
    cur.execute("SELECT TOP 1 BuyerName, DespatchDate, MarketingInvNo FROM Despatch.dbo.MarketingInvoice WHERE BuyerOrderNo=%s", (o,))
    inv = cur.fetchone()
    print(f"\n  >>> TEST A — Fresh order: {o}")
    print(f"      Customer:    {bom[0]['Customer'] if bom else pick.get('Customer')}")
    print(f"      Bag type:    {pick['BagType']}")
    print(f"      Qty:         {pick['Qty']}")
    print(f"      Dispatch:    {inv['DespatchDate'] if inv else 'N/A (use today+30)'}")
    for i, f in enumerate(bom[:3]):
        print(f"      Fabric {i+1}: GSM={f['GSM']}, Width={f['FabricSize']} cm, Meters={f['TotalMtr']}, Heading={f.get('Heading','')[:30]}")

section("6. Execution test — PO 9305/4LT-0775")
o = "PO 9305/4LT-0775"
cur.execute("SELECT SUM(qty) FROM prod_fibcallocationMaster WHERE orderno=%s", (o,))
planned = cur.fetchone()[0]
cur.execute("SELECT SUM(BagPCS) FROM FIBCTeamWiseProduction WHERE PONO=%s", (o,))
produced = (cur.fetchone()[0] or 0)
cur.execute("SELECT SUM(BailPcs) FROM FIBCBailingEntry WHERE MarketingOrdNo=%s", (o,))
bailed = (cur.fetchone()[0] or 0)
cur.execute("SELECT linenos, shift, CAST(sysdate AS DATE) d, qty, PBagType FROM prod_fibcallocationMaster WHERE orderno=%s ORDER BY sysdate, shift", (o,))
slots = cur.fetchall()
print(f"\n  >>> TEST B — Execution/Timeline (already planned + fully produced)")
print(f"      Order:       {o}")
print(f"      Party:       SO Bag")
print(f"      Bag type:    UPanel")
print(f"      Planned:     {planned} pcs")
print(f"      Produced:    {produced} pcs")
print(f"      Bailed:      {bailed} pcs")
print(f"      Slots:")
for s in slots:
    print(f"        Line {s['linenos']} Shift {s['shift']} {s['d']} — {s['qty']} pcs")

section("7. Line master (Setup reference)")
cur.execute("SELECT LNo, BagType, Bagcapacity FROM NewLineMaster WHERE CompanyName LIKE %s ORDER BY LNo", (COMPANY_LIKE,))
for r in cur.fetchall(): sp(f"Line {r['LNo']}", r["BagType"], f"cap={r['Bagcapacity']}/shift")

section("8. Loom pool sample (Unit II)")
cur.execute("""
SELECT TOP 12 LoomNo, LoomType, CompanyName
FROM NewMISLoomMaster WHERE CompanyName LIKE %s AND LoomNo IS NOT NULL ORDER BY LoomNo
""", (COMPANY_LIKE,))
for r in cur.fetchall(): sp(r)

section("9. Capacity factors sample")
cur.execute("""
SELECT TOP 8 LNo, [shift], BagType, capacity, CompanyNam
FROM CapacityPlanning WHERE CompanyNam LIKE %s ORDER BY LNo, [shift]
""", (COMPANY_LIKE,))
for r in cur.fetchall(): sp(r)

conn.close()
print("\nDone.")
