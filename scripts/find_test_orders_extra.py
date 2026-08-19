import pymssql

SERVER = "103.240.33.122"
PORT = 5115
USER = "sa"
PASSWORD = "PlastOswal#@123$%^&*()iop"
COMPANY_LIKE = "%Unit -II%"

conn = pymssql.connect(server=SERVER, port=PORT, user=USER, password=PASSWORD, database="MaterialProcessing")
cur = conn.cursor(as_dict=True)


def section(title):
    print("\n" + "=" * 80)
    print(title)
    print("=" * 80)


def sp(*parts):
    print("  " + " | ".join(str(p) for p in parts))


section("A. Recent BOM orders (any qty) with dispatch")
cur.execute("""
SELECT TOP 15 b.FilePONo AS OrderNo, b.BagType, b.Qty,
       LEFT(b.Customer, 30) AS Customer,
       CAST(m.DespatchDate AS DATE) AS DispatchDate,
       ISNULL(f.PlannedQty, 0) AS Planned
FROM production.dbo.Vw_Bom_PPC b WITH (NOLOCK)
INNER JOIN Despatch.dbo.MarketingInvoice m WITH (NOLOCK) ON m.BuyerOrderNo = b.FilePONo
LEFT JOIN (
    SELECT orderno, SUM(qty) AS PlannedQty
    FROM prod_fibcallocationMaster WITH (NOLOCK)
    WHERE Companyname LIKE %s GROUP BY orderno
) f ON f.orderno = b.FilePONo
WHERE m.DespatchDate >= DATEADD(DAY, -30, GETDATE())
ORDER BY m.DespatchDate DESC
""", (COMPANY_LIKE,))
rows = cur.fetchall()
for r in rows:
    sp(r["OrderNo"], r["BagType"], f"Qty={r['Qty']}", f"Dispatch={r['DispatchDate']}", f"Planned={r['Planned']}")

section("B. BOM orders with zero FIBC plan (relaxed qty)")
cur.execute("""
SELECT TOP 15 b.FilePONo AS OrderNo, b.BagType, b.Qty,
       CAST(m.DespatchDate AS DATE) AS DispatchDate,
       LEFT(b.Customer, 30) AS Customer
FROM production.dbo.Vw_Bom_PPC b WITH (NOLOCK)
INNER JOIN Despatch.dbo.MarketingInvoice m WITH (NOLOCK) ON m.BuyerOrderNo = b.FilePONo
WHERE NOT EXISTS (
    SELECT 1 FROM prod_fibcallocationMaster f WITH (NOLOCK)
    WHERE f.orderno = b.FilePONo AND f.Companyname LIKE %s
)
  AND TRY_CAST(b.Qty AS FLOAT) > 100
  AND m.DespatchDate >= CAST(GETDATE() AS DATE)
ORDER BY m.DespatchDate
""", (COMPANY_LIKE,))
fresh = cur.fetchall()
for r in fresh:
    sp(r["OrderNo"], r["BagType"], f"Qty={r['Qty']}", f"Dispatch={r['DispatchDate']}", r["Customer"])

section("C. Fabric lines for top fresh orders")
for r in fresh[:5]:
    o = r["OrderNo"]
    cur.execute("""
    SELECT TOP 5 FilePONo, Heading, GSM, FabricSize, TotalMtr, BagType, Qty
    FROM production.dbo.Vw_Bom_PPC WITH (NOLOCK)
    WHERE FilePONo = %s ORDER BY Heading
    """, (o,))
    print(f"\n  Order {o}:")
    for f in cur.fetchall():
        sp(f.get("Heading", "")[:25], f"GSM={f.get('GSM')}", f"W={f.get('FabricSize')}", f"Mtr={f.get('TotalMtr')}")

section("D. All FIBC planned orders (Unit II)")
cur.execute("""
SELECT orderno, SUM(qty) Planned, MIN(CAST(sysdate AS DATE)) FirstD, MAX(CAST(sysdate AS DATE)) LastD, COUNT(*) Slots
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname LIKE %s
GROUP BY orderno ORDER BY MAX(sysdate) DESC
""", (COMPANY_LIKE,))
planned = cur.fetchall()
for r in planned:
    sp(r["orderno"], f"Plan={r['Planned']}", f"{r['FirstD']}->{r['LastD']}", f"{r['Slots']} slots")

section("E. FIBC slots for 9305/4LT-0775")
cur.execute("""
SELECT linenos, [shift], CAST(sysdate AS DATE) PlanDate, qty, PBagType, ALLOCATEDPER
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE orderno = '9305/4LT-0775' ORDER BY sysdate, linenos
""")
for r in cur.fetchall():
    sp(f"L{r['linenos']}", r["shift"], r["PlanDate"], r["qty"], r["PBagType"])

section("F. Loom allocations Unit II")
cur.execute("""
SELECT TOP 10 a.PONO, a.LoomNo, CAST(a.AllocationDate AS DATE) FromD, CAST(a.ToDate AS DATE) ToD,
       a.ReqGSM, a.asize
FROM Prod_LoomAlocationMaster a WITH (NOLOCK)
INNER JOIN NewMISLoomMaster m WITH (NOLOCK) ON m.LoomNo = a.LoomNo
WHERE m.CompanyName LIKE %s ORDER BY a.AllocationDate DESC
""", (COMPANY_LIKE,))
for r in cur.fetchall():
    sp(r["PONO"], f"Loom={r['LoomNo']}", f"{r['FromD']}->{r['ToD']}", f"GSM={r['ReqGSM']}", f"W={r['asize']}")

section("G. Lines + capacity")
cur.execute("SELECT LNo, BagType, Bagcapacity FROM NewLineMaster WHERE CompanyName LIKE %s ORDER BY LNo", (COMPANY_LIKE,))
for r in cur.fetchall():
    sp(f"Line {r['LNo']}", r["BagType"], f"cap={r['Bagcapacity']}")

section("H. Portal backlog/downtime rows")
for tbl in ["PlanningBacklog", "PlanningDowntime"]:
    try:
        cur.execute(f"SELECT TOP 5 * FROM {tbl}")
        print(f"\n  {tbl}:")
        for r in cur.fetchall():
            sp(r)
    except Exception as e:
        print(f"  {tbl}: {e}")

conn.close()
print("\nDone.")
