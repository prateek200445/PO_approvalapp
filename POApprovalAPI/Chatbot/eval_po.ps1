# PO approval golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "pending_po_count_oswal"
    message = "How many purchase orders are pending approval for Oswal Extrusion Limited?"
    checks = @("sql_has:ApprovePO|ApprovePOHOD", "sql_has:PurchasePayment", "sql_has:COUNT|PendingPOCount", "sql_has:Oswal", "rows_gte:1", "governed_warning")
  },
  @{
    id = "pending_po_count"
    message = "How many purchase orders are pending approval?"
    checks = @("sql_has:ApprovePO|ApprovePOHOD", "sql_has:Pending|COUNT|PendingPOCount", "rows_gte:1", "governed_warning")
  },
  @{
    id = "pending_po_oswal"
    message = "Show pending purchase orders for Oswal Extrusion Limited"
    checks = @("sql_has:ApprovePO|ApprovePOHOD", "sql_has:PurchasePayment", "sql_has:Oswal", "sql_has:Pending", "rows_gt:0", "governed_warning")
  },
  @{
    id = "po_queue_compare"
    message = "Compare pending PO counts in standard queue vs HOD queue"
    checks = @("sql_has:ApprovePO", "sql_has:ApprovePOHOD", "sql_has:Standard", "sql_has:HOD", "rows_gte:1")
  },
  @{
    id = "rejected_po_30d"
    message = "Show rejected POs in the last 30 days"
    checks = @("sql_has:Rejected", "sql_has:ApprovePO|ApprovePOHOD", "rows_gt:0")
  },
  @{
    id = "high_value_po_oswal_fy"
    message = "High value purchase orders for Oswal Extrusion Limited financial year 2025-26"
    checks = @("sql_has:PurchasePayment", "sql_has:Oswal", "sql_has:TotalAmount", "rows_gt:0")
  },
  @{
    id = "pending_po_vendor_kp"
    message = "Pending purchase orders to vendor K.P. Woven"
    checks = @("sql_has:FirmName|Vw_PurchaseOrder", "sql_has:Pending", "sql_has:ApprovePO|ApprovePOHOD", "rows_gt:0")
  },
  @{
    id = "po_header_polyfilms"
    message = "Show recent purchase order headers for Plastene Polyfilms Limited with currency and delivery"
    checks = @("sql_has:PurchasePayment", "sql_has:Polyfilms", "rows_gt:0")
  },
  @{
    id = "pending_wo"
    message = "How many work orders are pending approval?"
    checks = @("sql_has:ApproveWorkOrder", "sql_has:Pending", "rows_gte:1")
  },
  @{
    id = "po_allocation"
    message = "PO allocation limits for user jinal"
    checks = @("sql_has:POAllocation", "sql_has:jinal", "rows_gt:0")
  },
  @{
    id = "messy_pending_po"
    message = "any pos pending for oswal extrusion?"
    checks = @("sql_has:Pending", "sql_has:Oswal|PurchasePayment", "rows_gt:0")
  }
)

$fail = Invoke-EvalSuite -SuiteName "PO" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_po_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
