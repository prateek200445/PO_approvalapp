# Wave 3 — Store outward / issue golden eval
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "slip_350_kp"
    message = "Show items on store issue slip 350 for company K.P. WOVEN PRIVATE LIMITED with item code name and qty"
    checks = @("sql_has:StoreOutwards", "sql_has:350", "sql_has:CompName", "sql_not:CompanyName", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "oswal_slip_215"
    message = "List materials issued on issue slip 215 at Oswal Extrusion Limited"
    checks = @("sql_has:StoreOutwards", "sql_has:215", "sql_has:CompName|Oswal", "rows_gt:0")
  },
  @{
    id = "oswal_issued_to_fibc"
    message = "For Oswal Extrusion Limited show recent store outwards issued to FIBC with item and qty"
    checks = @("sql_has:StoreOutwards", "sql_has:CompName", "sql_has:IssueTo|FIBC", "sql_not:CompanyName", "rows_gt:0")
  },
  @{
    id = "daily_outward_oswal"
    message = "For Oswal Extrusion Limited show items with outward quantity today from inward outward view"
    checks = @("sql_has:vw_ItemInwardOutward", "sql_has:companyname|Oswal", "sql_has:Outward", "rows_gt:0")
  },
  @{
    id = "item_wip00013_movement"
    message = "Inward and outward qty for item WIP00013 at Oswal Extrusion Limited"
    checks = @("sql_has:vw_ItemInwardOutward|StoreOutwards", "sql_has:WIP00013", "sql_has:Oswal", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "monthly_aug_2026"
    message = "Monthly outward quantities for Oswal Extrusion Limited in August 2026"
    checks = @("sql_has:vw_ItemMonthlyInwardOutward", "sql_has:2026", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "messy_issue_slip"
    message = "kp woven issue slip 350 what was issued"
    checks = @("sql_has:StoreOutwards", "sql_has:350", "rows_gt:0")
  },
  @{
    id = "messy_oswal_outward"
    message = "oswal extrusion how much stock issued outward today by item"
    checks = @("sql_has:vw_ItemInwardOutward|StoreOutwards", "sql_has:Oswal", "rows_gt:0")
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
    # Allow CompName; fail only if bare CompanyName appears as identifier (case-insensitive word)
    $ban = $check.Substring(8)
    if ($ban -eq "CompanyName") {
      return -not [regex]::IsMatch($sql, '(?i)\bCompanyName\b')
    }
    return -not ($sql -match $ban)
  }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  if ($check -eq "answer_not_no_data") {
    if ($rowCount -le 0) { return $false }
    return -not ($answer.ToLowerInvariant() -match 'no (rows|records|data|issues|outwards).*found')
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Store Outward cases against $BaseUrl ..."
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
$out = Join-Path $PSScriptRoot "eval_outward_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
