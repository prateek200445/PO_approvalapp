# Production wave golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "factory_daily"
    message = "Factory production for Oswal Extrusion Limited recent days with tape fabric and small bag"
    checks = @("sql_has:vw_FactoryProduction", "sql_has:Oswal", "governed_warning", "rows_gt:0")
  },
  @{
    id = "tape_plant"
    message = "Tape production opening closing and Loom Dept for K.P. WOVEN PRIVATE LIMITED recent"
    checks = @("sql_has:vw_daily_tape_prod_New", "sql_has:K.P.|KP", "governed_warning", "rows_gt:0")
  },
  @{
    id = "loom_rolls"
    message = "Recent loom rolls produced at Oswal Extrusion Limited with roll no net weight and quality"
    checks = @("sql_has:vw_LoomProductionENtry", "sql_has:Oswal", "sql_has:CompanyName|Sysdate", "rows_gt:0")
  },
  @{
    id = "fibc_bags"
    message = "FIBC bag production BagPCS and weight for Oswal Extrusion Limited"
    checks = @("sql_has:VW_FIBCBagwiseProduction", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "production_ebd"
    message = "Production qty by plant for Oswal Extrusion Limited from production EBD detail"
    checks = @("sql_has:VW_PRODUCTION_EBD_DTL", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "wip_item"
    message = "WIP consumption for item WIP00013 at Oswal Extrusion Limited"
    checks = @("sql_has:vw_WIPReport", "sql_has:WIP00013", "sql_has:Oswal", "governed_warning", "rows_gt:0")
  },
  @{
    id = "smallbag"
    message = "Small bag cutting and stitching production for Plastene India Limited (Unit -II)"
    checks = @("sql_has:SmallBagProductionEntry", "sql_has:Plastene", "rows_gt:0")
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
  if ($check -eq "governed_warning") {
    return -not [string]::IsNullOrWhiteSpace([string]$resp.warning)
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Production cases against $BaseUrl ..."
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
$out = Join-Path $PSScriptRoot "eval_production_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
if ($fail -gt 0) { exit 1 } else { exit 0 }
