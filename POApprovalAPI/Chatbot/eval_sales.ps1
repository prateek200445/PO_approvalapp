param([string]$BaseUrl = "http://localhost:5115", [int]$SleepSeconds = 15)
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\_eval_common.ps1"

$cases = @(
  @{
    id = "ppl_total_sales_fy2526"
    message = "What is the total sales for Plastene India Limited for financial year 25-26?"
    checks = @("sql_has:vw_Sales_EBIDTA", "sql_has:SUM|TotalSales", "sql_has:Intergroup", "sql_has:Plastene India", "rows_gte:1", "governed_warning")
  },
  @{
    id = "oswal_sales_by_group"
    message = "Sales by product group for Oswal Extrusion Limited FY 2025-26"
    checks = @("sql_has:vw_Sales_EBIDTA", "sql_has:Groupname", "sql_has:Oswal", "sql_not_has:Destination", "rows_gt:0", "governed_warning")
  },
  @{
    id = "oswal_interunit"
    message = "Show inter-unit sales invoices for Oswal Extrusion Limited"
    checks = @("sql_has:vw_Salesvoucher", "sql_has:InterUnit|Inter Unit", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "ppl_country_wise_fy2526"
    message = "give country wise sales of plastene india limited for the last financial year 25-26"
    checks = @("sql_has:vw_Countrywise_sales_dashboard", "sql_has:InvYear", "sql_has:25-26", "sql_has:Country", "sql_has:Value|SalesAmount", "sql_not_has:Destination", "rows_gt:0")
  },
  @{
    id = "ppl_recent_invoices"
    message = "Show recent sales invoices for Plastene Polyfilms Limited with buyer and bill amount"
    checks = @("sql_has:vw_Salesvoucher|SalesVoucher|vw_SalesInvList", "sql_has:Polyfilms|PPL", "sql_has:BuyerName|BillAMount|InvNo", "rows_gt:0")
  },
  @{
    id = "invoice_items_468"
    message = "Show items on sales invoice 468 for Plastene Polyfilms Limited with qty rate amount"
    checks = @("sql_has:SalesVoucherItem|vw_Salesvoucher", "sql_has:468", "sql_has:Polyfilms|PPL|CompanyName", "governed_warning", "rows_gt:0")
  },
  @{
    id = "oswal_export_this_month"
    message = "List export sales invoices for Oswal Extrusion Limited this month"
    checks = @("sql_has:vw_Salesvoucher", "sql_has:Export|InvType", "sql_has:Oswal", "sql_not_has:POAllocation", "sql_has:InvDate", "governed_warning")
  },
  @{
    id = "oswal_export"
    message = "List export sales invoices for Oswal Extrusion Limited"
    checks = @("sql_has:vw_Salesvoucher|SalesVoucher", "sql_has:Oswal", "sql_has:Export|InvType", "rows_gt:0")
  },
  @{
    id = "inv_list_despatch"
    message = "Recent despatch invoice list for Oswal Extrusion Limited"
    checks = @("sql_has:vw_SalesInvList|vw_Salesvoucher", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "messy_sales"
    message = "polyfilms latest sales invoice buyers amounts"
    checks = @("sql_has:vw_Salesvoucher|SalesVoucher|vw_SalesInvList", "rows_gt:0")
  }
)

$fail = Invoke-EvalSuite -SuiteName "Sales" -Cases $cases -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds -ResultsFile "eval_sales_results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
