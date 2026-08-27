# Wave 7 â€” Gate pass golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "kpv_rgp_pending"
    message = "Which returnable gate passes still have pending qty for K.P. WOVEN PRIVATE LIMITED?"
    checks = @("sql_has:vw_returngatepasspending|Vw_ReturnGatePass", "sql_has:K.P.|CompName", "sql_has:PendingQty|Pending", "rows_gt:0")
  },
  @{
    id = "rgp_by_number"
    message = "Show items on returnable gate pass KPV/26-27/GP/162"
    checks = @("sql_has:Vw_ReturnGatePass|ReturnGatePass|vw_returngatepasspending", "sql_has:KPV/26-27/GP/162", "rows_gt:0")
  },
  @{
    id = "oswal_nrgp"
    message = "Show non-returnable gate passes for Oswal Extrusion Limited"
    checks = @("sql_has:Vw_NonReturnGatePass|NonReturnGatePass", "sql_has:CompName", "sql_has:Oswal", "sql_not:CompanyName", "rows_gt:0")
  },
  @{
    id = "nrgp_by_number"
    message = "Details of non-returnable gate pass OEL/26-27/NGP/2"
    checks = @("sql_has:Vw_NonReturnGatePass|NonReturnGatePass", "sql_has:OEL/26-27/NGP/2", "rows_gt:0")
  },
  @{
    id = "inward_igp"
    message = "Show inward return gate pass PPL/26-27/IGP/9"
    checks = @("sql_has:InwdReturnGatePass", "sql_has:PPL/26-27/IGP/9", "rows_gt:0")
  },
  @{
    id = "messy_pending"
    message = "kp woven pending returnable gate pass returns"
    checks = @("sql_has:vw_returngatepasspending|Vw_ReturnGatePass", "rows_gt:0")
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
  if ($check -like "sql_not:*") {
    $ban = $check.Substring(8)
    if ($ban -eq "CompanyName") { return -not [regex]::IsMatch($sql, '(?i)\bCompanyName\b') }
    return -not ($sql -match $ban)
  }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Gate Pass cases against $BaseUrl ..."
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
  Start-Sleep -Seconds 60
}
$out = Join-Path $PSScriptRoot "eval_gatepass_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
