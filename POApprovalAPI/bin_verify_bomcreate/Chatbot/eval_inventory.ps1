# Inventory / warehouse / MIS SP golden eval
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15
)

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "warehouse_stock_summary"
    message = "Warehouse stock summary for Oswal Extrusion Limited FY 25-26"
    checks = @("sql_has:sp_WarehouseStockSummry", "sql_has:Oswal", "governed_warning")
  },
  @{
    id = "plant_loom_rm_stock"
    message = "Loom plant raw material stock for Oswal Extrusion Limited FY 25-26"
    checks = @("sql_has:sp_Prod_GetRowMaterialStock_Loom", "sql_has:Oswal", "governed_warning")
  },
  @{
    id = "mis_report"
    message = "MIS consolidated report for Plastene India Limited FY 25-26"
    checks = @("sql_has:sp_ac_getMISReportData", "sql_has:Plastene India", "governed_warning")
  },
  @{
    id = "top100_purchased"
    message = "Top 100 items purchased stores spares value wise"
    checks = @("sql_has:sp_top100_items", "governed_warning")
  },
  @{
    id = "auto_roll_stock"
    message = "Auto roll stock report"
    checks = @("sql_has:sp_Auto_RollStock", "governed_warning")
  },
  @{
    id = "stock_analysis_pil"
    message = "Stock analysis report for Plastene India Limited FY 25-26"
    checks = @("sql_has:SP_STOCKANALYSIS_RPT_ALL", "sql_has:Plastene India", "governed_warning")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Inventory" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_inventory_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
