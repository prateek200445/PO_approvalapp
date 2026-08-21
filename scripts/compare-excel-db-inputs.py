#!/usr/bin/env python3
"""Compare PIL Excel FS inputs vs MaterialProcessing DB SPs/views."""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
import zipfile
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XLSX = ROOT / "docs" / "accounting" / "PIL Provisional FS_March-26_14.08.26 - Copy.xlsx"
OUT = ROOT / "docs" / "accounting" / "logs" / "excel-db-input-comparison.txt"

NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
COMPANY = "Plastene India Limited"
DATE_FROM = "2025-04-01"
DATE_TO = "2026-03-31"
TOLERANCE_LAKHS = 5.0
TOLERANCE_PCT = 0.02


def read_wb(path: Path) -> dict[str, dict[int, dict[int, str]]]:
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


def fnum(v, default=None):
    if v is None or v == "":
        return default
    try:
        return float(v)
    except (TypeError, ValueError):
        return default


def col_letter(n: int) -> str:
    s = ""
    while n:
        n, r = divmod(n - 1, 26)
        s = chr(65 + r) + s
    return s


def find_header_row(rows: dict, aliases: list[str], max_row=20) -> int | None:
    alias_set = {a.lower() for a in aliases}
    for r in range(1, max_row + 1):
        row_vals = [str(cell(rows, r, c)).strip().lower() for c in range(1, 20)]
        hits = sum(1 for v in row_vals if v in alias_set or any(a in v for a in alias_set))
        if hits >= 2:
            return r
    return None


def parse_tb_sheet(sheets: dict) -> dict:
    rows = sheets.get("TB Mar-26", {})
    header = find_header_row(
        rows,
        ["particulars", "ledger", "opening", "debit", "credit", "closing", "group"],
        max_row=15,
    )
    if not header:
        return {"error": "Could not detect TB header row"}

    headers = {}
    for c in range(1, 20):
        h = str(cell(rows, header, c)).strip()
        if h:
            headers[c] = h.lower()

    def find_col(*parts):
        for c, h in headers.items():
            if all(p in h for p in parts):
                return c
        return None

    particulars_col = find_col("particular") or find_col("ledger") or 2
    closing_cols = [c for c, h in headers.items() if h.startswith("closing")]
    closing_col = closing_cols[-1] if closing_cols else find_col("closing") or 7
    group_col = find_col("group") or None

    ledgers = []
    for r in range(header + 1, max(rows.keys(), default=header) + 1):
        ledger = str(cell(rows, r, particulars_col)).strip()
        if not ledger or ledger.lower() in ("total", "totals", "grand total"):
            continue
        closing = fnum(cell(rows, r, closing_col), 0.0)
        grp = str(cell(rows, r, group_col)).strip() if group_col else ""
        if grp == "0":
            grp = ""
        ledgers.append({"ledger": ledger, "closing": closing, "group": grp})

    return {
        "header_row": header,
        "closing_col": closing_col,
        "closing_header": headers.get(closing_col, col_letter(closing_col)),
        "ledger_count": len(ledgers),
        "total_closing_rupees": round(sum(x["closing"] for x in ledgers), 2),
        "ledgers": ledgers,
    }


def parse_check_sheet(sheets: dict) -> dict[str, dict]:
    rows = sheets.get("check", {})
    groups: dict[str, dict] = {}
    for r in range(1, max(rows.keys(), default=1) + 1):
        group = str(cell(rows, r, 2)).strip()
        if not group:
            continue
        raw = fnum(cell(rows, r, 4))
        fs = fnum(cell(rows, r, 5))
        adj = fnum(cell(rows, r, 6))
        if fs is None and raw is None:
            continue
        groups[group] = {
            "raw_lakhs": raw,
            "fs_lakhs": fs,
            "adjustment_lakhs": adj,
        }
    return groups


def parse_group_sheet(sheets: dict) -> dict[str, str]:
    rows = sheets.get("group", {})
    mapping = {}
    for r in range(2, max(rows.keys(), default=2) + 1):
        ledger = str(cell(rows, r, 1)).strip()
        group = str(cell(rows, r, 2)).strip()
        if ledger and group and group != "0":
            mapping[ledger] = group
    return mapping


def parse_working_totals(sheets: dict) -> dict[str, float | None]:
    out: dict[str, float | None] = {}

    # 7 to 10 trade payables (col 6, lakhs implied by magnitude in workbook)
    rows710 = sheets.get("7 to 10", {})
    for r in range(1, max(rows710.keys(), default=1) + 1):
        label = str(cell(rows710, r, 1)).strip()
        amt = fnum(cell(rows710, r, 6))
        if amt is None:
            continue
        if label.startswith("Total outstanding dues of micro and small enterprise"):
            out["trade_payables_msme"] = round(abs(amt), 2)
        elif label.startswith("Total outstanding dues of creditors other than micro"):
            out["trade_payables_others"] = round(abs(amt), 2)
        elif label.startswith("Bill of Exchange"):
            out["trade_payables_boe"] = round(abs(amt), 2)
        elif "Other current liabilities" in label and amt > 0:
            out["other_current_liabilities"] = round(abs(amt), 2)

    # 18 to 24 P&L working totals (col 3)
    rows1824 = sheets.get("18 to 24", {})
    section = None
    for r in range(1, max(rows1824.keys(), default=1) + 1):
        label = str(cell(rows1824, r, 1)).strip()
        sub = str(cell(rows1824, r, 2)).strip()
        amt = fnum(cell(rows1824, r, 3))
        if "Other operating revenue" in label:
            section = "other_operating_revenue"
        elif "Revenue from operations" in label:
            section = "revenue"
        elif "19. Other income" in label:
            section = "other_income"
        elif "20. Cost of materials" in label or "Cost of materials consumed" in label:
            section = "cogs"
        elif "22. Changes in inventories" in label:
            section = "changes_in_inventory"
        elif "23. Employees" in label:
            section = "employee_benefits"
        elif "24. Finance costs" in label:
            section = "finance_costs"

        if amt is None:
            continue
        is_total = sub.strip() in ("-", "—", "") and label and not label[0].isdigit()
        if section == "revenue" and ("Total" in label or label.endswith("Total")):
            out["revenue_total"] = round(abs(amt), 2)
        if section == "other_income" and abs(amt) > 100:
            out["other_income_total"] = round(abs(amt), 2)
        if section == "cogs" and abs(amt) > 1000:
            out["cogs_total"] = round(abs(amt), 2)
        if section == "changes_in_inventory" and abs(amt) < 5000:
            out["changes_in_inventory"] = round(amt, 2)
        if section == "employee_benefits" and abs(amt) > 1000:
            out["employee_benefits"] = round(abs(amt), 2)
        if section == "finance_costs" and abs(amt) > 100:
            out["finance_costs"] = round(abs(amt), 2)

    # 25 to 28 other expenses total
    rows2528 = sheets.get("25 to 28", {})
    for r in range(1, max(rows2528.keys(), default=1) + 1):
        label = str(cell(rows2528, r, 1)).strip()
        amt = fnum(cell(rows2528, r, 3))
        if amt and "Total" in label and "other expenses" in label.lower():
            out["other_expenses_total"] = round(abs(amt), 2)

    # IT WORKING depreciation
    for sheet_name in ("IT WORKING", "IT "):
        rows_it = sheets.get(sheet_name, {})
        if not rows_it:
            continue
        for r in range(1, max(rows_it.keys(), default=1) + 1):
            label = str(cell(rows_it, r, 1)).strip().lower()
            amt = fnum(cell(rows_it, r, 3)) or fnum(cell(rows_it, r, 4))
            if amt and "depreciation" in label and "total" in label:
                out["depreciation_total"] = round(abs(amt), 2)
                break

    return out


def aggregate_tb_by_group(ledgers: list[dict], mapping: dict[str, str]) -> dict[str, float]:
    totals: dict[str, float] = defaultdict(float)
    unmapped = 0
    for row in ledgers:
        group = row["group"] or mapping.get(row["ledger"], "")
        if not group:
            unmapped += 1
            group = "(unmapped)"
        totals[group] += row["closing"]
    return dict(totals), unmapped


def rupees_to_lakhs(v: float) -> float:
    return round(v / 100_000.0, 2)


def compare_amount(label: str, excel_val, db_val, unit: str, lines: list[str]) -> str:
    if excel_val is None and db_val is None:
        lines.append(f"  {label}: both missing")
        return "missing"
    if excel_val is None:
        lines.append(f"  {label}: Excel missing, DB={db_val} {unit}")
        return "excel_missing"
    if db_val is None:
        lines.append(f"  {label}: Excel={excel_val} {unit}, DB missing")
        return "db_missing"
    diff = round(float(db_val) - float(excel_val), 2)
    pct = abs(diff / excel_val * 100) if excel_val else 0
    flag = "MATCH" if abs(diff) <= TOLERANCE_LAKHS or pct <= TOLERANCE_PCT * 100 else "MISMATCH"
    lines.append(
        f"  {label}: Excel={excel_val} {unit} | DB={db_val} {unit} | Diff={diff:+} | {flag}"
    )
    return flag


def run_db_queries() -> dict:
    pwd = os.environ.get("DB_PASSWORD")
    if not pwd:
        launch = ROOT / "POApprovalAPI" / "Properties" / "launchSettings.json"
        if launch.exists():
            text = launch.read_text(encoding="utf-8")
            m = re.search(r'"DB_PASSWORD"\s*:\s*"([^"]+)"', text)
            if m:
                pwd = m.group(1)
    if not pwd:
        return {"error": "DB_PASSWORD not set"}

    cs = (
        "Server=103.240.33.122,5115;Database=MaterialProcessing;User Id=sa;"
        f"Password={pwd};TrustServerCertificate=True;Connection Timeout=60;"
    )

    csharp = r'''
using Microsoft.Data.SqlClient;
using System.Text.Json;

var cs = Environment.GetEnvironmentVariable("DB_CS")!;
var company = Environment.GetEnvironmentVariable("DB_COMPANY")!;
var dateFrom = DateTime.Parse(Environment.GetEnvironmentVariable("DB_FROM")!);
var dateTo = DateTime.Parse(Environment.GetEnvironmentVariable("DB_TO")!);

var result = new Dictionary<string, object?>();

await using var conn = new SqlConnection(cs);
await conn.OpenAsync();

static async Task<List<Dictionary<string, object?>>> Query(SqlConnection c, string sql, params (string, object?)[] ps)
{
    await using var cmd = new SqlCommand(sql, c) { CommandTimeout = 180 };
    foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
    var rows = new List<Dictionary<string, object?>>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < r.FieldCount; i++)
            row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
        rows.Add(row);
    }
    return rows;
}

static decimal? Dec(object? v) => v == null ? null : Convert.ToDecimal(v);

// Sales EBIDTA FY
var sales = await Query(conn, @"
SELECT ROUND(SUM(Amount),2) AS SalesRupees
FROM vw_Sales_EBIDTA WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%'
  AND invdate >= @df AND invdate < DATEADD(day,1,@dt)
  AND ISNULL(InterGroup,'') <> 'Intergroup'", ("@co", company), ("@df", dateFrom), ("@dt", dateTo));
result["sales_rupees"] = sales.FirstOrDefault()?["SalesRupees"];

// Ledger grouping count
var grpCount = await Query(conn, @"
SELECT COUNT(*) AS Cnt FROM vw_ledgergrouping WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%'", ("@co", company));
result["ledgergrouping_rows"] = grpCount.FirstOrDefault()?["Cnt"];

// Sample ledger grouping
result["ledgergrouping_sample"] = await Query(conn, @"
SELECT TOP 5 ledgername, expensehead, expensegrouphead, b, CompanyName
FROM vw_ledgergrouping WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%' ORDER BY ledgername", ("@co", company));

// LedgerMaster TB snapshot (opening balance field - ERP snapshot, not period TB)
var lmAgg = await Query(conn, @"
SELECT Under, ROUND(SUM(Openingbalance),2) AS TotalOpening
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%'
GROUP BY Under
ORDER BY ABS(SUM(Openingbalance)) DESC", ("@co", company));
result["ledger_master_by_under"] = lmAgg.Take(40).ToList();

// Try trial balance SP schedule6
List<Dictionary<string, object?>>? tbRows = null;
string? tbError = null;
try
{
    tbRows = await Query(conn, @"
EXEC Sp_agstref_TrialBal_schedule6
  @DateFrom=@df, @DateTo=@dt, @companyname=@co, @Name='', @Factor='Group'", ("@df", dateFrom), ("@dt", dateTo), ("@co", company));
}
catch (Exception ex) { tbError = ex.Message; }
result["tb_sp_error"] = tbError;
result["tb_sp_rows"] = tbRows?.Take(5).ToList();
result["tb_sp_row_count"] = tbRows?.Count;

// vw_trialbalance aggregate by expensegrouphead (may need login context)
var tbView = await Query(conn, @"
SELECT TOP 30 expensegrouphead,
       ROUND(SUM(ISNULL(ClosingBalanceDebit,0)-ISNULL(ClosingBalanceCredit,0)),2) AS NetClosing
FROM vw_trialbalance WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%'
GROUP BY expensegrouphead
ORDER BY ABS(SUM(ISNULL(ClosingBalanceDebit,0)-ISNULL(ClosingBalanceCredit,0))) DESC", ("@co", company));
result["tb_view_by_expensegroup"] = tbView;

// MSME creditors outstanding (bill-wise proxy)
var msme = await Query(conn, @"
SELECT ROUND(SUM(ABS(b.Amount)),2) AS TotalRupees, COUNT(DISTINCT b.LedgerName) AS Parties
FROM vw_BillWiseTransaction b WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK) ON b.CompanyName=lm.CompanyName AND b.LedgerName=lm.LedgerName
INNER JOIN Vendor v WITH (NOLOCK) ON LTRIM(RTRIM(v.FirmName)) = LTRIM(RTRIM(lm.LedgerName))
WHERE b.CompanyName LIKE @co + '%' AND lm.Under LIKE 'Creditors%' AND v.ISMSME='yes'", ("@co", company));
result["msme_billwise_rupees"] = msme.FirstOrDefault()?["TotalRupees"];

// Provision ledgers from LedgerMaster opening
var prov = await Query(conn, @"
SELECT LedgerName, Under, ROUND(Openingbalance,2) AS Opening
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName LIKE @co + '%' AND Under LIKE '%Provision%'
ORDER BY ABS(Openingbalance) DESC", ("@co", company));
result["provision_ledgers"] = prov.Take(15).ToList();

// Depreciation view
var depCnt = await Query(conn, "SELECT COUNT(*) AS Cnt FROM vw_Depreciation WITH (NOLOCK)");
result["depreciation_view_rows"] = depCnt.FirstOrDefault()?["Cnt"];

Console.WriteLine(JsonSerializer.Serialize(result));
'''

    with tempfile.TemporaryDirectory() as td:
        td_path = Path(td)
        (td_path / "Compare.csproj").write_text(
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=\"Microsoft.Data.SqlClient\" Version=\"7.0.1\" />
  </ItemGroup>
</Project>""",
            encoding="utf-8",
        )
        (td_path / "Program.cs").write_text(csharp, encoding="utf-8")
        env = os.environ.copy()
        env["DB_CS"] = cs
        env["DB_COMPANY"] = COMPANY
        env["DB_FROM"] = DATE_FROM
        env["DB_TO"] = DATE_TO
        proc = subprocess.run(
            ["dotnet", "run", "--project", str(td_path / "Compare.csproj")],
            capture_output=True,
            text=True,
            env=env,
            timeout=300,
        )
        if proc.returncode != 0:
            return {"error": proc.stderr or proc.stdout}
        # last line should be JSON
        for line in reversed(proc.stdout.splitlines()):
            line = line.strip()
            if line.startswith("{"):
                return json.loads(line)
        return {"error": "No JSON output", "stdout": proc.stdout[-2000:]}


def main() -> int:
    if not XLSX.exists():
        print(f"Missing Excel: {XLSX}", file=sys.stderr)
        return 1

    sheets = read_wb(XLSX)
    tb = parse_tb_sheet(sheets)
    check = parse_check_sheet(sheets)
    mapping = parse_group_sheet(sheets)
    working = parse_working_totals(sheets)

    tb_by_group_rupees, unmapped = aggregate_tb_by_group(tb.get("ledgers", []), mapping)

    lines: list[str] = []
    lines.append("PIL EXCEL vs DATABASE INPUT COMPARISON")
    lines.append(f"Excel: {XLSX.name}")
    lines.append(f"Company: {COMPANY}")
    lines.append(f"Period: {DATE_FROM} to {DATE_TO}")
    lines.append("")

    lines.append("=" * 80)
    lines.append("1. TRIAL BALANCE (TB Mar-26 sheet)")
    lines.append(f"   Header row: {tb.get('header_row')} | Closing column: {tb.get('closing_header')} (col {tb.get('closing_col')})")
    lines.append(f"   Ledger rows: {tb.get('ledger_count')} | Unmapped to group: {unmapped}")
    lines.append(f"   Sum of closing (rupees): {tb.get('total_closing_rupees'):,.2f}")
    lines.append(f"   Sum of closing (lakhs): {rupees_to_lakhs(tb.get('total_closing_rupees', 0)):,.2f}")
    lines.append(f"   Group mappings in 'group' tab: {len(mapping)}")
    lines.append("")

    lines.append("=" * 80)
    lines.append("2. CHECK TAB — Raw TB vs FS presentation by group (lakhs)")
    lines.append(f"{'Group':<45} {'Raw':>12} {'FS':>12} {'Adjust':>12}")
    lines.append("-" * 80)
    mismatched_groups = 0
    for group, vals in sorted(check.items(), key=lambda x: abs(x[1].get("fs_lakhs") or 0), reverse=True):
        raw = vals.get("raw_lakhs")
        fs = vals.get("fs_lakhs")
        adj = vals.get("adjustment_lakhs")
        if fs is None and raw is None:
            continue
        tb_lakhs = rupees_to_lakhs(tb_by_group_rupees.get(group, 0)) if group in tb_by_group_rupees else None
        flag = ""
        if tb_lakhs is not None and raw is not None and abs(abs(tb_lakhs) - abs(raw or 0)) > TOLERANCE_LAKHS:
            flag = " *TB!=raw*"
            mismatched_groups += 1
        lines.append(
            f"{group[:45]:<45} {raw or 0:12.2f} {fs or 0:12.2f} {adj or 0:12.2f}{flag}"
        )
    lines.append(f"\nGroups where TB-derived raw != check raw: {mismatched_groups}")
    lines.append("")

    lines.append("=" * 80)
    lines.append("3. WORKING TAB TOTALS (Excel inputs used by FS engine)")
    for k, v in sorted(working.items()):
        lines.append(f"   {k}: {v}")
    lines.append("")

    lines.append("=" * 80)
    lines.append("4. DATABASE QUERIES")
    db = run_db_queries()
    if "error" in db:
        lines.append(f"   DB ERROR: {db['error']}")
    else:
        lines.append(f"   vw_Sales_EBIDTA FY sales (rupees): {db.get('sales_rupees')}")
        lines.append(f"   vw_Sales_EBIDTA FY sales (lakhs): {rupees_to_lakhs(float(db.get('sales_rupees') or 0)):,.2f}")
        lines.append(f"   vw_ledgergrouping rows for PIL units: {db.get('ledgergrouping_rows')}")
        lines.append(f"   Sp_agstref_TrialBal_schedule6 rows: {db.get('tb_sp_row_count')} error={db.get('tb_sp_error')}")
        lines.append(f"   vw_Depreciation rows: {db.get('depreciation_view_rows')}")
        lines.append(f"   MSME bill-wise outstanding (rupees): {db.get('msme_billwise_rupees')}")
        lines.append("")

        lines.append("=" * 80)
        lines.append("5. HEAD-TO-HEAD COMPARISONS (Excel working input vs DB)")
        excel_rev = working.get("revenue_total")
        db_rev_lakhs = rupees_to_lakhs(float(db.get("sales_rupees") or 0))
        compare_amount("Revenue / Sales total", excel_rev, db_rev_lakhs, "lakhs", lines)

        excel_msme = working.get("trade_payables_msme")
        db_msme_lakhs = rupees_to_lakhs(float(db.get("msme_billwise_rupees") or 0))
        compare_amount("Trade payables MSME (7-10 tab vs bill-wise DB)", excel_msme, db_msme_lakhs, "lakhs", lines)

        compare_amount(
            "Depreciation (IT tab vs vw_Depreciation)",
            working.get("depreciation_total"),
            0 if int(db.get("depreciation_view_rows") or 0) == 0 else working.get("depreciation_total"),
            "lakhs",
            lines,
        )

        lines.append("")
        lines.append("Top check-tab FS groups vs DB vw_trialbalance expensegrouphead (lakhs, directional only):")
        tb_view = {r.get("expensegrouphead"): float(r.get("NetClosing") or 0) for r in db.get("tb_view_by_expensegroup", [])}
        for group, vals in list(sorted(check.items(), key=lambda x: abs(x[1].get("fs_lakhs") or 0), reverse=True))[:15]:
            fs = vals.get("fs_lakhs") or 0
            # rough map ERP expensegrouphead contains words
            db_match = None
            for eg, net in tb_view.items():
                if eg and (eg.split()[0].lower() in group.lower() or group.lower().split()[0] in (eg or "").lower()):
                    db_match = rupees_to_lakhs(net)
                    break
            lines.append(f"   FS group '{group[:40]}' excel={fs:.2f} lakhs | nearest DB view group={db_match}")

    lines.append("")
    lines.append("=" * 80)
    lines.append("6. VERDICT FOR ACCOUNTING TEAM DISCUSSION")
    lines.append("   Items that MUST stay Excel/manual if DB != Excel:")
    verdicts = []
    if db.get("tb_sp_row_count") in (None, 0):
        verdicts.append("   - Trial balance SP returned no rows / failed — cannot replace TB Mar-26 sheet yet")
    if int(db.get("depreciation_view_rows") or 0) == 0:
        verdicts.append("   - vw_Depreciation is empty — IT/IT WORKING tab cannot come from DB")
    if mismatched_groups:
        verdicts.append(f"   - {mismatched_groups} groups: TB sheet totals != check 'raw' column — internal Excel reconciliation issue to confirm")
    if working.get("changes_in_inventory") is not None:
        verdicts.append("   - Changes in inventories is a working-paper figure — validate separately against stock SPs")
    if not verdicts:
        verdicts.append("   - Run full numeric diff after TB SP is confirmed working with accounting login context")
    lines.extend(verdicts)

    report = "\n".join(lines)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(report, encoding="utf-8")
    print(report)
    print(f"\nReport saved to: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
