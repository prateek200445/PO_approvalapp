# Sales wave golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "ppl_recent_invoices"
    message = "Show recent sales invoices for Plastene Polyfilms Limited with buyer and bill amount"
    checks = @("sql_has:vw_Salesvoucher|SalesVoucher|vw_SalesInvList", "sql_has:Polyfilms|PPL", "sql_has:BuyerName|BillAMount|InvNo", "rows_gt:0")
  },
  @{
    id = "invoice_items_468"
    message = "Show items on sales invoice 468 for Plastene Polyfilms Limited with qty rate amount"
    checks = @("sql_has:SalesVoucherItem|vw_Salesvoucher", "sql_has:468", "sql_has:Polyfilms|PPL|CompanyName", "rows_gt:0")
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

function Test-Check([string]$check, $resp) {
  $sql = [string]$resp.sql
  $rowCount = [int]$resp.rowCount
  if ($check -like "sql_has:*") {
    foreach ($a in ($check.Substring(8) -split '\|')) { if ($sql -like "*$a*") { return $true } }
    return $false
  }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Sales cases against $BaseUrl ..."
foreach ($c in $cases) {
  Write-Host "`n=== $($c.id) ==="
  Write-Host "Q: $($c.message)"
  try {
    $body = @{ message = $c.message; topK = 4 } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 180
  } catch {
    $resp = [pscustomobject]@{ sql=""; answer="$_"; rowCount=0; warning="$_" }
    Write-Host "ERROR: $_" -ForegroundColor Red
  }
  $failed=@()
  foreach ($chk in $c.checks) { if (-not (Test-Check $chk $resp)) { $failed += $chk } }
  if ($failed.Count -eq 0) { $pass++; Write-Host "PASS" -ForegroundColor Green }
  else { $fail++; Write-Host "FAIL: $($failed -join ', ')" -ForegroundColor Red }
  Write-Host "SQL: $($resp.sql)"
  Write-Host "Rows: $($resp.rowCount)"
  $results += [pscustomobject]@{ id=$c.id; pass=($failed.Count -eq 0); sql=$resp.sql; rowCount=$resp.rowCount }
  Start-Sleep -Seconds 60
}
$out = Join-Path $PSScriptRoot "eval_sales_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
if ($fail -gt 0) { exit 1 } else { exit 0 }
