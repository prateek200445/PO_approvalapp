#!/usr/bin/env python3
"""Compare fs-generated-latest.json against reference Excel BS/P&L."""

from __future__ import annotations

import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XLSX = ROOT / "docs" / "accounting" / "PIL Provisional FS_March-26_14.08.26 - Copy.xlsx"
LOG = ROOT / "docs" / "accounting" / "logs" / "fs-generated-latest.json"
OUT = ROOT / "docs" / "accounting" / "logs" / "fs-comparison-report.txt"

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


def cell(rows, r, c, default=""):
    return rows.get(r, {}).get(c, default)


def fnum(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def extract_excel_reference(path: Path) -> tuple[dict, dict]:
    sheets = read_wb(path)
    bs, pl = sheets["BS"], sheets["P&L"]
    excel_bs = {}
    for r in range(4, 60):
        label = str(cell(bs, r, 1)).strip()
        amt = fnum(cell(bs, r, 4))
        if label and amt is not None and label != "Total":
            excel_bs[label] = round(amt, 2)

    skip = ("Summary", "The accompanying", "As per", "Sanjay", "Chartered", "Membership", "UDIN", "Place", "Date", "Mr.")
    excel_pl = {}
    for r in range(4, 45):
        label = str(cell(pl, r, 2)).strip()
        amt = fnum(cell(pl, r, 5))
        if label and amt is not None and not label.startswith(skip):
            excel_pl[label] = round(amt, 2)
    return excel_bs, excel_pl


def load_generated(log_path: Path) -> dict:
    data = json.loads(log_path.read_text(encoding="utf-8"))
    summary = data.get("summary") or {}

    def aggregate_lines(lines: list[dict]) -> dict[str, float]:
        totals: dict[str, float] = {}
        for line in lines:
            label = line["label"]
            amount = round(line["amountLakhs"], 2)
            totals[label] = round(totals.get(label, 0) + amount, 2)
        return totals

    return {
        "meta": {
            "generatedAtUtc": data.get("generatedAtUtc"),
            "sourceFileName": data.get("sourceFileName"),
            "company": (data.get("request") or {}).get("companyName"),
            "period": (data.get("request") or {}).get("periodLabel"),
        },
        "summary": summary,
        "bs": aggregate_lines(summary.get("balanceSheetLines", [])),
        "pl": aggregate_lines(summary.get("profitAndLossLines", [])),
        "schedules": summary.get("schedules", []),
    }


def normalize_label(label: str) -> str:
    return " ".join(label.split()).lower()


def find_generated_amount(generated: dict, label: str) -> float:
    if label in generated:
        return generated[label]
    norm = normalize_label(label)
    for key, value in generated.items():
        if normalize_label(key).startswith(norm) or norm.startswith(normalize_label(key)):
            return value
    return 0.0


def compare_section(title: str, excel: dict, generated: dict, lines: list[str]) -> None:
    lines.append(f"\n{'=' * 80}")
    lines.append(title)
    lines.append(f"{'Label':<55} {'Excel':>12} {'Generated':>12} {'Diff':>12}")
    lines.append("-" * 80)
    for label in sorted(set(excel) | set(generated)):
        if label not in excel:
            continue
        e = excel[label]
        g = find_generated_amount(generated, label)
        diff = round(g - e, 2)
        flag = " ***" if abs(diff) > 1 else ""
        lines.append(f"{label[:55]:<55} {e:12.2f} {g:12.2f} {diff:+12.2f}{flag}")


def main() -> int:
    if not LOG.exists():
        print(f"Missing generated log: {LOG}", file=sys.stderr)
        print("Run financial statement generation in Development mode first.", file=sys.stderr)
        return 1
    if not XLSX.exists():
        print(f"Missing reference Excel: {XLSX}", file=sys.stderr)
        return 1

    excel_bs, excel_pl = extract_excel_reference(XLSX)
    gen = load_generated(LOG)

    lines: list[str] = []
    lines.append("FINANCIAL STATEMENT COMPARISON REPORT")
    lines.append(f"Reference Excel: {XLSX.name}")
    lines.append(f"Generated log:   {LOG.name}")
    lines.append(f"Company:         {gen['meta'].get('company')}")
    lines.append(f"Period:          {gen['meta'].get('period')}")
    lines.append(f"Generated at:    {gen['meta'].get('generatedAtUtc')}")

    s = gen["summary"]
    lines.append("\nSUMMARY")
    lines.append(f"  Ledgers mapped: {s.get('mappedLedgers')} / {s.get('totalLedgers')} (unmapped {s.get('unmappedLedgers')})")
    lines.append(f"  Total assets (Lakhs):              {s.get('totalAssetsLakhs')}")
    lines.append(f"  Total liabilities + equity:      {s.get('totalLiabilitiesAndEquityLakhs')}")
    lines.append(f"  Balance diff (Assets - L&E):     {s.get('balanceSheetDiffLakhs')}")

    compare_section("BALANCE SHEET — Excel vs Generated (Lakhs)", excel_bs, gen["bs"], lines)
    compare_section("PROFIT & LOSS — Excel vs Generated (Lakhs)", excel_pl, gen["pl"], lines)

    lines.append(f"\n{'=' * 80}")
    lines.append("SCHEDULE NOTE TOTALS (Generated only)")
    for note in gen["schedules"]:
        lines.append(f"  Note {note['note']:>2}  {note['totalLakhs']:12.2f}  {note['title'][:45]}")

    report = "\n".join(lines)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(report, encoding="utf-8")
    print(report)
    print(f"\nReport saved to: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
