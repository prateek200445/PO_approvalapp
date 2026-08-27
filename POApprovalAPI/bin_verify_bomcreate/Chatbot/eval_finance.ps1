# Finance / inventory ageing golden eval (no LLM needed to verify SQL routing in response)
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "stock_ageing_pil"
    message = "Stock ageing by subgroup for Plastene India Limited"
    checks = @("sql_has:sp_Agingreport_SubgroupName", "sql_has:Plastene India", "governed_warning")
  },
  @{
    id = "group_overdue_days"
    message = "Group overdue 90 days for Sundry Debtors at Plastene India Limited"
    checks = @("sql_has:sp_Overdue_Group_Days", "sql_has:Sundry Debtors", "governed_warning")
  },
  @{
    id = "purchase_voucher_fy"
    message = "Purchase invoices for Plastene Polyfilms Limited FY 25-26"
    checks = @("sql_has:PurchaseVoucher", "sql_has:Polyfilms", "governed_warning")
  },
  @{
    id = "payment_voucher_fy"
    message = "Payment vouchers for Oswal Extrusion Limited FY 25-26"
    checks = @("sql_has:Payment", "sql_has:Oswal", "governed_warning")
  },
  @{
    id = "payment_receipt_fy"
    message = "Payment receipts for Plastene India Limited FY 25-26"
    checks = @("sql_has:PaymentReceipt", "sql_has:Plastene India", "governed_warning")
  },
  @{
    id = "advance_bill_outstanding"
    message = "Advance bill outstanding for Plastene India Limited"
    checks = @("sql_has:vw_advancebilloutstanding", "governed_warning")
  },
  @{
    id = "due_overdue_summary"
    message = "Due overdue summary"
    checks = @("sql_has:vw_DueOverDue", "governed_warning")
  },
  @{
    id = "cash_flow_lc"
    message = "Cash flow LC due dates"
    checks = @("sql_has:Vw_DueDateCashFlow", "governed_warning")
  },
  @{
    id = "sales_discount"
    message = "Sales discount report for Plastene Polyfilms Limited"
    checks = @("sql_has:sp_salesdiscount_companyname", "sql_has:Polyfilms", "governed_warning")
  },
  @{
    id = "sales_discount_customer"
    message = "Sales discount for customer Commercial Bag Company"
    checks = @("sql_has:sp_salesdiscount_customer", "sql_has:Commercial Bag", "governed_warning")
  },
  @{
    id = "outstanding_all_debtors"
    message = "All outstanding sundry debtors for Plastene India Limited"
    checks = @("sql_has:sp_OutstandingAll", "sql_has:Sundry Debtors", "sql_has:Plastene India", "governed_warning")
  },
  @{
    id = "import_po_mrn_pending"
    message = "Import PO MRN pending qty for Plastene India Limited"
    checks = @("sql_has:vw_ImportPurchasewithPOandMRNqty", "sql_has:pendingqty", "governed_warning")
  },
  @{
    id = "export_debtors_due"
    message = "Export debtors due for Plastene Polyfilms Limited"
    checks = @("sql_has:AutoMail_Export_Debtors_Due", "sql_has:Polyfilms", "governed_warning")
  },
  @{
    id = "msme_overdue_vendor"
    message = "MSME overdue ageing for vendor Bright Rubber at Oswal Extrusion Limited"
    checks = @("sql_has:sp_Overdue_Ledger_MSME", "sql_has:Bright Rubber|Oswal", "governed_warning")
  },
  @{
    id = "export_debtors_last3m"
    message = "Export debtors last 3 months for Plastene Polyfilms Limited"
    checks = @("sql_has:sp_Export_Debtors_Last3Months", "sql_has:Polyfilms", "governed_warning")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Finance" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_finance_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
