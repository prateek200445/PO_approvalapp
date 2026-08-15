# Ops / PO amendments / PR / payment drafts golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "job_mrn_pending"
    message = "Job MRN pending work order for Plastene Polyfilms Limited"
    checks = @("sql_has:vw_JobMRN_PendingWO", "sql_has:Polyfilms", "governed_warning")
  },
  @{
    id = "po_amendment_pending"
    message = "PO amendments pending for Plastene Polyfilms Limited"
    checks = @("sql_has:Vw_AmendmentPurchaseOrder", "sql_has:PendingQty", "governed_warning")
  },
  @{
    id = "payment_draft"
    message = "Bill payment draft requests for Oswal Extrusion Limited"
    checks = @("sql_has:vw_BillPaymentReqDraft", "sql_has:Oswal", "governed_warning")
  },
  @{
    id = "pending_pr"
    message = "Pending purchase requisitions not yet ordered for Oswal Extrusion Limited"
    checks = @("sql_has:Vw_PurchaseReq", "sql_has:ReqQty", "governed_warning")
  },
  @{
    id = "ledger_grouping"
    message = "Ledger expense grouping for Plastene India Limited"
    checks = @("sql_has:vw_Commonledgergrouping", "governed_warning")
  },
  @{
    id = "voucher_approval_pending"
    message = "Pending account voucher approvals for Plastene India Limited"
    checks = @("sql_has:AccountVoucherApproval", "sql_has:Pending", "governed_warning")
  },
  @{
    id = "small_bag_production"
    message = "Daily small bag production report"
    checks = @("sql_has:vw_DailySmallBagProductionReport", "governed_warning")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Ops" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_ops_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
