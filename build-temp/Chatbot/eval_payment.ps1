# Bill payment golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "pending_payment_jinal"
    message = "Show recent pending bill payments for approver jinal"
    checks = @("sql_has:BillPaymentHODApproval", "sql_has:Pending", "sql_has:jinal", "rows_gt:0")
  },
  @{
    id = "rejected_payments_month"
    message = "Show rejected bill payments in the last 30 days"
    checks = @("sql_has:BillPaymentHODApproval", "sql_has:Rejected", "rows_gt:0")
  },
  @{
    id = "approved_payment_total_oswal"
    message = "Total approved payment amount for Oswal Extrusion Limited since July"
    checks = @("sql_has:BillPaymentEntry|BillPaymentHODApproval", "sql_has:Oswal", "sql_has:SUM|PaymentAmount|Total", "rows_gte:1")
  },
  @{
    id = "pending_payments_list"
    message = "List pending bill payments with party name and amount"
    checks = @("sql_has:BillPaymentHODApproval|BillPaymentEntry", "sql_has:Pending", "rows_gt:0")
  },
  @{
    id = "payment_by_mrn"
    message = "Was payment raised for MRN RM 269 and for how much?"
    checks = @("sql_has:BillPaymentEntry", "sql_has:RM 269", "rows_gt:0")
  },
  @{
    id = "messy_pending_payments"
    message = "pending bill payments for jinal pls"
    checks = @("sql_has:jinal", "sql_has:Pending", "rows_gt:0")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Payment" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_payment_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
