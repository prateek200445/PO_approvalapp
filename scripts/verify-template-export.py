#!/usr/bin/env python3
"""Verify template export matches reference BS/P&L current-year amounts."""

from __future__ import annotations

import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REF = ROOT / "docs" / "accounting" / "PIL Provisional FS_March-26_14.08.26 - Copy.xlsx"
EXP = ROOT / "docs" / "accounting" / "logs" / "fs-exported-template-test.xlsx"
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


def read_wb(path: Path) -> dict:
    with zipfile.ZipFile(path) as z:
        names = set(z.namelist())
        shared: list[str] = []
        if "xl/sharedStrings.xml" in names:
            root = ET.fromstring(z.read("xl/sharedStrings.xml"))
            for si in root.findall("m:si", NS):
                texts = [
                    t.text or ""
                    for t in si.iter("{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t")
                ]
                shared.append("".join(texts))

        wb = ET.fromstring(z.read("xl/workbook.xml"))
        rels = ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
        rel_map = {rel.get("Id"): rel.get("Target") for rel in rels}
        sheets: dict = {}
        for sh in wb.findall("m:sheets/m:sheet", NS):
            name = sh.get("name")
            rid = sh.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
            target = rel_map.get(rid, "")
            if not target.startswith("xl/"):
                target = "xl/" + target.lstrip("/")
            if target not in names:
                base = target.split("/")[-1]
                for n in names:
                    if n.endswith("/" + base):
                        target = n
                        break
            root = ET.fromstring(z.read(target))
            rows: dict = {}
            for row_el in root.findall(".//m:sheetData/m:row", NS):
                rnum = int(row_el.get("r"))
                for cell in row_el.findall("m:c", NS):
                    ref = cell.get("r")
                    m = re.match(r"([A-Z]+)(\d+)", ref or "")
                    if not m:
                        continue
                    col = m.group(1)
                    t = cell.get("t")
                    v_el = cell.find("m:v", NS)
                    n = 0
                    for c in col:
                        n = n * 26 + (ord(c) - 64)
                    if v_el is None:
                        val = ""
                    elif t == "s":
                        val = shared[int(v_el.text)] if v_el.text else ""
                    else:
                        val = v_el.text or ""
                    rows.setdefault(rnum, {})[n] = val
            sheets[name] = rows
        return sheets


def fnum(v):
    try:
        return round(float(v), 2)
    except (TypeError, ValueError):
        return None


def compare_sheet(ref_rows, exp_rows, col, rows, sheet_name):
    mismatches = []
    for r in rows:
        rv = fnum(ref_rows.get(r, {}).get(col))
        ev = fnum(exp_rows.get(r, {}).get(col))
        if rv != ev:
            mismatches.append((r, rv, ev))
    return mismatches


def main() -> int:
    ref_path = Path(sys.argv[1]) if len(sys.argv) > 1 else REF
    exp_path = Path(sys.argv[2]) if len(sys.argv) > 2 else EXP

    ref_wb = read_wb(ref_path)
    exp_wb = read_wb(exp_path)

    print(f"Reference sheets: {len(ref_wb)}  Exported sheets: {len(exp_wb)}")

    bs_rows = [
        7, 8, 9, 12, 13, 14, 15, 16, 19, 21, 22, 23, 24, 25, 27,
        31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 49,
    ]
    pl_rows = [
        6, 7, 8, 9, 11, 12, 15, 16, 17, 18, 19, 20, 21, 23,
        26, 29, 30, 33, 34, 35, 36, 38,
    ]

    bs_mm = compare_sheet(ref_wb["BS"], exp_wb["BS"], 4, bs_rows, "BS")
    pl_mm = compare_sheet(ref_wb["P&L"], exp_wb["P&L"], 5, pl_rows, "P&L")

    print(f"BS mismatches: {len(bs_mm)}")
    for r, rv, ev in bs_mm:
        print(f"  BS row {r}: ref={rv} exported={ev}")

    print(f"P&L mismatches: {len(pl_mm)}")
    for r, rv, ev in pl_mm:
        print(f"  P&L row {r}: ref={rv} exported={ev}")

    return 0 if not bs_mm and not pl_mm else 1


if __name__ == "__main__":
    raise SystemExit(main())
