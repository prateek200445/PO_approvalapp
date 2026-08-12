# Job work golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "formal_jwo"
    message = "Show job work order PIL2/JRO/14-15/1 with charges and items"
    checks = @("sql_has:Vw_EditJOBWorkOrder|Vw_JOBWORKORDER|JOBWORKORDER", "sql_has:PIL2/JRO/14-15/1", "rows_gt:0")
  },
  @{
    id = "ebd_qty_pil2"
    message = "Job work material quantities for Plastene India Limited (Unit -II) by item"
    checks = @("sql_has:VW_JobWork_EBD_DTL|VW_JobWork_EBD", "sql_has:Unit -II|companyname|CompanyName", "rows_gt:0")
  },
  @{
    id = "rec_jobwork"
    message = "Show receipts from job work for Plastene India Limited with MRNo"
    checks = @("sql_has:VW_RECJOBWORK_EBD_DTL|VW_RECJOBWORK", "sql_has:MRNo|JBIN", "rows_gt:0")
  },
  @{
    id = "rgp_jobwork_purpose"
    message = "Returnable gate passes for job work purpose"
    checks = @("sql_has:Vw_ReturnGatePass|ReturnGatePass", "sql_has:Job Work|JobWork|Purpose", "rows_gt:0")
  },
  @{
    id = "messy_jobwork"
    message = "pil unit 2 job work ebd item qty"
    checks = @("sql_has:VW_JobWork_EBD|Vw_EditJOBWorkOrder|VW_RECJOBWORK", "rows_gt:0")
  }
)

function Test-Check([string]$check, $resp) {
  $sql = [string]$resp.sql
  $rowCount = [int]$resp.rowCount
  if ($check -like "sql_has:*") {
    foreach ($a in ($check.Substring(8) -split '\|')) { if ($sql -like "*$a*") { return $true } }
    return $false
  }
  if ($check -like "sql_not:*") { return -not ($sql -match $check.Substring(8)) }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Job Work cases against $BaseUrl ..."
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
  $results += [pscustomobject]@{ id=$c.id; pass=($failed.Count -eq 0); failedChecks=($failed -join ';'); sql=$resp.sql; rowCount=$resp.rowCount }
  Start-Sleep -Seconds 60
}
$out = Join-Path $PSScriptRoot "eval_jobwork_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
