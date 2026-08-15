# Ledger master golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "debtor_ageing_pil"
    message = "Debtor ageing for Plastene India Limited as on today"
    checks = @("sql_has:sp_Representative_Outstanding_Pivot", "sql_has:Sundry Debtors", "rows_gt:0", "governed_warning")
  },
  @{
    id = "party_overdue_polyfilms"
    message = "Overdue ageing for customer Commercial Bag Company at Plastene Polyfilms Limited"
    checks = @("sql_has:sp_Overdue_Ledger", "sql_has:Commercial Bag", "rows_gt:0", "governed_warning")
  },
  @{
    id = "ledger_count_sundry_debtors_pil"
    message = "How many ledgers are there under Sundry Debtors for Plastene India Limited?"
    checks = @("sql_has:LedgerMaster", "sql_has:COUNT|LedgerCount", "sql_has:Under", "sql_has:Sundry|Debtor", "sql_has:Plastene India", "rows_gte:1", "governed_warning")
  },
  @{
    id = "ledger_count_oswal"
    message = "How many ledgers does Oswal Extrusion Limited have?"
    checks = @("sql_has:LedgerMaster", "sql_has:COUNT", "sql_has:Oswal", "rows_gte:1", "governed_warning")
  },
  @{
    id = "ledger_groups_oswal"
    message = "List ledger groups for Oswal Extrusion Limited"
    checks = @("sql_has:LedgerMaster", "sql_has:Under", "sql_not_has:LedgerGroupMaster", "rows_gt:0", "governed_warning")
  },
  @{
    id = "ledger_count_polyfilms"
    message = "How many ledgers does Plastene Polyfilms Limited have?"
    checks = @("sql_has:LedgerMaster", "sql_has:COUNT", "sql_has:Polyfilms", "rows_gte:1")
  },
  @{
    id = "party_outstanding"
    message = "Pending balance for customer Commercial Bag Company at Plastene Polyfilms Limited"
    checks = @("sql_has:LedgerMaster", "sql_has:PendingBalance|Openingbalance", "sql_has:Commercial Bag", "rows_gt:0")
  },
  @{
    id = "company_gst"
    message = "What is the GST number for Oswal Extrusion Limited?"
    checks = @("sql_has:FactoryInfo", "sql_has:Oswal", "sql_has:GST|NewGSTNo", "rows_gte:1")
  },
  @{
    id = "no_ledger_group_master"
    message = "Show all account groups from ledger group master"
    checks = @("sql_has:Under|LedgerMaster", "sql_not_has:LedgerGroupMaster", "rows_gt:0")
  },
  @{
    id = "messy_ledger_count"
    message = "how many ledgers oswal extrusion has?"
    checks = @("sql_has:LedgerMaster", "sql_has:COUNT", "rows_gte:1")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Ledger" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_ledger_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
