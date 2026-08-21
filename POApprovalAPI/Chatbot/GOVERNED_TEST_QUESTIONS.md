# Governed chatbot test questions (Phases 3b–7)

Manual test pack for **`POST /api/chat`** — user-style questions that should hit **governed** paths (no LLM SQL generation).

**Catalog version:** 3.3.0  
**API default:** `http://localhost:5115/api/chat`  
**Body:** `{ "message": "<question>", "topK": 4 }`

---

## How to confirm a response is governed

Pass **all** of these checks:

| Check | Pass criteria |
|-------|----------------|
| **Warning** | `warning` is non-empty and contains **`Governed`**, **`(governed`**, **`ERP `**, or **`Rewrote`** |
| **SQL object** | `sql` uses the **Expected object** below (not `ApproveQuotation`, `FactoryInfo` for vendors, bare `GETDATE()` on outward, etc.) |
| **Answer (optional)** | For SELECT paths with rows: answer may show **`Found N matching row(s)`** and **`Sample rows:`** (deterministic answer — no LLM prose) |
| **EXEC paths** | `sql` is a description like `EXEC sp_...` and **`rowCount > 0`** or valid empty with warning |

**Fail signs (not governed):**

- Empty `warning` and generic LLM answer
- Wrong table (`ApproveQuotation`, `LedgerGroupMaster`, `BillPaymentEntry.BillNo` for receipt lookup)
- `CompanyName` used where `CompName` is required (StoreOutwards / gate pass)

---

## Phase 3b — Finance SPs & import pending

### Outstanding all parties
**Expected:** `EXEC sp_OutstandingAll`  
**Warning contains:** `ERP` or `Governed`

- All outstanding sundry debtors for Plastene India Limited
- Show full outstanding all trade creditors at Oswal Extrusion Limited
- All parties outstanding debtors Plastene India

### Sales discount (company)
**Expected:** `sp_salesdiscount_companyname`

- Sales discount report for Plastene Polyfilms Limited
- Discount given on sales for polyfilms FY 25-26

### Sales discount (customer)
**Expected:** `sp_salesdiscount_customer`

- Sales discount for customer Commercial Bag Company
- Show discount report for buyer Commercial Bag Company

### Import PO / MRN pending
**Expected:** `vw_ImportPurchasewithPOandMRNqty`

- Import PO MRN pending qty for Plastene India Limited
- Pending MRN against import purchase orders for Plastene India
- Import purchase orders still pending receipt at Plastene India Limited

---

## Phase 4 — Ops SELECT paths

### Job MRN pending WO
**Expected:** `vw_JobMRN_PendingWO`

- Job MRN pending work order for Plastene Polyfilms Limited
- Show job work MRN still pending against work orders at polyfilms

### PO amendments
**Expected:** `Vw_AmendmentPurchaseOrder`

- PO amendments pending for Plastene Polyfilms Limited
- Pending purchase order amendments at Plastene Polyfilms

### Bill payment drafts
**Expected:** `vw_BillPaymentReqDraft`

- Bill payment draft requests for Oswal Extrusion Limited
- Show payment request drafts for oswal extrusion

### Purchase requisition (PR) gap
**Expected:** `Vw_PurchaseReq`

- Pending purchase requisitions not yet ordered for Oswal Extrusion Limited
- oswal extrusion pending purchase reqs not fully on PO yet
- Show items on purchase requisition PIL/PR/25-26/STO00700 with quantities

### Ledger grouping
**Expected:** `vw_Commonledgergrouping`

- Ledger expense grouping for Plastene India Limited
- Common ledger grouping for Plastene India Limited

### Voucher approval queue
**Expected:** `AccountVoucherApproval`

- Pending account voucher approvals for Plastene India Limited
- Which account vouchers are pending approval at Plastene India?

### Edit PO lines
**Expected:** `Vw_EditPurchaseOrder`

- Show edit purchase order lines for Oswal Extrusion Limited
- PO lines open for editing at Oswal

### Small bag production report (ops view)
**Expected:** `vw_DailySmallBagProductionReport` or `SmallBagProductionEntry`

- Daily small bag production report
- Small bag cutting and stitching production for Plastene India Limited (Unit -II)

---

## Phase 4 — Inventory EXEC SPs

**Warning contains:** `ERP`

| Intent | Expected SP | Example questions |
|--------|-------------|-------------------|
| Warehouse stock summary | `sp_WarehouseStockSummry` | Warehouse stock summary for Oswal Extrusion Limited FY 25-26 |
| Plant RM stock (loom) | `sp_Prod_GetRowMaterialStock_Loom` | Loom plant raw material stock for Oswal Extrusion Limited FY 25-26 |
| MIS consolidated | `sp_ac_getMISReportData` | MIS consolidated report for Plastene India Limited FY 25-26 |
| Top 100 purchased | `sp_top100_items` | Top 100 items purchased stores spares value wise |
| Auto roll stock | `sp_Auto_RollStock` | Auto roll stock report |
| Auto FIBC stock | `sp_Auto_FIBCStock` | Auto FIBC stock report for Oswal |
| Auto small bag stock | `sp_Auto_SmallBagStock` | Auto small bag stock report |
| EBIDTA sales pivot | `SP_Sales_EBIDTA_Pivot` | Sales EBIDTA pivot for Oswal FY 25-26 |
| EBIDTA purchase pivot | `SP_Purchase_EBIDTA_Pivot` | Purchase EBIDTA pivot Plastene India FY 25-26 |

---

## Phase 4 — Finance EXEC / SELECT (ageing & vouchers)

### Stock ageing (inventory — not debtor ageing)
**Expected:** `sp_Agingreport_SubgroupName`

- Stock ageing by subgroup for Plastene India Limited
- Inventory ageing report Plastene India

### Group overdue days
**Expected:** `sp_Overdue_Group_Days`

- Group overdue 90 days for Sundry Debtors at Plastene India Limited
- Overdue more than 90 days sundry debtors Plastene India

### Purchase / payment vouchers
**Expected:** `PurchaseVoucher` / `Payment` / `PaymentReceipt`

- Purchase invoices for Plastene Polyfilms Limited FY 25-26
- Payment vouchers for Oswal Extrusion Limited FY 25-26
- Payment receipts for Plastene India Limited FY 25-26

### Advance / due / cash flow
**Expected:** `vw_advancebilloutstanding` / `vw_DueOverDue` / `Vw_DueDateCashFlow`

- Advance bill outstanding for Plastene India Limited
- Due overdue summary
- Cash flow LC due dates

### Debtor/creditor ageing (monthly EXEC)
**Expected:** `sp_Representative_Outstanding_Pivot` or `sp_Overdue_Ledger`

- Debtor ageing for Commercial Bag Company at Plastene Polyfilms Limited
- Creditor ageing vendor Bright Rubber at Oswal Extrusion Limited

### Ledger statement
**Expected:** `sp_ac_LedgerSummary_BankRecoDate`

- Ledger statement for Commercial Bag Company at Plastene Polyfilms Limited
- Show ledger summary bank reco date for buyer Commercial Bag Company

---

## Phase 5 — Export debtors & stock analysis

### Export debtors due (snapshot table — not automail SP)
**Expected:** `AutoMail_Export_Debtors_Due`

- Export debtors due for Plastene Polyfilms Limited
- Overseas debtor bills due at polyfilms
- Show export debtors pending amount Plastene Polyfilms Limited

### Export debtors last 3 months
**Expected:** `sp_Export_Debtors_Last3Months`

- Export debtors last 3 months for Plastene Polyfilms Limited
- Last three months export sales by overseas debtor for polyfilms

### FIBC / product-line sales (monthly, per kg, rolling period)
**Expected:** `vw_Sales_EBIDTA` (governed SELECT — not LLM)

- What is the per kg of FIBC sold last 6 months?
- Show me monthly FIBC sales
- FIBC sales per kg for Plastene Polyfilms last 6 months
- Average rate per kg for jumbo bags sold last 3 months
- Monthly tape sales at Oswal Extrusion last 6 months

> **Note:** Filters `InterGroup <> 'Intergroup'`. PerKg = SUM(Amount)/SUM(netwt). Default period last 6 months when unstated. Company optional.

### Stock analysis (data caveat in warning)
**Expected:** `SP_STOCKANALYSIS_RPT_ALL`

- Stock analysis report for Plastene India Limited FY 25-26
- Opening closing stock analysis Plastene India 25-26

> **Note:** Warning should mention data caveats. Do not trust opening/closing numbers until ERP SP is fixed.

---

## Phase 6 — Procurement, DN/CN, gate pass, job work

### Final / awarded quotation
**Expected:** `FinalQuotation`

- Who was the awarded vendor on PO KPV/SPR/26-27/539?
- Final quotation vendor for purchase code KPV/SPR/26-27/539
- Which vendor won PO KPV/SPR/26-27/539?

### Vendor quotes by PO
**Expected:** `Vw_Quotation` (never `ApproveQuotation`)

- Which vendors quoted for purchase code KPV/SPR/26-27/539 and at what rates?
- who all quoted on po KPV/SPR/26-27/539 show rates
- Show vendor quotation lines for PO KPV/SPR/26-27/539 with FirmName Rate and NegoRate

### Indent quotations
**Expected:** `Vw_IndentQuotation`

- Show quotation rates against indent GPL/20-21/RWM00004
- Indent GPL/20-21/RWM00004 vendor quote rates

### Sales invoice line items
**Expected:** `SalesVoucherItem`

- Show items on sales invoice 468 for Plastene Polyfilms Limited with qty rate amount
- Invoice 468 line items polyfilms qty and rate

### Credit notes
**Expected:** `vw_creditnote` (CompanyName = ours, PartyName = customer)

- List credit notes for Plastene Polyfilms Limited with total credit amount and party
- Details of credit note PPL/CR/26-27/9
- polyfilms credit notes to commercial bag company

### Debit notes
**Expected:** `vw_DebitNote` (CompanyName = ours, PartyName = vendor)

- Show recent debit notes for Oswal Extrusion Limited with party and amount
- Details of debit note OEL/DB/26-27/16 amount party and type
- oswal extrusion debit notes provisional amounts

### Gate pass (early — by number)
**Expected:** `Vw_ReturnGatePass` / `Vw_NonReturnGatePass` / `InwdReturnGatePass`

- Show items on returnable gate pass KPV/26-27/GP/162
- Details of non-returnable gate pass OEL/26-27/NGP/2
- Show inward return gate pass PPL/26-27/IGP/9

### Gate pass (early — pending list)
**Expected:** `vw_returngatepasspending`

- Which returnable gate passes still have pending qty for K.P. WOVEN PRIVATE LIMITED?
- kp woven pending returnable gate pass returns
- Show non-returnable gate passes for Oswal Extrusion Limited

### Issue slip (early)
**Expected:** `StoreOutwards` + `CompName`

- Show items on store issue slip 350 for company K.P. WOVEN PRIVATE LIMITED with item code name and qty
- kp woven issue slip 350 what was issued
- List materials issued on issue slip 215 at Oswal Extrusion Limited

### Today outward (latest business date — not GETDATE)
**Expected:** `StoreOutwards` or `vw_ItemInwardOutward`

- oswal extrusion how much stock issued outward today by item
- For Oswal Extrusion Limited show items with outward quantity today from inward outward view
- Daily stock movement for Oswal Extrusion Limited

### Job work order
**Expected:** `Vw_EditJOBWorkOrder`

- Show job work order PIL2/JRO/14-15/1 with charges and items
- Formal job work orders for Plastene India Limited (Unit -II)

### Job work EBD qty
**Expected:** `VW_JobWork_EBD_DTL`

- Job work material quantities for Plastene India Limited (Unit -II) by item
- pil unit 2 job work ebd item qty

### Job work receipts
**Expected:** `VW_RECJOBWORK_EBD_DTL`

- Show receipts from job work for Plastene India Limited with MRNo
- Job work receipts with JBIN at Plastene India

### PO pending receipt (domestic — not import view)
**Expected:** `Vw_PurchaseOrder` + `PendingQty > 0`

- Show PO lines pending receipt for Oswal Extrusion Limited
- Purchase orders still to receive at Oswal Extrusion Limited
- PO lines with pending qty Oswal

### FIBC bag production
**Expected:** `VW_FIBCBagwiseProduction`

- FIBC bag production BagPCS and weight for Oswal Extrusion Limited
- FIBC bags produced at oswal extrusion recent

---

## Phase 7 — MRN (stores)

### MRN line items by MRNo
**Expected:** `Vw_StoreInwards`

- What all materials came in under receipt RM 283, with quantities?
- wat items came in mrn RM 283 show qty
- Show MRN RM 283 item wise qty

### MRN header (vendor / bill)
**Expected:** `Vw_StoreInwards`

- For receipt RM 283, who is the vendor and what is the bill number and amount?
- RM 283 vendor name and bill details

### MRN → PO link
**Expected:** `Vw_StoreInwards` + `PONo`

- Which purchase order was this material receipt RM 283 made against?
- PO number for MRN RM 283

### MRN payment status
**Expected:** `BillPaymentEntry` + `MRNno` (not bare `vw_MRNToBillPayment`)

- Has receipt RM 269 already been paid? If yes, show the payment number and UTR
- is rm 269 paid already? giv payment no and utr if ther
- any payment raised on mrn RM 269 how much money
- Was there any payment raised against material receipt RM 269 and for how much?

### Receipts by bill number
**Expected:** `Vw_StoreInwards.BillNo`

- Find receipts linked to bill number PPL/D/540
- find material reciept for bill PPL/D/540
- Which MRN is against bill PPL/D/540?

### Pending qty at company
**Expected:** `Vw_StoreInwards` + `CompanyName` + `PendingQty > 0`

- For company Oswal Extrusion Limited, list material receipts that still have pending quantity to receive
- oswal extrusion limited which mrns still have pending qty left

### Party / vendor receipts (-Purchase names)
**Expected:** `Vw_StoreInwards.PartyName`

- Show recent goods receipts for Plastene Polyfilms Ltd-Purchase with party and bill
- recnt goods reciepts for plastene polyfilms ltd-purchase with party bill

---

## Phase 7 — Vendor master & rates

### Vendor profile (GST / email / address)
**Expected:** `Vendor` or `vw_VendorListwithBankdtls` + `NewGSTNo`

- What is the GST number and email for vendor Bright Rubber?
- bright rubber ka gst aur email kya hai
- Show Chemline India Ltd GST PAN and email

### Vendor bank / IFSC
**Expected:** `vw_VendorListwithBankdtls`

- Show bank account IFSC and payment terms for Chemline India Ltd
- Chemline bank details and IFSC

### Vendor code
**Expected:** `Vendor.VendorCode`

- What is the vendor code for Lohia Corp Limited Gujarat?
- Vendor code for Lohia Corp

### Vendor rates
**Expected:** `VendorRate` + TOP + FirmName/ItemCode filter

- Show latest item rates from vendor Bright Rubber with Rate and NegoRate
- Which vendors have rates for item WIP00013 and at what rates?
- Bright Rubber rates for item WIP00013

### MSME vendor list
**Expected:** `Vendor` + `ISMSME`

- List MSME vendors with MSME number and firm name
- Show all MSME registered vendors

### Internal vendors
**Expected:** `InternalVendor`

- Which companies are listed as internal vendors?
- Internal vendor list group companies

### MSME overdue (EXEC)
**Expected:** `sp_Overdue_Ledger_MSME`

- MSME overdue ageing for vendor Bright Rubber at Oswal Extrusion Limited
- MSME overdue outstanding Bright Rubber Oswal Extrusion

---

## Phase 7 — Production & despatch

### Factory daily production
**Expected:** `vw_FactoryProduction`

- Factory production for Oswal Extrusion Limited recent days with tape fabric and small bag
- Oswal tape and fabric production last few days
- Recent factory production summary Oswal Extrusion

### WEBBING production (specific governed path)
**Expected:** `vw_FactoryProduction` + `WEBBING`

- WEBBING production for Oswal Extrusion Limited
- Factory webbing production oswal latest

### Tape plant (loom / FIBC dept)
**Expected:** `vw_daily_tape_prod_New`

- Tape production opening closing and Loom Dept for K.P. WOVEN PRIVATE LIMITED recent
- KP woven loom dept opening closing production
- Tape plant daily report K.P. Woven

### Loom production by quality
**Expected:** `vw_LoomProductionENtry`

- Recent loom rolls produced at Oswal Extrusion Limited grouped by quality
- Loom production by quality group Oswal Extrusion

### WIP consumption
**Expected:** `vw_WIPReport`

- WIP consumption for item WIP00013 at Oswal Extrusion Limited
- WIP report WIP00013 Oswal

### Production EBD by plant
**Expected:** `VW_PRODUCTION_EBD_DTL`

- Production qty by plant for Oswal Extrusion Limited from production EBD detail
- Production EBD Oswal by item and plant

### FIBC bag production (Phase 6 + 7)
**Expected:** `VW_FIBCBagwiseProduction`

- FIBC bag production BagPCS and weight for Oswal Extrusion Limited

### Roll despatch (shipped)
**Expected:** `vw_MISrolldespatch`

- Show recent roll despatch for Oswal Extrusion Limited with roll no net weight and party
- Roll despatch list Oswal last week

### Rolls waiting (not yet invoiced)
**Expected:** `vw_RollforDespatch`

- Rolls available for despatch at Oswal Extrusion Limited
- Needle loom rolls waiting despatch Oswal

### FIBC despatch
**Expected:** `FIBCDespatch`

- FIBC despatch packing list bails for Oswal Extrusion Limited
- FIBC bail despatch oswal packing list

### Yarn despatch
**Expected:** `MIS_YarnDespatch`

- Yarn despatch packing list for Plastene India Limited
- Yarn packing list despatch Plastene India

### Small bag bail despatch
**Expected:** `SmallBagBailForDespatch`

- Small bag bails despatched for Oswal Extrusion Limited packing list
- Small bag despatch bails Oswal

---

## Phase 7 — Users, indent lines, sales EBD

### User email / profile
**Expected:** `loginentry.dbo.LoginRights` (never `Password`)

- What is the email id of user jinal?
- jinal ka email kya hai
- Show full name and contact for user account5
- How many admin users are there?
- List finance people with email addresses
- Show purchase category users with email
- PO requester email from recent purchase orders

### Indent line items (not pending queue)
**Expected:** `Vw_StoreDeptt` + `ItemInfo`

- What items are on indent GPL/20-21/RWM00004?
- Show indent GPL/20-21/RWM00004 items with qty
- Indent line detail GPL/20-21/RWM00004 materials

### Sales EBD (MIS qty — not priced lines)
**Expected:** `VW_SALES_EBD_DTL`

- Item wise sales qty for Oswal Extrusion Limited
- Sales EBD detail Oswal by item
- Show sales quantity by item code at Plastene Polyfilms Limited

---

## Phase 8 — Wastage KPI, multi-material inventory, stitcher attendance

### Department wastage % vs production
**Expected:** `vw_FactoryProduction` (WastagePct column) or `vw_daily_tape_prod_New` (tape-plant wastage dept)

- What was Tape department wastage today at Oswal Extrusion Limited and what percent against production?
- Fabric department wastage today for Oswal how much percent against production?
- Show wastage percentage by department for Oswal Extrusion Limited today

### Multi-material inventory
**Expected:** `vw_itemwiseStock` with OR ItemName LIKE (Governed multi-material)

- What is the inventory of fabric/webbing/filler cord at Oswal Extrusion Limited?
- What is the inventory of fabric/webbing/filler cord at PIL2?
- Inventory of fabric and webbing at Plastene India unit 2

### Stitcher / sewer attendance
**Expected:** `Loginentry.dbo.Attendancemachine` + `empinfo`

- How many sewers were present yesterday?
- How many stitchers were present yesterday at Plastene India Limited?

---

## Quick smoke checklist (10 questions, one per area)

Copy-paste these for a fast governed pass:

1. `How many purchase orders are pending approval for Oswal Extrusion Limited?` → `ApprovePO`
2. `Has receipt RM 269 already been paid? show payment number and UTR` → `BillPaymentEntry`
3. `What is the GST number and email for vendor Bright Rubber?` → `Vendor`
4. `Which vendors quoted for purchase code KPV/SPR/26-27/539?` → `Vw_Quotation`
5. `Export debtors due for Plastene Polyfilms Limited` → `AutoMail_Export_Debtors_Due`
6. `Show items on store issue slip 350 for K.P. WOVEN PRIVATE LIMITED` → `StoreOutwards`
7. `Warehouse stock summary for Oswal Extrusion Limited FY 25-26` → `sp_WarehouseStockSummry`
8. `Factory production for Oswal Extrusion Limited recent days` → `vw_FactoryProduction`
9. `What is the email id of user jinal?` → `LoginRights`
10. `Show recent roll despatch for Oswal Extrusion Limited` → `vw_MISrolldespatch`

---

## Automated eval (optional)

When the API is running and LLM quota allows:

```powershell
cd POApprovalAPI\Chatbot
powershell -File eval_all.ps1 -BaseUrl http://localhost:5115 -SleepSeconds 15
```

Or run individual suites: `eval_mrn.ps1`, `eval_vendor.ps1`, `eval_ops.ps1`, `eval_inventory.ps1`, etc.

---

## Known non-governed / do not use for pass criteria

| Question type | Why it may not be governed |
|---------------|--------------------------|
| Random ad-hoc SQL | LLM fallback |
| `List pending quotations from ApproveQuotation` | Empty table — should rewrite or warn |
| Full stock analysis opening/closing | Governed but **data wrong** (ERP bug) |
| Anything answered via `sp_Automail_*` | Should not route there |

---

*Generated for catalog v3.2.0 — Phases 3b through 7 governed plug-ins.*
