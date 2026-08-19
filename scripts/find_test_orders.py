import pymssql
from datetime import date, timedelta

SERVER = "103.240.33.122"
PORT = 5115
USER = "sa"
PASSWORD = "PlastOswal#@123$%^&*()iop"
COMPANY = "Plastene India Limited (Unit -II)"
COMPANY_LIKE = "%Unit -II%"

conn = pymssql.connect(server=SERVER, port=PORT, user=USER, password=PASSWORD, database="MaterialProcessing")
cur = conn.cursor(as_dict=True)


def section(title: str) -> None:
    print("\n" + "=" * 80)
    print(title)
    print("=" * 80)


def safe_print(*parts):
    print("  " + " | ".join(str(p) for p in parts))


section("1. FRESH ORDERS — BOM + future dispatch, zero FIBC plan")
cur.execute(
    """
SELECT TOP 12
    b.FilePONo AS OrderNo,
    b.BagType,
    b.Qty,
    LEFT(b.Customer, 35) AS Customer,
    CAST(m.DespatchDate AS DATE) AS DispatchDate,
    m.MarketingInvNo,
    ISNULL(f.PlannedQty, 0) AS AlreadyPlanned
FROM production.dbo.Vw_Bom_PPC b WITH (NOLOCK)
INNER JOIN Despatch.dbo.MarketingInvoice m WITH (NOLOCK)
    ON m.BuyerOrderNo = b.FilePONo
LEFT JOIN (
    SELECT orderno, SUM(qty) AS PlannedQty
    FROM prod_fibcallocationMaster WITH (NOLOCK)
    WHERE Companyname LIKE %s
    GROUP BY orderno
) f ON f.orderno = b.FilePONo
WHERE TRY_CAST(b.Qty AS FLOAT) BETWEEN 500 AND 50000
  AND m.DespatchDate >= CAST(GETDATE() AS DATE)
  AND m.DespatchDate <= DATEADD(DAY, 90, GETDATE())
  AND ISNULL(f.PlannedQty, 0) = 0
ORDER BY m.DespatchDate
""",
    (COMPANY_LIKE,),
)
fresh = []
for r in cur.fetchall():
    fresh.append(r)
    safe_print(r["OrderNo"], r["BagType"], f"Qty={r['Qty']}", f"Dispatch={r['DispatchDate']}", r["Customer"])

section("2. FABRIC LINES (GSM, width, meters) — fresh orders")
fabric_by_order = {}
if fresh:
    orders = [r["OrderNo"] for r in fresh[:6]]
    ph = ",".join(["%s"] * len(orders))
    cur.execute(
        f"""
        SELECT FilePONo AS OrderNo, Heading, BagType, Qty, GSM, FabricSize, TotalMtr, Targetdate, Customer
        FROM production.dbo.Vw_Bom_PPC WITH (NOLOCK)
        WHERE FilePONo IN ({ph})
        ORDER BY FilePONo, Heading
        """,
        tuple(orders),
    )
    for r in cur.fetchall():
        o = r["OrderNo"]
        fabric_by_order.setdefault(o, []).append(r)
        safe_print(
            o,
            r.get("Heading", "")[:20],
            f"GSM={r.get('GSM')}",
            f"Width={r.get('FabricSize')}",
            f"Mtr={r.get('TotalMtr')}",
        )

section("3. ORDERS WITH EXISTING FIBC PLAN")
cur.execute(
    """
SELECT TOP 10
    orderno AS OrderNo,
    SUM(qty) AS PlannedQty,
    MIN(CAST(sysdate AS DATE)) AS FirstSlot,
    MAX(CAST(sysdate AS DATE)) AS LastSlot,
    COUNT(*) AS SlotRows,
    MAX(partyname) AS Party
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname LIKE %s
GROUP BY orderno
ORDER BY MAX(CAST(sysdate AS DATE)) DESC
""",
    (COMPANY_LIKE,),
)
planned_orders = []
for r in cur.fetchall():
    planned_orders.append(r)
    safe_print(r["OrderNo"], f"Planned={r['PlannedQty']}", f"{r['FirstSlot']}->{r['LastSlot']}", f"{r['SlotRows']} slots")

section("4. PRODUCTION + BAILING vs PLAN")
if planned_orders:
    tops = [r["OrderNo"] for r in planned_orders[:8]]
    ph = ",".join(["%s"] * len(tops))
    cur.execute(
        f"""
        SELECT p.orderno AS OrderNo, p.PlannedQty,
               ISNULL(pr.ProducedQty, 0) AS ProducedQty,
               ISNULL(b.BailedQty, 0) AS BailedQty
        FROM (
            SELECT orderno, SUM(qty) AS PlannedQty
            FROM prod_fibcallocationMaster WITH (NOLOCK)
            WHERE orderno IN ({ph}) GROUP BY orderno
        ) p
        LEFT JOIN (
            SELECT PONO AS orderno, SUM(BagPCS) AS ProducedQty
            FROM FIBCTeamWiseProduction WITH (NOLOCK)
            WHERE PONO IN ({ph}) AND CompanyName LIKE %s GROUP BY PONO
        ) pr ON pr.orderno = p.orderno
        LEFT JOIN (
            SELECT MarketingOrdNo AS orderno, SUM(BailPcs) AS BailedQty
            FROM FIBCBailingEntry WITH (NOLOCK)
            WHERE MarketingOrdNo IN ({ph}) GROUP BY MarketingOrdNo
        ) b ON b.orderno = p.orderno
        ORDER BY ISNULL(pr.ProducedQty, 0) DESC
        """,
        tuple(tops + tops + [COMPANY_LIKE] + tops),
    )
    exec_rows = cur.fetchall()
    for r in exec_rows:
        safe_print(r["OrderNo"], f"Plan={r['PlannedQty']}", f"Prod={r['ProducedQty']}", f"Bail={r['BailedQty']}")

section("5. SAMPLE FIBC SLOTS — most recent planned order")
if planned_orders:
    o = planned_orders[0]["OrderNo"]
    cur.execute(
        """
        SELECT TOP 6 linenos AS FibcLine, [shift] AS FibcShift, CAST(sysdate AS DATE) AS PlanDate,
               qty, ALLOCATEDPER, PBagType
        FROM prod_fibcallocationMaster WITH (NOLOCK)
        WHERE orderno = %s ORDER BY sysdate, linenos
        """,
        (o,),
    )
    print(f"  Order: {o}")
    for r in cur.fetchall():
        safe_print(f"L{r['FibcLine']}", r["FibcShift"], r["PlanDate"], f"{r['qty']} pcs", r["PBagType"])

section("6. LOOM PLANS (recent)")
cur.execute(
    """
SELECT TOP 8 a.PONO AS OrderNo, a.LoomNo,
       CAST(a.AllocationDate AS DATE) AS FromDate,
       CAST(a.ToDate AS DATE) AS ToDate,
       a.ReqGSM, a.asize AS Width
FROM Prod_LoomAlocationMaster a WITH (NOLOCK)
INNER JOIN NewMISLoomMaster m WITH (NOLOCK) ON m.LoomNo = a.LoomNo
WHERE m.CompanyName LIKE %s
ORDER BY a.AllocationDate DESC
""",
    (COMPANY_LIKE,),
)
loom_rows = cur.fetchall()
for r in loom_rows:
    safe_print(r["OrderNo"], f"Loom {r['LoomNo']}", f"{r['FromDate']}->{r['ToDate']}", f"GSM={r['ReqGSM']}", f"W={r['Width']}")

section("7. LINE MASTER + SHIFTS")
cur.execute(
    "SELECT LNo, BagType, Bagcapacity FROM NewLineMaster WITH (NOLOCK) WHERE CompanyName LIKE %s ORDER BY LNo",
    (COMPANY_LIKE,),
)
for r in cur.fetchall():
    safe_print(f"Line {r['LNo']}", r["BagType"], f"cap={r['Bagcapacity']}")

cur.execute(
    "SELECT DISTINCT [shift] AS Shift FROM CapacityPlanning WITH (NOLOCK) WHERE CompanyNam LIKE %s",
    (COMPANY_LIKE,),
)
shifts = [r["Shift"] for r in cur.fetchall()]
print("  Shifts in ERP:", ", ".join(shifts))

section("8. COPY-PASTE UI TEST CARD")
print(f"  Company (fixed): {COMPANY}")

if fresh:
    pick = fresh[0]
    o = pick["OrderNo"]
    cur.execute(
        "SELECT TOP 1 DespatchDate, BuyerName, MarketingInvNo FROM Despatch.dbo.MarketingInvoice WHERE BuyerOrderNo=%s ORDER BY DespatchDate DESC",
        (o,),
    )
    mkt = cur.fetchone() or {}
    fabrics = fabric_by_order.get(o, [])
    fab = fabrics[0] if fabrics else {}
    dispatch = mkt.get("DespatchDate") or pick["DispatchDate"]
    if hasattr(dispatch, "date"):
        dispatch_d = dispatch.date() if hasattr(dispatch, "date") else dispatch
    else:
        dispatch_d = dispatch
    try:
        fabric_req = dispatch_d - timedelta(days=12)
    except Exception:
        fabric_req = "dispatch minus 12 days"

    gsm = fab.get("GSM") or "170"
    width = fab.get("FabricSize") or "100"
    meters = fab.get("TotalMtr") or "5000"

    print("\n  --- TEST A: Full workflow (Loom + FIBC) — fresh order ---")
    print(f"  Order No:              {o}")
    print(f"  Customer:              {mkt.get('BuyerName') or pick.get('Customer')}")
    print(f"  Bag type:              {pick['BagType']}")
    print(f"  Quantity (FIBC):       {pick['Qty']}")
    print(f"  Dispatch date:         {dispatch_d}")
    print(f"  Allotment mode:        OrderWise")
    print(f"  Dust level:            Normal")
    print(f"  --- Loom panel ---")
    print(f"  Req GSM:               {gsm}")
    print(f"  Size (width cm):       {width}")
    print(f"  Required meters:       {meters}")
    print(f"  Fabric requirement date: {fabric_req}")

if len(fresh) > 1:
    pick2 = fresh[1]
    print("\n  --- TEST B: Second fresh order (slot-wise compare) ---")
    print(f"  Order No:              {pick2['OrderNo']}")
    print(f"  Bag type:              {pick2['BagType']}")
    print(f"  Quantity:              {pick2['Qty']}")
    print(f"  Dispatch:              {pick2['DispatchDate']}")
    print(f"  Allotment mode:        SlotWise  (compare with Test A)")

if planned_orders:
    for r in planned_orders:
        if r["PlannedQty"] and r["PlannedQty"] > 0:
            pick3 = r["OrderNo"]
            break
    else:
        pick3 = planned_orders[0]["OrderNo"]
    cur.execute("SELECT SUM(qty) p FROM prod_fibcallocationMaster WHERE orderno=%s", (pick3,))
    planned = (cur.fetchone() or {}).get("p", 0)
    cur.execute(
        "SELECT SUM(BagPCS) p FROM FIBCTeamWiseProduction WHERE PONO=%s AND CompanyName LIKE %s",
        (pick3, COMPANY_LIKE),
    )
    produced = (cur.fetchone() or {}).get("p") or 0
    print("\n  --- TEST C: Execution + Timeline (already planned) ---")
    print(f"  Order No:              {pick3}")
    print(f"  Planned pcs:           {planned}")
    print(f"  Produced pcs:          {produced}")

    # find one with production near plan
    for r in exec_rows if planned_orders else []:
        if r["ProducedQty"] and r["PlannedQty"] and r["ProducedQty"] >= r["PlannedQty"] * 0.9:
            print("\n  --- TEST D: Auto-clear backlog candidate ---")
            print(f"  Order No:              {r['OrderNo']}")
            print(f"  Planned:               {r['PlannedQty']}")
            print(f"  Produced:              {r['ProducedQty']}")
            print("  Setup backlog first on a line, then load Execution to see auto-clear")
            break

if loom_rows:
    lr = loom_rows[0]
    print("\n  --- TEST E: Loom re-plan reference ---")
    print(f"  Order No:              {lr['OrderNo']}")
    print(f"  Loom No:               {lr['LoomNo']}")
    print(f"  Req GSM:               {lr['ReqGSM']}")
    print(f"  Width:                 {lr['Width']}")

print("\n  --- SETUP tab test values ---")
print("  Backlog:  Line=3, Shift=A, Order=<Test A order>, Qty=500")
print("  Downtime: tomorrow, Line=3, Shift=A, Factor=0.5, Reason=Power cut test")

conn.close()
print("\nDone.")
