#!/usr/bin/env python3
import json
from pathlib import Path

import urllib.request

ROOT = Path(__file__).resolve().parents[1]
EXCEL = ROOT / "docs" / "accounting" / "PIL Provisional FS_March-26_14.08.26 - Copy.xlsx"
REQ = ROOT / "docs" / "accounting" / "logs" / "fs-test-request.json"
OUT = ROOT / "docs" / "accounting" / "logs" / "fs-test-response.json"

req_json = REQ.read_text(encoding="utf-8").strip()
boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW"
file_bytes = EXCEL.read_bytes()

parts = [
    f"--{boundary}\r\n"
    f'Content-Disposition: form-data; name="requestJson"\r\n\r\n'
    f"{req_json}\r\n",
    f"--{boundary}\r\n"
    f'Content-Disposition: form-data; name="file"; filename="{EXCEL.name}"\r\n'
    f"Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n".encode(
        "utf-8"
    )
    + file_bytes
    + b"\r\n",
    f"--{boundary}--\r\n".encode("utf-8"),
]
payload = b"".join(p if isinstance(p, bytes) else p.encode("utf-8") for p in parts)

request = urllib.request.Request(
    "http://localhost:5120/api/financial-statements/generate",
    data=payload,
    method="POST",
)
request.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")

with urllib.request.urlopen(request, timeout=120) as resp:
    data = json.loads(resp.read().decode("utf-8"))

OUT.write_text(json.dumps(data, indent=2), encoding="utf-8")
result = data["result"]
print("Assets:", result["totalAssetsLakhs"])
print("Liab+Eq:", result["totalLiabilitiesAndEquityLakhs"])
print("Diff:", result["balanceSheetTotalLakhs"])
for section in result["balanceSheet"]:
    for line in section["lines"]:
        if line.get("label") == "Other current liabilities":
            print("Other current liabilities:", line["amountLakhs"])
for line in result["profitAndLoss"]:
    if str(line.get("label", "")).startswith("Changes in inventories"):
        print("Changes in inventory:", line["amountLakhs"])
