#!/usr/bin/env python3
"""
Seed three isolated planning demo orders in ERP (production DB).

Creates BOM header + lines by cloning real template orders, then verifies Vw_Bom_PPC.

Usage:
  python scripts/seed_planning_demo_orders.py
  python scripts/seed_planning_demo_orders.py --verify-only
  python scripts/seed_planning_demo_orders.py --clean-plans   # also remove loom/FIBC saves

DB password (first match): DB_PASSWORD env → scripts/.db_password →
  POApprovalAPI/Properties/launchSettings.json (same as dotnet run)

Demo orders (Script 6):
  DEMO-U2-STD-LINER-001    <- HGL-14-07092023   (circular + liner, same unit)
  DEMO-U2-ICO-SULZER-001   <- 6110/JS-15028     (ICO / Sulzer inter-unit)
  DEMO-U2-VENT-NOLINER-001 <- KPW/01/2022/03    (ventilated U-panel, same unit)
  DEMO-U2-CRITICAL-001     <- HGL-14-07092023   (circular — Script 6D critical shift demo)
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from dataclasses import dataclass
from datetime import datetime
from typing import Any

try:
    import pymssql
except ImportError:
    print("Install: pip install pymssql", file=sys.stderr)
    sys.exit(1)

SERVER = os.environ.get("DB_SERVER", "103.240.33.122")
PORT = int(os.environ.get("DB_PORT", "5115"))
USER = os.environ.get("DB_USER", "sa")
COMPANY = "Plastene India Limited (Unit -II)"

DEMO_ORDERS = (
    "DEMO-U2-STD-LINER-001",
    "DEMO-U2-ICO-SULZER-001",
    "DEMO-U2-VENT-NOLINER-001",
    "DEMO-U2-CRITICAL-001",
)


@dataclass(frozen=True)
class DemoSpec:
    demo_order: str
    template_order: str
    customer: str
    bag_type: str | None  # None = keep template
    fab_color: str | None
    qty: str
    ref_no: str


SPECS: list[DemoSpec] = [
    DemoSpec(
        demo_order="DEMO-U2-STD-LINER-001",
        template_order="HGL-14-07092023",
        customer="DEMO — Hidden Gold LLC (Circular + Liner)",
        bag_type="Circular/Non-Builder/Std/Type A",
        fab_color="MILKY WHITE",
        qty="7200",
        ref_no="2309-119",
    ),
    DemoSpec(
        demo_order="DEMO-U2-ICO-SULZER-001",
        template_order="6110/JS-15028",
        customer="DEMO — Jumbo Sack Corp (ICO Sulzer)",
        bag_type="UPanel/Sulzer /Std/Type A",
        fab_color="MILKYWHITE/ SULZER FABRIC",
        qty="1225",
        ref_no="2604-091",
    ),
    DemoSpec(
        demo_order="DEMO-U2-VENT-NOLINER-001",
        template_order="KPW/01/2022/03",
        customer="DEMO — Indralok Domestic (Ventilated)",
        bag_type="UPanel/Ventilated/Std/Type A",
        fab_color="VENTILATED — MILKY WHITE",  # no SULZER → same-unit demo (Case C)
        qty="2000",
        ref_no="2201-165",
    ),
    DemoSpec(
        demo_order="DEMO-U2-CRITICAL-001",
        template_order="HGL-14-07092023",
        customer="DEMO — Critical Rush LLC (Circular bump)",
        bag_type="Circular/Non-Builder/Std/Type A",
        fab_color="MILKY WHITE",
        qty="3600",
        ref_no="2608-CRIT",
    ),
]

SYS_DATE = datetime(2026, 8, 15)


def _password_from_launch_settings() -> str:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    path = os.path.join(root, "POApprovalAPI", "Properties", "launchSettings.json")
    if not os.path.isfile(path):
        return ""
    try:
        data = json.load(open(path, encoding="utf-8"))
        profiles = data.get("profiles") or {}
        for profile in profiles.values():
            env = profile.get("environmentVariables") or {}
            pw = (env.get("DB_PASSWORD") or "").strip()
            if pw:
                return pw
    except (OSError, json.JSONDecodeError, TypeError, AttributeError):
        pass
    return ""


def resolve_password() -> str:
    pw = os.environ.get("DB_PASSWORD", "").strip()
    if pw:
        return pw
    pw_file = os.path.join(os.path.dirname(__file__), ".db_password")
    if os.path.isfile(pw_file):
        return open(pw_file, encoding="utf-8").read().strip()
    pw = _password_from_launch_settings()
    if pw:
        return pw
    print(
        "Set DB_PASSWORD (env), scripts/.db_password, or POApprovalAPI/Properties/launchSettings.json.",
        file=sys.stderr,
    )
    sys.exit(1)


def connect(db: str):
    return pymssql.connect(
        server=SERVER,
        port=PORT,
        user=USER,
        password=resolve_password(),
        database=db,
    )


def delete_demo_bom(cur, order: str) -> None:
    cur.execute("DELETE FROM BOM WHERE PONo = %s", (order,))
    cur.execute("DELETE FROM BOM1 WHERE FilePONo = %s", (order,))


def copy_bom1(cur, spec: DemoSpec) -> None:
    cur.execute(
        """
        SELECT * FROM BOM1
        WHERE FilePONo = %s AND ISNULL(SrNo, '') <> 'temp'
        ORDER BY SysDate DESC
        """,
        (spec.template_order,),
    )
    row = cur.fetchone()
    if not row:
        raise RuntimeError(f"Template BOM1 not found: {spec.template_order}")

    row = dict(row)
    row["FilePONo"] = spec.demo_order
    row["Customer"] = spec.customer
    row["Qty"] = spec.qty
    row["SysDate"] = SYS_DATE
    row["refNo"] = spec.ref_no
    row["UserName"] = row.get("UserName") or "DEMO_SEED"
    if spec.bag_type:
        row["BagType"] = spec.bag_type
    if spec.fab_color:
        row["FabColor"] = spec.fab_color

    cols = list(row.keys())
    placeholders = ", ".join(["%s"] * len(cols))
    col_list = ", ".join(cols)
    cur.execute(
        f"INSERT INTO BOM1 ({col_list}) VALUES ({placeholders})",
        [row[c] for c in cols],
    )


def copy_bom_lines(cur, template: str, demo: str, customer: str) -> int:
    cur.execute("SELECT * FROM BOM WHERE PONo = %s ORDER BY TransId", (template,))
    rows = cur.fetchall()
    if not rows:
        raise RuntimeError(f"Template BOM lines not found: {template}")

    count = 0
    for row in rows:
        row = dict(row)
        row.pop("TransId", None)
        row["PONo"] = demo
        row["PartyName"] = customer
        cols = list(row.keys())
        placeholders = ", ".join(["%s"] * len(cols))
        col_list = ", ".join(cols)
        cur.execute(
            f"INSERT INTO BOM ({col_list}) VALUES ({placeholders})",
            [row[c] for c in cols],
        )
        count += 1
    return count


def clean_plans(mp_cur, orders: tuple[str, ...]) -> None:
    for o in orders:
        mp_cur.execute(
            """
            DELETE FROM prod_fibcallocationMaster
            WHERE orderno = %s AND Companyname = %s
            """,
            (o, COMPANY),
        )
        mp_cur.execute(
            """
            DELETE FROM Prod_LoomAlocationMaster
            WHERE PONO = %s
            """,
            (o,),
        )


def verify(prod_cur, spec: DemoSpec) -> dict[str, Any]:
    prod_cur.execute(
        """
        SELECT TOP 1 Customer, BagType, Qty, Targetdate
        FROM Vw_Bom_PPC WHERE FilePONo = %s
        """,
        (spec.demo_order,),
    )
    header = prod_cur.fetchone()
    prod_cur.execute(
        """
        SELECT Heading, GSM, FabricSize, TotalMtr
        FROM Vw_Bom_PPC
        WHERE FilePONo = %s AND Heading LIKE %s
        ORDER BY Heading
        """,
        (spec.demo_order, "%Body%"),
    )
    body = prod_cur.fetchone()
    return {"header": header, "body": body}


def seed_one(prod_conn, spec: DemoSpec) -> None:
    cur = prod_conn.cursor(as_dict=True)
    delete_demo_bom(cur, spec.demo_order)
    copy_bom1(cur, spec)
    n = copy_bom_lines(cur, spec.template_order, spec.demo_order, spec.customer)
    prod_conn.commit()
    print(f"  {spec.demo_order}: BOM1 + {n} BOM lines (from {spec.template_order})")


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed planning demo BOM orders in ERP")
    parser.add_argument("--verify-only", action="store_true")
    parser.add_argument("--clean-plans", action="store_true", help="Remove loom/FIBC saves for demo orders")
    args = parser.parse_args()

    print(f"=== Planning demo BOM seed ({SERVER}:{PORT}) ===\n")

    if args.verify_only:
        with connect("production") as prod:
            cur = prod.cursor(as_dict=True)
            for spec in SPECS:
                v = verify(cur, spec)
                h, b = v["header"], v["body"]
                if not h:
                    print(f"MISSING {spec.demo_order}")
                else:
                    print(
                        f"OK {spec.demo_order} | {h['BagType']} | qty={h['Qty']} | "
                        f"target={h['Targetdate']} | body GSM={b and b['GSM']} "
                        f"W={b and b['FabricSize']} mtr={b and b['TotalMtr']}"
                    )
        return

    with connect("production") as prod, connect("MaterialProcessing") as mp:
        if args.clean_plans:
            mp_cur = mp.cursor()
            clean_plans(mp_cur, DEMO_ORDERS)
            mp.commit()
            print("Cleaned loom/FIBC plans for demo orders.\n")

        print("Seeding BOM...")
        for spec in SPECS:
            seed_one(prod, spec)

        print("\nVerify Vw_Bom_PPC:")
        cur = prod.cursor(as_dict=True)
        for spec in SPECS:
            v = verify(cur, spec)
            h, b = v["header"], v["body"]
            if not h or not b:
                print(f"  FAIL {spec.demo_order}")
                sys.exit(1)
            print(
                f"  OK {spec.demo_order} | body GSM={b['GSM']} W={b['FabricSize']} "
                f"mtr={b['TotalMtr']} | bag={h['BagType']}"
            )

    print("\nDone. Use these orders in Script 6 (planning-demo-inputs-unit2.md).")


if __name__ == "__main__":
    main()
