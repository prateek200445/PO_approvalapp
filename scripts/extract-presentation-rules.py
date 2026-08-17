#!/usr/bin/env python3
"""Extract company presentation rules from PIL reference Excel check sheet."""

from __future__ import annotations

import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XLSX = ROOT / "docs" / "accounting" / "PIL Provisional FS_March-26_14.08.26 - Copy.xlsx"
OUT_DIR = ROOT / "POApprovalAPI" / "Data" / "FinancialStatements" / "companies" / "plastene-india-limited"

NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


def read_wb(path: Path) -> dict[str, dict]:
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
        sheets: dict[str, dict] = {}
        for sh in wb.findall("m:sheets/m:sheet", NS):
            name = sh.get("name") or ""
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


def cell(rows: dict, r: int, c: int, default: str = "") -> str:
    return str(rows.get(r, {}).get(c, default)).strip()


def fnum(v: str) -> float | None:
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def main() -> int:
    if not XLSX.exists():
        print(f"Missing reference Excel: {XLSX}", file=sys.stderr)
        return 1

    sheets = read_wb(XLSX)
    check = sheets["check"]
    bs = sheets["BS"]
    pl = sheets["P&L"]

    rules: dict = {
        "companyKey": "plastene-india-limited",
        "companyName": "Plastene India Limited",
        "groupPresentationLakhs": {},
        "groupAdjustmentsLakhs": {},
        "cogsMovement": {
            "purchaseGroups": [
                "Add : Purchase of Raw Material",
                "Add : Purchase of Packing Material",
                "Add : Purchase Expenses",
            ],
            "openingRmPackingLakhs": 936.83,
            "closingRmPackingLakhs": 2657.35,
        },
        "loanMaturityLakhs": {},
        "tradePayables": {
            "msmeLakhs": 76.15,
            "creditorsOthersLakhs": 488.85,
            "billOfExchangeLakhs": 331.5,
        },
    }

    for r in range(1, max(check) + 1):
        grp = cell(check, r, 2)
        if not grp:
            continue
        raw = fnum(cell(check, r, 4))
        fs = fnum(cell(check, r, 5))
        adj = fnum(cell(check, r, 6))
        if adj is not None and abs(adj) > 0.01 and fs is not None:
            rules["groupAdjustmentsLakhs"][grp] = round(adj, 2)
            rules["groupPresentationLakhs"][grp] = round(abs(fs), 2)

    short_term = sheets.get("short term", {})
    lt_c: dict[str, float] = defaultdict(float)
    lt_nc: dict[str, float] = defaultdict(float)
    for r in range(15, 500):
        grp = cell(short_term, r, 9)
        if not grp:
            continue
        cur = fnum(cell(short_term, r, 7))
        nc = fnum(cell(short_term, r, 8))
        if cur is not None:
            lt_c[grp] += cur
        if nc is not None:
            lt_nc[grp] += nc
    for grp in set(lt_c) | set(lt_nc):
        rules["loanMaturityLakhs"][grp] = {
            "currentLakhs": round(-lt_c[grp] / 100_000, 2),
            "nonCurrentLakhs": round(-lt_nc[grp] / 100_000, 2),
        }

    line_overrides = {
        "tradeReceivablesLakhs": "Trade receivables",
        "inventoriesLakhs": "Inventories",
        "longTermBorrowingsLakhs": "Long term borrowings",
        "otherCurrentLiabilitiesLakhs": "Other current liabilities",
        "reservesAndSurplusLakhs": "Reserves and surplus",
        "propertyPlantEquipmentLakhs": "Property, plant & equipments",
        "otherNonCurrentAssetsLakhs": "Other non-current assets",
        "otherCurrentAssetsLakhs": "Other current assets",
        "longTermProvisionsLakhs": "Long-term provisions",
        "shortTermProvisionsLakhs": "Short-term provisions",
        "depreciationLakhs": "Depreciation and amortization expenses",
        "changesInInventoryLakhs": "Changes in inventories of finished goods, work-in-progr",
        "otherOperatingRevenueLakhs": "Other operating revenue",
        "financeCostsLakhs": "Finance costs",
        "otherExpensesLakhs": "Other expenses",
        "insuranceClaimEarlierYearLakhs": "Insurance claim of earlier year",
        "currentTaxLakhs": "Current tax",
        "deferredTaxLakhs": "Deferred tax",
    }

    for r in range(4, 60):
        label = cell(bs, r, 1)
        amt = fnum(cell(bs, r, 4))
        if not label or amt is None:
            continue
        for key, prefix in line_overrides.items():
            if label.startswith(prefix):
                rules[key] = round(amt, 2)

    for r in range(4, 45):
        label = cell(pl, r, 2)
        amt = fnum(cell(pl, r, 5))
        if not label or amt is None:
            continue
        for key, prefix in line_overrides.items():
            if label.startswith(prefix):
                rules[key] = round(amt, 2)

    rules["loansAndAdvancesNonCurrentLakhs"] = round(fnum(cell(bs, 35, 4)) or 0, 2)
    rules["loansAndAdvancesCurrentLakhs"] = round((fnum(cell(bs, 35, 4)) or 0) * 0 + (4591.80 - (fnum(cell(bs, 35, 4)) or 0)), 2)
    # Current/non-current loans split from BS note 13 lines
    for r in range(4, 60):
        if cell(bs, r, 1) == "Loans and advances":
            amt = fnum(cell(bs, r, 4))
            if amt is None:
                continue
            if r < 40:
                rules["loansAndAdvancesNonCurrentLakhs"] = round(amt, 2)
            else:
                rules["loansAndAdvancesCurrentLakhs"] = round(amt, 2)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    out_path = OUT_DIR / "presentation-rules.json"
    out_path.write_text(json.dumps(rules, indent=2), encoding="utf-8")
    print(f"Wrote {out_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
