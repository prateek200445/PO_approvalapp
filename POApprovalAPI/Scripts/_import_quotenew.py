"""Import the new quotenew.xlsx quotes, rates, and customer into the running API."""
import json
import urllib.error
import urllib.request
from datetime import date, datetime
from openpyxl import load_workbook
from openpyxl.utils import column_index_from_string

API = "http://localhost:5115"
SRC = r"c:\Users\Admin\Desktop\approval\PO_approvalapp\quotenew.xlsx"
SEED = r"c:\Users\Admin\Desktop\approval\PO_approvalapp\POApprovalAPI\Data\WovenQuote\seed.json"

FABRIC = [
    (70, "PP"), (71, "HD"), (72, "LLDPE"), (73, "LDPE_FY"), (74, "LDPE7"), (75, "PP_H350"),
    (76, "LDPE4"), (77, "RP"), (78, "FILLER"), (79, "TPT"), (80, "UV"), (81, "COL"),
    (82, "BLACK"), (83, "CONDUCTIVE"),
]
COATING = [
    (86, "PP"), (87, "HD"), (88, "LLDPE"), (89, "LDPE_FY"), (90, "LDPE7"), (91, "PP_H350"),
    (92, "LDPE4"), (93, "RP"), (94, "FILLER"), (95, "TPT"), (96, "UV"), (97, "COL"), (98, "BLACK"),
]
LINER = [
    (101, "LLDPE"), (102, "LDPE_FY"), (103, "LDPE7"), (104, "PP_H350"), (105, "LDPE4"),
    (106, "RP"), (107, "FILLER"), (108, "TPT"), (109, "UV"), (110, "COL"), (111, "BLACK"),
]
MATERIALS = [
    "PP", "HD", "LLDPE", "LDPE_FY", "LDPE7", "PP_H350", "LDPE4", "RP", "FILLER", "TPT", "UV",
    "COL", "BLACK", "CONDUCTIVE", "BACKSEAM", "WHITE_PAPER", "BROWN_PAPER", "GLOSSY", "MATT",
    "METAL", "ADHESIVE", "INK", "SOLVENT",
]


def request(method, path, body=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(API + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=60) as response:
        raw = response.read().decode()
        return json.loads(raw) if raw else None


def text(value):
    return "" if value is None else str(value).strip()


def num(value):
    if value is None or value == "":
        return None
    if isinstance(value, bool):
        return None
    if isinstance(value, str):
        try:
            return float(value.strip())
        except ValueError:
            return None
    return float(value)


def day(value):
    if isinstance(value, datetime):
        return value.date().isoformat()
    if isinstance(value, date):
        return value.isoformat()
    return None


def recipe(sheet, col, pairs):
    out = {}
    for row, code in pairs:
        value = num(sheet.cell(row, col).value)
        if value:
            out[code] = value
    return out


def optional_num(value):
    parsed = num(value)
    return None if parsed in (None, 0) else parsed


wb = load_workbook(SRC, data_only=True)
sheet = wb["Quote Exports"]
quotes = []
for col in range(column_index_from_string("BBW"), column_index_from_string("BCS") + 1):
    customer = text(sheet.cell(7, col).value)
    plate = sheet.cell(198, col).value
    gsm = sheet.cell(207, col).value
    quotes.append({
        "sheet": sheet.cell(1, col).column_letter,
        "sheetPrice": num(sheet.cell(193, col).value),
        "draft": {
            "productType": "WovenBag",
            "quoteDate": day(sheet.cell(5, col).value),
            "sector": text(sheet.cell(6, col).value) or "Export",
            "customerName": customer,
            "ourRef": text(sheet.cell(8, col).value),
            "customerRef": text(sheet.cell(9, col).value),
            "bagType": text(sheet.cell(10, col).value) or "Tubular Bag",
            "fabricColour": text(sheet.cell(11, col).value),
            "faceWidthCm": num(sheet.cell(12, col).value),
            "gussetCm": num(sheet.cell(13, col).value),
            "lengthCm": num(sheet.cell(14, col).value),
            "top": text(sheet.cell(15, col).value),
            "totalWeightGram": num(sheet.cell(17, col).value),
            "fabricGsm": num(sheet.cell(18, col).value),
            "linerMicron": optional_num(sheet.cell(21, col).value),
            "glossyBoppMicron": optional_num(sheet.cell(22, col).value),
            "mattBoppMicron": optional_num(sheet.cell(23, col).value),
            "metalisedBoppMicron": optional_num(sheet.cell(24, col).value),
            "whitePaperGsm": optional_num(sheet.cell(25, col).value),
            "brownPaperGsm": optional_num(sheet.cell(26, col).value),
            "adhesiveGsm": optional_num(sheet.cell(27, col).value),
            "inkGsm": num(sheet.cell(28, col).value) or 0,
            "coatingGsm": num(sheet.cell(30, col).value) or 0,
            "handleGram": optional_num(sheet.cell(31, col).value),
            "fabricRecipe": recipe(sheet, col, FABRIC),
            "coatingRecipe": recipe(sheet, col, COATING),
            "linerRecipe": recipe(sheet, col, LINER),
            "fabricConversion": num(sheet.cell(132, col).value),
            "coatingConversion": num(sheet.cell(133, col).value),
            "linerConversion": num(sheet.cell(134, col).value),
            "wastageFabric": num(sheet.cell(136, col).value),
            "wastageLiner": num(sheet.cell(138, col).value),
            "wastageFinishing": num(sheet.cell(139, col).value),
            "realisationFabric": num(sheet.cell(140, col).value),
            "realisationLiner": num(sheet.cell(142, col).value),
            "bagFinishing": num(sheet.cell(146, col).value),
            "thumbNotch": text(sheet.cell(187, col).value).lower() == "yes",
            "metalDetection": text(sheet.cell(188, col).value).lower() == "yes",
            "perforation": text(sheet.cell(189, col).value).lower() == "yes",
            "flexoPrinting": num(sheet.cell(150, col).value) or 0,
            "rewindingPerKg": num(sheet.cell(151, col).value) or 0,
            "packingPerKg": num(sheet.cell(152, col).value),
            "palletPerKg": num(sheet.cell(153, col).value),
            "fobCostPerKg": num(sheet.cell(154, col).value),
            "paymentDays": num(sheet.cell(157, col).value),
            "profitPercent": num(sheet.cell(160, col).value),
            "usdInr": num(sheet.cell(163, col).value),
            "euroPerUsd": num(sheet.cell(164, col).value),
            "gbpPerUsd": num(sheet.cell(165, col).value),
            "freightUsdPerKg": num(sheet.cell(169, col).value) or 0,
            "commissionUsdPerKg": num(sheet.cell(170, col).value) or 0,
            "dimensionUnit": text(sheet.cell(176, col).value) or "CM",
            "terms": text(sheet.cell(190, col).value) or "FOB",
            "seaPort": text(sheet.cell(191, col).value),
            "currency": text(sheet.cell(192, col).value) or "USD",
            "packing": text(sheet.cell(194, col).value),
            "pcsPerBale": num(sheet.cell(195, col).value),
            "palletSize": text(sheet.cell(196, col).value),
            "bagsPerPallet": num(sheet.cell(197, col).value),
            "printPlateCost": num(plate),
            "printPlateCostText": "" if num(plate) is not None or plate is None else text(plate),
            "displayGsm": "" if gsm is None else str(gsm).strip(),
            "meterPerRoll": num(sheet.cell(208, col).value),
            "rollsPerPallet": num(sheet.cell(212, col).value),
            "remarks": text(sheet.cell(199, col).value),
        },
    })

AD_FORMULA = {
    "FW": {"adUseNetWidth": True, "adPatchExtraGsm": 28, "adValvePatchAdd": 1},
    "FX": {"adUseNetWidth": True, "adPatchExtraGsm": 28, "adValvePatchAdd": 1},
    "FY": {"adUseNetWidth": True, "adPatchExtraGsm": 28, "adValvePatchAdd": 1},
    "FZ": {"adUseNetWidth": True, "adPatchExtraGsm": 20, "adValvePatchAdd": 0},
    "GA": {"adUseNetWidth": True, "adPatchExtraGsm": 20, "adValvePatchAdd": 0},
    "GB": {"adUseNetWidth": True, "adPatchExtraGsm": 20, "adValvePatchAdd": 0},
}
ad = wb["Ad Star Bag"]
for col in range(column_index_from_string("FW"), column_index_from_string("GB") + 1):
    quotes.append({
        "sheet": ad.cell(1, col).column_letter if ad.cell(1, col).value else chr(0),
        "sheetPrice": num(ad.cell(33, col).value),
        "draft": {
            "productType": "AdStar",
            "quoteDate": day(ad.cell(4, col).value),
            "sector": text(ad.cell(5, col).value) or "Domestic",
            "customerName": text(ad.cell(6, col).value),
            "ourRef": text(ad.cell(7, col).value),
            "customerRef": text(ad.cell(8, col).value),
            "bagType": "AD Star",
            "dimensionUnit": "CM",
            "currency": "USD",
            "terms": "FOB",
            "adWidth": num(ad.cell(9, col).value),
            "adLength": num(ad.cell(10, col).value),
            "adPatch": num(ad.cell(11, col).value),
            "adValveLength": num(ad.cell(12, col).value),
            "adExtendedValve": num(ad.cell(13, col).value),
            "adFabricGsm": num(ad.cell(14, col).value),
            "adCoatingGsm": num(ad.cell(15, col).value),
            "adBoppGsm": num(ad.cell(16, col).value),
            "adOpenMouth": text(ad.cell(17, col).value).lower() == "yes",
            "adUv": text(ad.cell(18, col).value).lower() == "yes",
            "adPrinting": text(ad.cell(19, col).value),
            "adLinerMicron": num(ad.cell(21, col).value) or 0,
            "adPcsPerBale": num(ad.cell(24, col).value),
            "adPcsPerPallet": num(ad.cell(25, col).value),
            "adConversion": num(ad.cell(27, col).value),
            "adSeaFreight": num(ad.cell(29, col).value) or 0,
            "usdInr": num(ad.cell(30, col).value),
            "euroPerUsd": num(ad.cell(31, col).value),
            "gbpPerUsd": num(ad.cell(32, col).value),
            "fabricRecipe": {},
            "coatingRecipe": {},
            "linerRecipe": {},
        },
    })
# Restore real AD Star column letters.
for item, letter in zip(quotes[-6:], ["FW", "FX", "FY", "FZ", "GA", "GB"]):
    item["sheet"] = letter
    item["draft"].update(AD_FORMULA[letter])

existing = []
page = 1
while True:
    data = request("GET", f"/api/woven-quote?page={page}&pageSize=100")
    existing.extend(data["items"])
    if page * 100 >= data["total"]:
        break
    page += 1
have = {
    (row["productType"], row["quoteDate"][:10], row["customerName"].strip().lower(),
     (row.get("ourRef") or "").strip().lower(), (row.get("customerRef") or "").strip().lower()): row["id"]
    for row in existing
}

added = skipped = 0
mismatches = []
for item in quotes:
    draft = item["draft"]
    key = (draft["productType"], draft["quoteDate"], draft["customerName"].strip().lower(),
           draft["ourRef"].strip().lower(), draft["customerRef"].strip().lower())
    if key in have:
        if draft["productType"] != "AdStar":
            print("skip", item["sheet"], draft["customerName"], draft["ourRef"], draft["customerRef"])
            skipped += 1
            continue
        saved = request("PUT", f"/api/woven-quote/{have[key]}", {"draft": draft})
    else:
        saved = request("POST", "/api/woven-quote?username=workbook", {"draft": draft})
    price = saved["result"]["pricePerPiece"]
    delta = abs(price - (item["sheetPrice"] or 0))
    flag = "MATCH" if delta < 0.0001 else "DIFF"
    if flag == "DIFF":
        mismatches.append((item["sheet"], item["sheetPrice"], price, delta))
    print(flag, item["sheet"], draft["quoteDate"], draft["customerName"], draft["customerRef"], "sheet", item["sheetPrice"], "app", price)
    added += 1

rates = wb["RM Prices"]
rate_added = rate_existing = 0
for sector, header, first in (("Export", 4, 5), ("Domestic", 31, 32)):
    col = rates.cell(header, column_index_from_string("BO" if sector == "Export" else "BI")).column
    when = day(rates.cell(header, col).value)
    for offset, code in enumerate(MATERIALS):
        rate = num(rates.cell(first + offset, col).value)
        if rate is None:
            continue
        try:
            request("POST", "/api/woven-quote/rates", {
                "materialCode": code, "sector": sector, "effectiveDate": when, "rate": round(rate, 6),
            })
            rate_added += 1
        except urllib.error.HTTPError as error:
            print("rate failed", code, sector, error.read().decode())
            raise

customers = request("GET", "/api/woven-quote/masters")["customers"]
if not any(row["shortName"].strip().lower() == "berdal" and row["sector"] == "Export" for row in customers):
    request("POST", "/api/woven-quote/customers", {"sector": "Export", "fullName": "Berdal", "shortName": "Berdal", "isActive": True})
    print("customer added Berdal")
else:
    print("customer exists Berdal")

with open(SEED, encoding="utf-8") as handle:
    seed = json.load(handle)
known = {(row["code"], row["sector"], row["effectiveDate"][:10]) for row in seed["rates"]}
for sector, header, first in (("Export", 4, 5), ("Domestic", 31, 32)):
    col = rates.cell(header, column_index_from_string("BO" if sector == "Export" else "BI")).column
    when = day(rates.cell(header, col).value)
    for offset, code in enumerate(MATERIALS):
        rate = num(rates.cell(first + offset, col).value)
        if rate is None or (code, sector, when) in known:
            continue
        seed["rates"].append({"code": code, "sector": sector, "effectiveDate": when, "rate": round(rate, 6)})
if not any(row["shortName"].strip().lower() == "berdal" for row in seed["customers"]):
    seed["customers"].append({"sector": "Export", "fullName": "Berdal", "shortName": "Berdal"})
with open(SEED, "w", encoding="utf-8") as handle:
    json.dump(seed, handle, ensure_ascii=False, separators=(",", ":"))

print("SUMMARY added", added, "skipped", skipped, "rates", rate_added, "mismatches", len(mismatches))
for row in mismatches:
    print("MISMATCH", row)
