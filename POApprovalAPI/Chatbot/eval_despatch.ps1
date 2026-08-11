# Despatch wave golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "oswal_rolls"
    message = "Show recent roll despatch for Oswal Extrusion Limited with roll no net weight and party"
    checks = @("sql_has:vw_MISrolldespatch|MISRollforDespatch", "sql_has:Oswal", "sql_has:Companyname|CompanyName", "rows_gt:0")
  },
  @{
    id = "fibc_despatch"
    message = "FIBC despatch packing list bails for Oswal Extrusion Limited"
    checks = @("sql_has:FIBCDespatch", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "yarn_packing"
    message = "Yarn despatch packing list for Plastene India Limited"
    checks = @("sql_has:MIS_YarnDespatch", "sql_has:Plastene", "rows_gt:0")
  },
  @{
    id = "smallbag"
    message = "Small bag bails despatched for Oswal Extrusion Limited packing list"
    checks = @("sql_has:SmallBagBailForDespatch", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "rolls_waiting"
    message = "Rolls available for despatch at Oswal Extrusion Limited"
    checks = @("sql_has:vw_RollforDespatch", "sql_has:Oswal", "rows_gt:0")
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
Write-Host "Evaluating $($cases.Count) Despatch cases against $BaseUrl ..."
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
  Start-Sleep -Seconds 12
}
$out = Join-Path $PSScriptRoot "eval_despatch_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
if ($fail -gt 0) { exit 1 } else { exit 0 }
