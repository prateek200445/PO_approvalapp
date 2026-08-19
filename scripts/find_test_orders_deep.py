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

section("1. Sample MarketingInvoice (recent)")
cur.execute("SELECT TOP 10 BuyerOrderNo, BuyerName, CAST(DespatchDate AS DATE) D, MarketingInvNo FROM Despatch.dbo.MarketingInvoice ORDER BY DespatchDate DESC")
for r in cur.fetchall(): sp(r)

section("2. Sample Vw_Bom_PPC")
cur.execute("SELECT TOP 10 FilePONo, BagType, Qty, Customer, GSM, FabricSize, TotalMtr FROM production.dbo.Vw_Bom_PPC ORDER BY FilePONo DESC")
for r in cur.fetchall(): sp(r)

section("3. Join test - do BOM and Invoice match?")
cur.execute("""
SELECT TOP 10 b.FilePONo, m.BuyerOrderNo, b.BagType, b.Qty, CAST(m.DespatchDate AS DATE) D
FROM production.dbo.Vw_Bom_PPC b
INNER JOIN Despatch.dbo.MarketingInvoice m ON m.BuyerOrderNo = b.FilePONo
ORDER BY m.DespatchDate DESC
""")
for r in cur.fetchall(): sp(r)

section("4. FIBC planned order details + party")
cur.execute("""
SELECT TOP 1 orderno, partyname, PBagType, SUM(qty) q, MIN(sysdate) d1, MAX(sysdate) d2
FROM prod_fibcallocationMaster WHERE Companyname LIKE %s GROUP BY orderno, partyname, PBagType ORDER BY MAX(sysdate) DESC
""", (COMPANY_LIKE,))
r = cur.fetchone()
sp("Summary:", r)
if r:
    ono = r["orderno"]
    cur.execute("SELECT linenos, shift, CAST(sysdate AS DATE) d, qty, ALLOCATEDPER FROM prod_fibcallocationMaster WHERE orderno=%s", (ono,))
    for s in cur.fetchall(): sp(" slot:", s)

section("5. Loom orders with real PONO (not Self)")
cur.execute("""
SELECT TOP 15 a.PONO, a.LoomNo, CAST(a.AllocationDate AS DATE) FromD, CAST(a.ToDate AS DATE) ToD,
       a.ReqGSM, a.asize, a.ReqMtr
FROM Prod_LoomAlocationMaster a
INNER JOIN NewMISLoomMaster m ON m.LoomNo = a.LoomNo
WHERE m.CompanyName LIKE %s AND a.PONO <> 'Self' AND a.PONO IS NOT NULL AND LTRIM(RTRIM(a.PONO)) <> ''
ORDER BY a.AllocationDate DESC
""", (COMPANY_LIKE,))
loom_real = cur.fetchall()
for r in loom_real: sp(r)

section("6. BOM for loom PONOs")
if loom_real:
    orders = list({r["PONO"] for r in loom_real[:8]})
    ph = ",".join(["%s"]*len(orders))
    cur.execute(f"""
    SELECT FilePONo, BagType, Qty, GSM, FabricSize, TotalMtr, Customer
    FROM production.dbo.Vw_Bom_PPC WHERE FilePONo IN ({ph})
    """, tuple(orders))
    for r in cur.fetchall(): sp(r)

section("7. Unplanned BOM orders (no join to invoice)")
cur.execute("""
SELECT TOP 15 b.FilePONo, b.BagType, b.Qty, b.GSM, b.FabricSize, b.TotalMtr, LEFT(b.Customer,25) Cust
FROM production.dbo.Vw_Bom_PPC b
WHERE NOT EXISTS (SELECT 1 FROM prod_fibcallocationMaster f WHERE f.orderno = b.FilePONo AND f.Companyname LIKE %s)
  AND TRY_CAST(b.Qty AS FLOAT) BETWEEN 500 AND 50000
ORDER BY b.FilePONo DESC
""", (COMPANY_LIKE,))
unplanned = cur.fetchall()
for r in unplanned: sp(r)

section("8. Invoice for unplanned BOM orders")
for r in unplanned[:5]:
    o = r["FilePONo"]
    cur.execute("SELECT TOP 3 BuyerOrderNo, BuyerName, CAST(DespatchDate AS DATE) D FROM Despatch.dbo.MarketingInvoice WHERE BuyerOrderNo LIKE %s ORDER BY DespatchDate DESC", ('%' + o.split('/')[-1] + '%',))
    inv = cur.fetchall()
    print(f"\n  BOM {o}:")
    for i in inv: sp(i)
    if not inv:
        cur.execute("SELECT TOP 3 BuyerOrderNo, BuyerName, CAST(DespatchDate AS DATE) D FROM Despatch.dbo.MarketingInvoice WHERE BuyerOrderNo = %s", (o,))
        for i in cur.fetchall(): sp(" exact:", i)

section("9. Companies in FIBC allocation")
cur.execute("SELECT DISTINCT Companyname, COUNT(*) c FROM prod_fibcallocationMaster GROUP BY Companyname ORDER BY c DESC")
for r in cur.fetchall(): sp(r)

section("10. Production for PO 9305/4LT-0775")
cur.execute("SELECT PONO, SUM(BagPCS) p, MIN(CAST(ProdDate AS DATE)) d1, MAX(CAST(ProdDate AS DATE)) d2 FROM FIBCTeamWiseProduction WHERE PONO LIKE '%0775%' GROUP BY PONO")
for r in cur.fetchall(): sp(r)

conn.close()
print("\nDone.")
