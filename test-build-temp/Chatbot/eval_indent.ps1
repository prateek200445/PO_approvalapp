# Indent approval golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "pending_indent_count"
    message = "How many indents are pending approval?"
    checks = @("sql_has:ApproveIndent", "sql_has:Pending", "rows_gte:1")
  },
  @{
    id = "pending_indent_oswal"
    message = "Show pending indents for Oswal Extrusion Limited"
    checks = @("sql_has:ApproveIndent", "sql_has:Pending", "sql_has:Oswal|Vw_StoreDeptt", "rows_gt:0")
  },
  @{
    id = "pending_store_indent"
    message = "Pending store indents for Oswal Extrusion Limited"
    checks = @("sql_has:ApproveIndent", "sql_has:Pending", "rows_gt:0")
  },
  @{
    id = "indent_by_number"
    message = "What items are on indent GPL/20-21/RWM00004?"
    checks = @("sql_has:Vw_StoreDeptt|ItemInfo", "sql_has:GPL/20-21/RWM00004", "governed_warning", "rows_gt:0")
  },
  @{
    id = "messy_pending_indent"
    message = "oswal pending indents list"
    checks = @("sql_has:ApproveIndent", "sql_has:Pending", "rows_gt:0")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Indent" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_indent_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
