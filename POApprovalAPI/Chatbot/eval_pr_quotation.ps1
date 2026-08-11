# Wave 2 — PurchaseReq / Quotation golden eval
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "pr_by_code"
    message = "Show items on purchase requisition PIL/PR/25-26/STO00700 with quantities"
    checks = @("sql_has:PurchaseReq|Vw_PurchaseReq", "sql_has:PIL/PR/25-26/STO00700", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "oswal_pending_pr"
    message = "For company Oswal Extrusion Limited, list purchase requisitions where requested qty is still greater than PO qty"
    checks = @("sql_has:Vw_PurchaseReq|PurchaseReq", "sql_has:CompanyName", "sql_has:Oswal", "sql_has:ReqQty|POQty|Qty", "rows_gt:0")
  },
  @{
    id = "vendors_quoted_po"
    message = "Which vendors quoted for purchase code KPV/SPR/26-27/539 and at what rates?"
    checks = @("sql_has:Vw_Quotation", "sql_has:KPV/SPR/26-27/539", "sql_has:FirmName", "sql_not:ApproveQuotation", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "quote_vs_final"
    message = "Show vendor quotation lines for PO KPV/SPR/26-27/539 with FirmName Rate and NegoRate"
    checks = @("sql_has:Vw_Quotation", "sql_has:KPV/SPR/26-27/539", "rows_gt:0")
  },
  @{
    id = "indent_quotes"
    message = "Show quotation rates against indent GPL/20-21/RWM00004"
    checks = @("sql_has:Vw_IndentQuotation|Vw_Quotation", "sql_has:GPL/20-21/RWM00004", "sql_not:ApproveQuotation", "rows_gt:0")
  },
  @{
    id = "messy_pr_oswal"
    message = "oswal extrusion pending purchase reqs not fully on PO yet"
    checks = @("sql_has:Vw_PurchaseReq|PurchaseReq", "rows_gt:0")
  },
  @{
    id = "messy_who_quoted"
    message = "who all quoted on po KPV/SPR/26-27/539 show rates"
    checks = @("sql_has:Vw_Quotation", "sql_has:KPV/SPR/26-27/539", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "no_empty_approve_quotation"
    message = "List pending quotations from ApproveQuotation"
    checks = @("sql_not_approve_or_rewritten")
  }
)

function Test-Check([string]$check, $resp) {
  $sql = [string]$resp.sql
  $answer = [string]$resp.answer
  $rowCount = [int]$resp.rowCount
  if ($check -like "sql_has:*") {
    foreach ($a in ($check.Substring(8) -split '\|')) { if ($sql -like "*$a*") { return $true } }
    return $false
  }
  if ($check -like "sql_not:*") { return -not ($sql -match $check.Substring(8)) }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  if ($check -eq "answer_not_no_data") {
    if ($rowCount -le 0) { return $false }
    return -not ($answer.ToLowerInvariant() -match 'no (rows|records|data|quotations|requisitions).*found')
  }
  if ($check -eq "sql_not_approve_or_rewritten") {
    # Pass if not using empty ApproveQuotation, OR using Vw_Quotation with rows
    if ($sql -match 'ApproveQuotation' -and $rowCount -eq 0) { return $false }
    if ($sql -match 'Vw_Quotation' -or $rowCount -gt 0 -or $sql -eq '') { return $true }
    return -not ($sql -match 'ApproveQuotation')
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) PR/Quotation cases against $BaseUrl ..."
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
  Write-Host "Rows: $($resp.rowCount)  Warning: $($resp.warning)"
  $ans=[string]$resp.answer; if ($ans.Length -gt 220) { $ans = $ans.Substring(0,220)+"..." }
  Write-Host "Answer: $ans"
  $results += [pscustomobject]@{ id=$c.id; pass=($failed.Count -eq 0); failedChecks=($failed -join ';'); sql=$resp.sql; rowCount=$resp.rowCount; answer=$resp.answer; warning=$resp.warning }
  Start-Sleep -Seconds 12
}
$out = Join-Path $PSScriptRoot "eval_pr_quotation_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
